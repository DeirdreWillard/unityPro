using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Google.Protobuf.Collections;
using Google.Protobuf;
using UnityGameFramework.Runtime;
using static UtilityBuiltin;
using Cysharp.Threading.Tasks;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class PDKGamePanel : UIFormBase
{
    private const float LONG_PRESS_DURATION = 0.1f;
    private const int MAX_CARDS_PER_ROW_PLAY_AREA = 8;
    private const int MAX_CARDS_PER_ROW_SETTLEMENT = 10;
    #region 聊天表情
    public GameObject chatPanel;
    public GameObject zhaNiao;
    #endregion
    [Header("===== 桌布 =====")]
    public Sprite[] tableCloth;
    private const string TABLE_BG_PREF_KEY = "PDK_SelectedTableBg"; // 保存桌布选择的键
    [Header("===== 状态 =====")]
    //翻倍按钮
    public GameObject DoubleBtn;
    private PDKCardManager cardManager;
    private PDKUIManager uiManager;
    private PDKGameLogic gameLogic;
    private PDKCardPool cardPool;
    private PDKCardHelper cardHelper; // 卡牌辅助工具实例
    private PDKProcedures pdkProcedure; // PDK流程管理器实例
    [Header("===== 界面=====")]
    public GameObject TuoGuan; // 托管面板
    public GameObject XJdaojishi; // 下局倒计时
    public GameObject ZBdaojishi; // 准备倒计时
    public GameObject womenVoiceOn; // 女声按钮
    public GameObject menVoiceOn;   // 男声按钮

    /// <summary>
    /// 获取PDK流程管理器实例（延迟加载）
    /// </summary>
    private PDKProcedures PdkProcedure
    {
        get
        {
            if (pdkProcedure == null)
            {
                pdkProcedure = GF.Procedure.GetProcedure<PDKProcedures>() as PDKProcedures;
            }
            return pdkProcedure;
        }
    }
    [Header("UI Configuration")]
    [SerializeField] private bool enableDebugLog = false;
    private PDKConstants.GameState currentGameState = PDKConstants.GameState.RUNFAST_WAIT;
    private List<PDKPlayerData> players = new List<PDKPlayerData>();
    private int currentPlayerIndex = -1;
    private int dealerPlayerIndex = -1;
    private int roundWinnerIndex = -1;
    private PDKPlayCardData lastPlayedCards = null;
    private List<int> passedPlayers = new List<int>();
    private int lastPassedCountBeforeClear = 0; // 存储清空前的过牌数（用于判断是否轮回）
    private bool isFirstRound = true;
    private long serverMaxDisCardPlayer = 0; // 当前这一轮出最大牌的玩家ID（客户端实时更新，用于判断是否本轮首出）
    private long serverLastMaxDiscard = 0;   // 服务端传来的上次出最大牌的玩家ID（仅重连时使用）
    private List<PDKCardData> myHandCards = new List<PDKCardData>();
    public PDKConstants.GameRuleConfig currentGameRules = new PDKConstants.GameRuleConfig();
    private PDKHintState hintState = new PDKHintState();
    private bool isReady = false;
    public bool IsReady => isReady;
    public Dictionary<long, GameObject> playerIdToHeadBg = new Dictionary<long, GameObject>();
    public Dictionary<long, GameObject> playerIdToPlayArea = new Dictionary<long, GameObject>();
    private Dictionary<long, List<int>> playerIdToLastCards = new Dictionary<long, List<int>>();
    // 【新增】记录每个玩家的当前手牌数量（用于剩牌数量显示）
    private Dictionary<long, int> playerIdToCardCount = new Dictionary<long, int>();
    public Sprite[] TisSprite;
    #region 桌子初始化（从服务器数据恢复游戏状态）
    /// <summary>
    /// 从服务器数据初始化跑得快桌子（用于首次进入或重连恢复）
    /// </summary>
    /// <param name="enterData">服务器返回的进入桌子数据</param>
    public override void OnClickClose()
    {
        if (PdkProcedure?.EnterRunFastDeskRs == null)
        {
            GF.LogWarning("[PDKGamePanel] OnClickClose: 无法获取桌子信息");
            return;
        }
        // 检查当前玩家是否是旁观者
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        bool isSeated = PdkProcedure.EnterRunFastDeskRs.DeskPlayers != null &&
                        PdkProcedure.EnterRunFastDeskRs.DeskPlayers.Any(p => p.BasePlayer.PlayerId == myPlayerId);
        // 旁观者无论什么状态都直接退出
        if (!isSeated)
        {
            Util.GetInstance().OpenMJConfirmationDialog("退出房间", "确定要退出房间吗?", () =>
            {
                varMenu.SetActive(false);
                PdkProcedure.Send_LeaveDeskRq();
            });
            return;
        }
        // 游戏未开始 - 直接退出
        if (PdkProcedure.EnterRunFastDeskRs.DeskState == RunFastDeskState.RunfastWait)
        {
            Util.GetInstance().OpenMJConfirmationDialog("退出房间", "确定要退出房间吗?", () =>
            {
                varMenu.SetActive(false);
                PdkProcedure.Send_LeaveDeskRq();
            });
        }
        // 游戏已开始 - 申请解散（仅对已坐下的玩家）
        else
        {
            // 关闭菜单
            varMenu.SetActive(false);
            GF.UI.ShowToast("游戏进行中，无法退出房间");

            // Util.GetInstance().OpenMJConfirmationDialog("解散房间", "游戏进行中，确定要申请解散房间吗?", () =>
            // {
            //     PdkProcedure.Send_Msg_ApplyMJDismissRq();
            // });
        }
    }
    /// <summary>
    /// 更新结算界面的按钮状态（根据是否解散房间）
    /// </summary>
    public void UpdateSettlementButtons()
    {
        if (PdkProcedure == null) return;
        //清除不要按钮显示
        // 如果结算面板正在显示
        if (varNextGamePlay != null && varNextGamePlay.activeSelf)
        {
            // 如果有解散请求，隐藏"下一局"按钮，显示"总结算"按钮
            if (PdkProcedure.HasDismissRequest)
            {
                varNextGame.SetActive(false);
                varRecord.SetActive(true);
                if (XJdaojishi != null) XJdaojishi.SetActive(false);
            }
            else
            {
                // 如果没有解散请求（或被拒绝），根据准备状态显示"下一局"按钮
                // 【修复】玩家自动准备时隐藏下局按钮
                varNextGame.SetActive(!isReady);
                varRecord.SetActive(false);
                if (isReady && XJdaojishi != null)
                {
                    XJdaojishi.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// 显示总结算战绩面板（所有局结束后）
    /// </summary>
    public async void ShowGameRecordPanel()
    {
        // 检查是否有单局战绩数据
        if (PdkProcedure.singleGameRecord == null) return;
        if (PdkProcedure.singleGameRecord.Record.MethodType != MethodType.RunFast) return;
        if (PdkProcedure.EnterRunFastDeskRs == null) return;
        // 重置局数和发牌标志（游戏结束，准备下一轮）
        PdkProcedure.currentRoundNum = 1;
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("SingleGameRecord", PdkProcedure.singleGameRecord.ToByteArray());
        uiParams.Set<VarInt64>("PlayerTime", new VarInt64 { Value = PdkProcedure.EnterRunFastDeskRs.BaseConfig.PlayerTime });
        uiParams.Set<VarInt32>("MethodType", (int)MethodType.RunFast);
        await GF.UI.OpenUIFormAwait(UIViews.MJGameSettle, uiParams);
    }


    /// </summary>
    /// <param name="otherClientPos">其他玩家的客户端位置</param>
    /// <param name="myClientPos">自己的客户端位置</param>
    /// <param name="totalPlayerNum">总玩家数</param>
    /// <returns>对应的头像框GameObject</returns>
    private GameObject GetHeadBgByPos(int otherClientPos, int myClientPos, int totalPlayerNum)
    {
        if (totalPlayerNum == 2)
        {
            return varHeadBg1;
        }
        else if (totalPlayerNum == 3)
        {

            int nextPos = (myClientPos + 1) % 3;
            int prevPos = (myClientPos - 1 + 3) % 3;

            if (otherClientPos == nextPos)
            {
                return varHeadBg1; // 右边
            }
            else if (otherClientPos == prevPos)
            {

                return varHeadBg2; // 左边······
            }
            else
            {
                return varHeadBg1;
            }
        }
        return varHeadBg1; // 默认返回右边位置而不是null
    }

    /// <summary>
    /// 根据头像框获取对应的出牌区域
    /// </summary>
    /// <param name="headBg">头像框GameObject</param>
    /// <returns>对应的出牌区域GameObject</returns>
    private GameObject GetPlayAreaByHeadBg(GameObject headBg)
    {
        if (headBg == null) return null;
        if (headBg == varHeadBg0) return varPlayArea;
        else if (headBg == varHeadBg1) return varPlayArea1; // 右边玩家（下家）的出牌区域
        else if (headBg == varHeadBg2) return varPlayArea2; // 左边玩家（上家）的出牌区域
        return null;
    }

    /// <summary>
    /// 设置玩家头像
    /// </summary>
    private void SetPlayerHead(GameObject headBg, DeskPlayer player)
    {
        if (headBg == null || player == null) return;
        var headTransform = headBg.transform.Find("head");
        var headImage = headTransform.GetComponent<RawImage>();
        Util.DownloadHeadImage(headImage, player.BasePlayer.HeadImage);
    }

    /// <summary>
    /// 设置玩家昵称
    /// </summary>
    private void SetPlayerNickname(GameObject headBg, DeskPlayer player)
    {
        if (headBg == null || player == null) return;
        var nicknameTransform = headBg.transform.Find("Name/NameText");
        var nicknameText = nicknameTransform.GetComponent<Text>();
        if (nicknameText != null)
        {
            string nickname = player.BasePlayer.Nick;
            if (nickname.Length > 8)
            {
                nickname = nickname.Substring(0, 7) + "...";
            }
            nicknameText.text = nickname;
        }
    }

    /// <summary>
    /// 设置玩家金币
    /// </summary>
    private void SetPlayerCoin(GameObject headBg, DeskPlayer player)
    {
        if (headBg == null || player == null) return;
        
        // 判断是否是本地玩家
        long myPlayerId = Util.GetMyselfInfo()?.PlayerId ?? 0;
        bool isLocalPlayer = player.BasePlayer.PlayerId == myPlayerId;
        
        // 处理主要积分显示 (Score/ScoreText)
        var coinTransform = headBg.transform.Find("Score/ScoreText");
        if (coinTransform != null)
        {
            var coinText = coinTransform.GetComponent<Text>();
            if (coinText != null)
            {
                if (double.TryParse(player.Coin, out double coin))
                {
                    // 使用与麻将一致的显示逻辑：本地玩家显示总分(BringInit)，其他玩家只显示BringInit
                    coinText.text = isLocalPlayer 
                        ? $"{player.Coin}({Util.FormatAmount(player.BringInit)})" 
                        : $"{Util.FormatAmount(player.BringInit)}";
                }
                else
                {
                    coinText.text = player.Coin;
                }
            }
        }
    }
    /// <summary>
    /// 设置玩家庄家标识（基于玩家ID判断）
    /// </summary>
    /// <param name="headBg">头像框GameObject</param>
    /// <param name="playerId">玩家ID</param>
    /// <param name="bankerId">庄家ID</param>
    private void SetPlayerBankerById(GameObject headBg, long playerId, long bankerId)
    {
        if (headBg == null) return;
        bool isBanker = (playerId == bankerId);
        // 查找庄家标识节点
        var bankerTransform = headBg.transform.Find("Banker");
        if (bankerTransform != null)
        {
            // 根据是否为庄家显示/隐藏庄家标识
            bankerTransform.gameObject.SetActive(isBanker);
        }

    }


    /// <summary>
    /// 遍历所有激活的头像框执行指定操作
    /// </summary>
    private void ForEachHeadBg(System.Action<GameObject> action)
    {
        GameObject[] allHeadBgs = { varHeadBg0, varHeadBg1, varHeadBg2 };
        foreach (var headBg in allHeadBgs)
        {
            if (headBg != null && headBg.activeSelf)
            {
                action(headBg);
            }
        }
    }

    /// <summary>
    /// 更新所有玩家的庄家标识（游戏开始后调用）
    /// </summary>
    /// <param name="deskPlayers">桌子上的玩家列表</param>
    /// <param name="totalPlayerNum">总玩家数</param>
    /// <param name="bankerId">庄家ID</param>
    public void UpdateAllPlayerBankerStatus(Google.Protobuf.Collections.RepeatedField<DeskPlayer> deskPlayers, int totalPlayerNum, long bankerId)
    {
        // 【关键修复】更新庄家ID字段
        this.bankerId = bankerId;

        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        var myPlayer = deskPlayers.FirstOrDefault(p => p.BasePlayer.PlayerId == myPlayerId);
        int myClientPos = 0; // 自己永远在0号位
        Position myServerPos = myPlayer != null ? myPlayer.Pos : Position.Default;

        foreach (var player in deskPlayers)
        {
            GameObject headBg = null;

            // 判断是否是自己
            if (player.BasePlayer.PlayerId == myPlayerId)
            {
                headBg = varHeadBg0;
            }
            else
            {
                int clientPos = Util.GetPosByServerPos_pdk(myServerPos, player.Pos, totalPlayerNum);
                headBg = GetHeadBgByPos(clientPos, myClientPos, totalPlayerNum);
            }

            if (headBg != null)
            {
                SetPlayerBankerById(headBg, player.BasePlayer.PlayerId, bankerId);
            }
        }

    }

    /// <summary>
    /// 设置玩家在线状态
    /// </summary>
    public void SetPlayerOnlineStatus(GameObject headBg, DeskPlayer player)
    {
        if (headBg == null || player == null) return;

        // 查找离线标识节点
        var offlineTransform = headBg.transform.Find("offline");
        if (offlineTransform != null)
        {
            // 根据在线状态显示/隐藏离线标识（State 为 OffLine 时显示离线标识）
            offlineTransform.gameObject.SetActive(player.State == PlayerState.OffLine);
        }
    }



    /// <summary>
    /// 设置玩家托管状态
    /// </summary>
    public void SetPlayerTuoGuanStatus(GameObject headBg, DeskPlayer player)
    {
        if (headBg == null || player == null) return;

        // 查找托管标识节点
        var tuoGuanTransform = headBg.transform.Find("TuoGuan");
        if (tuoGuanTransform != null)
        {
            // 显示/隐藏托管标识
            tuoGuanTransform.gameObject.SetActive(player.State == PlayerState.ToGuo);
        }

        // 如果是自己，显示/隐藏大托管面板
        if (player.BasePlayer.PlayerId == Util.GetMyselfInfo()?.PlayerId)
        {
            ShowTuoGuanGo(player.State == PlayerState.ToGuo);
        }
    }


    /// <summary>
    /// 格式化金币显示（例如：1234 -> 1.23K）
    /// </summary>
    private string FormatCoin(long coin)
    {
        if (coin < 1000)
        {
            return coin.ToString();
        }
        else if (coin < 1000000)
        {
            return $"{(coin / 1000f):F2}K";
        }
        else
        {
            return $"{(coin / 1000000f):F2}M";
        }
    }





    /// <summary>
    /// 更新Panel的游戏状态（LastPlayedCards）
    /// </summary>
    private void UpdatePanelStateWithLastPlayedCards(Msg_EnterRunFastDeskRs enterData, long lastMaxDiscardPlayerId, List<int> lastMaxDiscardCardIds)
    {
        // 恢复手牌数据
        if (enterData.Cards != null && enterData.Cards.Count > 0)
        {
            myHandCards = ConvertCardIdsToCardDataList(enterData.Cards.ToList());
        }

        // 恢复LastPlayedCards
        if (lastMaxDiscardPlayerId > 0 && lastMaxDiscardCardIds != null && lastMaxDiscardCardIds.Count > 0)
        {
            int playerIndex = GetPlayerIndexByServerId(lastMaxDiscardPlayerId);
            if (playerIndex != -1)
            {
                List<PDKCardData> cardDataList = ConvertCardIdsToCardDataList(lastMaxDiscardCardIds);
                PDKConstants.CardType cardType = PDKRuleChecker.DetectCardType(cardDataList, null);
                int weight = PDKRuleChecker.CalculateWeight(cardDataList, cardType);

                lastPlayedCards = new PDKPlayCardData
                {
                    playerId = playerIndex,
                    cards = new List<PDKCardData>(cardDataList),
                    cardType = cardType,
                    weight = weight
                };
                serverMaxDisCardPlayer = lastMaxDiscardPlayerId;
            }
        }
    }
    #endregion
    #region Constants
    // Constants moved to top of class
    #endregion

    #region 辅助方法





    /// <summary>
    /// 根据提示选中卡牌
    /// </summary>
    /// <param name="hintCards">提示的牌数据</param>
    private void SelectHintCards(List<PDKCardData> hintCards)
    {
        if (hintCards == null || hintCards.Count == 0 || varHandCard == null)
        {
            GF.LogWarning($"[PDK选牌] SelectHintCards参数检查失败 - hintCards: {hintCards?.Count ?? 0}, varHandCard: {varHandCard != null}");
            return;
        }

        GF.LogInfo($"[PDK选牌] 开始选择提示牌，数量：{hintCards.Count}");
        var allHandCards = GetAllHandCardComponents();
        GF.LogInfo($"[PDK选牌] 获取到手牌组件数量：{allHandCards?.Count ?? 0}");

        int selectedCount = 0;
        // 从右往左遍历（从列表末尾向开头），优先选择靠右边的牌
        for (int i = allHandCards.Count - 1; i >= 0; i--)
        {
            var cardComponent = allHandCards[i];
            var cardData = cardHelper.ConvertToCardData(cardComponent);
            if (cardData == null) continue;

            // 查找是否在提示列表中
            bool shouldSelect = hintCards.Any(hint =>
                hint.suit == cardData.suit && hint.value == cardData.value);

            if (shouldSelect && !IsCardSelected(cardComponent))
            {
                // 直接操作选择列表
                selectedCards.Add(cardComponent);
                selectedCardNumbers.Add(cardComponent.numD);
                cardComponent.SetSelected(true);

                // 上移卡牌
                var rectTransform = cardComponent.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    Vector3 targetPos = cardComponent.GetOriginalPosition() + new Vector3(0, 30f, 0);
                    rectTransform.localPosition = targetPos;
                }

                selectedCount++;
                GF.LogInfo($"[PDK选牌] 选中第{selectedCount}张牌: {cardData.value}{cardData.suit}");
            }
        }

        GF.LogInfo($"[PDK选牌] 选牌完成，共选中 {selectedCount} 张牌，selectedCards: {selectedCards.Count}, selectedCardNumbers: {selectedCardNumbers.Count}");

        // 更新出牌按钮状态
        CheckSelectedCardsValidity();
    }

    #endregion

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        InitializeManagers();
        BindUIEvents();
        InitVoiceGo();  // 初始化语音和聊天功能
        varNextGamePlay.SetActive(false);
        // 初始化时隐藏所有玩家的时钟(准备阶段不应显示时钟)
        HideAllPlayerClocks();
        // 初始化桌布选项按钮
        InitializeBackgroundButtons();
        // 加载保存的桌布设置
        LoadSavedTableBg();
        // 初始化声音按钮显示状态
        InitializeVoiceButtons();

        // 根据服务器返回的GPS权限数据控制GPS按钮显示
        InitializeGPSButton();

        // 播放跑得快背景音乐
        Sound.PlayMusic("Audio/pdk/bgmusic.mp3");
    }
    protected override async void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "打开设置":
                varMenu.SetActive(!varMenu.activeSelf);
                break;
            case "设置":
                varSetBg.SetActive(!varSetBg.activeSelf);
                break;
            case "游戏音乐开关":
                var gameMusicBtn = varSetBg.transform.Find("SettingPanel/CommonPanel/EFF/On");
                gameMusicBtn.gameObject.SetActive(!gameMusicBtn.gameObject.activeSelf);
                // 切换音效开关状态并同步到音频系统
                bool sfxEnabled = gameMusicBtn.gameObject.activeSelf;
                Sound.SetSfxEnabled(sfxEnabled);
                break;
            case "背景音乐开关":
                var backMusicBtn = varSetBg.transform.Find("SettingPanel/CommonPanel/BG/On");
                backMusicBtn.gameObject.SetActive(!backMusicBtn.gameObject.activeSelf);
                // 切换背景音乐开关状态并同步到音频系统
                bool musicEnabled = backMusicBtn.gameObject.activeSelf;
                Sound.SetMusicEnabled(musicEnabled);
                if (musicEnabled)
                {
                    // 如果开启,恢复播放背景音乐
                    if (!Sound.IsMusicPlaying())
                    {
                        Sound.PlayMusic("Audio/pdk/bgmusic.mp3");
                    }
                    else
                    {
                        Sound.ResumeMusic();
                    }
                }
                else
                {
                    // 如果关闭,暂停背景音乐
                    Sound.PauseMusic();
                }
                break;
            case "强制解散":
                // 弹出确认对话框
                Util.GetInstance().OpenMJConfirmationDialog("确认解散", "确定要强制解散当前房间吗？", () =>
                {
                    // 确认解散
                    PdkProcedure.Send_DismissDesk();
                });
                break;
            case "退出":
                OnExitButtonClick();
                break;
            case "战绩":
                ShowGameRecordPanel();
                break;
            case "下一局":
                HandleNextGameButton();
                break;
            case "准备":
                HandleReadyButton();
                break;
            case "取消准备":
                HandleCancelReadyButton();
                break;
            case "BtnMessage":
                // 聊天按钮
                OnChatButtonClick();
                break;
            case "翻倍":
                OnDoubleBtnClick(2); // 2倍
                break;
            case "不翻倍":
                OnDoubleBtnClick(1); // 不翻倍
                break;
            case "超级翻倍":
                OnDoubleBtnClick(4); // 4倍
                break;
            case "BtnQiFu":
                ShowBlessingUI();
                break;
            case "通用设置":
                // 切换到通用设置面板
                var commonOn = varSetBg.transform.Find("SettingPanel/Common/CommonOn");
                var pdkOff = varSetBg.transform.Find("SettingPanel/PDK/PDKOn");
                var commonPanel = varSetBg.transform.Find("SettingPanel/CommonPanel");
                var gamePanel = varSetBg.transform.Find("SettingPanel/GamePanel");

                if (commonOn != null) commonOn.gameObject.SetActive(true);
                if (pdkOff != null) pdkOff.gameObject.SetActive(false);
                if (commonPanel != null) commonPanel.gameObject.SetActive(true);
                if (gamePanel != null) gamePanel.gameObject.SetActive(false);
                GF.LogInfo("[PDK设置] 切换到通用设置面板");
                break;
            case "跑的快设置":
                // 切换到跑的快设置面板
                var commonOff = varSetBg.transform.Find("SettingPanel/Common/CommonOn");
                var pdkOn = varSetBg.transform.Find("SettingPanel/PDK/PDKOn");
                var commonPanel1 = varSetBg.transform.Find("SettingPanel/CommonPanel");
                var gamePanel1 = varSetBg.transform.Find("SettingPanel/GamePanel");
                if (commonOff != null) commonOff.gameObject.SetActive(false);
                if (pdkOn != null) pdkOn.gameObject.SetActive(true);
                if (commonPanel1 != null) commonPanel1.gameObject.SetActive(false);
                if (gamePanel1 != null) gamePanel1.gameObject.SetActive(true);

                GF.LogInfo("[PDK设置] 切换到跑的快设置面板");
                break;
            case "女声":
                // 选择女声
                var womenVoiceOn = varSetBg.transform.Find("SettingPanel/CommonPanel/Women/On");
                var womenVoiceOff = varSetBg.transform.Find("SettingPanel/CommonPanel/Women/Off");
                var menVoiceOn = varSetBg.transform.Find("SettingPanel/CommonPanel/Men/On");
                var menVoiceOff = varSetBg.transform.Find("SettingPanel/CommonPanel/Men/Off");

                if (womenVoiceOn != null) womenVoiceOn.gameObject.SetActive(true);
                if (womenVoiceOff != null) womenVoiceOff.gameObject.SetActive(false);
                if (menVoiceOn != null) menVoiceOn.gameObject.SetActive(false);
                if (menVoiceOff != null) menVoiceOff.gameObject.SetActive(true);

                // 保存语音性别设置到PlayerPrefs
                PlayerPrefs.SetString("MahjongVoiceGender", "female");
                PlayerPrefs.Save();

                GF.LogInfo("[PDK设置] 切换到女声");
                break;

            case "男声":
                // 选择男声
                var womenVoiceOn2 = varSetBg.transform.Find("SettingPanel/CommonPanel/Women/On");
                var womenVoiceOff2 = varSetBg.transform.Find("SettingPanel/CommonPanel/Women/Off");
                var menVoiceOn2 = varSetBg.transform.Find("SettingPanel/CommonPanel/Men/On");
                var menVoiceOff2 = varSetBg.transform.Find("SettingPanel/CommonPanel/Men/Off");

                if (womenVoiceOn2 != null) womenVoiceOn2.gameObject.SetActive(false);
                if (womenVoiceOff2 != null) womenVoiceOff2.gameObject.SetActive(true);
                if (menVoiceOn2 != null) menVoiceOn2.gameObject.SetActive(true);
                if (menVoiceOff2 != null) menVoiceOff2.gameObject.SetActive(false);

                // 保存语音性别设置到PlayerPrefs
                PlayerPrefs.SetString("MahjongVoiceGender", "male");
                PlayerPrefs.Save();

                GF.LogInfo("[PDK设置] 切换到男声");
                break;
            case "BtnCancelAuto":
                // 取消托管
                Msg_CancelAutoRq req = new Msg_CancelAutoRq();
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_CancelAutoRq, req);
                // 隐藏托管面板
                ShowTuoGuanGo(false);
                break;

        }
    }

    #region 托管面板
    /// <summary>
    /// 显示或隐藏托管取消面板
    /// </summary>
    /// <param name="show">是否显示</param>
    public void ShowTuoGuanGo(bool show)
    {
        if (TuoGuan != null)
        {
            TuoGuan.SetActive(show);
            GF.LogInfo($"[托管面板] {(show ? "显示" : "隐藏")}托管取消面板");
        }
    }
    #endregion

    public async void ShowBlessingUI()
    {
        bool IsSpectator = Util.IsMySelf(PdkProcedure.EnterRunFastDeskRs.Creator.PlayerId);
        if (IsSpectator)
        {
            GF.LogWarning($"[PDK游戏界面] 打开祈福界面失败：观战状态无法使用祈福功能");
            return;
        }
        //如果有blessing界面并是显示激活状态就不打开了
        if (GF.UI.HasUIForm(UIViews.Blessing))
        {
            GF.LogInfo($"[PDK游戏界面] 祈福界面已打开，跳过重复打开");
            return;
        }

        // 获取自己的头像位置（varHeadBg0是自己的位置）
        Vector3 targetPos = varHeadBg0.transform.position;

        var uiParams = UIParams.Create();
        uiParams.Set<VarVector3>("targetPosition", targetPos);
        uiParams.Set<VarBoolean>("isLeft", true);
        await GF.UI.OpenUIFormAwait(UIViews.Blessing, uiParams);
    }

    public async void ShowBlessingUI_other(SynPrayRs synPrayRs)
    {
        //如果有blessing界面并是显示激活状态就不打开了
        if (GF.UI.HasUIForm(UIViews.Blessing))
        {
            GF.LogInfo($"[PDK游戏界面] 祈福界面已打开，跳过他人祈福通知");
            return;
        }

        Vector3 targetPos = varHeadBg0.transform.position; // 默认位置
        var uiParams = UIParams.Create();
        // 根据synPrayRs.PlayerId找到对应的头像位置
        if (playerIdToHeadBg.TryGetValue(synPrayRs.PlayerId, out GameObject headBg))
        {
            targetPos = headBg.transform.position;
            // 判断是否为左侧位置（headBg0或HeadBg2，不区分大小写）
            bool isLeft = headBg.name.Equals("headBg0", StringComparison.OrdinalIgnoreCase) ||
                         headBg.name.Equals("HeadBg2", StringComparison.OrdinalIgnoreCase);
            uiParams.Set<VarBoolean>("isLeft", isLeft);
        }
        uiParams.Set<VarVector3>("targetPosition", targetPos);
        uiParams.Set<VarByteArray>("synPrayRs", synPrayRs.ToByteArray());
        await GF.UI.OpenUIFormAwait(UIViews.Blessing, uiParams);
    }
    /// <summary>
    /// 加载保存的桌布设置
    /// </summary>
    private void LoadSavedTableBg()
    {
        if (!PlayerPrefs.HasKey(TABLE_BG_PREF_KEY))
            return;

        int savedTableBgIndex = PlayerPrefs.GetInt(TABLE_BG_PREF_KEY, 0);

        // 验证索引有效性
        if (savedTableBgIndex < 0 || savedTableBgIndex >= tableCloth.Length)
        {
            GF.LogWarning($"[PDK桌布] 无效的保存索引: {savedTableBgIndex}，重置为0");
            savedTableBgIndex = 0;
        }

        // 设置背景图
        SetTableBgSprite(savedTableBgIndex);

        // 更新选中状态可视化
        UpdateTableBgSelectionUI(savedTableBgIndex);

    }

    /// <summary>
    /// 更新桌布设置
    /// </summary>
    private void UpdateTableBg(int tableBgIndex)
    {
        // 保存到本地缓存
        PlayerPrefs.SetInt(TABLE_BG_PREF_KEY, tableBgIndex);
        PlayerPrefs.Save();

        // 设置背景图
        SetTableBgSprite(tableBgIndex);

        // 更新UI选中状态
        UpdateTableBgSelectionUI(tableBgIndex);
    }

    /// <summary>
    /// 设置背景图片
    /// </summary>
    private void SetTableBgSprite(int tableBgIndex)
    {
        if (tableBgIndex < 0 || tableBgIndex >= tableCloth.Length)
        {
            GF.LogWarning($"[PDK桌布] 无效的桌布索引: {tableBgIndex}");
            return;
        }

        if (varBg != null)
        {
            var bgImage = varBg.GetComponent<Image>();
            if (bgImage != null && tableCloth[tableBgIndex] != null)
            {
                bgImage.sprite = tableCloth[tableBgIndex];
            }
        }
    }

    /// <summary>
    /// 更新桌布选择的UI状态
    /// </summary>
    private void UpdateTableBgSelectionUI(int selectedIndex)
    {
        Transform tableBgContainer = varSetBg?.transform.Find("SettingPanel/GamePanel/tableBg");
        // 遍历所有桌布选项，更新选中状态
        for (int i = 0; i < tableBgContainer.childCount; i++)
        {
            Transform child = tableBgContainer.GetChild(i);
            Transform bgChild = child.Find("bg");
            bgChild.gameObject.SetActive(i == selectedIndex);
        }

    }

    /// <summary>
    /// 初始化GPS按钮显示状态
    /// </summary>
    private void InitializeGPSButton()
    {
        var BtnGPS = this.transform.Find("BtnGPS");
        if (BtnGPS != null)
        {
            // 根据后端传入的 gpsLimit 字段判断是否显示GPS按钮
            bool hasGPSPermission = false;

            if (PdkProcedure?.EnterRunFastDeskRs?.BaseConfig != null)
            {
                // 使用后端协议中的 gpsLimit 字段
                hasGPSPermission = PdkProcedure.EnterRunFastDeskRs.BaseConfig.GpsLimit;
            }

            BtnGPS.gameObject.SetActive(hasGPSPermission);
            GF.LogInfo($"[PDK GPS] GPS按钮显示状态: {hasGPSPermission}");
        }
    }

    /// <summary>
    /// 初始化声音按钮显示状态（根据本地设置显示男声/女声的On/Off状态）
    /// </summary>
    private void InitializeVoiceButtons()
    {
        if (varSetBg == null) return;

        // 从PlayerPrefs读取语音性别设置，默认为男声
        string gender = PlayerPrefs.GetString("MahjongVoiceGender", "male");
        bool isFemale = (gender == "female");

        // 获取按钮对象
        Transform womenVoiceOnTrans = varSetBg.transform.Find("SettingPanel/CommonPanel/Women/On");
        Transform womenVoiceOffTrans = varSetBg.transform.Find("SettingPanel/CommonPanel/Women/Off");
        Transform menVoiceOnTrans = varSetBg.transform.Find("SettingPanel/CommonPanel/Men/On");
        Transform menVoiceOffTrans = varSetBg.transform.Find("SettingPanel/CommonPanel/Men/Off");

        if (womenVoiceOnTrans != null && womenVoiceOffTrans != null &&
            menVoiceOnTrans != null && menVoiceOffTrans != null)
        {
            // 根据设置显示/隐藏对应的On/Off状态
            womenVoiceOnTrans.gameObject.SetActive(isFemale);
            womenVoiceOffTrans.gameObject.SetActive(!isFemale);
            menVoiceOnTrans.gameObject.SetActive(!isFemale);
            menVoiceOffTrans.gameObject.SetActive(isFemale);

            GF.LogInfo($"[PDK语音] 初始化语音按钮: {(isFemale ? "女声" : "男声")}");
        }
        else
        {
            GF.LogWarning("[PDK语音] 无法找到语音按钮对象");
        }
    }
    /// <summary>
    /// 初始化桌布选项按钮（参考 PersonalitySet 实现）
    /// </summary>
    private void InitializeBackgroundButtons()
    {
        Transform tableBgContainer = varSetBg?.transform.Find("SettingPanel/GamePanel/tableBg");
        int childCount = tableBgContainer.childCount;
        for (int i = 0; i < childCount; i++)
        {
            var btn = tableBgContainer.GetChild(i).GetComponent<Button>();
            // 创建局部变量避免闭包问题
            int buttonIndex = i;
            btn.onClick.RemoveAllListeners(); // 清除旧的监听器
            btn.onClick.AddListener(() => OnTableBgButtonClick(buttonIndex));
        }

        // 初始化完成，不再打印日志以避免过多输出
    }

    /// <summary>
    /// 桌布按钮点击处理
    /// </summary>
    /// <param name="selectedIndex">选中的桌布索引</param>
    private void OnTableBgButtonClick(int selectedIndex)
    {
        UpdateTableBg(selectedIndex);
    }

    /// <summary>下一局按钮</summary>
    private void HandleNextGameButton()
    {
        // 【修复】点击下一局时不清空准备状态标识，只清理结算UI
        ClearSettlementUI();
        ResetGameStateData();
        ShowPlayButtons(false);
        varNextGamePlay?.SetActive(false);
        HideAllSettlementRankings();

        if (!isReady) SendReadyRequest();
    }

    /// <summary>准备按钮</summary>
    private void HandleReadyButton()
    {
        if (!isReady)
        {
            ToggleReadyButtons(true);
            SendReadyRequest();
        }
    }
    /// <summary>取消准备按钮</summary>
    private void HandleCancelReadyButton()
    {
        if (isReady)
        {
            PdkProcedure?.Send_MJPlayerReadyRq(false);
            ToggleReadyButtons(false);
            isReady = false;

            // 取消准备后重新开始倒计时
            if (ZBdaojishi != null)
            {
                ZBdaojishi.SetActive(true);
                PdkProcedure?.StartCountdown(ZBdaojishi.GetComponent<Text>(), 15).Forget();
            }
        }
    }

    /// <summary>切换准备按钮显示状态</summary>
    private void ToggleReadyButtons(bool ready)
    {
        varBtnReady?.SetActive(!ready);
        varBtnQuitReady?.SetActive(ready);
        if (ready)
        {
            if (ZBdaojishi != null) ZBdaojishi.SetActive(false);
            PdkProcedure?.CancelCountdown();
        }
    }

    /// <summary>清理结算状态</summary>
    private void ClearSettlementState()
    {
        ClearAllGameUI();
        ResetGameStateData();
        ShowPlayButtons(false);
        varNextGamePlay?.SetActive(false);
        HideAllSettlementRankings();
    }

    /// <summary>
    /// 清理结算UI，但保留准备状态标识
    /// 用于"下一局"按钮，避免清空其他玩家的准备状态
    /// </summary>
    private void ClearSettlementUI()
    {
        ClearAllPlayAreas();
        ClearAllSettlementAreas();
        ClearAllHandCards();

        HideAllBankerIndicators();
        // 【关键】不调用 HideAllReadyIndicators()，保留准备状态
        HideAllPassIndicators();
        HideAllPlayerClocks();
        HideAllWarningEffects();
        HideAllDoubleIndicators();
        HideAllPlayersCardBack();

        PdkProcedure?.ShowRobBankerUI(false);

        DoubleBtn?.SetActive(false);
        varChuPaiBtns_15?.SetActive(false);
        // 【关键】不隐藏准备按钮，保留准备状态
        // varBtnReady?.SetActive(false);
        // varBtnQuitReady?.SetActive(false);
        varMenu?.SetActive(false);
        varNextGame?.SetActive(false);
        zhaNiao?.SetActive(false);

        HidePlayCardTipImage();
        if (currentTipCountdown != null)
        {
            StopCoroutine(currentTipCountdown);
            currentTipCountdown = null;
        }

        ClearAllChatEmojis();
        ClearSelectedCards();
        myHandCards.Clear();
    }

    /// <summary>发送准备请求</summary>
    private void SendReadyRequest()
    {
        if (PdkProcedure != null)
        {
            PdkProcedure.Send_MJPlayerReadyRq(true);
            isReady = true;
            OnReadyStateChanged(1);
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {

        CleanupCardEvents();
        ClearSelectedCards();
        ClearAllHandCards();

        // 清理对象池
        if (cardPool != null)
        {
            cardPool.Clear();
        }

        // 清理字典
        playerIdToHeadBg?.Clear();
        playerIdToPlayArea?.Clear();
        playerIdToLastCards?.Clear();

        base.OnClose(isShutdown, userData);
    }
    #region 按钮绑定
    /// <summary>
    /// 绑定UI界面按钮事件
    /// </summary>
    private void BindUIEvents()
    {
        // 1. 出牌按钮
        BindPlayCardsButton();
        // 2. 不出按钮
        BindPassButton();
        // 3. 提示按钮
        BindHintButton();
        // 4. 头像点击事件
        BindHeadBgClickEvents();


    }

    /// <summary>
    /// 绑定出牌按钮
    /// </summary>
    private void BindPlayCardsButton()
    {

        var button = varChuPaiBtn.GetComponent<Button>();
        var button1 = varChuPaiBtn_NO.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button1.onClick.RemoveAllListeners();

        button.onClick.AddListener(() =>
        {
            OnPlayCardsButtonClick();
        });
        button1.onClick.AddListener(() =>
        {
            ClearSelectedCards();
        });
    }

    /// <summary>
    /// 出牌按钮点击事件
    /// </summary>
    /// <summary>
    /// 出牌按钮点击事件
    /// 重要：此方法只发送请求到服务器，不修改本地手牌数据
    /// 等待服务器Syn_RunFastDiscardCard确认后才在HandlePlayerPlayCards中移除手牌
    /// 这样可以避免服务器拒绝出牌（如牌型不合法）时本地数据错乱
    /// </summary>
    private void OnPlayCardsButtonClick()
    {
        List<int> cardIds = selectedCardNumbers.ToList();
        if (cardIds.Count == 0) return;
        //如果提示了就不发送出牌请求
        int tipIndex = CheckPlayCardsRules(cardIds);
        if (tipIndex == -2)
        {
            // 【修复】牌型不对时，只清空选择，不显示提示
            ClearSelectedCards();
        }
        else if (tipIndex != -1)
        {
            // 其他提示（1-4）显示提示动画
            ShowNoCardsToFollowTip(tipIndex);
            ClearSelectedCards();
        }
        else
        {
            // 获取当前玩家索引
            long myPlayerId = Util.GetMyselfInfo().PlayerId;
            string myPlayerName = GetPlayerNameById(myPlayerId);
            int myPlayerIndex = GetPlayerIndexByServerId(myPlayerId);
            // 【关键】只发送出牌请求，不修改本地数据
            // 等服务器返回Syn_RunFastDiscardCard后再在HandlePlayerPlayCards中移除手牌
            gameLogic?.ExecutePlayCards(myPlayerIndex, cardIds);
        }
    }

    /// <summary>
    /// 检查出牌规则并返回提示索引
    /// </summary>
    /// <param name="cardIds">要出的牌</param>
    /// <returns>提示索引(0-4)，-1表示无需提示</returns>
    private int CheckPlayCardsRules(List<int> cardIds)
    {
        if (cardIds == null || cardIds.Count == 0) return -1;

        // 转换为PDKCardData
        List<PDKCardData> selectedCards = ConvertCardIdsToCardDataList(cardIds);
        PDKConstants.CardType cardType = PDKRuleChecker.DetectCardType(selectedCards, myHandCards);

        // 判断是否是整局游戏的第一次出牌
        // 使用服务器状态字段判断，而不是统计手牌数量（更可靠）
        // 逻辑：
        // 1. serverLastMaxDiscard == 0 && serverMaxDisCardPlayer == 0：整局从未有人出过牌（发牌后第一次）
        // 2. serverLastMaxDiscard == 0 && serverMaxDisCardPlayer > 0：本轮有人出牌了，但之前轮次没有记录（非首轮）
        // 3. serverLastMaxDiscard > 0：明确有历史出牌记录（非首轮）
        bool isFirstHandOfGame = (serverLastMaxDiscard == 0 && serverMaxDisCardPlayer == 0);
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        bool isRoundLeader = (serverMaxDisCardPlayer == 0 || serverMaxDisCardPlayer == myPlayerId);

        // 【调试】输出当前状态和规则配置
        GF.LogInfo_wl($"<color=#00FFFF>[出牌检查] serverLastMaxDiscard={serverLastMaxDiscard}, serverMaxDisCardPlayer={serverMaxDisCardPlayer}, isFirstHandOfGame={isFirstHandOfGame}, isRoundLeader={isRoundLeader}, mustPlayMin={currentGameRules.mustPlayMin}</color>");

        // 【关键理解】两种规则的区别:
        // 1. 先出规则(firstRoundBlackThree/firstRoundMinCard): 后端用来判断庄家(谁先出),客户端不检查
        // 2. 出牌规则(mustPlayMin/freePlay): 决定出第一手牌时是否必须带最小牌

        // 检查: 首出必须带最小牌（只在整局第一次出牌且配置了"必带最小"时检查）
        if (isFirstHandOfGame && currentGameRules.mustPlayMin)
        {
            var smallestValue = myHandCards.Min(c => c.value);

            // 检查所有选中的牌中是否包含最小牌（只比较点数，不比较花色）
            bool hasSmallest = selectedCards.Any(c => c.value == smallestValue);

            GF.LogInfo_wl($"<color=#FFFF00>[首出检查] 牌型={cardType}, 最小点数={smallestValue}, 包含最小={hasSmallest}</color>");

            if (!hasSmallest)
            {
                // 【修复】根据先出规则返回对应的提示索引
                // 如果配置了"首局黑桃3先出"，返回索引3；否则返回索引4
                if (currentGameRules.firstRoundBlackThree)
                {
                    return 3; // 提示索引3: 首出请带黑桃3
                }
                else
                {
                    return 4; // 提示索引4: 首出请带最小
                }
            }
        }
        else if (isFirstHandOfGame && currentGameRules.freePlay)
        {
        }

        // 检查1: 牌型检查（只在非本轮首出且有上家牌时检查）
        if (!isRoundLeader && lastPlayedCards != null)
        {
            bool canPlay = PDKRuleChecker.CanPlayCards(selectedCards, myHandCards, lastPlayedCards, currentGameRules);
            if (!canPlay)
            {
                // 【修复】无论是牌型不对还是牌不够大，都只清空选择不显示提示
                // 因为提示是在玩家出牌前检查的，这里出牌时不需要提示
                return -2; // 特殊值-2: 无法出牌，清空选择但不显示提示
            }
        }

        // 检查3: 炸弹不可拆（无论是否本轮首出都要检查）
        if (currentGameRules.bombCannotSplit)
        {
            bool isSplittingBomb = CheckIfSplittingBomb(cardIds);
            if (isSplittingBomb)
            {
                return 2; // 提示索引2: 炸弹不可拆
            }
        }

        // 检查2: 下家报单，请出最大牌（2人和3人游戏都需要检查）
        // 2人和3人游戏都需要下家报单提示，但只在出单牌时检查
        int playerCount = players?.Count ?? 0;
        bool isNextPlayerHasOneCard = CheckIfNextPlayerHasOneCard();
        if (isNextPlayerHasOneCard && cardType == PDKConstants.CardType.SINGLE)
        {
            bool isPlayingLargest = IsPlayingLargestSingleCard(selectedCards);
            if (!isPlayingLargest)
            {
                return 1; // 提示索引1: 下家报单，请出最大牌
            }
        }

        return -1; // 无需提示
    }

    /// <summary>
    /// 检查是否拆开了炸弹（包括AAA规则）
    /// </summary>
    private bool CheckIfSplittingBomb(List<int> selectedCardIds)
    {
        // 统计每个点数的数量
        var valueGroups = myHandCards.GroupBy(c => c.value).ToDictionary(g => g.Key, g => g.Count());
        var selectedValueGroups = ConvertCardIdsToCardDataList(selectedCardIds)
            .GroupBy(c => c.value).ToDictionary(g => g.Key, g => g.Count());

        // 检查是否有炸弹被拆
        foreach (var kvp in valueGroups)
        {
            if (kvp.Value == 4) // 手上有4张同点数的牌（炸弹）
            {
                if (selectedValueGroups.ContainsKey(kvp.Key) &&
                    selectedValueGroups[kvp.Key] < 4) // 但只出了其中几张
                {
                    return true; // 拆开了炸弹
                }
            }

            // 【关键修复】检查AAA炸弹规则
            if (currentGameRules.aaaIsBomb && kvp.Key == PDKConstants.CardValue.Ace && kvp.Value >= 3)
            {
                // 手上有3张或4张A（AAA为炸弹）
                if (selectedValueGroups.ContainsKey(kvp.Key))
                {
                    int selectedAceCount = selectedValueGroups[kvp.Key];
                    // 如果选中了1张或2张A（没有选中全部3张作为炸弹），视为拆炸弹
                    if (selectedAceCount > 0 && selectedAceCount < 3)
                    {
                        GF.LogWarning($"[PDK炸弹] AAA规则开启，检测到拆AAA炸弹：手上{kvp.Value}张A，只出{selectedAceCount}张");
                        return true; // 拆开了AAA炸弹
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 检查下家是否只有一张牌
    /// </summary>
    private bool CheckIfNextPlayerHasOneCard()
    {
        if (players == null || players.Count <= 1) return false;

        long myPlayerId = Util.GetMyselfInfo().PlayerId;


        // 【关键】找到客户端1号位的玩家（下家）
        // 2人游戏：1号位是对手
        // 3人游戏：1号位是顺时针下一个玩家
        long nextPlayerServerId = TryGetNextPlayerIdBySeat();
        if (nextPlayerServerId <= 0)
        {
            // 兜底：从旧的 playArea 映射查找
            foreach (var kvp in playerIdToPlayArea)
            {
                long playerId = kvp.Key;
                GameObject playArea = kvp.Value;
                if (playerId == myPlayerId) continue;
                if (playArea == varPlayArea1)
                {
                    nextPlayerServerId = playerId;
                    break;
                }
            }
        }

        if (nextPlayerServerId == -1)
        {
            GF.LogWarning($"[PDK报警] 找不到下家玩家ID");
            return false;
        }

        // 获取下家的牌数
        int cardCount = GetPlayerCardCount(nextPlayerServerId);
        bool hasOneCard = (cardCount == 1);

        return hasOneCard;
    }

    /// <summary>
    /// 更稳定的“下家ID”获取：优先用 headBg(座位) 计算；必要时用桌子Pos计算。
    /// </summary>
    private long TryGetNextPlayerIdBySeat()
    {
        long myPlayerId = Util.GetMyselfInfo().PlayerId;

        // 1) 优先使用 playerIdToHeadBg（SetDeskPlayers 已维护）
        foreach (var kvp in playerIdToHeadBg)
        {
            if (kvp.Key == myPlayerId) continue;
            if (kvp.Value == varHeadBg1)
            {
                return kvp.Key;
            }
        }

        // 2) 兜底：从桌子数据按Pos算相对位置
        var enter = PdkProcedure?.EnterRunFastDeskRs;
        var deskPlayers = enter?.DeskPlayers;
        if (deskPlayers == null || deskPlayers.Count <= 1)
        {
            return -1;
        }

        int totalPlayerNum = enter.BaseConfig?.PlayerNum ?? deskPlayers.Count;
        var myPlayer = deskPlayers.FirstOrDefault(p => p.BasePlayer.PlayerId == myPlayerId);
        if (myPlayer == null) return -1;

        int myClientPos = 0; // 自己永远在0号位
        Position myServerPos = myPlayer.Pos;
        foreach (var player in deskPlayers)
        {
            if (player.BasePlayer.PlayerId == myPlayerId) continue;
            int clientPos = Util.GetPosByServerPos_pdk(myServerPos, player.Pos, totalPlayerNum);
            var headBg = GetHeadBgByPos(clientPos, myClientPos, totalPlayerNum);
            if (headBg == varHeadBg1)
            {
                return player.BasePlayer.PlayerId;
            }
        }

        return -1;
    }

    /// <summary>
    /// 检查是否出了最大的单牌
    /// </summary>
    private bool IsPlayingLargestSingleCard(List<PDKCardData> selectedCards)
    {
        if (selectedCards == null || selectedCards.Count != 1) return false;
        if (myHandCards == null || myHandCards.Count == 0) return false;

        // 牌已经按点数排序，最大的牌点数可以通过 myHandCards.Max(c => c.value) 获取
        // 为了性能，也可以直接取排序后的第一个（如果排序是降序的话）
        var maxValue = myHandCards.Max(c => (int)c.value);
        var playedCard = selectedCards[0];

        // 检查出的单牌是否是最大的牌点数（不再检查花色）
        bool isLargest = ((int)playedCard.value == maxValue);

        return isLargest;
    }



    /// <summary>
    /// 提示倒计时协程（4秒倒计时，自动点击不出）
    /// </summary>
    private Coroutine currentTipCountdown = null;

    private IEnumerator PlayCardTipCountdownCoroutine(GameObject tipObject)
    {

        // 1. 所有手牌变灰
        DisableAllHandCards();

        // 2. 显示时钟并从4开始倒计时
        Transform clockTransform = varChuPaiBtns_15?.transform.Find("clock");
        if (clockTransform != null)
        {
            clockTransform.gameObject.SetActive(true);
            var skeletonAnim = clockTransform.GetComponent<Spine.Unity.SkeletonAnimation>();
            if (skeletonAnim != null && skeletonAnim.AnimationState != null)
            {
                // 设置动画从4秒位置开始播放（假设动画总长度是10秒，对应0-9的倒计时）
                // 倒计时4秒对应动画时间6秒位置（10-4=6）
                var trackEntry = skeletonAnim.AnimationState.SetAnimation(0, "animation", false);
                if (trackEntry != null)
                {
                    // 获取动画总时长
                    float animDuration = trackEntry.Animation.Duration;
                    // 从4秒位置开始：如果动画是倒计时，则从 (总时长 - 4) 的位置开始
                    trackEntry.TrackTime = 6;
                    trackEntry.TimeScale = 1f; // 正常速度播放

                }
            }
        }

        // 3. 等待4秒
        float countdown = 4f;
        while (countdown > 0)
        {
            yield return new WaitForSeconds(0.1f);
            countdown -= 0.1f;

            // 检查是否被手动中断（玩家点击了不出按钮）
            if (tipObject == null || !tipObject.activeSelf)
            {
                yield break;
            }
        }

        // 4. 倒计时结束，自动点击不出（这会触发CancelTipCountdown，统一处理隐藏逻辑）
        AutoClickPassButton();
    }

    /// <summary>
    /// 所有手牌变灰（禁用交互）
    /// </summary>
    private void DisableAllHandCards()
    {
        var allHandCards = GetAllHandCardComponents();
        foreach (var card in allHandCards)
        {
            if (card != null && card.image != null)
            {
                card.image.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                // 禁用点击事件
                SetCardInteractable(card.gameObject, false);
            }
        }
    }

    /// <summary>
    /// 自动点击不出按钮（倒计时结束时调用）
    /// </summary>
    private void AutoClickPassButton()
    {
        // 【Bug修复4】添加安全检查，防止偶现的自动过牌失败

        try
        {
            // 【关键】检查是否还轮到自己出牌
            if (varChuPaiBtns_15 == null || !varChuPaiBtns_15.activeSelf)
            {
                GF.LogWarning("[PDK自动不出] 出牌按钮已隐藏，取消自动过牌");
                return;
            }

            // 【移除】不再立即播放音效，等待服务器确认后在HandlePlayCards中播放
            // 【移除】不再立即显示"不出"标识，等待服务器确认后在HandlePlayCards中显示

            // 获取当前玩家索引
            long myPlayerId = Util.GetMyselfInfo().PlayerId;
            int myPlayerIndex = GetPlayerIndexByServerId(myPlayerId);


            // 清空自己的出牌区域
            ClearPlayArea(varPlayArea);

            // 发送过牌请求，等待服务器确认后才显示UI和播放音效
            gameLogic?.ExecutePlayCards(myPlayerIndex, new List<int>());

            // 注意：提示图片和时钟会在后端返回下一位操作者时隐藏
        }
        catch (System.Exception ex)
        {
            GF.LogError($"[PDK自动不出] 执行自动过牌时出错: {ex.Message}\n{ex.StackTrace}");
            // 出错时也要清理状态
            HidePlayCardTipImage();
            RestoreAllHandCardsInteraction();
        }
    }

    /// <summary>
    /// 取消当前提示倒计时（玩家手动点击不出时调用）
    /// </summary>
    private void CancelTipCountdown()
    {
        if (currentTipCountdown != null)
        {
            StopCoroutine(currentTipCountdown);
            currentTipCountdown = null;

            // 隐藏时钟
            Transform clockTransform = varChuPaiBtns_15?.transform.Find("clock");
            clockTransform.gameObject.SetActive(false);

            // 恢复手牌亮度
            RestoreAllHandCardsInteraction();


            // 注意：提示图片会在后端返回下一位操作者时隐藏
        }
    }

    /// <summary>
    /// 隐藏提示图片（在后端返回下一位操作者时调用）
    /// </summary>
    private void HidePlayCardTipImage()
    {
        if (varChuPaiBtns_15 != null)
        {
            Transform imgTransform = varChuPaiBtns_15.transform.Find("img");
            if (imgTransform != null && imgTransform.gameObject.activeSelf)
            {
                // 【关键】只隐藏倒计时类型的提示（tipIndex=0），其他提示由动画自动完成
                // 检查是否有倒计时协程在运行
                if (currentTipCountdown != null)
                {
                    imgTransform.gameObject.SetActive(false);
                }
                else
                {
                    // 弹性提示类型，让动画自然完成，不要强制隐藏
                }
            }
            // 【关键】停止倒计时协程（防止自动过牌）
            if (currentTipCountdown != null)
            {
                StopCoroutine(currentTipCountdown);
                currentTipCountdown = null;
            }
            // 【关键】恢复所有手牌的正常状态（取消变灰）
            RestoreAllHandCardsInteraction();
        }
    }

    /// <summary>
    /// 简单的延迟隐藏协程（用于ShowPlayCardTipWithCountdown）
    /// </summary>
    private IEnumerator SimpleTipHideCoroutine(GameObject tipObject, float delay)
    {
        yield return new WaitForSeconds(delay);
        tipObject.SetActive(false);
    }


    /// <summary>
    /// 设置下一个操作的玩家（清空其出牌区域并显示倒计时）
    /// </summary>
    /// <param name="nextPlayerId">下一个操作玩家的服务器ID</param>
    /// <summary>
    /// 检查指定玩家是否还有剩余手牌
    /// </summary>
    /// <param name="playerId">玩家服务器ID</param>
    /// <returns>true表示还有手牌,false表示已出完所有牌</returns>
    public bool HasPlayerRemainingCards(long playerId)
    {
        long myPlayerId = Util.GetMyselfInfo().PlayerId;

        // 如果是自己,检查myHandCards列表
        if (playerId == myPlayerId)
        {
            bool hasCards = myHandCards != null && myHandCards.Count > 0;
            return hasCards;
        }

        // 对于其他玩家,我们无法直接获取手牌数,默认返回true让服务器逻辑控制
        // 服务器会通过OptionPlayer=0或直接发送结算消息来表示游戏结束
        return true;
    }

    /// <summary>
    /// 设置下一个操作的玩家
    /// </summary>
    /// <param name="nextPlayerId">下一个操作玩家的服务器ID</param>
    /// <param name="shouldClearPlayArea">是否应该清空该玩家的出牌区域（由后端逻辑判断）</param>
    public void SetNextPlayer(long nextPlayerId, bool shouldClearPlayArea = true)
    {
        long myPlayerId = Util.GetMyselfInfo().PlayerId;

        // 【关键】后端返回下一位操作者，隐藏提示图片
        HidePlayCardTipImage();

        // 【修复】先隐藏所有玩家的倒计时动画（包含自己），清理上一轮状态
        HideAllPlayerClocks();

        if (nextPlayerId == myPlayerId)
        {
            ShowPlayerClock(myPlayerId, true);
            // 【修复】轮到自己时，隐藏自己的"不要"标识
            ShowPassIndicator(myPlayerId, false);
            return;
        }

        // 轮到其他玩家时显示倒计时
        ShowPlayerClock(nextPlayerId, true);
        // 【修复】轮到该玩家时，隐藏其"不要"标识
        ShowPassIndicator(nextPlayerId, false);
        
        // 【关键修复】根据后端逻辑判断是否清空出牌区域
        // shouldClearPlayArea=true: 新一轮开始，清空出牌区域
        // shouldClearPlayArea=false: 轮回情况（玩家连续获得出牌权），保留之前的牌直到实际出牌
        if (shouldClearPlayArea)
        {
            if (playerIdToPlayArea.TryGetValue(nextPlayerId, out GameObject playArea) && playArea != null)
            {
                ClearPlayArea(playArea);
            }
        }
    }

    /// <summary>
    /// 显示或隐藏出牌按钮
    /// </summary>
    /// <param name="show">true显示，false隐藏</param>
    /// <param name="isReconnecting">是否是重连场景(重连时不显示首出提示)</param>
    public void ShowPlayButtons(bool show, bool isReconnecting = false)
    {

        if (show)
        {

            HidePlayCardTipImage();
            // 清空自己的出牌区域
            ClearPlayArea(varPlayArea);
            //有大必压 
            UpdatePassButtonVisibility();
            // 【修复】轮到自己出牌时，只隐藏自己的"不要"标识，不影响其他玩家
            long myPlayerId = Util.GetMyselfInfo().PlayerId;
            ShowPassIndicator(myPlayerId, false);
            // 出牌前检查已选牌的合法性
            CheckSelectedCardsValidity();
            HideAllPlayerClocks();
        }
        else
        {
            // 隐藏出牌按钮时，恢复所有手牌的正常交互
            RestoreAllHandCardsInteraction();
        }

        varChuPaiBtns_15.SetActive(show);

        // 【新增】检测并执行最后一手自动出牌 - 延迟执行确保UI已更新
        if (show)
        {
            StartCoroutine(CheckAndAutoPlayLastHandDelayed());
        }

        // 【修复】轮到自己出牌时，显示自己的倒计时（10 -> 0）
        if (show)
        {
            long myPlayerId = Util.GetMyselfInfo().PlayerId;
            ShowPlayerClock(myPlayerId, true, isReconnecting);
        }

        // 【修复】显示出牌按钮时，确保隐藏提示图片（避免开局时显示错误提示）
        if (show)
        {
            Transform imgTransform = varChuPaiBtns_15.transform.Find("img");
            if (imgTransform != null && imgTransform.gameObject.activeSelf)
            {
                imgTransform.gameObject.SetActive(false);
            }
        }

    }

    /// <summary>
    /// 更新"不出"按钮的显示状态（根据有大必压规则）
    /// </summary>
    private void UpdatePassButtonVisibility()
    {
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        bool isLeading = false;

        // 优先使用服务器协议字段判断
        if (serverMaxDisCardPlayer > 0)
        {
            // 打牌过程中的判断：当前玩家ID == MaxDisCardPlayer 则为首出
            isLeading = (myPlayerId == serverMaxDisCardPlayer);
        }
        else if (serverLastMaxDiscard > 0)
        {
            // 重连时的判断：当前玩家ID == LastMaxDiscard 则为首出
            isLeading = (myPlayerId == serverLastMaxDiscard);
        }
        else if (serverMaxDisCardPlayer == 0 && serverLastMaxDiscard == 0)
        {
            isLeading = true;
        }
        // 首出状态:必须出牌,隐藏不出按钮
        if (isLeading)
        {
            varBuChuBtn.SetActive(false);
            return;
        }

        // 非首出状态，根据玩法规则决定是否显示不出按钮
        // 玩法1：有大必压，需要检查是否有牌可以跟
        if (PDKConstants.wanfa == 1)
        {
            // 只有在非首出且lastPlayedCards不为null时才调用CanFollowLastPlay
            if (lastPlayedCards != null && lastPlayedCards.cards != null && lastPlayedCards.cards.Count > 0)
            {
                bool hasCardsToFollow = gameLogic.CanFollowLastPlay(myHandCards, lastPlayedCards);
                varBuChuBtn.SetActive(!hasCardsToFollow);
            }
            else
            {
                // lastPlayedCards为空,显示不出按钮(安全策略)
                varBuChuBtn.SetActive(true);
            }
        }
        else
        {
            // 其他玩法：总是显示不出按钮
            varBuChuBtn.SetActive(true);
        }
    }

    /// <summary>
    /// 轮到我操作时，检查可出牌并智能提示
    /// </summary>
    /// <param name="isReconnecting">是否是重连场景</param>
    public void CheckPlayableCardsOnMyTurn(bool isReconnecting = false)
    {
        long myPlayerId = Util.GetMyselfInfo().PlayerId;

        // 判断是否是首出
        bool isLeading = false;
        if (isReconnecting)
        {
            // 重连时使用 serverLastMaxDiscard 判断
            isLeading = (serverLastMaxDiscard == 0 || serverLastMaxDiscard == myPlayerId);
        }
        else
        {
            // 正常游戏流程使用 serverMaxDisCardPlayer 判断
            isLeading = (serverMaxDisCardPlayer == 0 || serverMaxDisCardPlayer == myPlayerId);
        }
        // 【优化3】如果是首出，不需要检查可出牌（首出的规则检查已经在 CheckAndShowPlayCardTipOnTurn 中完成）
        // 跑得快规则：首出必须带最小牌（通常是黑桃3）
        if (isLeading)
        {
            return;
        }
        // 非首出，检查是否有牌可以跟
        if (lastPlayedCards == null || lastPlayedCards.cards == null || lastPlayedCards.cards.Count == 0)
        {
            return;
        }

        // 判断是否有组合能压过当前牌
        bool canFollow = gameLogic.CanFollowLastPlay(myHandCards, lastPlayedCards);
        if (canFollow)
        {
            // 1. 有组合能压过
            // 【关键修复】确保清除之前的"没有牌可跟"提示（如果有的话）
            HidePlayCardTipImage();

            if (currentGameRules.bigMustPress)
            {
                // 【优化3】有大必压规则：隐藏不出按钮
                varBuChuBtn.SetActive(false);
            }
            // 【优化3】智能变灰不可用的牌，炸弹始终保持可用
            if (lastPlayedCards.cardType == PDKConstants.CardType.SINGLE)
            {
                // 单张：将小于等于目标牌值的单张变灰,但炸弹保持可用
                DisableCardsLessThanOrEqualTo(lastPlayedCards.cards[0], checkBombs: true);
            }
            else if (lastPlayedCards.cardType == PDKConstants.CardType.PAIR)
            {
                // 对子：需要更智能的处理,炸弹保持可用
                DisableCardsForPair(lastPlayedCards.cards[0], checkBombs: true);
            }
            else if (lastPlayedCards.cardType == PDKConstants.CardType.STRAIGHT ||
                     lastPlayedCards.cardType == PDKConstants.CardType.STRAIGHT_PAIR)
            {
                // 顺子/连对：比较最小的牌值，小于等于最小值的牌变灰,但炸弹保持可用
                PDKCardData minCard = lastPlayedCards.cards.OrderBy(c => (int)c.value).First();
                DisableCardsLessThanOrEqualTo(minCard, checkBombs: true);
            }
            else if (lastPlayedCards.cardType == PDKConstants.CardType.THREE_WITH_ONE ||
                     lastPlayedCards.cardType == PDKConstants.CardType.THREE_WITH_TWO ||
                     lastPlayedCards.cardType == PDKConstants.CardType.FOUR_WITH_TWO ||
                     lastPlayedCards.cardType == PDKConstants.CardType.FOUR_WITH_THREE ||
                     lastPlayedCards.cardType == PDKConstants.CardType.PLANE)
            {
                // 【优化3】复杂牌型（三带、飞机、四带）不做自动变灰，让玩家自由选择
            }
        }
        else
        {
            // 2. 没有组合能压过：显示提示图片，手牌变灰，4秒后自动过牌
            // 提示索引0: 没有大过玩家的牌
            ShowNoCardsToFollowTip(0);
        }
    }

    /// <summary>
    /// 禁用小于等于指定牌值的手牌（变灰）- 用于单张
    /// 规则：单张2最大(15)，A第二(14)，然后是K(13)到3(3)
    /// </summary>
    /// <param name="checkBombs">是否检测炸弹并保持炸弹可用(炸弹能压任何牌型)</param>
    private void DisableCardsLessThanOrEqualTo(PDKCardData targetCard, bool checkBombs = false)
    {
        // 【BUG2修复】禁用手牌时清空已选中的牌,避免无法取消选择
        if (selectedCards.Count > 0)
        {
            ClearSelectedCards();
        }

        var allHandCards = GetAllHandCardComponents();
        int disabledCount = 0;
        int targetValue = (int)targetCard.value;

        // 【关键修复】如果需要检测炸弹,先统计每个牌值的数量
        // card.value存储的是原始值（A=1, 2=2, 3-13直接映射）
        // 需要转换为跑得快规则值（A=14, 2=15, 3-13不变）
        Dictionary<int, int> valueCount = new Dictionary<int, int>();
        HashSet<PDKCard> bombCards = new HashSet<PDKCard>();

        if (checkBombs)
        {
            // 统计每个值的数量（使用逻辑值）
            foreach (var card in allHandCards)
            {
                if (card != null && card.value >= 1 && card.value <= 15)
                {
                    int logicValue = card.value;
                    if (card.value == 1) logicValue = 14; // A
                    else if (card.value == 2) logicValue = 15; // 2

                    if (!valueCount.ContainsKey(logicValue))
                    {
                        valueCount[logicValue] = 0;
                    }
                    valueCount[logicValue]++;
                }
            }

            // 找出所有炸弹(数量=4的牌值)
            foreach (var card in allHandCards)
            {
                if (card != null)
                {
                    int logicValue = card.value;
                    if (card.value == 1) logicValue = 14;
                    else if (card.value == 2) logicValue = 15;

                    if (valueCount.ContainsKey(logicValue) && valueCount[logicValue] == 4)
                    {
                        bombCards.Add(card);
                    }
                }
            }

            // 【关键修复】如果开启AAA为炸弹规则，标记3张A为炸弹
            if (currentGameRules.aaaIsBomb && valueCount.ContainsKey(14) && valueCount[14] >= 3)
            {
                // 将所有A标记为炸弹牌（保持可用）
                foreach (var card in allHandCards)
                {
                    if (card != null && card.value == 1) // A的原始值是1
                    {
                        bombCards.Add(card);
                    }
                }
                GF.LogInfo($"[PDK炸弹] AAA规则开启，标记{valueCount[14]}张A为炸弹牌，保持可用");
            }
        }

        // 变灰小于等于目标值的牌（炸弹除外）
        foreach (var card in allHandCards)
        {
            if (card != null && card.image != null)
            {
                // 如果是炸弹的一部分,保持可用(不变灰)
                if (checkBombs && bombCards.Contains(card))
                {
                    continue;
                }

                // 【关键修复】转换为逻辑值后判断大小
                // 2最大(15)，A第二(14)，K(13)到3(3)
                int logicValue = card.value;
                if (card.value == 1) logicValue = 14; // A
                else if (card.value == 2) logicValue = 15; // 2

                if (logicValue >= 3 && logicValue <= targetValue)
                {
                    card.image.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                    // 禁用点击事件
                    SetCardInteractable(card.gameObject, false);
                    disabledCount++;
                }
            }
        }

    }

    /// <summary>
    /// 对子模式：智能变灰不可用的牌
    /// </summary>
    /// <param name="checkBombs">是否检测炸弹并保持炸弹可用(炸弹能压任何牌型)</param>
    private void DisableCardsForPair(PDKCardData targetCard, bool checkBombs = false)
    {
        // 【BUG2修复】禁用手牌时清空已选中的牌,避免无法取消选择
        if (selectedCards.Count > 0)
        {
            ClearSelectedCards();
        }

        var allHandCards = GetAllHandCardComponents();
        int targetValue = (int)targetCard.value;
        Dictionary<int, List<PDKCard>> valueGroups = new Dictionary<int, List<PDKCard>>();
        foreach (var card in allHandCards)
        {
            if (card != null && card.value >= 1 && card.value <= 15)
            {
                // 转换为跑得快逻辑值
                int logicValue = card.value;
                if (card.value == 1) // A
                {
                    logicValue = 14;
                }
                else if (card.value == 2) // 2
                {
                    logicValue = 15;
                }
                // 3-13保持不变

                if (!valueGroups.ContainsKey(logicValue))
                {
                    valueGroups[logicValue] = new List<PDKCard>();
                }
                valueGroups[logicValue].Add(card);
            }
        }

        // 找出所有能压过目标对子的对子（数量>=2 且 值>目标值）
        HashSet<PDKCard> validCards = new HashSet<PDKCard>();


        foreach (var kvp in valueGroups)
        {
            int value = kvp.Key;
            List<PDKCard> cards = kvp.Value;


            // 【修复Bug】对子判断：数量>=2 且 值>目标值（包括对A，value=14）
            if (cards.Count >= 2 && value > targetValue)
            {
                // 这是一个可用的对子，保留所有同值的牌(例如有3个K就保留全部3个K)
                foreach (var card in cards)
                {
                    if (card != null)
                    {
                        validCards.Add(card);
                    }
                }
            }

            // 【优化】炸弹可以压任何牌型，包括对子
            if (checkBombs && cards.Count == 4)
            {
                foreach (var card in cards)
                {
                    if (card != null)
                    {
                        validCards.Add(card);
                    }
                }
            }

            // 【关键修复】如果开启AAA为炸弹规则，3张A也可以压对子
            if (checkBombs && currentGameRules.aaaIsBomb && value == 14 && cards.Count >= 3)
            {
                foreach (var card in cards)
                {
                    if (card != null)
                    {
                        validCards.Add(card);
                    }
                }
                GF.LogInfo($"[PDK炸弹] AAA规则开启，保持{cards.Count}张A可用（可压对子）");
            }
        }

        // 将所有不在 validCards 中的牌变灰并禁用交互
        int disabledCount = 0;
        foreach (var card in allHandCards)
        {
            if (card != null && card.image != null)
            {
                if (!validCards.Contains(card))
                {
                    card.image.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                    // 禁用点击事件
                    SetCardInteractable(card.gameObject, false);
                    disabledCount++;
                }
            }
        }

    }

    /// <summary>
    /// 显示提示图片并启动4秒倒计时自动过牌
    /// </summary>
    /// <param name="tipIndex">
    /// 提示索引: 
    /// 0-没有大过玩家的牌 
    /// 1-下家报单请出最大 
    /// 2-炸弹不可拆 
    /// 3-首出请带黑桃3 
    /// 4-首出请带最小牌
    /// </param>
    private void ShowNoCardsToFollowTip(int tipIndex = 0)
    {
        if (TisSprite == null || tipIndex < 0 || tipIndex >= TisSprite.Length)
        {
            GF.LogWarning($"[PDK提示] 提示精灵数组无效或索引越界: {tipIndex}");
            return;
        }

        // 查找提示图片对象
        if (varChuPaiBtns_15 != null)
        {
            Transform imgTransform = varChuPaiBtns_15.transform.Find("img");
            if (imgTransform != null)
            {
                Image tipImage = imgTransform.GetComponent<Image>();
                if (tipImage != null)
                {
                    string[] tipNames = { "没有大过玩家的牌", "下家报单请出最大", "炸弹不可拆", "首出请带黑桃3", "首出请带最小牌" };
                    string tipName = tipIndex < tipNames.Length ? tipNames[tipIndex] : $"索引{tipIndex}";

                    // 设置提示图片精灵
                    tipImage.sprite = TisSprite[tipIndex];
                    // 【关键】还原原图尺寸，避免变形
                    tipImage.SetNativeSize();
                    tipImage.gameObject.SetActive(true);
                    imgTransform.gameObject.SetActive(true);

                    if (tipName == "没有大过玩家的牌")
                    {
                        // 启动4秒倒计时，期间手牌变灰，倒计时结束自动过牌
                        if (currentTipCountdown != null)
                        {
                            StopCoroutine(currentTipCountdown);
                            currentTipCountdown = null;
                        }
                        currentTipCountdown = StartCoroutine(PlayCardTipCountdownCoroutine(tipImage.gameObject));
                    }
                    else
                    {
                        // 其他提示：弹性弹出后自动消失
                        ShowElasticTipAndHide(tipImage.gameObject, tipImage.sprite);
                    }
                }

            }
        }
    }

    /// <summary>
    /// 显示弹性提示动画并自动隐藏
    /// </summary>
    /// <param name="tipObject">提示对象</param>
    /// <param name="tipSprite">提示精灵（可选，用于在动画中设置）</param>
    private void ShowElasticTipAndHide(GameObject tipObject, Sprite tipSprite = null)
    {
        if (tipObject == null) return;

        RectTransform rectTransform = tipObject.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        // 杀掉之前的动画
        rectTransform.DOKill();

        // 【新增】确保图片正确设置
        if (tipSprite != null)
        {
            Image tipImage = tipObject.GetComponent<Image>();
            if (tipImage != null)
            {
                tipImage.sprite = tipSprite;
                tipImage.SetNativeSize();
            }
        }

        // 设置初始缩放为0
        rectTransform.localScale = Vector3.zero;
        tipObject.SetActive(true);

        // 创建弹性弹出动画序列
        Sequence tipSequence = DOTween.Sequence();

        // 1. 弹性放大到1.2倍（使用Back缓动函数产生回弹效果）
        tipSequence.Append(rectTransform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack));

        // 2. 稍微缩小到1倍
        tipSequence.Append(rectTransform.DOScale(1f, 0.1f).SetEase(Ease.InOutQuad));

        // 3. 停留1.5秒（延长停留时间，让玩家能看清）
        tipSequence.AppendInterval(1.5f);

        // 4. 淡出并缩小消失
        tipSequence.Append(rectTransform.DOScale(0f, 0.2f).SetEase(Ease.InBack));

        // 5. 动画结束后隐藏对象
        tipSequence.OnComplete(() =>
        {
            tipObject.SetActive(false);
        });

        // 【关键】设置动画不受游戏暂停影响，并标记为独立动画
        tipSequence.SetUpdate(true).SetAutoKill(true);

    }

    /// <summary>
    /// 处理出牌消息（自己或其他玩家出牌）
    /// </summary>
    /// <param name="playerId">出牌玩家的服务器ID</param>
    /// <param name="cardIds">出的牌的ID列表（0-51）</param>
    /// <param name="isMyself">是否是自己出牌</param>
    public void HandlePlayCards(long playerId, List<int> cardIds, bool isMyself, int passedCountBeforeClear = 0)
    {

        // 【新增】如果是自己过牌或出牌，恢复手牌状态并隐藏提示图片
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        if (playerId == myPlayerId)
        {
            // 恢复所有手牌的正常状态（取消变灰）
            RestoreAllHandCardsInteraction();

            // 隐藏提示图片
            HidePlayCardTipImage();

            // 取消倒计时
            if (currentTipCountdown != null)
            {
                StopCoroutine(currentTipCountdown);
                currentTipCountdown = null;
            }

        }

        // 判断是否为过牌
        bool isPass = (cardIds.Count == 0);
        if (isPass)
        {
            // 过牌：清空出牌区域，显示"不出"标识，播放音效，隐藏倒计时
            if (playerIdToPlayArea.TryGetValue(playerId, out GameObject playArea))
            {
                ClearPlayArea(playArea);
            }
            ShowPassIndicator(playerId, true);
            // 播放过牌音效（服务器确认后，根据玩家ID选择声音）
            string gender = GetVoiceGenderForPlayer(playerId);
            Sound.PlayEffect($"pdk/pt/{gender}/control/buyao.mp3");
            ShowPlayerClock(playerId, false);
        }
        else
        {
            // 出牌：隐藏"不出"标识，播放音效，显示卡牌
            ShowPassIndicator(playerId, false);
            playerIdToLastCards[playerId] = new List<int>(cardIds);

            // 【新增】播放出牌音效
            Sound.PlayEffect("pdk/chupai.mp3");

            // 播放牌型音效（传入玩家ID以支持不同玩家不同声音）
            PlayCardSound(cardIds, playerId);

            HandlePlayerPlayCards(playerId, cardIds, isMyself);
        }
    }
    /// <param name="playerId">出牌玩家的服务器ID</param>
    /// <param name="cardIds">出的牌的ID列表（0-51），如果为空表示过牌</param>
    /// <param name="cardType">牌型</param>
    /// <param name="maxDisCardPlayer">服务器传来的首出玩家ID（用于首出判断）</param>
    /// <summary>
    /// 更新游戏逻辑状态（处理出牌/过牌）
    /// </summary>
    public void UpdateGameLogicState(long playerId, List<int> cardIds, PDKConstants.CardType cardType, long maxDisCardPlayer = 0)
    {
        long myPlayerId = Util.GetMyselfInfo().PlayerId;

        // 更新首出玩家ID
        UpdateMaxDisCardPlayer(maxDisCardPlayer);
        // 获取玩家索引
        int playerIndex = GetPlayerIndexByServerId(playerId);

        if (playerIndex == -1)
        {
            GF.LogWarning($"[游戏逻辑] 找不到玩家ID {playerId} 的索引");
            return;
        }
        // 处理过牌
        if (cardIds == null || cardIds.Count == 0)
        {
            HandlePassLogic(playerId, playerIndex);
            return;
        }

        // 处理出牌（获取清空前的过牌数）
        lastPassedCountBeforeClear = HandlePlayLogic(playerId, playerIndex, cardIds, cardType);
    }

    #endregion

    #region 游戏逻辑处理

    /// <summary>
    /// 更新首出玩家ID
    /// </summary>
    private void UpdateMaxDisCardPlayer(long maxDisCardPlayer)
    {
        if (maxDisCardPlayer > 0 && GetPlayerIndexByServerId(maxDisCardPlayer) != -1)
        {
            // 检测是否进入新一轮(首出玩家改变)
            bool isNewRound = (serverMaxDisCardPlayer != maxDisCardPlayer);

            serverMaxDisCardPlayer = maxDisCardPlayer;

            // 新一轮开始,清空上一轮的出牌记录
            if (isNewRound)
            {
                lastPlayedCards = null;
            }
        }
    }

    /// <summary>
    /// 处理过牌逻辑
    /// </summary>
    private void HandlePassLogic(long playerId, int playerIndex)
    {
        if (!passedPlayers.Contains(playerIndex))
        {
            passedPlayers.Add(playerIndex);
        }
    }

    /// <summary>
    /// 处理出牌逻辑
    /// </summary>
    /// <returns>清空前的过牌玩家数量</returns>
    private int HandlePlayLogic(long playerId, int playerIndex, List<int> cardIds, PDKConstants.CardType cardType)
    {
        long myPlayerId = Util.GetMyselfInfo().PlayerId;

        // 有人出牌，清空过牌记录并重置UI
        int previousPassedCount = passedPlayers.Count;
        if (passedPlayers.Count > 0)
        {
            passedPlayers.Clear();
            // 【修复】不再统一隐藏所有玩家的"不要"标识，遵循"谁出牌/到谁谁清"原则
            // HideAllPassIndicators();
            HideAllPlayerClocks();
        }

        // 更新lastPlayedCards
        List<PDKCardData> cardDataList = ConvertCardIdsToCardDataList(cardIds);
        int weight = PDKRuleChecker.CalculateWeight(cardDataList, cardType);

        lastPlayedCards = new PDKPlayCardData
        {
            playerId = playerIndex,
            cards = new List<PDKCardData>(cardDataList),
            cardType = cardType,
            weight = weight
        };


        // 【已移除】牌数更新逻辑已移至 HandlePlayerPlayCards 方法中统一处理

        // 【新增】出牌后更新剩牌数量显示
        UpdateAllPlayersCardCount();

        return previousPassedCount;
    }

    /// <summary>
    /// 根据服务器玩家ID获取在游戏逻辑中的索引
    /// </summary>
    /// <param name="serverId">服务器玩家ID</param>
    /// <returns>玩家索引（0-2），找不到返回-1</returns>
    private int GetPlayerIndexByServerId(long serverId)
    {
        // 自己永远是索引0
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        if (serverId == myPlayerId) return 0;

        // 优先使用 headBg 映射（比 playArea 引用比较更稳定）
        if (playerIdToHeadBg.TryGetValue(serverId, out GameObject headBg))
        {
            if (headBg == varHeadBg1) return 1;
            if (headBg == varHeadBg2) return players.Count <= 2 ? 1 : 2;
        }

        // 兜底：旧逻辑
        if (playerIdToPlayArea.TryGetValue(serverId, out GameObject playArea))
        {
            if (playArea == varPlayArea1) return 1;
            if (playArea == varPlayArea2) return players.Count <= 2 ? 1 : 2;
        }

        return -1;
    }


    /// <summary>
    /// 处理玩家出牌（统一处理自己和其他玩家）
    /// <param name="playerId">出牌玩家ID</param>
    /// <param name="cardIds">出的牌ID列表</param>
    /// <param name="isMyself">是否是自己</param>
    private void HandlePlayerPlayCards(long playerId, List<int> cardIds, bool isMyself)
    {
        string playerName = GetPlayerNameById(playerId);

        // 隐藏该玩家的过牌标识
        ShowPassIndicator(playerId, false);
        // 获取出牌区域
        GameObject playArea = isMyself ? varPlayArea :
            (playerIdToPlayArea.TryGetValue(playerId, out GameObject area) ? area : null);
        // 如果是自己，需要额外处理手牌数据和UI
        if (isMyself)
        {
            // 【关键修复】先还原所有选中卡牌的UI状态（位置），然后清空选中列表
            // 这样可以避免托管自动出牌后，之前选中的牌UI状态未还原导致下次点击偏移的问题
            ClearSelectedCards();

            // 从手牌数组中移除已出的牌（服务器已确认，可以安全移除）
            RemoveCardsFromHand(cardIds);

            // 【关键修复】根据后端传回的卡牌ID移除手牌UI对象
            // 无论手动出牌还是托管自动出牌，均以后端数据为准
            RemoveHandCardUIByIds(cardIds);

            // 重新排列剩余手牌
            uiManager.ArrangeHandCards();

            // 重置提示状态
            hintState.ResetLeadCycle();
            hintState.ResetFollowCycle();
        }
        else
        {
            //如果不是自己则只需要更新玩家手牌数量
            if (playerIdToCardCount.TryGetValue(playerId, out int currentCount))
            {
                int newCount = Mathf.Max(0, currentCount - cardIds.Count);
                playerIdToCardCount[playerId] = newCount;
                GF.LogInfo_wl($"<color=#00FF00>[出牌更新] 玩家{playerId}: {currentCount} -> {newCount}张</color>");
            }
            else
            {
                // 【关键修复】字典是唯一数据源，如果没有数据说明同步有问题
                GF.LogError($"<color=#FF0000>[出牌处理] ❌ 字典中找不到玩家{playerId}的数据！可能重连同步失败</color>");
                // 不从players列表获取，因为那个数据不可靠
            }
        }

        // 清空出牌区域
        ClearPlayArea(playArea);

        // 显示卡牌到出牌区域
        DisplayCardsInPlayArea(playArea, cardIds);

        // 播放牌型特效
        if (cardIds != null && cardIds.Count > 0)
        {
            List<PDKCardData> cardDataList = ConvertCardIdsToCardDataList(cardIds);
            PDKConstants.CardType cardType = PDKRuleChecker.DetectCardType(cardDataList, myHandCards);
            PlayCardTypeEffect(playArea, cardType);
        }

        // 隐藏倒计时动画
        ShowPlayerClock(playerId, false);

        // 【新增】出牌后更新剩牌数量显示
        UpdateAllPlayersCardCount();

        // 【新增】出牌后检查并更新报警特效
        UpdatePlayerWarningEffect(playerId);
    }

    /// <summary>
    /// 从手牌数组中移除已出的牌
    /// </summary>
    private void RemoveCardsFromHand(List<int> cardIds)
    {
        List<PDKCardData> playedCardsData = ConvertCardIdsToCardDataList(cardIds);
        foreach (var playedCard in playedCardsData)
        {
            for (int i = myHandCards.Count - 1; i >= 0; i--)
            {
                if (myHandCards[i].CardId == playedCard.CardId)
                {
                    myHandCards.RemoveAt(i);
                    break;
                }
            }
        }
        if (myHandCards.Count > 0)
        {
        }

        // 【新增】出牌后更新扎鸟标识（如果打出了红桃10，需要隐藏标识）
        CheckAndShowZhaNiaoMark();

        // 【新增】出牌后更新剩牌数量显示
        UpdateAllPlayersCardCount();
    }

    /// <summary>
    /// 根据卡牌ID列表移除手牌UI对象
    /// 无论手动出牌还是托管自动出牌，均以后端数据为准进行表现同步
    /// </summary>
    /// <param name="cardIds">要移除的卡牌ID列表</param>
    private void RemoveHandCardUIByIds(List<int> cardIds)
    {
        if (cardIds == null || cardIds.Count == 0 || varHandCard == null) return;

        HashSet<int> idsToMatch = new HashSet<int>(cardIds);
        PDKCard[] allHandCards = varHandCard.GetComponentsInChildren<PDKCard>();
        int removedCount = 0;

        foreach (var card in allHandCards)
        {
            if (card != null && idsToMatch.Contains(card.numD))
            {
                // 如果有对象池优先使用对象池回收，否则销毁
                if (cardPool != null)
                {
                    cardPool.RecycleCard(card.gameObject);
                }
                else
                {
                    DestroyImmediate(card.gameObject);
                }

                idsToMatch.Remove(card.numD);
                removedCount++;
                if (idsToMatch.Count == 0) break;
            }
        }

        GF.LogInfo_wl($"[PDK手牌同步] 已根据后端数据同步移除 {removedCount} 张卡牌UI");
    }

    /// <summary>
    /// 绑定不出（过牌）按钮
    /// </summary>
    private void BindPassButton()
    {
        var button = varBuChuBtn.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            // 取消提示倒计时（如果正在进行）
            CancelTipCountdown();

            // 【移除】不再立即播放音效，等待服务器确认后在HandlePlayCards中播放
            // 【移除】不再立即显示"不出"标识，等待服务器确认后在HandlePlayCards中显示

            // 【迁移】获取当前玩家索引
            long myPlayerId = Util.GetMyselfInfo().PlayerId;
            int myPlayerIndex = GetPlayerIndexByServerId(myPlayerId);

            // 【关键】不再立即清空自己的出牌区域，等待服务器确认后在HandlePlayCards中统一处理
            // ClearPlayArea(varPlayArea);

            // 发送过牌请求（空的cardIds列表），等待服务器确认后才显示UI
            gameLogic?.ExecutePlayCards(myPlayerIndex, new List<int>());
        });
    }

    #endregion

    #region UI状态显示控制

    /// <summary>
    /// 显示或隐藏玩家的"不出"标识
    /// </summary>
    /// <param name="playerId">玩家服务器ID</param>
    /// <param name="show">true显示，false隐藏</param>
    private void ShowPassIndicator(long playerId, bool show)
    {
        if (playerIdToHeadBg.TryGetValue(playerId, out GameObject headBg))
        {
            Transform buyaoTransform = headBg.transform.Find("buyao");
            buyaoTransform.gameObject.SetActive(show);

        }
    }

    /// <summary>
    /// 隐藏所有玩家的"不出"标识
    /// </summary>
    public void HideAllPassIndicators()
    {
        foreach (var kvp in playerIdToHeadBg)
        {
            ShowPassIndicator(kvp.Key, false);
        }
    }

    /// <summary>
    /// 显示或隐藏玩家的倒计时动画（clock）
    /// </summary>
    /// <param name="playerId">玩家服务器ID</param>
    /// <param name="show">true显示，false隐藏</param>
    /// <param name="isReconnecting">是否是重连场景（重连时暂停时钟动画）</param>
    private void ShowPlayerClock(long playerId, bool show, bool isReconnecting = false)
    {
        // 1. 处理自己的操作按钮区域的时钟（如果是自己且时钟存在）
        if (playerId == Util.GetMyselfInfo().PlayerId)
        {
            Transform centerClock = varChuPaiBtns_15 != null ? varChuPaiBtns_15.transform.Find("clock") : null;
            if (centerClock != null)
            {
                centerClock.gameObject.SetActive(show);
                if (show)
                {
                    var skeletonAnim = centerClock.GetComponent<Spine.Unity.SkeletonAnimation>();
                    if (skeletonAnim != null && skeletonAnim.AnimationState != null)
                    {
                        var trackEntry = skeletonAnim.AnimationState.SetAnimation(0, "animation", false);
                        if (trackEntry != null)
                        {
                            if (isReconnecting)
                            {
                                float animDuration = trackEntry.Animation.Duration;
                                trackEntry.TrackTime = animDuration;
                                trackEntry.TimeScale = 0;
                            }
                            trackEntry.Event += (Spine.TrackEntry entry, Spine.Event e) =>
                            {
                                if (e != null && e.Data != null && e.Data.Name == "countdown_alert")
                                {
                                    Sound.PlayEffect("pdk/SEND_CARD0");
                                }
                            };
                        }
                    }
                }
            }
        }

        // 2. 处理玩家头像框上的时钟
        if (playerIdToHeadBg.TryGetValue(playerId, out GameObject headBg) && headBg != null)
        {
            Transform headClock = headBg.transform.Find("clock");
            if (headClock != null)
            {
                headClock.gameObject.SetActive(show);
                if (show)
                {
                    var skeletonAnim = headClock.GetComponent<Spine.Unity.SkeletonAnimation>();
                    if (skeletonAnim != null && skeletonAnim.AnimationState != null)
                    {
                        var trackEntry = skeletonAnim.AnimationState.SetAnimation(0, "animation", false);
                        if (trackEntry != null)
                        {
                            if (isReconnecting)
                            {
                                float animDuration = trackEntry.Animation.Duration;
                                trackEntry.TrackTime = animDuration;
                                trackEntry.TimeScale = 0;
                            }
                            trackEntry.Event += (Spine.TrackEntry entry, Spine.Event e) =>
                            {
                                if (e != null && e.Data != null && e.Data.Name == "countdown_alert")
                                {
                                    Sound.PlayEffect("pdk/SEND_CARD0");
                                }
                            };
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 隐藏所有玩家的倒计时动画
    /// </summary>
    public void HideAllPlayerClocks()
    {


        // 隐藏所有玩家的时钟
        foreach (var kvp in playerIdToHeadBg)
        {
            ShowPlayerClock(kvp.Key, false);
        }
    }

    #endregion

    #region 提示功能

    /// <summary>
    /// 绑定提示按钮
    /// </summary>
    private void BindHintButton()
    {
        var button = varTiSshiBtn.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            OnHintButtonClick();
        });
    }

    /// <summary>
    /// 绑定头像点击事件
    /// </summary>
    private void BindHeadBgClickEvents()
    {
        // 绑定varHeadBg0（自己）
        BindHeadBgClick(varHeadBg0);
        // 绑定varHeadBg1（右边玩家）
        BindHeadBgClick(varHeadBg1);
        // 绑定varHeadBg2（左边玩家）
        BindHeadBgClick(varHeadBg2);
    }

    /// <summary>
    /// 为单个头像绑定点击事件
    /// </summary>
    private void BindHeadBgClick(GameObject headBg)
    {
        var avatarButton = headBg.GetComponent<Button>();
        avatarButton.onClick.RemoveAllListeners();
        avatarButton.onClick.AddListener(() =>
        {
            OpenPlayerInfoPanel(headBg);
        });

    }

    /// <summary>
    /// 打开玩家信息面板
    /// </summary>
    private async void OpenPlayerInfoPanel(GameObject headBg)
    {
        // 根据headBg获取对应的玩家ID
        long playerId = GetPlayerIdByHeadBg(headBg);
        // 从playerIdToHeadBg字典中查找对应的DeskPlayer
        // 查找DeskPlayer
        DeskPlayer deskPlayer = null;
        foreach (var player in PdkProcedure.EnterRunFastDeskRs.DeskPlayers)
        {
            if (player.BasePlayer.PlayerId == playerId)
            {
                deskPlayer = player;
                break;
            }
        }
        // 播放按钮音效
        Sound.PlayEffect(AudioKeys.SOUND_BTN);

        // 准备参数
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("deskPlayer", deskPlayer.ToByteArray());

        // 判断是否是房主
        bool isRoomCreate = false;
        uiParams.Set<VarBoolean>("isRoomCreate", isRoomCreate);

        // 打开玩家信息面板
        await GF.UI.OpenUIFormAwait(UIViews.playerInforPanel, uiParams);
    }

    /// <summary>
    /// 根据headBg获取对应的玩家ID
    /// </summary>
    private long GetPlayerIdByHeadBg(GameObject headBg)
    {
        foreach (var kvp in playerIdToHeadBg)
        {
            if (kvp.Value == headBg)
            {
                return kvp.Key;
            }
        }
        return 0;
    }

    /// <summary>
    /// 延迟检测并执行最后一手自动出牌（确保UI已完全更新）
    /// </summary>
    private System.Collections.IEnumerator CheckAndAutoPlayLastHandDelayed()
    {
        // 等待1帧，确保UI状态已更新
        yield return null;

        GF.LogInfo($"[PDK自动出牌] 延迟检测开始 - 按钮激活状态: {varChuPaiBtns_15?.activeSelf ?? false}，手牌数：{myHandCards?.Count ?? 0}");

        // 【关键保护】先检查手牌是否为空，避免已出完牌后还触发检测
        if (myHandCards == null || myHandCards.Count == 0)
        {
            GF.LogInfo("[PDK自动出牌] 手牌已空，跳过自动出牌检测");
            yield break;
        }

        // 再次确认按钮状态
        if (varChuPaiBtns_15 == null || !varChuPaiBtns_15.activeSelf)
        {
            GF.LogWarning("[PDK自动出牌] 出牌按钮未激活，跳过自动出牌检测");
            yield break;
        }

        CheckAndAutoPlayLastHand();
    }

    /// <summary>
    /// 检测并执行最后一手自动出牌
    /// 规则：
    /// 1. 当玩家手牌只有1手牌（可以一手出完）时自动出牌
    /// 2. 当玩家手上有炸弹时不拆炸弹，除非手上只剩炸弹
    /// 3. 当规则开了AAA为炸弹时，不拆AAA，除非只剩AAA
    /// </summary>
    private void CheckAndAutoPlayLastHand()
    {
        GF.LogInfo($"[PDK自动出牌] 检测最后一手自动出牌条件，当前手牌数：{myHandCards?.Count ?? 0}");
        // 安全检查
        if (myHandCards == null || myHandCards.Count == 0)
        {
            GF.LogInfo("[PDK自动出牌] 手牌为空，跳过检测");
            return;
        }

        // 检测是否只剩一手牌
        var validCombination = DetectLastHandCombination();
        if (validCombination == null || validCombination.Count == 0)
        {
            GF.LogInfo("[PDK自动出牌] 不满足自动出牌条件（不是最后一手或有炸弹需保护）");
            return;
        }

        GF.LogInfo($"[PDK自动出牌] 检测到可以自动出牌，牌数：{validCombination.Count}");

        // 自动选中这些牌
        ClearSelectedCards();
        SelectHintCards(validCombination);

        GF.LogInfo($"[PDK自动出牌] 已选中 {selectedCards.Count} 张牌，selectedCardNumbers: {selectedCardNumbers.Count}");

        // 延迟执行自动出牌，给玩家一点反应时间
        StartCoroutine(AutoPlayLastHandDelayed());
    }

    /// <summary>
    /// 检测手牌是否只剩最后一手牌（可以一次出完）
    /// 返回null表示不是最后一手，返回非空列表表示最后一手的牌
    /// </summary>
    private List<PDKCardData> DetectLastHandCombination()
    {
        if (myHandCards == null || myHandCards.Count == 0)
            return null;

        GF.LogInfo($"[PDK检测最后一手] 手牌数量：{myHandCards.Count}");

        // 规则2：检查是否有炸弹（4张相同或AAA）
        bool hasBomb = CheckIfHandHasBomb(myHandCards);
        GF.LogInfo($"[PDK检测最后一手] 是否有炸弹：{hasBomb}");

        // 如果有炸弹，只有当手牌全部是炸弹时才允许自动出
        if (hasBomb)
        {
            // 检查手牌是否全部是一个炸弹
            if (IsAllCardsSingleBomb(myHandCards))
            {
                GF.LogInfo("[PDK检测最后一手] 手牌全是炸弹，允许自动出");
                // 手上只有一个炸弹，可以自动出
                return new List<PDKCardData>(myHandCards);
            }
            else
            {
                GF.LogInfo("[PDK检测最后一手] 手上有炸弹但还有其他牌，不自动出（保护炸弹）");
                // 手上有炸弹但还有其他牌，不自动出（避免拆炸弹）
                return null;
            }
        }

        // 没有炸弹，检测是否可以一手出完
        var cardType = PDKRuleChecker.DetectCardType(myHandCards, myHandCards);
        GF.LogInfo($"[PDK检测最后一手] 检测到牌型：{cardType}");

        // 如果是有效牌型（可以一手出完）
        if (cardType != PDKConstants.CardType.UNNOWN)
        {
            // 检查是否符合当前出牌规则（如果需要跟牌）
            if (lastPlayedCards != null && !lastPlayedCards.IsPass)
            {
                // 获取当前玩家的索引
                long myPlayerId = Util.GetMyselfInfo().PlayerId;
                int myPlayerIndex = GetPlayerIndexByServerId(myPlayerId);

                // 判断上家是否是自己
                bool isLastPlayedByMe = (lastPlayedCards.playerId == myPlayerIndex);

                if (isLastPlayedByMe)
                {
                    GF.LogInfo("[PDK检测最后一手] 上家是自己，直接允许出牌（首出）");
                    // 上家是自己，相当于首出，直接出
                    return new List<PDKCardData>(myHandCards);
                }

                GF.LogInfo($"[PDK检测最后一手] 需要跟牌，上家牌型：{lastPlayedCards.cardType}，上家玩家索引：{lastPlayedCards.playerId}，我的索引：{myPlayerIndex}");
                // 需要跟牌，检查是否能压过上家
                bool canPlay = PDKRuleChecker.CanPlayCards(myHandCards, myHandCards, lastPlayedCards, currentGameRules);
                if (canPlay)
                {
                    GF.LogInfo("[PDK检测最后一手] 可以压过上家，允许自动出");
                    return new List<PDKCardData>(myHandCards);
                }
                else
                {
                    GF.LogInfo("[PDK检测最后一手] 不能压过上家，不自动出");
                    // 不能压过上家，不自动出
                    return null;
                }
            }
            else
            {
                GF.LogInfo("[PDK检测最后一手] 首出或上家过牌，允许自动出");
                // 首出或上家过牌，直接出
                return new List<PDKCardData>(myHandCards);
            }
        }

        GF.LogInfo("[PDK检测最后一手] 不是有效牌型，不能自动出");
        return null;
    }

    /// <summary>
    /// 检查手牌中是否有炸弹（包括AAA规则）
    /// </summary>
    private bool CheckIfHandHasBomb(List<PDKCardData> handCards)
    {
        if (handCards == null || handCards.Count == 0)
            return false;

        var valueGroups = handCards.GroupBy(c => c.value).ToDictionary(g => g.Key, g => g.Count());

        foreach (var kvp in valueGroups)
        {
            // 检查4张炸弹
            if (kvp.Value == 4)
            {
                return true;
            }

            // 检查AAA炸弹规则
            if (currentGameRules.aaaIsBomb && kvp.Key == PDKConstants.CardValue.Ace && kvp.Value >= 3)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 检查手牌是否全部是一个炸弹
    /// </summary>
    private bool IsAllCardsSingleBomb(List<PDKCardData> handCards)
    {
        if (handCards == null || handCards.Count == 0)
            return false;

        // 检查是否所有牌都是同一点数
        var firstValue = handCards[0].value;
        bool allSameValue = handCards.All(c => c.value == firstValue);

        if (!allSameValue)
            return false;

        // 检查数量
        int count = handCards.Count;

        // 4张炸弹
        if (count == 4)
            return true;

        // AAA炸弹（3张A）
        if (count == 3 && currentGameRules.aaaIsBomb && firstValue == PDKConstants.CardValue.Ace)
            return true;

        return false;
    }

    /// <summary>
    /// 延迟自动出牌协程
    /// </summary>
    private System.Collections.IEnumerator AutoPlayLastHandDelayed()
    {
        GF.LogInfo("[PDK自动出牌] 延迟协程开始，等待0.3秒");
        // 等待0.3秒让玩家看到选中的牌
        yield return new WaitForSeconds(0.3f);

        GF.LogInfo($"[PDK自动出牌] 延迟结束，检查出牌状态 - 按钮激活：{varChuPaiBtns_15?.activeSelf ?? false}，已选牌数：{selectedCards.Count}，手牌数：{myHandCards?.Count ?? 0}");

        // 【关键保护】再次检查手牌是否为空，防止在延迟期间牌已打完
        if (myHandCards == null || myHandCards.Count == 0)
        {
            GF.LogWarning("[PDK自动出牌] 手牌已空，取消自动出牌");
            yield break;
        }

        // 检查是否还在出牌状态（防止期间状态改变）
        if (varChuPaiBtns_15 != null && varChuPaiBtns_15.activeSelf && selectedCards.Count > 0)
        {
            GF.LogInfo("[PDK自动出牌] 触发自动出牌");
            // 触发出牌按钮点击
            OnPlayCardsButtonClick();
        }
        else
        {
            GF.LogWarning($"[PDK自动出牌] 无法自动出牌 - 按钮：{varChuPaiBtns_15 != null}，激活：{varChuPaiBtns_15?.activeSelf ?? false}，选牌数：{selectedCards.Count}");
        }
    }

    /// <summary>
    /// 【已重构】提示按钮点击事件 - 使用纯函数版本
    /// </summary>
    private void OnHintButtonClick()
    {
        // 【新增】检查是否轮到自己出牌，只有轮到自己时才能使用提示
        if (varChuPaiBtns_15 == null || !varChuPaiBtns_15.activeSelf)
        {
            GF.LogWarning("[PDK提示] 当前不是自己的回合，无法使用提示功能");
            return;
        }

        if (gameLogic == null || myHandCards == null || myHandCards.Count == 0)
        {
            GF.LogWarning("[PDK提示] gameLogic或myHandCards为空");
            return;
        }

        // 【新版本】使用纯函数GetHintPure
        List<PDKCardData> hintCards = gameLogic.GetHintPure(myHandCards, lastPlayedCards, hintState);


        // 清空当前选中
        ClearSelectedCards();
        // 选中提示的牌
        SelectHintCards(hintCards);
    }

    #endregion

    #region 退出和解散功能

    /// <summary>
    /// 退出按钮点击事件
    /// 如果当前桌子是在准备状态，则发送离开请求；如果当前桌子是在游戏中，则发送申请解散请求
    /// </summary>
    private void OnExitButtonClick()
    {
        if (PdkProcedure?.EnterRunFastDeskRs == null)
        {
            GF.LogWarning("[PDK退出] 无法获取桌子信息");
            return;
        }

        // 检查当前玩家是否是旁观者
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        bool isSeated = PdkProcedure.EnterRunFastDeskRs.DeskPlayers != null &&
                        PdkProcedure.EnterRunFastDeskRs.DeskPlayers.Any(p => p.BasePlayer.PlayerId == myPlayerId);

        // 旁观者无论什么状态都直接退出
        if (!isSeated)
        {
            Util.GetInstance().OpenMJConfirmationDialog(
                "退出房间",
                "确定要退出房间吗？",
                () =>
                {
                    varMenu.SetActive(false);
                    PdkProcedure.Send_LeaveDeskRq();
                }
            );
            return;
        }

        // 判断桌子状态
        bool isWaiting = PdkProcedure.EnterRunFastDeskRs.DeskState == RunFastDeskState.RunfastWait;

        if (isWaiting)
        {
            // 准备状态 - 直接退出
            Util.GetInstance().OpenMJConfirmationDialog(
                "退出房间",
                "确定要退出房间吗？",
                () =>
                {
                    varMenu.SetActive(false);
                    PdkProcedure.Send_LeaveDeskRq();
                }
            );
        }
        else
        {
            //弹窗游戏中无法退出
            GF.UI.ShowToast("游戏进行中，无法退出房间");
            // //游戏中 - 申请解散（仅对已坐下的玩家）
            // Util.GetInstance().OpenMJConfirmationDialog(
            //     "解散房间",
            //     "游戏进行中，确定要申请解散房间吗？",
            //     () =>
            //     {
            //         varMenu.SetActive(false);
            //         PdkProcedure.Send_Msg_ApplyMJDismissRq();
            //     }
            // );
        }
    }
    /// <summary>
    /// 发送离开桌子请求
    /// </summary>
    /// <summary>
    /// 发送离开桌子请求（仅在等待状态使用）
    /// </summary>
    private void SendLeaveDeskRequest()
    {
        if (PdkProcedure != null)
        {
            PdkProcedure.Send_LeaveDeskRq();
        }
        else
        {
            GF.LogError("[PDK退出] 无法获取PDKProcedures");
        }
    }

    #endregion

    #region 管理器初始化

    /// <summary>
    /// 初始化所有管理器
    /// </summary>
    private void InitializeManagers()
    {
        // 初始化卡牌管理器
        cardManager = GetOrAddComponent<PDKCardManager>();
        // 初始化UI管理器
        uiManager = GetOrAddComponent<PDKUIManager>();
        // 初始化游戏逻辑管理器（自动添加）
        gameLogic = GetOrAddComponent<PDKGameLogic>();
        if (gameLogic != null)
        {
            gameLogic.Initialize(this);
        }
        // 初始化卡牌对象池
        cardPool = GetOrAddComponent<PDKCardPool>();
        if (cardPool != null && varCard != null)
        {
            cardPool.Initialize(varCard, this.transform);
        }
        // 初始化卡牌辅助工具
        cardHelper = GetOrAddComponent<PDKCardHelper>();
        if (cardHelper != null && varCard != null)
        {
            cardHelper.Initialize(varCard, this.transform);
        }
        // 设置UI管理器引用
        if (uiManager != null)
        {
            uiManager.varHandCard = varHandCard;
            uiManager.varCard = varCard;
            uiManager.enableDebugLog = enableDebugLog;
        }
        // 判断当前玩家是否为房间创建者
        varSetBg.transform.Find("SettingPanel/jiesanBtn").gameObject.SetActive(Util.IsMySelf(PdkProcedure.EnterRunFastDeskRs.Creator.PlayerId));
    }

    /// <summary>
    /// 获取或添加组件（泛型辅助方法）
    /// </summary>
    private T GetOrAddComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }
        return component;
    }

    /// <summary>
    /// 从服务器数据初始化游戏数据（在 InitPDKDesk 中调用）  
    /// <param name="enterData">服务器返回的进入桌子数据</param>
    private void InitializeGameLogicFromServer(Msg_EnterRunFastDeskRs enterData)
    {
        // 初始化游戏规则配置
        if (enterData.Config != null)
        {
            currentGameRules.InitFromServerConfig(enterData.Config);
        }
        else
        {
            GF.LogWarning($"[PDKGamePanel] 服务器配置为空，使用默认规则");
        }

        // 清空旧数据
        players.Clear();
        passedPlayers.Clear();
        myHandCards.Clear();

        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        int totalPlayerNum = enterData.BaseConfig.PlayerNum;

        // 从服务器数据创建玩家列表
        foreach (var serverPlayer in enterData.DeskPlayers)
        {
            int clientPos = CalculateClientPosition(serverPlayer, myPlayerId, enterData.DeskPlayers, totalPlayerNum);
            var playerData = new PDKPlayerData(clientPos, serverPlayer.BasePlayer.Nick);
            playerData.playerId = clientPos;

            // 如果是自己，恢复手牌
            if (clientPos == 0 && serverPlayer.HandCard?.Count > 0)
            {
                var validCards = serverPlayer.HandCard.Where(cardId => cardId > 0).ToList();
                if (validCards.Count > 0)
                {
                    var serverHandCards = ConvertCardIdsToCardDataList(validCards);
                    playerData.AddCards(serverHandCards);
                    myHandCards = new List<PDKCardData>(serverHandCards);
                }
            }
            else
            {
                // 【关键修复】对于其他玩家，从playerIdToCardCount字典获取正确的牌数
                // 不要使用serverPlayer.HandCard，因为服务器可能不会发送其他玩家的完整手牌数据
                long serverId = serverPlayer.BasePlayer.PlayerId;
                if (playerIdToCardCount.TryGetValue(serverId, out int cardCount))
                {
                    // 使用字典中的正确数据（来自HandCardNum）
                    for (int i = 0; i < cardCount; i++)
                    {
                        playerData.AddCard(new PDKCardData(PDKConstants.CardSuit.Spades, PDKConstants.CardValue.Three));
                    }
                    GF.LogInfo_wl($"<color=#00FF00>[InitGameLogic] 玩家{serverId}从字典获取牌数: {cardCount}张</color>");
                }
                else
                {
                    GF.LogWarning($"<color=#FF0000>[InitGameLogic] ⚠️ 玩家{serverId}在字典中找不到数据</color>");
                }
            }

            players.Add(playerData);
        }
        // 按客户端位置排序
        players = players.OrderBy(p => p.playerId).ToList();
        // 初始化游戏状态
        currentPlayerIndex = -1;
        dealerPlayerIndex = -1;
        roundWinnerIndex = -1;
        lastPlayedCards = null;

        // 【前端防御】判断是否是游戏进行中状态
        bool isPlaying = (enterData.DeskState == RunFastDeskState.RunfastStart ||
                          enterData.DeskState == RunFastDeskState.RunfastDiscard);

        // 【前端防御】只在游戏进行中才设置庄家（防止后端在等待阶段给错误数据）
        if (isPlaying && enterData.Banker > 0)
        {
            var bankerPlayer = enterData.DeskPlayers.FirstOrDefault(p => p.BasePlayer.PlayerId == enterData.Banker);
            if (bankerPlayer != null)
            {
                dealerPlayerIndex = CalculateClientPosition(bankerPlayer, myPlayerId, enterData.DeskPlayers, totalPlayerNum);
            }
        }
        else if (!isPlaying)
        {
        }

        // 【前端防御】只在游戏进行中才设置当前玩家（防止后端在等待阶段给错误数据）
        if (isPlaying && enterData.PlayerId > 0)
        {
            var currentServerPlayer = enterData.DeskPlayers.FirstOrDefault(p => p.BasePlayer.PlayerId == enterData.PlayerId);
            if (currentServerPlayer != null)
            {
                // 【关键修复】判断是否为旁观者
                var myPlayer = enterData.DeskPlayers.FirstOrDefault(p => p.BasePlayer.PlayerId == myPlayerId);
                bool isSpectator = (myPlayer == null);

                if (isSpectator)
                {
                    // 旁观者：使用默认位置作为自己的位置
                    currentPlayerIndex = Util.GetPosByServerPos_pdk(Position.Default, currentServerPlayer.Pos, totalPlayerNum);
                }
                else
                {
                    // 普通玩家：使用相对位置计算
                    currentPlayerIndex = CalculateClientPosition(currentServerPlayer, myPlayerId, enterData.DeskPlayers, totalPlayerNum);
                }
            }
        }
        else if (!isPlaying)
        {
        }

        // 【前端防御】只在游戏进行中才设置首出玩家和出牌相关数据
        if (isPlaying)
        {
            serverLastMaxDiscard = enterData.LastMaxDiscard;
            serverMaxDisCardPlayer = enterData.LastMaxDiscard;
            isFirstRound = (enterData.LastMaxDiscard == 0);
        }
        else
        {
            serverLastMaxDiscard = 0;
            serverMaxDisCardPlayer = 0;
            isFirstRound = true;
        }

        // 设置游戏状态
        currentGameState = (isPlaying && currentPlayerIndex >= 0)
            ? PDKConstants.GameState.RUNFAST_DISCARD
            : PDKConstants.GameState.RUNFAST_WAIT;

        // 【重连恢复】检查是否需要显示翻倍按钮
        if (enterData.DeskState == RunFastDeskState.RunfastStart)
        {
            UpdateDoubleBtnDisplay();
        }
    }

    /// <summary>
    /// 检查玩家是否已经翻倍过
    /// </summary>
    private bool CheckIfPlayerHasDoubled(long playerId, Msg_EnterRunFastDeskRs enterData)
    {
        if (enterData.DoubleRob == null || enterData.DoubleRob.Count == 0)
        {
            return false; // 没有翻倍数据，视为未翻倍
        }


        // 找到对应玩家的翻倍倍数
        var player = enterData.DeskPlayers.FirstOrDefault(p => p.BasePlayer.PlayerId == playerId);
        if (player == null)
        {
            GF.LogWarning($"[PDK翻倍-检查] 玩家{playerId}: 在DeskPlayers中找不到，判定未翻倍");
            return false;
        }

        int playerIndex = enterData.DeskPlayers.IndexOf(player);

        if (playerIndex >= 0 && playerIndex < enterData.DoubleRob.Count)
        {
            int doubleValue = enterData.DoubleRob[playerIndex].Val;

            // doubleValue: 1=不翻倍, 2=翻倍, 4=超级翻倍
            // 返回 true 表示已经做过选择（包括选择了不翻倍）
            bool hasDoubled = (doubleValue > 0);
            return hasDoubled;
        }

        GF.LogWarning($"[PDK翻倍-检查] 玩家{playerId}: playerIndex越界，判定未翻倍");
        return false;
    }

    /// <summary>
    /// 获取玩家的翻倍倍数
    /// </summary>
    private long GetPlayerDoubleValue(long playerId, Msg_EnterRunFastDeskRs enterData)
    {
        if (enterData.DoubleRob == null || enterData.DoubleRob.Count == 0)
        {
            return 0;
        }

        var player = enterData.DeskPlayers.FirstOrDefault(p => p.BasePlayer.PlayerId == playerId);
        if (player == null) return 0;

        int playerIndex = enterData.DeskPlayers.IndexOf(player);
        if (playerIndex >= 0 && playerIndex < enterData.DoubleRob.Count)
        {
            return enterData.DoubleRob[playerIndex].Val;
        }

        return 0;
    }

    #region 翻倍功能

    /// <summary>
    /// 翻倍按钮点击处理
    /// </summary>
    /// <param name="rob">翻倍倍数：1=不翻倍, 2=翻倍, 4=超级翻倍</param>
    private void OnDoubleBtnClick(int rob)
    {

        // 发送翻倍请求到服务器
        if (PdkProcedure != null)
        {
            PdkProcedure.SendDoubleRequest(rob);
        }
        else
        {
            GF.LogWarning("[PDK翻倍] 无法获取PDKProcedures");
        }
    }

    /// <summary>
    /// 处理翻倍推送消息（显示其他玩家的翻倍状态）
    /// </summary>
    public void HandleDoubleResult(long playerId, int rob)
    {

        // 如果是自己，隐藏翻倍按钮
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        if (playerId == myPlayerId)
        {
            DoubleBtn.SetActive(false);
        }

        // 显示玩家头像上的加倍标识
        ShowPlayerDoubleIndicator(playerId, rob);
    }

    /// <summary>
    /// 显示玩家头像框上的加倍标识
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="rob">翻倍倍数：1=不翻倍, 2=翻倍, 4=超级翻倍</param>
    private void ShowPlayerDoubleIndicator(long playerId, int rob)
    {
        // 获取玩家对应的头像框
        if (!playerIdToHeadBg.TryGetValue(playerId, out GameObject headBg) || headBg == null)
        {
            GF.LogWarning($"[PDK翻倍] 找不到玩家{playerId}的头像框");
            return;
        }

        // 查找jiabei节点
        Transform jiabeiTransform = headBg.transform.Find("jiabei");


        // 根据倍数显示或隐藏加倍标识
        // rob=1表示不翻倍，应该隐藏标识
        // rob=2或4表示翻倍，应该显示标识
        bool shouldShow = (rob >= 2);
        jiabeiTransform.gameObject.SetActive(shouldShow);

    }

    #endregion

    #region 剩牌数量显示功能



    /// <summary>
    /// 更新所有玩家的剩牌数量显示
    /// 根据游戏规则showRemainCard决定是否显示
    /// </summary>
    public void UpdateAllPlayersCardCount()
    {
        long myPlayerId = Util.GetMyselfInfo().PlayerId;

        // 【观战者逻辑】判断是否为观战者
        bool isSpectator = PdkProcedure?.EnterRunFastDeskRs?.DeskPlayers == null ||
                          !PdkProcedure.EnterRunFastDeskRs.DeskPlayers.Any(p => p.BasePlayer.PlayerId == myPlayerId);

        //GF.LogInfo_wl($"<color=#FFFF00>[UpdateAllPlayersCardCount] 开始更新，规则showRemainCard={currentGameRules.showRemainCard}, isSpectator={isSpectator}, playerIdToHeadBg数量={playerIdToHeadBg.Count}</color>");

        // 【关键】检查是否开启了显示剩牌规则（观战者总是显示）
        if (!currentGameRules.showRemainCard && !isSpectator)
        {
            // 规则未开启且不是观战者，隐藏所有剩牌显示
            //  GF.LogWarning($"<color=#FF0000>[UpdateAllPlayersCardCount] 规则未开启且不是观战者，隐藏所有剩牌显示</color>");
            HideAllPlayersCardBack();
            return;
        }

        // 遍历所有玩家的头像框
        foreach (var kvp in playerIdToHeadBg)
        {
            long playerId = kvp.Key;
            GameObject headBg = kvp.Value;

            if (headBg == null || !headBg.activeSelf)
            {
                GF.LogWarning($"<color=#FFA500>[UpdateAllPlayersCardCount] 玩家{playerId}的headBg为null或未激活，跳过</color>");
                continue;
            }

            int cardCount = GetPlayerCardCount(playerId);
            GF.LogInfo_wl($"<color=#00FF00>[UpdateAllPlayersCardCount] 玩家{playerId}: {cardCount}张牌，headBg={headBg.name}</color>");
            UpdatePlayerCardBack(headBg, cardCount, isSpectator);
        }
    }

    /// <summary>
    /// 获取指定玩家的手牌数量
    /// </summary>
    /// <param name="playerId">玩家服务器ID</param>
    /// <returns>手牌数量</returns>
    private int GetPlayerCardCount(long playerId)
    {
        long myPlayerId = Util.GetMyselfInfo().PlayerId;

        // 如果是自己，直接返回myHandCards的数量
        if (playerId == myPlayerId)
        {
            int myCount = myHandCards?.Count ?? 0;
            return myCount;
        }

        // 【关键修复】直接从playerIdToCardCount字典获取，这是唯一可信的数据源
        // 重连时从服务器HandCardNum同步，出牌时实时更新
        if (playerIdToCardCount.TryGetValue(playerId, out int cardCount))
        {
            return cardCount;
        }

        // 【移除旧逻辑】不再从players列表获取，因为那个数据可能过期
        // players列表在InitializeGameLogicFromServer时填充，但HandCardCount可能不准确
        GF.LogWarning($"[PDK剩牌] ⚠️ 字典中找不到玩家{playerId}的数据，返回0");
        return 0;
    }

    /// <summary>
    /// 更新单个玩家头像框下的cardBack节点显示
    /// </summary>
    /// <param name="headBg">玩家头像框</param>
    /// <param name="cardCount">剩牌数量</param>
    /// <param name="isSpectator">是否为观战者</param>
    public void UpdatePlayerCardBack(GameObject headBg, int cardCount, bool isSpectator = false)
    {
        if (headBg == null)
        {
            return;
        }
        // 注意：不应该在这里反向更新playerIdToCardCount字典，避免覆盖正确的数据源
        long playerId = -1;
        foreach (var kvp in playerIdToHeadBg)
        {
            if (kvp.Value == headBg)
            {
                playerId = kvp.Key;
                break;
            }
        }
        if (playerId == -1)
        {
            GF.LogInfo_wl($"[PDK剩牌] 找不到headBg对应的玩家ID: {headBg.name}");
            return;
        }

        // 查找cardBack节点
        Transform cardBackTransform = headBg.transform.Find("cardBack");
        if (cardBackTransform == null)
        {
            GF.LogInfo_wl($"[PDK剩牌] 头像框{headBg.name}下找不到cardBack节点");
            return;
        }
        // 【关键】根据规则决定是否显示（观战者总是显示）
        bool shouldShow = currentGameRules.showRemainCard || isSpectator;
        GF.LogInfo_wl($"<color=#00FFFF>[UpdatePlayerCardBack] 玩家{playerId}, cardCount={cardCount}, shouldShow={shouldShow}, cardBack当前active={cardBackTransform.gameObject.activeSelf}</color>");
        cardBackTransform.gameObject.SetActive(shouldShow);
        if (!shouldShow)
        {
            return; // 规则未开启且不是观战者，不更新文本
        }
        // 查找Text子节点
        Transform textTransform = cardBackTransform.Find("Text");
        if (textTransform == null)
        {
            GF.LogWarning($"[PDK剩牌] cardBack节点下找不到Text子节点");
            return;
        }
        // 更新文本显示
        Text cardCountText = textTransform.GetComponent<Text>();
        cardCountText.text = cardCount.ToString();

    }

    /// <summary>
    /// 初始化所有玩家的剩牌数量（发牌时调用）
    /// </summary>
    /// <param name="playerIds">玩家ID数组</param>
    /// <param name="initialCount">初始牌数（通常为16，进入房间等待时为0）</param>
    public void InitializePlayerCardCounts(long[] playerIds, int initialCount)
    {
        if (playerIds == null || playerIds.Length == 0)
        {
            GF.LogWarning("[PDK剩牌] InitializePlayerCardCounts: 玩家ID数组为空");
            return;
        }


        // 清空旧数据
        playerIdToCardCount.Clear();

        // 初始化每个玩家的剩牌数量
        foreach (long playerId in playerIds)
        {
            playerIdToCardCount[playerId] = initialCount;
        }

        // 刷新UI显示
        UpdateAllPlayersCardCount();
    }

    /// <summary>
    /// 隐藏所有玩家的cardBack节点
    /// </summary>
    private void HideAllPlayersCardBack()
    {
        ForEachHeadBg(headBg => headBg.transform.Find("cardBack")?.gameObject.SetActive(false));
    }

    #endregion

    /// <summary>
    /// 更新翻倍按钮显示状态（根据规则和玩家是否已翻倍）
    /// </summary>
    public void UpdateDoubleBtnDisplay()
    {
        if (DoubleBtn == null)
        {
            GF.LogWarning("[PDK翻倍] DoubleBtn为空，无法更新显示状态");
            return;
        }

        // 如果规则没有开启翻倍，直接隐藏
        if (!currentGameRules.doubleScore)
        {
            DoubleBtn.SetActive(false);
            return;
        }

        if (PdkProcedure?.EnterRunFastDeskRs == null)
        {
            DoubleBtn.SetActive(true);
            return;
        }

        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        bool hasDoubled = CheckIfPlayerHasDoubled(myPlayerId, PdkProcedure.EnterRunFastDeskRs);
        DoubleBtn.SetActive(!hasDoubled);
    }
    /// <summary>
    /// 计算玩家的客户端位置（0=自己, 1=右边/下家, 2=左边/上家）
    /// </summary>
    private int CalculateClientPosition(DeskPlayer player, long myPlayerId,
        Google.Protobuf.Collections.RepeatedField<DeskPlayer> allPlayers, int totalPlayerNum)
    {
        if (player.BasePlayer.PlayerId == myPlayerId)
        {
            return 0;
        }
        var myPlayer = allPlayers.FirstOrDefault(p => p.BasePlayer.PlayerId == myPlayerId);
        if (myPlayer == null)
        {
            return -1;
        }
        Position myServerPos = myPlayer.Pos;
        // 直接使用新方法计算相对位置
        return Util.GetPosByServerPos_pdk(myServerPos, player.Pos, totalPlayerNum);
    }
    /// <summary>
    /// 将服务器返回的卡牌ID列表转换为 PDKCardData 列表
    /// </summary>
    /// <param name="cardIds">卡牌ID列表（服务器格式：0-51）</param>
    /// <returns>PDKCardData 列表</returns>
    private List<PDKCardData> ConvertCardIdsToCardDataList(List<int> cardIds)
    {
        List<PDKCardData> cardDataList = new List<PDKCardData>();
        foreach (int cardId in cardIds)
        {
            int color = GameUtil.GetInstance().GetColor(cardId);
            int value = GameUtil.GetInstance().GetValue(cardId);
            PDKConstants.CardSuit suit = (PDKConstants.CardSuit)(color + 1);
            PDKConstants.CardValue cardValue;
            if (value == 1)
            {
                cardValue = PDKConstants.CardValue.Ace;
            }
            else if (value == 2) // 2
            {
                cardValue = PDKConstants.CardValue.Two;
            }
            else // 3-13 (3-K)
            {
                cardValue = (PDKConstants.CardValue)value; // 直接映射
            }
            PDKCardData cardData = new PDKCardData(suit, cardValue);
            cardDataList.Add(cardData);
        }
        return cardDataList;
    }
    #endregion

    #region 网络消息处理（联网核心逻辑）

    /// <summary>
    /// 处理自己的准备状态更新（收到服务器响应）
    /// </summary>
    /// <param name="readyState">准备状态：1=已准备，0=取消准备</param>
    public void OnReadyStateChanged(int readyState)
    {
        bool isReadyNow = (readyState == 1);
        isReady = isReadyNow;
        var YiJiuXiu0 = varHeadBg0.transform.Find("YiJiuXu");
        if (YiJiuXiu0 != null) YiJiuXiu0.gameObject.SetActive(isReadyNow);
        ToggleReadyButtons(isReadyNow);
        UpdateSettlementButtons();
    }

    /// <summary>
    /// 处理其他玩家的准备状态更新（收到服务器同步通知）
    /// </summary>
    /// <param name="playerId">玩家ID（服务器ID）</param>
    /// <param name="readyState">准备状态：1=已准备，0=取消准备</param>
    public void OnOtherPlayerReadyChanged(long playerId, int readyState)
    {
        bool isReadyNow = (readyState == 1);
        if (playerIdToHeadBg.TryGetValue(playerId, out GameObject headBg))
        {
            var YiJiuXiu = headBg.transform.Find("YiJiuXu");
            YiJiuXiu.gameObject.SetActive(isReadyNow);
        }
        else
        {
            GF.LogWarning($"[PDK准备] playerIdToHeadBg 中找不到玩家ID {playerId}");
        }
    }


    #endregion

    #region 本地手牌管理

    /// <summary>
    /// 检查并显示扎鸟标识（红桃10）
    /// </summary>
    private void CheckAndShowZhaNiaoMark()
    {
        // 检查是否开启扎鸟模式
        bool isZhaNiaoMode = PDKConstants.HasCheckboxRule(5); // 规则5: 红桃10扎鸟
        if (!isZhaNiaoMode || zhaNiao == null)
        {
            zhaNiao.SetActive(false);
            return;
        }
        // 查找红桃10
        GameObject redTenCard = FindRedTenCard();
        if (redTenCard != null)
        {
            // 显示扎鸟标识并设置到红桃10下方
            PositionZhaNiaoUnderCard(redTenCard);
            zhaNiao.SetActive(true);
        }
        else
        {
            // 没有红桃10，隐藏标识
            zhaNiao.SetActive(false);
        }
    }

    /// <summary>
    /// 查找红桃10卡牌
    /// </summary>
    private GameObject FindRedTenCard()
    {
        if (varHandCard == null) return null;

        foreach (Transform child in varHandCard.transform)
        {

            if (child.name.Contains("HandCard_42"))
            {
                return child.gameObject;
            }
        }
        return null;
    }

    /// <summary>
    /// 将扎鸟标识定位到红桃10下方
    /// </summary>
    private void PositionZhaNiaoUnderCard(GameObject card)
    {

        if (zhaNiao == null || card == null) return;

        // 实例化一个新的扎鸟对象，作为红桃10的子物体
        GameObject zhaNiaoCopy = Instantiate(zhaNiao, card.transform);
        zhaNiaoCopy.SetActive(true);

        // 设置RectTransform属性
        var zhaNiaoRect = zhaNiaoCopy.GetComponent<RectTransform>();
        if (zhaNiaoRect == null)
        {
            zhaNiaoRect = zhaNiaoCopy.AddComponent<RectTransform>();
        }
        zhaNiaoRect.anchoredPosition = new Vector2(-80f, -20f);
        zhaNiaoCopy.transform.SetAsLastSibling();

    }

    /// <summary>
    /// 创建手牌对象（带发牌动画）
    /// </summary>
    /// <param name="cardList">卡牌编号列表</param>
    /// <param name="withAnimation">是否播放发牌动画（默认true，重连时传false）</param>
    private void CreateHandCardObjects(List<int> cardList, bool withAnimation = true)
    {
        //先排序手牌 在创建
        cardList = cardManager.SortCards(cardList);

        if (withAnimation)
        {
            // 启动发牌动画协程
            StartCoroutine(DealCardsAnimation(cardList));
        }
        else
        {
            // 重连时直接创建手牌，不播放动画
            StartCoroutine(CreateHandCardsWithoutAnimation(cardList));
        }
    }

    /// <summary>
    /// 发牌动画：从中间向两边扩散
    /// </summary>
    private IEnumerator DealCardsAnimation(List<int> cardList)
    {
        int totalCards = cardList.Count;
        int middleIndex = totalCards / 2;

        // 先创建所有卡牌但设置为不可见
        List<GameObject> cardObjects = new List<GameObject>();
        for (int i = 0; i < totalCards; i++)
        {
            int cardId = cardList[i];
            // 使用对象池创建卡牌，cardId会在GetCard中立即初始化
            GameObject cardObj = CreateCardGameObject(i, cardId);
            SetupCardComponent(cardObj, cardId);
            SetupCardTransform(cardObj);

            // 初始设置：缩放为0，透明度为0
            RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
            rectTransform.localScale = Vector3.zero;

            CanvasGroup canvasGroup = cardObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = cardObj.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;

            cardObjects.Add(cardObj);
        }

        // 从中间向两边扩散发牌
        float dealInterval = 0.03f; // 每张牌的间隔时间
        float animDuration = 0.15f; // 每张牌的动画时长

        // 计算发牌顺序：中间->左右交替
        List<int> dealOrder = new List<int>();

        if (totalCards % 2 == 0)
        {
            // 偶数张牌：从中间两张开始
            int leftIndex = middleIndex - 1;
            int rightIndex = middleIndex;

            while (leftIndex >= 0 || rightIndex < totalCards)
            {
                if (leftIndex >= 0)
                {
                    dealOrder.Add(leftIndex);
                    leftIndex--;
                }
                if (rightIndex < totalCards)
                {
                    dealOrder.Add(rightIndex);
                    rightIndex++;
                }
            }
        }
        else
        {
            // 奇数张牌：从正中间开始
            dealOrder.Add(middleIndex);
            int leftIndex = middleIndex - 1;
            int rightIndex = middleIndex + 1;

            while (leftIndex >= 0 || rightIndex < totalCards)
            {
                if (leftIndex >= 0)
                {
                    dealOrder.Add(leftIndex);
                    leftIndex--;
                }
                if (rightIndex < totalCards)
                {
                    dealOrder.Add(rightIndex);
                    rightIndex++;
                }
            }
        }

        // 按顺序播放发牌动画
        foreach (int index in dealOrder)
        {
            GameObject cardObj = cardObjects[index];
            RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = cardObj.GetComponent<CanvasGroup>();

            // 使用DOTween播放缩放和淡入动画
            rectTransform.DOScale(Vector3.one, animDuration).SetEase(Ease.OutBack);
            canvasGroup.DOFade(1f, animDuration);

            // 播放发牌音效（可选）
            // Sound.PlayEffect("Audio/pdk/deal_card.mp3");

            yield return new WaitForSeconds(dealInterval);
        }

        // 等待最后一张牌的动画完成
        yield return new WaitForSeconds(animDuration);

        // 动画完成后重新排列
        if (uiManager != null)
        {
            StartCoroutine(uiManager.ArrangeHandCardsNextFrame());
        }

        // 【关键】发牌完成后，初始化myHandCards数组（从cardList转换）
        myHandCards = ConvertCardIdsToCardDataList(cardList);
        // 发牌完成后，恢复所有手牌的正常交互状态
        RestoreAllHandCardsInteraction();
        // 发牌完成后，检查并显示扎鸟标识
        CheckAndShowZhaNiaoMark();
    }

    /// <summary>
    /// 创建手牌（无动画版本，用于重连恢复）
    /// </summary>
    private IEnumerator CreateHandCardsWithoutAnimation(List<int> cardList)
    {
        int totalCards = cardList.Count;

        // 直接创建所有卡牌，不播放动画
        for (int i = 0; i < totalCards; i++)
        {
            int cardId = cardList[i];
            GameObject cardObj = CreateCardGameObject(i, cardId);
            SetupCardComponent(cardObj, cardId);
            SetupCardTransform(cardObj);

            // 直接设置为可见状态，不播放动画
            RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
            rectTransform.localScale = Vector3.one;

            CanvasGroup canvasGroup = cardObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = cardObj.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 1f;
        }

        // 等待一帧后重新排列
        yield return null;

        if (uiManager != null)
        {
            uiManager.ArrangeHandCards();
        }

        // 初始化myHandCards数组
        myHandCards = ConvertCardIdsToCardDataList(cardList);

        // 恢复所有手牌的正常交互状态
        RestoreAllHandCardsInteraction();

        // 【新增】检查并显示扎鸟标识
        CheckAndShowZhaNiaoMark();
    }

    /// <summary>
    /// 创建单张手牌（无动画版本，用于特殊情况）
    /// </summary>
    /// <param name="cardNum">牌的编号(0-51)</param>
    /// <param name="index">在手牌中的索引</param>
    private void CreateHandCard(int cardNum, int index)
    {
        GameObject cardObj = CreateCardGameObject(index, cardNum);
        SetupCardComponent(cardObj, cardNum);
        SetupCardTransform(cardObj);

        if (enableDebugLog)
        {
            int value = GameUtil.GetInstance().GetValue(cardNum);
            int color = GameUtil.GetInstance().GetColor(cardNum);
        }
    }

    /// <summary>
    /// 创建卡牌游戏对象（使用对象池）
    /// </summary>
    public GameObject CreateCardGameObject(int index, int cardId)
    {
        // 使用对象池获取卡牌，确保在激活前完成初始化
        if (cardPool != null && varCard != null)
        {
            return cardPool.GetCard(varHandCard.transform, cardId);
        }

        // 降级方案：直接实例化（不应该走到这里）
        GF.LogWarning("[PDKGamePanel] 对象池未初始化，使用直接实例化（性能较差）");
        GameObject cardObj = varCard != null ?
            Instantiate(varCard, varHandCard.transform) :
            new GameObject($"Card_{index}");

        if (varCard == null)
        {
            cardObj.transform.SetParent(varHandCard.transform);
        }

        return cardObj;
    }

    /// <summary>
    /// 创建卡牌游戏对象（使用对象池，带父物体参数）
    /// </summary>
    public GameObject CreateCardGameObject(int index, int cardId, Transform parent)
    {
        // 使用对象池获取卡牌
        if (cardPool != null)
        {
            return cardPool.GetCard(parent, cardId);
        }

        // 降级方案：直接实例化
        GF.LogWarning("[PDKGamePanel] 对象池未初始化，使用直接实例化（性能较差）");
        GameObject cardObj = varCard != null ?
            Instantiate(varCard, parent) :
            new GameObject($"Card_{index}");

        if (varCard == null)
        {
            cardObj.transform.SetParent(parent);
        }

        return cardObj;
    }

    /// <summary>
    /// 设置卡牌组件
    /// </summary>
    public void SetupCardComponent(GameObject cardObj, int cardNum)
    {
        cardObj.name = $"HandCard_{cardNum}";

        PDKCard cardComponent = cardObj.GetComponent<PDKCard>();
        if (cardComponent == null)
        {
            cardComponent = cardObj.AddComponent<PDKCard>();
        }

        // 【Bug修复1】确保卡牌完全初始化，防止偶现赋值不完整
        // 先确保Image组件存在
        if (cardComponent.image == null)
        {
            cardComponent.image = cardObj.GetComponent<Image>();
            if (cardComponent.image == null)
            {
                GF.LogWarning($"[卡牌初始化] 卡牌{cardNum}缺少Image组件");
            }
        }

        // 调用Init初始化卡牌数据
        cardComponent.Init(cardNum);

        // 验证初始化结果
        if (cardComponent.numD != cardNum)
        {
            GF.LogError($"[卡牌初始化] 卡牌{cardNum}初始化失败，numD={cardComponent.numD}");
            // 强制重新赋值
            cardComponent.numD = cardNum;
            cardComponent.value = GameUtil.GetInstance().GetValue(cardNum);
        }

        // 设置卡牌点击事件
        SetupCardClickEvent(cardComponent);

    }

    /// <summary>
    /// 设置卡牌变换组件
    /// </summary>
    public void SetupCardTransform(GameObject cardObj)
    {
        RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = cardObj.AddComponent<RectTransform>();
        }

        // 设置锚点为中心
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // 设置初始位置和大小
        rectTransform.localPosition = Vector3.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.sizeDelta = new Vector2(PDKUIManager.s_cardWidth, PDKUIManager.s_cardHeight);

        // 确保卡牌在正确的层级
        rectTransform.SetAsLastSibling();
    }

    /// <summary>
    /// 清空所有手牌（使用对象池回收）
    /// </summary>
    public void ClearAllHandCards()
    {
        if (varHandCard == null) return;

        // 使用对象池回收
        if (cardPool != null)
        {
            cardPool.RecycleAllCardsInParent(varHandCard.transform);
        }
        else
        {
            // 降级方案：直接销毁
            Transform parent = varHandCard.transform;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }
    }



    /// <summary>
    /// 收集现有卡牌数据
    /// </summary>
    private List<(int cardNum, GameObject cardObj)> CollectExistingCards()
    {
        var cardData = new List<(int cardNum, GameObject cardObj)>();
        Transform parent = varHandCard.transform;

        for (int i = 0; i < parent.childCount; i++)
        {
            GameObject cardObj = parent.GetChild(i).gameObject;
            PDKCard cardComponent = cardObj.GetComponent<PDKCard>();
            if (cardComponent != null)
            {
                cardData.Add((cardComponent.numD, cardObj));
            }
        }

        return cardData;
    }

    /// <summary>
    /// 获取所有手牌组件
    /// </summary>
    private List<PDKCard> GetAllHandCardComponents()
    {
        if (varHandCard == null) return new List<PDKCard>();

        return varHandCard.transform
            .Cast<Transform>()
            .Select(t => t.GetComponent<PDKCard>())
            .Where(card => card != null)
            .ToList();
    }

    /// <summary>
    /// 获取所有手牌的PDKCardData列表
    /// </summary>
    private List<PDKCardData> GetAllHandCards()
    {
        return GetAllHandCardComponents()
            .Select(c => cardHelper.ConvertToCardData(c))
            .Where(data => data != null)
            .ToList();
    }

    #endregion

    #region 音效播放

    /// <summary>
    /// 播放出牌音效
    /// </summary>
    /// <param name="cardIds">出的牌ID列表</param>
    /// <param name="playerId">玩家ID，用于确定使用男声还是女声</param>
    private void PlayCardSound(List<int> cardIds, long playerId = 0)
    {
        if (cardIds == null || cardIds.Count == 0) return;

        // 转换为卡牌数据
        List<PDKCardData> cardDataList = ConvertCardIdsToCardDataList(cardIds);
        if (cardDataList == null || cardDataList.Count == 0) return;
        // 检测牌型
        PDKConstants.CardType cardType = PDKRuleChecker.DetectCardType(cardDataList);
        // 生成音效路径（根据玩家ID选择声音）
        string soundPath = GetCardSoundPath(cardDataList, cardType, playerId);
        if (!string.IsNullOrEmpty(soundPath))
        {
            // 【关键修复】检查音频资源是否存在
            string fullPath = UtilityBuiltin.AssetsPath.GetSoundPath(soundPath);
            var hasAsset = GFBuiltin.Resource.HasAsset(fullPath);

            if (hasAsset == GameFramework.Resource.HasAssetResult.NotExist)
            {
                return;
            }

            Sound.PlayEffect(soundPath);
        }
    }

    // 防止重复播放特效的标记（特效容器 -> 是否正在播放）
    private Dictionary<Transform, bool> effectPlayingFlags = new Dictionary<Transform, bool>();

    /// <summary>
    /// 播放牌型特效（完全模仿麻将的实现）
    /// </summary>
    /// <param name="playArea">出牌区域</param>
    /// <param name="cardType">牌型</param>
    private void PlayCardTypeEffect(GameObject playArea, PDKConstants.CardType cardType)
    {
        if (playArea == null)
        {
            return;
        }
        // 根据出牌区域确定对应的特效容器
        Transform effectContainer = null;
        if (playArea == varPlayArea)
        {
            effectContainer = varGameOverCard.transform;
        }
        else if (playArea == varPlayArea1)
        {
            effectContainer = varGameOverCard1.transform;
        }
        else if (playArea == varPlayArea2)
        {
            effectContainer = varGameOverCard2.transform;
        }

        if (effectContainer == null)
        {
            return;
        }
        // 【修复】检查是否已经在播放特效，避免重复播放
        if (effectPlayingFlags.TryGetValue(effectContainer, out bool isPlaying) && isPlaying)
        {
            return;
        }

        // 【已移除客户端春天判断】春天/反春天特效由后端在结算消息中通过 Chun 字段告知
        // 客户端不再在出牌时自动检测,而是在收到结算消息时根据后端返回的 Chun 字段播放

        // 根据牌型确定特效预制体路径
        string effPath = null;
        string effectType = null;
        switch (cardType)
        {
            case PDKConstants.CardType.THREE_WITH_TWO:
                effPath = AssetsPath.GetPrefab("UI/PDK/3d2Eff");
                effectType = "三带二";
                break;
            case PDKConstants.CardType.STRAIGHT:
                effPath = AssetsPath.GetPrefab("UI/PDK/sunziEff");
                effectType = "顺子";
                break;
            case PDKConstants.CardType.STRAIGHT_PAIR:
                effPath = AssetsPath.GetPrefab("UI/PDK/lianduiEff");
                effectType = "连对";
                break;
            case PDKConstants.CardType.PLANE:
                effPath = AssetsPath.GetPrefab("UI/PDK/feijEff");
                effectType = "飞机";
                break;
            case PDKConstants.CardType.BOMB:
                effPath = AssetsPath.GetPrefab("UI/PDK/BoomEff");
                effectType = "炸弹";
                break;
            default:
                // 单张、对子、三张等不播放特效
                return;
        }
        if (string.IsNullOrEmpty(effPath))
        {
            return;
        }
        // 使用通用播放方法
        PlayEffect(effectContainer, effPath, effectType);
    }
    /// <summary>
    /// 延迟清除特效播放标记
    /// </summary>
    private IEnumerator ClearEffectFlagAfterDelay(Transform effectContainer, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (effectPlayingFlags.ContainsKey(effectContainer))
        {
            effectPlayingFlags[effectContainer] = false;
            // GF.LogInfo($"[PDK特效] 清除播放标记: {effectContainer.name}");
        }
    }
    /// <summary>
    /// 播放特效对应的音效
    /// </summary>
    /// <param name="effectType">特效类型</param>
    private void PlayEffectSound(string effectType)
    {
        string soundPath = null;

        switch (effectType)
        {
            case "飞机":
                soundPath = "pdk/plane.mp3"; // 或者 "pdk/yx_feiji.mp3"
                break;
            case "顺子":
                soundPath = "pdk/shunzi.mp3";
                break;
            case "连对":
                soundPath = "pdk/liandui.mp3";
                break;
            case "炸弹":
                soundPath = "pdk/boom.mp3"; // 或者 "pdk/bomb1.mp3"
                break;
            case "春天":
            case "反春天":
                soundPath = "pdk/chuntian.mp3";
                break;
            case "报警":
                soundPath = "pdk/Warning.mp3";
                break;
            default:
                // 其他特效不播放音效
                return;
        }

        if (!string.IsNullOrEmpty(soundPath))
        {
            Sound.PlayEffect(soundPath);
        }
    }

    /// <summary>
    /// 【新增】根据后端返回的 Chun 字段播放春天/反春天特效
    /// 在收到结算消息 Syn_RunFastSettle 后调用
    /// </summary>
    /// <param name="chunType">春天类型: 0=无, 1=春天, 2=反春天</param>
    /// <param name="onComplete">特效播放完成后的回调</param>
    public void PlaySpringEffectFromServer(int chunType, System.Action onComplete = null)
    {
        if (chunType == 0)
        {
            onComplete?.Invoke();
            return;
        }
        // 确定特效路径和类型
        string effPath = null;
        string effectType = null;
        if (chunType == 1)
        {
            effPath = AssetsPath.GetPrefab("UI/PDK/chuntianEff");
            effectType = "春天";
        }
        else if (chunType == 2)
        {
            effPath = AssetsPath.GetPrefab("UI/PDK/fanchuntianEff");
            effectType = "反春天";
        }
        else
        {
            GF.LogWarning($"[PDK特效] 无效的 Chun 值: {chunType}");
            onComplete?.Invoke();
            return;
        }
        // 使用自己的出牌区域的特效容器（因为春天/反春天是全局特效）
        Transform effectContainer = varGameOverCard.transform;

        if (effectContainer == null)
        {
            GF.LogWarning($"[PDK特效] 特效容器为空，无法播放{effectType}特效");
            onComplete?.Invoke();
            return;
        }
        // GF.LogInfo($"[PDK特效] 准备播放{effectType}特效(Chun={chunType})");
        PlayEffectWithCallback(effectContainer, effPath, effectType, onComplete);
    }

    /// <summary>
    /// 播放特效（通用方法，带完成回调）
    /// </summary>
    /// <param name="effectContainer">特效容器</param>
    /// <param name="effPath">特效预制体路径</param>
    /// <param name="effectType">特效类型（用于日志）</param>
    /// <param name="onComplete">特效播放完成后的回调</param>
    private void PlayEffectWithCallback(Transform effectContainer, string effPath, string effectType, System.Action onComplete = null)
    {
        if (string.IsNullOrEmpty(effPath))
        {
            onComplete?.Invoke();
            return;
        }

        // 【新增】播放特效对应的音效
        PlayEffectSound(effectType);

        // 标记开始播放特效
        effectPlayingFlags[effectContainer] = true;
        // GF.LogInfo($"[PDK特效] 开始加载特效: {effectType}, 容器: {effectContainer.name}");

        GF.UI.LoadPrefab(effPath, (gameObject) =>
        {
            if (gameObject == null)
            {
                GF.LogWarning($"[PDK特效] 实例化特效失败: {effectType}");
                effectPlayingFlags[effectContainer] = false;
                onComplete?.Invoke();
                return;
            }

            GameObject effectObj = Instantiate(gameObject as GameObject, effectContainer);

            // 设置特效位置（归零）
            RectTransform rectTransform = effectObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;

            effectObj.SetActive(true);
            var spineAni = effectObj.GetComponent<Spine.Unity.SkeletonAnimation>();
            if (spineAni != null)
            {
                // 【修复】安全播放动画，检查动画是否存在
                Spine.TrackEntry trackEntry = null;
                string animationName = "animation"; // 默认动画名称
                float animationDuration = 2.5f; // 默认时长

                try
                {
                    // 先检查动画是否存在
                    if (spineAni.Skeleton != null && spineAni.Skeleton.Data != null)
                    {
                        var skeletonData = spineAni.Skeleton.Data;

                        // 检查默认动画是否存在
                        if (skeletonData.FindAnimation(animationName) != null)
                        {
                            trackEntry = spineAni.AnimationState.SetAnimation(0, animationName, false);
                        }
                        else
                        {
                            // 如果默认动画不存在，尝试使用第一个可用动画
                            var animations = skeletonData.Animations;
                            if (animations != null && animations.Count > 0)
                            {
                                animationName = animations.Items[0].Name;
                                trackEntry = spineAni.AnimationState.SetAnimation(0, animationName, false);
                                // GF.LogInfo($"[PDK特效] 默认动画不存在，使用第一个动画: {animationName}");
                            }
                            else
                            {
                                GF.LogWarning($"[PDK特效] 特效没有可用动画: {effectType}");
                            }
                        }
                    }

                    // 获取动画时长
                    if (trackEntry != null)
                    {
                        animationDuration = trackEntry.Animation.Duration;
                        // GF.LogInfo($"[PDK特效] 显示特效: {effectType}, 容器: {effectContainer.name}, 动画: {animationName}, 时长: {animationDuration}秒");
                    }
                    else
                    {
                        // GF.LogInfo($"[PDK特效] 显示特效: {effectType}, 容器: {effectContainer.name}, 使用默认时长");
                    }
                }
                catch (System.Exception e)
                {
                    GF.LogError($"[PDK特效] 播放动画异常: {effectType}, 错误: {e.Message}");
                }

                Destroy(effectObj, animationDuration);
                StartCoroutine(ClearEffectFlagAfterDelay(effectContainer, animationDuration));
                // 【新增】在动画时长后执行回调
                if (onComplete != null)
                {
                    StartCoroutine(InvokeCallbackAfterDelay(onComplete, animationDuration));
                }
            }
            else
            {
                Destroy(effectObj, 2.5f);
                StartCoroutine(ClearEffectFlagAfterDelay(effectContainer, 2.5f));
                GF.LogWarning($"[PDK特效] 特效没有SkeletonAnimation组件，使用默认时长");
                // 【新增】在默认时长后执行回调
                if (onComplete != null)
                {
                    StartCoroutine(InvokeCallbackAfterDelay(onComplete, 2.5f));
                }
            }
        });
    }

    /// <summary>
    /// 延迟执行回调
    /// </summary>
    private IEnumerator InvokeCallbackAfterDelay(System.Action callback, float delay)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }

    /// <summary>
    /// 播放特效（通用方法）
    /// </summary>
    /// <param name="effectContainer">特效容器</param>
    /// <param name="effPath">特效预制体路径</param>
    /// <param name="effectType">特效类型（用于日志）</param>
    private void PlayEffect(Transform effectContainer, string effPath, string effectType)
    {
        if (string.IsNullOrEmpty(effPath))
        {
            return;
        }

        // 【新增】播放特效对应的音效
        PlayEffectSound(effectType);

        // 标记开始播放特效
        effectPlayingFlags[effectContainer] = true;
        // GF.LogInfo($"[PDK特效] 开始加载特效: {effectType}, 容器: {effectContainer.name}");

        GF.UI.LoadPrefab(effPath, (gameObject) =>
        {
            if (gameObject == null)
            {
                GF.LogWarning($"[PDK特效] 实例化特效失败: {effectType}");
                effectPlayingFlags[effectContainer] = false;
                return;
            }

            GameObject effectObj = Instantiate(gameObject as GameObject, effectContainer);

            // 设置特效位置（归零）
            RectTransform rectTransform = effectObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;

            effectObj.SetActive(true);
            var spineAni = effectObj.GetComponent<Spine.Unity.SkeletonAnimation>();
            if (spineAni != null)
            {
                // 【修复】安全播放动画，检查动画是否存在
                Spine.TrackEntry trackEntry = null;
                string animationName = "animation"; // 默认动画名称
                float animationDuration = 2.5f; // 默认时长

                try
                {
                    // 先检查动画是否存在
                    if (spineAni.Skeleton != null && spineAni.Skeleton.Data != null)
                    {
                        var skeletonData = spineAni.Skeleton.Data;

                        // 检查默认动画是否存在
                        if (skeletonData.FindAnimation(animationName) != null)
                        {
                            trackEntry = spineAni.AnimationState.SetAnimation(0, animationName, false);
                        }
                        else
                        {
                            // 如果默认动画不存在，尝试使用第一个可用动画
                            var animations = skeletonData.Animations;
                            if (animations != null && animations.Count > 0)
                            {
                                animationName = animations.Items[0].Name;
                                trackEntry = spineAni.AnimationState.SetAnimation(0, animationName, false);
                                // GF.LogInfo($"[PDK特效] 默认动画不存在，使用第一个动画: {animationName}");
                            }
                            else
                            {
                                GF.LogWarning($"[PDK特效] 特效没有可用动画: {effectType}");
                            }
                        }
                    }

                    // 获取动画时长
                    if (trackEntry != null)
                    {
                        animationDuration = trackEntry.Animation.Duration;
                        // GF.LogInfo($"[PDK特效] 显示特效: {effectType}, 容器: {effectContainer.name}, 动画: {animationName}, 时长: {animationDuration}秒");
                    }
                    else
                    {
                        // GF.LogInfo($"[PDK特效] 显示特效: {effectType}, 容器: {effectContainer.name}, 使用默认时长");
                    }
                }
                catch (System.Exception e)
                {
                    GF.LogError($"[PDK特效] 播放动画异常: {effectType}, 错误: {e.Message}");
                }

                Destroy(effectObj, animationDuration);
                StartCoroutine(ClearEffectFlagAfterDelay(effectContainer, animationDuration));
            }
            else
            {
                Destroy(effectObj, 2.5f);
                StartCoroutine(ClearEffectFlagAfterDelay(effectContainer, 2.5f));
                GF.LogWarning($"[PDK特效] 特效没有SkeletonAnimation组件，使用默认时长");
            }
        });
    }

    /// <summary>
    /// 获取当前选择的语音性别（从PlayerPrefs读取）
    /// </summary>
    /// <returns>"man" 或 "woman"</returns>
    private string GetVoiceGender()
    {
        // 从PlayerPrefs读取语音性别设置，默认为男声
        string gender = PlayerPrefs.GetString("MahjongVoiceGender", "male");
        // 转换为路径格式: male->man, female->woman
        return gender == "female" ? "woman" : "man";
    }

    /// <summary>
    /// 根据玩家ID获取语音性别（基于哈希算法，同一玩家始终相同）
    /// </summary>
    /// <param name="playerId">玩家ID，0表示自己</param>
    /// <returns>"man" 或 "woman"</returns>
    private string GetVoiceGenderForPlayer(long playerId)
    {
       // 跑得快全部使用男声
        return "man";
    }

    /// <summary>
    /// 根据牌型和数值生成音效路径
    /// </summary>
    /// <param name="cards">卡牌列表</param>
    /// <param name="cardType">牌型</param>
    /// <param name="playerId">玩家ID，用于确定使用男声还是女声（0表示自己）</param>
    /// <returns>音效路径</returns>
    private string GetCardSoundPath(List<PDKCardData> cards, PDKConstants.CardType cardType, long playerId = 0)
    {
        if (cards == null || cards.Count == 0) return null;

        // 基础路径: pdk/pt/{性别}/card/
        // 自己使用本地设置，其他玩家根据哈希值计算
        string gender = GetVoiceGenderForPlayer(playerId);
        string basePath = $"pdk/pt/{gender}/card/";

        string soundKey = null;

        switch (cardType)
        {
            case PDKConstants.CardType.SINGLE:
                // 单张: 播放数值(如"3","4"..."A","2")
                soundKey = GetCardValueSound((int)cards[0].value);
                break;

            case PDKConstants.CardType.PAIR:
                soundKey = GetPairSound((int)cards[0].value);
                break;
            case PDKConstants.CardType.THREE_WITH_NO:
                soundKey = "sanzhang";
                break;
            case PDKConstants.CardType.THREE_WITH_ONE:
                soundKey = "sandaiyi";
                break;
            case PDKConstants.CardType.THREE_WITH_TWO:
                soundKey = "sandaier";
                break;
            case PDKConstants.CardType.FOUR_WITH_TWO:

                break;
            case PDKConstants.CardType.FOUR_WITH_THREE:
                break;
            case PDKConstants.CardType.STRAIGHT:
                soundKey = "shunzi";
                break;
            case PDKConstants.CardType.STRAIGHT_PAIR:
                soundKey = "liandui";
                break;
            case PDKConstants.CardType.PLANE:
                soundKey = "feiji";
                break;
            case PDKConstants.CardType.BOMB:
                // 炸弹: 播放"zhadan"
                soundKey = "zhadan";
                break;

            default:
                // 未知牌型,播放单张的声音
                soundKey = GetCardValueSound((int)cards[0].value);
                break;
        }

        return string.IsNullOrEmpty(soundKey) ? null : basePath + soundKey + ".mp3";
    }

    /// <summary>
    /// 获取卡牌数值对应的音效名称
    /// </summary>
    /// <param name="value">卡牌点数(1-15)</param>
    /// <returns>音效名称</returns>
    private string GetCardValueSound(int value)
    {
        // 【重要说明】返回的是文件名（不含.mp3扩展名），会在GetCardSoundPath中拼接完整路径
        // 确保与AudioKeys.cs中的定义一致，避免打包后找不到资源
        switch (value)
        {
            case 1: return "A";      // A -> pdk/pt/man/card/A.mp3
            case 2: return "2";      // 2 -> pdk/pt/man/card/2.mp3
            case 3: return "3";      // 3 -> pdk/pt/man/card/3.mp3
            case 4: return "4";      // 4 -> pdk/pt/man/card/4.mp3
            case 5: return "5";      // 5 -> pdk/pt/man/card/5.mp3
            case 6: return "6";      // 6 -> pdk/pt/man/card/6.mp3
            case 7: return "7";      // 7 -> pdk/pt/man/card/7.mp3
            case 8: return "8";      // 8 -> pdk/pt/man/card/8.mp3
            case 9: return "9";      // 9 -> pdk/pt/man/card/9.mp3
            case 10: return "10";    // 10 -> pdk/pt/man/card/10.mp3
            case 11: return "J";     // J -> pdk/pt/man/card/J.mp3
            case 12: return "Q";     // Q -> pdk/pt/man/card/Q.mp3
            case 13: return "K";     // K -> pdk/pt/man/card/K.mp3
            case 14: return "A";     // A (某些系统中A可能是14)
            case 15: return "2";     // 2 (某些系统中2可能是15)
            default:
                GF.LogWarning($"[PDK音效] 未知的卡牌点数: {value}，使用默认音效3");
                return "3";     // 默认返回3
        }
    }

    /// <summary>
    /// 获取对子对应的音效名称(双数字)
    /// </summary>
    /// <param name="value">卡牌点数(1-15)</param>
    /// <returns>音效名称(如"22","33","44"等)</returns>
    private string GetPairSound(int value)
    {
        // 【重要说明】返回的是文件名（不含.mp3扩展名），会在GetCardSoundPath中拼接完整路径
        // 确保与AudioKeys.cs中的定义一致，避免打包后找不到资源
        switch (value)
        {
            case 1: return "AA";      // AA -> pdk/pt/man/card/AA.mp3
            case 2: return "22";      // 22 -> pdk/pt/man/card/22.mp3
            case 3: return "33";      // 33 -> pdk/pt/man/card/33.mp3
            case 4: return "44";      // 44 -> pdk/pt/man/card/44.mp3
            case 5: return "55";      // 55 -> pdk/pt/man/card/55.mp3
            case 6: return "66";      // 66 -> pdk/pt/man/card/66.mp3
            case 7: return "77";      // 77 -> pdk/pt/man/card/77.mp3
            case 8: return "88";      // 88 -> pdk/pt/man/card/88.mp3
            case 9: return "99";      // 99 -> pdk/pt/man/card/99.mp3
            case 10: return "1010";   // 1010 -> pdk/pt/man/card/1010.mp3
            case 11: return "JJ";     // JJ -> pdk/pt/man/card/JJ.mp3
            case 12: return "QQ";     // QQ -> pdk/pt/man/card/QQ.mp3
            case 13: return "KK";     // KK -> pdk/pt/man/card/KK.mp3
            case 14: return "AA";     // AA (某些系统中A可能是14)
            case 15: return "22";     // 22 (某些系统中2可能是15)
            default:
                GF.LogWarning($"[PDK音效] 未知的对子点数: {value}，使用默认音效33");
                return "33";     // 默认返回33
        }
    }

    #endregion

    #region 卡牌选择和交互

    /// <summary>
    /// 设置卡牌点击事件
    /// </summary>
    /// <param name="cardComponent">卡牌组件</param>
    public void SetupCardClickEvent(PDKCard cardComponent)
    {
        if (cardComponent == null) return;

        // 清理旧组件并添加EventTrigger
        var eventTrigger = GetOrAddEventTrigger(cardComponent.gameObject);

        // 注册所有交互事件
        AddEventTrigger(eventTrigger, UnityEngine.EventSystems.EventTriggerType.PointerDown,
            (data) => OnCardPointerDown(cardComponent, data));
        AddEventTrigger(eventTrigger, UnityEngine.EventSystems.EventTriggerType.PointerUp,
            (data) => OnCardPointerUp(cardComponent, data));
        AddEventTrigger(eventTrigger, UnityEngine.EventSystems.EventTriggerType.Drag,
            (data) => OnCardDrag(cardComponent, data));
        AddEventTrigger(eventTrigger, UnityEngine.EventSystems.EventTriggerType.PointerEnter,
            (data) => OnCardPointerEnter(cardComponent, data));
    }

    /// <summary>
    /// 设置卡牌的交互状态（启用/禁用点击事件）
    /// </summary>
    private void SetCardInteractable(GameObject cardObject, bool interactable)
    {
        if (cardObject == null) return;

        // 方法1: 通过Image的raycastTarget控制是否接收射线检测
        var image = cardObject.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = interactable;
        }

        // 方法2: 通过EventTrigger的enabled控制事件触发
        var eventTrigger = cardObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger != null)
        {
            eventTrigger.enabled = interactable;
        }
    }

    /// <summary>
    /// 获取或添加EventTrigger组件（清理旧组件）
    /// </summary>
    public UnityEngine.EventSystems.EventTrigger GetOrAddEventTrigger(GameObject target)
    {
        // 移除旧的Button组件
        var button = target.GetComponent<Button>();
        DestroyImmediate(button);

        // 移除并重新创建EventTrigger
        var oldTrigger = target.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        DestroyImmediate(oldTrigger);

        return target.AddComponent<UnityEngine.EventSystems.EventTrigger>();
    }

    /// <summary>
    /// 添加事件触发器
    /// </summary>
    private void AddEventTrigger(UnityEngine.EventSystems.EventTrigger trigger,
        UnityEngine.EventSystems.EventTriggerType eventType,
        UnityEngine.Events.UnityAction<UnityEngine.EventSystems.BaseEventData> callback)
    {
        var entry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }







    /// <summary>
    /// 检查当前选中的牌是否符合规则
    /// 用于实时显示/隐藏出牌按钮的禁用标识
    /// </summary>
    private void CheckSelectedCardsValidity()
    {
        if (selectedCards.Count == 0)
        {
            varChuPaiBtn_NO.SetActive(true);
            return;
        }
        List<PDKCardData> cardDataList = cardHelper.ConvertToCardDataList(selectedCards);
        List<PDKCardData> allHandCardDataList = GetAllHandCards();
        PDKConstants.CardType cardType = PDKRuleChecker.DetectCardType(cardDataList, allHandCardDataList);

        // 根据牌型是否有效来控制显示/隐藏
        bool isInvalid = cardType == PDKConstants.CardType.UNNOWN;

        // 【关键修复】判断是否是首出状态
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        bool isLeading = false;

        // 优先使用服务器协议字段判断
        if (serverMaxDisCardPlayer > 0)
        {
            isLeading = (myPlayerId == serverMaxDisCardPlayer);
        }
        else if (serverLastMaxDiscard > 0)
        {
            isLeading = (myPlayerId == serverLastMaxDiscard);
        }
        else
        {

            if (lastPlayedCards == null)
            {
                isLeading = true;
            }
            else
            {

                isLeading = false;
            }
        }

        if (!isInvalid && !isLeading && lastPlayedCards != null && !lastPlayedCards.IsPass)
        {
            // 检查是否能跟上家的牌（使用从服务器初始化的游戏规则配置）
            bool canPlay = PDKRuleChecker.CanPlayCards(cardDataList, allHandCardDataList, lastPlayedCards, currentGameRules);
            if (!canPlay)
            {
                if (enableDebugLog)
                {

                    int currentWeight = PDKRuleChecker.CalculateWeight(cardDataList, cardType);


                }
            }
        }
        varChuPaiBtn_NO.SetActive(isInvalid);

    }

    /// <summary>
    /// 清空所有选中的卡牌
    /// </summary>
    private void ClearSelectedCards()
    {
        // 【Bug修复】不要使用StopAllCoroutines，这会停止倒计时协程
        // 改为直接设置卡牌位置，不再依赖动画协程

        // 恢复所有选中卡牌的位置（只对未被销毁的卡牌进行操作）
        foreach (var card in selectedCards.Where(c => c != null && c.gameObject != null))
        {
            // 【关键修复】停止正在进行的动画协程
            if (card.selectionCoroutine != null)
            {
                StopCoroutine(card.selectionCoroutine);
                card.selectionCoroutine = null;
            }

            var rectTransform = card.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 直接设置位置，不使用动画，避免协程中访问已销毁对象
                rectTransform.localPosition = card.GetOriginalPosition();
                card.SetSelected(false);
            }
        }

        selectedCards.Clear();
        selectedCardNumbers.Clear();

        // 清空选中后，显示禁用标识（因为没有选中牌算作"无效牌型"）
        varChuPaiBtn_NO.SetActive(true);

    }

    /// <summary>
    /// 处理普通点击事件
    /// </summary>
    /// <param name="card">点击的卡牌</param>
    private void HandleClick(PDKCard card)
    {
        if (card == null) return;

        if (IsCardSelected(card))
        {
            // 卡牌已选中，取消选择
            DeselectCard(card);
        }
        else
        {
            // 卡牌未选中，添加到选择列表
            SelectCard(card);
        }

        if (enableDebugLog)
        {
            int value = GameUtil.GetInstance().GetValue(card.numD);
            int color = GameUtil.GetInstance().GetColor(card.numD);
            string action = IsCardSelected(card) ? "选中" : "取消选中";
        }
    }

    /// <summary>
    /// 清理所有卡牌事件监听
    /// </summary>
    private void CleanupCardEvents()
    {
        if (varHandCard == null) return;

        Transform parent = varHandCard.transform;
        for (int i = 0; i < parent.childCount; i++)
        {
            GameObject cardObj = parent.GetChild(i).gameObject;
            PDKCard cardComponent = cardObj.GetComponent<PDKCard>();
            if (cardComponent != null)
            {
                // 移除所有EventTrigger事件
                var eventTrigger = cardObj.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (eventTrigger != null)
                {
                    eventTrigger.triggers.Clear();
                }

                // 移除Button组件
                var button = cardObj.GetComponent<Button>();
                DestroyImmediate(button);
            }
        }
    }

    #endregion
    #region 出牌区域管理

    /// <summary>
    /// 清空指定出牌区域的所有卡牌
    /// </summary>
    /// <param name="playArea">要清空的出牌区域，null表示清空自己的出牌区域</param>
    private void ClearPlayArea(GameObject playArea = null)
    {
        if (playArea == null) playArea = varPlayArea;
        int childCount = playArea.transform.childCount;
        if (childCount == 0) return;

        // 使用对象池回收
        if (cardPool != null)
        {
            cardPool.RecycleAllCardsInParent(playArea.transform);
        }
        else
        {
            // 降级方案：直接销毁
            foreach (Transform child in playArea.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>玩家0是首出
    /// 根据玩家ID清空出牌区域
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    public void ClearPlayAreaByPlayerId(long playerId)
    {
        if (playerIdToPlayArea.TryGetValue(playerId, out GameObject playArea))
        {
            ClearPlayArea(playArea);
        }
    }

    /// <summary>
    /// 清空所有结算区域的卡牌
    /// </summary>
    private void ClearAllSettlementAreas()
    {
        GameObject[] settlementAreas = { varGameOverCard, varGameOverCard1, varGameOverCard2 };
        foreach (var area in settlementAreas)
        {
            if (area != null)
            {
                foreach (Transform child in area.transform)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// 隐藏所有玩家的庄家标识
    /// </summary>
    private void HideAllBankerIndicators()
    {
        ForEachHeadBg(headBg => SetPlayerBankerById(headBg, 0, -1));
    }

    /// <summary>
    /// 隐藏所有玩家的已就绪标识
    /// </summary>
    private void HideAllReadyIndicators()
    {
        ForEachHeadBg(headBg => headBg.transform.Find("YiJiuXu")?.gameObject.SetActive(false));
    }

    /// <summary>
    /// 清空所有出牌区域（包括自己和其他玩家）
    /// </summary>
    private void ClearAllPlayAreas()
    {
        GameObject[] playAreas = { varPlayArea, varPlayArea1, varPlayArea2 };
        foreach (var area in playAreas)
        {
            if (area != null)
            {
                ClearPlayArea(area);
            }
        }
    }

    /// <summary>
    /// 隐藏所有玩家的结算排名和完成标识
    /// </summary>
    private void HideAllSettlementRankings()
    {
        if (varNextGamePlay == null) return;

        string[] playerPaths = { "player0", "player1", "player2" };
        foreach (var path in playerPaths)
        {
            varNextGamePlay.transform.Find($"{path}/ranking")?.gameObject.SetActive(false);
            varNextGamePlay.transform.Find($"{path}/finish")?.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 重置游戏状态数据（用于开始新一局游戏）
    /// </summary>
    private void ResetGameStateData()
    {
        lastPlayedCards = null;
        serverMaxDisCardPlayer = 0;
        serverLastMaxDiscard = 0;
        isFirstRound = true;
        passedPlayers.Clear();
        hintState = new PDKHintState(); // 重置提示状态
    }

    /// <summary>
    /// 统一清理所有游戏UI
    /// </summary>
    public void ClearAllGameUI()
    {
        ClearAllPlayAreas();
        ClearAllSettlementAreas();
        ClearAllHandCards();

        HideAllBankerIndicators();
        HideAllReadyIndicators();
        HideAllPassIndicators();
        HideAllPlayerClocks();
        HideAllWarningEffects();
        HideAllDoubleIndicators();
        HideAllPlayersCardBack();

        PdkProcedure?.ShowRobBankerUI(false);

        DoubleBtn?.SetActive(false);
        varChuPaiBtns_15?.SetActive(false);
        varBtnReady?.SetActive(false);
        varBtnQuitReady?.SetActive(false);
        varMenu?.SetActive(false);
        varNextGame?.SetActive(false);
        zhaNiao?.SetActive(false);
        ZBdaojishi?.SetActive(false);
        XJdaojishi?.SetActive(false);
        PdkProcedure?.CancelCountdown();

        HidePlayCardTipImage();
        if (currentTipCountdown != null)
        {
            StopCoroutine(currentTipCountdown);
            currentTipCountdown = null;
        }

        ClearAllChatEmojis();
        ClearSelectedCards();
        myHandCards.Clear();
    }

    /// <summary>
    /// 隐藏所有玩家的加倍标识
    /// </summary>
    private void HideAllDoubleIndicators()
    {
        ForEachHeadBg(headBg => headBg.transform.Find("jiabei")?.gameObject.SetActive(false));
    }

    /// <summary>
    /// 清理所有聊天表情和文本
    /// </summary>
    private void ClearAllChatEmojis()
    {
        foreach (var kvp in playerIdToCurrentEmoji)
        {
            if (kvp.Value != null) Destroy(kvp.Value);
        }
        playerIdToCurrentEmoji.Clear();

        foreach (var kvp in playerIdToChatText)
        {
            if (kvp.Value != null)
            {
                kvp.Value.text = "";
                kvp.Value.gameObject.SetActive(false);
            }
        }

        foreach (var kvp in playerIdToChatHideCoroutine)
        {
            if (kvp.Value != null) StopCoroutine(kvp.Value);
        }
        playerIdToChatHideCoroutine.Clear();

        foreach (var kvp in playerIdToVoiceWave)
        {
            kvp.Value?.SetActive(false);
        }
    }

    #region 报警特效管理

    /// <summary>
    /// 显示或隐藏玩家的报警特效
    /// </summary>
    private void ShowPlayerWarningEffect(long playerId, bool show)
    {
        if (playerIdToHeadBg.TryGetValue(playerId, out GameObject headBg))
        {
            Transform baojingEff = headBg.transform.Find("baojingEff");
            if (baojingEff != null)
            {
                baojingEff.gameObject.SetActive(show);
                if (show)
                {
                    // 统一使用特效音效映射，播放“报警”类型音效（pdk/Warning.mp3）
                    PlayEffectSound("报警");
                }
            }
        }
    }

    /// <summary>
    /// 更新玩家的报警特效状态（根据剩余牌数）
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    private void UpdatePlayerWarningEffect(long playerId)
    {
        // 获取玩家剩余牌数
        int cardCount = GetPlayerCardCount(playerId);

        // 只剩一张牌时显示报警特效
        bool shouldShow = (cardCount == 1);
        ShowPlayerWarningEffect(playerId, shouldShow);
    }

    /// <summary>
    /// 隐藏所有玩家的报警特效
    /// </summary>
    private void HideAllWarningEffects()
    {
        foreach (var kvp in playerIdToHeadBg)
        {
            ShowPlayerWarningEffect(kvp.Key, false);
        }
    }

    /// <summary>
    /// 更新所有玩家的报警特效
    /// </summary>
    public void UpdateAllWarningEffects()
    {
        foreach (var kvp in playerIdToHeadBg)
        {
            UpdatePlayerWarningEffect(kvp.Key);
        }
    }
    #endregion
    /// <summary>
    /// 在指定出牌区域显示卡牌（用于显示其他玩家的出牌）
    /// </summary>
    /// <param name="playArea">目标出牌区域</param>
    /// <param name="cardIds">卡牌ID列表（0-51）</param>
    private void DisplayCardsInPlayArea(GameObject playArea, List<int> cardIds)
    {
        if (playArea == null) return;
        if (cardIds == null || cardIds.Count == 0) return;
        for (int i = 0; i < cardIds.Count; i++)
        {
            int cardId = cardIds[i];
            int color = GameUtil.GetInstance().GetColor(cardId);
            int value = GameUtil.GetInstance().GetValue(cardId);
        }
        // 【关键】先对卡牌进行排序，使用与自己打牌相同的排序规则
        List<int> sortedCardIds = cardIds.ToList(); // 创建副本避免修改原列表
        if (cardManager != null)
        {
            sortedCardIds = cardManager.SortCards(sortedCardIds);
        }
        // 对于特殊牌型（3带1、3带2、飞机带等），重新排序显示（主牌在前，带牌在后）
        sortedCardIds = ReorderSpecialCardTypeForDisplay(sortedCardIds);
        // 根据出牌区域决定卡牌缩放比例
        float cardScale = 0.5f; // 默认缩放比例
        // 判断是哪个出牌区域
        if (playArea == varPlayArea)
        {
            cardScale = 0.5f; // 自己的出牌区：0.5倍
        }
        else if (playArea == varPlayArea1)
        {
            cardScale = 0.5f; // 右边玩家出牌区：0.5倍
        }
        else if (playArea == varHeadBg2)
        {
            cardScale = 0.45f; // 左边玩家出牌区：0.45倍（稍小）
        }
        // 使用与自己打牌相同的布局参数（卡牌尺寸和间距）
        float baseCardWidth = PDKUIManager.s_cardWidth;
        float baseCardSpacing = PDKUIManager.s_cardSpacing;
        float baseCardHeight = baseCardWidth * 1.4f; // 扑克牌标准高度
        // 【新增】双排布局逻辑：如果超过8张牌，分成两排显示
        int totalCards = sortedCardIds.Count;
        bool needTwoRows = totalCards > MAX_CARDS_PER_ROW_PLAY_AREA;
        // 计算每排的牌数
        int firstRowCount = needTwoRows ? MAX_CARDS_PER_ROW_PLAY_AREA : totalCards;
        int secondRowCount = needTwoRows ? (totalCards - MAX_CARDS_PER_ROW_PLAY_AREA) : 0;
        // 创建卡牌并显示在出牌区域
        for (int i = 0; i < sortedCardIds.Count; i++)
        {
            int cardId = sortedCardIds[i];
            // 创建卡牌GameObject（使用对象池）
            GameObject cardObj;
            if (cardPool != null)
            {
                cardObj = cardPool.GetCard(playArea.transform, cardId);
            }
            else
            {
                cardObj = Instantiate(varCard, playArea.transform);
            }
            if (cardObj == null)
            {
                continue;
            }
            // 设置RectTransform
            RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 1. 【关键修复】设置锚点为左对齐,从容器最左边开始排列
                rectTransform.anchorMin = new Vector2(0f, 0.5f);  // 左侧中心
                rectTransform.anchorMax = new Vector2(0f, 0.5f);  // 左侧中心
                rectTransform.pivot = new Vector2(0f, 0.5f);      // pivot在卡牌左侧中心
                // 2. 设置卡牌尺寸（使用基础尺寸）
                rectTransform.sizeDelta = new Vector2(baseCardWidth, baseCardHeight);
                // 3. 应用缩放到卡牌本身
                rectTransform.localScale = new Vector3(cardScale, cardScale, 1f);
                // 4. 重置旋转,确保卡牌水平显示
                rectTransform.localRotation = Quaternion.identity;
                // 5. 计算位置（从容器中心点对称排列）
                // 注意:sizeDelta已设为原始尺寸,localScale处理缩放,所以间距直接用基础值
                float scaledWidth = baseCardWidth * cardScale;
                float scaledHeight = baseCardHeight * cardScale;
                float xPos, yPos;
                // 出牌区间距优化:使用缩放后宽度的40%作为间距(更紧凑美观)
                float cardSpacing = scaledWidth * 0.4f;
                // 【新算法】从中心点对称排列
                // 计算总宽度并从中心向两侧排列
                int currentRowCount = (i < firstRowCount) ? firstRowCount : secondRowCount;
                float totalWidth = (currentRowCount - 1) * cardSpacing; // 总占用宽度
                float startOffset = -totalWidth / 2f; // 从中心点开始的偏移量
                if (i < firstRowCount)
                {
                    // 第一排 - 中心对称排列
                    xPos = startOffset + i * cardSpacing;
                    yPos = needTwoRows ? (scaledHeight * 0.25f) : 0; // 如果有第二排，第一排向上偏移

                }
                else
                {
                    // 第二排 - 中心对称排列
                    int secondRowIndex = i - firstRowCount;
                    xPos = startOffset + secondRowIndex * cardSpacing;
                    yPos = -(scaledHeight * 0.25f); // 向下偏移
                }
                rectTransform.anchoredPosition = new Vector2(xPos, yPos);
            }

            // 获取PDKCard组件并验证初始化（对象池已完成Init）
            PDKCard card = cardObj.GetComponent<PDKCard>();
            if (card != null)
            {
                // 对象池获取的卡牌已经初始化，这里再次确保
                if (cardPool == null)
                {
                    card.Init(cardId);
                }

                // 【Bug修复1】确保打出的牌显示为亮色（对象池复用时可能残留灰色）
                Image cardImage = card.GetComponent<Image>();
                if (cardImage != null)
                {
                    cardImage.color = Color.white;
                }

                // 【诊断】验证Init后的结果
                int color = GameUtil.GetInstance().GetColor(cardId);
                int value = GameUtil.GetInstance().GetValue(cardId);
            }

            // 禁用卡牌交互（出牌区的牌不可点击）
            var button = cardObj.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
            }
        }

        if (enableDebugLog)
        {
            string layout = needTwoRows ? $"双排布局（第一排{firstRowCount}张，第二排{secondRowCount}张）" : $"单排布局";
        }
    }



    #region 结算区域卡牌显示

    /// <summary>
    /// 在结算区域显示卡牌（带牌型重排序）
    /// </summary>
    /// <param name="cardArea">结算区域GameObject</param>
    /// <param name="cardIds">卡牌ID列表</param>
    public void DisplayCardsInSettlementArea(GameObject cardArea, List<int> cardIds)
    {
        if (cardArea == null)
        {
            GF.LogWarning("[PDK结算区] 结算区域为null，无法显示卡牌");
            return;
        }

        // 【关键修复】允许空列表，这种情况下只清空区域，不显示卡牌
        if (cardIds == null || cardIds.Count == 0)
        {
            // 清空旧的手牌
            foreach (Transform child in cardArea.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
            return; // 空列表是正常情况（玩家已出完所有牌）
        }


        // 【关键】对卡牌进行排序
        List<int> sortedCardIds = cardIds.ToList();
        if (cardManager != null)
        {
            sortedCardIds = cardManager.SortCards(sortedCardIds);
        }

        // 【新增】对于特殊牌型（3带1、3带2、飞机带等），重新排序显示（主牌在前，带牌在后）
        sortedCardIds = ReorderSpecialCardTypeForDisplay(sortedCardIds);

        // 结算区域使用较大的缩放比例,让卡牌更明显
        float cardScale = 0.5f;

        // 使用标准布局参数
        float baseCardWidth = PDKUIManager.s_cardWidth;
        float baseCardSpacing = PDKUIManager.s_cardSpacing * 0.8f; // 结算区间距更紧凑
        float baseCardHeight = baseCardWidth * 1.4f;

        // 双排布局逻辑
        int totalCards = sortedCardIds.Count;
        bool needTwoRows = totalCards > MAX_CARDS_PER_ROW_SETTLEMENT;

        int firstRowCount = needTwoRows ? MAX_CARDS_PER_ROW_SETTLEMENT : totalCards;
        int secondRowCount = needTwoRows ? (totalCards - MAX_CARDS_PER_ROW_SETTLEMENT) : 0;


        // 创建卡牌（使用对象池）
        for (int i = 0; i < sortedCardIds.Count; i++)
        {
            int cardId = sortedCardIds[i];

            GameObject cardObj = null;
            try
            {
                if (cardPool != null)
                {
                    cardObj = cardPool.GetCard(cardArea.transform, cardId);
                }
                else if (varCard != null)
                {
                    cardObj = Instantiate(varCard, cardArea.transform);
                }
                else
                {
                    GF.LogError($"[PDK结算区] 卡牌预制体为null，无法创建卡牌");
                    continue;
                }
            }
            catch (System.Exception ex)
            {
                GF.LogError($"[PDK结算区] 创建卡牌异常 ID={cardId}: {ex.Message}");
                continue;
            }

            if (cardObj == null)
            {
                GF.LogWarning($"[PDK结算区] 无法创建卡牌ID: {cardId}，跳过该卡牌");
                continue;
            }

            RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 【关键修复】使用左对齐的锚点,让卡牌从容器最左边开始排列
                rectTransform.anchorMin = new Vector2(0f, 0.5f);  // 左侧中心
                rectTransform.anchorMax = new Vector2(0f, 0.5f);  // 左侧中心
                rectTransform.pivot = new Vector2(0f, 0.5f);      // pivot在卡牌左侧中心
                rectTransform.sizeDelta = new Vector2(baseCardWidth, baseCardHeight);
                rectTransform.localScale = new Vector3(cardScale, cardScale, 1f);

                // 【关键】重置旋转,确保卡牌水平显示
                rectTransform.localRotation = Quaternion.identity;

                // 注意:sizeDelta已设为原始尺寸,localScale处理缩放,所以间距直接用基础值
                float scaledWidth = baseCardWidth * cardScale;
                float scaledHeight = baseCardHeight * cardScale;

                // 获取容器宽度,计算起始位置(容器最左边)
                RectTransform containerRect = cardArea.GetComponent<RectTransform>();
                float containerWidth = containerRect != null ? containerRect.rect.width : 470f;
                float startX = -containerWidth / 2f; // 从容器左边界开始

                float xPos, yPos;
                float leftMargin = 5f; // 左边距5像素
                // 结算区间距优化:使用缩放后宽度的25%作为间距(非常紧凑,适合显示剩余手牌)
                float cardSpacing = scaledWidth * 0.25f;

                if (i < firstRowCount)
                {
                    // 第一排 - 从容器最左边开始排列
                    xPos = startX + leftMargin + i * cardSpacing;
                    yPos = needTwoRows ? (scaledHeight * 0.3f) : 0;
                }
                else
                {
                    // 第二排 - 从容器最左边开始排列
                    int secondRowIndex = i - firstRowCount;
                    xPos = startX + leftMargin + secondRowIndex * cardSpacing;
                    yPos = -(scaledHeight * 0.3f);
                }

                rectTransform.anchoredPosition = new Vector2(xPos, yPos);
            }

            PDKCard card = cardObj.GetComponent<PDKCard>();
            if (card != null)
            {
                // 对象池获取的卡牌已经初始化，这里再次确保
                if (cardPool == null)
                {
                    card.Init(cardId);
                }

                // 【Bug修复2】确保结算区的牌显示为亮色（对象池复用时可能残留灰色）
                Image cardImage = card.GetComponent<Image>();
                cardImage.color = Color.white;
            }

            // 禁用卡牌交互
            var button = cardObj.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
            }
        }
    }

    #endregion


    #region 牌型显示排序辅助方法

    /// <summary>
    /// 对于特殊牌型（3带1、3带2、飞机带等），重新排序显示（主牌在前，带牌在后）
    /// </summary>
    private List<int> ReorderSpecialCardTypeForDisplay(List<int> cardIds)
    {
        if (cardIds == null || cardIds.Count < 4) return cardIds;

        var cards = cardIds.Select(id => new PDKCardData(
            (PDKConstants.CardSuit)(id / 100),
            (PDKConstants.CardValue)(id % 100)
        )).ToList();

        var groups = cards.GroupBy(c => c.value)
                         .OrderByDescending(g => g.Count())
                         .ThenByDescending(g => g.Key)
                         .ToList();

        var bombGroups = groups.Where(g => g.Count() == 4).ToList();
        var tripleGroups = groups.Where(g => g.Count() == 3).ToList();

        List<PDKCardData> reorderedCards = new List<PDKCardData>();

        if (bombGroups.Count == 1)
        {
            HandleBombType(cards, groups, bombGroups[0], reorderedCards);
        }
        else if (tripleGroups.Count >= 2 && IsTripleContinuous(tripleGroups))
        {
            HandlePlaneType(cards, groups, tripleGroups, reorderedCards);
        }
        else if (tripleGroups.Count == 1)
        {
            HandleTripleType(cards, groups, tripleGroups[0], reorderedCards);
        }
        else
        {
            return cardIds;
        }
        return reorderedCards.Count == cardIds.Count
            ? reorderedCards.Select(c => c.CardId).ToList()
            : cardIds;
    }

    private void HandleBombType(List<PDKCardData> allCards, List<IGrouping<PDKConstants.CardValue, PDKCardData>> groups,
        IGrouping<PDKConstants.CardValue, PDKCardData> bombGroup, List<PDKCardData> result)
    {
        int kickerCount = allCards.Count - 4;
        if (kickerCount == 0) return; // Pure bomb, no reorder needed

        // Add bomb cards first
        result.AddRange(bombGroup.OrderByDescending(c => c.value).ThenByDescending(c => c.suit));

        // Add kickers
        foreach (var g in groups.Where(g => g.Key != bombGroup.Key).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key))
        {
            result.AddRange(g.OrderByDescending(c => c.value).ThenByDescending(c => c.suit));
        }
    }

    private void HandleTripleType(List<PDKCardData> allCards, List<IGrouping<PDKConstants.CardValue, PDKCardData>> groups,
        IGrouping<PDKConstants.CardValue, PDKCardData> tripleGroup, List<PDKCardData> result)
    {
        int kickerCount = allCards.Count - 3;
        if (kickerCount == 0) return;

        // Add triple cards first
        result.AddRange(tripleGroup.OrderByDescending(c => c.value).ThenByDescending(c => c.suit));

        // Add kickers
        foreach (var g in groups.Where(g => g.Key != tripleGroup.Key).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key))
        {
            result.AddRange(g.OrderByDescending(c => c.value).ThenByDescending(c => c.suit));
        }
    }

    private void HandlePlaneType(List<PDKCardData> allCards, List<IGrouping<PDKConstants.CardValue, PDKCardData>> groups,
        List<IGrouping<PDKConstants.CardValue, PDKCardData>> tripleGroups, List<PDKCardData> result)
    {
        int tripleCardCount = tripleGroups.Count * 3;
        int kickerCount = allCards.Count - tripleCardCount;

        if (kickerCount == 0)
        {
            // Plane without kickers
            result.AddRange(tripleGroups.OrderBy(g => g.Key)
                .SelectMany(g => g.OrderByDescending(c => c.value).ThenByDescending(c => c.suit)));
            return;
        }

        // Add triples first (sorted by value ascending for plane)
        foreach (var group in tripleGroups.OrderBy(g => g.Key))
        {
            result.AddRange(group.OrderByDescending(c => c.value).ThenByDescending(c => c.suit));
        }

        // Add kickers
        var tripleKeys = new HashSet<PDKConstants.CardValue>(tripleGroups.Select(g => g.Key));
        var kickers = groups.Where(g => !tripleKeys.Contains(g.Key))
                           .SelectMany(g => g)
                           .OrderByDescending(c => c.value)
                           .ThenByDescending(c => c.suit);
        result.AddRange(kickers);
    }

    private bool IsTripleContinuous(List<IGrouping<PDKConstants.CardValue, PDKCardData>> triples)
    {
        if (triples.Count < 2) return false;
        var values = triples.Select(g => (int)g.Key).OrderBy(v => v).ToList();
        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] == (int)PDKConstants.CardValue.Two || values[i - 1] == (int)PDKConstants.CardValue.Two)
                return false;
            if (values[i] - values[i - 1] != 1)
                return false;
        }
        return true;
    }

    #endregion

    #region 数据查询与调试工具

    /// <summary>
    /// 根据玩家ID获取玩家名字
    /// </summary>
    private string GetPlayerNameById(long playerId)
    {
        if (PdkProcedure?.EnterRunFastDeskRs?.DeskPlayers == null)
        {
            return $"玩家{playerId}";
        }
        var player = PdkProcedure.EnterRunFastDeskRs.DeskPlayers.FirstOrDefault(p => p.BasePlayer.PlayerId == playerId);
        return player?.BasePlayer.Nick ?? $"玩家{playerId}";
    }

    /// <summary>
    /// 获取手牌信息字符串
    /// </summary>
    private string GetHandCardsInfo()
    {
        int uiCardCount = varHandCard != null ? varHandCard.transform.childCount : 0;

        if (myHandCards == null || myHandCards.Count == 0)
        {
            return $"无手牌 (myHandCards:0张, UI显示:{uiCardCount}张)";
        }

        var cardStrs = myHandCards.Select(c => $"{c.suit}-{c.value}");
        return $"[{string.Join(", ", cardStrs)}] (myHandCards:{myHandCards.Count}张, UI显示:{uiCardCount}张)";
    }


    /// <summary>
    /// 清理游戏状态（解散房间或重新开始时调用）
    /// </summary>
    public void ClearGameState()
    {
        // 【关键修复】重置游戏状态数据（包括 serverLastMaxDiscard 和 serverMaxDisCardPlayer）
        ResetGameStateData();

        // 使用统一清理方法
        ClearAllGameUI();

        // 隐藏出牌按钮
        ShowPlayButtons(false);

        // 隐藏结算面板
        varNextGamePlay?.SetActive(false);

        GF.LogInfo_wl("<color=#00FF00>[清理游戏状态] 已重置所有游戏状态数据</color>");
    }

    #endregion

    #region 语音和聊天功能

    public MJVoicePanel voiceGo;
    public GameObject chatPanelUI;

    // 每个玩家的表情和聊天容器（对应图片中的Emoji和TextChat节点）
    private Dictionary<long, GameObject> playerIdToEmojiContainer = new Dictionary<long, GameObject>();
    private Dictionary<long, Text> playerIdToChatText = new Dictionary<long, Text>();
    private Dictionary<long, GameObject> playerIdToVoiceWave = new Dictionary<long, GameObject>();
    private Dictionary<long, GameObject> playerIdToCurrentEmoji = new Dictionary<long, GameObject>();
    private Dictionary<long, Coroutine> playerIdToChatHideCoroutine = new Dictionary<long, Coroutine>();

    private const float CHAT_TEXT_HIDE_DELAY = 3f;

    /// <summary>
    /// 判断当前玩家是否是旁观者（不在座位上）
    /// </summary>
    /// <returns>true表示旁观者，false表示座位玩家</returns>
    private bool IsSpectator()
    {
        if (PdkProcedure?.EnterRunFastDeskRs?.DeskPlayers == null)
        {
            return true;
        }

        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        return !PdkProcedure.EnterRunFastDeskRs.DeskPlayers.Any(p => p.BasePlayer.PlayerId == myPlayerId);
    }

    /// <summary>
    /// 初始化语音和聊天组件
    /// </summary>
    public void InitVoiceGo()
    {
        voiceGo.gameObject.SetActive(false);
        chatPanel.SetActive(false);

        // 初始化容器映射
        InitChatContainers();

        // GF.LogInfo("[PDK聊天] 聊天系统初始化完成");
    }

    /// <summary>
    /// 初始化聊天容器映射
    /// </summary>
    private void InitChatContainers()
    {
        playerIdToEmojiContainer.Clear();
        playerIdToChatText.Clear();
        playerIdToVoiceWave.Clear();
        playerIdToCurrentEmoji.Clear();
        playerIdToChatHideCoroutine.Clear();

        // 从后端数据中获取玩家列表
        if (PdkProcedure == null || PdkProcedure.EnterRunFastDeskRs == null)
        {
            GF.LogWarning("[PDK聊天] 无法获取玩家数据,稍后初始化");
            return;
        }

        var deskPlayers = PdkProcedure.EnterRunFastDeskRs.DeskPlayers;
        if (deskPlayers == null || deskPlayers.Count == 0)
        {
            GF.LogWarning("[PDK聊天] 桌子上没有玩家");
            return;
        }

        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        int totalPlayerNum = (int)PdkProcedure.EnterRunFastDeskRs.BaseConfig.PlayerNum;


        // 找到自己的位置
        var myPlayer = deskPlayers.FirstOrDefault(p => p.BasePlayer.PlayerId == myPlayerId);
        if (myPlayer == null)
        {
            GF.LogWarning("[PDK聊天] 找不到自己的玩家数据");
            return;
        }

        int myClientPos = 0; // 自己永远在0号位
        Position myServerPos = myPlayer.Pos;

        // 为每个玩家建立映射关系
        foreach (var player in deskPlayers)
        {
            long playerId = player.BasePlayer.PlayerId;
            GameObject headBg = null;

            // 判断是否是自己
            if (playerId == myPlayerId)
            {
                headBg = varHeadBg0;
            }
            else
            {
                int clientPos = Util.GetPosByServerPos_pdk(myServerPos, player.Pos, totalPlayerNum);
                headBg = GetHeadBgByPos(clientPos, myClientPos, totalPlayerNum);
            }

            if (headBg != null)
            {
                InitContainerForHeadBg(headBg, playerId);
            }
            else
            {
                GF.LogError($"[PDK聊天] 玩家{playerId}的headBg为空,无法初始化聊天容器!");
            }
        }

    }

    /// <summary>
    /// 为指定头像框初始化容器
    /// </summary>
    private void InitContainerForHeadBg(GameObject headBg, long playerId)
    {
        if (headBg == null) return;

        // 查找Emoji容器
        Transform emojiTransform = headBg.transform.Find("Emoji");
        if (emojiTransform != null)
        {
            playerIdToEmojiContainer[playerId] = emojiTransform.gameObject;
            emojiTransform.gameObject.SetActive(false);
        }

        // 查找TextChat
        Transform textChatTransform = headBg.transform.Find("TextChat");

        if (textChatTransform != null)
        {
            Transform textTransform = textChatTransform.Find("Text");
            if (textTransform == null)
            {
                GF.LogWarning($"[PDK聊天初始化] 玩家{playerId} TextChat下找不到Text子对象");
            }
            else
            {
                Text chatText = textTransform.GetComponent<Text>();
                if (chatText != null)
                {
                    playerIdToChatText[playerId] = chatText;
                }
                else
                {
                    GF.LogWarning($"[PDK聊天初始化] 玩家{playerId} Text对象上没有Text组件");
                }
            }
            textChatTransform.gameObject.SetActive(false);
        }
        else
        {
            GF.LogWarning($"[PDK聊天初始化] 玩家{playerId} 找不到TextChat对象");
        }

        // 查找VoiceWave
        Transform voiceWaveTransform = headBg.transform.Find("VoiceWave");
        if (voiceWaveTransform != null)
        {
            playerIdToVoiceWave[playerId] = voiceWaveTransform.gameObject;
            voiceWaveTransform.gameObject.SetActive(false);
        }

        // GF.LogInfo($"[PDK聊天] 已初始化玩家 {playerId} 的聊天容器");
    }

    public void OnVoicePress()
    {
        if (pdkProcedure.EnterRunFastDeskRs.BaseConfig.ForbidVoice)
        {
            GF.LogInfo_gsc("禁止语音");
            GF.UI.ShowToast("禁止聊天!");
            return;
        }

        if (IsSpectator())
        {
            GF.UI.ShowToast("观战状态下无法使用语音聊天");
            return;
        }

        // 使用PDK自己的玩家数据
        var deskPlayers = PdkProcedure?.EnterRunFastDeskRs?.DeskPlayers;
        if (deskPlayers == null || deskPlayers.Count == 0)
        {
            GF.LogWarning("[PDK语音] 无法获取玩家列表");
            return;
        }

        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        DeskPlayer selfPlayer = deskPlayers.FirstOrDefault(p => p.BasePlayer.PlayerId == myPlayerId);
        if (selfPlayer == null)
        {
            GF.LogWarning("[PDK语音] 未找到自己的玩家信息");
            return;
        }

        // 显示录音界面并开始录音
        voiceGo.gameObject.SetActive(true);
        voiceGo.StartRecord(selfPlayer.BasePlayer.PlayerId, pdkProcedure.EnterRunFastDeskRs.DeskId);
    }

    public void OnVoiceRelease()
    {
        if (pdkProcedure.EnterRunFastDeskRs.BaseConfig.ForbidVoice)
        {
            return;
        }
        voiceGo.StopRecord();
        voiceGo.gameObject.SetActive(false);
    }

    public void OnVoiceCancel()
    {
        if (pdkProcedure.EnterRunFastDeskRs.BaseConfig.ForbidVoice)
        {
            return;
        }
        voiceGo.CancelRecord();
        voiceGo.gameObject.SetActive(false);

    }
    public void onDragInVisual()
    {
        voiceGo.ShowSend(true);
    }

    public void onDragOutVisual()
    {
        voiceGo.ShowSend(false);
    }

    /// <summary>
    /// 更新桌子玩家列表（当玩家加入或离开时调用）
    /// </summary>

    /// <summary>
    /// 处理聊天消息（由PDKProcedures在接收到Msg_SynDeskChat后调用）
    /// </summary>
    public void HandleDeskChat(Msg_DeskChat deskChat)
    {

        long senderId = deskChat.Sender.PlayerId;


        switch (deskChat.Type)
        {
            case 0: // 文本
                ShowChatText(senderId, deskChat.Chat);
                break;
            case 1: // 快捷语
                ShowChatText(senderId, deskChat.Chat);
                break;
            case 2: // 表情
                ShowEmoji(senderId, deskChat.Chat);
                break;
            case 3: // 语音
                ShowVoiceWave(senderId, deskChat.Chat); // deskChat.Chat 包含语音URL
                break;
            default:
                GF.LogWarning($"[PDK聊天] 未知的消息类型: {deskChat.Type}");
                break;
        }
    }

    /// <summary>
    /// 显示玩家表情
    /// </summary>
    private void ShowEmoji(long playerId, string emojiIndex)
    {


        GameObject emojiContainer = playerIdToEmojiContainer[playerId];

        // 如果该玩家已经有表情在显示，先清理掉
        if (playerIdToCurrentEmoji.ContainsKey(playerId) && playerIdToCurrentEmoji[playerId] != null)
        {
            Destroy(playerIdToCurrentEmoji[playerId]);
            playerIdToCurrentEmoji[playerId] = null;
        }

        // 加载新表情
        GF.UI.LoadPrefab(AssetsPath.GetPrefab("UI/Chat/" + emojiIndex), (gameObject) =>
        {
            if (gameObject == null)
            {
                GF.LogWarning($"[PDK表情] 表情预制体加载失败: {emojiIndex}");
                return;
            }

            GameObject emoji = Instantiate(gameObject as GameObject, emojiContainer.transform);
            var frameAnimator = emoji.GetComponent<FrameAnimator>();
            if (frameAnimator != null)
            {
                frameAnimator.Framerate = 10;
            }
            emoji.transform.localScale = Vector3.one;
            emoji.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20f);
            emojiContainer.SetActive(true);

            // 记录当前表情对象
            playerIdToCurrentEmoji[playerId] = emoji;

            // 动画效果：放大1.5倍后销毁（与麻将一致）
            emoji.transform.DOScale(Vector3.one * 1.5f, 1f).OnComplete(async () =>
            {
                if (emoji == null || emoji.gameObject == null) return;

                Destroy(emoji, 1f);
                await UniTask.Delay(System.TimeSpan.FromSeconds(0.9f));

                // 只有当前记录的表情对象与要销毁的对象一致时，才隐藏容器
                if (playerIdToCurrentEmoji.ContainsKey(playerId) && playerIdToCurrentEmoji[playerId] == emoji)
                {
                    emojiContainer.SetActive(false);
                    playerIdToCurrentEmoji[playerId] = null;
                }
            });

            GF.LogInfo($"[PDK表情] 显示玩家{playerId}的表情: {emojiIndex}");
        });
    }

    /// <summary>
    /// 处理送礼物消息
    /// </summary>
    public void HandleSendGift(long senderId, long receiverId, string giftInfo)
    {
        // 查找发送者和接收者的头像位置
        if (!playerIdToHeadBg.TryGetValue(senderId, out GameObject senderHeadBg) || senderHeadBg == null)
        {
            GF.LogWarning($"[PDK送礼] 找不到发送者{senderId}的HeadBg");
            return;
        }

        if (!playerIdToHeadBg.TryGetValue(receiverId, out GameObject receiverHeadBg) || receiverHeadBg == null)
        {
            GF.LogWarning($"[PDK送礼] 找不到接收者{receiverId}的HeadBg");
            return;
        }

        // 获取起始和目标位置
        Vector3 seatFrom = senderHeadBg.transform.localPosition;
        Vector3 seatTo = receiverHeadBg.transform.localPosition;


        // 加载礼物预制体
        GF.UI.LoadPrefab(AssetsPath.GetPrefab("UI/Chat/" + giftInfo), (gameObject) =>
        {
            if (gameObject == null)
            {
                GF.LogWarning($"[PDK送礼] 礼物预制体加载失败: {giftInfo}");
                return;
            }

            GameObject giftObj = Instantiate(gameObject as GameObject, transform);
            giftObj.transform.localPosition = seatFrom;

            // 根据礼物类型设置缩放
            giftObj.transform.localScale = giftInfo switch
            {
                "2001" => new Vector3(1.8f, 1.8f, 1.8f),
                "2003" => new Vector3(1.8f, 1.8f, 1.8f),
                "2005" => new Vector3(2.2f, 2.2f, 2.2f),
                "2007" => new Vector3(2.2f, 2.2f, 2.2f),
                _ => new Vector3(2f, 2f, 2f),
            };

            // 获取动画组件
            FrameAnimator frameAnimator = giftObj.GetComponent<FrameAnimator>();
            if (frameAnimator != null)
            {
                frameAnimator.Loop = false;
                frameAnimator.Stop();
                frameAnimator.FinishEvent += (giftobj) =>
                {
                    Destroy(giftobj);
                };
            }

            // 移动到目标位置，到达后播放动画和音效
            giftObj.transform.DOLocalMove(seatTo, 0.8f).OnComplete(() =>
            {
                if (frameAnimator != null)
                {
                    frameAnimator.Framerate = giftInfo == "2003" ? 2 : 6;
                    frameAnimator.Play();
                }
                Sound.PlayEffect("face/" + giftInfo + ".mp3");
            });

            GF.LogInfo($"[PDK送礼] 播放礼物动画 - 发送者: {senderId}, 接收者: {receiverId}, 礼物: {giftInfo}");
        });
    }

    /// <summary>
    /// 显示聊天文本
    /// </summary>
    private void ShowChatText(long playerId, string chatStr)
    {
        // 检查字典中是否存在该玩家ID
        if (!playerIdToChatText.ContainsKey(playerId))
        {
            return;
        }

        Text chatText = playerIdToChatText[playerId];
        if (chatText == null)
        {
            return;
        }


        // 激活父对象和Text的GameObject
        chatText.transform.parent.gameObject.SetActive(true);
        chatText.gameObject.SetActive(true);


        // 设置文本内容
        chatText.text = chatStr;


        // 强制刷新布局
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatText.GetComponent<RectTransform>());
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatText.transform.parent.GetComponent<RectTransform>());

        // 获取或添加CanvasGroup组件
        CanvasGroup canvasGroup = chatText.transform.parent.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = chatText.transform.parent.gameObject.AddComponent<CanvasGroup>();
        }

        // 停止之前的DOTween动画
        canvasGroup.DOKill();

        // 渐显效果
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);

        // 如果该玩家已经有隐藏协程在运行，先停止它
        if (playerIdToChatHideCoroutine.ContainsKey(playerId) && playerIdToChatHideCoroutine[playerId] != null)
        {
            StopCoroutine(playerIdToChatHideCoroutine[playerId]);
            playerIdToChatHideCoroutine[playerId] = null;
        }

        // 启动新的隐藏协程
        playerIdToChatHideCoroutine[playerId] = StartCoroutine(HideChatTextAfterDelay(playerId, chatText, CHAT_TEXT_HIDE_DELAY));

        GF.LogInfo($"[PDK聊天] 显示玩家{playerId}的文本: {chatStr}");
    }

    /// <summary>
    /// 延迟隐藏聊天文本
    /// </summary>
    private IEnumerator HideChatTextAfterDelay(long playerId, Text chatText, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (chatText != null && chatText.transform.parent != null)
        {
            CanvasGroup canvasGroup = chatText.transform.parent.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, 0.5f).SetEase(Ease.InQuad).OnComplete(() =>
                {
                    if (chatText != null && chatText.transform.parent != null)
                    {
                        chatText.transform.parent.gameObject.SetActive(false);
                    }
                });
            }
        }

        // 清空协程引用
        if (playerIdToChatHideCoroutine.ContainsKey(playerId))
        {
            playerIdToChatHideCoroutine[playerId] = null;
        }
    }

    /// <summary>
    /// 显示金币变化动画效果（炸弹等金币变化时使用）
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="scoreChange">金币变化值（正数为赢，负数为输）</param>
    public void ShowScoreChangeEffect(long playerId, double scoreChange)
    {
        // 金额为0时不显示特效
        if (scoreChange == 0) return;
        // 查找玩家的头像框
        if (!playerIdToHeadBg.TryGetValue(playerId, out GameObject headBg)) return;
        string effPath = scoreChange >= 0
            ? AssetsPath.GetPrefab("UI/MaJiang/Game/ScoreWinEff")
            : AssetsPath.GetPrefab("UI/MaJiang/Game/ScoreLoseEff");
        // 加载并创建得分特效对象
        GF.UI.LoadPrefab(effPath, (gameObject) =>
        {
            if (gameObject == null) return;
            GameObject prefab = gameObject as GameObject;
            // 在头像框上创建特效（使用EffContainer或直接在headBg上）
            Transform effectContainer = headBg.transform.Find("EffContainer");
            if (effectContainer == null) effectContainer = headBg.transform;
            GameObject effectObj = Instantiate(gameObject as GameObject, effectContainer);
            // 获取 Text 组件并设置分数文本
            Text scoreText = effectObj.GetComponent<Text>();
            if (scoreText == null)
            {
                Destroy(effectObj);
                return;
            }
            // 设置分数文本
            string scoreStr = scoreChange >= 0 ? $"+{scoreChange}" : $"{scoreChange}";
            scoreText.text = scoreStr;
            // 设置初始位置和状态
            RectTransform rectTransform = effectObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 固定位置
                rectTransform.anchoredPosition = Vector2.zero;
                // 初始缩放为较大（盖章效果的起始状态）
                rectTransform.localScale = Vector3.one * 1.5f;
                rectTransform.localRotation = Quaternion.identity;
            }
            effectObj.SetActive(true);
            // DOTween 动画：盖章效果 + 停留 + 向上渐隐
            if (rectTransform != null && scoreText != null)
            {
                Sequence sequence = DOTween.Sequence();
                sequence.SetTarget(effectObj);
                sequence.SetAutoKill(true);

                // 阶段1：盖章效果 - 从大缩小到正常大小 (0.5秒)
                sequence.Append(rectTransform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack));

                // 阶段2：停留显示 (1.5秒)
                sequence.AppendInterval(1.5f);

                // 阶段3：向上移动并渐隐 (0.5秒)
                sequence.Append(rectTransform.DOAnchorPosY(50f, 0.5f).SetEase(Ease.OutQuad));
                sequence.Join(scoreText.DOFade(0f, 0.5f).SetEase(Ease.InQuad));

                // 动画结束后销毁对象
                sequence.OnComplete(() =>
                {
                    if (effectObj != null)
                    {
                        Destroy(effectObj);
                    }
                });
            }
        });
    }

    // 金币飞行动画相关
    private List<GameObject> flyingCoins = new List<GameObject>();
    private const float coinFlyInterval = 0.01f; // 每个金币间隔时间
    private const float coinFlyDuration = 0.5f;  // 金币飞行时长
    private const int coinFlyCount = 10;         // 每次飞行的金币数量

    /// <summary>
    /// 创建金币飞行动画（从输分玩家飞向赢分玩家）
    /// 参考NNGamePanel实现
    /// </summary>
    /// <param name="losers">输分玩家列表（playerId, 输的分数）</param>
    /// <param name="winners">赢分玩家列表（playerId, 赢的分数）</param>
    public void PlayCoinFlyAnimation(List<(long playerId, double score)> losers, List<(long playerId, double score)> winners)
    {
        if (losers == null || winners == null || losers.Count == 0 || winners.Count == 0)
        {
            return;
        }
        // 启动金币飞行协程
        StartCoroutine(CoinFlyAnimation(losers, winners));
    }

    /// <summary>
    /// 金币飞行动画协程（参考NNGamePanel）
    /// </summary>
    private IEnumerator CoinFlyAnimation(List<(long playerId, double score)> losers, List<(long playerId, double score)> winners)
    {
        // 清理之前的金币
        foreach (GameObject coin in flyingCoins)
        {
            if (coin != null) Destroy(coin);
        }
        flyingCoins.Clear();

        // 播放音效
        Sound.PlayEffect("niuniu/win.mp3");

        // 从输家飞向赢家
        for (int k = 0; k < coinFlyCount; k++)
        {
            foreach (var loser in losers)
            {
                if (!playerIdToHeadBg.TryGetValue(loser.playerId, out GameObject loserHeadBg) || loserHeadBg == null)
                    continue;

                foreach (var winner in winners)
                {
                    if (!playerIdToHeadBg.TryGetValue(winner.playerId, out GameObject winnerHeadBg) || winnerHeadBg == null)
                        continue;

                    // 创建金币实例
                    GameObject coin = Instantiate(varCoin, transform);
                    coin.SetActive(true);
                    coin.transform.position = loserHeadBg.transform.position;
                    flyingCoins.Add(coin);

                    // 使用DOTween移动金币
                    coin.transform.DOMove(winnerHeadBg.transform.position, coinFlyDuration).OnComplete(() =>
                    {
                        if (coin != null) coin.SetActive(false);
                    });
                }
            }
            yield return new WaitForSeconds(coinFlyInterval);
        }

        // 等待金币飞行完成
        yield return new WaitForSeconds(coinFlyDuration);

        // 清理所有金币
        foreach (GameObject coin in flyingCoins)
        {
            if (coin != null) Destroy(coin);
        }
        flyingCoins.Clear();

        GF.LogInfo($"[金币飞行] 动画完成: {losers.Count}个输家 -> {winners.Count}个赢家");

    }

    /// <summary>
    /// 显示语音波浪动画
    /// </summary>
    private void ShowVoiceWave(long playerId, string voiceUrl = "")
    {
        if (!playerIdToVoiceWave.ContainsKey(playerId)) return;

        GameObject voiceWave = playerIdToVoiceWave[playerId];
        if (voiceWave != null)
        {
            voiceWave.SetActive(true);
            // GF.LogInfo($"[PDK聊天] 显示玩家{playerId}的语音波浪");
            // 播放语音(如果有URL)
            if (!string.IsNullOrEmpty(voiceUrl))
            {
                StartCoroutine(PlayVoiceAudio(playerId, voiceUrl));
            }
            else
            {
                // 如果没有URL,3秒后自动隐藏
                StartCoroutine(HideVoiceWaveAfterDelay(playerId, 3f));
            }
        }
    }

    /// <summary>
    /// 播放语音音频
    /// </summary>
    private IEnumerator PlayVoiceAudio(long playerId, string voiceUrl)
    {
        if (PdkProcedure == null || PdkProcedure.EnterRunFastDeskRs == null)
        {
            GF.LogWarning("[PDK聊天] 无法获取桌子信息");
            yield break;
        }

        long deskId = PdkProcedure.EnterRunFastDeskRs.DeskId;
        string downloadUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}/voice/{deskId}/";

        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(downloadUrl + voiceUrl, AudioType.WAV))
        {
            request.certificateHandler = new BypassCertificate();
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError ||
                request.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError)
            {
                GF.LogError($"[PDK聊天] 下载语音失败: {request.error}");
                // 失败后也要隐藏波浪
                if (playerIdToVoiceWave.ContainsKey(playerId) && playerIdToVoiceWave[playerId] != null)
                {
                    playerIdToVoiceWave[playerId].SetActive(false);
                }
                yield break;
            }

            AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(request);
            if (clip != null)
            {
                // GF.LogInfo($"[PDK聊天] 开始播放语音: {voiceUrl}");
                SoundManager.Instance.PlaySFX(clip, 0, () =>
                {
                    // 播放完成后隐藏波浪
                    if (playerIdToVoiceWave.ContainsKey(playerId) && playerIdToVoiceWave[playerId] != null)
                    {
                        playerIdToVoiceWave[playerId].SetActive(false);
                    }
                    // GF.LogInfo($"[PDK聊天] 语音播放完成");
                });
            }
            else
            {
                GF.LogError("[PDK聊天] 无法获取AudioClip");
                if (playerIdToVoiceWave.ContainsKey(playerId) && playerIdToVoiceWave[playerId] != null)
                {
                    playerIdToVoiceWave[playerId].SetActive(false);
                }
            }
        }
    }
    /// <summary>
    /// 延迟隐藏语音波浪
    /// </summary>
    private IEnumerator HideVoiceWaveAfterDelay(long playerId, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (playerIdToVoiceWave.ContainsKey(playerId) && playerIdToVoiceWave[playerId] != null)
        {
            playerIdToVoiceWave[playerId].SetActive(false);
        }
    }
    /// <summary>
    /// 聊天按钮点击（切换聊天面板显示）
    /// </summary>
    private void OnChatButtonClick()
    {
        if (chatPanelUI != null)
        {
            chatPanelUI.SetActive(!chatPanelUI.activeSelf);
        }
    }

    #endregion

    //点击生成测试牌 
    //在指定区域生成测试牌
    #endregion
    //
}

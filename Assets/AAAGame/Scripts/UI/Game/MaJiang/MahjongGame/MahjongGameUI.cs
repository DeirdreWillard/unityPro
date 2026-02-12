using System.Collections.Generic;
using System.Linq;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityGameFramework.Runtime;
using Google.Protobuf;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class MahjongGameUI : UIFormBase
{
    public MJGameProcedure mj_procedure;
    public DefaultMahjongRule currentRule;
    public BaseMahjongGameManager baseMahjongGameManager;

    #region 规则判断属性（统一从 Rule 层获取）

    /// <summary>
    /// 是否是仙桃晃晃规则
    /// </summary>
    public bool IsXianTaoRule => currentRule?.IsXianTaoHuangHuang ?? false;

    /// <summary>
    /// 是否是卡五星规则
    /// </summary>
    public bool IsKaWuXingRule => currentRule?.IsKaWuXing ?? false;

    /// <summary>
    /// 是否是血流成河规则
    /// </summary>
    public bool IsXueLiuRule => currentRule?.IsXueLiuChengHe ?? false;

    /// <summary>
    /// 是否是土豪必掷模式（仙桃晃晃特有）
    /// </summary>
    public bool IsTuHaoBiZhi => currentRule?.IsTuHaoBiZhi() ?? false;

    /// <summary>
    /// 是否支持换三张
    /// </summary>
    public bool HasChangeCard => currentRule?.HasChangeCard ?? false;

    /// <summary>
    /// 是否支持甩牌
    /// </summary>
    public bool HasThrowCard => currentRule?.HasThrowCard ?? false;

    /// <summary>
    /// 是否支持亮牌
    /// </summary>
    public bool HasLiangPai => currentRule?.HasLiangPai ?? false;

    /// <summary>
    /// 是否支持飘分
    /// </summary>
    public bool HasPiao => currentRule?.HasPiao ?? false;

    /// <summary>
    /// 是否需要显示飘分选择面板
    /// </summary>
    public bool NeedShowPiaoPanel => currentRule?.NeedShowPiaoPanel() ?? false;

    /// <summary>
    /// 获取固定飘分值（-1表示需要玩家选择）
    /// </summary>
    public int FixedPiaoValue => currentRule?.GetFixedPiaoValue() ?? 0;

    /// <summary>
    /// 获取飘分选项数量
    /// </summary>
    public int PiaoOptionCount => currentRule?.GetPiaoOptionCount() ?? 3;

    /// <summary>
    /// 获取锁牌阈值
    /// </summary>
    public int LockCardThreshold => currentRule?.GetLockCardThreshold() ?? 0;

    #endregion

    // 对象池相关 - 使用 GameObjectPool 替代 ItemController
    private GameObjectPool mjCard2DPool = new GameObjectPool();
    private GameObjectPool mjCardKongPool = new GameObjectPool();

    // 记录已创建的手牌对象，用于回收
    private Dictionary<int, List<GameObject>> playerHandObjects = new Dictionary<int, List<GameObject>>();

    // 麻将牌预制体
    [Header("===== 麻将牌预制体 =====")]
    public GameObject mjCard2DPrefab;       // 2D麻将牌预制体（用于结算面板和2D视图）
    public GameObject mjCardKongPrefab;     // 空位预制体（用于结算面板显示空位）

    public GameObject mahjongPanel_2D;
    public GameObject roomInfo;
    public GameObject menu;
    public GameObject ready;
    public GameObject quitReady;
    public GameObject gps;
    public Text DeskIDText;
    public Text DeskNumText;

    public GameObject TuoGuanGo;     // 托管界面

    // 自己准备状态
    bool isReady = false;
    private Text readyCountdownText; // 准备按钮倒计时文本

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        if (Sound.IsMusicEnabled() && !Sound.IsMusicPlaying())
            PlayBgMusic();
    }

    public void PlayBgMusic()
    {
        Sound.PlayMusic(AudioKeys.MJ_MUSIC_AUDIO_PLAYINGINGAME);
    }

    public void InitPanel(DefaultMahjongRule mahjongRule, Msg_EnterMJDeskRs enterData)
    {
        mj_procedure = GF.Procedure.CurrentProcedure as MJGameProcedure;
        baseMahjongGameManager = mj_procedure?.GetCurrentBaseMahjongGameManager();
        currentRule = mahjongRule;

        // 初始化测试功能
        transform.Find("TestUI").GetComponent<MahjongMachineTest>().TestMahjongFuncInterface(false);

        // 初始化对象池
        InitializeObjectPools();

        // 初始化胡牌按钮
        InitHuPaiBtn();

        // 初始化2D系统
        Initialize2DUI();

        // 初始化2D操作按钮
        Initialize2DOperationButtons();

        // 初始化所有麻将设置（牌样式、视图模式、语音等）
        MahjongSettings.InitializeAll();

        // 初始化语音功能
        InitVoiceGo();

        // 重置面板显示
        commonPanel.transform.Find("DismissDeskBtn").gameObject.SetActive(Util.IsMySelf(mj_procedure.enterMJDeskRs.BaseInfo.Creator.PlayerId));
        endGamePanel.SetActive(false);
        menu.SetActive(false);
        chatPanel.SetActive(false);
        voiceGo.gameObject.SetActive(false);
        gps.SetActive(false);
        TuoGuanGo?.SetActive(false); // 托管面板初始化隐藏
        //kwx - 初始化隐藏所有卡五星相关面板
        PiaoPanel?.SetActive(false);
        TouZiAniPanel?.SetActive(false);
        HuanPaiAniPanel?.SetActive(false);
        XuanPaiPanel?.SetActive(false);
        HuanPaiInfoPanel?.SetActive(false);
        LiangMaPai?.SetActive(false);
        DissOutCardTips?.SetActive(false);
        LiangPaiPanel?.SetActive(false);
        XuanAnPuPaiPanel?.SetActive(false);
        // 血流 - 初始化隐藏所有血流相关面板
        GameEnd_XL?.SetActive(false);
        XuanShuaiPaiPanel?.SetActive(false);
        // 隐藏选牌中/选牌结束状态
        if (XuanPaiZhongs != null)
        {
            foreach (var go in XuanPaiZhongs)
                go?.SetActive(false);
        }
        if (XuanPaiEnds != null)
        {
            foreach (var go in XuanPaiEnds)
                go?.SetActive(false);
        }
        // 血流 - 隐藏甩牌状态
        if (ShuaiPaiZhongs != null)
        {
            foreach (var go in ShuaiPaiZhongs)
                go?.SetActive(false);
        }

        // 初始化2D房间信息显示
        Update2DRoomInfo();

        // 根据麻将类型 动态加载不同类型图片
        string spriteName = string.Empty;
        if (IsKaWuXingRule) spriteName = "kwx";
        else if (IsXueLiuRule) spriteName = "xlch";
        else if (IsXianTaoRule) spriteName = "xthh";

        if (!string.IsNullOrEmpty(spriteName))
        {
            gameType.SetSprite($"MJGame/MJGameCommon/{spriteName}.png", true);
            gameType.gameObject.SetActive(true);
        }
        else
        {
            gameType.gameObject.SetActive(false);
        }

        roomInfo.transform.Find("Title").GetComponent<Text>().text = mj_procedure.enterMJDeskRs.BaseInfo.BaseConfig.DeskName;
        // 动态房间规则描述（根据不同麻将规则）
        var descText = roomInfo.transform.Find("Bg/Scroll View/TxtContent").GetComponent<Text>();
        descText.text = MahjongRuleFactory.BuildRoomDescription(enterData.BaseInfo);
        DeskIDText.text = $"房号: <color=DCDA95>{mj_procedure.enterMJDeskRs.BaseInfo.DeskId}</color>";
        endGamePanel.transform.Find("BaseScoreBg/BaseScore").GetComponent<Text>().text = $"底分: {mj_procedure.enterMJDeskRs.BaseInfo.BaseConfig.BaseCoin}";
        gps.SetActive(mj_procedure.enterMJDeskRs.BaseInfo.BaseConfig.GpsLimit);

        // 检查重连数据中是否有解散状态，如果有则恢复解散面板
        if (mj_procedure.enterMJDeskRs.DismissState && mj_procedure.enterMJDeskRs.AgreeDismissTime > 0)
        {
            Msg_SynMJDismiss ack = new Msg_SynMJDismiss
            {
                AgreeDismissTime = mj_procedure.enterMJDeskRs.AgreeDismissTime,
                PlayerId = mj_procedure.enterMJDeskRs.AgreeDismissPlayer.FirstOrDefault()
            };
            Function_Msg_SynMJDismiss(ack);
        }

        // 检查是否为赖子玩法，如果是则显示赖子信息界面
        if (currentRule != null && currentRule.HasLaiZi())
        {
            ShowLaiZiInfoPanel();

            // 如果不是等待状态（说明是游戏中重连），且有赖子数据，则直接显示赖子牌
            if (mj_procedure.enterMJDeskRs.CkState != MJState.MjWait &&
                (enterData.PiaoLai > 0 || enterData.Laizi > 0))
            {
                MahjongFaceValue piaoLaizi = enterData.PiaoLai > 0
                    ? Util.ConvertServerIntToFaceValue(enterData.PiaoLai)
                    : MahjongFaceValue.MJ_UNKNOWN;

                MahjongFaceValue laizi = enterData.Laizi > 0
                    ? Util.ConvertServerIntToFaceValue(enterData.Laizi)
                    : MahjongFaceValue.MJ_UNKNOWN;

                GF.LogInfo_gsc("赖子检查", $"游戏中重连，显示赖子: 飘癞{piaoLaizi}, 赖子{laizi}");
                ShowLaiZiCardsImmediate(piaoLaizi, laizi);
            }
        }

        // 安全获取当前玩家的准备状态
        var selfPlayer = baseMahjongGameManager?.GetDeskPlayer(0);
        isReady = selfPlayer != null ? selfPlayer.Ready : false;

        // 检查自己是否处于托管状态，如果是则显示托管取消面板
        if (selfPlayer != null)
            ShowTuoGuanGo(selfPlayer.Auto == 1);

        if (mj_procedure.enterMJDeskRs.CkState == MJState.MjWait)
        {
            roomInfo.SetActive(true);
            UpdateReady();
        }
        else
        {
            roomInfo.SetActive(false);
            ClearReadyState();
        }
        mahjongPanel_2D.SetActive(true);
    }

    public override void OnClickClose()
    {
        OnButtonClick("Quit");
    }

    public async void OnButtonClick(string btId)
    {
        if (Util.IsClickLocked()) return;
        Sound.PlayEffect(AudioKeys.SOUND_BTN);
        switch (btId)
        {
            case "GameInfoBtn":
                roomInfo.SetActive(!roomInfo.activeSelf);
                break;
            case "BtnMessage":
                if (IsSpectator())
                {
                    GF.UI.ShowToast("观战状态下无法使用聊天");
                    return;
                }
                chatPanel.SetActive(!chatPanel.activeSelf);
                break;
            case "BtnMenu":
                menu.SetActive(!menu.activeSelf);
                break;
            case "BtnGPS":
                break;
            case "BtnWatch":
                break;
            case "BtnRoom":
                break;
            case "BtnMV":
                break;
            case "BtnQiFu":
                if (IsSpectator())
                {
                    GF.UI.ShowToast("观战状态下无法使用祈福功能");
                    return;
                }
                ShowBlessingUI();
                break;
            case "Ready":
                if (!isReady)
                {
                    // 停止倒计时
                    CoroutineRunner.Instance.StopCountdown(ready);
                    mj_procedure.Send_MJPlayerReadyRq(true);
                }
                break;
            case "QuitReady":
                if (isReady)
                {
                    // 停止倒计时
                    CoroutineRunner.Instance.StopCountdown(ready);
                    mj_procedure.Send_MJPlayerReadyRq(false);
                }
                break;
            case "Quit":
                if (IsSpectator() || mj_procedure.enterMJDeskRs.CkState == MJState.MjWait)
                {
                    Util.GetInstance().OpenMJConfirmationDialog("退出房间", "确定要退出房间吗?", () =>
                {
                    menu.SetActive(false);
                    mj_procedure.Send_LeaveDeskRq();
                });
                }
                else
                {
                    GF.UI.ShowToast("游戏进行中，无法退出房间");

                    //     Util.GetInstance().OpenMJConfirmationDialog("解散房间", "确定要申请解散房间吗?", () =>
                    // {
                    //     menu.SetActive(false);
                    //     mj_procedure.Send_Msg_ApplyMJDismissRq();
                    // });
                }
                break;
            case "Setting":
                ShowSettingPanel();
                menu.SetActive(false);
                break;
            case "Next":
                HideGameEndPanel();
                mj_procedure.Send_MJPlayerReadyRq(true);
                break;
            case "Record":
                ShowGameRecordPanel();
                break;
            case "DissAgreeYes":
                mj_procedure.Send_Msg_ProcessDismissRq(true);
                break;
            case "DissAgreeNo":
                mj_procedure.Send_Msg_ProcessDismissRq(false);
                break;
            case "BtnHuanPaiInfo":
                //显示换牌结果按钮
                break;
            case "Piao0":
                OnPiaoButtonClick(0);
                break;
            case "Piao1":
                OnPiaoButtonClick(1);
                break;
            case "Piao2":
                OnPiaoButtonClick(2);
                break;
            case "Piao3":
                OnPiaoButtonClick(3);
                break;
            case "Piao4":
                OnPiaoButtonClick(4);
                break;
            case "Piao5":
                OnPiaoButtonClick(5);
                break;
            case "BtnXuanPai":
                //选择换牌完成请求
                OnConfirmChangeCard();
                break;
            // 血流成河
            case "BtnShuaiPai":
                // 确认甩牌
                OnConfirmThrowCard();
                break;
            case "BtnBuShuai":
                // 不甩牌
                OnSkipThrowCard();
                break;
            case "XL_Next":
                // 血流下一局
                OnXueLiuNextClick();
                break;
            case "XL_Record":
                // 血流战绩
                OnXueLiuRecordClick();
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

    public async void ShowBlessingUI()
    {
        if (IsSpectator())
        {
            GF.LogWarning($"[麻将游戏界面] 打开祈福界面失败：观战状态无法使用祈福功能");
            return;
        }
        //如果有blessing界面并是显示激活状态就不打开了
        if (GF.UI.HasUIForm(UIViews.Blessing))
        {
            GF.LogInfo($"[麻将游戏界面] 祈福界面已打开，跳过重复打开");
            return;
        }

        var uiParams = UIParams.Create();
        uiParams.Set<VarVector3>("targetPosition", seats2D[0].PlayerAvatar.transform.position);
        uiParams.Set<VarBoolean>("isLeft", true);
        await GF.UI.OpenUIFormAwait(UIViews.Blessing, uiParams);
    }

    public async void ShowBlessingUI_other(SynPrayRs synPrayRs)
    {
        //如果有blessing界面并是显示激活状态就不打开了
        if (GF.UI.HasUIForm(UIViews.Blessing))
        {
            GF.LogInfo($"[麻将游戏界面] 祈福界面已打开，跳过他人祈福通知");
            return;
        }

        Vector3 targetPos;
        //根据synPrayRs.PlayerId找到对应座位
        int seatIdx = baseMahjongGameManager.GetSeatIndexByPlayerId(synPrayRs.PlayerId);
        if (seatIdx < 0 || seatIdx >= 4)
        {
            targetPos = seats2D[0].PlayerAvatar.transform.position;
        }
        else
        {
            targetPos = GetSeat(seatIdx).PlayerAvatar.transform.position;
        }
        var uiParams = UIParams.Create();
        uiParams.Set<VarVector3>("targetPosition", targetPos);
        uiParams.Set<VarBoolean>("isLeft", seatIdx < 2);
        uiParams.Set<VarByteArray>("synPrayRs", synPrayRs.ToByteArray());
        await GF.UI.OpenUIFormAwait(UIViews.Blessing, uiParams);
    }

    public void SetReadyState(int seatIdx, bool readyState)
    {
        // 2D模式: 更新指定座位的准备状态
        Set2DReadyState(seatIdx, readyState);

        // 本地玩家(座位0)额外更新准备按钮
        if (seatIdx == 0)
        {
            isReady = readyState;
            UpdateReady();
        }
    }

    public void UpdateReady()
    {
        //旁观隐藏准备按钮
        if (IsSpectator())
        {
            ready.SetActive(false);
            quitReady.SetActive(false);
            return;
        }

        ready.SetActive(!isReady);
        quitReady.SetActive(isReady);

        // 获取准备按钮的倒计时文本组件
        if (readyCountdownText == null)
        {
            readyCountdownText = ready.transform.Find("Text").GetComponent<Text>();
        }

        // 如果未准备，启动15秒倒计时
        if (!isReady && readyCountdownText != null)
        {
            // 停止之前的倒计时（如果有）
            CoroutineRunner.Instance.StopCountdown(ready);

            // 启动新的15秒倒计时
            CoroutineRunner.Instance.StartCountdown(15, readyCountdownText, ready, null, "准备({0}s)");
        }
    }

    public void ClearReadyState()
    {
        isReady = false;

        // 停止准备倒计时
        CoroutineRunner.Instance.StopCountdown(ready);

        ready.SetActive(false);
        quitReady.SetActive(false);
        // 清空所有座位的牌显示
        for (int i = 0; i < 4; i++)
        {
            GetSeat(i)?.SetReadyState(false);
        }
    }

    /// <summary>
    /// 显示或隐藏托管取消面板
    /// </summary>
    /// <param name="show">是否显示</param>
    public void ShowTuoGuanGo(bool show)
    {
        TuoGuanGo.SetActive(show);
        GF.LogInfo($"[托管面板] {(show ? "显示" : "隐藏")}托管取消面板");
    }

    /// <summary>
    /// 初始化对象池
    /// </summary>
    private void InitializeObjectPools()
    {
        // 创建对象池 - 增加对象池大小以支持4个玩家的手牌显示
        if (mjCard2DPrefab != null)
        {
            mjCard2DPool.CreatePool(mjCard2DPrefab, 80, endGamePanel.transform);  // 每个玩家最多约20张牌，4个玩家需要80个对象
        }
        else
        {
            GF.LogError("[对象池] mjCard2DPrefab为空，无法创建麻将牌对象池");
        }

        if (mjCardKongPrefab != null)
        {
            mjCardKongPool.CreatePool(mjCardKongPrefab, 20, endGamePanel.transform);  // 每个玩家最多5个空位，4个玩家需要20个对象
        }
        else
        {
            GF.LogError("[对象池] mjCardKongPrefab为空，无法创建空位对象池");
        }

        // 初始化视图切换功能
        InitializeViewSwitch();
    }

    /// <summary>
    /// 初始化视图切换功能 (已废弃 - 仅保留2D模式)
    /// </summary>
    private void InitializeViewSwitch()
    {
        // 固定为2D视图模式,不再需要切换功能
        GF.LogInfo_gsc("[视图初始化] 当前视图模式: 2D (固定)");
    }

    /// <summary>
    /// 回收玩家的手牌对象到对象池
    /// </summary>
    /// <param name="playerIndex">玩家索引</param>
    private void RecyclePlayerHandObjects(int playerIndex)
    {
        if (playerHandObjects.ContainsKey(playerIndex))
        {
            foreach (GameObject obj in playerHandObjects[playerIndex])
            {
                if (obj != null)
                {
                    // 判断对象类型并回收到对应的对象池
                    if (obj.GetComponent<MjCard_2D>() != null)
                    {
                        mjCard2DPool.PushGameObject(obj);
                    }
                    else
                    {
                        mjCardKongPool.PushGameObject(obj);
                    }
                }
            }
            playerHandObjects[playerIndex].Clear();
        }
        else
        {
            playerHandObjects[playerIndex] = new List<GameObject>();
        }
    }

    /// <summary>
    /// 创建玩家的手牌对象
    /// </summary>
    /// <param name="playerIndex">玩家索引</param>
    /// <param name="handContent">手牌容器</param>
    /// <param name="handCards">手牌数据</param>
    /// <param name="separateLastCard">单独显示的最后一张牌（0表示不单独显示）</param>
    private void CreatePlayerHandObjects(int playerIndex, GameObject handContent, List<int> handCards, int separateLastCard = 0)
    {
        if (!playerHandObjects.ContainsKey(playerIndex))
        {
            playerHandObjects[playerIndex] = new List<GameObject>();
        }

        foreach (var card in handCards)
        {
            GameObject cardObj;
            if (card == -1)
            {
                // 创建空位对象
                cardObj = mjCardKongPool.PopGameObject();
                cardObj.SetActive(true);
                cardObj.transform.SetParent(handContent.transform);
                cardObj.transform.localScale = Vector3.one;
            }
            else
            {
                // 创建麻将牌对象
                cardObj = mjCard2DPool.PopGameObject();
                cardObj.SetActive(true);
                cardObj.transform.SetParent(handContent.transform);
                cardObj.transform.localScale = Vector3.one;

                // 判断是否为赖子并设置相应的标记
                MahjongFaceValue cardFaceValue = Util.ConvertServerIntToFaceValue(card);
                bool isLaizi = baseMahjongGameManager.IsLaiZiCard(cardFaceValue);

                cardObj.GetComponent<MjCard_2D>().SetCardValue(card, isLaizi);
            }

            // 记录创建的对象，用于后续回收
            playerHandObjects[playerIndex].Add(cardObj);
        }

        // 如果有单独显示的最后一张牌，添加空位分隔符后再添加该牌
        if (separateLastCard > 0)
        {
            // 添加空位分隔符
            GameObject kongObj = mjCardKongPool.PopGameObject();
            kongObj.SetActive(true);
            kongObj.transform.SetParent(handContent.transform);
            kongObj.transform.localScale = Vector3.one;
            playerHandObjects[playerIndex].Add(kongObj);

            // 添加单独显示的最后一张牌
            GameObject lastCardObj = mjCard2DPool.PopGameObject();
            lastCardObj.SetActive(true);
            lastCardObj.transform.SetParent(handContent.transform);
            lastCardObj.transform.localScale = Vector3.one;

            MahjongFaceValue lastCardFaceValue = Util.ConvertServerIntToFaceValue(separateLastCard);
            bool isLastCardLaizi = baseMahjongGameManager.IsLaiZiCard(lastCardFaceValue);
            lastCardObj.GetComponent<MjCard_2D>().SetCardValue(separateLastCard, isLastCardLaizi);

            playerHandObjects[playerIndex].Add(lastCardObj);
        }
    }

    #region 单局结算
    public GameObject endGamePanel;
    public GameObject recordBtn;
    public List<GameObject> winStates;
    public Text endGameCountDownText;
    public List<GameObject> playerItems;

    public void ShowGameEndPanel(Msg_Hu msg_Hu, List<DeskPlayer> players, int laiZi)
    {
        ShowTuoGuanGo(false); // 隐藏托管取消面板
        if (msg_Hu == null) return;

        bool isLiuJu = msg_Hu.LiuJu;
        MahjongFaceValue winCard = isLiuJu ? MahjongFaceValue.MJ_UNKNOWN : Util.ConvertServerIntToFaceValue(msg_Hu.WinCard);

        if (!isLiuJu)
        {
            Sound.PlayEffect(Util.IsMySelf(msg_Hu.WinPlayer) ? AudioKeys.MJ_EFFECT_AUDIO_GAME_WIN : AudioKeys.MJ_EFFECT_AUDIO_GAME_LOST);
        }
        winStates[0].SetActive(!isLiuJu && Util.IsMySelf(msg_Hu.WinPlayer));
        winStates[1].SetActive(!isLiuJu && !Util.IsMySelf(msg_Hu.WinPlayer));
        winStates[2].SetActive(isLiuJu);
        endGamePanel.transform.Find("TimeBg/Time").GetComponent<Text>().text = Util.MillisecondsToDateString(Util.GetServerTime(), "yyyy-MM-dd HH:mm");

        var laiziInfoObj = endGamePanel.transform.Find("LaiziObj");
        if (laiZi > 0)
        {
            // 显示赖子信息
            laiziInfoObj.Find("Laizi").GetComponent<MjCard_2D>().SetCardValue(laiZi, true);
            var weiCard = laiziInfoObj.Find("Wei");
            if (msg_Hu.LastCard == 0)
            {
                weiCard.gameObject.SetActive(false);
            }
            else
            {
                weiCard.GetComponent<MjCard_2D>().SetCardValue(msg_Hu.LastCard, msg_Hu.LastCard == laiZi);
                weiCard.gameObject.SetActive(true);
            }
            laiziInfoObj.gameObject.SetActive(true);
        }
        else
        {
            // 隐藏赖子信息
            laiziInfoObj.gameObject.SetActive(false);
        }

        // 判断是否是自摸或杠开（WinWay == 1 表示自摸，WinWay == 3 表示杠开）
        bool isZimoOrGangKai = (msg_Hu.WinWay == 1 || msg_Hu.WinWay == 3);

        for (int i = 0; i < playerItems.Count; i++)
        {
            GameObject go = playerItems[i];
            if (i >= msg_Hu.OtherCard.Count)
            {
                // 没有数据,隐藏该位置
                go.SetActive(false);
                continue;
            }
            go.SetActive(true);
            Msg_OtherCard otherCard = msg_Hu.OtherCard[i];
            DeskPlayer basePlayer = players.FirstOrDefault(p => p.BasePlayer.PlayerId == otherCard.Player);

            if (basePlayer == null)
            {
                GF.LogInfo_gsc("结算界面", $"未找到玩家信息，玩家ID:{otherCard.Player}");
                go.SetActive(false); // 如果找不到玩家信息,也隐藏该位置
                continue;
            }

            // 安全获取玩家的积分变化
            var scoreItem = msg_Hu.ScoreChange?.FirstOrDefault(s => s.Key == otherCard.Player);
            double scoreChange = scoreItem?.Val ?? 0;

            // 判断是否是赢家
            bool isWinner = !isLiuJu && (otherCard.Player == msg_Hu.WinPlayer);
            // 判断是否放炮
            bool isDianPao = !isLiuJu && (otherCard.Player == msg_Hu.FromPlayer);

            GF.LogInfo_gsc("结算界面", $"玩家:{basePlayer.BasePlayer.Nick} ID:{otherCard.Player} 积分变化:{scoreChange}, 是否赢家:{isWinner}, 是否点炮:{isDianPao}");
            go.transform.Find("Name").GetComponent<Text>().FormatNickname(basePlayer.BasePlayer.Nick);
            go.transform.Find("BankerFlag").gameObject.SetActive(
                baseMahjongGameManager.GetDeskPlayer(baseMahjongGameManager.dealerSeatIdx).BasePlayer.PlayerId == basePlayer.BasePlayer.PlayerId);
            go.transform.Find("DianPao").gameObject.SetActive(!isZimoOrGangKai && isDianPao);
            go.transform.Find("JiePao").gameObject.SetActive(!isZimoOrGangKai && isWinner);
            go.transform.Find("ZiMo").gameObject.SetActive(isZimoOrGangKai && isWinner);

            // 设置胡牌类型文本
            string huTypeText = "";
            if (!isLiuJu && isZimoOrGangKai && isWinner)
            {
                // 卡五星玩法：显示"自摸"+牌型名称
                if (IsKaWuXingRule)
                {
                    // 判断房间是否开启了杠上开花
                    int gangKaiConfig = mj_procedure?.enterMJDeskRs?.BaseInfo?.MjConfig?.Xthh?.GangKai ?? 1;
                    bool showGangKaiHua = (gangKaiConfig == 1);

                    if (msg_Hu.WinWay == 1)
                    {
                        huTypeText = "自摸";
                    }
                    else
                    {
                        // 杠开时，根据配置决定显示杠上开花还是自摸
                        huTypeText = showGangKaiHua ? "杠上开花" : "自摸";
                    }
                    // 从Fan字段获取牌型名称
                    if (msg_Hu.Fan != null && msg_Hu.Fan.Count > 0)
                    {
                        List<string> fanNames = new List<string>();
                        foreach (var fanCode in msg_Hu.Fan)
                        {
                            string fanName = GameUtil.GetFanTypeName(fanCode);
                            if (!string.IsNullOrEmpty(fanName) && !fanNames.Contains(fanName))
                            {
                                fanNames.Add(fanName);
                            }
                        }
                        if (fanNames.Count > 0)
                        {
                            huTypeText += " " + string.Join(" ", fanNames);
                        }
                    }
                }
                else
                {
                    // 其他玩法：自摸或杠开时，判断是黑摸还是软摸
                    // 判断房间是否开启了杠上开花
                    int gangKaiConfig = mj_procedure?.enterMJDeskRs?.BaseInfo?.MjConfig?.Xthh?.GangKai ?? 1;
                    bool showGangKaiHua = (gangKaiConfig == 1);

                    if (msg_Hu.WinWay == 1)
                    {
                        huTypeText = IsLaiziUsedAsSubstitute(otherCard.Cards) ? "软摸" : "黑摸";
                    }
                    else
                    {
                        // 杠开时，根据配置决定显示杠上开花还是黑摸/软摸
                        if (showGangKaiHua)
                        {
                            huTypeText = "杠上开花";
                        }
                        else
                        {
                            huTypeText = IsLaiziUsedAsSubstitute(otherCard.Cards) ? "软摸" : "黑摸";
                        }
                    }
                }
            }
            go.transform.Find("HuTypeText").GetComponent<Text>().text = huTypeText;

            // 如果结算时存在包三家的情况（解锁玩家的下家胡牌），在解锁玩家条目中显示 SuopaiBao
            var suopaiObj = go.transform.Find("SuoPaiBao");
            if (suopaiObj != null)
            {
                bool shouldShowSuopaiBao = false;
                long currentPlayerId = basePlayer.BasePlayer.PlayerId;
                long winPlayerId = msg_Hu.WinPlayer;

                // 检查当前玩家是否在解锁列表中
                if (winPlayerId != 0 && openDoublePlayers != null && openDoublePlayers.Contains(currentPlayerId))
                {
                    // 当前玩家是解锁玩家，计算他的下家座位
                    int currentSeat = baseMahjongGameManager.GetSeatIndexByPlayerId(currentPlayerId);
                    int playerCount = GetRealPlayerCount();
                    // 下家偏移：2人桌下家偏移为2，3/4人桌下家偏移为1
                    int nextSeatOffset = (playerCount == 2) ? 2 : 1;
                    int nextSeat = (currentSeat + nextSeatOffset) % 4;
                    long nextPlayerId = baseMahjongGameManager.GetDeskPlayer(nextSeat)?.BasePlayer?.PlayerId ?? 0;

                    // 如果下家是赢家，则显示 SuopaiBao
                    if (nextPlayerId == winPlayerId)
                    {
                        shouldShowSuopaiBao = true;
                    }
                }
                suopaiObj.gameObject.SetActive(shouldShowSuopaiBao);
            }

            go.transform.Find("LaiziCountText").GetComponent<Text>().text = otherCard.PiaoLai > 0 ? $"飘赖子数：{otherCard.PiaoLai}" : "";

            if (scoreChange > 0)
            {
                go.transform.Find("WinScore").GetComponent<Text>().text = "+" + scoreChange.ToString();
                go.transform.Find("LoseScore").GetComponent<Text>().text = "";
                go.transform.Find("WinEff").gameObject.SetActive(true);
            }
            else
            {
                go.transform.Find("LoseScore").GetComponent<Text>().text = scoreChange.ToString();
                go.transform.Find("WinScore").GetComponent<Text>().text = "";
                go.transform.Find("WinEff").gameObject.SetActive(false);
            }

            // 构建手牌列表
            List<int> handCards = new List<int>();

            // 添加面子牌（已经碰、杠的牌）
            if (otherCard.Melds != null && otherCard.Melds.Count > 0)
            {
                foreach (var meld in otherCard.Melds)
                {
                    if (meld.Card != null && meld.Card.Val != null && meld.Card.Val.Count > 0)
                    {
                        int[] cardValues = meld.Card.Val.OrderBy(v => v).ToArray();
                        handCards.AddRange(cardValues);
                        handCards.Add(-1); // 添加分隔符
                    }
                }
            }

            // 添加手牌
            int separateLastCard = 0;
            if (otherCard.Cards != null && otherCard.Cards.Count > 0)
            {
                // 判断是否需要单独显示最后一张牌：
                // 1. 如果是自摸或杠开，且是赢家
                // 2. 如果是仙桃晃晃流局，所有玩家的最后一张牌都需要单独显示（作为最后摸上的牌）
                bool needSeparateLastCard = isWinner || (isLiuJu && IsXianTaoRule);
                if (needSeparateLastCard && otherCard.Cards.Count > 0)
                {
                    // 先保存原始手牌的最后一张（未排序的最后一张，即实际摸到的牌）
                    separateLastCard = otherCard.Cards[otherCard.Cards.Count - 1];

                    // 将除最后一张外的其他牌进行排序后加入手牌列表
                    int[] sortedCards = otherCard.Cards.Take(otherCard.Cards.Count - 1).OrderBy(v => v).ToArray();
                    handCards.AddRange(sortedCards);
                }
                else
                {
                    // 正常添加所有手牌（排序）
                    int[] sortedCards = otherCard.Cards.OrderBy(v => v).ToArray();
                    handCards.AddRange(sortedCards);
                }
            }

            // 回收之前的手牌对象到对象池
            RecyclePlayerHandObjects(i);
            // 创建新的手牌对象（如果有单独显示的牌，会在最后添加）
            CreatePlayerHandObjects(i, go.transform.Find("Content").gameObject, handCards, separateLastCard);
        }

        //更新局数
        Update2DRoomInfo();

        //最后一局
        if (msg_Hu.NextTime <= 0 || mj_procedure.enterMJDeskRs.GameNumbers >= mj_procedure.enterMJDeskRs.BaseInfo.BaseConfig.PlayerTime)
        {
            endGameCountDownText.transform.parent.gameObject.SetActive(false);
            recordBtn.SetActive(true);
            //隐藏解散房间界面
            GF.UI.CloseUIForms(UIViews.MJDismissRoom);
        }
        else
        {
            endGameCountDownText.transform.parent.gameObject.SetActive(true);
            recordBtn.SetActive(false);
            //启动倒计时，倒计时结束自动调用OnButtonClick("Next");
            int nextTimeSeconds = (int)(msg_Hu.NextTime - Util.GetServerTime()) / 1000;
            CoroutineRunner.Instance.StartCountdown(nextTimeSeconds - 5, endGameCountDownText, endGamePanel, () => OnButtonClick("Next"), "ss");
        }

        // 清空所有数据和UI
        endGamePanel.SetActive(true);
    }

    public void HideGameEndPanel()
    {
        // 停止倒计时
        CoroutineRunner.Instance.StopCountdown(endGamePanel);

        // 清空倒计时显示
        if (endGameCountDownText != null)
        {
            endGameCountDownText.text = "";
        }

        // 回收所有玩家的手牌对象
        RecycleAllPlayerHandObjects();

        ResetGameUI();

        // 避免在隐藏结算面板时就清理UI，应该在下一局开始前清理
        endGamePanel.SetActive(false);
    }

    public void ResetGameUI()
    {
        HideHuPaiBtn();
        // 清理赖子信息
        ClearLaiZiCards();
        // 清理胡牌提示数据
        huPaiTipsInfos = null;
        // 清空2D座位显示
        CleanupAll2DObjects();
        // 清空锁牌状态（下一局重新判定）
        ClearSuoPaiState();

        // 还原到游戏准备状态
        if (mj_procedure?.enterMJDeskRs != null)
        {
            // 检查是否不是最后一局（最后一局不需要还原到准备状态）
            bool isNotLastGame = mj_procedure.enterMJDeskRs.GameNumbers < mj_procedure.enterMJDeskRs.BaseInfo.BaseConfig.PlayerTime;

            if (isNotLastGame)
            {
                // 所有玩家恢复为未准备状态
                Reset2DAllPlayersReadyState();

                // 更新2D房间信息显示
                Update2DRoomInfo();

                GF.LogInfo_gsc("[游戏重置] 还原到游戏准备状态");
            }
            else
            {
                GF.LogInfo_gsc("[游戏重置] 最后一局，不还原准备状态");
            }
        }
    }

    /// <summary>
    /// 回收所有玩家的手牌对象
    /// </summary>
    private void RecycleAllPlayerHandObjects()
    {
        foreach (var kvp in playerHandObjects)
        {
            RecyclePlayerHandObjects(kvp.Key);
        }
    }

    public async void ShowGameRecordPanel()
    {
        if (mj_procedure.singleGameRecord == null)
        {
            GF.LogError("[单局战绩] singleGameRecord数据为空，无法显示单局战绩面板");
            return;
        }
        HideGameEndPanel();
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("SingleGameRecord", mj_procedure.singleGameRecord.ToByteArray());
        uiParams.Set<VarInt64>("PlayerTime", mj_procedure.enterMJDeskRs.BaseInfo.BaseConfig.PlayerTime);
        uiParams.Set<VarInt32>("MJMethod", (int)mj_procedure.enterMJDeskRs.BaseInfo.MjConfig.MjMethod);
        await GF.UI.OpenUIFormAwait(UIViews.MJGameSettle, uiParams);
    }

    #endregion

    #region 解散房间
    /// <summary>
    /// 处理麻将解散消息 - 由Procedure调用
    /// </summary>
    public void Function_Msg_SynMJDismiss(Msg_SynMJDismiss ack)
    {
        List<DeskPlayer> deskPlayers = baseMahjongGameManager?.GetAliveDeskPlayers();
        if (deskPlayers == null) return;
        // 转换为BasePlayer列表
        List<BasePlayer> allPlayers = deskPlayers.Select(dp => dp.BasePlayer).ToList();
        // 获取已同意解散的玩家列表
        List<long> agreeDismissPlayerList = mj_procedure.enterMJDeskRs.AgreeDismissPlayer.ToList();
        // 通过框架打开解散面板
        Util.ShowDismissPanel(ack.AgreeDismissTime, ack.PlayerId, allPlayers, agreeDismissPlayerList.ToList());
    }

    #endregion

    #region 桌子状态改变处理

    /// <summary>
    /// 处理桌子状态改变
    /// <summary>
    /// 处理桌子状态改变
    /// KWX流程：
    /// 1. KwxPiao（飘分阶段）→ 显示飘分面板
    /// 2. ChangeCard（换牌阶段）→ 预显示HuanPaiAniPanel和选牌中状态（XuanPaiPanel在发牌完成后由ShowKWXChangeCardUI显示）
    /// </summary>
    public void HandleDeskStateChange(MJState newState)
    {
        GF.LogInfo($"桌子状态改变: {newState}");

        switch (newState)
        {
            case MJState.KwxPiao:
                ShowPiaoPanel();
                // 飘分阶段清空准备显示（类似MJ_PLAY阶段）
                ClearReadyState();
                break;
            case MJState.ChangeCard:
                // 状态改变时预显示换牌动画面板和选牌中状态
                // XuanPaiPanel 在发牌完成后由 ShowKWXChangeCardUI 显示
                break;
            case MJState.ThrowCard:
                if (IsXueLiuRule)
                {
                    // 甩牌阶段
                    ShowThrowCardUI();
                    ClearReadyState();
                }
                break;
        }
    }

    #endregion

    #region 赖子信息
    public GameObject laiziInfo;

    // 记录生成的赖子牌对象，用于清理
    private GameObject currentPiaoLaiziCard;
    private GameObject currentLaiziCard;

    /// <summary>
    /// 显示赖子信息界面（赖子玩法时立即显示）
    /// </summary>
    public void ShowLaiZiInfoPanel()
    {
        laiziInfo.SetActive(true);
        GF.LogInfo_gsc("赖子信息", "显示赖子信息界面");
    }

    /// <summary>
    /// 显示具体的赖子牌（发牌后调用）
    /// </summary>
    /// <param name="piaoLaizi">飘癞子牌值</param>
    /// <param name="laizi">赖子牌值</param>
    public void ShowLaiZiCards(MahjongFaceValue piaoLaizi, MahjongFaceValue laizi)
    {
        if (laiziInfo == null) return;

        // 确保界面已显示
        if (!laiziInfo.activeSelf)
        {
            laiziInfo.SetActive(true);
        }

        // 清理之前的赖子牌（如果有的话）
        ClearLaiZiCards();

        Transform piaoLaiziParent = laiziInfo.transform.Find("PiaoLaiziParent");
        Transform laiziParent = laiziInfo.transform.Find("LaiziParent");

        // 生成飘癞子牌（先显示）
        if (piaoLaizi != MahjongFaceValue.MJ_UNKNOWN && piaoLaiziParent != null)
        {
            CreateAndAnimateLaiziCard(piaoLaizi, piaoLaiziParent, ref currentPiaoLaiziCard, "飘癞子", false);
        }

        // 生成赖子牌（延迟0.3秒显示，让动画更有层次感）
        if (laizi != MahjongFaceValue.MJ_UNKNOWN && laiziParent != null)
        {
            DOTween.Sequence()
                .AppendInterval(0.3f)
                .AppendCallback(() =>
                {
                    CreateAndAnimateLaiziCard(laizi, laiziParent, ref currentLaiziCard, "赖子", true);
                });
        }
    }

    /// <summary>
    /// 断线重连时显示赖子信息（无动画，直接显示）
    /// </summary>
    /// <param name="piaoLaizi">飘癞子牌值</param>
    /// <param name="laizi">赖子牌值</param>
    public void ShowLaiZiCardsImmediate(MahjongFaceValue piaoLaizi, MahjongFaceValue laizi)
    {
        if (laiziInfo == null) return;

        // 确保界面已显示
        if (!laiziInfo.activeSelf)
        {
            laiziInfo.SetActive(true);
        }

        // 清理之前的赖子牌（如果有的话）
        ClearLaiZiCards();

        Transform piaoLaiziParent = laiziInfo.transform.Find("PiaoLaiziParent");
        Transform laiziParent = laiziInfo.transform.Find("LaiziParent");

        // 生成飘癞子牌（无动画）
        if (piaoLaizi != MahjongFaceValue.MJ_UNKNOWN && piaoLaiziParent != null)
        {
            CreateLaiziCardImmediate(piaoLaizi, piaoLaiziParent, ref currentPiaoLaiziCard, "飘癞子", false);
        }

        // 生成赖子牌（无动画）
        if (laizi != MahjongFaceValue.MJ_UNKNOWN && laiziParent != null)
        {
            CreateLaiziCardImmediate(laizi, laiziParent, ref currentLaiziCard, "赖子", true);
        }
    }

    /// <summary>
    /// 创建并动画显示赖子牌
    /// </summary>
    /// <param name="cardValue">牌值</param>
    /// <param name="targetParent">目标父物体</param>
    /// <param name="cardObj">创建的牌对象引用</param>
    /// <param name="cardType">牌类型（用于日志）</param>
    /// <param name="showLaiziMark">是否显示赖子标记</param>
    private void CreateAndAnimateLaiziCard(MahjongFaceValue cardValue, Transform targetParent, ref GameObject cardObj, string cardType, bool showLaiziMark = true)
    {
        // 从对象池获取麻将牌
        cardObj = mjCard2DPool.PopGameObject();
        if (cardObj == null)
        {
            GF.LogError("赖子信息", $"无法从对象池获取{cardType}牌对象");
            return;
        }
        cardObj.SetActive(true);

        // 设置牌值
        MjCard_2D mjCard = cardObj.GetComponent<MjCard_2D>();
        if (mjCard != null)
        {
            mjCard.SetCardValue(cardValue, showLaiziMark);
        }

        // 设置初始状态：从屏幕上方飞入，带透明度和缩放效果
        cardObj.transform.SetParent(targetParent, false);
        cardObj.transform.localPosition = new Vector3(0, 300, 0); // 从上方开始
        cardObj.transform.localScale = Vector3.zero; // 从0开始缩放

        // 设置初始透明度
        CanvasGroup canvasGroup = cardObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = cardObj.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0;

        // 创建动画序列
        Sequence animSequence = DOTween.Sequence();

        // 1. 位置动画：从上方飞入，带回弹效果
        animSequence.Append(cardObj.transform.DOLocalMove(Vector3.zero, 0.6f)
            .SetEase(DG.Tweening.Ease.OutBack));

        // 2. 缩放动画：从0放大到0.6，带弹性效果
        animSequence.Join(cardObj.transform.DOScale(0.6f, 0.6f)
            .SetEase(DG.Tweening.Ease.OutBack));

        // 3. 透明度动画：淡入效果
        animSequence.Join(canvasGroup.DOFade(1f, 0.4f)
            .SetEase(DG.Tweening.Ease.OutQuad));

        // 4. 轻微的摆动效果，让牌看起来更生动
        animSequence.Append(cardObj.transform.DORotate(new Vector3(0, 0, 3), 0.15f)
            .SetEase(DG.Tweening.Ease.InOutSine));
        animSequence.Append(cardObj.transform.DORotate(new Vector3(0, 0, -3), 0.15f)
            .SetEase(DG.Tweening.Ease.InOutSine));
        animSequence.Append(cardObj.transform.DORotate(Vector3.zero, 0.1f)
            .SetEase(DG.Tweening.Ease.InOutSine));
    }

    /// <summary>
    /// 创建赖子牌（无动画，用于断线重连）
    /// </summary>
    /// <param name="cardValue">牌值</param>
    /// <param name="targetParent">目标父物体</param>
    /// <param name="cardObj">创建的牌对象引用</param>
    /// <param name="cardType">牌类型（用于日志）</param>
    /// <param name="showLaiziMark">是否显示赖子标记</param>
    private void CreateLaiziCardImmediate(MahjongFaceValue cardValue, Transform targetParent, ref GameObject cardObj, string cardType, bool showLaiziMark = true)
    {
        // 从对象池获取麻将牌
        cardObj = mjCard2DPool.PopGameObject();
        if (cardObj == null)
        {
            GF.LogError("赖子信息", $"无法从对象池获取{cardType}牌对象");
            return;
        }
        cardObj.SetActive(true);

        // 设置牌值
        MjCard_2D mjCard = cardObj.GetComponent<MjCard_2D>();
        if (mjCard != null)
        {
            mjCard.SetCardValue(cardValue, showLaiziMark);
        }

        // 直接设置到目标位置，无动画
        cardObj.transform.SetParent(targetParent, false);
        cardObj.transform.localPosition = Vector3.zero;
        cardObj.transform.localScale = Vector3.one * 0.6f;
        cardObj.transform.localRotation = Quaternion.identity;

        // 确保透明度为1（完全不透明）
        CanvasGroup canvasGroup = cardObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = cardObj.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 清理赖子牌对象（但保持界面显示）
    /// </summary>
    public void ClearLaiZiCards()
    {
        // 回收飘癞子牌到对象池
        if (currentPiaoLaiziCard != null)
        {
            // 停止可能正在进行的动画
            currentPiaoLaiziCard.transform.DOKill();

            // 清理CanvasGroup组件（如果有的话）
            CanvasGroup cg = currentPiaoLaiziCard.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.DOKill();
            }

            mjCard2DPool.PushGameObject(currentPiaoLaiziCard);
            currentPiaoLaiziCard = null;
        }

        // 回收赖子牌到对象池
        if (currentLaiziCard != null)
        {
            // 停止可能正在进行的动画
            currentLaiziCard.transform.DOKill();

            // 清理CanvasGroup组件（如果有的话）
            CanvasGroup cg = currentLaiziCard.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.DOKill();
            }

            mjCard2DPool.PushGameObject(currentLaiziCard);
            currentLaiziCard = null;
        }
    }

    #endregion

    #region 胡牌提示
    // 胡牌提示按钮
    public GameObject huPaiBtn;
    public HuPaiTips huPaiTips;
    private HuPaiTipsInfo[] huPaiTipsInfos;

    public void InitHuPaiBtn()
    {
        EventTriggerListener.Get(huPaiBtn).onDown = null;
        EventTriggerListener.Get(huPaiBtn).onUp = null;
        EventTriggerListener.Get(huPaiBtn).onDown = OnHuPaiButtonDown;
        EventTriggerListener.Get(huPaiBtn).onUp = OnHuPaiButtonUp;
        HideHuPaiBtn();
    }

    public void HideHuPaiBtn()
    {
        huPaiBtn.SetActive(false);
        huPaiTips.Hide();
    }

    private void OnHuPaiButtonDown(GameObject go)
    {
        if (huPaiTipsInfos != null && huPaiTipsInfos.Length > 0)
        {
            huPaiTips.Show(huPaiTipsInfos, baseMahjongGameManager.CurrentLaiZi);
            GF.LogInfo_gsc("胡牌提示", $"显示胡牌提示，共{huPaiTipsInfos.Length}种胡法");
        }
    }

    private void OnHuPaiButtonUp(GameObject go)
    {
        huPaiTips.Hide();
        GF.LogInfo_gsc("胡牌提示", "隐藏胡牌提示");
    }

    /// <summary>
    /// 当抬起一张牌时，显示打出这张牌后可以胡的牌
    /// </summary>
    /// <param name="raisedCard">抬起的牌</param>
    public void ShowTingHuTipsForCard(MahjongFaceValue raisedCard)
    {
        // 血流成河换牌/甩牌阶段不显示胡牌提示
        if (mj_procedure?.enterMJDeskRs?.CkState == MJState.ChangeCard ||
            mj_procedure?.enterMJDeskRs?.CkState == MJState.ThrowCard)
        {
            return;
        }

        // 土豪必掷模式下，有赖子不能胡牌
        if (IsTuHaoBiZhi && GetCurrentLaiziCount() >= 1)
        {
            GF.UI.ShowToast("有赖子不能胡牌");
            return;
        }

        if (GetCurrentLaiziCount() >= 2)
        {
            return; // 赖子数量>=2时不检测胡牌提示
        }
        // 获取除抬起牌外的所有牌
        var mySeat = seats2D[0];
        var allCards = mySeat.GetAllCardsData();

        // 转换为byte数组用于检测
        byte[] cardArray = new byte[34];
        bool removed = false; // 确保只移除一张
        foreach (var card in allCards)
        {
            if (card == MahjongFaceValue.MJ_UNKNOWN) continue;

            if (!removed && card == raisedCard)
            {
                removed = true;
                continue; // 跳过这张抬起的牌
            }

            int index = (int)card; // MahjongFaceValue枚举从0开始，直接使用即可
            if (index >= 0 && index < 34)
            {
                cardArray[index]++;
            }
        }

        byte laiziIndex = GetCurrentLaiziIndex();

        // 获取打出这张牌后，可以胡的牌（传入统计场上已使用牌数的回调函数）
        var huInfos = baseMahjongGameManager?.mjHuTingCheck?.GetHuCards(cardArray, laiziIndex);

        if (huInfos != null && huInfos.Length > 0)
        {
            huPaiTips.Show(huInfos, baseMahjongGameManager.CurrentLaiZi);
            GF.LogInfo_gsc("听牌提示", $"打出 {raisedCard} 后可听，显示胡牌提示，共{huInfos.Length}种胡法");
        }
    }

    /// <summary>
    /// 检查胡牌按钮的可见性（打牌后如果已听牌则显示）
    /// </summary>
    public void CheckHuPaiBtnVisibility()
    {
        HideHuPaiBtn();

        // 土豪必掷模式下，有赖子不能胡牌
        if (IsTuHaoBiZhi && GetCurrentLaiziCount() >= 1)
        {
            return;
        }

        if (GetCurrentLaiziCount() >= 2)
        {
            return; // 赖子数量>=2时不检测胡牌提示
        }
        var mySeat = seats2D[0];

        // ✅ 获取完整的牌数据（手牌+摸牌）转换为检测数组
        byte[] cardArray = GetPlayerCompleteCardArray();
        byte laiziIndex = GetCurrentLaiziIndex();

        // 使用MahjongHuTingCheck的GetHuCards方法获取可胡的牌（传入统计场上已使用牌数的回调函数）
        huPaiTipsInfos = baseMahjongGameManager?.mjHuTingCheck?.GetHuCards(cardArray, laiziIndex);

        // 如果已经听牌（GetHuCards返回空数组表示已胡牌，返回有数据表示可胡哪些牌）
        if (huPaiTipsInfos != null && huPaiTipsInfos.Length > 0)
        {
            //按住显示胡牌提示 松开隐藏
            huPaiBtn.SetActive(true);
            GF.LogInfo_gsc("胡牌提示", $"显示胡牌按钮，可胡{huPaiTipsInfos.Length}种牌 (含手牌{mySeat.GetHandCardCount()}张+摸牌{(mySeat.GetMoPaiData() != MahjongFaceValue.MJ_UNKNOWN ? 1 : 0)}张)");
        }
        else
        {
            HideHuPaiBtn();
            GF.LogInfo_gsc("胡牌提示", $"未听牌，隐藏胡牌按钮 (含手牌{mySeat.GetHandCardCount()}张+摸牌{(mySeat.GetMoPaiData() != MahjongFaceValue.MJ_UNKNOWN ? 1 : 0)}张)");
        }
    }

    /// <summary>
    /// 检查并显示听牌标记（手牌中哪些牌打出后可以听牌）
    /// </summary>
    public void CheckAndShowTingMarks()
    {
        // 先隐藏听牌按钮
        HideHuPaiBtn();

        // 土豪必掷模式下，有赖子不能胡牌
        if (IsTuHaoBiZhi && GetCurrentLaiziCount() >= 1)
        {
            GF.UI.ShowToast("有赖子不能胡牌");
            return;
        }

        // 赖子数量>=2时不检测听牌标记
        if (GetCurrentLaiziCount() >= 2)
        {
            GF.UI.ShowToast("手中有多个赖子，无法胡牌");
            return;
        }
        var mySeat = seats2D[0];

        // ✅ 获取完整的牌数据（手牌+摸牌）转换为检测数组
        byte[] cardArray = GetPlayerCompleteCardArray();
        byte laiziIndex = GetCurrentLaiziIndex();

        // 使用CheckTing检测听牌信息
        TingData[] tingDatas = baseMahjongGameManager?.mjHuTingCheck?.CreateTingDataMemory();
        if (tingDatas != null)
        {
            baseMahjongGameManager.mjHuTingCheck.CheckTing(cardArray, laiziIndex, ref tingDatas);
        }

        // 先清除所有听牌标记
        ClearAllTingMarks();

        // 遍历听牌数据，给对应的手牌和摸牌添加听牌标记
        if (tingDatas != null)
        {
            int tingCardCount = 0;
            for (int i = 0; i < tingDatas.Length; i++)
            {
                if (tingDatas[i].tingCardIdx == -1)
                    break;

                tingCardCount++;

                // tingCardIdx是牌的索引（0-33）,直接对应MahjongFaceValue枚举值
                int tingCardIdx = tingDatas[i].tingCardIdx;
                MahjongFaceValue tingCard = (MahjongFaceValue)tingCardIdx;

                // 在手牌容器中找到这张牌并显示听牌标记
                if (mySeat.HandContainer != null)
                {
                    for (int j = 0; j < mySeat.HandContainer.childCount; j++)
                    {
                        Transform cardTrans = mySeat.HandContainer.GetChild(j);
                        MjCard_2D mjCard = cardTrans.GetComponent<MjCard_2D>();
                        if (mjCard != null && mjCard.mahjongFaceValue == tingCard)
                        {
                            mjCard.SetTingMark(true);
                        }
                    }
                }

                // ✅ 在摸牌容器中找到这张牌并显示听牌标记
                if (mySeat.MoPaiContainer != null && mySeat.MoPaiContainer.childCount > 0)
                {
                    Transform moPaiTrans = mySeat.MoPaiContainer.GetChild(0);
                    MjCard_2D moPaiCard = moPaiTrans.GetComponent<MjCard_2D>();
                    if (moPaiCard != null && moPaiCard.mahjongFaceValue == tingCard)
                    {
                        moPaiCard.SetTingMark(true);
                    }
                }
            }

            var allCards = mySeat.GetAllCardsData();
            GF.LogInfo_gsc("听牌检测", $"检测完成，总牌数{allCards.Count}张 (手牌{mySeat.GetHandCardCount()}张+摸牌{(mySeat.GetMoPaiData() != MahjongFaceValue.MJ_UNKNOWN ? 1 : 0)}张)，听牌组合{tingCardCount}种");
        }
    }

    /// <summary>
    /// 清除所有听牌标记
    /// </summary>
    public void ClearAllTingMarks()
    {
        var mySeat = seats2D[0];

        // 清除所有手牌的听牌标记
        if (mySeat.HandContainer != null)
        {
            for (int i = 0; i < mySeat.HandContainer.childCount; i++)
            {
                Transform cardTrans = mySeat.HandContainer.GetChild(i);
                MjCard_2D mjCard = cardTrans.GetComponent<MjCard_2D>();
                if (mjCard != null)
                {
                    mjCard.SetTingMark(false);
                }
            }
        }

        // ✅ 清除摸牌的听牌标记
        if (mySeat.MoPaiContainer != null && mySeat.MoPaiContainer.childCount > 0)
        {
            Transform moPaiTrans = mySeat.MoPaiContainer.GetChild(0);
            MjCard_2D moPaiCard = moPaiTrans.GetComponent<MjCard_2D>();
            if (moPaiCard != null)
            {
                moPaiCard.SetTingMark(false);
            }
        }
    }

    /// <summary>
    /// 获取玩家当前的完整牌数据（包含手牌和摸牌，转换为胡听检测用的byte数组）
    /// </summary>
    /// <returns>用于胡听检测的牌数组</returns>
    private byte[] GetPlayerCompleteCardArray()
    {
        var mySeat = seats2D[0];
        var allCards = mySeat.GetAllCardsData();

        // 转换为byte数组用于检测
        byte[] cardArray = new byte[34];
        foreach (var card in allCards)
        {
            if (card == MahjongFaceValue.MJ_UNKNOWN) continue;
            int index = (int)card; // MahjongFaceValue枚举从0开始，无需减1
            if (index >= 0 && index < 34)
            {
                cardArray[index]++;
            }
        }

        return cardArray;
    }

    /// <summary>
    /// 获取当前赖子的索引
    /// </summary>
    /// <returns>赖子索引，没有赖子返回255</returns>
    private byte GetCurrentLaiziIndex()
    {
        if (baseMahjongGameManager?.CurrentLaiZi != MahjongFaceValue.MJ_UNKNOWN)
        {
            return (byte)((int)baseMahjongGameManager.CurrentLaiZi);
        }
        return 255; // 默认无赖子
    }

    /// <summary>
    /// 获取手牌和摸牌里赖子数量
    /// </summary>
    /// <returns>赖子数量，没有赖子返回0</returns>
    private int GetCurrentLaiziCount()
    {
        byte[] cardArray = GetPlayerCompleteCardArray();
        byte laiziIndex = GetCurrentLaiziIndex();

        // 如果没有赖子，返回0
        if (laiziIndex == 255)
        {
            return 0;
        }
        // 返回赖子数量
        return cardArray[laiziIndex];
    }

    /// <summary>
    /// 获取场上指定牌已被使用的数量（包括所有玩家的弃牌和碰吃杠）
    /// </summary>
    /// <param name="faceValue">牌面值</param>
    /// <returns>已使用的数量</returns>
    public int GetUsedCardCountInField(MahjongFaceValue faceValue)
    {
        int usedCount = 0;

        // 遍历所有座位
        for (int i = 0; i < seats2D.Length; i++)
        {
            if (seats2D[i] == null) continue;

            // 统计弃牌区的牌
            if (seats2D[i].DiscardContainer != null)
            {
                for (int j = 0; j < seats2D[i].DiscardContainer.childCount; j++)
                {
                    Transform cardTransform = seats2D[i].DiscardContainer.GetChild(j);
                    MjCard_2D mjCard = cardTransform?.GetComponent<MjCard_2D>();
                    if (mjCard != null && mjCard.mahjongFaceValue == faceValue)
                    {
                        usedCount++;
                    }
                }
            }

            // 统计碰吃杠区的牌
            if (seats2D[i].MeldContainer != null)
            {
                for (int j = 0; j < seats2D[i].MeldContainer.childCount; j++)
                {
                    Transform cardTransform = seats2D[i].MeldContainer.GetChild(j);
                    MjCard_2D mjCard = cardTransform?.GetComponent<MjCard_2D>();
                    if (mjCard != null && mjCard.mahjongFaceValue == faceValue)
                    {
                        usedCount++;
                    }
                }
            }
        }

        return usedCount;
    }

    #endregion

    #region 发送语音
    public MJVoicePanel voiceGo;

    public void InitVoiceGo()
    {
    }

    public void OnVoicePress()
    {
        if (mj_procedure.enterMJDeskRs.BaseInfo.BaseConfig.ForbidVoice)
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

        List<DeskPlayer> players = baseMahjongGameManager?.GetAliveDeskPlayers();
        if (players == null) return;
        DeskPlayer selfPlayer = players.Find(p => p.BasePlayer.PlayerId == Util.GetMyselfInfo().PlayerId);
        if (selfPlayer == null)
        {
            GF.LogWarning("未找到自己的玩家信息");
            return;
        }

        // 显示录音界面并开始录音
        voiceGo.gameObject.SetActive(true);
        voiceGo.StartRecord(selfPlayer.BasePlayer.PlayerId, mj_procedure.enterMJDeskRs.BaseInfo.DeskId);
    }

    public void OnVoiceRelease()
    {
        if (mj_procedure.enterMJDeskRs.BaseInfo.BaseConfig.ForbidVoice)
        {
            return;
        }
        voiceGo.StopRecord();
        voiceGo.gameObject.SetActive(false);
    }

    public void OnVoiceCancel()
    {
        if (mj_procedure.enterMJDeskRs.BaseInfo.BaseConfig.ForbidVoice)
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

    #endregion

    #region 聊天表情
    public GameObject chatPanel;

    #endregion

    #region 设置
    public GameObject settingPanel;
    public GameObject commonPanel;
    public GameObject mjPanel;
    public GameObject commonOn;
    public GameObject mjOn;
    public List<GameObject> Useings;

    public void ShowSettingPanel()
    {
        settingPanel.SetActive(true);
        ShowCommonSettings();
        GF.LogInfo_gsc("[MahjongGameUI] 显示设置面板");
    }

    public void HideSettingPanel()
    {
        settingPanel.SetActive(false);
        GF.LogInfo_gsc("[MahjongGameUI] 隐藏设置面板");
    }

    public void OnSettingTabClick(string tabName)
    {
        if (Util.IsClickLocked()) return;
        Sound.PlayEffect(AudioKeys.SOUND_BTN);
        switch (tabName)
        {
            case "HideSettingPanel":
                HideSettingPanel();
                break;
            case "Common":
                ShowCommonSettings();
                break;
            case "MJ":
                ShowMJSettings();
                break;
            case "BgOn":
                // 切换音乐开关
                Sound.ToggleMusic();
                UpdateCommonSettingsUI();
                break;
            case "EffOn":
                // 切换音效开关
                Sound.ToggleSfx();
                UpdateCommonSettingsUI();
                break;
            case "PutonghuaOn":
                // 切换普通话
                MahjongSettings.SetLanguage(VoiceLanguage.Mandarin);
                UpdateCommonSettingsUI();
                break;
            case "FangyanOn":
                // 切换方言 
                MahjongSettings.SetLanguage(VoiceLanguage.XianTao);
                UpdateCommonSettingsUI();
                break;
            case "MenOn":
                // 切换男声
                MahjongSettings.SetGender(PlayerType.MALE);
                UpdateCommonSettingsUI();
                break;
            case "WomenOn":
                // 切换女声
                MahjongSettings.SetGender(PlayerType.FEMALE);
                UpdateCommonSettingsUI();
                break;
            case "MjCard1":
                //没有变化返回
                if (MahjongSettings.GetCardStyleIndex() == 1) return;
                // 切换麻将牌样式1（绿色）
                MahjongSettings.SetCardStyle(MjColor.lv);
                UpdateMJSettingsUI();
                GF.UI.ShowToast($"已切换为麻将牌样式：{MahjongSettings.GetCardStyleName()}");
                break;
            case "MjCard2":
                //没有变化返回
                if (MahjongSettings.GetCardStyleIndex() == 2) return;
                // 切换麻将牌样式2（黄色）
                MahjongSettings.SetCardStyle(MjColor.huang);
                UpdateMJSettingsUI();
                GF.UI.ShowToast($"已切换为麻将牌样式：{MahjongSettings.GetCardStyleName()}");
                break;
            case "MjCard3":
                //没有变化返回
                if (MahjongSettings.GetCardStyleIndex() == 3) return;
                // 切换麻将牌样式3（蓝色）
                MahjongSettings.SetCardStyle(MjColor.lan);
                UpdateMJSettingsUI();
                GF.UI.ShowToast($"已切换为麻将牌样式：{MahjongSettings.GetCardStyleName()}");
                break;
            case "DismissDesk":
                Util.GetInstance().OpenMJConfirmationDialog("解散房间", "是否强制解散房间？", () =>
                {
                    Msg_DisMissDeskRq req = MessagePool.Instance.Fetch<Msg_DisMissDeskRq>();
                    req.DeskId = mj_procedure.enterMJDeskRs.BaseInfo.DeskId;
                    HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_DisMissDeskRq, req);
                });
                break;
            default:
                GF.LogWarning("[MahjongGameUI] 未知设置按钮点击: " + tabName);
                break;
        }
    }

    /// <summary>
    /// 桌布设置点击处理
    /// 索引 1-9：1-6 对应 2D 桌布，7-9 对应 3D 桌布
    /// </summary>
    /// <param name="tabName">桌布索引，例如："1", "5", "9"</param>
    public void OnSetTableClick(string tabName)
    {
        if (Util.IsClickLocked()) return;
        Sound.PlayEffect(AudioKeys.SOUND_BTN);

        // 解析桌布索引
        if (!int.TryParse(tabName, out int tableBgIndex) || tableBgIndex < 1 || tableBgIndex > 9)
        {
            GF.LogWarning($"[MahjongGameUI] 无效的桌布索引: {tabName}，有效值为 1-9");
            return;
        }

        // 检查是否与当前设置相同，避免重复操作
        if (MahjongSettings.GetTableBg() == tableBgIndex)
        {
            GF.LogInfo($"[MahjongGameUI] 桌布设置未改变: 样式{tableBgIndex}");
            return;
        }

        // 根据索引自动判断视图模式：1-6 是 2D，7-9 是 3D
        MahjongViewMode viewMode = tableBgIndex <= 6 ? MahjongViewMode.View2D : MahjongViewMode.View3D;

        // 应用新的桌布设置
        UpdateTableBg(tableBgIndex, viewMode);
        UpdateMJSettingsUI();

        // 显示切换提示
        string viewModeName = viewMode == MahjongViewMode.View2D ? "2D" : "3D";
        GF.UI.ShowToast($"已切换桌布：样式{tableBgIndex}（{viewModeName}）");

        GF.LogInfo($"[MahjongGameUI] 桌布切换成功: 样式{tableBgIndex} - {viewModeName}视图");
    }

    public void ShowCommonSettings()
    {
        commonPanel.SetActive(true);
        commonOn.SetActive(true);
        mjOn.SetActive(false);
        mjPanel.SetActive(false);

        UpdateCommonSettingsUI();
    }

    /// <summary>
    /// 更新通用设置UI显示
    /// </summary>
    private void UpdateCommonSettingsUI()
    {
        commonPanel.transform.Find("BG/On").gameObject.SetActive(Sound.IsMusicEnabled());
        commonPanel.transform.Find("BG/Off").gameObject.SetActive(!Sound.IsMusicEnabled());
        commonPanel.transform.Find("EFF/On").gameObject.SetActive(Sound.IsSfxEnabled());
        commonPanel.transform.Find("EFF/Off").gameObject.SetActive(!Sound.IsSfxEnabled());

        // 根据当前语言设置显示UI
        VoiceLanguage currentLang = MahjongSettings.GetLanguage();
        commonPanel.transform.Find("PuTongHua/On").gameObject.SetActive(currentLang == VoiceLanguage.Mandarin);
        commonPanel.transform.Find("FangYan/On").gameObject.SetActive(currentLang != VoiceLanguage.Mandarin);

        // 根据当前性别设置显示UI
        PlayerType currentGender = MahjongSettings.GetGender();
        commonPanel.transform.Find("Women/On").gameObject.SetActive(currentGender == PlayerType.FEMALE);
        commonPanel.transform.Find("Men/On").gameObject.SetActive(currentGender == PlayerType.MALE);
    }

    public void ShowMJSettings()
    {
        commonPanel.SetActive(false);
        commonOn.SetActive(false);
        mjOn.SetActive(true);
        mjPanel.SetActive(true);

        UpdateMJSettingsUI();

        GF.LogInfo_gsc("[MahjongGameUI] 显示麻将设置面板");
    }

    /// <summary>
    /// 更新麻将设置UI显示
    /// </summary>
    private void UpdateMJSettingsUI()
    {
        int currentStyle = MahjongSettings.GetCardStyleIndex();

        mjPanel.transform.Find("MjCard1/On").gameObject.SetActive(currentStyle == 1);
        mjPanel.transform.Find("MjCard2/On").gameObject.SetActive(currentStyle == 2);
        mjPanel.transform.Find("MjCard3/On").gameObject.SetActive(currentStyle == 3);

        // 更新桌布选择状态（索引 1-9 直接对应，索引-1 即为数组下标）
        int currentTableBg = MahjongSettings.GetTableBg();
        for (int i = 0; i < Useings.Count; i++)
        {
            Useings[i].SetActive(i == currentTableBg - 1);
        }
    }

    public void UpdateTableBg(int tableBg, MahjongViewMode viewMode)
    {
        // 保存到本地缓存
        MahjongSettings.SetViewMode(viewMode);
        MahjongSettings.SetTableBg(tableBg);

        // 刷新桌面背景显示
        SetTableBg();
    }
    #endregion


    #region DOTween 动画管理

    /// <summary>
    /// 组件禁用时，暂停所有与此 GameObject 关联的 DOTween 动画
    /// </summary>
    private void OnDisable()
    {
        // 暂停所有与此 GameObject 关联的 DOTween 动画
        DOTween.Pause(gameObject);
    }

    /// <summary>
    /// 组件销毁时清理资源
    /// </summary>
    private void OnDestroy()
    {
        // 停止所有DOTween动画
        DOTween.Kill(this);

        // 回收所有对象到对象池
        RecycleAllPlayerHandObjects();

        // 清理2D视图对象
        CleanupAll2DObjects();

        // 清理赖子信息
        ClearLaiZiCards();

        // 清空锁牌状态
        ClearSuoPaiState();

        // 清空字典
        playerHandObjects.Clear();

        // 清理语音资源 (MJVoicePanel会自动清理)
        if (voiceGo != null && voiceGo.gameObject != null)
        {
            voiceGo.gameObject.SetActive(false);
        }
    }

    #endregion
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using DG.Tweening;
using UnityGameFramework.Runtime;
using static UtilityBuiltin;

/// <summary>
/// PDK回放界面
/// =============================================
/// 功能：游戏录像回放、步骤控制、快照系统
/// 
/// 模块结构：
/// ├── 1. UI组件引用 (玩家信息/结算/按钮)
/// ├── 2. 回放数据 (m_Data, m_CurrentStepIndex)
/// ├── 3. 快照系统 (PlaybackSnapshot)
/// ├── 4. 播放控制 (Play/Pause/Next/Previous)
/// ├── 5. 手牌渲染 (RenderHandCards)
/// └── 6. 结算显示 (ShowResult)
/// 
/// 最后更新：2025-12-12
/// </summary>
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class PDKPlayBack : UIFormBase
{
    [Header("===== 玩家信息 =====")]
    public GameObject[] PlayerHandCardAreas; // 玩家手牌区域 0-2 对应 玩家1-3
    public GameObject[] PlayArea;              // 出牌区域 0-2 对应 玩家1-3
    public GameObject[] PlayerInfoPanels;    // 玩家信息面板 0-2 对应 玩家1-3
    public GameObject zhaNiaoIndicators; // 扎鸟标识
    public GameObject Coin; // 金币预制体
    private RawImage[] headImage;              // 玩家头像
    private Text[] nickName;                 // 玩家昵称
    private Text[] handCardCountTexts;       // 手牌剩余数量文本
    private Text[] scoreTexts;        // 玩家积分
    private GameObject[] dealerIndicators; //庄家标识
    private GameObject[] buyaoIndicators; // 不要标识
    private GameObject[] qiangzhuangIndicators; // 抢庄标识
    private GameObject[] qiangzhuangIndicatorsNO; // 不抢庄标识
    private GameObject[] baojing; // 报警标识
    private GameObject[] chuntian; // 春天标识

    [Header("===== 特效容器 =====")]
    public GameObject[] EffectContainers; // 特效容器 0-2 对应 玩家1-3
    private Dictionary<Transform, bool> effectPlayingFlags = new Dictionary<Transform, bool>();
    //玩家

    [Header("===== 结算信息 =====")]
    public GameObject[] PlayerResultPanels; // 玩家结算面板 0-2 对应 玩家1-3
    private Text[] Winscore; // 玩家结算分数文本
    private Text[] LoseScore; // 玩家结算分数文本
    public GameObject over;//回放完毕

    [Header("===== 回放按钮 =====")]
    public GameObject agoBtn; // 上一步按钮
    public GameObject afterBtn;// 下一步按钮
    public GameObject stopBtn;// 停止按钮
    public GameObject playBtn;// 播放按钮
    public GameObject replayBtn;// 重播按钮
    public GameObject closeBtn; // 退出按钮
    public Button jumpToEndBtn; // 跳转到结束按钮
    [Header("===== 房间信息 =====")]
    public GameObject roomNumber;// 房号
    public GameObject baseScore;//底分
    public GameObject roundNumber;//局数
    public GameObject timeText;//时间  这个时间是当前系统时间

    [Header("===== 卡牌预制体 =====")]
    public GameObject cardPrefab; // 卡牌预制体

    // 运行时数据
    private Msg_RunFastPlayBack m_Data;
    private int m_CurrentStepIndex = 0;
    private bool m_IsPlaying = false;
    private Coroutine m_PlayCoroutine;
    private int m_CurrentRoundNumber = 1; // 当前回放的局数 (从战绩界面传入)

    // 回放增强功能
    private float[] m_PlaybackSpeeds = { 0.5f, 1f, 2f, 4f }; // 播放速度选项
    private int m_CurrentSpeedIndex = 1; // 默认1x速度
    private float m_CurrentPlaybackSpeed = 1f;
    private bool m_EnableKeyboardShortcuts = true; // 是否启用键盘快捷键

    // 模拟手牌数据 Key: ServerSeat, Value: CardList
    private Dictionary<int, List<int>> m_HandCards = new Dictionary<int, List<int>>();
    // 初始手牌备份，用于重置
    private Dictionary<int, List<int>> m_InitialHandCards = new Dictionary<int, List<int>>();
    // 玩家座位映射 PlayerId -> Seat
    private Dictionary<long, int> m_PlayerSeats = new Dictionary<long, int>();
    // 玩家ID映射 Seat -> PlayerId
    private Dictionary<int, long> m_SeatToPlayerId = new Dictionary<int, long>();
    // 自己的PlayerId
    private long m_MyPlayerId = 0;
    // 被查看玩家的PlayerId（会长查看成员战绩时使用）
    private long m_ViewedPlayerId = 0;
    // 自己的ServerSeat
    private int m_MyServerSeat = 0;
    // 总玩家数
    private int m_TotalPlayerNum = 3;
    // 【使用PDKCardHelper】卡牌辅助工具实例
    private PDKCardHelper m_CardHelper;
    // 【复用PDKGamePanel】引用游戏面板的卡牌创建方法
    private PDKCardManager m_CardManager;
    // 手牌卡牌对象缓存 Seat -> CardObjects
    private Dictionary<int, List<GameObject>> m_HandCardObjects = new Dictionary<int, List<GameObject>>();
    // 出牌区卡牌对象缓存 Seat -> CardObjects
    private Dictionary<int, List<GameObject>> m_PlayAreaCardObjects = new Dictionary<int, List<GameObject>>();

    // 【关键】保存每个玩家的初始卡牌间距 ServerSeat -> CardSpacing
    private Dictionary<int, float> m_InitialCardSpacing = new Dictionary<int, float>();
    
    // 【新增】玩家当前积分 PlayerId -> CurrentScore (回放过程中累加)
    private Dictionary<long, double> m_CurrentScores = new Dictionary<long, double>();

    // 回放快照系统（用于后退操作）
    private class PlaybackSnapshot
    {
        public Dictionary<int, List<int>> HandCards;
        public int ServerSeat;
        public List<int> PlayedCards;
        public Dictionary<long, double> CurrentScores; // 【新增】积分快照

        public PlaybackSnapshot(Dictionary<int, List<int>> handCards, int serverSeat, List<int> playedCards, Dictionary<long, double> currentScores)
        {
            // 深拷贝手牌数据
            HandCards = new Dictionary<int, List<int>>();
            foreach (var kvp in handCards)
            {
                HandCards[kvp.Key] = new List<int>(kvp.Value);
            }
            ServerSeat = serverSeat;
            PlayedCards = new List<int>(playedCards);
            
            // 【新增】深拷贝积分数据
            CurrentScores = new Dictionary<long, double>(currentScores);
        }
    }

    // 快照栈（存储每一步的状态）
    private Stack<PlaybackSnapshot> m_SnapshotStack = new Stack<PlaybackSnapshot>();

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        // 初始化卡牌辅助工具
        InitCardHelper();

        // 初始化卡牌管理器
        InitCardManager();

        // 初始化UI组件引用
        InitUIComponents();

        // 绑定按钮事件
        BindButtonEvents();

        // 解析回放数据
        UIParams uiParams = userData as UIParams;
        var msgBytes = uiParams.Get<VarByteArray>("Msg_RunFastPlayBack");
        m_Data = Msg_RunFastPlayBack.Parser.ParseFrom(msgBytes.Value);

        // 获取当前局数 (从战绩界面传入)
        var lastChooseIndex = uiParams.Get<VarInt32>("LastChooseIndex");
        m_CurrentRoundNumber = lastChooseIndex?.Value ?? 1; // 默认第1局

        // 获取被查看玩家ID（会长查看成员战绩时传入）
        var viewedPlayerId = uiParams.Get<VarInt64>("ViewedPlayerId");
        m_ViewedPlayerId = viewedPlayerId?.Value ?? 0; // 默认0表示查看自己

        // GF.LogInfo_wl($"[PDKPlayBack] 回放数据解析完成，当前局数: {m_CurrentRoundNumber}");

        // 初始化游戏数据
        InitGame();
    }

    /// <summary>
    /// 初始化卡牌辅助工具
    /// </summary>
    private void InitCardHelper()
    {
        GameObject helperObj = new GameObject("CardHelper");
        helperObj.transform.SetParent(transform);
        m_CardHelper = helperObj.AddComponent<PDKCardHelper>();

        if (cardPrefab != null)
        {
            m_CardHelper.Initialize(cardPrefab, helperObj.transform);
        }
        else
        {
            GF.LogError("[PDKPlayBack] 卡牌预制体未设置！");
        }
    }

    /// <summary>
    /// 初始化卡牌管理器
    /// </summary>
    private void InitCardManager()
    {
        GameObject managerObj = new GameObject("CardManager");
        managerObj.transform.SetParent(transform);
        m_CardManager = managerObj.AddComponent<PDKCardManager>();

        // GF.LogInfo_wl("[PDKPlayBack] 卡牌管理器初始化完成");
    }

    /// <summary>
    /// 绑定按钮事件
    /// </summary>
    private void BindButtonEvents()
    {
        // 【修复】先移除所有监听器,防止重复添加导致点击多次执行
        if (agoBtn != null)
        {
            agoBtn.GetComponent<Button>().onClick.RemoveAllListeners();
            agoBtn.GetComponent<Button>().onClick.AddListener(OnClickAgo);
        }
        if (afterBtn != null)
        {
            afterBtn.GetComponent<Button>().onClick.RemoveAllListeners();
            afterBtn.GetComponent<Button>().onClick.AddListener(OnClickAfter);
        }
        if (stopBtn != null)
        {
            stopBtn.GetComponent<Button>().onClick.RemoveAllListeners();
            stopBtn.GetComponent<Button>().onClick.AddListener(OnClickStop);
        }
        if (playBtn != null)
        {
            playBtn.GetComponent<Button>().onClick.RemoveAllListeners();
            playBtn.GetComponent<Button>().onClick.AddListener(OnClickPlay);
        }
        if (replayBtn != null)
        {
            replayBtn.GetComponent<Button>().onClick.RemoveAllListeners();
            replayBtn.GetComponent<Button>().onClick.AddListener(OnClickReplay);
        }
        if (closeBtn != null)
        {
            closeBtn.GetComponent<Button>().onClick.RemoveAllListeners();
            closeBtn.GetComponent<Button>().onClick.AddListener(OnClickClose);
        }
    }

    /// <summary>
    /// 初始化UI组件引用
    /// </summary>
    private void InitUIComponents()
    {
        int playerCount = PlayerInfoPanels?.Length ?? 3;

        headImage = new RawImage[playerCount];
        nickName = new Text[playerCount];
        handCardCountTexts = new Text[playerCount];
        scoreTexts = new Text[playerCount];
        dealerIndicators = new GameObject[playerCount];
        buyaoIndicators = new GameObject[playerCount];
        qiangzhuangIndicators = new GameObject[playerCount];
        qiangzhuangIndicatorsNO = new GameObject[playerCount];
        baojing = new GameObject[playerCount];
        chuntian = new GameObject[playerCount];
        Winscore = new Text[playerCount];
        LoseScore = new Text[playerCount];

        for (int i = 0; i < playerCount; i++)
        {
            if (PlayerInfoPanels != null && i < PlayerInfoPanels.Length && PlayerInfoPanels[i] != null)
            {
                // 查找子组件（根据实际层级调整）
                headImage[i] = PlayerInfoPanels[i].transform.Find("head")?.GetComponent<RawImage>();
                nickName[i] = PlayerInfoPanels[i].transform.Find("Name/NameText")?.GetComponent<Text>();
                handCardCountTexts[i] = PlayerInfoPanels[i].transform.Find("cardBack/Text")?.GetComponent<Text>();
                scoreTexts[i] = PlayerInfoPanels[i].transform.Find("Score/ScoreText")?.GetComponent<Text>();
                dealerIndicators[i] = PlayerInfoPanels[i].transform.Find("Banker")?.gameObject;
                buyaoIndicators[i] = PlayerInfoPanels[i].transform.Find("buyao")?.gameObject;
                qiangzhuangIndicators[i] = PlayerInfoPanels[i].transform.Find("qiangzhuang")?.gameObject;
                qiangzhuangIndicatorsNO[i] = PlayerInfoPanels[i].transform.Find("qiangzhuang_no")?.gameObject;
                baojing[i] = PlayerInfoPanels[i].transform.Find("baojingEff")?.gameObject;
                chuntian[i] = PlayerInfoPanels[i].transform.Find("chuntian")?.gameObject;
            }
        }

        // 初始化结算面板的积分文本
        for (int i = 0; i < playerCount; i++)
        {
            if (PlayerResultPanels != null && i < PlayerResultPanels.Length && PlayerResultPanels[i] != null)
            {
                Winscore[i] = PlayerResultPanels[i].transform.Find("bg/WinScore")?.GetComponent<Text>();
                LoseScore[i] = PlayerResultPanels[i].transform.Find("bg/LoseScore")?.GetComponent<Text>();
                // 初始状态隐藏结算面板
                PlayerResultPanels[i].SetActive(false);
            }
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        StopAutoPlay();

        // 清理卡牌辅助工具
        if (m_CardHelper != null)
        {
            m_CardHelper.Cleanup();
        }

        // 【修复】清理所有数据，防止下次进入回放时数据错乱
        CleanupAllData();

        base.OnClose(isShutdown, userData);
    }

    /// <summary>
    /// 清理所有数据（退出回放时调用）
    /// </summary>
    private void CleanupAllData()
    {
        // GF.LogInfo_wl("[PDKPlayBack] ========== 开始清理所有数据 ==========");

        // 清理快照栈
        m_SnapshotStack.Clear();
        // GF.LogInfo_wl($"[PDKPlayBack] 已清理快照栈");

        // 清理手牌数据
        m_HandCards.Clear();
        m_InitialHandCards.Clear();
        // GF.LogInfo_wl($"[PDKPlayBack] 已清理手牌数据");

        // 清理玩家映射
        m_PlayerSeats.Clear();
        m_SeatToPlayerId.Clear();
        // GF.LogInfo_wl($"[PDKPlayBack] 已清理玩家映射");

        // 清理卡牌对象缓存
        m_HandCardObjects.Clear();
        m_PlayAreaCardObjects.Clear();
        // GF.LogInfo_wl($"[PDKPlayBack] 已清理卡牌对象缓存");

        // 清理卡牌间距缓存
        m_InitialCardSpacing.Clear();
        // GF.LogInfo_wl($"[PDKPlayBack] 已清理卡牌间距缓存");

        // 清理特效播放标志
        effectPlayingFlags.Clear();
        // GF.LogInfo_wl($"[PDKPlayBack] 已清理特效播放标志");

        // 重置状态变量
        m_Data = null;
        m_CurrentStepIndex = 0;
        m_IsPlaying = false;
        m_PlayCoroutine = null;
        m_CurrentRoundNumber = 1;
        m_MyPlayerId = 0;
        m_MyServerSeat = 0;
        m_TotalPlayerNum = 3;
        m_CurrentSpeedIndex = 1;
        m_CurrentPlaybackSpeed = 1f;
        // GF.LogInfo_wl($"[PDKPlayBack] 已重置状态变量");

        // GF.LogInfo_wl("[PDKPlayBack] ========== 数据清理完成 ==========");
    }

    private void Update()
    {
        if (!m_EnableKeyboardShortcuts || m_Data == null) return;

        // 空格键: 播放/暂停
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (m_IsPlaying)
                OnClickStop();
            else
                OnClickPlay();
        }

        // 左箭头: 上一步
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            OnClickAgo();
        }

        // 右箭头: 下一步
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            OnClickAfter();
        }

        // R键: 重播
        if (Input.GetKeyDown(KeyCode.R))
        {
            OnClickReplay();
        }

        // S键: 切换速度
        if (Input.GetKeyDown(KeyCode.S))
        {
            OnClickSpeed();
        }

        // Home键: 跳转到开始
        if (Input.GetKeyDown(KeyCode.Home))
        {
            OnClickJumpToStart();
        }

        // End键: 跳转到结束
        if (Input.GetKeyDown(KeyCode.End))
        {
            OnClickJumpToEnd();
        }
    }

    #region 游戏初始化

    /// <summary>
    /// 同步房间基础信息 (deskCommon)
    /// </summary>
    private void SyncDeskCommonInfo()
    {
        var deskCommon = m_Data.DeskCommon;
        roomNumber.GetComponent<Text>().text = deskCommon.DeskId.ToString();// 房号
        baseScore.GetComponent<Text>().text = deskCommon.BaseConfig.BaseCoin.ToString();//底分

        // 显示当前局数 (从战绩界面传入的 LastChooseIndex)
        long totalRounds = deskCommon.BaseConfig.PlayerTime;
        roundNumber.GetComponent<Text>().text = $"{m_CurrentRoundNumber}/{totalRounds}";//局数

        timeText.GetComponent<Text>().text = System.DateTime.Now.ToString("HH:mm:ss");//时间  这个时间是当前系统时间
        // GF.LogInfo_wl($"[PDKPlayBack] 房间基础信息同步完成 - 当前局数: {m_CurrentRoundNumber}/{totalRounds}");
    }

    /// <summary>
    /// 初始化游戏数据
    /// </summary>
    private void InitGame()
    {
        if (m_Data == null)
        {
            GF.LogError("[PDKPlayBack] 回放数据为空");
            return;
        }

        // 同步房间基础信息 (deskCommon)
        SyncDeskCommonInfo();

        // 初始化玩家座位映射
        InitPlayerSeats();

        // 初始化手牌数据（使用回放数据中的初始手牌）
        InitHandCards();

        // 【新增】设置玩家信息（昵称、头像、积分）
        InitPlayerInfo();

        // 刷新所有UI
        RefreshAllUI();

        // 控制两人游戏的UI显示
        ControlTwoPlayerUI();

        // 设置庄家标识
        SetBankerIndicator();

        // GF.LogInfo_wl($"[PDKPlayBack] 游戏初始化完成，共{m_Data.Option.Count}步");
    }

    /// <summary>
    /// 初始化玩家信息（昵称、头像、积分）
    /// </summary>
    private void InitPlayerInfo()
    {
        if (m_Data == null || m_Data.Players == null)
        {
            GF.LogWarning("[PDKPlayBack] 玩家数据为空，无法初始化玩家信息");
            return;
        }

        // GF.LogInfo_wl($"[PDKPlayBack] 开始初始化玩家信息，共{m_Data.Players.Count}个玩家");

        // 遍历所有玩家
        foreach (var player in m_Data.Players)
        {
            long playerId = player.PlayerId;

            // 获取玩家的服务器座位
            if (!m_PlayerSeats.TryGetValue(playerId, out int serverSeat))
            {
                GF.LogWarning($"[PDKPlayBack] 找不到玩家ID {playerId} 的座位信息");
                continue;
            }

            // 转换为本地座位
            int localSeat = ServerSeatToLocalSeat(serverSeat);
            if (localSeat < 0 || localSeat >= PlayerInfoPanels.Length)
            {
                GF.LogWarning($"[PDKPlayBack] 本地座位 {localSeat} 超出范围");
                continue;
            }

            // 设置昵称
            if (nickName != null && localSeat < nickName.Length && nickName[localSeat] != null)
            {
                string displayName = player.Nick;
                if (displayName.Length > 8)
                {
                    displayName = displayName.Substring(0, 7) + "...";
                }
                nickName[localSeat].text = displayName;
                // GF.LogInfo_wl($"[PDKPlayBack] 设置玩家{playerId}昵称: {displayName} (本地座位{localSeat})");
            }

            // 设置头像
            if (headImage != null && localSeat < headImage.Length && headImage[localSeat] != null)
            {
                Util.DownloadHeadImage(headImage[localSeat], player.HeadImage);
                // GF.LogInfo_wl($"[PDKPlayBack] 设置玩家{playerId}头像 (本地座位{localSeat})");
            }

            // 设置初始积分为0
            if (scoreTexts != null && localSeat < scoreTexts.Length && scoreTexts[localSeat] != null)
            {
                m_CurrentScores[playerId] = 0;
                scoreTexts[localSeat].text = $"积分: 0";
                // GF.LogInfo_wl($"[PDKPlayBack] 设置玩家{playerId}初始积分为0 (本地座位{localSeat})");
            }
        }

        // GF.LogInfo_wl("[PDKPlayBack] 玩家信息初始化完成");
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
    /// 初始化玩家座位映射
    /// </summary>
    private void InitPlayerSeats()
    {
        m_PlayerSeats.Clear();
        m_SeatToPlayerId.Clear();

        // 获取自己的PlayerId
        m_MyPlayerId = Util.GetMyselfInfo().PlayerId;

        // 记录总玩家数
        m_TotalPlayerNum = m_Data.Pos.Count;

        // 使用Pos字段映射 PlayerId -> ServerSeat
        for (int i = 0; i < m_Data.Pos.Count; i++)
        {
            var posData = m_Data.Pos[i];
            long playerId = posData.Key;
            int serverSeat = posData.Val;

            m_PlayerSeats[playerId] = serverSeat;
            m_SeatToPlayerId[serverSeat] = playerId;

            // 【关键修改】如果传入了被查看玩家ID，使用被查看玩家作为基准（0号位）
            // 这样会长查看成员战绩时，0号位显示的是被查看成员的手牌
            long targetPlayerId = m_ViewedPlayerId > 0 ? m_ViewedPlayerId : m_MyPlayerId;
            
            if (playerId == targetPlayerId)
            {
                m_MyServerSeat = serverSeat;
                // GF.LogInfo_wl($"[PDKPlayBack] 找到基准玩家: PlayerId={playerId}, ServerSeat={serverSeat}, IsViewed={m_ViewedPlayerId > 0}");
            }

            // GF.LogInfo_wl($"[PDKPlayBack] 玩家映射: PlayerId={playerId}, ServerSeat={serverSeat}");
        }

        // 记录玩家信息
        for (int i = 0; i < m_Data.Players.Count; i++)
        {
            var player = m_Data.Players[i];
            // GF.LogInfo_wl($"[PDKPlayBack] 玩家{i}: PlayerId={player.PlayerId}, Nick={player.Nick}");
        }
    }

    /// <summary>
    /// 初始化手牌数据（从回放数据中获取初始手牌）
    /// </summary>
    private void InitHandCards()
    {
        m_HandCards.Clear();
        m_InitialHandCards.Clear();

        // 从HandCard字段获取每个玩家的初始手牌（LongListInt格式）
        for (int i = 0; i < m_Data.HandCard.Count; i++)
        {
            var handCardData = m_Data.HandCard[i];
            long playerId = handCardData.Key;
            List<int> cards = new List<int>(handCardData.Vals);

            // 通过PlayerId获取ServerSeat
            if (m_PlayerSeats.TryGetValue(playerId, out int serverSeat))
            {
                m_HandCards[serverSeat] = cards;
                m_InitialHandCards[serverSeat] = new List<int>(cards); // 备份

                // GF.LogInfo_wl($"[PDKPlayBack] 玩家PlayerId={playerId}, ServerSeat={serverSeat}, 初始手牌数量: {cards.Count}");
            }
            else
            {
                GF.LogError($"[PDKPlayBack] 无法找到PlayerId={playerId}的座位映射");
            }
        }
    }

    /// <summary>
    /// 设置庄家标识
    /// </summary>
    private void SetBankerIndicator()
    {
        if (m_Data == null || dealerIndicators == null)
            return;

        long bankerId = m_Data.Banker;
        // GF.LogInfo_wl($"[PDKPlayBack] 庄家ID: {bankerId}");

        // 隐藏所有庄家标识
        for (int i = 0; i < dealerIndicators.Length; i++)
        {
            if (dealerIndicators[i] != null)
            {
                dealerIndicators[i].SetActive(false);
            }
        }

        // 显示庄家标识
        if (bankerId > 0 && m_PlayerSeats.TryGetValue(bankerId, out int bankerServerSeat))
        {
            int bankerLocalSeat = ServerSeatToLocalSeat(bankerServerSeat);
            if (bankerLocalSeat >= 0 && bankerLocalSeat < dealerIndicators.Length && dealerIndicators[bankerLocalSeat] != null)
            {
                dealerIndicators[bankerLocalSeat].SetActive(true);
                // GF.LogInfo_wl($"[PDKPlayBack] 设置庄家标识: PlayerId={bankerId}, ServerSeat={bankerServerSeat}, LocalSeat={bankerLocalSeat}");
            }
        }
    }

    /// <summary>
    /// 控制两人游戏时的UI显示（隐藏左边玩家相关UI）
    /// </summary>
    private void ControlTwoPlayerUI()
    {
        if (m_TotalPlayerNum == 2)
        {
            // 隐藏左边玩家（localSeat=2）的UI元素
            if (PlayerInfoPanels != null && PlayerInfoPanels.Length > 2 && PlayerInfoPanels[2] != null)
            {
                PlayerInfoPanels[2].SetActive(false);
                // GF.LogInfo_wl("[PDKPlayBack] 两人游戏：隐藏左边玩家信息面板");
            }

            if (PlayerHandCardAreas != null && PlayerHandCardAreas.Length > 2 && PlayerHandCardAreas[2] != null)
            {
                PlayerHandCardAreas[2].SetActive(false);
                // GF.LogInfo_wl("[PDKPlayBack] 两人游戏：隐藏左边玩家手牌区域");
            }

            if (PlayArea != null && PlayArea.Length > 2 && PlayArea[2] != null)
            {
                PlayArea[2].SetActive(false);
                // GF.LogInfo_wl("[PDKPlayBack] 两人游戏：隐藏左边玩家出牌区域");
            }

            if (PlayerResultPanels != null && PlayerResultPanels.Length > 2 && PlayerResultPanels[2] != null)
            {
                PlayerResultPanels[2].SetActive(false);
                // GF.LogInfo_wl("[PDKPlayBack] 两人游戏：隐藏左边玩家结算面板");
            }
        }
        else
        {
            // 三人游戏时确保所有UI都显示
            if (PlayerInfoPanels != null && PlayerInfoPanels.Length > 2 && PlayerInfoPanels[2] != null)
            {
                PlayerInfoPanels[2].SetActive(true);
            }

            if (PlayerHandCardAreas != null && PlayerHandCardAreas.Length > 2 && PlayerHandCardAreas[2] != null)
            {
                PlayerHandCardAreas[2].SetActive(true);
            }

            if (PlayArea != null && PlayArea.Length > 2 && PlayArea[2] != null)
            {
                PlayArea[2].SetActive(true);
            }

            if (PlayerResultPanels != null && PlayerResultPanels.Length > 2 && PlayerResultPanels[2] != null)
            {
                PlayerResultPanels[2].SetActive(true);
            }
        }
    }

    #endregion

    #region UI更新

    /// <summary>
    /// 刷新所有UI
    /// </summary>
    private void RefreshAllUI()
    {
        // 清空所有出牌区
        for (int i = 0; i < PlayArea.Length; i++)
        {
            ClearPlayArea(i);
        }

        // 更新所有玩家的手牌显示
        foreach (var kvp in m_HandCards)
        {
            int serverSeat = kvp.Key;
            UpdateHandCardUI(serverSeat);
        }
    }

    /// <summary>
    /// 更新指定玩家的手牌UI（使用PDKCardHelper生成）
    /// </summary>
    private void UpdateHandCardUI(int serverSeat)
    {
        int localSeat = ServerSeatToLocalSeat(serverSeat);
        if (localSeat < 0 || localSeat >= PlayerHandCardAreas.Length)
            return;

        Transform handCardParent = PlayerHandCardAreas[localSeat].transform;

        // 清空现有手牌
        if (m_CardHelper != null)
        {
            m_CardHelper.RecycleAllCardsInParent(handCardParent);
        }

        // 获取该玩家的手牌数据
        if (!m_HandCards.ContainsKey(serverSeat))
            return;

        List<int> cards = m_HandCards[serverSeat];

        // 【回放模式】所有玩家都能看到所有人的手牌

        // 【关键修复】先对手牌进行排序（使用游戏相同的排序逻辑）
        if (m_CardManager != null && cards.Count > 0)
        {
            cards = m_CardManager.SortCards(cards);
        }

        // 【使用PDKCardHelper批量创建卡牌】
        if (m_CardHelper != null && cards.Count > 0)
        {
            List<GameObject> cardObjects = m_CardHelper.CreateCards(cards, handCardParent);

            // 【关键修复】使用CalculateLayoutForCurrentHand方法计算正确的布局参数
            float cardWidth, cardSpacing, cardHeight;
            float cardScale = localSeat == 0 ? 1.0f : 0.8f; // 自己的手牌使用原始大小，其他玩家稍小

            // 创建临时UIManager实例来计算布局参数
            GameObject tempManagerObj = new GameObject("TempUIManager");
            tempManagerObj.transform.SetParent(transform);
            PDKUIManager tempManager = tempManagerObj.AddComponent<PDKUIManager>();

            // 使用反射调用私有方法CalculateLayoutForCurrentHand
            var method = typeof(PDKUIManager).GetMethod("CalculateLayoutForCurrentHand",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(tempManager, new object[] { cardObjects.Count });
                cardWidth = PDKUIManager.s_cardWidth;
                cardSpacing = PDKUIManager.s_cardSpacing;
                cardHeight = PDKUIManager.s_cardHeight;
            }
            else
            {
                // 如果反射失败，使用默认值
                cardWidth = PDKConstants.BASE_CARD_WIDTH;
                cardSpacing = cardWidth * PDKConstants.IDEAL_SPACING_RATIO;
                cardHeight = cardWidth * PDKConstants.CARD_ASPECT_RATIO;
            }

            // 清理临时对象
            DestroyImmediate(tempManagerObj);

            // 设置每张卡牌的组件和位置
            for (int i = 0; i < cardObjects.Count; i++)
            {
                GameObject cardObj = cardObjects[i];

                // 使用PDKCardHelper设置卡牌组件
                m_CardHelper.SetupCardComponent(cardObj, cards[i]);

                // 设置卡牌Transform
                RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // 设置锚点和pivot
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);

                    // 设置大小（使用计算得到的尺寸）
                    rectTransform.sizeDelta = new Vector2(cardWidth, cardHeight);

                    // 设置缩放
                    rectTransform.localScale = new Vector3(cardScale, cardScale, 1f);

                    // 使用与PDKUIManager.ArrangeHandCards相同的位置计算方法
                    float totalWidth = (cardObjects.Count - 1) * cardSpacing + cardWidth;
                    float startX = -totalWidth / 2f;
                    float xPos = startX + i * cardSpacing;

                    rectTransform.anchoredPosition = new Vector2(xPos, 0);
                }
            }

            // 【关键】保存初始间距供后续重新排列使用
            if (!m_InitialCardSpacing.ContainsKey(serverSeat))
            {
                m_InitialCardSpacing[serverSeat] = cardSpacing;
                // GF.LogInfo_wl($"[PDKPlayBack] 保存玩家{serverSeat}的初始卡牌间距: {cardSpacing}");
            }

            // GF.LogInfo_wl($"[PDKPlayBack] 更新玩家{serverSeat}手牌UI，共{cardObjects.Count}张牌，缩放:{cardScale}");
        }

        // 更新手牌数量文本
        if (handCardCountTexts != null && localSeat < handCardCountTexts.Length && handCardCountTexts[localSeat] != null)
        {
            handCardCountTexts[localSeat].text = cards.Count.ToString();
        }
    }

    /// <summary>
    /// 更新出牌区UI（使用PDKCardHelper生成）
    /// </summary>
    private void UpdatePlayAreaUI(int serverSeat, List<int> cards)
    {
        int localSeat = ServerSeatToLocalSeat(serverSeat);
        if (localSeat < 0 || localSeat >= PlayArea.Length)
            return;

        Transform playAreaParent = PlayArea[localSeat].transform;

        // 清空现有出牌
        if (m_CardHelper != null)
        {
            m_CardHelper.RecycleAllCardsInParent(playAreaParent);
        }

        // 【关键修复】先对出牌进行排序（使用游戏相同的排序逻辑）
        if (m_CardManager != null && cards != null && cards.Count > 0)
        {
            cards = m_CardManager.SortCards(cards);
        }

        // 【使用PDKCardHelper批量创建卡牌】
        if (m_CardHelper != null && cards != null && cards.Count > 0)
        {
            List<GameObject> cardObjects = m_CardHelper.CreateCards(cards, playAreaParent);

            // 【关键修复】使用与游戏相同的出牌区布局参数
            float baseCardWidth = PDKUIManager.s_cardWidth;
            float baseCardHeight = baseCardWidth * 1.4f;
            float cardScale = 0.5f; // 出牌区使用0.5倍缩放
            float cardSpacing = baseCardWidth * cardScale * 0.4f; // 间距为缩放后宽度的40%

            // 设置每张卡牌的组件和位置
            for (int i = 0; i < cardObjects.Count; i++)
            {
                GameObject cardObj = cardObjects[i];

                // 使用PDKCardHelper设置卡牌组件
                m_CardHelper.SetupCardComponent(cardObj, cards[i]);

                // 设置卡牌Transform
                RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // 设置锚点为左对齐
                    rectTransform.anchorMin = new Vector2(0f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0f, 0.5f);
                    rectTransform.pivot = new Vector2(0f, 0.5f);

                    // 设置大小
                    rectTransform.sizeDelta = new Vector2(baseCardWidth, baseCardHeight);

                    // 设置缩放
                    rectTransform.localScale = new Vector3(cardScale, cardScale, 1f);

                    // 计算位置（从中心对称排列）
                    float totalWidth = (cardObjects.Count - 1) * cardSpacing;
                    float startOffset = -totalWidth / 2f;
                    float xPos = startOffset + i * cardSpacing;

                    rectTransform.anchoredPosition = new Vector2(xPos, 0);

                    // 重置旋转
                    rectTransform.localRotation = Quaternion.identity;
                }
            }

            // GF.LogInfo_wl($"[PDKPlayBack] 更新玩家{serverSeat}出牌区，共{cardObjects.Count}张牌，缩放:{cardScale}");
        }

        // 【新增】当出牌为空时显示"不要"标识，否则隐藏
        if (buyaoIndicators != null && localSeat < buyaoIndicators.Length && buyaoIndicators[localSeat] != null)
        {
            if (cards == null || cards.Count == 0)
            {
                buyaoIndicators[localSeat].SetActive(true);

                // 【动画】播放弹性缩放动画: 1.0 -> 1.2 -> 1.0
                RectTransform buyaoRect = buyaoIndicators[localSeat].GetComponent<RectTransform>();
                if (buyaoRect != null)
                {
                    buyaoRect.localScale = Vector3.one;
                    buyaoRect.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack).OnComplete(() =>
                    {
                        buyaoRect.DOScale(1.0f, 0.15f).SetEase(Ease.InBack);
                    });
                }
                // GF.LogInfo_wl($"[PDKPlayBack] 玩家{serverSeat}不要，显示buyao标识并播放动画");
            }
            else
            {
                buyaoIndicators[localSeat].SetActive(false);
            }
        }
    }

    /// <summary>
    /// 清空出牌区
    /// </summary>
    private void ClearPlayArea(int localSeat)
    {
        if (localSeat < 0 || localSeat >= PlayArea.Length || PlayArea[localSeat] == null)
            return;

        // 回收卡牌到对象池
        if (m_CardHelper != null)
        {
            m_CardHelper.RecycleAllCardsInParent(PlayArea[localSeat].transform);
        }

        // 【新增】清空出牌区时隐藏buyao标识
        if (buyaoIndicators != null && localSeat < buyaoIndicators.Length && buyaoIndicators[localSeat] != null)
        {
            buyaoIndicators[localSeat].SetActive(false);
        }
    }



    #endregion

    #region 按钮事件

    private void OnClickPlay()
    {
        if (m_IsPlaying) return;
        m_IsPlaying = true;
        if (playBtn != null) playBtn.SetActive(false);
        if (stopBtn != null) stopBtn.SetActive(true);
        m_PlayCoroutine = StartCoroutine(AutoPlayRoutine());
    }

    private void OnClickStop()
    {
        StopAutoPlay();
    }

    private void OnClickAgo()
    {
        StopAutoPlay();
        StepBackward();
    }

    private void OnClickAfter()
    {
        StopAutoPlay();
        StepForward();
    }

    private void OnClickReplay()
    {
        // 停止当前播放
        StopAutoPlay();

        // 重置回放到初始状态
        ResetPlayback();

    }

    private void OnClickSpeed()
    {
        m_CurrentSpeedIndex = (m_CurrentSpeedIndex + 1) % m_PlaybackSpeeds.Length;
        m_CurrentPlaybackSpeed = m_PlaybackSpeeds[m_CurrentSpeedIndex];
        // GF.LogInfo_wl($"[PDKPlayBack] 播放速度切换为: {m_CurrentPlaybackSpeed}x");

        // 如果正在播放,重启协程以应用新速度
        if (m_IsPlaying)
        {
            StopCoroutine(m_PlayCoroutine);
            m_PlayCoroutine = StartCoroutine(AutoPlayRoutine());
        }
    }

    private void OnClickJumpToStart()
    {
        StopAutoPlay();
        ResetPlayback();
    }

    private void OnClickJumpToEnd()
    {
        StopAutoPlay();
        while (m_CurrentStepIndex < m_Data.Option.Count)
        {
            StepForward();
        }
    }

    private void OnProgressSliderChanged(float value)
    {
        if (m_Data == null || m_Data.Option.Count == 0) return;

        // 防止播放时拖动造成冲突
        if (m_IsPlaying) return;

        int targetStep = Mathf.RoundToInt(value * m_Data.Option.Count);
        targetStep = Mathf.Clamp(targetStep, 0, m_Data.Option.Count);

        JumpToStep(targetStep);
    }

    #endregion

    #region 核心逻辑

    private void StopAutoPlay()
    {
        m_IsPlaying = false;
        if (m_PlayCoroutine != null) StopCoroutine(m_PlayCoroutine);
        if (playBtn != null) playBtn.SetActive(true);
        if (stopBtn != null) stopBtn.SetActive(false);
    }

    private IEnumerator AutoPlayRoutine()
    {
        while (m_CurrentStepIndex < m_Data.Option.Count)
        {
            StepForward();
            // 根据播放速度调整等待时间
            float waitTime = 1.5f / m_CurrentPlaybackSpeed;
            yield return new WaitForSeconds(waitTime);
        }
        StopAutoPlay();
    }

    private void StepForward()
    {
        if (m_CurrentStepIndex >= m_Data.Option.Count)
        {
            StopAutoPlay();
            return;
        }

        var stepData = m_Data.Option[m_CurrentStepIndex];
        int serverSeat = GetSeatFromKey(stepData.Option.Key);
        List<int> playedCards = new List<int>(stepData.Option.Vals);

        // 保存快照
        SaveSnapshot(serverSeat, playedCards);

        // 处理出牌
        ProcessPlayCard(serverSeat, playedCards); 

        // 【新增】处理积分变化动画
        ProcessScoreChange(stepData);

        m_CurrentStepIndex++;

        // 【积分显示】检测是否为最后一步
        // GF.LogInfo($"[PDKPlayBack] 当前步数: {m_CurrentStepIndex}/{m_Data.Option.Count}");
        if (m_CurrentStepIndex >= m_Data.Option.Count)
        {
            // 【新增】同步最终积分到当前积分显示
            SyncFinalScoresToCurrent();

            if (over != null) over.SetActive(true);
            ShowFinalScores();
        }
        else
        {
            if (over != null) over.SetActive(false);
            HideFinalScores();
        }
    }

    private void StepBackward()
    {
        if (m_CurrentStepIndex <= 0 || m_SnapshotStack.Count == 0)
        {
            // GF.LogInfo_wl("[PDKPlayBack] 已经是第一步，无法后退");
            return;
        }

        PlaybackSnapshot snapshot = m_SnapshotStack.Pop();

        // 恢复手牌数据
        m_HandCards.Clear();
        foreach (var kvp in snapshot.HandCards)
        {
            m_HandCards[kvp.Key] = new List<int>(kvp.Value);
        }

        // 【新增】恢复积分数据
        m_CurrentScores.Clear();
        foreach (var kvp in snapshot.CurrentScores)
        {
            m_CurrentScores[kvp.Key] = kvp.Value;
        }

        // 【新增】更新所有玩家的积分显示
        foreach (var kvp in m_CurrentScores)
        {
            UpdatePlayerScoreText(kvp.Key);
        }

        m_CurrentStepIndex--;

        // 【关键】回退时隐藏结算面板和积分
        HideFinalScores();
        if (over != null) over.SetActive(false);

        // 【优化】使用快照恢复，只更新受影响的玩家
        int affectedServerSeat = snapshot.ServerSeat;
        int localSeat = ServerSeatToLocalSeat(affectedServerSeat);

        // 【优化】将出牌区的卡牌移回手牌区，而不是重新创建
        if (localSeat >= 0 && localSeat < PlayArea.Length && PlayArea[localSeat] != null &&
            localSeat < PlayerHandCardAreas.Length && PlayerHandCardAreas[localSeat] != null)
        {
            Transform playAreaParent = PlayArea[localSeat].transform;
            Transform handCardParent = PlayerHandCardAreas[localSeat].transform;

            // 将出牌区的卡牌移回手牌区
            List<GameObject> playedCardObjects = new List<GameObject>();
            for (int i = playAreaParent.childCount - 1; i >= 0; i--)
            {
                Transform child = playAreaParent.GetChild(i);
                if (child.GetComponent<PDKCard>() != null)
                {
                    playedCardObjects.Add(child.gameObject);
                    child.SetParent(handCardParent, false);
                }
            }

            // 对手牌区的所有卡牌重新排序和布局
            List<int> currentCards = m_HandCards[affectedServerSeat];
            if (m_CardManager != null && currentCards.Count > 0)
            {
                currentCards = m_CardManager.SortCards(currentCards);
            }

            // 重新排列所有手牌
            List<Transform> allHandCards = new List<Transform>();
            for (int i = 0; i < handCardParent.childCount; i++)
            {
                allHandCards.Add(handCardParent.GetChild(i));
            }

            // 【优化】提前计算布局参数，避免创建临时对象
            float cardScale = localSeat == 0 ? 1.0f : 0.8f;
            float cardWidth = PDKUIManager.s_cardWidth;
            float cardSpacing = PDKUIManager.s_cardSpacing;
            float cardHeight = PDKUIManager.s_cardHeight;

            // 【优化】预缓存组件，避免重复GetComponent
            Dictionary<int, RectTransform> cardTransformMap = new Dictionary<int, RectTransform>();
            for (int i = 0; i < allHandCards.Count; i++)
            {
                PDKCard card = allHandCards[i].GetComponent<PDKCard>();
                RectTransform rect = allHandCards[i].GetComponent<RectTransform>();
                if (card != null && rect != null)
                {
                    cardTransformMap[card.numD] = rect;
                }
            }

            // 【优化】批量计算位置参数
            float totalWidth = (currentCards.Count - 1) * cardSpacing + cardWidth;
            float startX = -totalWidth / 2f;
            Vector2 anchorCenter = new Vector2(0.5f, 0.5f);
            Vector3 scale = new Vector3(cardScale, cardScale, 1f);
            Vector2 sizeDelta = new Vector2(cardWidth, cardHeight);

            // 【优化】批量设置属性，减少逐个操作
            for (int i = 0; i < currentCards.Count; i++)
            {
                int cardId = currentCards[i];
                if (cardTransformMap.TryGetValue(cardId, out RectTransform rectTransform))
                {
                    rectTransform.anchorMin = anchorCenter;
                    rectTransform.anchorMax = anchorCenter;
                    rectTransform.pivot = anchorCenter;
                    rectTransform.sizeDelta = sizeDelta;
                    rectTransform.localScale = scale;
                    rectTransform.anchoredPosition = new Vector2(startX + i * cardSpacing, 0);
                    rectTransform.transform.SetSiblingIndex(i);
                }
            }

            // GF.LogInfo_wl($"[PDKPlayBack] 后退：将{playedCardObjects.Count}张出牌移回手牌区，总共{currentCards.Count}张手牌");
        }

        // 更新该玩家的手牌数量文本
        UpdateHandCardCountText(affectedServerSeat);

        // 隐藏该玩家的buyao标识
        if (buyaoIndicators != null && localSeat < buyaoIndicators.Length && buyaoIndicators[localSeat] != null)
        {
            buyaoIndicators[localSeat].SetActive(false);
        }

        // 显示上一步的出牌（如果有）
        if (m_CurrentStepIndex > 0 && m_CurrentStepIndex <= m_Data.Option.Count)
        {
            var prevStepData = m_Data.Option[m_CurrentStepIndex - 1];
            int prevServerSeat = GetSeatFromKey(prevStepData.Option.Key);
            List<int> prevPlayedCards = new List<int>(prevStepData.Option.Vals);
            UpdatePlayAreaUI(prevServerSeat, prevPlayedCards);
        }

        // GF.LogInfo_wl($"[PDKPlayBack] 后退到第 {m_CurrentStepIndex} 步，优化后退速度");
    }

    private void SaveSnapshot(int serverSeat, List<int> playedCards)
    {
        PlaybackSnapshot snapshot = new PlaybackSnapshot(m_HandCards, serverSeat, playedCards, m_CurrentScores);
        m_SnapshotStack.Push(snapshot);
    }

    private void ProcessPlayCard(int serverSeat, List<int> cards)
    {
        // 更新手牌数据
        if (m_HandCards.ContainsKey(serverSeat))
        {
            foreach (var card in cards)
            {
                m_HandCards[serverSeat].Remove(card);
            }
        }

        // 【优化】只删除打出的牌，并复用到出牌区，保持手牌间距不变
        RemovePlayedCardsAndMoveToPlayArea(serverSeat, cards);

        // 更新手牌数量文本
        UpdateHandCardCountText(serverSeat);
    }

    /// <summary>
    /// 【新增】处理积分变化动画（金币飞行和飘分动画）
    /// </summary>
    private void ProcessScoreChange(NetMsg.Msg_RunFastOption stepData)
    {
        if (stepData.Score == null || stepData.Score.Count == 0)
            return;

        // 收集输家和赢家信息
        List<(long playerId, double score)> losers = new List<(long, double)>();
        List<(long playerId, double score)> winners = new List<(long, double)>();

        foreach (var scoreData in stepData.Score)
        {
            long playerId = scoreData.Key;
            double scoreChange = scoreData.Val;

            if (scoreChange == 0) continue;

            // 更新当前积分
            if (!m_CurrentScores.ContainsKey(playerId))
            {
                m_CurrentScores[playerId] = 0;
            }
            m_CurrentScores[playerId] += scoreChange;

            // 更新积分显示
            UpdatePlayerScoreText(playerId);

            // 播放飘分动画
            ShowScoreChangeAnimation(playerId, scoreChange);

            // 收集输赢信息用于金币飞行
            if (scoreChange < 0)
            {
                losers.Add((playerId, scoreChange));
            }
            else if (scoreChange > 0)
            {
                winners.Add((playerId, scoreChange));
            }
        }

        // 播放金币飞行动画
        if (losers.Count > 0 && winners.Count > 0)
        {
            StartCoroutine(PlayCoinFlyAnimationDelayed(losers, winners));
        }
    }

    /// <summary>
    /// 【新增】更新玩家积分显示
    /// </summary>
    private void UpdatePlayerScoreText(long playerId)
    {
        if (!m_PlayerSeats.TryGetValue(playerId, out int serverSeat))
            return;

        int localSeat = ServerSeatToLocalSeat(serverSeat);
        if (localSeat < 0 || localSeat >= scoreTexts.Length || scoreTexts[localSeat] == null)
            return;

        if (m_CurrentScores.TryGetValue(playerId, out double currentScore))
        {
            scoreTexts[localSeat].text = $"积分: {FormatCoin((long)currentScore)}";
        }
    }

    /// <summary>
    /// 【新增】显示飘分动画（在玩家头像位置显示积分变化）
    /// </summary>
    private void ShowScoreChangeAnimation(long playerId, double scoreChange)
    {
        if (!m_PlayerSeats.TryGetValue(playerId, out int serverSeat))
            return;

        int localSeat = ServerSeatToLocalSeat(serverSeat);
        if (localSeat < 0 || localSeat >= PlayerInfoPanels.Length || PlayerInfoPanels[localSeat] == null)
            return;

        // 查找头像背景或者信息面板作为特效容器
        Transform headBg = PlayerInfoPanels[localSeat].transform.Find("head");
        if (headBg == null) headBg = PlayerInfoPanels[localSeat].transform;

        // 根据分数正负选择特效路径
        string effPath = scoreChange >= 0
            ? AssetsPath.GetPrefab("UI/MaJiang/Game/ScoreWinEff")
            : AssetsPath.GetPrefab("UI/MaJiang/Game/ScoreLoseEff");

        // 加载并创建得分特效对象
        GF.UI.LoadPrefab(effPath, (gameObject) =>
        {
            if (gameObject == null) return;

            // 在头像上创建特效
            Transform effectContainer = headBg.Find("EffContainer");
            if (effectContainer == null) effectContainer = headBg;

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
                sequence.SetAutoKill(true);

                // 阶段1：停留显示 (1.5秒)
                sequence.AppendInterval(1.5f);

                // 阶段2：向上移动并渐隐 (0.7秒)
                sequence.Append(rectTransform.DOAnchorPosY(50f, 0.7f).SetEase(Ease.OutQuad));
                sequence.Join(scoreText.DOFade(0f, 0.7f).SetEase(Ease.InQuad));

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

    /// <summary>
    /// 【新增】延迟播放金币飞行动画（给飘分动画留出时间）
    /// </summary>
    private IEnumerator PlayCoinFlyAnimationDelayed(List<(long playerId, double score)> losers, List<(long playerId, double score)> winners)
    {
        // 等待0.2秒，让飘分动画先播放
        yield return new WaitForSeconds(0.2f);

        // 播放金币飞行动画
        yield return StartCoroutine(CoinFlyAnimation(losers, winners));
    }

    /// <summary>
    /// 【新增】金币飞行动画协程（参考PDKGamePanel）
    /// </summary>
    private IEnumerator CoinFlyAnimation(List<(long playerId, double score)> losers, List<(long playerId, double score)> winners)
    {
        // 金币飞行参数
        const float coinFlyInterval = 0.01f; // 每个金币间隔时间
        const float coinFlyDuration = 0.5f;  // 金币飞行时长
        const int coinFlyCount = 10;         // 每次飞行的金币数量

        List<GameObject> flyingCoins = new List<GameObject>();

        // 播放音效
        Sound.PlayEffect("niuniu/win.mp3");

        // 从输家飞向赢家
        for (int k = 0; k < coinFlyCount; k++)
        {
            foreach (var loser in losers)
            {
                if (!m_PlayerSeats.TryGetValue(loser.playerId, out int loserServerSeat))
                    continue;

                int loserLocalSeat = ServerSeatToLocalSeat(loserServerSeat);
                if (loserLocalSeat < 0 || loserLocalSeat >= PlayerInfoPanels.Length || PlayerInfoPanels[loserLocalSeat] == null)
                    continue;

                Transform loserHeadBg = PlayerInfoPanels[loserLocalSeat].transform.Find("head");
                if (loserHeadBg == null) continue;

                foreach (var winner in winners)
                {
                    if (!m_PlayerSeats.TryGetValue(winner.playerId, out int winnerServerSeat))
                        continue;

                    int winnerLocalSeat = ServerSeatToLocalSeat(winnerServerSeat);
                    if (winnerLocalSeat < 0 || winnerLocalSeat >= PlayerInfoPanels.Length || PlayerInfoPanels[winnerLocalSeat] == null)
                        continue;

                    Transform winnerHeadBg = PlayerInfoPanels[winnerLocalSeat].transform.Find("head");
                    if (winnerHeadBg == null) continue;

                    // 创建金币实例
                    if (Coin == null) continue;
                    
                    GameObject coin = Instantiate(Coin, transform);
                    coin.SetActive(true);
                    coin.transform.position = loserHeadBg.position;
                    flyingCoins.Add(coin);

                    // 使用DOTween移动金币
                    coin.transform.DOMove(winnerHeadBg.position, coinFlyDuration).OnComplete(() =>
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
    }

    /// <summary>
    /// 删除已打出的手牌对象并移动到出牌区（复用卡牌对象）
    /// </summary>
    /// </summary>
    private void RemovePlayedCardsAndMoveToPlayArea(int serverSeat, List<int> playedCards)
    {
        int localSeat = ServerSeatToLocalSeat(serverSeat);
        if (localSeat < 0 || localSeat >= PlayerHandCardAreas.Length)
            return;

        Transform handCardParent = PlayerHandCardAreas[localSeat].transform;
        Transform playAreaParent = PlayArea[localSeat].transform;

        // 清空出牌区
        if (m_CardHelper != null)
        {
            m_CardHelper.RecycleAllCardsInParent(playAreaParent);
        }

        // 找到要删除的手牌对象并移动到出牌区
        List<GameObject> cardsToMove = new List<GameObject>();
        List<Transform> remainingCards = new List<Transform>();

        for (int i = 0; i < handCardParent.childCount; i++)
        {
            Transform child = handCardParent.GetChild(i);
            PDKCard cardComponent = child.GetComponent<PDKCard>();
            if (cardComponent != null)
            {
                if (playedCards.Contains(cardComponent.numD))
                {
                    cardsToMove.Add(child.gameObject);
                }
                else
                {
                    remainingCards.Add(child);
                }
            }
        }

        // 对要打出的牌进行排序
        if (m_CardManager != null && playedCards.Count > 0)
        {
            playedCards = m_CardManager.SortCards(playedCards);
        }

        // 【优化】提前计算所有布局参数,减少重复计算
        float baseCardWidth = PDKUIManager.s_cardWidth;
        float baseCardHeight = baseCardWidth * 1.4f;
        float cardScale = 0.5f;
        float cardSpacing = baseCardWidth * cardScale * 0.4f;
        float totalWidth = (playedCards.Count - 1) * cardSpacing + baseCardWidth * cardScale;
        float startOffset = -totalWidth / 2f;

        Vector2 anchorCenter = new Vector2(0.5f, 0.5f);
        Vector3 scale = new Vector3(cardScale, cardScale, 1f);
        Vector2 sizeDelta = new Vector2(baseCardWidth, baseCardHeight);

        // 【优化】批量移动并设置卡牌
        for (int i = 0; i < playedCards.Count; i++)
        {
            int cardId = playedCards[i];
            GameObject cardObj = cardsToMove.Find(c => c.GetComponent<PDKCard>()?.numD == cardId);

            if (cardObj != null)
            {
                // 移动到出牌区
                cardObj.transform.SetParent(playAreaParent, false);

                RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // 【优化】批量设置属性,减少逐个操作
                    rectTransform.anchorMin = anchorCenter;
                    rectTransform.anchorMax = anchorCenter;
                    rectTransform.pivot = anchorCenter;
                    rectTransform.sizeDelta = sizeDelta;
                    rectTransform.localScale = scale;
                    rectTransform.anchoredPosition = new Vector2(startOffset + i * cardSpacing, 0);
                    rectTransform.localRotation = Quaternion.identity;
                }
            }
        }

        // 【新增】当出牌为空时显示"不要"标识，否则隐藏
        if (buyaoIndicators != null && localSeat < buyaoIndicators.Length && buyaoIndicators[localSeat] != null)
        {
            if (playedCards.Count == 0)
            {
                buyaoIndicators[localSeat].SetActive(true);
                // 【动画】播放弹性缩放动画: 1.0 -> 1.2 -> 1.0
                RectTransform buyaoRect = buyaoIndicators[localSeat].GetComponent<RectTransform>();
                if (buyaoRect != null)
                {
                    buyaoRect.localScale = Vector3.one;
                    buyaoRect.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack).OnComplete(() =>
                    {
                        buyaoRect.DOScale(1.0f, 0.15f).SetEase(Ease.InBack);
                    });
                }
                // 【音效】播放不要音效
                Sound.PlayEffect("pdk/pt/man/control/buyao.mp3");
                // GF.LogInfo_wl($"[PDKPlayBack] 玩家{serverSeat}不要，显示buyao标识并播放动画和音效");
            }
            else
            {
                buyaoIndicators[localSeat].SetActive(false);
                // 【音效】播放出牌物理音效
                Sound.PlayEffect("pdk/chupai.mp3");
                // 【音效】播放出牌语音音效
                PlayCardSound(playedCards);
                // 【特效】检测牌型并播放特效
                PlayCardTypeEffect(localSeat, playedCards);
            }
        }

        // 【修改】打完牌后重新排列手牌，但保持原始间距不变
        RearrangeRemainingHandCards(localSeat, remainingCards);

        // GF.LogInfo_wl($"[PDKPlayBack] 玩家{serverSeat}打出{playedCards.Count}张牌，剩余{remainingCards.Count}张手牌，保持原始间距");
    }

    /// <summary>
    /// 重新排列剩余手牌，保持原始间距不变
    /// </summary>
    private void RearrangeRemainingHandCards(int localSeat, List<Transform> remainingCards)
    {
        if (remainingCards.Count == 0) return;

        // 【关键修改】使用保存的初始间距，而不是从当前牌中计算
        if (remainingCards.Count > 0)
        {
            RectTransform firstCard = remainingCards[0].GetComponent<RectTransform>();
            if (firstCard == null) return;

            // 从现有卡牌读取原始尺寸和缩放
            float cardWidth = firstCard.sizeDelta.x;
            float cardHeight = firstCard.sizeDelta.y;
            float cardScale = firstCard.localScale.x;

            // 【关键修复】使用保存的初始间距，而不是从当前牌的位置计算
            // 通过遍历找到对应的serverSeat
            int serverSeat = -1;
            foreach (var kvp in m_HandCards)
            {
                if (ServerSeatToLocalSeat(kvp.Key) == localSeat)
                {
                    serverSeat = kvp.Key;
                    break;
                }
            }

            float cardSpacing;

            if (serverSeat >= 0 && m_InitialCardSpacing.ContainsKey(serverSeat))
            {
                // 使用保存的初始间距
                cardSpacing = m_InitialCardSpacing[serverSeat];
                // GF.LogInfo_wl($"[PDKPlayBack] 使用玩家{serverSeat}保存的初始间距: {cardSpacing}");
            }
            else
            {
                // 如果没有保存的间距，使用默认计算（不应该发生）
                cardSpacing = cardWidth * PDKConstants.IDEAL_SPACING_RATIO;
                GF.LogWarning($"[PDKPlayBack] LocalSeat={localSeat}, ServerSeat={serverSeat}没有保存的初始间距，使用默认值: {cardSpacing}");
            }

            // 【关键】使用原始间距重新居中排列剩余手牌
            float totalWidth = (remainingCards.Count - 1) * cardSpacing + cardWidth;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < remainingCards.Count; i++)
            {
                RectTransform rectTransform = remainingCards[i].GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // 保持原始尺寸和缩放不变，只更新X位置使其居中
                    float xPos = startX + i * cardSpacing;
                    rectTransform.anchoredPosition = new Vector2(xPos, rectTransform.anchoredPosition.y);
                }
            }

            // GF.LogInfo_wl($"[PDKPlayBack] 重新排列{remainingCards.Count}张手牌，保持原始间距{cardSpacing}");
        }
    }

    /// <summary>
    /// 更新手牌数量文本
    /// </summary>
    private void UpdateHandCardCountText(int serverSeat)
    {
        int localSeat = ServerSeatToLocalSeat(serverSeat);
        if (localSeat < 0 || localSeat >= handCardCountTexts.Length)
            return;

        if (handCardCountTexts[localSeat] != null && m_HandCards.ContainsKey(serverSeat))
        {
            int cardCount = m_HandCards[serverSeat].Count;
            handCardCountTexts[localSeat].text = cardCount.ToString();
            
            // 【新增】报警特效处理：只剩1张牌时显示报警特效并播放音效
            if (baojing != null && localSeat < baojing.Length && baojing[localSeat] != null)
            {
                bool shouldShow = (cardCount == 1);
                bool wasShowing = baojing[localSeat].activeSelf;
                
                baojing[localSeat].SetActive(shouldShow);
                
                // 如果是从不报警变为报警，且当前是正常播放状态（不是后退），则播放报警音效
                if (shouldShow && !wasShowing && m_IsPlaying)
                {
                    PlayEffectSound("报警");
                }
            }
            
            // GF.LogInfo_wl($"[PDKPlayBack] 更新玩家{serverSeat}剩余手牌数量: {cardCount}");
        }
    }

    private void ResetPlayback()
    {
        // GF.LogInfo_wl("[PDKPlayBack] 开始重置回放...");

        m_CurrentStepIndex = 0;
        m_SnapshotStack.Clear();

        // 清理特效播放标志
        effectPlayingFlags.Clear();

        // 【关键】重置时隐藏结算面板和积分
        HideFinalScores();
        if (over != null) over.SetActive(false);

        // 【新增】重置积分为0
        m_CurrentScores.Clear();
        foreach (var kvp in m_PlayerSeats)
        {
            long playerId = kvp.Key;
            m_CurrentScores[playerId] = 0;
            UpdatePlayerScoreText(playerId);
        }

        // 恢复初始手牌数据
        m_HandCards.Clear();
        foreach (var kvp in m_InitialHandCards)
        {
            m_HandCards[kvp.Key] = new List<int>(kvp.Value);
        }

        // 【优化】只更新必要的UI,不重新创建卡牌
        // 1. 清空所有出牌区
        for (int i = 0; i < PlayArea.Length; i++)
        {
            ClearPlayArea(i);
        }

        // 2. 恢复每个玩家的手牌显示(复用现有卡牌对象,只调整可见性和位置)
        foreach (var kvp in m_HandCards)
        {
            int serverSeat = kvp.Key;
            int localSeat = ServerSeatToLocalSeat(serverSeat);
            if (localSeat < 0 || localSeat >= PlayerHandCardAreas.Length)
                continue;

            Transform handCardParent = PlayerHandCardAreas[localSeat].transform;
            List<int> initialCards = kvp.Value;

            // 【优化】检查是否可以复用现有卡牌对象
            int existingCardCount = handCardParent.childCount;
            bool canReuse = (existingCardCount == initialCards.Count);

            if (!canReuse)
            {
                // 卡牌数量不匹配,需要重新创建
                // GF.LogInfo_wl($"[PDKPlayBack] 座位{serverSeat}卡牌数量不匹配({existingCardCount}!={initialCards.Count}),重新创建");
                UpdateHandCardUI(serverSeat);
            }
            else
            {
                // 【快速路径】卡牌数量匹配
                // 验证卡牌值是否也匹配(排序后比较)
                List<int> sortedInitialCards = m_CardManager != null
                    ? m_CardManager.SortCards(new List<int>(initialCards))
                    : new List<int>(initialCards);

                bool valuesMatch = true;
                for (int i = 0; i < sortedInitialCards.Count; i++)
                {
                    Transform cardTransform = handCardParent.GetChild(i);
                    var pdkCard = cardTransform.GetComponent<PDKCard>();
                    if (pdkCard == null || pdkCard.value != sortedInitialCards[i])
                    {
                        valuesMatch = false;
                        break;
                    }
                }

                if (!valuesMatch)
                {
                    // 卡牌值不匹配,重新创建
                    // GF.LogInfo_wl($"[PDKPlayBack] 座位{serverSeat}卡牌值不匹配,重新创建");
                    UpdateHandCardUI(serverSeat);
                }
                else
                {
                    // 【最快路径】数量和值都匹配,只需恢复初始位置和状态
                    float cardWidth = PDKConstants.BASE_CARD_WIDTH;
                    float spacing = m_InitialCardSpacing.ContainsKey(serverSeat)
                        ? m_InitialCardSpacing[serverSeat]
                        : cardWidth * PDKConstants.IDEAL_SPACING_RATIO;

                    for (int i = 0; i < existingCardCount; i++)
                    {
                        Transform cardTransform = handCardParent.GetChild(i);

                        // 确保卡牌可见和可交互
                        cardTransform.gameObject.SetActive(true);

                        // 恢复初始位置
                        float xPos = i * spacing - (sortedInitialCards.Count - 1) * spacing / 2f;
                        cardTransform.localPosition = new Vector3(xPos, 0, 0);
                        cardTransform.localScale = Vector3.one;
                    }

                    // GF.LogInfo_wl($"[PDKPlayBack] 座位{serverSeat}快速恢复初始状态(复用{existingCardCount}张卡牌,无需重新创建)");
                }
            }

        // 更新手牌数量文本
            UpdateHandCardCountText(serverSeat);
        }

        // 【新增】重置时隐藏结算面板和积分
        HideFinalScores();

        // GF.LogInfo_wl("[PDKPlayBack] 重置回放完成");
    }

    private void JumpToStep(int targetStep)
    {
        if (m_Data == null || targetStep < 0 || targetStep > m_Data.Option.Count)
            return;

        if (targetStep < m_CurrentStepIndex)
        {
            ResetPlayback();
        }

        while (m_CurrentStepIndex < targetStep)
        {
            StepForward();
        }

        // GF.LogInfo_wl($"[PDKPlayBack] 跳转到第 {targetStep} 步");
    }

    #endregion

    #region 辅助方法

    private int GetSeatFromKey(long playerId)
    {
        if (m_PlayerSeats.TryGetValue(playerId, out int seat))
        {
            return seat;
        }
        GF.LogError($"[PDKPlayBack] 未找到PlayerId={playerId}的座位");
        return 0;
    }

    /// <summary>
    /// 将服务器座位转换为本地座位（自己永远在本地座位0）
    /// 参考PDKGamePanel的逻辑：使用Util.GetPosByServerPos_pdk
    /// 规则：自己在0号位（下方），下家在1号位（右边），上家在2号位（左边）
    /// </summary>
    private int ServerSeatToLocalSeat(int serverSeat)
    {
        // 如果是自己，永远返回0
        if (serverSeat == m_MyServerSeat)
        {
            return 0;
        }
        // 将int座位转换为Position枚举
        NetMsg.Position myPos = (NetMsg.Position)m_MyServerSeat;
        NetMsg.Position targetPos = (NetMsg.Position)serverSeat;
        // 直接使用新的工具方法转换（自动处理2人和3人游戏）
        int localSeat = Util.GetPosByServerPos_pdk(myPos, targetPos, m_TotalPlayerNum);
        // GF.LogInfo_wl($"[PDKPlayBack] 座位转换：MyServerSeat={m_MyServerSeat}, TargetServerSeat={serverSeat}, LocalSeat={localSeat}, PlayerNum={m_TotalPlayerNum}");
        return localSeat;
    }

    #endregion

    #region 积分显示系统

    /// <summary>
    /// 【新增】同步最终积分到当前积分(确保累加和最终结果一致)
    /// </summary>
    private void SyncFinalScoresToCurrent()
    {
        if (m_Data == null || m_Data.Score == null || m_Data.Score.Count == 0)
            return;

        foreach (var scoreData in m_Data.Score)
        {
            long playerId = scoreData.Key;
            double finalScore = scoreData.Val;

            // 更新当前积分为最终积分
            m_CurrentScores[playerId] = finalScore;

            // 更新积分显示
            UpdatePlayerScoreText(playerId);
        }
    }

    /// <summary>
    /// 显示最终积分
    /// </summary>
    private void ShowFinalScores()
    {
        // GF.LogInfo("[PDKPlayBack] ========== ShowFinalScores 方法被调用 ==========");

        // 【调试】检查所有条件
        // GF.LogInfo($"[PDKPlayBack] 检查条件: m_Data={m_Data != null}, m_Data.Score={m_Data?.Score != null}, Count={m_Data?.Score?.Count ?? 0}");

        // 【修改】使用 m_Data.Score 字段而不是 m_Data.Pos
        if (m_Data == null)
        {
            GF.LogWarning("[PDKPlayBack] m_Data 为空，无法显示积分");
            return;
        }

        if (m_Data.Score == null)
        {
            GF.LogWarning("[PDKPlayBack] m_Data.Score 为空，无法显示积分");
            return;
        }

        if (m_Data.Score.Count == 0)
        {
            GF.LogWarning("[PDKPlayBack] m_Data.Score.Count = 0，没有积分数据");
            return;
        }

        // GF.LogInfo($"[PDKPlayBack] ✅ 开始显示最终积分，共{m_Data.Score.Count}个玩家数据");

        // 遍历积分数据（LongDouble类型）
        foreach (var scoreData in m_Data.Score)
        {
            long playerId = scoreData.Key;
            double score = scoreData.Val; // 【修改】使用 double 类型

            // 获取玩家服务器座位
            if (!m_PlayerSeats.TryGetValue(playerId, out int serverSeat))
            {
                GF.LogWarning($"[PDKPlayBack] 找不到玩家ID {playerId} 的座位信息");
                continue;
            }

            // 转换为本地座位
            int localSeat = ServerSeatToLocalSeat(serverSeat);
            if (localSeat < 0 || localSeat >= PlayerResultPanels.Length)
            {
                GF.LogWarning($"[PDKPlayBack] 本地座位 {localSeat} 超出范围");
                continue;
            }

            if (PlayerResultPanels[localSeat] == null)
            {
                GF.LogWarning($"[PDKPlayBack] 座位 {localSeat} 的结算面板为空");
                continue;
            }

            // 【调试】检查Text组件
            // GF.LogInfo($"[PDKPlayBack] 检查Text组件: Winscore[{localSeat}]={Winscore[localSeat] != null}, LoseScore[{localSeat}]={LoseScore[localSeat] != null}");

            // 显示结算面板
            PlayerResultPanels[localSeat].SetActive(true);
            // GF.LogInfo($"[PDKPlayBack] ✅ 已激活结算面板: localSeat={localSeat}");

            // 根据积分正负显示输赢（double类型）
            if (score >= 0)
            {
                // 赢分
                if (Winscore[localSeat] != null)
                {
                    // 【修改】格式化double类型，保留2位小数
                    Winscore[localSeat].text = "+" + score.ToString("F2");
                    Winscore[localSeat].gameObject.SetActive(true);
                }
                if (LoseScore[localSeat] != null)
                {
                    LoseScore[localSeat].gameObject.SetActive(false);
                }
                // GF.LogInfo($"[PDKPlayBack] 玩家 {playerId} (本地座位{localSeat}) 赢分: +{score:F2}");

                // 【新增】如果是自己赢了，播放胜利音效
                if (localSeat == 0)
                {
                    Sound.PlayEffect("pdk/pt/man/control/win.mp3");
                }
            }
            else
            {
                // 输分
                if (LoseScore[localSeat] != null)
                {
                    // 【修改】格式化double类型，保留2位小数（负数已经带负号）
                    LoseScore[localSeat].text = score.ToString("F2");
                    LoseScore[localSeat].gameObject.SetActive(true);
                }
                if (Winscore[localSeat] != null)
                {
                    Winscore[localSeat].gameObject.SetActive(false);
                }
                // GF.LogInfo($"[PDKPlayBack] 玩家 {playerId} (本地座位{localSeat}) 输分: {score:F2}");
            }
        }
    }

    /// <summary>
    /// 隐藏结算积分
    /// </summary>
    private void HideFinalScores()
    {
        if (PlayerResultPanels == null) return;

        for (int i = 0; i < PlayerResultPanels.Length; i++)
        {
            if (PlayerResultPanels[i] != null)
            {
                PlayerResultPanels[i].SetActive(false);
            }
        }

        // GF.LogInfo("[PDKPlayBack] 已隐藏所有结算面板");
    }

    #endregion

    #region 特效系统

    /// <summary>
    /// 播放牌型特效
    /// </summary>
    private void PlayCardTypeEffect(int localSeat, List<int> playedCards)
    {
        if (playedCards == null || playedCards.Count == 0)
        {
            GF.LogWarning("[PDKPlayBack特效] playedCards为空，无法播放特效");
            return;
        }
        
        if (EffectContainers == null)
        {
            GF.LogError("[PDKPlayBack特效] EffectContainers数组未初始化！请在Unity编辑器中设置");
            return;
        }
        
        if (localSeat < 0 || localSeat >= EffectContainers.Length)
        {
            GF.LogError($"[PDKPlayBack特效] localSeat={localSeat} 超出范围 [0, {EffectContainers.Length})");
            return;
        }
        
        if (EffectContainers[localSeat] == null)
        {
            GF.LogError($"[PDKPlayBack特效] EffectContainers[{localSeat}]未赋值！请在Unity编辑器中设置");
            return;
        }
        
        GF.LogInfo($"[PDKPlayBack特效] 准备播放特效 localSeat={localSeat}, 牌数={playedCards.Count}");

        Transform effectContainer = EffectContainers[localSeat].transform;

        // 【修复】如果正在播放特效，先停止前一个特效（清理子对象）
        if (effectPlayingFlags.TryGetValue(effectContainer, out bool isPlaying) && isPlaying)
        {
            // 清除特效容器中的所有子对象
            for (int i = effectContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(effectContainer.GetChild(i).gameObject);
            }
            effectPlayingFlags[effectContainer] = false;
        }

        // 转换为卡牌数据并检测牌型
        List<PDKCardData> cardDataList = ConvertCardIdsToCardDataList(playedCards);
        if (cardDataList == null || cardDataList.Count == 0) return;

        PDKConstants.CardType cardType = PDKRuleChecker.DetectCardType(cardDataList);

        // 根据牌型确定特效路径
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
                return;
        }

        if (string.IsNullOrEmpty(effPath)) return;

        // 调用通用的播放特效方法
        PlayEffect(effectContainer, effPath, effectType);
    }

    /// <summary>
    /// 【新增】播放春天/反春天特效
    /// </summary>
    /// <param name="chunType">春天类型: 0=无, 1=春天, 2=反春天</param>
    public void PlaySpringEffect(int chunType)
    {
        if (chunType == 0) return;

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
            return;
        }

        // 使用自己的特效容器（春天是全局特效）
        if (EffectContainers != null && EffectContainers.Length > 0 && EffectContainers[0] != null)
        {
            PlayEffect(EffectContainers[0].transform, effPath, effectType);
        }
    }

    /// <summary>
    /// 播放特效（通用方法，支持指定路径）
    /// </summary>
    private void PlayEffect(Transform effectContainer, string effPath, string effectType)
    {
        if (string.IsNullOrEmpty(effPath) || effectContainer == null) return;

        // 播放特效对应的音效
        PlayEffectSound(effectType);

        // 【修复】如果正在播放特效，先停止前一个特效（清理子对象）
        if (effectPlayingFlags.TryGetValue(effectContainer, out bool isPlaying) && isPlaying)
        {
            for (int i = effectContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(effectContainer.GetChild(i).gameObject);
            }
        }

        effectPlayingFlags[effectContainer] = true;
        GF.LogInfo($"[PDKPlayBack特效] 开始加载特效: {effectType}, 路径: {effPath}");

        GF.UI.LoadPrefab(effPath, (gameObject) =>
        {
            if (gameObject == null)
            {
                GF.LogError($"[PDKPlayBack特效] 加载预制体失败: {effectType}");
                effectPlayingFlags[effectContainer] = false;
                return;
            }

            GameObject effectObj = Instantiate(gameObject as GameObject, effectContainer);
            
            // 【修复】完整设置RectTransform，确保特效可见
            RectTransform rectTransform = effectObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.localScale = Vector3.one;
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                GF.LogInfo($"[PDKPlayBack特效] RectTransform设置完成: {effectType}");
            }

            effectObj.SetActive(true);
            
            float animationDuration = 2.5f;
            bool animationPlayed = false;

            // 尝试获取 SkeletonAnimation (3D/World)
            var spineAni = effectObj.GetComponent<Spine.Unity.SkeletonAnimation>();
            // 尝试获取 SkeletonGraphic (UI专用)
            var spineGraphic = effectObj.GetComponent<Spine.Unity.SkeletonGraphic>();

            if (spineAni != null)
            {
                try
                {
                    if (spineAni.Skeleton != null && spineAni.Skeleton.Data != null)
                    {
                        string animName = "animation";
                        if (spineAni.Skeleton.Data.FindAnimation(animName) == null)
                        {
                            var animations = spineAni.Skeleton.Data.Animations;
                            if (animations.Count > 0) animName = animations.Items[0].Name;
                        }

                        var track = spineAni.AnimationState.SetAnimation(0, animName, false);
                        if (track != null)
                        {
                            animationDuration = track.Animation.Duration;
                            animationPlayed = true;
                        }
                    }
                }
                catch (System.Exception e) { GF.LogWarning($"[PDKPlayBack特效] SkeletonAnimation播放失败: {e.Message}"); }
            }
            else if (spineGraphic != null)
            {
                try
                {
                    if (spineGraphic.Skeleton != null && spineGraphic.Skeleton.Data != null)
                    {
                        string animName = "animation";
                        if (spineGraphic.Skeleton.Data.FindAnimation(animName) == null)
                        {
                            var animations = spineGraphic.Skeleton.Data.Animations;
                            if (animations.Count > 0) animName = animations.Items[0].Name;
                        }

                        var track = spineGraphic.AnimationState.SetAnimation(0, animName, false);
                        if (track != null)
                        {
                            animationDuration = track.Animation.Duration;
                            animationPlayed = true;
                        }
                    }
                }
                catch (System.Exception e) { GF.LogWarning($"[PDKPlayBack特效] SkeletonGraphic播放失败: {e.Message}"); }
            }

            if (!animationPlayed)
            {
                GF.LogWarning($"[PDKPlayBack特效] 未找到Spine组件或动画播放失败: {effectType}");
            }

            // 【修复】增加缓冲时间，防止消失太快，且确保至少显示2秒
            float finalDuration = Mathf.Max(animationDuration + 0.8f, 2.0f);
            GF.LogInfo($"[PDKPlayBack特效] 特效显示中: {effectType}, 预计时长: {finalDuration}s");

            Destroy(effectObj, finalDuration);
            StartCoroutine(ClearEffectFlagAfterDelay(effectContainer, finalDuration));
        });
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
            case "三带二":
                soundPath = "pdk/sandaier.mp3";
                break;
            case "飞机":
                soundPath = "pdk/plane.mp3";
                break;
            case "顺子":
                soundPath = "pdk/shunzi.mp3";
                break;
            case "连对":
                soundPath = "pdk/liandui.mp3";
                break;
            case "炸弹":
                soundPath = "pdk/boom.mp3";
                break;
            case "春天":
            case "反春天":
                soundPath = "pdk/chuntian.mp3";
                break;
            case "报警":
                soundPath = "pdk/Warning.mp3";
                break;
            default:
                return;
        }

        if (!string.IsNullOrEmpty(soundPath))
        {
            Sound.PlayEffect(soundPath);
        }
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
        }
    }

    #endregion

    #region 音效系统

    /// <summary>
    /// 播放出牌音效
    /// </summary>
    private void PlayCardSound(List<int> cardIds)
    {
        if (cardIds == null || cardIds.Count == 0) return;

        try
        {
            // 转换为卡牌数据
            List<PDKCardData> cardDataList = ConvertCardIdsToCardDataList(cardIds);
            if (cardDataList == null || cardDataList.Count == 0) return;

            // 检测牌型
            PDKConstants.CardType cardType = PDKRuleChecker.DetectCardType(cardDataList);

            // 生成音效路径
            string soundPath = GetCardSoundPath(cardDataList, cardType);

            if (!string.IsNullOrEmpty(soundPath))
            {
                // 【关键修复】检查音频资源是否存在
                string fullPath = UtilityBuiltin.AssetsPath.GetSoundPath(soundPath);
                var hasAsset = GFBuiltin.Resource.HasAsset(fullPath);

                if (hasAsset == GameFramework.Resource.HasAssetResult.NotExist)
                {
                    // GF.LogError($"[PDKPlayBack音效] 音频资源不存在: {fullPath}");
                    return;
                }

                // GF.LogInfo_wl($"[PDKPlayBack音效] 播放音效: {soundPath}");
                Sound.PlayEffect(soundPath);
            }
        }
        catch (System.Exception e)
        {
            GF.LogError($"[PDKPlayBack音效] 播放出牌音效失败: {e.Message}");
        }
    }

    /// <summary>
    /// 根据牌型和数值生成音效路径
    /// </summary>
    private string GetCardSoundPath(List<PDKCardData> cards, PDKConstants.CardType cardType)
    {
        if (cards == null || cards.Count == 0) return null;

        string basePath = "pdk/pt/man/card/";
        string soundKey = null;

        switch (cardType)
        {
            case PDKConstants.CardType.SINGLE:
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
                soundKey = "zhadan";
                break;
            default:
                soundKey = GetCardValueSound((int)cards[0].value);
                break;
        }

        return string.IsNullOrEmpty(soundKey) ? null : basePath + soundKey + ".mp3";
    }

    /// <summary>
    /// 获取卡牌数值对应的音效名称
    /// </summary>
    private string GetCardValueSound(int value)
    {
        switch (value)
        {
            case 1: return "A";
            case 2: return "2";
            case 3: return "3";
            case 4: return "4";
            case 5: return "5";
            case 6: return "6";
            case 7: return "7";
            case 8: return "8";
            case 9: return "9";
            case 10: return "10";
            case 11: return "J";
            case 12: return "Q";
            case 13: return "K";
            case 14: return "A";
            case 15: return "2";
            default: return "3";
        }
    }

    /// <summary>
    /// 获取对子对应的音效名称
    /// </summary>
    private string GetPairSound(int value)
    {
        switch (value)
        {
            case 1: return "AA";
            case 2: return "22";
            case 3: return "33";
            case 4: return "44";
            case 5: return "55";
            case 6: return "66";
            case 7: return "77";
            case 8: return "88";
            case 9: return "99";
            case 10: return "1010";
            case 11: return "JJ";
            case 12: return "QQ";
            case 13: return "KK";
            case 14: return "AA";
            case 15: return "22";
            default: return "33";
        }
    }

    /// <summary>
    /// 转换卡牌ID列表为PDKCardData列表
    /// </summary>
    private List<PDKCardData> ConvertCardIdsToCardDataList(List<int> cardIds)
    {
        if (cardIds == null || cardIds.Count == 0) return null;

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
            else if (value == 2)
            {
                cardValue = PDKConstants.CardValue.Two;
            }
            else
            {
                cardValue = (PDKConstants.CardValue)value;
            }

            PDKCardData cardData = new PDKCardData(suit, cardValue);
            cardDataList.Add(cardData);
        }
        return cardDataList;
    }

    #endregion
}

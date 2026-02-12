using System.Collections.Generic;
using System.Collections;
using System.Linq;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using DG.Tweening;
using Spine.Unity;
using Cysharp.Threading.Tasks;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class MJPlayBack : UIFormBase
{
    #region UI节点定义

    [Header("===== 2D房间信息显示 =====")]
    public GameObject roomInfo;                     // 房间信息面板（点击GameInfoBtn显示/隐藏）
    public Text DeskIDText;
    public Text DeskNumText;
    public Image tableBg;                           // 桌布
    public Image gameType;                           // 游戏类型图标
    public Text center2DRoomInfoText;               // 房间信息文本（合并显示：余牌/底分/局数/房号）
    public GameObject center2DPointer;              // 当前操作玩家指针（通过旋转指示不同座位）
    public Text center2DCountdownText;              // 倒计时文本

    // 四个座位的管理对象（在 Inspector 中直接拖入预制体的座位脚本）
    [Header("===== 2D四家座位 =====")]
    public MahjongSeat_Back[] seats2D = new MahjongSeat_Back[4];

    [Header("===== 出牌动画 =====")]
    public MjCard_2D DapaiAniCard;                  // 出牌动画牌（参考MahjongGameUI_2D）

    [Header("===== 赖子显示 =====")]
    public GameObject laiziInfo;                    // 赖子信息面板
    public MjCard_2D currentPiaoLaiziCard;        // 当前飘癞子牌对象
    public MjCard_2D currentLaiziCard;            // 当前赖子牌对象

    // UI控制按钮
    [Header("===== 控制按钮 =====")]
    public GameObject varStopImage;                 // 暂停时显示的播放按钮图标
    public GameObject varPlayImage;                 // 播放时显示的暂停按钮图标

    private MahjongFaceValue lastDiscard = MahjongFaceValue.MJ_UNKNOWN;
    private int lastDiscardSeatIdx = -1;            // 记录上一次打牌的座位索引（用于碰吃时移除弃牌）
    public GameObject discardFlag;                  // 最后一张弃牌标记
    public GameObject playOver;                  // 播放结束提示

    #endregion

    #region 回放状态管理
    private Msg_MJPlayBack mJPlayBack;

    // 回放控制状态
    private int currentOptionIndex = 0;              // 当前回放到第几步
    private bool isReplaying = false;                // 是否正在回放中
    private bool isPaused = false;                    // 是否暂停
    private float m_ReplaySpeed = 1.0f;              // 回放速度倍率
    private Coroutine m_ReplayCoroutine = null;      // 回放协程
    private bool isAnimationPlaying = false;         // 是否有动画正在播放

    // 当前动画的上下文(用于强制完成动画)
    private MahjongSeat_Back currentAnimationSeat = null;
    private int currentAnimationSeatIdx = -1;
    private int currentAnimationCardValue = -1;
    private Coroutine currentAnimationCoroutine = null;

    // 预设速度列表
    private readonly float[] m_SpeedPresets = { 1.0f, 2.0f, 3.0f, 0.5f };
    private int m_CurrentSpeedIndex = 0;

    // 回放数据（从 Msg_MJPlayBack 解析）
    private List<BasePlayer> playerInfos = new();                    // 玩家列表（来自 Players）
    private Dictionary<long, int> playerSeatMap = new();             // playerId -> 客户端座位索引映射
    private List<Msg_MJRecord> options = new();                      // 操作记录列表（来自 Option）
    private List<Msg_MJHandCard> initialHandCards = new();           // 初始手牌列表（来自 HandCard）

    // 庄家信息
    private long bankerPlayerId = 0;                 // 庄家的 playerId（来自 Banker）
    private int bankerSeatIdx = -1;                  // 庄家在客户端的座位索引（转换后）

    // 赖子信息
    private MahjongFaceValue currentLaiZi = MahjongFaceValue.MJ_UNKNOWN;  // 当前赖子牌值

    // 回放阶段（骰子->换牌->甩牌->正常操作）
    private int replayPhase = 0;                     // 当前回放阶段
    private const int PHASE_DICE = 0;                // 骰子阶段（卡五星/血流）
    private const int PHASE_CHANGE = 1;              // 换牌阶段（卡五星/血流）
    private const int PHASE_THROW = 2;               // 甩牌阶段（血流麻将专属）
    private const int PHASE_NORMAL = 3;              // 正常回放阶段

    // 血流麻将状态
    private bool isXueLiu = false;                   // 是否为血流麻将模式
    private List<LongListInt> throwCardData = new(); // 甩牌数据

    // 仙桃晃晃解锁相关
    private HashSet<long> openDoublePlayers = new HashSet<long>();

    #endregion

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        // 确保 MahjongHuTingCheck 全局单例已初始化（回放场景可能不经过正常游戏流程）
        if (!MahjongHuTingCheck.IsInitialized)
        {
            _ = MahjongHuTingCheck.InitializeGlobalInstanceAsync();
        }

        mJPlayBack = Msg_MJPlayBack.Parser.ParseFrom(Params.Get<VarByteArray>("Msg_MJPlayBack"));
        int lastChooseIndex = Params.Get<VarInt32>("LastChooseIndex");
        InitializeReplay(mJPlayBack, lastChooseIndex);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        StopReplay();
        base.OnClose(isShutdown, userData);
    }

    /// <summary>
    /// 初始化回放数据
    /// 座位转换规则：
    /// 1. 通过 Util.GetMyselfInfo().PlayerId 找到观看者的 playerId
    /// 2. 在 msg_playback.Pos 中找到观看者的服务器位置作为基准
    /// 3. 使用 Util.TransformSeatS2C 将所有玩家的服务器位置转换为客户端座位
    /// 4. 观看者始终显示在座位0，其他玩家按相对位置分布
    /// </summary>
    private void InitializeReplay(Msg_MJPlayBack msg_playback, int lastChooseIndex)
    {
        playerInfos = msg_playback.Players.ToList();
        options = msg_playback.Option.ToList();
        initialHandCards = msg_playback.HandCard.ToList();
        openDoublePlayers.Clear();

        // 获取庄家信息
        bankerPlayerId = msg_playback.Banker;
        int playerCount = playerInfos.Count;

        // 检查是否为卡五星模式
        isKaWuXing = IsKaWuXingMode();
        if (isKaWuXing && msg_playback.ChangeCard != null)
        {
            changeCardData = msg_playback.ChangeCard.ToList();
        }

        // 检查是否为血流麻将模式
        isXueLiu = IsXueLiuMode();

        // 初始化桌布
        int tableBgIndex = MahjongSettings.GetTableBg();
        tableBg.SetSprite($"MJGame/Bgs/{tableBgIndex}.png");

        // 根据麻将类型 动态加载不同类型图片
        string spriteName = string.Empty;
        if (isKaWuXing) spriteName = "kwx";
        else if (isXueLiu) spriteName = "xlch";
        else if (IsXianTaoMode()) spriteName = "xthh";

        if (!string.IsNullOrEmpty(spriteName))
        {
            gameType.SetSprite(spriteName);
            gameType.gameObject.SetActive(true);
        }
        else
        {
            gameType.gameObject.SetActive(false);
        }

        // 血流模式也需要读取换牌数据（如果有的话）
        if (isXueLiu && msg_playback.ChangeCard != null && msg_playback.ChangeCard.Count > 0)
        {
            changeCardData = msg_playback.ChangeCard.ToList();
        }
        if (isXueLiu && msg_playback.ThrowCard != null && msg_playback.ThrowCard.Count > 0)
        {
            throwCardData = msg_playback.ThrowCard.ToList();
        }

        // 获取视角基准玩家（不再使用 Util.GetMyselfInfo()，因为会长查看成员回放时会报错）
        // 策略：找到 pos 值最小的玩家作为视角基准，该玩家将显示在座位0
        long viewPlayerId = 0;
        
        // 先检查自己是否在玩家列表中
        bool selfInPlayers = msg_playback.Pos.Any(p => p.Key == Util.GetMyselfInfo().PlayerId);
        
        if (selfInPlayers)
        {
            // 自己在玩家列表中，以自己为视角
            viewPlayerId = Util.GetMyselfInfo().PlayerId;
        }
        else
        {
            // 旁观/查看他人战绩：找到Pos值最小的玩家作为视角基准
            int minPosValue = int.MaxValue;
            foreach (var posData in msg_playback.Pos)
            {
                if (posData.Val < minPosValue)
                {
                    minPosValue = posData.Val;
                    viewPlayerId = posData.Key;
                }
            }
        }

        // 找到视角基准玩家在 Pos 列表中的服务器位置
        int selfPosAtServer = -1;
        LongInt myPosData = msg_playback.Pos.FirstOrDefault(p => p.Key == viewPlayerId);
        if (myPosData != null)
        {
            selfPosAtServer = Util.GetPosByServerPos((Position)myPosData.Val, playerCount);
        }
        else
        {
            GF.LogError($"[回放] 未找到视角基准玩家的位置信息! PlayerId={viewPlayerId}");
        }

        foreach (var seat in seats2D)
        {
            seat.gameObject.SetActive(false);
        }
        // 建立玩家位置映射（使用座位转换，自己始终在座位0）
        playerSeatMap.Clear();
        bankerSeatIdx = -1;

        foreach (var posData in msg_playback.Pos)
        {
            long playerId = posData.Key;

            // 将服务器位置转换为客户端座位索引
            int serverPos = Util.GetPosByServerPos((Position)posData.Val, playerCount);
            int seatIdx = Util.TransformSeatS2C(serverPos, selfPosAtServer, playerCount);

            playerSeatMap[playerId] = seatIdx;

            if (playerId == bankerPlayerId)
            {
                bankerSeatIdx = seatIdx;
            }
        }

        // 初始化座位系统（根据玩家信息和座位映射）
        foreach (var player in playerInfos)
        {
            long playerId = player.PlayerId;

            // 获取转换后的座位索引
            if (!playerSeatMap.TryGetValue(playerId, out int seatIdx))
            {
                GF.LogError($"[回放] 玩家{playerId}未找到座位映射");
                continue;
            }

            if (seats2D[seatIdx] == null)
            {
                GF.LogError($"[回放] 座位{seatIdx}对象为空");
                continue;
            }

            // 初始化座位（传入赖子值）
            seats2D[seatIdx].Initialize(msg_playback);

            // 构建DeskPlayer对象用于显示
            DeskPlayer deskPlayer = new DeskPlayer
            {
                BasePlayer = player,
                Coin = "0"
            };
            seats2D[seatIdx].UpdatePlayerInfo(deskPlayer, (FengWei)seatIdx);

            // 显示庄家标识
            bool isBanker = (playerId == bankerPlayerId);
            seats2D[seatIdx].ShowBankerIcon(isBanker, false);

            // 查找该玩家的初始手牌
            Msg_MJHandCard handCard = initialHandCards.FirstOrDefault(h => h.PlayerId == playerId);
            if (handCard != null && handCard.HandCard != null && handCard.HandCard.Count > 0)
            {
                List<int> cards = handCard.HandCard.ToList();

                // 规则1：庄家最后一张牌显示在摸牌区（第一张摸牌）
                if (isBanker && cards.Count > 0)
                {
                    int lastCard = cards[cards.Count - 1];
                    cards.RemoveAt(cards.Count - 1);

                    // 创建手牌（不包含最后一张）
                    seats2D[seatIdx].CreateHandCards(cards);

                    // 最后一张显示在摸牌区
                    seats2D[seatIdx].AddMoPai(lastCard);
                }
                else
                {
                    // 非庄家：所有牌都在手牌区
                    seats2D[seatIdx].CreateHandCards(cards);
                }
            }
        }

        // 指针指向庄家
        UpdatePointer(bankerSeatIdx);

        RefreshAllBaoStatus();

        // 设置房间信息
        if (msg_playback.DeskCommon != null)
        {
            DeskIDText.text = $"房号: <color=DCDA95>{msg_playback.DeskCommon.DeskId}</color>";
            DeskNumText.text = $"局数: <color=DCDA95>{lastChooseIndex}/{msg_playback.DeskCommon.BaseConfig.PlayerTime}</color>";

            center2DRoomInfoText.text =
                $"房号: {msg_playback.DeskCommon.DeskId} | " +
                $"底分: {msg_playback.DeskCommon.BaseConfig.BaseCoin} | " +
                $"进程: 0/{options.Count}";

            // 初始化房间信息面板（默认隐藏，点击GameInfoBtn显示）
            if (roomInfo != null)
            {
                roomInfo.SetActive(false);
                
                // 设置房间标题
                Transform titleTransform = roomInfo.transform.Find("Title");
                if (titleTransform != null)
                {
                    Text titleText = titleTransform.GetComponent<Text>();
titleText.text = msg_playback.DeskCommon.BaseConfig.DeskName;
                }

                // 设置房间规则描述（使用回放数据构建）
                var descText = roomInfo.transform.Find("Bg/Scroll View/TxtContent").GetComponent<Text>();
                descText.text = MahjongRuleFactory.BuildRoomDescription(msg_playback.DeskCommon);
            }
        }
        else
        {
            center2DRoomInfoText.text = $"麻将回放 | 进程: 0/{options.Count}";
            
            // 没有房间数据时隐藏房间信息面板
            roomInfo.SetActive(false);
        }

        // 显示赖子牌（如果有赖子）
        if (msg_playback.Laizi > 0)
        {
            laiziInfo.SetActive(true);
            MahjongFaceValue laiziValue = Util.ConvertServerIntToFaceValue(msg_playback.Laizi);
            currentLaiZi = laiziValue;  // 保存赖子值供听牌计算使用
            currentLaiziCard.SetCardValue(laiziValue, true);
            MahjongFaceValue piaolaiziValue = Util.ConvertServerIntToFaceValue(msg_playback.Piao);
            currentPiaoLaiziCard.SetCardValue(piaolaiziValue);
        }
        else
        {
            laiziInfo.SetActive(false);
            currentLaiZi = MahjongFaceValue.MJ_UNKNOWN;  // 无赖子
        }

        // 初始化回放状态
        currentOptionIndex = 0;
        isReplaying = false;
        isPaused = false;
        m_ReplaySpeed = 1.0f;
        m_CurrentSpeedIndex = 0;
        m_ReplayCoroutine = null;

        // 初始化飘分显示 (血流/卡五星都支持)
        // LongDouble: Key = playerId, Val = piaoFenValue
        if (msg_playback.PiaoFen != null && msg_playback.PiaoFen.Count > 0)
        {
            foreach (var piaoFenInfo in msg_playback.PiaoFen)
            {
                if (piaoFenInfo == null) continue;

                long playerId = piaoFenInfo.Key;
                int piaoFenValue = (int)piaoFenInfo.Val;

                if (playerSeatMap.TryGetValue(playerId, out int seatIdx))
                {
                    if (seatIdx >= 0 && seatIdx < seats2D.Length && seats2D[seatIdx] != null)
                    {
                        seats2D[seatIdx].SetPiaoFenDisplay(piaoFenValue);
                    }
                }
            }
        }

        // 更新UI显示
        UpdateUI();

        // 初始化阶段状态
        bool hasChangeCard = changeCardData != null && changeCardData.Count > 0;
        bool hasThrowCard = isXueLiu && throwCardData != null && throwCardData.Count > 0;

        if (hasChangeCard || hasThrowCard)
        {
            replayPhase = PHASE_DICE;  // 从骰子阶段开始
        }
        else
        {
            replayPhase = PHASE_NORMAL;  // 直接进入正常阶段
        }

        // 自动开始播放
        StartReplay();
    }

    /// <summary>
    /// 获取总步数（包含卡五星额外步骤）
    /// </summary>
    private int GetTotalSteps()
    {
        int total = options.Count;
        bool hasChangeCard = changeCardData != null && changeCardData.Count > 0;
        bool hasThrowCard = isXueLiu && throwCardData != null && throwCardData.Count > 0;

        if (hasChangeCard)
        {
            total += 2;  // 骰子 + 换牌
        }
        if (hasThrowCard)
        {
            total += 1;  // 甩牌
        }
        return total;
    }

    /// <summary>
    /// 获取当前显示的步骤索引（包含特殊阶段步骤）
    /// </summary>
    private int GetDisplayStepIndex()
    {
        bool hasChangeCard = changeCardData != null && changeCardData.Count > 0;
        bool hasThrowCard = isXueLiu && throwCardData != null && throwCardData.Count > 0;

        if (hasChangeCard || hasThrowCard)
        {
            // 特殊阶段：根据当前阶段计算显示索引
            if (replayPhase < PHASE_NORMAL)
            {
                return replayPhase;  // PHASE_DICE=0, PHASE_CHANGE=1, PHASE_THROW=2
            }

            // 正常阶段：计算前置步骤数量
            int preSteps = 0;
            if (hasChangeCard)
            {
                preSteps += 2;  // 骰子 + 换牌
            }
            if (hasThrowCard)
            {
                preSteps += 1;  // 甩牌
            }
            return preSteps + currentOptionIndex;
        }
        return currentOptionIndex;
    }

    /// <summary>
    /// 更新指针指向（带逆时针旋转动画）
    /// </summary>
    private void UpdatePointer(int seatIdx)
    {
        if (center2DPointer == null || seatIdx < 0 || seatIdx >= 4)
        {
            return;
        }

        // 座位到角度的映射（Unity Z轴顺时针旋转）
        float targetRotation = 0f;
        switch (seatIdx)
        {
            case 0: targetRotation = 0f; break;    // 下方（自己）
            case 1: targetRotation = 270f; break;  // 左方（左家）
            case 2: targetRotation = 180f; break;  // 上方（对家）
            case 3: targetRotation = 90f; break;   // 右方（右家）
        }

        // 杀死之前的旋转动画
        center2DPointer.transform.DOKill();

        // 获取当前角度并规范化到0-360范围
        float currentRotation = center2DPointer.transform.localEulerAngles.z;

        // 计算逆时针旋转的角度差（逆时针方向：0° → 90° → 180° → 270° → 0°，角度递增）
        float angleDiff = targetRotation - currentRotation;

        // 将角度差规范化到[0, 360)范围，确保逆时针旋转（正向）
        if (angleDiff < 0)
        {
            angleDiff += 360f;
        }
        else if (angleDiff >= 360f)
        {
            angleDiff -= 360f;
        }

        // 如果角度差很小（小于0.1度），认为已经在目标位置，直接返回不旋转
        if (Mathf.Abs(angleDiff) < 0.1f || Mathf.Abs(angleDiff - 360f) < 0.1f)
        {
            return;
        }

        // 应用逆时针旋转动画（0.3秒，使用EaseOutQuad缓动）
        center2DPointer.transform.DORotate(
            new Vector3(0, 0, currentRotation + angleDiff),
            0.3f,
            RotateMode.FastBeyond360
        ).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        // 更新房间信息中的进程
        if (center2DRoomInfoText != null && mJPlayBack != null)
        {
            int displayStep = GetDisplayStepIndex();
            int totalSteps = GetTotalSteps();
            bool isCompleted = (replayPhase >= PHASE_NORMAL && currentOptionIndex >= options.Count);

            if (mJPlayBack.DeskCommon != null)
            {
                playOver.SetActive(isCompleted);
                center2DRoomInfoText.text =
                    $"房号: {mJPlayBack.DeskCommon.DeskId} | " +
                    $"底分: {mJPlayBack.DeskCommon.BaseConfig.BaseCoin} | " +
                    $"进程: {displayStep}/{totalSteps}";
            }
            else
            {
                center2DRoomInfoText.text = $"麻将回放 | 进程: {displayStep}/{totalSteps}";
            }
        }

        varStopImage.SetActive(!isPaused && isReplaying);

        varPlayImage.SetActive(isPaused || !isReplaying);
    }

    /// <summary>
    /// 回放协程 - 定时播放下一步操作
    /// </summary>
    private IEnumerator ReplayCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay / m_ReplaySpeed);
        if (!isPaused)
        {
            ReplayNextOperation();
        }
        else
        {
            m_ReplayCoroutine = null;
        }
    }

    /// <summary>
    /// 回放下一个操作
    /// </summary>
    private void ReplayNextOperation()
    {
        // 检查是否已完成所有步骤
        bool isComplete = (replayPhase >= PHASE_NORMAL && currentOptionIndex >= options.Count);
        if (isComplete || gameObject.activeInHierarchy == false)
        {
            StopReplay();
            return;
        }
        Play();
        if (isReplaying && !isPaused)
        {
            // 特殊阶段动画需要更长时间
            float delay = (replayPhase < PHASE_NORMAL) ? 2f : 1f;
            m_ReplayCoroutine = CoroutineRunner.Instance.StartCoroutine(ReplayCoroutine(delay));
        }
    }
    /// <summary>
    /// 前进一步 - 播放下一个操作
    /// </summary>
    public void Play()
    {
        // 如果有动画正在播放,强制完成动画
        if (isAnimationPlaying)
        {
            CompleteCurrentAnimation();
        }

        // 处理特殊阶段（骰子、换牌、甩牌）
        if (replayPhase < PHASE_NORMAL)
        {
            switch (replayPhase)
            {
                case PHASE_DICE:
                    // 步骤1：播放骰子动画
                    PlayKWXDiceStep();
                    replayPhase = PHASE_CHANGE;
                    UpdateUI();
                    return;

                case PHASE_CHANGE:
                    // 步骤2：播放换牌动画
                    PlayKWXChangeCardStep();
                    // 如果是血流且有甩牌数据，进入甩牌阶段；否则直接进入正常阶段
                    if (isXueLiu && throwCardData != null && throwCardData.Count > 0)
                    {
                        replayPhase = PHASE_THROW;
                    }
                    else
                    {
                        replayPhase = PHASE_NORMAL;
                    }
                    UpdateUI();
                    return;

                case PHASE_THROW:
                    // 步骤3：播放甩牌（血流专属）
                    PlayThrowCardStep();
                    replayPhase = PHASE_NORMAL;
                    UpdateUI();
                    return;
            }
        }

        // 正常回放阶段
        // 先强制完成所有换牌动画，防止快速点击导致位置错误
        foreach (var seat in seats2D)
        {
            if (seat != null)
            {
                seat.CompleteDropAnimations();
            }
        }

        if (currentOptionIndex >= options.Count)
        {
            return;
        }

        Msg_MJRecord record = options[currentOptionIndex];
        PlayOption(record);

        currentOptionIndex++;
        UpdateUI();
    }

    /// <summary>
    /// 强制完成当前正在播放的动画
    /// </summary>
    private void CompleteCurrentAnimation()
    {
        // 强制完成所有座位的下坠动画（换牌动画）
        foreach (var seat in seats2D)
        {
            if (seat != null)
            {
                seat.CompleteDropAnimations();
            }
        }

        // 杀死换牌相关的延时回调
        DOTween.Kill("KWXChangeCardAnimation");

        if (DapaiAniCard != null && DapaiAniCard.gameObject.activeSelf)
        {
            // 隐藏动画牌
            DapaiAniCard.gameObject.SetActive(false);
        }

        // 完成所有 DOTween 动画
        DOTween.Complete(DapaiAniCard?.transform);

        // 停止当前动画协程
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }

        // 立即执行动画完成后的操作:添加弃牌
        if (currentAnimationSeat != null && currentAnimationCardValue > 0)
        {
            currentAnimationSeat.AddDiscardCard(currentAnimationCardValue);
            RefreshAllBaoStatus();
            ShowDiscardFlag(currentAnimationSeatIdx);
        }

        // 清理上下文
        currentAnimationSeat = null;
        currentAnimationSeatIdx = -1;
        currentAnimationCardValue = -1;
        isAnimationPlaying = false;
    }

    /// <summary>
    /// 后退一步 - 撤销当前操作的效果
    /// </summary>
    public void UnPlay()
    {
        // 如果有动画正在播放,强制完成动画
        if (isAnimationPlaying)
        {
            CompleteCurrentAnimation();
        }

        // 处理特殊阶段回退
        bool hasChangeCard = changeCardData != null && changeCardData.Count > 0;
        bool hasThrowCard = isXueLiu && throwCardData != null && throwCardData.Count > 0;

        if (hasChangeCard || hasThrowCard)
        {
            if (replayPhase == PHASE_NORMAL && currentOptionIndex == 0)
            {
                // 从正常阶段回退
                if (hasThrowCard)
                {
                    // 有甩牌数据，回退到甩牌阶段
                    UnPlayThrowCardStep();
                    replayPhase = PHASE_THROW;
                }
                else
                {
                    // 无甩牌数据，回退到换牌阶段
                    UnPlayKWXChangeCardStep();
                    replayPhase = PHASE_CHANGE;
                }
                UpdateUI();
                return;
            }
            else if (replayPhase == PHASE_THROW)
            {
                // 从甩牌阶段回退到换牌阶段
                UnPlayKWXChangeCardStep();
                replayPhase = PHASE_CHANGE;
                UpdateUI();
                return;
            }
            else if (replayPhase == PHASE_CHANGE)
            {
                // 从换牌阶段回退到骰子阶段
                UnPlayKWXDiceStep();
                replayPhase = PHASE_DICE;
                UpdateUI();
                return;
            }
            else if (replayPhase == PHASE_DICE)
            {
                return;
            }
        }

        if (currentOptionIndex <= 0)
        {
            return;
        }

        // 后退一步：减少索引
        currentOptionIndex--;

        // 获取需要撤销的操作
        Msg_MJRecord record = options[currentOptionIndex];

        if (!playerSeatMap.TryGetValue(record.PlayerId, out int seatIdx))
        {
            GF.LogWarning($"[回放] 找不到玩家 {record.PlayerId} 的座位信息");
            UpdateUI();
            return;
        }

        MahjongSeat_Back seat = seats2D[seatIdx];
        if (seat == null)
        {
            GF.LogWarning($"[回放] 座位 {seatIdx} 未初始化");
            UpdateUI();
            return;
        }

        // 根据操作类型撤销
        switch (record.Option)
        {
            case MJOption.Draw:
                // 撤销摸牌
                UndoDraw(seat, seatIdx, record);
                break;

            case MJOption.Discard:
                // 撤销打牌：移除弃牌区最后一张，添加回手牌
                UndoDiscard(seat, seatIdx, record);
                break;

            case MJOption.Pen:
            case MJOption.Chi:
            case MJOption.Gang:
                // 撤销碰吃杠：移除组牌，恢复手牌和对方弃牌
                UndoMeld(seat, seatIdx, record);
                break;

            case MJOption.Hu:
                // 撤销胡牌
                UndoHu(seat, seatIdx, record);
                break;

            case MJOption.Guo:
                // 过无需撤销
                break;

            case MJOption.Ting:
                // 撤销听牌（卡五星亮牌）
                UndoTing(seat, seatIdx, record);
                break;
        }

        // 更新指针到上一个操作者
        if (currentOptionIndex > 0)
        {
            Msg_MJRecord prevRecord = options[currentOptionIndex - 1];
            if (playerSeatMap.TryGetValue(prevRecord.PlayerId, out int prevSeatIdx))
            {
                UpdatePointer(prevSeatIdx);
            }
        }
        else
        {
            UpdatePointer(bankerSeatIdx);
        }

        // 处理得分变化（例如飘癞子扣分）
        if (record.CoinChange != null && record.CoinChange.Count > 0)
        {
            // 撤销操作时，得分变化需要取反恢复
            // 如果是胡牌，UndoHu 内部已经处理了得分恢复，这里跳过
            if (record.Option != MJOption.Hu)
            {
                // 遍历所有玩家的得分变化
                foreach (var coinChange in record.CoinChange)
                {
                    long changePlayerId = coinChange.Key;
                    double scoreChange = 0 - coinChange.Val;

                    // 查找对应的座位
                    if (playerSeatMap.TryGetValue(changePlayerId, out int changeSeatIdx))
                    {
                        if (seats2D[changeSeatIdx] != null)
                        {
                            // 更新玩家积分显示
                            seats2D[changeSeatIdx].UpdatePlayerCoin(scoreChange);
                        }
                    }
                    else
                    {
                        GF.LogWarning($"[回放] 找不到玩家{changePlayerId}的座位映射");
                    }
                }
            }
        }

        UpdateUI();

        if (IsXianTaoMode())
        {
            UpdateOpenDoublePlayers();
            RefreshAllBaoStatus();
        }
    }

    /// <summary>
    /// 撤销打牌操作
    /// </summary>
    private void UndoDiscard(MahjongSeat_Back seat, int seatIdx, Msg_MJRecord record)
    {
        // 1. 移除弃牌区最后一张牌
        if (seat.DiscardContainer != null && seat.DiscardContainer.childCount > 0)
        {
            int lastIndex = seat.GetNewestDiscardIndex();
            if (lastIndex >= 0)
            {
                Transform lastCard = seat.DiscardContainer.GetChild(lastIndex);
                if (lastCard != null)
                {
                    DestroyImmediate(lastCard.gameObject);
                    seat.UpdateLaiziCount();
                }
            }
        }

        // 2. 分析打牌前的状态
        // 查找打牌前的上一个操作
        Msg_MJRecord prevRecord = null;
        if (currentOptionIndex > 0)
        {
            prevRecord = options[currentOptionIndex - 1];
        }

        // 判断打牌前摸牌区是否有牌
        bool hadMoPaiBeforeDiscard = false;
        int moPaiValueBeforeDiscard = 0;

        // 特殊情况:撤回到第0步(初始状态),如果是庄家,需要恢复初始摸牌
        if (currentOptionIndex == 0 && seatIdx == bankerSeatIdx)
        {
            // 庄家初始状态:14张牌,最后一张在摸牌区
            Msg_MJHandCard handCard = initialHandCards.FirstOrDefault(h => h.PlayerId == record.PlayerId);
            if (handCard != null && handCard.HandCard != null && handCard.HandCard.Count > 0)
            {
                hadMoPaiBeforeDiscard = true;
                moPaiValueBeforeDiscard = handCard.HandCard[handCard.HandCard.Count - 1]; // 初始最后一张牌
            }
        }
        else if (prevRecord != null && prevRecord.PlayerId == record.PlayerId)
        {
            if (prevRecord.Option == MJOption.Draw)
            {
                // 上一步是摸牌,打牌前摸牌区有牌
                hadMoPaiBeforeDiscard = true;
                moPaiValueBeforeDiscard = prevRecord.Card;
            }
            else if (prevRecord.Option == MJOption.Pen || prevRecord.Option == MJOption.Chi || prevRecord.Option == MJOption.Gang)
            {
                // 上一步是碰吃杠,这些操作后会调用EnsureMoPaiSlot,所以打牌前摸牌区有牌
                // 但我们不知道具体是哪张牌,需要从当前手牌推测
                hadMoPaiBeforeDiscard = true;
                // 摸牌值暂时未知,后面处理
            }
        }

        // 3. 还原状态
        if (hadMoPaiBeforeDiscard && moPaiValueBeforeDiscard > 0 && moPaiValueBeforeDiscard == record.Card)
        {
            // 情况1: 从摸牌区打出(摸的牌直接打)
            // 打牌时清空了摸牌区,所以撤回时直接恢复到摸牌区即可
            seat.AddMoPai(record.Card);
        }
        else if (hadMoPaiBeforeDiscard && moPaiValueBeforeDiscard > 0)
        {
            // 情况2: 从手牌打出,但摸牌区有牌(摸了牌但打的是手牌里的)
            // 关键:打牌时ArrangeHandCards()已经将摸牌整理到手牌,所以当前手牌已包含摸的牌
            // 撤回需要:
            // 1) 当前手牌已有摸牌区的牌
            // 2) 加上打出的牌
            // 3) 从手牌中取出摸牌区应该有的牌

            List<int> currentHandCards = new List<int>();
            if (seat.HandContainer != null)
            {
                for (int i = 0; i < seat.HandContainer.childCount; i++)
                {
                    GameObject cardObj = seat.HandContainer.GetChild(i).gameObject;
                    MjCard_2D mjCard = cardObj.GetComponent<MjCard_2D>();
                    if (mjCard != null)
                    {
                        currentHandCards.Add(mjCard.cardValue);
                    }
                }
            }

            // 添加打出的牌
            currentHandCards.Add(record.Card);

            // 从手牌中移除摸牌区应该有的牌(因为要放回摸牌区)
            bool removed = currentHandCards.Remove(moPaiValueBeforeDiscard);

            // 重新创建手牌
            seat.CreateHandCards(currentHandCards);

            // 将摸的牌放回摸牌区
            seat.AddMoPai(moPaiValueBeforeDiscard);

        }
        else if (hadMoPaiBeforeDiscard)
        {
            // 情况3: 上一步是碰吃杠,打牌前摸牌区有牌,但不知道是哪张
            // 简化处理: 将打出的牌恢复到摸牌区
            seat.AddMoPai(record.Card);
        }
        else
        {
            // 情况4: 从手牌打出,摸牌区无牌
            List<int> currentHandCards = new List<int>();
            if (seat.HandContainer != null)
            {
                for (int i = 0; i < seat.HandContainer.childCount; i++)
                {
                    GameObject cardObj = seat.HandContainer.GetChild(i).gameObject;
                    MjCard_2D mjCard = cardObj.GetComponent<MjCard_2D>();
                    if (mjCard != null)
                    {
                        currentHandCards.Add(mjCard.cardValue);
                    }
                }
            }

            currentHandCards.Add(record.Card);
            seat.CreateHandCards(currentHandCards);

        }

        // 4. 隐藏弃牌标记
        HideDiscardFlag();
    }

    /// <summary>
    /// 撤销摸牌操作
    /// </summary>
    private void UndoDraw(MahjongSeat_Back seat, int seatIdx, Msg_MJRecord record)
    {
        // 摸牌可能在两个地方：
        // 1. 摸牌区（正常情况）
        // 2. 手牌区（朝天暗杠后整理了）

        bool removedFromMoPai = false;
        bool removedFromHand = false;

        // 1. 尝试从摸牌区移除
        if (seat.MoPaiContainer != null && seat.MoPaiContainer.childCount > 0)
        {
            Transform moPaiCard = seat.MoPaiContainer.GetChild(0);
            MjCard_2D mjCard = moPaiCard?.GetComponent<MjCard_2D>();
            if (mjCard != null && mjCard.cardValue == record.Card)
            {
                seat.ClearContainer(seat.MoPaiContainer);
                removedFromMoPai = true;
            }
        }

        // 2. 如果摸牌区没有，说明牌已经插入手牌了（朝天暗杠后整理），需要从手牌移除
        if (!removedFromMoPai)
        {
            // 获取当前手牌
            List<int> currentHandCards = new List<int>();
            if (seat.HandContainer != null)
            {
                for (int i = 0; i < seat.HandContainer.childCount; i++)
                {
                    GameObject cardObj = seat.HandContainer.GetChild(i).gameObject;
                    MjCard_2D mjCard = cardObj.GetComponent<MjCard_2D>();
                    if (mjCard != null)
                    {
                        currentHandCards.Add(mjCard.cardValue);
                    }
                }
            }

            // 找到并移除摸的牌（从最后开始找，因为摸牌会插入到合适位置）
            int removeIndex = currentHandCards.LastIndexOf(record.Card);
            if (removeIndex >= 0)
            {
                currentHandCards.RemoveAt(removeIndex);
                seat.CreateHandCards(currentHandCards);
                removedFromHand = true;
            }
        }

        if (!removedFromMoPai && !removedFromHand)
        {
            GF.LogWarning($"[回放] 撤销摸牌失败: 未找到牌 {Util.ConvertServerIntToFaceValue(record.Card)}");
        }
    }

    /// <summary>
    /// 撤销碰吃杠操作
    /// </summary>
    private void UndoMeld(MahjongSeat_Back seat, int seatIdx, Msg_MJRecord record)
    {
        // 补杠特殊处理：调用座位的 UndoBuGangCard
        if (record.Option == MJOption.Gang && record.Meld != null && record.Meld.SubType == 3)
        {
            int buGangCard = record.Meld.Card.Val.Count > 0 ? record.Meld.Card.Val[0] : record.Card;
            seat.UndoBuGangCard(buGangCard);
        }
        // 1. 移除组牌区最后一组（需要移除整组的所有牌）
        else if (seat.MeldContainer != null && record.Meld != null && record.Meld.Card != null)
        {
            int meldCardCount = record.Meld.Card.Val.Count;

            // 朝天杠和朝天暗杠只显示3张牌
            if (record.Option == MJOption.Gang &&
                (record.Meld.SubType == 6 || record.Meld.SubType == 7))
            {
                meldCardCount = 3;
            }

            // 3号位（右家）的组牌是反序插入的，最新的在前面（索引0开始）
            // 其他座位最新的在后面
            bool isNewestAtFirst = (seatIdx == 3);

            // 收集需要删除的对象
            List<GameObject> toDestroy = new List<GameObject>();

            if (isNewestAtFirst)
            {
                // 3号位：最新的牌组在前面，从索引0开始删除
                int endIndex = Mathf.Min(meldCardCount, seat.MeldContainer.childCount);
                for (int i = 0; i < endIndex; i++)
                {
                    Transform card = seat.MeldContainer.GetChild(i);
                    if (card != null)
                    {
                        toDestroy.Add(card.gameObject);
                    }
                }
            }
            else
            {
                // 其他座位：最新的牌组在后面，从后往前删除
                int startIndex = seat.MeldContainer.childCount - meldCardCount;
                if (startIndex < 0) startIndex = 0;

                for (int i = startIndex; i < seat.MeldContainer.childCount; i++)
                {
                    Transform card = seat.MeldContainer.GetChild(i);
                    if (card != null)
                    {
                        toDestroy.Add(card.gameObject);
                    }
                }
            }

            // 执行删除
            foreach (var obj in toDestroy)
            {
                DestroyImmediate(obj);
            }

        }

        // 2. 恢复手牌（将牌添加回手牌区）
        // 注意：碰吃杠后会调用EnsureMoPaiSlot，将最后一张手牌移到摸牌区
        // 所以撤回时需要把摸牌区的牌也加回手牌
        if (record.Meld != null && record.Meld.Card != null)
        {
            List<int> cardValues = record.Meld.Card.Val.ToList();

            // 确定需要恢复到手牌的数量（根据RemoveCardsForPengChiGang的逻辑）
            // subType: 0=默认明杠, 1=明杠, 2=暗杠, 3=补杠, 4=皮杠, 5=癞子杠, 6=朝天杠, 7=朝天暗杠
            int restoreCount = 2; // 碰、吃默认恢复2张
            if (record.Option == MJOption.Gang)
            {
                // 根据subType判断杠的类型
                if (record.Meld.SubType == 0 || record.Meld.SubType == 1)
                {
                    restoreCount = 3; // 明杠恢复3张
                }
                else if (record.Meld.SubType == 2)
                {
                    restoreCount = 4; // 暗杠恢复4张
                }
                else if (record.Meld.SubType == 3)
                {
                    restoreCount = 1; // 补杠恢复1张（只从摸牌区取）
                }
                else if (record.Meld.SubType == 4)
                {
                    restoreCount = 4; // 皮杠恢复4张
                }
                else if (record.Meld.SubType == 5)
                {
                    restoreCount = 4; // 癞子杠恢复4张
                }
                else if (record.Meld.SubType == 6)
                {
                    restoreCount = 2; // 朝天杠恢复2张（手牌2张+别人打的1张=3张）
                }
                else if (record.Meld.SubType == 7)
                {
                    restoreCount = 3; // 朝天暗杠恢复3张
                }
            }

            // 获取当前手牌
            List<int> currentHandCards = new List<int>();
            if (seat.HandContainer != null)
            {
                for (int i = 0; i < seat.HandContainer.childCount; i++)
                {
                    GameObject cardObj = seat.HandContainer.GetChild(i).gameObject;
                    MjCard_2D mjCard = cardObj.GetComponent<MjCard_2D>();
                    if (mjCard != null)
                    {
                        currentHandCards.Add(mjCard.cardValue);
                    }
                }
            }

            // 获取摸牌区的牌（碰吃杠后EnsureMoPaiSlot移过去的，或朝天杠后移过去的）
            bool hadMoPai = false;
            if (seat.MoPaiContainer != null && seat.MoPaiContainer.childCount > 0)
            {
                Transform moPaiCard = seat.MoPaiContainer.GetChild(0);
                MjCard_2D mjCard = moPaiCard?.GetComponent<MjCard_2D>();
                if (mjCard != null)
                {
                    currentHandCards.Add(mjCard.cardValue);
                    hadMoPai = true;
                }
            }

            // 添加要恢复的牌（碰吃时只取前restoreCount张，不包括被别人打出的那张）
            for (int i = 0; i < restoreCount && i < cardValues.Count; i++)
            {
                currentHandCards.Add(cardValues[i]);
            }

            // 重新创建手牌
            seat.CreateHandCards(currentHandCards);

            // 清空摸牌区
            seat.ClearContainer(seat.MoPaiContainer);

            // 朝天暗杠会整理手牌，之前摸的牌被整理进手牌了
            // 撤回时需要把上一步摸的牌重新提取到摸牌区
            if (record.Option == MJOption.Gang && record.Meld.SubType == 7)
            {
                // 向后查找上一个操作，如果是摸牌，需要把摸牌提取到摸牌区
                for (int i = currentOptionIndex - 1; i >= 0; i--)
                {
                    Msg_MJRecord prevRecord = options[i];
                    if (playerSeatMap.TryGetValue(prevRecord.PlayerId, out int prevSeatIdx) && prevSeatIdx == seatIdx)
                    {
                        if (prevRecord.Option == MJOption.Draw)
                        {
                            // 找到上一个摸牌操作，需要把这张摸牌提取到摸牌区
                            int drawCardValue = prevRecord.Card;

                            // 从手牌中找到这张摸牌并提取到摸牌区
                            if (seat.HandContainer != null)
                            {
                                // 找到摸牌的索引（从后往前找，因为可能有多张相同的牌）
                                int drawCardIndex = -1;
                                for (int j = seat.HandContainer.childCount - 1; j >= 0; j--)
                                {
                                    Transform cardTransform = seat.HandContainer.GetChild(j);
                                    MjCard_2D mjCard = cardTransform?.GetComponent<MjCard_2D>();
                                    if (mjCard != null && mjCard.cardValue == drawCardValue)
                                    {
                                        drawCardIndex = j;
                                        break;
                                    }
                                }

                                if (drawCardIndex >= 0)
                                {
                                    // 移除这张手牌并移到摸牌区
                                    Transform drawCard = seat.HandContainer.GetChild(drawCardIndex);
                                    drawCard.SetParent(seat.MoPaiContainer, false);

                                    // 设置摸牌的位置
                                    RectTransform rectTransform = drawCard.GetComponent<RectTransform>();
                                    if (rectTransform != null)
                                    {
                                        rectTransform.anchoredPosition = Vector2.zero;
                                        rectTransform.localScale = Vector3.one;
                                    }

                                    // 手动重新排列剩余手牌的位置（不能调用私有方法，所以简单地重新创建手牌）
                                    List<int> remainingHandCards = new List<int>();
                                    if (seat.HandContainer != null)
                                    {
                                        for (int k = 0; k < seat.HandContainer.childCount; k++)
                                        {
                                            GameObject cardObj = seat.HandContainer.GetChild(k).gameObject;
                                            MjCard_2D mjCard2 = cardObj.GetComponent<MjCard_2D>();
                                            if (mjCard2 != null)
                                            {
                                                remainingHandCards.Add(mjCard2.cardValue);
                                            }
                                        }
                                        // 重新创建手牌以确保位置正确
                                        seat.CreateHandCards(remainingHandCards);
                                    }

                                }
                                else
                                {
                                    GF.LogWarning($"[回放] 撤销朝天暗杠: 未找到摸牌{Util.ConvertServerIntToFaceValue(drawCardValue)}");
                                }
                            }
                            break;
                        }
                        else if (prevRecord.Option == MJOption.Discard)
                        {
                            // 如果上一个操作是打牌，说明摸牌已经打出去了，不需要提取
                            break;
                        }
                    }
                }
            }

        }

        // 3. 恢复对方弃牌区的牌（碰吃明杠朝天杠需要）
        // subType: 0=默认明杠, 1=明杠, 6=朝天杠需要恢复别人打出的牌
        if ((record.Option == MJOption.Pen || record.Option == MJOption.Chi ||
            (record.Option == MJOption.Gang && (record.Meld.SubType == 0 || record.Meld.SubType == 1 || record.Meld.SubType == 6))))
        {
            // 向前查找上一个打牌操作,找到被碰吃杠的那张牌
            int discardPlayerSeatIdx = -1;
            int discardCardValue = -1;

            for (int i = currentOptionIndex - 1; i >= 0; i--)
            {
                Msg_MJRecord prevRecord = options[i];
                // 同时检查Discard和Ting操作（听牌时也会打出牌）
                if (prevRecord.Option == MJOption.Discard || prevRecord.Option == MJOption.Ting)
                {
                    // 找到上一个打牌操作
                    if (playerSeatMap.TryGetValue(prevRecord.PlayerId, out int prevSeatIdx))
                    {
                        discardPlayerSeatIdx = prevSeatIdx;
                        discardCardValue = prevRecord.Card;
                        break;
                    }
                }
            }

            // 恢复找到的弃牌
            if (discardPlayerSeatIdx >= 0 && discardPlayerSeatIdx < seats2D.Length && discardCardValue > 0)
            {
                var targetSeat = seats2D[discardPlayerSeatIdx];
                if (targetSeat != null)
                {
                    // 恢复被碰吃杠走的牌
                    targetSeat.AddDiscardCard(discardCardValue);
                    RefreshAllBaoStatus();
                    // 显示弃牌标记
                    ShowDiscardFlag(discardPlayerSeatIdx);
                }
            }
            else
            {
                GF.LogWarning($"[回放] 未找到被{record.Option}的弃牌记录");
            }
        }

    }

    /// <summary>
    /// 播放单个操作
    /// </summary>
    private void PlayOption(Msg_MJRecord record)
    {
        if (!playerSeatMap.TryGetValue(record.PlayerId, out int seatIdx))
        {
            GF.LogWarning($"[回放] 找不到玩家 {record.PlayerId} 的座位信息");
            return;
        }

        MahjongSeat_Back seat = seats2D[seatIdx];
        if (seat == null)
        {
            GF.LogWarning($"[回放] 座位 {seatIdx} 未初始化");
            return;
        }

        // 规则2：指针需要实时更新
        UpdatePointer(seatIdx);


        switch (record.Option)
        {
            case MJOption.Draw:
                // 摸牌
                HandleDraw(seat, record);
                break;

            case MJOption.Discard:
                // 打牌（启动协程播放动画,保存协程引用）
                currentAnimationCoroutine = StartCoroutine(HandleDiscardWithAnimation(seat, seatIdx, record));
                break;

            case MJOption.Pen:
                // 碰
                HandlePeng(seat, seatIdx, record);
                break;

            case MJOption.Chi:
                // 吃
                HandleChi(seat, seatIdx, record);
                break;

            case MJOption.Gang:
                // 杠（明杠/暗杠/补杠）
                HandleGang(seat, seatIdx, record);
                break;

            case MJOption.Hu:
                // 胡
                HandleHu(seat, seatIdx, record);
                break;

            case MJOption.Guo:
                // 过 - 不做处理
                break;

            case MJOption.Ting:
                // 听牌（卡五星亮牌）
                HandleTing(seat, seatIdx, record);
                break;

            default:
                GF.LogWarning($"[回放] 未处理的操作类型: {record.Option}");
                break;
        }
    }

    /// <summary>
    /// 处理摸牌（参照 Handle2DMoPai）
    /// </summary>
    private void HandleDraw(MahjongSeat_Back seat, Msg_MJRecord record)
    {
        if (record.Card > 0)
        {
            // 直接添加到摸牌区（游戏中的逻辑）
            seat.AddMoPai(record.Card);

            MahjongFaceValue cardValue = Util.ConvertServerIntToFaceValue(record.Card);
        }
    }

    /// <summary>
    /// 处理打牌（带动画,参照 Handle2DDaPai）
    /// </summary>
    private IEnumerator HandleDiscardWithAnimation(MahjongSeat_Back seat, int seatIdx, Msg_MJRecord record)
    {
        // 标记动画开始,保存上下文
        isAnimationPlaying = true;
        currentAnimationSeat = seat;
        currentAnimationSeatIdx = seatIdx;
        currentAnimationCardValue = record.Card;

        if (record.Card > 0)
        {
            MahjongFaceValue cardValue = Util.ConvertServerIntToFaceValue(record.Card);
            lastDiscard = cardValue;
            lastDiscardSeatIdx = seatIdx;  // 记录打牌的座位索引

            // 判断从摸牌区还是手牌打出
            HandPaiType paiType = HandPaiType.HandPai;
            int paiIdx = 0;
            Vector3 startPos = Vector3.zero;

            // 处理得分变化（例如飘癞子扣分）
            if (record.CoinChange != null && record.CoinChange.Count > 0)
            {
                // 遍历所有玩家的得分变化
                foreach (var coinChange in record.CoinChange)
                {
                    long changePlayerId = coinChange.Key;
                    double scoreChange = coinChange.Val;

                    // 查找对应的座位
                    if (playerSeatMap.TryGetValue(changePlayerId, out int changeSeatIdx))
                    {
                        if (seats2D[changeSeatIdx] != null)
                        {
                            // 显示得分特效
                            seats2D[changeSeatIdx].ShowScoreChange(scoreChange);
                            // 更新玩家积分显示
                            seats2D[changeSeatIdx].UpdatePlayerCoin(scoreChange);
                        }
                    }
                    else
                    {
                        GF.LogWarning($"[回放] 找不到玩家{changePlayerId}的座位映射");
                    }
                }
            }

            // 检查摸牌区是否有牌
            if (seat.MoPaiContainer.childCount > 0)
            {
                // 检查摸牌区的牌值是否匹配
                Transform moPaiCard = seat.MoPaiContainer.GetChild(0);
                MjCard_2D mjCard = moPaiCard?.GetComponent<MjCard_2D>();
                if (mjCard != null && mjCard.cardValue == record.Card)
                {
                    // 从摸牌区打出，获取摸牌位置
                    paiType = HandPaiType.MoPai;
                    startPos = moPaiCard.position;
                    seat.ClearContainer(seat.MoPaiContainer);
                }
                else
                {
                    // 摸牌区有牌但不匹配，从手牌打出
                    paiIdx = FindHandCardIndex(seat, record.Card);
                    if (seat.HandContainer != null && paiIdx >= 0 && paiIdx < seat.HandContainer.childCount)
                    {
                        startPos = seat.HandContainer.GetChild(paiIdx).position;
                    }
                    seat.RemoveHandCard(paiIdx);
                }
            }
            else
            {
                // 摸牌区无牌,从手牌打出
                paiIdx = FindHandCardIndex(seat, record.Card);
                if (seat.HandContainer != null && paiIdx >= 0 && paiIdx < seat.HandContainer.childCount)
                {
                    startPos = seat.HandContainer.GetChild(paiIdx).position;
                }
                seat.RemoveHandCard(paiIdx);
            }

            // 整理手牌（参考MahjongGameUI_2D.cs line 289）
            seat.ArrangeHandCards();

            // 播放出牌动画（参考MahjongGameUI_2D.cs line 302）
            if (DapaiAniCard != null)
            {
                yield return StartCoroutine(PlayDaPaiAnimation(seatIdx, record.Card, cardValue, startPos, seat));
            }

            // 播放出牌音效
            Sound.PlayEffect(AudioKeys.MJ_EFFECT_AUDIO_SEND_CARD0);

            // 动画播放完成后,添加到弃牌区
            seat.AddDiscardCard(record.Card);
            RefreshAllBaoStatus();

            // 获取玩家ID用于音效性别判断
            long playerId = seat.deskPlayer?.BasePlayer?.PlayerId ?? 0;

            // 获取玩法类型，用于判断语音语言
            NetMsg.MJMethod mjMethod = mJPlayBack.DeskCommon.MjConfig.MjMethod;

            if (mJPlayBack.Laizi > 0)
            {
                if (record.Card == mJPlayBack.Laizi)
                {
                    MahjongAudio.PlayActionVoiceForPlayer("laizigang", playerId, mjMethod, 0);
                    seat.ShowEffect("PiaoLaizi", 1.5f);
                }
                else
                {
                    MahjongAudio.PlayCardVoiceForPlayer(Util.ConvertServerIntToFaceValue(record.Card), playerId, mjMethod);
                }
            }
            else
            {
                MahjongAudio.PlayCardVoiceForPlayer(Util.ConvertServerIntToFaceValue(record.Card), playerId, mjMethod);
            }

            // 显示弃牌标记
            ShowDiscardFlag(seatIdx);

        }

        // 清理动画上下文,标记动画结束
        currentAnimationSeat = null;
        currentAnimationSeatIdx = -1;
        currentAnimationCardValue = -1;
        currentAnimationCoroutine = null;
        isAnimationPlaying = false;
    }

    /// <summary>
    /// 播放出牌动画（参照 MahjongGameUI_2D.PlayDaPaiAnimation）
    /// </summary>
    private IEnumerator PlayDaPaiAnimation(int seatIdx, int cardValue, MahjongFaceValue faceValue, Vector3 startPos, MahjongSeat_Back seat)
    {
        // 激活动画牌并设置牌值
        DapaiAniCard.gameObject.SetActive(true);
        // 判断是否为赖子
        bool isLaizi = (mJPlayBack != null && mJPlayBack.Laizi > 0 && cardValue == mJPlayBack.Laizi);
        DapaiAniCard.SetCardValue(cardValue, isLaizi);

        // 设置起始位置
        DapaiAniCard.transform.position = startPos;
        DapaiAniCard.transform.localScale = Vector3.one * 0.8f;

        // 获取目标位置（弃牌区下一张牌的位置）
        Vector3 targetPos = seat.GetNextDiscardCardPosition();

        // 执行动画：缩放从0.8到1，同时移动到目标位置
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 缩放动画：0.8 -> 1
            float scale = Mathf.Lerp(0.8f, 1f, t);
            DapaiAniCard.transform.localScale = Vector3.one * scale;

            // 位置动画
            DapaiAniCard.transform.position = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        // 确保最终状态
        DapaiAniCard.transform.localScale = Vector3.one;
        DapaiAniCard.transform.position = targetPos;

        // 在终点停留0.2秒
        yield return new WaitForSeconds(0.2f);

        // 隐藏动画牌
        DapaiAniCard.gameObject.SetActive(false);

        // 标记动画结束
        isAnimationPlaying = false;
    }

    /// <summary>
    /// 查找手牌中指定牌值的索引
    /// </summary>
    private int FindHandCardIndex(MahjongSeat_Back seat, int cardValue)
    {
        if (seat.HandContainer == null)
            return 0;

        // 从后往前遍历，找到第一张匹配的牌
        for (int i = seat.HandContainer.childCount - 1; i >= 0; i--)
        {
            GameObject cardObj = seat.HandContainer.GetChild(i).gameObject;
            MjCard_2D mjCard = cardObj.GetComponent<MjCard_2D>();
            if (mjCard != null && mjCard.cardValue == cardValue)
            {
                return i;
            }
        }

        // 未找到则返回最后一张
        return Mathf.Max(0, seat.HandContainer.childCount - 1);
    }

    /// <summary>
    /// 处理碰（参照 Handle2DPengPai）
    /// </summary>
    private void HandlePeng(MahjongSeat_Back seat, int seatIdx, Msg_MJRecord record)
    {
        if (record.Meld != null && record.Meld.Card != null && record.Meld.Card.Val.Count >= 3)
        {
            List<int> cardValues = record.Meld.Card.Val.ToList();

            // 1. 从手牌移除对应的牌
            seat.RemoveCardsForPengChiGang(PengChiGangPaiType.PENG, cardValues.ToArray());

            // 2. 移除被碰的弃牌区的牌（参考MahjongGameUI_2D.cs line 520-533）
            if (lastDiscardSeatIdx >= 0 && lastDiscardSeatIdx < seats2D.Length)
            {
                var targetSeat = seats2D[lastDiscardSeatIdx];
                if (targetSeat != null && targetSeat.DiscardContainer != null && targetSeat.DiscardContainer.childCount > 0)
                {
                    int lastIndex = targetSeat.GetNewestDiscardIndex();
                    if (lastIndex >= 0)
                    {
                        Transform lastCard = targetSeat.DiscardContainer.GetChild(lastIndex);
                        if (lastCard != null)
                        {
                            DestroyImmediate(lastCard.gameObject);
                            targetSeat.UpdateLaiziCount();
                        }
                    }
                }
            }

            // 3. 播放特效和音效
            seat.ShowEffect("Peng", 1.5f, PengChiGangPaiType.PENG);

            // 4. 生成组牌显示
            seat.AddMeldGroup(cardValues);

            // 5. 整理手牌（参考MahjongGameUI_2D.cs line 540）
            seat.ArrangeHandCards();

            // 6. 碰牌后将一张手牌移到摸牌区（参考MahjongGameUI_2D.cs line 543）
            seat.EnsureMoPaiSlot();

            // 7. 隐藏弃牌标记
            HideDiscardFlag();

            if (IsXianTaoMode())
            {
                long playerId = 0;
                foreach (var kvp in playerSeatMap)
                {
                    if (kvp.Value == seatIdx)
                    {
                        playerId = kvp.Key;
                        break;
                    }
                }
                if (playerId != 0)
                {
                    int playerCount = playerInfos.Count;
                    int lockingSeatIdx = GetLockingSeatIdx(seatIdx, playerCount);
                    var lockingSeat = (lockingSeatIdx >= 0 && lockingSeatIdx < 4) ? seats2D[lockingSeatIdx] : null;
                    int lockCardThreshold = mJPlayBack?.DeskCommon?.MjConfig?.Xthh?.LockCard ?? 0;
                    if (lockingSeat != null && lockCardThreshold > 1 && lockingSeat.laiziDiscardCount >= lockCardThreshold)
                    {
                        openDoublePlayers.Add(playerId);
                    }
                }
                RefreshAllBaoStatus();
            }

        }
    }

    /// <summary>
    /// 处理吃（参照 Handle2DChiPai）
    /// </summary>
    private void HandleChi(MahjongSeat_Back seat, int seatIdx, Msg_MJRecord record)
    {
        if (record.Meld != null && record.Meld.Card != null && record.Meld.Card.Val.Count >= 3)
        {
            List<int> cardValues = record.Meld.Card.Val.ToList();

            // 1. 从手牌移除对应的牌
            seat.RemoveCardsForPengChiGang(PengChiGangPaiType.CHI, cardValues.ToArray());

            // 2. 移除被吃的弃牌区的牌（参考MahjongGameUI_2D.cs line 630-643）
            if (lastDiscardSeatIdx >= 0 && lastDiscardSeatIdx < seats2D.Length)
            {
                var targetSeat = seats2D[lastDiscardSeatIdx];
                if (targetSeat != null && targetSeat.DiscardContainer != null && targetSeat.DiscardContainer.childCount > 0)
                {
                    int lastIndex = targetSeat.GetNewestDiscardIndex();
                    if (lastIndex >= 0)
                    {
                        Transform lastCard = targetSeat.DiscardContainer.GetChild(lastIndex);
                        if (lastCard != null)
                        {
                            DestroyImmediate(lastCard.gameObject);
                            targetSeat.UpdateLaiziCount();
                        }
                    }
                }
            }

            // 3. 播放特效和音效
            seat.ShowEffect("Chi", 1.5f, PengChiGangPaiType.CHI);

            // 4. 生成组牌显示
            seat.AddMeldGroup(cardValues);

            // 5. 整理手牌（参考MahjongGameUI_2D.cs line 650）
            seat.ArrangeHandCards();

            // 6. 吃牌后将一张手牌移到摸牌区（参考MahjongGameUI_2D.cs line 653）
            seat.EnsureMoPaiSlot();

            // 7. 隐藏弃牌标记
            HideDiscardFlag();
            RefreshAllBaoStatus();

        }
    }

    /// <summary>
    /// 处理杠（参照 Handle2DGangPai）
    /// </summary>
    private void HandleGang(MahjongSeat_Back seat, int seatIdx, Msg_MJRecord record)
    {
        if (record.Meld != null && record.Meld.Card != null && record.Meld.Card.Val.Count >= 4)
        {
            List<int> cardValues = record.Meld.Card.Val.ToList();

            // 处理得分变化（杠牌时的分数结算）
            if (record.CoinChange != null && record.CoinChange.Count > 0)
            {
                // 遍历所有玩家的得分变化
                foreach (var coinChange in record.CoinChange)
                {
                    long changePlayerId = coinChange.Key;
                    double scoreChange = coinChange.Val;

                    // 查找对应的座位
                    if (playerSeatMap.TryGetValue(changePlayerId, out int changeSeatIdx))
                    {
                        if (seats2D[changeSeatIdx] != null)
                        {
                            // 显示得分特效
                            seats2D[changeSeatIdx].ShowScoreChange(scoreChange);
                            // 更新玩家积分显示
                            seats2D[changeSeatIdx].UpdatePlayerCoin(scoreChange);
                        }
                    }
                    else
                    {
                        GF.LogWarning($"[回放] 找不到玩家{changePlayerId}的座位映射");
                    }
                }
            }

            // 根据 subType 判断杠的类型
            // subType: 0=默认明杠, 1=明杠, 2=暗杠, 3=补杠, 4=皮杠, 5=癞子杠, 6=朝天杠, 7=朝天暗杠
            PengChiGangPaiType gangType = PengChiGangPaiType.GANG; // 默认明杠

            if (record.Meld.SubType == 2)
            {
                gangType = PengChiGangPaiType.AN_GANG; // 暗杠
            }
            else if (record.Meld.SubType == 3)
            {
                gangType = PengChiGangPaiType.BU_GANG; // 补杠
            }
            else if (record.Meld.SubType == 4)
            {
                gangType = PengChiGangPaiType.PI_GANG; // 皮杠
            }
            else if (record.Meld.SubType == 5)
            {
                gangType = PengChiGangPaiType.LAIZI_GANG; // 癞子杠
            }
            else if (record.Meld.SubType == 6)
            {
                gangType = PengChiGangPaiType.CHAO_TIAN_GANG; // 朝天杠（明杠赖根）
            }
            else if (record.Meld.SubType == 7)
            {
                gangType = PengChiGangPaiType.CHAO_TIAN_AN_GANG; // 朝天暗杠（暗杠赖根）
            }

            // 1. 从手牌移除对应的牌
            seat.RemoveCardsForPengChiGang(gangType, cardValues.ToArray());

            // 2. 对于明杠和朝天杠，需要移除被杠的弃牌区的牌（参考MahjongGameUI_2D.cs line 571-593）
            if (gangType == PengChiGangPaiType.GANG || gangType == PengChiGangPaiType.CHAO_TIAN_GANG)
            {
                if (lastDiscardSeatIdx >= 0 && lastDiscardSeatIdx < seats2D.Length)
                {
                    var targetSeat = seats2D[lastDiscardSeatIdx];
                    if (targetSeat != null && targetSeat.DiscardContainer != null && targetSeat.DiscardContainer.childCount > 0)
                    {
                        int lastIndex = targetSeat.GetNewestDiscardIndex();
                        if (lastIndex >= 0)
                        {
                            Transform lastCard = targetSeat.DiscardContainer.GetChild(lastIndex);
                            if (lastCard != null)
                            {
                                DestroyImmediate(lastCard.gameObject);
                                targetSeat.UpdateLaiziCount();
                            }
                        }
                    }
                }
            }

            // 3. 播放特效和音效
            seat.ShowEffect("Gang", 1.5f, gangType);

            // 4. 生成组牌显示
            seat.AddMeldGroup(gangType, cardValues);

            if (IsXianTaoMode())
            {
                // 如果是接杠（明杠）
                if (gangType == PengChiGangPaiType.GANG || gangType == PengChiGangPaiType.CHAO_TIAN_GANG)
                {
                    long playerId = 0;
                    foreach (var kvp in playerSeatMap)
                    {
                        if (kvp.Value == seatIdx)
                        {
                            playerId = kvp.Key;
                            break;
                        }
                    }
                    if (playerId != 0)
                    {
                        int playerCount = playerInfos.Count;
                        int lockingSeatIdx = GetLockingSeatIdx(seatIdx, playerCount);
                        var lockingSeat = (lockingSeatIdx >= 0 && lockingSeatIdx < 4) ? seats2D[lockingSeatIdx] : null;
                        int lockCardThreshold = mJPlayBack?.DeskCommon?.MjConfig?.Xthh?.LockCard ?? 0;
                        if (lockingSeat != null && lockCardThreshold > 1 && lockingSeat.laiziDiscardCount >= lockCardThreshold)
                        {
                            openDoublePlayers.Add(playerId);
                        }
                    }
                }
                RefreshAllBaoStatus();
            }

            // 5. 整理手牌（参考MahjongGameUI_2D.cs line 600）
            seat.ArrangeHandCards();

            // 6. 朝天杠（明杠赖根）后，需要将一张手牌移到摸牌区（朝天暗杠不需要摸牌）
            if (gangType == PengChiGangPaiType.CHAO_TIAN_GANG)
            {
                seat.EnsureMoPaiSlot();
            }

            // 7. 对于明杠和朝天杠，隐藏弃牌标记
            if (gangType == PengChiGangPaiType.GANG || gangType == PengChiGangPaiType.CHAO_TIAN_GANG)
            {
                HideDiscardFlag();
            }

        }
    }

    /// <summary>
    /// 处理听牌操作（卡五星亮牌）
    /// 流程: 打出牌 -> 计算听牌搭子 -> 搭子亮着其他变暗 -> 显示胡牌提示
    /// showCard: 暗铺的刻子（已成型的牌，需要变暗）
    /// </summary>
    private void HandleTing(MahjongSeat_Back seat, int seatIdx, Msg_MJRecord record)
    {
        if (!isKaWuXing)
        {
            // 非卡五星模式，直接显示听牌标记
            seat.ShowTingPaiIcon(true);
            return;
        }

        // 卡五星亮牌处理
        // 1. 打出牌到弃牌区（与HandleDiscard保持一致）
        if (record.Card > 0)
        {
            // 检查摸牌区是否有牌
            if (seat.MoPaiContainer != null && seat.MoPaiContainer.childCount > 0)
            {
                // 检查摸牌区的牌值是否匹配
                Transform moPaiCard = seat.MoPaiContainer.GetChild(0);
                MjCard_2D mjCard = moPaiCard?.GetComponent<MjCard_2D>();
                if (mjCard != null && mjCard.cardValue == record.Card)
                {
                    // 从摸牌区打出
                    seat.ClearContainer(seat.MoPaiContainer);
                }
                else
                {
                    // 摸牌区有牌但不匹配，从手牌打出
                    int paiIdx = FindHandCardIndex(seat, record.Card);
                    if (paiIdx >= 0)
                    {
                        seat.RemoveHandCard(paiIdx);
                    }
                }
            }
            else
            {
                // 摸牌区无牌，从手牌打出
                int paiIdx = FindHandCardIndex(seat, record.Card);
                if (paiIdx >= 0)
                {
                    seat.RemoveHandCard(paiIdx);
                }
            }

            // 整理手牌
            seat.ArrangeHandCards();

            // 添加到弃牌区
            seat.AddDiscardCard(record.Card);
            RefreshAllBaoStatus();

            // 记录打牌的座位索引（用于被碰吃时移除弃牌）
            lastDiscardSeatIdx = seatIdx;
        }

        // 2. 获取暗铺牌（showCard）- 这些是选择不参与胡牌计算的刻子，需要变暗
        // 如果 showCard 为空，表示没有暗铺，需要计算哪些牌参与听牌
        List<int> anPuCards = record.ShowCard?.ToList() ?? new List<int>();

        // 3. 获取当前手牌（打出牌后）
        List<int> currentHandCards = seat.GetCurrentHandCards();

        // 4. 计算需要亮出的牌（参与听牌组合的牌亮着，已成型不参与听牌的牌变暗）
        List<int> liangPaiCards = CalculateLiangPaiCards(currentHandCards, anPuCards);

        // 5. 显示亮牌效果（liangPaiCards中的牌亮着，其他变暗）
        // 不需要调整位置和顺序，只需要设置亮度
        seat.ShowLiangPaiEffect(liangPaiCards);

        // 6. 显示听牌图标
        seat.ShowTingPaiIcon(true);

        // 7. 计算并显示胡牌提示（使用当前手牌减去暗铺牌来计算可胡的牌）
        List<HuPaiTipsInfo> huPaiInfos = CalculateTingPaiInfo(currentHandCards, anPuCards, record.Card);
        if (huPaiInfos.Count > 0)
        {
            seat.ShowHuPaiTips(huPaiInfos);
        }

        // 8. 播放亮牌音效（根据玩法类型判断语音语言）
        long playerId = seat.deskPlayer?.BasePlayer?.PlayerId ?? 0;
        NetMsg.MJMethod mjMethod = mJPlayBack.DeskCommon.MjConfig.MjMethod;
        MahjongAudio.PlayActionVoiceForPlayer("liang", playerId, mjMethod, 0);

    }

    /// <summary>
    /// 撤销听牌操作（卡五星亮牌）
    /// 流程: 恢复弃牌到手牌/摸牌区 -> 清除亮牌效果 -> 隐藏胡牌提示
    /// </summary>
    private void UndoTing(MahjongSeat_Back seat, int seatIdx, Msg_MJRecord record)
    {
        if (!isKaWuXing)
        {
            // 非卡五星模式，隐藏听牌标记
            seat.ShowTingPaiIcon(false);
            return;
        }

        // 1. 移除弃牌区最后一张牌
        if (record.Card > 0 && seat.DiscardContainer != null && seat.DiscardContainer.childCount > 0)
        {
            int lastIndex = seat.GetNewestDiscardIndex();
            if (lastIndex >= 0)
            {
                Transform lastCard = seat.DiscardContainer.GetChild(lastIndex);
                if (lastCard != null)
                {
                    Object.Destroy(lastCard.gameObject);
                }
            }
        }

        // 2. 分析听牌前的状态（与UndoDiscard保持一致）
        Msg_MJRecord prevRecord = null;
        if (currentOptionIndex > 0)
        {
            prevRecord = options[currentOptionIndex - 1];
        }

        bool hadMoPaiBeforeTing = false;
        int moPaiValueBeforeTing = 0;

        // 特殊情况:撤回到第0步(初始状态),如果是庄家,需要恢复初始摸牌
        if (currentOptionIndex == 0 && seatIdx == bankerSeatIdx)
        {
            Msg_MJHandCard handCard = initialHandCards.FirstOrDefault(h => h.PlayerId == record.PlayerId);
            if (handCard != null && handCard.HandCard != null && handCard.HandCard.Count > 0)
            {
                hadMoPaiBeforeTing = true;
                moPaiValueBeforeTing = handCard.HandCard[handCard.HandCard.Count - 1];
            }
        }
        else if (prevRecord != null && prevRecord.PlayerId == record.PlayerId)
        {
            if (prevRecord.Option == MJOption.Draw)
            {
                hadMoPaiBeforeTing = true;
                moPaiValueBeforeTing = prevRecord.Card;
            }
            else if (prevRecord.Option == MJOption.Pen || prevRecord.Option == MJOption.Chi || prevRecord.Option == MJOption.Gang)
            {
                hadMoPaiBeforeTing = true;
            }
        }

        // 3. 还原状态
        if (record.Card > 0)
        {
            if (hadMoPaiBeforeTing && moPaiValueBeforeTing > 0 && moPaiValueBeforeTing == record.Card)
            {
                // 情况1: 从摸牌区打出
                seat.AddMoPai(record.Card);
            }
            else if (hadMoPaiBeforeTing && moPaiValueBeforeTing > 0)
            {
                // 情况2: 从手牌打出,但摸牌区有牌
                List<int> currentHandCards = new List<int>();
                if (seat.HandContainer != null)
                {
                    for (int i = 0; i < seat.HandContainer.childCount; i++)
                    {
                        GameObject cardObj = seat.HandContainer.GetChild(i).gameObject;
                        MjCard_2D mjCard = cardObj.GetComponent<MjCard_2D>();
                        if (mjCard != null)
                        {
                            currentHandCards.Add(mjCard.cardValue);
                        }
                    }
                }

                currentHandCards.Add(record.Card);
                currentHandCards.Remove(moPaiValueBeforeTing);

                seat.CreateHandCards(currentHandCards);
                seat.AddMoPai(moPaiValueBeforeTing);

            }
            else if (hadMoPaiBeforeTing)
            {
                // 情况3: 上一步是碰吃杠
                seat.AddMoPai(record.Card);
            }
            else
            {
                // 情况4: 从手牌打出,摸牌区无牌
                List<int> currentHandCards = new List<int>();
                if (seat.HandContainer != null)
                {
                    for (int i = 0; i < seat.HandContainer.childCount; i++)
                    {
                        GameObject cardObj = seat.HandContainer.GetChild(i).gameObject;
                        MjCard_2D mjCard = cardObj.GetComponent<MjCard_2D>();
                        if (mjCard != null)
                        {
                            currentHandCards.Add(mjCard.cardValue);
                        }
                    }
                }

                currentHandCards.Add(record.Card);
                seat.CreateHandCards(currentHandCards);

            }
        }

        // 4. 清除亮牌效果（恢复所有牌的正常亮度）
        seat.ClearLiangPaiEffect();

        // 5. 隐藏听牌图标
        seat.ShowTingPaiIcon(false);

        // 6. 隐藏胡牌提示
        seat.HideHuPaiTips();

    }

    /// <summary>
    /// 撤销胡牌操作（血流麻将专用）
    /// 流程: 移除胡牌容器最后一张牌 -> 还原胡牌到手牌/摸牌区或弃牌区 -> 恢复得分
    /// </summary>
    private void UndoHu(MahjongSeat_Back seat, int seatIdx, Msg_MJRecord record)
    {

        // 1. 移除胡牌容器中的最后一张胡牌
        int removedHuCard = seat.RemoveLastXueLiuHuCard();

        // 2. 判断胡牌类型（自摸 or 点炮）
        long winWayPlayerId = record.WinWayPlayer;
        int providerAbsSeatIdx = -1; // 放铳玩家绝对座位
        bool isZiMo = true;

        // 如果 WinWayPlayer 有值且不是自己，说明是点炮
        if (winWayPlayerId > 0 && winWayPlayerId != record.PlayerId)
        {
            if (playerSeatMap.TryGetValue(winWayPlayerId, out int absSeatIdx))
            {
                providerAbsSeatIdx = absSeatIdx;
                isZiMo = false;
            }
        }

        // 3. 还原胡牌
        if (record.Card > 0)
        {
            if (isZiMo)
            {
                // 自摸：还原到胡牌玩家的摸牌区或手牌
                // 查看上一个操作判断牌原来在哪里
                bool shouldAddToMoPai = false;

                if (currentOptionIndex > 0)
                {
                    Msg_MJRecord prevRecord = options[currentOptionIndex - 1];
                    // 如果上一步是自己摸牌或杠牌，说明胡的是摸牌区的牌
                    if (prevRecord.PlayerId == record.PlayerId)
                    {
                        if (prevRecord.Option == MJOption.Draw ||
                            prevRecord.Option == MJOption.Gang)
                        {
                            shouldAddToMoPai = true;
                        }
                    }
                }
                else
                {
                    // currentOptionIndex == 0：第一步操作就胡牌（开局天胡）
                    // 如果是庄家自摸，牌应该在摸牌区（初始化时庄家最后一张放摸牌区）
                    if (record.PlayerId == bankerPlayerId)
                    {
                        shouldAddToMoPai = true;
                    }
                }

                if (shouldAddToMoPai)
                {
                    // 还原到摸牌区
                    seat.AddMoPai(record.Card);
                }
                else
                {
                    // 还原到手牌
                    List<int> currentHandCards = seat.GetCurrentHandCards();
                    currentHandCards.Add(record.Card);
                    seat.CreateHandCards(currentHandCards);
                }
            }
            else
            {
                // 点炮：还原到放铳玩家的弃牌区
                if (providerAbsSeatIdx >= 0 && providerAbsSeatIdx < seats2D.Length)
                {
                    var providerSeat = seats2D[providerAbsSeatIdx];
                    if (providerSeat != null)
                    {
                        providerSeat.AddDiscardCard(record.Card);
                        RefreshAllBaoStatus();
                        ShowDiscardFlag(providerAbsSeatIdx);
                    }
                }
                else
                {
                    GF.LogWarning($"[回放] 撤销点炮胡牌失败，找不到放铳玩家座位: {providerAbsSeatIdx}");
                }
            }
        }

        // 4. 恢复得分变化
        if (record.CoinChange != null && record.CoinChange.Count > 0)
        {
            foreach (var coinChange in record.CoinChange)
            {
                long playerId = coinChange.Key;
                double scoreChange = 0 - coinChange.Val; // 取反恢复

                if (playerSeatMap.TryGetValue(playerId, out int changeSeatIdx))
                {
                    if (seats2D[changeSeatIdx] != null)
                    {
                        seats2D[changeSeatIdx].UpdatePlayerCoin(scoreChange);
                    }
                }
            }
        }

    }

    /// <summary>
    /// 计算需要亮出的牌（参与听牌组合的搭子）
    /// 调用 MahjongHuTingCheck.CalculateLiangPaiCards 进行计算
    /// </summary>
    private List<int> CalculateLiangPaiCards(List<int> handCards, List<int> anPuCards)
    {
        // 调试：打印传入的原始手牌

        // 如果检测器未初始化，所有牌都亮着
        if (!MahjongHuTingCheck.IsInitialized)
        {
            return new List<int>(handCards);
        }

        // 1. 先移除暗铺牌，得到参与听牌计算的手牌
        List<int> effectiveHandCards = new List<int>(handCards);
        if (anPuCards != null && anPuCards.Count > 0)
        {
            foreach (int anPuCard in anPuCards)
            {
                // 每种暗铺牌移除3张
                for (int i = 0; i < 3; i++)
                {
                    effectiveHandCards.Remove(anPuCard);
                }
            }
        }

        // 调试：打印移除暗铺后的有效手牌

        // 2. 将有效手牌转换为 byte[] 格式
        byte[] cardArray = new byte[34];
        foreach (int cardValue in effectiveHandCards)
        {
            MahjongFaceValue faceValue = Util.ConvertServerIntToFaceValue(cardValue);
            if (faceValue != MahjongFaceValue.MJ_UNKNOWN)
            {
                int index = (int)faceValue;
                if (index >= 0 && index < 34)
                {
                    cardArray[index]++;
                }
            }
        }

        // 调试：打印cardArray中非0的牌
        List<string> cardArrayInfo = new List<string>();
        for (int i = 0; i < 34; i++)
        {
            if (cardArray[i] > 0)
            {
                cardArrayInfo.Add($"{(MahjongFaceValue)i}={cardArray[i]}");
            }
        }

        // 3. 获取赖子索引
        byte laiziIndex = 255;
        if (currentLaiZi != MahjongFaceValue.MJ_UNKNOWN)
        {
            laiziIndex = (byte)((int)currentLaiZi);
        }

        // 4. 调用 MahjongHuTingCheck 的计算方法
        List<int> liangPaiIndices = MahjongHuTingCheck.Instance.CalculateLiangPaiCards(cardArray, laiziIndex);

        // 调试：打印liangPaiIndices
        List<string> indicesInfo = liangPaiIndices.Select(idx => $"{idx}({(MahjongFaceValue)idx})").ToList();

        // 5. 将索引转换回服务器牌值，并按原手牌顺序返回
        List<int> liangPaiCards = new List<int>();
        List<int> tempIndices = new List<int>(liangPaiIndices);

        foreach (int card in handCards)
        {
            MahjongFaceValue fv = Util.ConvertServerIntToFaceValue(card);
            if (fv != MahjongFaceValue.MJ_UNKNOWN)
            {
                int idx = (int)fv;
                if (tempIndices.Contains(idx))
                {
                    tempIndices.Remove(idx);
                    liangPaiCards.Add(card);
                }
            }
        }


        return liangPaiCards;
    }

    /// <summary>
    /// 计算听牌信息（根据手牌和打出的牌）
    /// 使用全局 MahjongHuTingCheck.Instance 进行真实的胡牌计算
    /// anPuCards: 暗铺牌列表（选择不参与胡牌计算的刻子），每种牌只记录一次，代表3张
    /// </summary>
    private List<HuPaiTipsInfo> CalculateTingPaiInfo(List<int> handCards, List<int> anPuCards, int discardCard)
    {
        List<HuPaiTipsInfo> huPaiInfos = new List<HuPaiTipsInfo>();

        // 检查 MahjongHuTingCheck 是否已初始化
        if (!MahjongHuTingCheck.IsInitialized)
        {
            GF.LogWarning("[回放] MahjongHuTingCheck 未初始化，无法计算听牌信息");
            return huPaiInfos;
        }

        // 将手牌转换为 byte[] 格式（与 MahjongHuTingCheck.GetHuCards 兼容）
        byte[] cardArray = new byte[34];
        foreach (int cardValue in handCards)
        {
            MahjongFaceValue faceValue = Util.ConvertServerIntToFaceValue(cardValue);
            if (faceValue != MahjongFaceValue.MJ_UNKNOWN)
            {
                int index = (int)faceValue;
                if (index >= 0 && index < 34)
                {
                    cardArray[index]++;
                }
            }
        }

        // 移除要打出的牌（听牌时已经打出，不参与胡牌计算）
        MahjongFaceValue discardFace = Util.ConvertServerIntToFaceValue(discardCard);
        if (discardFace != MahjongFaceValue.MJ_UNKNOWN)
        {
            int discardIndex = (int)discardFace;
            if (discardIndex >= 0 && discardIndex < 34 && cardArray[discardIndex] > 0)
            {
                cardArray[discardIndex]--;
            }
        }

        // 移除暗铺牌（不参与胡牌计算的刻子，每种3张）
        if (anPuCards != null && anPuCards.Count > 0)
        {
            foreach (int cardValue in anPuCards)
            {
                MahjongFaceValue faceValue = Util.ConvertServerIntToFaceValue(cardValue);
                if (faceValue != MahjongFaceValue.MJ_UNKNOWN)
                {
                    int index = (int)faceValue;
                    if (index >= 0 && index < 34)
                    {
                        // 暗铺牌每种3张
                        if (cardArray[index] >= 3)
                            cardArray[index] -= 3;
                        else
                            cardArray[index] = 0;
                    }
                }
            }
        }

        // 获取当前赖子索引
        byte laiziIndex = 255; // 默认无赖子
        if (currentLaiZi != MahjongFaceValue.MJ_UNKNOWN)
        {
            laiziIndex = (byte)((int)currentLaiZi);
        }

        // 调用 MahjongHuTingCheck 的 GetHuCards 方法获取真实的听牌信息
        HuPaiTipsInfo[] huCards = MahjongHuTingCheck.Instance.GetHuCards(cardArray, laiziIndex);

        if (huCards != null && huCards.Length > 0)
        {
            huPaiInfos.AddRange(huCards);
        }

        return huPaiInfos;
    }

    /// <summary>
    /// 处理胡牌
    /// 参考 MahjongGameUI_2D.Handle2DHuPai 的特效播放逻辑
    /// 支持血流麻将多次胡牌，使用 record.Types_ 和 record.WinWayPlayer
    /// </summary>
    private void HandleHu(MahjongSeat_Back seat, int seatIdx, Msg_MJRecord record)
    {
        // 获取胡牌牌型
        List<int> huTypes = record.Types_ != null && record.Types_.Count > 0 ? record.Types_.ToList() : null;

        // 获取点炮/自摸的人员（WinWayPlayer）
        long winWayPlayerId = record.WinWayPlayer;
        int providerSeatIdx = -1; // -1表示自摸
        int providerAbsSeatIdx = -1; // 放铳玩家绝对座位

        // 如果 WinWayPlayer 有值且不是自己，说明是点炮
        if (winWayPlayerId > 0 && winWayPlayerId != record.PlayerId)
        {
            // 获取放铳玩家的绝对座位索引
            if (playerSeatMap.TryGetValue(winWayPlayerId, out int absSeatIdx))
            {
                providerAbsSeatIdx = absSeatIdx;
                // 计算放铳玩家相对于胡牌玩家的方向
                int playerCount = playerInfos.Count;
                providerSeatIdx = (providerAbsSeatIdx - seatIdx + playerCount) % playerCount;
            }
        }

        bool isZiMo = (providerSeatIdx < 0);

        // 根据游戏规则判断胡牌特效类型
        // 卡五星/血流：统一逻辑 - 无番型播放基础特效（屁胡），有番型播放番型特效
        // 仙桃晃晃：根据 winWay 判断（黑摸/软摸/抓冲/杠开）
        string effectType = "RuanMo"; // 默认软摸（点炮胡）
        bool isHeiMo = false;
        bool isXianTao = (mJPlayBack?.DeskCommon?.MjConfig?.MjMethod == MJMethod.Huanghuang);

        if (isXianTao)
        {
            // 仙桃晃晃：根据 winWay 判断特效类型
            if (record.WinWay > 0)
            {
                switch (record.WinWay)
                {
                    case 1:
                        // 自摸：需要判定黑摸还是软摸
                        string zimoType = DetermineZiMoType(seat, record);
                        isHeiMo = (zimoType == "HeiMo");
                        effectType = isHeiMo ? "HeiMo" : "RuanMo";
                        break;
                    case 2:
                        effectType = "ZhuoChong"; // 抓冲（对应游戏中的 case 2）
                        break;
                    case 3:
                        // 杠开（对应游戏中的 case 3）- 仅在配置允许时显示
                        int gangKaiConfig = mJPlayBack?.DeskCommon?.MjConfig?.Xthh?.GangKai ?? 1;
                        if (gangKaiConfig == 1)
                        {
                            effectType = "GangKai";
                        }
                        else
                        {
                            string gangKaiZimoType = DetermineZiMoType(seat, record);
                            isHeiMo = (gangKaiZimoType == "HeiMo");
                            effectType = isHeiMo ? "HeiMo" : "RuanMo";
                        }
                        break;
                    default:
                        effectType = isZiMo ? "ZiMo" : "Hu";
                        break;
                }
            }
            else
            {
                // 无 winWay：根据是否自摸判断
                if (isZiMo)
                {
                    string zimoType = DetermineZiMoType(seat, record);
                    isHeiMo = (zimoType == "HeiMo");
                    effectType = isHeiMo ? "HeiMo" : "RuanMo";
                }
                else
                {
                    effectType = "RuanMo";
                }
            }
        }
        else
        {
            // 卡五星/血流：统一逻辑
            // 默认基础特效（屁胡）
            effectType = isZiMo ? "ZiMo" : "Hu";
        }

        // 移除胡牌对应的牌
        if (record.Card > 0)
        {
            RemoveXueLiuHuCardFromReplay(seatIdx, record.Card, providerAbsSeatIdx);
        }

        // 统一添加胡牌到 HuContainer 显示（三款游戏通用）
        if (record.Card > 0)
        {
            int huIndex = seat.HuContainer != null ? seat.HuContainer.childCount : 0;
            seat.AddXueLiuHuCard(record.Card, huIndex, providerSeatIdx, huTypes);
        }

        // 根据游戏规则显示胡牌特效
        if (isXianTao)
        {
            // 仙桃晃晃：直接显示判断好的特效（黑摸/软摸/抓冲/杠开）
            seat.ShowEffect(effectType, 2f);

            // 如果不是自摸，显示点炮特效
            if (!isZiMo && providerAbsSeatIdx >= 0 && providerAbsSeatIdx < seats2D.Length)
            {
                seats2D[providerAbsSeatIdx]?.ShowEffect("DianPao", 1.5f);
            }

        }
        else if (isKaWuXing || isXueLiu)
        {
            // 卡五星/血流：根据番型决定特效
            // 如果不是自摸，先显示点炮特效
            if (!isZiMo && providerAbsSeatIdx >= 0 && providerAbsSeatIdx < seats2D.Length)
            {
                seats2D[providerAbsSeatIdx]?.ShowEffect("DianPao", 1.5f);
            }

            // 获取番型特效列表（卡五星和血流共用同一个方法）
            var fanList = new Google.Protobuf.Collections.RepeatedField<long>();
            if (huTypes != null && huTypes.Count > 0)
            {
                foreach (var code in huTypes)
                {
                    fanList.Add(code);
                    string fanName = GameUtil.GetFanTypeName(code);
                }
            }

            List<string> huTypeEffects = GetKWXHuTypeEffectsFromFan(fanList);

            // 根据番型决定播放方式
            if (huTypeEffects.Count == 0)
            {
                // 无番型：播放基础胡牌特效（屁胡）
                seat.ShowEffect(effectType, 2f);
            }
            else
            {
                // 有番型：异步依次播放番型特效
                NetMsg.MJMethod mjMethod = mJPlayBack.DeskCommon.MjConfig.MjMethod;
                long winPlayerId = record.PlayerId;
                PlayHuTypeEffectsAsync(seat, winPlayerId, mjMethod, huTypeEffects).Forget();
            }

            // 卡五星：显示亮码牌
            if (isKaWuXing && mJPlayBack.Horse > 0)
            {
                ShowLiangMaPai(mJPlayBack.Horse);
            }
        }


        // 处理得分变化
        if (record.CoinChange != null && record.CoinChange.Count > 0)
        {
            // 遍历所有玩家的得分变化
            foreach (var coinChange in record.CoinChange)
            {
                long playerId = coinChange.Key;
                double scoreChange = coinChange.Val;

                // 查找对应的座位
                if (playerSeatMap.TryGetValue(playerId, out int changeSeatIdx))
                {
                    if (seats2D[changeSeatIdx] != null)
                    {
                        // 显示得分特效
                        seats2D[changeSeatIdx].ShowScoreChange(scoreChange);
                        // 更新玩家积分显示
                        seats2D[changeSeatIdx].UpdatePlayerCoin(scoreChange);
                    }
                }
                else
                {
                    GF.LogWarning($"[回放] 找不到玩家{playerId}的座位映射");
                }
            }
        }
    }

    /// <summary>
    /// 异步播放回放胡牌类型特效（清一色、碰碰胡等）
    /// 多个番型时连续播放
    /// </summary>
    private async UniTaskVoid PlayHuTypeEffectsAsync(MahjongSeat_Back seat, long huPlayerId, NetMsg.MJMethod mjMethod, IReadOnlyList<string> effectNames)
    {
        if (seat == null || effectNames == null || effectNames.Count == 0)
        {
            return;
        }

        for (int i = 0; i < effectNames.Count; i++)
        {
            var effectName = effectNames[i];
            // 播放牌型特效
            seat.ShowEffect(effectName, 1.5f);

            // 播放牌型语音
            MahjongAudio.PlayHuPaiTypeForPlayer(effectName, huPlayerId, mjMethod);


            // 如果有多个特效，等待一段时间后播放下一个
            if (effectNames.Count > 1 && i < effectNames.Count - 1)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(0.8));
            }
        }
    }

    /// <summary>
    /// 根据服务器返回的fan字段获取卡五星/血流胡牌类型特效列表
    /// 复用游戏逻辑中的同名方法
    /// </summary>
    private List<string> GetKWXHuTypeEffectsFromFan(Google.Protobuf.Collections.RepeatedField<long> fanList)
    {
        List<string> effects = new List<string>();

        if (fanList == null || fanList.Count == 0)
            return effects;

        foreach (long fanCode in fanList)
        {
            // 获取番型名称
            string fanName = GameUtil.GetFanTypeName(fanCode);

            if (string.IsNullOrEmpty(fanName))
                continue;

            // 只处理卡五星特有番型
            if (!GameUtil.IsKaWuXingFanType(fanCode))
                continue;

            // 根据番型编号映射到特效名称
            string effectName = GameUtil.GetKWXEffectNameByFanCode(fanCode);
            if (!string.IsNullOrEmpty(effectName) && !effects.Contains(effectName))
            {
                effects.Add(effectName);
            }
        }

        return effects;
    }

    /// <summary>
    /// 移除胡牌对应的牌（回放专用）
    /// 自摸：移除胡牌玩家摸牌区的牌
    /// 放铳：移除放铳玩家弃牌区的最后一张牌
    /// </summary>
    private void RemoveXueLiuHuCardFromReplay(int seatIdx, int huCard, int providerAbsSeatIdx)
    {
        if (providerAbsSeatIdx < 0)
        {
            // 自摸：优先从摸牌区移除
            var seat = seats2D[seatIdx];
            if (seat == null) return;

            bool removedFromMoPai = false;

            if (seat.MoPaiContainer != null && seat.MoPaiContainer.childCount > 0)
            {
                for (int i = seat.MoPaiContainer.childCount - 1; i >= 0; i--)
                {
                    Transform cardTrans = seat.MoPaiContainer.GetChild(i);
                    MjCard_2D mjCard = cardTrans.GetComponent<MjCard_2D>();
                    if (mjCard != null && mjCard.cardValue == huCard)
                    {
                        DestroyImmediate(cardTrans.gameObject);
                        removedFromMoPai = true;
                        break;
                    }
                }
            }

            // 如果摸牌区没有，从手牌移除
            if (!removedFromMoPai && seat.HandContainer != null && seat.HandContainer.childCount > 0)
            {
                for (int i = seat.HandContainer.childCount - 1; i >= 0; i--)
                {
                    Transform cardTrans = seat.HandContainer.GetChild(i);
                    MjCard_2D mjCard = cardTrans.GetComponent<MjCard_2D>();
                    if (mjCard != null && mjCard.cardValue == huCard)
                    {
                        DestroyImmediate(cardTrans.gameObject);
                        break;
                    }
                }
            }
        }
        else
        {
            // 放铳：移除放铳玩家弃牌区的最后一张牌
            if (providerAbsSeatIdx < seats2D.Length)
            {
                var providerSeat = seats2D[providerAbsSeatIdx];
                if (providerSeat != null && providerSeat.DiscardContainer != null && providerSeat.DiscardContainer.childCount > 0)
                {
                    int lastIndex = providerSeat.GetNewestDiscardIndex();
                    if (lastIndex >= 0)
                    {
                        Transform lastCard = providerSeat.DiscardContainer.GetChild(lastIndex);
                        if (lastCard != null)
                        {
                            DestroyImmediate(lastCard.gameObject);
                        }
                    }
                }
            }

            // 隐藏弃牌标记
            HideDiscardFlag();
        }
    }

    /// <summary>
    /// 判定自摸类型：黑摸还是软摸
    /// 定义：
    /// - 无赖子 = 黑摸
    /// - 有赖子但赖子作为本身参与胡牌 = 黑摸
    /// - 有赖子且赖子被当作万能牌代替其他牌 = 软摸
    /// </summary>
    private string DetermineZiMoType(MahjongSeat_Back seat, Msg_MJRecord record)
    {
        // 没有赖子：直接是黑摸
        if (mJPlayBack.Laizi <= 0)
        {
            return "HeiMo";
        }

        // 有赖子：需要判断赖子是否被代替使用
        // 获取完整手牌（手牌区+摸牌区）
        List<int> allCards = new List<int>();

        // 添加手牌
        if (seat.HandContainer != null)
        {
            for (int i = 0; i < seat.HandContainer.childCount; i++)
            {
                MjCard_2D card = seat.HandContainer.GetChild(i).GetComponent<MjCard_2D>();
                if (card != null)
                {
                    allCards.Add(card.cardValue);
                }
            }
        }

        // 添加摸牌区的牌
        if (seat.MoPaiContainer != null && seat.MoPaiContainer.childCount > 0)
        {
            MjCard_2D moPai = seat.MoPaiContainer.GetChild(0).GetComponent<MjCard_2D>();
            if (moPai != null)
            {
                allCards.Add(moPai.cardValue);
            }
        }

        // 检查手牌中是否有赖子
        bool hasLaizi = allCards.Contains(mJPlayBack.Laizi);
        if (!hasLaizi)
        {
            // 手牌中没有赖子：黑摸
            return "HeiMo";
        }

        // 手牌中有赖子：判断赖子是否被代替使用
        // 将赖子当作原始值，检查是否能胡牌
        bool canHuWithLaiziAsNormal = CanHuWithoutSubstitute(allCards, mJPlayBack.Laizi);

        if (canHuWithLaiziAsNormal)
        {
            // 赖子当作本身能胡牌 → 黑摸
            return "HeiMo";
        }
        else
        {
            // 赖子必须当作万能牌才能胡 → 软摸
            return "RuanMo";
        }
    }

    /// <summary>
    /// 判断手牌在赖子不代替其他牌的情况下是否能胡牌
    /// 简化算法：将牌按花色分组，检查每组是否能组成顺子/刻子+将牌
    /// </summary>
    private bool CanHuWithoutSubstitute(List<int> handCards, int laiziValue)
    {
        // 统计每种牌的数量
        Dictionary<int, int> cardCount = new Dictionary<int, int>();
        foreach (int card in handCards)
        {
            if (!cardCount.ContainsKey(card))
                cardCount[card] = 0;
            cardCount[card]++;
        }

        // 简化判定：检查是否有对子（将牌）
        // 尝试每种牌作为将牌
        foreach (var kvp in cardCount)
        {
            if (kvp.Value >= 2)
            {
                // 尝试将这张牌作为将牌
                Dictionary<int, int> remainCards = new Dictionary<int, int>(cardCount);
                remainCards[kvp.Key] -= 2;
                if (remainCards[kvp.Key] == 0)
                    remainCards.Remove(kvp.Key);

                // 检查剩余牌是否都能组成顺子或刻子
                if (CanFormMelds(remainCards))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 检查牌是否都能组成顺子或刻子（简化算法）
    /// </summary>
    private bool CanFormMelds(Dictionary<int, int> cardCount)
    {
        // 没有牌了，成功
        if (cardCount.Count == 0)
            return true;

        // 关键修复：必须按从小到大的顺序处理牌，才能正确组成顺子
        int firstCard = cardCount.Keys.Min();
        int count = cardCount[firstCard];

        // 尝试组成刻子（3张相同）
        if (count >= 3)
        {
            Dictionary<int, int> newCount = new Dictionary<int, int>(cardCount);
            newCount[firstCard] -= 3;
            if (newCount[firstCard] == 0)
                newCount.Remove(firstCard);

            if (CanFormMelds(newCount))
                return true;
        }

        // 尝试组成顺子（连续3张，仅适用于万/条/筒，且必须同花色）
        if (IsNumberCard(firstCard) && CanFormShunZi(firstCard))
        {
            int next1 = firstCard + 1;
            int next2 = firstCard + 2;

            if (cardCount.ContainsKey(next1) && cardCount.ContainsKey(next2))
            {
                Dictionary<int, int> newCount = new Dictionary<int, int>(cardCount);
                newCount[firstCard]--;
                newCount[next1]--;
                newCount[next2]--;

                if (newCount[firstCard] == 0) newCount.Remove(firstCard);
                if (newCount[next1] == 0) newCount.Remove(next1);
                if (newCount[next2] == 0) newCount.Remove(next2);

                if (CanFormMelds(newCount))
                    return true;
            }
        }

        // 无法组成合法牌型
        return false;
    }

    /// <summary>
    /// 判断是否为数字牌（万/条/筒 1-9）
    /// 服务器牌值范围：万1-9, 条11-19, 筒21-29
    /// </summary>
    private bool IsNumberCard(int cardValue)
    {
        // 万: 1-9
        if (cardValue >= 1 && cardValue <= 9)
            return true;
        // 条: 11-19
        if (cardValue >= 11 && cardValue <= 19)
            return true;
        // 筒: 21-29
        if (cardValue >= 21 && cardValue <= 29)
            return true;

        return false; // 字牌
    }

    /// <summary>
    /// 判断一张牌是否可以作为顺子的起始牌（只有1-7可以作为顺子起始）
    /// 服务器牌值范围：万1-9, 条11-19, 筒21-29
    /// </summary>
    private bool CanFormShunZi(int cardValue)
    {
        // 万: 1-7可以作为顺子起始
        if (cardValue >= 1 && cardValue <= 7)
            return true;
        // 条: 11-17可以作为顺子起始
        if (cardValue >= 11 && cardValue <= 17)
            return true;
        // 筒: 21-27可以作为顺子起始
        if (cardValue >= 21 && cardValue <= 27)
            return true;

        return false;
    }

    /// <summary>
    /// 显示指定座位的弃牌标记
    /// </summary>
    /// <param name="seatIdx">座位索引</param>
    /// <param name="cardValue">弃牌的牌值（可选，如果不指定则显示在最后一张弃牌上）</param>
    public void ShowDiscardFlag(int seatIdx, MahjongFaceValue cardValue = MahjongFaceValue.MJ_UNKNOWN)
    {
        if (discardFlag == null)
        {
            GF.LogWarning("[2D弃牌标记] discardFlag对象未设置");
            return;
        }

        var seat = seats2D[seatIdx];
        if (seat == null)
        {
            GF.LogWarning($"[2D弃牌标记] 无法获取座位{seatIdx}");
            return;
        }

        // 激活弃牌标记
        discardFlag.SetActive(true);

        // 获取弃牌位置
        Vector3 position;
        if (cardValue != MahjongFaceValue.MJ_UNKNOWN)
        {
            position = seat.GetDiscardCardPosition(cardValue);
        }
        else
        {
            position = seat.GetLastDiscardCardPosition();
        }

        // 设置标记位置
        if (position != Vector3.zero)
        {
            discardFlag.transform.position = position + new Vector3(0, 0.3f, 0); // 略微上移以避免遮挡

            // 停止之前的动画
            discardFlag.transform.DOKill();

            // 添加循环跳动动画（上下跳动）
            discardFlag.transform.DOLocalMoveY(discardFlag.transform.localPosition.y + 20f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            GF.LogInfo_gsc($"[2D弃牌标记] 显示在座位{seatIdx}的弃牌上，牌值:{cardValue}, 位置:{position}");
        }
        else
        {
            discardFlag.SetActive(false);
        }
    }

    /// <summary>
    /// 隐藏弃牌标记
    /// </summary>
    public void HideDiscardFlag()
    {
        if (discardFlag != null)
        {
            // 停止动画
            discardFlag.transform.DOKill();
            discardFlag.SetActive(false);
            GF.LogInfo_gsc("[2D弃牌标记] 已隐藏");
        }
    }

    /// <summary>
    /// 开始回放
    /// </summary>
    public void StartReplay()
    {
        if (isReplaying && !isPaused) return;

        if (isPaused)
        {
            // 继续播放
            isPaused = false;
            if (m_ReplayCoroutine == null)
            {
                m_ReplayCoroutine = CoroutineRunner.Instance.StartCoroutine(ReplayCoroutine(1f));
            }
        }
        else
        {
            // 重新开始播放
            isReplaying = true;
            isPaused = false;
            currentOptionIndex = 0;
            ReplayNextOperation();
        }
    }

    /// <summary>
    /// 重新开始回放
    /// </summary>
    public void RestartReplay()
    {

        StopReplay();

        // 重置所有座位（根据玩家信息和座位映射）
        foreach (var player in playerInfos)
        {
            long playerId = player.PlayerId;

            // 获取转换后的座位索引
            if (!playerSeatMap.TryGetValue(playerId, out int seatIdx))
            {
                continue;
            }

            if (seats2D[seatIdx] != null)
            {
                // 清空座位的牌数据（但不清空玩家信息）
                seats2D[seatIdx].ClearGameGos();

                // 重新显示玩家信息
                DeskPlayer deskPlayer = new DeskPlayer
                {
                    BasePlayer = player,
                    Coin = "0"
                };
                seats2D[seatIdx].UpdatePlayerInfo(deskPlayer, (FengWei)seatIdx);

                // 显示庄家标识
                bool isBanker = (playerId == bankerPlayerId);
                seats2D[seatIdx].ShowBankerIcon(isBanker, false);

                // 查找该玩家的初始手牌
                Msg_MJHandCard handCard = initialHandCards.FirstOrDefault(h => h.PlayerId == playerId);
                if (handCard != null && handCard.HandCard != null && handCard.HandCard.Count > 0)
                {
                    List<int> cards = handCard.HandCard.ToList();

                    // 规则1：庄家最后一张牌显示在摸牌区
                    if (isBanker && cards.Count > 0)
                    {
                        int lastCard = cards[cards.Count - 1];
                        cards.RemoveAt(cards.Count - 1);

                        seats2D[seatIdx].CreateHandCards(cards);
                        seats2D[seatIdx].AddMoPai(lastCard);
                    }
                    else
                    {
                        seats2D[seatIdx].CreateHandCards(cards);
                    }
                }
            }
        }

        // 重置指针指向庄家
        UpdatePointer(bankerSeatIdx);

        currentOptionIndex = 0;
        lastDiscard = MahjongFaceValue.MJ_UNKNOWN;
        HideDiscardFlag();

        // 重置阶段状态
        bool hasChangeCard = changeCardData != null && changeCardData.Count > 0;
        bool hasThrowCard = isXueLiu && throwCardData != null && throwCardData.Count > 0;

        if (hasChangeCard || hasThrowCard)
        {
            replayPhase = PHASE_DICE; // 重置为骰子阶段

            // 隐藏换牌相关UI
            HuanPaiAniPanel?.SetActive(false);
            BtnHuanPaiInfo?.gameObject.SetActive(false);
            TouZiAniPanel?.SetActive(false);
            TouZiResultImage?.gameObject.SetActive(false);

            // 停止换牌Spine动画
            if (HuanPaiAnis != null)
            {
                foreach (var anim in HuanPaiAnis)
                {
                    if (anim != null)
                    {
                        anim.AnimationState?.ClearTracks();
                        anim.gameObject.SetActive(false);
                    }
                }
            }
        }
        else
        {
            replayPhase = PHASE_NORMAL;
        }

        // 清除飘分显示
        foreach (var seat in seats2D)
        {
            if (seat != null)
            {
                seat.SetPiaoFenDisplay(-1);
                seat.ClearHuContainer();
            }
        }

        // 重新初始化飘分显示
        // LongDouble: Key = playerId, Val = piaoFenValue
        if (mJPlayBack != null && mJPlayBack.PiaoFen != null && mJPlayBack.PiaoFen.Count > 0)
        {
            foreach (var piaoFenInfo in mJPlayBack.PiaoFen)
            {
                if (piaoFenInfo == null) continue;

                long playerId = piaoFenInfo.Key;
                int piaoFenValue = (int)piaoFenInfo.Val;

                if (playerSeatMap.TryGetValue(playerId, out int seatIdx))
                {
                    if (seatIdx >= 0 && seatIdx < seats2D.Length && seats2D[seatIdx] != null)
                    {
                        seats2D[seatIdx].SetPiaoFenDisplay(piaoFenValue);
                    }
                }
            }
        }

        UpdateUI();

        StartReplay();
    }

    /// <summary>
    /// 停止回放
    /// </summary>
    public void StopReplay()
    {
        isReplaying = false;
        isPaused = false;
        if (m_ReplayCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(m_ReplayCoroutine);
            m_ReplayCoroutine = null;
        }
    }

    /// <summary>
    /// 暂停/继续回放
    /// </summary>
    public void PauseReplay()
    {
        if (!isReplaying) return;

        isPaused = !isPaused;
        UpdateUI();


        if (!isPaused && m_ReplayCoroutine == null)
        {
            // 继续播放
            m_ReplayCoroutine = CoroutineRunner.Instance.StartCoroutine(ReplayCoroutine(1f));
        }
    }

    /// <summary>
    /// 调整回放速度（循环切换）
    /// </summary>
    public void AdjustReplaySpeed(bool increase)
    {
        // 循环切换预设速度
        m_CurrentSpeedIndex = (m_CurrentSpeedIndex + 1) % m_SpeedPresets.Length;
        m_ReplaySpeed = m_SpeedPresets[m_CurrentSpeedIndex];
        UpdateUI();

    }


    protected override void OnButtonClick(object sender, string btId)
    {
        switch (btId)
        {
            case "关闭":
                GF.UI.Close(this.UIForm);
                break;
            case "GameInfoBtn":
                // 切换房间信息面板显示状态
                roomInfo.SetActive(!roomInfo.activeSelf);
                break;
            case "上一步":
                if (isReplaying && !isPaused)
                {
                    PauseReplay();
                }
                UnPlay();
                break;
            case "下一步":
                if (isReplaying && !isPaused)
                {
                    PauseReplay();
                }
                Play();
                break;
            case "重放":
                RestartReplay();
                break;
            case "暂停":
                PauseReplay();
                break;
            case "加速":
                AdjustReplaySpeed(true);
                break;
        }
    }

    #region  卡五星特殊处理
    [Header("===== 卡五星显示 =====")]
    public GameObject TouZiAniPanel;         //  骰子动画面板(换牌时根据骰子点数播放换牌方向动画)
    public SkeletonAnimation TouZiSpineAni;  //  骰子Spine动画
    public Image TouZiResultImage;           //  骰子结果图片
    public Button BtnHuanPaiInfo;            //  换牌结果按钮
    public List<SkeletonAnimation> HuanPaiAnis;         //  换牌动画展示(同时根据顺序完成XuanPaiEnds移到下一位置动画)
    public GameObject HuanPaiAniPanel;         //  换牌动画面板
    public GameObject LiangMaPai;         //  亮码牌动画展示

    // 卡五星状态
    private bool isKaWuXing = false;              // 是否为卡五星模式
    private List<Msg_MJHandCard> changeCardData = new(); // 换牌后的手牌数据

    /// <summary>
    /// 判断是否为卡五星玩法
    /// </summary>
    private bool IsKaWuXingMode()
    {
        if (mJPlayBack?.DeskCommon?.MjConfig == null) return false;
        return mJPlayBack.DeskCommon.MjConfig.MjMethod == MJMethod.Kwx;
    }

    private bool IsXianTaoMode()
    {
        return mJPlayBack?.DeskCommon?.MjConfig?.Xthh != null;
    }

    /// <summary>
    /// 重新计算所有玩家的解锁状态（全包）
    /// 用于回放撤回操作时恢复正确的解锁状态
    /// </summary>
    private void UpdateOpenDoublePlayers()
    {
        openDoublePlayers.Clear();
        if (!IsXianTaoMode()) return;

        int lockCardThreshold = mJPlayBack?.DeskCommon?.MjConfig?.Xthh?.LockCard ?? 0;
        if (lockCardThreshold <= 1) return;

        int playerCount = playerInfos.Count;

        // 模拟从第0步到当前步的所有操作，统计赖子弃牌并判定解锁
        Dictionary<int, int> tempLaiziCounts = new Dictionary<int, int>();
        for (int i = 0; i < 4; i++) tempLaiziCounts[i] = 0;

        for (int i = 0; i < currentOptionIndex; i++)
        {
            Msg_MJRecord record = options[i];
            if (!playerSeatMap.TryGetValue(record.PlayerId, out int seatIdx)) continue;

            if (record.Option == MJOption.Discard)
            {
                if (mJPlayBack.Laizi > 0 && record.Card == mJPlayBack.Laizi)
                {
                    tempLaiziCounts[seatIdx]++;
                }
            }
            else if (record.Option == MJOption.Pen || record.Option == MJOption.Gang)
            {
                // 判定该玩家在执行碰杠时是否处于被锁状态
                int lockingSeatIdx = GetLockingSeatIdx(seatIdx, playerCount);
                if (lockingSeatIdx >= 0 && tempLaiziCounts.ContainsKey(lockingSeatIdx) && tempLaiziCounts[lockingSeatIdx] >= lockCardThreshold)
                {
                    // 如果是杠，只有明杠（接杠）和朝天杠才触发解锁
                    if (record.Option == MJOption.Gang)
                    {
                        int subType = record.Meld?.SubType ?? -1;
                        if (subType == 0 || subType == 1 || subType == 6)
                        {
                            openDoublePlayers.Add(record.PlayerId);
                        }
                    }
                    else
                    {
                        // 碰牌直接触发解锁
                        openDoublePlayers.Add(record.PlayerId);
                    }
                }
            }
        }
    }

    public void RefreshAllBaoStatus()
    {
        if (!IsXianTaoMode()) return;

        var xthh = mJPlayBack?.DeskCommon?.MjConfig?.Xthh;
        if (xthh == null) return;

        int lockCardThreshold = xthh.LockCard;
        if (lockCardThreshold <= 1) return;

        int playerCount = playerInfos.Count;

        for (int i = 0; i < 4; i++)
        {
            var seat = seats2D[i];
            if (seat == null || !seat.gameObject.activeSelf || seat.deskPlayer == null || seat.deskPlayer.BasePlayer == null)
            {
                seat?.UpdateBaoStatus(false, false);
                continue;
            }

            long playerId = seat.deskPlayer.BasePlayer.PlayerId;
            bool isUnlocked = openDoublePlayers.Contains(playerId);

            // 判定是否被锁：下家弃牌赖子数 >= 阈值
            int lockingSeatIdx = GetLockingSeatIdx(i, playerCount);
            var lockingSeat = (lockingSeatIdx >= 0 && lockingSeatIdx < 4) ? seats2D[lockingSeatIdx] : null;
            bool isLocked = false;
            if (lockingSeat != null && lockingSeat.gameObject.activeSelf)
            {
                isLocked = lockingSeat.laiziDiscardCount >= lockCardThreshold;
            }

            seat.UpdateBaoStatus(isUnlocked, isLocked);
        }
    }

    private int GetLockingSeatIdx(int seatIdx, int playerCount)
    {
        if (playerCount == 2) return (seatIdx == 0) ? 2 : 0;
        if (playerCount == 3)
        {
            if (seatIdx == 0) return 3;
            if (seatIdx == 3) return 2;
            if (seatIdx == 2) return 0;
            return -1;
        }
        if (seatIdx == 0) return 3;
        if (seatIdx == 3) return 2;
        if (seatIdx == 2) return 1;
        if (seatIdx == 1) return 0;
        return -1;
    }

    /// <summary>
    /// 判断是否为血流麻将玩法
    /// </summary>
    private bool IsXueLiuMode()
    {
        if (mJPlayBack?.DeskCommon?.MjConfig == null) return false;
        // MJMethod.Xl = 血流麻将, MJMethod.Xz = 血战麻将
        return mJPlayBack.DeskCommon.MjConfig.MjMethod == MJMethod.Xl;
    }

    /// <summary>
    /// 获取骰子点数（用于换牌方向计算）
    /// </summary>
    private int GetDiceKey()
    {
        return mJPlayBack?.Dice?.Key ?? 1;
    }
    /// <summary>
    /// 播放卡五星骰子步骤（同步版本，用于Play/UnPlay）
    /// </summary>
    private void PlayKWXDiceStep()
    {

        if (TouZiAniPanel == null)
        {
            GF.LogWarning("[回放] TouZiAniPanel 为空，跳过骰子动画");
            return;
        }

        TouZiAniPanel.SetActive(true);
        int diceValue = Mathf.Clamp(GetDiceKey(), 1, 6);

        TouZiResultImage.SetSprite($"MJGame/KWX/touzi_{diceValue}.png");
        TouZiSpineAni?.gameObject.SetActive(true);
        TouZiResultImage.gameObject.SetActive(false);
        var trackEntry = TouZiSpineAni?.AnimationState?.SetAnimation(0, "saizi", false);
        if (trackEntry != null)
        {
            trackEntry.Complete += (entry) =>
            {
                TouZiResultImage?.gameObject.SetActive(true);
                TouZiSpineAni?.gameObject.SetActive(false);
            };
        }
        else
        {
            TouZiResultImage?.gameObject.SetActive(true);
            TouZiSpineAni?.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 撤销卡五星骰子步骤
    /// </summary>
    private void UnPlayKWXDiceStep()
    {
        TouZiAniPanel?.SetActive(false);
        TouZiResultImage?.gameObject.SetActive(false);
        TouZiSpineAni?.gameObject.SetActive(false);
    }

    /// <summary>
    /// 播放卡五星换牌步骤（同步版本，用于Play/UnPlay）
    /// </summary>
    private void PlayKWXChangeCardStep()
    {
        if (changeCardData == null || changeCardData.Count == 0) return;

        // 先取消所有正在进行的换牌动画，防止快速点击导致位置错误
        DOTween.Kill("KWXChangeCardAnimation");

        // 强制完成所有座位的下坠动画，确保牌在正确位置
        foreach (var seat in seats2D)
        {
            if (seat != null)
            {
                seat.CompleteDropAnimations();
            }
        }

        TouZiAniPanel?.SetActive(false);
        HuanPaiAniPanel?.SetActive(true);

        int animationType = CalculateChangeCardAnimationType();
        string animationTypeStr = "";
        if (HuanPaiAnis != null && HuanPaiAnis.Count > animationType && HuanPaiAnis[animationType] != null)
        {
            var animation = HuanPaiAnis[animationType];
            animationTypeStr = animationType switch { 0 => "shunshizhen", 1 => "nishizhen", 2 => "duijiahuan", _ => "shunshizhen" };
            animation.gameObject.SetActive(true);
            animation.AnimationState?.SetAnimation(0, animationTypeStr, false);
        }

        foreach (var changeCard in changeCardData)
        {
            if (!playerSeatMap.TryGetValue(changeCard.PlayerId, out int seatIdx)) continue;
            var originalHandCard = initialHandCards.FirstOrDefault(h => h.PlayerId == changeCard.PlayerId);
            if (originalHandCard == null) continue;

            var newCards = changeCard.HandCard.ToList();

            // changeCard.HandCard 最后三张就是换过来的新牌
            var changedInCards = newCards.Count >= 3
                ? newCards.Skip(newCards.Count - 3).ToList()
                : new List<int>();

            if (seats2D[seatIdx] != null)
            {
                bool isBanker = (changeCard.PlayerId == bankerPlayerId);
                var handCards = new List<int>(newCards);
                int moPaiCard = -1;
                if (isBanker && handCards.Count > 0)
                {
                    moPaiCard = handCards[handCards.Count - 1];
                    handCards.RemoveAt(handCards.Count - 1);

                    // 判断摸牌是否是换入的牌（需要播放动画）
                    bool moPaiIsChangedIn = changedInCards.Contains(moPaiCard);
                    if (moPaiIsChangedIn)
                    {
                        // 从换入牌列表中移除摸牌，避免手牌区重复播放动画
                        changedInCards.Remove(moPaiCard);
                    }

                    seats2D[seatIdx].CreateHandCardsWithDropAnimation(handCards, changedInCards);

                    // 摸牌区根据是否换入决定是否播放动画
                    if (moPaiIsChangedIn)
                    {
                        seats2D[seatIdx].AddMoPaiWithDropAnimation(moPaiCard);
                    }
                    else
                    {
                        seats2D[seatIdx].AddMoPai(moPaiCard);
                    }
                }
                else
                {
                    seats2D[seatIdx].CreateHandCardsWithDropAnimation(handCards, changedInCards);
                }
            }
        }
        animationTypeStr = animationType switch { 0 => "btn_shunshizhen", 1 => "btn_nishizhen", 2 => "btn_duijiahuan", _ => "btn_shunshizhen" };
        string spritePath = $"MJGame/KWX/exchangeLayer/{animationTypeStr}.png";
        BtnHuanPaiInfo.image.SetSprite(spritePath);
        BtnHuanPaiInfo.gameObject.SetActive(true);

        // 1秒后隐藏换牌动画面板
        DOVirtual.DelayedCall(1f, () =>
        {
            HuanPaiAniPanel?.SetActive(false);
            // 停止换牌Spine动画
            if (HuanPaiAnis != null)
            {
                foreach (var anim in HuanPaiAnis)
                {
                    if (anim != null && anim.gameObject.activeSelf)
                    {
                        anim.AnimationState?.ClearTracks();
                        anim.gameObject.SetActive(false);
                    }
                }
            }
        }).SetId("KWXChangeCardAnimation");
    }

    /// <summary>
    /// 撤销卡五星换牌步骤（恢复为初始手牌）
    /// </summary>
    private void UnPlayKWXChangeCardStep()
    {
        HuanPaiAniPanel?.SetActive(false);
        BtnHuanPaiInfo?.gameObject.SetActive(false);

        if (HuanPaiAnis != null)
        {
            foreach (var anim in HuanPaiAnis)
            {
                if (anim != null && anim.gameObject.activeSelf)
                {
                    anim.AnimationState?.ClearTracks();
                    anim.gameObject.SetActive(false);
                }
            }
        }

        foreach (var handCard in initialHandCards)
        {
            if (!playerSeatMap.TryGetValue(handCard.PlayerId, out int seatIdx)) continue;
            if (seats2D[seatIdx] == null) continue;

            var cards = handCard.HandCard.ToList();
            bool isBanker = (handCard.PlayerId == bankerPlayerId);
            if (isBanker && cards.Count > 0)
            {
                int lastCard = cards[cards.Count - 1];
                cards.RemoveAt(cards.Count - 1);
                seats2D[seatIdx].CreateHandCards(cards);
                seats2D[seatIdx].AddMoPai(lastCard);
            }
            else
            {
                seats2D[seatIdx].CreateHandCards(cards);
            }
        }
        TouZiAniPanel?.SetActive(true);
        TouZiResultImage?.gameObject.SetActive(true);
    }

    #region 血流回放 - 甩牌流程

    /// <summary>
    /// 播放甩牌步骤（血流专属）
    /// 甩牌数据格式：LongListInt - Key = playerId, Vals = [card1, card2, ...]
    /// </summary>
    private void PlayThrowCardStep()
    {
        if (throwCardData == null || throwCardData.Count == 0)
        {
            return;
        }


        // 先取消所有正在进行的甩牌相关动画，防止快速点击导致位置错误
        DOTween.Kill("XLThrowCardAnimation");

        // 强制完成所有座位的下坠动画，确保牌在正确位置
        foreach (var seat in seats2D)
        {
            if (seat != null)
            {
                seat.CompleteDropAnimations();
            }
        }

        HuanPaiAniPanel?.SetActive(false);

        foreach (var throwInfo in throwCardData)
        {
            if (throwInfo == null || throwInfo.Vals == null || throwInfo.Vals.Count == 0)
                continue;

            long playerId = throwInfo.Key;
            List<int> throwCards = throwInfo.Vals.ToList();

            if (throwCards.Count == 0)
                continue;

            if (!playerSeatMap.TryGetValue(playerId, out int seatIdx))
            {
                GF.LogWarning($"[回放] 甩牌找不到玩家座位: playerId={playerId}");
                continue;
            }

            var seat = seats2D[seatIdx];
            if (seat == null)
                continue;

            // 先从手牌移除甩出的牌
            seat.RemoveThrowCardsFromHand(throwCards);

            // 整理手牌位置
            seat.ArrangeHandCards();

            // 在副露区添加甩牌组
            seat.AddShuaiPaiGroup(throwCards);
        }
    }

    /// <summary>
    /// 撤销甩牌步骤（恢复手牌）
    /// </summary>
    private void UnPlayThrowCardStep()
    {
        if (throwCardData == null || throwCardData.Count == 0)
            return;


        // 清除所有玩家的甩牌组
        foreach (var seat in seats2D)
        {
            if (seat != null)
            {
                seat.ClearShuaiPaiGroups();
            }
        }

        // 判断使用哪个数据源恢复手牌
        // 如果有换牌数据，使用换牌后的手牌数据；否则使用初始手牌数据
        var restoreData = (changeCardData != null && changeCardData.Count > 0) ? changeCardData : initialHandCards;

        foreach (var handCardData in restoreData)
        {
            if (handCardData == null)
                continue;

            long playerId = handCardData.PlayerId;
            if (!playerSeatMap.TryGetValue(playerId, out int seatIdx))
                continue;

            var seat = seats2D[seatIdx];
            if (seat == null)
                continue;

            // 恢复手牌状态
            var cards = handCardData.HandCard.ToList();
            bool isBanker = (playerId == bankerPlayerId);

            if (isBanker && cards.Count > 0)
            {
                int lastCard = cards[cards.Count - 1];
                cards.RemoveAt(cards.Count - 1);
                seat.CreateHandCards(cards);
                seat.AddMoPai(lastCard);
            }
            else
            {
                seat.CreateHandCards(cards);
            }
        }
    }

    #endregion


    /// <summary>
    /// 播放换入牌的下坠动画
    /// </summary>
    private async Cysharp.Threading.Tasks.UniTask PlayChangeCardDropAnimation(MahjongSeat_Back seat, List<int> changedInCards, List<int> newHandCards)
    {
        if (seat == null) return;

        // 庄家处理：最后一张牌在摸牌区
        bool isBanker = (seat.deskPlayer?.BasePlayer?.PlayerId == bankerPlayerId);
        List<int> handCards = new List<int>(newHandCards);
        int moPaiCard = -1;

        if (isBanker && handCards.Count > 0)
        {
            moPaiCard = handCards[handCards.Count - 1];
            handCards.RemoveAt(handCards.Count - 1);
        }

        // 重新创建手牌（带下坠动画的牌会高亮显示）
        seat.CreateHandCardsWithDropAnimation(handCards, changedInCards);

        // 如果是庄家，处理摸牌区
        if (moPaiCard > 0)
        {
            seat.AddMoPai(moPaiCard);
        }

        // 等待动画完成
        await Cysharp.Threading.Tasks.UniTask.Delay(500);
    }

    /// <summary>
    /// 根据骰子key和房间人数计算换牌方向
    /// </summary>
    private int CalculateChangeCardAnimationType()
    {
        // 获取实际玩家数量
        int playerCount = playerInfos.Count;

        // 两人房：默认对家换
        if (playerCount == 2)
        {
            return 2; // 对家换
        }

        // 三人房及以上：根据骰子key判断
        int diceKey = GetDiceKey();

        // key单数=顺时针, key双数=逆时针
        if (diceKey % 2 == 1)
        {
            return 0; // 顺时针
        }
        else
        {
            return 1; // 逆时针
        }
    }

    /// <summary>
    /// 亮码牌动画开始Y偏移
    /// </summary>
    private const float LIANG_MA_PAI_START_Y = 500f;

    /// <summary>
    /// 亮码牌动画持续时间
    /// </summary>
    private const float LIANG_MA_PAI_DURATION = 0.5f;

    /// <summary>
    /// 显示亮码牌动画（卡五星胡牌时调用）
    /// </summary>
    /// <param name="horseValue">码牌值（服务器牌值）</param>
    private void ShowLiangMaPai(int horseValue)
    {
        if (LiangMaPai == null)
        {
            GF.LogWarning("[回放] LiangMaPai 未配置");
            return;
        }

        if (horseValue <= 0)
        {
            return;
        }

        // 显示亮码牌面板
        LiangMaPai.SetActive(true);

        // 获取MaPai子物体并设置牌值
        Transform maPaiTrans = LiangMaPai.transform.Find("MaPai");
        if (maPaiTrans != null)
        {
            MjCard_2D mjCard = maPaiTrans.GetComponent<MjCard_2D>();
            if (mjCard != null)
            {
                // 将服务器牌值转换为MahjongFaceValue
                MahjongFaceValue faceValue = Util.ConvertServerIntToFaceValue(horseValue);
                bool isLaizi = (mJPlayBack?.Laizi > 0 && horseValue == mJPlayBack.Laizi);
                mjCard.SetCardValue(faceValue, isLaizi);
            }
            else
            {
                GF.LogWarning("[回放] MaPai 上没有 MjCard_2D 组件");
            }
        }
        else
        {
            GF.LogWarning("[回放] LiangMaPai 下没有找到 MaPai 子物体");
        }

        // 播放掉落动画
        PlayLiangMaPaiDropAnimation();
    }

    /// <summary>
    /// 播放亮码牌掉落动画
    /// </summary>
    private void PlayLiangMaPaiDropAnimation()
    {
        if (LiangMaPai == null) return;

        // 获取需要做动画的子物体列表
        List<Transform> animTargets = new List<Transform>();

        Transform image1 = LiangMaPai.transform.Find("Image1");
        Transform image2 = LiangMaPai.transform.Find("Image2");
        Transform image3 = LiangMaPai.transform.Find("Image3");
        Transform maPai = LiangMaPai.transform.Find("MaPai");

        if (image1 != null) animTargets.Add(image1);
        if (image2 != null) animTargets.Add(image2);
        if (image3 != null) animTargets.Add(image3);
        if (maPai != null) animTargets.Add(maPai);

        // 使用Sequence依次播放动画
        Sequence seq = DOTween.Sequence();
        seq.SetTarget(LiangMaPai);

        for (int i = 0; i < animTargets.Count; i++)
        {
            Transform target = animTargets[i];
            RectTransform rectTrans = target as RectTransform;
            if (rectTrans == null)
                rectTrans = target.GetComponent<RectTransform>();

            if (rectTrans != null)
            {
                // 记录原始位置
                float originalY = rectTrans.anchoredPosition.y;

                // 设置初始位置（从高处开始）
                Vector2 startPos = rectTrans.anchoredPosition;
                startPos.y = originalY + LIANG_MA_PAI_START_Y;
                rectTrans.anchoredPosition = startPos;

                // 依次掉落动画（每个间隔0.1秒）
                float delay = i * 0.1f;
                seq.Insert(delay, rectTrans.DOAnchorPosY(originalY, LIANG_MA_PAI_DURATION)
                    .SetEase(Ease.OutBounce));
            }
        }

        // 动画完成后的回调
        seq.OnComplete(() =>
        {
        });
    }

    /// <summary>
    /// 隐藏亮码牌
    /// </summary>
    private void HideLiangMaPai()
    {
        if (LiangMaPai != null)
        {
            // 停止所有动画
            LiangMaPai.transform.DOKill(true);

            // 重置子物体位置
            Transform image1 = LiangMaPai.transform.Find("Image1");
            Transform image2 = LiangMaPai.transform.Find("Image2");
            Transform image3 = LiangMaPai.transform.Find("Image3");
            Transform maPai = LiangMaPai.transform.Find("MaPai");

            if (image1 != null) image1.DOKill(true);
            if (image2 != null) image2.DOKill(true);
            if (image3 != null) image3.DOKill(true);
            if (maPai != null) maPai.DOKill(true);

            LiangMaPai.SetActive(false);
        }
    }

    #endregion

}

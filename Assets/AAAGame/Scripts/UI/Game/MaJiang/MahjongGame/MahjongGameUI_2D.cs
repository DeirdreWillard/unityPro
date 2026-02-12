using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using NetMsg;
using static UtilityBuiltin;

/// <summary>
/// MahjongGameUI 的 2D 视图功能扩展
/// 使用 partial class 分离 2D 麻将界面逻辑，保持代码组织清晰
/// </summary>
public partial class MahjongGameUI
{
    #region 2D界面UI节点定义

    [Header("===== 2D房间信息显示 =====")]
    public Image tableBg;                           // 桌布
    public Image gameType;                           // 游戏类型图标
    public Text center2DRoomInfoText;               // 房间信息文本（合并显示：余牌/底分/局数/房号）
    public GameObject CountdownPanel2D;          // 2D倒计时面板
    public GameObject center2DPointer;              // 当前操作玩家指针（通过旋转指示不同座位）
    public Text center2DCountdownText;              // 倒计时文本

    // 四个座位的管理对象（在 Inspector 中直接拖入预制体的座位脚本）
    [Header("===== 2D四家座位 =====")]
    public MahjongSeat_2D[] seats2D = new MahjongSeat_2D[4];

    [Header("===== 仙桃晃晃 =====")]
    public Image suoPaiTips;                     // 锁牌提示图标
    // 锁牌相关状态
    // 记录已经触发解锁（翻倍）的玩家ID（来自重连或本地解锁）
    private System.Collections.Generic.HashSet<long> openDoublePlayers = new System.Collections.Generic.HashSet<long>();
    // 本地玩家是否处于被锁牌限制（显示suoPaiTips并限制碰/杠/小朝天）
    private bool isLocalLockedBySuoPai = false;

    // 2D操作按钮
    [Header("===== 2D操作按钮 =====")]
    public GameObject pcghtButtonPanel;         // 碰吃杠听胡按钮面板
    public Button pengButton;                   // 碰按钮
    public Button chiButton;                    // 吃按钮
    public Button gangButton;                   // 杠按钮
    public Button xiaoButton;                   // 笑按钮
    public Button tingButton;                   // 听按钮
    public Button huButton;                     // 胡按钮
    public Button guoButton;                    // 过按钮

    public Button ShowTableBtn_OP;              // 显示桌面按钮(专用于op显示隐藏)

    // 当前可执行的操作和目标牌
    private List<MJOption> current2DOptions = new List<MJOption>();
    private MahjongFaceValue lastDiscard = MahjongFaceValue.MJ_UNKNOWN;
    public GameObject discardFlag;               // 最后一张弃牌标记

    // 2D碰吃杠选择相关数据（参考3D实现）
    private List<MahjongFaceValue[]> current2DChiCombinations;      // 当前吃牌组合列表
    private List<MahjongFaceValue> current2DGangCards;              // 当前杠牌列表
    private int selected2DChiIndex = 0;                             // 选中的吃牌组合索引
    private int selected2DGangIndex = 0;                            // 选中的杠牌索引

    // 2D出牌控制相关
    public MjCard_2D DapaiAniCard;                             // 出牌动画牌对象
    private bool canPlayCard2D = false;                             // 是否可以出牌
    private HandPaiType select2DPaiType;                            // 选中的牌类型
    private int select2DPaiHandPaiIdx = -1;                         // 选中的手牌索引

    // 倒计时协程引用
    private Coroutine countdownCoroutine;

    // 显示桌面按钮按住隐藏操作面板的状态
    private bool isOpPanelHiddenByPress = false;
    private bool wasKwxPanelVisible = false;
    private bool wasNormalPanelVisible = false;


    #endregion

    #region 2D视图核心方法

    /// <summary>
    /// 判断当前玩家是否是旁观者（不在座位上）
    /// </summary>
    /// <returns>true表示旁观者，false表示座位玩家</returns>
    public bool IsSpectator()
    {
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        int mySeatIdx = baseMahjongGameManager?.GetSeatIndexByPlayerId(myPlayerId) ?? -1;
        return mySeatIdx < 0;
    }

    /// <summary>
    /// 判断指定座位是否是本地玩家（而非固定判断seatIdx==0）
    /// 旁观者进入时，0号位可能是其他玩家
    /// </summary>
    /// <param name="seatIdx">座位索引</param>
    /// <returns>true表示本地玩家，false表示其他玩家</returns>
    private bool IsSeatLocalPlayer(int seatIdx)
    {
        var seat = GetSeat(seatIdx);
        if (seat == null || seat.deskPlayer == null) return false;
        long myPlayerId = Util.GetMyselfInfo()?.PlayerId ?? 0;
        return seat.deskPlayer.BasePlayer.PlayerId == myPlayerId;
    }

    /// <summary>
    /// 初始化2D
    /// 优化：座位通过 gameUI2D 引用自动获取资源，无需手动传参
    /// </summary>
    private void Initialize2DUI()
    {
        HideDiscardFlag();

        // 为每个座位调用初始化（座位会自动从 gameUI2D 引用获取资源）
        for (int i = 0; i < 4; i++)
        {
            seats2D[i].Initialize(this);
        }

        // 初始化所有座位的玩家信息
        Update2DAllPlayerInfo();

        SetTableBg();

        // 初始化锁牌提示（如果房间配置了锁牌）
        InitializeSuoPaiTips();

        RefreshAllBaoStatus();

        // 订阅麻将牌样式改变事件
        MahjongSettings.OnCardStyleChanged += OnCardStyleChanged;
    }

    /// <summary>
    /// 初始化锁牌提示图（根据房间配置的 LockCard 值选择不同图片）
    /// 如果房间开启了锁牌规则，则预加载对应提示图并根据重连/本地状态显示或隐藏
    /// </summary>
    private void InitializeSuoPaiTips()
    {
        suoPaiTips.gameObject.SetActive(false);

        // 读取房间配置
        if (mj_procedure == null || mj_procedure.enterMJDeskRs == null || mj_procedure.enterMJDeskRs.BaseInfo == null)
        {
            return;
        }

        var xthh = mj_procedure.enterMJDeskRs.BaseInfo.MjConfig?.Xthh;
        if (xthh == null) return;

        int lockCard = xthh.LockCard;
        if (lockCard <= 1)
        {
            // 不锁牌，保持隐藏
            return;
        }

        // 选择图片路径（locktip_2/3/4）
        suoPaiTips.SetSprite($"MJGame/MJGameCommon/locktip_{lockCard}.png");
    }

    /// <summary>
    /// 检查某座位飘赖子数是否达到阈值，若达到则提示其上家锁牌
    /// 锁牌规则：2人桌是2号位（对家），3/4人桌是3号位（下家/右家）飘赖子触发0号位（本地玩家）锁牌
    /// </summary>
    /// <param name="seatIdx">弃牌的座位索引</param>
    private void CheckAndShowSuoPaiTips(int seatIdx)
    {
        if(IsXianTaoRule == false)
        {
            // 非仙桃晃晃规则不处理
            return;
        }

        // 根据人数判断下家座位：2人桌下家是2号位（对家），3/4人桌下家是3号位（右家）
        int playerCount = GetRealPlayerCount();
        int nextSeat = (playerCount == 2) ? 2 : 3;

        // 只有下家飘赖子才触发本地玩家（0号位）的锁牌提示
        if (seatIdx != nextSeat) return;

        var seat = GetSeat(seatIdx);
        if (seat == null) return;

        // 读取房间锁牌阈值
        int lockCardThreshold = mj_procedure.enterMJDeskRs.BaseInfo.MjConfig.Xthh.LockCard;

        if (lockCardThreshold <= 1) return; // 不锁牌

        // 从座位类获取该座位弃牌中赖子数量
        int laiziCount = seat.laiziDiscardCount;

        if (laiziCount >= lockCardThreshold)
        {
            long myId = GetSeat(0)?.deskPlayer?.BasePlayer?.PlayerId ?? 0;
            if (!openDoublePlayers.Contains(myId))
            {
                isLocalLockedBySuoPai = true;
                suoPaiTips.gameObject.SetActive(true);
                GF.LogInfo_gsc($"[锁牌] 下家座位{seatIdx}飘赖子数{laiziCount}达到阈值{lockCardThreshold}，显示锁牌提示");
            }
        }
        RefreshAllBaoStatus();
    }

    /// <summary>
    /// 刷新所有座位的解锁状态显示（仙桃晃晃）
    /// </summary>
    public void RefreshAllBaoStatus()
    {
        if (!IsXianTaoRule) return;

        var xthh = mj_procedure?.enterMJDeskRs?.BaseInfo?.MjConfig?.Xthh;
        if (xthh == null) return;

        int lockCardThreshold = xthh.LockCard;
        if (lockCardThreshold <= 1) return;

        int playerCount = GetRealPlayerCount();

        for (int i = 0; i < 4; i++)
        {
            var seat = GetSeat(i);
            if (seat == null || seat.deskPlayer == null || seat.deskPlayer.BasePlayer == null)
            {
                seat?.UpdateBaoStatus(false, false);
                continue;
            }

            long playerId = seat.deskPlayer.BasePlayer.PlayerId;
            bool isUnlocked = openDoublePlayers.Contains(playerId);

            // 判定是否被锁：下家弃牌赖子数 >= 阈值
            int lockingSeatIdx = GetLockingSeatIdx(i, playerCount);
            var lockingSeat = GetSeat(lockingSeatIdx);
            bool isLocked = false;
            if (lockingSeat != null)
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
            // 0 -> 3 -> 2 -> 0
            if (seatIdx == 0) return 3;
            if (seatIdx == 3) return 2;
            if (seatIdx == 2) return 0;
            return -1;
        }

        // 4人桌: 0 -> 3 -> 2 -> 1 -> 0
        if (seatIdx == 0) return 3;
        if (seatIdx == 3) return 2;
        if (seatIdx == 2) return 1;
        if (seatIdx == 1) return 0;

        return -1;
    }

    /// <summary>
    /// 清空锁牌相关状态（下一局/关闭界面时调用）
    /// </summary>
    public void ClearSuoPaiState()
    {
        isLocalLockedBySuoPai = false;
        openDoublePlayers.Clear();
        suoPaiTips.gameObject.SetActive(false);
        RefreshAllBaoStatus();
    }

    /// <summary>
    /// 重连时检查是否需要显示锁牌提示
    /// 根据下家弃牌中的赖子数量判断
    /// </summary>
    private void CheckSuoPaiTipsOnReconnect()
    {
        // 读取房间锁牌阈值
        if (mj_procedure?.enterMJDeskRs?.BaseInfo?.MjConfig?.Xthh == null)
        {
            return;
        }

        int lockCardThreshold = mj_procedure.enterMJDeskRs.BaseInfo.MjConfig.Xthh.LockCard;
        if (lockCardThreshold <= 1)
        {
            // 不锁牌规则
            return;
        }

        // 根据人数判断下家座位：2人桌下家是2号位（对家），3/4人桌下家是3号位（右家）
        int playerCount = GetRealPlayerCount();
        int nextSeat = (playerCount == 2) ? 2 : 3;

        var seat = GetSeat(nextSeat);
        if (seat == null)
        {
            return;
        }

        // 获取下家弃牌中的赖子数量
        int laiziCount = seat.laiziDiscardCount;

        if (laiziCount >= lockCardThreshold)
        {
            // 下家赖子数达到阈值，显示锁牌提示
            isLocalLockedBySuoPai = true;
            suoPaiTips.SetSprite($"MJGame/MJGameCommon/locktip_{lockCardThreshold}.png");
            suoPaiTips.gameObject.SetActive(true);
        }
        else
        {
            isLocalLockedBySuoPai = false;
            suoPaiTips.gameObject.SetActive(false);
        }
        RefreshAllBaoStatus();
    }

    public void SetTableBg()
    {
        // 从本地缓存读取桌布设置（索引 1-9）
        int tableBgIndex = MahjongSettings.GetTableBg();
        tableBg.SetSprite($"MJGame/Bgs/{tableBgIndex}.png");
    }

    /// <summary>
    /// 获取指定座位
    /// </summary>
    public MahjongSeat_2D GetSeat(int seatIdx)
    {
        if (seatIdx < 0 || seatIdx >= 4) return null;
        return seats2D[seatIdx];
    }

    /// <summary>
    /// 获取玩家数量
    /// </summary>
    private int GetRealPlayerCount()
    {
        if (baseMahjongGameManager == null)
        {
            GF.LogError("[2D视图] baseMahjongGameManager 为空,返回默认玩家数量4");
            return 4;
        }
        return baseMahjongGameManager.realPlayerCount;
    }

    /// <summary>
    /// 获取座位风位
    /// </summary>
    private FengWei GetSeatFengWei(int seatIdx)
    {
        if (seatIdx < 0 || seatIdx >= 4) return FengWei.EAST;

        if (baseMahjongGameManager == null)
        {
            GF.LogError("[2D视图] baseMahjongGameManager 为空,返回默认风位EAST");
            return FengWei.EAST;
        }

        return baseMahjongGameManager.SeatFengWei[seatIdx];
    }

    /// <summary>
    /// 获取剩余牌数
    /// </summary>
    private int GetRemainCardCount()
    {
        if (baseMahjongGameManager == null)
        {
            GF.LogError("[2D视图] baseMahjongGameManager 为空,返回默认剩余牌数0");
            return 0;
        }
        return baseMahjongGameManager.richMjCount;
    }

    #endregion

    #region 2D玩家进入/离开处理

    /// <summary>
    /// 2D模式下处理玩家离开（Function_SynSitUp）
    /// </summary>
    public void Handle2DPlayerLeave(int seatIdx)
    {
        var seat = GetSeat(seatIdx);
        // 清空玩家信息（会调用ClearPlayerInfo -> ClearAll）
        seat.UpdatePlayerInfo(null, FengWei.EAST);
    }

    #endregion

    #region 2D准备状态管理

    /// <summary>
    /// 设置玩家2D准备状态显示
    /// </summary>
    /// <param name="seatIdx">座位索引</param>
    /// <param name="isReady">是否准备</param>
    public void Set2DReadyState(int seatIdx, bool isReady)
    {
        var seat = GetSeat(seatIdx);
        seat?.SetReadyState(isReady);
    }

    /// <summary>
    /// 重置所有玩家的准备状态（结算后还原到等待状态）
    /// </summary>
    public void Reset2DAllPlayersReadyState()
    {
        // 清除所有座位的准备状态显示
        for (int seatIdx = 0; seatIdx < 4; seatIdx++)
        {
            Set2DReadyState(seatIdx, false);
        }
    }

    #endregion

    #region 2D消息处理方法

    /// <summary>
    /// 2D模式下处理准备消息
    /// </summary>
    public void Handle2DMJReadyRs(Msg_MJReadyRs ack)
    {
        Set2DReadyState(0, ack.ReadyState == 1);
    }

    /// <summary>
    /// 2D模式下处理同步准备消息
    /// </summary>
    public void Handle2DSynMJReady(Msg_SynMJReady ack, int seatIdx)
    {
        Set2DReadyState(seatIdx, ack.ReadyState == 1);
    }

    /// <summary>
    /// 2D模式下处理摸牌
    /// </summary>
    public void Handle2DMoPai(int seatIdx, int cardValue)
    {
        // 播放摸牌音效
        MahjongAudio.PlayVoice(AudioKeys.MJ_EFFECT_AUDIO_MOPAI);
        GetSeat(seatIdx).AddMoPai(cardValue);

        // 如果是本地玩家摸牌
        if (IsSeatLocalPlayer(seatIdx))
        {
            var seat = GetSeat(0);
            // 血流已胡牌则跳过听牌计算
            if (IsXueLiuRule && seat != null && seat.IsXueLiuHu)
            {
                // 跳过听牌标记计算
            }
            else
            {
                CheckAndShowTingMarks();
            }
            
            // 血流麻将检查花猪提示
            if (IsXueLiuRule)
            {
                CheckAndShowHuaZhuTip();
            }
        }

        // 实时更新剩余牌数（摸牌后牌堆减少）
        Update2DRoomInfo();
    }

    /// <summary>
    /// 2D模式下处理打牌
    /// </summary>
    public void Handle2DDaPai(int seatIdx, int cardValue, HandPaiType paiType, int paiIdx)
    {
        var seat = GetSeat(seatIdx);

        // 播放出牌音效（区分赖子和普通牌）
        MahjongFaceValue faceValue = Util.ConvertServerIntToFaceValue(cardValue);

        // 更新最后打出的牌（用于后续碰杠胡等操作判断）
        lastDiscard = faceValue;

        // 获取手牌位置（用于动画起点）
        Vector3 startPos = Vector3.zero;
        bool isLaizi = baseMahjongGameManager.IsLaiZiCard(faceValue);

        // 亮牌后只能打摸牌，手牌已固定在组牌区
        if (IsSeatLocalPlayer(seatIdx) && seat.HasLiangPai)
        {
            // 亮牌后：直接从摸牌区移除
            if (seat.MoPaiContainer != null && seat.MoPaiContainer.childCount > 0)
            {
                startPos = seat.MoPaiContainer.GetChild(seat.MoPaiContainer.childCount - 1).position;
            }
            seat.ClearContainer(seat.MoPaiContainer);
        }
        else if (paiType == HandPaiType.MoPai)
        {
            // 从摸牌区打出，获取摸牌位置
            if (seat.MoPaiContainer != null && seat.MoPaiContainer.childCount > 0)
            {
                startPos = seat.MoPaiContainer.GetChild(seat.MoPaiContainer.childCount - 1).position;
            }
            seat.ClearContainer(seat.MoPaiContainer);
        }
        else
        {
            // 从手牌打出，获取手牌位置
            if (seat.HandContainer != null && paiIdx >= 0 && paiIdx < seat.HandContainer.childCount)
            {
                startPos = seat.HandContainer.GetChild(paiIdx).position;
            }
            seat.RemoveHandCard(paiIdx);
        }

        seat.ArrangeHandCards();

        // 如果是本地玩家打牌，清除听牌标记并检查是否已听牌
        if (IsSeatLocalPlayer(seatIdx))
        {
            ClearAllTingMarks(); // 打牌后清除听牌标记
            CheckHuPaiBtnVisibility();
            
            // 血流麻将检查花猪提示（打牌后手牌变化）
            if (IsXueLiuRule)
            {
                CheckAndShowHuaZhuTip();
            }

            // 本地玩家跳过出牌动画，直接添加弃牌
            OnDaPaiAnimationComplete(seatIdx, cardValue, faceValue, seat);
        }
        else
        {
            // 其他玩家播放出牌动画
            StartCoroutine(PlayDaPaiAnimation(seatIdx, cardValue, faceValue, startPos, isLaizi, seat));
        }
    }

    /// <summary>
    /// 播放出牌动画
    /// </summary>
    private System.Collections.IEnumerator PlayDaPaiAnimation(int seatIdx, int cardValue, MahjongFaceValue faceValue, Vector3 startPos, bool isLaizi, MahjongSeat_2D seat)
    {
        // 激活动画牌并设置牌值
        DapaiAniCard.gameObject.SetActive(true);
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

        // 执行出牌动画完成后的逻辑
        OnDaPaiAnimationComplete(seatIdx, cardValue, faceValue, seat);
    }

    /// <summary>
    /// 出牌动画完成后的处理（添加弃牌、音效、弃牌标记等）
    /// 本地玩家直接调用跳过动画，其他玩家在动画结束后调用
    /// </summary>
    private void OnDaPaiAnimationComplete(int seatIdx, int cardValue, MahjongFaceValue faceValue, MahjongSeat_2D seat)
    {
        // 播放出牌音效
        Sound.PlayEffect(AudioKeys.MJ_EFFECT_AUDIO_SEND_CARD0);

        // 添加弃牌（座位类内部会自动更新laiziDiscardCount）
        seat.AddDiscardCard(cardValue);

        // 检查该座位赖子弃牌数是否达到锁牌阈值，触发上家锁牌提示
        CheckAndShowSuoPaiTips(seatIdx);

        // 获取玩家ID用于音效性别判断
        long playerId = seat.deskPlayer?.BasePlayer?.PlayerId ?? 0;

        MJMethod mJMethod = mj_procedure.enterMJDeskRs.BaseInfo.MjConfig.MjMethod;
        if (currentRule != null && currentRule.HasLaiZi())
        {
            if (baseMahjongGameManager.IsLaiZiCard(faceValue))
            {
                MahjongAudio.PlayActionVoiceForPlayer("laizigang", playerId, mJMethod, 0);
                seat.ShowEffect("PiaoLaizi", 1.5f);
            }
            else
            {
                MahjongAudio.PlayCardVoiceForPlayer(faceValue, playerId, mJMethod);
            }
        }
        else
        {
            MahjongAudio.PlayCardVoiceForPlayer(faceValue, playerId, mJMethod);
        }

        // 显示弃牌标记（在最新打出的牌上）
        ShowDiscardFlag(seatIdx, faceValue);
    }

    /// <summary>
    /// 玩家交互出牌处理（拖拽/点击/双击）
    /// 2D模式专用,使用2D独立的状态管理
    /// </summary>
    public void OnPlayerPlayCard(int seatIdx, int cardIndex, int cardValue, HandPaiType paiType)
    {
        // 只处理0号座位（本地玩家）的出牌
        if (seatIdx != 0)
        {
            return;
        }

        // 使用2D独立的状态检查
        if (!Can2DPlayCard())
        {
            return;
        }

        // 检查是否是其他玩家亮牌后的禁牌
        if (IsOtherPlayerTingCard(cardValue))
        {
            ShowDissOutCardTips();
            return;
        }

        // 保存玩家UI选择的牌信息，供服务器响应时使用
        select2DPaiType = paiType;
        select2DPaiHandPaiIdx = cardIndex;

        // 转换牌值为服务器格式
        MahjongFaceValue faceValue = Util.ConvertServerIntToFaceValue(cardValue);

        // 发送出牌协议到服务器
        if (mj_procedure != null)
        {
            mj_procedure.Send_MJOptionRq(MJOption.Discard, cardValue);

            // 发送后立即禁用打牌交互,等待服务器响应
            Disable2DPlayCard();

            // 修复：打完牌后立即隐藏操作面板
            Hide2DOperationButtons();
        }
        else
        {
            GF.LogError($"[2D视图] mj_procedure 为空，无法发送出牌协议");
        }
    }

    /// <summary>
    /// 检查2D模式下是否可以出牌
    /// </summary>
    public bool Can2DPlayCard()
    {
        // 检查是否轮到自己且状态允许出牌
        if (baseMahjongGameManager == null)
        {
            GF.LogError("[2D视图] baseMahjongGameManager 为空,无法出牌");
            return false;
        }
        return baseMahjongGameManager.CurrentTurnSeat == 0 && canPlayCard2D;
    }

    /// <summary>
    /// 禁用2D出牌交互
    /// </summary>
    private void Disable2DPlayCard()
    {
        canPlayCard2D = false;
    }

    /// <summary>
    /// 启用2D出牌交互（禁牌提示后恢复）
    /// </summary>
    private void Enable2DPlayCard()
    {
        canPlayCard2D = true;
        // 重新启用手牌交互
        Update2DHandCardInteraction(0);
    }

    /// <summary>
    /// 设置当前轮到哪个座位操作
    /// </summary>
    public void Set2DTurnSeat(int seatIdx)
    {
        if (baseMahjongGameManager == null)
        {
            GF.LogError($"[2D视图] baseMahjongGameManager 为空,无法设置当前操作座位");
            return;
        }

        baseMahjongGameManager.CurrentTurnSeat = seatIdx;
        // 更新指针旋转（如果没有操作玩家则回到0度位置）
        if (center2DPointer != null)
        {
            if (seatIdx >= 0 && seatIdx < 4)
            {
                // 根据座位旋转指针：座位0=0度, 座位1=90度, 座位2=180度, 座位3=270度
                Rotate2DPointer(seatIdx);
            }
            else
            {
                // 没有操作玩家时，指针逆时针回到0度位置
                Rotate2DPointer(0);
            }
        }
        // 更新倒计时：获取当前倒计时并启动倒计时协程
        Start2DCountdown(baseMahjongGameManager.GetCurrentCountdownSeconds());
    }

    /// <summary>
    /// 获取2D玩家选择的牌类型
    /// </summary>
    public HandPaiType GetSelect2DPaiType()
    {
        return select2DPaiType;
    }

    /// <summary>
    /// 获取2D玩家选择的牌索引
    /// </summary>
    public int GetSelect2DPaiHandPaiIdx()
    {
        return select2DPaiHandPaiIdx;
    }

    /// <summary>
    /// 清除2D玩家选择信息（服务器响应后调用）
    /// </summary>
    public void ClearSelect2DPaiInfo()
    {
        select2DPaiType = HandPaiType.HandPai;
        select2DPaiHandPaiIdx = -1;
    }

    /// <summary>
    /// 2D模式下处理碰牌
    /// </summary>
    public void Handle2DPengPai(int seatIdx, int[] cardValues, int targetSeatIdx)
    {
        var seat = GetSeat(seatIdx);

        seat.RemoveCardsForPengChiGang(PengChiGangPaiType.PENG, cardValues);
        seat.ShowEffect("Peng", 1.5f, PengChiGangPaiType.PENG);

        // 数据同步2：移除目标座位的最后一张弃牌（被碰走的牌）
        var targetSeat = GetSeat(targetSeatIdx);
        if (targetSeat != null && targetSeat.DiscardContainer != null && targetSeat.DiscardContainer.childCount > 0)
        {
            // 使用座位的GetNewestDiscardIndex方法，考虑不同座位的倒序情况
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

        // 隐藏弃牌标记（弃牌已被碰走）
        HideDiscardFlag();

        seat.AddMeldGroup(new List<int>(cardValues));

        if (IsXianTaoRule)
        {
            long playerId = seat.deskPlayer?.BasePlayer?.PlayerId ?? 0;
            if (playerId != 0)
            {
                int playerCount = GetRealPlayerCount();
                int lockingSeatIdx = GetLockingSeatIdx(seatIdx, playerCount);
                var lockingSeat = GetSeat(lockingSeatIdx);
                int lockCardThreshold = mj_procedure?.enterMJDeskRs?.BaseInfo?.MjConfig?.Xthh?.LockCard ?? 0;
                if (lockingSeat != null && lockCardThreshold > 1 && lockingSeat.laiziDiscardCount >= lockCardThreshold)
                {
                    openDoublePlayers.Add(playerId);
                    if (IsSeatLocalPlayer(seatIdx))
                    {
                        isLocalLockedBySuoPai = false;
                        suoPaiTips.gameObject.SetActive(false);
                    }
                }
            }
            RefreshAllBaoStatus();
        }

        // 整理手牌
        seat.ArrangeHandCards();

        // 碰牌后将一张手牌移到摸牌区（视觉提示轮到该玩家操作）
        seat.EnsureMoPaiSlot();

        // 如果是本地玩家碰牌
        if (IsSeatLocalPlayer(seatIdx))
        {
            // 血流已胡牌则跳过听牌计算
            if (IsXueLiuRule && seat != null && seat.IsXueLiuHu)
            {
                GF.LogInfo("[血流] 已胡牌，跳过听牌标记计算");
            }
            else
            {
                CheckAndShowTingMarks();
            }
            
            // 血流麻将检查花猪提示（碰牌后组牌区变化）
            if (IsXueLiuRule)
            {
                CheckAndShowHuaZhuTip();
            }
        }
    }

    /// <summary>
    /// 2D模式下处理杠牌
    /// </summary>
    /// <param name="seatIdx">杠牌的玩家座位</param>
    /// <param name="cardValues">杠牌的牌值数组</param>
    /// <param name="gangType">杠牌类型</param>
    /// <param name="formSeatIdx">打出牌的玩家座位（明杠/朝天杠时有效，-1表示无效）</param>
    public void Handle2DGangPai(int seatIdx, int[] cardValues, PengChiGangPaiType gangType, int formSeatIdx = -1)
    {
        var seat = GetSeat(seatIdx);

        // UI显示层：智能移除手牌和摸牌
        seat.RemoveCardsForPengChiGang(gangType, cardValues);
        seat.ShowEffect("Gang", 1.5f, gangType);

        // 数据同步2：对于明杠和朝天杠，需要移除别人打出的牌
        if (gangType == PengChiGangPaiType.GANG || gangType == PengChiGangPaiType.CHAO_TIAN_GANG)
        {
            // 明杠/朝天杠需要移除打出牌的座位的最后一张弃牌
            if (formSeatIdx >= 0 && formSeatIdx < 4)
            {
                var targetSeat = GetSeat(formSeatIdx);
                if (targetSeat != null && targetSeat.DiscardContainer != null && targetSeat.DiscardContainer.childCount > 0)
                {
                    // 使用座位的GetNewestDiscardIndex方法，考虑不同座位的倒序情况
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

            // 隐藏弃牌标记（弃牌已被杠走）
            HideDiscardFlag();
        }

        seat.AddMeldGroup(gangType, new List<int>(cardValues));

        if (IsXianTaoRule)
        {
            // 如果是接杠（别人打的牌我杠）
            if (formSeatIdx != -1)
            {
                long playerId = seat.deskPlayer?.BasePlayer?.PlayerId ?? 0;
                if (playerId != 0)
                {
                    int playerCount = GetRealPlayerCount();
                    int lockingSeatIdx = GetLockingSeatIdx(seatIdx, playerCount);
                    var lockingSeat = GetSeat(lockingSeatIdx);
                    int lockCardThreshold = mj_procedure?.enterMJDeskRs?.BaseInfo?.MjConfig?.Xthh?.LockCard ?? 0;
                    if (lockingSeat != null && lockCardThreshold > 1 && lockingSeat.laiziDiscardCount >= lockCardThreshold)
                    {
                        openDoublePlayers.Add(playerId);
                        if (IsSeatLocalPlayer(seatIdx))
                        {
                            isLocalLockedBySuoPai = false;
                            suoPaiTips.gameObject.SetActive(false);
                        }
                    }
                }
            }
            RefreshAllBaoStatus();
        }

        // 整理手牌
        seat.ArrangeHandCards();

        // 朝天杠后如果没有摸牌，需要移动最边上的手牌到摸牌区
        // 注意：朝天杠和朝天暗杠都是3张牌，需要确保显示正确
        if (gangType == PengChiGangPaiType.CHAO_TIAN_GANG || gangType == PengChiGangPaiType.CHAO_TIAN_AN_GANG)
        {
            // 确保朝天杠只显示3张牌
            seat.EnsureMoPaiSlot();
        }
        // 如果是本地玩家杠牌
        if (IsSeatLocalPlayer(seatIdx))
        {
            // 血流已胡牌则跳过听牌计算
            if (IsXueLiuRule && seat != null && seat.IsXueLiuHu)
            {
                // 跳过听牌标记计算
            }
            else
            {
                CheckAndShowTingMarks();
            }
            
            // 血流麻将检查花猪提示（杠牌后组牌区变化）
            if (IsXueLiuRule)
            {
                CheckAndShowHuaZhuTip();
            }
        }
    }

    /// <summary>
    /// 2D模式下处理吃牌
    /// </summary>
    public void Handle2DChiPai(int seatIdx, int[] cardValues, int targetSeatIdx)
    {
        var seat = GetSeat(seatIdx);

        // 获取上家打出的牌值（用于判断左吃、中吃、右吃）
        int lastDaPaiValue = GetLastDiscardedCardValue(targetSeatIdx);

        // UI显示层：移除手牌
        seat.RemoveCardsForPengChiGang(PengChiGangPaiType.CHI, cardValues, lastDaPaiValue);
        seat.ShowEffect("Chi", 1.5f, PengChiGangPaiType.CHI);

        var targetSeat = GetSeat(targetSeatIdx);
        if (targetSeat != null && targetSeat.DiscardContainer != null && targetSeat.DiscardContainer.childCount > 0)
        {
            // 使用座位的GetNewestDiscardIndex方法，考虑不同座位的倒序情况
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

        // 隐藏弃牌标记（弃牌已被吃走）
        HideDiscardFlag();

        seat.AddMeldGroup(new List<int>(cardValues));

        // 整理手牌
        seat.ArrangeHandCards();

        // 吃牌后将一张手牌移到摸牌区（视觉提示轮到该玩家操作）
        seat.EnsureMoPaiSlot();

        // 如果是本地玩家吃牌
        if (IsSeatLocalPlayer(seatIdx))
        {
            // 血流已胡牌则跳过听牌计算
            if (IsXueLiuRule && seat != null && seat.IsXueLiuHu)
            {
                GF.LogInfo("[血流] 已胡牌，跳过听牌标记计算");
            }
            else
            {
                CheckAndShowTingMarks();
            }
            
            // 血流麻将检查花猪提示（吃牌后组牌区变化）
            if (IsXueLiuRule)
            {
                CheckAndShowHuaZhuTip();
            }
        }
    }

    /// <summary>
    /// 获取指定座位最后打出的牌值
    /// </summary>
    private int GetLastDiscardedCardValue(int seatIdx)
    {
        var seat = GetSeat(seatIdx);
        if (seat == null || seat.DiscardContainer == null || seat.DiscardContainer.childCount == 0)
            return -1;

        // 使用座位的GetNewestDiscardIndex方法，考虑不同座位的倒序情况
        int lastIndex = seat.GetNewestDiscardIndex();
        if (lastIndex < 0)
            return -1;

        GameObject lastCard = seat.DiscardContainer.GetChild(lastIndex).gameObject;
        MjCard_2D mjCard = lastCard.GetComponent<MjCard_2D>();

        return mjCard != null ? mjCard.cardValue : -1;
    }

    /// <summary>
    /// 判断赖子是否被用作代替其他牌值
    /// 黑摸判定：需要检查赖子当作原始值时整副牌是否能胡牌
    /// 例如：手牌一万二万三万四万五万，五万是赖子
    /// - 如果五万当作本身能胡牌 → 黑摸（赖子没有代替其他牌）
    /// - 如果五万当作本身不能胡牌，必须用赖子万能属性才能胡 → 软摸（赖子代替了其他牌）
    /// </summary>
    /// <param name="handCards">手牌列表</param>
    /// <param name="melds">牌组列表（吃碰杠）</param>
    /// <returns>true表示赖子被用作代替牌（软摸），false表示赖子只作为本身牌值使用（黑摸）或没有赖子</returns>
    private bool IsLaiziUsedAsSubstitute(Google.Protobuf.Collections.RepeatedField<int> handCards)
    {
        if (baseMahjongGameManager == null || baseMahjongGameManager.mjHuTingCheck == null)
            return false;

        MahjongFaceValue laiziValue = baseMahjongGameManager.CurrentLaiZi;
        if (laiziValue == MahjongFaceValue.MJ_UNKNOWN)
            return false; // 没有赖子，返回false

        int laiziServerValue = Util.ConvertFaceValueToServerInt(laiziValue);

        // 统计手牌中每种牌的数量（包括赖子）
        Dictionary<int, int> cardCount = new Dictionary<int, int>();
        if (handCards != null)
        {
            foreach (var card in handCards)
            {
                if (!cardCount.ContainsKey(card))
                    cardCount[card] = 0;
                cardCount[card]++;
            }
        }

        // 检查手牌中是否有赖子
        if (!cardCount.ContainsKey(laiziServerValue))
        {
            return false; // 没有赖子
        }

        // 手牌中有赖子，需要判断赖子当作原始值时是否能胡牌
        // 将手牌转换为byte数组格式（MahjongHuTingCheck需要的格式）
        byte[] cardArray = new byte[34]; // 34种牌：万9+筒9+条9+字7

        foreach (var kvp in cardCount)
        {
            MahjongFaceValue faceValue = Util.ConvertServerIntToFaceValue(kvp.Key);
            int idx = (int)faceValue; // MahjongFaceValue枚举本身就是索引
            if (idx >= 0 && idx < 34)
            {
                cardArray[idx] = (byte)kvp.Value;
            }
        }

        // 获取赖子索引
        byte laiziIndex = (byte)((int)laiziValue);

        // 检测1：赖子当作普通牌时能否胡牌（不使用万能属性）
        // 传入255表示没有赖子，所有牌都按原始值计算
        bool canHuWithLaiziAsNormal = baseMahjongGameManager.mjHuTingCheck.CheckCanHu(cardArray, 255);

        if (canHuWithLaiziAsNormal)
        {
            // 赖子当作原始值能胡牌 → 黑摸（赖子没有代替其他牌）
            return false;
        }
        else
        {
            // 赖子当作原始值不能胡牌，必须用万能属性 → 软摸（赖子代替了其他牌）
            return true;
        }
    }

    /// <summary>
    /// 2D模式下处理胡牌
    /// </summary>
    public void Handle2DHuPai(Msg_Hu ack)
    {
        mj_procedure.SetBrecord(ack.Over);
        // 实现: 2D胡牌逻辑
        var winSeat = GetSeat(GetClientSeatFromServerPos((Position)ack.WinPos));

        // 获取赖子牌值（直接访问数据属性）
        int laiziValue = 0;
        if (baseMahjongGameManager != null)
        {
            MahjongFaceValue laiziCard = baseMahjongGameManager.CurrentLaiZi;
            if (laiziCard != MahjongFaceValue.MJ_UNKNOWN)
            {
                laiziValue = Util.ConvertFaceValueToServerInt(laiziCard);
            }
        }

        // 获取玩家列表
        List<DeskPlayer> players = baseMahjongGameManager?.GetAliveDeskPlayers();

        // 判断胡牌方式（1=自摸，3=杠开）
        bool isZimoOrGangKai = (ack.WinWay == 1 || ack.WinWay == 3);

        // 显示所有玩家的手牌（使用胡牌展示效果）
        // 【优化1】使用Sequence确保所有座位同时开始显示，避免延迟
        DOTween.Kill(gameObject);
        var sequence = DOTween.Sequence();
        sequence.SetTarget(gameObject);

        if (ack.OtherCard != null)
        {
            foreach (var otherCard in ack.OtherCard)
            {
                int clientSeat = GetClientSeatFromServerPos((Position)otherCard.Pos);

                var seat = GetSeat(clientSeat);
                if (seat != null && otherCard.Cards != null && otherCard.Cards.Count > 0)
                {
                    // 获取积分变化
                    var scoreItem = ack.HuScoreChange?.FirstOrDefault(s => s.Key == otherCard.Player);
                    double scoreChange = scoreItem?.Val ?? 0;

                    // 判断是否是赢家
                    bool isWinner = (otherCard.Player == ack.WinPlayer);

                    // 【优化2】完全根据Msg_Hu数据重新生成：先清空组牌区，再重建
                    // 【优化3】如果是自摸或杠开，且是赢家，单独显示最后一张摸牌
                    bool separateLastCard = isZimoOrGangKai && isWinner;

                    // 准备手牌数据
                    List<int> handCards = new List<int>(otherCard.Cards);

                    // 如果需要单独显示最后一张牌，先将其从手牌中移除
                    int lastMoCard = 0;
                    if (separateLastCard && handCards.Count > 0)
                    {
                        lastMoCard = handCards[handCards.Count - 1];
                        handCards.RemoveAt(handCards.Count - 1);
                    }

                    // 排序手牌（不包括最后摸的牌）
                    handCards = baseMahjongGameManager.SortHandCards(handCards);

                    // 同时执行倒牌和显示分数（无延迟）
                    sequence.AppendCallback(() =>
                    {
                        // 使用新的胡牌展示方法（完全重建组牌区）
                        seat.ShowWinHandCardsWithMelds(otherCard.Melds, handCards, separateLastCard ? lastMoCard : 0);

                        // 播放得分效果
                        seat.ShowScoreChange(scoreChange);

                        if (scoreChange > 0)
                            Sound.PlayEffect(AudioKeys.MJ_EFFECT_AUDIO_MJ_SCORE);
                    });
                }
            }
        }

        // 判断是否为黑摸：只有仙桃晃晃玩法才判断黑摸，其他玩法自摸就是自摸
        if (IsXianTaoRule && ack.OtherCard != null && ack.OtherCard.Count > 0)
        {
            // 播放胡牌特效
            bool isHeiMo = false;
            //播放黑摸软摸接冲特效
            string effectType = "RuanMo";
            var winPlayerCard = ack.OtherCard.FirstOrDefault(c => c.Player == ack.WinPlayer);
            switch (ack.WinWay)
            {
                case 1:
                    if (winPlayerCard.Cards != null && winPlayerCard.Cards.Count > 0)
                    {
                        // 黑摸：赖子没有被用作代替牌
                        isHeiMo = !IsLaiziUsedAsSubstitute(winPlayerCard.Cards);
                    }
                    effectType = isHeiMo ? "HeiMo" : "RuanMo";
                    break;
                case 2:
                    effectType = "ZhuoChong";
                    break;
                case 3:
                    // 杠开：仅在配置允许时显示，否则按自摸黑摸/软摸处理
                    int gangKaiConfig = mj_procedure?.enterMJDeskRs?.BaseInfo?.MjConfig?.Xthh?.GangKai ?? 1;
                    if (gangKaiConfig == 1)
                    {
                        effectType = "GangKai";
                    }
                    else
                    {
                        if (winPlayerCard.Cards != null && winPlayerCard.Cards.Count > 0)
                        {
                            isHeiMo = !IsLaiziUsedAsSubstitute(winPlayerCard.Cards);
                        }
                        effectType = isHeiMo ? "HeiMo" : "RuanMo";
                    }
                    break;
                default:
                    break;
            }
            sequence.AppendCallback(() =>
            {
                winSeat.ShowEffect(effectType, 1.5f);
            });
        }

        // 卡五星玩法：显示胡牌类型特效（从fan字段获取）
        // 逻辑统一：无番型播放基础胡牌特效（屁胡），有番型依次播放番型特效（不播放基础特效）
        if (IsKaWuXingRule || IsXueLiuRule)
        {
            // 打印服务器返回的番型信息（便于调试）
            if (ack.Fan != null && ack.Fan.Count > 0)
            {
                foreach (var fanCode in ack.Fan)
                {
                    string fanName = GameUtil.GetFanTypeName(fanCode);
                }
            }

            // 收集卡五星特有番型的特效名称
            List<string> huTypeEffects = GetKWXHuTypeEffectsFromFan(ack.Fan);

            // 根据番型决定特效播放方式
            if (huTypeEffects.Count == 0)
            {
                // 无番型：播放基础胡牌特效（屁胡）
                string baseEffectType = isZimoOrGangKai ? "ZiMo" : "Hu";
                sequence.AppendCallback(() =>
                {
                    winSeat.ShowEffect(baseEffectType, 1.5f);
                });
            }
            else
            {
                // 有番型：依次播放番型特效（不播放基础胡牌特效）
                foreach (var huTypeEffect in huTypeEffects)
                {
                    sequence.AppendInterval(0.8f); // 稍微延迟，等上一个特效显示
                    var currentEffect = huTypeEffect; // 捕获变量
                    sequence.AppendCallback(() =>
                    {
                        winSeat.ShowEffect(currentEffect, 1.5f);

                        // 播放对应的牌型语音（根据赢家性别与玩法选择普通话/方言）
                        long winPlayerId = ack.WinPlayer;
                        // 使用 Rule 层获取玩法类型
                        var mjMethod = currentRule?.GetMJMethod();
                        MahjongAudio.PlayHuPaiTypeForPlayer(currentEffect, winPlayerId, mjMethod);
                    });
                }
            }
        }

        // 如果有码牌值，播放亮码牌动画
        if (IsKaWuXingRule && ack.Horse > 0)
        {
            sequence.AppendInterval(0.5f); // 稍微延迟，等胡牌特效显示
            sequence.AppendCallback(() =>
            {
                ShowLiangMaPai(ack.Horse);
            });
        }

        // 延迟显示结算面板（等待亮牌动画，延迟5秒）
        sequence
            .AppendInterval(5f)
            .AppendCallback(() =>
            {
                // 隐藏亮码牌
                HideLiangMaPai();

                if (IsSpectator())
                {
                    // 旁观者：直接清理UI，准备下一局
                    GF.LogInfo_gsc("[胡牌流程]", "旁观者不显示结算面板，直接清理UI");
                    ResetGameUI();
                }
                else
                {
                    // 座位上的玩家：显示结算面板
                    ShowGameEndPanel(ack, players, laiziValue);
                }
            });
    }

    /// <summary>
    /// 2D模式下处理流局（通过Msg_Hu.liuJu判断）
    /// </summary>
    public void Handle2DLoseGame(Msg_Hu ack)
    {
        // 实现: 2D流局逻辑
        mj_procedure.SetBrecord(ack.Over);

        // 获取玩家列表
        List<DeskPlayer> players = baseMahjongGameManager?.GetAliveDeskPlayers();

        // 显示所有玩家的手牌（使用同步显示方式）
        DOTween.Kill(gameObject);
        var sequence = DOTween.Sequence();
        sequence.SetTarget(gameObject);

        // 仙桃晃晃流局时，需要将每个玩家的最后一张牌单独显示
        bool showLastCardSeparately = IsXianTaoRule;

        if (ack.OtherCard != null)
        {
            foreach (var otherCard in ack.OtherCard)
            {
                int clientSeat = GetClientSeatFromServerPos((Position)otherCard.Pos);
                var seat = GetSeat(clientSeat);
                if (seat != null && otherCard.Cards != null && otherCard.Cards.Count > 0)
                {
                    // 准备手牌数据
                    List<int> handCards = new List<int>(otherCard.Cards);

                    // 仙桃晃晃流局时，保存最后一张牌单独显示
                    int lastCard = 0;
                    if (showLastCardSeparately && handCards.Count > 0)
                    {
                        lastCard = handCards[handCards.Count - 1];
                        handCards.RemoveAt(handCards.Count - 1);
                    }

                    // 排序手牌（如果单独显示最后一张，则不包括最后一张）
                    handCards = baseMahjongGameManager.SortHandCards(handCards);

                    // 同时执行倒牌和显示分数（流局也显示服务器返回分数）
                    sequence.AppendCallback(() =>
                    {
                        // 使用胡牌展示方法（在MeldContainer中紧密显示，仙桃晃晃流局时最后一张牌单独显示）
                        seat.ShowWinHandCardsWithMelds(otherCard.Melds, handCards, lastCard);

                        var scoreItem = ack.ScoreChange?.FirstOrDefault(s => s.Key == otherCard.Player);
                        double scoreChange = scoreItem?.Val ?? 0;
                        seat.ShowScoreChange(scoreChange);
                    });
                }
            }
        }

        // 延迟显示结算面板（等待亮牌动画，延迟5秒）
        sequence
            .AppendInterval(5f)
            .AppendCallback(() =>
            {
                if (players != null)
                {
                    // 判断当前玩家是否在座位上，旁观者直接清理UI不显示结算面板
                    if (IsSpectator())
                    {
                        // 旁观者：直接清理UI，准备下一局
                        GF.LogInfo_gsc("[流局流程]", "旁观者不显示结算面板，直接清理UI");
                        ResetGameUI();
                    }
                    else
                    {
                        // 座位上的玩家：直接使用服务器下发的Msg_Hu显示流局结算
                        ShowGameEndPanel(ack, players, 0);
                    }
                }
                else
                {
                    GF.LogError($"[2D视图] 无法获取玩家列表，无法显示流局结算面板");
                }
            });
    }

    #endregion

    #region 2D房间信息更新

    /// <summary>
    /// 2D模式下处理游戏开始（发牌动画）
    /// </summary>
    public async void Handle2DGameStart(PBMJStart gameStartData)
    {

        // 空引用检查
        if (baseMahjongGameManager == null)
        {
            GF.LogError("[2D游戏] baseMahjongGameManager 为空,无法开始游戏");
            return;
        }

        if (gameStartData == null)
        {
            GF.LogError("[2D游戏] gameStartData 为空,无法开始游戏");
            return;
        }

        // 初始化准备工作
        await InitializeGameStart();

        // 获取庄家座位
        int bankerSeat = baseMahjongGameManager.CurrentTurnSeat;
        var bankerSeatObj = GetSeat(bankerSeat);
        if (bankerSeatObj != null)
        {
            bankerSeatObj.ShowBankerIcon(true, true);
        }
        else
        {
            GF.LogError($"[2D游戏] 无法获取庄家座位对象,座位索引:{bankerSeat}");
        }

        // 显示赖子牌（如果是赖子模式）
        await ShowLaiZiCardsIfNeeded(gameStartData);

        // 播放骰子动画（发牌前需要换三张）
        // 使用 Rule 层统一的 HasChangeCard 属性判断
        if (HasChangeCard)
        {
            await PlayTouZiAnimation();
        }

        // 播放发牌音效
        Sound.PlayEffect(AudioKeys.MJ_EFFECT_AUDIO_FAPAI);

        // 执行发牌动画
        await PlayDealCardsAnimation(gameStartData);

        // 延迟后显示庄家多摸的牌并启动倒计时
        await FinalizeGameStart(gameStartData, bankerSeat);
    }

    /// <summary>
    /// 初始化游戏开始的准备工作
    /// </summary>
    private async Cysharp.Threading.Tasks.UniTask InitializeGameStart()
    {
        // 清空2D状态
        canPlayCard2D = false;
        select2DPaiType = HandPaiType.HandPai;
        select2DPaiHandPaiIdx = -1;
        ClearReadyState();
        ClearSuoPaiState();

        // 更新房间信息（局数、余牌等）
        Update2DRoomInfo();

        await Cysharp.Threading.Tasks.UniTask.Yield();
    }

    /// <summary>
    /// 如果是赖子模式，显示赖子牌并等待动画完成
    /// </summary>
    private async Cysharp.Threading.Tasks.UniTask ShowLaiZiCardsIfNeeded(PBMJStart gameStartData)
    {
        if (currentRule == null || gameStartData == null) return;

        // 如果是赖子模式，等待赖子牌显示动画完成
        if (currentRule.HasLaiZi())
        {
            // 从游戏开始数据获取赖子牌值
            MahjongFaceValue laiziCard = MahjongFaceValue.MJ_UNKNOWN;
            if (gameStartData.Laizi > 0)
            {
                laiziCard = Util.ConvertServerIntToFaceValue(gameStartData.Laizi);
            }

            // 从游戏开始数据获取飘癞子牌值
            MahjongFaceValue piaoLaiziCard = MahjongFaceValue.MJ_UNKNOWN;
            if (gameStartData.Piao > 0)
            {
                piaoLaiziCard = Util.ConvertServerIntToFaceValue(gameStartData.Piao);
            }

            // 如果有赖子或飘癞子，显示具体的赖子牌
            if (laiziCard != MahjongFaceValue.MJ_UNKNOWN || piaoLaiziCard != MahjongFaceValue.MJ_UNKNOWN)
            {
                ShowLaiZiCards(piaoLaiziCard, laiziCard);

                // 设置赖子和飘赖
                baseMahjongGameManager.SetLaiZiCard(laiziCard);
                baseMahjongGameManager.SetPiaoLaiZi(piaoLaiziCard);

                GF.LogInfo_gsc($"[2D游戏] 赖子模式 - 显示赖子牌 飘癞子:{piaoLaiziCard}({gameStartData.Piao}), 赖子:{laiziCard}({gameStartData.Laizi})");

                await Cysharp.Threading.Tasks.UniTask.Delay(100);
            }
        }
    }

    /// <summary>
    /// 执行发牌动画（4张4张无序发牌 -> MeldContainer背牌展示 -> 排序后手牌展示）
    /// </summary>
    private async Cysharp.Threading.Tasks.UniTask PlayDealCardsAnimation(PBMJStart gameStartData)
    {
        const int cardsPerRound = 4;
        const int totalRounds = 3;
        const int dealDelayMs = 100;
        const int showBackCardsDelayMs = 300; // 在MeldContainer显示背牌0.3秒
        const int transitionDelayMs = 300;

        // 获取本地玩家的手牌数据（未排序）
        List<int> myHandCards = gameStartData.HandCard != null ? new List<int>(gameStartData.HandCard) : new List<int>();

        // 第一阶段：4张4张无序发牌（共3轮，每轮每家4张）
        for (int round = 0; round < totalRounds; round++)
        {
            for (int seatIdx = 0; seatIdx < 4; seatIdx++)
            {
                var seat = GetSeat(seatIdx);
                if (seat == null) continue;

                // 计算当前应该显示的牌数
                int currentCardCount = (round + 1) * cardsPerRound;

                if (IsSeatLocalPlayer(seatIdx))
                {
                    // 本地玩家：取前currentCardCount张牌显示（无序）
                    List<int> cardsToShow = myHandCards.Take(currentCardCount).ToList();

                    // 在手牌容器显示
                    seat.CreateHandCards(cardsToShow);
                }
                else
                {
                    // 其他座位：显示背牌
                    seat.ShowBackCardsInHandContainer(currentCardCount);
                }

                await Cysharp.Threading.Tasks.UniTask.Delay(dealDelayMs);
            }
        }

        // 第二阶段：在MeldContainer显示背牌（13张，持续1秒）
        await Cysharp.Threading.Tasks.UniTask.Delay(transitionDelayMs);

        for (int seatIdx = 0; seatIdx < 4; seatIdx++)
        {
            var seat = GetSeat(seatIdx);
            if (seat != null)
            {
                // 清空手牌容器
                seat.ClearContainer(seat.HandContainer);

                // 在MeldContainer显示13张背牌
                seat.ShowBackCardsInMeldContainer(13);
            }
        }

        await Cysharp.Threading.Tasks.UniTask.Delay(showBackCardsDelayMs);

        // 第三阶段：清空MeldContainer，在手牌容器显示排序后的手牌
        for (int seatIdx = 0; seatIdx < 4; seatIdx++)
        {
            var seat = GetSeat(seatIdx);
            if (seat == null)
            {
                GF.LogError($"[2D游戏] 座位{seatIdx}对象为空,跳过");
                continue;
            }

            // 清空MeldContainer
            seat.ClearContainer(seat.MeldContainer);
            if (IsSeatLocalPlayer(seatIdx))
            {
                // 本地玩家：显示排序后的13张牌（不包括第14张）
                // 前13张手牌排序
                List<int> first13Cards = myHandCards.Take(13).ToList();
                first13Cards.Sort();
                // 刷新手牌显示（只显示排序后的前13张）
                seat.CreateHandCards(first13Cards);
            }
            else
            {
                // 其他座位：显示13张背牌
                List<int> backCards = new List<int>();
                for (int i = 0; i < 13; i++)
                {
                    backCards.Add(0); // 0 表示背面牌
                }
                seat.CreateHandCards(backCards);
            }
        }

        await Cysharp.Threading.Tasks.UniTask.Delay(transitionDelayMs);
    }

    /// <summary>
    /// 完成游戏开始的最后步骤（显示庄家多摸的牌、启动倒计时）
    /// </summary>
    private async Cysharp.Threading.Tasks.UniTask FinalizeGameStart(PBMJStart gameStartData, int dealerSeat)
    {
        ShowDealerExtraCard(gameStartData, dealerSeat);

        // 使用 Rule 层统一的属性判断换牌/甩牌
        if (HasChangeCard)
        {
            // 发牌完成后：显示换牌动画面板 + 选牌面板 + 选牌中状态
            ShowKWXChangeCardUI();
            return;
        }

        // 血流模式：没有换三张但有甩牌，则进入甩牌阶段
        if (IsXueLiuRule && HasThrowCard)
        {
            ShowThrowCardUI();
            return;
        }

        // 其他模式：直接设置庄家轮次和操作
        // 更新轮次到庄家
        Set2DTurnSeat(dealerSeat);

        // 启动10秒倒计时
        const int BANKER_PLAY_COUNTDOWN = 10;
        Start2DCountdown(BANKER_PLAY_COUNTDOWN);

        // 恢复操作选项
        current2DOptions = gameStartData.OperationList.Select(op => (MJOption)op).ToList();
        Show2DOperationButtons(current2DOptions);

        // 如果自己是庄家，允许出牌
        if (dealerSeat == 0)
        {
            canPlayCard2D = true;
            CheckAndShowTingMarks();
        }
        else
        {
            CheckHuPaiBtnVisibility();
        }
    }

    /// <summary>
    /// 从游戏开始数据获取庄家座位
    /// </summary>
    private int GetClientSeatFromServerPos(Position serverPos)
    {
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        int selfServerSeatIndex = Util.GetPosByServerPos(baseMahjongGameManager.GetSeatPositionById(myPlayerId), GetRealPlayerCount());

        int realPlayerCount = GetRealPlayerCount();
        return Util.TransformSeatS2C(
            Util.GetPosByServerPos(serverPos, realPlayerCount),
            selfServerSeatIndex,
            realPlayerCount
        );
    }

    /// <summary>
    /// 显示庄家多摸的一张牌
    /// </summary>
    private void ShowDealerExtraCard(PBMJStart gameStartData, int dealerSeat)
    {
        var dealerSeatObj = GetSeat(dealerSeat);
        if (dealerSeatObj == null) return;

        if (dealerSeat == 0)
        {
            // 显示摸牌（原始第14张）
            dealerSeatObj.AddMoPai(gameStartData.HandCard[13]);
        }
        else
        {
            // 其他人是庄家，在摸牌区显示一张背牌（生成新牌而不是移动手牌）
            dealerSeatObj.AddMoPai(0);  // 0 表示背面牌
        }

        // 实时更新剩余牌数（庄家多摸一张后牌堆减少）
        Update2DRoomInfo();
    }

    #endregion

    #region 2D重连数据恢复

    /// <summary>
    /// 2D断线重连恢复 - 在游戏进行中重连时调用
    /// 使用服务器下发的重连数据（enterData）完整还原游戏状态
    /// </summary>
    public void Restore2DFromReconnectData(Msg_EnterMJDeskRs enterData)
    {

        // 更新2D房间信息（余牌、倒计时等）
        Update2DRoomInfo();

        // 恢复最后打出的牌
        lastDiscard = enterData.LastDiscard == null ? MahjongFaceValue.MJ_UNKNOWN : Util.ConvertServerIntToFaceValue(enterData.LastDiscard.Val);
        if (lastDiscard != MahjongFaceValue.MJ_UNKNOWN)
        {
            long lastDiscardPlayerId = enterData.LastDiscard.Key;
            int lastDiscardSeatIdx = baseMahjongGameManager.GetSeatIndexByPlayerId(lastDiscardPlayerId);
            MahjongFaceValue lastCard = Util.ConvertServerIntToFaceValue(enterData.LastDiscard.Val);
            ShowDiscardFlag(lastDiscardSeatIdx, lastCard);
        }

        // 恢复当前轮到操作的玩家
        Set2DTurnSeat(baseMahjongGameManager.CurrentTurnSeat);

        // 恢复操作选项
        current2DOptions = enterData.OperationList.Select(op => (MJOption)op).ToList();

        // 显示操作按钮（如果有可用操作）
        // 这会自动恢复打牌交互状态
        Show2DOperationButtons(current2DOptions);

        // 同步解锁翻倍玩家（openDoublePlayer）
        openDoublePlayers.Clear();
        if (enterData.OpenDoublePlayer != null)
        {
            foreach (var pid in enterData.OpenDoublePlayer)
            {
                openDoublePlayers.Add(pid);
            }
        }

        long myId = GetSeat(0)?.deskPlayer?.BasePlayer?.PlayerId ?? 0;
        
        // 检查本地玩家是否已解锁
        bool isMyIdInOpenDouble = myId != 0 && openDoublePlayers.Contains(myId);
        if (isMyIdInOpenDouble)
        {
            // 本地已解锁
            isLocalLockedBySuoPai = false;
            if (suoPaiTips != null)
            {
                suoPaiTips.gameObject.SetActive(false);
            }
        }
        else
        {
            // 本地未解锁，检查下家赖子弃牌数是否达到锁牌阈值
            CheckSuoPaiTipsOnReconnect();
        }

        if (baseMahjongGameManager.CurrentTurnSeat == 0)
        {
            // 如果轮到自己操作，检测听牌标记
            CheckAndShowTingMarks();
        }
        else
        {
            // 轮到其他玩家操作，检测胡牌按钮可见性
            CheckHuPaiBtnVisibility();
        }
    }

    /// <summary>
    /// 更新房间信息（合并显示：余牌/底分/局数/房号）
    /// 格式: "余牌: 23  底分: 0.5  还有: 7局  房号:501421"
    /// </summary>
    public void Update2DRoomInfo()
    {
        int remainCount = GetRemainCardCount();
        if (remainCount == 0 && mj_procedure.enterMJDeskRs != null)
        {
            remainCount = mj_procedure.enterMJDeskRs.CardNum;
        }

        // 获取底分
        float baseScore = 0;
        if (mj_procedure.enterMJDeskRs != null)
        {
            baseScore = mj_procedure.enterMJDeskRs.BaseInfo.BaseConfig.BaseCoin;
        }

        // 获取局数
        int currentRound = 0;
        int totalRounds = 0;
        if (mj_procedure.enterMJDeskRs != null)
        {
            currentRound = mj_procedure.enterMJDeskRs.GameNumbers;
            totalRounds = (int)mj_procedure.enterMJDeskRs.BaseInfo.BaseConfig.PlayerTime;
        }
        int remainRounds = totalRounds - currentRound;

        // 获取房号
        string roomId = "";
        if (mj_procedure.enterMJDeskRs != null)
        {
            roomId = mj_procedure.enterMJDeskRs.BaseInfo.DeskId.ToString();
        }

        // 合并显示
        center2DRoomInfoText.text = $"余牌: {remainCount}  底分: {baseScore}  还有: {remainRounds}局  房号:{roomId}";
        DeskNumText.text = $"局数: <color=DCDA95>{mj_procedure.enterMJDeskRs.GameNumbers}/{mj_procedure.enterMJDeskRs.BaseInfo.BaseConfig.PlayerTime}</color>";
    }


    /// <summary>
    /// 更新手牌交互状态
    /// 使用2D独立的状态管理
    /// </summary>
    private void Update2DHandCardInteraction(int currentTurnSeat)
    {
        var seat = GetSeat(0);
        if (currentTurnSeat != 0)
        {
            // 不是自己的回合，禁用交互
            seat.DisableHandCardInteraction();
            return;
        }
        if (Can2DPlayCard())
        {
            // 轮到玩家且允许打牌，启用交互
            seat.EnableHandCardInteraction();
        }
        else
        {
            // 其他情况禁用交互
            seat.DisableHandCardInteraction();
        }
    }

    /// <summary>
    /// 旋转2D指针到指定座位
    /// Unity Z轴顺时针旋转：0度=下, 90度=右, 180度=上, 270度=左
    /// 座位映射：座位0(下/自己)=0度, 座位1(左家)=270度, 座位2(上/对家)=180度, 座位3(右家)=90度
    /// </summary>
    private void Rotate2DPointer(int seatIdx)
    {
        if (center2DPointer == null || seatIdx < 0 || seatIdx >= 4) return;

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
    /// 更新2D倒计时显示
    /// </summary>
    /// <param name="seconds">剩余秒数</param>
    public void Update2DCountdown(int seconds = -1)
    {
        center2DCountdownText.text = seconds < 0 ? "" : seconds.ToString();
    }

    /// <summary>
    /// 启动倒计时读秒(重连时使用)
    /// </summary>
    /// <param name="startSeconds">起始秒数（如果<=0则使用默认10秒）</param>
    public void Start2DCountdown(int startSeconds)
    {
        // 停止之前的倒计时
        if (countdownCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(countdownCoroutine);
        }

        if (startSeconds < 0)
        {
            Update2DCountdown();
            return;
        }

        // 显示倒计时面板
        CountdownPanel2D.SetActive(true);

        // 启动新的倒计时
        countdownCoroutine = CoroutineRunner.Instance.StartCoroutine(CountdownCoroutine(startSeconds));
    }

    /// <summary>
    /// 倒计时协程
    /// </summary>
    private System.Collections.IEnumerator CountdownCoroutine(int startSeconds)
    {
        int remainSeconds = startSeconds;

        while (remainSeconds > 0)
        {
            Update2DCountdown(remainSeconds);
            yield return new WaitForSeconds(1f);
            remainSeconds--;
            // 在最后5秒播放倒计时音效
            if (baseMahjongGameManager.CurrentTurnSeat == 0 && remainSeconds <= 5 && remainSeconds > 0)
                Sound.PlayEffect(AudioKeys.MJ_EFFECT_AUDIO_GAME_TIME_TICK);
        }

        // 倒计时结束
        Update2DCountdown(0);
        countdownCoroutine = null;
    }

    #endregion

    #region 2D玩家信息更新

    /// <summary>
    /// 更新所有座位的玩家信息
    /// </summary>
    public void Update2DAllPlayerInfo()
    {
        for (int seatIdx = 0; seatIdx < 4; seatIdx++)
        {
            var seat = GetSeat(seatIdx);

            // 获取对应座位的玩家数据
            DeskPlayer player = baseMahjongGameManager.GetDeskPlayer(seatIdx);
            if (player != null)
            {
                FengWei windPos = GetSeatFengWei(seatIdx);
                seat.UpdatePlayerInfo(player, windPos);
            }
            else
            {
                seat.UpdatePlayerInfo(null, FengWei.EAST); // 清空信息
            }
        }
    }

    /// <summary>
    /// 更新指定座位的玩家信息
    /// </summary>
    public void Update2DPlayerInfo(int seatIdx, DeskPlayer player)
    {
        var seat = GetSeat(seatIdx);
        FengWei windPos = GetSeatFengWei(seatIdx);
        seat.UpdatePlayerInfo(player, windPos);
    }

    /// <summary>
    /// 更新指定座位的金币显示
    /// </summary>
    public void CoinChange(int seatIdx, double scoreChange)
    {
        var seat = GetSeat(seatIdx);
        seat.ShowScoreChangeSimple(scoreChange);
    }

    #endregion

    #region 2D视图清理

    /// <summary>
    /// 清理所有2D视图对象
    /// </summary>
    public void CleanupAll2DObjects()
    {
        HideDiscardFlag();

        // 隐藏倒计时面板
        CountdownPanel2D.SetActive(false);

        // 清理所有卡五星相关面板
        CleanupAllKWXPanels();

        // 清理所有血流相关面板
        CleanupAllXueLiuPanels();

        // 清理所有座位
        for (int i = 0; i < 4; i++)
        {
            seats2D[i]?.ClearGameGos();
        }

        // 隐藏操作按钮
        Hide2DOperationButtons();

        // 取消订阅麻将牌样式改变事件
        MahjongSettings.OnCardStyleChanged -= OnCardStyleChanged;
    }

    #endregion

    #region 2D聊天表情管理

    /// <summary>
    /// 显示指定座位的聊天表情
    /// </summary>
    public void ShowEmoji(int seatIdx, string emojiName)
    {
        var seat = GetSeat(seatIdx);
        seat.ShowEmoji(emojiName);
    }

    public void ShowChatText(int seatIdx, string chatText)
    {
        var seat = GetSeat(seatIdx);
        seat.ShowChatText(chatText);
    }

    public void ShowVoiceWave(int seatIdx)
    {
        var seat = GetSeat(seatIdx);
        seat.ShowVoiceWave();
    }

    /// <summary>
    /// 隐藏语音波浪
    /// </summary>
    public void HideVoiceWave(int seatIdx)
    {
        var seat = GetSeat(seatIdx);
        seat.HideVoiceWave();
    }

    /// <summary>
    /// 礼物
    /// </summary>
    public void Function_SynSendGift(int fromIndex, int toIndex, string gift)
    {
        Vector3 seatFrom = fromIndex == 0 ? GetSeat(fromIndex).transform.localPosition + Vector3.up * 400 : GetSeat(fromIndex).transform.localPosition;
        Vector3 seatTo = toIndex == 0 ? GetSeat(toIndex).transform.localPosition + Vector3.up * 400 : GetSeat(toIndex).transform.localPosition;
        GF.UI.LoadPrefab(AssetsPath.GetPrefab("UI/Chat/" + gift), (gameObject) =>
        {
            if (gameObject == null) return;
            GameObject giftObj = Object.Instantiate(gameObject as GameObject, transform);
            if (fromIndex == -1)
            {
                giftObj.transform.localPosition = transform.localPosition;
            }
            else
            {
                giftObj.transform.localPosition = seatFrom;
            }
            giftObj.transform.localScale = gift switch
            {
                "2001" => new Vector3(1.8f, 1.8f, 1.8f),
                "2003" => new Vector3(1.8f, 1.8f, 1.8f),
                "2005" => new Vector3(2.2f, 2.2f, 2.2f),
                "2007" => new Vector3(2.2f, 2.2f, 2.2f),
                _ => new Vector3(2f, 2f, 2f),
            };
            FrameAnimator frameAnimator = giftObj.GetComponent<FrameAnimator>();
            frameAnimator.Loop = false;
            frameAnimator.Stop();
            frameAnimator.FinishEvent += (giftobj) =>
            {
                Object.Destroy(giftobj);
            };
            giftObj.transform.DOLocalMove(seatTo, 0.8f).OnComplete(() =>
            {
                frameAnimator.Framerate = 6;
                if (gift == "2003")
                {
                    frameAnimator.Framerate = 2;
                }
                frameAnimator.Play();
                Sound.PlayEffect("face/" + gift + ".mp3");
            });
        });
    }
    #endregion

    #region 特效管理
    /// <summary>
    /// 显示特效
    /// </summary>
    /// <param name="effectType"></param>
    /// <param name="duration"></param>
    public void ShowEffect(string effectType, float duration)
    {
        string effPath = "";
        string audioKeys = "";
        switch (effectType)
        {
            case "Last2Eff":
                effPath = AssetsPath.GetPrefab("UI/MaJiang/Game/Last2Eff");
                audioKeys = AudioKeys.MJ_EFFECT_AUDIO_ZHSZ;
                break;
            case "Last3Eff":
                effPath = AssetsPath.GetPrefab("UI/MaJiang/Game/Last3Eff");
                audioKeys = AudioKeys.MJ_EFFECT_AUDIO_ZHSZ;
                break;
            case "Last4Eff":
                effPath = AssetsPath.GetPrefab("UI/MaJiang/Game/Last4Eff");
                audioKeys = AudioKeys.MJ_EFFECT_AUDIO_ZHSZ;
                break;
        }

        if (!string.IsNullOrEmpty(effPath))
        {
            GF.UI.LoadPrefab(effPath, (gameObject) =>
            {
                if (gameObject == null) return;
                GameObject effectObj = Object.Instantiate(gameObject as GameObject, transform);

                if (effectType == "Last2Eff" || effectType == "Last3Eff" || effectType == "Last4Eff")
                {
                    effectObj.transform.localPosition = new Vector3(0, 500, 0);
                }
                // 播放音效
                Sound.PlayEffect(audioKeys);

                // 如果设置了持续时间，自动销毁
                if (duration > 0)
                {
                    Destroy(effectObj, duration);
                }
            });
        }
    }


    #endregion

    #region 2D操作按钮管理

    /// <summary>
    /// 初始化2D操作按钮（在InitPanel中调用）
    /// </summary>
    private void Initialize2DOperationButtons()
    {
        pengButton?.onClick.RemoveAllListeners();
        chiButton?.onClick.RemoveAllListeners();
        gangButton?.onClick.RemoveAllListeners();
        xiaoButton?.onClick.RemoveAllListeners();
        tingButton?.onClick.RemoveAllListeners();
        huButton?.onClick.RemoveAllListeners();
        guoButton?.onClick.RemoveAllListeners();

        // 绑定按钮点击事件
        pengButton?.onClick.AddListener(() => On2DButtonClick(MJOption.Pen));
        chiButton?.onClick.AddListener(() => On2DButtonClick(MJOption.Chi));
        gangButton?.onClick.AddListener(() => On2DButtonClick(MJOption.Gang));
        xiaoButton?.onClick.AddListener(() => On2DButtonClick(MJOption.Gang));
        tingButton?.onClick.AddListener(() => On2DButtonClick(MJOption.Ting));
        huButton?.onClick.AddListener(() => On2DButtonClick(MJOption.Hu));
        guoButton?.onClick.AddListener(() => On2DButtonClick(MJOption.Guo));

        // 初始隐藏按钮面板
        pcghtButtonPanel.SetActive(false);

        // 初始化显示桌面按钮
        InitializeShowTableButton();

        // 初始化卡五星操作按钮
        InitializeKwxOperationButtons();
    }

    /// <summary>
    /// 显示2D操作按钮
    /// 同时管理2D打牌交互状态
    /// </summary>
    /// <param name="options">可执行的操作列表</param>
    /// <param name="targetCard">目标牌值</param>
    /// <param name="showXiao">是否显示笑按钮（替代杠按钮），如果不指定则根据规则自动判断</param>
    public void Show2DOperationButtons(List<MJOption> options)
    {
        if (options == null || options.Count == 0)
        {
            Hide2DOperationButtons();
            canPlayCard2D = false;
            return;
        }

        if (options.Contains(MJOption.Discard))
        {
            // 有打牌操作时，启用打牌交互
            canPlayCard2D = true;
            Update2DHandCardInteraction(baseMahjongGameManager.CurrentTurnSeat);
            options.RemoveAll(opt => opt == MJOption.Discard);
        }

        // 如果没有任何操作，隐藏面板
        if (options.Count == 0)
        {
            Hide2DOperationButtons();
            return;
        }

        // 如果只有过按钮，则不显示面板
        if (options.Count == 1 && options.Contains(MJOption.Guo))
        {
            Hide2DOperationButtons();
            return;
        }

        current2DChiCombinations = null;
        current2DGangCards = null;

        // 卡五星和血流规则使用卡五星专属面板
        if (IsKaWuXingRule || IsXueLiuRule)
        {
            // 卡五星亮牌（MJOption.Ting）特殊处理
            if (IsKaWuXingRule && options.Contains(MJOption.Ting))
            {
                // 获取可听的牌列表
                List<int> tingCards = GetTingCards();
                tingPaiList = tingCards ?? new List<int>();
            }

            // 显示卡五星操作按钮
            ShowKwxOperationButtons(options);

            // 处理碰杠标记
            HandlePengGangMarks(options);
            return;
        }

        // 非卡五星规则下，Ting按钮不显示，所以如果只有Ting和Guo则不显示面板
        // 检查是否有实际需要显示的操作（排除Ting，因为非卡五星规则不显示Ting按钮）
        bool hasValidOptions = options.Any(opt => opt != MJOption.Ting && opt != MJOption.Guo);
        if (!hasValidOptions)
        {
            // 只有Ting和/或Guo，不显示面板
            Hide2DOperationButtons();
            return;
        }

        // 显示面板
        pcghtButtonPanel.SetActive(true);
        SetShowTableBtnVisible(true);

        // 使用统一的按钮更新逻辑
        UpdateOperationButtons(options);

        // 如果显示碰或杠按钮，标记手牌中可以碰杠的牌
        HandlePengGangMarks(options);
    }

    /// <summary>
    /// 统一的按钮更新逻辑（避免代码重复）
    /// </summary>
    /// <param name="options">可执行的操作列表</param>
    private void UpdateOperationButtons(List<MJOption> options)
    {
        if (IsKaWuXingRule || IsXueLiuRule)
        {
            // 卡五星和血流按钮（血流不显示亮按钮）
            pengButton_Kwx?.gameObject.SetActive(options.Contains(MJOption.Pen));
            chiButton_Kwx?.gameObject.SetActive(options.Contains(MJOption.Chi));
            gangButton_Kwx?.gameObject.SetActive(options.Contains(MJOption.Gang));
            liangButton_Kwx?.gameObject.SetActive(options.Contains(MJOption.Ting) && IsKaWuXingRule); // 只有卡五星显示亮按钮
            huButton_Kwx?.gameObject.SetActive(options.Contains(MJOption.Hu));
            guoButton_Kwx?.gameObject.SetActive(options.Contains(MJOption.Guo));
        }
        else
        {
            // 通用按钮（其他规则）
            pengButton?.gameObject.SetActive(options.Contains(MJOption.Pen));
            chiButton?.gameObject.SetActive(options.Contains(MJOption.Chi));

            // 杠和笑按钮互斥
            bool hasGang = options.Contains(MJOption.Gang);
            if (hasGang)
            {
                bool isXianTaoHuangHuang = currentRule is XianTaoHuangHuangRule;
                gangButton?.gameObject.SetActive(!isXianTaoHuangHuang);
                xiaoButton?.gameObject.SetActive(isXianTaoHuangHuang);
            }
            else
            {
                gangButton?.gameObject.SetActive(false);
                xiaoButton?.gameObject.SetActive(false);
            }

            // 听按钮只在卡五星规则显示，仙桃晃晃等其他规则不显示
            tingButton?.gameObject.SetActive(false);
            huButton?.gameObject.SetActive(options.Contains(MJOption.Hu));
            guoButton?.gameObject.SetActive(options.Contains(MJOption.Guo));
        }
    }

    /// <summary>
    /// 隐藏2D操作按钮
    /// </summary>
    public void Hide2DOperationButtons()
    {
        pcghtButtonPanel.SetActive(false);
        // 隐藏卡五星操作按钮
        HideKwxOperationButtons();
        SetShowTableBtnVisible(false);
        ResetShowTableBtnPressState();
        // 清除操作选项
        current2DOptions.Clear();

        // 清除碰杠标记
        var mySeat = GetSeat(0);
        mySeat.ClearPengGangMarks();

        // 隐藏亮牌面板
        HideLiangPaiPanel();
    }

    private void InitializeShowTableButton()
    {
        if (ShowTableBtn_OP == null) return;

        ShowTableBtn_OP.gameObject.SetActive(false);

        var trigger = ShowTableBtn_OP.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = ShowTableBtn_OP.gameObject.AddComponent<EventTrigger>();
        }

        trigger.triggers.Clear();

        AddEventTrigger(trigger, EventTriggerType.PointerDown, _ => OnShowTableBtnPointerDown());
        AddEventTrigger(trigger, EventTriggerType.PointerUp, _ => OnShowTableBtnPointerUp());
        AddEventTrigger(trigger, EventTriggerType.PointerExit, _ => OnShowTableBtnPointerUp());
    }

    private void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, System.Action<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(data => action?.Invoke(data));
        trigger.triggers.Add(entry);
    }

    private void OnShowTableBtnPointerDown()
    {
        if (ShowTableBtn_OP == null || !ShowTableBtn_OP.gameObject.activeInHierarchy) return;

        wasKwxPanelVisible = pcghtButtonPanel_Kwx != null && pcghtButtonPanel_Kwx.activeSelf;
        wasNormalPanelVisible = pcghtButtonPanel != null && pcghtButtonPanel.activeSelf;

        if (wasKwxPanelVisible)
        {
            pcghtButtonPanel_Kwx.SetActive(false);
        }
        if (wasNormalPanelVisible)
        {
            pcghtButtonPanel.SetActive(false);
        }

        isOpPanelHiddenByPress = wasKwxPanelVisible || wasNormalPanelVisible;
    }

    private void OnShowTableBtnPointerUp()
    {
        if (!isOpPanelHiddenByPress) return;

        if (wasKwxPanelVisible && pcghtButtonPanel_Kwx != null)
        {
            pcghtButtonPanel_Kwx.SetActive(true);
        }
        if (wasNormalPanelVisible && pcghtButtonPanel != null)
        {
            pcghtButtonPanel.SetActive(true);
        }

        ResetShowTableBtnPressState();
    }

    private void ResetShowTableBtnPressState()
    {
        isOpPanelHiddenByPress = false;
        wasKwxPanelVisible = false;
        wasNormalPanelVisible = false;
    }

    private void SetShowTableBtnVisible(bool visible)
    {
        if (ShowTableBtn_OP == null) return;
        ShowTableBtn_OP.gameObject.SetActive(visible);
    }

    /// <summary>
    /// 2D操作按钮点击处理
    /// </summary>
    /// <param name="selectedOption">选择的操作</param>
    private void On2DButtonClick(MJOption selectedOption)
    {
        if (Util.IsClickLocked()) return;

        // 播放按钮点击音效
        Sound.PlayEffect(AudioKeys.SOUND_BTN);

        // 发送操作请求到服务器（按钮隐藏统一在Send2DOperation内部处理）
        Send2DOperation(selectedOption);
    }

    /// <summary>
    /// 发送2D操作到服务器（参考3D实现，支持吃碰杠的详细参数）
    /// </summary>
    /// <param name="option">操作类型</param>
    private void Send2DOperation(MJOption option)
    {
        if (mj_procedure == null)
        {
            GF.LogError("[2D视图] mj_procedure 为空，无法发送操作");
            return;
        }

        // 获取目标牌值（碰吃杠胡使用）
        int targetCardValue = Util.ConvertFaceValueToServerInt(lastDiscard);

        // 如果本地处于被锁牌限制，且即将执行碰或杠等会触发解锁的操作，则弹出确认解锁提示
        // 注意：只有接杠（明杠，别人出的牌我杠）才需要提示，补杠和暗杠不需要
        // 判断依据：接杠时当前不是自己的回合（CurrentTurnSeat != 0），补杠/暗杠是自己的回合
        bool isJieGang = option == MJOption.Gang && baseMahjongGameManager.CurrentTurnSeat != 0;
        
        if (isLocalLockedBySuoPai && (option == MJOption.Pen || isJieGang))
        {
            string title = "解锁提示";
            string content = "您是否确定解锁，解锁后胡牌下家胡分翻倍；若下家胡牌则自己需承包";
            Util.GetInstance().OpenMJConfirmationDialog(title, content, () =>
            {
                // 确认解锁：标记本地为已解锁（本地状态），隐藏suoPaiTips，并记录到 openDoublePlayers
                isLocalLockedBySuoPai = false;
                long myId = GetSeat(0)?.deskPlayer?.BasePlayer?.PlayerId ?? 0;
                if (myId != 0)
                {
                    openDoublePlayers.Add(myId);
                }
                suoPaiTips.gameObject.SetActive(false);

                // 继续发送原始操作请求
                mj_procedure.Send_MJOptionRq(option, targetCardValue);
                Hide2DOperationButtons();
            });
            return;
        }

        Hide2DOperationButtons();

        switch (option)
        {
            case MJOption.Pen:
                // 碰牌：发送碰的牌值
                mj_procedure.Send_MJOptionRq(MJOption.Pen, targetCardValue);
                break;

            case MJOption.Chi:
                // 吃牌：实时计算吃牌组合（点击时计算，确保数据最新）
                if (lastDiscard != MahjongFaceValue.MJ_UNKNOWN)
                {
                    current2DChiCombinations = Calculate2DChiCombinations(lastDiscard);
                }
                else
                {
                    GF.LogWarning("[2D视图] 吃牌操作但目标牌为UNKNOWN，无法计算吃牌组合");
                    current2DChiCombinations = null;
                }

                // 需要判断吃牌类型（左吃/中吃/右吃）
                // 如果有多个吃牌组合
                if (current2DChiCombinations != null && current2DChiCombinations.Count > 1)
                {
                }
                else
                {
                    // 只有一种吃法，直接发送
                    int chiType = Calculate2DChiType();
                    mj_procedure.Send_MJOptionRq(MJOption.Chi, chiType);
                }
                break;

            case MJOption.Gang:
                // 杠牌：实时计算可杠牌列表（点击时计算，确保数据最新）
                current2DGangCards = Calculate2DGangCombinations();

                // 需要判断杠哪个字
                // 如果有多个杠牌 按照索引取最小值的杠牌
                if (current2DGangCards != null && current2DGangCards.Count > 1)
                {
                    // 选择索引最小的杠牌
                    MahjongFaceValue selectedGangCard = current2DGangCards[0];
                    int gangCardValue = Util.ConvertFaceValueToServerInt(selectedGangCard);
                    mj_procedure.Send_MJOptionRq(MJOption.Gang, gangCardValue);
                }
                else if (current2DGangCards != null && current2DGangCards.Count == 1)
                {
                    // 只有一种杠牌，直接发送
                    int gangCardValue = Util.ConvertFaceValueToServerInt(current2DGangCards[0]);
                    mj_procedure.Send_MJOptionRq(MJOption.Gang, gangCardValue);
                }
                else
                {
                    // ⚠️ 异常情况：没有找到可杠的牌
                    GF.LogError($"[2D视图] 杠牌列表为空，无法确定要杠哪张牌！targetCard={lastDiscard}");
                }
                break;

            case MJOption.Ting:
                // 发送听牌请求
                mj_procedure.Send_MJOptionRq(MJOption.Ting);
                break;

            case MJOption.Hu:
                // 发送胡牌请求（带上胡的牌值）
                // 需要区分自摸胡和点炮胡：自己回合用摸牌，别人回合用lastDiscard
                int huTargetCard = Calculate2DOperationTargetCard();
                mj_procedure.Send_MJOptionRq(MJOption.Hu, huTargetCard);
                break;

            case MJOption.Guo:
                // 发送过牌请求（带上过的牌值）
                // 需要正确计算过牌的目标牌：
                // 1. 自己摸牌后的暗杠/自摸胡弃胡 -> 使用摸牌
                // 2. 别人出牌后的碰/杠/胡弃胡 -> 使用lastDiscard
                // 3. 如果当前有可杠牌列表（暗杠场景），使用计算的杠牌值
                int guoTargetCard = Calculate2DGuoTargetCard();
                mj_procedure.Send_MJOptionRq(MJOption.Guo, guoTargetCard);
                break;

            default:
                GF.LogWarning($"[2D视图] 未知操作类型: {option}");
                break;
        }
    }

    /// <summary>
    /// 计算操作的目标牌值（胡牌、过牌通用）
    /// 自己回合（摸牌后）使用摸牌值，别人回合使用lastDiscard
    /// </summary>
    /// <returns>目标牌值（服务器格式）</returns>
    private int Calculate2DOperationTargetCard()
    {
        // 判断当前是否是自己的回合（自己摸牌后弹出的操作选项）
        bool isMyTurn = baseMahjongGameManager.CurrentTurnSeat == 0;
        
        if (isMyTurn)
        {
            // 自己回合：使用摸牌值（自摸胡/暗杠等场景）
            var mySeat = GetSeat(0);
            if (mySeat != null)
            {
                MahjongFaceValue moPaiValue = mySeat.GetMoPaiData();
                if (moPaiValue != MahjongFaceValue.MJ_UNKNOWN)
                {
                    return Util.ConvertFaceValueToServerInt(moPaiValue);
                }
            }
        }
        
        // 别人回合或没有摸牌：使用lastDiscard
        return Util.ConvertFaceValueToServerInt(lastDiscard);
    }

    /// <summary>
    /// 计算过牌操作的目标牌值
    /// 需要考虑多种场景：自己摸牌后暗杠弃杠、别人出牌后碰杠胡弃胡等
    /// </summary>
    /// <returns>目标牌值（服务器格式）</returns>
    private int Calculate2DGuoTargetCard()
    {
        // 判断当前是否是自己的回合
        bool isMyTurn = baseMahjongGameManager.CurrentTurnSeat == 0;
        
        if (isMyTurn)
        {
            // 自己回合：可能是暗杠/自摸胡的弃操作
            // 优先检查是否有可杠牌列表（暗杠场景）
            var gangCards = Calculate2DGangCombinations();
            if (gangCards != null && gangCards.Count > 0)
            {
                // 有可杠的牌，使用第一张杠牌作为过牌目标
                return Util.ConvertFaceValueToServerInt(gangCards[0]);
            }
            
            // 没有杠牌，使用摸牌值（自摸胡弃胡场景）
            var mySeat = GetSeat(0);
            if (mySeat != null)
            {
                MahjongFaceValue moPaiValue = mySeat.GetMoPaiData();
                if (moPaiValue != MahjongFaceValue.MJ_UNKNOWN)
                {
                    return Util.ConvertFaceValueToServerInt(moPaiValue);
                }
            }
        }
        
        // 别人回合：使用lastDiscard（碰/杠/胡的弃操作）
        return Util.ConvertFaceValueToServerInt(lastDiscard);
    }

    /// <summary>
    /// 计算吃牌类型（参考3D实现）
    /// </summary>
    /// <returns>吃牌类型：1=左吃，2=中吃，3=右吃</returns>
    private int Calculate2DChiType()
    {
        // 默认左吃
        int chiType = 1;

        // 如果有选中的吃牌组合
        if (current2DChiCombinations != null &&
            selected2DChiIndex >= 0 &&
            selected2DChiIndex < current2DChiCombinations.Count)
        {
            MahjongFaceValue[] selectedCombo = current2DChiCombinations[selected2DChiIndex];

            if (selectedCombo != null && selectedCombo.Length == 3)
            {
                // 找到目标牌在组合中的位置
                for (int i = 0; i < selectedCombo.Length; i++)
                {
                    if (selectedCombo[i] == lastDiscard)
                    {
                        chiType = i + 1; // 位置0->LEFT(1), 位置1->MIDDLE(2), 位置2->RIGHT(3)
                        break;
                    }
                }

                GF.LogInfo_gsc($"[2D吃牌] 组合: [{selectedCombo[0]}, {selectedCombo[1]}, {selectedCombo[2]}], 目标牌: {lastDiscard}, 类型: {chiType}");
            }
        }

        return chiType;
    }

    /// <summary>
    /// 计算2D模式下的杠牌组合（基于2D座位数据进行计算）
    /// </summary>
    /// <returns>返回所有可能的杠牌组合，每个元素是一个牌值</returns>
    private List<MahjongFaceValue> Calculate2DGangCombinations()
    {
        List<MahjongFaceValue> combinations = new List<MahjongFaceValue>();
        // 记录明杠的牌（用于排序时优先明杠）
        HashSet<MahjongFaceValue> mingGangCards = new HashSet<MahjongFaceValue>();

        // 获取本地玩家座位（座位0）
        var mySeat = GetSeat(0);
        if (mySeat == null)
        {
            GF.LogWarning("[2D杠牌] 无法获取自己的2D座位");
            return combinations;
        }

        // 获取当前游戏规则
        if (currentRule == null)
        {
            GF.LogWarning("[2D杠牌] currentRule为空");
            return combinations;
        }

        // 直接从 Manager 读取飘赖子数据（避免通过规则类转换出错）
        MahjongFaceValue piaoLaiZi = MahjongFaceValue.MJ_UNKNOWN;
        bool hasLaizi = currentRule.HasLaiZi();
        piaoLaiZi = baseMahjongGameManager.CurrentPiaoLaiZi;

        // 统计手牌（不包括摸牌区）
        // 亮牌后手牌被移到组牌区，需要从liangPaiRemainingCards获取
        Dictionary<MahjongFaceValue, int> handCardCounts = new Dictionary<MahjongFaceValue, int>();
        if (mySeat.HasLiangPai)
        {
            // 亮牌状态：从liangPaiRemainingCards获取剩余手牌
            var remainingCards = mySeat.GetLiangPaiRemainingCards();
            foreach (int cardValue in remainingCards)
            {
                MahjongFaceValue faceValue = Util.ConvertServerIntToFaceValue(cardValue);
                if (faceValue != MahjongFaceValue.MJ_UNKNOWN)
                {
                    if (!handCardCounts.ContainsKey(faceValue))
                        handCardCounts[faceValue] = 0;
                    handCardCounts[faceValue]++;
                }
            }
            GF.LogInfo_gsc($"[2D杠牌] 亮牌状态，从liangPaiRemainingCards获取手牌，数量:{remainingCards.Count}");
        }
        else
        {
            // 正常状态：从HandContainer获取手牌
            var handCards = mySeat.GetHandCardsData();
            if (handCards != null)
            {
                foreach (var card in handCards)
                {
                    if (!handCardCounts.ContainsKey(card))
                        handCardCounts[card] = 0;
                    handCardCounts[card]++;
                }
            }
        }

        // 获取摸牌区的牌
        MahjongFaceValue moPaiValue = MahjongFaceValue.MJ_UNKNOWN;
        if (mySeat.MoPaiContainer.childCount > 0)
        {
            var moPaiObj = mySeat.MoPaiContainer.GetChild(0).gameObject;
            var mjCard = moPaiObj.GetComponent<MjCard_2D>();
            if (mjCard != null)
            {
                // 直接使用mahjongFaceValue属性，已经是正确的MahjongFaceValue
                moPaiValue = mjCard.mahjongFaceValue;
            }
        }

        // 1. 检查明杠：最后打出的牌+手牌3张（普通牌）或2张（赖子）【优先检查明杠】
        // 获取最后打出的牌（从lastDiscard，这是重连时传入的目标牌）
        // 只有当摸牌区没有牌时才检查明杠（摸牌区有牌代表已经摸牌了，不再判定最后打的那一张牌）
        if (lastDiscard != MahjongFaceValue.MJ_UNKNOWN && !combinations.Contains(lastDiscard) && moPaiValue == MahjongFaceValue.MJ_UNKNOWN)
        {
            int handCount = handCardCounts.ContainsKey(lastDiscard) ? handCardCounts[lastDiscard] : 0;
            bool isLaizi = hasLaizi && lastDiscard == piaoLaiZi;

            // 赖子只需要2张手牌，普通牌需要3张手牌
            int requiredHandCount = isLaizi ? 2 : 3;

            if (handCount >= requiredHandCount)
            {
                combinations.Add(lastDiscard);
                mingGangCards.Add(lastDiscard);  // 记录为明杠
            }
        }

        // 2. 检查补杠：遍历MeldContainer中的碰牌
        if (mySeat.MeldContainer != null && mySeat.MeldContainer.childCount > 0)
        {
            // 统计MeldContainer中的牌组
            Dictionary<MahjongFaceValue, int> meldCounts = new Dictionary<MahjongFaceValue, int>();
            for (int i = 0; i < mySeat.MeldContainer.childCount; i++)
            {
                var cardObj = mySeat.MeldContainer.GetChild(i).gameObject;
                var mjCard = cardObj.GetComponent<MjCard_2D>();
                if (mjCard != null)
                {
                    // 直接使用mahjongFaceValue属性，已经是正确的MahjongFaceValue
                    MahjongFaceValue cardValue = mjCard.mahjongFaceValue;
                    if (!meldCounts.ContainsKey(cardValue))
                        meldCounts[cardValue] = 0;
                    meldCounts[cardValue]++;
                }
            }

            // 检查每个碰过的牌（数量=3的牌组）
            foreach (var kvp in meldCounts)
            {
                if (kvp.Value == 3)  // 碰牌是3张
                {
                    MahjongFaceValue pengPaiValue = kvp.Key;

                    // 检查摸牌区或手牌区是否有这张牌
                    bool hasPaiInMoPai = (moPaiValue == pengPaiValue);
                    int handCount = handCardCounts.ContainsKey(pengPaiValue) ? handCardCounts[pengPaiValue] : 0;

                    if (hasPaiInMoPai || handCount >= 1)
                    {
                        if (!combinations.Contains(pengPaiValue))
                        {
                            combinations.Add(pengPaiValue);
                        }
                    }
                }
            }
        }

        // 3. 检查暗杠：摸牌+手牌共4张（普通牌）或3张（赖根）
        if (moPaiValue != MahjongFaceValue.MJ_UNKNOWN)
        {
            int handCount = handCardCounts.ContainsKey(moPaiValue) ? handCardCounts[moPaiValue] : 0;
            // 总数 = 手牌数 + 摸牌（1张）
            int totalCount = handCount + 1;
            bool isLaizi = hasLaizi && moPaiValue == piaoLaiZi;

            // 赖根（飘赖子）3张就可以暗杠（小朝天/朝天杠），普通牌需要4张
            int requiredCount = isLaizi ? 3 : 4;

            // 判断是否满足暗杠条件
            if (totalCount >= requiredCount && !combinations.Contains(moPaiValue))
            {
                combinations.Add(moPaiValue);
            }
        }

        // 4. 检查手牌中的暗杠（用于重连等特殊情况）
        foreach (var kvp in handCardCounts)
        {
            bool isLaizi = hasLaizi && kvp.Key == piaoLaiZi;
            // 赖根（飘赖子）3张就可以暗杠，普通牌需要4张
            int requiredCount = isLaizi ? 3 : 4;

            if (kvp.Value >= requiredCount && !combinations.Contains(kvp.Key))
            {
                combinations.Add(kvp.Key);
            }
        }

        // 排序规则：明杠优先，同类型内按牌值从小到大排序
        // 明杠 > 补杠/暗杠（补杠和暗杠按牌值排序）
        combinations.Sort((a, b) =>
        {
            bool aIsMingGang = mingGangCards.Contains(a);
            bool bIsMingGang = mingGangCards.Contains(b);

            // 明杠优先
            if (aIsMingGang && !bIsMingGang) return -1;
            if (!aIsMingGang && bIsMingGang) return 1;

            // 同类型按牌值排序
            return Util.ConvertFaceValueToServerInt(a).CompareTo(Util.ConvertFaceValueToServerInt(b));
        });

        return combinations;
    }

    /// <summary>
    /// 计算2D模式下的吃牌组合（独立实现，不依赖3D）
    /// </summary>
    /// <param name="targetCard">目标牌值（被吃的牌）</param>
    /// <returns>返回所有可能的吃牌组合</returns>
    private List<MahjongFaceValue[]> Calculate2DChiCombinations(MahjongFaceValue targetCard)
    {
        List<MahjongFaceValue[]> combinations = new List<MahjongFaceValue[]>();

        // 只有数字牌才能吃
        if (!IsNumberCard2D(targetCard))
        {
            return combinations;
        }

        // 从2D座位获取自己的手牌
        var mySeat = GetSeat(0);  // 0号座位是本地玩家
        // 统计手牌（不包括摸牌区）
        // 亮牌后手牌被移到组牌区，需要从liangPaiRemainingCards获取
        Dictionary<MahjongFaceValue, int> handCardCounts = new Dictionary<MahjongFaceValue, int>();
        if (mySeat.HasLiangPai)
        {
            // 亮牌状态：从liangPaiRemainingCards获取剩余手牌
            var remainingCards = mySeat.GetLiangPaiRemainingCards();
            foreach (int cardValue in remainingCards)
            {
                MahjongFaceValue faceValue = Util.ConvertServerIntToFaceValue(cardValue);
                if (faceValue != MahjongFaceValue.MJ_UNKNOWN)
                {
                    if (!handCardCounts.ContainsKey(faceValue))
                        handCardCounts[faceValue] = 0;
                    handCardCounts[faceValue]++;
                }
            }
        }
        else
        {
            // 正常状态：从HandContainer获取手牌
            var handCards = mySeat.GetHandCardsData();
            if (handCards != null)
            {
                foreach (var card in handCards)
                {
                    if (!handCardCounts.ContainsKey(card))
                        handCardCounts[card] = 0;
                    handCardCounts[card]++;
                }
            }
        }

        // 检查三种吃法
        // 1. 左吃：手里有 target+1, target+2
        MahjongFaceValue card1 = GetNextCard2D(targetCard, 1);
        MahjongFaceValue card2 = GetNextCard2D(targetCard, 2);
        if (card1 != MahjongFaceValue.MJ_UNKNOWN && card2 != MahjongFaceValue.MJ_UNKNOWN)
        {
            if (HasCard(handCardCounts, card1) && HasCard(handCardCounts, card2))
            {
                combinations.Add(new MahjongFaceValue[] { targetCard, card1, card2 });
            }
        }

        // 2. 中吃：手里有 target-1, target+1
        MahjongFaceValue cardPrev = GetNextCard2D(targetCard, -1);
        MahjongFaceValue cardNext = GetNextCard2D(targetCard, 1);
        if (cardPrev != MahjongFaceValue.MJ_UNKNOWN && cardNext != MahjongFaceValue.MJ_UNKNOWN)
        {
            if (HasCard(handCardCounts, cardPrev) && HasCard(handCardCounts, cardNext))
            {
                combinations.Add(new MahjongFaceValue[] { cardPrev, targetCard, cardNext });
            }
        }

        // 3. 右吃：手里有 target-2, target-1
        MahjongFaceValue card_2 = GetNextCard2D(targetCard, -2);
        MahjongFaceValue card_1 = GetNextCard2D(targetCard, -1);
        if (card_2 != MahjongFaceValue.MJ_UNKNOWN && card_1 != MahjongFaceValue.MJ_UNKNOWN)
        {
            if (HasCard(handCardCounts, card_2) && HasCard(handCardCounts, card_1))
            {
                combinations.Add(new MahjongFaceValue[] { card_2, card_1, targetCard });
            }
        }

        return combinations;
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

        var seat = GetSeat(seatIdx);
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
        }
    }

    /// <summary>
    /// 检查手牌中是否有指定的牌
    /// </summary>
    private bool HasCard(Dictionary<MahjongFaceValue, int> handCardCounts, MahjongFaceValue card)
    {
        return handCardCounts.ContainsKey(card) && handCardCounts[card] > 0;
    }

    /// <summary>
    /// 判断是否为数字牌（条万筒）- 2D版本
    /// </summary>
    private bool IsNumberCard2D(MahjongFaceValue card)
    {
        return (card >= MahjongFaceValue.MJ_TIAO_1 && card <= MahjongFaceValue.MJ_TIAO_9) ||
               (card >= MahjongFaceValue.MJ_WANG_1 && card <= MahjongFaceValue.MJ_WANG_9) ||
               (card >= MahjongFaceValue.MJ_TONG_1 && card <= MahjongFaceValue.MJ_TONG_9);
    }

    /// <summary>
    /// 获取相邻牌值 - 2D版本
    /// </summary>
    /// <param name="card">基础牌值</param>
    /// <param name="offset">偏移量（-1表示前一张，+1表示后一张）</param>
    /// <returns>相邻的牌值，如果无效则返回MJ_UNKNOWN</returns>
    private MahjongFaceValue GetNextCard2D(MahjongFaceValue card, int offset)
    {
        int cardValue = (int)card;
        int newValue = cardValue + offset;

        // 检查是否在有效范围内
        if (newValue >= (int)MahjongFaceValue.MJ_TIAO_1 && newValue <= (int)MahjongFaceValue.MJ_TIAO_9)
            return (MahjongFaceValue)newValue;
        if (newValue >= (int)MahjongFaceValue.MJ_WANG_1 && newValue <= (int)MahjongFaceValue.MJ_WANG_9)
            return (MahjongFaceValue)newValue;
        if (newValue >= (int)MahjongFaceValue.MJ_TONG_1 && newValue <= (int)MahjongFaceValue.MJ_TONG_9)
            return (MahjongFaceValue)newValue;

        return MahjongFaceValue.MJ_UNKNOWN;
    }

    #endregion

    #region 麻将牌样式切换

    /// <summary>
    /// 麻将牌样式改变回调（使用统一资源管理器）
    /// 更新所有麻将牌的背景颜色（包括四个座位的手牌、弃牌、组牌、摸牌，以及胡牌提示）
    /// </summary>
    /// <param name="newStyle">新的颜色样式</param>
    private void OnCardStyleChanged(MjColor newStyle)
    {
        GF.LogInfo_gsc($"[麻将样式切换] 开始更新所有麻将牌: {newStyle}");

        // 更新所有座位的麻将牌
        if (seats2D != null)
        {
            for (int i = 0; i < 4; i++)
            {
                seats2D[i]?.UpdateAllCardColors(newStyle);
            }
        }

        // 更新出牌动画牌的背景
        if (DapaiAniCard != null)
        {
            var sprite = MahjongResourceManager.GetBackgroundSprite(newStyle, 0, "hand");
            if (sprite != null)
            {
                DapaiAniCard.SetCardBgSprite(sprite);
            }
        }

        // 更新胡牌提示中的牌
        UpdateHuPaiTipsColors(newStyle);

        GF.LogInfo_gsc($"[麻将样式切换] 所有麻将牌更新完成");
    }

    /// <summary>
    /// 更新胡牌提示中所有牌的颜色
    /// </summary>
    private void UpdateHuPaiTipsColors(MjColor newStyle)
    {
        // 胡牌提示在HuPaiTips组件的huPaiTipsContent容器中
        if (huPaiTips == null || huPaiTips.huPaiTipsContent == null) return;

        var sprite = MahjongResourceManager.GetBackgroundSprite(newStyle, 0, "discard");
        if (sprite == null) return;

        for (int i = 0; i < huPaiTips.huPaiTipsContent.childCount; i++)
        {
            Transform child = huPaiTips.huPaiTipsContent.GetChild(i);
            MjCard_2D mjCard = child.GetComponent<MjCard_2D>();
            if (mjCard != null)
            {
                mjCard.SetCardBgSprite(sprite);
            }
        }
    }

    #endregion

    #region 卡五星亮牌辅助方法

    /// <summary>
    /// 获取可听的牌列表
    /// </summary>
    private List<int> GetTingCards()
    {
        List<int> tingCards = new List<int>();

        if (baseMahjongGameManager == null || baseMahjongGameManager.mjHuTingCheck == null)
            return tingCards;

        var mySeat = GetSeat(0);
        if (mySeat == null)
            return tingCards;

        // 获取完整的牌数据（手牌+摸牌）转换为检测数组
        byte[] cardArray = GetPlayerCompleteCardArray();
        byte laiziIndex = GetCurrentLaiziIndex();

        // 使用CheckTing检测听牌信息
        TingData[] tingDatas = baseMahjongGameManager.mjHuTingCheck.CreateTingDataMemory();
        if (tingDatas != null)
        {
            baseMahjongGameManager.mjHuTingCheck.CheckTing(cardArray, laiziIndex, ref tingDatas);

            // 收集所有可听的牌
            for (int i = 0; i < tingDatas.Length; i++)
            {
                if (tingDatas[i].tingCardIdx == -1)
                    break;

                // tingCardIdx是牌的索引（0-33），需要转换为服务器牌值
                int tingCardIdx = tingDatas[i].tingCardIdx;
                MahjongFaceValue tingCard = (MahjongFaceValue)tingCardIdx;
                int serverCardValue = Util.ConvertFaceValueToServerInt(tingCard);

                if (!tingCards.Contains(serverCardValue))
                {
                    tingCards.Add(serverCardValue);
                }
            }
        }

        return tingCards;
    }

    /// <summary>
    /// 根据服务器返回的fan字段获取卡五星胡牌类型特效列表
    /// </summary>
    /// <param name="fanList">服务器返回的番型编号列表</param>
    /// <returns>胡牌类型特效名称列表</returns>
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
                GF.LogInfo($"[卡五星] 识别番型: {fanCode}({fanName}) -> {effectName}");
            }
        }

        return effects;
    }

    #endregion

}

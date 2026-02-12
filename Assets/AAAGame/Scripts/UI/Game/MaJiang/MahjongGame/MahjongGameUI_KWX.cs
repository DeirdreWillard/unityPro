
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using NetMsg;
using Spine.Unity;
using DG.Tweening;
using System.Collections;
using System;

/// <summary>
/// 卡五星额外逻辑
/// </summary>
public partial class MahjongGameUI
{
    [Header("===== 卡五星 =====")]
    public GameObject PiaoPanel;         //  飘分面板(发牌前飘分阶段)
    public List<GameObject> XuanPaiZhongs;         //  选牌中(换牌时没有选好牌需要显示选牌中)
    public List<GameObject> XuanPaiEnds;         //  选牌结束
    public GameObject TouZiAniPanel;         //  骰子动画面板(换牌时根据骰子点数播放换牌方向动画)
    public SkeletonAnimation TouZiSpineAni;  //  骰子Spine动画
    public Image TouZiResultImage;           //  骰子结果图片
    public Button BtnHuanPaiInfo;            //  换牌结果按钮
    public List<SkeletonAnimation> HuanPaiAnis;         //  换牌动画展示(同时根据顺序完成XuanPaiEnds移到下一位置动画)
    public GameObject HuanPaiAniPanel;         //  换牌动画面板
    public GameObject XuanPaiPanel;         //  选择换牌面板
    public Button BtnXuanPai;            //  确认换牌按钮
    public GameObject HuanPaiInfoPanel;         //  换牌结果展示面板
    public GameObject LiangMaPai;         //  亮码牌动画展示
    public GameObject DissOutCardTips;         //  限制出牌提示(别人亮牌了胡字不能打出提示)

    public GameObject LiangPaiPanel; // 亮牌操作面板
    public GameObject XuanAnPuPaiPanel; // 选暗铺牌面板(点击亮牌按钮后  如果有可选的暗铺牌需要选择然后根据选择看亮哪些牌)
    public Button BtnLiangPaiTrue; // 亮牌确认按钮
    public Button BtnLiangPaiGuo; // 亮牌取消按钮

    [Header("===== 2D操作按钮(卡五星专属) =====")]
    public GameObject pcghtButtonPanel_Kwx;         // 卡五星碰吃杠听胡按钮面板
    public Button pengButton_Kwx;                   // 碰按钮(卡五星专属)
    public Button chiButton_Kwx;                    // 吃按钮(卡五星专属)
    public Button gangButton_Kwx;                   // 杠按钮(卡五星专属)
    public Button liangButton_Kwx;                  // 亮按钮(卡五星专属)
    public Button huButton_Kwx;                     // 胡按钮(卡五星专属)
    public Button guoButton_Kwx;                    // 过按钮(卡五星专属)


    // 换牌选择相关
    private List<MjCard_2D_Interaction> selectedChangeCards = new List<MjCard_2D_Interaction>();  // 选中的换牌
    private const int CHANGE_CARD_COUNT = 3;  // 需要选择3张牌
    private List<int> sentChangeCardValues = new List<int>();  // 已发送换牌请求的牌值（用于识别新换入的牌）
    private List<int> oldHandCardsBeforeChange = new List<int>();  // 换牌前的原手牌

    // 亮牌相关
    private List<int> tingPaiList = new List<int>();  // 可听的牌列表
    private List<int> anPuPaiOptions = new List<int>();  // 可选的暗铺牌列表
    private List<int> selectedAnPuPai = new List<int>();  // 选中的暗铺牌
    private int selectedTingCard = -1;  // 选中要打出的听牌

    #region 卡五星操作按钮管理


    /// <summary>
    /// 初始化卡五星操作按钮（在InitPanel中调用）
    /// </summary>
    private void InitializeKwxOperationButtons()
    {
        // 绑定卡五星按钮点击事件
        pengButton_Kwx?.onClick.RemoveAllListeners();
        chiButton_Kwx?.onClick.RemoveAllListeners();
        gangButton_Kwx?.onClick.RemoveAllListeners();
        liangButton_Kwx?.onClick.RemoveAllListeners();
        huButton_Kwx?.onClick.RemoveAllListeners();
        guoButton_Kwx?.onClick.RemoveAllListeners();

        pengButton_Kwx?.onClick.AddListener(() => On2DButtonClick(MJOption.Pen));
        chiButton_Kwx?.onClick.AddListener(() => On2DButtonClick(MJOption.Chi));
        gangButton_Kwx?.onClick.AddListener(() => On2DButtonClick(MJOption.Gang));
        liangButton_Kwx?.onClick.AddListener(OnLiangPaiButtonClick);
        huButton_Kwx?.onClick.AddListener(() => On2DButtonClick(MJOption.Hu));
        guoButton_Kwx?.onClick.AddListener(() => On2DButtonClick(MJOption.Guo));

        // 初始隐藏卡五星按钮面板
        pcghtButtonPanel_Kwx?.SetActive(false);
    }

    /// <summary>
    /// 显示卡五星操作按钮
    /// </summary>
    /// <param name="options">可执行的操作列表</param>
    private void ShowKwxOperationButtons(List<MJOption> options)
    {
        // 显示卡五星面板，隐藏普通面板
        pcghtButtonPanel_Kwx.SetActive(true);
        pcghtButtonPanel?.SetActive(false);
        SetShowTableBtnVisible(true);

        // 使用统一的按钮更新逻辑
        UpdateOperationButtons(options);

        GF.LogInfo($"[卡五星] 显示操作按钮: {string.Join(",", options)}");
    }

    /// <summary>
    /// 隐藏卡五星操作按钮
    /// </summary>
    private void HideKwxOperationButtons()
    {
        pcghtButtonPanel_Kwx?.SetActive(false);
        SetShowTableBtnVisible(false);
    }

    /// <summary>
    /// 处理碰杠标记（卡五星复用）
    /// </summary>
    private void HandlePengGangMarks(List<MJOption> options)
    {
        // 如果显示碰或杠按钮，标记手牌中可以碰杠的牌
        if (options.Contains(MJOption.Pen) || options.Contains(MJOption.Gang))
        {
            var mySeat = GetSeat(0);
            if (mySeat != null)
            {
                if (options.Contains(MJOption.Pen))
                {
                    // 碰牌一定是针对最后打出的牌
                    int targetCard = (lastDiscard != MahjongFaceValue.MJ_UNKNOWN)
                        ? Util.ConvertFaceValueToServerInt(lastDiscard)
                        : -1;
                    mySeat.UpdatePengGangMarks(targetCard);
                }
                else if (options.Contains(MJOption.Gang))
                {
                    // 杠牌不一定针对最后打出的牌，需要检查所有可杠的牌
                    // 计算所有可杠的牌
                    current2DGangCards = Calculate2DGangCombinations();

                    // 如果有多个杠牌，按照索引取最小值的杠牌进行标记
                    if (current2DGangCards != null && current2DGangCards.Count > 0)
                    {
                        // 取第一个杠牌（索引最小）进行标记
                        MahjongFaceValue targetGangCard = current2DGangCards[0];
                        int targetCardValue = Util.ConvertFaceValueToServerInt(targetGangCard);
                        mySeat.UpdatePengGangMarks(targetCardValue);
                        GF.LogInfo($"[卡五星] 杠牌标记: 选择索引最小的杠牌 {targetGangCard} 进行标记");
                    }
                    else
                    {
                        // 没有可杠的牌，清除标记
                        mySeat.ClearPengGangMarks();
                    }
                }
            }
        }
    }

    #endregion

    #region 飘分逻辑

    /// <summary>
    /// 显示飘分面板
    /// 统一使用 Rule 层属性判断飘分配置
    /// </summary>
    private void ShowPiaoPanel()
    {
        // 使用 Rule 层统一的飘分判断
        int fixedPiaoValue = FixedPiaoValue;
        
        GF.LogInfo($"[飘分] 规则类型: {currentRule?.RuleType}, 固定飘值: {fixedPiaoValue}, 需要面板: {NeedShowPiaoPanel}");
        
        // 如果是定漂模式（fixedPiaoValue >= 0），直接给所有座位赋值
        if (fixedPiaoValue >= 0)
        {
            AutoSetFixedPiaoForAllSeats(fixedPiaoValue);
            return;
        }
        
        // 需要玩家选择，显示飘分选择面板
        PiaoPanel?.SetActive(true);
        
        // 获取Panel容器
        Transform panelContainer = PiaoPanel?.transform.Find("Panel");
        if (panelContainer == null)
        {
            GF.LogWarning("飘分面板缺少Panel容器");
            return;
        }
        
        // 根据规则获取飘分选项数量
        int optionCount = PiaoOptionCount;
        bool showPiao3To5 = optionCount > 3;
        
        // 设置按钮显示/隐藏
        // Piao0、Piao1、Piao2 始终显示
        // Piao3、Piao4、Piao5 根据规则决定
        for (int i = 3; i <= 5; i++)
        {
            Transform piaoBtn = panelContainer.Find($"Piao{i}");
            if (piaoBtn != null)
            {
                piaoBtn.gameObject.SetActive(showPiao3To5);
            }
        }
        
        GF.LogInfo($"显示飘分面板，选项数量: {optionCount}, 是否显示Piao3-5: {showPiao3To5}");
    }
    
    /// <summary>
    /// 定漂模式：自动给所有座位赋值飘分
    /// </summary>
    private void AutoSetFixedPiaoForAllSeats(int piaoValue)
    {
        GF.LogInfo($"[飘分] 定漂模式，自动设置所有座位飘分: {piaoValue}");
        
        // 给每个座位更新飘分显示
        for (int i = 0; i < 4; i++)
        {
            var seat = GetSeat(i);
            if (seat != null)
            {
                seat.UpdatePiaoText(piaoValue);
            }
        }
    }

    /// <summary>
    /// 隐藏飘分面板
    /// </summary>
    private void HidePiaoPanel()
    {
        PiaoPanel?.SetActive(false);
    }

    /// <summary>
    /// 飘分按钮点击处理 (从OnButtonClick调用)
    /// </summary>
    public void OnPiaoButtonClick(int piaoValue)
    {
        // 发送飘分请求
        mj_procedure?.Send_PiaoRq(piaoValue);
        GF.LogInfo($"玩家选择飘分: {piaoValue}");

        // 隐藏飘分面板
        HidePiaoPanel();
    }

    /// <summary>
    /// 处理同步飘分消息
    /// </summary>
    public void HandleSynPiao(Msg_SynPiaoRs msg)
    {
        if (msg == null) return;

        // 通过PlayerId查找座位索引
        int seatIdx = baseMahjongGameManager?.GetSeatIndexByPlayerId(msg.PlayerId) ?? -1;
        if (seatIdx < 0)
        {
            GF.LogWarning($"未找到玩家{msg.PlayerId}的座位");
            return;
        }

        // 更新座位的飘分显示
        var seat = GetSeat(seatIdx);
        if (seat != null)
        {
            seat.UpdatePiaoText(msg.Piao);
            GF.LogInfo($"玩家{msg.PlayerId}(座位{seatIdx})飘分: {msg.Piao}");
        }
    }

    #endregion

    #region 骰子动画逻辑

    /// <summary>
    /// 播放骰子动画（发牌前调用，仅卡五星模式）
    /// </summary>
    public async Cysharp.Threading.Tasks.UniTask PlayTouZiAnimation()
    {
        if (TouZiAniPanel == null)
        {
            GF.LogWarning("TouZiAniPanel 为空，跳过骰子动画");
            return;
        }

        // 显示骰子动画面板
        TouZiAniPanel.SetActive(true);

        // 隐藏结果图片，显示Spine动画
        TouZiResultImage?.gameObject.SetActive(false);

        // 获取骰子点数并开始异步加载图片（在动画播放期间加载）
        // 骰子点数是 dice.key 值
        int diceValue = baseMahjongGameManager?.GetDiceKey() ?? 1;
        diceValue = Mathf.Clamp(diceValue, 1, 6);

        // 异步加载骰子结果图片
        TouZiResultImage.SetSprite($"MJGame/KWX/touzi_{diceValue}.png");

        // 播放骰子Spine动画
        TouZiSpineAni?.gameObject.SetActive(true);

        // 播放saizi动画
        var trackEntry = TouZiSpineAni?.state.SetAnimation(0, "saizi", false);

        if (trackEntry != null)
        {
            // 等待动画播放完成
            float animDuration = trackEntry.Animation.Duration;
            await Cysharp.Threading.Tasks.UniTask.Delay((int)(animDuration * 1000));
        }
        else
        {
            // 如果没有获取到动画，等待默认时间
            await Cysharp.Threading.Tasks.UniTask.Delay(1000);
        }

        // 隐藏Spine动画
        TouZiSpineAni?.gameObject.SetActive(false);
        TouZiResultImage?.gameObject.SetActive(true);

        // 显示一段时间后隐藏
        await Cysharp.Threading.Tasks.UniTask.Delay(1500);

        // 隐藏骰子动画面板
        HideTouZiPanel();

        GF.LogInfo("骰子动画播放完成");
    }

    /// <summary>
    /// 隐藏骰子动画面板
    /// </summary>
    public void HideTouZiPanel()
    {
        TouZiAniPanel?.SetActive(false);
        TouZiSpineAni?.gameObject.SetActive(false);
        TouZiResultImage?.gameObject.SetActive(false);
    }

    #endregion

    #region 换牌逻辑

    /// <summary>
    /// 显示卡五星换牌UI（发牌完成后调用）
    /// 同时显示：HuanPaiAniPanel + XuanPaiPanel + 选牌中状态
    /// </summary>
    public void ShowKWXChangeCardUI()
    {
        // 0. 设置换牌状态，使手牌可以被选择
        if (mj_procedure != null && mj_procedure.enterMJDeskRs != null)
        {
            mj_procedure.enterMJDeskRs.CkState = NetMsg.MJState.ChangeCard;
            GF.LogInfo("[KWX] 设置换牌状态: ChangeCard");
        }

        // 1. 显示换牌动画面板
        HuanPaiAniPanel?.SetActive(true);

        // 2. 显示选牌中状态(只显示有玩家的座位)
        for (int i = 0; i < XuanPaiZhongs.Count; i++)
        {
            if (baseMahjongGameManager != null &&
                i < baseMahjongGameManager.deskPlayers.Length &&
                baseMahjongGameManager.deskPlayers[i] != null)
            {
                if (XuanPaiZhongs[i] != null)
                    XuanPaiZhongs[i].SetActive(true);
                if (XuanPaiEnds[i] != null)
                    XuanPaiEnds[i].SetActive(false);
            }
        }

        // 3. 显示选牌面板
        ShowXuanPaiPanel();

        // 4. 显示换牌方向按钮
        ShowHuanPaiInfoButton();

        GF.LogInfo("[KWX] 发牌完成，显示换牌UI（HuanPaiAniPanel + XuanPaiPanel + 选牌中状态）");
    }

    /// <summary>
    /// 显示换牌方向按钮，根据骰子点数设置对应图片
    /// 顺时针：btn_shunshizhen
    /// 逆时针：btn_nishizhen
    /// 对家换：btn_duijia
    /// </summary>
    public void ShowHuanPaiInfoButton()
    {
        // 检查是否开启换三张（使用 Rule 层统一属性）
        if (!HasChangeCard)
        {
            GF.LogInfo("[KWX] 换三张未开启，不显示换牌方向按钮");
            return;
        }

        BtnHuanPaiInfo.gameObject.SetActive(true);

        // 获取换牌方向类型
        int animationType = CalculateChangeCardAnimationType();

        // 根据换牌方向设置按钮图片
        string spriteName = "btn_shunshizhen";
        switch (animationType)
        {
            case 0: // 顺时针
                spriteName = "btn_shunshizhen";
                break;
            case 1: // 逆时针
                spriteName = "btn_nishizhen";
                break;
            case 2: // 对家换
                spriteName = "btn_duijia";
                break;
        }

        BtnHuanPaiInfo.image.SetSprite($"MJGame/KWX/exchangeLayer/{spriteName}.png");
    }

    /// <summary>
    /// 隐藏换牌方向按钮
    /// </summary>
    private void HideHuanPaiInfoButton()
    {
        BtnHuanPaiInfo.gameObject.SetActive(false);
    }

    /// <summary>
    /// 显示选牌面板（发牌完成后调用，让玩家可以选牌）
    /// </summary>
    public void ShowXuanPaiPanel()
    {
        XuanPaiPanel.SetActive(true);
        selectedChangeCards.Clear();

        // 确保确认按钮默认显示
        BtnXuanPai.gameObject.SetActive(true);

        // 初始时禁用确认按钮
        UpdateChangeCardButton();

        // 启用本地玩家手牌选择功能
        EnableChangeCardSelection();

        // 智能提起三张推荐换牌
        AutoSelectSuggestedChangeCards();

        GF.LogInfo("显示选牌面板，玩家可以开始选择换牌");
    }

    /// <summary>
    /// 智能选择三张推荐换牌（自动提起）
    /// 策略：选择最少的花色中数量最少、牌值最孤立的三张牌
    /// </summary>
    private void AutoSelectSuggestedChangeCards()
    {
        // 重连时检查：如果自己已经提交了选牌，则不再自动抬起三张
        if (mj_procedure?.enterMJDeskRs?.ChangeCardPlayer != null)
        {
            long myPlayerId = Util.GetMyselfInfo().PlayerId;
            if (mj_procedure.enterMJDeskRs.ChangeCardPlayer.Contains(myPlayerId))
            {
                GF.LogInfo("[KWX] 自己已完成选牌，跳过智能选牌");
                return;
            }
        }

        var seat = GetSeat(0);
        if (seat == null || seat.HandContainer == null) return;

        // 收集所有手牌信息（包括手牌区和摸牌区）
        List<(MjCard_2D_Interaction card, int value, int suit)> allCards = new List<(MjCard_2D_Interaction, int, int)>();
        
        // 从手牌区收集
        for (int i = 0; i < seat.HandContainer.childCount; i++)
        {
            Transform cardTrans = seat.HandContainer.GetChild(i);
            MjCard_2D_Interaction interaction = cardTrans.GetComponent<MjCard_2D_Interaction>();
            if (interaction != null && interaction.cardValue > 0)
            {
                int suit = interaction.cardValue / 10; // 花色：0=万, 1=条, 2=筒
                allCards.Add((interaction, interaction.cardValue, suit));
            }
        }

        // 从摸牌区收集
        if (seat.MoPaiContainer != null)
        {
            for (int i = 0; i < seat.MoPaiContainer.childCount; i++)
            {
                Transform cardTrans = seat.MoPaiContainer.GetChild(i);
                MjCard_2D_Interaction interaction = cardTrans.GetComponent<MjCard_2D_Interaction>();
                if (interaction != null && interaction.cardValue > 0)
                {
                    int suit = interaction.cardValue / 10; // 花色：0=万, 1=条, 2=筒
                    allCards.Add((interaction, interaction.cardValue, suit));
                }
            }
        }

        if (allCards.Count < CHANGE_CARD_COUNT)
        {
            GF.LogWarning($"[KWX] 手牌数量不足{CHANGE_CARD_COUNT}张，无法自动选牌");
            return;
        }

        // 统计每个花色的牌数
        var suitCounts = allCards.GroupBy(c => c.suit)
                                  .Select(g => new { Suit = g.Key, Count = g.Count(), Cards = g.ToList() })
                                  .OrderBy(s => s.Count)  // 按数量升序
                                  .ToList();

        // 找到数量最少且>=3的花色
        var selectedSuit = suitCounts.FirstOrDefault(s => s.Count >= CHANGE_CARD_COUNT);
        if (selectedSuit == null)
        {
            GF.LogInfo("[KWX] 没有找到数量>=3的花色，无法自动选牌");
            return;
        }

        // 从选中花色中选择最孤立的三张牌（优先选择边张、孤张）
        var suitCards = selectedSuit.Cards.OrderBy(c => c.value).ToList();
        
        // 计算每张牌的"孤立度"（周围相邻牌越少，孤立度越高）
        List<(MjCard_2D_Interaction card, int isolation)> cardIsolations = new List<(MjCard_2D_Interaction, int)>();
        
        foreach (var cardInfo in suitCards)
        {
            int value = cardInfo.value;
            int isolation = 0;
            
            // 检查是否有相邻牌（同花色内）
            bool hasLeft = suitCards.Any(c => c.value == value - 1);
            bool hasRight = suitCards.Any(c => c.value == value + 1);
            
            if (!hasLeft) isolation += 2;  // 左边没有相邻牌
            if (!hasRight) isolation += 2; // 右边没有相邻牌
            
            // 边张（1、9）额外加分
            int rank = value % 10;
            if (rank == 1 || rank == 9) isolation += 1;
            
            cardIsolations.Add((cardInfo.card, isolation));
        }

        // 按孤立度降序排列，取前三张
        var suggestedCards = cardIsolations
            .OrderByDescending(c => c.isolation)
            .Take(CHANGE_CARD_COUNT)
            .Select(c => c.card)
            .ToList();

        // 提起选中的牌
        foreach (var card in suggestedCards)
        {
            card.RaiseCardProgrammatically();
            selectedChangeCards.Add(card);
            GF.LogInfo($"[KWX] 智能选牌: {card.cardValue}");
        }

        // 更新按钮状态
        UpdateChangeCardButton();

        GF.LogInfo($"[KWX] 智能选牌完成，选中{selectedChangeCards.Count}张牌，花色={selectedSuit.Suit}");
    }

    /// <summary>
    /// 隐藏换牌面板（只隐藏选牌面板，动画面板在动画完成前不隐藏）
    /// </summary>
    private void HideChangeCardPanel()
    {
        XuanPaiPanel.SetActive(false);
        selectedChangeCards.Clear();
    }

    /// <summary>
    /// 隐藏换牌操作面板（但保持换牌动画面板显示）
    /// 用于自己已完成换牌但仍需要看到换牌动画的情况
    /// </summary>
    public void HideChangeCardOperationPanel()
    {
        // 隐藏选牌面板（XuanPaiPanel），因为自己已经选好了
        XuanPaiPanel?.SetActive(false);

        // 清空选择状态（禁用操作）
        selectedChangeCards.Clear();

        // 换牌动画面板（HuanPaiAniPanel）保持显示，直到动画播放完成

        GF.LogInfo("隐藏换牌选牌面板（自己已完成换牌，但保持动画面板显示）");
    }

    /// <summary>
    /// 启用换牌选择功能
    /// </summary>
    private void EnableChangeCardSelection()
    {
        // 获取本地玩家座位(座位0)
        var seat = GetSeat(0);
        if (seat == null) return;

        // 为手牌添加换牌选择回调
        // 注意: 实际实现需要在MahjongSeat_2D中添加换牌选择模式
        GF.LogInfo("启用换牌选择模式");
    }

    /// <summary>
    /// 手牌点击选择换牌 (由MahjongSeat_2D回调)
    /// </summary>
    public void OnChangeCardSelect(MjCard_2D_Interaction card)
    {
        // 检查是否已选择
        if (selectedChangeCards.Contains(card))
        {
            // 取消选择
            selectedChangeCards.Remove(card);
            GF.LogInfo($"取消选择换牌: {card.cardValue}");
        }
        else
        {
            // 检查是否达到上限
            if (selectedChangeCards.Count >= CHANGE_CARD_COUNT)
            {
                GF.UI.ShowToast("最多只能选择3张牌!");
                card.RestorePosition(true);
                return;
            }

            // 检查是否同花色
            if (selectedChangeCards.Count > 0)
            {
                if (!IsSameSuit(selectedChangeCards[0].cardValue, card.cardValue))
                {
                    GF.UI.ShowToast("请选择相同花色的牌!");
                    card.RestorePosition(true);
                    return;
                }
            }

            selectedChangeCards.Add(card);
            GF.LogInfo($"选择换牌: {card.cardValue}, 已选择{selectedChangeCards.Count}张");
        }

        // 更新确认按钮状态
        UpdateChangeCardButton();
    }

    /// <summary>
    /// 更新换牌确认按钮状态
    /// </summary>
    private void UpdateChangeCardButton()
    {
        if (BtnXuanPai != null)
            BtnXuanPai.interactable = selectedChangeCards.Count == CHANGE_CARD_COUNT;
    }

    /// <summary>
    /// 判断两张牌是否同花色
    /// </summary>
    private bool IsSameSuit(int card1, int card2)
    {
        // 获取花色 (万条筒)
        // 假设牌值编码: 1-9万, 11-19条, 21-29筒
        int suit1 = card1 / 10;
        int suit2 = card2 / 10;
        return suit1 == suit2;
    }

    /// <summary>
    /// 确认换牌按钮点击 (从OnButtonClick调用)
    /// </summary>
    public void OnConfirmChangeCard()
    {
        if (selectedChangeCards.Count != CHANGE_CARD_COUNT)
        {
            GF.UI.ShowToast($"请选择{CHANGE_CARD_COUNT}张牌");
            return;
        }

        // 再次验证是否同花色
        if (!ValidateChangeCards())
        {
            GF.UI.ShowToast("请选择相同花色的牌!");
            return;
        }

        // 发送换牌请求
        var values = selectedChangeCards.Select(c => c.cardValue).ToList();

        // 保存换出的牌值和原手牌（用于识别新换入的牌）
        sentChangeCardValues = new List<int>(values);
        var seat0 = GetSeat(0);
        if (seat0 != null)
        {
            oldHandCardsBeforeChange = seat0.GetHandCardsData().Select(v => (int)v).ToList();
        }

        mj_procedure?.Send_ChangeCardRq(values);

        // 清空选择状态
        selectedChangeCards.Clear();

        GF.LogInfo($"发送换牌请求: {string.Join(",", values)}");
    }

    /// <summary>
    /// 验证选中的换牌是否合规
    /// </summary>
    private bool ValidateChangeCards()
    {
        if (selectedChangeCards.Count != CHANGE_CARD_COUNT)
            return false;

        // 验证是否同花色
        int firstSuit = selectedChangeCards[0].cardValue / 10;
        for (int i = 1; i < selectedChangeCards.Count; i++)
        {
            int suit = selectedChangeCards[i].cardValue / 10;
            if (suit != firstSuit)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 处理换牌响应
    /// </summary>
    public void HandleChangeCardRs(Msg_ChangeCardRs msg)
    {
        if (msg == null) return;

        GF.LogInfo($"收到换牌响应, 换出的牌: {string.Join(",", msg.Card)}, 新手牌: {string.Join(",", msg.HandCard)}");
        HideChangeCardPanel();

        // 直接使用服务器返回的完整手牌刷新(以服务器为主)
        var seat = GetSeat(0);
        if (seat != null && msg.HandCard != null)
        {
            // 清除摸牌区
            seat.ClearContainer(seat.MoPaiContainer);

            var newHandCards = msg.HandCard.ToList();
            newHandCards.Sort();
            seat.CreateHandCards(newHandCards);
            GF.LogInfo("根据服务器返回刷新手牌");
        }

        // 显示自己的换牌完成状态
        if (XuanPaiEnds != null && XuanPaiEnds.Count > 0)
        {
            // 座位0是自己，显示第一个完成标记
            if (XuanPaiZhongs[0] != null)
                XuanPaiZhongs[0].SetActive(false);
            if (XuanPaiEnds[0] != null)
                XuanPaiEnds[0].SetActive(true);
        }
    }

    /// <summary>
    /// 处理其他玩家换牌完成的同步消息
    /// </summary>
    public void HandleSynChangeCard(Msg_SynChangeCardRs msg)
    {
        if (msg == null) return;

        GF.LogInfo($"收到其他玩家换牌完成消息, PlayerId: {msg.PlayerId}");

        // 将 PlayerId 转换为座位索引
        int seatIdx = baseMahjongGameManager?.GetSeatIndexByPlayerId(msg.PlayerId) ?? -1;
        if (seatIdx < 0 || seatIdx >= XuanPaiEnds.Count)
        {
            GF.LogError($"无效的座位索引: {seatIdx}, PlayerId: {msg.PlayerId}");
            return;
        }

        // 更新该玩家的换牌完成状态
        UpdateChangeCardPlayerState(seatIdx, true);

        // 其他玩家（座位1-3）隐藏3张手牌
        if (seatIdx > 0)
        {
            var seat = GetSeat(seatIdx);
            if (seat != null)
            {
                // 隐藏其他玩家的前3张手牌
                HideCardsForOtherPlayer(seat, CHANGE_CARD_COUNT);
                GF.LogInfo($"其他玩家(座位{seatIdx})隐藏{CHANGE_CARD_COUNT}张手牌");
            }
        }
    }

    /// <summary>
    /// 更新玩家的换牌状态（用于重连恢复）
    /// </summary>
    /// <param name="seatIdx">座位索引</param>
    /// <param name="isFinished">是否完成换牌</param>
    public void UpdateChangeCardPlayerState(int seatIdx, bool isFinished)
    {
        if (seatIdx < 0 || seatIdx >= XuanPaiEnds.Count)
        {
            GF.LogWarning($"无效的座位索引: {seatIdx}");
            return;
        }

        // 显示或隐藏换牌状态
        if (XuanPaiZhongs[seatIdx] != null)
            XuanPaiZhongs[seatIdx].SetActive(!isFinished);
        if (XuanPaiEnds[seatIdx] != null)
            XuanPaiEnds[seatIdx].SetActive(isFinished);

        GF.LogInfo($"更新玩家换牌状态: 座位{seatIdx}, 完成状态: {isFinished}");
    }

    /// <summary>
    /// 处理同步手牌消息（换牌结束）
    /// </summary>
    public void HandleSynHandCard(Msg_SynHandCard msg)
    {
        if (msg == null) return;

        GF.LogInfo($"收到同步手牌消息, 新手牌: {string.Join(",", msg.HandCard)}");

        // 播放换牌动画效果（动画播放完成后会刷新手牌）
        PlayChangeCardAnimation(msg.HandCard.ToList());
    }

    /// <summary>
    /// 播放换牌动画
    /// </summary>
    private void PlayChangeCardAnimation(List<int> newHandCards)
    {
        // 显示所有在座玩家的换牌完成状态
        for (int i = 0; i < XuanPaiEnds.Count; i++)
        {
            // 检查该座位是否有玩家
            if (baseMahjongGameManager != null &&
                i < baseMahjongGameManager.deskPlayers.Length &&
                baseMahjongGameManager.deskPlayers[i] != null)
            {
                if (XuanPaiZhongs[i] != null)
                    XuanPaiZhongs[i].SetActive(false);
                if (XuanPaiEnds[i] != null)
                    XuanPaiEnds[i].SetActive(true);
            }
        }

        // 播放换牌动画
        // 根据骰子key和房间人数计算换牌方向
        int animationType = CalculateChangeCardAnimationType();
        GF.LogInfo($"换牌方向: animationType={animationType} (0=顺时针, 1=逆时针, 2=对家换)");

        // 播放 XuanPaiEnds 移动到下一个位置的动画
        PlayXuanPaiEndsMoveAnimation(animationType, newHandCards);

        // 播放换牌 Spine 动画
        if (HuanPaiAnis != null && HuanPaiAnis.Count > animationType && HuanPaiAnis[animationType] != null)
        {
            var animation = HuanPaiAnis[animationType];
            if (animation != null && animation.AnimationState != null)
            {
                // 根据类型播放对应动画
                string animationName = "";
                switch (animationType)
                {
                    case 0:
                        animationName = "shunshizhen";
                        break;
                    case 1:
                        animationName = "nishizhen";
                        break;
                    case 2:
                        animationName = "duijiahuan";
                        break;
                }

                animation.gameObject.SetActive(true);
                animation.AnimationState.SetAnimation(0, animationName, true);

                GF.LogInfo($"播放换牌 Spine 动画: {animationName}");
            }
        }
    }

    /// <summary>
    /// 播放 XuanPaiEnds 移动到下一个位置的动画
    /// </summary>
    private void PlayXuanPaiEndsMoveAnimation(int animationType, List<int> newHandCards)
    {
        // 根据换牌方向计算下一个位置
        // 0=顺时针: 0->1, 1->2, 2->3, 3->0
        // 1=逆时针: 0->3, 1->0, 2->1, 3->2
        // 2=对家换: 0->2, 1->3, 2->0, 3->1

        List<GameObject> sourceEnds = new List<GameObject>();
        List<int> sourceIndices = new List<int>();
        List<int> targetIndices = new List<int>();

        // 收集需要移动的 XuanPaiEnds 和目标位置
        for (int i = 0; i < XuanPaiEnds.Count; i++)
        {
            // 检查该座位是否有玩家
            if (baseMahjongGameManager != null &&
                i < baseMahjongGameManager.deskPlayers.Length &&
                baseMahjongGameManager.deskPlayers[i] != null)
            {
                if (XuanPaiEnds[i] != null && XuanPaiEnds[i].activeSelf)
                {
                    sourceEnds.Add(XuanPaiEnds[i]);
                    sourceIndices.Add(i);
                    int nextIndex = GetNextChangeCardSeatIndex(i, animationType);
                    targetIndices.Add(nextIndex);
                }
            }
        }

        if (sourceEnds.Count == 0)
        {
            // 没有需要移动的，直接完成
            OnXuanPaiEndsMoveComplete(newHandCards);
            return;
        }

        // 保存每个 XuanPaiEnds 的原始位置，用于动画完成后还原
        List<Vector3> originalPositions = new List<Vector3>();
        for (int i = 0; i < sourceEnds.Count; i++)
        {
            originalPositions.Add(sourceEnds[i].transform.position);
        }

        // 保存目标位置的原始位置
        List<Vector3> targetPositions = new List<Vector3>();
        for (int i = 0; i < targetIndices.Count; i++)
        {
            int targetIdx = targetIndices[i];
            if (targetIdx >= 0 && targetIdx < XuanPaiEnds.Count && XuanPaiEnds[targetIdx] != null)
            {
                targetPositions.Add(XuanPaiEnds[targetIdx].transform.position);
            }
            else
            {
                // 如果目标位置不存在，使用当前位置（不应该发生）
                targetPositions.Add(sourceEnds[i].transform.position);
            }
        }

        // 播放移动动画
        float moveDuration = 1.5f; // 移动动画时长
        int completedCount = 0;
        int totalCount = sourceEnds.Count;

        // 保存原始位置和源对象的映射，用于动画完成后还原
        Dictionary<GameObject, Vector3> originalPosMap = new Dictionary<GameObject, Vector3>();
        for (int i = 0; i < sourceEnds.Count; i++)
        {
            originalPosMap[sourceEnds[i]] = originalPositions[i];
        }

        for (int i = 0; i < sourceEnds.Count; i++)
        {
            GameObject sourceEnd = sourceEnds[i];
            Vector3 targetPos = targetPositions[i];

            // 使用 DOTween 播放移动动画
            sourceEnd.transform.DOMove(targetPos, moveDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() =>
                {
                    completedCount++;
                    if (completedCount >= totalCount)
                    {
                        // 所有动画完成后，还原所有 XuanPaiEnds 的坐标
                        foreach (var kvp in originalPosMap)
                        {
                            if (kvp.Key != null)
                            {
                                kvp.Key.transform.position = kvp.Value;
                            }
                        }

                        GF.LogInfo("XuanPaiEnds 移动动画完成，已还原所有坐标");

                        // 然后恢复手牌
                        OnXuanPaiEndsMoveComplete(newHandCards);
                    }
                });
        }

        GF.LogInfo($"播放 XuanPaiEnds 移动动画: {sourceEnds.Count} 个移动到下一个位置");
    }

    /// <summary>
    /// 根据骰子key和房间人数计算换牌方向
    /// 规则：
    /// - 两人房：默认对家换 (animationType = 2)
    /// - 四人房：使用血流规则：1-2=顺时针(0), 3-4=对家换(2), 5-6=逆时针(1)
    /// - 三人房：使用卡五星规则：key单数=顺时针(0), key双数=逆时针(1)
    /// </summary>
    private int CalculateChangeCardAnimationType()
    {
        if (baseMahjongGameManager == null)
        {
            GF.LogWarning("CalculateChangeCardAnimationType: baseMahjongGameManager为空，默认顺时针");
            return 0;
        }

        // 获取实际玩家数量
        int playerCount = baseMahjongGameManager.realPlayerCount;

        // 两人房：默认对家换
        if (playerCount == 2)
        {
            GF.LogInfo($"换牌方向: 两人房，对家换 (playerCount={playerCount})");
            return 2; // 对家换
        }

        // 三人房及以上：根据骰子key判断
        int diceKey = Mathf.Clamp(baseMahjongGameManager.GetDiceKey(), 1, 6);

        // 4人房：使用血流换牌规则（与卡五星不同）
        if (playerCount == 4)
        {
            if (diceKey <= 2)
            {
                GF.LogInfo($"[4人/血流规则] 换牌方向: 骰子key={diceKey}(1-2)，顺时针 (playerCount={playerCount})");
                return 0;
            }

            if (diceKey <= 4)
            {
                GF.LogInfo($"[4人/血流规则] 换牌方向: 骰子key={diceKey}(3-4)，对家换 (playerCount={playerCount})");
                return 2;
            }

            GF.LogInfo($"[4人/血流规则] 换牌方向: 骰子key={diceKey}(5-6)，逆时针 (playerCount={playerCount})");
            return 1;
        }

        // 其他玩法：key单数=顺时针, key双数=逆时针
        if (diceKey % 2 == 1)
        {
            GF.LogInfo($"换牌方向: 骰子key={diceKey}(单数)，顺时针 (playerCount={playerCount})");
            return 0;
        }

        GF.LogInfo($"换牌方向: 骰子key={diceKey}(双数)，逆时针 (playerCount={playerCount})");
        return 1;
    }

    /// <summary>
    /// 根据换牌方向获取下一个有玩家的座位索引
    /// 2人房：0号和2号有人
    /// 3人房：0号、1号、3号有人
    /// 4人房：0号、1号、2号、3号都有人
    /// </summary>
    private int GetNextChangeCardSeatIndex(int currentIndex, int animationType)
    {
        if (baseMahjongGameManager == null || baseMahjongGameManager.deskPlayers == null)
            return currentIndex;

        int nextIndex = currentIndex;
        int attempts = 0; // 防止死循环
        const int maxAttempts = 4; // 最多尝试4次

        do
        {
            // 根据动画类型计算下一个索引
            switch (animationType)
            {
                case 0: // 顺时针
                    nextIndex = (nextIndex + 1) % 4;
                    break;
                case 1: // 逆时针
                    nextIndex = (nextIndex - 1 + 4) % 4;
                    break;
                case 2: // 对家换
                    nextIndex = (nextIndex + 2) % 4;
                    break;
                default:
                    nextIndex = (nextIndex + 1) % 4; // 默认顺时针
                    break;
            }

            attempts++;

            // 如果找到有玩家的座位，返回
            if (nextIndex < baseMahjongGameManager.deskPlayers.Length &&
                baseMahjongGameManager.deskPlayers[nextIndex] != null)
            {
                return nextIndex;
            }

        } while (attempts < maxAttempts);

        // 如果循环结束还没找到，返回当前索引（不应该发生）
        GF.LogWarning($"GetNextChangeCardSeatIndex: 未找到有效的下一个座位，当前索引={currentIndex}, 动画类型={animationType}");
        return currentIndex;
    }

    /// <summary>
    /// XuanPaiEnds 移动动画完成回调
    /// </summary>
    private void OnXuanPaiEndsMoveComplete(List<int> newHandCards)
    {
        GF.LogInfo("XuanPaiEnds 移动动画完成，准备恢复手牌");

        // 隐藏所有 XuanPaiEnds（移动动画已完成）
        for (int i = 0; i < XuanPaiEnds.Count; i++)
        {
            if (XuanPaiEnds[i] != null)
            {
                XuanPaiEnds[i].SetActive(false);
            }
        }

        // 隐藏换牌面板和动画面板
        HideChangeCardPanel();
        HuanPaiAniPanel.SetActive(false);

        // 隐藏换牌 Spine 动画
        if (HuanPaiAnis != null && HuanPaiAnis.Count > 0)
        {
            foreach (var anim in HuanPaiAnis)
            {
                if (anim != null)
                {
                    anim.gameObject.SetActive(false);
                }
            }
        }

        // 恢复手牌
        RefreshHandCardsAfterChange(newHandCards);
    }

    /// <summary>
    /// 动画播放完成后刷新手牌
    /// </summary>
    private void RefreshHandCardsAfterChange(List<int> newHandCards)
    {
        // 刷新自己（座位0）的手牌
        var seat0 = GetSeat(0);
        if (seat0 != null)
        {
            // 服务器返回的手牌中，最后3张就是新换进来的牌
            List<int> newInCards = new List<int>();
            if (newHandCards.Count >= CHANGE_CARD_COUNT)
            {
                // 取最后3张作为新换入的牌
                newInCards = newHandCards.Skip(newHandCards.Count - CHANGE_CARD_COUNT).Take(CHANGE_CARD_COUNT).ToList();
            }
            
            GF.LogInfo($"换牌结束: 新手牌数量={newHandCards.Count}, 新手牌={string.Join(",", newHandCards)}, 新换入牌(最后3张)={string.Join(",", newInCards)}");

            // 排序手牌
            newHandCards.Sort();

            // 如果是14张牌（庄家情况），将最大的牌移到摸牌区
            if (newHandCards.Count == 14)
            {
                // 取出最大的牌（排序后最后一张）放到摸牌区
                int maxCard = newHandCards[newHandCards.Count - 1];
                List<int> handCardsWithoutMax = newHandCards.Take(13).ToList();
                
                // 创建13张手牌
                seat0.CreateHandCards(handCardsWithoutMax);
                // 将最大的牌放到摸牌区
                seat0.AddMoPai(maxCard);
                
                GF.LogInfo($"14张牌处理: 最大牌{maxCard}移到摸牌区, 剩余13张手牌={string.Join(",", handCardsWithoutMax)}");
            }
            else
            {
                // 13张牌正常创建
                seat0.CreateHandCards(newHandCards);
            }

            // 为新换入的牌添加从上往下的动画
            if (newInCards.Count > 0)
            {
                PlayNewCardsDropAnimation(seat0, newInCards);
                GF.LogInfo($"播放新换入牌动画: {string.Join(",", newInCards)}");
            }
        }

        // 清空换牌记录
        sentChangeCardValues.Clear();
        oldHandCardsBeforeChange.Clear();

        // 为其他玩家（座位1-3）显示回隐藏的3张手牌
        for (int i = 1; i < 4; i++)
        {
            // 检查该座位是否有玩家
            if (baseMahjongGameManager != null &&
                i < baseMahjongGameManager.deskPlayers.Length &&
                baseMahjongGameManager.deskPlayers[i] != null)
            {
                var seat = GetSeat(i);
                if (seat != null)
                {
                    // 显示回其他玩家的前3张手牌
                    ShowCardsForOtherPlayer(seat, CHANGE_CARD_COUNT);
                    GF.LogInfo($"其他玩家(座位{i})显示回{CHANGE_CARD_COUNT}张手牌");
                }
            }
        }

        // 换牌流程结束，根据规则决定下一步
        OnChangeCardComplete();
    }

    /// <summary>
    /// 换牌完成后的处理
    /// 血流麻将：显示甩牌界面（如果开启了甩牌）
    /// 卡五星：启动庄家打牌倒计时
    /// </summary>
    private void OnChangeCardComplete()
    {
        // 血流麻将：换牌完成后进入甩牌阶段（如果房间开启了甩牌）
        // 使用 Rule 层统一的 HasThrowCard 属性
        if (IsXueLiuRule && HasThrowCard)
        {
            ShowThrowCardUI();
        }
        else
        {
            // 卡五星等：启动庄家打牌倒计时
            StartBankerPlayCountdown();
        }
    }

    /// <summary>
    /// 启动庄家打牌倒计时（换牌流程结束后调用）
    /// </summary>
    private void StartBankerPlayCountdown()
    {
        // 获取庄家座位
        int bankerSeat = baseMahjongGameManager?.CurrentTurnSeat ?? -1;
        if (bankerSeat < 0)
        {
            GF.LogWarning("无法获取庄家座位，跳过倒计时");
            return;
        }

        // 设置当前轮次到庄家
        Set2DTurnSeat(bankerSeat);

        // 启动10秒倒计时
        const int BANKER_PLAY_COUNTDOWN = 10;
        Start2DCountdown(BANKER_PLAY_COUNTDOWN);

        // 如果自己是庄家，允许出牌
        if (bankerSeat == 0)
        {
            canPlayCard2D = true;
            CheckAndShowTingMarks();
        }
        else
        {
            CheckHuPaiBtnVisibility();
        }

        GF.LogInfo($"换牌流程结束，庄家(座位{bankerSeat})开始打牌，启动{BANKER_PLAY_COUNTDOWN}秒倒计时");
    }

    /// <summary>
    /// 计算新换入的牌值（通过对比换牌前后的差异）
    /// 这个方法能正确识别别家换过来的牌
    /// </summary>
    private List<int> CalculateNewInCardsFromDiff(List<int> oldHandCards, List<int> newHandCards)
    {
        List<int> newInCards = new List<int>();

        // 如果没有保存的旧手牌，返回空列表
        if (oldHandCards == null || oldHandCards.Count == 0)
        {
            GF.LogWarning("没有旧手牌记录，无法计算新换入的牌");
            return newInCards;
        }

        // 复制旧手牌和新手牌列表（避免修改原列表）
        List<int> tempOld = new List<int>(oldHandCards);
        List<int> tempNew = new List<int>(newHandCards);

        // 移除相同的牌（保留下来的牌）
        // 遍历旧手牌，如果在新手牌中也存在，就都移除
        for (int i = tempOld.Count - 1; i >= 0; i--)
        {
            int cardValue = tempOld[i];
            if (tempNew.Contains(cardValue))
            {
                tempOld.RemoveAt(i);      // 从旧手牌移除
                tempNew.Remove(cardValue); // 从新手牌移除第一个匹配的
            }
        }

        // tempOld 中剩余的是换出的牌
        // tempNew 中剩余的是新换入的牌（别家换过来的3张）
        newInCards.AddRange(tempNew);

        GF.LogInfo($"计算新换入的牌(通过差异对比): 换出={string.Join(",", tempOld)}, 新换入={string.Join(",", newInCards)}");
        
        if (newInCards.Count != CHANGE_CARD_COUNT)
        {
            GF.LogWarning($"新换入牌数量异常: 预期{CHANGE_CARD_COUNT}张，实际{newInCards.Count}张");
        }

        return newInCards;
    }

    /// <summary>
    /// 计算新换入的牌值（旧方法，保留用于兼容）
    /// </summary>
    private List<int> CalculateNewInCards(List<int> newHandCards)
    {
        List<int> newInCards = new List<int>();

        // 如果没有保存的换牌记录，返回空列表
        if (sentChangeCardValues.Count == 0 || oldHandCardsBeforeChange.Count == 0)
        {
            GF.LogInfo("没有换牌记录，无法计算新换入的牌");
            return newInCards;
        }

        // 计算换牌后应该保留的牌（原手牌 - 换出的牌）
        List<int> remainingCards = new List<int>(oldHandCardsBeforeChange);
        foreach (int cardValue in sentChangeCardValues)
        {
            remainingCards.Remove(cardValue);  // 移除换出的牌
        }

        // 新换入的牌 = 新手牌 - 保留的牌
        List<int> tempNewHand = new List<int>(newHandCards);
        foreach (int cardValue in remainingCards)
        {
            tempNewHand.Remove(cardValue);  // 移除保留的牌
        }

        // tempNewHand 中剩余的就是新换入的牌
        newInCards.AddRange(tempNewHand);

        GF.LogInfo($"计算新换入的牌: 换出={string.Join(",", sentChangeCardValues)}, 新换入={string.Join(",", newInCards)}");
        return newInCards;
    }

    /// <summary>
    /// 播放新换入牌从上往下掉落的动画
    /// </summary>
    private void PlayNewCardsDropAnimation(MahjongSeat_2D seat, List<int> newInCardValues)
    {
        if (seat == null || seat.HandContainer == null || newInCardValues.Count == 0)
        {
            GF.LogWarning($"PlayNewCardsDropAnimation: 参数无效 - seat={seat != null}, HandContainer={seat?.HandContainer != null}, newInCardValues.Count={newInCardValues?.Count ?? 0}");
            return;
        }

        float dropOffset = 100f;  // 从上方多少像素处开始
        float dropDuration = 0.5f;  // 动画持续时间

        // 复制一份要查找的牌值列表（避免修改原列表影响调试）
        List<int> cardsToFind = new List<int>(newInCardValues);
        
        GF.LogInfo($"PlayNewCardsDropAnimation: 开始查找新增牌 - 待查找={string.Join(",", cardsToFind)}, 手牌容器子对象数={seat.HandContainer.childCount}");

        int animatedCount = 0;
        
        // 遍历手牌容器，找到新换入的牌并播放动画
        for (int i = 0; i < seat.HandContainer.childCount; i++)
        {
            Transform cardTrans = seat.HandContainer.GetChild(i);
            if (cardTrans == null) continue;

            MjCard_2D mjCard = cardTrans.GetComponent<MjCard_2D>();
            if (mjCard == null) continue;

            int cardValue = mjCard.cardValue;

            // 检查是否是新换入的牌
            if (cardsToFind.Contains(cardValue))
            {
                // 移除一个匹配的值（处理多张相同牌的情况）
                cardsToFind.Remove(cardValue);

                // 获取目标位置
                RectTransform rectTrans = cardTrans.GetComponent<RectTransform>();
                if (rectTrans != null)
                {
                    Vector2 targetPos = rectTrans.anchoredPosition;

                    // 设置初始位置（在目标位置上方）
                    rectTrans.anchoredPosition = new Vector2(targetPos.x, targetPos.y + dropOffset);

                    // 播放从上往下的缓动动画
                    rectTrans.DOAnchorPosY(targetPos.y, dropDuration)
                        .SetEase(Ease.Linear);  // 使用平滑缓动效果，缓慢插入

                    animatedCount++;
                    GF.LogInfo($"播放新换入牌动画: 牌值={cardValue}, 位置索引={i}, 目标Y={targetPos.y}");
                }
            }
        }
        
        GF.LogInfo($"PlayNewCardsDropAnimation: 完成 - 播放动画数={animatedCount}, 未找到的牌={string.Join(",", cardsToFind)}");
    }

    /// <summary>
    /// 隐藏其他玩家前N张手牌
    /// </summary>
    private void HideCardsForOtherPlayer(MahjongSeat_2D seat, int count)
    {
        if (seat == null || seat.HandContainer == null)
            return;

        int hiddenCount = 0;
        for (int i = 0; i < count && i < seat.HandContainer.childCount; i++)
        {
            Transform cardTransform = seat.HandContainer.GetChild(i);
            if (cardTransform != null)
            {
                cardTransform.gameObject.SetActive(false);
                hiddenCount++;
            }
        }

        GF.LogInfo($"其他玩家(座位{seat.SeatIndex})隐藏{hiddenCount}张手牌");
    }

    /// <summary>
    /// 显示其他玩家前N张手牌
    /// </summary>
    private void ShowCardsForOtherPlayer(MahjongSeat_2D seat, int count)
    {
        if (seat == null || seat.HandContainer == null)
            return;

        int shownCount = 0;
        for (int i = 0; i < count && i < seat.HandContainer.childCount; i++)
        {
            Transform cardTransform = seat.HandContainer.GetChild(i);
            if (cardTransform != null && !cardTransform.gameObject.activeSelf)
            {
                cardTransform.gameObject.SetActive(true);
                shownCount++;
            }
        }

        GF.LogInfo($"其他玩家(座位{seat.SeatIndex})显示回{shownCount}张手牌");
    }

    #endregion

    #region 亮牌逻辑

    /// <summary>
    /// 显示亮牌面板（卡五星专用）
    /// </summary>
    /// <param name="tingCards">可听的牌列表（无暗铺情况下）</param>
    public void ShowLiangPaiPanel(List<int> tingCards)
    {
        if (LiangPaiPanel == null)
        {
            GF.LogError("LiangPaiPanel未配置");
            return;
        }

        tingPaiList = tingCards ?? new List<int>();

        // 显示亮牌面板
        LiangPaiPanel.SetActive(true);
        // 隐藏选暗铺牌面板
        HideXuanAnPuPaiPanel();

        BtnLiangPaiTrue?.gameObject.SetActive(false);
        BtnLiangPaiGuo?.gameObject.SetActive(false);

        GF.LogInfo($"显示亮牌面板，可听的牌: {string.Join(",", tingPaiList)}");
    }

    /// <summary>
    /// 隐藏亮牌面板
    /// </summary>
    public void HideLiangPaiPanel()
    {
        if(!IsKaWuXingRule) return;
        LiangPaiPanel?.SetActive(false);

        // 恢复手牌亮度
        RestoreHandCardsBrightness();

        // 重置按钮状态
        BtnLiangPaiTrue?.gameObject.SetActive(false);
        BtnLiangPaiGuo?.gameObject.SetActive(false);

        tingPaiList.Clear();
        selectedAnPuPai.Clear();
        selectedTingCard = -1;
    }

    /// <summary>
    /// 设置手牌变暗效果（除了可听的牌）
    /// </summary>
    private void SetHandCardsDimmed(List<int> tingCards)
    {
        var seat = GetSeat(0);
        if (seat == null || seat.HandContainer == null)
            return;

        // 遍历所有手牌
        for (int i = 0; i < seat.HandContainer.childCount; i++)
        {
            Transform cardTransform = seat.HandContainer.GetChild(i);
            var mjCardInteraction = cardTransform.GetComponent<MjCard_2D_Interaction>();
            var mjCard2D = cardTransform.GetComponent<MjCard_2D>();
            if (mjCardInteraction != null && mjCard2D != null)
            {
                // 如果这张牌不在可听列表中，设置为变暗
                bool isTingCard = tingCards.Contains(mjCardInteraction.cardValue);
                bool shouldDim = !isTingCard;

                // 使用听牌优先级遮罩（中等优先级）
                mjCard2D.SetTingPaiMark(shouldDim);
                // 变暗的牌禁用交互
                mjCardInteraction.enabled = !shouldDim;
            }
        }

        // 摸牌区也需要处理
        if (seat.MoPaiContainer != null && seat.MoPaiContainer.childCount > 0)
        {
            Transform moCardTransform = seat.MoPaiContainer.GetChild(0);
            var mjCardInteraction = moCardTransform.GetComponent<MjCard_2D_Interaction>();
            var mjCard2D = moCardTransform.GetComponent<MjCard_2D>();
            if (mjCardInteraction != null && mjCard2D != null)
            {
                bool isTingCard = tingCards.Contains(mjCardInteraction.cardValue);
                bool shouldDim = !isTingCard;

                // 使用听牌优先级遮罩（中等优先级）
                mjCard2D.SetTingPaiMark(shouldDim);
                // 变暗的牌禁用交互
                mjCardInteraction.enabled = !shouldDim;
            }
        }
    }

    /// <summary>
    /// 恢复手牌亮度（仅用于亮牌流程，不影响听牌标记）
    /// </summary>
    private void RestoreHandCardsBrightness()
    {
        var seat = GetSeat(0);
        if (seat == null)
            return;

        // 恢复手牌亮度
        if (seat.HandContainer != null)
        {
            for (int i = 0; i < seat.HandContainer.childCount; i++)
            {
                Transform cardTransform = seat.HandContainer.GetChild(i);
                var mjCardInteraction = cardTransform.GetComponent<MjCard_2D_Interaction>();
                var mjCard2D = cardTransform.GetComponent<MjCard_2D>();
                if (mjCardInteraction != null && mjCard2D != null)
                {
                    // 隐藏遮罩，恢复亮度
                    mjCard2D.SetMark(false);
                    // 启用交互
                    mjCardInteraction.enabled = true;
                }
            }
        }

        // 恢复摸牌区亮度
        if (seat.MoPaiContainer != null && seat.MoPaiContainer.childCount > 0)
        {
            Transform moCardTransform = seat.MoPaiContainer.GetChild(0);
            var mjCardInteraction = moCardTransform.GetComponent<MjCard_2D_Interaction>();
            var mjCard2D = moCardTransform.GetComponent<MjCard_2D>();
            if (mjCardInteraction != null && mjCard2D != null)
            {
                // 隐藏遮罩，恢复亮度
                mjCard2D.SetMark(false);
                // 启用交互
                mjCardInteraction.enabled = true;
            }
        }
    }

    /// <summary>
    /// 亮牌按钮点击处理（进入亮牌流程）
    /// 点击liangButton进入亮牌流程，显示LiangPaiPanel面板和BtnLiangPaiTrue、BtnLiangPaiGuo按钮
    /// </summary>
    public void OnLiangPaiButtonClick()
    {
        // 显示亮牌面板
        if (LiangPaiPanel != null)
            LiangPaiPanel.SetActive(true);

        // 隐藏亮按钮，显示确认和取消按钮
        if (pcghtButtonPanel_Kwx != null)
            pcghtButtonPanel_Kwx.SetActive(false);
        if (BtnLiangPaiTrue != null)
            BtnLiangPaiTrue.gameObject.SetActive(true);
        if (BtnLiangPaiGuo != null)
            BtnLiangPaiGuo.gameObject.SetActive(true);

        // 设置手牌变暗效果（除了可听的牌）
        SetHandCardsDimmed(tingPaiList);


        // 启用可听牌的交互（允许抬起选牌）
        EnableTingCardsInteraction();

        GF.LogInfo("进入亮牌流程，置灰不可听的牌");

        // 检查是否已有抬起的牌，如果有则处理暗铺显示
        var raisedCardInfo = FindCurrentRaisedCard();
        if (raisedCardInfo.cardValue > 0 && tingPaiList.Contains(raisedCardInfo.cardValue))
        {
            // 记录已抬起的牌
            selectedTingCard = raisedCardInfo.cardValue;
            select2DPaiType = raisedCardInfo.isMoPai ? HandPaiType.MoPai : HandPaiType.HandPai;
            select2DPaiHandPaiIdx = raisedCardInfo.cardIndex;

            GF.LogInfo($"进入亮牌时已有抬起的牌: {selectedTingCard}, 索引={select2DPaiHandPaiIdx}");

            // 计算需要暗铺的牌
            anPuPaiOptions = CalculateRequiredAnPuPai(selectedTingCard);
            if (anPuPaiOptions.Count > 0)
            {
                // 有需要选择的暗铺牌，显示选择面板
                selectedAnPuPai.Clear();
                ShowXuanAnPuPaiPanel(anPuPaiOptions);
                GF.LogInfo($"检测到{anPuPaiOptions.Count}种参与听牌的暗铺牌: {string.Join(",", anPuPaiOptions)}");
            }
            else
            {
                // 没有需要暗铺的牌，直接显示听牌提示
                selectedAnPuPai.Clear();
                HideXuanAnPuPaiPanel();
                var tingInfo = CheckTingWithAnPu(selectedAnPuPai, selectedTingCard);
                if (tingInfo != null && tingInfo.Length > 0)
                {
                    huPaiTips.Show(tingInfo, baseMahjongGameManager.CurrentLaiZi);
                    GF.LogInfo($"无需暗铺，直接显示听牌提示: {tingInfo.Length}种胡法");
                }
            }
        }
    }

    /// <summary>
    /// 亮牌确认按钮点击处理
    /// </summary>
    public void OnLiangPaiTrueButtonClick()
    {
        // 如果还没有选择牌，尝试从当前抬起的牌中获取
        if (selectedTingCard <= 0)
        {
            // 查找当前抬起的牌
            var raisedCardInfo = FindCurrentRaisedCard();
            if (raisedCardInfo.cardValue > 0)
            {
                // 检查这张牌是否是可听的牌
                if (tingPaiList.Contains(raisedCardInfo.cardValue))
                {
                    // 检查是否是别人亮牌胡的牌（禁牌）
                    if (IsOtherPlayerTingCard(raisedCardInfo.cardValue))
                    {
                        ShowDissOutCardTips();
                        return;
                    }

                    // 使用这张抬起的牌
                    selectedTingCard = raisedCardInfo.cardValue;
                    select2DPaiType = raisedCardInfo.isMoPai ? HandPaiType.MoPai : HandPaiType.HandPai;
                    select2DPaiHandPaiIdx = raisedCardInfo.cardIndex;
                    GF.LogInfo($"亮牌确认：使用已抬起的牌 {selectedTingCard}, 索引={select2DPaiHandPaiIdx}");
                }
                else
                {
                    GF.UI.ShowToast("请抬起可听的牌（高亮的牌）");
                    return;
                }
            }
            else
            {
                GF.UI.ShowToast("请先抬起要亮的牌");
                return;
            }
        }

        // 在发送亮牌请求前，再次检查禁牌（可能是通过OnCardRaised设置的selectedTingCard）
        if (selectedTingCard > 0 && IsOtherPlayerTingCard(selectedTingCard))
        {
            ShowDissOutCardTips();
            return;
        }

        // 直接使用已选择的暗铺数据发送请求
        SendLiangPaiRequest(selectedAnPuPai);
    }

    /// <summary>
    /// 查找当前抬起的牌
    /// </summary>
    private (int cardValue, int cardIndex, bool isMoPai) FindCurrentRaisedCard()
    {
        var seat = GetSeat(0);
        if (seat == null)
            return (-1, -1, false);

        // 检查手牌区
        if (seat.HandContainer != null)
        {
            for (int i = 0; i < seat.HandContainer.childCount; i++)
            {
                Transform cardTransform = seat.HandContainer.GetChild(i);
                var mjCardInteraction = cardTransform.GetComponent<MjCard_2D_Interaction>();
                if (mjCardInteraction != null && mjCardInteraction.IsRaised)
                {
                    return (mjCardInteraction.cardValue, i, false);
                }
            }
        }

        // 检查摸牌区
        if (seat.MoPaiContainer != null && seat.MoPaiContainer.childCount > 0)
        {
            Transform moCardTransform = seat.MoPaiContainer.GetChild(0);
            var mjCardInteraction = moCardTransform.GetComponent<MjCard_2D_Interaction>();
            if (mjCardInteraction != null && mjCardInteraction.IsRaised)
            {
                return (mjCardInteraction.cardValue, 0, true);
            }
        }

        return (-1, -1, false);
    }

    /// <summary>
    /// 亮牌取消按钮点击处理（退出亮牌流程）
    /// </summary>
    public void OnLiangPaiGuoButtonClick()
    {
        // 恢复手牌亮度
        RestoreHandCardsBrightness();
        // 重置按钮状态
        if (pcghtButtonPanel_Kwx != null)
            pcghtButtonPanel_Kwx.SetActive(true);
        if (BtnLiangPaiTrue != null)
            BtnLiangPaiTrue.gameObject.SetActive(false);
        if (BtnLiangPaiGuo != null)
            BtnLiangPaiGuo.gameObject.SetActive(false);

        // 隐藏暗铺选择面板并重置选择
        HideXuanAnPuPaiPanel();
        selectedAnPuPai.Clear();
        selectedTingCard = -1;

        GF.LogInfo("取消亮牌，退出亮牌流程");
    }

    /// <summary>
    /// 启用可听牌的交互（允许抬起选牌）
    /// </summary>
    private void EnableTingCardsInteraction()
    {
        var seat = GetSeat(0);
        if (seat == null)
            return;

        // 手牌区
        if (seat.HandContainer != null)
        {
            for (int i = 0; i < seat.HandContainer.childCount; i++)
            {
                Transform cardTransform = seat.HandContainer.GetChild(i);
                var mjCardInteraction = cardTransform.GetComponent<MjCard_2D_Interaction>();
                if (mjCardInteraction != null)
                {
                    // 只有可听的牌才能交互
                    bool isTingCard = tingPaiList.Contains(mjCardInteraction.cardValue);
                    mjCardInteraction.enabled = isTingCard;
                }
            }
        }

        // 摸牌区
        if (seat.MoPaiContainer != null && seat.MoPaiContainer.childCount > 0)
        {
            Transform moCardTransform = seat.MoPaiContainer.GetChild(0);
            var mjCardInteraction = moCardTransform.GetComponent<MjCard_2D_Interaction>();
            if (mjCardInteraction != null)
            {
                bool isTingCard = tingPaiList.Contains(mjCardInteraction.cardValue);
                mjCardInteraction.enabled = isTingCard;
            }
        }
    }

    /// <summary>
    /// 计算需要暗铺的牌列表（只有参与听牌组合的三张刻子才需要暗铺）
    /// </summary>
    /// <param name="discardCard">要打出的牌</param>
    private List<int> CalculateRequiredAnPuPai(int discardCard)
    {
        List<int> requiredAnPu = new List<int>();

        if (baseMahjongGameManager == null || baseMahjongGameManager.mjHuTingCheck == null)
            return requiredAnPu;

        // 获取打出牌后的手牌数据
        byte[] cardArray = GetPlayerCompleteCardArray();
        MahjongFaceValue discardFace = Util.ConvertServerIntToFaceValue(discardCard);
        if (discardFace != MahjongFaceValue.MJ_UNKNOWN)
        {
            int index = (int)discardFace;
            if (index >= 0 && index < cardArray.Length && cardArray[index] > 0)
                cardArray[index]--;
        }

        byte laiziIndex = GetCurrentLaiziIndex();

        // 获取当前可听的牌
        var huTips = baseMahjongGameManager.mjHuTingCheck.GetHuCards(cardArray, laiziIndex);
        if (huTips == null || huTips.Length == 0)
            return requiredAnPu; // 打出后不听牌，无需暗铺

        // 统计手牌中的刻子（≥3张相同）
        Dictionary<int, int> cardCount = new Dictionary<int, int>();
        for (int i = 0; i < cardArray.Length; i++)
        {
            if (cardArray[i] >= 3)
            {
                // 转换为服务器牌值
                int serverValue = Util.ConvertFaceValueToServerInt((MahjongFaceValue)i);
                if (serverValue > 0)
                    cardCount[serverValue] = cardArray[i];
            }
        }

        // 对每个刻子进行测试：移除3张后是否会失去某些听牌
        foreach (var kvp in cardCount)
        {
            int cardVal = kvp.Key;
            MahjongFaceValue face = Util.ConvertServerIntToFaceValue(cardVal);
            if (face == MahjongFaceValue.MJ_UNKNOWN)
                continue;

            int faceIndex = (int)face;

            // 复制数组，模拟移除这个刻子
            byte[] testArray = new byte[cardArray.Length];
            System.Array.Copy(cardArray, testArray, cardArray.Length);

            if (testArray[faceIndex] >= 3)
                testArray[faceIndex] -= 3;

            // 检查移除后的听牌情况
            var testHuTips = baseMahjongGameManager.mjHuTingCheck.GetHuCards(testArray, laiziIndex);

            // 如果移除后听牌数量减少，说明这个刻子参与了听牌组合
            bool requiresAnPu = (testHuTips == null || testHuTips.Length < huTips.Length);

            // 只有当该刻子参与听牌，并且暗铺后仍然能听牌时，才需要显示
            if (requiresAnPu)
            {
                // 进一步检查：暗铺这个刻子后是否还能听牌
                var checkWithAnPu = CheckTingWithAnPu(new List<int> { cardVal }, discardCard);
                if (checkWithAnPu != null && checkWithAnPu.Length > 0)
                {
                    requiredAnPu.Add(cardVal);
                }
            }
        }

        return requiredAnPu;
    }

    /// <summary>
    /// 检查选择暗铺后的听牌信息
    /// </summary>
    /// <returns>听牌信息数组，null或空表示不听牌</returns>
    private HuPaiTipsInfo[] CheckTingWithAnPu(List<int> anPuCards, int discardCard)
    {
        if (baseMahjongGameManager == null || baseMahjongGameManager.mjHuTingCheck == null)
            return null;

        // 获取当前完整手牌数据
        byte[] cardArray = GetPlayerCompleteCardArray();

        // 移除要打出的牌
        MahjongFaceValue discardFace = Util.ConvertServerIntToFaceValue(discardCard);
        if (discardFace != MahjongFaceValue.MJ_UNKNOWN)
        {
            int index = (int)discardFace;
            if (index >= 0 && index < cardArray.Length)
            {
                if (cardArray[index] > 0)
                    cardArray[index]--;
            }
        }

        // 移除暗铺牌 (每种3张)
        if (anPuCards != null)
        {
            foreach (int cardVal in anPuCards)
            {
                MahjongFaceValue face = Util.ConvertServerIntToFaceValue(cardVal);
                if (face != MahjongFaceValue.MJ_UNKNOWN)
                {
                    int index = (int)face;
                    if (index >= 0 && index < cardArray.Length)
                    {
                        if (cardArray[index] >= 3)
                            cardArray[index] -= 3;
                        else
                            cardArray[index] = 0;
                    }
                }
            }
        }

        // 检查是否听牌，返回听牌信息
        byte laiziIndex = GetCurrentLaiziIndex();
        return baseMahjongGameManager.mjHuTingCheck.GetHuCards(cardArray, laiziIndex);
    }

    /// <summary>
    /// 显示选暗铺牌面板
    /// </summary>
    private void ShowXuanAnPuPaiPanel(List<int> anPuOptions)
    {
        if (XuanAnPuPaiPanel == null)
        {
            GF.LogError("XuanAnPuPaiPanel未配置");
            return;
        }

        GameObject itemTemplate = XuanAnPuPaiPanel.transform.Find("AnPuPaiBtnItem")?.gameObject;
        Transform contentTransform = XuanAnPuPaiPanel.transform.Find("Content");
        if (itemTemplate == null || contentTransform == null)
        {
            GF.LogError("XuanAnPuPaiPanel缺少AnPuPaiBtnItem或Content");
            return;
        }
        // 清空现有选项
        foreach (Transform child in contentTransform)
        {
            if (child.gameObject != itemTemplate)
                GameObject.Destroy(child.gameObject);
        }
        // 创建选项按钮
        foreach (int cardValue in anPuOptions)
        {
            int capturedValue = cardValue; // 捕获变量用于闭包
            GameObject item = GameObject.Instantiate(itemTemplate, contentTransform);
            item.SetActive(true);
            foreach (var textComp in item.GetComponentsInChildren<MjCard_2D>())
            {
                textComp.SetCardValue(capturedValue);
            }
            var gou = item.transform.Find("Gou");

            // 初始化勾选状态
            if (gou != null)
                gou.gameObject.SetActive(selectedAnPuPai.Contains(capturedValue));

            var btn = item.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                // 切换选择状态
                if (selectedAnPuPai.Contains(capturedValue))
                {
                    selectedAnPuPai.Remove(capturedValue);
                }
                else
                {
                    selectedAnPuPai.Add(capturedValue);
                }

                // 更新勾选显示
                if (gou != null)
                    gou.gameObject.SetActive(selectedAnPuPai.Contains(capturedValue));

                // 检查当前选择后的听牌信息
                var tingInfo = CheckTingWithAnPu(selectedAnPuPai, selectedTingCard);
                bool canTing = (tingInfo != null && tingInfo.Length > 0);

                // 更新确认按钮状态
                if (BtnLiangPaiTrue != null)
                {
                    BtnLiangPaiTrue.interactable = canTing;
                }

                // 实时更新听牌提示显示
                if (canTing && tingInfo != null)
                {
                    huPaiTips.Show(tingInfo, baseMahjongGameManager.CurrentLaiZi);
                    GF.LogInfo($"暗铺选择变更，更新听牌提示: {tingInfo.Length}种胡法");
                }
                else
                {
                    huPaiTips.Hide();
                    GF.UI.ShowToast("当前选择无法听牌");
                }
            });
        }

        XuanAnPuPaiPanel.SetActive(true);

        // 初始检查并显示听牌信息
        var initialTingInfo = CheckTingWithAnPu(selectedAnPuPai, selectedTingCard);
        bool initialCanTing = (initialTingInfo != null && initialTingInfo.Length > 0);
        if (BtnLiangPaiTrue != null)
        {
            BtnLiangPaiTrue.interactable = initialCanTing;
        }
        if (initialCanTing && initialTingInfo != null)
        {
            huPaiTips.Show(initialTingInfo , baseMahjongGameManager.CurrentLaiZi);
        }

        GF.LogInfo($"显示选暗铺牌面板，可选暗铺牌: {string.Join(",", anPuOptions)}");
    }    /// <summary>
         /// 隐藏选暗铺牌面板
         /// </summary>
    public void HideXuanAnPuPaiPanel()
    {
        if (XuanAnPuPaiPanel != null)
            XuanAnPuPaiPanel.SetActive(false);
    }

    /// <summary>
    /// 发送亮牌请求
    /// </summary>
    private void SendLiangPaiRequest(List<int> anPuPais)
    {
        if (mj_procedure == null)
        {
            GF.LogError("mj_procedure为空，无法发送亮牌请求");
            return;
        }

        // 发送亮牌请求
        Msg_LiangDaoRq req = new Msg_LiangDaoRq();
        // 设置打出的牌(Discard)为玩家抬起选择的听牌
        req.Discard = selectedTingCard > 0 ? selectedTingCard : 0;

        // 如果有选择的暗铺牌，添加到Card列表中
        if (anPuPais != null && anPuPais.Count > 0)
        {
            for (int i = 0; i < anPuPais.Count; i++)
            {
                req.Card.Add(anPuPais[i]);
                req.Card.Add(anPuPais[i]);
                req.Card.Add(anPuPais[i]);
            }
            GF.LogInfo($"发送亮牌请求 - Discard: {req.Discard}, 暗铺牌: [{string.Join(",", anPuPais)}]");
        }
        else
        {
            // 不暗铺，Card列表为空
            GF.LogInfo($"发送亮牌请求 - Discard: {req.Discard}, 不暗铺（Card为空列表）");
        }

        mj_procedure.Send_LiangDaoRq(req);

        // 隐藏亮牌面板
        HideLiangPaiPanel();
    }

    /// <summary>
    /// 当有牌被抬起时（由 MjCard_2D_Interaction 调用），用于亮牌流程记录选择的抬起牌
    /// </summary>
    /// <param name="cardValue">牌值</param>
    /// <param name="isMoPai">是否为摸牌</param>
    /// <param name="cardIndex">牌在手牌容器中的索引</param>
    public void OnCardRaised(int cardValue, bool isMoPai, int cardIndex = -1)
    {
        // 仅在亮牌面板可见并且已进入亮牌确认阶段时处理
        if (LiangPaiPanel == null || !LiangPaiPanel.activeSelf)
            return;

        // 只有在玩家已经点击了亮牌按钮、并可进行确认时，记录抬起的牌
        // BtnLiangPaiTrue 可视说明玩家已进入确认状态
        if (BtnLiangPaiTrue != null && BtnLiangPaiTrue.gameObject.activeSelf)
        {
            selectedTingCard = cardValue;

            // 同时保存到2D打牌选择信息，供Ting协议处理时使用
            select2DPaiType = isMoPai ? HandPaiType.MoPai : HandPaiType.HandPai;
            select2DPaiHandPaiIdx = cardIndex;

            GF.LogInfo($"亮牌流程：记录抬起牌为 {cardValue}, 索引={cardIndex}, 类型={select2DPaiType}");

            // 计算需要暗铺的牌（只有参与听牌组合的刻子才需要暗铺）
            anPuPaiOptions = CalculateRequiredAnPuPai(cardValue);
            if (anPuPaiOptions.Count > 0)
            {
                // 有需要选择的暗铺牌，显示选择面板
                selectedAnPuPai.Clear();
                ShowXuanAnPuPaiPanel(anPuPaiOptions);
                GF.LogInfo($"检测到{anPuPaiOptions.Count}种参与听牌的暗铺牌: {string.Join(",", anPuPaiOptions)}");
            }
            else
            {
                // 没有需要暗铺的牌（刻子都不参与听牌，可直接杠）
                selectedAnPuPai.Clear();
                HideXuanAnPuPaiPanel();

                // 直接显示听牌提示
                var tingInfo = CheckTingWithAnPu(selectedAnPuPai, cardValue);
                if (tingInfo != null && tingInfo.Length > 0)
                {
                    huPaiTips.Show(tingInfo , baseMahjongGameManager.CurrentLaiZi);
                    GF.LogInfo($"无需暗铺，直接显示听牌提示: {tingInfo.Length}种胡法");
                }
            }
        }
    }

    /// <summary>
    /// 显示听牌提示（某玩家听牌后显示可胡的牌）
    /// </summary>
    /// <param name="seatIdx">座位索引</param>
    /// <param name="tingCards">可听的牌列表（从服务器返回）</param>
    public void ShowTingPaiTips(int seatIdx, List<int> tingCards)
    {
        var seat = GetSeat(seatIdx);
        if (seat == null)
        {
            GF.LogError($"座位{seatIdx}不存在，无法显示听牌提示");
            return;
        }

        if (tingCards == null || tingCards.Count == 0)
        {
            GF.LogWarning($"座位{seatIdx}听牌列表为空");
            return;
        }

        // 存储听牌到座位（用于判断禁牌）
        seat.SetTingCards(tingCards);

        List<HuPaiTipsInfo> huPaiInfos = new List<HuPaiTipsInfo>();
        // 只有自己（座位0）需要计算番数和剩余张数
        if (seatIdx == 0 && baseMahjongGameManager != null && baseMahjongGameManager.mjHuTingCheck != null)
        {
            // 使用当前手牌（不加听牌）调用GetHuCards，获取所有可胡牌的番数和剩余张数信息
            byte laiziIndex = GetCurrentLaiziIndex();
            byte[] cardArray = GetPlayerCompleteCardArray();

            // 先获取所有可胡牌的信息（包含番数和剩余张数）
            var allHuTips = baseMahjongGameManager.mjHuTingCheck.GetHuCards(cardArray, laiziIndex);

            // 将结果转换为字典，方便查找
            Dictionary<MahjongFaceValue, HuPaiTipsInfo> huTipsDict = new Dictionary<MahjongFaceValue, HuPaiTipsInfo>();
            if (allHuTips != null)
            {
                foreach (var tip in allHuTips)
                {
                    if (!huTipsDict.ContainsKey(tip.faceValue))
                    {
                        huTipsDict[tip.faceValue] = tip;
                    }
                }
            }

            // 遍历服务器返回的听牌列表，从字典中获取对应的番数和剩余张数
            foreach (int tingCard in tingCards)
            {
                MahjongFaceValue tingFace = Util.ConvertServerIntToFaceValue(tingCard);
                if (tingFace == MahjongFaceValue.MJ_UNKNOWN)
                    continue;

                // 从字典中查找对应的胡牌信息
                HuPaiTipsInfo info = new HuPaiTipsInfo();
                info.faceValue = tingFace;

                if (huTipsDict.TryGetValue(tingFace, out HuPaiTipsInfo matchedInfo))
                {
                    info.fanAmount = matchedInfo.fanAmount;
                    info.zhangAmount = matchedInfo.zhangAmount;
                }
                else
                {
                    // 如果在GetHuCards结果中找不到（可能是服务器数据与本地计算不一致），使用默认值
                    info.fanAmount = 1;
                    info.zhangAmount = CalculateRemainingCount(tingFace, cardArray);
                }

                huPaiInfos.Add(info);
            }

            GF.LogInfo($"座位{seatIdx}显示听牌提示（含番数和剩余张数）: 服务器指定{tingCards.Count}张听牌");
        }
        else
        {
            // 其他玩家：直接使用服务器返回的听牌数据，不显示番数和剩余张数
            foreach (int card in tingCards)
            {
                HuPaiTipsInfo info = new HuPaiTipsInfo();
                info.faceValue = Util.ConvertServerIntToFaceValue(card);
                info.fanAmount = -1;  // -1 表示不显示番数
                info.zhangAmount = -1;  // -1 表示不显示张数
                huPaiInfos.Add(info);
            }
            GF.LogInfo($"座位{seatIdx}显示听牌提示（简单模式）: {tingCards.Count}张可胡牌 [{string.Join(",", tingCards)}]");
        }

        seat.ShowHuPaiTips(huPaiInfos);
    }

    /// <summary>
    /// 计算指定牌的剩余张数
    /// </summary>
    /// <param name="faceValue">牌面值</param>
    /// <param name="cardArray">当前手牌数组</param>
    /// <returns>剩余张数</returns>
    private int CalculateRemainingCount(MahjongFaceValue faceValue, byte[] cardArray)
    {
        int faceIndex = (int)faceValue;
        int selfHasCount = (faceIndex >= 0 && faceIndex < cardArray.Length) ? cardArray[faceIndex] : 0;
        int usedInField = GetUsedCardCountInField(faceValue);
        // 剩余张数 = 总数4 - 自己手里的 - 场上其他人使用的
        return System.Math.Max(0, 4 - selfHasCount - usedInField);
    }

    /// <summary>
    /// 处理玩家亮牌消息（Msg_SynPlayerHandCard）
    /// </summary>
    public void HandleSynPlayerHandCard(Msg_SynPlayerHandCard msg)
    {
        if (msg == null)
        {
            GF.LogError("Msg_SynPlayerHandCard 消息为空");
            return;
        }

        // 通过 PlayerId 查找座位索引
        int seatIdx = baseMahjongGameManager?.GetSeatIndexByPlayerId(msg.PlayerId) ?? -1;
        if (seatIdx < 0)
        {
            GF.LogWarning($"未找到玩家{msg.PlayerId}的座位");
            return;
        }

        var seat = GetSeat(seatIdx);
        if (seat == null)
        {
            GF.LogError($"座位{seatIdx}不存在");
            return;
        }

        // msg.HandCard 包含亮出的牌（每种牌3张表示刻子）
        var liangPaiCards = msg.HandCard.ToList();

        GF.LogInfo($"玩家{msg.PlayerId}(座位{seatIdx})亮牌: {string.Join(",", liangPaiCards)}");

        // 播放亮牌音效（卡五星使用普通话）
        MahjongAudio.PlayActionVoiceForPlayer("liang", msg.PlayerId, mj_procedure.enterMJDeskRs.BaseInfo.MjConfig.MjMethod, 0);

        // 显示亮牌
        seat.ShowLiangPaiCards(liangPaiCards, seatIdx == 0);
    }

    /// <summary>
    /// 检查某张牌是否是其他玩家亮牌后的禁牌（别人可胡的牌）
    /// </summary>
    /// <param name="cardValue">要检查的牌值（服务器格式）</param>
    /// <returns>如果是禁牌返回true，否则返回false</returns>
    public bool IsOtherPlayerTingCard(int cardValue)
    {
        // 遍历其他座位（座位1、2、3），检查是否有玩家亮牌且该牌在其听牌列表中
        for (int seatIdx = 1; seatIdx < seats2D.Length; seatIdx++)
        {
            var seat = GetSeat(seatIdx);
            if (seat == null) continue;

            // 只有亮牌的玩家才需要检查禁牌
            if (!seat.HasLiangPai) continue;

            // 检查该牌是否在对方的听牌列表中
            if (seat.TingCards != null && seat.TingCards.Contains(cardValue))
            {
                GF.LogInfo($"牌{cardValue}是座位{seatIdx}的听牌，禁止打出");
                return true;
            }
        }
        return false;
    }

    // 禁牌提示动画序列（用于判断是否正在播放）
    private Sequence dissOutCardTipsSeq;
    // 禁牌提示是否正在显示
    private bool isDissOutCardTipsShowing = false;

    /// <summary>
    /// 显示禁止出牌提示（别人亮牌了该牌不能打出）
    /// 使用DOTween动画：缩放弹出 + 震动 + 渐隐消失
    /// </summary>
    public void ShowDissOutCardTips()
    {
        if (DissOutCardTips != null)
        {
            var canvasGroup = DissOutCardTips.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = DissOutCardTips.AddComponent<CanvasGroup>();
            }

            // 如果正在显示，重新开始动画（确保完整显示）
            if (isDissOutCardTipsShowing && dissOutCardTipsSeq != null && dissOutCardTipsSeq.IsActive())
            {
                // 停止当前动画
                dissOutCardTipsSeq.Kill();

                // 直接从震动开始（跳过弹出，因为已经显示了）
                DissOutCardTips.transform.localScale = Vector3.one;
                canvasGroup.alpha = 1f;

                dissOutCardTipsSeq = DOTween.Sequence();
                // 震动
                dissOutCardTipsSeq.Append(DissOutCardTips.transform.DOShakePosition(0.3f, new Vector3(10f, 0f, 0f), 20, 0f, false, true));
                // 停留
                dissOutCardTipsSeq.AppendInterval(1.5f);
                // 渐隐
                dissOutCardTipsSeq.Append(canvasGroup.DOFade(0f, 0.3f).SetEase(Ease.InQuad));
                // 结束
                dissOutCardTipsSeq.OnComplete(() =>
                {
                    isDissOutCardTipsShowing = false;
                    DissOutCardTips.SetActive(false);
                    DissOutCardTips.transform.localScale = Vector3.one;
                    canvasGroup.alpha = 1f;
                });

                GF.LogInfo("禁止出牌提示：重新震动（连续触发）");
                return;
            }

            // 停止之前的动画
            if (dissOutCardTipsSeq != null)
            {
                dissOutCardTipsSeq.Kill();
            }
            DissOutCardTips.transform.DOKill();
            canvasGroup.DOKill();

            // 初始状态
            isDissOutCardTipsShowing = true;
            DissOutCardTips.SetActive(true);
            DissOutCardTips.transform.localScale = Vector3.zero;
            canvasGroup.alpha = 1f;

            // 动画序列
            dissOutCardTipsSeq = DOTween.Sequence();

            // 1. 弹出动画（缩放从0到1.1再回弹到1）
            dissOutCardTipsSeq.Append(DissOutCardTips.transform.DOScale(1.15f, 0.2f).SetEase(Ease.OutBack));
            dissOutCardTipsSeq.Append(DissOutCardTips.transform.DOScale(1f, 0.1f).SetEase(Ease.InOutSine));

            // 2. 轻微震动提示
            dissOutCardTipsSeq.Append(DissOutCardTips.transform.DOShakePosition(0.3f, new Vector3(10f, 0f, 0f), 20, 0f, false, true));

            // 3. 停留一段时间后渐隐消失
            dissOutCardTipsSeq.AppendInterval(1.5f);
            dissOutCardTipsSeq.Append(canvasGroup.DOFade(0f, 0.3f).SetEase(Ease.InQuad));

            // 4. 动画结束后隐藏
            dissOutCardTipsSeq.OnComplete(() =>
            {
                isDissOutCardTipsShowing = false;
                DissOutCardTips.SetActive(false);
                DissOutCardTips.transform.localScale = Vector3.one;
                canvasGroup.alpha = 1f;
            });

            GF.LogInfo("显示禁止出牌提示（DOTween动画）");
        }
        Enable2DPlayCard();
    }

    /// <summary>
    /// 隐藏禁止出牌提示
    /// </summary>
    public void HideDissOutCardTips()
    {
        if (DissOutCardTips != null)
        {
            isDissOutCardTipsShowing = false;
            if (dissOutCardTipsSeq != null)
            {
                dissOutCardTipsSeq.Kill();
                dissOutCardTipsSeq = null;
            }
            DissOutCardTips.transform.DOKill();
            var canvasGroup = DissOutCardTips.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 1f;
            }
            DissOutCardTips.transform.localScale = Vector3.one;
            DissOutCardTips.SetActive(false);
        }
    }

    #endregion

    #region 亮码牌

    /// <summary>
    /// 亮码牌动画起始Y轴高度
    /// </summary>
    private const float LIANG_MA_PAI_START_Y = 500f;

    /// <summary>
    /// 亮码牌动画持续时间
    /// </summary>
    private const float LIANG_MA_PAI_DURATION = 0.5f;

    /// <summary>
    /// 显示亮码牌动画
    /// </summary>
    /// <param name="horseValue">码牌值（服务器牌值）</param>
    public void ShowLiangMaPai(int horseValue)
    {
        if (LiangMaPai == null)
        {
            GF.LogWarning("LiangMaPai 未配置");
            return;
        }

        if (horseValue <= 0)
        {
            GF.LogInfo("无码牌值，不显示亮码牌");
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
                bool isLaizi = baseMahjongGameManager != null && baseMahjongGameManager.IsLaiZiCard(horseValue);
                mjCard.SetCardValue(faceValue, isLaizi);
                GF.LogInfo($"亮码牌设置牌值: {horseValue} -> {faceValue}, 是否赖子: {isLaizi}");
            }
            else
            {
                GF.LogWarning("MaPai 上没有 MjCard_2D 组件");
            }
        }
        else
        {
            GF.LogWarning("LiangMaPai 下没有找到 MaPai 子物体");
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
            GF.LogInfo("亮码牌动画播放完成");
        });
    }

    /// <summary>
    /// 隐藏亮码牌
    /// </summary>
    public void HideLiangMaPai()
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

    #region KWX清理

    /// <summary>
    /// 清理所有卡五星相关的面板和状态（退出房间或游戏结束时调用）
    /// </summary>
    public void CleanupAllKWXPanels()
    {
        mj_procedure.enterMJDeskRs.ChangeCardPlayer.Clear();
        // 隐藏飘分面板
        PiaoPanel?.SetActive(false);

        // 隐藏骰子动画面板
        TouZiAniPanel?.SetActive(false);

        // 隐藏换牌相关面板
        HuanPaiAniPanel?.SetActive(false);
        XuanPaiPanel?.SetActive(false);
        HuanPaiInfoPanel?.SetActive(false);

        // 隐藏换牌方向按钮
        HideHuanPaiInfoButton();

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

        // 隐藏亮码牌
        HideLiangMaPai();

        // 隐藏限制出牌提示
        DissOutCardTips?.SetActive(false);

        // 隐藏亮牌相关面板
        LiangPaiPanel?.SetActive(false);
        XuanAnPuPaiPanel?.SetActive(false);

        // 清理状态数据
        selectedChangeCards.Clear();
        sentChangeCardValues.Clear();
        oldHandCardsBeforeChange.Clear();
        tingPaiList.Clear();
        anPuPaiOptions.Clear();
        selectedAnPuPai.Clear();
        selectedTingCard = -1;

        GF.LogInfo("[KWX] 清理所有卡五星面板完成");
    }

    #endregion
}

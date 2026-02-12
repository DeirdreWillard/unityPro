using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using UnityGameFramework.Runtime;
using Google.Protobuf;
using static UtilityBuiltin;
using DG.Tweening;
using System.Collections;
using Cysharp.Threading.Tasks;

/// <summary>
/// 麻将回放座位类 - 统一管理单个座位的UI显示
/// 包含头像、手牌、弃牌、吃碰杠等所有相关显示
/// </summary>
public class MahjongSeat_Back : MonoBehaviour
{
    #region 字段定义

    public DeskPlayer deskPlayer;
    private Msg_MJPlayBack msg_playback;            // 回放消息

    /// <summary>座位索引 0-3 (下左上右)</summary>
    [Header("===== 座位配置 =====")]
    public int SeatIndex = 0;

    /// <summary>弃牌区每行最大牌数</summary>
    [Tooltip("弃牌区每行最多显示的牌数")]
    public int maxDiscardCardsPerRow = 9;

    /// <summary>弃牌区最大行数</summary>
    [Tooltip("弃牌区最多显示的行数")]
    public int maxDiscardRows = 2;

    #region 遮挡顺序配置（统一管理不同座位的节点顺序逻辑）

    /// <summary>
    /// 手牌容器是否需要反向节点顺序（解决遮挡问题）
    /// 左家(1)垂直从下到上排列，需要反向节点顺序，下面的牌遮挡上面的牌
    /// </summary>
    private bool NeedReverseHandNodeOrder => SeatIndex == 1;

    /// <summary>
    /// 弃牌容器是否需要新牌插入到最前面（解决遮挡问题）
    /// 右家(3)和对家(2)的弃牌需要被旧牌遮挡，所以新牌插入到最前面
    /// </summary>
    private bool NeedInsertDiscardAtFirst => SeatIndex == 3 || SeatIndex == 2;

    /// <summary>
    /// 吃碰杠容器是否需要新牌插入到最前面（解决遮挡问题）
    /// 右家(3)的吃碰杠需要反序节点，新牌组插入到最前面
    /// 这样能确保后面的牌遮挡前面的牌（视觉上从下到上）
    /// </summary>
    private bool NeedInsertMeldAtFirst => SeatIndex == 3;

    /// <summary>
    /// 获取弃牌容器中最新添加的牌的索引
    /// 考虑垒牌逻辑：根据当前牌数动态计算索引
    /// </summary>
    public int GetNewestDiscardIndex()
    {
        if (DiscardContainer == null || DiscardContainer.childCount == 0)
            return -1;

        int cardCount = DiscardContainer.childCount;
        int totalCardsPerTwoRows = maxDiscardCardsPerRow * maxDiscardRows;

        // 座位0/1：最新牌始终在最后
        if (!NeedInsertDiscardAtFirst)
        {
            return cardCount - 1;
        }

        // 座位2/3：需要根据是否有垒牌来计算
        if (cardCount <= totalCardsPerTwoRows)
        {
            // 未垒牌：最新牌在索引0
            return 0;
        }
        else
        {
            // 有垒牌：需要逆向计算
            // 最新牌的逻辑索引是 cardCount - 1
            int logicalIndex = cardCount - 1;
            int layers = logicalIndex / totalCardsPerTwoRows;
            int adjustedIndex = logicalIndex % totalCardsPerTwoRows;
            
            // 计算实际的节点索引（逆向插入逻辑）
            // 第n层的第m张牌的插入位置计算
            int totalCardsBeforeCurrentLayer = (layers - 1) * totalCardsPerTwoRows;
            int cardsInCurrentLayerBeforeCurrentCard = adjustedIndex;
            int actualIndex = cardCount - (totalCardsBeforeCurrentLayer + cardsInCurrentLayerBeforeCurrentCard + 1);
            
            actualIndex = Mathf.Clamp(actualIndex, 0, cardCount - 1);
            return actualIndex;
        }
    }

    /// <summary>
    /// 获取手牌容器中逻辑上最后一张牌的索引（显示在最右侧/最上方的牌）
    /// 注意：这张牌在数据上应该是最大的牌（或赖子）
    /// - 0号位（自己）：数据正序+节点正常 → 逻辑最后一张=节点最后（最大牌）
    /// - 1号位（左家）：数据逆序+节点反向 → 逻辑最后一张=节点索引0（最大牌，因为节点反向后在最下面）
    /// - 2号位（对家）：数据逆序+节点正常 → 逻辑最后一张=节点索引0（最大牌，在最右边）
    /// - 3号位（右家）：数据逆序+节点正常 → 逻辑最后一张=节点索引0（最大牌，在最上面）
    /// </summary>
    public int GetLogicalLastHandCardIndex()
    {
        if (HandContainer == null || HandContainer.childCount == 0)
            return -1;

        // 1/2/3号位数据都是逆序的，最大牌在索引0
        if (SeatIndex == 1 || SeatIndex == 2 || SeatIndex == 3)
        {
            return 0;
        }
        
        // 0号位：数据正序，最大牌在最后
        return HandContainer.childCount - 1;
    }

    /// <summary>
    /// 撤回补杠：将4张的组还原为3张，并将多出的牌返回手牌
    /// </summary>
    public void UndoBuGangCard(int cardValue)
    {
        if (MeldContainer == null) return;

        // 1. 收集当前所有组
        List<List<int>> allMeldGroups = new List<List<int>>();
        List<int> currentGroup = new List<int>();
        int lastCardValue = -1;

        // 右家(3号位)的牌是反序插入的(最新的在节点最前面)，需要反向遍历以获取正确的逻辑顺序
        bool isRightSeat = (SeatIndex == 3);
        int startIdx = isRightSeat ? MeldContainer.childCount - 1 : 0;
        int endIdx = isRightSeat ? -1 : MeldContainer.childCount;
        int step = isRightSeat ? -1 : 1;

        for (int i = startIdx; i != endIdx; i += step)
        {
            MjCard_2D mjCard = MeldContainer.GetChild(i).GetComponent<MjCard_2D>();
            if (mjCard != null)
            {
                int val = mjCard.cardValue;
                if (val == lastCardValue || lastCardValue == -1)
                {
                    currentGroup.Add(val);
                }
                else
                {
                    if (currentGroup.Count > 0) allMeldGroups.Add(new List<int>(currentGroup));
                    currentGroup.Clear();
                    currentGroup.Add(val);
                }
                lastCardValue = val;
            }
        }
        if (currentGroup.Count > 0) allMeldGroups.Add(currentGroup);

        // 2. 找到对应的4张组并还原为3张
        bool found = false;
        for (int i = allMeldGroups.Count - 1; i >= 0; i--)
        {
            if (allMeldGroups[i].Count == 4 && allMeldGroups[i][0] == cardValue)
            {
                allMeldGroups[i].RemoveAt(3);
                found = true;
                break;
            }
        }

        if (!found) return;

        // 3. 重建MeldContainer
        ClearContainer(MeldContainer);
        foreach (var group in allMeldGroups)
        {
            AddMeldGroup(group);
        }
    }

    #endregion

    /// <summary>玩家头像</summary>
    [Header("===== 玩家信息UI =====")]
    public RawImage PlayerAvatar;

    /// <summary>玩家昵称</summary>
    public Text PlayerNickname;

    /// <summary>玩家金币</summary>
    public Text PlayerCoin;

    /// <summary>玩家赖子数</summary>
    public Text LaiNum;

    /// <summary>庄家标识</summary>
    public GameObject BankerIcon;

    /// <summary>风位文本（东南西北）</summary>
    public Text WindText;

    /// <summary>听牌标识</summary>
    public GameObject TingPaiIcon;

    /// <summary>手牌容器</summary>
    public RectTransform HandContainer;

    /// <summary>摸牌容器 - 单独显示摸到的牌</summary>
    public RectTransform MoPaiContainer;

    /// <summary>弃牌容器</summary>
    public RectTransform DiscardContainer;

    /// <summary>吃碰杠容器</summary>
    public RectTransform MeldContainer;

    /// <summary>特效容器</summary>
    public RectTransform EffContainer;

    /// <summary>准备图标</summary>
    public GameObject ReadyIcon;

    [Header("===== 仙桃晃晃解锁 =====")]
    public GameObject BaoBg;
    public Text BaoText;

    /// <summary>手牌对象</summary>
    public GameObject objectHand;

    /// <summary>弃牌对象</summary>
    public GameObject objectDiscard;

    /// <summary>吃碰杠对象</summary>
    public GameObject objectMeld;

    /// <summary>胡牌对象(用于血流成河HuContainer)</summary>
    public GameObject objectHuCard;

    /// <summary>胡牌容器(用于血流成河多次胡牌)</summary>
    public RectTransform HuContainer;

    public int intervalHand = 0;
    public int intervalDiscard = 0;
    public int intervalMeld = 0;
    public int intervalHu = 0;

    /// <summary>当前座位弃牌中赖子的数量（用于锁牌判定）</summary>
    public int laiziDiscardCount { get; private set; } = 0;

    /// <summary>最新弃牌的实际索引（考虑垒牌后的复杂插入逻辑）</summary>
    private int newestDiscardActualIndex = -1;

    /// <summary>
    /// 胡牌容器是否需要新牌插入到最前面（解决遮挡问题）
    /// 右家(3)和对家(2)的胡牌需要新牌被旧牌遮挡，所以新牌插入到最前面
    /// </summary>
    private bool NeedInsertHuAtFirst => SeatIndex == 3 || SeatIndex == 2;

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化座位（从 gameUI2D 自动获取所有需要的资源）
    /// </summary>
    public void Initialize(Msg_MJPlayBack msg_playback)
    {
        this.msg_playback = msg_playback;
        ClearSeat();
        
        // 初始化时隐藏HuPaiTips
        HideHuPaiTips();
    }

    /// <summary>
    /// 判断是否为赖子牌（回放专用）
    /// </summary>
    private bool IsLaiZiCard(int cardValue)
    {
        return msg_playback.Laizi > 0 && cardValue == msg_playback.Laizi;
    }

    #endregion
    #region 准备状态管理

    /// <summary>
    /// 设置准备状态显示
    /// </summary>
    public void SetReadyState(bool isReady)
    {
        ReadyIcon.SetActive(isReady);
    }

    /// <summary>
    /// 更新仙桃晃晃解锁状态显示
    /// </summary>
    /// <param name="isUnlocked">是否已解锁（翻倍）</param>
    /// <param name="isLocked">是否处于未解锁（被锁）状态</param>
    public void UpdateBaoStatus(bool isUnlocked, bool isLocked)
    {
        if (BaoBg == null) return;

        if (isUnlocked)
        {
            BaoBg.SetActive(true);
            if (BaoText != null) BaoText.text = "已解锁";
        }
        else if (isLocked)
        {
            BaoBg.SetActive(true);
            if (BaoText != null) BaoText.text = "未解锁";
        }
        else
        {
            BaoBg.SetActive(false);
        }
    }

    #endregion

    #region 手牌管理

    /// <summary>
    /// 创建手牌显示
    /// </summary>
    public void CreateHandCards(List<int> handCards)
    {
        // 清理现有手牌
        ClearContainer(HandContainer);

        // 回放模式：使用赖子值进行排序（只对自己座位排序）
        List<int> sortedCards = Util.SortMahjongHandCards(handCards, msg_playback.Laizi);

        // 座位1和2需要逆序手牌数据，使显示顺序正常
        // 1号位（左家）：垂直从下到上排列，数据逆序后从最大牌开始显示
        // 2号位（对家）：水平从左到右排列，数据逆序后从最大牌开始显示
        // 这样插入新牌时逻辑更简单
        if (SeatIndex == 1 || SeatIndex == 2 || SeatIndex == 3)
        {
            sortedCards.Reverse();
        }

        // 使用objectHand创建手牌
        CreateCardsWithPrefab(sortedCards, HandContainer, objectHand);

        GF.LogInfo($"[2D座位{SeatIndex}] 创建手牌完成，共{sortedCards.Count}张");
    }

    /// <summary>
    /// 创建手牌显示（带下坠动画，用于换牌）
    /// </summary>
    /// <param name="handCards">所有手牌</param>
    /// <param name="changedInCards">换入的牌（需要播放下坠动画）</param>
    public void CreateHandCardsWithDropAnimation(List<int> handCards, List<int> changedInCards)
    {
        // 清理现有手牌
        ClearContainer(HandContainer);

        // 回放模式：使用赖子值进行排序
        List<int> sortedCards = Util.SortMahjongHandCards(handCards, msg_playback.Laizi);

        // 座位1、2、3需要逆序手牌数据
        if (SeatIndex == 1 || SeatIndex == 2 || SeatIndex == 3)
        {
            sortedCards.Reverse();
        }

        // 复制一份换入牌列表用于匹配
        List<int> remainingChangedCards = new List<int>(changedInCards);

        // 左家(座位1)特殊处理:垂直显示从下到上,需要反向加载
        // - 先加载index大的牌(y坐标大,位置靠上,渲染层级低,被遮挡)
        // - 后加载index小的牌(y坐标小,位置靠下,渲染层级高,遮挡其他牌)
        bool needReverse = NeedReverseHandNodeOrder;

        // 创建手牌，换入的牌带下坠动画
        for (int i = 0; i < sortedCards.Count; i++)
        {
            // 左家手牌反向遍历: 从后往前取
            int index = needReverse ? (sortedCards.Count - 1 - i) : i;

            int cardValue = sortedCards[index];
            Vector2 position = CalculateCardPosition(index, sortedCards.Count, objectHand);
            bool isLaizi = IsLaiZiCard(cardValue);

            // 检查这张牌是否是换入的牌
            bool isChangedIn = remainingChangedCards.Contains(cardValue);
            if (isChangedIn)
            {
                remainingChangedCards.Remove(cardValue);
            }

            // 创建手牌
            GameObject cardObj = CreateCardWithPrefab(cardValue, HandContainer, objectHand, position, isLaizi);

            // 如果是换入的牌，播放下坠动画
            if (isChangedIn && cardObj != null)
            {
                PlayCardDropAnimation(cardObj, position);
            }
        }

        GF.LogInfo($"[2D座位{SeatIndex}] 创建手牌完成（换牌模式），共{sortedCards.Count}张，换入{changedInCards.Count}张");
    }

    /// <summary>
    /// 换牌下坠动画的ID前缀
    /// </summary>
    private const string DROP_ANIMATION_ID = "CardDropAnimation_";

    /// <summary>
    /// 播放单张牌的下坠动画
    /// </summary>
    private void PlayCardDropAnimation(GameObject cardObj, Vector2 targetPosition)
    {
        if (cardObj == null) return;

        RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        // 设置初始位置（从高处开始）
        float dropHeight = 300f;
        Vector2 startPos = targetPosition;
        
        // 根据座位方向调整下坠方向
        if (SeatIndex == 0 || SeatIndex == 2) // 上下座位，垂直下坠
        {
            startPos.y = targetPosition.y + dropHeight;
        }
        else // 左右座位
        {
            if (SeatIndex == 1) // 左家
            {
                startPos.x = targetPosition.x - dropHeight;
            }
            else // 右家
            {
                startPos.x = targetPosition.x + dropHeight;
            }
        }

        rectTransform.anchoredPosition = startPos;

        // 播放下坠动画，设置ID以便后续可以正确取消
        rectTransform.DOAnchorPos(targetPosition, 0.5f)
            .SetEase(Ease.OutBounce)
            .SetId(DROP_ANIMATION_ID + SeatIndex);
    }

    /// <summary>
    /// 刷新手牌显示
    /// </summary>
    public void RefreshHandCards(List<int> handCards)
    {
        CreateHandCards(handCards);
    }

    /// <summary>
    /// 在手牌容器中显示多张背牌（用于发牌动画）
    /// </summary>
    public void ShowBackCardsInHandContainer(int cardCount)
    {
        // 清空手牌容器
        ClearContainer(HandContainer);

        if (cardCount <= 0) return;

        // 创建指定数量的背牌
        for (int i = 0; i < cardCount; i++)
        {
            Vector2 position = CalculateCardPosition(i);

            // 创建背牌
            GameObject cardObj = Instantiate(objectHand, HandContainer);
            if (cardObj != null)
            {
                cardObj.SetActive(true);
                RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = position;
                }
            }
        }

        GF.LogInfo($"[2D座位{SeatIndex}] 手牌容器显示{cardCount}张背牌");
    }

    /// <summary>
    /// 添加摸牌到独立区域（复用手牌预制体）
    /// </summary>
    public void AddMoPai(int cardValue)
    {
        ClearContainer(MoPaiContainer);

        // 摸牌容器使用固定位置
        Vector2 position = Vector2.zero;

        // 判断是否为赖子
        bool isLaizi = IsLaiZiCard(cardValue);

        // 复用手牌预制体创建摸牌
        GameObject cardObj = CreateCardWithPrefab(cardValue, MoPaiContainer, objectHand, position, isLaizi);

        GF.LogInfo($"[2D座位{SeatIndex}] 摸牌: {cardValue}");
    }

    /// <summary>
    /// 添加摸牌到独立区域（带下坠动画，用于换牌）
    /// </summary>
    public void AddMoPaiWithDropAnimation(int cardValue)
    {
        ClearContainer(MoPaiContainer);

        // 摸牌容器使用固定位置
        Vector2 position = Vector2.zero;

        // 判断是否为赖子
        bool isLaizi = IsLaiZiCard(cardValue);

        // 复用手牌预制体创建摸牌
        GameObject cardObj = CreateCardWithPrefab(cardValue, MoPaiContainer, objectHand, position, isLaizi);

        // 播放下坠动画
        if (cardObj != null)
        {
            PlayCardDropAnimation(cardObj, position);
        }

        GF.LogInfo($"[2D座位{SeatIndex}] 摸牌(带动画): {cardValue}");
    }

    /// <summary>
    /// 强制完成所有下坠动画，将牌立即移动到目标位置
    /// </summary>
    public void CompleteDropAnimations()
    {
        // 杀死并完成该座位的所有下坠动画
        DOTween.Complete(DROP_ANIMATION_ID + SeatIndex);
        DOTween.Kill(DROP_ANIMATION_ID + SeatIndex);
    }

    public int GetMoPaiCount()
    {
        return MoPaiContainer.childCount;
    }

    /// <summary>
    /// 移除一张手牌（打牌）
    /// </summary>
    public void RemoveHandCard(int cardIndex)
    {
        if (HandContainer == null || cardIndex < 0 || cardIndex >= HandContainer.childCount)
        {
            GF.LogWarning($"[2D座位{SeatIndex}] 移除手牌失败: cardIndex={cardIndex}, childCount={HandContainer?.childCount ?? 0}");
            return;
        }

        Transform cardTransform = HandContainer.GetChild(cardIndex);
        MjCard_2D mjCard = cardTransform.GetComponent<MjCard_2D>();
        int actualCardValue = mjCard != null ? mjCard.cardValue : -1;

        GF.LogInfo($"[2D座位{SeatIndex}] 准备移除手牌: 索引={cardIndex}, 牌值={actualCardValue}");

        // 先将对象移出父节点，确保childCount立即更新
        cardTransform.SetParent(null);
        // 再销毁对象
        DestroyImmediate(cardTransform.gameObject);

        GF.LogInfo($"[2D座位{SeatIndex}] 移除手牌完成: 索引={cardIndex}, 牌值={actualCardValue}");
    }

    /// <summary>
    /// 碰吃杠手牌移除逻辑（参考3D实现）
    /// </summary>
    /// <param name="pcgType">碰吃杠类型</param>
    /// <param name="cardValues">碰吃杠的牌值数组</param>
    /// <param name="lastDaPaiValue">上家打出的牌值（用于吃牌判断，-1表示未知）</param>
    public void RemoveCardsForPengChiGang(PengChiGangPaiType pcgType, int[] cardValues, int lastDaPaiValue = -1)
    {
        if (cardValues == null || cardValues.Length == 0)
        {
            GF.LogWarning($"[2D座位{SeatIndex}] 碰吃杠移除手牌：牌值数组为空");
            return;
        }

        int targetValue = cardValues[0]; // 目标牌值
        int handRemoveCount = 0;          // 从手牌移除的数量
        int moPaiRemoveCount = 0;         // 从摸牌移除的数量

        // 根据不同的碰吃杠类型，确定移除策略
        switch (pcgType)
        {
            case PengChiGangPaiType.PENG:
                // 碰：手牌2张 + 别人打的1张 = 3张
                handRemoveCount = 2;
                moPaiRemoveCount = 0;
                break;

            case PengChiGangPaiType.CHI:
                // 吃：手牌2张 + 别人打的1张 = 3张
                handRemoveCount = 2;
                moPaiRemoveCount = 0;
                break;

            case PengChiGangPaiType.GANG:
                // 明杠：手牌3张 + 别人打的1张 = 4张
                handRemoveCount = 3;
                moPaiRemoveCount = 0;
                break;

            case PengChiGangPaiType.AN_GANG:
                // 暗杠：可能是手牌4张，或手牌3张+摸牌1张
                if (ContainsMoPaiWithValue(targetValue))
                {
                    // 情况1：手牌3张 + 摸牌1张
                    handRemoveCount = 3;
                    moPaiRemoveCount = 1;
                    GF.LogInfo($"[2D座位{SeatIndex}] 暗杠策略：手牌3张+摸牌1张");
                }
                else
                {
                    // 情况2：手牌4张（直接有4张或重连后摸牌已插入手牌）
                    handRemoveCount = 4;
                    moPaiRemoveCount = 0;
                    GF.LogInfo($"[2D座位{SeatIndex}] 暗杠策略：手牌4张");
                }
                break;

            case PengChiGangPaiType.BU_GANG:
                // 补杠：之前已碰，现在摸到第4张
                // 优先从摸牌区移除，如果摸牌区没有则从手牌移除
                if (ContainsMoPaiWithValue(targetValue))
                {
                    handRemoveCount = 0;
                    moPaiRemoveCount = 1;
                    GF.LogInfo($"[2D座位{SeatIndex}] 补杠策略：从摸牌区移除");
                }
                else
                {
                    handRemoveCount = 1;
                    moPaiRemoveCount = 0;
                    GF.LogInfo($"[2D座位{SeatIndex}] 补杠策略：从手牌移除（重连等特殊情况）");
                }
                break;

            case PengChiGangPaiType.CHAO_TIAN_GANG:
                // 朝天杠：别人打的赖根 + 手牌2张 = 3张（赖根天然少一张）
                handRemoveCount = 2;
                moPaiRemoveCount = 0;
                break;

            case PengChiGangPaiType.CHAO_TIAN_AN_GANG:
                // 朝天暗杠：可能是手牌3张，或手牌2张+摸牌1张
                if (ContainsMoPaiWithValue(targetValue))
                {
                    // 情况1：手牌2张 + 摸牌1张
                    handRemoveCount = 2;
                    moPaiRemoveCount = 1;
                    GF.LogInfo($"[2D座位{SeatIndex}] 朝天暗杠策略：手牌2张+摸牌1张");
                }
                else
                {
                    // 情况2：手牌3张（直接有3张或重连后摸牌已插入手牌）
                    handRemoveCount = 3;
                    moPaiRemoveCount = 0;
                    GF.LogInfo($"[2D座位{SeatIndex}] 朝天暗杠策略：手牌3张");
                }
                break;

            case PengChiGangPaiType.PI_GANG:
            case PengChiGangPaiType.LAIZI_GANG:
                // 皮杠、赖子杠：目前没有规则，暂不处理
                GF.LogInfo($"[2D座位{SeatIndex}] 特殊杠牌类型:{pcgType} 暂不处理");
                return;

            default:
                GF.LogWarning($"[2D座位{SeatIndex}] 未知碰吃杠类型:{pcgType}");
                return;
        }

        // 先移除摸牌区的牌
        int actualMoPaiRemoved = 0;
        if (moPaiRemoveCount > 0)
        {
            actualMoPaiRemoved = RemoveMoPaiByValue(targetValue, moPaiRemoveCount);
        }

        // 创建需要移除的手牌面值列表
        List<int> cardsToRemove = new List<int>();

        // 根据操作类型处理手牌移除
        if (pcgType == PengChiGangPaiType.CHI)
        {
            // 吃：需要移除除了别人打的牌之外的两张
            if (lastDaPaiValue == -1)
            {
                // 如果不知道上家打的是哪张，就按顺序移除
                int maxCount = Mathf.Min(handRemoveCount, cardValues.Length);
                for (int i = 0; i < maxCount; i++)
                {
                    cardsToRemove.Add(cardValues[i]);
                }
            }
            else
            {
                // 移除除了上家打的牌之外的其他牌
                foreach (int value in cardValues)
                {
                    if (value != lastDaPaiValue && cardsToRemove.Count < handRemoveCount)
                    {
                        cardsToRemove.Add(value);
                    }
                }
            }
        }
        else
        {
            // 碰、杠、暗杠、补杠、朝天杠、朝天暗杠：移除相同面值的牌
            for (int i = 0; i < handRemoveCount; i++)
            {
                cardsToRemove.Add(targetValue);
            }
        }

        int actualRemovedCount = RemoveHandCardsByValue(cardsToRemove);

        GF.LogInfo($"[2D座位{SeatIndex}] 碰吃杠移除完成 类型:{pcgType} 期望手牌:{handRemoveCount}张 实际手牌:{actualRemovedCount}张 期望摸牌:{moPaiRemoveCount}张 实际摸牌:{actualMoPaiRemoved}张 剩余手牌:{GetHandCardCount()}");
    }

    /// <summary>
    /// 检查摸牌区是否有指定面值的牌
    /// </summary>
    private bool ContainsMoPaiWithValue(int targetValue)
    {
        if (MoPaiContainer == null || MoPaiContainer.childCount == 0)
            return false;

        for (int i = 0; i < MoPaiContainer.childCount; i++)
        {
            GameObject cardObj = MoPaiContainer.GetChild(i).gameObject;
            MjCard_2D mjCard = cardObj.GetComponent<MjCard_2D>();
            if (mjCard != null && mjCard.cardValue == targetValue)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 从摸牌区移除指定面值的牌
    /// </summary>
    private int RemoveMoPaiByValue(int targetValue, int removeCount)
    {
        if (MoPaiContainer == null)
            return 0;

        int actualRemoved = 0;
        for (int i = MoPaiContainer.childCount - 1; i >= 0 && actualRemoved < removeCount; i--)
        {
            Transform cardTransform = MoPaiContainer.GetChild(i);
            MjCard_2D mjCard = cardTransform.GetComponent<MjCard_2D>();
            if (mjCard != null && mjCard.cardValue == targetValue)
            {
                // 先将对象移出父节点，确保childCount立即更新
                cardTransform.SetParent(null);
                DestroyImmediate(cardTransform.gameObject);
                actualRemoved++;
                GF.LogInfo($"[2D座位{SeatIndex}] 移除摸牌：牌值={targetValue}");
            }
        }
        return actualRemoved;
    }

    /// <summary>
    /// 从手牌中移除指定面值的牌（0号位专用）
    /// </summary>
    private int RemoveHandCardsByValue(List<int> cardValues)
    {
        if (HandContainer == null)
            return 0;

        int actualRemoved = 0;
        foreach (int valueToRemove in cardValues)
        {
            // 从后往前遍历，找到第一张匹配的牌并移除
            for (int i = HandContainer.childCount - 1; i >= 0; i--)
            {
                GameObject cardObj = HandContainer.GetChild(i).gameObject;
                MjCard_2D mjCard = cardObj.GetComponent<MjCard_2D>();
                if (mjCard != null && mjCard.cardValue == valueToRemove)
                {
                    DestroyImmediate(cardObj);
                    actualRemoved++;
                    GF.LogInfo($"[2D座位{SeatIndex}] 移除手牌：牌值={valueToRemove}");
                    break; // 只移除一张
                }
            }
        }
        return actualRemoved;
    }

    /// <summary>
    /// 从手牌中移除指定数量的牌（其他位置专用）
    /// </summary>
    private int RemoveHandCardsByCount(int removeCount)
    {
        if (HandContainer == null)
            return 0;

        int actualRemoved = 0;
        for (int i = 0; i < removeCount && HandContainer.childCount > 0; i++)
        {
            // 随机移除一张牌（因为其他位置我们看不到具体牌值）
            int randomIndex = UnityEngine.Random.Range(0, HandContainer.childCount);
            GameObject cardObj = HandContainer.GetChild(randomIndex).gameObject;
            DestroyImmediate(cardObj);
            actualRemoved++;
        }

        GF.LogInfo($"[2D座位{SeatIndex}] 随机移除手牌：数量={actualRemoved}");
        return actualRemoved;
    }

    #endregion

    #region 弃牌管理

    /// <summary>
    /// 创建弃牌显示
    /// </summary>
    public void CreateDiscardCards(List<int> discardCards)
    {
        ClearContainer(DiscardContainer);

        // 使用统一的属性判断是否需要插入到最前面
        bool insertAtFirst = NeedInsertDiscardAtFirst;

        // 服务器返回的弃牌列表是最新的在前，需要反转才能按时间顺序显示
        var reversedCards = new List<int>(discardCards);
        reversedCards.Reverse();

        // 逐张添加弃牌，保证层级关系正确
        // 对于2D视角，需要确保牌的创建顺序正确，避免后加载的牌遮挡先加载的牌
        for (int i = 0; i < reversedCards.Count; i++)
        {
            int cardValue = reversedCards[i];
            
            // 当弃牌超过两排时，重新从第一排开始垒起来
            int adjustedIndex = i;
            int totalCardsPerTwoRows = maxDiscardCardsPerRow * maxDiscardRows;
            Vector2 position;
            int layers = 0; // 记录当前牌的层数
            
            if (i >= totalCardsPerTwoRows)
            {
                // 计算在两排循环中的位置
                adjustedIndex = i % totalCardsPerTwoRows;
                
                // 为垒起来的牌添加垂直偏移，每完整两排增加一层高度
                layers = i / totalCardsPerTwoRows;
                Vector2 basePosition = CalculateCardPosition(adjustedIndex, objectDiscard);
                
                // 根据不同座位调整垂直偏移方向
                float verticalOffset = layers * 23f; // 每层向上偏移23个单位

                basePosition.y += verticalOffset;

                position = basePosition;
            }
            else
            {
                // 弃牌未超过两排，使用原始逻辑
                position = CalculateCardPosition(i, objectDiscard);
            }
            
            bool isLaizi = IsLaiZiCard(cardValue);
            
            // 创建牌对象
            GameObject cardObj = CreateCardWithPrefab(cardValue, DiscardContainer, objectDiscard, position, isLaizi, false);
            
            // 根据座位和层数调整牌的层级，确保上面的牌层不会被下面的牌层遮挡
            if (insertAtFirst)
            {
                // 对家(2)和右家(3)需要将新牌放在最前面
                if (layers > 0)
                {
                    // 对于垒起来的牌，需要确保层级正确
                    // 计算应该插入的位置，确保第二排的牌在第一排牌的后面
                    // 使用反向插入：第二排的牌应该插入到第一排牌的后面
                    int totalCardsBeforeCurrentLayer = (layers - 1) * totalCardsPerTwoRows;
                    int cardsInCurrentLayerBeforeCurrentCard = adjustedIndex;
                    int insertPosition = DiscardContainer.childCount - (totalCardsBeforeCurrentLayer + cardsInCurrentLayerBeforeCurrentCard + 1);
                    
                    // 确保插入位置在有效范围内
                    insertPosition = Mathf.Clamp(insertPosition, 0, DiscardContainer.childCount);
                    cardObj.transform.SetSiblingIndex(insertPosition);
                }
                else
                {
                    // 第一排的牌放在最前面
                    cardObj.transform.SetAsFirstSibling();
                }
            }
            else
            {
                // 自己(0)和左家(1)需要将新牌放在最后面
                cardObj.transform.SetAsLastSibling();
            }
        }

        // 更新已打出赖子数量显示
        UpdateLaiziCount();

        GF.LogInfo($"[2D座位{SeatIndex}] 创建弃牌完成，共{reversedCards.Count}张");
    }

    /// <summary>
    /// 添加一张弃牌
    /// </summary>
    public void AddDiscardCard(int cardValue)
    {
        int cardCount = DiscardContainer.childCount;
        
        // 当弃牌超过两排时，重新从第一排开始垒起来
        int adjustedIndex = cardCount;
        int totalCardsPerTwoRows = maxDiscardCardsPerRow * maxDiscardRows;

        GameObject cardObj = null;
        
        if (cardCount >= totalCardsPerTwoRows)
        {
            // 计算在两排循环中的位置
            adjustedIndex = cardCount % totalCardsPerTwoRows;
            
            // 为垒起来的牌添加垂直偏移，每完整两排增加一层高度
            int layers = cardCount / totalCardsPerTwoRows;
            Vector2 basePosition = CalculateCardPosition(adjustedIndex, objectDiscard);
            
            // 根据不同座位调整垂直偏移方向
            float verticalOffset = layers * 23f; // 每排增加23f的垂直偏移

            // 向上偏移（修正方向）
            basePosition.y += verticalOffset;

            // 使用调整后的位置
            Vector2 position = basePosition;
            
            bool isLaizi = IsLaiZiCard(cardValue);
            
            // 创建牌对象，不使用insertAtFirst参数
            cardObj = CreateCardWithPrefab(cardValue, DiscardContainer, objectDiscard, position, isLaizi, false);
            
            // 根据座位和层数调整牌的层级，确保上面的牌层不会被下面的牌层遮挡
            if (NeedInsertDiscardAtFirst)
            {
                // 对家(2)和右家(3)需要将新牌放在最前面
                if (layers > 0)
                {
                    // 对于垒起来的牌，需要确保层级正确
                    // 计算应该插入的位置，确保第二排的牌在第一排牌的后面
                    // 使用反向插入：第二排的牌应该插入到第一排牌的后面
                    int totalCardsBeforeCurrentLayer = (layers - 1) * totalCardsPerTwoRows;
                    int cardsInCurrentLayerBeforeCurrentCard = adjustedIndex;
                    int insertPosition = DiscardContainer.childCount - (totalCardsBeforeCurrentLayer + cardsInCurrentLayerBeforeCurrentCard + 1);
                    
                    // 确保插入位置在有效范围内
                    insertPosition = Mathf.Clamp(insertPosition, 0, DiscardContainer.childCount);
                    cardObj.transform.SetSiblingIndex(insertPosition);
                }
                else
                {
                    // 第一排的牌放在最前面
                    cardObj.transform.SetAsFirstSibling();
                }
            }
            else
            {
                // 自己(0)和左家(1)需要将新牌放在最后面
                cardObj.transform.SetAsLastSibling();
            }
        }
        else
        {
            // 弃牌未超过两排，使用原始逻辑
            Vector2 position = CalculateCardPosition(cardCount, objectDiscard);
            
            bool isLaizi = IsLaiZiCard(cardValue);
            
            // 使用统一的属性判断是否需要插入到最前面
            bool insertAtFirst = NeedInsertDiscardAtFirst;
            CreateCardWithPrefab(cardValue, DiscardContainer, objectDiscard, position, isLaizi, insertAtFirst);
        }

        // 更新已打出赖子数量显示
        UpdateLaiziCount();

        GF.LogInfo($"[2D座位{SeatIndex}] 添加弃牌: {cardValue}");
    }

    /// <summary>
    /// 获取最后一张弃牌的世界坐标位置（用于显示弃牌标记）
    /// </summary>
    /// <returns>最后一张弃牌的世界坐标，如果没有弃牌返回Vector3.zero</returns>
    public Vector3 GetLastDiscardCardPosition()
    {
        if (DiscardContainer == null || DiscardContainer.childCount == 0)
        {
            return Vector3.zero;
        }

        // 获取最后一张弃牌（最新添加的牌）
        // 使用统一方法获取最新索引，考虑倒序插入的情况
        int newestIndex = GetNewestDiscardIndex();
        if (newestIndex < 0)
        {
            return Vector3.zero;
        }

        Transform lastCard = DiscardContainer.GetChild(newestIndex);
        if (lastCard != null)
        {
            return lastCard.position;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// 根据牌值获取指定弃牌的世界坐标位置（用于显示弃牌标记）
    /// </summary>
    /// <param name="cardValue">要查找的牌值</param>
    /// <returns>找到的弃牌的世界坐标，如果没找到返回最后一张弃牌位置</returns>
    public Vector3 GetDiscardCardPosition(MahjongFaceValue cardValue)
    {
        if (DiscardContainer == null || DiscardContainer.childCount == 0)
        {
            return Vector3.zero;
        }

        int serverCardValue = Util.ConvertFaceValueToServerInt(cardValue);

        // 从最新到最旧查找弃牌
        // 考虑倒序插入：右家和对家新牌在前(索引0开始)，其他座位新牌在后(从后往前)
        if (NeedInsertDiscardAtFirst)
        {
            // 新牌在前面，从前往后查找
            for (int i = 0; i < DiscardContainer.childCount; i++)
            {
                Transform cardTrans = DiscardContainer.GetChild(i);
                if (cardTrans == null) continue;

                MjCard_2D mjCard = cardTrans.GetComponent<MjCard_2D>();
                if (mjCard != null && mjCard.cardValue == serverCardValue)
                {
                    return cardTrans.position;
                }
            }
        }
        else
        {
            // 新牌在后面，从后往前查找
            for (int i = DiscardContainer.childCount - 1; i >= 0; i--)
            {
                Transform cardTrans = DiscardContainer.GetChild(i);
                if (cardTrans == null) continue;

                MjCard_2D mjCard = cardTrans.GetComponent<MjCard_2D>();
                if (mjCard != null && mjCard.cardValue == serverCardValue)
                {
                    return cardTrans.position;
                }
            }
        }

        // 如果没找到指定牌值，返回最后一张弃牌的位置
        return GetLastDiscardCardPosition();
    }

    /// <summary>
    /// 获取下一张弃牌应该放置的世界坐标位置（用于出牌动画的目标位置）
    /// </summary>
    /// <returns>下一张弃牌的世界坐标</returns>
    public Vector3 GetNextDiscardCardPosition()
    {
        if (DiscardContainer == null)
        {
            return Vector3.zero;
        }

        // 计算下一张弃牌的本地位置
        int nextIndex = DiscardContainer.childCount;
        
        // 当弃牌超过两排时，重新从第一排开始垒起来
        int adjustedIndex = nextIndex;
        int totalCardsPerTwoRows = maxDiscardCardsPerRow * maxDiscardRows;
        Vector2 localPosition;
        
        if (nextIndex >= totalCardsPerTwoRows)
        {
            // 计算在两排循环中的位置
            adjustedIndex = nextIndex % totalCardsPerTwoRows;
            
            // 为垒起来的牌添加垂直偏移，每完整两排增加一层高度
            int layers = nextIndex / totalCardsPerTwoRows;
            Vector2 basePosition = CalculateCardPosition(adjustedIndex, objectDiscard);
            
            // 根据不同座位调整垂直偏移方向
            float verticalOffset = layers * 23f; // 每排增加23f的垂直偏移

            basePosition.y += verticalOffset;

            localPosition = basePosition;
        }
        else
        {
            // 弃牌未超过两排，使用原始逻辑
            localPosition = CalculateCardPosition(nextIndex, objectDiscard);
        }

        // 将本地位置转换为世界坐标
        // 使用 RectTransform 的 TransformPoint 方法
        RectTransform containerRect = DiscardContainer.GetComponent<RectTransform>();
        if (containerRect != null)
        {
            return containerRect.TransformPoint(localPosition);
        }

        return DiscardContainer.TransformPoint(localPosition);
    }

    #endregion

    #region 吃碰杠管理

    /// <summary>
    /// 创建吃碰杠牌组显示
    /// </summary>
    public void CreateMeldGroups(List<List<int>> meldGroups)
    {
        ClearContainer(MeldContainer);

        // 使用统一的属性判断是否需要插入到最前面
        bool insertAtFirst = NeedInsertMeldAtFirst;

        int cardIndex = 0;
        for (int groupIndex = 0; groupIndex < meldGroups.Count; groupIndex++)
        {
            var group = meldGroups[groupIndex];

            for (int i = 0; i < group.Count; i++)
            {
                Vector2 position = CalculateMeldCardPosition(cardIndex, objectMeld, i);
                bool isLaizi = IsLaiZiCard(group[i]);

                // 右家的杠牌第4张不执行insertAtFirst,让它自然盖在前3张上
                bool shouldInsertFirst = insertAtFirst && !(i == 3);
                CreateCardWithPrefab(group[i], MeldContainer, objectMeld, position, isLaizi, shouldInsertFirst);

                // 只有4张杠牌的第4张不占用新位置
                if (i < 3)
                {
                    cardIndex++;
                }
            }
            // 组与组之间增加间距
            cardIndex += 1;
        }

        GF.LogInfo($"[2D座位{SeatIndex}] 创建吃碰杠完成，共{meldGroups.Count}组");
    }

    /// <summary>
    /// 添加一组吃碰杠牌
    /// </summary>
    /// <summary>
    /// 添加一组吃碰杠牌（自动判断类型）
    /// </summary>
    /// <param name="meldGroup">牌值列表</param>
    public void AddMeldGroup(List<int> meldGroup)
    {
        // 根据牌数自动判断类型
        PengChiGangPaiType meldType = PengChiGangPaiType.PENG; // 默认碰

        if (meldGroup.Count == 4)
        {
            // 4张牌，判断是杠
            meldType = PengChiGangPaiType.GANG;
        }
        else if (meldGroup.Count == 3)
        {
            // 3张牌，判断是碰还是吃
            bool allSame = meldGroup[0] == meldGroup[1] && meldGroup[1] == meldGroup[2];
            meldType = allSame ? PengChiGangPaiType.PENG : PengChiGangPaiType.CHI;
        }

        // 调用带类型的重载方法
        AddMeldGroup(meldType, meldGroup);
    }

    /// <summary>
    /// 添加一组吃碰杠牌（重连恢复专用）
    /// </summary>
    /// <param name="meldType">吃碰杠类型</param>
    /// <param name="cards">牌值列表</param>
    /// <summary>
    /// 添加吃碰杠组牌（带类型检测和逻辑优化）
    /// </summary>
    /// <param name="meldType">吃碰杠类型</param>
    /// <param name="cards">牌值列表</param>
    public void AddMeldGroup(PengChiGangPaiType meldType, List<int> cards)
    {
        // 补杠特殊处理：需要找到原来的碰牌并升级
        if (meldType == PengChiGangPaiType.BU_GANG)
        {
            AddBuGangCard(cards[0]);
            return;
        }

        // 获取当前组牌区已有的牌数（用于计算新牌的起始位置）
        int currentCardCount = MeldContainer.childCount;

        // 朝天杠和朝天暗杠只显示3张牌（赖根少一张）
        int displayCardCount = cards.Count;
        if (meldType == PengChiGangPaiType.CHAO_TIAN_GANG || meldType == PengChiGangPaiType.CHAO_TIAN_AN_GANG)
        {
            displayCardCount = 3;
            GF.LogInfo($"[2D座位{SeatIndex}] {GetMeldTypeText(meldType)}特殊处理：只显示3张牌");
        }

        // 判断是否是杠牌（4张）
        bool isGang = (cards.Count == 4);

        // 右家(3号位)需要反序插入节点
        bool insertAtFirst = NeedInsertMeldAtFirst;

        // 计算逻辑位置索引（杠牌只占3个逻辑位置，第4张不占位）
        int logicalPositionIndex = CalculateLogicalMeldPositions();

        GF.LogInfo($"[2D座位{SeatIndex}] 添加{GetMeldTypeText(meldType)} - 牌数:{cards.Count}, 显示牌数:{displayCardCount}, 当前逻辑位置:{logicalPositionIndex}, 反序插入:{insertAtFirst}");

        // 遍历牌组中的每张牌（朝天杠只遍历前3张）
        for (int i = 0; i < displayCardCount; i++)
        {
            int cardValue = cards[i];
            int cardLogicalIndex = logicalPositionIndex + (i < 3 ? i : 2); // 杠牌第4张使用第3张的逻辑位置

            // 判断是否是杠牌的第4张
            int cardIndexInGroup = (isGang && i == 3) ? 3 : i;

            // 计算牌的位置
            Vector2 position = CalculateMeldCardPosition(cardLogicalIndex, objectMeld, cardIndexInGroup);

            // 判断是否为赖子
            bool isLaizi = IsLaiZiCard(cardValue);

            // 右家(3号位)的杠牌第4张需要特殊处理节点顺序
            // 第4张需要在中间牌后面（节点索引更大），这样第4张后渲染遮挡中间牌
            bool shouldInsertFirst = insertAtFirst;
            if (isGang && i == 3 && SeatIndex == 3)
            {
                // 右家杠牌第4张：不插到最前面
                shouldInsertFirst = false;
                GF.LogInfo($"[2D座位{SeatIndex}] 杠牌第4张，shouldInsertFirst设为false");
            }

            GameObject cardObj = CreateCardWithPrefab(cardValue, MeldContainer, objectMeld, position, isLaizi, shouldInsertFirst);

            // 右家杠牌第4张需要调整节点顺序：直接移到最后
            if (isGang && i == 3 && SeatIndex == 3 && cardObj != null)
            {
                int beforeIndex = cardObj.transform.GetSiblingIndex();
                // 将第4张设置到最后位置，后渲染遮挡前面的牌
                cardObj.transform.SetAsLastSibling();
                int afterIndex = cardObj.transform.GetSiblingIndex();
                GF.LogInfo($"[2D座位{SeatIndex}] 右家杠牌第4张移到最后 - 调整前索引:{beforeIndex}, 调整后索引:{afterIndex}, 容器总数:{MeldContainer.childCount}");
            }
        }

        string typeText = GetMeldTypeText(meldType);
        GF.LogInfo($"[2D座位{SeatIndex}] 添加{typeText}组完成，牌数: {cards.Count}");
    }

    /// <summary>
    /// 补杠：在已有的碰牌基础上添加一张牌
    /// 采用统计所有牌组数据、清空重新生成的方案，避免位置顺序问题
    /// </summary>
    /// <param name="cardValue">补杠的牌值</param>
    public void AddBuGangCard(int cardValue)
    {
        if (MeldContainer == null)
        {
            GF.LogWarning($"[2D座位{SeatIndex}] 补杠失败：MeldContainer为空");
            return;
        }

        // 1. 统计所有现有的牌组数据
        List<List<int>> allMeldGroups = new List<List<int>>();
        List<int> currentGroup = new List<int>();
        int lastCardValue = -1;

        // 右家(3号位)的牌是反序插入的(最新的在节点最前面)，需要反向遍历以获取正确的逻辑顺序
        bool isRightSeat = (SeatIndex == 3);
        int startIdx = isRightSeat ? MeldContainer.childCount - 1 : 0;
        int endIdx = isRightSeat ? -1 : MeldContainer.childCount;
        int step = isRightSeat ? -1 : 1;

        for (int i = startIdx; i != endIdx; i += step)
        {
            Transform child = MeldContainer.GetChild(i);
            MjCard_2D mjCard = child.GetComponent<MjCard_2D>();

            if (mjCard != null)
            {
                int currentCardValue = mjCard.cardValue;

                // 如果当前牌与上一张牌相同，继续累积到当前组
                if (currentCardValue == lastCardValue || lastCardValue == -1)
                {
                    currentGroup.Add(currentCardValue);
                    lastCardValue = currentCardValue;
                }
                else
                {
                    // 遇到不同的牌，保存当前组，开始新组
                    if (currentGroup.Count > 0)
                    {
                        allMeldGroups.Add(new List<int>(currentGroup));
                    }
                    currentGroup.Clear();
                    currentGroup.Add(currentCardValue);
                    lastCardValue = currentCardValue;
                }
            }
        }

        // 保存最后一组
        if (currentGroup.Count > 0)
        {
            allMeldGroups.Add(currentGroup);
        }

        GF.LogInfo($"[2D座位{SeatIndex}] 补杠前统计：共{allMeldGroups.Count}组牌");

        // 2. 从后往前查找需要补杠的碰牌组（3张）
        bool foundPengGroup = false;
        for (int i = allMeldGroups.Count - 1; i >= 0; i--)
        {
            var group = allMeldGroups[i];
            // 找到最后一组碰牌（3张相同牌值）
            if (group.Count == 3 && group[0] == cardValue)
            {
                // 将碰牌升级为杠牌（添加第4张）
                group.Add(cardValue);
                foundPengGroup = true;
                GF.LogInfo($"[2D座位{SeatIndex}] 找到碰牌组（索引{i}），升级为杠牌：牌值={cardValue}，升级后={string.Join(",", group)}");
                break;
            }
        }

        if (!foundPengGroup)
        {
            GF.LogWarning($"[2D座位{SeatIndex}] 补杠失败：未找到对应碰牌组（牌值={cardValue}）");
            return;
        }

        // 3. 清空MeldContainer
        ClearContainer(MeldContainer);

        // 4. 重新生成所有牌组
        GF.LogInfo($"[2D座位{SeatIndex}] 开始重建牌组：共{allMeldGroups.Count}组");
        for (int groupIdx = 0; groupIdx < allMeldGroups.Count; groupIdx++)
        {
            var group = allMeldGroups[groupIdx];
            GF.LogInfo($"[2D座位{SeatIndex}] 重建牌组{groupIdx}：{group.Count}张，牌值=[{string.Join(",", group)}]");
            AddMeldGroup(group);
        }

        GF.LogInfo($"[2D座位{SeatIndex}] 补杠成功：清空重建完成，总共{allMeldGroups.Count}组牌");
    }

    /// <summary>
    /// 移除最后一组吃碰杠牌（用于撤销亮牌等操作）
    /// </summary>
    public void RemoveLastMeldGroup()
    {
        if (MeldContainer == null || MeldContainer.childCount == 0)
        {
            GF.LogWarning($"[2D座位{SeatIndex}] RemoveLastMeldGroup失败：MeldContainer为空或没有牌");
            return;
        }

        // 从后往前找到最后一组牌（通过牌值判断）
        int lastCardValue = -1;
        List<Transform> cardsToRemove = new List<Transform>();
        
        // 右家(3号位)需要从前往后找第一组
        bool isRightSeat = (SeatIndex == 3);
        
        if (isRightSeat)
        {
            // 右家：从前往后找第一组
            for (int i = 0; i < MeldContainer.childCount; i++)
            {
                Transform child = MeldContainer.GetChild(i);
                MjCard_2D mjCard = child.GetComponent<MjCard_2D>();
                if (mjCard != null)
                {
                    if (lastCardValue == -1)
                    {
                        lastCardValue = mjCard.cardValue;
                        cardsToRemove.Add(child);
                    }
                    else if (mjCard.cardValue == lastCardValue)
                    {
                        cardsToRemove.Add(child);
                    }
                    else
                    {
                        // 遇到不同牌值，第一组结束
                        break;
                    }
                }
            }
        }
        else
        {
            // 其他座位：从后往前找最后一组
            for (int i = MeldContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = MeldContainer.GetChild(i);
                MjCard_2D mjCard = child.GetComponent<MjCard_2D>();
                if (mjCard != null)
                {
                    if (lastCardValue == -1)
                    {
                        lastCardValue = mjCard.cardValue;
                        cardsToRemove.Add(child);
                    }
                    else if (mjCard.cardValue == lastCardValue)
                    {
                        cardsToRemove.Add(child);
                    }
                    else
                    {
                        // 遇到不同牌值，最后一组结束
                        break;
                    }
                }
            }
        }

        // 删除找到的牌
        foreach (Transform card in cardsToRemove)
        {
            Object.Destroy(card.gameObject);
        }

        GF.LogInfo($"[2D座位{SeatIndex}] 移除最后一组牌：{cardsToRemove.Count}张，牌值={lastCardValue}");
    }

    /// <summary>
    /// 获取吃碰杠类型的中文文本
    /// </summary>
    private string GetMeldTypeText(PengChiGangPaiType meldType)
    {
        switch (meldType)
        {
            case PengChiGangPaiType.PENG: return "碰";
            case PengChiGangPaiType.CHI: return "吃";
            case PengChiGangPaiType.GANG: return "杠";
            case PengChiGangPaiType.AN_GANG: return "暗杠";
            case PengChiGangPaiType.BU_GANG: return "补杠";
            case PengChiGangPaiType.PI_GANG: return "皮杠";
            case PengChiGangPaiType.LAIZI_GANG: return "癞子杠";
            default: return meldType.ToString();
        }
    }

    /// <summary>
    /// 显示胡牌手牌（复用MeldContainer，不动现有吃碰杠组牌，只在后面添加手牌）
    /// </summary>
    /// <param name="handCards">手牌列表（来自胡牌或流局协议的otherCard.Cards）</param>
    public void ShowWinHandCards(List<int> handCards)
    {
        // 清空手牌区和摸牌区，但保留MeldContainer中的吃碰杠组牌
        ClearContainer(HandContainer);
        ClearContainer(MoPaiContainer);

        // 回放模式：使用赖子值进行排序（只对自己座位排序）
        List<int> sortedCards =Util.SortMahjongHandCards(handCards, msg_playback.Laizi);

        // 计算当前MeldContainer中已占用的逻辑位置数量
        // 需要统计实际的牌组数量（吃=3张，碰=3张，杠=4张，但杠的第4张不占用额外位置）
        int logicalPositionCount = CalculateLogicalMeldPositions();

        // 右家需要插入到最前面(被旧牌遮挡),其他座位添加到最后(遮挡旧牌)
        bool insertAtFirst = (SeatIndex == 3);

        // 在MeldContainer末尾继续添加手牌（按规范布局，类似亮牌效果）
        for (int i = 0; i < sortedCards.Count; i++)
        {
            // 计算手牌在组牌区的位置（紧接在已有牌组后面）
            int handCardLogicalIndex = logicalPositionCount + i;
            Vector2 position = CalculateMeldCardPosition(handCardLogicalIndex, objectMeld, i);

            // 判断是否为赖子
            bool isLaizi = IsLaiZiCard(sortedCards[i]);

            CreateCardWithPrefab(sortedCards[i], MeldContainer, objectMeld, position, isLaizi, insertAtFirst);
        }

        GF.LogInfo($"[2D座位{SeatIndex}] 胡牌亮牌: 保留现有牌组，已占用逻辑位置{logicalPositionCount}个，添加手牌{sortedCards.Count}张");
    }

    /// <summary>
    /// 显示胡牌手牌（完全重建版本）- 根据Msg_Hu数据完全重新生成组牌区
    /// </summary>
    /// <param name="melds">组牌数据（来自otherCard.Melds）</param>
    /// <param name="handCards">手牌列表（来自otherCard.Cards，已排序，不含最后摸牌）</param>
    /// <param name="lastMoCard">最后摸的牌（自摸/杠开时单独显示），0表示没有</param>
    public void ShowWinHandCardsWithMelds(Google.Protobuf.Collections.RepeatedField<Msg_Melds> melds, List<int> handCards, int lastMoCard = 0)
    {
        // 【步骤1】清空所有容器，完全重建
        ClearContainer(HandContainer);
        ClearContainer(MoPaiContainer);
        ClearContainer(MeldContainer);  // 关键：清空组牌区

        GF.LogInfo($"[2D座位{SeatIndex}] 开始完全重建胡牌展示 - 组牌数:{melds?.Count ?? 0}, 手牌数:{handCards.Count}, 最后摸牌:{lastMoCard}");

        // 【步骤2】重建组牌区（根据melds数据）
        int logicalPositionCount = 0;
        if (melds != null && melds.Count > 0)
        {
            // 右家(3号位)需要反序插入
            bool insertAtFirst = NeedInsertMeldAtFirst;

            foreach (var meld in melds)
            {
                if (meld.Card?.Val != null && meld.Card.Val.Count > 0)
                {
                    List<int> meldCards = new List<int>(meld.Card.Val);

                    // 根据牌的数量判断是吃(3)、碰(3)、杠(4)
                    bool isGang = meldCards.Count == 4;

                    GF.LogInfo($"[2D座位{SeatIndex}] 重建组牌 - 类型:{(isGang ? "杠" : "碰/吃")}, 牌数:{meldCards.Count}, 牌值:[{string.Join(",", meldCards)}]");

                    // 创建组牌中的每张牌
                    for (int i = 0; i < meldCards.Count; i++)
                    {
                        int cardValue = meldCards[i];
                        int cardLogicalIndex = logicalPositionCount + (i < 3 ? i : 2); // 杠牌第4张使用第3张的逻辑位置

                        // 判断是否是杠牌的第4张
                        int cardIndexInGroup = (isGang && i == 3) ? 3 : i;

                        // 计算牌的位置
                        Vector2 position = CalculateMeldCardPosition(cardLogicalIndex, objectMeld, cardIndexInGroup);

                        // 判断是否为赖子
                        bool isLaizi = IsLaiZiCard(cardValue);

                        // 右家(3号位)的杠牌第4张需要特殊处理节点顺序
                        bool shouldInsertFirst = insertAtFirst;
                        if (isGang && i == 3 && SeatIndex == 3)
                        {
                            shouldInsertFirst = false; // 第4张不插到最前面
                            GF.LogInfo($"[2D座位{SeatIndex}] 胡牌-杠牌第4张，shouldInsertFirst设为false");
                        }

                        GameObject cardObj = CreateCardWithPrefab(cardValue, MeldContainer, objectMeld, position, isLaizi, shouldInsertFirst);

                        // 右家杠牌第4张需要调整节点顺序：直接移到最后
                        if (isGang && i == 3 && SeatIndex == 3 && cardObj != null)
                        {
                            int beforeIndex = cardObj.transform.GetSiblingIndex();
                            // 将第4张设置到最后位置，后渲染遮挡前面的牌
                            cardObj.transform.SetAsLastSibling();
                            int afterIndex = cardObj.transform.GetSiblingIndex();
                            GF.LogInfo($"[2D座位{SeatIndex}] 胡牌-右家杠牌第4张移到最后 - 调整前:{beforeIndex}, 调整后:{afterIndex}, 总数:{MeldContainer.childCount}");
                        }
                    }

                    // 更新逻辑位置计数（杠牌只占3个逻辑位置）
                    logicalPositionCount += 3;
                }
            }
        }

        // 【步骤3】在组牌区后面继续添加手牌
        bool insertHandAtFirst = NeedInsertMeldAtFirst;
        for (int i = 0; i < handCards.Count; i++)
        {
            int handCardLogicalIndex = logicalPositionCount + i;
            // 手牌不会触发杠牌特殊布局，cardIndexInGroup固定为0（表示正常排列）
            Vector2 position = CalculateMeldCardPosition(handCardLogicalIndex, objectMeld, 0);

            // 判断是否为赖子
            bool isLaizi = IsLaiZiCard(handCards[i]);

            CreateCardWithPrefab(handCards[i], MeldContainer, objectMeld, position, isLaizi, insertHandAtFirst);
        }

        // 【步骤4】如果是自摸或杠开，单独显示最后摸的牌（放在摸牌区，与手牌有间距）
        if (lastMoCard != 0)
        {
            // 计算摸牌的位置（在手牌最后，增加额外间距）
            int moCardLogicalIndex = logicalPositionCount + handCards.Count;

            // 使用摸牌的间距规则（比手牌间距更大）
            Vector2 moPosition = CalculateMoPaiPosition(moCardLogicalIndex);

            // 判断是否为赖子
            bool isMoLaizi = IsLaiZiCard(lastMoCard);

            // 在摸牌区显示（使用MeldContainer，但位置计算特殊）
            CreateCardWithPrefab(lastMoCard, MeldContainer, objectMeld, moPosition, isMoLaizi, insertHandAtFirst);

            GF.LogInfo($"[2D座位{SeatIndex}] 单独显示最后摸牌:{lastMoCard}, 位置:{moPosition}, 是否赖子:{isMoLaizi}");
        }

        GF.LogInfo($"[2D座位{SeatIndex}] 完全重建胡牌展示完成 - 组牌逻辑位置:{logicalPositionCount}, 手牌数:{handCards.Count}, 最后摸牌:{lastMoCard}");
    }

    /// <summary>
    /// 计算摸牌的位置（在组牌区后面，增加额外间距）
    /// </summary>
    private Vector2 CalculateMoPaiPosition(int logicalIndex)
    {
        // 基础位置计算（复用组牌区的位置计算）
        Vector2 basePosition = CalculateMeldCardPosition(logicalIndex, objectMeld, 0);

        // 根据座位方向增加额外间距
        float extraGap = 30f;  // 额外间距（可根据需要调整）

        switch (SeatIndex)
        {
            case 0: // 下家：水平向右，增加右侧间距
                basePosition.x += extraGap;
                break;
            case 1: // 左家：垂直向下，增加下方间距
                basePosition.y -= extraGap;
                break;
            case 2: // 对家：水平向左，增加左侧间距
                basePosition.x -= extraGap;
                break;
            case 3: // 右家：垂直向上，增加上方间距
                basePosition.y += extraGap;
                break;
        }

        return basePosition;
    }

    /// <summary>
    /// 计算MeldContainer中已占用的逻辑位置数量
    /// 杠牌的第4张不占用额外逻辑位置，所以需要特殊计算
    /// </summary>
    private int CalculateLogicalMeldPositions()
    {
        if (MeldContainer == null || MeldContainer.childCount == 0)
            return 0;

        // 统计已有牌组的逻辑位置占用
        // 简化处理：假设每3张牌占用3个逻辑位置，杠牌的第4张不额外占用
        int totalCards = MeldContainer.childCount;
        int logicalPositions = 0;

        // 按牌组统计：每组3-4张牌，前3张占用3个逻辑位置
        int processedCards = 0;
        while (processedCards < totalCards)
        {
            // 每组至少3张牌，最多4张牌（杠牌）
            int groupSize = Mathf.Min(4, totalCards - processedCards);

            // 检查是否是杠牌组（4张相同）
            bool isGangGroup = false;
            if (groupSize == 4 && processedCards + 3 < totalCards)
            {
                // 检查是否4张牌都相同（简单检查前3张和第4张）
                var card1 = MeldContainer.GetChild(processedCards).GetComponent<MjCard_2D>();
                var card4 = MeldContainer.GetChild(processedCards + 3).GetComponent<MjCard_2D>();
                if (card1 != null && card4 != null && card1.cardValue == card4.cardValue)
                {
                    isGangGroup = true;
                }
            }

            if (isGangGroup)
            {
                // 杠牌组：4张牌只占用3个逻辑位置
                logicalPositions += 3;
                processedCards += 4;
            }
            else
            {
                // 吃/碰牌组：3张牌占用3个逻辑位置
                logicalPositions += 3;
                processedCards += 3;
            }
        }

        return logicalPositions;
    }

    public void ShowScoreChange(double score)
    {
        // 金额为0时不显示特效
        if (score == 0)
        {
            return;
        }
        
        string effPath = score >= 0 ? AssetsPath.GetPrefab("UI/MaJiang/Game/ScoreWinEff") : AssetsPath.GetPrefab("UI/MaJiang/Game/ScoreLoseEff");

        // 创建得分特效对象
        GF.UI.LoadPrefab(effPath, (gameObject) =>
        {
            if (gameObject == null)
            {
                GF.LogWarning($"[2D座位{SeatIndex}] 加载得分特效失败: {effPath}");
                return;
            }
            GameObject effectObj = Instantiate(gameObject as GameObject, EffContainer);

            // 获取 Text 组件并设置分数文本
            Text scoreText = effectObj.GetComponent<Text>();
            if (scoreText == null)
            {
                GF.LogWarning($"[2D座位{SeatIndex}] 得分特效对象没有 Text 组件");
                Destroy(effectObj);
                return;
            }

            // 设置分数文本
            string scoreStr = score >= 0 ? $"+{score}" : $"{score}";
            scoreText.text = scoreStr;

            // 设置初始位置和状态
            RectTransform rectTransform = effectObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 固定位置在 -50px
                rectTransform.anchoredPosition = Vector3.zero;
                // 初始缩放为较大（盖章效果的起始状态）
                rectTransform.localScale = Vector3.one * 1.5f;
                rectTransform.localRotation = Quaternion.identity;
            }

            effectObj.SetActive(true);

            // DOTween 动画：盖章效果 + 停留 + 向上渐隐
            if (rectTransform != null && scoreText != null)
            {
                Sequence sequence = DOTween.Sequence();
                sequence.SetTarget(effectObj);  // 修复：使用 effectObj 而不是 gameObject
                sequence.SetAutoKill(true);     // 确保动画完成后自动清理

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
            else
            {
                // 如果没有 RectTransform，2秒后销毁
                Destroy(effectObj, 2f);
            }
        });

        GF.LogInfo($"[2D座位{SeatIndex}] 显示得分特效: {score}");
    }

    /// <summary>
    /// 显示得分特效（简单版 - 无盖章效果）
    /// </summary>
    public void ShowScoreChangeSimple(double score)
    {
        // 金额为0时不显示特效
        if (score == 0)
        {
            return;
        }
        
        string effPath = score >= 0 ? AssetsPath.GetPrefab("UI/MaJiang/Game/ScoreWinEff") : AssetsPath.GetPrefab("UI/MaJiang/Game/ScoreLoseEff");

        // 创建得分特效对象
        GF.UI.LoadPrefab(effPath, (gameObject) =>
        {
            if (gameObject == null)
            {
                GF.LogWarning($"[2D座位{SeatIndex}] 加载得分特效失败: {effPath}");
                return;
            }
            GameObject effectObj = Instantiate(gameObject as GameObject, EffContainer);

            // 获取 Text 组件并设置分数文本
            Text scoreText = effectObj.GetComponent<Text>();
            if (scoreText == null)
            {
                GF.LogWarning($"[2D座位{SeatIndex}] 得分特效对象没有 Text 组件");
                Destroy(effectObj);
                return;
            }

            // 设置分数文本
            string scoreStr = score >= 0 ? $"+{score}" : $"{score}";
            scoreText.text = scoreStr;

            // 设置初始位置和状态
            RectTransform rectTransform = effectObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 起始位置在 -50px
                rectTransform.anchoredPosition = new Vector2(0, -50f);
            }

            effectObj.SetActive(true);

            // DOTween 动画：简单版 - 停留 + 向上渐隐
            if (rectTransform != null && scoreText != null)
            {
                Sequence sequence = DOTween.Sequence();
                sequence.SetTarget(effectObj);  // 修复：使用 effectObj 而不是 gameObject
                sequence.SetAutoKill(true);     // 确保动画完成后自动清理

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
            else
            {
                // 如果没有 RectTransform，2秒后销毁
                Destroy(effectObj, 2f);
            }
        });

        GF.LogInfo($"[2D座位{SeatIndex}] 显示得分特效(简单版): {score}");
    }

    #endregion

    #region 清理和辅助方法

    /// <summary>
    /// 清理所有显示
    /// </summary>
    public void ClearGameGos()
    {
        ShowBankerIcon(false);
        TingPaiIcon.SetActive(false);
        UpdateLaiziText(0);
        SetPiaoFenDisplay(-1);
        ClearContainer(HandContainer);
        ClearContainer(MoPaiContainer);
        ClearContainer(DiscardContainer);
        ClearContainer(MeldContainer);
        ClearContainer(EffContainer);
        ClearHuContainer();
        SetReadyState(false);
    }

    /// <summary>
    /// 清空胡牌容器
    /// </summary>
    public void ClearHuContainer()
    {
        if (HuContainer != null)
        {
            ClearContainer(HuContainer);
        }
    }

    /// <summary>
    /// 获取当前手牌数量
    /// </summary>
    public int GetHandCardCount()
    {
        return HandContainer != null ? HandContainer.childCount : 0;
    }

    /// <summary>
    /// 获取手牌数据（返回牌值列表）
    /// </summary>
    /// <returns>手牌的牌值列表（不包括摸牌区）</returns>
    public List<MahjongFaceValue> GetHandCardsData()
    {
        List<MahjongFaceValue> handCards = new List<MahjongFaceValue>();

        if (HandContainer == null)
        {
            return handCards;
        }

        // 遍历手牌容器中的所有牌
        for (int i = 0; i < HandContainer.childCount; i++)
        {
            Transform cardTrans = HandContainer.GetChild(i);
            if (cardTrans == null) continue;

            // 获取牌值组件
            MjCard_2D mjCard = cardTrans.GetComponent<MjCard_2D>();
            if (mjCard != null)
            {
                // 直接使用mahjongFaceValue属性，已经是正确的MahjongFaceValue
                handCards.Add(mjCard.mahjongFaceValue);
            }
        }

        return handCards;
    }

    /// <summary>
    /// 获取摸牌数据（只有一张或没有）
    /// </summary>
    /// <returns>摸牌的牌值，没有摸牌返回MJ_UNKNOWN</returns>
    public MahjongFaceValue GetMoPaiData()
    {
        if (MoPaiContainer == null || MoPaiContainer.childCount == 0)
        {
            return MahjongFaceValue.MJ_UNKNOWN;
        }

        // 摸牌区应该只有一张牌
        Transform cardTrans = MoPaiContainer.GetChild(0);
        if (cardTrans == null) return MahjongFaceValue.MJ_UNKNOWN;

        // 获取牌值组件
        MjCard_2D mjCard = cardTrans.GetComponent<MjCard_2D>();
        if (mjCard != null)
        {
            return mjCard.mahjongFaceValue;
        }

        return MahjongFaceValue.MJ_UNKNOWN;
    }

    /// <summary>
    /// 获取完整的牌数据（手牌+摸牌）
    /// </summary>
    /// <returns>包含手牌和摸牌的完整牌值列表</returns>
    public List<MahjongFaceValue> GetAllCardsData()
    {
        List<MahjongFaceValue> allCards = GetHandCardsData();

        // 添加摸牌（如果有的话）
        MahjongFaceValue mopaiValue = GetMoPaiData();
        if (mopaiValue != MahjongFaceValue.MJ_UNKNOWN)
        {
            allCards.Add(mopaiValue);
        }

        return allCards;
    }

    /// <summary>
    /// 根据牌值在手牌中查找索引
    /// </summary>
    /// <param name="cardValue">要查找的牌值</param>
    /// <returns>找到返回索引，未找到返回-1</returns>
    public int FindHandCardIndex(int cardValue)
    {
        for (int i = 0; i < HandContainer.childCount; i++)
        {
            GameObject cardObj = HandContainer.GetChild(i).gameObject;
            MjCard_2D mjCard = cardObj.GetComponent<MjCard_2D>();

            if (mjCard != null && mjCard.cardValue == cardValue)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 确保摸牌区补位（如果轮到打牌但摸牌区没牌，将手牌最后一张移动到摸牌区）
    /// 参考3D的 EnsureSelfMoPaiSlotIfNeeded 逻辑
    /// 用于碰吃杠后，视觉上提示轮到该玩家操作
    /// </summary>
    public void EnsureMoPaiSlot()
    {
        // 获取手牌逻辑上最后一张（考虑左家倒序的情况）
        int lastHandIdx = GetLogicalLastHandCardIndex();

        Transform lastHandCard = HandContainer.GetChild(lastHandIdx);

        MjCard_2D mjCard = lastHandCard.GetComponent<MjCard_2D>();
        int movedCardValue = mjCard != null ? mjCard.cardValue : -1;

        // 将最后一张手牌移动到摸牌区
        lastHandCard.SetParent(MoPaiContainer, false);

        // 设置摸牌的位置
        RectTransform rectTransform = lastHandCard.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = Vector2.zero;
        }

        // 重新排列剩余手牌的位置
        // 节点顺序不变，只更新位置
        RearrangeHandCardsPosition(skipNodeReorder: true);
    }

    /// <summary>
    /// /// 整理手牌（打牌后调用）
    /// 智能插入摸牌到合适位置，最少改动坐标
    /// ?? 注意：此方法仅操作UI层，需要外部调用者同步Manager数据
    /// </summary>
    public void ArrangeHandCards()
    {
        GF.LogInfo($"[2D座位{SeatIndex}] 开始整理手牌 - 手牌数:{HandContainer?.childCount ?? 0} 摸牌数:{MoPaiContainer?.childCount ?? 0}");

        // 检查摸牌区是否有牌
        if (MoPaiContainer.childCount > 0)
        {
            // 有摸牌：移入手牌区并整理
            Transform moPaiCard = MoPaiContainer.GetChild(0);
            ArrangeHandCardsForLocalPlayer(moPaiCard);
        }
        else
        {
            // 没有摸牌：只需要消除空位（打牌后留下的空位）
            // 节点顺序不变，只更新位置
            RearrangeHandCardsPosition(skipNodeReorder: true);
        }
    }

    /// <summary>
    /// 整理手牌：智能插入摸牌到合适位置
    /// </summary>
    private void ArrangeHandCardsForLocalPlayer(Transform moPaiCard)
    {
        MjCard_2D moPaiComponent = moPaiCard.GetComponent<MjCard_2D>();
        int moPaiValue = moPaiComponent.cardValue;
        int handCount = HandContainer.childCount;

        // 步骤1：查找合适的插入位置（数据逻辑位置）
        int dataInsertIndex = FindInsertPosition(moPaiValue, handCount);

        // 步骤2：对于1号位，需要将数据索引转换为节点索引
        // 因为1号位节点反向：数据索引0 → 节点索引n-1，数据索引n-1 → 节点索引0
        int nodeInsertIndex = dataInsertIndex;
        if (NeedReverseHandNodeOrder)
        {
            // 1号位：将数据索引转换为节点索引
            nodeInsertIndex = handCount - dataInsertIndex;
        }

        GF.LogInfo($"[2D座位{SeatIndex}] 整理: 手牌{handCount}张, 摸牌值={moPaiValue}, 数据插入位置={dataInsertIndex}, 节点插入位置={nodeInsertIndex}");

        // 步骤3：将摸牌移入手牌区并调整节点索引
        moPaiCard.SetParent(HandContainer, false);
        moPaiCard.SetSiblingIndex(nodeInsertIndex);

        // 步骤4：统一重新计算所有手牌的位置坐标
        // 注意：此时节点顺序已经正确，不需要再次重排序
        RearrangeHandCardsPosition(skipNodeReorder: true);
    }

    /// <summary>
    /// 查找摸牌的插入位置（保持手牌有序）
    /// 返回的是数据逻辑索引，不是节点索引
    /// 规则：
    /// - 0号位（自己）：小牌在前（索引小），赖子最前，大牌在后（索引大）
    /// - 1号位（左家）、2号位（对家）、3号位（右家）：数据已逆序，大牌在前，小牌在后
    /// </summary>
    private int FindInsertPosition(int moPaiValue, int handCount)
    {
        // 1号位、2号位、3号位的手牌数据是逆序的（大牌在前）
        bool isReversedData = (SeatIndex == 1 || SeatIndex == 2 || SeatIndex == 3);

        if (isReversedData)
        {
            // 1号位、2号位、3号位：数据逆序，大牌在前（索引小），小牌在后（索引大）
            // 赖子插到最后面（索引最大，显示在最右边/最上面）
            if (IsLaiZiCard(moPaiValue))
            {
                return handCount; // 插到末尾
            }

            // 从前往后遍历手牌（数据逻辑顺序），找到第一个 < moPaiValue 的位置，插入到它前面
            // 因为手牌是降序排列（大牌在前），所以找第一个比摸牌小的牌
            // 注意：遇到赖子时，应该返回赖子的位置（插到赖子前面）
            for (int i = 0; i < handCount; i++)
            {
                // 对于1号位，需要将数据索引转换为节点索引来获取牌值
                int nodeIndex = NeedReverseHandNodeOrder ? (handCount - 1 - i) : i;
                Transform cardTransform = HandContainer.GetChild(nodeIndex);
                MjCard_2D cardComponent = cardTransform.GetComponent<MjCard_2D>();

                if (cardComponent != null)
                {
                    int cardValue = cardComponent.cardValue;

                    // 遇到赖子，插到赖子前面
                    if (IsLaiZiCard(cardValue))
                    {
                        return i;
                    }

                    // 找到第一个比摸牌小的牌，插入到它前面
                    if (cardValue < moPaiValue)
                    {
                        return i;
                    }
                }
            }

            // 所有牌都 >= moPaiValue 且没有赖子，插到末尾
            return handCount;
        }
        else
        {
            // 0号位：正常顺序，小牌在前（索引小），大牌在后（索引大）
            // 赖子插到最前面（索引0，显示在最左边）
            if (IsLaiZiCard(moPaiValue))
            {
                return 0;
            }

            // 从前往后遍历手牌，找到第一个 > moPaiValue 的位置，插入到它前面
            // 因为手牌是升序排列（小牌在前），所以找第一个比摸牌大的牌
            for (int i = 0; i < handCount; i++)
            {
                Transform cardTransform = HandContainer.GetChild(i);
                MjCard_2D cardComponent = cardTransform.GetComponent<MjCard_2D>();

                if (cardComponent != null)
                {
                    int cardValue = cardComponent.cardValue;

                    // 跳过赖子（赖子永远在最前面）
                    if (IsLaiZiCard(cardValue))
                    {
                        continue;
                    }

                    // 找到第一个比摸牌大的牌，插入到它前面
                    if (cardValue > moPaiValue)
                    {
                        return i;
                    }
                }
            }

            // 所有非赖子牌都 <= moPaiValue，插到末尾（最大的位置）
            return handCount;
        }
    }


    /// <summary>
    /// 重新排列手牌位置和节点顺序（统一处理方法）
    /// 用于所有需要调整手牌位置的场景：打牌后整理、碰吃杠后补位等
    /// </summary>
    /// <param name="skipNodeReorder">是否跳过节点重排序（默认false）。当节点顺序已经正确时设为true</param>
    private void RearrangeHandCardsPosition(bool skipNodeReorder = false)
    {
        int handCount = HandContainer.childCount;

        // 左家(座位1)特殊处理：需要反向调整节点顺序，保证渲染层级正确
        // 原因：左家垂直从下到上排列，下面的牌应该遮挡上面的牌
        // 节点索引0 → 位置最下方(y最小，渲染层级最高，遮挡其他牌)
        // 节点索引n-1 → 位置最上方(y最大，渲染层级最低，被遮挡)
        // 注意：只在创建手牌时需要重排序，整理手牌时节点顺序已经正确，不需要再次重排
        if (NeedReverseHandNodeOrder && !skipNodeReorder)
        {
            // 从后往前遍历，将节点按反向顺序重新排列
            for (int i = handCount - 1; i >= 0; i--)
            {
                Transform cardTransform = HandContainer.GetChild(i);
                if (cardTransform != null)
                {
                    // 将当前节点移到最后，实现反向排列
                    cardTransform.SetAsLastSibling();
                }
            }
        }

        // 更新所有手牌的位置和交互组件
        for (int i = 0; i < handCount; i++)
        {
            Transform cardTransform = HandContainer.GetChild(i);
            if (cardTransform != null)
            {
                RectTransform rectTransform = cardTransform.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // 使用统一属性判断是否需要反向索引计算位置
                    int posIndex = NeedReverseHandNodeOrder ? (handCount - 1 - i) : i;
                    Vector2 newPosition = CalculateCardPosition(posIndex, handCount, objectHand);
                    rectTransform.anchoredPosition = newPosition;
                }

            }
        }

        GF.LogInfo($"[2D座位{SeatIndex}] 重新排列手牌位置: 共{handCount}张，节点重排={!skipNodeReorder}");
    }

    /// <summary>
    /// 清理指定容器
    /// </summary>
    public void ClearContainer(Transform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform childTransform = container.GetChild(i);
            if (childTransform != null)
            {
                // 先将对象移出父节点，确保childCount立即更新
                childTransform.SetParent(null);
                // 再销毁对象
                DestroyImmediate(childTransform.gameObject);
            }
        }
    }

    /// <summary>
    /// 使用指定预制体创建单张牌
    /// </summary>
    /// <param name="insertAtFirst">是否插入到容器第一个位置(用于右家弃牌,实现被遮挡效果)</param>
    private GameObject CreateCardWithPrefab(int cardValue, Transform container, GameObject prefab, Vector2 position, bool isLaizi = false, bool insertAtFirst = false)
    {
        GameObject cardObj = Instantiate(prefab, container);
        if (cardObj == null) return null;

        cardObj.SetActive(true);

        // 如果需要插入到最前面,调整同级索引
        if (insertAtFirst)
        {
            cardObj.transform.SetAsFirstSibling();
        }

        RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = cardObj.AddComponent<RectTransform>();
        }

        rectTransform.anchoredPosition = position;
        rectTransform.localScale = Vector3.one;

        // 设置牌值
        MjCard_2D mjCard = cardObj.GetComponent<MjCard_2D>();
        if (mjCard != null)
        {
            mjCard.SetCardValue(cardValue, isLaizi);
        }

        return cardObj;
    }

    /// <summary>
    /// 使用指定预制体创建牌组（整理手牌时，最后一张牌x=0为基准）
    /// </summary>
    private void CreateCardsWithPrefab(List<int> cards, Transform container, GameObject prefab)
    {
        // 左家(座位1)特殊处理:垂直显示从下到上,需要反向加载
        // - 先加载index大的牌(y坐标大,位置靠上,渲染层级低,被遮挡)
        // - 后加载index小的牌(y坐标小,位置靠下,渲染层级高,遮挡其他牌)
        // - 例如: 先加载index=12(y=480,最上),最后加载index=0(y=0,最下)
        bool needReverse = NeedReverseHandNodeOrder && container == HandContainer;

        for (int i = 0; i < cards.Count; i++)
        {
            // 左家手牌反向遍历: 从后往前取
            int index = needReverse ? (cards.Count - 1 - i) : i;

            Vector2 position = CalculateCardPosition(index, cards.Count, prefab);

            bool isLaizi = IsLaiZiCard(cards[index]);

            GameObject cardObj = CreateCardWithPrefab(cards[index], container, prefab, position, isLaizi);

        }
    }

    /// <summary>
    /// 手牌出牌回调
    /// </summary>
    /// <param name="cardIndex">牌索引</param>
    /// <param name="cardValue">牌值</param>
    /// <param name="isMoPai">是否为摸牌</param>
    private void OnCardPlay(int cardIndex, int cardValue, bool isMoPai)
    {
        HandPaiType paiType = isMoPai ? HandPaiType.MoPai : HandPaiType.HandPai;
        GF.LogInfo($"[2D座位{SeatIndex}] 玩家出牌: 索引={cardIndex}, 牌值={cardValue}, 类型={paiType}");

        // 回放模式：不需要通知游戏UI
        // mjGameUI.OnPlayerPlayCard(SeatIndex, cardIndex, cardValue, paiType);
    }

    /// <summary>
    /// 计算牌的位置（整理手牌时，根据座位偏移方向计算）
    /// 自己座位(case 0)：水平排列，靠右对齐
    /// - index=0（赖子/最小牌） → x最负（最左边）
    /// - index=n-1（最大牌） → x=0（最右边，对齐起点）
    /// </summary>
    public Vector2 CalculateCardPosition(int index, int totalCount, GameObject cardPrefab = null)
    {
        // 如果没有指定预制体，默认使用手牌预制体
        if (cardPrefab == null)
        {
            cardPrefab = objectHand;
        }

        // 获取牌的尺寸
        RectTransform rectTransform = cardPrefab.GetComponent<RectTransform>();
        float cardWidth = rectTransform.sizeDelta.x;

        float xOffset = 0f;
        float yOffset = 0f;
        switch (SeatIndex)
        {
            case 0: // 自己座位(下方) - 靠右对齐，index=n-1在x=0
                xOffset = -((totalCount - 1 - index) * (cardWidth + intervalHand));
                break;
            case 1: // 左家 - 垂直排列，从下到上
                yOffset = index * intervalHand;
                break;
            case 2: // 对家(上方) - 水平排列，从左向右
                xOffset = index * (cardWidth + intervalHand);
                break;
            case 3: // 右家 - 垂直排列，从上到下
                yOffset = -index * intervalHand;
                break;
            default:
                break;
        }
        return new Vector2(xOffset, yOffset);
    }

    /// <summary>
    /// 计算牌的位置（重载方法，用于单张牌添加）
    /// </summary>
    public Vector2 CalculateCardPosition(int index, GameObject cardPrefab = null)
    {
        // 如果没有指定预制体，默认使用手牌预制体
        if (cardPrefab == null)
        {
            cardPrefab = objectHand;
        }

        // 获取牌的尺寸
        RectTransform rectTransform = cardPrefab.GetComponent<RectTransform>();
        float cardWidth = rectTransform.sizeDelta.x;
        float cardHeight = rectTransform.sizeDelta.y;

        float xOffset = 0f;
        float yOffset = 0f;

        if (cardPrefab == objectDiscard)
        {
            // 弃牌需要换行处理
            int row = index / maxDiscardCardsPerRow;  // 第几行
            int col = index % maxDiscardCardsPerRow;  // 该行第几列

            switch (SeatIndex)
            {
                case 0: // 自己座位(下方) - 水平排列，从左向右，换行向下
                    xOffset = col * cardWidth;
                    yOffset = -row * intervalDiscard;
                    break;
                case 1: // 左家 - 垂直排列，从上到下，换行向左
                    // 新牌y坐标更小(在下面)，后添加会遮挡上面的旧牌
                    xOffset = -row * cardWidth;
                    yOffset = -col * intervalDiscard;
                    break;
                case 2: // 对家(上方) - 水平排列，从右向左，换行向上
                    // 新牌y坐标更大(在上面)，通过SetAsFirstSibling让新牌遮挡旧牌
                    xOffset = -col * cardWidth;
                    yOffset = row * intervalDiscard;
                    break;
                case 3: // 右家 - 垂直排列，从下到上，换行向右
                    // 新牌y坐标更大(在上面)，通过SetAsFirstSibling被下面的旧牌遮挡
                    xOffset = row * cardWidth;
                    yOffset = col * intervalDiscard;
                    break;
                default:
                    break;
            }
        }
        else if (cardPrefab == objectHand)
        {
            // 手牌不换行，直接排列（带间隙）
            switch (SeatIndex)
            {
                case 0: // 自己座位(下方) - 水平排列，从右向左
                    xOffset = -index * (cardWidth + intervalHand);
                    break;
                case 1: // 左家 - 垂直排列，从下到上
                    yOffset = index * intervalHand;
                    break;
                case 2: // 对家(上方) - 水平排列，从左向右
                    xOffset = index * (cardWidth + intervalHand);
                    break;
                case 3: // 右家 - 垂直排列，从上到下
                    yOffset = -index * intervalHand;
                    break;
                default:
                    break;
            }
        }
        else if (cardPrefab == objectMeld)
        {
            // 吃碰杠牌不换行，使用intervalMeld间隙
            switch (SeatIndex)
            {
                case 0: // 自己座位(下方) - 水平排列，从左向右
                    xOffset = index * cardWidth;
                    break;
                case 1: // 左家 - 垂直排列，从上到下（与弃牌方向一致）
                    yOffset = -index * intervalMeld;
                    break;
                case 2: // 对家(上方) - 水平排列，从右向左
                    xOffset = -index * cardWidth;
                    break;
                case 3: // 右家 - 垂直排列，从下到上（与弃牌方向一致）
                    yOffset = index * intervalMeld;
                    break;
                default:
                    break;
            }
        }



        return new Vector2(xOffset, yOffset);
    }

    /// <summary>
    /// 计算吃碰杠牌的位置（支持4张杠牌特殊排列：下面三张,中间上方一张）
    /// </summary>
    /// <summary>
    /// 计算组牌区的牌位置（统一处理吃碰杠和胡牌展示）
    /// </summary>
    /// <param name="logicalIndex">逻辑索引（在组牌区中的逻辑位置，杠牌只占3个逻辑位置）</param>
    /// <param name="cardPrefab">牌预制体</param>
    /// <param name="cardIndexInGroup">当前牌在本组中的索引(0-3)，为3时表示是杠牌的第4张</param>
    /// <returns>牌的本地坐标位置</returns>
    private Vector2 CalculateMeldCardPosition(int logicalIndex, GameObject cardPrefab, int cardIndexInGroup)
    {
        // 获取牌的尺寸
        RectTransform rectTransform = cardPrefab.GetComponent<RectTransform>();
        float cardWidth = rectTransform.sizeDelta.x;
        float cardHeight = rectTransform.sizeDelta.y;

        float xOffset = 0f;
        float yOffset = 0f;

        // 杠牌第4张特殊处理：放在中间牌(组内索引1)的上方
        if (cardIndexInGroup == 3)
        {
            // 计算中间牌的逻辑索引（当前逻辑索引 - 1，因为第4张复用第3张的逻辑位置）
            int middleCardLogicalIndex = logicalIndex - 1;

            switch (SeatIndex)
            {
                case 0: // 自己(下家) - 水平向右 X+
                    // 中间牌的x位置
                    xOffset = middleCardLogicalIndex * cardWidth;
                    // 第4张在中间牌上方
                    yOffset = cardHeight * 0.2f;
                    break;

                case 1: // 上家(左家) - 垂直向下 Y-
                    xOffset = 0;
                    // 中间牌的y位置（负值）
                    yOffset = -middleCardLogicalIndex * intervalMeld;
                    // 第4张在中间牌上方（加正值让y变大）
                    yOffset += cardWidth * 0.25f;
                    break;

                case 2: // 对家 - 水平向左 X-
                    // 中间牌的x位置（负值）
                    xOffset = -middleCardLogicalIndex * cardWidth;
                    // 第4张在中间牌上方
                    yOffset = cardHeight * 0.2f;
                    break;

                case 3: // 下家(右家) - 垂直向上 Y+
                    xOffset = 0;
                    // 中间牌的y位置（正值）
                    yOffset = middleCardLogicalIndex * intervalMeld;
                    // 第4张在中间牌上方（Y+，加正值让y变大）
                    yOffset += cardWidth * 0.25f;
                    break;
            }
        }
        else
        {
            // 普通牌（吃、碰、杠的前3张）按逻辑索引正常排列
            switch (SeatIndex)
            {
                case 0: // 自己(下家) - 水平向右 X+
                    xOffset = logicalIndex * cardWidth;
                    yOffset = 0;
                    break;

                case 1: // 上家(左家) - 垂直向下 Y-
                    xOffset = 0;
                    yOffset = -logicalIndex * intervalMeld;
                    break;

                case 2: // 对家 - 水平向左 X-
                    xOffset = -logicalIndex * cardWidth;
                    yOffset = 0;
                    break;

                case 3: // 下家(右家) - 垂直向上 Y+
                    xOffset = 0;
                    yOffset = logicalIndex * intervalMeld;
                    break;
            }
        }

        return new Vector2(xOffset, yOffset);
    }


    #endregion

    #region 玩家信息更新

    /// <summary>
    /// 更新玩家完整信息
    /// </summary>
    /// <param name="deskPlayer">玩家数据</param>
    /// <param name="isDealer">是否为庄家</param>
    /// <param name="windPosition">风位（东南西北）</param>
    public void UpdatePlayerInfo(DeskPlayer deskPlayer, FengWei windPosition)
    {
        if (deskPlayer == null)
        {
            ClearSeat(); // 清空信息
            gameObject.SetActive(false);
            return;
        }
        this.deskPlayer = deskPlayer;
        Util.DownloadHeadImage(PlayerAvatar, deskPlayer.BasePlayer.HeadImage);
        PlayerNickname.text = deskPlayer.BasePlayer.Nick;
        PlayerCoin.text = deskPlayer.Coin.ToString();
        // 回放模式：暂不显示庄家图标（需要从回放数据传入）
        // ShowBankerIcon(deskPlayer.BasePlayer.PlayerId == mjGameUI.mj_procedure.enterMJDeskRs.Banker);
        WindText.text = GetWindText(windPosition);
        // 更新赖子数量
        UpdateLaiziCount();
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 更新玩家积分显示
    /// </summary>
    /// <param name="scoreChange">积分变化值</param>
    public void UpdatePlayerCoin(double scoreChange)
    {
        if (deskPlayer != null)
        {
            // 解析当前积分
            if (double.TryParse(deskPlayer.Coin, out double currentCoin))
            {
                // 更新积分
                currentCoin += scoreChange;
                deskPlayer.Coin = currentCoin.ToString();
                
                // 更新UI显示
                PlayerCoin.text = deskPlayer.Coin;
                
                GF.LogInfo($"[2D座位{SeatIndex}] 更新玩家积分: {scoreChange:F2}, 当前积分: {currentCoin:F2}");
            }
            else
            {
                GF.LogWarning($"[2D座位{SeatIndex}] 解析玩家积分失败: {deskPlayer.Coin}");
            }
        }
        else
        {
            GF.LogWarning($"[2D座位{SeatIndex}] 无法更新积分 - deskPlayer为null");
        }
    }

    /// <summary>
    /// 更新赖子数量显示（根据座位头像UI统计当前座位已打出的赖子数）
    /// 同时更新 laiziDiscardCount 供锁牌判定使用
    /// </summary>
    public void UpdateLaiziCount()
    {
        int laiziCount = CountLaiziInContainer(DiscardContainer);
        laiziDiscardCount = laiziCount;
        UpdateLaiziText(laiziCount);
    }

    public void UpdateLaiziText(int laiziCount = 0)
    {
        LaiNum.text = laiziCount == 0 ? "" : "赖" + laiziCount.ToString();
        LaiNum.transform.parent.gameObject.SetActive(laiziCount > 0);
    }

    /// <summary>
    /// 统计指定容器中的赖子数量
    /// </summary>
    /// <param name="container">牌容器</param>
    /// <returns>赖子数量</returns>
    private int CountLaiziInContainer(Transform container)
    {
        int count = 0;
        for (int i = 0; i < container.childCount; i++)
        {
            Transform child = container.GetChild(i);

            // 获取牌组件
            MjCard_2D cardComponent = child.GetComponent<MjCard_2D>();
            if (cardComponent != null)
            {
                // 检查赖子标记是否激活
                if (cardComponent.mark != null && cardComponent.mark.activeSelf)
                {
                    count++;
                }
                else if (cardComponent.laizi != null && cardComponent.laizi.activeSelf)
                {
                    count++;
                }
            }
        }

        return count;
    }

    public void ShowBankerIcon(bool isShow, bool isAnim = false)
    {
        if (isShow)
        {
            if (isAnim)
            {
                // 激活图标
                BankerIcon.SetActive(true);

                // 保存目标位置（世界坐标）
                Vector3 targetPos = BankerIcon.transform.position;

                // 设置起始位置为中心（世界坐标）
                Vector3 startPos = transform.position;
                BankerIcon.transform.position = startPos;

                // 执行动画：从中心移动到庄家座位
                BankerIcon.transform.DOMove(targetPos, 0.5f).SetEase(Ease.OutBounce);
            }
            else
            {
                BankerIcon.SetActive(true);
            }
        }
        else
        {
            BankerIcon.SetActive(false);
        }
    }

    /// <summary>
    /// 清空玩家信息（玩家离开时）
    /// </summary>
    public void ClearSeat()
    {
        PlayerNickname.text = "";
        PlayerCoin.text = "";
        WindText.text = "";
        ClearGameGos();
    }

    /// <summary>
    /// 将风位枚举转换为中文文本
    /// </summary>
    private string GetWindText(FengWei feng)
    {
        switch (feng)
        {
            case FengWei.EAST: return "东";
            case FengWei.SOUTH: return "南";
            case FengWei.WEST: return "西";
            case FengWei.NORTH: return "北";
            default: return "";
        }
    }

    #endregion

    #region 特效管理

    /// <summary>
    /// 在特效容器中显示特效
    /// </summary>
    /// <param name="effectType">特效类型</param>
    /// <param name="duration">特效持续时间（秒），-1表示不自动销毁</param>
    /// <returns>创建的特效GameObject</returns>
    public void ShowEffect(string effectType, float duration, PengChiGangPaiType meldType = PengChiGangPaiType.GANG)
    {
        string effPath = "";
        string animationName = "animation";

        // 血流成河、卡五星等玩法使用统一的特效预制体
        bool isKWX = (msg_playback?.DeskCommon?.MjConfig?.MjMethod == MJMethod.Kwx) || 
                     (msg_playback?.DeskCommon?.MjConfig?.MjMethod == MJMethod.Xl);

        // 血流成河、卡五星等玩法：尝试使用专用方法处理
        if (isKWX && TryGetKWXEffectInfo(effectType, out effPath, out animationName))
        {
            // 已通过卡五星专用方法获取特效信息
        }
        else
        {
            // 非卡五星玩法或通用特效
            switch (effectType)
            {
                case "PiaoLaizi":
                    effPath = AssetsPath.GetPrefab("UI/MaJiang/Game/PiaoLaiziEff");
                    break;
                case "Peng":
                    effPath = AssetsPath.GetPrefab("UI/MaJiang/Game/PengEff");
                    break;
                case "Chi":
                    effPath = AssetsPath.GetPrefab("UI/MaJiang/Game/ChiEff");
                    break;
                case "Gang":
                    if (msg_playback.DeskCommon.MjConfig.MjMethod == MJMethod.Huanghuang)
                    {
                        effPath = AssetsPath.GetPrefab("UI/MaJiang/Game/XiaoEff");
                    }
                    else
                    {
                        effPath = AssetsPath.GetPrefab("UI/MaJiang/Game/GangEff");
                    }
                    break;
                case "HeiMo":
                    effPath = AssetsPath.GetPrefab("UI/MaJiang/Game/HeiMoEff");
                    break;
                case "RuanMo":
                    effPath = AssetsPath.GetPrefab("UI/MaJiang/Game/RuanMoEff");
                    break;
                case "ZhuoChong":
                    effPath = AssetsPath.GetPrefab("UI/MaJiang/Game/ZhuoChongEff");
                    break;
                case "GangKai":
                    effPath = AssetsPath.GetPrefab("UI/MaJiang/Game/GangKaiEff");
                    animationName = "hu_gangshangkaihua";
                    break;
                default:
                    GF.LogWarning($"[回放座位{SeatIndex}] 未知特效类型: {effectType}");
                    return;
            }
        }

        if (string.IsNullOrEmpty(effPath))
        {
            GF.LogWarning($"[回放座位{SeatIndex}] 特效路径为空: {effectType}");
            return;
        }

        PlayEffectAnimation(effPath, animationName, effectType, duration, meldType);
    }

    /// <summary>
    /// 血流成河、卡五星等玩法使用统一的特效预制体和命名规则
    /// </summary>
    /// <param name="effectType">特效类型</param>
    /// <param name="effPath">输出：特效预制体路径</param>
    /// <param name="animationName">输出：动画名称</param>
    /// <returns>是否成功获取（true=使用卡五星特效，false=使用通用特效）</returns>
    private bool TryGetKWXEffectInfo(string effectType, out string effPath, out string animationName)
    {
        effPath = "";
        animationName = "animation";

        // 卡五星统一使用 PCGHEff_KWX 预制体
        string kwxEffPath = AssetsPath.GetPrefab("UI/MaJiang/Game/PCGHEff_KWX");

        // 根据特效类型获取动画名称
        string kwxAnimName = GameUtil.GetKWXAnimationName(effectType);

        if (!string.IsNullOrEmpty(kwxAnimName))
        {
            effPath = kwxEffPath;
            animationName = kwxAnimName;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 播放特效动画（内部方法）
    /// </summary>
    private void PlayEffectAnimation(string effPath, string animationName, string effectType, float duration, PengChiGangPaiType meldType)
    {
        GF.UI.LoadPrefab(effPath, (gameObject) =>
        {
            if (gameObject == null)
            {
                GF.LogWarning($"[回放座位{SeatIndex}] 实例化特效失败: {effectType}");
                return;
            }
            GameObject effectObj = Instantiate(gameObject as GameObject, EffContainer);

            // 设置特效位置（归零）
            RectTransform rectTransform = effectObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
            }

            effectObj.SetActive(true);
            var spineAni = effectObj.GetComponent<Spine.Unity.SkeletonAnimation>();
            //播放默认循环动画
            spineAni.AnimationState.SetAnimation(0, animationName, false);

            if (Sound.IsSfxEnabled())
            {
                PlayMeldSound(effectType, meldType);
            }

            // GF.LogInfo($"[回放座位{SeatIndex}] 显示特效: {effectType}, 动画: {animationName}");

            // 如果设置了持续时间，自动销毁
            if (duration > 0)
            {
                Destroy(effectObj, duration);
            }
        });
    }

    /// <summary>
    /// 播放吃碰杠音效
    /// </summary>
    public void PlayMeldSound(string effectType, PengChiGangPaiType meldType = PengChiGangPaiType.GANG)
    {
        string actionName = null;
        int variation = 0;

        switch (effectType)
        {
            case "Peng":
                actionName = "peng";
                Sound.PlayEffect(AudioKeys.MJ_EFFECT_AUDIO_PENG);
                break;
            case "Chi":
                actionName = "chi";
                break;
            case "Gang":
                switch (meldType)
                {
                    case PengChiGangPaiType.GANG:
                        actionName = "gang";
                        variation = Random.Range(0, 2);
                        break;
                    case PengChiGangPaiType.AN_GANG:
                        actionName = "angang";
                        break;
                    case PengChiGangPaiType.BU_GANG:
                        actionName = "xugang";
                        break;
                    case PengChiGangPaiType.CHAO_TIAN_GANG:
                        actionName = "xiaochaotian";
                        break;
                    case PengChiGangPaiType.CHAO_TIAN_AN_GANG:
                        actionName = "chaogang";
                        break;
                }
                break;
            case "HeiMo":
                actionName = "heimo";
                break;
            case "RuanMo":
                actionName = "hu";
                break;
            case "ZiMo":
                actionName = "zimo";
                break;
            case "Hu":
                actionName = "hu";
                break;
            case "ZhuoChong":
                actionName = "rechong";
                Sound.PlayEffect(AudioKeys.MJ_EFFECT_AUDIO_ZHUOCHONG);
                break;
            case "GangKai":
                actionName = "gangshangkaihua";
                break;
        }

        if (!string.IsNullOrEmpty(actionName))
        {
            MahjongAudio.PlayActionVoiceForPlayer(actionName, deskPlayer.BasePlayer.PlayerId, msg_playback.DeskCommon.MjConfig.MjMethod, variation);
            return;
        }

        switch (effectType)
        {
            case "PiaoLaizi":
                actionName = AudioKeys.MJ_EFFECT_AUDIO_PIAOLAIZI;
                break;
        }

        if (!string.IsNullOrEmpty(actionName))
        {
            MahjongAudio.PlayVoice(actionName);
        }
    }

    #endregion

    #region DOTween 动画管理

    /// <summary>
    /// 组件销毁时，停止并清理所有与此 GameObject 关联的 DOTween 动画
    /// </summary>
    private void OnDestroy()
    {
        // 杀死所有与此 GameObject 关联的 DOTween 动画
        DOTween.Kill(gameObject);
    }

    #endregion

    #region  卡五星特殊处理
    [Header("===== 卡五星显示 =====")]
    public HuPaiTips HuPaiTips; // 胡牌提示(亮牌需要显示胡牌信息)

    // 亮牌状态
    private List<int> liangPaiCards = new List<int>(); // 亮出的牌列表

    /// <summary>
    /// 显示听牌图标
    /// </summary>
    public void ShowTingPaiIcon(bool show)
    {
        if (TingPaiIcon != null)
        {
            // TingPaiIcon.SetActive(show);
        }
    }

    /// <summary>
    /// 显示胡牌提示
    /// </summary>
    public void ShowHuPaiTips(List<HuPaiTipsInfo> huPaiInfos)
    {
        if (HuPaiTips == null)
        {
            GF.LogWarning($"[2D座位{SeatIndex}] HuPaiTips 未配置");
            return;
        }

        if (huPaiInfos == null || huPaiInfos.Count == 0)
        {
            HuPaiTips.gameObject.SetActive(false);
            return;
        }

        HuPaiTips.gameObject.SetActive(true);
        HuPaiTips.Show(huPaiInfos.ToArray());
        
        GF.LogInfo($"[2D座位{SeatIndex}] 显示胡牌提示，共{huPaiInfos.Count}种");
    }

    /// <summary>
    /// 隐藏胡牌提示
    /// </summary>
    public void HideHuPaiTips()
    {
        if (HuPaiTips != null)
        {
            HuPaiTips.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 获取当前手牌列表
    /// </summary>
    public List<int> GetCurrentHandCards()
    {
        List<int> cards = new List<int>();
        
        if (HandContainer != null)
        {
            for (int i = 0; i < HandContainer.childCount; i++)
            {
                Transform cardTransform = HandContainer.GetChild(i);
                MjCard_2D mjCard = cardTransform.GetComponent<MjCard_2D>();
                if (mjCard != null)
                {
                    cards.Add(mjCard.cardValue);
                }
            }
        }
        
        return cards;
    }

    /// <summary>
    /// 显示亮牌效果（指定的牌正常显示，其他牌变暗）
    /// </summary>
    /// <param name="liangCards">需要亮出的牌（保持正常亮度）</param>
    public void ShowLiangPaiEffect(List<int> liangCards)
    {
        liangPaiCards = new List<int>(liangCards);
        
        // 复制一份用于匹配
        List<int> remainingLiangCards = new List<int>(liangCards);
        
        // 遍历手牌，未亮的牌变暗
        if (HandContainer != null)
        {
            for (int i = 0; i < HandContainer.childCount; i++)
            {
                Transform cardTransform = HandContainer.GetChild(i);
                MjCard_2D mjCard = cardTransform.GetComponent<MjCard_2D>();
                if (mjCard != null)
                {
                    // 检查这张牌是否在亮牌列表中
                    bool isLiangPai = remainingLiangCards.Contains(mjCard.cardValue);
                    if (isLiangPai)
                    {
                        remainingLiangCards.Remove(mjCard.cardValue);
                        // 亮牌保持正常亮度
                        mjCard.SetMark(false);
                    }
                    else
                    {
                        // 非亮牌变暗
                        mjCard.SetMark(true, "#00000032");
                    }
                }
            }
        }
        
        // 摸牌区也需要处理
        if (MoPaiContainer != null && MoPaiContainer.childCount > 0)
        {
            Transform moPaiTransform = MoPaiContainer.GetChild(0);
            MjCard_2D moPaiCard = moPaiTransform.GetComponent<MjCard_2D>();
            if (moPaiCard != null)
            {
                bool isLiangPai = remainingLiangCards.Contains(moPaiCard.cardValue);
                if (isLiangPai)
                {
                    moPaiCard.SetMark(false);
                }
                else
                {
                    moPaiCard.SetMark(true);
                }
            }
        }
        
        GF.LogInfo($"[2D座位{SeatIndex}] 显示亮牌效果，亮出{liangCards.Count}张牌");
    }

    /// <summary>
    /// 清除亮牌效果（恢复所有牌的正常亮度）
    /// </summary>
    public void ClearLiangPaiEffect()
    {
        liangPaiCards.Clear();
        
        // 恢复手牌亮度
        if (HandContainer != null)
        {
            for (int i = 0; i < HandContainer.childCount; i++)
            {
                Transform cardTransform = HandContainer.GetChild(i);
                MjCard_2D mjCard = cardTransform.GetComponent<MjCard_2D>();
                if (mjCard != null)
                {
                    mjCard.SetMark(false);
                }
            }
        }
        
        // 恢复摸牌区亮度
        if (MoPaiContainer != null && MoPaiContainer.childCount > 0)
        {
            Transform moPaiTransform = MoPaiContainer.GetChild(0);
            MjCard_2D moPaiCard = moPaiTransform.GetComponent<MjCard_2D>();
            if (moPaiCard != null)
            {
                moPaiCard.SetMark(false);
            }
        }
    }

    /// <summary>
    /// 显示成牌效果（指定的牌变暗，其他牌保持正常）
    /// 用于听牌时：成牌（暗铺牌/已成型的牌）变暗，单张/未成型的牌亮着
    /// </summary>
    /// <param name="chengPaiCards">成牌列表（需要变暗的牌）</param>
    public void ShowChengPaiEffect(List<int> chengPaiCards)
    {
        // 复制一份用于匹配（因为可能有多张相同的牌）
        List<int> remainingChengPai = new List<int>(chengPaiCards);
        
        // 遍历手牌，成牌变暗
        if (HandContainer != null)
        {
            for (int i = 0; i < HandContainer.childCount; i++)
            {
                Transform cardTransform = HandContainer.GetChild(i);
                MjCard_2D mjCard = cardTransform.GetComponent<MjCard_2D>();
                if (mjCard != null)
                {
                    // 检查这张牌是否在成牌列表中
                    bool isChengPai = remainingChengPai.Contains(mjCard.cardValue);
                    if (isChengPai)
                    {
                        remainingChengPai.Remove(mjCard.cardValue);
                        // 成牌变暗
                        mjCard.SetMark(true, "#00000032");
                    }
                    else
                    {
                        // 非成牌保持正常亮度
                        mjCard.SetMark(false);
                    }
                }
            }
        }
        
        // 摸牌区也需要处理
        if (MoPaiContainer != null && MoPaiContainer.childCount > 0)
        {
            Transform moPaiTransform = MoPaiContainer.GetChild(0);
            MjCard_2D moPaiCard = moPaiTransform.GetComponent<MjCard_2D>();
            if (moPaiCard != null)
            {
                bool isChengPai = remainingChengPai.Contains(moPaiCard.cardValue);
                if (isChengPai)
                {
                    moPaiCard.SetMark(true, "#00000032");
                }
                else
                {
                    moPaiCard.SetMark(false);
                }
            }
        }
        
        GF.LogInfo($"[2D座位{SeatIndex}] 显示成牌效果，{chengPaiCards.Count}张牌变暗");
    }

    /// <summary>
    /// 添加一张牌到手牌区（用于撤销操作）
    /// </summary>
    public void AddHandCard(int cardValue)
    {
        if (HandContainer == null)
            return;
            
        // 计算新牌的位置（放在最后）
        int cardCount = HandContainer.childCount;
        Vector2 position = CalculateCardPosition(cardCount, cardCount + 1, objectHand);
        
        bool isLaizi = IsLaiZiCard(cardValue);
        GameObject cardObj = CreateCardWithPrefab(cardValue, HandContainer, objectHand, position, isLaizi);
        
        if (cardObj != null)
        {
            GF.LogInfo($"[2D座位{SeatIndex}] 添加手牌：牌值={cardValue}");
        }
    }
    
    #endregion

    #region 血流回放 - 多次胡牌、甩牌、飘分

    /// <summary>
    /// 添加血流成河胡牌到胡牌容器
    /// </summary>
    /// <param name="huCard">胡的牌值</param>
    /// <param name="huIndex">第几次胡（从0开始）</param>
    /// <param name="providerSeatIdx">放铳玩家座位索引（相对于当前座位）：0自己,1上家,2对家,3下家；-1表示自摸</param>
    /// <param name="huTypes">胡牌牌型列表</param>
    public void AddXueLiuHuCard(int huCard, int huIndex, int providerSeatIdx = -1, List<int> huTypes = null)
    {
        if (HuContainer == null)
        {
            GF.LogWarning($"[2D座位{SeatIndex}] 胡牌容器为空，无法添加血流胡牌");
            return;
        }

        // 使用胡牌预制体创建胡牌
        if (objectHuCard == null)
        {
            GF.LogWarning($"[2D座位{SeatIndex}] 未设置胡牌预制体，使用弃牌预制体");
            objectHuCard = objectDiscard;
        }

        // 创建胡牌
        Vector2 position = CalculateHuCardPosition(huIndex);
        GameObject cardObj = Instantiate(objectHuCard, HuContainer);
        cardObj.SetActive(true);
        
        // 处理节点排序（解决遮挡问题）
        if (NeedInsertHuAtFirst)
        {
            cardObj.transform.SetAsFirstSibling();
        }
        
        RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = position;
        }

        // 设置牌值
        MjCard_2D mjCard = cardObj.GetComponent<MjCard_2D>();
        if (mjCard != null)
        {
            MahjongFaceValue faceValue = Util.ConvertServerIntToFaceValue(huCard);
            bool isLaizi = IsLaiZiCard(huCard);
            mjCard.SetCardValue(faceValue, isLaizi);
        }

        // 设置Point和ZiMo显示
        Transform pointTrans = cardObj.transform.Find("Point");
        Transform ziMoTrans = cardObj.transform.Find("ZiMo");

        bool isZiMo = (providerSeatIdx < 0);

        if (ziMoTrans != null)
        {
            ziMoTrans.gameObject.SetActive(isZiMo);
        }

        if (pointTrans != null)
        {
            if (!isZiMo && providerSeatIdx >= 0 && providerSeatIdx <= 3)
            {
                pointTrans.gameObject.SetActive(true);
                Image pointImage = pointTrans.GetComponent<Image>();
                pointImage.SetSprite($"MJGame/MJCardAll/card_provider_{providerSeatIdx}.png");
            }
            else
            {
                pointTrans.gameObject.SetActive(false);
            }
        }

        string providerDesc = isZiMo ? "自摸" : $"点炮方向{providerSeatIdx}";
        string typesStr = huTypes != null && huTypes.Count > 0 ? string.Join(",", huTypes) : "无";
        GF.LogInfo($"[2D座位{SeatIndex}] 添加第{huIndex + 1}次血流胡牌: {huCard}, {providerDesc}, 牌型=[{typesStr}]");
    }

    /// <summary>
    /// 计算胡牌容器中牌的位置
    /// </summary>
    private Vector2 CalculateHuCardPosition(int index)
    {
        GameObject prefab = objectHuCard != null ? objectHuCard : objectDiscard;
        
        RectTransform rectTransform = prefab.GetComponent<RectTransform>();
        float cardWidth = rectTransform != null ? rectTransform.sizeDelta.x : 60f;
        float spacing = intervalHu > 0 ? intervalHu : cardWidth;

        float xOffset = 0f;
        float yOffset = 0f;

        switch (SeatIndex)
        {
            case 0: // 自己座位(下方) - 水平排列，从左向右
                xOffset = index * spacing;
                break;
            case 1: // 左家 - 垂直排列，从上到下
                yOffset = -index * spacing;
                break;
            case 2: // 对家(上方) - 水平排列，从右向左
                xOffset = -index * spacing;
                break;
            case 3: // 右家 - 垂直排列，从下到上
                yOffset = index * spacing;
                break;
        }

        return new Vector2(xOffset, yOffset);
    }

    /// <summary>
    /// 移除最后一张血流胡牌（回放撤销专用）
    /// </summary>
    /// <returns>返回被移除的胡牌值，如果没有胡牌返回-1</returns>
    public int RemoveLastXueLiuHuCard()
    {
        if (HuContainer == null || HuContainer.childCount == 0)
        {
            GF.LogWarning($"[2D座位{SeatIndex}] 胡牌容器为空或无胡牌，无法移除");
            return -1;
        }

        // 获取最后一张胡牌的索引
        // 注意：座位2和3使用SetAsFirstSibling，最后添加的牌在索引0
        int lastIndex = NeedInsertHuAtFirst ? 0 : (HuContainer.childCount - 1);
        Transform lastCardTrans = HuContainer.GetChild(lastIndex);
        
        int cardValue = -1;
        if (lastCardTrans != null)
        {
            MjCard_2D mjCard = lastCardTrans.GetComponent<MjCard_2D>();
            if (mjCard != null)
            {
                cardValue = mjCard.cardValue;
            }
            
            Destroy(lastCardTrans.gameObject);
            GF.LogInfo($"[2D座位{SeatIndex}] 移除最后一张血流胡牌: {cardValue}");
        }

        return cardValue;
    }

    /// <summary>
    /// 添加甩牌组到MeldContainer（血流麻将专用）
    /// </summary>
    /// <param name="shuaiPaiCards">甩牌列表</param>
    public void AddShuaiPaiGroup(List<int> shuaiPaiCards)
    {
        if (shuaiPaiCards == null || shuaiPaiCards.Count == 0)
            return;

        int currentCardCount = MeldContainer.childCount;
        bool insertAtFirst = NeedInsertMeldAtFirst;
        int logicalPositionIndex = CalculateLogicalMeldPositions();

        for (int i = 0; i < shuaiPaiCards.Count; i++)
        {
            int cardValue = shuaiPaiCards[i];
            int cardLogicalIndex = logicalPositionIndex + i;
            Vector2 position = CalculateMeldCardPosition(cardLogicalIndex, objectMeld, i);

            // 统一使用 CreateCardWithPrefab 创建牌（保证牌值正确设置）
            bool isLaizi = IsLaiZiCard(cardValue);
            GameObject cardObj = CreateCardWithPrefab(cardValue, MeldContainer, objectMeld, position, isLaizi, insertAtFirst);

            // 标记为甩牌
            if (cardObj != null)
            {
                MjCard_2D mjCard = cardObj.GetComponent<MjCard_2D>();
                if (mjCard != null)
                {
                    mjCard.isShuaiPaiCard = true;
                }
            }
        }

        GF.LogInfo($"[2D座位{SeatIndex}] 添加甩牌组完成，牌数: {shuaiPaiCards.Count}");
    }

    /// <summary>
    /// 从手牌移除甩牌（回放专用）
    /// </summary>
    /// <param name="throwCards">要移除的甩牌列表</param>
    public void RemoveThrowCardsFromHand(List<int> throwCards)
    {
        if (throwCards == null || throwCards.Count == 0)
            return;

        List<int> cardsToRemove = new List<int>(throwCards);
        int removedFromHand = 0;
        int removedFromMoPai = 0;

        // 首先从摸牌区检查并移除
        if (MoPaiContainer != null && MoPaiContainer.childCount > 0 && cardsToRemove.Count > 0)
        {
            for (int i = MoPaiContainer.childCount - 1; i >= 0 && cardsToRemove.Count > 0; i--)
            {
                Transform cardTrans = MoPaiContainer.GetChild(i);
                MjCard_2D mjCard = cardTrans.GetComponent<MjCard_2D>();
                if (mjCard != null)
                {
                    if (cardsToRemove.Contains(mjCard.cardValue))
                    {
                        cardsToRemove.Remove(mjCard.cardValue);
                        DestroyImmediate(cardTrans.gameObject);
                        removedFromMoPai++;
                    }
                }
            }
        }

        // 然后从手牌区移除剩余的牌
        if (HandContainer != null)
        {
            for (int i = HandContainer.childCount - 1; i >= 0 && cardsToRemove.Count > 0; i--)
            {
                Transform cardTrans = HandContainer.GetChild(i);
                MjCard_2D mjCard = cardTrans.GetComponent<MjCard_2D>();
                if (mjCard != null)
                {
                    if (cardsToRemove.Contains(mjCard.cardValue))
                    {
                        cardsToRemove.Remove(mjCard.cardValue);
                        DestroyImmediate(cardTrans.gameObject);
                        removedFromHand++;
                    }
                }
            }
        }

        GF.LogInfo($"[2D座位{SeatIndex}] 甩牌移除完成: 手牌区{removedFromHand}张, 摸牌区{removedFromMoPai}张");
    }

    /// <summary>
    /// 设置飘分显示
    /// </summary>
    /// <param name="piaoFen">飘分值</param>
    public void SetPiaoFenDisplay(double piaoCount)
    {
       if (piaoCount < 0)
        {
            transform.Find("piaoScoreInfo").gameObject.SetActive(false);
            return;
        }
        else
        {
            if (piaoCount == 0)
            {
                transform.Find("piaoScoreInfo/Nopiao").gameObject.SetActive(true);
                transform.Find("piaoScoreInfo/piaoCountBg").gameObject.SetActive(false);
            }
            else
            {
                transform.Find("piaoScoreInfo/Nopiao").gameObject.SetActive(false);
                transform.Find("piaoScoreInfo/piaoCountBg").gameObject.SetActive(true);
                transform.Find("piaoScoreInfo/piaoCountBg/piaoCount").GetComponent<Text>().text = piaoCount.ToString();
            }
            transform.Find("piaoScoreInfo").gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 清除甩牌组（从MeldContainer中移除所有甩牌）
    /// </summary>
    public void ClearShuaiPaiGroups()
    {
        if (MeldContainer == null) return;

        // 遍历MeldContainer，找到所有标记为甩牌的卡牌并移除
        List<GameObject> toRemove = new List<GameObject>();
        for (int i = MeldContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = MeldContainer.GetChild(i);
            MjCard_2D mjCard = child.GetComponent<MjCard_2D>();
            if (mjCard != null && mjCard.isShuaiPaiCard)
            {
                toRemove.Add(child.gameObject);
            }
        }

        foreach (var obj in toRemove)
        {
            DestroyImmediate(obj);
        }

        GF.LogInfo($"[2D座位{SeatIndex}] 清除甩牌组，移除 {toRemove.Count} 张牌");
    }

    #endregion
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public partial class PDKGamePanel
{
    #region Card Selection
    [Header("Card Selection")]
    [SerializeField] private List<PDKCard> selectedCards = new List<PDKCard>();
    [SerializeField] private List<int> selectedCardNumbers = new List<int>();
    #endregion

    #region 拖拽选择状态变量

    private PDKCard pressedCard = null;
    private float pressStartTime = 0f;
    private bool isLongPressTriggered = false;
    private bool isDragging = false;
    private PDKCard dragStartCard = null;
    private PDKCard currentHoverCard = null;
    private List<PDKCard> dragSelectedCards = new List<PDKCard>();
    internal long bankerId;//庄家Id

    #endregion

    /// <summary>
    /// 卡牌按下事件
    /// </summary>
    /// <param name="card">卡牌组件</param>
    /// <param name="eventData">事件数据</param>
    private void OnCardPointerDown(PDKCard card, UnityEngine.EventSystems.BaseEventData eventData)
    {
        if (card == null) return;

        pressedCard = card;
        pressStartTime = Time.time;
        isLongPressTriggered = false;
        isDragging = false;
        dragStartCard = card;
        currentHoverCard = card;
        dragSelectedCards.Clear();

      
    }

    /// <summary>
    /// 卡牌抬起事件
    /// </summary>
    /// <param name="card">卡牌组件</param>
    /// <param name="eventData">事件数据</param>
    private void OnCardPointerUp(PDKCard card, UnityEngine.EventSystems.BaseEventData eventData)
    {
        if (card == null || pressedCard == null) return;

        float pressDuration = Time.time - pressStartTime;

        if (isDragging && isLongPressTriggered)
        {
            // 完成拖拽选择
            ApplyDragSelection();
        }
        else if (!isLongPressTriggered)
        {
            // 短按事件
            HandleClick(pressedCard);
        }
        // 重置状态
        ResetDragState();
    }

    /// <summary>
    /// 卡牌拖拽事件
    /// </summary>
    /// <param name="card">卡牌组件</param>
    /// <param name="eventData">事件数据</param>
    private void OnCardDrag(PDKCard card, UnityEngine.EventSystems.BaseEventData eventData)
    {
        if (pressedCard == null) return;

        float pressDuration = Time.time - pressStartTime;

        // 检查是否达到长按时间并开始拖拽模式
        if (pressDuration >= LONG_PRESS_DURATION && !isLongPressTriggered)
        {
            isLongPressTriggered = true;
            isDragging = true;
        }

        // 如果在拖拽模式中，更新选择范围
        if (isDragging && isLongPressTriggered)
        {
            UpdateDragSelection();
        }
    }

    /// <summary>
    /// 指针进入卡牌事件
    /// </summary>
    /// <param name="card">卡牌组件</param>
    /// <param name="eventData">事件数据</param>
    private void OnCardPointerEnter(PDKCard card, UnityEngine.EventSystems.BaseEventData eventData)
    {
        if (card == null) return;

        currentHoverCard = card;

        // 如果正在拖拽选择模式，更新选择范围
        if (isDragging && isLongPressTriggered)
        {
            UpdateDragSelection();
        }
    }

    /// <summary>
    /// 更新拖拽选择范围
    /// </summary>
    private void UpdateDragSelection()
    {
        if (dragStartCard == null || currentHoverCard == null || varHandCard == null) return;

        Transform parent = varHandCard.transform;

        // 找到起始卡牌和当前卡牌的索引
        int startIndex = GetCardIndex(dragStartCard);
        int currentIndex = GetCardIndex(currentHoverCard);

        if (startIndex == -1 || currentIndex == -1) return;

        // 确定选择范围
        int minIndex = Mathf.Min(startIndex, currentIndex);
        int maxIndex = Mathf.Max(startIndex, currentIndex);

        // 清除之前的拖拽高亮
        ClearDragHighlight();

        // 重新构建拖拽选择列表
        dragSelectedCards.Clear();

        for (int i = minIndex; i <= maxIndex; i++)
        {
            if (i >= 0 && i < parent.childCount)
            {
                PDKCard cardComponent = parent.GetChild(i).GetComponent<PDKCard>();
                if (cardComponent != null)
                {
                    dragSelectedCards.Add(cardComponent);
                    // 添加拖拽高亮效果
                    HighlightCard(cardComponent, true);
                }
            }
        }

        if (enableDebugLog)
        {
        }
    }
    /// <summary>
    /// 获取卡牌在父容器中的索引
    /// </summary>
    /// <param name="card">卡牌组件</param>
    /// <returns>索引，未找到返回-1</returns>
    private int GetCardIndex(PDKCard card)
    {
        if (card == null || varHandCard == null) return -1;

        Transform parent = varHandCard.transform;

        for (int i = 0; i < parent.childCount; i++)
        {
            PDKCard cardComponent = parent.GetChild(i).GetComponent<PDKCard>();
            if (cardComponent == card)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 清除拖拽高亮效果
    /// </summary>
    private void ClearDragHighlight()
    {
        foreach (var card in dragSelectedCards)
        {
            if (card != null)
            {
                HighlightCard(card, false);
            }
        }
    }

    /// <summary>
    /// 应用拖拽选择
    /// </summary>
    private void ApplyDragSelection()
    {
        if (dragSelectedCards.Count == 0) return;

        // 清除拖拽高亮
        ClearDragHighlight();
        
        // 【Bug修复5】智能识别拖拽的牌型，优先选择能组成常见牌型的牌
        List<PDKCard> optimizedCards = OptimizeDragSelectionForCardType(dragSelectedCards);

        // 新的选择逻辑：已选中的卡牌取消选中，未选中的卡牌选中
        List<PDKCard> cardsToSelect = new List<PDKCard>();
        List<PDKCard> cardsToDeselect = new List<PDKCard>();

        foreach (var card in optimizedCards)
        {
            if (IsCardSelected(card))
            {
                // 当前已被选中的卡牌，需要取消选中（还原）
                cardsToDeselect.Add(card);
            }
            else
            {
                // 当前未选中的卡牌，需要选中（上移）
                cardsToSelect.Add(card);
            }
        }

        // 执行取消选中操作（已选中的卡牌还原位置）
        foreach (var card in cardsToDeselect)
        {
            DeselectCard(card);
        }

        // 执行选中操作（未选中的卡牌上移）
        foreach (var card in cardsToSelect)
        {
            SelectCard(card);
        }

        if (enableDebugLog)
        {
        }
    }

    /// <summary>
    /// 重置拖拽状态
    /// </summary>
    private void ResetDragState()
    {
        // 清除拖拽高亮
        ClearDragHighlight();

        pressedCard = null;
        isLongPressTriggered = false;
        isDragging = false;
        dragStartCard = null;
        currentHoverCard = null;
        dragSelectedCards.Clear();
    }

    /// <summary>
    /// 高亮显示卡牌（用于拖拽预览）
    /// </summary>
    /// <param name="card">卡牌组件</param>
    /// <param name="highlight">是否高亮</param>
    private void HighlightCard(PDKCard card, bool highlight)
    {
        if (card == null) return;

        var cardImage = card.GetComponent<Image>();
cardImage.color = highlight ? PDKConstants.CARD_HIGHLIGHT_COLOR : Color.white;
    }

  
    /// <summary>
    /// 选中卡牌
    /// </summary>
    /// <param name="card">要选中的卡牌</param>
    private void SelectCard(PDKCard card)
    {
        if (card == null || IsCardSelected(card)) return;

        // 【关键修复】停止之前的动画协程，防止位置冲突
        if (card.selectionCoroutine != null)
        {
            StopCoroutine(card.selectionCoroutine);
        }

        // 添加到选中列表
        selectedCards.Add(card);
        selectedCardNumbers.Add(card.numD);

        // 播放选中动画（上移）并保存协程引用
        card.selectionCoroutine = StartCoroutine(uiManager.AnimateCardSelection(card, true));

        // 设置选中状态
        card.SetSelected(true);

        // 检查选中的牌是否符合规则
        CheckSelectedCardsValidity();
    }
    /// <summary>
    /// 取消选中卡牌
    /// </summary>
    /// <param name="card">要取消选中的卡牌</param>
    private void DeselectCard(PDKCard card)
    {
        if (card == null || !IsCardSelected(card)) return;

        // 【关键修复】停止之前的动画协程
        if (card.selectionCoroutine != null)
        {
            StopCoroutine(card.selectionCoroutine);
        }

        // 从选中列表移除
        selectedCards.Remove(card);
        selectedCardNumbers.Remove(card.numD);

        // 播放取消选中动画（下移）并保存协程引用
        card.selectionCoroutine = StartCoroutine(uiManager.AnimateCardSelection(card, false));

        // 设置选中状态
        card.SetSelected(false);

        // 检查选中的牌是否符合规则
        CheckSelectedCardsValidity();
    }

    /// <summary>
    /// 检查卡牌是否已选中
    /// </summary>
    /// <param name="card">要检查的卡牌</param>
    /// <returns>是否已选中</returns>
    private bool IsCardSelected(PDKCard card)
    {
        return card != null && selectedCards.Contains(card);
    }

    /// <summary>
    /// 更新手牌的交互状态和显示(根据需要压的牌型)
    /// </summary>
    private void UpdateHandCardInteraction()
    {
        // 【逻辑修正】如果有选中的牌，必须先取消选中并让所有牌恢复原位，再重新进行智能变灰逻辑
        // 因为“智能变灰”是提供给玩家当前的跟牌提示，如果玩家已经拉起了牌，逻辑基准应该重置
        if (selectedCards.Count > 0)
        {
            ClearSelectedCards();
        }

        // 【关键】判断是否首出状态
        // 首出条件：serverMaxDisCardPlayer == 0 或 lastPlayedCards == null
        bool isLeading = (serverMaxDisCardPlayer == 0 || lastPlayedCards == null);


        if (varHandCard == null)
        {
            GF.LogWarning($"[手牌交互] varHandCard 为 null，跳过更新");
            return;
        }

        if (isLeading)
        {
            // 首出状态：所有手牌都可以出，恢复正常状态
            RestoreAllHandCardsInteraction();
            return;
        }

        // 获取所有手牌GameObject
        var allHandCards = GetAllHandCardComponents();
        if (allHandCards.Count == 0)
        {
            GF.LogWarning($"[手牌交互] 没有找到手牌组件，跳过更新");
            return;
        }


        // 【修复】按点数分组统计，使用 numD 计算 value 而不是使用可能未初始化的 card.value
        var valueGroups = allHandCards
            .GroupBy(card => GameUtil.GetInstance().GetValue(card.numD))
            .ToDictionary(g => g.Key, g => g.ToList());

        // 根据需要压的牌型设置交互状态
        switch (lastPlayedCards.cardType)
        {
            case PDKConstants.CardType.SINGLE:
                UpdateHandCardsForSingle(allHandCards, valueGroups);
                break;
            case PDKConstants.CardType.PAIR:
                UpdateHandCardsForPair(allHandCards, valueGroups);
                break;
            case PDKConstants.CardType.THREE_WITH_NO:
            case PDKConstants.CardType.THREE_WITH_ONE:
            case PDKConstants.CardType.THREE_WITH_TWO:
                UpdateHandCardsForTriple(allHandCards, valueGroups);
                break;
            case PDKConstants.CardType.STRAIGHT:
            case PDKConstants.CardType.STRAIGHT_PAIR:
            case PDKConstants.CardType.PLANE:
                UpdateHandCardsForStraight(allHandCards, valueGroups);
                break;
            default:
                // 其他牌型(炸弹等)暂不限制，恢复正常状态
                RestoreAllHandCardsInteraction();
                break;
        }

    }

    /// <summary>
    /// 单张压牌时更新手牌交互
    /// 规则：小于等于该单张的牌变灰且取消交互
    /// </summary>
    private void UpdateHandCardsForSingle(List<PDKCard> allHandCards, Dictionary<int, List<PDKCard>> valueGroups)
    {
        if (lastPlayedCards == null || lastPlayedCards.cards.Count == 0) return;

        // 使用 weight 而不是直接取 value，确保与 BuildFollowCandidates 逻辑一致
        int targetWeight = lastPlayedCards.weight;

        foreach (var card in allHandCards)
        {
            // 【修正】使用 LogicValue (A=14, 2=15) 进行比较
            int cardValue = GameUtil.GetInstance().GetValue(card.numD);
            int logicValue = cardValue;
            if (cardValue == 1) logicValue = 14; 
            else if (cardValue == 2) logicValue = 15;

            // 目标牌也需要转换 logicValue
            //强转
            int targetLogicValue = (int)lastPlayedCards.cards[0].value;
            if (targetLogicValue == 1) targetLogicValue = 14;
            else if (targetLogicValue == 2) targetLogicValue = 15;

            // 小于等于目标权重的牌：变灰+禁用
            bool shouldDisable = (logicValue <= targetLogicValue);

            SetCardInteraction(card, !shouldDisable);
        }

    }

    /// <summary>
    /// 对子压牌时更新手牌交互
    /// 规则：
    /// 1. 小于等于该对子的牌变灰
    /// 2. 不是对子的变灰(单张)
    /// 3. 但3张的不变灰(因为可以拆成对子)
    /// </summary>
    private void UpdateHandCardsForPair(List<PDKCard> allHandCards, Dictionary<int, List<PDKCard>> valueGroups)
    {
        if (lastPlayedCards == null || lastPlayedCards.cards.Count == 0) return;

        // 使用 weight 而不是直接取 value，确保与 BuildFollowCandidates 逻辑一致
        int targetWeight = lastPlayedCards.weight;

        foreach (var card in allHandCards)
        {
            // 【修正】使用 LogicValue (A=14, 2=15) 进行比较，而不是原始权重
            int cardValue = GameUtil.GetInstance().GetValue(card.numD);
            int logicValue = cardValue;
            if (cardValue == 1) logicValue = 14; 
            else if (cardValue == 2) logicValue = 15;

            // 目标牌也需要转换 logicValue
            int targetLogicValue = (int)lastPlayedCards.cards[0].value;
            if (targetLogicValue == 1) targetLogicValue = 14;
            else if (targetLogicValue == 2) targetLogicValue = 15;

            // 获取该点数的牌数量
            int count = valueGroups.ContainsKey(cardValue) ? valueGroups[cardValue].Count : 1;

            // 规则判断
            bool shouldDisable = false;

            // 1. 小于等于目标点数的牌变灰
            if (logicValue <= targetLogicValue)
            {
                shouldDisable = true;
            }
            // 2. 只有1张的单牌也变灰(不能出对子)
            else if (count < 2)
            {
                shouldDisable = true;
            }
            // 3. 有2张或3张以上的，可以交互(可以出对子)
            else
            {
                shouldDisable = false;
            }

            SetCardInteraction(card, !shouldDisable);
        }
    }

    /// <summary>
    /// 三张压牌时更新手牌交互
    /// 规则：
    /// 1. 小于等于该三张的牌变灰
    /// 2. 不足3张的变灰
    /// 3. 有3张或4张的正常显示
    /// </summary>
    private void UpdateHandCardsForTriple(List<PDKCard> allHandCards, Dictionary<int, List<PDKCard>> valueGroups)
    {
        if (lastPlayedCards == null || lastPlayedCards.cards.Count == 0) return;

        // 使用 weight 而不是重新计算，确保与 BuildFollowCandidates 逻辑一致
        int targetWeight = lastPlayedCards.weight;


        foreach (var card in allHandCards)
        {
            // 【修正】使用 LogicValue (A=14, 2=15) 进行比较
            int cardValue = GameUtil.GetInstance().GetValue(card.numD);
            int logicValue = cardValue;
            if (cardValue == 1) logicValue = 14; 
            else if (cardValue == 2) logicValue = 15;

            // 获取上家三张的值
            int targetLogicValue = lastPlayedCards.weight; // weight在DetectCardType后已是主牌值

            int count = valueGroups.ContainsKey(cardValue) ? valueGroups[cardValue].Count : 1;

            bool shouldDisable = false;

            // 1. 小于等于目标点数的牌变灰
            if (logicValue <= targetLogicValue)
            {
                shouldDisable = true;
            }
            // 2. 不足3张的变灰
            else if (count < 3)
            {
                shouldDisable = true;
            }
            // 3. 有3张或更多的正常显示
            else
            {
                shouldDisable = false;
            }

            SetCardInteraction(card, !shouldDisable);
        }
    }

    /// <summary>
    /// 顺子/连对/飞机压牌时更新手牌交互
    /// 规则：暂不做特殊限制，保持默认交互
    /// (因为顺子/飞机的判断较复杂，玩家需要自己组合)
    /// </summary>
    private void UpdateHandCardsForStraight(List<PDKCard> allHandCards, Dictionary<int, List<PDKCard>> valueGroups)
    {
        // 顺子/飞机等复杂牌型，不做交互限制
        // 让玩家自由选择牌进行组合
        RestoreAllHandCardsInteraction();
    }

    /// <summary>
    /// 设置单张牌的交互状态
    /// </summary>
    /// <param name="card">牌组件</param>
    /// <param name="enabled">是否启用交互(true=正常,false=变灰禁用)</param>
    private void SetCardInteraction(PDKCard card, bool enabled)
    {
        if (card == null) return;

        // 【修复】直接使用 card.image 而不是 GetComponent<Image>()
        if (card.image != null)
        {
            card.image.color = enabled ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f); // 灰色
            // 设置raycastTarget控制是否接收射线检测
            card.image.raycastTarget = enabled;
        }
        else
        {
            GF.LogWarning($"[SetCardInteraction] card.image 为 null! value={card.value}");
        }

        // 设置EventTrigger交互
        var eventTrigger = card.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger != null)
        {
            eventTrigger.enabled = enabled;
        }

        // 如果已经被选中，不改变其选中状态
        // 只是禁用后续的点击交互
    }

    /// <summary>
    /// 【Bug修复5】智能优化拖拽选择的牌，优先选择能组成常见牌型的牌
    /// 优先级：飞机 > 顺子 > 连对 > 三带二 > 四带三/四带二
    /// </summary>
    private List<PDKCard> OptimizeDragSelectionForCardType(List<PDKCard> draggedCards)
    {
        if (draggedCards == null || draggedCards.Count < 2)
        {
            return draggedCards; // 少于2张牌，无需优化
        }

        // 按牌值分组统计（使用numD计算value）
        var valueGroups = new Dictionary<int, List<PDKCard>>();
        foreach (var card in draggedCards)
        {
            int value = GameUtil.GetInstance().GetValue(card.numD);
            if (!valueGroups.ContainsKey(value))
            {
                valueGroups[value] = new List<PDKCard>();
            }
            valueGroups[value].Add(card);
        }


        // 优先级1：检测飞机（至少2组连续的三张）
        var tripleValues = valueGroups.Where(g => g.Value.Count >= 3)
                                     .Select(g => g.Key)
                                     .OrderBy(v => v)
                                     .ToList();
        if (tripleValues.Count >= 2)
        {
            var consecutiveTriples = FindAllConsecutiveValues(tripleValues, 2);
            if (consecutiveTriples.Count > 0)
            {
                var result = new List<PDKCard>();
                foreach (int val in consecutiveTriples)
                {
                    result.AddRange(valueGroups[val].Take(3)); // 每个值取3张
                }
                return result;
            }
        }

        // 优先级2：检测顺子（至少5张连续单牌）
        var singleValues = valueGroups.Keys.Where(v => v < 15).OrderBy(v => v).ToList(); // 排除2（value=15），包含A（value=14）
        if (singleValues.Count >= 5)
        {
            var consecutiveSingles = FindAllConsecutiveValues(singleValues, 5);
            if (consecutiveSingles.Count > 0)
            {
                var result = new List<PDKCard>();
                foreach (int val in consecutiveSingles)
                {
                    result.Add(valueGroups[val].First()); // 每个值取1张
                }
                return result;
            }
        }

        // 优先级3：检测三带二（1组三张+任意2张）
        if (tripleValues.Count >= 1 && draggedCards.Count >= 5)
        {
            int tripleVal = tripleValues.First();
            var result = new List<PDKCard>();
            result.AddRange(valueGroups[tripleVal].Take(3)); // 三张
            
            // 找剩余的2张（优先选择对子，其次选择最小的两张）
            var remaining = draggedCards.Except(result).ToList();
            if (remaining.Count >= 2)
            {
                // 尝试找对子
                var remainingPair = remaining.GroupBy(c => GameUtil.GetInstance().GetValue(c.numD))
                                           .Where(g => g.Count() >= 2)
                                           .OrderBy(g => g.Key)
                                           .FirstOrDefault();
                if (remainingPair != null)
                {
                    result.AddRange(remainingPair.Take(2));
                }
                else
                {
                    result.AddRange(remaining.OrderBy(c => GameUtil.GetInstance().GetValue(c.numD)).Take(2));
                }
                return result;
            }
        }

        // 优先级4：检测连对（至少2对连续对子）
        var pairValues = valueGroups.Where(g => g.Value.Count >= 2)
                                   .Select(g => g.Key)
                                   .Where(v => v <= 14) // 排除2
                                   .OrderBy(v => v)
                                   .ToList();
        if (pairValues.Count >= 2)
        {
            var consecutivePairs = FindAllConsecutiveValues(pairValues, 2);
            if (consecutivePairs.Count > 0)
            {
                var result = new List<PDKCard>();
                foreach (int val in consecutivePairs)
                {
                    result.AddRange(valueGroups[val].Take(2)); // 每个值取2张
                }
                return result;
            }
        }

        // 优先级5：检测四带三/四带二（需要检查规则是否开启）
        var quadValues = valueGroups.Where(g => g.Value.Count == 4)
                                   .Select(g => g.Key)
                                   .OrderBy(v => v)
                                   .ToList();
        if (quadValues.Count >= 1)
        {
            int quadVal = quadValues.First();
            var result = new List<PDKCard>();
            result.AddRange(valueGroups[quadVal]); // 四张
            
            var remaining = draggedCards.Except(result).ToList();
            
            // 优先尝试四带三（如果规则开启且有足够的牌）
            if (currentGameRules.canFourWithThree && remaining.Count >= 3)
            {
                result.AddRange(remaining.OrderBy(c => GameUtil.GetInstance().GetValue(c.numD)).Take(3));
                return result;
            }
            // 其次尝试四带二
            else if (currentGameRules.canFourWithTwo && remaining.Count >= 2)
            {
                result.AddRange(remaining.OrderBy(c => GameUtil.GetInstance().GetValue(c.numD)).Take(2));
                return result;
            }
        }

        // 没有识别到常见牌型，返回原选择
        return draggedCards;
    }

    /// <summary>
    /// 找出连续的牌值序列（找到第一个符合条件的最长序列）
    /// </summary>
    /// <param name="values">已排序的牌值列表</param>
    /// <param name="minLength">最小长度</param>
    /// <returns>找到的连续牌值序列（尽可能长）</returns>
    private List<int> FindAllConsecutiveValues(List<int> values, int minLength)
    {
        if (values == null || values.Count < minLength) return new List<int>();

        List<int> bestResult = new List<int>();
        List<int> current = new List<int> { values[0] };

        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] == current[current.Count - 1] + 1)
            {
                // 连续，添加到当前序列
                current.Add(values[i]);
            }
            else
            {
                // 不连续，检查当前序列是否符合条件
                if (current.Count >= minLength)
                {
                    // 如果当前序列比之前找到的更长，更新最佳结果
                    if (current.Count > bestResult.Count)
                    {
                        bestResult = new List<int>(current);
                    }
                }
                // 开始新序列
                current = new List<int> { values[i] };
            }
        }

        // 检查最后一个序列
        if (current.Count >= minLength && current.Count > bestResult.Count)
        {
            bestResult = new List<int>(current);
        }

        return bestResult;
    }

    /// <summary>
    /// 恢复所有手牌的正常交互状态
    /// </summary>
    private void RestoreAllHandCardsInteraction()
    {
        var allHandCards = GetAllHandCardComponents();
        foreach (var card in allHandCards)
        {
            SetCardInteraction(card, true);
        }
    }

}

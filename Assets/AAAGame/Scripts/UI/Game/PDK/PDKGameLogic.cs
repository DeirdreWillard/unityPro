using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// PDK提示状态数据结构（用于GetHintPure纯函数）
/// </summary>
public class PDKHintState
{
    // 首出循环
    public int singleIndex = 0;
    public int pairIndex = 0;
    public bool usingPairs = false;

    // 跟牌循环
    public int followIndex = 0;

    public void ResetLeadCycle()
    {
        singleIndex = 0;
        pairIndex = 0;
        usingPairs = false;
    }

    public void ResetFollowCycle()
    {
        followIndex = 0;
    }
}

/// <summary>
/// PDK游戏逻辑处理器（提示/跟牌判断/出牌执行）
/// </summary>
public class PDKGameLogic : MonoBehaviour
{
    private PDKGamePanel panel;

    public void Initialize(PDKGamePanel gamePanel)
    {
        panel = gamePanel;
    }

    /// <summary>
    /// 获取游戏规则配置（用于炸弹判断等）
    /// </summary>
    private PDKConstants.GameRuleConfig GetGameRules()
    {
        return panel?.currentGameRules ?? new PDKConstants.GameRuleConfig();
    }

    #region 纯逻辑方法（无状态依赖）

    /// <summary>
    /// 【纯函数】判断玩家手牌是否有能够压过上家的牌
    /// 用于迁移期间的兼容，最终会替换整个 PDKGameLogic
    /// </summary>
    /// <param name="handCards">玩家手牌</param>
    /// <param name="lastPlayedCards">上家出牌数据</param>
    /// <returns>true=有能压的牌, false=没有能压的牌</returns>
    public bool CanFollowLastPlay(List<PDKCardData> handCards, PDKPlayCardData lastPlayedCards)
    {
        // 【安全检查】如果lastPlayedCards为null或为空,表示是首出状态,不应该调用此方法
        if (lastPlayedCards == null || lastPlayedCards.cards == null || lastPlayedCards.cards.Count == 0)
        {
            GF.LogWarning($"[CanFollowLastPlay-新] ⚠️ lastPlayedCards为空,这是首出状态,不应该调用此方法!");
            return true; // 返回true避免阻止出牌
        }
        
        // 【安全检查】handCards不能为空
        if (handCards == null || handCards.Count == 0)
        {
            GF.LogWarning($"[CanFollowLastPlay-新] ⚠️ handCards为空!");
            return false;
        }

        // 构建能够压过上家的牌型候选列表
        var candidates = BuildFollowCandidates(handCards, lastPlayedCards);
        return candidates.Count > 0;
    }
    /// <param name="handCards">玩家手牌</param>
    /// <param name="lastPlayedCards">上家出牌数据（null表示首出）</param>
    /// <param name="hintState">提示状态（用于循环提示）</param>
    /// <returns>提示出的牌列表（可直接用于高亮/上升），如果无可提示则返回空列表</returns>
    public List<PDKCardData> GetHintPure(List<PDKCardData> handCards, PDKPlayCardData lastPlayedCards, PDKHintState hintState)
    {
        var result = new List<PDKCardData>();

        if (handCards == null || handCards.Count == 0)
        {
            GF.LogWarning("[GetHintPure] 手牌为空");
            return result;
        }

        if (hintState == null)
        {
            GF.LogWarning("[GetHintPure] hintState 为空");
            return result;
        }

        // 判断是否是首出状态
        bool isLeading = (lastPlayedCards == null);

        if (isLeading)
        {
            // 首出：从右向左找（从小到大），同点数时方块在最右边先找
            var singles = handCards
                .OrderBy(c => c.value) // 从小到大
                .ThenBy(c => GetSuitPriorityForHint(c.suit)) // 方块优先
                .Select(c => new List<PDKCardData> { c })
                .ToList();
            var pairs = handCards
                .GroupBy(c => c.value)
                .Where(g => g.Count() >= 2)
                .OrderBy(g => g.Key) // 从小到大
                .Select(g => g.OrderBy(c => GetSuitPriorityForHint(c.suit)).Take(2).ToList()) // 方块对优先
                .ToList();

            if (!hintState.usingPairs)
            {
                if (hintState.singleIndex < singles.Count)
                {
                    result = singles[hintState.singleIndex];
                    hintState.singleIndex++;
                    // 如果已经到最后一个单张，下次开始对子模式
                    if (hintState.singleIndex >= singles.Count)
                    {
                        hintState.usingPairs = true;
                        hintState.pairIndex = 0;
                    }
                }
                else
                {
                    // 保险：如果越界，切换到对子
                    hintState.usingPairs = true;
                    hintState.pairIndex = 0;
                    hintState.singleIndex = singles.Count; // 锁定
                    return GetHintPure(handCards, lastPlayedCards, hintState); // 递归一次进入对子逻辑
                }
            }
            else // 使用对子模式
            {
                if (pairs.Count == 0)
                {
                    // 没有对子则回到单张循环
                    hintState.ResetLeadCycle();
                    return GetHintPure(handCards, lastPlayedCards, hintState);
                }
                if (hintState.pairIndex < pairs.Count)
                {
                    result = pairs[hintState.pairIndex];
                    hintState.pairIndex++;
                    if (hintState.pairIndex >= pairs.Count)
                    {
                        // 对子循环结束回到单张
                        hintState.ResetLeadCycle();
                    }
                }
                else
                {
                    // 保险：重置并回到单张
                    hintState.ResetLeadCycle();
                    return GetHintPure(handCards, lastPlayedCards, hintState);
                }
            }
        }
        else
        {
            // 跟出：从候选列表中按顺序取出
            var candidates = BuildFollowCandidates(handCards, lastPlayedCards);
            if (candidates.Count == 0) return result;
            if (hintState.followIndex < candidates.Count)
            {
                result = candidates[hintState.followIndex];
                hintState.followIndex++;
            }
            else
            {
                hintState.followIndex = 0;
                result = candidates[hintState.followIndex];
                hintState.followIndex++;
            }
        }
        return result;
    }

    /// <summary>
    /// 构建跟牌候选列表（直接按牌面值逻辑判断，从小到大排序）
    /// </summary>
    private List<List<PDKCardData>> BuildFollowCandidates(List<PDKCardData> handCards, PDKPlayCardData lastPlay)
    {
        var candidates = new List<List<PDKCardData>>();
        var cardType = lastPlay.cardType;

        // 获取上家出牌的基准值（用于比较）
        var lastPlayValue = GetBaseValueFromCards(lastPlay.cards);

        // 根据牌型直接判断
        switch (cardType)
        {
            case PDKConstants.CardType.SINGLE:
                // 单张：找所有比基准值大的单张（3-A, 2最大）
                candidates.AddRange(FindBiggerSingles(handCards, lastPlayValue));
                break;

            case PDKConstants.CardType.PAIR:
                // 对子：找所有比基准值大的对子（A最大，因为2只有一张）
                candidates.AddRange(FindBiggerPairs(handCards, lastPlayValue));
                break;

            case PDKConstants.CardType.THREE_WITH_ONE:
                // 三带一：找三张比基准值大的，带任意单张
                candidates.AddRange(FindBiggerThreeWithOne(handCards, lastPlayValue));
                break;

            case PDKConstants.CardType.THREE_WITH_TWO:
                // 三带二：找三张比基准值大的，带任意两张（不必是对子）
                candidates.AddRange(FindBiggerThreeWithTwo(handCards, lastPlayValue));
                break;

            case PDKConstants.CardType.FOUR_WITH_TWO:
                // 四带二：找四张比基准值大的，带任意两张
                candidates.AddRange(FindBiggerFourWithTwo(handCards, lastPlayValue));
                break;

            case PDKConstants.CardType.FOUR_WITH_THREE:
                // 四带三：找四张比基准值大的，带任意三张
                var fourWithThreeCandidates = FindBiggerFourWithThree(handCards, lastPlayValue);
                candidates.AddRange(fourWithThreeCandidates);
                break;

            case PDKConstants.CardType.BOMB:
                // 炸弹：找比基准值大的炸弹（3最小，AAA规则下AAA最大）
                candidates.AddRange(FindBiggerBombs(handCards, lastPlayValue));
                break;

            case PDKConstants.CardType.STRAIGHT:
                // 顺子：相同长度、结尾值更大的顺子
                candidates.AddRange(FindBiggerStraights(handCards, lastPlay.cards.Count, lastPlayValue));
                break;

            case PDKConstants.CardType.STRAIGHT_PAIR:
                // 连对：相同长度、结尾值更大的连对
                candidates.AddRange(FindBiggerPairStraights(handCards, lastPlay.cards.Count / 2, lastPlayValue));
                break;

            case PDKConstants.CardType.PLANE:
                // 飞机：相同长度、结尾值更大的三连
                candidates.AddRange(FindBiggerPlanes(handCards, lastPlay.cards));
                break;
        }

        // 炸弹可压任何非炸弹牌型
        if (cardType != PDKConstants.CardType.BOMB)
        {
            var bombs = FindAllBombs(handCards);
            if (bombs.Count > 0)
            {
                candidates.AddRange(bombs);
            }
            else
            {
            }
        }

        // 去重并按牌面值排序
        candidates = candidates
            .Distinct(new CardListComparer())
            .OrderBy(combo => GetBaseValueFromCards(combo))
            .ThenBy(combo => combo.Count > 0 ? combo.Min(c => GetSuitPriorityForHint(c.suit)) : 0)
            .ToList();

        return candidates;
    }

    /// <summary>
    /// 获取牌组的基准值（用于比较大小）
    /// </summary>
    private int GetBaseValueFromCards(List<PDKCardData> cards)
    {
        if (cards == null || cards.Count == 0) return 0;

        // 找出现最多的牌值作为基准（三带一取三张的值，对子取对子的值）
        var mainValue = cards.GroupBy(c => c.value)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => (int)g.Key)
            .First()
            .Key;

        return (int)mainValue;
    }

    /// <summary>
    /// 获取花色在提示中的优先级（从小往大找时的排序权重）
    /// 方块优先（权重最小），然后梅花、红桃、黑桃
    /// </summary>
    private int GetSuitPriorityForHint(PDKConstants.CardSuit suit)
    {
        switch (suit)
        {
            case PDKConstants.CardSuit.Diamonds: return 0; // 方块优先
            case PDKConstants.CardSuit.Clubs:    return 1; // 梅花
            case PDKConstants.CardSuit.Hearts:   return 2; // 红桃
            case PDKConstants.CardSuit.Spades:   return 3; // 黑桃
            default: return 99;
        }
    }

    /// <summary>
    /// 查找比基准值大的单张
    /// </summary>
    private List<List<PDKCardData>> FindBiggerSingles(List<PDKCardData> handCards, int baseValue)
    {
        return handCards
            .Where(c => (int)c.value > baseValue)
            .OrderBy(c => (int)c.value)
            .ThenBy(c => GetSuitPriorityForHint(c.suit)) // 同点数优先方块
            .Select(c => new List<PDKCardData> { c })
            .ToList();
    }

    /// <summary>
    /// 查找比基准值大的对子
    /// </summary>
    private List<List<PDKCardData>> FindBiggerPairs(List<PDKCardData> handCards, int baseValue)
    {
        return handCards
            .GroupBy(c => c.value)
            .Where(g => g.Count() >= 2 && (int)g.Key > baseValue)
            .OrderBy(g => (int)g.Key)
            .Select(g => g.OrderBy(c => GetSuitPriorityForHint(c.suit)).Take(2).ToList()) // 优先方块
            .ToList();
    }

    /// <summary>
    /// 查找比基准值大的三带一
    /// </summary>
    private List<List<PDKCardData>> FindBiggerThreeWithOne(List<PDKCardData> handCards, int baseValue)
    {
        var result = new List<List<PDKCardData>>();
        var triples = handCards
            .GroupBy(c => c.value)
            .Where(g => g.Count() >= 3 && (int)g.Key > baseValue)
            .OrderBy(g => (int)g.Key)
            .ToList();

        foreach (var triple in triples)
        {
            var threeCards = triple.OrderBy(c => GetSuitPriorityForHint(c.suit)).Take(3).ToList();
            var remaining = handCards.Except(threeCards).ToList();

            if (remaining.Count > 0)
            {
                // 带最小的单张
                var wing = remaining.OrderBy(c => (int)c.value)
                                   .ThenBy(c => GetSuitPriorityForHint(c.suit))
                                   .First();
                var combo = new List<PDKCardData>(threeCards) { wing };
                result.Add(combo);
            }
        }

        return result;
    }

    /// <summary>
    /// 查找比基准值大的三带二
    /// </summary>
    private List<List<PDKCardData>> FindBiggerThreeWithTwo(List<PDKCardData> handCards, int baseValue)
    {
        var result = new List<List<PDKCardData>>();
        var triples = handCards
            .GroupBy(c => c.value)
            .Where(g => g.Count() >= 3 && (int)g.Key > baseValue)
            .OrderBy(g => (int)g.Key)
            .ToList();

        foreach (var triple in triples)
        {
            var threeCards = triple.OrderBy(c => GetSuitPriorityForHint(c.suit)).Take(3).ToList();
            var remaining = handCards.Except(threeCards).ToList();

            if (remaining.Count >= 2)
            {
                // 带最小的两张
                var wings = remaining.OrderBy(c => (int)c.value)
                                    .ThenBy(c => GetSuitPriorityForHint(c.suit))
                                    .Take(2).ToList();
                var combo = new List<PDKCardData>(threeCards);
                combo.AddRange(wings);
                result.Add(combo);
            }
        }

        return result;
    }

    /// <summary>
    /// 查找比基准值大的四带二
    /// </summary>
    private List<List<PDKCardData>> FindBiggerFourWithTwo(List<PDKCardData> handCards, int baseValue)
    {
        var result = new List<List<PDKCardData>>();
        var quads = handCards
            .GroupBy(c => c.value)
            .Where(g => g.Count() == 4 && (int)g.Key > baseValue)
            .OrderBy(g => (int)g.Key)
            .ToList();

        foreach (var quad in quads)
        {
            var fourCards = quad.ToList();
            var remaining = handCards.Except(fourCards).ToList();
            if (remaining.Count >= 2)
            {
                var wings = remaining.OrderBy(c => (int)c.value)
                                    .ThenBy(c => GetSuitPriorityForHint(c.suit))
                                    .Take(2).ToList();
                var combo = new List<PDKCardData>(fourCards);
                combo.AddRange(wings);
                result.Add(combo);
            }
        }
        return result;
    }

    /// <summary>
    /// 查找比基准值大的四带三
    /// </summary>
    private List<List<PDKCardData>> FindBiggerFourWithThree(List<PDKCardData> handCards, int baseValue)
    {
        var result = new List<List<PDKCardData>>();
        // 统计手牌中的四张牌
        var allGroups = handCards.GroupBy(c => c.value).OrderBy(g => (int)g.Key).ToList();
        var quads = allGroups
            .Where(g => g.Count() == 4 && (int)g.Key > baseValue)
            .ToList();
        foreach (var quad in quads)
        {
            var fourCards = quad.ToList();
            var remaining = handCards.Except(fourCards).ToList();
            if (remaining.Count >= 3)
            {
                var wings = remaining.OrderBy(c => (int)c.value)
                                    .ThenBy(c => GetSuitPriorityForHint(c.suit))
                                    .Take(3).ToList();
                var combo = new List<PDKCardData>(fourCards);
                combo.AddRange(wings);
                result.Add(combo);
            }
            else
            {
            }
        }

        return result;
    }

    /// <summary>
    /// 查找比基准值大的炸弹（包括AAA规则）
    /// </summary>
    private List<List<PDKCardData>> FindBiggerBombs(List<PDKCardData> handCards, int baseValue)
    {
        var rules = GetGameRules();
        var result = new List<List<PDKCardData>>();
        
        // 查找比基准值大的4张炸弹
        var fourBombs = handCards
            .GroupBy(c => c.value)
            .Where(g => g.Count() == 4 && (int)g.Key > baseValue)
            .OrderBy(g => (int)g.Key)
            .Select(g => g.ToList())
            .ToList();
        result.AddRange(fourBombs);
        
        // 【关键修复】如果开启AAA为炸弹规则且基准值小于A(14)，AAA可以压过
        if (rules.aaaIsBomb)
        {
            // AAA的逻辑值是14（A），如果基准值 < 14，AAA可以压
            int aceValue = (int)PDKConstants.CardValue.Ace; // 14
            if (baseValue < aceValue)
            {
                var threeAces = handCards
                    .Where(c => c.value == PDKConstants.CardValue.Ace)
                    .ToList();
                if (threeAces.Count >= 3)
                {
                    // 只取3张A作为炸弹
                    result.Add(threeAces.Take(3).ToList());
                    GF.LogInfo($"[PDK炸弹] AAA规则开启，AAA(值14)可以压过基准值{baseValue}");
                }
            }
        }
        
        return result;
    }

    /// <summary>
    /// 查找所有炸弹（包括AAA规则）
    /// </summary>
    private List<List<PDKCardData>> FindAllBombs(List<PDKCardData> handCards)
    {
        var rules = GetGameRules();
        var result = new List<List<PDKCardData>>();
        
        // 查找4张炸弹
        var fourBombs = handCards
            .GroupBy(c => c.value)
            .Where(g => g.Count() == 4)
            .OrderBy(g => (int)g.Key)
            .Select(g => g.ToList())
            .ToList();
        result.AddRange(fourBombs);
        
        // 【关键修复】如果开启AAA为炸弹规则，查找3张A
        if (rules.aaaIsBomb)
        {
            var threeAces = handCards
                .Where(c => c.value == PDKConstants.CardValue.Ace)
                .ToList();
            if (threeAces.Count >= 3)
            {
                // 只取3张A作为炸弹
                result.Add(threeAces.Take(3).ToList());
                GF.LogInfo("[PDK炸弹] AAA规则开启，检测到3张A作为炸弹");
            }
        }
        
        return result;
    }

    /// <summary>
    /// 查找比基准值大的顺子
    /// </summary>
    private List<List<PDKCardData>> FindBiggerStraights(List<PDKCardData> handCards, int length, int baseEndValue)
    {
        var result = new List<List<PDKCardData>>();
        var values = handCards
            .Where(c => c.value != PDKConstants.CardValue.Two)
            .GroupBy(c => c.value)
            .Select(g => (int)g.Key)
            .Distinct()
            .OrderBy(v => v)
            .ToList();

        if (values.Count < length) return result;

        for (int i = 0; i <= values.Count - length; i++)
        {
            // 检查是否连续
            bool consecutive = true;
            for (int k = 1; k < length; k++)
            {
                if (values[i + k] - values[i + k - 1] != 1)
                {
                    consecutive = false;
                    break;
                }
            }

            if (!consecutive) continue;

            int endValue = values[i + length - 1];
            if (endValue <= baseEndValue) continue;

            // 构建顺子（每个点数选最小花色）
            var straight = new List<PDKCardData>();
            for (int k = 0; k < length; k++)
            {
                var card = handCards
                    .Where(c => (int)c.value == values[i + k])
                    .OrderBy(c => GetSuitPriorityForHint(c.suit))
                    .First();
                straight.Add(card);
            }
            result.Add(straight);
        }

        return result;
    }

    /// <summary>
    /// 查找比基准值大的连对
    /// </summary>
    private List<List<PDKCardData>> FindBiggerPairStraights(List<PDKCardData> handCards, int pairCount, int baseEndValue)
    {
        var result = new List<List<PDKCardData>>();
        var pairGroups = handCards
            .Where(c => c.value != PDKConstants.CardValue.Two)
            .GroupBy(c => c.value)
            .Where(g => g.Count() >= 2)
            .OrderBy(g => (int)g.Key)
            .ToList();

        if (pairGroups.Count < pairCount) return result;

        for (int i = 0; i <= pairGroups.Count - pairCount; i++)
        {
            // 检查是否连续
            bool consecutive = true;
            for (int k = 1; k < pairCount; k++)
            {
                if ((int)pairGroups[i + k].Key - (int)pairGroups[i + k - 1].Key != 1)
                {
                    consecutive = false;
                    break;
                }
            }

            if (!consecutive) continue;

            int endValue = (int)pairGroups[i + pairCount - 1].Key;
            if (endValue <= baseEndValue) continue;

            // 构建连对
            var pairStraight = new List<PDKCardData>();
            for (int k = 0; k < pairCount; k++)
            {
                pairStraight.AddRange(pairGroups[i + k].OrderBy(c => GetSuitPriorityForHint(c.suit)).Take(2));
            }
            result.Add(pairStraight);
        }

        return result;
    }

    /// <summary>
    /// 查找比基准值大的飞机
    /// </summary>
    private List<List<PDKCardData>> FindBiggerPlanes(List<PDKCardData> handCards, List<PDKCardData> lastPlayCards)
    {
        // 分析上家飞机结构
        var lastTriples = lastPlayCards.GroupBy(c => c.value).Where(g => g.Count() >= 3).ToList();
        int tripleCount = lastTriples.Count;
        int attachCount = lastPlayCards.Count - tripleCount * 3;
        int baseEndValue = lastTriples.Max(g => (int)g.Key);

        var result = new List<List<PDKCardData>>();
        var tripleGroups = handCards
            .Where(c => c.value != PDKConstants.CardValue.Two)
            .GroupBy(c => c.value)
            .Where(g => g.Count() >= 3)
            .OrderBy(g => (int)g.Key)
            .ToList();

        if (tripleGroups.Count < tripleCount) return result;

        for (int i = 0; i <= tripleGroups.Count - tripleCount; i++)
        {
            // 检查是否连续
            bool consecutive = true;
            for (int k = 1; k < tripleCount; k++)
            {
                if ((int)tripleGroups[i + k].Key - (int)tripleGroups[i + k - 1].Key != 1)
                {
                    consecutive = false;
                    break;
                }
            }

            if (!consecutive) continue;

            int endValue = (int)tripleGroups[i + tripleCount - 1].Key;
            if (endValue <= baseEndValue) continue;

            // 构建飞机主体
            var plane = new List<PDKCardData>();
            for (int k = 0; k < tripleCount; k++)
            {
                plane.AddRange(tripleGroups[i + k].OrderBy(c => GetSuitPriorityForHint(c.suit)).Take(3));
            }

            // 添加附件
            if (attachCount > 0)
            {
                var remaining = handCards.Except(plane).ToList();
                if (remaining.Count >= attachCount)
                {
                    plane.AddRange(remaining.OrderBy(c => (int)c.value)
                                           .ThenBy(c => GetSuitPriorityForHint(c.suit))
                                           .Take(attachCount));
                    result.Add(plane);
                }
            }
            else
            {
                result.Add(plane);
            }
        }

        return result;
    }



    #endregion // 纯逻辑方法（无状态依赖）

    #region 网络请求方法

    /// <summary>
    /// 执行出牌（发送网络请求到服务器）
    /// 流程说明：
    /// 1. 客户端 Panel 调用此方法发送出牌请求
    /// 2. 服务器验证并广播出牌结果
    /// 3. 客户端收到服务器响应后在 Panel 中更新游戏状态
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="cardIds">卡牌ID列表（0-51的服务器卡牌编号）</param>
    public void ExecutePlayCards(int playerId, List<int> cardIds)
    {
        // 创建出牌请求消息
        NetMsg.Msg_RanFastDiscardRq req = MessagePool.Instance.Fetch<NetMsg.Msg_RanFastDiscardRq>();

        // 直接添加卡牌ID列表（0-51的服务器编号）
        req.Cards.AddRange(cardIds);

        // 日志输出
        string cardIdsStr = string.Join(",", req.Cards);

        // 发送消息到服务器
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(
            HotfixNetworkComponent.AccountClientName,
            MessageID.Msg_RanFastDiscardRq,
            req);
    }

    #endregion
}

/// <summary>
/// 卡牌列表比较器，用于去重候选牌组
/// </summary>
public class CardListComparer : IEqualityComparer<List<PDKCardData>>
{
    public bool Equals(List<PDKCardData> x, List<PDKCardData> y)
    {
        if (x == null || y == null) return x == y;
        if (x.Count != y.Count) return false;

        // 按CardId排序后逐一比较
        var xSorted = x.OrderBy(c => c.CardId).ToList();
        var ySorted = y.OrderBy(c => c.CardId).ToList();

        for (int i = 0; i < xSorted.Count; i++)
        {
            if (xSorted[i].CardId != ySorted[i].CardId) return false;
        }
        return true;
    }

    public int GetHashCode(List<PDKCardData> obj)
    {
        if (obj == null) return 0;

        // 计算所有卡牌CardId的异或值作为哈希码（顺序无关）
        int hash = 0;
        foreach (var card in obj)
        {
            hash ^= card.CardId.GetHashCode();
        }
        return hash;
    }
}

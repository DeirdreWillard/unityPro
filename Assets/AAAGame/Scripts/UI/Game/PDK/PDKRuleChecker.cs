using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// PDK规则检查器
/// =============================================
/// 功能：牌型检测、权重计算、规则验证
/// 
/// 模块结构：
/// ├── 1. 牌型检测 (DetectCardType)
/// ├── 2. 权重计算 (CalculateWeight)
/// ├── 3. 出牌验证 (CanPlayCards)
/// ├── 4. 规则检查 (ContainsMinCard, ContainsBlackThree, IsBombSplit)
/// └── 5. 牌型判断辅助 (IsPair, IsTriple, IsStraight, IsBomb等)
/// 
/// 最后更新：2025-12-12
/// </summary>
public class PDKRuleChecker
{
    /// <summary>
    /// 检测牌型
    /// </summary>
    public static PDKConstants.CardType DetectCardType(List<PDKCardData> cards, List<PDKCardData> allHandCards = null)
    {
        if (cards == null || cards.Count == 0)
            return PDKConstants.CardType.UNNOWN;
        
        // 排序
        var sortedCards = cards.OrderBy(c => c.value).ToList();
        int count = sortedCards.Count;
        // 检查各种规则
        bool hasRule1 = PDKConstants.CheckboxRules != null && Array.Exists(PDKConstants.CheckboxRules, x => x == 1); // 可四带二
        bool hasRule2 = PDKConstants.CheckboxRules != null && Array.Exists(PDKConstants.CheckboxRules, x => x == 2); // 可四带三
        bool hasRule3 = PDKConstants.CheckboxRules != null && Array.Exists(PDKConstants.CheckboxRules, x => x == 3); // 炸弹不可拆
        bool hasRule4 = PDKConstants.CheckboxRules != null && Array.Exists(PDKConstants.CheckboxRules, x => x == 4); // AAA为炸弹
        bool hasRule8 = PDKConstants.CheckboxRules != null && Array.Exists(PDKConstants.CheckboxRules, x => x == 8); // 三带任意出
        bool hasRule9 = PDKConstants.CheckboxRules != null && Array.Exists(PDKConstants.CheckboxRules, x => x == 9); // 最后三张可少带完
        // 规则9：最后三张可少带完 - 只要是出完手中所有的牌，且包含三张/飞机，就允许少带
        bool isFinishHand = allHandCards != null && cards.Count == allHandCards.Count && hasRule9;
        // 【关键】规则3：炸弹不可拆 - 在检测牌型之前先检查是否拆了炸弹
        if (hasRule3 && allHandCards != null && allHandCards.Count > 0)
        {
            if (IsBombSplitInDetect(cards, allHandCards, hasRule4))
            {
                return PDKConstants.CardType.UNNOWN; // 拆了炸弹，返回无效牌型
            }
        }
        switch (count)
        {
            case 1:
                return PDKConstants.CardType.SINGLE;
            case 2:
                return IsPair(sortedCards) ? PDKConstants.CardType.PAIR : PDKConstants.CardType.UNNOWN;
            case 3:
                // 规则4: AAA为炸弹 - 检查是否为3个A
                if (hasRule4 && IsTriple(sortedCards) && sortedCards[0].value == PDKConstants.CardValue.Ace)
                    return PDKConstants.CardType.BOMB;
                // 如果是三张相同
                if (IsTriple(sortedCards))
                {
                    // 规则9：最后3张可少带完 - 出完最后3张时可以直接出
                    if (isFinishHand)
                        return PDKConstants.CardType.THREE_WITH_NO;
                    // 规则8：三带任意出 - 可以3不带
                    if (hasRule8)
                        return PDKConstants.CardType.THREE_WITH_NO;
                }
                // 没有规则8且不是最后3张，3张牌不能单独出
                return PDKConstants.CardType.UNNOWN;
            case 4:
                // 先检查是否为炸弹（4张相同）
                if (IsBomb(sortedCards))
                    return PDKConstants.CardType.BOMB;
                // 检查是否为连对（2个对子）
                if (IsPairStraight(sortedCards))
                    return PDKConstants.CardType.STRAIGHT_PAIR;
                // 检查是否为三带一
                if (IsTripleWithOne(sortedCards))
                {
                    // 规则9：最后三张可少带完 - 出完最后4张时允许三带一
                    if (isFinishHand)
                        return PDKConstants.CardType.THREE_WITH_ONE;
                    // 规则8：三带任意出 - 可以3带1
                    if (hasRule8)
                        return PDKConstants.CardType.THREE_WITH_ONE;
                }
                return PDKConstants.CardType.UNNOWN;
            case 5:
                // 3带2（不管有没有规则8都可以，可以是1对+1单或2个单牌）
                if (IsTripleWithTwo(sortedCards))
                    return PDKConstants.CardType.THREE_WITH_TWO;
                // 检查顺子
                if (IsStraight(sortedCards))
                    return PDKConstants.CardType.STRAIGHT;
                return PDKConstants.CardType.UNNOWN;
            case 6:
                // 规则1: 可四带二 - 4张相同 + 2张单牌
                if (hasRule1 && IsBombWithTwo(sortedCards))
                    return PDKConstants.CardType.FOUR_WITH_TWO;
                // 检查顺子
                if (IsStraight(sortedCards))
                    return PDKConstants.CardType.STRAIGHT;
                // 检查连对
                if (IsPairStraight(sortedCards))
                    return PDKConstants.CardType.STRAIGHT_PAIR;
                // 检查飞机（最后出完时允许少带）
                if (IsTripleStraight(sortedCards, isFinishHand))
                    return PDKConstants.CardType.PLANE;
                return PDKConstants.CardType.UNNOWN;
            case 7:
                // 规则2: 可四带三 - 4张相同 + 3张（可以是三张或1+2）
                if (hasRule2 && IsBombWithThree(sortedCards))
                    return PDKConstants.CardType.FOUR_WITH_THREE;
                // 检查顺子
                if (IsStraight(sortedCards))
                    return PDKConstants.CardType.STRAIGHT;
                // 检查飞机（最后出完时允许少带）
                if (IsTripleStraight(sortedCards, isFinishHand))
                    return PDKConstants.CardType.PLANE;
                return PDKConstants.CardType.UNNOWN;
            default:
                // 检查顺子
                if (IsStraight(sortedCards))
                    return PDKConstants.CardType.STRAIGHT;
                // 检查连对
                if (IsPairStraight(sortedCards))
                    return PDKConstants.CardType.STRAIGHT_PAIR;
                // 检查飞机（最后出完时允许少带）
                if (IsTripleStraight(sortedCards, isFinishHand))
                    return PDKConstants.CardType.PLANE;
                return PDKConstants.CardType.UNNOWN;
        }
        
    }
    
    /// <summary>
    /// 计算牌型权重
    /// </summary>
    public static int CalculateWeight(List<PDKCardData> cards, PDKConstants.CardType cardType)
    {
        if (cards == null || cards.Count == 0)
            return 0;
        
        var sortedCards = cards.OrderBy(c => c.value).ToList();
        
        switch (cardType)
        {
            case PDKConstants.CardType.SINGLE:
                return (int)sortedCards[0].value;
            
            case PDKConstants.CardType.PAIR:
                return (int)sortedCards[0].value;
            
            case PDKConstants.CardType.THREE_WITH_NO:
            case PDKConstants.CardType.THREE_WITH_ONE:
            case PDKConstants.CardType.THREE_WITH_TWO:
                // For triples, we need to find the triple value
                var groups = sortedCards.GroupBy(c => c.value).OrderByDescending(g => g.Count()).ToList();
                return (int)groups[0].Key;
            
            case PDKConstants.CardType.BOMB:
                return (int)sortedCards[0].value + 1000; // 炸弹权重很高
            
            case PDKConstants.CardType.FOUR_WITH_TWO:
            case PDKConstants.CardType.FOUR_WITH_THREE:
                 // For four with X, we need to find the four value
                var bombGroups = sortedCards.GroupBy(c => c.value).OrderByDescending(g => g.Count()).ToList();
                return (int)bombGroups[0].Key;

            case PDKConstants.CardType.STRAIGHT:
                return (int)sortedCards.Last().value; // 以最大牌为准
            
            case PDKConstants.CardType.STRAIGHT_PAIR:
                return (int)sortedCards.Last().value; // 以最大对子为准
            
            case PDKConstants.CardType.PLANE:
                // For plane, we need to find the largest triple value
                var planeGroups = sortedCards.GroupBy(c => c.value).Where(g => g.Count() >= 3).OrderBy(g => g.Key).ToList();
                return (int)planeGroups.Last().Key;
            
            default:
                return 0;
        }
    }
    
    /// <summary>
    /// 检查是否可以出牌
    /// </summary>
    public static bool CanPlayCards(List<PDKCardData> playCards, List<PDKCardData> handCards, PDKPlayCardData lastPlay, PDKConstants.GameRuleConfig rules)
    {
        if (playCards == null || playCards.Count == 0)
            return false; // 不能出空牌
        
        var cardType = DetectCardType(playCards);
        if (cardType == PDKConstants.CardType.UNNOWN)
            return false; // 无效牌型
        
        // 规则3：炸弹不可拆 - 检查是否拆掉了炸弹
        if (IsBombSplit(playCards, handCards))
            return false; // 拆了炸弹，不允许出牌
        
        // 如果没有上次出牌，可以随意出
        if (lastPlay == null || lastPlay.IsPass)
            return true;
        
        // 炸弹可以压任何牌型
        if (cardType == PDKConstants.CardType.BOMB) 
        {
            // 如果上次也是炸弹，需要比较大小
            if (lastPlay.cardType == PDKConstants.CardType.BOMB)
            {
                int bombWeight = CalculateWeight(playCards, cardType);
                return bombWeight > lastPlay.weight;
            }
            return true;
        }
        
        // 非炸弹不能压炸弹
        if (lastPlay.cardType == PDKConstants.CardType.BOMB)
            return false;
        
        // 牌型必须相同
        if (cardType != lastPlay.cardType)
            return false;
        
        // 牌数必须相同
        if (playCards.Count != lastPlay.cards.Count)
            return false;
        
        // 比较权重
        int currentWeight = CalculateWeight(playCards, cardType);
        return currentWeight > lastPlay.weight;
    }
    
    /// <summary>
    /// 检查是否包含最小牌(必带最小规则)
    /// </summary>
    public static bool ContainsMinCard(List<PDKCardData> playCards, List<PDKCardData> handCards)
    {
        if (playCards == null || handCards == null || playCards.Count == 0 || handCards.Count == 0)
            return false;
        
        var minCard = handCards.OrderBy(c => c.value).ThenBy(c => c.suit).First();
        return playCards.Contains(minCard);
    }
    
    /// <summary>
    /// 检查是否包含黑桃3(首局黑桃3先出规则)
    /// </summary>
    public static bool ContainsBlackThree(List<PDKCardData> playCards)
    {
        if (playCards == null || playCards.Count == 0)
            return false;
        
        return playCards.Any(c => c.suit == PDKConstants.CardSuit.Spades && c.value == PDKConstants.CardValue.Three);
    }
    
    /// <summary>
    /// 检查是否拆掉了炸弹（炸弹不可拆规则）- 用于DetectCardType内部检测
    /// </summary>
    /// <param name="playCards">要出的牌</param>
    /// <param name="handCards">当前所有手牌（包含要出的牌）</param>
    /// <param name="hasRule4">是否有AAA为炸弹规则</param>
    /// <returns>如果拆掉了炸弹返回true</returns>
    private static bool IsBombSplitInDetect(List<PDKCardData> playCards, List<PDKCardData> handCards, bool hasRule4)
    {
        if (playCards == null || handCards == null || playCards.Count == 0 || handCards.Count == 0)
            return false;
        
        // 【关键修复】检查手牌中每个点数的牌，看是否构成炸弹且被拆分
        var handCardGroups = handCards.GroupBy(c => c.value);
        
        foreach (var group in handCardGroups)
        {
            var cardValue = group.Key;
            int totalCount = group.Count(); // 手牌中该点数的总数
            int playCount = playCards.Count(c => c.value == cardValue); // 要出的该点数的数量
            
            // 情况1：手牌中有4张相同（4张炸弹）
            if (totalCount == 4)
            {
                // 如果出了该点数的牌，但不是全部4张，说明拆了炸弹
                if (playCount > 0 && playCount < 4)
                {
                    Debug.Log($"[PDK规则] 检测到拆炸弹：手牌有{totalCount}张{cardValue}，但只出了{playCount}张");
                    return true;
                }
            }
            
            // 情况2：规则4生效，手牌中有3个A（AAA炸弹）
            if (hasRule4 && cardValue == PDKConstants.CardValue.Ace && totalCount == 3)
            {
                // 如果出了A，但不是全部3张，说明拆了AAA炸弹
                if (playCount > 0 && playCount < 3)
                {
                    Debug.Log($"[PDK规则] 检测到拆AAA炸弹：手牌有{totalCount}张A，但只出了{playCount}张");
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查是否拆掉了炸弹（炸弹不可拆规则）
    /// </summary>
    /// <param name="playCards">要出的牌</param>
    /// <param name="handCards">当前所有手牌（包含要出的牌）</param>
    /// <returns>如果拆掉了炸弹返回true</returns>
    public static bool IsBombSplit(List<PDKCardData> playCards, List<PDKCardData> handCards)
    {
        if (playCards == null || handCards == null || playCards.Count == 0 || handCards.Count == 0)
            return false;
        
        // 检查是否有规则3（炸弹不可拆）
        bool hasRule3 = PDKConstants.CheckboxRules != null && Array.Exists(PDKConstants.CheckboxRules, x => x == 3);
        if (!hasRule3)
            return false; // 没有炸弹不可拆规则，不需要检查
        
        // 检查是否有规则4（AAA为炸弹）
        bool hasRule4 = PDKConstants.CheckboxRules != null && Array.Exists(PDKConstants.CheckboxRules, x => x == 4);
        
        // 如果出的牌本身就是炸弹，不算拆炸弹
        var playCardType = DetectCardType(playCards, handCards);
        if (playCardType == PDKConstants.CardType.BOMB)
            return false;
        
        return IsBombSplitInDetect(playCards, handCards, hasRule4);
    }
    
    #region 牌型检测
    
    /// <summary>
    /// 是否为对子
    /// </summary>
    private static bool IsPair(List<PDKCardData> cards)
    {
        return cards.Count == 2 && cards[0].value == cards[1].value;
    }
    
    /// <summary>
    /// 是否为三张
    /// </summary>
    private static bool IsTriple(List<PDKCardData> cards)
    {
        return cards.Count == 3 && 
               cards[0].value == cards[1].value && 
               cards[1].value == cards[2].value;
    }
    
    /// <summary>
    /// 是否为三带一
    /// </summary>
    private static bool IsTripleWithOne(List<PDKCardData> cards)
    {
        if (cards.Count != 4)
            return false;
        
        var groups = cards.GroupBy(c => c.value).OrderByDescending(g => g.Count()).ToList();
        
        // 必须有一个3张的组和一个1张的组
        return groups.Count == 2 && groups[0].Count() == 3 && groups[1].Count() == 1;
    }
    
    /// <summary>
    /// 是否为三带二
    /// </summary>
    private static bool IsTripleWithTwo(List<PDKCardData> cards)
    {
        if (cards.Count != 5)
            return false;
        
        var groups = cards.GroupBy(c => c.value).OrderByDescending(g => g.Count()).ToList();
        
        // 必须有一个3张的组
        if (groups.Count < 1 || groups[0].Count() != 3)
            return false;
        
        // 剩余2张可以是：1对(2张相同) 或 2个单牌(2张不同)
        if (groups.Count == 2)
        {
            // 3张 + 1对
            return groups[1].Count() == 2;
        }
        else if (groups.Count == 3)
        {
            // 3张 + 2个单牌
            return groups[1].Count() == 1 && groups[2].Count() == 1;
        }
        
        return false;
    }
    
    /// <summary>
    /// 是否为炸弹
    /// </summary>
    private static bool IsBomb(List<PDKCardData> cards)
    {
        return cards.Count == 4 && 
               cards[0].value == cards[1].value && 
               cards[1].value == cards[2].value && 
               cards[2].value == cards[3].value;
    }
    
    /// <summary>
    /// 是否为四带二（炸弹带2张单牌）
    /// </summary>
    private static bool IsBombWithTwo(List<PDKCardData> cards)
    {
        if (cards.Count != 6)
            return false;
        
        var groups = cards.GroupBy(c => c.value).OrderByDescending(g => g.Count()).ToList();
        
        // 必须有一个4张的组（炸弹）
        if (groups.Count < 1 || groups[0].Count() != 4)
            return false;
        
        // 剩余2张可以是：1对(2张相同) 或 2个单牌(2张不同)
        if (groups.Count == 2)
        {
            // 4张 + 1对
            return groups[1].Count() == 2;
        }
        else if (groups.Count == 3)
        {
            // 4张 + 2个单牌
            return groups[1].Count() == 1 && groups[2].Count() == 1;
        }
        
        return false;
    }
    
    /// <summary>
    /// 是否为四带三（炸弹带3张牌）
    /// </summary>
    private static bool IsBombWithThree(List<PDKCardData> cards)
    {
        if (cards.Count != 7)
            return false;
        
        var groups = cards.GroupBy(c => c.value).OrderByDescending(g => g.Count()).ToList();
        
        // 必须有一个4张的组（炸弹）
        if (groups.Count < 1 || groups[0].Count() != 4)
            return false;
        
        // 剩余3张可以是：3张相同 或 1对+1单 或 3个单牌
        if (groups.Count == 2)
        {
            // 4张 + 3张相同
            return groups[1].Count() == 3;
        }
        else if (groups.Count == 3)
        {
            // 4张 + 1对 + 1单
            return (groups[1].Count() == 2 && groups[2].Count() == 1) ||
                   (groups[1].Count() == 1 && groups[2].Count() == 2);
        }
        else if (groups.Count == 4)
        {
            // 4张 + 3个单牌
            return groups[1].Count() == 1 && groups[2].Count() == 1 && groups[3].Count() == 1;
        }
        
        return false;
    }
    
    /// <summary>
    /// 是否为顺子
    /// </summary>
    private static bool IsStraight(List<PDKCardData> cards)
    {
        if (cards.Count < PDKConstants.MIN_STRAIGHT_LENGTH)
            return false;
        
        var values = cards.Select(c => (int)c.value).Distinct().OrderBy(v => v).ToList();
        
        // 数量必须相等(不能有重复)
        if (values.Count != cards.Count)
            return false;
        
        // 2不能在顺子中
        if (values.Contains((int)PDKConstants.CardValue.Two))
            return false;
        
        // 检查连续性
        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] - values[i - 1] != 1)
                return false;
        }
        
        // 【关键修复】A(14)可以作为最大牌：10-J-Q-K-A 是合法顺子
        // 顺子范围：3-4-5-6-7 到 10-J-Q-K-A
        // 最大值必须 <= 14 (A)
        if (values.Max() > (int)PDKConstants.CardValue.Ace)
            return false;
        
        return true;
    }
    
    /// <summary>
    /// 是否为连对
    /// </summary>
    private static bool IsPairStraight(List<PDKCardData> cards)
    {
        if (cards.Count < PDKConstants.MIN_PAIR_STRAIGHT_LENGTH * 2 || cards.Count % 2 != 0)
            return false;
        
        var groups = cards.GroupBy(c => c.value).OrderBy(g => g.Key).ToList();
        
        // 每组必须有2张牌
        if (groups.Any(g => g.Count() != 2))
            return false;
        
        var values = groups.Select(g => (int)g.Key).ToList();
        
        // 检查连续性
        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] - values[i - 1] != 1)
                return false;
        }
        
        // 2不能在连对中
        if (values.Contains((int)PDKConstants.CardValue.Two))
            return false;
        
        return true;
    }
    
    /// <summary>
    /// 是否为飞机(三带)
    /// </summary>
    private static bool IsTripleStraight(List<PDKCardData> cards, bool allowLessWing = false)
    {
        if (cards.Count < 6) // 最少6张(2个三张)
            return false;

        var groups = cards.GroupBy(c => c.value).OrderBy(g => g.Key).ToList();
        var tripleValues = groups.Where(g => g.Count() >= 3)
                                 .Select(g => (int)g.Key)
                                 .Where(v => v != (int)PDKConstants.CardValue.Two) // 2不能在飞机主体中
                                 .OrderBy(v => v)
                                 .ToList();

        if (tripleValues.Count < 2) return false;

        // 【关键修复】寻找最长的连续三张子序列
        // 因为玩家可能选中了 333+777+888+9，其中 777-888 是飞机主体，333 是带出的牌
        List<List<int>> consecutiveSequences = new List<List<int>>();
        if (tripleValues.Count > 0)
        {
            List<int> currentSeq = new List<int> { tripleValues[0] };
            for (int i = 1; i < tripleValues.Count; i++)
            {
                if (tripleValues[i] == tripleValues[i - 1] + 1)
                {
                    currentSeq.Add(tripleValues[i]);
                }
                else
                {
                    consecutiveSequences.Add(new List<int>(currentSeq));
                    currentSeq = new List<int> { tripleValues[i] };
                }
            }
            consecutiveSequences.Add(currentSeq);
        }

        // 遍历所有可能的连续长度
        foreach (var seq in consecutiveSequences)
        {
            // 飞机长度 n 可以是 seq.Count 到 2 之间的任何值
            for (int n = seq.Count; n >= 2; n--)
            {
                // 在这个连续序列中，长度为 n 的子序列可能有多个
                for (int startIdx = 0; startIdx <= seq.Count - n; startIdx++)
                {
                    int bodyCards = n * 3;
                    int wingCards = cards.Count - bodyCards;

                    // 检查带牌数量是否合理
                    // PDK常见规则：
                    // 1. 不带翅膀：wingCards == 0
                    // 2. 带单张翅膀：wingCards == n
                    // 3. 带对子翅膀：wingCards == n * 2
                    bool isValidNormal = (wingCards == 0 || wingCards == n || wingCards == n * 2);

                    // 规则9：最后出完时允许少带
                    if (allowLessWing)
                    {
                        // 只要有主体，剩下的牌不超过最大翅膀量即认为合法
                        if (wingCards >= 0 && wingCards <= n * 2) return true;
                    }

                    if (isValidNormal) return true;
                }
            }
        }

        return false;
    }
    
    #endregion
    
    #region 智能提示
    
    /// <summary>
    /// 获取可出牌提示
    /// </summary>
    public static List<List<PDKCardData>> GetPlayableCardHints(List<PDKCardData> handCards, PDKPlayCardData lastPlay, PDKConstants.GameRuleConfig rules)
    {
        var hints = new List<List<PDKCardData>>();
        
        if (handCards == null || handCards.Count == 0)
            return hints;
        
        // 如果没有上次出牌，可以出任何有效牌型
        if (lastPlay == null || lastPlay.IsPass)
        {
            hints.AddRange(GetAllValidCardTypes(handCards));
        }
        else
        {
            // 寻找可以压过上次出牌的牌型
            hints.AddRange(GetCounterCards(handCards, lastPlay));
        }
        
        return hints;
    }
    
    /// <summary>
    /// 获取所有有效牌型
    /// </summary>
    private static List<List<PDKCardData>> GetAllValidCardTypes(List<PDKCardData> handCards)
    {
        var result = new List<List<PDKCardData>>();
        
        // 单张
        foreach (var card in handCards)
        {
            result.Add(new List<PDKCardData> { card });
        }
        
        // 对子
        var pairs = handCards.GroupBy(c => c.value).Where(g => g.Count() >= 2);
        foreach (var pair in pairs)
        {
            result.Add(pair.Take(2).ToList());
        }
        
        // 三张
        var triples = handCards.GroupBy(c => c.value).Where(g => g.Count() >= 3);
        foreach (var triple in triples)
        {
            result.Add(triple.Take(3).ToList());
        }
        
        // 炸弹
        var bombs = handCards.GroupBy(c => c.value).Where(g => g.Count() == 4);
        foreach (var bomb in bombs)
        {
            result.Add(bomb.ToList());
        }
        
        // TODO: 添加顺子、连对、飞机的检测
        
        return result;
    }
    
    /// <summary>
    /// 获取可以压过指定出牌的牌型
    /// </summary>
    private static List<List<PDKCardData>> GetCounterCards(List<PDKCardData> handCards, PDKPlayCardData lastPlay)
    {
        var result = new List<List<PDKCardData>>();
        
        // 炸弹可以压任何牌型
        var bombs = handCards.GroupBy(c => c.value).Where(g => g.Count() == 4);
        foreach (var bomb in bombs)
        {
            var bombCards = bomb.ToList();
            if (lastPlay.cardType != PDKConstants.CardType.BOMB || 
                CalculateWeight(bombCards, PDKConstants.CardType.BOMB) > lastPlay.weight)
            {
                result.Add(bombCards);
            }
        }
        
        // 相同牌型的更大牌
        switch (lastPlay.cardType)
        {
            case PDKConstants.CardType.SINGLE:
                result.AddRange(GetBiggerSingles(handCards, lastPlay.weight));
                break;
            case PDKConstants.CardType.PAIR:
                result.AddRange(GetBiggerPairs(handCards, lastPlay.weight));
                break;
            case PDKConstants.CardType.THREE_WITH_NO:
            case PDKConstants.CardType.THREE_WITH_ONE:
            case PDKConstants.CardType.THREE_WITH_TWO:
                result.AddRange(GetBiggerTriples(handCards, lastPlay.weight));
                break;
            // TODO: 添加其他牌型的处理
        }
        
        return result;
    }
    
    private static List<List<PDKCardData>> GetBiggerSingles(List<PDKCardData> handCards, int minWeight)
    {
        return handCards.Where(c => (int)c.value > minWeight)
                       .Select(c => new List<PDKCardData> { c })
                       .ToList();
    }
    
    private static List<List<PDKCardData>> GetBiggerPairs(List<PDKCardData> handCards, int minWeight)
    {
        return handCards.GroupBy(c => c.value)
                       .Where(g => g.Count() >= 2 && (int)g.Key > minWeight)
                       .Select(g => g.Take(2).ToList())
                       .ToList();
    }
    
    private static List<List<PDKCardData>> GetBiggerTriples(List<PDKCardData> handCards, int minWeight)
    {
        return handCards.GroupBy(c => c.value)
                       .Where(g => g.Count() >= 3 && (int)g.Key > minWeight)
                       .Select(g => g.Take(3).ToList())
                       .ToList();
    }
    
    #endregion
}

using System.Collections.Generic;
using NetMsg;
using System.Linq;

/// <summary>
/// 仙桃晃晃麻将规则实现
/// 特点：
/// 1. 136张牌
/// 2. 有赖子玩法
/// 3. 可以飘赖子
/// 4. 无大胡，主要以基础胡牌为主
/// 5. 支持特殊配置：癞子玩法、飘癞、锁牌等
/// </summary>
public class XianTaoHuangHuangRule : DefaultMahjongRule
{
    // 仙桃晃晃使用赖子
    public XianTaoHuangHuangRule()
    {
        hasLaiZi = true;
    }

    #region 规则类型实现

    /// <summary>
    /// 获取当前规则类型
    /// </summary>
    public override MahjongRuleType RuleType => MahjongRuleType.XianTaoHuangHuang;

    /// <summary>
    /// 获取麻将玩法类型
    /// </summary>
    public override MJMethod GetMJMethod() => MJMethod.Huanghuang;

    /// <summary>
    /// 获取锁牌阈值
    /// </summary>
    public override int GetLockCardThreshold() => lockCard;

    /// <summary>
    /// 是否是土豪必掷模式
    /// laiPlay == 2 表示土豪必掷，此模式下有赖子不能胡牌
    /// </summary>
    public override bool IsTuHaoBiZhi() => laiPlay == 2;

    #endregion

    // 仙桃晃晃特殊配置
    private int laiPlay = 1;           // 1:1癞到底 2:土豪必掷
    private int piaoLai = 1;           // 1:飘癞有奖 2:飘赖无奖
    private List<int> checks = new List<int>(); // 特殊规则检查
    private int lockCard = 1;          // 锁牌规则
    private int gangKai = 1;           // 杠开规则
    private int zhuo = 1;              // 捉铳规则
    private int openDouble = 0;        // 解锁翻倍
    private int maxTime = 0;           // 封顶倍数

    public override void HandleSpecialConfig(Msg_EnterMJDeskRs msg_EnterMJDeskRs)
    {
        MJConfig mjConfig = msg_EnterMJDeskRs.BaseInfo.MjConfig;
        //需要根据人数调整牌数 只有筒条万 四人是三门 两三人都是两门少万
        mjTotalCount = msg_EnterMJDeskRs.BaseInfo.BaseConfig.PlayerNum == 4 ? 108 : 72;
        if (mjConfig.Xthh != null)
        {
            GF.LogInfo_gsc("仙桃晃晃配置", $"玩法:{mjConfig.Xthh}");
            var xthhConfig = mjConfig.Xthh;

            laiPlay = xthhConfig.LaiPlay;
            piaoLai = xthhConfig.PiaoLai;
            checks = xthhConfig.Check?.ToList() ?? new List<int>();
            lockCard = xthhConfig.LockCard;
            gangKai = xthhConfig.GangKai;
            zhuo = xthhConfig.Zhuo;
            openDouble = xthhConfig.OpenDouble;
            maxTime = xthhConfig.MaxTime;

            GF.LogInfo_gsc("仙桃晃晃配置", $"癞玩法:{laiPlay} 飘癞:{piaoLai} 锁牌:{lockCard} 杠开:{gangKai} 捉铳:{zhuo}");
        }
    }

    public override bool CanHu(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard)
    {
        var allCards = new List<MahjongFaceValue>(handCards);
        if (targetCard != MahjongFaceValue.MJ_UNKNOWN)
        {
            allCards.Add(targetCard);
        }

        // 仙桃晃晃主要检查基础胡牌，不支持大胡
        return CheckBasicHuWithLaiZi(allCards);
    }

    /// <summary>
    /// 带赖子的基础胡牌检查
    /// </summary>
    private bool CheckBasicHuWithLaiZi(List<MahjongFaceValue> cards)
    {
        if (cards.Count != 14) return false;

        // 统计赖子数量
        int laiZiCount = cards.Count(c => baseMahjongGameManager.IsLaiZiCard(c));
        var normalCards = cards.Where(c => !baseMahjongGameManager.IsLaiZiCard(c)).ToList();

        // 使用赖子万能替换检查胡牌
        return CheckHuWithLaiZiRecursive(normalCards, laiZiCount, 0, 0);
    }

    /// <summary>
    /// 带赖子的递归胡牌检查
    /// </summary>
    private bool CheckHuWithLaiZiRecursive(List<MahjongFaceValue> cards, int laiZiCount, int meldCount, int pairCount)
    {
        if (cards.Count == 0 && laiZiCount == 0)
        {
            return meldCount == 4 && pairCount == 1;
        }

        if (cards.Count == 0)
        {
            // 只剩赖子，检查能否组成剩余需要的组合
            int needMelds = 4 - meldCount;
            int needPairs = 1 - pairCount;
            int needCards = needMelds * 3 + needPairs * 2;
            return laiZiCount == needCards;
        }

        var firstCard = cards[0];
        var firstCardCount = cards.Count(c => c == firstCard);

        // 尝试组成对子
        if (pairCount == 0)
        {
            if (firstCardCount >= 2)
            {
                var newCards = new List<MahjongFaceValue>(cards);
                newCards.Remove(firstCard);
                newCards.Remove(firstCard);
                if (CheckHuWithLaiZiRecursive(newCards, laiZiCount, meldCount, 1))
                    return true;
            }
            else if (firstCardCount == 1 && laiZiCount >= 1)
            {
                var newCards = new List<MahjongFaceValue>(cards);
                newCards.Remove(firstCard);
                if (CheckHuWithLaiZiRecursive(newCards, laiZiCount - 1, meldCount, 1))
                    return true;
            }
        }

        // 尝试组成刻子
        if (meldCount < 4)
        {
            if (firstCardCount >= 3)
            {
                var newCards = new List<MahjongFaceValue>(cards);
                newCards.Remove(firstCard);
                newCards.Remove(firstCard);
                newCards.Remove(firstCard);
                if (CheckHuWithLaiZiRecursive(newCards, laiZiCount, meldCount + 1, pairCount))
                    return true;
            }
            else if (firstCardCount == 2 && laiZiCount >= 1)
            {
                var newCards = new List<MahjongFaceValue>(cards);
                newCards.Remove(firstCard);
                newCards.Remove(firstCard);
                if (CheckHuWithLaiZiRecursive(newCards, laiZiCount - 1, meldCount + 1, pairCount))
                    return true;
            }
            else if (firstCardCount == 1 && laiZiCount >= 2)
            {
                var newCards = new List<MahjongFaceValue>(cards);
                newCards.Remove(firstCard);
                if (CheckHuWithLaiZiRecursive(newCards, laiZiCount - 2, meldCount + 1, pairCount))
                    return true;
            }
        }

        // 尝试组成顺子（带赖子）
        if (meldCount < 4 && firstCard < MahjongFaceValue.MJ_FENG_DONG)
        {
            if (TryFormSequenceWithLaiZi(cards, firstCard, laiZiCount, out var remainingCards, out var usedLaiZi))
            {
                if (CheckHuWithLaiZiRecursive(remainingCards, laiZiCount - usedLaiZi, meldCount + 1, pairCount))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 尝试使用赖子组成顺子
    /// </summary>
    private bool TryFormSequenceWithLaiZi(List<MahjongFaceValue> cards, MahjongFaceValue firstCard, 
        int laiZiCount, out List<MahjongFaceValue> remainingCards, out int usedLaiZi)
    {
        remainingCards = new List<MahjongFaceValue>(cards);
        usedLaiZi = 0;

        int suit = (int)firstCard / 9;
        int number = (int)firstCard % 9;

        if (number > 6) return false; // 不能作为顺子的第一张

        var second = (MahjongFaceValue)(suit * 9 + number + 1);
        var third = (MahjongFaceValue)(suit * 9 + number + 2);

        // 移除第一张牌
        remainingCards.Remove(firstCard);

        // 检查第二张牌
        if (remainingCards.Contains(second))
        {
            remainingCards.Remove(second);
        }
        else if (laiZiCount > usedLaiZi)
        {
            usedLaiZi++;
        }
        else
        {
            return false;
        }

        // 检查第三张牌
        if (remainingCards.Contains(third))
        {
            remainingCards.Remove(third);
        }
        else if (laiZiCount > usedLaiZi)
        {
            usedLaiZi++;
        }
        else
        {
            return false;
        }

        return true;
    }

    public override List<MahjongFaceValue> GetTingPaiList(List<MahjongFaceValue> handCards)
    {
        var tingPaiList = new List<MahjongFaceValue>();

        if (handCards.Count != 13) return tingPaiList;

        // 遍历所有可能的牌，检查加上后是否能胡
        for (int i = 0; i < (int)MahjongFaceValue.MJ_UNKNOWN; i++)
        {
            var testCard = (MahjongFaceValue)i;
            if (CanHu(handCards, testCard))
            {
                tingPaiList.Add(testCard);
            }
        }

        return tingPaiList.Distinct().ToList();
    }

    public override bool CanPeng(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard)
    {
        // 仙桃晃晃赖子不能被碰
        if (baseMahjongGameManager.IsLaiZiCard(targetCard)) return false;

        // 检查锁牌规则
        if (IsCardLocked(handCards, targetCard)) return false;

        // 检查手牌中是否有足够的目标牌来碰（至少需要2张）
        int count = handCards.Count(card => card == targetCard || baseMahjongGameManager.IsLaiZiCard(card));
        return count >= 2;
    }

    public override bool CanGang(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard)
    {
        // 普通赖子不能被杠
        if (baseMahjongGameManager.IsLaiZiCard(targetCard)) return false;

        // 检查锁牌规则
        if (IsCardLocked(handCards, targetCard)) return false;

        // 特殊情况:飘赖子(赖根)可以杠,只需要3张
        if (baseMahjongGameManager.IsPiaoLaiZiCard(targetCard))
        {
            int piaoLaiGenCount = handCards.Count(card => baseMahjongGameManager.IsPiaoLaiZiCard(card));
            return piaoLaiGenCount >= 2; // 手上2张赖根 + 摸上1张赖根 = 3张可以杠(朝天杠)
        }
        // 检查手牌中是否有足够的目标牌来杠（至少需要3张）
        int count = handCards.Count(card => card == targetCard || baseMahjongGameManager.IsLaiZiCard(card));
        return count >= 3;
    }

    /// <summary>
    /// 检查是否锁牌
    /// </summary>
    private bool IsCardLocked(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard)
    {
        // 如果没有配置锁牌规则，默认不锁牌
        if (lockCard <= 0) return false;
        
        int laiZiCount = handCards.Count(c => baseMahjongGameManager.IsLaiZiCard(c));
        
        // 根据锁牌规则判断
        switch (lockCard)
        {
            case 2: return laiZiCount >= 2; // 2赖锁牌
            case 3: return laiZiCount >= 3; // 3癞锁牌
            case 4: return laiZiCount >= 4; // 4癞锁牌
            default: return false;
        }
    }

    public override List<List<MahjongFaceValue>> CanChi(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard)
    {
        var result = new List<List<MahjongFaceValue>>();
        
        // 仙桃晃晃支持吃牌，但赖子不能被吃
        if (baseMahjongGameManager.IsLaiZiCard(targetCard)) return result;

        // 检查锁牌规则
        if (IsCardLocked(handCards, targetCard)) return result;

        // 只有万、条、筒可以吃成顺子
        if (targetCard >= MahjongFaceValue.MJ_FENG_DONG) return result;

        int suit = (int)targetCard / 9;
        int number = (int)targetCard % 9;

        // 检查所有可能的吃法
        // 1. ABC（目标牌作为第一张）
        if (number <= 6)
        {
            var second = (MahjongFaceValue)(suit * 9 + number + 1);
            var third = (MahjongFaceValue)(suit * 9 + number + 2);
            if (handCards.Contains(second) && handCards.Contains(third))
            {
                result.Add(new List<MahjongFaceValue> { targetCard, second, third });
            }
        }

        // 2. ABC（目标牌作为第二张）
        if (number >= 1 && number <= 7)
        {
            var first = (MahjongFaceValue)(suit * 9 + number - 1);
            var third = (MahjongFaceValue)(suit * 9 + number + 1);
            if (handCards.Contains(first) && handCards.Contains(third))
            {
                result.Add(new List<MahjongFaceValue> { first, targetCard, third });
            }
        }

        // 3. ABC（目标牌作为第三张）
        if (number >= 2)
        {
            var first = (MahjongFaceValue)(suit * 9 + number - 2);
            var second = (MahjongFaceValue)(suit * 9 + number - 1);
            if (handCards.Contains(first) && handCards.Contains(second))
            {
                result.Add(new List<MahjongFaceValue> { first, second, targetCard });
            }
        }

        return result;
    }

    #region 花色筛选重写方法

    /// <summary>
    /// 仙桃晃晃规则：根据人数决定支持的花色
    /// 4人三门：万、条、筒（无风牌）
    /// 2-3人两门：条、筒（无万、无风牌）  
    /// </summary>
    public override List<MahjongHuaSe> GetValidColors()
    {
        int playerCount = deskBaseInfo?.BaseConfig?.PlayerNum ?? 4;
        
        if (playerCount == 4)
        {
            // 4人三门：万、条、筒
            return new List<MahjongHuaSe> { MahjongHuaSe.WAN, MahjongHuaSe.TONG, MahjongHuaSe.TIAO };
        }
        else
        {
            // 2-3人两门：条、筒（没有万字牌）
            return new List<MahjongHuaSe> { MahjongHuaSe.TONG, MahjongHuaSe.TIAO };
        }
    }

    /// <summary>
    /// 仙桃晃晃规则：获取指定花色的最大牌数
    /// 仙桃晃晃没有风牌和中发白
    /// </summary>
    public override int GetMaxCardCountForColor(MahjongHuaSe color)
    {
        var validColors = GetValidColors();
        if (!validColors.Contains(color))
        {
            return 0; // 不支持的花色
        }

        switch (color)
        {
            case MahjongHuaSe.WAN:
            case MahjongHuaSe.TONG:
            case MahjongHuaSe.TIAO:
                return 9; // 万、条、筒各有9种牌（1-9）
            case MahjongHuaSe.FENG:
                return 0; // 仙桃晃晃没有风牌
            default:
                return 0;
        }
    }

    #endregion
}

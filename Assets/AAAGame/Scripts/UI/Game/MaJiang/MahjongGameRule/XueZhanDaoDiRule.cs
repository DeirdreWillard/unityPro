using System.Collections.Generic;
using NetMsg;
using System.Linq;

/// <summary>
/// 血战到底麻将规则实现（四川麻将）
/// 特点：
/// 1. 108张牌（万条筒），无赖子
/// 2. 游戏开始需要换三张
/// 3. 需要定缺（选择不要的一门花色）
/// 4. 支持多种胡法（大胡、小胡、七对等）
/// 5. 可连续胡牌，胡牌后继续摸牌打牌，直到所有人都胡或牌摸完
/// 6. 与血流成河区别：血战支持小胡和七对，更灵活
/// </summary>
public class XueZhanDaoDiRule : DefaultMahjongRule
{
    // 血战到底特殊配置
    private bool allowContinuousHu = true;    // 允许连续胡牌
    private bool mustFinishAllCards = true;   // 必须摸完所有牌
    private bool supportDaHu = true;         // 支持大胡
    private bool supportXiaoHu = true;       // 支持小胡（普通胡）
    private bool supportQiDui = true;        // 支持七对
    private int huanSanZhang = 1;            // 换三张方式 1:顺时针 2:逆时针 3:对家
    private int dingQueType = 1;             // 定缺类型 1:随机 2:手动
    private List<int> checks = new List<int>(); // 特殊规则复选框
    private int maxTime = 0;                 // 封顶倍数

    public override MahjongRuleType RuleType => MahjongRuleType.XueZhanDaoDi;

    public XueZhanDaoDiRule()
    {
        mjTotalCount = 108; // 只有万条筒，无风牌、字牌
        hasLaiZi = false;   // 血战到底无赖子
    }

    public override void HandleSpecialConfig(Msg_EnterMJDeskRs msg_EnterMJDeskRs)
    {
        MJConfig mjConfig = msg_EnterMJDeskRs.BaseInfo.MjConfig;
        if (mjConfig != null)
        {
            GF.LogInfo_gsc("血战到底配置", $"玩法:{mjConfig.MjMethod}");

            // TODO: 等服务器添加 XZ_Config 后读取配置
            // 临时使用默认配置
            huanSanZhang = 1;    // 顺时针
            dingQueType = 1;     // 随机定缺
            supportXiaoHu = true;
            supportQiDui = true;
            
            GF.LogInfo_gsc("血战到底配置", $"换三张:{huanSanZhang} 定缺:{dingQueType} 小胡:{supportXiaoHu} 七对:{supportQiDui}");
        }
    }

    /// <summary>
    /// 获取换三张方式
    /// </summary>
    public int GetHuanSanZhangMode()
    {
        return huanSanZhang;
    }

    /// <summary>
    /// 获取定缺类型
    /// </summary>
    public int GetDingQueType()
    {
        return dingQueType;
    }

    /// <summary>
    /// 检查是否可以连续胡牌
    /// </summary>
    public bool CanContinuousHu()
    {
        return allowContinuousHu;
    }

    /// <summary>
    /// 检查是否必须摸完所有牌
    /// </summary>
    public bool MustFinishAllCards()
    {
        return mustFinishAllCards;
    }

    public override bool CanHu(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard)
    {
        var allCards = new List<MahjongFaceValue>(handCards);
        if (targetCard != MahjongFaceValue.MJ_UNKNOWN)
        {
            allCards.Add(targetCard);
        }

        // 血战到底支持多种胡牌方式
        return CheckDaHu(allCards) || 
               (supportXiaoHu && CheckBasicHu(allCards)) ||
               (supportQiDui && CheckQiDui(allCards));
    }

    /// <summary>
    /// 检查基础胡牌（小胡）：4组3张+1对
    /// </summary>
    private bool CheckBasicHu(List<MahjongFaceValue> cards)
    {
        if (cards.Count != 14) return false;

        var sortedCards = cards.OrderBy(c => (int)c).ToList();
        return CheckHuRecursive(sortedCards, 0, 0);
    }

    /// <summary>
    /// 递归检查胡牌
    /// </summary>
    private bool CheckHuRecursive(List<MahjongFaceValue> cards, int meldCount, int pairCount)
    {
        if (cards.Count == 0)
        {
            return meldCount == 4 && pairCount == 1;
        }

        var firstCard = cards[0];
        var firstCardCount = cards.Count(c => c == firstCard);

        // 尝试组成对子
        if (pairCount == 0 && firstCardCount >= 2)
        {
            var newCards = new List<MahjongFaceValue>(cards);
            newCards.Remove(firstCard);
            newCards.Remove(firstCard);
            if (CheckHuRecursive(newCards, meldCount, 1))
                return true;
        }

        // 尝试组成刻子
        if (meldCount < 4 && firstCardCount >= 3)
        {
            var newCards = new List<MahjongFaceValue>(cards);
            newCards.Remove(firstCard);
            newCards.Remove(firstCard);
            newCards.Remove(firstCard);
            if (CheckHuRecursive(newCards, meldCount + 1, pairCount))
                return true;
        }

        // 尝试组成顺子（只有万条筒可以组顺子）
        if (meldCount < 4 && firstCard < MahjongFaceValue.MJ_FENG_DONG)
        {
            int suit = (int)firstCard / 9;
            int number = (int)firstCard % 9;
            
            if (number <= 6)
            {
                var second = (MahjongFaceValue)(suit * 9 + number + 1);
                var third = (MahjongFaceValue)(suit * 9 + number + 2);
                
                if (cards.Contains(second) && cards.Contains(third))
                {
                    var newCards = new List<MahjongFaceValue>(cards);
                    newCards.Remove(firstCard);
                    newCards.Remove(second);
                    newCards.Remove(third);
                    if (CheckHuRecursive(newCards, meldCount + 1, pairCount))
                        return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 检查七对
    /// </summary>
    private bool CheckQiDui(List<MahjongFaceValue> cards)
    {
        if (!supportQiDui || cards.Count != 14) return false;

        var groups = cards.GroupBy(c => c).ToList();
        
        // 必须是7个不同的对子
        if (groups.Count != 7) return false;
        
        foreach (var group in groups)
        {
            if (group.Count() != 2) return false;
        }
        
        return true;
    }

    /// <summary>
    /// 检查大胡牌型
    /// </summary>
    private bool CheckDaHu(List<MahjongFaceValue> cards)
    {
        if (!supportDaHu) return false;

        return CheckQingYiSe(cards) ||           // 清一色
               CheckPengPengHu(cards) ||         // 碰碰胡
               CheckJinGouDiao(cards) ||         // 金钩钓
               CheckQuanQiuRen(cards);           // 全求人
    }

    /// <summary>
    /// 检查清一色
    /// </summary>
    private bool CheckQingYiSe(List<MahjongFaceValue> cards)
    {
        // 血流只有万条筒，检查是否全是同一花色
        if (cards.Count == 0) return false;
        
        int firstSuit = (int)cards[0] / 9;
        
        // 所有牌必须是同一花色
        bool allSameSuit = cards.All(c => (int)c / 9 == firstSuit);
        
        return allSameSuit && CheckBasicHu(cards);
    }

    /// <summary>
    /// 检查碰碰胡
    /// </summary>
    private bool CheckPengPengHu(List<MahjongFaceValue> cards)
    {
        if (cards.Count != 14) return false;

        var cardGroups = cards.GroupBy(c => c).ToList();
        
        int tripletCount = 0;
        int pairCount = 0;

        foreach (var group in cardGroups)
        {
            int count = group.Count();
            if (count == 3 || count == 4)
            {
                tripletCount++;
            }
            else if (count == 2)
            {
                pairCount++;
            }
            else
            {
                return false; // 有单张，不是碰碰胡
            }
        }

        return tripletCount == 4 && pairCount == 1;
    }

    /// <summary>
    /// 检查金钩钓（单吊将牌胡牌）
    /// </summary>
    private bool CheckJinGouDiao(List<MahjongFaceValue> cards)
    {
        // 金钩钓：手牌（含目标牌）共2张，且为对子
        // 此时说明其他12张牌都已经碰或杠出去了
        if (cards.Count == 2 && cards[0] == cards[1])
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 检查全求人（全部是吃碰的牌，只剩一张胡牌）
    /// </summary>
    private bool CheckQuanQiuRen(List<MahjongFaceValue> cards)
    {
        // 全求人牌型上等同于金钩钓，区别在于最后一张是点炮
        // 由于此处只检查牌型，复用金钩钓逻辑
        return CheckJinGouDiao(cards);
    }

    public override List<MahjongFaceValue> GetTingPaiList(List<MahjongFaceValue> handCards)
    {
        var tingPaiList = new List<MahjongFaceValue>();

        if (handCards.Count != 13) return tingPaiList;

        // 遍历所有可能的牌，检查能否胡
        for (int i = 0; i < (int)MahjongFaceValue.MJ_FENG_DONG; i++) // 只检查万条筒
        {
            var testCard = (MahjongFaceValue)i;
            if (CanHu(handCards, testCard))
            {
                tingPaiList.Add(testCard);
            }
        }

        return tingPaiList.Distinct().ToList();
    }

    public override List<List<MahjongFaceValue>> CanChi(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard)
    {
        // 血战到底不支持吃牌
        return new List<List<MahjongFaceValue>>();
    }
}

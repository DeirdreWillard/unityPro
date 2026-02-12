using System.Collections.Generic;
using NetMsg;
using System.Linq;

/// <summary>
/// 血流成河麻将规则实现（四川麻将）
/// 特点：
/// 1. 108张牌（万条筒），无赖子
/// 2. 游戏开始需要换三张
/// 3. 需要定缺（选择不要的一门花色）
/// 4. 可连续胡牌，必须打到最后
/// 5. 胡牌类型：基础胡、清一色、碰碰胡、清一色+碰碰胡
/// </summary>
public class XueLiuChengHeRule : DefaultMahjongRule
{
    // 血流成河特殊配置
    private bool allowContinuousHu = true;    // 允许连续胡牌
    private bool mustFinishAllCards = true;   // 必须摸完所有牌
    private int fan = 1;                     // 番数配置 1:2,4,6 2:3,6,9 3:4,8,12
    private int piao = 2;                    // 飘配置 0:自由飘 1~5:定漂1~5 6:不飘
    private int changeCard = 0;              // 换三张配置 0:换三张 1:不换牌
    private List<int> checks = new List<int>(); // 特殊规则复选框

    public XueLiuChengHeRule()
    {
        mjTotalCount = 108; // 只有万条筒，无风牌、字牌
        hasLaiZi = false;   // 血流成河无赖子
    }

    #region 规则类型实现

    /// <summary>
    /// 获取当前规则类型
    /// </summary>
    public override MahjongRuleType RuleType => MahjongRuleType.XueLiuChengHe;

    /// <summary>
    /// 获取麻将玩法类型
    /// </summary>
    public override MJMethod GetMJMethod() => MJMethod.Xl;

    /// <summary>
    /// 是否支持换三张
    /// </summary>
    public override bool HasChangeCard => changeCard == 0;

    /// <summary>
    /// 是否支持甩牌（Check.Contains(3)表示开启甩牌）
    /// </summary>
    public override bool HasThrowCard => checks.Contains(3);

    /// <summary>
    /// 是否支持飘分
    /// </summary>
    public override bool HasPiao => piao != 6;

    /// <summary>
    /// 获取飘分配置模式
    /// </summary>
    public override int GetPiaoMode() => piao;

    /// <summary>
    /// 获取固定飘分值（-1表示需要玩家选择）
    /// piao: 0自由飘 1~5定漂1~5 6不飘
    /// </summary>
    public override int GetFixedPiaoValue()
    {
        if (piao >= 1 && piao <= 5)
            return piao;  // 定漂1~5
        if (piao == 6)
            return 0;     // 不飘
        return -1;        // 自由飘，需要玩家选择
    }

    /// <summary>
    /// 是否需要显示飘分选择面板
    /// </summary>
    public override bool NeedShowPiaoPanel() => piao == 0;

    /// <summary>
    /// 血流飘分选项数量（0~5共6档）
    /// </summary>
    public override int GetPiaoOptionCount() => 6;

    #endregion

    public override void HandleSpecialConfig(Msg_EnterMJDeskRs msg_EnterMJDeskRs)
    {
        MJConfig mjConfig = msg_EnterMJDeskRs.BaseInfo.MjConfig;
        if (mjConfig != null)
        {
            GF.LogInfo_gsc("血流成河配置", $"玩法:{mjConfig.MjMethod}");

            // 读取 XL_Config 配置
            var xlConfig = mjConfig.XlConfig;
            if (xlConfig != null)
            {
                fan = xlConfig.Fan;
                piao = xlConfig.Piao;
                changeCard = xlConfig.ChangeCard;
                checks = new List<int>(xlConfig.Check);
                
                GF.LogInfo_gsc("血流成河配置", $"番数:{fan} 飘:{piao} 换三张:{changeCard} 特殊规则:[{string.Join(",", checks)}]");
            }
            else
            {
                GF.LogWarning("血流成河配置为空，使用默认配置");
            }
        }
    }

    /// <summary>
    /// 获取番数配置
    /// </summary>
    public int GetFan()
    {
        return fan;
    }

    /// <summary>
    /// 获取飘配置
    /// </summary>
    public int GetPiao()
    {
        return piao;
    }

    /// <summary>
    /// 获取换三张配置
    /// </summary>
    public int GetChangeCardMode()
    {
        return changeCard;
    }

    /// <summary>
    /// 检查是否启用特殊规则
    /// </summary>
    public bool HasCheck(int checkId)
    {
        return checks.Contains(checkId);
    }

    /// <summary>
    /// 根据番数配置和胡牌类型计算番数
    /// fan配置: 1:2,4,6 2:3,6,9 3:4,8,12
    /// 胡牌类型: 屁胡=基础番, 清一色/蹦蹦胡=中番, 清一色+蹦蹦胡=大番
    /// </summary>
    /// <param name="huType">胡牌类型：0-屁胡，1-清一色或蹦蹦胡，2-清一色+蹦蹦胡</param>
    /// <returns>番数</returns>
    public int CalculateFanScore(int huType)
    {
        // 根据fan配置获取对应的番数数组
        int[] fanValues;
        switch (fan)
        {
            case 1:
                fanValues = new int[] { 2, 4, 6 }; // 屁胡2, 清一色/蹦蹦胡4, 清一色+蹦蹦胡6
                break;
            case 2:
                fanValues = new int[] { 3, 6, 9 }; // 屁胡3, 清一色/蹦蹦胡6, 清一色+蹦蹦胡9
                break;
            case 3:
                fanValues = new int[] { 4, 8, 12 }; // 屁胡4, 清一色/蹦蹦胡8, 清一色+蹦蹦胡12
                break;
            default:
                fanValues = new int[] { 2, 4, 6 }; // 默认使用配置1
                break;
        }

        // 限制huType在有效范围内
        huType = System.Math.Max(0, System.Math.Min(huType, 2));
        return fanValues[huType];
    }

    /// <summary>
    /// 判断胡牌类型（用于番数计算）
    /// </summary>
    /// <param name="handCards">手牌列表（包含胡的牌）</param>
    /// <returns>0-屁胡，1-清一色或蹦蹦胡，2-清一色+蹦蹦胡</returns>
    public int GetHuType(List<MahjongFaceValue> handCards)
    {
        bool isQingYiSe = CheckQingYiSe(handCards);
        bool isBengBengHu = CheckPengPengHu(handCards);

        if (isQingYiSe && isBengBengHu)
        {
            return 2; // 清一色+蹦蹦胡
        }
        else if (isQingYiSe || isBengBengHu)
        {
            return 1; // 清一色或蹦蹦胡
        }
        else
        {
            return 0; // 屁胡
        }
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

        // 血流成河胡牌检查：基础胡牌（包含清一色、碰碰胡、清一色+碰碰胡都是基于基础胡牌）
        // 基础胡牌 = 4面子 + 1对（面子可以是顺子或刻子）
        return CheckBasicHu(allCards);
    }

    /// <summary>
    /// 根据手牌计算番数（综合方法，用于听牌提示显示）
    /// </summary>
    /// <param name="handCards">手牌列表（包含胡的牌）</param>
    /// <returns>番数</returns>
    public int CalculateFanByHandCards(List<MahjongFaceValue> handCards)
    {
        int huType = GetHuType(handCards);
        return CalculateFanScore(huType);
    }

    /// <summary>
    /// 检查基础胡牌（4组3张+1对）
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

        // 尝试组成顺子
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
    /// 检查清一色（只用于番数计算，不检查胡牌条件）
    /// 清一色：所有牌都是同一花色（万/条/筒）
    /// </summary>
    private bool CheckQingYiSe(List<MahjongFaceValue> cards)
    {
        if (cards.Count == 0) return false;
        
        // 血流成河只有万条筒，检查所有牌是否同一花色
        var wanCards  = cards.Where(c => c >= MahjongFaceValue.MJ_WANG_1 && c <= MahjongFaceValue.MJ_WANG_9).ToList();
        var tiaoCards = cards.Where(c => c >= MahjongFaceValue.MJ_TIAO_1 && c <= MahjongFaceValue.MJ_TIAO_9).ToList();
        var tongCards = cards.Where(c => c >= MahjongFaceValue.MJ_TONG_1 && c <= MahjongFaceValue.MJ_TONG_9).ToList();

        // 检查是否所有牌都是同一花色
        return wanCards.Count == cards.Count || 
               tiaoCards.Count == cards.Count || 
               tongCards.Count == cards.Count;
    }

    /// <summary>
    /// 检查碰碰胡（只用于番数计算，不检查胡牌条件）
    /// 碰碰胡：4个刻子 + 1个对子，没有顺子
    /// </summary>
    private bool CheckPengPengHu(List<MahjongFaceValue> cards)
    {
        if (cards.Count == 0) return false;

        var cardGroups = cards.GroupBy(c => c).ToList();
        
        int tripletCount = 0;
        int pairCount = 0;

        foreach (var group in cardGroups)
        {
            int count = group.Count();
            if (count == 3)
            {
                tripletCount++;
            }
            else if (count == 4)
            {
                tripletCount++; // 杠算一个刻子
            }
            else if (count == 2)
            {
                pairCount++;
            }
            else if (count == 1)
            {
                return false; // 有单张，不是碰碰胡
            }
        }

        // 碰碰胡：4个刻子 + 1个对子
        return tripletCount >= 4 && pairCount == 1;
    }

    public override List<MahjongFaceValue> GetTingPaiList(List<MahjongFaceValue> handCards)
    {
        var tingPaiList = new List<MahjongFaceValue>();

        if (handCards.Count != 13) return tingPaiList;

        // 检查所有可能的听牌
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

    public override List<List<MahjongFaceValue>> CanChi(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard)
    {
        // 血流成河不支持吃牌
        return new List<List<MahjongFaceValue>>();
    }
}

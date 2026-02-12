using System.Collections.Generic;
using System.Linq;
using NetMsg;

/// <summary>
/// 大众麻将默认规则实现（作为通用基础玩法：136张，无赖子，基础 4 面子 + 1 将）
/// </summary>
public class DazhongMahjongRule : DefaultMahjongRule
{
    public override MahjongRuleType RuleType =>  MahjongRuleType.DaZhongMahjong;

    /// <summary>
    /// 基础胡牌：4 组刻/顺 + 1 对，将不处理七对 / 大胡 / 赖子等扩展
    /// </summary>
    public override bool CanHu(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard)
    {
        var all = new List<MahjongFaceValue>(handCards);
        if (targetCard != MahjongFaceValue.MJ_UNKNOWN)
            all.Add(targetCard);
        if (all.Count != 14) return false;
        all.Sort((a,b)=>((int)a).CompareTo((int)b));
        return CheckHuBasic(all, 0, 0);
    }

    private bool CheckHuBasic(List<MahjongFaceValue> cards, int meldCount, int pairCount)
    {
        if (cards.Count == 0)
            return meldCount == 4 && pairCount == 1;

        var first = cards[0];
        int sameCount = cards.Count(c => c == first);

        // 尝试将牌
        if (pairCount == 0 && sameCount >= 2)
        {
            var next = new List<MahjongFaceValue>(cards);
            next.Remove(first); next.Remove(first);
            if (CheckHuBasic(next, meldCount, 1)) return true;
        }

        // 刻子
        if (meldCount < 4 && sameCount >= 3)
        {
            var next = new List<MahjongFaceValue>(cards);
            next.Remove(first); next.Remove(first); next.Remove(first);
            if (CheckHuBasic(next, meldCount + 1, pairCount)) return true;
        }

        // 顺子（仅万/条/筒）
        if (meldCount < 4 && first < MahjongFaceValue.MJ_FENG_DONG)
        {
            int suit = (int)first / 9;
            int num = (int)first % 9; // 0~8
            if (num <= 6)
            {
                var second = (MahjongFaceValue)(suit * 9 + num + 1);
                var third = (MahjongFaceValue)(suit * 9 + num + 2);
                if (cards.Contains(second) && cards.Contains(third))
                {
                    var next = new List<MahjongFaceValue>(cards);
                    next.Remove(first);
                    next.Remove(second);
                    next.Remove(third);
                    if (CheckHuBasic(next, meldCount + 1, pairCount)) return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 基础听牌：枚举所有可能的牌加入后能胡
    /// </summary>
    public override List<MahjongFaceValue> GetTingPaiList(List<MahjongFaceValue> handCards)
    {
        var result = new List<MahjongFaceValue>();
        if (handCards.Count != 13) return result;
        for (int i = 0; i < (int)MahjongFaceValue.MJ_UNKNOWN; i++)
        {
            var test = (MahjongFaceValue)i;
            if (CanHu(handCards, test)) result.Add(test);
        }
        return result.Distinct().ToList();
    }

    public override void HandleSpecialConfig(Msg_EnterMJDeskRs msg_EnterMJDeskRs)
    {
        // 大众麻将暂未有特殊配置，预留扩展点
    }
}

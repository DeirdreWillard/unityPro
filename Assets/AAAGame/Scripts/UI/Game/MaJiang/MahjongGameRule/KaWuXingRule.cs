using System.Collections.Generic;
using NetMsg;
using System.Linq;

/// <summary>
/// 卡五星麻将规则实现
/// 特点：
/// 1. 136张牌，无赖子
/// 2. 有大胡，支持七对
/// 3. 支持大三元、小三元、三元七对
/// 4. 杠可当做两对
/// 5. 支持选漂、亮倒牌等特殊规则
/// 6. 特殊的胡牌规则和番数计算
/// </summary>
public class KaWuXingRule : DefaultMahjongRule
{
    // 卡五星特殊配置
    private bool supportDaHu = true;        // 支持大胡
    private bool supportQiDui = true;       // 支持七对
    private bool gangAsTwoPair = true;      // 杠可当做两对
    private int xuanPiao = 0;               // 选漂倍数 (0-3)
    private bool liangDaoPai = false;       // 是否亮倒牌(明牌)
    private List<int> checks = new List<int>(); // 特殊规则复选框
    private int maxTime = 0;                // 封顶倍数
    
    // 卡五星无赖子
    public KaWuXingRule()
    {
        mjTotalCount = 136;
        hasLaiZi = false; // 明确无赖子
    }

    #region 规则类型实现

    /// <summary>
    /// 获取当前规则类型
    /// </summary>
    public override MahjongRuleType RuleType => MahjongRuleType.KaWuXing;

    /// <summary>
    /// 获取麻将玩法类型
    /// </summary>
    public override MJMethod GetMJMethod() => MJMethod.Kwx;

    /// <summary>
    /// 是否支持换三张
    /// </summary>
    public override bool HasChangeCard => checks.Contains(14);

    /// <summary>
    /// 是否支持亮牌
    /// </summary>
    public override bool HasLiangPai => true;

    /// <summary>
    /// 是否支持飘分
    /// </summary>
    public override bool HasPiao => choicePiaoMode != 3;

    /// <summary>
    /// 获取飘分配置模式
    /// </summary>
    public override int GetPiaoMode() => choicePiaoMode;

    /// <summary>
    /// 获取固定飘分值（-1表示需要玩家选择）
    /// choicePiao: 1每局选漂 2首局定漂 3不漂 4固定飘1 5固定飘2 6固定飘3
    /// </summary>
    public override int GetFixedPiaoValue()
    {
        switch (choicePiaoMode)
        {
            case 3: return 0;  // 不漂
            case 4: return 1;  // 固定飘1
            case 5: return 2;  // 固定飘2
            case 6: return 3;  // 固定飘3
            default: return -1; // 需要玩家选择
        }
    }

    /// <summary>
    /// 是否需要显示飘分选择面板
    /// </summary>
    public override bool NeedShowPiaoPanel() => choicePiaoMode == 1 || choicePiaoMode == 2;

    /// <summary>
    /// 卡五星飘分选项数量（只有0,1,2三档）
    /// </summary>
    public override int GetPiaoOptionCount() => 3;

    /// <summary>
    /// 获取选漂模式（从配置读取）
    /// </summary>
    public int GetChoicePiaoMode() => choicePiaoMode;

    #endregion

    // 选漂模式：1=每局选漂 2=首局定漂 3=不漂 4=固定飘1 5=固定飘2 6=固定飘3
    private int choicePiaoMode = 3;

    public override void HandleSpecialConfig(Msg_EnterMJDeskRs msg_EnterMJDeskRs)
    {
        MJConfig mjConfig = msg_EnterMJDeskRs.BaseInfo.MjConfig;
        GF.LogInfo_gsc("卡五星配置", $"玩法:{mjConfig.MjMethod}");

        // 读取卡五星配置
        if (mjConfig.Kwx != null)
        {
            var kwxConfig = mjConfig.Kwx;
            
            // 买马配置: 1=亮倒自摸买马 2=自摸买马 0=不买马
            int buyHorse = kwxConfig.BuyHorse;
            liangDaoPai = (buyHorse == 1); // 亮倒自摸买马时启用亮倒牌
            
            // 封顶倍数: 8/16/32
            maxTime = kwxConfig.TopOut;
            
            // 选漂模式: 1=每局选漂 2=首局定漂 3=不漂 4=固定飘1 5=固定飘2 6=固定飘3
            choicePiaoMode = kwxConfig.ChoicePiao;
            if (choicePiaoMode == 3) xuanPiao = 0;      // 不漂
            else if (choicePiaoMode == 4) xuanPiao = 1; // 固定飘1
            else if (choicePiaoMode == 5) xuanPiao = 2; // 固定飘2
            else if (choicePiaoMode == 6) xuanPiao = 3; // 固定飘3
            
            // 玩法复选框
            checks = kwxConfig.Play?.ToList() ?? new List<int>();
            // 1:杠上花*4 2:卡五星*4 3:碰碰胡*4 4:小三元七对*8 
            // 5:大三元七对*16 6:海底捞/炮*2 7:杠上炮翻番 8:对亮翻番 
            // 9:全频道 10:数坎 11:双规 12:两番起胡 13:过手碰 14:换三张
            
            GF.LogInfo_gsc("卡五星配置", 
                $"买马:{buyHorse} 亮倒牌:{liangDaoPai} 封顶:{maxTime} " +
                $"选漂模式:{choicePiaoMode}(值:{xuanPiao}) 玩法数:{checks.Count}");
        }
        else
        {
            // 使用默认配置
            xuanPiao = 0;
            liangDaoPai = false;
            maxTime = 0;
            choicePiaoMode = 3;
            
            GF.LogInfo_gsc("卡五星配置", "使用默认配置");
        }
    }

    /// <summary>
    /// 获取选漂倍数
    /// </summary>
    public int GetXuanPiao()
    {
        return xuanPiao;
    }

    /// <summary>
    /// 是否亮倒牌(明牌游戏)
    /// </summary>
    public bool IsLiangDaoPai()
    {
        return liangDaoPai;
    }

    public override bool CanHu(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard)
    {
        var allCards = new List<MahjongFaceValue>(handCards);
        if (targetCard != MahjongFaceValue.MJ_UNKNOWN)
        {
            allCards.Add(targetCard);
        }

        // 检查各种胡牌类型（无赖子版本）
        return CheckBasicHu(allCards) || 
               CheckQiDui(allCards) || 
               CheckDaHu(allCards);
    }

    /// <summary>
    /// 基础胡牌：4 面子 + 1 将（无赖子）
    /// </summary>
    private bool CheckBasicHu(List<MahjongFaceValue> cards)
    {
        if (cards.Count != 14) return false;
        var sorted = cards.OrderBy(c => (int)c).ToList();
        return CheckHuRecursive(sorted, 0, 0);
    }

    /// <summary>
    /// 七对（无赖子）：14 张且能凑 7 对或杠当两对
    /// </summary>
    private bool CheckQiDui(List<MahjongFaceValue> cards)
    {
        if (!supportQiDui || cards.Count != 14) return false;
        var groups = cards.GroupBy(c => c).ToList();
        
        int pairCount = 0;
        foreach (var g in groups)
        {
            int count = g.Count();
            if (count == 2)
            {
                pairCount++;
            }
            else if (count == 3)
            {
                // 三张不能成对结构
                return false;
            }
            else if (count == 4)
            {
                // 四张相同的牌（杠），可以当做两对
                if (gangAsTwoPair)
                {
                    pairCount += 2;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        
    return pairCount == 7;
    }

    /// <summary>
    /// 检查大胡牌型
    /// </summary>
    private bool CheckDaHu(List<MahjongFaceValue> cards)
    {
        if (!supportDaHu) return false;

    return CheckDaSanYuan(cards) || 
           CheckXiaoSanYuan(cards) || 
           (supportQiDui && CheckSanYuanQiDui(cards)) ||
           CheckQingYiSe(cards) ||
           CheckPengPengHu(cards) ||
           CheckQuanQiuRen(cards);
    }

    /// <summary>
    /// 检查大三元
    /// </summary>
    private bool CheckDaSanYuan(List<MahjongFaceValue> cards)
    {
        var sanYuan = new[] { MahjongFaceValue.MJ_ZFB_HONGZHONG, MahjongFaceValue.MJ_ZFB_FACAI, MahjongFaceValue.MJ_ZFB_BAIBAN };
        
        foreach (var yuan in sanYuan)
        {
            int count = cards.Count(c => c == yuan);
            if (count < 3) return false;
        }
        return CheckBasicHu(cards);
    }

    /// <summary>
    /// 检查小三元
    /// </summary>
    private bool CheckXiaoSanYuan(List<MahjongFaceValue> cards)
    {
        var sanYuan = new[] { MahjongFaceValue.MJ_ZFB_HONGZHONG, MahjongFaceValue.MJ_ZFB_FACAI, MahjongFaceValue.MJ_ZFB_BAIBAN };
        
        int haveMeldCount = 0;
        int havePairCount = 0;
        
        foreach (var yuan in sanYuan)
        {
            int count = cards.Count(c => c == yuan);
            if (count >= 3) haveMeldCount++;
            else if (count == 2) havePairCount++;
        }
        return haveMeldCount == 2 && havePairCount == 1 && CheckBasicHu(cards);
    }

    /// <summary>
    /// 检查三元七对
    /// </summary>
    private bool CheckSanYuanQiDui(List<MahjongFaceValue> cards)
    {
    if (!CheckQiDui(cards)) return false;
        
        var sanYuan = new[] { MahjongFaceValue.MJ_ZFB_HONGZHONG, MahjongFaceValue.MJ_ZFB_FACAI, MahjongFaceValue.MJ_ZFB_BAIBAN };
        
        int sanYuanPairCount = 0;
        foreach (var yuan in sanYuan)
        {
            int count = cards.Count(c => c == yuan);
            if (count >= 2) sanYuanPairCount++;
        }
        
        return sanYuanPairCount >= 2;
    }

    /// <summary>
    /// 检查清一色
    /// </summary>
    private bool CheckQingYiSe(List<MahjongFaceValue> cards)
    {
    var normalCards = cards.Where(c => c < MahjongFaceValue.MJ_FENG_DONG).ToList();
        
        if (normalCards.Count == 0) return false;
        
        int firstSuit = (int)normalCards[0] / 9;
    return normalCards.All(c => (int)c / 9 == firstSuit) && CheckBasicHu(cards);
    }

    /// <summary>
    /// 检查碰碰胡
    /// </summary>
    private bool CheckPengPengHu(List<MahjongFaceValue> cards)
    {
        // 碰碰胡：4组刻子+1对
        return CheckAllMelds(cards);
    }

    /// <summary>
    /// 检查所有面子均为刻子（碰碰胡）
    /// </summary>
    private bool CheckAllMelds(List<MahjongFaceValue> cards)
    {
        if (cards.Count != 14) return false;
        var groups = cards.GroupBy(c => c).ToList();
        int triplets = 0;
        int pairs = 0;
        foreach (var g in groups)
        {
            int count = g.Count();
            if (count == 2) pairs++;
            else if (count == 3) triplets++;
            else if (count == 4)
            {
                triplets++; // 视作一个刻子（不拆两刻）
                pairs++;    // 并可视作一对（杠拆一对可配置，这里按常规碰碰胡允许）
            }
            else return false;
        }
        return triplets >= 4 && pairs >= 1;
    }

    private bool CheckHuRecursive(List<MahjongFaceValue> cards, int meldCount, int pairCount)
    {
        if (cards.Count == 0)
            return meldCount == 4 && pairCount == 1;

        var first = cards[0];
        int same = cards.Count(c => c == first);

        // 对子
        if (pairCount == 0 && same >= 2)
        {
            var next = new List<MahjongFaceValue>(cards);
            next.Remove(first); next.Remove(first);
            if (CheckHuRecursive(next, meldCount, 1)) return true;
        }
        // 刻子
        if (meldCount < 4 && same >= 3)
        {
            var next = new List<MahjongFaceValue>(cards);
            next.Remove(first); next.Remove(first); next.Remove(first);
            if (CheckHuRecursive(next, meldCount + 1, pairCount)) return true;
        }
        // 顺子
        if (meldCount < 4 && first < MahjongFaceValue.MJ_FENG_DONG)
        {
            int suit = (int)first / 9;
            int num = (int)first % 9;
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
                    if (CheckHuRecursive(next, meldCount + 1, pairCount)) return true;
                }
            }
        }
        return false;
    }

    public override List<MahjongFaceValue> GetTingPaiList(List<MahjongFaceValue> handCards)
    {
        var tingPaiList = new List<MahjongFaceValue>();

        if (handCards.Count != 13) return tingPaiList;

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
        // 卡五星无赖子，使用基础碰牌逻辑
        return handCards.Count(card => card == targetCard) >= 2;
    }

    public override bool CanGang(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard)
    {
        return handCards.Count(card => card == targetCard) >= 3;
    }

    public override List<List<MahjongFaceValue>> CanChi(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard)
    {
        // 卡五星不支持吃牌
        return new List<List<MahjongFaceValue>>();
    }

    /// <summary>
    /// 检查全求人（金钩钓）
    /// </summary>
    private bool CheckQuanQiuRen(List<MahjongFaceValue> cards)
    {
        // 全求人：手牌（含目标牌）共2张，且为对子
        if (cards.Count == 2 && cards[0] == cards[1])
        {
            return true;
        }
        return false;
    }
}

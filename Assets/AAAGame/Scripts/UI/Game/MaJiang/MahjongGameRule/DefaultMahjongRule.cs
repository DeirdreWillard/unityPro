using System.Collections.Generic;
using NetMsg;
using System.Linq;

/// <summary>
/// 麻将规则类型枚举
/// </summary>
public enum MahjongRuleType
{
    Unknown,
    DaZhongMahjong,   // 大众麻将
    XianTaoHuangHuang,  // 仙桃晃晃
    KaWuXing,           // 卡五星
    XueLiuChengHe,       // 血流成河
    XueZhanDaoDi        // 血战到底
}

/// <summary>
/// 麻将规则基类 - 实现通用的麻将逻辑
/// 职责：定义游戏规则和逻辑判断，不存储实时游戏数据
/// </summary>
public abstract class DefaultMahjongRule : IMahjongRule
{
    protected MahjongGameUI mahjongGameUI;

    // 规则配置数据（静态配置，不随游戏进行改变）
    protected int mjTotalCount = 136;           // 该规则的总牌数
    protected bool hasLaiZi = false;            // 该规则是否支持赖子

    // 保存桌子基础信息（规则配置相关）
    protected Desk_BaseInfo deskBaseInfo;

    #region 规则类型统一判断（子类重写）

    /// <summary>
    /// 获取当前规则类型
    /// </summary>
    public abstract MahjongRuleType RuleType { get; }

    /// <summary>
    /// 是否是仙桃晃晃规则
    /// </summary>
    public bool IsXianTaoHuangHuang => RuleType == MahjongRuleType.XianTaoHuangHuang;

    /// <summary>
    /// 是否是卡五星规则
    /// </summary>
    public bool IsKaWuXing => RuleType == MahjongRuleType.KaWuXing;

    /// <summary>
    /// 是否是血流成河规则
    /// </summary>
    public bool IsXueLiuChengHe => RuleType == MahjongRuleType.XueLiuChengHe;

    /// <summary>
    /// 是否支持换三张
    /// </summary>
    public virtual bool HasChangeCard => false;

    /// <summary>
    /// 是否支持甩牌
    /// </summary>
    public virtual bool HasThrowCard => false;

    /// <summary>
    /// 是否支持亮牌
    /// </summary>
    public virtual bool HasLiangPai => false;

    /// <summary>
    /// 是否支持飘分（子类可重写）
    /// </summary>
    public virtual bool HasPiao => false;

    /// <summary>
    /// 获取飘分配置（0=不飘, >0为具体模式）
    /// </summary>
    public virtual int GetPiaoMode() => 0;

    /// <summary>
    /// 获取固定飘分值（-1表示需要玩家选择）
    /// </summary>
    public virtual int GetFixedPiaoValue() => 0;

    /// <summary>
    /// 是否需要显示飘分选择面板
    /// </summary>
    public virtual bool NeedShowPiaoPanel() => false;

    /// <summary>
    /// 获取飘分选项数量（用于UI显示）
    /// </summary>
    public virtual int GetPiaoOptionCount() => 3;

    /// <summary>
    /// 获取锁牌阈值（0表示不锁牌）
    /// </summary>
    public virtual int GetLockCardThreshold() => 0;

    /// <summary>
    /// 是否是土豪必掷模式（仙桃晃晃特有）
    /// 土豪必掷模式下，有赖子不能胡牌
    /// </summary>
    public virtual bool IsTuHaoBiZhi() => false;

    /// <summary>
    /// 获取麻将玩法类型（用于音效选择等）
    /// </summary>
    public virtual MJMethod GetMJMethod() => MJMethod.Huanghuang;

    #endregion

    /// <summary>
    /// 便捷访问 Manager 的属性（避免重复写长链路）
    /// </summary>
    protected BaseMahjongGameManager baseMahjongGameManager => mahjongGameUI?.mj_procedure?.GetCurrentBaseMahjongGameManager();

    public virtual void Initialize()
    {
    }

    public virtual int GetTotalCardCount()
    {
        return mjTotalCount;
    }

    public bool HasLaiZi()
    {
        return hasLaiZi;
    }

    public virtual void SetDeskInfo(Msg_EnterMJDeskRs enterData)
    {
        // 保存桌子基础信息（规则配置）
        deskBaseInfo = enterData.BaseInfo;
        
        GF.LogInfo_gsc("麻将规则基类", $"保存桌子基础信息 - 玩家数:{deskBaseInfo.BaseConfig.PlayerNum}, 桌子类型:{deskBaseInfo.DeskType}");
        
        // 子类可以重写此方法来处理特殊配置
        HandleSpecialConfig(enterData);
    }

    public virtual bool CanPeng(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard)
    {
        // 赖子不能被碰（通过 Manager 获取实时赖子数据）
        if (baseMahjongGameManager.IsLaiZiCard(targetCard)) return false;

        int count = handCards.Count(card => card == targetCard);
        return count >= 2;
    }

    public virtual bool CanGang(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard)
    {
        // 赖子不能被杠（通过 Manager 获取实时赖子数据）
        if (baseMahjongGameManager.IsLaiZiCard(targetCard)) return false;

        int count = handCards.Count(card => card == targetCard);
        return count >= 3;
    }

    public virtual List<List<MahjongFaceValue>> CanChi(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard)
    {
        List<List<MahjongFaceValue>> result = new List<List<MahjongFaceValue>>();
        
        // 只有万、条、筒可以组成顺子
        if (targetCard >= MahjongFaceValue.MJ_FENG_DONG) return result;

        int targetValue = (int)targetCard;
        int suit = targetValue / 9; // 花色（0=万，1=条，2=筒）
        int number = targetValue % 9 + 1; // 点数（1-9）

        // 检查可能的顺子组合
        CheckSequence(handCards, targetCard, suit, number, result);

        return result;
    }

    protected virtual void CheckSequence(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard, 
        int suit, int number, List<List<MahjongFaceValue>> result)
    {
        // 作为中间牌：如 3,4,5 中的 4
        if (number >= 2 && number <= 8)
        {
            var prev = (MahjongFaceValue)(suit * 9 + number - 2);
            var next = (MahjongFaceValue)(suit * 9 + number);
            if (HasCard(handCards, prev) && HasCard(handCards, next))
            {
                result.Add(new List<MahjongFaceValue> { prev, targetCard, next });
            }
        }

        // 作为最小牌：如 1,2,3 中的 1
        if (number <= 7)
        {
            var mid = (MahjongFaceValue)(suit * 9 + number);
            var next = (MahjongFaceValue)(suit * 9 + number + 1);
            if (HasCard(handCards, mid) && HasCard(handCards, next))
            {
                result.Add(new List<MahjongFaceValue> { targetCard, mid, next });
            }
        }

        // 作为最大牌：如 7,8,9 中的 9
        if (number >= 3)
        {
            var prev1 = (MahjongFaceValue)(suit * 9 + number - 3);
            var prev2 = (MahjongFaceValue)(suit * 9 + number - 2);
            if (HasCard(handCards, prev1) && HasCard(handCards, prev2))
            {
                result.Add(new List<MahjongFaceValue> { prev1, prev2, targetCard });
            }
        }
    }

    protected virtual bool HasCard(List<MahjongFaceValue> handCards, MahjongFaceValue card)
    {
        return handCards.Contains(card) || (hasLaiZi && handCards.Any(baseMahjongGameManager.IsLaiZiCard));
    }

    // 抽象方法，子类必须实现
    public abstract bool CanHu(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard);
    public abstract List<MahjongFaceValue> GetTingPaiList(List<MahjongFaceValue> handCards);
    public abstract void HandleSpecialConfig(Msg_EnterMJDeskRs msg_EnterMJDeskRs);

    #region 通用判断方法

    /// <summary>
    /// 判断是否为顺子
    /// </summary>
    protected bool IsSequentialMeld(List<MahjongFaceValue> cards)
    {
        if (cards.Count != 3) return false;

        var sortedCards = cards.OrderBy(c => (int)c).ToList();
        
        // 只有万、条、筒可以组成顺子
        if (sortedCards[0] >= MahjongFaceValue.MJ_FENG_DONG) return false;
        
        // 检查是否为连续的三张牌
        return (int)sortedCards[1] == (int)sortedCards[0] + 1 &&
               (int)sortedCards[2] == (int)sortedCards[1] + 1;
    }

    /// <summary>
    /// 判断是否为刻子
    /// </summary>
    protected bool IsTripletMeld(List<MahjongFaceValue> cards)
    {
        if (cards.Count < 3) return false;
        
        // 检查是否为相同的牌
        var firstCard = cards[0];
        return cards.All(c => c == firstCard);
    }

    /// <summary>
    /// 判断是否为对子
    /// </summary>
    protected bool IsPairMeld(List<MahjongFaceValue> cards)
    {
        if (cards.Count != 2) return false;
        
        return cards[0] == cards[1];
    }

    /// <summary>
    /// 统计某张牌的数量（包括赖子）
    /// </summary>
    protected int CountCard(List<MahjongFaceValue> cards, MahjongFaceValue targetCard)
    {
        int normalCount = cards.Count(c => c == targetCard);
        int laiZiCount = hasLaiZi ? cards.Count(baseMahjongGameManager.IsLaiZiCard) : 0;
        
        // 赖子可以当作任何牌，但不能当作自己
        if (baseMahjongGameManager.IsLaiZiCard(targetCard))
        {
            return normalCount; // 赖子本身的数量
        }
        else
        {
            return normalCount; // 普通牌的数量，赖子需要单独计算
        }
    }

    /// <summary>
    /// 获取可用的赖子数量
    /// </summary>
    protected int GetAvailableLaiZiCount(List<MahjongFaceValue> cards)
    {
        return hasLaiZi ? cards.Count(baseMahjongGameManager.IsLaiZiCard) : 0;
    }

    /// <summary>
    /// 检查是否为万子
    /// </summary>
    protected bool IsWanCard(MahjongFaceValue card)
    {
        return card >= MahjongFaceValue.MJ_WANG_1 && card <= MahjongFaceValue.MJ_WANG_9;
    }

    /// <summary>
    /// 检查是否为条子
    /// </summary>
    protected bool IsTiaoCard(MahjongFaceValue card)
    {
        return card >= MahjongFaceValue.MJ_TIAO_1 && card <= MahjongFaceValue.MJ_TIAO_9;
    }

    /// <summary>
    /// 检查是否为筒子
    /// </summary>
    protected bool IsTongCard(MahjongFaceValue card)
    {
        return card >= MahjongFaceValue.MJ_TONG_1 && card <= MahjongFaceValue.MJ_TONG_9;
    }

    /// <summary>
    /// 检查是否为风牌
    /// </summary>
    protected bool IsFengCard(MahjongFaceValue card)
    {
        return card >= MahjongFaceValue.MJ_FENG_DONG && card <= MahjongFaceValue.MJ_FENG_BEI;
    }

    /// <summary>
    /// 检查是否为箭牌（中发白）
    /// </summary>
    protected bool IsZFBCard(MahjongFaceValue card)
    {
        return card >= MahjongFaceValue.MJ_ZFB_HONGZHONG && card <= MahjongFaceValue.MJ_ZFB_BAIBAN;
    }

    /// <summary>
    /// 检查是否为字牌（风牌+箭牌）
    /// </summary>
    protected bool IsZiCard(MahjongFaceValue card)
    {
        return IsFengCard(card) || IsZFBCard(card);
    }

    /// <summary>
    /// 获取牌的花色（0=万，1=条，2=筒，3=字牌）
    /// </summary>
    protected int GetCardSuit(MahjongFaceValue card)
    {
        if (IsWanCard(card)) return 0;
        if (IsTiaoCard(card)) return 1;
        if (IsTongCard(card)) return 2;
        if (IsZiCard(card)) return 3;
        return -1; // 未知花色
    }

    /// <summary>
    /// 获取牌的点数（1-9，字牌返回特殊值）
    /// </summary>
    protected int GetCardNumber(MahjongFaceValue card)
    {
        if (IsWanCard(card) || IsTiaoCard(card) || IsTongCard(card))
        {
            return ((int)card % 9) + 1;
        }
        return -1; // 字牌没有点数概念
    }

    #endregion

    /// <summary>
    /// 根据牌的数量和牌值判断meld类型
    /// </summary>
    protected MJOption DetermineMeldType(int cardCount, List<MahjongFaceValue> cards)
    {
        if (cards == null || cards.Count < 3)
            return MJOption.Guo;

        switch (cardCount)
        {
            case 3:
                if (IsTripletMeld(cards))
                    return MJOption.Pen;
                else if (IsSequentialMeld(cards))
                    return MJOption.Chi;
                break;
            case 4:
                if (IsTripletMeld(cards))
                    return MJOption.Gang;
                break;
        }

        return MJOption.Guo;
    }

    /// <summary>
    /// 洗牌前设置赖子数据(仅设置数据不显示UI) - 默认实现为空
    /// </summary>
    public virtual void SetupLaiZiBeforeXiPai(NetMsg.PBMJStart gameStartData)
    {
        // 默认不做任何处理，有赖子的规则子类需要重写此方法
    }

    /// <summary>
    /// 获取当前规则支持的花色范围(默认支持所有花色)
    /// </summary>
    public virtual List<MahjongHuaSe> GetValidColors()
    {
        // 默认支持所有花色:万、条、筒、风字牌
        return new List<MahjongHuaSe> 
        { 
            MahjongHuaSe.WAN, 
            MahjongHuaSe.TONG, 
            MahjongHuaSe.TIAO, 
            MahjongHuaSe.FENG 
        };
    }

    /// <summary>
    /// 获取指定花色的最大牌数(默认实现)
    /// </summary>
    public virtual int GetMaxCardCountForColor(MahjongHuaSe color)
    {
        switch (color)
        {
            case MahjongHuaSe.WAN:
            case MahjongHuaSe.TONG:
            case MahjongHuaSe.TIAO:
                return 9; // 万、条、筒各有9种牌
            case MahjongHuaSe.FENG:
                return 7; // 风字牌:4个风牌 + 3个箭牌(中发白)
            default:
                return 0;
        }
    }
}

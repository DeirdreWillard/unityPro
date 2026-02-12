using System;
using System.Collections.Generic;
using System.Linq;
using NetMsg;
using UnityEngine;

public class GameConstants
{
    public enum WidgetType
    {
        Zhuang,
        DownChip,
        Operate,
        CardType
    }

    public static float GetScaleByScreenSize(float scale = 1f){
        // return Screen.height / 1920f * scale;
        return scale;
    }

    /// <summary>
    /// 根据座位索引获取按钮显示坐标
    /// </summary>
    /// <param name="playerGuid"></param>
    /// <param name="type"></param>
    /// <param name="seatIndex"></param>
    /// <returns></returns>
    public static Vector3 GetWidgetPosition(long playerGuid, WidgetType type, Vector3 seatPos, bool IsPlayBack = false)
    {
        Vector3 vector3 =  Vector3.zero;
        if (Util.IsMySelf(playerGuid) && !IsPlayBack)
        {
            if (type == WidgetType.Zhuang) vector3 = new Vector3(210, -310, 0);
            else if (type == WidgetType.DownChip) vector3 = new Vector3(200, 100, 0);
            else if (type == WidgetType.Operate) vector3 = new Vector3(-200, -10, 0);
            else if (type == WidgetType.CardType) vector3 = new Vector3(0, -10, 0);
        }
        else
        {
            if (type == WidgetType.Zhuang)
            {
                vector3 = (seatPos.x < 150 || seatPos.y > 600) ? new Vector3(100, -80, 0) : new Vector3(-100, -80, 0);
            }
            else if (type == WidgetType.DownChip)
            {
                vector3 = (seatPos.x < 150 || seatPos.y > 600) ? new Vector3(150 * GameConstants.GetScaleByScreenSize(), 5, 0) : new Vector3(-150 * GameConstants.GetScaleByScreenSize(), 5, 0);
            }
            else if (type == WidgetType.Operate)
            {
                vector3 = (seatPos.x < 150 || seatPos.y > 600) ? new Vector3(150 * GameConstants.GetScaleByScreenSize(), 60, 0) : new Vector3(-150 * GameConstants.GetScaleByScreenSize(), 60, 0);
            }
            else if (type == WidgetType.CardType)
            {
                vector3 = new Vector3(0, -160, 0);
            }
        }
        return vector3;
    }

    public static string GetOperateImgString(long playerGuid, ZjhOption type, Vector3 seatPos, bool isWin = false, bool IsPlayBack = false)
    {
        string imgPath = "ZJH/Operate/{0}.png";
        string imgName = "";
        if ((Util.IsMySelf(playerGuid) && !IsPlayBack) || seatPos.x > 300)
        {
            switch (type)
            {
                case ZjhOption.Follow:
                    imgName = "genzhu_you_game_jinhua";
                    break;
                case ZjhOption.Double:
                    imgName = "jiazhu_you_game_jinhua";
                    break;
                case ZjhOption.Compare:
                    imgName = isWin ? "bipai_you_game_jinhua" : "bipaishu_you_game_jinhua";
                    break;
                case ZjhOption.Look:
                    imgName = "kanpai_you_game_jinhua";
                    break;
                case ZjhOption.Give:
                    imgName = "qipai_you_game_jinhua";
                    break;
                case ZjhOption.Allin:
                    imgName = "bipai_you_game_jinhua";
                    break;
            }
        }
        else
        {
            switch (type)
            {
                case ZjhOption.Follow:
                    imgName = "genzhu_zuo_game_jinhua";
                    break;
                case ZjhOption.Double:
                    imgName = "jiazhu_zuo_game_jinhua";
                    break;
                case ZjhOption.Compare:
                    imgName = isWin ? "bipai_zuo_game_jinhua" : "bipaishu_zuo_game_jinhua";
                    break;
                case ZjhOption.Look:
                    imgName = "kanpai_zuo_game_jinhua";
                    break;
                case ZjhOption.Give:
                    imgName = "qipai_zuo_game_jinhua";
                    break;
                case ZjhOption.Allin:
                    imgName = "bipai_zuo_game_jinhua";
                    break;
            }
        }
        return string.Format(imgPath, imgName);
    }

    // 播报牌型
    public static void PlayCardSound(ZjhCardType type)
    {
        // 单牌、单牌235、无牌型时不播放声音
        if (type == ZjhCardType.High ||
            type == ZjhCardType.TowThreeFive)
        {
            // 无声音
        }
        else if (type == ZjhCardType.Pair)
        {
            // 对子也不播放声音
            // 无声音
        }
        else if (type == ZjhCardType.Along)
        {
            // 顺子牌型播放对应声音
           Sound.PlayEffect(AudioKeys.ZJH_PK8_JSS_SHUNZI_8KP);
        }
        else if (type == ZjhCardType.GoldFlower)
        {
            // 同花牌型播放对应声音
           Sound.PlayEffect(AudioKeys.ZJH_PK8_JSS_JINHUA_8KP);
        }
        else if (type == ZjhCardType.GoldAlong)
        {
            // 同花顺牌型播放对应声音
           Sound.PlayEffect(AudioKeys.ZJH_PK8_JSS_TONGHUASHUN_8KP);
        }
        else if (type == ZjhCardType.Boom)
        {
            // 三条（豹子）牌型播放对应声音
           Sound.PlayEffect(AudioKeys.ZJH_PK8_JSS_BAOZI_8KP);
        }
    }

    /// <summary>
    /// 获取牌型的名称字符串
    /// </summary>
    /// <param name="type">牌型的类型</param>
    /// <returns>对应的牌型名称</returns>
    public static string GetCardTypeString(ZjhCardType type)
    {
        string imgPath = "ZJH/Paixing/{0}.png";
        string imgName = "";
        switch (type)
        {
            case ZjhCardType.High:
            case ZjhCardType.TowThreeFive:
                imgName = "gaopai_game_jinhua";
                break;
            case ZjhCardType.Pair:
                imgName = "duizi_game_jinhua";
                break;
            case ZjhCardType.Along:
                imgName = "shunzi_game_jinhua";
                break;
            case ZjhCardType.GoldAlong:
                imgName = "tonghuashun_game_jinhua";
                break;
            case ZjhCardType.GoldFlower:
                imgName = "jinhua_game_jinhua";
                break;
            case ZjhCardType.Boom:
                imgName = "baozi_game_jinhua";
                break;
            default:
                imgName = "gaopai_game_jinhua";
                break;
        }
        return string.Format(imgPath, imgName);
    }

    /// <summary>
    /// 根据座位索引获取按钮显示坐标
    /// </summary>
    /// <param name="playerGuid"></param>
    /// <param name="type">1.HandCards 2.CompareCards</param>
    /// <param name="seatIndex"></param>
    /// <returns></returns>
    public static Vector3 GetWidgetPosition(int type, SeatPosType seatPosType)
    {
        Vector3 vector3 =  Vector3.zero;
        if (type == 1)
        {
            float scale = GetScaleByScreenSize();
            switch (seatPosType)
            {
                case SeatPosType.Top:
                    vector3 = new Vector3(0, -180 * scale, 0);
                    break;
                case SeatPosType.Down:
                    vector3 = new Vector3(0, 180 * scale, 0);
                    break;
                case SeatPosType.Left:
                    vector3 = new Vector3(160, -180 * scale, 0);
                    break;
                case SeatPosType.Right:
                    vector3 = new Vector3(-160, -180 * scale, 0);
                    break;
            }
        }
        else if (type == 2)
        {
            vector3 = (seatPosType == SeatPosType.Down) ? new Vector3(0, 250, 0) : new Vector3(0, -250, 0);
        }
        return vector3;
    }
    

    /// <summary>
    /// 获取牌型的名称字符串
    /// </summary>
    /// <param name="type">牌型的类型</param>
    /// <returns>对应的牌型名称</returns>
    public static string GetCardTypeString_bj(ZjhCardType type,int mode = 0)
    {
        string imgName = "";
        switch (type)
        {
            case ZjhCardType.High:
            case ZjhCardType.TowThreeFive:
                imgName = "乌龙";
                break;
            case ZjhCardType.Pair:
                imgName = "对子";
                break;
            case ZjhCardType.Along:
                imgName = "顺子";
                break;
            case ZjhCardType.GoldAlong:
                imgName = "同花顺";
                break;
            case ZjhCardType.GoldFlower:
                imgName = "同花";
                break;
            case ZjhCardType.Boom:
                imgName = "三条";
                break;
            default:
                imgName = "乌龙";
                break;
        }
        switch(mode){
            case 0:
                return imgName;
            case 1:
                string imgPath = "BJ/{0}.png";
                return string.Format(imgPath, imgName);
            default:
                return imgName;
        }
    }

    /// <summary>
    /// 根据结果获取颜色
    /// </summary>
    /// <param name="isWin">是否赢</param>
    /// <returns>颜色数组</returns>
    public static Color32[] GetColorsByResult(bool isWin)
    {
        Color32[] colors = new Color32[2];
        if (isWin)
        {
            colors[0] = new Color32(255, 193, 7, 255);
            colors[1] = new Color32(255, 239, 223, 255);
        }
        else
        {
            colors[0] = new Color32(221, 245, 255, 255);
            colors[1] = new Color32(204, 237, 252, 255);
        }
        return colors;
    }

    public static ZjhCardType GetCardTypeByCards(int[] cards, bool redBlack, out List<CardAvatar> cardsCache)
    {
        // 将整数转为CardAvatar对象便于处理
        List<CardAvatar> cardList = new List<CardAvatar>();
        foreach (int card in cards)
        {
            // GF.LogInfo("card： " + card);
            if (card != 0) // 跳过空牌
            {
                cardList.Add(new CardAvatar(card));
            }
        }

        // foreach (CardAvatar card in cardList)
        // {   
            // GF.LogInfo("Value " + card.Value);
            // GF.LogInfo("Suit " + card.Suit);
        // }
        cardsCache = cardList;

        // 牌数不足3张时无法形成有效牌型
        if (cardList.Count < 3)
        {
            return ZjhCardType.High; // 默认返回高牌
        }
        
        // 按照牌型规则从大到小依次判断
        // 1. 三条（Boom）：3张同种点数的牌
        if (HasBoom(cardList, redBlack, out cardsCache))
        {
            return ZjhCardType.Boom;
        }
        
        // 2. 同花顺（GoldAlong）：同花色的顺子
        List<CardAvatar> tempCardList = new List<CardAvatar>(cardList);
        if (HasStraightFlush(tempCardList, redBlack, out cardsCache))
        {
            return ZjhCardType.GoldAlong;
        }
        
        // 3. 同花（GoldFlower）：相同花色
        tempCardList = new List<CardAvatar>(cardList);
        if (HasFlush(tempCardList, redBlack, out cardsCache))
        {
            return ZjhCardType.GoldFlower;
        }
        
        // 4. 顺子（Along）：连续但不同花色
        tempCardList = new List<CardAvatar>(cardList);
        if (HasStraight(tempCardList, redBlack, out cardsCache))
        {
            return ZjhCardType.Along;
        }
        
        // 5. 对子（Pair）：两张相同点数
        tempCardList = new List<CardAvatar>(cardList);
        if (HasPair(tempCardList, redBlack, out cardsCache))
        {
            return ZjhCardType.Pair;
        }
        
        // 6. 单张/高牌（High）：无其他牌型组合
        return ZjhCardType.High;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cards">最多三张牌</param>
    /// <param name="redBlack"></param>
    /// <param name="cardsCache">鬼牌替换后的牌</param>
    /// <returns></returns>
    public static bool HasBoom(List<CardAvatar> cards, bool redBlack, out List<CardAvatar> cardsCache)
    {
        int kingCount = cards.Count(c => c.IsKing);
        var normalCards = cards.Where(c => !c.IsKing).ToList();
        cardsCache = cards;

        // 统计所有普通牌的点数，按点数降序、花色降序排列
        var valueGroups = normalCards
            .GroupBy(c => c.Value)
            .OrderByDescending(g => g.Key)
            .ToList();

        int targetValue = -1;
        int targetSuit = -1;
        List<CardAvatar> newCards = new List<CardAvatar>();

        // 1. 三张王
        if (kingCount >= 3)
        {
            targetValue = 1; // A
            targetSuit = redBlack ? 2 : 3; // 红黑王配红桃A/黑桃A
            for (int i = 0; i < 3; i++)
            {
                int cardId = GameUtil.GetInstance().CombineColorAndValue(targetSuit, targetValue);
                newCards.Add(new CardAvatar(cardId));
            }
            cardsCache = newCards;
            return true;
        }

        // 2. 两张王+1张普通牌
        if (kingCount == 2 && normalCards.Count == 1)
        {
            var onlyCard = normalCards[0];
            targetValue = onlyCard.Value;
            targetSuit = redBlack ? 2 : 3;
            // 一张普通牌
            newCards.Add(new CardAvatar(GameUtil.GetInstance().CombineColorAndValue(targetSuit, targetValue)));
            // 两张王都配成同点数同花色
            for (int i = 0; i < 2; i++)
            {
                int cardId = GameUtil.GetInstance().CombineColorAndValue(targetSuit, targetValue);
                newCards.Add(new CardAvatar(cardId));
            }
            cardsCache = newCards;
            return true;
        }

        // 3. 有两张及以上相同点数的普通牌，加鬼牌配三条
        if (kingCount >= 1 && valueGroups.Count > 0 && valueGroups[0].Count() >= 2)
        {
            targetValue = valueGroups[0].Key;
            // 取该点数下最大花色
            targetSuit = valueGroups[0].Max(c => c.Suit);
            // 添加两张普通牌
            newCards.AddRange(valueGroups[0].OrderByDescending(c => c.Suit).Take(2));
            // 用鬼牌补一张
            int cardId = GameUtil.GetInstance().CombineColorAndValue(targetSuit, targetValue);
            newCards.Add(new CardAvatar(cardId));
            cardsCache = newCards;
            return true;
        }

        // 4. 有2张鬼牌+1张普通牌（但普通牌数量大于1时，优先最大点数最大花色）
        if (kingCount >= 2 && normalCards.Count >= 1)
        {
            var maxCard = normalCards.OrderByDescending(c => c.Value).ThenByDescending(c => c.Suit).First();
            targetValue = maxCard.Value;
            targetSuit = redBlack ? 2 : 3;
            newCards.Add(new CardAvatar(GameUtil.GetInstance().CombineColorAndValue(targetSuit, targetValue)));
            for (int i = 0; i < 2; i++)
            {
                int cardId = GameUtil.GetInstance().CombineColorAndValue(targetSuit, targetValue);
                newCards.Add(new CardAvatar(cardId));
            }
            cardsCache = newCards;
            return true;
        }

        // 5. 无鬼，三张同点数
        if (kingCount == 0 && valueGroups.Count > 0 && valueGroups[0].Count() >= 3)
        {
            // 取最大点数最大花色的三张
            targetValue = valueGroups[0].Key;
            newCards.AddRange(valueGroups[0].OrderByDescending(c => c.Suit).Take(3));
            cardsCache = newCards;
            return true;
        }

        // 无法组成三条
        cardsCache = cards;
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cards">最多三张牌</param>
    /// <param name="redBlack"></param>
    /// <param name="cardsCache">鬼牌替换后的牌</param>
    /// <returns></returns>
    public static bool HasStraightFlush(List<CardAvatar> cards, bool redBlack, out List<CardAvatar> cardsCache)
    {
        // 创建原始牌的副本
        List<CardAvatar> originalCards = new List<CardAvatar>(cards);
        cardsCache = originalCards;
        
        // 处理所有牌都是鬼牌的情况
        int kingCount = originalCards.Count(c => c.IsKing);
        if (kingCount >= 3)
            return false;
        
        // 分离鬼牌和普通牌
        List<CardAvatar> kings = originalCards.Where(c => c.IsKing).ToList();
        List<CardAvatar> normalCards = originalCards.Where(c => !c.IsKing).ToList();
        
        // 用于存储是否找到同花顺及其花色
        bool foundStraightFlush = false;
        int straightFlushSuit = -1;
        List<int> missingValues = new List<int>();
        
        // 无鬼牌情况
        if (kingCount == 0)
        {
            // 检查是否有同花且是顺子
            var suitGroups = normalCards.GroupBy(c => c.Suit).ToList();
            foreach (var group in suitGroups)
            {
                List<CardAvatar> suitCards = group.ToList();
                if (suitCards.Count >= 3 && IsStraight(suitCards, 0))
                {
                    foundStraightFlush = true;
                    straightFlushSuit = suitCards[0].Suit;
                    break;
                }
            }
            return foundStraightFlush;
        }
        
        // 鬼当红黑模式
        if (redBlack && kings.Count > 0)
        {
            // 统计大小鬼数量
            int bigKingCount = kings.Count(c => c.Value == 15); // 大鬼数量
            int smallKingCount = kings.Count(c => c.Value == 14); // 小鬼数量
            
            // 检查各种花色的牌组是否能形成同花顺
            // 方片牌组(0) - 红色，使用大鬼(15)
            if (CheckSuitStraightFlush(normalCards, bigKingCount, 0))
            {
                foundStraightFlush = true;
                straightFlushSuit = 0;
                missingValues = GetMissingStraightValues(normalCards.Where(c => c.Suit == 0).ToList(), bigKingCount);
            }
            // 红桃牌组(2) - 红色，使用大鬼(15)
            else if (CheckSuitStraightFlush(normalCards, bigKingCount, 2))
            {
                foundStraightFlush = true;
                straightFlushSuit = 2;
                missingValues = GetMissingStraightValues(normalCards.Where(c => c.Suit == 2).ToList(), bigKingCount);
            }
            // 梅花牌组(1) - 黑色，使用小鬼(14)
            else if (CheckSuitStraightFlush(normalCards, smallKingCount, 1))
            {
                foundStraightFlush = true;
                straightFlushSuit = 1;
                missingValues = GetMissingStraightValues(normalCards.Where(c => c.Suit == 1).ToList(), smallKingCount);
            }
            // 黑桃牌组(3) - 黑色，使用小鬼(14)
            else if (CheckSuitStraightFlush(normalCards, smallKingCount, 3))
            {
                foundStraightFlush = true;
                straightFlushSuit = 3;
                missingValues = GetMissingStraightValues(normalCards.Where(c => c.Suit == 3).ToList(), smallKingCount);
            }
            
            // 如果没有找到同花顺，尝试合并红色或黑色牌再检查
            if (!foundStraightFlush)
            {
                // 尝试把所有红色牌当作一种花色处理并检查是否能形成顺子
                List<CardAvatar> redCards = normalCards.Where(c => c.Suit == 0 || c.Suit == 2).ToList();
                if (redCards.Count > 0)
                {
                    // 分别检查方片和红桃
                    for (int suit = 0; suit <= 2; suit += 2)
                    {
                        List<CardAvatar> suitCardsFiltered = redCards.Where(c => c.Suit == suit).ToList();
                        if (suitCardsFiltered.Count > 0 && CheckSuitStraightFlush(suitCardsFiltered, bigKingCount, suit))
                        {
                            foundStraightFlush = true;
                            straightFlushSuit = suit;
                            missingValues = GetMissingStraightValues(suitCardsFiltered, bigKingCount);
                            break;
                        }
                    }
                }
                
                // 尝试把所有黑色牌当作一种花色处理并检查是否能形成顺子
                if (!foundStraightFlush)
                {
                    List<CardAvatar> blackCards = normalCards.Where(c => c.Suit == 1 || c.Suit == 3).ToList();
                    if (blackCards.Count > 0)
                    {
                        // 分别检查梅花和黑桃
                        for (int suit = 1; suit <= 3; suit += 2)
                        {
                            List<CardAvatar> suitCardsFiltered = blackCards.Where(c => c.Suit == suit).ToList();
                            if (suitCardsFiltered.Count > 0 && CheckSuitStraightFlush(suitCardsFiltered, smallKingCount, suit))
                            {
                                foundStraightFlush = true;
                                straightFlushSuit = suit;
                                missingValues = GetMissingStraightValues(suitCardsFiltered, smallKingCount);
                                break;
                            }
                        }
                    }
                }
            }
        }
        // 鬼当任意模式
        else if (kings.Count > 0)
        {
            // 按花色分组
            for (int suit = 0; suit < 4; suit++)
            {
                List<CardAvatar> suitCards = normalCards.Where(c => c.Suit == suit).ToList();
                if (suitCards.Count > 0 && CheckSuitStraightFlush(suitCards, kingCount, suit))
                {
                    foundStraightFlush = true;
                    straightFlushSuit = suit;
                    missingValues = GetMissingStraightValues(suitCards, kingCount);
                    break;
                }
            }
        }
        
        // 如果找到同花顺且有鬼牌，替换鬼牌
        if (foundStraightFlush && kingCount > 0 && straightFlushSuit != -1)
        {
            // 创建替换后的牌列表
            List<CardAvatar> newCards = new List<CardAvatar>();
            
            // 添加所有非鬼牌
            newCards.AddRange(normalCards);
            
            // 添加替换后的鬼牌
            int kingIndex = 0;
            foreach (var card in originalCards)
            {
                if (card.IsKing && kingIndex < missingValues.Count)
                {
                    // 检查该鬼牌在redBlack模式下是否可用于当前花色
                    bool canUseKing = true;
                    if (redBlack)
                    {
                        bool isRedSuit = (straightFlushSuit == 0 || straightFlushSuit == 2);
                        canUseKing = (card.Value == 15 && isRedSuit) || // 大鬼对应红色花色
                                    (card.Value == 14 && !isRedSuit); // 小鬼对应黑色花色
                    }
                    
                    if (canUseKing)
                    {
                        int cardId = GameUtil.GetInstance().CombineColorAndValue(straightFlushSuit, missingValues[kingIndex]);
                        CardAvatar newCard = new CardAvatar(cardId);
                        newCards.Add(newCard);
                        kingIndex++;
                    }
                }
            }
            
            // 更新cardsCache引用
            cardsCache = newCards;
        }
        
        return foundStraightFlush;
    }
    
    // 辅助方法：获取缺失的顺子牌值
    private static List<int> GetMissingStraightValues(List<CardAvatar> cards, int kingCount)
    {
        // 获取现有牌的点数
        List<int> values = cards.Select(c => c.Value).Distinct().OrderBy(v => v).ToList();
        List<int> missingValues = new List<int>();
        
        // 检查是否存在A-K-Q顺子(A当作1或14)
        bool isAKQSequence = (values.Contains(13) && values.Contains(12)) && 
                            (values.Contains(1) || cards.Any(c => c.Value == 1));
        
        if (isAKQSequence)
        {
            // 如果是A-K-Q的特殊顺子，明确指定缺失的牌值
            if (!values.Contains(1)) missingValues.Add(1); // A
            if (!values.Contains(13)) missingValues.Add(13); // K
            if (!values.Contains(12)) missingValues.Add(12); // Q
        }
        else if (values.Count > 0)
        {
            // 寻找最小和最大值之间的缺口
            int min = values.Min();
            int max = values.Max();
            
            // 查找连续范围内缺失的值
            for (int i = min; i <= max + kingCount && missingValues.Count < kingCount; i++)
            {
                if (!values.Contains(i) && i >= 1 && i <= 13)
                {
                    missingValues.Add(i);
                }
            }
            
            // 如果缺失值不足，往前找
            for (int i = min - 1; i >= 1 && missingValues.Count < kingCount; i--)
            {
                missingValues.Add(i);
            }
            
            // 如果还是不足，往后找(不超过13)
            for (int i = max + 1; i <= 13 && missingValues.Count < kingCount; i++)
            {
                missingValues.Add(i);
            }
        }
        else if (kingCount >= 3)
        {
            // 如果没有普通牌，用鬼牌组成A-2-3
            missingValues.Add(1); // A
            missingValues.Add(2); // 2
            missingValues.Add(3); // 3
        }
        
        return missingValues;
    }

    // 辅助方法：检查特定花色的牌是否能形成同花顺
    public static bool CheckSuitStraightFlush(List<CardAvatar> cards, int kingCount, int suit)
    {
        List<CardAvatar> suitCards = cards.Where(c => c.Suit == suit).ToList();
        return suitCards.Count + kingCount >= 3 && IsStraight(suitCards, kingCount);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cards">最多三张牌</param>
    /// <param name="redBlack"></param>
    /// <param name="cardsCache">鬼牌替换后的牌</param>
    /// <returns></returns>
    public static bool HasFlush(List<CardAvatar> cards, bool redBlack, out List<CardAvatar> cardsCache)
    {
        // 创建原始牌的副本
        List<CardAvatar> originalCards = new List<CardAvatar>(cards);
        cardsCache = originalCards;
        
        // 分离鬼牌和普通牌
        int kingCount = originalCards.Count(c => c.IsKing);
        var normalCards = originalCards.Where(c => !c.IsKing).ToList();
        
        // 如果全是鬼牌，返回false，只能判断为三条
        if (kingCount >= 3)
            return false;
        
        // 如果没有鬼牌，检查普通牌是否有三张同花
        if (kingCount == 0)
        {
            // 按花色分组检查是否有三张及以上同一花色的牌
            var suitGroups = normalCards.GroupBy(c => c.Suit).ToList();
            foreach (var group in suitGroups)
            {
                if (group.Count() >= 3)
                    return true;
            }
            return false;
        }
        
        // 用于存储是否找到同花以及对应的花色
        bool foundFlush = false;
        int flushSuit = 0;
        
        // 鬼当红黑模式
        if (redBlack)
        {
            // 分别统计大小鬼
            int bigKingCount = originalCards.Count(c => c.IsKing && c.Value == 15);
            int smallKingCount = originalCards.Count(c => c.IsKing && c.Value == 14);
            
            // 分别检查每种花色
            for (int suit = 0; suit < 4; suit++)
            {
                int suitCount = normalCards.Count(c => c.Suit == suit);
                int availableKings = (suit == 0 || suit == 2) ? bigKingCount : smallKingCount;
                
                if (suitCount + availableKings >= 3)
                {
                    foundFlush = true;
                    flushSuit = suit;
                    break;
                }
            }
        }
        // 鬼当任意模式
        else
        {
            // 检查是否有足够的同花色牌加上鬼牌能组成同花
            var suitGroups = normalCards.GroupBy(c => c.Suit).OrderByDescending(g => g.Count()).ToList();
            if (suitGroups.Count > 0 && suitGroups[0].Count() + kingCount >= 3)
            {
                foundFlush = true;
                flushSuit = suitGroups[0].Key;
            }
        }
        
        // 如果找到同花，替换鬼牌
        if (foundFlush && kingCount > 0)
        {
            // 创建替换后的牌列表
            List<CardAvatar> newCards = new List<CardAvatar>();
            // 添加所有非鬼牌
            newCards.AddRange(normalCards);

            // 优先用最大点数替换鬼牌，允许同花内有重复点数
            List<int> availableValues = new List<int> { 1 }; // A
            for (int v = 13; v >= 2; v--)
            {
                availableValues.Add(v);
            }

            int kingIndex = 0;
            foreach (var card in originalCards)
            {
                if (card.IsKing)
                {
                    // 检查该鬼牌在redBlack模式下是否可用于当前花色
                    bool canUseKing = true;
                    if (redBlack)
                    {
                        canUseKing = (card.Value == 15 && (flushSuit == 0 || flushSuit == 2)) || // 大鬼对应红色花色
                                    (card.Value == 14 && (flushSuit == 1 || flushSuit == 3)); // 小鬼对应黑色花色
                    }

                    if (canUseKing && kingIndex < availableValues.Count)
                    {
                        int cardId = GameUtil.GetInstance().CombineColorAndValue(flushSuit, availableValues[kingIndex]);
                        CardAvatar newCard = new CardAvatar(cardId);
                        newCards.Add(newCard);
                        kingIndex++;
                        // 如果已经有足够的同花牌，退出循环
                        if (newCards.Count(c => c.Suit == flushSuit) >= 3)
                            break;
                    }
                }
            }
            // 更新cardsCache引用
            cardsCache = newCards;
        }
        
        return foundFlush;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cards">最多三张牌</param>
    /// <param name="redBlack"></param>
    /// <param name="cardsCache">鬼牌替换后的牌</param>
    /// <returns></returns>
    public static bool HasStraight(List<CardAvatar> cards, bool redBlack, out List<CardAvatar> cardsCache)
    {
        // 创建原始牌的副本
        List<CardAvatar> originalCards = new List<CardAvatar>(cards);
        cardsCache = originalCards;
        
        // 分离鬼牌和普通牌
        List<CardAvatar> kings = originalCards.Where(c => c.IsKing).ToList();
        List<CardAvatar> normalCards = originalCards.Where(c => !c.IsKing).ToList();
        int kingCount = kings.Count;
        
        // 如果全是鬼牌，返回false，只能判断为三条
        if (kingCount >= 3)
            return false;

        bool hasStraight = IsStraight(normalCards, kingCount);
        
        // 如果有顺子，替换鬼牌
        if (hasStraight && kingCount > 0)
        {
            // 创建替换后的牌列表
            List<CardAvatar> newCards = new List<CardAvatar>();
            
            // 添加所有非鬼牌
            newCards.AddRange(normalCards);
            
            // 对牌值进行排序
            List<int> values = normalCards.Select(c => c.Value).Distinct().OrderBy(v => v).ToList();
            
            // 查找需要填补的牌值
            List<int> missingValues = new List<int>();
            
            // 查找缺失的牌值
            if (values.Count > 0)
            {
                // 检查是否存在A-K-Q顺子(A当作14)
                bool isAKQSequence = values.Contains(13) && values.Contains(12) && values.Contains(1);
                
                if (isAKQSequence)
                {
                    // 如果是A-K-Q的特殊顺子，明确指定缺失的牌值
                    if (!values.Contains(1)) missingValues.Add(1); // A
                    if (!values.Contains(13)) missingValues.Add(13); // K
                    if (!values.Contains(12)) missingValues.Add(12); // Q
                }
                else
                {
                    // 寻找最小和最大值之间的缺口
                    int min = values.Min();
                    int max = values.Max();
                    
                    // 查找连续范围内缺失的值
                    for (int i = min; i <= max + kingCount && missingValues.Count < kingCount; i++)
                    {
                        if (!values.Contains(i) && i >= 1 && i <= 13)
                        {
                            missingValues.Add(i);
                        }
                    }
                    
                    // 如果缺失值不足，往前找
                    for (int i = min - 1; i >= 1 && missingValues.Count < kingCount; i--)
                    {
                        missingValues.Add(i);
                    }
                    
                    // 如果还是不足，往后找(不超过13)
                    for (int i = max + 1; i <= 13 && missingValues.Count < kingCount; i++)
                    {
                        missingValues.Add(i);
                    }
                }
            }
            
            // 添加替换后的鬼牌
            int kingIndex = 0;
            foreach (var card in originalCards)
            {
                if (card.IsKing && kingIndex < missingValues.Count)
                {
                    // 花色根据redBlack规则生成
                    int suitValue = 3; // 默认黑桃
                    if (redBlack)
                    {
                        suitValue = card.Value == 14 ? 3 : 2; // 小王用黑桃，大王用红桃
                    }
                    
                    int cardId = GameUtil.GetInstance().CombineColorAndValue(suitValue, missingValues[kingIndex]);
                    CardAvatar newCard = new CardAvatar(cardId);
                    newCards.Add(newCard);
                    kingIndex++;
                }
            }
            
            // 更新cardsCache引用
            cardsCache = newCards;
        }
        
        return hasStraight;
    }

    public static bool HasPair(List<CardAvatar> cards, bool redBlack, out List<CardAvatar> cardsCache)
    {
        // 创建原始牌的副本
        List<CardAvatar> originalCards = new List<CardAvatar>(cards);
        cardsCache = originalCards;
        
        // 分离王牌和普通牌
        int kingCount = originalCards.Count(c => c.IsKing);
        List<CardAvatar> normalCards = originalCards.Where(c => !c.IsKing).ToList();

        if (kingCount >= 3)
            return false;
        
        // 如果有2张及以上王牌，直接可以组成对子
        if (kingCount >= 2)
            return true;
            
        // 如果有1张王牌，只需要有1张普通牌即可组成对子
        if (kingCount == 1 && normalCards.Count >= 1)
        {
            // 选择点数最大的牌与鬼牌组成对子
            var highestCard = normalCards.OrderByDescending(c => c.Value).First();
            
            // 替换鬼牌并赋值给cardsCache
            List<CardAvatar> newCards = new List<CardAvatar>();
            // 先添加所有非鬼牌
            newCards.AddRange(normalCards);
            
            // 然后添加替换后的鬼牌
            foreach (var card in originalCards)
            {
                if (card.IsKing)
                {
                    // 花色根据redBlack规则生成
                    int randomSuit = 3;
                    if(redBlack){
                        randomSuit = card.Value == 14 ? 3 : 2;
                    }else{
                        randomSuit = 3;
                    }
                    int cardId = GameUtil.GetInstance().CombineColorAndValue(randomSuit, highestCard.Value);
                    CardAvatar newCard = new CardAvatar(cardId);
                    newCards.Add(newCard);
                    break; // 只需要一张鬼牌
                }
            }
            
            cardsCache = newCards; // 更新cardsCache引用
            return true;
        }
            
        // 无王牌情况，检查普通牌中是否有对子
        var valueGroups = normalCards.GroupBy(c => c.Value).ToList();
        foreach (var group in valueGroups)
        {
            if (group.Count() >= 2)
                return true;
        }
        
        return false;
    }

    public static bool IsStraight(List<CardAvatar> cards, int kingCount)
    {
        // 全是鬼牌情况，返回false，只能判断为三条
        if (cards.Count == 0 && kingCount >= 3)
            return false;
            
        // 创建一个新列表，处理A作为最小值和最大值的情况
        List<int> values = new List<int>();
        bool hasAce = false;
        
        foreach (var card in cards)
        {
            values.Add(card.Value);
            if (card.Value == 1)
                hasAce = true;
        }
        
        values.Sort();
        
        // 去重，避免重复计算
        List<int> uniqueValues = values.Distinct().ToList();
        
        // 如果没有牌且全是鬼牌，返回false
        if (uniqueValues.Count == 0 && kingCount >= 3)
            return false;
        
        // 特殊检查QKA顺子
        if (hasAce)
        {
            int qkaCount = 0;
            if (uniqueValues.Contains(1)) qkaCount++; // A
            if (uniqueValues.Contains(13)) qkaCount++; // K
            if (uniqueValues.Contains(12)) qkaCount++; // Q
            
            if (qkaCount + kingCount >= 3)
                return true;
        }
            
        // 尝试所有可能的起始位置
        for (int start = 0; start < uniqueValues.Count; start++)
        {
            int gapsNeeded = 0;
            int consecutive = 1;
            int lastValue = uniqueValues[start];
            
            // 计算从这个起始位置开始，需要多少鬼牌来填补缺口
            for (int i = start + 1; i < uniqueValues.Count && consecutive < 3; i++)
            {
                int gap = uniqueValues[i] - lastValue - 1;
                if (gap > 0)
                {
                    gapsNeeded += gap;
                    if (gapsNeeded > kingCount) // 如果缺口太大，无法用现有鬼牌填补
                        break;
                }
                
                lastValue = uniqueValues[i];
                consecutive++;
                
                // 已经找到足够多的连续牌
                if (consecutive + Mathf.Min(gapsNeeded, kingCount) >= 3)
                    return true;
            }
            
            // 如果剩余的鬼牌足够填补缺口，或者已有牌加鬼牌数量可以组成顺子
            int remainingKings = kingCount - gapsNeeded;
            if (gapsNeeded <= kingCount && consecutive + gapsNeeded >= 3)
                return true;
                
            // 如果已有连续牌加剩余鬼牌可以在末尾继续延长顺子
            if (consecutive + remainingKings >= 3)
                return true;
            
            // 如果起始点可以向前扩展形成顺子
            if (uniqueValues[start] > 1 && remainingKings > 0)
            {
                int possibleExtendCount = Mathf.Min(remainingKings, uniqueValues[start] - 1);
                if (consecutive + possibleExtendCount >= 3)
                    return true;
            }
        }
        
        return false;
    }
}

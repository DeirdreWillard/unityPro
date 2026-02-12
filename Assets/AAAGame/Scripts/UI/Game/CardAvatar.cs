using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardAvatar
{
    public int Suit { get; set; }  // 花色 黑桃3 红桃2 梅花1 方片0
    public int Value { get; set; } // 牌值 1-15
    public bool IsKing => Value == 14 || Value == 15;
    
    public CardAvatar(int cardId)
    {
        Suit = GameUtil.GetInstance().GetColor(cardId);
        Value = GameUtil.GetInstance().GetValue(cardId);
    }

    public int GetCardId()
    {
        return GameUtil.GetInstance().CombineColorAndValue(Suit, Value);
    }

    public override string ToString()
    {
        return $"Suit: {Suit}, Value: {Value}";
    }
}

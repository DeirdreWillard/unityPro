using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 卡牌数据类
/// </summary>
[Serializable]
public class PDKCardData : IComparable<PDKCardData>
{
    public PDKConstants.CardSuit suit;
    public PDKConstants.CardValue value;
    
    public PDKCardData(PDKConstants.CardSuit suit, PDKConstants.CardValue value)
    {
        this.suit = suit;
        this.value = value;
    }
    
    /// <summary>
    /// 卡牌唯一ID
    /// </summary>
    public int CardId => (int)suit * 100 + (int)value;
    
    /// <summary>
    /// 卡牌权重(用于排序)
    /// </summary>
    public int Weight => (int)value * 10 + (int)suit;
    
    /// <summary>
    /// 比较方法
    /// </summary>
    public int CompareTo(PDKCardData other)
    {
        if (other == null) return 1;
        
        // 只比较点数，不再判断花色大小
        return value.CompareTo(other.value);
    }
    
    public override string ToString()
    {
        return $"{suit}{value}";
    }
    
    public override bool Equals(object obj)
    {
        if (obj is PDKCardData other)
        {
            return suit == other.suit && value == other.value;
        }
        return false;
    }
    
    public override int GetHashCode()
    {
        return CardId;
    }
}

/// <summary>
/// 出牌数据类
/// </summary>
[Serializable]
public class PDKPlayCardData
{
    public List<PDKCardData> cards;
    public PDKConstants.CardType cardType;
    public int weight;
    public int playerId;
    public float playTime;
    
    public PDKPlayCardData()
    {
        cards = new List<PDKCardData>();
        cardType = PDKConstants.CardType.UNNOWN;
        weight = 0;
        playerId = -1;
       // playTime = Time.time;
    }
    
    public PDKPlayCardData(List<PDKCardData> cards, PDKConstants.CardType type, int weight, int playerId)
    {
        this.cards = new List<PDKCardData>(cards);
        this.cardType = type;
        this.weight = weight;
        this.playerId = playerId;
        this.playTime = Time.time;
    }
    
    /// <summary>
    /// 是否为空出牌(Pass)
    /// </summary>
    public bool IsPass => cards == null || cards.Count == 0;
    
    /// <summary>
    /// 卡牌数量
    /// </summary>
    public int CardCount => cards?.Count ?? 0;
    
    /// <summary>
    /// 获取最小卡牌
    /// </summary>
    public PDKCardData GetMinCard()
    {
        if (cards == null || cards.Count == 0) return null;
        return cards.OrderBy(c => c.value).First();
    }
    
    /// <summary>
    /// 获取最大卡牌
    /// </summary>
    public PDKCardData GetMaxCard()
    {
        if (cards == null || cards.Count == 0) return null;
        return cards.OrderByDescending(c => c.value).First();
    }
    
    public override string ToString()
    {
        if (IsPass) return "Pass";
        return $"{cardType}: [{string.Join(", ", cards)}] Weight: {weight}";
    }
}

/// <summary>
/// 玩家数据类
/// </summary>
[Serializable]
public class PDKPlayerData
{
    public int playerId;
    public string playerName;
    public string avatar;
    public List<PDKCardData> handCards;
    public PDKConstants.PlayerState state;
    public bool isReady;
    public int score;
    public int totalScore;
    public bool isOnline;
    public float lastActionTime;
    public int remainingCardCount; // 剩余牌数，由服务器同步
    
    public PDKPlayerData()
    {
        handCards = new List<PDKCardData>();
        state = PDKConstants.PlayerState.Waiting;
        isReady = false;
        score = 0;
        totalScore = 0;
        isOnline = true;
        lastActionTime = Time.time;
        remainingCardCount = 0;
    }
    
    public PDKPlayerData(int id, string name)
    {
        playerId = id;
        playerName = name;
        handCards = new List<PDKCardData>();
        state = PDKConstants.PlayerState.Waiting;
        isReady = false;
        score = 0;
        totalScore = 0;
        isOnline = true;
        lastActionTime = Time.time;
        remainingCardCount = 0;
    }
    
    /// <summary>
    /// 手牌数量（使用服务器同步的剩余牌数）
    /// </summary>
    public int HandCardCount => remainingCardCount;
    
    /// <summary>
    /// 是否已出完牌
    /// </summary>
    public bool IsFinished => HandCardCount == 0;
    
    /// <summary>
    /// 添加手牌
    /// </summary>
    public void AddCard(PDKCardData card)
    {
        if (card != null)
        {
            handCards.Add(card);
            SortHandCards();
        }
    }
    
    /// <summary>
    /// 添加多张手牌
    /// </summary>
    public void AddCards(List<PDKCardData> cards)
    {
        if (cards != null && cards.Count > 0)
        {
            handCards.AddRange(cards);
            SortHandCards();
        }
    }
    
    /// <summary>
    /// 移除手牌
    /// </summary>
    public bool RemoveCard(PDKCardData card)
    {
        return handCards.Remove(card);
    }
    
    /// <summary>
    /// 移除多张手牌
    /// </summary>
    public void RemoveCards(List<PDKCardData> cards)
    {
        if (cards != null)
        {
            foreach (var card in cards)
            {
                handCards.Remove(card);
            }
        }
    }
    
    /// <summary>
    /// 清空手牌
    /// </summary>
    public void ClearCards()
    {
        handCards.Clear();
    }
    
    /// <summary>
    /// 手牌排序
    /// </summary>
    public void SortHandCards()
    {
        handCards.Sort();
    }
    
    /// <summary>
    /// 检查是否有指定卡牌
    /// </summary>
    public bool HasCard(PDKCardData card)
    {
        return handCards.Contains(card);
    }
    
    /// <summary>
    /// 检查是否有黑桃3
    /// </summary>
    public bool HasBlackThree()
    {
        return handCards.Any(c => c.suit == PDKConstants.CardSuit.Spades && c.value == PDKConstants.CardValue.Three);
    }
    
    /// <summary>
    /// 获取最小牌
    /// </summary>
    public PDKCardData GetMinCard()
    {
        if (handCards.Count == 0) return null;
        return handCards.OrderBy(c => c.value).ThenBy(c => c.suit).First();
    }
    
    /// <summary>
    /// 更新剩余牌数（由服务器同步）
    /// </summary>
    public void UpdateRemainingCardCount(int count)
    {
        remainingCardCount = count;
    }
    
    public override string ToString()
    {
        return $"Player[{playerId}] {playerName}: {HandCardCount} cards, State: {state}";
    }
}

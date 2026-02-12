using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// PDK卡牌管理器
/// =============================================
/// 功能：牌堆创建、洗牌、发牌、手牌管理
/// 
/// 模块结构：
/// ├── 1. 属性 (PlayerHands, RemainingCards)
/// ├── 2. 生命周期 (Awake, Initialize)
/// ├── 3. 牌堆管理 (ShuffleDeck, DealCards)
/// ├── 4. 手牌查询 (GetPlayerHand, GetMinCard)
/// └── 5. 工具方法 (SortCards, FindBlackThree)
/// 
/// 最后更新：2025-12-12
/// </summary>
public class PDKCardManager : MonoBehaviour
{
    
    // 事件
    public event Action<List<PDKCardData>> OnDeckCreated;
    public event Action<int, List<PDKCardData>> OnCardsDealt;
    public event Action OnDealComplete;
    
    // 私有变量
    private List<PDKCardData> originalDeck;
    private List<PDKCardData> shuffledDeck;
    private List<List<PDKCardData>> playerHands;
    private PDKConstants.PlayerCount playerCount;
    
    #region 属性
    
    /// <summary>
    /// 玩家手牌
    /// </summary>
    public List<List<PDKCardData>> PlayerHands => playerHands;
    
    /// <summary>
    /// 剩余牌数
    /// </summary>
    public int RemainingCards => shuffledDeck?.Count ?? 0;
    
    #endregion
    
    #region Unity生命周期
    
    void Awake()
    {
        Initialize();
    }
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化
    /// </summary>
    private void Initialize()
    {
        if (originalDeck == null)
            originalDeck = new List<PDKCardData>();
        if (shuffledDeck == null)
            shuffledDeck = new List<PDKCardData>();
        if (playerHands == null)
            playerHands = new List<List<PDKCardData>>();
            
        Debug.Log("PDKCardManager初始化完成");
    }
    
    /// <summary>
    /// 重新初始化（清空所有数据）
    /// </summary>
    public void ReInitialize()
    {
        originalDeck?.Clear();
        shuffledDeck?.Clear();
        playerHands?.Clear();
        
        Initialize();
        
        Debug.Log("PDKCardManager已重新初始化");
    }
    
    #endregion
    
    #region 牌堆管理
    
    
    
    /// <summary>
    /// 洗牌
    /// </summary>
    public void ShuffleDeck()
    {
        shuffledDeck = new List<PDKCardData>(originalDeck);
        
        // Fisher-Yates洗牌算法
        for (int i = shuffledDeck.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            var temp = shuffledDeck[i];
            shuffledDeck[i] = shuffledDeck[randomIndex];
            shuffledDeck[randomIndex] = temp;
        }
        
        Debug.Log("洗牌完成");
    }
    
    /// <summary>
    /// 发牌
    /// </summary>
    public void DealCards(PDKConstants.PlayerCount playerCount)
    {
        this.playerCount = playerCount;
        
        // 计算每人手牌数
        int cardsPerPlayer = playerCount == PDKConstants.PlayerCount.Two ? 
            PDKConstants.CARDS_PER_PLAYER_TWO : PDKConstants.CARDS_PER_PLAYER_THREE;
        
        int totalPlayers = (int)playerCount;
        
        // 初始化玩家手牌
        playerHands.Clear();
        for (int i = 0; i < totalPlayers; i++)
        {
            playerHands.Add(new List<PDKCardData>());
        }
        
        // 发牌
        int cardIndex = 0;
        for (int cardNum = 0; cardNum < cardsPerPlayer; cardNum++)
        {
            for (int player = 0; player < totalPlayers; player++)
            {
                if (cardIndex < shuffledDeck.Count)
                {
                    playerHands[player].Add(shuffledDeck[cardIndex]);
                    cardIndex++;
                }
            }
        }
        
        // 排序每个玩家的手牌
        for (int i = 0; i < totalPlayers; i++)
        {
            playerHands[i].Sort();
            OnCardsDealt?.Invoke(i, new List<PDKCardData>(playerHands[i]));
        }
        
        // 移除已发的牌
        shuffledDeck.RemoveRange(0, cardIndex);
        
        OnDealComplete?.Invoke();
        
        Debug.Log($"发牌完成，{totalPlayers}人局，每人{cardsPerPlayer}张牌，剩余{shuffledDeck.Count}张牌");
    }
    
    #endregion
    
    #region 手牌操作
    
    /// <summary>
    /// 获取玩家手牌
    /// </summary>
    public List<PDKCardData> GetPlayerHand(int playerId)
    {
        if (playerId >= 0 && playerId < playerHands.Count)
            return new List<PDKCardData>(playerHands[playerId]);
        
        return new List<PDKCardData>();
    }
    
    /// <summary>
    /// 添加手牌
    /// </summary>
    public void AddCardsToPlayer(int playerId, List<PDKCardData> cards)
    {
        if (playerId >= 0 && playerId < playerHands.Count && cards != null)
        {
            playerHands[playerId].AddRange(cards);
            playerHands[playerId].Sort();
        }
    }
    
    /// <summary>
    /// 移除玩家手牌
    /// </summary>
    public bool RemoveCardsFromPlayer(int playerId, List<PDKCardData> cards)
    {
        if (playerId < 0 || playerId >= playerHands.Count || cards == null)
            return false;
        
        var playerHand = playerHands[playerId];
        
        // 检查是否都有这些牌
        foreach (var card in cards)
        {
            if (!playerHand.Contains(card))
            {
                Debug.LogWarning($"玩家{playerId}没有卡牌{card}");
                return false;
            }
        }
        
        // 移除卡牌
        foreach (var card in cards)
        {
            playerHand.Remove(card);
        }
        
        return true;
    }
    
    /// <summary>
    /// 检查玩家是否有指定卡牌
    /// </summary>
    public bool PlayerHasCards(int playerId, List<PDKCardData> cards)
    {
        if (playerId < 0 || playerId >= playerHands.Count || cards == null)
            return false;
        
        var playerHand = playerHands[playerId];
        return cards.All(card => playerHand.Contains(card));
    }
    
    /// <summary>
    /// 获取玩家手牌数量
    /// </summary>
    public int GetPlayerHandCount(int playerId)
    {
        if (playerId >= 0 && playerId < playerHands.Count)
            return playerHands[playerId].Count;
        
        return 0;
    }
    
    #endregion
    
    #region 特殊检查
    
    /// <summary>
    /// 查找有黑桃3的玩家
    /// </summary>
    public int FindPlayerWithBlackThree()
    {
        for (int i = 0; i < playerHands.Count; i++)
        {
            if (playerHands[i].Any(card => 
                card.suit == PDKConstants.CardSuit.Spades && 
                card.value == PDKConstants.CardValue.Three))
            {
                return i;
            }
        }
        return -1;
    }
    
    /// <summary>
    /// 查找有最小牌的玩家
    /// </summary>
    public int FindPlayerWithMinCard()
    {
        PDKCardData minCard = null;
        int playerWithMinCard = -1;
        
        for (int i = 0; i < playerHands.Count; i++)
        {
            if (playerHands[i].Count > 0)
            {
                var playerMinCard = playerHands[i].OrderBy(c => c.value).ThenBy(c => c.suit).First();
                
                if (minCard == null || playerMinCard.CompareTo(minCard) < 0)
                {
                    minCard = playerMinCard;
                    playerWithMinCard = i;
                }
            }
        }
        
        return playerWithMinCard;
    }
    
    /// <summary>
    /// 检查游戏是否结束
    /// </summary>
    public bool IsGameFinished()
    {
        return playerHands.Any(hand => hand.Count == 0);
    }
    
    /// <summary>
    /// 获取获胜玩家ID
    /// </summary>
    public int GetWinnerPlayerId()
    {
        for (int i = 0; i < playerHands.Count; i++)
        {
            if (playerHands[i].Count == 0)
                return i;
        }
        return -1;
    }
    
    #endregion
    
    #region 重置和清理
    
    /// <summary>
    /// 重置游戏
    /// </summary>
    public void ResetGame()
    {
        originalDeck.Clear();
        shuffledDeck.Clear();
        playerHands.Clear();
        
        Debug.Log("游戏重置完成");
    }
    
    /// <summary>
    /// 清理资源
    /// </summary>
    void OnDestroy()
    {
        ResetGame();
    }
    
    #endregion
    
    #region 调试功能
    
    /// <summary>
    /// 打印玩家手牌(调试用)
    /// </summary>
    [ContextMenu("打印所有玩家手牌")]
    public void DebugPrintAllHands()
    {
        for (int i = 0; i < playerHands.Count; i++)
        {
            Debug.Log($"玩家{i}手牌({playerHands[i].Count}张): {string.Join(", ", playerHands[i])}");
        }
    }
 
    #endregion

    
    /// <summary>
    /// 打乱列表顺序（Fisher-Yates算法）
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
    
    /// <summary>
    /// 对卡牌进行排序（跑得快规则：从右到左，大到小）
    /// </summary>
    /// <param name="cards">卡牌编号列表</param>
    /// <returns>排序后的卡牌列表</returns>
    public List<int> SortCards(List<int> cards)
    {
        if (cards == null || cards.Count == 0)
            return new List<int>();
        
        List<int> sortedCards = new List<int>(cards);
        
        sortedCards.Sort((card1, card2) =>
        {
            int value1 = GameUtil.GetInstance().GetValue(card1);
            int value2 = GameUtil.GetInstance().GetValue(card2);
            int color1 = GameUtil.GetInstance().GetColor(card1);
            int color2 = GameUtil.GetInstance().GetColor(card2);
            
            // 转换为排序权重
            int sortValue1 = GetCardSortValue(value1);
            int sortValue2 = GetCardSortValue(value2);
            
            // 先按点数排序
            if (sortValue1 != sortValue2)
            {
                return sortValue1.CompareTo(sortValue2);
            }
            
            // 点数相同时按花色排序：黑桃 > 红桃 > 梅花 > 方片
            int colorWeight1 = GetSuitWeight(color1);
            int colorWeight2 = GetSuitWeight(color2);
            
            return colorWeight1.CompareTo(colorWeight2);
        });
        
        // 反转结果：右边最大，左边最小
        sortedCards.Reverse();
        
       
        
        return sortedCards;
    }
    
    /// <summary>
    /// 获取卡牌排序权重（使用PDKConstants配置）
    /// </summary>
    /// <param name="value">卡牌点数</param>
    /// <returns>排序权重</returns>
    private int GetCardSortValue(int value)
    {
        return PDKConstants.VALUE_ORDER_WEIGHTS.TryGetValue(value, out int weight) ? weight : 0;
    }
    
    /// <summary>
    /// 获取花色权重（使用PDKConstants配置）
    /// </summary>
    /// <param name="color">花色编号</param>
    /// <returns>花色权重</returns>
    private int GetSuitWeight(int color)
    {
        if (color >= 0 && color < PDKConstants.SUIT_ORDER_WEIGHTS.Length)
        {
            return PDKConstants.SUIT_ORDER_WEIGHTS[color];
        }
        return 999; // 无效花色排到最后
    }
    
    
  
}
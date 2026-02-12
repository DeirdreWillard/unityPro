using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PDK卡牌辅助工具
/// =============================================
/// 功能：通用的卡牌转换、创建和处理方法
/// 
/// 模块结构：
/// ├── 1. 初始化 (Initialize, Cleanup)
/// ├── 2. 卡牌转换 (ConvertToCardData, ConvertCardIdsToCardDataList)
/// ├── 3. 卡牌创建 (CreateCardGameObject, CreateCards)
/// ├── 4. 卡牌设置 (SetupCardComponent, SetupCardTransform)
/// └── 5. 布局计算 (CalculateCardSpacing)
/// 
/// 【非静态实例】避免静态内存占用，需要初始化后使用
/// 
/// 最后更新：2025-12-12
/// </summary>
public class PDKCardHelper : MonoBehaviour
{
    [Header("卡牌资源引用")]
    public GameObject cardPrefab; // 卡牌预制体
    
    [Header("对象池设置")]
    public int initialPoolSize = 54; // 初始对象池大小
    
    private PDKCardPool cardPool; // 卡牌对象池
    
    #region 初始化
    
    /// <summary>
    /// 初始化卡牌辅助工具
    /// </summary>
    public void Initialize(GameObject prefab, Transform poolParent)
    {
        cardPrefab = prefab;
        
        // 初始化对象池
        if (cardPool == null)
        {
            GameObject poolObj = new GameObject("PDKCardPool");
            poolObj.transform.SetParent(poolParent != null ? poolParent : transform);
            cardPool = poolObj.AddComponent<PDKCardPool>();
            cardPool.Initialize(cardPrefab, poolObj.transform);
        }
        
        // GF.LogInfo_wl("[PDKCardHelper] 初始化完成");
    }
    
    /// <summary>
    /// 清理资源
    /// </summary>
    public void Cleanup()
    {
        if (cardPool != null)
        {
            cardPool.Clear();
        }
        // GF.LogInfo_wl("[PDKCardHelper] 清理完成");
    }
    
    #endregion
    
    #region 卡牌转换方法
    
    /// <summary>
    /// 将 PDKCard 组件转换为 PDKCardData
    /// </summary>
    public PDKCardData ConvertToCardData(PDKCard card)
    {
        if (card == null) return null;
        int cardId = card.numD;
        return ConvertCardIdToCardData(cardId);
    }

    /// <summary>
    /// 将卡牌ID转换为 PDKCardData
    /// </summary>
    public PDKCardData ConvertCardIdToCardData(int cardId)
    {
        int color = GameUtil.GetInstance().GetColor(cardId);
        int value = GameUtil.GetInstance().GetValue(cardId);

        PDKConstants.CardSuit suit = (PDKConstants.CardSuit)(color + 1);

        PDKConstants.CardValue cardValue;
        if (value == 1) // A
        {
            cardValue = PDKConstants.CardValue.Ace; // 14
        }
        else if (value == 2) // 2
        {
            cardValue = PDKConstants.CardValue.Two; // 15
        }
        else
        {
            cardValue = (PDKConstants.CardValue)value;
        }

        return new PDKCardData(suit, cardValue);
    }

    /// <summary>
    /// 将 PDKCard 列表转换为 PDKCardData 列表
    /// </summary>
    public List<PDKCardData> ConvertToCardDataList(List<PDKCard> cards)
    {
        if (cards == null) return new List<PDKCardData>();

        return cards
            .Select(ConvertToCardData)
            .Where(data => data != null)
            .ToList();
    }

    /// <summary>
    /// 将卡牌ID列表转换为 PDKCardData 列表
    /// </summary>
    public List<PDKCardData> ConvertCardIdsToCardDataList(List<int> cardIds)
    {
        if (cardIds == null) return new List<PDKCardData>();

        return cardIds
            .Select(ConvertCardIdToCardData)
            .Where(data => data != null)
            .ToList();
    }
    
    #endregion
    
    #region 卡牌创建方法
    
    /// <summary>
    /// 创建卡牌游戏对象（使用对象池）
    /// </summary>
    public GameObject CreateCardGameObject(int cardId, Transform parent)
    {
        if (cardPool == null)
        {
            GF.LogError("[PDKCardHelper] 对象池未初始化");
            return null;
        }
        
        return cardPool.GetCard(parent, cardId);
    }
    
    /// <summary>
    /// 批量创建卡牌
    /// </summary>
    public List<GameObject> CreateCards(List<int> cardIds, Transform parent)
    {
        List<GameObject> cards = new List<GameObject>();
        
        for (int i = 0; i < cardIds.Count; i++)
        {
            GameObject cardObj = CreateCardGameObject(cardIds[i], parent);
            if (cardObj != null)
            {
                cards.Add(cardObj);
            }
        }
        
        return cards;
    }
    
    /// <summary>
    /// 设置卡牌组件
    /// </summary>
    public void SetupCardComponent(GameObject cardObj, int cardNum)
    {
        if (cardObj == null) return;
        
        cardObj.name = $"Card_{cardNum}";

        PDKCard cardComponent = cardObj.GetComponent<PDKCard>();
        if (cardComponent == null)
        {
            cardComponent = cardObj.AddComponent<PDKCard>();
        }

        if (cardComponent.image == null)
        {
            cardComponent.image = cardObj.GetComponent<Image>();
            if (cardComponent.image == null)
            {
                GF.LogWarning($"[PDKCardHelper] 卡牌{cardNum}缺少Image组件");
            }
        }

        cardComponent.Init(cardNum);

        if (cardComponent.numD != cardNum)
        {
            GF.LogError($"[PDKCardHelper] 卡牌{cardNum}初始化失败，numD={cardComponent.numD}");
            cardComponent.numD = cardNum;
            cardComponent.value = GameUtil.GetInstance().GetValue(cardNum);
        }
    }
    
    /// <summary>
    /// 设置卡牌变换（位置和大小）
    /// </summary>
    public void SetupCardTransform(GameObject cardObj, int index, int totalCards, float spacing = 30f, float scale = 0.5f)
    {
        if (cardObj == null) return;
        
        RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = cardObj.AddComponent<RectTransform>();
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(120f, 160f);
        
        float startX = -(totalCards - 1) * spacing * 0.5f;
        rectTransform.anchoredPosition = new Vector2(startX + index * spacing, 0);
        rectTransform.localScale = Vector3.one * scale;
        rectTransform.SetAsLastSibling();
    }
    
    /// <summary>
    /// 回收卡牌到对象池
    /// </summary>
    public void RecycleCard(GameObject cardObj)
    {
        if (cardPool != null && cardObj != null)
        {
            cardPool.RecycleCard(cardObj);
        }
    }
    
    /// <summary>
    /// 批量回收卡牌
    /// </summary>
    public void RecycleCards(List<GameObject> cards)
    {
        if (cardPool != null && cards != null)
        {
            cardPool.RecycleCards(cards);
        }
    }
    
    /// <summary>
    /// 回收父物体下的所有卡牌
    /// </summary>
    public void RecycleAllCardsInParent(Transform parent)
    {
        if (cardPool != null && parent != null)
        {
            cardPool.RecycleAllCardsInParent(parent);
        }
    }
    
    #endregion
    
    #region 卡牌排序和筛选
    
    /// <summary>
    /// 对卡牌进行排序
    /// </summary>
    public List<int> SortCards(List<int> cardIds, bool ascending = true)
    {
        if (cardIds == null || cardIds.Count == 0) return cardIds;
        
        var sorted = cardIds.OrderBy(id => {
            int value = GameUtil.GetInstance().GetValue(id);
            int color = GameUtil.GetInstance().GetColor(id);
            
            // 跑的快排序：3最小，2最大，A次大
            int sortValue;
            if (value == 2) sortValue = 15;
            else if (value == 1) sortValue = 14;
            else sortValue = value;
            
            return sortValue * 10 + color;
        });
        
        return ascending ? sorted.ToList() : sorted.Reverse().ToList();
    }
    
    /// <summary>
    /// 按花色分组
    /// </summary>
    public Dictionary<PDKConstants.CardSuit, List<int>> GroupBySuit(List<int> cardIds)
    {
        var groups = new Dictionary<PDKConstants.CardSuit, List<int>>();
        
        foreach (int cardId in cardIds)
        {
            var cardData = ConvertCardIdToCardData(cardId);
            if (!groups.ContainsKey(cardData.suit))
            {
                groups[cardData.suit] = new List<int>();
            }
            groups[cardData.suit].Add(cardId);
        }
        
        return groups;
    }
    
    /// <summary>
    /// 按点数分组
    /// </summary>
    public Dictionary<PDKConstants.CardValue, List<int>> GroupByValue(List<int> cardIds)
    {
        var groups = new Dictionary<PDKConstants.CardValue, List<int>>();
        
        foreach (int cardId in cardIds)
        {
            var cardData = ConvertCardIdToCardData(cardId);
            if (!groups.ContainsKey(cardData.value))
            {
                groups[cardData.value] = new List<int>();
            }
            groups[cardData.value].Add(cardId);
        }
        
        return groups;
    }
    
    #endregion
}

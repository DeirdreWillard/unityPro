using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PDK卡牌对象池
/// =============================================
/// 功能：卡牌GameObject的创建、回收和复用，提高性能
/// 
/// 模块结构：
/// ├── 1. 对象池初始化 (Initialize, PrewarmPool)
/// ├── 2. 卡牌获取 (GetCard, CreateNewCard)
/// ├── 3. 卡牌回收 (RecycleCard, RecycleAllCards)
/// └── 4. 工具方法 (ResetCard, GetPoolStats)
/// 
/// 最后更新：2025-12-12
/// </summary>
public class PDKCardPool : MonoBehaviour
{
    private GameObject cardPrefab;
    private Transform poolRoot;
    
    // 空闲卡牌池（可复用的卡牌）
    private Queue<GameObject> availableCards = new Queue<GameObject>();
    
    // 正在使用的卡牌集合（用于追踪）
    private HashSet<GameObject> activeCards = new HashSet<GameObject>();
    
    // 预热数量（初始创建的卡牌数量）
    private const int PREWARM_COUNT = 54; // 一副牌54张（含大小王）
    
    /// <summary>
    /// 初始化对象池
    /// </summary>
    /// <param name="prefab">卡牌预制体</param>
    /// <param name="parent">对象池根节点（可选）</param>
    public void Initialize(GameObject prefab, Transform parent = null)
    {
        if (prefab == null)
        {
            GF.LogError("[PDKCardPool] 卡牌预制体为空，无法初始化对象池");
            return;
        }
        
        cardPrefab = prefab;
        
        // 创建对象池根节点
        if (poolRoot == null)
        {
            GameObject poolObj = new GameObject("PDKCardPool");
            poolObj.transform.SetParent(parent ?? this.transform);
            poolRoot = poolObj.transform;
            poolRoot.gameObject.SetActive(false); // 隐藏对象池
        }
        
        // 预热对象池
        PrewarmPool(PREWARM_COUNT);
        
        // GF.LogInfo_wl($"[PDKCardPool] 对象池初始化完成，预热 {PREWARM_COUNT} 张卡牌");
    }
    
    /// <summary>
    /// 预热对象池（预先创建一定数量的卡牌）
    /// </summary>
    private void PrewarmPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject card = CreateNewCard();
            card.SetActive(false);
            availableCards.Enqueue(card);
        }
    }
    
    /// <summary>
    /// 创建新卡牌实例
    /// </summary>
    private GameObject CreateNewCard()
    {
        GameObject card = Instantiate(cardPrefab, poolRoot);
        card.name = $"PooledCard_{availableCards.Count + activeCards.Count}";
        return card;
    }
    
    /// <summary>
    /// 从对象池获取卡牌
    /// </summary>
    /// <param name="parent">卡牌的父节点</param>
    /// <param name="cardId">卡牌ID（0-51）</param>
    /// <returns>初始化完成的卡牌GameObject</returns>
    public GameObject GetCard(Transform parent, int cardId)
    {
        // 【安全检查】确保卡牌预制体存在
        if (cardPrefab == null)
        {
            GF.LogError("[PDKCardPool] cardPrefab为null，无法获取卡牌");
            return null;
        }
        
        GameObject card;
        
        // 从空闲池获取或创建新卡牌
        if (availableCards.Count > 0)
        {
            card = availableCards.Dequeue();
        }
        else
        {
            card = CreateNewCard();
            if (card == null)
            {
                GF.LogError("[PDKCardPool] 创建新卡牌失败");
                return null;
            }
            // GF.LogInfo_wl("[PDKCardPool] 对象池不足，动态创建新卡牌");
        }
        
        // 设置父节点
        card.transform.SetParent(parent);
        card.transform.localScale = Vector3.one;
        card.transform.localPosition = Vector3.zero;
        card.SetActive(true);
        
        // 初始化卡牌数据（关键：在激活前完成初始化，避免异步问题）
        PDKCard cardComponent = card.GetComponent<PDKCard>();
        if (cardComponent != null)
        {
            // 强制同步初始化，确保卡牌面显示正确
            cardComponent.Init(cardId);
            
            // 验证初始化结果
            int color = GameUtil.GetInstance().GetColor(cardId);
            int value = GameUtil.GetInstance().GetValue(cardId);
            
            if (enableDebugLog)
            {
                // GF.LogInfo_wl($"[PDKCardPool] 取出卡牌: ID={cardId}, 花色={color}, 点数={value}, 名称={card.name}");
            }
        }
        else
        {
            GF.LogError($"[PDKCardPool] 卡牌缺少PDKCard组件: {card.name}");
        }
        
        // 添加到激活集合
        activeCards.Add(card);
        
        return card;
    }
    
    /// <summary>
    /// 回收单张卡牌到对象池
    /// </summary>
    /// <param name="card">要回收的卡牌</param>
    public void RecycleCard(GameObject card)
    {
        if (card == null) return;
        
        // 从激活集合移除
        activeCards.Remove(card);
        
        // 重置卡牌状态
        ResetCard(card);
        
        // 移动到对象池根节点
        card.transform.SetParent(poolRoot);
        card.SetActive(false);
        
        // 加入空闲池
        availableCards.Enqueue(card);
    }
    
    /// <summary>
    /// 回收多张卡牌
    /// </summary>
    /// <param name="cards">要回收的卡牌列表</param>
    public void RecycleCards(List<GameObject> cards)
    {
        if (cards == null) return;
        
        foreach (GameObject card in cards)
        {
            RecycleCard(card);
        }
    }
    
    /// <summary>
    /// 回收某个父节点下的所有卡牌
    /// </summary>
    /// <param name="parent">父节点</param>
    public void RecycleAllCardsInParent(Transform parent)
    {
        if (parent == null) return;
        
        List<GameObject> cardsToRecycle = new List<GameObject>();
        
        // 收集所有子卡牌
        for (int i = 0; i < parent.childCount; i++)
        {
            GameObject child = parent.GetChild(i).gameObject;
            if (child.GetComponent<PDKCard>() != null)
            {
                cardsToRecycle.Add(child);
            }
        }
        
        // 回收
        RecycleCards(cardsToRecycle);
        
        if (enableDebugLog)
        {
            // GF.LogInfo_wl($"[PDKCardPool] 从 {parent.name} 回收了 {cardsToRecycle.Count} 张卡牌");
        }
    }
    
    /// <summary>
    /// 重置卡牌状态（回收前清理）
    /// </summary>
    private void ResetCard(GameObject card)
    {
        // 重置Transform
        card.transform.localPosition = Vector3.zero;
        card.transform.localRotation = Quaternion.identity;
        card.transform.localScale = Vector3.one;
        
        // 重置选中状态
        PDKCard cardComponent = card.GetComponent<PDKCard>();
        if (cardComponent != null)
        {
            cardComponent.SetSelected(false);
        }
        
        // 【Bug修复1&2】重置卡牌颜色为亮色，防止打出和结算的牌显示为灰色
        Image cardImage = card.GetComponent<Image>();
        if (cardImage != null)
        {
            cardImage.color = Color.white; // 恢复为亮色
        }
        
        // 重置CanvasGroup
        CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        
        // 重置Button状态
        Button button = card.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = true;
        }
    }
    
    /// <summary>
    /// 清空对象池（销毁所有卡牌）
    /// </summary>
    public void Clear()
    {
        // 销毁所有空闲卡牌
        while (availableCards.Count > 0)
        {
            GameObject card = availableCards.Dequeue();
            if (card != null)
            {
                Destroy(card);
            }
        }
        
        // 销毁所有激活的卡牌
        foreach (GameObject card in activeCards)
        {
            if (card != null)
            {
                Destroy(card);
            }
        }
        activeCards.Clear();
        
        // GF.LogInfo_wl("[PDKCardPool] 对象池已清空");
    }
    
    /// <summary>
    /// 获取对象池统计信息
    /// </summary>
    public string GetPoolStats()
    {
        return $"空闲: {availableCards.Count}, 使用中: {activeCards.Count}, 总计: {availableCards.Count + activeCards.Count}";
    }
    
    private bool enableDebugLog = false;
    
    private void OnDestroy()
    {
        Clear();
    }
}

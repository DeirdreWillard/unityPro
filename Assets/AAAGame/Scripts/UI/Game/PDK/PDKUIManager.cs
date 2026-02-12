using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// PDK UI管理器 - 负责卡牌布局和动画
/// </summary>
public class PDKUIManager : MonoBehaviour
{
    #region 公共引用

    [Header("UI组件引用")]
    public GameObject varHandCard;
    public GameObject varCard;

    [Header("调试设置")]
    public bool enableDebugLog = true;

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
        if (enableDebugLog)
        {
            Debug.Log("PDKUIManager初始化完成");
        }
    }

   
    #endregion

    #region 布局计算


    // 静态变量存储计算好的卡牌布局参数
    public static float s_cardWidth = PDKConstants.BASE_CARD_WIDTH;
    public static float s_cardHeight = PDKConstants.BASE_CARD_HEIGHT;
    public static float s_cardSpacing = PDKConstants.BASE_CARD_WIDTH * PDKConstants.IDEAL_SPACING_RATIO;
    public static bool s_isLayoutCalculated = false;

    /// <summary>
    /// 计算卡牌布局参数
    /// </summary>
    /// <param name="cardCount">卡牌数量</param>
    /// <param name="screenWidth">屏幕宽度</param>
    /// <returns>布局参数(卡牌宽度, 卡牌高度, 卡牌间距)</returns>
    public static (float cardWidth, float cardHeight, float cardSpacing) CalculateCardLayout(int cardCount, float screenWidth)
    {
        float availableWidth = screenWidth * PDKConstants.SCREEN_WIDTH_USAGE;
        float baseCardWidth = PDKConstants.BASE_CARD_WIDTH;
        float idealSpacing = baseCardWidth * PDKConstants.IDEAL_SPACING_RATIO;

        // 计算理想情况下的总宽度
        float totalWidthWithIdeal = (cardCount - 1) * idealSpacing + baseCardWidth;

        float cardWidth, cardSpacing, cardHeight;

        if (totalWidthWithIdeal <= availableWidth)
        {
            // 理想间距能放下
            cardWidth = baseCardWidth;
            cardSpacing = idealSpacing;
        }
        else
        {
            // 需要压缩
            cardWidth = baseCardWidth;
            cardSpacing = (availableWidth - baseCardWidth) / (cardCount - 1);

            // 限制最小间距
            float minSpacing = baseCardWidth * PDKConstants.MIN_SPACING_RATIO;
            if (cardSpacing < minSpacing)
            {
                cardSpacing = minSpacing;
                // 如果间距太小，适当缩小卡牌
                cardWidth = (availableWidth - (cardCount - 1) * cardSpacing);
            }
        }

        cardHeight = cardWidth * PDKConstants.CARD_ASPECT_RATIO;

        return (cardWidth, cardHeight, cardSpacing);
    }

    /// <summary>
    /// 计算卡牌位置
    /// </summary>
    /// <param name="index">卡牌索引</param>
    /// <param name="totalCount">总卡牌数</param>
    /// <param name="cardWidth">卡牌宽度</param>
    /// <param name="cardSpacing">卡牌间距</param>
    /// <returns>局部位置</returns>
    public static Vector3 CalculateCardPosition(int index, int totalCount, float cardWidth, float cardSpacing)
    {
        float totalWidth = (totalCount - 1) * cardSpacing + cardWidth;
        float startX = -totalWidth / 2f;
        float localPosX = startX + index * cardSpacing;

        return new Vector3(localPosX, 0, 0);
    }

    #endregion

    #region 动画辅助

   
    /// <summary>
    /// 卡牌选中动画
    /// </summary>
    /// <param name="card">卡牌组件</param>
    /// <param name="isSelect">是否为选中动画</param>
    /// <returns>协程</returns>
    public IEnumerator AnimateCardSelection(PDKCard card, bool isSelect)
    {
        if (card == null || card.gameObject == null) yield break;

        RectTransform rectTransform = card.GetComponent<RectTransform>();
        if (rectTransform == null) yield break;

        Vector3 startPos = rectTransform.localPosition;
        Vector3 targetPos;

        if (isSelect)
        {
            // 选中：保存当前位置为原始位置，然后上移
            card.ResetOriginalPosition();
            targetPos = startPos + new Vector3(0, PDKConstants.CARD_SELECT_OFFSET_Y, 0);
        }
        else
        {
            // 取消选中：回到原始位置
            targetPos = card.GetOriginalPosition();
        }

        float elapsed = 0f;
        float duration = PDKConstants.CARD_SELECT_ANIMATION_DURATION;

        while (elapsed < duration)
        {
            // 每帧检查对象是否还存在
            if (card == null || card.gameObject == null || rectTransform == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 使用平滑曲线
            t = Mathf.SmoothStep(0f, 1f, t);
            rectTransform.localPosition = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        // 确保最终位置准确（再次检查对象是否存在）
        if (card != null && card.gameObject != null && rectTransform != null)
        {
            rectTransform.localPosition = targetPos;
            card.selectionCoroutine = null;
        }
    }
    #endregion

    #region 调试信息

    /// <summary>
    /// 格式化卡牌信息（用于调试）
    /// </summary>
    /// <param name="cardNum">卡牌编号</param>
    /// <returns>格式化的卡牌信息</returns>
    public static string FormatCardInfo(int cardNum)
    {
        int value = GameUtil.GetInstance().GetValue(cardNum);
        int color = GameUtil.GetInstance().GetColor(cardNum);
        string[] suitNames = { "方片", "梅花", "红桃", "黑桃" };
        string[] valueNames = { "", "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

        return $"{suitNames[color]}{valueNames[value]}(#{cardNum})";
    }

    /// <summary>
    /// 输出卡牌列表信息（用于调试）
    /// </summary>
    /// <param name="cards">卡牌列表</param>
    /// <param name="title">标题</param>
    public static void LogCardList(System.Collections.Generic.List<int> cards, string title = "卡牌列表")
    {
        if (cards == null || cards.Count == 0)
        {
            Debug.Log($"{title}: 空");
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"{title}({cards.Count}张):");

        for (int i = 0; i < cards.Count; i++)
        {
            string position = i == 0 ? "最左" : (i == cards.Count - 1 ? "最右" : $"第{i + 1}");
            sb.AppendLine($"  {position}: {FormatCardInfo(cards[i])}");
        }

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// 重新排列手牌 - 居中显示（
    /// </summary>
    public void ArrangeHandCards()
    {
        if (varHandCard == null) return;
        Transform parent = varHandCard.transform;
        int childCount = parent.childCount;
        if (childCount == 0) return;
        // 只在第一次或未初始化时计算布局参数（固定间距）
        if (!s_isLayoutCalculated)
        {
            CalculateLayoutForStandardHand();
        }
        // 计算总宽度和起始位置（相对于父物体varHandCard的局部坐标）
        float totalWidth = (childCount - 1) * s_cardSpacing + s_cardWidth;
        float startX = -totalWidth / 2f; // 相对于父物体居中对齐
        // 重新排列所有手牌（在varHandCard的局部坐标系内）
        for (int i = 0; i < childCount; i++)
        {
            Transform child = parent.GetChild(i);
            RectTransform rectTransform = child.GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                // 计算当前卡牌的局部X位置（相对于varHandCard）
                float localPosX = startX + i * s_cardSpacing;
                Vector3 basePosition = new Vector3(localPosX, 0, 0);
                Vector3 targetLocalPos = basePosition;
                // 首先设置基础位置
                rectTransform.localPosition = basePosition;
                // 获取卡牌组件并设置原始位置
                PDKCard cardComponent = child.GetComponent<PDKCard>();
                if (cardComponent != null)
                { 
                    cardComponent.ResetOriginalPosition();
                    if (cardComponent.IsSelected())
                    {
                        targetLocalPos.y += PDKConstants.CARD_SELECT_OFFSET_Y;
                    }
                }
                // 如果目标位置与当前位置不同，进行平滑移动
                if (Vector3.Distance(rectTransform.localPosition, targetLocalPos) > 0.1f)
                {
                    StartCoroutine(MoveCardSmooth(rectTransform, targetLocalPos, PDKConstants.CARD_ANIMATION_DURATION));
                }
                else
                {
                    rectTransform.localPosition = targetLocalPos; 
                }
                // 设置卡牌层级（后面的卡牌在上层）
                rectTransform.SetSiblingIndex(i);
                // 设置卡牌大小
                rectTransform.sizeDelta = new Vector2(s_cardWidth, s_cardHeight);        
            } 
        }
    }

    /// <summary>
    /// 计算标准16张手牌的布局参数
    /// </summary>
    private void CalculateLayoutForStandardHand()
    {
        CalculateLayoutForCurrentHand(16);
    }
    /// <summary>
    /// 根据当前手牌数量计算布局参数
    /// </summary>
    /// <param name="cardCount">当前手牌数量</param>
    private void CalculateLayoutForCurrentHand(int cardCount)
    {
        if (cardCount <= 0) return;

        // 【修改】固定使用1920*1080分辨率计算，不根据实际屏幕宽度变化
        const float fixedScreenWidth = 1920f;
        float availableWidth = fixedScreenWidth * PDKConstants.SCREEN_WIDTH_USAGE;

        float baseCardWidth = PDKConstants.BASE_CARD_WIDTH;
        float idealSpacing = baseCardWidth * PDKConstants.IDEAL_SPACING_RATIO;

        // 计算当前牌数用理想间距的总宽度
        float totalWidthWithIdeal = (cardCount - 1) * idealSpacing + baseCardWidth;

        if (totalWidthWithIdeal <= availableWidth)
        {
            // 理想间距能放下，使用理想参数
            s_cardWidth = baseCardWidth;
            s_cardSpacing = idealSpacing;
        }
        else
        {
            // 需要压缩，计算实际可用间距
            s_cardWidth = baseCardWidth;
            s_cardSpacing = (availableWidth - baseCardWidth) / (cardCount - 1);

            // 限制最小间距
            float minSpacing = baseCardWidth * PDKConstants.MIN_SPACING_RATIO;
            if (s_cardSpacing < minSpacing)
            {
                s_cardSpacing = minSpacing;
                // 如果间距太小，适当缩小卡牌
                s_cardWidth = (availableWidth - (cardCount - 1) * s_cardSpacing);
            }
        }

        s_cardHeight = s_cardWidth * PDKConstants.CARD_ASPECT_RATIO;
        s_isLayoutCalculated = true;

        // if (enableDebugLog)
        // {
        //     GF.LogInfo_wl($"计算布局参数({cardCount}张): 固定宽度={fixedScreenWidth}, 可用宽度={availableWidth:F0}, " +
        //                  $"卡牌={s_cardWidth:F0}x{s_cardHeight:F0}, 间距={s_cardSpacing:F0}");
        // }
    }

    /// <summary>
    /// 平滑移动卡牌到目标位置
    /// </summary>
    private IEnumerator MoveCardSmooth(RectTransform rectTransform, Vector3 targetPos, float duration)
    {
        if (rectTransform == null) yield break;
        
        Vector3 startPos = rectTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 每帧检查对象是否还存在
            if (rectTransform == null || rectTransform.gameObject == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 使用Lerp平滑插值
            rectTransform.localPosition = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        // 确保最终位置准确（再次检查对象是否存在）
        if (rectTransform != null && rectTransform.gameObject != null)
        {
            rectTransform.localPosition = targetPos;
        }
    }

    /// <summary>
    /// 移除指定卡牌并重新排列剩余手牌
    /// </summary>
    public void RemoveCardAndRearrange(GameObject cardToRemove)
    {
        if (cardToRemove != null)
        {
            DestroyImmediate(cardToRemove);

            // 延迟一帧后重新排列，确保卡牌已被销毁
            StartCoroutine(ArrangeHandCardsNextFrame());
        }
    }

    /// <summary>
    /// 移除多张卡牌并重新排列
    /// </summary>
    public void RemoveCardsAndRearrange(List<GameObject> cardsToRemove)
    {
        if (cardsToRemove != null && cardsToRemove.Count > 0)
        {
            foreach (var card in cardsToRemove)
            {
                if (card != null)
                {
                    DestroyImmediate(card);
                }
            }
            // 延迟一帧后重新排列
            StartCoroutine(ArrangeHandCardsNextFrame());
        }
    }

/// <summary>
    /// 延迟一帧重新排列手牌（确保所有卡牌都已创建）
    /// </summary>
    public IEnumerator ArrangeHandCardsNextFrame()
    {
        yield return null; // 等待一帧   
            ArrangeHandCards();       
    }
    #endregion
}

using System.Collections.Generic;
using Unity.Mathematics;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static UtilityBuiltin;

public class HuPaiTips : MonoBehaviour
{
    public GameObject prefabUiHuPaiDetailTips;
    public Transform huPaiTipsContent;

    public GameObject tips;
    public GameObject huAll;

    RectTransform tipsRectTransform; // tips的RectTransform
    RectTransform scrollViewRectTransform; // ScrollView的RectTransform
    RectTransform viewportRectTransform; // Viewport的RectTransform
    RectTransform contentRectTransform; // Content的RectTransform
    
    Vector2 detailTipsSize;

    GridLayoutGroup gridLayoutGroup;
    ScrollRect scrollRect;

    public float spacingx = 15f;
    public float spacingy = 15f;
    public int maxRowsWithoutScroll = 2; // 超过2行才启用滚动
    public float maxHeightWithoutScroll = 200f; // 不滚动时的最大高度
    public float paddingHorizontal = 20f; // 左右内边距
    public float paddingVertical = 20f; // 上下内边距

    public MahjongFaceValue CurrentLaiZi { get; set; } = MahjongFaceValue.MJ_UNKNOWN;

    public void Init(){
        tipsRectTransform = tips.GetComponent<RectTransform>();
        
        detailTipsSize = prefabUiHuPaiDetailTips.GetComponent<RectTransform>().sizeDelta;
        contentRectTransform = huPaiTipsContent.GetComponent<RectTransform>();
        gridLayoutGroup = huPaiTipsContent.GetComponent<GridLayoutGroup>();
        
        // 获取ScrollView和Viewport
        scrollRect = tips.GetComponentInChildren<ScrollRect>();
        if (scrollRect != null)
        {
            scrollViewRectTransform = scrollRect.GetComponent<RectTransform>();
            viewportRectTransform = scrollRect.viewport;
        }
        
        spacingx = gridLayoutGroup.spacing.x;
        spacingy = gridLayoutGroup.spacing.y;
    }

    /// <summary>
    /// 为手牌设置胡牌麻将提示
    /// </summary>
    /// <param name="paiType"></param>
    /// <param name="huPaiTipsInfos"></param>
    public void SetHuPaiInfo(HuPaiTipsInfo[] huPaiTipsInfos)
    {
        Init();
        RemoveAllDetailTips();

        if (huPaiTipsInfos == null)
            return;

        int colCount = 6; // 固定每行6列
        
        // 边界检查：如果没有听牌信息，直接返回
        if (huPaiTipsInfos == null || huPaiTipsInfos.Length == 0)
        {
            contentRectTransform.sizeDelta = new Vector2(0, 0);
            return;
        }
        
        // 对胡牌提示进行排序：按牌值(faceValue)从小到大排序
        huPaiTipsInfos = huPaiTipsInfos.OrderBy(h => h.faceValue).ToArray();

        // 设置GridLayoutGroup为固定列数模式
        gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        colCount = Mathf.Min(colCount, huPaiTipsInfos.Length);
        colCount = Mathf.Max(1, colCount); // 确保至少为1，避免除零
        gridLayoutGroup.constraintCount = colCount;

        // 计算实际需要的行数和列数
        int actualRows = (huPaiTipsInfos.Length + colCount - 1) / colCount; // 向上取整
        int actualCols = Mathf.Min(colCount, huPaiTipsInfos.Length);

        // 计算Content区域的实际内容高度和宽度（避免负数）
        float contentWidth = actualCols * detailTipsSize.x + Mathf.Max(0, actualCols - 1) * spacingx;
        contentWidth += gridLayoutGroup.padding.left + gridLayoutGroup.padding.right;
        
        float contentHeight = actualRows * detailTipsSize.y + Mathf.Max(0, actualRows - 1) * spacingy;
        contentHeight += gridLayoutGroup.padding.top + gridLayoutGroup.padding.bottom;

        // 设置Content的大小
        contentRectTransform.sizeDelta = new Vector2(contentWidth, contentHeight);

        // 计算包含padding的总宽度和高度
        float totalWidth = contentWidth + paddingHorizontal * 2;
        float totalHeight = contentHeight + paddingVertical * 2;

        // 根据内容数量决定是否启用滚动
        if (scrollRect != null)
        {
            if (actualRows <= maxRowsWithoutScroll)
            {
                // 内容少,不需要滚动
                scrollRect.vertical = false;
                
                // 调整所有层级的大小 (Tips比其他层级宽100)
                if (scrollViewRectTransform != null)
                    scrollViewRectTransform.sizeDelta = new Vector2(totalWidth, totalHeight);
                if (viewportRectTransform != null)
                    viewportRectTransform.sizeDelta = new Vector2(totalWidth, totalHeight);
                tipsRectTransform.sizeDelta = new Vector2(totalWidth + 100f, totalHeight);
            }
            else
            {
                // 内容多,启用滚动,限制外层容器高度
                scrollRect.vertical = true;
                float limitedHeight = maxHeightWithoutScroll;
                
                // 调整所有层级的大小 (Tips比其他层级宽100)
                if (scrollViewRectTransform != null)
                    scrollViewRectTransform.sizeDelta = new Vector2(totalWidth, limitedHeight);
                if (viewportRectTransform != null)
                    viewportRectTransform.sizeDelta = new Vector2(totalWidth, limitedHeight);
                tipsRectTransform.sizeDelta = new Vector2(totalWidth + 100f, limitedHeight);
            }
        }
        else
        {
            // 没有ScrollRect,直接设置tips大小 (Tips比其他层级宽100)
            tipsRectTransform.sizeDelta = new Vector2(totalWidth + 100f, totalHeight);
        }

        // 重置所有位置并应用偏移
        ResetPositions();

        // 添加所有胡牌提示
        for (int i = 0; i < huPaiTipsInfos.Length; i++)
        {
            AddDetailTips(huPaiTipsInfos[i]);
        }
    }

    /// <summary>
    /// 重置所有位置为零,并应用指定偏移
    /// </summary>
    private void ResetPositions()
    {
        // Content位置归零
        if (contentRectTransform != null)
            contentRectTransform.anchoredPosition = Vector2.zero;

        // Viewport位置归零
        if (viewportRectTransform != null)
            viewportRectTransform.anchoredPosition = Vector2.zero;

        // ScrollView位置归零后右移30
        if (scrollViewRectTransform != null)
            scrollViewRectTransform.anchoredPosition = new Vector2(30f, 0f);
    }

    GameObject AddDetailTips(HuPaiTipsInfo huPaiTipsInfo)
    {
        //ui
        GameObject huPaiDetailTips = Object.Instantiate(prefabUiHuPaiDetailTips, huPaiTipsContent);
        huPaiDetailTips.SetActive(true);
        bool isLaizi = CurrentLaiZi != MahjongFaceValue.MJ_UNKNOWN && huPaiTipsInfo.faceValue == CurrentLaiZi;

        MjCard_2D mjCard = huPaiDetailTips.GetComponent<MjCard_2D>();
        mjCard.SetCardValue(huPaiTipsInfo.faceValue, isLaizi);

        // 设置胡牌提示牌的背景（使用座位0的手牌背景）
        MjColor currentColor = MahjongSettings.GetCardStyle();
        var sprite = MahjongResourceManager.GetBackgroundSprite(currentColor, 0, "discard");
        if (sprite != null)
        {
            mjCard.SetCardBgSprite(sprite);
        }

        huPaiDetailTips.transform.Find("Fan").GetComponent<Text>().text = huPaiTipsInfo.fanAmount > 0 ? huPaiTipsInfo.fanAmount.ToString() + "番" : "";
        huPaiDetailTips.transform.Find("CountImage/Count").GetComponent<Text>().text = huPaiTipsInfo.zhangAmount >= 0 ? huPaiTipsInfo.zhangAmount.ToString() + "张" : "";

        return huPaiDetailTips;
    }

    public void RemoveAllDetailTips()
    {
        for (int i = huPaiTipsContent.childCount - 1; i >= 0; i--)
        {
            Destroy(huPaiTipsContent.GetChild(i).gameObject);
        }
    }

    public void Show(HuPaiTipsInfo[] huPaiTipsInfos = null, MahjongFaceValue currentLaiZi = MahjongFaceValue.MJ_UNKNOWN)
    {
        if (huPaiTipsInfos == null)
            return;

        this.CurrentLaiZi = currentLaiZi;
        if (huPaiTipsInfos.Length > 15){
            tips.SetActive(false);
            if(huAll != null)
                huAll.SetActive(true);
        }
        else{
            SetHuPaiInfo(huPaiTipsInfos);
            tips.SetActive(true);
            if(huAll != null)
                huAll.SetActive(false);
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

}


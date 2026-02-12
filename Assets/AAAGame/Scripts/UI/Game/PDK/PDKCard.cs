using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using static UtilityBuiltin;

/// <summary>
/// PDK卡牌组件
/// =============================================
/// 功能：单张卡牌的数据和显示管理
/// 
/// 模块结构：
/// ├── 1. 卡牌数据 (color, value, numD)
/// ├── 2. 初始化 (Init, InitByName)
/// ├── 3. 选中状态 (SetSelected, IsSelected)
/// └── 4. 显示设置 (SetImgColor, EnsureRectTransformSetup)
/// 
/// 最后更新：2025-12-12
/// </summary>
public class PDKCard : MonoBehaviour
{
    #region ==================== 1. 卡牌数据 ====================
    
    public int color = -1;
    public int value = -1;
    public Image image;
    public int numD; // 二进制值判断牌型
    
    #endregion

    #region ==================== 2. 初始化 ====================
    #endregion

    #region ==================== 2. 初始化 ====================
    

    public void Init(int num)
    {
        numD = num;
        string CardPath;
        if (num >= 0)
        {    
            //花色固定4  78 79
            color = GameUtil.GetInstance().GetColor(num);
            value = GameUtil.GetInstance().GetValue(num);
            CardPath = "Common/Cards_max/" + color + value+".png";
            
           
        }
        else if(num == -2) {
            CardPath = "Common/Cards_max/-2.png";
        }else{
            CardPath = "Common/Cards_max/-1.png";
        }
        string s = AssetsPath.GetSpritesPath(CardPath);
        GF.UI.LoadSprite(s, (sprite) =>{
            // 【关键修复】检查对象是否已被销毁，避免MissingReferenceException
            if (this == null || gameObject == null) return;
            
            transform.TryGetComponent<Image>(out image);
            if (image == null)
            {
                // 再次检查gameObject是否有效
                if (gameObject == null) return;
                gameObject.AddComponent<Image>();
                image = gameObject.GetComponent<Image>();
            }
            
            // 检查image组件是否有效
            if (image == null) return;
            image.sprite = sprite;
            
            // 确保RectTransform设置正确
            EnsureRectTransformSetup();
        });
    }

    public void InitByName(string name)
    {
        if (gameObject.activeSelf)
            gameObject.SetActive(true);
        transform.TryGetComponent<Image>(out image);
        if (image == null)
        {
            gameObject.AddComponent<Image>();
            image = gameObject.GetComponent<Image>();
        }
        // image.sprite = CoroutineRunner.Instance.GetCardSprite(name);
            
        string CardPath = "Common/Cards_max/" + name + ".png";
        string s = AssetsPath.GetSpritesPath(CardPath);
        GF.UI.LoadSprite(s, (sprite) =>{
            // 【关键修复】检查对象是否已被销毁
            if (this == null || gameObject == null) return;
            
            transform.TryGetComponent<Image>(out image);
            if (image == null)
            {
                // 再次检查gameObject是否有效
                if (gameObject == null) return;
                gameObject.AddComponent<Image>();
                image = gameObject.GetComponent<Image>();
            }
            
            // 检查image组件是否有效
            if (image == null) return;
            image.sprite = sprite;
        });
    }

    public void SetImgColor(Color color)
    {
        if (image!= null)
            image.color = color;
    }
    
    #endregion

    #region ==================== 3. 选中状态 ====================
    
    [Header("选中状态")]
    public bool isSelected = false;
    public Coroutine selectionCoroutine; // 用于追踪当前正在播放的选中/取消选中动画
    private Vector3 originalPosition;
    private bool positionSaved = false;

    /// <summary>
    /// 设置卡牌选中状态
    /// </summary>
    /// <param name="selected">是否选中</param>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        // 保存原始位置（首次设置时）
        if (!positionSaved)
        {
            originalPosition = transform.localPosition;
            positionSaved = true;
        }
    }
    
    /// <summary>
    /// 获取卡牌是否被选中
    /// </summary>
    /// <returns>是否选中</returns>
    public bool IsSelected()
    {
        return isSelected;
    }
    
    /// <summary>
    /// 获取原始位置
    /// </summary>
    /// <returns>原始位置</returns>
    public Vector3 GetOriginalPosition()
    {
        return originalPosition;
    }
    
    /// <summary>
    /// 重置原始位置
    /// </summary>
    public void ResetOriginalPosition()
    {
        originalPosition = transform.localPosition;
        positionSaved = true;
    }
    
    #endregion

    #region ==================== 4. 显示设置 ====================
    
    /// <summary>
    /// 确保RectTransform设置正确
    /// </summary>
    private void EnsureRectTransformSetup()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }
        
        // 【修复】保存当前位置，避免被重置
        Vector3 savedPosition = rectTransform.anchoredPosition;
        Vector3 savedScale = rectTransform.localScale;
        
        // 设置锚点为中心（相对于父物体）- 只设置锚点，不改变位置
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        // 【修复】恢复之前保存的位置和缩放（不重置为0）
        rectTransform.anchoredPosition = savedPosition;
        rectTransform.localScale = savedScale;
        
       // GF.LogInfo_wl($"[PDKCard.EnsureRectTransformSetup] 保持位置={savedPosition}, 缩放={savedScale}");
    }
    
    #endregion
}

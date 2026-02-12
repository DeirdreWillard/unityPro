using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using UnityEngine.UI;
[Obfuz.ObfuzIgnore]
public class CustomUIGroupHelper : UIGroupHelperBase
{
    /// <summary>
    /// 是否启用刘海屏安全区域适配
    /// </summary>
    public static bool EnableSafeAreaAdapter = false;
    
    private Canvas m_CachedCanvas = null;
    private RectTransform m_RectTransform = null;
    
    // 安全区域相关
    private Rect m_LastSafeArea = Rect.zero;
    private Vector2Int m_LastScreenSize = Vector2Int.zero;

    /// <summary>
    /// 设置界面组深度。
    /// </summary>
    /// <param name="depth">界面组深度。</param>
    public override void SetDepth(int depth)
    {
        m_CachedCanvas.overrideSorting = true;
        m_CachedCanvas.sortingOrder = depth;
    }

    private void Awake()
    {
        m_CachedCanvas = gameObject.GetOrAddComponent<Canvas>();
        gameObject.GetOrAddComponent<GraphicRaycaster>();
        m_RectTransform = gameObject.GetOrAddComponent<RectTransform>();
    }

    private void Start()
    {
        m_RectTransform.anchorMin = Vector2.zero;
        m_RectTransform.anchorMax = Vector2.one;
        m_RectTransform.anchoredPosition = Vector2.zero;
        m_RectTransform.sizeDelta = Vector2.zero;
        m_RectTransform.localPosition = Vector3.zero;
        
        // 初始应用安全区域适配
        if (EnableSafeAreaAdapter)
        {
            ApplySafeArea();
        }
    }
    
    private void Update()
    {
        // 检测屏幕变化（横竖屏切换、分辨率变化）
        if (EnableSafeAreaAdapter && HasScreenChanged())
        {
            ApplySafeArea();
        }
    }
    
    /// <summary>
    /// 检测屏幕是否发生变化
    /// </summary>
    private bool HasScreenChanged()
    {
        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        return safeArea != m_LastSafeArea || screenSize != m_LastScreenSize;
    }
    
    /// <summary>
    /// 应用安全区域适配（自动根据横竖屏方向）
    /// </summary>
    private void ApplySafeArea()
    {
        if (m_RectTransform == null) return;
        
        Rect safeArea = Screen.safeArea;
        
        // 更新缓存
        m_LastSafeArea = safeArea;
        m_LastScreenSize = new Vector2Int(Screen.width, Screen.height);

        // 如果安全区域与屏幕大小相同，说明没有刘海，直接全屏
        if (Mathf.Approximately(safeArea.width, Screen.width) && 
            Mathf.Approximately(safeArea.height, Screen.height) &&
            Mathf.Approximately(safeArea.x, 0) && 
            Mathf.Approximately(safeArea.y, 0))
        {
            SetFullScreen();
            return;
        }

        // 计算安全区域的归一化锚点
        Vector2 anchorMin = new Vector2(
            safeArea.x / Screen.width,
            safeArea.y / Screen.height
        );
        Vector2 anchorMax = new Vector2(
            (safeArea.x + safeArea.width) / Screen.width,
            (safeArea.y + safeArea.height) / Screen.height
        );

        bool isPortrait = Screen.height > Screen.width;

        if (isPortrait)
        {
            // 竖屏：只适配上下（刘海在顶部），左右全屏
            anchorMin.x = 0;
            anchorMax.x = 1;
        }
        else
        {
            // 横屏：根据刘海位置适配
            // 如果左边有刘海（左边距>0），适配左边和顶部
            // 如果右边有刘海（右边距>0），适配右边和顶部
            // 底部通常是手势区域，也需要适配
            float leftMargin = safeArea.x;
            float rightMargin = Screen.width - safeArea.x - safeArea.width;
            
            // 横屏时左右都可能有刘海，保持左右适配
            // 但上下一般不需要太多空间，可以选择性适配
            // 如果有明显的上下边距（>10像素），则适配
            if (safeArea.y < 10)
            {
                anchorMin.y = 0;  // 底部全屏
            }
            if (Screen.height - (safeArea.y + safeArea.height) < 10)
            {
                anchorMax.y = 1;  // 顶部全屏
            }
        }

        // 应用到RectTransform
        m_RectTransform.anchorMin = anchorMin;
        m_RectTransform.anchorMax = anchorMax;
        m_RectTransform.offsetMin = Vector2.zero;
        m_RectTransform.offsetMax = Vector2.zero;
    }
    
    /// <summary>
    /// 设置为全屏
    /// </summary>
    private void SetFullScreen()
    {
        m_RectTransform.anchorMin = Vector2.zero;
        m_RectTransform.anchorMax = Vector2.one;
        m_RectTransform.offsetMin = Vector2.zero;
        m_RectTransform.offsetMax = Vector2.zero;
    }
}

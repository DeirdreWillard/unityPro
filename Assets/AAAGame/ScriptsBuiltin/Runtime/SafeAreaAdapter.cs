using UnityEngine;

/// <summary>
/// 刘海屏/异形屏安全区域适配组件
/// 自动根据横竖屏方向适配对应的安全区域边缘
/// - 竖屏：适配上下
/// - 横屏：适配左右
/// </summary>
[Obfuz.ObfuzIgnore]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaAdapter : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea = Rect.zero;
    private Vector2Int lastScreenSize = Vector2Int.zero;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        ApplySafeArea();
    }

    private void Update()
    {
        // 检测屏幕变化（横竖屏切换、分辨率变化）
        if (HasScreenChanged())
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
        return safeArea != lastSafeArea || screenSize != lastScreenSize;
    }

    /// <summary>
    /// 应用安全区域
    /// </summary>
    public void ApplySafeArea()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        Rect safeArea = Screen.safeArea;
        
        // 更新缓存
        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

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
            // 竖屏：只适配上下，左右全屏
            anchorMin.x = 0;
            anchorMax.x = 1;
        }
        else
        {
            // 横屏：只适配左右，上下全屏
            anchorMin.y = 0;
            anchorMax.y = 1;
        }

        // 应用到RectTransform
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// 设置为全屏
    /// </summary>
    private void SetFullScreen()
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}

using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.All)]
public class GFBuiltin : MonoBehaviour
{
    public static GFBuiltin Instance { get; private set; }
    public static BaseComponent Base { get; private set; }
    public static ConfigComponent Config { get; private set; }
    public static DataNodeComponent DataNode { get; private set; }
    public static DataTableComponent DataTable { get; private set; }
    public static DebuggerComponent Debugger { get; private set; }
    public static DownloadComponent Download { get; private set; }
    public static EntityComponent Entity { get; private set; }
    public static EventComponent Event { get; private set; }
    public static FsmComponent Fsm { get; private set; }
    public static FileSystemComponent FileSystem { get; private set; }
    public static LocalizationComponent Localization { get; private set; }
    public static NetworkComponent Network { get; private set; }
    public static ProcedureComponent Procedure { get; private set; }
    public static ResourceComponent Resource { get; private set; }
    public static SceneComponent Scene { get; private set; }
    public static SettingComponent Setting { get; private set; }
    public static SoundComponent Sound { get; private set; }
    public static UIComponent UI { get; private set; }
    public static ObjectPoolComponent ObjectPool { get; private set; }
    public static WebRequestComponent WebRequest { get; private set; }
    public static BuiltinViewComponent BuiltinView { get; private set; }
    public static Camera UICamera { get; private set; }

    public static Canvas RootCanvas { get; private set; } = null;


    private void Awake()
    {
        if (Instance == null)
        {
            Application.runInBackground = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Instance = this;
            var resCom = GameEntry.GetComponent<ResourceComponent>();
            if (resCom != null)
            {
                var resTp = resCom.GetType();
                var m_ResourceMode = resTp.GetField("m_ResourceMode", BindingFlags.Instance | BindingFlags.NonPublic);
                m_ResourceMode.SetValue(resCom, AppSettings.Instance.ResourceMode);
                GFBuiltin.LogInfo($"------------Set ResourceMode:{AppSettings.Instance.ResourceMode}------------");
            }
        }
    }

    private void Start()
    {
        GFBuiltin.Base = GameEntry.GetComponent<BaseComponent>();
        GFBuiltin.Config = GameEntry.GetComponent<ConfigComponent>();
        GFBuiltin.DataNode = GameEntry.GetComponent<DataNodeComponent>();
        GFBuiltin.DataTable = GameEntry.GetComponent<DataTableComponent>();
        GFBuiltin.Debugger = GameEntry.GetComponent<DebuggerComponent>();
        GFBuiltin.Download = GameEntry.GetComponent<DownloadComponent>();
        GFBuiltin.Entity = GameEntry.GetComponent<EntityComponent>();
        GFBuiltin.Event = GameEntry.GetComponent<EventComponent>();
        GFBuiltin.Fsm = GameEntry.GetComponent<FsmComponent>();
        GFBuiltin.Procedure = GameEntry.GetComponent<ProcedureComponent>();
        GFBuiltin.Localization = GameEntry.GetComponent<LocalizationComponent>();
        GFBuiltin.Network = GameEntry.GetComponent<NetworkComponent>();
        GFBuiltin.Resource = GameEntry.GetComponent<ResourceComponent>();
        GFBuiltin.FileSystem = GameEntry.GetComponent<FileSystemComponent>();
        GFBuiltin.Scene = GameEntry.GetComponent<SceneComponent>();
        GFBuiltin.Setting = GameEntry.GetComponent<SettingComponent>();
        GFBuiltin.Sound = GameEntry.GetComponent<SoundComponent>();
        GFBuiltin.UI = GameEntry.GetComponent<UIComponent>();
        GFBuiltin.ObjectPool = GameEntry.GetComponent<ObjectPoolComponent>();
        GFBuiltin.WebRequest = GameEntry.GetComponent<WebRequestComponent>();
        GFBuiltin.BuiltinView = GameEntry.GetComponent<BuiltinViewComponent>();

        RootCanvas = GFBuiltin.UI.GetComponentInChildren<Canvas>();
        GFBuiltin.UICamera = RootCanvas.worldCamera;

        UpdateCanvasScaler();
    }
    public void UpdateCanvasScaler(bool isLandscape = false)
    {
        if (RootCanvas == null)
        {
            GFBuiltin.LogError("RootCanvas is null!");
            return;
        }

        CanvasScaler canvasScaler = RootCanvas.GetComponent<CanvasScaler>();
        if (canvasScaler == null)
        {
            GFBuiltin.LogError("CanvasScaler component not found!");
            return;
        }

        // 获取设计分辨率
        Vector2 designResolution = AppSettings.Instance.DesignResolution;

        // 横屏时若设计分辨率为竖屏，交换宽高
        if (isLandscape && designResolution.x < designResolution.y)
        {
            designResolution = new Vector2(designResolution.y, designResolution.x);
        }

        // 使用按屏幕尺寸缩放模式
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = designResolution;

        // 计算屏幕和设计分辨率的宽高比
        float screenAspect = Screen.width / (float)Screen.height;
        float targetAspect = designResolution.x / designResolution.y;
        
        // 宽高比差异(用于判断是否需要特殊处理)
        float aspectDiff = Mathf.Abs(screenAspect - targetAspect);
        
        // 标准适配策略(Unity官方推荐):
        // 1. 屏幕比设计更宽 -> 匹配高度,左右留黑边(保证UI高度不被裁切)
        // 2. 屏幕比设计更窄 -> 匹配宽度,上下留黑边(保证UI宽度不被裁切)
        ScreenFitMode canvasFitMode;
        float matchValue;
        
        if (screenAspect > targetAspect)
        {
            // 屏幕更宽(如平板 iPad 0.75 vs 手机设计 0.45)
            // 标准做法: 匹配高度,左右留黑边
            canvasFitMode = ScreenFitMode.Height;
            matchValue = 1f; // 匹配高度
        }
        else
        {
            // 屏幕更窄或相同(如全面屏)
            // 标准做法: 匹配宽度,上下留黑边或裁切
            canvasFitMode = ScreenFitMode.Width;
            matchValue = 0f; // 匹配宽度
        }

        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = matchValue;

        GFBuiltin.LogInfo($"----------UI适配模式:{canvasFitMode}(match={matchValue:F2}), 屏幕方向:{Screen.orientation}, 设计分辨率:{designResolution}, 屏幕比例:{screenAspect:F3}, 目标比例:{targetAspect:F3}, 差异:{aspectDiff:F3}----------");
    }

    /// <summary>
    /// 切换到横屏模式并更新UI适配
    /// </summary>
    /// <param name="onComplete">切换完成后的回调</param>
    public IEnumerator SwitchToLandscape(System.Action onComplete = null)
    {
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;

        Screen.orientation = ScreenOrientation.LandscapeLeft;

        // 等到分辨率/方向切换生效或超时
        float deadline = Time.realtimeSinceStartup + 1f;
        while (Time.realtimeSinceStartup < deadline && Screen.width < Screen.height)
        {
            yield return null;
        }

        // 再等一帧让 Canvas 正确拿到新分辨率
        yield return null;

        GFBuiltin.Instance.UpdateCanvasScaler(true);
        
        // 执行完成回调
        onComplete?.Invoke();
    }

    /// <summary>
    /// 切换到竖屏模式并更新UI适配
    /// </summary>
    /// <param name="onComplete">切换完成后的回调</param>
    public IEnumerator SwitchToPortrait(System.Action onComplete = null)
    {
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;

        Screen.orientation = ScreenOrientation.Portrait;

        // 等到分辨率/方向切换生效或超时
        float deadline = Time.realtimeSinceStartup + 1f;
        while (Time.realtimeSinceStartup < deadline && Screen.width > Screen.height)
        {
            yield return null;
        }

        // 再等一帧让 Canvas 正确拿到新分辨率
        yield return null;

        GFBuiltin.Instance.UpdateCanvasScaler();
        
        // 执行完成回调
        onComplete?.Invoke();
    }

    /// <summary>
    /// 退出或重启
    /// </summary>
    /// <param name="type"></param>
    public static void Shutdown(ShutdownType type)
    {
        GameEntry.Shutdown(type);
    }

    /// <summary>
    /// Info级别日志 - 同时写入控制台和持久化文件
    /// </summary>
    /// <param name="format">主要日志内容</param>
    /// <param name="debugInfo">调试信息(仅DebugMode显示在Console,但始终写入文件)</param>
    public static void LogInfo(string format, string debugInfo = "")
    {
        // 构建完整消息(包含debugInfo)
        string fullMessage = format;
        if (!string.IsNullOrEmpty(debugInfo))
            fullMessage += debugInfo;

        // Console显示(根据DebugMode决定是否显示debugInfo)
        string consoleMessage = AppSettings.Instance.DebugMode ? fullMessage : format;
        var colorfulFormat = $"<color=#2BD988>{consoleMessage}</color>";
        Debug.Log(colorfulFormat);

        // 持久化日志(始终包含debugInfo,不受DebugMode影响)
        string stackTrace = UnityEngine.StackTraceUtility.ExtractStackTrace();
        LogManager.Instance?.WriteLog(LogManager.LogLevel.Info, fullMessage , stackTrace);
    }

    /// <summary>
    /// Warning级别日志(带堆栈追踪)
    /// </summary>
    public static void LogWarning(string format, string debugInfo = "")
    {
        string fullMessage = format;
        if (!string.IsNullOrEmpty(debugInfo))
            fullMessage += debugInfo;

        string consoleMessage = AppSettings.Instance.DebugMode ? fullMessage : format;
        var colorfulFormat = $"<color=#F2A20C>{consoleMessage}</color>";
        Debug.LogWarning(colorfulFormat);

        // Warning级别也记录堆栈
        string stackTrace = UnityEngine.StackTraceUtility.ExtractStackTrace();
        LogManager.Instance?.WriteLog(LogManager.LogLevel.Warning, fullMessage, stackTrace);
    }

    /// <summary>
    /// Error级别日志(带堆栈追踪)
    /// </summary>
    public static void LogError(string format, string debugInfo = "")
    {
        string fullMessage = format;
        if (!string.IsNullOrEmpty(debugInfo))
            fullMessage += debugInfo;

        string consoleMessage = AppSettings.Instance.DebugMode ? fullMessage : format;
        var colorfulFormat = $"<color=#F22E2E>{consoleMessage}</color>";
        Debug.LogError(colorfulFormat);

        // Error级别记录堆栈
        string stackTrace = UnityEngine.StackTraceUtility.ExtractStackTrace();
        LogManager.Instance?.WriteLog(LogManager.LogLevel.Error, fullMessage, stackTrace);
    }

    /// <summary>
    /// 网络日志(黄色) - wl可能是"网络"拼音首字母
    /// </summary>
    public static void LogInfo_wl(string format, string debugInfo = "")
    {
        string fullMessage = format;
        if (!string.IsNullOrEmpty(debugInfo))
            fullMessage += debugInfo;

        string consoleMessage = AppSettings.Instance.DebugMode ? fullMessage : format;
        var colorfulFormat = $"<color=#FFFF00>{consoleMessage}</color>";
        Debug.Log(colorfulFormat);

        string stackTrace = UnityEngine.StackTraceUtility.ExtractStackTrace();
        LogManager.Instance?.WriteLog(LogManager.LogLevel.Info, fullMessage, stackTrace);
    }

    /// <summary>
    /// 游戏逻辑日志(绿色) - gsc可能是"公司"或特定业务缩写
    /// </summary>
    public static void LogInfo_gsc(string format, string debugInfo = "")
    {
        string fullMessage = format;
        if (!string.IsNullOrEmpty(debugInfo))
            fullMessage += debugInfo;

        string consoleMessage = AppSettings.Instance.DebugMode ? fullMessage : format;
        var colorfulFormat = $"<color=#00FF13>{consoleMessage}</color>";
        Debug.Log(colorfulFormat);

        string stackTrace = UnityEngine.StackTraceUtility.ExtractStackTrace();
        LogManager.Instance?.WriteLog(LogManager.LogLevel.Info, fullMessage, stackTrace);
    }

    /// <summary>
    /// Debug级别日志(仅开发期使用)
    /// </summary>
    public static void LogDebug(string format, string debugInfo = "")
    {
        if (!AppSettings.Instance.DebugMode)
            return;

        string fullMessage = format;
        if (!string.IsNullOrEmpty(debugInfo))
            fullMessage += debugInfo;

        var colorfulFormat = $"<color=#AAAAAA>[Debug] {fullMessage}</color>";
        Debug.Log(colorfulFormat);

        LogManager.Instance?.WriteLog(LogManager.LogLevel.Debug, fullMessage);
    }
}

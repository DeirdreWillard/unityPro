using UnityEngine;

/// <summary>
/// 日志系统初始化器
/// 挂载在Launch场景的GameEntry对象上,确保日志系统在游戏启动时就初始化
/// </summary>
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class LogSystemInitializer : MonoBehaviour
{
    [Header("日志配置")]
    [Tooltip("是否启用日志持久化(建议线上开启)")]
    public bool enablePersistence = true;

    [Tooltip("单个日志文件最大大小(MB)")]
    public float maxLogFileSizeMB = 5f;

    [Tooltip("日志保留天数")]
    public int logRetentionDays = 7;

    [Tooltip("Console显示的最低日志等级(建议线上设为Info)")]
    public LogManager.LogLevel consoleMinLevel = LogManager.LogLevel.Info;

    [Tooltip("持久化存储的最低日志等级(建议设为Debug以保留完整信息)")]
    public LogManager.LogLevel persistenceMinLevel = LogManager.LogLevel.Debug;

    private void Awake()
    {
        InitializeLogSystem();
    }

    private void InitializeLogSystem()
    {
        // 查找或创建LogManager
        LogManager logManager = FindObjectOfType<LogManager>();
        if (logManager == null)
        {
            GameObject logObj = new GameObject("LogManager");
            logManager = logObj.AddComponent<LogManager>();
            DontDestroyOnLoad(logObj);
        }

        // 应用配置
        logManager.enablePersistence = enablePersistence;
        logManager.maxLogFileSizeMB = maxLogFileSizeMB;
        logManager.logRetentionDays = logRetentionDays;
        logManager.consoleMinLevel = consoleMinLevel;
        logManager.persistenceMinLevel = persistenceMinLevel;

        UnityEngine.Debug.Log($"<color=#2BD988>[LogSystem] Initialized - Persistence:{enablePersistence}, ConsoleLevel:{consoleMinLevel}, PersistenceLevel:{persistenceMinLevel}</color>");
    }
}

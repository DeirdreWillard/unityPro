

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 日志管理器 - 支持持久化存储和分级管理
/// 特点:
/// 1. 所有日志都会写入文件,不受DebugMode影响
/// 2. 支持日志等级过滤(Debug/Info/Warning/Error)
/// 3. 自动日志分片(按大小和天数)
/// 4. 性能优化(异步写入队列)
/// </summary>
public class LogManager : MonoBehaviour
{
    public static LogManager Instance { get; private set; }

    // 防止自身通过 UnityEngine.Debug 输出时触发 logMessageReceived 造成重复/递归
    private static int s_SuppressUnityCallbackDepth;

    private static bool IsSuppressingUnityCallback => s_SuppressUnityCallbackDepth > 0;

    private static void BeginSuppressUnityCallback()
    {
        s_SuppressUnityCallbackDepth++;
    }

    private static void EndSuppressUnityCallback()
    {
        s_SuppressUnityCallbackDepth = Math.Max(0, s_SuppressUnityCallbackDepth - 1);
    }

    [Header("日志配置")]
    [Tooltip("是否启用日志持久化")]
    public bool enablePersistence = true;

    [Tooltip("单个日志文件最大大小(MB)")]
    public float maxLogFileSizeMB = 5f;

    [Tooltip("日志保留天数")]
    public int logRetentionDays = 7;

    [Tooltip("Console显示的最低日志等级")]
    public LogLevel consoleMinLevel = LogLevel.Info;

    [Tooltip("持久化存储的最低日志等级")]
    public LogLevel persistenceMinLevel = LogLevel.Debug;

    // 日志等级
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    // 日志结构
    private class LogEntry
    {
        public DateTime timestamp;
        public LogLevel level;
        public string message;
        public string stackTrace;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] ");
            sb.Append($"[{level}] ");
            sb.Append(message);
            if (!string.IsNullOrEmpty(stackTrace) && level >= LogLevel.Error)
            {
                sb.Append($"\nStackTrace: {stackTrace}");
            }
            return sb.ToString();
        }
    }

    // 日志写入队列
    private Queue<LogEntry> logQueue = new Queue<LogEntry>();
    private object lockObject = new object();
    private string currentLogFilePath;
    private StreamWriter logWriter;
    private float currentLogFileSize;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLogSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeLogSystem()
    {
        if (!enablePersistence) return;

        // 创建日志目录
        string logDir = GetLogDirectory();
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        // 清理过期日志
        CleanupOldLogs(logDir);

        // 创建当前日志文件
        CreateNewLogFile();

        // 监听Unity日志(用于捕获第三方/原生 Debug.Log 等)
        Application.logMessageReceived += OnUnityLogReceived;
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= OnUnityLogReceived;
        CloseLogFile();
    }

    /// <summary>
    /// 获取日志目录路径
    /// </summary>
    private string GetLogDirectory()
    {
        // 持久化目录: Application.persistentDataPath/Logs
        return Path.Combine(Application.persistentDataPath, "Logs");
    }

    /// <summary>
    /// 创建新的日志文件
    /// </summary>
    private void CreateNewLogFile()
    {
        CloseLogFile();

        string logDir = GetLogDirectory();
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        currentLogFilePath = Path.Combine(logDir, $"game_{timestamp}.log");

        try
        {
            // 使用FileStream+FileShare.Read允许其他进程同时读取日志文件
            var fileStream = new FileStream(currentLogFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            logWriter = new StreamWriter(fileStream, Encoding.UTF8);
            logWriter.AutoFlush = true;
            currentLogFileSize = 0f;

            // 写入日志头
            logWriter.WriteLine("=================================================");
            logWriter.WriteLine($"Game Log Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logWriter.WriteLine($"Unity Version: {Application.unityVersion}");
            logWriter.WriteLine($"Platform: {Application.platform}");
            logWriter.WriteLine($"Device: {SystemInfo.deviceModel}");
            logWriter.WriteLine($"OS: {SystemInfo.operatingSystem}");
            logWriter.WriteLine($"App Version: {Application.version}");
            logWriter.WriteLine("=================================================\n");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[LogManager] Failed to create log file: {ex.Message}");
        }
    }

    /// <summary>
    /// 强制刷新日志队列到文件（上传前调用）
    /// </summary>
    public void FlushLogs()
    {
        if (!enablePersistence || logWriter == null) return;

        lock (lockObject)
        {
            while (logQueue.Count > 0)
            {
                var entry = logQueue.Dequeue();
                try
                {
                    logWriter.WriteLine(entry.ToString());
                }
                catch { }
            }
            logWriter.Flush();
        }
    }

    /// <summary>
    /// 关闭日志文件
    /// </summary>
    private void CloseLogFile()
    {
        if (logWriter != null)
        {
            try
            {
                logWriter.Close();
                logWriter.Dispose();
            }
            catch { }
            logWriter = null;
        }
    }

    /// <summary>
    /// 清理过期日志
    /// </summary>
    private void CleanupOldLogs(string logDir)
    {
        try
        {
            var files = Directory.GetFiles(logDir, "*.log");
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if ((DateTime.Now - fileInfo.CreationTime).TotalDays > logRetentionDays)
                {
                    File.Delete(file);
                    UnityEngine.Debug.Log($"[LogManager] Deleted old log: {Path.GetFileName(file)}");
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[LogManager] Failed to cleanup old logs: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查文件大小并轮转
    /// </summary>
    private void CheckLogFileRotation()
    {
        if (currentLogFileSize >= maxLogFileSizeMB)
        {
            CreateNewLogFile();
        }
    }

    /// <summary>
    /// Unity日志监听器
    /// </summary>
    private void OnUnityLogReceived(string logString, string stackTrace, LogType type)
    {
        // 忽略本系统主动输出到 Console 的日志，避免重复/递归
        if (IsSuppressingUnityCallback)
            return;

        // 忽略带颜色标签的日志(这些是GFBuiltin已经处理过的日志,避免重复记录)
        if (!string.IsNullOrEmpty(logString) && logString.Contains("<color="))
            return;

        LogLevel level = LogLevel.Info;
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                level = LogLevel.Error;
                break;
            case LogType.Warning:
                level = LogLevel.Warning;
                break;
        }

        // 只做“落盘”，不再二次输出到 Console（原始调用方已经输出过）
        if (!enablePersistence || level < persistenceMinLevel)
            return;

        // 非错误日志不强制写入堆栈，避免文件膨胀
        string persistStack = level >= LogLevel.Error ? stackTrace : null;
        EnqueuePersistOnly(level, logString, persistStack);
    }

    private void EnqueuePersistOnly(LogLevel level, string message, string stackTrace = null)
    {
        var entry = new LogEntry
        {
            timestamp = DateTime.Now,
            level = level,
            message = message,
            stackTrace = stackTrace
        };

        lock (lockObject)
        {
            logQueue.Enqueue(entry);
        }
    }

    /// <summary>
    /// 写入日志(核心方法)
    /// </summary>
    public void WriteLog(LogLevel level, string message, string stackTrace = null)
    {
        // 创建日志条目
        var entry = new LogEntry
        {
            timestamp = DateTime.Now,
            level = level,
            message = message,
            stackTrace = stackTrace
        };

        // 注意：LogManager不再直接输出到Console，由GFBuiltin.LogXxx统一输出
        // 这样避免日志双重打印（GFBuiltin已经打印过了）

        // 持久化存储(不受DebugMode影响)
        if (enablePersistence && level >= persistenceMinLevel)
        {
            lock (lockObject)
            {
                logQueue.Enqueue(entry);
            }
        }
    }

    /// <summary>
    /// 获取日志颜色代码
    /// </summary>
    private string GetColorCode(LogLevel level)
    {
        switch (level)
        {
            case LogLevel.Debug: return "#AAAAAA";
            case LogLevel.Info: return "#2BD988";
            case LogLevel.Warning: return "#F2A20C";
            case LogLevel.Error: return "#F22E2E";
            default: return "#FFFFFF";
        }
    }

    /// <summary>
    /// Update循环处理日志队列
    /// </summary>
    private void Update()
    {
        if (!enablePersistence || logWriter == null) return;

        // 批量写入日志(每帧最多10条)
        int processCount = 0;
        while (processCount < 10)
        {
            LogEntry entry = null;
            lock (lockObject)
            {
                if (logQueue.Count > 0)
                    entry = logQueue.Dequeue();
            }

            if (entry == null) break;

            try
            {
                string logText = entry.ToString();
                logWriter.WriteLine(logText);
                currentLogFileSize += logText.Length / 1024f / 1024f; // 转MB
                processCount++;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[LogManager] Write log failed: {ex.Message}");
            }
        }

        // 检查文件轮转
        CheckLogFileRotation();
    }

    /// <summary>
    /// 获取所有日志文件路径
    /// </summary>
    public List<string> GetAllLogFiles()
    {
        var logFiles = new List<string>();
        try
        {
            string logDir = GetLogDirectory();
            if (Directory.Exists(logDir))
            {
                var files = Directory.GetFiles(logDir, "*.log");
                logFiles.AddRange(files);
                logFiles.Sort((a, b) => File.GetCreationTime(b).CompareTo(File.GetCreationTime(a)));
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[LogManager] Failed to get log files: {ex.Message}");
        }
        return logFiles;
    }

    /// <summary>
    /// 获取最新的日志文件路径
    /// </summary>
    public string GetLatestLogFile()
    {
        return currentLogFilePath;
    }

    /// <summary>
    /// 打包所有日志为ZIP(用于上传)
    /// </summary>
    public string PackLogsToZip()
    {
        try
        {
            string logDir = GetLogDirectory();
            string zipPath = Path.Combine(logDir, $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.zip");

            // 强制刷新当前日志
            lock (lockObject)
            {
                while (logQueue.Count > 0)
                {
                    var entry = logQueue.Dequeue();
                    logWriter?.WriteLine(entry.ToString());
                }
            }

            // 使用Unity内置压缩或第三方库(这里简化为直接返回目录)
            // 实际项目中应该使用 System.IO.Compression 或 SharpZipLib
            return logDir; // 临时返回日志目录
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[LogManager] Failed to pack logs: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 清空所有日志
    /// </summary>
    public void ClearAllLogs()
    {
        try
        {
            string logDir = GetLogDirectory();
            if (Directory.Exists(logDir))
            {
                var files = Directory.GetFiles(logDir, "*.log");
                foreach (var file in files)
                {
                    File.Delete(file);
                }
                CreateNewLogFile();
                UnityEngine.Debug.Log("[LogManager] All logs cleared");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[LogManager] Failed to clear logs: {ex.Message}");
        }
    }

    // ===== 快捷方法 =====
    public static void LogDebug(string message) => Instance?.WriteLog(LogLevel.Debug, message);
    public static void Info(string message) => Instance?.WriteLog(LogLevel.Info, message);
    public static void Warning(string message) => Instance?.WriteLog(LogLevel.Warning, message);
    public static void Error(string message, string stackTrace = null) => Instance?.WriteLog(LogLevel.Error, message, stackTrace);
}

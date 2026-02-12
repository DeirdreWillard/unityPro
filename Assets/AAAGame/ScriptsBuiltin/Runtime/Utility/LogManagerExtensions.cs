using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 日志系统高级功能扩展示例
/// </summary>
public static class LogManagerExtensions
{
    /// <summary>
    /// 记录性能日志(带时间戳)
    /// </summary>
    public static void LogPerformance(string operation, float timeMs)
    {
        if (timeMs > 100) // 超过100ms才记录
        {
            GFBuiltin.LogWarning($"[Performance] {operation} took {timeMs:F2}ms");
        }
    }

    /// <summary>
    /// 记录网络请求日志
    /// </summary>
    public static void LogNetwork(string url, string method, int statusCode, float duration)
    {
        string message = $"[Network] {method} {url} | Status:{statusCode} | Duration:{duration:F2}ms";
        if (statusCode >= 400)
            GFBuiltin.LogError(message);
        else if (duration > 3000)
            GFBuiltin.LogWarning(message);
        else
            GFBuiltin.LogInfo_wl(message);
    }

    /// <summary>
    /// 记录用户行为日志(埋点)
    /// </summary>
    public static void LogUserAction(string action, Dictionary<string, object> parameters = null)
    {
        string message = $"[UserAction] {action}";
        if (parameters != null && parameters.Count > 0)
        {
            var paramStr = string.Join(", ", parameters);
            message += $" | Params: {paramStr}";
        }
        GFBuiltin.LogInfo(message);
    }

    /// <summary>
    /// 记录资源加载日志
    /// </summary>
    public static void LogAssetLoad(string assetPath, bool success, float loadTime)
    {
        string message = $"[Asset] {assetPath} | Success:{success} | Time:{loadTime:F2}ms";
        if (!success)
            GFBuiltin.LogError(message);
        else if (loadTime > 500)
            GFBuiltin.LogWarning(message);
        else
            GFBuiltin.LogInfo(message);
    }

    /// <summary>
    /// 获取日志统计信息
    /// </summary>
    public static string GetLogStats()
    {
        var logFiles = LogManager.Instance?.GetAllLogFiles();
        if (logFiles == null || logFiles.Count == 0)
            return "No logs available";

        long totalSize = 0;
        foreach (var file in logFiles)
        {
            try
            {
                var info = new System.IO.FileInfo(file);
                totalSize += info.Length;
            }
            catch { }
        }

        return $"Logs: {logFiles.Count} files, {totalSize / 1024}KB total";
    }
}

/// <summary>
/// 性能计时器(using自动记录)
/// 用法:
/// using (new PerformanceTimer("LoadScene"))
/// {
///     // 执行耗时操作
/// }
/// </summary>
public class PerformanceTimer : IDisposable
{
    private string operation;
    private float startTime;

    public PerformanceTimer(string operation)
    {
        this.operation = operation;
        this.startTime = Time.realtimeSinceStartup;
    }

    public void Dispose()
    {
        float elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
        LogManagerExtensions.LogPerformance(operation, elapsed);
    }
}

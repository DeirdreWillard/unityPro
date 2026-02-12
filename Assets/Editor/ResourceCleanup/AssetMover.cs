using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.ResourceCleanup
{
    /// <summary>
    /// 资源清理器 - 支持移动或删除
    /// </summary>
    public class AssetMover
    {
        /// <summary>
        /// 删除未使用的资源（推荐：更可靠）
        /// </summary>
        public int DeleteUnusedAssets(List<string> assetPaths)
        {
            if (assetPaths == null || assetPaths.Count == 0)
                return 0;

            Debug.Log($"[资源清理] 开始删除 {assetPaths.Count} 个未使用的资源");

            int deletedCount = 0;
            int failedCount = 0;
            List<string> failedAssets = new List<string>();
            List<string> deletedAssets = new List<string>();

            // 先刷新 AssetDatabase
            AssetDatabase.Refresh();

            // 使用进度条显示
            for (int i = 0; i < assetPaths.Count; i++)
            {
                var assetPath = assetPaths[i];

                // 显示进度条
                float progress = (i + 1) / (float)assetPaths.Count;
                if (EditorUtility.DisplayCancelableProgressBar("删除资源",
                    $"正在删除资源 ({i + 1}/{assetPaths.Count}): {Path.GetFileName(assetPath)}",
                    progress))
                {
                    Debug.LogWarning("[资源清理] 用户取消了删除操作");
                    break;
                }

                if (string.IsNullOrEmpty(assetPath))
                {
                    failedCount++;
                    continue;
                }

                // 标准化路径
                assetPath = assetPath.Replace("\\", "/");

                if (!File.Exists(assetPath))
                {
                    failedAssets.Add($"{assetPath} (文件不存在)");
                    failedCount++;
                    continue;
                }

                try
                {
                    // 删除资源（会同时删除 meta 文件）
                    if (AssetDatabase.DeleteAsset(assetPath))
                    {
                        deletedCount++;
                        deletedAssets.Add(assetPath);
                    }
                    else
                    {
                        failedAssets.Add($"{assetPath} (删除失败)");
                        failedCount++;
                    }
                }
                catch (Exception e)
                {
                    failedAssets.Add($"{assetPath} (异常: {e.Message})");
                    failedCount++;
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            // 创建删除记录文件
            CreateDeletionRecord(deletedAssets, failedAssets);

            // 输出结果日志
            if (failedCount > 0)
            {
                Debug.LogWarning($"[资源清理] 完成删除: 成功 {deletedCount}, 失败 {failedCount}, 总计 {assetPaths.Count}");
                if (failedAssets.Count > 0)
                {
                    Debug.LogWarning($"[资源清理] 前10个失败的资源:\n" + string.Join("\n", failedAssets.Take(10)));
                }
            }
            else
            {
                Debug.Log($"[资源清理] 成功删除 {deletedCount}/{assetPaths.Count} 个资源");
            }

            return deletedCount;
        }

        /// <summary>
        /// 创建删除记录文件
        /// </summary>
        private void CreateDeletionRecord(List<string> deletedAssets, List<string> failedAssets)
        {
            string recordDir = "Assets/Editor/ResourceCleanup/Records";
            if (!Directory.Exists(recordDir))
            {
                Directory.CreateDirectory(recordDir);
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string recordPath = Path.Combine(recordDir, $"DeletionRecord_{timestamp}.txt");

            var content = $@"==========================================
资源清理删除记录
==========================================

删除时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
删除资源数: {deletedAssets.Count}
失败数: {failedAssets.Count}

==========================================
已删除的资源列表
==========================================

";

            foreach (var asset in deletedAssets)
            {
                content += asset + "\n";
            }

            if (failedAssets.Count > 0)
            {
                content += "\n==========================================\n";
                content += "删除失败的资源\n";
                content += "==========================================\n\n";
                foreach (var failed in failedAssets)
                {
                    content += failed + "\n";
                }
            }

            content += @"

==========================================
说明
==========================================

1. 以上资源已被永久删除
2. 如需恢复，请使用版本控制系统（Git等）
3. 建议在删除后进行完整测试
4. 此记录文件仅供参考

";

            File.WriteAllText(recordPath, content, System.Text.Encoding.UTF8);
            Debug.Log($"[资源清理] 删除记录已生成: {recordPath}");
        }

        /// <summary>
        /// 移动资源到 UnusedAssets 目录
        /// </summary>
        public int MoveToUnusedFolder(List<string> assetPaths)
        {
            if (assetPaths == null || assetPaths.Count == 0)
                return 0;

            // 创建带时间戳的目标目录
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string targetRoot = $"Assets/UnusedAssets/{timestamp}";

            Debug.Log($"[资源清理] 开始移动 {assetPaths.Count} 个资源到 {targetRoot}");

            if (!Directory.Exists(targetRoot))
            {
                Directory.CreateDirectory(targetRoot);
            }

            int movedCount = 0;
            int failedCount = 0;
            List<string> failedAssets = new List<string>();
            
            // 先刷新 AssetDatabase
            AssetDatabase.Refresh();

            // 使用进度条显示
            for (int i = 0; i < assetPaths.Count; i++)
            {
                var assetPath = assetPaths[i];
                
                // 显示进度条
                float progress = (i + 1) / (float)assetPaths.Count;
                if (EditorUtility.DisplayCancelableProgressBar("移动资源", 
                    $"正在移动资源 ({i + 1}/{assetPaths.Count}): {Path.GetFileName(assetPath)}", 
                    progress))
                {
                    Debug.LogWarning("[资源清理] 用户取消了移动操作");
                    break;
                }
                
                if (string.IsNullOrEmpty(assetPath))
                {
                    failedCount++;
                    continue;
                }

                // 标准化路径
                assetPath = assetPath.Replace("\\", "/");
                
                if (!File.Exists(assetPath))
                {
                    failedAssets.Add($"{assetPath} (文件不存在)");
                    failedCount++;
                    continue;
                }

                try
                {
                    // 构建目标路径，保持原始目录结构
                    string relativePath = assetPath.Replace("Assets/", "");
                    string targetPath = $"{targetRoot}/{relativePath}";

                    // 确保目标目录存在
                    string targetDir = Path.GetDirectoryName(targetPath)?.Replace("\\", "/");
                    if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    // 检查目标文件是否已存在
                    if (File.Exists(targetPath))
                    {
                        failedAssets.Add($"{assetPath} (目标已存在)");
                        failedCount++;
                        continue;
                    }

                    // 移动资源（会保留 meta 文件）
                    string error = AssetDatabase.MoveAsset(assetPath, targetPath);
                    if (string.IsNullOrEmpty(error))
                    {
                        movedCount++;
                    }
                    else
                    {
                        failedAssets.Add($"{assetPath} (错误: {error})");
                        failedCount++;
                    }
                }
                catch (Exception e)
                {
                    failedAssets.Add($"{assetPath} (异常: {e.Message})");
                    failedCount++;
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            // 创建恢复说明文件
            CreateRestoreInstructions(targetRoot, assetPaths, movedCount, failedAssets);

            // 输出结果日志
            if (failedCount > 0)
            {
                Debug.LogWarning($"[资源清理] 完成移动: 成功 {movedCount}, 失败 {failedCount}, 总计 {assetPaths.Count}");
                Debug.LogWarning($"[资源清理] 前10个失败的资源:\n" + string.Join("\n", failedAssets.Take(10)));
            }
            else
            {
                Debug.Log($"[资源清理] 成功移动 {movedCount}/{assetPaths.Count} 个资源到 {targetRoot}");
            }

            return movedCount;
        }

        /// <summary>
        /// 创建恢复说明文件
        /// </summary>
        private void CreateRestoreInstructions(string targetRoot, List<string> originalPaths, int movedCount, List<string> failedAssets = null)
        {
            string instructionsPath = Path.Combine(targetRoot, "README_恢复说明.txt");
            
            var content = $@"==========================================
未使用资源清理记录
==========================================

清理时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
移动资源数: {movedCount}

==========================================
如何恢复资源
==========================================

方法1: 使用 Unity 撤销功能
- 按 Ctrl+Z 可以撤销移动操作

方法2: 手动移回原位置
- 在 Project 窗口找到需要恢复的资源
- 拖回原来的目录即可

方法3: 批量恢复
- 打开此目录: {targetRoot}
- 将需要的资源文件夹直接移回 Assets 对应位置

==========================================
安全提示
==========================================

1. 此目录的资源已经脱离项目引用
2. 建议保留 1-2 周，确认无问题后再删除
3. 删除前请再次确认资源确实不需要
4. 可以使用 Git 等版本控制系统管理

==========================================
原始路径列表 (前100个)
==========================================

";

            int displayCount = Math.Min(100, originalPaths.Count);
            for (int i = 0; i < displayCount; i++)
            {
                content += originalPaths[i] + "\n";
            }

            if (originalPaths.Count > 100)
            {
                content += $"\n... 还有 {originalPaths.Count - 100} 个资源未列出\n";
            }

            // 添加失败资源列表
            if (failedAssets != null && failedAssets.Count > 0)
            {
                content += "\n==========================================\n";
                content += "移动失败的资源\n";
                content += "==========================================\n\n";
                foreach (var failed in failedAssets)
                {
                    content += failed + "\n";
                }
            }

            File.WriteAllText(instructionsPath, content, System.Text.Encoding.UTF8);
        }
    }
}

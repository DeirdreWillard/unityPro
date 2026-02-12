using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.ResourceCleanup
{
    /// <summary>
    /// 资源扫描器 - 使用 AssetDatabase.GetDependencies 进行静态依赖分析
    /// </summary>
    public class AssetScanner
    {
        private CleanupConfig config;
        private HashSet<string> referencedAssets = new HashSet<string>();
        private HashSet<string> dynamicAssets = new HashSet<string>();

        public AssetScanner(CleanupConfig config)
        {
            this.config = config;
        }

        public CleanupResult Scan(Action<float, string> progressCallback = null)
        {
            var result = new CleanupResult
            {
                scanTime = DateTime.Now
            };

            progressCallback?.Invoke(0f, "收集所有资源...");

            // 1. 收集所有需要扫描的资源
            var allAssets = CollectAllAssets();
            result.totalAssets = allAssets.Count;

            progressCallback?.Invoke(0.1f, $"找到 {allAssets.Count} 个资源文件");

            // 2. 收集动态声明的资源
            progressCallback?.Invoke(0.2f, "扫描动态资源声明...");
            CollectDynamicAssets();
            result.declaredDynamicAssets = dynamicAssets.ToList();

            // 3. 扫描场景依赖
            if (config.checkScenes)
            {
                progressCallback?.Invoke(0.3f, "扫描场景依赖...");
                ScanSceneDependencies();
            }

            // 4. 扫描 Prefab 依赖
            if (config.checkPrefabs)
            {
                progressCallback?.Invoke(0.5f, "扫描 Prefab 依赖...");
                ScanPrefabDependencies(progressCallback);
            }

            // 5. 扫描 ScriptableObject 依赖
            if (config.checkScriptableObjects)
            {
                progressCallback?.Invoke(0.7f, "扫描 ScriptableObject 依赖...");
                ScanScriptableObjectDependencies();
            }

            // 6. 扫描其他入口资源的依赖
            progressCallback?.Invoke(0.8f, "扫描其他资源依赖...");
            ScanOtherEntryPoints();

            // 7. 分析结果
            progressCallback?.Invoke(0.9f, "分析结果...");
            foreach (var asset in allAssets)
            {
                if (referencedAssets.Contains(asset) || dynamicAssets.Contains(asset))
                {
                    result.usedAssets.Add(asset);
                }
                else
                {
                    result.unusedAssets.Add(asset);
                }
            }

            progressCallback?.Invoke(1f, "扫描完成");

            Debug.Log($"[资源清理] 扫描完成 - 总资源: {result.totalAssets}, 被引用: {result.usedAssets.Count}, 未使用: {result.unusedAssets.Count}, 动态声明: {result.declaredDynamicAssets.Count}");

            return result;
        }

        /// <summary>
        /// 收集所有需要扫描的资源
        /// </summary>
        private List<string> CollectAllAssets()
        {
            var assets = new List<string>();

            foreach (var scanDir in config.scanDirectories)
            {
                if (!Directory.Exists(scanDir))
                {
                    Debug.LogWarning($"[资源清理] 扫描目录不存在: {scanDir}");
                    continue;
                }

                var guids = AssetDatabase.FindAssets("", new[] { scanDir });
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    // 排除目录检查
                    if (IsExcluded(path))
                        continue;

                    // 排除扩展名检查
                    string ext = Path.GetExtension(path).ToLower();
                    if (config.excludeExtensions.Contains(ext))
                        continue;

                    // 必须是文件
                    if (File.Exists(path))
                    {
                        assets.Add(path);
                    }
                }
            }

            return assets.Distinct().ToList();
        }

        /// <summary>
        /// 收集动态声明的资源
        /// </summary>
        private void CollectDynamicAssets()
        {
            // 方法1: 自动分析代码中的动态加载（主要方法）
            Debug.Log("[资源清理] 开始自动分析代码中的动态加载...");
            var analyzer = new DynamicSpriteAnalyzer();
            var analyzedAssets = analyzer.AnalyzeAllScripts((progress, status) =>
            {
                Debug.Log($"[资源清理] {status}");
            });

            foreach (var asset in analyzedAssets)
            {
                if (!string.IsNullOrEmpty(asset))
                {
                    dynamicAssets.Add(asset);
                    // 动态资源的依赖也要加入
                    AddDependencies(asset);
                }
            }

            Debug.Log($"[资源清理] 代码分析找到 {analyzedAssets.Count} 个动态加载资源");

            // 生成动态资源分析报告（合并到主报告中，不单独生成）
            // analyzer.GenerateAnalysisReport 已移除，统一在 CleanupReportGenerator 中生成

            // 方法2: 查找所有 DynamicAssetDeclaration ScriptableObject（可选的手动声明）
            var guids = AssetDatabase.FindAssets("t:DynamicAssetDeclaration");
            int manualCount = 0;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var declaration = AssetDatabase.LoadAssetAtPath<DynamicAssetDeclaration>(path);
                if (declaration != null && declaration.dynamicAssets != null)
                {
                    foreach (var asset in declaration.dynamicAssets)
                    {
                        if (asset != null)
                        {
                            string assetPath = AssetDatabase.GetAssetPath(asset);
                            if (!string.IsNullOrEmpty(assetPath) && !dynamicAssets.Contains(assetPath))
                            {
                                dynamicAssets.Add(assetPath);
                                // 动态资源的依赖也要加入
                                AddDependencies(assetPath);
                                manualCount++;
                            }
                        }
                    }
                }
            }

            Debug.Log($"[资源清理] 手动声明找到 {manualCount} 个额外动态资源");
            Debug.Log($"[资源清理] 总计 {dynamicAssets.Count} 个动态资源（代码分析 + 手动声明）");
        }

        /// <summary>
        /// 扫描场景依赖
        /// </summary>
        private void ScanSceneDependencies()
        {
            var sceneGuids = AssetDatabase.FindAssets("t:Scene");
            foreach (var guid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsExcluded(scenePath))
                {
                    AddDependencies(scenePath);
                }
            }
        }

        /// <summary>
        /// 扫描 Prefab 依赖
        /// </summary>
        private void ScanPrefabDependencies(Action<float, string> progressCallback)
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                if (i % 50 == 0)
                {
                    float progress = 0.5f + (i / (float)prefabGuids.Length) * 0.2f;
                    progressCallback?.Invoke(progress, $"扫描 Prefab ({i}/{prefabGuids.Length})...");
                }

                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (!IsExcluded(prefabPath))
                {
                    AddDependencies(prefabPath);
                }
            }
        }

        /// <summary>
        /// 扫描 ScriptableObject 依赖
        /// </summary>
        private void ScanScriptableObjectDependencies()
        {
            var soGuids = AssetDatabase.FindAssets("t:ScriptableObject");
            foreach (var guid in soGuids)
            {
                string soPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsExcluded(soPath))
                {
                    AddDependencies(soPath);
                }
            }
        }

        /// <summary>
        /// 扫描其他入口点（Resources、StreamingAssets 等）
        /// </summary>
        private void ScanOtherEntryPoints()
        {
            // Resources 目录的所有资源都是入口
            var resourcesPaths = new[]
            {
                "Assets/Resources",
                "Assets/AAAGame/Resources"
            };

            foreach (var resourcesPath in resourcesPaths)
            {
                if (Directory.Exists(resourcesPath))
                {
                    var guids = AssetDatabase.FindAssets("", new[] { resourcesPath });
                    foreach (var guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (!IsExcluded(path) && File.Exists(path))
                        {
                            AddDependencies(path);
                        }
                    }
                }
            }

            // StreamingAssets 不需要扫描，因为它不属于 AssetDatabase 管理
        }

        /// <summary>
        /// 添加资源及其所有依赖
        /// </summary>
        private void AddDependencies(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || IsExcluded(assetPath))
                return;

            // 标记此资源被引用
            referencedAssets.Add(assetPath);

            // 获取所有依赖（递归）
            string[] dependencies = AssetDatabase.GetDependencies(assetPath, recursive: true);
            foreach (var dep in dependencies)
            {
                if (!IsExcluded(dep) && File.Exists(dep))
                {
                    referencedAssets.Add(dep);
                }
            }
        }

        /// <summary>
        /// 检查路径是否被排除
        /// </summary>
        private bool IsExcluded(string path)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            // 标准化路径
            path = path.Replace("\\", "/");

            foreach (var excludeDir in config.excludeDirectories)
            {
                string normalizedExclude = excludeDir.Replace("\\", "/");
                if (path.StartsWith(normalizedExclude, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Editor.ResourceCleanup
{
    /// <summary>
    /// 未使用资源清理工具 - 安全的资源清理方案
    /// </summary>
    public class UnusedAssetCleanupTool : EditorWindow
    {
        private Vector2 scrollPosition;
        private CleanupConfig config;
        private CleanupResult lastResult;
        private bool isScanning = false;
        private float scanProgress = 0f;
        private string scanStatus = "";

        [MenuItem("Tools/资源清理工具", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<UnusedAssetCleanupTool>("资源清理工具");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnEnable()
        {
            LoadOrCreateConfig();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawConfigSection();
            EditorGUILayout.Space(10);

            DrawActionsSection();
            EditorGUILayout.Space(10);

            if (lastResult != null)
            {
                DrawResultSection();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("Unity 资源清理工具 - Sprites 专用版", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "功能说明:\n" +
                "1. 静态扫描所有资源依赖关系（场景、Prefab、ScriptableObject）\n" +
                "2. 自动分析代码中的动态加载（SetSprite、LoadSprite、字符串拼接等）\n" +
                "3. 自动扫描已知的动态资源目录（Cards、Avatar、CardType等）\n" +
                "4. 自动扫描 DataTable 配置中的图片路径\n" +
                "5. 识别未被引用的资源\n" +
                "6. 安全移动到 UnusedAssets 目录或直接删除\n" +
                "7. 生成详细的清理报告和动态资源分析报告\n\n" +
                "💡 专为 Sprites 目录优化，自动排除动态加载的图片资源\n" +
                "⚠️ 建议：清理前先提交代码到版本控制系统（Git）",
                MessageType.Info);
            GUILayout.EndVertical();
        }

        private void DrawConfigSection()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("配置选项", EditorStyles.boldLabel);

            // 扫描目录
            EditorGUILayout.LabelField("扫描目录 (相对于 Assets)", EditorStyles.miniBoldLabel);
            for (int i = 0; i < config.scanDirectories.Count; i++)
            {
                GUILayout.BeginHorizontal();
                config.scanDirectories[i] = EditorGUILayout.TextField(config.scanDirectories[i]);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    config.scanDirectories.RemoveAt(i);
                    i--;
                }
                GUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ 添加扫描目录"))
            {
                config.scanDirectories.Add("Assets/AAAGame/Arts");
            }

            EditorGUILayout.Space(5);

            // 排除目录
            EditorGUILayout.LabelField("排除目录 (相对于 Assets)", EditorStyles.miniBoldLabel);
            for (int i = 0; i < config.excludeDirectories.Count; i++)
            {
                GUILayout.BeginHorizontal();
                config.excludeDirectories[i] = EditorGUILayout.TextField(config.excludeDirectories[i]);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    config.excludeDirectories.RemoveAt(i);
                    i--;
                }
                GUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ 添加排除目录"))
            {
                config.excludeDirectories.Add("Assets/Plugins");
            }

            EditorGUILayout.Space(5);

            // 排除扩展名
            EditorGUILayout.LabelField("排除文件类型", EditorStyles.miniBoldLabel);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < config.excludeExtensions.Count; i++)
            {
                config.excludeExtensions[i] = EditorGUILayout.TextField(config.excludeExtensions[i], GUILayout.Width(80));
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    config.excludeExtensions.RemoveAt(i);
                    i--;
                }
            }
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                config.excludeExtensions.Add(".cs");
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 其他选项
            config.moveToUnusedFolder = EditorGUILayout.Toggle("移动到 UnusedAssets 目录", config.moveToUnusedFolder);
            config.generateReport = EditorGUILayout.Toggle("生成清理报告", config.generateReport);
            config.checkScenes = EditorGUILayout.Toggle("检查场景依赖", config.checkScenes);
            config.checkPrefabs = EditorGUILayout.Toggle("检查 Prefab 依赖", config.checkPrefabs);
            config.checkScriptableObjects = EditorGUILayout.Toggle("检查 ScriptableObject 依赖", config.checkScriptableObjects);

            EditorGUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("保存配置"))
            {
                SaveConfig();
            }
            if (GUILayout.Button("重置为默认"))
            {
                config = CleanupConfig.CreateDefault();
                SaveConfig();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawActionsSection()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("操作", EditorStyles.boldLabel);

            GUI.enabled = !isScanning;

            if (GUILayout.Button("1. 扫描未使用的资源", GUILayout.Height(35)))
            {
                ScanUnusedAssets();
            }

            GUI.enabled = !isScanning && lastResult != null && lastResult.unusedAssets.Count > 0;

            if (GUILayout.Button("2. 删除未使用资源（推荐）", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("确认删除",
                    $"将永久删除 {lastResult.unusedAssets.Count} 个未使用的资源。\n\n" +
                    $"⚠️ 此操作无法撤销！\n" +
                    $"建议先提交代码到版本控制系统。\n\n" +
                    $"是否继续？",
                    "确认删除", "取消"))
                {
                    DeleteUnusedAssets();
                }
            }

            if (GUILayout.Button("3. 移动未使用资源到 UnusedAssets 目录（备选）", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("确认操作",
                    $"将移动 {lastResult.unusedAssets.Count} 个未使用的资源到 UnusedAssets 目录。\n\n" +
                    $"注意：移动功能可能不稳定，推荐使用删除功能。\n\n" +
                    $"是否继续？",
                    "确认移动", "取消"))
                {
                    MoveUnusedAssets();
                }
            }

            GUI.enabled = true;

            if (isScanning)
            {
                EditorGUILayout.Space(5);
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(20)), scanProgress, scanStatus);
            }

            GUILayout.EndVertical();
        }

        private void DrawResultSection()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("扫描结果", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("扫描时间", lastResult.scanTime.ToString("yyyy-MM-dd HH:mm:ss"));
            EditorGUILayout.LabelField("总资源数", lastResult.totalAssets.ToString());
            EditorGUILayout.LabelField("被引用资源", lastResult.usedAssets.Count.ToString());
            EditorGUILayout.LabelField("未使用资源", lastResult.unusedAssets.Count.ToString(), EditorStyles.boldLabel);
            EditorGUILayout.LabelField("动态声明资源", lastResult.declaredDynamicAssets.Count.ToString());

            EditorGUILayout.Space(5);

            if (lastResult.unusedAssets.Count > 0)
            {
                EditorGUILayout.LabelField("未使用资源列表 (前100个):", EditorStyles.miniBoldLabel);
                int displayCount = Mathf.Min(100, lastResult.unusedAssets.Count);
                for (int i = 0; i < displayCount; i++)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.TextField(lastResult.unusedAssets[i], GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("定位", GUILayout.Width(60)))
                    {
                        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(lastResult.unusedAssets[i]);
                        if (obj != null)
                        {
                            Selection.activeObject = obj;
                            EditorGUIUtility.PingObject(obj);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                if (lastResult.unusedAssets.Count > 100)
                {
                    EditorGUILayout.HelpBox($"还有 {lastResult.unusedAssets.Count - 100} 个未使用资源未显示，请查看生成的报告文件。", MessageType.Info);
                }
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("打开报告文件"))
            {
                if (!string.IsNullOrEmpty(lastResult.reportPath) && File.Exists(lastResult.reportPath))
                {
                    System.Diagnostics.Process.Start(lastResult.reportPath);
                }
            }

            GUILayout.EndVertical();
        }

        private void ScanUnusedAssets()
        {
            isScanning = true;
            scanProgress = 0f;
            scanStatus = "准备扫描...";

            try
            {
                var scanner = new AssetScanner(config);
                lastResult = scanner.Scan((progress, status) =>
                {
                    scanProgress = progress;
                    scanStatus = status;
                    Repaint();
                });

                if (config.generateReport)
                {
                    var reportGenerator = new CleanupReportGenerator();
                    lastResult.reportPath = reportGenerator.GenerateReport(lastResult, config);
                }

                EditorUtility.DisplayDialog("扫描完成",
                    $"扫描完成！\n\n" +
                    $"总资源数: {lastResult.totalAssets}\n" +
                    $"被引用资源: {lastResult.usedAssets.Count}\n" +
                    $"未使用资源: {lastResult.unusedAssets.Count}\n" +
                    $"动态声明资源: {lastResult.declaredDynamicAssets.Count}\n\n" +
                    $"报告路径: {lastResult.reportPath}",
                    "确定");
            }
            catch (Exception e)
            {
                Debug.LogError($"扫描失败: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("扫描失败", $"扫描过程中发生错误:\n{e.Message}", "确定");
            }
            finally
            {
                isScanning = false;
                scanProgress = 0f;
                scanStatus = "";
                Repaint();
            }
        }

        private void DeleteUnusedAssets()
        {
            if (lastResult == null || lastResult.unusedAssets.Count == 0)
                return;

            var mover = new AssetMover();
            int deletedCount = mover.DeleteUnusedAssets(lastResult.unusedAssets);

            EditorUtility.DisplayDialog("删除完成",
                $"成功删除 {deletedCount} 个未使用的资源。\n\n" +
                $"删除记录已保存到:\nAssets/Editor/ResourceCleanup/Records/\n\n" +
                $"如需恢复，请使用版本控制系统（Git）。",
                "确定");

            AssetDatabase.Refresh();
            lastResult = null;
        }

        private void MoveUnusedAssets()
        {
            if (lastResult == null || lastResult.unusedAssets.Count == 0)
                return;

            var mover = new AssetMover();
            int movedCount = mover.MoveToUnusedFolder(lastResult.unusedAssets);

            EditorUtility.DisplayDialog("移动完成",
                $"成功移动 {movedCount} 个资源到 UnusedAssets 目录。\n\n" +
                $"目标路径: Assets/UnusedAssets/{DateTime.Now:yyyyMMdd_HHmmss}/\n\n" +
                $"如需恢复，可使用 Ctrl+Z 撤销或手动移回。",
                "确定");

            AssetDatabase.Refresh();
            lastResult = null;
        }

        private void LoadOrCreateConfig()
        {
            string configPath = GetConfigPath();
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                config = JsonUtility.FromJson<CleanupConfig>(json);
            }
            else
            {
                config = CleanupConfig.CreateDefault();
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            string json = JsonUtility.ToJson(config, true);
            File.WriteAllText(GetConfigPath(), json);
            AssetDatabase.Refresh();
        }

        private string GetConfigPath()
        {
            string dir = "Assets/Editor/ResourceCleanup";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return Path.Combine(dir, "CleanupConfig.json");
        }
    }

    /// <summary>
    /// 清理配置
    /// </summary>
    [Serializable]
    public class CleanupConfig
    {
        public List<string> scanDirectories = new List<string>();
        public List<string> excludeDirectories = new List<string>();
        public List<string> excludeExtensions = new List<string>();
        public bool moveToUnusedFolder = true;
        public bool generateReport = true;
        public bool checkScenes = true;
        public bool checkPrefabs = true;
        public bool checkScriptableObjects = true;

        public static CleanupConfig CreateDefault()
        {
            return new CleanupConfig
            {
                scanDirectories = new List<string>
                {
                    "Assets/AAAGame/Sprites"
                },
                excludeDirectories = new List<string>
                {
                    "Assets/Plugins",
                    "Assets/Editor",
                    "Assets/HybridCLRData",
                    "Assets/UnusedAssets",
                    "Assets/AAAGame/ScriptsBuiltin",
                    "Assets/AAAGame/Scripts"
                },
                excludeExtensions = new List<string>
                {
                    ".cs", ".dll", ".meta", ".asmdef", ".spriteatlasv2"
                },
                moveToUnusedFolder = true,
                generateReport = true,
                checkScenes = true,
                checkPrefabs = true,
                checkScriptableObjects = true
            };
        }
    }

    /// <summary>
    /// 清理结果
    /// </summary>
    public class CleanupResult
    {
        public DateTime scanTime;
        public int totalAssets;
        public List<string> usedAssets = new List<string>();
        public List<string> unusedAssets = new List<string>();
        public List<string> declaredDynamicAssets = new List<string>();
        public Dictionary<string, List<string>> assetDependencies = new Dictionary<string, List<string>>();
        public string reportPath;
    }
}

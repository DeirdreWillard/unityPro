using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UGF.EditorTools;

namespace AAAGame.Editor
{
    /// <summary>
    /// 音效映射生成器 - 自动扫描Audio文件夹并生成映射文件
    /// </summary>
    [EditorToolMenu("音效/音效映射生成器", null, 400)]
    public class AudioMappingGenerator : EditorToolBase
    {
        public override string ToolName => "音效映射生成器";
        public override Vector2Int WinSize => new Vector2Int(600, 400);
        
        private const string AUDIO_ROOT_PATH = "Assets/AAAGame/Audio";
        private const string OUTPUT_CONST_PATH = "Assets/AAAGame/Scripts/Common/AudioKeys.cs";
        
        private static HashSet<string> supportedExtensions = new HashSet<string> 
        { 
            ".mp3", ".wav", ".ogg", ".aiff", ".aif" 
        };
        
        private bool includeSubfolders = true;
        private Vector2 scrollPosition;
        private List<AudioFileInfo> scannedFiles = new List<AudioFileInfo>();
        private bool showSettings = false;
        
        [System.Serializable]
        public class AudioFileInfo
        {
            public string fullPath;
            public string relativePath;
            public string keyName;
            public bool isSelected = true;
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("音效映射生成器", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "此工具会自动扫描 Assets/AAAGame/Audio 文件夹下的所有音频文件,\n" +
                "生成音效键常量类,常量值为对应的音频文件路径(不含扩展名)。\n\n" +
                "生成文件:\n" +
                "• AudioKeys.cs - 音效键常量类 (包含路径映射)", 
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // 自动生成设置（折叠面板）
            showSettings = EditorGUILayout.Foldout(showSettings, "⚙️ 自动生成设置", true, EditorStyles.foldoutHeader);
            if (showSettings)
            {
                EditorGUILayout.BeginVertical("box");
                
                EditorGUI.BeginChangeCheck();
                bool autoGenerate = AudioMappingGeneratorPrefs.AutoGenerateEnabled;
                bool showNotification = AudioMappingGeneratorPrefs.ShowNotification;
                
                autoGenerate = EditorGUILayout.Toggle(
                    new GUIContent("🔄 启用自动生成", 
                    "检测到音频文件变化时自动生成映射文件\n" +
                    "（导入/删除/移动音频文件时触发）"), 
                    autoGenerate);
                
                EditorGUI.BeginDisabledGroup(!autoGenerate);
                showNotification = EditorGUILayout.Toggle(
                    new GUIContent("📢 显示生成通知", 
                    "在控制台显示自动生成的详细日志"), 
                    showNotification);
                EditorGUI.EndDisabledGroup();
                
                if (EditorGUI.EndChangeCheck())
                {
                    AudioMappingGeneratorPrefs.AutoGenerateEnabled = autoGenerate;
                    AudioMappingGeneratorPrefs.ShowNotification = showNotification;
                }
                
                EditorGUILayout.Space(5);
                
                // 状态提示
                if (autoGenerate)
                {
                    EditorGUILayout.HelpBox(
                        "✅ 自动生成已启用\n" +
                        "当您拖入、删除或移动音频文件时，会自动重新生成映射文件。", 
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "⚠️ 自动生成已禁用\n" +
                        "您需要手动点击\"生成映射文件\"按钮来更新映射。", 
                        MessageType.Warning);
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            
            // 配置选项
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("扫描选项", EditorStyles.boldLabel);
            includeSubfolders = EditorGUILayout.Toggle("包含子文件夹", includeSubfolders);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("扫描音频文件", GUILayout.Height(30)))
            {
                ScanAudioFiles();
            }
            
            GUI.enabled = scannedFiles.Count > 0;
            if (GUILayout.Button("生成映射文件", GUILayout.Height(30)))
            {
                GenerateMappingFiles();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // 显示扫描结果
            if (scannedFiles.Count > 0)
            {
                EditorGUILayout.LabelField($"扫描到 {scannedFiles.Count} 个音频文件:", EditorStyles.boldLabel);
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                
                foreach (var file in scannedFiles)
                {
                    EditorGUILayout.BeginHorizontal("box");
                    file.isSelected = EditorGUILayout.Toggle(file.isSelected, GUILayout.Width(20));
                    EditorGUILayout.LabelField(file.keyName, GUILayout.Width(250));
                    EditorGUILayout.LabelField(file.relativePath, EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
        }
        
        private void ScanAudioFiles()
        {
            scannedFiles.Clear();
            
            if (!Directory.Exists(AUDIO_ROOT_PATH))
            {
                EditorUtility.DisplayDialog("错误", $"音频根目录不存在: {AUDIO_ROOT_PATH}", "确定");
                return;
            }
            
            var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var allFiles = Directory.GetFiles(AUDIO_ROOT_PATH, "*.*", searchOption);
            
            foreach (var filePath in allFiles)
            {
                var extension = Path.GetExtension(filePath).ToLower();
                if (!supportedExtensions.Contains(extension))
                    continue;
                
                var relativePath = filePath.Replace("\\", "/").Replace(AUDIO_ROOT_PATH + "/", "");
                var keyName = GenerateKeyName(relativePath);
                
                scannedFiles.Add(new AudioFileInfo
                {
                    fullPath = filePath.Replace("\\", "/"),
                    relativePath = relativePath,
                    keyName = keyName,
                    isSelected = true
                });
            }
            
            scannedFiles.Sort((a, b) => string.Compare(a.keyName, b.keyName));
            
            Debug.Log($"扫描完成,找到 {scannedFiles.Count} 个音频文件");
        }
        
        private string GenerateKeyName(string relativePath)
        {
            // 移除扩展名
            var pathWithoutExt = Path.ChangeExtension(relativePath, null);
            
            // 替换路径分隔符和特殊字符为下划线
            var keyName = pathWithoutExt
                .Replace("/", "_")
                .Replace("\\", "_")
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace(".", "_")
                .ToUpper();
            
            return keyName;
        }
        
        private void GenerateMappingFiles()
        {
            var selectedFiles = scannedFiles.FindAll(f => f.isSelected);
            
            if (selectedFiles.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "请至少选择一个音频文件", "确定");
                return;
            }
            
            // 生成常量类
            GenerateConstClass(selectedFiles);
            
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("完成", 
                $"成功生成音效常量文件!\n\n" +
                $"常量类: {OUTPUT_CONST_PATH}\n" +
                $"共处理 {selectedFiles.Count} 个音频文件", 
                "确定");
        }
        
        private void GenerateConstClass(List<AudioFileInfo> files)
        {
            GenerateConstClass(files, OUTPUT_CONST_PATH);
        }

        /// <summary>
        /// 生成常量类（静态方法，供自动生成器调用）
        /// </summary>
        public static void GenerateConstClass(List<AudioFileInfo> files, string outputPath = null)
        {
            if (outputPath == null)
                outputPath = OUTPUT_CONST_PATH;
                
            var sb = new StringBuilder();
            
            sb.AppendLine("// 此文件由AudioMappingGenerator自动生成,请勿手动修改");
            sb.AppendLine("// 生成时间: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine();
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine("/// <summary>");
            sb.AppendLine("/// 音效键常量定义");
            sb.AppendLine("/// 使用方法:Sound.PlayEffect(AudioKeys.Get(\"KEY_NAME\"));");
            sb.AppendLine("/// </summary>");
            sb.AppendLine("public static class AudioKeys");
            sb.AppendLine("{");
            sb.AppendLine("    private static readonly Dictionary<string, string> _audioMap = new Dictionary<string, string>");
            sb.AppendLine("    {");
            
            // 生成字典键值对
            int index = 0;
            foreach (var file in files)
            {
                var pathWithExtension = file.relativePath.Replace("\\", "/");
                sb.Append($"        {{ \"{file.keyName}\", \"{pathWithExtension}\" }}");
                
                if (index < files.Count - 1)
                    sb.AppendLine(",");
                else
                    sb.AppendLine();
                    
                index++;
            }
            
            sb.AppendLine("    };");
            sb.AppendLine();
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// 通过键名获取音频路径");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static string Get(string key)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (_audioMap.TryGetValue(key, out string path))");
            sb.AppendLine("            return path;");
            sb.AppendLine("        return null;");
            sb.AppendLine("    }");
            sb.AppendLine();
            
            // 按文件夹分组生成常量（可选，用于代码提示）
            var groupedFiles = new Dictionary<string, List<AudioFileInfo>>();
            foreach (var file in files)
            {
                var folder = Path.GetDirectoryName(file.relativePath).Replace("\\", "/");
                if (string.IsNullOrEmpty(folder)) folder = "根目录";
                
                if (!groupedFiles.ContainsKey(folder))
                    groupedFiles[folder] = new List<AudioFileInfo>();
                
                groupedFiles[folder].Add(file);
            }
            
            // 生成常量名（用于代码提示和编译时检查,值直接就是路径）
            foreach (var group in groupedFiles)
            {
                sb.AppendLine();
                sb.AppendLine($"    #region {group.Key}");
                sb.AppendLine();
                
                foreach (var file in group.Value)
                {
                    var pathWithExtension = file.relativePath.Replace("\\", "/");
                    var comment = $"/// <summary>{file.relativePath}</summary>";
                    sb.AppendLine($"    {comment}");
                    sb.AppendLine($"    public const string {file.keyName} = \"{pathWithExtension}\";");
                }
                
                sb.AppendLine();
                sb.AppendLine("    #endregion");
            }
            
            sb.AppendLine("}");
            
            // 确保目录存在
            var directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"生成常量类: {outputPath}");
        }
    }
}

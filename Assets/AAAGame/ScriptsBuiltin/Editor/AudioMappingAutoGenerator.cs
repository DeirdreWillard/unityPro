using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AAAGame.Editor
{
    /// <summary>
    /// 音效映射自动生成器 - 监听Audio文件夹变化，自动生成映射文件
    /// </summary>
    public class AudioMappingAutoGenerator : AssetPostprocessor
    {
        private const string AUDIO_ROOT_PATH = "Assets/AAAGame/Audio";
        private const string AUDIO_ROOT_PATH_NORMALIZED = "Assets/AAAGame/Audio/";
        
        private static readonly HashSet<string> supportedExtensions = new HashSet<string> 
        { 
            ".mp3", ".wav", ".ogg", ".aiff", ".aif" 
        };

        /// <summary>
        /// 监听资源导入、删除、移动事件
        /// </summary>
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool audioChanged = false;
            List<string> changedFiles = new List<string>();

            // 检查导入的资源
            foreach (string path in importedAssets)
            {
                if (IsAudioFileInTargetFolder(path))
                {
                    audioChanged = true;
                    changedFiles.Add($"导入: {path}");
                }
            }

            // 检查删除的资源
            foreach (string path in deletedAssets)
            {
                if (IsAudioFileInTargetFolder(path))
                {
                    audioChanged = true;
                    changedFiles.Add($"删除: {path}");
                }
            }

            // 检查移动的资源
            for (int i = 0; i < movedAssets.Length; i++)
            {
                string toPath = movedAssets[i];
                string fromPath = movedFromAssetPaths[i];
                
                // 移入Audio文件夹或在Audio文件夹内移动
                if (IsAudioFileInTargetFolder(toPath) || IsAudioFileInTargetFolder(fromPath))
                {
                    audioChanged = true;
                    changedFiles.Add($"移动: {fromPath} → {toPath}");
                }
            }

            // 如果检测到音频文件变化，自动生成映射
            if (audioChanged)
            {
                // 检查是否启用自动生成
                if (!AudioMappingGeneratorPrefs.AutoGenerateEnabled)
                {
                    if (AudioMappingGeneratorPrefs.ShowNotification)
                    {
                        Debug.Log($"<color=yellow>[音效映射自动生成]</color> 检测到音频文件变化，但自动生成已禁用\n" +
                                  $"可以在 Edit → Preferences → 音效映射生成器 中启用");
                    }
                    return;
                }

                if (AudioMappingGeneratorPrefs.ShowNotification)
                {
                    Debug.Log($"<color=cyan>[音效映射自动生成]</color> 检测到音频文件变化:\n{string.Join("\n", changedFiles)}");
                }
                
                // 延迟执行，确保资源导入完成
                EditorApplication.delayCall += () =>
                {
                    GenerateMappingFiles();
                };
            }
        }

        /// <summary>
        /// 手动触发生成（供偏好设置调用）
        /// </summary>
        public static void ManualGenerate()
        {
            Debug.Log("<color=cyan>[音效映射手动生成]</color> 开始扫描并生成映射文件...");
            GenerateMappingFiles();
        }

        /// <summary>
        /// 检查路径是否是Audio文件夹下的音频文件
        /// </summary>
        private static bool IsAudioFileInTargetFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // 规范化路径
            string normalizedPath = path.Replace("\\", "/");
            
            // 检查是否在Audio文件夹下
            if (!normalizedPath.StartsWith(AUDIO_ROOT_PATH_NORMALIZED) && 
                !normalizedPath.Equals(AUDIO_ROOT_PATH))
                return false;

            // 检查文件扩展名
            string extension = Path.GetExtension(path).ToLower();
            return supportedExtensions.Contains(extension);
        }

        /// <summary>
        /// 生成映射文件（调用AudioMappingGenerator的逻辑）
        /// </summary>
        private static void GenerateMappingFiles()
        {
            try
            {
                // 扫描所有音频文件
                var audioFiles = ScanAudioFiles();
                
                if (audioFiles.Count == 0)
                {
                    Debug.LogWarning("[音效映射自动生成] 未找到音频文件，跳过生成");
                    return;
                }

                // 生成常量类（包含路径映射）
                AudioMappingGenerator.GenerateConstClass(audioFiles);
                
                AssetDatabase.Refresh();
                
                if (AudioMappingGeneratorPrefs.ShowNotification)
                {
                    Debug.Log($"<color=green>[音效映射自动生成]</color> 成功生成音效常量文件！共处理 {audioFiles.Count} 个音频文件");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[音效映射自动生成] 生成失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 扫描所有音频文件
        /// </summary>
        private static List<AudioMappingGenerator.AudioFileInfo> ScanAudioFiles()
        {
            var audioFiles = new List<AudioMappingGenerator.AudioFileInfo>();
            
            if (!Directory.Exists(AUDIO_ROOT_PATH))
            {
                return audioFiles;
            }

            var allFiles = Directory.GetFiles(AUDIO_ROOT_PATH, "*.*", SearchOption.AllDirectories);
            
            foreach (var filePath in allFiles)
            {
                var extension = Path.GetExtension(filePath).ToLower();
                if (!supportedExtensions.Contains(extension))
                    continue;
                
                var relativePath = filePath.Replace("\\", "/").Replace(AUDIO_ROOT_PATH_NORMALIZED, "");
                var keyName = GenerateKeyName(relativePath);
                
                audioFiles.Add(new AudioMappingGenerator.AudioFileInfo
                {
                    fullPath = filePath.Replace("\\", "/"),
                    relativePath = relativePath,
                    keyName = keyName,
                    isSelected = true
                });
            }
            
            audioFiles.Sort((a, b) => string.Compare(a.keyName, b.keyName));
            
            return audioFiles;
        }

        /// <summary>
        /// 生成常量名
        /// </summary>
        private static string GenerateKeyName(string relativePath)
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
    }
}

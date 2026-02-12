using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Editor.ResourceCleanup
{
    /// <summary>
    /// 动态 Sprite 分析器 - 自动检测代码中动态加载的图片资源
    /// </summary>
    public class DynamicSpriteAnalyzer
    {
        // 需要检测的动态加载方法模式
        private static readonly string[] LoadMethods = new[]
        {
            // 基础的 SetSprite/LoadSprite 调用
            @"\.SetSprite\s*\(\s*[""']([^""']+)[""']",           // image.SetSprite("spriteName")
            @"\.LoadSprite\s*\(\s*[""']([^""']+)[""']",          // GF.UI.LoadSprite("spriteName")
            @"\.SetTexture\s*\(\s*[""']([^""']+)[""']",          // rawImage.SetTexture("textureName")
            @"\.LoadTexture\s*\(\s*[""']([^""']+)[""']",         // GF.UI.LoadTexture("textureName")
            @"\.LoadSpriteAtlas\s*\(\s*[""']([^""']+)[""']",     // GF.UI.LoadSpriteAtlas("atlasName")
            
            // 使用 $字符串插值 的 SetSprite 调用
            @"\.SetSprite\s*\(\s*\$[""']([^""']+)[""']",         // image.SetSprite($"path/{var}.png")
            @"\.LoadSprite\s*\(\s*\$[""']([^""']+)[""']",        // GF.UI.LoadSprite($"path/{var}.png")
            
            // GetSpritesPath/GetTexturePath 调用
            @"GetSpritesPath\s*\(\s*[""']([^""']+)[""']",        // AssetsPath.GetSpritesPath("name")
            @"GetSpritesPath\s*\(\s*\$[""']([^""']+)[""']",      // AssetsPath.GetSpritesPath($"Avatar/{index}.png") - 字符串插值
            @"GetTexturePath\s*\(\s*[""']([^""']+)[""']",        // AssetsPath.GetTexturePath("name")
            @"GetTexturePath\s*\(\s*\$[""']([^""']+)[""']",      // AssetsPath.GetTexturePath($"name") - 字符串插值
            
            // Resources.Load 调用
            @"Resources\.Load\s*<\s*Sprite\s*>\s*\(\s*[""']([^""']+)[""']",   // Resources.Load<Sprite>("path")
            @"Resources\.Load\s*<\s*Texture2D\s*>\s*\(\s*[""']([^""']+)[""']", // Resources.Load<Texture2D>("path")
        };

        // 检测字符串拼接模式（+ 拼接符）
        private static readonly string[] ConcatPatterns = new[]
        {
            @"[""']([^""']*)/[""']\s*\+\s*[\w\.]+\s*\+\s*[""']([^""']+)[""']",  // "path/" + var + ".png"
            @"[""']([^""']+)[""']\s*\+\s*[\w\.]+\s*\+\s*[""']([^""']*)[""']",  // "prefix" + var + "suffix"
            @"\.SetSprite\s*\(\s*[""']([^""']+)[""']\s*\+[^)]+\+\s*[""']([^""']+)[""']", // SetSprite("prefix" + var + "suffix")
            @"\.LoadSprite\s*\([^)]*[""']([^""']+)[""']\s*\+[^)]+\+\s*[""']([^""']+)[""']", // LoadSprite(path + "prefix" + var + ".png")
        };

        // 检测字符串变量赋值（用于检测先赋值后使用的情况）
        private static readonly string[] VariablePatterns = new[]
        {
            @"string\s+\w+\s*=\s*\$[""']([^""']+)[""']",  // string varName = $"path/{var}.png"
            @"var\s+\w+\s*=\s*\$[""']([^""']+)[""']",     // var varName = $"path/{var}.png"
            @"\w+\s*=\s*[""']([^""']*)[""']\s*\+\s*[\w\.]+\s*\+\s*[""']([^""']*\.(?:png|jpg|jpeg))[""']",  // path = "prefix" + var + ".png"
            @"string\.Format\s*\(\s*[""']([^""']+\.(?:png|jpg|jpeg))[""']",  // string.Format("path/{0}.png", var)
            @"string\.Format\s*\(\s*[""']([^""']+\{0\}[^""']*)[""']",  // string.Format("path/{0}.png", var) - 带占位符
            @"CardPath\s*=\s*[""']([^""']+)[""']\s*\+",   // CardPath = "Common/Cards/" + ...
            @"typepath\s*=\s*[""']([^""']+)[""']\s*\+",   // typepath = "path" + ...
            @"imgPath\s*=\s*[""']([^""']+)[""']",         // imgPath = "ZJH/Operate/{0}.png"
        };
        
        // 检测从变量或属性读取路径的模式（如DataTable字段）
        private static readonly string[] PropertyPathPatterns = new[]
        {
            @"row\.([\w]+Icon)",       // row.LanguageIcon, row.XxxIcon
            @"row\.([\w]+Path)",       // row.XxxPath
            @"config\.([\w]+Path)",    // config.XxxPath
            @"([\w]+)\.Path\s*\+\s*[""']\.(?:png|jpg)",  // itemConfig.Path + ".png"
        };

        // Sprite 资源的路径模式（用于搜索资源）
        private static readonly string[] SpritePathPatterns = new[]
        {
            "Assets/AAAGame/Sprites/",
            "Assets/Resources/Sprites/",
            "Sprites/"
        };
        
        // 需要检索的动态资源目录前缀（代码中引用的路径）
        private static readonly string[] DynamicPathPrefixes = new[]
        {
            "Common/",
            "NN/",
            "ZJH/",
            "MJGame/",
            "BJ/",
            "PDK/",
            "DZ/",
            "Img/",
            "Avatar/"
        };

        public HashSet<string> AnalyzeAllScripts(Action<float, string> progressCallback = null)
        {
            var dynamicAssets = new HashSet<string>();
            var scriptPaths = new List<string>();

            // 0. 首先扫描已知的动态资源目录
            progressCallback?.Invoke(0f, "扫描已知动态资源目录...");
            var knownDynamicAssets = ScanKnownDynamicDirectories();
            foreach (var asset in knownDynamicAssets)
            {
                dynamicAssets.Add(asset);
            }

            // 1. 收集所有 C# 脚本
            progressCallback?.Invoke(0.1f, "收集所有脚本文件...");
            
            // 同时扫描 Scripts 和 ScriptsBuiltin 目录
            string[] scriptDirectories = new[]
            {
                "Assets/AAAGame/Scripts",
                "Assets/AAAGame/ScriptsBuiltin"
            };
            
            foreach (var scriptDir in scriptDirectories)
            {
                if (!Directory.Exists(scriptDir)) continue;
                
                var scriptGuids = AssetDatabase.FindAssets("t:Script", new[] { scriptDir });
                foreach (var guid in scriptGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".cs") && !scriptPaths.Contains(path))
                    {
                        scriptPaths.Add(path);
                    }
                }
            }

            Debug.Log($"[动态资源分析] 找到 {scriptPaths.Count} 个脚本文件");

            // 2. 分析每个脚本
            for (int i = 0; i < scriptPaths.Count; i++)
            {
                if (i % 50 == 0)
                {
                    float progress = 0.1f + (i / (float)scriptPaths.Count) * 0.8f;
                    progressCallback?.Invoke(progress, $"分析脚本 ({i}/{scriptPaths.Count})...");
                }

                try
                {
                    var foundAssets = AnalyzeScript(scriptPaths[i]);
                    foreach (var asset in foundAssets)
                    {
                        dynamicAssets.Add(asset);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[动态资源分析] 分析脚本失败: {scriptPaths[i]}, 错误: {e.Message}");
                }
            }
            
            // 3. 扫描 DataTable 中的图片路径字段
            progressCallback?.Invoke(0.9f, "扫描 DataTable 配置...");
            var dataTableAssets = ScanDataTableImagePaths();
            foreach (var asset in dataTableAssets)
            {
                dynamicAssets.Add(asset);
            }

            progressCallback?.Invoke(1f, "分析完成");

            Debug.Log($"[动态资源分析] 共发现 {dynamicAssets.Count} 个动态加载的图片资源");

            return dynamicAssets;
        }
        
        /// <summary>
        /// 扫描 DataTable 文本文件中的图片路径
        /// </summary>
        private HashSet<string> ScanDataTableImagePaths()
        {
            var results = new HashSet<string>();
            
            string dataTableDir = "Assets/AAAGame/DataTable";
            if (!Directory.Exists(dataTableDir))
            {
                // 尝试备用路径
                dataTableDir = "AAAGameData/DataTables";
                if (!Directory.Exists(dataTableDir)) return results;
            }
            
            // 查找所有 txt 数据表文件
            var dataTableFiles = Directory.GetFiles(dataTableDir, "*.txt", SearchOption.AllDirectories);
            
            // 匹配图片路径的正则
            var imagePathPattern = new Regex(@"([A-Za-z0-9_/]+\.(?:png|jpg|jpeg|tga))", RegexOptions.IgnoreCase);
            
            foreach (var file in dataTableFiles)
            {
                try
                {
                    string content = File.ReadAllText(file);
                    var matches = imagePathPattern.Matches(content);
                    
                    foreach (Match match in matches)
                    {
                        string imagePath = match.Groups[1].Value;
                        
                        // 尝试解析为完整路径
                        var resolvedPaths = ResolveAssetPath(imagePath);
                        foreach (var path in resolvedPaths)
                        {
                            results.Add(path);
                        }
                    }
                }
                catch { /* 忽略读取错误 */ }
            }
            
            Debug.Log($"[动态资源分析] 从 DataTable 中找到 {results.Count} 个图片路径");
            
            return results;
        }

        private HashSet<string> AnalyzeScript(string scriptPath)
        {
            var foundAssets = new HashSet<string>();

            if (!File.Exists(scriptPath))
                return foundAssets;

            string content = File.ReadAllText(scriptPath);

            // 移除注释
            content = RemoveComments(content);

            // 1. 使用每个模式匹配
            foreach (var pattern in LoadMethods)
            {
                var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        string resourceName = match.Groups[1].Value.Trim();
                        
                        // 处理字符串插值中的变量 (如 Avatar/{index}.png)
                        // 提取目录部分，然后搜索该目录下的所有图片
                        if (resourceName.Contains("{") && resourceName.Contains("}"))
                        {
                            // 替换变量为通配符进行模糊匹配
                            string pattern2 = Regex.Replace(resourceName, @"\{[^}]+\}", "*");
                            var wildcardAssets = ResolveWildcardPath(pattern2);
                            foreach (var asset in wildcardAssets)
                            {
                                foundAssets.Add(asset);
                            }
                        }
                        else
                        {
                            // 尝试解析资源路径
                            var assetPaths = ResolveAssetPath(resourceName);
                            foreach (var assetPath in assetPaths)
                            {
                                foundAssets.Add(assetPath);
                            }
                        }
                    }
                }
            }

            // 2. 检测字符串拼接模式
            foreach (var pattern in ConcatPatterns)
            {
                var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 2)
                    {
                        // 提取前缀和后缀，构建通配符路径
                        string prefix = match.Groups[1].Value.Trim();
                        string suffix = match.Groups[2].Value.Trim();
                        string wildcardPattern = prefix + "*" + suffix;
                        
                        var wildcardAssets = ResolveWildcardPath(wildcardPattern);
                        foreach (var asset in wildcardAssets)
                        {
                            foundAssets.Add(asset);
                        }
                    }
                }
            }

            // 3. 检测字符串变量赋值（如: string spritePath = $"path/{var}.png"）
            foreach (var pattern in VariablePatterns)
            {
                var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        string resourceName = match.Groups[1].Value.Trim();
                        
                        // 处理字符串插值中的变量
                        if (resourceName.Contains("{") && resourceName.Contains("}"))
                        {
                            string pattern2 = Regex.Replace(resourceName, @"\{[^}]+\}", "*");
                            var wildcardAssets = ResolveWildcardPath(pattern2);
                            foreach (var asset in wildcardAssets)
                            {
                                foundAssets.Add(asset);
                            }
                        }
                        // 处理 string.Format 中的占位符
                        else if (resourceName.Contains("{0}"))
                        {
                            string pattern2 = resourceName.Replace("{0}", "*");
                            var wildcardAssets = ResolveWildcardPath(pattern2);
                            foreach (var asset in wildcardAssets)
                            {
                                foundAssets.Add(asset);
                            }
                        }
                        // 处理字符串拼接赋值（如果匹配到 Group 2）
                        else if (match.Groups.Count > 2)
                        {
                            string prefix = match.Groups[1].Value.Trim();
                            string suffix = match.Groups[2].Value.Trim();
                            string wildcardPattern = prefix + "*" + suffix;
                            
                            var wildcardAssets = ResolveWildcardPath(wildcardPattern);
                            foreach (var asset in wildcardAssets)
                            {
                                foundAssets.Add(asset);
                            }
                        }
                        // 解析目录路径前缀
                        else
                        {
                            var wildcardAssets = ResolveWildcardPath(resourceName + "*");
                            foreach (var asset in wildcardAssets)
                            {
                                foundAssets.Add(asset);
                            }
                        }
                    }
                }
            }
            
            // 4. 检测从属性/变量读取路径的模式（如 DataTable 字段）
            foreach (var pattern in PropertyPathPatterns)
            {
                var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                foreach (Match match in matches)
                {
                    // 这些模式匹配的是变量名，无法直接解析路径
                    // 但可以标记这个脚本使用了动态路径
                    // 需要依赖 DataTable 扫描来补充
                }
            }

            return foundAssets;
        }
        
        /// <summary>
        /// 扫描已知的动态资源目录，将整个目录下的资源都标记为动态使用
        /// </summary>
        public HashSet<string> ScanKnownDynamicDirectories()
        {
            var dynamicAssets = new HashSet<string>();
            
            // 已知的动态资源目录（代码中经常动态拼接加载的目录）
            string[] knownDynamicDirs = new[]
            {
                "Assets/AAAGame/Sprites/Common/Cards",       // 扑克牌
                "Assets/AAAGame/Sprites/Common/Cards_max",   // 大尺寸扑克牌
                "Assets/AAAGame/Sprites/Common/TableBG",     // 桌布背景
                "Assets/AAAGame/Sprites/Avatar",             // 头像
                "Assets/AAAGame/Sprites/NN/CardType",        // 牛牛牌型
                "Assets/AAAGame/Sprites/ZJH/Operate",        // 炸金花操作图标
                "Assets/AAAGame/Sprites/ZJH/Paixing",        // 炸金花牌型
                "Assets/AAAGame/Sprites/BJ",                 // 比鸡
                "Assets/AAAGame/Sprites/MJGame/Bgs",         // 麻将桌布
                "Assets/AAAGame/Sprites/MJGame/MJCardAll",   // 麻将牌
                "Assets/AAAGame/Sprites/MJGame/MJGameCommon", // 麻将通用
                "Assets/AAAGame/Sprites/MJGame/KWX",         // 卡五星
                "Assets/AAAGame/Sprites/Img/房间列表资源",     // 房间列表图标
            };
            
            string[] imageExtensions = { "*.png", "*.jpg", "*.jpeg", "*.tga", "*.psd" };
            
            foreach (var dir in knownDynamicDirs)
            {
                if (!Directory.Exists(dir)) continue;
                
                foreach (var ext in imageExtensions)
                {
                    try
                    {
                        var files = Directory.GetFiles(dir, ext, SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            string assetPath = file.Replace("\\", "/");
                            dynamicAssets.Add(assetPath);
                        }
                    }
                    catch { /* 忽略访问错误 */ }
                }
            }
            
            Debug.Log($"[动态资源分析] 扫描已知动态目录，找到 {dynamicAssets.Count} 个资源");
            
            return dynamicAssets;
        }

        /// <summary>
        /// 移除代码中的注释
        /// </summary>
        private string RemoveComments(string code)
        {
            // 移除单行注释
            code = Regex.Replace(code, @"//.*?$", "", RegexOptions.Multiline);
            
            // 移除多行注释
            code = Regex.Replace(code, @"/\*.*?\*/", "", RegexOptions.Singleline);
            
            return code;
        }

        /// <summary>
        /// 解析带通配符的路径（处理字符串插值中的变量）
        /// 例如: Avatar/*.png 会匹配 Avatar 目录下的所有 png 文件
        /// </summary>
        private List<string> ResolveWildcardPath(string wildcardPattern)
        {
            var results = new List<string>();

            // 提取目录和文件名模式
            // 例如: "Avatar/*.png" -> directory: "Avatar", filePattern: "*.png"
            string directory = Path.GetDirectoryName(wildcardPattern)?.Replace("\\", "/") ?? "";
            string filePattern = Path.GetFileName(wildcardPattern);

            // 如果文件名包含通配符，搜索整个目录
            if (filePattern.Contains("*"))
            {
                // 在多个可能的基础目录下搜索
                string[] baseSearchDirs = new[]
                {
                    "Assets/AAAGame/Sprites",
                    "Assets/Resources/Sprites",
                    "Assets/AAAGame/Arts"
                };
                
                foreach (var baseDir in baseSearchDirs)
                {
                    string searchDir = string.IsNullOrEmpty(directory) 
                        ? baseDir 
                        : $"{baseDir}/{directory}".Replace("//", "/");
                    
                    if (Directory.Exists(searchDir))
                    {
                        // 获取扩展名
                        string ext = Path.GetExtension(filePattern).Replace("*", "");
                        if (string.IsNullOrEmpty(ext))
                        {
                            // 如果没有扩展名，搜索所有图片格式
                            string[] extensions = { "*.png", "*.jpg", "*.jpeg", "*.tga", "*.psd" };
                            foreach (var extPattern in extensions)
                            {
                                try
                                {
                                    var files = Directory.GetFiles(searchDir, extPattern, SearchOption.TopDirectoryOnly);
                                    results.AddRange(files.Select(f => f.Replace("\\", "/")));
                                }
                                catch { /* 忽略访问错误 */ }
                            }
                        }
                        else
                        {
                            // 使用指定的扩展名
                            try
                            {
                                var files = Directory.GetFiles(searchDir, "*" + ext, SearchOption.TopDirectoryOnly);
                                results.AddRange(files.Select(f => f.Replace("\\", "/")));
                            }
                            catch { /* 忽略访问错误 */ }
                        }
                    }
                }

                if (results.Count > 0)
                {
                    Debug.Log($"[动态资源分析] 通配符匹配 '{wildcardPattern}' 找到 {results.Count} 个文件");
                }
            }

            return results;
        }

        /// <summary>
        /// 解析资源名称到实际的 Asset 路径
        /// </summary>
        private List<string> ResolveAssetPath(string resourceName)
        {
            var results = new List<string>();

            // 1. 直接是完整路径
            if (resourceName.StartsWith("Assets/"))
            {
                if (File.Exists(resourceName))
                {
                    results.Add(resourceName);
                    return results;
                }
            }

            // 2. 可能的基础目录列表
            string[] baseDirectories = new[]
            {
                "Assets/AAAGame/Sprites",
                "Assets/Resources/Sprites",
                "Assets/AAAGame/Arts"
            };
            
            // 3. 可能的扩展名
            string[] possibleExtensions = { ".png", ".jpg", ".jpeg", ".tga", ".psd", "" };
            
            foreach (var baseDir in baseDirectories)
            {
                foreach (var ext in possibleExtensions)
                {
                    // 尝试不同的路径组合
                    string candidate;
                    
                    // 如果已经有扩展名，不再添加
                    if (HasImageExtension(resourceName))
                    {
                        candidate = $"{baseDir}/{resourceName}".Replace("//", "/");
                    }
                    else
                    {
                        candidate = $"{baseDir}/{resourceName}{ext}".Replace("//", "/");
                    }
                    
                    if (File.Exists(candidate) && !results.Contains(candidate))
                    {
                        results.Add(candidate);
                    }
                }
            }

            // 4. 使用 AssetDatabase 搜索（模糊匹配）
            if (results.Count == 0)
            {
                string fileName = Path.GetFileNameWithoutExtension(resourceName);
                if (!string.IsNullOrEmpty(fileName))
                {
                    // 搜索多个目录
                    string[] searchDirs = new[]
                    {
                        "Assets/AAAGame/Sprites",
                        "Assets/AAAGame/Arts"
                    };
                    
                    foreach (var searchDir in searchDirs)
                    {
                        if (!Directory.Exists(searchDir)) continue;
                        
                        var guids = AssetDatabase.FindAssets($"{fileName} t:Texture2D", new[] { searchDir });
                        
                        foreach (var guid in guids)
                        {
                            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                            string assetFileName = Path.GetFileNameWithoutExtension(assetPath);
                            
                            // 精确匹配或部分匹配
                            if (assetFileName.Equals(fileName, StringComparison.OrdinalIgnoreCase) ||
                                assetPath.Contains(resourceName))
                            {
                                if (!results.Contains(assetPath))
                                {
                                    results.Add(assetPath);
                                }
                            }
                        }
                    }
                }
            }

            return results;
        }
        
        /// <summary>
        /// 检查文件名是否包含图片扩展名
        /// </summary>
        private bool HasImageExtension(string fileName)
        {
            string[] imageExtensions = { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".bmp", ".gif" };
            string lowerName = fileName.ToLower();
            return imageExtensions.Any(ext => lowerName.EndsWith(ext));
        }

        /// <summary>
        /// 生成动态资源分析报告
        /// </summary>
        public void GenerateAnalysisReport(HashSet<string> dynamicAssets, string outputPath)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("# 动态加载资源分析报告");
            sb.AppendLine();
            sb.AppendLine($"**生成时间:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine($"**检测到的动态加载资源数量:** {dynamicAssets.Count}");
            sb.AppendLine();

            sb.AppendLine("## 检测方法");
            sb.AppendLine();
            sb.AppendLine("自动扫描以下方法调用:");
            sb.AppendLine("- `image.SetSprite(\"name\")`");
            sb.AppendLine("- `GF.UI.LoadSprite(\"name\")`");
            sb.AppendLine("- `rawImage.SetTexture(\"name\")`");
            sb.AppendLine("- `GF.UI.LoadTexture(\"name\")`");
            sb.AppendLine("- `GF.UI.LoadSpriteAtlas(\"name\")`");
            sb.AppendLine("- `Resources.Load<Sprite>(\"name\")`");
            sb.AppendLine();

            sb.AppendLine("## 动态加载资源列表");
            sb.AppendLine();
            sb.AppendLine("```");
            foreach (var asset in dynamicAssets.OrderBy(a => a))
            {
                sb.AppendLine(asset);
            }
            sb.AppendLine("```");
            sb.AppendLine();

            sb.AppendLine("## 说明");
            sb.AppendLine();
            sb.AppendLine("这些资源在代码中通过字符串路径动态加载，静态依赖分析无法检测到。");
            sb.AppendLine("资源清理工具会自动排除这些资源，避免误删。");
            sb.AppendLine();

            File.WriteAllText(outputPath, sb.ToString(), System.Text.Encoding.UTF8);
            Debug.Log($"[动态资源分析] 报告已生成: {outputPath}");
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Editor.ResourceCleanup
{
    /// <summary>
    /// 清理报告生成器 - 生成详细的 Markdown 报告
    /// </summary>
    public class CleanupReportGenerator
    {
        public string GenerateReport(CleanupResult result, CleanupConfig config)
        {
            string reportDir = "Assets/Editor/ResourceCleanup/Reports";
            if (!Directory.Exists(reportDir))
            {
                Directory.CreateDirectory(reportDir);
            }

            // 删除旧的报告文件（只保留最新的）
            if (Directory.Exists(reportDir))
            {
                var oldReports = Directory.GetFiles(reportDir, "CleanupReport_*.md");
                foreach (var oldReport in oldReports)
                {
                    try
                    {
                        File.Delete(oldReport);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[资源清理] 删除旧报告失败: {oldReport}, 错误: {e.Message}");
                    }
                }
            }

            string timestamp = result.scanTime.ToString("yyyyMMdd_HHmmss");
            string reportPath = Path.Combine(reportDir, $"CleanupReport_{timestamp}.md");

            var sb = new StringBuilder();

            // 标题和概览
            sb.AppendLine("# Unity 资源清理报告");
            sb.AppendLine();
            sb.AppendLine($"**生成时间:** {result.scanTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            // 摘要
            sb.AppendLine("## 📊 扫描摘要");
            sb.AppendLine();
            sb.AppendLine("| 项目 | 数量 |");
            sb.AppendLine("|------|------|");
            sb.AppendLine($"| 总资源数 | {result.totalAssets} |");
            sb.AppendLine($"| 被引用资源 | {result.usedAssets.Count} |");
            sb.AppendLine($"| **未使用资源** | **{result.unusedAssets.Count}** |");
            sb.AppendLine($"| 动态声明资源 | {result.declaredDynamicAssets.Count} |");
            sb.AppendLine();

            // 配置信息
            sb.AppendLine("## ⚙️ 扫描配置");
            sb.AppendLine();
            sb.AppendLine("### 扫描目录");
            foreach (var dir in config.scanDirectories)
            {
                sb.AppendLine($"- `{dir}`");
            }
            sb.AppendLine();

            sb.AppendLine("### 排除目录");
            foreach (var dir in config.excludeDirectories)
            {
                sb.AppendLine($"- `{dir}`");
            }
            sb.AppendLine();

            sb.AppendLine("### 排除扩展名");
            sb.AppendLine($"`{string.Join("`, `", config.excludeExtensions)}`");
            sb.AppendLine();

            // 未使用资源详情
            if (result.unusedAssets.Count > 0)
            {
                sb.AppendLine("## 🗑️ 未使用资源列表");
                sb.AppendLine();
                sb.AppendLine($"共 **{result.unusedAssets.Count}** 个资源未被引用。");
                sb.AppendLine();

                // 按类型分组统计
                var groupedByType = result.unusedAssets
                    .GroupBy(path => Path.GetExtension(path).ToLower())
                    .OrderByDescending(g => g.Count())
                    .ToList();

                sb.AppendLine("### 按文件类型统计");
                sb.AppendLine();
                sb.AppendLine("| 文件类型 | 数量 | 占比 |");
                sb.AppendLine("|----------|------|------|");
                foreach (var group in groupedByType)
                {
                    string ext = string.IsNullOrEmpty(group.Key) ? "(无扩展名)" : group.Key;
                    double percentage = (group.Count() / (double)result.unusedAssets.Count) * 100;
                    sb.AppendLine($"| `{ext}` | {group.Count()} | {percentage:F1}% |");
                }
                sb.AppendLine();

                // 按目录分组统计
                var groupedByDir = result.unusedAssets
                    .GroupBy(path =>
                    {
                        string dir = Path.GetDirectoryName(path).Replace("\\", "/");
                        // 只取前两级目录
                        var parts = dir.Split('/');
                        return parts.Length > 2 ? string.Join("/", parts.Take(3)) : dir;
                    })
                    .OrderByDescending(g => g.Count())
                    .Take(20)
                    .ToList();

                sb.AppendLine("### 按目录统计 (Top 20)");
                sb.AppendLine();
                sb.AppendLine("| 目录 | 资源数 |");
                sb.AppendLine("|------|--------|");
                foreach (var group in groupedByDir)
                {
                    sb.AppendLine($"| `{group.Key}` | {group.Count()} |");
                }
                sb.AppendLine();

                // 详细列表
                sb.AppendLine("### 完整列表");
                sb.AppendLine();
                sb.AppendLine("```");
                foreach (var asset in result.unusedAssets.OrderBy(a => a))
                {
                    sb.AppendLine(asset);
                }
                sb.AppendLine("```");
                sb.AppendLine();
            }

            // 动态声明资源
            if (result.declaredDynamicAssets.Count > 0)
            {
                sb.AppendLine("## 🔄 动态加载资源");
                sb.AppendLine();
                sb.AppendLine($"通过代码分析和手动声明，共识别出 **{result.declaredDynamicAssets.Count}** 个动态加载的资源。");
                sb.AppendLine();
                sb.AppendLine("### 检测方法");
                sb.AppendLine();
                sb.AppendLine("自动扫描以下代码模式:");
                sb.AppendLine("- 字符串插值: `image.SetSprite($\"MJGame/Bgs/{index}.png\")`");
                sb.AppendLine("- 字符串拼接: `\"Common/Cards/\" + color + value + \".png\"`");
                sb.AppendLine("- 格式化字符串: `string.Format(\"Img/{0}.png\", name)`");
                sb.AppendLine("- 直接加载: `GF.UI.LoadSprite(\"name\")`");
                sb.AppendLine("- 扩展方法: `image.SetSprite(\"name\")`");
                sb.AppendLine("- Resources: `Resources.Load<Sprite>(\"name\")`");
                sb.AppendLine("- 路径辅助: `AssetsPath.GetSpritesPath(\"path\")`");
                sb.AppendLine("- 手动声明: `DynamicAssetDeclaration` ScriptableObject");
                sb.AppendLine();
                sb.AppendLine("自动扫描已知的动态资源目录:");
                sb.AppendLine("- `Common/Cards`, `Common/Cards_max`, `Common/TableBG`");
                sb.AppendLine("- `Avatar`, `NN/CardType`, `ZJH/Operate`, `ZJH/Paixing`");
                sb.AppendLine("- `MJGame/Bgs`, `MJGame/MJCardAll`, `MJGame/KWX`");
                sb.AppendLine("- `Img/房间列表资源` 等");
                sb.AppendLine();
                sb.AppendLine("自动扫描 DataTable 配置:");
                sb.AppendLine("- 扫描 `.txt` 数据表文件中的图片路径字段");
                sb.AppendLine();
                sb.AppendLine("> 📌 自动识别变量拼接，将整个目录标记为动态资源");
                sb.AppendLine();
                sb.AppendLine("### 资源列表");
                sb.AppendLine();
                sb.AppendLine("```");
                foreach (var asset in result.declaredDynamicAssets.OrderBy(a => a))
                {
                    sb.AppendLine(asset);
                }
                sb.AppendLine("```");
                sb.AppendLine();
                sb.AppendLine("> 💡 这些资源已自动排除清理，无需担心误删。");
                sb.AppendLine();
            }

            // 建议和注意事项
            sb.AppendLine("## 💡 清理建议");
            sb.AppendLine();
            sb.AppendLine("### ⚠️ 清理前检查");
            sb.AppendLine();
            sb.AppendLine("1. **动态加载资源**");
            sb.AppendLine("   - 通过 `Resources.Load()` 加载的资源");
            sb.AppendLine("   - Addressables 动态加载的资源");
            sb.AppendLine("   - AssetBundle 中的资源");
            sb.AppendLine("   - 使用字符串路径加载的资源");
            sb.AppendLine();
            sb.AppendLine("2. **编辑器专用资源**");
            sb.AppendLine("   - Editor 目录下的测试资源");
            sb.AppendLine("   - Gizmos 图标资源");
            sb.AppendLine();
            sb.AppendLine("3. **配置引用资源**");
            sb.AppendLine("   - 在 Inspector 中配置但未实例化的资源");
            sb.AppendLine("   - ScriptableObject 中引用的资源");
            sb.AppendLine();

            sb.AppendLine("### ✅ 清理步骤");
            sb.AppendLine();
            sb.AppendLine("1. 仔细审查未使用资源列表");
            sb.AppendLine("2. 将确认不需要的资源移至 `UnusedAssets` 目录");
            sb.AppendLine("3. 完整测试项目所有功能");
            sb.AppendLine("4. 保留 1-2 周观察期");
            sb.AppendLine("5. 确认无问题后再永久删除");
            sb.AppendLine();

            sb.AppendLine("### 🛡️ 安全措施");
            sb.AppendLine();
            sb.AppendLine("- ✅ 使用版本控制（Git）管理清理前后的变化");
            sb.AppendLine("- ✅ 清理后立即提交，便于回滚");
            sb.AppendLine("- ✅ 在测试分支进行清理，验证通过后再合并");
            sb.AppendLine("- ✅ 保留清理报告，记录所有被移动的资源");
            sb.AppendLine();

            // 写入文件
            File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8);

            Debug.Log($"[资源清理] 报告已生成: {reportPath}");

            return reportPath;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Editor.ResourceCleanup
{
    /// <summary>
    /// 动态资源声明 ScriptableObject
    /// 用于标记运行时动态加载的资源，防止被错误清理
    /// 
    /// 使用方法:
    /// 1. 在 Project 窗口右键 -> Create -> Resource Cleanup -> Dynamic Asset Declaration
    /// 2. 将运行时动态加载的资源拖入 Dynamic Assets 列表
    /// 3. 清理工具会自动识别这些资源为"被使用"状态
    /// </summary>
    [CreateAssetMenu(fileName = "DynamicAssetDeclaration", menuName = "Resource Cleanup/Dynamic Asset Declaration", order = 1)]
    public class DynamicAssetDeclaration : ScriptableObject
    {
        [Header("动态加载的资源列表")]
        [Tooltip("将运行时通过 Resources.Load、Addressables 或其他方式动态加载的资源拖入此列表")]
        public List<Object> dynamicAssets = new List<Object>();

        [Header("说明")]
        [TextArea(3, 10)]
        public string description = "此配置用于声明运行时动态加载的资源\n" +
                                   "包括:\n" +
                                   "- Resources.Load 加载的资源\n" +
                                   "- Addressables 动态加载的资源\n" +
                                   "- AssetBundle 中的资源\n" +
                                   "- 通过反射或字符串路径加载的资源\n\n" +
                                   "这些资源在静态分析时无法被检测到引用关系，需要手动声明。";

        [Header("分类标签（可选）")]
        [Tooltip("用于组织管理，如: UI动态图集、特效资源、音频资源等")]
        public string category = "未分类";

        private void OnValidate()
        {
            // 移除空引用
            if (dynamicAssets != null)
            {
                dynamicAssets.RemoveAll(asset => asset == null);
            }
        }
    }
}

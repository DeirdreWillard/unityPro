using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class FindScriptInPrefabs : MonoBehaviour
{
    [MenuItem("Assets/查找预制体中的脚本引用", false, 2000)]
    private static void FindScriptUsageInPrefabs()
    {
        // 获取当前选中的脚本
        MonoScript selectedScript = Selection.activeObject as MonoScript;
        if (selectedScript == null)
        {
            EditorUtility.DisplayDialog("错误", "请选择一个 C# 脚本。", "确定");
            return;
        }

        // 获取脚本的类名
        string scriptClassName = selectedScript.GetClass()?.Name;
        if (string.IsNullOrEmpty(scriptClassName))
        {
            EditorUtility.DisplayDialog("错误", "在选中的脚本中找不到有效的类。", "确定");
            return;
        }

        // 搜索所有的预制体
        string[] allPrefabs = Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories);
        List<string> foundPrefabs = new List<string>();

        foreach (string prefabPath in allPrefabs)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) continue;

            // 检查预制体中是否有使用该脚本
            Component[] components = prefab.GetComponentsInChildren<Component>(true);
            foreach (Component component in components)
            {
                if (component == null) continue; // 可能会遇到空组件
                if (component.GetType().Name == scriptClassName)
                {
                    foundPrefabs.Add(prefabPath);
                    break;
                }
            }
        }

        // 输出结果
        if (foundPrefabs.Count > 0)
        {
            string result = "该脚本在以下预制体中被引用：\n" + string.Join("\n", foundPrefabs);
            EditorUtility.DisplayDialog("搜索结果", result, "确定");
            foreach (string path in foundPrefabs)
            {
                Debug.Log($"预制体中发现脚本引用: {path}", AssetDatabase.LoadAssetAtPath<Object>(path));
            }
        }
        else
        {
            EditorUtility.DisplayDialog("搜索结果", "该脚本未在任何预制体中被引用。", "确定");
        }
    }

    [MenuItem("Assets/查找预制体中的脚本引用", true)]
    private static bool ValidateFindScriptUsageInPrefabs()
    {
        // 确保右键菜单项只在选中脚本时可用
        return Selection.activeObject is MonoScript;
    }
}

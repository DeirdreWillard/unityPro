using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEditor.Events;

public class ReplaceFontInPrefabs : EditorWindow
{
    private enum ReplaceMode
    {
        OnlyMatchSource, // 只替换指定待替换字体
        ReplaceAll      // 全部替换
    }

    [Header("要查找的文件夹（Assets/开头）")]
    [SerializeField] private string m_FolderPath = "Assets/YourFolder";
    [SerializeField] private DefaultAsset m_FolderAsset;
    [Header("只处理一级目录（不递归子文件夹）")]
    [SerializeField] private bool m_OnlyTopLevel = false;
    [Header("待替换字体（只替换为此字体的组件）")]
    [SerializeField] private Font m_SourceFont;
    [Header("替换字体（UGUI Text）")]
    [SerializeField] private Font m_TargetFont;
    [Header("待替换TMP字体（只替换为此TMP字体的组件）")]
    [SerializeField] private TMP_FontAsset m_SourceTMPFont;
    [Header("替换TMP字体（TextMeshProUGUI）")]
    [SerializeField] private TMP_FontAsset m_TargetTMPFont;
    [Header("替换模式")]
    [SerializeField] private ReplaceMode m_ReplaceMode = ReplaceMode.OnlyMatchSource;

    [MenuItem("Tools/批量替换预制体字体")]
    private static void ShowWindow()
    {
        GetWindow<ReplaceFontInPrefabs>("批量替换预制体字体");
    }

    private void OnGUI()
    {
        GUILayout.Label("批量替换预制体字体", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        m_FolderPath = EditorGUILayout.TextField("文件夹路径", m_FolderPath);
        m_FolderAsset = (DefaultAsset)EditorGUILayout.ObjectField(m_FolderAsset, typeof(DefaultAsset), false, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
        // 拖拽文件夹后自动更新路径
        if (m_FolderAsset != null)
        {
            string assetPath = AssetDatabase.GetAssetPath(m_FolderAsset);
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                m_FolderPath = assetPath;
            }
        }
    m_SourceFont = (Font)EditorGUILayout.ObjectField("待替换字体（留空 = 替换丢失引用）", m_SourceFont, typeof(Font), false);
    m_TargetFont = (Font)EditorGUILayout.ObjectField("替换字体", m_TargetFont, typeof(Font), false);
    m_SourceTMPFont = (TMP_FontAsset)EditorGUILayout.ObjectField("待替换TMP字体（留空 = 替换丢失引用）", m_SourceTMPFont, typeof(TMP_FontAsset), false);
    m_TargetTMPFont = (TMP_FontAsset)EditorGUILayout.ObjectField("替换TMP字体", m_TargetTMPFont, typeof(TMP_FontAsset), false);
        m_ReplaceMode = (ReplaceMode)EditorGUILayout.EnumPopup("替换模式", m_ReplaceMode);
        m_OnlyTopLevel = EditorGUILayout.Toggle("只处理一级目录", m_OnlyTopLevel);

        if (GUILayout.Button("开始替换"))
        {
            ReplaceFontsInPrefabs();
        }

        GUILayout.Space(10);
        GUILayout.Label("查找丢失引用的预制体", EditorStyles.boldLabel);
        if (GUILayout.Button("查找Missing内容"))
        {
            FindMissingInPrefabs(m_FolderPath);
        }
    }

    private void ReplaceFontsInPrefabs()
    {
        // 1. 查找 prefab 和 scene 文件
        string fullPath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - 6), m_FolderPath); // 去掉"Assets"后拼接
        if (!Directory.Exists(fullPath))
        {
            Debug.LogError($"目录不存在: {fullPath}");
            return;
        }
        SearchOption searchOption = m_OnlyTopLevel ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
        string[] prefabFiles = Directory.GetFiles(fullPath, "*.prefab", searchOption);
        string[] sceneFiles = Directory.GetFiles(fullPath, "*.unity", searchOption);

        int replacePrefabCount = 0;
        int replaceSceneCount = 0;

        // 2. 处理Prefab
        foreach (string prefabFile in prefabFiles)
        {
            string assetPath = prefabFile.Replace("\\", "/");
            int assetsIndex = assetPath.IndexOf("Assets/");
            if (assetsIndex >= 0)
                assetPath = assetPath.Substring(assetsIndex);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) continue;
            bool changed = false;

            // 处理UGUI Text
            Text[] texts = prefab.GetComponentsInChildren<Text>(true);
            foreach (var text in texts)
            {
                bool shouldReplace = false;
                if (m_ReplaceMode == ReplaceMode.ReplaceAll)
                {
                    shouldReplace = true;
                }
                else if (m_ReplaceMode == ReplaceMode.OnlyMatchSource)
                {
                    // 如果未指定源字体，则视为替换丢失引用（null）的字体
                    if (m_SourceFont == null)
                    {
                        if (text.font == null)
                            shouldReplace = true;
                    }
                    else if (text.font == m_SourceFont)
                    {
                        shouldReplace = true;
                    }
                }
                if (shouldReplace)
                {
                    Undo.RecordObject(text, "Replace Font");
                    var oldFont = text.font;
                    text.font = m_TargetFont;
                    changed = true;
                    string nodePath = GetGameObjectPath(text.gameObject);
                    string oldName = oldFont == null ? "(Missing)" : oldFont.name;
                    string newName = m_TargetFont == null ? "(Null)" : m_TargetFont.name;
                    Debug.Log($"[Replace] 资源: {assetPath} 节点: {nodePath} 组件: Text 字段: font 从: {oldName} -> {newName}");
                }
            }

            // 处理TextMeshProUGUI
            TextMeshProUGUI[] tmps = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                bool shouldReplace = false;
                if (m_ReplaceMode == ReplaceMode.ReplaceAll)
                {
                    shouldReplace = true;
                }
                else if (m_ReplaceMode == ReplaceMode.OnlyMatchSource)
                {
                    // 如果未指定源TMP字体，则视为替换丢失引用（null）的TMP字体
                    if (m_SourceTMPFont == null)
                    {
                        if (tmp.font == null)
                            shouldReplace = true;
                    }
                    else if (tmp.font == m_SourceTMPFont)
                    {
                        shouldReplace = true;
                    }
                }
                if (shouldReplace)
                {
                    Undo.RecordObject(tmp, "Replace TMP Font");
                    var oldTmp = tmp.font;
                    tmp.font = m_TargetTMPFont;
                    changed = true;
                    string nodePathTmp = GetGameObjectPath(tmp.gameObject);
                    string oldTmpName = oldTmp == null ? "(Missing)" : oldTmp.name;
                    string newTmpName = m_TargetTMPFont == null ? "(Null)" : m_TargetTMPFont.name;
                    Debug.Log($"[Replace] 资源: {assetPath} 节点: {nodePathTmp} 组件: TextMeshProUGUI 字段: font 从: {oldTmpName} -> {newTmpName}");
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(prefab);
                PrefabUtility.SavePrefabAsset(prefab);
                replacePrefabCount++;
                Debug.Log($"已替换Prefab：{assetPath}");
            }
        }

        // 3. 处理Scene
        foreach (string sceneFile in sceneFiles)
        {
            string assetPath = sceneFile.Replace("\\", "/");
            int assetsIndex = assetPath.IndexOf("Assets/");
            if (assetsIndex >= 0)
                assetPath = assetPath.Substring(assetsIndex);

            var scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);
            bool changed = false;
            foreach (GameObject rootObj in scene.GetRootGameObjects())
            {
                // 处理UGUI Text
                Text[] texts = rootObj.GetComponentsInChildren<Text>(true);
                foreach (var text in texts)
                {
                    bool shouldReplace = false;
                    if (m_ReplaceMode == ReplaceMode.ReplaceAll)
                    {
                        shouldReplace = true;
                    }
                    else if (m_ReplaceMode == ReplaceMode.OnlyMatchSource)
                    {
                        if (m_SourceFont == null)
                        {
                            if (text.font == null)
                                shouldReplace = true;
                        }
                        else if (text.font == m_SourceFont)
                        {
                            shouldReplace = true;
                        }
                    }
                    if (shouldReplace)
                    {
                        Undo.RecordObject(text, "Replace Font");
                        var oldFont = text.font;
                        text.font = m_TargetFont;
                        changed = true;
                        string nodePath = GetGameObjectPath(text.gameObject);
                        string oldName = oldFont == null ? "(Missing)" : oldFont.name;
                        string newName = m_TargetFont == null ? "(Null)" : m_TargetFont.name;
                        Debug.Log($"[Replace] Scene: {assetPath} 节点: {nodePath} 组件: Text 字段: font 从: {oldName} -> {newName}");
                    }
                }
                // 处理TextMeshProUGUI
                TextMeshProUGUI[] tmps = rootObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmps)
                {
                    bool shouldReplace = false;
                    if (m_ReplaceMode == ReplaceMode.ReplaceAll)
                    {
                        shouldReplace = true;
                    }
                    else if (m_ReplaceMode == ReplaceMode.OnlyMatchSource)
                    {
                        if (m_SourceTMPFont == null)
                        {
                            if (tmp.font == null)
                                shouldReplace = true;
                        }
                        else if (tmp.font == m_SourceTMPFont)
                        {
                            shouldReplace = true;
                        }
                    }
                    if (shouldReplace)
                    {
                        Undo.RecordObject(tmp, "Replace TMP Font");
                            var oldTmp = tmp.font;
                            tmp.font = m_TargetTMPFont;
                            changed = true;
                            string nodePathTmp = GetGameObjectPath(tmp.gameObject);
                            string oldTmpName = oldTmp == null ? "(Missing)" : oldTmp.name;
                            string newTmpName = m_TargetTMPFont == null ? "(Null)" : m_TargetTMPFont.name;
                            Debug.Log($"[Replace] Scene: {assetPath} 节点: {nodePathTmp} 组件: TextMeshProUGUI 字段: font 从: {oldTmpName} -> {newTmpName}");
                    }
                }
            }
            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                replaceSceneCount++;
                Debug.Log($"已替换Scene：{assetPath}");
            }
            EditorSceneManager.CloseScene(scene, true);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("替换完成", $"共处理{replacePrefabCount}个预制体，{replaceSceneCount}个场景。", "确定");
    }

    private void FindMissingInPrefabs(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        int missingCount = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            // 逐节点检查，便于定位具体节点路径
            Transform[] allTransforms = prefab.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                GameObject go = t.gameObject;
                string goPath = GetGameObjectPath(go);

                Component[] comps = go.GetComponents<Component>();
                for (int ci = 0; ci < comps.Length; ci++)
                {
                    Component comp = comps[ci];
                    if (comp == null)
                    {
                        Debug.LogError($"[Missing Component] 预制体: {path} 节点: {goPath} 存在丢失的组件槽位 (index {ci})", prefab);
                        missingCount++;
                        continue;
                    }

                    // 检查 Button OnClick 丢失
                    if (comp is Button button)
                    {
                        var onClick = button.onClick;
                        var eventCount = onClick.GetPersistentEventCount();
                        for (int i = 0; i < eventCount; i++)
                        {
                            var target = onClick.GetPersistentTarget(i);
                            var method = onClick.GetPersistentMethodName(i);
                            if (target == null)
                            {
                                Debug.LogError($"[Missing Button OnClick Target] 预制体: {path} 节点: {goPath} Button: {button.name} 第{i + 1}个回调目标丢失", prefab);
                                missingCount++;
                            }
                            else if (string.IsNullOrEmpty(method))
                            {
                                Debug.LogError($"[Missing Button OnClick Method] 预制体: {path} 节点: {goPath} Button: {button.name} 第{i + 1}个回调方法丢失", prefab);
                                missingCount++;
                            }
                        }
                    }

                    // 检查字段是否有丢失引用
                    SerializedObject so = new SerializedObject(comp);
                    SerializedProperty prop = so.GetIterator();
                    while (prop.NextVisible(true))
                    {
                        if (prop.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            if (prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
                            {
                                Debug.LogError($"[Missing Reference] 预制体: {path} 节点: {goPath} 组件: {comp.GetType().Name} 字段: {prop.displayName} 丢失引用", prefab);
                                missingCount++;
                            }
                        }
                    }
                }
            }
        }
        Debug.Log($"查找完成，共发现 {missingCount} 处丢失引用。");
    }

    // 获取 GameObject 在层级中的完整路径（从根到自身）
    private string GetGameObjectPath(GameObject go)
    {
        if (go == null) return "(null)";
        string path = go.name;
        Transform t = go.transform;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
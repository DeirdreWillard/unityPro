using UnityEditor;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.IO;

public class FindImageUsageContextMenu : EditorWindow
{
    private Dictionary<string, List<string>> groupedResults = new Dictionary<string, List<string>>();
    private Vector2 scrollPos;
    private bool isSearching = false;
    private Queue<string> imagesToSearch = new Queue<string>();
    private string currentImage;
    private string[] allPrefabs;

    [MenuItem("Assets/查找引用", true)]
    private static bool ValidateFindReferences()
    {
        // 验证选中的资源是否为文件夹、贴图、精灵、材质、字体（包括 TMP 字体）或着色器
        Object selected = Selection.activeObject;
        if (selected == null) return false;

        string path = AssetDatabase.GetAssetPath(selected);
        return AssetDatabase.IsValidFolder(path)
            || selected is Texture2D
            || selected is Sprite
            || selected is Material
            || selected is Font
            || selected is TMP_FontAsset
            || selected is Shader;
    }

    [MenuItem("Assets/查找引用")]
    private static void FindReferencesContextMenu()
    {
        Object[] selectedObjects = Selection.objects;
        if (selectedObjects == null || selectedObjects.Length == 0) return;

        // 如果只选中了一个对象，使用原有逻辑
        if (selectedObjects.Length == 1)
        {
            Object selected = selectedObjects[0];
            string path = AssetDatabase.GetAssetPath(selected);

            if (AssetDatabase.IsValidFolder(path))
            {
                // 选中了文件夹
                OpenWindowForFolder(path);
            }
            else if (selected is Texture2D || selected is Sprite)
            {
                // 选中了单个图片
                OpenWindowForSingleImage(path);
            }
            else if (selected is Material)
            {
                // 选中了材质
                OpenWindowForSingleMaterial(path);
            }
            else if (selected is Font || selected is TMP_FontAsset)
            {
                // 选中了字体（常规或 TMP）
                OpenWindowForSingleFont(path);
            }
            else if (selected is Shader)
            {
                // 选中了着色器
                OpenWindowForSingleShader(path);
            }
        }
        else
        {
            // 选中了多个资源
            OpenWindowForMultipleAssets(selectedObjects);
        }
    }

    private static void OpenWindowForFolder(string folderPath)
    {
        var window = GetWindow<FindImageUsageContextMenu>("查找文件夹引用");
        window.StartSearchForFolder(folderPath);
    }

    private static void OpenWindowForSingleImage(string imagePath)
    {
        var window = GetWindow<FindImageUsageContextMenu>("查找图片引用");
        window.StartSearchForSingleImage(imagePath);
    }

    private static void OpenWindowForSingleMaterial(string materialPath)
    {
        var window = GetWindow<FindImageUsageContextMenu>("查找材质引用");
        window.StartSearchForSingleMaterial(materialPath);
    }

    private static void OpenWindowForSingleFont(string fontPath)
    {
        var window = GetWindow<FindImageUsageContextMenu>("查找字体引用");
        window.StartSearchForSingleFont(fontPath);
    }

    private static void OpenWindowForSingleShader(string shaderPath)
    {
        var window = GetWindow<FindImageUsageContextMenu>("查找着色器引用");
        window.StartSearchForSingleShader(shaderPath);
    }

    private static void OpenWindowForMultipleAssets(Object[] assets)
    {
        var window = GetWindow<FindImageUsageContextMenu>("查找引用");
        window.StartSearchForMultipleAssets(assets);
    }

    private void StartSearchForFolder(string folderPath)
    {
        groupedResults.Clear();
        imagesToSearch.Clear();
        currentImage = null;

        // Collect all images in the folder
        string[] allImageGUIDs = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        foreach (string imageGUID in allImageGUIDs)
        {
            string imagePath = AssetDatabase.GUIDToAssetPath(imageGUID);
            imagesToSearch.Enqueue(imagePath);
        }

        // Collect fonts in the folder (regular Font)
        string[] allFontGUIDs = AssetDatabase.FindAssets("t:Font", new[] { folderPath });
        foreach (string fontGUID in allFontGUIDs)
        {
            string fontPath = AssetDatabase.GUIDToAssetPath(fontGUID);
            imagesToSearch.Enqueue(fontPath);
        }

        // Collect TMP font assets if present
        string[] allTMPFontGUIDs = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { folderPath });
        foreach (string tmpGuid in allTMPFontGUIDs)
        {
            string tmpPath = AssetDatabase.GUIDToAssetPath(tmpGuid);
            imagesToSearch.Enqueue(tmpPath);
        }

        // Collect shaders in the folder
        string[] allShaderGUIDs = AssetDatabase.FindAssets("t:Shader", new[] { folderPath });
        foreach (string shaderGUID in allShaderGUIDs)
        {
            string shaderPath = AssetDatabase.GUIDToAssetPath(shaderGUID);
            imagesToSearch.Enqueue(shaderPath);
        }

        PrepareForSearch();
    }

    private void StartSearchForSingleImage(string imagePath)
    {
        groupedResults.Clear();
        imagesToSearch.Clear();
        currentImage = null;

        // Enqueue the single image for searching
        imagesToSearch.Enqueue(imagePath);

        PrepareForSearch();
    }

    private void StartSearchForSingleMaterial(string materialPath)
    {
        groupedResults.Clear();
        imagesToSearch.Clear();
        currentImage = null;

        // Enqueue the single material for searching
        imagesToSearch.Enqueue(materialPath);

        PrepareForSearch();
    }

    private void StartSearchForSingleFont(string fontPath)
    {
        groupedResults.Clear();
        imagesToSearch.Clear();
        currentImage = null;

        // Enqueue the single font (or TMP font asset) for searching
        imagesToSearch.Enqueue(fontPath);

        PrepareForSearch();
    }

    private void StartSearchForSingleShader(string shaderPath)
    {
        groupedResults.Clear();
        imagesToSearch.Clear();
        currentImage = null;

        // Enqueue the single shader for searching
        imagesToSearch.Enqueue(shaderPath);

        PrepareForSearch();
    }

    private void StartSearchForMultipleAssets(Object[] assets)
    {
        groupedResults.Clear();
        imagesToSearch.Clear();
        currentImage = null;

        // Enqueue all selected assets for searching
        foreach (Object asset in assets)
        {
            if (asset == null) continue;

            string path = AssetDatabase.GetAssetPath(asset);

            // Skip folders
            if (AssetDatabase.IsValidFolder(path))
            {
                // If folder is selected, add all assets within it
                string[] folderImageGUIDs = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
                foreach (string imageGUID in folderImageGUIDs)
                {
                    imagesToSearch.Enqueue(AssetDatabase.GUIDToAssetPath(imageGUID));
                }

                string[] folderFontGUIDs = AssetDatabase.FindAssets("t:Font", new[] { path });
                foreach (string fontGUID in folderFontGUIDs)
                {
                    imagesToSearch.Enqueue(AssetDatabase.GUIDToAssetPath(fontGUID));
                }

                string[] folderTMPFontGUIDs = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { path });
                foreach (string tmpGuid in folderTMPFontGUIDs)
                {
                    imagesToSearch.Enqueue(AssetDatabase.GUIDToAssetPath(tmpGuid));
                }

                string[] folderShaderGUIDs = AssetDatabase.FindAssets("t:Shader", new[] { path });
                foreach (string shaderGUID in folderShaderGUIDs)
                {
                    imagesToSearch.Enqueue(AssetDatabase.GUIDToAssetPath(shaderGUID));
                }
            }
            else if (asset is Texture2D || asset is Sprite || asset is Material || 
                     asset is Font || asset is TMP_FontAsset || asset is Shader)
            {
                // Add individual assets
                imagesToSearch.Enqueue(path);
            }
        }

        PrepareForSearch();
    }

    private void PrepareForSearch()
    {
        // Collect all prefabs in the project
        allPrefabs = AssetDatabase.FindAssets("t:Prefab");

        if (imagesToSearch.Count > 0)
        {
            isSearching = true;
            EditorApplication.update += PerformSearchStep; // Start asynchronous search
        }
    }

    private void PerformSearchStep()
    {
        if (imagesToSearch.Count > 0)
        {
            currentImage = imagesToSearch.Dequeue();
            Object image = AssetDatabase.LoadAssetAtPath<Object>(currentImage);

            if (image != null)
            {
                foreach (string prefabGUID in allPrefabs)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                    string nodePath = FindReferenceInPrefab(prefab, currentImage);
                    if (!string.IsNullOrEmpty(nodePath))
                    {
                        if (!groupedResults.ContainsKey(currentImage))
                        {
                            groupedResults[currentImage] = new List<string>();
                        }
                        groupedResults[currentImage].Add(prefabPath + " -> " + nodePath);
                    }
                }
            }
        }
        else
        {
            // 搜索完成
            isSearching = false;
            currentImage = null;
            EditorApplication.update -= PerformSearchStep; // 停止异步搜索
            Debug.Log("搜索完成。");
        }
    }

    // Returns the hierarchy path of the first component in the prefab that references assetPath, or null if none
    private string FindReferenceInPrefab(GameObject prefab, string assetPath)
    {
        Component[] components = prefab.GetComponentsInChildren<Component>(true);

        foreach (Component component in components)
        {
            if (component == null) continue;

            // Helper to build path
            string path = GetHierarchyPath((component as Component).gameObject, prefab);

            // Check for SpriteRenderer
            if (component is SpriteRenderer spriteRenderer && spriteRenderer.sprite != null)
            {
                string spritePath = AssetDatabase.GetAssetPath(spriteRenderer.sprite);
                if (spritePath == assetPath)
                {
                    return path;
                }
            }

            // Check for UI Image
            if (component is UnityEngine.UI.Image uiImage)
            {
                // Check sprite reference
                if (uiImage.sprite != null)
                {
                    string spritePath = AssetDatabase.GetAssetPath(uiImage.sprite);
                    if (spritePath == assetPath)
                    {
                        return path;
                    }
                }

                // Check material reference
                if (uiImage.material != null)
                {
                    string materialPath = AssetDatabase.GetAssetPath(uiImage.material);
                    if (materialPath == assetPath)
                    {
                        return path;
                    }

                    // Check shader reference in the UI Image material
                    if (uiImage.material.shader != null)
                    {
                        string shaderPath = AssetDatabase.GetAssetPath(uiImage.material.shader);
                        if (shaderPath == assetPath)
                        {
                            return path;
                        }
                    }

                    // Check textures in the UI Image material
                    foreach (var texture in uiImage.material.GetTexturePropertyNames())
                    {
                        Texture matTexture = uiImage.material.GetTexture(texture);
                        if (matTexture != null)
                        {
                            string texturePath = AssetDatabase.GetAssetPath(matTexture);
                            if (texturePath == assetPath)
                            {
                                return path;
                            }
                        }
                    }
                }
            }

            // Check for UI RawImage
            if (component is UnityEngine.UI.RawImage rawImage)
            {
                // Check texture reference
                if (rawImage.texture != null)
                {
                    string texturePath = AssetDatabase.GetAssetPath(rawImage.texture);
                    if (texturePath == assetPath)
                    {
                        return path;
                    }
                }

                // Check material reference
                if (rawImage.material != null)
                {
                    string materialPath = AssetDatabase.GetAssetPath(rawImage.material);
                    if (materialPath == assetPath)
                    {
                        return path;
                    }

                    // Check shader reference in the RawImage material
                    if (rawImage.material.shader != null)
                    {
                        string shaderPath = AssetDatabase.GetAssetPath(rawImage.material.shader);
                        if (shaderPath == assetPath)
                        {
                            return path;
                        }
                    }

                    // Check textures in the RawImage material
                    foreach (var texture in rawImage.material.GetTexturePropertyNames())
                    {
                        Texture matTexture = rawImage.material.GetTexture(texture);
                        if (matTexture != null)
                        {
                            string texturePath = AssetDatabase.GetAssetPath(matTexture);
                            if (texturePath == assetPath)
                            {
                                return path;
                            }
                        }
                    }
                }
            }

            // Check for UI Text
            if (component is UnityEngine.UI.Text text)
            {
                // Check material reference
                if (text.material != null)
                {
                    string materialPath = AssetDatabase.GetAssetPath(text.material);
                    if (materialPath == assetPath)
                    {
                        return path;
                    }

                    // Check shader reference in the Text material
                    if (text.material.shader != null)
                    {
                        string shaderPath = AssetDatabase.GetAssetPath(text.material.shader);
                        if (shaderPath == assetPath)
                        {
                            return path;
                        }
                    }

                    // Check textures in the Text material
                    foreach (var texture in text.material.GetTexturePropertyNames())
                    {
                        Texture matTexture = text.material.GetTexture(texture);
                        if (matTexture != null)
                        {
                            string texturePath = AssetDatabase.GetAssetPath(matTexture);
                            if (texturePath == assetPath)
                            {
                                return path;
                            }
                        }
                    }
                }
                // Check font reference (legacy Font)
                if (text.font != null)
                {
                    string fontPath = AssetDatabase.GetAssetPath(text.font);
                    if (fontPath == assetPath)
                    {
                        return path;
                    }
                }
            }

            // Check for TextMesh (3D Text) font reference
            if (component is TextMesh textMesh)
            {
                if (textMesh.font != null)
                {
                    string fontPath = AssetDatabase.GetAssetPath(textMesh.font);
                    if (fontPath == assetPath)
                    {
                        return path;
                    }
                }
            }

            // Check for TextMeshProUGUI (UI) font asset reference
            if (component is TextMeshProUGUI tmpUGUI)
            {
                if (tmpUGUI.font != null)
                {
                    string tmpPath = AssetDatabase.GetAssetPath(tmpUGUI.font);
                    if (tmpPath == assetPath)
                    {
                        return path;
                    }
                }
            }

            // Check for TextMeshPro (3D) font asset reference
            if (component is TextMeshPro tmp3D)
            {
                if (tmp3D.font != null)
                {
                    string tmpPath = AssetDatabase.GetAssetPath(tmp3D.font);
                    if (tmpPath == assetPath)
                    {
                        return path;
                    }
                }
            }

            // Check for Renderer components (MeshRenderer, SkinnedMeshRenderer, etc.)
            if (component is Renderer renderer)
            {
                // Check if the material itself matches (for Material asset search)
                if (renderer.sharedMaterial != null)
                {
                    string materialPath = AssetDatabase.GetAssetPath(renderer.sharedMaterial);
                    if (materialPath == assetPath)
                    {
                        return path;
                    }

                    // Check shader reference in the Renderer material
                    if (renderer.sharedMaterial.shader != null)
                    {
                        string shaderPath = AssetDatabase.GetAssetPath(renderer.sharedMaterial.shader);
                        if (shaderPath == assetPath)
                        {
                            return path;
                        }
                    }

                    // Check for Material texture references (for Texture/Sprite asset search)
                    foreach (var texture in renderer.sharedMaterial.GetTexturePropertyNames())
                    {
                        Texture matTexture = renderer.sharedMaterial.GetTexture(texture);
                        if (matTexture != null)
                        {
                            string texturePath = AssetDatabase.GetAssetPath(matTexture);
                            if (texturePath == assetPath)
                            {
                                return path;
                            }
                        }
                    }
                }

                // Check all materials in case of multi-material renderers
                if (renderer.sharedMaterials != null)
                {
                    foreach (Material mat in renderer.sharedMaterials)
                    {
                        if (mat != null)
                        {
                            string materialPath = AssetDatabase.GetAssetPath(mat);
                            if (materialPath == assetPath)
                            {
                                return path;
                            }

                            // Check shader reference in the material
                            if (mat.shader != null)
                            {
                                string shaderPath = AssetDatabase.GetAssetPath(mat.shader);
                                if (shaderPath == assetPath)
                                {
                                    return path;
                                }
                            }

                            // Check textures in each material
                            foreach (var texture in mat.GetTexturePropertyNames())
                            {
                                Texture matTexture = mat.GetTexture(texture);
                                if (matTexture != null)
                                {
                                    string texturePath = AssetDatabase.GetAssetPath(matTexture);
                                    if (texturePath == assetPath)
                                    {
                                        return path;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    // Helper to get hierarchy path from prefab root to the child gameobject
    private string GetHierarchyPath(GameObject child, GameObject root)
    {
        if (child == null || root == null) return string.Empty;
        if (child == root) return root.name;

        var parts = new List<string>();
        Transform t = child.transform;
        while (t != null && t.gameObject != root)
        {
            parts.Insert(0, t.name);
            t = t.parent;
        }
        parts.Insert(0, root.name);
        return string.Join("/", parts.ToArray());
    }

    private void OnGUI()
    {
        if (isSearching)
        {
            GUILayout.Label($"正在搜索: {currentImage}", EditorStyles.boldLabel);
            Repaint(); // 搜索时刷新 UI
            return;
        }

        // 显示结果
        if (groupedResults.Count > 0)
        {
            GUILayout.Label("搜索结果:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            foreach (var entry in groupedResults)
            {
                GUILayout.Label($"资源: {entry.Key}", EditorStyles.boldLabel);
                foreach (string resultEntry in entry.Value)
                {
                    // resultEntry 格式: "prefabPath -> nodePath"
                    if (GUILayout.Button(resultEntry, EditorStyles.linkLabel))
                    {
                        // 提取箭头前的 prefab 路径
                        string[] parts = resultEntry.Split(new string[] { " -> " }, System.StringSplitOptions.None);
                        string prefabPathOnly = parts.Length > 0 ? parts[0] : resultEntry;
                        // 选中并定位 prefab 资源
                        SelectPrefab(prefabPathOnly);
                        // 打印该 prefab 下所有引用了当前资源的节点路径
                        PrintReferenceNodesForPrefab(prefabPathOnly, entry.Key);
                    }
                }
                GUILayout.Space(10);
            }

            EditorGUILayout.EndScrollView();
        }
        else
        {
            GUILayout.Label("无结果。请右键点击资源并选择 '查找引用'。", EditorStyles.helpBox);
        }
    }

    private void SelectPrefab(string prefabPath)
    {
        Object prefab = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);
        if (prefab != null)
        {
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
    }

    // Print all nodes in the given prefab that reference the specified assetPath
    private void PrintReferenceNodesForPrefab(string prefabPath, string assetPath)
    {
        // Load prefab contents for safe inspection
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            Debug.LogWarning($"Could not load prefab: {prefabPath}");
            return;
        }

        List<string> nodes = FindAllReferencesInPrefab(prefabRoot, assetPath);
        if (nodes.Count == 0)
        {
            Debug.Log($"No references found in prefab: {prefabPath} for asset {assetPath}");
        }
        else
        {
            Debug.Log($"References in prefab {prefabPath} for asset {assetPath}:");
            foreach (string node in nodes)
            {
                Debug.Log(node);
            }
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    // Return all hierarchy paths inside prefabRoot that reference assetPath
    private List<string> FindAllReferencesInPrefab(GameObject prefabRoot, string assetPath)
    {
        var results = new List<string>();
        Component[] components = prefabRoot.GetComponentsInChildren<Component>(true);

        foreach (Component component in components)
        {
            if (component == null) continue;

            string path = GetHierarchyPath(component.gameObject, prefabRoot);

            bool matched = false;

            // Reuse similar checks as FindReferenceInPrefab but collect all matches
            if (component is SpriteRenderer spriteRenderer && spriteRenderer.sprite != null)
            {
                string spritePath = AssetDatabase.GetAssetPath(spriteRenderer.sprite);
                if (spritePath == assetPath) matched = true;
            }

            if (!matched && component is UnityEngine.UI.Image uiImage)
            {
                if (uiImage.sprite != null && AssetDatabase.GetAssetPath(uiImage.sprite) == assetPath) matched = true;
                if (!matched && uiImage.material != null && AssetDatabase.GetAssetPath(uiImage.material) == assetPath) matched = true;
                if (!matched && uiImage.material != null && uiImage.material.shader != null && AssetDatabase.GetAssetPath(uiImage.material.shader) == assetPath) matched = true;
                if (!matched && uiImage.material != null)
                {
                    foreach (var texture in uiImage.material.GetTexturePropertyNames())
                    {
                        Texture matTexture = uiImage.material.GetTexture(texture);
                        if (matTexture != null && AssetDatabase.GetAssetPath(matTexture) == assetPath)
                        {
                            matched = true; break;
                        }
                    }
                }
            }

            if (!matched && component is UnityEngine.UI.RawImage rawImage)
            {
                if (rawImage.texture != null && AssetDatabase.GetAssetPath(rawImage.texture) == assetPath) matched = true;
                if (!matched && rawImage.material != null && AssetDatabase.GetAssetPath(rawImage.material) == assetPath) matched = true;
                if (!matched && rawImage.material != null && rawImage.material.shader != null && AssetDatabase.GetAssetPath(rawImage.material.shader) == assetPath) matched = true;
                if (!matched && rawImage.material != null)
                {
                    foreach (var texture in rawImage.material.GetTexturePropertyNames())
                    {
                        Texture matTexture = rawImage.material.GetTexture(texture);
                        if (matTexture != null && AssetDatabase.GetAssetPath(matTexture) == assetPath)
                        {
                            matched = true; break;
                        }
                    }
                }
            }

            if (!matched && component is UnityEngine.UI.Text text)
            {
                if (text.material != null && AssetDatabase.GetAssetPath(text.material) == assetPath) matched = true;
                if (!matched && text.material != null && text.material.shader != null && AssetDatabase.GetAssetPath(text.material.shader) == assetPath) matched = true;
                if (!matched && text.font != null && AssetDatabase.GetAssetPath(text.font) == assetPath) matched = true;
            }

            if (!matched && component is TextMesh textMesh)
            {
                if (textMesh.font != null && AssetDatabase.GetAssetPath(textMesh.font) == assetPath) matched = true;
            }

            if (!matched && component is TextMeshProUGUI tmpUGUI)
            {
                if (tmpUGUI.font != null && AssetDatabase.GetAssetPath(tmpUGUI.font) == assetPath) matched = true;
            }

            if (!matched && component is TextMeshPro tmp3D)
            {
                if (tmp3D.font != null && AssetDatabase.GetAssetPath(tmp3D.font) == assetPath) matched = true;
            }

            if (!matched && component is Renderer renderer)
            {
                if (renderer.sharedMaterial != null && AssetDatabase.GetAssetPath(renderer.sharedMaterial) == assetPath) matched = true;
                if (!matched && renderer.sharedMaterial != null && renderer.sharedMaterial.shader != null && AssetDatabase.GetAssetPath(renderer.sharedMaterial.shader) == assetPath) matched = true;
                if (!matched)
                {
                    foreach (var texture in renderer.sharedMaterial.GetTexturePropertyNames())
                    {
                        Texture matTexture = renderer.sharedMaterial.GetTexture(texture);
                        if (matTexture != null && AssetDatabase.GetAssetPath(matTexture) == assetPath)
                        {
                            matched = true; break;
                        }
                    }
                }
                if (!matched && renderer.sharedMaterials != null)
                {
                    foreach (Material mat in renderer.sharedMaterials)
                    {
                        if (mat == null) continue;
                        if (AssetDatabase.GetAssetPath(mat) == assetPath) { matched = true; break; }
                        if (mat.shader != null && AssetDatabase.GetAssetPath(mat.shader) == assetPath) { matched = true; break; }
                        foreach (var texture in mat.GetTexturePropertyNames())
                        {
                            Texture matTexture = mat.GetTexture(texture);
                            if (matTexture != null && AssetDatabase.GetAssetPath(matTexture) == assetPath) { matched = true; break; }
                        }
                        if (matched) break;
                    }
                }
            }

            if (matched)
            {
                results.Add(path);
            }
        }

        return results;
    }
}

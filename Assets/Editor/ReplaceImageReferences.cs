using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class ReplaceImageReferences : EditorWindow
{
    private Object sourceImage;
    private Object targetImage;
    private List<string> modifiedPrefabs = new List<string>();
    private Vector2 scrollPos;

    [MenuItem("Tools/替换预制体中的图片引用")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceImageReferences>("替换图片引用");
    }

    private void OnGUI()
    {
        GUILayout.Label("批量替换预制体中的图片引用", EditorStyles.boldLabel);

        // 选择源图和目标图
        sourceImage = EditorGUILayout.ObjectField("源图片 (Sprite/Texture):", sourceImage, typeof(Object), false);
        targetImage = EditorGUILayout.ObjectField("目标图片 (Sprite/Texture):", targetImage, typeof(Object), false);

        // 替换按钮
        if (GUILayout.Button("开始替换"))
        {
            if (sourceImage == null || targetImage == null)
            {
                Debug.LogError("请同时选择源图片和目标图片。");
                return;
            }

            modifiedPrefabs.Clear();
            ReplaceReferencesInPrefabs();
        }

        // 显示已修改的预制体
        if (modifiedPrefabs.Count > 0)
        {
            GUILayout.Label("已修改的预制体:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            foreach (string prefabPath in modifiedPrefabs)
            {
                if (GUILayout.Button(prefabPath, EditorStyles.linkLabel))
                {
                    SelectPrefab(prefabPath);
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void ReplaceReferencesInPrefabs()
    {
        string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");
        string sourceImagePath = AssetDatabase.GetAssetPath(sourceImage);
        string targetImagePath = AssetDatabase.GetAssetPath(targetImage);

        foreach (string prefabGUID in allPrefabs)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            bool modified = false;

            // Open prefab for editing
            GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);

            // Replace references in the prefab instance
            Component[] components = instance.GetComponentsInChildren<Component>(true);
            foreach (Component component in components)
            {
                if (component == null) continue;

                // Replace in SpriteRenderer
                if (component is SpriteRenderer spriteRenderer && spriteRenderer.sprite != null)
                {
                    string spritePath = AssetDatabase.GetAssetPath(spriteRenderer.sprite);
                    if (spritePath == sourceImagePath)
                    {
                        Sprite targetSprite = AssetDatabase.LoadAssetAtPath<Sprite>(targetImagePath);
                        spriteRenderer.sprite = targetSprite;
                        modified = true;
                    }
                }

                // Replace in UI Image
                if (component is UnityEngine.UI.Image uiImage && uiImage.sprite != null)
                {
                    string spritePath = AssetDatabase.GetAssetPath(uiImage.sprite);
                    if (spritePath == sourceImagePath)
                    {
                        Sprite targetSprite = AssetDatabase.LoadAssetAtPath<Sprite>(targetImagePath);
                        uiImage.sprite = targetSprite;
                        modified = true;
                    }
                }

                // Replace in Materials
                if (component is Renderer renderer && renderer.sharedMaterial != null)
                {
                    Material material = renderer.sharedMaterial;
                    foreach (string textureProperty in material.GetTexturePropertyNames())
                    {
                        Texture texture = material.GetTexture(textureProperty);
                        if (texture != null && AssetDatabase.GetAssetPath(texture) == sourceImagePath)
                        {
                            Texture targetTexture = AssetDatabase.LoadAssetAtPath<Texture>(targetImagePath);
                            material.SetTexture(textureProperty, targetTexture);
                            modified = true;
                        }
                    }
                }
            }

            // Save prefab if modified
            if (modified)
            {
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                modifiedPrefabs.Add(prefabPath);
                Debug.Log($"Replaced references in prefab: {prefabPath}");
            }

            // Cleanup
            PrefabUtility.UnloadPrefabContents(instance);
        }

        Debug.Log("Image reference replacement complete.");
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
}

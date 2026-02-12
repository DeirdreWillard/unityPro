using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI; // 添加此行以支持 UnityEngine.UI.Image

public class SpriteReferenceReplacer : EditorWindow
{
    private Object spritesFolder; // 新大图所在文件夹（包含Sprite资源）
    private Object targetFolder;  // 需要替换引用的资源文件夹（Prefab、材质等）

    [MenuItem("Tools/替换PlayingGUI小图引用")]
    public static void ShowWindow()
    {
        GetWindow<SpriteReferenceReplacer>("替换Sprite引用");
    }

    private void OnGUI()
    {
        GUILayout.Label("新大图所在文件夹(包含Sprite资源)", EditorStyles.boldLabel);
        spritesFolder = EditorGUILayout.ObjectField("Sprites Folder", spritesFolder, typeof(Object), false);

        GUILayout.Label("需要替换的目标文件夹", EditorStyles.boldLabel);
        targetFolder = EditorGUILayout.ObjectField("Target Folder", targetFolder, typeof(Object), false);

        if (GUILayout.Button("开始替换"))
        {
            ReplaceSprites();
        }
    }

    private void ReplaceSprites()
    {
        if (spritesFolder == null || targetFolder == null)
        {
            Debug.LogError("请先指定两个文件夹路径");
            return;
        }

        string spritesPath = AssetDatabase.GetAssetPath(spritesFolder);
        string targetPath = AssetDatabase.GetAssetPath(targetFolder);

        // 1. 读取新大图文件夹中所有 Sprite（支持多个纹理文件，每个文件可能有一个或多个 Sprite）
        List<Sprite> allSprites = new List<Sprite>();
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { spritesPath });
        foreach (string guid in textureGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var sub in subAssets.OfType<Sprite>())
            {
                allSprites.Add(sub);
            }
        }

        if (allSprites.Count == 0)
        {
            Debug.LogError("新大图文件夹中未找到任何 Sprite");
            return;
        }

        // 建立名字到 Sprite 的映射，处理可能的重复名称（使用第一个找到的，并警告重复）
        var spriteDict = new Dictionary<string, Sprite>();
        foreach (var s in allSprites)
        {
            if (!spriteDict.ContainsKey(s.name))
            {
                spriteDict.Add(s.name, s);
            }
            else
            {
                Debug.LogWarning($"发现重复的 Sprite 名称: {s.name}，使用第一个找到的版本。");
            }
        }

        // 2. 查找目标文件夹内所有资源
        string[] guids = AssetDatabase.FindAssets("", new[] { targetPath });

        int replaceCount = 0;

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

            if (obj is GameObject go)
            {
                bool dirty = false;

                // 遍历 Prefab 内所有 SpriteRenderer 组件
                var spriteRenderers = go.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var r in spriteRenderers)
                {
                    if (r.sprite != null && spriteDict.TryGetValue(r.sprite.name, out Sprite newSprite))
                    {
                        r.sprite = newSprite;
                        replaceCount++;
                        dirty = true;
                    }
                }

                // 遍历 Prefab 内所有 UI Image 组件
                var images = go.GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    if (img.sprite != null && spriteDict.TryGetValue(img.sprite.name, out Sprite newSprite))
                    {
                        img.sprite = newSprite;
                        img.material = null; // 替换后置空 Image 的 material
                        replaceCount++;
                        dirty = true;
                    }
                }

                if (dirty)
                {
                    EditorUtility.SetDirty(go);
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"替换完成，总共替换 {replaceCount} 个引用。");
    }
}
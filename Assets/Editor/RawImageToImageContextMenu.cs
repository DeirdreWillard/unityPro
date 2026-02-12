using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;

namespace AAAGame.Editor
{
    public static class RawImageToImageContextMenu
    {
        [MenuItem("Assets/GF Tools/转换RawImage为Image", false, 1000)]
        private static void ConvertSelectedPrefabs()
        {
            List<GameObject> selectedPrefabs = GetSelectedPrefabs();
            
            if (selectedPrefabs.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "请选择一个或多个预制体", "确定");
                return;
            }

            int totalConverted = 0;
            List<string> log = new List<string>();
            
            try
            {
                AssetDatabase.StartAssetEditing();
                
                foreach (GameObject prefab in selectedPrefabs)
                {
                    int converted = ConvertPrefab(prefab, log);
                    totalConverted += converted;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
            
            // 显示结果
            string message = $"转换完成!\n总计转换了 {totalConverted} 个RawImage组件";
            if (log.Count > 0)
            {
                message += "\n\n详细信息:\n" + string.Join("\n", log.ToArray());
            }
            
            EditorUtility.DisplayDialog("转换结果", message, "确定");
        }

        [MenuItem("Assets/AAAGame/转换RawImage为Image", true)]
        private static bool ValidateConvertSelectedPrefabs()
        {
            return GetSelectedPrefabs().Count > 0;
        }

        private static List<GameObject> GetSelectedPrefabs()
        {
            List<GameObject> prefabs = new List<GameObject>();
            
            foreach (Object obj in Selection.objects)
            {
                if (obj is GameObject)
                {
                    GameObject go = obj as GameObject;
                    if (PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab)
                    {
                        prefabs.Add(go);
                    }
                }
            }
            
            return prefabs;
        }

        private static int ConvertPrefab(GameObject prefab, List<string> log)
        {
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(prefabPath))
            {
                log.Add($"错误: 无法获取预制体 '{prefab.name}' 的路径");
                return 0;
            }

            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabInstance == null)
            {
                log.Add($"错误: 无法加载预制体内容 '{prefab.name}'");
                return 0;
            }

            int convertedCount = 0;
            
            try
            {
                RawImage[] rawImages = prefabInstance.GetComponentsInChildren<RawImage>(true);
                
                if (rawImages.Length == 0)
                {
                    log.Add($"预制体 '{prefab.name}': 未找到RawImage组件");
                    return 0;
                }
                
                foreach (RawImage rawImage in rawImages)
                {
                    if (ConvertRawImageToImage(rawImage, log))
                    {
                        convertedCount++;
                    }
                }
                
                if (convertedCount > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
                    log.Add($"预制体 '{prefab.name}': 成功转换 {convertedCount} 个组件");
                }
            }
            catch (System.Exception e)
            {
                log.Add($"错误: 转换预制体 '{prefab.name}' 时发生异常: {e.Message}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabInstance);
            }
            
            return convertedCount;
        }

        private static bool ConvertRawImageToImage(RawImage rawImage, List<string> log)
        {
            try
            {
                GameObject gameObject = rawImage.gameObject;
                string objectPath = GetGameObjectPath(gameObject, gameObject.transform.root.gameObject);
                
                // 保存RawImage的属性
                Texture texture = rawImage.texture;
                Color color = rawImage.color;
                Material material = rawImage.material;
                bool raycastTarget = rawImage.raycastTarget;
                bool maskable = rawImage.maskable;
                RectTransform rectTransform = rawImage.rectTransform;
                
                // 保存RectTransform属性
                Vector2 anchorMin = rectTransform.anchorMin;
                Vector2 anchorMax = rectTransform.anchorMax;
                Vector2 anchoredPosition = rectTransform.anchoredPosition;
                Vector2 sizeDelta = rectTransform.sizeDelta;
                Vector2 pivot = rectTransform.pivot;
                Vector3 localScale = rectTransform.localScale;
                Quaternion localRotation = rectTransform.localRotation;

                // 删除RawImage组件
                Object.DestroyImmediate(rawImage);
                
                // 添加Image组件
                Image image = gameObject.AddComponent<Image>();
                
                // 设置Image属性
                image.color = color;
                image.material = material;
                image.raycastTarget = raycastTarget;
                image.maskable = maskable;
                
                // 恢复RectTransform属性
                rectTransform.anchorMin = anchorMin;
                rectTransform.anchorMax = anchorMax;
                rectTransform.anchoredPosition = anchoredPosition;
                rectTransform.sizeDelta = sizeDelta;
                rectTransform.pivot = pivot;
                rectTransform.localScale = localScale;
                rectTransform.localRotation = localRotation;
                
                // 处理纹理转换
                if (texture != null)
                {
                    Sprite sprite = ConvertTextureToSprite(texture);
                    if (sprite != null)
                    {
                        image.sprite = sprite;
                        image.preserveAspect = true;
                    }
                    else
                    {
                        log.Add($"警告: 无法为 '{objectPath}' 转换纹理为Sprite");
                    }
                }
                
                return true;
            }
            catch (System.Exception e)
            {
                log.Add($"错误: 转换RawImage时发生异常: {e.Message}");
                return false;
            }
        }

        private static Sprite ConvertTextureToSprite(Texture texture)
        {
            string texturePath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(texturePath)) return null;
            
            // 检查是否已经是Sprite
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
            foreach (Object asset in assets)
            {
                if (asset is Sprite)
                {
                    return asset as Sprite;
                }
            }
            
            // 尝试修改纹理导入设置
            TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (textureImporter != null)
            {
                if (textureImporter.textureType != TextureImporterType.Sprite)
                {
                    textureImporter.textureType = TextureImporterType.Sprite;
                    textureImporter.spriteImportMode = SpriteImportMode.Single;
                    AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
                    
                    // 重新加载资源
                    assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
                    foreach (Object asset in assets)
                    {
                        if (asset is Sprite)
                        {
                            return asset as Sprite;
                        }
                    }
                }
            }
            
            return null;
        }

        private static string GetGameObjectPath(GameObject gameObject, GameObject root)
        {
            if (gameObject == root) return root.name;
            
            string path = gameObject.name;
            Transform parent = gameObject.transform.parent;
            
            while (parent != null && parent.gameObject != root)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return root.name + "/" + path;
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AAAGame.Editor
{
    public class RawImageToImageBatchProcessor : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<string> folderPaths = new List<string>();
        private bool includeSubfolders = true;
        private bool showStatistics = false;
        private ConversionStatistics stats = new ConversionStatistics();
        
        [System.Serializable]
        public class ConversionStatistics
        {
            public int totalPrefabsScanned;
            public int prefabsWithRawImages;
            public int totalRawImagesFound;
            public int successfulConversions;
            public int failedConversions;
            public List<string> errorMessages = new List<string>();
        }

        [MenuItem("Tools/批量RawImage转Image工具")]
        public static void ShowBatchWindow()
        {
            RawImageToImageBatchProcessor window = GetWindow<RawImageToImageBatchProcessor>("批量RawImage转Image工具");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("批量 RawImage 转 Image 工具", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 文件夹选择区域
            EditorGUILayout.LabelField("选择要扫描的文件夹", EditorStyles.boldLabel);
            
            // 拖拽区域
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "拖拽文件夹到这里");
            
            HandleFolderDragAndDrop(dropArea);

            EditorGUILayout.Space();

            // 选项
            includeSubfolders = EditorGUILayout.Toggle("包含子文件夹", includeSubfolders);
            
            EditorGUILayout.Space();

            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加当前选中的文件夹"))
            {
                AddSelectedFolders();
            }
            if (GUILayout.Button("清空文件夹列表"))
            {
                folderPaths.Clear();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 显示选中的文件夹列表
            if (folderPaths.Count > 0)
            {
                EditorGUILayout.LabelField($"扫描文件夹 ({folderPaths.Count})", EditorStyles.boldLabel);
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(100));
                for (int i = folderPaths.Count - 1; i >= 0; i--)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(folderPaths[i]);
                    if (GUILayout.Button("移除", GUILayout.Width(50)))
                    {
                        folderPaths.RemoveAt(i);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();

                // 扫描和转换按钮
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("扫描预制体"))
                {
                    ScanPrefabs();
                }
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("开始批量转换", GUILayout.Height(30)))
                {
                    BatchConvertPrefabs();
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            // 显示统计信息
            if (showStatistics)
            {
                EditorGUILayout.Space();
                DisplayStatistics();
            }
        }

        private void HandleFolderDragAndDrop(Rect dropArea)
        {
            Event currentEvent = Event.current;
            if (dropArea.Contains(currentEvent.mousePosition))
            {
                if (currentEvent.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        string path = AssetDatabase.GetAssetPath(draggedObject);
                        if (AssetDatabase.IsValidFolder(path))
                        {
                            if (!folderPaths.Contains(path))
                            {
                                folderPaths.Add(path);
                            }
                        }
                    }
                    currentEvent.Use();
                }
            }
        }

        private void AddSelectedFolders()
        {
            foreach (Object obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (AssetDatabase.IsValidFolder(path))
                {
                    if (!folderPaths.Contains(path))
                    {
                        folderPaths.Add(path);
                    }
                }
            }
        }

        private void ScanPrefabs()
        {
            if (folderPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "请先选择要扫描的文件夹", "确定");
                return;
            }

            stats = new ConversionStatistics();
            List<string> prefabPaths = GetAllPrefabPaths();
            
            try
            {
                for (int i = 0; i < prefabPaths.Count; i++)
                {
                    string prefabPath = prefabPaths[i];
                    EditorUtility.DisplayProgressBar("扫描预制体", 
                        $"扫描中... {i + 1}/{prefabPaths.Count}", 
                        (float)i / prefabPaths.Count);
                    
                    ScanPrefab(prefabPath);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            
            showStatistics = true;
            
            string message = $"扫描完成!\n" +
                           $"总预制体: {stats.totalPrefabsScanned}\n" +
                           $"包含RawImage的预制体: {stats.prefabsWithRawImages}\n" +
                           $"总RawImage组件: {stats.totalRawImagesFound}";
            
            EditorUtility.DisplayDialog("扫描结果", message, "确定");
        }

        private void BatchConvertPrefabs()
        {
            if (folderPaths.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "请先选择要扫描的文件夹", "确定");
                return;
            }

            bool confirm = EditorUtility.DisplayDialog("确认批量转换", 
                "这将修改选中文件夹中的所有预制体。建议先备份项目。\n\n确定要继续吗？", 
                "确定", "取消");
            
            if (!confirm) return;

            stats = new ConversionStatistics();
            List<string> prefabPaths = GetAllPrefabPaths();
            
            try
            {
                AssetDatabase.StartAssetEditing();
                
                for (int i = 0; i < prefabPaths.Count; i++)
                {
                    string prefabPath = prefabPaths[i];
                    EditorUtility.DisplayProgressBar("批量转换", 
                        $"转换中... {i + 1}/{prefabPaths.Count}", 
                        (float)i / prefabPaths.Count);
                    
                    ConvertPrefabAtPath(prefabPath);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
            
            showStatistics = true;
            
            string message = $"批量转换完成!\n" +
                           $"总预制体: {stats.totalPrefabsScanned}\n" +
                           $"成功转换: {stats.successfulConversions}\n" +
                           $"失败转换: {stats.failedConversions}";
            
            EditorUtility.DisplayDialog("转换结果", message, "确定");
        }

        private List<string> GetAllPrefabPaths()
        {
            List<string> allPrefabPaths = new List<string>();
            
            foreach (string folderPath in folderPaths)
            {
                string[] guids;
                if (includeSubfolders)
                {
                    guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
                }
                else
                {
                    // 只在当前文件夹查找，不包含子文件夹
                    string[] allGuids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
                    List<string> currentFolderGuids = new List<string>();
                    
                    foreach (string guid in allGuids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        string directory = Path.GetDirectoryName(path).Replace('\\', '/');
                        if (directory == folderPath)
                        {
                            currentFolderGuids.Add(guid);
                        }
                    }
                    guids = currentFolderGuids.ToArray();
                }
                
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!allPrefabPaths.Contains(path))
                    {
                        allPrefabPaths.Add(path);
                    }
                }
            }
            
            return allPrefabPaths;
        }

        private void ScanPrefab(string prefabPath)
        {
            stats.totalPrefabsScanned++;
            
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;
            
            RawImage[] rawImages = prefab.GetComponentsInChildren<RawImage>(true);
            if (rawImages.Length > 0)
            {
                stats.prefabsWithRawImages++;
                stats.totalRawImagesFound += rawImages.Length;
            }
        }

        private void ConvertPrefabAtPath(string prefabPath)
        {
            stats.totalPrefabsScanned++;
            
            try
            {
                GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);
                if (prefabInstance == null)
                {
                    stats.failedConversions++;
                    stats.errorMessages.Add($"无法加载预制体: {prefabPath}");
                    return;
                }

                RawImage[] rawImages = prefabInstance.GetComponentsInChildren<RawImage>(true);
                if (rawImages.Length == 0)
                {
                    PrefabUtility.UnloadPrefabContents(prefabInstance);
                    return;
                }

                stats.prefabsWithRawImages++;
                stats.totalRawImagesFound += rawImages.Length;

                bool hasChanges = false;
                foreach (RawImage rawImage in rawImages)
                {
                    if (ConvertRawImageToImage(rawImage))
                    {
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
                    stats.successfulConversions++;
                }

                PrefabUtility.UnloadPrefabContents(prefabInstance);
            }
            catch (System.Exception e)
            {
                stats.failedConversions++;
                stats.errorMessages.Add($"转换预制体 {prefabPath} 时发生错误: {e.Message}");
            }
        }

        private bool ConvertRawImageToImage(RawImage rawImage)
        {
            try
            {
                GameObject gameObject = rawImage.gameObject;
                
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
                DestroyImmediate(rawImage);
                
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
                }
                
                return true;
            }
            catch (System.Exception e)
            {
                stats.errorMessages.Add($"转换RawImage时发生异常: {e.Message}");
                return false;
            }
        }

        private Sprite ConvertTextureToSprite(Texture texture)
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
            if (textureImporter != null && textureImporter.textureType != TextureImporterType.Sprite)
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
            
            return null;
        }

        private void DisplayStatistics()
        {
            EditorGUILayout.LabelField("统计信息", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField($"扫描的预制体总数: {stats.totalPrefabsScanned}");
            EditorGUILayout.LabelField($"包含RawImage的预制体: {stats.prefabsWithRawImages}");
            EditorGUILayout.LabelField($"找到的RawImage组件总数: {stats.totalRawImagesFound}");
            EditorGUILayout.LabelField($"成功转换: {stats.successfulConversions}");
            EditorGUILayout.LabelField($"转换失败: {stats.failedConversions}");
            
            if (stats.errorMessages.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("错误信息:", EditorStyles.boldLabel);
                foreach (string error in stats.errorMessages.Take(10)) // 只显示前10条错误
                {
                    EditorGUILayout.LabelField(error, EditorStyles.wordWrappedLabel);
                }
                if (stats.errorMessages.Count > 10)
                {
                    EditorGUILayout.LabelField($"... 还有 {stats.errorMessages.Count - 10} 条错误信息");
                }
            }
            
            EditorGUILayout.EndVertical();
        }
    }
}
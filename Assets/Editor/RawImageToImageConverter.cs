using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace AAAGame.Editor
{
    public class RawImageToImageConverter : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<GameObject> selectedPrefabs = new List<GameObject>();
        private bool showConversionLog = false;
        private List<string> conversionLog = new List<string>();
        private bool convertTexturesToSprites = true;
        private bool preserveAspectRatio = true;
        private bool showPreview = false;
        
        [MenuItem("Tools/RawImage转Image工具")]
        public static void ShowWindow()
        {
            RawImageToImageConverter window = GetWindow<RawImageToImageConverter>("RawImage转Image工具");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("RawImage 转 Image 工具", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 选项设置
            EditorGUILayout.LabelField("转换选项", EditorStyles.boldLabel);
            convertTexturesToSprites = EditorGUILayout.Toggle("自动将Texture转换为Sprite", convertTexturesToSprites);
            preserveAspectRatio = EditorGUILayout.Toggle("保持宽高比", preserveAspectRatio);
            showPreview = EditorGUILayout.Toggle("显示预览信息", showPreview);
            
            EditorGUILayout.Space();

            // 预制体选择区域
            EditorGUILayout.LabelField("选择预制体", EditorStyles.boldLabel);
            
            // 拖拽区域
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "拖拽预制体到这里或使用下面的按钮添加");
            
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
                        if (draggedObject is GameObject)
                        {
                            GameObject go = draggedObject as GameObject;
                            if (PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab)
                            {
                                if (!selectedPrefabs.Contains(go))
                                {
                                    selectedPrefabs.Add(go);
                                }
                            }
                        }
                    }
                    currentEvent.Use();
                }
            }

            EditorGUILayout.Space();

            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加选中的预制体"))
            {
                AddSelectedPrefabs();
            }
            if (GUILayout.Button("清空列表"))
            {
                selectedPrefabs.Clear();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 显示选中的预制体列表
            if (selectedPrefabs.Count > 0)
            {
                EditorGUILayout.LabelField($"已选择的预制体 ({selectedPrefabs.Count})", EditorStyles.boldLabel);
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
                for (int i = selectedPrefabs.Count - 1; i >= 0; i--)
                {
                    if (selectedPrefabs[i] == null)
                    {
                        selectedPrefabs.RemoveAt(i);
                        continue;
                    }
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(selectedPrefabs[i], typeof(GameObject), false);
                    if (GUILayout.Button("移除", GUILayout.Width(50)))
                    {
                        selectedPrefabs.RemoveAt(i);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();

                // 预览信息
                if (showPreview && GUILayout.Button("预览转换信息"))
                {
                    PreviewConversion();
                }

                EditorGUILayout.Space();

                // 转换按钮
                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("开始转换", GUILayout.Height(30)))
                {
                    ConvertSelectedPrefabs();
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            // 显示转换日志
            if (conversionLog.Count > 0)
            {
                EditorGUILayout.Space();
                showConversionLog = EditorGUILayout.Foldout(showConversionLog, "转换日志");
                if (showConversionLog)
                {
                    EditorGUILayout.BeginVertical("box");
                    foreach (string log in conversionLog)
                    {
                        EditorGUILayout.LabelField(log, EditorStyles.wordWrappedLabel);
                    }
                    if (GUILayout.Button("清空日志"))
                    {
                        conversionLog.Clear();
                    }
                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void AddSelectedPrefabs()
        {
            foreach (Object obj in Selection.objects)
            {
                if (obj is GameObject)
                {
                    GameObject go = obj as GameObject;
                    if (PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab)
                    {
                        if (!selectedPrefabs.Contains(go))
                        {
                            selectedPrefabs.Add(go);
                        }
                    }
                }
            }
        }

        private void PreviewConversion()
        {
            conversionLog.Clear();
            conversionLog.Add("=== 转换预览 ===");
            
            int totalRawImages = 0;
            foreach (GameObject prefab in selectedPrefabs)
            {
                if (prefab == null) continue;
                
                RawImage[] rawImages = prefab.GetComponentsInChildren<RawImage>(true);
                totalRawImages += rawImages.Length;
                conversionLog.Add($"预制体 '{prefab.name}': 找到 {rawImages.Length} 个RawImage组件");
                
                foreach (RawImage rawImage in rawImages)
                {
                    string path = GetGameObjectPath(rawImage.gameObject, prefab);
                    conversionLog.Add($"  - {path}");
                }
            }
            
            conversionLog.Add($"总计将转换 {totalRawImages} 个RawImage组件");
        }

        private void ConvertSelectedPrefabs()
        {
            if (selectedPrefabs.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "请先选择要转换的预制体", "确定");
                return;
            }

            conversionLog.Clear();
            conversionLog.Add("=== 开始转换 ===");
            
            int totalConverted = 0;
            int totalPrefabs = selectedPrefabs.Count;
            
            try
            {
                AssetDatabase.StartAssetEditing();
                
                for (int i = 0; i < selectedPrefabs.Count; i++)
                {
                    GameObject prefab = selectedPrefabs[i];
                    if (prefab == null) continue;
                    
                    EditorUtility.DisplayProgressBar("转换进度", 
                        $"正在转换预制体 {i + 1}/{totalPrefabs}: {prefab.name}", 
                        (float)i / totalPrefabs);
                    
                    int converted = ConvertPrefab(prefab);
                    totalConverted += converted;
                    
                    if (converted > 0)
                    {
                        conversionLog.Add($"预制体 '{prefab.name}': 转换了 {converted} 个组件");
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
            
            conversionLog.Add($"=== 转换完成 ===");
            conversionLog.Add($"总计转换了 {totalConverted} 个RawImage组件");
            
            if (totalConverted > 0)
            {
                EditorUtility.DisplayDialog("转换完成", 
                    $"成功转换了 {totalConverted} 个RawImage组件", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("转换完成", 
                    "没有找到需要转换的RawImage组件", "确定");
            }
        }

        private int ConvertPrefab(GameObject prefab)
        {
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(prefabPath))
            {
                conversionLog.Add($"错误: 无法获取预制体 '{prefab.name}' 的路径");
                return 0;
            }

            // 实例化预制体进行编辑
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabInstance == null)
            {
                conversionLog.Add($"错误: 无法加载预制体内容 '{prefab.name}'");
                return 0;
            }

            int convertedCount = 0;
            
            try
            {
                RawImage[] rawImages = prefabInstance.GetComponentsInChildren<RawImage>(true);
                
                foreach (RawImage rawImage in rawImages)
                {
                    if (ConvertRawImageToImage(rawImage))
                    {
                        convertedCount++;
                    }
                }
                
                if (convertedCount > 0)
                {
                    // 保存修改
                    PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
                }
            }
            catch (System.Exception e)
            {
                conversionLog.Add($"错误: 转换预制体 '{prefab.name}' 时发生异常: {e.Message}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabInstance);
            }
            
            return convertedCount;
        }

        private bool ConvertRawImageToImage(RawImage rawImage)
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
                        if (preserveAspectRatio)
                        {
                            image.preserveAspect = true;
                        }
                    }
                    else
                    {
                        conversionLog.Add($"警告: 无法为 '{objectPath}' 转换纹理为Sprite");
                    }
                }
                
                conversionLog.Add($"成功转换: {objectPath}");
                return true;
            }
            catch (System.Exception e)
            {
                conversionLog.Add($"错误: 转换RawImage时发生异常: {e.Message}");
                return false;
            }
        }

        private Sprite ConvertTextureToSprite(Texture texture)
        {
            if (!convertTexturesToSprites) return null;
            
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

        private string GetGameObjectPath(GameObject gameObject, GameObject root)
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
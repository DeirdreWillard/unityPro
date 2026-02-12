using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    [EditorToolMenu("创建图集", typeof(CompressToolEditor), 3)]
    public class CreateAtlasPanel : CompressToolSubPanel
    {
        public override string AssetSelectorTypeFilter => "t:folder";

        public override string DragAreaTips => "拖拽到此处添加文件夹";
        public override string ReadmeText => "批量创建图集";
        private Type[] mSupportAssetTypes = { typeof(Sprite), typeof(Texture2D) };
        protected override Type[] SupportAssetTypes => mSupportAssetTypes;

        //图集相关
        AtlasVariantSettings atlasSettings;
        readonly int[] paddingOptionValues = { 2, 4, 8 };
        readonly string[] paddingDisplayOptions = { "2", "4", "8" };
        readonly int[] maxTextureSizeOptionValues = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };
        readonly string[] maxTextureSizeDisplayOptions = { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" };

        int[] texFormatValues;
        string[] texFormatDisplayOptions;

        public override void OnEnter()
        {
            base.OnEnter();
            if (null == atlasSettings)
            {
                atlasSettings = ReferencePool.Acquire<AtlasVariantSettings>();
            }
            CompressTexturePanel.InitTextureFormatOptions(out texFormatValues, out texFormatDisplayOptions);
        }
        public override void OnExit()
        {
            base.OnExit();
            if (atlasSettings != null)
            {
                ReferencePool.Release(atlasSettings);
            }
        }
        public override void DrawBottomButtonsPanel()
        {
            if (EditorSettings.spritePackerMode == SpritePackerMode.Disabled)
            {
                EditorGUILayout.HelpBox("SpritePackerMode已禁用, 在ProjectSettings中启用后才能使用此功能", MessageType.Error);
            }

            EditorGUI.BeginDisabledGroup(EditorSettings.spritePackerMode == SpritePackerMode.Disabled);
            {
                EditorGUILayout.BeginHorizontal("box");
                {
                    if (GUILayout.Button("创建图集", GUILayout.Height(30)))
                    {
                        CreateAtlas();
                    }

                    if (GUILayout.Button("保存设置", GUILayout.Height(30), GUILayout.MaxWidth(100)))
                    {
                        SaveSettings();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        public override void DrawSettingsPanel()
        {
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorToolSettings.Instance.CreateAtlasIncludeChildrenFolder = EditorGUILayout.ToggleLeft("包括每个子文件夹", EditorToolSettings.Instance.CreateAtlasIncludeChildrenFolder, GUILayout.Width(170));
                    EditorToolSettings.Instance.CreateAtlasSpriteSizeLimit = EditorGUILayout.IntPopup("过滤图片像素大于:", EditorToolSettings.Instance.CreateAtlasSpriteSizeLimit, maxTextureSizeDisplayOptions, maxTextureSizeOptionValues);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();
                {
                    EditorToolSettings.Instance.CreateAtlasGenerateVariant = EditorGUILayout.ToggleLeft("创建AtlasVariant", EditorToolSettings.Instance.CreateAtlasGenerateVariant, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!EditorToolSettings.Instance.CreateAtlasGenerateVariant);
                    {
                        EditorGUILayout.LabelField("Variant Scale:", GUILayout.Width(100));
                        EditorToolSettings.Instance.CreateAtlasVariantScale = EditorGUILayout.Slider(EditorToolSettings.Instance.CreateAtlasVariantScale, 0, 1f);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //Include In Build
                EditorGUILayout.BeginHorizontal();
                {
                    EditorToolSettings.Instance.CreateAtlasOverrideIncludeInBuild = EditorGUILayout.ToggleLeft("Include In Build", EditorToolSettings.Instance.CreateAtlasOverrideIncludeInBuild, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!EditorToolSettings.Instance.CreateAtlasOverrideIncludeInBuild);
                    {
                        EditorToolSettings.Instance.CreateAtlasIncludeInBuild = EditorGUILayout.Toggle(EditorToolSettings.Instance.CreateAtlasIncludeInBuild);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //Allow Rotation
                EditorGUILayout.BeginHorizontal();
                {
                    EditorToolSettings.Instance.CreateAtlasOverrideAllowRotation = EditorGUILayout.ToggleLeft("Allow Rotation", EditorToolSettings.Instance.CreateAtlasOverrideAllowRotation, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!EditorToolSettings.Instance.CreateAtlasOverrideAllowRotation);
                    {
                        EditorToolSettings.Instance.CreateAtlasAllowRotation = EditorGUILayout.Toggle(EditorToolSettings.Instance.CreateAtlasAllowRotation);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //Tight Packing
                EditorGUILayout.BeginHorizontal();
                {
                    EditorToolSettings.Instance.CreateAtlasOverrideTightPacking = EditorGUILayout.ToggleLeft("Tight Packing", EditorToolSettings.Instance.CreateAtlasOverrideTightPacking, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!EditorToolSettings.Instance.CreateAtlasOverrideTightPacking);
                    {
                        EditorToolSettings.Instance.CreateAtlasTightPacking = EditorGUILayout.Toggle(EditorToolSettings.Instance.CreateAtlasTightPacking);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //Alpha Dilation
                EditorGUILayout.BeginHorizontal();
                {
                    EditorToolSettings.Instance.CreateAtlasOverrideAlphaDilation = EditorGUILayout.ToggleLeft("Alpha Dilation", EditorToolSettings.Instance.CreateAtlasOverrideAlphaDilation, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!EditorToolSettings.Instance.CreateAtlasOverrideAlphaDilation);
                    {
                        EditorToolSettings.Instance.CreateAtlasAlphaDilation = EditorGUILayout.Toggle(EditorToolSettings.Instance.CreateAtlasAlphaDilation);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //Padding
                EditorGUILayout.BeginHorizontal();
                {
                    EditorToolSettings.Instance.CreateAtlasOverridePadding = EditorGUILayout.ToggleLeft("Padding", EditorToolSettings.Instance.CreateAtlasOverridePadding, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!EditorToolSettings.Instance.CreateAtlasOverridePadding);
                    {
                        EditorToolSettings.Instance.CreateAtlasPadding = EditorGUILayout.IntPopup(EditorToolSettings.Instance.CreateAtlasPadding, paddingDisplayOptions, paddingOptionValues);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //ReadWrite
                EditorGUILayout.BeginHorizontal();
                {
                    EditorToolSettings.Instance.CreateAtlasOverrideReadWrite = EditorGUILayout.ToggleLeft("Read/Write", EditorToolSettings.Instance.CreateAtlasOverrideReadWrite, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!EditorToolSettings.Instance.CreateAtlasOverrideReadWrite);
                    {
                        EditorToolSettings.Instance.CreateAtlasReadWrite = EditorGUILayout.Toggle(EditorToolSettings.Instance.CreateAtlasReadWrite);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //mipMaps
                EditorGUILayout.BeginHorizontal();
                {
                    EditorToolSettings.Instance.CreateAtlasOverrideMipMaps = EditorGUILayout.ToggleLeft("Generate Mip Maps", EditorToolSettings.Instance.CreateAtlasOverrideMipMaps, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!EditorToolSettings.Instance.CreateAtlasOverrideMipMaps);
                    {
                        EditorToolSettings.Instance.CreateAtlasMipMaps = EditorGUILayout.Toggle(EditorToolSettings.Instance.CreateAtlasMipMaps);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //sRGB
                EditorGUILayout.BeginHorizontal();
                {
                    EditorToolSettings.Instance.CreateAtlasOverrideSRGB = EditorGUILayout.ToggleLeft("sRGB", EditorToolSettings.Instance.CreateAtlasOverrideSRGB, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!EditorToolSettings.Instance.CreateAtlasOverrideSRGB);
                    {
                        EditorToolSettings.Instance.CreateAtlasSRGB = EditorGUILayout.Toggle(EditorToolSettings.Instance.CreateAtlasSRGB);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //filterMode
                EditorGUILayout.BeginHorizontal();
                {
                    EditorToolSettings.Instance.CreateAtlasOverrideFilterMode = EditorGUILayout.ToggleLeft("Filter Mode", EditorToolSettings.Instance.CreateAtlasOverrideFilterMode, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!EditorToolSettings.Instance.CreateAtlasOverrideFilterMode);
                    {
                        EditorToolSettings.Instance.CreateAtlasFilterMode = (int)(FilterMode)EditorGUILayout.EnumPopup((FilterMode)EditorToolSettings.Instance.CreateAtlasFilterMode);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //MaxTextureSize
                EditorGUILayout.BeginHorizontal();
                {
                    EditorToolSettings.Instance.CreateAtlasOverrideMaxTexSize = EditorGUILayout.ToggleLeft("Max Texture Size", EditorToolSettings.Instance.CreateAtlasOverrideMaxTexSize, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!EditorToolSettings.Instance.CreateAtlasOverrideMaxTexSize);
                    {
                        EditorToolSettings.Instance.CreateAtlasMaxTexSize = EditorGUILayout.IntPopup(EditorToolSettings.Instance.CreateAtlasMaxTexSize, maxTextureSizeDisplayOptions, maxTextureSizeOptionValues);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //TextureFormat
                EditorGUILayout.BeginHorizontal();
                {
                    EditorToolSettings.Instance.CreateAtlasOverrideTexFormat = EditorGUILayout.ToggleLeft("Texture Format", EditorToolSettings.Instance.CreateAtlasOverrideTexFormat, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!EditorToolSettings.Instance.CreateAtlasOverrideTexFormat);
                    {
                        EditorToolSettings.Instance.CreateAtlasTexFormat = EditorGUILayout.IntPopup(EditorToolSettings.Instance.CreateAtlasTexFormat, texFormatDisplayOptions, texFormatValues);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                //CompressQuality
                EditorGUILayout.BeginHorizontal();
                {
                    EditorToolSettings.Instance.CreateAtlasOverrideCompressQuality = EditorGUILayout.ToggleLeft("Compress Quality", EditorToolSettings.Instance.CreateAtlasOverrideCompressQuality, GUILayout.Width(170));
                    EditorGUI.BeginDisabledGroup(!EditorToolSettings.Instance.CreateAtlasOverrideCompressQuality);
                    {
                        EditorToolSettings.Instance.CreateAtlasCompressQuality = EditorGUILayout.IntSlider(EditorToolSettings.Instance.CreateAtlasCompressQuality, 0, 100);
                        EditorGUI.EndDisabledGroup();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }

        }
        /// <summary>
        /// 获取选择的文件夹
        /// </summary>
        /// <returns></returns>
        private List<string> GetSelectedFolders()
        {
            List<string> folders = new List<string>();
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            foreach (var item in EditorToolSettings.Instance.CompressImgToolItemList)
            {
                if (item == null) continue;

                var assetPath = AssetDatabase.GetAssetPath(item);
                if (ShouldIgnoreAvatarFolder(assetPath))
                {
                    continue;
                }
                var itmTp = GetSelectedItemType(assetPath);
                if (itmTp == ItemType.Folder)
                {
                    folders.Add(assetPath);
                    if (EditorToolSettings.Instance.CreateAtlasIncludeChildrenFolder)
                    {
                        var dirs = Directory.GetDirectories(assetPath, "*", SearchOption.AllDirectories);
                        foreach (var dir in dirs)
                        {
                            var relativeDir = dir;
                            if (!dir.StartsWith("Assets"))
                            {
                                relativeDir = Path.GetRelativePath(dir, projectRoot);
                            }
                            if (ShouldIgnoreAvatarFolder(relativeDir))
                            {
                                continue;
                            }
                            folders.Add(relativeDir);
                        }
                    }
                }
            }
            return folders.Distinct().ToList();
        }
        private bool ShouldIgnoreAvatarFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return false;
            }

            var normalizedPath = folderPath.Replace('\\', '/');
            var parts = normalizedPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Any(part => string.Equals(part, "Avatar", StringComparison.OrdinalIgnoreCase) 
                                  || string.Equals(part, "Bgs", StringComparison.OrdinalIgnoreCase));
        }
        private AtlasVariantSettings GetUserAtlasSettins()
        {
            var result = AtlasVariantSettings.CreateFrom(atlasSettings);
            result.variantScale = EditorToolSettings.Instance.CreateAtlasVariantScale;

            // 根据设置状态决定是否使用用户设置的值
            result.includeInBuild = EditorToolSettings.Instance.CreateAtlasOverrideIncludeInBuild ? EditorToolSettings.Instance.CreateAtlasIncludeInBuild : (bool?)null;
            result.allowRotation = EditorToolSettings.Instance.CreateAtlasOverrideAllowRotation ? EditorToolSettings.Instance.CreateAtlasAllowRotation : (bool?)null;
            result.tightPacking = EditorToolSettings.Instance.CreateAtlasOverrideTightPacking ? EditorToolSettings.Instance.CreateAtlasTightPacking : (bool?)null;
            result.alphaDilation = EditorToolSettings.Instance.CreateAtlasOverrideAlphaDilation ? EditorToolSettings.Instance.CreateAtlasAlphaDilation : (bool?)null;
            result.padding = EditorToolSettings.Instance.CreateAtlasOverridePadding ? EditorToolSettings.Instance.CreateAtlasPadding : (int?)null;
            result.readWrite = EditorToolSettings.Instance.CreateAtlasOverrideReadWrite ? EditorToolSettings.Instance.CreateAtlasReadWrite : (bool?)null;
            result.mipMaps = EditorToolSettings.Instance.CreateAtlasOverrideMipMaps ? EditorToolSettings.Instance.CreateAtlasMipMaps : (bool?)null;
            result.sRGB = EditorToolSettings.Instance.CreateAtlasOverrideSRGB ? EditorToolSettings.Instance.CreateAtlasSRGB : (bool?)null;
            result.filterMode = EditorToolSettings.Instance.CreateAtlasOverrideFilterMode ? (FilterMode)EditorToolSettings.Instance.CreateAtlasFilterMode : (FilterMode?)null;
            result.maxTexSize = EditorToolSettings.Instance.CreateAtlasOverrideMaxTexSize ? EditorToolSettings.Instance.CreateAtlasMaxTexSize : (int?)null;
            result.texFormat = EditorToolSettings.Instance.CreateAtlasOverrideTexFormat ? (TextureImporterFormat)EditorToolSettings.Instance.CreateAtlasTexFormat : (TextureImporterFormat?)null;
            result.compressQuality = EditorToolSettings.Instance.CreateAtlasOverrideCompressQuality ? EditorToolSettings.Instance.CreateAtlasCompressQuality : (int?)null;
            
            return result;
        }
        private void CreateAtlas()
        {
            //var getSizeFunc = Utility.Assembly.GetType("UnityEditor.TextureUtil").GetMethod("GetGPUWidth", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            //创建图集
            var texFolders = GetSelectedFolders();
            int totalCount = texFolders.Count;
            for (int i = 0; i < totalCount; i++)
            {
                var folder = texFolders[i];
                if(EditorUtility.DisplayCancelableProgressBar($"创建图集({i}/{totalCount})", folder, i / (float)totalCount))
                {
                    break;
                }
                if (!Directory.Exists(folder)) continue;

                var texFiles = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly).Where(fName => IsSupportAsset(fName));
                List<UnityEngine.Object> texObjs = new List<UnityEngine.Object>();
                foreach (var file in texFiles)
                {
                    var texObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(file);
                    if (texObj == null) continue;
                    var tmpTex = texObj as Texture;
                    if (Mathf.Max(tmpTex.width, tmpTex.height) > EditorToolSettings.Instance.CreateAtlasSpriteSizeLimit)
                    {
                        //宽/高超过限制的贴图不打进图集
                        continue;
                    }
                    texObjs.Add(texObj);
                }
                if (texObjs.Count > 0)
                {
                    string atlasAssetName = Path.Combine(folder, $"{new DirectoryInfo(folder).Name}_Atlas{CompressTool.GetAtlasExtensionV1V2()}");
                    CompressTool.CreateAtlas(atlasAssetName, GetUserAtlasSettins(), texObjs.ToArray(), EditorToolSettings.Instance.CreateAtlasGenerateVariant, EditorToolSettings.Instance.CreateAtlasVariantScale);
                }
            }
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }
    }
}


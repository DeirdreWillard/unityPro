using UnityEditor;
using UnityEngine;

namespace AAAGame.Editor
{
    /// <summary>
    /// 音效映射生成器偏好设置
    /// </summary>
    public static class AudioMappingGeneratorPrefs
    {
        private const string PREF_KEY_AUTO_GENERATE = "AudioMapping_AutoGenerate";
        private const string PREF_KEY_SHOW_NOTIFICATION = "AudioMapping_ShowNotification";

        /// <summary>
        /// 是否启用自动生成（默认开启）
        /// </summary>
        public static bool AutoGenerateEnabled
        {
            get => EditorPrefs.GetBool(PREF_KEY_AUTO_GENERATE, true);
            set => EditorPrefs.SetBool(PREF_KEY_AUTO_GENERATE, value);
        }

        /// <summary>
        /// 是否显示生成通知（默认开启）
        /// </summary>
        public static bool ShowNotification
        {
            get => EditorPrefs.GetBool(PREF_KEY_SHOW_NOTIFICATION, true);
            set => EditorPrefs.SetBool(PREF_KEY_SHOW_NOTIFICATION, value);
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Preferences/音效映射生成器", SettingsScope.User)
            {
                label = "音效映射生成器",
                guiHandler = (searchContext) =>
                {
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField("自动生成设置", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(
                        "当检测到 Assets/AAAGame/Audio 文件夹下有音频文件变化时，" +
                        "自动重新生成 AudioKeys.cs 和 AudioMapping.json 文件。",
                        MessageType.Info);
                    
                    EditorGUILayout.Space(5);
                    
                    AutoGenerateEnabled = EditorGUILayout.Toggle(
                        new GUIContent("启用自动生成", "检测到音频文件变化时自动生成映射文件"),
                        AutoGenerateEnabled);
                    
                    ShowNotification = EditorGUILayout.Toggle(
                        new GUIContent("显示生成通知", "在控制台显示自动生成的详细日志"),
                        ShowNotification);
                    
                    EditorGUILayout.Space(10);
                    
                    if (GUILayout.Button("立即重新生成映射文件", GUILayout.Height(30)))
                    {
                        if (EditorUtility.DisplayDialog("确认", 
                            "确定要立即重新扫描并生成映射文件吗？", "确定", "取消"))
                        {
                            // 触发手动生成
                            AudioMappingAutoGenerator.ManualGenerate();
                        }
                    }
                    
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField("快捷方式", EditorStyles.boldLabel);
                    if (GUILayout.Button("打开音效映射生成器工具"))
                    {
                        EditorApplication.ExecuteMenuItem("音效/音效映射生成器");
                    }
                },
                
                keywords = new[] { "音效", "Audio", "映射", "Mapping", "自动生成" }
            };

            return provider;
        }
    }
}

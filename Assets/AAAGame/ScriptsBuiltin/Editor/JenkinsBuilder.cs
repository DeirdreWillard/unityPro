using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UGF.EditorTools
{
    public class JenkinsBuilder
    {
        const string BuildResourceConfigFile = "Tools/Jenkins/BuildResourceConfig.json";
        const string BuildAppConfigFile = "Tools/Jenkins/BuildAppConfig.json";
        const string BuildResultFile = "Tools/Jenkins/BuildResult.json";
        
        public static void BuildResource()
        {
            Debug.Log("------------------------------Start BuildResource------------------------------");
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var configFile = UtilityBuiltin.AssetsPath.GetCombinePath(projectRoot, BuildResourceConfigFile);
            if (!File.Exists(configFile))
            {
                Debug.LogError($"构建失败! 构建配置文件不存在:{configFile}");
                WriteBuildResult(false, "构建配置文件不存在", null);
                EditorApplication.Exit(1);
                return;
            }
            JenkinsBuildResourceConfig configJson = null;
            try
            {
                var jsonStr = File.ReadAllText(configFile);
                configJson = UtilityBuiltin.Json.ToObject<JenkinsBuildResourceConfig>(jsonStr);

            }
            catch (Exception err)
            {
                Debug.LogError($"构建失败! 构建配置文件解析失败:{configFile}, Error:{err.Message}");
                WriteBuildResult(false, $"构建配置文件解析失败: {err.Message}", null);
                EditorApplication.Exit(1);
                return;
            }
            if (configJson == null)
            {
                Debug.LogError($"构建失败! 反序列构建配置参数失败:{configFile}");
                WriteBuildResult(false, "反序列化构建配置失败", null);
                EditorApplication.Exit(1);
                return;
            }
            
            if (!CheckAndSwitchPlatform(configJson.Platform))
            {
                Debug.LogError($"构建失败! 切换平台({configJson.Platform})失败.");
                WriteBuildResult(false, $"切换平台失败: {configJson.Platform}", null);
                EditorApplication.Exit(1);
                return;
            }

            try
            {
                var appBuilder = EditorWindow.GetWindow<AppBuildEidtor>();
                appBuilder.Show();
                bool buildSuccess = appBuilder.JenkinsBuildResource(configJson);
                
                // 构建完成后清理资源
                CleanupBuildResources();
                
                string versionFolder = AppBuildSettings.Instance.ApplicableGameVersion.Replace('.', '_') + "_" + configJson.ResourceVersion;
                
                var outputPath = Path.Combine(configJson.ResourceOutputDir, "Full", versionFolder, configJson.Platform.ToString());
                outputPath = outputPath.Replace('\\', '/');
                
                if (buildSuccess)
                {
                    WriteBuildResult(true, "Build resources success", outputPath);
                    Debug.Log($"------------------------------Build Resources Complete: {outputPath}------------------------------");
                    Debug.Log("Exiting Unity Editor...");
                    // 强制退出
                    EditorApplication.Exit(0);
                    return;
                }
                else
                {
                    WriteBuildResult(false, "Build resources failed", outputPath);
                    Debug.LogError("------------------------------Build Resources Failed------------------------------");
                    EditorApplication.Exit(1);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"构建失败! 异常: {ex.Message}\n{ex.StackTrace}");
                WriteBuildResult(false, $"构建异常: {ex.Message}", null);
                CleanupBuildResources();
                EditorApplication.Exit(1);
            }
        }
        
        /// <summary>
        /// 写入构建结果到JSON文件，供后续上传脚本读取
        /// </summary>
        private static void WriteBuildResult(bool success, string message, string outputPath)
        {
            try
            {
                var projectRoot = Directory.GetParent(Application.dataPath).FullName;
                var resultFile = UtilityBuiltin.AssetsPath.GetCombinePath(projectRoot, BuildResultFile);
                var result = new JenkinsBuildResult
                {
                    Success = success,
                    Message = message,
                    OutputPath = outputPath,
                    BuildTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                var json = UtilityBuiltin.Json.ToJson(result);
                File.WriteAllText(resultFile, json);
                Debug.Log($"构建结果已写入: {resultFile}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"写入构建结果失败: {ex.Message}");
            }
        }
        public static void BuildApp()
        {
            Debug.Log("------------------------------Start BuildApp------------------------------");
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var configFile = UtilityBuiltin.AssetsPath.GetCombinePath(projectRoot, BuildAppConfigFile);
            
            if (!File.Exists(configFile))
            {
                Debug.LogError($"构建失败! 构建配置文件不存在:{configFile}");
                WriteBuildResult(false, "构建配置文件不存在", null);
                EditorApplication.Exit(1);
                return;
            }
            
            JenkinsBuildAppConfig configJson = null;
            try
            {
                var jsonStr = File.ReadAllText(configFile);
                configJson = UtilityBuiltin.Json.ToObject<JenkinsBuildAppConfig>(jsonStr);
            }
            catch (Exception err)
            {
                Debug.LogError($"构建失败! 构建配置文件解析失败:{configFile}, Error:{err.Message}");
                WriteBuildResult(false, $"构建配置文件解析失败: {err.Message}", null);
                EditorApplication.Exit(1);
                return;
            }
            
            if (configJson == null)
            {
                Debug.LogError($"构建失败! 反序列化构建配置失败:{configFile}");
                WriteBuildResult(false, "反序列化构建配置失败", null);
                EditorApplication.Exit(1);
                return;
            }
            
            if (!CheckAndSwitchPlatform(configJson.Platform))
            {
                Debug.LogError($"构建失败! 切换平台{configJson.Platform}失败.");
                WriteBuildResult(false, $"切换平台失败: {configJson.Platform}", null);
                EditorApplication.Exit(1);
                return;
            }
            
            try
            {
                var appBuilder = EditorWindow.GetWindow<AppBuildEidtor>();
                appBuilder.Show();
                
                // 注册构建完成回调
                AppBuildEidtor.OnJenkinsBuildComplete = (success, outputPath) =>
                {
                    // 清理资源
                    CleanupBuildResources();
                    
                    if (success)
                    {
                        WriteBuildResult(true, "Build app success", outputPath);
                        Debug.Log($"------------------------------Build App Complete: {outputPath}------------------------------");
                        EditorApplication.Exit(0);
                    }
                    else
                    {
                        WriteBuildResult(false, "Build app failed", outputPath);
                        Debug.LogError("------------------------------Build App Failed------------------------------");
                        EditorApplication.Exit(1);
                    }
                };
                
                appBuilder.JenkinsBuildApp(configJson);
            }
            catch (Exception ex)
            {
                Debug.LogError($"构建失败! 异常: {ex.Message}\n{ex.StackTrace}");
                WriteBuildResult(false, $"构建异常: {ex.Message}", null);
                CleanupBuildResources();
                EditorApplication.Exit(1);
            }
        }
        
        /// <summary>
        /// 清理构建过程中的资源
        /// </summary>
        private static void CleanupBuildResources()
        {
            try
            {
                Debug.Log("[Cleanup] Starting resource cleanup...");
                
                // 卸载未使用的资源
                EditorUtility.UnloadUnusedAssetsImmediate();
                
                // 强制垃圾回收
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();
                
                Debug.Log("[Cleanup] Resource cleanup completed.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Cleanup] Cleanup failed: {ex.Message}");
            }
        }
        /// <summary>
        /// 切换到目标平台
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        private static bool CheckAndSwitchPlatform(BuildTarget platform)
        {
            if (EditorUserBuildSettings.activeBuildTarget != platform)
            {
                BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(platform);
                Debug.Log($"#########切换平台,TargetGroup:{buildTargetGroup}, BuildTarget:{platform}#######");
                return EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, platform);
            }
            return true;
        }
    }

    /// <summary>
    /// 构建结果，用于Jenkins后续步骤读取
    /// </summary>
    public class JenkinsBuildResult
    {
        public bool Success;        // 是否成功
        public string Message;      // 结果消息
        public string OutputPath;   // 输出路径
        public string BuildTime;    // 构建时间
    }

    public class JenkinsBuildResourceConfig
    {
        public string ResourceOutputDir; //构建资源输出目录
        public BuildTarget Platform; //构建平台
        public bool ForceRebuild; //强制重新构建全部资源
        public int ResourceVersion; //资源版本号
        public string UpdatePrefixUrl; //热更资源服务器地址
        public string ApplicableVersions; //资源适用的App版本号
        public bool ForceUpdate; //是否强制更新App
        public string AppUpdateUrl; //App更新地址
        public string AppUpdateDescription; //App更新说明
    }
    public class JenkinsBuildAppConfig
    {
        public string ResourceOutputDir; //构建资源输出目录(只有非全热更需要)
        public BuildTarget Platform; //构建平台
        public bool FullBuild; //打包前先为热更生成AOT dll
        public bool DebugMode; //显示debug窗口
        public bool DevelopmentBuild; //调试模式
        public bool BuildAppBundle; //打Google Play aab包
        public string Version; //App版本号
        public int VersionCode; //App版本编号
    }
}


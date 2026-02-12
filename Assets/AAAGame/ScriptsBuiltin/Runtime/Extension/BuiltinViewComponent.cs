using System.Collections;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
using UnityEngine.UI;

/// <summary>
/// 内置的UI界面(热更之前)
/// </summary>
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.All)]
public class BuiltinViewComponent : GameFrameworkComponent
{
    [SerializeField] Button RepairBtn;  // 修复游戏按钮
    [Header("Loading Progress:")]
    [SerializeField] GameObject loadingProgressNode = null;
    [SerializeField] private Text loadSliderText;
    [SerializeField] private Slider loadSlider;

    [SerializeField] private Text TextCurVersion;
    [SerializeField] private Text TextLastVersion;

    [Space(20)]
    [Header("Tips Dialog:")]
    [SerializeField] GameObject tipsDialog = null;
    [SerializeField] Text tipsTitleText;
    [SerializeField] Text tipsContentText;
    [SerializeField] Button tipsPositiveBtn;
    [SerializeField] Button tipsNegativeBtn;

    private void Start()
    {
        ShowLoadingProgress();

        // 为修复按钮添加点击事件
        if (RepairBtn != null)
        {
            RepairBtn.onClick.RemoveAllListeners();
            RepairBtn.onClick.AddListener(RepairGame);
        }
    }

    public void SetVersionText(string version, int resourceVersion)
    {
        TextCurVersion.text = $"v {GameFramework.Version.GameVersion}";//({ GFBuiltin.Resource.InternalResourceVersion })
        TextLastVersion.text = $"v{version}({resourceVersion})";
    }

    public void ShowLoadingProgress(float defaultProgress = 0, int showtext = 0, string text = "")
    {
        loadingProgressNode.SetActive(true);
        SetLoadingProgress(defaultProgress, showtext, text);
    }
    public void SetLoadingProgress(float progress, int showtext = 0, string text = "")
    {
        // GFBuiltin.LogInfo("设置加载进度: " + progress);
        loadSlider.value = progress;
        if (showtext == 1)
        {
            loadSliderText.text = "加载中, 请勿关闭游戏...";
        }
        else if (showtext == 2)
        {
            loadSliderText.text = "";
        }
        else
        {
            if (text == "")
            {
                loadSliderText.text = Utility.Text.Format("{0:N0}% 请勿关闭游戏", loadSlider.value * 100);
            }
            else
            {
                loadSliderText.text = text;
            }
        }
    }

    public void HideLoadingProgress()
    {
        loadingProgressNode.SetActive(false);
    }

    public void ShowDialog(string title, string content, string yes_btn_title = "确定", string no_btn_title = "取消", UnityEngine.Events.UnityAction yes_cb = null, UnityEngine.Events.UnityAction no_cb = null)
    {
        tipsDialog.SetActive(true);
        tipsNegativeBtn.GetComponentInChildren<Text>().text = no_btn_title;
        tipsPositiveBtn.GetComponentInChildren<Text>().text = yes_btn_title;
        tipsTitleText.text = title;
        tipsContentText.text = content;
        tipsNegativeBtn.onClick.RemoveAllListeners();
        tipsPositiveBtn.onClick.RemoveAllListeners();
        tipsNegativeBtn.onClick.AddListener(() => { no_cb?.Invoke(); HideDialog(); });
        tipsPositiveBtn.onClick.AddListener(() => { yes_cb?.Invoke(); HideDialog(); });
    }


    public void RepairGame()
    {
        ShowDialog(
            "修复游戏",
            "修复将清除本地数据并重启游戏，是否继续？",
            "确定",
            "取消",
            () =>
            {
                Log.Info("用户确认修复游戏，开始执行修复流程");
                StartCoroutine(RepairGameCoroutine());
            },
            () =>
            {
                Log.Info("用户取消修复游戏");
            }
        );
    }

    private IEnumerator RepairGameCoroutine()
    {
        // 禁用按钮，防止重复点击
        RepairBtn.interactable = false;

        // 显示加载进度条
        ShowLoadingProgress(0f, 0, "开始修复游戏...");
        yield return new WaitForSeconds(0.5f);

        StartCoroutine(ExecuteRepairProcess());
        yield break;
    }

    private IEnumerator ExecuteRepairProcess()
    {
        // 显示清理本地缓存的进度
        ShowLoadingProgress(0.2f, 0, "清理本地缓存数据中...");
        yield return new WaitForSeconds(0.5f);

        bool isSuccess = false;
        string errorMessage = string.Empty;

        try
        {
            Log.Info("开始清除本地游戏数据");

#if UNITY_WEBGL && !UNITY_EDITOR
            ClearWebGLCache();
#else

            // 清除persistentDataPath下的文件和目录
            string persistentDataPath = Application.persistentDataPath;
            ClearPersistentDataPath(persistentDataPath);
#endif

            isSuccess = true;
            Log.Info("清除本地游戏数据完成");
        }
        catch (System.Exception e)
        {
            isSuccess = false;
            errorMessage = e.Message;
            Log.Error("修复游戏时发生错误: {0}", e.Message);
        }

        if (isSuccess)
        {
            ShowLoadingProgress(1f, 0, "修复完成，即将重启游戏...");
            yield return new WaitForSeconds(1f);

            Log.Info("游戏修复完成，准备重启应用");
#if UNITY_WEBGL && !UNITY_EDITOR
            // 在WebGL中，我们不能退出应用，而是重新加载页面
            Application.ExternalEval("location.reload()");
#else
            // 重启游戏或重新加载场景
            Application.Quit();
#endif
        }
        else
        {
            ShowDialog("修复失败", "游戏修复过程中发生错误，请尝试重新安装游戏。\n错误信息: " + errorMessage, "确定");
        }

        // 恢复按钮状态
        RepairBtn.interactable = true;
        yield break;
    }

    /// <summary>
    /// 清除persistentDataPath下的所有文件和目录
    /// </summary>
    private void ClearPersistentDataPath(string _persistentDataPath)
    {
        int successCount = 0;
        int failCount = 0;

        try
        {
            // 获取所有文件和目录
            string[] directories = System.IO.Directory.GetDirectories(_persistentDataPath);
            string[] files = System.IO.Directory.GetFiles(_persistentDataPath);

            // 删除所有子目录
            foreach (string directory in directories)
            {
                try
                {
                    // 跳过特定的系统目录（如果有的话）
                    string dirName = System.IO.Path.GetFileName(directory);
                    if (dirName.StartsWith(".") || dirName == "il2cpp" || dirName == "unitybuiltinshaders")
                    {
                        Log.Info("跳过系统目录: {0}", directory);
                        continue;
                    }

                    System.IO.Directory.Delete(directory, true);
                    Log.Info("已清除目录: {0}", directory);
                    successCount++;
                }
                catch (System.Exception dirEx)
                {
                    failCount++;
                    Log.Warning("清除目录失败: {0}, 错误: {1}", directory, dirEx.Message);
                }
            }

            // 删除所有文件
            foreach (string file in files)
            {
                try
                {
                    // 跳过特定类型的文件（如果有的话）
                    string fileName = System.IO.Path.GetFileName(file);
                    if (fileName.StartsWith(".") || fileName.EndsWith(".log"))
                    {
                        Log.Info("跳过文件: {0}", file);
                        continue;
                    }

                    System.IO.File.Delete(file);
                    Log.Info("已清除文件: {0}", file);
                    successCount++;
                }
                catch (System.Exception fileEx)
                {
                    failCount++;
                    Log.Warning("清除文件失败: {0}, 错误: {1}", file, fileEx.Message);
                }
            }

            Log.Info("清理结果 - 成功: {0}, 失败: {1}", successCount, failCount);
            Log.Info("已清除所有本地缓存数据: {0}", _persistentDataPath);
        }
        catch (System.Exception e)
        {
            Log.Error("清除缓存目录时发生严重错误: {0}", e.Message);
            throw; // 重新抛出异常，让RepairGameCoroutine方法处理
        }
    }

    public void HideDialog()
    {
        tipsDialog.SetActive(false);
    }

    private string m_GpsData = "0,0";

    public string GetGpsData()
    {
        return m_GpsData;
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void ClearWebGLCaches();
 // 清除WebGL缓存
    private void ClearWebGLCache()
    {
        try 
        {
            // 使用现代的JSLib交互方法
            Log.Info("清除WebGL数据");
            ClearWebGLCaches();
        }
        catch (System.Exception e)
        {
            Log.Error("清除WebGL缓存失败: {0}", e.Message);
        }
    }

      [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void GetLocationFromBrowser();

    public void GetLocationFromBrowserDLl()
    {
        try 
        {
            Log.Info("WEBGL 开始获取定位");
            GetLocationFromBrowser();
        }
        catch (System.Exception e)
        {
            Log.Error("获取定位失败: {0}", e.Message);
        }
    }

    public void OnLocationReceived(string _gpsData)
    {
        Log.Info("获取定位成功: {0}", _gpsData);
        m_GpsData = _gpsData;
    }

        /// <summary>
    /// 定位失败时的回调方法
    /// </summary>
    /// <param name="_errorMessage">错误消息</param>
    public void OnLocationError(string _errorMessage)
    {
        Log.Error($"定位失败: {_errorMessage}");
    }

#endif

}

using System.Collections.Generic;
using GameFramework;
using GameFramework.Procedure;
using GameFramework.Fsm;
using GameFramework.Event;
using UnityGameFramework.Runtime;
using System;
using GameFramework.Resource;
using ResourceUpdateStartEventArgs = UnityGameFramework.Runtime.ResourceUpdateStartEventArgs;
using ResourceUpdateChangedEventArgs = UnityGameFramework.Runtime.ResourceUpdateChangedEventArgs;
using ResourceUpdateSuccessEventArgs = UnityGameFramework.Runtime.ResourceUpdateSuccessEventArgs;
using ResourceUpdateFailureEventArgs = UnityGameFramework.Runtime.ResourceUpdateFailureEventArgs;
using UnityEngine;
using ResourceVerifyStartEventArgs = UnityGameFramework.Runtime.ResourceVerifyStartEventArgs;
using ResourceVerifySuccessEventArgs = UnityGameFramework.Runtime.ResourceVerifySuccessEventArgs;
using ResourceVerifyFailureEventArgs = UnityGameFramework.Runtime.ResourceVerifyFailureEventArgs;
using UnityEngine.Networking;
#if UNITY_IOS
using UnityEngine.iOS;
#endif

[Serializable]
public class VersionInfo
{
    public int InternalResourceVersion;//资源版本号
    public int VersionListLength;
    public int VersionListHashCode;
    public int VersionListCompressedLength;
    public int VersionListCompressedHashCode;
    public string ApplicableGameVersion;//资源适用的App版本
    public string UpdatePrefixUri;//热更资源地址
    public string UpdatePrefixUriFallback;//备用热更资源地址（当主地址失败时使用）

    public string LastAppVersion; //最新的App版本号
    public bool ForceUpdateApp;//是否强制更新App
    public string AppUpdateUrl;//强制更新App地址
    public string AppUpdateDesc;//强制更新说明文字,显示在对话框中
}
/// <summary>
/// 初始化资源流程
/// 1.如果是单机模式直接初始化资源
/// 2.如果是热更新模式先检测更新再初始化资源
/// </summary>
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class UpdateResourcesProcedure : ProcedureBase
{
    private bool initComplete = false;
    private long mDownloadTotalZipLength = 0L;
    private List<DownloadProgressData> mDownloadProgressData;
    private bool m_IsWaitingForNetworkPermission = false;
    private int m_NetworkRequestRetryCount = 0;
    private const int c_MaxNetworkRetryCount = 3;
    private float m_NetworkRetryTimer = 0f;
    private const float c_NetworkRetryInterval = 2f;
    
    // 回退机制相关
    private int m_DownloadFailureCount = 0; // 下载失败次数（用于调试）
    private bool m_IsUsingFallbackUri = false; // 是否正在使用备用地址
    private string m_FallbackUri = ""; // 备用地址
    private VersionInfo m_CurrentVersionInfo = null; // 当前版本信息
    
    // 文本提示跳动动画相关
    private string m_LoadingTipsBaseText = null;
    private float m_LoadingTipsTimer = 0f;

    private void ShowStaticLoadingTips(string baseText)
    {
        m_LoadingTipsBaseText = baseText;
        m_LoadingTipsTimer = 10f; // 确保下一帧立即刷新
    }

    private void HideStaticLoadingTips()
    {
        m_LoadingTipsBaseText = null;
    }

    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        initComplete = false;
        mDownloadProgressData = new List<DownloadProgressData>();
        m_IsWaitingForNetworkPermission = false;
        m_NetworkRequestRetryCount = 0;
        m_NetworkRetryTimer = 0f;
        
        // 重置回退机制相关变量
        m_DownloadFailureCount = 0;
        m_IsUsingFallbackUri = false;
        m_FallbackUri = "";
        m_CurrentVersionInfo = null;
        
        HideStaticLoadingTips();

        GFBuiltin.Event.Subscribe(WebRequestSuccessEventArgs.EventId, OnWebRequestSuccess);
        GFBuiltin.Event.Subscribe(WebRequestFailureEventArgs.EventId, OnWebRequestFailure);
        GFBuiltin.Event.Subscribe(UnityGameFramework.Runtime.ResourceUpdateStartEventArgs.EventId, OnResourceUpdateStart);
        GFBuiltin.Event.Subscribe(UnityGameFramework.Runtime.ResourceUpdateChangedEventArgs.EventId, OnResourceUpdateChanged);
        GFBuiltin.Event.Subscribe(UnityGameFramework.Runtime.ResourceUpdateSuccessEventArgs.EventId, OnResourceUpdateSuccess);
        GFBuiltin.Event.Subscribe(UnityGameFramework.Runtime.ResourceUpdateAllCompleteEventArgs.EventId, OnResourceUpdateAllComplete);
        GFBuiltin.Event.Subscribe(UnityGameFramework.Runtime.ResourceUpdateFailureEventArgs.EventId, OnResourceUpdateFailure);
        GFBuiltin.Event.Subscribe(UnityGameFramework.Runtime.ResourceVerifyStartEventArgs.EventId, OnResourceVerifyStart);
        GFBuiltin.Event.Subscribe(UnityGameFramework.Runtime.ResourceVerifySuccessEventArgs.EventId, OnResourceVerifySuccess);
        GFBuiltin.Event.Subscribe(UnityGameFramework.Runtime.ResourceVerifyFailureEventArgs.EventId, OnResourceVerifyFailure);

        CheckVersion();
    }


    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        GFBuiltin.Event.Unsubscribe(WebRequestSuccessEventArgs.EventId, OnWebRequestSuccess);
        GFBuiltin.Event.Unsubscribe(WebRequestFailureEventArgs.EventId, OnWebRequestFailure);
        GFBuiltin.Event.Unsubscribe(UnityGameFramework.Runtime.ResourceUpdateStartEventArgs.EventId, OnResourceUpdateStart);
        GFBuiltin.Event.Unsubscribe(UnityGameFramework.Runtime.ResourceUpdateChangedEventArgs.EventId, OnResourceUpdateChanged);
        GFBuiltin.Event.Unsubscribe(UnityGameFramework.Runtime.ResourceUpdateSuccessEventArgs.EventId, OnResourceUpdateSuccess);
        GFBuiltin.Event.Unsubscribe(UnityGameFramework.Runtime.ResourceUpdateAllCompleteEventArgs.EventId, OnResourceUpdateAllComplete);
        GFBuiltin.Event.Unsubscribe(UnityGameFramework.Runtime.ResourceUpdateFailureEventArgs.EventId, OnResourceUpdateFailure);
        GFBuiltin.Event.Unsubscribe(UnityGameFramework.Runtime.ResourceVerifyStartEventArgs.EventId, OnResourceVerifyStart);
        GFBuiltin.Event.Unsubscribe(UnityGameFramework.Runtime.ResourceVerifySuccessEventArgs.EventId, OnResourceVerifySuccess);
        GFBuiltin.Event.Unsubscribe(UnityGameFramework.Runtime.ResourceVerifyFailureEventArgs.EventId, OnResourceVerifyFailure);
        
        HideStaticLoadingTips();

        base.OnLeave(procedureOwner, isShutdown);
    }
    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

        if (m_IsWaitingForNetworkPermission)
        {
            // 等待网络权限
#if UNITY_IOS
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                m_IsWaitingForNetworkPermission = false;
                RequestVersionInfo();
            }
#elif UNITY_WEBGL
            m_IsWaitingForNetworkPermission = false;
            RequestVersionInfo();
#endif

            // 添加网络权限等待超时处理
            m_NetworkRetryTimer += elapseSeconds;
            if (m_NetworkRetryTimer >= c_NetworkRetryInterval)
            {
                m_NetworkRetryTimer = 0f;
                m_NetworkRequestRetryCount++;

                if (m_NetworkRequestRetryCount >= c_MaxNetworkRetryCount)
                {
                    m_IsWaitingForNetworkPermission = false;
                    // 多次重试仍然失败，提示用户检查网络并重试
                    GFBuiltin.BuiltinView.ShowDialog(
                        "网络连接错误",
                        "无法连接到服务器，请检查网络设置后重试。",
                        "重试",
                        "取消",
                        () =>
                        {
                            m_NetworkRequestRetryCount = 0;
                            CheckVersion();
                        },
                        () =>
                        {
                            Application.Quit();
                        });
                }
                else
                {
                    // 尝试再次请求
                    RequestVersionInfo();
                }
            }
        }

        if (!string.IsNullOrEmpty(m_LoadingTipsBaseText))
        {
            m_LoadingTipsTimer += elapseSeconds;
            if (m_LoadingTipsTimer >= 0.5f)
            {
                m_LoadingTipsTimer = 0f;
                string dots = new string('.', (int)(Time.time * 2f) % 4);
                GFBuiltin.BuiltinView.ShowLoadingProgress(0, 0, m_LoadingTipsBaseText + dots);
            }
        }

        if (initComplete)
        {
            ChangeState<LoadHotfixDllProcedure>(procedureOwner);
        }
    }

    private string GetPlatformPath()
    {
#if UNITY_ANDROID
        return "Android";
#elif UNITY_IOS
            return "IOS";
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
#if UNITY_64
            return "Windows64";
#else
            return "Windows";
#endif
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            return "MacOS";
#elif UNITY_WEBGL
            return "WebGL";
#else
        throw new System.NotSupportedException(Utility.Text.Format("Platform '{0}' is not supported.", Application.platform));
#endif
    }

    //向服务器发送请求获取版本信息 进行版本更新检测
    void CheckVersion()
    {
        if (GFBuiltin.Resource.ResourceMode == GameFramework.Resource.ResourceMode.Updatable || GFBuiltin.Resource.ResourceMode == GameFramework.Resource.ResourceMode.UpdatableWhilePlaying)
        {
#if UNITY_IOS
            // 在iOS上，检查网络权限状态
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                // 当前没有网络连接，可能是权限问题或真的没网络
                Log.Info("iOS设备可能需要网络权限，等待用户授权...");
                m_IsWaitingForNetworkPermission = true;
                ShowStaticLoadingTips("正在检查网络连接");
                
                // 显示网络权限提示，促使iOS显示网络权限弹窗
                RequestVersionInfo();
                return;
            }
#endif

            // 正常情况下直接发起请求
            RequestVersionInfo();
        }
        else
        {
            GFBuiltin.Resource.InitResources(OnResInitComplete);
        }
    }

    private void RequestVersionInfo()
    {
        Log.Info("当前为热更新模式, Web请求最新版本号...", AppSettings.Instance.CheckVersionUrl);
        string verFileUrl = UtilityBuiltin.AssetsPath.GetCombinePath(AppSettings.Instance.CheckVersionUrl, GetPlatformPath(), ConstBuiltin.VersionFile);
        Log.Info("请求版本信息地址:{0}", verFileUrl);
        GFBuiltin.WebRequest.AddWebRequest(verFileUrl, this);
        ShowStaticLoadingTips("正在获取版本信息");
    }

    private void OnWebRequestFailure(object sender, GameEventArgs e)
    {
        WebRequestFailureEventArgs ne = (WebRequestFailureEventArgs)e;
        if (ne.UserData != this)
        {
            return;
        }

        Log.Warning("Web请求失败，错误信息: {0}", ne.ErrorMessage);

        m_NetworkRequestRetryCount++;
        if (m_NetworkRequestRetryCount < c_MaxNetworkRetryCount)
        {
            // 自动重试
            GameFramework.GameFrameworkLog.Info("网络请求失败，{0}秒后自动重试 ({1}/{2})...",
                c_NetworkRetryInterval, m_NetworkRequestRetryCount, c_MaxNetworkRetryCount);

            m_NetworkRetryTimer = 0f;
            m_IsWaitingForNetworkPermission = true;
        }
        else
        {
            HideStaticLoadingTips();
            // 多次重试失败，提示用户
            GFBuiltin.BuiltinView.ShowDialog(
                "网络连接错误",
                "无法连接到服务器，请检查网络设置后重试。",
                "重试",
                "取消",
                () =>
                {
                    m_NetworkRequestRetryCount = 0;
                    CheckVersion();
                },
                () =>
                {
                    Application.Quit();
                });
        }
    }

    private void OnWebRequestSuccess(object sender, GameEventArgs e)
    {
        var arg = e as WebRequestSuccessEventArgs;
        if (arg.UserData != this)
        {
            return;
        }

        HideStaticLoadingTips();
        m_NetworkRequestRetryCount = 0;
        m_IsWaitingForNetworkPermission = false;

        var webText = Utility.Converter.GetString(arg.GetWebResponseBytes());
        GFBuiltin.LogInfo("最新资源版本信息:", webText);
        var vinfo = Utility.Json.ToObject<VersionInfo>(webText);
        CheckVersionList(vinfo);
    }

    private void CheckVersionList(VersionInfo vinfo)
    {
        if (vinfo == null)
        {
            GFBuiltin.LogError("热更失败", " 解析version.json信息失败!");
            return;
        }
        
        // 保存版本信息，用于后续可能的重试
        m_CurrentVersionInfo = vinfo;
        
        //Log.Info("{0},{1},{2},{3}", vinfo.VersionListLength, vinfo.VersionListHashCode, vinfo.VersionListCompressedLength, vinfo.VersionListCompressedHashCode);
        var curAppVersion = System.Version.Parse(GameFramework.Version.GameVersion);
        var lastAppVersion = System.Version.Parse(vinfo.LastAppVersion);
        GFBuiltin.BuiltinView.SetVersionText(vinfo.LastAppVersion, vinfo.InternalResourceVersion);
        if (lastAppVersion > curAppVersion)
        {
            GFBuiltin.BuiltinView.ShowDialog("版本更新",
                vinfo.AppUpdateDesc, "更新", "取消",
                () =>
                {
                    Application.OpenURL(vinfo.AppUpdateUrl);
                    GFBuiltin.Shutdown(ShutdownType.Quit);
                },
                () =>
                {
                    if (vinfo.ForceUpdateApp)//强制更新时点不更新则退出游戏
                        GFBuiltin.Shutdown(ShutdownType.Quit);
                    else
                        CheckVersionAndUpdate(vinfo);
                });
            return;
        }

        CheckVersionAndUpdate(vinfo);
    }
    private void CheckVersionAndUpdate(VersionInfo vinfo)
    {
        // 保存备用地址
        if (!string.IsNullOrEmpty(vinfo.UpdatePrefixUriFallback))
        {
            m_FallbackUri = UtilityBuiltin.AssetsPath.GetCombinePath(vinfo.UpdatePrefixUriFallback);
            GFBuiltin.LogInfo("备用资源服务器地址", m_FallbackUri);
        }
        
        GFBuiltin.Resource.UpdatePrefixUri = UtilityBuiltin.AssetsPath.GetCombinePath(vinfo.UpdatePrefixUri);
        GFBuiltin.LogInfo("主资源服务器地址", GFBuiltin.Resource.UpdatePrefixUri);
        CheckVersionListResult checkResult;
        if (CheckResourceApplicable(vinfo.ApplicableGameVersion))
        {
            checkResult = GFBuiltin.Resource.CheckVersionList(vinfo.InternalResourceVersion);
            GFBuiltin.LogInfo($"是否存需要更新资源:{checkResult}");
        }
        else
        {
            GFBuiltin.LogInfo("资源不适用当前客户端版本, 已跳过更新");
            checkResult = GFBuiltin.Resource.CheckVersionList(GFBuiltin.Resource.InternalResourceVersion);
        }
        if (checkResult == GameFramework.Resource.CheckVersionListResult.NeedUpdate)
        {
            GFBuiltin.LogInfo("更新资源列表文件...");
            ShowStaticLoadingTips("正在获取资源列表");
            var updateVersionCall = new UpdateVersionListCallbacks(OnUpdateVersionListSuccess, OnUpdateVersionListFailed);
            GFBuiltin.Resource.UpdateVersionList(vinfo.VersionListLength, vinfo.VersionListHashCode, vinfo.VersionListCompressedLength, vinfo.VersionListCompressedHashCode, updateVersionCall);
        }
        else
        {
            ShowStaticLoadingTips("正在验证资源");
            GFBuiltin.Resource.VerifyResources(OnVerifyResourcesComplete);
            //GFBuiltin.Resource.CheckResources(OnCheckResurcesComplete);
        }
    }
    /// <summary>
    /// 检测最新资源是否适用于当前客户端版本
    /// </summary>
    /// <param name="applicableGameVersion"></param>
    /// <returns></returns>
    private bool CheckResourceApplicable(string applicableGameVersion)
    {
        string[] versionArr = applicableGameVersion.Split('|');
        foreach (var version in versionArr)
        {
            var fixVer = version.Trim();
            if (GameFramework.Version.GameVersion.CompareTo(fixVer) == 0)
            {
                return true;
            }
        }
        return false;
    }

    private void OnVerifyResourcesComplete(bool result)
    {
        Log.Info<bool>("资源验证完成, 验证结果:{0}", result);
        GFBuiltin.Resource.CheckResources(OnCheckResurcesComplete);
    }

    private void OnCheckResurcesComplete(int movedCount, int removedCount, int updateCount, long updateTotalLength, long updateTotalZipLength)
    {
        mDownloadTotalZipLength = updateTotalZipLength;
        if (updateCount <= 0)
        {
            Log.Info("资源已是最新,无需更新.");
            OnResInitComplete();
        }
        else
        {
            Log.Info<int, long, string>("需要更新资源个数:{0},资源大小:{1},下载地址:{2}", updateCount, updateTotalZipLength, GFBuiltin.Resource.UpdatePrefixUri);
            GFBuiltin.Resource.UpdateResources(OnUpdateResourceComplete);
        }
    }
    private void RefreshDownloadProgress()
    {
        long currentTotalUpdateLength = 0L;
        for (int i = 0; i < mDownloadProgressData.Count; i++)
        {
            currentTotalUpdateLength += mDownloadProgressData[i].Length;
        }

        float progressTotal = (float)currentTotalUpdateLength / mDownloadTotalZipLength;
        string dots = new string('.', (int)(Time.time * 2f) % 4);
        string text = $"正在更新资源 {UtilityBuiltin.Valuer.GetByteLengthString(currentTotalUpdateLength)}/{UtilityBuiltin.Valuer.GetByteLengthString(mDownloadTotalZipLength)} 请勿关闭{dots}";
        GFBuiltin.BuiltinView.SetLoadingProgress(progressTotal, 0, text);
    }
    private void OnResourceUpdateStart(object sender, GameEventArgs e)
    {
        HideStaticLoadingTips();
        ResourceUpdateStartEventArgs ne = (ResourceUpdateStartEventArgs)e;

        for (int i = 0; i < mDownloadProgressData.Count; i++)
        {
            if (mDownloadProgressData[i].Name == ne.Name)
            {
                //Log.Warning("Update resource '{0}' is invalid.", ne.Name);
                mDownloadProgressData[i].Length = 0;
                RefreshDownloadProgress();
                return;
            }
        }

        mDownloadProgressData.Add(new DownloadProgressData(ne.Name));
    }
    private void OnResourceUpdateFailure(object sender, GameEventArgs e)
    {
        ResourceUpdateFailureEventArgs ne = (ResourceUpdateFailureEventArgs)e;
        
        if (ne.RetryCount >= ne.TotalRetryCount)
        {
            Log.Error<string, string, string, int>("Download '{0}' failure from '{1}' with error message '{2}', retry count '{3}'.", ne.Name, ne.DownloadUri, ne.ErrorMessage, ne.RetryCount);
            
            // 资源重试次数用完了，统计失败次数
            m_DownloadFailureCount++;
            
            // 判断是否需要切换到备用地址（第一次彻底失败就切换）
            if (!m_IsUsingFallbackUri && !string.IsNullOrEmpty(m_FallbackUri))
            {
                Log.Info("资源下载失败，切换到备用地址: {0}", m_FallbackUri);
                
                m_IsUsingFallbackUri = true;
                m_DownloadFailureCount = 0;
                
                // 切换到备用地址
                GFBuiltin.Resource.UpdatePrefixUri = m_FallbackUri;
                GFBuiltin.LogInfo($"已切换到备用资源服务器地址: {m_FallbackUri}");
                
                // 显示切换提示并重新开始下载
                mDownloadProgressData.Clear();
                DelayRetryResourceDownload();
                return;
            }
            else if (m_IsUsingFallbackUri)
            {
                // 备用地址也失败了，显示错误对话框
                Log.Error("备用地址下载也失败: {0}", ne.Name);
                ShowUpdateFailedDialog();
            }
            
            return;
        }
        else
        {
            Log.Warning("Download '{0}' failure from '{1}' with error message '{2}', retry count '{3}'.", ne.Name, ne.DownloadUri, ne.ErrorMessage, ne.RetryCount);
        }

        // 下载失败时，只重置进度为0，不要移除，让重试机制继续工作
        for (int i = 0; i < mDownloadProgressData.Count; i++)
        {
            if (mDownloadProgressData[i].Name == ne.Name)
            {
                mDownloadProgressData[i].Length = 0;
                // 失败时不刷新进度条，避免进度回退的视觉问题
                // RefreshDownloadProgress();
                return;
            }
        }
    }
    private void OnResourceUpdateSuccess(object sender, GameEventArgs e)
    {
        ResourceUpdateSuccessEventArgs ne = (ResourceUpdateSuccessEventArgs)e;
        Log.Info("Download '{0}' success.", ne.Name);
        
        // 下载成功，重置连续失败计数
        m_DownloadFailureCount = 0;

        for (int i = 0; i < mDownloadProgressData.Count; i++)
        {
            if (mDownloadProgressData[i].Name == ne.Name)
            {
                mDownloadProgressData[i].Length = ne.CompressedLength;
                RefreshDownloadProgress();
                return;
            }
        }
        
        // 如果不在列表中（可能是重试成功），添加并更新进度
        var newData = new DownloadProgressData(ne.Name);
        newData.Length = ne.CompressedLength;
        mDownloadProgressData.Add(newData);
        RefreshDownloadProgress();
    }
    private void OnResourceUpdateChanged(object sender, GameEventArgs e)
    {
        ResourceUpdateChangedEventArgs ne = (ResourceUpdateChangedEventArgs)e;

        for (int i = 0; i < mDownloadProgressData.Count; i++)
        {
            if (mDownloadProgressData[i].Name == ne.Name)
            {
                mDownloadProgressData[i].Length = ne.CurrentLength;
                RefreshDownloadProgress();
                return;
            }
        }
    }
    private void OnUpdateResourceComplete(IResourceGroup resourceGroup, bool result)
    {
        if (result)
        {
            Log.Info("Update resources complete!");
            OnResInitComplete();
        }
        else
        {
            Log.Error("Update resources complete with errors.");
        }
    }

    private void OnResourceUpdateAllComplete(object sender, GameEventArgs e)
    {

    }

    private void OnUpdateVersionListSuccess(string downloadPath, string downloadUri)
    {
        GFBuiltin.Resource.CheckResources(OnCheckResurcesComplete);
    }

    private void OnUpdateVersionListFailed(string downloadUri, string errorMessage)
    {
        Log.Warning("UpdateVersionListFailed, downloadUri:{0}, errorMessage:{1}", downloadUri, errorMessage);
        
        // 版本列表下载失败，立即尝试切换到备用地址
        if (!m_IsUsingFallbackUri && !string.IsNullOrEmpty(m_FallbackUri))
        {
            Log.Info("版本列表下载失败，立即切换到备用资源地址: {0}", m_FallbackUri);
            
            m_IsUsingFallbackUri = true;
            
            // 切换到备用地址
            GFBuiltin.Resource.UpdatePrefixUri = m_FallbackUri;
            GFBuiltin.LogInfo($"已切换到备用资源服务器地址: {m_FallbackUri}");
            
            // 重新尝试更新版本列表
            if (m_CurrentVersionInfo != null)
            {
                // 显示切换提示并重试
                DelayRetryUpdateVersionList();
            }
            else
            {
                Log.Error("m_CurrentVersionInfo 为空，无法重试");
                ShowUpdateFailedDialog();
            }
        }
        else
        {
            // 已经使用备用地址且仍然失败，或者没有备用地址
            Log.Error("版本列表下载失败，无可用的备用地址或备用地址也失败");
            ShowUpdateFailedDialog();
        }
    }
    
    /// <summary>
    /// 延迟重试更新版本列表（带进度提示）
    /// </summary>
    private async void DelayRetryUpdateVersionList()
    {
        // 显示线路切换提示，点号跳动效果
        for (int i = 0; i < 3; i++)
        {
            string dots = new string('.', (i % 3) + 1);
            GFBuiltin.BuiltinView.ShowLoadingProgress(0, 0, $"正在选择最优线路{dots}");
            await System.Threading.Tasks.Task.Delay(400); // 每400ms切换一次点号
        }
        
        ShowStaticLoadingTips("正在获取资源列表");
        await System.Threading.Tasks.Task.Delay(100);
        
        try
        {
            var updateVersionCall = new UpdateVersionListCallbacks(OnUpdateVersionListSuccess, OnUpdateVersionListFailed);
            GFBuiltin.Resource.UpdateVersionList(
                m_CurrentVersionInfo.VersionListLength, 
                m_CurrentVersionInfo.VersionListHashCode, 
                m_CurrentVersionInfo.VersionListCompressedLength, 
                m_CurrentVersionInfo.VersionListCompressedHashCode, 
                updateVersionCall);
        }
        catch (System.Exception ex)
        {
            Log.Error("重试更新版本列表失败: {0}", ex.Message);
            ShowUpdateFailedDialog();
        }
    }
    
    /// <summary>
    /// 延迟重试资源下载（带进度提示）
    /// </summary>
    private async void DelayRetryResourceDownload()
    {
        // 显示线路切换提示，点号跳动效果
        for (int i = 0; i < 3; i++)
        {
            string dots = new string('.', (i % 3) + 1);
            GFBuiltin.BuiltinView.ShowLoadingProgress(0, 0, $"正在选择最优线路{dots}");
            await System.Threading.Tasks.Task.Delay(400); // 每400ms切换一次点号
        }
        
        ShowStaticLoadingTips("正在准备资源下载");
        await System.Threading.Tasks.Task.Delay(100);
        
        try
        {
            GFBuiltin.Resource.CheckResources(OnCheckResurcesComplete);
        }
        catch (System.Exception ex)
        {
            Log.Error("重新检查资源失败: {0}", ex.Message);
            ShowUpdateFailedDialog();
        }
    }
    
    /// <summary>
    /// 显示更新失败对话框
    /// </summary>
    private void ShowUpdateFailedDialog()
    {
        GFBuiltin.BuiltinView.ShowDialog(
            "更新失败",
            "无法下载游戏资源，请检查网络后重试",
            "重试",
            "退出",
            () =>
            {
                m_DownloadFailureCount = 0;
                m_IsUsingFallbackUri = false;
                CheckVersion();
            },
            () =>
            {
                Application.Quit();
            });
    }
    private void OnResourceVerifyStart(object sender, GameEventArgs e)
    {
        ResourceVerifyStartEventArgs ne = (ResourceVerifyStartEventArgs)e;
        Log.Info("Start verify resources, verify resource count '{0}', verify resource total length '{1}'.", ne.Count, ne.TotalLength);
    }

    private void OnResourceVerifySuccess(object sender, GameEventArgs e)
    {
        ResourceVerifySuccessEventArgs ne = (ResourceVerifySuccessEventArgs)e;
        Log.Info("Verify resource '{0}' success.", ne.Name);
    }

    private void OnResourceVerifyFailure(object sender, GameEventArgs e)
    {
        ResourceVerifyFailureEventArgs ne = (ResourceVerifyFailureEventArgs)e;
        Log.Warning("Verify resource '{0}' failure.", ne.Name);
    }
    void OnResInitComplete()
    {
        initComplete = true;

        GFBuiltin.LogInfo("All Resource Completed!");
    }
    private class DownloadProgressData
    {
        private readonly string m_Name;

        public DownloadProgressData(string name)
        {
            m_Name = name;
        }

        public string Name
        {
            get
            {
                return m_Name;
            }
        }

        public int Length
        {
            get;
            set;
        }
    }
}

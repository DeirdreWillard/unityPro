using DG.Tweening;
using UnityEngine;
using System.Collections;

public class AppBackgroundHandler : MonoBehaviour
{
    private const float ShortBackgroundTime = 30f; // 短时间后台阈值 (30秒)
    private const float BackgroundReconnectDelay = 300f; // 后台重连延迟时间
    private float m_LastBackgroundTime = 0f;
    private bool m_IsReconnecting = false; // 标记是否正在重连中
    private bool m_HasPendingReconnect = false; // 是否有待处理的重连请求
    private Coroutine m_ReconnectCoroutine = null; // 重连协程
    private bool m_IsInBackground = false;

    private void OnApplicationPause(bool pauseStatus)
    {
        if (Application.platform != RuntimePlatform.Android
        && Application.platform != RuntimePlatform.IPhonePlayer
        && Application.platform != RuntimePlatform.WebGLPlayer)
        {
            return;
        }
        if (pauseStatus)
        {
            // 应用进入后台
            m_LastBackgroundTime = Time.realtimeSinceStartup;
            GF.LogInfo($"[AppBackgroundHandler] 进入后台时间 {m_LastBackgroundTime}");
            
            // 未登录状态（如登录界面），不需要处理后台逻辑
            if (!HotfixNetworkManager.Ins.isLogin)
            {
                GF.LogInfo("[AppBackgroundHandler] 未登录状态，跳过后台处理");
                return;
            }
            
            m_IsInBackground = true;
            HotfixNetworkManager.Ins.SetBackground(true);
            
            // 暂停消息处理
            HotfixNetworkManager.Ins.hotfixNetworkComponent.PauseMessageProcess(true);
            HotfixNetworkManager.Ins.hotfixNetworkComponent.PauseHeartBeat(true);

            // 取消所有待执行的重连操作
            m_HasPendingReconnect = false;

            if (m_ReconnectCoroutine != null)
            {
                StopCoroutine(m_ReconnectCoroutine);
                m_ReconnectCoroutine = null;
            }
            m_IsReconnecting = false;
        }
        else
        {
            // 应用回到前台
            float backgroundDuration = Time.realtimeSinceStartup - m_LastBackgroundTime;
            GF.LogInfo($"[AppBackgroundHandler] 回来时间 {backgroundDuration} seconds");
            
            // 未登录状态（如登录界面），不需要处理前台恢复逻辑
            if (!HotfixNetworkManager.Ins.isLogin)
            {
                GF.LogInfo("[AppBackgroundHandler] 未登录状态，跳过前台恢复处理");
                return;
            }
            
            m_IsInBackground = false;
            HotfixNetworkManager.Ins.SetBackground(false);

             // 恢复消息处理
            HotfixNetworkManager.Ins.hotfixNetworkComponent.PauseMessageProcess(false);
            HotfixNetworkManager.Ins.hotfixNetworkComponent.PauseHeartBeat(false);

            // 如果正在重连中，标记有待处理的请求但不执行
            if (m_IsReconnecting)
            {
                m_HasPendingReconnect = true;
                GF.LogInfo("[AppBackgroundHandler] 已有重连任务正在进行，标记为待处理");
                return;
            }

            if (backgroundDuration > BackgroundReconnectDelay)
            {
                // 超过10分钟,强制返回登录界面
                GF.LogInfo($"[AppBackgroundHandler] 间隔时间超过 {BackgroundReconnectDelay} 秒，强制返回登录界面");
                HomeProcedure homeProcedure = GF.Procedure.GetProcedure<HomeProcedure>() as HomeProcedure;
                homeProcedure.QuitGame();
                GF.UI.ShowToast("离开时间过长，请重新登录");
            }
            else if (backgroundDuration > ShortBackgroundTime)
            {
                // 30秒到10分钟之间,检查连接状态,必要时重连并重新登录,然后同步桌子信息
                GF.LogInfo($"[AppBackgroundHandler] 间隔时间超过 {ShortBackgroundTime} 秒，开始检查连接状态");
                
                // 清空后台积累的旧消息,防止旧数据覆盖新数据
                HotfixNetworkManager.Ins.hotfixNetworkComponent.ClearRecvPackets();
                
                // 启动重连和同步流程
                m_ReconnectCoroutine = StartCoroutine(ReconnectAndSyncCoroutine());
            }
            else
            {
                // 30秒以内,直接处理积累的消息,不清空,不同步
                GF.LogInfo($"[AppBackgroundHandler] 间隔时间 {backgroundDuration:F1} 秒，直接处理积累的消息");
                bool isConnected = HotfixNetworkManager.Ins?.hotfixNetworkComponent?.IsGameConnectOK() ?? false;
                bool needReconnect = HotfixNetworkManager.Ins?.hotfixNetworkComponent?.CheckGameToReconnect() ?? false;
                if (!isConnected || needReconnect)
                {
                    GF.LogInfo("[AppBackgroundHandler] 短时后台但连接异常，启动重连同步流程");
                    HotfixNetworkManager.Ins.hotfixNetworkComponent.ClearRecvPackets();
                    m_ReconnectCoroutine = StartCoroutine(ReconnectAndSyncCoroutine());
                }
            }
        }
    }

    /// <summary>
    /// 重连并同步协程
    /// </summary>
    private IEnumerator ReconnectAndSyncCoroutine()
    {
        m_IsReconnecting = true;
        
        // 等待一小段时间,让网络状态稳定(避免心跳刚好在这个时候发送)
        yield return new WaitForSeconds(0.5f);
        
        // 1. 检查连接状态(不仅检查IsConnectOK,还要检查心跳状态)
        bool isConnected = HotfixNetworkManager.Ins?.hotfixNetworkComponent?.IsGameConnectOK() ?? false;
        bool isLoggedIn = HotfixNetworkManager.Ins?.isLogin ?? false;
        
        // 强制检查是否有待重连标志
        bool needReconnect = HotfixNetworkManager.Ins?.hotfixNetworkComponent?.CheckGameToReconnect() ?? false;
        
        GF.LogInfo($"[AppBackgroundHandler] 连接状态详细检查: 连接={isConnected}, 登录={isLoggedIn}, 需要重连={needReconnect}");
        
        // 2. 如果连接断开或需要重连,等待重连完成
        if (!isConnected || needReconnect)
        {
            // 清理过期的登录状态
            if (HotfixNetworkManager.Ins != null)
            {
                HotfixNetworkManager.Ins.isLogin = false;
            }
            
            GF.LogInfo("[AppBackgroundHandler] 检测到需要重连,等待自动重连机制完成");
            Util.GetInstance().ShowWaiting("正在重新连接...", "AppBackgroundReconnect");
            
            // 等待重连建立连接(最多20秒)
            float connectTimeout = 20f;
            float connectTimer = 0f;
            
            while (connectTimer < connectTimeout)
            {
                // 检查连接是否恢复
                if (HotfixNetworkManager.Ins?.hotfixNetworkComponent?.IsGameConnectOK() ?? false)
                {
                    GF.LogInfo("[AppBackgroundHandler] 连接已恢复");
                    break;
                }
                
                connectTimer += Time.deltaTime;
                yield return null;
            }
            
            // 检查连接结果
            if (!(HotfixNetworkManager.Ins?.hotfixNetworkComponent?.IsGameConnectOK() ?? false))
            {
                GF.LogError("[AppBackgroundHandler] 连接恢复超时");
                Util.GetInstance().CloseWaiting("AppBackgroundReconnect");
                GF.UI.ShowToast("网络连接失败，请重新登录");
                HomeProcedure homeProcedure = GF.Procedure.GetProcedure<HomeProcedure>() as HomeProcedure;
                homeProcedure?.QuitGame();
                m_IsReconnecting = false;
                m_ReconnectCoroutine = null;
                yield break;
            }
        }
        
        // 3. 等待额外时间确保连接完全稳定
        yield return new WaitForSeconds(1f);
        
        // 4. 无论之前是什么状态,都重新登录确保状态同步
        GF.LogInfo("[AppBackgroundHandler] 开始重新登录确保状态同步");
        Util.GetInstance().ShowWaiting("正在登录...", "AppBackgroundReconnect");
        
        // 清理登录状态
        if (HotfixNetworkManager.Ins != null)
        {
            HotfixNetworkManager.Ins.isLogin = false;
        }
        
        GlobalManager.GetInstance()?.AutoLogin(false);
        
        // 等待登录完成(最多等待15秒)
        float loginTimeout = 15f;
        float loginTimer = 0f;
        while (!(HotfixNetworkManager.Ins?.isLogin ?? false) && loginTimer < loginTimeout)
        {
            loginTimer += Time.deltaTime;
            yield return null;
        }
        
        // 检查登录结果
        if (!(HotfixNetworkManager.Ins?.isLogin ?? false))
        {
            GF.LogError("[AppBackgroundHandler] 登录超时或失败");
            Util.GetInstance().CloseWaiting("AppBackgroundReconnect");
            GF.UI.ShowToast("登录失败，请重新登录");
            HomeProcedure homeProcedure = GF.Procedure.GetProcedure<HomeProcedure>() as HomeProcedure;
            homeProcedure?.QuitGame();
            m_IsReconnecting = false;
            m_ReconnectCoroutine = null;
            yield break;
        }
        
        GF.LogInfo("[AppBackgroundHandler] 登录成功");
        
        // 广播重连成功事件，通知所有UI刷新数据
        GF.Event.Fire(this, GameFramework.ReferencePool.Acquire<GFEventArgs>().Fill(GFEventType.eve_ReConnectGame, null));
        
        // 5. 最终状态确认并同步桌子信息
        bool finalConnected = HotfixNetworkManager.Ins?.hotfixNetworkComponent?.IsGameConnectOK() ?? false;
        bool finalLoggedIn = HotfixNetworkManager.Ins?.isLogin ?? false;
        
        if (finalConnected && finalLoggedIn)
        {
            if (Util.InGameProcedure())
            {
                GF.LogInfo("[AppBackgroundHandler] 状态正常，获取最新桌子信息");
                Util.GetInstance().ShowWaiting("同步游戏状态...", "AppBackgroundReconnect");
                
                // 等待一小段时间确保网络完全稳定
                yield return new WaitForSeconds(0.3f);
                
                GlobalManager.GetInstance()?.Function_GetDeskInfoRq();
                
                // 等待请求发送
                yield return new WaitForSeconds(0.2f);
            }
            else
            {
                GF.LogInfo("[AppBackgroundHandler] 当前不在游戏中，跳过同步桌子信息");
            }
        }
        else
        {
            GF.LogWarning($"[AppBackgroundHandler] 最终状态异常: 连接={finalConnected}, 登录={finalLoggedIn}");
        }
        
        Util.GetInstance().CloseWaiting("AppBackgroundReconnect");
        m_IsReconnecting = false;
        m_ReconnectCoroutine = null;
        
        // 如果有待处理的重连请求,现在可以处理了
        if (m_HasPendingReconnect)
        {
            m_HasPendingReconnect = false;
            GF.LogInfo("[AppBackgroundHandler] 检测到待处理的重连请求，但当前流程已完成");
        }
        
        GF.LogInfo("[AppBackgroundHandler] 后台恢复流程完成");
    }
}

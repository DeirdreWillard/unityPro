using System;
using System.Collections;
using GameFramework;
using NetMsg;
using UnityEngine;

public class HotfixNetworkManager : MonoBehaviour
{
    private static HotfixNetworkManager _instance = null;
    public static HotfixNetworkManager Ins
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<HotfixNetworkManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("HotfixNetworkManager");
                    _instance = obj.AddComponent<HotfixNetworkManager>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
            }
            return _instance;
        }
    }
    public HotfixNetworkComponent hotfixNetworkComponent;

    /// <summary>
    /// 是否登录
    /// </summary>
    public bool isLogin = false;

    #region Private Fields
    /// <summary>
    /// 是否正在重连
    /// </summary>
    private bool isReconnecting = false;
    private bool isManualDisconnect = false; // 是否手动断开
    private float reconnectDelay = 2f; // 减少重连延迟从5秒到2秒
    private int maxReconnectAttempts = 10;
    private int reconnectAttempts = 0;
    private float lastReconnectAttemptTime = 0f; // 记录上次尝试重连的时间
    private bool m_IsProcessingReconnect = false; // 添加新字段，用于防止重连检测循环
    private bool m_IsServerIPInitialized = false; // 是否已初始化服务器IP

    private long serverTimeOffset; // 偏移量：服务器时间 - 本地时间

    private bool isSilentlyReconnecting = false; // 防止多次无感重连
    private Coroutine heartbeatTimeoutCoroutine;
    private bool m_IsInBackground = false;
    private bool m_WasLoggedInBeforeReconnect = false;
    #endregion

    public void Init()
    {
        if (_instance == null)
        {
            _instance = this;
        }

        // 初始化后台处理
        if (!gameObject.TryGetComponent<AppBackgroundHandler>(out var backgroundHandler))
        {
            backgroundHandler = gameObject.AddComponent<AppBackgroundHandler>();
            GF.LogInfo("[HotfixNetworkManager] AppBackgroundHandler initialized");
        }

        // 初始化网络
        hotfixNetworkComponent = new HotfixNetworkComponent();
        hotfixNetworkComponent.Initialize();

        // 初始化IP管理器并连接服务器
        InitializeServerIPAndConnect();
        hotfixNetworkComponent.OnSendHeartMsgHandler = PingReq;
        HotfixNetworkComponent.AddListener(MessageID.MsgPingAck, OnPingRspMsg);
        HotfixNetworkComponent.AddListener(MessageID.MsgLoginAck, OnLoginAck);
        isLogin = false;
    }

    /// <summary>
    /// 初始化服务器IP并连接
    /// </summary>
    private void InitializeServerIPAndConnect()
    {
        // 只在游戏启动和连接失败时执行
        if (m_IsServerIPInitialized || !Const.ServerInfo_IpListFlag)
        {
            ConnectServer();
            return;
        }

        GF.LogInfo("[HotfixNetworkManager] 列表 ", $"IP URL: {Const.ServerInfo_IpListUrl}");
        
        // 显示初始化服务器的loading动画
        Util.GetInstance().ShowWaiting("正在初始化服务器...","InitializeServerIPAndConnect");

        ServerIPManager.Ins.Initialize(Const.ServerInfo_IpListUrl, (success) =>
        {
            Util.GetInstance().CloseWaiting("InitializeServerIPAndConnect");
            m_IsServerIPInitialized = true;
            ConnectServer();
        });
    }

    public void Disconnect()
    {
        CancelHeartbeatTimeout();
        
        isLogin = false;
        isManualDisconnect = true;
        isReconnecting = false;
        isSilentlyReconnecting = false;
        m_WasLoggedInBeforeReconnect = false;
        
        if (hotfixNetworkComponent != null)
        {
            hotfixNetworkComponent.Dispose();
            hotfixNetworkComponent = null;
        }
    }

    private void OnDestroy()
    {
        Disconnect();
        _instance = null;
    }

    private void Update()
    {
        if (hotfixNetworkComponent == null)
            return;
            
        hotfixNetworkComponent.Update(Time.deltaTime);
        
        // 添加重连冷却时间检查
        float currentTime = Time.realtimeSinceStartup;
        bool coolingDown = (currentTime - lastReconnectAttemptTime) < reconnectDelay;
        
        // 添加处理中检查，避免重复触发（未登录状态如登录界面不触发自动重连）
        if (isLogin && !m_IsInBackground && !isReconnecting && !isManualDisconnect && !isSilentlyReconnecting && 
            !coolingDown && !m_IsProcessingReconnect && hotfixNetworkComponent.CheckGameToReconnect())
        {
            m_IsProcessingReconnect = true; // 设置正在处理标记
            lastReconnectAttemptTime = currentTime; // 更新上次尝试时间
            m_WasLoggedInBeforeReconnect = isLogin;
            GF.LogInfo("[HotfixNetworkManager] 检测到需要重连: " +
                $"isReconnecting={isReconnecting}, isManualDisconnect={isManualDisconnect}, " +
                $"isSilentlyReconnecting={isSilentlyReconnecting}, " +
                $"m_IsProcessingReconnect={m_IsProcessingReconnect}, " +
                $"lastReconnectAttemptTime={lastReconnectAttemptTime}, " +
                $"连接状态={hotfixNetworkComponent.IsGameConnectOK()}");
            
            StartCoroutine(HandleReconnect());
        }
        
        // 实时更新服务器时间
        UpdateServerTimestamp();
    }

    /// <summary>
    /// 无感重连 重新连接并登录
    /// </summary>
    public void ReconnectSilently()
    {
        if (isSilentlyReconnecting || isReconnecting || m_IsProcessingReconnect) 
        {
            GF.LogInfo("[HotfixNetworkManager] 已经在进行重连流程，忽略此次无感重连请求");
            return;
        }
        
        GF.LogInfo("[HotfixNetworkManager] 开始无感重连");
        isLogin = false;
        isSilentlyReconnecting = true;
        CoroutineRunner.Instance.StartCoroutine(SilentReconnectProcess());
    }

    private IEnumerator SilentReconnectProcess()
    {
        GF.LogInfo("[HotfixNetworkManager] 开始无感重连流程");
        Util.GetInstance().ShowWaiting("正在重连游戏...","SilentReconnectProcess");
        
        // 刷新服务器IP线路
        yield return StartCoroutine(RefreshServersForSilentReconnect());
        
        // 等待连接建立
        float timeout = 10f;
        float timer = 0f;
        while (!hotfixNetworkComponent.IsGameConnectOK() && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        
        // 连接是否成功
        if (!hotfixNetworkComponent.IsGameConnectOK())
        {
            GF.LogError("[无感重连] 服务器连接失败");
            Util.GetInstance().CloseWaiting("SilentReconnectProcess");
            GF.UI.ShowToast("网络连接失败，请检查网络状态");
            isSilentlyReconnecting = false;
            yield break;
        }
        
        // 连接成功，开始自动登录
        GF.LogInfo("[HotfixNetworkManager] 服务器连接成功，开始自动登录");
        GlobalManager.GetInstance().AutoLogin(false);
        GF.LogInfo("[无感重连] 正在自动重新登录...");
        
        // 等待登录结果
        float loginTimeout = 60f;
        float loginTimer = 0f;
        while (!isLogin && loginTimer < loginTimeout)
        {
            loginTimer += Time.deltaTime;
            yield return null;
        }
        
        // 登录是否成功
        if (isLogin)
        {
            GF.LogInfo("[无感重连] 重连并重新登录成功");
            Util.GetInstance().CloseWaiting("SilentReconnectProcess");
            GF.Event.Fire(this, ReferencePool.Acquire<GFEventArgs>().Fill(GFEventType.eve_ReConnectGame, null));
        }
        else
        {
            GF.LogWarning("[无感重连] 自动登录超时");
            GF.UI.ShowToast("网络连接中断,请重新登录");
            Util.GetInstance().CloseWaiting("SilentReconnectProcess");
            HomeProcedure homeProcedure = GF.Procedure.GetProcedure<HomeProcedure>() as HomeProcedure;
            homeProcedure.QuitGame();
        }
        
        isSilentlyReconnecting = false;
        GF.LogInfo("[HotfixNetworkManager] 无感重连流程结束，重置标志位");
    }
    
    private IEnumerator RefreshServersForSilentReconnect()
    {
        // 重新测试IP线路
        bool isRefreshComplete = false;
        
        ServerIPManager.Ins.RefreshServers((success) => {
            isRefreshComplete = true;
        });
        
        // 等待刷新完成，最多等待3秒
        float refreshTimeout = 3f;
        float refreshTimer = 0f;
        while (!isRefreshComplete && refreshTimer < refreshTimeout)
        {
            refreshTimer += Time.deltaTime;
            yield return null;
        }
        
        // 获取最优服务器并连接
        var (ip, port) = ServerIPManager.Ins.GetOptimalServer();
        GF.LogInfo("[HotfixNetworkManager] ", $"无感重连使用服务器 {ip}:{port}");

        hotfixNetworkComponent.ConnectServer(ip, port, HotfixNetworkComponent.AccountClientName, null);
    }

    private void UpdateServerTimestamp()
    {
        // 计算当前时间相对于服务器时间
        long currentLocalTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; // 当前本地时间（毫秒）
        ServerTimestamp = currentLocalTime + serverTimeOffset; // 服务器时间
    }

    public void ConnectServer()
    {
        var (ip, port) = ServerIPManager.Ins.GetOptimalServer();
        // 显示连接服务器的loading动画，使用特定key
        Util.GetInstance().ShowWaiting("正在连接服务器...", "reconnect_server");
        
        hotfixNetworkComponent.ConnectServer(ip, port, HotfixNetworkComponent.AccountClientName, (isConnected) =>
        {
            if (isConnected)
            {
                GF.LogInfo("[HotfixNetworkManager] 连接服务器成功");
                reconnectAttempts = 0;
                isReconnecting = false;
                isManualDisconnect = false; // 重置手动断开标志
                
                // 发送心跳包
                hotfixNetworkComponent.OnSendHeartMsgHandler?.Invoke();
                
                // 关闭连接等待提示
                Util.GetInstance().CloseWaiting("reconnect_server");
                
                // 如果是重连场景且之前已登录,需要重新登录
                if (m_IsProcessingReconnect && isLogin)
                {
                    GF.LogInfo("[HotfixNetworkManager] 重连成功，触发重新登录流程");
                    isLogin = false;
                    StartCoroutine(WaitLoginAfterReconnect());
                }
                else
                {
                    GF.LogInfo("[HotfixNetworkManager] 连接成功");
                }
            }
            else
            {
                GF.LogError("[HotfixNetworkManager] 连接服务器失败");
                // 关闭loading动画
                Util.GetInstance().CloseWaiting("reconnect_server");
                
                // 标记当前服务器失败
                ServerIPManager.Ins.MarkCurrentServerFailed();
                
                // 重置IP初始化标志，强制重新初始化
                m_IsServerIPInitialized = false;
                
                if (!isReconnecting && !isSilentlyReconnecting)
                {
                    GF.LogInfo("[HotfixNetworkManager] 开始重连流程");
                    m_WasLoggedInBeforeReconnect = isLogin;
                    StartCoroutine(HandleReconnect());
                }
            }
        });
    }

    private IEnumerator HandleReconnect()
    {
        // 避免重复重连
        if (isReconnecting || isSilentlyReconnecting) 
        {
            GF.LogWarning("[HotfixNetworkManager] 已经在重连流程中，忽略此次请求");
            m_IsProcessingReconnect = false; // 重置处理标记
            yield break;
        }
        
        isReconnecting = true;
        
        GF.LogInfo("[HotfixNetworkManager] 开始常规重连流程");
        Util.GetInstance().ShowWaiting("正在重连服务器...", "reconnect_server");
        
        while (reconnectAttempts < maxReconnectAttempts)
        {
            // 如果手动断开或已经连接成功，退出重连
            if (isManualDisconnect || (hotfixNetworkComponent != null && hotfixNetworkComponent.IsGameConnectOK()))
            {
                GF.LogInfo("[HotfixNetworkManager] 连接已恢复或手动断开，结束重连流程");
                isReconnecting = false;
                reconnectAttempts = 0;
                m_IsProcessingReconnect = false; // 重置处理标记
                Util.GetInstance().CloseWaiting("reconnect_server");
                break;
            }
            
            reconnectAttempts++;
            GF.LogInfo("[HotfixNetworkManager] 尝试第 ", $"{reconnectAttempts} 次重连...");

            // 获取最优服务器
            var (reconnectIp, reconnectPort) = ServerIPManager.Ins.GetOptimalServer();
            hotfixNetworkComponent.ConnectServer(reconnectIp, reconnectPort, HotfixNetworkComponent.AccountClientName, null);

            // 使用动态延迟：第一次1秒，之后逐渐增加但不超过3秒
            float currentDelay = Mathf.Min(1f + (reconnectAttempts - 1) * 0.5f, 3f);
            yield return new WaitForSeconds(currentDelay);

            if (hotfixNetworkComponent != null && hotfixNetworkComponent.IsGameConnectOK())
            {
                GF.LogInfo("[HotfixNetworkManager] 重连成功！");
                Util.GetInstance().CloseWaiting("reconnect_server");
                GF.Event.Fire(this, ReferencePool.Acquire<GFEventArgs>().Fill(GFEventType.TCPReconnectSuccess, null));
                if (m_WasLoggedInBeforeReconnect)
                {
                    GF.LogInfo("[HotfixNetworkManager] 重连成功，开始重新登录");
                    isLogin = false;
                    GlobalManager.GetInstance().AutoLogin(false);
                    
                    // 等待登录完成，最多等15秒
                    float loginTimeout = 15f;
                    float loginTimer = 0f;
                    while (!isLogin && loginTimer < loginTimeout)
                    {
                        loginTimer += Time.deltaTime;
                        yield return null;
                    }
                    
                    if (isLogin)
                    {
                        GF.LogInfo("[HotfixNetworkManager] 重连后登录成功，通知UI刷新数据");
                        GF.Event.Fire(this, ReferencePool.Acquire<GFEventArgs>().Fill(GFEventType.eve_ReConnectGame, null));
                    }
                    else
                    {
                        GF.LogError("[HotfixNetworkManager] 重连后登录超时");
                        GF.UI.ShowToast("登录失败，请重新登录");
                        HomeProcedure homeProcedure = GF.Procedure.GetProcedure<HomeProcedure>() as HomeProcedure;
                        homeProcedure?.QuitGame();
                    }
                }
                isReconnecting = false;
                reconnectAttempts = 0; // 重置尝试次数
                m_IsProcessingReconnect = false; // 重置处理标记
                m_WasLoggedInBeforeReconnect = false;
                yield break;
            }
            else
            {
                // 连接失败，切换到下一个IP
                ServerIPManager.Ins.MarkCurrentServerFailed();
            }
        }

        if (!hotfixNetworkComponent.IsGameConnectOK())
        {
            GF.LogError("[HotfixNetworkManager] 重连失败，达到最大重连次数！");
            Util.GetInstance().CloseWaiting("reconnect_server");
            GF.Event.Fire(this, ReferencePool.Acquire<GFEventArgs>().Fill(GFEventType.TCPReconnectFailed, null));
            GF.UI.ShowToast("重连失败,请检查网络状态");
            
            // 可以在这里添加自动跳转到登录界面的逻辑
            HomeProcedure homeProcedure = GF.Procedure.GetProcedure<HomeProcedure>() as HomeProcedure;
            if (homeProcedure != null)
            {
                homeProcedure.QuitGame();
            }
        }

        isReconnecting = false;
        reconnectAttempts = 0; // 重置尝试次数
        m_IsProcessingReconnect = false; // 重置处理标记
        m_WasLoggedInBeforeReconnect = false;
    }

    /// <summary>
    /// 重连后等待登录完成，再广播重连成功事件让UI刷新数据
    /// </summary>
    private IEnumerator WaitLoginAfterReconnect()
    {
        GlobalManager.GetInstance().AutoLogin(false);
        
        float loginTimeout = 15f;
        float loginTimer = 0f;
        while (!isLogin && loginTimer < loginTimeout)
        {
            loginTimer += Time.deltaTime;
            yield return null;
        }
        
        if (isLogin)
        {
            GF.LogInfo("[HotfixNetworkManager] 重连后登录成功，通知UI刷新数据");
            GF.Event.Fire(this, ReferencePool.Acquire<GFEventArgs>().Fill(GFEventType.eve_ReConnectGame, null));
        }
        else
        {
            GF.LogError("[HotfixNetworkManager] 重连后登录超时");
            GF.UI.ShowToast("登录失败，请重新登录");
            HomeProcedure homeProcedure = GF.Procedure.GetProcedure<HomeProcedure>() as HomeProcedure;
            homeProcedure?.QuitGame();
        }
    }

    public void SetBackground(bool isInBackground)
    {
        m_IsInBackground = isInBackground;
    }

    long sendtime = 0;
    long recvtime = 0;
    long? timediff;

    private void CancelHeartbeatTimeout()
    {
        if (heartbeatTimeoutCoroutine != null)
        {
            CancelInvoke("TimeoutHB");
            StopCoroutine(heartbeatTimeoutCoroutine);
            heartbeatTimeoutCoroutine = null;
        }
    }

    public void TimeoutHB() {
        timediff = null;
    }

    public long GetPing() {
        if (timediff == null) {
            return 9999;
        }
        else {
            return timediff.Value;
        }
    }

    /// <summary>
    /// 心跳
    /// </summary>
    public void PingReq()
    {
        if (hotfixNetworkComponent == null || !hotfixNetworkComponent.IsGameConnectOK())
        {
            return; // 如果网络组件不存在或未连接，不发送心跳
        }
        
        // 取消之前的超时检测
        CancelHeartbeatTimeout();
        
        // 发送心跳
        LoginOrCreateRq req = MessagePool.Instance.Fetch<LoginOrCreateRq>();
        hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.MsgPingReq, req);
        sendtime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        recvtime = 0;
        
        // 设置新的超时检测
        Invoke("TimeoutHB", 2.0f);
    }

    public long ServerTimestamp { get; private set; }

    private void OnPingRspMsg(MessageRecvData data)
    {
        var time = MessageProtoHelp.Deserialize<NetMsg.Int64>(data.Data);
        // GF.LogInfo("收到心跳包Time: " , time.Val);

        // 记录服务器时间并计算偏移量
        long currentLocalTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond; // 本地时间（毫秒）
        serverTimeOffset = time.Val - currentLocalTime; // 偏移量 = 服务器时间 - 本地时间

        ServerTimestamp = time.Val; // 设置初始服务器时间
        recvtime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        timediff = (recvtime - sendtime) / 2;
        hotfixNetworkComponent.RecvHeartBeat();
    }

    /// <summary>
    /// 登录结果处理
    /// </summary>
    /// <param name="data"></param>
    private void OnLoginAck(MessageRecvData data)
    {
        try
        {
            LoginOrCreateRs ack = LoginOrCreateRs.Parser.ParseFrom(data.Data);
            GF.LogInfo("[HotfixNetworkManager] 收到登录回调消息: " , ack.ToString());
            
            switch (ack.Code)
            {
                case 0: // 成功
                    var userDm = Util.GetMyselfInfo();
                    userDm.InitUserData(ack);
                    GF.LogInfo("[HotfixNetworkManager] 登录成功");
                    isLogin = true;

                    if (GF.Procedure.CurrentProcedure is StartProcedure startProcedure)
                    {
                        startProcedure.startPanel.SaveLoginData();
                        startProcedure.EnterHome();
                    }
                    break;
                default:
                    isLogin = false;
                    GF.LogError("[HotfixNetworkManager] 登录失败，错误码：" , ack.Code.ToString());
                    // GF.UI.ShowToast("登录失败，请重试");
                    break;
            }
        }
        catch (Exception ex)
        {
            isLogin = false;
            GF.LogError("[HotfixNetworkManager] 登录过程出现异常: " , ex.Message);
            GF.UI.ShowToast("登录过程出现错误，请重试");
        }
    }
}

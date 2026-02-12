using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Protobuf;
using UnityEngine;

public class HotfixNetworkComponent : HotfixComponent
{
    private readonly Dictionary<string, HotfixNetworkClient> s_Clients = new Dictionary<string, HotfixNetworkClient>();
    private static readonly Dictionary<int, MessageCallbackData> s_MessageDispatchs = new Dictionary<int, MessageCallbackData>();
    private static readonly List<MessageRecvData> s_RecvPackets = new List<MessageRecvData>();

    // 添加连接状态锁，防止重复连接
    private Dictionary<string, bool> m_ConnectionInProgress = new Dictionary<string, bool>();
    private float m_LastConnectTime = 0f;
    private const float c_ConnectionCooldown = 1.0f; // 连接冷却时间

    private const int HeadLength = 26;
    private static byte[] m_PacketHeader = new byte[HeadLength];
    private static uint m_PacketIndex = 0;  //包索引
    private static int m_nWritePos = 0;

    // 添加错误计数和阈值
    private static int m_ErrorCount = 0;
    private static int m_MaxErrorCount = 10; // 允许的最大错误次数，可根据需要调整
    private static float m_LastErrorTime = 0;
    private static float m_ErrorResetInterval = 30f; // 错误计数重置间隔，单位：秒
    private static float m_LastCheckReconnectTime = 0f; // 上次检查重连的时间

    private ulong m_uRoleID = 0;

    private float m_LastSendTime = 0; //上次客户端发心跳包时间
    private float m_RecvHeartTime = 0; //上次客户端发包时间
    private int m_LossRecvHeartTimes = 0; // 已检测到弱网状态的次数

    // 心跳包配置参数
    private float m_HeartBeatInterval = 5f; // 心跳包间隔时间
    private float m_HeartBeatTimeout = 20f; // 心跳包超时时间，增加到20秒
    private int m_WeaklyConnectedThreshold = 4; // 弱网判定阈值，增加到4次
    private float m_ReconnectDelay = 3f; // 重连延迟时间，减少到3秒
    private float m_MaxReconnectAttempts = 5f; // 最大重连尝试次数，增加到5次

    // 心跳包统计
    private int m_TotalHeartBeats = 0;
    private int m_FailedHeartBeats = 0;
    private float m_LastHeartBeatLatency = 0; // 最后一次心跳延迟
    private float m_AverageHeartBeatLatency = 0; // 平均心跳延迟
    private int m_ConsecutiveFailures = 0; // 连续失败次数
    private float m_LastReconnectTime = 0; // 上次重连时间
    private int m_ReconnectAttempts = 0; // 重连尝试次数

    // 心跳包状态控制
    private bool m_IsHeartBeatPaused = false;
    private bool m_IsReconnecting = false;

    // 消息处理控制 - 后台时暂停处理消息但保持连接
    private bool m_IsMessageProcessPaused = false;

    public ConnectCallback OnConnectSuccess = null;
    public static string AccountClientName = "Account";

    public System.Action OnSendHeartMsgHandler;
    public System.Action OnRecvHeartMsgHandler;

    public HotfixNetworkComponent()
    {
        m_LastSendTime = 0;
        m_RecvHeartTime = 0;
    }

    private Coroutine m_HeartbeatCoroutine;

    public override void Initialize()
    {
        base.Initialize();
        m_HeartbeatCoroutine = CoroutineRunner.Instance.StartCoroutine(HeartBeatCoroutine());
    }

    private IEnumerator HeartBeatCoroutine()
    {
        while (true)
        {
            if (!m_IsHeartBeatPaused)
            {
                yield return new WaitForSecondsRealtime(m_HeartBeatInterval);
                SendHeartBeat();
            }
            else
            {
                yield return new WaitForSecondsRealtime(1f);
            }
        }
    }

    public override void Update(float deltaTime)
    {
        try
        {
            Execute();
            
            // 检查是否需要重置错误计数
            if (m_ErrorCount > 0 && (Time.realtimeSinceStartup - m_LastErrorTime) > m_ErrorResetInterval)
            {
                m_ErrorCount = 0;
                GF.LogInfo("[HotfixNetwork] 重置错误计数");
            }
        }
        catch (Exception e)
        {
            GF.LogError(e.ToString());
        }
    }

    public bool IsGameConnectOK()
    {
        if (s_Clients.TryGetValue(AccountClientName, out HotfixNetworkClient client))
            return client?.IsConnectOK() == true;

        return false;
    }

    public bool CheckGameToReconnect()
    {
        if (s_Clients.TryGetValue(AccountClientName, out HotfixNetworkClient client))
        {
            // 如果已经连接正常，不需要重连
            if (client.IsConnectOK())
            {
                return false;
            }
            
            // 添加检查，避免频繁调用PrepareReconnect
            if (!client.IsConnectOK() && !client.IsReconnectReady)
            {
                // 如果距离上次重连请求时间太短，则忽略
                float currentTime = Time.realtimeSinceStartup;
                if (currentTime - m_LastCheckReconnectTime < 1.0f)
                {
                    return false;
                }
                
                m_LastCheckReconnectTime = currentTime;
                client.PrepareReconnect();
                GF.LogInfo($"[HotfixNetwork] CheckGameToReconnect 派发重连接事件 ", $"client={client.ClientName}");
                return true;
            }
            else
                return client.IsReconnectReady;
        }

        return false;
    }

    public void ResetNetwork()
    {
        foreach (var client in s_Clients.Values)
        {
            client.Dispose();
        }
        s_Clients.Clear();
    }

    /// <summary>
    /// 发送心跳包
    /// </summary>
    public void SendHeartBeat()
    {
        if (!s_Clients.TryGetValue(AccountClientName, out HotfixNetworkClient client))
            return;

        if (!client.IsConnectOK())
            return;

        m_TotalHeartBeats++;

        if (m_RecvHeartTime > 0)
        {
            float timeSinceLastHeartBeat = Time.realtimeSinceStartup - m_RecvHeartTime;
            m_LastHeartBeatLatency = timeSinceLastHeartBeat;
            m_AverageHeartBeatLatency = (m_AverageHeartBeatLatency * (m_TotalHeartBeats - 1) + timeSinceLastHeartBeat) / m_TotalHeartBeats;

            if (timeSinceLastHeartBeat > m_HeartBeatTimeout)
            {
                m_LossRecvHeartTimes++;
                m_FailedHeartBeats++;
                m_ConsecutiveFailures++;

                GF.LogError("[HotfixNetwork] 丢失心跳",
                    $"recvHeartTime={m_RecvHeartTime} lastSendTime={m_LastSendTime} " +
                    $"weaklyConnectedTimes={m_LossRecvHeartTimes} " +
                    $"totalHeartBeats={m_TotalHeartBeats} failedHeartBeats={m_FailedHeartBeats} " +
                    $"lastLatency={m_LastHeartBeatLatency:F2} avgLatency={m_AverageHeartBeatLatency:F2}");

                if (m_LossRecvHeartTimes >= m_WeaklyConnectedThreshold)
                {
                    HandleWeaklyConnected(client);
                    return;
                }
            }
            else
            {
                m_LossRecvHeartTimes = 0;
                m_ConsecutiveFailures = 0;
            }
        }

        m_LastSendTime = Time.realtimeSinceStartup;
        OnSendHeartMsgHandler?.Invoke();
        if (!Application.isEditor)
        {
            GF.LogInfo("[HotfixNetwork] 发送心跳",
                $"sendTime={m_LastSendTime} totalHeartBeats={m_TotalHeartBeats} " +
                $"failedHeartBeats={m_FailedHeartBeats} lastLatency={m_LastHeartBeatLatency:F2}");
        }
    }

    private void HandleWeaklyConnected(HotfixNetworkClient client)
    {
        if (m_IsReconnecting)
            return;

        float timeSinceLastReconnect = Time.realtimeSinceStartup - m_LastReconnectTime;
        if (timeSinceLastReconnect < m_ReconnectDelay)
            return;

        if (m_ReconnectAttempts >= m_MaxReconnectAttempts)
        {
            // 显示网络状态提示
            string tips = "网络连接不稳定，正在尝试重连...";
            if (m_ReconnectAttempts == 0)
            {
                tips = "网络连接不稳定，请检查网络设置";
            }
            else if (m_ReconnectAttempts == 1)
            {
                tips = "正在重新连接服务器...";
            }
            else if (m_ReconnectAttempts == 2)
            {
                tips = "连接服务器中...";
            }
            else if (m_ReconnectAttempts == 3)
            {
                tips = "正在恢复游戏状态...";
            }
            else
            {
                tips = "连接失败，请检查网络后重试";
            }

            // 通知UI显示网络状态
            NotifyNetworkStatus(client, EConnectStatus.ECS_WEAKLY_CONNECTED, tips);
            return;
        }

        m_IsReconnecting = true;
        m_LastReconnectTime = Time.realtimeSinceStartup;
        m_ReconnectAttempts++;

        // 重置客户端连接
        if (client.IsConnectOK())
        {
            client.Close(false);
            GF.LogInfo($"[HotfixNetwork] 弱联网状态下关闭现有连接 ", $"client={client.ClientName}");
        }

        // 重新建立连接
        client.ConnectAsync().ConfigureAwait(false);
        m_RecvHeartTime = m_LastSendTime = Time.realtimeSinceStartup;
        NotifyNetworkStatus(client, EConnectStatus.ECS_WEAKLY_CONNECTED, "正在重新连接服务器...");
    }

    public void RecvHeartBeat()
    {
        m_RecvHeartTime = Time.realtimeSinceStartup;
        m_IsReconnecting = false;
        m_ReconnectAttempts = 0;
        if (!Application.isEditor)
        {
            GF.LogInfo("[HotfixNetwork] 心跳回包",
                $"recvTime={m_RecvHeartTime} latency={m_LastHeartBeatLatency:F2}");
        }
    }

    public void Execute()
    {
#if (UNITY_WEBGL && !UNITY_EDITOR) || UNITY_EDITOR_WEB
        if (s_Clients.TryGetValue(AccountClientName, out HotfixNetworkClient client))
        {
            client.MessageQueue();
        }
#endif

        // 如果消息处理被暂停（如后台状态），则不处理接收到的消息，但保持连接
        if (IsMessageProcessPaused())
        {
            // 可以选择性地清理过多的消息缓存，防止内存占用过大
            lock (s_RecvPackets)
            {
                if (s_RecvPackets.Count > 1000) // 防止消息堆积过多
                {
                    GF.LogWarning("[HotfixNetwork] 后台消息堆积过多，清理部分消息", $"消息数量：{s_RecvPackets.Count}");
                    s_RecvPackets.RemoveRange(0, s_RecvPackets.Count - 500); // 保留最近500条消息
                }
            }
            return;
        }

        List<MessageRecvData> packetsToProcess;
        lock (s_RecvPackets)
        {
            if (s_RecvPackets.Count == 0)
                return;

            // 创建一个副本进行处理
            packetsToProcess = new List<MessageRecvData>(s_RecvPackets);
            s_RecvPackets.Clear();
        }

        // 处理副本中的数据
        foreach (var recv in packetsToProcess)
        {
            DispatchPacket(recv);
        }
    }

    public void ConnectServer(string ip, int port, string name, ConnectCallback onConnect)
    {
        GF.LogInfo("[HotfixNetwork] ConnectServer: 连接服务器", $"ip = {ip},port = {port}  name={name}");
        // 检查是否正在连接中，避免重复连接
        if (s_Clients.TryGetValue(name, out HotfixNetworkClient client))
        {
            // 直接判断client的m_IsConnecting状态（需提供public属性）
            if (client.IsConnecting)
            {
                GF.LogInfo($"[HotfixNetwork] ConnectServer: 连接已在进行中，忽略重复连接请求 ", $"name={name}");
                return;
            }
            GF.LogError($"[HotfixNetwork] ConnectServer: Connect To Sever , Close Old TcpClient Frist!!! ", $"name={name}");
            client.Dispose();
            s_Clients.Remove(name);
        }

        // 检查连接冷却时间
        float currentTime = Time.realtimeSinceStartup;
        if (currentTime - m_LastConnectTime < c_ConnectionCooldown)
        {
            GF.LogInfo($"[HotfixNetwork] ConnectServer: 连接请求过于频繁，忽略此次请求 ", $"name={name}");
            return;
        }
        m_LastConnectTime = currentTime;

        client = new HotfixNetworkClient(ip, port, name)
        {
            OnConnectCallback = (success) => 
            {
                onConnect?.Invoke(success);
            }
        };
        s_Clients.Add(name, client);

        client.ConnectAsync().ConfigureAwait(false);
        m_LastSendTime = Time.realtimeSinceStartup;
        m_RecvHeartTime = Time.realtimeSinceStartup;
    }

    public void Close(string name, bool isShutdown = false)
    {
        if (!s_Clients.TryGetValue(name, out HotfixNetworkClient client))
            return;

        if (!isShutdown)
            client.Close(false);
        else
            client.Dispose();

        InitReliableMessageSend();
        ClearData();
    }

    // 消息包-注册 发送
    public void SendPB3<T>(string name, MessageID messageID, T obj) where T : IMessage
    {
        if (!s_Clients.TryGetValue(name, out HotfixNetworkClient client))
            return;

        if (!client.IsConnectOK())
        {
            GF.LogError("[HotfixNetwork] SendPB3 failed, client IsConnect False !!!");
            NotifyNetworkStatus(client, EConnectStatus.ECS_TCP_NO_CONNNECT);
            return;
        }

        if ((int)messageID != 1900003)
        {
            GF.LogInfo("[HotfixNetwork] Send Message ", $"ID:{messageID} NO:{(int)messageID}");
            GF.LogInfo("[HotfixNetwork] Message Content: ", obj.ToString());
            LoadingManager.Instance.OnRequestSent((int)messageID);
        }


        byte[] bytes = null;
        PackPB3((int)messageID, obj, m_uRoleID, ref bytes);
        client.AddPacket(bytes);
    }

    //TODO: 优化点
    //1. MemoryStream
    public static void Recv(HotfixNetworkClient client, byte[] bytes)
    {
        // 首先检查客户端是否有效
        if (client == null || !client.IsConnectOK())
        {
            // 客户端无效或已断开，忽略此数据包
            return;
        }

        // 添加长度检查
        if (bytes == null || bytes.Length < HeadLength)
        {
            GF.LogError("[HotfixNetwork] 接收到的数据包长度不足", $"期望长度至少为{HeadLength}，实际长度为{bytes?.Length ?? 0}");
            
            // 记录错误时间
            m_LastErrorTime = Time.realtimeSinceStartup;
            
            // 增加错误计数
            m_ErrorCount++;
            GF.LogWarning("[HotfixNetwork] 网络错误计数", $"当前计数：{m_ErrorCount}/{m_MaxErrorCount}");
            
            // 检查是否达到错误阈值
            if (m_ErrorCount >= m_MaxErrorCount)
            {
                m_ErrorCount = 0; // 重置错误计数
                
                // 如果是账号客户端，则安全关闭连接
                if (client.ClientName == AccountClientName)
                {
                    GF.LogWarning("[HotfixNetwork] 达到最大错误次数，准备重置连接");
                    
                    // 先关闭当前连接，防止继续接收无效数据
                    try 
                    {
                        client.Close(true);
                    }
                    catch (Exception ex) 
                    {
                        GF.LogError("[HotfixNetwork] 关闭连接异常", ex.Message);
                    }
                    
                    // 使用主线程调用退出游戏，避免线程安全问题
                    CoroutineRunner.Instance.StartCoroutine(SafeQuitGame());
                }
            }
            
            return;
        }

        try
        {
            MessageRecvData evet = new MessageRecvData
            {
                Data = bytes.Skip(HeadLength).ToArray(),
                Client = client,
                MsgID = BitConverter.ToInt32(bytes, 6),
                TargetID = BitConverter.ToUInt64(bytes, 8)
            };
            GF.LogInfo("[HotfixNetwork] 接收数据: ", $"ID:{evet.MsgID} DataLength:{evet.Data.Length}");

            lock (s_RecvPackets)
            {
                s_RecvPackets.Add(evet);
            }
        }
        catch (Exception e)
        {
            GF.LogError("[HotfixNetwork] 处理接收数据异常", e.Message);
        }
    }

    // 安全地在主线程中调用QuitGame
    private static IEnumerator SafeQuitGame()
    {
        // 等待一帧确保在主线程中执行
        yield return null;
        
        try
        {
            HomeProcedure homeProcedure = GF.Procedure.GetProcedure<HomeProcedure>() as HomeProcedure;
            if (homeProcedure != null)
            {
                homeProcedure.QuitGame();
            }
        }
        catch (Exception ex)
        {
            GF.LogError("[HotfixNetwork] 退出游戏异常", ex.Message);
        }
    }

    public static void AddListener(MessageID id, Action<MessageRecvData> handle)
    {
        AddListener((int)id, handle);
    }

    public static void AddListener(int id, Action<MessageRecvData> handle)
    {
        if (!s_MessageDispatchs.TryGetValue(id, out MessageCallbackData msg))
        {
            msg = new MessageCallbackData();
            s_MessageDispatchs[id] = msg;
        }

        if (msg.Handler != null && msg.Handler.GetInvocationList().Contains(handle))
        {
            // GF.LogError("重复注册id: " , id.ToString());
            return; // 避免重复注册
        }
        // GF.LogError("注册id: " , id);
        msg.Handler += handle;
        msg.ID = id;
    }

    public static void RemoveListener(MessageID id, Action<MessageRecvData> handle)
    {
        RemoveListener((int)id, handle);
    }

    public static void RemoveListener(int id, Action<MessageRecvData> handle)
    {
        if (s_MessageDispatchs.TryGetValue(id, out MessageCallbackData msg))
        {
            msg.Handler -= handle;
            if (msg.Handler == null) // 只有在完全为空时移除
            {
                // GF.LogError("移除注册id: " , id);
                s_MessageDispatchs.Remove(id);
            }
        }
    }

    private static void ClearListener()
    {
        foreach (var item in s_MessageDispatchs)
        {
            item.Value.Handler = null;
        }
        s_MessageDispatchs.Clear();
    }

    private static void DispatchPacket(MessageRecvData recv)
    {
        try
        {
            // 检查是否需要缓存麻将消息（在游戏初始化期间）
            if (MJMessageCache.TryCache((MessageID)recv.MsgID, recv.Data))
            {
                return; // 消息已缓存，不分发
            }
            
            s_MessageDispatchs.TryGetValue(recv.MsgID, out MessageCallbackData msg);
            if (msg != null)
            {
                msg.Handler(recv);
            }
            else
            {
                GF.LogWarning("[HotfixNetwork] 未找到消息处理器", $" ID:{recv.MsgID}");
            }
        }
        catch (Exception e)
        {
            GF.LogError("[HotfixNetwork] DispatchPacket 处理消息异常", $" ID:{recv.MsgID} Error:{e.Message}\n{e.StackTrace}");
        }
    }

    private static void PackPB3<T>(int messageID, T msgObj, UInt64 u64TargetID, ref byte[] bytes) where T : IMessage
    {
        var bodyMs = new MemoryStream();
        msgObj.WriteTo(bodyMs);
        int nLen = bodyMs.ToArray().Length;

        var byteMs = new MemoryStream();
        MakePacketHeader(messageID, u64TargetID, (uint)nLen);
        byteMs.Write(m_PacketHeader, 0, HeadLength);
        msgObj.WriteTo(byteMs);
        bytes = byteMs.ToArray();

        m_PacketIndex++;
    }

    public static void WriteUInt16(UInt16 v)
    {
        byte[] getdata = BitConverter.GetBytes((short)v);
        for (int i = 0; i < getdata.Length; i++)
        {
            m_PacketHeader[m_nWritePos++] = getdata[i];
        }
    }

    public static void WriteUInt32(UInt32 v)
    {
        for (int i = 0; i < 4; i++)
        {
            m_PacketHeader[m_nWritePos++] = (Byte)(v >> i * 8 & 0xff);
        }
    }

    public static void WriteUInt64(UInt64 v)
    {
        byte[] getdata = BitConverter.GetBytes(v);
        for (int i = 0; i < getdata.Length; i++)
        {
            m_PacketHeader[m_nWritePos++] = getdata[i];
        }
    }

    public static bool MakePacketHeader(int messageID, UInt64 u64Target, UInt32 bodyLength)
    {
        m_nWritePos = 0;
        UInt16 checkCode = 0x71ab;
        UInt32 dwMsgID = (UInt32)messageID;

        WriteUInt16(checkCode);                 //消息分隔标识
        WriteUInt32(bodyLength + HeadLength);   //包长度
        WriteUInt32(dwMsgID);                   //协议号
        WriteUInt64(u64Target);                 //用户id
        WriteUInt32(0);                         //加密类型
        WriteUInt32(0);                         //......
        return true;
    }

    public static void NotifyNetworkStatus(HotfixNetworkClient client, EConnectStatus status, string tips = "")
    {
        if (!client.m_bNotify)
            return;

        switch (status)
        {
            case EConnectStatus.ECS_SUCCESSED:
                GF.LogInfo($"[HotfixNetwork] 连接成功 ", $"client={client.ClientName}");
                client.OnConnectCallback?.Invoke(true);
                client.OnConnectCallback = null;
                Util.GetInstance().CloseWaiting("ConnectServer");
                break;

            case EConnectStatus.ECS_RECON_SUCCESS:
                if (client.ClientName == AccountClientName && client.IsReconnecting)
                {
                    GF.LogInfo($"[HotfixNetwork] 重连接成功 ", $"client={client.ClientName}");
                    client.EndReconnect();
                    // 显示重连成功提示
                    if (!string.IsNullOrEmpty(tips))
                    {
                        GF.UI.ShowToast(tips);
                    }
                }
                break;

            case EConnectStatus.ECS_FAILURE:
                GF.LogInfo($"[HotfixNetwork] 连接失败 ", $"client={client.ClientName}");
                client.OnConnectCallback?.Invoke(false);
                client.OnConnectCallback = null;
                if (!string.IsNullOrEmpty(tips))
                {
                    GF.UI.ShowToast(tips);
                }
                break;

            case EConnectStatus.ESC_DISCONNECT:
            case EConnectStatus.ECS_TCP_NO_CONNNECT:
                GF.LogInfo($"[HotfixNetwork] 连接断开 ", $"client={client.ClientName}");
                if (client.ClientName == AccountClientName && !client.IsReconnectReady)
                {
                    client.PrepareReconnect();
                    GF.LogInfo($"[HotfixNetwork] 派发重连接事件 ", $"client={client.ClientName}");
                    if (!string.IsNullOrEmpty(tips))
                    {
                        GF.UI.ShowToast(tips);
                    }
                }
                break;

            case EConnectStatus.ECS_RECON_FAIL:
                GF.LogInfo($"[HotfixNetwork] 重连失败 ", $"client={client.ClientName}");
                if (client.ClientName == AccountClientName)
                {
                    client.PrepareReconnect();
                    if (!string.IsNullOrEmpty(tips))
                    {
                        GF.UI.ShowToast(tips);
                    }
                }
                break;

            case EConnectStatus.ECS_WEAKLY_CONNECTED:
                GF.LogInfo("[HotfixNetwork] 弱联网环境");
                if (client.ClientName == AccountClientName && !client.IsReconnectReady)
                {
                    client.PrepareReconnect();
                    GF.LogInfo($"[HotfixNetwork] 派发重连接事件 ", $"client={client.ClientName}");
                    if (!string.IsNullOrEmpty(tips))
                    {
                        GF.UI.ShowToast(tips);
                    }
                }
                break;
        }
    }

    public HotfixNetworkClient GetNetworkChannelByName(string name)
    {
        s_Clients.TryGetValue(name, out HotfixNetworkClient value);
        return value;
    }

    private void InitReliableMessageSend()
    {
        m_LossRecvHeartTimes = 0;
        ClearData();
    }

    public void ClearData()
    {
        s_RecvPackets.Clear();
    }

    public override void Dispose()
    {
        // 停止心跳协程
        if (m_HeartbeatCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(m_HeartbeatCoroutine);
            m_HeartbeatCoroutine = null;
        }

        InitReliableMessageSend();
        ClearData();
        // ...existing code...
        GF.LogInfo("[HotfixNetwork] Dispose, 清理所有连接 !!!");
        foreach (var item in s_Clients.Keys)
        {
            Close(item, true);
        }
        s_Clients.Clear();
        Array.Clear(m_PacketHeader, 0, m_PacketHeader.Length);
    }

    public override void Shutdown()
    {
        Dispose();
    }

    public void PauseHeartBeat(bool pause)
    {
        m_IsHeartBeatPaused = pause;
        if (!pause)
        {
            m_LastSendTime = Time.realtimeSinceStartup;
            m_RecvHeartTime = Time.realtimeSinceStartup;
        }
    }

    /// <summary>
    /// 暂停/恢复消息处理（用于应用后台/前台切换）
    /// </summary>
    /// <param name="pause">true=暂停处理消息但保持连接，false=恢复处理消息</param>
    public void PauseMessageProcess(bool pause)
    {
        m_IsMessageProcessPaused = pause;
        
        if (pause)
        {
            GF.LogInfo("[HotfixNetwork] 暂停消息处理（保持连接）");
        }
        else
        {
            GF.LogInfo("[HotfixNetwork] 恢复消息处理", $"待处理消息数量：{s_RecvPackets.Count}");
        }
    }

    /// <summary>
    /// 获取当前消息处理状态
    /// </summary>
    /// <returns>true=消息处理已暂停，false=正常处理消息</returns>
    public bool IsMessageProcessPaused()
    {
        return m_IsMessageProcessPaused;
    }

    /// <summary>
    /// 清空待处理的消息队列
    /// </summary>
    public void ClearRecvPackets()
    {
        lock (s_RecvPackets)
        {
            int count = s_RecvPackets.Count;
            s_RecvPackets.Clear();
            GF.LogInfo("[HotfixNetwork] 清空待处理消息队列", $"清空消息数量：{count}");
        }
    }
}

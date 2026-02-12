using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if (UNITY_WEBGL && !UNITY_EDITOR) || UNITY_EDITOR_WEB
using NativeWebSocket;
#endif

public enum EConnectStatus
{
    ECS_SUCCESSED,  //连接成功
    ECS_FAILURE,    //连接失败
    ESC_DISCONNECT, //连接断开
    ECS_RECON_FAIL, //重连失败
    ECS_RECON_SUCCESS, //重连成功
    ECS_WEAKLY_CONNECTED = 10, //弱联网环境
    ECS_STRONGLY_CONNECTED = 11, //强联网环境
    ECS_TCP_NO_CONNNECT,
}

public delegate void ConnectCallback(bool status);

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class HotfixNetworkClient
{
    #region 公共成员
    public string m_IP;
    public int m_Port;
    public string ClientName { get; private set; }
    public ConnectCallback OnConnectCallback = null;
    public bool m_bNotify = false;
    public bool IsReconnectReady { get; private set; } = false;
    public bool IsReconnecting { get; private set; } = false;
    private bool m_IsConnecting = false;
    public bool IsConnecting => m_IsConnecting;
    private DateTime m_LastConnectTime = DateTime.MinValue;
    private readonly TimeSpan c_ConnectCooldown = TimeSpan.FromSeconds(1);
    private const int PacketHeaderSize = 26;
    private int m_RecvLen = 0;
    private int m_DataLen = 0;
    private readonly byte[] m_RecvBuffer = new byte[512000 * 2];
    private readonly byte[] m_DataBuffer = new byte[1024000 * 2];
    private static readonly Queue<byte[]> m_SendPackets = new Queue<byte[]>();
    private static bool m_IsSending = false;
    #endregion

#if (UNITY_WEBGL && !UNITY_EDITOR) || UNITY_EDITOR_WEB
    private WebSocket m_WebSocket;
#else
    private System.Net.Sockets.TcpClient m_Tcp;
#endif

    public HotfixNetworkClient(string ip, int port, string name)
    {
        m_IP = ip;
        m_Port = port;
        ClientName = name;
    }

    public async Task ConnectAsync()
    {
        var now = DateTime.Now;
        if (now - m_LastConnectTime < c_ConnectCooldown)
        {
            GF.LogInfo($"[HotfixClient] ConnectAsync冷却中", $"client={ClientName}, 忽略此次连接请求");
            return;
        }
        if (m_IsConnecting)
        {
            GF.LogInfo($"[HotfixClient] ConnectAsync已经在连接中", $"client={ClientName}, 忽略此次连接请求");
            return;
        }
        if (IsConnectOK())
        {
            GF.LogInfo($"[HotfixClient] ConnectAsync已连接", $"client={ClientName}, 忽略此次连接请求");
            return;
        }
        m_IsConnecting = true;
        m_LastConnectTime = now;
#if (UNITY_WEBGL && !UNITY_EDITOR) || UNITY_EDITOR_WEB
        string wsUrl = $"ws://{m_IP}:{m_Port}/ws";
        if(Const.CurrentServerType == Const.ServerType.外网服)
        {
            wsUrl = $"wss://{m_IP}/ws";
        }
        m_WebSocket = new WebSocket(wsUrl);
        GF.LogInfo($"[HotfixClient] WebSocket ConnectAsync", $"client={ClientName}, wsUrl={wsUrl}");
        m_WebSocket.OnOpen += () =>
        {
            if (!m_bNotify)
            {
                m_bNotify = true;
                HotfixNetworkComponent.NotifyNetworkStatus(this, EConnectStatus.ECS_SUCCESSED);
            }
            else if (IsReconnectReady)
            {
                HotfixNetworkComponent.NotifyNetworkStatus(this, EConnectStatus.ECS_RECON_SUCCESS, "网络重连成功");
            }
            OnConnectAsync().ConfigureAwait(false);
        };
        m_WebSocket.OnError += (e) =>
        {
            GF.LogError("[HotfixClient] WebSocket ConnectAsync错误", e);
            HotfixNetworkComponent.NotifyNetworkStatus(this, IsReconnectReady ? EConnectStatus.ECS_RECON_FAIL : EConnectStatus.ECS_FAILURE, e);
            m_IsConnecting = false;
        };
        m_WebSocket.OnClose += (e) =>
        {
            GF.LogError("[HotfixClient] WebSocket 断开", $"client={ClientName} code={e}");
            HotfixNetworkComponent.NotifyNetworkStatus(this, EConnectStatus.ESC_DISCONNECT);
            m_IsConnecting = false;
        };
        m_WebSocket.OnMessage += (bytes) =>
        {
            GF.LogInfo("[HotfixClient] WebSocket 收到消息", $"client={ClientName}, bytes.Length={bytes.Length}");
            Array.Copy(bytes, 0, m_DataBuffer, m_DataLen, bytes.Length);
            m_DataLen += bytes.Length;
            bool hasMorePackets = true;
            while (hasMorePackets && IsConnectOK())
            {
                hasMorePackets = MakeRealPacket();
            }
        };
        try
        {
            await m_WebSocket.Connect();
        }
        catch (Exception e)
        {
            GF.LogError("[HotfixClient] WebSocket ConnectAsync异常", e.Message);
            HotfixNetworkComponent.NotifyNetworkStatus(this, IsReconnectReady ? EConnectStatus.ECS_RECON_FAIL : EConnectStatus.ECS_FAILURE, e.Message);
            m_IsConnecting = false;
        }
        finally
        {
            m_IsConnecting = false;
        }
#else
        if (m_Tcp == null)
            m_Tcp = new System.Net.Sockets.TcpClient();
        System.Net.IPAddress address = null;
        try
        {
            address = System.Net.IPAddress.Parse(m_IP);
        }
        catch (ArgumentNullException e)
        {
            GF.LogError("[HotfixClient] Set Connect to ", $"{m_IP}, IP address can not be null! {e.Message}");
            m_IsConnecting = false;
            return;
        }
        catch (FormatException)
        {
            address = (await System.Net.Dns.GetHostAddressesAsync(m_IP))[0];
        }
        catch (Exception e)
        {
            GF.LogError("[HotfixClient] Set Connect to", $" {m_IP}, Exception! {e.Message}");
            m_IsConnecting = false;
            return;
        }
        try
        {
            var connectTask = m_Tcp.ConnectAsync(address, m_Port);
            var timeoutTask = Task.Delay(5000);
            if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
            {
                GF.LogError("[HotfixClient] ConnectAsync超时", $"client={ClientName}");
                Close();
                HotfixNetworkComponent.NotifyNetworkStatus(this, EConnectStatus.ECS_FAILURE, "连接服务器超时");
                m_IsConnecting = false;
                return;
            }
            await connectTask;
            if (!m_bNotify)
            {
                m_bNotify = true;
                HotfixNetworkComponent.NotifyNetworkStatus(this, EConnectStatus.ECS_SUCCESSED);
            }
            else if (IsReconnectReady)
            {
                HotfixNetworkComponent.NotifyNetworkStatus(this, EConnectStatus.ECS_RECON_SUCCESS, "网络重连成功");
            }
            await OnConnectAsync();
        }
        catch (Exception e)
        {
            GF.LogError("[HotfixClient] ConnectAsync to ", $"{m_IP}, Exception! {e.Message}");
            HotfixNetworkComponent.NotifyNetworkStatus(this, IsReconnectReady ? EConnectStatus.ECS_RECON_FAIL : EConnectStatus.ECS_FAILURE);
            m_IsConnecting = false;
            return;
        }
        finally
        {
            m_IsConnecting = false;
        }
#endif
    }

    public void Close(bool bNotify = true)
    {
        GF.LogError("[HotfixClient] Close", $",name={ClientName} bNotify={bNotify}");
#if (UNITY_WEBGL && !UNITY_EDITOR) || UNITY_EDITOR_WEB
        if (m_WebSocket == null)
            return;
        try
        {
            m_WebSocket.Close();
        }
        catch { }
        m_WebSocket = null;
#else
        if (m_Tcp == null)
            return;
        try
        {
            m_Tcp.Close();
            m_Tcp.Dispose();
        }
        catch { }
        m_Tcp = null;
#endif
        OnConnectCallback = null;
        m_RecvLen = 0;
        m_DataLen = 0;
        if (m_SendPackets != null)
            m_SendPackets.Clear();
        m_bNotify = bNotify;
        m_IsSending = false;
    }

    public void Dispose()
    {
        Close(false);
        Array.Clear(m_RecvBuffer, 0, m_RecvBuffer.Length);
        Array.Clear(m_DataBuffer, 0, m_DataBuffer.Length);
    }

    public bool IsConnectOK()
    {
#if (UNITY_WEBGL && !UNITY_EDITOR) || UNITY_EDITOR_WEB
        return m_WebSocket != null && m_WebSocket.State == WebSocketState.Open;
#else
        return m_Tcp?.Connected ?? false;
#endif
    }

    private async Task OnConnectAsync()
    {
#if (UNITY_WEBGL && !UNITY_EDITOR) || UNITY_EDITOR_WEB
        await Task.CompletedTask;
#else
        if (m_Tcp.Client == null || !m_Tcp.Client.Connected)
        {
            GF.LogError("[HotfixClient] OnConnect Client is not connected!");
            HotfixNetworkComponent.NotifyNetworkStatus(this, EConnectStatus.ECS_FAILURE);
            return;
        }
        m_Tcp.ReceiveBufferSize = 1024 * 512;
        m_Tcp.SendBufferSize = 1024 * 512;
        try
        {
            var stream = m_Tcp.GetStream();
            await ReadAsync(stream);
        }
        catch (Exception e)
        {
            GF.LogError($"[HotfixClient] OnConnect BeginRead Exception, msg={e.Message}!");
            HotfixNetworkComponent.NotifyNetworkStatus(this, EConnectStatus.ECS_FAILURE);
            Close();
        }
#endif
    }

#if (UNITY_WEBGL && !UNITY_EDITOR) || UNITY_EDITOR_WEB
    // WebSocket 不需要 ReadAsync
#else
    private async Task ReadAsync(System.Net.Sockets.NetworkStream stream)
    {
        int consecutiveZeroBytesCount = 0;
        const int maxConsecutiveZeroBytesAllowed = 5;
        while (IsConnectOK())
        {
            try
            {
                m_RecvLen = await stream.ReadAsync(m_RecvBuffer, 0, m_RecvBuffer.Length);
                if (m_RecvLen == 0)
                {
                    consecutiveZeroBytesCount++;
                    GF.LogWarning("[HotfixClient] ReadAsync接收到0字节数据", $"连续次数:{consecutiveZeroBytesCount}");
                    if (consecutiveZeroBytesCount >= maxConsecutiveZeroBytesAllowed)
                    {
                        GF.LogError("[HotfixClient] ReadAsync连续多次接收到0字节数据", $"断开连接");
                        Close();
                        HotfixNetworkComponent.NotifyNetworkStatus(this, EConnectStatus.ESC_DISCONNECT);
                        return;
                    }
                    await Task.Delay(100);
                    continue;
                }
                consecutiveZeroBytesCount = 0;
                if (m_RecvLen > 0)
                {
                    Array.Copy(m_RecvBuffer, 0, m_DataBuffer, m_DataLen, m_RecvLen);
                    m_DataLen += m_RecvLen;
                    m_RecvLen = 0;
                    bool hasMorePackets = true;
                    while (hasMorePackets && IsConnectOK())
                    {
                        hasMorePackets = MakeRealPacket();
                    }
                }
                else
                {
                    GF.LogError("[HotfixClient] ReadAsync失败", $"服务器关闭连接! name={ClientName}");
                    Close();
                    HotfixNetworkComponent.NotifyNetworkStatus(this, EConnectStatus.ESC_DISCONNECT);
                    return;
                }
            }
            catch (ObjectDisposedException)
            {
                GF.LogWarning("[HotfixClient] ReadAsync对象已释放", $"name={ClientName}");
                return;
            }
            catch (Exception e)
            {
                GF.LogError($"[HotfixClient] ReadAsync异常", $"msg={e.Message}! name={ClientName}");
                try { Close(); } catch { }
                HotfixNetworkComponent.NotifyNetworkStatus(this, EConnectStatus.ESC_DISCONNECT);
                return;
            }
        }
    }
#endif

    private bool MakeRealPacket()
    {
        if (m_DataLen < PacketHeaderSize)
            return false;
        try
        {
            UInt16 CheckCode = BitConverter.ToUInt16(m_DataBuffer, 0);
            if (CheckCode != 0x71ab)
            {
                GF.LogError("[HotfixClient] MakeRealPacket校验码错误", $"期望值:0x71ab, 实际值:0x{CheckCode:X4}");
                m_DataLen = 0;
                return false;
            }
            UInt32 nPacketSize = BitConverter.ToUInt32(m_DataBuffer, 2);
            if (nPacketSize < PacketHeaderSize || nPacketSize > 1024000)
            {
                GF.LogError("[HotfixClient] MakeRealPacket包大小错误", $"包大小:{nPacketSize}");
                m_DataLen = 0;
                return false;
            }
            if (nPacketSize > m_DataLen)
                return false;
            byte[] realPacket = new byte[(int)nPacketSize];
            Array.Copy(m_DataBuffer, 0, realPacket, 0, (int)nPacketSize);
            Array.Copy(m_DataBuffer, (int)nPacketSize, m_DataBuffer, 0, m_DataLen - (int)nPacketSize);
            m_DataLen -= (int)nPacketSize;
            if (IsConnectOK())
            {
                HotfixNetworkComponent.Recv(this, realPacket);
            }
            return true;
        }
        catch (Exception ex)
        {
            GF.LogError("[HotfixClient] MakeRealPacket处理异常", ex.Message);
            m_DataLen = 0;
            return false;
        }
    }

    public void PrepareReconnect()
    {
        IsReconnectReady = true;
        IsReconnecting = true;
        GF.LogInfo("[HotfixClient] PrepareReconnect", $"client={ClientName}, IsReconnectReady={IsReconnectReady}, IsReconnecting={IsReconnecting}");
    }

    public void EndReconnect()
    {
        IsReconnecting = false;
        IsReconnectReady = false;
        GF.LogInfo("[HotfixClient] EndReconnect", $"client={ClientName}, IsReconnectReady={IsReconnectReady}, IsReconnecting={IsReconnecting}");
    }

    public void AddPacket(byte[] bytes)
    {
        if (bytes == null)
            return;
#if (UNITY_WEBGL && !UNITY_EDITOR) || UNITY_EDITOR_WEB
        if (m_WebSocket != null && m_WebSocket.State == WebSocketState.Open)
        {
            m_WebSocket.Send(bytes);
        }
#else
        lock (m_SendPackets)
        {
            m_SendPackets.Enqueue(bytes);
        }
        if (!m_IsSending)
        {
            m_IsSending = true;
            byte[] tPacket = null;
            lock (m_SendPackets)
            {
                tPacket = m_SendPackets.Dequeue();
            }
            try
            {
                var stream = m_Tcp.GetStream();
                stream.BeginWrite(tPacket, 0, tPacket.Length, OnAsyncWrite, stream);
            }
            catch (System.IO.IOException e)
            {
                GF.LogError("[HotfixClient] AddPacket NetworkStream.BeginWrite, Exception=", e.Message);
                Close();
                HotfixNetworkComponent.NotifyNetworkStatus(this, EConnectStatus.ESC_DISCONNECT);
                return;
            }
        }
#endif
    }

#if (UNITY_WEBGL && !UNITY_EDITOR) || UNITY_EDITOR_WEB
    public void MessageQueue()
    {
        // m_WebSocket?.DispatchMessageQueue();
    }
#else
    private void OnAsyncWrite(IAsyncResult ar)
    {
        System.Net.Sockets.NetworkStream stream;
        try
        {
            stream = m_Tcp.GetStream();
            stream.EndWrite(ar);
        }
        catch (System.IO.IOException e)
        {
            GF.LogError("[HotfixClient] OnAsyncWrite:EndWrite, Exception=", e.Message);
            Close();
            HotfixNetworkComponent.NotifyNetworkStatus(this, EConnectStatus.ESC_DISCONNECT);
            return;
        }
        if (m_SendPackets.Count <= 0)
        {
            m_IsSending = false;
            return;
        }
        else
        {
            byte[] tPacket;
            lock (m_SendPackets)
            {
                tPacket = m_SendPackets.Dequeue();
            }
            try
            {
                stream.BeginWrite(tPacket, 0, tPacket.Length, OnAsyncWrite, stream);
            }
            catch (System.IO.IOException e)
            {
                GF.LogError("[HotfixClient] OnAsyncWrite:BeginWrite, Exception=", e.Message);
                return;
            }
        }
    }
#endif
}


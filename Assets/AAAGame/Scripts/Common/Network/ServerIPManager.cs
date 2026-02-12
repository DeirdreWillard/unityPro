using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using System.Net.Sockets;

/// <summary>
/// 服务器IP管理器：负责获取、测试和选择最优IP线路
/// </summary>
public class ServerIPManager : MonoBehaviour
{
    #region 单例实现
    private static ServerIPManager s_Instance;
    public static ServerIPManager Ins
    {
        get
        {
            if (s_Instance == null)
            {
                GameObject obj = new GameObject("ServerIPManager");
                s_Instance = obj.AddComponent<ServerIPManager>();
                DontDestroyOnLoad(obj);
            }
            return s_Instance;
        }
    }
    #endregion

    #region 常量定义
    private const int c_MaxPingAttempts = 3;
    private const float c_PingInterval = 0.5f;
    private const int c_RequestTimeout = 3;
    #endregion

    #region 私有字段
    [SerializeField] private string m_IPListUrl = string.Empty;
    private List<ServerIPInfo> m_ServerIPList = new List<ServerIPInfo>();
    private bool m_IsTestingIPs = false;
    private ServerIPInfo m_OptimalServer;
    private Action<bool> m_OnIPListReadyCallback;
    #endregion

    #region 公开属性
    public bool IsIPListReady { get; private set; } = false;
    public int IPCount => m_ServerIPList.Count;
    #endregion

    #region 数据结构
    [Serializable]
    public class ServerIPInfo
    {
        public string m_IP;
        public int m_Port;
        public long m_Ping = 9999;
        public bool m_Available = false;
        public int m_FailCount = 0;
    }
    #endregion

    #region 公开方法
    /// <summary>
    /// 初始化IP管理器
    /// </summary>
    /// <param name="_ipListUrl">IP列表URL地址</param>
    /// <param name="_callback">初始化完成回调</param>
    public void Initialize(string _ipListUrl, Action<bool> _callback = null)
    {
        m_IPListUrl = _ipListUrl;
        m_OnIPListReadyCallback = _callback;
        StartCoroutine(FetchServerIPList());
    }

    /// <summary>
    /// 获取最优服务器IP信息
    /// </summary>
    /// <returns>最优IP和端口</returns>
    public (string ip, int port) GetOptimalServer()
    {
        // 如果不需要IP列表，直接返回默认服务器
        if(!Const.ServerInfo_IpListFlag){
            return (Const.ServerInfo_ServerIP, Const.ServerInfo_ServerPort);
        }

        if (m_OptimalServer != null)
        {
            // 默认第一个为默认服务器
            GF.LogInfo("[ServerIPManager] 获取最优服务器: ", $"{m_OptimalServer.m_IP}:{m_OptimalServer.m_Port}");
            Const.ServerInfo_ServerIP = m_OptimalServer.m_IP;
            Const.ServerInfo_ServerPort = m_OptimalServer.m_Port;
            Const.ServerInfo_BackEndIP = m_OptimalServer.m_IP;
            GF.LogInfo("[ServerIPManager] 设置默认服务器: ", $"{Const.ServerInfo_ServerIP}:{Const.ServerInfo_ServerPort}");
            return (m_OptimalServer.m_IP, m_OptimalServer.m_Port);
        }
        
        // 如果没有最优服务器，使用默认值
        return (Const.ServerInfo_ServerIP, Const.ServerInfo_ServerPort);
    }

    /// <summary>
    /// 标记当前IP不可用，并切换到下一个IP
    /// </summary>
    /// <returns>新的IP和端口</returns>
    public (string ip, int port) MarkCurrentServerFailed()
    {
        if (m_OptimalServer != null)
        {
            m_OptimalServer.m_FailCount++;
            m_OptimalServer.m_Available = false;
            SelectOptimalServer();
        }
        
        return GetOptimalServer();
    }

    /// <summary>
    /// 重新测试所有服务器
    /// </summary>
    /// <param name="_callback">完成回调</param>
    public void RefreshServers(Action<bool> _callback = null)
    {
        StartCoroutine(TestServerIPsCoroutine(_callback));
    }
    #endregion

    #region 私有方法
    private void OnDestroy()
    {
        StopAllCoroutines();
        s_Instance = null;
    }

    private IEnumerator FetchServerIPList()
    {
        GF.LogInfo($"[ServerIPManager] 开始请求IP列表: {m_IPListUrl}");
        IsIPListReady = false;
        
        using (UnityWebRequest request = UnityWebRequest.Get(m_IPListUrl))
        {
            request.timeout = c_RequestTimeout;
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string ipListText = request.downloadHandler.text;
                ParseServerIPList(ipListText);
                
                if (m_ServerIPList.Count > 0)
                {
                    yield return StartCoroutine(TestServerIPsCoroutine(null));
                }
                else
                {
                    // 回退到默认IP
                    AddDefaultServerIP();
                    SelectOptimalServer();
                }
            }
            else
            {
                GF.LogError("[ServerIPManager] 获取IP列表失败: ", request.error);
                AddDefaultServerIP();
                SelectOptimalServer();
            }
        }
        
        IsIPListReady = true;
        m_OnIPListReadyCallback?.Invoke(m_ServerIPList.Count > 0);
    }

    private void ParseServerIPList(string _ipListText)
    {
        m_ServerIPList.Clear();
        GF.LogInfo("[ServerIPManager] IP列表: ", _ipListText);
        try
        {
                // 简单处理JSON数组，去掉外层的方括号
                string content = _ipListText.Substring(1, _ipListText.Length - 2);
                // 分割IP地址
                string[] ips = content.Split(',');
                
                foreach (string ip in ips)
                {
                    // 清除引号和多余空格
                    string cleanIp = ip.Replace("\"", "").Replace("'", "").Trim();
                    
                    if (string.IsNullOrEmpty(cleanIp))
                        continue;
                    
                    // 添加IP地址，使用默认端口
                    m_ServerIPList.Add(new ServerIPInfo 
                    { 
                        m_IP = cleanIp, 
                        m_Port = Const.ServerInfo_ServerPort,
                        m_Available = true
                    });
                    
                    GF.LogInfo("[ServerIPManager] 添加IP: ", $"{cleanIp}:{Const.ServerInfo_ServerPort}");
                }
                
                // 如果IP列表为空，添加默认IP
                if (m_ServerIPList.Count == 0)
                {
                    AddDefaultServerIP();
                }
        }
        catch (Exception ex)
        {
            GF.LogError("[ServerIPManager] 解析IP列表异常: ", ex.Message);
            // 解析失败时添加默认IP
            AddDefaultServerIP();
        }
        
        GF.LogInfo($"[ServerIPManager] 解析到 {m_ServerIPList.Count} 个服务器IP");
    }

    private void AddDefaultServerIP()
    {
        // 检查默认IP是否已存在
        bool defaultExists = m_ServerIPList.Any(s => s.m_IP == Const.ServerInfo_ServerIP && 
                                                s.m_Port == Const.ServerInfo_ServerPort);
        
        if (!defaultExists)
        {
            m_ServerIPList.Add(new ServerIPInfo
            {
                m_IP = Const.ServerInfo_ServerIP,
                m_Port = Const.ServerInfo_ServerPort,
                m_Available = true
            });
            GF.LogInfo("[ServerIPManager] 添加默认服务器IP");
        }
    }

    private IEnumerator TestServerIPsCoroutine(Action<bool> _callback)
    {
        if (m_IsTestingIPs)
        {
            _callback?.Invoke(false);
            yield break;
        }
            
        m_IsTestingIPs = true;
        GF.LogInfo("[ServerIPManager] 开始测试所有服务器IP线路质量");
        
        // 创建一个副本以避免遍历时修改集合的问题
        List<ServerIPInfo> serverListCopy = new List<ServerIPInfo>(m_ServerIPList);
        
        foreach (ServerIPInfo serverInfo in serverListCopy)
        {
            yield return StartCoroutine(TestServerPing(serverInfo.m_IP, serverInfo.m_Port));
        }
        
        // 根据ping值排序
        m_ServerIPList.Sort((a, b) => a.m_Ping.CompareTo(b.m_Ping));
        
        string ipInfoLog = "IP线路测试结果:\n";
        foreach (var ip in m_ServerIPList)
        {
            ipInfoLog += $"IP:{ip.m_IP}:{ip.m_Port} Ping:{ip.m_Ping}ms 可用:{ip.m_Available}\n";
        }
        GF.LogInfo(ipInfoLog);
        
        SelectOptimalServer();
        m_IsTestingIPs = false;
        
        _callback?.Invoke(true);
    }

    private IEnumerator TestServerPing(string ip, int port)
    {
        ServerIPInfo serverInfo = m_ServerIPList.FirstOrDefault(s => s.m_IP == ip && s.m_Port == port);
        
        if (serverInfo == null)
        {
            GF.LogError("[ServerIPManager] 服务器 ", $"{ip}:{port} 不存在");
            yield break;
        }
        
        TcpClient client = new TcpClient();
        float startTime = 0;
        
        try
        {
            // 开始计时
            startTime = Time.realtimeSinceStartup;
            
            // 设置连接
            client.BeginConnect(ip, port, null, null);
        }
        catch (Exception ex)
        {
            GF.LogError("[ServerIPManager] 连接服务器 ", $"{ip}:{port} 异常: {ex.Message}");
            serverInfo.m_Ping = 9999;
            serverInfo.m_Available = false;
            
            try { client.Close(); } catch { }
            
            // 更新服务器信息
            UpdateServerInfo(serverInfo);
            
            GF.LogInfo("[ServerIPManager] 服务器 ", $"{ip}:{port} 测试结果: Ping={serverInfo.m_Ping}ms, 可用={serverInfo.m_Available}");
            yield break;
        }
        
        // 等待连接完成或超时
        float timeoutSeconds = 5.0f;
        float elapsedTime = 0f;
        
        while (!client.Connected && elapsedTime < timeoutSeconds)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 处理连接结果
        try
        {
            if (client.Connected)
            {
                // 连接成功，计算延迟
                float pingTime = (Time.realtimeSinceStartup - startTime) * 1000;
                serverInfo.m_Ping = Mathf.RoundToInt(pingTime);
                serverInfo.m_Available = true;
            }
            else
            {
                // 连接失败
                serverInfo.m_Ping = 9999;
                serverInfo.m_Available = false;
            }
        }
        catch (Exception ex)
        {
            GF.LogError("[ServerIPManager] 处理连接结果异常: ", ex.Message);
            serverInfo.m_Ping = 9999;
            serverInfo.m_Available = false;
        }
        finally
        {
            // 确保关闭连接
            try { client.Close(); } catch { }
        }
        
        // 更新服务器信息
        UpdateServerInfo(serverInfo);
        
        GF.LogInfo("[ServerIPManager] 服务器 ", $"{ip}:{port} 测试结果: Ping={serverInfo.m_Ping}ms, 可用={serverInfo.m_Available}");
    }
    
    private void UpdateServerInfo(ServerIPInfo serverInfo)
    {
        lock (m_ServerIPList)
        {
            for (int i = 0; i < m_ServerIPList.Count; i++)
            {
                if (m_ServerIPList[i].m_IP == serverInfo.m_IP && m_ServerIPList[i].m_Port == serverInfo.m_Port)
                {
                    m_ServerIPList[i] = serverInfo;
                    break;
                }
            }
        }
    }

    private void SelectOptimalServer()
    {
        // 找到可用且ping值最小的服务器
        ServerIPInfo bestServer = m_ServerIPList
            .Where(s => s.m_Available && s.m_FailCount < 3)
            .OrderBy(s => s.m_Ping)
            .FirstOrDefault();
        
        if (bestServer == null)
        {
            GF.LogWarning("[ServerIPManager] 没有可用的服务器IP，将尝试使用默认服务器");
            
            // 使用默认服务器
            bestServer = m_ServerIPList.FirstOrDefault(s => 
                s.m_IP == Const.ServerInfo_ServerIP && 
                s.m_Port == Const.ServerInfo_ServerPort);
                
            if (bestServer == null && m_ServerIPList.Count > 0)
            {
                // 如果没有默认服务器，使用列表中的第一个服务器
                bestServer = m_ServerIPList[0];
                bestServer.m_Available = true; // 强制设置为可用
                GF.LogWarning("[ServerIPManager] 强制使用未测试成功的IP: ", $"{bestServer.m_IP}:{bestServer.m_Port}");
            }
            else if (bestServer != null)
            {
                // 如果有默认服务器但测试失败，强制设置为可用
                bestServer.m_Available = true;
                GF.LogWarning("[ServerIPManager] 强制使用默认服务器: ", $"{bestServer.m_IP}:{bestServer.m_Port}");
            }
            else
            {
                // 如果IP列表为空，创建一个默认服务器
                bestServer = new ServerIPInfo
                {
                    m_IP = Const.ServerInfo_ServerIP,
                    m_Port = Const.ServerInfo_ServerPort,
                    m_Available = true,
                    m_Ping = 5000
                };
                m_ServerIPList.Add(bestServer);
                GF.LogWarning("[ServerIPManager] 创建并使用默认服务器: ", $"{bestServer.m_IP}:{bestServer.m_Port}");
            }
        }
        
        if (bestServer != null)
        {
            m_OptimalServer = bestServer;
            GF.LogInfo("[ServerIPManager] 已选择服务器 ", $"{bestServer.m_IP}:{bestServer.m_Port} Ping:{bestServer.m_Ping}ms 可用:{bestServer.m_Available}");
        }
    }
    #endregion
} 
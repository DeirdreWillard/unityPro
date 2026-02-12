﻿using System.Collections.Generic;
using UnityEngine;

public class LoadingManager : MonoBehaviour
{
    private static LoadingManager _instance;

    public static LoadingManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var obj = new GameObject("LoadingManager");
                _instance = obj.AddComponent<LoadingManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    // 记录每种协议当前是否在请求中
    private Dictionary<int, bool> _requestingDict = new Dictionary<int, bool>();

    // 协议请求与回包ID的映射关系（一对多）
    private Dictionary<int, List<int>> _requestResponseMap = new Dictionary<int, List<int>>
    {
        { (int)MessageID.MsgLoginReq,
            new List<int> {
                (int)MessageID.Msg_Error,
                (int)MessageID.MsgLoginAck } },

        { (int)MessageID.Msg_DeskListRq,
            new List<int> {
                (int)MessageID.Msg_DeskListRs } },

        { (int)MessageID.Msg_MyLeagueListRq,
            new List<int> {
                (int)MessageID.Msg_MyLeagueListRs } },

        { (int)MessageID.Msg_EnterDeskRq,
            new List<int> {
                (int)MessageID.Msg_Error,
                (int)MessageID.Msg_EnterNiuNiuDeskRs,
                (int)MessageID.Msg_EnterZjhDeskRs,
                (int)MessageID.Msg_EnterCompareChickenDeskRs,
                (int)MessageID.Msg_EnterTexasDeskRs,
                (int)MessageID.Msg_EnterMJDeskRs
            } },

        { (int)MessageID.Msg_AutoCompareCardRq,
            new List<int> {
                (int)MessageID.Msg_AutoCompareCardRs ,
                (int)MessageID.Msg_Error} },

        { (int)MessageID.Msg_PlayBackRq,
            new List<int> {
                (int)MessageID.Msg_PlayBackRs ,
                (int)MessageID.Msg_PlayBackRs_zjh,
                (int)MessageID.Msg_TexasPokerPlayBackRs,
                (int)MessageID.Msg_CKPlayBackRs} },
        
    };

    // 修改为一对多映射
    private Dictionary<int, List<int>> _responseToRequestMap = new Dictionary<int, List<int>>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化响应到请求的映射
        InitializeResponseMap();

        // 注册网络消息处理函数
        RegisterNetworkEvents();
    }

    /// <summary>
    /// 初始化响应到请求的映射关系
    /// </summary>
    private void InitializeResponseMap()
    {
        _responseToRequestMap.Clear();
        foreach (var pair in _requestResponseMap)
        {
            int requestId = pair.Key;
            foreach (int responseId in pair.Value)
            {
                if (!_responseToRequestMap.ContainsKey(responseId))
                {
                    _responseToRequestMap[responseId] = new List<int>();
                }
                _responseToRequestMap[responseId].Add(requestId);
            }
        }
    }

    private void OnDestroy()
    {
        // 注销网络消息处理函数
        UnregisterNetworkEvents();
    }

    /// <summary>
    /// 注册网络消息监听
    /// </summary>
    private void RegisterNetworkEvents()
    {
        // 为所有可能的响应ID注册监听
        foreach (var pair in _requestResponseMap)
        {
            foreach (int responseId in pair.Value)
            {
                HotfixNetworkComponent.AddListener((MessageID)responseId, OnResponseReceived);
            }
        }
    }

    /// <summary>
    /// 注销网络消息监听
    /// </summary>
    private void UnregisterNetworkEvents()
    {
        // 注销所有响应监听
        foreach (var pair in _requestResponseMap)
        {
            foreach (int responseId in pair.Value)
            {
                HotfixNetworkComponent.RemoveListener((MessageID)responseId, OnResponseReceived);
            }
        }
    }

    /// <summary>
    /// 处理网络请求发送
    /// </summary>
    /// <param name="msgId">请求消息ID</param>
    public void OnRequestSent(int msgId)
    {
        // 检查是否是需要显示loading的协议
        if (_requestResponseMap.ContainsKey(msgId))
        {
            // 标记该消息ID正在请求中
            _requestingDict[msgId] = true;

            // 生成请求专用的loading key
            string loadingKey = $"req_{msgId}";
            
            // 获取协议专用的loading文本
            string content = GetLoadingTextForRequest(msgId);
            
            // 显示加载中提示，使用唯一key
            Util.GetInstance().ShowWaiting(content, loadingKey);

            GF.LogInfo("显示Loading", $"MsgID: {(MessageID)msgId}, Key: {loadingKey}");
        }
    }

    /// <summary>
    /// 根据请求ID获取合适的Loading文本
    /// </summary>
    private string GetLoadingTextForRequest(int msgId)
    {
        // 根据不同的请求类型，返回适合的文本提示
        switch ((MessageID)msgId)
        {
            // case MessageID.MsgLoginReq:
            //     return "正在登录...";
            // case MessageID.Msg_EnterDeskRq:
            //     return "正在进入游戏...";
            // case MessageID.Msg_PlayBackRq:
            //     return "正在加载回放...";
            default:
                return "";
        }
    }

    /// <summary>
    /// 处理网络响应接收
    /// </summary>
    /// <param name="data">接收到的消息数据</param>
    private void OnResponseReceived(MessageRecvData data)
    {
        int responseId = data.MsgID;

        if (_responseToRequestMap.TryGetValue(responseId, out List<int> requestIds))
        {
            foreach (int requestId in requestIds)
            {
                if (_requestingDict.TryGetValue(requestId, out bool isRequesting) && isRequesting)
                {
                    // 标记请求已完成
                    _requestingDict[requestId] = false;
                    
                    // 生成对应的loading key
                    string loadingKey = $"req_{requestId}";
                    
                    // 关闭加载中提示
                    Util.GetInstance().CloseWaiting(loadingKey);
                    
                    GF.LogInfo("关闭Loading", $"ResponseID: {(MessageID)responseId}, RequestID: {(MessageID)requestId}, Key: {loadingKey}");
                }
            }
        }
    }

    /// <summary>
    /// 添加一对多的请求-响应映射
    /// </summary>
    /// <param name="requestId">请求ID</param>
    /// <param name="responseIds">对应的多个响应ID</param>
    public void AddRequestResponseMapping(int requestId, List<int> responseIds)
    {
        if (_requestResponseMap.ContainsKey(requestId))
        {
            // 更新已有映射
            _requestResponseMap[requestId] = responseIds;
        }
        else
        {
            // 添加新映射
            _requestResponseMap.Add(requestId, responseIds);
        }

        // 更新反向映射并注册监听
        foreach (int responseId in responseIds)
        {
            if (!_responseToRequestMap.ContainsKey(responseId))
            {
                _responseToRequestMap[responseId] = new List<int>();
            }
            _responseToRequestMap[responseId].Add(requestId);
            HotfixNetworkComponent.AddListener((MessageID)responseId, OnResponseReceived);
        }
    }

    /// <summary>
    /// 添加单个请求-响应映射
    /// </summary>
    /// <param name="requestId">请求ID</param>
    /// <param name="responseId">响应ID</param>
    public void AddResponseToRequest(int requestId, int responseId)
    {
        if (!_requestResponseMap.ContainsKey(requestId))
        {
            _requestResponseMap[requestId] = new List<int>();
        }

        if (!_requestResponseMap[requestId].Contains(responseId))
        {
            _requestResponseMap[requestId].Add(responseId);
            if (!_responseToRequestMap.ContainsKey(responseId))
            {
                _responseToRequestMap[responseId] = new List<int>();
            }
            _responseToRequestMap[responseId].Add(requestId);
            HotfixNetworkComponent.AddListener((MessageID)responseId, OnResponseReceived);
        }
    }
}

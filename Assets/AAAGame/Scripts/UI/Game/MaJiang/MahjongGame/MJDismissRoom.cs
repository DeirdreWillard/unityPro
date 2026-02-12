using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using NetMsg;

/// <summary>
/// 解散房间面板 - 通用组件
/// 完全独立处理解散房间逻辑,不依赖其他游戏模块
/// </summary>
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class MJDismissRoom : UIFormBase
{
    #region UI组件
    public List<GameObject> DissPlayerItems;
    public Text dissDeskCD;
    public GameObject waiting;
    public GameObject yesBtn;
    public GameObject noBtn;
    #endregion

    #region 数据成员
    private long currentPlayerId;                  // 当前玩家ID
    private List<BasePlayer> allPlayers;           // 所有玩家列表
    private List<long> agreeDismissPlayerList;     // 已同意解散的玩家ID列表
    private long applyPlayerId;                    // 发起解散的玩家ID
    private long agreeDismissTime;                 // 解散截止时间戳
    private bool isListenerRegistered;             // 消息监听是否已注册
    #endregion

    #region 生命周期
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        RegisterNetworkMessages();

        // 解析传入数据
        if (userData is UIParams uiParams)
        {
            this.applyPlayerId = uiParams.Get<VarInt64>("applyPlayerId");
            this.agreeDismissTime = uiParams.Get<VarInt64>("agreeDismissTime");
            this.currentPlayerId = uiParams.Get<VarInt64>("currentPlayerId");
            
            // 解析玩家列表
            var playersVar = uiParams.Get<VarObject>("allPlayers");
            if (playersVar != null && playersVar.Value is List<BasePlayer> players)
            {
                this.allPlayers = players;
            }
            
            // 解析已同意列表
            var agreeListVar = uiParams.Get<VarObject>("agreeDismissPlayerList");
            if (agreeListVar != null && agreeListVar.Value is List<long> agreeList)
            {
                this.agreeDismissPlayerList = agreeList;
            }
            else
            {
                this.agreeDismissPlayerList = new List<long>();
            }
        }

        // 刷新显示
        RefreshPanel();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        StopCountdown();
        UnregisterNetworkMessages();
        ClearData();
    }
    #endregion

    #region 网络消息注册/注销
    /// <summary>
    /// 注册网络消息监听
    /// </summary>
    private void RegisterNetworkMessages()
    {
        if (isListenerRegistered) return;

        HotfixNetworkComponent.AddListener(MessageID.Msg_SynMJDismiss, OnReceive_SynMJDismiss);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynProcessDismiss, OnReceive_SynProcessDismiss);
        
        isListenerRegistered = true;
        GF.LogInfo("DissDeskPanel 注册网络消息监听");
    }

    /// <summary>
    /// 注销网络消息监听
    /// </summary>
    private void UnregisterNetworkMessages()
    {
        if (!isListenerRegistered) return;

        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynMJDismiss, OnReceive_SynMJDismiss);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynProcessDismiss, OnReceive_SynProcessDismiss);
        
        isListenerRegistered = false;
        GF.LogInfo("DissDeskPanel 注销网络消息监听");
    }
    #endregion

    #region 网络消息发送
    /// <summary>
    /// 发送处理解散请求(同意/拒绝)
    /// </summary>
    /// <param name="isAgree">是否同意</param>
    private void SendProcessDismissRequest(bool isAgree)
    {
        Msg_ProcessDismissRq req = MessagePool.Instance.Fetch<Msg_ProcessDismissRq>();
        req.IsAgree = isAgree;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ProcessDismissRq, req);
        GF.LogInfo($"发送处理解散请求: {(isAgree ? "同意" : "拒绝")}");
    }
    #endregion

    #region 网络消息接收
    /// <summary>
    /// 收到申请解散房间消息
    /// </summary>
    private void OnReceive_SynMJDismiss(MessageRecvData data)
    {
        Msg_SynMJDismiss ack = Msg_SynMJDismiss.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 麻将解散通知", ack.ToString());

        // 更新解散信息
        applyPlayerId = ack.PlayerId;
        agreeDismissTime = ack.AgreeDismissTime;

        // 刷新面板显示
        RefreshPanel();
    }

    /// <summary>
    /// 收到处理解散消息(其他玩家同意/拒绝)
    /// </summary>
    private void OnReceive_SynProcessDismiss(MessageRecvData data)
    {
        Msg_SynProcessDismiss ack = Msg_SynProcessDismiss.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 处理解散通知", ack.ToString());

        if (!gameObject.activeSelf) return;

        // 查找玩家
        BasePlayer player = allPlayers?.Find(p => p.PlayerId == ack.PlayerId);
        if (player == null) return;

        if (ack.IsAgree)
        {
            // 同意解散 - 更新投票状态
            UpdatePlayerVoteState(ack.PlayerId, true, player.Nick);

            // 如果是自己同意,隐藏按钮
            if (ack.PlayerId == currentPlayerId)
            {
                UpdateButtonState(true);
            }
        }
        else
        {
            // 拒绝解散 - 隐藏面板并提示
            GF.UI.ShowToast($"玩家 {player.Nick} 拒绝了解散房间申请!");
            GF.UI.Close(this.UIForm);
        }
    }
    #endregion

    #region 按钮事件
    public void OnAgreeClick()
    {
        SendProcessDismissRequest(true);
    }

    public void OnRefuseClick()
    {
        SendProcessDismissRequest(false);
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 更新玩家投票状态
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="isAgree">是否同意</param>
    /// <param name="playerNick">玩家昵称</param>
    public void UpdatePlayerVoteState(long playerId, bool isAgree, string playerNick)
    {
        if (!gameObject.activeSelf) return;
        if (!isAgree) return;

        // 添加到已同意列表
        if (!agreeDismissPlayerList.Contains(playerId))
        {
            agreeDismissPlayerList.Add(playerId);
        }

        // 查找对应的玩家项并更新状态
        GameObject dissPlayerItem = DissPlayerItems.FirstOrDefault(item => 
        {
            Text nameText = item.transform.Find("Name")?.GetComponent<Text>();
            return nameText != null && nameText.text == playerNick;
        });

        if (dissPlayerItem != null)
        {
            Text stateText = dissPlayerItem.transform.Find("State")?.GetComponent<Text>();
            if (stateText != null)
            {
                stateText.text = "<color=#52A439>(已同意)</color>";
            }
        }

        // 检查是否所有玩家都已同意(包括发起者)
        CheckAllPlayersAgreed();
    }

    /// <summary>
    /// 检查是否所有玩家都已同意解散
    /// </summary>
    private void CheckAllPlayersAgreed()
    {
        if (allPlayers == null || allPlayers.Count == 0) return;

        // 需要同意的玩家数 = 总玩家数 - 1(发起者自动同意)
        int needAgreeCount = allPlayers.Count - 1;
        
        // 已同意的玩家数
        int agreedCount = agreeDismissPlayerList.Count;

        // 如果所有需要同意的玩家都已同意,关闭界面
        if (agreedCount >= needAgreeCount)
        {
            GF.LogInfo($"所有玩家已同意解散,关闭界面。总玩家数:{allPlayers.Count}, 已同意数:{agreedCount}");
            GF.UI.Close(this.UIForm);
        }
    }

    #endregion

    #region 私有方法
    /// <summary>
    /// 刷新面板显示
    /// </summary>
    private void RefreshPanel()
    {
        // 判断自己是否已经同意或是发起者
        bool isAgreeSelf = agreeDismissPlayerList.Contains(currentPlayerId);
        bool isApplySelf = applyPlayerId > 0 && applyPlayerId == currentPlayerId;

        // 更新按钮状态
        UpdateButtonState(isAgreeSelf || isApplySelf);

        // 更新玩家列表显示
        UpdatePlayerItems();
    }

    /// <summary>
    /// 更新玩家项显示
    /// </summary>
    private void UpdatePlayerItems()
    {
        if (allPlayers == null) return;

        for (int i = 0; i < 4 && i < DissPlayerItems.Count; i++)
        {
            if (i < allPlayers.Count && allPlayers[i] != null)
            {
                BasePlayer player = allPlayers[i];
                bool isAgree = agreeDismissPlayerList.Contains(player.PlayerId);
                bool isApplyPlayer = applyPlayerId > 0 && player.PlayerId == applyPlayerId;

                DissPlayerItems[i].SetActive(true);

                // 设置玩家名称
                Text nameText = DissPlayerItems[i].transform.Find("Name")?.GetComponent<Text>();
                if (nameText != null)
                {
                    nameText.text = player.Nick;
                }

                // 设置头像
                RawImage headImg = DissPlayerItems[i].transform.Find("headImg")?.GetComponent<RawImage>();
                if (headImg != null && !string.IsNullOrEmpty(player.HeadImage))
                {
                    Util.DownloadHeadImage(headImg, player.HeadImage);
                }

                // 设置解散标识和状态
                GameObject dissObj = DissPlayerItems[i].transform.Find("Diss")?.gameObject;
                Text stateText = DissPlayerItems[i].transform.Find("State")?.GetComponent<Text>();

                if (isApplyPlayer)
                {
                    // 显示发起解散标识
                    if (dissObj != null) dissObj.SetActive(true);
                    if (stateText != null) stateText.text = "";
                }
                else
                {
                    // 显示投票状态
                    if (dissObj != null) dissObj.SetActive(false);
                    if (stateText != null)
                    {
                        stateText.text = isAgree ? "<color=#52A439>(已同意)</color>" : "<color=#5a1414ff>(投票中)</color>";
                    }
                }
            }
            else
            {
                DissPlayerItems[i].SetActive(false);
            }
        }
    }

    /// <summary>
    /// 更新按钮和倒计时状态
    /// </summary>
    /// <param name="hideButtons">是否隐藏按钮(已同意或是发起者)</param>
    private void UpdateButtonState(bool hideButtons)
    {
        if (hideButtons)
        {
            // 已同意或发起者：隐藏按钮，显示等待
            if (yesBtn != null) yesBtn.SetActive(false);
            if (noBtn != null) noBtn.SetActive(false);
            if (waiting != null) waiting.SetActive(true);
            
            // 只显示倒计时，不执行任何操作
            if (agreeDismissTime > 0 && dissDeskCD != null)
            {
                CoroutineRunner.Instance.StartCountdown(Util.GetRemainTime(agreeDismissTime), dissDeskCD, gameObject, null);
            }
        }
        else
        {
            // 未同意：显示按钮，隐藏等待，开启倒计时
            if (yesBtn != null) yesBtn.SetActive(true);
            if (noBtn != null) noBtn.SetActive(true);
            if (waiting != null) waiting.SetActive(false);

            // 显示倒计时
            if (agreeDismissTime > 0 && dissDeskCD != null)
            {
                CoroutineRunner.Instance.StartCountdown(Util.GetRemainTime(agreeDismissTime), dissDeskCD, gameObject, () =>
                {
                    // 倒计时结束 - 自动视为拒绝
                    GF.LogInfo("解散倒计时结束");
                });
            }
        }
    }

    /// <summary>
    /// 停止倒计时
    /// </summary>
    private void StopCountdown()
    {
        CoroutineRunner.Instance.StopCountdown(gameObject);
    }

    /// <summary>
    /// 清除数据
    /// </summary>
    private void ClearData()
    {
        allPlayers = null;
        agreeDismissPlayerList = null;
        applyPlayerId = 0;
        currentPlayerId = 0;
        agreeDismissTime = 0;
    }
    #endregion
}

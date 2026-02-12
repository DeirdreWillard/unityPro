using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameFramework.Fsm;
using GameFramework.Procedure;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using Cysharp.Threading.Tasks;
using GameFramework;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class PDKProcedures : ProcedureBase
{
    public int deskID;

    private IFsm<IProcedureManager> procedure;
    public PDKGamePanel GamePanel_pdk { get; private set; }
    public Msg_EnterRunFastDeskRs EnterRunFastDeskRs;
    public bool brecord = false;


    // 解散状态存储（用于重连恢复）
    public bool HasDismissRequest { get; private set; }        // 是否有解散请求
    public long DismissApplyPlayerId { get; private set; }     // 发起解散的玩家ID
    public long DismissDeadlineTime { get; private set; }      // 解散截止时间
    public List<long> AgreedPlayerIds { get; private set; }    // 已同意的玩家ID列表
    public SingleGameRecord singleGameRecord { get; private set; }
    public int currentRoundNum = 1; // 当前局数,从1开始
    private bool hasReceivedFirstDeal = false; // 是否已收到过第一次发牌（用于判断是否应该增加局数）

    // 本地抢庄记录：玩家ID -> 是否抢庄
    private Dictionary<long, bool> localRobBankerRecords = new Dictionary<long, bool>();
    private System.Threading.CancellationTokenSource countdownTokenSource;


    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        CoroutineRunner.Instance.RunCoroutine(GFBuiltin.Instance.SwitchToLandscape(async () =>
        {
            this.procedure = procedureOwner;
            var data1 = procedure.GetData<VarByteArray>("enterRunFastDeskRs");
            EnterRunFastDeskRs = Msg_EnterRunFastDeskRs.Parser.ParseFrom(data1);
            deskID = EnterRunFastDeskRs.DeskId;
            Util.GetMyselfInfo().DeskId = deskID;

            // 保存进入桌子前的亲友圈房间ID（如果尚未保存）
            var leagueInfo = GlobalManager.GetInstance().LeagueInfo;
            if (leagueInfo != null && leagueInfo.LeagueId > 0)
            {
                // 只有在没有保存过的情况下才保存，避免覆盖之前保存的ID
                if (GlobalManager.GetInstance().PeekBeforeEnterDeskLeagueId() == 0)
                {
                    GlobalManager.GetInstance().SaveBeforeEnterDeskLeagueId(leagueInfo.LeagueId);
                    GF.LogInfo($"[跑得快游戏] 保存亲友圈房间ID: {leagueInfo.LeagueId}");
                }
            }

            // 从服务器数据读取当前局数
            currentRoundNum = EnterRunFastDeskRs.Round > 0 ? EnterRunFastDeskRs.Round : 1;
            // 【发牌修复】根据服务器局数或桌子状态判断是否已经开始游戏（用于后续发牌时的局数增加判断）
            hasReceivedFirstDeal = (currentRoundNum > 1 || EnterRunFastDeskRs.DeskState != RunFastDeskState.RunfastWait);
            GF.LogInfo_wl($"[OnEnter] 服务器返回局数={EnterRunFastDeskRs.Round}, 状态={EnterRunFastDeskRs.DeskState}, 本地设置currentRoundNum={currentRoundNum}, hasReceivedFirstDeal={hasReceivedFirstDeal}");
            // 更新游戏规则配置
            UpdateGameRules(EnterRunFastDeskRs);
            // 等待UI加载完成
            await ShowbaseMahjongGameManagerPanel();
        }));
    }
    public async UniTask ShowbaseMahjongGameManagerPanel()
    {
        var gamePanel = await GF.UI.OpenUIFormAwait(UIViews.PDKGamePanel);
        GamePanel_pdk = gamePanel as PDKGamePanel;
        brecord = false;
        AddListener();
        GlobalManager.SendSystemConfigRq();
        // 面板打开后，立即初始化桌子数据
        if (EnterRunFastDeskRs != null)
        {
            InitPDKDeskFromServer(EnterRunFastDeskRs);
        }
    }
    bool isListenerAdded = false;

    //添加监听
    public void AddListener()
    {
        // 避免重复添加
        if (isListenerAdded)
        {
            return;
        }
        isListenerAdded = true;
        // 订阅游戏事件
        GF.Event.Subscribe(GFEventArgs.EventId, OnGFEventChanged);

        // 订阅跑的快网络消息
        // 进入跑的快桌子响应
        HotfixNetworkComponent.AddListener(MessageID.Msg_EnterRunFastDeskRs, Function_EnterRunFastDeskRs);
        // 发牌消息
        HotfixNetworkComponent.AddListener(MessageID.Syn_RunFastDealCard, Function_RunFastDealCard);
        // 离开桌子响应
        HotfixNetworkComponent.AddListener(MessageID.Msg_LeaveDeskRs, Function_LeaveDeskRs);
        // 出牌同步消息
        HotfixNetworkComponent.AddListener(MessageID.Syn_RunFastDiscardCard, Function_SynRunFastDiscardCard);
        // 桌子玩家信息同步消息
        HotfixNetworkComponent.AddListener(MessageID.Syn_DeskPlayerInfo, Function_DeskPlayerInfoRs);
        HotfixNetworkComponent.AddListener(MessageID.Syn_DeskPlayerInfo1, Function_DeskPlayerInfoRs);
        HotfixNetworkComponent.AddListener(MessageID.Syn_DeskPlayerInfo2, Function_DeskPlayerInfoRs);

        // 玩家离开桌子（站起）的消息
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynSitUp, Function_SynSitUp);

        // 准备相关消息（复用麻将的准备消息）
        HotfixNetworkComponent.AddListener(MessageID.Msg_MJReadyRs, Function_Msg_MJReadyRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynMJReady, Function_Msg_SynMJReady);

        // 桌子状态变更消息（用于通知轮到谁操作）
        HotfixNetworkComponent.AddListener(MessageID.Syn_ChangeRunFastDeskState, Function_SynChangeRunFastDeskState);

        // 结算消息
        HotfixNetworkComponent.AddListener(MessageID.Syn_RunFastSettle, Function_SynRunFastSettle);

        // 金币变化消息（与麻将共用）
        HotfixNetworkComponent.AddListener(MessageID.Syn_HHCoinChange, Function_Syn_HHCoinChange);

        // 单局战绩消息
        HotfixNetworkComponent.AddListener(MessageID.SingleGameRecord, Function_SingleGameRecord);

        // 解散相关消息
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynMJDismiss, Function_Msg_SynMJDismiss);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynProcessDismiss, Function_Msg_SynProcessDismiss);
        HotfixNetworkComponent.AddListener(MessageID.Msg_DisMissDeskRs, Function_DisMissDeskRs);

        // 送礼物消息
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynSendGift, Function_SynSendGift);

        // 抢庄相关消息
        HotfixNetworkComponent.AddListener(MessageID.Msg_RanFastRobBankerRs, Function_RanFastRobBankerRs);

        // 翻倍相关消息
        HotfixNetworkComponent.AddListener(MessageID.Syn_RanFastDouble, Function_SynRanFastDouble);

        //祈福
        HotfixNetworkComponent.AddListener(MessageID.SynPrayRs, Function_Syn_Pray_Rs);
        HotfixNetworkComponent.AddListener(MessageID.Syn_PlayerAuto, Function_Syn_PlayerAuto);


    }

    public void RemoveListener()
    {
        isListenerAdded = false;

        GF.Event.Unsubscribe(GFEventArgs.EventId, OnGFEventChanged);

        HotfixNetworkComponent.RemoveListener(MessageID.Msg_EnterRunFastDeskRs, Function_EnterRunFastDeskRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_RunFastDealCard, Function_RunFastDealCard);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_LeaveDeskRs, Function_LeaveDeskRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_RunFastDiscardCard, Function_SynRunFastDiscardCard);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_DeskPlayerInfo, Function_DeskPlayerInfoRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_DeskPlayerInfo1, Function_DeskPlayerInfoRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_DeskPlayerInfo2, Function_DeskPlayerInfoRs);

        // 玩家离开桌子（站起）的消息
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynSitUp, Function_SynSitUp);

        // 准备相关消息
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MJReadyRs, Function_Msg_MJReadyRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynMJReady, Function_Msg_SynMJReady);

        // 桌子状态变更消息
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_ChangeRunFastDeskState, Function_SynChangeRunFastDeskState);

        // 结算消息
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_RunFastSettle, Function_SynRunFastSettle);

        // 金币变化消息（与麻将共用）
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_HHCoinChange, Function_Syn_HHCoinChange);

        // 单局战绩消息
        HotfixNetworkComponent.RemoveListener(MessageID.SingleGameRecord, Function_SingleGameRecord);

        // 解散相关消息
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynMJDismiss, Function_Msg_SynMJDismiss);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynProcessDismiss, Function_Msg_SynProcessDismiss);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_DisMissDeskRs, Function_DisMissDeskRs);

        // 送礼物消息
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynSendGift, Function_SynSendGift);

        // 抢庄相关消息
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_RanFastRobBankerRs, Function_RanFastRobBankerRs);

        // 翻倍相关消息
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_RanFastDouble, Function_SynRanFastDouble);

        //祈福
        HotfixNetworkComponent.RemoveListener(MessageID.SynPrayRs, Function_Syn_Pray_Rs);
    }


    /// <summary>
    /// 收到玩家托管状态通知（通知其他玩家）
    /// </summary>
    public void Function_Syn_PlayerAuto(MessageRecvData data)
    {
        Syn_PlayerAuto ack = Syn_PlayerAuto.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 玩家托管状态通知", ack.ToString());

        // 确定新的玩家状态：1=托管，0=在线
        PlayerState newState = ack.Auto == 1 ? PlayerState.ToGuo : PlayerState.OnLine;

        // 更新 EnterRunFastDeskRs 中的玩家状态
        if (EnterRunFastDeskRs?.DeskPlayers != null)
        {
            foreach (var player in EnterRunFastDeskRs.DeskPlayers)
            {
                if (player.BasePlayer.PlayerId == ack.PlayerId)
                {
                    player.State = newState;

                    // 更新UI显示
                    if (GamePanel_pdk?.playerIdToHeadBg != null &&
                        GamePanel_pdk.playerIdToHeadBg.TryGetValue(ack.PlayerId, out GameObject headBg) &&
                        headBg != null)
                    {
                        GamePanel_pdk.SetPlayerTuoGuanStatus(headBg, player);
                        GamePanel_pdk.SetPlayerOnlineStatus(headBg, player);

                        GF.LogInfo($"[玩家托管状态更新] 玩家{ack.PlayerId} 状态:{newState} (Auto={ack.Auto})");
                    }
                    else
                    {
                        GF.LogWarning($"[玩家托管状态更新] 未找到玩家{ack.PlayerId}的headBg");
                    }
                    break;
                }
            }
        }
    }
    #region 发送请求

    /// <summary>
    /// 发送跑得快准备请求
    /// </summary>
    /// <param name="ready">true=准备，false=取消准备</param>
    public void Send_MJPlayerReadyRq(bool ready)
    {
        Msg_MJReadyRq req = MessagePool.Instance.Fetch<Msg_MJReadyRq>();
        req.ReadyState = ready ? 1 : 0;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_MJReadyRq, req);
    }

    /// <summary>
    /// 发送申请解散房间请求
    /// </summary>
    public void Send_Msg_ApplyMJDismissRq()
    {
        // 读取配置的解散倒计时时长
        var configManager = MJConfigManager.Instance;
        var pdkConfig = configManager?.GetConfig("跑的快") as MJConfigManager.PDKConfigData;
        int applyDismissTime = pdkConfig?.ApplyDismissTime ?? 1;
        Msg_ApplyMJDismissRq req = MessagePool.Instance.Fetch<Msg_ApplyMJDismissRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ApplyMJDismissRq, req);
    }


    /// <summary>
    /// 发送抢庄请求
    /// </summary>
    /// <param name="rob">true=抢庄，false=不抢</param>
    public void Send_Msg_RanFastRobBankerRq(bool rob)
    {
        Msg_RanFastRobBankerRq req = MessagePool.Instance.Fetch<Msg_RanFastRobBankerRq>();
        req.Rob = rob;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_RanFastRobBankerRq, req);
    }

    /// <summary>
    /// 发送强制解散桌子请求
    /// </summary>
    public void Send_DismissDesk()
    {
        Msg_DisMissDeskRq req = MessagePool.Instance.Fetch<Msg_DisMissDeskRq>();
        req.DeskId = deskID;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_DisMissDeskRq, req);
        GF.LogInfo_wl($"[PDK] 发送强制解散桌子请求, DeskId={deskID}");
    }

    #endregion



    #region 送礼物相关

    /// <summary>
    /// 处理送礼物消息广播 Msg_SynSendGift (ID: 2000229)
    /// </summary>
    public void Function_SynSendGift(MessageRecvData data)
    {
        // Msg_SynSendGift
        Msg_SynSendGift ack = Msg_SynSendGift.Parser.ParseFrom(data.Data);
        if (GamePanel_pdk == null) return;
        GamePanel_pdk.HandleSendGift(ack.Sender.PlayerId, ack.ToPlayer, ack.Gift.ToString());

    }

    #endregion

    #region 战绩相关

    /// <summary>
    /// 处理单局战绩消息
    /// </summary>
    public void Function_SingleGameRecord(MessageRecvData data)
    {

        singleGameRecord = SingleGameRecord.Parser.ParseFrom(data.Data);
        GF.LogInfo_wl("收到跑的快总结算", singleGameRecord.ToString());
        brecord = true;
        GF.UI.CloseUIForms(UIViews.MJDismissRoom);
    }

    #endregion

    #region 解散相关

    /// <summary>
    /// 处理解散申请广播（服务器广播 - 有人申请解散）
    /// 消息ID: 2008014
    /// </summary>
    public void Function_Msg_SynMJDismiss(MessageRecvData data)
    {

        Msg_SynMJDismiss ack = Msg_SynMJDismiss.Parser.ParseFrom(data.Data);

        // 保存解散状态到内存（用于在线时的状态跟踪）
        HasDismissRequest = true;
        DismissApplyPlayerId = ack.PlayerId;
        DismissDeadlineTime = ack.AgreeDismissTime;
        AgreedPlayerIds = new List<long>();
        AgreedPlayerIds.Clear();
        GamePanel_pdk.Function_Msg_SynMJDismiss(ack);
        // 【关键修复】收到解散申请时，如果已经在结算界面，立即更新按钮
        GamePanel_pdk.UpdateSettlementButtons();

    }

    /// <summary>
    /// 处理解散投票过程（有人同意或拒绝）
    /// 消息ID: 2008015
    /// </summary>
    public void Function_Msg_SynProcessDismiss(MessageRecvData data)
    {
        Msg_SynProcessDismiss ack = Msg_SynProcessDismiss.Parser.ParseFrom(data.Data);
        if (!ack.IsAgree)
        {
            // 有人拒绝，重置解散状态
            HasDismissRequest = false;
            // 同时也通知UI更新按钮（如果当前在结算界面）
            GamePanel_pdk.UpdateSettlementButtons();
        }
    }

    /// <summary>
    /// 处理解散牌桌响应
    /// 消息ID: 2100002
    /// </summary>
    public void Function_DisMissDeskRs(MessageRecvData data)
    {
        GF.LogInfo("收到消息解散牌桌");
        Msg_DisMissDeskRs ack = Msg_DisMissDeskRs.Parser.ParseFrom(data.Data);
        if (ack.State == 1)
        {
            GF.UI.ShowToast("本小局打完房间将解散");
        }
    }
    /// <summary>
    /// 从协议中同步解散状态（重连时使用）
    /// </summary>
    public void SyncDismissStateFromProtocol(Msg_EnterRunFastDeskRs enterData)
    {
        if (enterData == null) return;

        HasDismissRequest = enterData.DismissState;
        DismissDeadlineTime = enterData.AgreeDismissTime;

        if (AgreedPlayerIds == null)
        {
            AgreedPlayerIds = new List<long>();
        }
        else
        {
            AgreedPlayerIds.Clear();
        }

        // 从协议中复制已同意的玩家列表
        if (enterData.AgreeDismissPlayer != null && enterData.AgreeDismissPlayer.Count > 0)
        {
            AgreedPlayerIds.AddRange(enterData.AgreeDismissPlayer);
            DismissApplyPlayerId = enterData.AgreeDismissPlayer[0]; // 第一个是申请人
        }

    }

    #endregion

    #region 抢庄相关



    /// <summary>
    /// 显示或隐藏抢庄UI
    /// </summary>
    /// <param name="show">是否显示</param>
    public void ShowRobBankerUI(bool show)
    {
        if (GamePanel_pdk != null && GamePanel_pdk.varQiangZhuangBtn != null)
        {
            GamePanel_pdk.varQiangZhuangBtn.SetActive(show);
            if (show)
            {
                // 绑定抢庄按钮事件
                BindRobBankerButtons();
            }
        }
    }

    /// <summary>
    /// 显示某个玩家的抢庄状态标识
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="isRob">是否抢庄</param>
    private void ShowPlayerRobBankerStatus(long playerId, bool isRob)
    {
        if (GamePanel_pdk == null || GamePanel_pdk.playerIdToHeadBg == null)
        {
            //GF.LogWarning("[PDK抢庄] GamePanel_pdk 或 playerIdToHeadBg 为空");
            return;
        }

        if (!GamePanel_pdk.playerIdToHeadBg.TryGetValue(playerId, out GameObject headBg) || headBg == null)
        {
            // GF.LogWarning($"[PDK抢庄] 找不到玩家{playerId}的HeadBg");
            return;
        }

        // 查找 qiangZhuang 和 buqiangZhuang 子对象
        Transform qiangZhuangTrans = headBg.transform.Find("qiangZhuang");
        Transform buqiangZhuangTrans = headBg.transform.Find("buqiangZhuang");

        if (qiangZhuangTrans != null && buqiangZhuangTrans != null)
        {
            // 根据isRob显示对应的标识
            qiangZhuangTrans.gameObject.SetActive(isRob);
            buqiangZhuangTrans.gameObject.SetActive(!isRob);
        }
        else
        {
            GF.LogWarning($"[PDK抢庄] 玩家{playerId}的HeadBg下找不到 qiangZhuang 或 buqiangZhuang 子对象");
        }
    }

    /// <summary>
    /// 隐藏所有玩家的抢庄状态标识
    /// </summary>
    public void HideAllPlayerRobBankerStatus()
    {
        if (GamePanel_pdk == null || GamePanel_pdk.playerIdToHeadBg == null)
        {
            return;
        }

        foreach (var kvp in GamePanel_pdk.playerIdToHeadBg)
        {
            GameObject headBg = kvp.Value;
            if (headBg == null) continue;

            Transform qiangZhuangTrans = headBg.transform.Find("qiangZhuang");
            Transform buqiangZhuangTrans = headBg.transform.Find("buqiangZhuang");

            if (qiangZhuangTrans != null)
            {
                qiangZhuangTrans.gameObject.SetActive(false);
            }

            if (buqiangZhuangTrans != null)
            {
                buqiangZhuangTrans.gameObject.SetActive(false);
            }
        }

    }

    /// <summary>
    /// 绑定抢庄按钮事件
    /// </summary>
    private void BindRobBankerButtons()
    {
        if (GamePanel_pdk?.varQiangZhuangBtn == null) return;

        // 绑定Yes按钮（抢庄）
        var yesBtn = GamePanel_pdk.varQiangZhuangBtn.transform.Find("Yes")?.GetComponent<UnityEngine.UI.Button>();
        if (yesBtn != null)
        {
            yesBtn.onClick.RemoveAllListeners();
            yesBtn.onClick.AddListener(() =>
            {
                Send_Msg_RanFastRobBankerRq(true);
                ShowRobBankerUI(false);
            });
        }

        // 绑定No按钮（不抢）
        var noBtn = GamePanel_pdk.varQiangZhuangBtn.transform.Find("No")?.GetComponent<UnityEngine.UI.Button>();
        if (noBtn != null)
        {
            noBtn.onClick.RemoveAllListeners();
            noBtn.onClick.AddListener(() =>
            {

                Send_Msg_RanFastRobBankerRq(false);
                ShowRobBankerUI(false);
            });
        }
    }

    /// <summary>
    /// 解析抢庄数据,判断当前抢庄阶段
    /// </summary>
    /// <param name="robBankerList">服务器的robBanker数组</param>
    /// <param name="allPlayerIds">所有玩家ID列表</param>
    /// <param name="originalBankerId">原始庄家ID</param>
    /// <param name="nextOperatorId">输出参数:下一个需要操作的玩家ID(0表示抢庄结束)</param>
    /// <param name="finalBankerId">输出参数:最终确定的庄家ID(抢庄未结束时为0)</param>
    /// <returns>true=抢庄阶段进行中或已结束, false=抢庄未开始</returns>
    private bool ParseRobBankerState(
        Google.Protobuf.Collections.RepeatedField<NetMsg.LongBool> robBankerList,
        long[] allPlayerIds,
        long originalBankerId,
        out long nextOperatorId,
        out long finalBankerId)
    {
        nextOperatorId = 0;
        finalBankerId = 0;

        if (robBankerList == null || robBankerList.Count == 0)
        {
            return false;
        }
        // 记录所有已操作的玩家
        System.Collections.Generic.Dictionary<long, bool> robRecords = new System.Collections.Generic.Dictionary<long, bool>();
        foreach (var record in robBankerList)
        {
            robRecords[record.Key] = record.Val;

            // 如果有玩家选择抢庄,他就是最终庄家
            if (record.Val)
            {
                finalBankerId = record.Key;
            }
        }
        // 如果已经确定最终庄家,抢庄结束
        if (finalBankerId > 0)
        {
            nextOperatorId = 0;
            return true;
        }

        // 找出原始庄家在玩家列表中的索引
        int bankerIndex = -1;
        for (int i = 0; i < allPlayerIds.Length; i++)
        {
            if (allPlayerIds[i] == originalBankerId)
            {
                bankerIndex = i;
                break;
            }
        }

        if (bankerIndex < 0)
        {
            return false;
        }

        // 从庄家下一位开始轮询,找到第一个未操作的玩家
        int playerCount = allPlayerIds.Length;
        for (int i = 1; i < playerCount; i++)
        {
            int checkIndex = (bankerIndex + i) % playerCount;
            long checkPlayerId = allPlayerIds[checkIndex];

            if (!robRecords.ContainsKey(checkPlayerId))
            {
                nextOperatorId = checkPlayerId;
                return true;
            }
        }
        // 所有玩家都不抢,原庄家继续当庄
        finalBankerId = originalBankerId;
        nextOperatorId = 0;
        return true;
    }

    /// <summary>
    /// 处理抢庄结果
    /// </summary>
    private void Function_RanFastRobBankerRs(object obj)
    {
        MessageRecvData data = obj as MessageRecvData;
        if (data == null) return;

        Msg_RanFastRobBankerRs ack = Msg_RanFastRobBankerRs.Parser.ParseFrom(data.Data);

        ShowRobBankerUI(false);
        long myPlayerId = Util.GetMyselfInfo().PlayerId;


        ShowPlayerRobBankerStatus(ack.PlayerId, ack.Rob);


        bool shouldEndRobPhase = false;
        if (ack.NextRoboPlayer > 0 && localRobBankerRecords.ContainsKey(ack.NextRoboPlayer))
        {
            shouldEndRobPhase = true;
        }


        localRobBankerRecords[ack.PlayerId] = ack.Rob;


        if (ack.NextRoboPlayer > 0 && !shouldEndRobPhase)
        {
            if (ack.NextRoboPlayer == myPlayerId)
            {
                ShowRobBankerUI(true);
            }
            // 显示抢庄玩家的闹钟
            GamePanel_pdk.SetNextPlayer(ack.NextRoboPlayer);
        }
        else if (ack.NextRoboPlayer <= 0 || shouldEndRobPhase)
        {

            HideAllPlayerRobBankerStatus();

            // 从本地记录中找出抢庄的玩家
            long finalBankerId = 0;
            foreach (var record in localRobBankerRecords)
            {
                if (record.Value)  // 如果这个玩家选择抢庄
                {
                    finalBankerId = record.Key;

                    break;
                }
            }

            // 如果没人抢庄，使用原始庄家
            if (finalBankerId == 0 && EnterRunFastDeskRs != null)
            {
                finalBankerId = EnterRunFastDeskRs.Banker;

            }

            if (finalBankerId > 0 && EnterRunFastDeskRs != null && EnterRunFastDeskRs.DeskPlayers != null && EnterRunFastDeskRs.DeskPlayers.Count > 0)
            {


                // 【关键修复】更新所有玩家的庄家标识（这会同时更新GamePanel_pdk.bankerId字段）
                GamePanel_pdk.UpdateAllPlayerBankerStatus(
                    EnterRunFastDeskRs.DeskPlayers,
                    EnterRunFastDeskRs.BaseConfig.PlayerNum,
                    finalBankerId
                );

                // 如果自己是最终庄家，显示出牌按钮
                if (finalBankerId == myPlayerId) GamePanel_pdk.ShowPlayButtons(true);
                else GamePanel_pdk.ShowPlayButtons(false);

                // 【修复】确定庄家后，庄家为第一个出牌者，显示其闹钟
                GamePanel_pdk.SetNextPlayer(finalBankerId);
            }

        }

    }

    #endregion

    #region 翻倍功能

    /// <summary>
    /// 发送翻倍请求到服务器
    /// </summary>
    /// <param name="rob">翻倍倍数：1=不翻倍, 2=翻倍, 4=超级翻倍</param>
    public void SendDoubleRequest(int rob)
    {

        Msg_RanFastDoubleRq req = MessagePool.Instance.Fetch<Msg_RanFastDoubleRq>();
        req.Rob = rob;

        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(
            HotfixNetworkComponent.AccountClientName,
            MessageID.Msg_RanFastDoubleRq,
            req
        );
    }

    /// <summary>
    /// 处理翻倍推送消息
    /// </summary>
    private void Function_SynRanFastDouble(MessageRecvData data)
    {

        Syn_RanFastDouble ack = Syn_RanFastDouble.Parser.ParseFrom(data.Data);
        GamePanel_pdk.HandleDoubleResult(ack.PlayerId, ack.Rob);
    }

    private void Function_Syn_Pray_Rs(MessageRecvData data)
    {
        SynPrayRs ack = SynPrayRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 祈福通知", ack.ToString());
        if (ack.PlayerId == Util.GetMyselfInfo().PlayerId) return;
        GamePanel_pdk?.ShowBlessingUI_other(ack);
    }

    #endregion

    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        if (countdownTokenSource != null)
        {
            countdownTokenSource.Cancel();
            countdownTokenSource.Dispose();
            countdownTokenSource = null;
        }
        GamePanel_pdk.ClearAllGameUI();
        GlobalManager.GetInstance().ReSetUI();
        RemoveListener();
        Util.GetMyselfInfo().DeskId = 0;
        base.OnLeave(procedureOwner, isShutdown);

    }

    #region 事件处理器

    private void OnGFEventChanged(object sender, GameFramework.Event.GameEventArgs e)
    {
        // 处理游戏框架事件
        GFEventArgs args = (GFEventArgs)e;

        switch (args.EventType)
        {
            case GFEventType.eve_SynDeskChat:
                // 处理聊天消息（参考麻将实现）
                var chatData = args.UserData as Msg_DeskChat;
                if (chatData != null && GamePanel_pdk != null)
                {
                    GamePanel_pdk.HandleDeskChat(chatData);
                }
                break;

            case GFEventType.eve_ReConnectGame:
                // 处理重连逻辑（当已经在跑得快游戏中断线重连）
                HandleReconnect();
                break;
        }
    }

    /// <summary>
    /// 处理重连逻辑（当已经在跑得快游戏中时）
    /// </summary>
    private void HandleReconnect()
    {
        var myselfInfo = Util.GetMyselfInfo();

        // 检查是否在跑得快桌子上
        if (myselfInfo != null && myselfInfo.DeskId > 0 &&
            EnterRunFastDeskRs != null && myselfInfo.DeskId == EnterRunFastDeskRs.DeskId)
        {
            Util.GetInstance().Send_EnterDeskRq(myselfInfo.DeskId);
        }
        else
        {
            Util.GetInstance().CloseWaiting("GameStateSync");
            HomeProcedure homeProcedure = GF.Procedure.GetProcedure<HomeProcedure>() as HomeProcedure;
            homeProcedure.QuitGame();
        }
    }

    /// <summary>
    /// 进入跑得快桌子响应处理
    /// </summary>
    /// <param name="obj"></param>
    private void Function_EnterRunFastDeskRs(MessageRecvData data)
    {
        EnterRunFastDeskRs = Msg_EnterRunFastDeskRs.Parser.ParseFrom(data.Data);
        deskID = EnterRunFastDeskRs.DeskId;
        Util.GetMyselfInfo().DeskId = deskID;
        HasDismissRequest = false;
        DismissApplyPlayerId = 0;
        DismissDeadlineTime = 0;
        AgreedPlayerIds?.Clear();
        brecord = false;
        currentRoundNum = EnterRunFastDeskRs.Round > 0 ? EnterRunFastDeskRs.Round : 1;
        hasReceivedFirstDeal = (currentRoundNum > 1 || EnterRunFastDeskRs.DeskState != RunFastDeskState.RunfastWait);
        GamePanel_pdk.ClearAllGameUI();
        // 更新游戏规则配置
        UpdateGameRules(EnterRunFastDeskRs);
        // 关闭匹配界面
        GF.StaticUI.ShowMatching(false);
        // 初始化桌子数据（会同步玩家信息、房间信息等）
        InitPDKDeskFromServer(EnterRunFastDeskRs);
        // 关闭游戏状态同步的等待框
        Util.GetInstance().CloseWaiting("GameStateSync");


    }

    /// <summary>
    /// 发牌消息处理
    /// </summary>
    /// <param name="obj"></param>
    private void Function_RunFastDealCard(MessageRecvData data)
    {
        GF.LogInfo_wl("收到发牌消息" + data.ToString());

        HasDismissRequest = false;
        DismissApplyPlayerId = 0;
        DismissDeadlineTime = 0;
        AgreedPlayerIds?.Clear();

        // 清空上一轮的本地抢庄记录
        localRobBankerRecords.Clear();
        GamePanel_pdk.ClearGameState();
        // 发牌消息处理
        var dealCardMsg = Syn_RunFastDealCard.Parser.ParseFrom(data.Data);
        List<int> handCards = new List<int>(dealCardMsg.HandCards);

        // 把庄家Id存起来
        long finalBankerId = dealCardMsg.Banker; // 默认使用发牌消息中的庄家

        hasReceivedFirstDeal = true;
        GF.LogInfo_wl($"<color=#00FF00>[发牌] 局数: {currentRoundNum}</color>");

        // 【关键修复】确定最终庄家ID：如果抢庄已经确定了庄家，使用抢庄结果；否则使用发牌消息中的庄家
        // 检查抢庄是否已经结束并确定了新庄家
        if (EnterRunFastDeskRs != null && EnterRunFastDeskRs.RobBanker != null && EnterRunFastDeskRs.RobBanker.Count > 0)
        {
            // 获取所有玩家ID
            long[] allPlayerIds = new long[EnterRunFastDeskRs.DeskPlayers.Count];
            for (int i = 0; i < EnterRunFastDeskRs.DeskPlayers.Count; i++)
            {
                allPlayerIds[i] = EnterRunFastDeskRs.DeskPlayers[i].BasePlayer.PlayerId;
            }

            // 解析抢庄记录,确定最终庄家
            long nextOperatorId;
            long robBankerId;
            if (ParseRobBankerState(
                EnterRunFastDeskRs.RobBanker,
                allPlayerIds,
                EnterRunFastDeskRs.Banker,
                out nextOperatorId,
                out robBankerId))
            {
                // 抢庄已结束
                if (nextOperatorId == 0 && robBankerId > 0)
                {
                    finalBankerId = robBankerId;

                }
            }
        }

        //把庄家Id存起来
        GamePanel_pdk.bankerId = finalBankerId;

        // 【关键】发牌后立即显示庄家
        if (EnterRunFastDeskRs != null && EnterRunFastDeskRs.DeskPlayers != null && EnterRunFastDeskRs.DeskPlayers.Count > 0)
        {
            GamePanel_pdk.UpdateAllPlayerBankerStatus(
                EnterRunFastDeskRs.DeskPlayers,
                EnterRunFastDeskRs.BaseConfig.PlayerNum,
                finalBankerId
            );
        }


        // 发牌给玩家
        GamePanel_pdk.OnReceiveHandCards(handCards);
        // 发牌后更新局数显示
        GamePanel_pdk.UpdateRoundDisplay();

        // 【抢庄逻辑】发牌后,判断是否需要显示抢庄按钮
        // 直接使用后端提供的OptionPlayer字段(第一个需要抢庄的玩家ID)
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        long optionPlayerId = dealCardMsg.OptionPlayer;

        if (optionPlayerId > 0)
        {
            if (optionPlayerId == myPlayerId)
            {

                ShowRobBankerUI(true);
            }
            else
            {
                ShowRobBankerUI(false);
            }

            // 【关键修复】发牌后显示对应操作玩家的闹钟
            GamePanel_pdk.SetNextPlayer(optionPlayerId);
        }

    }

    private void Function_LeaveDeskRs(MessageRecvData data)
    {

        Msg_LeaveDeskRs ack = Msg_LeaveDeskRs.Parser.ParseFrom(data.Data);
        // 如果已经收到战绩数据，忽略离开桌子消息，避免在显示战绩面板时退出
        if (brecord == true)
        {
            return;
        }
        // 新加判断：如果不是桌子上已坐下的玩家，点击退出直接退出房间
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        bool isSeated = EnterRunFastDeskRs.DeskPlayers != null &&
                        EnterRunFastDeskRs.DeskPlayers.Any(p => p.BasePlayer.PlayerId == myPlayerId);

        // 如果在游戏中或结算状态，不退出（等待显示小结算和总结算）
        if (isSeated && EnterRunFastDeskRs != null && EnterRunFastDeskRs.DeskState != RunFastDeskState.RunfastWait)
        {
            brecord = true;
            HasDismissRequest = true;
            GamePanel_pdk.HideAllPassIndicators();
            GamePanel_pdk.UpdateSettlementButtons();
            return;
        }
        // 清空桌号
        Util.GetMyselfInfo().DeskId = 0;
        ExitPDKGame();
    }

    /// <summary>
    /// 推送桌子玩家信息（单个玩家进入/离开/信息变化）
    /// </summary>
    private void Function_DeskPlayerInfoRs(MessageRecvData data)
    {
        Syn_DeskPlayerInfo ack = Syn_DeskPlayerInfo.Parser.ParseFrom(data.Data);
        if (EnterRunFastDeskRs != null && ack.Player != null)
        {
            bool playerExists = false;
            for (int i = 0; i < EnterRunFastDeskRs.DeskPlayers.Count; i++)
            {
                if (EnterRunFastDeskRs.DeskPlayers[i].BasePlayer.PlayerId == ack.Player.BasePlayer.PlayerId)
                {
                    // 玩家已存在，更新信息
                    EnterRunFastDeskRs.DeskPlayers[i] = ack.Player;
                    playerExists = true;
                    break;
                }
            }

            if (!playerExists)
            {
                // 玩家不存在，添加新玩家
                EnterRunFastDeskRs.DeskPlayers.Add(ack.Player);
            }
        }

        if (EnterRunFastDeskRs != null)
        {
            if (EnterRunFastDeskRs.DeskPlayers != null && EnterRunFastDeskRs.DeskPlayers.Count > 0)
            {
                GamePanel_pdk.UpdateDeskPlayers(EnterRunFastDeskRs.DeskPlayers);
            }
        }
    }

    /// <summary>
    /// 玩家站起（离开桌子）的消息
    /// </summary>
    private void Function_SynSitUp(MessageRecvData data)
    {
        Msg_SynSitUp ack = Msg_SynSitUp.Parser.ParseFrom(data.Data);
        if (brecord == true)
        {
            return;
        }
        // 从玩家列表中移除该位置的玩家
        if (EnterRunFastDeskRs != null && EnterRunFastDeskRs.DeskPlayers != null)
        {
            // 查找并移除指定位置的玩家
            for (int i = EnterRunFastDeskRs.DeskPlayers.Count - 1; i >= 0; i--)
            {
                if (EnterRunFastDeskRs.DeskPlayers[i].Pos == ack.Pos)
                {
                    var leavingPlayer = EnterRunFastDeskRs.DeskPlayers[i];
                    EnterRunFastDeskRs.DeskPlayers.RemoveAt(i);
                    break;
                }
            }
        }
        // 更新UI
        if (EnterRunFastDeskRs != null)
        {
            GamePanel_pdk.UpdateDeskPlayers(EnterRunFastDeskRs.DeskPlayers);
        }
    }

    /// <summary>
    /// 准备响应（复用麻将准备协议）
    /// </summary>
    private void Function_Msg_MJReadyRs(MessageRecvData data)
    {
        Msg_MJReadyRs ack = Msg_MJReadyRs.Parser.ParseFrom(data.Data);
        GamePanel_pdk.OnReadyStateChanged(ack.ReadyState);
    }

    /// <summary>
    /// 同步准备状态（其他玩家准备时推送）
    /// </summary>
    private void Function_Msg_SynMJReady(MessageRecvData data)
    {

        Msg_SynMJReady ack = Msg_SynMJReady.Parser.ParseFrom(data.Data);
        GamePanel_pdk.OnOtherPlayerReadyChanged(ack.PlayerId, ack.ReadyState);
    }

    /// <summary>
    /// 同步出牌消息（服务器推送，包括自己和其他玩家出牌/过牌）
    /// </summary>
    private void Function_SynRunFastDiscardCard(MessageRecvData data)
    {
        Syn_RunFastDiscardCard ack = Syn_RunFastDiscardCard.Parser.ParseFrom(data.Data);
        // 判断是否是自己操作
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        bool isMyself = (ack.DiscardPlayer == myPlayerId);
        bool isPass = (ack.Cards == null || ack.Cards.Count == 0);
        List<int> cardIds = isPass ? new List<int>() : new List<int>(ack.Cards);
        // 1. 先更新游戏逻辑状态
        GamePanel_pdk.UpdateGameLogicState(ack.DiscardPlayer, cardIds, (PDKConstants.CardType)ack.CardType, ack.MaxDisCardPlayer);

        // 2. 然后处理UI显示（出牌区域、过牌标识、音效等）
        GamePanel_pdk.HandlePlayCards(ack.DiscardPlayer, cardIds, isMyself);

        // 【关键检查】如果当前出牌玩家手牌为空（已出完牌），游戏结束，不再执行后续操作判断
        if (!GamePanel_pdk.HasPlayerRemainingCards(ack.DiscardPlayer))
        {
            return;
        }

        // 3. 使用服务器返回的 OptionPlayer 字段判断下一个出牌玩家
        long nextPlayerId = ack.OptionPlayer;
        
        // 【关键检查】如果 OptionPlayer <= 0，说明游戏已结束（有玩家出完所有牌），不再执行后续操作
        // 服务器会通过 OptionPlayer=0 或直接发送结算消息来表示游戏结束
        if (nextPlayerId <= 0)
        {
            GF.LogInfo($"[PDK出牌] OptionPlayer={nextPlayerId}，游戏已结束，停止处理");
            return;
        }
        
        bool isMyTurn = (nextPlayerId == myPlayerId);

        // 【关键判断】是否应该清空下一个玩家的出牌区域
        // 逻辑：只有当前玩家实际"出牌"（非过牌）且下一个操作玩家是新的首出玩家时，才清空
        // 
        // 情况1：当前玩家出牌 && 下一个玩家 == 首出玩家 → 清空（新一轮开始）
        // 情况2：当前玩家出牌 && 下一个玩家 != 首出玩家 → 不清空（轮回，保留之前的牌）
        // 情况3：当前玩家过牌 → 不清空（无论下一个是谁，都保留下一个玩家之前的牌）
        bool shouldClearPlayArea = !isPass && (nextPlayerId == ack.MaxDisCardPlayer);

        // 4. 根据服务器指定的操作玩家显示/隐藏按钮
        GamePanel_pdk.ShowPlayButtons(isMyTurn);

        // 5. 设置下一个操作玩家（传递是否清空出牌区域的标志）
        GamePanel_pdk.SetNextPlayer(nextPlayerId, shouldClearPlayArea);
        // 6. 如果轮到我，延迟执行智能出牌检查逻辑（等待卡牌初始化完成）
        if (isMyTurn)
        {
            CoroutineRunner.Instance.RunCoroutine(DelayedCheckPlayableCards());
        }

    }

    /// <summary>
    /// 延迟检查可出牌（等待卡牌初始化完成）
    /// </summary>
    private IEnumerator DelayedCheckPlayableCards()
    {
        // 等待2帧，确保卡牌的 Init 方法和 Start 方法都执行完成
        yield return null;
        yield return null;
        GamePanel_pdk.CheckPlayableCardsOnMyTurn();
    }

    /// <summary>
    /// 同步跑的快桌子状态变化
    /// </summary>
    private void Function_SynChangeRunFastDeskState(MessageRecvData data)
    {
        Syn_ChangeRunFastDeskState ack = Syn_ChangeRunFastDeskState.Parser.ParseFrom(data.Data);
        // 【关键修复】同步更新本地桌子状态，确保其他地方读取的状态是最新的
        if (EnterRunFastDeskRs != null)
        {
            RunFastDeskState oldState = EnterRunFastDeskRs.DeskState;
            EnterRunFastDeskRs.DeskState = ack.DeskState;
        }

        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        bool isMyTurn = (ack.CurrentOption == myPlayerId);

        // 【观战者逻辑】判断是否为观战者（不在DeskPlayers列表中）
        bool isSpectator = EnterRunFastDeskRs.DeskPlayers == null ||
                          !EnterRunFastDeskRs.DeskPlayers.Any(p => p.BasePlayer.PlayerId == myPlayerId);

        // 当状态不是准备阶段时，观战者关闭战绩页面和隐藏下局按钮
        if (isSpectator && ack.DeskState != RunFastDeskState.RunfastWait)
        {
            GF.LogInfo_wl("观战收到跑的快桌子状态变化消息");
            GamePanel_pdk.varNextGamePlay.SetActive(false);
            //关闭已就绪显示
            GamePanel_pdk.ClearGameState();
        }

        // 【关键】只在出牌阶段初始化所有玩家的剩牌数量(发牌都是16张)
        // 这样旁观者也能收到初始化，不依赖发牌消息
        // 注意：不能在结算阶段初始化，会覆盖实际剩余手牌数
        if (ack.DeskState == RunFastDeskState.RunfastDiscard)
        {
            List<long> playerIdsList = new List<long>(GamePanel_pdk.playerIdToHeadBg.Keys);
            if (playerIdsList.Count > 0)
            {
                GamePanel_pdk.InitializePlayerCardCounts(playerIdsList.ToArray(), 16);
            }
        }

        // 根据服务器当前发的状态判断
        switch (ack.DeskState)
        {
            case RunFastDeskState.RunfastWait:
                GF.LogInfo_wl("跑的快进入准备阶段");
                //准备阶段
                GamePanel_pdk.ShowPlayButtons(false);
                ShowRobBankerUI(false);
                HideAllPlayerRobBankerStatus();
                if (GamePanel_pdk.DoubleBtn != null) GamePanel_pdk.DoubleBtn.SetActive(false);
                HideAllPlayerDoubleStatus();
                break;

            case RunFastDeskState.RunfastRob:
                GF.LogInfo_wl("跑的快进入抢庄阶段");
                //抢庄阶段
                GamePanel_pdk.ShowPlayButtons(false);
                bool shouldShowRobUI = (ack.CurrentOption == myPlayerId);
                ShowRobBankerUI(shouldShowRobUI);
                if (GamePanel_pdk.DoubleBtn != null) GamePanel_pdk.DoubleBtn.SetActive(false);
                // 显示抢庄者的倒计时
                if (ack.CurrentOption > 0)
                {
                    GamePanel_pdk.SetNextPlayer(ack.CurrentOption);
                }
                break;

            case RunFastDeskState.RunfastStart:
                GF.LogInfo_wl("跑的快进入翻倍阶段");
                //翻倍阶段
                GamePanel_pdk.UpdateDoubleBtnDisplay();
                ShowRobBankerUI(false);
                HideAllPlayerRobBankerStatus();
                //显示庄家
                GamePanel_pdk.UpdateAllPlayerBankerStatus(
                    EnterRunFastDeskRs.DeskPlayers,
                    EnterRunFastDeskRs.BaseConfig.PlayerNum,
                    GamePanel_pdk.bankerId
                );
                break;

            case RunFastDeskState.RunfastDiscard:
                GF.LogInfo_wl("跑的快进入出牌阶段");
                // 出牌阶段
                ShowRobBankerUI(false);
                HideAllPlayerRobBankerStatus();
                if (GamePanel_pdk.DoubleBtn != null) GamePanel_pdk.DoubleBtn.SetActive(false);
                GamePanel_pdk.ShowPlayButtons(isMyTurn);
                // 显示当前操作者的倒计时
                if (ack.CurrentOption > 0)
                {
                    GamePanel_pdk.SetNextPlayer(ack.CurrentOption);
                }
                break;

            case RunFastDeskState.RunfastSettle:
                GF.LogInfo_wl("跑的快进入结算阶段");
                // 结算阶段
                GamePanel_pdk.ShowPlayButtons(false);
                ShowRobBankerUI(false);
                HideAllPlayerRobBankerStatus();
                if (GamePanel_pdk.DoubleBtn != null) GamePanel_pdk.DoubleBtn.SetActive(false);
                HideAllPlayerDoubleStatus();
                ResetAllSettlementUI();
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// 重置所有玩家的结算UI状态(避免上一局数据残留)
    /// </summary>
    private void ResetAllSettlementUI()
    {
        foreach (Transform child in GamePanel_pdk.varGameOverCard.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        foreach (Transform child in GamePanel_pdk.varGameOverCard1.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        foreach (Transform child in GamePanel_pdk.varGameOverCard2.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        // 2. 遍历所有可能的结算节点（player0, player1, player2）
        Transform[] possibleNodes = new Transform[3];
        possibleNodes[0] = GamePanel_pdk.varNextGamePlay.transform.Find("player0");
        possibleNodes[1] = GamePanel_pdk.varNextGamePlay.transform.Find("player1");
        possibleNodes[2] = GamePanel_pdk.varNextGamePlay.transform.Find("player2");

        foreach (var node in possibleNodes)
        {
            if (node == null) continue;

            Transform ranking = node.Find("ranking");
            Transform finish = node.Find("finish");
            if (ranking != null) ranking.gameObject.SetActive(false);
            if (finish != null) finish.gameObject.SetActive(false);
            Transform bg = node.Find("bg");
            if (bg == null) continue;
            Transform winScore = bg.Find("WinScore");
            Transform loseScore = bg.Find("LoseScore");
            if (winScore != null) winScore.gameObject.SetActive(false);
            if (loseScore != null) loseScore.gameObject.SetActive(false);
        }

    }

    /// <summary>
    /// 处理金币变化消息（与麻将共用）
    /// 服务器在结算后会单独发送此消息通知金币变化
    /// </summary>
    private void Function_Syn_HHCoinChange(MessageRecvData data)
    {
        Syn_HHCoinChange ack = Syn_HHCoinChange.Parser.ParseFrom(data.Data);
        // 【关键判断】判断是否在结算状态，结算时不播放金币飞行动画
        bool isInSettlement = (EnterRunFastDeskRs != null &&
                               EnterRunFastDeskRs.DeskState == RunFastDeskState.RunfastSettle);

        foreach (var change in ack.ScoreChange)
        {
            long playerId = change.Key;
            double scoreChange = change.Val;

            // 更新 EnterRunFastDeskRs.DeskPlayers 中的金币数据
            if (EnterRunFastDeskRs != null && EnterRunFastDeskRs.DeskPlayers != null)
            {
                DeskPlayer deskPlayer = null;
                foreach (var player in EnterRunFastDeskRs.DeskPlayers)
                {
                    if (player.BasePlayer.PlayerId == playerId)
                    {
                        deskPlayer = player;
                        break;
                    }
                }
            }
            // 显示分数变化特效
            GamePanel_pdk.ShowScoreChangeEffect(playerId, scoreChange);
        }

        if (!isInSettlement && ack.ScoreChange != null && ack.ScoreChange.Count > 0)
        {
            // 收集输赢信息用于金币飞行动画
            List<(long playerId, double score)> losers = new List<(long, double)>();
            List<(long playerId, double score)> winners = new List<(long, double)>();
            foreach (var change in ack.ScoreChange)
            {
                if (change.Val < 0)
                {
                    losers.Add((change.Key, change.Val));
                }
                else if (change.Val > 0)
                {
                    winners.Add((change.Key, change.Val));
                }
            }
            // 如果有输赢，播放金币飞行动画
            if (losers.Count > 0 && winners.Count > 0)
            {
                CoroutineRunner.Instance.StartCoroutine(DelayPlayCoinAnimation(losers, winners, 0.3f));
            }
        }
    }
    /// <summary>
    /// 同步跑得快结算消息
    /// </summary>
    private void Function_SynRunFastSettle(MessageRecvData data)
    {
        Syn_RunFastSettle ack = Syn_RunFastSettle.Parser.ParseFrom(data.Data);
        GF.LogInfo_wl("收到跑的快结算消息", ack.ToString());
        // 【关键修复】局数增加移到结算消息处理：旁观者也能收到结算消息同步局数
        // 增加防守机制：如果整局结束(ack.Over)或者已达上限，则不增加局数
        int totalRounds = (int)(EnterRunFastDeskRs?.BaseConfig?.PlayerTime ?? 0);
        if (!ack.Over)
        {
            currentRoundNum++;
            if (EnterRunFastDeskRs != null)
            {
                EnterRunFastDeskRs.Round = currentRoundNum;
            }
            GF.LogInfo_wl($"<color=#00FF00>[结算] 本局结束，局数更新为: {currentRoundNum}/{totalRounds}</color>");
            GamePanel_pdk.UpdateRoundDisplay();
        }
        else
        {
            // 防守机制：确保结算后局数不显示超出总局数，如果是整局结束，保持当前局数
            if (currentRoundNum > totalRounds && totalRounds > 0)
            {
                currentRoundNum = totalRounds;
                if (EnterRunFastDeskRs != null) EnterRunFastDeskRs.Round = currentRoundNum;
            }
            GamePanel_pdk.UpdateRoundDisplay();
        }

        GamePanel_pdk.HideAllPlayerClocks();
        GamePanel_pdk.HideAllPassIndicators(); // 【修复】结算时清空所有玩家的"不要"标识
        if (ack.Info != null && ack.Info.Count > 0)
        {
            GamePanel_pdk.ShowPlayButtons(false);
            foreach (var playerInfo in ack.Info)
            {
                if (playerInfo.HandCard.Count == 0) continue;
                GamePanel_pdk.ClearPlayAreaByPlayerId(playerInfo.PlayerId);
            }
            ResetAllSettlementUI();
            // ack.Chun: 0=无, 1=春天, 2=反春天
            bool shouldPlaySpringEffect = (ack.Chun == 1 || ack.Chun == 2);
            if (shouldPlaySpringEffect)
            {
                // 播放春天/反春天特效，等待动画播放完成后再显示结算面板
                GamePanel_pdk.PlaySpringEffectFromServer(ack.Chun, () =>
                {
                    // 动画播放完成后的回调，显示结算面板
                    CoroutineRunner.Instance.RunCoroutine(DelayShowSettlement(ack, 0f));
                });
            }
            else
            {
                CoroutineRunner.Instance.RunCoroutine(DelayShowSettlement(ack, 0f));
            }
            // 【关键修改】判断是否整局结束（服务器标记Over或本地有解散请求）
            bool isGameOver = ack.Over || HasDismissRequest;
            GF.LogInfo_wl("isGameOver" + isGameOver);
            if (isGameOver)
            {
                GF.LogInfo_wl("整局结束，显示总结算按钮");
                GamePanel_pdk.varNextGame.SetActive(false);
                GamePanel_pdk.varRecord.SetActive(true);
                if (GamePanel_pdk.XJdaojishi != null) GamePanel_pdk.XJdaojishi.SetActive(false);
                CancelCountdown(); // 局结束，不再需要倒计时
            }
            else
            {
                GF.LogInfo_wl("准备开始下一局倒计时");
                GamePanel_pdk.UpdateSettlementButtons();
                if (!GamePanel_pdk.IsReady && GamePanel_pdk.XJdaojishi != null)
                {
                    // 显示下局倒计时文本并在其上开始倒计时
                    GamePanel_pdk.XJdaojishi.SetActive(true);
                    StartCountdown(GamePanel_pdk.XJdaojishi.GetComponent<Text>(), 15).Forget();
                }
            }
        }
    }

    /// <summary>
    /// 延迟显示结算面板（等待特效播放完成）
    /// </summary>
    /// <param name="ack">结算消息</param>
    /// <param name="delayTime">延迟时间（秒）</param>
    private IEnumerator DelayShowSettlement(Syn_RunFastSettle ack, float delayTime)
    {
        if (delayTime > 0)
        {
            yield return new WaitForSeconds(delayTime);
        }

        // 显示结算面板
        GamePanel_pdk.varNextGamePlay.gameObject.SetActive(true);
        //如果为观战者
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        bool isSpectator = EnterRunFastDeskRs.DeskPlayers == null ||
                          !EnterRunFastDeskRs.DeskPlayers.Any(p => p.BasePlayer.PlayerId == myPlayerId);

        // 【关键修复】根据服务器返回的over字段来控制按钮显示
        // over=true: 牌桌结束，显示总结算按钮
        // over=false: 继续游戏，显示下一局按钮
        if (ack.Over)
        {
            // 牌桌结束，显示总结算按钮
            GamePanel_pdk.varRecord.SetActive(true);
            GamePanel_pdk.varNextGame.SetActive(false);
        }
        else
        {
            // 继续游戏，显示下一局按钮
            GamePanel_pdk.UpdateSettlementButtons();
        }
        if (isSpectator)
        {
            GamePanel_pdk.varNextGamePlay.transform.Find("nextGame").gameObject.SetActive(false);
            GamePanel_pdk.varNextGamePlay.transform.Find("Record").gameObject.SetActive(false);
        }
        // 判断是否是2人局（需要显示牌堆）
        bool isTwoPlayerGame = (ack.Info.Count == 2);

        // 【关键】获取玩家数量，用于位置映射
        int playerCount = EnterRunFastDeskRs?.BaseConfig?.PlayerNum ?? 3;

        // 先找到自己的服务端位置
        Position myServerPos = Position.Default;
        if (EnterRunFastDeskRs != null && EnterRunFastDeskRs.DeskPlayers != null)
        {
            foreach (var deskPlayer in EnterRunFastDeskRs.DeskPlayers)
            {
                if (deskPlayer.BasePlayer.PlayerId == myPlayerId)
                {
                    myServerPos = deskPlayer.Pos;
                    break;
                }
            }
        }




        // 遍历所有玩家的结算信息
        foreach (var playerInfo in ack.Info)
        {
            long playerId = playerInfo.PlayerId;
            double score = playerInfo.Score;
            int handCardCount = playerInfo.HandCard?.Count ?? 0;
            int clientPos = -1;
            long myPlayerId1 = Util.GetMyselfInfo().PlayerId;
            Position serverPos = Position.Default;
            if (playerId == myPlayerId)
            {
                clientPos = 0;
            }
            else if (EnterRunFastDeskRs != null && EnterRunFastDeskRs.DeskPlayers != null)
            {
                foreach (var deskPlayer in EnterRunFastDeskRs.DeskPlayers)
                {
                    if (deskPlayer.BasePlayer.PlayerId == playerId)
                    {
                        serverPos = deskPlayer.Pos;
                        clientPos = Util.GetPosByServerPos_pdk(myServerPos, deskPlayer.Pos, playerCount);
                        break;
                    }
                }
            }

            // 【增强调试】检查 playerIdToHeadBg 映射
            GameObject debugHeadBg = null;
            if (GamePanel_pdk.playerIdToHeadBg.TryGetValue(playerId, out debugHeadBg))
            {
                string headBgName = debugHeadBg == GamePanel_pdk.varHeadBg0 ? "varHeadBg0" :
                                   debugHeadBg == GamePanel_pdk.varHeadBg1 ? "varHeadBg1" :
                                   debugHeadBg == GamePanel_pdk.varHeadBg2 ? "varHeadBg2" : "Unknown";
                GF.LogInfo_wl($"<color=#FFFF00>[头像映射] PlayerId={playerId} -> {headBgName}</color>");
            }
            else
            {
                GF.LogError($"<color=#FF0000>[头像映射错误] PlayerId={playerId} 不在playerIdToHeadBg中！</color>");
            }

            // 【关键修复】直接使用 clientPos 查找结算UI节点，不要尝试用 playerId
            // 结算面板的节点名称固定为 player0, player1, player2
            Transform finishedPlayerUI = null;
            string playerNodeName = "";

            if (clientPos >= 0)
            {
                playerNodeName = $"player{clientPos}";
                finishedPlayerUI = GamePanel_pdk.varNextGamePlay.transform.Find(playerNodeName);
            }

            if (finishedPlayerUI != null)
            {
                GF.LogInfo_wl(finishedPlayerUI.name);
                // 查找UI节点
                Transform bg = finishedPlayerUI.Find("bg");
                if (bg == null)
                {
                    continue;
                }
                Transform winScore = bg.Find("WinScore");
                Transform loseScore = bg.Find("LoseScore");
                Transform cardNum = bg.Find("cardNum");
                // 显示分数
                if (score >= 0 && winScore != null)
                {
                    // 赢分或平分：显示 WinScore
                    winScore.gameObject.SetActive(true);
                    if (loseScore != null) loseScore.gameObject.SetActive(false);

                    var winText = winScore.GetComponent<UnityEngine.UI.Text>();
                    if (winText != null)
                    {
                        winText.text = $"+{score:F2}";
                    }
                }
                else if (score < 0 && loseScore != null)
                {
                    // 输分：显示 LoseScore
                    loseScore.gameObject.SetActive(true);
                    if (winScore != null) winScore.gameObject.SetActive(false);

                    var loseText = loseScore.GetComponent<UnityEngine.UI.Text>();
                    if (loseText != null)
                    {
                        loseText.text = $"{score:F2}";
                    }
                }
                // 显示剩余手牌数
                if (cardNum != null)
                {
                    var cardNumText = cardNum.GetComponent<UnityEngine.UI.Text>();
                    if (cardNumText != null)
                    {
                        cardNumText.text = $"剩余{handCardCount}张，";
                    }
                }
                // 如果手牌为0，显示排名UI（第一名出完牌）
                if (handCardCount == 0)
                {
                    Transform ranking = finishedPlayerUI.Find("ranking");
                    Transform finish = finishedPlayerUI.Find("finish");

                    if (ranking != null) ranking.gameObject.SetActive(true);
                    if (finish != null) finish.gameObject.SetActive(true);

                }
                else if (handCardCount > 0)
                {
                    // 【关键修复】有剩余手牌时,确保隐藏"出完"UI（避免上一局数据残留）
                    Transform ranking = finishedPlayerUI.Find("ranking");
                    Transform finish = finishedPlayerUI.Find("finish");
                    if (ranking != null) ranking.gameObject.SetActive(false);
                    if (finish != null) finish.gameObject.SetActive(false);

                    GameObject cardArea = null;

                    // 通过 playerIdToHeadBg 映射找到玩家的头像框
                    if (GamePanel_pdk.playerIdToHeadBg.TryGetValue(playerId, out GameObject headBg))
                    {
                        // 根据头像框确定结算卡牌显示区域
                        if (headBg == GamePanel_pdk.varHeadBg0)
                        {
                            cardArea = GamePanel_pdk.varGameOverCard;
                        }
                        else if (headBg == GamePanel_pdk.varHeadBg1)
                        {
                            cardArea = GamePanel_pdk.varGameOverCard1;
                        }
                        else if (headBg == GamePanel_pdk.varHeadBg2)
                        {
                            cardArea = GamePanel_pdk.varGameOverCard2;
                        }
                    }
                    else
                    {

                        if (clientPos == 0)
                        {
                            cardArea = GamePanel_pdk.varGameOverCard;
                        }
                        else if (clientPos == 1)
                        {
                            cardArea = GamePanel_pdk.varGameOverCard1;
                        }
                        else if (clientPos == 2)
                        {
                            cardArea = GamePanel_pdk.varGameOverCard2;
                        }
                    }

                    if (cardArea != null)
                    {
                        // 清空旧的手牌
                        foreach (Transform child in cardArea.transform)
                        {
                            GameObject.Destroy(child.gameObject);
                        }

                        // 显示剩余手牌（结算区）
                        List<int> handCards = new List<int>(playerInfo.HandCard);
                        // 结算区手牌从大到小排序
                        handCards.Sort((a, b) => b.CompareTo(a));
                        GamePanel_pdk.DisplayCardsInSettlementArea(cardArea, handCards);
                    }
                }
            }
        }
        // 如果是2人局，显示牌堆在第3个玩家位置（varGameOverCard2）
        if (isTwoPlayerGame && ack.CardPool != null && ack.CardPool.Count > 0)
        {

            GameObject cardPoolArea = GamePanel_pdk.varGameOverCard2;
            // 清空旧的牌
            foreach (Transform child in cardPoolArea.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
            // 显示牌堆
            List<int> cardPool = new List<int>(ack.CardPool);
            GamePanel_pdk.DisplayCardsInSettlementArea(cardPoolArea, cardPool);
        }
        GamePanel_pdk.UpdateRoundDisplay();
    }

    /// <summary>
    /// 延迟播放金币飞行动画
    /// </summary>
    private IEnumerator DelayPlayCoinAnimation(List<(long playerId, double score)> losers, List<(long playerId, double score)> winners, float delay)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        if (GamePanel_pdk != null)
        {
            GamePanel_pdk.PlayCoinFlyAnimation(losers, winners);
        }
    }
    #endregion

    #region 游戏规则更新

    /// <summary>
    /// 从服务器数据更新游戏规则配置
    /// </summary>
    /// <param name="enterData">进入桌子的服务器数据</param>
    private void UpdateGameRules(Msg_EnterRunFastDeskRs enterData)
    {
        PDKConstants.UpdateRulesFromServer(enterData.Config);

    }
    #endregion
    #region 桌子初始化
    /// <summary>
    /// 从服务器数据初始化跑得快桌子
    /// </summary>
    /// <param name="enterData">服务器返回的进入桌子数据</param>
    private void InitPDKDeskFromServer(Msg_EnterRunFastDeskRs enterData)
    {
        // 【修复】同步服务器局数，确保重连或重新进入时局数正确
        currentRoundNum = enterData.Round > 0 ? enterData.Round : 1;
        // 【发牌修复】根据服务器局数或桌子状态判断是否已经开始游戏
        hasReceivedFirstDeal = (currentRoundNum > 1 || enterData.DeskState != RunFastDeskState.RunfastWait);
        GF.LogInfo_wl($"[InitPDKDeskFromServer] 服务器局数={enterData.Round}, 状态={enterData.DeskState}, 本地设置currentRoundNum={currentRoundNum}, hasReceivedFirstDeal={hasReceivedFirstDeal}");
        // 同时更新协议数据中的Round字段，保持一致性
        if (EnterRunFastDeskRs != null)
        {
            EnterRunFastDeskRs.Round = currentRoundNum;
        }
       GamePanel_pdk.InitPDKDesk(enterData);
    }
    #endregion
    #region 离开桌子
    /// <summary>
    /// 发送离开桌子请求
    /// </summary>
    public void Send_LeaveDeskRq()
    {
        Msg_LeaveDeskRq req = MessagePool.Instance.Fetch<Msg_LeaveDeskRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_LeaveDeskRq, req);
    }
    #endregion
    #region 退出游戏

    /// <summary>
    /// 退出跑的快游戏，返回麻将大厅
    /// </summary>
    public void ExitPDKGame()
    {
        // 切换到麻将大厅流程
        ChangeState<MJHomeProcedure>(procedure);
    }

    #endregion

    #region 隐藏加倍状态

    /// <summary>
    /// 隐藏所有玩家的加倍标识
    /// </summary>
    public void HideAllPlayerDoubleStatus()
    {
        if (GamePanel_pdk == null || GamePanel_pdk.playerIdToHeadBg == null)
        {
            return;
        }

        foreach (var kvp in GamePanel_pdk.playerIdToHeadBg)
        {
            GameObject headBg = kvp.Value;
            if (headBg == null) continue;

            Transform jiabeiTrans = headBg.transform.Find("jiabei");
            if (jiabeiTrans != null)
            {
                jiabeiTrans.gameObject.SetActive(false);
            }
        }
    }

    #endregion

    /// <summary>
    /// 开始倒计时并更新UI
    /// </summary>
    /// <param name="text">显示的Text组件</param>
    /// <param name="seconds">起始秒数</param>
    public async UniTask StartCountdown(Text text, int seconds)
    {
        if (countdownTokenSource != null)
        {
            countdownTokenSource.Cancel();
            countdownTokenSource.Dispose();
        }
        countdownTokenSource = new System.Threading.CancellationTokenSource();
        try
        {
            while (seconds >= 0)
            {
                text.text = seconds.ToString();
                await UniTask.Delay(1000, cancellationToken: countdownTokenSource.Token);
                seconds--;
            }
        }
        catch (System.OperationCanceledException)
        {
            // 倒计时取消
        }
        catch (System.Exception e)
        {
            GF.LogError("倒计时更新异常: " + e.Message);
        }
    }

    /// <summary>
    /// 取消当前进行的倒计时
    /// </summary>
    public void CancelCountdown()
    {
        if (countdownTokenSource != null)
        {
            countdownTokenSource.Cancel();
            countdownTokenSource.Dispose();
            countdownTokenSource = null;
        }
    }
}
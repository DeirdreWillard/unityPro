using Cysharp.Threading.Tasks;
using GameFramework.Procedure;
using GameFramework.Fsm;
using NetMsg;
using UnityGameFramework.Runtime;
using UnityEngine.SceneManagement;
using UnityEngine;
using GameFramework.Event;
using System.Collections.Generic;
using System;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class MJGameProcedure : ProcedureBase
{
    private IFsm<IProcedureManager> procedure;
    private BaseMahjongGameManager baseMahjongGameManager;
    public MahjongGameUI GamePanel_mj { get; private set; }

    public int deskID;
    public Msg_EnterMJDeskRs enterMJDeskRs;
    public bool brecord = false;
    
    /// <summary>
    /// 标记初始化是否完成，用于消息缓存判断
    /// </summary>
    private bool isInitialized = false;
    
    /// <summary>
    /// 初始化期间缓存的消息队列
    /// </summary>
    private Queue<(Action action, string msgName)> pendingMessages = new Queue<(Action, string)>();

    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        
        // 重置初始化状态
        isInitialized = false;
        pendingMessages.Clear();

        CoroutineRunner.Instance.RunCoroutine(GFBuiltin.Instance.SwitchToLandscape(async () =>
        {
            this.procedure = procedureOwner;
            GF.LogInfo("进入麻将游戏流程!");

            var data1 = procedure.GetData<VarByteArray>("enterMJDeskRs");
            enterMJDeskRs = Msg_EnterMJDeskRs.Parser.ParseFrom(data1);
            
            GF.LogInfo("麻将桌子信息", enterMJDeskRs.ToString());
            deskID = enterMJDeskRs.BaseInfo.DeskId;
            Util.GetMyselfInfo().DeskId = deskID;
            
            // 保存进入桌子前的亲友圈房间ID（如果尚未保存）
            var leagueInfo = GlobalManager.GetInstance().LeagueInfo;
            if (leagueInfo != null && leagueInfo.LeagueId > 0)
            {
                // 只有在没有保存过的情况下才保存，避免覆盖之前保存的ID
                if (GlobalManager.GetInstance().PeekBeforeEnterDeskLeagueId() == 0)
                {
                    GlobalManager.GetInstance().SaveBeforeEnterDeskLeagueId(leagueInfo.LeagueId);
                    GF.LogInfo($"[麻将游戏] 保存亲友圈房间ID: {leagueInfo.LeagueId}");
                }
            }
            
            // 大厅打开时后台预加载麻将资源，提升进入游戏的流畅度
            if (!MahjongResourceManager.IsInitialized())
            {
                GF.LogInfo("[大厅] 开始后台预加载麻将资源...");
                MahjongResourceManager.PreloadAllInBackground();
            }
            
            // 等待UI加载完成
            await ShowbaseMahjongGameManagerPanel();
        }));
    }

    public BaseMahjongGameManager GetCurrentBaseMahjongGameManager()
    {
        return baseMahjongGameManager;
    }

    /// <summary>
    /// 判断当前玩家是否是旁观者（未坐下）
    /// </summary>
    private bool IsSpectator()
    {
        return baseMahjongGameManager?.GetSeatIndexByPlayerId(Util.GetMyselfInfo().PlayerId) == -1;
    }

    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        GlobalManager.GetInstance().ReSetUI();
        RemoveListener();
		Util.GetMyselfInfo().DeskId = 0;
        
        // 重置状态
        isInitialized = false;
        pendingMessages.Clear();
        
        // 清理全局消息缓存
        MJMessageCache.Clear();
        
        base.OnLeave(procedureOwner, isShutdown);
    }

    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
    }

    public async Cysharp.Threading.Tasks.UniTask ShowbaseMahjongGameManagerPanel()
    {
        GF.LogInfo("打开麻将界面");
        
        // 【重要】提前注册网络监听器，避免在UI加载期间丢失协议
        // 在资源加载和UI初始化完成前就开始监听，协议会被缓存等待处理
        AddListener();
        
        var gamePanel = await GF.UI.OpenUIFormAwait(UIViews.MJGamePanel);
        
        if (gamePanel != null)
        {
            GamePanel_mj = gamePanel as MahjongGameUI;
            baseMahjongGameManager = new BaseMahjongGameManager(this, GamePanel_mj);
            
            // 异步加载麻将缓存数据
            await baseMahjongGameManager.InitializeMahjongCacheAsync();
            
            baseMahjongGameManager.InitMahjongDesk(enterMJDeskRs);
            
            brecord = false;
            
            // 标记初始化完成
            isInitialized = true;
            
            // 处理本地缓存的消息
            ProcessPendingMessages();
            
            // 处理全局缓存的消息（在进入流程前由网络层缓存）
            ProcessGlobalCachedMessages();
            
            GlobalManager.SendSystemConfigRq();
        }
    }
    
    /// <summary>
    /// 处理全局缓存的麻将消息（由MJMessageCache在进入流程前缓存）
    /// </summary>
    private void ProcessGlobalCachedMessages()
    {
        var cachedMessages = MJMessageCache.StopCachingAndGetMessages();
        
        if (cachedMessages.Count > 0)
        {
            GF.LogInfo($"[麻将] 开始处理 {cachedMessages.Count} 条全局缓存消息");
        }
        
        while (cachedMessages.Count > 0)
        {
            var (msgId, data) = cachedMessages.Dequeue();
            try
            {
                GF.LogInfo($"[麻将] 处理全局缓存消息: {msgId}");
                
                // 构造消息数据并分发
                var recvData = new MessageRecvData
                {
                    MsgID = (int)msgId,
                    Data = data
                };
                
                // 调用对应的处理函数
                DispatchCachedMessage(msgId, recvData);
            }
            catch (Exception e)
            {
                GF.LogError($"[麻将] 处理全局缓存消息 {msgId} 失败: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// 分发缓存的消息到对应处理函数
    /// </summary>
    private void DispatchCachedMessage(MessageID msgId, MessageRecvData data)
    {
        switch (msgId)
        {
            case MessageID.Syn_DeskPlayerInfo:
            case MessageID.Syn_DeskPlayerInfo1:
            case MessageID.Syn_DeskPlayerInfo2:
                Function_DeskPlayerInfoRs(data);
                break;
            case MessageID.Msg_SynSitUp:
                Function_SynSitUp(data);
                break;
            case MessageID.PBMJStart:
                Function_PBMJStart(data);
                break;
            case MessageID.Msg_MJOption1:
                Function_Msg_MJOption1(data);
                break;
            case MessageID.Msg_MJOption2:
                Function_Msg_MJOption2(data);
                break;
            case MessageID.Msg_MJOption3:
                Function_Msg_MJOption3(data);
                break;
            case MessageID.Msg_MJOption4:
                Function_Msg_MJOption4(data);
                break;
            case MessageID.Msg_MJOption5:
                Function_Msg_MJOption5(data);
                break;
            case MessageID.Msg_MJOption6:
                Function_Msg_MJOption6(data);
                break;
            case MessageID.Msg_MJOption7:
                Function_Msg_MJOption7(data);
                break;
            case MessageID.Msg_MJOption8:
                Function_Msg_MJOption8(data);
                break;
            case MessageID.Msg_MJOption9:
                Function_Msg_MJOption9(data);
                break;
            case MessageID.Syn_HHCoinChange:
                Function_Syn_HHCoinChange(data);
                break;
            case MessageID.Msg_Hu:
                Function_Msg_Hu(data);
                break;
            case MessageID.Msg_MJReadyRs:
                Function_Msg_MJReadyRs(data);
                break;
            case MessageID.Msg_SynMJReady:
                Function_Msg_SynMJReady(data);
                break;
            case MessageID.Msg_SynMJDismiss:
                Function_Msg_SynMJDismiss(data);
                break;
            case MessageID.MSG_ChangeMJDeskState:
                Function_MSG_ChangeMJDeskState(data);
                break;
            case MessageID.Msg_SynPiaoRs:
                Function_Msg_SynPiaoRs(data);
                break;
            case MessageID.Msg_ChangeCardRs:
                Function_Msg_ChangeCardRs(data);
                break;
            case MessageID.Msg_SynChangeCardRs:
                Function_Msg_SynChangeCardRs(data);
                break;
            case MessageID.Msg_SynHandCard:
                Function_Msg_SynHandCard(data);
                break;
            case MessageID.Msg_SynPlayerHandCard:
                Function_Msg_SynPlayerHandCard(data);
                break;
            case MessageID.Msg_ThrowCardRs:
                Function_Syn_ThrowCardRs(data);
                break;
            case MessageID.Msg_XlSettle:
                Function_Msg_XlSettle(data);
                break;
            case MessageID.SynPrayRs:
                Function_Syn_Pray_Rs(data);
                break;
            case MessageID.Msg_SynPlayerState:
                Function_Msg_SynPlayerState(data);
                break;
            case MessageID.Syn_PlayerAuto:
                Function_Syn_PlayerAuto(data);
                break;
            case MessageID.Msg_SynSendGift:
                Function_SynSendGift(data);
                break;
            default:
                GF.LogWarning($"[麻将] 未处理的缓存消息类型: {msgId}");
                break;
        }
    }
    
    /// <summary>
    /// 处理初始化期间缓存的消息
    /// </summary>
    private void ProcessPendingMessages()
    {
        int count = pendingMessages.Count;
        if (count > 0)
        {
            GF.LogInfo($"[麻将] 开始处理 {count} 条缓存的消息");
        }
        
        while (pendingMessages.Count > 0)
        {
            var (action, msgName) = pendingMessages.Dequeue();
            try
            {
                GF.LogInfo($"[麻将] 处理缓存消息: {msgName}");
                action?.Invoke();
            }
            catch (Exception e)
            {
                GF.LogError($"[麻将] 处理缓存消息 {msgName} 失败: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// 如果未初始化完成，将消息加入缓存队列；否则直接执行
    /// </summary>
    private bool TryQueueOrExecute(Action action, string msgName)
    {
        if (!isInitialized)
        {
            pendingMessages.Enqueue((action, msgName));
            GF.LogInfo($"[麻将] 缓存消息: {msgName}（初始化未完成）");
            return true; // 已缓存，调用方不需要处理
        }
        return false; // 未缓存，调用方需要继续处理
    }



    bool isListenerAdded = false;

    public void AddListener()
    {
        //避免重复添加
        if (isListenerAdded)
        {
            return;
        }
        isListenerAdded = true;

        GF.Event.Subscribe(GFEventArgs.EventId, OnGFEventChanged);
        HotfixNetworkComponent.AddListener(MessageID.Msg_EnterMJDeskRs, Function_EnterMJDeskRs);
		HotfixNetworkComponent.AddListener(MessageID.Msg_DisMissDeskRs, Function_DismissDesk);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynSendGift, Function_SynSendGift);
        HotfixNetworkComponent.AddListener(MessageID.Msg_LeaveDeskRs, Function_LeaveDeskRs);
        HotfixNetworkComponent.AddListener(MessageID.SingleGameRecord, Function_SingleGameRecord);
        HotfixNetworkComponent.AddListener(MessageID.Syn_DeskPlayerInfo, Function_DeskPlayerInfoRs);
        HotfixNetworkComponent.AddListener(MessageID.Syn_DeskPlayerInfo1, Function_DeskPlayerInfoRs);
        HotfixNetworkComponent.AddListener(MessageID.Syn_DeskPlayerInfo2, Function_DeskPlayerInfoRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynSitUp, Function_SynSitUp);
        HotfixNetworkComponent.AddListener(MessageID.PBMJStart, Function_PBMJStart);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MJOption1, Function_Msg_MJOption1);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MJOption2, Function_Msg_MJOption2);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MJOption3, Function_Msg_MJOption3);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MJOption4, Function_Msg_MJOption4);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MJOption5, Function_Msg_MJOption5);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MJOption6, Function_Msg_MJOption6);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MJOption7, Function_Msg_MJOption7);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MJOption8, Function_Msg_MJOption8);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MJOption9, Function_Msg_MJOption9);
        HotfixNetworkComponent.AddListener(MessageID.Syn_HHCoinChange, Function_Syn_HHCoinChange);
        HotfixNetworkComponent.AddListener(MessageID.Msg_Hu, Function_Msg_Hu);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MJReadyRs, Function_Msg_MJReadyRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynMJReady, Function_Msg_SynMJReady);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynMJDismiss, Function_Msg_SynMJDismiss);
        HotfixNetworkComponent.AddListener(MessageID.MSG_ChangeMJDeskState, Function_MSG_ChangeMJDeskState);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynPiaoRs, Function_Msg_SynPiaoRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_ChangeCardRs, Function_Msg_ChangeCardRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynChangeCardRs, Function_Msg_SynChangeCardRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynHandCard, Function_Msg_SynHandCard);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynPlayerHandCard, Function_Msg_SynPlayerHandCard);
        // 血流成河
        HotfixNetworkComponent.AddListener(MessageID.Msg_ThrowCardRs, Function_Syn_ThrowCardRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_XlSettle, Function_Msg_XlSettle);
        //祈福
        HotfixNetworkComponent.AddListener(MessageID.SynPrayRs, Function_Syn_Pray_Rs);
        // 玩家状态
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynPlayerState, Function_Msg_SynPlayerState);
        HotfixNetworkComponent.AddListener(MessageID.Syn_PlayerAuto, Function_Syn_PlayerAuto);
    }

    public void RemoveListener()
    {
        isListenerAdded = false;

        GF.Event.Unsubscribe(GFEventArgs.EventId, OnGFEventChanged);

        HotfixNetworkComponent.RemoveListener(MessageID.Msg_EnterMJDeskRs, Function_EnterMJDeskRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_DisMissDeskRs, Function_DismissDesk);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynSendGift, Function_SynSendGift);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_LeaveDeskRs, Function_LeaveDeskRs);
        HotfixNetworkComponent.RemoveListener(MessageID.SingleGameRecord, Function_SingleGameRecord);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_DeskPlayerInfo, Function_DeskPlayerInfoRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_DeskPlayerInfo1, Function_DeskPlayerInfoRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_DeskPlayerInfo2, Function_DeskPlayerInfoRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynSitUp, Function_SynSitUp);
        HotfixNetworkComponent.RemoveListener(MessageID.PBMJStart, Function_PBMJStart);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MJOption1, Function_Msg_MJOption1);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MJOption2, Function_Msg_MJOption2);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MJOption3, Function_Msg_MJOption3);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MJOption4, Function_Msg_MJOption4);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MJOption5, Function_Msg_MJOption5);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MJOption6, Function_Msg_MJOption6);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MJOption7, Function_Msg_MJOption7);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MJOption8, Function_Msg_MJOption8);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MJOption9, Function_Msg_MJOption9);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_HHCoinChange, Function_Syn_HHCoinChange);
        HotfixNetworkComponent.RemoveListener(MessageID.MSG_ChangeMJDeskState, Function_MSG_ChangeMJDeskState);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynPiaoRs, Function_Msg_SynPiaoRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_ChangeCardRs, Function_Msg_ChangeCardRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynChangeCardRs, Function_Msg_SynChangeCardRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynHandCard, Function_Msg_SynHandCard);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynPlayerHandCard, Function_Msg_SynPlayerHandCard);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_Hu, Function_Msg_Hu);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MJReadyRs, Function_Msg_MJReadyRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynMJReady, Function_Msg_SynMJReady);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynMJDismiss, Function_Msg_SynMJDismiss);
        // 血流成河
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_ThrowCardRs, Function_Syn_ThrowCardRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_XlSettle, Function_Msg_XlSettle);
        //祈福
        HotfixNetworkComponent.RemoveListener(MessageID.SynPrayRs, Function_Syn_Pray_Rs);
        // 玩家状态
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynPlayerState, Function_Msg_SynPlayerState);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_PlayerAuto, Function_Syn_PlayerAuto);
    }

    /// <summary>
    /// 离开桌子
    /// </summary>
    public void Send_LeaveDeskRq()
    {
        // Msg_LeaveDeskRq
        Msg_LeaveDeskRq req = MessagePool.Instance.Fetch<Msg_LeaveDeskRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_LeaveDeskRq, req);
    }

    public void Send_MJPlayerReadyRq(bool ready)
    {
        Msg_MJReadyRq req = MessagePool.Instance.Fetch<Msg_MJReadyRq>();
        req.ReadyState = ready ? 1 : 0;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_MJReadyRq, req);
    }

    public void Send_Msg_ProcessDismissRq(bool isAgree)
    {
        Msg_ProcessDismissRq req = MessagePool.Instance.Fetch<Msg_ProcessDismissRq>();
        req.IsAgree = isAgree;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ProcessDismissRq, req);
    }

    /// <summary>
    /// 发送麻将解散申请
    /// </summary>
    public void Send_Msg_ApplyMJDismissRq()
    {
        Msg_ApplyMJDismissRq req = MessagePool.Instance.Fetch<Msg_ApplyMJDismissRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ApplyMJDismissRq, req);
    }

    /// <summary>
    /// 发送麻将操作请求
    /// </summary>
    /// <param name="option"></param>
    public void Send_MJOptionRq(MJOption option, int card = 0)
    {
        Msg_MJOptionRq req = MessagePool.Instance.Fetch<Msg_MJOptionRq>();
        req.Option = option;
        req.CardVal = card;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_MJOptionRq, req);
    }

    /// <summary>
    /// 退出麻将游戏
    /// </summary>
    public void ExitbaseMahjongGameManager()
    {
        GF.LogInfo("退出麻将游戏流程，跳转麻将大厅流程");
        ChangeState<MJHomeProcedure>(procedure);
    }

    public void Function_EnterMJDeskRs(MessageRecvData data)
    {
        enterMJDeskRs = Msg_EnterMJDeskRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("刷新麻将桌子信息", enterMJDeskRs.ToString());
        GF.StaticUI.ShowMatching(false);

        if (GamePanel_mj != null)
        {
            baseMahjongGameManager.InitMahjongDesk(enterMJDeskRs);
        }

        // 关闭游戏状态同步的等待框
        Util.GetInstance().CloseWaiting("GameStateSync");
    }

    //收到解散房间
    void Function_DismissDesk(MessageRecvData data)
	{
		GF.LogInfo("收到解散牌桌");
		Msg_DisMissDeskRs ack = Msg_DisMissDeskRs.Parser.ParseFrom(data.Data);
		if (ack.State == 1) {
			GF.UI.ShowToast("本小局打完房间将解散");
		}
	}

    public void Function_SynSendGift(MessageRecvData data)//
    {
        //Msg_SynSendGift
        Msg_SynSendGift ack = Msg_SynSendGift.Parser.ParseFrom(data.Data);
        
        if (TryQueueOrExecute(() => baseMahjongGameManager?.Function_SynSendGift(ack.Sender.PlayerId, ack.ToPlayer, ack.Gift.ToString()), "SynSendGift"))
            return;
            
        baseMahjongGameManager?.Function_SynSendGift(ack.Sender.PlayerId, ack.ToPlayer, ack.Gift.ToString());
    }

    public void Function_LeaveDeskRs(MessageRecvData data)
    {
        // Msg_LeaveDeskRs
        Msg_LeaveDeskRs ack = Msg_LeaveDeskRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息离开桌子", ack.ToString());
        if (brecord == true) return;
        if(!IsSpectator() && enterMJDeskRs.CkState != MJState.MjWait) return;
        Util.GetMyselfInfo().DeskId = 0;
        //退出游戏逻辑
        ExitbaseMahjongGameManager();
    }

    public SingleGameRecord singleGameRecord { get; private set; }

    public void Function_SingleGameRecord(MessageRecvData data)
    {
        singleGameRecord = SingleGameRecord.Parser.ParseFrom(data.Data);
        //旁观者不处理
        if (IsSpectator()) return;
        brecord = true;
    }

    public void SetBrecord(bool over)
    {
        if (IsSpectator()) return;
        brecord = over;
    }

    /// <summary>
    /// 推送桌子玩家信息
    /// </summary>
    public void Function_DeskPlayerInfoRs(MessageRecvData data)
    {
        // Syn_DeskPlayerInfo
        Syn_DeskPlayerInfo ack = Syn_DeskPlayerInfo.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息推送桌子玩家", ack.ToString());
        
        // 如果未初始化完成，缓存消息
        if (TryQueueOrExecute(() => baseMahjongGameManager?.Function_DeskPlayerInfoRs(ack.Player), "DeskPlayerInfo"))
            return;
            
        baseMahjongGameManager?.Function_DeskPlayerInfoRs(ack.Player);
    }

    public void Function_SynSitUp(MessageRecvData data)
    {
        //Msg_SynSitUp
        Msg_SynSitUp ack = Msg_SynSitUp.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息站起", ack.ToString());
        if (brecord == true) return;
        
        // 如果未初始化完成，缓存消息
        if (TryQueueOrExecute(() => baseMahjongGameManager?.Function_SynSitUp(ack.Pos), "SynSitUp"))
            return;
            
        baseMahjongGameManager?.Function_SynSitUp(ack.Pos);
    }

    public void Function_PBMJStart(MessageRecvData data)
    {
        PBMJStart ack = PBMJStart.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息开始", ack.ToString());
        enterMJDeskRs.CkState = MJState.MjPlay;
        
        // 如果未初始化完成，缓存消息
        if (TryQueueOrExecute(() => baseMahjongGameManager?.StartGame(ack), "PBMJStart"))
            return;
            
        baseMahjongGameManager?.StartGame(ack);
    }

    public void Function_Msg_MJOption1(MessageRecvData data)
    {
        Msg_MJOption ack = Msg_MJOption.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 摸牌", ack.ToString());
        
        if (TryQueueOrExecute(() => baseMahjongGameManager?.Function_MJOption(ack, MJOption.Draw), "MJOption_Draw"))
            return;
            
        baseMahjongGameManager?.Function_MJOption(ack, MJOption.Draw);
    }

    public void Function_Msg_MJOption2(MessageRecvData data)
    {
        Msg_MJOption ack = Msg_MJOption.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 出牌", ack.ToString());
        
        if (TryQueueOrExecute(() => baseMahjongGameManager?.Function_MJOption(ack, MJOption.Discard), "MJOption_Discard"))
            return;
            
        baseMahjongGameManager?.Function_MJOption(ack, MJOption.Discard);
    }

    public void Function_Msg_MJOption3(MessageRecvData data)
    {
        Msg_MJOption ack = Msg_MJOption.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 吃", ack.ToString());
        
        if (TryQueueOrExecute(() => baseMahjongGameManager?.Function_MJOption(ack, MJOption.Chi), "MJOption_Chi"))
            return;
            
        baseMahjongGameManager?.Function_MJOption(ack, MJOption.Chi);
    }

    public void Function_Msg_MJOption4(MessageRecvData data)
    {
        Msg_MJOption ack = Msg_MJOption.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 碰", ack.ToString());
        
        if (TryQueueOrExecute(() => baseMahjongGameManager?.Function_MJOption(ack, MJOption.Pen), "MJOption_Pen"))
            return;
            
        baseMahjongGameManager?.Function_MJOption(ack, MJOption.Pen);
    }

    public void Function_Msg_MJOption5(MessageRecvData data)
    {
        Msg_MJOption ack = Msg_MJOption.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 杠", ack.ToString());
        
        if (TryQueueOrExecute(() => baseMahjongGameManager?.Function_MJOption(ack, MJOption.Gang), "MJOption_Gang"))
            return;
            
        baseMahjongGameManager?.Function_MJOption(ack, MJOption.Gang);
    }
    public void Function_Msg_MJOption6(MessageRecvData data)
    {
        Msg_MJOption ack = Msg_MJOption.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 听 (卡五星需要亮牌操作)", ack.ToString());
        
        if (TryQueueOrExecute(() => baseMahjongGameManager?.Function_MJOption(ack, MJOption.Ting), "MJOption_Ting"))
            return;
            
        baseMahjongGameManager?.Function_MJOption(ack,MJOption.Ting);
    }

    public void Function_Msg_MJOption7(MessageRecvData data)
    {
        Msg_MJOption ack = Msg_MJOption.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 过", ack.ToString());
        
        if (TryQueueOrExecute(() => baseMahjongGameManager?.Function_MJOption(ack, MJOption.Guo), "MJOption_Guo"))
            return;
            
        baseMahjongGameManager?.Function_MJOption(ack, MJOption.Guo);
    }

    public void Function_Msg_MJOption8(MessageRecvData data)
    {
        Msg_MJOption ack = Msg_MJOption.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 有操作", ack.ToString());
        
        if (TryQueueOrExecute(() => baseMahjongGameManager?.Function_MJOption8(ack), "MJOption8"))
            return;
            
        baseMahjongGameManager?.Function_MJOption8(ack);
    }

    public void Function_Msg_MJOption9(MessageRecvData data)
    {
        Msg_MJOption ack = Msg_MJOption.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 血流胡牌", ack.ToString());
        
        // 血流连续胡牌操作
        if (TryQueueOrExecute(() => GamePanel_mj?.HandleXueLiuHuOption(ack), "MJOption9_XueLiuHu"))
            return;
            
        GamePanel_mj?.HandleXueLiuHuOption(ack);
    }

    /// <summary>
    /// 收到甩牌响应消息（血流成河）
    /// </summary>
    public void Function_Syn_ThrowCardRs(MessageRecvData data)
    {
        Syn_ThrowCardRs ack = Syn_ThrowCardRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 甩牌响应", ack.ToString());
        
        if (TryQueueOrExecute(() => GamePanel_mj?.HandleSynThrowCardRs(ack), "ThrowCardRs"))
            return;
            
        GamePanel_mj?.HandleSynThrowCardRs(ack);
    }

    /// <summary>
    /// 收到血流结算消息
    /// </summary>
    public void Function_Msg_XlSettle(MessageRecvData data)
    {
        Msg_XlSettle ack = Msg_XlSettle.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 血流结算", ack.ToString());
        enterMJDeskRs.CkState = MJState.MjSettle;
        
        if (TryQueueOrExecute(() => {
            var players = baseMahjongGameManager?.GetAliveDeskPlayers();
            GamePanel_mj?.ShowXueLiuSettlePanel(ack, players);
        }, "XlSettle"))
            return;
            
        var players = baseMahjongGameManager?.GetAliveDeskPlayers();
        GamePanel_mj?.ShowXueLiuSettlePanel(ack, players);
    }

    public void Function_Msg_Hu(MessageRecvData data)
    {
        Msg_Hu ack = Msg_Hu.Parser.ParseFrom(data.Data);
        GF.LogInfo(ack.LiuJu ? "收到消息 流局(胡协议)" : "收到消息 胡", ack.ToString());
        enterMJDeskRs.CkState = MJState.MjSettle;
        
        if (TryQueueOrExecute(() => baseMahjongGameManager?.Function_Msg_Hu(ack), "Msg_Hu"))
            return;
            
        baseMahjongGameManager?.Function_Msg_Hu(ack);
    }

    public void Function_Syn_HHCoinChange(MessageRecvData data)
    {
        Syn_HHCoinChange ack = Syn_HHCoinChange.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 积分变化", ack.ToString());
        
        if (TryQueueOrExecute(() => baseMahjongGameManager?.Function_Syn_HHCoinChange(ack), "HHCoinChange"))
            return;
            
        baseMahjongGameManager?.Function_Syn_HHCoinChange(ack);
    }

    public void Function_Msg_MJReadyRs(MessageRecvData data)
    {
        Msg_MJReadyRs ack = Msg_MJReadyRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 麻将准备返回", ack.ToString());
        
        if (TryQueueOrExecute(() => baseMahjongGameManager?.Function_Msg_MJReadyRs(ack), "MJReadyRs"))
            return;
            
        baseMahjongGameManager?.Function_Msg_MJReadyRs(ack);
    }

    public void Function_Msg_SynMJReady(MessageRecvData data)
    {
        Msg_SynMJReady ack = Msg_SynMJReady.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 麻将准备通知", ack.ToString());
        
        if (TryQueueOrExecute(() => baseMahjongGameManager?.Function_Msg_SynMJReady(ack), "SynMJReady"))
            return;
            
        baseMahjongGameManager?.Function_Msg_SynMJReady(ack);
    }

    public void Function_Msg_SynMJDismiss(MessageRecvData data)
    {
        Msg_SynMJDismiss ack = Msg_SynMJDismiss.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 麻将解散通知", ack.ToString());
        
        if (TryQueueOrExecute(() => baseMahjongGameManager?.Function_Msg_SynMJDismiss(ack), "SynMJDismiss"))
            return;
            
        baseMahjongGameManager?.Function_Msg_SynMJDismiss(ack);
    }

    /// <summary>
    /// 收到桌子状态改变消息
    /// </summary>
    public void Function_MSG_ChangeMJDeskState(MessageRecvData data)
    {
        MSG_ChangeMJDeskState ack = MSG_ChangeMJDeskState.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 桌子状态改变", ack.ToString());
        enterMJDeskRs.CkState = ack.State;
        
        //忽略甩牌状态改变消息
        if (ack.State != MJState.ThrowCard)
        {
            if (TryQueueOrExecute(() => GamePanel_mj?.HandleDeskStateChange(ack.State), "ChangeMJDeskState"))
                return;
                
            GamePanel_mj?.HandleDeskStateChange(ack.State);
        }
    }

    /// <summary>
    /// 收到同步飘分消息
    /// </summary>
    public void Function_Msg_SynPiaoRs(MessageRecvData data)
    {
        Msg_SynPiaoRs ack = Msg_SynPiaoRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 同步飘分", ack.ToString());
        
        if (TryQueueOrExecute(() => GamePanel_mj?.HandleSynPiao(ack), "SynPiaoRs"))
            return;
            
        GamePanel_mj?.HandleSynPiao(ack);
    }

    /// <summary>
    /// 收到换牌响应消息
    /// </summary>
    public void Function_Msg_ChangeCardRs(MessageRecvData data)
    {
        Msg_ChangeCardRs ack = Msg_ChangeCardRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 换牌响应", ack.ToString());
        
        if (TryQueueOrExecute(() => GamePanel_mj?.HandleChangeCardRs(ack), "ChangeCardRs"))
            return;
            
        GamePanel_mj?.HandleChangeCardRs(ack);
    }

    /// <summary>
    /// 收到同步手牌消息（换牌结束）
    /// </summary>
    public void Function_Msg_SynHandCard(MessageRecvData data)
    {
        Msg_SynHandCard ack = Msg_SynHandCard.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 同步手牌（换牌结束）", ack.ToString());
        
        if (TryQueueOrExecute(() => GamePanel_mj?.HandleSynHandCard(ack), "SynHandCard"))
            return;
            
        GamePanel_mj?.HandleSynHandCard(ack);
    }

    /// <summary>
    /// 收到玩家亮牌消息（卡五星亮牌）
    /// </summary>
    public void Function_Msg_SynPlayerHandCard(MessageRecvData data)
    {
        Msg_SynPlayerHandCard ack = Msg_SynPlayerHandCard.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 玩家亮牌", ack.ToString());
        
        if (TryQueueOrExecute(() => GamePanel_mj?.HandleSynPlayerHandCard(ack), "SynPlayerHandCard"))
            return;
            
        GamePanel_mj?.HandleSynPlayerHandCard(ack);
    }

    public void Function_Msg_SynChangeCardRs(MessageRecvData data)
    {
        Msg_SynChangeCardRs ack = Msg_SynChangeCardRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 其他玩家换牌完成", ack.ToString());
        
        if (TryQueueOrExecute(() => GamePanel_mj?.HandleSynChangeCard(ack), "SynChangeCardRs"))
            return;
            
        GamePanel_mj?.HandleSynChangeCard(ack);
    }

    /// <summary>
    /// 发送飘分请求
    /// </summary>
    public void Send_PiaoRq(int piaoValue)
    {
        Msg_PiaoRq req = MessagePool.Instance.Fetch<Msg_PiaoRq>();
        req.Piao = piaoValue;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_PiaoRq, req);
        GF.LogInfo($"发送飘分请求: {piaoValue}");
    }

    /// <summary>
    /// 发送换牌请求
    /// </summary>
    public void Send_ChangeCardRq(List<int> cards)
    {
        Msg_ChangeCardRq req = MessagePool.Instance.Fetch<Msg_ChangeCardRq>();
        req.Card.AddRange(cards);
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ChangeCardRq, req);
        GF.LogInfo($"发送换牌请求: {string.Join(",", cards)}");
    }
    
    /// <summary>
    /// 发送亮牌请求（卡五星专用）
    /// </summary>
    public void Send_LiangDaoRq(Msg_LiangDaoRq req)
    {
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_LiangDaoRq, req);
        GF.LogInfo($"发送亮牌请求: Discard={req.Discard}, AnPuCards={string.Join(",", req.Card)}");
    }

    /// <summary>
    /// 发送甩牌请求（血流成河专用）
    /// </summary>
    public void Send_ThrowCardRq(Msg_ThrowCardRq req)
    {
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ThrowCardRq, req);
        GF.LogInfo($"发送甩牌请求: Cards={string.Join(",", req.Card)}");
    }

    public void Function_Syn_Pray_Rs(MessageRecvData data)
    {
        SynPrayRs ack = SynPrayRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 祈福通知", ack.ToString());
        if(ack.PlayerId == Util.GetMyselfInfo().PlayerId) return;
        
        if (TryQueueOrExecute(() => GamePanel_mj?.ShowBlessingUI_other(ack), "Syn_Pray_Rs"))
            return;
            
        GamePanel_mj?.ShowBlessingUI_other(ack);
    }

    /// <summary>
    /// 收到玩家状态变更消息（离线/托管）
    /// </summary>
    public void Function_Msg_SynPlayerState(MessageRecvData data)
    {
        Msg_SynPlayerState ack = Msg_SynPlayerState.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 玩家状态变更", ack.ToString());
        
        if (TryQueueOrExecute(() => baseMahjongGameManager?.UpdatePlayerState(ack.PlayerId, ack.State), "SynPlayerState"))
            return;
            
        baseMahjongGameManager?.UpdatePlayerState(ack.PlayerId, ack.State);
    }

    /// <summary>
    /// 收到玩家托管状态通知（通知其他玩家）
    /// </summary>
    public void Function_Syn_PlayerAuto(MessageRecvData data)
    {
        Syn_PlayerAuto ack = Syn_PlayerAuto.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息 玩家托管状态通知", ack.ToString());
        
        if (TryQueueOrExecute(() => baseMahjongGameManager?.UpdatePlayerAutoState(ack.PlayerId, ack.Auto == 1), "Syn_PlayerAuto"))
            return;
            
        baseMahjongGameManager?.UpdatePlayerAutoState(ack.PlayerId, ack.Auto == 1);
    }

    private void OnGFEventChanged(object sender, GameEventArgs e)
    {
        baseMahjongGameManager?.OnGFEventChanged(sender, e);
    }

}

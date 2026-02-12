using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using NetMsg;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class NNProcedure : ProcedureBase
{
    public NNGamePanel nnGamePanel { get; private set; }
    private IFsm<IProcedureManager> procedure;
    public Msg_EnterNiuNiuDeskRs enterNiuNiuDeskRs;
    public int deskID;
    
    public Msg_SynCKJackPot msg_SynCKJackPot;

    //public NiuNiuState niuNiuState; //WAIT = 0;//未开始 等待状态 只有一个人 满足开始游戏的条件的时候
    //                                //ROBBANKER = 1;//抢庄阶段
    //                                //BET_COIN = 2; //下注阶段
    //                                //SHOW_CARD = 3; //翻牌阶段
    //                                //SETTLE = 4;//结算阶段
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        this.procedure = procedureOwner;
        CoroutineRunner.Instance.RunCoroutine(GFBuiltin.Instance.SwitchToPortrait());

        var data1 = procedure.GetData<VarByteArray>("enterNiuNiuDeskRs");
        enterNiuNiuDeskRs = Msg_EnterNiuNiuDeskRs.Parser.ParseFrom(data1);
        GF.LogInfo("牛牛桌子信息", enterNiuNiuDeskRs.ToString());
        deskID = enterNiuNiuDeskRs.DeskId;
		Util.GetMyselfInfo().DeskId = deskID;
        ShowNNGamePanel();
    }

	public void AddListener(){
		GF.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
        GF.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
        //网络监听事件
        HotfixNetworkComponent.AddListener(MessageID.Msg_LeaveDeskRs, Function_LeaveDeskRs);
        HotfixNetworkComponent.AddListener(MessageID.Syn_DeskPlayerInfo, Function_DeskPlayerInfoRs);
        HotfixNetworkComponent.AddListener(MessageID.Syn_DeskPlayerInfo1, Function_DeskPlayerInfoRs1);
        HotfixNetworkComponent.AddListener(MessageID.Syn_DeskPlayerInfo2, Function_DeskPlayerInfoRs2);
        HotfixNetworkComponent.AddListener(MessageID.Msg_BringRs, Function_BringRs);
        HotfixNetworkComponent.AddListener(MessageID.Syn_DeskUpdate, Function_DeskUpdate);
        HotfixNetworkComponent.AddListener(MessageID.Syn_ShowCard, Function_ShowCard);
        HotfixNetworkComponent.AddListener(MessageID.MsgDealCard, Function_DealCard);
        HotfixNetworkComponent.AddListener(MessageID.Syn_RobBanker, Function_RobBanker);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynBanker, Function_SynBanker);
        HotfixNetworkComponent.AddListener(MessageID.Syn_BetCoin, Function_BetCoin);
        HotfixNetworkComponent.AddListener(MessageID.Syn_Settle, Function_Settle);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynSitUp, Function_SynSitUp);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SitUpRs, Function_SitUpRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_DisMissDeskRs, Function_DisMissDeskRs);

        HotfixNetworkComponent.AddListener(MessageID.Msg_ForbidChatRs, Function_ForbidChatRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynSendGift, Function_SynSendGift);
     
        HotfixNetworkComponent.AddListener(MessageID.Msg_CancelLeaveRs, Function_CancelLeaveRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynPlayerState, Function_SynPlayerState);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynDeskStart, Function_SynDeskStart);
        
        HotfixNetworkComponent.AddListener(MessageID.Msg_PauseDeskRs, Function_PauseDeskRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynDeskState, Function_SynDeskState);
        HotfixNetworkComponent.AddListener(MessageID.Msg_DeskContinuedTimeRs, Function_DeskContinuedTimeRs);
		HotfixNetworkComponent.AddListener(MessageID.Msg_AdvanceSettleRs, Function_AdvanceSettle);
		HotfixNetworkComponent.AddListener(MessageID.Syn_AutoShowCard, Function_AutoShowCard);
        
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynCKJackPot, Function_SynCKJackPot);
	}

    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

    }

    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        GlobalManager.GetInstance().ReSetUI();
		Util.GetMyselfInfo().DeskId = 0;
        GF.Event.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
        GF.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);

        //网络监听事件
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_LeaveDeskRs, Function_LeaveDeskRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_DeskPlayerInfo, Function_DeskPlayerInfoRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_DeskPlayerInfo1, Function_DeskPlayerInfoRs1);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_DeskPlayerInfo2, Function_DeskPlayerInfoRs2);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_BringRs, Function_BringRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_DeskUpdate, Function_DeskUpdate);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_ShowCard, Function_ShowCard);
        HotfixNetworkComponent.RemoveListener(MessageID.MsgDealCard, Function_DealCard);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_RobBanker, Function_RobBanker);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynBanker, Function_SynBanker);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_BetCoin, Function_BetCoin);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_Settle, Function_Settle);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynSitUp, Function_SynSitUp);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SitUpRs, Function_SitUpRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_DisMissDeskRs, Function_DisMissDeskRs);
        
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_ForbidChatRs, Function_ForbidChatRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynSendGift, Function_SynSendGift);
        
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_CancelLeaveRs, Function_CancelLeaveRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynPlayerState, Function_SynPlayerState);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynDeskStart, Function_SynDeskStart);
        
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_PauseDeskRs, Function_PauseDeskRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynDeskState, Function_SynDeskState);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_DeskContinuedTimeRs, Function_DeskContinuedTimeRs);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_AdvanceSettleRs, Function_AdvanceSettle);
		HotfixNetworkComponent.RemoveListener(MessageID.Syn_AutoShowCard, Function_AutoShowCard);
 
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynCKJackPot, Function_SynCKJackPot);

        // NNActor.GetInstance().DestroyNNActorFsm();
      
        base.OnLeave(procedureOwner, isShutdown);
    }

	private void OnOpenUIFormSuccess(object sender, GameEventArgs e)
	{
		var args = e as OpenUIFormSuccessEventArgs;
		if (args.UIForm.Logic.GetType() == typeof(NNGamePanel)) {
			nnGamePanel = args.UIForm.Logic as NNGamePanel;

        //     //初始化牛牛游戏状态机
        //     NNActor.GetInstance().Init();
        //     NNActor.GetInstance().HandChangeState(enterNiuNiuDeskRs.State);

        //     //重连或者中途进入

        //     // 要不要显示开始
        //     if (IsRoomOwner() && enterNiuNiuDeskRs.DeskState == DeskState.WaitStart)
        //         nnGamePanel.btnGameBegain.SetActive(true);
        //     else
        //         nnGamePanel.btnGameBegain.SetActive(false);

        //     //初始化座位
        //     nnGamePanel.seatManager.Init(enterNiuNiuDeskRs.BaseConfig.PlayerNum);

			// for (int i = 0; i < enterNiuNiuDeskRs.DeskPlayers.Count; i++)
			// {
			// 	nnGamePanel.NewPlayerInSeat(enterNiuNiuDeskRs.DeskPlayers[i]);
			// }
		}
	}

    private void OnShowEntitySuccess(object sender, GameEventArgs e)
    {
        var args = e as ShowEntitySuccessEventArgs;

    }

    public async void ShowNNGamePanel()
    {
		var gamePanel = await GF.UI.OpenUIFormAwait(UIViews.NNGamePanel);
        if (gamePanel != null)
        {
            nnGamePanel = gamePanel as NNGamePanel;
            AddListener();
			GlobalManager.SendSystemConfigRq();
        }
    }
 
    /// <summary>
    /// 判断是否是房主
    /// </summary>
    public bool IsRoomOwner()
    {
        if (Util.IsMySelf(enterNiuNiuDeskRs.Creator.PlayerId))
            return true;
        return false;
    }

    public DeskPlayer GetSelfPlayer()
    {
        for (var i = 0;i < enterNiuNiuDeskRs.DeskPlayers.Count;i++)
        {
            if (Util.IsMySelf(enterNiuNiuDeskRs.DeskPlayers[i].BasePlayer.PlayerId))
                return enterNiuNiuDeskRs.DeskPlayers[i];
        }
        return null;
    }

    public DeskPlayer GetPosPlayer(int pos)
    {
        for (var i = 0;i < enterNiuNiuDeskRs.DeskPlayers.Count;i++)
        {
            if ((int)enterNiuNiuDeskRs.DeskPlayers[i].Pos == pos) return enterNiuNiuDeskRs.DeskPlayers[i];
        }
        return null;
    }

    public DeskPlayer GetPlayerByID(long playerid)
    {
        for (var i = 0;i < enterNiuNiuDeskRs.DeskPlayers.Count;i++)
        {
            if ((int)enterNiuNiuDeskRs.DeskPlayers[i].BasePlayer.PlayerId == playerid) return enterNiuNiuDeskRs.DeskPlayers[i];
        }
        return null;
    }

    #region 网络发送接收

	public void Send_SitDownRq(Position pos)
	{
		if(!enterNiuNiuDeskRs.BaseConfig.GpsLimit){
			Msg_SitDownRq req = MessagePool.Instance.Fetch<Msg_SitDownRq>();
			req.Pos = pos;
			req.Gps = "0,0";
			HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_SitDownRq, req);
			return;
		}
        CoroutineRunner.Instance.RunCoroutine(GetGPSAndSendSitDownRequest(pos));
	}

    private IEnumerator GetGPSAndSendSitDownRequest(Position pos)
    {
        // 使用通用GPS获取方法
        return Util.GetGPSLocation((gpsString) => {
            // 获取位置信息成功，发送请求
            Msg_SitDownRq req = MessagePool.Instance.Fetch<Msg_SitDownRq>();
            req.Pos = pos;
            req.Gps = gpsString;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_SitDownRq, req);
        });
    }

	public void Send_SitUpRq()
	{
		Msg_SitUpRq req = MessagePool.Instance.Fetch<Msg_SitUpRq>();
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_SitUpRq, req);
	}

	public void Function_DeskPlayerInfoRs(MessageRecvData data)
	{
		Syn_DeskPlayerInfo ack = Syn_DeskPlayerInfo.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息推送桌子玩家", ack.ToString());
		nnGamePanel.NewPlayerInSeat(ack.Player, true);
	}

	public void Function_DeskPlayerInfoRs1(MessageRecvData data)
	{
		Syn_DeskPlayerInfo ack = Syn_DeskPlayerInfo.Parser.ParseFrom(data.Data);
		GF.LogInfo("玩家买入", ack.ToString());
		if (enterNiuNiuDeskRs.DeskPlayers.Any(basePlayer => basePlayer.BasePlayer.PlayerId == ack.Player.BasePlayer.PlayerId))
		{
			nnGamePanel.Syn_PlayerInfo(ack.Player);
		}
		else
		{
			nnGamePanel.NewPlayerInSeat(ack.Player, false);
		}
		// nnGamePanel.Syn_BringIn(ack);
		if (Util.IsMySelf(ack.Player.BasePlayer.PlayerId)) {
			if (enterNiuNiuDeskRs.DeskState == DeskState.WaitStart || enterNiuNiuDeskRs.State == NiuNiuState.Wait) {
				GF.UI.ShowToast("带入成功");
			}
			else {
				if (ack.Player.BringNum > 1) {
					GF.UI.ShowToast("带入下局生效");
				}
				else {
					GF.UI.ShowToast("带入成功");
				}
			}
		}
	}

	public void Function_DeskPlayerInfoRs2(MessageRecvData data)
	{
		Syn_DeskPlayerInfo ack = Syn_DeskPlayerInfo.Parser.ParseFrom(data.Data);
		GF.LogInfo("玩家筹码变更", ack.ToString());
		nnGamePanel.Syn_PlayerInfo(ack.Player);
	}

	public void Function_SynSitUp(MessageRecvData data)
	{
		Msg_SynSitUp ack = Msg_SynSitUp.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息站起", ack.ToString());
		nnGamePanel.LeaveSeat((int)ack.Pos);
	}

	public void Send_BringRq(int amount, long leagueId)
	{
		Msg_BringRq req = MessagePool.Instance.Fetch<Msg_BringRq>();
		req.DeskId = deskID;
		req.Amount = amount;
		req.ClubId = leagueId;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_BringRq, req);
	}

	public void Function_BringRs(MessageRecvData data)
	{
		Msg_BringRs ack = Msg_BringRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息请求带入", ack.ToString());
	}

	public void Send_LeaveDeskRq()
	{
		Msg_LeaveDeskRq req = MessagePool.Instance.Fetch<Msg_LeaveDeskRq>();
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_LeaveDeskRq, req);
	}

	public void Function_LeaveDeskRs(MessageRecvData data)
	{
		Msg_LeaveDeskRs ack = Msg_LeaveDeskRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息离开桌子", ack.ToString());
		if (nnGamePanel == null) return;
		if (nnGamePanel.brecord == true) return;
		Util.GetMyselfInfo().DeskId = 0;
		EnterHome();
	}

	public void EnterHome()
    {
        GF.LogInfo("退出牛牛流程，跳转大厅流程");
        ChangeState<HomeProcedure>(procedure);
    }

	public void Send_Start()
	{
		Msg_Start req = MessagePool.Instance.Fetch<Msg_Start>();
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_Start, req);
	}

	public void Function_DealCard(MessageRecvData data)
	{
		Msg_DealCard ack = Msg_DealCard.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息发牌", ack.ToString());
		if (ack.Deal.Count > 0 && ack.Deal[0].HandCards.Count == 4) {
			nnGamePanel.coroutinemanager.AddCoroutine(nnGamePanel.Syn_DealCard(ack.Deal.ToList()));
		}
	}

	public void Send_RobBanker(int num)
	{
		Msg_RobBanker req = MessagePool.Instance.Fetch<Msg_RobBanker>();
		req.RobBanker = num;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_RobBanker, req);
	}

	public void Function_RobBanker(MessageRecvData data)
	{
		Syn_RobBanker ack = Syn_RobBanker.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息抢庄", ack.ToString());
		nnGamePanel.Syn_RobBanker(ack);
	}

	public Coroutine BankerCoroutine;

	public void Function_SynBanker(MessageRecvData data)
	{
		Msg_SynBanker ack = Msg_SynBanker.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息推送庄家", ack.ToString());
		foreach (DeskPlayer deskplayer in this.enterNiuNiuDeskRs.DeskPlayers) {
			if (deskplayer.Pos == ack.Pos) {
				deskplayer.IsBanker = true;
			}
			else {
				deskplayer.IsBanker = false;
			}
		}

		BankerCoroutine = CoroutineRunner.Instance.RunCoroutine(nnGamePanel.Syn_Banker(ack));
	}

	public void Function_DeskUpdate(MessageRecvData data)
	{
		Syn_DeskUpdate ack = Syn_DeskUpdate.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息通知游戏状态改变", ack.ToString());
		nnGamePanel.Syn_DeskUpdate(ack);
	}

	public void Send_BetCoin(float num)
	{
		Msg_BetCoin req = MessagePool.Instance.Fetch<Msg_BetCoin>();
		req.BetCoin = num;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_BetCoin, req);
	}

	public void Function_BetCoin(MessageRecvData data)
	{
		Syn_BetCoin ack = Syn_BetCoin.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息下注", ack.ToString());
		nnGamePanel.Syn_BetCoin(ack);
	}

	public void Send_ShowCard()
	{
		Msg_ShowCard req = MessagePool.Instance.Fetch<Msg_ShowCard>();
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_ShowCard, req);
	}

	public void Function_ShowCard(MessageRecvData data)
	{
		Syn_ShowCard ack = Syn_ShowCard.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息翻牌", ack.ToString());
		nnGamePanel.Syn_ShowCard(ack);
	}

	public void Function_Settle(MessageRecvData data)
	{
		Syn_Settle ack = Syn_Settle.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息结算", ack.ToString());
		nnGamePanel.coroutinemanager.AddCoroutine(nnGamePanel.Syn_Settle(ack));
	}

	public void Send_DisMissDeskRq()
	{
		Msg_DisMissDeskRq req = MessagePool.Instance.Fetch<Msg_DisMissDeskRq>();
		req.DeskId = deskID;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_DisMissDeskRq, req);
	}

	public void Function_DisMissDeskRs(MessageRecvData data)
	{
		Msg_DisMissDeskRs ack = Msg_DisMissDeskRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息解散牌桌", ack.ToString());
		if (ack.State == 1) {
			GF.UI.ShowToast("房间将解散");
		}
	}

	public void Function_SitUpRs(MessageRecvData data)
	{
		Msg_SitUpRs ack = Msg_SitUpRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("下局结束站起", ack.ToString());
		GF.UI.ShowToast("牌局结束站起");
	}

	public void Send_CancelLeaveRq()
	{
		Msg_CancelLeaveRq req = MessagePool.Instance.Fetch<Msg_CancelLeaveRq>();
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_CancelLeaveRq, req);
	}

	public void Function_CancelLeaveRs(MessageRecvData data)
	{
		Msg_CancelLeaveRs ack = Msg_CancelLeaveRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("取消留座离桌", ack.ToString());
		// nnGamePanel.Function_CancelLeaveRs();
	}

	public void Function_SynPlayerState(MessageRecvData data)//
	{
		Msg_SynPlayerState ack = Msg_SynPlayerState.Parser.ParseFrom(data.Data);
		GF.LogInfo("玩家在线状态变更", ack.ToString());
		nnGamePanel.Syn_PlayerState(ack);
	}

	public void Function_SynDeskStart(MessageRecvData data)//
	{
		Msg_SynDeskStart ack = Msg_SynDeskStart.Parser.ParseFrom(data.Data);
		GF.LogInfo("房间开始玩家", ack.ToString());
		nnGamePanel.Syn_DeskStart(ack);
	}

	public void Send_SendGiftRq(long id,int gift)
	{
		Msg_SendGiftRq req = MessagePool.Instance.Fetch<Msg_SendGiftRq>();
		req.ToPlayer = id;
		req.Gift = gift;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_SendGiftRq, req);
	}

	public void Function_SynSendGift(MessageRecvData data)//
	{
		Msg_SynSendGift ack = Msg_SynSendGift.Parser.ParseFrom(data.Data);
		GF.LogInfo("礼物", ack.ToString());
		nnGamePanel.Syn_SendGift(ack.Sender.PlayerId, ack.ToPlayer, ack.Gift.ToString());
	}

	public void Send_PauseDeskRq(int state)
	{
		Msg_PauseDeskRq req = MessagePool.Instance.Fetch<Msg_PauseDeskRq>();
		req.State = state;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_PauseDeskRq, req);
	}

	public void Function_PauseDeskRs(MessageRecvData data)
	{
		Msg_PauseDeskRs ack = Msg_PauseDeskRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("房间暂停", ack.ToString());
	}

	public void Function_SynDeskState(MessageRecvData data)
	{
		Msg_SynDeskState ack = Msg_SynDeskState.Parser.ParseFrom(data.Data);
		GF.LogInfo("房间暂停", ack.ToString());
		enterNiuNiuDeskRs.DeskState = ack.State;
		if (ack.State == DeskState.Pause)
		{
			nnGamePanel.goPanelPause.SetActive(true);
			if (IsRoomOwner()) {
				nnGamePanel.goPause1.SetActive(true);
				nnGamePanel.goPause2.SetActive(false);
			}
			else {
				nnGamePanel.goPause1.SetActive(false);
				nnGamePanel.goPause2.SetActive(true);
			}
		}
		else
		{
			nnGamePanel.goPanelPause.SetActive(false);
		}
	}

	public void Send_DeskContinuedTimeRq(int continuedTime)
	{
		Msg_DeskContinuedTimeRq req = MessagePool.Instance.Fetch<Msg_DeskContinuedTimeRq>();
		req.ContinuedTime = continuedTime;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_DeskContinuedTimeRq, req);
	}

	public void Function_DeskContinuedTimeRs(MessageRecvData data)
	{
		Msg_DeskContinuedTimeRs ack = Msg_DeskContinuedTimeRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("房间续时", ack.ToString());
		GF.UI.ShowToast("房间续时成功");
		nnGamePanel.remaintime += ack.ContinuedTime * 60 * 1000;
	}

	public void Function_ForbidChatRs(MessageRecvData data)
	{
		Msg_ForbidChatRs ack = Msg_ForbidChatRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("禁言", ack.ToString());
		DeskPlayer deskplayer = GetPlayerByID(ack.PlayerId);
		deskplayer.Forbid = ack.Type == 1;
	}

	public void Send_AdvanceSettle() {
		Msg_AdvanceSettleRq req = MessagePool.Instance.Fetch<Msg_AdvanceSettleRq>();
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_AdvanceSettleRq, req);
	}

	public void Function_AdvanceSettle(MessageRecvData data) {
		Msg_AdvanceSettleRs ack = Msg_AdvanceSettleRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("玩家提前结算", ack.ToString());
		GF.UI.ShowToast("结算成功");
	}

	public void Function_AutoShowCard(MessageRecvData data) {
		Syn_AutoShowCard ack = Syn_AutoShowCard.Parser.ParseFrom(data.Data);
		GF.LogInfo("自动摊牌", ack.ToString());
		nnGamePanel.Syn_AutoShowCard(ack);
	}

	public void Function_SynCKJackPot(MessageRecvData data)
	{
		Msg_SynCKJackPot ack = Msg_SynCKJackPot.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息奖池得奖通知", ack.ToString());
		msg_SynCKJackPot = ack;
	}

	#endregion
}

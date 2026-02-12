using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using NetMsg;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class DZProcedure : ProcedureBase
{
	public DZGamePanel GamePanel { get; private set; }
	public int GameUIFormId;
	private IFsm<IProcedureManager> procedure;
	public Msg_EnterTexasDeskRs deskinfo;
	public int deskID;

	public Msg_SynCKJackPot msg_SynCKJackPot;

	protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
	{
		base.OnEnter(procedureOwner);
		this.procedure = procedureOwner;
        CoroutineRunner.Instance.RunCoroutine(GFBuiltin.Instance.SwitchToPortrait());
		deskinfo = Msg_EnterTexasDeskRs.Parser.ParseFrom(procedure.GetData<VarByteArray>("enterDZDeskRs"));
        GF.LogInfo("德州桌子信息" , deskinfo.ToString());
        deskID = deskinfo.DeskId;
		Util.GetMyselfInfo().DeskId = deskID;
		ShowDZGamePanel();
	}

	public void AddListener()
	{
		GF.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
		HotfixNetworkComponent.AddListener(MessageID.Msg_LeaveDeskRs, Function_LeaveDesk);
		HotfixNetworkComponent.AddListener(MessageID.Syn_DeskPlayerInfo, Function_DeskPlayerInfoRs);
		HotfixNetworkComponent.AddListener(MessageID.Syn_DeskPlayerInfo1, Function_DeskPlayerInfoRs1);
		HotfixNetworkComponent.AddListener(MessageID.Syn_DeskPlayerInfo2, Function_DeskPlayerInfoRs2);
		HotfixNetworkComponent.AddListener(MessageID.Msg_SynSitUp, Function_SynSitUp);
		HotfixNetworkComponent.AddListener(MessageID.Msg_SynDeskStart, Function_SynDeskStart);
		HotfixNetworkComponent.AddListener(MessageID.Msg_SynDeskState, Function_DeskState);
		HotfixNetworkComponent.AddListener(MessageID.Msg_DisMissDeskRs, Function_DismissDesk);
		HotfixNetworkComponent.AddListener(MessageID.Syn_TexasStart, Function_TexasStart);
		HotfixNetworkComponent.AddListener(MessageID.MsgDealCard, Function_DealerCard);
		HotfixNetworkComponent.AddListener(MessageID.Syn_TexasOptionInfo, Function_TexasOptionInfo);
		HotfixNetworkComponent.AddListener(MessageID.Syn_TexasChangeOption, Function_TexasChangeOption);
		HotfixNetworkComponent.AddListener(MessageID.Syn_DealCommonCard, Function_DealCommonCard);
		HotfixNetworkComponent.AddListener(MessageID.Syn_BetCoin, Function_BetCoin);
		HotfixNetworkComponent.AddListener(MessageID.Msg_SynTexasSettle, Function_TexasSettle);
		HotfixNetworkComponent.AddListener(MessageID.Msg_EdgePot, Function_EdgePot);
		HotfixNetworkComponent.AddListener(MessageID.Msg_DeskContinuedTimeRs, Function_DeskContinuedTime);
		HotfixNetworkComponent.AddListener(MessageID.Msg_AdvanceSettleRs, Function_AdvanceSettle);
		HotfixNetworkComponent.AddListener(MessageID.Msg_ForbidChatRs, Function_ForbidChat);
		HotfixNetworkComponent.AddListener(MessageID.Msg_SynSendGift, Function_SynSendGift);
		HotfixNetworkComponent.AddListener(MessageID.Msg_SitUpRs, Function_SitUp);
		HotfixNetworkComponent.AddListener(MessageID.Msg_SynPlayerState, Function_PlayerState);
		HotfixNetworkComponent.AddListener(MessageID.Msg_SynTexasPokerHandCards, Function_HandCards);
		HotfixNetworkComponent.AddListener(MessageID.Msg_ProtectInfo, Function_InsuranceInfo);
		HotfixNetworkComponent.AddListener(MessageID.Msg_ProtectInfo_end, Function_InsuranceInfo_end);
		HotfixNetworkComponent.AddListener(MessageID.Msg_SynProtectCard, Function_BuyInsurance);
		HotfixNetworkComponent.AddListener(MessageID.Msg_SynTexasState, Function_TexasState);
		HotfixNetworkComponent.AddListener(MessageID.Msg_LookCommonCardRs, Function_LookCommonCard);
		HotfixNetworkComponent.AddListener(MessageID.Msg_BackPot, Function_BackPot);
		HotfixNetworkComponent.AddListener(MessageID.Msg_SynCKJackPot, Function_SynCKJackPot);
	}

	public async void ShowDZGamePanel()
    {
        // 异步打开游戏界面，自动显示等待框
        var uiParams = UIParams.Create();
        GF.LogInfo("打开德州主游戏界面");
        var gamePanel = await GF.UI.OpenUIFormAwait(UIViews.DZGamePanel, uiParams);
        if (gamePanel != null)
        {
            GamePanel = gamePanel as DZGamePanel;
            AddListener();
        }
    }

	protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
	{
		base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
	}

	protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
	{
        GlobalManager.GetInstance().ReSetUI();
		Util.GetMyselfInfo().DeskId = 0;
		GF.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_LeaveDeskRs, Function_LeaveDesk);
		HotfixNetworkComponent.RemoveListener(MessageID.Syn_DeskPlayerInfo, Function_DeskPlayerInfoRs);
		HotfixNetworkComponent.RemoveListener(MessageID.Syn_DeskPlayerInfo1, Function_DeskPlayerInfoRs1);
		HotfixNetworkComponent.RemoveListener(MessageID.Syn_DeskPlayerInfo2, Function_DeskPlayerInfoRs2);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynSitUp, Function_SynSitUp);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynDeskStart, Function_SynDeskStart);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynDeskState, Function_DeskState);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_DisMissDeskRs, Function_DismissDesk);
		HotfixNetworkComponent.RemoveListener(MessageID.Syn_TexasStart, Function_TexasStart);
		HotfixNetworkComponent.RemoveListener(MessageID.MsgDealCard, Function_DealerCard);
		HotfixNetworkComponent.RemoveListener(MessageID.Syn_TexasOptionInfo, Function_TexasOptionInfo);
		HotfixNetworkComponent.RemoveListener(MessageID.Syn_TexasChangeOption, Function_TexasChangeOption);
		HotfixNetworkComponent.RemoveListener(MessageID.Syn_DealCommonCard, Function_DealCommonCard);
		HotfixNetworkComponent.RemoveListener(MessageID.Syn_BetCoin, Function_BetCoin);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynTexasSettle, Function_TexasSettle);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_EdgePot, Function_EdgePot);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_DeskContinuedTimeRs, Function_DeskContinuedTime);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_AdvanceSettleRs, Function_AdvanceSettle);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_ForbidChatRs, Function_ForbidChat);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynSendGift, Function_SynSendGift);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_SitUpRs, Function_SitUp);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynPlayerState, Function_PlayerState);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynTexasPokerHandCards, Function_HandCards);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_ProtectInfo, Function_InsuranceInfo);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_ProtectInfo_end, Function_InsuranceInfo_end);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynProtectCard, Function_BuyInsurance);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynTexasState, Function_TexasState);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_LookCommonCardRs, Function_LookCommonCard);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_BackPot, Function_BackPot);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynCKJackPot, Function_SynCKJackPot);
		base.OnLeave(procedureOwner, isShutdown);
	}

	private void OnOpenUIFormSuccess(object sender, GameEventArgs e)
	{
		var args = e as OpenUIFormSuccessEventArgs;
		if (args.UIForm.Logic.GetType() == typeof(DZGamePanel)) {
			GamePanel = args.UIForm.Logic as DZGamePanel;
		}
	}

	public bool IsRoomOwner()
	{
		if (deskinfo.Creator == null) return false;
		if (Util.IsMySelf(deskinfo.Creator.PlayerId))
			return true;
		return false;
	}

	public DeskPlayer GetSelfPlayer()
	{
		for (var i = 0;i < deskinfo.DeskPlayers.Count;i++) {
			if (Util.IsMySelf(deskinfo.DeskPlayers[i].BasePlayer.PlayerId))
				return deskinfo.DeskPlayers[i];
		}
		return null;
	}

	public DeskPlayer GetPlayerByPos(Position position)
	{
		for (var i = 0;i < deskinfo.DeskPlayers.Count;i++) {
			if (deskinfo.DeskPlayers[i].Pos == position) return deskinfo.DeskPlayers[i];
		}
		return null;
	}

	public DeskPlayer GetPlayerByID(long playerid)
	{
		for (var i = 0;i < deskinfo.DeskPlayers.Count;i++) {
			if ((int)deskinfo.DeskPlayers[i].BasePlayer.PlayerId == playerid) return deskinfo.DeskPlayers[i];
		}
		return null;
	}

	//请求离开桌子
	public void Send_LeaveDesk()
	{
		Msg_LeaveDeskRq msg_leavedesk = MessagePool.Instance.Fetch<Msg_LeaveDeskRq>();
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_LeaveDeskRq, msg_leavedesk);
	}

	//请求坐下
	public void Send_SitDown(Position pos)
	{
		if(!deskinfo.BaseConfig.GpsLimit){
			Msg_SitDownRq req = MessagePool.Instance.Fetch<Msg_SitDownRq>();
			req.Pos = pos;
			req.Gps = "0,0";
			HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_SitDownRq, req);
			return;
		}
		// 使用通用GPS获取方法
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

	//请求站起
	public void Send_SitUpRq()
	{
		Msg_SitUpRq req = MessagePool.Instance.Fetch<Msg_SitUpRq>();
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_SitUpRq, req);
	}

	//请求开始游戏
	public void Send_StartGame()
	{
		Msg_Start req = MessagePool.Instance.Fetch<Msg_Start>();
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_Start, req);
	}

	//请求带入
	public void Send_BringRq(int amount, long leagueId)
	{
		Msg_BringRq req = MessagePool.Instance.Fetch<Msg_BringRq>();
		req.DeskId = deskID;
		req.Amount = amount;
		req.ClubId = leagueId;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_BringRq, req);
	}

	//发送解散房间
	public void Send_DismissDesk()
	{
		Msg_DisMissDeskRq req = MessagePool.Instance.Fetch<Msg_DisMissDeskRq>();
		req.DeskId = deskID;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_DisMissDeskRq, req);
	}

	//暂停恢复房间
	public void Send_PauseDesk(int state)
	{
		Msg_PauseDeskRq req = MessagePool.Instance.Fetch<Msg_PauseDeskRq>();
		req.State = state;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_PauseDeskRq, req);
	}

	//购买时长
	public void Send_DeskContinuedTimeRq(int continuedTime)
	{
		Msg_DeskContinuedTimeRq req = MessagePool.Instance.Fetch<Msg_DeskContinuedTimeRq>();
		req.ContinuedTime = continuedTime;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_DeskContinuedTimeRq, req);
	}

	//购买保险
	public void Send_BuyInsurance(float number, List<int> card) {
		Msg_BuyProtectCardRq req = MessagePool.Instance.Fetch<Msg_BuyProtectCardRq>();
		req.Coin = number;
		req.Card.AddRange(card);
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_BuyProtectCardRq, req);
	}

	//秀手牌
	public void Send_ShowCard(int card1, int card2) {
		Msg_TexasOption req = MessagePool.Instance.Fetch<Msg_TexasOption>();
		req.Option = TexasOption.Show;
		req.ShowCard.Add(card1); req.ShowCard.Add(card2);
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_TexasOption, req);
	}

	//发送盖牌
	public void Send_FoldCard() {
		Msg_TexasOption req = MessagePool.Instance.Fetch<Msg_TexasOption>();
		req.Option = TexasOption.Give;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_TexasOption, req);
	}

	//发送跟注
	public void Send_CallCard() {
		Msg_TexasOption req = MessagePool.Instance.Fetch<Msg_TexasOption>();
		if (deskinfo != null && (deskinfo.FollowCoin == "" || float.Parse(deskinfo.FollowCoin) == 0))
		{
			req.Option = TexasOption.Look;
		}
		else {
			req.Option = TexasOption.Follow;
		}
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_TexasOption, req);
	}

	//发送加注
	public void Send_AddCoin(float amount) {
		DeskPlayer selfplayer = GetSelfPlayer();
		if (selfplayer == null) return;
		Msg_TexasOption req = MessagePool.Instance.Fetch<Msg_TexasOption>();
		req.Option = TexasOption.AddCoin;
		req.Param = amount;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_TexasOption, req);
	}

	//发送提前结算
	public void Send_AdvanceSettle() {
		Msg_AdvanceSettleRq req = MessagePool.Instance.Fetch<Msg_AdvanceSettleRq>();
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_AdvanceSettleRq, req);
	}

	//发送表情
	public void Send_SendGift(long id, int gift)
	{
		Msg_SendGiftRq req = MessagePool.Instance.Fetch<Msg_SendGiftRq>();
		req.ToPlayer = id;
		req.Gift = gift;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_SendGiftRq, req);
	}


	//取消离座
	public void Send_CancelLeave()
	{
		Msg_CancelLeaveRq req = MessagePool.Instance.Fetch<Msg_CancelLeaveRq>();
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_CancelLeaveRq, req);
	}

	//发送加时
	public void Send_AddTime()
	{
		Msg_TexasOption req = MessagePool.Instance.Fetch<Msg_TexasOption>();
		req.Option = TexasOption.AddTime;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_TexasOption, req);
	}

	//看玩家手牌
	public void Send_LookCard(int round, long playerid)
	{
		Msg_LookCardRq req = MessagePool.Instance.Fetch<Msg_LookCardRq>();
		req.Round = round;
		req.PlayerId = playerid;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_LookCardRq, req);
	}

	//看公共牌
	public void Send_LookCommonCard()
	{
		Msg_LookCommonCardRq req = MessagePool.Instance.Fetch<Msg_LookCommonCardRq>();
		req.Round = 0;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_LookCommonCardRq, req);
	}

	//随机座位
	public void Send_RandomSeat() {
		Msg_TexasRandomPosRq req = MessagePool.Instance.Fetch<Msg_TexasRandomPosRq>();
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_TexasRandomPosRq, req);
	}


	//收到离开桌子
	void Function_LeaveDesk(MessageRecvData data)
	{
		Msg_LeaveDeskRs ack = Msg_LeaveDeskRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息离开桌子" , ack.ToString());
		if (GamePanel == null) return;
		if (GamePanel.brecord == true) return;
		Util.GetMyselfInfo().DeskId = 0;
		EnterHome();
	}

	//收到坐下并弹出买入
	void Function_DeskPlayerInfoRs(MessageRecvData data)
	{
		Syn_DeskPlayerInfo ack = Syn_DeskPlayerInfo.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到进桌买入"  + ack.ToString());
		CoroutineRunner.Instance.RunCoroutine(GamePanel.EnterSeat(ack.Player, true));
	}

	//收到坐下不弹出买入
	void Function_DeskPlayerInfoRs1(MessageRecvData data)
	{
		Syn_DeskPlayerInfo ack = Syn_DeskPlayerInfo.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到进桌" , ack.ToString());
		CoroutineRunner.Instance.RunCoroutine(GamePanel.EnterSeat(ack.Player, false));
		if (Util.IsMySelf(ack.Player.BasePlayer.PlayerId)) {
			if (deskinfo.DeskState == DeskState.WaitStart || deskinfo.TexasState == TexasState.TexasWait) {
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

	//玩家筹码变更
	void Function_DeskPlayerInfoRs2(MessageRecvData data)
	{
		Syn_DeskPlayerInfo ack = Syn_DeskPlayerInfo.Parser.ParseFrom(data.Data);
		GF.LogInfo("玩家筹码变更." + ack.ToString());
		GamePanel.SynPlayerInfo(ack.Player);
	}

	//收到站起
	void Function_SynSitUp(MessageRecvData data)
	{
		Msg_SynSitUp ack = Msg_SynSitUp.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到站起. ", ack.Pos.ToString());
		GamePanel.LeaveSeat(ack.Pos);
	}

	//收到开始玩家列表
	void Function_SynDeskStart(MessageRecvData data)
	{
		Msg_SynDeskStart ack = Msg_SynDeskStart.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息通知游戏开始", ack.ToString());
		GamePanel.coroutinemanager.AddCoroutine(GamePanel.DeskStart(ack));
	}

	//收到桌子状态
	void Function_DeskState(MessageRecvData data)
	{
		Msg_SynDeskState ack = Msg_SynDeskState.Parser.ParseFrom(data.Data);
		GF.LogInfo("房间状态:" + ack.State);
		deskinfo.DeskState = ack.State;
		GamePanel.endtime = ack.DestroyTime;
		if (ack.State == DeskState.Pause) {
			GamePanel.GoPanelPause.SetActive(true);
			if (IsRoomOwner()) {
				GamePanel.GoPause1.SetActive(true);
				GamePanel.GoPause2.SetActive(false);
			}
			else {
				GamePanel.GoPause1.SetActive(false);
				GamePanel.GoPause2.SetActive(true);
			}
		}
		else {
			GamePanel.GoPanelPause.SetActive(false);
		}
		if (deskinfo.DeskState != DeskState.WaitStart) {
			GamePanel.GoButtonStart.SetActive(false);
		}
	}

	//收到解散房间
	void Function_DismissDesk(MessageRecvData data)
	{
		GF.LogInfo("收到解散牌桌");
		Msg_DisMissDeskRs ack = Msg_DisMissDeskRs.Parser.ParseFrom(data.Data);
		if (ack.State == 1) {
			GF.UI.ShowToast("房间将解散");
		}
	}

	//收到牌局开始
	void Function_TexasStart(MessageRecvData data)
	{
		Syn_TexasStart ack = Syn_TexasStart.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息通知德州开始", ack.ToString());
		GamePanel.coroutinemanager.AddCoroutine(GamePanel.GameStart(ack));
	}

	//收到牌局发牌
	void Function_DealerCard(MessageRecvData data)
	{
		Syn_DealCard ack = Syn_DealCard.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息发牌", ack.ToString());
		GamePanel.coroutinemanager.AddCoroutine(GamePanel.DealCard(ack));
	}

	//收到玩家操作
	void Function_TexasOptionInfo(MessageRecvData data)
	{
		Syn_TexasOptionInfo ack = Syn_TexasOptionInfo.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息德州操作通知", ack.ToString());
		GamePanel.coroutinemanager.AddCoroutine(GamePanel.TexasOptionInfo(ack));
	}

	//收到更改操作对象
	void Function_TexasChangeOption(MessageRecvData data)
	{
		Syn_TexasChangeOption ack = Syn_TexasChangeOption.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息更改操作对象", ack.ToString());
		GamePanel.coroutinemanager.AddCoroutine(GamePanel.TexasChangeOption(ack));
	}

	//收到公共牌
	void Function_DealCommonCard(MessageRecvData data)
	{
		Syn_DealCommonCard ack = Syn_DealCommonCard.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息公共牌", ack.ToString());
		GamePanel.coroutinemanager.AddCoroutine(GamePanel.DealCommonCard(ack));
	}

	//收到下注
	public void Function_BetCoin(MessageRecvData data)
	{
		Syn_BetCoin ack = Syn_BetCoin.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息下注.", ack.ToString());
		GamePanel.BetCoin(ack);
	}

	//收到结算
	public void Function_TexasSettle(MessageRecvData data)
	{
		Msg_SynTexasSettle ack = Msg_SynTexasSettle.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息德州结算", ack.ToString());
		GamePanel.coroutinemanager.AddCoroutine(GamePanel.TexasSettle(ack));
	}

	//收到主池边池信息
	public void Function_EdgePot(MessageRecvData data)
	{
		Msg_EdgePot ack = Msg_EdgePot.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息边池", ack.ToString());
		GamePanel.coroutinemanager.AddCoroutine(GamePanel.EdgePot(ack));
	}

	//收到房间购买时长
	public void Function_DeskContinuedTime(MessageRecvData data)
	{
		Msg_DeskContinuedTimeRs ack = Msg_DeskContinuedTimeRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("房间续时成功", ack.ToString());
		GF.UI.ShowToast("房间续时成功");
		GamePanel.endtime += ack.ContinuedTime * 60 * 1000;
	}

	//收到提前结算
	public void Function_AdvanceSettle(MessageRecvData data) {
		GF.LogInfo("玩家提前结算");
		Msg_AdvanceSettleRs ack = Msg_AdvanceSettleRs.Parser.ParseFrom(data.Data);
		GF.UI.ShowToast("结算成功");
	}

	//收到禁言响应
	public void Function_ForbidChat(MessageRecvData data)
	{
		GF.LogInfo("禁言");
		Msg_ForbidChatRs ack = Msg_ForbidChatRs.Parser.ParseFrom(data.Data);
		DeskPlayer deskplayer = GetPlayerByID(ack.PlayerId);
		deskplayer.Forbid = ack.Type == 1;
	}

	//收到表情
	public void Function_SynSendGift(MessageRecvData data)//
	{
		Msg_SynSendGift ack = Msg_SynSendGift.Parser.ParseFrom(data.Data);
		GF.LogInfo("礼物. ", ack.ToString());
		GamePanel.SendGift(ack.Sender.PlayerId, ack.ToPlayer, ack.Gift.ToString());
	}

	//收到下局结束站起
	public void Function_SitUp(MessageRecvData data)
	{
		GF.LogInfo("下局结束站起");
		Msg_SitUpRs ack = Msg_SitUpRs.Parser.ParseFrom(data.Data);
		GF.UI.ShowToast("牌局结束站起");
	}

	//收到玩家在线状态变更
	public void Function_PlayerState(MessageRecvData data)
	{
		Msg_SynPlayerState ack = Msg_SynPlayerState.Parser.ParseFrom(data.Data);
		GF.LogInfo($"玩家在线状态变更. {ack.PlayerId} {ack.State}");
		GamePanel.SynPlayerState(ack);
	}

	//收到玩家手牌
	public void Function_HandCards(MessageRecvData data)
	{
		Msg_SynTexasPokerHandCards ack = Msg_SynTexasPokerHandCards.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息手牌展示", ack.ToString());
		GamePanel.coroutinemanager.AddCoroutine(GamePanel.HandCards(ack));
	}

	//收到允许购买保险
	public void Function_InsuranceInfo(MessageRecvData data)
	{
		Msg_ProtectInfo ack = Msg_ProtectInfo.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息保险通知", ack.ToString());
		GamePanel.coroutinemanager.AddCoroutine(GamePanel.InsuranceInfo(ack));
	}

	//收到保险结束
	public void Function_InsuranceInfo_end(MessageRecvData data)
	{
		GF.LogInfo("玩家购买保险结束");
		GamePanel.coroutinemanager.AddCoroutine(GamePanel.BuyInsurance_end());
	}

	//收到成功购买保险
	public void Function_BuyInsurance(MessageRecvData data)
	{
		Msg_SynProtectCard ack = Msg_SynProtectCard.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息购买保险", ack.ToString());
		GamePanel.coroutinemanager.AddCoroutine(GamePanel.BuyInsurance(ack));
	}

	//收到切换到保险状态
	public void Function_TexasState(MessageRecvData data)
	{
		Msg_SynTexasState ack = Msg_SynTexasState.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息同步状态", ack.ToString());
		GamePanel.coroutinemanager.AddCoroutine(GamePanel.SynTexasState(ack));
	}

	//收到查看公共牌
	public void Function_LookCommonCard(MessageRecvData data)
	{
		Msg_LookCommonCardRs ack = Msg_LookCommonCardRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("查看公共牌", ack.ToString());
		GamePanel.LookCommonCard(ack);
	}

	//收到查看公共牌
	public void Function_BackPot(MessageRecvData data)
	{
		Syn_BackPot ack = Syn_BackPot.Parser.ParseFrom(data.Data);
		GF.LogInfo("底池退回" , ack.ToString());
		GamePanel.SynBackPot(ack);
	}

	public void EnterHome()
    {
        GF.LogInfo("退出德州流程，跳转大厅流程");
        ChangeState<HomeProcedure>(procedure);
    }

	//获取牌型比较结构
	//首先比较cardtype，牌型会决定大小
	//如果cardtype相同，比较realcard，依次循环，第一个不相同的值决定大小
	//注意，这个readcard可能不到5张牌，因为公共牌可能没发，这个不影响结构体的大小判断
	public class TexasCardInfo
	{
		int MakeCard(int color, int value) {
			if (value == 14) {
				return (color << 4) + 1;
			}
			return (color << 4) + value;
		}
		List<int> FindColor(List<int> allcard, int value, int count = 1) {
			int ncount = 0; List<int> result = new();
			for (var i = 0;i < allcard.Count;i++) {
				if (value == 14) {
					if (GameUtil.GetInstance().GetValue(allcard[i]) == 1) {
						result.Add(allcard[i]);
						ncount++;
					}
				}
				else {
					if (GameUtil.GetInstance().GetValue(allcard[i]) == value) {
						result.Add(allcard[i]);
						ncount++;
					}
				}
				if (count == ncount) break;
			}
			return result;
		}

		public TexasCardType cardtype = TexasCardType.High;
		public List<int> maincard = new(), slavecard = new();
		public List<int> realcard = new();
		public TexasCardInfo(List<int> allcard, TexasCardType _cardtype, List<int> _maincard, List<int> _slavecard, int ncolor = 0) {
			cardtype = _cardtype;
			maincard.AddRange(_maincard);
			slavecard.AddRange(_slavecard);
			switch (cardtype) {
				case TexasCardType.KingTonghuashun:
					realcard.Add(MakeCard(ncolor,  1));
					realcard.Add(MakeCard(ncolor, 13));
					realcard.Add(MakeCard(ncolor, 12));
					realcard.Add(MakeCard(ncolor, 11));
					realcard.Add(MakeCard(ncolor, 10));
					break;
				case TexasCardType.Tonghuashun:
					realcard.Add(MakeCard(ncolor, maincard[0]    ));
					realcard.Add(MakeCard(ncolor, maincard[0] - 1));
					realcard.Add(MakeCard(ncolor, maincard[0] - 2));
					realcard.Add(MakeCard(ncolor, maincard[0] - 3));
					realcard.Add(MakeCard(ncolor, maincard[0] - 4));
					break;
				case TexasCardType.FourCard:
					realcard.Add(MakeCard(0, maincard[0]));
					realcard.Add(MakeCard(1, maincard[0]));
					realcard.Add(MakeCard(2, maincard[0]));
					realcard.Add(MakeCard(3, maincard[0]));
					realcard.AddRange(FindColor(allcard, slavecard[0], 1));
					break;
				case TexasCardType.Gourd:
					realcard.AddRange(FindColor(allcard, maincard[0], 3));
					realcard.AddRange(FindColor(allcard, slavecard[0], 2));
					break;
				case TexasCardType.SameColor:
					for (var i = 0;i < maincard.Count;i++) {
						realcard.Add(MakeCard(ncolor, maincard[i]));
					}
					break;
				case TexasCardType.Shunzi:
					realcard.AddRange(FindColor(allcard, maincard[0], 1));
					realcard.AddRange(FindColor(allcard, maincard[0] - 1, 1));
					realcard.AddRange(FindColor(allcard, maincard[0] - 2, 1));
					realcard.AddRange(FindColor(allcard, maincard[0] - 3, 1));
					realcard.AddRange(FindColor(allcard, maincard[0] - 4, 1));
					break;
				case TexasCardType.ThreeCard:
					realcard.AddRange(FindColor(allcard, maincard[0], 3));
					realcard.AddRange(FindColor(allcard, slavecard[0], 1));
					realcard.AddRange(FindColor(allcard, slavecard[1], 1));
					break;
				case TexasCardType.TwoPair:
					realcard.AddRange(FindColor(allcard, maincard[0], 2));
					realcard.AddRange(FindColor(allcard, maincard[1], 2));
					realcard.AddRange(FindColor(allcard, slavecard[0], 1));
					break;
				case TexasCardType.Pair:
					realcard.AddRange(FindColor(allcard, maincard[0], 2));
					foreach (var card in slavecard) {
						realcard.AddRange(FindColor(allcard, card, 1));
					}
					break;
				case TexasCardType.High:
					foreach (var card in maincard) {
						realcard.AddRange(FindColor(allcard, card, 1));
					}
					break;
			}
		}
	}

	//获取牌型比较结构，输入两个int数组，一个是手牌，一个是公共牌，输出TexasCardInfo
	public TexasCardInfo GetCardType(List<int> allcard)
	{
		//colors为花色对应的张数，values为点数对应的张数，nvalues为同花(>=5)的那个颜色中点数对应的张数
		Dictionary<int, int> colors = new(), values = new(), nvalues = new(); int ncolor = 0;
		foreach (var card in allcard) {
			var color = GameUtil.GetInstance().GetColor(card);
			if (colors.ContainsKey(color)) {
				colors[color]++;
			}
			else {
				colors[color] = 1;
			}
			var value = GameUtil.GetInstance().GetValue(card);
			if (value == 1) value = 14;
			if (values.ContainsKey(value)) {
				values[value]++;
			}
			else {
				values[value] = 1;
			}
		}
		//可能手牌+公共牌不到5张，这个不影响最终的结果
		//后续代码中不应该直接使用master[??]或者slave[??]的方式，要避免地址越界。
		//应该使用List.Take(??).ToList()的方式来截取。
		List<int> master = new(), slave = new();
		if (colors.Count(kvp => kvp.Value >= 5) == 1) {
			ncolor = colors.First(kvp => kvp.Value >= 5).Key;
			foreach (var card in allcard) {
				if (GameUtil.GetInstance().GetColor(card) != ncolor) continue;
				var value = GameUtil.GetInstance().GetValue(card);
				if (value == 1) value = 14;
				if (nvalues.ContainsKey(value)) {
					nvalues[value]++;
				}
				else {
					nvalues[value] = 1;
				}
			}
			//同花顺，主牌为最大的一张牌，副牌为空
			do {
				if (nvalues.ContainsKey(14) == true && nvalues.ContainsKey(13) == true && nvalues.ContainsKey(12) == true && nvalues.ContainsKey(11) == true && nvalues.ContainsKey(10) == true) {
					//皇家同花顺，主牌为空，副牌为空
					return new TexasCardInfo(
						allcard,
						TexasCardType.KingTonghuashun,
						new List<int>(),
						new List<int>(),
						ncolor
					);
				}
				if (nvalues.ContainsKey(13) == true && nvalues.ContainsKey(12) == true && nvalues.ContainsKey(11) == true && nvalues.ContainsKey(10) == true && nvalues.ContainsKey(9) == true) {
					return new TexasCardInfo(
						allcard,
						TexasCardType.Tonghuashun,
						new List<int> {13},
						new List<int>(),
						ncolor
					);
				}
				if (nvalues.ContainsKey(12) == true && nvalues.ContainsKey(11) == true && nvalues.ContainsKey(10) == true && nvalues.ContainsKey(9) == true && nvalues.ContainsKey(8) == true) {
					return new TexasCardInfo(
						allcard,
						TexasCardType.Tonghuashun,
						new List<int> {12},
						new List<int>(),
						ncolor
					);
				}
				if (nvalues.ContainsKey(11) == true && nvalues.ContainsKey(10) == true && nvalues.ContainsKey(9) == true && nvalues.ContainsKey(8) == true && nvalues.ContainsKey(7) == true) {
					return new TexasCardInfo(
						allcard,
						TexasCardType.Tonghuashun,
						new List<int> {11},
						new List<int>(),
						ncolor
					);
				}
				if (nvalues.ContainsKey(10) == true && nvalues.ContainsKey(9) == true && nvalues.ContainsKey(8) == true && nvalues.ContainsKey(7) == true && nvalues.ContainsKey(6) == true) {
					return new TexasCardInfo(
						allcard,
						TexasCardType.Tonghuashun,
						new List<int> {10},
						new List<int>(),
						ncolor
					);
				}
				if (nvalues.ContainsKey(9) == true && nvalues.ContainsKey(8) == true && nvalues.ContainsKey(7) == true && nvalues.ContainsKey(6) == true && nvalues.ContainsKey(5) == true) {
					return new TexasCardInfo(
						allcard,
						TexasCardType.Tonghuashun,
						new List<int> {9},
						new List<int>(),
						ncolor
					);
				}
				if (nvalues.ContainsKey(8) == true && nvalues.ContainsKey(7) == true && nvalues.ContainsKey(6) == true && nvalues.ContainsKey(5) == true && nvalues.ContainsKey(4) == true) {
					return new TexasCardInfo(
						allcard,
						TexasCardType.Tonghuashun,
						new List<int> {8},
						new List<int>(),
						ncolor
					);
				}
				if (nvalues.ContainsKey(7) == true && nvalues.ContainsKey(6) == true && nvalues.ContainsKey(5) == true && nvalues.ContainsKey(4) == true && nvalues.ContainsKey(3) == true) {
					return new TexasCardInfo(
						allcard,
						TexasCardType.Tonghuashun,
						new List<int> {7},
						new List<int>(),
						ncolor
					);
				}
				if (nvalues.ContainsKey(6) == true && nvalues.ContainsKey(5) == true && nvalues.ContainsKey(4) == true && nvalues.ContainsKey(3) == true && nvalues.ContainsKey(2) == true) {
					return new TexasCardInfo(
						allcard,
						TexasCardType.Tonghuashun,
						new List<int> {6},
						new List<int>(),
						ncolor
					);
				}
				if (nvalues.ContainsKey(5) == true && nvalues.ContainsKey(4) == true && nvalues.ContainsKey(3) == true && nvalues.ContainsKey(2) == true && nvalues.ContainsKey(14) == true) {
					return new TexasCardInfo(
						allcard,
						TexasCardType.Tonghuashun,
						new List<int> {5},
						new List<int>(),
						ncolor
					);
				}
			} while (false);
		}
		if (values.ContainsValue(4) == true) {
			//四条，主牌为四条的牌，副牌为剩余的最大的一张牌
			master = values.Where(kvp => kvp.Value == 4).OrderByDescending(kvp => kvp.Key).Select(kvp => kvp.Key).ToList();
			slave = values.Where(kvp => kvp.Value != 4).OrderByDescending(kvp => kvp.Key).Select(kvp => kvp.Key).ToList();
			return new TexasCardInfo(
				allcard,
				TexasCardType.FourCard,
				master.Take(1).ToList(),
				slave.Take(1).ToList()
			);
		}
		if (values.Count(kvp => kvp.Value == 3) == 2) {
			//葫芦，主牌为三条的牌，副牌为对子的牌(两个三条)
			master = values.Where(kvp => kvp.Value == 3).OrderByDescending(kvp => kvp.Key).Select(kvp => kvp.Key).ToList();
			slave = values.Where(kvp => kvp.Value == 3).OrderBy(kvp => kvp.Key).Select(kvp => kvp.Key).ToList();
			return new TexasCardInfo(
				allcard,
				TexasCardType.Gourd,
				master.Take(1).ToList(),
				master.Take(1).ToList()
			);
		}
		if (values.ContainsValue(3) == true && values.ContainsValue(2) == true) {
			//葫芦，主牌为三条的牌，副牌为对子的牌
			master = values.Where(kvp => kvp.Value == 3).OrderByDescending(kvp => kvp.Key).Select(kvp => kvp.Key).ToList();
			slave = values.Where(kvp => kvp.Value == 2).OrderByDescending(kvp => kvp.Key).Select(kvp => kvp.Key).ToList();
			return new TexasCardInfo(
				allcard,
				TexasCardType.Gourd,
				master.Take(1).ToList(),
				slave.Take(1).ToList()
			);
		}
		if (colors.Count(kvp => kvp.Value >= 5) == 1) {
			//同花，主牌为最大的五张牌，副牌为空
			return new TexasCardInfo(
				allcard,
				TexasCardType.SameColor,
				nvalues.OrderByDescending(kvp => kvp.Key).Select(kvp => kvp.Key).Take(5).ToList(),
				new List<int>(),
				ncolor
			);
		}
		do {
			//顺子，主牌为最大的一张牌，副牌为空
			if (values.ContainsKey(14) == true && values.ContainsKey(13) == true && values.ContainsKey(12) == true && values.ContainsKey(11) == true && values.ContainsKey(10) == true) {
				return new TexasCardInfo(
					allcard,
					TexasCardType.Shunzi,
					new List<int> {14},
					new List<int>()
				);
			}
			if (values.ContainsKey(13) == true && values.ContainsKey(12) == true && values.ContainsKey(11) == true && values.ContainsKey(10) == true && values.ContainsKey(9) == true) {
				return new TexasCardInfo(
					allcard,
					TexasCardType.Shunzi,
					new List<int> {13},
					new List<int>()
				);
			}
			if (values.ContainsKey(12) == true && values.ContainsKey(11) == true && values.ContainsKey(10) == true && values.ContainsKey(9) == true && values.ContainsKey(8) == true) {
				return new TexasCardInfo(
					allcard,
					TexasCardType.Shunzi,
					new List<int> {12},
					new List<int>()
				);
			}
			if (values.ContainsKey(11) == true && values.ContainsKey(10) == true && values.ContainsKey(9) == true && values.ContainsKey(8) == true && values.ContainsKey(7) == true) {
				return new TexasCardInfo(
					allcard,
					TexasCardType.Shunzi,
					new List<int> {11},
					new List<int>()
				);
			}
			if (values.ContainsKey(10) == true && values.ContainsKey(9) == true && values.ContainsKey(8) == true && values.ContainsKey(7) == true && values.ContainsKey(6) == true) {
				return new TexasCardInfo(
					allcard,
					TexasCardType.Shunzi,
					new List<int> {10},
					new List<int>()
				);
			}
			if (values.ContainsKey(9) == true && values.ContainsKey(8) == true && values.ContainsKey(7) == true && values.ContainsKey(6) == true && values.ContainsKey(5) == true) {
				return new TexasCardInfo(
					allcard,
					TexasCardType.Shunzi,
					new List<int> {9},
					new List<int>()
				);
			}
			if (values.ContainsKey(8) == true && values.ContainsKey(7) == true && values.ContainsKey(6) == true && values.ContainsKey(5) == true && values.ContainsKey(4) == true) {
				return new TexasCardInfo(
					allcard,
					TexasCardType.Shunzi,
					new List<int> {8},
					new List<int>()
				);
			}
			if (values.ContainsKey(7) == true && values.ContainsKey(6) == true && values.ContainsKey(5) == true && values.ContainsKey(4) == true && values.ContainsKey(3) == true) {
				return new TexasCardInfo(
					allcard,
					TexasCardType.Shunzi,
					new List<int> {7},
					new List<int>()
				);
			}
			if (values.ContainsKey(6) == true && values.ContainsKey(5) == true && values.ContainsKey(4) == true && values.ContainsKey(3) == true && values.ContainsKey(2) == true) {
				return new TexasCardInfo(
					allcard,
					TexasCardType.Shunzi,
					new List<int> {6},
					new List<int>()
				);
			}
			if (values.ContainsKey(5) == true && values.ContainsKey(4) == true && values.ContainsKey(3) == true && values.ContainsKey(2) == true && values.ContainsKey(14) == true) {
				return new TexasCardInfo(
					allcard,
					TexasCardType.Shunzi,
					new List<int> {5},
					new List<int>()
				);
			}
		} while (false);
		if (values.ContainsValue(3) == true) {
			//三条，主牌为三条的牌，副牌为剩余的最大的两张牌
			master = values.Where(kvp => kvp.Value == 3).OrderByDescending(kvp => kvp.Key).Select(kvp => kvp.Key).ToList();
			slave = values.Where(kvp => kvp.Value == 1).OrderByDescending(kvp => kvp.Key).Select(kvp => kvp.Key).ToList();
			return new TexasCardInfo(
				allcard,
				TexasCardType.ThreeCard,
				master.Take(1).ToList(),
				slave.Take(2).ToList()
			);
		}
		if (values.ContainsValue(2) == true) {
			master = values.Where(kvp => kvp.Value == 2).OrderByDescending(kvp => kvp.Key).Select(kvp => kvp.Key).ToList();
			if (values.Count(kvp => kvp.Value == 2) >= 2) {
				//两对，主牌为两对的牌，副牌为剩余的最大的一张牌
				Dictionary<int, int> temp = new();
				List<int> exclude = master.Take(2).ToList();
				foreach (var kvp in values) {
					if (exclude.Contains(kvp.Key)) continue;
					temp[kvp.Key] = kvp.Value;
				}
				slave = temp.OrderByDescending(kvp => kvp.Key).Select(kvp => kvp.Key).ToList();
				return new TexasCardInfo(
					allcard,
					TexasCardType.TwoPair,
					master.Take(2).ToList(),
					slave.Take(1).ToList()
				);
			}
			else {
				//一对，主牌为一对的牌，副牌为剩余的最大的三张牌
				slave = values.Where(kvp => kvp.Value == 1).OrderByDescending(kvp => kvp.Key).Select(kvp => kvp.Key).ToList();
				return new TexasCardInfo(
					allcard,
					TexasCardType.Pair,
					master.Take(1).ToList(),
					slave.Take(3).ToList()
				);
			}
		}
		//高牌，主牌为最大的五张牌，副牌为空
		master = values.Where(kvp => kvp.Value == 1).OrderByDescending(kvp => kvp.Key).Select(kvp => kvp.Key).ToList();
		return new TexasCardInfo(
			allcard,
			TexasCardType.High,
			master.Take(5).ToList(),
			new List<int>()
		);
	}

	public string CardType2String(TexasCardType CardType)
	{
		return CardType switch {
			TexasCardType.High => "高牌",
			TexasCardType.Pair => "一对",
			TexasCardType.TwoPair => "两对",
			TexasCardType.ThreeCard => "三条",
			TexasCardType.Shunzi => "顺子",
			TexasCardType.SameColor => "同花",
			TexasCardType.Gourd => "葫芦",
			TexasCardType.FourCard => "四条",
			TexasCardType.Tonghuashun => "同花顺",
			TexasCardType.KingTonghuashun => "皇家同花顺",
			_ => "",
		};
	}

	public void Function_SynCKJackPot(MessageRecvData data)
	{
		Msg_SynCKJackPot ack = Msg_SynCKJackPot.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息奖池得奖通知", ack.ToString());
		msg_SynCKJackPot = ack;
	}
}

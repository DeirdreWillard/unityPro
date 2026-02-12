using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using NetMsg;
using System.Linq;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class BJProcedure : ProcedureBase
{
    public GamePanel_bj GamePanel_bj { get; private set; }
    public int GameUIFormId_bj;
    private IFsm<IProcedureManager> procedure;
    public int deskID;
    public Msg_EnterCompareChickenDeskRs enterBJDeskRs;
    public Msg_Match MatchInfo;
    

    public long PreOperateGuid { get; set; } = 0;
    public bool brecord = false;

    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        this.procedure = procedureOwner;
        CoroutineRunner.Instance.RunCoroutine(GFBuiltin.Instance.SwitchToPortrait());

        enterBJDeskRs = Msg_EnterCompareChickenDeskRs.Parser.ParseFrom(procedure.GetData<VarByteArray>("enterBJDeskRs"));
        deskID = enterBJDeskRs.BaseInfo.DeskId;
        Util.GetMyselfInfo().DeskId = deskID;
        GF.LogInfo("比鸡桌子信息", enterBJDeskRs.ToString());

        GlobalManager.SendSystemConfigRq();
        //显示界面
        ShowBJGamePanel();
    }

    public void AddListener()
    {
        //网络监听事件
        GF.Event.Subscribe(GFEventArgs.EventId, OnGFEventChanged);
        GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
        GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);

        HotfixNetworkComponent.AddListener(MessageID.Msg_LeaveDeskRs, Function_LeaveDeskRs);
        HotfixNetworkComponent.AddListener(MessageID.Syn_DeskPlayerInfo, Function_DeskPlayerInfoRs);
        HotfixNetworkComponent.AddListener(MessageID.Syn_DeskPlayerInfo1, Function_DeskPlayerInfoRs1);
        HotfixNetworkComponent.AddListener(MessageID.Syn_DeskPlayerInfo2, Function_DeskPlayerInfoRs2);
        HotfixNetworkComponent.AddListener(MessageID.Msg_BringRs, Function_BringRs);
        HotfixNetworkComponent.AddListener(MessageID.MsgDealCard, Function_DealCard);

        HotfixNetworkComponent.AddListener(MessageID.Msg_SynSitUp, Function_SynSitUp);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SitUpRs, Function_SitUpRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_DisMissDeskRs, Function_DisMissDeskRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_AdvanceSettleRs, Function_AdvanceSettle);

        HotfixNetworkComponent.AddListener(MessageID.Msg_ForbidChatRs, Function_ForbidChatRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynSendGift, Function_SynSendGift);

        HotfixNetworkComponent.AddListener(MessageID.Msg_CancelLeaveRs, Function_CancelLeaveRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynPlayerState, Function_SynPlayerState);

        HotfixNetworkComponent.AddListener(MessageID.Msg_PauseDeskRs, Function_PauseDeskRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynDeskState, Function_SynDeskState);
        HotfixNetworkComponent.AddListener(MessageID.Msg_DeskContinuedTimeRs, Function_DeskContinuedTimeRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynDeskLessFive, Function_SynNotifyDeskLessTime);
        HotfixNetworkComponent.AddListener(MessageID.SingleGameRecord, Function_SingleGameRecord);

        HotfixNetworkComponent.AddListener(MessageID.Msg_AutoCompareCardRs, Function_AutoCompareCardRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_CompareCardRs, Function_CompareCardRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynCompareCard, Function_SynCompareCard);
        HotfixNetworkComponent.AddListener(MessageID.Msg_CompareCardGiveRs, Function_CompareCardGiveRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynPlayerGiveCard, Function_SynPlayerGiveCard);
        HotfixNetworkComponent.AddListener(MessageID.Syn_CompareChickenSettle, Function_Settle);
        HotfixNetworkComponent.AddListener(MessageID.Syn_CompareChickenState, Function_CompareChickenState);
        HotfixNetworkComponent.AddListener(MessageID.Msg_EnterCompareChickenDeskRs, Function_EnterCompareChickenDeskRs);

        HotfixNetworkComponent.AddListener(MessageID.Msg_SynCKJackPot, Function_SynCKJackPot);
        HotfixNetworkComponent.AddListener(MessageID.Msg_getSingleMatchRs, Function_getSingleMatchRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynMatchLose, Function_SynMatchLose);
    }


    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
		Util.GetMyselfInfo().DeskId = 0;
        GlobalManager.GetInstance().ReSetUI();
        GF.Event.Unsubscribe(GFEventArgs.EventId, OnGFEventChanged);
        GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
        GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);

        //网络监听事件
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_LeaveDeskRs, Function_LeaveDeskRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_DeskPlayerInfo, Function_DeskPlayerInfoRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_DeskPlayerInfo1, Function_DeskPlayerInfoRs1);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_DeskPlayerInfo2, Function_DeskPlayerInfoRs2);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_BringRs, Function_BringRs);
        HotfixNetworkComponent.RemoveListener(MessageID.MsgDealCard, Function_DealCard);

        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynSitUp, Function_SynSitUp);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SitUpRs, Function_SitUpRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_DisMissDeskRs, Function_DisMissDeskRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_AdvanceSettleRs, Function_AdvanceSettle);

        HotfixNetworkComponent.RemoveListener(MessageID.Msg_ForbidChatRs, Function_ForbidChatRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynSendGift, Function_SynSendGift);

        HotfixNetworkComponent.RemoveListener(MessageID.Msg_CancelLeaveRs, Function_CancelLeaveRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynPlayerState, Function_SynPlayerState);

        HotfixNetworkComponent.RemoveListener(MessageID.Msg_PauseDeskRs, Function_PauseDeskRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynDeskState, Function_SynDeskState);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_DeskContinuedTimeRs, Function_DeskContinuedTimeRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynDeskLessFive, Function_SynNotifyDeskLessTime);
        HotfixNetworkComponent.RemoveListener(MessageID.SingleGameRecord, Function_SingleGameRecord);

        HotfixNetworkComponent.RemoveListener(MessageID.Msg_AutoCompareCardRs, Function_AutoCompareCardRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_CompareCardRs, Function_CompareCardRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynCompareCard, Function_SynCompareCard);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_CompareCardGiveRs, Function_CompareCardGiveRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynPlayerGiveCard, Function_SynPlayerGiveCard);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_CompareChickenSettle, Function_Settle);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_CompareChickenState, Function_CompareChickenState);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_EnterCompareChickenDeskRs, Function_EnterCompareChickenDeskRs);
        
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynCKJackPot, Function_SynCKJackPot);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_getSingleMatchRs, Function_getSingleMatchRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynMatchLose, Function_SynMatchLose);
        base.OnLeave(procedureOwner, isShutdown);
    }
    private void OnGFEventChanged(object sender, GameEventArgs e)
    {
        if (GamePanel_bj != null)
            GamePanel_bj.OnGFEventChanged(sender, e);
    }

    public void OnUserDataChanged(object sender, GameEventArgs e)
    {
        if (GamePanel_bj != null)
            GamePanel_bj.OnUserDataChanged(sender, e);
    }

    private void OnClubDataChanged(object sender, GameEventArgs e)
    {
        if (GamePanel_bj != null)
            GamePanel_bj.OnClubDataChanged(sender, e);
    }

    public async void ShowBJGamePanel()
    {
        // 异步打开游戏界面，自动显示等待框
        var uiParams = UIParams.Create();
        GF.LogInfo("打开比鸡主游戏界面");
        var gamePanel = await GF.UI.OpenUIFormAwait(UIViews.BJ_GamePanel, uiParams);
        if (gamePanel != null)
        {
            GamePanel_bj = gamePanel as GamePanel_bj;
            brecord = false;
            AddListener();
        }
    }

    #region 网络发送接收

    public void Send_AdvanceSettle()
    {
        Msg_AdvanceSettleRq req = MessagePool.Instance.Fetch<Msg_AdvanceSettleRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_AdvanceSettleRq, req);
    }
    /// <summary>
    /// 房主开始游戏
    /// </summary>
    public void Send_Start()
    {
        // Msg_Start
        Msg_Start req = MessagePool.Instance.Fetch<Msg_Start>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_Start, req);
    }
    /// <summary>
    ///解散牌桌
    /// </summary>

    public void Send_DisMissDeskRq()
    {
        // Msg_DisMissDeskRq
        Msg_DisMissDeskRq req = MessagePool.Instance.Fetch<Msg_DisMissDeskRq>();
        req.DeskId = deskID;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_DisMissDeskRq, req);
    }

    public void Function_AdvanceSettle(MessageRecvData data)
    {
        GF.LogInfo("玩家提前结算");
        Msg_AdvanceSettleRs ack = Msg_AdvanceSettleRs.Parser.ParseFrom(data.Data);
        GF.UI.ShowToast("结算成功");
    }

    public void Function_DisMissDeskRs(MessageRecvData data)
    {
        // Msg_LeaveDeskRs
        GF.LogInfo("收到消息解散牌桌");
        Msg_DisMissDeskRs ack = Msg_DisMissDeskRs.Parser.ParseFrom(data.Data);
        if (ack.State == 1)
        {
            GF.UI.ShowToast("房间将解散");
        }
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

    public void Function_LeaveDeskRs(MessageRecvData data)
    {
        // Msg_LeaveDeskRs
        Msg_LeaveDeskRs ack = Msg_LeaveDeskRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息离开桌子", ack.ToString());
        if (brecord == true) return;
		Util.GetMyselfInfo().DeskId = 0;
        //退出游戏逻辑
        EnterHome();
    }

    public void EnterHome()
    {
        GF.LogInfo("退出比鸡流程，跳转大厅流程");
        if (enterBJDeskRs.BaseInfo.BaseConfig.DeskType == DeskType.Match && MatchInfo?.MatchId > 0)
        {
            procedure.SetData<VarInt64>("matchID", MatchInfo.MatchId);
        }
        ChangeState<HomeProcedure>(procedure);
    }

    public DeskPlayer GetSelfPlayer()
    {
        Seat_bj player_BJ = GamePanel_bj.seatMgr.GetSelfSeat();
        if (player_BJ != null)
        {
            return player_BJ.playerInfo;
        }
        return null;
    }

    /// <summary>
    /// 推送桌子玩家信息
    /// </summary>
    /// 自动检测只有一次
    public void Function_DeskPlayerInfoRs(MessageRecvData data)
    {
        // Syn_DeskPlayerInfo
        Syn_DeskPlayerInfo ack = Syn_DeskPlayerInfo.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息推送桌子玩家", ack.ToString());
        CoroutineRunner.Instance.StartCoroutine(GamePanel_bj.Function_DeskPlayerInfoRs(ack.Player, true));
    }

    public void Function_DeskPlayerInfoRs1(MessageRecvData data)
    {
        Syn_DeskPlayerInfo ack = Syn_DeskPlayerInfo.Parser.ParseFrom(data.Data);
        GF.LogInfo("玩家买入", ack.ToString());
        GamePanel_bj.seatMgr.PlayerEnter(ack.Player);
        if (Util.IsMySelf(ack.Player.BasePlayer.PlayerId))
        {
            if (enterBJDeskRs.CkState == CompareChickenState.CkWait)
            {
                GF.UI.ShowToast("带入成功");
            }
            else
            {
                if (ack.Player.BringNum > 0)
                {
                    GF.UI.ShowToast("带入下局生效");
                }
                else
                {
                    GF.UI.ShowToast("带入成功");
                }
            }
        }
    }

    //玩家筹码变更
    void Function_DeskPlayerInfoRs2(MessageRecvData data)
    {
        Syn_DeskPlayerInfo ack = Syn_DeskPlayerInfo.Parser.ParseFrom(data.Data);
        GF.LogInfo("玩家筹码变更", ack.ToString());
        GamePanel_bj.seatMgr.UpdatePlayerInfo(ack.Player);
        Seat_bj player = GamePanel_bj.seatMgr.GetPlayerByPlayerID(ack.Player.BasePlayer.PlayerId);
        player.Function_SynPlayerState(player.playerInfo.State, player.playerInfo.LeaveTime);
    }

    /// <summary>
    /// 发牌
    /// </summary>
    public void Function_DealCard(MessageRecvData data)
    {
        Msg_DealCard ack = Msg_DealCard.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息发牌", ack.ToString());
        GamePanel_bj.Syn_DealCard(ack.Deal.ToList());
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

    public void Function_BringRs(MessageRecvData data)
    {
        //Msg_BringRs
        Msg_BringRs ack = Msg_BringRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("请求带入返回", ack.ToString());
        // GamePanel_bj.ShowDairuUI(false);
    }

    /// <summary>
    /// 站起
    /// </summary>
    public void Send_SitUpRq()
    {
        //Msg_SitUpRq
        Msg_SitUpRq req = MessagePool.Instance.Fetch<Msg_SitUpRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SitUpRq, req);
    }

    public void Function_SitUpRs(MessageRecvData data)//推送玩家 下局结束站起2000218
    {
        //Msg_SitUpRs
        Msg_SitUpRs ack = Msg_SitUpRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("下局结束站起 2000218", ack.ToString());
        GF.UI.ShowToast("牌局结束站起");
    }

    public void Function_SynSitUp(MessageRecvData data)
    {
        //Msg_SynSitUp
        Msg_SynSitUp ack = Msg_SynSitUp.Parser.ParseFrom(data.Data);
        Seat_bj player = GamePanel_bj.seatMgr.GetPlayerByPos(ack.Pos);
        if (player == null) return;
        GF.LogInfo("收到消息站起", player.playerInfo.ToString());
        GamePanel_bj.seatMgr.Function_SynSitUp(ack.Pos);
    }

    public void Function_ForbidChatRs(MessageRecvData data)////推送玩家 下局结束站起2000218
    {
        //Msg_ForbidChatRs
        Msg_ForbidChatRs ack = Msg_ForbidChatRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("禁言", ack.ToString());
        GamePanel_bj.seatMgr.Function_ForbidChatRs(ack.PlayerId, ack.Type);
    }

    public void Function_SynSendGift(MessageRecvData data)//
    {
        //Msg_SynSendGift
        Msg_SynSendGift ack = Msg_SynSendGift.Parser.ParseFrom(data.Data);
        if(GamePanel_bj == null) return;
        GamePanel_bj.seatMgr.Function_SynSendGift(ack.Sender.PlayerId, ack.ToPlayer, ack.Gift.ToString());
    }

    /// <summary>
    /// 取消留座离桌5001015
    /// </summary>
    public void Send_CancelLeaveRq()
    {
        Msg_CancelLeaveRq req = MessagePool.Instance.Fetch<Msg_CancelLeaveRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_CancelLeaveRq, req);
    }
    public void Function_CancelLeaveRs(MessageRecvData data)
    {
        //Msg_SynSendGift
        GF.LogInfo("取消留座离桌");
        GamePanel_bj.ShowBtnCancelLiuZuo(false);
    }

    public void Function_SynPlayerState(MessageRecvData data)//
    {
        //Msg_SynSendGift
        Msg_SynPlayerState ack = Msg_SynPlayerState.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息玩家状态改变", ack.ToString());
        GamePanel_bj.seatMgr.Function_SynPlayerState(ack);
        //是用户自己
        if (Util.IsMySelf(ack.PlayerId))
        {
            GamePanel_bj.ShowBtnCancelLiuZuo(ack.State == PlayerState.OffLine);
        }
    }

    /// <summary>
    /// 房间暂停5001016
    /// </summary>

    public void Send_PauseDeskRq(int state)
    {
        Msg_PauseDeskRq req = MessagePool.Instance.Fetch<Msg_PauseDeskRq>();
        req.State = state;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_PauseDeskRq, req);
    }
    public void Function_PauseDeskRs(MessageRecvData data)
    {
        Msg_PauseDeskRs ack = Msg_PauseDeskRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("房间暂停返回", ack.ToString());
        
    }

    public void Function_SynDeskState(MessageRecvData data)//
    {
        //Msg_SynSendGift
        Msg_SynDeskState ack = Msg_SynDeskState.Parser.ParseFrom(data.Data);
        GF.LogInfo("房间暂停通知", ack.ToString());
        GamePanel_bj.SetDeskState(ack.State);
    }

    /// <summary>
    /// 房间暂停5001016
    /// </summary>

    public void Send_DeskContinuedTimeRq(int continuedTime)
    {
        Msg_DeskContinuedTimeRq req = MessagePool.Instance.Fetch<Msg_DeskContinuedTimeRq>();
        req.ContinuedTime = continuedTime;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_DeskContinuedTimeRq, req);
    }
    public void Function_DeskContinuedTimeRs(MessageRecvData data)
    {
        Msg_DeskContinuedTimeRs ack = Msg_DeskContinuedTimeRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("5001017房间续时", ack.ToString());
        GF.UI.ShowToast("房间续时成功");
        // nnGamePanel.remaintime += ack.ContinuedTime * 60 * 1000;
    }

    public void Function_SynNotifyDeskLessTime(MessageRecvData data)//
    {
        GF.UI.ShowToast("5分钟后将解散房间");
    }

    public void Function_SingleGameRecord(MessageRecvData data)
    {
        GF.LogInfo("收到单局战绩");
        GamePanel_bj.ClearTable();
        brecord = true;
    }

    #endregion

    /// <summary>
    /// 结算
    /// </summary>
    public void Function_Settle(MessageRecvData data)
    {
        //Syn_CompareChickenSettle
        Syn_CompareChickenSettle ack = Syn_CompareChickenSettle.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息结算" , ack.ToString());
        GamePanel_bj.Function_SettleAni(ack.Settle.ToArray(),ack.FirstOpen);
    }

    public void Function_CompareChickenState(MessageRecvData data)
    {
        Syn_CompareChickenState ack = Syn_CompareChickenState.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息切换牌桌状态", ack.ToString());
        enterBJDeskRs.CkState = ack.CkState;
        enterBJDeskRs.NextTime = ack.NextTime;
        GamePanel_bj.SwitchChickenState();
    }

    public void Function_AutoCompareCardRs(MessageRecvData data)
    {
        Msg_AutoCompareCardRs ack = Msg_AutoCompareCardRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息自动配牌", ack.ToString());
        GamePanel_bj.AutoCompareCardRs(ack.ComposeCard.ToArray());
    }

    public void Function_CompareCardRs(MessageRecvData data)
    {
        Msg_CompareCardRs ack = Msg_CompareCardRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息配牌完成返回", ack.ToString());
        GamePanel_bj.CompareCardRs();
    }

    public void Function_SynCompareCard(MessageRecvData data)
    {
        Msg_SynCompareCard ack = Msg_SynCompareCard.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息其他玩家配牌完成通知", ack.ToString());
        GamePanel_bj.SynCompareCard(ack.PlayerId);
    }

    public void Function_CompareCardGiveRs(MessageRecvData data)
    {
        Msg_CompareCardGiveRs ack = Msg_CompareCardGiveRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息玩家弃牌", ack.ToString());
        GamePanel_bj.CompareCardGiveRs(ack);
    }

    public void Function_SynPlayerGiveCard(MessageRecvData data)
    {
        Msg_SynPlayerGiveCard ack = Msg_SynPlayerGiveCard.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息玩家投降", ack.ToString());
        GamePanel_bj.SynPlayerGiveCard(ack.GivePlayer);
    }

    //进入比鸡桌子
    public void Function_EnterCompareChickenDeskRs(MessageRecvData data)
    {
        enterBJDeskRs = Msg_EnterCompareChickenDeskRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("刷新比鸡桌子信息", enterBJDeskRs.ToString());
        if (GamePanel_bj != null)
            CoroutineRunner.Instance.StartCoroutine(GamePanel_bj.InitPanel(false));
        // 关闭游戏状态同步的等待框
        Util.GetInstance().CloseWaiting("GameStateSync");
    }

    public Msg_SynCKJackPot msg_SynCKJackPot;

    public void Function_SynCKJackPot(MessageRecvData data)
    {
        Msg_SynCKJackPot ack = Msg_SynCKJackPot.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息奖池得奖通知", ack.ToString());
        msg_SynCKJackPot = ack;
    }

    public void Function_getSingleMatchRs(MessageRecvData data)
    {
        Msg_getSingleMatchRs ack = Msg_getSingleMatchRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息赛制信息", ack.ToString());
        if(ack.Matchs.MatchId == enterBJDeskRs.BaseInfo.BaseConfig.MatchId){
            MatchInfo = ack.Matchs;
            GamePanel_bj.GetSingleMatchRs(MatchInfo);
        }
    }

    public void Function_SynMatchLose(MessageRecvData data)
    {
        Msg_SynMatchLose ack = Msg_SynMatchLose.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息比赛淘汰通知", ack.ToString());
        GamePanel_bj.SynMatchLose(ack);
    }

}

using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using NetMsg;
using System.Linq;
using UnityGameFramework.Runtime;
using System.Collections.Generic;
using DG.Tweening;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class ZJHProcedure : ProcedureBase
{
    public GamePanel_zjh GamePanel_zjh { get; private set; }
    private IFsm<IProcedureManager> procedure;
    public int deskID;
    public Msg_EnterZjhDeskRs enterZjhDeskRs;

    public int IsGame { get; set; } = 0; //0:准备中 1:游戏中 2:结算中
    public long PreOperateGuid { get; set; } = 0;
    public int[] showCardsData = new int[3] { -1, -1, -1 };
    public bool brecord = false;
    
    public Msg_SynCKJackPot msg_SynCKJackPot;

    // 比赛信息
    public Msg_Match MatchInfo { get; private set; }

    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        this.procedure = procedureOwner;
        CoroutineRunner.Instance.RunCoroutine(GFBuiltin.Instance.SwitchToPortrait());


        enterZjhDeskRs = Msg_EnterZjhDeskRs.Parser.ParseFrom(procedure.GetData<VarByteArray>("enterZjhDeskRs"));
        GF.LogInfo("炸金花桌子信息", enterZjhDeskRs.ToString());
        deskID = enterZjhDeskRs.DeskId;
        Util.GetMyselfInfo().DeskId = deskID;
        //显示界面
        ShowZJHGamePanel();
    }

    public void AddListener()
    {
        //网络监听事件
        GF.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
        GF.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
        GF.Event.Subscribe(GFEventArgs.EventId, OnGFEventChanged);
        GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
        GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);

        HotfixNetworkComponent.AddListener(MessageID.Msg_LeaveDeskRs, Function_LeaveDeskRs);
        HotfixNetworkComponent.AddListener(MessageID.Syn_DeskPlayerInfo, Function_DeskPlayerInfoRs);
        HotfixNetworkComponent.AddListener(MessageID.Syn_DeskPlayerInfo1, Function_DeskPlayerInfoRs1);
        HotfixNetworkComponent.AddListener(MessageID.Syn_DeskPlayerInfo2, Function_DeskPlayerInfoRs2);
        HotfixNetworkComponent.AddListener(MessageID.Msg_BringRs, Function_BringRs);
        HotfixNetworkComponent.AddListener(MessageID.Syn_ChangeOperation, Function_DeskUpdate);
        HotfixNetworkComponent.AddListener(MessageID.MsgDealCard, Function_DealCard);

        HotfixNetworkComponent.AddListener(MessageID.Syn_OptionInfo, Function_Opreate);
        HotfixNetworkComponent.AddListener(MessageID.Syn_LookCard, Function_LookCard);
        HotfixNetworkComponent.AddListener(MessageID.Syn_CompareCard, Function_CompareCard);
        HotfixNetworkComponent.AddListener(MessageID.Syn_ZjhPotChange, Function_ZjhPotChange);

        HotfixNetworkComponent.AddListener(MessageID.Syn_ZjhSettle, Function_Settle);
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
        HotfixNetworkComponent.AddListener(MessageID.Msg_EnterZjhDeskRs, Function_EnterZjhDeskRs);
        
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynCKJackPot, Function_SynCKJackPot);

        // 比赛相关消息
        HotfixNetworkComponent.AddListener(MessageID.Msg_getSingleMatchRs, Function_GetSingleMatchRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynMatchLose, Function_SynMatchLose);
    }



    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        GlobalManager.GetInstance().ReSetUI();
		Util.GetMyselfInfo().DeskId = 0;
        GF.Event.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
        GF.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
        GF.Event.Unsubscribe(GFEventArgs.EventId, OnGFEventChanged);
        GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
        GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);

        //网络监听事件
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_LeaveDeskRs, Function_LeaveDeskRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_DeskPlayerInfo, Function_DeskPlayerInfoRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_DeskPlayerInfo1, Function_DeskPlayerInfoRs1);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_DeskPlayerInfo2, Function_DeskPlayerInfoRs2);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_BringRs, Function_BringRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_ChangeOperation, Function_DeskUpdate);
        HotfixNetworkComponent.RemoveListener(MessageID.MsgDealCard, Function_DealCard);

        HotfixNetworkComponent.RemoveListener(MessageID.Syn_OptionInfo, Function_Opreate);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_LookCard, Function_LookCard);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_CompareCard, Function_CompareCard);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_ZjhPotChange, Function_ZjhPotChange);

        HotfixNetworkComponent.RemoveListener(MessageID.Syn_ZjhSettle, Function_Settle);
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

        HotfixNetworkComponent.RemoveListener(MessageID.Msg_EnterZjhDeskRs, Function_EnterZjhDeskRs);
        
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynCKJackPot, Function_SynCKJackPot);

        // 比赛相关消息
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_getSingleMatchRs, Function_GetSingleMatchRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynMatchLose, Function_SynMatchLose);

        base.OnLeave(procedureOwner, isShutdown);
    }
    private void OnOpenUIFormSuccess(object sender, GameEventArgs e)
    {
        var args = e as OpenUIFormSuccessEventArgs;
        if (args.UIForm.Logic.GetType() == typeof(GamePanel_zjh))
        {
            GamePanel_zjh = args.UIForm.Logic as GamePanel_zjh;
            //重连或者中途进入
        }
    }

    private void OnGFEventChanged(object sender, GameEventArgs e)
    {
        if (GamePanel_zjh != null)
            GamePanel_zjh.OnGFEventChanged(sender, e);
    }

    public void OnUserDataChanged(object sender, GameEventArgs e)
    {
        if (GamePanel_zjh != null)
            GamePanel_zjh.OnUserDataChanged(sender, e);
    }

    private void OnClubDataChanged(object sender, GameEventArgs e)
    {
        if (GamePanel_zjh != null)
            GamePanel_zjh.OnClubDataChanged(sender, e);
    }

    private void OnShowEntitySuccess(object sender, GameEventArgs e)
    {
        var args = e as ShowEntitySuccessEventArgs;
    }
    public async void ShowZJHGamePanel()
    {
        GF.LogInfo("打开炸金花主游戏界面");
        var gamePanel = await GF.UI.OpenUIFormAwait(UIViews.ZJH_GamePanel);
        if (gamePanel != null)
        {
            GamePanel_zjh = gamePanel as GamePanel_zjh;
            brecord = false;
            AddListener();
            GlobalManager.SendSystemConfigRq();
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
        GF.LogInfo("退出炸金花流程，跳转大厅流程");
        if (enterZjhDeskRs.BaseConfig.DeskType == DeskType.Match && MatchInfo?.MatchId > 0)
        {
            procedure.SetData<VarInt64>("matchID", MatchInfo.MatchId);
        }
        ChangeState<HomeProcedure>(procedure);
    }

    public void UpdateShowCardsData(int pos, int showCard)
    {
        if (showCardsData.Contains(showCard))
        {
            showCardsData[pos] = -1;
        }
        else
        {
            showCardsData[pos] = showCard;
        }
    }

    public DeskPlayer GetSelfPlayer()
    {
        Seat_zjh player_Zjh = GamePanel_zjh?.seatMgr?.GetSelfSeat();
        return player_Zjh?.playerInfo ?? null;
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
        CoroutineRunner.Instance.StartCoroutine(GamePanel_zjh.Function_DeskPlayerInfoRs(ack.Player, true));
    }

    public void Function_DeskPlayerInfoRs1(MessageRecvData data)
    {
        Syn_DeskPlayerInfo ack = Syn_DeskPlayerInfo.Parser.ParseFrom(data.Data);
        GF.LogInfo("玩家买入", ack.ToString());
        if (Util.IsMySelf(ack.Player.BasePlayer.PlayerId) && GamePanel_zjh.varLocalScore.activeSelf)
        {
        }
        else
        {
            GamePanel_zjh.seatMgr.PlayerEnter(ack.Player);
        }
        if (Util.IsMySelf(ack.Player.BasePlayer.PlayerId))
        {
            if (enterZjhDeskRs.DeskState == DeskState.WaitStart || IsGame == 0)
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
        GamePanel_zjh.seatMgr.UpdatePlayerInfo(ack.Player, IsGame != 2);
        Seat_zjh player = GamePanel_zjh.seatMgr.GetPlayerByPlayerID(ack.Player.BasePlayer.PlayerId);
        if (player == null) return;
        player.Function_SynPlayerState(player.playerInfo.State, player.playerInfo.LeaveTime);
        if (Util.IsMySelf(ack.Player.BasePlayer.PlayerId))
        {
            GamePanel_zjh.UpdateRoundInfo(enterZjhDeskRs.Round);
            GamePanel_zjh.UpdateAddTimePrice();
            if (IsGame != 2)
            {
                GamePanel_zjh.SetLocalScore(float.Parse(ack.Player.Coin));
            }
        }
    }

    /// <summary>
    /// 发牌
    /// </summary>
    public void Function_DealCard(MessageRecvData data)
    {
        Msg_DealCard ack = Msg_DealCard.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息发牌", ack.ToString());
        GamePanel_zjh.Syn_DealCard(ack.Deal.ToList());
    }

    public List<long> compareUsers = new();

    /// <summary>
    /// 更换操作对象
    /// </summary>
    public void Function_DeskUpdate(MessageRecvData data)
    {
        //Syn_ChangeOperation
        Syn_ChangeOperation ack = Syn_ChangeOperation.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息更换操作对象", ack.ToString());
        compareUsers = ack.CompareUsers.ToList();
        enterZjhDeskRs.Round = ack.Round;
        GamePanel_zjh.ChangeOperation(ack.CurrentOption, ack.NextTime, ack.Round, float.Parse(ack.BaseFollow));
    }

    /// <summary>
    /// 收到操作信息
    /// </summary>
    public void Function_Opreate(MessageRecvData data)
    {
        //Syn_OptionInfo
        Syn_OptionInfo ack = Syn_OptionInfo.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到操作信息", ack.ToString());
        IsGame = 1;
        //自动跟住弃牌
        if (ack.Option == ZjhOption.AutoFollow || ack.Option == ZjhOption.AutoGive)
        {
            GamePanel_zjh.SetAutoDate(ack.CurrentOption, ack.Option, ack.Param);
        }
        else if (ack.Option == ZjhOption.Show)
        {
            GamePanel_zjh.SetShowCardBools(ack.CurrentOption, ack.Param2.ToArray());
        }
        else
        {
            CoroutineRunner.Instance.RunCoroutine(GamePanel_zjh.AsynOperate(ack));
        }
    }

    /// <summary>
    /// 收到看牌操作信息
    /// </summary>
    public void Function_LookCard(MessageRecvData data)
    {
        //Syn_OptionInfo
        Syn_LookCard ack = Syn_LookCard.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到看牌操作信息", ack.ToString());
        GamePanel_zjh.ShowMyselfCards(ack.HandCards.ToArray(), ack.Type);
    }

    /// <summary>
    /// 推送比牌信息
    /// </summary>
    public void Function_CompareCard(MessageRecvData data)
    {
        //Syn_CompareCard
        Syn_CompareCard ack = Syn_CompareCard.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到比牌操作信息", ack.ToString());
       Sound.PlayEffect("zjh/pk8_jss_compare_8kp.mp3");
        Seat_zjh player = GamePanel_zjh.seatMgr.GetPlayerByPlayerID(ack.Launcher);
        Seat_zjh comparePlayer = GamePanel_zjh.seatMgr.GetPlayerByPlayerID(ack.Receiver);
        bool isWin = ack.Launcher == ack.Winner;
        if (player == null) return;
        //孤注一掷
        if (comparePlayer == null)
        {
            GamePanel_zjh.GuZhuYiZhi_BiPai(player);
        }
        else
        {
            // 直接调用新的DOTween动画方法，而不是使用协程
            GamePanel_zjh.PlayBiPaiAnimation(player, comparePlayer, isWin);
        }
    }

    public float lastPot = 0;
    public void Function_ZjhPotChange(MessageRecvData data)
    {
        //Syn_ZjhPotChange
        Syn_ZjhPotChange ack = Syn_ZjhPotChange.Parser.ParseFrom(data.Data);
        // GF.LogInfo("收到底池变化" , ack.ToString());
        lastPot = float.Parse(ack.Pot);
    }

    /// <summary>
    /// 结算
    /// </summary>
    public void Function_Settle(MessageRecvData data)
    {
        //Syn_Settle
        Syn_ZjhSettle ack = Syn_ZjhSettle.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息结算", ack.ToString());
        // 直接调用新的DOTween动画方法，而不是使用协程
        GamePanel_zjh.PlaySettleAnimation(ack.Settle.ToArray());
    }

    public void Send_OptionRq(ZjhOption option, bool param = false, Position comparePos = Position.NoSit)
    {
        Msg_OptionRq req = MessagePool.Instance.Fetch<Msg_OptionRq>();
        req.Option = option;
        if (option == ZjhOption.Compare)
        {
            req.Pos = comparePos;
        }
        if (option == ZjhOption.AutoFollow || option == ZjhOption.AutoGive)
        {
            req.Param = param ? 0 : 1;
        }
        if (option == ZjhOption.Show)
        {
            foreach (int i in showCardsData)
            {
                req.Param2.Add(i);
            }
        }
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_OptionRq, req);
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
        // GamePanel_zjh.ShowDairuUI(false);
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
        Seat_zjh player = GamePanel_zjh.seatMgr.GetPlayerByPos(ack.Pos);
        if (player == null) return;
        GF.LogInfo("收到消息站起", player.playerInfo.ToString());
        GamePanel_zjh.seatMgr.Function_SynSitUp(ack.Pos);
    }

    public void Function_ForbidChatRs(MessageRecvData data)////推送玩家 下局结束站起2000218
    {
        //Msg_ForbidChatRs
        Msg_ForbidChatRs ack = Msg_ForbidChatRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("禁言", ack.ToString());
        GamePanel_zjh.seatMgr.Function_ForbidChatRs(ack.PlayerId, ack.Type);
    }

    public void Function_SynSendGift(MessageRecvData data)//
    {
        //Msg_SynSendGift
        Msg_SynSendGift ack = Msg_SynSendGift.Parser.ParseFrom(data.Data);
        GamePanel_zjh.seatMgr.Function_SynSendGift(ack.Sender.PlayerId, ack.ToPlayer, ack.Gift.ToString());
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
        GamePanel_zjh.ShowBtnCancelLiuZuo(false);
    }

    public void Function_SynPlayerState(MessageRecvData data)//
    {
        //Msg_SynSendGift
        Msg_SynPlayerState ack = Msg_SynPlayerState.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息玩家状态改变", ack.ToString());
        GamePanel_zjh.seatMgr.Function_SynPlayerState(ack);
        //是用户自己
        if (Util.IsMySelf(ack.PlayerId))
        {
            GamePanel_zjh.ShowBtnCancelLiuZuo(ack.State == PlayerState.OffLine);
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
        GamePanel_zjh.SetDeskState(ack.State);
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
        GamePanel_zjh.ClearTable();
        brecord = true;
    }

    //进入炸金花桌子 2000401
    public void Function_EnterZjhDeskRs(MessageRecvData data)
    {
        enterZjhDeskRs = Msg_EnterZjhDeskRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("刷新金花桌子信息", enterZjhDeskRs.ToString());
        if (GamePanel_zjh != null)
        {
            // 在重新加载桌子信息前，先清除所有DOTween动画
            DOTween.Kill(GamePanel_zjh.gameObject);
            CoroutineRunner.Instance.StartCoroutine(GamePanel_zjh.InitPanel(false));
        }
        // 关闭游戏状态同步的等待框
        Util.GetInstance().CloseWaiting("GameStateSync");
    }

    public void Function_SynCKJackPot(MessageRecvData data)
    {
        Msg_SynCKJackPot ack = Msg_SynCKJackPot.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息奖池得奖通知", ack.ToString());
        msg_SynCKJackPot = ack;
    }

    // 比赛相关消息
    public void Function_GetSingleMatchRs(MessageRecvData data)
    {
        Msg_getSingleMatchRs ack = Msg_getSingleMatchRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到单局比赛结果", ack.ToString());
        MatchInfo = ack.Matchs;
        if (GamePanel_zjh != null)
        {
            GamePanel_zjh.GetSingleMatchRs(MatchInfo);
        }
    }

    public void Function_SynMatchLose(MessageRecvData data)
    {
        Msg_SynMatchLose ack = Msg_SynMatchLose.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到单局比赛失败通知", ack.ToString());
        if (GamePanel_zjh != null)
        {
            GamePanel_zjh.SynMatchLose(ack);
        }
    }

    #endregion
}

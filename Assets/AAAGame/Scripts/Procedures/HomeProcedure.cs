﻿using System;
using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using NetMsg;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class HomeProcedure : ProcedureBase
{
    IFsm<IProcedureManager> procedure;
    public long m_matchID = 0;
    private bool m_NeedAutoJumpToLastFlow = false; // 是否需要自动跳转到最后的游戏流程
    
    // 存储麻将默认配置

    protected override void OnInit(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnInit(procedureOwner);
    }
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        GF.LogInfo("HomeProcedure");
        base.OnEnter(procedureOwner);
        procedure = procedureOwner;

        GF.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);//订阅Entity打开事件, Entity显示成功时触发
        GF.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);//订阅UI打开事件, UI打开成功时触发
        HotfixNetworkComponent.AddListener(MessageID.DeskCommonInfo, FunckDeskCommonInfoRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_EnterNiuNiuDeskRs, Function_EnterNiuNiuDeskRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_EnterZjhDeskRs, Function_EnterZjhDeskRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_EnterTexasDeskRs, Function_EnterDZDeskRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_EnterCompareChickenDeskRs, Function_EnterCompareChickenDeskRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_EnterMJDeskRs, Function_EnterMJDeskRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_EnterRunFastDeskRs, Function_EnterRunFastDeskRs);
       
        // 检查是否需要自动跳转到最后的游戏流程
        if (procedureOwner.HasData("AutoJumpToLastFlow"))
        {
            m_NeedAutoJumpToLastFlow = procedureOwner.GetData<VarBoolean>("AutoJumpToLastFlow");
            procedureOwner.RemoveData("AutoJumpToLastFlow");
            GF.LogInfo($"[HomeProcedure] 需要自动跳转到最后游戏流程: {m_NeedAutoJumpToLastFlow}");
        }

        // 检查是否需要显示EventDesk
        if (procedureOwner.HasData("matchID"))
        {
            m_matchID = procedureOwner.GetData<VarInt64>("matchID");
            procedureOwner.RemoveData("matchID");
        }
        CoroutineRunner.Instance.RunCoroutine(GFBuiltin.Instance.SwitchToPortrait(
            () =>
            {
                ShowHomePanel();//显示大厅界面
            }
        ));
    }

    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
    }

    public async void ShowHomePanel(bool refresh = false)
    {
        Util.UpdateClubInfoRq();
        MessageManager.GetInstance().RequestAllMsg();
        GlobalManager.GetInstance().RequestAllMsg();
        if (Util.GetMyselfInfo().DeskId != 0)
        {
            Util.GetInstance().Send_EnterDeskRq(Util.GetMyselfInfo().DeskId);
            Util.GetMyselfInfo().DeskId = 0;
            return;
        }
        if (refresh)
        {
            GlobalManager.GetInstance().ReSetUI();
        }

        // 检查是否需要自动跳转到最后的游戏流程
        if (m_NeedAutoJumpToLastFlow)
        {
            m_NeedAutoJumpToLastFlow = false; // 重置标志

            var playerId = Util.GetMyselfInfo()?.PlayerId ?? 0;
            if (playerId > 0)
            {
                GameFlowType lastFlow = GlobalManager.GetLastGameFlow(playerId);
                GF.LogInfo($"[HomeProcedure] 检测到上次游戏流程为: {lastFlow}");

                // 根据上次的流程类型自动跳转
                if (lastFlow == GameFlowType.MahjongFlow)
                {
                    GF.LogInfo("[HomeProcedure] 自动跳转到麻将大厅流程");
                    // 延迟跳转，确保HomeProcedure的初始化完成
                    await Cysharp.Threading.Tasks.UniTask.Delay(TimeSpan.FromSeconds(0.5f));
                    ChangeState<MJHomeProcedure>(procedure);
                    return;
                }
                else
                {
                    GF.LogInfo("[HomeProcedure] 进入默认扑克大厅流程");
                    // 保存当前流程状态为扑克流程
                    GlobalManager.SaveLastGameFlow(playerId, GameFlowType.PokerFlow);
                }
            }
        }

        await GF.UI.OpenUIFormAwait(UIViews.HomePanel);
    }

    public void QuitGame(bool isClearData = true)
    {
        GF.LogInfo("退出游戏");
        if (Util.InLoginProcedure()) return;
        //断开网络链接
        if (isClearData)
        {
            HotfixNetworkManager.Ins.Disconnect();
        }
        HotfixNetworkManager.Ins.isLogin = false;
        //初始化大厅需要的模块
        BringManager.GetInstance().Clear();
        RecordManager.GetInstance().Clear();
        ChatManager.GetInstance().Clear();
        MessageManager.GetInstance().Clear();
        GlobalManager.GetInstance().Clear();
        if (GF.Procedure.CurrentProcedure is not StartProcedure)
        {
            ChangeState<StartProcedure>(procedure);
        }
    }

    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        GlobalManager.GetInstance().ReSetUI();
        GF.Event.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
        GF.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);
        HotfixNetworkComponent.RemoveListener(MessageID.DeskCommonInfo, FunckDeskCommonInfoRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_EnterNiuNiuDeskRs, Function_EnterNiuNiuDeskRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_EnterZjhDeskRs, Function_EnterZjhDeskRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_EnterTexasDeskRs, Function_EnterDZDeskRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_EnterCompareChickenDeskRs, Function_EnterCompareChickenDeskRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_EnterMJDeskRs, Function_EnterMJDeskRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_EnterRunFastDeskRs, Function_EnterRunFastDeskRs);
        base.OnLeave(procedureOwner, isShutdown);
    }


    private void OnOpenUIFormSuccess(object sender, GameEventArgs e)
    {
        var args = e as OpenUIFormSuccessEventArgs;
        GF.LogInfo(args + "=========OnOpenUIFormSuccess");
    }
    private void OnShowEntitySuccess(object sender, GameEventArgs e)
    {
        var args = e as ShowEntitySuccessEventArgs;
        GF.LogInfo(args + "=========OnShowEntitySuccess");
    }

    /// <summary>
    /// 桌子创建请求
    /// </summary>
    /// <param name="req"></param>
    public void CreatorRoom(Msg_CreateDeskRq req)
    {
        Util.GetInstance().CheckSafeCodeState2(
            success: () =>
            {
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
              (HotfixNetworkComponent.AccountClientName, MessageID.Msg_CreateDeskRq, req);
            }
        );
    }

    private void FunckDeskCommonInfoRs(MessageRecvData data)
    {
        DeskCommonInfo deskCommonInfo = DeskCommonInfo.Parser.ParseFrom(data.Data);
        GF.LogInfo("桌子创建成功返回" + deskCommonInfo);
        // 根据不同的玩法类型发送进入桌子请求
        if (Util.IsPortraitGame(deskCommonInfo.MethodType))
        {
            Util.GetInstance().Send_EnterDeskRq(deskCommonInfo.DeskId);
        }
    }

    //进入牛牛桌子 2000210
    public void Function_EnterNiuNiuDeskRs(MessageRecvData data)
    {
        // Msg_LeaveDeskRs
        GF.LogInfo("收到消息进入牛牛桌子");
        GF.StaticUI.ShowMatching(false);
        this.procedure.SetData<VarByteArray>("enterNiuNiuDeskRs", data.Data);
        ChangeState<NNProcedure>(procedure);
    }

    //进入炸金花桌子 2000401
    public void Function_EnterZjhDeskRs(MessageRecvData data)
    {
        GF.LogInfo("收到消息进入炸金花桌子");
        GF.StaticUI.ShowMatching(false);
        this.procedure.SetData<VarByteArray>("enterZjhDeskRs", data.Data);
        ChangeState<ZJHProcedure>(procedure);
    }
    //进入德州流程
    public void Function_EnterDZDeskRs(MessageRecvData data)
    {
        GF.LogInfo("收到消息进入德州桌子");
        GF.StaticUI.ShowMatching(false);
        this.procedure.SetData<VarByteArray>("enterDZDeskRs", data.Data);
        ChangeState<DZProcedure>(procedure);
    }
    //进入比鸡桌子
    public void Function_EnterCompareChickenDeskRs(MessageRecvData data)
    {
        GF.LogInfo("收到消息进入比鸡桌子");
        GF.StaticUI.ShowMatching(false);
        this.procedure.SetData<VarByteArray>("enterBJDeskRs", data.Data);
        ChangeState<BJProcedure>(procedure);
    }

    //进入麻将桌子
    public void Function_EnterMJDeskRs(MessageRecvData data)
    {
        GF.LogInfo("收到消息进入麻将桌子");
        GF.StaticUI.ShowMatching(false);
        
        // 在切换流程之前启动消息缓存，避免初始化期间丢失协议
        MJMessageCache.StartCaching();
        
        this.procedure.SetData<VarByteArray>("enterMJDeskRs", data.Data);
        ChangeState<MJGameProcedure>(procedure);
    }

    public void Function_EnterRunFastDeskRs(MessageRecvData data)
    {

        if (data == null || data.Data == null)
        {

            return;
        }
        GF.StaticUI.ShowMatching(false);
        this.procedure.SetData<VarByteArray>("enterRunFastDeskRs", data.Data);
        ChangeState<PDKProcedures>(procedure);

    }
    public void EnterMJProcedure()
    {
        ChangeState<MJHomeProcedure>(procedure);
    }

   
    
}

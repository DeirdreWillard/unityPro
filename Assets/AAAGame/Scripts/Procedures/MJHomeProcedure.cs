﻿using GameFramework.Procedure;
using GameFramework.Fsm;
using UnityGameFramework.Runtime;
using NetMsg;
using Cysharp.Threading.Tasks;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class MJHomeProcedure : ProcedureBase
{
    private IFsm<IProcedureManager> procedure;
    private MJHomePanel MJHomePanel;
    public static Msg_DefaultCreateDeskRs s_DefaultCreateDeskConfig = null;


    protected override async void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        CoroutineRunner.Instance.RunCoroutine(GFBuiltin.Instance.SwitchToLandscape(async () =>
        {
            this.procedure = procedureOwner;
            GF.LogInfo("进入麻将大厅流程!");

            // 保存用户进入麻将流程的状态
            var playerId = Util.GetMyselfInfo()?.PlayerId ?? 0;
            if (playerId > 0)
            {
                GlobalManager.SaveLastGameFlow(playerId, GameFlowType.MahjongFlow);
            }

            HotfixNetworkComponent.AddListener(MessageID.Msg_EnterMJDeskRs, Function_EnterMJDeskRs);
            HotfixNetworkComponent.AddListener(MessageID.Msg_EnterRunFastDeskRs, Function_EnterRunFastDeskRs);
            HotfixNetworkComponent.AddListener(MessageID.Msg_DefaultCreateDeskRs, Function_DefaultCreateDeskRs);
            HotfixNetworkComponent.AddListener(MessageID.DeskCommonInfo, Function_DeskCommonInfoRs);

            // 请求默认创建房间配置
            RequestDefaultCreateDeskConfig();

            //加载麻将大厅场景
            ShowMJHomePanel();
        }));
    }

    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        GlobalManager.GetInstance().ReSetUI();
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_EnterMJDeskRs, Function_EnterMJDeskRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_EnterRunFastDeskRs, Function_EnterRunFastDeskRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_DefaultCreateDeskRs, Function_DefaultCreateDeskRs);
        HotfixNetworkComponent.RemoveListener(MessageID.DeskCommonInfo, Function_DeskCommonInfoRs);

        base.OnLeave(procedureOwner, isShutdown);
    }

    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
    }

    public void Function_EnterMJDeskRs(object obj)
    {
        MessageRecvData data = obj as MessageRecvData;
        GF.StaticUI.ShowMatching(false);
        
        // 在切换流程之前启动消息缓存，避免初始化期间丢失协议
        MJMessageCache.StartCaching();
        
        this.procedure.SetData<VarByteArray>("enterMJDeskRs", data.Data);
        ChangeState<MJGameProcedure>(procedure);
    }

    /// <summary>
    /// 处理进入跑的快桌子的响应消息
    /// </summary>
    /// <param name="obj">服务器返回的进入跑的快桌子消息数据</param>
    public void Function_EnterRunFastDeskRs(object obj)
    {
        MessageRecvData data = obj as MessageRecvData;
        GF.StaticUI.ShowMatching(false);
        this.procedure.SetData<VarByteArray>("enterRunFastDeskRs", data.Data);
        ChangeState<PDKProcedures>(procedure);
    }

    /// <summary>
    /// 桌子创建成功返回
    /// </summary>
    /// <param name="obj"></param>
    private void Function_DeskCommonInfoRs(object obj)
    {
        MessageRecvData data = obj as MessageRecvData;
        DeskCommonInfo deskCommonInfo = DeskCommonInfo.Parser.ParseFrom(data.Data);
        // GF.LogInfo_wl("桌子创建成功返回" + deskCommonInfo);
        // 牛牛、炸金花、德州扑克、比鸡桌子不处理
        if (Util.IsPortraitGame(deskCommonInfo.MethodType))
        {
            return;
        }
        // 仅处理个人麻将桌子
        if (deskCommonInfo.DeskType != DeskType.Simple)
        {
            return;
        }
        Util.GetInstance().Send_EnterDeskRq(deskCommonInfo.DeskId);
    }

    public async void ShowMJHomePanel(bool refresh = true)
    {
        // 【优化】提前检测是否需要返回亲友圈，避免界面闪烁
        long savedLeagueId = GlobalManager.GetInstance().PeekBeforeEnterDeskLeagueId();
        bool shouldReopenClubRoom = false;
        
        if (savedLeagueId > 0)
        {
            // 查找对应的亲友圈信息
            var myJoinLeagueInfos = GlobalManager.GetInstance().MyJoinLeagueInfos;
            if (myJoinLeagueInfos != null && myJoinLeagueInfos.ContainsKey(savedLeagueId))
            {
                shouldReopenClubRoom = true;
                LeagueInfo leagueInfo = myJoinLeagueInfos[savedLeagueId];
                // 提前设置当前亲友圈信息
                GlobalManager.GetInstance().LeagueInfo = leagueInfo;
                GF.LogInfo($"[亲友圈返回] 检测到需要返回亲友圈房间，LeagueId: {savedLeagueId}，将直接打开亲友圈界面");
            }
            // 清除保存的ID
            GlobalManager.GetInstance().ClearBeforeEnterDeskLeagueId();
        }
        
        // 异步打开游戏界面，自动显示等待框
        var uiParams = UIParams.Create();
        var gamePanel = await GF.UI.OpenUIFormAwait(UIViews.MJHomePanel, uiParams);
        if (gamePanel != null)
        {
            MJHomePanel = gamePanel as MJHomePanel;
            // 大厅打开时后台预加载麻将资源，提升进入游戏的流畅度
            if (!MahjongResourceManager.IsInitialized())
            {
                MahjongResourceManager.PreloadAllInBackground();
            }
            
            // 如果需要返回亲友圈，立即打开（减少闪烁）
            if (shouldReopenClubRoom)
            {
                // 立即打开亲友圈房间界面，无需等待
                await GF.UI.OpenUIFormAwait(UIViews.CreatMJRoom);
                GF.LogInfo($"[亲友圈返回] 已自动打开亲友圈房间界面");
            }
        }
    }

    public void ExitMJHome()
    {
        GF.LogInfo("退出麻将大厅流程，跳转扑克大厅流程");

        // 保存用户返回扑克流程的状态
        var playerId = Util.GetMyselfInfo()?.PlayerId ?? 0;
        if (playerId > 0)
        {
            GlobalManager.SaveLastGameFlow(playerId, GameFlowType.PokerFlow);
        }

        ChangeState<HomeProcedure>(procedure);
    }

    /// <summary>
    /// 请求默认创建房间配置
    /// </summary>
    private void RequestDefaultCreateDeskConfig()
    {
        // 如果没有配置，则请求服务器
        Msg_DefaultCreateDeskRq req = MessagePool.Instance.Fetch<Msg_DefaultCreateDeskRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(
            HotfixNetworkComponent.AccountClientName,
            MessageID.Msg_DefaultCreateDeskRq,
            req
        );
    }

    /// <summary>
    /// 处理默认创建房间配置响应
    /// </summary>
    private void Function_DefaultCreateDeskRs(MessageRecvData data)
    {
        Msg_DefaultCreateDeskRs msg_DefaultCreateDeskRs = Msg_DefaultCreateDeskRs.Parser.ParseFrom(data.Data);
        GF.LogInfo_wl("[MJHomeProcedure] 收到默认房间配置: " + msg_DefaultCreateDeskRs.ToString());
        // 直接存储到HomeProcedure的字段
        s_DefaultCreateDeskConfig = msg_DefaultCreateDeskRs;
    }

    /// <summary>
    /// 创建个人房间
    /// </summary>
    /// <param name="req">创建房间请求</param>
    public static void CreatMJRoom(Msg_CreateDeskRq req)
    {
        if (req == null) return;
        
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(
            HotfixNetworkComponent.AccountClientName,
            MessageID.Msg_CreateDeskRq,
            req
        );
    }
}

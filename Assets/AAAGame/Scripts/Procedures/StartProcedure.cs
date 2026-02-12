using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityEngine;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class StartProcedure : ProcedureBase
{
    public StartPanel startPanel;
    IFsm<IProcedureManager> procedure;
    protected override void OnInit(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnInit(procedureOwner);
    }
    public bool isInit = false;
    protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        procedure = procedureOwner;
        CoroutineRunner.Instance.RunCoroutine(GFBuiltin.Instance.SwitchToPortrait());

        GF.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);//订阅Entity打开事件, Entity显示成功时触发
        GF.Event.Subscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);//订阅UI打开事件, UI打开成功时触发

        ShowStartPanel();//显示登陆界面

        //初始化网络
        HotfixNetworkManager.Ins.Init();
        if (!isInit)
        {
            //初始化大厅需要的模块
            RecordManager.GetInstance().Init();
            ChatManager.GetInstance().Init();
            MessageManager.GetInstance().Init();
            BringManager.GetInstance().Init();
            GlobalManager.GetInstance().Init();
            isInit = true;
        }
        PlayerPrefs.SetInt("FirstEnterHome", 0);
    }

    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        GlobalManager.GetInstance().ReSetUI();
        GF.Event.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
        GF.Event.Unsubscribe(OpenUIFormSuccessEventArgs.EventId, OnOpenUIFormSuccess);

        base.OnLeave(procedureOwner, isShutdown);
    }
    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        if (startPanel == null)
        {
            return;
        }
    }
    public async void ShowStartPanel()
    {
        var uiParms = UIParams.Create();
        var startPanel = await GF.UI.OpenUIFormAwait(UIViews.StartPanel, uiParms);
        if (startPanel != null)
        {
            this.startPanel = startPanel as StartPanel;
            OnLevelAllReady();
        }
    }

    public void EnterHome()
    {
        GF.LogInfo("登录成功跳转大厅流程");
        
        // 标记需要自动跳转到最后的游戏流程
        procedure.SetData<VarBoolean>("AutoJumpToLastFlow", true);
        
        ChangeState<HomeProcedure>(procedure);
    }

    private void OnLevelAllReady()
    {
        GF.BuiltinView.HideLoadingProgress();
        GF.StaticUI.ShowMatching(false);
    }

    private void OnOpenUIFormSuccess(object sender, GameEventArgs e)
    {
        var args = e as OpenUIFormSuccessEventArgs;
        GF.LogInfo(args.UIForm.UIFormAssetName + "=========OnOpenUIFormSuccess");
    }
    private void OnShowEntitySuccess(object sender, GameEventArgs e)
    {
        var args = e as ShowEntitySuccessEventArgs;
        GF.LogInfo(args + "=========OnShowEntitySuccess");

    }
}

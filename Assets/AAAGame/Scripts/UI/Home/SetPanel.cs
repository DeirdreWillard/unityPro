using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using GameFramework.Event;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class SetPanel : UIFormBase
{

    public Text TextVersion;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);

        SetUI();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
        base.OnClose(isShutdown, userData);
    }

    public void SetUI(){
        varSafeCodeOn.gameObject.SetActive(GF.DataModel.GetDataModel<UserDataModel>().LockState != 0);
        varSafeCodeOff.gameObject.SetActive(GF.DataModel.GetDataModel<UserDataModel>().LockState == 0);
        varSafeCodeOff.gameObject.SetActive(GF.DataModel.GetDataModel<UserDataModel>().LockState == 0);
        varSafeCodeOff.gameObject.SetActive(GF.DataModel.GetDataModel<UserDataModel>().LockState == 0);
#if !UNITY_EDITOR
        TextVersion.text = "Version: " + GameFramework.Version.GameVersion + "(" + GFBuiltin.Resource.InternalResourceVersion + ")";
#endif
        InitSound();
    }

    protected override async void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        var uiParams = UIParams.Create();
        switch (btId)
        {
            case "新手引导":
               
                break;
            case "修改密码":
                uiParams.Set<VarString>("TextTitle", "修改登录密码");
                await GF.UI.OpenUIFormAwait(UIViews.PasswordDialog, uiParams);
                break;
            case "修改钱包密码":
                uiParams.Set<VarString>("TextTitle", "修改钱包密码");
                await GF.UI.OpenUIFormAwait(UIViews.PasswordDialog, uiParams);
                break;
            case "官方网站":
                GUIUtility.systemCopyBuffer = "xingyunxing999.xin";
                GF.UI.ShowToast("复制成功!");
                break;
            case "退出游戏":
                Util.GetInstance().OpenConfirmationDialog("退出登录", "确定要返回登录界面吗?", () =>
                {
                    HomeProcedure homeProcedure = GF.Procedure.CurrentProcedure as HomeProcedure;
                    homeProcedure.QuitGame();
                });
                break;
            case "SoundOn":
                varSoundOn.gameObject.SetActive(false);
                varSoundOff.gameObject.SetActive(true);
                Sound.SetSfxEnabled(false);
                break;
            case "SoundOff":
                varSoundOn.gameObject.SetActive(true);
                varSoundOff.gameObject.SetActive(false);
                Sound.SetSfxEnabled(true);
                break;
            case "SoundBgOn":
                varSoundBgOn.gameObject.SetActive(false);
                varSoundBgOff.gameObject.SetActive(true);
                Sound.SetMusicEnabled(false);
                break;
            case "SoundBgOff":
                varSoundBgOn.gameObject.SetActive(true);
                varSoundBgOff.gameObject.SetActive(false);
                Sound.SetMusicEnabled(true);
                break;
            case "SafeCodeOn":
                await GF.UI.OpenUIFormAwait(UIViews.SecurityCodeDialog);
                break;
            case "SafeCodeOff":
                Msg_LockRq req = MessagePool.Instance.Fetch<Msg_LockRq>();
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                    (HotfixNetworkComponent.AccountClientName, MessageID.Msg_LockRq, req);
                break;
        }
    }

    public void InitSound()
    {
        bool isSfxOn = Sound.IsSfxEnabled();
        varSoundOn.gameObject.SetActive(isSfxOn);
        varSoundOff.gameObject.SetActive(!isSfxOn);
        bool isBgmOn = Sound.IsMusicEnabled();
        varSoundBgOn.gameObject.SetActive(isBgmOn);
        varSoundBgOff.gameObject.SetActive(!isBgmOn);
    }

    private void OnUserDataChanged(object sender, GameEventArgs e)
    {
        var args = e as UserDataChangedEventArgs;
        switch (args.Type)
        {
            case UserDataType.SafeCodeState:
                SetUI();
                GF.LogInfo("收到 安全码解锁事件");
                break;
            case UserDataType.MONEY:
                SetUI();
                GF.LogInfo("收到 安全码解锁事件");
                break;
        }
    }
}

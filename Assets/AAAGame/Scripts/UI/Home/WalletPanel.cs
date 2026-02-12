using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using GameFramework.Event;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class WalletPanel : UIFormBase
{
    UserDataModel userDataModel;
    public AvatarInfo avatarinfo;
    public Text textPlayerId;
    public Text textCoin;
    public Text textDiamond;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        userDataModel = Util.GetMyselfInfo();

        textPlayerId.text = userDataModel.PlayerId.ToString();
        avatarinfo.Init(userDataModel);
        textCoin.text = userDataModel.Gold;
        textDiamond.text = userDataModel.Diamonds.ToString();
        HotfixNetworkComponent.AddListener(MessageID.Msg_LockRs, FunckMsg_LockRsRs);
        GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
        Util.GetInstance().CheckSafeCodeState(gameObject);

    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_LockRs, FunckMsg_LockRsRs);
        GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
        base.OnClose(isShutdown, userData);
    }

    private void FunckMsg_LockRsRs(MessageRecvData data)
    {
        GF.LogInfo("上锁返回");
        Msg_LockRs ack = Msg_LockRs.Parser.ParseFrom(data.Data);
        GF.DataModel.GetDataModel<UserDataModel>().LockState = 1;
        GF.UI.Close(this.UIForm);
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "明细":
                OnClickBtnDetail();
                break;
            case "上锁":
                OnClickBtnLock();
                break;
            case "兑换金币":
                OnClickBtnCoin();
                break;
            case "兑换钻石":
                OnClickBtnDiamond();
                break;
        }
    }

    private async void OnClickBtnDetail()
    {
        var uiParams = UIParams.Create();
        uiParams.Set<VarInt64>("playerID", Util.GetMyselfInfo().PlayerId);
        uiParams.Set<VarInt32>("coinType", 3);
        await GF.UI.OpenUIFormAwait(UIViews.XXXDetailsPanel, uiParams);
    }

    private void OnClickBtnLock()
    {
        Msg_LockRq req = MessagePool.Instance.Fetch<Msg_LockRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_LockRq, req);
    }

    private async void OnClickBtnCoin()
    {
        await GF.UI.OpenUIFormAwait(UIViews.ShopPanel);
    }

    private void OnClickBtnDiamond()
    {
        Application.OpenURL($"{Const.HttpStr}{GlobalManager.ServerInfo_HouTaiIP}/api/pay/" + Util.GetMyselfInfo().PlayerId);
    }

    private void OnUserDataChanged(object sender, GameEventArgs e)
    {
        var args = e as UserDataChangedEventArgs;
        switch (args.Type)
        {
            case UserDataType.MONEY:
                textCoin.text = GF.DataModel.GetDataModel<UserDataModel>().Gold.ToString();
                break;
            case UserDataType.DIAMOND:
                textDiamond.text = GF.DataModel.GetDataModel<UserDataModel>().Diamonds.ToString();
                break;
            case UserDataType.VipState:
                avatarinfo.Init(userDataModel);
                break;
        }
    }
}

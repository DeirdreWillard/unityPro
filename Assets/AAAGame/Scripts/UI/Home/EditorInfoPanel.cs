using UnityEngine.UI;
using NetMsg;


[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class EditorInfoPanel : UIFormBase
{
    public Text TextCostDiamond1;
    public Text TextCostDiamond2;
    public Text TextDiamond;

    public InputField inputName;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.ModifyUserAck, OnifyUserAck);
        inputName.text = Util.GetMyselfInfo().NickName;
        Util.DownloadHeadImage(varAvatar, Util.GetMyselfInfo().HeadIndex);
        OnFunckSystemConfigRs();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.ModifyUserAck, OnifyUserAck);
        base.OnClose(isShutdown, userData);
    }

    private void OnFunckSystemConfigRs()
    {
        TextCostDiamond1.text = Util.GetMyselfInfo().ModifyHeadNum > 0 ? GlobalManager.GetConstants(10).ToString() : "首次免费";
        TextCostDiamond2.text = Util.GetMyselfInfo().ModifyNickNum > 0 ? GlobalManager.GetConstants(11).ToString() : "首次免费";
        TextDiamond.text = Util.GetMyselfInfo().Diamonds.ToString();
    }

    public void OnifyUserAck(MessageRecvData data)
    {   
        Msg_ModifyUserRs ack = Msg_ModifyUserRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到 改名回包 :" , ack.ToString());
        Util.GetMyselfInfo().NickName = ack.Nick;
        Util.GetMyselfInfo().HeadIndex = ack.HeadImage;
        Util.GetMyselfInfo().ModifyNickNum = ack.ModifyNickNum;
        Util.GetMyselfInfo().ModifyHeadNum = ack.ModifyHeadNum;
        GF.UI.ShowToast("修改成功!");
        GF.UI.Close(this.UIForm);
    }
    
    protected override async void OnButtonClick(object sender, string btId)
    {
       base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "ButOk":
                if (string.IsNullOrEmpty(inputName.text))
                {
                    GF.UI.ShowToast("昵称不能为空!");
                    return;
                }
                if (inputName.text == Util.GetMyselfInfo().NickName)
                {
                    GF.UI.ShowToast("没有改动!");
                    return;
                }
                if (!Util.IsValidName(inputName.text))
                {
                    inputName.text = "";
                    GF.UI.ShowToast("昵称含有特殊字符,请重新输入!");
                    return;
                }
                Util.GetInstance().OpenConfirmationDialog("改名", $"确定要改名成:{inputName.text}", () =>
                {
                    GF.LogInfo("请求 改名 :" , inputName.text);
                    Msg_ModifyUserRq req = MessagePool.Instance.Fetch<Msg_ModifyUserRq>();
                    req.Nick = inputName.text;
                    HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.ModifyUserReq, req);
                });
                break;
            case "UpdateAvatar":
                await GF.UI.OpenUIFormAwait(UIViews.UpdateHeadPanel);
                break;
        }
    }
    
}

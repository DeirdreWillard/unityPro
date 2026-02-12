using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using UnityGameFramework.Runtime;
using Google.Protobuf;


[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class ResetClubPanel : UIFormBase
{
    public InputField clubNameInput;
    public InputField clubContentInput;

    public Text TextCostDiamond1;
    public Text TextCostDiamond2;
    public Text TextDiamond;

    public RawImage varAvatar;
    bool isClickHead = false;
    private LeagueInfo leagueInfo;
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.Msg_ModifyLeagueRs, OnMsg_ModifyLeagueRs);
        leagueInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("leagueInfo"));
        clubNameInput.text = leagueInfo.LeagueName;
        clubContentInput.text = leagueInfo.Introduction;
        Util.DownloadHeadImage(varAvatar, leagueInfo.LeagueHead, leagueInfo.Type);
        OnFunckSystemConfigRs();
        Util.GetInstance().CheckSafeCodeState(gameObject);
        isClickHead = false;
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_ModifyLeagueRs, OnMsg_ModifyLeagueRs);
        base.OnClose(isShutdown, userData);
    }

    private void OnFunckSystemConfigRs()
    {
        TextCostDiamond1.text = GlobalManager.GetInstance().LeagueInfo.ModifyNameNum > 0 ? GlobalManager.GetConstants(13).ToString() : "首次免费";
        TextCostDiamond2.text = GlobalManager.GetInstance().LeagueInfo.ModifyHeadNum > 0 ? GlobalManager.GetConstants(12).ToString() : "首次免费";
        TextDiamond.text = Util.GetMyselfInfo().Diamonds.ToString();
    }

    public void BtnSure()
    {
        GF.LogInfo("请求 改名 ");
        Msg_ModifyLeagueRq req = MessagePool.Instance.Fetch<Msg_ModifyLeagueRq>();
        req.LeagueName = clubNameInput.text;
        req.Introduction = clubContentInput.text;
        req.ClubId = leagueInfo.LeagueId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_ModifyLeagueRq, req);
    }
    public void OnMsg_ModifyLeagueRs(MessageRecvData data)
    {
        Msg_ModifyLeagueRs ack = Msg_ModifyLeagueRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_ModifyLeagueRs收到 改名回包 " , ack.ToString());
        Util.UpdateClubInfoRq();
        GF.UI.ShowToast("修改成功");
        GF.UI.Close(this.UIForm);
    }

        protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "ButOk":
                if (string.IsNullOrEmpty(clubNameInput.text))
                {
                    GF.UI.ShowToast("俱乐部名称不能为空!");
                    return;
                }
                if (clubNameInput.text == leagueInfo.LeagueName && clubContentInput.text == leagueInfo.Introduction)
                {
                    GF.UI.ShowToast("没有改动!");
                    return;
                }
                if (!Util.IsValidName(clubNameInput.text))
                {
                    clubNameInput.text = "";
                    GF.UI.ShowToast("俱乐部名称含有特殊字符,请重新输入!");
                    return;
                }
                Msg_ModifyLeagueRq req = MessagePool.Instance.Fetch<Msg_ModifyLeagueRq>();
                if (clubNameInput.text != leagueInfo.LeagueName)
                {
                    req.LeagueName = clubNameInput.text;
                }
                if (clubContentInput.text != leagueInfo.Introduction)
                {
                    req.Introduction = clubContentInput.text;
                }
                req.ClubId = leagueInfo.LeagueId;
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_ModifyLeagueRq, req);
                break;
            case "ButOkSetHead":
                if (!isClickHead)
                {
                    GF.UI.ShowToast("没有改动!");
                    return;
                }
                Util.GetInstance().OpenConfirmationDialog("修改头像", "确定要修改头像吗?", () =>
                {
                    Msg_ModifyLeagueRq req = MessagePool.Instance.Fetch<Msg_ModifyLeagueRq>();
                    if (varAvatar.texture.name != "PlayerHead120")
                    {
                        Texture2D temp = varAvatar.texture as Texture2D;
                        if (temp == null)
                        {
                            GF.LogWarning("头像为空.");
                            return;
                        }
                        byte[] bytes = PhotoManager.GetInstance().Texture2DTobytes(temp, false, 80);
                        req.LeagueHead = ByteString.CopyFrom(bytes);
                    }
                    else
                    {
                        GF.UI.ShowToast("请选择新头像!");
                        return;
                    }
                    req.ClubId = leagueInfo.LeagueId;
                    HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_ModifyLeagueRq, req);
                });
                break;
            case "UpdateAvatar":
                isClickHead = true;
                PhotoManager.GetInstance().TakePhoto(varAvatar);
                break;
        }
    }
}

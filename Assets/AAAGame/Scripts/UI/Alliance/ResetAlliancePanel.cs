using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using UnityGameFramework.Runtime;
using Google.Protobuf;
using GameFramework;


[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class ResetAlliancePanel : UIFormBase
{
    public Text TextCostDiamond1;
    public Text TextCostDiamond2;
    public Text TextDiamond;

    bool isClickHead = false;
    private LeagueInfo leagueInfo = new();
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.Msg_ModifyLeagueRs, OnMsg_ModifyLeagueRs);
        leagueInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("leagueInfo"));
        varNameInput.text = leagueInfo.LeagueName;
        varCoinNameInput.text = leagueInfo.GoldName;
        Util.GetInstance().CheckSafeCodeState(gameObject);
        Util.DownloadHeadImage(varAvatar, leagueInfo.LeagueHead, leagueInfo.Type);
        OnFunckSystemConfigRs();
        isClickHead = false;
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_ModifyLeagueRs, OnMsg_ModifyLeagueRs);
        base.OnClose(isShutdown, userData);
    }

    private void OnFunckSystemConfigRs()
    {
        TextCostDiamond1.text = GlobalManager.GetInstance().LeagueInfo.ModifyHeadNum > 0 ? GlobalManager.GetConstants(12).ToString() : "首次免费";
        TextCostDiamond2.text = GlobalManager.GetInstance().LeagueInfo.ModifyNameNum > 0 ? GlobalManager.GetConstants(13).ToString() : "首次免费";
        TextDiamond.FormatAmount(Util.GetMyselfInfo().Diamonds);
    }

    public void OnMsg_ModifyLeagueRs(MessageRecvData data)
    {
        Msg_ModifyLeagueRs ack = Msg_ModifyLeagueRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("OnMsg_ModifyLeagueRs收到改名回包", ack.ToString());
        GlobalManager.GetInstance().LeagueInfo = ack.LeagueInfo;
        GF.Event.Fire(this, ReferencePool.Acquire<ClubDataChangedEventArgs>().Fill(ClubDataType.eve_LeagueDateUpdate, null, null));
        GF.UI.ShowToast("修改成功");
        GF.UI.Close(this.UIForm);
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "ButOk":
                if (string.IsNullOrEmpty(varNameInput.text) || string.IsNullOrEmpty(varCoinNameInput.text))
                {
                    GF.UI.ShowToast("请输入完整信息!");
                    return;
                }
                if (!Util.IsValidName(varNameInput.text))
                {
                    varNameInput.text = "";
                    GF.UI.ShowToast("联盟名字含有特殊字符,请重新输入!");
                    return;
                }
                if (!Util.IsValidName(varCoinNameInput.text))
                {
                    varCoinNameInput.text = "";
                    GF.UI.ShowToast("联盟币名称含有特殊字符,请重新输入!");
                    return;
                }
                if (varNameInput.text == leagueInfo.LeagueName && varCoinNameInput.text == leagueInfo.GoldName)
                {
                    GF.UI.ShowToast("没有改动!");
                    return;
                }
                Msg_ModifyLeagueRq req = MessagePool.Instance.Fetch<Msg_ModifyLeagueRq>();
                req.LeagueName = varNameInput.text;
                req.GoldName = varCoinNameInput.text;
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
                    if (varAvatar.texture.name != "UnionHead120")
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

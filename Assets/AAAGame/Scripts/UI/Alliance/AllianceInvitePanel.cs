
using NetMsg;
using UnityGameFramework.Runtime;
using UnityEngine.UI;
using UnityEngine;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class AllianceInvitePanel : UIFormBase
{
    LeagueInfo leagueInfo;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SearchClubRs, Function_SearchClubRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_InvitationClubRs, Function_InvitationClubRs);
        varInputFieldLegacy.onValueChanged.AddListener(delegate
        {
            if (varInputFieldLegacy.text.Length == 6)
            {
                FuncSearchClubRq(varInputFieldLegacy.text);
            }
            else{
                ShowItem(false);
            }
        });
        varInputFieldLegacy.text = "";

        leagueInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("leagueInfo"));
        InitPage();
        Util.GetInstance().CheckSafeCodeState(gameObject);
    }

    void InitPage()
    {
        ShowItem(false);
        varTxtInvite.text = leagueInfo.InviteCode;
    }

    public void ShowItem(bool isShow){
        varItem.SetActive(isShow);
        varNoFIndImg.SetActive(!isShow);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SearchClubRs, Function_SearchClubRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_InvitationClubRs, Function_InvitationClubRs);
        varInputFieldLegacy.onValueChanged.RemoveAllListeners();
        base.OnClose(isShutdown, userData);
    }

    private void Function_SearchClubRs(MessageRecvData data)
    {
        Msg_SearchClubRs ack = Msg_SearchClubRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("搜索俱乐部" , ack.ToString());
        if (ack.BaseInfo.Type == LeagueType.Club)
        {
            varItem.transform.Find("nickName").GetComponent<Text>().text = ack.BaseInfo.LeagueName;
            varItem.transform.Find("playerId").GetComponent<Text>().text = "ID: " + ack.BaseInfo.LeagueId.ToString();
            varItem.transform.Find("State").GetComponent<Text>().text = Util.GetJoinState(ack.State);
            //头像
            Util.DownloadHeadImage(varItem.transform.Find("Mask/Avatar").GetComponent<RawImage>(), ack.BaseInfo.LeagueHead, (int)ack.BaseInfo.Type);
            ShowItem(true);
        }
        else
        {
            GF.UI.ShowToast("请输入正确的俱乐部ID");
        }
    }

    private void Function_InvitationClubRs(MessageRecvData data)
    {
        Msg_SearchClubRs ack = Msg_SearchClubRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("邀请俱乐部" , ack.ToString());
        GF.UI.ShowToast("邀请成功!");
        GF.UI.Close(this.UIForm);
    }

    public void FuncSearchClubRq(string clubId)
    {
        Msg_SearchClubRq req = MessagePool.Instance.Fetch<Msg_SearchClubRq>();
        req.ClubId = long.Parse(clubId);
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SearchClubRq, req);
    }


    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "SureBtn":
                //邀请
                if (varNoFIndImg.activeSelf)
                    return;
                    
                Msg_InvitationClubRq InviteReq = MessagePool.Instance.Fetch<Msg_InvitationClubRq>();
                InviteReq.ClubId = long.Parse(varInputFieldLegacy.text);
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                    (HotfixNetworkComponent.AccountClientName, MessageID.Msg_InvitationClubRq, InviteReq);
                break;
            case "BtnCopy":
                GUIUtility.systemCopyBuffer = leagueInfo.InviteCode.ToString();
                GF.UI.ShowToast("复制成功!");
                break;
        }
    }
}

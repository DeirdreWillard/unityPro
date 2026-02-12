using UnityEngine.UI;

using NetMsg;


[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class JoinAlliance : UIFormBase
{

    public InputField InputClubName;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.Msg_JoinUnionRs, FuncJoinUnionRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SearchClubRs, Function_SearchClubRs);
        InputClubName.onValueChanged.AddListener(delegate
        {
            if (InputClubName.text.Length == 6)
            {
                FuncSearchClubRq(InputClubName.text);
            }
            else
            {
                ShowItem(false);
            }
        });
        InputClubName.text = "";
        ShowItem(false);
    }

    public void ShowItem(bool isShow)
    {
        varItem.SetActive(isShow);
        varNoFIndImg.SetActive(!isShow);
    }

    public void FuncSearchClubRq(string clubId)
    {
        long.TryParse(InputClubName.text,out long clubID);
        if (clubID == 0)
        {
            GF.UI.ShowToast("请输入正确的联盟号");
            return;
        }
        Msg_SearchClubRq req = MessagePool.Instance.Fetch<Msg_SearchClubRq>();
        req.ClubId = clubID;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SearchClubRq, req);
    }

    private void Function_SearchClubRs(MessageRecvData data)
    {
        Msg_SearchClubRs ack = Msg_SearchClubRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("搜索俱乐部" , ack.ToString());
        if (ack.BaseInfo.Type == LeagueType.Union)
        {
            varItem.transform.Find("nickName").GetComponent<Text>().text = ack.BaseInfo.LeagueName;
            varItem.transform.Find("playerId").GetComponent<Text>().text = "ID: " + ack.BaseInfo.LeagueId.ToString();
            varItem.transform.Find("State").GetComponent<Text>().text = Util.GetJoinState(ack.State);
            //头像
            Util.DownloadHeadImage(varItem.transform.Find("Mask/avatar").GetComponent<RawImage>(), ack.BaseInfo.LeagueHead,(int)ack.BaseInfo.Type);
            ShowItem(true);
        }else
        {
            GF.UI.ShowToast("获取失败");
        }
    }

    private void FuncJoinUnionRs(MessageRecvData data)
    {
        Msg_JoinUnionRs ack = Msg_JoinUnionRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_JoinUnionRs申请加入联盟" , ack.ToString());
        if (ack.Code == 0)
        {
            GF.UI.ShowToast("成功加入",1);
        }
        else if (ack.Code==1){
            GF.UI.ShowToast("等待审核",1);
        }
        GF.UI.Close(this.UIForm);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_JoinUnionRs, FuncJoinUnionRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SearchClubRs, Function_SearchClubRs);
        InputClubName.onValueChanged.RemoveAllListeners();
        base.OnClose(isShutdown, userData);
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "FindBtn":
                //搜索
                if (InputClubName.text.Length == 6)
                {
                    Msg_SearchClubRq req = MessagePool.Instance.Fetch<Msg_SearchClubRq>();
                    req.ClubId = long.Parse(InputClubName.text);
                    HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                        (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SearchClubRq, req);
                }
                break;
            case "SureBtn":
                if (varNoFIndImg.activeSelf)
                    return;

                Msg_JoinUnionRq joinUnionRq = MessagePool.Instance.Fetch<Msg_JoinUnionRq>();
                joinUnionRq.Param = InputClubName.text;
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                    (HotfixNetworkComponent.AccountClientName, MessageID.Msg_JoinUnionRq, joinUnionRq);
                break;
        }
    }


}

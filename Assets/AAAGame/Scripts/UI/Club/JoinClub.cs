using UnityEngine;
using UnityEngine.UI;

using NetMsg;


[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class JoinClub : UIFormBase
{

    public InputField InputClubName;
   
    public GameObject bg;
    public GameObject ClubItem;
    public Text clubName;
    public Text ID;
    public RawImage avatar;
    public Text state;
 
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.Msg_JoinLeagueRs, FuncMsgJoinLeagueRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SearchClubRs, FunckSearchClubRs);
        InputClubName.onValueChanged.AddListener(delegate
        {
            if (InputClubName.text.Length == 6)
            {
                FuncSearchClubRq(InputClubName.text);
            }else{
                ClubItem.SetActive(false);
                bg.SetActive(true);
            }
        });
        InputClubName.text = "";
        ClubItem.SetActive(false);
        bg.SetActive(true);

    }

    public void FuncSearchClubRq(string clubId)
    {
        Msg_SearchClubRq req = MessagePool.Instance.Fetch<Msg_SearchClubRq>();
        req.ClubId = long.Parse(clubId);
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SearchClubRq, req);
    }
    private void FuncMsgJoinLeagueRs(MessageRecvData data)
    {
        Msg_JoinLeagueRs ack = Msg_JoinLeagueRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_JoinLeagueRs申请加入俱乐部" , ack.ToString());
        if (ack.Code==0){
            GF.UI.ShowToast("成功加入",1);
            state.text = Util.GetJoinState(2);
        }
        else if (ack.Code==1){
            GF.UI.ShowToast("等待审核",1);
            state.text = Util.GetJoinState(1);
        }
        GF.UI.Close(this.UIForm);
    }
 
    private void FunckSearchClubRs(MessageRecvData data)
    {
        GF.LogInfo("搜索俱乐部");
        Msg_SearchClubRs ack = Msg_SearchClubRs.Parser.ParseFrom(data.Data);
        // Msg_ClubBaseInfo baseInfo = 1;//基础信息
        if (ack.BaseInfo.Type == LeagueType.Club)
        {
            bg.SetActive(false);
            clubName.text = ack.BaseInfo.LeagueName;
            ID.text = "ID: " + ack.BaseInfo.LeagueId.ToString();
            Util.DownloadHeadImage(avatar, ack.BaseInfo.LeagueHead, (int)ack.BaseInfo.Type);
            state.text = Util.GetJoinState(ack.State);
            ClubItem.SetActive(true);
        }else
        {
            GF.UI.ShowToast("请输入正确的俱乐部ID");
        }
    }


    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_JoinLeagueRs, FuncMsgJoinLeagueRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SearchClubRs, FunckSearchClubRs);
        InputClubName.onValueChanged.RemoveAllListeners();
        base.OnClose(isShutdown, userData);
    }

   protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "SureBtn":
                if (bg.activeSelf)
                    return;

                Msg_JoinLeagueRq req = MessagePool.Instance.Fetch<Msg_JoinLeagueRq>();
                req.InviteCode = InputClubName.text;
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                    (HotfixNetworkComponent.AccountClientName, MessageID.Msg_JoinLeagueRq, req);
                break;
        }
    }

   
    
}

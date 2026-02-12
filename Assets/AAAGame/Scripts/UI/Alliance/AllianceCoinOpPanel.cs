
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using NetMsg;


[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class AllianceCoinOpPanel : UIFormBase
{

    public InputField InputNum;
    public Text txtName;
    public Text txtId;
    public RawImage avatar;
    public Toggle toggle1;
    public Toggle toggle2;
    public Text textCoins;
    public Text textUnionCoins;
    public Button BtnSure;
    public LeagueInfo leagueInfo;
    public LeagueInfo clubInfo;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SetClubGoldRs, FunctionMsgSetClubGoldRs);

        leagueInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("leagueInfo"));
        clubInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("clubInfo"));
        txtName.text = clubInfo.LeagueName;
        txtId.text = "ID：" + clubInfo.LeagueId.ToString();
        textCoins.text = $"{Util.FormatAmount(clubInfo.LeagueGold)} ({Util.FormatAmount(clubInfo.TotalGold)})";
        textUnionCoins.text = GlobalManager.GetInstance().GetAllianceCoins(leagueInfo.LeagueId).ToString();
        Util.DownloadHeadImage(avatar, clubInfo.LeagueHead, clubInfo.Type);
        toggle1.isOn = true;
        InputNum.text = "";
        transform.Find("Panel/CoinImg/Text").GetComponent<Text>().text = clubInfo.Type == 0 ? "俱乐部当前余额：" : "联盟当前余额：";
        transform.Find("Panel/T1").GetComponent<Text>().text = clubInfo.Type == 0 ? "设置该俱乐部联盟币" : "设置该联盟的联盟币";
        transform.Find("Panel/UnionCoins/Text").GetComponent<Text>().text = leagueInfo.Type == 1 ? "联盟余额：" : "超级联盟余额：";
        Util.GetInstance().CheckSafeCodeState(gameObject);
    }

    /// <summary>
    ///  设置联盟币返回
    /// </summary>
    /// <param name="data"></param>
    private void FunctionMsgSetClubGoldRs(MessageRecvData data)
    {
        if (leagueInfo.Type == 1)
        {
            Msg_GetUnionClubListRq req = MessagePool.Instance.Fetch<Msg_GetUnionClubListRq>();
            req.UnionId = leagueInfo.LeagueId;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GetUnionClubListRq, req);
        }
        else if(leagueInfo.Type == 2)
        {
            Msg_GetSuperUnionListRq req = MessagePool.Instance.Fetch<Msg_GetSuperUnionListRq>();
            req.UnionId = leagueInfo.LeagueId;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GetSuperUnionListRq, req);
        }
        GF.UI.Close(this.UIForm);
        GF.UI.ShowToast("操作成功！");
    }
    public void Send_SetClubGoldRq()
    {
        float.TryParse(InputNum.text, out float num);
        if (num == 0)
            return;
        string action = toggle1.isOn ? "发放" : "回收";
        Util.GetInstance().OpenConfirmationDialog("修改联盟币", $"确定要<color=yellow>{action} {num}</color> 联盟币吗？", () =>
        {
            Msg_SetClubGoldRq req = MessagePool.Instance.Fetch<Msg_SetClubGoldRq>();
            req.LeagueId = clubInfo.LeagueId;
            req.Amount = num;
            req.Option = toggle1.isOn ? 0 : 1;
            req.Type = 0;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SetClubGoldRq, req);
        });
    }

    public void InputChangeUpdate()
    {
        float.TryParse(InputNum.text, out float num);
        if (num == 0){
            return;
        }
        if (toggle1.isOn)
        {
        }
        else
        {
            if (float.Parse(clubInfo.LeagueGold) < num)
            {
                InputNum.text = clubInfo.LeagueGold;
            }
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SetClubGoldRs, FunctionMsgSetClubGoldRs);
        base.OnClose(isShutdown, userData);
    }

}

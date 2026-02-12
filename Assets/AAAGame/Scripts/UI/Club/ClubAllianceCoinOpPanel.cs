using UnityEngine.UI;
using UnityGameFramework.Runtime;
using NetMsg;
using GameFramework.Event;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class ClubAllianceCoinOpPanel : UIFormBase
{
    public InputField InputNum;
    public Text txtName;
    public Text txtId;
    public RawImage avatar;
    public Toggle toggle1;
    public Toggle toggle2;
    public Text textCoins1;
    public Text textCoins2;
    public LeagueInfo leagueInfo;
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.Msg_ApplyClubGoldRs, Function_ApplyClubGoldRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_QuitUnionRs, Function_QuitUnionRs);
        GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);

        leagueInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("leagueInfo"));
        SetUI();
        toggle1.isOn = true;
        InputNum.text = "";
        textCoins1.transform.parent.GetComponent<Text>().text = leagueInfo.Type == 0 ? "俱乐部余额：" : "联盟余额：";
        textCoins2.transform.parent.GetComponent<Text>().text = leagueInfo.Type == 0 ? "联盟余额：" : "超级联盟余额：";
        transform.Find("Panel/Image/QuitBtn/Text").GetComponent<Text>().text = leagueInfo.Type == 0 ? "退出联盟" : "退出超级联盟";
        Util.GetInstance().CheckSafeCodeState(gameObject);
    }

    void SetUI()
    {
        txtName.text = leagueInfo.FatherInfo?.LeagueName;
        txtId.text = "ID：" + leagueInfo.FatherInfo?.LeagueId.ToString();
        textCoins1.text = GlobalManager.GetInstance().GetAllianceCoins(leagueInfo.LeagueId).ToString();
        textCoins2.text = Util.FormatAmount(leagueInfo.FatherInfo?.LeagueGold);
        Util.DownloadHeadImage(avatar, leagueInfo.FatherInfo?.LeagueHead, leagueInfo.Type);
    }

    public void InputChangeUpdate()
    {
        float.TryParse(InputNum.text, out float num);
        if (num == 0)
        {
            return;
        }
        if (toggle1.isOn)
        {
        }
        else
        {
            if (GlobalManager.GetInstance().GetAllianceCoins(leagueInfo.LeagueId) < num)
            {
                InputNum.text = GlobalManager.GetInstance().GetAllianceCoins(leagueInfo.LeagueId).ToString();
            }
        }
    }

    private void OnClubDataChanged(object sender, GameEventArgs e)
    {
        var args = e as ClubDataChangedEventArgs;
        switch (args.Type)
        {
            case ClubDataType.eve_LeagueDateUpdate:
                GF.LogInfo("俱乐部信息更新");
                leagueInfo = GlobalManager.GetInstance().LeagueInfo;
                SetUI();
                break;
        }
    }
    /// <summary>
    ///  设置俱乐部额度返回
    /// </summary>
    /// <param name="data"></param>
    private void Function_ApplyClubGoldRs(MessageRecvData data)
    {
        // Msg_ApplyClubGoldRs ack = Msg_ApplyClubGoldRs.Parser.ParseFrom(data.Data);
        // GF.LogInfo("收到申请联盟币返回:" , ack.ToString());
        GF.UI.ShowToast("申请已提交,等待审核");
        InputNum.text = "";
        GF.UI.Close(this.UIForm);
    }

    /// <summary>
    /// 退出联盟
    /// </summary>
    /// <param name="data"></param>
    private void Function_QuitUnionRs(MessageRecvData data)
    {
        Msg_QuitUnionRs ack = Msg_QuitUnionRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_QuitUnionRs收到退出联盟返回:" , ack.ToString());
        if (GF.Procedure.CurrentProcedure is HomeProcedure homeProcedure)
        {
            homeProcedure.ShowHomePanel(true);
        }
    }
    public void Send_SetClubGoldRq()
    {
        Msg_ApplyClubGoldRq req = MessagePool.Instance.Fetch<Msg_ApplyClubGoldRq>();
        float.TryParse(InputNum.text, out float num);
        if (num == 0)
            return;
        req.Amount = num;
        req.Option = toggle1.isOn ? 0 : 1;
        req.SelfClub = leagueInfo.LeagueId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ApplyClubGoldRq, req);
    }

    public void Send_QuitUnionRq()
    {
        Util.GetInstance().OpenConfirmationDialog(leagueInfo.Type == 0 ? "退出联盟" : "退出超级联盟", 
            leagueInfo.Type == 0 ? "确定要退出联盟" : "确定要退出超级联盟", () =>
        {
            // GF.LogInfo(leagueInfo.Type == 0 ? "退出联盟" : "退出超级联盟");
            Msg_QuitUnionRq req = MessagePool.Instance.Fetch<Msg_QuitUnionRq>();
            req.ClubId = leagueInfo.LeagueId;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_QuitUnionRq, req);
        });
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_ApplyClubGoldRs, Function_ApplyClubGoldRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_QuitUnionRs, Function_QuitUnionRs);
        GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        base.OnClose(isShutdown, userData);
    }

}

using UnityEngine.UI;
using UnityGameFramework.Runtime;
using NetMsg;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class ClubPlayerCoinOpPanel : UIFormBase
{

    public InputField InputNum;
    public Text txtName;
    public Text txtId;
    public RawImage avatar;
    public Toggle toggle1;
    public Toggle toggle2;
    public Text textCoins1;
    // public Text textCoins2;
    // public Text textCoins3;
    public LeagueInfo leagueInfo;
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.Msg_UserApplyClubCapitalRs, Function_UserApplyClubCapitalRs);


        leagueInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("leagueInfo"));

        toggle1.isOn = true;
        InputNum.text = "";
        SetUI();
        Util.GetInstance().CheckSafeCodeState(gameObject);
    }

    public void SetUI()
    {
        txtName.text = leagueInfo.LeagueName;
        txtId.text = "ID：" + leagueInfo.LeagueId.ToString();
        textCoins1.text = GlobalManager.GetInstance().GetAllianceCoins(leagueInfo.LeagueId).ToString();
        Util.DownloadHeadImage(avatar, leagueInfo.LeagueHead, leagueInfo.Type);
        // textCoins2.text = leagueInfo.FatherInfo?.LeagueGold.ToString();
        // textCoins3.text = leagueInfo.LeagueCoin.ToString();
    }
    /// <summary>
    ///  设置俱乐部额度返回
    /// </summary>
    /// <param name="data"></param>
    private void Function_UserApplyClubCapitalRs(MessageRecvData data)
    {
        // Msg_UserApplyClubCapitalRs ack = Msg_UserApplyClubCapitalRs.Parser.ParseFrom(data.Data);
        // GF.LogInfo("收到申请联盟币返回:" , ack.ToString());
        GF.UI.ShowToast("申请成功,等待审核");
        GF.UI.Close(this.UIForm);
    }
    public void Send_UserApplyClubGoldRq()
    {
        Msg_UserApplyClubCapitalRq req = MessagePool.Instance.Fetch<Msg_UserApplyClubCapitalRq>();
        long.TryParse(InputNum.text, out long num);
        if (num == 0)
            return;
        req.Amount = num;
        req.Option = toggle1.isOn ? 0 : 1;
        req.ClubId = leagueInfo.LeagueId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_UserApplyClubCapitalRq, req);
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
            float coin = GlobalManager.GetInstance().GetAllianceCoins(leagueInfo.LeagueId);
            if (coin < num)
            {
                InputNum.text = coin.ToString();
            }
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_UserApplyClubCapitalRs, Function_UserApplyClubCapitalRs);
        base.OnClose(isShutdown, userData);
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "SureBtn":
                Send_UserApplyClubGoldRq();
                break;
        }
    }

}

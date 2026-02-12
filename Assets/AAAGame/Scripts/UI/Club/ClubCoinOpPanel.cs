
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using NetMsg;
using UnityEngine;
using GameFramework;


[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class ClubCoinOpPanel : UIFormBase
{
    public InputField InputNum;
    public Text txtName;
    public Text txtId;
    public Toggle toggle1;
    public Toggle toggle2;
    public LeagueInfo leagueInfo;
    public LeagueUser player;

    [SerializeField] private Toggle[] toggles;
    public int type = 0;//0钻石 1联盟币 2欢乐豆
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SetClubUserGoldRs, OnMsg_SetClubUserGoldRs);
        for (int i = 0; i < toggles.Length; i++)
        {
            int type = i; 
            toggles[i].onValueChanged.AddListener(isOn => TypeToggleValueChange(isOn, type));
        }

        leagueInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("leagueInfo"));
        player = LeagueUser.Parser.ParseFrom(Params.Get<VarByteArray>("player"));
        txtName.text = player.NickName;
        txtId.text = "ID：" + player.PlayerId.ToString();
        Util.DownloadHeadImage(varAvatar, player.HeadImage);
        // textCoins.text = player.LeagueGold.ToString();
        varCoinType0.transform.Find("Value").GetComponent<Text>().text = player.Diamond.ToString();
        varCoinType1.transform.Find("Value").GetComponent<Text>().text = player.Gold;
        varCoinType2.transform.Find("Value").GetComponent<Text>().text = player.Coin;
        toggle1.isOn = true;
        toggles[2].isOn = true;
        TypeToggleValueChange(true, 2);
        InputNum.text = "";

        Util.GetInstance().CheckSafeCodeState(gameObject);
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
            switch (type)
            {
                case 0:
                    if (player.Diamond < num)
                    {
                        InputNum.text = player.Diamond.ToString();
                    }
                    break;
                case 1:
                    if (float.Parse(player.Gold) < num)
                    {
                        InputNum.text = player.Gold;
                    }
                    break;
                case 2:
                    if (float.Parse(player.Coin) < num)
                    {
                        InputNum.text = player.Coin;
                    }
                    break;
            }
        }
    }

    public void TypeToggleValueChange(bool isOn, int type)
    {
        if (isOn)
        {
            this.type = type;
            // GF.LogError("type--------------------" + type);
            toggle1.isOn = true;
            varCoinType0.SetActive(type == 0);
            varCoinType1.SetActive(type == 1);
            varCoinType2.SetActive(type == 2);
            toggle2.gameObject.SetActive(type == 1);
            InputNum.text = "";
        }
    }
    /// <summary>
    ///  设置联盟币返回
    /// </summary>
    /// <param name="data"></param>
    private void OnMsg_SetClubUserGoldRs(MessageRecvData data)
    {
        GF.Event.Fire(this, ReferencePool.Acquire<ClubDataChangedEventArgs>().Fill(ClubDataType.eve_LeagueUsersUpdate, null, null));
        GF.UI.Close(this.UIForm);
        GF.UI.ShowToast("操作成功！");
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SetClubUserGoldRs, OnMsg_SetClubUserGoldRs);
        for (int i = 0; i < toggles.Length; i++)
        {
            toggles[i].onValueChanged.RemoveAllListeners();
        }
        base.OnClose(isShutdown, userData);
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "SureBtn":
                float.TryParse(InputNum.text, out float num);
                if (num == 0)
                    return;
                string typeStr = type == 0 ? "钻石" : type == 1 ? "联盟币" : "欢乐豆";
                string action = toggle1.isOn ? "发放" : "回收";
                Util.GetInstance().OpenConfirmationDialog(action + typeStr, $"确定要<color=yellow>{action} {num}</color> {typeStr}吗？", () =>
                {
                    Msg_SetClubUserCapitalRq req = MessagePool.Instance.Fetch<Msg_SetClubUserCapitalRq>();
                    req.ClubId = leagueInfo.LeagueId; //俱乐部ID
                    req.UserId = player.PlayerId;
                    req.Type = type; //0钻石 1联盟币 2欢乐豆
                    req.Amount = num;
                    req.Option = toggle1.isOn ? 0 : 1;//0发放 1回收 (钻石只有发放)
                    HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SetClubUserCapitalRq, req);
                });
                break;
        }
    }
}

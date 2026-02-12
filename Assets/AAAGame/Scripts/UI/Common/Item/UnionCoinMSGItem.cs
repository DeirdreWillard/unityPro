
using UnityEngine.UI;
using NetMsg;
using UnityEngine;


public class UnionCoinMSGItem : MonoBehaviour
{
    public Button BtnOK;
    public Button BtnNO;
    public GameObject stateOver;

    public long applyId;
    public Msg_ApplyClubGold msg;

    public void Init(Msg_ApplyClubGold data)
    {
        applyId = data.ApplyId;
        msg = data;
        if (data.Type == 1)//1俱乐部对联盟  2玩家对俱乐部
        {
            transform.Find("UnionName").GetComponent<Text>().text = data.Union.LeagueName +"·联盟";
            transform.Find("Name").GetComponent<Text>().text = data.Club.LeagueName;
            transform.Find("ID").GetComponent<Text>().text = "ID:" + data.Club.LeagueId;
        }
        else
        {
            transform.Find("UnionName").GetComponent<Text>().text = data.Club.LeagueName +"·俱乐部";
            transform.Find("Name").GetComponent<Text>().text = data.Player.Nick;
            transform.Find("ID").GetComponent<Text>().text = "ID:" + data.Player.PlayerId;
        }
        transform.Find("Image/Time").GetComponent<Text>().text = Util.MillisecondsToDateString(data.OpTime);
        transform.Find("Info/Value").GetComponent<Text>().text = data.Amount.ToString();
        //0发放 1回收
        switch (data.Option)
        {
            case 0:
                transform.Find("Info/Text").GetComponent<Text>().text = "申请发放联盟币";
                transform.Find("Info/Text").GetComponent<Text>().color = Color.green;
                transform.Find("State").GetComponent<Text>().text = "发放";
                transform.Find("State").GetComponent<Text>().color = Color.red;
                break;
            case 1:
                transform.Find("Info/Text").GetComponent<Text>().text = "申请回收联盟币";
                transform.Find("Info/Text").GetComponent<Text>().color = Color.green;
                transform.Find("State").GetComponent<Text>().text = "回收";
                transform.Find("State").GetComponent<Text>().color = Color.red;
                break;
            default:
                break;
        }

        BtnOK.gameObject.SetActive(data.State == 0);
        BtnNO.gameObject.SetActive(data.State == 0);
        stateOver.SetActive(data.State != 0);
        //0未处理 1同意 2拒绝
        switch (data.State)
        {
            case 0:
                BtnOK.onClick.RemoveAllListeners();
                BtnNO.onClick.RemoveAllListeners();
                BtnOK.onClick.AddListener(OK);
                BtnNO.onClick.AddListener(NO);
                break;
            case 1:
                stateOver.GetComponent<Text>().text = Util.GetMyselfInfo().NickName + "已同意";
                stateOver.GetComponent<Text>().color = Color.green;
                break;
            case 2:
                stateOver.GetComponent<Text>().text = Util.GetMyselfInfo().NickName + "已拒绝";
                stateOver.GetComponent<Text>().color = Color.red;
                break;
            default:
                break;
        }

    }
    public void OK()
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (msg.Type == 1)//1俱乐部对联盟  2玩家对俱乐部
        {
            Msg_ApplyClubGoldOptionRq req = MessagePool.Instance.Fetch<Msg_ApplyClubGoldOptionRq>();
            req.ApplyId = applyId;
            req.State = 1;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ApplyClubGoldOptionRq, req);
        }
        else
        {
            Msg_UserApplyClubOptionRq req = MessagePool.Instance.Fetch<Msg_UserApplyClubOptionRq>();
            req.ApplyId = applyId;
            req.State = 1;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_UserApplyClubOptionRq, req);
        }
    }
    public void NO()
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (msg.Type == 1)//1俱乐部对联盟  2玩家对俱乐部
        {
            Msg_ApplyClubGoldOptionRq req = MessagePool.Instance.Fetch<Msg_ApplyClubGoldOptionRq>();
            req.ApplyId = applyId;
            req.State = 2;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ApplyClubGoldOptionRq, req);
        }
        else
        {
            Msg_UserApplyClubOptionRq req = MessagePool.Instance.Fetch<Msg_UserApplyClubOptionRq>();
            req.ApplyId = applyId;
            req.State = 2;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_UserApplyClubOptionRq, req);
        }
    }
}

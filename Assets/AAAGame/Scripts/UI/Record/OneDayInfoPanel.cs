using UnityEngine;
using UnityEngine.UI;
using NetMsg;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class OneDayInfoPanel : UIFormBase
{
    public GameObject clubInfo;
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
    }
    public void AddListener()
    {
        HotfixNetworkComponent.AddListener(MessageID.Msg_ClubDayTotalRs, Function_ClubDayTotalRs);
    }

    public void RemoveListener(){
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_ClubDayTotalRs, Function_ClubDayTotalRs);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
    }

    private void Function_ClubDayTotalRs(MessageRecvData data)
    {
        // Msg_ClubDayTotalRs
        Msg_ClubDayTotalRs ack = Msg_ClubDayTotalRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_ClubDayTotalRs收到俱乐部一日汇总:" , ack.ToString());
        ShowList(ack);
    }

    public void SendClubDayTotalRq(LeagueInfo leagueInfo, string startTime , string endTime)
    {
        Msg_ClubDayTotalRq req = MessagePool.Instance.Fetch<Msg_ClubDayTotalRq>();
        req.ClubId = leagueInfo.LeagueId;
        req.StartTime = startTime;
        req.EndTime = endTime;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ClubDayTotalRq, req);
    }


    //显示战绩信息

    public GameObject content;
    public GameObject itemPrefab;
    public void ShowList(Msg_ClubDayTotalRs ack)
    {
        Msg_ClubDayTotal clubDayTotal = ack.ClubDayTotal;
        clubInfo.transform.Find("ClubNameText").GetComponent<Text>().text = clubDayTotal.ClubBaseInfo.LeagueName;
        clubInfo.transform.Find("BringText").GetComponent<Text>().text = clubDayTotal.Bring;
        clubInfo.transform.Find("HandText").GetComponent<Text>().text = clubDayTotal.Hand.ToString();
        clubInfo.transform.Find("ProtectText").GetComponent<Text>().text = clubDayTotal.Protect;
        clubInfo.transform.Find("JackpotText").GetComponent<Text>().text = clubDayTotal.JackPot;
        clubInfo.transform.Find("ProfitText").GetComponent<Text>().text = clubDayTotal.Profit;
        clubInfo.transform.Find("RateText").GetComponent<Text>().text = clubDayTotal.Rate;
        //创建列表
        foreach (Transform item in content.transform)
        {
            Destroy(item.gameObject);
        }
        for (int i = 0; i < ack.List.Count; i++)
        {
            Msg_ClubUserDayTotal data = ack.List[i];
            GameObject newItem = Instantiate(itemPrefab);
            newItem.transform.SetParent(content.transform, false);
            newItem.transform.Find("ClubNameText").GetComponent<Text>().text = data.BasePlayer.Nick;
            newItem.transform.Find("BringText").GetComponent<Text>().text = data.Bring;
            newItem.transform.Find("HandText").GetComponent<Text>().text = data.Hand.ToString();
            newItem.transform.Find("ProtectText").GetComponent<Text>().text = data.Protect;
            newItem.transform.Find("JackpotText").GetComponent<Text>().text = data.JackPot;
            newItem.transform.Find("ProfitText").GetComponent<Text>().text = data.Profit;
            newItem.transform.Find("RateText").GetComponent<Text>().text = data.Rate;
        }
    }
}

using UnityEngine;
using NetMsg;
using System.Linq;
using System.Collections.Generic;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class RecordInfoPanel : UIFormBase
{
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
    }

    public void AddListener()
    {
        HotfixNetworkComponent.AddListener(MessageID.Msg_ClubRecordDeskRs, Function_ClubRecordDeskRs);
    }

    public void RemoveListener(){
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_ClubRecordDeskRs, Function_ClubRecordDeskRs);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
    }

    private void Function_ClubRecordDeskRs(MessageRecvData data)
    {
        // Msg_ClubDayTotalRs
        Msg_ClubRecordDeskRs ack = Msg_ClubRecordDeskRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_ClubRecordDeskRs收到俱乐部战绩列表:" , ack.ToString());
        ShowList(ack);
    }

    public void SendClubRecordDeskRq(LeagueInfo leagueInfo , string day){
        Msg_ClubRecordDeskRq req = MessagePool.Instance.Fetch<Msg_ClubRecordDeskRq>();
        req.ClubId = leagueInfo.LeagueId;
        req.Day = day;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ClubRecordDeskRq, req);
    }


    //显示工会列表
    public GameObject content;
    public GameObject item;

    public void ShowList(Msg_ClubRecordDeskRs ack)
    {
        List<Msg_GameRecord> gameRecordList = ack.Record.ToList();
        Util.SortListByLatestTime(gameRecordList, log => log.Time);
        //创建列表
        foreach (Transform item in content.transform)
        {
            Destroy(item.gameObject);
        }
        foreach (var gameRecord in gameRecordList)
        {
            GameObject newItem = Instantiate(item);
            newItem.transform.SetParent(content.transform, false);
            newItem.GetComponent<RecordItem>().Init(gameRecord);
        }
    }
}

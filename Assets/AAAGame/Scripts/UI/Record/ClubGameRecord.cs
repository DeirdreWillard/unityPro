
using System;
using DG.Tweening;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class ClubGameRecord : UIFormBase
{
    LeagueInfo leagueInfo;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.Msg_ClubRecordRs, Function_ClubRecordRs);
        RecordManager.GetInstance().joinType = 1;

        leagueInfo = GlobalManager.GetInstance().LeagueInfo;

        varRecordInfoPanel.SetActive(false);
        varOneDayInfoPanel.SetActive(false);

        Msg_ClubRecordRq req = MessagePool.Instance.Fetch<Msg_ClubRecordRq>();
        req.ClubId = leagueInfo.LeagueId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ClubRecordRq, req);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        RecordManager.GetInstance().joinType = 0;
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_ClubRecordRs, Function_ClubRecordRs);
        base.OnClose(isShutdown, userData);
    }

    private void Function_ClubRecordRs(MessageRecvData data)
    {
        // Msg_ClubRecordRs
        Msg_ClubRecordRs ack = Msg_ClubRecordRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_ClubRecordRs收到俱乐部战绩列表:" , ack.ToString());
        ShowMyDeskList(ack);
    }

    //显示工会列表
    public GameObject content;

    public void ShowMyDeskList(Msg_ClubRecordRs ack)
    {
        //创建列表
        foreach (Transform item in content.transform)
        {
            Destroy(item.gameObject);
        }
        for (int i = 0; i < ack.Record.Count; i++)
        {
            Msg_ClubRecord data = ack.Record[i];
            GameObject newItem = Instantiate(varRecordCountItem);
            newItem.transform.SetParent(content.transform, false);
            if (DateTime.TryParseExact(data.Day, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
            {
                newItem.transform.Find("TimeText").GetComponent<Text>().text = parsedDate.ToString("yyyy-MM-dd");
            }
            newItem.transform.Find("RecordCountText").GetComponent<Text>().text = string.Format("共计{0}个牌局", data.DeskNum);
            newItem.transform.Find("OneDayInfoBtn").GetComponent<Button>().onClick.RemoveAllListeners();
            newItem.transform.Find("OneDayInfoBtn").GetComponent<Button>().onClick.AddListener(delegate ()
            {
               Sound.PlayEffect(AudioKeys.SOUND_BTN);
                varOneDayInfoPanel.transform.SetLocalPositionX(1200);
                string startTime = "";
                string endTime = "";
                if (DateTime.TryParseExact(data.Day, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                {
                    startTime = Util.DateTimeToDateString(parsedDate.Date, "yyyy-MM-dd HH:mm:ss");
                    endTime = Util.DateTimeToDateString(parsedDate.Date.AddDays(1).AddSeconds(-1), "yyyy-MM-dd HH:mm:ss");
                }
                varOneDayInfoPanel.GetComponent<OneDayInfoPanel>().AddListener();
                varOneDayInfoPanel.GetComponent<OneDayInfoPanel>().SendClubDayTotalRq(leagueInfo, startTime, endTime);
                varOneDayInfoPanel.SetActive(true);
                varOneDayInfoPanel.transform.DOLocalMoveX(0, 0.5f);
            });
            newItem.transform.Find("RecordListBtn").GetComponent<Button>().onClick.RemoveAllListeners();
            newItem.transform.Find("RecordListBtn").GetComponent<Button>().onClick.AddListener(delegate ()
            {
               Sound.PlayEffect(AudioKeys.SOUND_BTN);
                if (DateTime.TryParseExact(data.Day, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                {
                    varDate.text = Util.DateTimeToDateString(parsedDate.Date, "MM/dd");
                }
                varRecordInfoPanel.transform.SetLocalPositionX(1200);
                varRecordInfoPanel.GetComponent<RecordInfoPanel>().AddListener();
                varRecordInfoPanel.GetComponent<RecordInfoPanel>().SendClubRecordDeskRq(leagueInfo, data.Day);
                varRecordInfoPanel.SetActive(true);
                varRecordInfoPanel.transform.DOLocalMoveX(0, 0.5f);
            });

        }
    }


    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "CloseRecordInfoPanel":
                varRecordInfoPanel.transform.DOLocalMoveX(1200, 0.3f).OnComplete(() =>
                    {
                        varRecordInfoPanel.GetComponent<RecordInfoPanel>().RemoveListener();
                        varRecordInfoPanel.SetActive(false);
                    });
                break;
            case "CloseOneDayInfoPanel":
                varOneDayInfoPanel.transform.DOLocalMoveX(1200, 0.3f).OnComplete(() =>
                {
                    varOneDayInfoPanel.GetComponent<OneDayInfoPanel>().RemoveListener();
                    varOneDayInfoPanel.SetActive(false);
                });
                break;
        }
    }

}

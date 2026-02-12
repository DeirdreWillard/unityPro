using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using UnityGameFramework.Runtime;
using Google.Protobuf;
using System.Collections.Generic;
using Tacticsoft;
using GameFramework.Event;
using System.Linq;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class ClubTeamManagePanel : UIFormBase
{
    public LeagueInfo leagueInfo;
    List<LeagueUser> leagueUsers = new();
    Dictionary<long, float> dayRateDic = new();
    Dictionary<long, float> sevenDayRateDic = new();
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        InitializeScroll();
        GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        HotfixNetworkComponent.AddListener(MessageID.Msg_ClubTeamRs, FunctionMsg_ClubTeamRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SetClubTeamLeaderRs, FunctionMsg_SetClubTeamLeaderRs);
        leagueInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("leagueInfo"));
        SendMsg_ClubTeamRq();
        Util.GetInstance().CheckSafeCodeState(gameObject);
    }

    public void SendMsg_ClubTeamRq()
    {
        GF.LogInfo("请求 战队列表");
        Msg_ClubTeamRq req = MessagePool.Instance.Fetch<Msg_ClubTeamRq>();
        req.ClubId = leagueInfo.LeagueId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ClubTeamRq, req);
    }


    #region 循环滚动
    public GameObject content;
    private void InitializeScroll()
    {
        varScroll.SetNumberOfRowsForTableView(tv => leagueUsers.Count);
        varScroll.SetHeightForRowInTableView((tv, row) => varItem.GetComponent<RectTransform>().rect.height);
        varScroll.SetCellForRowInTableView(CellForRowInTableView);       // 获取指定行的 UI
    }
    public TableViewCell CellForRowInTableView(TableView tv, int row)
    {
        TableViewCell cell = tv.GetReusableCell("cell");
        if (cell == null)
        {
            GameObject go = Instantiate(varItem, content.transform);
            cell = go.GetComponent<TableViewCell>();
            cell.name = "cell";
            cell.reuseIdentifier = "cell";// 设置复用标识符
        }
        cell.gameObject.SetActive(true);
        UpdateItemData(cell.gameObject, leagueUsers[row]);
                return cell;
;
    }

    private void UpdateItemData(GameObject cell, LeagueUser data)
    {
        cell.transform.Find("PlayerId").GetComponent<Text>().text = "ID:" + data.PlayerId.ToString();
        cell.transform.Find("Nickname").GetComponent<Text>().text = data.NickName.ToString();
        cell.transform.Find("Rate").GetComponent<Text>().text = "当日收益:" + dayRateDic[data.PlayerId] + " " + "七日收益:" + sevenDayRateDic[data.PlayerId];
        Util.DownloadHeadImage(cell.transform.Find("Mask/Head").GetComponent<RawImage>(), data.HeadImage);
        cell.transform.Find("BtnTeamUserManage").GetComponent<Button>().onClick.RemoveAllListeners();
        cell.transform.Find("BtnTeamUserManage").GetComponent<Button>().onClick.AddListener(async () =>
            {
               Sound.PlayEffect(AudioKeys.SOUND_BTN);
                var uiParams = UIParams.Create();
                uiParams.Set<VarByteArray>("leagueInfo", leagueInfo.ToByteArray());
                uiParams.Set<VarInt64>("leaderId", data.PlayerId);
                uiParams.Set<VarInt32>("type", 1);
                uiParams.Set<VarFloat>("rebate", data.Rebate);
                await GF.UI.OpenUIFormAwait(UIViews.TeamUserManagePanel, uiParams);
            });
    }
    #endregion

    private void FunctionMsg_ClubTeamRs(MessageRecvData recvData)
    {
        Msg_ClubTeamRs ack = Msg_ClubTeamRs.Parser.ParseFrom(recvData.Data);
        GF.LogInfo("收到 战队列表" , ack.ToString());
        dayRateDic = ack.TeamDayRate.ToDictionary(x => x.Key, x => float.Parse(x.Val));
        sevenDayRateDic = ack.TeamSevenRate.ToDictionary(x => x.Key, x => float.Parse(x.Val));
        varRateText.text = "当日收益:" + dayRateDic.Sum(x => x.Value) + " " + "七日收益:" + sevenDayRateDic.Sum(x => x.Value);
        leagueUsers.Clear();
        leagueUsers.AddRange(ack.LeagueUser);
        varScroll.ReloadData();
    }
    
    public void FunctionMsg_SetClubTeamLeaderRs(MessageRecvData data)
    {
        GF.LogInfo("设置队长刷新列表");
        SendMsg_ClubTeamRq();
    }

    public async void OpenSetPlayerTeamPanel(){
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("leagueInfo", leagueInfo.ToByteArray());
        uiParams.Set<VarInt64>("leaderId", 0);
        uiParams.Set<VarInt32>("type", 0);
        await GF.UI.OpenUIFormAwait(UIViews.SetPlayerTeamPanel, uiParams);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        varScroll.ClearAllRows();
        GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_ClubTeamRs, FunctionMsg_ClubTeamRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SetClubTeamLeaderRs, FunctionMsg_SetClubTeamLeaderRs);
        base.OnClose(isShutdown, userData);
    }

    private void OnClubDataChanged(object sender, GameEventArgs e)
    {
        var args = e as ClubDataChangedEventArgs;
        switch (args.Type)
        {
            case ClubDataType.eve_LeagueUsersUpdate:
                GF.LogInfo("俱乐部成员更新");
                leagueInfo = GlobalManager.GetInstance().LeagueInfo;
                SendMsg_ClubTeamRq();
                break;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using UnityGameFramework.Runtime;
using System.Collections.Generic;
using Tacticsoft;
using GameFramework.Event;
using System.Linq;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class SetPlayerTeamPanel : UIFormBase
{
    #region Private Fields
    public InputField SearchIdInput;
    public LeagueInfo leagueInfo;
    List<LeagueUser> leagueUsers = new();
    // 0: 添加战队队长
    // 1: 添加战队成员
    int type = 0;
    long leaderId = 0;
    #endregion

    #region Unity Lifecycle
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        InitializeScroll();
        GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        HotfixNetworkComponent.AddListener(MessageID.Msg_LeagueUserListRs, FunctionMsg_LeagueUserListRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SetClubTeamLeaderRs, FunctionMsg_SetClubTeamLeaderRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SetClubTeamPlayerRs, FunctionMsg_SetClubTeamPlayerRs);
        leagueInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("leagueInfo"));
        type = Params.Get<VarInt32>("type");
        if (type == 0){
            varTitleText.text = "设置战队队长";
        }else{
            varTitleText.text = "添加战队成员";
        }
        Params.TryGet<VarInt64>("leaderId", out VarInt64 leaderId);
        if (leaderId != null)
        {
            this.leaderId = (long)leaderId;
        }
        SearchIdInput.text = "";
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        varScroll.ClearAllRows();
        GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_LeagueUserListRs, FunctionMsg_LeagueUserListRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SetClubTeamLeaderRs, FunctionMsg_SetClubTeamLeaderRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SetClubTeamPlayerRs, FunctionMsg_SetClubTeamPlayerRs);
        base.OnClose(isShutdown, userData);
    }
    #endregion

    #region UI Logic
    public GameObject content;
    private void InitializeScroll()
    {
        varScroll.SetNumberOfRowsForTableView(tv => leagueUsers.Count);
        varScroll.SetHeightForRowInTableView((tv, row) => varItem.GetComponent<RectTransform>().rect.height);
        varScroll.SetCellForRowInTableView(CellForRowInTableView);
    }
    
    public TableViewCell CellForRowInTableView(TableView tv, int row)
    {
        TableViewCell cell = tv.GetReusableCell("cell");
        if (cell == null)
        {
            GameObject go = Instantiate(varItem, content.transform);
            cell = go.GetComponent<TableViewCell>();
            cell.name = "cell";
            cell.reuseIdentifier = "cell";
        }
        cell.gameObject.SetActive(true);
        UpdateItemData(cell.gameObject, leagueUsers[row]);
        return cell;
    }

    private void UpdateItemData(GameObject cell, LeagueUser data)
    {
        cell.transform.Find("Nickname").GetComponent<Text>().text = data.NickName;
        cell.transform.Find("PlayerId").GetComponent<Text>().text = "ID:" + data.PlayerId.ToString();
        Util.DownloadHeadImage(cell.transform.Find("Mask/Head").GetComponent<RawImage>(), data.HeadImage);
        GameObject btnSetting = cell.transform.Find("BtnSetting").gameObject;
        // 如果是创建者，则不能设置队长
        if(data.Identify == 9 ||
        (type == 0 && data.Identify == 2) ||
        (type == 1 && data.Identify == 1)){
            btnSetting.SetActive(false);
        }else{
            btnSetting.SetActive(true);
            btnSetting.GetComponent<Button>().onClick.RemoveAllListeners();
            if (type == 0)
            {
                btnSetting.GetComponentInChildren<Text>().text = data.Identify == 0 ? "设置队长" : "取消队长";
                btnSetting.GetComponent<Button>().onClick.AddListener(() =>
                {
                   Sound.PlayEffect(AudioKeys.SOUND_BTN);
                    if(data.Identify == 0){
                        Util.GetInstance().OpenConfirmationDialog("设置队长", $"确定要设置队长 <color=#D0FE36>{data.NickName}</color>", () =>
                        {
                            GF.LogInfo("设置队长");
                            SendMsg_SetClubTeamLeaderRq(data);
                        });
                    }else{
                        Util.GetInstance().OpenConfirmationDialog("取消队长", $"确定要取消队长 <color=#D0FE36>{data.NickName}</color>", () =>
                        {
                            GF.LogInfo("取消队长");
                            SendMsg_SetClubTeamLeaderRq(data);
                        });
                    }
                });
            }
            else
            {
                btnSetting.GetComponentInChildren<Text>().text = data.Identify == 0 ? "添加成员" : "移除成员";
                btnSetting.GetComponent<Button>().onClick.AddListener(() =>
                {
                   Sound.PlayEffect(AudioKeys.SOUND_BTN);
                    if(data.Identify == 0){
                        Util.GetInstance().OpenConfirmationDialog("添加成员", $"确定要添加成员 <color=#D0FE36>{data.NickName}</color>", () =>
                        {
                            GF.LogInfo("添加成员");
                            SendMsg_SetClubTeamPlayerRq(data);
                        });
                    }else{
                        Util.GetInstance().OpenConfirmationDialog("移除成员", $"确定要移除成员 <color=#D0FE36>{data.NickName}</color>", () =>
                        {
                            GF.LogInfo("移除成员");
                            SendMsg_SetClubTeamPlayerRq(data);
                        });
                    }
                });
            }
        }
    }
    #endregion

    #region Network
    private void SendMsg_SetClubTeamLeaderRq(LeagueUser userData)
    {
        Msg_SetClubTeamLeaderRq req = MessagePool.Instance.Fetch<Msg_SetClubTeamLeaderRq>();
        req.ClubId = leagueInfo.LeagueId;
        req.PlayerId = userData.PlayerId;
        req.State = userData.Identify == 0 ? 1 : 0; // 1:设置, 0:取消
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_SetClubTeamLeaderRq, req);
    }
    
    private void FunctionMsg_SetClubTeamLeaderRs(MessageRecvData data)
    {
        GF.LogInfo("设置战队队长返回");
        // TODO: 刷新列表或更新UI UpdateItemData
        SearchPlayer();
    }

    private void SendMsg_SetClubTeamPlayerRq(LeagueUser userData)
    {
        Msg_SetClubTeamPlayerRq req = MessagePool.Instance.Fetch<Msg_SetClubTeamPlayerRq>();
        req.ClubId = leagueInfo.LeagueId;
        req.LeaderId = leaderId;
        req.PlayerId = userData.PlayerId;
        req.State = userData.Identify == 0 ? 1 : 0; // 1:设置, 0:取消
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_SetClubTeamPlayerRq, req);
    }
    
    private void FunctionMsg_SetClubTeamPlayerRs(MessageRecvData data)
    {
        GF.LogInfo("设置战队成员返回");
        // TODO: 刷新列表或更新UI
        SearchPlayer();
    }

    private void FunctionMsg_LeagueUserListRs(MessageRecvData data)
    {
        Msg_LeagueUserListRs ack = Msg_LeagueUserListRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到 成员列表" , ack.ToString());
        leagueUsers = ack.Users.ToList();
        varScroll.ScrollY = 0;
        varScroll.ReloadData();
    }

    public void SearchPlayer()
    {
        if (SearchIdInput.text == "")
        {
            return;
        }
        varScroll.ClearAllRows();
        Msg_LeagueUserListRq req = MessagePool.Instance.Fetch<Msg_LeagueUserListRq>();
        req.LeagueId = leagueInfo.LeagueId;
        req.SortType = 0;
        req.SortDesc = 0;
        req.PageNum = 0;
        req.SearchNameOrId = SearchIdInput.text;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_LeagueUserListRq, req);
    }

    #endregion

    #region Event Handlers
    private void OnClubDataChanged(object sender, GameEventArgs e)
    {
        var args = e as ClubDataChangedEventArgs;
        switch (args.Type)
        {
            case ClubDataType.eve_LeagueUsersUpdate:
                GF.LogInfo("俱乐部成员更新");
                // clubInfo = GlobalManager.GetInstance().clubInfo;
                break;
        }
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "BtnSearch":
                SearchPlayer();
                break;
            case "Refresh":
                SearchIdInput.text = "";
                leagueUsers.Clear();
                varScroll.ClearAllRows();
                break;
        }
    }
    #endregion
}

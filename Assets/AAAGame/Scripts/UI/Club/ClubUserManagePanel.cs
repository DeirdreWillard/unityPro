using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using UnityGameFramework.Runtime;
using System;
using Google.Protobuf;
using System.Collections.Generic;
using Tacticsoft;
using GameFramework.Event;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class ClubUserManagePanel : UIFormBase
{
    public InputField SearchIdInput;

    private int currentSortType = 0;//排序方式 0金额 1时间
    private bool isLoadingMoreData = false; // 避免重复加载
    private int currentPage = 0; // 当前页码
    private int totalCount = 0; // 总数量

    public LeagueInfo leagueInfo;

    List<LeagueUser> leagueUsers = new();
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        leagueUsers?.Clear(); // 清空上次的数据
        InitializeScroll();
        GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        HotfixNetworkComponent.AddListener(MessageID.Msg_LeagueUserListRs, FunctionMsg_LeagueUserListRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SetRemarkRs, FunctionMsg_SetRemarkRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_RemovePlayerRs, FunctionMsg_RemovePlayerRs);
        leagueInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("leagueInfo"));

        currentSortType = 0;

        SendMsg_LeagueUserListRq(true);
        Util.GetInstance().CheckSafeCodeState(gameObject);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        varScroll.ClearAllRows();
        GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_LeagueUserListRs, FunctionMsg_LeagueUserListRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SetRemarkRs, FunctionMsg_SetRemarkRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_RemovePlayerRs, FunctionMsg_RemovePlayerRs);
        base.OnClose(isShutdown, userData);
    }

    public void SendMsg_LeagueUserListRq(bool isRefresh = false)
    {
        if (isRefresh){
            leagueUsers?.Clear();
            currentPage = 0; // 当前页码
            totalCount = 0; // 总数量
        }
        varAmountSortText.color = currentSortType == 0 ? Color.white : Color.grey;
        varTimeSortText.color = currentSortType == 1 ? Color.white : Color.grey;
        GF.LogInfo("请求 成员列表");
        Msg_LeagueUserListRq req = MessagePool.Instance.Fetch<Msg_LeagueUserListRq>();
        req.LeagueId = leagueInfo.LeagueId;
        req.SortType = currentSortType;
        req.SortDesc = 0;
        req.PageNum = currentPage;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_LeagueUserListRq, req);
    }


    #region 循环滚动
    public GameObject content;
    private void LoadMoreData()
    {
        if (isLoadingMoreData) return; // 避免重复加载
        // 使用已加载数量判断是否还有更多数据
        if (leagueUsers.Count >= totalCount) return;
        isLoadingMoreData = true;
        currentPage++; // 增加当前页码
        SendMsg_LeagueUserListRq();
    }
    public void InitPage(){
        leagueUsers?.Clear();
        currentPage = 0; // 当前页码
        totalCount = 0; // 总数量
    }
    private void InitializeScroll()
    {
        InitPage();
        // 监听 TableView 的滚动加载更多事件
        varScroll.OnScrollToLoadMore = LoadMoreData;
        varScroll.SetNumberOfRowsForTableView(tv => leagueUsers.Count);
        varScroll.SetHeightForRowInTableView((tv, row) => varClubUserManageItem.GetComponent<RectTransform>().rect.height);
        varScroll.SetCellForRowInTableView(CellForRowInTableView);       // 获取指定行的 UI
    }
    public TableViewCell CellForRowInTableView(TableView tv, int row)
    {
        TableViewCell cell = tv.GetReusableCell("cell");
        if (cell == null)
        {
            GameObject go = Instantiate(varClubUserManageItem, content.transform);
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
        cell.GetComponent<ClubUserManageItem>().Init(data);
        cell.transform.Find("Bg/ButtonCoinOp").GetComponent<Button>().onClick.RemoveAllListeners();
        cell.transform.Find("Bg/ButtonCoinOp").GetComponent<Button>().onClick.AddListener(async () =>
        {
           Sound.PlayEffect(AudioKeys.SOUND_BTN);
            var uiParams = UIParams.Create();
            uiParams.Set<VarByteArray>("leagueInfo", leagueInfo.ToByteArray());
            uiParams.Set<VarByteArray>("player", data.ToByteArray());
            await GF.UI.OpenUIFormAwait(UIViews.ClubCoinOpPanel, uiParams);
        });
        cell.transform.Find("Bg/BtnDetails").GetComponent<Button>().onClick.RemoveAllListeners();
        cell.transform.Find("Bg/BtnDetails").GetComponent<Button>().onClick.AddListener(async () =>
        {
           Sound.PlayEffect(AudioKeys.SOUND_BTN);
            var uiParams = UIParams.Create();
            uiParams.Set<VarByteArray>("leagueInfo", leagueInfo.ToByteArray());
            uiParams.Set<VarInt64>("playerID", data.PlayerId);
            uiParams.Set<VarInt32>("coinType", 2);
            await GF.UI.OpenUIFormAwait(UIViews.XXXDetailsPanel, uiParams);
        });
        cell.transform.Find("Bg/BtnNotes").GetComponent<Button>().onClick.RemoveAllListeners();
        cell.transform.Find("Bg/BtnNotes").GetComponent<Button>().onClick.AddListener(delegate ()
        {
           Sound.PlayEffect(AudioKeys.SOUND_BTN);
            SetRemark.transform.Find("Des").GetComponent<Text>().text = string.Format("请为 <color=#D0FE36>{0}</color> 设置备注", data.NickName);
            SetRemark.SetActive(true);
            RemarkInputField.text = "";
            RemarkClubId = leagueInfo.LeagueId;
            RemarkPlayerId = data.PlayerId;
        });
        cell.transform.Find("Bg/BtnKickOut").GetComponent<Button>().onClick.RemoveAllListeners();
        cell.transform.Find("Bg/BtnKickOut").GetComponent<Button>().onClick.AddListener(delegate ()
        {
           Sound.PlayEffect(AudioKeys.SOUND_BTN);
            Util.GetInstance().OpenConfirmationDialog("移除成员", $"确定移除成员 <color=#D0FE36>{data.NickName}</color>", () =>
            {
                Msg_RemovePlayerRq req = MessagePool.Instance.Fetch<Msg_RemovePlayerRq>();
                req.PlayerId = data.PlayerId;
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                    (HotfixNetworkComponent.AccountClientName, MessageID.Msg_RemovePlayerRq, req);
            });
        });
    }
    #endregion

    private void FunctionMsg_LeagueUserListRs(MessageRecvData data)
    {
        Msg_LeagueUserListRs ack = Msg_LeagueUserListRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到 成员列表" , ack.ToString());
        bool isFirstPage = (currentPage == 0);
        totalCount = (int)ack.Total;
        varAlliancNameText.text = "成员管理(" + totalCount + ")";
        GF.LogInfo($"总记录数: {totalCount}, 本次返回: {ack.Users.Count}, 已加载: {leagueUsers.Count}");
        // 将新数据添加到列表中
        leagueUsers.AddRange(ack.Users);
        varAmountSumText.text = "成员联盟币总额: " + Util.FormatAmount(float.Parse(ack.TotalGold));
        // 只有首次加载时才重置滚动位置
        if (isFirstPage)
        {
            varScroll.ScrollY = 0;
        }
        varScroll.ReloadData();
        isLoadingMoreData = false; // 重置加载标志
    }

    #region 备注

    public GameObject SetRemark;
    public InputField RemarkInputField;

    public long RemarkClubId;
    public long RemarkPlayerId;

    public void SendMsg_SetRemarkRq(long clubId, long playerId, string remark)
    {
        Msg_SetRemarkRq req = MessagePool.Instance.Fetch<Msg_SetRemarkRq>();
        req.ClubId = clubId; //俱乐部ID
        req.PlayerId = playerId; //俱乐部ID
        req.Remark = remark; //俱乐部ID
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SetRemarkRq, req);
    }

    public void FunctionMsg_SetRemarkRs(MessageRecvData data)
    {
        Msg_SetRemarkRs ack = Msg_SetRemarkRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_SetRemarkRs备注返回" , ack.ToString());
        GF.UI.ShowToast("设置成功");
        SetRemark.SetActive(false);
        foreach (Transform item in content.transform)
        {
            if (item.GetComponent<ClubUserManageItem>().leagueUser.PlayerId == ack.PlayerId)
            {
                item.GetComponent<ClubUserManageItem>().remarks.text = "备注：" + ack.Remark;
            }
        }
    }

    public void FunctionMsg_RemovePlayerRs(MessageRecvData data)
    {
        GF.LogInfo("踢出玩家刷新列表");
        SendMsg_LeagueUserListRq(true);
    }

    public void OnBtnRemark()
    {
        if (string.IsNullOrEmpty(RemarkInputField.text))
        {
            GF.UI.ShowToast("备注不能为空");
            return;
        }
        if (!Util.IsValidName(RemarkInputField.text))
        {
            RemarkInputField.text = "";
            GF.UI.ShowToast("备注含有特殊字符,请重新输入!");
            return;
        }
        SendMsg_SetRemarkRq(RemarkClubId, RemarkPlayerId, RemarkInputField.text);
    }
    #endregion

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
    }

    protected override async void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "BtnRemark":
                OnBtnRemark();
                break;
            case "OpenMessageBtn":
                await GF.UI.OpenUIFormAwait(UIViews.MessagePanel);
                break;
            case "BtnSearch":
                if (SearchIdInput.text == "")
                {
                    SendMsg_LeagueUserListRq(true);
                    return;
                }
                InitPage();
                varScroll.ClearAllRows();
                Msg_LeagueUserListRq req = MessagePool.Instance.Fetch<Msg_LeagueUserListRq>();
                req.LeagueId = leagueInfo.LeagueId;
                req.SortType = currentSortType;
                req.SortDesc = 0;
                req.PageNum = 0;
                req.SearchNameOrId = SearchIdInput.text;
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                    (HotfixNetworkComponent.AccountClientName, MessageID.Msg_LeagueUserListRq, req);
                break;
            case "Refresh":
                SearchIdInput.text = "";
                SendMsg_LeagueUserListRq(true);
                break;
            case "BtnClear":
                SearchIdInput.text = "";
                break;
            case "AmountSort":
                SearchIdInput.text = "";
                OnBtnClickTemp(0);
                break;
            case "TimeSort":
                SearchIdInput.text = "";
                OnBtnClickTemp(1);
                break;
        }
    }

    public void OnBtnClickTemp(int sortType)
    {
        if (currentSortType == sortType)
        {
            return;
        }
        currentSortType = sortType;
        currentPage = 1;
        SendMsg_LeagueUserListRq(true);
    }

    private void OnClubDataChanged(object sender, GameEventArgs e)
    {
        var args = e as ClubDataChangedEventArgs;
        switch (args.Type)
        {
            case ClubDataType.eve_LeagueUsersUpdate:
                GF.LogInfo("俱乐部成员更新");
                leagueInfo = GlobalManager.GetInstance().LeagueInfo;
                SendMsg_LeagueUserListRq(true);
                break;
        }
    }
}


using System;
using System.Collections.Generic;
using NetMsg;
using Tacticsoft;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class XXXDetailsPanel : UIFormBase
{
    private bool isLoadingMoreData = false; // 避免重复加载
    private int currentPage = 0; // 当前页码
    private int totalCount = 0; // 总数量

    List<Msg_ClubCapital> ClubCapitalList = new();
    AmountType amountType = AmountType.diamond;
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        ClubCapitalList?.Clear(); // 清空上次的数据
        InitializeScroll();
        HotfixNetworkComponent.AddListener(MessageID.Msg_SearchClubCapitalRs, Function_SearchClubCapitalRs);
        RequestClubCapitalData();
        Util.GetInstance().CheckSafeCodeState(gameObject);
    }

    public void RequestClubCapitalData(){
        Util.GetInstance().ShowWaiting("正在搜索...","RequestClubCapitalData");
        Msg_SearchClubCapitalRq req = MessagePool.Instance.Fetch<Msg_SearchClubCapitalRq>();
        Params.TryGet<VarByteArray>("leagueInfo", out VarByteArray byteArray);
        if (byteArray != null)
        {
            LeagueInfo leagueInfo = LeagueInfo.Parser.ParseFrom(byteArray);
            req.ClubId = leagueInfo.LeagueId;
        }
        Params.TryGet<VarInt64>("playerID", out VarInt64 curPlayerID);
        if (curPlayerID != null && (long)curPlayerID != 0) 
        {
            req.UserId = (long)curPlayerID; 
        }
        Params.TryGet<VarInt32>("coinType", out VarInt32 coinType);
        amountType = coinType == null ? AmountType.diamond : (AmountType)(int)coinType;
        req.Type = (int)amountType;
        req.StartTime = Util.DateTimeToDateString(DateTime.Today.AddDays(-7), "yyyy-MM-dd HH:mm:ss");
        req.EndTime = Util.DateTimeToDateString(DateTime.Today.AddDays(1).AddTicks(-1), "yyyy-MM-dd HH:mm:ss");
        req.PageNum = currentPage;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SearchClubCapitalRq, req);
    }
    protected override void OnClose(bool isShutdown, object userData)
    {
        varScroll.ClearAllRows();
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SearchClubCapitalRs, Function_SearchClubCapitalRs);
        base.OnClose(isShutdown, userData);
    }

    private void Function_SearchClubCapitalRs(MessageRecvData data)
    {
        // Msg_DeskListRs
        Util.GetInstance().CloseWaiting("RequestClubCapitalData");
        Msg_SearchClubCapitalRs ack = Msg_SearchClubCapitalRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到联盟币明细列表:" , ack.ToString());
        bool isFirstPage = (currentPage == 0);
        totalCount = (int)ack.Total;
        GF.LogInfo($"总记录数: {totalCount}, 本次返回: {ack.ClubCapital.Count}, 已加载: {ClubCapitalList.Count}");
        // 将新数据添加到列表中
        ClubCapitalList.AddRange(ack.ClubCapital);
        // Util.SortListByLatestTime(ClubCapitalList, log => log.OpTime);
        varNoFindImg.SetActive(ClubCapitalList.Count == 0);
        // 只有首次加载时才重置滚动位置
        if (isFirstPage)
        {
            varScroll.ScrollY = 0;
        }
        varScroll.ReloadData();
        isLoadingMoreData = false; // 重置加载标志
    }

    #region 循环滚动
    public GameObject content;

    private void LoadMoreData()
    {
        if (isLoadingMoreData) return; // 避免重复加载
        // 使用已加载数量判断是否还有更多数据
        if (ClubCapitalList.Count >= totalCount) return;
        isLoadingMoreData = true;
        currentPage++; // 增加当前页码
        RequestClubCapitalData();
    }
    private void InitializeScroll()
    {
        ClubCapitalList = new();
        currentPage = 0; // 当前页码
        totalCount = 0; // 总数量
        // 监听 TableView 的滚动加载更多事件
        varScroll.OnScrollToLoadMore = LoadMoreData;
        varScroll.SetNumberOfRowsForTableView(tv => ClubCapitalList.Count);
        varScroll.SetHeightForRowInTableView((tv, row) => varUnionDetailItem.GetComponent<RectTransform>().rect.height);
        varScroll.SetCellForRowInTableView(CellForRowInTableView);       // 获取指定行的 UI
    }
    public TableViewCell CellForRowInTableView(TableView tv, int row)
    {
        TableViewCell cell = tv.GetReusableCell("cell");
        if (cell == null)
        {
            GameObject go = Instantiate(varUnionDetailItem, content.transform);
            cell = go.GetComponent<TableViewCell>();
            cell.name = $"Cell {row}"; 
            cell.reuseIdentifier = "cell";// 设置复用标识符
        }
        cell.gameObject.SetActive(true);
        cell.GetComponent<UnionDetailItem>().Init(ClubCapitalList[row], amountType);
                return cell;
;
    }
    #endregion
}


using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using Google.Protobuf.Collections;
using Google.Protobuf;
using System;
using UnityEngine.UI.Extensions.Examples;
using System.Collections.Generic;
using GameFramework.Event;
using Tacticsoft;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class ClubAssetsPanel : UIFormBase
{
    [SerializeField] private Toggle[] dayToggles; // 拖入 Toggle 数组
    [SerializeField] private Toggle[] coinTypeToggles; // 拖入 Toggle 数组
    private readonly int[] daysArray = { 0, 1, 2, 6 };   // 对应每个 Toggle 的天数

    LeagueInfo leagueInfo;
    List<Msg_ClubCapital> ClubCapitalList = new();

    private bool isLoadingMoreData = false; // 避免重复加载
    private int currentPage = 0; // 当前页码
    private int totalCount = 0; // 总数量

    private readonly string[] dropdownOptions = new[] {
            "全部",
            "俱乐部转账",
            "俱乐部回收",
            "联盟转账",
            "联盟回收",
            "牌桌带入",
         };
    private int currentChangeType = 0;
    private string startTime;
    private string endTime;

    private AmountType coinType = AmountType.coin;//1欢乐豆 2联盟币 3 钻石
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        ClubCapitalList?.Clear(); // 清空上次的数据
        
        // 先设置默认状态（不触发事件）
        varType1.SetIsOnWithoutNotify(true);
        varType2.SetIsOnWithoutNotify(false);
        dayToggles[0].SetIsOnWithoutNotify(true);
        for (int i = 1; i < dayToggles.Length; i++)
        {
            dayToggles[i].SetIsOnWithoutNotify(false);
        }
        coinTypeToggles[0].SetIsOnWithoutNotify(true);
        for (int i = 1; i < coinTypeToggles.Length; i++)
        {
            coinTypeToggles[i].SetIsOnWithoutNotify(false);
        }
        
        // 再添加监听器
        for (int i = 0; i < dayToggles.Length; i++)
        {
            int days = daysArray[i]; // 捕获当前的 days 值
            dayToggles[i].onValueChanged.AddListener(isOn => DayToggleValueChange(isOn, days));
        }
        for (int i = 0; i < coinTypeToggles.Length; i++)
        {
            int coinType = i + 1;
            coinTypeToggles[i].onValueChanged.AddListener(isOn => OnCoinTypeToggle(coinType, isOn));
        }

        varDeskToggle1.onValueChanged.AddListener(OnDeskToggleChanged);
        varType1.onValueChanged.AddListener(OnTypeToggleChanged);
        GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SearchClubCapitalRs, Function_SearchClubCapitalRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_UnionRecordRs, Function_UnionRecordRs);
        InitializeScroll();

        leagueInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("leagueInfo"));
        varStartTime.text = Util.DateTimeToDateString(DateTime.Today, "yyyy-MM-dd-HH:mm");
        varEndTime.text = Util.DateTimeToDateString(DateTime.Today.AddDays(1).AddTicks(-1), "yyyy-MM-dd-HH:mm");
        startTime = Util.DateTimeToDateString(DateTime.Today, "yyyy-MM-dd HH:mm:ss");
        endTime = Util.DateTimeToDateString(DateTime.Today.AddDays(1).AddTicks(-1), "yyyy-MM-dd HH:mm:ss");


        SetUI();

        SendMsg_SearchClubCapitalRq(true);
        Util.GetInstance().CheckSafeCodeState(gameObject);
    }
    protected override void OnClose(bool isShutdown, object userData)
    {
        // 移除所有监听器
        varDropdown.onValueChanged.RemoveAllListeners();
        varScroll1.ClearAllRows();
        for (int i = 0; i < dayToggles.Length; i++)
        {
            dayToggles[i].onValueChanged.RemoveAllListeners();
        }
        for (int i = 0; i < coinTypeToggles.Length; i++)
        {
            coinTypeToggles[i].onValueChanged.RemoveAllListeners();
        }
        varDeskToggle1.onValueChanged.RemoveListener(OnDeskToggleChanged);
        varType1.onValueChanged.RemoveListener(OnTypeToggleChanged);
        GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);

        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SearchClubCapitalRs, Function_SearchClubCapitalRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_UnionRecordRs, Function_UnionRecordRs);
        
        // 重置所有状态到默认值
        ClubCapitalList?.Clear();
        currentPage = 0;
        totalCount = 0;
        isLoadingMoreData = false;
        isRefreshing = false;
        currentChangeType = 0;
        coinType = AmountType.coin;
        leagueInfo = null;
        
        // 关闭正在显示的等待提示
        Util.GetInstance().CloseWaiting("SendMsg_SearchClubCapitalRq");
        Util.GetInstance().CloseWaiting("SendMsg_UnionRecordRq");
        
        base.OnClose(isShutdown, userData);
    }

    public void InitDropdown()
    {
        // 清空已有的选项
        varDropdown.options.Clear();

        // 添加选项到 Dropdown
        foreach (var item in dropdownOptions)
        {
            varDropdown.options.Add(new Dropdown.OptionData(item));
        }

        varDropdown.value = 0; // 默认选中第一个选项
        varDropdown.captionText.text = dropdownOptions[0];

        varDropdown.onValueChanged.RemoveAllListeners();
        varDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        varDropdown.gameObject.SetActive(this.coinType == AmountType.gold);
    }

    private void OnDropdownValueChanged(int index)
    {
        GF.LogInfo($"选中项: {dropdownOptions[index]}");
        switch (index)
        {
            case 0:
                currentChangeType = 0;
                break;
            case 1:
                currentChangeType = 7;
                break;
            case 2:
                currentChangeType = 8;
                break;
            case 3:
                currentChangeType = 11;
                break;
            case 4:
                currentChangeType = 12;
                break;
            case 5:
                currentChangeType = 13;
                break;
            default:
                break;
        }
        SendMsg_SearchClubCapitalRq(true);
    }

    public void SetUI()
    {
        InitDropdown();
        varCoin.text = Util.GetMyselfInfo().Gold.ToString();
        varDiamond.text = Util.GetMyselfInfo().Diamonds.ToString();
        varUnionCoin.text = GlobalManager.GetInstance().GetAllianceCoins(leagueInfo.LeagueId).ToString();
    }

    private void OnClubDataChanged(object sender, GameEventArgs e)
    {
        var args = e as ClubDataChangedEventArgs;
        switch (args.Type)
        {
            case ClubDataType.eve_LeagueDateUpdate:
                GF.LogInfo("俱乐部信息更新");
                leagueInfo = GlobalManager.GetInstance().LeagueInfo;
                SetUI();
                break;
        }
    }

    public void OnCoinTypeToggle(int coinType, bool isOn)
    {
        if (isOn)
        {
           Sound.PlayEffect(AudioKeys.SOUND_BTN);
            this.coinType = (AmountType)coinType;
            varDropdown.value = 0;
            varDropdown.gameObject.SetActive(this.coinType == AmountType.gold);
            SendMsg_SearchClubCapitalRq(true);
        }
    }
    private void OnDeskToggleChanged(bool isOn)
    {
        for (int i = 0; i < 4; i++)
        {
            varScroll2.transform.Find(string.Format("Viewport/Content/ScoreItem{0}/Club", i)).gameObject.SetActive(isOn);
            varScroll2.transform.Find(string.Format("Viewport/Content/ScoreItem{0}/Union", i)).gameObject.SetActive(!isOn);
        }
        SendMsg_UnionRecordRq();
    }

    private void OnTypeToggleChanged(bool isOn)
    {
        varToggle1.SetActive(isOn);
        varToggle2.SetActive(!isOn);
        varScroll1.gameObject.SetActive(isOn);
        varScroll2.SetActive(!isOn);
    }
    public void SendMsg_SearchClubCapitalRq(bool refresh = false)
    {
        if (refresh){
            ClubCapitalList?.Clear();
            currentPage = 0; // 当前页码
            totalCount = 0; // 总数量，初始为0表示未知
            isRefreshing = true; // 标记为刷新操作
        }
        Util.GetInstance().ShowWaiting("正在搜索...","SendMsg_SearchClubCapitalRq");
        Msg_SearchClubCapitalRq req = MessagePool.Instance.Fetch<Msg_SearchClubCapitalRq>();
        req.ClubId = leagueInfo.LeagueId;
        req.Type = (int)coinType;
        req.StartTime = startTime;
        req.EndTime = endTime;
        req.ChangeType = currentChangeType;
        req.PageNum = currentPage;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SearchClubCapitalRq, req);
    }
    public void SendMsg_UnionRecordRq()
    {
        Util.GetInstance().ShowWaiting("正在搜索...","SendMsg_UnionRecordRq");
        Msg_UnionRecordRq req = MessagePool.Instance.Fetch<Msg_UnionRecordRq>();
        req.ClubId = leagueInfo.LeagueId;
        req.DeskType = varDeskToggle1.isOn ? 1 : 2;
        req.StartTime = startTime;
        req.EndTime = endTime;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_UnionRecordRq, req);
    }

    private bool isRefreshing = false; // 标记是否为刷新操作
    private void Function_SearchClubCapitalRs(MessageRecvData data)
    {
        // Msg_DeskListRs
        Util.GetInstance().CloseWaiting("SendMsg_SearchClubCapitalRq");
        Msg_SearchClubCapitalRs ack = Msg_SearchClubCapitalRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到联盟币明细列表:" , ack.ToString());
        totalCount = (int)ack.Total;
        GF.LogInfo($"总记录数: {totalCount}, 本次返回: {ack.ClubCapital.Count}, 已加载: {ClubCapitalList.Count}");
        ClubCapitalList.AddRange(ack.ClubCapital);
        varNoFIndImg.SetActive(ClubCapitalList.Count == 0);
        
        // 只有刷新时才重置滚动位置，加载更多时保持当前位置
        if (isRefreshing)
        {
            varScroll1.ScrollY = 0;
            isRefreshing = false;
        }
        varScroll1.ReloadData();
        isLoadingMoreData = false; // 重置加载标志
    }

    private void Function_UnionRecordRs(MessageRecvData data)
    {
        // Msg_UnionRecordRs联盟组织财产4000227
        Util.GetInstance().CloseWaiting("SendMsg_UnionRecordRq");
        Msg_UnionRecordRs ack = Msg_UnionRecordRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("联盟组织财产信息:" , ack.ToString());
        ClearShouYiTongJi();
        UpdateShouYiTongJi(ack);
    }

    public void UpdateShouYiTongJi(Msg_UnionRecordRs ack)
    {
        RepeatedField<Msg_TotalRecord> msg_TotalRecords = ack.TotalRecord;
        for (int i = 0; i < msg_TotalRecords.Count; i++)
        {
            var data = msg_TotalRecords[i];
            var go = varScroll2.transform.Find(string.Format("Viewport/Content/ScoreItem{0}", (int)data.Type));
            go.Find("Club").gameObject.SetActive(varDeskToggle1.isOn);
            go.Find("Union").gameObject.SetActive(!varDeskToggle1.isOn);
            go.Find("Club/Record1/Value").GetComponent<Text>().text = data.Original;
            go.Find("Club/Record2/Value").GetComponent<Text>().text = data.Actual;
            go.Find("Club/Record3/Value").GetComponent<Text>().text = data.Rate;
            go.Find("Club/Record4/Value").GetComponent<Text>().text = data.Protect;
            go.Find("Union/Record1/Value").GetComponent<Text>().text = data.ClubRtn;
            go.Find("Union/Record2/Value").GetComponent<Text>().text = data.VipRtn;
            go.Find("Union/Record3/Value").GetComponent<Text>().text = data.RtnRate;
            go.Find("Union/Record4/Value").GetComponent<Text>().text = data.ClubProtect;
            go.Find("Union/Record5/Value").GetComponent<Text>().text = data.VipProtect;
            go.Find("Union/Record6/Value").GetComponent<Text>().text = data.Protect;
        }
    }
    
    public void ClearShouYiTongJi(){
        for (int i = 0; i < 4; i++){
            var go = varScroll2.transform.Find(string.Format("Viewport/Content/ScoreItem{0}", i));
            go.Find("Club").gameObject.SetActive(varDeskToggle1.isOn);
            go.Find("Union").gameObject.SetActive(!varDeskToggle1.isOn);
            go.Find("Club/Record1/Value").GetComponent<Text>().text = "0";
            go.Find("Club/Record2/Value").GetComponent<Text>().text = "0";
            go.Find("Club/Record3/Value").GetComponent<Text>().text = "0";
            go.Find("Club/Record4/Value").GetComponent<Text>().text = "0";
            go.Find("Union/Record1/Value").GetComponent<Text>().text = "0";
            go.Find("Union/Record2/Value").GetComponent<Text>().text = "0";
            go.Find("Union/Record3/Value").GetComponent<Text>().text = "0";
            go.Find("Union/Record4/Value").GetComponent<Text>().text = "0";
            go.Find("Union/Record5/Value").GetComponent<Text>().text = "0";
            go.Find("Union/Record6/Value").GetComponent<Text>().text = "0";
        }
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
        SendMsg_SearchClubCapitalRq();
    }
    private void InitializeScroll()
    {
        ClubCapitalList?.Clear();
        currentPage = 0; // 当前页码
        totalCount = 0; // 总数量
        varScroll1.OnScrollToLoadMore = LoadMoreData;
        varScroll1.SetNumberOfRowsForTableView(tv => ClubCapitalList.Count);
        varScroll1.SetHeightForRowInTableView((tv, row) => varUnionDetailItem.GetComponent<RectTransform>().rect.height);
        varScroll1.SetCellForRowInTableView(CellForRowInTableView);       // 获取指定行的 UI
    }
    public TableViewCell CellForRowInTableView(TableView tv, int row)
    {
        TableViewCell cell = tv.GetReusableCell("cell");
        if (cell == null)
        {
            GameObject go = Instantiate(varUnionDetailItem, content.transform);
            cell = go.GetComponent<TableViewCell>();
            cell.name = "cell";
            cell.reuseIdentifier = "cell";// 设置复用标识符
        }
        cell.gameObject.SetActive(true);
        cell.GetComponent<UnionDetailItem>().Init(ClubCapitalList[row], coinType);
                return cell;
;
    }

    #endregion

    public void DayToggleValueChange(bool isOn, int days)
    {
        if (isOn)
        {
           Sound.PlayEffect(AudioKeys.SOUND_BTN);
            // GF.LogError("days--------------------" + days);
            startTime = Util.DateTimeToDateString(DateTime.Today.AddDays(0 - days), "yyyy-MM-dd HH:mm:ss");
            varStartTime.text = Util.DateTimeToDateString(DateTime.Today.AddDays(0 - days), "yyyy-MM-dd-HH:mm");

            endTime = Util.DateTimeToDateString(DateTime.Today.AddDays(1).AddTicks(-1), "yyyy-MM-dd HH:mm:ss");
            varEndTime.text = Util.DateTimeToDateString(DateTime.Today.AddDays(1).AddTicks(-1), "yyyy-MM-dd-HH:mm");

            if (varType1.isOn)
            {
                SendMsg_SearchClubCapitalRq(true);
            }
            else
            {
                SendMsg_UnionRecordRq();
            }
        }
    }


    protected override async void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        Util.AddLockTime(1f);
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("leagueInfo", leagueInfo.ToByteArray());
        switch (btId)
        {
            case "Time1":
                varDatePicker.GetComponent<ScrollingCalendar>().SetDate(DateTime.Now,
                 Confirm: () =>
                        {
                           Sound.PlayEffect(AudioKeys.SOUND_BTN);
                            DateTime rs = varDatePicker.GetComponent<ScrollingCalendar>().GetDateResult();
                            varStartTime.text = Util.DateTimeToDateString(rs, "yyyy-MM-dd-HH:mm");
                            startTime = Util.DateTimeToDateString(rs, "yyyy-MM-dd HH" + ":00:00");
                            varDatePicker.SetActive(false);
                        },
                        cancel: () =>
                        {
                           Sound.PlayEffect(AudioKeys.SOUND_BTN);
                            varDatePicker.SetActive(false);
                        }
                        );
                varDatePicker.SetActive(true);
                break;
            case "Time2":
                varDatePicker.GetComponent<ScrollingCalendar>().SetDate(DateTime.Now,
                 Confirm: () =>
                        {
                           Sound.PlayEffect(AudioKeys.SOUND_BTN);
                            DateTime rs = varDatePicker.GetComponent<ScrollingCalendar>().GetDateResult();
                            varEndTime.text = Util.DateTimeToDateString(rs, "yyyy-MM-dd-HH:mm");
                            endTime = Util.DateTimeToDateString(rs, "yyyy-MM-dd HH:mm:ss");
                            varDatePicker.SetActive(false);
                        },
                        cancel: () =>
                        {
                           Sound.PlayEffect(AudioKeys.SOUND_BTN);
                            varDatePicker.SetActive(false);
                        }
                        );
                varDatePicker.SetActive(true);
                break;
            case "FindBtn":
                if (varType1.isOn){
                    SendMsg_SearchClubCapitalRq(true);
                }
                else
                    SendMsg_UnionRecordRq();
                break;
            case "AllianceCoinBtn":
                if (leagueInfo.Father == 0)
                    await GF.UI.OpenUIFormAwait(UIViews.JoinAlliance, uiParams);
                else
                    await GF.UI.OpenUIFormAwait(UIViews.ClubAllianceCoinOpPanel, uiParams);
                break;
            case "ShopBtn":
                await GF.UI.OpenUIFormAwait(UIViews.ShopPanel, uiParams);
                break;
        }
    }
}

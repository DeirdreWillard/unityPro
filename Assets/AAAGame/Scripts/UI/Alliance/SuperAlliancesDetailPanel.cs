
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using NetMsg;
using Google.Protobuf;
using Google.Protobuf.Collections;
using System.Collections.Generic;
using System.Linq;
using Tacticsoft;
using GameFramework.Event;


[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class SuperAlliancesDetailPanel : UIFormBase
{
    LeagueInfo leagueInfo;

    private int methodType;
    private RepeatedField<DeskCommonInfo> deskCommonInfos;
    private List<DeskCommonInfo> deskCommonInfosCache = new();
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        InitializeScroll();
        HotfixNetworkComponent.AddListener(MessageID.Msg_QuitLeagueRs, OnMsg_QuitLeagueRsAck);
        HotfixNetworkComponent.AddListener(MessageID.Msg_DeskListRs, Function_DeskListRs);
        GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        // 初始化 Toggle 监听事件
        varAll.onValueChanged.AddListener(OnAllToggleChanged);
        varNiuniu.onValueChanged.AddListener(OnNiuToggleChanged);
        varJinhua.onValueChanged.AddListener(OnJHToggleChanged);
        varDezhou.onValueChanged.AddListener(OnDZToggleChanged);
        varBiji.onValueChanged.AddListener(OnBijiToggleChanged);

        leagueInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("leagueInfo"));
        InitPage();
        Util.GetInstance().CheckSafeCodeState(gameObject);
    }

    void InitPage()
    {
        varBtnOpenCreatePanel.SetActive(Util.IsMySelf(leagueInfo.Creator));
        SetLeaguePanel();
        // 默认选择"全部"
        methodType = -1;
        varAll.isOn = true;
        Send_DeskListRq();
    }

    public void SetLeaguePanel()
    {
        varNickname.text = leagueInfo.LeagueName;
        varID.text = "ID: " +  leagueInfo.LeagueId;
        Util.DownloadHeadImage(varAvatar, leagueInfo.LeagueHead, leagueInfo.Type);
    }

    /// <summary>
    /// 牌桌列表
    /// </summary>
    /// <param name="data"></param>

    public void Send_DeskListRq()
    {
        // Msg_DeskListRq
        Msg_DeskListRq req = MessagePool.Instance.Fetch<Msg_DeskListRq>();
        req.LeagureId = (int)leagueInfo.LeagueId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_DeskListRq, req);
    }
    public void Function_DeskListRs(MessageRecvData data)
    {
        // Msg_DeskListRs
        Msg_DeskListRs ack = Msg_DeskListRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息牌桌列表:" , ack.ToString());
        deskCommonInfos = ack.DeskInfos;
        ShowMyDeskList();
    }

    public void ShowMyDeskList()
    {
        deskCommonInfosCache = new();
        if (methodType != -1)
        {
            for (int i = 0; i < deskCommonInfos.Count; i++)
            {
                if (deskCommonInfos[i].MethodType == (MethodType)methodType)
                {
                    deskCommonInfosCache.Add(deskCommonInfos[i]);
                }
            }
        }
        else
        {
            deskCommonInfosCache = deskCommonInfos
                .Where(info => Util.IsPortraitGame(info.MethodType))
                .OrderByDescending(info => info.State == DeskState.StartRun)
                .ToList();
        }
        varScroll.ScrollY = 0;
        varScroll.ReloadData();
    }

    #region 循环滚动
    public GameObject content;
    public GameObject itemPrefab;

    private void InitializeScroll()
    {
        varScroll.SetNumberOfRowsForTableView(tv => deskCommonInfosCache.Count);
        varScroll.SetHeightForRowInTableView((tv, row) => itemPrefab.GetComponent<RectTransform>().rect.height);
        varScroll.SetCellForRowInTableView(CellForRowInTableView);       // 获取指定行的 UI
    }
    public TableViewCell CellForRowInTableView(TableView tv, int row)
    {
        TableViewCell cell = tv.GetReusableCell("cell");
        if (cell == null)
        {
            GameObject go = Instantiate(itemPrefab, content.transform);
            cell = go.GetComponent<TableViewCell>();
            cell.name = $"Cell {row}"; 
            cell.reuseIdentifier = "cell";// 设置复用标识符
        }
        cell.gameObject.SetActive(true);
        UpdateItemData(cell.gameObject, deskCommonInfosCache[row]);
                return cell;
;
    }

    private void UpdateItemData(GameObject cell, DeskCommonInfo data)
    {
        cell.GetComponent<DeskItem>().Init(data);
        cell.GetComponent<Button>().onClick.RemoveAllListeners();
        cell.GetComponent<Button>().onClick.AddListener(delegate ()
        {
           Sound.PlayEffect(AudioKeys.SOUND_BTN);
            this.ItemClick(cell);
        });
    }
    #endregion

    public void ItemClick(GameObject sender)
    {
        DeskCommonInfo deskCommonInfo = sender.GetComponent<DeskItem>().deskCommonInfo;
		Util.GetInstance().Send_EnterDeskRq(deskCommonInfo.DeskId);
    }

    public void OnMsg_QuitLeagueRsAck(MessageRecvData data)
    {
        Msg_QuitLeagueRs ack = Msg_QuitLeagueRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到退出");
        if (GF.Procedure.CurrentProcedure is HomeProcedure homeProcedure)
        {
            homeProcedure.ShowHomePanel(true);
        }
    }

    private void OnAllToggleChanged(bool isOn)
    {
        if (isOn)
            UpdateTable(-1);
    }
    private void OnNiuToggleChanged(bool isOn)
    {
        if (isOn)
            UpdateTable(0);
    }
    private void OnJHToggleChanged(bool isOn)
    {
        if (isOn)
            UpdateTable(1);
    }
    private void OnDZToggleChanged(bool isOn)
    {
        if (isOn)
            UpdateTable(2);
    }
    private void OnBijiToggleChanged(bool isOn)
    {
        if (isOn)
            UpdateTable(3);
    }


    void UpdateTable(int gameType)
    {
        // GF.LogInfo("游戏选择: " + gameType);
        methodType = gameType;
        ShowMyDeskList();
    }

    protected override async void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("leagueInfo", leagueInfo.ToByteArray());
        switch (btId)
        {
            case "UpdateAllianceName":
                await GF.UI.OpenUIFormAwait(UIViews.AllianceResetPanel, uiParams);
                break;
            case "ManagerList":
                // await GF.UI.OpenUIFormAwait(UIViews.ShopUIForm);
                break;
            case "BtnQuitSuperUnion":
                Util.GetInstance().OpenConfirmationDialog("解散超级联盟", "确定要解散超级联盟", () =>
                   {
                       GF.LogInfo("解散超级联盟");
                       Msg_QuitLeagueRq req = MessagePool.Instance.Fetch<Msg_QuitLeagueRq>();
                       req.LeagueId = leagueInfo.LeagueId;
                       HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                           (HotfixNetworkComponent.AccountClientName, MessageID.Msg_QuitLeagueRq, req);
                   });
                break;
            case "Shop":
                await GF.UI.OpenUIFormAwait(UIViews.AllianceShopPanel);
                break;
            case "ClubManage":
                await GF.UI.OpenUIFormAwait(UIViews.AllianceClubManagePanel, uiParams);
                break;
            case "Invite":
                await GF.UI.OpenUIFormAwait(UIViews.AllianceInvitePanel, uiParams);
                break;
            case "CoinManage":
                await GF.UI.OpenUIFormAwait(UIViews.AllianceCoinManagePanel, uiParams);
                break;
            case "Assets":
                await GF.UI.OpenUIFormAwait(UIViews.AllianceAssetsPanel, uiParams);
                break;
            case "Jackpot":
                await GF.UI.OpenUIFormAwait(UIViews.ClubJackpotPanel, uiParams);
                break;
            case "BtnOpenCreatePanel":
                await GF.UI.OpenUIFormAwait(UIViews.CreateRoom, uiParams);
                break;
            case "SuperAlliance":
                await GF.UI.OpenUIFormAwait(UIViews.CreateRoom, uiParams);
                break;
        }
    }

    private void OnClubDataChanged(object sender, GameEventArgs e)
    {
        var args = e as ClubDataChangedEventArgs;
        switch (args.Type)
        {
            case ClubDataType.eve_LeagueDateUpdate:
                GF.LogInfo("俱乐部信息更新");
                leagueInfo = GlobalManager.GetInstance().LeagueInfo;
                Send_DeskListRq();
                SetLeaguePanel();
                break;
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        
        varScroll.ClearAllRows();
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_QuitLeagueRs, OnMsg_QuitLeagueRsAck);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_DeskListRs, Function_DeskListRs);
        GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);

        varAll.onValueChanged.RemoveListener(OnAllToggleChanged);
        varNiuniu.onValueChanged.RemoveListener(OnNiuToggleChanged);
        varJinhua.onValueChanged.RemoveListener(OnJHToggleChanged);
        varDezhou.onValueChanged.RemoveListener(OnDZToggleChanged);
        varBiji.onValueChanged.RemoveListener(OnBijiToggleChanged);
        base.OnClose(isShutdown, userData);
    }
}

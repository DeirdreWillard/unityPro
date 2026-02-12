
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using NetMsg;
using Google.Protobuf;
using Google.Protobuf.Collections;
using GameFramework.Event;
using System.Collections.Generic;
using System.Linq;
using Tacticsoft;


[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class ClubDetailPanel : UIFormBase
{

    public Text leagueCoin; //金币
    public Text leagueGold;  //联盟币
    //注册界面相关

    public Text alliancNameText;

    public Button BtnUnLock;
    public Button BtnLock;

    public LeagueInfo leagueInfo;
    private int methodType;
    private RepeatedField<DeskCommonInfo> deskCommonInfos;
    private List<DeskCommonInfo> deskCommonInfosCache = new();

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        leagueInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("leagueInfo"));

        InitializeScroll();
        HotfixNetworkComponent.AddListener(MessageID.Msg_DeskListRs, Function_DeskListRs);
        GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
        GF.Event.Subscribe(GFEventArgs.EventId, OnGFEventChanged);

        varClubChatPanel.GetComponent<ClubChatPanelMono>().Init(leagueInfo);

        // 初始化 Toggle 监听事件
        varAll.onValueChanged.AddListener(OnAllToggleChanged);
        varNiuniu.onValueChanged.AddListener(OnNiuToggleChanged);
        varJinhua.onValueChanged.AddListener(OnJHToggleChanged);
        varDezhou.onValueChanged.AddListener(OnDZToggleChanged);
        varBiji.onValueChanged.AddListener(OnBijiToggleChanged);
        varShowContent.onValueChanged.AddListener(OnContentToggleChanged);

        Send_DeskListRq();
        SetLeaguePanel();
        varBtnOpenCreatePanel.SetActive(Util.IsMySelf(leagueInfo.Creator));

        // 默认选择"全部"
        methodType = -1;
        varAll.isOn = true;
        varShowContent.isOn = true;
        SwitchPanel();
        SwitchCoinType(leagueInfo.Father == 0);
        SwitchHideState();
        SetChatNum();
    }

    public void SetLeaguePanel(){
        alliancNameText.text = leagueInfo.LeagueName;
        Util.DownloadHeadImage(varAvatar, leagueInfo.LeagueHead, leagueInfo.Type);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        varScroll.ClearAllRows();
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_DeskListRs, Function_DeskListRs);
        GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
        GF.Event.Unsubscribe(GFEventArgs.EventId, OnGFEventChanged);

        varAll.onValueChanged.RemoveListener(OnAllToggleChanged);
        varNiuniu.onValueChanged.RemoveListener(OnNiuToggleChanged);
        varJinhua.onValueChanged.RemoveListener(OnJHToggleChanged);
        varDezhou.onValueChanged.RemoveListener(OnDZToggleChanged);
        varBiji.onValueChanged.RemoveListener(OnBijiToggleChanged);
        varShowContent.onValueChanged.RemoveListener(OnContentToggleChanged);

        varClubChatPanel.GetComponent<ClubChatPanelMono>().Close();
        base.OnClose(isShutdown, userData);
    }

    private void OnClubDataChanged(object sender, GameEventArgs e)
    {
        var args = e as ClubDataChangedEventArgs;
        switch (args.Type)
        {
            case ClubDataType.eve_LeagueDateUpdate:
                GF.LogInfo("俱乐部信息更新");
                leagueInfo = GlobalManager.GetInstance().LeagueInfo;
                SwitchHideState();
                // Send_DeskListRq();
                SetLeaguePanel();
                break;
        }
    }

    public void SwitchHideState(){
        bool safeCodeLock = GF.DataModel.GetDataModel<UserDataModel>().LockState == 1;
        BtnUnLock.gameObject.SetActive(!safeCodeLock);
        BtnLock.gameObject.SetActive(safeCodeLock);
        leagueCoin.text = safeCodeLock ? "******" : Util.GetMyselfInfo().Gold;
        leagueGold.text = safeCodeLock ? "******" : GlobalManager.GetInstance().GetAllianceCoins(leagueInfo.LeagueId).ToString();
    }

    public void SetChatNum(){
        ChatManager.GetInstance().GetClubChatNumByClubID(leagueInfo.LeagueId, out int chatNum);
        varChatRedDot.SetDotState(chatNum > 0, chatNum);
        varChatLightText.text = "聊天";
        if (chatNum > 0)
        {
            varChatLightText.text += string.Format("<color=red>({0})</color>", chatNum);
        }
    }

    public void SwitchPanel()
    {
        varContent.SetActive(varShowContent.isOn);
        varClubChatPanel.SetActive(!varShowContent.isOn);

        varContentLine.SetActive(varShowContent.isOn);
        varChatLine.SetActive(!varShowContent.isOn);
        varContentLightText.color = varShowContent.isOn ? Color.white : Color.gray;
        varChatLightText.color = !varShowContent.isOn ? Color.white : Color.gray;
        if (!varShowContent.isOn)
        {
            ChatManager.GetInstance().SetClubChatNumByClubID(leagueInfo.LeagueId, 0);
            varClubChatPanel.GetComponent<ClubChatPanelMono>().Open();
        }
    }

    public void SwitchCoinType(bool isCoin){
        transform.Find("Content/Detail/BtnSwitch").gameObject.SetActive(leagueInfo.Father != 0);
        leagueCoin.transform.parent.gameObject.SetActive(isCoin);
        leagueGold.transform.parent.gameObject.SetActive(!isCoin);
        varBtnPay.SetActive(isCoin);
        varBtnApplyFor.SetActive(!isCoin && !Util.IsMySelf(leagueInfo.Creator));
    }

    public async void OpenClubChatPanel()
    {
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("leagueInfo", leagueInfo.ToByteArray());
        await GF.UI.OpenUIFormAwait(UIViews.ClubChatPanel, uiParams);
    }

    protected override async void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("leagueInfo", leagueInfo.ToByteArray());
        switch (btId)
        {
            case "Detail":
                //  会长查看俱乐部的  玩家查看个人的需要PlayerId
                if (!Util.IsMySelf(leagueInfo.Creator))
                {
                    uiParams.Set<VarInt64>("playerID", Util.GetMyselfInfo().PlayerId);
                }
                uiParams.Set<VarInt32>("coinType", 2);
                await GF.UI.OpenUIFormAwait(UIViews.XXXDetailsPanel, uiParams);
                break;
            case "BtnOpenCreatePanel":
                await GF.UI.OpenUIFormAwait(UIViews.CreateRoom, uiParams);
                break;
            case "OpenDetailPanelContentBtn":
                await GF.UI.OpenUIFormAwait(UIViews.ClubDetailPanelContent, uiParams);
                break;
            case "BtnPay":
                await GF.UI.OpenUIFormAwait(UIViews.ShopPanel);
                break;
            case "BtnApplyFor":
                await GF.UI.OpenUIFormAwait(UIViews.ClubPlayerCoinOpPanel, uiParams);
                break;
            case "BtnUnLock":
                Msg_LockRq req = MessagePool.Instance.Fetch<Msg_LockRq>();
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                    (HotfixNetworkComponent.AccountClientName, MessageID.Msg_LockRq, req);
                break;
            case "BtnLock":
                Util.GetInstance().CheckSafeCodeState();
                break;
            case "BtnSwitch":
                SwitchCoinType(!varBtnPay.activeSelf);
                break;
        }
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
        varContentLightText.text = "牌局(" + deskCommonInfos.Count + ")";
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
            // 全部模式下过滤掉横屏游戏（麻将、跑得快），当前界面为竖屏
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

    private void OnContentToggleChanged(bool isOn)
    {
        SwitchPanel();
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

    private void OnUserDataChanged(object sender, GameEventArgs e)
    {
        var args = e as UserDataChangedEventArgs;
        switch (args.Type)
        {
            case UserDataType.SafeCodeState:
                SwitchHideState();
                GF.LogInfo("收到 钱包密码解锁事件");
                break;
        }
    }

    private void OnGFEventChanged(object sender, GameEventArgs e)
    {
        var args = e as GFEventArgs;
        switch (args.EventType)
        {
            case GFEventType.eve_SynClubChatNum:
                SetChatNum();
                break;

        }
    }

}

using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Collections;
using NetMsg;
using Tacticsoft;
using UnityEngine;
using UnityEngine.UI;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class MainInterface : MonoBehaviour
{
    public Toggle varAll;
    public Toggle varNiuniu;
    public Toggle varJinhua;
    public Toggle varDezhou;
    public Toggle varBiji;
  
    private int methodType = -1;
    private RepeatedField<DeskCommonInfo> deskCommonInfos;
    private List<DeskCommonInfo> deskCommonInfosCache = new();
 
    public void Init(){
        InitializeScroll();
        HotfixNetworkComponent.AddListener(MessageID.Msg_DeskListRs, Function_DeskListRs);
        // 初始化 Toggle 监听事件
        varAll.onValueChanged.AddListener(OnAllToggleChanged);
        varNiuniu.onValueChanged.AddListener(OnNiuToggleChanged);
        varJinhua.onValueChanged.AddListener(OnJHToggleChanged);
        varDezhou.onValueChanged.AddListener(OnDZToggleChanged);
        varBiji.onValueChanged.AddListener(OnBijiToggleChanged);
        // Send_DeskListRq();
    }


    public void Clear() {
        varScroll.ClearAllRows();
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_DeskListRs, Function_DeskListRs);
        varAll.onValueChanged.RemoveListener(OnAllToggleChanged);
        varNiuniu.onValueChanged.RemoveListener(OnNiuToggleChanged);
        varJinhua.onValueChanged.RemoveListener(OnJHToggleChanged);
        varDezhou.onValueChanged.RemoveListener(OnDZToggleChanged);
        varBiji.onValueChanged.RemoveListener(OnBijiToggleChanged);
    }

    public void OnEnable()
    {   //获取桌子列表
        Send_DeskListRq();
    }

    /// <summary>
    /// 牌桌列表
    /// </summary>
    /// <param name="data"></param>

    public void Send_DeskListRq(int leagureId = 0)
    {
        // Msg_DeskListRq
        Msg_DeskListRq req = MessagePool.Instance.Fetch<Msg_DeskListRq>();
        req.LeagureId = leagureId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_DeskListRq, req);
    }
    public void Function_DeskListRs(MessageRecvData data)
    {
        // Msg_DeskListRs
        Msg_DeskListRs ack = Msg_DeskListRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_DeskListRs收到消息牌桌列表:" , ack.ToString());
        deskCommonInfos = ack.DeskInfos;
        ShowMyDeskList();
    }

    public void ShowMyDeskList()
    {
        deskCommonInfosCache = new();
        if (methodType != -1)
        {
            deskCommonInfosCache = deskCommonInfos
            .Where(info => info.MethodType == (MethodType)methodType)
            .OrderByDescending(info => info.State == DeskState.StartRun)
            .ToList();
        }
        else{
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
    public TableView varScroll;
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
        cell.SetActive(true);
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

    public async void OnClick(string btId)
    {
        switch (btId)
        {
            case "AddRoom":
                break;
            case "BuildRoom":
                await GF.UI.OpenUIFormAwait(UIViews.CreateRoom);
                break;
        }
    }

    private void OnAllToggleChanged(bool isOn)
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (isOn)
            UpdateTable(-1);
    }
    private void OnNiuToggleChanged(bool isOn)
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (isOn)
            UpdateTable(0);
    }
    private void OnJHToggleChanged(bool isOn)
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (isOn)
            UpdateTable(1);
    }
    private void OnDZToggleChanged(bool isOn)
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (isOn)
            UpdateTable(2);
    }
    private void OnBijiToggleChanged(bool isOn)
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (isOn)
            UpdateTable(3);
    }

    void UpdateTable(int gameType)
    {
        // GF.LogInfo("游戏选择: " + gameType);
        methodType = gameType;
        ShowMyDeskList();
    }
}

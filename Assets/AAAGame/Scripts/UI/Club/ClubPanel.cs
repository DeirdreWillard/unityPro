using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Event;
using UnityGameFramework.Runtime;
using NetMsg;
using Google.Protobuf;
using Tacticsoft;
using System.Collections.Generic;


[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class ClubPanel : MonoBehaviour
{
    private List<LeagueInfo> leagueInfos = new List<LeagueInfo>();
    public void Init()
    {
        InitializeScroll();
        HotfixNetworkComponent.AddListener(MessageID.Msg_MyLeagueListRs, FuncMyLeagueListRs);
        GF.Event.Subscribe(GFEventArgs.EventId, OnGFEventChanged);

        RedDotManager.GetInstance().RefreshAll();
    }

    public void Clear()
    {
        varScroll.ClearAllRows();
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MyLeagueListRs, FuncMyLeagueListRs);
        GF.Event.Unsubscribe(GFEventArgs.EventId, OnGFEventChanged);
    }


    public void OnEnable()
    {
        GF.LogInfo("ClubPanelOnEnable");
        Util.UpdateClubInfoRq();
    }

    /// <summary>
    /// 我的公会列表
    /// </summary>
    /// <param name="data"></param>
    private void FuncMyLeagueListRs(MessageRecvData data)
    {
        try
        {
            if (gameObject.activeSelf == false)
            {
                GF.LogInfo("ClubPanel不活跃，忽略处理联盟列表消息");
                return;
            }

            if (data == null || data.Data == null)
            {
                GF.LogError("接收到的联盟列表数据为空");
                ShowNeedCreate(); // 默认显示创建界面
                return;
            }

            Msg_MyLeagueListRs ack = null;
            try
            {
                ack = Msg_MyLeagueListRs.Parser.ParseFrom(data.Data);
            }
            catch (System.Exception ex)
            {
                GF.LogError("解析联盟列表数据失败: ", ex.Message);
                ShowNeedCreate(); // 出错时显示创建界面
                return;
            }

            if (ack != null && ack.Infos != null && ack.Infos.Count != 0)
            {
                ShowMyLeagueList(ack);
            }
            else
            {
                GF.LogInfo("联盟列表为空，显示创建界面");
                ShowNeedCreate();
            }
        }
        catch (System.Exception ex)
        {
            GF.LogError("处理联盟列表出现未知错误: ", ex.Message);
            // 确保界面不会出错
            try
            {
                ShowNeedCreate();
            }
            catch
            {
                GF.LogError("显示创建界面时出现错误，请检查界面组件");
            }
        }
    }


    //显示默认创建界面
    public void ShowNeedCreate()
    {
        transform.Find("BtnCreateAlliances").gameObject.SetActive(true);
        transform.Find("BtnCreateClub").gameObject.SetActive(true);
        transform.Find("default").gameObject.SetActive(true);
        transform.Find("Room").gameObject.SetActive(false);
    }

    #region 循环滚动
    public GameObject content;
    public TableView varScroll;
    public GameObject itemPrefab;
    private void InitializeScroll()
    {
        varScroll.SetNumberOfRowsForTableView(tv => leagueInfos.Count);
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
            cell.name = "cell";
            cell.reuseIdentifier = "cell";// 设置复用标识符
        }
        cell.gameObject.SetActive(true);
        UpdateItemData(cell.gameObject, leagueInfos[row]);
                return cell;
;
    }

    private void UpdateItemData(GameObject cell, LeagueInfo data)
    {
        cell.GetComponent<ClubItem>().Init(data);
        cell.GetComponent<Button>().onClick.RemoveAllListeners();
        cell.GetComponent<Button>().onClick.AddListener(delegate ()
        {
            this.onBtnClick(cell);
        });
    }
    #endregion

    public void ShowMyLeagueList(Msg_MyLeagueListRs ack)
    {
        transform.Find("BtnCreateAlliances").gameObject.SetActive(GlobalManager.GetInstance().MyLeagueInfos.Count == 0);
        //创建过联盟 或者创建的俱乐部加入了联盟 都不显示加入联盟按钮
        transform.Find("BtnCreateClub").gameObject.SetActive(GlobalManager.GetInstance().MyLeagueInfos.Count == 0);
        transform.Find("default").gameObject.SetActive(false);
        transform.Find("Room").gameObject.SetActive(true);
        leagueInfos = ack.Infos
            .OrderByDescending(info => info.Creator == Util.GetMyselfInfo().PlayerId) // 自己的放最上面
            .ThenByDescending(info => info.Type)    //优先联盟
            .ToList();
        varScroll.ScrollY = 0;
        varScroll.ReloadData();
    }

    public async void onBtnClick(GameObject sender)
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        LeagueInfo leagueInfo = sender.GetComponent<ClubItem>().leagueInfoData;
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("leagueInfo", leagueInfo.ToByteArray());
        GlobalManager.GetInstance().LeagueInfo = leagueInfo;
        if (leagueInfo.Creator == Util.GetMyselfInfo().PlayerId){
            switch (leagueInfo.Type)
            {
                case 0:
                    await GF.UI.OpenUIFormAwait(UIViews.ClubDetailPanel, uiParams);
                    break;
                case 1:
                    await GF.UI.OpenUIFormAwait(UIViews.AlliancesDetailPanel, uiParams);
                    break;
                case 2:
                    await GF.UI.OpenUIFormAwait(UIViews.SuperAlliancesDetailPanel, uiParams);
                    break;
            }
        }
        else
        {
            await GF.UI.OpenUIFormAwait(UIViews.ClubDetailPanel, uiParams);
        }
    }

    public async void OnClick(string btId)
    {
        switch (btId)
        {
            case "CreateAlliances":
                await GF.UI.OpenUIFormAwait(UIViews.CreateAlliances);
                break;
            case "CreateClub":
                await GF.UI.OpenUIFormAwait(UIViews.CreateClub);
                break;
            case "JoinClub":
                await GF.UI.OpenUIFormAwait(UIViews.JoinClub);
                break;
        }
    }

    private void OnGFEventChanged(object sender, GameEventArgs e)
    {
        var args = e as GFEventArgs;
        switch (args.EventType)
        {
            case GFEventType.eve_SynClubChatNum:
                if (gameObject.activeSelf == false) return;
                varScroll.ScrollY = 0;
                varScroll.ReloadData();
                break;

        }
    }

}

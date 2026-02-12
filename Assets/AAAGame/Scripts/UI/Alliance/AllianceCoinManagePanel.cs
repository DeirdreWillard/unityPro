
using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using Google.Protobuf;
using GameFramework.Event;
using System.Collections.Generic;
using Tacticsoft;
using System.Linq;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class AllianceCoinManagePanel : UIFormBase
{
    LeagueInfo leagueInfo;

    private List<LeagueInfo> leagueInfos = new();
    private List<LeagueInfo> leagueInfos_show = new();
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        InitializeScroll();
        HotfixNetworkComponent.AddListener(MessageID.Msg_GetUnionClubListRs, Function_MyLeagueListRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_GetSuperUnionListRs, Function_MyLeagueListRs);

        GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);

        leagueInfo = GlobalManager.GetInstance().LeagueInfo;

        if (leagueInfo.Type == 1)
        {
            Msg_GetUnionClubListRq req = MessagePool.Instance.Fetch<Msg_GetUnionClubListRq>();
            req.UnionId = leagueInfo.LeagueId;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GetUnionClubListRq, req);
        }
        else if(leagueInfo.Type == 2)
        {
            Msg_GetSuperUnionListRq req = MessagePool.Instance.Fetch<Msg_GetSuperUnionListRq>();
            req.UnionId = leagueInfo.LeagueId;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GetSuperUnionListRq, req);
        }

        varInputFieldLegacy.onValueChanged.RemoveAllListeners();
        varInputFieldLegacy.onValueChanged.AddListener(delegate
        {
            if (varInputFieldLegacy.text.Length > 0)
            {
                ShowList(varInputFieldLegacy.text);
            }
            else
            {
                ShowList();
            }
        });
        varInputFieldLegacy.text = "";
        varInputFieldLegacy.placeholder.GetComponent<Text>().text = leagueInfo.Type == 1 ? "输入俱乐部ID搜索" : "输入联盟ID搜索";
        InitPage();
        Util.GetInstance().CheckSafeCodeState(gameObject);
    }

    void InitPage()
    {
        varTxtName.text = leagueInfo.LeagueName;
        varTxtID.text = "ID: " + leagueInfo.LeagueId;
        varTxtCoinValue.text = "联盟币余额: " + GlobalManager.GetInstance().GetAllianceCoins(leagueInfo.LeagueId);
        Util.DownloadHeadImage(varAvatar, leagueInfo.LeagueHead, leagueInfo.Type);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        varScroll.ClearAllRows();
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_GetUnionClubListRs, Function_MyLeagueListRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_GetSuperUnionListRs, Function_MyLeagueListRs);
        GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
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
                InitPage();
                break;
        }
    }

    private void Function_MyLeagueListRs(MessageRecvData data)
    {
        // Msg_DeskListRs
        Msg_GetUnionClubListRs ack = Msg_GetUnionClubListRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到联盟俱乐部列表:" , ack.ToString());
        leagueInfos = ack.Infos.ToList();
        leagueInfos_show = leagueInfos;
        ShowList();
    }

    public void ShowList(string findID = "")
    {
        if (!string.IsNullOrEmpty(findID))
        {
            leagueInfos_show = new();
            for (int i = 0; i < leagueInfos.Count; i++)
            {
                string leagueIdStr = leagueInfos[i].LeagueId.ToString();
                if (leagueIdStr.StartsWith(findID))
                {
                    leagueInfos_show.Add(leagueInfos[i]);
                }
            }
        }
        else
        {
            leagueInfos_show = leagueInfos;
        }
        varScroll.ScrollY = 0;
        varScroll.ReloadData();
    }


    #region 循环滚动
    public GameObject content;
    private void InitializeScroll()
    {
        varScroll.SetNumberOfRowsForTableView(tv => leagueInfos_show.Count);
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
        UpdateItemData(cell.gameObject, leagueInfos_show[row]);
                return cell;
;
    }

    private async void UpdateItemData(GameObject cell, LeagueInfo data)
    {
        cell.transform.Find("Nickname").GetComponent<Text>().text = data.LeagueName;
        cell.transform.Find("PlayerId").GetComponent<Text>().text = "ID: " + data.LeagueId;
        cell.transform.Find("CoinImg/Value").GetComponent<Text>().text = $"{Util.FormatAmount(data.LeagueGold)} ({Util.FormatAmount(data.TotalGold)})";
        //头像
        Util.DownloadHeadImage(cell.transform.Find("Mask/Head").GetComponent<RawImage>(), data.LeagueHead, data.Type);
        cell.transform.Find("BtnSetUnionCoin").GetComponent<Button>().onClick.RemoveAllListeners();
        cell.transform.Find("BtnSetUnionCoin").GetComponent<Button>().onClick.AddListener(async () =>
        {
           Sound.PlayEffect(AudioKeys.SOUND_BTN);
            //打开修改联盟币弹窗
            var uiParams = UIParams.Create();
            uiParams.Set<VarByteArray>("clubInfo", data.ToByteArray());
            uiParams.Set<VarByteArray>("leagueInfo", leagueInfo.ToByteArray());
            await GF.UI.OpenUIFormAwait(UIViews.AllianceCoinOpPanel, uiParams);
        });
    }
    #endregion

    protected override async void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("leagueInfo", leagueInfo.ToByteArray());
        switch (btId)
        {
            case "BtnClear":
                varInputFieldLegacy.text = "";
                break;
            case "FindBtn":
                //搜索
                if (varInputFieldLegacy.text.Length > 0)
                {
                    int findID = int.Parse(varInputFieldLegacy.text);
                    ShowList(findID.ToString());
                }
                else
                {
                    ShowList();
                }
                break;
            case "DetailsBtn":
                //明细
                uiParams.Set<VarInt32>("coinType", 2);
                await GF.UI.OpenUIFormAwait(UIViews.XXXDetailsPanel, uiParams);
                break;
        }
    }
}

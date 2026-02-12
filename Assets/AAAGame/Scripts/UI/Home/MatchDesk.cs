using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.Collections;
using NetMsg;
using Tacticsoft;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class MatchDesk : MonoBehaviour
{
    [SerializeField] private CarouselManager m_CarouselManager;
    [SerializeField] private Sprite[] m_AdSprites;
    private RepeatedField<Msg_Match> matches;
    private List<Msg_Match> matchesCache = new();

    public void Init()
    {
        InitializeScroll();
        m_CarouselManager.Initialize(m_AdSprites, OnAdClicked);
        m_CarouselManager.SetAutoScroll(true, 3f);
        HotfixNetworkComponent.AddListener(MessageID.Msg_getMatchRs, Function_getMatchRs);
        HotfixNetworkComponent.AddListener(MessageID.Syn_Match, Function_syn_Match);
    }


    private void OnAdClicked(int index)
    {
        Debug.Log("点击了广告索引: " + index);
    }


    public void Clear()
    {
        varScroll.ClearAllRows();
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_getMatchRs, Function_getMatchRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_Match, Function_syn_Match);
    }

    public void OnEnable()
    {
        //获取桌子列表
        Msg_getMatchRq req = MessagePool.Instance.Fetch<Msg_getMatchRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_getMatchRq, req);
    }

    public void Function_getMatchRs(MessageRecvData data)
    {
        Msg_getMatchRs ack = Msg_getMatchRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_getMatchRs收到比赛牌桌列表:", ack.ToString());
        matches = ack.Matchs;
        ShowMyDeskList();
    }

    public void Function_syn_Match(MessageRecvData data)
    {
        Syn_Match ack = Syn_Match.Parser.ParseFrom(data.Data);
        GF.LogInfo("Syn_Match收到比赛牌桌变化通知", ack.ToString());
        if (ack.Match == null)
        {
            return;
        }
        int index = matchesCache.FindIndex(m => m.MatchId == ack.Match.MatchId);
        if (index != -1)
        {
            matchesCache[index] = ack.Match;
        }
        varScroll.ReloadData();
    }

    public async void ShowMyDeskList()
    {
        matchesCache.Clear();
        matchesCache = matches.ToList();
        if (GF.Procedure.CurrentProcedure is HomeProcedure homeProcedure)
        {
            Msg_Match match = matchesCache.Find(m => m.MatchId == homeProcedure.m_matchID);
            if (match != null)
            {
                var uiParams = UIParams.Create();
                uiParams.Set<VarByteArray>("matchConfig", match.ToByteArray());
                await GF.UI.OpenUIFormAwait(UIViews.MatchContentPanel, uiParams);
            }
            homeProcedure.m_matchID = 0;
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
        varScroll.SetNumberOfRowsForTableView(tv => matchesCache.Count);
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
        UpdateItemData(cell.gameObject, matchesCache[row]);
        return cell;
    }

    private void UpdateItemData(GameObject cell, Msg_Match data)
    {
        cell.SetActive(true);
        cell.GetComponent<MatchDeskItem>().Init(data);
        cell.GetComponent<Button>().onClick.RemoveAllListeners();
        cell.GetComponent<Button>().onClick.AddListener(async () =>
        {
            if (Util.IsClickLocked()) return;
           Sound.PlayEffect(AudioKeys.SOUND_BTN);
            var uiParams = UIParams.Create();
            uiParams.Set<VarByteArray>("matchConfig", data.ToByteArray());
            await GF.UI.OpenUIFormAwait(UIViews.MatchContentPanel, uiParams);
        });
    }
    #endregion


}

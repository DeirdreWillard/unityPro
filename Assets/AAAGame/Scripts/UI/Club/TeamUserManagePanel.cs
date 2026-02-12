using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using UnityGameFramework.Runtime;
using Google.Protobuf;
using System.Collections.Generic;
using Tacticsoft;
using GameFramework.Event;
using UnityEngine.EventSystems;
using UnityEngine.Assertions.Must;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class TeamUserManagePanel : UIFormBase
{
    public LeagueInfo leagueInfo;
    List<LeagueUser> leagueUsers = new();
    long leaderId = 0;
    float rebate = 0;
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        InitializeScroll();
        InitializeSliderEvents();
        GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        HotfixNetworkComponent.AddListener(MessageID.Msg_ClubTeamUserRs, FunctionMsg_ClubTeamUserRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SetClubTeamPlayerRs, FunctionMsg_SetClubTeamPlayerRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SetClubTeamRebateRs, FunctionMsg_SetClubTeamRebateRs);
        leagueInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("leagueInfo"));
        rebate = Params.Get<VarFloat>("rebate");
        leaderId = Params.Get<VarInt64>("leaderId");
        varCreateObj.SetActive(Util.IsMySelf(leagueInfo.Creator));
        varTeamObj.SetActive(Util.IsMySelf(leaderId));
        varAddTeam.SetActive(Util.IsMySelf(leagueInfo.Creator));
        SendMsg_ClubTeamUserRq();
        Util.GetInstance().CheckSafeCodeState(gameObject);
    }

    public void SendMsg_ClubTeamUserRq()
    {
        GF.LogInfo("请求 战队成员列表");
        Msg_ClubTeamUserRq req = MessagePool.Instance.Fetch<Msg_ClubTeamUserRq>();
        req.ClubId = leagueInfo.LeagueId;
        req.TeamLeader = leaderId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ClubTeamUserRq, req);
    }


    #region 循环滚动
    public GameObject content;
    private void InitializeScroll()
    {
        varScroll.SetNumberOfRowsForTableView(tv => leagueUsers.Count);
        varScroll.SetHeightForRowInTableView((tv, row) => varTeamUserManageItem.GetComponent<RectTransform>().rect.height);
        varScroll.SetCellForRowInTableView(CellForRowInTableView);       // 获取指定行的 UI
    }
    public TableViewCell CellForRowInTableView(TableView tv, int row)
    {
        TableViewCell cell = tv.GetReusableCell("cell");
        if (cell == null)
        {
            GameObject go = Instantiate(varTeamUserManageItem, content.transform);
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
        cell.GetComponent<TeamUserManageItem>().Init(data);
        cell.transform.Find("Bg/BtnKickOut").gameObject.SetActive(Util.IsMySelf(leagueInfo.Creator));
        cell.transform.Find("Bg/ButtonCoinOp").GetComponent<Button>().onClick.RemoveAllListeners();
        cell.transform.Find("Bg/ButtonCoinOp").GetComponent<Button>().onClick.AddListener(async () =>
            {
               Sound.PlayEffect(AudioKeys.SOUND_BTN);
                var uiParams = UIParams.Create();
                uiParams.Set<VarByteArray>("leagueInfo", leagueInfo.ToByteArray());
                uiParams.Set<VarByteArray>("player", data.ToByteArray());
                await GF.UI.OpenUIFormAwait(UIViews.ClubCoinOpPanel, uiParams);
            });
        cell.transform.Find("Bg/BtnKickOut").GetComponent<Button>().onClick.RemoveAllListeners();
        cell.transform.Find("Bg/BtnKickOut").GetComponent<Button>().onClick.AddListener(delegate ()
        {
           Sound.PlayEffect(AudioKeys.SOUND_BTN);
            Util.GetInstance().OpenConfirmationDialog("移除成员", $"确定要移除成员 <color=#D0FE36>{data.NickName}</color>", () =>
            {
                Msg_SetClubTeamPlayerRq req = MessagePool.Instance.Fetch<Msg_SetClubTeamPlayerRq>();
                req.PlayerId = data.PlayerId;
                req.ClubId = leagueInfo.LeagueId;
                req.LeaderId = leaderId;
                req.State = 0;
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                    (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SetClubTeamPlayerRq, req);
            });
        });
    }
    #endregion

    #region 滑块事件处理
    private void InitializeSliderEvents()
    {
        EventTrigger trigger = varPopSlider.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = varPopSlider.gameObject.AddComponent<EventTrigger>();
        }
        // 避免重复添加
        trigger.triggers.RemoveAll(e => e.eventID == EventTriggerType.PointerUp);
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerUp;
        entry.callback.AddListener((data) => { SetTeamRebate(); });
        trigger.triggers.Add(entry);
    }

    public void ScoreProSliderChange()
    {
        int pro = (int)varPopSlider.value;
        varClubRate.text = "俱乐部: " + (100 - pro) + "%";
        varTeamRate.text = "战队: " + pro + "%";
    }

    #endregion

    public void SetTeamRebate()
    {
        GF.LogInfo("设置战队分成");
        Util.GetInstance().OpenConfirmationDialog("设置分成", $"确定要设置分成 <color=#D0FE36>{varPopSlider.value}%</color>",
        () =>
        {
            GF.LogInfo("设置分成");
            Msg_SetClubTeamRebateRq req = MessagePool.Instance.Fetch<Msg_SetClubTeamRebateRq>();
            req.ClubId = leagueInfo.LeagueId;
            req.LeaderId = leaderId;
            req.Rebate = (int)varPopSlider.value;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SetClubTeamRebateRq, req);
        });
    }

    public async void OpenSetPlayerTeamPanel()
    {
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("leagueInfo", leagueInfo.ToByteArray());
        uiParams.Set<VarInt64>("leaderId", leaderId);
        uiParams.Set<VarInt32>("type", 1);
        await GF.UI.OpenUIFormAwait(UIViews.SetPlayerTeamPanel, uiParams);
    }

    private void FunctionMsg_ClubTeamUserRs(MessageRecvData data)
    {
        Msg_ClubTeamUserRs ack = Msg_ClubTeamUserRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到 战队成员列表", ack.ToString());
        varTeamObj.transform.Find("RateText").GetComponent<Text>().text = "分成: " + rebate + "%" + " " + "当日收益:" + ack.TeamDayRate + " " + "七日收益:" + ack.TeamSevenRate;
        leagueUsers.Clear();
        leagueUsers.AddRange(ack.LeagueUser);
        varPopSlider.maxValue = Mathf.Floor((float)ack.MaxReBack);
        if (rebate > varPopSlider.maxValue)
        {           
            varPopSlider.value = varPopSlider.maxValue;
            rebate = varPopSlider.maxValue;
            // Util.GetMyselfInfo().ClubLeagueDataMap[leagueInfo.LeagueId].Rebate = (int)varPopSlider.value;
            GF.UI.ShowToast("当前分成已超过最大值，已自动调整为最大值");
            Msg_SetClubTeamRebateRq req = MessagePool.Instance.Fetch<Msg_SetClubTeamRebateRq>();
            req.ClubId = leagueInfo.LeagueId;
            req.LeaderId = leaderId;
            req.Rebate =  (int)rebate;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SetClubTeamRebateRq, req);
        }
        else
        {
            varPopSlider.value = rebate;
        }
        ScoreProSliderChange();
        varScroll.ReloadData();
    }

    public void FunctionMsg_SetClubTeamPlayerRs(MessageRecvData data)
    {
        GF.LogInfo("设置战队成员刷新列表");
        SendMsg_ClubTeamUserRq();
    }

    public void FunctionMsg_SetClubTeamRebateRs(MessageRecvData data)
    {
        GF.LogInfo("设置战队返利刷新列表");
        GF.LogInfo("请求 战队列表");
        Msg_ClubTeamRq req = MessagePool.Instance.Fetch<Msg_ClubTeamRq>();
        req.ClubId = leagueInfo.LeagueId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ClubTeamRq, req);
        rebate = (int)varPopSlider.value;
        Util.GetMyselfInfo().ClubLeagueDataMap[leagueInfo.LeagueId].Rebate = (int)varPopSlider.value;
        GF.UI.ShowToast("修改成功");
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        varScroll.ClearAllRows();
        GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_ClubTeamUserRs, FunctionMsg_ClubTeamUserRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SetClubTeamPlayerRs, FunctionMsg_SetClubTeamPlayerRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SetClubTeamRebateRs, FunctionMsg_SetClubTeamRebateRs);
        base.OnClose(isShutdown, userData);
    }

    private void OnClubDataChanged(object sender, GameEventArgs e)
    {
        var args = e as ClubDataChangedEventArgs;
        switch (args.Type)
        {
            case ClubDataType.eve_LeagueUsersUpdate:
                GF.LogInfo("俱乐部成员更新");
                leagueInfo = GlobalManager.GetInstance().LeagueInfo;
                SendMsg_ClubTeamUserRq();
                break;
        }
    }
}

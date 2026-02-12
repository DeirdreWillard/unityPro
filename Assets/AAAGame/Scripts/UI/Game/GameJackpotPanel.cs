﻿using UnityEngine.UI;
using NetMsg;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityGameFramework.Runtime;


[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class GameJackpotPanel : UIFormBase
{
    /// <summary>
    /// 当前游戏类型
    /// </summary>
    private MethodType methodType;
    /// <summary>
    /// 选择页签
    /// </summary>
    private MethodType chooseMethodType = MethodType.TexasPoker;

    Desk_BaseConfig deskConfig;

    Msg_Get_JackpotRs msg_Get_JackpotRs = null;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        isInitJpInfo = false;
        methodType = (MethodType)(int)Params.Get<VarInt32>("methodType");
        deskConfig = Desk_BaseConfig.Parser.ParseFrom(Params.Get<VarByteArray>("deskConfig"));

        switch (methodType)
        {
            case MethodType.NiuNiu:
                varJackpotTitle.text = "牛牛分池";
                chooseMethodType = MethodType.NiuNiu;
                varNiuniuToggle.isOn = true;
                break;
            case MethodType.GoldenFlower:
                varJackpotTitle.text = "金花分池";
                chooseMethodType = MethodType.GoldenFlower;
                varJinhuaToggle.isOn = true;
                break;
            case MethodType.TexasPoker:
                varJackpotTitle.text = "德州分池";
                chooseMethodType = MethodType.TexasPoker;
                varDezhouToggle.isOn = true;
                break;
            case MethodType.CompareChicken:
                varJackpotTitle.text = "比鸡分池";
                chooseMethodType = MethodType.CompareChicken;
                varBijiToggle.isOn = true;
                break;
        }

        varToggle1.onValueChanged.AddListener(OnToggle1Changed);
        varToggle2.onValueChanged.AddListener(OnToggle2Changed);
        varToggle3.onValueChanged.AddListener(OnToggle3Changed);
        varDezhouToggle.onValueChanged.AddListener(OnDezhouToggleChanged);
        varNiuniuToggle.onValueChanged.AddListener(OnNNToggleChanged);
        varJinhuaToggle.onValueChanged.AddListener(OnJinhuaToggleChanged);
        varBijiToggle.onValueChanged.AddListener(OnBijiToggleChanged);
        HotfixNetworkComponent.AddListener(MessageID.Msg_JackPotReportRs, Function_JackPotReportRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_Get_JackpotRs, Function_Get_JackpotRs);
        HotfixNetworkComponent.AddListener(MessageID.Syn_Jackpot, Function_Syn_Jackpot);

        SendGetJackpotRq();
        SendJackPotReportRq();

        UpdateTable(1);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        msg_Get_JackpotRs = null;
        varToggle1.onValueChanged.RemoveAllListeners();
        varToggle2.onValueChanged.RemoveAllListeners();
        varToggle3.onValueChanged.RemoveAllListeners();
        varDezhouToggle.onValueChanged.RemoveAllListeners();
        varNiuniuToggle.onValueChanged.RemoveAllListeners();
        varJinhuaToggle.onValueChanged.RemoveAllListeners();
        varBijiToggle.onValueChanged.RemoveAllListeners();
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_JackPotReportRs, Function_JackPotReportRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_Get_JackpotRs, Function_Get_JackpotRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_Jackpot, Function_Syn_Jackpot);
        base.OnClose(isShutdown, userData);
    }

    public void SendGetJackpotRq()
    {
        Msg_Get_JackpotRq req = MessagePool.Instance.Fetch<Msg_Get_JackpotRq>();
        req.UnionId = deskConfig.ClubId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_Get_JackpotRq, req);
    }

    public void SendJackPotReportRq()
    {
        Msg_JackPotReportRq req = MessagePool.Instance.Fetch<Msg_JackPotReportRq>();
        req.ClubId = deskConfig.ClubId;
        req.MethodType = methodType;
        req.BaseCoin = deskConfig.BaseCoin;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_JackPotReportRq, req);
    }

    public bool isInitJpInfo = false;
    public void InitJpInfoPanel()
    {
        isInitJpInfo = true;
        List<Msg_Jackpot> jackpots = msg_Get_JackpotRs.Jackpot.Where(item => item.Type == methodType).ToList();
        var match = jackpots.FirstOrDefault(item => float.TryParse(item.BaseCoin, out float baseCoin) && baseCoin == deskConfig.BaseCoin);
        varJackpotRoller2.SetData(float.Parse(match.Pot));
        varBaseCoinText.text = "底注级别:    " + float.Parse(match.BaseCoin) + "/" + (float.Parse(match.BaseCoin) * 2);
        CardRate cardRate = msg_Get_JackpotRs.CardRate.FirstOrDefault(item => item.Type == methodType);
        List<PairInt> sortedReports ;
        if(methodType == MethodType.CompareChicken){
            int[] sortOrder = new int[] { 14, 7, 10, 5 };
            // 按照sortOrder中的顺序排序，没有在sortOrder中的排在后面
            sortedReports = cardRate.RateConfig
                .OrderBy(rate => {
                    int index = System.Array.IndexOf(sortOrder, rate.Key);
                    return index == -1 ? int.MaxValue : index;
                })
                .ToList();
        }
        else{
            sortedReports = cardRate.RateConfig.OrderByDescending(rate => rate.Key).ToList();
        }
        //创建列表
        foreach (Transform item in varCardTypeContent.transform)
        {
            if (item.name == "CardTypeTop")
            {
                continue;
            }
            Destroy(item.gameObject);
        }
        for (int i = 0; i < sortedReports.Count; i++)
        {
            var data = sortedReports[i];
            //比鸡特殊处理
            GameObject newItem = methodType == MethodType.CompareChicken ? Instantiate(varCardTypeItem_bj) : Instantiate(varCardTypeItem);
            newItem.transform.SetParent(varCardTypeContent.transform, false);
            newItem.transform.Find("Bg/Fill").GetComponent<Image>().fillAmount = Mathf.Clamp((float)data.Val / 100, 0, 1);
            newItem.transform.Find("Bg/Rate").GetComponent<Text>().text = data.Val + "%";
            newItem.transform.Find("Name").GetComponent<Text>().text = GameUtil.ShowType(data.Key, methodType);
            List<string> cards = GameUtil.GetCardsByType(data.Key, methodType);
            Transform cardContent = newItem.transform.Find("Cards");
            for (int j = 0; j < cardContent.childCount; j++)
            {
                if (cards.Count > j && !string.IsNullOrEmpty(cards[j]))
                {
                    cardContent.GetChild(j).gameObject.SetActive(true);
                    cardContent.GetChild(j).GetComponent<Card>().InitByName(cards[j]);
                }
                else
                {
                    cardContent.GetChild(j).gameObject.SetActive(false);
                }
            }
        }
        // varCardTypeTop.transform.SetAsFirstSibling();
    }

    public void Function_Syn_Jackpot(MessageRecvData data)
    {
        Syn_Jackpot ack = Syn_Jackpot.Parser.ParseFrom(data.Data);
        // GF.LogInfo("Syn_Jackpot奖池信息更新" , ack.ToString());
        if (ack.Jackpot.Type != chooseMethodType) return;
        foreach (var item in msg_Get_JackpotRs.Jackpot)
        {
            if (item.BaseCoin == ack.Jackpot.BaseCoin && item.Type == ack.Jackpot.Type)
            {
                item.Pot = ack.Jackpot.Pot;
                if (float.Parse(item.BaseCoin) == deskConfig.BaseCoin && item.Type == methodType)
                {
                    varJackpotRoller2.SetData(float.Parse(item.Pot));
                }
                foreach (Transform tr in varJpItemContent.transform)
                {
                    if (tr.name == item.BaseCoin)
                    {
                        tr.transform.Find("InputField/Value").GetComponent<Text>().text = float.Parse(item.Pot).ToString();
                        break;
                    }
                }
            }
        }
    }

    public void Function_Get_JackpotRs(MessageRecvData data)
    {
        Msg_Get_JackpotRs ack = Msg_Get_JackpotRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_Get_JackpotRs奖池信息", ack.ToString());
        msg_Get_JackpotRs = ack;
        UpdateJpList();
    }

    private void Function_JackPotReportRs(MessageRecvData data)
    {
        // Msg_DeskListRs
        Msg_JackPotReportRs ack = Msg_JackPotReportRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到奖池记录返回:", ack.ToString());
        InitJpReportPanel(ack);
    }

    /// <summary>
    /// 奖池记录
    /// </summary>
    /// <param name="ack"></param>
    public void InitJpReportPanel(Msg_JackPotReportRs ack)
    {
        varTop.transform.Find("Name").GetComponent<Text>().text = "虚位以待";
        varTop.transform.Find("BaseCoin").GetComponent<Text>().text = "";
        varTop.transform.Find("Type").GetComponent<Text>().text = "";
        varTop.transform.Find("Jackpot").GetComponent<Text>().text = "";
        varTop.transform.Find("Time").GetComponent<Text>().text = "";
        //创建列表
        foreach (Transform item in varContent.transform)
        {
            Destroy(item.gameObject);
        }
        var maxReport = ack.Report
                    .OrderByDescending(report => float.TryParse(report.JackPot, out var value) ? value : float.MinValue)
                    .FirstOrDefault();
        if (maxReport != null)
        {
            varTop.transform.Find("Name").GetComponent<Text>().FormatNickname(maxReport.BasePlayer.Nick);
            varTop.transform.Find("BaseCoin").GetComponent<Text>().text = maxReport.BaseCoin + "/" + (maxReport.BaseCoin * 2);
            varTop.transform.Find("Type").GetComponent<Text>().text = GameUtil.ShowType(maxReport.CardType, maxReport.Type);
            varTop.transform.Find("Jackpot").GetComponent<Text>().text = maxReport.JackPot;
            varTop.transform.Find("Time").GetComponent<Text>().text = Util.MillisecondsToDateString(maxReport.OpTime, "MM/dd");
        }
        var sortedReports = ack.Report
                           .OrderByDescending(report => report.OpTime)
                           .ToList();
        for (int i = 0; i < sortedReports.Count; i++)
        {
            var data = sortedReports[i];
            GameObject newItem = Instantiate(varRecordItem);
            newItem.transform.SetParent(varContent.transform, false);
            newItem.transform.Find("Name").GetComponent<Text>().FormatNickname(data.BasePlayer.Nick);
            newItem.transform.Find("BaseCoin").GetComponent<Text>().text = data.BaseCoin + "/" + (data.BaseCoin * 2);
            newItem.transform.Find("Type").GetComponent<Text>().text = GameUtil.ShowType(data.CardType, data.Type);
            newItem.transform.Find("Jackpot").GetComponent<Text>().text = data.JackPot;
            newItem.transform.Find("Time").GetComponent<Text>().text = Util.MillisecondsToDateString(data.OpTime, "MM/dd");
        }
    }
    private void OnToggle1Changed(bool isOn)
    {
        if (isOn)
            UpdateTable(1);
    }
    private void OnToggle2Changed(bool isOn)
    {
        if (isOn)
            UpdateTable(2);
    }
    private void OnToggle3Changed(bool isOn)
    {
        if (isOn)
            UpdateTable(3);
    }

    void UpdateTable(int type)
    {
        varPanel1.SetActive(type == 1);
        varPanel2.SetActive(type == 2);
        varPanel3.SetActive(type == 3);
        varCheckShow1.SetActive(type == 1);
        varCheckShow2.SetActive(type == 2);
        varCheckShow3.SetActive(type == 3);
        if (type == 2 && !isInitJpInfo)
        {
            InitJpInfoPanel();
        }
    }

    private void OnDezhouToggleChanged(bool isOn)
    {
        if (isOn)
        {
            chooseMethodType = MethodType.TexasPoker;
            UpdateJpList();
        }
    }
    private void OnNNToggleChanged(bool isOn)
    {
        if (isOn)
        {
            chooseMethodType = MethodType.NiuNiu;
            UpdateJpList();
        }
    }
    private void OnJinhuaToggleChanged(bool isOn)
    {
        if (isOn)
        {
            chooseMethodType = MethodType.GoldenFlower;
            UpdateJpList();
        }
    }
    private void OnBijiToggleChanged(bool isOn)
    {
        if (isOn)
        {
            chooseMethodType = MethodType.CompareChicken;
            UpdateJpList();
        }
    }
    void UpdateJpList()
    {
        List<Msg_Jackpot> jackpots = msg_Get_JackpotRs.Jackpot.Where(item => item.Type == chooseMethodType).ToList();
        InitList(jackpots);
        varRules.text = GlobalManager.GetInstance().GetMethodRules(chooseMethodType);
        LayoutRebuilder.ForceRebuildLayoutImmediate(varRules.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(varJpItemContent.GetComponentInParent<RectTransform>());
        varJpItem.transform.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 1;
    }

    public void InitList(List<Msg_Jackpot> jackpots)
    {
        jackpots.Sort((a, b) => float.Parse(a.BaseCoin).CompareTo(float.Parse(b.BaseCoin)));
        //创建列表
        for (int i = 0; i < varJpItemContent.transform.childCount; i++)
        {
            GameObject item = varJpItemContent.transform.GetChild(i).gameObject;
            if (i < jackpots.Count)
            {
                item.SetActive(true);
                var data = jackpots[i];
                item.name = data.BaseCoin;
                item.transform.Find("BaseCoin").GetComponent<Text>().text = float.Parse(data.BaseCoin) + "/" + (float.Parse(data.BaseCoin) * 2);
                item.transform.Find("InputField/Value").GetComponent<Text>().text = float.Parse(data.Pot).ToString();
            }
            else
            {
                item.SetActive(false);
            }
        }
        var potSum = jackpots.Sum(item => float.TryParse(item.Pot, out var pot) ? pot : 0);
        varJackpotRoller.SetData(potSum);
    }


}

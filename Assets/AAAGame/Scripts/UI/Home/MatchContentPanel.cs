using System.Collections.Generic;
using DG.Tweening;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using System;
using System.Linq;
using GameFramework.Event;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class MatchContentPanel : UIFormBase
{
    public enum EventPanelType
    {
        Info = 1,
        Award,
        Player,
        Desk,
    }
    #region Private Fields
    private Dictionary<EventPanelType, GameObject> m_PanelDict;
    private Dictionary<EventPanelType, Vector3> m_PanelInitPosDict;
    private EventPanelType m_CurrentPanelType = EventPanelType.Info;
    private bool m_IsAnimating = false;
    private Coroutine m_CurrentCountdownCoroutine; // 当前运行的倒计时协程
    #endregion

    private Msg_Match m_Match = new Msg_Match();

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        m_PanelDict = new Dictionary<EventPanelType, GameObject>
        {
            { EventPanelType.Info, varInfoPanel },
            { EventPanelType.Award, varAwardPanel },
            { EventPanelType.Player, varPlayerPanel },
            { EventPanelType.Desk, varDesksPanel },
        };
        m_PanelInitPosDict = new Dictionary<EventPanelType, Vector3>();
        foreach (var kv in m_PanelDict)
        {
            m_PanelInitPosDict[kv.Key] = kv.Value.transform.localPosition;
        }

        GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        HotfixNetworkComponent.AddListener(MessageID.Syn_Match, Function_syn_Match);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MatchRewardRs, Function_Msg_MatchRewardRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MatchRankListRs, Function_Msg_MatchRankListRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MatchDeskListRs, Function_Msg_MatchDeskListRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SignUpMatchRs, Function_Msg_SignUpMatchRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynMatchEnd, Function_Msg_SynMatchEnd);

        m_Match = Msg_Match.Parser.ParseFrom(Params.Get<VarByteArray>("matchConfig"));

        // 初始化按钮状态
        ResetPage(EventPanelType.Info, false);
        InitializeButtonStates();
        Util.UpdateClubInfoRq();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        StopStartCountdown(); // 停止开赛倒计时
        
        if (varChooseClubPanel.activeSelf)
        {
            HideChooseClubPanel();
        }
        if (varSignUpConfirmationDialog.activeSelf)
        {
            HideSignUpConfirmationDialog();
        }
        // 停止倒计时协程
        if (m_CurrentCountdownCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(m_CurrentCountdownCoroutine);
            m_CurrentCountdownCoroutine = null;
        }
        GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_Match, Function_syn_Match);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MatchRewardRs, Function_Msg_MatchRewardRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MatchRankListRs, Function_Msg_MatchRankListRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MatchDeskListRs, Function_Msg_MatchDeskListRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SignUpMatchRs, Function_Msg_SignUpMatchRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynMatchEnd, Function_Msg_SynMatchEnd);
        base.OnClose(isShutdown, userData);
    }

    private void OnClubDataChanged(object sender, GameEventArgs e)
    {
        var args = e as ClubDataChangedEventArgs;
        switch (args.Type)
        {
            case ClubDataType.eve_LeagueDateUpdate:
                if (gameObject.activeSelf == false)
                {
                    GF.LogInfo("MatchContentPanel不活跃，忽略处理联盟列表消息");
                    return;
                }
                InitSignUp();
                break;
        }
    }
    private long m_ChooseClubId = 0;

    public void InitSignUp()
    {

        foreach (Transform item in varChooseClubScroll.content.transform)
        {
            Destroy(item.gameObject);
        }
        foreach (var item in GlobalManager.GetInstance().MyJoinLeagueInfos)
        {
            var data = item.Value;
            //忽略联盟(超级联盟)
            if (data.Type == 1 || data.Type == 2)
            {
                continue;
            }
            GameObject newItem = Instantiate(varClubItem);
            newItem.transform.SetParent(varChooseClubScroll.content.transform, false);
            newItem.transform.Find("ClubName").GetComponent<Text>().text = data.LeagueName;
            // newItem.transform.Find("ClubCount").GetComponent<Text>().text = GlobalManager.GetInstance().GetAllianceCoins(data.LeagueId).ToString();
            //如果没有选择俱乐部 默认第一个
            if (m_ChooseClubId == 0)
            {
                m_ChooseClubId = data.LeagueId;
                newItem.transform.Find("choose").gameObject.SetActive(true);
                newItem.transform.Find("choose2").gameObject.SetActive(true);
            }
            else
            {
                newItem.transform.Find("choose").gameObject.SetActive(m_ChooseClubId == data.LeagueId);
                newItem.transform.Find("choose2").gameObject.SetActive(m_ChooseClubId == data.LeagueId);
            }
            newItem.GetComponent<Button>().onClick.RemoveAllListeners();
            newItem.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (m_ChooseClubId == data.LeagueId)
                {
                    return;
                }
                m_ChooseClubId = data.LeagueId;
                HideChooseClubPanel();
            });
        }
    }

    public void Function_syn_Match(MessageRecvData data)
    {
        Syn_Match ack = Syn_Match.Parser.ParseFrom(data.Data);
        GF.LogInfo("Syn_Match收到比赛牌桌变化通知", ack.ToString());
        if (ack.Match.MatchId == m_Match.MatchId)
        {
            m_Match = ack.Match;
            if (m_CurrentPanelType == EventPanelType.Info)
            {
                ResetPage(EventPanelType.Info, false);
            }
        }
    }

    public void Function_Msg_MatchRewardRs(MessageRecvData data)
    {
        Msg_MatchRewardRs ack = Msg_MatchRewardRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_MatchRewardRs收到比赛奖励列表", ack.ToString());
        
        int childCount = varAwardScroll.content.transform.childCount;
        int dataCount = ack.RewardConfig.Count;
        
        // 重用或创建项目
        for (int i = 0; i < dataCount; i++)
        {
            GameObject itemObj;
            if (i < childCount)
            {
                // 重用已有物体
                itemObj = varAwardScroll.content.transform.GetChild(i).gameObject;
            }
            else
            {
                // 创建新物体
                itemObj = Instantiate(varAwardItem);
                itemObj.transform.SetParent(varAwardScroll.content.transform, false);
            }
            
            // 更新物体数据
            itemObj.transform.Find("Index").GetComponent<Text>().text = ack.RewardConfig[i].Rank == 0 ? (i + 1).ToString() : ack.RewardConfig[i].Rank.ToString();
            itemObj.transform.Find("Award").GetComponent<Text>().text = ack.RewardConfig[i].Desc;
        }
        
        // 删除多余的物体
        for (int i = childCount - 1; i >= dataCount; i--)
        {
            Destroy(varAwardScroll.content.transform.GetChild(i).gameObject);
        }
        
        var钱圈人数.text = ack.RewardConfig.Count.ToString();
    }

    public void Function_Msg_MatchRankListRs(MessageRecvData data)
    {
        Msg_MatchRankListRs ack = Msg_MatchRankListRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_MatchRankListRs收到比赛排名列表", ack.ToString());

        if(m_Match.MatchState == 3 || m_Match.MatchState == 4){
            for (int i = 0; i < 3; i++)
            {
                GameObject rankObj = varRankResult.transform.Find("Rank" + (i + 1)).gameObject;
                if (i < ack.MatchRank.Count)
                {
                    Util.DownloadHeadImage(rankObj.transform.Find("mask/avatar").GetComponent<RawImage>(), ack.MatchRank[i].PlayerInfo.HeadImage);
                    rankObj.transform.Find("Name").GetComponent<Text>().text = ack.MatchRank[i].PlayerInfo.Nick;
                }
                else {
                    rankObj.transform.Find("Name").GetComponent<Text>().text = "虚位以待";
                }
            }
        }
        
        int childCount = varPlayerScroll.content.transform.childCount;
        int dataCount = ack.MatchRank.Count;
        
        // 重用或创建项目
        for (int i = 0; i < dataCount; i++)
        {
            GameObject itemObj;
            if (i < childCount)
            {
                // 重用已有物体
                itemObj = varPlayerScroll.content.transform.GetChild(i).gameObject;
            }
            else
            {
                // 创建新物体
                itemObj = Instantiate(varPlayerItem);
                itemObj.transform.SetParent(varPlayerScroll.content.transform, false);
            }
            
            // 更新物体数据
            itemObj.transform.Find("Index").GetComponent<Text>().text = ack.MatchRank[i].Rank == 0 ? (i + 1).ToString() : ack.MatchRank[i].Rank.ToString();
            itemObj.transform.Find("Name").GetComponent<Text>().text = ack.MatchRank[i].PlayerInfo.Nick;
            itemObj.transform.Find("Count").GetComponent<Text>().text = ack.MatchRank[i].Coin;
        }
        
        // 删除多余的物体
        for (int i = childCount - 1; i >= dataCount; i--)
        {
            Destroy(varPlayerScroll.content.transform.GetChild(i).gameObject);
        }
    }

    public void Function_Msg_MatchDeskListRs(MessageRecvData data)
    {
        Msg_MatchDeskListRs ack = Msg_MatchDeskListRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_MatchDeskListRs收到比赛牌桌列表", ack.ToString());
        var牌桌总数.text = ack.DeskInfos.Count.ToString();
        
        int childCount = varDesksScroll.content.transform.childCount;
        int dataCount = ack.DeskInfos.Count;
        
        // 重用或创建项目
        for (int i = 0; i < dataCount; i++)
        {
            Msg_MatchDesk date = ack.DeskInfos[i];
            GameObject itemObj;
            
            if (i < childCount)
            {
                // 重用已有物体
                itemObj = varDesksScroll.content.transform.GetChild(i).gameObject;
            }
            else
            {
                // 创建新物体
                itemObj = Instantiate(varDesksItem);
                itemObj.transform.SetParent(varDesksScroll.content.transform, false);
            }
            
            // 更新物体数据
            itemObj.transform.Find("Index").GetComponent<Text>().text = (i + 1).ToString();
            itemObj.transform.Find("PlayerNum").GetComponent<Text>().text = date.DeskInfos.SitDownNum.ToString();
            itemObj.transform.Find("MinCount").GetComponent<Text>().text = date.MinCoin;
            itemObj.transform.Find("MaxCount").GetComponent<Text>().text = date.MaxCoin;
            
            // 重新绑定点击事件
            itemObj.transform.Find("GotoBtn").GetComponent<Button>().onClick.RemoveAllListeners();
            itemObj.transform.Find("GotoBtn").GetComponent<Button>().onClick.AddListener(() =>
            {
                if (Util.IsClickLocked()) return;
               Sound.PlayEffect(AudioKeys.SOUND_BTN);
                if (Util.GetMyselfInfo().DeskId != 0)
                {
                    GF.UI.ShowToast("您还有牌桌正在参赛,请点击参加比赛前往牌桌");
                }
                else{
                    Util.GetInstance().Send_EnterDeskRq(date.DeskInfos.DeskId);
                }
            });
        }
        
        // 删除多余的物体
        for (int i = childCount - 1; i >= dataCount; i--)
        {
            Destroy(varDesksScroll.content.transform.GetChild(i).gameObject);
        }
    }

    public void Function_Msg_SignUpMatchRs(MessageRecvData data)
    {
        Msg_SignUpMatchRs ack = Msg_SignUpMatchRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_SignUpMatchRs收到报名返回", ack.ToString());
        if (ack.MatchId == m_Match.MatchId)
        {
            GF.UI.ShowToast("报名成功");
        }
    }

    public void Function_Msg_SynMatchEnd(MessageRecvData data)
    {
        Msg_SynMatchEnd ack = Msg_SynMatchEnd.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_SynMatchEnd收到比赛结束通知", ack.ToString());
        if (ack.Match.MatchId == m_Match.MatchId)
        {
            GF.UI.ShowToast("比赛已结束");
            GF.UI.Close(this.UIForm);
        }
    }

    protected override async void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "Info":
                ResetPage(EventPanelType.Info);
                break;
            case "Award":
                ResetPage(EventPanelType.Award);
                break;
            case "Player":
                ResetPage(EventPanelType.Player);
                break;
            case "Desk":
                ResetPage(EventPanelType.Desk);
                break;
            case "AddCoin":
                var uiParams = UIParams.Create();
                uiParams.Set<VarString>("matchName", m_Match.MatchName);
                await GF.UI.OpenUIFormAwait(UIViews.MatchAddCoinPanel, uiParams);
                break;
            case "SignUp":
                if(m_Match.MatchState == 3 || m_Match.MatchState == 4){
                    return;
                }else if(m_Match.MatchState == 2){
                    if(m_Match.SignUpState == 1){
                        //已报名进入牌桌
                        if (Util.GetMyselfInfo().DeskId != 0)
                        {
                            Util.GetInstance().Send_EnterDeskRq(Util.GetMyselfInfo().DeskId);
                        }
                        else
                        {
                            //跳转牌桌
                            ResetPage(EventPanelType.Desk);
                        }
                    }
                    else{
                        //未报名跳转牌桌
                        ResetPage(EventPanelType.Desk);
                    }
                    return;
                }
                if (m_Match.SignUpState == 0)
                {
                    ShowSignUpConfirmationDialog();
                }
                else
                {
                    Util.GetInstance().OpenConfirmationDialog("取消报名", "确定取消报名吗?\n(一定时间内无法再次报名)", () =>
                    {
                        GF.LogInfo("取消报名");
                        Msg_SignUpMatchRq req = MessagePool.Instance.Fetch<Msg_SignUpMatchRq>();
                        req.MatchId = m_Match.MatchId;
                        req.State = 0;
                        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SignUpMatchRq, req);
                    });
                }
                break;
            case "ChooseClub":
                ShowChooseClubPanel();
                break;
            case "CloseChooseClub":
                HideChooseClubPanel();
                break;
            case "SignUpCancel":
                HideSignUpConfirmationDialog();
                break;
            case "SignUpOk":
                HideSignUpConfirmationDialog();
                Msg_SignUpMatchRq req = MessagePool.Instance.Fetch<Msg_SignUpMatchRq>();
                req.MatchId = m_Match.MatchId;
                req.ClubId = m_ChooseClubId;
                req.State = 1;
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                    (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SignUpMatchRq, req);
                break;
        }
    }

    public void ShowSignUpConfirmationDialog()
    {
        varSignUpConfirmationDialog.SetActive(true);
        var panelTransform = varSignUpConfirmationDialog.transform.GetChild(0);
        panelTransform.localScale = Vector3.zero;
        panelTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }

    public void HideSignUpConfirmationDialog()
    {
        var panelTransform = varSignUpConfirmationDialog.transform.GetChild(0);
        panelTransform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
        {
            if (varSignUpConfirmationDialog.activeSelf)
            {
                varSignUpConfirmationDialog.SetActive(false);
            }
        });
    }

    public void ShowChooseClubPanel()
    {
        varChooseClubPanel.SetActive(true);
        var panelTransform = varChooseClubPanel.transform;
        panelTransform.localPosition = new Vector3(1200, panelTransform.localPosition.y, panelTransform.localPosition.z);
        panelTransform.DOLocalMoveX(0, 0.3f);
    }

    public void HideChooseClubPanel()
    {
        var panelTransform = varChooseClubPanel.transform;
        var originalPos = panelTransform.localPosition;
        panelTransform.DOLocalMoveX(1200, 0.3f).OnComplete(() =>
        {
            if (varChooseClubPanel.activeSelf)
            {
                varChooseClubPanel.SetActive(false);
                panelTransform.localPosition = originalPos;
            }
        });
    }

    private void HandlePanelFunction(EventPanelType _panelType)
    {
        switch (_panelType)
        {
            case EventPanelType.Info:
                HandleInfoPanel();
                break;
            case EventPanelType.Award:
                HandleAwardPanel();
                break;
            case EventPanelType.Player:
                HandlePlayerPanel();
                break;
            case EventPanelType.Desk:
                HandleDeskPanel();
                break;
        }
    }

    private void HandleInfoPanel()
    {
        // 处理信息面板的逻辑
        GF.LogInfo("显示信息面板");
        //初始化一个门票类型 其他地方都需要使用 最好能够掉一个方法能够获取门票类型和消耗
        var (costType, cost) = Util.GetCurrentMatchTicketInfo(m_Match);
        var余额.text = costType == Util.MatchCostType.Tick ? GlobalManager.GetInstance().GetMatchTicketCount(m_Match.MatchId).ToString() : Util.GetMyselfInfo().Diamonds.ToString();
        var余额.transform.Find("钻石").gameObject.SetActive(costType == Util.MatchCostType.Diamond);
        var余额.transform.Find("门票").gameObject.SetActive(costType == Util.MatchCostType.Tick);
        varClubCount.text = costType == Util.MatchCostType.Tick ? GlobalManager.GetInstance().GetMatchTicketCount(m_Match.MatchId).ToString() : Util.GetMyselfInfo().Diamonds.ToString();
        varClubCount.transform.Find("钻石").gameObject.SetActive(costType == Util.MatchCostType.Diamond);
        varClubCount.transform.Find("门票").gameObject.SetActive(costType == Util.MatchCostType.Tick);
        var门票金额.text = cost.ToString();
        var门票金额.transform.Find("钻石").gameObject.SetActive(costType == Util.MatchCostType.Diamond);
        var门票金额.transform.Find("门票").gameObject.SetActive(costType == Util.MatchCostType.Tick);
        var钻石.SetActive(costType == Util.MatchCostType.Diamond);
        var门票金额2.text = cost.ToString();
        var门票金额2.transform.Find("钻石").gameObject.SetActive(costType == Util.MatchCostType.Diamond);
        var门票金额2.transform.Find("门票").gameObject.SetActive(costType == Util.MatchCostType.Tick);
        var买入.text = $"{(costType == Util.MatchCostType.Diamond ? "钻石" : "比赛券")} {cost}";
        var消耗.text = "-" + cost;
        var消耗.transform.Find("钻石").gameObject.SetActive(costType == Util.MatchCostType.Diamond);
        var消耗.transform.Find("门票").gameObject.SetActive(costType == Util.MatchCostType.Tick);
        var总奖池.text = m_Match.Bonus.ToString();
        var奖池.text = m_Match.Bonus.ToString();
        var报名人数.text = m_Match.Num.ToString();
        var初始筹码.text = "10000";
        var初始筹码2.text = "10000";
        varTitleText.text = m_Match.MatchName;
        var比赛名称.text = m_Match.MatchName;
        var比赛名字2.text = m_Match.MatchName;
        var开赛时间.text = Util.MillisecondsToDateString(m_Match.StartTime, "MM/dd HH:mm");
        var最小开赛人数.text = m_Match.MinStartNum.ToString(); // 默认值
        var最大报名数.text = m_Match.MaxSignNum.ToString(); // 默认值
        var参赛人数.text = $"{m_Match.MinStartNum}~{m_Match.MaxSignNum}";

        switch(m_Match.MatchState)
        {
            case 0:
            case 1:
                var距开赛Bg.GetComponent<Image>().color = new Color(0, 174/255f, 102/255f);
                var距开赛Bg.transform.Find("Text").GetComponent<Text>().text = "距开赛";
                varSignUpBtnText.text = m_Match.SignUpState == 1 ? "取消报名" : "报名";
                //当前时间距离开赛时间小时数(向上取整)
                UpdateStartCountdownDisplay();
                StartStartCountdown();
                varRankResult.SetActive(false);
                break;
            case 2:
                var距开赛Bg.GetComponent<Image>().color = new Color(0, 81/255f, 255/255f);
                var距开赛Bg.transform.Find("Text").GetComponent<Text>().text = "剩余人数";
                var距开赛.text = m_Match.AliveNum.ToString();
                varSignUpBtnText.text = m_Match.SignUpState == 1 ? "参加比赛" : "观看比赛";
                varRankResult.SetActive(false);
                break;
            case 3:
            case 4:
                var距开赛Bg.GetComponent<Image>().color = new Color(135/255f, 147/255f, 163/255f);
                varSignUpBtnText.text = "已结束";
                var距开赛Bg.transform.Find("Text").GetComponent<Text>().text = "已结束";
                var距开赛.text = "00:00";
                varRankResult.SetActive(true);
                // 更新玩家列表
                UpdatePlayerList();
                break;
        }

        //开始时间加上12分钟显示
        // var报名截止.text = Util.MillisecondsToDateString(m_Match.StartTime + 12 * 60 * 1000, "MM/dd HH:mm") + "(等级5)";
        var报名截止.text = Util.MillisecondsToDateString(m_Match.StartTime, "MM/dd HH:mm") + "";

        // 设置其他信息
        switch (m_Match.MethodType)
        {
            case MethodType.NiuNiu:
                var玩法规则.text = "牛牛";
                break;
            case MethodType.GoldenFlower:
                var玩法规则.text = "金花";
                break;
            case MethodType.TexasPoker:
                var玩法规则.text = "德州";
                break;
            case MethodType.CompareChicken:
                var玩法规则.text = "双王比鸡";
                break;
        }
        var涨底时间.text = "3分钟"; // 默认值
        var每桌人数.text = "6人"; // 默认值
        // var休息时间.text = "每隔55分钟休息5分钟"; // 默认值
        var决赛桌.text = "当参赛人数超过10触发"; // 默认值

        // 实现根据当前底注 初始化底注信息 显示倒计时 
        #region 底注初始化 倒计时显示
        // 停止之前的倒计时协程
        if (m_CurrentCountdownCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(m_CurrentCountdownCoroutine);
            m_CurrentCountdownCoroutine = null;
        }
        long timeToStart = m_Match.StartTime - Util.GetServerTime();
        if (timeToStart > 0)
        {
            // 比赛未开始，显示开赛倒计时
            var底注等级.text = "等待开始";
            var底注.text = "-";
            var下一级底注.text = "-";
            var倒计时.text = "00:00";
        }
        else
        {
            int currentBaseLevelIndex = m_Match.BaseCoinLevel - 1; // 索引从0开始，级别从1开始
            var addCoinTable = GlobalManager.GetInstance().m_AddCoinTable; // 底注涨注表

            if (addCoinTable != null && currentBaseLevelIndex >= 0 && currentBaseLevelIndex < addCoinTable.Count)
            {
                var currentBaseCoin = addCoinTable[currentBaseLevelIndex];
                var底注.text = currentBaseCoin.SmallBlind.ToString();
                // 获取下一级底注信息（如果存在）
                if (currentBaseLevelIndex + 1 < addCoinTable.Count)
                {
                    // 计算距离下一次加底注的时间
                    long nextLevelTime = CalculateNextLevelTime(currentBaseLevelIndex, addCoinTable);
                    if (nextLevelTime > 0)
                    {
                        var倒计时.text = Util.MillisecondsToDateString(nextLevelTime, "mm:ss");
                        var下一级底注.text = addCoinTable[currentBaseLevelIndex + 1].SmallBlind.ToString();
                        // 计算目标时间点用于协程
                        long targetTime = Util.GetServerTime() + nextLevelTime;
                        m_CurrentCountdownCoroutine = CoroutineRunner.Instance.StartCoroutine(UpdateCountdown(targetTime));
                    }
                    else
                    {
                        var下一级底注.text = "-";
                        var倒计时.text = "00:00";
                    }
                }
            }
            else
            {
                var底注.text = "-";
                var下一级底注.text = "-";
                var底注等级.text = "底注等级 -";
                var倒计时.text = "00:00";
            }
        }
        #endregion
    }

    #region 开赛倒计时
    private Coroutine m_StartCountdownCoroutine; // 开赛倒计时协程

    private void StartStartCountdown()
    {
        StopStartCountdown();
        m_StartCountdownCoroutine = CoroutineRunner.Instance.StartCoroutine(StartCountdownCoroutine());
    }

    private void StopStartCountdown()
    {
        if (m_StartCountdownCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(m_StartCountdownCoroutine);
            m_StartCountdownCoroutine = null;
        }
    }

    private System.Collections.IEnumerator StartCountdownCoroutine()
    {
        while (m_Match != null && (m_Match.MatchState == 0 || m_Match.MatchState == 1))
        {
            UpdateStartCountdownDisplay();
            yield return new WaitForSeconds(1f);
        }
    }

    private void UpdateStartCountdownDisplay()
    {
        long timeDiff = m_Match.StartTime - Util.GetServerTime();
        if (timeDiff >= 24 * 3600 * 1000) // 大于等于24小时
        {
            int days = Mathf.CeilToInt(timeDiff / (1000f * 60f * 60f * 24f));
            var距开赛.text = $"{days}天";
        }
        else if (timeDiff >= 3600 * 1000) // 大于等于1小时
        {
            int hours = Mathf.CeilToInt(timeDiff / (1000f * 60f * 60f));
            var距开赛.text = $"{hours}小时";
        }
        else if (timeDiff > 0) // 小于1小时
        {
            TimeSpan ts = TimeSpan.FromMilliseconds(timeDiff);
            var距开赛.text = $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
        else
        {
            var距开赛.text = "00:00";
        }
    }
    #endregion

    /// <summary>
    /// 更新倒计时协程
    /// </summary>
    private System.Collections.IEnumerator UpdateCountdown(long targetTime)
    {
        while (true)
        {
            long remainingTime = targetTime - Util.GetServerTime();
            if (remainingTime <= 0)
            {
                // 倒计时结束，刷新页面
                var底注.text = "-";
                var下一级底注.text = "-";
                var底注等级.text = "底注等级 -";
                var倒计时.text = "00:00";
                HandleInfoPanel();
                yield break;
            }

            // 格式化显示倒计时，根据剩余时间选择不同的格式
            TimeSpan ts = TimeSpan.FromMilliseconds(remainingTime);
            string timeStr;
            if ((int)ts.TotalHours > 0)
            {
                timeStr = $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
            }
            else
            {
                timeStr = $"{ts.Minutes:D2}:{ts.Seconds:D2}";
            }
            var倒计时.text = timeStr;

            yield return new WaitForSeconds(1.0f);
        }
    }

    private void HandleAwardPanel()
    {
        // 处理奖励面板的逻辑
        GF.LogInfo("显示奖励面板");
        var总奖金.text = m_Match.Bonus.ToString();
        // 更新奖励列表
        UpdateAwardList();
    }

    private void HandlePlayerPanel()
    {
        // 处理玩家面板的逻辑
        GF.LogInfo("显示玩家面板");
        var剩余人数.text = $"{m_Match.Num}/{m_Match.MaxSignNum}"; // 使用默认最大人数

        // 更新玩家列表
        UpdatePlayerList();
    }

    private void HandleDeskPanel()
    {
        // 处理牌桌面板的逻辑
        GF.LogInfo("显示牌桌面板");
        // 更新牌桌列表
        UpdateDeskList();
    }

    private void UpdateAwardList()
    {
        // TODO: 实现奖励列表的更新逻辑
        Msg_MatchRewardRq req = MessagePool.Instance.Fetch<Msg_MatchRewardRq>();
        req.MatchId = m_Match.MatchId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_MatchRewardRq, req);
    }

    private void UpdatePlayerList()
    {
        // TODO: 实现玩家列表的更新逻辑
        Msg_MatchRankListRq req = MessagePool.Instance.Fetch<Msg_MatchRankListRq>();
        req.MatchId = m_Match.MatchId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_MatchRankListRq, req);
    }

    private void UpdateDeskList()
    {
        // TODO: 实现牌桌列表的更新逻辑
        Msg_MatchDeskListRq req = MessagePool.Instance.Fetch<Msg_MatchDeskListRq>();
        req.MatchId = m_Match.MatchId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_MatchDeskListRq, req);

    }

    void ResetPage(EventPanelType eventPanelType, bool withAnimation = true)
    {
        if (withAnimation && (m_IsAnimating || m_CurrentPanelType == eventPanelType))
            return;

        if (!withAnimation && m_CurrentPanelType == eventPanelType)
        {
            bool isInitialized = false;
            foreach (var panel in m_PanelDict.Values)
            {
                if (panel.activeSelf)
                {
                    isInitialized = true;
                    break;
                }
            }
            if (isInitialized)
            {
                // 即使面板已经初始化，也要确保调用HandlePanelFunction来更新数据
                HandlePanelFunction(eventPanelType);
                return;
            }
        }

        m_IsAnimating = true;

        // 获取选中的按钮和取消选中的按钮
        Button selectedBtn = GetButtonByType(eventPanelType);
        Button unselectedBtn = GetButtonByType(m_CurrentPanelType);

        // 处理选中按钮的文本变化
        Text selectedText = selectedBtn.GetComponentInChildren<Text>();
        if (selectedText != null)
        {
            if (withAnimation)
            {
                // 创建一个序列动画
                Sequence sequence = DOTween.Sequence();
                // 添加颜色变化的回调
                sequence.AppendCallback(() =>
                {
                    selectedText.color = new Color(1f, 1f, 1f, 1f); // 颜色从C4C4C6变为FFFFFF
                    selectedText.fontStyle = FontStyle.Bold;
                });
                // 添加大小变化的动画
                sequence.Append(selectedText.transform.DOScale(new Vector3(48f / 36f, 48f / 36f, 1f), 0.2f));
            }
            else
            {
                selectedText.color = new Color(1f, 1f, 1f, 1f); // 颜色从C4C4C6变为FFFFFF
                selectedText.fontStyle = FontStyle.Bold;
                selectedText.transform.localScale = new Vector3(48f / 36f, 48f / 36f, 1f);
            }
        }

        // 处理取消选中按钮的文本变化
        Text unselectedText = unselectedBtn.GetComponentInChildren<Text>();
        if (unselectedText != null)
        {
            if (withAnimation)
            {
                // 创建一个序列动画
                Sequence sequence = DOTween.Sequence();
                // 添加颜色变化的回调
                sequence.AppendCallback(() =>
                {
                    unselectedText.color = new Color(0.77f, 0.77f, 0.77f, 1f); // 颜色恢复为C4C4C6
                    unselectedText.fontStyle = FontStyle.Normal;
                });
                // 添加大小变化的动画
                sequence.Append(unselectedText.transform.DOScale(Vector3.one, 0.2f));
            }
            else
            {
                unselectedText.color = new Color(0.77f, 0.77f, 0.77f, 1f); // 颜色恢复为C4C4C6
                unselectedText.fontStyle = FontStyle.Normal;
                unselectedText.transform.localScale = Vector3.one;
            }
        }

        // 移动varLine到选中按钮下方
        if (withAnimation)
        {
            varLine.transform.DOLocalMoveX(selectedBtn.transform.localPosition.x, 0.2f);
        }
        else
        {
            varLine.transform.localPosition = new Vector3(selectedBtn.transform.localPosition.x, varLine.transform.localPosition.y, varLine.transform.localPosition.z);
        }

        // 动画切换
        var outPanel = m_PanelDict[m_CurrentPanelType];
        var inPanel = m_PanelDict[eventPanelType];

        if (withAnimation)
        {
            float width = ((RectTransform)outPanel.transform).rect.width;
            float outTargetX = eventPanelType > m_CurrentPanelType ? -width : width;
            float inStartX = eventPanelType > m_CurrentPanelType ? width : -width;

            // 离场动画
            outPanel.transform.DOLocalMoveX(outTargetX, 0.25f).OnComplete(() =>
            {
                outPanel.SetActive(false);
                outPanel.transform.localPosition = m_PanelInitPosDict[m_CurrentPanelType];
            });
            // 入场动画
            inPanel.SetActive(true);
            inPanel.transform.localPosition = new Vector3(inStartX, m_PanelInitPosDict[eventPanelType].y, m_PanelInitPosDict[eventPanelType].z);
            inPanel.transform.DOLocalMoveX(m_PanelInitPosDict[eventPanelType].x, 0.25f).OnComplete(() =>
            {
                m_IsAnimating = false;
                m_CurrentPanelType = eventPanelType;
                HandlePanelFunction(eventPanelType);
            });
        }
        else
        {
            foreach (var panel in m_PanelDict)
            {
                panel.Value.SetActive(panel.Key == eventPanelType);
                panel.Value.transform.localPosition = m_PanelInitPosDict[panel.Key];
            }
            m_IsAnimating = false;
            m_CurrentPanelType = eventPanelType;
            HandlePanelFunction(eventPanelType);
        }
    }

    /// <summary>
    /// 根据面板类型获取对应的按钮
    /// </summary>
    private Button GetButtonByType(EventPanelType type)
    {
        switch (type)
        {
            case EventPanelType.Info:
                return varInfo;
            case EventPanelType.Award:
                return varAward;
            case EventPanelType.Player:
                return varPlayer;
            case EventPanelType.Desk:
                return varDesk;
            default:
                return varInfo;
        }
    }

    /// <summary>
    /// 初始化按钮状态
    /// </summary>
    private void InitializeButtonStates()
    {
        // 设置初始选中按钮的样式
        Button selectedBtn = GetButtonByType(m_CurrentPanelType);
        Text selectedText = selectedBtn.GetComponentInChildren<Text>();
        if (selectedText != null)
        {
            selectedText.color = new Color(1f, 1f, 1f, 1f); // 白色
            selectedText.fontStyle = FontStyle.Bold;
            selectedText.transform.localScale = new Vector3(48f / 36f, 48f / 36f, 1f); // 大小为48
        }

        // 设置其他按钮的样式为未选中状态
        foreach (EventPanelType type in System.Enum.GetValues(typeof(EventPanelType)))
        {
            if (type != m_CurrentPanelType)
            {
                Button btn = GetButtonByType(type);
                Text text = btn.GetComponentInChildren<Text>();
                if (text != null)
                {
                    text.color = new Color(0.77f, 0.77f, 0.77f, 1f); // C4C4C6颜色
                    text.fontStyle = FontStyle.Normal;
                    text.transform.localScale = Vector3.one; // 原始大小36
                }
            }
        }

        // 设置初始线条位置
        varLine.transform.localPosition = new Vector3(selectedBtn.transform.localPosition.x,
                                                      varLine.transform.localPosition.y,
                                                      varLine.transform.localPosition.z);
    }

    /// <summary>
    /// 计算距离下一次加底注的时间
    /// </summary>
    /// <param name="_currentBaseLevelIndex">当前底注等级索引</param>
    /// <param name="_addCoinTable">底注涨注表</param>
    /// <returns>距离下一次加底注的毫秒数</returns>
    private long CalculateNextLevelTime(int _currentBaseLevelIndex, System.Collections.Generic.List<AddCoinLevelData> _addCoinTable)
    {
        if (_addCoinTable == null || _currentBaseLevelIndex < 0 || _currentBaseLevelIndex >= _addCoinTable.Count)
        {
            return 0;
        }

        long matchStartTime = m_Match.StartTime;
        long currentTime = Util.GetServerTime();

        // 计算当前回合应该结束的时间点
        long currentLevelEndTime = matchStartTime;
        for (int i = 0; i <= _currentBaseLevelIndex; i++)
        {
            if (i < _addCoinTable.Count)
            {
                currentLevelEndTime += _addCoinTable[i].AddTime * 1000; // 毫秒
            }
        }

        // 如果当前时间已经超过了当前回合的结束时间，说明需要进入下一回合
        if (currentTime >= currentLevelEndTime)
        {
            // 计算下一回合的结束时间
            long nextLevelEndTime = currentLevelEndTime;
            if (_currentBaseLevelIndex + 1 < _addCoinTable.Count)
            {
                nextLevelEndTime += _addCoinTable[_currentBaseLevelIndex + 1].AddTime * 1000;
            }

            return nextLevelEndTime - currentTime;
        }
        else
        {
            // 当前回合还未结束，返回距离当前回合结束的时间
            return currentLevelEndTime - currentTime;
        }
    }

    public override void OnClickClose()
    {
        if (Util.IsClickLocked()) return;
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (varChooseClubPanel.activeSelf)
        {
            HideChooseClubPanel();
            return;
        }
        if (varSignUpConfirmationDialog.activeSelf)
        {
            HideSignUpConfirmationDialog();
            return;
        }
        GF.UI.Close(this.UIForm);
    }
}

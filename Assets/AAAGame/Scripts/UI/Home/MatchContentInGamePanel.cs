using System.Collections.Generic;
using DG.Tweening;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using System;
using System.Linq;
using System.IO;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class MatchContentInGamePanel : UIFormBase
{
    public enum EventInGamePanelType
    {
        Info = 1,
        Award,
        Desk,
    }
    #region Private Fields
    private Dictionary<EventInGamePanelType, GameObject> m_PanelDict;
    private Dictionary<EventInGamePanelType, Vector3> m_PanelInitPosDict;
    private EventInGamePanelType m_CurrentPanelType = EventInGamePanelType.Info;
    private bool m_IsAnimating = false;
    private Coroutine m_CurrentCountdownCoroutine; // 当前运行的倒计时协程
    #endregion

    private Msg_Match m_Match = new Msg_Match();
    long[] playerInTableIds = new long[0];
    // 新增：排行榜显示模式，true为只显示桌上玩家，false为显示全部
    private bool m_ShowOnlyTablePlayers = false;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        m_PanelDict = new Dictionary<EventInGamePanelType, GameObject>
        {
            { EventInGamePanelType.Info, varInfoPanel },
            { EventInGamePanelType.Award, varAwardPanel },
            { EventInGamePanelType.Desk, varDesksPanel },
        };
        m_PanelInitPosDict = new Dictionary<EventInGamePanelType, Vector3>();
        foreach (var kv in m_PanelDict)
        {
            m_PanelInitPosDict[kv.Key] = kv.Value.transform.localPosition;
        }
        
        HotfixNetworkComponent.AddListener(MessageID.Syn_Match, Function_syn_Match);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MatchRewardRs, Function_Msg_MatchRewardRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MatchRankListRs, Function_Msg_MatchRankListRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MatchDeskListRs, Function_Msg_MatchDeskListRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynMatchEnd, Function_Msg_SynMatchEnd);

        m_Match = Msg_Match.Parser.ParseFrom(Params.Get<VarByteArray>("matchConfig"));
        VarByteArray varBytes = Params.Get<VarByteArray>("playerInTableIds");
        if (varBytes != null && varBytes.Value.Length > 0)
        {
            using (var memoryStream = new MemoryStream(varBytes.Value))
            {
                using (var binaryReader = new BinaryReader(memoryStream))
                {
                    var ids = new List<long>();
                    while(memoryStream.Position < memoryStream.Length)
                    {
                        ids.Add(binaryReader.ReadInt64());
                    }
                    playerInTableIds = ids.ToArray();
                }
            }
        }
        m_ShowOnlyTablePlayers = false;
        varChangeRankText.text = "显示本桌";
        // 初始化按钮状态
        ResetPage(EventInGamePanelType.Info, false);
        InitializeButtonStates();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        // 停止倒计时协程
        if (m_CurrentCountdownCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(m_CurrentCountdownCoroutine);
            m_CurrentCountdownCoroutine = null;
        }
        HotfixNetworkComponent.RemoveListener(MessageID.Syn_Match, Function_syn_Match);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MatchRewardRs, Function_Msg_MatchRewardRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MatchRankListRs, Function_Msg_MatchRankListRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MatchDeskListRs, Function_Msg_MatchDeskListRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynMatchEnd, Function_Msg_SynMatchEnd);
        base.OnClose(isShutdown, userData);
    }

    public void Function_syn_Match(MessageRecvData data)
    {
        Syn_Match ack = Syn_Match.Parser.ParseFrom(data.Data);
        GF.LogInfo("Syn_Match收到比赛牌桌变化通知" , ack.ToString());
        if(ack.Match.MatchId == m_Match.MatchId){
            m_Match = ack.Match;
            if(m_CurrentPanelType == EventInGamePanelType.Info){
                ResetPage(EventInGamePanelType.Info, false);
            }
        }
    }

    public void Function_Msg_MatchRewardRs(MessageRecvData data)
    {
        Msg_MatchRewardRs ack = Msg_MatchRewardRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_MatchRewardRs收到比赛奖励列表" , ack.ToString());
        
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
        GF.LogInfo("Msg_MatchRankListRs收到比赛排名列表" , ack.ToString());
        List<Msg_MatchRank> matchRankList = new List<Msg_MatchRank>();
        if(m_ShowOnlyTablePlayers)
        {
            matchRankList = ack.MatchRank.Where(x => playerInTableIds.Contains(x.PlayerInfo.PlayerId)).ToList();
        }
        else
        {
            matchRankList = ack.MatchRank.ToList();
        }
        
        int childCount = varPlayerScroll.content.transform.childCount;
        int dataCount = matchRankList.Count;
        
        int remainNum = 0;
        float maxChip = 0;
        float minChip = 0;
        float totalBuy = 0;
        Msg_MatchRank selfRank = null;
        
        // 重用或创建项目
        for (int i = 0; i < dataCount; i++)
        {
            Msg_MatchRank matchRank = matchRankList[i];
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
            itemObj.transform.Find("Index").GetComponent<Text>().text = matchRank.Rank == 0 ? (i + 1).ToString() : matchRank.Rank.ToString();
            itemObj.transform.Find("Name").GetComponent<Text>().text = matchRank.PlayerInfo.Nick;
            itemObj.transform.Find("Count").GetComponent<Text>().text = matchRank.Coin;
            
            totalBuy += float.Parse(matchRank.Coin);
            if(float.Parse(matchRank.Coin) > 0)
            {
                remainNum++;
            }
            if(float.Parse(matchRank.Coin) > maxChip)
            {
                maxChip = float.Parse(matchRank.Coin);
            }
            if(float.Parse(matchRank.Coin) < minChip)
            {
                minChip = float.Parse(matchRank.Coin);
            }
            if(Util.IsMySelf(matchRank.PlayerInfo.PlayerId))
            {
                selfRank = matchRank;
            }
        }
        
        // 删除多余的物体
        for (int i = childCount - 1; i >= dataCount; i--)
        {
            Destroy(varPlayerScroll.content.transform.GetChild(i).gameObject);
        }
        
        if (!m_ShowOnlyTablePlayers)
        {
            varMyItem.transform.Find("Name").GetComponent<Text>().text = selfRank != null ? selfRank.PlayerInfo.Nick : Util.GetMyselfInfo().NickName;
            varMyItem.transform.Find("Index").GetComponent<Text>().text = selfRank != null ? selfRank.Rank.ToString() : "0";
            var (costType, cost) = Util.GetCurrentMatchTicketInfo(m_Match);
            varMyItem.transform.Find("Count").GetComponent<Text>().text = selfRank != null ? selfRank.Coin : "0";
            var剩余人数.text = $"{remainNum}/{m_Match.MaxSignNum}";
            var总买入.text = (ack.MatchRank.Count * cost).ToString();
            var最大筹码.text = maxChip.ToString();
            var平均筹码.text = (totalBuy / ack.MatchRank.Count).ToString();
            var最小筹码.text = minChip.ToString();
        }
    }

    public void Function_Msg_MatchDeskListRs(MessageRecvData data)
    {
        Msg_MatchDeskListRs ack = Msg_MatchDeskListRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_MatchDeskListRs收到比赛牌桌列表" , ack.ToString());
        
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
                if (playerInTableIds.Contains(Util.GetMyselfInfo().PlayerId))
                {
                    // GF.UI.ShowToast("您正在比赛,请勿前往其他牌桌");
                }
                else
                {
                    Util.Send_ChangeDeskRq(date.DeskInfos.DeskId);
                }
            });
        }
        
        // 删除多余的物体
        for (int i = childCount - 1; i >= dataCount; i--)
        {
            Destroy(varDesksScroll.content.transform.GetChild(i).gameObject);
        }
    }

    public void Function_Msg_SynMatchEnd(MessageRecvData data)
    {
        Msg_SynMatchEnd ack = Msg_SynMatchEnd.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_SynMatchEnd收到比赛结束通知" , ack.ToString());
        if(ack.Match.MatchId == m_Match.MatchId){
            GF.UI.Close(this.UIForm);
        }
    }

    protected override async void OnButtonClick(object sender, string btId)
    {
       base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "Info":
                ResetPage(EventInGamePanelType.Info);
                break;
            case "Award":
                ResetPage(EventInGamePanelType.Award);
                break;
            case "Desk":
                ResetPage(EventInGamePanelType.Desk);
                break;
            case "AddCoin":
                var uiParams = UIParams.Create();
                uiParams.Set<VarString>("matchName", m_Match.MatchName);
                await GF.UI.OpenUIFormAwait(UIViews.MatchAddCoinPanel, uiParams);
                break;
            case "ChangeRank":
                m_ShowOnlyTablePlayers = !m_ShowOnlyTablePlayers;
                varChangeRankText.text = m_ShowOnlyTablePlayers ? "显示全部" : "显示本桌";
                UpdatePlayerList();
                break;
        }
    }

    private void HandlePanelFunction(EventInGamePanelType _panelType)
    {
        switch (_panelType)
        {
            case EventInGamePanelType.Info:
                HandleInfoPanel();
                break;
            case EventInGamePanelType.Award:
                HandleAwardPanel();
                break;
            case EventInGamePanelType.Desk:
                HandleDeskPanel();
                break;
        }
    }

    private void HandleInfoPanel()
    {
        HandlePlayerPanel();
        // 处理信息面板的逻辑
        GF.LogInfo("显示信息面板");
        //初始化一个门票类型 其他地方都需要使用 最好能够掉一个方法能够获取门票类型和消耗
        var总奖金.text = m_Match.Bonus.ToString();
        varTitleText.text = m_Match.MatchName;

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
                var底注等级.text = "底注等级 -";
                var倒计时.text = "00:00";
                HandleInfoPanel();
                yield break;
            }
            
            // 格式化显示倒计时，根据剩余时间选择不同的格式
            TimeSpan ts = TimeSpan.FromMilliseconds(remainingTime);
            string timeStr = $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
            var倒计时.text = timeStr;
            
            yield return new WaitForSeconds(1.0f);
        }
    }

    private void HandleAwardPanel()
    {
        // 处理奖励面板的逻辑
        GF.LogInfo("显示奖励面板");
        // 更新奖励列表
        UpdateAwardList();
    }

    private void HandlePlayerPanel()
    {
        // 处理玩家面板的逻辑
        GF.LogInfo("显示玩家面板");
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
        // 实现玩家列表的更新逻辑
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

    void ResetPage(EventInGamePanelType eventPanelType, bool withAnimation = false)
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
            if(isInitialized) 
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
    private Button GetButtonByType(EventInGamePanelType type)
    {
        switch (type)
        {
            case EventInGamePanelType.Info:
                return varInfo;
            case EventInGamePanelType.Award:
                return varAward;
            case EventInGamePanelType.Desk:
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
            selectedText.transform.localScale = new Vector3(48f/36f, 48f/36f, 1f); // 大小为48
        }
        
        // 设置其他按钮的样式为未选中状态
        foreach (EventInGamePanelType type in System.Enum.GetValues(typeof(EventInGamePanelType)))
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

}

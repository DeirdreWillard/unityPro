using UnityEngine;
using GameFramework.Event;
using UnityGameFramework.Runtime;
using Newtonsoft.Json;
using System.Collections.Generic;
using DG.Tweening;
using NetMsg;
using static UtilityBuiltin;
using UnityEngine.Networking;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class HomePanel : UIFormBase
{
    //BotPanel
    public enum BotBtnType
    {
        BuildRoom = 1,
        Club,
        MainInterface,
        EventDesk,
        InfoPanel,
    }
    public RedDotItem redDot;
    public RedDotItem clubRedDot;

    public GameObject varLine;
    public GameObject[] varToggles;

    public GameObject BuildRoom;
    public GameObject ClubPanel;
    public GameObject MainInterface;
    public GameObject EventDesk;
    public GameObject InfoPanel;

    #region 动画相关字段
    private Dictionary<BotBtnType, GameObject> m_PanelDict;
    private Dictionary<BotBtnType, Vector3> m_PanelInitPosDict;
    private BotBtnType m_CurrentPanelType = BotBtnType.MainInterface;
    private bool m_IsAnimating = false;
    #endregion

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        
        // 保存用户在扑克大厅的状态
        var playerId = Util.GetMyselfInfo()?.PlayerId ?? 0;
        if (playerId > 0)
        {
            GlobalManager.SaveLastGameFlow(playerId, GameFlowType.PokerFlow);
        }
        
        // 初始化Panel字典和初始位置
        m_PanelDict = new Dictionary<BotBtnType, GameObject>
        {
            { BotBtnType.BuildRoom, BuildRoom },
            { BotBtnType.Club, ClubPanel },
            { BotBtnType.MainInterface, MainInterface },
            { BotBtnType.EventDesk, EventDesk },
            { BotBtnType.InfoPanel, InfoPanel }
        };
        m_PanelInitPosDict = new Dictionary<BotBtnType, Vector3>();
        foreach (var kv in m_PanelDict)
        {
            m_PanelInitPosDict[kv.Key] = kv.Value.transform.localPosition;
        }

        MainInterface.GetComponent<MainInterface>().Init();
        ClubPanel.GetComponent<ClubPanel>().Init();
        InfoPanel.GetComponent<InfoPanel>().Init();
        EventDesk.GetComponent<MatchDesk>().Init();

        RedDotManager.GetInstance().SetRedDotNodeCallBack(E_RedDotDefine.MsgBox, MsgCallBack);
        RedDotManager.GetInstance().RefreshAll();
        GF.Event.Subscribe(GFEventArgs.EventId, OnGFEventChanged);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynMatchFirst, Function_SynMatchWin);

        HomeProcedure homeProcedure = GF.Procedure.CurrentProcedure as HomeProcedure;
        RetSetPage(homeProcedure.m_matchID > 0 ? BotBtnType.EventDesk : BotBtnType.MainInterface);
        clubRedDot.SetDotState(false, 0);
        CheckSetSecurityState();
        ReqNotificationUrlAsync();
        GetShareUrlListAsync();
        
    }

    public void Function_SynMatchWin(MessageRecvData data)
    {
        Msg_SynMatchFirst ack = Msg_SynMatchFirst.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到单局比赛胜利通知", ack.ToString());
        // 全局处理比赛胜利通知
        GF.UI.LoadPrefab(AssetsPath.GetPrefab("UI/Tips/MatchSettleTips"), (gameObject) =>
            {
                if (gameObject == null) return;
                GameObject matchSettle = Instantiate(gameObject as GameObject, transform);
                if (matchSettle == null)
                {
                    return;
                }
                MatchSettleTips tips = matchSettle.GetComponent<MatchSettleTips>();
                tips.ShowWin(ack, () =>
                {
                });
            });
    }

    /// <summary>
    /// 检查初始化信息状态
    /// </summary> 
    public async void CheckSetSecurityState()
    {
        if (Util.GetMyselfInfo().LockState != -1)
            return;

        await GF.UI.OpenUIFormAwait(UIViews.SetSecurityDialog);
    }

    /// <summary>
    /// 异步获取分享链接列表
    /// </summary>
    /// <returns>获取任务</returns>
    public async System.Threading.Tasks.Task GetShareUrlListAsync()
    {
        string notificationUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}:{Const.ServerInfo_BackEndPort}/url.txt";
        if (Const.CurrentServerType == Const.ServerType.外网服)
        {
            notificationUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}/url.txt";
        }
        GF.LogInfo("请求分享链接", notificationUrl);

        UnityWebRequest request = UnityWebRequest.Get(notificationUrl);
        request.certificateHandler = new BypassCertificate();
        
        var operation = request.SendWebRequest();
        while (!operation.isDone)
        {
            await System.Threading.Tasks.Task.Yield();
        }
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            string content = request.downloadHandler.text;
            if (!string.IsNullOrEmpty(content))
            {
                GlobalManager.allUrls = JsonConvert.DeserializeObject<List<string>>(content);
            }
            else
            {
                GF.LogWarning("分享链接数据为空");
            }
        }
        else
        {
            GF.LogError("获取分享链接失败: " + request.error);
        }
    }
    
    public async void ReqNotificationUrlAsync()
    {
        if (!GlobalManager.CanExecuteDailyAction("RequestNotification"))
        {
            return;
        }

        string notificationUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}:{Const.ServerInfo_BackEndPort}/data/notice.html";
        if (Const.CurrentServerType == Const.ServerType.外网服)
        {
            notificationUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}/data/notice.html";
        }
        GF.LogInfo("请求公告信息", notificationUrl);

        UnityWebRequest request = UnityWebRequest.Get(notificationUrl);
        request.certificateHandler = new BypassCertificate();
        
        var operation = request.SendWebRequest();
        while (!operation.isDone)
        {
            await System.Threading.Tasks.Task.Yield();
        }
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonStr = request.downloadHandler.text;
            GF.LogInfo("公告信息：" + jsonStr);
            if (!string.IsNullOrEmpty(jsonStr))
            {
                var noticeList = JsonConvert.DeserializeObject<NoticeList>(jsonStr);
                if (noticeList?.Notices?.Count > 0)
                {
                    var uiParams = UIParams.Create();
                    uiParams.Set<VarString>("title", noticeList.Notices[0].Title);
                    uiParams.Set<VarString>("content", noticeList.Notices[0].Content);
                    var homeNotification = await GF.UI.OpenUIFormAwait(UIViews.HomeNotification, uiParams);
                    if (homeNotification != null)
                    {
                        GlobalManager.RecordDailyAction("RequestNotification");
                    }
                }
                else
                {
                    GF.LogWarning("公告信息为空");
                }
            }
        }
        else
        {
            GF.LogError("获取公告信息失败: " + request.error);
        }
    }

    void MsgCallBack(RedDotNode node)
    {
        if (gameObject == null) return;
        redDot.SetDotState(node.rdCount > 0, node.rdCount);
    }

    private void OnGFEventChanged(object sender, GameEventArgs e)
    {
        var args = e as GFEventArgs;
        switch (args.EventType)
        {
            case GFEventType.eve_SynClubChatNum:
                clubRedDot.SetDotState(ChatManager.GetInstance().GetAllClubChatNum() > 0);
                break;
            case GFEventType.eve_ReConnectGame:
                break;
        }
    }

    public async void OnmessClick()
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        await GF.UI.OpenUIFormAwait(UIViews.MessagePanel);
    }

    private Queue<float> clickTimes = new Queue<float>();
    private float timeWindow = 3f; // 时间窗口（秒）
    private int maxClicks = 6;     // 最大允许点击次数

    /// <summary>
    /// 点击锁（时间窗口内最大点击次数限制）
    /// </summary>
    /// <returns>true表示允许点击，false表示拒绝点击</returns>
    public bool ClickLock()
    {
        float currentTime = Time.time;

        // 移除过期的时间记录
        while (clickTimes.Count > 0 && currentTime - clickTimes.Peek() > timeWindow)
        {
            clickTimes.Dequeue();
        }

        // 检查点击次数是否超过限制
        if (clickTimes.Count >= maxClicks)
        {
            return false;
        }

        // 记录本次点击时间
        clickTimes.Enqueue(currentTime);
        return true;
    }

     protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "OpenMaJiang":
            //打开界面流程
                if (GF.Procedure.CurrentProcedure is HomeProcedure homeProcedure)
                {
                    homeProcedure.EnterMJProcedure();
                }
                break;
        }   
    }
                


    public void OnToggleClick(int botBtnType)
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (!ClickLock())
        {
            GF.StaticUI.ShowError("操作频繁，请稍后再试");
            return;
        }
        RetSetPage((BotBtnType)botBtnType);
    }

    void RetSetPage(BotBtnType botTypecm)
    {
        if (m_IsAnimating || m_CurrentPanelType == botTypecm)
            return;
        m_IsAnimating = true;
        // 处理toggle UI
        foreach (GameObject toggle in varToggles)
        {
            toggle.transform.Find("Checkmark").gameObject.SetActive(false);
            toggle.transform.Find("Background").gameObject.SetActive(true);
        }
        GameObject btn = varToggles[(int)botTypecm - 1];
        varLine.SetActive(botTypecm != BotBtnType.MainInterface);
        varLine.transform.DOLocalMoveX(btn.transform.localPosition.x, 0.1f);
        btn.transform.Find("Background").gameObject.SetActive(false);
        btn.transform.Find("Checkmark").gameObject.SetActive(true);

        // 动画切换
        var outPanel = m_PanelDict[m_CurrentPanelType];
        var inPanel = m_PanelDict[botTypecm];
        float width = ((RectTransform)outPanel.transform).rect.width;
        float outTargetX = botTypecm > m_CurrentPanelType ? -width : width;
        float inStartX = botTypecm > m_CurrentPanelType ? width : -width;

        // 离场动画
        outPanel.transform.DOLocalMoveX(outTargetX, 0.25f).OnComplete(() =>
        {
            outPanel.SetActive(false);
            outPanel.transform.localPosition = m_PanelInitPosDict[m_CurrentPanelType];
        });
        // 入场动画
        inPanel.SetActive(true);
        inPanel.transform.localPosition = new Vector3(inStartX, m_PanelInitPosDict[botTypecm].y, m_PanelInitPosDict[botTypecm].z);
        inPanel.transform.DOLocalMoveX(m_PanelInitPosDict[botTypecm].x, 0.25f).OnComplete(() =>
        {
            m_IsAnimating = false;
            m_CurrentPanelType = botTypecm;
            // 切换后如有特殊逻辑
            switch (botTypecm)
            {
                case BotBtnType.BuildRoom:
                    break;
                case BotBtnType.Club:
                    break;
                case BotBtnType.MainInterface:
                    break;
                case BotBtnType.EventDesk:
                    break;
                case BotBtnType.InfoPanel:
                    RecordManager.GetInstance().Send_MyGameRecordRq();
                    break;
            }
        });
    }
    protected override void OnClose(bool isShutdown, object userData)
    {
        RedDotManager.GetInstance().RemoveRedDotNodeCallBack(E_RedDotDefine.MsgBox, MsgCallBack);
        GF.Event.Unsubscribe(GFEventArgs.EventId, OnGFEventChanged);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynMatchFirst, Function_SynMatchWin);
        MainInterface.GetComponent<MainInterface>().Clear();
        ClubPanel.GetComponent<ClubPanel>().Clear();
        InfoPanel.GetComponent<InfoPanel>().Clear();
        EventDesk.GetComponent<MatchDesk>().Clear();
        base.OnClose(isShutdown, userData);
    }

    public override void OnClickClose()
    {
        if (Util.IsClickLocked()) return;
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        Util.GetInstance().OpenConfirmationDialog("退出登录", "确定要返回登录界面吗?", () =>
        {
            HomeProcedure homeProcedure = GF.Procedure.CurrentProcedure as HomeProcedure;
            homeProcedure.QuitGame();
        });
    }

    [System.Serializable]
    private class NoticeData
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("title")]
        public string Title;

        [JsonProperty("content")]
        public string Content;
    }

    [System.Serializable]
    private class NoticeList
    {
        [JsonProperty("notics")]
        public List<NoticeData> Notices;
    }

}

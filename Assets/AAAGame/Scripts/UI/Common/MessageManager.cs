

using Google.Protobuf;
using NetMsg;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityGameFramework.Runtime;
public class MessageManager
{
    private static MessageManager instance;
    private MessageManager() { }

    public class MsgParams
    {
        public long Time { get; set; }
        // 四个描述信息
        public Dictionary<string, string> DesDic { get; set; }
        // 确认和取消的操作
        public UnityAction OkAction { get; set; }
        public UnityAction CancelAction { get; set; }

        // 文本和颜色映射
        public Dictionary<string, Color> TextColors { get; set; }

        // 构造函数，简化初始化
        public MsgParams(long time, UnityAction okAction = null, UnityAction cancelAction = null)
        {
            Time = time;
            OkAction = okAction;
            CancelAction = cancelAction;
            DesDic = new Dictionary<string, string>();
            TextColors = new Dictionary<string, Color>();
        }
    }
    public static MessageManager GetInstance()
    {
        if (instance == null)
        {
            instance = new MessageManager();
        }
        return instance;
    }

    public void Init()
    {
        HotfixNetworkComponent.AddListener(MessageID.Msg_SysMsgRs, Function_SysMsgRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynSystemMsg, Function_SynSystemMsg);

        //申请加入公会列表登录推送2100006
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynApplyList, Function_SynApplyList);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynApplyInfo, Function_SynApplyInfo);
        //申请加入联盟登录推送2100012
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynApplyUnion, Function_SynApplyUnion);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynApplyUnionList, Function_SynApplyUnionList);
        //申请加入超级联盟登录推送2100012
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynJoinSuperUnionApplyRs, Function_SynJoinSuperUnionApplyRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_GetJoinSuperUnionApplyRs, Function_GetJoinSuperUnionApplyRs);

        //俱乐部申请联盟币
        HotfixNetworkComponent.AddListener(MessageID.Msg_GetApplyClubGoldListRs, Function_GetApplyClubGoldListRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynApplyClubGold, Function_SynApplyClubGold);

        //申请加入联盟登录推送2100012
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynInvitationInfo, Function_SynInvitationInfo);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynInvitationInfoList, Function_SynInvitationInfoList);

        //审核结果通知推送
        // HotfixNetworkComponent.AddListener(MessageID.Msg_SynApplyMsg, Function_SynApplyMsg);

        //红点通知
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynRedDot, Function_SynRedDot);

        //回放全局监听
        HotfixNetworkComponent.AddListener(MessageID.Msg_PlayBackRs, Function_NNPlayBackRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_PlayBackRs_zjh, Function_ZJHPlayBackRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_TexasPokerPlayBackRs, Function_DZPlayBackRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_CKPlayBackRs, Function_CKPlayBackRs);

    }

    public void Clear()
    {
        msg_SysMsgList.Clear();
        applyClubList.Clear();
        applyUnionList.Clear();
        invitationInfoList.Clear();
        applyClubGoldDict.Clear();
        applySuperUnionList.Clear();
    }

    public void RequestAllMsg()
    {
        RequestSysMsg();
        RequestLeagueMsg();
        RequestCoinMsg();
    }

    public void RequestSysMsg()
    {
        Send_SysMsgRq();
    }

    public void RequestLeagueMsg()
    {
        Send_getApplyClubInfoRq();
        Send_ClubUserChangeRq();
        Send_UnionClubChangeRq();
        Send_getJoinSuperUnionApplyRq();
        Send_GetApplyClubGoldListRq();
        Send_GetApplyClubGoldListRq_super();
    }

    public void RequestCoinMsg()
    {
        Send_getBringListRq();
        Send_GoldMsgRq();
    }
    public void Function_SynRedDot(MessageRecvData data)
    {
        Msg_SynRedDot ack = Msg_SynRedDot.Parser.ParseFrom(data.Data);
        GF.LogInfo("红点通知", ack.ToString());
        switch (ack.RedInfo.Key)
        {
            case 0:
                RedDotManager.GetInstance().Set(E_RedDotDefine.MsgBox_System, ack.RedInfo.Val);
                break;
            case 1:
                RedDotManager.GetInstance().Set(E_RedDotDefine.MsgBox_Team, ack.RedInfo.Val);
                break;
            case 2:
                RedDotManager.GetInstance().Set(E_RedDotDefine.MsgBox_Coin, ack.RedInfo.Val);
                break;
        }
        Util.UpdateClubInfoRq();
    }

    public void SysRedDotSum()
    {
        // int sum = 0;
        // sum += msg_SysMsgList != null ? msg_SysMsgList.Count : 0;
        // RedDotManager.GetInstance().Set(E_RedDotDefine.MsgBox_System, sum);
    }
    #region 官方通告

    private List<Msg_SysMsg> msg_SysMsgList = new();
    public List<Msg_SysMsg> Getmsg_SysMsgList()
    {
        return msg_SysMsgList;
    }

    public void Send_SysMsgRq()
    {
        Msg_SysMsgRq req = MessagePool.Instance.Fetch<Msg_SysMsgRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SysMsgRq, req);
    }

    public void Send_SynApplyMsg(int msgType)
    {
        Msg_getApplyOptionMsgRq req = MessagePool.Instance.Fetch<Msg_getApplyOptionMsgRq>();
        req.Type = msgType;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_getApplyOptionMsgRq, req);
    }

    public void Send_ClubUserChangeRq()
    {
        // Msg_ClubUserChangeRq req = MessagePool.Instance.Fetch<Msg_ClubUserChangeRq>();
        // HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
        // (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ClubUserChangeRq, req);
    }

    public void Send_UnionClubChangeRq()
    {
        // LeagueInfo info = GlobalManager.GetInstance().MyLeagueInfos.FirstOrDefault(item => item.Type == 2);
        // if (info == null)
        // {
        //     return; 
        // }
        // Msg_UnionClubChangeRq req = MessagePool.Instance.Fetch<Msg_UnionClubChangeRq>();
        // req.UnionId = info.LeagueId;
        // HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
        // (HotfixNetworkComponent.AccountClientName, MessageID.Msg_UnionClubChangeRq, req);
    }

    public void Function_SysMsgRs(MessageRecvData data)
    {
        //Msg_BringOptionRs
        Msg_SysMsgRs ack = Msg_SysMsgRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("官方通告", ack.ToString());
        msg_SysMsgList?.Clear();
        foreach (Msg_SysMsg item in ack.Msg)
        {
            msg_SysMsgList.Add(item);
        }
        Util.SortListByLatestTime(msg_SysMsgList, cell => cell.SendTime);
        SysRedDotSum();
    }

    public void Function_SynSystemMsg(MessageRecvData data)
    {
        //Msg_SynBringInfo
        GF.LogInfo("官方通告");
        Msg_SynSystemMsg ack = Msg_SynSystemMsg.Parser.ParseFrom(data.Data);
        msg_SysMsgList.Add(ack.Msg);
        Util.SortListByLatestTime(msg_SysMsgList, cell => cell.SendTime);
    }
    #endregion

    public void TeamRedDotSum()
    {
        // int sum = 0;
        // sum += applyClubList != null ? applyClubList.Count : 0;
        // sum += applyUnionList != null ? applyUnionList.Count : 0;
        // sum += invitationInfoList != null ? invitationInfoList.Count : 0;
        // RedDotManager.GetInstance().Set(E_RedDotDefine.MsgBox_Team, sum);
    }

    #region 申请加入公会列表登录推送

    private List<ApplyClub> applyClubList = new();
    public List<ApplyClub> GetApplyClubList()
    {
        return applyClubList;
    }

    public void UpdateApplyClubList(ApplyClub applyClub)
    {
        if (applyClub.State == 0)
        {
            ApplyClub applyClubCache = applyClubList.FirstOrDefault(item => item.BaseInfo.PlayerId == applyClub.BaseInfo.PlayerId);
            if (applyClubCache == null)
            {
                applyClubList.Add(applyClub);
            }
            else
            {
                applyClubCache = applyClub;
            }
        }
        else
        {
            applyClubList.RemoveAll(item => item.BaseInfo.PlayerId == applyClub.BaseInfo.PlayerId);
        }
        TeamRedDotSum();
    }

    public void Function_SynApplyList(MessageRecvData data)
    {
        //Msg_BringOptionRs
        Msg_SynApplyList ack = Msg_SynApplyList.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_SynApplyList申请加入公会列表登录推送", ack.ToString());
        applyClubList?.Clear();
        foreach (ApplyClub item in ack.ApplyClub)
        {
            UpdateApplyClubList(item);
        }
    }

    public void Function_SynApplyInfo(MessageRecvData data)
    {
        //Msg_SynBringInfo
        Msg_SynApplyInfo ack = Msg_SynApplyInfo.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_SynApplyInfo申请加入公会在线推送", ack.ToString());
        UpdateApplyClubList(ack.ApplyClub);
    }

    public void Send_ApplyLeagueRq(long userId, int state)
    {
        Msg_ApplyLeagueRq req = MessagePool.Instance.Fetch<Msg_ApplyLeagueRq>();
        req.UserId = userId;
        req.State = state;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ApplyLeagueRq, req);
    }

    public void Send_getApplyClubInfoRq()
    {
        if (GlobalManager.GetInstance().MyLeagueInfos == null || GlobalManager.GetInstance().MyLeagueInfos.Count == 0)
            return;
        LeagueInfo info = GlobalManager.GetInstance().MyLeagueInfos.FirstOrDefault(item => item.Type == 1);
        if (info == null)
        {
            return;
        }
        Msg_getApplyClubInfoRq req = MessagePool.Instance.Fetch<Msg_getApplyClubInfoRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_getApplyClubInfoRq, req);
    }

    #endregion

    #region 申请加入联盟登录推送

    private List<Msg_ApplyUnion> applyUnionList = new();
    public Msg_ApplyUnion[] GetApplyUnionList()
    {
        return applyUnionList.ToArray();
    }

    public void UpdateApplyUnionList(Msg_ApplyUnion applyUnion)
    {
        if (applyUnion.State == 0)
        {
            Msg_ApplyUnion applyUnionCache = applyUnionList.FirstOrDefault(item => item.LeagueId == applyUnion.LeagueId);
            if (applyUnionCache == null)
            {
                applyUnionList.Add(applyUnion);
            }
            else
            {
                applyUnionCache = applyUnion;
            }
        }
        else
        {
            applyUnionList.RemoveAll(item => item.LeagueId == applyUnion.LeagueId);
        }
        TeamRedDotSum();
    }

    public void Function_SynApplyUnionList(MessageRecvData data)
    {
        //Msg_SynApplyUnionList
        Msg_SynApplyUnionList ack = Msg_SynApplyUnionList.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_SynApplyUnionList申请加入联盟俱乐部列表登录推送", ack.ToString());
        applyUnionList?.Clear();
        foreach (Msg_ApplyUnion item in ack.Apply)
        {
            if (item.State == 0)
            {
                applyUnionList.Add(item);
            }
        }
        TeamRedDotSum();
    }
    public void Function_SynApplyUnion(MessageRecvData data)
    {
        //Msg_SynApplyUnion
        Msg_SynApplyUnion ack = Msg_SynApplyUnion.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_SynApplyUnion申请加入联盟俱乐部列表实时推送", ack.ToString());
        UpdateApplyUnionList(ack.Apply);
    }

    public void Send_ApplyClubRq(long clubId, int state)
    {
        Msg_ApplyClubRq req = MessagePool.Instance.Fetch<Msg_ApplyClubRq>();
        req.ClubId = clubId;
        req.State = state;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ApplyClubRq, req);
    }

    #endregion

    #region 邀请俱乐部加入联盟通知列表
    //联盟币申请记录列表
    List<Msg_InvitationInfo> invitationInfoList = new();
    public List<Msg_InvitationInfo> GetInvitationInfoList()
    {
        return invitationInfoList;
    }
    public void Function_SynInvitationInfo(MessageRecvData data)
    {
        Msg_SynInvitationInfo msg_SynInvitationInfo = Msg_SynInvitationInfo.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_InvitationInfo邀请俱乐部加入联盟通知", msg_SynInvitationInfo.ToString());
        invitationInfoList.Add(msg_SynInvitationInfo.Info);
        TeamRedDotSum();
    }

    public void Function_SynInvitationInfoList(MessageRecvData data)
    {
        Msg_SynInvitationInfoList ack = Msg_SynInvitationInfoList.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_InvitationInfo邀请俱乐部加入联盟通知列表", ack.ToString());
        invitationInfoList = ack.Info.ToList();
        Util.SortListByLatestTime(invitationInfoList, invitationInfoList => invitationInfoList.InviteTime);
        TeamRedDotSum();
    }

    public void Update_SynInvitationInfoList(long UnionId)
    {
        invitationInfoList.RemoveAll(info => info.Union.LeagueId == UnionId);
    }

    public void Send_OptionInvitationClubRq(long UnionId, int state)
    {
        LeagueInfo info = GlobalManager.GetInstance().MyLeagueInfos.FirstOrDefault(item => item.Type == 0);
        if (info == null)
        {
            return;
        }
        Msg_OptionInvitationClubRq req = MessagePool.Instance.Fetch<Msg_OptionInvitationClubRq>();
        req.UnionId = UnionId;
        req.SelfClub = (int)info.LeagueId;
        req.State = state;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_OptionInvitationClubRq, req);
    }
    #endregion

    #region 申请加入超级联盟登录推送

    private List<Msg_ApplySuperUnion> applySuperUnionList = new();
    public Msg_ApplySuperUnion[] GetApplySuperUnionList()
    {
        return applySuperUnionList.ToArray();
    }

    public void UpdateApplySuperUnion(Msg_ApplySuperUnion applySuperUnion)
    {
        if (applySuperUnion.State == 0)
        {
            Msg_ApplySuperUnion msg_ApplySuperUnion = applySuperUnionList.FirstOrDefault(item => item.SuperUnionId == applySuperUnion.SuperUnionId);
            if (msg_ApplySuperUnion == null)
            {
                applySuperUnionList.Add(applySuperUnion);
            }
            else
            {
                msg_ApplySuperUnion = applySuperUnion;
            }
        }
        else
        {
            applySuperUnionList.RemoveAll(item => item.SuperUnionId == applySuperUnion.SuperUnionId);
        }
        TeamRedDotSum();
    }

    public void Function_GetJoinSuperUnionApplyRs(MessageRecvData data)
    {
        //Msg_SynApplyUnionList
        Msg_GetJoinSuperUnionApplyRs ack = Msg_GetJoinSuperUnionApplyRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_SynApplyUnionList申请加入超级联盟列表登录推送", ack.ToString());
        applySuperUnionList?.Clear();
        foreach (Msg_ApplySuperUnion item in ack.Apply)
        {
            if (item.State == 0)
            {
                applySuperUnionList.Add(item);
            }
        }
        TeamRedDotSum();
    }

    public void Function_SynJoinSuperUnionApplyRs(MessageRecvData data)
    {
        //Msg_SynApplyUnionList
        Msg_SynJoinSuperUnionApplyRs ack = Msg_SynJoinSuperUnionApplyRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_SynApplyUnionList申请加入超级联盟实时推送", ack.ToString());
        UpdateApplySuperUnion(ack.Apply);
    }


    public void Send_ApplyJoinSuperUnionRq(long unionId, int state)
    {
        Msg_HandelJoinSuperUnionRq req = MessagePool.Instance.Fetch<Msg_HandelJoinSuperUnionRq>();
        req.UnionId = unionId;
        req.State = state;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_HandelJoinSuperUnionRq, req);
    }

    public void Send_getJoinSuperUnionApplyRq()
    {
        if (GlobalManager.GetInstance().MyLeagueInfos == null || GlobalManager.GetInstance().MyLeagueInfos.Count == 0)
            return;
        LeagueInfo info = GlobalManager.GetInstance().MyLeagueInfos.FirstOrDefault(item => item.Type == 2);
        if (info == null)
        {
            return;
        }
        Msg_GetJoinSuperUnionApplyRq req = MessagePool.Instance.Fetch<Msg_GetJoinSuperUnionApplyRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GetJoinSuperUnionApplyRq, req);
    }
    #endregion

    #region 资金消息

    public void CoinRedDotSum()
    {
        // int sum = 0;
        // var bringList = BringManager.GetInstance().GetBringInfoList();
        // if (bringList != null && bringList.Count > 0)
        // {
        //     for (int i = 0; i < bringList.Count; i++)
        //     {
        //         if (bringList[i].State == 0)
        //         {
        //             sum += 1;
        //         }
        //     }
        // }
        // if (applyClubGoldList != null && applyClubGoldList.Count > 0)
        // {
        //     for (int i = 0; i < applyClubGoldList.Count; i++)
        //     {
        //         if (applyClubGoldList[i].State == 0)
        //         {
        //             sum += 1;
        //         }
        //     }
        // }
        // RedDotManager.GetInstance().Set(E_RedDotDefine.MsgBox_Coin, sum);
    }

    //联盟币申请记录列表
    private Dictionary<long, Msg_ApplyClubGold> applyClubGoldDict = new();

    public List<Msg_ApplyClubGold> GetApplyClubGoldList()
    {
        return applyClubGoldDict.Values.ToList();
    }

    public void UpdateApplyClubGold(Msg_ApplyClubGold applyClubGold)
    {
        applyClubGoldDict ??= new Dictionary<long, Msg_ApplyClubGold>();
        if (applyClubGoldDict.ContainsKey(applyClubGold.ApplyId))
        {
            if (applyClubGold.State == 0)
            {
                applyClubGoldDict[applyClubGold.ApplyId] = applyClubGold;
            }
            else
            {
                applyClubGoldDict.Remove(applyClubGold.ApplyId);
            }
        }
        else if (applyClubGold.State == 0)
        {
            applyClubGoldDict.Add(applyClubGold.ApplyId, applyClubGold);
        }

        Util.SortListByLatestTime(applyClubGoldDict.Values.ToList(), item => item.OpTime);
        TeamRedDotSum();
    }


    public void Send_GetApplyClubGoldListRq()
    {
        if (GlobalManager.GetInstance().MyLeagueInfos == null || GlobalManager.GetInstance().MyLeagueInfos.Count == 0)
            return;
        LeagueInfo info = GlobalManager.GetInstance().MyLeagueInfos.FirstOrDefault(item => item.Type == 1);
        if (info == null)
        {
            return;
        }
        Msg_GetApplyClubGoldListRq req = MessagePool.Instance.Fetch<Msg_GetApplyClubGoldListRq>();
        req.ClubId = info.LeagueId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GetApplyClubGoldListRq, req);
    }
    //获取超盟
    public void Send_GetApplyClubGoldListRq_super()
    {
        if (GlobalManager.GetInstance().MyLeagueInfos == null || GlobalManager.GetInstance().MyLeagueInfos.Count == 0)
            return;
        LeagueInfo info = GlobalManager.GetInstance().MyLeagueInfos.FirstOrDefault(item => item.Type == 2);
        if (info == null)
        {
            return;
        }
        Msg_GetApplyClubGoldListRq req = MessagePool.Instance.Fetch<Msg_GetApplyClubGoldListRq>();
        req.ClubId = info.LeagueId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GetApplyClubGoldListRq, req);
    }
    public void Function_GetApplyClubGoldListRs(MessageRecvData data)
    {
        Msg_GetApplyClubGoldListRs GetApplyClubGoldListRs = Msg_GetApplyClubGoldListRs.Parser.ParseFrom(data.Data);
        applyClubGoldDict ??= new Dictionary<long, Msg_ApplyClubGold>();
        foreach (var item in GetApplyClubGoldListRs.Apply)
        {
            UpdateApplyClubGold(item);
        }
        GF.LogInfo("GetApplyClubGoldListRs公会联盟币申请记录", GetApplyClubGoldListRs.ToString());
    }

    public void Function_SynApplyClubGold(MessageRecvData data)
    {
        //Msg_SynApplyClubGold
        Msg_SynApplyClubGold ack = Msg_SynApplyClubGold.Parser.ParseFrom(data.Data);
        GF.LogInfo("俱乐部联盟币申请通知", ack.ToString());
        UpdateApplyClubGold(ack.Apply);
    }

    public void Update_ApplyClubGold(long applyId, int state)
    {
        if (applyClubGoldDict.ContainsKey(applyId))
        {
            applyClubGoldDict[applyId].State = state;
            CoinRedDotSum();
        }
    }

    public Msg_GoldMsgRs GoldMsgRs { get; set; }

    /// <summary>
    /// 资金消息
    /// </summary>
    public void Send_GoldMsgRq()
    {
        Msg_GoldMsgRq req = MessagePool.Instance.Fetch<Msg_GoldMsgRq>();
        req.PlayerId = GF.DataModel.GetDataModel<UserDataModel>().PlayerId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GoldMsgRq, req);
    }

    public void Send_getBringListRq()
    {
        Msg_getBringListRq req = MessagePool.Instance.Fetch<Msg_getBringListRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_getBringListRq, req);
    }
    #endregion

    // List<Msg_ApplyOption> applyMsgs;

    // public List<Msg_ApplyOption> GetApplyOptionMsgRs()
    // {
    //     return applyMsgs;
    // }

    // public void Function_SynApplyMsg(MessageRecvData data)
    // {
    //     Msg_SynApplyMsg msg = Msg_SynApplyMsg.Parser.ParseFrom(data.Data);
    //     GF.LogInfo("Msg_SynApplyMsg审核结果单条通知" , msg.ToString());
    //     applyMsgs.Add(msg.Applymsg);
    //     Util.SortListByLatestTime(applyMsgs, applyMsgs => applyMsgs.OpTime);
    // }

    #region 回放
    public async void Function_NNPlayBackRs(MessageRecvData data)
    {
        Msg_PlayBackRs ack = Msg_PlayBackRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到牛牛单局回放数据", ack.ToString());
        if (ack.Option != null && ack.Option.Count > 0)
        {
            // 记录每位玩家最后一个ORob操作的索引
            var playerLastORobIndex = new System.Collections.Generic.Dictionary<long, int>();
            // 记录所有ORob操作的索引
            var allORobIndices = new System.Collections.Generic.List<int>();

            for (int i = 0; i < ack.Option.Count; i++)
            {
                var operated = ack.Option[i];
                if (operated.Option == OptionType.ORob)
                {
                    playerLastORobIndex[operated.PlayerId] = i;
                    allORobIndices.Add(i);
                }
            }

            // 只保留每位玩家最后一个ORob操作的索引
            var keepIndices = new System.Collections.Generic.HashSet<int>(playerLastORobIndex.Values);

            // 倒序移除所有不需要保留的ORob操作
            for (int i = allORobIndices.Count - 1; i >= 0; i--)
            {
                int index = allORobIndices[i];
                if (!keepIndices.Contains(index))
                {
                    ack.Option.RemoveAt(index);
                }
            }

            GF.LogInfo("筛选后的牛牛回放数据", ack.ToString());
        }

        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("PlayBack", ack.ToByteArray());
        await GF.UI.OpenUIFormAwait(UIViews.NNRecordPanel, uiParams);
    }

    public async void Function_ZJHPlayBackRs(MessageRecvData data)
    {
        Msg_PlayBackRs ack = Msg_PlayBackRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到金花单局回放数据", ack.ToString());
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("PlayBack", ack.ToByteArray());
        await GF.UI.OpenUIFormAwait(UIViews.ZJHRecordPanel, uiParams);
    }

    public async void Function_DZPlayBackRs(MessageRecvData data)
    {
        Msg_TexasPokerPlayBackRs ack = Msg_TexasPokerPlayBackRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到德州单局回放数据", ack.ToString());
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("PlayBack", ack.ToByteArray());
        await GF.UI.OpenUIFormAwait(UIViews.DZRecordPanel, uiParams);
    }

    public async void Function_CKPlayBackRs(MessageRecvData data)
    {
        Msg_CKPlayBackRs ack = Msg_CKPlayBackRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到比鸡单局回放数据", ack.ToString());
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("PlayBack", ack.ToByteArray());
        await GF.UI.OpenUIFormAwait(UIViews.BJRecordPanel, uiParams);
    }

    #endregion
}
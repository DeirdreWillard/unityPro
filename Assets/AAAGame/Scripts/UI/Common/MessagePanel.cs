
using UnityEngine.UI;
using DG.Tweening;
using NetMsg;
using UnityEngine;

using static MessageManager;
using System.Collections.Generic;
using System.Linq;
using GameFramework.Event;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class MessagePanel : UIFormBase
{
    public RedDotItem msgDot;
    public RedDotItem teamDot;
    public RedDotItem coinDot;
    public Button BtnMSG1;
    public Button BtnMSG2;
    public Button BtnMSG3;
    public Text title;

    public Button BtnClose1;

    public GameObject MessageContent;
    public GameObject content;
    public GameObject MsgItem1;
    public GameObject MsgItem2;
    public GameObject BringInItem;
    public GameObject MsgItem4;
    public GameObject MsgItem5;

    public GameObject NoMsgPanel;

    private int type = 0;//0 系统 1组织 2资金
    private string[] playerOpDes = { "直接加入", "审批加入", "主动退出", "被踢出" };
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        MessageContent.SetActive(false);
    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        RedDotManager.GetInstance().SetRedDotNodeCallBack(E_RedDotDefine.MsgBox_System, MsgCallBack);
        RedDotManager.GetInstance().SetRedDotNodeCallBack(E_RedDotDefine.MsgBox_Team, TeamCallBack);
        RedDotManager.GetInstance().SetRedDotNodeCallBack(E_RedDotDefine.MsgBox_Coin, CoinCallBack);
        RedDotManager.GetInstance().RefreshAll();

        HotfixNetworkComponent.AddListener(MessageID.Msg_getApplyOptionMsgRs, Function_getApplyOptionMsgRs);
        // HotfixNetworkComponent.AddListener(MessageID.Msg_GoldMsgRs, Function_GoldMsgRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_ApplyLeagueRs, Function_ApplyLeagueRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_ApplyClubRs, Function_ApplyClubRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_HandelJoinSuperUnionRs, Function_HandelJoinSuperUnionRs);
        // HotfixNetworkComponent.AddListener(MessageID.Msg_ClubUserChangeRs, Function_ClubUserChangeRs);
        // HotfixNetworkComponent.AddListener(MessageID.Msg_UnionClubChangeRs, Function_UnionClubChangeRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynApplyClubGold, Function_SynApplyClubGold);
        HotfixNetworkComponent.AddListener(MessageID.Msg_OptionInvitationClubRs, Function_OptionInvitationClubRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_ApplyClubGoldOptionRs, Function_ApplyClubGoldOptionRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_UserApplyClubOptionRs, Function_UserApplyClubOptionRs);

        //在线推送
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynApplyInfo, Function_SynApplyInfo);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynApplyUnion, Function_SynApplyUnion);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynJoinSuperUnionApplyRs, Function_SynJoinSuperUnionApplyRs);

        GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);

        BtnClose1.onClick.AddListener(onBtnClose1Click);

        MessageManager.GetInstance().RequestAllMsg();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        RedDotManager.GetInstance().RemoveRedDotNodeCallBack(E_RedDotDefine.MsgBox_System, MsgCallBack);
        RedDotManager.GetInstance().RemoveRedDotNodeCallBack(E_RedDotDefine.MsgBox_Team, TeamCallBack);
        RedDotManager.GetInstance().RemoveRedDotNodeCallBack(E_RedDotDefine.MsgBox_Coin, CoinCallBack);

        HotfixNetworkComponent.RemoveListener(MessageID.Msg_getApplyOptionMsgRs, Function_getApplyOptionMsgRs);
        // HotfixNetworkComponent.RemoveListener(MessageID.Msg_GoldMsgRs, Function_GoldMsgRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_ApplyLeagueRs, Function_ApplyLeagueRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_ApplyClubRs, Function_ApplyClubRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_HandelJoinSuperUnionRs, Function_HandelJoinSuperUnionRs);
        // HotfixNetworkComponent.RemoveListener(MessageID.Msg_ClubUserChangeRs, Function_ClubUserChangeRs);
        // HotfixNetworkComponent.RemoveListener(MessageID.Msg_UnionClubChangeRs, Function_UnionClubChangeRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynApplyClubGold, Function_SynApplyClubGold);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_OptionInvitationClubRs, Function_OptionInvitationClubRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_ApplyClubGoldOptionRs, Function_ApplyClubGoldOptionRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_UserApplyClubOptionRs, Function_UserApplyClubOptionRs);

        //在线推送
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynApplyInfo, Function_SynApplyInfo);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynApplyUnion, Function_SynApplyUnion);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynJoinSuperUnionApplyRs, Function_SynJoinSuperUnionApplyRs);

        GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);

        BtnClose1.onClick.RemoveListener(onBtnClose1Click);
        base.OnClose(isShutdown, userData);
    }

    void MsgCallBack(RedDotNode node)
    {
        if(gameObject == null) return;
        msgDot.SetDotState(node.rdCount > 0, node.rdCount);
    }

    void TeamCallBack(RedDotNode node)
    {
        if(gameObject == null) return;
        teamDot.SetDotState(node.rdCount > 0, node.rdCount);
    }
    void CoinCallBack(RedDotNode node)
    {
        if(gameObject == null) return;
        coinDot.SetDotState(node.rdCount > 0, node.rdCount);
    }

    public void onBtnClose1Click()
    {
        // 判断是否在点击间隔内
        if (Util.IsClickLocked()) return;
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        ShowMessageContent(false);
    }

    public void ShowMessageContent(bool isShow)
    {
        if (isShow)
        {
            MessageContent.SetActive(true);
            MessageContent.transform.localPosition = new Vector3(1200, 0, 0);
            MessageContent.transform.DOLocalMoveX(0, 0.3f);
        }
        else
        {
            MessageContent.transform.DOLocalMoveX(1200, 0.3f).OnComplete(() =>
            {
                MessageContent.SetActive(false);
            });
        }
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "BtnMSG1":
                type = 0;
                MessageManager.GetInstance().RequestSysMsg();
                RedDotManager.GetInstance().Set(E_RedDotDefine.MsgBox_System, 0);
                MessageManager.GetInstance().Send_SynApplyMsg(0);
                break;
            case "BtnMSG2":
                type = 1;
                MessageManager.GetInstance().RequestLeagueMsg();
                RedDotManager.GetInstance().Set(E_RedDotDefine.MsgBox_Team, 0);
                MessageManager.GetInstance().Send_SynApplyMsg(1);
                break;
            case "BtnMSG3":
                type = 2;
                MessageManager.GetInstance().RequestCoinMsg();
                RedDotManager.GetInstance().Set(E_RedDotDefine.MsgBox_Coin, 0);
                MessageManager.GetInstance().Send_SynApplyMsg(2);
                break;
        }
    }

    public List<Msg_ApplyOption> applyMsgs = new();
    public void Function_getApplyOptionMsgRs(MessageRecvData data)
    {
        Msg_getApplyOptionMsgRs msg = Msg_getApplyOptionMsgRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_getApplyOptionMsgRs审核结果通知" , msg.ToString());
        applyMsgs = msg.Applymsg.ToList();
        Util.SortListByLatestTime(applyMsgs, applyMsgs => applyMsgs.OpTime);
        NoMsgPanel.SetActive(true);
        switch (type)
        {
            case 0:
                title.text = "系统消息";
                RushUI1();
                break;
            case 1:
                title.text = "组织消息";
                RushUI2();
                break;
            case 2:
                title.text = "资金消息";
                RushUI3();
                break;
        }
        ShowMessageContent(true);
    }

    #region 系统消息

    public void RushUI1()
    {
        foreach (Transform item in content.transform)
        {
            Destroy(item.gameObject);
        }
        var list = MessageManager.GetInstance().Getmsg_SysMsgList();
        if (list != null && list.Count != 0)
        {
            NoMsgPanel.SetActive(false);
            for (int i = 0; i < list.Count; i++)
            {
                var obj = Instantiate(MsgItem1, content.transform);
                obj.transform.Find("Content").GetComponent<Text>().text = list[i].Param;
                obj.transform.Find("Image/Time").GetComponent<Text>().text = Util.MillisecondsToDateString(list[i].SendTime);
            }
        }
        if (applyMsgs != null && applyMsgs.Count != 0)
        {
            NoMsgPanel.SetActive(false);
            for (int i = 0; i < applyMsgs.Count; i++)
            {
                var obj = Instantiate(MsgItem1, content.transform);
                obj.transform.Find("Content").GetComponent<Text>().text = applyMsgs[i].Msg;
                obj.transform.Find("Image/Time").GetComponent<Text>().text = Util.MillisecondsToDateString(applyMsgs[i].OpTime);
            }
        }
    }

    #endregion

    #region 组织消息
    // private Msg_ClubUserChangeRs clubUserChangeRs;
    public void Function_ClubUserChangeRs(MessageRecvData data)
    {
        GF.LogInfo("成员变动信息 组织消息");
        // clubUserChangeRs = Msg_ClubUserChangeRs.Parser.ParseFrom(data.Data);
    }

    // private Msg_UnionClubChangeRs unionClubChangeRs;
    public void Function_UnionClubChangeRs(MessageRecvData data)
    {
        // unionClubChangeRs = Msg_UnionClubChangeRs.Parser.ParseFrom(data.Data);
        // GF.LogInfo("申请加入联盟列表" , unionClubChangeRs.ToString());
    }

    public void Function_OptionInvitationClubRs(MessageRecvData data)
    {
        Msg_OptionInvitationClubRs ack = Msg_OptionInvitationClubRs.Parser.ParseFrom(data.Data);
        MessageManager.GetInstance().Update_SynInvitationInfoList(ack.UnionId);
        //通知界面更新
        RushUI2();
    }

    public void Function_ApplyLeagueRs(MessageRecvData data)
    {
        GF.LogInfo("个人申请加入俱乐部处理");
    }

    public void Function_SynApplyInfo(MessageRecvData data)
    {
        RushUI2();
    }

    public void Function_ApplyClubRs(MessageRecvData data)
    {
        //Msg_SynBringInfo
        GF.LogInfo("公会申请加入联盟处理");
    }

    public void Function_SynApplyUnion(MessageRecvData data)
    {
        RushUI2();
    }

    public void Function_HandelJoinSuperUnionRs(MessageRecvData data)
    {
        //Msg_SynBringInfo
        GF.LogInfo("联盟申请加入超级联盟处理");
    }

    public void Function_SynJoinSuperUnionApplyRs(MessageRecvData data)
    {
        RushUI2();
    }

    public void RushUI2()
    {
                // 两个独立的缓存列表
        List<KeyValuePair<long, GameObject>> reviewingItems = new List<KeyValuePair<long, GameObject>>();
        List<KeyValuePair<long, GameObject>> otherItems = new List<KeyValuePair<long, GameObject>>();

        foreach (Transform item in content.transform)
        {
            Destroy(item.gameObject);
        }
        //先显示超级联盟
        var superUnionList = MessageManager.GetInstance().GetApplySuperUnionList();
        if (superUnionList != null && superUnionList.Length != 0)
        {
            NoMsgPanel.SetActive(false);
            for (int i = 0; i < superUnionList.Length; i++)
            {
                var obj = Instantiate(MsgItem2, content.transform);
                var data = superUnionList[i];
                var msgParams = new MsgParams(
                    time: data.ApplyTime,
                    okAction: () =>
                    {
                       Sound.PlayEffect(AudioKeys.SOUND_BTN);
                        MessageManager.GetInstance().Send_ApplyJoinSuperUnionRq(data.UnionId, 0);
                    },
                    cancelAction: () =>
                    {
                       Sound.PlayEffect(AudioKeys.SOUND_BTN);
                        MessageManager.GetInstance().Send_ApplyJoinSuperUnionRq(data.UnionId, 1);
                    }
                    );

                msgParams.DesDic["Title"] = "超级联盟申请通知：";
                msgParams.DesDic["State"] = "申请加入";
                msgParams.TextColors["State"] = Color.green;
                msgParams.DesDic["Des1"] = data.BaseInfo.Nick;
                msgParams.DesDic["Des2"] = "ID:" + data.BaseInfo.PlayerId;

                obj.GetComponent<ClubMSGItem>().Init(msgParams);
                reviewingItems.Add(new KeyValuePair<long, GameObject>(data.ApplyTime, obj));
            }
        }
        //先显示联盟
        var UnionList = MessageManager.GetInstance().GetApplyUnionList();
        if (UnionList != null && UnionList.Length != 0)
        {
            NoMsgPanel.SetActive(false);
            for (int i = 0; i < UnionList.Length; i++)
            {
                var obj = Instantiate(MsgItem2, content.transform);
                var data = UnionList[i];
                var msgParams = new MsgParams(
                    time: data.ApplyTime,
                    okAction: () =>
                    {
                       Sound.PlayEffect(AudioKeys.SOUND_BTN);
                        MessageManager.GetInstance().Send_ApplyClubRq(data.LeagueId, 0);
                    },
                    cancelAction: () =>
                    {
                       Sound.PlayEffect(AudioKeys.SOUND_BTN);
                        MessageManager.GetInstance().Send_ApplyClubRq(data.LeagueId, 1);
                    }
                    );

                msgParams.DesDic["Title"] = "联盟申请通知：";
                msgParams.DesDic["State"] = "申请加入";
                msgParams.TextColors["State"] = Color.green;
                msgParams.DesDic["Des1"] = data.BaseInfo.Nick;
                msgParams.DesDic["Des2"] = "ID:" + data.BaseInfo.PlayerId;

                obj.GetComponent<ClubMSGItem>().Init(msgParams);
                reviewingItems.Add(new KeyValuePair<long, GameObject>(data.ApplyTime, obj));
            }
        }
        //显示俱乐部
        var ClubList = MessageManager.GetInstance().GetApplyClubList();
        if (ClubList != null && ClubList.Count != 0)
        {
            NoMsgPanel.SetActive(false);
            for (int i = 0; i < ClubList.Count; i++)
            {
                var obj = Instantiate(MsgItem2, content.transform);
                var data = ClubList[i];
                var msgParams = new MsgParams(
                        time: data.ApplyTime,
                        okAction: () =>
                        {
                           Sound.PlayEffect(AudioKeys.SOUND_BTN);
                            MessageManager.GetInstance().Send_ApplyLeagueRq(data.BaseInfo.PlayerId, 0);
                        },
                        cancelAction: () =>
                        {
                           Sound.PlayEffect(AudioKeys.SOUND_BTN);
                            MessageManager.GetInstance().Send_ApplyLeagueRq(data.BaseInfo.PlayerId, 1);
                        }
                        );
                msgParams.DesDic["Title"] = "俱乐部申请通知：";
                msgParams.DesDic["State"] = "申请加入";
                msgParams.TextColors["State"] = Color.green;
                msgParams.DesDic["Des1"] = data.BaseInfo.Nick;
                msgParams.DesDic["Des2"] = "ID:" + data.BaseInfo.PlayerId;

                obj.GetComponent<ClubMSGItem>().Init(msgParams);
                reviewingItems.Add(new KeyValuePair<long, GameObject>(data.ApplyTime, obj));
            }
        }

        //显示联盟邀请信息
        var invitationInfoList = MessageManager.GetInstance().GetInvitationInfoList();
        if (invitationInfoList != null && invitationInfoList.Count != 0)
        {
            NoMsgPanel.SetActive(false);
            for (int i = 0; i < invitationInfoList.Count; i++)
            {
                var obj = Instantiate(MsgItem2, content.transform);
                var data = invitationInfoList[i];
                var msgParams = new MsgParams(
                        time: data.InviteTime,
                        okAction: () =>
                        {
                           Sound.PlayEffect(AudioKeys.SOUND_BTN);
                            MessageManager.GetInstance().Send_OptionInvitationClubRq(data.Union.LeagueId, 0);
                        },
                        cancelAction: () =>
                        {
                           Sound.PlayEffect(AudioKeys.SOUND_BTN);
                            MessageManager.GetInstance().Send_OptionInvitationClubRq(data.Union.LeagueId, 1);
                        }
                        );
                msgParams.DesDic["Title"] = "联盟邀请通知：";
                msgParams.DesDic["State"] = "邀请加入";
                msgParams.TextColors["State"] = Color.red;
                msgParams.DesDic["Des1"] = data.Inviter.Nick;
                msgParams.DesDic["Des2"] = "ID:" + data.Inviter.PlayerId;

                obj.GetComponent<ClubMSGItem>().Init(msgParams);
                reviewingItems.Add(new KeyValuePair<long, GameObject>(data.InviteTime, obj));
            }
        }

        if (applyMsgs != null && applyMsgs.Count != 0)
        {
            NoMsgPanel.SetActive(false);
            for (int i = 0; i < applyMsgs.Count; i++)
            {
                var obj = Instantiate(MsgItem1, content.transform);
                obj.transform.Find("Content").GetComponent<Text>().text = applyMsgs[i].Msg;
                obj.transform.Find("Image/Time").GetComponent<Text>().text = Util.MillisecondsToDateString(applyMsgs[i].OpTime);
                otherItems.Add(new KeyValuePair<long, GameObject>(applyMsgs[i].OpTime, obj));
            }
        }

        // 排序两个列表（按时间降序）
        var sortedReviewing = reviewingItems.OrderByDescending(pair => pair.Key).ToList();
        var sortedOther = otherItems.OrderByDescending(pair => pair.Key).ToList();

        // 更新 GameObject 的层级，先审核中，再其他
        int index = 0;
        foreach (var item in sortedReviewing)
        {
            item.Value.transform.SetSiblingIndex(index++);
        }

        foreach (var item in sortedOther)
        {
            item.Value.transform.SetSiblingIndex(index++);
        }

        #region 废弃
        //显示成员变动
        // if (clubUserChangeRs != null && clubUserChangeRs.ChangeInfos.Count > 0)
        // {
        //     NoMsgPanel.gameObject.SetActive(false);
        //     for (int i = 0; i < clubUserChangeRs.ChangeInfos.Count; i++)
        //     {
        //         var obj = Instantiate(MsgItem4, content.transform);
        //         obj.transform.Find("Title").GetComponent<Text>().text = "俱乐部成员变动通知";
        //         obj.transform.Find("Des1").GetComponent<Text>().text = Util.MillisecondsToDateString(clubUserChangeRs.ChangeInfos[i].OpTime);
        //         obj.transform.Find("Des1").GetComponent<Text>().text = clubUserChangeRs.ChangeInfos[i].Player.Nick;
        //         obj.transform.Find("Des2").GetComponent<Text>().text = clubUserChangeRs.ChangeInfos[i].Player.PlayerId.ToString();
        //         obj.transform.Find("State").GetComponent<Text>().text = playerOpDes[clubUserChangeRs.ChangeInfos[i].Type];
        //         //主动退出
        //         obj.transform.Find("OpPlayer").GetComponent<Text>().text =
        //             clubUserChangeRs.ChangeInfos[i].Type == 2 ? "" : clubUserChangeRs.ChangeInfos[i].Player.PlayerId.ToString();
        //     }
        // }

        //显示俱乐部变动
        // if (unionClubChangeRs != null && unionClubChangeRs.ClubChanges.Count > 0)
        // {
        //     NoMsgPanel.gameObject.SetActive(false);
        //     for (int i = 0; i < unionClubChangeRs.ClubChanges.Count; i++)
        //     {
        //         var obj = Instantiate(MsgItem4, content.transform);
        //         obj.transform.Find("Clubname").GetComponent<Text>().text = "俱乐部变动通知";
        //         obj.transform.Find("name").GetComponent<Text>().text = Util.MillisecondsToDateString(unionClubChangeRs.ClubChanges[i].OpTime);
        //         obj.transform.Find("name").GetComponent<Text>().text = unionClubChangeRs.ClubChanges[i].LeagueName;
        //         obj.transform.Find("ID").GetComponent<Text>().text = unionClubChangeRs.ClubChanges[i].LeagueId.ToString();
        //         obj.transform.Find("State").GetComponent<Text>().text = playerOpDes[unionClubChangeRs.ClubChanges[i].Type];
        //         obj.transform.Find("OpPlayer").GetComponent<Text>().text = "";
        //     }
        // }
        #endregion
    }

    #endregion

    #region 资金消息

    public void Function_ApplyClubGoldOptionRs(MessageRecvData data)
    {
        Msg_ApplyClubGoldOptionRs ack = Msg_ApplyClubGoldOptionRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_ApplyClubGoldOptionRs处理俱乐部申请信息返回" , ack.ToString());
        MessageManager.GetInstance().Update_ApplyClubGold(ack.ApplyId, ack.State);
        //通知界面更新
        RushUI3();
    }

    public void Function_UserApplyClubOptionRs(MessageRecvData data)
    {
        Msg_UserApplyClubOptionRs ack = Msg_UserApplyClubOptionRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_UserApplyClubOptionRs处理玩家申请俱乐部联盟币信息返回" , ack.ToString());
        MessageManager.GetInstance().Update_ApplyClubGold(ack.ApplyId, ack.State);
        //通知界面更新
        RushUI3();
    }

    public void Function_SynApplyClubGold(MessageRecvData data)
    {
        RushUI3();
    }

    public void Function_GoldMsgRs(MessageRecvData data)
    {
        // MessageManager.GetInstance().GoldMsgRs = Msg_GoldMsgRs.Parser.ParseFrom(data.Data);
        // GF.LogInfo("Msg_GoldMsgRs资金消息返回" , MessageManager.GetInstance().GoldMsgRs.ToString());
        // RushUI3();
    }
    public void RushUI3()
    {
        foreach (Transform item in content.transform)
        {
            Destroy(item.gameObject);
        }

        // 两个独立的缓存列表
        List<KeyValuePair<long, GameObject>> reviewingItems = new List<KeyValuePair<long, GameObject>>();
        List<KeyValuePair<long, GameObject>> otherItems = new List<KeyValuePair<long, GameObject>>();

        List<Msg_ApplyClubGold> applyClubGoldList = MessageManager.GetInstance().GetApplyClubGoldList();
        if (applyClubGoldList != null && applyClubGoldList.Count != 0)
        {
            NoMsgPanel.SetActive(false);
            for (int i = 0; i < applyClubGoldList.Count; i++)
            {
                var obj = Instantiate(MsgItem5, content.transform);
                obj.GetComponent<UnionCoinMSGItem>().Init(applyClubGoldList[i]);

                if (applyClubGoldList[i].State == 0)
                {
                    reviewingItems.Add(new KeyValuePair<long, GameObject>(applyClubGoldList[i].OpTime, obj));
                }
                else
                {
                    otherItems.Add(new KeyValuePair<long, GameObject>(applyClubGoldList[i].OpTime, obj));
                }
            }
        }

        Msg_GoldMsgRs goldMsgRs = MessageManager.GetInstance().GoldMsgRs;
        if (goldMsgRs != null && goldMsgRs.Coins.Count != 0)
        {
            NoMsgPanel.SetActive(false);
            for (int i = 0; i < goldMsgRs.Coins.Count; i++)
            {
                var obj = Instantiate(MsgItem1, content.transform);
                string des = goldMsgRs.Coins[i].Title + "<color=#EAC190>" + goldMsgRs.Coins[i].ChangeNum + "</color>  剩余<color=#EAC190>" +
                    goldMsgRs.Coins[i].AfterNum + "</color>";
                obj.transform.Find("Content").GetComponent<Text>().text = des;
                obj.transform.Find("Image/Time").GetComponent<Text>().text = Util.MillisecondsToDateString(goldMsgRs.Coins[i].OpTime);

                // 归类到“其他项”
                otherItems.Add(new KeyValuePair<long, GameObject>(goldMsgRs.Coins[i].OpTime, obj));
            }
        }

        var bringInfoList = BringManager.GetInstance().GetBringInfoList();
        var bringInfoListOver = BringManager.GetInstance().GetBringInfoListOver();

        // 创建带审核状态的列表
        for (int i = 0; i < bringInfoList.Count; i++)
        {
            GameObject newItem = Instantiate(BringInItem);
            newItem.transform.SetParent(content.transform, false);
            newItem.GetComponent<BringItem>().Init(bringInfoList[i]);

            if (bringInfoList[i].State == 0)
                reviewingItems.Add(new KeyValuePair<long, GameObject>(bringInfoList[i].Time, newItem));
            else
                otherItems.Add(new KeyValuePair<long, GameObject>(bringInfoList[i].Time, newItem));
        }

        // 创建过期列表
        for (int i = 0; i < bringInfoListOver.Count; i++)
        {
            GameObject newItem = Instantiate(BringInItem);
            newItem.transform.SetParent(content.transform, false);
            newItem.GetComponent<BringItem>().Init(bringInfoListOver[i]);
            otherItems.Add(new KeyValuePair<long, GameObject>(bringInfoListOver[i].Time, newItem));
        }

        if (applyMsgs != null && applyMsgs.Count != 0)
        {
            NoMsgPanel.SetActive(false);
            for (int i = 0; i < applyMsgs.Count; i++)
            {
                var obj = Instantiate(MsgItem1, content.transform);
                obj.transform.Find("Content").GetComponent<Text>().text = applyMsgs[i].Msg;
                obj.transform.Find("Image/Time").GetComponent<Text>().text = Util.MillisecondsToDateString(applyMsgs[i].OpTime);
                otherItems.Add(new KeyValuePair<long, GameObject>(applyMsgs[i].OpTime, obj));
            }
        }

        // 排序两个列表（按时间降序）
        var sortedReviewing = reviewingItems.OrderByDescending(pair => pair.Key).ToList();
        var sortedOther = otherItems.OrderByDescending(pair => pair.Key).ToList();

        // 更新 GameObject 的层级，先审核中，再其他
        int index = 0;
        foreach (var item in sortedReviewing)
        {
            item.Value.transform.SetSiblingIndex(index++);
        }

        foreach (var item in sortedOther)
        {
            item.Value.transform.SetSiblingIndex(index++);
        }
    }

    private void OnUserDataChanged(object sender, GameEventArgs e)
    {
        var args = e as UserDataChangedEventArgs;
        switch (args.Type)
        {
            case UserDataType.Msg:
                RushUI3();
                GF.LogInfo("收到消息变更");
                break;
        }
    }

    #endregion


}

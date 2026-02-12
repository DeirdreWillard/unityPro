
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using NetMsg;
using Google.Protobuf;
using GameFramework.Event;
using System.Linq;
using System.Collections.Generic;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class ClubDetailPanelContent : UIFormBase
{
    public RedDotItem redDot;
    public LeagueInfo leagueInfo;
    public Text TxtName;
    public Text TxtId;
    public Text TxtContent;
    public RawImage avatar;

    public GameObject[] Masks;
    public GameObject UpdateClubName;
    public GameObject BtnUserList;

    public GameObject BtnQuitClub;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.Msg_QuitLeagueRs, OnMsg_QuitLeagueRsAck);
        HotfixNetworkComponent.AddListener(MessageID.Msg_DisMissLeagueRs, OnMsg_DisMissLeagueRsAck);
        GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);

        RedDotManager.GetInstance().SetRedDotNodeCallBack(E_RedDotDefine.MsgBox, MsgCallBack);
        RedDotManager.GetInstance().RefreshAll();

        leagueInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("leagueInfo"));
        SetUI();
        Util.GetInstance().CheckSafeCodeState(gameObject);
    }

    public void SetUI()
    {
        TxtName.text = leagueInfo.LeagueName;
        TxtId.text = leagueInfo.LeagueId.ToString();
        TxtContent.text = leagueInfo.Introduction == "" ? "暂无简介" : leagueInfo.Introduction;
        Util.DownloadHeadImage(avatar, leagueInfo.LeagueHead, leagueInfo.Type);

        for (int i = 0; i < Masks.Length; i++)
        {
            if (i < leagueInfo.Users.Count)
            {
                Masks[i].SetActive(true);
                Util.DownloadHeadImage(Masks[i].transform.Find("avatar").GetComponent<RawImage>(), leagueInfo.Users[i].HeadImage);
            }
            else
            {
                Masks[i].SetActive(false);
            }
        }

        //是否是创建者
        bool isCreate = Util.IsMySelf(leagueInfo.Creator);
        BtnQuitClub.transform.Find("Text").GetComponent<Text>().text = isCreate ? "解散俱乐部" : "退出俱乐部";
        // BtnCancel.SetActive(isCreate);
        UpdateClubName.SetActive(isCreate);
        BtnUserList.SetActive(isCreate);
        varAllianceBtn.SetActive(isCreate);
        varAssetsBtn.SetActive(isCreate);
        varJackPotBtn.SetActive(isCreate);
        varGameRecordBtn.SetActive(isCreate);
        varTeamBtn.SetActive(Util.GetMyselfInfo().ClubLeagueDataMap[leagueInfo.LeagueId].Identify == 9 ||
        Util.GetMyselfInfo().ClubLeagueDataMap[leagueInfo.LeagueId].Identify == 1);
        varBtnCopy.SetActive(!isCreate);
        varShareBtn.SetActive(isCreate);
   
    }

    private void OnClubDataChanged(object sender, GameEventArgs e)
    {
        var args = e as ClubDataChangedEventArgs;
        switch (args.Type)
        {
            case ClubDataType.eve_LeagueDateUpdate:
                GF.LogInfo("俱乐部信息更新");
                leagueInfo = GlobalManager.GetInstance().LeagueInfo;
                SetUI();
                break;
        }
    }

    void MsgCallBack(RedDotNode node)
    {
        if (gameObject == null) return;
        redDot.SetDotState(node.rdCount > 0, node.rdCount);
    }

    public void OnMsg_QuitLeagueRsAck(MessageRecvData data)
    {
        Msg_QuitLeagueRs ack = Msg_QuitLeagueRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到退出");
        if (GF.Procedure.CurrentProcedure is HomeProcedure homeProcedure)
        {
            homeProcedure.ShowHomePanel(true);
        }
    }
    public void OnMsg_DisMissLeagueRsAck(MessageRecvData data)
    {
        Msg_DisMissLeagueRs ack = Msg_DisMissLeagueRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到注销");
        if (GF.Procedure.CurrentProcedure is HomeProcedure homeProcedure)
        {
            homeProcedure.ShowHomePanel(true);
        }
    }

    protected override async void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("leagueInfo", leagueInfo.ToByteArray());
        switch (btId)
        {
            case "AllianceBtn":
                if (leagueInfo.Father == 0)
                    await GF.UI.OpenUIFormAwait(UIViews.JoinAlliance, uiParams);
                else
                    await GF.UI.OpenUIFormAwait(UIViews.ClubAllianceCoinOpPanel, uiParams);
                break;
            case "AssetsBtn":
                await GF.UI.OpenUIFormAwait(UIViews.ClubAssetsPanel, uiParams);
                break;
            case "JackPotBtn":
                await GF.UI.OpenUIFormAwait(UIViews.ClubJackpotPanel, uiParams);
                break;
            case "GameRecordBtn":
                await GF.UI.OpenUIFormAwait(UIViews.ClubGameRecord, uiParams);
                break;
            case "TeamBtn":
                if (leagueInfo.Creator == Util.GetMyselfInfo().PlayerId)
                {
                    await GF.UI.OpenUIFormAwait(UIViews.ClubTeamManagePanel, uiParams);
                }
                else
                {
                    uiParams.Set<VarInt64>("leaderId", Util.GetMyselfInfo().PlayerId);
                    uiParams.Set<VarFloat>("rebate", Util.GetMyselfInfo().ClubLeagueDataMap[leagueInfo.LeagueId].Rebate);
                    await GF.UI.OpenUIFormAwait(UIViews.TeamUserManagePanel, uiParams);
                }
                break;
            case "UpdateClubName":
                await GF.UI.OpenUIFormAwait(UIViews.ResetClubPanel, uiParams);
                break;
            case "UserList":
                if (leagueInfo.Creator == Util.GetMyselfInfo().PlayerId)
                {
                    await GF.UI.OpenUIFormAwait(UIViews.ClubUserManagePanel, uiParams);
                }
                break;
            case "BtnCopy":
                bool isCreate = Util.IsMySelf(leagueInfo.Creator);//是否是会长
                if (!isCreate)
                {
                    GUIUtility.systemCopyBuffer = leagueInfo.LeagueId.ToString();
                    GF.UI.ShowToast("复制成功!");
                    break;
                }
                else
                {
                    //打开分享链接列表二级界面
                    ShowShareUrlList();
                    break;
                }
            case "BtnMessage":
                await GF.UI.OpenUIFormAwait(UIViews.MessagePanel);
                break;
            case "BtnCancel":
                Util.GetInstance().OpenConfirmationDialog("注销俱乐部", "确定要注销俱乐部", () =>
                 {
                     GF.LogInfo("注销俱乐部");
                     Msg_DisMissLeagueRq req = MessagePool.Instance.Fetch<Msg_DisMissLeagueRq>();
                     req.ClubId = leagueInfo.LeagueId;
                     HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                                      (HotfixNetworkComponent.AccountClientName, MessageID.Msg_DisMissLeagueRq, req);
                 });
                break;
            case "BtnQuitClub":
                string title = leagueInfo.Creator == Util.GetMyselfInfo().PlayerId ? "解散俱乐部" : "退出俱乐部";
                Util.GetInstance().OpenConfirmationDialog(title, "确定要" + title, () =>
                {
                    GF.LogInfo("退出或者解散俱乐部");
                    Msg_QuitLeagueRq req = MessagePool.Instance.Fetch<Msg_QuitLeagueRq>();
                    req.LeagueId = leagueInfo.LeagueId;
                    HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                    (HotfixNetworkComponent.AccountClientName, MessageID.Msg_QuitLeagueRq, req);
                });
                break;
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        varShareUrlList.SetActive(false);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_QuitLeagueRs, OnMsg_QuitLeagueRsAck);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_DisMissLeagueRs, OnMsg_DisMissLeagueRsAck);
        GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        RedDotManager.GetInstance().RemoveRedDotNodeCallBack(E_RedDotDefine.MsgBox, MsgCallBack);
        base.OnClose(isShutdown, userData);
    }

    private void ShowShareUrlList()
    {
        string[] urls = GlobalManager.Get_allShareUrls();
        if (urls == null || urls.Length == 0)
        {
            GUIUtility.systemCopyBuffer = leagueInfo.LeagueId.ToString();
            GF.UI.ShowToast("复制成功!");
            return;
        }
        foreach (Transform item in varContent.transform)
        {
            Destroy(item.gameObject);
        }
        for (int i = 0; i < urls.Length; i++)
        {
            GameObject newItem = Instantiate(varShareItem);
            string shareUrl = urls[i] + "?clubId=" + leagueInfo.LeagueId;
            newItem.transform.SetParent(varContent.transform, false);

            // 设置文本内容
            Text textComponent = newItem.transform.Find("Text").GetComponent<Text>();
            textComponent.text = shareUrl;

            // 为文本添加Button组件，实现点击打开网站功能
            Button textButton = textComponent.gameObject.GetComponent<Button>();
            if (textButton == null)
            {
                textButton = textComponent.gameObject.AddComponent<Button>();
            }
            textButton.onClick.RemoveAllListeners();
            textButton.onClick.AddListener(delegate ()
            {
               Sound.PlayEffect(AudioKeys.SOUND_BTN);
                Application.OpenURL(shareUrl);
            });

            // 复制按钮功能
            newItem.transform.Find("BtnCopy").GetComponent<Button>().onClick.AddListener(delegate ()
            {
               Sound.PlayEffect(AudioKeys.SOUND_BTN);
                GUIUtility.systemCopyBuffer = shareUrl;
                GF.UI.ShowToast("复制成功!");
            });
        }
        varShareUrlList.SetActive(true);
    }


}

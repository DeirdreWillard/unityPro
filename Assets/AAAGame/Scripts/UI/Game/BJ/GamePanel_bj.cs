﻿using DG.Tweening;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Event;
using static UtilityBuiltin;
using UnityGameFramework.Runtime;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine.Networking;
using System.Linq;
using System;
using System.IO;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class GamePanel_bj : UIFormBase
{
    public GameObject btnGameBegain;
    BJProcedure bj_procedure;

    public SeatManager_bj seatMgr;
    // UI节点
    public GameObject menu;
    public Text goldtxt;
    public Text diamondstxt;
    public GameObject bringUI;

    public Transform roomInfo;

    public CountDownFxControl countdownCtr;

    //弹幕
    public Transform barrage;
    public Transform barrageItem;

    //暂停
    public Transform ui_game_pause;

    public Card[] localCards;

    public List<Toggle> toggleTable;

    #region 金币动画对象池管理
    private List<GameObject> m_CoinObjs = new List<GameObject>();
    #endregion

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        #region 
        seatMgr = transform.Find("Seats").GetComponent<SeatManager_bj>();
        countdownCtr = transform.Find("Buttons/Time").GetComponent<CountDownFxControl>();
        btnGameBegain = transform.Find("LBBase/RoomInfo/BtnGameBegain").gameObject;
        menu = transform.Find("Panels/btnOpenMenu/Menu").gameObject;
        goldtxt = menu.transform.Find("Menuc/GamegoldButton/gold").GetComponent<Text>();
        diamondstxt = menu.transform.Find("Menuc/GamediamondsButton/diamonds").GetComponent<Text>();
        bringUI = transform.Find("Panels/BringUI").gameObject;
        roomInfo = transform.Find("LBBase/RoomInfo");
        barrage = transform.Find("Perfabs/BarrageRoot/Barrage");
        barrageItem = transform.Find("Perfabs/BarrageRoot/BarrageItem");
        ui_game_pause = transform.Find("Panels/ui_game_pause");

        varLocalOP.InitPanel();
        #endregion
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        RedDotManager.GetInstance().SetRedDotNodeCallBack(E_RedDotDefine.MsgBox, MsgCallBack);
        RedDotManager.GetInstance().RefreshAll();

        foreach (var toggle in toggleTable)
        {
            toggle.onValueChanged.AddListener(OnTableBGToggleChanged);
        }

        bj_procedure = GF.Procedure.CurrentProcedure as BJProcedure;
        CoroutineRunner.Instance.StartCoroutine(InitPanel());
    }

    public IEnumerator InitPanel(bool isAni = true)
    {
        ClearTable();
        Util.UpdateTableColor(transform.GetComponent<Image>());
        varBtnGameWait.SetActive(bj_procedure.enterBJDeskRs.BaseInfo.DeskType != DeskType.Match);
        menu.transform.Find("Menuc/BtnSitUp").gameObject.SetActive(bj_procedure.enterBJDeskRs.BaseInfo.DeskType != DeskType.Match);
        menu.transform.Find("Menuc/BtnDissolveRoom").gameObject.SetActive(bj_procedure.enterBJDeskRs.BaseInfo.DeskType != DeskType.Match);
        menu.transform.Find("Menuc/BringBtn").gameObject.SetActive(bj_procedure.enterBJDeskRs.BaseInfo.DeskType != DeskType.Match);
        menu.transform.Find("Menuc/ShopButton").gameObject.SetActive(bj_procedure.enterBJDeskRs.BaseInfo.DeskType != DeskType.Match);
        menu.transform.Find("Menuc/GamegoldButton").gameObject.SetActive(bj_procedure.enterBJDeskRs.BaseInfo.DeskType != DeskType.Match);
        menu.transform.Find("Menuc/GamediamondsButton").gameObject.SetActive(bj_procedure.enterBJDeskRs.BaseInfo.DeskType != DeskType.Match);
        varMatchRoomInfo.gameObject.SetActive(bj_procedure.enterBJDeskRs.BaseInfo.DeskType == DeskType.Match);
        varMatchRoomTime.gameObject.SetActive(bj_procedure.enterBJDeskRs.BaseInfo.DeskType == DeskType.Match);
        //初始化座位
        seatMgr.Init(bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.PlayerNum);
        //要不要显示开始
        btnGameBegain.SetActive(IsRoomCreate() && bj_procedure.enterBJDeskRs.BaseInfo.State == DeskState.WaitStart);
        //初始化奖池
        varBtnJackpot.SetActive(bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.Jackpot);
        //初始化Wpk
        varBtnWPK.SetActive(bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.OpenSafe);
        //初始化房间信息
        varRoomNameID.text = "房间:" + bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.DeskName + "(ID:" + bj_procedure.deskID + ")";
        varRoomInfo.text = "底注: " + bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.BaseCoin +
                                      "  王牌: " + (bj_procedure.enterBJDeskRs.BaseInfo.CkConfig.King == 0 ? "无王牌" : bj_procedure.enterBJDeskRs.BaseInfo.CkConfig.King + "张") +
                                      "\n摆牌时间: " + bj_procedure.enterBJDeskRs.BaseInfo.CkConfig.OptionTime +
                                      (bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.IpLimit && bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.GpsLimit ? "\nIP/GPS限制" :
                                      (bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.IpLimit ? "\nIP限制" : "") +
                                      (bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.GpsLimit ? "\nGPS限制" : ""));


        SetDeskState(bj_procedure.enterBJDeskRs.BaseInfo.State);
        //初始化座位玩家信息
        for (int i = 0; i < bj_procedure.enterBJDeskRs.DeskPlayers.Count; i++)
        {
            seatMgr.PlayerEnter(bj_procedure.enterBJDeskRs.DeskPlayers[i], isAni);
        }
        yield return new WaitForSeconds(0.3f);
        Seat_bj selfPlayer = seatMgr.GetSelfSeat();
        if (selfPlayer != null)
        {
            ShowBtnCancelLiuZuo(selfPlayer.playerInfo.State == PlayerState.OffLine);
        }
        RefreshUI();
        UpdateJackpot();

        if(bj_procedure.enterBJDeskRs.BaseInfo.DeskType == DeskType.Match){
            Msg_getSingleMatchRq req = MessagePool.Instance.Fetch<Msg_getSingleMatchRq>();
            req.MatchId = bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.MatchId;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_getSingleMatchRq, req);
        }
    }

    public void RefreshUI()
    {
        switch (bj_procedure.enterBJDeskRs.CkState)
        {
            case CompareChickenState.CkWait:
                break;
            case CompareChickenState.CkDealCard:
            case CompareChickenState.CkGroupCard:
                //显示倒计时
                int defaultTime = bj_procedure.enterBJDeskRs.BaseInfo.CkConfig.OptionTime;
                long serverTime = Util.GetServerTime();
                long nextTime = bj_procedure.enterBJDeskRs.NextTime;
                int remainTime = nextTime > serverTime ? (int)((nextTime - serverTime) / 1000) : 0;

                if (remainTime > 0)
                {
                    countdownCtr.gameObject.SetActive(true);
                    countdownCtr.ResetAndStartCountDown(remainTime > defaultTime ? remainTime : defaultTime, remainTime, true);
                }
                else
                {
                    countdownCtr.gameObject.SetActive(false);
                }

                if (bj_procedure.enterBJDeskRs.OtherComposeState != null && bj_procedure.enterBJDeskRs.OtherComposeState.Count > 0)
                {
                    for (int i = 0; i < bj_procedure.enterBJDeskRs.OtherComposeState.Count; i++)
                    {
                        Seat_bj seat = seatMgr.GetPlayerByPlayerID(bj_procedure.enterBJDeskRs.OtherComposeState[i].Key);
                        if (seat != null && seat.playerInfo.InGame)
                        {
                            if (bj_procedure.enterBJDeskRs.OtherComposeState[i].Val == 1)
                            {
                                seat.ShowCompareCards();
                                bj_procedure.enterBJDeskRs.OtherComposeState[i].Val = 0;
                            }
                            else
                            {
                                if (Util.IsMySelf(bj_procedure.enterBJDeskRs.OtherComposeState[i].Key))
                                {
                                    varLocalOP.ShowCountdown(remainTime);
                                }
                                else
                                {
                                    seat.ShowHandCards(false);
                                }
                            }
                        }
                    }
                }
                break;
            case CompareChickenState.CkSettle:
                if (bj_procedure.enterBJDeskRs.Settle != null && bj_procedure.enterBJDeskRs.Settle.Count > 0)
                {
                    foreach (var item in bj_procedure.enterBJDeskRs.Settle)
                    {
                        seatMgr.GetPlayerByPlayerID(item.PlayerId).PlayCardAniEverySecondNoAni(item);
                    }
                    bj_procedure.enterBJDeskRs.Settle.Clear();
                }
                break;
        }

    }

    public IEnumerator Function_DeskPlayerInfoRs(DeskPlayer deskPlayer, bool isLeave)
    {
        seatMgr.PlayerEnter(deskPlayer);
        if (Util.IsMySelf(deskPlayer.BasePlayer.PlayerId))
        {
            yield return new WaitForSeconds(0.3f);
            ShowBringUI(isLeave);
        }
        yield return null;
    }

    /// <summary>
    /// 判断是否是房主
    /// </summary>
    public bool IsRoomCreate()
    {
        return bj_procedure.enterBJDeskRs.BaseInfo.Creator.PlayerId == Util.GetMyselfInfo().PlayerId;
    }

    public void OnUserDataChanged(object sender, GameEventArgs e)
    {
        var args = e as UserDataChangedEventArgs;
        switch (args.Type)
        {
            case UserDataType.MONEY:
                goldtxt.FormatAmount(Util.GetMyselfInfo().Gold);
                break;
            case UserDataType.DIAMOND:
                diamondstxt.FormatAmount(Util.GetMyselfInfo().Diamonds);
                varGameSetting.GetComponent<GameSettingBJ>().lb_RemianMoney.text = GF.DataModel.GetDataModel<UserDataModel>().Diamonds.ToString();
                break;
            case UserDataType.VipState:
                break;
        }
    }

    public void OnClubDataChanged(object sender, GameEventArgs e)
    {
        var args = e as ClubDataChangedEventArgs;
        switch (args.Type)
        {
            case ClubDataType.eve_LeagueJackpotUpdate:
                UpdateJackpot();
                break;
        }
    }

    public void UpdateJackpot()
    {
        if (varBtnJackpot.activeSelf == false) return;
        GlobalManager.GetInstance().GetJackpotData(bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.ClubId, out float total);
        double form = varBtnJackpot.GetComponent<JumpingNumberTextComponent>().number;
        varBtnJackpot.GetComponent<JumpingNumberTextComponent>().Change(form, total);
    }

    public void OnGFEventChanged(object sender, GameEventArgs e)
    {
        var args = e as GFEventArgs;
        var t = args.UserData as Msg_DeskChat;
        switch (args.EventType)
        {
            case GFEventType.eve_SynDeskChat:
                if (t.Type == 0)//文本
                {
                    var barrageItemT = Instantiate(barrageItem, barrage);
                    float randomFloat = UnityEngine.Random.Range(-375f, 337f); // 对于浮点数，上限不需要+1
                    barrageItemT.GetComponent<BarrageItem>().InitText(t, randomFloat);
                }
                else if (t.Type == 1)//快捷
                {
                    if (Util.IsMySelf(t.Sender.PlayerId))
                    {
                        GF.UI.CloseUIForms(UIViews.ChatPanel);
                    }
                    var barrageItemT = Instantiate(barrageItem, barrage);
                    float randomFloat = UnityEngine.Random.Range(-375f, 337f); // 对于浮点数，上限不需要+1
                    barrageItemT.GetComponent<BarrageItem>().InitText(t, randomFloat);
                }
                else if (t.Type == 2)//表情
                {
                    if (Util.IsMySelf(t.Sender.PlayerId))
                    {
                        GF.UI.CloseUIForms(UIViews.ChatPanel);
                    }
                    seatMgr.Function_Biao(t.Sender.PlayerId, t.Chat);
                }
                else if (t.Type == 3)//语音
                {
                    CoroutineRunner.Instance.StartCoroutine(DownloadAndPlayMp3(t.Sender.PlayerId, t.Voice));
                }
                break;
            case GFEventType.eve_ReConnectGame:
                if (Util.GetMyselfInfo().DeskId != 0 && Util.GetMyselfInfo().DeskId == bj_procedure.deskID)
                {
                    Util.GetInstance().Send_EnterDeskRq(bj_procedure.deskID);
                }
                else
                {
                    GF.LogInfo("返回登录界面");
                    HotfixNetworkManager.Ins.isLogin = false;
                    HomeProcedure homeProcedure = GF.Procedure.GetProcedure<HomeProcedure>() as HomeProcedure;
                    homeProcedure.QuitGame();
                }
                break;
        }
    }

    IEnumerator DownloadAndPlayMp3(long playerid, string url)
    {
        string downloadUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}:{Const.ServerInfo_BackEndPort}/voice/{bj_procedure.deskID}/";
        if (Const.CurrentServerType == Const.ServerType.外网服)
        {
            downloadUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}/voice/{bj_procedure.deskID}/";
        }
        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(downloadUrl + url, AudioType.MPEG);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            GF.LogError("Error downloading audio: ", request.error);
            yield break;
        }
        Seat_bj seatInfo = seatMgr.GetPlayerByPlayerID(playerid);
        if (seatInfo != null)
        {
            seatInfo.voice.SetActive(true);
        }
        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
        SoundManager.Instance.PlaySFX(clip, 0, () =>
        {
            if (seatInfo != null)
            {
                seatInfo.voice.SetActive(false);
            }
        });
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        if (bj_procedure.enterBJDeskRs.BaseInfo.State == DeskState.Pause)
        {
            countdownCtr.Pause();
        }
    }
    protected override void OnClose(bool isShutdown, object userData)
    {
        if (m_RaiseCountdownCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(m_RaiseCountdownCoroutine);
            m_RaiseCountdownCoroutine = null;
        }
        if (m_CountdownCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(m_CountdownCoroutine);
            m_CountdownCoroutine = null;
        }
        RedDotManager.GetInstance().RemoveRedDotNodeCallBack(E_RedDotDefine.MsgBox, MsgCallBack);
        ClearTable();
        varGameSetting.SetActive(false);
        menu.SetActive(false);
        bringUI.GetComponent<BringUI>().Clear();
        base.OnClose(isShutdown, userData);

        foreach (var toggle in toggleTable)
        {
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(OnTableBGToggleChanged);
            }
        }
    }

    private readonly float openTime = 0.3f;
    private readonly float closeTime = 0.1f;
    protected override async void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        var uiParams = UIParams.Create();
        switch (btId)
        {
            case "房主开始游戏":
                bj_procedure.Send_Start();
                btnGameBegain.SetActive(false);
                break;
            case "打开左上角菜单":
                goldtxt.FormatAmount(Util.GetMyselfInfo().Gold);
                diamondstxt.FormatAmount(Util.GetMyselfInfo().Diamonds);
                if (menu.activeSelf)
                {
                    menu.transform.DOLocalMoveX(-500, closeTime).OnComplete(() =>
                    {
                        menu.SetActive(false);
                    });
                }
                else
                {
                    menu.SetActive(true);
                    menu.transform.localPosition = new Vector3(-500, menu.transform.localPosition.y, menu.transform.localPosition.z);
                    menu.transform.DOLocalMoveX(0, openTime);
                }
                break;
            case "关闭左上角菜单":
                menu.transform.DOLocalMoveX(-500, closeTime).OnComplete(() =>
                {
                    menu.SetActive(false);
                });
                break;
            case "牌桌设置":
                int index = SettingExtension.GetTableBG();
                for (var i = 0; i < toggleTable.Count; i++)
                {
                    toggleTable[i].isOn = i == index;
                }
                varGameSetting.transform.localScale = Vector3.zero;
                varGameSetting.SetActive(true);
                varGameSetting.transform.DOScale(Vector3.one, openTime);
                break;
            case "关闭设置":
                varGameSetting.transform.localScale = Vector3.one;
                varGameSetting.transform.DOScale(Vector3.zero, openTime).OnComplete(() =>
                {
                    varGameSetting.SetActive(false);
                });
                break;
            case "游戏规则":
                varRule.transform.localScale = Vector3.zero;
                varRule.SetActive(true);
                varRule.transform.DOScale(Vector3.one, openTime);
                break;
            case "关闭规则":
                varRule.transform.localScale = Vector3.one;
                varRule.transform.DOScale(Vector3.zero, closeTime).OnComplete(() =>
                {
                    varRule.SetActive(false);
                });
                break;
            case "提前结算":
                bj_procedure.Send_AdvanceSettle();
                break;
            case "带入":
                Seat_bj player = seatMgr.GetSelfSeat();
                if (player != null)
                {
                    ShowBringUI();
                }
                break;
            case "商城":
                await GF.UI.OpenUIFormAwait(UIViews.ShopPanel);
                break;
            case "回放":
                RectTransform varRealTimeBRPanel_bj_rev = varGameReview.transform.GetComponent<RectTransform>();
                varRealTimeBRPanel_bj_rev.anchoredPosition = new Vector2(varRealTimeBRPanel_bj_rev.transform.Find("Panel").GetComponent<RectTransform>().rect.width, varRealTimeBRPanel_bj_rev.anchoredPosition.y);
                varGameReview.SetActive(true);
                varRealTimeBRPanel_bj_rev.DOAnchorPosX(0, openTime);
                break;
            case "关闭回放":
                varGameReview.transform.GetComponent<RectTransform>().DOAnchorPosX(varGameReview.transform.Find("Panel").GetComponent<RectTransform>().rect.width, closeTime).OnComplete(() =>
                {
                    varGameReview.SetActive(false);
                });
                break;
            case "即时战绩":
                if (bj_procedure.enterBJDeskRs.BaseInfo.DeskType == DeskType.Match)
                {
                    uiParams.Set<VarByteArray>("matchConfig", bj_procedure.MatchInfo.ToByteArray());
                    long[] myList = seatMgr.GetInGameSeats().Select(seat => seat.playerInfo.BasePlayer.PlayerId).ToArray();
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var binaryWriter = new BinaryWriter(memoryStream))
                        {
                            foreach (long id in myList)
                            {
                                binaryWriter.Write(id);
                            }
                        }
                        uiParams.Set<VarByteArray>("playerInTableIds", memoryStream.ToArray());
                    }
                    await GF.UI.OpenUIFormAwait(UIViews.MatchContentInGamePanel, uiParams);
                }
                else
                {
                    RectTransform varRealTimeBRPanel_bj_rct = varRealTimeBRPanel_bj.transform.GetComponent<RectTransform>();
                    varRealTimeBRPanel_bj_rct.anchoredPosition = new Vector2(0 - varRealTimeBRPanel_bj.transform.Find("Panel").GetComponent<RectTransform>().rect.width, varRealTimeBRPanel_bj_rct.anchoredPosition.y);
                    varRealTimeBRPanel_bj.SetActive(true);
                    varRealTimeBRPanel_bj_rct.DOAnchorPosX(0, openTime);
                }
                break;
            case "关闭战绩":
                varRealTimeBRPanel_bj.transform.GetComponent<RectTransform>().DOAnchorPosX(0 - varRealTimeBRPanel_bj.transform.Find("Panel").GetComponent<RectTransform>().rect.width, closeTime).OnComplete(() =>
                {
                    varRealTimeBRPanel_bj.SetActive(false);
                });
                break;
            case "文字聊天":
                await GF.UI.OpenUIFormAwait(UIViews.ChatPanel);
                break;
            case "消息记录":
                await GF.UI.OpenUIFormAwait(UIViews.MessagePanel);
                break;
            case "退出牌局":
                if (bj_procedure.enterBJDeskRs.BaseInfo.DeskType == DeskType.Match
                    && seatMgr.IsInTable(Util.GetMyselfInfo().PlayerId))
                {
                    bj_procedure.EnterHome();
                }
                else
                {
                    bj_procedure.Send_LeaveDeskRq();
                }
                break;
            case "站起围观":
                bj_procedure.Send_SitUpRq();
                break;
            case "解散房间":
                Util.GetInstance().OpenConfirmationDialog("解散房间", "确定要解散房间?", () =>
                {
                    GF.LogInfo("解散房间");
                    bj_procedure.Send_DisMissDeskRq();
                });
                break;
            case "黑名单":
                GF.LogInfo("黑名单");
                await GF.UI.OpenUIFormAwait(UIViews.GameBlackPlayerPanel);
                break;
            case "取消离桌":
                bj_procedure.Send_CancelLeaveRq();
                break;
            case "暂停房间":
                bj_procedure.Send_PauseDeskRq(1);
                break;
            case "恢复房间":
                if (IsRoomCreate())
                {
                    bj_procedure.Send_PauseDeskRq(0);
                }
                break;
            case "购买时长":
                GameSettingBJ setting = varGameSetting.GetComponent<GameSettingBJ>();
                float fen = setting.SD_TimeArry[(int)setting.SD_TimeSlider.value] * 60;
                bj_procedure.Send_DeskContinuedTimeRq((int)fen);
                break;
            case "等待队列":
                varWaitSeatQueue.SetActive(true);
                break;
            case "关闭队列":
                varWaitSeatQueue.SetActive(false);
                break;
            case "加入队列":
                if (Util.GetMyselfInfo().VipState == 0)
                {
                    GF.UI.ShowToast("VIP专享功能");
                    return;
                }
                List<Seat_bj> seats = bj_procedure.GamePanel_bj.seatMgr.GetSeats();
                if (seats.Count < bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.PlayerNum)
                {
                    GF.UI.ShowToast("还有空位");
                    return;
                }
                DeskPlayer selfPlayer = seatMgr.GetSelfSeat()?.playerInfo;
                if (selfPlayer == null)
                {
                    if (varWaitSeatQueue.GetComponent<GameWaitSeat>().waitPlayers.Any(basePlayer => basePlayer.PlayerId == Util.GetMyselfInfo().PlayerId))
                    {
                        GF.UI.ShowToast("已经在等待队列");
                        return;
                    }
                    else
                    {
                        Msg_LineUpRq req = MessagePool.Instance.Fetch<Msg_LineUpRq>();
                        req.State = 1;
                        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_LineUpRq, req);
                    }
                }
                else if (selfPlayer.Pos != Position.NoSit)
                {
                    GF.UI.ShowToast("已经在座位上");
                    return;
                }
                else
                {
                    Msg_LineUpRq req = MessagePool.Instance.Fetch<Msg_LineUpRq>();
                    req.State = 1;
                    HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_LineUpRq, req);
                }
                break;
            case "离开队列":
                Msg_LineUpRq req1 = MessagePool.Instance.Fetch<Msg_LineUpRq>();
                req1.State = 0;
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_LineUpRq, req1);
                break;
            case "加时":
                // bj_procedure.Send_OptionRq(ZjhOption.AddTime);
                break;
            case "Jackpot":
                uiParams.Set<VarInt32>("methodType", (int)MethodType.CompareChicken);
                uiParams.Set<VarByteArray>("deskConfig", bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.ToByteArray());
                await GF.UI.OpenUIFormAwait(UIViews.GameJackpotPanel, uiParams);
                break;
            case "wpk":
                uiParams.Set<VarByteArray>("deskConfig", bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.ToByteArray());
                await GF.UI.OpenUIFormAwait(UIViews.GameWPKPanel, uiParams);
                break;
            case "btn_Check":
                break;
        }
        ;
    }

    private void OnTableBGToggleChanged(bool isOn)
    {
        if (isOn)
        {
            for (int i = 0; i < toggleTable.Count; i++)
            {
                if (toggleTable[i].isOn)
                {
                    SettingExtension.SetTableBG(i);
                    Util.UpdateTableColor(transform.GetComponent<Image>());
                    break;
                }
            }
        }
    }
    
    public void ShowBringUI(bool isLeave = false)
    {
        if (bringUI.activeSelf || bj_procedure.enterBJDeskRs.BaseInfo.DeskType == DeskType.Match) return;
        bringUI.GetComponent<BringUI>().ShowBring(isLeave);
    }


    /// <summary>
    /// 收到发牌消息
    /// </summary>
    public void Syn_DealCard(List<DealCard> Deals)
    {
        ResetTable();
        foreach (var item in Deals)
        {
            seatMgr.Syn_DealCard(item);
        }
        if (btnGameBegain.activeSelf) btnGameBegain.SetActive(false);
        AsynDearAnim(Deals);
    }

    // 启动动画协程
    public void AsynDearAnim(List<DealCard> Deals)
    {
        foreach (var item in Deals)
        {
            Seat_bj player = seatMgr.GetPlayerByPlayerID(item.PlayerId);
            if (player != null)
            {
                bool isSelf = Util.IsMySelf(player.playerInfo.BasePlayer.PlayerId);
                if (isSelf)
                {
                    player.handCardDatas = item.HandCards.ToArray();
                    varLocalOP.DealCardAni();
                }
                else
                {
                    player.ShowHandCards();
                }
            }
        }
    }

    //显示开始比牌特效音效
    public void ShowSettleAni()
    {
        HideCountdown();
        Sequence seq = DOTween.Sequence();
        seq.SetTarget(gameObject);
        varBiPaiEffect.SetActive(true);
       Sound.PlayEffect("BJ/开始比牌.mp3");
       Sound.PlayEffect("BJ/电击音效.mp3");
        seq.AppendInterval(2.5f);
        seq.AppendCallback(() =>
        {
            varBiPaiEffect.SetActive(false);
        });
    }

    /// <summary>
    /// 结算动画
    /// </summary>
    public void Function_SettleAni(CompareChickenSettle[] settles, long firstOpen)
    {
        //总结下 三种模式比牌 服务器分别需要等待多长时间开始下一把
        varLocalOP.ClearLocalOP();
        float time1 = 0.8f;
        //1:完整比牌 2:每道比牌 3:跳过比牌
        int SettleAnimation = bj_procedure.enterBJDeskRs.BaseInfo.CkConfig.Animation;
        switch (SettleAnimation)
        {
            case 1:
                // 完整比牌 - 先从firstOpen开始依次亮头道 然后循环亮中道 亮尾道 最后一起亮结算喜钱输赢

                // 获取所有参与比牌没有弃牌的座位
                var allSeats = settles.Select(s => s.IsGive ? null : seatMgr.GetPlayerByPlayerID(s.PlayerId))
                                      .Where(seat => seat != null)
                                      .ToList();

                Sequence fullCompareSequence = DOTween.Sequence();
                fullCompareSequence.SetTarget(gameObject);

                fullCompareSequence.AppendCallback(() =>
                {
                    ShowSettleAni();
                    foreach (var item in allSeats)
                    {
                        item.ShowCompareCards();
                    }
                });
                fullCompareSequence.AppendInterval(3f);

                //如果有弃牌的跳过 展示弃牌就行
                var giveSeats = settles.Where(s => s.IsGive).ToList();
                if (giveSeats.Count > 0)
                {
                    foreach (var settle in giveSeats)
                    {
                        var seat = seatMgr.GetPlayerByPlayerID(settle.PlayerId);
                        if (seat != null)
                        {
                            seat.PlayCompareCards(settle, 2);
                        }
                    }
                }


                // 找到firstOpen玩家的座位
                var firstSeat = allSeats.FirstOrDefault(seat => seat.playerInfo.BasePlayer.PlayerId == firstOpen);

                // 重新排序座位：firstOpen玩家优先，其他按座位号顺序排列
                var playerSeats = new List<Seat_bj>();
                if (firstSeat != null)
                {
                    playerSeats.Add(firstSeat);
                    playerSeats.AddRange(allSeats.Where(s => s != firstSeat).OrderBy(seat => seat.SeatId));
                }
                else
                {
                    // 如果找不到firstOpen玩家，则按座位号排序
                    playerSeats = allSeats.OrderBy(seat => seat.SeatId).ToList();
                }

                // 转换为数组
                var orderedSeats = playerSeats.ToArray();

                // 先亮头道  playerSeats.count * 0.8
                for (int i = 0; i < orderedSeats.Length; i++)
                {
                    int index = i;
                    fullCompareSequence.AppendCallback(() =>
                    {
                        var seat = orderedSeats[index];
                        seat.ShowCompareCards();
                        // 只显示第一道牌
                        var settle = settles.FirstOrDefault(s => s.PlayerId == seat.playerInfo.BasePlayer.PlayerId);
                        if (settle != null && settle.ChickenCards.Count > 0)
                        {
                            seat.compareCards[0].PlayAni(settle.ChickenCards[0], SettleAnimation);
                        }
                    });
                    fullCompareSequence.AppendInterval(time1);
                }

                // 亮中道
                for (int i = 0; i < orderedSeats.Length; i++)
                {
                    int index = i;
                    fullCompareSequence.AppendCallback(() =>
                    {
                        var seat = orderedSeats[index];
                        var settle = settles.FirstOrDefault(s => s.PlayerId == seat.playerInfo.BasePlayer.PlayerId);
                        if (settle != null && settle.ChickenCards.Count > 1)
                        {
                            seat.compareCards[1].PlayAni(settle.ChickenCards[1], SettleAnimation);
                        }
                    });
                    fullCompareSequence.AppendInterval(time1);
                }

                // 亮尾道
                for (int i = 0; i < orderedSeats.Length; i++)
                {
                    int index = i;
                    fullCompareSequence.AppendCallback(() =>
                    {
                        var seat = orderedSeats[index];
                        var settle = settles.FirstOrDefault(s => s.PlayerId == seat.playerInfo.BasePlayer.PlayerId);
                        if (settle != null && settle.ChickenCards.Count > 2)
                        {
                            seat.compareCards[2].PlayAni(settle.ChickenCards[2], SettleAnimation);
                        }
                    });
                    fullCompareSequence.AppendInterval(time1);
                }

                // 最后一起亮结算喜钱输赢
                fullCompareSequence.AppendInterval(2f);
                fullCompareSequence.AppendCallback(() =>
                {
                    foreach (var item in settles)
                    {
                        var seat = seatMgr.GetPlayerByPlayerID(item.PlayerId);
                        if (seat != null)
                        {
                            seat.ShowLuckCoin(item);
                        }
                    }
                    ShowSettleCoinAni(settles);
                });
                // float sumTime = playerSeats.Length * 0.8f * 3 + 2;
                fullCompareSequence.AppendInterval(3f);
                fullCompareSequence.AppendCallback(() =>
                {
                    Function_SynCKJackPot();
                    // ResetTable();
                });

                fullCompareSequence.Play();
                break;
            case 2:
                Sequence sequence = DOTween.Sequence();
                sequence.SetTarget(gameObject);
                sequence.AppendCallback(() =>
                {
                    ShowSettleAni();
                });
                sequence.AppendInterval(3f);
                // 为每个结算项添加动画
                foreach (var item in settles)
                {
                    sequence.AppendCallback(() =>
                    {
                        seatMgr.GetPlayerByPlayerID(item.PlayerId).PlayCompareCards(item, SettleAnimation);
                    });
                }
                sequence.AppendInterval(5f);
                sequence.AppendCallback(() =>
                {
                    ShowSettleCoinAni(settles);
                });
                sequence.AppendInterval(3f);
                sequence.AppendCallback(() =>
                {
                    Function_SynCKJackPot();
                    // ResetTable();
                });
                // float sumTime = 4.5f + 1;
                sequence.Play();
                break;
            case 3:
                // 跳过比牌 - 所有人的三道牌一起摊开，直接显示结算
                Sequence skipSequence = DOTween.Sequence();
                skipSequence.SetTarget(gameObject);
                skipSequence.AppendCallback(() =>
                {
                    ShowSettleAni();
                });
                skipSequence.AppendInterval(3f);
                // 为每个结算项添加动画
                foreach (var item in settles)
                {
                    var currentItem = item; // 防止闭包问题
                    skipSequence.AppendCallback(() =>
                    {
                        seatMgr.GetPlayerByPlayerID(currentItem.PlayerId).PlayCompareCards(currentItem, SettleAnimation);
                    });
                    skipSequence.AppendInterval(0.05f); // 每个动画间隔0.2秒，可根据实际调整
                }
                skipSequence.AppendInterval(3f - settles.Length * 0.05f);
                skipSequence.AppendCallback(() =>
                {
                    ShowSettleCoinAni(settles);
                });
                skipSequence.AppendInterval(2f);
                skipSequence.AppendCallback(() =>
                {
                    Function_SynCKJackPot();
                    // ResetTable();
                });
                // float sumTime = 4.5f + 1;
                skipSequence.Play();
                break;
        }

    }

    /// <summary>
    /// 从输家座位生成varCoinAni 汇聚到varCoinCenterAni 显示varCoinCenterAni 然后在生成varCoinAni 移动到赢家
    /// </summary>
    /// <param name="settles"></param>
    public void ShowSettleCoinAni(CompareChickenSettle[] settles)
    {
        ClearCoinObjs();

        #region 收集赢家和输家
        List<Seat_bj> loserSeats = new List<Seat_bj>();
        List<Seat_bj> winnerSeats = new List<Seat_bj>();
        foreach (var settle in settles)
        {
            var seat = seatMgr.GetPlayerByPlayerID(settle.PlayerId);
            if (seat == null) continue;
            float realWin = 0;
            float.TryParse(settle.RealWin, out realWin);
            if (realWin < 0)
                loserSeats.Add(seat);
            else if (realWin > 0)
                winnerSeats.Add(seat);
        }
        #endregion
        if (loserSeats.Count == 0 && winnerSeats.Count == 0)
        {
            return;
        }

        int coinCountPerSeat = 6;
        float delayStep = 0.08f;
        Vector3 centerPos = varCoinCenterAni.transform.position;
        Sequence seq = DOTween.Sequence();
        seq.SetTarget(gameObject);

        // 1. 正序播放中央动画
        seq.AppendCallback(() =>
        {
            PlayCoinCenterAni(true, true);
        });

        // 2. 输家金币飞向中央
        float maxLoserDelay = 0f;
        foreach (var seat in loserSeats)
        {
            for (int i = 0; i < coinCountPerSeat; i++)
            {
                int index = i;
                GameObject coin = GF.StaticUI.GetCoinFromPool();
                coin.transform.SetParent(varCoinPool.transform);
                coin.SetActive(true);
                coin.transform.position = seat.transform.position;
                m_CoinObjs.Add(coin);
                float delay = i * delayStep;
                seq.Insert(delay, coin.transform.DOMove(centerPos, 0.5f)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        if (index == 0)
                        {
                            var settleForSeat = settles.FirstOrDefault(s => s.PlayerId == seat.playerInfo.BasePlayer.PlayerId);
                            if (settleForSeat != null)
                            {
                                float.TryParse(settleForSeat.RealWin, out float realWin);
                                float.TryParse(settleForSeat.CoinChange, out float coinChange);
                                seat.ShowSettleCoinChange(coinChange, realWin);
                            }
                        }
                        coin.SetActive(false);
                    }));
                if (delay > maxLoserDelay) maxLoserDelay = delay;
            }
        }

        // 3. 停顿0.5秒
        seq.AppendInterval(0.5f + 0.5f); // 0.5f为金币飞行时间

        // 4. 倒序播放中央动画
        seq.AppendCallback(() =>
        {
            PlayCoinCenterAni(true, false);
        });

        // 5. 金币从中央飞向赢家
        float maxWinnerDelay = 0f;
        foreach (var seat in winnerSeats)
        {
            for (int i = 0; i < coinCountPerSeat; i++)
            {
                int index = i;
                GameObject coin = GF.StaticUI.GetCoinFromPool();
                coin.transform.SetParent(varCoinPool.transform);
                coin.SetActive(true);
                coin.transform.position = centerPos;
                m_CoinObjs.Add(coin);
                float delay = i * delayStep;
                seq.Insert(maxLoserDelay + 2.0f + delay, coin.transform.DOMove(seat.transform.position, 0.5f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        if (index == 0)
                        {
                            var settleForSeat = settles.FirstOrDefault(s => s.PlayerId == seat.playerInfo.BasePlayer.PlayerId);
                            if (settleForSeat != null)
                            {
                                float.TryParse(settleForSeat.RealWin, out float realWin);
                                float.TryParse(settleForSeat.CoinChange, out float coinChange);
                                seat.ShowSettleCoinChange(coinChange, realWin);
                            }
                        }
                        coin.SetActive(false);
                    }));
                if (delay > maxWinnerDelay) maxWinnerDelay = delay;
            }
        }

        // 6. 动画结束后清理
        seq.AppendInterval(maxWinnerDelay + 0.5f + 0.5f); // 0.5f为金币飞行时间
        seq.AppendCallback(() =>
        {
            PlayCoinCenterAni(false);
            ClearCoinObjs();
        });

        seq.Play();
    }

    public void PlayCoinCenterAni(bool isPlay, bool order = true)
    {
        if (isPlay)
        {
            varCoinCenterAni.gameObject.SetActive(true);
            varCoinCenterAni.Framerate = order ? 20f : -30f;
            varCoinCenterAni.Loop = false;
            varCoinCenterAni.Reset();
            varCoinCenterAni.Play();
            varCoinCenterAni.FinishEvent -= OnCoinAniFinish; // 防止重复注册
            if (order == false)
            {
                varCoinCenterAni.FinishEvent += OnCoinAniFinish;
            }
           Sound.PlayEffect(order ? "BJ/金币汇聚.mp3" : "chipmove.mp3");
        }
        else
        {
            varCoinCenterAni.Stop();
            varCoinCenterAni.gameObject.SetActive(false);
        }
    }

    void OnCoinAniFinish(GameObject go)
    {
        go.SetActive(false);
    }

    public void AutoCompareCardRs(ChickenCards[] cards)
    {
        varLocalOP.AutoCompareCardRs(cards);
    }

    public void SwitchChickenState()
    {
        ShowCountdown();
    }

    public void CompareCardRs()
    {
        varLocalOP.ClearLocalOP();
    }

    public void SynCompareCard(long playerId)
    {
        if (Util.IsMySelf(playerId))
        {
            varLocalOP.ClearLocalOP();
        }
        Seat_bj seat_Bj = seatMgr.GetPlayerByPlayerID(playerId);
        if (seat_Bj != null)
        {
            seat_Bj.ShowCompareCards();
        }
    }

    public void CompareCardGiveRs(Msg_CompareCardGiveRs ack)
    {
        varLocalOP.ClearLocalOP();
        Seat_bj seat_Bj = seatMgr.GetSelfSeat();
        if (seat_Bj != null)
        {
            seat_Bj.ShowCompareCards();
        }
    }

    public void SynPlayerGiveCard(long playerId)
    {
        seatMgr.GetPlayerByPlayerID(playerId).ShowCompareCards();
    }

    public void SynMatchLose(Msg_SynMatchLose ack)
    {
        if (ack.Match.MatchId == bj_procedure.MatchInfo.MatchId)
        {
            GF.UI.LoadPrefab(AssetsPath.GetPrefab("UI/Tips/MatchSettleTips"), (gameObject) =>
            {
                if (gameObject == null) return;
                GameObject matchSettle = Instantiate(gameObject as GameObject, transform.Find("Panels"));
                if (matchSettle == null)
                {
                    return;
                }
                MatchSettleTips tips = matchSettle.GetComponent<MatchSettleTips>();
                tips.ShowLose(ack, () =>
                {
                    if (bj_procedure == null) return;
                    bj_procedure.Send_LeaveDeskRq();
                });
            });
        }
    }

    public void GetSingleMatchRs(Msg_Match matchs)
    {
        // 获取涨底表和当前底注
        var addCoinTable = GlobalManager.GetInstance().m_AddCoinTable;
        int currentBaseCoinLevel = matchs.BaseCoinLevel;
        // 查找下一级底注
        int nextBaseCoin = addCoinTable[currentBaseCoinLevel].SmallBlind;
        long nextRaiseTime = 0;

        if (addCoinTable != null && addCoinTable.Count > 0)
        {
            // 找到下一个涨底等级 - 直接使用currentBaseCoinLevel + 1
            long matchStartTime = matchs.StartTime;
            long currentServerTime = Util.GetServerTime();

            // 查找下一级底注
            var nextLevelQuery = addCoinTable.Where(item => item.Level == currentBaseCoinLevel + 1).ToList();
            if (nextLevelQuery.Count > 0)
            {
                var nextLevel = nextLevelQuery[0];
                nextBaseCoin = nextLevel.SmallBlind;
                // 计算下一次涨底时间点 = 开始时间 + currentBaseCoinLevel * 涨底间隔
                long raiseTimePoint = matchStartTime + (currentBaseCoinLevel * nextLevel.AddTime * 1000);

                // 确保涨底时间点在当前时间之后
                if (raiseTimePoint > currentServerTime)
                {
                    nextRaiseTime = raiseTimePoint;
                }
                else
                {
                    // 如果涨底时间已过，尝试查找下一级
                    var nextNextLevelQuery = addCoinTable.Where(item => item.Level > currentBaseCoinLevel + 1).OrderBy(item => item.Level).ToList();
                    if (nextNextLevelQuery.Count > 0)
                    {
                        var nextNextLevel = nextNextLevelQuery[0];
                        nextBaseCoin = nextNextLevel.SmallBlind;
                        nextRaiseTime = matchStartTime + ((nextNextLevel.Level - 1) * nextNextLevel.AddTime * 1000);
                    }
                }
            }
        }

        // 更新比赛信息显示
        varMatchRoomInfo.text = "下一级 " + nextBaseCoin +
                               "\n奖励人数: " + matchs.RewardNum +
                               "\n平均积分: " + matchs.AvgCoin +
                               "\n剩余人数: " + matchs.AliveNum;

        // 更新涨底时间显示
        if (nextRaiseTime > 0)
        {
            long m_ServerTime = Util.GetServerTime();
            long m_RemainTime = nextRaiseTime - m_ServerTime;

            if (m_RemainTime > 0)
            {
                m_RemainTime /= 1000; // 转换为秒
                int m_Minutes = (int)(m_RemainTime / 60);
                int m_Seconds = (int)(m_RemainTime % 60);
                varMatchRoomTime.text = string.Format("涨底时间 {0:D2}:{1:D2}", m_Minutes, m_Seconds);

                // 开启涨底倒计时协程
                if (m_RaiseCountdownCoroutine != null)
                {
                    CoroutineRunner.Instance.StopCoroutine(m_RaiseCountdownCoroutine);
                }
                m_RaiseCountdownCoroutine = CoroutineRunner.Instance.StartCoroutine(UpdateRaiseCountdown(nextRaiseTime, nextBaseCoin));
            }
            else
            {
                varMatchRoomTime.text = "涨底时间 00:00";
            }
        }
        else
        {
            varMatchRoomTime.text = "涨底时间 --:--";
        }

        // 比赛倒计时
        if (matchs.EndTime > 0)
        {
            varMatchTimeText.transform.parent.gameObject.SetActive(bj_procedure.enterBJDeskRs.BaseInfo.DeskType == DeskType.Match);
            varMatchTimeText.text = "00:00";
            long m_ServerTime = Util.GetServerTime();
            long m_RemainTime = matchs.EndTime - m_ServerTime;

            if (m_RemainTime > 0)
            {
                m_RemainTime /= 1000; // 转换为秒
                int m_Minutes = (int)(m_RemainTime / 60);
                int m_Seconds = (int)(m_RemainTime % 60);
                varMatchTimeText.text = string.Format("{0:D2}:{1:D2}", m_Minutes, m_Seconds);

                // 开启倒计时协程
                if (m_CountdownCoroutine != null)
                {
                    CoroutineRunner.Instance.StopCoroutine(m_CountdownCoroutine);
                }
                m_CountdownCoroutine = CoroutineRunner.Instance.StartCoroutine(UpdateMatchCountdown(matchs.EndTime));
            }
            else
            {
                varMatchTimeText.text = "00:00";
            }
        }
        else
        {
            varMatchTimeText.transform.parent.gameObject.SetActive(false);
        }
    }

    #region 比赛倒计时
    private Coroutine m_CountdownCoroutine;

    /// <summary>
    /// 更新比赛倒计时
    /// </summary>
    /// <param name="_endTime">比赛结束时间戳（毫秒）</param>
    private IEnumerator UpdateMatchCountdown(long _endTime)
    {
        while (true)
        {
            long m_ServerTime = Util.GetServerTime();
            long m_RemainTime = _endTime - m_ServerTime;

            if (m_RemainTime <= 0)
            {
                varMatchTimeText.text = "00:00";
                break;
            }

            m_RemainTime /= 1000; // 转换为秒
            int m_Minutes = (int)(m_RemainTime / 60);
            int m_Seconds = (int)(m_RemainTime % 60);
            varMatchTimeText.text = string.Format("{0:D2}:{1:D2}", m_Minutes, m_Seconds);

            yield return new WaitForSeconds(1.0f);
        }
    }
    #endregion

    #region 涨底倒计时
    private Coroutine m_RaiseCountdownCoroutine;

    /// <summary>
    /// 更新涨底倒计时
    /// </summary>
    /// <param name="_raiseTime">涨底时间戳（毫秒）</param>
    /// <param name="_nextBaseCoin">下一级底注</param>
    private IEnumerator UpdateRaiseCountdown(long _raiseTime, long _nextBaseCoin)
    {
        while (true)
        {
            long m_ServerTime = Util.GetServerTime();
            long m_RemainTime = _raiseTime - m_ServerTime;

            if (m_RemainTime <= 0)
            {
                varMatchRoomTime.text = "涨底时间 00:00";

                // 自动进入下一阶段底注
                if (bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.MatchId > 0)
                {
                    // 更新当前底注级别
                    bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.BaseCoin = _nextBaseCoin;

                    // 更新房间信息显示
                    varRoomInfo.text = "底注: " + bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.BaseCoin +
                                      "  王牌: " + (bj_procedure.enterBJDeskRs.BaseInfo.CkConfig.King == 0 ? "无王牌" : bj_procedure.enterBJDeskRs.BaseInfo.CkConfig.King + "张") +
                                      "\n摆牌时间: " + bj_procedure.enterBJDeskRs.BaseInfo.CkConfig.OptionTime +
                                      (bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.IpLimit && bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.GpsLimit ? "\nIP/GPS限制" :
                                      (bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.IpLimit ? "\nIP限制" : "") +
                                      (bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.GpsLimit ? "\nGPS限制" : ""));

                    // 查找下一级底注
                    var addCoinTable = GlobalManager.GetInstance().m_AddCoinTable;
                    var matchs = bj_procedure.MatchInfo;

                    if (matchs != null && addCoinTable != null && addCoinTable.Count > 0)
                    {
                        // 更新底注级别
                        int newBaseCoinLevel = matchs.BaseCoinLevel + 1;
                        matchs.BaseCoinLevel = newBaseCoinLevel;

                        // 查找下一级底注
                        var nextLevelQuery = addCoinTable.Where(item => item.Level == newBaseCoinLevel + 1).ToList();
                        if (nextLevelQuery.Count > 0)
                        {
                            var nextLevel = nextLevelQuery[0];
                            int nextBaseCoin = nextLevel.SmallBlind;

                            // 计算下一次涨底时间点
                            long matchStartTime = matchs.StartTime;
                            long nextRaiseTime = matchStartTime + (newBaseCoinLevel * nextLevel.AddTime * 1000);

                            // 更新UI显示
                            varMatchRoomInfo.text = "下一级 " + nextBaseCoin +
                                                  "\n奖励人数: " + matchs.RewardNum +
                                                  "\n平均积分: " + matchs.AvgCoin +
                                                  "\n剩余人数: " + matchs.AliveNum;

                            // 启动新的涨底倒计时
                            if (nextRaiseTime > m_ServerTime)
                            {
                                m_RaiseCountdownCoroutine = CoroutineRunner.Instance.StartCoroutine(UpdateRaiseCountdown(nextRaiseTime, nextBaseCoin));
                            }
                        }
                        else
                        {
                            // 没有下一级底注，显示为--
                            varMatchRoomInfo.text = varMatchRoomInfo.text.Replace("下一级 " + _nextBaseCoin, "下一级 --");
                        }
                    }
                    else
                    {
                        // 如果没有比赛信息或涨底表，显示为--
                        varMatchRoomInfo.text = varMatchRoomInfo.text.Replace("下一级 " + _nextBaseCoin, "下一级 --");
                    }

                    // 通知房间内所有玩家底注已更新
                    //Sound.PlayEffect("BJ/涨底提示.mp3");

                    // 显示涨底提示
                    // GF.UI.ShowToast("底注已上涨至: " + _nextBaseCoin);
                }
                else
                {
                    // 非比赛模式，只更新UI显示
                    varMatchRoomInfo.text = varMatchRoomInfo.text.Replace("下一级 " + _nextBaseCoin, "下一级 --");
                }

                break;
            }

            m_RemainTime /= 1000; // 转换为秒
            int m_Minutes = (int)(m_RemainTime / 60);
            int m_Seconds = (int)(m_RemainTime % 60);
            varMatchRoomTime.text = string.Format("涨底时间 {0:D2}:{1:D2}", m_Minutes, m_Seconds);

            yield return new WaitForSeconds(1.0f);
        }
    }
    #endregion

    public void Function_SynCKJackPot()
    {
        if (bj_procedure.msg_SynCKJackPot == null) return;
        Msg_SynCKJackPot ack = bj_procedure.msg_SynCKJackPot;
        bj_procedure.msg_SynCKJackPot = null;
        //根据数据 显示获得奖池动画
        int index = 0;
        foreach (var item in ack.Jackpots)
        {
            int currentIndex = index;
            long playerId = item.Key;
            if (!float.TryParse(item.Val, out float jackPotValue))
            {
                GF.LogError("Function_SynCKJackPot 转换失败: ", item.Val);
                continue;
            }
            string jackPot = ((int)Math.Floor(jackPotValue)).ToString();
            Seat_bj seat_Bj = seatMgr.GetPlayerByPlayerID(playerId);
            if (seat_Bj == null) continue;

            // 创建序列，延迟处理多个奖池
            Sequence jackpotSequence = DOTween.Sequence();
            jackpotSequence.SetTarget(gameObject);

            // 根据索引设置延迟
            jackpotSequence.AppendInterval(currentIndex * 0.2f);

            // 添加动画回调
            jackpotSequence.AppendCallback(() =>
            {
                //真正克隆爆炸动画和文本
                GameObject boomAni = Instantiate(varJackBoomAni, varCoinPool.transform);
                GameObject boomText = boomAni.transform.Find("JackBoomText").gameObject;

                //设置初始位置和状态
                boomAni.transform.localScale = Vector3.one * 50;
                boomAni.transform.position = varBtnJackpot.transform.position;
                var spineAni = boomAni.GetComponent<Spine.Unity.SkeletonAnimation>();
                //播放默认循环动画
                spineAni.AnimationState.SetAnimation(0, "idle2", true);
                boomAni.SetActive(true);
                boomText.SetActive(false);

                //移动到玩家位置，同时放大到100倍
                Sequence moveSequence = DOTween.Sequence();
                moveSequence.SetTarget(boomAni);

                //同时执行移动和缩放
                moveSequence.Append(boomAni.transform.DOMove(seat_Bj.transform.position, 1.0f).SetEase(Ease.OutQuad));
                moveSequence.Insert(0, boomAni.transform.DOScale(Vector3.one * 100, 1.0f).SetEase(Ease.OutQuad));

                moveSequence.OnComplete(() =>
                {
                    //播放爆炸动画
                    spineAni.AnimationState.SetAnimation(0, "idle", false);

                    // 在爆炸动画开始的同时显示文本并播放跳动动画
                    boomText.SetActive(true);
                    boomText.GetComponent<Text>().text = jackPot;
                    boomText.transform.position = boomAni.transform.position;

                    // 播放声音效果
                   Sound.PlayEffect("ANIzhuanpan2Audio.mp3");

                    // 添加跳动动画
                    Sequence bounceSequence = DOTween.Sequence();
                    bounceSequence.SetTarget(boomText);

                    // 跳动效果 - 创建3次连续的上下弹跳
                    for (int i = 0; i < 3; i++)
                    {
                        // 向上弹跳
                        bounceSequence.Append(boomText.transform.DOLocalMoveY(boomText.transform.localPosition.y + 0.5f, 0.2f)
                            .SetEase(Ease.OutQuad));
                        // 向下落回
                        bounceSequence.Append(boomText.transform.DOLocalMoveY(boomText.transform.localPosition.y, 0.2f)
                            .SetEase(Ease.InQuad));
                    }

                    // 在跳动动画完成后延迟销毁对象
                    bounceSequence.OnComplete(() =>
                    {
                        DOTween.Sequence()
                            .AppendInterval(2.0f)
                            .AppendCallback(() =>
                            {
                                if (boomText != null) Destroy(boomText);
                                if (boomAni != null) Destroy(boomAni);
                            });
                    });

                    bounceSequence.Play();
                });

                moveSequence.Play();
            });

            jackpotSequence.Play();
            index++;
        }
    }

    /// <summary>
    /// 取消留座离桌
    /// </summary>
    public void ShowBtnCancelLiuZuo(bool isShow)
    {
        if (isShow == false)
        {
            varBtncancelliuzuo_Match.SetActive(false);
            varBtncancelliuzuo.SetActive(false);
            return;
        }
        if (bj_procedure.enterBJDeskRs.BaseInfo.DeskType == DeskType.Match)
        {
            varBtncancelliuzuo_Match.SetActive(true);
        }
        else
        {
            varBtncancelliuzuo.SetActive(true);
        }
    }

    // 操作倒计时
    public void ShowCountdown()
    {
        // 检查游戏状态是否为等待玩家操作
        if (bj_procedure.enterBJDeskRs.BaseInfo.State == DeskState.WaitStart) return;

        int defaultTime = 30;
        switch (bj_procedure.enterBJDeskRs.CkState)
        {
            case CompareChickenState.CkWait:
                defaultTime = 30;
                ResetTable();
                break;
            case CompareChickenState.CkDealCard:
                defaultTime = 10;
                break;
            case CompareChickenState.CkGroupCard:
                defaultTime = bj_procedure.enterBJDeskRs.BaseInfo.CkConfig.OptionTime;
                long serverTime = Util.GetServerTime();
                long nextTime = bj_procedure.enterBJDeskRs.NextTime;
                int remainTime = nextTime > serverTime ? (int)((nextTime - serverTime) / 1000) : 0;

                if (remainTime > 0)
                {
                    countdownCtr.gameObject.SetActive(true);
                    countdownCtr.ResetAndStartCountDown(remainTime > defaultTime ? remainTime : defaultTime, remainTime, true);
                }
                else
                {
                    countdownCtr.gameObject.SetActive(false);
                }
                Seat_bj selfPlayer = seatMgr.GetSelfSeat();
                if (selfPlayer != null)
                {
                    if (selfPlayer.playerInfo.InGame)
                    {
                        varLocalOP.ShowCountdown(remainTime);
                    }
                }
                break;
            case CompareChickenState.CkSettle:
                defaultTime = 10;
                break;
        }
    }

    public void HideCountdown()
    {
        countdownCtr.Pause();
        countdownCtr.gameObject.SetActive(false);
    }

    public void SetDeskState(DeskState deskState)
    {
        bj_procedure.enterBJDeskRs.BaseInfo.State = deskState;
        ui_game_pause.gameObject.SetActive(deskState == DeskState.Pause);
    }

    public void ClearCoinObjs()
    {
        // 回收所有未回收的金币对象
        if (m_CoinObjs != null)
        {
            foreach (var coin in m_CoinObjs)
            {
                if (coin != null)
                {
                    GF.StaticUI.ReturnCoinToPool(coin);
                }
            }
            m_CoinObjs.Clear();
        }
    }

    public void ResetTable()
    {
        GF.StaticUI.ShowMatching(false);
        varMatchTimeText.transform.parent.gameObject.SetActive(bj_procedure.enterBJDeskRs.BaseInfo.DeskType == DeskType.Match);
        ClearCoinObjs();
        varBiPaiEffect.SetActive(false);
        PlayCoinCenterAni(false);
        varLocalOP.ResetLocalOP();
        seatMgr.ResetSeatsPlayer();
        HideCountdown();
    }

    public void ClearTable()
    {
        ClearCoinObjs();
        varMatchTimeText.transform.parent.gameObject.SetActive(bj_procedure.enterBJDeskRs.BaseInfo.DeskType == DeskType.Match);
        DOTween.Kill(gameObject);
        varLocalOP.ClearLocalOP();
        seatMgr.ClearSeatsPlayer();
        varBiPaiEffect.SetActive(false);
        PlayCoinCenterAni(false);
        varBtnJackpot.SetActive(false);
        ShowBtnCancelLiuZuo(false);
        HideCountdown();
    }

    void MsgCallBack(RedDotNode node)
    {
        if (gameObject == null) return;
        varRedDot.SetDotState(node.rdCount > 0, node.rdCount);
    }

    public void OnVoicePress()
    {
        if (bj_procedure.enterBJDeskRs.BaseInfo.BaseConfig.ForbidVoice)
        {
            GF.LogInfo("禁止语音");
            GF.UI.ShowToast("禁止聊天!");
            return;
        }
        Seat_bj seat = seatMgr.GetSelfSeat();
        if (seat == null || seat.IsNotEmpty() == false)
        {
            return;
        }
        if (seat.playerInfo.LeaveTime > 0 || float.Parse(seat.playerInfo.Coin) <= 0)
        {
            return;
        }
        varVoice.StartRecord(Util.GetMyselfInfo().PlayerId, bj_procedure.deskID);
    }

    public void OnVoiceRelease()
    {
        varVoice.StopRecord();
    }

    public void OnVoiceCancel()
    {
        varVoice.CancelRecord();
    }

    public Image changeImg;

    public void onDragInVisual()
    {
        changeImg.SetSprite("NN/voice_recording_1.png");
    }

    public void onDragOutVisual()
    {
        changeImg.SetSprite("NN/voice_recording_2.png");
    }
    public override void OnClickClose()
    {
        if (Util.IsClickLocked()) return;
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        Util.GetInstance().OpenConfirmationDialog("退出房间", "确定要退出房间吗?", () =>
        {
            bj_procedure.Send_LeaveDeskRq();
        });
    }

}

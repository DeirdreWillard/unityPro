using DG.Tweening;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Event;
using static UtilityBuiltin;
using UnityGameFramework.Runtime;
using System.Collections;
using System.Collections.Generic;
using static GameConstants;
using System.Linq;
using Google.Protobuf;
using UnityEngine.Networking;
using System;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class GamePanel_zjh : UIFormBase
{
    public GameObject btnGameBegain;
    ZJHProcedure zjh_procedure;

    // 比赛相关变量
    private Coroutine m_CountdownCoroutine;
    private Coroutine m_RaiseCountdownCoroutine;

    public SeatManager_zjh seatMgr;
    public ChipThrower chipManager;
    // UI节点
    public GameObject menu;
    public Text goldtxt;
    public Text diamondstxt;
    public GameObject bringUI;

    public Transform fapaiAniObjChild;
    public Transform poolChipsObj;
    public Transform roomInfo;

    public Image typeImg;
    public GameObject zhuang;

    //倒计时
    public CountDownFxControl countdownCtr;

    //弹幕
    public Transform barrage;
    public Transform barrageItem;

    //暂停
    public Transform ui_game_pause;

    //本人
    public GameObject localCard;
    //操作
    public GameObject btn_LookCard;
    public GameObject btn_Fold;
    public GameObject btn_AutoFold;
    public GameObject btn_Call;
    public GameObject btn_AutoCall;
    public GameObject btn_DoubleChip;
    public GameObject btn_CanCard;
    public GameObject btn_Gamble;
    public List<Toggle> toggleTable;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        #region 
        seatMgr = transform.Find("Seats").GetComponent<SeatManager_zjh>();
        countdownCtr = transform.Find("go_CountDown").GetComponent<CountDownFxControl>();
        chipManager = transform.Find("go_chipManager").GetComponent<ChipThrower>();
        btnGameBegain = transform.Find("LBBase/RoomInfo/BtnGameBegain").gameObject;
        menu = transform.Find("Panels/btnOpenMenu/Menu").gameObject;
        goldtxt = menu.transform.Find("Menuc/GamegoldButton/gold").GetComponent<Text>();
        diamondstxt = menu.transform.Find("Menuc/GamediamondsButton/diamonds").GetComponent<Text>();
        bringUI = transform.Find("Panels/BringUI").gameObject;
        fapaiAniObjChild = transform.Find("FapaiAni");
        poolChipsObj = transform.Find("PoolChipsObj");
        roomInfo = transform.Find("LBBase/RoomInfo");
        typeImg = transform.Find("OpreateButtons/TypeImage").GetComponent<Image>();
        zhuang = transform.Find("Zhuang").gameObject;
        barrage = transform.Find("Perfabs/BarrageRoot/Barrage");
        barrageItem = transform.Find("Perfabs/BarrageRoot/BarrageItem");
        ui_game_pause = transform.Find("Panels/ui_game_pause");

        localCard = transform.Find("LocalCard").gameObject;

        btn_LookCard = transform.Find("OpreateButtons/btn_LookCard").gameObject;
        btn_Fold = transform.Find("OpreateButtons/btn_Fold").gameObject;
        btn_AutoFold = transform.Find("OpreateButtons/btn_AutoFold").gameObject;
        btn_Call = transform.Find("OpreateButtons/btn_Call").gameObject;
        btn_AutoCall = transform.Find("OpreateButtons/btn_AutoCall").gameObject;
        btn_DoubleChip = transform.Find("OpreateButtons/btn_DoubleChip").gameObject;
        btn_CanCard = transform.Find("OpreateButtons/btn_CanCard").gameObject;
        btn_Gamble = transform.Find("OpreateButtons/btn_Gamble").gameObject;

        #endregion
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        RedDotManager.GetInstance().SetRedDotNodeCallBack(E_RedDotDefine.MsgBox, MsgCallBack);
        RedDotManager.GetInstance().RefreshAll();

        zjh_procedure = GF.Procedure.CurrentProcedure as ZJHProcedure;

        foreach (var toggle in toggleTable)
        {
            toggle.onValueChanged.AddListener(OnTableBGToggleChanged);
        }

        CoroutineRunner.Instance.StartCoroutine(InitPanel(true));
    }

    public IEnumerator InitPanel(bool isAni)
    {
        ClearTable();
        Util.UpdateTableColor(transform.GetComponent<Image>());
        varBtnGameWait.SetActive(zjh_procedure.enterZjhDeskRs.BaseConfig.DeskType != DeskType.Match);
        menu.transform.Find("Menuc/BtnSitUp").gameObject.SetActive(zjh_procedure.enterZjhDeskRs.BaseConfig.DeskType != DeskType.Match);
        menu.transform.Find("Menuc/BtnDissolveRoom").gameObject.SetActive(zjh_procedure.enterZjhDeskRs.BaseConfig.DeskType != DeskType.Match);
        menu.transform.Find("Menuc/BringBtn").gameObject.SetActive(zjh_procedure.enterZjhDeskRs.BaseConfig.DeskType != DeskType.Match);
        menu.transform.Find("Menuc/ShopButton").gameObject.SetActive(zjh_procedure.enterZjhDeskRs.BaseConfig.DeskType != DeskType.Match);
        menu.transform.Find("Menuc/GamegoldButton").gameObject.SetActive(zjh_procedure.enterZjhDeskRs.BaseConfig.DeskType != DeskType.Match);
        menu.transform.Find("Menuc/GamediamondsButton").gameObject.SetActive(zjh_procedure.enterZjhDeskRs.BaseConfig.DeskType != DeskType.Match);
        varMatchRoomInfo.gameObject.SetActive(zjh_procedure.enterZjhDeskRs.BaseConfig.DeskType == DeskType.Match);
        varMatchRoomTime.gameObject.SetActive(zjh_procedure.enterZjhDeskRs.BaseConfig.DeskType == DeskType.Match);
        //初始化座位
        seatMgr.Init(zjh_procedure.enterZjhDeskRs.BaseConfig.PlayerNum);
        //要不要显示开始
        btnGameBegain.SetActive(IsRoomCreate() && zjh_procedure.enterZjhDeskRs.DeskState == DeskState.WaitStart);
        //初始化奖池
        varBtnJackpot.SetActive(zjh_procedure.enterZjhDeskRs.BaseConfig.Jackpot);
        //初始化Wpk
        varBtnWPK.SetActive(zjh_procedure.enterZjhDeskRs.BaseConfig.OpenSafe);
        //初始化房间信息
        UpdateRoundInfo(zjh_procedure.enterZjhDeskRs.Round);
        varQiangZhiLiangPai.SetActive(zjh_procedure.enterZjhDeskRs.Config.SysShowCard);
        varRoomNameID.text = "房间:" + zjh_procedure.enterZjhDeskRs.BaseConfig.DeskName + "(ID:" + zjh_procedure.deskID + ")";
        varRoominfo.text = "底注: " + zjh_procedure.enterZjhDeskRs.BaseConfig.BaseCoin +
                              "  必闷: " + zjh_procedure.enterZjhDeskRs.Config.MustStuffy +
                              "  比牌圈数: " + zjh_procedure.enterZjhDeskRs.Config.CompareRound +
                              "\n单注上限: " + (zjh_procedure.enterZjhDeskRs.Config.SingleCoin <= 0 ? "无上限" : zjh_procedure.enterZjhDeskRs.Config.SingleCoin.ToString()) +
                              "\n底池封顶: " + (zjh_procedure.enterZjhDeskRs.Config.MaxPot <= 0 ? "无上限" : zjh_procedure.enterZjhDeskRs.Config.MaxPot.ToString()) +
                              (zjh_procedure.enterZjhDeskRs.Config.Use235 ? "\n235大三条" : "") +
                              (zjh_procedure.enterZjhDeskRs.BaseConfig.IpLimit && zjh_procedure.enterZjhDeskRs.BaseConfig.GpsLimit ? "\nIP/GPS限制" :
                              (zjh_procedure.enterZjhDeskRs.BaseConfig.IpLimit ? "\nIP限制" : "") +
                              (zjh_procedure.enterZjhDeskRs.BaseConfig.GpsLimit ? "\nGPS限制" : "")) +
                              (zjh_procedure.enterZjhDeskRs.Config.BigLuckCard ? "\n大牌奖励" : "") +
                              (zjh_procedure.enterZjhDeskRs.Config.IsCompareDouble ? "\n双倍比牌" : "") +
                              (zjh_procedure.enterZjhDeskRs.Config.ShotCard ? "\n短牌模式" : "") +
                              (zjh_procedure.enterZjhDeskRs.Config.Stuffy ? "\n禁止闷比" : "");
        LayoutRebuilder.ForceRebuildLayoutImmediate(varRoominfo.GetComponent<RectTransform>());

        SetDeskState(zjh_procedure.enterZjhDeskRs.DeskState);
        //初始化座位玩家信息
        for (int i = 0; i < zjh_procedure.enterZjhDeskRs.DeskPlayers.Count; i++)
        {
            seatMgr.PlayerEnter(zjh_procedure.enterZjhDeskRs.DeskPlayers[i], isAni);
        }
        //如果有自己座位等待动画完成后再显示
        // if (zjh_procedure.enterZjhDeskRs.DeskPlayers.ToList().FirstOrDefault(item => Util.GetInstance().IsMy(item.BasePlayer.PlayerId)) != null)
        // {
        // }
        if (isAni)
        {
            yield return new WaitForSeconds(0.3f);
        }
        Seat_zjh selfPlayer = seatMgr.GetSelfSeat();
        if (selfPlayer != null)
        {
            ShowBtnCancelLiuZuo(selfPlayer.playerInfo.State == PlayerState.OffLine);
        }
        //游戏中
        if (float.Parse(zjh_procedure.enterZjhDeskRs.Pot) > 0)
        {
            zjh_procedure.IsGame = 1;
            // 添加非空检查和长度检查
            if (zjh_procedure.enterZjhDeskRs.PlayerOp != null && zjh_procedure.enterZjhDeskRs.PlayerOp.Count > 0)
            {
                seatMgr.RecoverySeats(zjh_procedure.enterZjhDeskRs.PlayerOp.ToList(), zjh_procedure.enterZjhDeskRs.CurrentOption);
            }
            ShowZhuang(false);
            SwitchPoolChipsObj(float.Parse(zjh_procedure.enterZjhDeskRs.Pot) > 0);
            zjh_procedure.lastPot = float.Parse(zjh_procedure.enterZjhDeskRs.Pot);
            zjh_procedure.compareUsers = zjh_procedure.enterZjhDeskRs.CompareUsers.ToList();
            SetPot(zjh_procedure.lastPot);
            // GF.LogInfo($"手牌:{selfPlayer?.HandCardsData}, 玩家信息:{selfPlayer?.playerInfo}");
            //自己参与了游戏
            if (selfPlayer != null && (selfPlayer.playerInfo.InGame || (selfPlayer.playerInfo.HandCard != null && selfPlayer.playerInfo.HandCard.Count > 0)))
            {
                SwitchSelfSeat(false);
                InitMyselfCards(selfPlayer.playerInfo.HandCard.ToArray());
                SwitchTypeImg(selfPlayer.playerInfo.HandCard[0] != -1, (ZjhCardType)selfPlayer.playerInfo.Niu);
                zjh_procedure.showCardsData = selfPlayer.playerInfo.ShowCard.ToArray();
                UpdateShowCardsBySelf(zjh_procedure.showCardsData);
                QiPaiCards(selfPlayer.playerInfo.IsGive || selfPlayer.playerInfo.CompareLose ? Color.gray : Color.white);
            }
            if (zjh_procedure.enterZjhDeskRs.CurrentOption != Position.NoSit)
            {
                ChangeOperation(zjh_procedure.enterZjhDeskRs.CurrentOption, zjh_procedure.enterZjhDeskRs.NextTime,
                    zjh_procedure.enterZjhDeskRs.Round, float.Parse(zjh_procedure.enterZjhDeskRs.BaseFollow == "" ? "0" : zjh_procedure.enterZjhDeskRs.BaseFollow));
            }
        }
        UpdateJackpot();

        if (zjh_procedure.enterZjhDeskRs.BaseConfig.DeskType == DeskType.Match)
        {
            Msg_getSingleMatchRq req = MessagePool.Instance.Fetch<Msg_getSingleMatchRq>();
            req.MatchId = zjh_procedure.enterZjhDeskRs.BaseConfig.MatchId;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_getSingleMatchRq, req);
        }
    }

    public IEnumerator Function_DeskPlayerInfoRs(DeskPlayer deskPlayer, bool isLeave)
    {
        seatMgr.PlayerEnter(deskPlayer);
        if (Util.IsMySelf(deskPlayer.BasePlayer.PlayerId))
        {
            SwitchSelfSeat(!deskPlayer.InGame);
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
        return zjh_procedure.enterZjhDeskRs.BaseConfig.Creator == Util.GetMyselfInfo().PlayerId;
    }

    // 更新圈数信息
    public void UpdateRoundInfo(int curIndex)
    {
        string str = "第 " + curIndex + "/" + (zjh_procedure.enterZjhDeskRs.Config.MaxRound == -1 ? "∞" : zjh_procedure.enterZjhDeskRs.Config.MaxRound) + " 圈";
        Seat_zjh player_Zjh = seatMgr.GetSelfSeat();
        str += "  我的投入 " + (player_Zjh != null ? player_Zjh.playerInfo.BetCoin : 0);
        varRoundInfo.text = str;
    }

    //回收上一个筹码
    public void ReSetLastChip()
    {
        if (zjh_procedure.PreOperateGuid != 0)
        {
            Seat_zjh lastPlayer = seatMgr.GetPlayerByPlayerID(zjh_procedure.PreOperateGuid);
            if (lastPlayer == null) return;
            lastPlayer.chipValue.transform.parent.gameObject.SetActive(false);
            PlayChips(lastPlayer.GetChipPos(), poolChipsObj.transform.localPosition);
            zjh_procedure.PreOperateGuid = 0;
        }
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
                varGameSetting.GetComponent<GameSettingZJH>().lb_RemianMoney.text = GF.DataModel.GetDataModel<UserDataModel>().Diamonds.ToString();
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
        GlobalManager.GetInstance().GetJackpotData(zjh_procedure.enterZjhDeskRs.BaseConfig.ClubId, out float total);
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
                if (Util.GetMyselfInfo().DeskId != 0 && Util.GetMyselfInfo().DeskId == zjh_procedure.deskID)
                {
                    Util.GetInstance().Send_EnterDeskRq(zjh_procedure.deskID);
                }
                else
                {
                    GF.LogInfo("返回登录界面");
                    HomeProcedure homeProcedure = GF.Procedure.GetProcedure<HomeProcedure>() as HomeProcedure;
                    homeProcedure.QuitGame();
                    // zjh_procedure.EnterHome();
                }
                break;
        }
    }

    IEnumerator DownloadAndPlayMp3(long playerid, string url)
    {
        // if (Util.GetInstance().IsMy(playerid) == true) yield break;
        string downloadUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}:{Const.ServerInfo_BackEndPort}/voice/{zjh_procedure.deskID}/";
        if (Const.CurrentServerType == Const.ServerType.外网服)
        {
            downloadUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}/voice/{zjh_procedure.deskID}/";
        }
        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(downloadUrl + url, AudioType.MPEG);
        request.certificateHandler = new BypassCertificate();
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            GF.LogError($"Error downloading audio: {request.error}");
            yield break;
        }
        Seat_zjh seatInfo = seatMgr.GetPlayerByPlayerID(playerid);
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
        if (zjh_procedure.enterZjhDeskRs.DeskState == DeskState.Pause)
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
        foreach (var toggle in toggleTable)
        {
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(OnTableBGToggleChanged);
            }
        }

        RedDotManager.GetInstance().RemoveRedDotNodeCallBack(E_RedDotDefine.MsgBox, MsgCallBack);
        ClearTable();
        varGameSetting.SetActive(false);
        menu.SetActive(false);
        bringUI.GetComponent<BringUI>().Clear();
        base.OnClose(isShutdown, userData);
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
                zjh_procedure.Send_Start();
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
                zjh_procedure.Send_AdvanceSettle();
                break;
            case "带入":
                Seat_zjh player = seatMgr.GetSelfSeat();
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
                if (zjh_procedure.enterZjhDeskRs.BaseConfig.DeskType == DeskType.Match)
                {
                    uiParams.Set<VarByteArray>("matchConfig", zjh_procedure.MatchInfo.ToByteArray());
                    long[] myList = seatMgr.GetInGameSeats().Select(seat => seat.playerInfo.BasePlayer.PlayerId).ToArray();
                    using (var memoryStream = new System.IO.MemoryStream())
                    {
                        using (var binaryWriter = new System.IO.BinaryWriter(memoryStream))
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
                    RectTransform varRealTimeBRPanel_zjh_rct = varRealTimeBRPanel_zjh.transform.GetComponent<RectTransform>();
                    varRealTimeBRPanel_zjh_rct.anchoredPosition = new Vector2(0 - varRealTimeBRPanel_zjh.transform.Find("Panel").GetComponent<RectTransform>().rect.width, varRealTimeBRPanel_zjh_rct.anchoredPosition.y);
                    varRealTimeBRPanel_zjh.SetActive(true);
                    varRealTimeBRPanel_zjh_rct.DOAnchorPosX(0, openTime);
                }
                break;
            case "关闭战绩":
                varRealTimeBRPanel_zjh.transform.GetComponent<RectTransform>().DOAnchorPosX(0 - varRealTimeBRPanel_zjh.transform.Find("Panel").GetComponent<RectTransform>().rect.width, closeTime).OnComplete(() =>
                {
                    varRealTimeBRPanel_zjh.SetActive(false);
                });
                break;
            case "文字聊天":
                await GF.UI.OpenUIFormAwait(UIViews.ChatPanel);
                break;
            case "消息记录":
                await GF.UI.OpenUIFormAwait(UIViews.MessagePanel);
                break;
            case "退出牌局":
                if (zjh_procedure.enterZjhDeskRs.BaseConfig.DeskType == DeskType.Match
                    && seatMgr.IsInTable(Util.GetMyselfInfo().PlayerId))
                {
                    zjh_procedure.EnterHome();
                }
                else
                {
                    zjh_procedure.Send_LeaveDeskRq();
                }
                break;
            case "站起围观":
                zjh_procedure.Send_SitUpRq();
                break;
            case "解散房间":
                Util.GetInstance().OpenConfirmationDialog("解散房间", "确定要解散房间?", () =>
                {
                    GF.LogInfo("解散房间");
                    zjh_procedure.Send_DisMissDeskRq();
                });
                break;
            case "黑名单":
                GF.LogInfo("黑名单");
                await GF.UI.OpenUIFormAwait(UIViews.GameBlackPlayerPanel);
                break;
            case "取消离桌":
                zjh_procedure.Send_CancelLeaveRq();
                break;
            case "暂停房间":
                zjh_procedure.Send_PauseDeskRq(1);
                break;
            case "恢复房间":
                if (IsRoomCreate())
                {
                    zjh_procedure.Send_PauseDeskRq(0);
                }
                break;
            case "购买时长":
                GameSettingZJH setting = varGameSetting.GetComponent<GameSettingZJH>();
                float fen = setting.SD_TimeArry[(int)setting.SD_TimeSlider.value] * 60;
                zjh_procedure.Send_DeskContinuedTimeRq((int)fen);
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
                List<Seat_zjh> seats = zjh_procedure.GamePanel_zjh.seatMgr.GetSeats();
                if (seats.Count < zjh_procedure.enterZjhDeskRs.BaseConfig.PlayerNum)
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
                zjh_procedure.Send_OptionRq(ZjhOption.AddTime);
                break;
            case "Jackpot":
                uiParams.Set<VarInt32>("methodType", (int)MethodType.GoldenFlower);
                uiParams.Set<VarByteArray>("deskConfig", zjh_procedure.enterZjhDeskRs.BaseConfig.ToByteArray());
                await GF.UI.OpenUIFormAwait(UIViews.GameJackpotPanel, uiParams);
                break;
            case "wpk":
                uiParams.Set<VarByteArray>("deskConfig", zjh_procedure.enterZjhDeskRs.BaseConfig.ToByteArray());
                await GF.UI.OpenUIFormAwait(UIViews.GameWPKPanel, uiParams);
                break;
            case "btn_LookCard":
                zjh_procedure.Send_OptionRq(ZjhOption.Look);
                break;
            case "btn_Fold":
                zjh_procedure.Send_OptionRq(ZjhOption.Give);
                break;
            case "btn_AutoFold":
                Util.AddLockTime(1f);
                zjh_procedure.Send_OptionRq(ZjhOption.AutoGive, varAutoFoldAni.activeSelf);
                break;
            case "btn_Call":
                zjh_procedure.Send_OptionRq(ZjhOption.Follow);
                break;
            case "btn_AutoCall":
                Util.AddLockTime(1f);
                zjh_procedure.Send_OptionRq(ZjhOption.AutoFollow, varAutoCallAni.activeSelf);
                break;
            case "btn_DoubleChip":
                zjh_procedure.Send_OptionRq(ZjhOption.Double);
                break;
            case "btn_CanCard":
                if (zjh_procedure.compareUsers.Count == 1)
                {
                    Seat_zjh seat_Zjh = seatMgr.GetPlayerByPlayerID(zjh_procedure.compareUsers[0]);
                    if (seat_Zjh == null) return;
                    zjh_procedure.Send_OptionRq(ZjhOption.Compare, false, seat_Zjh.GetPosition());
                }
                else
                {
                    seatMgr.ShowPkBoxList(true);
                }
                break;
            case "btn_Gamble":
                zjh_procedure.Send_OptionRq(ZjhOption.Allin);
                break;
            case "btn_selfCard1":
                RequestShowCard(0);
                break;
            case "btn_selfCard2":
                RequestShowCard(1);
                break;
            case "btn_selfCard3":
                RequestShowCard(2);
                break;
            case "btn_Check":
                break;
        }
        ;
    }

    public void RequestShowCard(int pos)
    {
        if (zjh_procedure.enterZjhDeskRs.Config.SysShowCard) return;
        Seat_zjh selfSeat = seatMgr.GetSelfSeat();
        if (selfSeat == null || selfSeat.IsNotEmpty() || !selfSeat.playerInfo.IsLook) return;
        if (selfSeat.playerInfo.HandCard != null && selfSeat.playerInfo.HandCard.Count > 0)
        {
            int showCard = selfSeat.playerInfo.HandCard[pos];
            zjh_procedure.UpdateShowCardsData(pos, showCard);
            zjh_procedure.Send_OptionRq(ZjhOption.Show);
        }
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
        if (bringUI.activeSelf || zjh_procedure.enterZjhDeskRs.BaseConfig.DeskType == DeskType.Match) return;
        bringUI.GetComponent<BringUI>().ShowBring(isLeave);
    }

    /// <summary>
    /// 收到发牌消息
    /// </summary>
    public void Syn_DealCard(List<DealCard> Deals)
    {
        zjh_procedure.IsGame = 1;
        ResetTable();
        foreach (var item in Deals)
        {
            seatMgr.Syn_DealCard(item);
        }
        if (btnGameBegain.activeSelf) btnGameBegain.SetActive(false);
        // 替换协程为DOTween动画序列
        DealCardsAnimation(Deals);
    }

    /// <summary>
    /// 发牌动画 - 使用DOTween替代协程
    /// </summary>
    public void DealCardsAnimation(List<DealCard> Deals)
    {
        QiPaiCards(Color.white);
        SwitchPoolChipsObj(true);
        ShowZhuang(true);

        // 创建动画序列
        Sequence dealSequence = DOTween.Sequence();

        // 添加初始延迟
        dealSequence.AppendInterval(0.3f);

        // 播放下筹码的声音和动画
        dealSequence.AppendCallback(() =>
        {
            Sound.PlayEffect("xiazhu.mp3");

            // 遍历玩家，播放筹码动画
            foreach (var item in Deals)
            {
                Seat_zjh player = seatMgr.GetPlayerByPlayerID(item.PlayerId);
                if (player != null)
                {
                    if (Util.IsMySelf(player.playerInfo.BasePlayer.PlayerId))
                    {
                        SwitchSelfSeat(false);
                    }
                    var seatPosition = player.transform.localPosition;
                    var poolPosition = poolChipsObj.localPosition;
                    PlayChips(seatPosition, poolPosition);
                }
            }
            SetPot(zjh_procedure.lastPot);
        });

        // 等待筹码动画完成
        dealSequence.AppendInterval(0.5f);

        // 发牌动画
        dealSequence.AppendCallback(() =>
        {
            GameObject card = fapaiAniObjChild.Find("Card").gameObject;
            SwitchTypeImg(false);
            Sound.PlayEffect("niuniu/card.mp3");

            foreach (var item in Deals)
            {
                Seat_zjh player = seatMgr.GetPlayerByPlayerID(item.PlayerId);
                if (player != null)
                {
                    var cardInstance = Instantiate(card, fapaiAniObjChild.transform);
                    cardInstance.transform.localScale = Vector3.one;
                    cardInstance.transform.localPosition = Vector3.zero;
                    cardInstance.SetActive(true);
                    bool isSelf = Util.IsMySelf(player.playerInfo.BasePlayer.PlayerId);
                    var targetGo = isSelf ? localCard : player.gameObject;

                    // 将卡片动画地移动到目标座位位置
                    Sequence cardSequence = DOTween.Sequence();
                    cardSequence.Append(cardInstance.transform.DOScale(Vector3.one * 1.5f, 0.6f));
                    cardSequence.Join(cardInstance.transform.DOLocalMove(targetGo.transform.localPosition, 0.6f));
                    cardSequence.OnComplete(() =>
                    {
                        Destroy(cardInstance);
                        if (isSelf)
                        {
                            int[] cards = new int[3] { -1, -1, -1 };
                            InitMyselfCards(cards);
                        }
                        else
                        {
                            player.SwitchHideCards(true, -1);
                        }
                    });
                }
            }
        });

        // 等待发牌动画完成
        dealSequence.AppendInterval(0.6f);

        // 最后播放音效
        dealSequence.AppendCallback(() =>
        {
            Sound.PlayEffect("qipai.mp3");
        });

        // 设置动画序列的ID为当前游戏面板，便于后续清除
        dealSequence.SetId(gameObject);
    }

    public void PlayChips(Vector3 startPos, Vector3 endPos)
    {
        // GF.LogError( $"startPos:{startPos},endPos:{endPos}");
        chipManager.PlayChips(startPos, endPos);
    }

    public void GuZhuYiZhi_BiPai(Seat_zjh player)
    {
        player.ShowOperateImage(ZjhOption.Compare, false);
        if (Util.IsMySelf(player.playerInfo.BasePlayer.PlayerId))
        {
            //隐藏按钮  置灰底牌
            ShowOperateBtns(false);
            SetAutoBtns(false);
            player.playerInfo.CompareLose = true;
            QiPaiCards(Color.gray); // 将本地牌设置为灰色
        }
        else
        {
            SetOtherCardsColor(player.GetPosition(), Color.gray);
        }
    }

    //比牌动画
    public void PlayBiPaiAnimation(Seat_zjh player, Seat_zjh ComparePlayer, bool isWin)
    {
        bool isSelf = Util.IsMySelf(player.playerInfo.BasePlayer.PlayerId);
        bool isSelfCompare = Util.IsMySelf(ComparePlayer.playerInfo.BasePlayer.PlayerId);

        // 创建动画序列
        Sequence biPaiSequence = DOTween.Sequence();

        // 显示比牌效果
        biPaiSequence.AppendCallback(() =>
        {
            varBiPaiEffect.SetActive(true);
            // 设置比牌效果的位置为当前玩家的位置
            varBiPaiEffect.transform.localPosition = player.transform.localPosition;
        });

        // 移动效果到被比较的座位位置
        biPaiSequence.Append(varBiPaiEffect.transform.DOLocalMove(ComparePlayer.transform.localPosition, 0.4f));

        // 完成移动后的操作
        biPaiSequence.AppendCallback(() =>
        {
            varBiPaiEffect.SetActive(false);

            //如果赢
            if (isWin)
            {
                Sound.PlayEffect("zjh/pk8_paipai_8kp.mp3");
                player.ShowOperateImage(ZjhOption.Compare, isWin);
                ComparePlayer.ShowOperateImage(ZjhOption.Compare, !isWin);
                //输家是自己
                if (isSelfCompare)
                {
                    //隐藏按钮  置灰底牌
                    ShowOperateBtns(false);
                    SetAutoBtns(false);
                    ComparePlayer.playerInfo.CompareLose = true;
                    QiPaiCards(Color.gray); // 将本地牌设置为灰色
                }
                else
                {
                    SetOtherCardsColor(ComparePlayer.GetPosition(), Color.gray);
                }
            }
            else
            {
                player.ShowOperateImage(ZjhOption.Compare, isWin);
                //比牌发起人输了 不用显示赢家
                // ComparePlayer.ShowOperateImage(ZjhOption.Compare, isWin);
                //输家是自己
                if (isSelf)
                {
                    //隐藏按钮  置灰底牌
                    ShowOperateBtns(false);
                    SetAutoBtns(false);
                    player.playerInfo.CompareLose = true;
                    QiPaiCards(Color.gray); // 将本地牌设置为灰色
                }
                else
                {
                    SetOtherCardsColor(player.GetPosition(), Color.gray);
                }

                // 显示被赢效果
                varBeWinEffect.SetActive(true);
                varBeWinEffect.transform.localPosition = ComparePlayer.transform.localPosition;
                // 播放比牌赢音效
                Sound.PlayEffect("zjh/pk8_shield_8kp.mp3");
            }
        });

        // 如果是输的情况，添加被赢效果的显示时间
        if (!isWin)
        {
            biPaiSequence.AppendInterval(0.6f);
            biPaiSequence.AppendCallback(() =>
            {
                varBeWinEffect.SetActive(false);
            });
        }

        // 设置动画序列的ID为当前游戏面板，便于后续清除
        biPaiSequence.SetId(gameObject);
    }

    public void SetOtherCardsColor(Position pos, Color color)
    {
        Seat_zjh player = seatMgr.GetPlayerByPos(pos);
        if (player == null) return;
        player.SetHideCardsColor(color);
    }

    private void PlayGZYZAnimation(int[] cards)
    {
        // 创建动画序列
        Sequence gzyzSequence = DOTween.Sequence();

        // 播放声音
        gzyzSequence.AppendCallback(() =>
        {
            // Play sound
            Sound.PlayEffect("zjh/pk8_arrow_8kp.mp3");
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    Card card = varGuZhuYiZhi.transform.Find("card" + (index + 1)).GetComponent<Card>();
                    if (cards != null && cards.Length > index)
                    {
                        card.SetImgColor(Color.white);
                        card.InitAni(cards[index], 0.2f);
                    }
                    else
                    {
                        card.SetImgColor(Color.gray);
                        card.Init(-1);
                    }
                });
            }
            varGuZhuYiZhi.SetActive(true);
            varGuZhuYiZhiEffect.transform.localPosition = new Vector3(-460, 0, 0);
        });

        // 执行移动动画
        gzyzSequence.Append(varGuZhuYiZhiEffect.transform.DOLocalMoveX(460, 0.5f));

        // 显示一段时间后隐藏
        gzyzSequence.AppendInterval((cards != null && cards.Length > 0) ? 2f : 1f);
        gzyzSequence.AppendCallback(() =>
        {
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    Card card = varGuZhuYiZhi.transform.Find("card" + (index + 1)).GetComponent<Card>();
                    card.SetImgColor(Color.gray);
                    card.Init(-1);
                });
            }
            varGuZhuYiZhi.SetActive(false);
        });

        // 设置动画序列的ID为当前游戏面板，便于后续清除
        gzyzSequence.SetId(gameObject);
    }

    /// <summary>
    /// 结算动画 - 使用DOTween实现
    /// </summary>
    public void PlaySettleAnimation(ZjhSettle[] settles)
    {
        List<Seat_zjh> inGamePlayers = seatMgr.GetInGameSeats();
        zjh_procedure.IsGame = 2;
        zjh_procedure.enterZjhDeskRs.Round = 0;
        ShowOperateBtns(false);
        SetAutoBtns(false);
        ShowLightBg(false);

        // 创建变量，在多个回调中共享
        ZjhCardType winType = ZjhCardType.High;
        List<Seat_zjh> winPlayers = new List<Seat_zjh>();
        List<float> playerWinChips = new List<float>();

        // 创建动画序列
        Sequence settleSequence = DOTween.Sequence();

        // 等待一段时间后执行清理操作
        settleSequence.AppendInterval(1f);
        settleSequence.AppendCallback(() =>
        {
            // 清理UI元素
            varAddTimeBtn.SetActive(false);
            varBeWinEffect.SetActive(false);
            varBiPaiEffect.SetActive(false);

            // 回收上一次下注的筹码
            ReSetLastChip();

            // 处理结算数据
            foreach (var info in settles)
            {
                float coinChange = float.Parse(info.CoinChange.ToString());
                Seat_zjh player = seatMgr.GetPlayerByPlayerID(info.PlayerId);
                if (player == null) continue;
                player.SwitchHideCards(false, -1);
                player.signLookCard.SetActive(false);

                if (coinChange > 0)
                {
                    winType = info.Type;
                    winPlayers.Add(player);
                    playerWinChips.Add(coinChange);
                }

                if (info.HandCard != null && info.HandCard.Count == 3)
                {
                    if (info.HandCard[0] == -1 && info.HandCard[1] == -1 && info.HandCard[2] == -1)
                    {
                        continue;
                    }
                    if (!Util.IsMySelf(info.PlayerId))
                    {
                        player.ShowHandCards(info.HandCard.ToArray(), info.Type);
                    }
                    else
                    {
                        GF.LogInfo(player.playerInfo.ToString());
                        if (!player.playerInfo.IsLook)
                        {
                            ShowMyselfCards(info.HandCard.ToArray(), info.Type);
                        }
                    }
                }
            }
        });

        // 展示玩家手牌后等待
        settleSequence.AppendInterval(1f);

        // 特殊牌型效果
        settleSequence.AppendCallback(() =>
        {
            if (zjh_procedure.enterZjhDeskRs.Config.BigLuckCard)
            {
                // 同花顺效果
                if (winType == ZjhCardType.GoldAlong)
                {
                    varTongHuaShunEffect.SetActive(true);

                    // 为每个赢家创建筹码动画
                    for (int i = 0; i < winPlayers.Count; i++)
                    {
                        int playerIndex = i; // 创建局部变量以在lambda中使用
                        Sequence playerSequence = DOTween.Sequence();

                        playerSequence.AppendCallback(() =>
                        {
                            Sound.PlayEffect("chipmove.mp3");
                            foreach (var player in inGamePlayers)
                            {
                                if (player && winPlayers[playerIndex] &&
                                    winPlayers[playerIndex].playerInfo.BasePlayer.PlayerId != player.playerInfo.BasePlayer.PlayerId)
                                {
                                    PlayChips(player.transform.localPosition, winPlayers[playerIndex].transform.localPosition);
                                }
                            }
                        });

                        playerSequence.AppendInterval(0.5f);
                        settleSequence.Append(playerSequence);
                    }

                    settleSequence.AppendInterval(1f);
                    settleSequence.AppendCallback(() =>
                    {
                        varTongHuaShunEffect.SetActive(false);
                    });
                }
                else if (winType == ZjhCardType.Boom)
                {
                    varSanTiaoEffect.SetActive(true);

                    settleSequence.AppendCallback(() =>
                    {
                        Sound.PlayEffect("chipmove.mp3");
                        for (int i = 0; i < winPlayers.Count; i++)
                        {
                            foreach (var player in inGamePlayers)
                            {
                                if (player && winPlayers[i] &&
                                    winPlayers[i].playerInfo.BasePlayer.PlayerId != player.playerInfo.BasePlayer.PlayerId)
                                {
                                    PlayChips(player.transform.localPosition, winPlayers[i].transform.localPosition);
                                }
                            }
                        }
                    });

                    settleSequence.AppendInterval(1f);
                    settleSequence.AppendCallback(() =>
                    {
                        varSanTiaoEffect.SetActive(false);
                    });
                }
            }
        });

        // 筹码移动到赢家
        settleSequence.AppendCallback(() =>
        {
            SwitchPoolChipsObj(false);
            Sound.PlayEffect("chipmove.mp3");

            for (int i = 0; i < winPlayers.Count; i++)
            {
                if (winPlayers[i] == null) continue;
                PlayChips(poolChipsObj.transform.localPosition, winPlayers[i].transform.localPosition);
            }
        });

        // 等待筹码移动完成
        settleSequence.AppendInterval(0.5f);

        // 更新玩家筹码
        settleSequence.AppendCallback(() =>
        {
            for (int i = 0; i < winPlayers.Count; i++)
            {
                if (winPlayers[i] == null) continue;
                winPlayers[i].SwitchWinChips(true, playerWinChips[i]);
            }

            foreach (var player in inGamePlayers)
            {
                if (player == null || !player.IsNotEmpty()) continue;
                player.ScoreChange(float.Parse(player.playerInfo.Coin));
                if (Util.IsMySelf(player.playerInfo.BasePlayer.PlayerId))
                {
                    SetLocalScore(float.Parse(player.playerInfo.Coin));
                }
            }

            // 显示赢家特效
            if (winPlayers.FirstOrDefault(item => item.IsNotEmpty() && Util.IsMySelf(item.playerInfo.BasePlayer.PlayerId)))
            {
                varYouWin.SetActive(true);
                Sound.PlayEffect("zjh/pk8_win_8kp.mp3");
            }
            Function_SynCKJackPot();
        });

        // 延迟自行清理桌面
        settleSequence.AppendInterval(2f);
        settleSequence.AppendCallback(() =>
        {
            if (zjh_procedure.enterZjhDeskRs.Round > 0)
            {
                return;
            }
            ResetTable();
            zjh_procedure.IsGame = 0;
        });

        // 设置动画序列的ID为当前游戏面板，便于后续清除
        settleSequence.SetId(gameObject);
    }

    public void Function_SynCKJackPot()
    {
        if (zjh_procedure.msg_SynCKJackPot == null) return;
        Msg_SynCKJackPot ack = zjh_procedure.msg_SynCKJackPot;
        zjh_procedure.msg_SynCKJackPot = null;
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
            Seat_zjh seat_zjh = seatMgr.GetPlayerByPlayerID(playerId);
            if (seat_zjh == null) continue;

            // 创建序列，延迟处理多个奖池
            Sequence jackpotSequence = DOTween.Sequence();
            jackpotSequence.SetTarget(gameObject);

            // 根据索引设置延迟
            jackpotSequence.AppendInterval(currentIndex * 0.2f);

            // 添加动画回调
            jackpotSequence.AppendCallback(() =>
            {
                //真正克隆爆炸动画和文本
                GameObject boomAni = Instantiate(varJackBoomAni, fapaiAniObjChild.transform);
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
                moveSequence.Append(boomAni.transform.DOMove(seat_zjh.transform.position, 1.0f).SetEase(Ease.OutQuad));
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
                    _ = GF.Sound.PlayEffect("ANIzhuanpan2Audio.mp3");

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
        if (zjh_procedure.enterZjhDeskRs.BaseConfig.DeskType == DeskType.Match)
        {
            varBtncancelliuzuo_Match.SetActive(true);
        }
        else
        {
            varBtncancelliuzuo.SetActive(true);
        }
    }

    //切换显示自己座位
    public void SwitchSelfSeat(bool isShow)
    {
        Seat_zjh player = seatMgr.GetSelfSeat();
        if (player == null) return;
        SetLocalScore(player.GetCoin());
        player.scoreValue.text = Util.FormatAmount(player.GetCoin());
        player.player.SetActive(isShow);
        player.empty.SetActive(isShow);
        varLocalScore.SetActive(!isShow);
    }

    public void SetLocalScore(float coin)
    {
        varLocalScoreText.text = Util.FormatAmount(coin);
        LayoutRebuilder.ForceRebuildLayoutImmediate(varLocalScoreText.GetComponent<RectTransform>());
    }

    public void UpdateAddTimePrice()
    {
        long buyCount = seatMgr.GetSelfSeat().playerInfo.BuyTime;
        int basePrice = GlobalManager.GetConstants(14);
        varAddTimePrice.text = buyCount == 0
            ? basePrice.ToString()
            : (basePrice * Mathf.Pow(2, buyCount)).ToString();  // 每次购买时翻倍价格
    }

    public void ShowZhuang(bool isAnim)
    {
        zhuang.SetActive(false);
        Seat_zjh seatNode = seatMgr.GetBanker();
        if (seatNode == null) return;
        long zhuangId = seatNode.playerInfo.BasePlayer.PlayerId;
        zhuang.SetActive(true);
        Vector3 position;
        position = GetWidgetPosition(zhuangId, WidgetType.Zhuang, seatNode.transform.localPosition);
        if (isAnim)
            zhuang.transform.DOLocalMove(position + seatNode.transform.localPosition, 0.2f);
        else
            zhuang.transform.localPosition = position + seatNode.transform.localPosition;
    }

    public void ReShowCountdown()
    {
        if (zjh_procedure.enterZjhDeskRs.DeskState == DeskState.WaitStart) return;

        if (countdownCtr.gameObject.activeSelf)
        {
            Position position = countdownCtr.Pos;
            Seat_zjh seat = seatMgr.GetPlayerByPos(position);
            if (seat == null || !seat.IsNotEmpty()) return;
            bool isLocalOperate = Util.IsMySelf(seat.playerInfo.BasePlayer.PlayerId);
            if (isLocalOperate)
            {
                countdownCtr.transform.localScale = new Vector3(1.35f, 1.35f, 1.35f);
                countdownCtr.GetComponent<RectTransform>().position = btn_Fold.GetComponent<RectTransform>().position;
            }
            else
            {
                countdownCtr.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f) * GameConstants.GetScaleByScreenSize();
                countdownCtr.transform.localPosition = seat.transform.localPosition;
            }
        }
    }

    public void ShowLightBg(bool isShow, Seat_zjh seat = null)
    {
        varLightBg.SetActive(isShow);
        if (isShow)
        {
            Quaternion angle = GameUtil.GetQuaternion(varLightBg.transform.localPosition, seat.transform.localPosition);
            Vector3 currentEuler = varLightBg.transform.localEulerAngles;
            Vector3 targetEuler = angle.eulerAngles;
            float targetZ = targetEuler.z;
            //相等不用动
            if (targetZ == currentEuler.z) return;
            // 如果目标角度小于当前角度，增加360度确保顺时针旋转
            if (targetZ > currentEuler.z)
            {
                targetZ -= 360f;
            }

            varLightBg.transform.DOLocalRotate(new Vector3(targetEuler.x, targetEuler.y, targetZ), 0.1f, RotateMode.FastBeyond360);
        }
    }

    // 操作倒计时
    public void ShowCountdown(Seat_zjh seat, int remainTime = 0)
    {
        // 检查游戏状态是否为等待玩家操作
        if (zjh_procedure.enterZjhDeskRs.DeskState == DeskState.WaitStart) return;

        // 检查是否为本地操作
        bool isLocalOperate = Util.IsMySelf(seat.playerInfo.BasePlayer.PlayerId);
        if (isLocalOperate)
        {
            countdownCtr.transform.localScale = new Vector3(1.35f, 1.35f, 1.35f);
            countdownCtr.GetComponent<RectTransform>().position = btn_Fold.GetComponent<RectTransform>().position;
            //强制牌变为白色
            QiPaiCards(Color.white);
        }
        else
        {
            countdownCtr.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f) * GameConstants.GetScaleByScreenSize();
            countdownCtr.transform.localPosition = seat.transform.localPosition;
            //强制牌变为白色
            SetOtherCardsColor(seat.GetPosition(), Color.white);
        }
        if (remainTime > 0)
        {
            countdownCtr.gameObject.SetActive(true);
            countdownCtr.SetWarningTime(5, true, localCard);
            countdownCtr.ResetAndStartCountDown(remainTime > 15 ? remainTime : 15, remainTime, isLocalOperate, seat.GetPosition());
        }
        else
        {
            countdownCtr.gameObject.SetActive(false);
        }
    }

    public void HideCountdown()
    {
        countdownCtr.Pause();
        countdownCtr.gameObject.SetActive(false);
    }

    //切换操作人
    public void ChangeOperation(Position pos, long nextTime, int round, float baseCoin)
    {
        UpdateRoundInfo(round);
        //GF.LogError("nextTime" + nextTime);
        int time = (int)(nextTime - Util.GetServerTime()) / 1000;
        ShowButtons(time, pos, baseCoin);
    }

    //显示按钮
    private void ShowButtons(int time, Position pos, float baseCoin)
    {
        Seat_zjh seat = seatMgr.GetPlayerByPos(pos);
        if (seat == null || !seat.IsNotEmpty()) return;
        ShowLightBg(true, seat);
        ShowCountdown(seat, time);
        ShowOperateBtnsBySeat(seat, baseCoin);
    }

    public void SetShowCardBools(Position pos, int[] param2)
    {
        Seat_zjh seat = seatMgr.GetPlayerByPos(pos);
        if (seat == null || !seat.IsNotEmpty()) return;
        if (Util.IsMySelf(seat.playerInfo.BasePlayer.PlayerId))
        {
            UpdateShowCardsBySelf(param2);
        }
        else
        {
            seat.ShowHandCardsBySelf(param2);
        }
    }

    public void SetAutoDate(Position pos, ZjhOption option, string param)
    {
        Seat_zjh seat = seatMgr.GetPlayerByPos(pos);
        if (seat == null || !seat.IsNotEmpty()) return;
        if (option == ZjhOption.AutoFollow)
        {
            seat.playerInfo.AutoFollow = float.Parse(param) == 1;
        }
        else if (option == ZjhOption.AutoGive)
        {
            seat.playerInfo.AutoGive = float.Parse(param) == 1;
        }
        SetAutoBtns(true);
    }

    /// <summary>
    /// 操作返回
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="option"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public IEnumerator AsynOperate(Syn_OptionInfo ack)
    {
        Position pos = ack.CurrentOption;
        ZjhOption option = ack.Option;
        string param = ack.Param;
        float amount = param == "" ? 0 : float.Parse(param);
        // 获取座位信息
        Seat_zjh seat = seatMgr.GetPlayerByPos(pos);
        if (seat == null || !seat.IsNotEmpty()) yield break;
        bool isSelf = Util.IsMySelf(seat.playerInfo.BasePlayer.PlayerId);
        // 暂停倒计时
        HideCountdown();
        seatMgr.ShowPkBoxList(false);

        seat.ShowOperateImage(option);
        switch (option)
        {
            case ZjhOption.Look:
                if (isSelf)
                {
                    Sound.PlayEffect("zjh/opencard.mp3");
                    seat.playerInfo.IsLook = true;
                    btn_LookCard.SetActive(false);
                }
                else
                {
                    Sound.PlayEffect("zjh/opencardOther.mp3");
                    seat.signLookCard.SetActive(true);
                    seat.SwitchHideCards(true, -2);
                }
                break;
            case ZjhOption.Follow:
            case ZjhOption.Double:
            case ZjhOption.Compare:
                if (isSelf)
                {
                    ShowOperateBtns(false);
                    SetAutoBtns(true);
                }
                break;
            case ZjhOption.Give:
                Sound.PlayEffect("qipai.mp3");
                if (isSelf)
                {
                    seat.playerInfo.IsGive = true;
                    ShowOperateBtns(false);
                    SetAutoBtns(false);
                    QiPaiCards(Color.gray);
                    Vector3 vector3 = localCard.transform.localPosition;
                    // 将卡牌移动到目标位置
                    localCard.transform
                        .DOLocalMoveY(900, 0.5f)
                        .OnComplete(() =>
                        {
                            localCard.transform.localPosition = vector3;
                        });
                }
                else
                {
                    SetOtherCardsColor(seat.GetPosition(), Color.gray);
                }
                break;
            case ZjhOption.Allin:
                // 孤注一掷逻辑
                PlayGZYZAnimation(ack.Param2.ToArray());
                break;
        }

        //没有下注操作直接跳过
        if (amount <= 0)
        {
            yield break;
        }
        // 需要把上一个玩家的筹码入池
        if (zjh_procedure.PreOperateGuid != 0)
        {
            ReSetLastChip();
            // yield return new WaitForSeconds(0.45f);
        }
        zjh_procedure.PreOperateGuid = seat.playerInfo.BasePlayer.PlayerId;
        Sound.PlayEffect("xiazhu.mp3");
        var offsetPosition = GetWidgetPosition(seat.playerInfo.BasePlayer.PlayerId, WidgetType.DownChip, seat.transform.localPosition);
        PlayChips(seat.transform.localPosition, seat.transform.localPosition + offsetPosition);
        yield return new WaitForSeconds(0.45f);
        seat.ShowBetCoin(amount);
        //改变底池
        SetPot(zjh_procedure.lastPot);
    }

    public void ShowOperateBtns(bool isShow)
    {
        btn_LookCard.SetActive(isShow);
        btn_Call.SetActive(isShow);
        btn_DoubleChip.SetActive(isShow);
        btn_CanCard.SetActive(isShow);
        btn_Fold.SetActive(isShow);
        btn_Gamble.SetActive(isShow);
    }

    public void SetAutoBtns(bool isShowAuto)
    {
        Seat_zjh selfPlayer = seatMgr.GetSelfSeat();
        if (selfPlayer == null) return;
        if (selfPlayer.playerInfo.IsGive || selfPlayer.playerInfo.CompareLose)
        {
            isShowAuto = false;
        }
        if (btn_Fold.activeSelf)
        {
            isShowAuto = false;
        }
        btn_AutoCall.SetActive(isShowAuto);
        btn_AutoFold.SetActive(isShowAuto);

        if (isShowAuto && selfPlayer.playerInfo.AutoGive)
        {
            varAutoFoldAni.transform.DOKill();
            varAutoFoldAni.transform.DORotate(new Vector3(0, 0, -360), 2f, RotateMode.LocalAxisAdd).SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart);
            varAutoFoldAni.SetActive(true);
        }
        else
        {
            varAutoFoldAni.SetActive(false);
        }
        if (isShowAuto && selfPlayer.playerInfo.AutoFollow)
        {
            varAutoCallAni.transform.DOKill();
            varAutoCallAni.transform.DORotate(new Vector3(0, 0, -360), 2f, RotateMode.LocalAxisAdd).SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart);
            varAutoCallAni.SetActive(true);
        }
        else
        {
            varAutoCallAni.SetActive(false);
        }
    }

    public void ShowOperateBtnsBySeat(Seat_zjh seat, float baseCoin)
    {
        bool isSelf = Util.IsMySelf(seat.playerInfo.BasePlayer.PlayerId);
        varAddTimeBtn.SetActive(isSelf && zjh_procedure.enterZjhDeskRs.BaseConfig.DeskType != DeskType.Match);
        seat.operateType.gameObject.SetActive(seat.operateType.name == "Look");
        if (isSelf)
        {
            SetAutoBtns(false);

            UpdateAddTimePrice();
            btn_LookCard.SetActive(!seat.playerInfo.IsLook);

            float addChip = 0;
            float canChip = 0;
            float callChip = 0;
            List<long> CanCompareGuids = zjh_procedure.compareUsers;
            //闷牌
            if (!seat.playerInfo.IsLook)
            {
                callChip = baseCoin != 0 ? baseCoin : zjh_procedure.enterZjhDeskRs.BaseConfig.BaseCoin;
                addChip = callChip * 2;

                canChip = zjh_procedure.enterZjhDeskRs.Config.IsCompareDouble ? callChip * 2 : callChip;

                if (zjh_procedure.enterZjhDeskRs.Config.SingleCoin > 0)
                {
                    int maxDownChip = Mathf.FloorToInt(zjh_procedure.enterZjhDeskRs.Config.SingleCoin / 2f);
                    addChip = Mathf.Min(addChip, maxDownChip);
                }
            }
            else // "看牌" (Show cards)
            {
                callChip = (baseCoin != 0 ? baseCoin : zjh_procedure.enterZjhDeskRs.BaseConfig.BaseCoin) * 2;
                addChip = callChip * 2;

                canChip = zjh_procedure.enterZjhDeskRs.Config.IsCompareDouble ? callChip * 2 : callChip;

                if (zjh_procedure.enterZjhDeskRs.Config.SingleCoin > 0)
                {
                    addChip = Mathf.Min(addChip, zjh_procedure.enterZjhDeskRs.Config.SingleCoin);
                }
            }

            // Update button visibility and text based on chips
            btn_Call.SetActive(seat.GetCoin() > callChip);
            btn_DoubleChip.SetActive(seat.GetCoin() >= addChip && addChip > callChip);

            btn_Call.transform.Find("Text").GetComponent<Text>().text = callChip.ToString();
            btn_DoubleChip.transform.Find("Text").GetComponent<Text>().text = addChip.ToString();

            btn_CanCard.transform.Find("Text").GetComponent<Text>().text = canChip.ToString();
            btn_CanCard.SetActive((seat.playerInfo.IsLook || !zjh_procedure.enterZjhDeskRs.Config.Stuffy)
                && zjh_procedure.enterZjhDeskRs.Round >= zjh_procedure.enterZjhDeskRs.Config.CompareRound
                && CanCompareGuids != null && CanCompareGuids.Count > 0 && seat.GetCoin() >= canChip);

            btn_Gamble.SetActive(seat.GetCoin() <= callChip);
            btn_Fold.SetActive(true);
        }
        else
        {
            if (seatMgr.GetSelfSeat() && seatMgr.GetSelfSeat().IsNotEmpty() && seatMgr.GetSelfSeat().playerInfo.InGame)
            {
                SetAutoBtns(true);
            }
        }
    }

    public void SwitchTypeImg(bool isShow, ZjhCardType cardType = ZjhCardType.High)
    {
        if (isShow)
        {
            typeImg.SetSprite(GameConstants.GetCardTypeString(cardType), true);
            typeImg.gameObject.SetActive(true);
        }
        else
        {
            typeImg.gameObject.SetActive(false);
        }
    }

    public void InitMyselfCards(int[] cards)
    {
        for (int i = 0; i < 3; i++)
        {
            Transform card = localCard.transform.GetChild(i);
            card.GetComponent<Card>().Init(cards[i]);
            card.gameObject.SetActive(true);
        }
    }

    public void QiPaiCards(Color color)
    {
        for (int i = 0; i < 3; i++)
        {
            Transform card = localCard.transform.GetChild(i);
            card.GetComponent<Card>().SetImgColor(color);
        }
    }

    public void ShowMyselfCards(int[] cards, ZjhCardType cardType)
    {
        if (typeImg.gameObject.activeSelf) return;
        Seat_zjh seat_Zjh = seatMgr.GetSelfSeat();
        if (seat_Zjh == null) return;
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            Transform card = localCard.transform.GetChild(index);
            card.transform.DORotate(new Vector3(0, 90, 0), 0.2f).OnComplete(() =>
            {
                if (card.gameObject == null) return;
                card.GetComponent<Card>().Init(cards[index]);
                card.transform.DORotate(Vector3.zero, 0.2f).OnComplete(() =>
                {
                    if (card.gameObject == null) return;
                    card.transform.localRotation = Quaternion.identity;
                    if (index == 2)
                    {
                        SwitchTypeImg(true, cardType);
                        GameConstants.PlayCardSound(cardType);
                    }
                });
            });
        }
    }
    public void UpdateShowCardsBySelf(int[] cards)
    {
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            if (cards.Length > index)
            {
                Transform card = localCard.transform.GetChild(index);
                card.Find("Show").gameObject.SetActive(cards[index] != -1);
            }
        }
    }


    public void SetDeskState(DeskState deskState)
    {
        zjh_procedure.enterZjhDeskRs.DeskState = deskState;
        ui_game_pause.gameObject.SetActive(deskState == DeskState.Pause);
    }

    public void SetPot(float pot)
    {
        varPotSumText.text = Util.FormatAmount(pot);
        LayoutRebuilder.ForceRebuildLayoutImmediate(varPotSumText.GetComponent<RectTransform>());
    }

    public void SwitchPoolChipsObj(bool isShow)
    {
        poolChipsObj.gameObject.SetActive(isShow);
    }

    public void ResetTable()
    {
        GF.StaticUI.ShowMatching(false);
        foreach (Transform item in localCard.transform)
        {
            item.gameObject.GetComponent<Card>().Init(-1);
            item.transform.Find("Show").gameObject.SetActive(false);
            item.gameObject.SetActive(false);
        }
        QiPaiCards(Color.white);
        seatMgr.ResetSeatsPlayer();
        varAddTimeBtn.SetActive(false);
        varYouWin.SetActive(false);
        varTongHuaShunEffect.SetActive(false);
        varSanTiaoEffect.SetActive(false);
        varGuZhuYiZhi.SetActive(false);
        SwitchTypeImg(false);
        ShowOperateBtns(false);
        SetAutoBtns(false);
        SetPot(0);
        SwitchSelfSeat(true);
        zhuang.SetActive(false);
        HideCountdown();
        ShowLightBg(false);
        UpdateRoundInfo(0);
        zjh_procedure.showCardsData = new int[3] { -1, -1, -1 };
    }

    public void ClearTable()
    {
        foreach (Transform item in localCard.transform)
        {
            item.gameObject.GetComponent<Card>().Init(-1);
            item.transform.Find("Show").gameObject.SetActive(false);
            item.gameObject.SetActive(false);
        }
        QiPaiCards(Color.white);
        seatMgr.ClearSeatsPlayer();
        varBtnJackpot.SetActive(false);
        varLocalScore.SetActive(false);
        varAddTimeBtn.SetActive(false);
        varYouWin.SetActive(false);
        varTongHuaShunEffect.SetActive(false);
        varSanTiaoEffect.SetActive(false);
        varGuZhuYiZhi.SetActive(false);
        SwitchTypeImg(false);
        ShowBtnCancelLiuZuo(false);
        ShowOperateBtns(false);
        SetAutoBtns(false);
        SwitchPoolChipsObj(false);
        SwitchSelfSeat(true);
        zhuang.SetActive(false);
        ShowLightBg(false);
        HideCountdown();
        UpdateRoundInfo(0);
        zjh_procedure.showCardsData = new int[3] { -1, -1, -1 };
    }

    void MsgCallBack(RedDotNode node)
    {
        if (gameObject == null) return;
        varRedDot.SetDotState(node.rdCount > 0, node.rdCount);
    }

    public override void OnClickClose()
    {
        if (Util.IsClickLocked()) return;
        _ = GF.Sound.PlayEffect(AudioKeys.SOUND_BTN);
        Util.GetInstance().OpenConfirmationDialog("退出房间", "确定要退出房间吗?", () =>
        {
            zjh_procedure.Send_LeaveDeskRq();
        });
    }

    public void OnVoicePress()
    {
        if (zjh_procedure.enterZjhDeskRs.BaseConfig.ForbidVoice)
        {
            GF.LogInfo("禁止语音");
            GF.UI.ShowToast("禁止聊天!");
            return;
        }
        Seat_zjh seat = seatMgr.GetSelfSeat();
        if (seat == null || seat.IsNotEmpty() == false)
        {
            return;
        }
        if (seat.playerInfo.LeaveTime > 0 || float.Parse(seat.playerInfo.Coin) <= 0)
        {
            return;
        }
        varVoice.StartRecord(Util.GetMyselfInfo().PlayerId, zjh_procedure.deskID);
    }

    public void OnVoiceRelease()
    {
        if (zjh_procedure.enterZjhDeskRs.BaseConfig.ForbidVoice)
        {
            return;
        }
        varVoice.StopRecord();
    }

    public void OnVoiceCancel()
    {
        if (zjh_procedure.enterZjhDeskRs.BaseConfig.ForbidVoice)
        {
            return;
        }
        varVoice.CancelRecord();
    }

    public Image changeImg;

    public void onDragInVisual()
    {
        changeImg.SetSprite(AssetsPath.GetSpritesPath("NN/voice_recording_1.png"));
    }

    public void onDragOutVisual()
    {
        changeImg.SetSprite(AssetsPath.GetSpritesPath("NN/voice_recording_2.png"));
    }

    #region 比赛相关功能
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
            varMatchTImeText.transform.parent.gameObject.SetActive(zjh_procedure.enterZjhDeskRs.BaseConfig.DeskType == DeskType.Match);
            varMatchTImeText.transform.parent.gameObject.SetActive(zjh_procedure.enterZjhDeskRs.BaseConfig.DeskType == DeskType.Match);
            varMatchTImeText.text = "00:00";
            long m_ServerTime = Util.GetServerTime();
            long m_RemainTime = matchs.EndTime - m_ServerTime;

            if (m_RemainTime > 0)
            {
                m_RemainTime /= 1000; // 转换为秒
                int m_Minutes = (int)(m_RemainTime / 60);
                int m_Seconds = (int)(m_RemainTime % 60);
                varMatchTImeText.text = string.Format("{0:D2}:{1:D2}", m_Minutes, m_Seconds);

                // 开启倒计时协程
                if (m_CountdownCoroutine != null)
                {
                    CoroutineRunner.Instance.StopCoroutine(m_CountdownCoroutine);
                }
                m_CountdownCoroutine = CoroutineRunner.Instance.StartCoroutine(UpdateMatchCountdown(matchs.EndTime));
            }
            else
            {
                varMatchTImeText.text = "00:00";
            }
        }
        else
        {
            varMatchTImeText.transform.parent.gameObject.SetActive(false);
        }
    }

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
                varMatchTImeText.text = "00:00";
                break;
            }

            m_RemainTime /= 1000; // 转换为秒
            int m_Minutes = (int)(m_RemainTime / 60);
            int m_Seconds = (int)(m_RemainTime % 60);
            varMatchTImeText.text = string.Format("{0:D2}:{1:D2}", m_Minutes, m_Seconds);

            yield return new WaitForSeconds(1.0f);
        }
    }

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
                if (zjh_procedure.enterZjhDeskRs.BaseConfig.MatchId > 0)
                {
                    // 更新当前底注级别
                    zjh_procedure.enterZjhDeskRs.BaseConfig.BaseCoin = _nextBaseCoin;

                    // 更新房间信息显示
                    varRoominfo.text = "底注: " + zjh_procedure.enterZjhDeskRs.BaseConfig.BaseCoin +
                                      "  必闷: " + zjh_procedure.enterZjhDeskRs.Config.MustStuffy +
                                      "  比牌圈数: " + zjh_procedure.enterZjhDeskRs.Config.CompareRound +
                                      "\n单注上限: " + (zjh_procedure.enterZjhDeskRs.Config.SingleCoin <= 0 ? "无上限" : zjh_procedure.enterZjhDeskRs.Config.SingleCoin.ToString()) +
                                      "\n底池封顶: " + (zjh_procedure.enterZjhDeskRs.Config.MaxPot <= 0 ? "无上限" : zjh_procedure.enterZjhDeskRs.Config.MaxPot.ToString()) +
                                      (zjh_procedure.enterZjhDeskRs.Config.Use235 ? "\n235大三条" : "") +
                                      (zjh_procedure.enterZjhDeskRs.BaseConfig.IpLimit && zjh_procedure.enterZjhDeskRs.BaseConfig.GpsLimit ? "\nIP/GPS限制" :
                                      (zjh_procedure.enterZjhDeskRs.BaseConfig.IpLimit ? "\nIP限制" : "") +
                                      (zjh_procedure.enterZjhDeskRs.BaseConfig.GpsLimit ? "\nGPS限制" : "")) +
                                      (zjh_procedure.enterZjhDeskRs.Config.BigLuckCard ? "\n大牌奖励" : "") +
                                      (zjh_procedure.enterZjhDeskRs.Config.IsCompareDouble ? "\n双倍比牌" : "") +
                                      (zjh_procedure.enterZjhDeskRs.Config.ShotCard ? "\n短牌模式" : "") +
                                      (zjh_procedure.enterZjhDeskRs.Config.Stuffy ? "\n禁止闷比" : "");
                    LayoutRebuilder.ForceRebuildLayoutImmediate(varRoominfo.GetComponent<RectTransform>());

                    // 查找下一级底注
                    var addCoinTable = GlobalManager.GetInstance().m_AddCoinTable;
                    var matchs = zjh_procedure.MatchInfo;

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

    public void SynMatchLose(Msg_SynMatchLose ack)
    {
        if (ack.Match.MatchId == zjh_procedure.MatchInfo.MatchId)
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
                    if (zjh_procedure == null) return;
                    zjh_procedure.Send_LeaveDeskRq();
                });
            });
        }
    }
    #endregion
}

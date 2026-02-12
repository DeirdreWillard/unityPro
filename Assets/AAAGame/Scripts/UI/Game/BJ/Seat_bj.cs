﻿using NetMsg;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using Google.Protobuf;
using DG.Tweening;
using System.Collections;
using UGUI.Extend;
using Spine.Unity;

public enum SeatPosType
{
    Top,
    Down,
    Right,
    Left,
}

public class Seat_bj : MonoBehaviour
{
    //座位的ID 怎么旋转座位的ID不变
    public int SeatId { get; set; }
    // 座位所在的位置 ,仅用于转桌动画
    public int SeatIndex { get; set; }
    public bool IsPlayBack { get; set; } = false;
    public SeatPosType SeatType { get; set; } = SeatPosType.Down;
    public Vector3 Pos { get; set; } = new Vector3(0, 0, 0); // 将 Pos 初始化为 (0, 0, 0)
    public DeskPlayer playerInfo;

    public GameObject player;
    public GameObject empty;
    public GameObject vip;
    public RawImage avatar;
    public GameObject voice;
    public Text Txtname;
    public Text scoreValue;
    public GameObject WinChipValue;
    public Text WinChipValueText;
    public GameObject lizuo;
    public Text lizuoTimeText;
    public Text liuzuoText;
    public GameObject handCardsObj;
    public Image[] handCards;
    public GameObject compareCardsObj;
    public ShowCardAniScript[] compareCards;
    public Image give;
    public Image luckImg;
    public GameObject luckTypeObj;
    public Text luckScore;
    public Text changeScore;
    public GameObject changeScoreBg;
    public SkeletonAnimation WinnerStarEff;
    public SkeletonAnimation LongAni;

    public int[] handCardDatas;


    public void Awake()
    {
        // Find and assign GameObjects
        player = transform.Find("Player").gameObject;
        empty = transform.Find("Empty").gameObject;
        vip = player.transform.Find("vip").gameObject;
        avatar = player.transform.Find("mask/avatar").GetComponent<RawImage>();
        voice = transform.transform.Find("voice").gameObject;
        Txtname = player.transform.Find("name").GetComponent<Text>();
        scoreValue = player.transform.Find("scoreImage/value").GetComponent<Text>();
        WinChipValue = transform.transform.Find("WinChipBg").gameObject;
        WinChipValueText = transform.transform.Find("WinChipBg/value").GetComponent<Text>();
        lizuo = player.transform.Find("lizuo").gameObject;
        lizuoTimeText = lizuo.transform.Find("Time").GetComponent<Text>();
        liuzuoText = lizuo.transform.Find("liuzuoText").GetComponent<Text>();
        handCardsObj = transform.transform.Find("HandCards").gameObject;
        handCards = handCardsObj.GetComponentsInChildren<Image>().Where(go => go.name == "Card").ToArray();
        compareCardsObj = transform.Find("CompareCards").gameObject;
        compareCards = compareCardsObj.GetComponentsInChildren<ShowCardAniScript>();
        give = compareCardsObj.transform.Find("Give").GetComponent<Image>();
        luckImg = transform.Find("CompareCards/LuckImg").GetComponent<Image>();
        luckTypeObj = transform.Find("CompareCards/LuckTypeObj").gameObject;
        luckScore = transform.transform.Find("CompareCards/LuckScore").GetComponent<Text>();
        changeScore = transform.Find("CompareCards/ChangeScore").GetComponent<Text>();
        changeScoreBg = transform.Find("CompareCards/ChangeScoreBg").gameObject;
        LongAni = transform.Find("CompareCards/LongAni").GetComponent<SkeletonAnimation>();
        WinnerStarEff = transform.Find("WinnerStarEff").GetComponent<SkeletonAnimation>();
        player.transform.GetComponent<Button>().onClick.RemoveAllListeners();
        player.transform.GetComponent<Button>().onClick.AddListener(OpenPlayerInfoPanel);
        empty.GetComponent<Button>().onClick.RemoveAllListeners();
        empty.GetComponent<Button>().onClick.AddListener(SitDownClick);
        ReSetScale();
    }

    public void ReSetScale()
    {
        float scale = GameConstants.GetScaleByScreenSize(1.7f);
        empty.transform.localScale = Vector3.one * scale;
        player.transform.localScale = Vector3.one * scale;
        WinChipValue.transform.localScale = Vector3.one * scale;
        handCardsObj.transform.localScale = Vector3.one * GameConstants.GetScaleByScreenSize(1f);
    }

    public void ResetPos()
    {
        handCardsObj.transform.localPosition = GameConstants.GetWidgetPosition(1, SeatType);
        compareCardsObj.transform.localPosition = GameConstants.GetWidgetPosition(2, SeatType);
        bool isLeft = SeatType != SeatPosType.Right;
        foreach (var card in compareCards)
        {
            card.InitPanel(isLeft);
        }
        luckScore.transform.localPosition = isLeft ? new Vector2(200, -100) : new Vector2(-200, -100);
        luckScore.alignment = isLeft ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
    }

    public void SitDownClick()
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        //Msg_SitDownRq
        var bJProcedure = GF.Procedure.GetProcedure<BJProcedure>() as BJProcedure;
        if (bJProcedure.GetSelfPlayer() != null)
        {
            return;
        }
        if (!bJProcedure.enterBJDeskRs.BaseInfo.BaseConfig.GpsLimit)
        {
            Msg_SitDownRq req = MessagePool.Instance.Fetch<Msg_SitDownRq>();
            req.Pos = GetPosition();
            req.Gps = "0,0";
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_SitDownRq, req);
            return;
        }
        CoroutineRunner.Instance.RunCoroutine(GetGPSAndSendSitDownRequest(GetPosition()));
    }


    private IEnumerator GetGPSAndSendSitDownRequest(Position pos)
    {
        // 使用通用GPS获取方法
        return Util.GetGPSLocation((gpsString) =>
        {
            // 获取位置信息成功，发送请求
            Msg_SitDownRq req = MessagePool.Instance.Fetch<Msg_SitDownRq>();
            req.Pos = pos;
            req.Gps = gpsString;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_SitDownRq, req);
        });
    }

    public Position GetPosition()
    {
        Position pos = Position.Default;
        switch (SeatId)
        {
            case 0:
                pos = Position.Default;
                break;
            case 1:
                pos = Position.One;
                break;
            case 2:
                pos = Position.Two;
                break;
            case 3:
                pos = Position.Three;
                break;
            case 4:
                pos = Position.Four;
                break;
            case 5:
                pos = Position.Five;
                break;
            case 6:
                pos = Position.Six;
                break;
            case 7:
                pos = Position.Seven;
                break;
            case 8:
                pos = Position.Eight;
                break;
            case 9:
                pos = Position.Nine;
                break;
            default:
                break;
        }
        return pos;
    }

    public async void OpenPlayerInfoPanel()
    {
        if (Util.IsClickLocked()) return;
        var bJProcedure = GF.Procedure.GetProcedure<BJProcedure>() as BJProcedure;
        //理牌中不让点击
        if (bJProcedure.GamePanel_bj.varLocalOP.localMask.activeSelf) return;
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("deskPlayer", playerInfo.ToByteArray());
        uiParams.Set<VarBoolean>("isRoomCreate", bJProcedure.GamePanel_bj.IsRoomCreate());
        await GF.UI.OpenUIFormAwait(UIViews.playerInforPanel, uiParams);
    }
    public void Init(DeskPlayer deskPlaye)
    {
        playerInfo = deskPlaye;
        handCardDatas = deskPlaye.HandCard.ToArray();
        Txtname.FormatNickname(playerInfo.BasePlayer.Nick);
        Util.DownloadHeadImage(avatar, playerInfo.BasePlayer.HeadImage);
        SetScore(float.Parse(deskPlaye.Coin));
        vip.SetActive(playerInfo.BasePlayer.Vip != 0);
        empty.SetActive(false);
        player.SetActive(true);
        Function_SynPlayerState(playerInfo.State, deskPlaye.LeaveTime);
    }

    public void UpdatePlayerInfo(DeskPlayer deskPlayer)
    {
        playerInfo = deskPlayer;
        SetScore(float.Parse(deskPlayer.Coin));
    }

    public void SetScore(float score)
    {
        playerInfo.Coin = score.ToString();
        scoreValue.text = Util.FormatAmount(score);
    }

    private Sequence cardWaveSequence;

    public void ShowHandCards(bool ani = true)
    {
        handCardsObj.transform.localPosition = GameConstants.GetWidgetPosition(1, SeatType);
        handCardsObj.transform.Find("lipai").gameObject.SetActive(false);
        handCardsObj.SetActive(true);

        // 停止之前的动画
        cardWaveSequence?.Kill();

        if (ani)
        {
            cardWaveSequence = DOTween.Sequence();
            cardWaveSequence.SetTarget(gameObject);

            // 初始透明度设为0并渐显
            Sequence fadeInSequence = DOTween.Sequence();
            fadeInSequence.SetTarget(gameObject);
            foreach (var (card, index) in handCards.Select((card, index) => (card, index)))
            {
                card.color = new Color(1, 1, 1, 0);
                fadeInSequence.Join(
                    DOTween.To(() => card.color, x => card.color = x, new Color(1, 1, 1, 1), 0.03f)
                        .SetDelay(index * 0.02f)
                        .SetEase(Ease.InQuad)
                );
            }
            fadeInSequence.OnComplete(() => StartWaveAnimation());
        }
        else
        {
            StartWaveAnimation();
        }

        // 渐显完成后开始波浪动画
    }

    public void StartWaveAnimation()
    {
        handCardsObj.transform.Find("lipai").gameObject.SetActive(true);
        float waveHeight = 20f;
        float duration = 0.2f;
        float delayBetween = 0.1f;

        // 重置位置
        foreach (var card in handCards)
        {
            card.transform.localPosition = new Vector3(
                card.transform.localPosition.x,
                0,
                card.transform.localPosition.z);
        }

        // 创建完整波浪动画
        var fullWave = DOTween.Sequence();
        fullWave.SetTarget(gameObject);

        foreach (var (card, index) in handCards.Select((card, index) => (card, index)))
        {
            // 单个卡牌动画（使用相对移动和Yoyo循环）
            var cardTween = card.transform.DOLocalMoveY(waveHeight, duration)
                .SetEase(Ease.OutQuad)
                .SetLoops(2, LoopType.Yoyo) // 往返运动
                .SetDelay(delayBetween)
                .SetRelative(); // 使用相对位移

            fullWave.Join(cardTween);
        }

        // 设置整体循环（完成后重置）
        cardWaveSequence = DOTween.Sequence()
            .SetTarget(gameObject)
            .Append(fullWave)
            .AppendCallback(() =>
            {
                // 重置所有卡牌位置
                foreach (var card in handCards)
                {
                    card.transform.localPosition = new Vector3(
                        card.transform.localPosition.x,
                        0,
                        card.transform.localPosition.z);
                }
            })
            .SetLoops(-1)
            .SetDelay(0.6f)
            .Play();
    }

    private void ResetCardsPosition()
    {
        if (handCardsObj == null) return;

        cardWaveSequence?.Kill();

        foreach (Image card in handCards)
        {
            Vector3 pos = card.transform.localPosition;
            card.transform.localPosition = new Vector3(pos.x, 0, pos.z);
        }
    }

    public void HideHandCards()
    {
        ResetCardsPosition();
        handCardsObj.SetActive(false);
    }

    public void PlayCardAniEverySecondNoAni(CompareChickenSettle settle)
    {
        ShowCompareCards();
        give.gameObject.SetActive(settle.IsGive);
        ChickenCards[] chickenCards = settle.ChickenCards.ToArray();
        for (int i = 0; i < chickenCards.Length; i++)
        {
            int index = i;
            compareCards[index].PlayAni(chickenCards[index], 0);
        }
        if (settle.LuckCard != null && settle.LuckCard.Count > 0)
        {
            ShowLuckType(settle.LuckCard.ToArray());
        }
        ColorFader luckColorFader = luckScore.GetComponent<ColorFader>();
        ColorFader changeColorFader = changeScore.GetComponent<ColorFader>();
        float luckCoin = float.Parse(settle.LuckCoin);
        float coinChange = float.Parse(settle.CoinChange);
        if (luckCoin > 0)
        {
            Color32[] colors_luck = GameConstants.GetColorsByResult(luckCoin > 0);
            luckColorFader.color1 = colors_luck[0];
            luckColorFader.color2 = colors_luck[1];
            luckScore.transform.localScale = Vector3.one;
            luckScore.text = $"喜分{Util.FormatRichText(luckCoin)}";
        }
        else
        {
            luckScore.text = "";
        }
        Color32[] colors_changeColor = GameConstants.GetColorsByResult(coinChange > 0);
        changeColorFader.color1 = colors_changeColor[0];
        changeColorFader.color2 = colors_changeColor[1];
        changeScore.transform.localScale = Vector3.one;
        changeScore.text = $"总计{Util.FormatRichText(coinChange)}";
        changeScoreBg.SetActive(true);
    }

    public void PlayCompareCards(CompareChickenSettle settle, int SettleAnimation)
    {
        ShowCompareCards();
        if (settle.IsGive || settle.ChickenCards.Count == 0)
        {
            // 创建一个从2倍缩小到1倍的动画
            give.transform.localScale = Vector3.one * 2f;
            give.gameObject.SetActive(true);
            give.transform.DOScale(Vector3.one, 0.5f)
                .SetEase(Ease.OutBack);
        }
        else
        {
            give.gameObject.SetActive(false);
            if (SettleAnimation == 3)
            {
                PlayCardAniEverySecond3(settle, SettleAnimation);
            }
            else if (SettleAnimation == 2)
            {
                PlayCardAniEverySecond2(settle, SettleAnimation);
            }
        }
    }

    private void PlayCardAniEverySecond3(CompareChickenSettle settle, int SettleAnimation)
    {
        //2s
        ChickenCards[] chickenCards = settle.ChickenCards.ToArray();
        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(gameObject);

        for (int i = 0; i < chickenCards.Length; i++)
        {
            int index = i;
            sequence.AppendCallback(() =>
            {
                compareCards[index].PlayAni(chickenCards[index], SettleAnimation);
            });
            sequence.AppendInterval(0.1f);
        }
        sequence.AppendInterval(1.5f);
        sequence.AppendCallback(() =>
        {
            ShowLuckCoin(settle);
        });

        sequence.Play();
    }

    private void PlayCardAniEverySecond2(CompareChickenSettle settle, int SettleAnimation)
    {
        //5s
        ChickenCards[] chickenCards = settle.ChickenCards.ToArray();
        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(gameObject);

        for (int i = 0; i < chickenCards.Length; i++)
        {
            int index = i;
            sequence.AppendCallback(() =>
            {
                compareCards[index].PlayAni(chickenCards[index], SettleAnimation);
            });
            sequence.AppendInterval(1.5f);
        }
        sequence.AppendCallback(() =>
        {
            ShowLuckCoin(settle);
        });

        sequence.Play();
    }

    public void ShowLuckType(int[] luckType)
    {
        bool isLeft = SeatType != SeatPosType.Right;
        luckTypeObj.transform.localPosition = isLeft ? new Vector2(240, -5) : new Vector2(-240, -5);
        luckTypeObj.GetComponent<GridLayoutGroup>().childAlignment = isLeft ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
        luckTypeObj.SetActive(true);
        for (int i = 0; i < luckTypeObj.transform.childCount; i++)
        {
            luckTypeObj.transform.GetChild(i).gameObject.SetActive(false);
        }
        for (int i = 0; i < luckType.Length; i++)
        {
            if (luckType[i] == 8)
            {
               Sound.PlayEffect("BJ/通关.mp3");
            }
            luckTypeObj.transform.GetChild(i).gameObject.SetActive(true);
            luckTypeObj.transform.GetChild(i).Find("Type").GetComponent<Text>().text = GameUtil.ShowType(luckType[i], MethodType.CompareChicken);
        }
    }

    public void ShowLuckCoin(CompareChickenSettle settle)
    {
        ColorFader luckColorFader = luckScore.GetComponent<ColorFader>();
        ColorFader changeColorFader = changeScore.GetComponent<ColorFader>();
        float luckCoin = float.Parse(settle.LuckCoin);
        float coinChange = float.Parse(settle.CoinChange);
        if (settle.LuckCard != null && settle.LuckCard.Count > 0)
        {
            ShowLuckType(settle.LuckCard.ToArray());
            Color32[] colors_luck = GameConstants.GetColorsByResult(luckCoin > 0);
            luckColorFader.color1 = colors_luck[0];
            luckColorFader.color2 = colors_luck[1];
            luckScore.transform.localScale = Vector3.one * 2f;
            luckScore.text = $"喜分{Util.FormatRichText(luckCoin)}";
            if (LongAni.gameObject.activeSelf == false) LongAni.gameObject.SetActive(true);
            LongAni.AnimationState.SetAnimation(0, "long", false);
            luckScore.transform.DOScale(Vector3.one, 0.5f)
                .SetEase(Ease.OutBack);
        }
        else
        {
            luckScore.text = "";
        }
        Color32[] colors_changeColor = GameConstants.GetColorsByResult(coinChange > 0);
        changeColorFader.color1 = colors_changeColor[0];
        changeColorFader.color2 = colors_changeColor[1];
        changeScore.transform.localScale = Vector3.one * 2.5f;
        changeScore.text = $"总计{Util.FormatRichText(coinChange)}";
        changeScore.transform.DOScale(Vector3.one, 0.5f)
            .SetEase(Ease.OutCubic).OnPlay(() =>
            {
                if (coinChange > 0)
                {
                   Sound.PlayEffect("BJ/赢牌音效.mp3");
                }
                else if (coinChange < 0)
                {
                   Sound.PlayEffect("BJ/输牌音效.mp3");
                }
            });
        changeScoreBg.SetActive(true);
    }

    public void ShowSettleCoinChange(float coinChange, float realWin)
    {
        // GF.LogInfo("coinChange" , coinChange + "   name:" + playerInfo.BasePlayer.Nick);
        SwitchWinChips(true, coinChange);
        SetScore(float.Parse(playerInfo.Coin) + realWin);
    }

    public void ShowCompareCards()
    {
        if (compareCardsObj.activeSelf == true) return;
        HideHandCards();
        HideCompareCards();
        compareCardsObj.transform.localPosition = GameConstants.GetWidgetPosition(2, SeatType);
        bool isLeft = SeatType != SeatPosType.Right;
        foreach (var card in compareCards)
        {
            card.InitPanel(isLeft);
        }
        luckScore.transform.localPosition = isLeft ? new Vector2(200, -100) : new Vector2(-200, -100);
        luckScore.alignment = isLeft ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
        compareCardsObj.SetActive(true);
    }

    public void HideCompareCards()
    {
        foreach (var card in compareCards)
        {
            card.Hide();
        }
        luckScore.text = "";
        changeScore.text = "";
        changeScoreBg.SetActive(false);
        luckImg.gameObject.SetActive(false);
        give.gameObject.SetActive(false);
        compareCardsObj.SetActive(false);
    }

    /// <summary>
    /// 重置用户UI(回放)
    /// </summary>
    public void ResetPlayer_Record()
    {
        SwitchWinChips(false);
        HideHandCards();
        HideCompareCards();
        LongAni.gameObject.SetActive(false);
        WinnerStarEff.gameObject.SetActive(false);
        luckTypeObj.SetActive(false);
        handCardDatas = new int[0];
        if (playerInfo != null)
        {
            playerInfo.IsLook = false;
            playerInfo.IsGive = false;
            playerInfo.AutoFollow = false;
            playerInfo.AutoGive = false;
        }
    }

    /// <summary>
    /// 单论结束 重置用户UI
    /// </summary>
    public void ResetPlayer()
    {
        SwitchWinChips(false);
        HideHandCards();
        HideCompareCards();
        LongAni.gameObject.SetActive(false);
        WinnerStarEff.gameObject.SetActive(false);
        luckTypeObj.SetActive(false);
        handCardDatas = new int[0];
        if (IsNotEmpty())
        {
            playerInfo.IsLook = false;
            playerInfo.IsGive = false;
            playerInfo.AutoFollow = false;
            playerInfo.AutoGive = false;
        }
    }

    public void ClearPlayer()
    {
        empty.SetActive(true);
        voice.SetActive(false);
        player.SetActive(false);
        SwitchWinChips(false);
        HideHandCards();
        HideCompareCards();
        LongAni.gameObject.SetActive(false);
        WinnerStarEff.gameObject.SetActive(false);
        luckTypeObj.SetActive(false);
        vip.SetActive(false);
        Txtname.text = "";
        scoreValue.text = "";
        IsPlayBack = false;
        playerInfo = null;
        handCardDatas = new int[0];
    }

    /// <summary>
    /// 结算获得
    /// </summary>
    public void SwitchWinChips(bool isShow, float coinChange = 0)
    {
        if (playerState == PlayerState.OffLine)
            return;
        if (isShow)
        {
            WinChipValue.transform.Find("Loss").gameObject.SetActive(coinChange < 0);
            WinChipValue.transform.Find("Win").gameObject.SetActive(coinChange > 0);
            WinChipValueText.FormatRichText(coinChange, "#000000", "#000000");
            if (coinChange > 0)
            {
                if (WinnerStarEff.gameObject.activeSelf == false) WinnerStarEff.gameObject.SetActive(true);
                WinnerStarEff.AnimationState.SetAnimation(0, "animation", false);
            }
        }
        WinChipValue.SetActive(isShow);
    }

    PlayerState playerState = PlayerState.OnLine;
    private float leaveTime;
    /// <summary>
    /// 留座离桌
    /// </summary>
    public void Function_SynPlayerState(PlayerState playerState, long leaveTime)
    {
        if (leaveTime <= 0)
        {
            lizuo.SetActive(false);
            return;
        }
        lizuo.SetActive(true);
        this.leaveTime = leaveTime;
        switch (playerState)
        {
            case PlayerState.OnLine:
                liuzuoText.text = "";
                break;
            case PlayerState.OffLine:
                liuzuoText.text = "离桌离场";
                playerInfo.InGame = false;
                lizuoTimeText.text = ((int)leaveTime).ToString(); // 更新文本显示
                break;
            case PlayerState.WaitBring:
                liuzuoText.text = "等待带入";
                playerInfo.InGame = false;
                lizuoTimeText.text = ((int)leaveTime).ToString(); // 更新文本显示
                break;
        }
    }

    public void Update()
    {
        // if (playerState == PlayerState.OffLine)
        // {
        if (leaveTime > 0)
        {
            // GF.LogInfo("playerState" + leaveTime);
            leaveTime -= Time.deltaTime; // 每帧减少时间
            lizuoTimeText.text = ((int)leaveTime).ToString(); // 更新文本显示
        }
        // }
    }

    public bool IsNotEmpty()
    {
        return playerInfo != null;
    }


    public void SetPos(Vector3 vec3, bool ani, Vector2[] anchors)
    {
        // GF.LogError("anchors " + anchors[0] + " " + anchors[1] + "SeatIndex  " + SeatIndex + "SeatId  " + SeatId);
        // GF.LogError("vec3 " + vec3);
        Pos = vec3;
        RectTransform rectTransform = transform.GetComponent<RectTransform>();

        // 记录原始世界空间位置
        Vector3 worldPos = rectTransform.position;

        // 设置锚点
        rectTransform.anchorMin = anchors[0];
        rectTransform.anchorMax = anchors[1];

        // 修正位置：将世界空间位置转换为新的锚点下的 anchoredPosition
        rectTransform.position = worldPos;
        if (!ani)
            rectTransform.anchoredPosition = vec3;
        else
        {
            // 使用 DOTween 的回调来逐帧插值 RectTransform 的 anchoredPosition
            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 targetPos = vec3;
            float duration = 0.5f;

            DOTween.To(() => startPos, x => rectTransform.anchoredPosition = x, targetPos, duration).OnComplete(() =>
            {
                rectTransform.anchoredPosition = vec3;
                ResetPos();
            }); ;
        }
        // GF.LogError("index" + SeatIndex + "SeatId" + SeatId);
    }


    public void PlayerEnter(DeskPlayer deskPlaye, bool isplayback = false)
    {
        IsPlayBack = isplayback;
        Init(deskPlaye);
    }

    /// <summary>
    /// 站起
    /// </summary>
    public void Function_SynSitUp()
    {
        if (Util.IsMySelf(playerInfo.BasePlayer.PlayerId))
        {
            if (GF.Procedure.CurrentProcedure is BJProcedure)
            {
                BJProcedure bJProcedure = GF.Procedure.CurrentProcedure as BJProcedure;
                // bJProcedure.GamePanel_bj.SwitchSelfSeat(true);
                bJProcedure.GamePanel_bj.ShowBtnCancelLiuZuo(false);
            }
        }
        ClearPlayer();
    }

}

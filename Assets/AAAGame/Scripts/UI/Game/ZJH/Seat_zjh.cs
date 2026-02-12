using NetMsg;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static UtilityBuiltin;
using UnityGameFramework.Runtime;
using Google.Protobuf;
using static GameConstants;
using DG.Tweening;
using System.Collections;
using System;
using Spine.Unity;

public class Seat_zjh : MonoBehaviour
{
    //座位的ID 怎么旋转座位的ID不变
    public int SeatId{ get; set; }
    // 座位所在的位置 ,仅用于转桌动画
    public int SeatIndex{ get; set; }
    public bool IsPlayBack{ get; set; } = false;
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
    public Text chipValue;
    public GameObject lizuo;
    public Text lizuoTimeText;
    public Text liuzuoText;
    public Image operateType;
    public GameObject hideCards;
    public GameObject signLookCard;
    public GameObject handCards;
    public Image spCardType;
    public GameObject btnPK;
    public SkeletonAnimation WinnerStarEff;

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
        hideCards = player.transform.Find("HideCards").gameObject;
        signLookCard = player.transform.Find("signLookCard").gameObject;
        handCards = transform.transform.Find("HandCards").gameObject;
        btnPK = player.transform.Find("btnPK").gameObject;
        spCardType = transform.transform.Find("spCardType").GetComponent<Image>();
        chipValue =transform.Find("chipImg/value").GetComponent<Text>();
        operateType = transform.Find("operateType").GetComponent<Image>();
        WinnerStarEff = transform.Find("WinnerStarEff").GetComponent<SkeletonAnimation>();
        player.transform.GetComponent<Button>().onClick.RemoveAllListeners();
        player.transform.GetComponent<Button>().onClick.AddListener(OpenPlayerInfoPanel);
        empty.GetComponent<Button>().onClick.RemoveAllListeners();
        empty.GetComponent<Button>().onClick.AddListener(SitDownClick);
        btnPK.GetComponent<Button>().onClick.RemoveAllListeners();
        btnPK.GetComponent<Button>().onClick.AddListener(PkClick);
        ReSetScale();
    }

    public void ReSetScale(){
        float scale = GameConstants.GetScaleByScreenSize(1.7f);
        empty.transform.localScale = Vector3.one * scale;
        player.transform.localScale = Vector3.one * scale;
        operateType.transform.localScale = Vector3.one * GameConstants.GetScaleByScreenSize(1.5f);
        WinChipValue.transform.localScale = Vector3.one * scale;
        handCards.transform.localScale = Vector3.one * GameConstants.GetScaleByScreenSize(1.5f);
    }

    public void PkClick(){
        if(IsPlayBack){return;}
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        var zjh_procedure = GF.Procedure.GetProcedure<ZJHProcedure>() as ZJHProcedure;
        zjh_procedure.Send_OptionRq(ZjhOption.Compare, false, GetPosition());
    }

    public void SitDownClick()
    {
        if(IsPlayBack){return;}
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        //Msg_SitDownRq
        var zjh_procedure = GF.Procedure.GetProcedure<ZJHProcedure>() as ZJHProcedure;
        if(zjh_procedure.GetSelfPlayer() != null)
        {
            return;
        }
        if(!zjh_procedure.enterZjhDeskRs.BaseConfig.GpsLimit){
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
        return Util.GetGPSLocation((gpsString) => {
            // 获取位置信息成功，发送请求
            Msg_SitDownRq req = MessagePool.Instance.Fetch<Msg_SitDownRq>();
            req.Pos = pos;
            req.Gps = gpsString;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_SitDownRq, req);
        });
    }

    public Position GetPosition(){
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
        if(IsPlayBack){return;}
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("deskPlayer", playerInfo.ToByteArray());
        var zjhProcedure = GF.Procedure.GetProcedure<ZJHProcedure>() as ZJHProcedure;
        uiParams.Set<VarBoolean>("isRoomCreate", zjhProcedure.GamePanel_zjh.IsRoomCreate());
        await GF.UI.OpenUIFormAwait(UIViews.playerInforPanel, uiParams);
    }
    public void Init(DeskPlayer deskPlayer)
    {
        playerInfo = deskPlayer;
        Txtname.FormatNickname(playerInfo.BasePlayer.Nick);
        Util.DownloadHeadImage(avatar, playerInfo.BasePlayer.HeadImage);
        ScoreChange(float.Parse(deskPlayer.Coin));
        vip.SetActive(playerInfo.BasePlayer.Vip != 0);
        chipValue.text = "" + playerInfo.BetCoin;
        signLookCard.SetActive(playerInfo.IsLook);
        SwitchHideCards(playerInfo.InGame && playerInfo.HandCard != null && playerInfo.HandCard.Count == 3, playerInfo.IsLook ? -2 : -1);
        SetHideCardsColor((playerInfo.IsGive || playerInfo.CompareLose) ? Color.grey : Color.white);
        ShowHandCards(playerInfo.HandCard.ToArray(),(ZjhCardType)playerInfo.Niu);
        empty.SetActive(false);
        player.SetActive(true);
        Function_SynPlayerState(playerInfo.State, deskPlayer.LeaveTime);
    }

    public void UpdatePlayerInfo(DeskPlayer deskPlayer, bool isRefresh = true){
        playerInfo = deskPlayer;
        ScoreChange(float.Parse(deskPlayer.Coin), isRefresh);
    }

    public Vector3 GetChipPos(){
        return chipValue.transform.parent.localPosition + transform.localPosition;
    }

    public void ScoreChange(float score, bool isRefresh = true)
    {
        SetCoin(score);
        if (isRefresh)
            scoreValue.text = Util.FormatAmount(score);
    }

    public float GetCoin(){
        return float.Parse(playerInfo.Coin);
    }
    public void SetCoin(float coin)
    {
        playerInfo.Coin = coin.ToString();
    }

    public void HideHandCards()
    {
        for (int i = 0; i < 3; i++)
        {
            Transform card = handCards.transform.GetChild(i);
            card.GetComponent<Card>().Init(-1);
        }
        handCards.SetActive(false);
    }

    public void ShowHandCards(int[] handCardsData, ZjhCardType cardType)
    {
        if (handCardsData == null || handCardsData.Length == 0 || handCardsData[0] == -1)
        {
            HideHandCards();
            return;
        }
        handCards.SetActive(true);
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            Transform card = handCards.transform.GetChild(index);
            card.transform.DORotate(new Vector3(0, 90, 0), 0.2f).OnComplete(() =>
            {
                card.GetComponent<Card>().Init(handCardsData[index]);
                card.transform.DORotate(Vector3.zero, 0.2f).OnComplete(() =>
                {
                    if (index == 2 && !handCardsData.Contains(-1))
                    {
                        SwitchCardTypeImg(true, cardType);
                    }
                });
            });
        }
    }

    /// <summary>
    /// 自行亮牌
    /// </summary>
    /// <param name="handCardsData"></param>
    /// <param name="cardType"></param>
     public void ShowHandCardsBySelf(int[] handCardsData)
    {
        if (handCardsData == null || handCardsData.Length == 0)
        {
            return;
        }
        handCards.SetActive(true);
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            if (handCardsData.Length > index)
            {
                Card card = handCards.transform.GetChild(index).GetComponent<Card>();
                card.Init(handCardsData[index]);
            }
        }
    }

    public void SwitchCardTypeImg(bool isShow, ZjhCardType cardType)
    {
        if (isShow)
        {
            spCardType.SetSprite(GameConstants.GetCardTypeString(cardType), true);
            spCardType.gameObject.SetActive(true);
        }
        else
        {
            spCardType.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 重置用户UI(回放)
    /// </summary>
    public void ResetPlayer_Record()
    {
        WinChipValue.SetActive(false);
        WinnerStarEff.gameObject.SetActive(false);
        SwitchChipObj(false);
        operateType.gameObject.SetActive(false);
        hideCards.SetActive(true);
        SwitchHideCards(true, -1);
        signLookCard.SetActive(false);
        HideHandCards();
        SwitchCardTypeImg(false, ZjhCardType.High);
        SetHideCardsColor(Color.white);
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
        WinChipValue.SetActive(false);
        WinnerStarEff.gameObject.SetActive(false);
        SwitchChipObj(false);
        operateType.gameObject.SetActive(false);
        hideCards.SetActive(true);
        SwitchHideCards(false, -1);
        signLookCard.SetActive(false);
        HideHandCards();
        SwitchCardTypeImg(false, ZjhCardType.High);
        btnPK.SetActive(false);
        SetHideCardsColor(Color.white);
        if (IsNotEmpty())
        {
            playerInfo.InGame = false;
            playerInfo.IsLook = false;
            playerInfo.IsGive = false;
            playerInfo.IsBanker = false;
            playerInfo.AutoFollow = false;
            playerInfo.AutoGive = false;
            playerInfo.HandCard.Clear();
        }
    }

    public void ClearPlayer(){
        empty.SetActive(true);
        voice.SetActive(false);
        player.SetActive(false);
        WinChipValue.SetActive(false);
        WinnerStarEff.gameObject.SetActive(false);
        SwitchChipObj(false);
        operateType.gameObject.SetActive(false);
        hideCards.SetActive(true);
        SwitchHideCards(false, -1);
        signLookCard.SetActive(false);
        HideHandCards();
        SwitchCardTypeImg(false, ZjhCardType.High);
        SetHideCardsColor(Color.white);
        btnPK.SetActive(false);
        vip.SetActive(false);
        Txtname.text = "";
        scoreValue.text = "";
        IsPlayBack = false;
        playerInfo = null;
    }

    public void SwitchHideCards(bool isShow, int cardNum)
    {
        foreach (Transform item in hideCards.transform)
        {
            item.gameObject.GetComponent<Card>().Init(cardNum);
            item.gameObject.SetActive(isShow);
        }
    }

    public void SetHideCardsColor(Color color){
        if (color == Color.gray)
        {
            signLookCard.SetActive(false);
        }
        foreach (Transform item in hideCards.transform)
        {
            item.gameObject.GetComponent<Card>().SetImgColor(color);
        }
    }

    /// <summary>
    /// 显示操作信息
    /// </summary>
    /// <param name="option"></param>
    /// <param name="isWin"></param>
    public void ShowOperateImage(ZjhOption option, bool isWin = false)
    {
        Vector3 position = GameConstants.GetWidgetPosition(playerInfo.BasePlayer.PlayerId, WidgetType.Operate, transform.localPosition, IsPlayBack);
        string imgStr = GameConstants.GetOperateImgString(playerInfo.BasePlayer.PlayerId, option, transform.localPosition, isWin, IsPlayBack);
        GF.UI.LoadSprite(AssetsPath.GetSpritesPath(imgStr), (sprite) =>
        {
            if (operateType == null) return;
            operateType.name = option.ToString();
            operateType.sprite = sprite;
            operateType.SetNativeSize();
            operateType.transform.localPosition = position;
            operateType.gameObject.SetActive(true);
        });
    }

    /// <summary>
    /// 收到消息下注
    /// </summary>
    public void ShowBetCoin(float betCoin)
    {
        chipValue.text = betCoin.ToString();
        SwitchChipObj(true);
        Vector3 chipPos = GetWidgetPosition(playerInfo.BasePlayer.PlayerId, WidgetType.DownChip, transform.localPosition, IsPlayBack);
        chipValue.transform.parent.localPosition = chipPos;
    }

    public void SwitchChipObj(bool isShow)
    {
        chipValue.transform.parent.gameObject.SetActive(isShow);
    }

    /// <summary>
    /// 结算获得
    /// </summary>
    public void SwitchWinChips(bool isShow,float coinChange)
    {
        if (playerState == PlayerState.OffLine)
            return;
        WinChipValue.SetActive(isShow);
        if (isShow)
        {
            WinChipValue.transform.Find("Loss").gameObject.SetActive(coinChange < 0);
            WinChipValue.transform.Find("Win").gameObject.SetActive(coinChange > 0);
            WinChipValueText.FormatRichText(coinChange, "#000000", "#000000");
            //playerInfo.Coin = (float.Parse(playerInfo.Coin) + coinChange).ToString(); // 更新金币
            if(coinChange > 0){
                if (WinnerStarEff.gameObject.activeSelf == false) WinnerStarEff.gameObject.SetActive(true);
                WinnerStarEff.AnimationState.SetAnimation(0, "animation", false);
            }
        }
    }

    void OnCoinAniFinish(GameObject go)
    {
        go.SetActive(false);
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
                lizuoTimeText.text = "";
                break;
            case PlayerState.OffLine:
                liuzuoText.text = "离桌留座";
                lizuoTimeText.text = ((int)leaveTime).ToString(); // 更新文本显示
                break;
            case PlayerState.WaitBring:
                liuzuoText.text = "等待带入";
                lizuoTimeText.text = ((int)leaveTime).ToString(); // 更新文本显示
                break;
        }
    }

    private float lastUpdateTime;

    public void Update()
    {
        if (Time.time - lastUpdateTime >= 1f)
        {
            if (leaveTime > 0)
            {
                // GF.LogInfo("playerState" + leaveTime);
                leaveTime -= 1f; // 每帧减少时间
                lizuoTimeText.text = ((int)leaveTime).ToString(); // 更新文本显示
            }
            lastUpdateTime = Time.time;
        }
    }

    public bool IsNotEmpty()
    {
        return playerInfo != null;
    }

    // public void SetPos(Vector3 vec3, bool ani)
    // {
    //     Pos = vec3;
    //     RectTransform rectTransform = transform.GetComponent<RectTransform>();

    //     if (!ani)
    //         rectTransform.anchoredPosition = vec3;
    //     else
    //         transform.DOLocalMove(vec3, 0.5f).OnComplete(() =>{
    //             ResetAnchors();
    //             rectTransform.anchoredPosition = vec3;
    //         });
    // }

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
        else{
            // 使用 DOTween 的回调来逐帧插值 RectTransform 的 anchoredPosition
            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 targetPos = vec3;
            float duration = 0.5f;

            DOTween.To(() => startPos, x => rectTransform.anchoredPosition = x, targetPos, duration).OnComplete(() =>{
                rectTransform.anchoredPosition = vec3;
            });
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
            if(GF.Procedure.CurrentProcedure is ZJHProcedure){
                ZJHProcedure zjhProcedure = GF.Procedure.CurrentProcedure as ZJHProcedure;
                zjhProcedure.GamePanel_zjh.SwitchSelfSeat(true);
                zjhProcedure.GamePanel_zjh.ShowBtnCancelLiuZuo(false);
            }
        }
        ClearPlayer();
    }

}

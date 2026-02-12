using UnityEngine;
using UnityEngine.UI;

using UnityGameFramework.Runtime;
using NetMsg;
using System.Collections.Generic;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class playerInforPanel : UIFormBase
{


    private DeskPlayer deskPlayer;
    private GameObject vipImage;
    private Text playerNameText;
    private Text playerIDText;//当前玩家ID
    private Text clubNameText;//当前俱乐部名称
    private RawImage avatar;
    private Toggle forbiddenSpeechToggle;
    private Button BtnKickOut;
    private Transform BtnGifs;
    private GameObject nan;
    private GameObject nv;
    public GameObject HorizontalPanel;//横板
    public GameObject VerticalPanel;//竖板
    bool init = true;
    bool isRoomCreate = false;

    public GameObject dzInfo;
    public Text dzHandSum;
    public List<GameObject> dzInfoList;

    private void InitUI()
    {
        // 通过屏幕宽高比判断横竖屏(与项目其他地方保持一致)
        bool isLandscape = GlobalManager.IsLandscape();
        if (HorizontalPanel != null) HorizontalPanel.SetActive(isLandscape);
        if (VerticalPanel != null) VerticalPanel.SetActive(!isLandscape);

        Transform root = isLandscape ? HorizontalPanel?.transform : VerticalPanel?.transform;
        if (root == null)
        {
            root = this.transform;
        }
        vipImage = root.Find("Top/vip")?.gameObject;
        playerNameText = root.Find("Top/PlayerName/PlayerName")?.GetComponent<Text>();
        nan = root.Find("Top/PlayerName/nan")?.gameObject;
        nv = root.Find("Top/PlayerName/nv")?.gameObject;
        playerIDText = root.Find("Top/PlayerID/ID")?.GetComponent<Text>();
        clubNameText = root.Find("Top/ClubName/ClubName")?.GetComponent<Text>();
        avatar = root.Find("Top/Mask/Avatar")?.GetComponent<RawImage>();
        forbiddenSpeechToggle = root.Find("Top/ForbiddenSpeechToggle")?.GetComponent<Toggle>();
        BtnKickOut = root.Find("Top/BtnKickOut")?.GetComponent<Button>();
        BtnGifs = root.Find("Gifs");

    }

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        InitUI();
        if (BtnKickOut != null)
        {
            BtnKickOut.onClick.RemoveAllListeners();
            BtnKickOut.onClick.AddListener(OnBtnKickOutClick);
        }
        if (forbiddenSpeechToggle != null)
        {
            forbiddenSpeechToggle.onValueChanged.RemoveAllListeners();
            forbiddenSpeechToggle.onValueChanged.AddListener(OnforbidToggleChanged);
        }
        if (BtnGifs != null)
        {
            Button[] buttons = BtnGifs.GetComponentsInChildren<Button>();
            int basePrice = GlobalManager.GetConstants(2);
            for (int i = 0; i < buttons.Length; i++)
            {
                string name = buttons[i].name;
                var textTrans = buttons[i].transform.Find("Text");
                if (textTrans != null)
                {
                    textTrans.GetComponent<Text>().text = basePrice.ToString();
                }
                buttons[i].onClick.RemoveAllListeners();
                buttons[i].onClick.AddListener(() => OnBtnGifClickAction(name));//给所有按钮绑定监听事件
            }
        }
    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        InitUI();
        // 如果是重用池中的对象，可能需要重新绑定监听器
        // 但由于 InitUI 重新查找了组件，而 listeners 是绑定在旧组件上的
        // 为了安全，建议在 OnOpen 中也重新绑定一次 listeners (如果组件实例发生了变化)
        if (BtnKickOut != null)
        {
            BtnKickOut.onClick.RemoveAllListeners();
            BtnKickOut.onClick.AddListener(OnBtnKickOutClick);
        }
        if (forbiddenSpeechToggle != null)
        {
            forbiddenSpeechToggle.onValueChanged.RemoveAllListeners();
            forbiddenSpeechToggle.onValueChanged.AddListener(OnforbidToggleChanged);
        }
        if (BtnGifs != null)
        {
            Button[] buttons = BtnGifs.GetComponentsInChildren<Button>();
            int basePrice = GlobalManager.GetConstants(2);
            for (int i = 0; i < buttons.Length; i++)
            {
                string name = buttons[i].name;
                var textTrans = buttons[i].transform.Find("Text");
                if (textTrans != null)
                {
                    textTrans.GetComponent<Text>().text = basePrice.ToString();
                }
                buttons[i].onClick.RemoveAllListeners();
                buttons[i].onClick.AddListener(() => OnBtnGifClickAction(name));
            }
        }
        GF.LogInfo("playerInforPanel");
        init = true;
        dzInfo.SetActive(false);
        HotfixNetworkComponent.AddListener(MessageID.Msg_PlayerInfoRs, FuncPlayerInfoRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_ClickPlayerRs, Function_ClickPlayerRs);

        var data = Params.Get<VarByteArray>("deskPlayer");
        deskPlayer = DeskPlayer.Parser.ParseFrom(data);

        // 如果不是俱乐部房隐藏 clubNameText父节点
        if (clubNameText != null && clubNameText.transform.parent != null)
        {
            clubNameText.transform.parent.gameObject.SetActive(deskPlayer.ClubId > 0);
        }

        //请求个人数据
        Msg_PlayerInfoRq req = MessagePool.Instance.Fetch<Msg_PlayerInfoRq>();
        req.Player = deskPlayer.BasePlayer.PlayerId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_PlayerInfoRq, req);
        //判断是否是房间创建者
        isRoomCreate = Params.Get<VarBoolean>("isRoomCreate");
        forbiddenSpeechToggle.gameObject.SetActive(isRoomCreate && !Util.IsMySelf(deskPlayer.BasePlayer.PlayerId));
        BtnKickOut.gameObject.SetActive(isRoomCreate && !Util.IsMySelf(deskPlayer.BasePlayer.PlayerId));
        // //如果开了Ip 则显示ip地址
        // if (!string.IsNullOrEmpty(deskPlayer.BasePlayer.Ip))
        // {
        //     IP.SetActive(true);
        //     // 如果IP GameObject下有Text组件，设置IP文本
        //     var ipText = IP.GetComponentInChildren<Text>();
        //     if (ipText != null)
        //     {
        //         ipText.text = deskPlayer.BasePlayer.Ip;
        //     }
        // }
        // else
        // {
        //     IP.SetActive(false);
        // }
        //根据是否是自己来判断使用本地设置还是哈希值
        //自己：使用本地设置的男声/女声
        //其他玩家：使用PlayerId哈希值判断性别
        bool isMale = GetPlayerGenderIsMale(deskPlayer.BasePlayer.PlayerId);
        if (nan != null)
        {
            nan.SetActive(isMale);
        }
        if (nv != null)
        {
            nv.SetActive(!isMale);
        }

        playerNameText.text = deskPlayer.BasePlayer.Nick;
        playerIDText.text = deskPlayer.BasePlayer.PlayerId.ToString();
        forbiddenSpeechToggle.isOn = deskPlayer.Forbid;
        Util.DownloadHeadImage(avatar, deskPlayer.BasePlayer.HeadImage);
        vipImage.SetActive(deskPlayer.BasePlayer.Vip > 0);
        init = false;
    }
    void OnBtnGifClickAction(string name)
    {
        if (int.TryParse(name, out var gift))
        {
            Sound.PlayEffect(AudioKeys.SOUND_BTN);
            Msg_SendGiftRq req = MessagePool.Instance.Fetch<Msg_SendGiftRq>();
            req.ToPlayer = deskPlayer.BasePlayer.PlayerId;
            req.Gift = gift;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_SendGiftRq, req);
            onBtnCloseClick();
        }
    }
    public void OnBtnKickOutClick()
    {
        Sound.PlayEffect(AudioKeys.SOUND_BTN);
        Util.GetInstance().OpenConfirmationDialog("踢出房间", $"确定要踢出房间 <color=#D0FE36>{deskPlayer.BasePlayer.Nick}</color>", () =>
        {
            GF.LogInfo("踢出房间");
            Msg_ClickPlayerRq req = MessagePool.Instance.Fetch<Msg_ClickPlayerRq>();
            req.PlayerId = deskPlayer.BasePlayer.PlayerId;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_ClickPlayerRq, req);
        });
    }
    public void OnforbidToggleChanged(bool isOn)
    {
        int type = 0;
        if (isOn) type = 1;
        if (init == false)
        {
            Msg_ForbidChatRq req = MessagePool.Instance.Fetch<Msg_ForbidChatRq>();
            req.PlayerId = deskPlayer.BasePlayer.PlayerId;
            req.Type = type;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_ForbidChatRq, req);
        }
    }
    public void onBtnCloseClick()
    {
        GF.UI.CloseUIForm(UIForm);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_PlayerInfoRs, FuncPlayerInfoRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_ClickPlayerRs, Function_ClickPlayerRs);
    }

    /// <summary>
    /// 获取德州玩家入池信息
    /// </summary>
    /// <param name="data"></param>
    private void FuncPlayerInfoRs(MessageRecvData data)
    {
        Msg_PlayerInfoRs ack = Msg_PlayerInfoRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("玩家信息 ", ack.ToString());
        
        string clubStr = ack.BringClub;
        if (string.IsNullOrEmpty(clubStr) && deskPlayer != null && deskPlayer.ClubId > 0 && ack.Player != null)
        {
            if (GlobalManager.GetInstance().MyJoinLeagueInfos.TryGetValue(deskPlayer.ClubId, out var league))
            {
                if (league.Creator == ack.Player.PlayerId)
                {
                    clubStr = league.LeagueName;
                }
            }
            else if (GlobalManager.GetInstance().LeagueInfo != null && GlobalManager.GetInstance().LeagueInfo.LeagueId == deskPlayer.ClubId)
            {
                if (GlobalManager.GetInstance().LeagueInfo.Creator == ack.Player.PlayerId)
                {
                    clubStr = GlobalManager.GetInstance().LeagueInfo.LeagueName;
                }
            }
        }
        
        if (clubNameText != null)
        {
            clubNameText.text = clubStr ?? "";
            if (clubNameText.transform.parent != null)
            {
                // 如果获取到的俱乐部名称为空，则隐藏该节点（修复非会长不隐藏问题）
                clubNameText.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(clubStr));
            }
        }

        if (GF.Procedure.CurrentProcedure is DZProcedure)
        {
            dzHandSum.text = ack.TexasHand.ToString();
            float enterPoolRate = (ack.TexasHand == 0 || ack.EnterPool == 0) ? 0 : (float)ack.EnterPool / ack.TexasHand * 100f;
            float turnNumRate = (ack.TurnNum == 0 || ack.TexasHand == 0) ? 0 : (float)ack.TurnNum / ack.TexasHand * 100f;
            float enterPoolWinRate = (ack.EnterPool == 0 || ack.EnterPoolWin == 0) ? 0 : (float)ack.EnterPoolWin / ack.EnterPool * 100f;
            float threeBetRate = (ack.ThreeBet == 0 || ack.TexasHand == 0) ? 0 : (float)ack.ThreeBet / ack.TexasHand * 100f;
            float stealBlindRate = (ack.StealBlind == 0 || ack.TexasHand == 0) ? 0 : (float)ack.StealBlind / ack.TexasHand * 100f;
            float[] dzRates = new float[5] { enterPoolRate, turnNumRate, enterPoolWinRate, threeBetRate, stealBlindRate };
            for (int i = 0; i < 5; i++)
            {
                dzInfoList[i].transform.Find("Bg/Rate").GetComponent<Text>().text = Mathf.RoundToInt(dzRates[i]).ToString() + "%";
                dzInfoList[i].transform.Find("Bg/Fill").GetComponent<Image>().fillAmount = Mathf.Clamp((float)dzRates[i] / 100, 0, 1);
            }
            dzInfo.SetActive(true);
        }
        else
        {
            dzInfo.SetActive(false);
        }
    }
    public void Function_ClickPlayerRs(MessageRecvData data)
    {
        //Msg_ClickPlayerRs
        GF.LogInfo("踢出");
        GF.UI.ShowToast("踢出成功");
        onBtnCloseClick();
    }

    /// <summary>
    /// 根据玩家ID获取性别（基于哈希算法，同一玩家始终相同）
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <returns>true=男性，false=女性</returns>
    private bool GetPlayerGenderIsMale(long playerId)
    {
        // 自己使用本地设置
        if (Util.IsMySelf(playerId))
        {
            string voiceGender = PlayerPrefs.GetString("MahjongVoiceGender", "male");
            return voiceGender == "male";
        }

        // 其他玩家：根据playerId哈希计算，保证同一玩家性别始终一致
        // 使用简单的奇偶判断：偶数=女性，奇数=男性
        return (playerId % 2 != 0);
    }
}

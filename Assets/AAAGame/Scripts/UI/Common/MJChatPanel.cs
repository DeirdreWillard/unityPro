using UnityEngine;
using UnityEngine.UI;
using GameFramework.Event;
using NetMsg;
using Cysharp.Threading.Tasks;
public class MJChatPanel : MonoBehaviour
{
    public string[] QLANGUs =
    {
        "不要走，决战到天亮",
        "快点出牌洛",
        "哎，一手牌烂到底",
        "又断线了，郁闷呀！",
        "不要吵啦！认真玩游戏吧！",
        "大家好！很高兴见到各位！",
        "哟！这牌打的也忒好了吧！",
        "各位不好意思，我要离开一会儿",
    };
    public Button Btnclose;
    public Button btnemoji;
    public GameObject btnemojiLight;
    public Button btnQLangu;
    public GameObject btnQLanguLight;
    public Button btnChat;
    public InputField inputField;
    public Button btnSend;
    public Transform emojiContent;

    public Transform QLanguItem;
    public Transform QLanguContent;

    public Transform OtherChatItem;
    public Transform MyChatItem;
    public Transform ChatContent;

    public Transform emojiScroll;
    public Transform QLanguScroll;
    public Transform ChatScroll;

    public bool QLanguPlanOpen = true;
    public bool emojiPlanOpen = false;
    public bool ChatPlanOpen = false;
    // Start is called before the first frame update
    void Awake()
    {
        Btnclose.onClick.AddListener(BtncloseFunction);
        btnemoji.onClick.AddListener(BtnemojiFunction);
        btnQLangu.onClick.AddListener(BtnQLanguFunction);
        btnChat.onClick.AddListener(BtnChatFunction);
        btnSend.onClick.AddListener(BtnSendFunction);
        for (int i = 0; i < QLANGUs.Length; i++)
        {
            int index = i;
            string text = QLANGUs[index]; // 创建局部变量副本
            var OtherChatItemTemplate = Instantiate(QLanguItem, QLanguContent);
            OtherChatItemTemplate.Find("Content").GetComponent<Text>().text = $"{index + 1}: {text}";
            OtherChatItemTemplate.GetComponent<Button>().onClick.AddListener
                    (
                            () => OnBtnQLANGUClickAction(index, text)
                        );
        }
        Button[] buttons = emojiContent.GetComponentsInChildren<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            string name = buttons[i].name;
            string localName = name; // 创建局部变量副本
            buttons[i].onClick.AddListener(() => OnBtnEmojiClickAction(localName));
        }

        GF.Event.Subscribe(GFEventArgs.EventId, OnGFEventChanged);
    }

    void Start()
    {
        inputField.text = "";
        QLanguPlanOpen = true;
        emojiPlanOpen = false;
        ChatPlanOpen = false;
        ShowPlan();
    }

    void OnDestroy()
    {
        GF.LogInfo_gsc("MJChatPanel OnDestroy");
        Btnclose.onClick.RemoveListener(BtncloseFunction);
        btnemoji.onClick.RemoveListener(BtnemojiFunction);
        btnQLangu.onClick.RemoveListener(BtnQLanguFunction);
        btnChat.onClick.RemoveListener(BtnChatFunction);
        btnSend.onClick.RemoveListener(BtnSendFunction);
        Button[] buttons = emojiContent.GetComponentsInChildren<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            string name = buttons[i].name;
            buttons[i].onClick.RemoveAllListeners();//给所有按钮绑定监听事件
        }
        foreach (Transform item in QLanguContent)
        {
            item.GetComponent<Button>().onClick.RemoveAllListeners();
        }
        GF.Event.Unsubscribe(GFEventArgs.EventId, OnGFEventChanged);
    }

    private void OnGFEventChanged(object sender, GameEventArgs e)
    {
        var args = e as GFEventArgs;
        switch (args.EventType)
        {
            case GFEventType.eve_SynDeskChat:
                AddChatPlan(args.UserData as Msg_DeskChat);
                break;

        }
    }
    void OnBtnEmojiClickAction(string name)
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
		if (Util.IsClickLocked()) return;
        if (GF.Procedure.CurrentProcedure is NNProcedure nnProcedure)
        {
            DeskPlayer deskplayer = nnProcedure.GetSelfPlayer();
            if (deskplayer == null)
            {
                GF.UI.ShowToast("请先坐下");
                return;
            }
        }
        if (GF.Procedure.CurrentProcedure is DZProcedure dzProcedure)
        {
            DeskPlayer deskplayer = dzProcedure.GetSelfPlayer();
            if (deskplayer == null)
            {
                GF.UI.ShowToast("请先坐下");
                return;
            }
        }
        if (GF.Procedure.CurrentProcedure is ZJHProcedure zjhProcedure)
        {
            DeskPlayer deskplayer = zjhProcedure.GetSelfPlayer();
            if (deskplayer == null)
            {
                GF.UI.ShowToast("请先坐下");
                return;
            }
        }
        if (GF.Procedure.CurrentProcedure is BJProcedure bjProcedure)
        {
            DeskPlayer deskplayer = bjProcedure.GetSelfPlayer();
            if (deskplayer == null)
            {
                GF.UI.ShowToast("请先坐下");
                return;
            }
        }
        ChatManager.GetInstance().SendEmoji(name);
    }
    void OnBtnQLANGUClickAction( int index, string str)
    {
        if (Util.IsClickLocked(1f)) return;
        Sound.PlayEffect(AudioKeys.SOUND_BTN);
        
        ChatManager.GetInstance().SendQuickVoice(index, str);
    }


    public void BtncloseFunction()
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        gameObject.SetActive(false);
    }
    public void BtnemojiFunction()
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (!emojiPlanOpen)
        {
            emojiPlanOpen = true;
            QLanguPlanOpen = false;
            ChatPlanOpen = false;
        }
        ShowPlan();
    }
    public void BtnQLanguFunction()
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (!QLanguPlanOpen)
        {
            emojiPlanOpen = false;
            QLanguPlanOpen = true;
            ChatPlanOpen = false;
        }

        ShowPlan();
    }

    public void BtnChatFunction()
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (!ChatPlanOpen)
        {
            emojiPlanOpen = false;
            QLanguPlanOpen = false;
            ChatPlanOpen = true;
        }

        ShowPlan();
    }

    public void ShowPlan()
    {
        emojiScroll.gameObject.SetActive(emojiPlanOpen);
        btnemojiLight.SetActive(emojiPlanOpen);
        btnQLanguLight.SetActive(QLanguPlanOpen);
        QLanguScroll.gameObject.SetActive(QLanguPlanOpen);
        ChatScroll.gameObject.SetActive(ChatPlanOpen);
        if (ChatPlanOpen)//刷新聊天内容
        {
            ReshUIAsync();
        }
    }
    public async void AddChatPlan(Msg_DeskChat deskChat)
    {
        // 无论在哪个页面都添加消息到列表
        AddChatItem(deskChat);
        
        // 只有在聊天页打开时才刷新布局和滚动
        if (ChatPlanOpen)
        {
            await UniTask.NextFrame();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)ChatContent);
            ChatScroll.GetComponent<ScrollRect>().verticalNormalizedPosition = 0f;
        }
    }

    public void BtnSendFunction()
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
		if (Util.IsClickLocked()) return;
        if (inputField.text.Trim() == "")
        {
            GF.UI.ShowToast("请输入聊天内容");
            return;
        }
        if (GF.Procedure.CurrentProcedure is MJGameProcedure mJGameProcedure)
        {
            if (mJGameProcedure.enterMJDeskRs.BaseInfo.BaseConfig.Forbid)
            {
                GF.LogInfo("禁止聊天");
                GF.UI.ShowToast("禁止聊天!");
                return;
            }
        }
        if (GF.Procedure.CurrentProcedure is PDKProcedures pdkProcedure)
        {
            if (pdkProcedure.EnterRunFastDeskRs.BaseConfig.Forbid)
            {
                GF.LogInfo("禁止聊天");
                GF.UI.ShowToast("禁止聊天!");
                return;
            }
        }
        string chatText = inputField.text;
        inputField.text = "";
        
        ChatManager.GetInstance().SendText(chatText);
        // 发送后不关闭面板，让用户看到自己发的消息
    }

    public async void ReshUIAsync()
    {
        var deskChatList = ChatManager.GetInstance().GetDeskChatList();
        //创建列表
        foreach (Transform item in ChatContent)
        {
            Destroy(item.gameObject);
        }
        for (int i = 0; i < deskChatList.Count; i++)
        {
            AddChatItem(deskChatList[i]);
        }
        await UniTask.NextFrame();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)ChatContent);
        ChatScroll.GetComponent<ScrollRect>().verticalNormalizedPosition = 0f;
    }
    public void AddChatItem(Msg_DeskChat deskChat)
    {
        if (deskChat.Type == 0)//文本
        {
            GameObject newItem;
            if (deskChat.Sender.PlayerId == Util.GetMyselfInfo().PlayerId)
                newItem = Instantiate(MyChatItem.gameObject, ChatContent);
            else
                newItem = Instantiate(OtherChatItem.gameObject, ChatContent);
            newItem.GetComponent<ChatItem>().InitText(deskChat);
        }
        else if (deskChat.Type == 1)//快捷语
        {
            GameObject newItem;
            if (deskChat.Sender.PlayerId == Util.GetMyselfInfo().PlayerId)
                newItem = Instantiate(MyChatItem.gameObject, ChatContent);
            else
                newItem = Instantiate(OtherChatItem.gameObject, ChatContent);
            newItem.GetComponent<ChatItem>().InitText(deskChat);
        }
        else if (deskChat.Type == 2)//表情
        {
            GameObject newItem;
            if (deskChat.Sender.PlayerId == Util.GetMyselfInfo().PlayerId)
                newItem = Instantiate(MyChatItem.gameObject, ChatContent);
            else
                newItem = Instantiate(OtherChatItem.gameObject, ChatContent);
            newItem.GetComponent<ChatItem>().InitEmoij(deskChat);
        }
        else if (deskChat.Type == 3)//语音
        {
            // 语音消息暂不在聊天列表显示
        }
    }

    public void OnInputFieldEnter()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            BtnSendFunction();
        }
    }
}

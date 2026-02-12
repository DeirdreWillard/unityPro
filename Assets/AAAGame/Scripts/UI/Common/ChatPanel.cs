using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Event;
using NetMsg;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class ChatPanel : UIFormBase
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
    public Button btnQLangu;
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

    public bool emojiPlanOpen = false;
    public bool QLanguPlanOpen = false;
    public bool ChatPlanOpen = false;
    // Start is called before the first frame update
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        for (int i = 0; i < QLANGUs.Length; i++)
        {
            var OtherChatItemTemplate = Instantiate(QLanguItem, QLanguContent);
            OtherChatItemTemplate.Find("Content").GetComponent<Text>().text = QLANGUs[i];
        }
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        Btnclose.onClick.AddListener(BtncloseFunction);
        btnemoji.onClick.AddListener(BtnemojiFunction);
        btnQLangu.onClick.AddListener(BtnQLanguFunction);
        btnSend.onClick.AddListener(BtnSendFunction);
        Button[] buttons = emojiContent.GetComponentsInChildren<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            string name = buttons[i].name;
            buttons[i].onClick.AddListener(() => OnBtnEmojiClickAction(name));//给所有按钮绑定监听事件
        }

        for(int i = 0; i < QLanguContent.childCount; i++)
        {
            int index = i;
            Transform item = QLanguContent.GetChild(i);
            item.GetComponent<Button>().onClick.AddListener(
                            () => OnBtnQLANGUClickAction(index, item.Find("Content").GetComponent<Text>().text)
                        );//给所有按钮绑定监听事件
        }

        GF.Event.Subscribe(GFEventArgs.EventId, OnGFEventChanged);

        transform.Find("Panel").transform.SetLocalPositionX(1200);
        transform.Find("Panel").transform.DOLocalMoveX(0, 0.3f);
        inputField.text = "";
        emojiPlanOpen = false;
        QLanguPlanOpen = false;
        ChatPlanOpen = true;
        ShowPlan();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        Btnclose.onClick.RemoveListener(BtncloseFunction);
        btnemoji.onClick.RemoveListener(BtnemojiFunction);
        btnQLangu.onClick.RemoveListener(BtnQLanguFunction);
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
        base.OnClose(isShutdown, userData);
    }


    private void OnGFEventChanged(object sender, GameEventArgs e)
    {
        var args = e as GFEventArgs;
        switch (args.EventType)
        {
            case GFEventType.eve_SynDeskChat:
                emojiPlanOpen = false;
                QLanguPlanOpen = false;
                ChatPlanOpen = true;
                inputField.text = "";
                AddChatPlan(args.UserData as Msg_DeskChat);
                break;

        }
    }
    void OnBtnEmojiClickAction(string name)
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (Time.time < chatcd + 2) return;
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
        chatcd = Time.time;
    }
    void OnBtnQLANGUClickAction( int index, string name)
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (Time.time < chatcd + 2) return;
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
        ChatManager.GetInstance().SendQuickVoice(index, name);
        chatcd = Time.time;
    }


    public void BtncloseFunction()
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        transform.Find("Panel").transform.DOLocalMoveX(-1200, 0.3f).OnComplete(() =>
        {
            GF.UI.CloseUIForm(UIForm);
        });
    }
    public void BtnemojiFunction()
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (emojiPlanOpen)
        {
            emojiPlanOpen = false;
            QLanguPlanOpen = false;
            ChatPlanOpen = true;
        }
        else
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
        if (QLanguPlanOpen)
        {
            emojiPlanOpen = false;
            QLanguPlanOpen = false;
            ChatPlanOpen = true;
        }
        else
        {
            emojiPlanOpen = false;
            QLanguPlanOpen = true;
            ChatPlanOpen = false;
        }

        ShowPlan();
    }

    public void ShowPlan()
    {
        emojiScroll.gameObject.SetActive(emojiPlanOpen);
        QLanguScroll.gameObject.SetActive(QLanguPlanOpen);
        ChatScroll.gameObject.SetActive(ChatPlanOpen);
        if (ChatPlanOpen)//刷新聊天内容
        {
            ReshUIAsync();
        }
    }
    public async void AddChatPlan(Msg_DeskChat deskChat)
    {
        // emojiScroll.gameObject.SetActive(emojiPlanOpen);
        // QLanguScroll.gameObject.SetActive(QLanguPlanOpen);
        ChatScroll.gameObject.SetActive(ChatPlanOpen);
        if (ChatPlanOpen)//刷新聊天内容
        {
            AddChatItem(deskChat);
            await UniTask.NextFrame();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)ChatContent);
            ChatScroll.GetComponent<ScrollRect>().verticalNormalizedPosition = 0f;
        }
    }

    float chatcd = 0;

    public void BtnSendFunction()
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (Time.time < chatcd + 2) return;
        if (GF.Procedure.CurrentProcedure is NNProcedure nnProcedure)
        {
            if (nnProcedure.enterNiuNiuDeskRs.BaseConfig.Forbid)
            {
                GF.LogInfo("禁止聊天");
                GF.UI.ShowToast("禁止聊天!");
                return;
            }
            DeskPlayer deskplayer = nnProcedure.GetSelfPlayer();
            if (deskplayer == null)
            {
                GF.UI.ShowToast("请先坐下");
                return;
            }
        }
        if (GF.Procedure.CurrentProcedure is DZProcedure dzProcedure)
        {
            if (dzProcedure.deskinfo.BaseConfig.Forbid)
            {
                GF.LogInfo("禁止聊天");
                GF.UI.ShowToast("禁止聊天!");
                return;
            }
            DeskPlayer deskplayer = dzProcedure.GetSelfPlayer();
            if (deskplayer == null)
            {
                GF.UI.ShowToast("请先坐下");
                return;
            }
        }
        if (GF.Procedure.CurrentProcedure is ZJHProcedure zjhProcedure)
        {
            if (zjhProcedure.enterZjhDeskRs.BaseConfig.Forbid)
            {
                GF.LogInfo("禁止聊天");
                GF.UI.ShowToast("禁止聊天!");
                return;
            }
            DeskPlayer deskplayer = zjhProcedure.GetSelfPlayer();
            if (deskplayer == null)
            {
                GF.UI.ShowToast("请先坐下");
                return;
            }
        }
        if (GF.Procedure.CurrentProcedure is BJProcedure bjProcedure)
        {
            if (bjProcedure.enterBJDeskRs.BaseInfo.BaseConfig.Forbid)
            {
                GF.LogInfo("禁止聊天");
                GF.UI.ShowToast("禁止聊天!");
                return;
            }
            DeskPlayer deskplayer = bjProcedure.GetSelfPlayer();
            if (deskplayer == null)
            {
                GF.UI.ShowToast("请先坐下");
                return;
            }
        }
        if (inputField.text.Trim() == "")
        {
            GF.UI.ShowToast("请输入聊天内容");
            return;
        }
        ChatManager.GetInstance().SendText(inputField.text);
        chatcd = Time.time;
    }

    public async Task ReshUIAsync()
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
        else if (deskChat.Type == 1)//快捷
        {
            // newItem.GetComponent<ChatItem>().InitText(deskChat);
        }
        else if (deskChat.Type == 2)//表情
        {
            // newItem.GetComponent<ChatItem>().InitEmoij(deskChat);
        }
        else if (deskChat.Type == 3)//语音
        {

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

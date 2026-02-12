using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Event;
using NetMsg;
using UnityGameFramework.Runtime;
using Cysharp.Threading.Tasks;
using System.Linq;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class ClubChatPanel : UIFormBase
{
    public ScrollRect varChatScroll;
    public GameObject varPanel;

    public InputField inputField;
    public Transform emojiContent;

    public GameObject OtherChatItem;
    public GameObject MyChatItem;
    public Transform ChatContent;

    private bool isEmoji = false;
    // Start is called before the first frame update
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        leagueInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("leagueInfo"));
        // varName.text = leagueInfo.LeagueName;
        inputField.text = "";

        HotfixNetworkComponent.AddListener(MessageID.Msg_ChatMsgListRs, Function_ChatMsgListRs);

        Button[] buttons = emojiContent.GetComponentsInChildren<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            string name = buttons[i].name;
            buttons[i].onClick.AddListener(() => OnBtnEmojiClickAction(name));//给所有按钮绑定监听事件
        }

        GF.Event.Subscribe(GFEventArgs.EventId, OnGFEventChanged);

        ChatManager.GetInstance().Send_ChatMsgListRq(leagueInfo.LeagueId);
        isEmoji = false;
        Util.GetInstance().CheckSafeCodeState(gameObject);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_ChatMsgListRs, Function_ChatMsgListRs);

        Button[] buttons = emojiContent.GetComponentsInChildren<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            string name = buttons[i].name;
            buttons[i].onClick.RemoveAllListeners();
        }
        GF.Event.Unsubscribe(GFEventArgs.EventId, OnGFEventChanged);
        base.OnClose(isShutdown, userData);
    }

    private void Function_ChatMsgListRs(MessageRecvData data)
    {
        // Msg_ChatMsgListRs
        Msg_ChatMsgListRs ack = Msg_ChatMsgListRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_ChatMsgListRs收到俱乐部聊天信息:" , ack.ToString());
        ChatManager.GetInstance().AddToChatMsgList(ack.ChatMsgs.ToArray());
        RushUI();
    }

    private void OnGFEventChanged(object sender, GameEventArgs e)
    {
        var args = e as GFEventArgs;
        switch (args.EventType)
        {
            case GFEventType.eve_SynClubChat:
                Msg_ChatMsg msg =  args.UserData as Msg_ChatMsg;
                AddChatItem(msg);
                if (Util.IsMySelf(msg.Sender.PlayerId))
                {
                    inputField.text = "";
                }
                break;

        }
    }
    void OnBtnEmojiClickAction(string name)
    {
        // ChatManager.GetInstance().SendEmoji(name);
    }

    public void BtnEmojiFunction()
    {
        isEmoji = !isEmoji;
        varPanel.transform.DOLocalMoveY(isEmoji ? -662 : -1582, 0.3f);
    }

    public LeagueInfo leagueInfo;
    public void BtnSendFunction()
    {
        if (inputField.text == "")
        {
            return;
        }
        ChatManager.GetInstance().Send_ChatMsgRq(inputField.text, leagueInfo.LeagueId);
    }

    public async void RushUI()
    {
        await UniTask.Delay(100);    // 延时 1 秒
        var chatMsgList = ChatManager.GetInstance().GetChatMsgList();
        //创建列表
        foreach (Transform item in ChatContent)
        {
            Destroy(item.gameObject);
        }
        for (int i = 0; i < chatMsgList.Count; i++)
        {
            AddChatItem(chatMsgList[i]);
            // await UniTask.Delay(10);    // 延时 1 秒
        }
        await UniTask.Delay(200);    // 延时 1 秒
    }
    public void AddChatItem(Msg_ChatMsg chatMsg)
    {
        GameObject newItem = Instantiate(Util.IsMySelf(chatMsg.Sender.PlayerId) ? MyChatItem : OtherChatItem, ChatContent);
        newItem.GetComponent<ChatItem>().InitChatMsgText(chatMsg);
        LayoutRebuilder.ForceRebuildLayoutImmediate(ChatContent.GetComponent<RectTransform>());
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        // 计算 ChatContent 高度
        float contentHeight = ChatContent.GetComponent<RectTransform>().rect.height;
        float viewportHeight = varChatScroll.viewport.rect.height;

        // 如果内容高度小于视口高度，无需滚动
        if (contentHeight <= viewportHeight)
            return;

        // 设置滚动目标位置
        float targetPosition = 0f; // ScrollRect 的 normalizedPosition 范围是 [0, 1]

        // 使用 DoTween 平滑滚动
        DOTween.To(
            () => varChatScroll.verticalNormalizedPosition,  // 当前滚动位置
            value => varChatScroll.verticalNormalizedPosition = value, // 设置滚动位置
            targetPosition, // 目标位置（底部）
            0.5f // 动画时间
        ).SetEase(Ease.InOutQuad); // 平滑动画曲线
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        // var uiParams = UIParams.Create();
        // uiParams.Set<VarByteArray>("leagueInfo", leagueInfo.ToByteArray());
        switch (btId)
        {
            case "Emoji":
                BtnEmojiFunction();
                break;
            case "Send":
                BtnSendFunction();
                break;
        }
    }
}


using UnityEngine;
using static UtilityBuiltin;
using UnityEngine.UI;
using NetMsg;

public class ChatItem : MonoBehaviour
{
    public Text timeText;
    public Text nameText;
    public RawImage avatar;

    public Text stateTxt;

    public GameObject emjCon;

    public ContentSizeFitter mContentSizeFitter;
    public Image chatContentBg; //聊天框背景

    public void InitChatMsgText(Msg_ChatMsg t)
    {

        nameText.text = t.Sender.Nick;
        stateTxt.text = t.Msg;
        Util.DownloadHeadImage(avatar, t.Sender.HeadImage);
        timeText.text = Util.MillisecondsToDateString(t.Time);

        //强制刷新contentsizefitter
        LayoutRebuilder.ForceRebuildLayoutImmediate(stateTxt.rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContentBg.rectTransform);

        if (stateTxt.preferredWidth >= 450)//超出长度单行限制，改为多行显示
        {
            mContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            mContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Vector2 temp = Vector2.zero;
            temp.x = 450;
            temp.y = stateTxt.rectTransform.sizeDelta.y;
            stateTxt.rectTransform.sizeDelta = temp;
        }
        else
        {
            mContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            mContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            Vector2 temp = Vector2.zero;
            temp.x = stateTxt.rectTransform.sizeDelta.x;
            temp.y = 28;
            stateTxt.rectTransform.sizeDelta = temp;
        }

        //强制刷新contentsizefitter
        LayoutRebuilder.ForceRebuildLayoutImmediate(stateTxt.rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContentBg.rectTransform);

        RectTransform rectTransform = transform.GetComponent<RectTransform>();

        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, -chatContentBg.rectTransform.anchoredPosition3D.y + chatContentBg.rectTransform.sizeDelta.y);

    }
    public void InitText(Msg_DeskChat t)
    {

        nameText.text = t.Sender.Nick;
        stateTxt.text = t.Chat; ;

        //强制刷新contentsizefitter
        LayoutRebuilder.ForceRebuildLayoutImmediate(stateTxt.rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContentBg.rectTransform);

        if (stateTxt.preferredWidth >= 450)//超出长度单行限制，改为多行显示
        {
            mContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            mContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Vector2 temp = Vector2.zero;
            temp.x = 450;
            temp.y = stateTxt.rectTransform.sizeDelta.y;
            stateTxt.rectTransform.sizeDelta = temp;
        }
        else
        {
            mContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            mContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            Vector2 temp = Vector2.zero;
            temp.x = stateTxt.rectTransform.sizeDelta.x;
            temp.y = 28;
            stateTxt.rectTransform.sizeDelta = temp;
        }

        //强制刷新contentsizefitter
        LayoutRebuilder.ForceRebuildLayoutImmediate(stateTxt.rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatContentBg.rectTransform);

        RectTransform rectTransform = transform.GetComponent<RectTransform>();

        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, -chatContentBg.rectTransform.anchoredPosition3D.y + chatContentBg.rectTransform.sizeDelta.y);

    }

    public void InitEmoij(Msg_DeskChat t)
    {
        nameText.text = t.Sender.Nick;
        chatContentBg.gameObject.SetActive(false);
        string s = AssetsPath.GetPrefab("UI/Chat/" + t.Chat);
        GF.UI.LoadPrefab(s, (gameObject) =>
        {
            GameObject a = Instantiate(gameObject as GameObject, emjCon.transform);
            a.GetComponent<FrameAnimator>().Framerate = 10;
        });
    }
}

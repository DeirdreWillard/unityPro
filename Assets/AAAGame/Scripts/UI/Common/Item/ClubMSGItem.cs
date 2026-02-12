
using UnityEngine.UI;
using UnityEngine;
using System;
using static MessageManager;


public class ClubMSGItem : MonoBehaviour
{
    public Text titleText;
    public Text timeText;
    public Text des1Text;
    public Text des2Text;
    public Text stateText;
    public Text stateOverText;

    public Image avatar;
    public Button okBtn;
    public Button cancelBtn;

    //string title, long time, List<string> des, string avatarUrl = null, UnityAction ok = null, UnityAction cancel = null
    public void Init(MsgParams msgParams)
    {
        timeText.text = Util.MillisecondsToDateString(msgParams.Time);
        timeText.color = msgParams.TextColors.TryGetValue("Time", out Color timeColor) ? timeColor : Color.grey;
        titleText.text = msgParams.DesDic.TryGetValue("Title", out String title) ? title : "";
        titleText.color = msgParams.TextColors.TryGetValue("Title", out Color titleColor) ? titleColor : Color.white;
        des1Text.text = msgParams.DesDic.TryGetValue("Des1", out String des1) ? des1 : "";
        des1Text.color = msgParams.TextColors.TryGetValue("Des1", out Color des1Color) ? des1Color : Color.white;
        des2Text.text = msgParams.DesDic.TryGetValue("Des2", out String des2) ? des2 : "";
        des2Text.color = msgParams.TextColors.TryGetValue("Des2", out Color des2Color) ? des2Color : Color.grey;
        stateText.text = msgParams.DesDic.TryGetValue("State", out String state) ? state : "";
        stateText.color = msgParams.TextColors.TryGetValue("State", out Color stateColor) ? stateColor : Color.white;
        stateOverText.text = msgParams.DesDic.TryGetValue("StateOver", out String stateOver) ? stateOver : "";
        stateOverText.color = msgParams.TextColors.TryGetValue("StateOver", out Color stateOverColor) ? stateOverColor : Color.white;

        msgParams.DesDic.TryGetValue("StateOver", out String avatarUrl);
        if (avatarUrl != null)
        {
            avatar.SetSprite(avatarUrl, true);
            avatar.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            avatar.transform.parent.gameObject.SetActive(false);
        }

        if (msgParams.OkAction != null)
        {
            okBtn.onClick.RemoveAllListeners();
            okBtn.onClick.AddListener(msgParams.OkAction);
        }
        if (msgParams.CancelAction != null)
        {
            cancelBtn.onClick.RemoveAllListeners();
            cancelBtn.onClick.AddListener(msgParams.CancelAction);
        }
    }
}

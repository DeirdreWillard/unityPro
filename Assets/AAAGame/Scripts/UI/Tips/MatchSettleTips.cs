using System;
using System.Collections;
using System.Collections.Generic;
using NetMsg;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

public class MatchSettleTips : MonoBehaviour
{
    public GameObject loseTips;
    public GameObject winTips;

    public Action action;
    public void ShowLose(Msg_SynMatchLose ack, Action action)
    {
        this.action = action;
        loseTips.SetActive(true);
        winTips.SetActive(false);
        SkeletonAnimation skeletonAnimation = loseTips.transform.Find("GameLoseAni").GetComponent<SkeletonAnimation>();
        skeletonAnimation.loop = false;
        skeletonAnimation.AnimationState.SetAnimation(0, "Out", false);
    }

    public void ShowWin(Msg_SynMatchFirst ack, Action action)
    {
        this.action = action;
        RawImage avatar = winTips.transform.Find("mask/avatar").GetComponent<RawImage>();
        Util.DownloadHeadImage(avatar, ack.First.HeadImage);
        winTips.SetActive(true);
        loseTips.SetActive(false);
    }

    public void TrueClick(){
        loseTips.SetActive(false);
        winTips.SetActive(false);
        this.action?.Invoke();
        Destroy(this.gameObject);
    }

    public void  CancelClick() {
        loseTips.SetActive(false);
        winTips.SetActive(false);
        Destroy(this.gameObject);
    }
}

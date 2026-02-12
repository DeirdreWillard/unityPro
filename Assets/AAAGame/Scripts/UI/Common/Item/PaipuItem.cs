using System.Collections.Generic;
using DG.Tweening;
using NetMsg;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PaipuItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{

    public Text Des;
    public Text BaseCoin;

    public List<Card> cards;
    public Button changeBtn;

    public ScrollRect parentScrollRect; // 外层 ScrollRect
    public Transform itemTransform; // 普通 Transform
    public float leftPositionX; // 最左的位置
    public float rightPositionX; // 最右的位置
    public float slideDuration; // 平滑滑动时间
    public float threshold; // 滑动触发阈值

    private Vector2 startTouchPosition; // 滑动起点
    private Vector3 startItemPosition; // Item 起始位置
    private Vector2 dragDelta;
    private bool isDragging = false;

    public Msg_PaiPu paipuInfo;

    public void Init(Msg_PaiPu paipuInfo)
    {
        this.paipuInfo = paipuInfo;

        Des.text = paipuInfo.BasePlayer.Nick + "以" + GameUtil.ShowType(paipuInfo.CardType, paipuInfo.Method) + "获胜";
        BaseCoin.text = float.Parse(paipuInfo.BaseCoin) + "/" + (float.Parse(paipuInfo.BaseCoin) * 2);
        for (var i = 0; i < cards.Count; i++)
        {
            if (paipuInfo.Cards.Count > i && paipuInfo.Cards[i] != 0)
            {
                cards[i].Init(paipuInfo.Cards[i]);
            }else{
                cards[i].gameObject.SetActive(false);
            }
        }
        MoveToLeft();
    }

    public void OnButtonClick(string btId)
    {
        if (Util.IsClickLocked()) return;
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        switch (btId)
        {
            case "ChangeBtn":
                MoveToRight();
                break;
            case "BtnShare":
                Msg_PlayBackRq req = MessagePool.Instance.Fetch<Msg_PlayBackRq>();
                req.ReportId = paipuInfo.ReportId;
                GF.LogInfo("请求牌谱:", req.ReportId);
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_PlayBackRq, req);
                break;
            case "BtnNotes":
                break;
            case "BtnDelete":
                Util.GetInstance().OpenConfirmationDialog("删除牌谱", "确定删除", () =>
                    {
                        Msg_CollectRoundRq req = MessagePool.Instance.Fetch<Msg_CollectRoundRq>();
                        req.ReportId = paipuInfo.ReportId;
                        req.State = 0;
                        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_CollectRoundRq, req);
                    });
                break;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 记录滑动开始的触摸点和 item 初始位置
        startTouchPosition = eventData.position;
        startItemPosition = itemTransform.localPosition; // 获取局部位置
        isDragging = true;
        dragDelta = Vector2.zero;
        // 通知 ScrollRect 开始拖动
        parentScrollRect?.OnBeginDrag(eventData);
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        dragDelta = eventData.position - startTouchPosition;

        if (Mathf.Abs(dragDelta.x) > Mathf.Abs(dragDelta.y))
        {
            float newX = Mathf.Clamp(startItemPosition.x + dragDelta.x, rightPositionX, leftPositionX);
            itemTransform.localPosition = new Vector3(newX, startItemPosition.y, startItemPosition.z);
        }
        else
        {
            parentScrollRect?.OnDrag(eventData);
        }
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        if (Mathf.Abs(dragDelta.x) >= threshold)
        {
            if (dragDelta.x < 0)
                MoveToRight();
            else
                MoveToLeft();
        }
        else
        {
            if (Mathf.Abs(itemTransform.localPosition.x - rightPositionX) < Mathf.Abs(itemTransform.localPosition.x - leftPositionX))
                MoveToRight();
            else
                MoveToLeft();
        }
        parentScrollRect?.OnEndDrag(eventData);
    }

    // 滑动到最左
    private void MoveToLeft()
    {
        changeBtn.gameObject.SetActive(true);
        itemTransform.DOLocalMove(new Vector3(leftPositionX, startItemPosition.y, startItemPosition.z), slideDuration)
            .SetEase(Ease.OutCubic);
    }

    // 滑动到最右
    private void MoveToRight()
    {
        changeBtn.gameObject.SetActive(false);
        itemTransform.DOLocalMove(new Vector3(rightPositionX, startItemPosition.y, startItemPosition.z), slideDuration)
            .SetEase(Ease.OutCubic);
    }

}

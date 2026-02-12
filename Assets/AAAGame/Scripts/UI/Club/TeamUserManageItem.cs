using DG.Tweening;
using NetMsg;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TeamUserManageItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    //int64 playerId = 1;//玩家ID
    //string nickName = 2;//玩家昵称
    //string headImage = 3;//头像
    //int64 gold = 4;//金币
    //int64 offline = 5;//离线时间 0表示在线
    //string remarks = 6;//备注

    public RawImage headImage;

    public Text playerId;
    public Text nickName;
    public Text coin;
    public Text gold;
    public Text diamond;
    public Text offline;
    public Text remarks;
    public Button changeBtn;

    public ScrollRect parentScrollRect; // 外层 ScrollRect
    public Transform itemTransform; // 普通 Transform
    public float leftPositionX = 369f; // 最左的位置
    public float rightPositionX = -326.5f; // 最右的位置
    public float slideDuration = 0.3f; // 平滑滑动时间
    public float threshold = 100f; // 滑动触发阈值

    private Vector2 startTouchPosition; // 滑动起点
    private Vector3 startItemPosition; // Item 起始位置
    private Vector2 dragDelta;
    private bool isDragging = false;

    public LeagueUser leagueUser;

    public void Init(LeagueUser leagueUser)
    {
        this.leagueUser = leagueUser;

        playerId.text = "ID:" + leagueUser.PlayerId.ToString();
        nickName.text = leagueUser.NickName.ToString();
        Util.DownloadHeadImage(headImage, leagueUser.HeadImage);
        gold.text = leagueUser.Gold.ToString();
        coin.text = leagueUser.Coin;
        diamond.text = leagueUser.Diamond.ToString();
        remarks.text = leagueUser.Remarks == "" ? "" : "备注: " + leagueUser.Remarks.ToString();
        if (leagueUser.Offline == 0)
        {
            offline.text = "在线";
            offline.color = Color.green;
        }
        else
        {
            offline.text = Util.GetOfflineTimeText(leagueUser.Offline);
            offline.color = Color.gray;
        }
        transform.Find("Bg/ButtonCoinOp").gameObject.SetActive(!Util.IsMySelf(leagueUser.PlayerId));

        MoveToLeft();
    }

    public void ChangeBtn()
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        MoveToRight();
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

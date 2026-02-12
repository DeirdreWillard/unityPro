using DG.Tweening;
using NetMsg;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static UtilityBuiltin;
using UnityEngine;

public class SeatManager_bj : MonoBehaviour
{
    #region
    public Dictionary<int, Vector3> seatPos2 = new();
    public Dictionary<int, Vector3> seatPos3 = new();
    public Dictionary<int, Vector3> seatPos4 = new();
    public Dictionary<int, Vector3> seatPos5 = new();
    public Dictionary<int, Vector3> seatPos6 = new();
    

    public GameObject goButtonEmojiStart;
    public void InitSeatPos()
    {
        float prop = 60f / (2400f - 1920f);
        float h = Screen.height * (1080f / Screen.width);
        float addY = (2400f - h) * prop;
        // GF.LogError("addY: " + addY);

        seatPos2.Add(0, new Vector3(0, 450, 0));
        seatPos2.Add(4, new Vector3(0, -330, 0));

        seatPos3.Add(0, new Vector3(0, 450, 0));
        seatPos3.Add(3, new Vector3(120, 300, 0));
        seatPos3.Add(6, new Vector3(-120, 300, 0));

        seatPos4.Add(0, new Vector3(0, 450, 0));
        seatPos4.Add(2, new Vector3(120, 50, 0));
        seatPos4.Add(4, new Vector3(0, -330, 0));
        seatPos4.Add(7, new Vector3(-120, 50, 0));

        seatPos5.Add(0, new Vector3(0, 450, 0));
        seatPos5.Add(1, new Vector3(120, 20 + addY, 0));
        seatPos5.Add(2, new Vector3(300, 700 - addY, 0));
        seatPos5.Add(6, new Vector3(-300, 700 - addY, 0));
        seatPos5.Add(7, new Vector3(-120, 20 + addY, 0));

        seatPos6.Add(0, new Vector3(0, 450, 0));
        seatPos6.Add(1, new Vector3(120, -220 + addY, 0));
        seatPos6.Add(2, new Vector3(120, 380 - addY, 0));
        seatPos6.Add(4, new Vector3(0, -330, 0));
        seatPos6.Add(6, new Vector3(-120, 380 - addY, 0));
        seatPos6.Add(7, new Vector3(-120, -220 + addY, 0));

    }
    #endregion

    //需要读配置   
    //_initPos 当2人时，当3人时候。。当6人时的坐标配置
    public Dictionary<int, Seat_bj> seats = new();

    public int PeopleNum = -1;
    public long PreOperateGuid = -1;

    public bool IsPlayBack{ get; set; } = false;

    // 所有座位的初始位置,座位index=>位置  只读类型不做操作
    public Dictionary<int, Vector3> seatPosDic = new Dictionary<int, Vector3>();
    // Start is called before the first frame update
    private void Awake()
    {
        InitSeatPos();
    }
    public void Init(int num)
    {
        PeopleNum = num;
        SetPeopleNum(PeopleNum);
    }

    public Dictionary<int, Vector3> GetPositionsByNum(int peopleNum){
        Dictionary<int, Vector3> seatPosTmp = new Dictionary<int, Vector3>();
        switch (peopleNum)
        {
            case 2:
                seatPosTmp = seatPos2;
                break;
            case 3:
                seatPosTmp = seatPos3;
                break;
            case 4:
                seatPosTmp = seatPos4;
                break;
            case 5:
                seatPosTmp = seatPos5;
                break;
            case 6:
                seatPosTmp = seatPos6;
                break;
        }
        return seatPosTmp;
    }
    public void SetPeopleNum(int peopleNum)
    {
        Dictionary<int, Vector3> seatPosTmp = GetPositionsByNum(peopleNum);
        ClearSeatsPlayer();
        for (int i = 0; i < seatPosTmp.Count; i++)
        {
            var kvp = seatPosTmp.ElementAt(i);
            var obj = transform.GetChild(i).gameObject;
            obj.SetActive(true);
            obj.transform.position = kvp.Value;
            Seat_bj seat = obj.GetComponent<Seat_bj>();
            seat.SeatId = kvp.Key;
            seat.SeatIndex = i;//没入座前正常显示
            seat.SeatType = GetSeatPosTypeByIndex(seat.SeatIndex);
            seat.ClearPlayer();
            seat.SetPos(kvp.Value, false, GetAnchorsByIndex(seat.SeatIndex));
            seatPosDic.Add(seat.SeatIndex, kvp.Value);
            seats.Add(seat.SeatId, seat);
        }
    }
    /// <summary>
    /// 玩家进入
    /// </summary>
    public void PlayerEnter(DeskPlayer deskPlayer,bool isAni = true)
    {
        Seat_bj seatNode = GetPlayerByPos(deskPlayer.Pos);
        if (seatNode == null) { GF.LogError("没有找到座位"); return;} 
        seatNode.PlayerEnter(deskPlayer,IsPlayBack);
        //只有自己进入才转
        if (Util.IsMySelf(deskPlayer.BasePlayer.PlayerId) && !IsPlayBack)
        {
            if (!this._isMoveSeatAnim)
            {
                CoroutineRunner.Instance.StartCoroutine(MoveSeatAnim(isAni));
            }
        }
    }

    /// <summary>
    /// 获取下一个位置的index
    /// </summary>
    public int GetNextSeatIndex(int seatId)
    {
        seatId += 1;
        if (seatId >= PeopleNum)
        {
            seatId = 0;
        }
        return seatId;
    }

    public List<Seat_bj> GetSeats()
    {
        return seats.Values.Where(item => item.IsNotEmpty()).ToList();
    }

    public List<Seat_bj> GetInGameSeats()
    {
        return seats.Values.Where(item => item.IsNotEmpty() && item.playerInfo.InGame).ToList();
    }
    public Seat_bj GetSelfSeat()
    {
        return seats.Values.FirstOrDefault(item => item.IsNotEmpty() && Util.IsMySelf(item.playerInfo.BasePlayer.PlayerId));
    }

    public bool IsInTable(long playerID){
        return GetPlayerByPlayerID(playerID) != null;
    }

    bool _isMoveSeatAnim = false;
    /// <summary>
    /// 座位转圈的动画
    /// </summary>
    IEnumerator MoveSeatAnim(bool isAni)
    {
        var seat = GetSelfSeat();
        if (seat == null)
        {
            yield break;
        }

        if (seat.SeatIndex == 0)
        {
            yield break;
        }

        this._isMoveSeatAnim = true;

        // 所有座位向下一个位置移动,直到自己座位的index == 0
        // 最多移动人数次,防止死循环
        for (int i = 0; i < PeopleNum; i++)
        {
            foreach (var item in this.seats)
            {
                var next_index = GetNextSeatIndex(item.Value.SeatIndex);
                item.Value.SeatIndex = next_index;
            }
            if (seat.SeatIndex == 0)
            {
                break;
            }
        }
        foreach (var item in this.seats)
        {
            item.Value.SeatType = GetSeatPosTypeByIndex(item.Value.SeatIndex);
            item.Value.SetPos(seatPosDic[item.Value.SeatIndex], isAni, GetAnchorsByIndex(item.Value.SeatIndex));
            //刷新下自己座位里详细的位置
        }

        // 如果不需要动画，则不等待
        if (isAni)
        {
            yield return new WaitForSeconds(0.5f);
        }
        _isMoveSeatAnim = false;
    }

    public Vector2[] GetAnchorsByIndex(int seatIndex)
    {
        Vector2[] anchors = new Vector2[2];
        Dictionary<int, Vector3> seatPosTmp = GetPositionsByNum(PeopleNum);
        int index = seatIndex;
        for (int i = 0; i < seatPosTmp.Count; i++)
        {
            if (i == seatIndex)
            {
                var kvp = seatPosTmp.ElementAt(i);
                index = kvp.Key;
            }
        }
        if (PeopleNum == 7)
        {
            //等于3要改成4 获取上面的锚点坐标
            index = (index == 3 || index == 6) ? 4 : index;
        }
        else if (PeopleNum == 8)
        {
            index = index == 5 ? 6 : index;
        }
        if (index == 0)
        {
            anchors[0] = new Vector2(0.5f, 0f);
            anchors[1] = new Vector2(0.5f, 0f);
        }
        else if (index > 0 && index < 4)
        {
            anchors[0] = new Vector2(0f, 0.5f);
            anchors[1] = new Vector2(0f, 0.5f);
        }
        else if (index > 3 && index < 6)
        {
            anchors[0] = new Vector2(0.5f, 1f);
            anchors[1] = new Vector2(0.5f, 1f);
        }
        else if (index > 5 && index < 9)
        {
            anchors[0] = new Vector2(1f, 0.5f);
            anchors[1] = new Vector2(1f, 0.5f);
        }
        return anchors;
    }

    public SeatPosType GetSeatPosTypeByIndex(int seatIndex){
        SeatPosType seatPosType = SeatPosType.Down;
        Dictionary<int, Vector3> seatPosTmp = GetPositionsByNum(PeopleNum);
        int index = seatIndex;
        for (int i = 0; i < seatPosTmp.Count; i++)
        {
            if (i == seatIndex)
            {
                var kvp = seatPosTmp.ElementAt(i);
                index = kvp.Key;
            }
        }
        if (PeopleNum == 5)
        {
            index = (index == 2 || index == 6) ? 4 : index;
        }
        if (index == 0)
        {
            seatPosType = SeatPosType.Top;
        }
        else if (index > 0 && index < 4)
        {
            seatPosType = SeatPosType.Left;
        }
        else if (index > 3 && index < 6)
        {
            seatPosType = SeatPosType.Top;
        }
        else if (index > 5 && index < 9)
        {
            seatPosType = SeatPosType.Right;
        }
        return seatPosType;
    }

    /// <summary>
    /// 单轮结束 重置用户UI
    /// </summary>
    public void ResetSeatsPlayer()
    {
        foreach (var item in seats)
        {
            item.Value.ResetPlayer();
        }
    }
    /// <summary>
    /// 退出游戏 清空所有座位
    /// </summary>
    public void ClearSeatsPlayer()
    {
        for (int i = 0; i < seatPos6.Count; i++)
        {
            var kvp = seatPos6.ElementAt(i);
            var seat = transform.GetChild(i).GetComponent<Seat_bj>();
            seat.transform.localPosition = kvp.Value;
            DOTween.Kill(seat.gameObject);
            seat.gameObject.SetActive(false);
        }
        IsPlayBack = false;
        seats.Clear();
        seatPosDic.Clear();
    }

    /// <summary>
    /// 根据ID查找座位
    /// </summary>
    public Seat_bj GetPlayerByPos(Position pos)
    {
        int seatId = GameUtil.GetInstance().GetSeatIdByPos(pos);
        return seats.TryGetValue(seatId, out Seat_bj seat) ? seat : null;
    }
    /// <summary>
    /// 根据PlayerID查找
    /// </summary>
    public Seat_bj GetPlayerByPlayerID(long playerID)
    {
        return seats.Values.FirstOrDefault(item => item.IsNotEmpty() && item.playerInfo.BasePlayer.PlayerId == playerID);
    }

    /// <summary>
    /// 根据PlayerID查找庄家
    /// </summary>
    public Seat_bj GetBanker() =>
        seats.Values.FirstOrDefault(seat => seat.IsNotEmpty() && seat.playerInfo.IsBanker);


    /// <summary>
    /// 收到发牌消息
    /// </summary>
    public void Syn_DealCard(DealCard Deal)
    {
        Seat_bj seatNode = GetPlayerByPos(Deal.Pos);
        if (seatNode == null) return;
        seatNode.playerInfo.IsBanker = Deal.IsBanker;
        seatNode.playerInfo.InGame = true;
    }

    public void UpdatePlayerInfo(DeskPlayer deskPlayer)
    {
        Seat_bj seatNode = GetPlayerByPlayerID(deskPlayer.BasePlayer.PlayerId);
        if (seatNode == null) return;
        seatNode.UpdatePlayerInfo(deskPlayer);
    }

    /// <summary>
    /// 站起
    /// </summary>
    public void Function_SynSitUp(Position pos)
    {
        Seat_bj seatNode = GetPlayerByPos(pos);
        if (seatNode == null) return;
        seatNode.Function_SynSitUp();
    }
    /// <summary>
    /// 禁言某个玩家
    /// </summary>
    public void Function_ForbidChatRs(long id, int type)
    {
        bool Forbid = false;
        if (type == 1)
            Forbid = true;
        Seat_bj seatNode = GetPlayerByPlayerID(id);
        if (seatNode == null) return;
        seatNode.playerInfo.Forbid = Forbid;
    }
    /// <summary>
    /// 礼物
    /// </summary>
    public void Function_SynSendGift(long basePlayerID, long toPlayerId, string gift)
    {
        Seat_bj from = GetPlayerByPlayerID(basePlayerID);
        Seat_bj to = GetPlayerByPlayerID(toPlayerId);
        //忽略理牌中的表情
        if (from != null && from.handCardsObj.activeSelf) return;
        if (to == null) return;
        GF.UI.LoadPrefab(AssetsPath.GetPrefab("UI/Chat/" + gift), (gameObject) =>
        {
            if (gameObject == null) return;
            BJProcedure bjProcedure = GF.Procedure.CurrentProcedure as BJProcedure;
            if (bjProcedure == null || bjProcedure.GamePanel_bj == null) return;
            GameObject giftObj = Instantiate((gameObject as GameObject), bjProcedure.GamePanel_bj.transform);
            if (from == null)
            {
                giftObj.transform.localPosition = goButtonEmojiStart.transform.localPosition;
            }
            else
            {
                giftObj.transform.localPosition = from.transform.localPosition;
            }
            giftObj.transform.localScale = gift switch
            {
                "2001" => new Vector3(1.8f, 1.8f, 1.8f),
                "2003" => new Vector3(1.8f, 1.8f, 1.8f),
                "2005" => new Vector3(2.2f, 2.2f, 2.2f),
                "2007" => new Vector3(2.2f, 2.2f, 2.2f),
                _ => new Vector3(2f, 2f, 2f),
            };
            FrameAnimator frameAnimator = giftObj.GetComponent<FrameAnimator>();
            frameAnimator.Loop = false;
            frameAnimator.Stop();
            frameAnimator.FinishEvent += (giftobj) =>
            {
                Destroy(giftobj);
            };
            giftObj.transform.DOLocalMove(to.transform.localPosition, 0.8f).OnComplete(() =>
            {
                frameAnimator.Framerate = 6;
                if (gift == "2003")
                {
                    frameAnimator.Framerate = 2;
                }
                frameAnimator.Play();
				_ = GF.Sound.PlayEffect("face/" + gift + ".mp3");
            });
        });
    }
    /// <summary>
    /// 表情
    /// </summary>
    public void Function_Biao(long basePlayerID, string chat)
    {
        Seat_bj seatInfo = GetPlayerByPlayerID(basePlayerID);
        if (seatInfo == null) return;
        GF.UI.LoadPrefab(AssetsPath.GetPrefab("UI/Chat/" + chat), (gameObject) =>
        {
            if (gameObject == null) return;
            BJProcedure bjProcedure = GF.Procedure.CurrentProcedure as BJProcedure;
            GameObject emoji = Instantiate(gameObject as GameObject, bjProcedure.GamePanel_bj.transform);
            emoji.GetComponent<FrameAnimator>().Framerate = 10;
            emoji.GetComponent<RectTransform>().position = seatInfo.transform.GetComponent<RectTransform>().position;
            // emoji.transform.localPosition = Vector3.zero;
            emoji.transform.localScale = Vector3.one;
            emoji.transform.DOScale(Vector3.one * 1.5f, 1f).OnComplete(() =>
            {
                if (emoji == null || emoji.gameObject == null) return;
                Destroy(emoji, 1f);
            });
        });
    }

    /// <summary>
    /// 留座离桌
    /// </summary>
    public void Function_SynPlayerState(Msg_SynPlayerState ack)
    {
        Seat_bj seatNode = GetPlayerByPlayerID(ack.PlayerId);
        if (seatNode == null) return;
        seatNode.Function_SynPlayerState(ack.State, ack.LeaveTime);
    }
}

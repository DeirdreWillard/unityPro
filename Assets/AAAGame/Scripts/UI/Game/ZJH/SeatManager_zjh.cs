﻿using DG.Tweening;
using NetMsg;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static UtilityBuiltin;
using UnityEngine;
using UnityEngine.UI;

public class SeatManager_zjh : MonoBehaviour
{
    #region
    public Dictionary<int, Vector3> seatPos2 = new();
    public Dictionary<int, Vector3> seatPos3 = new();
    public Dictionary<int, Vector3> seatPos4 = new();
    public Dictionary<int, Vector3> seatPos5 = new();
    public Dictionary<int, Vector3> seatPos6 = new();
    public Dictionary<int, Vector3> seatPos7 = new();
    public Dictionary<int, Vector3> seatPos8 = new();
    public Dictionary<int, Vector3> seatPos9 = new();

    public GameObject goButtonEmojiStart;
    public void InitSeatPos()
    {
        float addY;
        float ScreenRate = (float)Screen.height / (float)Screen.width;
        if (ScreenRate <= GlobalManager.TabletAspectThreshold)
        {
            addY = 0;
        }
        else
        {
            float prop = 60f / (2400f - 1920f);
            float h = Screen.height * (1080f / Screen.width);
            addY = (2400f - h) * prop;
        }
        // GF.LogError("addY: " + addY);

        seatPos2.Add(0, new Vector3(0, 390, 0));
        seatPos2.Add(4, new Vector3(0, -330, 0));

        seatPos3.Add(0, new Vector3(0, 390, 0));
        seatPos3.Add(3, new Vector3(120, 100, 0));
        seatPos3.Add(6, new Vector3(-120, 100, 0));

        seatPos4.Add(0, new Vector3(0, 390, 0));
        seatPos4.Add(2, new Vector3(120, 100, 0));
        seatPos4.Add(4, new Vector3(0, -330, 0));
        seatPos4.Add(7, new Vector3(-120, 100, 0));

        seatPos5.Add(0, new Vector3(0, 390, 0));
        seatPos5.Add(1, new Vector3(120, -400 + addY, 0));
        seatPos5.Add(2, new Vector3(120, 400 - addY, 0));
        seatPos5.Add(6, new Vector3(-120, 400 - addY, 0));
        seatPos5.Add(7, new Vector3(-120, -400 + addY, 0));

        seatPos6.Add(0, new Vector3(0, 390, 0));
        seatPos6.Add(1, new Vector3(120, -400 + addY, 0));
        seatPos6.Add(2, new Vector3(120, 290 - addY, 0));
        seatPos6.Add(4, new Vector3(0, -330, 0));
        seatPos6.Add(6, new Vector3(-120, 290 - addY, 0));
        seatPos6.Add(7, new Vector3(-120, -400 + addY, 0));

        seatPos7.Add(0, new Vector3(0, 390, 0));
        seatPos7.Add(1, new Vector3(120, -400 + addY, 0));
        seatPos7.Add(2, new Vector3(120, 290 - addY, 0));
        seatPos7.Add(3, new Vector3(-230, -330, 0));
        seatPos7.Add(6, new Vector3(230, -330, 0));
        seatPos7.Add(7, new Vector3(-120, 290 - addY, 0));
        seatPos7.Add(8, new Vector3(-120, -400 + addY, 0));

        seatPos8.Add(0, new Vector3(0, 390, 0));
        seatPos8.Add(1, new Vector3(120, -400 + addY, 0));
        seatPos8.Add(2, new Vector3(120, 0, 0));
        seatPos8.Add(3, new Vector3(120, 400- addY, 0));
        seatPos8.Add(4, new Vector3(0, -330, 0));
        seatPos8.Add(5, new Vector3(-120, 400- addY, 0));
        seatPos8.Add(6, new Vector3(-120, 0, 0));
        seatPos8.Add(7, new Vector3(-120, -400 + addY, 0));

        seatPos9.Add(0, new Vector3(0, 390, 0));
        seatPos9.Add(1, new Vector3(120, -400 + addY, 0));
        seatPos9.Add(2, new Vector3(120, 0, 0));
        seatPos9.Add(3, new Vector3(120, 400 - addY, 0));
        seatPos9.Add(4, new Vector3(-230, -330, 0));
        seatPos9.Add(5, new Vector3(230, -330, 0));
        seatPos9.Add(6, new Vector3(-120, 400 - addY, 0));
        seatPos9.Add(7, new Vector3(-120, 0, 0));
        seatPos9.Add(8, new Vector3(-120, -400 + addY, 0));
    }
    #endregion

    //需要读配置   
    //_initPos 当2人时，当3人时候。。当9人时的坐标配置
    public Dictionary<int, Seat_zjh> seats = new();

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
            case 7:
                seatPosTmp = seatPos7;
                break;
            case 8:
                seatPosTmp = seatPos8;
                break;
            case 9:
                seatPosTmp = seatPos9;
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
            var obj = transform.GetChild(kvp.Key).gameObject;
            obj.SetActive(true);
            obj.transform.position = kvp.Value;
            Seat_zjh seat = obj.GetComponent<Seat_zjh>();
            seat.SeatId = kvp.Key;
            seat.SeatIndex = i;//没入座前正常显示
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
        Seat_zjh seatNode = GetPlayerByPos(deskPlayer.Pos);
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

    //最后一轮操作信息
    public void RecoverySeats(List<Msg_PlayerOp> playerOp, Position currentOption)
    {
        // 参数安全检查
        if (playerOp == null || playerOp.Count == 0)
        {
            GF.LogError("RecoverySeats: playerOp为空或无数据");
            return;
        }

        List<Seat_zjh> seatNodes = GetInGameSeats();
        if (seatNodes.Count == 0)
        {
            GF.LogError("RecoverySeats: 没有在游戏中的玩家");
            return;
        }

        seatNodes.Sort((a, b) => a.SeatId.CompareTo(b.SeatId));
        
        // 检查currentOption是否有效
        int currentIndex = seatNodes.FindIndex(item => item.SeatId == ((int)currentOption - 1));
        if (currentIndex < 0 || currentIndex >= seatNodes.Count)
        {
            GF.LogError($"RecoverySeats: 无效的currentOption：{currentOption}，seatId：{((int)currentOption - 1)}");
            currentIndex = 0; // 使用默认值以防错误
        }

        Seat_zjh lastOpSeat = currentIndex == 0 ? seatNodes[seatNodes.Count - 1] : seatNodes[currentIndex - 1];

        for (int i = 1; i < seatNodes.Count + 1; i++)
        {
            int index = currentIndex - i < 0 ? seatNodes.Count + (currentIndex - i) : currentIndex - i;
            if (index < 0 || index >= seatNodes.Count)
            {
                GF.LogError($"RecoverySeats: 计算索引越界：{index}，跳过");
                continue;
            }
            
            Seat_zjh seat = seatNodes[index];
            if (seat == null || seat.playerInfo == null || seat.playerInfo.BasePlayer == null)
            {
                GF.LogError($"RecoverySeats: 座位或玩家信息为空，跳过");
                continue;
            }
            
            Msg_PlayerOp playerOpItem = playerOp.FirstOrDefault(item => item.PlayerId == seat.playerInfo.BasePlayer.PlayerId);
            if (playerOpItem == null) continue;
            if (playerOpItem.Option == ZjhOption.Follow || playerOpItem.Option == ZjhOption.Double || playerOpItem.Option == ZjhOption.Allin)
            {
                lastOpSeat = seat;
                break;
            }
        }

        foreach (var item in playerOp)
        {
            Seat_zjh seatNode = GetPlayerByPlayerID(item.PlayerId);
            if (seatNode == null) continue;
            if (seatNode.SeatId == ((int)currentOption - 1))
            {
                if (item.Option == ZjhOption.Look)
                {
                    seatNode.ShowOperateImage(item.Option);
                }
                continue;
            }
            else
            {
                seatNode.ShowOperateImage(item.Option);
                if (item.Param != "" && float.Parse(item.Param) > 0 && item.PlayerId == lastOpSeat.playerInfo.BasePlayer.PlayerId)
                {
                    seatNode.ShowBetCoin(float.Parse(item.Param));
                }
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

    public List<Seat_zjh> GetSeats()
    {
        return seats.Values.Where(item => item.IsNotEmpty()).ToList();
    }

    public List<Seat_zjh> GetInGameSeats()
    {
        return seats.Values.Where(item => item.IsNotEmpty() && item.playerInfo.InGame).ToList();
    }
    public Seat_zjh GetSelfSeat()
    {
        return seats.Values.FirstOrDefault(item => item.IsNotEmpty() && Util.IsMySelf(item.playerInfo.BasePlayer.PlayerId));
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
            item.Value.SetPos(seatPosDic[item.Value.SeatIndex], isAni, GetAnchorsByIndex(item.Value.SeatIndex));
            //刷新下自己座位里详细的位置
        }
        
        // 如果不需要动画，则不等待
        if (isAni)
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        ZJHProcedure zjhProcedure = GF.Procedure.CurrentProcedure as ZJHProcedure;
        zjhProcedure.GamePanel_zjh.ReShowCountdown();
        zjhProcedure.GamePanel_zjh.ShowZhuang(true);
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
        for (int i = 0; i < seatPos9.Count; i++)
        {
            var seat = transform.GetChild(i).GetComponent<Seat_zjh>();
            seat.transform.localPosition = seatPos9[i];
            seat.gameObject.SetActive(false);
        }
        IsPlayBack = false;
        seats.Clear();
        seatPosDic.Clear();
    }

    /// <summary>
    /// 根据ID查找座位
    /// </summary>
    public Seat_zjh GetPlayerByPos(Position pos)
    {
        int seatId = GameUtil.GetInstance().GetSeatIdByPos(pos);
        return seats.TryGetValue(seatId, out Seat_zjh seat) ? seat : null;
    }
    /// <summary>
    /// 根据PlayerID查找
    /// </summary>
    public Seat_zjh GetPlayerByPlayerID(long playerID)
    {
        return seats.Values.FirstOrDefault(item => item.IsNotEmpty() && item.playerInfo.BasePlayer.PlayerId == playerID);
    }

    /// <summary>
    /// 根据PlayerID查找庄家
    /// </summary>
    public Seat_zjh GetBanker() =>
        seats.Values.FirstOrDefault(seat => seat.IsNotEmpty() && seat.playerInfo.IsBanker);


    /// <summary>
    /// 收到发牌消息
    /// </summary>
    public void Syn_DealCard(DealCard Deal)
    {
        Seat_zjh seatNode = GetPlayerByPos(Deal.Pos);
        if (seatNode == null) return;
        seatNode.playerInfo.IsBanker = Deal.IsBanker;
        seatNode.playerInfo.InGame = true;
    }

    public void ShowPkBoxList(bool isShow)
    {
        ZJHProcedure zjhProcedure = GF.Procedure.CurrentProcedure as ZJHProcedure;
        for (int i = 0; i < seats.Count; i++)
        {
            Seat_zjh player = seats.Values.ToList()[i];
            player.btnPK.SetActive(isShow && player.IsNotEmpty() && zjhProcedure.compareUsers.Contains(player.playerInfo.BasePlayer.PlayerId));
        }
    }

    public void UpdatePlayerInfo(DeskPlayer deskPlayer, bool isRefresh = true)
    {
        Seat_zjh seatNode = GetPlayerByPlayerID(deskPlayer.BasePlayer.PlayerId);
        if (seatNode == null) return;
        seatNode.UpdatePlayerInfo(deskPlayer, isRefresh);
    }

    /// <summary>
    /// 站起
    /// </summary>
    public void Function_SynSitUp(Position pos)
    {
        Seat_zjh seatNode = GetPlayerByPos(pos);
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
        Seat_zjh seatNode = GetPlayerByPlayerID(id);
        if (seatNode == null) return;
        seatNode.playerInfo.Forbid = Forbid;
    }
    /// <summary>
    /// 礼物
    /// </summary>
    public void Function_SynSendGift(long basePlayerID, long toPlayerId, string gift)
    {
        Seat_zjh from = GetPlayerByPlayerID(basePlayerID);
        Seat_zjh to = GetPlayerByPlayerID(toPlayerId);
        if (to == null) return;
        GF.UI.LoadPrefab(AssetsPath.GetPrefab("UI/Chat/" + gift), (gameObject) =>
        {
            if (gameObject == null) return;
            ZJHProcedure zjhProcedure = GF.Procedure.CurrentProcedure as ZJHProcedure;
            if (zjhProcedure.GamePanel_zjh == null) return;
            GameObject giftObj = Instantiate((gameObject as GameObject), zjhProcedure.GamePanel_zjh.transform);
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
        Seat_zjh seatInfo = GetPlayerByPlayerID(basePlayerID);
        if (seatInfo == null) return;
        GF.UI.LoadPrefab(AssetsPath.GetPrefab("UI/Chat/" + chat), (gameObject) =>
        {
            if (gameObject == null) return;
            ZJHProcedure zjhProcedure = GF.Procedure.CurrentProcedure as ZJHProcedure;
            GameObject emoji = Instantiate(gameObject as GameObject, zjhProcedure.GamePanel_zjh.transform);
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
        Seat_zjh seatNode = GetPlayerByPlayerID(ack.PlayerId);
        if (seatNode == null) return;
        seatNode.Function_SynPlayerState(ack.State, ack.LeaveTime);
    }

    /// <summary>
    /// 检查指定玩家是否坐在桌子上
    /// </summary>
    /// <param name="playerId">要检查的玩家ID</param>
    /// <returns>如果玩家在桌子上返回true，否则返回false</returns>
    public bool IsInTable(long playerId)
    {
        foreach (var seat in seats.Values)
        {
            if (seat != null && 
                seat.playerInfo != null && 
                seat.playerInfo.BasePlayer != null && 
                seat.playerInfo.BasePlayer.PlayerId == playerId && 
                seat.playerInfo.Pos != Position.NoSit)
            {
                return true;
            }
        }
        return false;
    }
}

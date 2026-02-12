using NetMsg;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Linq;

/// <summary>
/// 血流麻将的特殊逻辑处理
/// 实现三大功能:
/// 1. THROW_CARD阶段(甩牌) - 选3张牌可为空,无花色限制
/// 2. 连续胡牌 - 使用Msg_MJOption9协议直到牌墙摸完
/// 3. 血流结算 - 使用Msg_XlSettle协议显示特殊结算界面
/// </summary>
public partial class MahjongGameUI
{
    #region 血流成河UI引用

    [Header("===== 血流成河 =====")]
    public GameObject huaZhu;                   // 花猪提示
    public GameObject ShuaiPaiPanel;             // 甩牌面板
    public GameObject XuanShuaiPaiPanel;             // 选择甩牌面板
    public Button BtnShuaiPai;                       // 确认甩牌按钮
    public Button BtnBuShuai;                        // 不甩牌按钮
    public List<GameObject> ShuaiPaiZhongs;          // 甩牌中状态（每个座位）

    // 血流结算玩家条目
    public GameObject GameEnd_XL;                    // 血流结算面板
    public GameObject GameEnd_XL_Bg;                 // 血流结算背景（ShowTableBtn按住时隐藏）
    public Button ShowTableBtn;                      // 显示桌面按钮
    public List<GameObject> XL_PlayerItems;          // 血流结算玩家条目列表

    #endregion

    #region 血流成河私有变量

    // 甩牌选择相关
    private List<MjCard_2D_Interaction> m_SelectedThrowCards = new List<MjCard_2D_Interaction>();
    private const int c_ThrowCardCount = 3;          // 甩牌数量

    #endregion

    #region 甩牌阶段逻辑 (THROW_CARD = 6)

    /// <summary>
    /// 显示甩牌UI（发牌完成后调用）
    /// </summary>
    public void ShowThrowCardUI()
    {
        if (!IsXueLiuRule) return;

        // 设置甩牌状态
        if (mj_procedure != null && mj_procedure.enterMJDeskRs != null)
        {
            mj_procedure.enterMJDeskRs.CkState = MJState.ThrowCard;
        }

        // 重连数据由 BaseMahjongGameManager.RestorePlayersCardData 处理，这里仅用于判断显示状态
        var shuaiPaiData = mj_procedure?.enterMJDeskRs?.ShuaiPai;

        // 显示甩牌阶段面板（一直显示直到甩牌阶段结束）
        ShuaiPaiPanel.SetActive(true);

        // 检查自己是否已经甩牌
        long myPlayerId = Util.GetMyselfInfo().PlayerId;

        bool selfAlreadyThrown = HasPlayerThrown(shuaiPaiData, myPlayerId);
        // 显示甩牌中状态(只显示有玩家的座位，已甩牌的不显示)
        for (int i = 0; i < ShuaiPaiZhongs.Count; i++)
        {
            if (baseMahjongGameManager != null &&
                i < baseMahjongGameManager.deskPlayers.Length &&
                baseMahjongGameManager.deskPlayers[i] != null)
            {
                var player = baseMahjongGameManager.deskPlayers[i];
                bool alreadyThrown = HasPlayerThrown(shuaiPaiData, player.BasePlayer.PlayerId);
                ShuaiPaiZhongs[i].SetActive(!alreadyThrown);
            }
        }

        // XuanShuaiPaiPanel：只要是甩牌阶段就显示，但自己已甩牌则不显示
        if (!selfAlreadyThrown)
        {
            ShowXuanShuaiPaiPanel();
        }
        else
        {
            XuanShuaiPaiPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 显示选择甩牌面板
    /// </summary>
    private void ShowXuanShuaiPaiPanel()
    {
        XuanShuaiPaiPanel.SetActive(true);

        m_SelectedThrowCards.Clear();

        // 确保按钮显示
        BtnShuaiPai.gameObject.SetActive(true);
        BtnShuaiPai.interactable = false; // 初始禁用，需要选3张
        BtnBuShuai.gameObject.SetActive(true);

    }

    /// <summary>
    /// 隐藏甩牌操作面板（自己已完成甩牌）
    /// </summary>
    public void HideThrowCardOperationPanel()
    {
        XuanShuaiPaiPanel.SetActive(false);
        m_SelectedThrowCards.Clear();
    }

    /// <summary>
    /// 检查玩家是否已经甩牌
    /// </summary>
    private bool HasPlayerThrown(Google.Protobuf.Collections.RepeatedField<LongListInt> shuaiPaiData, long playerId)
    {
        if (shuaiPaiData == null || shuaiPaiData.Count == 0) return false;
        
        foreach (var data in shuaiPaiData)
        {
            if (data.Key == playerId)
            {
                return true; // 找到该玩家的甩牌数据
            }
        }
        return false;
    }

    /// <summary>
    /// 手牌点击选择甩牌（由MahjongSeat_2D回调）
    /// 甩牌与换牌不同：可以选择不同花色
    /// </summary>
    public void OnThrowCardSelect(MjCard_2D_Interaction card)
    {
        if (!IsXueLiuRule) return;

        // 检查是否已选择
        if (m_SelectedThrowCards.Contains(card))
        {
            // 取消选择
            m_SelectedThrowCards.Remove(card);
        }
        else
        {
            // 检查是否达到上限
            if (m_SelectedThrowCards.Count >= c_ThrowCardCount)
            {
                GF.UI.ShowToast("最多只能选择3张牌!");
                card.RestorePosition(true);
                return;
            }

            // 甩牌可以选择不同花色，无需验证花色
            m_SelectedThrowCards.Add(card);
        }

        // 更新确认按钮状态
        UpdateThrowCardButton();
    }

    /// <summary>
    /// 更新甩牌确认按钮状态
    /// </summary>
    private void UpdateThrowCardButton()
    {
        BtnShuaiPai.interactable = m_SelectedThrowCards.Count == c_ThrowCardCount;
    }

    /// <summary>
    /// 确认甩牌按钮点击
    /// </summary>
    public void OnConfirmThrowCard()
    {
        if (m_SelectedThrowCards.Count != c_ThrowCardCount)
        {
            GF.UI.ShowToast($"请选择{c_ThrowCardCount}张牌");
            return;
        }

        // 发送甩牌请求
        var values = m_SelectedThrowCards.Select(c => c.cardValue).ToList();
        SendThrowCardRequest(values);

        // 清空选择状态
        m_SelectedThrowCards.Clear();

        // 隐藏操作面板
        HideThrowCardOperationPanel();

    }

    /// <summary>
    /// 不甩牌按钮点击
    /// </summary>
    public void OnSkipThrowCard()
    {

        // 清空选择状态并恢复所有牌的位置
        // 创建副本遍历，防止在移除过程中出现问题
        var cardsToRestore = new List<MjCard_2D_Interaction>(m_SelectedThrowCards);
        foreach (var card in cardsToRestore)
        {
            if (card != null && card.gameObject != null && card.gameObject.activeInHierarchy)
            {
                card.RestorePosition(true);
            }
        }
        m_SelectedThrowCards.Clear();

        // 发送空甩牌请求
        SendThrowCardRequest(new List<int>());

        // 隐藏操作面板
        HideThrowCardOperationPanel();

    }

    /// <summary>
    /// 发送甩牌请求
    /// </summary>
    private void SendThrowCardRequest(List<int> cards)
    {
        Msg_ThrowCardRq req = new Msg_ThrowCardRq();
        if (cards != null)
        {
            foreach (var card in cards)
            {
                req.Card.Add(card);
            }
        }
        mj_procedure?.Send_ThrowCardRq(req);
    }

    /// <summary>
    /// 处理甩牌响应
    /// </summary>
    public void HandleSynThrowCardRs(Syn_ThrowCardRs msg)
    {
        if (msg == null) return;

        // 通过PlayerId查找座位索引
        int seatIdx = baseMahjongGameManager?.GetSeatIndexByPlayerId(msg.PlayerId) ?? -1;
        if (seatIdx < 0) return;

        // 如果是自己完成甩牌，隐藏自己的操作面板
        if (seatIdx == 0)
        {
            HideThrowCardOperationPanel();
        }

        // 更新甩牌状态显示
        UpdateThrowCardPlayerState(seatIdx, true);

        var seat = GetSeat(seatIdx);
        if (seat == null) return;

        bool isSelf = seatIdx == 0;
        var throwCards = msg.Card.ToList();

        if (throwCards.Count > 0)
        {
            if (isSelf)
            {
                // 自己：在组牌区显示实际甩牌牌值
                ShowThrowCardsInMeldContainer(seat, throwCards, true);

                // 从手牌移除甩出的牌
                RemoveThrowCardsFromHand(seat, throwCards);

                // 整理手牌
                seat.ArrangeHandCards();
            }
            else
            {
                // 其他玩家：在组牌区显示三张背牌
                ShowThrowCardsInMeldContainer(seat, throwCards, false);

                // 删除其他玩家的前N张手牌（甩牌是永久移除，不是临时隐藏）
                RemoveHandCardsForOtherPlayer(seat, throwCards.Count);

                // 整理手牌
                seat.ArrangeHandCards();
            }

        }
        else
        {
            // 选择不甩牌时也需要整理手牌位置
            seat.ArrangeHandCards();
        }

        // 检查是否所有玩家都完成甩牌，如果是则隐藏甩牌面板并启动庄家出牌
        CheckAllPlayersThrowCardComplete();
    }

    /// <summary>
    /// 检查是否所有玩家都完成甩牌
    /// </summary>
    private void CheckAllPlayersThrowCardComplete()
    {
        // 检查所有甩牌中状态是否都已隐藏（表示所有玩家都完成了甩牌）
        bool allComplete = true;
        for (int i = 0; i < ShuaiPaiZhongs.Count; i++)
        {
            if (baseMahjongGameManager != null &&
                i < baseMahjongGameManager.deskPlayers.Length &&
                baseMahjongGameManager.deskPlayers[i] != null)
            {
                if (ShuaiPaiZhongs[i].activeSelf)
                {
                    allComplete = false;
                    break;
                }
            }
        }

        if (allComplete)
        {
            OnThrowCardComplete();
        }
    }

    /// <summary>
    /// 甩牌阶段完成后的处理（启动庄家打牌）
    /// </summary>
    private void OnThrowCardComplete()
    {
        // 隐藏甩牌阶段面板
        ShuaiPaiPanel.SetActive(false);

        // 设置游戏状态为MjPlay
        if (mj_procedure != null && mj_procedure.enterMJDeskRs != null)
        {
            mj_procedure.enterMJDeskRs.CkState = MJState.MjPlay;
        }

        // 启动庄家打牌倒计时（与换牌结束逻辑一致）
        StartBankerPlayCountdown();

    }

    /// <summary>
    /// 更新玩家的甩牌状态
    /// </summary>
    public void UpdateThrowCardPlayerState(int seatIdx, bool isFinished)
    {
        ShuaiPaiZhongs[seatIdx].SetActive(!isFinished);

    }

    /// <summary>
    /// 在组牌区显示甩牌
    /// </summary>
    /// <param name="seat">座位</param>
    /// <param name="cards">牌值列表</param>
    /// <param name="showFace">是否显示牌面（自己显示，其他人显示背牌）</param>
    private void ShowThrowCardsInMeldContainer(MahjongSeat_2D seat, List<int> cards, bool showFace)
    {
        if (seat == null || seat.MeldContainer == null || cards == null || cards.Count == 0)
            return;

        if (showFace)
        {
            // 自己：显示实际甩牌牌值（使用AddShuaiPaiGroup添加到组牌区，标记为甩牌）
            seat.AddShuaiPaiGroup(cards);
        }
        else
        {
            // 其他玩家：显示背牌（甩牌）
            seat.ShowBackCardsInMeldContainer(cards.Count, true);
        }
    }

    /// <summary>
    /// 从手牌移除甩出的牌（庄家需要同时检查摸牌区）
    /// </summary>
    private void RemoveThrowCardsFromHand(MahjongSeat_2D seat, List<int> throwCards)
    {
        if (seat == null || seat.HandContainer == null || throwCards == null)
            return;

        List<int> cardsToRemove = new List<int>(throwCards);
        int removedFromHand = 0;
        int removedFromMoPai = 0;

        // 首先从摸牌区检查并移除（庄家有14张牌，其中1张在摸牌区）
        if (seat.MoPaiContainer != null && seat.MoPaiContainer.childCount > 0 && cardsToRemove.Count > 0)
        {
            for (int i = seat.MoPaiContainer.childCount - 1; i >= 0 && cardsToRemove.Count > 0; i--)
            {
                Transform cardTrans = seat.MoPaiContainer.GetChild(i);
                MjCard_2D mjCard = cardTrans.GetComponent<MjCard_2D>();
                if (mjCard != null)
                {
                    int cardValue = Util.ConvertFaceValueToServerInt(mjCard.mahjongFaceValue);
                    if (cardsToRemove.Contains(cardValue))
                    {
                        cardsToRemove.Remove(cardValue);
                        DestroyImmediate(cardTrans.gameObject);
                        removedFromMoPai++;
                    }
                }
            }
        }

        // 然后从手牌区移除剩余的牌
        for (int i = seat.HandContainer.childCount - 1; i >= 0 && cardsToRemove.Count > 0; i--)
        {
            Transform cardTrans = seat.HandContainer.GetChild(i);
            MjCard_2D mjCard = cardTrans.GetComponent<MjCard_2D>();
            if (mjCard != null)
            {
                int cardValue = Util.ConvertFaceValueToServerInt(mjCard.mahjongFaceValue);
                if (cardsToRemove.Contains(cardValue))
                {
                    cardsToRemove.Remove(cardValue);
                    DestroyImmediate(cardTrans.gameObject);
                    removedFromHand++;
                }
            }
        }

    }

    /// <summary>
    /// 删除其他玩家前N张手牌（用于甩牌等永久移除场景）
    /// 与HideCardsForOtherPlayer不同，此方法会真正删除牌对象
    /// </summary>
    private void RemoveHandCardsForOtherPlayer(MahjongSeat_2D seat, int count)
    {
        if (seat == null || seat.HandContainer == null || count <= 0)
            return;

        int removedCount = 0;
        // 从前往后删除指定数量的手牌
        for (int i = 0; i < count && seat.HandContainer.childCount > 0; i++)
        {
            Transform cardTransform = seat.HandContainer.GetChild(0);
            if (cardTransform != null)
            {
                DestroyImmediate(cardTransform.gameObject);
                removedCount++;
            }
        }

    }

    /// <summary>
    /// 还原甩牌数据（重连时调用）
    /// LongListInt格式: Key=PlayerId, Vals=甩牌列表
    /// 自己显示实际牌值，其他玩家显示对应数量背牌
    /// </summary>
    private void RestoreThrowCardData(Google.Protobuf.Collections.RepeatedField<LongListInt> shuaiPaiData)
    {
        if (shuaiPaiData == null || shuaiPaiData.Count == 0) return;

        long myPlayerId = Util.GetMyselfInfo().PlayerId;

        foreach (var data in shuaiPaiData)
        {
            long playerId = data.Key;
            var throwCards = data.Vals?.ToList();

            if (throwCards == null || throwCards.Count == 0)
            {
                continue;
            }

            // 获取座位索引
            int seatIdx = baseMahjongGameManager?.GetSeatIndexByPlayerId(playerId) ?? -1;
            if (seatIdx < 0) continue;

            var seat = GetSeat(seatIdx);
            if (seat == null) continue;

            bool isSelf = playerId == myPlayerId;

            // 在组牌区显示甩牌
            if (isSelf)
            {
                // 自己：显示实际甩牌牌值
                ShowThrowCardsInMeldContainer(seat, throwCards, true);
                // 从手牌移除（重连时手牌数据已经是减去甩牌后的）
                // 所以这里不需要再次移除
            }
            else
            {
                // 其他玩家：显示背牌
                ShowThrowCardsInMeldContainer(seat, throwCards, false);
            }
        }
    }

    #endregion

    #region 连续胡牌逻辑 (Msg_MJOption9 = 2008025)

    /// <summary>
    /// 处理血流连续胡牌通知 (2008025)
    /// 这是血流有人胡牌的通知，需要显示胡牌效果和积分变化
    /// 按钮显示由通用消息处理，此接口只处理胡牌特效
    /// </summary>
    public void HandleXueLiuHuOption(Msg_MJOption msg)
    {
        if (msg == null || !IsXueLiuRule) return;

        // 获取胡牌玩家ID（从OptionPlayer字段）
        long playerId = msg.OptionPlayer?.PlayerId ?? 0;
        if (playerId == 0) return;

        // 获取放铳玩家ID（从Form字段，牌的来源）
        long providerPlayerId = msg.Form?.PlayerId ?? 0;
        
        var huTypes = (msg.Types_ != null && msg.Types_.Val.Count > 0) ? msg.Types_.Val.ToList() : null;
        GF.LogInfo_gsc("[血流胡牌]", $"牌:{msg.Card} 番型数:{huTypes?.Count ?? 0} 积分变化数:{msg.ScoreChange.Count}");

        // 获取胡牌玩家的座位索引
        int seatIdx = baseMahjongGameManager?.GetSeatIndexByPlayerId(playerId) ?? -1;
        if (seatIdx < 0) return;

        // 获取放铳玩家的座位索引（相对于胡牌玩家用于显示Point方向，绝对座位用于移除弃牌）
        int providerSeatIdx = -1; // -1表示自摸（相对座位，用于Point显示）
        int providerAbsSeatIdx = -1; // -1表示自摸（绝对座位，用于移除弃牌）
        if (providerPlayerId > 0 && providerPlayerId != playerId)
        {
            // 获取放铳玩家的绝对座位索引
            providerAbsSeatIdx = baseMahjongGameManager?.GetSeatIndexByPlayerId(providerPlayerId) ?? -1;
            if (providerAbsSeatIdx >= 0)
            {
                // 计算放铳玩家相对于胡牌玩家的方向（0:自己 1:上家 2:对家 3:下家）
                // 注意：这里需要计算相对于胡牌玩家的座位位置
                int playerCount = GetRealPlayerCount();
                providerSeatIdx = (providerAbsSeatIdx - seatIdx + playerCount) % playerCount;
            }
        }

        // 停止倒计时
        Start2DCountdown(0);

        // 移除胡牌对应的牌（类似碰杠吃）
        // 自摸：移除胡牌玩家摸牌区的牌
        // 放铳：移除放铳玩家弃牌区的最后一张牌（使用绝对座位索引）
        RemoveXueLiuHuCard(seatIdx, msg.Card, providerAbsSeatIdx);

        // 显示胡牌效果和播放音效（传入放铳玩家绝对座位索引）
        ShowXueLiuHuEffect(seatIdx, msg.Card, playerId, providerPlayerId, providerAbsSeatIdx, huTypes);

        // 获取当前玩家的胡牌次数（用于确定huIndex）
        var seat = GetSeat(seatIdx);
        int huIndex = seat?.HuContainer?.childCount ?? 0;
        
        // 添加胡牌到HuContainer（传入放铳玩家座位信息）
        AddXueLiuHuCard(seatIdx, msg.Card, huIndex, providerSeatIdx);

        // 显示积分变化效果
        ShowXueLiuScoreChange(msg.ScoreChange);
    }

    /// <summary>
    /// 移除血流胡牌对应的牌（类似碰杠吃）
    /// 自摸：移除胡牌玩家摸牌区的牌
    /// 放铳：移除放铳玩家弃牌区的最后一张牌
    /// </summary>
    /// <param name="seatIdx">胡牌玩家座位索引（绝对座位）</param>
    /// <param name="huCard">胡的牌值</param>
    /// <param name="providerAbsSeatIdx">放铳玩家绝对座位索引，-1表示自摸</param>
    private void RemoveXueLiuHuCard(int seatIdx, int huCard, int providerAbsSeatIdx)
    {
        if (providerAbsSeatIdx < 0)
        {
            // 自摸：优先从摸牌区移除，如果没有则从手牌移除（参考出牌逻辑）
            var seat = GetSeat(seatIdx);
            if (seat == null) return;

            bool removedFromMoPai = false;

            // 优先从摸牌区移除
            if (seat.MoPaiContainer != null && seat.MoPaiContainer.childCount > 0)
            {
                for (int i = seat.MoPaiContainer.childCount - 1; i >= 0; i--)
                {
                    Transform cardTrans = seat.MoPaiContainer.GetChild(i);
                    MjCard_2D mjCard = cardTrans.GetComponent<MjCard_2D>();
                    if (mjCard != null && mjCard.cardValue == huCard)
                    {
                        DestroyImmediate(cardTrans.gameObject);
                        removedFromMoPai = true;
                        break;
                    }
                }
            }

            // 如果摸牌区没有，从手牌移除
            if (!removedFromMoPai && seat.HandContainer != null && seat.HandContainer.childCount > 0)
            {
                for (int i = seat.HandContainer.childCount - 1; i >= 0; i--)
                {
                    Transform cardTrans = seat.HandContainer.GetChild(i);
                    MjCard_2D mjCard = cardTrans.GetComponent<MjCard_2D>();
                    if (mjCard != null && mjCard.cardValue == huCard)
                    {
                        DestroyImmediate(cardTrans.gameObject);
                        break;
                    }
                }
            }
        }
        else
        {
            // 放铳：移除放铳玩家弃牌区的最后一张牌（使用绝对座位索引）
            var providerSeat = GetSeat(providerAbsSeatIdx);
            if (providerSeat != null && providerSeat.DiscardContainer != null && providerSeat.DiscardContainer.childCount > 0)
            {
                // 使用座位的GetNewestDiscardIndex方法，考虑不同座位的倒序情况
                int lastIndex = providerSeat.GetNewestDiscardIndex();
                if (lastIndex >= 0)
                {
                    Transform lastCard = providerSeat.DiscardContainer.GetChild(lastIndex);
                    if (lastCard != null)
                    {
                        DestroyImmediate(lastCard.gameObject);
                    }
                }
            }

            // 隐藏弃牌标记（弃牌已被胡走）
            HideDiscardFlag();
        }
    }

    /// <summary>
    /// 显示血流胡牌特效和播放音效
    /// 血流成河强制使用普通话（通过MahjongAudio.IsForceMandarin统一处理）
    /// 基础胡(屁胡)：播放"hu"语音 + 基础胡牌特效（自摸RuanMo/点炮ZhuoChong）
    /// 清一色/碰碰胡：只播放牌型特效和语音，不播放基础胡牌特效
    /// 清一色+碰碰胡：连续播放两个牌型特效
    /// </summary>
    /// <param name="seatIdx">胡牌玩家座位索引</param>
    /// <param name="huCard">胡的牌值</param>
    /// <param name="huPlayerId">胡牌玩家ID</param>
    /// <param name="providerPlayerId">放铳玩家ID，0表示自摸</param>
    /// <param name="providerAbsSeatIdx">放铳玩家绝对座位索引，-1表示自摸</param>
    private void ShowXueLiuHuEffect(int seatIdx, int huCard, long huPlayerId, long providerPlayerId, int providerAbsSeatIdx, IReadOnlyList<int> huTypes = null)
    {
        var seat = GetSeat(seatIdx);
        if (seat == null) return;

        bool isZiMo = (providerAbsSeatIdx < 0);
        var mjMethod = mj_procedure?.enterMJDeskRs?.BaseInfo?.MjConfig?.MjMethod ?? NetMsg.MJMethod.Xl;

        // 使用服务器下发的胡牌类型（types字段与fan字段格式相同，都是番型编码列表）
        var fanList = new Google.Protobuf.Collections.RepeatedField<long>();
        if (huTypes != null && huTypes.Count > 0)
        {
            foreach (var code in huTypes)
            {
                fanList.Add(code);
            }
        }

        // 复用卡五星的番型特效获取方法（血流和卡五星的番型编码规则相同）
        List<string> huTypeEffects = GetKWXHuTypeEffectsFromFan(fanList);

        if(!isZiMo){
            var providerSeat = GetSeat(providerAbsSeatIdx);
            if(providerSeat != null){
                providerSeat.ShowEffect("DianPao", 1.5f);
            }
        }

        // 根据牌型决定特效和音效
        if (huTypeEffects.Count == 0)
        {
            string effectType = isZiMo ? "ZiMo" : "Hu";
            seat.ShowEffect(effectType, 1.5f);
            GF.LogInfo($"[血流] 座位{seatIdx}显示基础胡牌特效: {effectType}");
        }
        else
        {
            PlayXueLiuHuTypeEffectsAsync(seat, huPlayerId, mjMethod, huTypeEffects).Forget();
        }

        GF.LogInfo($"[血流] 座位{seatIdx}胡牌: 牌值={huCard}, 是否自摸={isZiMo}");
    }

    /// <summary>
    /// 异步播放血流胡牌类型特效（清一色、碰碰胡）
    /// 清一色+碰碰胡时连续播放两个特效
    /// </summary>
    private async UniTaskVoid PlayXueLiuHuTypeEffectsAsync(MahjongSeat_2D seat, long huPlayerId, NetMsg.MJMethod mjMethod, IReadOnlyList<string> effectNames)
    {
        if (seat == null || effectNames == null || effectNames.Count == 0)
        {
            return;
        }

        for (int i = 0; i < effectNames.Count; i++)
        {
            var effectName = effectNames[i];
            // 播放牌型特效
            seat.ShowEffect(effectName, 1.5f);
            
            // 播放牌型语音
            MahjongAudio.PlayHuPaiTypeForPlayer(effectName, huPlayerId, mjMethod);
            
            // 如果有多个特效，等待一段时间后播放下一个
            if (effectNames.Count > 1 && i < effectNames.Count - 1)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(0.8));
            }
        }
    }

    /// <summary>
    /// 获取血流胡牌玩家的所有牌（手牌+副露+胡牌）
    /// </summary>
    private List<MahjongFaceValue> GetXueLiuPlayerAllCards(int seatIdx, int huCard)
    {
        var allCards = new List<MahjongFaceValue>();
        var seat = GetSeat(seatIdx);
        if (seat == null) return allCards;

        // 添加手牌
        if (seat.HandContainer != null)
        {
            for (int i = 0; i < seat.HandContainer.childCount; i++)
            {
                var mjCard = seat.HandContainer.GetChild(i).GetComponent<MjCard_2D>();
                if (mjCard != null && mjCard.cardValue > 0)
                {
                    var faceValue = Util.ConvertServerIntToFaceValue(mjCard.cardValue);
                    if (faceValue != MahjongFaceValue.MJ_UNKNOWN)
                    {
                        allCards.Add(faceValue);
                    }
                }
            }
        }

        // 添加摸牌区的牌
        if (seat.MoPaiContainer != null)
        {
            for (int i = 0; i < seat.MoPaiContainer.childCount; i++)
            {
                var mjCard = seat.MoPaiContainer.GetChild(i).GetComponent<MjCard_2D>();
                if (mjCard != null && mjCard.cardValue > 0)
                {
                    var faceValue = Util.ConvertServerIntToFaceValue(mjCard.cardValue);
                    if (faceValue != MahjongFaceValue.MJ_UNKNOWN)
                    {
                        allCards.Add(faceValue);
                    }
                }
            }
        }

        // 添加副露区的牌（碰、杠）- 跳过甩牌（如果有的话）
        if (seat.MeldContainer != null)
        {
            // 检查是否启用了甩牌
            var xlConfig = mj_procedure?.enterMJDeskRs?.BaseInfo?.MjConfig?.XlConfig;
            bool hasShuaiPai = xlConfig?.Check != null && xlConfig.Check.Contains(3);
            int skipCount = hasShuaiPai ? 3 : 0; // 甩牌的3张需要跳过
            int cardIndex = 0;
            
            for (int i = 0; i < seat.MeldContainer.childCount; i++)
            {
                var mjCard = seat.MeldContainer.GetChild(i).GetComponent<MjCard_2D>();
                if (mjCard != null && mjCard.cardValue > 0)
                {
                    cardIndex++;
                    // 跳过甩牌
                    if (cardIndex <= skipCount)
                        continue;
                        
                    var faceValue = Util.ConvertServerIntToFaceValue(mjCard.cardValue);
                    if (faceValue != MahjongFaceValue.MJ_UNKNOWN)
                    {
                        allCards.Add(faceValue);
                    }
                }
            }
        }

        // 添加胡的牌
        if (huCard > 0)
        {
            var huFaceValue = Util.ConvertServerIntToFaceValue(huCard);
            if (huFaceValue != MahjongFaceValue.MJ_UNKNOWN)
            {
                allCards.Add(huFaceValue);
            }
        }

        return allCards;
    }

    /// <summary>
    /// 检查血流胡牌是否清一色
    /// </summary>
    private bool CheckXueLiuQingYiSe(List<MahjongFaceValue> cards)
    {
        if (cards.Count == 0) return false;
        
        var wanCards = cards.Where(c => c >= MahjongFaceValue.MJ_WANG_1 && c <= MahjongFaceValue.MJ_WANG_9).ToList();
        var tiaoCards = cards.Where(c => c >= MahjongFaceValue.MJ_TIAO_1 && c <= MahjongFaceValue.MJ_TIAO_9).ToList();
        var tongCards = cards.Where(c => c >= MahjongFaceValue.MJ_TONG_1 && c <= MahjongFaceValue.MJ_TONG_9).ToList();

        return wanCards.Count == cards.Count || 
               tiaoCards.Count == cards.Count || 
               tongCards.Count == cards.Count;
    }

    /// <summary>
    /// 检查血流胡牌是否碰碰胡
    /// 碰碰胡：所有面子都是刻子（3张相同）或杠（4张相同），加一个对子
    /// </summary>
    private bool CheckXueLiuPengPengHu(List<MahjongFaceValue> cards)
    {
        if (cards.Count == 0) return false;

        var cardGroups = cards.GroupBy(c => c).ToList();
        
        int tripletCount = 0;
        int pairCount = 0;

        foreach (var group in cardGroups)
        {
            int count = group.Count();
            if (count == 3)
            {
                tripletCount++;
            }
            else if (count == 4)
            {
                tripletCount++; // 杠算一个刻子
            }
            else if (count == 2)
            {
                pairCount++;
            }
            else if (count == 1)
            {
                return false; // 有单张，不是碰碰胡
            }
        }

        // 碰碰胡：4个刻子 + 1个对子
        return tripletCount >= 4 && pairCount == 1;
    }

    /// <summary>
    /// 显示血流积分变化效果
    /// </summary>
    private void ShowXueLiuScoreChange(Google.Protobuf.Collections.RepeatedField<LongDouble> scoreChange)
    {
        if (scoreChange == null || scoreChange.Count == 0) return;

        foreach (var change in scoreChange)
        {
            long playerId = change.Key;
            double score = change.Val;

            int seatIdx = baseMahjongGameManager?.GetSeatIndexByPlayerId(playerId) ?? -1;
            if (seatIdx >= 0 && seatIdx < 4)
            {
                // 显示积分变化动画
                CoinChange(seatIdx, score);
            }
        }
    }

    /// <summary>
    /// 血流胡牌成功后，添加胡牌到HuContainer
    /// </summary>
    /// <param name="seatIdx">座位索引</param>
    /// <param name="huCard">胡的牌值</param>
    /// <param name="huIndex">第几次胡（从0开始）</param>
    /// <param name="providerSeatIdx">放铳玩家座位索引（相对于胡牌玩家）：0自己,1上家,2对家,3下家；-1表示自摸</param>
    public void AddXueLiuHuCard(int seatIdx, int huCard, int huIndex, int providerSeatIdx = -1)
    {
        if (!IsXueLiuRule) return;

        var seat = GetSeat(seatIdx);
        if (seat == null) return;

        // 调用座位的添加胡牌方法（传入放铳玩家座位信息）
        seat.AddXueLiuHuCard(huCard, huIndex, providerSeatIdx);

        // 如果是自己胡牌，移除所有听牌标记并将手牌变暗
        if (seatIdx == 0)
        {
            ClearAllTingMarks();
            DimHandCardsAfterXueLiuHu();
        }
    }



    #endregion

    #region 血流结算逻辑 (Msg_XlSettle = 2008026)

    /// <summary>
    /// 显示血流结算面板
    /// </summary>
    /// <param name="msg">血流结算消息</param>
    /// <param name="players">玩家列表</param>
    public void ShowXueLiuSettlePanel(Msg_XlSettle msg, List<DeskPlayer> players)
    {
        if (msg == null || !IsXueLiuRule) return;

        // 显示血流结算面板
        if (GameEnd_XL != null)
            GameEnd_XL.SetActive(true);

        // 绑定ShowTableBtn事件
        InitShowTableBtn();

        // 填充玩家结算信息
        FillXueLiuSettlePlayerInfo(msg, players);

        // 更新局数
        Update2DRoomInfo();

        // 判断是否最后一局
        bool isLastGame = mj_procedure.brecord || 
            mj_procedure.enterMJDeskRs.GameNumbers >= mj_procedure.enterMJDeskRs.BaseInfo.BaseConfig.PlayerTime;

        // 显示对应按钮
        var nextBtn = GameEnd_XL.transform.Find("Bg/NextBtn").gameObject;
        var recordBtn = GameEnd_XL.transform.Find("Bg/RecordBtn").gameObject;
        var countDownText = GameEnd_XL.transform.Find("Bg/NextBtn/CountDown").GetComponent<Text>();

        if (isLastGame)
        {
            recordBtn.SetActive(true);
            nextBtn.SetActive(false);
        }
        else
        {
            recordBtn.SetActive(false);
            nextBtn.SetActive(true);

            // 启动倒计时
            CoroutineRunner.Instance.StartCountdown(55, countDownText, GameEnd_XL, 
                () => OnXueLiuNextClick(), "ss");
        }

        // 播放结算音效
        bool isSelfWin = IsSelfWinInXueLiu(msg);
        Sound.PlayEffect(isSelfWin ? AudioKeys.MJ_EFFECT_AUDIO_GAME_WIN : AudioKeys.MJ_EFFECT_AUDIO_GAME_LOST);
    }

    /// <summary>
    /// 判断自己是否在血流结算中赢钱
    /// </summary>
    private bool IsSelfWinInXueLiu(Msg_XlSettle msg)
    {
        if (msg == null || msg.Score == null) return false;

        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        foreach (var scoreItem in msg.Score)
        {
            if (scoreItem.Key == myPlayerId && scoreItem.Val > 0)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 填充血流结算玩家信息
    /// </summary>
    private void FillXueLiuSettlePlayerInfo(Msg_XlSettle msg, List<DeskPlayer> players)
    {
        if (XL_PlayerItems == null || XL_PlayerItems.Count == 0) return;

        // 遍历玩家条目
        for (int i = 0; i < XL_PlayerItems.Count; i++)
        {
            GameObject playerItem = XL_PlayerItems[i];
            if (playerItem == null) continue;

            // 检查是否有对应的玩家数据
            if (i >= players.Count)
            {
                playerItem.SetActive(false);
                continue;
            }

            playerItem.SetActive(true);
            DeskPlayer player = players[i];
            if (player == null)
            {
                playerItem.SetActive(false);
                continue;
            }

            // 设置玩家基本信息
            var nameText = playerItem.transform.Find("Name").GetComponent<Text>();
            nameText.FormatNickname(player.BasePlayer.Nick);

            var idText = playerItem.transform.Find("ID").GetComponent<Text>();
            idText.text = player.BasePlayer.PlayerId.ToString();

            // 设置头像
            var headImage = playerItem.transform.Find("head").GetComponent<RawImage>();
            if (!string.IsNullOrEmpty(player.BasePlayer.HeadImage))
            {
                Util.DownloadHeadImage(headImage, player.BasePlayer.HeadImage);
            }

            // 获取该玩家的分数变化
            double scoreChange = 0;
            foreach (var scoreItem in msg.Score)
            {
                if (scoreItem.Key == player.BasePlayer.PlayerId)
                {
                    scoreChange = scoreItem.Val;
                    break;
                }
            }

            // 设置输赢分数
            var winScoreText = playerItem.transform.Find("WinScore").GetComponent<Text>();
            var loseScoreText = playerItem.transform.Find("LoseScore").GetComponent<Text>();
            if (scoreChange > 0)
            {
                winScoreText.text = "+" + scoreChange.ToString();
                loseScoreText.text = "";
            }
            else
            {
                loseScoreText.text = scoreChange.ToString();
                winScoreText.text = "";
            }

            // 填充详细结算信息（胡牌方式、责任人、番数、分数）
            if(i == 0){
                FillXueLiuSettleDetail(playerItem, msg);
            }
        }
    }

    /// <summary>
    /// 填充血流结算详情
    /// </summary>
    private void FillXueLiuSettleDetail(GameObject playerItem, Msg_XlSettle msg)
    {
        // 获取Scroll/Viewport/Content容器
        var scrollContent = playerItem.transform.Find("Scroll/Viewport/Content");

        // 获取Item模板
        var itemTemplate = playerItem.transform.Find("Item").gameObject;

        // 清空现有子对象（保留模板）
        for (int i = scrollContent.childCount - 1; i >= 0; i--)
        {
            Transform child = scrollContent.GetChild(i);
            Destroy(child.gameObject);
        }

        // 查找该玩家的所有结算记录
        int itemIndex = 0;
        foreach (var settle in msg.XlSettle)
        {
            // 这里需要根据实际协议判断是否属于当前玩家
            // 假设XlSettle是按玩家顺序排列的
            if (itemIndex >= msg.XlSettle.Count) break;

            // 创建结算条目
            GameObject item = Instantiate(itemTemplate, scrollContent);
            item.SetActive(true);
            item.name = $"SettleItem_{itemIndex}";

            // 设置胡牌方式
            var typeText = item.transform.Find("Type").GetComponent<Text>();
            typeText.text = GetWinWayText(settle.WinWay);

            // 设置责任人
            var posText = item.transform.Find("Pos").GetComponent<Text>();
            posText.text = GetLiabilityText(settle.Liability);

            // 设置番数
            var fanText = item.transform.Find("Fan").GetComponent<Text>();
            fanText.text = settle.Fan.ToString();

            // 设置分数
            var scoreText = item.transform.Find("Score").GetComponent<Text>();
            string prefix = settle.Score > 0 ? "+" : "";
            scoreText.text = prefix + settle.Score.ToString();

            itemIndex++;
        }
    }

    /// <summary>
    /// 获取胡牌方式文本
    /// </summary>
    private string GetWinWayText(int winWay)
    {
        switch (winWay)
        {
            case 0: return "自摸";
            case 1: return "被自摸";
            case 2: return "点炮";
            case 3: return "被点炮";
            default: return "";
        }
    }

    /// <summary>
    /// 获取责任人文本
    /// </summary>
    private string GetLiabilityText(int liability)
    {
        switch (liability)
        {
            case 0: return "自己";
            case 1: return "上家";
            case 2: return "对家";
            case 3: return "下家";
            default: return "";
        }
    }

    /// <summary>
    /// 初始化ShowTableBtn按住隐藏Bg事件
    /// </summary>
    private void InitShowTableBtn()
    {
        // 添加事件监听
        EventTriggerListener.Get(ShowTableBtn.gameObject).onDown = OnShowTableBtnDown;
        EventTriggerListener.Get(ShowTableBtn.gameObject).onUp = OnShowTableBtnUp;
    }

    /// <summary>
    /// ShowTableBtn按下 - 隐藏Bg
    /// </summary>
    private void OnShowTableBtnDown(GameObject go)
    {
        GameEnd_XL_Bg.SetActive(false);
    }

    /// <summary>
    /// ShowTableBtn松开 - 显示Bg
    /// </summary>
    private void OnShowTableBtnUp(GameObject go)
    {
        GameEnd_XL_Bg.SetActive(true);
    }

    /// <summary>
    /// 血流下一局按钮点击
    /// </summary>
    public void OnXueLiuNextClick()
    {
        HideXueLiuSettlePanel();
        mj_procedure.Send_MJPlayerReadyRq(true);
    }

    /// <summary>
    /// 血流战绩按钮点击
    /// </summary>
    public void OnXueLiuRecordClick()
    {
        ShowGameRecordPanel();
    }

    /// <summary>
    /// 隐藏血流结算面板
    /// </summary>
    public void HideXueLiuSettlePanel()
    {
        // 停止倒计时
        CoroutineRunner.Instance.StopCountdown(GameEnd_XL);
        GameEnd_XL.SetActive(false);

        ResetGameUI();
    }

    #endregion

    #region 血流重连数据还原

    /// <summary>
    /// 还原血流胡牌数据
    /// LongListLongInt格式: Key=胡牌玩家ID, Vals=LongInt列表(每个LongInt: Key=放铳玩家ID, Val=胡的牌)
    /// </summary>
    public void RestoreXueLiuHuInfo(Google.Protobuf.Collections.RepeatedField<LongListLongInt> huInfoData)
    {
        if (!IsXueLiuRule || huInfoData == null || huInfoData.Count == 0) return;

        foreach (var huInfo in huInfoData)
        {
            long huPlayerId = huInfo.Key; // 胡牌玩家ID
            var huCards = huInfo.Vals;    // LongInt列表: Key=放铳玩家ID, Val=胡的牌

            if (huCards == null || huCards.Count == 0) continue;

            // 获取胡牌玩家的座位索引
            int seatIdx = baseMahjongGameManager?.GetSeatIndexByPlayerId(huPlayerId) ?? -1;
            if (seatIdx < 0) continue;

            var seat = GetSeat(seatIdx);
            if (seat == null) continue;

            int playerCount = GetRealPlayerCount();

            // 还原每次胡牌
            for (int i = 0; i < huCards.Count; i++)
            {
                var huCardInfo = huCards[i];
                long providerPlayerId = huCardInfo.Key; // 放铳玩家ID (0表示自摸)
                int huCard = huCardInfo.Val;            // 胡的牌

                // 计算放铳玩家相对于胡牌玩家的方向（0:自己 1:上家 2:对家 3:下家）
                int providerSeatIdx = -1; // -1表示自摸
                if (providerPlayerId > 0 && providerPlayerId != huPlayerId)
                {
                    int providerAbsSeatIdx = baseMahjongGameManager?.GetSeatIndexByPlayerId(providerPlayerId) ?? -1;
                    if (providerAbsSeatIdx >= 0)
                    {
                        providerSeatIdx = (providerAbsSeatIdx - seatIdx + playerCount) % playerCount;
                    }
                }

                seat.AddXueLiuHuCard(huCard, i, providerSeatIdx);
            }

            // 如果是自己胡牌，需要将手牌变暗
            if (seatIdx == 0 && huCards.Count > 0)
            {
                DimHandCardsAfterXueLiuHu();
            }
        }
    }

    #endregion

    #region 血流胡牌后手牌变暗

    /// <summary>
    /// 血流胡牌后将手牌变暗
    /// </summary>
    private void DimHandCardsAfterXueLiuHu()
    {
        var seat = GetSeat(0);
        if (seat == null)
        {
            return;
        }
        if (seat.IsXueLiuHu)
        {
            return;
        }

        // 标记为已胡牌（用于后续摸牌时自动变暗）
        seat.IsXueLiuHu = true;

        // 将所有手牌变暗（使用最高优先级遮罩）
        for (int i = 0; i < seat.HandContainer.childCount; i++)
        {
            Transform cardTransform = seat.HandContainer.GetChild(i);
            var mjCardInteraction = cardTransform.GetComponent<MjCard_2D_Interaction>();
            var mjCard2D = cardTransform.GetComponent<MjCard_2D>();
            // 使用血流胡牌专用遮罩（最高优先级，不会被碰杠标记清除）
            mjCard2D.SetXueLiuHuMark(true);
            mjCardInteraction.enabled = false;
        }
    }
    #endregion

    #region 花猪检测和提示

    /// <summary>
    /// 检查并显示花猪提示
    /// 花猪：手牌+副露（不包含甩牌）有三种花色（万、条、筒）
    /// 在出牌阶段检测，不需要判断听牌
    /// </summary>
    public void CheckAndShowHuaZhuTip()
    {
        if (!IsXueLiuRule || huaZhu == null) return;

        // 获取自己座位的所有牌（手牌+副露，不包含甩牌）
        bool hasThreeSuits = CheckHasThreeSuits();
        
        // 显示或隐藏花猪提示
        huaZhu.SetActive(hasThreeSuits);
    }

    /// <summary>
    /// 隐藏花猪提示
    /// </summary>
    public void HideHuaZhuTip()
    {
        if (huaZhu != null)
        {
            huaZhu.SetActive(false);
        }
    }

    /// <summary>
    /// 检查自己座位是否有三种花色（万、条、筒）
    /// 包含手牌+摸牌+副露区（碰吃杠），不包含甩牌
    /// </summary>
    /// <returns>是否有三种花色</returns>
    private bool CheckHasThreeSuits()
    {
        var seat = GetSeat(0);
        if (seat == null) return false;

        bool hasWan = false;  // 万
        bool hasTiao = false; // 条
        bool hasTong = false; // 筒

        // 检查手牌
        if (seat.HandContainer != null)
        {
            for (int i = 0; i < seat.HandContainer.childCount; i++)
            {
                var mjCard = seat.HandContainer.GetChild(i).GetComponent<MjCard_2D>();
                if (mjCard != null && mjCard.cardValue > 0)
                {
                    CheckCardSuit(mjCard.cardValue, ref hasWan, ref hasTiao, ref hasTong);
                    if (hasWan && hasTiao && hasTong) return true;
                }
            }
        }

        // 检查摸牌区
        if (seat.MoPaiContainer != null)
        {
            for (int i = 0; i < seat.MoPaiContainer.childCount; i++)
            {
                var mjCard = seat.MoPaiContainer.GetChild(i).GetComponent<MjCard_2D>();
                if (mjCard != null && mjCard.cardValue > 0)
                {
                    CheckCardSuit(mjCard.cardValue, ref hasWan, ref hasTiao, ref hasTong);
                    if (hasWan && hasTiao && hasTong) return true;
                }
            }
        }

        // 检查副露区（碰吃杠，不包含甩牌）
        if (seat.MeldContainer != null)
        {
            for (int i = 0; i < seat.MeldContainer.childCount; i++)
            {
                var mjCard = seat.MeldContainer.GetChild(i).GetComponent<MjCard_2D>();
                if (mjCard != null && mjCard.cardValue > 0)
                {
                    // 跳过甩牌标记的牌
                    if (mjCard.isShuaiPaiCard) continue;
                    
                    CheckCardSuit(mjCard.cardValue, ref hasWan, ref hasTiao, ref hasTong);
                    if (hasWan && hasTiao && hasTong) return true;
                }
            }
        }

        return hasWan && hasTiao && hasTong;
    }

    /// <summary>
    /// 检查牌的花色并更新标记
    /// </summary>
    /// <param name="cardValue">服务器牌值</param>
    /// <param name="hasWan">是否有万</param>
    /// <param name="hasTiao">是否有条</param>
    /// <param name="hasTong">是否有筒</param>
    private void CheckCardSuit(int cardValue, ref bool hasWan, ref bool hasTiao, ref bool hasTong)
    {
        var faceValue = Util.ConvertServerIntToFaceValue(cardValue);
        if (faceValue == MahjongFaceValue.MJ_UNKNOWN) return;

        // 万: MJ_WANG_1 ~ MJ_WANG_9
        if (faceValue >= MahjongFaceValue.MJ_WANG_1 && faceValue <= MahjongFaceValue.MJ_WANG_9)
        {
            hasWan = true;
        }
        // 条: MJ_TIAO_1 ~ MJ_TIAO_9
        else if (faceValue >= MahjongFaceValue.MJ_TIAO_1 && faceValue <= MahjongFaceValue.MJ_TIAO_9)
        {
            hasTiao = true;
        }
        // 筒: MJ_TONG_1 ~ MJ_TONG_9
        else if (faceValue >= MahjongFaceValue.MJ_TONG_1 && faceValue <= MahjongFaceValue.MJ_TONG_9)
        {
            hasTong = true;
        }
    }

    #endregion

    #region 血流清理

    /// <summary>
    /// 清理所有血流相关的面板和状态
    /// </summary>
    public void CleanupAllXueLiuPanels()
    {
        mj_procedure?.enterMJDeskRs?.ShuaiPai.Clear();

        // 隐藏自己的甩牌操作面板
        XuanShuaiPaiPanel.SetActive(false);

        // 隐藏甩牌阶段面板
        ShuaiPaiPanel.SetActive(false);

        // 隐藏甩牌状态
        foreach (var go in ShuaiPaiZhongs)
            go.SetActive(false);

        // 隐藏结算面板
        GameEnd_XL.SetActive(false);

        // 隐藏花猪提示
        HideHuaZhuTip();

        // 清理状态数据
        m_SelectedThrowCards.Clear();

    }

    #endregion
}

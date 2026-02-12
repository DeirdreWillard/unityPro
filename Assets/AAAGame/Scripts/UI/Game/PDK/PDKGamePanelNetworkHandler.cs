using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Google.Protobuf.Collections;
using Google.Protobuf;
using UnityGameFramework.Runtime;
using Cysharp.Threading.Tasks;
//网络消息处理模块代码
public partial class PDKGamePanel
{
    private Msg_EnterRunFastDeskRs enterData;

    private void HideReadyButtons()
    {
        varBtnReady?.SetActive(false);
        varBtnQuitReady?.SetActive(false);
    }

    public void InitPDKDesk(Msg_EnterRunFastDeskRs enterData)
    {
        this.enterData = enterData;
        GF.LogInfo_wl("[PDK进入] ，桌子状态：" + enterData.ToString());

        // 【新增】重连托管状态同步：在设置玩家UI前，根据 auto 字段（int 28）更新 PlayerState
        if (enterData.DeskPlayers != null)
        {
            foreach (var p in enterData.DeskPlayers)
            {
                if (p.Auto == 1)
                {
                    p.State = PlayerState.ToGuo;
                    GF.LogInfo_wl($"[重连托管检测] 玩家{p.BasePlayer.PlayerId}处于托管状态");
                }
            }
        } 

        // 【关键修复】确保从服务器数据更新当前局数（包括旁观者重连）
        var pdkProcedure = GF.Procedure.GetProcedure<PDKProcedures>() as PDKProcedures;
        pdkProcedure.currentRoundNum = enterData.Round > 0 ? enterData.Round : 1;
        // 【防守机制】初始化时也确保不超出总局数
        int totalRounds = (int)(pdkProcedure.EnterRunFastDeskRs?.BaseConfig?.PlayerTime ?? 0);
        if (totalRounds > 0 && pdkProcedure.currentRoundNum > totalRounds)
        {
            pdkProcedure.currentRoundNum = totalRounds;
        }

        if (enterData.HandCardNum != null && enterData.HandCardNum.Count > 0)
        {
            playerIdToCardCount.Clear();
            GF.LogInfo_wl($"<color=#00FFFF>[InitPDKDesk] 从HandCardNum初始化剩牌数据，共{enterData.HandCardNum.Count}个玩家</color>");
            foreach (var handCardData in enterData.HandCardNum)
            {
                long playerId = handCardData.Key;
                int cardCount = (int)handCardData.Val;
                playerIdToCardCount[playerId] = cardCount;
                GF.LogInfo_wl($"<color=#00FFFF>[InitPDKDesk] 玩家{playerId}: {cardCount}张牌</color>");
            }
        }
        else
        {
            GF.LogWarning($"<color=#FF0000>[InitPDKDesk] ⚠️ HandCardNum为空，将在SetDeskPlayers中尝试从HandCard初始化</color>");
        }

        ClearAllGameUI();
        SetDeskBaseInfo(enterData);
        SetDeskPlayers(enterData.DeskPlayers, enterData.BaseConfig.PlayerNum);
        InitializeGameLogicFromServer(enterData);
        CheckAndRestoreDismissPanel();
        switch (enterData.DeskState)
        {
            case RunFastDeskState.RunfastWait:
                InitWaitingState(enterData);
                break;
            case RunFastDeskState.RunfastRob:
                InitRobBankerState(enterData);
                break;
            case RunFastDeskState.RunfastStart:
                InitDoubleState(enterData);
                break;
            case RunFastDeskState.RunfastDiscard:
                InitPlayingState(enterData);
                break;
            case RunFastDeskState.RunfastSettle:
                // 获取自己的玩家ID
                long myPlayerId = Util.GetMyselfInfo().PlayerId;
                var myPlayer = enterData.DeskPlayers?.FirstOrDefault(p => p.BasePlayer.PlayerId == myPlayerId);
                // 【新增】判断是否为旁观者
                bool isSpectator = (myPlayer == null);
                if (isSpectator)
                {
                    // 旁观者：隐藏准备按钮
                    varBtnReady?.SetActive(false);
                    varBtnQuitReady?.SetActive(false);
                }
                else
                {
                    // 根据服务器返回的准备状态来设置按钮显示
                    isReady = myPlayer?.Ready ?? false;
                    // 设置按钮显示状态：未准备显示准备按钮，已准备显示取消准备按钮
                    varBtnReady?.SetActive(!isReady);
                    varBtnQuitReady?.SetActive(isReady);
                }
                ShowPlayButtons(false);
                break;
            default:
                break;
        }

    }
    /// <summary>
    /// 设置桌子基础信息
    /// </summary>
    private void SetDeskBaseInfo(Msg_EnterRunFastDeskRs enterData)
    {
        var FloorNum = varRuleBg.transform.Find("FloorNum/FloorNum");
        FloorNum.GetComponent<Text>().text = enterData.DeskId.ToString();

        var BaseScore = varRuleBg.transform.Find("DIFengNum/Num");
        BaseScore.GetComponent<Text>().text = enterData.BaseConfig.BaseCoin.ToString();
        var RuleText = varRuleBg.transform.Find("RuleText");
        RuleText.GetComponent<Text>().text = GetRulesText();
        // 更新局数显示（从服务器数据获取当前局数）
        UpdateRoundDisplay();
        UpdateAllPlayersCardCount();
    }
    /// <summary>
    /// 更新局数显示（当前局/总局数）
    /// </summary>
    public void UpdateRoundDisplay()
    {
        var pdkProcedure = GF.Procedure.GetProcedure<PDKProcedures>() as PDKProcedures;
        if (pdkProcedure?.EnterRunFastDeskRs?.BaseConfig == null)
        {
            GF.LogWarning("[PDK局数] 无法获取桌子配置信息");
            return;
        }

        // 【关键修复】优先使用 Procedure 中维护的 currentRoundNum，确保局数实时更新
        int currentRound = pdkProcedure.currentRoundNum;
        int totalRounds = (int)pdkProcedure.EnterRunFastDeskRs.BaseConfig.PlayerTime;

        // 【防守机制】确保局数不显示超出总局数
        if (currentRound > totalRounds && totalRounds > 0)
        {
            currentRound = totalRounds;
        }

        var jushu = varRuleBg.transform.Find("jushu/Num");
        if (jushu != null)
        {
            jushu.GetComponent<Text>().text = $"{currentRound}/{totalRounds}";
        }
    }
    /// <summary>
    /// 更新桌子玩家信息（用于玩家进入/离开时同步）
    /// </summary>
    /// <param name="deskPlayers">最新的桌子玩家列表</param>
    public void UpdateDeskPlayers(Google.Protobuf.Collections.RepeatedField<DeskPlayer> deskPlayers)
    {

        int totalPlayerNum;
        var pdkProcedure = GF.Procedure.GetProcedure<PDKProcedures>() as PDKProcedures;
        totalPlayerNum = (int)pdkProcedure.EnterRunFastDeskRs.BaseConfig.PlayerNum;
        // 调用现有的设置玩家信息方法
        SetDeskPlayers(deskPlayers, totalPlayerNum);
        // 重新初始化聊天容器映射
        InitChatContainers();
    }

    /// <summary>
    /// 获取游戏规则文本（包含先出规则、玩法规则、复选框规则）
    /// </summary>
    private string GetRulesText()
    {
        List<string> rules = new List<string>();
        // 添加出牌规则
        switch (PDKConstants.chupai)
        {
            case 1:
                rules.Add("首出必带最小");
                break;
            case 2:
                rules.Add("首出任意出牌");
                break;
        }
        // 添加先出规则
        switch (PDKConstants.xianchuRule)
        {
            case 0:
                rules.Add("首局最小牌先出");
                break;
            case 1:
                rules.Add("每局最小牌先出");
                break;
            case 2:
                rules.Add("首局黑桃3先出");
                break;
        }

        // 添加玩法规则
        switch (PDKConstants.wanfa)
        {
            case 1:
                rules.Add("有大必压");
                break;
            case 2:
                rules.Add("可不压");
                break;
        }

        // 添加复选框规则（从0开始遍历）
        if (PDKConstants.CheckboxRules != null && PDKConstants.CheckboxRules.Length > 0)
        {
            for (int i = 0; i < PDKConstants.CheckboxRules.Length; i++)
            {
                // CheckboxRules存储的是规则编号(从1开始)，需要转换为索引(从0开始)
                int ruleIndex = PDKConstants.CheckboxRules[i] - 1;
                string ruleName = PDKConstants.GetCheckboxRuleName(ruleIndex);
                if (!string.IsNullOrEmpty(ruleName))
                {
                    rules.Add(ruleName);
                }
            }
        }
        
        // 添加IP/GPS限制（从enterData获取）
        if (enterData != null && enterData.BaseConfig != null)
        {
            if (enterData.BaseConfig.IpLimit && enterData.BaseConfig.GpsLimit)
            {
                rules.Add("IP/GPS限制");
            }
            else if (enterData.BaseConfig.IpLimit)
            {
                rules.Add("IP限制");
            }
            else if (enterData.BaseConfig.GpsLimit)
            {
                rules.Add("GPS限制");
            }
        }
//默认添加 1分钟自动出牌规则
        rules.Add("1分钟自动出牌");
        // 用空格分隔所有规则
        string result = string.Join(" ", rules);
        return result;
    }

    /// <summary>
    /// 设置桌子玩家信息
    /// </summary>
    /// <param name="deskPlayers">当前桌子上的玩家列表</param>
    /// <param name="totalPlayerNum">桌子配置的玩家总数（从BaseConfig.PlayerNum获取）</param>
    private void SetDeskPlayers(RepeatedField<DeskPlayer> deskPlayers, int totalPlayerNum)
    {
        varHeadBg0.SetActive(false);
        varHeadBg1.SetActive(false);
        varHeadBg2.SetActive(false);
        playerIdToHeadBg.Clear();
        playerIdToPlayArea.Clear();
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        var myPlayer = deskPlayers.FirstOrDefault(p => p.BasePlayer.PlayerId == myPlayerId);

        // 【新增】判断是否为旁观者（不在桌子玩家列表中）
        bool isSpectator = (myPlayer == null);
        if (isSpectator)
        {
            // 旁观者模式：按服务器位置正确映射到UI位置，确保逆时针顺序
            // 使用默认位置作为自己的位置
            Position spectatorPos = Position.Default;
            foreach (var player in deskPlayers)
            {
                // 使用服务器位置转换为客户端位置（0=下方，1=右边，2=左边，逆时针）
                int clientPos = Util.GetPosByServerPos_pdk(spectatorPos, player.Pos, totalPlayerNum);

                GameObject headBg = null;
                if (clientPos == 0) headBg = varHeadBg0;      // 下方
                else if (clientPos == 1) headBg = varHeadBg1;  // 右边（下家）
                else if (clientPos == 2) headBg = varHeadBg2;  // 左边（上家）
                else continue; // 无效位置，跳过

                if (headBg != null)
                {
                    headBg.SetActive(true);
                    long playerId = player.BasePlayer.PlayerId;
                    playerIdToHeadBg[playerId] = headBg;
                    GameObject playArea = GetPlayAreaByHeadBg(headBg);
                    playerIdToPlayArea[playerId] = playArea;
                    SetPlayerHead(headBg, player);
                    SetPlayerNickname(headBg, player);
                    SetPlayerCoin(headBg, player);
                    SetPlayerOnlineStatus(headBg, player);
                    SetPlayerTuoGuanStatus(headBg, player);

                    // 同步准备状态
                    int readyState = player.Ready ? 1 : 0;
                    OnOtherPlayerReadyChanged(playerId, readyState);
                }
            }

            // 【说明】旁观者的剩牌数量已在InitPDKDesk开始时从HandCardNum初始化，这里不需要重复处理

            return; // 旁观者直接返回，不执行后续的正常玩家逻辑
        }

        // 获取自己的客户端位置（用于判断其他玩家的相对位置）
        int myClientPos = 0; // 自己永远在0号位
        Position myServerPos = myPlayer.Pos;
        // 遍历所有玩家，根据客户端位置设置UI
        foreach (var player in deskPlayers)
        {
            GameObject headBg = null;
            if (player.BasePlayer.PlayerId == myPlayerId)
            {
                headBg = varHeadBg0;
            }
            else
            {
                int clientPos = Util.GetPosByServerPos_pdk(myServerPos, player.Pos, totalPlayerNum);
                headBg = GetHeadBgByPos(clientPos, myClientPos, totalPlayerNum);
            }
            headBg.SetActive(true);
            long playerId = player.BasePlayer.PlayerId;
            playerIdToHeadBg[playerId] = headBg;
            GameObject playArea = GetPlayAreaByHeadBg(headBg);
            playerIdToPlayArea[playerId] = playArea;
            SetPlayerHead(headBg, player);
            SetPlayerNickname(headBg, player);
            SetPlayerCoin(headBg, player);
            SetPlayerOnlineStatus(headBg, player);
            SetPlayerTuoGuanStatus(headBg, player);
            // 【修复】同步准备状态（用于重连或玩家进入时恢复准备显示）
            if (playerId != myPlayerId)
            {
                // 其他玩家的准备状态
                int readyState = player.Ready ? 1 : 0;
                OnOtherPlayerReadyChanged(playerId, readyState);
            }

        }

        // 【说明】不再从HandCard.Count初始化剩牌数量
        // HandCard字段只有自己的手牌有具体内容，其他玩家的HandCard为空
        // 剩牌数量应该从enterData.HandCardNum字段读取（在InitPDKDesk开始时已处理）
    }
    /// <summary>
    /// 初始化等待状态（等待玩家准备或开始游戏）
    /// </summary>
    private void InitWaitingState(Msg_EnterRunFastDeskRs enterData)
    {
        // 清除所有庄家标识（等待阶段不应该显示庄家）
        ForEachHeadBg((headBg) =>
         {
             var bankerTransform = headBg.transform.Find("Banker");
             bankerTransform.gameObject.SetActive(false);
         });
        // 清除所有出牌区（等待阶段不应该有出牌显示）
        ClearAllPlayAreas();
        //隐藏打牌按钮（等待阶段不应该显示打牌按钮）
        ShowPlayButtons(false);
        // 隐藏所有报警特效（等待阶段不应该显示报警）
        HideAllWarningEffects();
        // 获取自己的玩家ID
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        var myPlayer = enterData.DeskPlayers?.FirstOrDefault(p => p.BasePlayer.PlayerId == myPlayerId);
        //是否为旁观者
        bool isSpectator = (myPlayer == null);
        if (isSpectator)
        {
            varBtnReady?.SetActive(false);
            varBtnQuitReady?.SetActive(false);
            ZBdaojishi?.SetActive(false);
        }
        else
        {
            isReady = myPlayer?.Ready ?? false;
            varBtnReady?.SetActive(!isReady);
            varBtnQuitReady?.SetActive(isReady);

            // 【新增】如果未准备，显示准备倒计时
            if (!isReady && ZBdaojishi != null)
            {
                ZBdaojishi.SetActive(true);
                PdkProcedure.StartCountdown(ZBdaojishi.GetComponent<Text>(), 15).Forget();
            }
            else
            {
                ZBdaojishi?.SetActive(false);
            }
        }
        if (enterData.DeskPlayers != null)
        {
            foreach (var player in enterData.DeskPlayers)
            {
                int readyState = player.Ready ? 1 : 0;
                if (player.BasePlayer.PlayerId == myPlayerId)
                {
                    OnReadyStateChanged(readyState);
                }
                else
                {
                    OnOtherPlayerReadyChanged(player.BasePlayer.PlayerId, readyState);
                }
            }
        }
    }

    /// <summary>
    /// 初始化翻倍阶段状态（用于重连恢复）
    /// </summary>
    private void InitDoubleState(Msg_EnterRunFastDeskRs enterData)
    {
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        isReady = false;
        HideReadyButtons();
        ClearAllPlayAreas();
        ShowPlayButtons(false);
        HideAllPassIndicators();
        HideAllPlayerClocks();
        HideAllWarningEffects();
        if (enterData.Banker > 0)
        {
            UpdateAllPlayerBankerStatus(enterData.DeskPlayers, enterData.BaseConfig.PlayerNum, enterData.Banker);
        }
        if (enterData.DeskPlayers != null && enterData.DeskPlayers.Count > 0)
        {
            var myPlayer = enterData.DeskPlayers.FirstOrDefault(p => p.BasePlayer.PlayerId == myPlayerId);

            if (myPlayer != null && myPlayer.HandCard != null && myPlayer.HandCard.Count > 0)
            {
                var validCards = myPlayer.HandCard.Where(cardId => cardId > 0).ToList();
                if (validCards.Count > 0)
                {
                    OnReceiveHandCards(validCards, withAnimation: false);
                }
            }
        }
        // playerIdToCardCount已在InitPDKDesk开始时从HandCardNum初始化
        UpdateAllPlayersCardCount();
        UpdateDoubleBtnDisplay();
        var pdkProcedure = GF.Procedure.GetProcedure<PDKProcedures>() as PDKProcedures;
        if (pdkProcedure != null)
        {
            pdkProcedure.ShowRobBankerUI(false);
            pdkProcedure.HideAllPlayerRobBankerStatus();
        }
    }

    /// <summary>
    /// 初始化抢庄阶段状态（用于重连恢复）
    /// </summary>
    private void InitRobBankerState(Msg_EnterRunFastDeskRs enterData)
    {
        // 获取自己的PlayerId（提前声明，后面多处使用）
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        // 重置准备状态
        isReady = false;
        // 1. 更新庄家标识（抢庄阶段可能还是原庄家）
        if (enterData.Banker > 0)
        {
            UpdateAllPlayerBankerStatus(enterData.DeskPlayers, enterData.BaseConfig.PlayerNum, enterData.Banker);
        }
        // 2. 恢复自己的手牌（关键修复）
        if (enterData.DeskPlayers != null && enterData.DeskPlayers.Count > 0)
        {
            var myPlayer = enterData.DeskPlayers.FirstOrDefault(p => p.BasePlayer.PlayerId == myPlayerId);
            if (myPlayer != null && myPlayer.HandCard != null && myPlayer.HandCard.Count > 0)
            {
                var validCards = myPlayer.HandCard.Where(cardId => cardId > 0).ToList();
                if (validCards.Count > 0)
                {
                    OnReceiveHandCards(validCards, withAnimation: false);
                }
            }
            else
            {
                // 从 HandCardNum 获取手牌数量
                if (enterData.HandCardNum != null && enterData.HandCardNum.Count > 0)
                {
                    var myCardNum = enterData.HandCardNum.FirstOrDefault(h => h.Key == myPlayerId);
                    if (myCardNum != null)
                    {
                        int cardCount = (int)myCardNum.Val;
                        // 创建占位牌（用于显示数量）
                        List<int> placeholderCards = new List<int>();
                        for (int i = 0; i < cardCount; i++)
                        {
                            placeholderCards.Add(-1); // -1 表示不显示牌面
                        }
                        OnReceiveHandCards(placeholderCards, withAnimation: false);
                    }
                }
            }
        }
        UpdateAllPlayersCardCount();
        UpdateAllPlayersCardCount();
        // 4. 隐藏出牌按钮（抢庄阶段不能出牌）
        ShowPlayButtons(false);
        // 5. 判断是否显示抢庄按钮
        // 获取PDKProcedures来调用抢庄UI显示方法
        var pdkProcedure = GF.Procedure.GetProcedure<PDKProcedures>() as PDKProcedures;
        if (pdkProcedure != null)
        {
            // 【关键】直接使用PlayerId字段判断轮到谁操作
            if (enterData.PlayerId > 0)
            {
                if (enterData.PlayerId == myPlayerId)
                {
                    pdkProcedure.ShowRobBankerUI(true);
                }
                else
                {
                    pdkProcedure.ShowRobBankerUI(false);
                }
            }
            else
            {
                pdkProcedure.ShowRobBankerUI(false);
            }
        }
        if (DoubleBtn != null) DoubleBtn.SetActive(false);
        HideAllDoubleIndicators();
    }
    /// <summary>
    /// 初始化游戏进行中状态（用于重连恢复）
    /// </summary>
    private void InitPlayingState(Msg_EnterRunFastDeskRs enterData)
    {
        // 0. 隐藏准备按钮（游戏已开始，不需要准备按钮）
        varBtnReady.SetActive(false);
        varBtnQuitReady.SetActive(false);
        // 重置准备状态
        isReady = false;
        // 1. 更新所有玩家的庄家标识
        UpdateAllPlayerBankerStatus(enterData.DeskPlayers, enterData.BaseConfig.PlayerNum, enterData.Banker);
        // 3. 恢复自己的手牌
        if (enterData.DeskPlayers != null && enterData.DeskPlayers.Count > 0)
        {
            // 查找自己的玩家数据（使用 PlayerId 字段）
            long myPlayerId = Util.GetMyselfInfo().PlayerId;
            var myPlayer = enterData.DeskPlayers.FirstOrDefault(p => p.BasePlayer.PlayerId == myPlayerId);
            Position myServerPos = myPlayer?.Pos ?? Position.Default;
            if (myPlayer != null && myPlayer.HandCard != null && myPlayer.HandCard.Count > 0)
            {
                OnReceiveHandCards(myPlayer.HandCard.ToList(), withAnimation: false); // 重连时不播放动画
            }
        }

        RestoreAllPlayersLastCards(enterData);
        UpdateAllPlayersCardCount();
        // 5. 【关键修复】设置 serverLastMaxDiscard 用于首出状态判断
        if (enterData.LastMaxDiscard > 0)
        {
            serverLastMaxDiscard = enterData.LastMaxDiscard;
        }
        else
        {
            serverLastMaxDiscard = 0;
        }
        // 使用协程延迟处理，确保手牌UI完全创建并排列完成
        StartCoroutine(RestorePlayerTurnAfterHandCardsLoaded(enterData));
        // 6. 隐藏抢庄和翻倍UI
        var pdkProcedure = GF.Procedure.GetProcedure<PDKProcedures>() as PDKProcedures;
        if (pdkProcedure != null)
        {
            pdkProcedure.ShowRobBankerUI(false);
            pdkProcedure.HideAllPlayerRobBankerStatus();
            pdkProcedure.HideAllPlayerDoubleStatus();
        }
        if (DoubleBtn != null) DoubleBtn.SetActive(false);
        HideAllDoubleIndicators();
    }
    /// <summary>
    /// 在手牌UI加载完成后恢复当前玩家回合状态（重连专用）
    /// </summary>
    private IEnumerator RestorePlayerTurnAfterHandCardsLoaded(Msg_EnterRunFastDeskRs enterData)
    {

        // 等待0.2秒，确保UI创建和排列都完成
        yield return new WaitForSeconds(0.2f);

        // 恢复当前操作玩家（高亮提示轮到谁出牌）
        if (enterData.PlayerId > 0)
        {

            // 查找当前操作玩家的位置
            var currentPlayer = enterData.DeskPlayers?.FirstOrDefault(p => p.BasePlayer.PlayerId == enterData.PlayerId);
            if (currentPlayer != null)
            {
                long myPlayerId = Util.GetMyselfInfo().PlayerId;
                var myPlayer = enterData.DeskPlayers?.FirstOrDefault(p => p.BasePlayer.PlayerId == myPlayerId);
                Position myServerPos = myPlayer?.Pos ?? Position.Default;
                int currentClientPos = Util.GetPosByServerPos_pdk(myServerPos, currentPlayer.Pos, enterData.BaseConfig.PlayerNum);

                // 【新增】判断是否为旁观者
                bool isSpectator = (myPlayer == null);

                // 【关键修复】轮到自己时才显示出牌按钮(重连场景,不检查首出提示和可出牌逻辑)
                bool isMyTurn = (enterData.PlayerId == myPlayerId);
                if (isMyTurn)
                {
                    ShowPlayButtons(true, isReconnecting: true);
                    // 【新增】重连时轮到自己，需要检查可出牌逻辑（包括4秒倒计时自动过牌）
                    CheckPlayableCardsOnMyTurn(isReconnecting: true);
                }
                else
                {
                    ShowPlayButtons(false);
                    if (playerIdToPlayArea.TryGetValue(enterData.PlayerId, out GameObject playArea) && playArea != null)
                    {
                        ClearPlayArea(playArea);

                    }
                    // 【关键修复】旁观者重连时也需要显示当前玩家的倒计时
                    ShowPlayerClock(enterData.PlayerId, true, isReconnecting: true);
                }
            }
        }

        // 6. 恢复倒计时
        if (enterData.NextTime > 0)
        {
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long remainingTime = enterData.NextTime - currentTime;
            if (remainingTime > 0)
            {
                // GF.LogInfo($"[PDK重连] 倒计时剩余 {remainingTime} 秒");
            }
            else
            {
                GF.LogWarning($"[PDK重连] 倒计时已过期（剩余{remainingTime}秒）");
            }
        }

        // GF.LogInfo("[PDK重连] ✅ 游戏进行中状态恢复完成（包含手牌交互状态）");

        // 【关键】重连后更新所有玩家的剩牌数量显示
        GF.LogInfo_wl($"<color=#00FFFF>[重连-剩牌] 准备更新UI显示，当前playerIdToCardCount字典有{playerIdToCardCount.Count}个玩家数据</color>");
        UpdateAllPlayersCardCount();

        // 【新增】重连后更新所有玩家的报警特效
        UpdateAllWarningEffects();
    }


    /// <summary>
    /// 恢复所有玩家最后打出的牌（重连时调用）
    /// </summary>
    private void RestoreAllPlayersLastCards(Msg_EnterRunFastDeskRs enterData)
    {
        if (enterData == null) return;


        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        long currentPlayerId = enterData.PlayerId;

        // 如果游戏刚开始还未出牌，直接返回
        if (enterData.LastMaxDiscard <= 0)
        {
            ClearAllPlayAreas();
            return;
        }

        if (enterData.LastCard == null || enterData.LastCard.Count == 0) return;

        long lastMaxDiscardPlayerId = enterData.LastMaxDiscard;
        List<int> lastMaxDiscardCardIds = null;

        HideAllPassIndicators();
        HideAllPlayerClocks();

        foreach (var lastCardData in enterData.LastCard)
        {
            if (lastCardData == null) continue;

            long playerId = lastCardData.Key;
            List<int> cardIds = (lastCardData.Vals != null) ? new List<int>(lastCardData.Vals) : new List<int>();

            // 记录需要跟打的牌
            if (playerId == lastMaxDiscardPlayerId && cardIds.Count > 0)
            {
                lastMaxDiscardCardIds = cardIds;
            }

            // 处理过牌标识
            if (cardIds.Count == 0 && playerId != currentPlayerId)
            {
                ShowPassIndicator(playerId, true);
                continue;
            }

            // 如果是自己且轮到自己出牌，不恢复出牌区
            if (playerId == myPlayerId && currentPlayerId == myPlayerId)
            {
                ClearPlayAreaByPlayerId(playerId);
                continue;
            }

            // 恢复出牌显示
            if (playerIdToPlayArea.TryGetValue(playerId, out GameObject playArea) && playArea != null)
            {
                ClearPlayArea(playArea);
                DisplayCardsInPlayArea(playArea, cardIds);
            }
        }

        UpdatePanelStateWithLastPlayedCards(enterData, lastMaxDiscardPlayerId, lastMaxDiscardCardIds);
    }

    /// <summary>
    /// 处理解散同步消息
    /// </summary>
    public void Function_Msg_SynMJDismiss(Msg_SynMJDismiss ack)
    {
        var pdkProcedure = GF.Procedure.GetProcedure<PDKProcedures>() as PDKProcedures;
        if (pdkProcedure == null || pdkProcedure.EnterRunFastDeskRs == null) return;

        // 获取桌子上的玩家列表
        List<DeskPlayer> deskPlayers = pdkProcedure.EnterRunFastDeskRs.DeskPlayers.ToList();
        if (deskPlayers == null || deskPlayers.Count == 0) return;

        // 转换为BasePlayer列表
        List<BasePlayer> allPlayers = deskPlayers.Select(dp => dp.BasePlayer).ToList();

        // 使用Procedure中保存的已同意列表（支持重连）
        List<long> agreeDismissPlayerList = pdkProcedure.AgreedPlayerIds ?? new List<long>();

        // 调用通用解散面板
        Util.ShowDismissPanel(ack.AgreeDismissTime, ack.PlayerId, allPlayers, agreeDismissPlayerList);
    }
    /// <summary>
    /// 处理服务器发牌消息
    /// </summary>
    /// <param name="cardList">服务器发送的卡牌编号列表</param>
    /// <param name="withAnimation">是否播放发牌动画（默认true，重连时传false）</param>
    public void OnReceiveHandCards(List<int> cardList, bool withAnimation = true)
    {
        // 【关键修复】允许空的卡牌列表（旁观者情况），不再直接 return
        bool hasHandCards = (cardList != null && cardList.Count > 0);
        if (!hasHandCards)
        {
            GF.LogInfo_wl("[PDK网络] 旁观者收到发牌消息，执行基础重置逻辑");
        }

        // 清空所有出牌区域（上一局的出牌记录）
        ClearPlayArea(varPlayArea);   // 自己的出牌区
        ClearPlayArea(varPlayArea1);  // 右边玩家的出牌区
        ClearPlayArea(varPlayArea2);  // 左边玩家的出牌区

        // 隐藏所有玩家的"不出"标识和倒计时动画
        HideAllPassIndicators();
        HideAllPlayerClocks();

        lastPlayedCards = null;
        serverMaxDisCardPlayer = 0;
        serverLastMaxDiscard = 0;
        passedPlayers.Clear();
        isFirstRound = true;
        roundWinnerIndex = -1;

        GF.LogInfo_wl($"<color=#00FF00>[发牌重置] serverLastMaxDiscard={serverLastMaxDiscard}, serverMaxDisCardPlayer={serverMaxDisCardPlayer}, isFirstRound={isFirstRound}</color>");

        // 清空提示状态
        if (gameLogic != null)
        {
            // 提示状态由各模块自行维护
        }
        // 隐藏准备和取消准备按钮（游戏已开始，发牌了）
        HideReadyButtons();

        // 重置准备状态
        isReady = false;

        UpdateRoundDisplay();

        // 清空现有手牌
        ClearAllHandCards();

        // 清空选中状态
        ClearSelectedCards();

        if (hasHandCards)
        {
            // 创建手牌对象（使用传入的 withAnimation 参数）
            CreateHandCardObjects(cardList, withAnimation);

            myHandCards = ConvertCardIdsToCardDataList(cardList);
            // 延迟一帧重新排列
            StartCoroutine(uiManager.ArrangeHandCardsNextFrame());
        }

        // 【关键修复】只有在正常发牌时（withAnimation=true）才初始化所有玩家为16张
        // 重连时（withAnimation=false）不要覆盖从HandCardNum读取的正确数据
        if (withAnimation)
        {
            // 正常发牌：初始化所有玩家的剩余牌数量为16张
            List<long> allPlayerIds = new List<long>(playerIdToHeadBg.Keys);
            if (allPlayerIds.Count > 0)
            {
                foreach (long playerId in allPlayerIds)
                {
                    playerIdToCardCount[playerId] = 16;
                }
                GF.LogInfo_wl($"<color=#00FF00>[发牌初始化] 所有玩家设置为16张：{allPlayerIds.Count}个玩家</color>");
            }
        }
        else
        {
            // 重连：不覆盖playerIdToCardCount，保留从HandCardNum读取的正确数据
            GF.LogInfo_wl($"<color=#FFFF00>[重连发牌] 跳过初始化16张，保留HandCardNum数据</color>");
        }

        UpdateAllPlayersCardCount();
    }

    /// <summary>
    /// 检查并恢复解散弹窗（重连时调用）
    /// </summary>
    private void CheckAndRestoreDismissPanel()
    {


        var pdkProcedure = GF.Procedure.GetProcedure<PDKProcedures>() as PDKProcedures;
        if (pdkProcedure == null || pdkProcedure.EnterRunFastDeskRs == null)
        {
            GF.LogWarning("[PDK解散-重连] pdkProcedure 或 EnterRunFastDeskRs 为空，无法检查解散状态");
            return;
        }
        var enterData = pdkProcedure.EnterRunFastDeskRs;
        // 【关键】从协议中读取解散状态（参照麻将逻辑）

        // 检查协议中是否有解散状态
        if (enterData.DismissState && enterData.AgreeDismissTime > 0)
        {

            // 检查是否已过期
            long currentTime = Util.GetServerTime();
            long remainMs = enterData.AgreeDismissTime - currentTime;
            int remainSeconds = Util.GetRemainTime(enterData.AgreeDismissTime);


            if (currentTime < enterData.AgreeDismissTime)
            {

                // 获取桌子上的玩家列表
                List<DeskPlayer> deskPlayers = enterData.DeskPlayers.ToList();

                if (deskPlayers != null && deskPlayers.Count > 0)
                {
                    // 转换为BasePlayer列表
                    List<BasePlayer> allPlayers = deskPlayers.Select(dp => dp.BasePlayer).ToList();

                    // 从协议中获取已同意的玩家列表
                    List<long> agreeDismissPlayerList = enterData.AgreeDismissPlayer.ToList();

                    // 获取申请人（已同意列表中的第一个）
                    long applyPlayerId = agreeDismissPlayerList.Count > 0 ? agreeDismissPlayerList[0] : 0;
                    // 调用通用解散面板
                    Util.ShowDismissPanel(
                        enterData.AgreeDismissTime,
                        applyPlayerId,
                        allPlayers,
                        agreeDismissPlayerList
                    );

                    // 同步到Procedure的内存状态（用于后续投票更新）
                    pdkProcedure.SyncDismissStateFromProtocol(enterData);
                }
                else
                {
                    GF.LogError("[PDK解散-重连] ❌ 玩家列表为空或数量为0，无法显示解散弹窗");
                }
            }
            else
            {
                GF.LogWarning($"[PDK解散-重连] ⏰ 解散请求已过期，不显示弹窗");
                GF.LogWarning($"[PDK解散-重连]   过期时间: {-remainMs}毫秒 ({-remainSeconds}秒)");
                GF.LogWarning($"[PDK解散-重连]   可能原因: 1.断线时间过长 2.服务器状态未及时清理 3.倒计时设置过短");
            }
        }
        else
        {
        }
    }
}

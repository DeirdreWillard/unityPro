using System;
using System.Collections.Generic;
using NetMsg;
using System.Linq;
using UnityEngine;
using GameFramework.Event;
using UnityEngine.Networking;
using System.Collections;

/// <summary>
/// 大众麻将游戏管理器
/// 负责管理麻将游戏的整体流程、状态、玩家信息和游戏数据
/// 整合了原 MahjongGameData2D 的数据管理功能
/// </summary>
public class BaseMahjongGameManager
{
    #region 核心组件引用

    public MahjongGameUI mahjongGameUI;
    public MJGameProcedure mJGameProcedure;
    public MahjongHuTingCheck mjHuTingCheck;

    /// <summary>当前使用的麻将规则实例</summary>
    private DefaultMahjongRule currentRule;
    
    /// <summary>麻将配置（玩法、封顶等）</summary>
    public MJConfig mjConfig;

    #endregion

    #region 游戏基础信息

    /// <summary>麻将总牌数</summary>
    private int mjTotalCount = 136;

    /// <summary>真实玩家数量（2人或4人）</summary>
    public int realPlayerCount = 4;

    /// <summary>自己的座位号（服务器坐标：0-3）</summary>
    public int selfPosAtServer = -1;

    /// <summary>自己的风位</summary>
    public FengWei selfFengWei = FengWei.EAST;

    /// <summary>座位玩家信息 - 按座位索引存储(0-3)</summary>
    public DeskPlayer[] deskPlayers = new DeskPlayer[4];

    /// <summary>剩余牌数（牌墙）</summary>
    public int richMjCount = 0;

    /// <summary>庄家位置（相对于我的客户端座位索引）</summary>
    public int dealerSeatIdx = 0;

    /// <summary>骰子点数</summary>
    private int[] dictNum = new int[2] { 1, 1 };

    #endregion

    #region 游戏数据存储说明
    
    #endregion

    #region 游戏状态属性（整合自 MahjongGameData2D）

    /// <summary>当前赖子牌值</summary>
    public MahjongFaceValue CurrentLaiZi { get; set; } = MahjongFaceValue.MJ_UNKNOWN;

    /// <summary>当前飘赖子牌值</summary>
    public MahjongFaceValue CurrentPiaoLaiZi { get; set; } = MahjongFaceValue.MJ_UNKNOWN;

    /// <summary>座位风位（东南西北）</summary>
    public FengWei[] SeatFengWei { get; set; } = new FengWei[4]
    {
        FengWei.EAST,
        FengWei.SOUTH,
        FengWei.WEST,
        FengWei.NORTH
    };

    /// <summary>当前轮到哪个座位操作（-1表示无人操作）</summary>
    public int CurrentTurnSeat { get; set; } = -1;

    /// <summary>当前操作的结束时间戳（毫秒，0表示无倒计时）</summary>
    public long CurrentNextTime { get; set; } = 0;

    /// <summary>获取当前倒计时剩余秒数（根据时间戳计算）</summary>
    public int GetCurrentCountdownSeconds()
    {
        if (CurrentNextTime < 0) return -1;
        return Util.GetRemainTime(CurrentNextTime);
    }

    #endregion

    #region 赖子相关方法

    /// <summary>
    /// 设置飘癞子值
    /// </summary>
    /// <param name="piao">飘癞子的服务器int值</param>
    public void SetPiaoLaiZi(MahjongFaceValue piao)
    {
        CurrentPiaoLaiZi = piao;
        GF.LogInfo_gsc("设置飘赖", $"飘赖牌:{piao}");
    }

    /// <summary>
    /// 判断一张牌是否为飘赖子(赖根)
    /// </summary>
    /// <param name="card">要判断的牌</param>
    /// <returns>是否为飘赖子</returns>
    public bool IsPiaoLaiZiCard(MahjongFaceValue card)
    {
        if (CurrentPiaoLaiZi == MahjongFaceValue.MJ_UNKNOWN)
            return false;
        return card == CurrentPiaoLaiZi;
    }

    /// <summary>
    /// 判断一张牌是否为赖子
    /// </summary>
    /// <param name="card">要判断的牌</param>
    /// <returns>是否为赖子</returns>
    public bool IsLaiZiCard(MahjongFaceValue card)
    {
        if (CurrentLaiZi == MahjongFaceValue.MJ_UNKNOWN)
            return false;
        return card == CurrentLaiZi;
    }

        /// <summary>
    /// 判断一张牌是否为赖子
    /// </summary>
    /// <param name="card">要判断的牌</param>
    /// <returns>是否为赖子</returns>
    public bool IsLaiZiCard(int cardValue)
    {
        MahjongFaceValue card = Util.ConvertServerIntToFaceValue(cardValue);
        if (CurrentLaiZi == MahjongFaceValue.MJ_UNKNOWN)
            return false;
        return card == CurrentLaiZi;
    }

    /// <summary>
    /// 设置赖子牌
    /// </summary>
    /// <param name="laiZiCard">赖子牌值</param>
    public void SetLaiZiCard(MahjongFaceValue laiZiCard)
    {
        CurrentLaiZi = laiZiCard;
        GF.LogInfo_gsc("设置赖子", $"赖子牌:{laiZiCard}");
    }

    #endregion

    #region 构造与初始化

    public BaseMahjongGameManager(MJGameProcedure mJGameProcedure, MahjongGameUI gameUI)
    {
        this.mahjongGameUI = gameUI;
        this.mJGameProcedure = mJGameProcedure;

        // 创建听牌检查器对象,数据稍后异步加载
        this.mjHuTingCheck = new MahjongHuTingCheck();
    }

    /// <summary>
    /// 异步加载麻将胡听检查缓存数据
    /// 优先从热更新配置加载,失败则使用Train方式
    /// </summary>
    public async Cysharp.Threading.Tasks.UniTask InitializeMahjongCacheAsync()
    {
        // 尝试从热更新配置加载
        bool cacheLoaded = await mjHuTingCheck.LoadFromConfigCacheAsync();
        
        if (!cacheLoaded)
        {
            // 加载失败,使用Train方式初始化
            GF.LogWarning("从配置加载麻将缓存失败,使用Train方式初始化");
            mjHuTingCheck.Train();
        }
        
        // 将游戏中创建的实例设置为全局单例，供回放等场景使用
        MahjongHuTingCheck.SetGlobalInstance(mjHuTingCheck);
    }

    public void InitMahjongDesk(Msg_EnterMJDeskRs enterData)
    {
        GF.LogInfo_gsc("开始根据协议初始化麻将桌子", enterData.ToString());

        // 设置桌子信息
        SetDeskInfo(enterData);

        // 设置玩家信息
        SetDeskPlayer(enterData);

        // 初始化UI面板
        mahjongGameUI.InitPanel(currentRule, enterData);

        // 按状态分流,避免重复初始化逻辑
        switch (enterData.CkState)
        {
            case MJState.MjWait:
                InitWaitingState(enterData);
                break;
            case MJState.KwxPiao:
                // 卡五星飘分阶段（游戏进行中的子阶段）
                InitPlayingState(enterData);
                InitKwxPiaoState(enterData);
                break;
            case MJState.ChangeCard:
                // 换牌阶段（游戏进行中的子阶段）
                InitPlayingState(enterData);
                InitChangeCardState(enterData);
                break;
            case MJState.ThrowCard:
                // 血流成河甩牌阶段（游戏进行中的子阶段）
                InitPlayingState(enterData);
                InitThrowCardState(enterData);
                break;
            case MJState.MjPlay:
                InitPlayingState(enterData);
                break;
            case MJState.MjSettle:
                InitSettlementState(enterData);
                break;
            default:
                GF.LogInfo_gsc("未知状态", enterData.CkState.ToString());
                break;
        }

        GF.LogInfo_gsc($"设置麻将游戏状态: {enterData.CkState}");
        GF.LogInfo_gsc("麻将桌子协议初始化完成");
    }

    void SetDeskInfo(Msg_EnterMJDeskRs enterData)
    {
        // 保存麻将配置
        this.mjConfig = enterData.BaseInfo.MjConfig;
        
        // 根据玩法类型创建对应的规则实例
        MJMethod mjMethod = enterData.BaseInfo.MjConfig.MjMethod;
        currentRule = MahjongRuleFactory.CreateRule(mjMethod);
        currentRule.Initialize();

        // 使用规则实例设置桌子信息
        currentRule.SetDeskInfo(enterData);

        // 获取该玩法的总牌数
        mjTotalCount = currentRule.GetTotalCardCount();

        // 设置真实玩家数量
        realPlayerCount = enterData.BaseInfo.BaseConfig.PlayerNum;

        // 设置剩余牌数
        richMjCount = enterData.CardNum;

        // 设置听牌检查器
        mjHuTingCheck.SetCurrentRule(currentRule, this);
        GF.LogInfo_gsc("规则设置", $"已将{MahjongRuleFactory.GetRuleName(mjMethod)}规则设置到听牌检查器");

        GF.LogInfo_gsc("麻将玩法初始化", $"玩法: {MahjongRuleFactory.GetRuleName(mjMethod)}, 总牌数: {mjTotalCount}");
        GF.LogInfo_gsc("玩法描述", MahjongRuleFactory.BuildRoomDescription(enterData.BaseInfo));

        // // 设置听牌规则
        // mahjongGameUI.SetTingPaiRule(enterData.TingPaiType);
        // // 设置杠牌规则
        // mahjongGameUI.SetGangPaiRule(enterData.GangPaiType);
        // // 设置花牌规则
        // mahjongGameUI.SetHuaPaiRule(enterData.HuaPaiType);
        // // 设置是否可抢杠胡
        // mahjongGameUI.SetQiangGangHuRule(enterData.IsQiangGangHu == 1);
        // // 设置是否可一炮多响
        // mahjongGameUI.SetYiPaoDuoXiangRule(enterData.IsYiPaoDuoXiang == 1);
        // // 设置是否可抢杠胡
        // mahjongGameUI.SetHaiDiLaoYueRule(enterData.IsHaiDiLaoYue == 1);
        // // 设置是否可海底炮
        // mahjongGameUI.SetHaiDiPaoRule(enterData.IsHaiDiPao == 1);
        // // 设置是否可杠上花
        // mahjongGameUI.SetGangShangHuaRule(enterData.IsGangShangHua == 1);
        // // 设置是否可杠上炮
        // mahjongGameUI.SetGangShangPaoRule(enterData.IsGangShangPao == 1);
        // // 设置是否可七对加倍
        // mahjongGameUI.SetQiDuiJiaBeiRule(enterData.IsQiDuiJiaBei == 1);
    }

    /// <summary>
    /// 初始化等待状态
    /// </summary>
    void InitWaitingState(Msg_EnterMJDeskRs enterData)
    {
        // 显示等待 其他玩家的UI
        UpdateReadyState(enterData);
    }

    /// <summary>
    /// 初始化卡五星飘分状态
    /// </summary>
    void InitKwxPiaoState(Msg_EnterMJDeskRs enterData)
    {
        GF.LogInfo_gsc("初始化卡五星飘分状态", "等待玩家选择飘分");
        
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        bool myPiaoFinished = false;
        
        // 检查自己是否已完成飘分（飘分信息已在InitPlayingState中恢复显示）
        if (enterData.PiaoInfo != null && enterData.PiaoInfo.Count > 0)
        {
            foreach (var piaoInfo in enterData.PiaoInfo)
            {
                long playerId = piaoInfo.Key;
                // 检查是否是自己
                if (playerId == myPlayerId)
                {
                    myPiaoFinished = true;
                    break;
                }
            }
        }
        
        // 如果自己未完成飘分，显示飘分面板
        if (!myPiaoFinished)
        {
            // 通知UI显示飘分面板
            mahjongGameUI.HandleDeskStateChange(MJState.KwxPiao);
            GF.LogInfo_gsc("重连恢复飘分", "自己未完成飘分，显示飘分面板");
        }
        else
        {
            GF.LogInfo_gsc("重连恢复飘分", "自己已完成飘分，不显示飘分面板");
        }
    }

    /// <summary>
    /// 初始化换牌状态
    /// </summary>
    void InitChangeCardState(Msg_EnterMJDeskRs enterData)
    {
        GF.LogInfo_gsc("初始化换牌状态", "等待玩家选择换牌");
        
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        bool myChangeCardFinished = false;
        List<int> finishedSeatIndices = new List<int>();
        
        // 收集已完成的换牌玩家信息
        if (enterData.ChangeCardPlayer != null && enterData.ChangeCardPlayer.Count > 0)
        {
            foreach (var playerId in enterData.ChangeCardPlayer)
            {
                // 检查是否是自己
                if (playerId == myPlayerId)
                {
                    myChangeCardFinished = true;
                }
                
                // 记录已完成换牌的座位索引
                int seatIdx = GetSeatIndexByPlayerId(playerId);
                if (seatIdx >= 0)
                {
                    finishedSeatIndices.Add(seatIdx);
                    GF.LogInfo_gsc("重连恢复换牌", $"玩家{playerId}(座位{seatIdx})已完成换牌");
                }
            }
        }
        
        // 换牌阶段始终显示换牌面板，以便显示换牌信息
        // 重连时发牌已完成，直接显示完整的换牌UI（HuanPaiAniPanel + XuanPaiPanel + 选牌中状态）
        mahjongGameUI.ShowKWXChangeCardUI();
        
        // 显示换牌面板后，恢复已完成玩家的状态为"选牌结束"
        foreach (var seatIdx in finishedSeatIndices)
        {
            mahjongGameUI.UpdateChangeCardPlayerState(seatIdx, true);
        }
        
        // 如果自己已完成换牌，隐藏操作面板或禁用操作按钮
        if (myChangeCardFinished)
        {
            mahjongGameUI.HideChangeCardOperationPanel();
            GF.LogInfo_gsc("重连恢复换牌", "自己已完成换牌，隐藏换牌操作面板");
        }
        else
        {
            GF.LogInfo_gsc("重连恢复换牌", "自己未完成换牌，显示换牌操作面板");
        }
    }

    /// <summary>
    /// 初始化血流甩牌状态
    /// </summary>
    void InitThrowCardState(Msg_EnterMJDeskRs enterData)
    {
        GF.LogInfo_gsc("初始化甩牌状态", "等待玩家选择甩牌");
        
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        bool myThrowCardFinished = false;
        
        // 检查自己是否已完成甩牌
        if (enterData.ShuaiPai != null && enterData.ShuaiPai.Count > 0)
        {
            myThrowCardFinished = enterData.ShuaiPai.Any(sp => sp.Key == myPlayerId);
        }
        
        // 显示甩牌阶段UI
        mahjongGameUI.HandleDeskStateChange(MJState.ThrowCard);
        
        // 如果自己已完成甩牌，隐藏操作面板
        if (myThrowCardFinished)
        {
            mahjongGameUI.HideThrowCardOperationPanel();
            GF.LogInfo_gsc("重连恢复甩牌", "自己已完成甩牌，隐藏甩牌操作面板");
        }
        else
        {
            GF.LogInfo_gsc("重连恢复甩牌", "自己未完成甩牌，显示甩牌操作面板");
        }
    }

    /// <summary>
    /// 初始化游戏进行状态（断线重连）
    /// </summary>
    void InitPlayingState(Msg_EnterMJDeskRs enterData)
    {
        GF.LogInfo_gsc("初始化游戏进行状态", "处理断线重连数据");
        SetBanker(enterData.Banker);

        // 设置骰子点数
        if (enterData.Dice != null)
        {
            Dict(enterData.Dice.Key, enterData.Dice.Val);
            GF.LogInfo_gsc("设置骰子点数", $"骰子 {enterData.Dice.Key}:{enterData.Dice.Val}");
        }

        // 设置赖子和飘赖
        SetLaiZiCard(Util.ConvertServerIntToFaceValue(enterData.Laizi));
        SetPiaoLaiZi(Util.ConvertServerIntToFaceValue(enterData.PiaoLai));

        // 恢复飘分信息显示（只要有数据就显示，不关乎阶段）
        if (enterData.PiaoInfo != null && enterData.PiaoInfo.Count > 0)
        {
            foreach (var piaoInfo in enterData.PiaoInfo)
            {
                long playerId = piaoInfo.Key;
                int piaoValue = piaoInfo.Val;
                
                // 更新该玩家的飘分显示
                int seatIdx = GetSeatIndexByPlayerId(playerId);
                if (seatIdx >= 0)
                {
                    var seat = mahjongGameUI.GetSeat(seatIdx);
                    if (seat != null)
                    {
                        seat.UpdatePiaoText(piaoValue);
                        GF.LogInfo_gsc("恢复飘分显示", $"玩家{playerId}(座位{seatIdx})飘分: {piaoValue}");
                    }
                }
            }
        }

        // 设置当前轮到操作的座位
        long currentPlayer = enterData.CurrentOption == 0 ? enterData.Banker : enterData.CurrentOption;
        CurrentTurnSeat = GetSeatIndexByPlayerId(currentPlayer);

        // 设置当前操作倒计时
        CurrentNextTime = enterData.NextTime;
        GF.LogInfo_gsc("当前操作倒计时", $"结束时间戳:{CurrentNextTime} 剩余秒数:{GetCurrentCountdownSeconds()}");

        // 恢复所有玩家的牌数据（包含血流甩牌数据）
        RestorePlayersCardData(enterData);

        // 恢复血流胡牌数据
        if (enterData.HuInfo != null && enterData.HuInfo.Count > 0)
        {
            mahjongGameUI.RestoreXueLiuHuInfo(enterData.HuInfo);
        }

        // 恢复2D视图的操作按钮和UI状态
        mahjongGameUI.Restore2DFromReconnectData(enterData);

        // 卡五星玩法：恢复换牌方向按钮（已过换牌阶段但仍在本局游戏中）
        mahjongGameUI.ShowHuanPaiInfoButton();

        GF.LogInfo_gsc("游戏进行状态初始化完成", "所有数据已恢复");
    }

    /// <summary>
    /// 恢复所有玩家的牌数据（断线重连）
    /// 顺序：手牌 → 甩牌（血流） → 吃碰杠 → 弃牌 → 亮牌 → 听牌提示
    /// </summary>
    void RestorePlayersCardData(Msg_EnterMJDeskRs enterData)
    {
        // 先构建甩牌数据映射（血流成河），Key=PlayerId，Val=甩牌列表
        var shuaiPaiMap = BuildShuaiPaiMap(enterData.ShuaiPai);
        bool hasShuaiPai = shuaiPaiMap.Count > 0;
        
        foreach (var otherCard in enterData.OtherCard)
        {
            // 转换座位
            int serverSeatIndex = Util.GetPosByServerPos((Position)otherCard.Pos, realPlayerCount);
            int clientSeatIdx = Util.TransformSeatS2C(serverSeatIndex, selfPosAtServer, realPlayerCount);
            long playerId = otherCard.Player;
            
            // 1. 恢复手牌数据
            if (otherCard.Cards != null && otherCard.Cards.Count > 0)
            {
                var handCardList = new List<MahjongFaceValue>();
                var serverCardList = new List<int>();
                
                foreach (var cardValue in otherCard.Cards)
                {
                    if (cardValue > 0) // -1 表示看不见的牌
                    {
                        MahjongFaceValue faceValue = Util.ConvertServerIntToFaceValue(cardValue);
                        handCardList.Add(faceValue);
                        serverCardList.Add(cardValue);
                    }
                    else
                    {
                        // 其他玩家的暗牌
                        handCardList.Add(MahjongFaceValue.MJ_UNKNOWN);
                        serverCardList.Add(-1);
                    }
                }

                // 判断是否需要将最后一张牌作为摸牌（如果轮到该玩家打牌）
                bool isCurrentPlayer = enterData.CurrentOption > 0 && otherCard.Player == enterData.CurrentOption;
                int moCardValue = -1;
                
                // 自己的手牌需要排序，但如果轮到自己打牌，最后一张是摸牌，不参与排序
                if (clientSeatIdx == 0 && enterData.OperationList.Contains((int)MJOption.Discard))
                {
                    List<int> cardsToSort = serverCardList;
                    
                    // 如果轮到自己打牌，最后一张牌是摸牌
                    if (isCurrentPlayer && serverCardList.Count > 0)
                    {
                        moCardValue = serverCardList[serverCardList.Count - 1];
                        cardsToSort = serverCardList.Take(serverCardList.Count - 1).ToList();
                    }
                    
                    // 排序手牌（不包括摸牌）
                    serverCardList = cardsToSort
                        .Select(s => new { server = s, face = s > 0 ? Util.ConvertServerIntToFaceValue(s) : MahjongFaceValue.MJ_UNKNOWN })
                        .OrderBy(x => (int)x.face)
                        .Select(x => x.server)
                        .ToList();
                    
                }
                var seat = mahjongGameUI.seats2D[clientSeatIdx];
                seat.CreateHandCards(serverCardList);
                if (moCardValue > 0)
                {
                    seat.AddMoPai(moCardValue);
                }
            }

            // 2. 恢复甩牌数据（血流成河特有）- 必须在吃碰杠之前恢复，显示在组牌区最前面
            if (hasShuaiPai && shuaiPaiMap.TryGetValue(playerId, out var throwCards) && throwCards.Count > 0)
            {
                var seat = mahjongGameUI.seats2D[clientSeatIdx];
                if (seat != null)
                {
                    bool isSelf = playerId == Util.GetMyselfInfo().PlayerId;
                    if (isSelf)
                    {
                        // 自己：显示实际甩牌牌值
                        seat.AddMeldGroup(throwCards);
                    }
                    else
                    {
                        // 其他玩家：显示背牌
                        seat.ShowBackCardsInMeldContainer(throwCards.Count);
                    }
                    GF.LogInfo_gsc("恢复甩牌", $"座位:{clientSeatIdx} 甩牌数:{throwCards.Count} 是否自己:{isSelf}");
                }
            }

            // 3. 恢复吃碰杠数据
            if (otherCard.Melds != null && otherCard.Melds.Count > 0)
            {
                foreach (var meld in otherCard.Melds)
                {
                    List<MahjongFaceValue> meldCardList = new List<MahjongFaceValue>();
                    List<int> meldServerCards = new List<int>();
                    
                    if (meld.Card?.Val != null)
                    {
                        foreach (var cardValue in meld.Card.Val)
                        {
                            meldServerCards.Add(cardValue);
                            meldCardList.Add(Util.ConvertServerIntToFaceValue(cardValue));
                        }
                    }

                    // 转换服务器牌型
                    PengChiGangPaiType meldType;
                    
                    // 对于杠牌类型，优先使用subType字段，因为它包含了更详细的杠牌类型信息
                    if (meld.Calc.Key == 3) // 杠牌类型
                    {
                        meldType = ConvertServerKongSubTypeToPcgType(meld.SubType);
                    }
                    else
                    {
                        meldType = ConvertServerMeldTypeToPCG(meld.Calc.Key);
                    }

                    GF.LogInfo_gsc("恢复吃碰杠", $"座位:{clientSeatIdx} 类型:{meldType} 牌数:{meldCardList.Count} subType:{meld.SubType} calcKey:{meld.Calc.Key}");

                    // 显示吃碰杠牌组 - 使用现有的2D座位方法
                    if (clientSeatIdx >= 0 && clientSeatIdx < mahjongGameUI.seats2D.Length)
                    {
                        var seat = mahjongGameUI.seats2D[clientSeatIdx];
                        if (seat != null)
                        {
                            // 使用带类型的AddMeldGroup方法，确保朝天杠只显示3张牌
                            seat.AddMeldGroup(meldType, meldServerCards);
                        }
                    }
                }
            }

            // 4. 恢复弃牌数据
            if (otherCard.Discard != null && otherCard.Discard.Count > 0)
            {
                var discardCardList = new List<int>();
                foreach (var cardValue in otherCard.Discard)
                {
                    discardCardList.Add(cardValue);
                }

                GF.LogInfo_gsc("恢复弃牌", $"座位:{clientSeatIdx} 弃牌数:{discardCardList.Count}");

                // 显示弃牌 - 使用刷新方法一次性创建所有弃牌
                if (clientSeatIdx >= 0 && clientSeatIdx < mahjongGameUI.seats2D.Length)
                {
                    var seat = mahjongGameUI.seats2D[clientSeatIdx];
                    if (seat != null)
                    {
                        // 使用 RefreshDiscardCards 一次性刷新显示
                        seat.RefreshDiscardCards(discardCardList);
                    }
                }
            }

            // 5. 恢复亮牌数据（卡五星特有）
            // 注意：亮牌会清空并重新显示手牌，所以必须在步骤1恢复手牌之后调用
            if (otherCard.ShowCards != null && otherCard.ShowCards.Count > 0)
            {
                var showCardList = new List<int>(otherCard.ShowCards);
                
                GF.LogInfo_gsc("恢复亮牌", $"座位:{clientSeatIdx} 亮牌数:{showCardList.Count}");

                if (clientSeatIdx >= 0 && clientSeatIdx < mahjongGameUI.seats2D.Length)
                {
                    var seat = mahjongGameUI.seats2D[clientSeatIdx];
                    if (seat != null)
                    {
                        // 调用亮牌显示方法（会根据当前手牌计算剩余手牌）
                        seat.ShowLiangPaiCards(showCardList, clientSeatIdx == 0);
                    }
                }
            }

            // 6. 恢复听牌提示（卡五星特有）
            if (otherCard.Ting != null && otherCard.Ting.Count > 0)
            {
                var tingCardList = new List<int>(otherCard.Ting);
                
                GF.LogInfo_gsc("恢复听牌提示", $"座位:{clientSeatIdx} 听牌数:{tingCardList.Count}");

                // 显示听牌提示
                mahjongGameUI.ShowTingPaiTips(clientSeatIdx, tingCardList);
            }
        }
    }

    /// <summary>
    /// 将重连的甩牌数据转换为字典，便于查询
    /// Key=PlayerId, Value=甩牌列表
    /// </summary>
    private Dictionary<long, List<int>> BuildShuaiPaiMap(Google.Protobuf.Collections.RepeatedField<LongListInt> shuaiPai)
    {
        var map = new Dictionary<long, List<int>>();
        if (shuaiPai == null || shuaiPai.Count == 0) return map;

        foreach (var item in shuaiPai)
        {
            if (!map.ContainsKey(item.Key))
            {
                map[item.Key] = item.Vals?.ToList() ?? new List<int>();
            }
        }

        return map;
    }

    /// <summary>
    /// 转换服务器牌型到客户端PengChiGangPaiType枚举
    /// </summary>
    PengChiGangPaiType ConvertServerMeldTypeToPCG(int serverType)
    {
        // 根据协议中的 subType 或 calc.key 值转换
        // 1=刻子(碰), 2=顺子(吃), 3=杠
        switch (serverType)
        {
            case 1: return PengChiGangPaiType.PENG; // 刻子/碰
            case 2: return PengChiGangPaiType.CHI; // 顺子/吃
            case 3: return PengChiGangPaiType.AN_GANG; // 暗杠
            default:
                GF.LogInfo_gsc("未知牌型", $"服务器类型:{serverType}");
                return PengChiGangPaiType.PENG;
        }
    }

    /// <summary>
    /// 初始化结算状态
    /// </summary>
    void InitSettlementState(Msg_EnterMJDeskRs enterData)
    {
        // 游戏结束状态，显示结算界面
        UpdateReadyState(enterData);

        GF.LogInfo_gsc("初始化结算状态", "显示结算界面");
    }

    public Position GetSeatPositionById(long id)
    {
        Position position = Position.Default;
        for (int i = 0; i < deskPlayers.Length; i++)
        {
            if (deskPlayers[i] != null && deskPlayers[i].BasePlayer.PlayerId == id)
            {
                position = deskPlayers[i].Pos;
                break;
            }
        }
        return position;
    }

    /// <summary>
    /// 通过PlayerId查找座位索引
    /// </summary>
    public int GetSeatIndexByPlayerId(long playerId)
    {
        for (int i = 0; i < deskPlayers.Length; i++)
        {
            if (deskPlayers[i] != null && deskPlayers[i].BasePlayer.PlayerId == playerId)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 通过座位索引获取玩家
    /// </summary>
    public DeskPlayer GetDeskPlayer(int seatIdx)
    {
        if (seatIdx >= 0 && seatIdx < deskPlayers.Length)
        {
            return deskPlayers[seatIdx];
        }
        return null;
    }

    /// <summary>
    /// 获取所有非空玩家列表
    /// </summary>
    public List<DeskPlayer> GetAliveDeskPlayers()
    {
        return deskPlayers.Where(p => p != null).ToList();
    }

    /// <summary>
    /// 更新玩家状态（离线/在线/托管）
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="state">玩家状态</param>
    public void UpdatePlayerState(long playerId, PlayerState state)
    {
        int seatIdx = GetSeatIndexByPlayerId(playerId);
        if (seatIdx < 0)
        {
            GF.LogWarning($"[玩家状态更新] 未找到玩家: {playerId}");
            return;
        }

        // 更新玩家数据中的状态
        if (deskPlayers[seatIdx] != null)
        {
            deskPlayers[seatIdx].State = state;
        }

        // 更新UI显示
        var seat = mahjongGameUI?.GetSeat(seatIdx);
        if (seat != null)
        {
            seat.UpdatePlayerStateIcon(state);
            GF.LogInfo($"[玩家状态更新] 座位{seatIdx} 玩家{playerId} 状态:{state}");
        }

        // 如果是自己托管，显示托管取消面板
        if (playerId == Util.GetMyselfInfo()?.PlayerId && state == PlayerState.ToGuo)
        {
            mahjongGameUI?.ShowTuoGuanGo(true);
        }
        else if (playerId == Util.GetMyselfInfo()?.PlayerId && state != PlayerState.ToGuo)
        {
            mahjongGameUI?.ShowTuoGuanGo(false);
        }
    }

    /// <summary>
    /// 更新玩家托管状态（Syn_PlayerAuto专用）
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="isAuto">是否托管</param>
    public void UpdatePlayerAutoState(long playerId, bool isAuto)
    {
        PlayerState newState = isAuto ? PlayerState.ToGuo : PlayerState.OnLine;
        UpdatePlayerState(playerId, newState);
    }

    /// <summary>
    /// 将服务器杠牌子类型转换为客户端PengChiGangPaiType
    /// </summary>
    /// <param name="subType">服务器杠牌子类型 (来自 Msg_Melds.subType)</param>
    /// <returns>对应的客户端PengChiGangPaiType</returns>
    PengChiGangPaiType ConvertServerKongSubTypeToPcgType(int subType)
    {
        switch (subType)
        {
            case 1: // MING 明杠
                return PengChiGangPaiType.GANG;
            case 2: // AN 暗杠
                return PengChiGangPaiType.AN_GANG;
            case 3: // BU 补杠
                return PengChiGangPaiType.BU_GANG;
            case 4: // PI 皮杠
                GF.LogInfo_gsc("皮杠类型转换", $"subType:{subType} → PI_GANG");
                return PengChiGangPaiType.PI_GANG;
            case 5: // LAIZI 癞子杠
                GF.LogInfo_gsc("癞子杠类型转换", $"subType:{subType} → LAIZI_GANG");
                return PengChiGangPaiType.LAIZI_GANG;
            case 6: // CHAOTIAN 朝天杠
                GF.LogInfo_gsc("朝天杠类型转换", $"subType:{subType} → CHAO_TIAN_GANG");
                return PengChiGangPaiType.CHAO_TIAN_GANG;
            case 7: // CHAOTIAN_AN 暗朝天杠
                GF.LogInfo_gsc("暗朝天杠类型转换", $"subType:{subType} → CHAO_TIAN_AN_GANG");
                return PengChiGangPaiType.CHAO_TIAN_AN_GANG;
            default:
                GF.LogInfo_gsc("未知杠牌子类型", $"subType:{subType} → 默认明杠");
                return PengChiGangPaiType.GANG; // 默认明杠
        }
    }


    public void SetDeskPlayer(Msg_EnterMJDeskRs enterData)
    {
        // 初始化数组
        deskPlayers = new DeskPlayer[4];

        // 从enterData获取玩家列表
        var enterDeskPlayers = enterData.DeskPlayers;

        // 找到自己的座位信息
        var mySelf = enterDeskPlayers.FirstOrDefault(dp => dp.BasePlayer.PlayerId == Util.GetMyselfInfo().PlayerId);
        if (mySelf != null)
        {
            selfPosAtServer = Util.GetPosByServerPos(mySelf.Pos, realPlayerCount);
            selfFengWei = Util.GetFengWeiByServerPos(mySelf.Pos, realPlayerCount);
        }

        // 将玩家放入对应的座位索引
        foreach (var deskPlayer in enterDeskPlayers)
        {
            int seatIdx = Util.TransformSeatS2C(Util.GetPosByServerPos(deskPlayer.Pos, realPlayerCount), selfPosAtServer, realPlayerCount);
            GF.LogInfo_gsc("初始化数据: ", $"PlayerId : {deskPlayer.BasePlayer.PlayerId} Pos : {deskPlayer.Pos} SeatIdx: {seatIdx}");
            deskPlayers[seatIdx] = deskPlayer;
        }
    }

    public void Function_SynSendGift(long basePlayerID, long toPlayerId, string gift)
    {
        int from = Util.TransformSeatS2C(Util.GetPosByServerPos(GetSeatPositionById(basePlayerID), realPlayerCount), selfPosAtServer, realPlayerCount);
        int to = Util.TransformSeatS2C(Util.GetPosByServerPos(GetSeatPositionById(toPlayerId), realPlayerCount), selfPosAtServer, realPlayerCount);
        GF.LogInfo_gsc("收到礼物", $"from:{from} to:{to} gift:{gift}");
        mahjongGameUI.Function_SynSendGift(from, to, gift);
    }

    public void StartGame(PBMJStart ack)
    {
       Sound.PlayEffect(AudioKeys.MJ_EFFECT_AUDIO_GAMESTART);

        // 更新剩余牌数
        richMjCount = ack.CardNum;
        // 设置当前轮到操作的座位
        CurrentTurnSeat = GetSeatIndexByPlayerId(ack.Banker);
        CurrentNextTime = ack.NextTime;
        SetBanker(ack.Banker);

        // 更新局数显示
        mJGameProcedure.enterMJDeskRs.GameNumbers++;
        mahjongGameUI.Update2DRoomInfo();

        // 设置骰子数据
        if (ack?.Dice != null)
        {
            Dict(ack.Dice.Key, ack.Dice.Val);
        }

        // 在洗牌前设置赖子数据
        if (currentRule != null && ack != null)
        {
            currentRule.SetupLaiZiBeforeXiPai(ack);
        }

        // 2D模式：直接通过UI处理游戏开始
        mahjongGameUI.Handle2DGameStart(ack);
        GF.LogInfo("[2D游戏] 开始游戏 - 2D发牌流程");

    }

    public void Function_DeskPlayerInfoRs(DeskPlayer deskPlayer)
    {
        if(deskPlayer.Pos == Position.NoSit) return;
        int seatIdx = Util.TransformSeatS2C(Util.GetPosByServerPos(deskPlayer.Pos, realPlayerCount), selfPosAtServer, realPlayerCount);
        if (seatIdx < 0)
        {
            GF.LogError($"无效的座位索引: Pos: {deskPlayer.Pos} realPlayerCount: {realPlayerCount} selfPosAtServer: {selfPosAtServer}");
            return;
        }
        // 更新数组中的玩家信息
        deskPlayers[seatIdx] = deskPlayer;
        // 2D模式响应：玩家进入
        mahjongGameUI.Update2DPlayerInfo(seatIdx, deskPlayer);
    }

    public void Function_SynSitUp(Position Pos)
    {
        int seatIdx = Util.TransformSeatS2C(Util.GetPosByServerPos(Pos, realPlayerCount), selfPosAtServer, realPlayerCount);
        if (seatIdx < 0)
        {
            GF.LogError($"无效的座位索引: Pos: {Pos} realPlayerCount: {realPlayerCount} selfPosAtServer: {selfPosAtServer}");
            return;
        }
        // 清除该座位的玩家
        if (deskPlayers[seatIdx] != null)
        {
            GF.LogInfo_gsc("玩家站起: ", $"Pos : {Pos} seatIdx : {seatIdx} PlayerId: {deskPlayers[seatIdx].BasePlayer.PlayerId}");
            deskPlayers[seatIdx] = null;
        }
        // 2D模式响应：玩家离开
        mahjongGameUI.Handle2DPlayerLeave(seatIdx);
    }

    /// <summary>
    /// 麻将操作
    /// </summary>
    /// <param name="ack"></param>
    /// <param name="option"></param>
    public void Function_MJOption(Msg_MJOption ack, MJOption option)
    {
        Msg_OptionPlayer optionPlayer = ack.OptionPlayer;
        Msg_OptionPlayer formPlayer = ack.Form;
        int card = ack.Card;
        MahjongFaceValue mahjongFaceValue = Util.ConvertServerIntToFaceValue(card);

        int optionSeatIdx = optionPlayer == null ? -1 : Util.TransformSeatS2C(Util.GetPosByServerPos((Position)optionPlayer.Pos, realPlayerCount), selfPosAtServer, realPlayerCount);
        int formSeatIdx = formPlayer == null ? -1 : Util.TransformSeatS2C(Util.GetPosByServerPos((Position)formPlayer.Pos, realPlayerCount), selfPosAtServer, realPlayerCount);

        // 更新倒计时时间戳
        if (ack.NextTime > 0)
        {
            CurrentNextTime = ack.NextTime;
        }
        int nextTime = Util.GetRemainTime(ack.NextTime);

        GF.LogInfo($"[2D模式] 处理麻将操作: {option} 座位:{optionSeatIdx} 牌值:{mahjongFaceValue}");

        switch (option)
        {
            case MJOption.Draw:
                // 摸牌时减少剩余牌数
                richMjCount--;

                if (richMjCount == realPlayerCount - 1)
                {
                    //最后几张牌
                    GF.LogInfo_gsc("提示", "牌堆只剩最后一圈牌了");
                    mahjongGameUI.ShowEffect($"Last{realPlayerCount}Eff", 2f);
                }
                
                // 2D摸牌
                mahjongGameUI.Handle2DMoPai(optionSeatIdx, card);
                // 更新轮次指示和倒计时
                mahjongGameUI.Set2DTurnSeat(optionSeatIdx);
                mahjongGameUI.Update2DCountdown(nextTime);
                break;

            case MJOption.Discard:
                HandPaiType daPaiType = HandPaiType.HandPai;
                int daPaiIdx = 0;

                if (optionSeatIdx == 0)
                {
                    // 玩家出牌：使用玩家在UI中实际选择的牌索引
                    int selectIdx = mahjongGameUI.GetSelect2DPaiHandPaiIdx();
                    HandPaiType selectType = mahjongGameUI.GetSelect2DPaiType();

                    if (selectIdx >= 0 && (selectType == HandPaiType.HandPai || selectType == HandPaiType.MoPai))
                    {
                        // 使用玩家UI选择的具体牌索引信息
                        daPaiType = selectType;
                        daPaiIdx = selectIdx;
                        GF.LogInfo($"[2D模式] 玩家打出UI选择的具体牌: seat={optionSeatIdx} value={mahjongFaceValue} type={selectType} idx={selectIdx}");

                        // 清除已使用的选择信息，避免重复使用
                        mahjongGameUI.ClearSelect2DPaiInfo();
                    }
                    else
                    {
                        // 玩家没有选择牌（托管/自动出牌）
                        var seat = mahjongGameUI.GetSeat(0);
                        
                        // 亮牌后只能打摸牌
                        if (seat != null && seat.HasLiangPai)
                        {
                            daPaiType = HandPaiType.MoPai;
                            daPaiIdx = 0;
                            GF.LogInfo($"[2D模式] 托管/自动打牌 - 已亮牌，直接打摸牌");
                        }
                        // 血流胡牌后只能打摸牌（手牌已禁用交互）
                        else if (seat != null && seat.IsXueLiuHu)
                        {
                            daPaiType = HandPaiType.MoPai;
                            daPaiIdx = 0;
                            GF.LogInfo($"[2D模式] 托管/自动打牌 - 血流已胡牌，直接打摸牌");
                        }
                        else
                        {
                            // 托管出牌逻辑：
                            // 1. 先判断摸牌区有没有牌，如果有且牌值匹配就打摸牌
                            // 2. 没有摸牌就去手牌找第一个匹配的牌打出
                            int moPaiCount = seat != null ? seat.GetMoPaiCount() : 0;
                            
                            if (moPaiCount > 0)
                            {
                                // 有摸牌，检查摸牌是否是要打的牌
                                int moPaiValue = seat.GetMoPaiCardValue();
                                if (moPaiValue == card)
                                {
                                    daPaiType = HandPaiType.MoPai;
                                    daPaiIdx = 0;
                                    GF.LogInfo($"[2D模式] 托管出牌 - 摸牌匹配，打摸牌: cardValue={card}");
                                }
                                else
                                {
                                    // 摸牌不匹配，从手牌找
                                    var handCards = seat.GetHandCardValues();
                                    int foundIdx = -1;
                                    for (int i = 0; i < handCards.Count; i++)
                                    {
                                        if (handCards[i] == card)
                                        {
                                            foundIdx = i;
                                            break;
                                        }
                                    }
                                    if (foundIdx >= 0)
                                    {
                                        daPaiType = HandPaiType.HandPai;
                                        daPaiIdx = foundIdx;
                                        GF.LogInfo($"[2D模式] 托管出牌 - 从手牌找到匹配: cardValue={card} idx={foundIdx}");
                                    }
                                    else
                                    {
                                        // 手牌也没找到，打摸牌（异常情况）
                                        daPaiType = HandPaiType.MoPai;
                                        daPaiIdx = 0;
                                        GF.LogWarning($"[2D模式] 托管出牌 - 未找到匹配牌，默认打摸牌: cardValue={card}");
                                    }
                                }
                            }
                            else
                            {
                                // 没有摸牌，从手牌找第一个匹配的牌
                                var handCards = seat.GetHandCardValues();
                                int foundIdx = -1;
                                for (int i = 0; i < handCards.Count; i++)
                                {
                                    if (handCards[i] == card)
                                    {
                                        foundIdx = i;
                                        break;
                                    }
                                }
                                if (foundIdx >= 0)
                                {
                                    daPaiType = HandPaiType.HandPai;
                                    daPaiIdx = foundIdx;
                                    GF.LogInfo($"[2D模式] 托管出牌 - 无摸牌，从手牌找到: cardValue={card} idx={foundIdx}");
                                }
                                else
                                {
                                    // 异常情况，手牌也没找到
                                    daPaiType = HandPaiType.HandPai;
                                    daPaiIdx = 0;
                                    GF.LogWarning($"[2D模式] 托管出牌 - 无摸牌且手牌未找到匹配: cardValue={card}");
                                }
                            }
                        }
                        
                        mahjongGameUI.ClearSelect2DPaiInfo();
                    }
                }
                else
                {
                    // 其他玩家：优先从摸牌打出，如果没有摸牌则从手牌打出
                    var seat = mahjongGameUI.GetSeat(optionSeatIdx);
                    if (seat != null)
                    {
                        int handCount = seat.GetHandCardCount();
                        int moPaiCount = seat.GetMoPaiCount();

                        // 优先从摸牌区打出（如果有摸牌的话）
                        if (moPaiCount > 0)
                        {
                            daPaiType = HandPaiType.MoPai;
                            daPaiIdx = 0;
                        }
                        else if (handCount > 0)
                        {
                            // 没有摸牌，从手牌随机打出
                            daPaiType = HandPaiType.HandPai;
                            daPaiIdx = UnityEngine.Random.Range(0, handCount);
                        }
                        else
                        {
                            // 既没有手牌也没有摸牌，这种情况不应该发生
                            GF.LogWarning($"[2D模式] 其他玩家{optionSeatIdx}既没有手牌也没有摸牌，无法打牌");
                            daPaiType = HandPaiType.HandPai;
                            daPaiIdx = 0;
                        }
                        
                        GF.LogInfo($"[2D模式] 其他玩家{optionSeatIdx}打牌: type={daPaiType} idx={daPaiIdx} (手牌{handCount}张, 摸牌{moPaiCount}张)");
                    }
                }
                //暂停倒计时
                mahjongGameUI.Start2DCountdown(0);
                mahjongGameUI.Handle2DDaPai(optionSeatIdx, card, daPaiType, daPaiIdx);
                break;

            case MJOption.Chi:
                // 2D吃牌
                mahjongGameUI.Handle2DChiPai(optionSeatIdx, ack.Calc.Card.Val.ToArray(), formSeatIdx);
                // 更新轮次指示和倒计时
                mahjongGameUI.Set2DTurnSeat(optionSeatIdx);
                mahjongGameUI.Update2DCountdown(nextTime);
                break;

            case MJOption.Pen:
                // 2D碰牌
                mahjongGameUI.Handle2DPengPai(optionSeatIdx, ack.Calc.Card.Val.ToArray(), formSeatIdx);
                // 更新轮次指示和倒计时
                mahjongGameUI.Set2DTurnSeat(optionSeatIdx);
                mahjongGameUI.Update2DCountdown(nextTime);
                break;

            case MJOption.Gang:
                // 2D杠牌
                if (ack.Calc?.Card?.Val != null)
                {
                    int[] cardValues = ack.Calc.Card.Val.ToArray();
                    PengChiGangPaiType gangType = ConvertServerKongSubTypeToPcgType(ack.Calc?.SubType ?? 0);
                    mahjongGameUI.Handle2DGangPai(optionSeatIdx, cardValues, gangType, formSeatIdx);
                }
                // 更新轮次指示和倒计时
                mahjongGameUI.Set2DTurnSeat(optionSeatIdx);
                mahjongGameUI.Update2DCountdown(nextTime);
                break;

            case MJOption.Ting:
                // 2D听牌 (卡五星亮牌)
                GF.LogInfo($"[2D模式] 玩家{optionSeatIdx}听牌, 打出牌:{mahjongFaceValue}");
                
                // 1. 先打出听牌的牌（同打牌逻辑）
                HandPaiType tingDaPaiType = HandPaiType.HandPai;
                int tingDaPaiIdx = 0;

                if (optionSeatIdx == 0)
                {
                    // 玩家出牌：使用玩家在UI中实际选择的牌索引
                    int selectIdx = mahjongGameUI.GetSelect2DPaiHandPaiIdx();
                    HandPaiType selectType = mahjongGameUI.GetSelect2DPaiType();

                    if (selectIdx >= 0 && (selectType == HandPaiType.HandPai || selectType == HandPaiType.MoPai))
                    {
                        tingDaPaiType = selectType;
                        tingDaPaiIdx = selectIdx;
                        GF.LogInfo($"[2D模式] 玩家听牌打出UI选择的牌: seat={optionSeatIdx} value={mahjongFaceValue} type={selectType} idx={selectIdx}");
                        mahjongGameUI.ClearSelect2DPaiInfo();
                    }
                    else
                    {
                        GF.LogWarning($"[2D模式] 玩家听牌选择信息无效，使用默认值: selectIdx={selectIdx} selectType={selectType}");
                    }
                }
                else
                {
                    // 其他玩家：随机决定从手牌还是摸牌打出
                    var seat = mahjongGameUI.GetSeat(optionSeatIdx);
                    if (seat != null)
                    {
                        int handCount = seat.GetHandCardCount();
                        int moPaiCount = seat.GetMoPaiCount();

                        if (moPaiCount > 0)
                        {
                            tingDaPaiType = HandPaiType.MoPai;
                            tingDaPaiIdx = 0;
                        }
                        else
                        {
                            if (UnityEngine.Random.value < 0.5f)
                            {
                                tingDaPaiType = HandPaiType.MoPai;
                                tingDaPaiIdx = 0;
                            }
                            else
                            {
                                tingDaPaiType = HandPaiType.HandPai;
                                tingDaPaiIdx = UnityEngine.Random.Range(0, handCount);
                            }
                        }
                    }
                }
                
                // 暂停倒计时
                mahjongGameUI.Start2DCountdown(0);
                // 执行打牌动作
                mahjongGameUI.Handle2DDaPai(optionSeatIdx, card, tingDaPaiType, tingDaPaiIdx);
                
                // 2. 打出牌后，计算并显示听牌提示（哪些牌可胡，番数，剩余张数）
                if (ack.Ting != null && ack.Ting.Count > 0)
                {
                    var tingCards = ack.Ting.Select(t => t.Param1).ToList();
                    mahjongGameUI.ShowTingPaiTips(optionSeatIdx, tingCards);
                    GF.LogInfo($"[2D模式] 显示听牌提示: 座位{optionSeatIdx}, 听牌: {string.Join(",", tingCards)}");
                }
                break;

            case MJOption.Guo:
                // 2D过
                GF.LogInfo($"[2D模式] 玩家{optionSeatIdx}过");
                break;
        }
        mahjongGameUI.Show2DOperationButtons(ack.OptionList.Select(opt => (MJOption)opt).ToList());
    }

    /// <summary>
    /// 现在分开两个协议 这个协议处理自己可执行的操作 card没有值需要从最后出的牌或者自己最后摸得牌自行判定
    /// </summary>
    /// <param name="ack"></param>
    public void Function_MJOption8(Msg_MJOption ack)
    {
        mahjongGameUI.Show2DOperationButtons(ack.OptionList.Select(opt => (MJOption)opt).ToList());
    }

    public void Function_Msg_Hu(Msg_Hu ack)
    {
        //暂停倒计时
        mahjongGameUI.Start2DCountdown(0);
        if (ack.LiuJu)
        {
            mahjongGameUI.Handle2DLoseGame(ack);
            return;
        }
        mahjongGameUI.Handle2DHuPai(ack);
    }

    public void Function_Syn_HHCoinChange(Syn_HHCoinChange ack)
    {
        //如果有分数变化得分效果
        foreach (var change in ack.ScoreChange)
        {
            int seatIdx = GetSeatIndexByPlayerId(change.Key);
            if (seatIdx >= 0 && seatIdx < 4)
            {
                double scoreChange = change.Val;
                mahjongGameUI.CoinChange(seatIdx, scoreChange);
            }
        }
    }

    public void Function_Msg_MJReadyRs(Msg_MJReadyRs ack)
    {
        mahjongGameUI.Handle2DMJReadyRs(ack);
        mahjongGameUI.SetReadyState(0, ack.ReadyState == 1);
    }

    public void Function_Msg_SynMJReady(Msg_SynMJReady ack)
    {
        int seatIdx = Util.TransformSeatS2C(Util.GetPosByServerPos(GetSeatPositionById(ack.PlayerId), realPlayerCount), selfPosAtServer, realPlayerCount);
        if (seatIdx == -1)
        {
            GF.LogInfo_gsc("座位转换失败: ", $"PlayerId : {ack.PlayerId} selfPosAtServer : {selfPosAtServer}");
            return;
        }

        mahjongGameUI.Handle2DSynMJReady(ack, seatIdx);
        mahjongGameUI.SetReadyState(seatIdx, ack.ReadyState == 1);
    }

    public void UpdateReadyState(Msg_EnterMJDeskRs enterData)
    {
        for (int seatIdx = 0; seatIdx < deskPlayers.Length; seatIdx++)
        {
            var player = deskPlayers[seatIdx];
            if (player != null)
            {
                mahjongGameUI.SetReadyState(seatIdx, player.Ready);
            }
        }
    }

    public void Function_Msg_SynMJDismiss(Msg_SynMJDismiss ack)
    {
        mahjongGameUI.Function_Msg_SynMJDismiss(ack);
    }

    public void OnGFEventChanged(object sender, GameEventArgs e)
    {
        var args = e as GFEventArgs;
        var t = args.UserData as Msg_DeskChat;
        switch (args.EventType)
        {
            case GFEventType.eve_SynDeskChat:
                if (t.Type == 0)//文本
                {
                    int seatIdx = Util.TransformSeatS2C(Util.GetPosByServerPos(GetSeatPositionById(t.Sender.PlayerId), realPlayerCount), selfPosAtServer, realPlayerCount);
                    mahjongGameUI.ShowChatText(seatIdx, t.Chat);
                }
                else if (t.Type == 1)//快捷
                {
                    // if (Util.IsMySelf(t.Sender.PlayerId))
                    // {
                    //     mahjongGameUI.chatPanel.SetActive(false);
                    // }
                    int seatIdx = Util.TransformSeatS2C(Util.GetPosByServerPos(GetSeatPositionById(t.Sender.PlayerId), realPlayerCount), selfPosAtServer, realPlayerCount);
                    mahjongGameUI.ShowChatText(seatIdx, t.Chat);
                    
                }
                else if (t.Type == 2)//表情
                {
                    if (Util.IsMySelf(t.Sender.PlayerId))
                    {
                        mahjongGameUI.chatPanel.SetActive(false);
                    }
                    int seatIdx = Util.TransformSeatS2C(Util.GetPosByServerPos(GetSeatPositionById(t.Sender.PlayerId), realPlayerCount), selfPosAtServer, realPlayerCount);
                    mahjongGameUI.ShowEmoji(seatIdx, t.Chat);
                }
                else if (t.Type == 3)//语音
                {
                    CoroutineRunner.Instance.StartCoroutine(DownloadAndPlayMp3(t.Sender.PlayerId, t.Voice));
                }
                break;
            case GFEventType.eve_ReConnectGame:
                if (Util.GetMyselfInfo().DeskId != 0 && mJGameProcedure.enterMJDeskRs != null &&
                    Util.GetMyselfInfo().DeskId == mJGameProcedure.enterMJDeskRs.BaseInfo.DeskId)
                {
                    Util.GetInstance().Send_EnterDeskRq(mJGameProcedure.enterMJDeskRs.BaseInfo.DeskId);
                }
                else
                {
                    GF.LogInfo("返回登录界面");
                    HomeProcedure homeProcedure = GF.Procedure.GetProcedure<HomeProcedure>() as HomeProcedure;
                    homeProcedure.QuitGame();
                }
                break;
        }
    }

    IEnumerator DownloadAndPlayMp3(long playerid, string url)
    {
        int deskID = mJGameProcedure.enterMJDeskRs.BaseInfo.DeskId;
        // if (Util.GetInstance().IsMy(playerid) == true) yield break;
        string downloadUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}:{Const.ServerInfo_BackEndPort}/voice/{deskID}/";
        if (Const.CurrentServerType == Const.ServerType.外网服)
        {
            downloadUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}/voice/{deskID}/";
        }
        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(downloadUrl + url, AudioType.MPEG);
        request.certificateHandler = new BypassCertificate();
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            GF.LogError($"Error downloading audio: {request.error}");
            yield break;
        }
        int seatIdx = Util.TransformSeatS2C(Util.GetPosByServerPos(GetSeatPositionById(playerid), realPlayerCount), selfPosAtServer, realPlayerCount);
        mahjongGameUI.ShowVoiceWave(seatIdx);
        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
        SoundManager.Instance.PlaySFX(clip, 0, () =>
        {
            mahjongGameUI.HideVoiceWave(seatIdx);
        });
    }

    void SetBanker(long banker)
    {
        int seatIdx = GetSeatIndexByPlayerId(banker);
        if (seatIdx >= 0)
        {
            var dp = deskPlayers[seatIdx];
            dealerSeatIdx = seatIdx;
        }
        else
        {
            dealerSeatIdx = 0;
        }
    }

    void Dict(int dict1, int dict2)
    {
        dictNum[0] = dict1;
        dictNum[1] = dict2;
    }

    /// <summary>
    /// 获取骰子的key值（用于判断换牌方向）
    /// </summary>
    public int GetDiceKey()
    {
        return dictNum[0];
    }

    /// <summary>
    /// 获取骰子的val值
    /// </summary>
    public int GetDiceVal()
    {
        return dictNum[1];
    }

        /// <summary>
    /// 排序手牌 - 统一使用 Util.SortMahjongHandCards
    /// 显示规则：从左到右，靠右对齐（节点索引从小到大）
    /// - 索引0 → 显示在最左边 → 赖子（最小）
    /// - 索引n-1 → 显示在最右边(x=0) → 最大的牌
    /// 排序策略：赖子在前（索引小），普通牌升序（万→筒→条→风→中发白）
    /// </summary>
    public List<int> SortHandCards(List<int> cards)
    {
        // 直接使用 Util 的统一排序方法
        return Util.SortMahjongHandCards(cards, Util.ConvertFaceValueToServerInt(CurrentLaiZi));
    }

    #endregion

}
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 跑的快游戏常量和配置
/// </summary>
public static class PDKConstants
{
    #region 游戏基础配置

    /// <summary>
    /// 支持的玩家数量
    /// </summary>
    public enum PlayerCount
    {
        Two = 2,    // 2人局
        Three = 3   // 3人局
    }

    /// <summary>
    /// 卡牌花色
    /// </summary>
    public enum CardSuit
    {
        Spades = 1,    // 黑桃
        Hearts = 2,    // 红桃
        Clubs = 3,     // 梅花
        Diamonds = 4   // 方块
    }

    /// <summary>
    /// 卡牌点数（跑得快中的大小顺序）
    /// </summary>
    public enum CardValue
    {
        Three = 3,   // 3最小
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14,    // A比K大
        Two = 15     // 2最大
    }

    /// <summary>
    /// 牌型（根据服务器协议定义 + 客户端扩展）
    /// </summary>
    public enum CardType
    {
        // ===== 服务器协议枚举值 =====
        UNNOWN = 0,              // 未知牌型
        SINGLE = 1,              // 单张
        PAIR = 2,                // 对子
        STRAIGHT_PAIR = 3,       // 连对
        STRAIGHT = 4,            // 顺子
        THREE_WITH_ONE = 5,      // 三带一
        THREE_WITH_TWO = 6,      // 三带二
        FOUR_WITH_TWO = 7,       // 四带二
        FOUR_WITH_THREE = 8,     // 四带三
        BOMB = 9,                // 炸弹
        PLANE = 10,              // 飞机
        THREE_WITH_NO = 11//三不带
    }

    #endregion

    #region 游戏规则配置

    /// <summary>
    /// 游戏规则配置
    /// </summary>
    [Serializable]
    public class GameRuleConfig
    {
        [Header("先出规则")]
        public bool firstRoundMinCard = true;        // 首局最小牌先出
        public bool everyRoundMinCard = false;       // 每局最小牌先出
        public bool firstRoundBlackThree = true;    // 首局黑桃3先出

        [Header("出牌规则")]
        public bool mustPlayMin = true;              // 必带最小
        public bool freePlay = false;                // 任意出牌

        public bool bigMustPress = true;             // 有大必压
        public bool canNotPress = false;             // 可不压


        [Header("玩法规则")]
        public bool canFourWithTwo = false;          // 可四带二 1
        public bool canFourWithThree = false;        // 可四带三 2
        public bool bombCannotSplit = true;          // 炸弹不可拆 3
        public bool aaaIsBomb = false;               // AAA为炸弹 4
        public bool redTenHole = false;              // 红桃10扎鸟 5
        public bool bombNoDouble = false;            // 炸弹不翻倍 6
        public bool bombPressNoScore = false;        // 炸弹被压无分 7
        public bool threeAnyPlay = false;            // 三带任意出 8
        public bool canReverse = false;              // 可反的 9
        public bool canRob = false;                  // 抢庄 10
        public bool lastThreeCanLess = false;        // 最后三张(飞机)可少带完 11
        public bool fastPass = false;                // 快速过牌 12
        public bool cutCard = false;                 // 切牌 13
        public bool showRemainCard = false;          // 显示剩牌 14
        public bool recordHistory = false;           // 记牌器道具 15
        public bool doubleScore = false;             // 加倍 16
        public bool advanceFold = false;             // 提前亮牌 17

        /// <summary>
        /// 从服务器配置初始化游戏规则
        /// </summary>
        /// <param name="config">服务器返回的跑得快配置</param>
        public void InitFromServerConfig(NetMsg.RunFastConfig config)
        {
            if (config == null)
            {
                Debug.LogWarning("[GameRuleConfig] 服务器配置为空，使用默认规则");
                return;
            }

            // 设置先出规则 (FirstRun: 0=首局最小牌先出, 1=每局最小牌先出, 2=首局黑桃3先出)
            firstRoundMinCard = (config.FirstRun == 0);
            everyRoundMinCard = (config.FirstRun == 1);
            firstRoundBlackThree = (config.FirstRun == 2);

            // 设置出牌规则 (WithMin: 1=必带最小, 2=任意出牌)
            mustPlayMin = (config.WithMin == 1);
            freePlay = (config.WithMin == 2);

            // 设置玩法规则 (Play: 1=有大必压, 2=可不压)
            bigMustPress = (config.Play == 1);
            canNotPress = (config.Play == 2);

            // 重置所有复选框规则为false
            canFourWithTwo = false;
            canFourWithThree = false;
            bombCannotSplit = false;
            aaaIsBomb = false;
            redTenHole = false;
            bombNoDouble = false;
            bombPressNoScore = false;
            threeAnyPlay = false;
            canReverse = false;
            canRob = false;
            lastThreeCanLess = false;
            fastPass = false;
            cutCard = false;
            showRemainCard = false;
            recordHistory = false;
            doubleScore = false;
            advanceFold = false;

            // 根据服务器Check数组设置复选框规则
            if (config.Check != null && config.Check.Count > 0)
            {
                foreach (int ruleNumber in config.Check)
                {
                    switch (ruleNumber)
                    {
                        case 1: canFourWithTwo = true; break;          // 可四带二
                        case 2: canFourWithThree = true; break;        // 可四带三
                        case 3: bombCannotSplit = true; break;         // 炸弹不可拆
                        case 4: aaaIsBomb = true; break;               // AAA为炸弹
                        case 5: redTenHole = true; break;              // 红桃10扎鸟
                        case 6: bombNoDouble = true; break;            // 炸弹不翻倍
                        case 7: bombPressNoScore = true; break;        // 炸弹被压无分
                        case 8: threeAnyPlay = true; break;            // 三带任意出
                        case 9: lastThreeCanLess = true; break;        // 最后三张可少带完
                        case 10: canRob = true; break;                 // 抢庄
                        case 11: canReverse = true; break;             // 可反的
                        case 12: fastPass = true; break;               // 快速过牌
                        case 13: cutCard = true; break;                // 切牌
                        case 14: showRemainCard = true; break;         // 显示剩牌
                        case 15: recordHistory = true; break;          // 记牌器道具
                        case 16: doubleScore = true; break;            // 加倍
                        case 17: advanceFold = true; break;            // 提前亮牌
                    }
                }
            }
            Debug.Log($"[GameRuleConfig] 规则配置已初始化 - 先出:{config.FirstRun}, 出牌:{config.WithMin}, 玩法:{config.Play}, 复选框数量:{config.Check?.Count ?? 0}");
        }
    }

    #endregion

    #region 游戏状态

    /// <summary>
    /// 游戏状态
    /// </summary>
    public enum GameState
    {
        RUNFAST_WAIT,        // 等待开始
        RUNFAST_START,        // 发牌中
        RUNFAST_DISCARD,        // 游戏中
        RUNFAST_SETTLE        // 游戏结束
    }

    /// <summary>
    /// 玩家状态
    /// </summary>
    public enum PlayerState
    {
        Waiting,        // 等待
        Thinking,       // 思考中
        Played,         // 已出牌
        Passed,         // 已过牌
        Finished        // 已出完
    }

    #endregion

    #region 游戏配置

    /// <summary>
    /// 2人局手牌数
    /// </summary>
    public const int CARDS_PER_PLAYER_TWO = 16;

    /// <summary>
    /// 3人局手牌数
    /// </summary>
    public const int CARDS_PER_PLAYER_THREE = 16;

    /// <summary>
    /// 出牌思考时间(秒)
    /// </summary>
    public const int THINK_TIME = 30;

    /// <summary>
    /// 最小顺子长度
    /// </summary>
    public const int MIN_STRAIGHT_LENGTH = 5;

    /// <summary>
    /// 最小连对长度（对子数量，2表示需要2个对子即4张牌）
    /// </summary>
    public const int MIN_PAIR_STRAIGHT_LENGTH = 2;

    #endregion

    #region UI配置常量

    /// <summary>
    /// 默认手牌数量（用于UI测试）
    /// </summary>
    public const int DEFAULT_HAND_CARD_COUNT = 16;

    /// <summary>
    /// 屏幕宽度使用比例
    /// </summary>
    public const float SCREEN_WIDTH_USAGE = 0.9f;

    /// <summary>
    /// 基础卡牌宽度
    /// </summary>
    public const float BASE_CARD_WIDTH = 220f;

    /// <summary>
    /// 基础卡牌高度
    /// </summary>
    public const float BASE_CARD_HEIGHT = 308f;

    /// <summary>
    /// 卡牌宽高比
    /// </summary>
    public const float CARD_ASPECT_RATIO = BASE_CARD_HEIGHT / BASE_CARD_WIDTH;

    /// <summary>
    /// 理想卡牌间距比例（相对于卡牌宽度）
    /// </summary>
    public const float IDEAL_SPACING_RATIO = 0.6f;

    /// <summary>
    /// 最小卡牌间距比例（相对于卡牌宽度）
    /// </summary>
    public const float MIN_SPACING_RATIO = 0.15f;

    /// <summary>
    /// 卡牌动画持续时间
    /// </summary>
    public const float CARD_ANIMATION_DURATION = 0.2f;

    /// <summary>
    /// 卡牌选中时上移距离
    /// </summary>
    public const float CARD_SELECT_OFFSET_Y = 30f;

    /// <summary>
    /// 卡牌选中动画时间
    /// </summary>
    public const float CARD_SELECT_ANIMATION_DURATION = 0.15f;

    /// <summary>
    /// 卡牌拖拽高亮颜色（灰色）- RGB(128, 128, 128)
    /// </summary>
    public static readonly UnityEngine.Color CARD_HIGHLIGHT_COLOR = new UnityEngine.Color(0.5f, 0.5f, 0.5f, 1f);

    /// <summary>
    /// 长按触发时间（秒）
    /// </summary>
    public const float LONG_PRESS_DURATION = 0.5f;

    /// <summary>
    /// 长按选择范围（像素）
    /// </summary>
    public const float LONG_PRESS_SELECTION_RANGE = 100f;

    #endregion

    #region 卡牌排序配置

    /// <summary>
    /// 花色排序权重（数值越小优先级越高）
    /// 跑的快规则：黑桃 > 红桃 > 梅花 > 方片
    /// 相同牌时，方块最小排在后面（手牌从左到右：大 -> 小，方块在最右边）
    /// </summary>
    public static readonly int[] SUIT_ORDER_WEIGHTS = { 0, 1, 2, 3 }; // 对应方片、梅花、红桃、黑桃

    /// <summary>
    /// 点数排序权重映射
    /// 跑得快规则：3最小，2最大
    /// </summary>
    public static readonly Dictionary<int, int> VALUE_ORDER_WEIGHTS = new Dictionary<int, int> 
    {
        { 3, 1 },   // 3最小
        { 4, 2 },
        { 5, 3 },
        { 6, 4 },
        { 7, 5 },
        { 8, 6 },
        { 9, 7 },
        { 10, 8 },
        { 11, 9 },  // J
        { 12, 10 }, // Q
        { 13, 11 }, // K
        { 1, 12 },  // A
        { 2, 13 }   // 2最大
    };

    #endregion

    #region 游戏规则配置（从服务器获取）

    /// <summary>
    ///chu牌规则：0=必带最小, 1=任意出牌
    /// </summary>
    public static int chupai = 0;
    /// <summary>
    /// 先出规则：0=首局最小牌先出, 1=每局最小牌先出, 2=首局黑桃3先出
    /// </summary>
    public static int xianchuRule = 0;

    /// <summary>
    /// 玩法规则：1=有大必压, 2=可不压
    /// </summary>
    public static int wanfa = 1;

    /// <summary>
    /// 复选框规则（从1开始编号）
    /// 1-可四带二, 2-可四带三, 3-炸弹不可拆, 4-AAA为炸弹, 5-红桃10扎鸟
    /// 6-炸弹不翻倍, 7-炸弹被压无分, 8-三带任意出, 9-最后三张可少带完
    /// 10-抢庄, 11-可反的, 12-快速过牌, 13-切牌, 14-显示剩牌
    /// 15-记牌器道具, 16-加倍, 17-提前亮牌
    /// </summary>
    public static int[] CheckboxRules = new int[] { };

    /// <summary>
    /// 从服务器配置更新游戏规则
    /// </summary>
    /// <param name="config">服务器返回的跑的快配置</param>
    public static void UpdateRulesFromServer(NetMsg.RunFastConfig config)
    {
        if (config == null)
        {
            Debug.LogWarning("[PDKConstants] 服务器配置为空，使用默认规则");
            return;
        }

        // 更新出牌规则
        chupai = config.WithMin;

        // 更新先出规则
        xianchuRule = config.FirstRun;

        // 更新玩法规则
        wanfa = config.Play;

        // 更新复选框规则
        if (config.Check != null && config.Check.Count > 0)
        {
            CheckboxRules = new int[config.Check.Count];
            for (int i = 0; i < config.Check.Count; i++)
            {
                CheckboxRules[i] = config.Check[i];
            }
        }
        else
        {
            CheckboxRules = new int[] { };
        }

        Debug.Log($"[PDKConstants] 规则配置已更新 - 先出规则:{xianchuRule}, 玩法:{wanfa}, 复选框规则:{string.Join(",", CheckboxRules)}");
    }

    /// <summary>
    /// 检查是否包含某个复选框规则
    /// </summary>
    /// <param name="ruleNumber">规则编号（从1开始：1-17）</param>
    /// <returns>是否包含该规则</returns>
    public static bool HasCheckboxRule(int ruleNumber)
    {
        if (CheckboxRules == null) return false;

        for (int i = 0; i < CheckboxRules.Length; i++)
        {
            if (CheckboxRules[i] == ruleNumber)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 获取复选框规则名称（传入索引从0开始，对应规则编号1-17）
    /// </summary>
    /// <param name="ruleIndex">规则索引（从0开始：0-16）</param>
    /// <returns>规则名称，如果索引无效返回空字符串</returns>
    public static string GetCheckboxRuleName(int ruleIndex)
    {
        // 规则索引从0开始，对应服务器规则编号1-17
        int ruleNumber = ruleIndex + 1;

        switch (ruleNumber)
        {
            case 1: return "可四带二";
            case 2: return "可四带三";
            case 3: return "炸弹不可拆";
            case 4: return "AAA为炸弹";
            case 5: return "红桃10扎鸟";
            case 6: return "炸弹不翻倍";
            case 7: return "炸弹被压无分";
            case 8: return "三带任意出";
            case 9: return "最后三张可少带完";
            case 10: return "抢庄";
            case 11: return "可反的";
            case 12: return "快速过牌";
            case 13: return "切牌";
            case 14: return "显示剩牌";
            case 15: return "记牌器道具";
            case 16: return "加倍";
            case 17: return "提前亮牌";
            default: return "";
        }
    }

    #endregion
}
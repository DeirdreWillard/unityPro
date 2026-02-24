using System.Collections;
using System.Collections.Generic;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using GameFramework;
using System;
using UnityGameFramework.Runtime;
using System.Linq;
using System.Data;
using Google.Protobuf.Collections;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class CreatMjPopup : UIFormBase
{
    #region 常量定义
    private const string PARAM_SET_FLOOR = "setFloor";
    private const string PARAM_FLOOR_DATA = "floorData";
    private const string MODE_CREATE_ROOM = "CreateRoom";
    private const string MODE_EDIT_ROOM = "EditRoom";
    private const string MODE_CREATE_FLOOR = "CreateFloor";

    /// <summary>
    /// 麻将游戏类型常量定义
    /// </summary>
    public static class MjGameType
    {
        public const string XIANTAO_HUANGHUANG = "仙桃晃晃";
        public const string PAO_DE_KUAI = "跑的快";
        public const string DAZHONG_MAJIANG = "大众麻将";
        public const string KA_WU_XING = "卡五星";
        public const string XUE_LIU_CHENG_HE = "血流成河";

        /// <summary>
        /// 检查是否为已开放的游戏类型
        /// </summary>
        public static bool IsAvailableType(string gameType)
        {
            return gameType == XIANTAO_HUANGHUANG || gameType == PAO_DE_KUAI;
        }

        /// <summary>
        /// 根据游戏类型获取MethodType
        /// </summary>
        public static MethodType GetMethodType(string gameType)
        {
            return gameType == PAO_DE_KUAI ? MethodType.RunFast : MethodType.MjSimple;
        }
    }
    #endregion
    private MJConfigSetter configSetter;

    #region 生命周期方法

    /// <summary>
    /// Awake 在对象实例化时立即调用，早于 OnOpen
    /// 在这里初始化 configSetter，确保 UI 预制体实例化时的 Toggle 回调能够使用它
    /// </summary>
    private void Awake()
    {
        // 提前初始化 configSetter，防止 ToggleGroup.OnEnable 触发的回调找不到它
        if (configSetter == null)
        {
            configSetter = new MJConfigSetter(this);
        }
    }

    #endregion

    #region 私有字段
    private readonly MJConfigManager mJConfigManager = MJConfigManager.Instance;

    // 当前操作模式
    private enum OperationMode
    {
        CreateRoom,     // 创建房间
        CreateFloor,    // 创建楼层
        EditRoom        // 编辑房间
    }

    private string chooseMjType = "";
    private OperationMode currentMode = OperationMode.CreateRoom;
    private Msg_Floor currentFloorData = null; // 当前楼层数据，用于编辑模式

    /// <summary>
    /// 是否正在初始化 UI，用于防止 Toggle 回调触发逻辑
    /// </summary>
    public bool IsInitializing { get; set; } = false;
    #endregion

    /// <summary>
    /// 获取正确的ClubId - 根据上级关系决定使用哪个ClubId
    /// 优先级：祖父级 > 父级 > 当前级
    /// </summary>
    /// <returns>正确的ClubId</returns>
    private long GetCorrectClubId()
    {
        var leagueInfo = GlobalManager.GetInstance().LeagueInfo;

        // 如果有祖父级，返回祖父级ID
        if (leagueInfo.Father != 0 && leagueInfo.FatherInfo.Father != 0)
        {
            return leagueInfo.FatherInfo.Father;
        }

        // 如果有父级，返回父级ID
        if (leagueInfo.Father != 0)
        {
            return leagueInfo.Father;
        }

        // 否则返回当前联盟ID
        return leagueInfo.LeagueId;
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        IsInitializing = true;
        try
        {
            // 初始化操作模式和UI
            CheckAndInitializeFromFloorData(userData);
            SetOperationMode();

            for (int i = 0; i < varGameRule.transform.childCount; i++)
            {
                Transform child = varGameRule.transform.GetChild(i);
                child.gameObject.SetActive(false);
                // 预制体全部应用：初始化所有面板的钻石显示
                InitializeDiamondDisplay(child.name);
            }

            // 确保 configSetter 已初始化（通常在 Awake 中已经初始化）
            if (configSetter == null)
            {
                configSetter = new MJConfigSetter(this);
                GF.LogWarning("OnOpen: configSetter 在 Awake 中未初始化，现在补充初始化");
            }

            // 根据操作模式决定是否过滤Toggle
            if (currentMode == OperationMode.EditRoom)
            {
                // 编辑模式：先初始化以获取游戏类型，然后只注册当前游戏类型的Toggle
                VarByteArray varByteArray = Params.Get<VarByteArray>(PARAM_FLOOR_DATA);
                currentFloorData = Msg_Floor.Parser.ParseFrom(varByteArray);

                // 获取当前游戏类型名称
                string currentGameType = GetGameTypeName(currentFloorData.MethodType, currentFloorData);

                // 只注册当前游戏类型的Toggle
                RegisterToggleListeners(varContent.GetComponent<ToggleGroup>(), toggleNames, varMjTypeItem, varGameRule, OnMjTypeChanged, currentGameType);

                // 设置配置
                SetMahjongConfig(currentFloorData);
            }
            else
            {
                // 创建模式：注册所有Toggle
                RegisterToggleListeners(varContent.GetComponent<ToggleGroup>(), toggleNames, varMjTypeItem, varGameRule, OnMjTypeChanged);
            }
        }
        finally
        {
            IsInitializing = false;
        }
    }

    /// <summary>
    /// 根据MethodType和楼层数据获取游戏类型名称
    /// </summary>
    private string GetGameTypeName(MethodType methodType, Msg_Floor floorData)
    {
        if (methodType == MethodType.RunFast)
        {
            return MjGameType.PAO_DE_KUAI;
        }
        else if (methodType == MethodType.MjSimple)
        {
            // 使用DeskName来判断具体麻将类型
            if (!string.IsNullOrEmpty(floorData.Config?.DeskName))
            {
                return floorData.Config.DeskName;
            }
            // 如果DeskName为空，尝试从MjConfig判断
            if (floorData.MjConfig?.Xthh != null)
            {
                return MjGameType.XIANTAO_HUANGHUANG;
            }
        }
        return MjGameType.DAZHONG_MAJIANG; // 默认
    }

    /// <summary>
    /// 初始化默认模式（创建房间/楼层）
    /// </summary>
    private void InitializeDefaultMode()
    {
        //默认选择第一个
        var firstToggle = varContent.GetComponent<ToggleGroup>().GetComponentsInChildren<Toggle>().FirstOrDefault();
        if (firstToggle != null)
        {
            if (firstToggle.isOn)
            {
                // 如果已经是选中状态，手动触发一次逻辑
                OnMjTypeChanged(firstToggle.name);
            }
            else
            {
                // 设置为选中状态，这将通过事件监听器触发 OnMjTypeChanged
                firstToggle.isOn = true;
            }
        }
    }
    /// <summary>
    /// 麻将类型改变时的回调
    /// </summary>
    /// <param name="mjType">选中的麻将类型</param>
    private void OnMjTypeChanged(string mjType)
    {
        GF.LogInfo_wl("第一次");
        chooseMjType = mjType;

        // 确保切换麻将类型时，只显示对应的规则面板
        for (int i = 0; i < varGameRule.transform.childCount; i++)
        {
            Transform child = varGameRule.transform.GetChild(i);
            bool shouldActivate = child.name == mjType;
            child.gameObject.SetActive(shouldActivate);

            if (shouldActivate)
            {
                // 如果不是编辑模式且不在初始化过程中，尝试加载记忆配置或默认配置（默认加载最低底分0.5）
                // IsInitializing 标志用于防止在设置楼层数据时被记忆配置覆盖
                if (currentMode != OperationMode.EditRoom && !IsInitializing)
                {
                    LoadMemoryConfigs(mjType, -1);
                }

                // 初始化钻石显示
                InitializeDiamondDisplay(mjType);

                // 切换面板后应用规则限制
                var config = GetCurrentConfig();
                if (config is MJConfigManager.XTHHConfigData xthhConfig)
                {
                    ApplyXTHHPeopleNumRules(xthhConfig.PeopleNum, xthhConfig);
                }
                else if (config is MJConfigManager.PDKConfigData pdkConfig)
                {
                    ApplyPDKPeopleNumRules(pdkConfig.PeopleNum, pdkConfig);
                }
            }
        }
    }

    /// <summary>
    /// 获取当前选中的麻将类型，如果没有选择则返回默认值
    /// </summary>
    /// <returns>当前选中的麻将类型</returns>
    public string GetCurrentMjType()
    {
        return string.IsNullOrEmpty(chooseMjType) ? toggleNames[0] : chooseMjType;
    }

    /// <summary>
    /// 初始化钻石显示 - 在玩法下的所有局数选项中显示对应局数的钻石消耗
    /// </summary>
    /// <param name="mjType">麻将类型</param>
    private void InitializeDiamondDisplay(string mjType)
    {
        // 构建路径: 玩法/rule/Viewport/Content/jushu
        Transform contentTransform = varGameRule.transform.Find($"{mjType}/rule/Viewport/Content");
        if (contentTransform == null)
        {
            GF.LogWarning($"未找到{mjType}的Content路径");
            return;
        }

        Transform jushuTransform = contentTransform.Find("jushu");
        Transform jushuTransform1 = contentTransform.Find("jushu1");
        
        // 遍历jushu下除了Text子物体外的所有子物体
        if (jushuTransform != null)
        {
            for (int i = 0; i < jushuTransform.childCount; i++)
            {
                Transform child = jushuTransform.GetChild(i);
                // 跳过名为"Text"的子物体
                if (child.name == "Text")
                {
                    continue; 
                }
                // 根据局数获取对应的钻石消耗
                long diamondCost = GetDiamondCostByRounds(mjType, child.name);
                
                // 查找 ZSbg/num 路径下的Text组件
                Transform numTransform = child.Find("ZSbg/num");
                Text numText = numTransform?.GetComponent<Text>();
                if (numText != null)
                {
                    numText.text = $"X{diamondCost}";
                    GF.LogInfo_wl($"初始化{mjType}玩法下局数{child.name}的钻石显示: {diamondCost}");
                }
            }
        }
        
        if (jushuTransform1 != null)
        {
            for (int i = 0; i < jushuTransform1.childCount; i++)
            {
                Transform child = jushuTransform1.GetChild(i);
                // 跳过名为"Text"的子物体
                if (child.name == "Text")
                {
                    continue;
                }
                
                // 根据局数获取对应的钻石消耗
                long diamondCost = GetDiamondCostByRounds(mjType, child.name);
                
                // 查找 ZSbg/num 路径下的Text组件
                Transform numTransform = child.Find("ZSbg/num");
                Text numText = numTransform?.GetComponent<Text>();
                if (numText != null)
                {
                    numText.text = $"X{diamondCost}";
                    GF.LogInfo_wl($"初始化{mjType}玩法下局数{child.name}的钻石显示: {diamondCost}");
                }
            }
        }
    }
    
    /// <summary>
    /// 根据麻将类型和局数获取对应的钻石消耗
    /// </summary>
    /// <param name="mjType">麻将类型</param>
    /// <param name="roundsName">局数名称(如"4"、"8"、"16"等)</param>
    /// <returns>对应的钻石消耗</returns>
    private long GetDiamondCostByRounds(string mjType, string roundsName)
    {
        // 尝试解析局数
        if (!int.TryParse(roundsName, out int rounds))
        {
            GF.LogWarning($"无法解析局数: {roundsName}");
            return 0;
        }
        
        // 获取玩法类型
        MethodType methodType = MjGameType.GetMethodType(mjType);
        
        // 根据当前模式和联盟信息判断房间类型
        DeskType deskType;
        var leagueInfo = GlobalManager.GetInstance().LeagueInfo;
        
        if (currentMode == OperationMode.CreateRoom)
        {
            // 创建个人房间
            deskType = DeskType.Simple;
        }
        else if (leagueInfo != null)
        {
            // 创建楼层或编辑房间,根据联盟类型确定
            deskType = GetDeskType(leagueInfo.Type);
        }
        else
        {
            // 默认为个人房
            deskType = DeskType.Simple;
        }
        
        // 调用GlobalManager的方法获取钻石消耗
        int diamondCost = GlobalManager.GetInstance().GetCreateDeskCostByRound(methodType, rounds, deskType);
        
        GF.LogInfo_wl($"查询{mjType}局数{rounds}房间类型{deskType}的钻石消耗: {diamondCost}");
        return diamondCost;
    }

    /// <summary>
    /// 加载指定游戏类型的配置（默认加载最低底分0.5配置）
    /// </summary>
    /// <param name="mjType">麻将游戏类型</param>
    /// <param name="baseCoinIndex">底分索引，-1表示加载默认（最低）档位</param>
    private void LoadMemoryConfigs(string mjType, int baseCoinIndex = -1)
    {
        if (MJHomeProcedure.s_DefaultCreateDeskConfig == null ||
            MJHomeProcedure.s_DefaultCreateDeskConfig.CreateDesk == null)
        {
            GF.LogWarning($"无法加载{mjType}配置：服务器配置为空");
            return;
        }

        // 获取目标底分值
        float targetBaseCoin = baseCoinIndex >= 0
            ? GetBaseCoinByIndex(baseCoinIndex)
            : GetLowestBaseCoinForType(mjType); // 默认加载最低底分（通常为0.5）

        // 从服务器配置中查找匹配的配置
        Msg_CreateDeskRq matchedConfig = FindConfigByTypeAndBaseCoin(mjType, targetBaseCoin);

        if (matchedConfig != null)
        {
            GF.LogInfo($"加载{mjType}的服务器配置，底分: {targetBaseCoin}");
            ApplyConfigToUI(mjType, matchedConfig);
        }
        else
        {
            GF.LogWarning($"未找到{mjType}底分{targetBaseCoin}的配置");
        }
    }

    /// <summary>
    /// 根据游戏类型和底分查找匹配的配置
    /// </summary>
    private Msg_CreateDeskRq FindConfigByTypeAndBaseCoin(string mjType, float baseCoin)
    {
        MethodType methodType = MjGameType.GetMethodType(mjType);

        foreach (var item in MJHomeProcedure.s_DefaultCreateDeskConfig.CreateDesk)
        {
            bool isTypeMatch = false;

            if (mjType == MjGameType.PAO_DE_KUAI)
            {
                isTypeMatch = item.MethodType == MethodType.RunFast;
            }
            else
            {
                isTypeMatch = item.MethodType == MethodType.MjSimple &&
                             item.Config != null &&
                             item.Config.DeskName == mjType;
            }

            // 匹配游戏类型和底分
            if (isTypeMatch && item.Config != null &&
                Mathf.Approximately(item.Config.BaseCoin, baseCoin))
            {
                return item;
            }
        }

        return null;
    }

    /// <summary>
    /// 获取指定游戏类型的最低底分值
    /// </summary>
    private float GetLowestBaseCoinForType(string mjType)
    {
        if (MJHomeProcedure.s_DefaultCreateDeskConfig == null ||
            MJHomeProcedure.s_DefaultCreateDeskConfig.CreateDesk == null)
        {
            return 0.5f; // 默认值
        }

        MethodType methodType = MjGameType.GetMethodType(mjType);
        float lowestBaseCoin = float.MaxValue;
        bool found = false;

        foreach (var item in MJHomeProcedure.s_DefaultCreateDeskConfig.CreateDesk)
        {
            bool isMatch = false;

            if (mjType == MjGameType.PAO_DE_KUAI)
            {
                isMatch = item.MethodType == MethodType.RunFast;
            }
            else
            {
                isMatch = item.MethodType == MethodType.MjSimple &&
                         item.Config != null &&
                         item.Config.DeskName == mjType;
            }

            if (isMatch && item.Config != null)
            {
                found = true;
                if (item.Config.BaseCoin < lowestBaseCoin)
                {
                    lowestBaseCoin = item.Config.BaseCoin;
                }
            }
        }

        return found ? lowestBaseCoin : 0.5f;
    }

    /// <summary>
    /// 根据索引获取底分值（需要根据实际UI配置调整）
    /// </summary>
    private float GetBaseCoinByIndex(int index)
    {
        // 这里假设底分档位为: 0.5, 1, 2, 5, 10
        // 如果实际档位不同，需要根据UI配置调整

        if (index >= 0 && index < ConfigArrays.BaseCoinList.Length)
        {
            return ConfigArrays.BaseCoinList[index];
        }

        return 0.5f; // 默认返回最低档
    }

    /// <summary>
    /// 根据当前游戏类型和底分索引加载配置（供外部调用）
    /// </summary>
    /// <param name="baseCoinIndex">底分索引，-1表示加载默认（最低）档位</param>
    public void LoadGameTypeConfig(int baseCoinIndex = -1)
    {
        string currentMjType = GetCurrentMjType();
        if (!string.IsNullOrEmpty(currentMjType))
        {
            LoadMemoryConfigs(currentMjType, baseCoinIndex);
        }
    }

    private void ApplyConfigToUI(string mjType, Msg_CreateDeskRq config)
    {
        if (config == null) return;

        IsInitializing = true;
        try
        {
            // 创建一个临时的 Msg_Floor 对象来复用现有的 Set...ConfigFromFloorData 方法
            Msg_Floor tempFloor = new Msg_Floor();
            tempFloor.Config = config.Config;
            tempFloor.MjConfig = config.MjConfig;
            tempFloor.RunFastConfig = config.RunFastConfig;

            if (mjType == MjGameType.PAO_DE_KUAI)
            {
                SetRunFastConfigFromFloorData(tempFloor);
            }
            else if (mjType == MjGameType.XIANTAO_HUANGHUANG)
            {
                SetHuanghuangConfigFromFloorData(tempFloor);
            }
            else if (mjType == MjGameType.KA_WU_XING)
            {
                SetKWXConfigFromFloorData(tempFloor);
            }
            else if (mjType == MjGameType.XUE_LIU_CHENG_HE)
            {
                SetXLCHConfigFromFloorData(tempFloor);
            }

        }
        finally
        {
            IsInitializing = false;
        }
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "ChuangJianFangJian":
                HandleCreateRoom();
                break;
            case "XingJianLouCeng":
                HandleCreateFloor();
                break;
            case "XiuGai":
                HandleEditRoom();
                break;
            case "rules":
                varRulesText.gameObject.SetActive(true);
                //根据当前游戏类型更改规则text
                varTextItem.text = "";
                varTextItem.text = GetRulesText(GetCurrentMjType());
                // 延迟重置滚动条到顶部（等待布局系统更新）
                StartCoroutine(ResetScrollToTopDelayed());
                break;
            case "Close":
                varRulesText.gameObject.SetActive(false);
                break;
            case "退出":
                GF.UI.Close(this.UIForm);
                break;
        }
    }
    private string GetRulesText(string mjType)
    {
        switch (mjType)
        {
            case MjGameType.XIANTAO_HUANGHUANG:
                return @"基础规则
1.筒、条、万共108张牌。
2.只能碰、杠，不能吃牌，任意将。
3.第一局由开房玩家坐庄，之后谁胡谁坐庄。
4.如果出现一炮多响，则下局由放统玩家坐庄。
5.发牌结束后翻一张，取大一个点数的牌为赖子。
6.过庄：需要过庄。
7.不能胡7对,没有大胡, 将一色、清一色、碰碰胡等成牌型了可以胡,只是胡下来不播将色、清一色、碰碰胡,算普通的自摸或捉铳。
计分规则
1.点杠:1分。
2.蓄杠:1分。
3.暗杠:2分。
4.放统:2分。
5.软摸:2分。
6.黑摸:4分。
7.抢杠:3分。
8.赖子:x2。
9.四个癞子:手中曾有4个癞子，胡牌时X2。
可选项说明
1、杠开加分：杠赖子补牌时自摸，加2分。
2、一赖到底：胡牌时手中只允许有一个赖子，捉统时手上不能有赖子；如果有玩家杠出赖子，则
所有玩家只能自摸胡。（赖子还原捉统也不能胡）
3、赖晃：胡牌时允许有多个赖子，捉统时手中不能有赖子；如果有玩家杠出赖子，则所有玩家只
能自摸胡。（赖子还原捉统也不能胡）
";
            case "大众麻将":
                return @"基础规则
1.使用标准麻将牌136张（筒、条、万各36张，风牌16张，箭牌12张）。
2.标准4人局。
3.可以吃、碰、杠，按标准麻将规则进行。
4.第一局由系统随机选庄，之后谁胡牌谁做庄。
5.如果流局（荒庄），庄家继续做庄。
6.标准胡牌：4个面子+1个对子。
计分规则
1.自摸：庄家6分，闲家各出2分；闲家自摸，庄家出4分，其他闲家各出1分。
2.点炮：点炮者支付全部分数。
3.杠牌得分：明杠1分，暗杠2分，点杠1分。
";
            case MjGameType.KA_WU_XING:
                return @"玩法规则:
1.
赔庄：荒庄后，如果有玩家没听牌，则需要赔付听牌玩家可胡的最大番数，如果所有玩家都听牌了，则
亮牌的玩家需赔付听牌且未亮牌玩家的最大胡型番数
2.
数坎：胡牌时，手上有三张一样的牌，单独成一个刻子，就算一坎。杠也算，碰胡算。
3.
小于
4.
荒庄不慌杠
5.
对亮翻番：勾选此玩法后，输赢两家都亮倒的情况下，得分
6.
最后一张牌，不可杠。打出的牌也不能杠。
牌型
1.
屁胡：
2.
碰碰胡：
3.
清一色：
4.
七对：
5.
豪华七对：
6.
卡五星：
7.
明四归：
8.
暗四归：
9.
手抓一：
10.
小三元：
11.
大三元：
12.
叠加牌型为牌型互乘。如：清一色卡五星，则清一色
"
;
            case MjGameType.XUE_LIU_CHENG_HE:
                return @"【基本规则】
1.用牌：使用万条筒共计108张牌
2牌权：不可吃，可碰，可胡牌
3碰牌：可以碰任意家牌
4.游戏人数：4人，3人，2人
5.定庄：首局随机庄，次局最后出牌玩家当庄。
6定缺：开局玩家可以任意选择一门牌，胡牌必须缺一门才能胡。
7换三张：游戏开始玩家选择3张同花色牌与其他人互换。
8.查花猪：无人胡牌的时候先查花猪（有3个花色的玩家为花猪）。
【胡牌分】
1.牌型分：平胡1分，碰碰胡3分，清一色6分，清一色碰碰胡9分
2加分项：门清1分，明杠1分，暗杠2分
【计分规则】
结算=牌型分+加分项+漂分
                "
;
            case MjGameType.PAO_DE_KUAI:
                return @"【玩法简介】
1.用牌：共48张牌，一副牌去掉大小王，红桃2，梅花2，方块2，黑桃A。
2人数：3人2人都可玩。
3胜负：先出完牌玩家获胜。
4.出牌顺序：第一局拥有黑桃3玩家先出，其他则上局获胜玩家先出牌。
【名词解释】
1报单：玩家剩余1张牌时，提示“报单”
2包赔：当下家报单时，再出单牌，必须打出手中最大得单张，如果没有打出最大单张下家出完
牌，则包赔所有结算分。
3有大必压：有打得过上家得牌，必须打出。
4抓鸟：开局随机翻出1张牌，拥有此牌玩家输赢都翻倍。（炸弹不翻倍）
【牌型介绍】
1.单张：一张牌单独的牌。
2对子：两张相同的点数的牌。
3.连对：相连的对子，两对或两对以上以上的牌，如5566或556677.4.三带二：三张点数相同
的牌带2张其他任意的牌，如55589，除非是最后一手牌，否则必须带两张牌。
5.四带三：四张点数相同的牌带3张其他任意的牌，如5555893。
6.飞机：相连的3带2或4带3，称为飞机，如555666789K或55556666789K23。
7.顺子：5张或5张以上相连的单张，最多12张，2不能出现在顺子中。
8.炸弹：4张相同的牌为炸弹，炸弹可以压任意牌型，炸弹的大小按牌点数大小排序。
9.三个A:三个A算炸弹，并且是最大的炸弹。
【积分计算】
1积分：其他玩家未出完牌，剩余n张扣n*底，报单玩家不输分。
2春天：胜负已分时，任意一家未出一张牌，则称为春天，未出牌玩家失分双倍。
3.炸弹分：炸弹10倍底分。受春天和反春影响，牌局结束结算。
4.包赔：包所有输分家分数，即一人支付自己+其他输家分数。
5.扎鸟：拥有红桃10的玩家扎鸟，输赢均*2。炸弹分不受扎鸟影响。

                ";
            default:
                return "";
        }
    }

    /// <summary>
    /// 延迟重置滚动条到顶部（协程版本，等待布局更新）
    /// </summary>
    private IEnumerator ResetScrollToTopDelayed()
    {
        // 等待一帧，让文本内容和布局系统完成更新
        yield return null;

        // 再次等待，确保 Content 的尺寸已经根据文本内容重新计算
        yield return new WaitForEndOfFrame();

        // 执行重置
        ResetScrollToTop();
    }

    /// <summary>
    /// 重置滚动条到顶部
    /// </summary>
    private void ResetScrollToTop()
    {
        // 查找规则弹窗中的ScrollRect组件
        ScrollRect scrollRect = varRulesText.GetComponentInChildren<ScrollRect>();
        if (scrollRect != null)
        {
            // 强制刷新布局
            Canvas.ForceUpdateCanvases();

            // 重建布局
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

            // 将垂直滚动位置设置为1（顶部）
            scrollRect.verticalNormalizedPosition = 1f;

            // 确保滚动速度为0（停止任何惯性滚动）
            scrollRect.velocity = Vector2.zero;
        }
        else
        {
            GF.LogWarning("未找到ScrollRect组件，无法重置滚动位置");
        }
    }

    #region 按钮处理方法
    /// <summary>
    /// 处理创建房间
    /// </summary>
    private void HandleCreateRoom()
    {
        CreatMJRoom();
    }

    /// <summary>
    /// 请求创建楼层
    /// </summary>
    private void HandleCreateFloor()
    {
        Msg_CreateMjFloorRq req = MessagePool.Instance.Fetch<Msg_CreateMjFloorRq>();
        req.ClubId = GetCorrectClubId();
        req.Config = GetDesk_BaseConfig();

        if (GetCurrentMjType() == MjGameType.PAO_DE_KUAI)
        {
            req.MethodType = MethodType.RunFast;
            req.RunFastConfig = mJConfigManager.GenerateRunFastConfig();
            GF.LogInfo_wl($"创建楼层-发送{MjGameType.PAO_DE_KUAI}配置: " + req.RunFastConfig.ToString());
            // Debug模式下强制修改ApplyDismissTime为1分钟
            if (AppSettings.Instance.DebugMode)
            {
                req.RunFastConfig.ApplyDismissTime = 1;
            }
        }
        else
        {
            req.MethodType = MethodType.MjSimple;
            req.MjConfig = mJConfigManager.GenerateMJConfig(GetCurrentMjType());
            GF.LogInfo_wl($"创建楼层-发送{GetCurrentMjType()}配置: " + req.MjConfig.ToString());

            // Debug模式下强制修改所有麻将类型的ApplyDismissTime为1分钟
            if (AppSettings.Instance.DebugMode)
            {
                if (req.MjConfig.Xthh != null)
                {
                    req.MjConfig.Xthh.ApplyDismissTime = 1;
                }
            }
        }
        //如果当前为仙桃晃晃和跑的快则请求 否则弹窗提示敬请期待
        if (!MjGameType.IsAvailableType(GetCurrentMjType()))
        {
            GF.UI.ShowToast("该游戏类型暂未开放，敬请期待！");
            return;
        }
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(
            HotfixNetworkComponent.AccountClientName,
            MessageID.Msg_CreateMjFloorRq,
            req);

        GF.UI.Close(this.UIForm);
    }

    /// <summary>
    /// 处理编辑房间
    /// </summary>
    private void HandleEditRoom()
    {
        if (currentFloorData == null)
        {
            GF.LogError("编辑模式下缺少楼层数据");
            GF.UI.ShowToast("编辑失败:缺少楼层数据");
            return;
        }

        Msg_SetMjFloorRq req = MessagePool.Instance.Fetch<Msg_SetMjFloorRq>();
        req.ClubId = GetCorrectClubId();
        req.Floor = currentFloorData.Floor;
        req.Config = GetDesk_BaseConfig();

        // 根据当前选择的游戏类型设置配置
        if (GetCurrentMjType() == MjGameType.PAO_DE_KUAI)
        {
            req.MethodType = MethodType.RunFast;
            req.RunFastConfig = mJConfigManager.GenerateRunFastConfig();
            GF.LogInfo_wl($"编辑楼层-发送{MjGameType.PAO_DE_KUAI}配置: " + req.RunFastConfig.ToString());
        }
        else
        {
            req.MethodType = MethodType.MjSimple;
            req.MjConfig = mJConfigManager.GenerateMJConfig(GetCurrentMjType());
            GF.LogInfo_wl($"编辑楼层-发送{GetCurrentMjType()}配置: " + req.MjConfig.ToString());
        }

        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(
            HotfixNetworkComponent.AccountClientName,
            MessageID.Msg_SetMjFloorRq,
            req);

        GF.UI.Close(this.UIForm);
        GF.UI.ShowToast("成功修改游戏配置");
    }
    /// <summary>
    /// 获取当前配置实例
    /// </summary>
    /// <returns>当前麻将类型的配置实例</returns>
    public MJConfigManager.BaseMJConfig GetCurrentConfig()
    {
        return mJConfigManager.GetConfig(GetCurrentMjType());
    }

    /// <summary>
    /// 获取桌子基础配置
    /// </summary>
    /// <returns>桌子基础配置对象</returns>
    public Desk_BaseConfig GetDesk_BaseConfig()
    {
        var currentConfig = GetCurrentConfig();
        var leagueInfo = GlobalManager.GetInstance().LeagueInfo;

        var deskConfig = new Desk_BaseConfig
        {
            DeskName = GetCurrentMjType(),
            BaseCoin = currentConfig.BaseCoin,
            PlayerTime = currentConfig.PlayNum,
            PlayerNum = currentConfig.PeopleNum,
            AutoStart = true,
            AutoStartNum = currentConfig.PeopleNum,
            IpLimit = currentConfig.IpLimit,
            GpsLimit = currentConfig.GpsLimit,
            ForbidVoice = currentConfig.ForbidVoice,
            Forbid = currentConfig.Forbid,
            OpenRate = currentConfig.openRate
        };

        if (leagueInfo != null)
        {
            deskConfig.Creator = leagueInfo.Creator;
            deskConfig.DeskType = GetDeskType(leagueInfo.Type);
            deskConfig.ClubId = leagueInfo.LeagueId;
        }
        else
        {
            deskConfig.DeskType = DeskType.Simple;
            deskConfig.ClubId = 0;
            deskConfig.Creator = Util.GetMyselfInfo()?.PlayerId ?? 0;
        }

        // 如果开启抽水,设置抽水相关配置
        if (currentConfig.openRate)
        {
            deskConfig.Rate = new NetMsg.PairInt
            {
                Key = currentConfig.Rate,
                Val = currentConfig.MinRate
            };
            deskConfig.RateType = currentConfig.RateType;
        }

        return deskConfig;
    }

    /// <summary>
    /// 根据联盟类型获取桌子类型
    /// </summary>
    /// <param name="leagueType">联盟类型</param>
    /// <returns>桌子类型</returns>
    private DeskType GetDeskType(int leagueType)
    {
        return leagueType switch
        {
            0 => DeskType.Guild,
            1 => DeskType.League,
            _ => DeskType.Super
        };
    }
    #endregion
    #region 初始化方法实现
    /// <summary>
    /// 设置操作模式
    /// </summary>
    private void SetOperationMode()
    {
        switch (currentMode)
        {
            case OperationMode.CreateRoom:
                SetCreateRoomMode();
                break;
            case OperationMode.CreateFloor:
                SetCreateFloorMode();
                break;
            case OperationMode.EditRoom:
                SetEditRoomMode();
                break;
        }
    }
    /// <summary>
    /// 设置创建房间模式
    /// </summary>
    private void SetCreateRoomMode()
    {
        varCreatMjRoom.SetActive(true);
        varXingjianlouceng.SetActive(false);
        varXiugaifangjian.SetActive(false);
        //隐藏所有玩法抽水选项IsOpenRate
        SetIsOpenRateVisibility(false);

    }

    /// <summary>
    /// 设置创建楼层模式
    /// </summary>
    private void SetCreateFloorMode()
    {
        varCreatMjRoom.SetActive(false);
        varXingjianlouceng.SetActive(true);
        varXiugaifangjian.SetActive(false);
        //打开所有玩法抽水选项IsOpenRate
        SetIsOpenRateVisibility(true);
    }

    /// <summary>
    /// 设置所有玩法下IsOpenRate的显示状态
    /// </summary>
    private void SetIsOpenRateVisibility(bool isVisible)
    {

        // 遍历所有游戏类型
        foreach (string mjType in toggleNames)
        {
            Transform contentTrans = varGameRule.transform.Find($"{mjType}/rule/Viewport/Content");

            if (contentTrans != null)
            {
                if (!isVisible)
                {
                    GF.LogInfo_wl("隐藏抽水选项");
                    Transform SetRateType = contentTrans.Find("SetRateType");
                    SetRateType.gameObject.SetActive(false);
                    Transform SetRate = contentTrans.Find("SetRate");
                    SetRate.gameObject.SetActive(false);
                    Transform SetRateValue = contentTrans.Find("SetRateValue");
                    SetRateValue.gameObject.SetActive(false);
                }
                // 只隐藏/显示IsOpenRate（是否开启抽水的Toggle选项）
                // SetRateType、SetRate、SetRateValue等组件会根据Toggle状态自动控制显示
                Transform isOpenRateGroupTrans = contentTrans.Find("IsOpenRate");
                if (isOpenRateGroupTrans != null)
                {
                    isOpenRateGroupTrans.gameObject.SetActive(isVisible);
                }

            }
        }
    }

    /// <summary>
    /// 设置编辑房间模式
    /// </summary>
    private void SetEditRoomMode()
    {
        varCreatMjRoom.SetActive(false);
        varXingjianlouceng.SetActive(false);
        varXiugaifangjian.SetActive(true);
    }

    #endregion



    #region 人数规则限制方法

    /// <summary>
    /// 应用仙桃晃晃人数变化时的规则限制
    /// 规则: 1.开锁牌才能开解锁翻倍 2.4人才能2赖 3.3人和2人只能3赖和4赖锁牌
    /// </summary>
    public void ApplyXTHHPeopleNumRules(int peopleNum, MJConfigManager.XTHHConfigData config)
    {
        Transform contentTransform = varGameRule.transform.Find("仙桃晃晃/rule/Viewport/Content");
        if (contentTransform == null)
        {
            GF.LogWarning("未找到仙桃晃晃配置面板");
            return;
        }

        GameObject content = contentTransform.gameObject;
        if (content == null)
        {
            GF.LogWarning("仙桃晃晃配置面板GameObject为空");
            return;
        }

        // 规则1: 如果锁牌=1(不锁牌),禁用解锁翻倍
        Transform openDoubleToggle = content.transform.Find("zhuochong/3");
        if (openDoubleToggle != null)
        {
            Toggle toggle = openDoubleToggle.GetComponent<Toggle>();
            if (toggle != null)
            {
                bool shouldEnable = (config.LockCard != 1);
                // 如果要禁用,先关闭并同步到配置
                if (!shouldEnable)
                {
                    config.OpenDouble = 0;
                }
                SetToggleStateAndInteractable(toggle, shouldEnable, shouldEnable && toggle.isOn);
            }
        }
        // 规则2&3: 根据人数限制锁牌选项
        Transform lockCardGroup = content.transform.Find("suopai");
        if (lockCardGroup != null)
        {
            Transform toggle2Lai = lockCardGroup.Find("2");
            if (toggle2Lai != null)
            {
                Toggle toggle = toggle2Lai.GetComponent<Toggle>();
                if (toggle != null)
                {
                    if (peopleNum == 4)
                    {
                        // 4人: 启用2赖选项(但不自动选中)
                        toggle.interactable = true;
                    }
                    else if (peopleNum == 3 || peopleNum == 2)
                    {
                        // 3人和2人: 禁用2赖选项,如果当前选中则切换到不锁牌
                        if (toggle.isOn && config.LockCard == 2)
                        {
                            // 找到"不锁牌"选项(suopai/1)并选中它
                            Transform toggle1 = lockCardGroup.Find("1");
                            if (toggle1 != null)
                            {
                                Toggle toggle1Component = toggle1.GetComponent<Toggle>();
                                if (toggle1Component != null)
                                {
                                    toggle1Component.isOn = true; // 这会触发SetLockCard(1)
                                }
                            }
                            else
                            {
                                // 如果找不到Toggle,直接设置配置
                                toggle.isOn = false;
                                config.LockCard = 1;
                            }
                        }
                        toggle.interactable = false;
                    }
                }
            }
        }
    }
    /// <summary>
    /// 应用跑的快人数变化时的规则限制
    /// 规则: 2人房才可以选择"可反的"(check 11)
    /// </summary>
    public void ApplyPDKPeopleNumRules(int peopleNum, MJConfigManager.PDKConfigData config)
    {
        Transform contentTransform = varGameRule.transform.Find("跑的快/rule/Viewport/Content");
        if (contentTransform == null)
        {
            return;
        }
        GameObject content = contentTransform.gameObject;
        if (content == null)
        {
            return;
        }
        // 查找"可反的"toggle (check 11)
        Transform fandeToggle = content.transform.Find("wanfa3/11");
        if (fandeToggle != null)
        {
            Toggle toggle = fandeToggle.GetComponent<Toggle>();
            if (toggle != null)
            {
                // 2人房启用,非2人房禁用
                bool shouldEnable = (peopleNum == 2);
                // 如果要禁用且当前选中,从配置中移除
                if (!shouldEnable && toggle.isOn && config.Check.Contains(11))
                {
                    config.Check.Remove(11);
                }
                SetToggleStateAndInteractable(toggle, shouldEnable, shouldEnable && toggle.isOn);
            }
        }
    }

    /// <summary>
    /// 启用或禁用指定Toggle
    /// </summary>
    private void EnableToggleByName(Transform parent, string toggleName, bool enable)
    {
        Transform toggleTransform = parent.Find(toggleName);
        if (toggleTransform != null)
        {
            Toggle toggle = toggleTransform.GetComponent<Toggle>();
            toggle.interactable = enable;
        }
    }

    /// <summary>
    /// 设置Toggle的选中状态和可交互状态
    /// 先设置选中状态,再设置可交互状态
    /// </summary>
    /// <param name="toggle">Toggle组件</param>
    /// <param name="interactable">是否可交互</param>
    /// <param name="isOn">是否选中</param>
    private void SetToggleStateAndInteractable(Toggle toggle, bool interactable, bool isOn)
    {
        if (toggle == null) return;

        // 先设置选中状态
        toggle.isOn = isOn;
        // 再设置可交互状态
        toggle.interactable = interactable;
    }

    /// <summary>
    /// 通过名称设置Toggle的可交互状态和选中状态
    /// </summary>
    /// <param name="parent">父Transform</param>
    /// <param name="toggleName">Toggle名称</param>
    /// <param name="interactable">是否可交互</param>
    /// <param name="isOn">是否选中</param>
    private void SetToggleInteractableByName(Transform parent, string toggleName, bool interactable, bool isOn)
    {
        Transform toggleTransform = parent.Find(toggleName);
        if (toggleTransform != null)
        {
            Toggle toggle = toggleTransform.GetComponent<Toggle>();
            if (toggle != null)
            {
                SetToggleStateAndInteractable(toggle, interactable, isOn);
            }
        }
    }

    /// <summary>
    /// 更新仙桃晃晃解锁翻倍Toggle的状态
    /// </summary>
    public void UpdateOpenDoubleToggle(bool isOn, bool interactable)
    {
        Transform contentTrans = varGameRule.transform.Find("仙桃晃晃/rule/Viewport/Content");
        if (contentTrans == null)
        {
            GF.LogWarning("未找到仙桃晃晃配置面板");
            return;
        }
        GameObject content = contentTrans.gameObject;

        Transform openDoubleToggle = content.transform.Find("zhuochong/3");
        if (openDoubleToggle != null)
        {
            Toggle toggle = openDoubleToggle.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.isOn = isOn;
                toggle.interactable = interactable;
            }
        }
    }

    #endregion

    #region xthh配置方法
    /// <summary>
    /// 从Toggle名称获取对应的复选框编号
    /// </summary>
    /// <param name="toggleName">Toggle的名称</param>
    /// <returns>复选框编号，如果无法解析则返回-1</returns>
    public int GetCheckNumberFromName(string toggleName)
    {
        // 1. 首先尝试直接解析数字
        if (int.TryParse(toggleName, out int directNumber))
        {
            return directNumber;
        }

        // 2. 尝试从带前缀的名称中提取数字（如"check_1", "option_2"）
        var parts = toggleName.Split('_');
        if (parts.Length >= 2 && int.TryParse(parts[parts.Length - 1], out int numberFromSuffix))
        {
            return numberFromSuffix;
        }
        // 3. 使用预定义的映射字典
        var checkboxNameMap = GetCheckboxNameMapping();
        if (checkboxNameMap.TryGetValue(toggleName, out int mappedNumber))
        {
            return mappedNumber;
        }
        return -1; // 无法解析
    }
    /// <summary>
    /// 获取复选框名称到编号的映射字典
    /// 可根据不同麻将玩法动态调整
    /// </summary>
    /// <returns>名称到编号的映射字典</returns>
    private Dictionary<string, int> GetCheckboxNameMapping()
    {
        return GetCurrentMjType() switch
        {
            MjGameType.XIANTAO_HUANGHUANG => new Dictionary<string, int>
            {
                {"杠不随飘", 1},
                {"4癞胡牌不加倍", 2},
                {"黑夹黑", 3}
            },
            MjGameType.DAZHONG_MAJIANG => new Dictionary<string, int>(),
            _ => new Dictionary<string, int>()
        };
    }

    /// <summary>
    /// 通用增加数值方法
    /// </summary>
    /// <param name="configKey">配置键名</param>
    public void AddValue(string configKey)
    {
        ProcessValueChange(configKey, 1);
    }

    /// <summary>
    /// </summary>
    /// <param name="configKey">配置键名</param>
    public void SubtractValue(string configKey)
    {
        ProcessValueChange(configKey, -1);
    }


    /// <summary>
    /// 智能查找文本组件
    /// </summary>
    /// <param name="button">按钮组件</param>
    /// <param name="configKey">配置键</param>
    /// <returns>文本组件</returns>
    public Text FindTextComponentSmart(Button button, string configKey)
    {
        Transform parent = button.transform.parent.parent;

        // 策略1: 查找同级的text组件
        Text text = parent?.Find("text")?.GetComponent<Text>();
        if (text != null) return text;

        // 策略2: 查找父级的父级中的text
        Transform grandParent = parent?.parent;
        text = grandParent?.Find("text")?.GetComponent<Text>();
        if (text != null) return text;

        // 策略3: 在兄弟节点中查找Text组件
        text = parent?.GetComponentInChildren<Text>();
        if (text != null) return text;

        return null;
    }

    /// <summary>
    /// 在配置数组中查找值的索引
    /// </summary>
    /// <param name="configArray">配置数组</param>
    /// <param name="value">要查找的值</param>
    /// <returns>索引，如果未找到返回-1</returns>
    public int FindValueIndexInArray(object[] configArray, object value)
    {
        for (int i = 0; i < configArray.Length; i++)
        {
            // 处理不同类型的比较
            if (AreValuesEqual(configArray[i], value))
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 比较两个值是否相等（处理类型转换）
    /// </summary>
    private bool AreValuesEqual(object arrayValue, object compareValue)
    {
        if (arrayValue == null && compareValue == null) return true;
        if (arrayValue == null || compareValue == null) return false;

        // 如果类型相同，直接比较
        if (arrayValue.GetType() == compareValue.GetType())
        {
            return arrayValue.Equals(compareValue);
        }

        // 处理数值类型转换
        if (IsNumericType(arrayValue) && IsNumericType(compareValue))
        {
            return Convert.ToDouble(arrayValue) == Convert.ToDouble(compareValue);
        }

        // 字符串比较
        return arrayValue.ToString() == compareValue.ToString();
    }

    /// <summary>
    /// 检查是否为数值类型
    /// </summary>
    private bool IsNumericType(object value)
    {
        return value is int || value is float || value is double || value is decimal;
    }

    /// <summary>
    /// 处理数值变化的核心方法 - 使用ConfigArrays替代valueConfigs
    /// </summary>
    /// <param name="configKey">配置键名</param>
    /// <summary>
    /// 处理数值变化的核心方法 - 简化版
    /// </summary>
    /// <param name="configKey">配置键名</param>
    private void ProcessValueChange(string configKey, int direction)
    {
        string mjType = GetCurrentMjType();
        string lowerKey = configKey.ToLower();

        // 验证配置项并获取数组
        if (!ConfigArrays.GetConfigurableKeys(mjType).Contains(lowerKey))
        {
            return;
        }
        object[] configArray = ConfigArrays.GetConfigArray(mjType, lowerKey);
        if (configArray.Length == 0)
        {
            return;
        }
        // 获取UI组件和当前值
        var button = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.GetComponent<Button>();
        var textComponent = FindTextComponentSmart(button, configKey);
        if (button == null || textComponent == null)
        {
            return;
        }

        // 解析当前文本值
        if (!TryParseCurrentValue(configKey, textComponent.text, out object currentValue))
        {
            return;
        }

        // 计算新值并检查边界
        int currentIndex = FindValueIndexInArray(configArray, currentValue);
        int newIndex = currentIndex + direction;

        // 检查边界 - 如果超出边界则不执行操作
        if (newIndex < 0 || newIndex >= configArray.Length)
        {
            // 即使达到边界，也要更新按钮状态
            UpdateButtonStatesSimple(button.transform.parent.parent, configArray, currentIndex);
            return;
        }

        // 更新数值和显示
        object newValue = configArray[newIndex];
        UpdateConfigValue(configKey, newValue);

        // 如果更改的是底分项目，则根据当前选中的麻将类型和新的底分索引重新加载记忆/服务器配置
        if (lowerKey == "basecoin" && currentMode != OperationMode.EditRoom)
        {
            LoadMemoryConfigs(mjType, newIndex);
        }

        // 格式化并更新文本显示
        string formattedValue = FormatDisplayValue(configKey, newValue);
        textComponent.text = formattedValue;

        // 实时更新按钮状态 - 根据新索引判断是否已接近边界
        UpdateButtonStatesSimple(button.transform.parent.parent, configArray, newIndex);

    }

    /// <summary>
    /// 更新带有加减按钮的UI组件状态
    /// </summary>
    /// <param name="content">配置面板根对象</param>
    /// <param name="uiPath">UI组件在面板中的路径(如 "difen" 或 "fengding")</param>
    /// <param name="configKey">配置键名(如 "baseCoin" 或 "fengDing")</param>
    /// <param name="value">当前配置值</param>
    private void UpdateAddSubUI(GameObject content, string uiPath, string configKey, object value)
    {
        if (content == null) return;

        // 1. 查找文本组件并更新显示 - 尝试多种可能的路径
        Transform textTrans = content.transform.Find($"{uiPath}/text")
                           ?? content.transform.Find($"{uiPath}/AddAndSub/text");
        if (textTrans != null)
        {
            Text textComponent = textTrans.GetComponent<Text>();
            if (textComponent != null)
            {
                textComponent.text = FormatDisplayValue(configKey, value);
            }
        }

        // 2. 查找UI组件的根节点（可能是 uiPath 本身或 uiPath/AddAndSub）
        Transform uiRoot = content.transform.Find(uiPath);
        if (uiRoot == null) return;

        // 尝试查找 AddAndSub 子节点，如果没有则使用 uiRoot
        Transform addSubParent = uiRoot.Find("AddAndSub") ?? uiRoot;

        // 3. 更新按钮状态
        object[] configArray = ConfigArrays.GetConfigArray(GetCurrentMjType(), configKey);
        if (configArray != null && configArray.Length > 0)
        {
            int currentIndex = FindValueIndexInArray(configArray, value);
            UpdateButtonStatesSimple(addSubParent, configArray, currentIndex);
        }
    }

    /// <summary>
    /// 更新按钮显示状态
    /// </summary>
    /// <param name="parent">父节点</param>
    /// <param name="configArray">数值配置数组</param>
    /// <param name="currentIndex">当前索引</param>
    private void UpdateButtonStatesSimple(Transform parent, object[] configArray, int currentIndex)
    {
        if (parent == null || configArray == null) return;

        // 1. 处理加号按钮
        // 兼容多种拼写：AddFase (截图显示), AddFlase (代码原有)
        string[] addPaths = { "AddFase", "AddFlase" };
        Transform addRoot = null;
        foreach (var path in addPaths)
        {
            addRoot = parent.Find(path);
            if (addRoot != null) break;
        }

        if (addRoot != null)
        {
            bool canAdd = currentIndex < configArray.Length - 1;
            // 查找高亮状态节点 AddTrue
            Transform addTrue = addRoot.Find("AddTrue");
            if (addTrue != null)
            {
                addTrue.gameObject.SetActive(canAdd);
            }

            // 设置按钮交互性 (Button 可能在父节点或子节点上)
            Button btn = addRoot.GetComponent<Button>();
            if (btn == null && addTrue != null) btn = addTrue.GetComponent<Button>();
            if (btn != null) btn.interactable = canAdd;

            // 如果没有 AddTrue 节点，则直接控制根节点的显示隐藏
            if (addTrue == null)
            {
                addRoot.gameObject.SetActive(canAdd);
            }
        }

        // 2. 处理减号按钮
        // 兼容多种拼写：SubtractFase, SubtractFlase
        string[] subPaths = { "SubtractFase", "SubtractFlase" };
        Transform subRoot = null;
        foreach (var path in subPaths)
        {
            subRoot = parent.Find(path);
            if (subRoot != null) break;
        }

        if (subRoot != null)
        {
            bool canSub = currentIndex > 0;
            // 查找高亮状态节点 SubtractTrue
            Transform subTrue = subRoot.Find("SubtractTrue");
            if (subTrue != null)
            {
                subTrue.gameObject.SetActive(canSub);
            }

            // 设置按钮交互性
            Button btn = subRoot.GetComponent<Button>();
            if (btn == null && subTrue != null) btn = subTrue.GetComponent<Button>();
            if (btn != null) btn.interactable = canSub;

            // 如果没有 SubtractTrue 节点，则直接控制根节点的显示隐藏
            if (subTrue == null)
            {
                subRoot.gameObject.SetActive(canSub);
            }
        }
    }

    /// <summary>
    /// 设置仙桃晃晃Toggle为选中状态
    /// </summary>
    private void SetXianTaoHuangHuangToggle()
    {

        // 先取消其他Toggle的选中状态
        for (int i = 0; i < varContent.transform.childCount; i++)
        {
            var childToggle = varContent.transform.GetChild(i).GetComponent<Toggle>();
            childToggle.isOn = false;
        }

        // 直接设置仙桃晃晃Toggle为选中状态
        var xianTaoToggle = varContent.transform.Find(MjGameType.XIANTAO_HUANGHUANG)?.GetComponent<Toggle>();
        if (xianTaoToggle != null)
        {
            if (xianTaoToggle.isOn)
            {
                OnMjTypeChanged(MjGameType.XIANTAO_HUANGHUANG);
            }
            else
            {
                xianTaoToggle.isOn = true;
            }
        }
    }


    //根据对应的名字设置Toggle 如果不是互斥的可以多选toggle
    private void SetToggleByName(GameObject content, string toggleGroupName, string toggleName, bool isOn)
    {
        Transform groupTrans = content.transform.Find(toggleGroupName);
        if (groupTrans == null)
        {
            return;
        }
        Transform toggleTrans = groupTrans.Find(toggleName);
        if (toggleTrans == null)
        {
            return;
        }
        Toggle toggle = toggleTrans.GetComponent<Toggle>();
        if (toggle != null)
        {
            toggle.isOn = isOn;
        }
    }

    /// <summary>
    /// 重置指定组下所有Toggle的状态
    /// </summary>
    /// <param name="content">内容根对象</param>
    /// <param name="toggleGroupName">Toggle组名</param>
    /// <param name="isOn">要设置的状态</param>
    private void ResetAllTogglesInGroup(GameObject content, string toggleGroupName, bool isOn)
    {
        Transform groupTrans = content.transform.Find(toggleGroupName);
        if (groupTrans == null)
        {
            return;
        }

        // 遍历该组下的所有子对象，将所有Toggle设置为指定状态
        for (int i = 0; i < groupTrans.childCount; i++)
        {
            Transform child = groupTrans.GetChild(i);
            Toggle toggle = child.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.isOn = isOn;
            }
        }
    }
    /// <summary>
    /// 设置麻将弹窗数据 - 根据当前楼层设置的数据
    /// </summary>
    /// <param name="floorData">楼层数据</param>
    private void SetMahjongConfig(Msg_Floor floorData)
    {
        // 根据游戏类型进行不同的处理
        switch (floorData.MethodType)
        {
            case MethodType.MjSimple:
                HandleMjSimpleConfig(floorData);
                break;
            case MethodType.RunFast:
                HandleRunFastConfig(floorData);
                break;
            default:
                GF.LogWarning($"不支持的游戏类型: {floorData.MethodType}");
                GF.UI.ShowToast($"不支持的游戏类型: {floorData.MethodType}");
                break;
        }
    }

    /// <summary>
    /// 处理简单麻将配置
    /// </summary>
    /// <param name="floorData">楼层数据</param>
    private void HandleMjSimpleConfig(Msg_Floor floorData)
    {
        // 根据麻将方法类型进行处理
        switch (floorData.MjConfig.MjMethod)
        {
            case MJMethod.Huanghuang:
                // 先设置麻将类型选择
                SetXianTaoHuangHuangToggle();
                // 直接设置具体配置
                SetHuanghuangConfigFromFloorData(floorData);
                break;
            case MJMethod.Kwx:
                // 卡五星
                SetKWXToggle();
                SetKWXConfigFromFloorData(floorData);
                break;
            case MJMethod.Xl:
                // 血流成河
                SetXLCHToggle();
                SetXLCHConfigFromFloorData(floorData);
                break;
            default:
                GF.LogWarning($"不支持的麻将方法: {floorData.MjConfig.MjMethod}");
                // 对于其他类型，可以在这里添加相应的处理逻辑
                break;
        }
    }

    /// <summary>
    /// 处理跑的快配置
    /// </summary>
    /// <param name="floorData">楼层数据</param>
    private void HandleRunFastConfig(Msg_Floor floorData)
    {
        // 设置初始化标志，防止 OnMjTypeChanged 中加载记忆配置覆盖我们要设置的配置
        IsInitializing = true;
        try
        {
            // 先设置跑的快类型选择（这会显示面板）
            SetRunFastToggle();
            // 延迟一帧后再设置具体配置，确保面板已经完全显示并初始化完成
            StartCoroutine(DelayedSetRunFastConfig(floorData));
        }
        finally
        {
            // 注意：这里不立即重置标志，在 DelayedSetRunFastConfig 完成后再重置
        }
    }

    /// <summary>
    /// 延迟设置跑的快配置（确保面板已完全初始化）
    /// </summary>
    private System.Collections.IEnumerator DelayedSetRunFastConfig(Msg_Floor floorData)
    {
        // 等待一帧，确保UI完全刷新
        yield return null;
        try
        {
            // 设置具体配置
            SetRunFastConfigFromFloorData(floorData);
        }
        finally
        {
            // 配置设置完成后，重置初始化标志
            IsInitializing = false;
        }
    }

    /// <summary>
    /// 设置跑的快Toggle为选中状态
    /// </summary>
    private void SetRunFastToggle()
    {
        // 先取消其他Toggle的选中状态
        for (int i = 0; i < varContent.transform.childCount; i++)
        {
            var childToggle = varContent.transform.GetChild(i).GetComponent<Toggle>();
            childToggle.isOn = false;
        }

        // 直接设置跑的快Toggle为选中状态
        var runFastToggle = varContent.transform.Find(MjGameType.PAO_DE_KUAI)?.GetComponent<Toggle>();
        if (runFastToggle != null)
        {
            if (runFastToggle.isOn)
            {
                OnMjTypeChanged(MjGameType.PAO_DE_KUAI);
            }
            else
            {
                runFastToggle.isOn = true;
            }
        }
    }

    /// <summary>
    /// 根据楼层数据设置跑的快配置
    /// </summary>
    /// <param name="floorData">楼层数据</param>
    private void SetRunFastConfigFromFloorData(Msg_Floor floorData)
    {
        Transform contentTransform = varGameRule.transform.Find("跑的快/rule/Viewport/Content");
        if (contentTransform == null)
        {
            return;
        }

        GameObject content = contentTransform.gameObject;
        if (content == null || floorData.RunFastConfig == null)
        {
            return;
        }
        GF.LogInfo_wl("设置跑的快配置从楼层数据开始", floorData.ToString());
        //设置通用配置  禁用语音 和禁用打字
        SetToggleByName(content, "gaoji", "jinyongyuying", floorData.Config.ForbidVoice);
        SetToggleByName(content, "gaoji", "jinyongdazi", floorData.Config.Forbid);
        // 设置基础配置
        SetToggleByName(content, "renshu", floorData.Config.PlayerNum.ToString(), true);
        SetToggleByName(content, "jushu", floorData.Config.PlayerTime.ToString(), true);
        SetToggleByName(content, "jushu1", floorData.Config.PlayerTime.ToString(), true);
        // 设置跑的快特有配置
        SetToggleByName(content, "xianchu", floorData.RunFastConfig.FirstRun.ToString(), true); // 先出权
        SetToggleByName(content, "wanfa0", floorData.RunFastConfig.Play.ToString(), true); // 玩法
        SetToggleByName(content, "chupai", floorData.RunFastConfig.WithMin.ToString(), true); // 出牌方式

        // 【修复】先重置所有复选框为 false，防止旧选项残留
        ResetAllTogglesInGroup(content, "wanfa1", false);
        ResetAllTogglesInGroup(content, "wanfa2", false);
        ResetAllTogglesInGroup(content, "wanfa3", false);
        ResetAllTogglesInGroup(content, "wanfa4", false);
        ResetAllTogglesInGroup(content, "wanfa5", false);

        // 设置复选框配置（设置新配置中有的选项为 true）
        foreach (var checkOption in floorData.RunFastConfig.Check)
        {
            SetToggleByName(content, "wanfa1", checkOption.ToString(), true);
            SetToggleByName(content, "wanfa2", checkOption.ToString(), true);
            SetToggleByName(content, "wanfa3", checkOption.ToString(), true);
            SetToggleByName(content, "wanfa4", checkOption.ToString(), true);
            SetToggleByName(content, "wanfa5", checkOption.ToString(), true);
        }
        // 设置底分
        UpdateAddSubUI(content, "difen", "BaseCoin", floorData.Config.BaseCoin);
        // 设置抽水相关配置
        SetRateConfig(floorData, contentTransform, content);
        // 应用人数规则限制
        var config = mJConfigManager.GetConfig(MjGameType.PAO_DE_KUAI) as MJConfigManager.PDKConfigData;
        if (config != null)
        {
            // 同步从floorData加载的配置到内存config
            config.PeopleNum = floorData.Config.PlayerNum;
            config.PlayNum = (int)floorData.Config.PlayerTime;
            config.BaseCoin = floorData.Config.BaseCoin;
            config.ForbidVoice = floorData.Config.ForbidVoice;
            config.Forbid = floorData.Config.Forbid;

            if (floorData.RunFastConfig != null)
            {
                config.FirstCard = floorData.RunFastConfig.FirstRun;
                config.PlayGame = floorData.RunFastConfig.Play;
                config.ChuPai = floorData.RunFastConfig.WithMin;
                config.Check.Clear();
                if (floorData.RunFastConfig.Check != null)
                {
                    config.Check.AddRange(floorData.RunFastConfig.Check);
                }
                config.DismissTimes = floorData.RunFastConfig.DismissTimes;
                config.ApplyDismissTime = floorData.RunFastConfig.ApplyDismissTime;
            }
            // 应用规则限制
            ApplyPDKPeopleNumRules(floorData.Config.PlayerNum, config);
        }
    }

    /// <summary>
    /// 检查并从楼层数据初始化
    /// </summary>
    /// <param name="userData">用户传入的数据</param>
    private void CheckAndInitializeFromFloorData(object userData)
    {
        VarString str = Params.Get<VarString>(PARAM_SET_FLOOR);
        // 检查参数是否为空
        if (str == null)
        {
            currentMode = OperationMode.CreateFloor;
            return;
        }

        // 获取字符串值进行比较
        string modeValue = str.Value;

        if (modeValue == MODE_CREATE_ROOM)
        {
            currentMode = OperationMode.CreateRoom;
        }
        else if (modeValue == MODE_EDIT_ROOM)
        {
            currentMode = OperationMode.EditRoom;
        }
        else if (modeValue == MODE_CREATE_FLOOR)
        {
            currentMode = OperationMode.CreateFloor;
        }
        else
        {
            currentMode = OperationMode.CreateFloor;
        }
    }    /// <summary>
         /// 点击选择
         /// </summary>
         /// <param name="selectContent">当前选着的</param>
         /// <param name="toggleNames">选择的名字</param>
         /// <param name="typeItem">生成的模板</param>
         /// <param name="changeContent">根据选着改变的</param>
         /// <param name="onToggleChanged">Toggle改变时的回调</param>
         /// <param name="filterGameType">过滤的游戏类型，如果不为空则只显示该类型</param>
    public void RegisterToggleListeners(ToggleGroup selectContent, string[] toggleNames, Toggle typeItem, GameObject changeContent, System.Action<string> onToggleChanged = null, string filterGameType = null)
    {
        // 定义选中和未选中的颜色
        Color selectedColor = new Color(0xAB / 255f, 0x64 / 255f, 0x2A / 255f); // #AB642A
        Color normalColor = Color.white;


        // 清空现有Toggle子对象
        foreach (Transform item in selectContent.transform)
        {
            Destroy(item.gameObject);
        }

        Toggle firstToggle = null;

        // 创建Toggle并注册监听
        for (int i = 0; i < toggleNames.Length; i++)
        {
            string type = toggleNames[i];

            // 如果指定了过滤类型，只创建匹配的Toggle
            if (!string.IsNullOrEmpty(filterGameType) && type != filterGameType)
            {
                continue;
            }
            GameObject go = Instantiate(typeItem.gameObject, selectContent.transform);
            go.name = type;

            // 查找并更新文本组件 - 尝试多个可能的路径
            Text changeNameText = go.transform.Find("ChangeName")?.GetComponent<Text>();
            if (changeNameText != null)
            {
                changeNameText.text = type;
            }

            // 兼容性检查：如果存在 Label 子节点，也同步更新
            Text labelText = go.transform.Find("Label")?.GetComponent<Text>();
            if (labelText != null)
            {
                labelText.text = type;
            }

            Toggle toggle = go.GetComponent<Toggle>();
            toggle.group = selectContent;
            toggle.onValueChanged.RemoveAllListeners();

            // 添加Toggle状态变化监听，包含字体颜色切换
            toggle.onValueChanged.AddListener((isOn) =>
            {
                if (changeNameText != null) changeNameText.color = isOn ? selectedColor : normalColor;
                if (labelText != null) labelText.color = isOn ? selectedColor : normalColor;

                if (isOn)
                {
                    // 只调用回调,由OnMjTypeChanged统一处理面板切换
                    onToggleChanged?.Invoke(type);
                }
            });

            // 记录第一个实际创建的Toggle
            if (firstToggle == null)
            {
                firstToggle = toggle;
            }
        }

        // 默认选中第一个Toggle,触发面板显示
        if (firstToggle != null)
        {
            if (firstToggle.isOn)
            {
                // 如果已经是开启状态，手动触发逻辑以确保面板切换
                onToggleChanged?.Invoke(firstToggle.name);
            }
            else
            {
                firstToggle.isOn = true;
            }

            // 确保字体颜色正确
            Text mainText = firstToggle.transform.Find("ChangeName")?.GetComponent<Text>()
                         ?? firstToggle.transform.Find("Label")?.GetComponent<Text>();
            if (mainText != null) mainText.color = selectedColor;
        }
    }
    #endregion


    #region 通用配置方法（整数参数）

    /// <summary>
    /// 通过方法名调用配置方法 - 自动解析方法名和参数
    /// 支持格式：
    /// 1. "SetPeopleNum_4" -> 调用 SetPeopleNum(4)
    /// 2. "SetPeopleNum(4)" -> 调用 SetPeopleNum(4)
    /// 3. "SetPeopleNum" -> 调用 SetPeopleNum(0)
    /// </summary>
    /// <param name="methodName">方法名称（可包含参数）</param>
    public void SetIntByName(string methodName)
    {
        if (string.IsNullOrEmpty(methodName))
        {
            return;
        }

        // 添加空引用检查
        if (configSetter == null)
        {
            configSetter = new MJConfigSetter(this);
            if (configSetter == null)
            {
                return;
            }
        }
        string actualMethodName = methodName;
        int value = 0;
        // 尝试从下划线格式提取 (SetPeopleNum_4)
        int underscoreIndex = methodName.LastIndexOf('_');
        if (underscoreIndex > 0)
        {
            string valueStr = methodName.Substring(underscoreIndex + 1);
            if (int.TryParse(valueStr, out int parsedValue))
            {
                actualMethodName = methodName.Substring(0, underscoreIndex);
                value = parsedValue;
            }
        }
        // 尝试从括号格式提取 (SetPeopleNum(4))
        else
        {
            int startIndex = methodName.IndexOf('(');
            int endIndex = methodName.IndexOf(')');
            if (startIndex > 0 && endIndex > startIndex)
            {
                string valueStr = methodName.Substring(startIndex + 1, endIndex - startIndex - 1);
                if (int.TryParse(valueStr, out int parsedValue))
                {
                    actualMethodName = methodName.Substring(0, startIndex);
                    value = parsedValue;
                }
            }
        }

        // 根据方法名调用对应的方法
        switch (actualMethodName)
        {
            // 通用配置
            case "SetPeopleNum":
                configSetter.SetPeopleNum(value);
                break;
            case "SetPlayNum":
                configSetter.SetPlayNum(value);
                break;
            case "SetRateType":
                configSetter.SetRateType(value);
                break;
            case "SetTuoGuan":
                configSetter.SetTuoGuan(value);
                break;

            // 仙桃晃晃配置
            case "SetLaiPlay":
                configSetter.SetLaiPlay(value);
                break;
            case "SetPiaoLai":
                configSetter.SetPiaoLai(value);
                break;
            case "SetLockCard":
                configSetter.SetLockCard(value);
                break;
            case "SetGangKai":
                configSetter.SetGangKai(value);
                break;
            case "SetZhuo":
                configSetter.SetZhuo(value);
                break;

            // 卡五星配置
            case "SetBuyHorse":
                configSetter.SetBuyHorse(value);
                break;
            case "SetTopOut":
                configSetter.SetTopOut(value);
                break;
            case "SetChoicePiao":
                configSetter.SetChoicePiao(value);
                break;

            // 跑得快配置
            case "SetFirstCard":
                configSetter.SetFirstCard(value);
                break;
            case "SetPlayGame":
                configSetter.SetPlayGame(value);
                break;
            case "SetChuPai":
                configSetter.SetChuPai(value);
                break;

            //血流配置
            case "SetFan":
                configSetter.SetFan(value);
                break;
            case "SetPiao":
                configSetter.SetPiao(value);
                break;
            case "SetChangeCard":
                configSetter.SetChangeCard(value);
                break;
            default:
                break;
        }
    }

    #region 加减值操作方法

    /// <summary>
    /// 通过方法名调用加减配置方法
    /// </summary>
    /// <param name="methodName">方法名称</param>
    public void SetAddOrSubByName(string methodName)
    {
        // 添加空引用检查
        if (configSetter == null)
        {
            configSetter = new MJConfigSetter(this);
            if (configSetter == null)
            {
                return;
            }
        }

        switch (methodName)
        {
            case "AddBaseCoin":
                configSetter.AddBaseCoin();
                break;
            case "SubtractBaseCoin":
                configSetter.SubtractBaseCoin();
                break;
            case "AddFengDing":
                configSetter.AddFengDing();
                break;
            case "SubtractFengDing":
                configSetter.SubtractFengDing();
                break;
            case "AddOfflineClick":
                configSetter.AddOfflineClick();
                break;
            case "SubtractOfflineClick":
                configSetter.SubtractOfflineClick();
                break;
            case "AddDismissTimes":
                configSetter.AddDismissTimes();
                break;
            case "SubtractDismissTimes":
                configSetter.SubtractDismissTimes();
                break;
            case "AddApplyDismissTime":
                configSetter.AddApplyDismissTime();
                break;
            case "SubtractApplyDismissTime":
                configSetter.SubtractApplyDismissTime();
                break;
            case "AddOffDismissTime":
                configSetter.AddOffDismissTime();
                break;
            case "SubtractOffDismissTime":
                configSetter.SubtractOffDismissTime();
                break;
            case "AddRate":
                configSetter.AddRate();
                break;
            case "SubtractRate":
                configSetter.SubtractRate();
                break;
            case "AddMinRate":
                configSetter.AddMinRate();
                break;
            case "SubtractMinRate":
                configSetter.SubtractMinRate();
                break;
            default:
                break;
        }
    }

    #endregion


    #endregion

    #region 卡五星配置恢复方法
    /// <summary>
    /// 设置卡五星Toggle为选中状态
    /// </summary>
    private void SetKWXToggle()
    {
        // 先取消其他Toggle的选中状态
        for (int i = 0; i < varContent.transform.childCount; i++)
        {
            var childToggle = varContent.transform.GetChild(i).GetComponent<Toggle>();
            if (childToggle != null)
            {
                childToggle.isOn = false;
            }
        }

        // 直接设置卡五星Toggle为选中状态
        var kwxToggle = varContent.transform.Find("卡五星")?.GetComponent<Toggle>();
        if (kwxToggle != null)
        {
            if (kwxToggle.isOn)
            {
                OnMjTypeChanged("卡五星");
            }
            else
            {
                kwxToggle.isOn = true;
            }
        }
    }


    /// <summary>
    /// 根据楼层数据设置仙桃晃晃配置
    /// </summary>
    /// <param name="popup">弹窗实例</param>
    /// <param name="mjConfig">麻将配置数据</param>
    private void SetHuanghuangConfigFromFloorData(Msg_Floor floorData)
    {
        GF.LogInfo_wl("设置仙桃晃晃配置从楼层数据", floorData.ToString());
        Transform contentTrans = varGameRule.transform.Find("仙桃晃晃/rule/Viewport/Content");
        GameObject content = contentTrans.gameObject;
        // 设置通用配置 禁用语音和禁用打字
        SetToggleByName(content, "gaoji", "jinyongyuying", floorData.Config.ForbidVoice);
        SetToggleByName(content, "gaoji", "jinyongdazi", floorData.Config.Forbid);
        SetToggleByName(content, "renshu", floorData.Config.PlayerNum.ToString(), true);
        SetToggleByName(content, "jushu", floorData.Config.PlayerTime.ToString(), true);
        SetToggleByName(content, "wanfa", floorData.MjConfig.Xthh.LaiPlay.ToString(), true);
        SetToggleByName(content, "piaolai", floorData.MjConfig.Xthh.PiaoLai.ToString(), true);

        // 【修复】先重置所有复选框为 false，防止旧选项残留
        ResetAllTogglesInGroup(content, "kexuan", false);

        // 设置新配置中有的选项为 true
        if (floorData.MjConfig.Xthh.Check != null && floorData.MjConfig.Xthh.Check.Count > 0)
        {
            foreach (var checkOption in floorData.MjConfig.Xthh.Check)
            {
                SetToggleByName(content, "kexuan", checkOption.ToString(), true);
            }
        }

        SetToggleByName(content, "suopai", floorData.MjConfig.Xthh.LockCard.ToString(), true);
        SetToggleByName(content, "gangkai", floorData.MjConfig.Xthh.GangKai.ToString(), true);
        SetToggleByName(content, "zhuochong", floorData.MjConfig.Xthh.Zhuo.ToString(), true);
        SetToggleByName(content, "zhuochong", "3", floorData.MjConfig.Xthh.OpenDouble.ToString().Equals("1"));

        // 设置底分
        UpdateAddSubUI(content, "difen", "BaseCoin", floorData.Config.BaseCoin);

        // 设置封顶
        UpdateAddSubUI(content, "fengding", "FengDing", floorData.MjConfig.Xthh.MaxTime);

        var difenTextTrans = content.transform.Find("difen/AddAndSub/text");
        if (difenTextTrans != null)
        {
            var difenText = difenTextTrans.GetComponent<Text>();
            if (difenText != null)
            {
                difenText.text = floorData.Config.BaseCoin.ToString() + "分";
            }
        }
        var fengdingTextTrans = content.transform.Find("fengding/AddAndSub/text");
        if (fengdingTextTrans != null)
        {
            var fengdingText = fengdingTextTrans.GetComponent<Text>();
            if (fengdingText != null)
            {
                if (floorData.MjConfig.Xthh.MaxTime == 0)
                {
                    fengdingText.text = "不封顶";
                }
                else
                {
                    fengdingText.text = floorData.MjConfig.Xthh.MaxTime.ToString();
                }
            }
        }

        SetToggleByName(content, "youxituoguan", floorData.MjConfig.Xthh.Tuoguan.ToString(), true);

        var lixiantirenTextTrans = content.transform.Find("lixiantiren/AddAndSub/text") ?? content.transform.Find("lixiantiren/text");
        if (lixiantirenTextTrans != null)
        {
            var lixiantirenText = lixiantirenTextTrans.GetComponent<Text>();
            if (lixiantirenText != null)
            {
                if (floorData.MjConfig.Xthh.OffDismissTime == 0)
                {
                    lixiantirenText.text = "不踢人";
                }
                else
                {
                    lixiantirenText.text = floorData.MjConfig.Xthh.OffDismissTime.ToString();
                }
            }
        }

        var jiesancishuTextTrans = content.transform.Find("jiesancishu/AddAndSub/text");
        if (jiesancishuTextTrans != null)
        {
            var jiesancishuText = jiesancishuTextTrans.GetComponent<Text>();
            if (jiesancishuText != null)
            {
                if (floorData.MjConfig.Xthh.DismissTimes == 0)
                {
                    jiesancishuText.text = "不限制";
                }
                else
                {
                    jiesancishuText.text = floorData.MjConfig.Xthh.DismissTimes.ToString();
                }
            }
        }

        var shenqingTextTrans = content.transform.Find("shenqingjiesanshijian/AddAndSub/text");
        if (shenqingTextTrans != null)
        {
            var shenqingText = shenqingTextTrans.GetComponent<Text>();
            if (shenqingText != null)
            {
                shenqingText.text = floorData.MjConfig.Xthh.ApplyDismissTime.ToString() + "分钟";
            }
        }
        // 入桌自动准备设置
        SetToggleByName(content, "zhunbeishezhi", "w", floorData.MjConfig.Xthh.ReadyState.ToString().Equals("1"));
        // 允许旁观设置
        SetToggleByName(content, "pangguan", "IsOpen", floorData.MjConfig.Xthh.Watch.ToString().Equals("1"));

        var lixianjiesanTextTrans = content.transform.Find("lixianjiesanshijian/AddAndSub/text");
        if (lixianjiesanTextTrans != null)
        {
            var lixianjiesanText = lixianjiesanTextTrans.GetComponent<Text>();
            if (lixianjiesanText != null)
            {
                lixianjiesanText.text = (floorData.MjConfig.Xthh.OffDismissTime / 60).ToString() + "小时";
            }
        }
        // 设置抽水相关配置
        SetRateConfig(floorData, contentTrans, content);
        // 应用人数规则限制
        var config = mJConfigManager.GetConfig(MjGameType.XIANTAO_HUANGHUANG) as MJConfigManager.XTHHConfigData;
        if (config != null)
        {
            // 同步从floorData加载的配置到内存config
            config.PeopleNum = floorData.Config.PlayerNum;
            config.PlayNum = (int)floorData.Config.PlayerTime;
            config.BaseCoin = floorData.Config.BaseCoin;
            config.ForbidVoice = floorData.Config.ForbidVoice;
            config.Forbid = floorData.Config.Forbid;

            if (floorData.MjConfig != null && floorData.MjConfig.Xthh != null)
            {
                var xthh = floorData.MjConfig.Xthh;
                config.LaiPlay = xthh.LaiPlay;
                config.PiaoLai = xthh.PiaoLai;
                config.LockCard = xthh.LockCard;
                config.GangKai = xthh.GangKai;
                config.Zhuo = xthh.Zhuo;
                config.OpenDouble = xthh.OpenDouble;
                config.MaxTime = xthh.MaxTime;
                config.Tuoguan = xthh.Tuoguan;
                config.OfflineClick = xthh.OfflineClick;
                config.DismissTimes = xthh.DismissTimes;
                config.ApplyDismissTime = xthh.ApplyDismissTime;
                config.OffDismissTime = xthh.OffDismissTime;
                config.ReadyState = 0;
                config.Watch = 0;

                config.Check.Clear();
                if (xthh.Check != null)
                {
                    config.Check.AddRange(xthh.Check);
                }
            }

            // 应用规则限制
            ApplyXTHHPeopleNumRules(floorData.Config.PlayerNum, config);
        }
    }

    /// <summary>
    /// 设置抽水相关配置
    /// </summary>
    private void SetRateConfig(Msg_Floor floorData, Transform contentTrans, GameObject content)
    {
        //抽水判断
        SetToggleByName(content, "IsOpenRate", "isOpen", floorData.Config.OpenRate);

        // 同步到内存配置
        var config = GetCurrentConfig();
        if (config != null)
        {
            config.openRate = floorData.Config.OpenRate;
        }

        // 如果是创建房间模式，不显示抽水相关组件（个人房不允许抽水）
        if (currentMode == OperationMode.CreateRoom)
        {
            contentTrans.Find("SetRateType").gameObject.SetActive(false);
            contentTrans.Find("SetRate").gameObject.SetActive(false);
            contentTrans.Find("SetRateValue").gameObject.SetActive(false);
            return;
        }

        //如果开启抽水 设置抽水方式
        if (floorData.Config.OpenRate == true)
        {
            contentTrans.Find("SetRateType").gameObject.SetActive(true);
            contentTrans.Find("SetRate").gameObject.SetActive(true);
            contentTrans.Find("SetRateValue").gameObject.SetActive(true);
            if (config != null)
            {
                config.RateType = floorData.Config.RateType;
                if (floorData.Config.Rate != null)
                {
                    config.Rate = floorData.Config.Rate.Key;
                    config.MinRate = floorData.Config.Rate.Val;
                }
            }

            //设置抽水方式
            contentTrans.Find("SetRateType").gameObject.SetActive(true);
            SetToggleByName(content, "SetRateType", floorData.Config.RateType.ToString(), true);
            //设置抽水比例
            contentTrans.Find("SetRate").gameObject.SetActive(true);
            if (floorData.Config.Rate != null)
            {
                UpdateAddSubUI(content, "SetRate", "Rate", floorData.Config.Rate.Key);
            }

            if (floorData.Config.Rate != null)
            {
                //将抽水比例显示到文本上
                var rateTextTrans = content.transform.Find("SetRate/AddAndSub/text");
                if (rateTextTrans != null)
                {
                    var rateText = rateTextTrans.GetComponent<Text>();
                    if (rateText != null)
                    {
                        rateText.text = (floorData.Config.Rate.Key / 10).ToString() + "%";
                    }
                }
                if (floorData.Config.Rate.Key / 10 > 0)
                {
                    content.transform.Find("SetRate/text");
                }
                else
                {
                    rateTextTrans.GetComponent<Text>().fontSize = 40;
                }
            }
            //设置最低抽水
            contentTrans.Find("SetRateValue").gameObject.SetActive(true);
            if (floorData.Config.Rate != null)
            {
                UpdateAddSubUI(content, "SetRateValue", "MinRate", floorData.Config.Rate.Val);
            }
        }
        else
        {
            //隐藏抽水方式和比例设置
            contentTrans.Find("SetRateType").gameObject.SetActive(false);
            contentTrans.Find("SetRate").gameObject.SetActive(false);
            contentTrans.Find("SetRateValue").gameObject.SetActive(false);
        }
    }


    /// <summary>
    /// 根据楼层数据设置卡五星配置
    /// </summary>
    /// <param name="floorData">楼层数据</param>
    private void SetKWXConfigFromFloorData(Msg_Floor floorData)
    {
        GF.LogInfo_wl("设置卡五星配置从楼层数据", floorData.ToString());

        Transform contentTransform = varGameRule.transform.Find("卡五星/rule/Viewport/Content");
        if (contentTransform == null)
        {
            return;
        }

        GameObject content = contentTransform.gameObject;
        if (content == null || floorData.MjConfig.Kwx == null)
        {
            return;
        }

        // 设置通用配置 禁用语音和禁用打字
        SetToggleByName(content, "gaoji", "jinyongyuying", floorData.Config.ForbidVoice);
        SetToggleByName(content, "gaoji", "jinyongdazi", floorData.Config.Forbid);

        // 设置基础配置
        SetToggleByName(content, "renshu", floorData.Config.PlayerNum.ToString(), true);
        SetToggleByName(content, "jushu", floorData.Config.PlayerTime.ToString(), true);

        // 设置卡五星特有配置
        SetToggleByName(content, "maima", floorData.MjConfig.Kwx.BuyHorse.ToString(), true); // 买马设置
        SetToggleByName(content, "fengding", floorData.MjConfig.Kwx.TopOut.ToString(), true); // 封顶
        SetToggleByName(content, "xuanpiao", floorData.MjConfig.Kwx.ChoicePiao.ToString(), true); // 选漂
        SetToggleByName(content, "xuanpiao (1)", floorData.MjConfig.Kwx.ChoicePiao.ToString(), true); // 选漂

        // 【修复】先重置所有复选框为 false，防止旧选项残留
        ResetAllTogglesInGroup(content, "wanfa1", false);
        ResetAllTogglesInGroup(content, "wanfa2", false);
        ResetAllTogglesInGroup(content, "wanfa3", false);
        ResetAllTogglesInGroup(content, "wanfa4", false);

        // 设置玩法复选框
        foreach (var playOption in floorData.MjConfig.Kwx.Play)
        {
            SetToggleByName(content, "wanfa1", playOption.ToString(), true);
            SetToggleByName(content, "wanfa2", playOption.ToString(), true);
            SetToggleByName(content, "wanfa3", playOption.ToString(), true);
            SetToggleByName(content, "wanfa4", playOption.ToString(), true);
        }
        // 设置抽水相关配置
        SetRateConfig(floorData, contentTransform, content);
        // // 设置准备状态复选框
        // foreach (var readyOption in floorData.MjConfig.Kwx.ReadyState)
        // {
        //     SetToggleByName(content, "zhunbeishezhi", readyOption.ToString(), true);
        // }

        // 设置底分
        UpdateAddSubUI(content, "difen", "BaseCoin", floorData.Config.BaseCoin);

        // 应用人数规则限制
        var config = mJConfigManager.GetConfig("卡五星") as MJConfigManager.KWXConfigData;
        if (config != null)
        {
            // 同步从floorData加载的配置到内存config
            config.PeopleNum = floorData.Config.PlayerNum;
            config.PlayNum = (int)floorData.Config.PlayerTime;
            config.BaseCoin = floorData.Config.BaseCoin;
            config.ForbidVoice = floorData.Config.ForbidVoice;
            config.Forbid = floorData.Config.Forbid;

            if (floorData.MjConfig != null && floorData.MjConfig.Kwx != null)
            {
                var kwx = floorData.MjConfig.Kwx;
                config.BuyHorse = kwx.BuyHorse;
                config.TopOut = kwx.TopOut;
                config.ChoicePiao = kwx.ChoicePiao;

                config.Play.Clear();
                if (kwx.Play != null)
                {
                    config.Play.AddRange(kwx.Play);
                }

                config.ReadyStateList.Clear();
                if (kwx.ReadyState != null)
                {
                    config.ReadyStateList.AddRange(kwx.ReadyState);
                }
            }

        }
    }
    #endregion

    #region 血流成河配置恢复方法
    /// <summary>
    /// 设置血流成河Toggle为选中状态
    /// </summary>
    private void SetXLCHToggle()
    {
        // 先取消其他Toggle的选中状态
        for (int i = 0; i < varContent.transform.childCount; i++)
        {
            var childToggle = varContent.transform.GetChild(i).GetComponent<Toggle>();
            if (childToggle != null)
            {
                childToggle.isOn = false;
            }
        }

        // 直接设置血流成河Toggle为选中状态
        var xlchToggle = varContent.transform.Find("血流成河")?.GetComponent<Toggle>();
        if (xlchToggle != null)
        {
            if (xlchToggle.isOn)
            {
                OnMjTypeChanged("血流成河");
            }
            else
            {
                xlchToggle.isOn = true;
            }
        }
    }

    /// <summary>
    /// 根据楼层数据设置血流成河配置
    /// </summary>
    /// <param name="floorData">楼层数据</param>
    private void SetXLCHConfigFromFloorData(Msg_Floor floorData)
    {
        GF.LogInfo_wl("设置血流成河配置从楼层数据", floorData.ToString());

        Transform contentTransform = varGameRule.transform.Find("血流成河/rule/Viewport/Content");
        if (contentTransform == null)
        {
            return;
        }

        GameObject content = contentTransform.gameObject;
        if (content == null || floorData.MjConfig.XlConfig == null)
        {
            return;
        }
        // 设置通用配置 禁用语音和禁用打字
        SetToggleByName(content, "gaoji", "jinyongyuying", floorData.Config.ForbidVoice);
        SetToggleByName(content, "gaoji", "jinyongdazi", floorData.Config.Forbid);
        // 设置基础配置
        SetToggleByName(content, "renshu", floorData.Config.PlayerNum.ToString(), true);
        SetToggleByName(content, "jushu", floorData.Config.PlayerTime.ToString(), true);
        // 设置血流成河特有配置
        SetToggleByName(content, "fanshu", floorData.MjConfig.XlConfig.Fan.ToString(), true); // 番型
        SetToggleByName(content, "piao1", floorData.MjConfig.XlConfig.Piao.ToString(), true); // 飘
        SetToggleByName(content, "piao2", floorData.MjConfig.XlConfig.Piao.ToString(), true); // 飘
        SetToggleByName(content, "piao3", floorData.MjConfig.XlConfig.Piao.ToString(), true); // 飘
        SetToggleByName(content, "wanfa", floorData.MjConfig.XlConfig.ChangeCard.ToString(), true); // 换牌

        // 【修复】先重置所有复选框为 false，防止旧选项残留
        ResetAllTogglesInGroup(content, "kexuan", false);

        // 设置复选框配置
        foreach (var checkOption in floorData.MjConfig.XlConfig.Check)
        {
            SetToggleByName(content, "kexuan", checkOption.ToString(), true);
        }

        // 设置底分
        UpdateAddSubUI(content, "difen", "BaseCoin", floorData.Config.BaseCoin);

        // 设置抽水相关配置
        SetRateConfig(floorData, contentTransform, content);
        // 应用人数规则限制
        var config = mJConfigManager.GetConfig("血流成河") as MJConfigManager.XLCHConfigData;
        if (config != null)
        {
            // 同步从floorData加载的配置到内存config
            config.PeopleNum = floorData.Config.PlayerNum;
            config.PlayNum = (int)floorData.Config.PlayerTime;
            config.BaseCoin = floorData.Config.BaseCoin;
            config.ForbidVoice = floorData.Config.ForbidVoice;
            config.Forbid = floorData.Config.Forbid;

            if (floorData.MjConfig != null && floorData.MjConfig.XlConfig != null)
            {
                var xl = floorData.MjConfig.XlConfig;
                config.Fan = xl.Fan;
                config.Piao = xl.Piao;
                config.ChangeCard = xl.ChangeCard;

                config.Check.Clear();
                if (xl.Check != null)
                {
                    config.Check.AddRange(xl.Check);
                }
            }

        }
    }
    #endregion
    /// <summary>
    /// 通过方法名调用Toggle相关的配置方法
    /// 适用于Unity界面通过字符串参数调用Toggle方法
    /// </summary>
    /// <param name="methodName">方法名称</param>
    public void SetToggleConfigByName(string methodName)
    {
        switch (methodName)
        {
            case "SetCheckByName":
                configSetter.SetCheckByName();
                break;
            case "SetOpenDouble":
                configSetter.SetOpenDouble();
                break;
            case "SetWatchByToggle":
                configSetter.SetWatchByToggle();
                break;
            case "SetReadyStateToggle":
                configSetter.SetReadyStateToggle();
                break;
            case "SetVoiceToggle":
                configSetter.SetVoiceToggle();
                break;
            case "SetChatToggle":
                configSetter.SetChatToggle();
                break;
            case "SetRateToggle":
                configSetter.SetRateToggle();
                break;
            case "SetIpLimitToggle":
                configSetter.SetIpLimitToggle();
                break;
            case "SetGpsLimitToggle":
                configSetter.SetGpsLimitToggle();
                break;

            // 卡五星专用Toggle方法
            case "SetPlayByName":
                configSetter.SetPlayByName();
                break;
            case "SetReadyStateByName":
                configSetter.SetReadyStateByName();
                break;
            //血流专用Toggle方法
            case "SetCheckByName_XLCH":
                configSetter.SetCheckByName_XLCH();
                break;
            default:
                break;
        }
    }
    ///    /// <summary>
    /// 创建个人房间
    /// </summary>
    /// <summary>
    /// 创建个人房间
    /// </summary>
    public void CreatMJRoom()
    {
        Msg_CreateDeskRq req = MessagePool.Instance.Fetch<Msg_CreateDeskRq>();
        if (req == null) return;

        // 获取基础配置
        Desk_BaseConfig desk_BaseConfig = GetDesk_BaseConfig();
        if (desk_BaseConfig == null) return;

        req.Config = desk_BaseConfig;
        req.Config.DeskType = DeskType.Simple;

        // 根据当前玩法类型设置具体配置
        string mjType = GetCurrentMjType();
        if (mjType == MjGameType.PAO_DE_KUAI)
        {
            req.MethodType = MethodType.RunFast;
            req.RunFastConfig = mJConfigManager.GenerateRunFastConfig();
            if (req.RunFastConfig == null)
            {
                GF.UI.ShowToast($"生成{MjGameType.PAO_DE_KUAI}配置失败");
                return;
            }
        }
        else
        {
            req.MethodType = MethodType.MjSimple;
            req.MjConfig = mJConfigManager.GenerateMJConfig(mjType);
            if (req.MjConfig == null)
            {
                GF.UI.ShowToast("生成麻将配置失败");
                return;
            }
        }
        //如果是卡五星和血流成河，跳过请求
        if (mjType == MjGameType.KA_WU_XING || mjType == MjGameType.XUE_LIU_CHENG_HE)
        {
            GF.UI.ShowToast("该游戏类型暂未开放，敬请期待！");
            return;
        }
        // 发送请求
        MJHomeProcedure.CreatMJRoom(req);
    }
}


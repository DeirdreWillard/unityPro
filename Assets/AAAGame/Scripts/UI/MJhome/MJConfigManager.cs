using System.Collections.Generic;
using UnityEngine;
using NetMsg;
using GameFramework;

/// <summary>
/// 麻将配置管理器
/// 负责管理各种麻将玩法的配置数据和默认值
/// </summary>
public partial class MJConfigManager
{
    #region 单例模式
    private static MJConfigManager _instance;
    public static MJConfigManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new MJConfigManager();
            }
            return _instance;
        }
    }
    #endregion

    #region 配置结构定义
    /// <summary>
    /// 麻将玩法配置基类
    /// </summary>
    public abstract class BaseMJConfig
    {
        public int PeopleNum { get; set; } = 4;
        public int PlayNum { get; set; } = 8;
        public bool ForbidVoice { get; set; } = false; // 禁止语音
        public bool Forbid { get; set; } = false; // 禁止聊天
        public bool openRate { get; set; } = false; // 抽水 0 不抽水 1 抽水
        public int RateType { get; set; } = 0; //  1对局结束抽大赢家 2 每局抽每个人 3 每局抽大赢家
        public int Rate { get; set; } = 10; // 抽水比例 10-50(步进10) 后端除以1000后为1%-5% UI显示除以10
        public int MinRate { get; set; } = 1; // 最低抽水  1-10
        public float BaseCoin { get; set; } = 1.0f; // 底分配置
        public int OfflineClick { get; set; } = 0; // 离线踢人 0 不踢人
        public int DismissTimes { get; set; } = 0; // 解散次数 0 不限制
        public int ApplyDismissTime { get; set; } = 5; // 申请解散时间 默认5分钟
        public int OffDismissTime { get; set; } = 720; // 离线解散时间默认12小时
        public int ReadyState { get; set; } = 0; // 1:入桌自动准备
        public int Watch { get; set; } = 0; // 1：允许旁观
        public int Tuoguan { get; set; } = 0; // 游戏托管 0:不托管 1:允许托管 2:大局托管
        public bool IpLimit { get; set; } = false; // IP限制 默认关闭
        public bool GpsLimit { get; set; } = false; // GPS限制 默认关闭
        public abstract string ConfigName { get; }
        public abstract NetMsg.MJMethod GetMJMethod();
        public abstract bool ValidateConfig();
        public abstract void ApplyDefaultValues();
    }

    /// <summary>
    /// 仙桃晃晃配置
    /// </summary>
    public class XTHHConfigData : BaseMJConfig
    {
        public override string ConfigName => "仙桃晃晃";
        // 仙桃晃晃特有配置
        public int LaiPlay { get; set; } = 1; // 1:1癞到底 2:土豪必掷
        public int PiaoLai { get; set; } = 1; // 1:飘癞有奖 2:飘赖无奖
        public List<int> Check { get; set; } = new List<int>(); // 1:杠不随飘 2：4赖胡牌不加倍 3：黑夹黑
        public int LockCard { get; set; } = 1; // 1:不锁牌 2：2赖锁牌 3：3癞锁牌 4:4癞锁牌
        public int GangKai { get; set; } = 1; // 1:杠开翻倍 2:杠开不翻倍
        public int Zhuo { get; set; } = 1; // 捉铳 1：捉铳可胡 2:捉铳不可胡
        public int OpenDouble { get; set; } = 0; // 1：解锁翻倍
        public int MaxTime { get; set; } = 0; // 封顶倍数 0 不封顶

        // 配置数组现在由ConfigArrays统一管理

        public override NetMsg.MJMethod GetMJMethod()
        {
            return NetMsg.MJMethod.Huanghuang;
        }

        public override bool ValidateConfig()
        {
            if (LaiPlay < 1 || LaiPlay > 2) return false;
            if (PiaoLai < 1 || PiaoLai > 2) return false;
            if (LockCard < 1 || LockCard > 4) return false;
            if (PeopleNum < 2 || PeopleNum > 4) return false;
            if (PlayNum <= 0) return false;
            return true;
        }

        public override void ApplyDefaultValues()
        {
            PeopleNum = 4;
            PlayNum = 8;
            LaiPlay = 1;
            PiaoLai = 1;
            Check.Clear();
            LockCard = 1;
            GangKai = 1;
            Zhuo = 1;
            OpenDouble = 0;
            MaxTime = 0;
            Tuoguan = 0;
            OfflineClick = 0;
            DismissTimes = 0;
            ApplyDismissTime = 5;
            OffDismissTime = 720;
            ReadyState = 0;
            Watch = 0;
            openRate = false;
            BaseCoin = 0.5f;
            IpLimit = false;
            GpsLimit = false;
        }
        /// <summary>
        /// 转换为protobuf配置
        /// </summary>
        public NetMsg.XTHH_Config ToProtobufConfig()
        {
            var config = new NetMsg.XTHH_Config();
            config.LaiPlay = LaiPlay;
            config.PiaoLai = PiaoLai;
            config.Check.AddRange(Check);
            config.LockCard = LockCard;
            config.GangKai = GangKai;
            config.Zhuo = Zhuo;
            config.OpenDouble = OpenDouble;
            config.MaxTime = MaxTime;
            config.Tuoguan = Tuoguan;
            config.OfflineClick = OfflineClick;
            config.DismissTimes = DismissTimes;
            config.ApplyDismissTime = ApplyDismissTime;
            config.OffDismissTime = OffDismissTime;
            config.ReadyState = ReadyState;
            config.Watch = Watch;

            GF.LogInfo_wl($"生成仙桃晃晃配置 - MaxTime: {MaxTime}");
            return config;
        }
    }

    /// <summary>
    /// 大众麻将配置
    /// </summary>
    public class CommonMJConfigData : BaseMJConfig
    {
        public override string ConfigName => "大众麻将";

        // 大众麻将特有配置（暂时为空，后续扩展）

        public override NetMsg.MJMethod GetMJMethod()
        {
            return NetMsg.MJMethod.Dazhong;
        }

        public override bool ValidateConfig()
        {
            if (PeopleNum < 2 || PeopleNum > 4) return false;
            if (PlayNum <= 0) return false;
            return true;
        }

        public override void ApplyDefaultValues()
        {
            PeopleNum = 4;
            PlayNum = 8;
        }
    }

    /// <summary>
    /// 卡五星配置
    /// </summary>
    public class KWXConfigData : BaseMJConfig
    {
        public override string ConfigName => "卡五星";

        // 卡五星特有配置
        public int BuyHorse { get; set; } = 1; // 买马 1:亮倒自摸买马 2:自摸买马 0:不买马
        public int TopOut { get; set; } = 8; // 封顶 8/16/32
        public int ChoicePiao { get; set; } = 1; // 选漂模式 1:每局选漂 2:首局定漂 3:不漂 4:固定飘1 5:固定飘2 6:固定飘3
        public List<int> Play { get; set; } = new List<int>(); // 玩法复选框
        // 1:杠上花*4 2:卡五星*4 3:碰碰胡*4 4:小三元七对*8 5:大三元七对*16 
        // 6:海底捞/炮*2 7:杠上炮翻番 8:对亮翻番 9:全频道 10:数坎 
        // 11:双规 12:两番起胡 13:过手碰 14:换三张
        public List<int> ReadyStateList { get; set; } = new List<int>(); // 准备状态 1:入桌自动准备 2:小结算自动准备

        public override NetMsg.MJMethod GetMJMethod()
        {
            // TODO: 等待后端添加 MJMethod.Kawuxing 枚举值
            return NetMsg.MJMethod.Kwx; // 临时使用，待后端添加枚举
        }

        public override bool ValidateConfig()
        {
            if (BuyHorse < 0 || BuyHorse > 2) return false;
            if (TopOut != 8 && TopOut != 16 && TopOut != 32) return false;
            if (ChoicePiao < 1 || ChoicePiao > 6) return false;
            if (PeopleNum < 2 || PeopleNum > 4) return false;
            if (PlayNum <= 0) return false;
            return true;
        }

        public override void ApplyDefaultValues()
        {
            PeopleNum = 3;
            PlayNum = 2;
            BuyHorse = 0;
            TopOut = 8;
            ChoicePiao = 1;
            Play.Clear();
            ReadyStateList.Clear();
            OfflineClick = 0;
            DismissTimes = 0;
            Tuoguan = 0;
            ApplyDismissTime = 5;
            OffDismissTime = 720;
            ReadyState = 0;
            Watch = 0;
            openRate = false;
            BaseCoin = 0.5f;
            IpLimit = false;
            GpsLimit = false;
        }

        /// <summary>
        /// 转换为protobuf配置
        /// </summary>
        public NetMsg.KWX_Config ToProtobufConfig()
        {
            var config = new NetMsg.KWX_Config
            {
                BuyHorse = BuyHorse,
                TopOut = TopOut,
                ChoicePiao = ChoicePiao
            };

            // 添加玩法复选框配置
            if (Play != null && Play.Count > 0)
            {
                config.Play.AddRange(Play);
            }

            // 添加准备状态配置
            if (ReadyStateList != null && ReadyStateList.Count > 0)
            {
                config.ReadyState.AddRange(ReadyStateList);
            }
            
            GF.LogInfo_wl($"生成卡五星配置 - BuyHorse: {BuyHorse}, TopOut: {TopOut}, ChoicePiao: {ChoicePiao}, Play: [{string.Join(",", Play)}], ReadyState: [{string.Join(",", ReadyStateList)}]");
            return config;
        }

    }

    /// <summary>
    /// 血流成河配置
    /// </summary>
    public class XLCHConfigData : BaseMJConfig
    {
        public override string ConfigName => "血流成河";

        // 血流成河特有配置
        public int Fan { get; set; } = 1; // 1:2,4,6 2:3,6,9 3:4,8,12 屁胡 清一色/蹦蹦胡 清一色+蹦蹦胡(aaabbbcccddee)
        public int Piao { get; set; } = 0; // 0:自由飘 1~5:定漂1~5 6:不飘
        public int ChangeCard { get; set; } = 0; // 0默认换三张 1:不换牌
        public List<int> Check { get; set; } = new List<int>(); // 1:过手碰 2：胡后可点杠 3:甩3张 4:接炮算门清(没有吃碰杠)

        public override NetMsg.MJMethod GetMJMethod()
        {
            return NetMsg.MJMethod.Xl; // 血流麻将
        }

        public override bool ValidateConfig()
        {
            if (Fan < 1 || Fan > 3) return false;
            if (Piao < 0 || Piao > 6) return false;
            if (ChangeCard < 0 || ChangeCard > 1) return false;
            if (PeopleNum < 2 || PeopleNum > 4) return false;
            if (PlayNum <= 0) return false;
            return true;
        }

        public override void ApplyDefaultValues()
        {
            PeopleNum = 4;
            PlayNum = 3;
            Fan = 1;
            Piao = 0;
            ChangeCard = 0;
            Check.Clear();
            Tuoguan = 0;
            OfflineClick = 0;
            DismissTimes = 0;
            ApplyDismissTime = 5;
            OffDismissTime = 720;
            ReadyState = 0;
            Watch = 0;
            openRate = false;
            BaseCoin = 0.5f;
            IpLimit = false;
            GpsLimit = false;
        }

        /// <summary>
        /// 转换为protobuf配置
        /// </summary>
        public NetMsg.XL_Config ToProtobufConfig() 
        {
            
            var config = new NetMsg.XL_Config();
            config.Fan = Fan;
            config.Piao = Piao;
            config.ChangeCard = ChangeCard;
            config.Check.AddRange(Check);

            GF.LogInfo_wl($"生成血流成河配置 - Fan: {Fan}, Piao: {Piao}, ChangeCard: {ChangeCard}, Check: [{string.Join(",", Check)}]");
            return config;
        }
    }

    /// <summary>
    /// 跑的快配置
    /// </summary>
    public class PDKConfigData : BaseMJConfig
    {
        public override string ConfigName => "跑的快";

        // 跑的快特有配置
        public int CardCount { get; set; } = 16; // 牌数 15张或16张
        public int FirstCard { get; set; } = 1; // 首出牌权 
        public List<int> Check { get; set; } = new List<int>(); // 特殊规则复选框
        public int MaxTime { get; set; } = 0; // 封顶倍数 0 不封顶
        

        public int PlayGame { get; set; } = 0; //玩法 1有大必压 2可不压
       public int ChuPai { get; set; } = 1; //出牌方式 1必带最小 2:任意出牌 
        public override NetMsg.MJMethod GetMJMethod()
        {
            // 注意：跑的快实际使用 MethodType.RunFast，不使用 MJMethod
            // 此方法仅为满足基类抽象方法要求，实际创建房间时使用 GenerateRunFastConfig()
            return NetMsg.MJMethod.Dazhong; // 返回默认值，实际不使用
        }

        public override bool ValidateConfig()
        {
            if (CardCount != 15 && CardCount != 16) return false;

            if (FirstCard < 0 || FirstCard > 2) return false;
            if (PeopleNum < 2 || PeopleNum > 4) return false;
            if (PlayNum <= 0) return false;
            return true;
        }

        public override void ApplyDefaultValues()
        {
            PeopleNum = 3;
            PlayNum = 3;
            CardCount = 16;
            FirstCard = 0; // 默认：首局最小牌先出
            Check.Clear();
            MaxTime = 0;
            Tuoguan = 0;
            OfflineClick = 0;
            DismissTimes = 0;
            ApplyDismissTime = 5;
            OffDismissTime = 720;
            ReadyState = 0;
            Watch = 0;
            openRate = false;
            BaseCoin = 0.5f;
            PlayGame = 1; // 默认：有大必压
            ChuPai = 1; // 默认：必带最小
            IpLimit = false;
            GpsLimit = false;
        }

        /// <summary>
        /// 转换为protobuf配置
        /// RunFastConfig 配置说明：
        /// firstRun: 0=首局最小牌先出 1=每局最小牌先出 2=首局黑桃三先出
        /// play: 1=有大必压 2=可不压
        /// check: 玩法选择数组
        ///   1:4带2 2:4带3 3:炸弹不能拆 4:AAA为炸弹 5:红桃10扎鸟 
        ///   6:炸弹不翻倍 7:炸弹被压无分 8:三带任意出 9:最后三张可少带接完 
        ///   10:抢庄 11:可反的 12:快速过牌 13:切牌 14:显示剩牌 
        ///   15:记牌器道具 16:加倍 17:提前亮牌
        /// dismissTimes: 解散次数限制 (0=不限制)
        /// applyDismissTime: 申请解散倒计时时长（分钟）
        /// </summary>
        public NetMsg.RunFastConfig ToProtobufConfig()
        {
            var config = new NetMsg.RunFastConfig
            {
                FirstRun = FirstCard,
                Play = PlayGame,
                DismissTimes = DismissTimes,
                ApplyDismissTime = ApplyDismissTime,
                WithMin = ChuPai
            };

            // 添加复选框配置
            if (Check != null && Check.Count > 0)
            {
                config.Check.AddRange(Check);
            }
           
            GF.LogInfo_wl($"[PDK] 生成跑的快配置 - FirstRun: {FirstCard}, Play: {PlayGame}, WithMin: {ChuPai}, Check: [{string.Join(",", Check)}], DismissTimes: {DismissTimes}, ApplyDismissTime: {ApplyDismissTime}分钟");
            return config;
        }
    }
    #endregion


    #region 配置管理方法
    private Dictionary<string, System.Type> _configTypes;
    private Dictionary<string, BaseMJConfig> _currentConfigs;

    private MJConfigManager()
    {
        InitializeConfigTypes();
        _currentConfigs = new Dictionary<string, BaseMJConfig>();
    }

    /// <summary>
    /// 初始化配置类型映射
    /// </summary>
    private void InitializeConfigTypes()
    {
        _configTypes = new Dictionary<string, System.Type>
        {
            { "仙桃晃晃", typeof(XTHHConfigData) },
            { "大众麻将", typeof(CommonMJConfigData) },
            { "卡五星", typeof(KWXConfigData) },
            { "血流成河", typeof(XLCHConfigData) },
            { "跑的快", typeof(PDKConfigData) }
        };
    }

    /// <summary>
    /// 获取指定玩法的配置实例
    /// </summary>
    /// <param name="mjTypeName">麻将玩法名称</param>
    /// <returns>配置实例</returns>
    public BaseMJConfig GetConfig(string mjTypeName)
    {
        if (!_currentConfigs.ContainsKey(mjTypeName))
        {
            CreateConfig(mjTypeName);
        }
        return _currentConfigs[mjTypeName];
    }

    /// <summary>
    /// 创建配置实例
    /// </summary>
    /// <param name="mjTypeName">麻将玩法名称</param>
    private void CreateConfig(string mjTypeName)
    {
        if (_configTypes.TryGetValue(mjTypeName, out System.Type configType))
        {
            var config = System.Activator.CreateInstance(configType) as BaseMJConfig;
            config.ApplyDefaultValues();
            _currentConfigs[mjTypeName] = config;
        }
        else
        {
            GF.LogWarning($"未找到麻将玩法配置类型: {mjTypeName}");
        }
    }

    /// <summary>
    /// 获取支持的麻将玩法列表
    /// </summary>
    /// <returns>玩法名称列表</returns>
    public string[] GetSupportedMJTypes()
    {
        var types = new string[_configTypes.Count];
        _configTypes.Keys.CopyTo(types, 0);
        return types;
    }

    /// <summary>
    /// 生成麻将配置
    /// 注意：跑的快不使用此方法，请使用 GenerateRunFastConfig()
    /// </summary>
    /// <param name="mjTypeName">麻将玩法名称</param>
    /// <returns>protobuf麻将配置</returns>
    public NetMsg.MJConfig GenerateMJConfig(string mjTypeName)
    {
        var mjConfig = new NetMsg.MJConfig();
        var config = GetConfig(mjTypeName);

        mjConfig.MjMethod = config.GetMJMethod();

        // 根据不同玩法设置特定配置
        switch (mjTypeName)
        {
            case "仙桃晃晃":
                var xthhConfig = config as XTHHConfigData;
                mjConfig.Xthh = xthhConfig.ToProtobufConfig();
                break;
            case "大众麻将":
                // 暂时没有特定配置
                break;
            case "卡五星":
                var kwxConfig = config as KWXConfigData;
                mjConfig.Kwx = kwxConfig.ToProtobufConfig();
                GF.LogInfo_wl("卡五星配置已生成");
                break;
            case "血流成河":
                var xlchConfig = config as XLCHConfigData;
                mjConfig.XlConfig = xlchConfig.ToProtobufConfig();
                GF.LogInfo_wl("血流成河配置已生成");
                break;
            case "跑的快":
                GenerateRunFastConfig();
                break;
        }
        return mjConfig;
    }

    /// <summary>
    /// 生成跑的快配置
    /// </summary>
    /// <returns>protobuf跑的快配置</returns>
    public NetMsg.RunFastConfig GenerateRunFastConfig()
    {
        var config = GetConfig("跑的快") as PDKConfigData;
        if (config == null)
        {
            GF.LogError("获取跑的快配置失败");
            return new NetMsg.RunFastConfig();
        }
        return config.ToProtobufConfig();
    }

    /// <summary>
    /// 验证指定玩法的配置
    /// </summary>
    /// <param name="mjTypeName">麻将玩法名称</param>
    /// <returns>验证结果</returns>
    public bool ValidateConfig(string mjTypeName)
    {
        var config = GetConfig(mjTypeName);
        return config.ValidateConfig();
    }

    /// <summary>
    /// 重置指定玩法的配置为默认值
    /// </summary>
    /// <param name="mjTypeName">麻将玩法名称</param>
    public void ResetConfig(string mjTypeName)
    {
        var config = GetConfig(mjTypeName);
        config.ApplyDefaultValues();
        GF.LogInfo($"已重置 {mjTypeName} 配置为默认值");
    }

    /// <summary>
    /// 获取配置摘要信息
    /// </summary>
    /// <param name="mjTypeName">麻将玩法名称</param>
    /// <returns>配置摘要</returns>
    public string GetConfigSummary(string mjTypeName)
    {
        var config = GetConfig(mjTypeName);
        var summary = $"麻将类型: {config.ConfigName}\n人数: {config.PeopleNum}\n局数: {config.PlayNum}\n";

        if (config is XTHHConfigData xthhConfig)
        {
            summary += $"癞子玩法: {xthhConfig.LaiPlay}\n";
            summary += $"飘癞: {xthhConfig.PiaoLai}\n";
            summary += $"复选配置: [{string.Join(",", xthhConfig.Check)}]\n";
            summary += $"锁牌: {xthhConfig.LockCard}\n";
            summary += $"杠开: {xthhConfig.GangKai}\n";
            summary += $"捉铳: {xthhConfig.Zhuo}\n";
            summary += $"托管: {xthhConfig.Tuoguan}\n";
        }
        else if (config is KWXConfigData kwxConfig)
        {
            summary += $"买马: {kwxConfig.BuyHorse}\n";
            summary += $"封顶: {kwxConfig.TopOut}\n";
            summary += $"选漂模式: {kwxConfig.ChoicePiao}\n";
            summary += $"玩法配置: [{string.Join(",", kwxConfig.Play)}]\n";
            summary += $"准备状态: [{string.Join(",", kwxConfig.ReadyStateList)}]\n";
        }
        else if (config is XLCHConfigData xlchConfig)
        {
            summary += $"番数: {xlchConfig.Fan}\n";
            summary += $"飘: {xlchConfig.Piao}\n";
            summary += $"换三张: {xlchConfig.ChangeCard}\n";
            summary += $"复选配置: [{string.Join(",", xlchConfig.Check)}]\n";
            summary += $"托管: {xlchConfig.Tuoguan}\n";
        }
        else if (config is PDKConfigData pdkConfig)
        {
            summary += $"牌数: {pdkConfig.CardCount}\n";

            summary += $"首出牌权: {pdkConfig.FirstCard}\n";
            summary += $"复选配置: [{string.Join(",", pdkConfig.Check)}]\n";
            summary += $"封顶倍数: {pdkConfig.MaxTime}\n";
            summary += $"托管: {pdkConfig.Tuoguan}\n";
        }

        return summary;
    }
    #endregion

    #region 配置字典方法
    /// <summary>
    /// 将配置实例转换为字典格式
    /// </summary>
    /// <param name="mjTypeName">麻将玩法名称</param>
    /// <returns>配置字典</returns>
    public Dictionary<string, object> GetConfigAsDictionary(string mjTypeName)
    {
        var config = GetConfig(mjTypeName);
        var configDict = new Dictionary<string, object>
        {
            {"peopleNum", config.PeopleNum},
            {"playNum", config.PlayNum},
            {"baseCoin", config.BaseCoin}
        };

        // 添加玩法特有配置
        if (config is XTHHConfigData xthhConfig)
        {
            configDict.Add("laiPlay", xthhConfig.LaiPlay);
            configDict.Add("piaoLai", xthhConfig.PiaoLai);
            configDict.Add("check", new List<int>(xthhConfig.Check));
            configDict.Add("lockCard", xthhConfig.LockCard);
            configDict.Add("gangKai", xthhConfig.GangKai);
            configDict.Add("zhuo", xthhConfig.Zhuo);
            configDict.Add("openDouble", xthhConfig.OpenDouble);
            configDict.Add("maxTime", xthhConfig.MaxTime);
            configDict.Add("tuoguan", xthhConfig.Tuoguan);
            configDict.Add("offlineClick", xthhConfig.OfflineClick);
            configDict.Add("dismissTimes", xthhConfig.DismissTimes);
            configDict.Add("applyDismissTime", xthhConfig.ApplyDismissTime);
            configDict.Add("offDismissTime", xthhConfig.OffDismissTime);
            configDict.Add("readyState", xthhConfig.ReadyState);
            configDict.Add("watch", xthhConfig.Watch);
        }
        else if (config is KWXConfigData kwxConfig)
        {
            configDict.Add("buyHorse", kwxConfig.BuyHorse);
            configDict.Add("topOut", kwxConfig.TopOut);
            configDict.Add("choicePiao", kwxConfig.ChoicePiao);
            configDict.Add("play", new List<int>(kwxConfig.Play));
            configDict.Add("readyStateList", new List<int>(kwxConfig.ReadyStateList));
            configDict.Add("offlineClick", kwxConfig.OfflineClick);
            configDict.Add("dismissTimes", kwxConfig.DismissTimes);
            configDict.Add("applyDismissTime", kwxConfig.ApplyDismissTime);
            configDict.Add("offDismissTime", kwxConfig.OffDismissTime);
            configDict.Add("readyState", kwxConfig.ReadyState);
            configDict.Add("watch", kwxConfig.Watch);
        }
        else if (config is XLCHConfigData xlchConfig)
        {
            configDict.Add("fan", xlchConfig.Fan);
            configDict.Add("piao", xlchConfig.Piao);
            configDict.Add("changeCard", xlchConfig.ChangeCard);
            configDict.Add("check", new List<int>(xlchConfig.Check));
            configDict.Add("tuoguan", xlchConfig.Tuoguan);
            configDict.Add("offlineClick", xlchConfig.OfflineClick);
            configDict.Add("dismissTimes", xlchConfig.DismissTimes);
            configDict.Add("applyDismissTime", xlchConfig.ApplyDismissTime);
            configDict.Add("offDismissTime", xlchConfig.OffDismissTime);
            configDict.Add("readyState", xlchConfig.ReadyState);
            configDict.Add("watch", xlchConfig.Watch);
        }
        else if (config is PDKConfigData pdkConfig)
        {
            configDict.Add("cardCount", pdkConfig.CardCount);
            configDict.Add("firstCard", pdkConfig.FirstCard);
            configDict.Add("check", new List<int>(pdkConfig.Check));
            configDict.Add("maxTime", pdkConfig.MaxTime);
            configDict.Add("tuoguan", pdkConfig.Tuoguan);
            configDict.Add("offlineClick", pdkConfig.OfflineClick);
            configDict.Add("dismissTimes", pdkConfig.DismissTimes);
            configDict.Add("applyDismissTime", pdkConfig.ApplyDismissTime);
            configDict.Add("offDismissTime", pdkConfig.OffDismissTime);
            configDict.Add("readyState", pdkConfig.ReadyState);
            configDict.Add("watch", pdkConfig.Watch);
        }

        return configDict;
    }

    /// <summary>
    /// 获取默认配置的字典形式
    /// </summary>
    /// <param name="mjTypeName">麻将玩法名称</param>
    /// <returns>默认配置字典</returns>
    public Dictionary<string, object> GetDefaultConfigAsDictionary(string mjTypeName)
    {
        var config = GetConfig(mjTypeName);
        config.ApplyDefaultValues();  // 确保是默认值
        return GetConfigAsDictionary(mjTypeName);
    }
    #endregion
}

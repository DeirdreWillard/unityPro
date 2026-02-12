using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 统一管理所有麻将配置的可选项数组
/// 避免在各个配置类中重复定义数组
/// </summary>
public static class ConfigArrays
{
    #region 通用配置数组（所有游戏类型都可使用）
    /// <summary>
    /// 底分配置选项（所有玩法通用）
    /// </summary>
    public static readonly float[] BaseCoinList = { 0.5f, 1f, 2f, 2.5f, 3f, 5f,10f,20f};

    /// <summary>
    /// 抽水比例配置选项（10-50，步进10，后端除以1000后为1%-5%，所有玩法通用）
    /// UI显示时需要除以10显示为1-5%
    /// </summary>
    public static readonly int[] RateList = { 10, 20, 30, 40, 50 };

    /// <summary>
    /// 最低抽水配置选项（1-10，所有玩法通用）
    /// </summary>
    public static readonly int[] MinRateList = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    /// <summary>
    /// 封顶倍数配置选项（0表示不封顶，所有玩法通用）
    /// </summary>
    public static readonly int[] FengDingList = {0,32,64,128};
    public static readonly int[] FengDingList_KWX = { 8, 16, 32 };
    /// <summary>
    /// 离线踢人时间配置选项（0表示不踢人，单位：秒，所有玩法通用）
    /// </summary>
    public static readonly int[] OfflineClickList = { 0, 30, 60, 120, 240 };

    /// <summary>
    /// 解散次数配置选项（-1表示不限制，所有玩法通用）
    /// </summary>
    public static readonly int[] DismissTimesList = { -1, 0, 1, 2,3,4,5 };

    /// <summary>
    /// 申请解散时间配置选项（单位：分钟，所有玩法通用）
    /// </summary>
    public static readonly int[] ApplyDismissTimeList = { 1, 2, 3,4,5 };

    /// <summary>
    /// 离线解散时间配置选项（单位：分钟，所有玩法通用）
    /// </summary>
    public static readonly int[] OffDismissTimeList = { 10, 30, 60, 720 };
    #endregion

    #region 仙桃晃晃专用配置数组
    // 仙桃晃晃特有配置可以在这里添加
    #endregion

    #region 大众麻将专用配置数组
    // 后续根据大众麻将具体需求扩展
    #endregion

    #region 卡五星专用配置数组
    /// <summary>
    /// 五星类型配置选项
    /// </summary>
    public static readonly string[] WuXingTypeList = { "软五星", "硬五星" };

    /// <summary>
    /// 胡牌类型配置选项
    /// </summary>
    public static readonly string[] KWXHuTypeList = { "点炮胡", "自摸胡" };

    /// <summary>
    /// 杠类型配置选项
    /// </summary>
    public static readonly string[] GangTypeList = { "明杠", "暗杠", "都有" };
    #endregion

    #region 血流成河专用配置数组
    /// <summary>
    /// 换三张配置选项
    /// </summary>
    public static readonly string[] HuanSanZhangList = { "顺时针", "逆时针", "对家" };

    /// <summary>
    /// 定缺类型配置选项
    /// </summary>
    public static readonly string[] DingQueTypeList = { "随机", "手动" };

    /// <summary>
    /// 胡牌类型配置选项（血流成河）
    /// </summary>
    public static readonly string[] XLCHHuTypeList = { "自摸胡", "点炮也算自摸" };
    #endregion

    #region 跑得快专用配置数组
    /// <summary>
    /// 牌数配置选项
    /// </summary>
    public static readonly int[] CardCountList = { 15, 16 };

    /// <summary>
    /// 炸弹类型配置选项
    /// </summary>
    public static readonly string[] BombTypeList = { "普通炸弹", "火箭炸弹", "都有" };

    /// <summary>
    /// 首出牌权配置选项
    /// </summary>
    public static readonly string[] FirstCardList = { "黑桃3", "随机" };
    #endregion

    #region 配置数组获取方法
    /// <summary>
    /// 根据麻将类型和配置键获取对应的配置数组
    /// </summary>
    /// <param name="mjType">麻将类型</param>
    /// <param name="configKey">配置键</param>
    /// <returns>配置数组</returns>
    public static object[] GetConfigArray(string mjType, string configKey)
    {
        if (string.IsNullOrEmpty(configKey)) return new object[0];

        switch (configKey.ToLower())
        {
            // 通用配置（所有游戏类型都可用）
            case "basecoin":
                return BaseCoinList.Cast<object>().ToArray();
            case "rate":
                return RateList.Cast<object>().ToArray();
            case "minrate":
                return MinRateList.Cast<object>().ToArray();
            case "fengding":
                if (mjType == "卡五星")
                {
                    return FengDingList_KWX.Cast<object>().ToArray();
                }
                return FengDingList.Cast<object>().ToArray();
            case "offlineclick":
                return OfflineClickList.Cast<object>().ToArray();
            case "dismisstimes":
                return DismissTimesList.Cast<object>().ToArray();
            case "applydismisstime":
                return ApplyDismissTimeList.Cast<object>().ToArray();
            case "offdismisstime":
                return OffDismissTimeList.Cast<object>().ToArray();

            // 游戏类型特有配置可以在这里添加
            // 例如：case "特殊配置" when mjType == "某个游戏":
            
            default:
                return new object[0];
        }
    }

    /// <summary>
    /// 根据麻将类型获取所有可配置的项目
    /// </summary>
    /// <param name="mjType">麻将类型</param>
    /// <returns>可配置项目的键列表</returns>
    public static List<string> GetConfigurableKeys(string mjType)
    {
        // 通用配置项（所有游戏类型都支持）
        var keys = new List<string> 
        { 
            "basecoin", 
            "rate", 
            "minrate",
            "fengding", 
            "offlineclick", 
            "dismisstimes",
            "applydismisstime", 
            "offdismisstime"
        };

        // 根据不同游戏类型添加特有配置项
        switch (mjType)
        {
            case "仙桃晃晃":
                // 仙桃晃晃特有配置可以在这里添加
                break;
            case "大众麻将":
                // 大众麻将特有配置可以在这里添加
                break;
            case "卡五星":
                // 卡五星特有配置可以在这里添加
                break;
            case "血流成河":
                // 血流成河特有配置可以在这里添加
                break;
            case "跑的快":
                // 跑的快特有配置可以在这里添加
                break;
        }

        return keys;
    }
    #endregion
}

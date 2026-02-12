using System.Collections.Generic;

/// <summary>
/// 麻将UI路径配置类
/// 集中管理所有麻将类型的UI路径，便于维护和修改
/// </summary>
public static class MJUIPathConfig
{
    #region 基础路径配置

    /// <summary>
    /// 获取麻将类型的内容根路径
    /// </summary>
    public static string GetContentPath(string mjType)
    {
        return $"{mjType}/rule/Viewport/Content";
    }

    #endregion

    #region 通用配置项路径

    /// <summary>
    /// 通用配置项路径（所有麻将类型共用）
    /// </summary>
    public static class CommonPaths
    {
        // 底分
        public const string BaseCoinAdd = "difen/AddFlase/AddTrue";
        public const string BaseCoinSub = "difen/SubtractFlase/SubtractTrue";

        // 封顶
        public const string FengDingAdd = "fengding/AddFlase/AddTrue";
        public const string FengDingSub = "fengding/SubtractFlase/SubtractTrue";

        // 离线踢人时间
        public const string OfflineClickAdd = "lixiantiren/AddFlase/AddTrue";
        public const string OfflineClickSub = "lixiantiren/SubtractFlase/SubtractTrue";

        // 解散次数
        public const string DismissTimesAdd = "jiesancishu/AddFlase/AddTrue";
        public const string DismissTimesSub = "jiesancishu/SubtractFlase/SubtractTrue";

        // 申请解散时间
        public const string ApplyDismissTimeAdd = "shenqingjiesanshijian/AddFlase/AddTrue";
        public const string ApplyDismissTimeSub = "shenqingjiesanshijian/SubtractFlase/SubtractTrue";

        // 离线解散时间
        public const string OffDismissTimeAdd = "lixianjiesanshijian/AddFlase/AddTrue";
        public const string OffDismissTimeSub = "lixianjiesanshijian/SubtractFlase/SubtractTrue";

        // 抽水比例
        public const string RateAdd = "SetRate/AddFlase/AddTrue";
        public const string RateSub = "SetRate/SubtractFlase/SubtractTrue";

        // 最低抽水
        public const string MinRateAdd = "SetRateValue/AddFlase/AddTrue";
        public const string MinRateSub = "SetRateValue/SubtractFlase/SubtractTrue";
    }

    #endregion

    #region 仙桃晃晃特有路径

    public static class XTHHPaths
    {
        // 继承通用路径
        // 这里可以添加仙桃晃晃特有的配置项路径
    }

    #endregion

    #region 大众麻将特有路径

    public static class DZMJPaths
    {
        // 大众麻将特有的配置项路径
    }

    #endregion

    #region 卡五星特有路径

    public static class KWXPaths
    {
        // 卡五星特有的配置项路径
    }

    #endregion

    #region 血流成河特有路径

    public static class XLCHPaths
    {
        // 血流成河特有的配置项路径
    }

    #endregion

    #region 跑得快特有路径

    public static class PDKPaths
    {
        // 跑得快特有的配置项路径
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 获取所有通用配置项的按钮路径对
    /// </summary>
    /// <returns>配置项路径字典</returns>
    public static Dictionary<string, (string addPath, string subPath)> GetCommonButtonPaths()
    {
        return new Dictionary<string, (string, string)>
        {
            { "baseCoin", (CommonPaths.BaseCoinAdd, CommonPaths.BaseCoinSub) },
            { "fengDing", (CommonPaths.FengDingAdd, CommonPaths.FengDingSub) },
            { "offlineClick", (CommonPaths.OfflineClickAdd, CommonPaths.OfflineClickSub) },
            { "dismissTimes", (CommonPaths.DismissTimesAdd, CommonPaths.DismissTimesSub) },
            { "applyDismissTime", (CommonPaths.ApplyDismissTimeAdd, CommonPaths.ApplyDismissTimeSub) },
            { "offDismissTime", (CommonPaths.OffDismissTimeAdd, CommonPaths.OffDismissTimeSub) },
            { "rate", (CommonPaths.RateAdd, CommonPaths.RateSub) },
            { "minRate", (CommonPaths.MinRateAdd, CommonPaths.MinRateSub) }
        };
    }

    #endregion
}

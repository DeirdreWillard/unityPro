using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NetMsg;
using UnityEngine;
using System;

public partial class CreatMjPopup
{
    // 配置管理器实例
    private MJConfigManager mjConfigManager = MJConfigManager.Instance;
    // 麻将类型名称数组
    private string[] toggleNames = { "卡五星", "血流成河", "跑的快" ,"仙桃晃晃"};
    #region 数值格式化方法
    #endregion
    // <summary>
    /// 更新配置值
    /// </summary>
    /// <param name="configKey">配置键</param>
    /// <param name="value">配置值</param>
    private void UpdateConfigValue(string configKey, object value)
    {
        var currentConfig = GetCurrentConfig();

        // 根据配置键更新对应的配置值
        switch (configKey)
        {
            case "baseCoin":
                if (value is float baseCoinValue)
                    currentConfig.BaseCoin = baseCoinValue;
                break;
            case "peopleNum":
                if (value is int peopleNumValue)
                    currentConfig.PeopleNum = peopleNumValue;
                break;
            case "playNum":
                if (value is int playNumValue)
                    currentConfig.PlayNum = playNumValue;
                break;
            case "rate":
                if (value is int rateValue)
                    currentConfig.Rate = rateValue;
                break;
            case "minRate":
                if (value is int minRateValue)
                    currentConfig.MinRate = minRateValue;
                break;
            case "applyDismissTime":
                if (value is int applyDismissTimeValue)
                {
                    currentConfig.ApplyDismissTime = applyDismissTimeValue;
                }
                break;
        }

        // 仙桃晃晃特有配置
        if (currentConfig is MJConfigManager.XTHHConfigData xthhConfig)
        {
            switch (configKey)
            {
                case "fengDing":
                    if (value is int fengDingValue)
                    {
                        xthhConfig.MaxTime = fengDingValue;
                    }
                    break;
                case "offlineClick":
                    if (value is int offlineClickValue)
                        xthhConfig.OfflineClick = offlineClickValue;
                    break;
                case "dismissTimes":
                    if (value is int dismissTimesValue)
                        xthhConfig.DismissTimes = dismissTimesValue;
                    break;

                case "offDismissTime":
                    if (value is int offDismissTimeValue)
                        xthhConfig.OffDismissTime = offDismissTimeValue;
                    break;
                case "tuoguan":
                    if (value is int tuoguanValue)
                        xthhConfig.Tuoguan = tuoguanValue;
                    break;
            }
        }

        // 大众麻将特有配置
        else if (currentConfig is MJConfigManager.CommonMJConfigData commonConfig)
        {
            switch (configKey)
            {
                // 暂时没有大众麻将特有配置
                default:
                    break;
            }
        }

        // 卡五星特有配置
        else if (currentConfig is MJConfigManager.KWXConfigData kwxConfig)
        {
            switch (configKey)
            {
                case "fengDing":
                    if (value is int fengDingValue)
                        kwxConfig.TopOut = fengDingValue;
                    break;
                case "offlineClick":
                    if (value is int offlineClickValue)
                        kwxConfig.OfflineClick = offlineClickValue;
                    break;
                case "dismissTimes":
                    if (value is int dismissTimesValue)
                        kwxConfig.DismissTimes = dismissTimesValue;
                    break;

                case "offDismissTime":
                    if (value is int offDismissTimeValue)
                        kwxConfig.OffDismissTime = offDismissTimeValue;
                    break;

            }
        }

        // 血流成河特有配置
        else if (currentConfig is MJConfigManager.XLCHConfigData xlchConfig)
        {
            switch (configKey)
            {
                case "offlineClick":
                    if (value is int offlineClickValue)
                        xlchConfig.OfflineClick = offlineClickValue;
                    break;
                case "dismissTimes":
                    if (value is int dismissTimesValue)
                        xlchConfig.DismissTimes = dismissTimesValue;
                    break;

                case "offDismissTime":
                    if (value is int offDismissTimeValue)
                        xlchConfig.OffDismissTime = offDismissTimeValue;
                    break;
                case "tuoguan":
                    if (value is int tuoguanValue)
                        xlchConfig.Tuoguan = tuoguanValue;
                    break;
            }
        }
        // 跑得快特有配置
        else if (currentConfig is MJConfigManager.PDKConfigData pdkConfig)
        {
            switch (configKey)
            {
                case "fengDing":
                    if (value is int fengDingValue)
                        pdkConfig.MaxTime = fengDingValue;
                    break;
                case "offlineClick":
                    if (value is int offlineClickValue)
                        pdkConfig.OfflineClick = offlineClickValue;
                    break;
                case "dismissTimes":
                    if (value is int dismissTimesValue)
                        pdkConfig.DismissTimes = dismissTimesValue;
                    break;
                case "offDismissTime":
                    if (value is int offDismissTimeValue)
                        pdkConfig.OffDismissTime = offDismissTimeValue;
                    break;
                case "tuoguan":
                    if (value is int tuoguanValue)
                        pdkConfig.Tuoguan = tuoguanValue;
                    break;
            }
        }
    }
    /// <summary>
    /// 格式化显示值
    /// </summary>
    /// <param name="configKey">配置键</param>
    /// <param name="value">值</param>
    /// <returns>格式化后的显示文本</returns>
    private string FormatDisplayValue(string configKey, object value)
    {
        if (value == null) return "";

        return configKey.ToLower() switch
        {
            "basecoin" => value is float floatValue ? floatValue + "分" : Convert.ToSingle(value) + "分",
            "rate" => (Convert.ToInt32(value) / 10) + "%",  // 显示时除以10: 10→1%, 20→2%, 50→5%
            "minrate" => Convert.ToInt32(value) + "",
            "fengding" => Convert.ToInt32(value) == 0 ? "不封顶" : Convert.ToInt32(value) + "倍",
            "offlineclick" => Convert.ToInt32(value) switch
            {
                0 => "不踢人",
                30 => "30秒",
                60 => "1分钟",
                120 => "2分钟",
                240 => "4分钟",
                _ => Convert.ToInt32(value) + "秒"
            },
            "dismisstimes" => Convert.ToInt32(value) == -1 ? "不限制" : Convert.ToInt32(value) + "次",
            "applydismisstime" => Convert.ToInt32(value) + "分钟",
            "offdismisstime" => Convert.ToInt32(value) switch
            {
                10 => "10分钟",
                30 => "30分钟",
                60 => "60分钟",
                720 => "12小时",
                _ => Convert.ToInt32(value) >= 60 && Convert.ToInt32(value) % 60 == 0 ?
                     (Convert.ToInt32(value) / 60) + "小时" : Convert.ToInt32(value) + "分钟"
            },
            "peoplenum" => Convert.ToInt32(value) + "人",
            "playnum" => Convert.ToInt32(value) + "局",
            _ => value.ToString()
        };
    }
    /// <summary>
    /// 尝试解析当前值
    /// </summary>
    /// <param name="configKey">配置键</param>
    /// <param name="text">文本内容</param>
    /// <param name="value">解析出的值</param>
    /// <returns>解析是否成功</returns>
    private bool TryParseCurrentValue(string configKey, string text, out object value)
    {
        value = null;
        if (string.IsNullOrEmpty(text)) return false;
        string cleanText = text.Trim();
        try
        {
            switch (configKey)
            {
                case "baseCoin":
                    cleanText = cleanText.Replace("分", "");
                    if (float.TryParse(cleanText, out float baseCoinFloat))
                    {
                        value = baseCoinFloat;
                        return true;
                    }
                    if (int.TryParse(cleanText, out int baseCoinInt))
                    {
                        value = baseCoinInt;
                        return true;
                    }
                    break;

                case "rate":
                    cleanText = cleanText.Replace("%", "");
                    if (int.TryParse(cleanText, out int rateDisplayValue))
                    {
                        value = rateDisplayValue * 10;  // 解析时乘以10: 1%→10, 2%→20, 5%→50
                        return true;
                    }
                    break;
                case "minRate":
                    if (int.TryParse(cleanText, out int minRateValue)) { value = minRateValue; return true; }
                    break;
                case "fengDing":
                    if (cleanText == "不封顶") { value = 0; return true; }
                    cleanText = cleanText.Replace("倍", "");
                    if (int.TryParse(cleanText, out int fengDing)) { value = fengDing; return true; }
                    break;
                case "offlineClick":
                    if (cleanText == "不踢人") { value = 0; return true; }
                    if (cleanText == "30秒") { value = 30; return true; }
                    if (cleanText == "1分钟") { value = 60; return true; }
                    if (cleanText == "2分钟") { value = 120; return true; }
                    if (cleanText == "4分钟") { value = 240; return true; }
                    cleanText = cleanText.Replace("秒", "").Replace("分钟", "");
                    if (int.TryParse(cleanText, out int offlineClick))
                    {
                        value = text.Contains("分钟") ? offlineClick * 60 : offlineClick;
                        return true;
                    }
                    break;

                case "dismissTimes":
                    if (cleanText == "不限制") { value = -1; return true; }
                    cleanText = cleanText.Replace("次", "");
                    if (int.TryParse(cleanText, out int dismissTimes)) { value = dismissTimes; return true; }
                    break;

                case "applyDismissTime":
                    cleanText = cleanText.Replace("分钟", "");
                    if (int.TryParse(cleanText, out int applyDismissTime)) { value = applyDismissTime; return true; }
                    break;

                case "offDismissTime":
                    if (cleanText == "10分钟") { value = 10; return true; }
                    if (cleanText == "30分钟") { value = 30; return true; }
                    if (cleanText == "60分钟") { value = 60; return true; }
                    if (cleanText == "12小时") { value = 720; return true; }
                    if (cleanText.Contains("小时"))
                    {
                        cleanText = cleanText.Replace("小时", "");
                        if (int.TryParse(cleanText, out int hours)) { value = hours * 60; return true; }
                    }
                    else
                    {
                        cleanText = cleanText.Replace("分钟", "");
                        if (int.TryParse(cleanText, out int minutes)) { value = minutes; return true; }
                    }
                    break;

                default:
                    if (int.TryParse(cleanText, out int defaultInt)) { value = defaultInt; return true; }
                    if (float.TryParse(cleanText, out float defaultFloat)) { value = defaultFloat; return true; }
                    break;
            }
        }
        catch (System.Exception ex)
        {
            GF.LogError($"TryParseCurrentValue 解析错误: {ex.Message}");
        }

        return false;
    }


    /// <summary>
    /// 复选框方法 - 支持多种游戏类型
    /// </summary>
    /// <param name="num">复选框编号</param>
    /// <param name="isOn">是否选中</param>
    public void SetCheck(int num, bool isOn)
    {
        var currentConfig = GetCurrentConfig();
        List<int> checkList = null;
        string configName = "";

        // 获取对应游戏类型的Check列表
        if (currentConfig is MJConfigManager.XTHHConfigData xthhConfig)
        {
            checkList = xthhConfig.Check;
            configName = "仙桃晃晃";
        }
        else if (currentConfig is MJConfigManager.KWXConfigData kwxConfig)
        {
            // KWX使用专门的Play和ReadyStateList，不使用Check
            // 应该使用 MJConfigSetter 中的 SetPlayByName 和 SetReadyStateByName 方法
            GF.LogWarning("卡五星不使用通用的Check复选框，请使用SetPlayByName或SetReadyStateByName方法");
            return;
        }
        else if (currentConfig is MJConfigManager.XLCHConfigData xlchConfig)
        {
            checkList = xlchConfig.Check;
            configName = "血流成河";
        }
        else if (currentConfig is MJConfigManager.PDKConfigData pdkConfig)
        {
            checkList = pdkConfig.Check;
            configName = "跑的快";
        }

        // 处理复选框状态
        if (checkList != null)
        {
            if (isOn)
            {
                if (!checkList.Contains(num))
                {
                    checkList.Add(num);
                }
            }
            else
            {
                checkList.Remove(num);
            }
            GF.LogInfo_wl($"{configName} 设置复选框 {num}: {(isOn ? "选中" : "取消")}，当前配置: [{string.Join(",", checkList)}]");
        }
        else
        {
            GF.LogWarning($"当前麻将类型({currentConfig?.ConfigName ?? "未知"})不支持复选框配置");
        }
    }
}

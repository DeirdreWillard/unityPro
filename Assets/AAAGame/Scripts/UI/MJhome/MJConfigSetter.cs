using System;
using UnityEngine;
using UnityEngine.UI;
using GameFramework;


/// <summary>
/// 麻将配置设置器 - 负责通过字符串参数调用各种配置方法
/// 用于Unity编辑器中通过Inspector面板绑定UI事件
/// 注意：这个类不继承 MonoBehaviour，因为它是通过 new 创建的普通类实例
/// </summary>
public class MJConfigSetter
{
    private CreatMjPopup owner; // 持有 CreatMjPopup 实例的引用

    public MJConfigSetter(CreatMjPopup ownerInstance)
    {
        if (ownerInstance == null)
        {
            GF.LogError("MJConfigSetter 初始化失败：ownerInstance 为 null");
        }
        this.owner = ownerInstance;
    }

    /// <summary>
    /// 安全获取当前选中的 Toggle
    /// </summary>
    /// <returns>Toggle 组件，如果获取失败则返回 null</returns>
    private Toggle GetCurrentToggle()
    {
        if (owner.IsInitializing) return null;

        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
        {
            return null;
        }

        return eventSystem.currentSelectedGameObject.GetComponent<Toggle>();
    }

    #region 通用配置方法

    public void SetPeopleNum(int num)
    {
        var currentConfig = owner.GetCurrentConfig();
        currentConfig.PeopleNum = num;
        GF.LogInfo_wl("设置人数" + num);
        
        // 仙桃晃晃人数变化规则检查
        if (currentConfig is MJConfigManager.XTHHConfigData xthhConfig)
        {
            owner.ApplyXTHHPeopleNumRules(num, xthhConfig);
        }
        // 跑得快人数变化规则检查
        else if (currentConfig is MJConfigManager.PDKConfigData pdkConfig)
        {
            owner.ApplyPDKPeopleNumRules(num, pdkConfig);
        }
    }

    public void SetPlayNum(int num)
    {
        var currentConfig = owner.GetCurrentConfig();
        currentConfig.PlayNum = num;
        GF.LogInfo_wl("设置局数" + num);
    }
    #endregion
    #region Xthh配置方法
    public void SetLaiPlay(int num)
    {
        var currentConfig = owner.GetCurrentConfig();
        if (currentConfig is MJConfigManager.XTHHConfigData xthhConfig)
        {
            xthhConfig.LaiPlay = num;
            GF.LogInfo_wl("设置赖牌" + num);
        }
    }
    public void SetLockCard(int num)
    {
        var currentConfig = owner.GetCurrentConfig();
        if (currentConfig is MJConfigManager.XTHHConfigData xthhConfig)
        {
            xthhConfig.LockCard = num;
            GF.LogInfo_wl("设置锁牌" + num);
            
            // 如果是不锁牌(1)，则强制关闭解锁翻倍
            if (num == 1)
            {
                xthhConfig.OpenDouble = 0;
            }
            
            // 锁牌变化时更新解锁翻倍的可用状态
            owner.UpdateOpenDoubleToggle(xthhConfig.OpenDouble == 1, num != 1);
        }
    }
    public void SetPiaoLai(int num)
    {
        var currentConfig = owner.GetCurrentConfig();
        if (currentConfig is MJConfigManager.XTHHConfigData xthhConfig)
        {
            xthhConfig.PiaoLai = num;
            GF.LogInfo_wl("设置飘癞" + num);
        }
    }
    public void SetTuoGuan(int num)
    {
        var currentConfig = owner.GetCurrentConfig();
        if (currentConfig is MJConfigManager.XTHHConfigData xthhConfig)
        {
            xthhConfig.Tuoguan = num;
            GF.LogInfo_wl("设置托管" + num);
        }
    }
    public void SetGangKai(int num)
    {
        var currentConfig = owner.GetCurrentConfig();
        if (currentConfig is MJConfigManager.XTHHConfigData xthhConfig)
        {
            xthhConfig.GangKai = num;
            GF.LogInfo_wl("设置杠开" + num);
        }
    }
    public void SetZhuo(int num)
    {
        var currentConfig = owner.GetCurrentConfig();
        if (currentConfig is MJConfigManager.XTHHConfigData xthhConfig)
        {
            xthhConfig.Zhuo = num;
            GF.LogInfo_wl("设置捉铳" + num);
        }
    }
    public void SetOpenDouble()
    {
        Toggle toggle = GetCurrentToggle();
        if (toggle == null) return;
        var currentConfig = owner.GetCurrentConfig();
        if (currentConfig is MJConfigManager.XTHHConfigData xthhConfig)
        {
            // 如果是不锁牌(1)，则强制关闭解锁翻倍
            if (xthhConfig.LockCard == 1)
            {
                xthhConfig.OpenDouble = 0;
            }
            else
            {
                xthhConfig.OpenDouble = toggle.isOn ? 1 : 0;
            }
            GF.LogInfo_wl($"设置解锁翻倍: {xthhConfig.OpenDouble}");
        }
    }
    #endregion

    #region PDK配置方法
    public void SetFirstCard(int num)
    {
        var currentConfig = owner.GetCurrentConfig();
        if (currentConfig is MJConfigManager.PDKConfigData pdkConfig)
        {
            pdkConfig.FirstCard = num;
            GF.LogInfo_wl("设置先出" + num);
        }
    }
    //设置玩法
    public void SetPlayGame(int num)
    {
        var currentConfig = owner.GetCurrentConfig();
        if (currentConfig is MJConfigManager.PDKConfigData pdkConfig)
        {
            pdkConfig.PlayGame = num;
            GF.LogInfo_wl("设置玩法" + num);
        }
    }
    //设置出牌
    public void SetChuPai(int num)
    {
        var currentConfig = owner.GetCurrentConfig();
        if (currentConfig is MJConfigManager.PDKConfigData pdkConfig)
        {
            pdkConfig.ChuPai = num;
            GF.LogInfo_wl("设置 出牌" + num);
        }
    }

    #endregion

    #region KWX配置方法
    /// <summary>
    /// 设置买马配置
    /// </summary>
    /// <param name="num">1:亮倒自摸买马 2:自摸买马 0:不买马</param>
    public void SetBuyHorse(int num)
    {
        var currentConfig = owner.GetCurrentConfig();
        if (currentConfig is MJConfigManager.KWXConfigData kwxConfig)
        {
            kwxConfig.BuyHorse = num;
            GF.LogInfo_wl("设置买马" + num);
        }
    }

    /// <summary>
    /// 设置封顶配置
    /// </summary>
    /// <param name="num">8/16/32</param>
    public void SetTopOut(int num)
    {
        var currentConfig = owner.GetCurrentConfig();
        if (currentConfig is MJConfigManager.KWXConfigData kwxConfig)
        {
            kwxConfig.TopOut = num;
            GF.LogInfo_wl("设置封顶" + num);
        }
    }

    /// <summary>
    /// 设置选漂模式
    /// </summary>
    /// <param name="num">1:每局选漂 2:首局定漂 3:不漂 4:固定飘1 5:固定飘2 6:固定飘3</param>
    public void SetChoicePiao(int num)
    {
        var currentConfig = owner.GetCurrentConfig();
        if (currentConfig is MJConfigManager.KWXConfigData kwxConfig)
        {
            kwxConfig.ChoicePiao = num;
            GF.LogInfo_wl("设置选漂模式" + num);
        }
    }

    /// <summary>
    /// 设置玩法复选框（KWX专用）
    /// 通过Toggle名称自动解析编号
    /// </summary>
    public void SetPlayByName()
    {
        Toggle toggle = GetCurrentToggle();
        if (toggle == null)
        {
            return;
        }

        var currentConfig = owner.GetCurrentConfig();
        if (currentConfig is MJConfigManager.KWXConfigData kwxConfig)
        {
            string objName = toggle.gameObject.name;
            int playNumber = owner.GetCheckNumberFromName(objName);

            if (playNumber > 0)
            {
                if (toggle.isOn)
                {
                    if (!kwxConfig.Play.Contains(playNumber))
                    {
                        kwxConfig.Play.Add(playNumber);
                        GF.LogInfo_wl($"卡五星-添加玩法: {playNumber}");
                    }
                }
                else
                {
                    kwxConfig.Play.Remove(playNumber);
                    GF.LogInfo_wl($"卡五星-移除玩法: {playNumber}");
                }
            }
            else
            {
                GF.LogWarning($"无法从名称 '{objName}' 解析玩法编号");
            }
        }
    }

    /// <summary>
    /// 设置准备状态配置（KWX专用）
    /// 通过Toggle名称自动解析编号
    /// </summary>
    public void SetReadyStateByName()
    {
        Toggle toggle = GetCurrentToggle();
        if (toggle == null)
        {
            return;
        }

        var currentConfig = owner.GetCurrentConfig();
        if (currentConfig is MJConfigManager.KWXConfigData kwxConfig)
        {
            string objName = toggle.gameObject.name;
            int readyStateNumber = owner.GetCheckNumberFromName(objName);

            if (readyStateNumber > 0)
            {
                if (toggle.isOn)
                {
                    if (!kwxConfig.ReadyStateList.Contains(readyStateNumber))
                    {
                        kwxConfig.ReadyStateList.Add(readyStateNumber);
                        GF.LogInfo_wl($"卡五星-添加准备状态: {readyStateNumber}");
                    }
                }
                else
                {
                    kwxConfig.ReadyStateList.Remove(readyStateNumber);
                    GF.LogInfo_wl($"卡五星-移除准备状态: {readyStateNumber}");
                }
            }
            else
            {
                GF.LogWarning($"无法从名称 '{objName}' 解析准备状态编号");
            }
        }
    }
    #endregion

    #region 血流配置方法
    public void SetFan(int num)
    {
        var currentConfig = owner.GetCurrentConfig();
        if (currentConfig is MJConfigManager.XLCHConfigData xlchConfig)
        {
            xlchConfig.Fan = num;
            GF.LogInfo_wl("设置番数" + num);
        }
    }
    public void SetPiao(int num)
    {
        var currentConfig = owner.GetCurrentConfig();
        if (currentConfig is MJConfigManager.XLCHConfigData xlchConfig)
        {
            xlchConfig.Piao = num;
            GF.LogInfo_wl("设置飘" + num);
        }
    }
    public void SetChangeCard(int num)
    {
        var currentConfig = owner.GetCurrentConfig();
        if (currentConfig is MJConfigManager.XLCHConfigData xlchConfig)
        {
            xlchConfig.ChangeCard = num;
            GF.LogInfo_wl("设置换三张" + num);
        }
    }
    /// <summary>
    /// 设置玩法复选框（血流专用）
    /// 通过Toggle名称自动解析编号
    /// </summary>
    public void SetCheckByName_XLCH()
    {
        Toggle toggle = GetCurrentToggle();
        if (toggle == null)
        {
            return;
        }

        var currentConfig = owner.GetCurrentConfig();
        if (currentConfig is MJConfigManager.XLCHConfigData xlchConfig)
        {
            string objName = toggle.gameObject.name;
            int checkNumber = owner.GetCheckNumberFromName(objName);

            if (checkNumber > 0)
            {
                if (toggle.isOn)
                {
                    if (!xlchConfig.Check.Contains(checkNumber))
                    {
                        xlchConfig.Check.Add(checkNumber);
                        GF.LogInfo_wl($"血流成河-添加复选配置: {checkNumber}");
                    }
                }
                else
                {
                    xlchConfig.Check.Remove(checkNumber);
                    GF.LogInfo_wl($"血流成河-移除复选配置: {checkNumber}");
                }
            }
            else
            {
                GF.LogWarning($"无法从名称 '{objName}' 解析复选框编号");
            }
        }
    }

#endregion
    #region 具体数值操作方法（公共方法 - 供 MJConfigSetter 调用）
    /// <summary>
    /// 增加底分值
    /// </summary>
    public void AddBaseCoin()
    {
        owner.AddValue("baseCoin");
    }

    /// <summary>
    /// 减少底分值
    /// </summary>
    public void SubtractBaseCoin()
    {

        owner.SubtractValue("baseCoin");
    }

    /// <summary>
    /// 增加封顶值
    /// </summary>
    public void AddFengDing()
    {
        GF.LogInfo_wl("AddFengDing called");
        owner.AddValue("fengDing");
    }

    /// <summary>
    /// 减少封顶值
    /// </summary>
    public void SubtractFengDing()
    {
        GF.LogInfo_wl("SubtractFengDing called");
        owner.SubtractValue("fengDing");
    }

    /// <summary>
    /// 增加离线踢人时间
    /// </summary>
    public void AddOfflineClick()
    {
        owner.AddValue("offlineClick");
    }

    /// <summary>
    /// 减少离线踢人时间
    /// </summary>
    public void SubtractOfflineClick()
    {
        owner.SubtractValue("offlineClick");
    }

    /// <summary>
    /// 增加解散次数
    /// </summary>
    public void AddDismissTimes()
    {
        owner.AddValue("dismissTimes");
    }

    /// <summary>
    /// 减少解散次数
    /// </summary>
    public void SubtractDismissTimes()
    {
        owner.SubtractValue("dismissTimes");
    }

    /// <summary>
    /// 增加申请解散时间
    /// </summary>
    public void AddApplyDismissTime()
    {
        owner.AddValue("applyDismissTime");
    }

    /// <summary>
    /// 减少申请解散时间
    /// </summary>
    public void SubtractApplyDismissTime()
    {
        owner.SubtractValue("applyDismissTime");
    }

    /// <summary>
    /// 增加离线解散时间
    /// </summary>
    public void AddOffDismissTime()
    {
        owner.AddValue("offDismissTime");
    }

    /// <summary>
    /// 减少离线解散时间
    /// </summary>
    public void SubtractOffDismissTime()
    {
        owner.SubtractValue("offDismissTime");
    }

    /// <summary>
    /// 通用增加方法（通过参数指定类型）
    /// </summary>
    /// <param name="valueType">数值类型</param>
    public void AddValueByType(string valueType)
    {
        owner.AddValue(valueType);
    }

    /// <summary>
    /// 通用减少方法（通过参数指定类型）
    /// </summary>
    /// <param name="valueType">数值类型</param>
    public void SubtractValueByType(string valueType)
    {
        owner.SubtractValue(valueType);
    }

    /// <summary>
    /// 增加抽水比例
    /// </summary>
    public void AddRate()
    {
        owner.AddValue("rate");
    }

    /// <summary>
    /// 减少抽水比例
    /// </summary>
    public void SubtractRate()
    {
        owner.SubtractValue("rate");
    }

    /// <summary>
    /// 增加最低抽水
    /// </summary>
    public void AddMinRate()
    {
        owner.AddValue("minRate");
    }

    /// <summary>
    /// 减少最低抽水
    /// </summary>
    public void SubtractMinRate()
    {
        owner.SubtractValue("minRate");
    }


    /// <summary>
    /// 观战功能开关
    /// </summary>
    public void SetWatchByToggle()
    {
        Toggle toggle = GetCurrentToggle();
        if (toggle == null)
        {
            return;
        }
        var currentConfig = owner.GetCurrentConfig();
        currentConfig.Watch = toggle.isOn ? 1 : 0;
        GF.LogInfo_wl($"设置允许旁观: {(toggle.isOn ? "开启" : "关闭")} (值: {currentConfig.Watch})");
    }

    /// <summary>
    /// 设置入桌自动准备状态
    /// </summary>
    public void SetReadyStateToggle()
    {
        Toggle toggle = GetCurrentToggle();
        if (toggle == null)
        {
            return;
        }
        var currentConfig = owner.GetCurrentConfig();
        currentConfig.ReadyState = toggle.isOn ? 1 : 0;
        GF.LogInfo_wl($"设置入桌自动准备: {(toggle.isOn ? "开启" : "关闭")} (值: {currentConfig.ReadyState})");
    }

    /// <summary>
    /// 禁用语音开关
    /// </summary>
    public void SetVoiceToggle()
    {
        Toggle toggle = GetCurrentToggle();
        if (toggle == null)
        {
            return;
        }
        var currentConfig = owner.GetCurrentConfig();
        currentConfig.ForbidVoice = toggle.isOn;
        GF.LogInfo_wl($"设置语音禁用: {(toggle.isOn ? "开启" : "关闭")} (值: {currentConfig.ForbidVoice})");
    }
    /// <summary>
    /// 禁用打字开关
    /// </summary>
    public void SetChatToggle()
    {
        Toggle toggle = GetCurrentToggle();
        if (toggle == null)
        {
            return;
        }
        var currentConfig = owner.GetCurrentConfig();
        currentConfig.Forbid = toggle.isOn;
        GF.LogInfo_wl($"设置打字禁用: {(toggle.isOn ? "开启" : "关闭")} (值: {currentConfig.Forbid})");
    }
    /// <summary>
    /// 设置抽水开关
    /// </summary>
    public void SetRateToggle()
    {
        Toggle toggle = GetCurrentToggle();
        if (toggle == null)
        {
            return;
        }
        var currentConfig = owner.GetCurrentConfig();
        currentConfig.openRate = toggle.isOn;
        GF.LogInfo_wl($"设置抽水: {(toggle.isOn ? "开启" : "关闭")} (值: {currentConfig.openRate})");

        UpdateRateUI(toggle.isOn);
    }

    /// <summary>
    /// 设置IP限制开关
    /// </summary>
    public void SetIpLimitToggle()
    {
        Toggle toggle = GetCurrentToggle();
        if (toggle == null)
        {
            return;
        }
        var currentConfig = owner.GetCurrentConfig();
        currentConfig.IpLimit = toggle.isOn;
        GF.LogInfo_wl($"设置IP限制: {(toggle.isOn ? "开启" : "关闭")} (值: {currentConfig.IpLimit})");
    }

    /// <summary>
    /// 设置GPS限制开关
    /// </summary>
    public void SetGpsLimitToggle()
    {
        Toggle toggle = GetCurrentToggle();
        if (toggle == null)
        {
            return;
        }
        var currentConfig = owner.GetCurrentConfig();
        currentConfig.GpsLimit = toggle.isOn;
        GF.LogInfo_wl($"设置GPS限制: {(toggle.isOn ? "开启" : "关闭")} (值: {currentConfig.GpsLimit})");
    }

    /// <summary>
    /// 更新抽水相关UI的显示状态
    /// </summary>
    /// <param name="isOn">是否显示</param>
    public void UpdateRateUI(bool isOn)
    {
        string currentMjType = owner.GetCurrentMjType();
        GF.LogInfo_wl($"设置抽水UI: {(isOn ? "显示" : "隐藏")} (类型: {currentMjType})");
        var SetRateType = owner.varGameRule.transform.Find($"{currentMjType}/rule/Viewport/Content/SetRateType").gameObject;
        var SetRate = owner.varGameRule.transform.Find($"{currentMjType}/rule/Viewport/Content/SetRate").gameObject;
        var SetRateValue = owner.varGameRule.transform.Find($"{currentMjType}/rule/Viewport/Content/SetRateValue").gameObject;
        SetRateType.SetActive(isOn);
        SetRate.SetActive(isOn);
        SetRateValue.SetActive(isOn);
    }

    /// <summary>
    /// 设置抽水玩法
    /// </summary>
    public void SetRateType(int num)
    {
        var currentConfig = owner.GetCurrentConfig();
        currentConfig.RateType = num;
        GF.LogInfo_wl("设置抽水玩法" + num);
    }
    /// <summary>
    /// 通用复选框方法，根据点击物体的名字和Toggle状态自动设置
    /// 支持多种命名约定：
    /// 1. 直接数字命名（如"1", "2", "3"）
    /// 2. 带前缀的数字命名（如"check_1", "option_2"）
    /// 3. 中文描述命名（通过映射字典转换）
    /// </summary>
    public void SetCheckByName()
    {
        // 获取当前点击的Toggle对象
        Toggle toggle = GetCurrentToggle();
        if (toggle == null)
        {
            return;
        }

        string objName = toggle.gameObject.name;
        int checkNumber = owner.GetCheckNumberFromName(objName);

        if (checkNumber > 0)
        {
            owner.SetCheck(checkNumber, toggle.isOn);
        }
        else
        {
            GF.LogWarning($"无法从名称 '{objName}' 解析复选框编号");
        }
    }
    #endregion
}
﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GameFramework;
using NetMsg;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using System.Security.Cryptography;
using System.Text;

using static UtilityBuiltin;

public class Util
{
    private static Util instance;
    private Util() { }

    public static Util GetInstance()
    {
        if (instance == null)
        {
            instance = new Util();
        }
        return instance;
    }

    public static void SortListByLatestTime<T>(List<T> items, Func<T, long> getOpTime)
    {
        // 使用委托获取 opTime 并排序
        var sortedItems = items.OrderByDescending(getOpTime).ToList();

        // 清空原始 RepeatedField
        items.Clear();

        // 将排序后的结果写回到 RepeatedField
        items.AddRange(sortedItems);
    }

    public static string FormatAmount(string amount)
    {
        return FormatAmount(double.Parse(amount));
    }

    /// <summary>
    /// 金额模版 (long类型)
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public static string FormatAmount(long amount)
    {
        return FormatAmount((double)amount);
    }

    /// <summary>
    /// 金额模版 (float类型)
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public static string FormatAmount(float amount)
    {
        return FormatAmount((double)amount);
    }

    /// <summary>
    /// 金额模版 (double类型 - 核心实现)
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public static string FormatAmount(double amount)
    {
        // 使用 Math.Abs 和小数比较判断是否为整数,避免浮点精度问题
        if (Math.Abs(amount - Math.Round(amount)) < 0.0001)
        {
            // 如果是整数(或非常接近整数),直接返回整数部分
            return Math.Round(amount).ToString("0");
        }
        else
        {
            // 如果有小数,保留两位小数(四舍五入)
            return amount.ToString("0.##");
        }
    }

    /// <summary>
    /// 正负模板显示
    /// </summary>
    /// <param name="value"></param>
    /// <param name="positiveColor"></param>
    /// <param name="negativeColor"></param>
    /// <returns></returns>
    public static string FormatRichText(float value, string positiveColor = "#00FF00", string negativeColor = "#FF0000")
    {
        // 选择颜色
        string color = value >= 0 ? negativeColor : positiveColor;

        // 格式化数值（最多两位小数，自动去掉多余的 0）
        string formattedValue;
        if (value == 0)
        {
            formattedValue = "+0"; // 零值特殊处理
        }
        else
        {
            // 使用绝对值并根据正负添加符号，确保符号宽度一致
            formattedValue = value > 0 ? $"+{System.Math.Abs(value):0.##}" : $"-{System.Math.Abs(value):0.##}";
        }

        // 返回带 Rich Text 的字符串
        return $"<color={color}>{formattedValue}</color>";
    }

    //新增奖池模板 
    public static string FormatJackpotText(double v, bool isJackpot = false)
    {
        string s;
        if (isJackpot)
        {
            string[] parts = v.ToString("00000000.00").Split('.');
            s = parts[0] + "@" + (parts.Length > 1 ? parts[1] : "00");
        }
        else
        {
            string[] parts = v.ToString("F2").Split('.');
            s = parts[0] + "@" + (parts.Length > 1 ? parts[1] : "00");
        }
        return s;
    }
    public static string FormatJackpotText(string s, bool isJackpot = false)
    {
        return FormatJackpotText(double.Parse(s), isJackpot);
    }


    public static long GetServerTime()
    {
        // 获取当前的客户端时间（毫秒）
        long currentClientTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // 获取服务器时间
        long serverTime = HotfixNetworkManager.Ins.ServerTimestamp;

        // 打印日志，帮助调试
        // GF.LogInfo("获取服务器时间: " , serverTime);
        // GF.LogInfo("获取客户端时间: " , currentClientTime);

        // 如果服务器时间为 0，或者服务器时间和客户端时间的差异过大（例如大于10秒），则返回客户端时间
        if (serverTime == 0) // 10秒的差异  || Math.Abs(currentClientTime - serverTime) > 10000
        {
            GF.LogError("服务器时间差异过大，使用客户端时间");
            return currentClientTime; // 使用客户端时间
        }

        // 否则使用服务器时间
        return serverTime;
    }

    private static float lastClickTime = 0f; // 上次点击的时间
    private static readonly object lockObject = new(); // 用于线程安全

    public static void AddLockTime(float time)
    {
        lock (lockObject)
        {
            lastClickTime += time;
        }
    }

    /// <summary>
    /// 检查是否在点击间隔内。
    /// </summary>
    /// <param name="interval">间隔时间（秒），默认为 0.2 秒。</param>
    /// <returns>如果在间隔内，返回 true；否则返回 false。</returns>
    public static bool IsClickLocked(float interval = 0.2f)
    {
        lock (lockObject)
        {
            if (Time.time - lastClickTime < interval)
            {
                return true;
            }

            lastClickTime = Time.time;
            return false;
        }
    }

    /// <summary>
    /// 判断该玩法是否为横屏游戏（麻将、跑得快）
    /// </summary>
    public static bool IsLandscapeGame(MethodType methodType)
    {
        return methodType == MethodType.MjSimple || methodType == MethodType.RunFast;
    }

    /// <summary>
    /// 判断该玩法是否为竖屏游戏
    /// </summary>
    public static bool IsPortraitGame(MethodType methodType)
    {
        return !IsLandscapeGame(methodType);
    }

    public static bool IsMySelf(long playerGuid)
    {
        return Util.GetMyselfInfo().PlayerId == playerGuid;
    }

    public static bool IsMyLeague(long leagueId)
    {
        return GlobalManager.GetInstance().MyLeagueInfos.Any(l => l.LeagueId == leagueId);
    }

    public static void UpdateClubInfoRq()
    {
        Msg_MyLeagueListRq req = MessagePool.Instance.Fetch<Msg_MyLeagueListRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_MyLeagueListRq, req);
    }

    /// <summary>
    /// 进入桌子
    /// </summary>
    public void Send_EnterDeskRq(int deskId, long clubId = 0)
    {
        CheckSafeCodeState2(
            success: () =>
            {
                Msg_EnterDeskRq req = MessagePool.Instance.Fetch<Msg_EnterDeskRq>();
                req.DeskId = deskId;
                req.ClubId = clubId;
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                    (HotfixNetworkComponent.AccountClientName, MessageID.Msg_EnterDeskRq, req);
            }
        );
    }

    public static void UpdateTableColor(Image image)
    {
        image.SetSprite("Common/TableBG/game_bg_" + SettingExtension.GetTableBG() + ".jpg");
    }


    /// <summary>
    /// 切换牌桌
    /// </summary>
    public static void Send_ChangeDeskRq(int deskId)
    {
        Msg_ChangeDeskRq req = MessagePool.Instance.Fetch<Msg_ChangeDeskRq>();
        req.DeskId = deskId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ChangeDeskRq, req);
    }

    /// <summary>
    /// 进入某个界面需要检测安全码（根据横竖屏显示不同预制体）
    /// </summary>
    /// <param name="go">可选的GameObject参数</param>
    public async void CheckSafeCodeState(GameObject go = null)
    {
        // 通过屏幕宽高比判断横竖屏（更准确）
        bool isLandscape = GlobalManager.IsLandscape();
        if (Util.GetMyselfInfo().LockState == 1)
        {
            var uiParams = UIParams.Create();
            uiParams.Set<VarGameObject>("go", go);

            // 通过屏幕宽高比判断横竖屏（比Screen.orientation更可靠）
            if (isLandscape)
            {
                // 横屏：打开横屏版本的安全码对话框（使用JoinFriendCircle）
                var form = await GF.UI.OpenUIFormAwait(UIViews.JoinFriendCircle, uiParams);
                // 设置为二级密码模式
                if (form != null)
                {
                    var joinFriendCircle = form.GetComponent<JoinFriendCircle>();
                    if (joinFriendCircle != null)
                    {
                        joinFriendCircle.ChangeTopImage(2);
                    }
                }
            }
            else
            {
                // 竖屏：打开竖屏版本的安全码对话框
                await GF.UI.OpenUIFormAwait(UIViews.SecurityCodeDialog, uiParams);
            }
        }
    }

    /// <summary>
    /// 进入某个界面需要检测安全码（带回调）
    /// </summary>
    /// <param name="success">验证成功回调</param>
    /// <param name="lose">验证失败回调</param>
    public async void CheckSafeCodeState2(UnityAction success = null, UnityAction lose = null)
    {
        // 通过屏幕宽高比判断横竖屏（更准确）
        bool isLandscape = GlobalManager.IsLandscape();
        if (Util.GetMyselfInfo().LockState == 1)
        {
            var uiParams = UIParams.Create();
            var actionWrapper = ReferencePool.Acquire<VarObject>();
            actionWrapper.Value = success;
            var actionWrapper2 = ReferencePool.Acquire<VarObject>();
            actionWrapper2.Value = lose;
            uiParams.Set<VarObject>("OnSuccess", actionWrapper);
            uiParams.Set<VarObject>("OnLose", actionWrapper2);
            if (isLandscape)
            {
                var form = await GF.UI.OpenUIFormAwait(UIViews.JoinFriendCircle, uiParams);
                if (form != null)
                {
                    var joinFriendCircle = form.GetComponent<JoinFriendCircle>();
                    if (joinFriendCircle != null)
                    {
                        joinFriendCircle.ChangeTopImage(2);
                    }
                }
            }
            else
            {
                await GF.UI.OpenUIFormAwait(UIViews.SecurityCodeDialog, uiParams);
            }
        }
        else
        {
            success?.Invoke();
        }
    }

    public static UserDataModel GetMyselfInfo()
    {
        return GF.DataModel.GetOrCreate<UserDataModel>();
    }

    /// <summary>
    /// 打开通用确定框
    /// </summary>
    /// <param name="title"></param>
    /// <param name="content"></param>
    /// <param name="onOk"></param>
    /// <param name="onCancel"></param>
    public void OpenConfirmationDialog(string title, string content, System.Action onOk = null, System.Action onCancel = null)
    {
        GF.LogInfo("确认弹窗");
        var uiParams = UIParams.Create();
        uiParams.Set<VarString>("Title", title);
        uiParams.Set<VarString>("Content", content);
        uiParams.ButtonClickCallback = (sender, btId) =>
        {
            switch (btId)
            {
                case "Ok":
                    onOk?.Invoke();
                    break;
                case "Cancel":
                    onCancel?.Invoke();
                    break;
            }
        };
        GF.UI.OpenUIForm(UIViews.ConfirmationDialog, uiParams);
    }
    /// <summary>
    /// 打开通用确定框
    /// </summary>
    /// <param name="title"></param>
    /// <param name="content"></param>
    /// <param name="onOk"></param>
    /// <param name="onCancel"></param>
    public void OpenMJConfirmationDialog(string title, string content, System.Action onOk = null, System.Action onCancel = null)
    {
        GF.LogInfo("确认麻将弹窗");
        var uiParams = UIParams.Create();
        uiParams.Set<VarString>("Title", title);
        uiParams.Set<VarString>("Content", content);
        uiParams.ButtonClickCallback = (sender, btId) =>
        {
            switch (btId)
            {
                case "Ok":
                    onOk?.Invoke();
                    break;
                case "Cancel":
                    onCancel?.Invoke();
                    break;
            }
        };
        GF.UI.OpenUIForm(UIViews.MJConfirmationDialog, uiParams);
    }

    /// <summary>
    /// 打开通用奖励框
    /// </summary>
    /// <param name="rewards">奖励数据列表</param>
    /// <param name="title">标题(可选)</param>
    /// <param name="callback">关闭回调(可选)</param>
    /// <param name="endPoint">飞向目标点(可选)</param>
    /// <param name="autoCloseDelay">自动关闭延迟秒数,0表示不自动关闭(可选)</param>
    public async void OpenRewardPanel(List<RewardItemData> rewards, System.Action callback = null, Vector3? endPoint = null, float autoCloseDelay = 0f)
    {
        if (rewards == null || rewards.Count == 0)
        {
            GF.LogWarning("奖励数据为空,无法打开奖励面板");
            return;
        }

        var uiParams = UIParams.Create();

        // 设置奖励数据
        var rewardDataVar = ReferencePool.Acquire<VarObject>();
        rewardDataVar.Value = rewards;
        uiParams.Set<VarObject>("RewardData", rewardDataVar);

        // 设置关闭回调
        if (callback != null)
        {
            var callbackVar = ReferencePool.Acquire<VarObject>();
            callbackVar.Value = callback;
            uiParams.Set<VarObject>("Callback", callbackVar);
        }

        // 设置飞向目标点
        if (endPoint.HasValue)
        {
            uiParams.Set<VarVector3>("EndPoint", endPoint.Value);
        }

        // 设置自动关闭延迟
        if (autoCloseDelay > 0f)
        {
            uiParams.Set<VarSingle>("AutoCloseDelay", autoCloseDelay);
        }

        GF.LogInfo_gsc("打开奖励面板");
        await GF.UI.OpenUIFormAwait(UIViews.RewardPanel, uiParams);
    }

    public static bool InLoginProcedure()
    {
        if (GF.Procedure.CurrentProcedure == null)
        {
            return true;
        }
        if (GF.Procedure.CurrentProcedure is StartProcedure)
        {
            return true;
        }
        if (GF.Procedure.CurrentProcedure is PreloadProcedure)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 是否在游戏过程
    /// </summary>
    /// <returns></returns>
    public static bool InGameProcedure()
    {
        if (GF.Procedure.CurrentProcedure == null)
        {
            return false;
        }
        if (GF.Procedure.CurrentProcedure is StartProcedure)
        {
            return false;
        }
        if (GF.Procedure.CurrentProcedure is PreloadProcedure)
        {
            return false;
        }
        if (GF.Procedure.CurrentProcedure is HomeProcedure)
        {
            return false;
        }
        if (GF.Procedure.CurrentProcedure is MJHomeProcedure)
        {
            return false;
        }
        return true;
    }

    private float loadingStartTime = 0f;              // 记录加载开始时间
    private bool isLoadingShowing = false;            // 加载界面显示状态
    private Coroutine loadingTimeoutCoroutine = null; // 超时检测协程
    private float loadingTimeoutThreshold = 60f;      // 加载超时阈值（秒），默认60秒

    // 新增：等待框key字典
    private Dictionary<string, LoadingInfo> m_LoadingDict = new Dictionary<string, LoadingInfo>();

    // 等待框信息
    private class LoadingInfo
    {
        public string Key;                   // 等待框key
        public string Content;               // 显示内容
        public float StartTime;              // 开始时间
        public Coroutine TimeoutCoroutine;   // 超时检测协程
    }

    /// <summary>
    /// 设置加载超时阈值
    /// </summary>
    /// <param name="seconds">超时时间（秒）</param>
    public void SetLoadingTimeoutThreshold(float seconds)
    {
        if (seconds > 0)
        {
            loadingTimeoutThreshold = seconds;
        }
    }

    /// <summary>
    /// 显示等待框，指定key来区分不同的等待框
    /// </summary>
    /// <param name="content">显示内容</param>
    /// <param name="key">等待框标识，不同的key互不影响</param>
    public void ShowWaiting(string content = "", string key = "default")
    {
        // GF.LogInfo($"打开, key:{key}, content:{content}");
        // 如果已经存在相同key的等待框，直接返回
        if (m_LoadingDict.ContainsKey(key))
        {
            return;
        }

        // 创建新的等待框信息
        LoadingInfo loadingInfo = new LoadingInfo
        {
            Key = key,
            Content = content,
            StartTime = Time.time
        };

        // 添加到字典
        m_LoadingDict[key] = loadingInfo;

        // 如果当前没有显示等待框，则显示
        if (!isLoadingShowing)
        {
            // GF.LogInfo($"打开Loading, key:{key}, content:{content}");
            GF.StaticUI.ShowWaiting(content);
            isLoadingShowing = true;
        }

        // 启动超时检测协程
        loadingInfo.TimeoutCoroutine = CoroutineRunner.Instance.RunCoroutine(LoadingTimeoutCheck(key, content));
    }

    /// <summary>
    /// 关闭指定key的等待框
    /// </summary>
    /// <param name="key">等待框标识，为空则关闭默认等待框</param>
    public void CloseWaiting(string key = "default")
    {
        // GF.LogInfo($"关闭, key:{key}");
        // 尝试获取指定key的等待框信息
        if (m_LoadingDict.TryGetValue(key, out LoadingInfo loadingInfo))
        {
            // 从字典中移除
            m_LoadingDict.Remove(key);

            // 停止超时检测协程
            if (loadingInfo.TimeoutCoroutine != null)
            {
                CoroutineRunner.Instance.StopCoroutine(loadingInfo.TimeoutCoroutine);
            }

            // GF.LogInfo($"关闭Loading, key:{key}");

            // 如果没有更多等待框，隐藏UI
            if (m_LoadingDict.Count == 0)
            {
                GF.StaticUI.HideWaiting();
                isLoadingShowing = false;
            }
            // 如果还有其他等待框，显示最后一个
            else if (isLoadingShowing)
            {
                // 显示最后添加的等待框
                var lastLoading = m_LoadingDict.Values.Last();
                GF.StaticUI.ShowWaiting(lastLoading.Content);
            }
        }
    }

    /// <summary>
    /// 关闭所有等待框
    /// </summary>
    public void CloseAllLoadingDialogs()
    {
        // 停止所有超时检测协程
        foreach (var loadingInfo in m_LoadingDict.Values)
        {
            if (loadingInfo.TimeoutCoroutine != null)
            {
                CoroutineRunner.Instance.StopCoroutine(loadingInfo.TimeoutCoroutine);
            }
        }

        // 清空字典
        m_LoadingDict.Clear();

        // 隐藏UI
        GF.StaticUI.HideWaiting();
        isLoadingShowing = false;

        GF.LogInfo("关闭所有Loading");
    }

    /// <summary>
    /// 加载超时检测协程
    /// </summary>
    /// <param name="key">等待框标识</param>
    /// <param name="content">当前加载提示内容</param>
    /// <returns></returns>
    private System.Collections.IEnumerator LoadingTimeoutCheck(string key, string content)
    {
        yield return new UnityEngine.WaitForSeconds(loadingTimeoutThreshold);

        // 如果指定key的等待框仍在显示，说明已超时
        if (m_LoadingDict.ContainsKey(key))
        {
            // 处理超时情况
            HandleLoadingTimeout(key, content);
        }
    }

    /// <summary>
    /// 处理加载超时情况
    /// </summary>
    /// <param name="key">等待框标识</param>
    /// <param name="content">当前加载提示内容</param>
    private void HandleLoadingTimeout(string key, string content)
    {
        // 获取等待框开始时间
        float startTime = 0;
        if (m_LoadingDict.TryGetValue(key, out LoadingInfo loadingInfo))
        {
            startTime = loadingInfo.StartTime;
        }

        // 关闭指定key的等待框
        CloseWaiting(key);

        // 记录日志
        GF.LogWarning($"加载界面显示超时：key:{key}, 内容:{content}，已显示{Time.time - startTime}秒");

        // 直接退出到登录界面
        HomeProcedure homeProcedure = GF.Procedure.GetProcedure<HomeProcedure>() as HomeProcedure;
        homeProcedure.QuitGame();
        GF.UI.ShowToast("请求超时，请检查网络状态并重新登录");
    }

    /// <summary>
    /// 延迟一段时间后显示加载界面
    /// </summary>
    /// <returns></returns>
    private System.Collections.IEnumerator ShowLoadingAfterDelay()
    {
        yield return new UnityEngine.WaitForSeconds(0.5f);
        // 显示界面
        ShowWaiting("正在重新连接中...", "reconnect");
    }

    /// <summary>
    /// 加入状态
    /// </summary>
    public static string GetJoinState(int state)
    {
        string[] states = new[] {
            "未加入",
            "已申请",
            "已加入",
            "已邀请",
         };
        return states[state];
    }

    public static string Sum2String(params object[] values)
    {
        float sum = Sum2Float(values);
        if (sum == Math.Floor(sum)) return sum.ToString("F0");
        return FormatAmount(sum);
    }

    public static float Sum2Float(params object[] values)
    {
        double sum = 0;
        foreach (var value in values)
        {
            if (value is int i)
            {
                sum += i;
            }
            else if (value is long l)
            {
                sum += l;
            }
            else if (value is float f)
            {
                sum += f;
            }
            else if (value is double d)
            {
                sum += d;
            }
            else if (value is string s && double.TryParse(s, out double p))
            {
                sum += p;
            }
        }
        sum = double.Parse(FormatAmount(sum.ToString()));
        return (float)sum;
    }


    public static string MillisecondsToDateString(long milliseconds, string format = "M/dd HH:mm")
    {
        DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime dateTime;
        // DateTime 的合法毫秒范围检查
        if (milliseconds < -62135596800000 || milliseconds > 253402300799999)
        {
            dateTime = DateTime.Now; // 超出范围，返回默认值
        }

        try
        {
            // 将毫秒数加到起始时间并转为本地时间
            dateTime = startTime.AddMilliseconds(milliseconds).ToLocalTime();
        }
        catch (ArgumentOutOfRangeException)
        {
            dateTime = DateTime.Now; // 万一出现异常，返回当前时间
        }
        return dateTime.ToString(format); // 根据需要格式化字符串
    }

    public static string DateTimeToDateString(DateTime dateTime, string format = "M/dd HH:mm")
    {
        return dateTime.ToString(format); // 根据需要格式化字符串
    }

    /// <summary>
    /// 格式化秒为 00:00:00
    /// </summary>
    /// <param name="seconds">单位秒</param>
    /// <returns></returns>
    public static string ToTime(float seconds)
    {
        seconds = seconds < 0 ? 0 : seconds;
        TimeSpan ts = TimeSpan.FromSeconds(seconds);
        return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="avatarImage"></param>
    /// <param name="LeagueHead"></param>
    /// <param name="headType"> -1:个人 0:俱乐部 1:联盟 </param>
    public static void DownloadHeadImage(RawImage avatarImage, string LeagueHead, int headType = -1)
    {
        string imagePath = "";
        switch (headType)
        {
            case -1: //个人
                imagePath = "Avatar/PlayerHead120.png";
                break;
            case 0: // club
                imagePath = "Avatar/ClubHead120.png";
                break;
            case 1: // union
                imagePath = "Avatar/UnionHead120.png";
                break;
            case 2: // superUnion
                imagePath = "Avatar/SuperUnionHead120.png";
                break;
        }
        if (string.IsNullOrEmpty(LeagueHead))
        {
            UIExtension.LoadSprite(null, AssetsPath.GetSpritesPath(imagePath), (sprite) =>
            {
                if (avatarImage && sprite != null)
                {
                    // 如果是从图集中的 Sprite，需要创建新的 Texture2D
                    if (sprite.packed)
                    {
                        // 从图集中提取 Sprite 区域创建新纹理
                        Texture2D originalTexture = sprite.texture;
                        Rect textureRect = sprite.textureRect;

                        Texture2D newTexture = new Texture2D((int)textureRect.width, (int)textureRect.height, TextureFormat.RGBA32, false);
                        Color[] pixels = originalTexture.GetPixels((int)textureRect.x, (int)textureRect.y, (int)textureRect.width, (int)textureRect.height);
                        newTexture.SetPixels(pixels);
                        newTexture.Apply();

                        avatarImage.texture = newTexture;
                    }
                    else
                    {
                        // 普通纹理直接使用
                        avatarImage.texture = sprite.texture;
                    }
                }
            });
        }
        else
        {
            CoroutineRunner.Instance.RunCoroutine(PhotoManager.GetInstance().LoadAvatar(LeagueHead, headType, avatar =>
           {
               if (avatarImage && avatar != null)
                   avatarImage.texture = avatar;
               else
               {
                   UIExtension.LoadSprite(null, AssetsPath.GetSpritesPath(imagePath), (sprite) =>
                   {
                       if (avatarImage && sprite != null)
                       {
                           // 如果是从图集中的 Sprite，需要创建新的 Texture2D
                           if (sprite.packed)
                           {
                               // 从图集中提取 Sprite 区域创建新纹理
                               Texture2D originalTexture = sprite.texture;
                               Rect textureRect = sprite.textureRect;

                               Texture2D newTexture = new Texture2D((int)textureRect.width, (int)textureRect.height, TextureFormat.RGBA32, false);
                               Color[] pixels = originalTexture.GetPixels((int)textureRect.x, (int)textureRect.y, (int)textureRect.width, (int)textureRect.height);
                               newTexture.SetPixels(pixels);
                               newTexture.Apply();

                               avatarImage.texture = newTexture;
                           }
                           else
                           {
                               // 普通纹理直接使用
                               avatarImage.texture = sprite.texture;
                           }
                       }
                   });
               }
           }));
        }
    }
    public static string GetErrorContent(int errorCode)
    {
        var tb = GF.DataTable.GetDataTable<ErrorTable>();
        var error = tb.GetDataRow(item => item.Id == errorCode);
        if (error == null)
        {
            GF.LogError("error 没有key：", errorCode.ToString());
            return "";
        }

        if (string.IsNullOrEmpty(error.Content))
        {
            GF.LogWarning("错误内容为空，返回默认错误信息");
            return "";
        }
        return error.Content;
    }

    public static bool ValidatePhoneNumber(string phoneNumber)
    {
        // 验证手机号码是否为11位数字，且以1开头
        return Regex.IsMatch(phoneNumber, @"^1\d{10}$");
    }

    public static bool ValidateEmail(string email)
    {
        // 验证邮箱格式
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    // GPS定位锁定状态
    private static bool isGettingGPSLocation = false;

    /// <summary>
    /// 获取GPS位置信息的通用协程方法
    /// </summary>
    /// <param name="callback">获取成功后的回调，参数为格式化的GPS字符串 "latitude,longitude"</param>
    /// <returns></returns>
    public static IEnumerator GetGPSLocation(System.Action<string> callback)
    {
        if (Application.isEditor)
        {
            //随机生成一个gps坐标
            float latitude = UnityEngine.Random.Range(-90, 90);
            float longitude = UnityEngine.Random.Range(-180, 180);
            callback?.Invoke($"{latitude},{longitude}");
            yield break;
        }
        // 如果已经在获取GPS位置，则直接返回
        if (isGettingGPSLocation)
        {
            GF.StaticUI.ShowError("正在获取定位信息,请稍候!");
            yield break;
        }

        // 设置锁定状态
        isGettingGPSLocation = true;

        // 如果定位未开启，尝试开启定位
        if (!Input.location.isEnabledByUser)
        {
            GF.LogWarning("定位未开启，请开启定位权限");
            GF.StaticUI.ShowError("定位权限未开启,请开启定位权限!");
            isGettingGPSLocation = false; // 解除锁定
            yield break;
        }

        // 开始获取位置信息
        Input.location.Start();
        yield return new WaitForSeconds(0.3f);

        // 等待初始化完成，最多等待15秒
        int maxWait = 15;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // 检查是否获取位置成功
        if (maxWait < 1 || Input.location.status == LocationServiceStatus.Failed)
        {
            GF.LogInfo("获取位置信息失败");
            GF.StaticUI.ShowError("获取位置信息失败");
            Input.location.Stop();
            isGettingGPSLocation = false; // 解除锁定
            yield break;
        }

        // 获取位置信息成功，调用回调
        string gpsString = $"{Input.location.lastData.latitude},{Input.location.lastData.longitude}";
        callback?.Invoke(gpsString);

        // 停止位置服务
        Input.location.Stop();

        // 解除锁定状态
        isGettingGPSLocation = false;
    }

    #region 获取设备唯一信息 设备信息
    /// <summary>
    /// 设备型号映射字典，将原始型号映射为更友好可读的名称
    /// </summary>
    private static readonly Dictionary<string, string> s_DeviceModelMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Apple设备
        { "iPhone1,1", "iPhone" },
        { "iPhone1,2", "iPhone 3G" },
        { "iPhone2,1", "iPhone 3GS" },
        { "iPhone3,1", "iPhone 4" },
        { "iPhone3,2", "iPhone 4" },
        { "iPhone3,3", "iPhone 4" },
        { "iPhone4,1", "iPhone 4S" },
        { "iPhone5,1", "iPhone 5" },
        { "iPhone5,2", "iPhone 5" },
        { "iPhone5,3", "iPhone 5C" },
        { "iPhone5,4", "iPhone 5C" },
        { "iPhone6,1", "iPhone 5S" },
        { "iPhone6,2", "iPhone 5S" },
        { "iPhone7,1", "iPhone 6 Plus" },
        { "iPhone7,2", "iPhone 6" },
        { "iPhone8,1", "iPhone 6S" },
        { "iPhone8,2", "iPhone 6S Plus" },
        { "iPhone8,4", "iPhone SE (第一代)" },
        { "iPhone9,1", "iPhone 7" },
        { "iPhone9,3", "iPhone 7" },
        { "iPhone9,2", "iPhone 7 Plus" },
        { "iPhone9,4", "iPhone 7 Plus" },
        { "iPhone10,1", "iPhone 8" },
        { "iPhone10,4", "iPhone 8" },
        { "iPhone10,2", "iPhone 8 Plus" },
        { "iPhone10,5", "iPhone 8 Plus" },
        { "iPhone10,3", "iPhone X" },
        { "iPhone10,6", "iPhone X" },
        { "iPhone11,2", "iPhone XS" },
        { "iPhone11,4", "iPhone XS Max" },
        { "iPhone11,6", "iPhone XS Max" },
        { "iPhone11,8", "iPhone XR" },
        { "iPhone12,1", "iPhone 11" },
        { "iPhone12,3", "iPhone 11 Pro" },
        { "iPhone12,5", "iPhone 11 Pro Max" },
        { "iPhone12,8", "iPhone SE (第二代)" },
        { "iPhone13,1", "iPhone 12 mini" },
        { "iPhone13,2", "iPhone 12" },
        { "iPhone13,3", "iPhone 12 Pro" },
        { "iPhone13,4", "iPhone 12 Pro Max" },
        { "iPhone14,2", "iPhone 13 Pro" },
        { "iPhone14,3", "iPhone 13 Pro Max" },
        { "iPhone14,4", "iPhone 13 mini" },
        { "iPhone14,5", "iPhone 13" },
        { "iPhone14,6", "iPhone SE (第三代)" },
        { "iPhone14,7", "iPhone 14" },
        { "iPhone14,8", "iPhone 14 Plus" },
        { "iPhone15,2", "iPhone 14 Pro" },
        { "iPhone15,3", "iPhone 14 Pro Max" },
        { "iPhone15,4", "iPhone 15" },
        { "iPhone15,5", "iPhone 15 Plus" },
        { "iPhone16,1", "iPhone 15 Pro" },
        { "iPhone16,2", "iPhone 15 Pro Max" },
        { "iPhone17,1", "iPhone 16 Pro" },
        { "iPhone17,2", "iPhone 16 Pro Max" },
        { "iPhone17,3", "iPhone 16" },
        { "iPhone17,4", "iPhone 16 Plus" },
        { "iPhone17,5", "iPhone 16 e" },
    };

    /// <summary>
    /// 获取更友好的设备型号名称
    /// </summary>
    private static string GetFriendlyDeviceName(string originalModel)
    {
        if (string.IsNullOrEmpty(originalModel))
            return "未知设备";

        // 直接匹配
        if (s_DeviceModelMapping.TryGetValue(originalModel, out string friendlyName))
            return friendlyName;

        // 尝试部分匹配关键词
        foreach (var mapping in s_DeviceModelMapping)
        {
            if (originalModel.Contains(mapping.Key))
                return mapping.Value;
        }

        return originalModel; // 如果没有匹配项，返回原始名称
    }

    private static string GetDeviceModelFallback()
    {
        string model = SystemInfo.deviceModel;

        // 如果deviceModel为空，尝试其他方式获取
        if (string.IsNullOrEmpty(model))
        {
#if UNITY_ANDROID
            try
            {
                using (AndroidJavaClass buildClass = new AndroidJavaClass("android.os.Build"))
                {
                    model = buildClass.GetStatic<string>("MODEL");
                    if (string.IsNullOrEmpty(model))
                    {
                        model = buildClass.GetStatic<string>("MANUFACTURER") + " " +
                               buildClass.GetStatic<string>("DEVICE");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Android设备信息获取失败: {e.Message}");
            }
#elif UNITY_IOS
            try
            {
                // 使用原生插件获取iOS设备信息
                model = UnityEngine.iOS.Device.generation.ToString();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"iOS设备信息获取失败: {e.Message}");
            }
#endif
        }

        // 如果仍然为空，使用系统信息组合
        if (string.IsNullOrEmpty(model))
        {
            model = $"{SystemInfo.operatingSystemFamily} {SystemInfo.deviceType}";
        }

        return model;
    }

    /// <summary>
    /// 获取友好的设备型号信息
    /// </summary>
    public static string GetFriendlyDeviceInfo()
    {
        string deviceModel = GetDeviceModelFallback();
        string friendlyModel = GetFriendlyDeviceName(deviceModel);
        string osInfo = SystemInfo.operatingSystem;
        bool isEmulator = IsEmulator();

        // 清理操作系统信息
        if (osInfo.Contains("("))
            osInfo = osInfo.Substring(0, osInfo.IndexOf("(")).Trim();

        // 添加设备唯一标识符
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        if (string.IsNullOrEmpty(deviceId) || deviceId == SystemInfo.unsupportedIdentifier)
        {
            deviceId = System.Guid.NewGuid().ToString();
        }

        // 最终组合信息
        string info;
        if (isEmulator)
        {
            info = $"{osInfo} | {deviceId} | [模拟器]";
        }
        else
        {
            info = $"{friendlyModel} ({deviceModel}) | {osInfo} | {SystemInfo.processorType} | {deviceId}";
        }
        return info;
    }
    #endregion

    /// <summary>
    /// 判断是否运行在模拟器上
    /// 通过cpu类型、设备型号、厂商等多重判断
    /// </summary>
    /// <returns>是否运行在模拟器上</returns>
    public static bool IsEmulator()
    {
        string processorType = SystemInfo.processorType;
        string deviceModel = SystemInfo.deviceModel;
        string deviceName = SystemInfo.deviceName;
        string deviceUniqueIdentifier = SystemInfo.deviceUniqueIdentifier;
        string operatingSystem = SystemInfo.operatingSystem;
        string manufacturer = "";
        string brand = "";
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass buildClass = new AndroidJavaClass("android.os.Build"))
            {
                manufacturer = buildClass.GetStatic<string>("MANUFACTURER");
                brand = buildClass.GetStatic<string>("BRAND");
            }
        }
        catch { }
#endif
        // 1. 检查CPU类型
        if (!string.IsNullOrEmpty(processorType) && processorType.StartsWith("x86", StringComparison.OrdinalIgnoreCase))
            return true;
        // 2. 检查常见模拟器关键字
        string[] emulatorKeywords = new[] { "emulator", "nox", "ldplayer", "droid4x", "mumu", "bluestacks", "genymotion", "vbox", "vbox86", "andy", "koplayer", "memu", "leapdroid" };
        string allInfo = $"{deviceModel}|{deviceName}|{manufacturer}|{brand}|{operatingSystem}".ToLower();
        foreach (var keyword in emulatorKeywords)
        {
            if (allInfo.Contains(keyword))
                return true;
        }
        // 3. 检查设备唯一标识符是否为默认值
        if (!string.IsNullOrEmpty(deviceUniqueIdentifier) && deviceUniqueIdentifier == "00000000000000000000000000000000")
            return true;
        // 4. 检查操作系统（PC直接排除）
        if (IsPC())
            return true;
        return false;
    }

    /// <summary>
    /// 判断是否为PC（Windows/Mac/Linux）
    /// </summary>
    public static bool IsPC()
    {
        var os = SystemInfo.operatingSystemFamily;
        return os == OperatingSystemFamily.Windows || os == OperatingSystemFamily.MacOSX || os == OperatingSystemFamily.Linux;
    }

    #region Encryption Methods
    /// <summary>
    /// 使用DES算法加密字符串。
    /// </summary>
    /// <param name="_toEncrypt">要加密的字符串。</param>
    /// <param name="_key">密钥（必须为8位）。</param>
    /// <returns>加密并转换为Base64的字符串。</returns>
    public static string Encrypt(string _toEncrypt, string _key)
    {
        if (string.IsNullOrEmpty(_toEncrypt)) return string.Empty;

        using (var des = new DESCryptoServiceProvider())
        {
            byte[] inputByteArray = Encoding.UTF8.GetBytes(_toEncrypt);
            des.Key = Encoding.ASCII.GetBytes(_key.Substring(0, 8));
            des.IV = Encoding.ASCII.GetBytes(_key.Substring(0, 8));
            des.Mode = CipherMode.ECB;
            des.Padding = PaddingMode.PKCS7;

            using (var ms = new System.IO.MemoryStream())
            {
                using (var cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
    /// <summary>
    /// 使用DES算法解密字符串。
    /// </summary>
    /// <param name="_toDecrypt">要解密的Base64字符串。</param>
    /// <param name="_key">密钥（必须为8位）。</param>
    /// <returns>解密后的字符串。</returns>
    public static string Decrypt(string _toDecrypt, string _key)
    {
        if (string.IsNullOrEmpty(_toDecrypt)) return string.Empty;

        byte[] inputByteArray = Convert.FromBase64String(_toDecrypt);

        using (var des = new DESCryptoServiceProvider())
        {
            des.Key = Encoding.ASCII.GetBytes(_key.Substring(0, 8));
            des.IV = Encoding.ASCII.GetBytes(_key.Substring(0, 8));
            des.Mode = CipherMode.ECB;
            des.Padding = PaddingMode.PKCS7;

            using (var ms = new System.IO.MemoryStream())
            {
                using (var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                }
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
    #endregion
    #region 
    /// <summary>
    /// 比赛消耗类型
    /// </summary>
    public enum MatchCostType
    {
        Tick = 1,
        Diamond,
    }
    /// <summary>
    /// 获取当前比赛的门票数量和消耗
    /// </summary>
    public static (MatchCostType costType, int cost) GetCurrentMatchTicketInfo(Msg_Match match)
    {
        MatchCostType costType = MatchCostType.Tick;
        int cost = 0;
        if (match.CostTick > 0)
        {
            costType = MatchCostType.Tick;
            cost = match.CostTick;
        }
        else if (match.CostDiamond > 0)
        {
            costType = MatchCostType.Diamond;
            cost = match.CostDiamond;
        }
        return (costType, cost);
    }
    #endregion

    /// <summary>
    /// 验证名字是否只包含允许的字符（中文、字母、数字、下划线、连字符），并屏蔽Emoji等特殊符号。
    /// </summary>
    /// <param name="name">要验证的名字</param>
    /// <returns>如果名字有效返回true，否则返回false</returns>
    public static bool IsValidName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        // 只允许中文、字母、数字、下划线和连字符
        // 这个正则表达式匹配任何不在指定范围内的字符
        // ^ 表示字符串的开始
        // [] 表示字符集
        // a-zA-Z 表示所有大小写字母
        // 0-9 表示所有数字
        // \u4e00-\u9fa5 表示所有基本中文字符
        // _- 表示下划线和连字符
        // + 表示一个或多个上述字符
        // $ 表示字符串的结束
        string pattern = @"^[a-zA-Z0-9\u4e00-\u9fa5_-]+$";

        return Regex.IsMatch(name, pattern);
    }

    public static bool IsValidPasswordCharacters(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;
        foreach (char c in password)
        {
            // 检查是否为汉字（中文字符范围）
            if (c >= 0x4E00 && c <= 0x9FFF)
            {
                return false; // 包含汉字，返回false
            }
            // 检查是否为其他中文符号或全角字符
            if (c >= 0x3000 && c <= 0x303F) // 中文符号
            {
                return false;
            }
            if (c >= 0xFF00 && c <= 0xFFEF) // 全角字符
            {
                return false;
            }
        }
        return true; // 所有字符都有效
    }

    /// <summary>
    /// 设置切换事件
    /// </summary>
    /// <param name="isOn">是否选中</param>
    public static void TestToggleChanged(string btnName, GameObject changeContent)
    {
        Debug.Log("点击了Toggle: " + btnName);
        // 根据btnName来控制varGameRule下面子物体的状态
        for (int i = 0; i < changeContent.transform.childCount; i++)
        {
            Transform child = changeContent.transform.GetChild(i);
            if (child.name == btnName)
            {
                child.gameObject.SetActive(true);
            }
            else
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    #region 麻将
    /// <summary>
    /// 将服务端座位号转换为客户端座位号
    /// </summary>
    /// <param name="serverPos">服务端座位号</param>
    /// <param name="playerCount">玩家数量</param>
    /// <returns>客户端座位号</returns>
    public static int GetPosByServerPos(Position serverPos, int playerCount)
    {
        // seatPos2.Add(1, new Vector3(0, 390, 0));
        // seatPos2.Add(5, new Vector3(0, -330, 0));

        // seatPos3.Add(1, new Vector3(0, 390, 0));
        // seatPos3.Add(4, new Vector3(120, 100, 0));
        // seatPos3.Add(7, new Vector3(-120, 100, 0));

        // seatPos4.Add(1, new Vector3(0, 390, 0));
        // seatPos4.Add(3, new Vector3(120, 100, 0));
        // seatPos4.Add(5, new Vector3(0, -330, 0));
        // seatPos4.Add(8, new Vector3(-120, 100, 0));
        switch (playerCount)
        {
            case 2:
                switch (serverPos)
                {
                    case Position.Default:
                    case Position.One:
                    case Position.Two:
                    case Position.Three:
                        return 0;
                    case Position.Four:
                    case Position.Five:
                    case Position.Six:
                    case Position.Seven:
                    case Position.Eight:
                    case Position.Nine:
                        return 2;
                }
                break;
            case 3:
                switch (serverPos)
                {
                    case Position.Default:
                        return 0;
                    case Position.One:
                    case Position.Two:
                    case Position.Three:
                    case Position.Four:
                        return 3;
                    case Position.Five:
                    case Position.Six:
                    case Position.Seven:
                    case Position.Eight:
                    case Position.Nine:
                        return 1;
                }
                break;
            case 4:
                switch (serverPos)
                {
                    case Position.Default:
                        return 0;
                    case Position.One:
                    case Position.Two:
                    case Position.Three:
                        return 3;
                    case Position.Four:
                    case Position.Five:
                        return 2;
                    case Position.Six:
                    case Position.Seven:
                    case Position.Eight:
                    case Position.Nine:
                        return 1;
                }
                break;
        }
        return -1;
    }
    /// <summary>
    /// 将服务端座位号转换为客户端座位号（PDK专用）
    /// 规则：自己永远在0号位（下方），根据服务端位置循环关系计算相对位置
    /// 服务端位置0-9循环：1是0的下家，0是9的上家
    /// 客户端位置：0=自己(下方), 1=下家(右边), 2=上家(左边)
    /// </summary>
    /// <param name="myServerPos">自己的服务端座位号</param>
    /// <param name="targetServerPos">目标玩家的服务端座位号</param>
    /// <param name="playerCount">玩家数量</param>
    /// <returns>客户端座位号</returns>
    public static int GetPosByServerPos_pdk(Position myServerPos, Position targetServerPos, int playerCount)
    {
        // 自己永远在0号位
        if (myServerPos == targetServerPos)
        {
            return 0;
        }

        int myPos = (int)myServerPos;
        int targetPos = (int)targetServerPos;

        // 计算相对位置差（考虑0-9循环）
        int diff = targetPos - myPos;
        if (diff < 0)
        {
            diff += 10; // 服务端位置0-9循环
        }

        switch (playerCount)
        {
            case 2:
                // 2人局：对家在对面，但PDK可能不会有2人局
                return diff > 0 ? 1 : 0;

            case 3:
                // 3人局：下家在右边(1)，上家在左边(2)
                // 服务端位置差：1-4为下家(右边)，5-9为上家(左边)
                if (diff >= 1 && diff <= 4)
                {
                    return 1; // 下家在右边
                }
                else if (diff >= 5 && diff <= 9)
                {
                    return 2; // 上家在左边
                }
                break;

            case 4:
                // 4人局（如果需要支持）
                // 可以根据实际需求扩展
                if (diff >= 1 && diff <= 3)
                {
                    return 1; // 下家
                }
                else if (diff >= 4 && diff <= 6)
                {
                    return 2; // 对家或其他位置
                }
                else if (diff >= 7 && diff <= 9)
                {
                    return 3; // 上家
                }
                break;
        }

        return -1;
    }
    public static FengWei GetFengWeiByServerPos(Position clientPos, int playerCount)
    {
        // seatPos2.Add(1, new Vector3(0, 390, 0));
        // seatPos2.Add(5, new Vector3(0, -330, 0));

        // seatPos3.Add(1, new Vector3(0, 390, 0));
        // seatPos3.Add(4, new Vector3(120, 100, 0));
        // seatPos3.Add(7, new Vector3(-120, 100, 0));

        // seatPos4.Add(1, new Vector3(0, 390, 0));
        // seatPos4.Add(3, new Vector3(120, 100, 0));
        // seatPos4.Add(5, new Vector3(0, -330, 0));
        // seatPos4.Add(8, new Vector3(-120, 100, 0));
        switch (playerCount)
        {
            case 2:
                switch (clientPos)
                {
                    case Position.Default:
                    case Position.One:
                    case Position.Two:
                    case Position.Three:
                    case Position.Four:
                        return FengWei.EAST;
                    case Position.Five:
                    case Position.Six:
                    case Position.Seven:
                    case Position.Eight:
                    case Position.Nine:
                        return FengWei.WEST;
                }
                break;
            case 3:
                switch (clientPos)
                {
                    case Position.Default:
                        return FengWei.EAST;
                    case Position.One:
                    case Position.Two:
                    case Position.Three:
                    case Position.Four:
                        return FengWei.SOUTH;
                    case Position.Five:
                    case Position.Six:
                    case Position.Seven:
                    case Position.Eight:
                    case Position.Nine:
                        return FengWei.NORTH;
                }
                break;
            case 4:
                switch (clientPos)
                {
                    case Position.Default:
                        return FengWei.EAST;
                    case Position.One:
                    case Position.Two:
                    case Position.Three:
                        return FengWei.SOUTH;
                    case Position.Four:
                    case Position.Five:
                        return FengWei.WEST;
                    case Position.Six:
                    case Position.Seven:
                    case Position.Eight:
                    case Position.Nine:
                        return FengWei.NORTH;
                }
                break;
        }
        return FengWei.EAST;
    }

    public static int TransformSeatS2C(int serverSeat, int selfPosAtServer, int playerCount)
    {
        if (serverSeat == -1)
        {
            return -1;
        }

        int normalizedSeat = ((serverSeat % 4) + 4) % 4;

        if (selfPosAtServer == -1 || playerCount <= 0)
        {
            return normalizedSeat;
        }

        int normalizedSelf = ((selfPosAtServer % 4) + 4) % 4;

        if (playerCount < 1)
        {
            playerCount = 1;
        }
        else if (playerCount > 4)
        {
            playerCount = 4;
        }

        if (playerCount == 2)
        {
            return normalizedSeat == normalizedSelf ? 0 : 2;
        }

        if (playerCount == 3)
        {
            if (normalizedSeat == normalizedSelf)
            {
                return 0;
            }

            switch (normalizedSelf)
            {
                case 0:
                    if (normalizedSeat == 1) return 1;
                    if (normalizedSeat == 3) return 3;
                    break;
                case 1:
                    if (normalizedSeat == 0) return 3;
                    if (normalizedSeat == 3) return 1;
                    break;
                case 3:
                    if (normalizedSeat == 0) return 1;
                    if (normalizedSeat == 1) return 3;
                    break;
            }

            int fallback = normalizedSeat - normalizedSelf;
            if (fallback < 0)
            {
                fallback += 4;
            }

            if (fallback == 2)
            {
                return 3;
            }

            if (fallback == 3)
            {
                return 1;
            }

            return fallback;
        }

        int diff = normalizedSeat - normalizedSelf;
        if (diff < 0)
        {
            diff += 4;
        }

        return diff;
    }

    public static int GetRemainTime(long nextTime)
    {
        long serverTime = Util.GetServerTime();
        int remainTime = nextTime > serverTime ? (int)((nextTime - serverTime) / 1000) : 0;
        return remainTime;
    }

    /// <summary>
    /// 将服务器整型牌值转换为 MahjongFaceValue。
    /// 服务器牌值规范：
    /// 1-9 万, 11-19 筒, 21-29 条, 31-34 东南西北, 41-43 中发白, 51-58 花牌
    /// 其余返回 MJ_UNKNOWN。
    /// </summary>
    public static MahjongFaceValue ConvertServerIntToFaceValue(int value)
    {
        if (value >= 1 && value <= 9)
        {
            return (MahjongFaceValue)((int)MahjongFaceValue.MJ_WANG_1 + (value - 1));
        }

        if (value >= 11 && value <= 19)
        {
            return (MahjongFaceValue)((int)MahjongFaceValue.MJ_TONG_1 + (value - 11));
        }

        if (value >= 21 && value <= 29)
        {
            return (MahjongFaceValue)((int)MahjongFaceValue.MJ_TIAO_1 + (value - 21));
        }

        switch (value)
        {
            case 31: return MahjongFaceValue.MJ_FENG_DONG;
            case 32: return MahjongFaceValue.MJ_FENG_NAN;
            case 33: return MahjongFaceValue.MJ_FENG_XI;
            case 34: return MahjongFaceValue.MJ_FENG_BEI;
            case 41: return MahjongFaceValue.MJ_ZFB_HONGZHONG;
            case 42: return MahjongFaceValue.MJ_ZFB_FACAI;
            case 43: return MahjongFaceValue.MJ_ZFB_BAIBAN;
        }

        if (value >= 51 && value <= 58)
        {
            return (MahjongFaceValue)((int)MahjongFaceValue.MJ_HUA_CHUN + (value - 51));
        }

        return MahjongFaceValue.MJ_UNKNOWN;
    }

    /// <summary>
    /// 批量转换服务器手牌值为 MahjongFaceValue 列表。
    /// </summary>
    public static List<MahjongFaceValue> ConvertServerHandToFaceList(IList<int> values)
    {
        List<MahjongFaceValue> list = new List<MahjongFaceValue>(values == null ? 0 : values.Count);
        if (values == null) return list;
        for (int i = 0; i < values.Count; i++)
        {
            list.Add(ConvertServerIntToFaceValue(values[i]));
        }
        return list;
    }

    public static int ConvertFaceValueToServerInt(MahjongFaceValue faceValue)
    {
        // 万 1-9
        if (faceValue >= MahjongFaceValue.MJ_WANG_1 && faceValue <= MahjongFaceValue.MJ_WANG_9)
        {
            return 1 + ((int)faceValue - (int)MahjongFaceValue.MJ_WANG_1);
        }

        // 筒 11-19
        if (faceValue >= MahjongFaceValue.MJ_TONG_1 && faceValue <= MahjongFaceValue.MJ_TONG_9)
        {
            return 11 + ((int)faceValue - (int)MahjongFaceValue.MJ_TONG_1);
        }

        // 条 21-29
        if (faceValue >= MahjongFaceValue.MJ_TIAO_1 && faceValue <= MahjongFaceValue.MJ_TIAO_9)
        {
            return 21 + ((int)faceValue - (int)MahjongFaceValue.MJ_TIAO_1);
        }

        // 风 31-34
        switch (faceValue)
        {
            case MahjongFaceValue.MJ_FENG_DONG: return 31;
            case MahjongFaceValue.MJ_FENG_NAN: return 32;
            case MahjongFaceValue.MJ_FENG_XI: return 33;
            case MahjongFaceValue.MJ_FENG_BEI: return 34;
        }

        // 中发白 41-43
        switch (faceValue)
        {
            case MahjongFaceValue.MJ_ZFB_HONGZHONG: return 41;
            case MahjongFaceValue.MJ_ZFB_FACAI: return 42;
            case MahjongFaceValue.MJ_ZFB_BAIBAN: return 43;
        }

        // 花 51-58
        if (faceValue >= MahjongFaceValue.MJ_HUA_CHUN && faceValue <= MahjongFaceValue.MJ_HUA_JU)
        {
            return 51 + ((int)faceValue - (int)MahjongFaceValue.MJ_HUA_CHUN);
        }

        // 未知
        return -1;
    }

    /// <summary>
    /// 批量转换 MahjongFaceValue 列表为服务器整型手牌值列表。
    /// 未知面值将转换为 -1。
    /// </summary>
    public static List<int> ConvertFaceListToServerHand(IList<MahjongFaceValue> values)
    {
        List<int> list = new List<int>(values == null ? 0 : values.Count);
        if (values == null) return list;
        for (int i = 0; i < values.Count; i++)
        {
            list.Add(ConvertFaceValueToServerInt(values[i]));
        }
        return list;
    }

    /// <summary>
    /// 麻将手牌排序
    /// 排序规则：赖子在最前面,其他牌按服务器值升序(万→筒→条→风→中发白)
    /// </summary>
    /// <param name="cards">要排序的手牌列表</param>
    /// <param name="laiziValue">赖子的服务器值(如12表示二筒)</param>
    /// <returns>排序后的手牌列表</returns>
    public static List<int> SortMahjongHandCards(List<int> cards, int laiziValue = 0)
    {
        if (cards == null || cards.Count == 0)
            return cards;

        // 创建副本避免修改原列表
        List<int> sortedCards = new List<int>(cards);

        // 按照赖子优先级和麻将牌面值排序
        sortedCards.Sort((card1, card2) =>
        {
            // 直接用服务器值判断是否为赖子
            bool isLaizi1 = (laiziValue > 0 && card1 == laiziValue);
            bool isLaizi2 = (laiziValue > 0 && card2 == laiziValue);

            // 赖子牌排在最前面（索引小，显示在最左边）
            if (isLaizi1 && !isLaizi2) return -1;
            if (!isLaizi1 && isLaizi2) return 1;

            // 如果都是赖子或都不是赖子，按服务器值升序排序（万→筒→条→风→中发白）
            return card1.CompareTo(card2);
        });

        return sortedCards;
    }


    #endregion

    #region  解散房间
    /// <summary>
    /// 显示解散面板 - 收到解散申请消息时调用
    /// </summary>
    /// <param name="agreeDismissTime">解散截止时间</param>
    /// <param name="applyPlayerId">发起解散的玩家ID</param>
    public static void ShowDismissPanel(long agreeDismissTime, long applyPlayerId, List<BasePlayer> allPlayers, List<long> agreeDismissPlayerList)
    {
        // 检查当前玩家是否是旁观者
        long myPlayerId = Util.GetMyselfInfo().PlayerId;
        bool isSeated = allPlayers != null && allPlayers.Any(p => p.PlayerId == myPlayerId);

        // 如果是旁观者，不弹出解散申请框
        if (!isSeated)
        {
            GF.LogInfo($"[解散申请] 当前玩家是旁观者，不弹出解散申请框");
            return;
        }

        var uiParams = UIParams.Create();
        uiParams.Set<VarInt64>("applyPlayerId", applyPlayerId);
        uiParams.Set<VarInt64>("agreeDismissTime", agreeDismissTime);
        uiParams.Set<VarInt64>("currentPlayerId", Util.GetMyselfInfo().PlayerId);
        uiParams.Set<VarObject>("allPlayers", new VarObject { Value = allPlayers });
        uiParams.Set<VarObject>("agreeDismissPlayerList", new VarObject { Value = agreeDismissPlayerList });
        // 通过框架打开解散面板
        GF.UI.OpenUIFormAwait(UIViews.MJDismissRoom, uiParams);
    }
    #endregion

    /// <summary>
    /// 获取离线时间的友好显示文本
    /// </summary>
    /// <param name="offlineTimestamp">离线时间戳(毫秒)</param>
    /// <returns>友好的时间显示文本</returns>
    public static string GetOfflineTimeText(long offlineTimestamp)
    {
        // 将毫秒时间戳转换为DateTime
        System.DateTime offlineTime = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)
            .AddMilliseconds(offlineTimestamp).ToLocalTime();

        System.TimeSpan timeDiff = System.DateTime.Now - offlineTime;

        // 超过一个月
        if (timeDiff.TotalDays > 30)
        {
            return "一个月前";
        }
        // 小于一个月大于一天
        else if (timeDiff.TotalDays >= 1)
        {
            int days = (int)timeDiff.TotalDays;
            return days + "天前";
        }
        // 小于一天大于一小时
        else if (timeDiff.TotalHours >= 1)
        {
            int hours = (int)timeDiff.TotalHours;
            return hours + "小时前";
        }
        // 小于一小时
        else
        {
            int minutes = Mathf.Max(1, (int)timeDiff.TotalMinutes); // 最小显示1分钟
            return minutes + "分钟前";
        }
    }
}

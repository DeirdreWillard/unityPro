﻿using GameFramework;
using NetMsg;
using UnityEngine;
using System.Collections.Generic;
using Google.Protobuf.Collections;
using System.Linq;
using GameFramework.Event;
using Cysharp.Threading.Tasks;
using System;

public enum AmountType
{
    coin = 1, //欢乐豆
    gold = 2,   //联盟币
    diamond = 3, //钻石
}

/// <summary>
/// 游戏流程类型枚举
/// </summary>
public enum GameFlowType
{
    None = 0,           // 未设置
    PokerFlow = 1,      // 竖屏扑克流程（HomeProcedure）
    MahjongFlow = 2     // 横屏麻将流程（MJHomeProcedure）
}

public class GlobalManager
{
    private static GlobalManager instance;
    private GlobalManager() { }

    #region 游戏流程状态缓存
    private const string c_LastGameFlowKey = "LastGameFlow_";
    
    /// <summary>
    /// 保存用户最后的游戏流程状态
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="flowType">游戏流程类型</param>
    public static void SaveLastGameFlow(long playerId, GameFlowType flowType)
    {
        string key = c_LastGameFlowKey + playerId;
        PlayerPrefs.SetInt(key, (int)flowType);
        PlayerPrefs.Save();
        GF.LogInfo($"[GameFlow] 保存玩家 {playerId} 最后游戏流程: {flowType}");
    }
    
    /// <summary>
    /// 获取用户最后的游戏流程状态
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <returns>游戏流程类型</returns>
    public static GameFlowType GetLastGameFlow(long playerId)
    {
        string key = c_LastGameFlowKey + playerId;
        int flowValue = PlayerPrefs.GetInt(key, (int)GameFlowType.PokerFlow); // 默认扑克流程
        GameFlowType flowType = (GameFlowType)flowValue;
        GF.LogInfo($"[GameFlow] 获取玩家 {playerId} 最后游戏流程: {flowType}");
        return flowType;
    }
    
    /// <summary>
    /// 清除用户的游戏流程状态缓存
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    public static void ClearLastGameFlow(long playerId)
    {
        string key = c_LastGameFlowKey + playerId;
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        GF.LogInfo($"[GameFlow] 清除玩家 {playerId} 的游戏流程缓存");
    }
    #endregion

    #region 亲友圈房间信息缓存
    // 用于保存进入桌子前的亲友圈房间LeagueId
    private long beforeEnterDeskLeagueId = 0;
    
    /// <summary>
    /// 保存进入桌子前的亲友圈房间ID
    /// </summary>
    /// <param name="leagueId">亲友圈房间的LeagueId</param>
    public void SaveBeforeEnterDeskLeagueId(long leagueId)
    {
        beforeEnterDeskLeagueId = leagueId;
        GF.LogInfo($"[亲友圈返回] 保存进入桌子前的亲友圈房间ID: {leagueId}");
    }
    
    /// <summary>
    /// 查看进入桌子前的亲友圈房间ID（不清除）
    /// </summary>
    /// <returns>亲友圈房间的LeagueId，如果没有则返回0</returns>
    public long PeekBeforeEnterDeskLeagueId()
    {
        return beforeEnterDeskLeagueId;
    }
    
    /// <summary>
    /// 获取并清除进入桌子前的亲友圈房间ID（一次性使用）
    /// </summary>
    /// <returns>亲友圈房间的LeagueId，如果没有则返回0</returns>
    public long GetAndClearBeforeEnterDeskLeagueId()
    {
        long leagueId = beforeEnterDeskLeagueId;
        beforeEnterDeskLeagueId = 0; // 获取后清除
        GF.LogInfo($"[亲友圈返回] 获取并清除进入桌子前的亲友圈房间ID: {leagueId}");
        return leagueId;
    }
    
    /// <summary>
    /// 清除保存的亲友圈房间ID
    /// </summary>
    public void ClearBeforeEnterDeskLeagueId()
    {
        beforeEnterDeskLeagueId = 0;
        GF.LogInfo($"[亲友圈返回] 清除保存的亲友圈房间ID");
    }
    #endregion

    public static string ServerInfo_HouTaiIP = "xyxhoutai.xin";
    public static float TabletAspectThreshold = 1.7f; // 1.7f 平板一般是4:3

    public static bool IsLandscape()
    {
        var orientation = Screen.orientation;
        if (orientation == ScreenOrientation.LandscapeLeft || orientation == ScreenOrientation.LandscapeRight)
        {
            return true;
        }

        if (orientation == ScreenOrientation.Portrait || orientation == ScreenOrientation.PortraitUpsideDown)
        {
            return false;
        }

        return Screen.width > Screen.height;
    }

    //当前进入的俱乐部或者联盟信息
    private LeagueInfo _leagueInfo;

    public LeagueInfo LeagueInfo
    {
        get => _leagueInfo;
        set
        {
            if (_leagueInfo == value) return;
            var oldValue = _leagueInfo;
            _leagueInfo = value;
            GF.Event.Fire(this, ReferencePool.Acquire<ClubDataChangedEventArgs>().Fill(ClubDataType.eve_LeagueSwitch, oldValue, value));
        }
    }

    /// <summary>
    /// 我参与的俱乐部或者联盟信息 (包括我创建的), Key: LeagueId
    /// </summary>
    public IReadOnlyDictionary<long, LeagueInfo> MyJoinLeagueInfos => m_MyJoinLeagueInfos;
    private readonly Dictionary<long, LeagueInfo> m_MyJoinLeagueInfos = new Dictionary<long, LeagueInfo>();

    /// <summary>
    /// 我创建的俱乐部或者联盟信息
    /// </summary>
    public List<LeagueInfo> MyLeagueInfos
    {
        get
        {
            var mySelf = Util.GetMyselfInfo();
            if (mySelf == null)
            {
                return new List<LeagueInfo>();
            }
            return m_MyJoinLeagueInfos.Values.Where(info => info.Creator == mySelf.PlayerId).ToList();
        }
    }

    public static GlobalManager GetInstance()
    {
        if (instance == null)
        {
            instance = new GlobalManager();
        }
        return instance;
    }

    private string _cachedUuid = "";
    public string Uuid
    {
        get
        {
            // 每次获取时都尝试从系统获取设备ID
            var deviceId = SystemInfo.deviceUniqueIdentifier;
            if (string.IsNullOrEmpty(deviceId) || deviceId == SystemInfo.unsupportedIdentifier)
            {
                deviceId = PlayerPrefs.GetString("DeviceUUID", "");
                if (string.IsNullOrEmpty(deviceId))
                {
                    deviceId = System.Guid.NewGuid().ToString();
                    PlayerPrefs.SetString("DeviceUUID", deviceId);
                    PlayerPrefs.Save(); // 强制立即保存
                    GF.LogInfo("NewDeviceUUID: ", $"{deviceId}");
                }
            }
            else
            {
                // 如果成功获取到系统设备ID，更新存储的值
                if (PlayerPrefs.GetString("DeviceUUID", "") != deviceId)
                {
                    PlayerPrefs.SetString("DeviceUUID", deviceId);
                    PlayerPrefs.Save();
                    GF.LogInfo("UpdateDeviceUUID: ", $"{deviceId}");
                }
            }

            // 更新缓存值并记录日志
            if (_cachedUuid != deviceId)
            {
                _cachedUuid = deviceId;
                GF.LogInfo("设备ID已更新: ", $"{deviceId}");
            }

            return deviceId;
        }
        private set
        {
            _cachedUuid = value;
        }
    }

    public static int lineCount = 100;

    public void Init()
    {
        InitAddCoinTable();
        HotfixNetworkComponent.AddListener(MessageID.Msg_Error, OnFunckMsgError);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SystemConfigRs, OnFunckSystemConfigRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynCoinChange, Function_SynCoinChange);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynDiamondChange, Function_SynDiamondChange);
        HotfixNetworkComponent.AddListener(MessageID.Msg_LockRs, Function_LockRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_UnlockRs, Function_UnLockRs);
        HotfixNetworkComponent.AddListener(MessageID.Syn_Jackpot, Function_Syn_Jackpot);
        HotfixNetworkComponent.AddListener(MessageID.Msg_CreateDeskConfigRs, Function_CreateDeskConfigRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_GetDeskInfoRs, Function_GetDeskInfoRs);

        //我的联盟或者公会详细信息登录推送
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynLeagueDetail, Function_SynLeagueDetail);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MyLeagueListRs, FuncMyLeagueListRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynClubCurrencyChange, FuncSynClubCurrencyChange);

        //全局监听通知
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynRefuseBring, FuncSynRefuseBring);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynClick, Function_SynClick);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynPmdMsg, Function_SynPmdMsg);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynForbid, Function_SynForbid);

        // 门票功能
        HotfixNetworkComponent.AddListener(MessageID.Msg_MatchTicketsRs, OnMatchTicketsRs);
        HotfixNetworkComponent.AddListener(MessageID.Syn_MatchTicketsUpdate, OnSynMatchTicketsUpdate);
        HotfixNetworkComponent.AddListener(MessageID.Syn_Notice_Match_Start, OnSynNotice_Match_Start);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynMatching, OnSynMatching);

        // 祈福功能
        HotfixNetworkComponent.AddListener(MessageID.Msg_PrayConfigRs, OnPrayConfigRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_PrayRs, OnPrayRs);

        GF.Event.Subscribe(GFEventArgs.EventId, OnGFEventChanged);

    }

    public void RequestAllMsg()
    {
        SendCreateDeskConfigRq();
        SendSystemConfigRq();
        SendPrayConfigRq();
    }

    public async void AutoLogin(bool isdelay = false)
    {
        GF.LogInfo("自动登录 ");
        if (isdelay)
            await UniTask.Delay(1000);    // 延时 1 秒
        LoginOrCreateRq req = MessagePool.Instance.Fetch<LoginOrCreateRq>();
        req.AccountName = PlayerPrefs.GetString(PlayerPrefs.GetInt("loginType") == 0 ? "phone" : "email");

        string storedPassword = PlayerPrefs.GetString("pwd");
        if (!string.IsNullOrEmpty(storedPassword))
        {
            try
            {
                // 尝试解密。如果成功，则使用解密后的密码。
                req.Pwd = Util.Decrypt(storedPassword, Const.PWD_KEY);
            }
            catch (System.Exception)
            {
                // 如果解密失败，说明可能是未加密的旧密码，直接使用。
                req.Pwd = storedPassword;
            }
        }
        else
        {
            req.Pwd = string.Empty;
        }

        req.LoginType = PlayerPrefs.GetInt("loginType");
        req.Uuid = Util.GetFriendlyDeviceInfo();
        req.Gps = GFBuiltin.BuiltinView.GetGpsData();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.MsgLoginReq, req);
    }


    private void OnGFEventChanged(object sender, GameEventArgs e)
    {
        var args = e as GFEventArgs;
        // HomeProcedure homeProcedure = GF.Procedure.GetProcedure<HomeProcedure>() as HomeProcedure;
        switch (args.EventType)
        {
            case GFEventType.TCPReconnectSuccess:
                // homeProcedure.QuitGame(false);
                break;
            case GFEventType.TCPReconnectFailed:
                // homeProcedure.QuitGame(false);
                break;
        }
    }

    public void Clear()
    {
        LeagueInfo = null;
        m_MyJoinLeagueInfos.Clear();
        jpDefaultData = null;
        m_MatchTickets?.Clear();
        m_PrayConfigs?.Clear();
        PrayWealth = 0;
        PrayLuck = 0;
        MaxWealth = 0;
        MaxLuck = 0;
        RewardState = 0;
    }

    public void Function_SynLeagueDetail(MessageRecvData data)
    {
        Msg_SynLeagueDetail ack = Msg_SynLeagueDetail.Parser.ParseFrom(data.Data);
        GF.LogInfo("我的联盟或者公会详细信息登录推送", ack.ToString());
        m_MyJoinLeagueInfos[ack.Detail.LeagueId] = ack.Detail;
        GF.Event.Fire(this, ReferencePool.Acquire<ClubDataChangedEventArgs>().Fill(ClubDataType.eve_LeagueDateUpdate, null, null));
    }

    /// <summary>
    /// 我的公会列表
    /// </summary>
    /// <param name="data"></param>
    private void FuncMyLeagueListRs(MessageRecvData data)
    {
        try
        {
            Msg_MyLeagueListRs ack = Msg_MyLeagueListRs.Parser.ParseFrom(data.Data);
            GF.LogInfo("Msg_MyLeagueListRs我的公会列表", ack.ToString());
            var clubLeagueDataMap = new Dictionary<long, ClubLeagueData>();

            if (ack != null && ack.Self != null && ack.Self.Count != 0)
            {
                if (ack.Infos != null && Util.GetMyselfInfo() != null)
                {
                    m_MyJoinLeagueInfos.Clear();
                    foreach (var info in ack.Infos)
                    {
                        m_MyJoinLeagueInfos[info.LeagueId] = info;
                    }
                    foreach (var item in ack.Self)
                    {
                        try
                        {
                            clubLeagueDataMap[item.ClubId] = new ClubLeagueData
                            {
                                Gold = item.Gold,
                                Offline = item.Offline,
                                Remarks = item.Remarks,
                                Identify = item.Identify,
                                Rebate = item.Rebate
                            };
                        }
                        catch (Exception ex)
                        {
                            GF.LogError("解析俱乐部联盟数据失败: ", ex.Message);
                        }
                    }
                }
            }

            if (Util.GetMyselfInfo() != null)
            {
                Util.GetMyselfInfo().ClubLeagueDataMap = clubLeagueDataMap;
            }

            // 确保事件一定会被触发
            GF.Event.Fire(this, ReferencePool.Acquire<ClubDataChangedEventArgs>().Fill(ClubDataType.eve_LeagueDateUpdate, null, null));
        }
        catch (Exception ex)
        {
            GF.LogError("处理我的公会列表消息失败: ", ex.Message);
            // 即使出错也触发事件，确保UI能够更新
            GF.Event.Fire(this, ReferencePool.Acquire<ClubDataChangedEventArgs>().Fill(ClubDataType.eve_LeagueDateUpdate, null, null));
        }
    }

    /// <summary>
    /// 联盟币变动
    /// </summary>
    /// <param name="data"></param>
    private void FuncSynClubCurrencyChange(MessageRecvData data)
    {
        Msg_SynClubCurrencyChange ack = Msg_SynClubCurrencyChange.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_SynClubCurrencyChange联盟币变动", ack.ToString());
        if (Util.GetMyselfInfo().ClubLeagueDataMap.TryGetValue(ack.LeagueId, out var clubData))
        {
            clubData.Gold = ack.Amount;
        }
        if (m_MyJoinLeagueInfos.TryGetValue(ack.LeagueId, out var info))
        {
            info.LeagueGold = ack.Amount;
        }
        GF.Event.Fire(this, ReferencePool.Acquire<ClubDataChangedEventArgs>().Fill(ClubDataType.eve_LeagueDateUpdate, null, null));
    }

    /// <summary>
    /// 获取某个俱乐部的联盟币
    /// </summary>
    /// <param name="clubId">俱乐部ID</param>
    /// <returns>联盟币数量</returns>
    public float GetAllianceCoins(long clubId)
    {
        if (Util.GetMyselfInfo().ClubLeagueDataMap.TryGetValue(clubId, out var clubData))
        {
            if (float.TryParse(clubData.Gold, out float goldValue))
            {
                return goldValue;
            }
        }
        return 0; // 如果没有记录或转换失败则返回默认值
    }

    /// <summary>
    /// 错误码
    /// </summary>
    /// <param name="data"></param>
    private void OnFunckMsgError(MessageRecvData data)
    {
        Msg_Error ack = Msg_Error.Parser.ParseFrom(data.Data);
        GF.LogInfo("有错误弹窗.", ack.ToString());
        switch (ack.Error)
        {
            case -3: // 账号其他登录
                HomeProcedure homeProcedure = GF.Procedure.GetProcedure<HomeProcedure>() as HomeProcedure;
                homeProcedure.QuitGame();
                GF.UI.ShowToast("账号在其他设备登录");
                return;
            case -27:
                if (Util.InGameProcedure())
                {
                    HomeProcedure homeProcedure2 = GF.Procedure.GetProcedure<HomeProcedure>() as HomeProcedure;
                    homeProcedure2.QuitGame();
                }
                GF.UI.ShowToast("账号已被封禁");
                return;
            case -56:
                int maxTime = GlobalManager.GetConstants(28);
                GF.UI.ShowToast($"续时最长{maxTime}小时");
                return;
            case -4:
                GF.LogWarning("操作异常");
                return;
        }
        GF.StaticUI.ShowError(Util.GetErrorContent(ack.Error));
    }

    #region 创建房间配置消耗

    private RepeatedField<Msg_CreateDeskConfig> msg_CreateDeskConfig;

    public static void SendCreateDeskConfigRq()
    {
        Msg_CreateDeskConfigRq req = MessagePool.Instance.Fetch<Msg_CreateDeskConfigRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_CreateDeskConfigRq, req);
    }

    private void Function_CreateDeskConfigRs(MessageRecvData data)
    {
        Msg_CreateDeskConfigRs ack = Msg_CreateDeskConfigRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到创建房间配置.", ack.ToString());
        msg_CreateDeskConfig = ack.Config;
    }

    public int GetMsg_CreateDeskConfigByType(MethodType methodType, float baseCoin, DeskType type)
    {
        if (msg_CreateDeskConfig == null) return 0;
        // 1是个人房免费配置
        if (type == DeskType.Simple && GetConstants(24) == 1) return 0;
        int expend;
        Msg_CreateDeskConfig msg_CreateDesk = msg_CreateDeskConfig.FirstOrDefault(item => item.MethodType == methodType &&
            float.Parse(item.Start) <= baseCoin && float.Parse(item.End) > baseCoin);
        if (msg_CreateDesk == null)
        {
            expend = 0;
        }
        else
        {
            expend = msg_CreateDesk.BaseCoin;
        }
        return expend;
    }

    /// <summary>
    /// 按局数获取创建房间消耗钻石数
    /// </summary>
    /// <param name="methodType">玩法类型</param>
    /// <param name="round">局数</param>
    /// <param name="type">房间类型</param>
    /// <returns>消耗钻石数，未找到返回0</returns>
    public int GetCreateDeskCostByRound(MethodType methodType, int round, DeskType type)
    {
        if (msg_CreateDeskConfig == null) return 0;
        if (type == DeskType.Simple && GetConstants(24) == 1) return 0;

        // 筛选按局数计费(costType==1)且玩法类型匹配的配置
        Msg_CreateDeskConfig config = msg_CreateDeskConfig.FirstOrDefault(item =>
            item.MethodType == methodType && item.CostType == 1 && item.CostRound == round);
        return config?.BaseCoin ?? 0;
    }

    #endregion

    #region 系统常量

    public static void SendSystemConfigRq()
    {
        // 发送请求
        Msg_SystemConfigRq req = MessagePool.Instance.Fetch<Msg_SystemConfigRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_SystemConfigRq, req);
    }

    /// <summary>
    /// 钻石消耗常量
    /// </summary>
    /// <value></value>
    private static Dictionary<int, int> Constants { get; set; }
    /// <summary>
    /// 常量配置
    /// </summary>
    /// <param name="data"></param>
    private void OnFunckSystemConfigRs(MessageRecvData data)
    {
        Msg_SystemConfigRs ack = Msg_SystemConfigRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到系统常量配置.", ack.ToString());
        Constants ??= new Dictionary<int, int>();
        foreach (var pair in ack.Config)
        {
            if (!Constants.ContainsKey(pair.Key))
            {
                Constants.Add(pair.Key, pair.Val);
            }
            else
            {
                Constants[pair.Key] = pair.Val;
            }
        }
    }

    /// <summary>
    /// 获取常量，如果不存在则返回默认值 0
    /// </summary>
    /// <param name="key">常量键</param>
    /// <returns>常量值</returns>
    public static int GetConstants(int key)
    {
        return Constants != null && Constants.TryGetValue(key, out int value) ? value : 0;
    }

    #endregion


    /// <summary>
    /// 钻石变动
    /// </summary>
    public void Function_SynDiamondChange(MessageRecvData data)
    {
        Msg_SynDiamondChange ack = Msg_SynDiamondChange.Parser.ParseFrom(data.Data);
        long oldValue = Util.GetMyselfInfo().Diamonds;
        Util.GetMyselfInfo().Diamonds = ack.Diamond;
        GF.LogInfo("钻石变动", ack.ToString());
        GF.Event.Fire(this, ReferencePool.Acquire<UserDataChangedEventArgs>().Fill(UserDataType.DIAMOND, oldValue, ack.Diamond));
    }

    public void Function_SynCoinChange(MessageRecvData data)
    {
        Msg_SynCoinChange ack = Msg_SynCoinChange.Parser.ParseFrom(data.Data);
        long oldNum = (long)float.Parse(Util.GetMyselfInfo().Gold);
        Util.GetMyselfInfo().Gold = ack.Amount;
        GF.LogInfo("金币变动", ack.ToString());
        GF.Event.Fire(this, ReferencePool.Acquire<UserDataChangedEventArgs>().Fill(UserDataType.MONEY, oldNum, ack.Amount));
    }

    /// <summary>
    /// 上锁返回
    /// </summary>
    /// <param name="data"></param>
    private void Function_LockRs(MessageRecvData data)
    {
        GF.LogInfo("上锁返回");
        // Msg_LockRs ack = Msg_LockRs.Parser.ParseFrom(data.Data);
        GF.DataModel.GetDataModel<UserDataModel>().LockState = 1;
        GF.UI.ShowToast("已锁定");
        GF.Event.Fire(this, ReferencePool.Acquire<UserDataChangedEventArgs>().Fill(UserDataType.SafeCodeState, 0, 1));
    }

    /// <summary>
    /// 解锁返回
    /// </summary>
    /// <param name="data"></param>
    private void Function_UnLockRs(MessageRecvData data)
    {
        GF.LogInfo("解锁返回");
        // Msg_LockRs ack = Msg_LockRs.Parser.ParseFrom(data.Data);
        GF.DataModel.GetDataModel<UserDataModel>().LockState = 0;
        GF.UI.ShowToast("已解锁");
        GF.Event.Fire(this, ReferencePool.Acquire<UserDataChangedEventArgs>().Fill(UserDataType.SafeCodeState, 1, 0));
    }

    /// <summary>
    /// 解锁返回
    /// </summary>
    /// <param name="data"></param>
    private void FuncSynRefuseBring(MessageRecvData data)
    {
        GF.UI.ShowToast("管理员拒绝了你的带入申请");
    }

    #region 奖池配置

    public Dictionary<long, float> jackpotData = new();

    public void GetJackpotData(long clubId, out float total)
    {
        if (jackpotData.TryGetValue(clubId, out total))
        {
            return;
        }
        total = 0;
    }

    public void Function_Syn_Jackpot(MessageRecvData data)
    {
        Syn_Jackpot ack = Syn_Jackpot.Parser.ParseFrom(data.Data);
        GF.LogInfo("Syn_Jackpot奖池信息更新", ack.ToString());
        jackpotData[ack.ClubId] = float.TryParse(ack.Total, out float total) ? total : 0;
        GF.Event.Fire(this, ReferencePool.Acquire<ClubDataChangedEventArgs>().Fill(ClubDataType.eve_LeagueJackpotUpdate, null, null));
    }

    public class DefaultJackpotConfig
    {
        public MethodType MethodType { get; set; }
        public RepeatedField<Msg_BaseJackpot> BaseConfig { get; set; }
        public RepeatedField<PairInt> RateConfig { get; set; }
        public int Donate { get; set; }
    }

    private Dictionary<MethodType, DefaultJackpotConfig> jpDefaultData = new();

    public DefaultJackpotConfig GetJpDefaultData(MethodType methodType)
    {
        if (jpDefaultData == null || !jpDefaultData.ContainsKey(methodType))
        {
            InitJackpotDefaultData();
        }
        return jpDefaultData[methodType];
    }

    public void InitJackpotDefaultData()
    {
        // 定义默认数据
        DefaultJackpotConfig defaultConfig_nn = new()
        {
            MethodType = MethodType.NiuNiu,
            BaseConfig = new RepeatedField<Msg_BaseJackpot>
            {
                new Msg_BaseJackpot { BaseCoin = "0.1", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "0.25", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "0.5", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "1.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "2.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "3.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "5.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "10.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "20.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "25.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "50.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "100.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "200.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "300.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "500.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "1000.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
            },
            RateConfig = new RepeatedField<PairInt>
            {
                new PairInt { Key = 17, Val = 8 },
                new PairInt { Key = 16, Val = 6 },
                new PairInt { Key = 15, Val = 4 },
                new PairInt { Key = 14, Val = 2 },
            },
            Donate = 0
        };
        DefaultJackpotConfig defaultConfig_zjh = new()
        {
            MethodType = MethodType.GoldenFlower,
            BaseConfig = new RepeatedField<Msg_BaseJackpot>
            {
                new Msg_BaseJackpot { BaseCoin = "0.1", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "0.25", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "0.5", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "1.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "2.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "3.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "5.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "10.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "20.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "25.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "50.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "100.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "200.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "300.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "500.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "1000.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
            },
            RateConfig = new RepeatedField<PairInt>
            {
                new PairInt { Key = 5, Val = 3 },
                new PairInt { Key = 4, Val = 2 },
            },
            Donate = 0
        };
        DefaultJackpotConfig defaultConfig_dz = new()
        {
            MethodType = MethodType.TexasPoker,
            BaseConfig = new RepeatedField<Msg_BaseJackpot>
            {
                new Msg_BaseJackpot { BaseCoin = "0.1", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "0.25", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "0.5", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "1.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "2.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "3.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "5.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "10.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "20.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "25.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "50.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "100.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "200.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "300.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "500.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "1000.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
            },
            RateConfig = new RepeatedField<PairInt>
            {
                new PairInt { Key = 7, Val = 5 },
                new PairInt { Key = 8, Val = 10 },
                new PairInt { Key = 9, Val = 15 },
            },
            Donate = 0
        };
        DefaultJackpotConfig defaultConfig_cc = new()
        {
            MethodType = MethodType.CompareChicken,
            BaseConfig = new RepeatedField<Msg_BaseJackpot>
            {
                new Msg_BaseJackpot { BaseCoin = "0.1", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "0.25", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "0.5", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "1.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "2.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "3.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "5.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "10.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "20.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "25.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "50.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "100.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "200.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "300.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "500.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
                new Msg_BaseJackpot { BaseCoin = "1000.0", Pot = "0", Profit = "0", Rate = "0" }, // 10 倍
            },
            RateConfig = new RepeatedField<PairInt>
            {
                //14, 7, 10, 5, 13, 6
                new PairInt { Key = 5, Val = 2 },
                new PairInt { Key = 10, Val = 3 },
                new PairInt { Key = 7, Val = 4 },
                new PairInt { Key = 14, Val = 5 },
            },
            Donate = 0
        };
        jpDefaultData = new Dictionary<MethodType, DefaultJackpotConfig>();
        jpDefaultData[MethodType.NiuNiu] = defaultConfig_nn;
        jpDefaultData[MethodType.GoldenFlower] = defaultConfig_zjh;
        jpDefaultData[MethodType.TexasPoker] = defaultConfig_dz;
        jpDefaultData[MethodType.CompareChicken] = defaultConfig_cc;
    }

    public string GetMethodRules(MethodType methodType)
    {
        string rule = "";
        switch (methodType)
        {
            case MethodType.NiuNiu:
                rule = "1.当前牌局如果参与玩家达到4人，则自动开启Jackpot彩池，不足4人不激活奖励也不抽取JP，开启后玩家击中特定牌型，即可获得彩池奖励;\n" +
                "2.投入彩池: 当前牌局设置为每手牌盈利数额达到特定数额时，系统自动从中拿出例如（1BB）投入Jackpot奖池。\n" +
                "3.击中特定牌型指的是:\n" +
                "同花顺牛，五小牛，炸弹牛，葫芦牛\n" +
                "4.同一把同时出多个指定牌型，都会获得奖池奖励。\n" +
                "5.注：有癞子牌型不触发奖池。\n";
                break;
            case MethodType.GoldenFlower:
                rule = "1.当前牌局如果参与玩家达到4人，则自动开启Jackpot彩池，不足4人不激活奖励也不抽取JP，开启后玩家击中特定牌型，即可获得彩池奖励;\n" +
                "2.投入彩池: 当前牌局设置为每手牌盈利数额达到特定数额时，系统自动从中拿出例如（1BB）投入Jackpot奖池。\n" +
                "3. 击中特定牌型指的是:\n" +
                "顺金，豹子\n" +
                "4.同一把同时出多个指定牌型，都会获得奖池奖励。\n" +
                "5.拥有特定牌型的玩家必须亮牌才可视为击中，获得奖励。\n";
                break;
            case MethodType.TexasPoker:
                rule = "1.当前手如果参与玩家达到4人，则自动开启Jackpot彩池，不足4人不激活奖励也不抽取JP，开启后玩家击中特定牌型，即可获得彩池奖励;\n" +
                "2.击中特定牌型指的是:\n" +
                "(1)玩家的2张手牌与公共牌共同组成了【皇家同花顺】【同花顺】【四条】其中一种。击中4条时，必须玩家的2张手牌为其中一对\n" +
                "(2)拥有特定牌型的玩家必须亮牌并且获胜才可视为击中，获得奖励。\n" +
                "*玩家与对手坚持到最后比牌阶段，系统会自动亮牌。\n" +
                "3.投入彩池: 当前牌局设置为每手牌盈利数额达到特定数额时，系统自动从中拿出1BB投入Jackpot奖池\n";
                break;
            case MethodType.CompareChicken:
                rule = "1.当前牌局如果参与玩家达到4人，则自动开启Jackpot彩池，不足4人不激活奖励也不抽取JP，开启后玩家击中特定牌型，即可获得彩池奖励;\n" +
                "2.投入彩池: 当前牌局设置为每手牌盈利数额达到特定数额时，系统自动从中拿出例如（1BB）投入Jackpot奖池。\n" +
                "3.击中特定牌型指的是:\n" +
                "双四头，全三条，清连顺，三顺清，四个头，双三条\n" +
                "4.同一把同时出多个指定牌型，都会获得奖池奖励。\n" +
                "5.注：有癞子牌型不触发奖池。\n";
                break;
            default:
                break;
        }
        return rule;
    }

    #endregion

    public void Function_SynClick(MessageRecvData data)
    {
        GF.LogInfo("通知玩家被踢出2000228");
        GF.UI.ShowToast("您已被踢出房间");
    }

    public void Function_SynPmdMsg(MessageRecvData data)
    {
        Msg_SynPmdMsg ack = Msg_SynPmdMsg.Parser.ParseFrom(data.Data);
        GF.LogInfo("跑马灯信息 ", ack.ToString());
        if (IsLandscape())
            GF.StaticUI.ShowNotificationLandscape(ack.Msg);
        else
            GF.StaticUI.ShowNotification(ack.Msg);
    }

    public void Function_SynForbid(MessageRecvData data)
    {
        //Msg_SynForbid
        GF.LogInfo("通知玩家禁言2000226");
        Msg_SynForbid ack = Msg_SynForbid.Parser.ParseFrom(data.Data);
        GF.UI.ShowToast(ack.Type == 1 ? "您已被禁言" : "您已解除禁言");
    }

    public void ReSetUI()
    {
        GF.UI.CloseAllLoadingUIForms();
        GF.UI.CloseAllLoadedUIForms();
        GF.Entity.HideAllLoadingEntities();
        GF.Entity.HideAllLoadedEntities();
        Util.GetInstance().CloseAllLoadingDialogs();
    }

    #region Daily Actions

    private static List<string> s_DailyActionNames = new List<string>();

    /// <summary>
    /// 重置所有每日操作记录。
    /// </summary>
    public static void ResetAllDailyActions()
    {
        foreach (var actionName in s_DailyActionNames)
        {
            string key = $"LastExecDate_{actionName}";
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
            }
        }
        s_DailyActionNames.Clear();
        PlayerPrefs.Save();
        GF.LogInfo("所有日常动作均已重置");
    }

    /// <summary>
    /// 检查指定的每日操作今天是否可以执行。
    /// </summary>
    /// <param name="actionName">操作的唯一名称。</param>
    /// <returns>如果操作今天尚未执行，则返回 true；否则返回 false。</returns>
    public static bool CanExecuteDailyAction(string actionName)
    {
        string key = $"LastExecDate_{actionName}";
        if (PlayerPrefs.HasKey(key))
        {
            string lastExecDateStr = PlayerPrefs.GetString(key);
            if (DateTime.TryParse(lastExecDateStr, out DateTime lastExecDate))
            {
                return lastExecDate.Date < DateTime.Today;
            }
        }
        // 如果没有记录或者解析失败，则认为可以执行
        return true;
    }

    /// <summary>
    /// 记录每日操作已在今天执行。
    /// </summary>
    /// <param name="actionName">操作的唯一名称。</param>
    public static void RecordDailyAction(string actionName)
    {
        if (!s_DailyActionNames.Contains(actionName))
        {
            s_DailyActionNames.Add(actionName);
        }
        string key = $"LastExecDate_{actionName}";
        PlayerPrefs.SetString(key, DateTime.Today.ToString("yyyy-MM-dd"));
        PlayerPrefs.Save();
    }

    #endregion

    #region 比赛数据管理

    //新建一个底注涨注表 等级一是25 每三分钟涨一次
    public List<AddCoinLevelData> m_AddCoinTable = new List<AddCoinLevelData>();

    public void InitAddCoinTable()
    {
        m_AddCoinTable.Clear();
        m_AddCoinTable.Add(new AddCoinLevelData(1, 25));
        m_AddCoinTable.Add(new AddCoinLevelData(2, 50));
        m_AddCoinTable.Add(new AddCoinLevelData(3, 75));
        m_AddCoinTable.Add(new AddCoinLevelData(4, 100));
        m_AddCoinTable.Add(new AddCoinLevelData(5, 125));
        m_AddCoinTable.Add(new AddCoinLevelData(6, 150));
        m_AddCoinTable.Add(new AddCoinLevelData(7, 200));
        m_AddCoinTable.Add(new AddCoinLevelData(8, 250));
        m_AddCoinTable.Add(new AddCoinLevelData(9, 300));
        m_AddCoinTable.Add(new AddCoinLevelData(10, 400));
        m_AddCoinTable.Add(new AddCoinLevelData(11, 500));
        m_AddCoinTable.Add(new AddCoinLevelData(12, 600));
        m_AddCoinTable.Add(new AddCoinLevelData(13, 750));
        m_AddCoinTable.Add(new AddCoinLevelData(14, 1000));
        m_AddCoinTable.Add(new AddCoinLevelData(15, 1300));
        m_AddCoinTable.Add(new AddCoinLevelData(16, 1800));
        m_AddCoinTable.Add(new AddCoinLevelData(17, 2000));
        m_AddCoinTable.Add(new AddCoinLevelData(18, 2500));
        m_AddCoinTable.Add(new AddCoinLevelData(19, 3000));
        m_AddCoinTable.Add(new AddCoinLevelData(20, 4000));
        m_AddCoinTable.Add(new AddCoinLevelData(21, 5000));
        m_AddCoinTable.Add(new AddCoinLevelData(22, 6000));
        m_AddCoinTable.Add(new AddCoinLevelData(23, 8000));
        m_AddCoinTable.Add(new AddCoinLevelData(24, 10000));
        m_AddCoinTable.Add(new AddCoinLevelData(25, 12000));
        m_AddCoinTable.Add(new AddCoinLevelData(26, 16000));
        m_AddCoinTable.Add(new AddCoinLevelData(27, 19000));
        m_AddCoinTable.Add(new AddCoinLevelData(28, 24000));
        m_AddCoinTable.Add(new AddCoinLevelData(29, 30000));
        m_AddCoinTable.Add(new AddCoinLevelData(30, 40000));
        m_AddCoinTable.Add(new AddCoinLevelData(31, 47500));
        m_AddCoinTable.Add(new AddCoinLevelData(32, 60000));
        m_AddCoinTable.Add(new AddCoinLevelData(33, 80000));
        m_AddCoinTable.Add(new AddCoinLevelData(34, 95000));
        m_AddCoinTable.Add(new AddCoinLevelData(35, 120000));
        m_AddCoinTable.Add(new AddCoinLevelData(36, 155000));
        m_AddCoinTable.Add(new AddCoinLevelData(37, 190000));
        m_AddCoinTable.Add(new AddCoinLevelData(38, 240000));
        m_AddCoinTable.Add(new AddCoinLevelData(39, 300000));
        m_AddCoinTable.Add(new AddCoinLevelData(40, 400000));
        m_AddCoinTable.Add(new AddCoinLevelData(41, 475000));
        m_AddCoinTable.Add(new AddCoinLevelData(42, 600000));
        m_AddCoinTable.Add(new AddCoinLevelData(43, 800000));
        m_AddCoinTable.Add(new AddCoinLevelData(44, 1000000));
        m_AddCoinTable.Add(new AddCoinLevelData(45, 1200000));
        m_AddCoinTable.Add(new AddCoinLevelData(46, 1600000));
        m_AddCoinTable.Add(new AddCoinLevelData(47, 2000000));
        m_AddCoinTable.Add(new AddCoinLevelData(48, 2500000));
        m_AddCoinTable.Add(new AddCoinLevelData(49, 3000000));
        m_AddCoinTable.Add(new AddCoinLevelData(50, 4000000));
    }

    #endregion

    #region 门票功能

    private readonly Dictionary<int, Msg_Ticket> m_MatchTickets = new();

    /// <summary>
    /// 获取指定比赛的门票信息。
    /// </summary>
    /// <param name="_matchId">比赛ID</param>
    /// <returns>门票信息，如果不存在则返回 null。</returns>
    public Msg_Ticket GetMatchTicket(int _matchId)
    {
        m_MatchTickets.TryGetValue(_matchId, out var ticket);
        return ticket;
    }

    /// <summary>
    /// 获取指定比赛的门票数量。
    /// </summary>
    /// <param name="_matchId">比赛ID</param>
    /// <returns>门票数量。</returns>
    public int GetMatchTicketCount(int _matchId)
    {
        if (m_MatchTickets.TryGetValue(_matchId, out var ticket))
        {
            return ticket.Num;
        }
        return 0;
    }

    /// <summary>
    /// 获取所有门票信息。
    /// </summary>
    /// <returns>所有门票的列表。</returns>
    public List<Msg_Ticket> GetAllMatchTickets()
    {
        return m_MatchTickets.Values.ToList();
    }

    /// <summary>
    /// 全量门票更新
    /// </summary>
    private void OnMatchTicketsRs(MessageRecvData _data)
    {
        Msg_MatchTicketsRs ack = Msg_MatchTicketsRs.Parser.ParseFrom(_data.Data);
        GF.LogInfo("收到全量门票信息更新", ack.ToString());
        m_MatchTickets?.Clear();
        foreach (var ticket in ack.Tickets)
        {
            if (ticket.Num > 0)
            {
                m_MatchTickets[ticket.MatchId] = ticket;
            }
        }
        GF.Event.Fire(this, ReferencePool.Acquire<UserDataChangedEventArgs>().Fill(UserDataType.TICKETS, null, null));
    }

    /// <summary>
    /// 增量门票更新
    /// </summary>
    private void OnSynMatchTicketsUpdate(MessageRecvData _data)
    {
        Syn_MatchTicketsUpdate ack = Syn_MatchTicketsUpdate.Parser.ParseFrom(_data.Data);
        GF.LogInfo("收到增量门票信息更新", ack.ToString());
        if (ack.Tickets != null)
        {
            if (ack.Tickets.Num > 0)
            {
                if (m_MatchTickets.ContainsKey(ack.Tickets.MatchId))
                {
                    m_MatchTickets[ack.Tickets.MatchId] = ack.Tickets;
                }
                else
                {
                    m_MatchTickets.Add(ack.Tickets.MatchId, ack.Tickets);
                }
            }
            else
            {
                if (m_MatchTickets.ContainsKey(ack.Tickets.MatchId))
                {
                    m_MatchTickets.Remove(ack.Tickets.MatchId);
                }
            }
            GF.Event.Fire(this, ReferencePool.Acquire<UserDataChangedEventArgs>().Fill(UserDataType.TICKETS, null, null));
        }
    }

    /// <summary>
    /// 比赛开始通知
    /// </summary>
    private void OnSynNotice_Match_Start(MessageRecvData _data)
    {
        Syn_Notice_Match_Start ack = Syn_Notice_Match_Start.Parser.ParseFrom(_data.Data);
        GF.LogInfo("收到比赛开始通知", ack.ToString());
        GF.UI.ShowToast("您参与的比赛即将开始,请准备!");
    }

    private void OnSynMatching(MessageRecvData _data)
    {
        Syn_Matching ack = Syn_Matching.Parser.ParseFrom(_data.Data);
        GF.LogInfo("收到进入匹配通知", ack.ToString());
        // GF.UI.ShowToast("您已进入匹配!");
        GF.StaticUI.ShowMatching(true);
    }

    #endregion


    #region 分享链接

    public static List<string> allUrls = new();

    public static string[] Get_allShareUrls()
    {
        return allUrls.ToArray();
    }

    #endregion

    #region 同步游戏状态

    public void Function_GetDeskInfoRq()
    {
        Msg_GetDeskInfoRq req = MessagePool.Instance.Fetch<Msg_GetDeskInfoRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_GetDeskInfoRq, req);
    }

    private void Function_GetDeskInfoRs(MessageRecvData _data)
    {
        Msg_GetDeskInfoRs ack = Msg_GetDeskInfoRs.Parser.ParseFrom(_data.Data);
        GF.LogInfo("获取牌桌信息返回2000119", ack.ToString());
        Util.GetMyselfInfo().Gold = ack.Gold.ToString();
        Util.GetMyselfInfo().Diamonds = ack.Diamonds;
        Util.GetMyselfInfo().DeskId = ack.DeskId;
        Util.GetMyselfInfo().MethodType = ack.MethodType;
        // 如果用户在桌子上，进行游戏状态同步
        var myselfInfo = Util.GetMyselfInfo();
        if (myselfInfo != null && myselfInfo.DeskId > 0)
        {
            GF.LogInfo($"[快速重连] 检测到用户在桌子 {myselfInfo.DeskId} 上，进行游戏状态同步");
            // 显示游戏状态同步的等待框
            Util.GetInstance().ShowWaiting("正在同步游戏状态...", "GameStateSync");
            // 使用现有的进入桌子协议同步最新游戏状态
            GF.Event.Fire(this, ReferencePool.Acquire<GFEventArgs>().Fill(GFEventType.eve_ReConnectGame, null));
        }
    }

    #endregion

    #region 祈福配置数据
    
    /// <summary>
    /// 祈福配置列表缓存
    /// </summary>
    private List<Msg_PrayConfig> m_PrayConfigs = new List<Msg_PrayConfig>();
    
    /// <summary>
    /// 当前财运值
    /// </summary>
    public int PrayWealth { get; set; }
    
    /// <summary>
    /// 当前幸运值
    /// </summary>
    public int PrayLuck { get; set; }
    
    /// <summary>
    /// 财运值上限
    /// </summary>
    public int MaxWealth { get; set; }
    
    /// <summary>
    /// 幸运值上限
    /// </summary>
    public int MaxLuck { get; set; }
    
    /// <summary>
    /// 神秘礼包领取状态：0-未领取，1-今天已领取
    /// </summary>
    public int RewardState { get; set; }
    
    /// <summary>
    /// 获取祈福配置
    /// </summary>
    /// <param name="prayId">祈福配置ID</param>
    /// <returns>祈福配置，如果不存在则返回null</returns>
    public Msg_PrayConfig GetPrayConfig(long prayId)
    {
        return m_PrayConfigs.Find(config => config.Id == prayId);
    }
    
    /// <summary>
    /// 请求祈福配置
    /// </summary>
    public static void SendPrayConfigRq()
    {
        Msg_PrayConfigRq req = MessagePool.Instance.Fetch<Msg_PrayConfigRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(
            HotfixNetworkComponent.AccountClientName, 
            MessageID.Msg_PrayConfigRq, 
            req);
        GF.LogInfo("[祈福] 请求祈福配置");
    }
    
    /// <summary>
    /// 祈福配置返回处理
    /// </summary>
    /// <param name="data">消息数据</param>
    private void OnPrayConfigRs(MessageRecvData data)
    {
        Msg_PrayConfigRs ack = Msg_PrayConfigRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("[祈福] 收到祈福配置", ack.ToString());
        
        // 缓存祈福配置
        m_PrayConfigs.Clear();
        if (ack.Config != null)
        {
            foreach (var config in ack.Config)
            {
                m_PrayConfigs.Add(config);
            }
        }
        
        // 缓存当前祈福数据
        PrayWealth = ack.PrayWealth;
        PrayLuck = ack.PrayLuck;
        MaxWealth = ack.MaxWealth;
        MaxLuck = ack.MaxLuck;
        RewardState = ack.RewardState;
        
        GF.LogInfo($"[祈福] 配置缓存成功: 配置数量={m_PrayConfigs.Count}, 财运={PrayWealth}/{MaxWealth}, 幸运={PrayLuck}/{MaxLuck}, 礼包状态={RewardState}");
    }
    
    /// <summary>
    /// 祈福返回处理 - 只更新缓存数据
    /// </summary>
    /// <param name="data">消息数据</param>
    private void OnPrayRs(MessageRecvData data)
    {
        Msg_PrayRs ack = Msg_PrayRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("[祈福] 祈福返回", ack.ToString());
        
        // 更新祈福数据缓存
        PrayWealth = ack.PrayWealth;
        PrayLuck = ack.PrayLuck;
        
        // 更新对应配置的已祈福次数
        var config = m_PrayConfigs.Find(c => c.Id == ack.PrayId);
        if (config != null)
        {
            config.ReadyPray = ack.ReadyPray;
        }
        
        GF.LogInfo($"[祈福] 数据缓存更新: 当前财运={PrayWealth}/{MaxWealth}, 当前幸运={PrayLuck}/{MaxLuck}, 祈福次数={ack.ReadyPray}");
    }

    #endregion


}

#region 比赛底注涨注表结构
public struct AddCoinLevelData
{
    public int Level;      // 等级
    public int SmallBlind; // 底注
    public int AddTime;    // 涨注时间（秒）

    public AddCoinLevelData(int _level, int _smallBlind, int _addTime = 180)
    {
        Level = _level;
        SmallBlind = _smallBlind;
        AddTime = _addTime;
    }
}
#endregion
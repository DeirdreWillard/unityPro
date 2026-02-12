using System;
using UnityEngine;

/// <summary>
/// 麻将设置管理类 - 2D独立版本
/// 管理麻将牌样式、视图模式、语音等设置
/// 从 MahjongMachine/MahjongAssetsMgr 中提取并简化
/// </summary>
public static class MahjongSettings
{
    #region 常量定义

    // PlayerPrefs 键名
    private const string PREF_MJ_CARD_STYLE = "MahjongCardStyle";
    private const string PREF_VIEW_MODE = "MahjongViewMode";
    private const string PREF_VOICE_LANGUAGE = "MahjongVoiceLanguage";
    private const string PREF_VOICE_GENDER = "MahjongVoiceGender";
    private const string PREF_TABLE_BG = "MahjongTableBg";

    #endregion

    #region 事件委托

    /// <summary>
    /// 麻将牌样式改变事件（使用Action简化委托）
    /// 订阅此事件可在样式切换时自动更新所有麻将预制体
    /// </summary>
    public static event Action<MjColor> OnCardStyleChanged;

    #endregion

    #region 麻将牌样式设置

    private static MjColor _currentCardStyle = MjColor.lv;
    private static bool _isCardStyleInitialized = false;

    /// <summary>
    /// 初始化麻将牌样式（从本地缓存加载）
    /// </summary>
    public static void InitializeCardStyle()
    {
        if (_isCardStyleInitialized) return;

        int savedStyle = PlayerPrefs.GetInt(PREF_MJ_CARD_STYLE, (int)MjColor.lv);
        _currentCardStyle = (MjColor)savedStyle;
        _isCardStyleInitialized = true;

        GF.LogInfo($"[麻将设置] 加载牌样式: {_currentCardStyle}");
    }

    /// <summary>
    /// 获取当前麻将牌样式
    /// </summary>
    public static MjColor GetCardStyle()
    {
        if (!_isCardStyleInitialized)
            InitializeCardStyle();
        return _currentCardStyle;
    }

    /// <summary>
    /// 设置麻将牌样式
    /// </summary>
    public static void SetCardStyle(MjColor style)
    {
        _currentCardStyle = style;
        PlayerPrefs.SetInt(PREF_MJ_CARD_STYLE, (int)style);
        PlayerPrefs.Save();

        GF.LogInfo($"[麻将设置] 切换牌样式: {style}");

        // 触发样式改变事件，通知所有订阅者更新UI
        OnCardStyleChanged?.Invoke(style);
    }

    /// <summary>
    /// 获取当前样式索引（1=绿色, 2=黄色, 3=蓝色）
    /// </summary>
    public static int GetCardStyleIndex()
    {
        return (int)GetCardStyle() + 1;
    }

    /// <summary>
    /// 获取当前样式名称
    /// </summary>
    public static string GetCardStyleName()
    {
        switch (GetCardStyle())
        {
            case MjColor.lv: return "绿色";
            case MjColor.huang: return "黄色";
            case MjColor.lan: return "蓝色";
            default: return "未知";
        }
    }

    #endregion

    #region 视图模式设置

    private static MahjongViewMode _currentViewMode = MahjongViewMode.View2D;
    private static bool _isViewModeInitialized = false;

    /// <summary>
    /// 初始化视图模式（从本地缓存加载）
    /// </summary>
    public static void InitializeViewMode()
    {
        if (_isViewModeInitialized) return;

        int savedViewMode = PlayerPrefs.GetInt(PREF_VIEW_MODE, (int)MahjongViewMode.View2D);
        _currentViewMode = (MahjongViewMode)savedViewMode;

        _isViewModeInitialized = true;

        GF.LogInfo($"[麻将设置] 加载视图模式: {_currentViewMode}");
    }

    /// <summary>
    /// 获取视图模式
    /// </summary>
    public static MahjongViewMode GetViewMode()
    {
        if (!_isViewModeInitialized)
            InitializeViewMode();
        return _currentViewMode;
    }

    /// <summary>
    /// 设置视图模式
    /// </summary>
    public static void SetViewMode(MahjongViewMode viewMode)
    {
        _currentViewMode = viewMode;
        PlayerPrefs.SetInt(PREF_VIEW_MODE, (int)viewMode);
        PlayerPrefs.Save();

        GF.LogInfo($"[麻将设置] 切换视图模式: {viewMode}");
    }

    /// <summary>
    /// 是否为2D模式
    /// </summary>
    public static bool Is2DMode()
    {
        return GetViewMode() == MahjongViewMode.View2D;
    }

    #endregion

    #region 语音设置

    private static VoiceLanguage _currentLanguage = VoiceLanguage.XianTao;
    private static PlayerType _currentGender = PlayerType.FEMALE;
    private static bool _isVoiceInitialized = false;

    /// <summary>
    /// 初始化语音设置（从本地缓存加载）
    /// </summary>
    public static void InitializeVoice()
    {
        if (_isVoiceInitialized) return;

        int savedLanguage = PlayerPrefs.GetInt(PREF_VOICE_LANGUAGE, (int)VoiceLanguage.XianTao);
        _currentLanguage = (VoiceLanguage)savedLanguage;

        int savedGender = PlayerPrefs.GetInt(PREF_VOICE_GENDER, (int)PlayerType.FEMALE);
        _currentGender = (PlayerType)savedGender;

        _isVoiceInitialized = true;

        GF.LogInfo($"[麻将设置] 加载语音: {_currentLanguage} / {_currentGender}");
    }

    /// <summary>
    /// 设置语言
    /// </summary>
    public static void SetLanguage(VoiceLanguage language)
    {
        _currentLanguage = language;
        PlayerPrefs.SetInt(PREF_VOICE_LANGUAGE, (int)language);
        PlayerPrefs.Save();

        GF.LogInfo($"[麻将设置] 切换语言: {language}");
    }

    /// <summary>
    /// 获取当前语言
    /// </summary>
    public static VoiceLanguage GetLanguage()
    {
        if (!_isVoiceInitialized)
            InitializeVoice();
        return _currentLanguage;
    }

    /// <summary>
    /// 设置性别
    /// </summary>
    public static void SetGender(PlayerType gender)
    {
        // 强制初始化（避免未初始化导致的问题）
        if (!_isVoiceInitialized)
        {
            InitializeVoice();
        }
        
        _currentGender = gender;
        PlayerPrefs.SetInt(PREF_VOICE_GENDER, (int)gender);
        PlayerPrefs.Save();

        GF.LogInfo($"[麻将设置] 切换性别: {gender}");
    }

    /// <summary>
    /// 获取当前性别
    /// </summary>
    public static PlayerType GetGender()
    {
        if (!_isVoiceInitialized)
            InitializeVoice();
        return _currentGender;
    }

    /// <summary>
    /// 根据玩家ID获取语音性别（基于哈希算法，同一玩家始终相同）
    /// </summary>
    /// <param name="playerId">玩家ID，0表示自己</param>
    /// <returns>玩家的语音性别</returns>
    public static PlayerType GetGenderForPlayer(long playerId)
    {
        if (!_isVoiceInitialized)
            InitializeVoice();

        // 自己使用本地设置
        if (Util.IsMySelf(playerId))
        {
            return _currentGender;
        }

        // 其他玩家：根据playerId哈希计算，保证同一玩家声音始终一致
        // 使用简单的奇偶判断，也可以用更复杂的哈希算法
        PlayerType gender = (playerId % 2 == 0) ? PlayerType.FEMALE : PlayerType.MALE;

        return gender;
    }

    #endregion

    #region 桌布设置

    private static int _currentTableBg = 1; // 默认样式1
    private static bool _isTableBgInitialized = false;

    /// <summary>
    /// 初始化桌布设置（从本地缓存加载）
    /// </summary>
    public static void InitializeTableBg()
    {
        if (_isTableBgInitialized) return;

        _currentTableBg = PlayerPrefs.GetInt(PREF_TABLE_BG, 1); // 默认样式1
        _isTableBgInitialized = true;

        GF.LogInfo($"[麻将设置] 加载桌布: 样式{_currentTableBg}");
    }

    /// <summary>
    /// 获取当前桌布样式
    /// </summary>
    public static int GetTableBg()
    {
        if (!_isTableBgInitialized)
            InitializeTableBg();
        return _currentTableBg;
    }

    /// <summary>
    /// 设置桌布样式
    /// </summary>
    public static void SetTableBg(int tableBg)
    {
        _currentTableBg = tableBg;
        PlayerPrefs.SetInt(PREF_TABLE_BG, tableBg);
        PlayerPrefs.Save();

        GF.LogInfo($"[麻将设置] 切换桌布: 样式{tableBg}");
    }

    #endregion

    #region 胡牌提示设置

    /// <summary>
    /// 胡牌提示开关:是否显示已出完的牌(剩余数量为0或负数的牌)
    /// true = 显示所有理论可胡牌(包括已出完的)
    /// false = 只显示有剩余数量的可胡牌
    /// </summary>
    public static bool showExhaustedCardsInHuTips = false;

    /// <summary>
    /// 胡牌提示开关:是否在听牌提示中显示当前赖子牌
    /// true = 显示赖子牌作为可胡选项
    /// false = 忽略赖子牌,不在听牌提示中显示
    /// </summary>
    public static bool showLaiziInHuTips = true;

    #endregion

    #region 统一初始化

    /// <summary>
    /// 统一初始化所有设置
    /// </summary>
    public static void InitializeAll()
    {
        InitializeCardStyle();
        InitializeViewMode();
        InitializeVoice();
        InitializeTableBg();
    }

    #endregion
}

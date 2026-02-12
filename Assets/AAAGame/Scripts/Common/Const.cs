/// <summary>
/// 热更Const
/// </summary>
[Obfuz.ObfuzIgnore]
public static partial class Const
{
    /// <summary>
    /// 服务器类型枚举
    /// </summary>
    public enum ServerType { 本地服, 测试服, 外网服 }
    
    /// <summary>
    /// 当前服务器类型
    /// </summary>
    public static ServerType CurrentServerType { get; set; } = ServerType.本地服;
    
    public static string ServerInfo_ServerIP = "192.168.31.82";
    public static int ServerInfo_ServerPort = 7001;
    public static string ServerInfo_BackEndIP = "192.168.31.12";
    public static int ServerInfo_BackEndPort = 80;
    public static string HttpStr = CurrentServerType == ServerType.外网服 ? "https://" : "http://";
    public static bool ServerInfo_IpListFlag = false;
    public static string ServerInfo_IpListUrl = HttpStr + "43.100.123.94/ip.txt";

    internal const long DefaultVibrateDuration = 50;//安卓手机震动强度
    internal static readonly float SHOW_CLOSE_INTERVAL = 1f;//出现关闭按钮的延迟
    internal static readonly string PWD_KEY = "aB5@z#9S";//加密解密key

    internal static class Tags
    {
        public static readonly string Player = "Player";
        public static readonly string AIPlayer = "AIPlayer";
    }
    internal static class UserData
    {
        internal static readonly string MONEY = "UserData.MONEY";

        internal static readonly string DIAMOND = "UserData.DIAMOND";
        internal static readonly string GUIDE_ON = "UserData.GUIDE_ON";

        internal static readonly string SHOW_RATING_COUNT = "UserData.SHOW_RATING_COUNT";
        internal static readonly string GAME_LEVEL = "UserData.GAME_LEVEL";
        internal static readonly string CAR_SKIN_ID = "UserData.CAR_SKIN_ID";

        internal static readonly string USER_SPAWN_POINT_TYPE = "UserData.USER_SPAWN_POINT_TYPE";
    }

    public static class UIParmKey
    {
        /// <summary>
        /// 点返回关闭界面
        /// </summary>
        public static readonly string EscapeClose = "EscapeClose";
        /// <summary>
        /// UI打开关闭动画
        /// </summary>
        public static readonly string OpenAnimType = "OpenAnimType";
        public static readonly string CloseAnimType = "CloseAnimType";
        /// <summary>
        /// UI层级
        /// </summary>
        public static readonly string SortOrder = "SortOrder";
        /// <summary>
        /// 按钮回调
        /// </summary>
        public static readonly string OnButtonClick = "OnButtonClick";
        public static readonly string OnShow = "OnShow";
        public static readonly string OnHide = "OnHide";
    }

    //其他游戏相关常量
    public static readonly int MaxPlayerNum = 8;
}

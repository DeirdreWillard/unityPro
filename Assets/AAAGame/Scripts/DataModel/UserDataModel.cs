using GameFramework;
using NetMsg;
using System.Collections.Generic;

/// <summary>
/// 玩家在俱乐部联盟中的数据
/// </summary>
public class ClubLeagueData
{
    /// <summary>
    /// 联盟币
    /// </summary>
    public string Gold { get; set; }

    /// <summary>
    /// 离线时间 0表示在线
    /// </summary>
    public long Offline { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string Remarks { get; set; }

    /// <summary>
    /// 身份: 9会长 0普通成员 1队长
    /// </summary>
    public int Identify { get; set; }

    /// <summary>
    /// 分成百分比
    /// </summary>
    public int Rebate { get; set; }
}

public class UserDataModel : DataModelBase
{
    #region Private Fields
    private int m_code = 1; //登录返回 0=登录成功 <0 登录失败
    private string m_accountName = ""; //账户名
    private long m_playerId = 0;//玩家ID
    private string m_nickName = "";//昵称
    private string m_headIndex = "";//头像
    private string m_gold ;//欢乐豆
    private long m_diamonds = 7;//钻石
    private long m_coin = 8;//积分
    private int m_identity = 9;//身份
    private int m_league = 10;//所属联盟
    private int m_guild = 11;//所属

    private int m_lockState = 12;//解锁状态
    private int m_vipState = 13;//vip开启状态
    private long m_vipEndTime = 14;//vip结束时间
    private int m_deskId = 0;//当前所在房间ID

    private int m_modifyNickNum = 0;
    private int m_modifyHeadNum = 0;
    private int m_methodType = 0;

    private Dictionary<long, ClubLeagueData> m_clubLeagueDataMap = new(); // 映射俱乐部ID到联盟数据
    #endregion

    #region Public Properties
    /// <summary>
    /// 俱乐部联盟数据映射
    /// </summary>
    public Dictionary<long, ClubLeagueData> ClubLeagueDataMap
    {
        get { return m_clubLeagueDataMap; }
        set { m_clubLeagueDataMap = value; }
    }

    /// <summary>
    /// 账户名
    /// </summary>
    public string AccountName
    {
        get
        {
            return m_accountName;
        }
        set
        {
            m_accountName = value; 
        }
    }

    /// <summary>
    /// 玩家ID
    /// </summary>
    public long PlayerId
    {
        get
        {
            return m_playerId;
        }
        set
        {
            m_playerId = value;
        }
    }
    /// <summary>
    /// 昵称
    /// </summary>
    public string NickName
    {
        get
        {
            return m_nickName;
        }
        set
        { 
            GF.LogInfo("开始改名发送事件");
            //通知个人界面修改名字显示
            GF.Event.Fire(this, ReferencePool.Acquire<UserDataChangedEventArgs>().Fill(UserDataType.Nick, m_nickName, value));
            m_nickName = value;
        }
    }

    /// <summary>
    /// 头像
    /// </summary>
    public string HeadIndex
    {
        get
        {
            return m_headIndex;
        }
        set
        {
            GF.Event.Fire(this, ReferencePool.Acquire<UserDataChangedEventArgs>().Fill(UserDataType.avatar, m_headIndex, value));
            m_headIndex = value;
        }
    }

    /// <summary>
    /// 金币
    /// </summary>
    public string Gold
    {
        get
        {
            return Util.FormatAmount(m_gold);
        }
        set
        {
            m_gold = value;
        }
    }
    /// <summary>
    /// 钻石
    /// </summary>
    public long Diamonds
    {
        get
        {
            return m_diamonds;
        }
        set
        {
            m_diamonds = value;
        }
    }

    /// <summary>
    /// 积分
    /// </summary>
    public long Coin
    {
        get
        {
            return m_coin;
        }
        set
        {
            m_coin = value;
        }
    }

    /// <summary>
    /// 身份
    /// </summary>
    public int Identity
    {
        get
        {
            return m_identity;
        }
        set
        {
            m_identity = value;
        }
    }

    /// <summary>
    /// 所属联盟
    /// </summary>
    public int League
    {
        get
        {
            return m_league;
        }
        set
        {
            m_league = value;
        }
    }
    /// <summary>
    /// 所属
    /// </summary>
    public int Guild
    {
        get
        {
            return m_guild;
        }
        set
        {
            m_guild = value;
        }
    }

    /// <summary>
    /// 解锁状态
    /// </summary>
    public int LockState
    {
        get
        {
            return m_lockState;
        }
        set
        {
            m_lockState = value;
        }
    }

    
    /// <summary>
    /// vip开启状态
    /// </summary>
    public int VipState
    {
        get
        {
            return m_vipState;
        }
        set
        {
            m_vipState = value;
        }
    }

    
    
    /// <summary>
    /// vip结束时间
    /// </summary>
    public long VipEndTime
    {
        get
        {
            return m_vipEndTime;
        }
        set
        {
            m_vipEndTime = value;
        }
    }

    /// <summary>
    /// 当前所在房间ID
    /// </summary>
    public int DeskId
    {
        get
        {
            return m_deskId;
        }
        set
        {
            m_deskId = value;
        }
    }

    public int ModifyNickNum
    {
        get
        {
            return m_modifyNickNum;
        }
        set
        {
            m_modifyNickNum = value;
        }
    }

    public int ModifyHeadNum
    {
        get
        {
            return m_modifyHeadNum;
        }
        set
        {
            m_modifyHeadNum = value;
        }
    }
    
    public int MethodType
    {
        get
        {
            return m_methodType;
        }
        set
        {
            m_methodType = value;
        }
    }
    #endregion

    #region Public Methods
    public void InitUserData(LoginOrCreateRs userData)
    {
        AccountName = userData.AccountName; //账户名
        PlayerId = userData.PlayerId;//玩家ID
        NickName = userData.NickName;//昵称
        HeadIndex = userData.HeadIndex;//头像
        Gold = userData.Gold;//金币
        Diamonds = userData.Diamonds;//钻石
        Identity = userData.Identity;//身份
        League = userData.League;//所属联盟
        Guild = userData.Guild;//所属

        LockState = userData.LockState;//身份
        VipState = userData.VipState;//所属联盟
        VipEndTime = userData.VipEndTime;//所属
        DeskId = userData.DeskId;//当前所在房间ID
        ModifyNickNum = userData.ModifyNickNum;
        ModifyHeadNum = userData.ModifyHeadNum;
        MethodType = userData.MethodType;
    }
    #endregion
}

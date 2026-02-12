//using GameFramework;
//using Google.Protobuf.WellKnownTypes;
//using NetMsg;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityGameFramework.Runtime;
//public class UserClubDataModel : DataModelBase
//{
//    private int code = 1; //登录返回 0=登录成功 <0 登录失败
//    private string accountName = ""; //账户名
//    private long playerId = 0;//玩家ID
//    private string nickName = "";//昵称
//    private string headIndex = "";//头像
//    private long gold = 6;//金币
//    private long diamonds = 7;//钻石
//    private long coin = 8;//积分
//    private int identity = 9;//身份
//    private int league = 10;//所属联盟
//    private int guild = 11;//所属



//    /// <summary>
//    /// 账户名
//    /// </summary>
//    public string AccountName
//    {
//        get
//        {
//            return accountName;
//        }
//        set
//        {
//            accountName = value;
//        }
//    }

//    /// <summary>
//    /// 玩家ID
//    /// </summary>
//    public long PlayerId
//    {
//        get
//        {
//            return playerId;
//        }
//        set
//        {
//            playerId = value;
//        }
//    }
//    /// <summary>
//    /// 昵称
//    /// </summary>
//    public string NickName
//    {
//        get
//        {
//            return nickName;
//        }
//        set
//        {
//            GF.LogInfo("开始改名发送事件");
//            //通知个人界面修改名字显示
//            GF.Event.Fire(this, ReferencePool.Acquire<UserDataChangedEventArgs>().Fill(UserDataType.Nick, nickName, value));
//            nickName = value;
//        }
//    }

//    /// <summary>
//    /// 头像
//    /// </summary>
//    public string HeadIndex
//    {
//        get
//        {
//            return headIndex;
//        }
//        set
//        {
//            headIndex = value;
//        }
//    }

//    /// <summary>
//    /// 金币
//    /// </summary>
//    public long Gold
//    {
//        get
//        {
//            return gold;
//        }
//        set
//        {
//            gold = value;
//        }
//    }
//    /// <summary>
//    /// 钻石
//    /// </summary>
//    public long Diamonds
//    {
//        get
//        {
//            return diamonds;
//        }
//        set
//        {
//            diamonds = value;
//        }
//    }

//    /// <summary>
//    /// 积分
//    /// </summary>
//    public long Coin
//    {
//        get
//        {
//            return coin;
//        }
//        set
//        {
//            coin = value;
//        }
//    }

//    /// <summary>
//    /// 身份
//    /// </summary>
//    public int Identity
//    {
//        get
//        {
//            return identity;
//        }
//        set
//        {
//            identity = value;
//        }
//    }

//    /// <summary>
//    /// 所属联盟
//    /// </summary>
//    public int League
//    {
//        get
//        {
//            return league;
//        }
//        set
//        {
//            league = value;
//        }
//    }
//    /// <summary>
//    /// 所属
//    /// </summary>
//    public int Guild
//    {
//        get
//        {
//            return guild;
//        }
//        set
//        {
//            guild = value;
//        }
//    }

//    public void InitUserData(LoginOrCreateRs userData)
//    {
//        AccountName = userData.AccountName; //账户名
//        PlayerId = userData.PlayerId;//玩家ID
//        NickName = userData.NickName;//昵称
//        HeadIndex = userData.HeadIndex;//头像
//        Gold = userData.Gold;//金币
//        Diamonds = userData.Diamonds;//钻石
//        Coin = userData.Coin;//积分
//        Identity = userData.Identity;//身份
//        League = userData.League;//所属联盟
//        Guild = userData.Guild;//所属
//    }

//    public string FormatBigNumber(double number)
//    {
//        double temp = number;
//        string suffix = "";

//        if (Math.Abs(temp) >= 1000000000)
//        {
//            temp /= 1000000000;
//            suffix = "B";
//        }
//        else if (Math.Abs(temp) >= 1000000)
//        {
//            temp /= 1000000;
//            suffix = "M";
//        }
//        else if (Math.Abs(temp) >= 1000)
//        {
//            temp /= 1000;
//            suffix = "K";
//        }

//        return $"{temp:N2}{suffix}";
//    }
//}

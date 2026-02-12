using GameFramework;
using System;
using System.Collections.Generic;
using UnityEngine;
public class PlayerDataModel : DataModelBase
{
    public int Coins
    {
        get
        {
            return GF.Setting.GetInt(Const.UserData.MONEY, GF.Config.GetInt("DEFAULT_COINS"));
        }
        set
        {
            int oldNum = Coins;
            int fixedNum = Mathf.Max(0, value);
            GF.Setting.SetInt(Const.UserData.MONEY, fixedNum);
            FireUserDataChanged(UserDataType.MONEY, oldNum, fixedNum);
        }
    }
 

    public int Diamond {
        get {
            return GF.Setting.GetInt(Const.UserData.DIAMOND, GF.Config.GetInt("DEFAULT_DIAMOND"));
        }
        set {
            int oldNum = Diamond;
            int fixedNum = Mathf.Max(0, value);
            GF.Setting.SetInt(Const.UserData.DIAMOND, fixedNum);
            FireUserDataChanged(UserDataType.DIAMOND, oldNum, fixedNum);
        }
    }

    internal void ClaimMoney(int bonus, bool showFx, Vector3 fxSpawnPos)
    {
        int initMoney = this.Coins;
        this.Coins += bonus;
        GF.Event.Fire(this, ReferencePool.Acquire<PlayerEventArgs>().Fill(PlayerEventType.ClaimMoney, new Dictionary<string, object>
        {
            ["ShowFX"] = showFx,
            ["SpawnPoint"] = fxSpawnPos,
            ["StartNum"] = initMoney
        }));
    }
    internal Vector2Int GetOfflineBonus()
    {
        int offlineFactor = GF.Config.GetInt("OfflineFactor");
        int bonus = GF.Config.GetInt("OfflineBonus");
        int bonusMulti = GF.Config.GetInt("OfflineAdBonusMulti");
        int maxBonus = GF.Config.GetInt("MaxOfflineBonus");
        var offlineMinutes = GetOfflineTime().TotalMinutes;
        Vector2Int result = Vector2Int.zero;

        result.x = Mathf.Clamp(bonus * Mathf.FloorToInt((float)offlineMinutes / offlineFactor), 0, maxBonus);
        result.y = Mathf.CeilToInt(result.x * bonusMulti);
        return result;
    }
    internal TimeSpan GetOfflineTime()
    {
        string dTimeStr = GF.Setting.GetString(ConstBuiltin.Setting.QuitAppTime, string.Empty);
        if (string.IsNullOrWhiteSpace(dTimeStr) || !DateTime.TryParse(dTimeStr, out DateTime exitTime))
        {
            return TimeSpan.Zero;
        }
        return System.DateTime.UtcNow.Subtract(exitTime);
    }
    internal bool IsNewDay()
    {
        string dTimeStr = GF.Setting.GetString(ConstBuiltin.Setting.QuitAppTime, string.Empty);
        if (string.IsNullOrWhiteSpace(dTimeStr) || !DateTime.TryParse(dTimeStr, out DateTime dTime))
        {
            return true;
        }

        var today = DateTime.Today;
        return !(today.Year == dTime.Year && today.Month == dTime.Month && today.Day == dTime.Day);
    }
    /// <summary>
    /// ????????????????
    /// </summary>
    /// <param name="tp"></param>
    /// <param name="udt"></param>
    private void FireUserDataChanged(UserDataType tp, object oldValue, object value)
    {
        GF.Event.Fire(this, ReferencePool.Acquire<UserDataChangedEventArgs>().Fill(tp, oldValue, value));
    }

    internal int GetMultiReward(int rewardNum, int multi)
    {
        return rewardNum * multi;
    }
}

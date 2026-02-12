using System.Collections;
using System.Collections.Generic;
using GameFramework.Event;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;

public partial class Monetary : MonoBehaviour
{
    // 货币相关的UI逻辑

    public Text varGoldText;
    public Text varDimText;
    public Text varLeagueCoinText;
    public GameObject union;

    private bool unionInitiallyActive = true; // 记录union的初始激活状态
    private bool eventsSubscribed = false; // 记录事件是否已订阅

    private void Start()
    {
        // 记录union的初始激活状态
        if (union != null)
        {
            unionInitiallyActive = union.activeInHierarchy;
        }

        if (transform.parent != null)
        {
            InitializeAsSubComponent();
        }
    }

    /// <summary>
    /// 作为子组件时的初始化
    /// </summary>
    private void InitializeAsSubComponent()
    {
        if (!eventsSubscribed)
        {
            try
            {
                GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
                GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
                eventsSubscribed = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to subscribe events in InitializeAsSubComponent: {ex.Message}");
            }
        }

        // 延迟初始化，确保UI完全加载
        StartCoroutine(DelayedInitialization());
    }

    /// <summary>
    /// 公共初始化方法，可被外部调用
    /// </summary>
    public void Initialize()
    {
        if (!eventsSubscribed)
        {
            try
            {
                GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
                GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
                eventsSubscribed = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to subscribe events: {ex.Message}");
            }
        }
        UpdateCurrencyDisplay();
    }

    public async void OnDim()
    {
        if(Util.IsClickLocked()) return;
        var item = await GF.UI.OpenUIFormAwait(UIViews.MJShopPanel);
        //默认选择钻石toogle
        item.GetComponent<MJShopPanel>().varShopContent.transform.Find("钻石").GetComponent<Toggle>().isOn = true;
    }
    public async void OnGold()
    {
        if(Util.IsClickLocked()) return;
        var item = await GF.UI.OpenUIFormAwait(UIViews.MJShopPanel);
        //默认选择金币toogle
        item.GetComponent<MJShopPanel>().varShopContent.transform.Find("金币").GetComponent<Toggle>().isOn = true;
    }
    public async void OnUnion()
    {
        if(Util.IsClickLocked()) return;
       // await GF.UI.OpenUIFormAwait(UIViews.GetUnion);
    }
    /// <summary>
    /// 公共清理方法，可被外部调用
    /// </summary>
    public void Cleanup()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void UnsubscribeEvents()
    {
        if (eventsSubscribed)
        {
            try
            {
                GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
            }
            catch (System.Exception ex)
            {
                // 忽略取消订阅失败的异常，防止程序崩溃
                Debug.LogWarning($"Failed to unsubscribe UserDataChangedEventArgs: {ex.Message}");
            }

            try
            {
                GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
            }
            catch (System.Exception ex)
            {
                // 忽略取消订阅失败的异常，防止程序崩溃
                Debug.LogWarning($"Failed to unsubscribe ClubDataChangedEventArgs: {ex.Message}");
            }

            eventsSubscribed = false;
        }
    }
    /// <summary>
    /// 延迟初始化，确保UI组件完全加载
    /// </summary>
    private IEnumerator DelayedInitialization()
    {
        yield return new WaitForEndOfFrame();
        UpdateCurrencyDisplay();
    }

    /// <summary>
    /// 更新货币显示
    /// </summary>
    private void UpdateCurrencyDisplay()
    {
        // 更新金币和钻石
        varGoldText.FormatAmount(Util.GetMyselfInfo().Gold);
        varDimText.FormatAmount(Util.GetMyselfInfo().Diamonds);
        // 根据当前所在类型判断union显示状态和联盟币显示
        var leagueInfo = GlobalManager.GetInstance().LeagueInfo;
        bool showUnion = false;
        string leagueCoinText = "0";

        if (leagueInfo != null)
        {
            switch (leagueInfo.Type)
            {
                case 0:
                    showUnion = leagueInfo.Father != 0;
                    break;

                case 1:
                case 2:
                    showUnion = true;
                    break;
            }
            if (showUnion)
            {
                var leagueCoinValue = GlobalManager.GetInstance().GetAllianceCoins(leagueInfo.LeagueId);
                leagueCoinText = leagueCoinValue.ToString();
            }
        }
        varLeagueCoinText.text = leagueCoinText;
        if (union != null)
        {
            // 如果union初始时是隐藏的，则永远保持隐藏状态
            if (!unionInitiallyActive)
            {
                union.SetActive(false);
            }
            else
            {
                union.SetActive(showUnion);
            }
        }
    }
    private void OnUserDataChanged(object sender, GameEventArgs e)
    {
        var args = e as UserDataChangedEventArgs;

        switch (args.Type)
        {
            case UserDataType.MONEY:
                varGoldText.FormatAmount(Util.GetMyselfInfo().Gold);
                break;
            case UserDataType.DIAMOND:
                varDimText.FormatAmount(Util.GetMyselfInfo().Diamonds);
                break;
            case UserDataType.LEAGUE_COIN:
                UpdateCurrencyDisplay();
                break;
        }
    }

    /// <summary>
    /// 俱乐部数据变化事件处理
    /// </summary>
    private void OnClubDataChanged(object sender, GameEventArgs e)
    {
        var args = e as ClubDataChangedEventArgs;

        switch (args.Type)
        {
            case ClubDataType.eve_LeagueSwitch:
                // 切换俱乐部时更新所有货币显示和union状态
                UpdateCurrencyDisplay();
                break;
            case ClubDataType.eve_LeagueDateUpdate:
                // 俱乐部信息更新时也刷新所有货币显示和union状态
                UpdateCurrencyDisplay();
                break;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using Google.Protobuf;
using NetMsg;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using System.Linq;
using static UtilityBuiltin;
using GameFramework.Event;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class MoreQyq : UIFormBase
{
    // 存储待处理的UI项和对应的亲友圈数据
    private Dictionary<long, GameObject> pendingUIItems = new Dictionary<long, GameObject>();
    private Dictionary<long, LeagueInfo> pendingLeagueData = new Dictionary<long, LeagueInfo>();
    private long activeRequestLeagueId = -1; // 当前正在请求楼层信息的亲友圈ID
    private HashSet<long> activeClubIds = new HashSet<long>(); // 当前界面管理的所有ClubId
    public Sprite[] sprites;

    // 背景相关
    private const string ROOM_BACKGROUND_INDEX_KEY = "PersonalitySet_BackgroundIndex";
    public Sprite[] backgroundSprites; // 背景图片数组
    private int currentBackgroundIndex = 0; // 当前背景索引

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        // 添加网络消息监听
        AddNetworkListeners();

        // 订阅背景变化事件
        SubscribeToBackgroundEvents();

        // 【修复】订阅亲友圈数据更新事件，当数据变化时自动刷新界面
        GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnLeagueDataChanged);

        // 同步大厅背景
        var bg = transform.Find("bg");
        if (bg != null)
        {
            LoadBackgroundSetting(bg);
        }
        else
        {
            GF.LogWarning("未找到bg对象，无法设置背景");
        }

        varInputQyqName.SetActive(false);

        // 【修复】统一使用 RefreshLeagueList 刷新列表，避免代码冗余
        RefreshLeagueList();
    }

    protected override void OnReveal()
    {
        base.OnReveal();
        // GF.LogInfo_wl("[MoreQyq] 界面重新显示，刷新列表...");
        // 【修复】当从其他界面（如创建房间）返回时，确保数据是最新的
        RefreshLeagueList();
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "创建亲友圈":
                varInputQyqName.SetActive(true);
                break;
            case "加入亲友圈":
                ShowJoinFriendCircle(0);
                break;
            case "确任":
                HandleConfirm();
                break;
            case "退出":
                varQYQAccount.text = "";
                varInputQyqName.SetActive(false);
                break;
        }
    }
    /// <summary>
    /// 加入亲友圈
    /// </summary>
    /// <param name="data"></param>
    private async void JoinLeague(LeagueInfo data)
    {
        Sound.PlayEffect(AudioKeys.SOUND_BTN);
        await GF.UI.OpenUIFormAwait(UIViews.CreatMJRoom);
    }

    /// <summary>
    /// 加入联盟
    /// </summary>
    /// <param name="data"></param>
    private async void JoinAlliance(LeagueInfo data)
    {
        //播放通用点击音效
        Sound.PlayEffect(AudioKeys.SOUND_BTN);
        await GF.UI.OpenUIFormAwait(UIViews.CreatMJRoom);

    }

    public async void ShowJoinFriendCircle(int imageIndex)
    {
        var joinFriendCircleForm = await GF.UI.OpenUIFormAwait(UIViews.JoinFriendCircle);
        //根据索引来改变这个预制体中的图片
        if (joinFriendCircleForm != null)
        {
            // 获取JoinFriendCircle组件并调用ChangeTopImage方法
            var joinFriendCircle = joinFriendCircleForm.GetComponent<JoinFriendCircle>();
            if (joinFriendCircle != null)
            {
                joinFriendCircle.ChangeTopImage(imageIndex);
            }
        }
    }

    /// <summary>
    /// 处理创建亲友圈确认操作
    /// </summary>
    private void HandleConfirm()
    {

    }
    /// <summary>
    /// 设置玩法图片
    /// </summary>
    /// <param name="imageComponent">图片组件</param>
    /// <param name="mjMethod">麻将玩法</param>
    private void SetGameMethodImage(Image imageComponent, NetMsg.MJMethod mjMethod)
    {
        try
        {
            string spriteName = GetMJMethodSpriteName(mjMethod);

            // 从sprites数组中查找对应名称的图片
            Sprite methodSprite = sprites?.FirstOrDefault(s => s.name == spriteName);

            if (methodSprite != null && imageComponent != null)
            {
                imageComponent.sprite = methodSprite;
                GF.LogInfo_wl($"成功设置{mjMethod}玩法图片: {spriteName}");
            }
            else
            {
                GF.LogWarning($"未找到玩法{mjMethod}对应的图片: {spriteName}，或图片组件为空");
            }
        }
        catch (System.Exception ex)
        {
            GF.LogInfo_wl($"设置玩法图片时发生错误: {ex.Message}");
        }
    }
    /// <summary>
    /// 根据游戏类型（MethodType）设置玩法图片
    /// </summary>
    /// <param name="imageComponent">图片组件</param>
    /// <param name="methodType">游戏类型</param>
    private void SetGameMethodImageByType(Image imageComponent, MethodType methodType)
    {
        try
        {
            string spriteName = GetMethodTypeSpriteName(methodType);

            // 从sprites数组中查找对应名称的图片
            Sprite methodSprite = sprites?.FirstOrDefault(s => s.name == spriteName);

            if (methodSprite != null && imageComponent != null)
            {
                imageComponent.sprite = methodSprite;
                // GF.LogInfo_wl($"成功设置{methodType}玩法图片: {spriteName}");
            }
            else
            {
                //GF.LogWarning($"未找到玩法{methodType}对应的图片: {spriteName}，或图片组件为空");
            }
        }
        catch (System.Exception ex)
        {
            GF.LogError($"设置玩法图片时发生错误: {ex.Message}");
        }
    }
    /// <summary>
    /// 根据游戏类型获取对应的图片名称
    /// </summary>
    /// <param name="methodType">游戏类型</param>
    /// <returns>图片名称</returns>
    private string GetMethodTypeSpriteName(MethodType methodType)
    {
        switch (methodType)
        {
            case MethodType.RunFast:
                return "跑的快"; // 跑的快图片名称
            case MethodType.MjSimple:
                return "大众麻将"; // 默认麻将图片
            default:
                return "未知玩法";
        }
    }

    /// <summary>
    /// 根据玩法类型获取对应的图片名称
    /// </summary>
    /// <param name="mjMethod">麻将玩法</param>
    /// <returns>图片名称</returns>
    private string GetMJMethodSpriteName(NetMsg.MJMethod mjMethod)
    {
        switch (mjMethod)
        {
            case NetMsg.MJMethod.Dazhong:
                return "大众麻将"; // 大众麻将图片名称
            case NetMsg.MJMethod.Huanghuang:
                return "仙桃晃晃"; // 仙桃晃晃图片名称
            case NetMsg.MJMethod.Kwx:
                return "卡五星"; // 卡五星图片名称
            case NetMsg.MJMethod.Xl:
                return "血流成河"; // 血流成河图片名称
            case NetMsg.MJMethod.Xz:
                return "血流成河"; // 血流成河图片名称
            default:
                GF.LogWarning($"未知的麻将玩法类型: {mjMethod}，使用默认图片名称");
                return "default"; // 默认图片名称
        }
    }


    /// <summary>
    /// 进入指定楼层
    /// </summary>
    /// <param name="leagueData">亲友圈数据</param>
    /// <param name="floorData">楼层数据</param>
    private async void EnterSpecificFloor(LeagueInfo leagueData, Msg_Floor floorData)
    {
        Sound.PlayEffect(AudioKeys.SOUND_BTN);
        // 打开CreatMjRoom界面
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("TargetFloor", floorData.ToByteArray());
        await GF.UI.OpenUIFormAwait(UIViews.CreatMJRoom, uiParams);
    }
    /// <summary>
    /// 添加网络消息监听
    /// </summary>
    private void AddNetworkListeners()
    {
        HotfixNetworkComponent.AddListener(MessageID.Msg_GetMjFloorRs, OnGetMjFloorListResponse);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynFloorChange, OnFloorChangeNotification);
        // GF.LogInfo_wl("MoreQyq: 已添加楼层信息和楼层变化网络监听");
    }

    /// <summary>
    /// 移除网络消息监听
    /// </summary>
    private void RemoveNetworkListeners()
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_GetMjFloorRs, OnGetMjFloorListResponse);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynFloorChange, OnFloorChangeNotification);
        GF.LogInfo_wl("MoreQyq: 已移除楼层信息和楼层变化网络监听");
    }

    /// <summary>
    /// 隐藏所有玩法图片
    /// </summary>
    /// <param name="itemUI">UI项</param>
    private void HideAllGameImages(GameObject itemUI)
    {
        for (int i = 0; i < 3; i++)
        {
            Transform imageContainer = itemUI.transform.Find($"wanfa{i + 1}");
            if (imageContainer != null)
            {
                imageContainer.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 请求亲友圈楼层信息
    /// </summary>
    /// <param name="leagueId">亲友圈ID</param>
    private void RequestLeagueFloorInfos(long leagueId)
    {
        if (pendingLeagueData.TryGetValue(leagueId, out var data))
        {
            activeRequestLeagueId = leagueId; // 【新增】记录当前请求的ID
            Msg_GetMjFloorRq rq = MessagePool.Instance.Fetch<Msg_GetMjFloorRq>();
            rq.ClubId = GetCorrectClubId(data);

            //GF.LogInfo_wl($"发送楼层信息请求: LeagueId={leagueId}, RequestClubId={rq.ClubId}");
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_GetMjFloorRq, rq);
        }
        else
        {
            ProcessNextLeague();
        }
    }
    /// <summary>
    /// 收到楼层信息响应
    /// </summary>
    private void OnGetMjFloorListResponse(MessageRecvData data)
    {

        Msg_GetMjFloorRs ack = Msg_GetMjFloorRs.Parser.ParseFrom(data.Data);

        // 【消息过滤】只处理当前界面管理的ClubId消息
        if (!activeClubIds.Contains(ack.ClubId) && ack.ClubId != 0)
        {

            return;
        }

        // 【修复】传入响应的 ClubId 进行校验
        ProcessCurrentLeagueFloors(ack.ClubId, ack.FloorInfo.ToList());


    }

    /// <summary>
    /// 处理当前亲友圈的楼层信息
    /// </summary>
    /// <param name="responseClubId">服务器返回的ClubId</param>
    /// <param name="floorInfos">楼层信息列表</param>
    private void ProcessCurrentLeagueFloors(long responseClubId, List<Msg_Floor> floorInfos)
    {
        try
        {
            // 【修复】使用 activeRequestLeagueId 替代 First()，并校验 ClubId
            if (activeRequestLeagueId != -1 && pendingUIItems.TryGetValue(activeRequestLeagueId, out GameObject uiItem))
            {
                long leagueId = activeRequestLeagueId;
                LeagueInfo leagueData = pendingLeagueData[leagueId];

                // 校验返回的 ClubId 是否匹配（考虑到 GetCorrectClubId 的转换逻辑）
                long expectedClubId = GetCorrectClubId(leagueData);
                if (responseClubId == expectedClubId)
                {
                    // 更新UI
                    UpdateFloorImages(uiItem, leagueData, floorInfos);
                }
                else
                {
                    GF.LogWarning($"楼层信息不匹配: 预期ClubId={expectedClubId}, 实际收到={responseClubId}。跳过更新UI。");
                }

                // 无论是否匹配，都从待处理队列中移除并处理下一个
                pendingUIItems.Remove(leagueId);
                pendingLeagueData.Remove(leagueId);
                activeRequestLeagueId = -1;

                // 继续处理下一个亲友圈
                ProcessNextLeague();
            }
            else
            {
                GF.LogWarning("收到楼层响应但没有对应的待处理请求");
                ProcessNextLeague();
            }
        }
        catch (System.Exception ex)
        {
            GF.LogError($"处理当前亲友圈楼层信息时发生错误: {ex.Message}");
            activeRequestLeagueId = -1;
            ProcessNextLeague();
        }
    }

    /// <summary>
    /// 处理下一个亲友圈
    /// </summary>
    private void ProcessNextLeague()
    {
        if (pendingUIItems.Count > 0)
        {
            var nextPending = pendingUIItems.First();
            // GF.LogInfo_wl($"继续处理下一个亲友圈，ID: {nextPending.Key}，剩余{pendingUIItems.Count}个");

            // 延时发送下一个请求，避免网络请求过于密集
            StartCoroutine(DelayedRequestNextFloor(nextPending.Key, 0.1f));
        }
        else
        {
            // GF.LogInfo_wl("所有亲友圈楼层信息处理完成");
        }
    }


    /// <summary>
    /// 根据楼层信息更新玩法图片
    /// </summary>
    /// <param name="itemUI">亲友圈项UI</param>
    /// <param name="leagueData">亲友圈数据</param>
    /// <param name="floorInfos">楼层信息列表</param>
    private void UpdateFloorImages(GameObject itemUI, LeagueInfo leagueData, List<Msg_Floor> floorInfos)
    {
        // 先隐藏所有玩法图片
        HideAllGameImages(itemUI);

        if (floorInfos != null && floorInfos.Count > 0)
        {
            // 最多显示3个楼层的玩法图片
            int maxFloors = Mathf.Min(floorInfos.Count, 3);

            for (int i = 0; i < maxFloors; i++)
            {
                var floorData = floorInfos[i];

                // 查找对应的图片容器 wanfa1, wanfa2, wanfa3
                Transform imageContainer = itemUI.transform.Find($"wanfa{i + 1}");
                if (imageContainer != null)
                {
                    Button imageButton = imageContainer.GetComponent<Button>();
                    Image gameImage = imageContainer.GetComponent<Image>();
                    if (imageButton != null && gameImage != null)
                    {
                        // 【修复】根据 MethodType 设置玩法图片
                        if (floorData.MethodType == MethodType.RunFast)
                        {
                            // 跑得快玩法
                            SetGameMethodImageByType(gameImage, MethodType.RunFast);
                        }
                        else if (floorData.MethodType == MethodType.MjSimple && floorData.MjConfig != null)
                        {
                            // 麻将玩法
                            SetGameMethodImage(gameImage, floorData.MjConfig.MjMethod);
                        }
                        else
                        {
                            // 其他玩法或数据异常，使用默认图片
                            GF.LogWarning($"楼层{floorData.Floor}的玩法类型未知或配置为空: {floorData.MethodType}");
                            continue; // 跳过这个楼层
                        }

                        // 添加点击事件：点击后直接进入对应楼层
                        imageButton.onClick.RemoveAllListeners();
                        imageButton.onClick.AddListener(() =>
                        {
                            GlobalManager.GetInstance().LeagueInfo = leagueData;
                            EnterSpecificFloor(leagueData, floorData);
                        });
                        // 显示图片容器
                        imageContainer.gameObject.SetActive(true);
                        string methodName = floorData.MethodType == MethodType.RunFast ? "跑的快"
                            : (floorData.MjConfig != null ? floorData.MjConfig.MjMethod.ToString() : "未知");
                    }
                }
            }

        }
    }

    /// <summary>
    /// 延时发送下一个亲友圈的楼层信息请求
    /// </summary>
    /// <param name="leagueId">亲友圈ID</param>
    /// <param name="delayTime">延时时间（秒）</param>
    /// <returns></returns>
    private System.Collections.IEnumerator DelayedRequestNextFloor(long leagueId, float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

        //        GF.LogInfo_wl($"延时{delayTime}秒后发送下一个楼层请求，亲友圈ID: {leagueId}");
        RequestLeagueFloorInfos(leagueId);
    }
    protected override void OnClose(bool isShutdown, object userData)
    {
        // 清理网络监听
        RemoveNetworkListeners();
        // 取消订阅背景变化事件
        UnsubscribeFromBackgroundEvents();
        // 【修复】取消订阅亲友圈数据更新事件
        GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnLeagueDataChanged);
        // 清理待处理数据
        pendingUIItems.Clear();
        pendingLeagueData.Clear();
        activeRequestLeagueId = -1;
        activeClubIds.Clear();
        base.OnClose(isShutdown, userData);
    }

    /// <summary>
    /// 【修复】处理亲友圈数据更新事件 - 当服务器推送数据更新时自动刷新界面
    /// </summary>
    private void OnLeagueDataChanged(object sender, GameEventArgs e)
    {
        ClubDataChangedEventArgs args = e as ClubDataChangedEventArgs;
        if (args == null || args.Type != ClubDataType.eve_LeagueDateUpdate)
        {
            return;
        }
        // 刷新整个亲友圈列表
        RefreshLeagueList();
    }
    /// <summary>
    /// 【修复】刷新亲友圈列表显示
    /// </summary>
    private void RefreshLeagueList()
    {

        // 清空现有UI
        foreach (Transform item in varQYQContent.transform)
        {
            Destroy(item.gameObject);
        }

        // 清空待处理的数据
        pendingUIItems.Clear();
        pendingLeagueData.Clear();
        activeRequestLeagueId = -1;
        activeClubIds.Clear();

        // 更新显示状态
        varJoin.gameObject.SetActive(GlobalManager.GetInstance().MyJoinLeagueInfos.Count > 0);
        varNotJoin.gameObject.SetActive(GlobalManager.GetInstance().MyJoinLeagueInfos.Count == 0);

        if (GlobalManager.GetInstance().MyJoinLeagueInfos.Count > 0)
        {
            // 重新创建所有UI项
            foreach (var item in GlobalManager.GetInstance().MyJoinLeagueInfos)
            {
                var data = item.Value;

                GameObject newItem = Instantiate(varJoinItem, varQYQContent.transform);
                newItem.transform.Find("qyqname").GetComponent<Text>().text = data.LeagueName;
                newItem.transform.Find("ID").GetComponent<Text>().text = "ID:" + data.LeagueId;
                var unionText = newItem.transform.Find("union/union/UnionText");

                // 先隐藏所有玩法图片
                HideAllGameImages(newItem);

                // 更新联盟币显示
                bool showUnion = false;
                string leagueCoinText = "0";
                if (data != null)
                {
                    switch (data.Type)
                    {
                        case 0:
                            showUnion = data.Father != 0;
                            break;
                        case 1:
                        case 2:
                            showUnion = true;
                            break;
                    }
                    if (showUnion)
                    {
                        var leagueCoinValue = GlobalManager.GetInstance().GetAllianceCoins(data.LeagueId);
                        leagueCoinText = leagueCoinValue.ToString();
                    }
                }
                unionText.GetComponent<Text>().text = leagueCoinText;

                // 根据类型显示不同的按钮
                Transform enterQyqBtn = newItem.transform.Find("EnterQyq");
                Transform enterLmBtn = newItem.transform.Find("EnterLm");

                if ((data.Type == 1 || data.Type == 2) && data.Creator == Util.GetMyselfInfo().PlayerId)
                {
                    // 联盟或超级联盟且是创建者
                    if (enterQyqBtn != null) enterQyqBtn.gameObject.SetActive(false);
                    if (enterLmBtn != null)
                    {
                        enterLmBtn.gameObject.SetActive(true);
                        enterLmBtn.GetComponent<Button>().onClick.RemoveAllListeners();
                        enterLmBtn.GetComponent<Button>().onClick.AddListener(() =>
                        {
                            GlobalManager.GetInstance().LeagueInfo = data;
                            JoinAlliance(data);
                        });
                    }
                }
                else
                {
                    // 俱乐部
                    if (enterQyqBtn != null)
                    {
                        enterQyqBtn.gameObject.SetActive(true);
                        enterQyqBtn.GetComponent<Button>().onClick.RemoveAllListeners();
                        enterQyqBtn.GetComponent<Button>().onClick.AddListener(() =>
                        {
                            GlobalManager.GetInstance().LeagueInfo = data;
                            JoinLeague(data);
                        });
                    }
                    if (enterLmBtn != null) enterLmBtn.gameObject.SetActive(false);
                }
                long clubId = GetCorrectClubId(data);
                activeClubIds.Add(clubId);
                pendingUIItems[data.LeagueId] = newItem;
                pendingLeagueData[data.LeagueId] = data;
            }
            // 开始请求楼层信息
            if (pendingUIItems.Count > 0)
            {
                var firstLeagueId = pendingUIItems.Keys.First();
                RequestLeagueFloorInfos(firstLeagueId);
            }
        }
    }

    /// <summary>
    /// 获取正确的ClubId - 根据上级关系决定使用哪个ClubId
    /// </summary>
    /// <param name="leagueInfo">亲友圈信息</param>
    /// <returns>正确的ClubId</returns>
    private long GetCorrectClubId(LeagueInfo leagueInfo)
    {
        if (leagueInfo == null) return 0;
        if (leagueInfo.Father == 0)
        {
            return leagueInfo.LeagueId;
        }
        else if (leagueInfo.Father != 0 && leagueInfo.FatherInfo != null && leagueInfo.FatherInfo.Father == 0)
        {
            return leagueInfo.Father;
        }
        else if (leagueInfo.Father != 0 && leagueInfo.FatherInfo != null && leagueInfo.FatherInfo.Father != 0)
        {
            return leagueInfo.FatherInfo.Father;
        }
        return leagueInfo.LeagueId; // 默认返回当前联盟ID
    }
    /// <summary>
    /// 楼层变化通知处理
    /// </summary>
    /// <param name="data">消息数据</param>
    private void OnFloorChangeNotification(MessageRecvData data)
    {
        Invoke(nameof(RefreshAllLeagueFloors), 1f);
    }
    /// <summary>
    /// 刷新所有亲友圈的楼层信息
    /// </summary>
    private void RefreshAllLeagueFloors()
    {
        // 直接调用 RefreshLeagueList，它会清空状态并重新开始顺序请求
        RefreshLeagueList();
    }
    #region 背景设置监听
    /// <summary>
    /// 订阅背景变化事件
    /// </summary>
    private void SubscribeToBackgroundEvents()
    {
        // 订阅PersonalitySet的背景确认事件
        PersonalitySet.OnBackgroundConfirmed += OnBackgroundChanged;
    }
    /// <summary>
    /// 取消订阅背景变化事件
    /// </summary>
    private void UnsubscribeFromBackgroundEvents()
    {
        // 取消订阅PersonalitySet的背景确认事件
        PersonalitySet.OnBackgroundConfirmed -= OnBackgroundChanged;
        GF.LogInfo_wl("MoreQyq: 已取消订阅背景变化事件");
    }
    /// <summary>
    /// 背景变化时的回调方法
    /// </summary>
    /// <param name="newBackgroundIndex">新的背景索引</param>
    private void OnBackgroundChanged(int newBackgroundIndex)
    {
        // 保存新的背景索引到本地
        SaveBackgroundIndex(newBackgroundIndex);
        // 应用背景变化
        var bg = transform.Find("bg");
        if (bg != null && backgroundSprites != null && newBackgroundIndex < backgroundSprites.Length)
        {
            var bgImage = bg.GetComponent<Image>();
            bgImage.sprite = backgroundSprites[newBackgroundIndex];
        }
    }

    /// <summary>
    /// 保存背景索引到本地
    /// </summary>
    /// <param name="index">要保存的背景索引</param>
    private void SaveBackgroundIndex(int index)
    {
        currentBackgroundIndex = index;
        PlayerPrefs.SetInt(ROOM_BACKGROUND_INDEX_KEY, index);
        PlayerPrefs.Save(); // 立即保存到磁盘
    }

    /// <summary>
    /// 从本地读取背景索引
    /// </summary>
    /// <returns>背景索引</returns>
    private int GetBackgroundIndex()
    {
        return PlayerPrefs.GetInt(ROOM_BACKGROUND_INDEX_KEY, 0); // 默认返回0（第一个背景）
    }

    /// <summary>
    /// 加载背景设置并应用到指定的背景对象
    /// </summary>
    /// <param name="bgTransform">背景Transform对象</param>
    private void LoadBackgroundSetting(Transform bgTransform)
    {
        try
        {
            // 从本地读取背景索引
            currentBackgroundIndex = GetBackgroundIndex();

            // 应用加载的背景设置
            if (bgTransform != null && backgroundSprites != null && currentBackgroundIndex < backgroundSprites.Length)
            {
                var bgImage = bgTransform.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.sprite = backgroundSprites[currentBackgroundIndex];
                    // GF.LogInfo_wl($"MoreQyq: 成功应用背景设置");
                }
                else
                {
                    GF.LogWarning("MoreQyq: bg对象上未找到Image组件");
                }
            }
            else
            {
                if (backgroundSprites == null)
                {
                    GF.LogWarning("MoreQyq: backgroundSprites未设置");
                }
                else if (currentBackgroundIndex >= backgroundSprites.Length)
                {
                    GF.LogWarning($"MoreQyq: 背景索引超出范围: {currentBackgroundIndex}, 数组长度: {backgroundSprites.Length}");
                }
            }
        }
        catch (System.Exception ex)
        {
            currentBackgroundIndex = 0; // 默认使用第一个背景
            if (bgTransform != null && backgroundSprites != null && backgroundSprites.Length > 0)
            {
                var bgImage = bgTransform.GetComponent<Image>();
                bgImage.sprite = backgroundSprites[0];
            }
            GF.LogError($"MoreQyq: 加载背景设置时出错，使用默认背景。错误信息: {ex.Message}");
        }
    }

    #endregion
}

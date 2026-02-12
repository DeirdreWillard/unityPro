using System.Collections.Generic;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using Tacticsoft;
using System.Linq;
using UnityGameFramework.Runtime;
using Google.Protobuf;
using System.Collections;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class CreatMjRoom : UIFormBase
{
    #region 常量定义
    private const int MAX_FLOORS_BEFORE_ADJUSTMENT = 5;
    // CreatMjRoom背景设置保存的键名·
    private const string ROOM_BACKGROUND_INDEX_KEY = "PersonalitySet_BackgroundIndex";
    // 当前界面的ClubId，用于过滤网络消息
    private long currentClubId = 0;
    // 初始化位置缓存
    private Vector3 initialKaiSeLouCengPosition;
    private bool hasInitializedPosition = false;
    private bool m_ShouldResetScroll = false; // 是否需要重置滚动位置
                                              //存放所有楼层的配置信息
    /// <summary>
    /// 获取正确的ClubId - 根据上级关系决定使用哪个ClubId
    /// </summary>
    /// <returns>正确的ClubId</returns>
    public GameObject TuoGuanGo;
    private long GetCorrectClubId()
    {
        var leagueInfo = GlobalManager.GetInstance().LeagueInfo;

        if (leagueInfo.Father == 0)
        {
            return leagueInfo.LeagueId;
        }
        else if (leagueInfo.Father != 0 && leagueInfo.FatherInfo.Father == 0)
        {
            return leagueInfo.Father;
        }
        else if (leagueInfo.Father != 0 && leagueInfo.FatherInfo.Father != 0)
        {
            return leagueInfo.FatherInfo.Father;
        }

        return leagueInfo.LeagueId; // 默认返回当前联盟ID
    }

    /// <summary> "error": -23
    /// 判断是否有权限创建房间 - 必须是最上级并且是创建者
    /// </summary>
    /// <returns>是否有创建权限</returns>
    private bool CanCreateRoom()
    {
        var leagueInfo = GlobalManager.GetInstance().LeagueInfo;
        bool isTopLevel = leagueInfo.Father == 0; // 是否为最上级
        bool isCreator = Util.IsMySelf(leagueInfo.Creator); // 是否为创建者
        return isTopLevel && isCreator;
    }
    #endregion
    public List<Sprite> IconSprs;
    private bool isCreator;
    //  private bool isOpenMixed;//是否打开混排
    private string MixHallName = "大厅";//混排大厅名称
    public List<Sprite> sprs;
    private int currentRoomBackgroundIndex = 0; // 当前房间背景索引
    // 外部指定的目标楼层（从其他界面跳转时使用）
    private Msg_Floor externalTargetFloor = null;
    // 【新增】楼层配置缓存：每个楼层的完整配置信息（从桌子信息中提取）
    // Key: 楼层ID(Floor), Value: 完整的楼层配置（包含RunFastConfig/MjConfig等）
    private Dictionary<long, Msg_Floor> floorConfigCache = new Dictionary<long, Msg_Floor>();
    // 【新增】防止重复请求：记录最后一次请求的楼层ID和时间
    private long lastRequestedFloorId = -1;// -1表示未请求过
    private float lastRequestTime = 0f;// 上次请求时间
    private const float REQUEST_COOLDOWN = 0.5f; // 请求冷却时间（秒）
    // 【改名刷新】标记改名操作后需要刷新桌子
    private bool needRefreshDesksAfterRename = false;// 默认不需要刷新
    private long floorIdBeforeRename = 0; // 改名前所在的楼层ID
    // 【OnReveal优化】标记是否需要在OnReveal时刷新数据（用于避免不必要的刷新）
    private bool needRefreshOnReveal = true; // 默认需要刷新（从游戏返回时）
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        m_ShouldResetScroll = true; // 初始打开时重置
        isCreator = CanCreateRoom();
        externalTargetFloor = null;
        Params.TryGet("TargetFloor", out VarByteArray byteArray);
        if (byteArray != null)
        {
            externalTargetFloor = Msg_Floor.Parser.ParseFrom(byteArray);
        }
        // 保存当前界面的ClubId，用于消息过滤
        currentClubId = GetCorrectClubId();
        // 重置选中状态为大厅（确保每次打开界面时默认进入大厅）
        chooseFloor = 0;
        chooseUIFloor = 0;
        // 【OnReveal优化】OnOpen时设置为true，表示首次打开时需要刷新
        needRefreshOnReveal = true;
        var text = varLouchengguanli.transform.Find("btn/text").GetComponent<Text>();
        if (isCreator) text.text = "楼层管理";
        else text.text = "切换楼层";
        RemoveNetworkListeners();
        AddNetworkListeners();
        InitializeUI();
        GetMjFloorRq();
        // 订阅背景变化事件
        SubscribeToBackgroundEvents();
        // 加载本地保存的背景设置
        LoadRoomBackgroundSetting();
    }

    /// <summary>
    /// 界面重新显示时调用（从游戏返回时会触发）
    /// </summary>
    protected override void OnReveal()
    {
        base.OnReveal();
        GF.LogInfo_wl("[CreatMjRoom] 界面重新显示");

        // 【修复】只在需要时才刷新，避免点击进入桌子取消后刷新到大厅
        if (needRefreshOnReveal)
        {
            GF.LogInfo_wl("[CreatMjRoom] 从游戏返回，刷新楼层和桌子数据");
            GetMjFloorRq();
            needRefreshOnReveal = false; // 刷新后重置标志
        }
        else
        {
            GF.LogInfo_wl("[CreatMjRoom] 从对话框返回，保持当前状态不刷新");
        }
    }
    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "管理玩法":
                OpenCreatMjFloorPopup();
                break;
            case "楼层管理":
                //如果varQiehuanlouceng关 则打开varLouchengguanli 反之
                bool newActive = varQiehuanlouceng.activeSelf;
                varQiehuanlouceng.SetActive(!newActive);
                varLouchengguanli.SetActive(newActive);
                varLouceng.SetActive(newActive);
                break;
            case "开设楼层":
                OpenCreatMjFloorPopup();
                break;
            case "战绩":
                GF.UI.OpenUIFormAwait(UIViews.MJClubRecord);
                break;
            case "设置":
                GF.UI.OpenUIFormAwait(UIViews.PersonalitySet);
                break;
            case "QuickStartBtn":
                var currentFloor = GetCurrentFloor();
                if (currentFloor != null)
                {
                    //不是大厅 在当前楼层快速进入一个不是满员的房间
                    QuickEnterNonFullRoom(currentFloor);
                }
                break;
            case "GameInfoBtn":
                varRoomInfo.SetActive(!varRoomInfo.activeSelf);
                break;

        }
    }

    /// <summary>
    /// 快速进入当前楼层的非满员房间
    /// </summary>
    /// <param name="floor">当前楼层信息</param>
    private void QuickEnterNonFullRoom(Msg_Floor floor)
    {
        // 从deskList中找出所有非满员的房间
        List<Msg_MJDeskInfo> availableRooms = new List<Msg_MJDeskInfo>();

        foreach (var mjDesk in deskList)
        {
            // 检查desk1
            if (mjDesk.desk1 != null && !IsRoomFull(mjDesk.desk1))
            {
                availableRooms.Add(mjDesk.desk1);
            }
            // 检查desk2
            if (mjDesk.desk2 != null && !IsRoomFull(mjDesk.desk2))
            {
                availableRooms.Add(mjDesk.desk2);
            }
        }

        if (availableRooms.Count == 0)
        {
            GF.UI.ShowToast("当前楼层没有可加入的房间");
            return;
        }

        // 选择第一个非满员房间
        var targetRoom = availableRooms[0];
        int deskId = targetRoom.DeskInfo.DeskId;
        long clubId = GlobalManager.GetInstance().LeagueInfo.LeagueId;
        string methodName = GetFloorGameMethod(floor);
        // 【修复】弹出确认对话框前，标记不需要刷新（避免取消时刷新到大厅）
        needRefreshOnReveal = false;
        // 弹出确认对话框
        Util.GetInstance().OpenMJConfirmationDialog("快速进入", $"确认进入[{methodName}]房间吗？",
            // 确认回调
            () =>
            {
                needRefreshOnReveal = true; // 确认进入时，标记需要刷新（如果进入失败返回时需要刷新）
                Util.GetInstance().Send_EnterDeskRq(deskId, clubId);
            },
            // 取消回调
            () =>
            {
                needRefreshOnReveal = false; // 取消时不需要刷新
            });
    }

    /// <summary>
    /// 判断房间是否满员
    /// </summary>
    /// <param name="deskInfo">房间信息</param>
    /// <returns>是否满员</returns>
    private bool IsRoomFull(Msg_MJDeskInfo deskInfo)
    {
        if (deskInfo?.DeskInfo?.BaseConfig == null)
        {
            return true; // 数据无效，视为满员
        }

        return deskInfo.DeskInfo.SitDownNum >= deskInfo.DeskInfo.BaseConfig.PlayerNum;
    }

    #region UI初始化方法
    /// <summary>
    /// 初始化UI
    /// </summary>
    public void InitUI()
    {
        varQhQYQAccount.transform.GetComponent<Text>().text = GlobalManager.GetInstance().LeagueInfo.LeagueName;
    }
    /// <summary>
    /// 初始化UI界面
    /// </summary>
    private void InitializeUI()
    {
        InitializeScroll();
        //InitializeHallScroll();
        InitializeRoomListUI();
        varQiehuanlouceng.SetActive(false);
        SetDefaultUIState();
    }
    /// <summary>
    /// 初始化房间列表UI
    /// </summary>
    private void InitializeRoomListUI()
    {
        varZuozi.SetActive(floorList.Count > 0);
        varLouchengguanli.SetActive(false);
        floorList.Clear();
        varQuickStartObj.SetActive(false);
    }
    /// <summary>
    /// 设置默认UI状态
    /// </summary>
    private void SetDefaultUIState()
    {
        bool newActive = varQiehuanlouceng.activeSelf;
        varQiehuanlouceng.SetActive(newActive);
        varLouchengguanli.SetActive(!newActive);
        varLouceng.SetActive(!newActive);
    }
    #endregion
    /// <summary>
    /// 修改楼层
    /// </summary>
    private async void Modifyfloor(Msg_Floor floorData)
    {
        var uiParams = UIParams.Create();

        uiParams.Set<VarByteArray>("floorData", floorData.ToByteArray());
        uiParams.Set<VarString>("setFloor", "EditRoom");
        await GF.UI.OpenUIFormAwait(UIViews.CreatMjPopup, uiParams);
    }
    /// <summary>
    /// 打开创建麻将弹窗
    /// </summary>
    private async void OpenCreatMjPopup()
    {
        var uiParams = UIParams.Create();
        uiParams.Set<VarString>("setFloor", "CreateRoom");
        await GF.UI.OpenUIFormAwait(UIViews.CreatMjPopup, uiParams);

    }
    /// <summary>
    /// 新建楼层弹窗
    /// </summary>
    private async void OpenCreatMjFloorPopup()
    {
        var uiParams = UIParams.Create();
        uiParams.Set<VarString>("setFloor", "CreateFloor");
        await GF.UI.OpenUIFormAwait(UIViews.CreatMjPopup, uiParams);
    }
    /// <summary>
    /// 楼层组管理 - 根据楼层数量调整UI布局
    /// </summary>
    public void AddLouCnengGroup()
    {
        // 初始化位置（只在第一次调用时记录）
        if (!hasInitializedPosition)
        {
            initialKaiSeLouCengPosition = varKaiSeLouCeng.transform.localPosition;
            hasInitializedPosition = true;
        }

        //获取当前楼层数量并调整布局
        int currentFloorCount = floorList.Count;
        Text floorNum = varKaiSeLouCeng.transform.Find("KaiSeLouCeng/loucengnum").GetComponent<Text>();
        floorNum.text = (currentFloorCount + 1).ToString();

        // 根据初始位置动态调整
        if (currentFloorCount <= MAX_FLOORS_BEFORE_ADJUSTMENT && currentFloorCount > 0)
        {
            Vector3 newPosition = initialKaiSeLouCengPosition;
            newPosition.y = initialKaiSeLouCengPosition.y + currentFloorCount * 150;
            varKaiSeLouCeng.transform.localPosition = newPosition;
            //varQiehuanlouceng.transform.Find("xuanzhelouceng").GetComponent<ScrollRect>().enabled = false;
        }
        else if (currentFloorCount > MAX_FLOORS_BEFORE_ADJUSTMENT)
        {
            Vector3 newPosition = initialKaiSeLouCengPosition;
            newPosition.y = initialKaiSeLouCengPosition.y + 749; // 278 - (-471) = 749的偏移量
            varKaiSeLouCeng.transform.localPosition = newPosition;
        }
        else
        {
            // 楼层数为0时，恢复到初始位置
            varKaiSeLouCeng.transform.localPosition = initialKaiSeLouCengPosition;
        }

        varKaiSeLouCeng.transform.Find("KaiSeLouCeng").gameObject.SetActive(isCreator);
        varKaiSeLouCeng.transform.Find("img").gameObject.SetActive(!isCreator);
        //文字改为当前游戏名字加局数
    }
    #region 网络消息处理
    /// <summary>
    /// 添加网络消息监听
    /// </summary>
    private void AddNetworkListeners()
    {
        HotfixNetworkComponent.AddListener(MessageID.Msg_CreateMjFloorRs, OnCreateMjFloorResponse);//创建楼层
        HotfixNetworkComponent.AddListener(MessageID.Msg_DelMjFloorRs, OnDeleteMjFloorResponse);//删除楼层
        HotfixNetworkComponent.AddListener(MessageID.Msg_GetMjFloorRs, OnGetMjFloorListResponse);//获取所有楼层列表
        HotfixNetworkComponent.AddListener(MessageID.Msg_SetMjFloorRs, OnSetMjFloorResponse);//设置楼层
        HotfixNetworkComponent.AddListener(MessageID.Msg_EnterMjFloorRs, OnEnterMjFloorResponse);//获取楼层信息
        HotfixNetworkComponent.AddListener(MessageID.Msg_SetHallRs, OnSetHallResponse);//混排设置
        HotfixNetworkComponent.AddListener(MessageID.Msg_SetHallFloorRs, OnSetHallFloorResponse);//大厅设置
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynFloorChange, Function_SynFloorChange);//楼层变动通知
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynFloorDeskChange, Function_SynFloorDeskChange);//房间人数变动通知
    }
    /// <summary>
    /// 移除网络消息监听
    /// </summary>
    private void RemoveNetworkListeners()
    {
        // 移除麻将房间相关消息监听
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_CreateMjFloorRs, OnCreateMjFloorResponse);//创建楼层
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_DelMjFloorRs, OnDeleteMjFloorResponse);//删除楼层
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_GetMjFloorRs, OnGetMjFloorListResponse);//获取所有楼层列表
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SetMjFloorRs, OnSetMjFloorResponse);//设置楼层
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_EnterMjFloorRs, OnEnterMjFloorResponse);//获取楼层信息
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SetHallRs, OnSetHallResponse);//混排设置
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SetHallFloorRs, OnSetHallFloorResponse);//大厅设置
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynFloorChange, Function_SynFloorChange);//楼层变动通知
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynFloorDeskChange, Function_SynFloorDeskChange);//房间人数变动通知
    }
    /// <summary>
    /// 获取所有楼层信息请求
    /// </summary>
    private void GetMjFloorRq()
    {
        Msg_GetMjFloorRq req = MessagePool.Instance.Fetch<Msg_GetMjFloorRq>();
        varQyqName.text = GlobalManager.GetInstance().LeagueInfo.LeagueName;
        req.ClubId = GetCorrectClubId();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
               (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GetMjFloorRq, req);
    }
    /// <summary>
    /// 创建房间响应
    /// </summary>
    private void OnCreateMjRoomResponse(MessageRecvData data)
    {

    }
    /// <summary>
    /// 创建楼层响应
    /// </summary>
    private void OnCreateMjFloorResponse(MessageRecvData data)
    {
        Msg_CreateMjFloorRs ack = Msg_CreateMjFloorRs.Parser.ParseFrom(data.Data);
        Msg_Floor msg_Floor = ack.Floor;
        floorList.Add(msg_Floor);
        //延迟0.2秒调用
        Invoke("EnterHall", 0.2f);
        Msg_DefaultCreateDeskRq req = MessagePool.Instance.Fetch<Msg_DefaultCreateDeskRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(
            HotfixNetworkComponent.AccountClientName,
            MessageID.Msg_DefaultCreateDeskRq,
            req
        );

    }
    /// <summary>
    /// 获取所有楼层列表响应
    /// </summary>
    private void OnGetMjFloorListResponse(MessageRecvData data)
    {
        Msg_GetMjFloorRs ack = Msg_GetMjFloorRs.Parser.ParseFrom(data.Data);
        GF.LogInfo_wl("获取所有楼层列表响应", ack.ToString());
        // 【消息过滤】只处理当前界面的ClubId消息
        if (ack.ClubId != currentClubId && ack.ClubId != 0)
        {
            return;
        }
        deskList.Clear();
       // GF.LogInfo_wl($"[CreatMjRoom] 获取所有楼层列表响应 ClubId={ack.ClubId}", ack.ToString());
        // 保存当前选中状态
        long previousChooseFloor = chooseFloor;
        int previousChooseUIFloor = chooseUIFloor;
        floorList = ack.FloorInfo.ToList();
        foreach (var floor in floorList)
        {
           // GF.LogInfo_wl($"楼层列表包含楼层ID: " + floor.ToString());
        }

        // 逻辑优化：确保首次进入时默认选中大厅
        if (previousChooseFloor == 0)
        {
            // 首次进入，保持 chooseFloor = 0（大厅）
            chooseFloor = 0;
            chooseUIFloor = 0;
        }
        else if (previousChooseFloor != 0)
        {
            // 之前有选中的楼层，尝试保持选中状态
            var existingFloor = floorList.FirstOrDefault(f => f.Floor == previousChooseFloor);
            if (existingFloor != null)
            {
                // 保持原来的选中状态
                chooseFloor = previousChooseFloor;
                chooseUIFloor = previousChooseUIFloor;
            }
            else
            {
                // 之前选中的楼层不存在了（可能被删除），重置为大厅
                chooseFloor = 0;
                chooseUIFloor = 0;
            }
        }

        UpdateFloor();

        // 【改名刷新】检查是否需要在楼层列表更新后刷新桌子
        if (needRefreshDesksAfterRename)
        {
            needRefreshDesksAfterRename = false; // 重置标志
            GF.LogInfo_wl($"[改名刷新] 楼层列表已更新，开始刷新桌子。改名前楼层ID: {floorIdBeforeRename}, 当前楼层ID: {chooseFloor}");
            // 延迟0.1秒后刷新，确保UI已经完全更新
            Invoke("RefreshDesksAfterFloorUpdate", 0.1f);
        }
    }
    /// <summary>
    /// 更新房间响应
    /// </summary>
    private void OnUpdateMjRoomResponse(MessageRecvData data)
    {

    }

    private void Function_SynFloorChange(MessageRecvData data)
    {
        GF.LogInfo_wl("楼层变动通知");
        // 保存当前选中状态
        long previousChooseFloor = chooseFloor;
        int previousChooseUIFloor = chooseUIFloor;
        Invoke("GetMjFloorRq", 1f);
    }
    /// <summary>
    /// 删除楼层响应
    /// </summary>
    private void OnDeleteMjFloorResponse(MessageRecvData data)
    {
        Msg_DelMjFloorRs ack = Msg_DelMjFloorRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("删除楼层响应", ack.ToString());
        long floorId = ack.Floor;
        floorList.RemoveAll(f => f.Floor == floorId);

        // 如果删除的是当前选中的楼层，需要重新选择楼层
        if (chooseFloor == floorId)
        {
            deskList.Clear();
            if (floorList.Count > 0)
            {
                // 选择最后一个楼层作为新的当前楼层
                var newFloor = floorList[floorList.Count - 1];
                chooseFloor = newFloor.Floor;
                chooseUIFloor = floorList.Count; // 更新UI楼层索引
                EnterMjFloor(newFloor);
            }
            else
            {
                // 没有楼层了，切换到大厅
                chooseFloor = 0;
                chooseUIFloor = 0;
            }
        }
        else
        {
            // 如果删除的不是当前选中的楼层，需要更新chooseUIFloor索引
            // 因为楼层列表发生了变化，需要重新计算当前选中楼层的UI索引
            if (chooseFloor != 0)
            {
                var currentFloor = floorList.FirstOrDefault(f => f.Floor == chooseFloor);
                if (currentFloor != null)
                {
                    chooseUIFloor = floorList.IndexOf(currentFloor) + 1;
                }
            }
        }
        UpdateFloor();
    }

    /// <summary>
    /// 设置楼层信息
    /// </summary>
    private void OnSetMjFloorResponse(MessageRecvData data)
    {
        Msg_SetMjFloorRs ack = Msg_SetMjFloorRs.Parser.ParseFrom(data.Data);
       // GF.LogInfo_wl("楼层设置响应", ack.ToString());
        needRefreshDesksAfterRename = true;
    }
    //存放当前所有楼层的配置信息

    /// <summary>
    /// 获取楼层桌子信息
    /// </summary>
    private void OnEnterMjFloorResponse(MessageRecvData data)
    {
        Msg_EnterMjFloorRs ack = Msg_EnterMjFloorRs.Parser.ParseFrom(data.Data);
        //GF.LogInfo_wl("ack" + ack.DeskInfo.Count());
        //如果
        //GF.LogInfo_wl("获取楼层桌子信息" + ack.ToString());
        if (ack == null)
        {
            return;
        }
        List<Msg_MJDeskInfo> deskInfos = ack.DeskInfo != null ? ack.DeskInfo.ToList() : new List<Msg_MJDeskInfo>();

        // 【关键】从桌子信息中提取并缓存楼层的完整配置
        CacheFloorConfigFromDesks(ack.ClubId, ack.Floor, deskInfos);
        // 【优化】更新桌子缓存
        UpdateDeskCache(deskInfos);
        // 判断是大厅模式还是楼层模式
        bool isHallMode = chooseUIFloor == 0;
        if (isHallMode)
        {
            hallDeskList.Clear();
            // 大厅模式：优化排序 - 先显示所有有人的桌子，最后每个玩法添加一个空桌
            List<Msg_MJDeskInfo> availableDesks = FilterAndSortDesksForHall(deskInfos);
            // 将筛选后的桌子按两两一组进行分组
            for (int i = 0; i < availableDesks.Count; i += 2)
            {
                MjDesk mjDesk = new MjDesk();
                if (i + 1 < availableDesks.Count)
                {
                    mjDesk.desk1 = availableDesks[i];
                    mjDesk.desk2 = availableDesks[i + 1];
                }
                else
                {
                    mjDesk.desk1 = availableDesks[i];
                    mjDesk.desk2 = null;
                }
                hallDeskList.Add(mjDesk);
            }
            // 切换到大厅滚动视图
            InitializeHallScroll();
        }
        else
        {
            deskList.Clear();
            // 楼层模式：简单排序，有人的桌子在前
            var sortedDesks = deskInfos.OrderByDescending(HasPlayers).ToArray();

            for (int i = 0; i < sortedDesks.Length; i += 2)
            {
                MjDesk mjDesk = new MjDesk();
                if (i + 1 < sortedDesks.Length)
                {
                    mjDesk.desk1 = sortedDesks[i];
                    mjDesk.desk2 = sortedDesks[i + 1];
                }
                else
                {
                    mjDesk.desk1 = sortedDesks[i];
                    mjDesk.desk2 = null;
                }
                deskList.Add(mjDesk);
            }
            // 使用普通楼层滚动视图
            InitializeScroll();
        }

        if (m_ShouldResetScroll)
        {
            varScroll.ResetScrollPosition();
            m_ShouldResetScroll = false;
        }
        varScroll.ReloadData();
        var currentFloor = GetCurrentFloor();
        // 设置Creator界面的输入框
        Text creatRoomNameText = varCreatorInterface.transform.Find("CreatRoomName").GetComponent<Text>();
        InputField creatorInputField = varCreatorInterface.transform.Find("QhQYQAccount")?.GetComponent<InputField>();
        Image image = varCreatorInterface.transform.Find("icon").GetComponent<Image>();
        Image image2 = varQuickStartObj.transform.Find("icon").GetComponent<Image>();
        //加个判断 当前不是进入大厅
        if (currentFloor != null)
        {
            string methodName = GetFloorGameMethod(currentFloor);
            Sprite iconSprite = IconSprs?.Find(s => s.name == methodName);
            if (iconSprite != null)
            {
                image.sprite = iconSprite;
                image.color = new Color(image.color.r, image.color.g, image.color.b, 255);
                image2.sprite = iconSprite;
                image2.color = new Color(image2.color.r, image2.color.g, image2.color.b, 255);
            }
            creatRoomNameText.text = methodName + currentFloor.Config.BaseCoin + "分";
            creatorInputField.placeholder.GetComponent<Text>().text = currentFloor.Config.DeskName;
            creatorInputField.text = currentFloor.Config.DeskName;
            // 根据权限设置输入框是否可编辑
            creatorInputField.interactable = CanCreateRoom();

            varQuickStartObj.transform.Find("floorName").GetComponent<Text>().text = methodName;
            varQuickStartObj.SetActive(true);
        }
        else
        {
            creatRoomNameText.text = "";
            creatorInputField.placeholder.GetComponent<Text>().text = MixHallName;
            creatorInputField.text = MixHallName;
            creatorInputField.interactable = false;
            //大厅模式下图标显示大众麻将
            string methodName = "大众麻将";
            Sprite iconSprite = IconSprs?.Find(s => s.name == methodName);
            image.sprite = iconSprite;
            image.color = new Color(image.color.r, image.color.g, image.color.b, 255);
            varQuickStartObj.SetActive(false);
        }

    }

    /// <summary>
    /// 混排设置响应 948033
    /// </summary>
    private void OnSetHallResponse(MessageRecvData data)
    {
        GetMjFloorRq();
    }
    /// <summary>
    /// 大厅设置响应
    /// </summary>
    private void OnSetHallFloorResponse(MessageRecvData data)
    {
    }
    #endregion

    /// <summary>
    /// 检查桌子是否满人
    /// </summary>
    private bool IsDeskFull(Msg_MJDeskInfo deskInfo)
    {
        if (deskInfo?.Player == null || deskInfo?.DeskInfo?.BaseConfig == null)
        {
            return false;
        }
        int playerCount = deskInfo.Player.Count;
        long maxPlayers = deskInfo.DeskInfo.BaseConfig.PlayerNum;
        return playerCount >= maxPlayers;
    }

    /// <summary>
    /// 检查桌子是否有玩家
    /// </summary>
    private bool HasPlayers(Msg_MJDeskInfo deskInfo)
    {
        if (deskInfo?.Player == null)
        {
            return false;
        }
        return deskInfo.Player.Count > 0;
    }

    /// <summary>
    /// 检查游戏是否已开始
    /// </summary>
    /// <param name="deskInfo">桌子信息</param>
    /// <returns>true表示游戏已开始，false表示游戏未开始</returns>
    private bool IsGameStarted(Msg_MJDeskInfo deskInfo)
    {
        if (deskInfo?.DeskInfo == null)
        {
            return false;
        }
        // 判断桌子状态是否为已开始（StartRun）
        return deskInfo.DeskInfo.State == NetMsg.DeskState.StartRun;
    }

    /// <summary>
    /// 大厅模式专用：优化桌子排序和筛选
    /// 策略：
    /// 1. 先显示所有有人且游戏未开始的桌子（按楼层ID排序）
    /// 2. 再显示每个楼层的一个空桌子
    /// 3. 按楼层ID分组，确保相同玩法但不同底分和配置的楼层都能显示
    /// 4. 过滤掉游戏已开始的桌子（State == StartRun）
    /// </summary>
    private List<Msg_MJDeskInfo> FilterAndSortDesksForHall(List<Msg_MJDeskInfo> allDesks)
    {
        if (allDesks == null || allDesks.Count == 0)
        {
            // GF.LogInfo_wl("[大厅筛选] 桌子列表为空");
            return new List<Msg_MJDeskInfo>();
        }

        // GF.LogInfo_wl($"[大厅筛选] 开始筛选，总桌子数: {allDesks.Count}, 楼层数: {floorList.Count}");

        var result = new List<Msg_MJDeskInfo>();

        // 1. 先添加所有有人且游戏未开始的桌子，按楼层ID和桌子ID排序
        // 过滤条件：有玩家 且 游戏状态为准备中（WaitStart）
        var desksWithPlayers = allDesks
            .Where(d => HasPlayers(d) && !IsGameStarted(d))
            .OrderBy(d => d.DeskInfo?.Floor ?? 0)
            .ThenBy(d => d.DeskInfo?.DeskId ?? 0)
            .ToList();

        // GF.LogInfo_wl($"[大厅筛选] 有人且未开始的桌子数量: {desksWithPlayers.Count}");
        result.AddRange(desksWithPlayers);

        // 2. 按楼层ID分组所有空桌子，每个楼层只保留第一个
        var emptyDesksByFloor = allDesks
            .Where(d => !HasPlayers(d))
            .GroupBy(d => d.DeskInfo?.Floor ?? 0)
            .OrderBy(g => g.Key);

        // 3. 每个楼层添加一个空桌（选择DeskId最小的那个）
        foreach (var floorGroup in emptyDesksByFloor)
        {
            var firstEmptyDesk = floorGroup.OrderBy(d => d.DeskInfo?.DeskId ?? 0).First();
            result.Add(firstEmptyDesk);
            //  GF.LogInfo_wl($"[大厅筛选] 添加空桌 - 楼层ID: {floorGroup.Key}, 桌号: {firstEmptyDesk.DeskInfo?.DeskId}");
        }

        // GF.LogInfo_wl($"[大厅筛选] 筛选完成，最终桌子数: {result.Count}");
        return result;
    }

    private class MjDesk
    {
        public Msg_MJDeskInfo desk1;
        public Msg_MJDeskInfo desk2;
    }
    private void Function_SynFloorDeskChange(MessageRecvData data)
    {
        //GF.LogInfo_wl("房间人数变动通知");
        Msg_SynFloorDeskChange ack = Msg_SynFloorDeskChange.Parser.ParseFrom(data.Data);
        // 添加详细的桌子信息日志
        if (ack.DeskInfo != null)
        {
            // GF.LogInfo_wl($"[桌子变化] 变化的桌号: {ack.DeskInfo.DeskInfo?.DeskId ?? 0}");
            // GF.LogInfo_wl($"[桌子变化] 桌子状态(DeskState): {ack.DeskInfo.DeskInfo?.State} (int):{(int)(ack.DeskInfo.DeskInfo?.State ?? 0)}");
            // GF.LogInfo_wl($"[桌子变化] 局数: {ack.DeskInfo.DeskInfo?.RoundNum ?? 0}");
            // GF.LogInfo_wl($"[桌子变化] 玩家数: {ack.DeskInfo.Player?.Count ?? 0}");
            if (ack.DeskInfo.DeskInfo != null)
            {
                string gameType = ack.DeskInfo.DeskInfo.MethodType == MethodType.RunFast ? "跑的快" :
                                  ack.DeskInfo.DeskInfo.MethodType == MethodType.MjSimple ? "麻将" :
                                  ack.DeskInfo.DeskInfo.MethodType.ToString();
            }
        }

        // 【退出刷新优化】检查桌子玩家数是否为0（所有玩家都退出）
        bool allPlayersLeft = ack.DeskInfo != null && (ack.DeskInfo.Player == null || ack.DeskInfo.Player.Count == 0);

        // 【游戏开始刷新】检查游戏是否刚刚开始
        bool gameJustStarted = ack.DeskInfo != null && IsGameStarted(ack.DeskInfo);

        // 判断是否需要刷新
        // 1. 如果当前在大厅（chooseFloor == 0），任何楼层的变化都要刷新大厅
        // 2. 如果当前在某个楼层，只刷新对应的楼层
        // 3. 【新增】如果有玩家退出且桌子变成空桌（玩家数为0），在大厅时刷新大厅
        // 4. 【新增】如果游戏刚开始，在大厅时刷新大厅（移除已开始的桌子）
        if (chooseFloor == 0)
        {
            // 当前在大厅
            // 【游戏开始刷新】如果游戏刚开始，直接刷新大厅移除该桌子
            if (gameJustStarted)
            {
               // GF.LogInfo_wl($"[桌子变化] 桌子 {ack.DeskInfo?.DeskInfo?.DeskId ?? 0} 游戏已开始，刷新大厅移除该桌子");
                RefreshCurrentFloorDesks();
            }
            // 【退出刷新优化】如果桌子所有玩家都退出了，直接刷新大厅，不使用增量更新
            else if (allPlayersLeft)
            {
               // GF.LogInfo_wl($"[桌子变化] 桌子 {ack.DeskInfo?.DeskInfo?.DeskId ?? 0} 所有玩家已退出，刷新大厅");
                RefreshCurrentFloorDesks();
            }
            // 【优化】尝试增量更新单个桌子
            else if (enableIncrementalUpdate && ack.DeskInfo != null)
            {
                IncrementalUpdateSingleDesk(ack.DeskInfo, true);
            }
            else
            {
                // 降级到全局刷新
                RefreshCurrentFloorDesks();
            }
        }
        else if (chooseFloor == ack.Floor)
        {
            // 当前在某个楼层，且变化的楼层就是当前楼层
            // 【优化】尝试增量更新单个桌子
            if (enableIncrementalUpdate && ack.DeskInfo != null)
            {
                IncrementalUpdateSingleDesk(ack.DeskInfo, false);
            }
            else
            {
                // 降级到全局刷新
                RefreshCurrentFloorDesks();
            }
        }
    }
    /// <summary>
    /// 刷新当前楼层的桌子信息（全局刷新）
    /// </summary>
    private void RefreshCurrentFloorDesks(long id = 0)
    {
        // 判断当前是否在大厅
        if (chooseFloor == 0)
        {
            // 当前在大厅，直接重新请求大厅数据（不调用EnterHall避免重复UI处理）
            EnterMjFloor(new Msg_Floor { Floor = 0 }, GetCorrectClubId(), false);
        }
        else
        {
            // 当前在某个楼层，刷新该楼层
            var currentFloor = GetCurrentFloor();
            if (currentFloor != null)
            {
                EnterMjFloor(currentFloor, GetCorrectClubId(), false);
            }
        }
    }

    #region 【优化】增量更新方法

    /// <summary>
    /// 【优化】增量更新单个桌子信息
    /// </summary>
    /// <param name="updatedDeskInfo">更新的桌子信息</param>
    /// <param name="isHallMode">是否为大厅模式</param>
    private void IncrementalUpdateSingleDesk(Msg_MJDeskInfo updatedDeskInfo, bool isHallMode)
    {
        if (updatedDeskInfo?.DeskInfo == null)
        {
            return;
        }
        long deskId = updatedDeskInfo.DeskInfo.DeskId;
        // 更新缓存
        if (deskInfoCache.ContainsKey(deskId))
        {
            deskInfoCache[deskId] = updatedDeskInfo;
        }
        else
        {
            deskInfoCache[deskId] = updatedDeskInfo;
        }
        // 根据模式选择更新列表
        List<MjDesk> targetList = isHallMode ? hallDeskList : deskList;
        // 查找包含该桌子的 MjDesk 项和它在列表中的索引
        int mjDeskIndex = -1;
        MjDesk targetMjDesk = null;
        bool isDeskInSlot1 = false;

        for (int i = 0; i < targetList.Count; i++)
        {
            var mjDesk = targetList[i];
            if (mjDesk.desk1?.DeskInfo?.DeskId == deskId)
            {
                targetMjDesk = mjDesk;
                mjDeskIndex = i;
                isDeskInSlot1 = true;
                break;
            }
            else if (mjDesk.desk2?.DeskInfo?.DeskId == deskId)
            {
                targetMjDesk = mjDesk;
                mjDeskIndex = i;
                isDeskInSlot1 = false;
                break;
            }
        }

        if (targetMjDesk != null && mjDeskIndex >= 0)
        {
            bool wasEmptyDesk = false;
            bool wasOccupiedDesk = false;
            if (isHallMode)
            {
                Msg_MJDeskInfo oldDeskInfo = isDeskInSlot1 ? targetMjDesk.desk1 : targetMjDesk.desk2;
                wasEmptyDesk = (oldDeskInfo != null && !HasPlayers(oldDeskInfo));
                wasOccupiedDesk = (oldDeskInfo != null && HasPlayers(oldDeskInfo));
            }

            // 更新桌子数据
            if (isDeskInSlot1)
            {
                targetMjDesk.desk1 = updatedDeskInfo;
            }
            else
            {
                targetMjDesk.desk2 = updatedDeskInfo;
            }
            UpdateSingleCellUI(mjDeskIndex, targetMjDesk, isHallMode);

            // 大厅模式下的特殊处理
            if (isHallMode)
            {
                bool nowHasPlayers = HasPlayers(updatedDeskInfo);

                // 情况1：空桌子变成有人的桌子 → 需要为该楼层添加新空桌子
                if (wasEmptyDesk && nowHasPlayers)
                {
                    // 尝试智能添加该楼层的空桌子
                    if (!TryAddEmptyDeskForFloor(deskId, updatedDeskInfo))
                    {
                        // 如果智能添加失败，降级为全局刷新
                        RefreshCurrentFloorDesks();
                    }
                }
                // 情况2：有人的桌子变成空桌子（玩家全部退出）→ 需要刷新（去除多余空桌子）
                else if (wasOccupiedDesk && !nowHasPlayers)
                {
                    RefreshCurrentFloorDesks();
                }
            }
            else
            {
                // 【修复】楼层模式下的特殊处理
                // 当有人的桌子变成空桌子时，需要刷新以重新排序（空桌子排到最后）
                bool nowHasPlayers = HasPlayers(updatedDeskInfo);
                Msg_MJDeskInfo oldDeskInfo = isDeskInSlot1 ? targetMjDesk.desk1 : targetMjDesk.desk2;
                bool wasOccupied = (oldDeskInfo != null && HasPlayers(oldDeskInfo));

                if (wasOccupied && !nowHasPlayers)
                {
                    // 有人的桌子变成空桌子，需要刷新排序
                    RefreshCurrentFloorDesks();
                }
                else if (!wasOccupied && nowHasPlayers)
                {
                    // 空桌子变成有人的桌子，也需要刷新排序
                    RefreshCurrentFloorDesks();
                }
            }

        }
        else
        {

            // 检查是否需要将该桌子添加到显示列表（例如空桌子现在有人了）
            bool shouldAddToList = ShouldDeskBeDisplayed(updatedDeskInfo, isHallMode);

            if (shouldAddToList)
            {
                // 降级为全局刷新，因为需要重新排序和分组
                RefreshCurrentFloorDesks();
            }
        }
    }

    /// <summary>
    /// 【优化】大厅模式专用：尝试为指定楼层添加空桌子
    /// 当一个空桌子被占用后，智能添加该楼层对应玩法类型的新空桌子
    /// </summary>
    /// <param name="occupiedDeskId">被占用的桌子ID</param>
    /// <param name="occupiedDeskInfo">被占用的桌子信息</param>
    /// <returns>是否成功添加</returns>
    private bool TryAddEmptyDeskForFloor(long occupiedDeskId, Msg_MJDeskInfo occupiedDeskInfo)
    {
        try
        {
            // 1. 获取被占用桌子所属的楼层ID
            long floorId = occupiedDeskInfo?.DeskInfo?.Floor ?? 0;
            int floorIndex = floorList.FindIndex(f => f.Floor == floorId);

            if (floorIndex < 0)
            {
                GF.LogWarning($"[智能添加空桌] 无法确定桌子 {occupiedDeskId} 的楼层ID {floorId}");
                return false;
            }

            // 2. 获取被占用桌子的玩法类型
            MJMethod method = occupiedDeskInfo?.DeskInfo?.MjConfig?.MjMethod ?? MJMethod.Dazhong;

            // 3. 从缓存中查找该楼层的所有空桌子（相同楼层ID）
            var emptyDesksInFloor = deskInfoCache.Values
                .Where(d => !HasPlayers(d) &&
                           (d.DeskInfo?.Floor ?? 0) == floorId &&
                           (d.DeskInfo?.MjConfig?.MjMethod ?? MJMethod.Dazhong) == method &&
                           d.DeskInfo?.DeskId != occupiedDeskId)
                .OrderBy(d => d.DeskInfo?.DeskId ?? 0)
                .ToList();

            if (emptyDesksInFloor.Count == 0)
            {
                GF.LogWarning($"[智能添加空桌] 楼层ID {floorId} 的玩法 {method} 没有可用的空桌子");
                return false;
            }

            // 4. 检查该空桌子是否已经在显示列表中
            var emptyDesk = emptyDesksInFloor.First();
            long emptyDeskId = emptyDesk.DeskInfo?.DeskId ?? 0;

            bool alreadyExists = hallDeskList.Any(mjDesk =>
                (mjDesk.desk1?.DeskInfo?.DeskId == emptyDeskId) ||
                (mjDesk.desk2?.DeskInfo?.DeskId == emptyDeskId));

            if (alreadyExists)
            {
                // 已经存在，不需要添加
                return true;
            }

            // 5. 计算插入位置：空桌子应该按照楼层ID排序
            // 先找出所有已显示的桌子（展开成单个桌子）
            var allDisplayedDesks = new List<Msg_MJDeskInfo>();
            foreach (var mjDesk in hallDeskList)
            {
                if (mjDesk.desk1 != null) allDisplayedDesks.Add(mjDesk.desk1);
                if (mjDesk.desk2 != null) allDisplayedDesks.Add(mjDesk.desk2);
            }

            // 找到所有空桌子的起始位置（有人的桌子都在前面）
            int emptyDeskStartIndex = allDisplayedDesks.FindIndex(d => !HasPlayers(d));
            if (emptyDeskStartIndex < 0)
            {
                // 没有空桌子，添加到末尾
                emptyDeskStartIndex = allDisplayedDesks.Count;
            }

            // 在空桌子区域中，找到应该插入的位置（按楼层ID排序）
            int insertPosition = emptyDeskStartIndex;
            for (int i = emptyDeskStartIndex; i < allDisplayedDesks.Count; i++)
            {
                var desk = allDisplayedDesks[i];
                long existingFloorId = desk.DeskInfo?.Floor ?? 0;

                // 如果当前桌子的楼层ID更大，则插入到这个位置
                if (existingFloorId > floorId)
                {
                    insertPosition = i;
                    break;
                }
                insertPosition = i + 1;
            }

            // 6. 将插入位置转换为 MjDesk 的索引和槽位
            // 计算应该插入到哪个 MjDesk 组
            int mjDeskInsertIndex = insertPosition / 2;
            bool insertInSlot1 = (insertPosition % 2) == 0;

            if (mjDeskInsertIndex >= hallDeskList.Count)
            {
                // 插入到末尾
                hallDeskList.Add(new MjDesk { desk1 = emptyDesk, desk2 = null });
            }
            else if (insertInSlot1)
            {
                // 需要在某个 MjDesk 组的 slot1 位置插入，意味着要插入一个新的 MjDesk 组
                var newMjDesk = new MjDesk { desk1 = emptyDesk, desk2 = null };
                hallDeskList.Insert(mjDeskInsertIndex, newMjDesk);
            }
            else
            {
                // 插入到某个 MjDesk 组的 slot2 位置
                if (hallDeskList[mjDeskInsertIndex].desk2 == null)
                {
                    // slot2 为空，直接填充
                    hallDeskList[mjDeskInsertIndex].desk2 = emptyDesk;
                }
                else
                {
                    // slot2 已占用，需要插入新的 MjDesk 组
                    var newMjDesk = new MjDesk { desk1 = emptyDesk, desk2 = null };
                    hallDeskList.Insert(mjDeskInsertIndex + 1, newMjDesk);
                }
            }

            // 7. 重新加载 TableView 以显示新添加的桌子
            varScroll.ReloadData();

            GF.LogInfo_wl($"[智能添加空桌] 成功为楼层ID {floorId} 的玩法 {method} 添加空桌子 {emptyDeskId}，插入位置: {insertPosition}");
            return true;
        }
        catch (System.Exception ex)
        {
            GF.LogError($"[智能添加空桌] 发生错误: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 【优化】更新单个 Cell 的 UI
    /// </summary>
    /// <param name="cellIndex">Cell 索引</param>
    /// <param name="mjDesk">MjDesk 数据</param>
    /// <param name="isHallMode">是否为大厅模式</param>
    private void UpdateSingleCellUI(int cellIndex, MjDesk mjDesk, bool isHallMode)
    {
        // 通过 TableView 获取 Cell
        TableViewCell cell = varScroll.GetCellAtCol(cellIndex);
        if (cell != null && cell.gameObject != null)
        {
            // Cell 已经存在且在可见区域，直接更新数据
            UpdateItemData(cell.gameObject, mjDesk);
        }
    }

    /// <summary>
    /// 【优化】判断桌子是否应该被显示
    /// </summary>
    /// <param name="deskInfo">桌子信息</param>
    /// <param name="isHallMode">是否为大厅模式</param>
    /// <returns>是否应该显示</returns>
    private bool ShouldDeskBeDisplayed(Msg_MJDeskInfo deskInfo, bool isHallMode)
    {
        if (deskInfo?.DeskInfo == null)
            return false;

        // 判断桌子是否有玩家
        bool hasPlayers = HasPlayers(deskInfo);

        if (isHallMode)
        {
            // 大厅模式：有人的桌子显示，或者作为空桌子补充显示
            return hasPlayers;
        }
        else
        {
            // 楼层模式：有人的桌子优先显示
            return hasPlayers;
        }
    }

    /// <summary>
    /// 【优化】更新桌子缓存（在收到服务器数据时调用）
    /// </summary>
    /// <param name="deskInfos">桌子信息列表</param>
    private void UpdateDeskCache(List<Msg_MJDeskInfo> deskInfos)
    {
        if (deskInfos == null || deskInfos.Count == 0)
            return;

        // GF.LogInfo_wl($"[缓存更新] 更新桌子缓存，共 {deskInfos.Count} 个桌子");

        // 清空旧缓存（可选，根据需求决定是否保留旧缓存）
        // deskInfoCache.Clear();

        foreach (var deskInfo in deskInfos)
        {
            if (deskInfo?.DeskInfo != null)
            {
                long deskId = deskInfo.DeskInfo.DeskId;
                deskInfoCache[deskId] = deskInfo;
            }
        }

        //GF.LogInfo_wl($"[缓存更新] 缓存更新完成，当前缓存中有 {deskInfoCache.Count} 个桌子");
    }

    #endregion
    /// <summary>
    /// 刷新楼层UI
    /// </summary>
    public void UpdateFloor()
    {
        ClearUI(varCreateFloorConter.transform);
        ClearUI(varFloorContent.transform);
        HallLogic();
        AddButtonClickEvent();
        LayoutRebuilder.ForceRebuildLayoutImmediate(varCreateFloorConter.GetComponent<RectTransform>());
        AddLouCnengGroup();
        varCreateFloorConter.transform.parent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 1;
        varGuanli.SetActive(floorList.Count == 0);
        bool hasCreatePermission = CanCreateRoom();
        //显示管理玩法按钮：只有最上级且是创建者才能显示
        varGuanli.transform.Find("guanliwanfa").gameObject.SetActive(hasCreatePermission);
        if (hasCreatePermission)
        {
            varGuanli.transform.Find("text").GetComponent<Text>().text = "当前亲友圈还未设置游戏哦，请点击管理玩法。";
        }
        else
        {
            varGuanli.transform.Find("text").GetComponent<Text>().text = "当前亲友圈还未设置游戏哦，请联系圈主创建房间";
        }
        varZuozi.SetActive(floorList.Count > 0);
    }

    /// <summary>
    /// 改名后刷新桌子信息（在楼层列表更新后调用）
    /// </summary>
    private void RefreshDesksAfterFloorUpdate()
    {
        GF.LogInfo_wl($"[改名刷新] 执行桌子刷新，当前楼层ID: {chooseFloor}");

        if (chooseFloor == 0)
        {
            // 当前在大厅，重新进入大厅以刷新桌子
            GF.LogInfo_wl("[改名刷新] 刷新大厅桌子");
            EnterHall();
        }
        else
        {
            // 当前在某个楼层，重新进入该楼层以刷新桌子
            var currentFloor = floorList.FirstOrDefault(f => f.Floor == chooseFloor);
            if (currentFloor != null)
            {
                GF.LogInfo_wl($"[改名刷新] 刷新楼层 {chooseFloor} 的桌子");
                EnterMjFloor(currentFloor);
            }
            else
            {
                GF.LogWarning($"[改名刷新] 未找到楼层 {chooseFloor}，改为刷新大厅");
                chooseFloor = 0;
                chooseUIFloor = 0;
                EnterHall();
            }
        }
    }

    /// <summary>
    /// 添加按钮点击事件 - 创建楼层UI项并设置交互逻辑
    /// </summary>
    private void AddButtonClickEvent()
    {
        // 创建所有楼层UI项
        for (int i = 0; i < floorList.Count; i++)
        {
            CreateFloorUIItem(i, floorList[i]);
        }

        if (externalTargetFloor != null && floorList.Count > 0)
        {
            // 外部指定了目标楼层（从其他界面跳转过来）
            var targetFloor = floorList.FirstOrDefault(f => f.Floor == externalTargetFloor.Floor);
            if (targetFloor != null)
            {
                chooseFloor = targetFloor.Floor;
                chooseUIFloor = floorList.IndexOf(targetFloor) + 1;
                EnterMjFloor(targetFloor);
            }
            else
            {
                GF.LogWarning($"未找到外部指定的楼层: {externalTargetFloor.Floor}，进入大厅");
                chooseFloor = 0;
                chooseUIFloor = 0;
                EnterHall();
            }
            // 清空外部目标楼层，避免重复使用
            externalTargetFloor = null;
        }
        else if (floorList.Count > 0 && chooseFloor == 0)
        {
            // 只有在当前未选择任何楼层（如初次进入）时，才默认进入大厅
            chooseFloor = 0;
            chooseUIFloor = 0;
            EnterHall();
        }
    }
    /// <summary>
    /// 创建单个楼层UI项
    /// </summary>
    /// <param name="index">楼层索引</param>
    /// <param name="floorData">楼层数据</param>
    private void CreateFloorUIItem(int index, Msg_Floor floorData)
    {
        if (varLouCengTypeItem == null || varCreateFloorConter == null || floorData == null)
        {
            GF.LogWarning("CreateFloorUIItem: 预制体或容器或数据为空");
            return;
        }

        GameObject floorItem = Instantiate(varLouCengTypeItem.gameObject, varCreateFloorConter.transform);
        floorItem.name = floorData.Floor.ToString();
        Transform floorTransform = floorItem.transform;
        Transform inputQyqName = floorTransform.Find("InputQyqName");

        // 设置楼层基本信息
        SetupFloorBasicInfo(floorTransform, floorData, index);
        // 设置楼层图标
        SetupFloorIcon(floorTransform, floorData);
        // 设置选中状态
        SetupFloorSelectionState(floorTransform, floorData);
        // 设置按钮事件和状态
        SetupFloorButtons(floorTransform, inputQyqName, floorData, index, floorItem);
    }

    /// <summary>
    /// 设置楼层基本信息（楼层号和名称）
    /// </summary>
    private void SetupFloorBasicInfo(Transform floorTransform, Msg_Floor floorData, int index)
    {
        if (floorTransform == null || floorData == null) return;

        // 设置楼层号
        Transform louCengNumTrans = floorTransform.Find("LouCengNum");
        if (louCengNumTrans != null)
        {
            Text floorNumText = louCengNumTrans.GetComponent<Text>();
            if (floorNumText != null)
            {
                floorNumText.text = (index + 1).ToString();
            }
        }
        // 设置楼层名称
        Transform creatRoomNameTrans = floorTransform.Find("CreatRoomName");
        if (creatRoomNameTrans != null)
        {
            Text floorNameText = creatRoomNameTrans.GetComponent<Text>();
            if (floorNameText != null)
            {
                string BaseCoin = (floorData.Config != null) ? floorData.Config.BaseCoin.ToString() : "";
                floorNameText.text = GetFloorGameMethod(floorData) + BaseCoin + "分";
            }
        }
        // 设置重命名输入框
        Transform qhQYQAccountTrans = floorTransform.Find("InputQyqName/QhQYQAccount");
        if (qhQYQAccountTrans != null)
        {
            InputField renameInputField = qhQYQAccountTrans.GetComponent<InputField>();
            Text renameInputText = qhQYQAccountTrans.Find("Placeholder")?.GetComponent<Text>();

            if (renameInputField != null && renameInputText != null && floorData.Config != null)
            {
                renameInputText.text = floorData.Config.DeskName;
                // 非最上级时禁用输入框编辑，只显示当前名字
                bool hasCreatePermission = CanCreateRoom();
                renameInputField.interactable = hasCreatePermission;
                if (!hasCreatePermission)
                {
                    // 非最上级时，将当前名字显示在输入框的Text组件中
                    Text inputDisplayText = qhQYQAccountTrans.Find("Text")?.GetComponent<Text>();
                    if (inputDisplayText != null)
                    {
                        inputDisplayText.text = floorData.Config.DeskName;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 设置楼层图标
    /// </summary>
    private void SetupFloorIcon(Transform floorTransform, Msg_Floor floorData)
    {
        if (floorTransform == null || floorData == null) return;
        string methodName = GetFloorGameMethod(floorData);
        Sprite iconSprite = IconSprs?.Find(s => s.name == methodName);
        if (iconSprite != null)
        {
            Transform iconTrans = floorTransform.Find("icon");
            if (iconTrans != null)
            {
                Image iconImage = iconTrans.GetComponent<Image>();
                if (iconImage != null) iconImage.sprite = iconSprite;
            }
            varQuickStartObj.transform.Find("icon").GetComponent<Image>().sprite = iconSprite;
        }
    }

    /// <summary>
    /// 设置楼层选中状态
    /// </summary>
    private void SetupFloorSelectionState(Transform floorTransform, Msg_Floor floorData)
    {
        if (floorTransform == null || floorData == null) return;
        bool isSelected = (chooseFloor == floorData.Floor);
        Transform selectionMarkTrans = floorTransform.Find("xuanzhong");
        if (selectionMarkTrans != null)
        {
            selectionMarkTrans.gameObject.SetActive(isSelected);
        }
    }
    /// <summary>
    /// 设置楼层按钮事件和状态
    /// </summary>
    private void SetupFloorButtons(Transform floorTransform, Transform inputQyqName,
        Msg_Floor floorData, int index, GameObject floorItem)
    {
        if (floorTransform == null || inputQyqName == null || floorData == null) return;
        // 获取按钮组件
        Button deleteBtn = inputQyqName.Find("DeleteBtn")?.GetComponent<Button>();
        Button setBtn = inputQyqName.Find("SetBtn")?.GetComponent<Button>();
        Button renameBtn = inputQyqName.Find("RenameBtn")?.GetComponent<Button>();
        Button viewBtn = inputQyqName.Find("ViewBtn")?.GetComponent<Button>();
        Button floorNumBtn = floorTransform.Find("LouCengNum")?.GetComponent<Button>();
        // 设置按钮交互状态 - 必须是最上级且是创建者才有权限
        bool hasCreatePermission = CanCreateRoom();

        if (deleteBtn != null) deleteBtn.interactable = hasCreatePermission;
        if (setBtn != null) setBtn.interactable = hasCreatePermission;
        if (renameBtn != null) renameBtn.interactable = hasCreatePermission;
        if (viewBtn != null) viewBtn.interactable = !hasCreatePermission;
        if (floorNumBtn != null) floorNumBtn.interactable = true;

        // 设置按钮点击事件
        if (deleteBtn != null)
        {
            SetupButton(deleteBtn, hasCreatePermission, () =>
                Util.GetInstance().OpenMJConfirmationDialog("删除楼层", "确定删除楼层吗？", () => DeleteFloor(floorData)));
        }
        if (setBtn != null)
        {
            SetupButton(setBtn, hasCreatePermission, () => Modifyfloor(floorData));
        }
        if (renameBtn != null)
        {
            SetupButton(renameBtn, hasCreatePermission, () =>
            {
                // 只有最上级且是创建者才能重命名
                if (hasCreatePermission)
                {
                    StartRenameFloor(floorTransform, floorData);
                }
            });
        }
        if (viewBtn != null)
        {
            SetupButton(viewBtn, !hasCreatePermission, async () =>
            {
                varRoomInfo.SetActive(true);
                Text text = varRoomInfo.transform.Find("Scroll View/Viewport/Content/Text").GetComponent<Text>();
                //获取当前玩法的通用设置并且显示               
                text.text = GetFloorConfigInfo(floorData);
            });
        }

        // 楼层切换按钮（最重要的交互）
        if (floorNumBtn != null)
        {
            SetupButton(floorNumBtn, true, () => SelectFloorOrHall(floorData, floorItem));
        }
    }
    private List<string> floorNameList = new List<string>();
    /// <summary>
    /// 大厅逻辑处理
    /// </summary>
    private void HallLogic()
    {

        int childCount = varFloorContent.transform.childCount;
        if (childCount > 0)
        {
            for (int i = childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(varFloorContent.transform.GetChild(i).gameObject);
            }
        }

        GameObject HallFloorItem = Instantiate(varFloorItem.gameObject, varFloorContent.transform);
        //当有楼层时 大厅按钮显示 否则隐藏
        HallFloorItem.SetActive(floorList.Count > 0);
        HallFloorItem.name = 0.ToString();

        // 获取大厅文本组件，用于设置选中时的黄色
        Text hallText = HallFloorItem.transform.Find("Text").GetComponent<Text>();
        hallText.text = MixHallName;

        Toggle HallChoose = HallFloorItem.transform.GetComponent<Toggle>();

        // 添加到 ToggleGroup（如果存在）
        var floorContentToggleGroup = varFloorContent.GetComponent<ToggleGroup>();
        if (floorContentToggleGroup == null)
        {
            floorContentToggleGroup = varFloorContent.AddComponent<ToggleGroup>();
        }
        HallChoose.group = floorContentToggleGroup;

        HallChoose.onValueChanged.RemoveAllListeners();
        HallChoose.onValueChanged.AddListener((isOn) =>
        {
            if (!isOn)
            {
                return;
            }

            CloseAllChooseViews();
            chooseFloor = 0;
            chooseUIFloor = 0;

            EnterHall();
        });

        // 检查是否当前选中的是大厅（chooseFloor == 0 或 chooseUIFloor == 0）
        bool isHallSelected = (chooseFloor == 0 || chooseUIFloor == 0);
        if (isHallSelected)
        {
            // 设置大厅为选中状态（不要在这里调用 CloseAllChooseViews，避免干扰）
            HallChoose.isOn = true;

            // 直接显示大厅的选中标记，不依赖 CloseAllChooseViews
            var hallMark = HallFloorItem.transform.Find("xuanzhong");
            if (hallMark != null)
            {
                hallMark.gameObject.SetActive(true);
            }
        }
        else
        {
            HallChoose.isOn = false;
        }

        floorNameList.Clear();
        // 根据游戏类型分组，需要处理跑得快等不同类型
        var groupsByName = floorList.GroupBy(f => GetFloorGameMethod(f));
        foreach (var group in groupsByName)
        {
            string deskName = group.Key;
            floorNameList.Add(deskName);
            GameObject floorItem = Instantiate(varFloorItem.gameObject, varFloorContent.transform);
            floorItem.SetActive(true);
            floorItem.name = deskName;
            var nameText = floorItem.transform.Find("Text").GetComponent<Text>();
            nameText.text = deskName;
            var chooseView = floorItem.transform.Find("ChooseGameView")?.gameObject;
            var chooseGameContent = floorItem.transform.Find("ChooseGameView/Viewport/ChooseGameContent").gameObject;
            var floorItemBtn = floorItem.GetComponent<Toggle>();
            floorItemBtn.onValueChanged.RemoveAllListeners();
            AddToggleListener(floorItemBtn, floorItem.transform);
            ClearUI(chooseGameContent.transform);
            // 检查当前楼层分组是否包含选中的楼层
            bool groupContainsSelectedFloor = group.Any(f => f.Floor == chooseFloor);

            foreach (var floorData in group)
            {
                var go = Instantiate(varChooseGameItem.gameObject, chooseGameContent.transform);
                go.name = floorData.Floor.ToString();
                var text = go.transform.Find("Text").GetComponent<Text>();
                text.text = floorData.Config.DeskName;
                var btn = go.GetComponent<Toggle>();
                var toggleGroup = chooseGameContent.GetComponent<ToggleGroup>();
                if (toggleGroup == null)
                {
                    toggleGroup = chooseGameContent.AddComponent<ToggleGroup>();
                }
                btn.group = toggleGroup;
                btn.onValueChanged.RemoveAllListeners();

                // 【修复】捕获当前楼层数据，避免闭包问题
                long capturedFloorId = floorData.Floor;
                btn.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                    {
                        // 【关键修复】检查是否真的需要切换楼层，避免重复请求
                        if (chooseFloor != capturedFloorId)
                        {
                          //  GF.LogInfo_wl($"[楼层切换] 从楼层{chooseFloor}切换到楼层{capturedFloorId}");
                            SelectFloorOrHall(floorData, go);
                        }
                        else
                        {
                           // GF.LogInfo_wl($"[楼层切换] 已经在楼层{capturedFloorId}，跳过重复请求");
                        }
                    }
                });
                // 如果这是当前选中的楼层，设置为选中状态
                if (floorData.Floor == chooseFloor && chooseFloor != 0)
                {
                    btn.isOn = true;
                }
            }
            // 如果当前分组包含选中的楼层，只显示选中标记，不自动展开侧边栏
            if (groupContainsSelectedFloor && chooseFloor != 0)
            {
                var xuanzhongMark = floorItem.transform.Find("xuanzhong")?.gameObject;
                xuanzhongMark.SetActive(true);
            }

        }

    }

    /// <summary>
    /// 删除楼层请求
    /// </summary>
    private void DeleteFloor(Msg_Floor floorData)
    {
        Msg_DelMjFloorRq req = MessagePool.Instance.Fetch<Msg_DelMjFloorRq>();
        req.Floor = floorData.Floor;
        req.ClubId = GetCorrectClubId();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
              (HotfixNetworkComponent.AccountClientName, MessageID.Msg_DelMjFloorRq, req);
    }
    /// 指定进入楼层请求
    /// </summary>
    public void EnterMjFloor(Msg_Floor floorData, long clubId = 0, bool shouldResetScroll = true)
    {
        m_ShouldResetScroll = shouldResetScroll;
        Msg_EnterMjFloorRq req = MessagePool.Instance.Fetch<Msg_EnterMjFloorRq>();
        req.Floor = floorData.Floor;
        if (clubId == 0)
        {
            req.ClubId = GetCorrectClubId();
        }
        else
        {
            req.ClubId = clubId;
        }
       // GF.LogInfo_wl($"[获取楼层桌子信息] 指定进入楼层{floorData.Floor}，亲友圈ID: {req.ClubId}");
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
              (HotfixNetworkComponent.AccountClientName, MessageID.Msg_EnterMjFloorRq, req);
    }
    #region UI刷新方法
    /// <summary>
    /// 当前选择的服务器楼层

    /// </summary>
    private long chooseFloor = 0;
    //当前选择的显示楼层
    private int chooseUIFloor = 0;

    //楼层列表
    public List<Msg_Floor> floorList = new List<Msg_Floor>();

    //大厅信息 当前为固定一个大厅
    //桌子列表
    private List<MjDesk> deskList = new List<MjDesk>();
    private List<MjDesk> hallDeskList = new List<MjDesk>();

    // 【优化】桌子信息缓存：用于增量更新，避免全局刷新
    // Key: DeskId, Value: 桌子信息
    private Dictionary<long, Msg_MJDeskInfo> deskInfoCache = new Dictionary<long, Msg_MJDeskInfo>();

    // 【优化】是否启用增量更新（可配置）
    private bool enableIncrementalUpdate = true;
    #endregion
    /// <summary>
    /// 关闭时清理资源
    /// </summary>
    protected override void OnClose(bool isShutdown, object userData)
    {
        varScroll.ClearAllClos();
        // 移除网络监听
        RemoveNetworkListeners();
        // 取消订阅背景变化事件
        UnsubscribeFromBackgroundEvents();
        // 【优化】清理桌子缓存
        deskInfoCache.Clear();

        base.OnClose(isShutdown, userData);
    }
    #region TableView相关
    [Header("TableView组件")]
    public TableViewX varScroll;
    public GameObject zuoziContent;
    public GameObject itemPrefab;

    private const string CELL_REUSE_IDENTIFIER = "cell";
    // <summary>
    /// 初始化楼层滚动视图
    /// </summary>
    private void InitializeScroll()
    {
        varScroll.SetNumberOfColsForTableView(tv => deskList.Count);
        varScroll.SetWidthForColInTableView((tv, col) =>
        {
            // 添加额外的宽度偏移来解决滚动边缘时桌子提前消失的问题
            float baseWidth = itemPrefab.GetComponent<RectTransform>().rect.width;
            // 为所有列添加缓冲宽度，确保边缘滚动时正确显示
            return baseWidth + 70f;
        });
        varScroll.SetCellForColInTableView(CellForRowInTableView);
    }
    // <summary>
    /// 初始化大厅滚动视图
    /// </summary>
    private void InitializeHallScroll()
    {
        varScroll.SetNumberOfColsForTableView(tv => hallDeskList.Count);
        varScroll.SetWidthForColInTableView((tv, col) =>
        {
            // 添加额外的宽度偏移来解决滚动边缘时桌子提前消失的问题
            float baseWidth = itemPrefab.GetComponent<RectTransform>().rect.width;
            // 为所有列添加缓冲宽度，确保边缘滚动时正确显示
            return baseWidth + 70f;
        });
        varScroll.SetCellForColInTableView(CellForRowInTableViewHall);
    }
    /// <summary>
    /// 为楼层桌子创建或复用Cell
    /// </summary>
    public TableViewCell CellForRowInTableView(TableViewX tv, int col)
    {
        TableViewCell cell = tv.GetReusableCell(CELL_REUSE_IDENTIFIER);

        if (cell == null)
        {
            GameObject cellObject = Instantiate(itemPrefab, zuoziContent.transform);
            cell = cellObject.GetComponent<TableViewCell>();
            cell.name = $"Cell {col}";
            cell.reuseIdentifier = CELL_REUSE_IDENTIFIER;
        }
        cell.gameObject.SetActive(true);
        UpdateItemData(cell.gameObject, deskList[col]);
        return cell;
    }
    /// <summary>
    /// 为大厅桌子创建或复用Cell
    /// </summary>
    /// <param name="tv"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public TableViewCell CellForRowInTableViewHall(TableViewX tv, int col)
    {
        TableViewCell cell = tv.GetReusableCell(CELL_REUSE_IDENTIFIER);

        if (cell == null)
        {
            GameObject cellObject = Instantiate(itemPrefab, varHallDeskContent.transform);
            cell = cellObject.GetComponent<TableViewCell>();
            cell.name = $"Cell {col}";
            cell.reuseIdentifier = CELL_REUSE_IDENTIFIER;
        }
        cell.gameObject.SetActive(true);
        UpdateItemData(cell.gameObject, hallDeskList[col]);
        return cell;
    }
    /// <summary>
    /// 更新Cell数据
    /// </summary>
    private void UpdateItemData(GameObject cell, MjDesk data)
    {
        // 判断是否为大厅模式
        bool isHallMode = chooseFloor == 0;

        if (isHallMode)
        {
            // 大厅模式：根据每个桌子的楼层ID获取对应的楼层数据

            UpdateTableItemForHall(cell.transform.Find("table1").GetComponent<MJHomeTableItem>(), data.desk1);
            UpdateTableItemForHall(cell.transform.Find("table2").GetComponent<MJHomeTableItem>(), data.desk2);
        }
        else
        {
            // 楼层模式：使用当前楼层数据
            var currentFloor = GetCurrentFloor();
            cell.transform.Find("table1").GetComponent<MJHomeTableItem>().Init(data.desk1, currentFloor);
            cell.transform.Find("table2").GetComponent<MJHomeTableItem>().Init(data.desk2, currentFloor);
        }
    }

    /// <summary>
    /// 为大厅模式更新单个桌子项
    /// </summary>
    /// <param name="tableItem">桌子UI项</param>
    /// <param name="deskInfo">桌子信息</param>
    private void UpdateTableItemForHall(MJHomeTableItem tableItem, Msg_MJDeskInfo deskInfo)
    {
        if (deskInfo == null)
        {
            tableItem.Init(null, null);
            return;
        }
        tableItem.Init(deskInfo, null);

    }
    #endregion

    /// <summary>
    /// 获取当前选中的楼层
    /// </summary>
    /// <returns></returns>
    public Msg_Floor GetCurrentFloor()
    {
        return floorList.FirstOrDefault(floor => floor.Floor == chooseFloor);
    }
    /// <summary>
    /// 清理UI容器
    /// </summary>
    private void ClearUI(Transform parent)
    {
        // 使用DestroyImmediate确保立即销毁,避免异步Destroy导致的重复显示问题
        int childCount = parent.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }
    /// <summary>
    /// 关闭所有已打开的chooseView界面
    /// </summary>
    private void CloseAllChooseViews()
    {
        foreach (Transform item in varFloorContent.transform)
        {
            // 跳过大厅项（name == "0"），如果当前选中的是大厅
            bool isHallItem = (item.name == "0");
            bool isHallSelected = (chooseFloor == 0 || chooseUIFloor == 0);

            var chooseView = item.Find("ChooseGameView")?.gameObject;
            chooseView.SetActive(false);

            // 隐藏所有floorItem的选中标记（但如果是大厅且大厅被选中，则保留）
            var xuanzhongMark = item.Find("xuanzhong")?.gameObject;
            if (xuanzhongMark != null)
            {
                if (isHallItem && isHallSelected)
                {
                    // 大厅被选中时，保持大厅的选中标记显示
                    xuanzhongMark.SetActive(true);
                }
                else
                {
                    xuanzhongMark.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// 关闭除了指定项之外的所有chooseView
    /// </summary>
    private void CloseOtherChooseViews(Transform exceptItem)
    {
        foreach (Transform item in varFloorContent.transform)
        {
            if (item == exceptItem) continue; // 跳过当前项

            var chooseView = item.Find("ChooseGameView")?.gameObject;
            chooseView.SetActive(false);

            // 隐藏其他floorItem的选中标记
            var xuanzhongMark = item.Find("xuanzhong")?.gameObject;
            xuanzhongMark.SetActive(false);

            // 重置其他Toggle状态（不触发事件）
            var toggle = item.GetComponent<Toggle>();
            if (toggle != null && toggle.isOn)
            {
                toggle.onValueChanged.RemoveAllListeners();
                toggle.isOn = false;
                // 重新添加监听器
                AddToggleListener(toggle, item);
            }
        }
    }

    /// <summary>
    /// 为Toggle添加监听器（避免重复代码）
    /// </summary>
    private void AddToggleListener(Toggle toggle, Transform floorItem)
    {
        var chooseView = floorItem.Find("ChooseGameView")?.gameObject;
        var chooseGameContent = floorItem.Find("ChooseGameView/Viewport/ChooseGameContent")?.gameObject;
        var xuanzhongMark = floorItem.Find("xuanzhong")?.gameObject; // 获取选中标记
        string deskName = floorItem.name;

        if (chooseView == null || chooseGameContent == null) return;

        toggle.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                // 当选中当前按钮时，先关闭所有其他的chooseView（除了当前项）
                CloseOtherChooseViews(floorItem);
                // 打开当前的chooseView
                chooseView.SetActive(true);
                // 显示当前floorItem的选中标记
                xuanzhongMark.SetActive(true);
                // 自动选中内部应该选中的楼层 Toggle
                bool foundTargetFloor = false;
                foreach (Transform child in chooseGameContent.transform)
                {
                    var childToggle = child.GetComponent<Toggle>();
                    // 检查是否是目标楼层
                    if (child.name == chooseFloor.ToString())
                    {
                        childToggle.isOn = true;
                        foundTargetFloor = true;
                    }
                    else
                    {
                        childToggle.isOn = false;
                    }
                }

                if (!foundTargetFloor && chooseGameContent.transform.childCount > 0)
                {
                    // 如果没找到目标楼层，选中第一个
                    var firstChild = chooseGameContent.transform.GetChild(0);
                    var firstToggle = firstChild.GetComponent<Toggle>();
                    if (firstToggle != null)
                    {
                        firstToggle.isOn = true;
                    }
                }
            }
            else
            {
                // 当取消选中时，关闭当前的chooseView
                chooseView.SetActive(false);
                // 隐藏当前floorItem的选中标记
                xuanzhongMark.SetActive(false);
            }
        });
    }
    /// <summary>
    /// 设置按钮事件
    /// </summary>
    private void SetupButton(Button btn, bool active, System.Action action)
    {
        btn.onClick.RemoveAllListeners();
        btn.gameObject.SetActive(active);
        if (active) btn.onClick.AddListener(() => action());
    }

    /// <summary>
    /// 清除选中状态
    /// </summary>
    private void ClearSelection(Transform parent)
    {
        foreach (Transform item in parent)
            item.Find("xuanzhong")?.gameObject.SetActive(false);
    }

    /// <summary>
    /// 选择楼层或大厅
    /// </summary>
    public void SelectFloorOrHall(Msg_Floor floorData, GameObject typeItem)
    {
        // 【防重复请求】检查是否已经在目标楼层
        if (chooseFloor == floorData.Floor)
        {
            GF.LogInfo_wl($"[楼层切换] 已经在楼层{floorData.Floor}，跳过SelectFloorOrHall");
            return;
        }

        GF.LogInfo_wl($"[楼层切换] SelectFloorOrHall: 从楼层{chooseFloor}切换到楼层{floorData.Floor}");

        //当前选选择的楼层
        chooseFloor = floorData.Floor;
        chooseUIFloor = floorList.IndexOf(floorData) + 1;
        SelectFloor(floorData, typeItem);
    }
    #region 进入大厅逻辑
    /// <summary>
    /// 进入大厅
    /// </summary>
    private void EnterHall()
    {
        float currentTime = Time.time;
        if (lastRequestedFloorId == 0 && (currentTime - lastRequestTime) < REQUEST_COOLDOWN)
        {
            return;
        }
        // 记录本次请求
        lastRequestedFloorId = 0;
        lastRequestTime = currentTime;
        m_ShouldResetScroll = true;
        chooseFloor = 0;
        chooseUIFloor = 0;
        // 清除右侧楼层列表中所有楼层项的选中标记
        foreach (Transform floorItem in varCreateFloorConter.transform)
        {
            floorItem.Find("xuanzhong")?.gameObject.SetActive(false);
        }
        // 清除左侧楼层分组的所有选中状态（除了大厅）
        foreach (Transform item in varFloorContent.transform)
        {
            if (item.name == "0") continue;
            item.Find("xuanzhong")?.gameObject.SetActive(false);
            item.Find("ChooseGameView")?.gameObject.SetActive(false);
        }
        // 设置大厅的 Toggle 和选中标记
        var hallItem = varFloorContent.transform.Find("0");
        if (hallItem != null)
        {
            // 设置 Toggle 为选中状态
            var hallToggle = hallItem.GetComponent<Toggle>();
            hallToggle.isOn = true;
            // 显示选中标记
            var hallMark = hallItem.Find("xuanzhong");
            hallMark.gameObject.SetActive(true);
        }
        //点击进入大厅 请求大厅信息(全部楼层信息)
        EnterMjFloor(new Msg_Floor { Floor = 0 });
    }
    #endregion
    /// <summary>
    /// 选择楼层
    /// </summary>
    private void SelectFloor(Msg_Floor floorData, GameObject typeItem)
    {
        ClearSelection(varCreateFloorConter.transform);
        typeItem.transform.Find("xuanzhong").gameObject.SetActive(true);

        // 添加边界检查，防止访问超出范围的子对象
        int childIndex = chooseUIFloor - 1;
        if (childIndex >= 0 && childIndex < varCreateFloorConter.transform.childCount)
        {
            var child = varCreateFloorConter.transform.GetChild(childIndex);
            var xuanzhong = child.Find("xuanzhong");
            xuanzhong.gameObject.SetActive(true);
        }
        EnterMjFloor(floorData);
    }

    #region 背景设置监听

    /// <summary>
    /// 订阅背景变化事件
    /// </summary>
    private void SubscribeToBackgroundEvents()
    {
        PersonalitySet.OnBackgroundConfirmed += OnBackgroundChanged;
    }

    /// <summary>
    /// 取消订阅背景变化事件
    /// </summary>
    private void UnsubscribeFromBackgroundEvents()
    {
        // 取消订阅PersonalitySet的背景确认事件
        PersonalitySet.OnBackgroundConfirmed -= OnBackgroundChanged;
        //GF.LogInfo_wl("已取消订阅背景变化事件");
    }
    /// <summary>
    /// 背景变化时的回调方法
    /// </summary>
    /// <param name="newBackgroundIndex">新的背景索引</param>
    private void OnBackgroundChanged(int newBackgroundIndex)
    {
        // 保存新的背景索引到本地
        SaveRoomBackgroundIndex(newBackgroundIndex);
        // 应用背景变化
        varBg.GetComponent<Image>().sprite = sprs[newBackgroundIndex];
    }

    /// <summary>
    /// 保存房间背景索引到本地
    /// </summary>
    /// <param name="index">要保存的背景索引</param>
    private void SaveRoomBackgroundIndex(int index)
    {
        currentRoomBackgroundIndex = index;
        PlayerPrefs.SetInt(ROOM_BACKGROUND_INDEX_KEY, index);
        PlayerPrefs.Save(); // 立即保存到磁盘
    }

    /// <summary>
    /// 从本地读取房间背景索引
    /// </summary>
    /// <returns>背景索引</returns>
    private int GetRoomBackgroundIndex()
    {
        return PlayerPrefs.GetInt(ROOM_BACKGROUND_INDEX_KEY, 0); // 默认返回0（第一个背景）
    }

    /// <summary>
    /// 加载房间背景设置
    /// </summary>
    private void LoadRoomBackgroundSetting()
    {
        // 从本地读取背景索引
        currentRoomBackgroundIndex = GetRoomBackgroundIndex();
        // 应用加载的背景设置
        varBg.GetComponent<Image>().sprite = sprs[currentRoomBackgroundIndex];
    }


    #endregion

    #region 楼层改名功能

    /// <summary>
    /// 开始楼层改名 - 仅进入输入状态，不执行改名逻辑
    /// </summary>
    /// <param name="floorTransform">楼层UI项Transform</param>
    /// <param name="floorData">楼层数据</param>
    private void StartRenameFloor(Transform floorTransform, Msg_Floor floorData)
    {
        //进入输入状态  清空文本并闪烁可输入状态
        Transform inputQyqName = floorTransform.Find("InputQyqName");
        if (inputQyqName == null) return;
        InputField inputField = inputQyqName.Find("QhQYQAccount")?.GetComponent<InputField>();
        if (inputField == null) return;
        inputField.text = "";
        inputField.ActivateInputField();
    }
    /// <summary>
    /// 离开输入框后处理输入数据 - 楼层列表输入框
    /// </summary>
    public void OnInputFieldEndEdit()
    {
        // 检查是否有权限修改
        if (!CanCreateRoom())
        {
            GF.UI.ShowToast("您没有权限修改房间名称");
            return;
        }
        HandleInputFieldEndEdit(false, "OnInputFieldEndEdit");
    }

    /// <summary>
    /// CreatorInterface输入框结束编辑处理 - 直接使用当前进入的楼层
    /// </summary>
    public void OnCreatorInterfaceInputFieldEndEdit()
    {
        // 检查是否有权限修改
        if (!CanCreateRoom())
        {
            GF.UI.ShowToast("您没有权限修改房间名称");
            return;
        }

        if (floorList.Count == 0)
        {
            GF.UI.ShowToast("当前无楼层，请先创建楼层");
            return;
        }
        HandleInputFieldEndEdit(true, "OnCreatorInterfaceInputFieldEndEdit");
    }

    /// <summary>
    /// 通用输入框结束编辑处理方法
    /// </summary>
    /// <param name="isCreatorInterface">是否为CreatorInterface的输入框</param>
    /// <param name="methodName">调用此方法的方法名，用于日志记录</param>
    private void HandleInputFieldEndEdit(bool isCreatorInterface, string methodName)
    {
        try
        {
            // 获取当前活动的输入框组件
            InputField currentInputField = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.GetComponent<InputField>();
            if (currentInputField == null) return;
            string inputText = currentInputField.text?.Trim() ?? "";
            // 获取原始名称用于对比
            string originalName = "";
            var targetFloor = isCreatorInterface ? GetCurrentFloor() : GetCurrentEditingFloor();
            if (targetFloor != null)
            {
                originalName = targetFloor.Config.DeskName ?? "";
            }

            // 如果输入为空或者未改变 则不处理
            if (string.IsNullOrEmpty(inputText) || inputText == originalName)
            {
                string interfaceType = isCreatorInterface ? "CreatorInterface" : "楼层列表";
                // 恢复原始文本显示
                currentInputField.text = "";
                return;
            }

            // 在弹出确认对话框之前保存必要的数据
            string savedInputText = inputText;
            Msg_Floor savedTargetFloor = targetFloor;

            Util.GetInstance().OpenMJConfirmationDialog("修改名字", "确认修改房间名吗？",
                // 确认回调
                () =>
                {
                    ProcessInputFieldEndEditWithSavedData(isCreatorInterface, savedInputText, savedTargetFloor);
                },
                // 取消回调 - 恢复原始文本
                () =>
                {
                    currentInputField.text = "";  // 清空输入框，显示placeholder
                });
        }
        catch (System.Exception ex)
        {
            GF.LogError($"{methodName}处理时发生错误: {ex.Message}");
        }
    }
    /// <summary>
    /// 使用保存的数据处理输入框结束编辑
    /// </summary>
    /// <param name="isCreatorInterface">是否为CreatorInterface的输入框</param>
    /// <param name="inputText">保存的输入文本</param>
    /// <param name="targetFloor">保存的目标楼层数据</param>
    private void ProcessInputFieldEndEditWithSavedData(bool isCreatorInterface, string inputText, Msg_Floor targetFloor)
    {
        // 根据类型调用不同的处理方法
        if (isCreatorInterface)
        {
            ProcessCreatorInterfaceValidInputWithFloor(inputText, targetFloor);
        }
        else
        {
            ProcessValidInputWithFloor(inputText, targetFloor);
        }
    }
    /// <summary>
    /// 处理CreatorInterface的有效输入 - 使用指定的楼层数据
    /// </summary>
    /// <param name="validText">有效的输入文本</param>
    /// <param name="targetFloor">指定的楼层数据</param>
    private void ProcessCreatorInterfaceValidInputWithFloor(string validText, Msg_Floor targetFloor)
    {
        if (targetFloor != null)
        {
            // 记录当前楼层ID，确保修改后不会切换楼层
            long currentFloorId = targetFloor.Floor;

            // 【关键】优先使用缓存的完整配置，如果缓存不存在则使用floorList中的数据
            Msg_Floor completeFloorData = null;
            if (floorConfigCache.ContainsKey(currentFloorId))
            {
                completeFloorData = floorConfigCache[currentFloorId].Clone();
                GF.LogInfo_wl($"[使用缓存配置] 从缓存中获取楼层{currentFloorId}的完整配置");
            }
            else
            {
                // 如果缓存中没有，从floorList获取
                var floorFromList = floorList.FirstOrDefault(f => f.Floor == currentFloorId);
                if (floorFromList != null)
                {
                    completeFloorData = floorFromList.Clone();
                    GF.LogWarning($"[使用floorList配置] 缓存中没有楼层{currentFloorId}，使用floorList中的数据");
                }
            }

            if (completeFloorData == null)
            {
                GF.LogError($"无法获取楼层{currentFloorId}的配置数据");
                return;
            }

            // 只修改DeskName
            completeFloorData.Config.DeskName = validText;

            // 发送楼层更新请求到服务器
            SendFloorUpdateRequest(completeFloorData);
            // 更新UI显示
            UpdateFloorUIDisplay(targetFloor);
            // 确保当前楼层选中状态保持不变
            chooseFloor = currentFloorId;
        }
    }

    /// <summary>
    /// 从桌子信息中提取并缓存楼层的完整配置
    /// </summary>
    /// <param name="clubId">俱乐部ID</param>
    /// <param name="floorId">楼层ID</param>
    /// <param name="deskInfos">桌子信息列表</param>
    private void CacheFloorConfigFromDesks(long clubId, long floorId, List<Msg_MJDeskInfo> deskInfos)
    {
        if (deskInfos == null || deskInfos.Count == 0)
        {
            return;
        }
        if (floorId == 0)
        {
            return;

        }
        // 获取第一个桌子的信息（所有桌子配置都相同）
        var firstDesk = deskInfos.FirstOrDefault()?.DeskInfo;
        if (firstDesk == null)
        {
            GF.LogWarning($"楼层{floorId}没有桌子信息，无法提取配置");
            return;
        }

        // 从桌子信息构建完整的楼层配置
        Msg_Floor floorConfig = new Msg_Floor
        {
            Floor = floorId,
            MethodType = firstDesk.MethodType,
            Config = firstDesk.BaseConfig?.Clone() ?? new Desk_BaseConfig()
        };

        // 根据玩法类型复制对应的配置
        if (firstDesk.MethodType == NetMsg.MethodType.RunFast && firstDesk.RunFastConfig != null)
        {
            floorConfig.RunFastConfig = firstDesk.RunFastConfig.Clone();
           // GF.LogInfo_wl($"[缓存楼层配置] 楼层{floorId} RunFastConfig: FirstRun={floorConfig.RunFastConfig.FirstRun}, Play={floorConfig.RunFastConfig.Play}, Check=[{string.Join(",", floorConfig.RunFastConfig.Check)}], DismissTimes={floorConfig.RunFastConfig.DismissTimes}, ApplyDismissTime={floorConfig.RunFastConfig.ApplyDismissTime}, WithMin={floorConfig.RunFastConfig.WithMin}");
        }
        else if (firstDesk.MjConfig != null)
        {
            floorConfig.MjConfig = firstDesk.MjConfig.Clone();
           // GF.LogInfo_wl($"[缓存楼层配置] 楼层{floorId} MjConfig: {floorConfig.MjConfig}");
        }

        // 保存到缓存
        floorConfigCache[floorId] = floorConfig;
       GF.LogInfo_wl($"[缓存楼层配置] 成功缓存楼层{floorId}的完整配置");
    }

    private Msg_Floor GetCurrentEditingFloor()
    {
        try
        {
            // 获取当前活动的输入框组件
            InputField currentInputField = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.GetComponent<InputField>();
            if (currentInputField == null) return null;
            // 通过输入框向上查找到楼层项的Transform
            int floorNum = int.Parse(currentInputField.transform.parent?.parent.name); // QhQYQAccount -> InputQyqName -> FloorItem
            // 在楼层列表中查找对应的楼层
            return floorList.FirstOrDefault(f => f.Floor == floorNum);
            //根据对应的楼层号找到对应的楼层数据
        }
        catch (System.Exception ex)
        {
            GF.LogInfo_wl($"获取当前编辑楼层时发生错误: {ex.Message}");
            //返回当前正进入的楼层
            return GetCurrentFloor();
        }
    }
    /// <summary>
    /// 发送楼层更新名字请求到服务器
    /// </summary>
    /// <param name="floorData">要更新的楼层数据</param>
    private void SendFloorUpdateRequest(Msg_Floor floorData)
    {
        GF.LogInfo_wl("发送楼层配置更新请求： " + floorData.ToString());
        // 发送楼层配置更新请求
        Msg_SetMjFloorRq req = MessagePool.Instance.Fetch<Msg_SetMjFloorRq>();
        req.ClubId = GetCorrectClubId();
        req.Floor = floorData.Floor;
        req.MethodType = floorData.MethodType;
        // 使用 Clone 确保数据的完整性，并避免引用冲突
        if (floorData.Config != null)
        {
            req.Config = floorData.Config.Clone();
        }
        else
        {
            req.Config = new Desk_BaseConfig();
        }
        // 确保 DeskName 是最新的
        req.Config.DeskName = floorData.Config?.DeskName ?? "";
        GF.LogInfo_wl("设置楼层请求Config： " + req.Config.ToString());
        if (floorData.MethodType == NetMsg.MethodType.RunFast)
        {
            if (floorData.RunFastConfig != null)
            {
                req.RunFastConfig = floorData.RunFastConfig.Clone();
                GF.LogInfo_wl("设置楼层请求RunFastConfig: " + req.RunFastConfig.ToString());
            }
        }
        else
        {
            if (floorData.MjConfig != null)
            {
                req.MjConfig = floorData.MjConfig.Clone();
                GF.LogInfo_wl("设置楼层请求MjConfig: " + req.MjConfig.ToString());
            }
        }
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(
            HotfixNetworkComponent.AccountClientName,
            MessageID.Msg_SetMjFloorRq,
            req
        );
        //Util.GetInstance().ShowWaiting("正在更新楼层配置...","SetMjFloor");
    }
    /// <summary>
    /// 更新楼层UI显示
    /// </summary>
    /// <param name="floorData">更新的楼层数据</param>
    private void UpdateFloorUIDisplay(Msg_Floor floorData)
    {
        // 查找对应的楼层UI项
        Transform floorUI = varCreateFloorConter.transform.Find(floorData.Floor.ToString());
        if (floorUI != null)
        {
            // 更新楼层名称显示
            Text floorNameText = floorUI.Find("CreatRoomName")?.GetComponent<Text>();
            floorNameText.text = GetFloorGameMethod(floorData) + floorData.Config.PlayerTime + "局";
            // 更新placeholder文本
            Text placeholderText = floorUI.Find("InputQyqName/QhQYQAccount/Placeholder")?.GetComponent<Text>();
            placeholderText.text = floorData.Config.DeskName;
        }
    }
    /// <summary>
    /// 将MJMethod枚举转换为对应的显示名称
    /// </summary>
    /// <param name="mjMethod">麻将玩法枚举</param>
    /// <returns>玩法显示名称</returns>
    private string GetMJMethodDisplayName(NetMsg.MJMethod mjMethod)
    {
        switch (mjMethod)
        {
            case NetMsg.MJMethod.Dazhong:
                return "大众麻将";
            case NetMsg.MJMethod.Huanghuang:
                return "仙桃晃晃";
            case NetMsg.MJMethod.Kwx:
                return "卡五星";
            case NetMsg.MJMethod.Xl:
                return "血流成河";
            default:
                return mjMethod.ToString(); // 未知类型时返回枚举名称
        }
    }

    /// <summary>
    /// 从楼层数据获取游戏方法的显示名称
    /// 根据 MethodType 判断是麻将还是跑得快，并返回对应的显示名称
    /// </summary>
    /// <param name="floor">楼层数据</param>
    /// <returns>游戏方法的显示名称</returns>
    private string GetFloorGameMethod(Msg_Floor floor)
    {
        if (floor == null)
        {
            return "未知游戏";
        }

        switch (floor.MethodType)
        {
            case NetMsg.MethodType.MjSimple:
                // 麻将游戏，使用 MjConfig
                if (floor.MjConfig != null)
                {
                    return GetMJMethodDisplayName(floor.MjConfig.MjMethod);
                }
                else
                {
                    return "麻将游戏";
                }

            case NetMsg.MethodType.RunFast:
                // 跑得快游戏
                return "跑的快";

            default:
                return floor.MethodType.ToString();
        }
    }

    /// <summary>
    /// 处理有效输入 - 使用指定的楼层数据
    /// </summary>
    /// <param name="validText">有效的输入文本</param>
    /// <param name="targetFloor">指定的楼层数据</param>
    private void ProcessValidInputWithFloor(string validText, Msg_Floor targetFloor)
    {
        if (targetFloor != null)
        {
            // 更新楼层配置中的DeskName
            targetFloor.Config.DeskName = validText;
            // 发送楼层更新请求到服务器
            SendFloorUpdateRequest(targetFloor);
            // 更新本地UI显示
            UpdateFloorUIDisplay(targetFloor);
        }
    }

    /// <summary>
    /// 获取楼层配置信息（用于查看按钮显示）
    /// </summary>
    private string GetFloorConfigInfo(Msg_Floor floorData)
    {
        if (floorData == null) return "楼层配置为空";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        // 根据玩法类型显示特定配置
        switch (floorData.MethodType)
        {
            case NetMsg.MethodType.MjSimple:
                var mjConfig = floorData.MjConfig;
                if (mjConfig != null)
                {
                    string mjName = GetMJMethodDisplayName(mjConfig.MjMethod);
                    varRoomInfo.transform.Find("Title").GetComponent<Text>().text = mjName;
                    // 使用工厂方法构建规则描述
                    var baseInfo = new Desk_BaseInfo
                    {
                        BaseConfig = floorData.Config,
                        MjConfig = mjConfig,
                        MethodType = floorData.MethodType
                    };
                    sb.Append(MahjongRuleFactory.BuildRoomDescription(baseInfo));
                }
                break;
            case NetMsg.MethodType.RunFast:
                if (floorData.RunFastConfig != null)
                {
                    varRoomInfo.transform.Find("Title").GetComponent<Text>().text = "跑的快";
                    sb.Append($"{floorData.Config.PlayerNum}人{floorData.Config.PlayerTime}局 ");
                    sb.Append($"底分：{floorData.Config.BaseCoin} ");
                    sb.Append(GetRunFastConfigInfo(floorData.RunFastConfig));
                }
                break;
        }

        return sb.ToString();
    }


    /// <summary>
    /// 获取跑得快配置信息
    /// </summary>
    private string GetRunFastConfigInfo(NetMsg.RunFastConfig config)
    {
        if (config == null) return "";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        // 首局出牌规则
        switch (config.FirstRun)
        {
            case 0: sb.Append("首局最小牌先出 "); break;
            case 1: sb.Append("每局最小牌先出 "); break;
            case 2: sb.Append("首局黑桃三先出 "); break;
        }
        // 出牌玩法
        switch (config.Play)
        {
            case 1: sb.Append("有大必压 "); break;
            case 2: sb.Append("可不压 "); break;
        }
        // 必带最小
        switch (config.WithMin)
        {
            case 1: sb.Append("必带最小 "); break;
            case 2: sb.Append("任意出牌 "); break;
        }
        // 玩法选择
        if (config.Check != null && config.Check.Count > 0)
        {
            foreach (var check in config.Check)
            {
                switch (check)
                {
                    case 1: sb.Append("4带2 "); break;
                    case 2: sb.Append("4带3 "); break;
                    case 3: sb.Append("炸弹不能拆 "); break;
                    case 4: sb.Append("AAA为炸弹 "); break;
                    case 5: sb.Append("红桃10扎鸟 "); break;
                    case 6: sb.Append("炸弹不翻倍 "); break;
                    case 7: sb.Append("炸弹被压无分 "); break;
                    case 8: sb.Append("三带任意出 "); break;
                    case 9: sb.Append("最后三张可少带接完 "); break;
                    case 10: sb.Append("抢庄 "); break;
                    case 11: sb.Append("可反的 "); break;
                    case 12: sb.Append("快速过牌 "); break;
                    case 13: sb.Append("切牌 "); break;
                    case 14: sb.Append("显示剩牌 "); break;
                    case 15: sb.Append("记牌器道具 "); break;
                    case 16: sb.Append("加倍 "); break;
                    case 17: sb.Append("提前亮牌 "); break;
                }
            }
        }

        return sb.ToString();
    }


    #endregion
}



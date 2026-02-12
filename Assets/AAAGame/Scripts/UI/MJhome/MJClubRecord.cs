using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Google.Protobuf;
using NetMsg;
using Tacticsoft;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class MJClubRecord : UIFormBase
{
    #region 常量定义
    private const int MAX_FLOOR = 20;                    // 最大楼层数
    private const int MIN_HOUR = 0;                      // 最小小时
    private const int MAX_HOUR = 24;                     // 最大小时
    private const int HOURS_PER_DAY = 24;                // 每天小时数
    private const long MILLISECOND_THRESHOLD = 9999999999; // 毫秒时间戳阈值
    #endregion

    #region 字段定义
    // 时段筛选相关 - 4个界面独立存储
    private int myRecordStartTime = MIN_HOUR;       // 我的战绩开始小时
    private int myRecordEndTime = MAX_HOUR;         // 我的战绩结束小时
    private int memberRecordStartTime = MIN_HOUR;   // 成员战绩开始小时
    private int memberRecordEndTime = MAX_HOUR;     // 成员战绩结束小时
    private int circleRecordStartTime = MIN_HOUR;   // 圈子战绩开始小时
    private int circleRecordEndTime = MAX_HOUR;     // 圈子战绩结束小时
    private int teamStatsStartTime = MIN_HOUR;      // 团队统计开始小时
    private int teamStatsEndTime = MAX_HOUR;        // 团队统计结束小时

    private string customTime;                      // 自定义时间字符串（临时变量）

    // 日期筛选相关
    private Toggle choseToggle;                      // 当前选中的日期Toggle
    private DateTime selectedDate = DateTime.Today;  // 当前选择的具体日期
    // 楼层筛选相关
    private int chooseFloor = 0;    // 当前选择的楼层(0表示所有楼层)
    // 缓存信息
    private long myPlayerId;        // 缓存我的玩家ID
    private int hour = 0;           // 时间调整临时变量
    private int lastChooseIndex = 0;    // 上次选择的索引
    private int currentMethodType = 0;  // 当前查看的记录的玩法类型

    private GameObject currentPanel;    // 当前显示的面板
    private GameObject lastMJMyRecordRequestPanel;   // 最后一次请求我的战绩时的面板
    private GameObject lastMJTeamRecordRequestPanel; // 最后一次请求团队战绩时的面板
    private System.Action currentRequestAction; // 当前正在查看的请求动作

    // 团队战绩数据缓存（用于成员战绩和团队统计页面的搜索）
    private List<NetMsg.MJTeamRecord> teamRecordDataCache = new List<NetMsg.MJTeamRecord>();

    // // 所有成员的原始总计数据（用于底部总计行，不受筛选影响）
    // private float allMembersTotalScore = 0f;
    // private int allMembersTotalRounds = 0;
    // private int allMembersTotalLowRounds = 0;
    // private int allMembersTotalBigWinners = 0;

    // 战绩数据缓存（用于搜索和筛选）
    private List<NetMsg.MJMyRecord> recordDataCache = new List<NetMsg.MJMyRecord>();
    // TableView 数据缓存
    private List<MJMyRecord> recordDataCacheForTableView = new List<MJMyRecord>();
    private List<MJTeamRecord> teamRecordDataCacheForTableView = new List<MJTeamRecord>();
    // 当前查看的战绩详情数据缓存（用于复制功能）
    private Msg_MJMYRecordDetailRs currentRecordDetail = null;
    // 当前查看的成员玩家ID（会长查看成员战绩详情时使用）
    private long currentViewedPlayerId = 0;
    #endregion
    [Header("TableView组件")]
    public TableView MyRqecordScroll;
    public TableView MemberRecordScroll;
    public TableView CircleRecordScroll;
    public TableView TeamStatsScroll;
    [Header("===== 交互按钮 =====")]
    public GameObject varMyRqecordBtn;//我的战绩按钮
    public GameObject varMemberRecordBtn;//成员战绩按钮
    public GameObject varCircleRecordBtn;//圈子战绩按钮
    public GameObject varTeamStatsBtn;//团队统计按钮

    [Header("===== 交互界面 =====")]
    public GameObject varChooseTime;               // 选择时间弹窗
    public GameObject varCustomTime;               // 自定义时间弹窗
    public GameObject varMyRqecord;                // 我的战绩界面
    public GameObject varMemberRecord;            // 成员战绩界面
    public GameObject varCircleRecord;            // 圈子战绩界面
    public GameObject varTeamStats;                // 团队统计界面
    public GameObject MemberRecordDetails;          // 成员战绩详情界面
    public InputField FindInput;               // 查找输入框  可以根据玩家ID和房号查找
    public GameObject duiJuXQ;          // 对局详情按钮
    public GameObject daYingJia;          // 大赢家按钮
    public GameObject varRecordDetails;          // 战绩详情弹窗界面

    // 按钮下的选中状态指示器
    private GameObject duijuXQ_YES;         // 对局详情选中状态
    private GameObject dayingjia_YES;       // 大赢家选中状态

    // 成员战绩详情筛选状态
    private bool filterBigWinnerOnly = false;  // 是否只显示大赢家记录

    [Header("===== 容器 =====")]
    public GameObject varAllIntegralItemConent;             // 总积分列表容器
    public GameObject varSingleOfficeRecordContent;         // 单局战绩列表容器
    public GameObject MyRqecordDateConent;          // 我的战绩日期内容容器
    public GameObject CircleRecordDateConent;          // 圈子战绩日期内容容器
    public GameObject MemberRecorddateConent;          // 成员战绩日期内容容器
    public GameObject varPlayerRqecordConent;        // 玩家战绩列表容器
    public GameObject MemberRecordPlayerRqecordConent;       // 成员战绩玩家战绩列表容器
    public GameObject MemberRecordDetailsConent;       // 成员战绩详情列表容器
    [Header("===== 单条 =====")]
    public GameObject varAllIntegralItem;                // 总积分单条
    public GameObject varSingleOfficeRecordItem;        // 单局战绩单条
    public GameObject MyRqecordDateConentItem;              // 我的战绩日期内容单条
    public GameObject CircleRecordDateConentItem;              // 圈子战绩日期内容单条
    public GameObject MemberRecorddateConentItem;              // 成员战绩日期内容单条
    public GameObject varRqecordItem;                // 战绩列表单条
    public GameObject varChooseFloorContentItem;     // 选择楼层内容单条
    public GameObject varPlayerRqecordItem;      // 玩家战绩列表单条
    public GameObject varSingleOfficeItem;      // 单局战绩单条
    public GameObject MemberRecordPlayerRqecorditem;      // 成员战绩玩家战绩单条
    private Text ChooseFloorText; // 楼层选择文本

    #region 排序枚举
    /// <summary>
    /// 排序类型
    /// </summary>
    private enum SortType
    {
        ScoreDescending,       // 战绩排序大到小
        ScoreAscending,        // 战绩排序小到大
        RoundDescending,       // 牌局排序大到小
        RoundAscending,        // 牌局排序小到大
        LowScoreDescending,    // 低分排序大到小
        LowScoreAscending,     // 低分排序小到大
        BigWinnerDescending,   // 大赢家排序大到小
        BigWinnerAscending     // 大赢家排序小到大
    }
    #endregion

    #region 辅助方法
    /// <summary>
    /// 将数字转换为中文数字（1-20）
    /// </summary>
    private string NumberToChinese(int number)
    {
        if (number == 0) return "所有楼层";

        string[] chineseNumbers = { "", "一", "二", "三", "四", "五", "六", "七", "八", "九", "十",
                                     "十一", "十二", "十三", "十四", "十五", "十六", "十七", "十八", "十九", "二十" };

        if (number > 0 && number <= 20)
        {
            return chineseNumbers[number] + "楼";
        }

        return $"{number}楼";
    }
    #endregion

    #region 生命周期方法
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        // 缓存玩家ID，避免重复调用
        myPlayerId = Util.GetMyselfInfo().PlayerId;
        // 默认请求动作
        currentRequestAction = RequestMyRecord;
        // 注册网络消息监听
        RegisterNetworkListeners();
        // 初始化 TableView
        InitializeTableViews();
        // 初始化UI和监听器
        InitializeUI();
        InitializeDateToggles();
        InitToggle();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        // 清空数据缓存
        recordDataCacheForTableView.Clear();
        recordDataCache.Clear();
        // 移除网络消息监听
        UnregisterNetworkListeners();
    }
    #endregion
    #region 初始化方法
    /// <summary>
    /// 初始化UI元素
    /// </summary>
    private void InitializeUI()
    {
        var today = DateTime.Now;
        // 设置日期文本显示
        SetDateText("4", today.AddDays(-4));
        SetDateText("5", today.AddDays(-5));
        SetDateText("6", today.AddDays(-6));
        SetDateText("7", today.AddDays(-7));
        // 隐藏弹窗
        varChooseTime.SetActive(false);
        varCustomTime.SetActive(false);
        varRecordDetails.SetActive(false);
        // 根据权限显示管理按钮 (仅限创建者)
        var leagueInfo = GlobalManager.GetInstance().LeagueInfo;
        bool isCreator = Util.IsMySelf(leagueInfo.Creator);
        bool hasPermission = isCreator;
        varMemberRecordBtn.SetActive(hasPermission);
        varCircleRecordBtn.SetActive(hasPermission);
        varTeamStatsBtn.SetActive(hasPermission);

        // 重要：必须先初始化按钮监听器，再触发Toggle选中状态
        // 否则Toggle触发时监听器还没注册，不会执行请求
        InitializeButtonListeners();
        // 初始化楼层选择列表
        InitializeFloorList();
        // 初始化成员战绩详情按钮
        InitializeMemberRecordDetailButtons();

        // 默认显示我的战绩界面，并触发选中状态（监听器已注册，会自动执行请求）
        var myRecordToggle = varMyRqecordBtn.GetComponent<Toggle>();
        if (myRecordToggle != null)
        {
            myRecordToggle.isOn = false; // 确保触发 onValueChanged
            myRecordToggle.isOn = true;  // 这会触发 RegisterRecordToggle 中注册的监听器，自动执行 RequestMyRecord()
        }
        else
        {
            // 如果没有Toggle组件，手动执行初始化
            ShowRecordPanel(varMyRqecord);
            UpdateActivePanelContext(varMyRqecord);
            RequestMyRecord();
        }
    }

    /// <summary>
    /// 初始化按钮监听器
    /// </summary>
    private void InitializeButtonListeners()
    {
        // 注册我的战绩按钮
        RegisterRecordToggle(varMyRqecordBtn, varMyRqecord, "我的战绩", RequestMyRecord);
        // 注册成员战绩按钮（管理员可见）
        RegisterRecordToggle(varMemberRecordBtn, varMemberRecord, "成员战绩", RequestMemberRecord, requireActive: true);
        // 注册圈子战绩按钮（管理员可见）
        RegisterRecordToggle(varCircleRecordBtn, varCircleRecord, "圈子战绩", RequestCircleRecord, requireActive: true);
        // 注册团队统计按钮（临时隐藏）
        // RegisterRecordToggle(varTeamStatsBtn, varTeamStats, "团队统计", RequestTeamStats, false);
        varTeamStatsBtn?.SetActive(false);
    }

    /// <summary>
    /// 初始化成员战绩详情筛选按钮
    /// </summary>
    private void InitializeMemberRecordDetailButtons()
    {
        // 获取按钮下的子对象引用
        if (duiJuXQ != null)
        {
            var yesObj = duiJuXQ.transform.Find("duijuXQ_YES");
            if (yesObj != null)
            {
                duijuXQ_YES = yesObj.gameObject;
            }
        }
        if (daYingJia != null)
        {
            var yesObj = daYingJia.transform.Find("dayingjia_YES");
            if (yesObj != null)
            {
                dayingjia_YES = yesObj.gameObject;
            }
        }
        // 设置默认状态：显示对局详情（所有记录）
        filterBigWinnerOnly = false;
        if (duijuXQ_YES != null) duijuXQ_YES.SetActive(true);
        if (dayingjia_YES != null) dayingjia_YES.SetActive(false);
    }

    /// <summary>
    /// 注册战绩类型Toggle按钮的统一方法
    /// </summary>
    /// <param name="toggleObject">Toggle游戏对象</param>
    /// <param name="panelObject">对应的界面面板</param>
    /// <param name="buttonName">按钮名称（用于日志）</param>
    /// <param name="requestAction">请求数据的方法</param>
    /// <param name="requireActive">是否需要检查activeSelf</param>
    private void RegisterRecordToggle(GameObject toggleObject, GameObject panelObject, string buttonName, System.Action requestAction, bool requireActive = false)
    {
        // 检查对象是否存在
        if (toggleObject == null) return;
        if (requireActive && !toggleObject.activeSelf) return;
        var toggle = toggleObject.GetComponent<Toggle>();
        toggle.onValueChanged.AddListener((isOn) =>
        {
            if (!isOn) return;
            // 隐藏当前面板的楼层选择弹窗
            var currentChooseFloor = GetCurrentChooseFloor();
            if (currentChooseFloor != null)
            {
                currentChooseFloor.SetActive(false);
            }
            // 切换面板显示
            ShowRecordPanel(panelObject);
            // 实时更新面板上下文（楼层文本、列表容器等）
            UpdateActivePanelContext(panelObject);
            // 重置楼层选择
            chooseFloor = 0;
            if (ChooseFloorText != null) ChooseFloorText.text = "所有楼层";
            // 更新当前请求动作
            currentRequestAction = requestAction;
            // 更新所有按钮的选中状态指示器
            UpdateToggleIndicators(toggleObject);
            // 执行数据请求
            requestAction?.Invoke();
        });
    }
    /// <summary>
    /// 切换显示的战绩面板
    /// </summary>
    private void ShowRecordPanel(GameObject activePanel)
    {
        currentPanel = activePanel;
        if (varMyRqecord != null) varMyRqecord.SetActive(varMyRqecord == activePanel);
        if (varMemberRecord != null) varMemberRecord.SetActive(varMemberRecord == activePanel);
        if (varCircleRecord != null) varCircleRecord.SetActive(varCircleRecord == activePanel);
        if (varTeamStats != null) varTeamStats.SetActive(varTeamStats == activePanel);
        // 切换面板时将对应的TableView滚动到顶部
        ResetScrollToTop(activePanel);
    }

    /// <summary>
    /// 将指定面板的TableView滚动到顶部
    /// </summary>
    private void ResetScrollToTop(GameObject panel)
    {
        if (panel == null) return;
        if (panel == varMyRqecord && MyRqecordScroll != null) MyRqecordScroll.ScrollY = 0;
        else if (panel == varMemberRecord && MemberRecordScroll != null) MemberRecordScroll.ScrollY = 0;
        else if (panel == varCircleRecord && CircleRecordScroll != null) CircleRecordScroll.ScrollY = 0;
        else if (panel == varTeamStats && TeamStatsScroll != null) TeamStatsScroll.ScrollY = 0;
    }
    /// <summary>
    /// 根据当前面板更新上下文引用（楼层文本、列表容器等）
    /// </summary>
    private void UpdateActivePanelContext(GameObject panel)
    {
        if (panel == null) return;
        // 1. 在切换引用前，先清理当前可能正在运行的加载逻辑和旧数据
        ClearAllRecords();
        // 2. 更新楼层文本引用（从 AllFloor 下查找）
        var allFloorTrans = panel.transform.Find("AllFloor");
        var textTrans = allFloorTrans.Find("ChooseFloorText");
        ChooseFloorText = textTrans.GetComponent<Text>();
        ScrollRect scrollRect = panel.GetComponentInChildren<ScrollRect>(true);
        if (scrollRect != null && scrollRect.content != null) varPlayerRqecordConent = scrollRect.content.gameObject;
        if (varPlayerRqecordConent != null) ClearAllRecords();
    }
    /// <summary>
    /// 更新所有Toggle按钮的选中状态指示器
    /// </summary>
    /// <param name="selectedToggle">当前选中的Toggle对象</param>
    private void UpdateToggleIndicators(GameObject selectedToggle)
    {
        // 定义所有战绩Toggle按钮
        var allToggles = new[]
        {
            varMyRqecordBtn,
            varMemberRecordBtn,
            varCircleRecordBtn,
            varTeamStatsBtn
        };
        // 更新每个按钮的指示器状态
        foreach (var toggleObj in allToggles)
        {
            if (toggleObj == null) continue;
            var indicator = toggleObj.transform.Find("ison");
            if (indicator != null)
            {
                indicator.gameObject.SetActive(toggleObj == selectedToggle);
            }
        }
    }
    /// <summary>
    /// 设置日期文本显示
    /// </summary>
    private void SetDateText(string itemName, DateTime date)
    {
        string dateStr = $"{date.Month}-{date.Day}";
        if (MyRqecordDateConent != null)
        {
            var text = MyRqecordDateConent.transform.Find($"{itemName}/timeText")?.GetComponent<Text>();
            if (text != null) text.text = dateStr;
        }
        // 更新圈子战绩日期文本
        if (CircleRecordDateConent != null)
        {
            var text = CircleRecordDateConent.transform.Find($"{itemName}/timeText")?.GetComponent<Text>();
            if (text != null) text.text = dateStr;
        }
        // 更新成员战绩日期文本
        if (MemberRecorddateConent != null)
        {
            var text = MemberRecorddateConent.transform.Find($"{itemName}/timeText")?.GetComponent<Text>();
            if (text != null) text.text = dateStr;
        }
    }
    /// <summary>
    /// 初始化楼层选择列表
    /// </summary>
    private void InitializeFloorList()
    {
        // 为每个面板初始化楼层列表
        InitializeFloorListForPanel(varMyRqecord);
        InitializeFloorListForPanel(varMemberRecord);
        InitializeFloorListForPanel(varCircleRecord);
        InitializeFloorListForPanel(varTeamStats);
    }

    /// <summary>
    /// 为指定面板初始化楼层选择列表
    /// </summary>
    private void InitializeFloorListForPanel(GameObject panel)
    {
        if (panel == null) return;

        var allFloorTrans = panel.transform.Find("AllFloor");
        if (allFloorTrans == null) return;

        var chooseFloorTrans = allFloorTrans.Find("ChooseFloor");
        if (chooseFloorTrans == null) return;

        var chooseFloorContent = chooseFloorTrans.Find("Viewport/ChooseFloorContent");
        if (chooseFloorContent == null) return;

        // 清空现有楼层项
        ClearContent(chooseFloorContent.gameObject);

        // 创建楼层选项（0-20楼）
        for (int i = 0; i <= MAX_FLOOR; i++)
        {
            CreateFloorItem(chooseFloorContent, i);
        }
    }

    /// <summary>
    /// 创建单个楼层选项
    /// </summary>
    private void CreateFloorItem(Transform parent, int floor)
    {
        var item = Instantiate(varChooseFloorContentItem, parent);
        item.name = floor.ToString();
        var text = item.GetComponent<Text>();
        text.text = NumberToChinese(floor);
        // 添加点击事件（捕获当前楼层值避免闭包问题）
        int currentFloor = floor;
        var button = item.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnFloorSelected(currentFloor));
        }
    }
    /// <summary>
    /// 初始化日期Toggle监听器
    /// </summary>
    private void InitializeDateToggles()
    {
        // 初始化我的战绩日期
        SetupDateToggles(MyRqecordDateConent, MyRqecordDateConentItem);
        // 初始化圈子战绩日期
        SetupDateToggles(CircleRecordDateConent, CircleRecordDateConentItem);
        // 初始化成员战绩日期
        SetupDateToggles(MemberRecorddateConent, MemberRecorddateConentItem);
    }

    /// <summary>
    /// 配置日期Toggle组
    /// </summary>
    private void SetupDateToggles(GameObject container, GameObject contentItem)
    {
        if (container == null || contentItem == null) return;

        foreach (Transform item in container.transform)
        {
            // 跳过内容面板本身
            if (item.gameObject == contentItem) continue;

            var toggle = item.GetComponent<Toggle>();
            if (toggle == null) continue;

            var currentItem = item;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener((isOn) =>
            {
                if (!isOn) return;

                // 记录当前选中的 Toggle
                choseToggle = toggle;

                // 根据Toggle名称更新选中的日期
                UpdateSelectedDate(currentItem.name);

                // 将内容面板移动到当前选中的日期下方
                if (contentItem.transform.parent != container.transform)
                {
                    contentItem.transform.SetParent(container.transform, false);
                }

                var clickedIndex = currentItem.GetSiblingIndex();
                var panelIndex = contentItem.transform.GetSiblingIndex();
                var targetIndex = clickedIndex + (panelIndex > clickedIndex ? 1 : 0);

                // 确保索引在有效范围内
                var maxIndex = container.transform.childCount - 1;
                targetIndex = Mathf.Clamp(targetIndex, 0, maxIndex);

                contentItem.transform.SetSiblingIndex(targetIndex);
            });
        }
    }

    /// <summary>
    /// 添加请求回调
    /// </summary>
    private void RegisterNetworkListeners()
    {
        HotfixNetworkComponent.AddListener(MessageID.Msg_MJMYRecordRs, OnMJMYRecordResponse);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MJTeamRecordRs, OnMJTeamRecordResponse);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MJMYRecordDetailRs, OnMJMYRecordDetailResponse);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MJPlayBack, OnMsg_MJPlayBack);
        HotfixNetworkComponent.AddListener(MessageID.Msg_RunFastPlayBackRs, OnMsg_RunFastPlayBack);
    }

    /// <summary>
    /// 移除请求回调
    /// </summary>
    private void UnregisterNetworkListeners()
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MJMYRecordRs, OnMJMYRecordResponse);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MJTeamRecordRs, OnMJTeamRecordResponse);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MJMYRecordDetailRs, OnMJMYRecordDetailResponse);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MJPlayBack, OnMsg_MJPlayBack);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_RunFastPlayBackRs, OnMsg_RunFastPlayBack);
    }

    /// <summary>
    /// 楼层选择回调
    /// </summary>
    private void OnFloorSelected(int floor)
    {
        chooseFloor = floor;

        // 更新当前面板的楼层文本显示
        UpdateCurrentFloorText(floor);

        // 隐藏当前面板的楼层选择弹窗
        var currentChooseFloor = GetCurrentChooseFloor();
        if (currentChooseFloor != null)
        {
            currentChooseFloor.SetActive(false);
        }

        // 使用当前保存的请求动作进行刷新
        currentRequestAction?.Invoke();
    }

    /// <summary>
    /// 更新当前面板的楼层文本显示
    /// </summary>
    private void UpdateCurrentFloorText(int floor)
    {
        if (currentPanel == null) return;

        var allFloorTrans = currentPanel.transform.Find("AllFloor");
        if (allFloorTrans == null) return;

        var floorTextTrans = allFloorTrans.Find("ChooseFloorText");
        if (floorTextTrans == null) return;

        var floorText = floorTextTrans.GetComponent<Text>();
        if (floorText != null)
        {
            floorText.text = NumberToChinese(floor);
        }
    }

    /// <summary>
    /// 获取当前面板的楼层选择弹窗
    /// </summary>
    private GameObject GetCurrentChooseFloor()
    {
        if (currentPanel == null) return null;
        var allFloorTrans = currentPanel.transform.Find("AllFloor");
        if (allFloorTrans == null) return null;

        var chooseFloorTrans = allFloorTrans.Find("ChooseFloor");
        return chooseFloorTrans?.gameObject;
    }
    #endregion

    #region 请求和网络回调方法

    /// <summary>
    /// 请求我的战绩（根据当前选择的日期、时段筛选和楼层）
    /// </summary>
    private void RequestMyRecord()
    {
        // 查看自己的战绩时，重置当前查看的玩家ID
        currentViewedPlayerId = 0;
        lastMJMyRecordRequestPanel = varMyRqecord;
        // 更新楼层显示文本
        if (ChooseFloorText != null)
            ChooseFloorText.text = NumberToChinese(chooseFloor);

        // 构建请求
        Msg_MJMYRecordRq req = MessagePool.Instance.Fetch<Msg_MJMYRecordRq>();
        req.StartTime = ConvertHourToTimestamp(myRecordStartTime);  // 选中日期 + 开始小时
        req.EndTime = ConvertHourToTimestamp(myRecordEndTime);      // 选中日期 + 结束小时
        req.Floor = chooseFloor;
        req.UserId = (int)myPlayerId;
        req.ClubId = (int)GlobalManager.GetInstance().LeagueInfo.LeagueId;

        GF.LogInfo_wl($"请求我的战绩 - 日期: {selectedDate:yyyy-MM-dd}, 时段: {myRecordStartTime}:00-{myRecordEndTime}:00, 楼层: {chooseFloor}, 时间戳: {req.StartTime}-{req.EndTime}");

        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(
            HotfixNetworkComponent.AccountClientName,
            MessageID.Msg_MJMYRecordRq,
            req);
    }

    /// <summary>
    /// 请求成员战绩（管理员功能）
    /// </summary>
    private void RequestMemberRecord()
    {
        lastMJTeamRecordRequestPanel = varMemberRecord;
        // 更新楼层显示文本
        if (ChooseFloorText != null)
            ChooseFloorText.text = NumberToChinese(chooseFloor);

        // 构建请求
        Msg_MJTeamRecordRq req = MessagePool.Instance.Fetch<Msg_MJTeamRecordRq>();
        req.StartTime = ConvertHourToTimestampMs(memberRecordStartTime);
        req.EndTime = ConvertHourToTimestampMs(memberRecordEndTime);
        req.Floor = chooseFloor;
        req.ClubId = (int)GlobalManager.GetInstance().LeagueInfo.LeagueId;

        GF.LogInfo_wl($"请求成员战绩 - 日期: {selectedDate:yyyy-MM-dd}, 时段: {memberRecordStartTime}:00-{memberRecordEndTime}:00, 楼层: {chooseFloor}, 时间戳(毫秒): {req.StartTime}-{req.EndTime}");
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(
            HotfixNetworkComponent.AccountClientName,
            MessageID.Msg_MJTeamRecordRq,
            req);
    }

    /// <summary>
    /// 请求圈子战绩（管理员功能）
    /// </summary>
    private void RequestCircleRecord()
    {
        lastMJMyRecordRequestPanel = varCircleRecord;
        // 更新楼层显示文本
        if (ChooseFloorText != null)
            ChooseFloorText.text = NumberToChinese(chooseFloor);

        // 圈子战绩通常使用俱乐部战绩协议
        Msg_MJMYRecordRq req = MessagePool.Instance.Fetch<Msg_MJMYRecordRq>();
        req.StartTime = ConvertHourToTimestamp(circleRecordStartTime);
        req.EndTime = ConvertHourToTimestamp(circleRecordEndTime);
        req.Floor = chooseFloor;
        req.UserId = 0; // 圈子战绩不传UserId或传0
        req.ClubId = (int)GlobalManager.GetInstance().LeagueInfo.LeagueId;

        GF.LogInfo_wl($"请求圈子战绩 - 日期: {selectedDate:yyyy-MM-dd}, 时段: {circleRecordStartTime}:00-{circleRecordEndTime}:00, 楼层: {chooseFloor}");

        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(
            HotfixNetworkComponent.AccountClientName,
            MessageID.Msg_MJMYRecordRq,
            req);
    }

    /// <summary>
    /// 请求团队统计（管理员功能）
    /// </summary>
    private void RequestTeamStats()
    {
        lastMJTeamRecordRequestPanel = varTeamStats;
        // 更新楼层显示文本
        if (ChooseFloorText != null)
            ChooseFloorText.text = NumberToChinese(chooseFloor);

        // 团队统计通常也使用TeamRecord协议
        Msg_MJTeamRecordRq req = MessagePool.Instance.Fetch<Msg_MJTeamRecordRq>();
        req.StartTime = ConvertHourToTimestamp(teamStatsStartTime);
        req.EndTime = ConvertHourToTimestamp(teamStatsEndTime);
        req.Floor = chooseFloor;
        req.ClubId = (int)GlobalManager.GetInstance().LeagueInfo.LeagueId;

        GF.LogInfo_wl($"请求团队统计 - 日期: {selectedDate:yyyy-MM-dd}, 时段: {teamStatsStartTime}:00-{teamStatsEndTime}:00, 楼层: {chooseFloor}");

        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(
            HotfixNetworkComponent.AccountClientName,
            MessageID.Msg_MJTeamRecordRq,
            req);
    }

    /// <summary>
    /// 我的战绩详情响应处理
    /// </summary>
    private void OnMJMYRecordDetailResponse(MessageRecvData data)
    {
        Msg_MJMYRecordDetailRs ack = Msg_MJMYRecordDetailRs.Parser.ParseFrom(data.Data);
        GF.LogInfo_wl("收到我的战绩详情返回" + ack);
        // 数据验证
        if (!ValidateRecordDetail(ack))
        {
            ClearRecordDetails();
            return;
        }
        // 缓存当前战绩详情数据（用于复制功能）
        currentRecordDetail = ack;
        varRecordDetails.SetActive(true);
        // 清空并重新加载内容
        ClearContent(varAllIntegralItemConent);
        ClearContent(varSingleOfficeRecordContent);
        // 加载玩家头像和总分
        LoadPlayerInfo(ack);
        // 加载回合详情
        LoadRoundDetails(ack);
    }

    /// <summary>
    /// 验证战绩详情数据
    /// </summary>
    private bool ValidateRecordDetail(Msg_MJMYRecordDetailRs ack)
    {
        if (ack == null) return false;
        if (ack.BasePlayer == null || ack.BasePlayer.Count == 0) return false;
        return true;
    }

    /// <summary>
    /// 加载玩家信息
    /// </summary>
    private void LoadPlayerInfo(Msg_MJMYRecordDetailRs ack)
    {
        // 【深度修复】创建数据深拷贝并同步排序
        var playerScorePairs = new List<(NetMsg.BasePlayer player, double score)>();
        
        foreach (var player in ack.BasePlayer)
        {
            double score = 0;
            // 从Score列表中查找对应玩家的分数（按PlayerId匹配）
            var scoreItem = ack.Score.FirstOrDefault(s => s.Key == player.PlayerId);
            if (scoreItem != null)
            {
                score = scoreItem.Val;
            }
            playerScorePairs.Add((player, score));
        }
        
        // 按玩家Id排序
        var sortedPairs = playerScorePairs.OrderBy(p => p.player.PlayerId).ToList();
        
        for (int i = 0; i < sortedPairs.Count; i++)
        {
            var pair = sortedPairs[i];
            var go = Instantiate(varAllIntegralItem, varAllIntegralItemConent.transform);
            go.name = i.ToString();
            
            // 设置玩家名称
            var nameText = go.transform.Find("Name")?.GetComponent<Text>();
            nameText.FormatNickname(pair.player.Nick);
            
            // 下载并设置头像
            var headImage = go.transform.Find("head")?.GetComponent<RawImage>();
            if (headImage != null)
                Util.DownloadHeadImage(headImage, pair.player.HeadImage);
            
            // 设置玩家ID
            var idText = go.transform.Find("Id")?.GetComponent<Text>();
            idText.text = $"ID:{pair.player.PlayerId}";
            
            // 设置积分
            var integralText = go.transform.Find("Integral")?.GetComponent<Text>();
            if (integralText != null)
            {
                integralText.FormatRichText((float)pair.score);
            }
        }
    }

    /// <summary>
    /// 加载回合详情
    /// </summary>
    private void LoadRoundDetails(Msg_MJMYRecordDetailRs ack)
    {
        int roundCount = ack.RoundDetail?.Count ?? 0;

        // 倒序遍历：从最后一局开始显示（因为服务器数据本身是倒序的）
        for (int i = roundCount - 1; i >= 0; i--)
        {
            int index = i;
            var go = Instantiate(varSingleOfficeRecordItem, varSingleOfficeRecordContent.transform);
            go.name = index.ToString();
            // 设置回合总分
            SetRoundScore(go, ack, index);
            // 设置局数：显示正确的局号（从第1局到第N局）
            var num = go.transform.Find("DuiJuNum")?.GetComponent<Text>();
            num.text = $"第{roundCount - i}局";
            // 加载玩家回合信息
            LoadRoundPlayerInfo(go, ack, index);
            // 添加回放按钮事件
            AddReplayButtonListener(go, ack, index);
        }
    }

    /// <summary>
    /// 设置回合分数
    /// </summary>
    private void SetRoundScore(GameObject go, Msg_MJMYRecordDetailRs ack, int index)
    {
        var integralText = go.transform.Find("Integral")?.GetComponent<Text>();
        if (integralText != null)
        {
            // 【修复】 index 是局数索引，应从 RoundDetail 中获取对应玩家的局分数，而不是从 ack.Score (玩家总分列表) 中拿
            long targetId = currentViewedPlayerId > 0 ? currentViewedPlayerId : myPlayerId;
            float roundScore = 0f;
            if (ack.RoundDetail != null && index < ack.RoundDetail.Count)
            {
                var round = ack.RoundDetail[index];
                var scoreItem = round.Score.FirstOrDefault(s => s.Key == targetId);
                if (scoreItem != null)
                {
                    roundScore = (float)scoreItem.Val;
                }
            }
            integralText.FormatRichText(roundScore);
        }
    }

    /// <summary>
    /// 加载回合玩家信息
    /// </summary>
    private void LoadRoundPlayerInfo(GameObject go, Msg_MJMYRecordDetailRs ack, int roundIndex)
    {
        var singleOfficeItemContent = go.transform.Find("SingleOfficeRecordViewport/SingleOfficeItemConent");

        // 【深度修复】创建数据深拷贝并同步排序
        var playerScorePairs = new List<(NetMsg.BasePlayer player, double score)>();
        
        foreach (var player in ack.BasePlayer)
        {
            double score = 0;
            // 查找该局该玩家的分数
            if (ack.RoundDetail != null && roundIndex < ack.RoundDetail.Count &&
                ack.RoundDetail[roundIndex]?.Score != null)
            {
                var scoreItem = ack.RoundDetail[roundIndex].Score.FirstOrDefault(s => s.Key == player.PlayerId);
                if (scoreItem != null)
                {
                    score = scoreItem.Val;
                }
            }
            playerScorePairs.Add((player, score));
        }
        
        // 按玩家Id排序
        var sortedPairs = playerScorePairs.OrderBy(p => p.player.PlayerId).ToList();

        for (int j = 0; j < sortedPairs.Count; j++)
        {
            var pair = sortedPairs[j];
            var playerGo = Instantiate(varSingleOfficeItem, singleOfficeItemContent);
            playerGo.name = pair.player.Nick;

            // 设置玩家名称
            var playerNameText = playerGo.transform.Find("Name")?.GetComponent<Text>();
            playerNameText.FormatNickname(pair.player.Nick);

            // 下载并设置头像
            var playerHeadImage = playerGo.transform.Find("head")?.GetComponent<RawImage>();
            if (playerHeadImage != null)
                Util.DownloadHeadImage(playerHeadImage, pair.player.HeadImage);

            // 设置玩家分数
            var integral = playerGo.transform.Find("Integral")?.GetComponent<Text>();
            if (integral != null)
            {
                integral.FormatRichText((float)pair.score);
            }
        }
    }

    /// <summary>
    /// 设置玩家回合分数
    /// </summary>
    private void SetPlayerRoundScore(GameObject playerGo, Msg_MJMYRecordDetailRs ack, int roundIndex, int playerIndex)
    {
        var integral = playerGo.transform.Find("Integral")?.GetComponent<Text>();
        if (integral == null) return;

        var playerId = ack.BasePlayer[playerIndex].PlayerId;
        float score = 0f;

        // 查找玩家分数
        if (ack.RoundDetail != null && roundIndex < ack.RoundDetail.Count &&
            ack.RoundDetail[roundIndex]?.Score != null)
        {
            foreach (var scoreItem in ack.RoundDetail[roundIndex].Score)
            {
                if (scoreItem.Key == playerId)
                {
                    score = (float)scoreItem.Val;
                    break;
                }
            }
        }
        else
        {
            GF.LogWarning($"RoundDetail索引越界或为空: index={roundIndex}, RoundDetail.Count={ack.RoundDetail?.Count ?? 0}");
        }

        integral.FormatRichText(score);
    }

    /// <summary>
    /// 添加回放按钮监听器
    /// </summary>
    private void AddReplayButtonListener(GameObject go, Msg_MJMYRecordDetailRs ack, int index)
    {
        var detailButton = go.transform.Find("ReplayBtn")?.GetComponent<Button>();
        if (detailButton == null) return;

        detailButton.onClick.AddListener(async () =>
        {
            GF.LogInfo_wl($"点击了第{index + 1}局回放，玩法类型: {currentMethodType}");
            lastChooseIndex = ack.RoundDetail.Count - index;

            // 边界检查
            if (ack.RoundDetail == null || index >= ack.RoundDetail.Count)
            {
                GF.LogWarning($"回放数据索引越界: index={index}, RoundDetail.Count={ack.RoundDetail?.Count ?? 0}");
                return;
            }

            // 根据玩法类型请求不同的回放协议
            if (currentMethodType == (int)MethodType.RunFast) // 跑得快
            {
                GF.LogInfo_wl("请求跑得快回放数据");
                Msg_RunFastPlayBackRq req = MessagePool.Instance.Fetch<Msg_RunFastPlayBackRq>();
                req.LogId = ack.RoundDetail[index].RoundLogId;
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(
                    HotfixNetworkComponent.AccountClientName,
                    MessageID.Msg_RunFastPlayBackRq,
                    req);
            }
            else // 麻将及其他玩法
            {
                GF.LogInfo_wl("请求麻将回放数据");
                Msg_MJPlayBackRq req = MessagePool.Instance.Fetch<Msg_MJPlayBackRq>();
                req.LogId = ack.RoundDetail[index].RoundLogId;
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(
                    HotfixNetworkComponent.AccountClientName,
                    MessageID.Msg_MJPlayBackRq,
                    req);
            }
        });
    }

    private async void OnMsg_MJPlayBack(MessageRecvData data)
    {
        Msg_MJPlayBack ack = Msg_MJPlayBack.Parser.ParseFrom(data.Data);
        GF.LogInfo_wl("收到麻将回放数据" + ack);
        // 打开回放界面
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("Msg_MJPlayBack", ack.ToByteArray());
        uiParams.Set<VarInt32>("LastChooseIndex", lastChooseIndex);
        await GF.UI.OpenUIFormAwait(UIViews.MJPlayBack, uiParams);
    }

    private void OnMsg_RunFastPlayBack(MessageRecvData data)
    {
        Msg_RunFastPlayBack ack = Msg_RunFastPlayBack.Parser.ParseFrom(data.Data);
        GF.LogInfo_wl("收到跑得快回放数据" + ack);
        // 打开跑得快回放界面，直接传递协议对象
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("Msg_RunFastPlayBack", ack.ToByteArray());
        uiParams.Set<VarInt32>("LastChooseIndex", lastChooseIndex);
        // 【新增】传入被查看玩家ID（会长查看成员战绩时使用）
        if (currentViewedPlayerId > 0)
        {
            uiParams.Set<VarInt64>("ViewedPlayerId", currentViewedPlayerId);
            GF.LogInfo_wl($"[回放] 传入被查看玩家ID: {currentViewedPlayerId}");
        }
        GF.UI.OpenUIForm(UIViews.PDKPlayBack, uiParams);
    }

    /// <summary>
    /// 清空内容
    /// </summary>
    private void ClearContent(GameObject content)
    {
        if (content == null) return;

        foreach (Transform item in content.transform)
        {
            Destroy(item.gameObject);
        }
    }

    /// <summary>
    /// 清空战绩详情界面
    /// </summary>
    private void ClearRecordDetails()
    {
        // 清空玩家总分列表
        ClearContent(varAllIntegralItemConent);

        // 清空单局战绩列表
        ClearContent(varSingleOfficeRecordContent);

        GF.LogInfo_wl("战绩详情界面已清空");
    }

    /// <summary>
    /// 成员战绩响应处理
    /// </summary>
    private void OnMJTeamRecordResponse(MessageRecvData data)
    {
        // 1. 验证响应是否属于当前面板
        if (currentPanel != lastMJTeamRecordRequestPanel)
        {
            GF.LogInfo_wl($"[战绩系统] 忽略过期的团队战绩响应: 当前面板={currentPanel?.name}, 请求面板={lastMJTeamRecordRequestPanel?.name}");
            return;
        }

        Msg_MJTeamRecordRs ack = Msg_MJTeamRecordRs.Parser.ParseFrom(data.Data);
        GF.LogInfo_wl($"[战绩系统] 收到成员/团队战绩响应" + ack.ToString());
        if (ack == null || ack.TeamRecord == null) return;
        // 缓存团队战绩数据
        teamRecordDataCache.Clear();
        foreach (var record in ack.TeamRecord)
        {
            teamRecordDataCache.Add(record);
        }
        // 清空旧数据
        ClearAllRecords();

        // 统计汇总数据
        float totalScore = 0f;
        int totalRounds = 0;
        int totalLowRounds = 0;
        int totalBigWinners = 0;

        foreach (var record in ack.TeamRecord)
        {
            totalScore += (float)record.TotalRecord;
            totalRounds += record.Round;
            totalLowRounds += (int)record.LowRound;
            totalBigWinners += record.BigWinner;
        }

        // 保存所有成员的原始总计数据（用于底部总计行）
        // allMembersTotalScore = totalScore;
        // allMembersTotalRounds = totalRounds;
        // allMembersTotalLowRounds = totalLowRounds;
        // allMembersTotalBigWinners = totalBigWinners;
        UpdateTeamStatisticsUI(totalScore, totalRounds, totalLowRounds, totalBigWinners, true);
        // 使用 TableView 显示成员战绩数据
        LoadTeamRecordsWithTableView(new List<MJTeamRecord>(ack.TeamRecord));
    }

    /// <summary>
    /// 更新成员底部Ui
    /// </summary>
    /// <param name="updateBottomTotal">是否同时更新底部总计（所有成员合计）</param>
    private void UpdateTeamStatisticsUI(float totalScore, int totalRounds, int lowRounds, int bigWinners, bool updateBottomTotal = false)
    {
        GF.LogInfo_wl($"更新底部总计行 - 积分: {totalScore}, 牌局数: {totalRounds}, 低分局: {lowRounds}, 大赢家: {bigWinners}");
        if (currentPanel == null) return;
        // 查找底部总计行的容器
        Transform totalRow = null;
        if (currentPanel == varMemberRecord)
        {
            totalRow = varMemberRecord?.transform.Find("statisticsBg");
        }
        totalRow.Find("overallRecord")?.GetComponent<Text>()?.FormatRichText(totalScore);
        totalRow.Find("CardNumText")?.GetComponent<Text>()?.FormatRichText(totalRounds);
        totalRow.Find("LowScoreText")?.GetComponent<Text>()?.FormatRichText(lowRounds);
        totalRow.Find("BigWinNumText")?.GetComponent<Text>()?.FormatRichText(bigWinners);
    }


    /// <summary>
    /// 递归查找子节点
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent == null) return null;

        var child = parent.Find(name);
        if (child != null) return child;

        foreach (Transform t in parent)
        {
            child = FindChildRecursive(t, name);
            if (child != null) return child;
        }

        return null;
    }
    /// <summary>
    /// 我的战绩响应处理
    /// </summary>
    private void OnMJMYRecordResponse(MessageRecvData data)
    {
        Msg_MJMYRecordRs ack = Msg_MJMYRecordRs.Parser.ParseFrom(data.Data);
        GF.LogInfo_wl("收到我的/圈子战绩响应" + ack.ToString());

        // 判断是否是成员战绩详情的响应（当成员战绩详情面板显示时）
        if (MemberRecordDetails != null && MemberRecordDetails.activeSelf)
        {
            GF.LogInfo_wl($"[成员战绩详情] 收到成员战绩数据，共 {ack.Record.Count} 条");
            LoadMemberRecordDetails(ack);
            return;
        }
        // 验证响应是否属于当前面板（防止切换页签时的网络延迟导致数据错乱）
        if (lastMJMyRecordRequestPanel != null)
        {
            if (currentPanel != lastMJMyRecordRequestPanel)
            {
                GF.LogInfo_wl($"[战绩系统] 忽略过时的响应，当前面板: {currentPanel?.name}, 请求面板: {lastMJMyRecordRequestPanel?.name}");
                return;
            }
        }

        // 缓存战绩数据到成员变量（重要：使用成员变量而不是局部变量）
        recordDataCache.Clear();
        foreach (var record in ack.Record)
        {
            recordDataCache.Add(record);
        }
        // 统计数据
        float totalScore = 0f;
        int totalRecordCount = recordDataCache.Count;
        int bigWinCount = 0;
        int completeCount = 0;
        int dismissCount = 0;

        // 判断是否是圈子战绩（圈子战绩需要统计所有玩家的总分）
        bool isCircleRecord = (currentPanel == varCircleRecord);
        foreach (var record in recordDataCache)
        {
            if (isCircleRecord)
            {

            }
            else
            {
                // 我的战绩：只统计我的积分
                UpdateStatistics(record, ref totalScore, ref bigWinCount);
            }
            // 统计完整牌局和中途解散
            if (record.PlayRound >= record.TotalRound)
                completeCount++;
            else
                dismissCount++;
        }
        // 更新统计 UI
        UpdateMyStatisticsUI(totalScore, totalRecordCount, bigWinCount, completeCount, dismissCount);
        LoadRecordsWithTableView(new List<MJMyRecord>(recordDataCache));
    }

    /// <summary>
    /// 更新统计数据（我的战绩）
    /// </summary>
    private void UpdateStatistics(NetMsg.MJMyRecord record, ref float totalScore, ref int bigWinCount)
    {
        // 检查是否是大赢家
        if (record.BigWinner == myPlayerId)
        {
            bigWinCount++;
        }
        // 累计自己的总积分
        for (int j = 0; j < record.Player.Count && j < record.Score.Count; j++)
        {
            if (record.Player[j].PlayerId == myPlayerId)
            {
                totalScore += (float)record.Score[j].Val;
                break;
            }
        }
    }


    /// <summary>
    /// 更新统计信息UI
    /// </summary>
    /// <summary>
    /// 更新我的/圈子战绩统计信息UI
    /// </summary>
    private void UpdateMyStatisticsUI(float totalScore, int recordCount, int bigWinCount, int completeCount, int dismissCount)
    {
        if (currentPanel == null) return;
        // 积分总计
        var varIntegralText = currentPanel.transform.Find("IntegralText")?.GetComponent<Text>();
        if (varIntegralText != null)
        {
            varIntegralText.color = Color.yellow;
            varIntegralText.text = $"积分总计：{totalScore:+0.##;-0.##;0}";
        }
        // 牌局数
        var varCardText = currentPanel.transform.Find("CardText")?.GetComponent<Text>();
        if (varCardText != null)
        {
            varCardText.color = Color.yellow;
            varCardText.text = $"牌局数：{recordCount}";
        }
        // 大赢家
        var varBigWinText = currentPanel.transform.Find("BigWinText")?.GetComponent<Text>();
        if (varBigWinText != null)
        {
            varBigWinText.color = Color.yellow;
            varBigWinText.text = $"大赢家：{bigWinCount}次";
        }
        // 完整牌局 (如果有该文本)
        var varCompleteText = currentPanel.transform.Find("FullCardtext")?.GetComponent<Text>();
        if (varCompleteText == null) varCompleteText = currentPanel.transform.Find("CompleteText")?.GetComponent<Text>();
        if (varCompleteText != null)
        {
            varCompleteText.color = Color.yellow;
            varCompleteText.text = $"完整牌局：{completeCount}";
        }
        // 中途解散 (如果有该文本)
        var varDismissText = currentPanel.transform.Find("DisbandHalfwayText")?.GetComponent<Text>();
        if (varDismissText == null) varDismissText = currentPanel.transform.Find("DismissText")?.GetComponent<Text>();
        if (varDismissText != null)
        {
            varDismissText.color = Color.yellow;
            varDismissText.text = $"中途解散：{dismissCount}";
        }

        // 低分局 (如果有该文本)
        var varLowBranchText = currentPanel.transform.Find("LowBranchText")?.GetComponent<Text>();
        if (varLowBranchText != null)
        {
            varLowBranchText.color = Color.yellow;
            varLowBranchText.text = $"低分局：0";
        }
    }
    /// <summary>
    /// 设置战绩基本信息
    /// </summary>
    private void SetRecordBasicInfo(GameObject item, NetMsg.MJMyRecord record, int index)
    {
        SetTextIfExists(item, "SerialText", (index + 1).ToString());
        SetTextIfExists(item, "RoomNumText", $"房号：{record.DeskId}");
        SetTextIfExists(item, "GameNameText", record.DeskName);
        SetTextIfExists(item, "FloorText", $"{record.Floor}楼");
        SetTextIfExists(item, "MatchNumText", $"{record.PlayRound}/{record.TotalRound}局");
        SetTextIfExists(item, "SituationText", record.PlayRound < record.TotalRound ? "中途解散" : "正常结束");
    }
    /// <summary>
    /// 设置战绩时间
    /// </summary>
    private void SetRecordTime(GameObject item, NetMsg.MJMyRecord record)
    {
        var matchTimeText = item.transform.Find("MatchTimeText")?.GetComponent<Text>();
        if (matchTimeText == null) return;

        try
        {
            DateTime dateTime = ConvertTimestampToDateTime(record.LogTime);
            // 格式：2025/08/08 14:18
            matchTimeText.text = dateTime.ToString("yyyy/MM/dd HH:mm");
        }
        catch (System.Exception ex)
        {
            matchTimeText.text = record.LogTime.ToString();
            GF.LogError($"时间转换失败: {ex.Message}, 时间戳: {record.LogTime}");
        }
    }

    /// <summary>
    /// 转换时间戳为DateTime
    /// </summary>
    private DateTime ConvertTimestampToDateTime(long timestamp)
    {
        // 判断是秒级还是毫秒级时间戳
        if (timestamp > MILLISECOND_THRESHOLD)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).ToLocalTime().DateTime;
        }
        else
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).ToLocalTime().DateTime;
        }
    }


    /// <summary>
    /// 加载玩家列表
    /// </summary>
    private void LoadPlayerList(GameObject item, NetMsg.MJMyRecord record)
    {
        var varPlayerListConent = item.transform.Find("PlayerListConent");
        if (varPlayerListConent == null) return;
        // 清空现有玩家信息显示
        ClearContent(varPlayerListConent.gameObject);
        
        // 【深度修复】创建数据深拷贝并同步排序，确保玩家信息和分数始终一致
        // 构建玩家-分数的配对列表
        var playerScorePairs = new List<(NetMsg.BasePlayer player, double score, long playerId)>();
        
        for (int i = 0; i < record.Player.Count; i++)
        {
            var player = record.Player[i];
            double score = 0;
            
            // 从Score列表中查找对应玩家的分数（按PlayerId匹配）
            foreach (var scoreInfo in record.Score)
            {
                if (scoreInfo.Key == player.PlayerId)
                {
                    score = scoreInfo.Val;
                    break;
                }
            }
            
            playerScorePairs.Add((player, score, player.PlayerId));
        }
        
        // 【关键修复】客户端根据实际分数计算大赢家，而不是依赖后台可能错误的BigWinner字段
        double maxScore = double.MinValue;
        long realBigWinnerId = 0;
        foreach (var pair in playerScorePairs)
        {
            if (pair.score > maxScore)
            {
                maxScore = pair.score;
                realBigWinnerId = pair.playerId;
            }
        }
        
        // 按玩家Id排序
        var sortedPairs = playerScorePairs.OrderBy(p => p.playerId).ToList();
        
        // 不再限制玩家数量，生成所有玩家的战绩项
        for (int j = 0; j < sortedPairs.Count; j++)
        {
            var pair = sortedPairs[j];
            // 使用客户端计算的真实大赢家ID
            CreatePlayerItemWithData(varPlayerListConent, pair.player, pair.score, realBigWinnerId, delayLoadImage: true);
        }
    }

    /// <summary>
    /// 创建玩家项
    /// </summary>
    private void CreatePlayerItem(Transform parent, NetMsg.MJMyRecord record, int playerIndex, bool delayLoadImage = false)
    {
        var go = Instantiate(varPlayerRqecordItem, parent);
        go.name = record.Player[playerIndex].Nick;

        // 设置玩家基本信息
        SetTextIfExists(go, "Name", record.Player[playerIndex].Nick);
        SetTextIfExists(go, "ID", $"ID:{record.Player[playerIndex].PlayerId}");

        // 设置头像(可选择延迟加载)
        var headImage = go.transform.Find("head")?.GetComponent<RawImage>();
        if (headImage != null)
        {
            if (delayLoadImage)
            {
                // 延迟加载头像,避免界面卡顿
                StartCoroutine(DelayLoadHeadImage(headImage, record.Player[playerIndex].HeadImage));
            }
            else
            {
                Util.DownloadHeadImage(headImage, record.Player[playerIndex].HeadImage);
            }
        }

        // 设置积分
        SetPlayerScore(go, record.Score[playerIndex].Val);

        // 设置大赢家标识
        var bigWinImage = go.transform.Find("BigWin")?.GetComponent<Image>();
        if (bigWinImage != null)
        {
            bigWinImage.gameObject.SetActive(record.BigWinner == record.Player[playerIndex].PlayerId);
        }

        // 设置自己标识
        var myImage = go.transform.Find("MySelf")?.GetComponent<Image>();
        if (myImage != null)
        {
            myImage.gameObject.SetActive(record.Player[playerIndex].PlayerId == myPlayerId);
        }
    }

    /// <summary>
    /// 创建玩家项（使用已配对的数据）
    /// </summary>
    private void CreatePlayerItemWithData(Transform parent, NetMsg.BasePlayer player, double score, long bigWinnerId, bool delayLoadImage = false)
    {
        var go = Instantiate(varPlayerRqecordItem, parent);
        go.name = player.Nick;

        // 设置玩家基本信息
        SetTextIfExists(go, "Name", player.Nick);
        SetTextIfExists(go, "ID", $"ID:{player.PlayerId}");

        // 设置头像(可选择延迟加载)
        var headImage = go.transform.Find("head")?.GetComponent<RawImage>();
        if (headImage != null)
        {
            if (delayLoadImage)
            {
                // 延迟加载头像,避免界面卡顿
                StartCoroutine(DelayLoadHeadImage(headImage, player.HeadImage));
            }
            else
            {
                Util.DownloadHeadImage(headImage, player.HeadImage);
            }
        }

        // 设置积分
        SetPlayerScore(go, score);

        // 设置大赢家标识
        var bigWinImage = go.transform.Find("BigWin")?.GetComponent<Image>();
        if (bigWinImage != null)
        {
            bigWinImage.gameObject.SetActive(bigWinnerId == player.PlayerId);
        }

        // 设置自己标识
        var myImage = go.transform.Find("MySelf")?.GetComponent<Image>();
        if (myImage != null)
        {
            myImage.gameObject.SetActive(player.PlayerId == myPlayerId);
        }
    }

    /// <summary>
    /// 延迟加载头像(优化性能)
    /// </summary>
    private System.Collections.IEnumerator DelayLoadHeadImage(RawImage headImage, string imageUrl)
    {
        // 等待一帧
        yield return null;

        // 检查对象是否还存在
        if (headImage != null && headImage.gameObject != null)
        {
            Util.DownloadHeadImage(headImage, imageUrl);
        }
    }

    /// <summary>
    /// 设置玩家分数
    /// </summary>
    private void SetPlayerScore(GameObject go, double scoreValue)
    {
        var integralText = go.transform.Find("IntegralText")?.GetComponent<Text>();
        if (integralText == null) return;

        integralText.text = scoreValue.ToString();
        integralText.color = scoreValue > 0 ? Color.red : Color.green;

        if (scoreValue > 0)
        {
            integralText.text = $"+{integralText.text}";
        }
    }

    /// <summary>
    /// 设置文本（如果存在）
    /// </summary>
    private void SetTextIfExists(GameObject parent, string path, string text)
    {
        var textComponent = parent.transform.Find(path)?.GetComponent<Text>();
        textComponent.FormatNickname(text);
    }
    #endregion

    #region UI事件处理

    /// <summary>
    /// 初始化Toggle默认选中状态
    /// </summary>
    private void InitToggle()
    {
        // 初始化我的战绩日期默认选中
        InitDateToggleGroup(MyRqecordDateConent, MyRqecordDateConentItem);

        // 初始化圈子战绩日期默认选中
        InitDateToggleGroup(CircleRecordDateConent, CircleRecordDateConentItem);

        // 初始化成员战绩日期默认选中
        InitDateToggleGroup(MemberRecorddateConent, MemberRecorddateConentItem);

    }

    /// <summary>
    /// 初始化特定日期容器的默认选中状态
    /// </summary>
    private void InitDateToggleGroup(GameObject container, GameObject contentItem)
    {
        if (container == null || contentItem == null) return;

        Transform firstItem = null;
        foreach (Transform child in container.transform)
        {
            if (child.gameObject == contentItem) continue;
            if (child.GetComponent<Toggle>() != null)
            {
                firstItem = child;
                break;
            }
        }

        if (firstItem == null) return;

        // 设置父级关系
        if (contentItem.transform.parent != container.transform)
        {
            contentItem.transform.SetParent(container.transform, false);
        }

        // 设置默认选中
        var firstToggle = firstItem.GetComponent<Toggle>();
        if (firstToggle != null)
        {
            firstToggle.isOn = true;
            // 注意：这里不需要手动调用 UpdateSelectedDate，因为 isOn = true 会触发 SetupDateToggles 中注册的监听器
        }

        // 调整面板位置
        var clickedIndex = firstItem.GetSiblingIndex();
        var panelIndex = contentItem.transform.GetSiblingIndex();
        var targetIndex = clickedIndex + (panelIndex > clickedIndex ? 1 : 0);
        var maxIndex = container.transform.childCount - 1;
        targetIndex = Mathf.Clamp(targetIndex, 0, maxIndex);
        contentItem.transform.SetSiblingIndex(targetIndex);
    }

    /// <summary>
    /// 按钮点击事件处理
    /// </summary>
    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);

        switch (btId)
        {
            case "duiJuXQ":
                ToggleMemberRecordFilter(false);  // 显示所有对局详情
                break;

            case "daYingJia":
                ToggleMemberRecordFilter(true);   // 只显示大赢家记录
                break;

            case "时段筛选弹窗":
                TogglePanel(varCustomTime);
                break;

            case "时段筛选选择":
                HandleTimeFilterSelection();
                break;

            case "选时间确定":
                HandleTimeConfirm();
                break;

            case "选时间弹窗":
                TogglePanel(varChooseTime);
                break;

            case "时段筛选确定":
                HandleTimeFilterConfirm();
                break;

            case "选择楼层":
                var chooseFloor = GetCurrentChooseFloor();
                if (chooseFloor != null)
                {
                    TogglePanel(chooseFloor);
                }
                break;
            case "退出":
                GF.UI.Close(this.UIForm);
                break;
            case "关闭战绩详情":
                varRecordDetails.SetActive(false);
                break;
            case "关闭成员战绩详情":
                MemberRecordDetails.SetActive(false);
                break;
            case "搜索":
                HandleSearch();
                break;
            case "战绩排序大到小":
                HandleSort(SortType.ScoreDescending);
                break;
            case "战绩排序小到大":
                HandleSort(SortType.ScoreAscending);
                break;
            case "牌局排序大到小":
                HandleSort(SortType.RoundDescending);
                break;
            case "牌局排序小到大":
                HandleSort(SortType.RoundAscending);
                break;
            case "低分排序大到小":
                HandleSort(SortType.LowScoreDescending);
                break;
            case "低分排序小到大":
                HandleSort(SortType.LowScoreAscending);
                break;
            case "大赢家排序大到小":
                HandleSort(SortType.BigWinnerDescending);
                break;
            case "大赢家排序小到大":
                HandleSort(SortType.BigWinnerAscending);
                break;
        }
    }

    /// <summary>
    /// 处理搜索请求
    /// </summary>
    private void HandleSearch()
    {
        if (FindInput == null)
        {
            return;
        }
        string searchText = FindInput.text?.Trim();
        if (string.IsNullOrEmpty(searchText))
        {
            //如果搜索内容为空，直接重新请求全部数据
            GF.LogInfo_wl("搜索内容为空，重新加载全部数据");
            currentRequestAction?.Invoke();
            return;
        }
        GF.LogInfo_wl("点击了搜索按钮");
        // 判断当前是哪个页面
        if (currentPanel == varMyRqecord)
        {
            GF.LogInfo_wl("在我的战绩中搜索");
            SearchInMyRecord(searchText);
        }
        else if (currentPanel == varMemberRecord)
        {
            GF.LogInfo_wl("在成员战绩中搜索");
            SearchInMemberRecord(searchText);
        }
        else if (currentPanel == varCircleRecord)
        {
            GF.LogInfo_wl("在圈子战绩中搜索");
            SearchInCircleRecord(searchText);
        }
        else if (currentPanel == varTeamStats)
        {
            SearchInTeamStats(searchText);
        }
        else
        {
            GF.UI.ShowToast("未知的页面类型");
        }
    }

    /// <summary>
    /// 在我的战绩中搜索
    /// </summary>
    private void SearchInMyRecord(string searchText)
    {
        SearchInRecordData(searchText, "我的战绩");
    }

    /// <summary>
    /// 在圈子战绩中搜索
    /// </summary>
    private void SearchInCircleRecord(string searchText)
    {
        SearchInRecordData(searchText, "圈子战绩");
    }

    /// <summary>
    /// 在战绩数据中搜索（我的战绩和圈子战绩共用）
    /// </summary>
    private void SearchInRecordData(string searchText, string panelName)
    {
        if (recordDataCache == null || recordDataCache.Count == 0)
        {
            GF.UI.ShowToast("暂无战绩数据");
            return;
        }

        // 尝试解析为玩家ID
        bool isPlayerId = long.TryParse(searchText, out long playerId);
        // 尝试解析为房号
        bool isDeskId = long.TryParse(searchText, out long deskId);

        if (!isPlayerId && !isDeskId)
        {
            GF.UI.ShowToast("请输入有效的玩家ID或房号");
            return;
        }

        // 筛选数据
        var filteredRecords = new List<MJMyRecord>();
        foreach (var record in recordDataCache)
        {
            bool match = false;

            // 按房号搜索
            if (isDeskId && record.DeskId == deskId)
            {
                match = true;
            }

            // 按玩家ID搜索
            if (isPlayerId && !match)
            {
                foreach (var player in record.Player)
                {
                    if (player.PlayerId == playerId)
                    {
                        match = true;
                        break;
                    }
                }
            }

            if (match)
            {
                filteredRecords.Add(record);
            }
        }

        // 显示搜索结果
        if (filteredRecords.Count == 0)
        {
            GF.UI.ShowToast("未找到匹配的战绩");
        }
        else
        {
            GF.LogInfo_wl($"[{panelName}] 搜索到 {filteredRecords.Count} 条匹配战绩");
            DisplayFilteredRecords(filteredRecords);
        }
    }

    /// <summary>
    /// 在成员战绩中搜索
    /// </summary>
    private void SearchInMemberRecord(string searchText)
    {
        SearchInTeamRecordData(searchText, "成员战绩");
    }

    /// <summary>
    /// 在团队统计中搜索
    /// </summary>
    private void SearchInTeamStats(string searchText)
    {
        SearchInTeamRecordData(searchText, "团队统计");
    }

    /// <summary>
    /// 在团队战绩数据中搜索（成员战绩和团队统计共用）
    /// </summary>
    private void SearchInTeamRecordData(string searchText, string panelName)
    {
        if (teamRecordDataCache == null || teamRecordDataCache.Count == 0)
        {
            GF.UI.ShowToast("暂无战绩数据");
            return;
        }
        // 尝试解析为玩家ID
        bool isPlayerId = long.TryParse(searchText, out long playerId);
        if (!isPlayerId)
        {
            GF.UI.ShowToast("请输入有效的玩家ID");
            return;
        }
        // 筛选数据（按玩家ID搜索）
        var filteredRecords = new List<NetMsg.MJTeamRecord>();
        foreach (var record in teamRecordDataCache)
        {
            if (record.BasePlayer != null && record.BasePlayer.PlayerId == playerId)
            {
                filteredRecords.Add(record);
                break; // 团队战绩中每个玩家只有一条记录
            }
        }

        // 显示搜索结果
        if (filteredRecords.Count == 0)
        {
            GF.UI.ShowToast("未找到匹配的玩家");
        }
        else
        {
            GF.LogInfo_wl($"[{panelName}] 搜索到 {filteredRecords.Count} 条匹配记录");
            DisplayFilteredTeamRecords(filteredRecords);
        }
    }

    /// <summary>
    /// 显示筛选后的团队战绩列表
    /// </summary>
    private void DisplayFilteredTeamRecords(List<NetMsg.MJTeamRecord> filteredRecords)
    {
        // 暂时保存原始缓存数据
        var originalCache = teamRecordDataCache;
        teamRecordDataCache = filteredRecords;

        // 重新计算统计数据
        float totalScore = 0f;
        int totalRounds = 0;
        int totalLowRounds = 0;
        int totalBigWinners = 0;

        foreach (var record in filteredRecords)
        {
            totalScore += (float)record.TotalRecord;
            totalRounds += record.Round;
            totalLowRounds += (int)record.LowRound;
            totalBigWinners += record.BigWinner;
        }

        // 更新统计 UI（顶部显示所有成员数据，底部显示筛选后数据）
        UpdateTeamStatisticsUI(totalScore, totalRounds, totalLowRounds, totalBigWinners);

        // 使用 TableView 显示筛选结果
        LoadTeamRecordsWithTableView(filteredRecords);

        // 注意：搜索后不恢复原始数据，用户可以通过重新选择日期或点击按钮来刷新
    }

    /// <summary>
    /// 显示筛选后的战绩列表
    /// </summary>
    private void DisplayFilteredRecords(List<MJMyRecord> filteredRecords)
    {
        // 清空当前显示
        ClearAllRecords();

        // 重新计算统计数据
        float totalScore = 0f;
        int bigWinCount = 0;
        int completeCount = 0;
        int dismissCount = 0;

        foreach (var record in filteredRecords)
        {
            UpdateStatistics(record, ref totalScore, ref bigWinCount);

            // 统计完整牌局和中途解散
            if (record.PlayRound >= record.TotalRound)
                completeCount++;
            else
                dismissCount++;
        }

        // 更新统计 UI
        UpdateMyStatisticsUI(totalScore, filteredRecords.Count, bigWinCount, completeCount, dismissCount);

        // 使用 TableView 显示筛选结果
        LoadRecordsWithTableView(filteredRecords);

        // 注意：搜索后不恢复原始数据，用户可以通过重新选择日期或点击按钮来刷新
        // 如果需要在搜索后显示"清除搜索"按钮，可以添加一个标志位和清除功能
    }

    /// <summary>
    /// 处理排序
    /// </summary>
    private void HandleSort(SortType sortType)
    {
        // 判断当前是哪个页面
        if (currentPanel == varMyRqecord || currentPanel == varCircleRecord)
        {
            // 我的战绩和圈子战绩排序（这些按钮在这些页面不可用，预留接口）
            GF.UI.ShowToast("该页面暂不支持此排序功能");
        }
        else if (currentPanel == varMemberRecord || currentPanel == varTeamStats)
        {
            // 成员战绩和团队统计排序
            SortTeamRecords(sortType);
        }
        else
        {
            GF.UI.ShowToast("未知的页面类型");
        }
    }

    /// <summary>
    /// 对团队战绩数据进行排序
    /// </summary>
    private void SortTeamRecords(SortType sortType)
    {
        if (teamRecordDataCache == null || teamRecordDataCache.Count == 0)
        {
            GF.UI.ShowToast("暂无数据可排序");
            return;
        }

        // 根据排序类型进行排序
        switch (sortType)
        {
            case SortType.ScoreDescending:
                teamRecordDataCache.Sort((a, b) => b.TotalRecord.CompareTo(a.TotalRecord));
                GF.LogInfo_wl("战绩排序：大到小");
                break;

            case SortType.ScoreAscending:
                teamRecordDataCache.Sort((a, b) => a.TotalRecord.CompareTo(b.TotalRecord));
                GF.LogInfo_wl("战绩排序：小到大");
                break;

            case SortType.RoundDescending:
                teamRecordDataCache.Sort((a, b) => b.Round.CompareTo(a.Round));
                GF.LogInfo_wl("牌局排序：大到小");
                break;

            case SortType.RoundAscending:
                teamRecordDataCache.Sort((a, b) => a.Round.CompareTo(b.Round));
                GF.LogInfo_wl("牌局排序：小到大");
                break;

            case SortType.LowScoreDescending:
                teamRecordDataCache.Sort((a, b) => b.LowRound.CompareTo(a.LowRound));
                GF.LogInfo_wl("低分排序：大到小");
                break;

            case SortType.LowScoreAscending:
                teamRecordDataCache.Sort((a, b) => a.LowRound.CompareTo(b.LowRound));
                GF.LogInfo_wl("低分排序：小到大");
                break;

            case SortType.BigWinnerDescending:
                teamRecordDataCache.Sort((a, b) => b.BigWinner.CompareTo(a.BigWinner));
                GF.LogInfo_wl("大赢家排序：大到小");
                break;

            case SortType.BigWinnerAscending:
                teamRecordDataCache.Sort((a, b) => a.BigWinner.CompareTo(b.BigWinner));
                GF.LogInfo_wl("大赢家排序：小到大");
                break;
        }

        // 重新显示排序后的数据
        DisplaySortedTeamRecords();
    }

    /// <summary>
    /// 显示排序后的团队战绩列表
    /// </summary>
    private void DisplaySortedTeamRecords()
    {
        // 重新计算统计数据
        float totalScore = 0f;
        int totalRounds = 0;
        int totalLowRounds = 0;
        int totalBigWinners = 0;
        foreach (var record in teamRecordDataCache)
        {
            totalScore += (float)record.TotalRecord;
            totalRounds += record.Round;
            totalLowRounds += (int)record.LowRound;
            totalBigWinners += record.BigWinner;
        }
        UpdateTeamStatisticsUI(totalScore, totalRounds, totalLowRounds, totalBigWinners);
        // 使用 TableView 显示排序结果
        LoadTeamRecordsWithTableView(new List<MJTeamRecord>(teamRecordDataCache));
    }

    /// <summary>
    /// 切换面板显示状态
    /// </summary>
    private void TogglePanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(!panel.activeSelf);
        }
    }

    /// <summary>
    /// 处理时段筛选选择
    /// </summary>
    private void HandleTimeFilterSelection()
    {
        var clickObj = EventSystem.current.currentSelectedGameObject?.transform;
        if (clickObj == null) return;

        var text = clickObj.GetChild(0)?.GetComponent<Text>();
        if (text != null)
        {
            customTime = text.text;
        }
    }

    /// <summary>
    /// 处理时间确定
    /// </summary>
    private void HandleTimeConfirm()
    {
        // 获取当前界面的时段引用
        GetCurrentTimeRange(out int currentStart, out int currentEnd);

        // 验证时间有效性
        if (currentStart >= currentEnd)
        {
            GF.UI.ShowToast("开始时间不能大于等于结束时间");
            return;
        }

        // 更新显示
        var timeText = varCustomTime.transform.Find("GameObject/4/text")?.GetComponent<Text>();
        timeText.text = $"{currentStart}:00-{currentEnd}:00";

        varChooseTime.SetActive(false);
    }

    /// <summary>
    /// 处理时段筛选确定
    /// </summary>
    private void HandleTimeFilterConfirm()
    {
        if (!ValidateCustomTime())
        {
            GF.UI.ShowToast("请选择自定义时间");
            return;
        }

        // 解析时间段
        ParseCustomTime();

        // 更新显示
        UpdateTimeDisplay();

        // 关闭面板并重新请求数据
        varCustomTime.SetActive(false);
        currentRequestAction?.Invoke();
    }

    /// <summary>
    /// 验证自定义时间
    /// </summary>
    private bool ValidateCustomTime()
    {
        return !string.IsNullOrEmpty(customTime) && customTime != "自定义时间";
    }

    /// <summary>
    /// 解析自定义时间并保存到对应界面的变量
    /// </summary>
    private void ParseCustomTime()
    {
        int parsedStart, parsedEnd;

        if (customTime == "全天")
        {
            parsedStart = MIN_HOUR;
            parsedEnd = MAX_HOUR;
        }
        else
        {
            // 分离时间段
            var time = customTime.Split('-');
            if (time.Length >= 2)
            {
                parsedStart = int.Parse(time[0].Replace(":00", ""));
                parsedEnd = int.Parse(time[1].Replace(":00", ""));
            }
            else
            {
                parsedStart = MIN_HOUR;
                parsedEnd = MAX_HOUR;
            }
        }

        // 根据当前面板保存到对应变量
        SetCurrentTimeRange(parsedStart, parsedEnd);
    }

    /// <summary>
    /// 更新时间显示（支持4个界面）
    /// </summary>
    private void UpdateTimeDisplay()
    {
        // 获取当前界面的时段
        GetCurrentTimeRange(out int currentStart, out int currentEnd);

        // 根据当前面板动态获取对应的日期内容容器
        GameObject contentItem = null;

        if (currentPanel == varMyRqecord)
            contentItem = MyRqecordDateConentItem;
        else if (currentPanel == varCircleRecord)
            contentItem = CircleRecordDateConentItem;
        else if (currentPanel == varMemberRecord)
            contentItem = MemberRecorddateConentItem;
        else if (currentPanel == varTeamStats)
            contentItem = MyRqecordDateConentItem; // 作为回退方案

        if (contentItem != null)
        {
            var timeText = contentItem.transform.Find("timeText")?.GetComponent<Text>();
            if (timeText != null)
            {
                timeText.text = $"{currentStart}:00-{currentEnd}:00";
            }
        }
    }

    /// <summary>
    /// 调整时间（小时）
    /// </summary>
    /// <param name="i">调整增量</param>
    public void GetHour(int i)
    {
        hour += i;

        // 获取点击对象的父物体
        var parent = EventSystem.current.currentSelectedGameObject?.transform.parent;
        if (parent == null) return;

        var item = parent.name;
        var textPath = $"{item}/{item}";
        var textComponent = varChooseTime.transform.Find(textPath)?.GetComponent<Text>();
        if (textComponent == null) return;

        // 解析当前小时数
        int itemText = int.Parse(textComponent.text.Replace("时", ""));
        int newHour = itemText + i;

        // 处理24小时循环
        if (newHour > MAX_HOUR)
            newHour -= HOURS_PER_DAY;
        else if (newHour < MIN_HOUR)
            newHour += HOURS_PER_DAY;

        // 更新显示
        textComponent.text = $"{newHour}时";

        // 更新对应的时间变量到当前界面
        if (item == "startTime")
            SetCurrentStartTime(newHour);
        else
            SetCurrentEndTime(newHour);
    }

    /// <summary>
    /// 获取当前界面的时段范围
    /// </summary>
    private void GetCurrentTimeRange(out int start, out int end)
    {
        if (currentPanel == varMyRqecord)
        {
            start = myRecordStartTime;
            end = myRecordEndTime;
        }
        else if (currentPanel == varMemberRecord)
        {
            start = memberRecordStartTime;
            end = memberRecordEndTime;
        }
        else if (currentPanel == varCircleRecord)
        {
            start = circleRecordStartTime;
            end = circleRecordEndTime;
        }
        else if (currentPanel == varTeamStats)
        {
            start = teamStatsStartTime;
            end = teamStatsEndTime;
        }
        else
        {
            start = MIN_HOUR;
            end = MAX_HOUR;
        }
    }

    /// <summary>
    /// 设置当前界面的时段范围
    /// </summary>
    private void SetCurrentTimeRange(int start, int end)
    {
        if (currentPanel == varMyRqecord)
        {
            myRecordStartTime = start;
            myRecordEndTime = end;
        }
        else if (currentPanel == varMemberRecord)
        {
            memberRecordStartTime = start;
            memberRecordEndTime = end;
        }
        else if (currentPanel == varCircleRecord)
        {
            circleRecordStartTime = start;
            circleRecordEndTime = end;
        }
        else if (currentPanel == varTeamStats)
        {
            teamStatsStartTime = start;
            teamStatsEndTime = end;
        }
    }

    /// <summary>
    /// 设置当前界面的开始时间
    /// </summary>
    private void SetCurrentStartTime(int hour)
    {
        if (currentPanel == varMyRqecord)
            myRecordStartTime = hour;
        else if (currentPanel == varMemberRecord)
            memberRecordStartTime = hour;
        else if (currentPanel == varCircleRecord)
            circleRecordStartTime = hour;
        else if (currentPanel == varTeamStats)
            teamStatsStartTime = hour;
    }

    /// <summary>
    /// 设置当前界面的结束时间
    /// </summary>
    private void SetCurrentEndTime(int hour)
    {
        if (currentPanel == varMyRqecord)
            myRecordEndTime = hour;
        else if (currentPanel == varMemberRecord)
            memberRecordEndTime = hour;
        else if (currentPanel == varCircleRecord)
            circleRecordEndTime = hour;
        else if (currentPanel == varTeamStats)
            teamStatsEndTime = hour;
    }

    /// <summary>
    /// 将小时数转换为选中日期对应时间的时间戳
    /// </summary>
    /// <param name="hour">小时数(0-24)</param>
    /// <returns>时间戳（秒）</returns>
    private long ConvertHourToTimestamp(int hour)
    {
        var targetTime = selectedDate.AddHours(hour);
        return ((DateTimeOffset)targetTime).ToUnixTimeSeconds();
    }

    /// <summary>
    /// 将小时转换为毫秒时间戳（基于当前选中日期）
    /// </summary>
    /// <param name="hour">小时数(0-24)</param>
    /// <returns>时间戳（毫秒）</returns>
    private long ConvertHourToTimestampMs(int hour)
    {
        var targetTime = selectedDate.AddHours(hour);
        return ((DateTimeOffset)targetTime).ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// 根据Toggle名称更新选中的日期
    /// </summary>
    /// <param name="toggleName">Toggle的名称</param>
    private void UpdateSelectedDate(string toggleName)
    {
        var today = DateTime.Today;

        selectedDate = toggleName switch
        {
            "1" => today,                    // 今天
            "2" => today.AddDays(-1),        // 昨天
            "3" => today.AddDays(-2),        // 前天
            "4" => today.AddDays(-3),        // 大前天
            "5" => today.AddDays(-4),        // 4天前
            "6" => today.AddDays(-5),        // 5天前
            "7" => today.AddDays(-6),        // 6天前
            _ => today                       // 默认今天
        };

        if (!int.TryParse(toggleName, out _))
        {
            GF.LogWarning($"未知的日期Toggle名称: {toggleName}，使用今天作为默认值");
        }


        // 根据新的选中日期重新请求战绩数据
        currentRequestAction?.Invoke();
    }

    /// <summary>
    /// 清空所有战绩显示
    /// </summary>
    private void ClearAllRecords()
    {
        // 清空数据缓存
        recordDataCacheForTableView.Clear();
        teamRecordDataCacheForTableView.Clear();

        // 获取当前 TableView 并刷新（让 TableView 自己管理单元格的销毁）
        var currentTableView = GetCurrentTableView();
        if (currentTableView != null)
        {
            // 先刷新清空数据，再重置滚动位置
            currentTableView.ReloadData();
            currentTableView.ScrollY = 0;
        }

        GF.LogInfo_wl("[战绩系统] 已清空所有战绩显示并重置滚动位置");
    }

    /// <summary>
    /// 初始化所有 TableView 组件
    /// </summary>
    private void InitializeTableViews()
    {
        // 初始化我的战绩 TableView
        if (MyRqecordScroll != null)
        {
            MyRqecordScroll.SetNumberOfRowsForTableView(tv => recordDataCacheForTableView.Count);
            MyRqecordScroll.SetHeightForRowInTableView((tv, row) => GetRecordItemHeight());
            MyRqecordScroll.SetCellForRowInTableView(CellForRowInTableView);
        }

        // 初始化圈子战绩 TableView
        if (CircleRecordScroll != null)
        {
            CircleRecordScroll.SetNumberOfRowsForTableView(tv => recordDataCacheForTableView.Count);
            CircleRecordScroll.SetHeightForRowInTableView((tv, row) => GetRecordItemHeight());
            CircleRecordScroll.SetCellForRowInTableView(CellForRowInTableView);
        }

        // 初始化成员战绩 TableView
        if (MemberRecordScroll != null)
        {
            MemberRecordScroll.SetNumberOfRowsForTableView(tv => GetTeamRecordCount());
            MemberRecordScroll.SetHeightForRowInTableView((tv, row) => GetMemberRecordItemHeight());
            MemberRecordScroll.SetCellForRowInTableView(CellForRowInTableView);
        }

        // 初始化团队统计 TableView
        if (TeamStatsScroll != null)
        {
            TeamStatsScroll.SetNumberOfRowsForTableView(tv => GetTeamRecordCount());
            TeamStatsScroll.SetHeightForRowInTableView((tv, row) => GetRecordItemHeight());
            TeamStatsScroll.SetCellForRowInTableView(CellForRowInTableView);
        }
    }

    /// <summary>
    /// 获取战绩Item的高度
    /// </summary>
    private float GetRecordItemHeight()
    {
        if (varRqecordItem != null)
        {
            var rectTransform = varRqecordItem.GetComponent<RectTransform>();
            return rectTransform != null ? rectTransform.rect.height : 200f;
        }
        return 200f;
    }

    /// <summary>
    /// 获取成员战绩Item的高度
    /// </summary>
    private float GetMemberRecordItemHeight()
    {
        if (MemberRecordPlayerRqecorditem != null)
        {
            var rectTransform = MemberRecordPlayerRqecorditem.GetComponent<RectTransform>();
            return rectTransform != null ? rectTransform.rect.height : 120f;
        }
        return 120f;
    }

    /// <summary>
    /// 获取团队战绩数据数量
    /// </summary>
    private int GetTeamRecordCount()
    {
        return teamRecordDataCacheForTableView.Count;
    }

    /// <summary>
    /// 使用 TableView 加载战绩数据
    /// </summary>
    private void LoadRecordsWithTableView(List<MJMyRecord> records)
    {
        // 获取当前使用的 TableView
        TableView targetTableView = GetCurrentTableView();
        if (targetTableView == null)
        {
            GF.LogError($"[战绩系统] 未找到对应的 TableView 组件，当前面板: {currentPanel?.name}");
            return;
        }
        if (records == null || records.Count == 0)
        {
            recordDataCacheForTableView.Clear();
            targetTableView.ReloadData();
            targetTableView.ScrollY = 0;
            return;
        }
        // 缓存战绩数据供回调使用
        recordDataCacheForTableView = records;
        GF.LogInfo_wl($"[战绩系统] 使用 TableView 加载 {records.Count} 条战绩记录");
        // 先刷新数据，再重置滚动位置，确保立即显示
        targetTableView.ReloadData();
        targetTableView.ScrollY = 0;
        // 强制刷新TableView布局，解决切换界面后需要手动滑动才显示的问题
        StartCoroutine(ForceRefreshTableView(targetTableView));
    }
    /// <summary>
    /// 使用 TableView 加载团队战绩数据（成员战绩/团队统计）
    /// </summary>
    private void LoadTeamRecordsWithTableView(List<MJTeamRecord> records)
    {
        // 获取当前使用的 TableView
        TableView targetTableView = GetCurrentTableView();
        if (targetTableView == null)
        {
            return;
        }
        if (records == null || records.Count == 0)
        {
            teamRecordDataCacheForTableView.Clear();
            targetTableView.ReloadData();
            targetTableView.ScrollY = 0;
            return;
        }
        // 缓存团队战绩数据供回调使用
        teamRecordDataCacheForTableView = records;
        GF.LogInfo_wl($"[战绩系统] 使用 TableView 加载 {records.Count} 条团队战绩记录");
        // 先刷新数据，再重置滚动位置，确保立即显示
        targetTableView.ReloadData();
        targetTableView.ScrollY = 0;
        // 强制刷新TableView布局，解决切换界面后需要手动滑动才显示的问题
        StartCoroutine(ForceRefreshTableView(targetTableView));
    }

    /// <summary>
    /// 获取当前面板对应的 TableView
    /// </summary>
    private TableView GetCurrentTableView()
    {
        if (currentPanel == varMyRqecord)
            return MyRqecordScroll;
        else if (currentPanel == varCircleRecord)
            return CircleRecordScroll;
        else if (currentPanel == varMemberRecord)
            return MemberRecordScroll;
        else if (currentPanel == varTeamStats)
            return TeamStatsScroll;

        return null;
    }

    /// <summary>
    /// 强制刷新 TableView 布局（解决切换界面后需要手动滑动才显示的问题）
    /// </summary>
    private IEnumerator ForceRefreshTableView(TableView tableView)
    {
        if (tableView == null) yield break;

        // 等待一帧，确保布局已经更新
        yield return null;

        // 强制重新加载数据
        tableView.ReloadData();

        // 再次确保滚动位置在顶部
        tableView.ScrollY = 0;

        GF.LogInfo_wl($"[战绩系统] TableView 强制刷新完成");
    }

    /// <summary>
    /// TableView 单元格回调
    /// </summary>
    private TableViewCell CellForRowInTableView(TableView tv, int row)
    {
        // 根据当前面板类型判断使用哪种数据源
        bool isTeamRecord = (currentPanel == varMemberRecord || currentPanel == varTeamStats);

        TableViewCell cell = tv.GetReusableCell("recordCell");
        if (cell == null)
        {
            GameObject prefab = varRqecordItem;

            // 如果是成员战绩面板，使用专用预制体
            if (currentPanel == varMemberRecord && MemberRecordPlayerRqecorditem != null)
            {
                prefab = MemberRecordPlayerRqecorditem;
            }

            // 确保预制体不为空
            if (prefab == null)
            {
                GF.LogError($"[战绩系统] 预制体为空，无法创建单元格，当前面板: {currentPanel?.name}");
                // 创建一个临时的空单元格避免崩溃
                GameObject emptyGo = new GameObject("EmptyCell");
                cell = emptyGo.AddComponent<TableViewCell>();
                cell.reuseIdentifier = "recordCell";
                return cell;
            }

            // TableView 会自动管理父节点，不需要手动指定 container
            GameObject go = Instantiate(prefab);
            cell = go.GetComponent<TableViewCell>();
            if (cell == null)
            {
                cell = go.AddComponent<TableViewCell>();
            }
            cell.name = "recordCell";
            cell.reuseIdentifier = "recordCell";

            GF.LogInfo_wl($"[战绩系统] 创建新单元格，面板: {currentPanel?.name}, 预制体: {prefab.name}");
        }

        cell.gameObject.SetActive(true);

        // 填充数据
        if (isTeamRecord)
        {
            // 填充团队战绩数据（成员战绩/团队统计）
            if (row >= 0 && row < teamRecordDataCacheForTableView.Count)
            {
                PopulateTeamRecordItemForTableView(cell.gameObject, teamRecordDataCacheForTableView[row], row);
            }
        }
        else
        {
            // 填充我的战绩/圈子战绩数据
            if (row >= 0 && row < recordDataCacheForTableView.Count)
            {
                PopulateRecordItemForTableView(cell.gameObject, recordDataCacheForTableView[row], row);
            }
        }

        return cell;
    }

    /// <summary>
    /// 为 TableView 填充战绩Item内容
    /// </summary>
    private void PopulateRecordItemForTableView(GameObject item, MJMyRecord record, int index)
    {
        if (item == null || record == null) return;
        // 设置基本信息
        SetRecordBasicInfo(item, record, index);
        // 设置时间
        SetRecordTime(item, record);
        // 添加详情按钮监听
        var detailButton = item.transform.Find("detailsBtn")?.GetComponent<Button>();
        if (detailButton != null)
        {
            detailButton.onClick.RemoveAllListeners();
            detailButton.onClick.AddListener(() =>
            {
                // 【关键】查看自己的战绩详情时，清空被查看玩家ID
                currentViewedPlayerId = 0;

                varRecordDetails.SetActive(!varRecordDetails.activeSelf);
                var varGameName = varRecordDetails.transform.Find("RecordDetailsBg/GameName")?.GetComponent<Text>();
                if (varGameName != null)
                    varGameName.text = $"{record.DeskName}{record.Floor}楼";
                var varRoomNumber = varRecordDetails.transform.Find("RecordDetailsBg/RoomNumber")?.GetComponent<Text>();
                if (varRoomNumber != null)
                    varRoomNumber.text = $"房号：{record.DeskId}";
                // 保存当前记录的玩法类型,用于后续判断回放协议
                currentMethodType = record.MethodType;
                //添加复制战绩按钮事件
                var copyBtn = varRecordDetails.transform.Find("RecordDetailsBg/CopyRecord")?.GetComponent<Button>();
                if (copyBtn != null)
                {
                    copyBtn.onClick.RemoveAllListeners();
                    copyBtn.onClick.AddListener(() =>
                    {
                        // 构建复制内容：名字：id : 总分：
                        System.Text.StringBuilder copyContent = new System.Text.StringBuilder();

                        // 【修复】创建玩家-分数配对并按PlayerId排序，确保与显示顺序一致
                        var playerScorePairs = new List<(NetMsg.BasePlayer player, double score)>();
                        foreach (var player in record.Player)
                        {
                            // 从score中查找该玩家的总分
                            double totalScore = 0;
                            foreach (var scoreInfo in record.Score)
                            {
                                if (scoreInfo.Key == player.PlayerId)
                                {
                                    totalScore = scoreInfo.Val;
                                    break;
                                }
                            }
                            playerScorePairs.Add((player, totalScore));
                        }
                        
                        // 按PlayerId排序
                        var sortedPairs = playerScorePairs.OrderBy(p => p.player.PlayerId).ToList();
                        
                        // 输出排序后的玩家总分
                        foreach (var pair in sortedPairs)
                        {
                            copyContent.AppendLine($"名字：{pair.player.Nick}：Id：{pair.player.PlayerId} : 积分总计：{pair.score:F2}");
                        }

                        // 添加详细的每局战绩
                        if (currentRecordDetail != null && currentRecordDetail.RoundDetail != null && currentRecordDetail.RoundDetail.Count > 0)
                        {
                            copyContent.AppendLine("\n===== 详细战绩 =====");

                            int roundCount = currentRecordDetail.RoundDetail.Count;
                            // 倒序遍历（因为服务器数据是倒序的，最后一局在前面）
                            for (int i = roundCount - 1; i >= 0; i--)
                            {
                                int roundNumber = roundCount - i; // 实际局号
                                var roundDetail = currentRecordDetail.RoundDetail[i];

                                copyContent.AppendLine($"\n第{roundNumber}局：");

                                // 遍历该局的所有玩家分数
                                if (roundDetail.Score != null)
                                {
                                    // 【修复】创建该局的玩家-分数配对并按PlayerId排序
                                    var roundPlayerScorePairs = new List<(NetMsg.BasePlayer player, double score)>();
                                    foreach (var player in currentRecordDetail.BasePlayer)
                                    {
                                        double roundScore = 0;
                                        var scoreInfo = roundDetail.Score.FirstOrDefault(s => s.Key == player.PlayerId);
                                        if (scoreInfo != null)
                                        {
                                            roundScore = scoreInfo.Val;
                                        }
                                        roundPlayerScorePairs.Add((player, roundScore));
                                    }
                                    
                                    // 按PlayerId排序
                                    var sortedRoundPairs = roundPlayerScorePairs.OrderBy(p => p.player.PlayerId).ToList();
                                    
                                    // 输出排序后的该局玩家分数
                                    foreach (var pair in sortedRoundPairs)
                                    {
                                        copyContent.AppendLine($"  {pair.player.Nick}：{pair.score:+0.##;-0.##;0}");
                                    }
                                }
                            }
                        }

                        // 复制到剪贴板
                        GUIUtility.systemCopyBuffer = copyContent.ToString();
                        GF.UI.ShowToast("战绩已复制到剪贴板");
                    });
                }
                // 请求我的战绩详情
                Msg_MJMYRecordDetailRq req = MessagePool.Instance.Fetch<Msg_MJMYRecordDetailRq>();
                req.LogId = record.LogId;
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(
                    HotfixNetworkComponent.AccountClientName,
                    MessageID.Msg_MJMYRecordDetailRq,
                    req);
            });
        }
        // 使用延迟加载玩家列表，避免大量数据时UI卡顿
        // 先清空玩家列表容器
        var varPlayerListConent = item.transform.Find("PlayerListConent");
        if (varPlayerListConent != null)
        {
            ClearContent(varPlayerListConent.gameObject);
            // 延迟加载，避免同时创建过多GameObject
            StartCoroutine(DelayLoadPlayerListForTableView(item, record));
        }
    }

    /// <summary>
    /// 延迟加载TableView中的玩家列表
    /// </summary>
    private System.Collections.IEnumerator DelayLoadPlayerListForTableView(GameObject item, NetMsg.MJMyRecord record)
    {
        // 等待一帧，避免同时加载过多内容
        yield return null;

        // 检查item是否还存在（可能已被回收）
        if (item != null && item.activeInHierarchy)
        {
            LoadPlayerList(item, record);
        }
    }

    /// <summary>
    /// 为 TableView 填充团队战绩Item内容（成员战绩/团队统计）
    /// </summary>
    private void PopulateTeamRecordItemForTableView(GameObject item, MJTeamRecord record, int index)
    {
        if (item == null || record == null) return;

        if (currentPanel == varMemberRecord)
        {
            // 成员战绩专用字段映射
            SetTextIfExists(item, "index", (index + 1).ToString());
            SetTextIfExists(item, "Name", record.BasePlayer.Nick);
            SetTextIfExists(item, "Id", record.BasePlayer.PlayerId.ToString());
            var headImage = item.transform.Find("head")?.GetComponent<RawImage>();
            if (headImage != null)
            {
                Util.DownloadHeadImage(headImage, record.BasePlayer.HeadImage);
            }

            var overallRecordText = item.transform.Find("overallRecord")?.GetComponent<Text>();
            if (overallRecordText != null)
            {
                overallRecordText.FormatRichText((float)record.TotalRecord);
            }

            SetTextIfExists(item, "cardNum", record.Round.ToString());
            SetTextIfExists(item, "lowScore", record.LowRound.ToString());
            SetTextIfExists(item, "bigWinNum", record.BigWinner.ToString());
            //详情按钮
            var detailButton = item.transform.Find("detailsBtn")?.GetComponent<Button>();
            if (detailButton != null)
            {
                detailButton.onClick.RemoveAllListeners();
                detailButton.onClick.AddListener(() =>
                {
                    // 显示成员战绩详情面板
                    MemberRecordDetails.SetActive(true);
                    // 这里可以添加加载详情数据的逻辑
                    Text overallRecordText = MemberRecordDetails.transform.Find("RecordDetailsBg/overallRecord/overallRecordNum")?.GetComponent<Text>();
                    overallRecordText.text = $"{record.TotalRecord:F2}";
                    Text bigWinner = MemberRecordDetails.transform.Find("RecordDetailsBg/bigWinner/bigWinnerNum")?.GetComponent<Text>();
                    bigWinner.text = $"{record.BigWinner}次";
                    //头像
                    RawImage headImageDetail = MemberRecordDetails.transform.Find("RecordDetailsBg/head")?.GetComponent<RawImage>();
                    Util.DownloadHeadImage(headImageDetail, record.BasePlayer.HeadImage);
                    //ID
                    Text idText = MemberRecordDetails.transform.Find("RecordDetailsBg/Id")?.GetComponent<Text>();
                    idText.text = $"ID:{record.BasePlayer.PlayerId}";
                    //名字
                    Text nameText = MemberRecordDetails.transform.Find("RecordDetailsBg/Name")?.GetComponent<Text>();
                    nameText.FormatNickname(record.BasePlayer.Nick);

                    // 清空成员战绩详情列表容器
                    if (MemberRecordDetailsConent != null)
                    {
                        ClearContent(MemberRecordDetailsConent);
                    }

                    // 【关键】保存当前查看的成员玩家ID（用于回放时传递）
                    currentViewedPlayerId = record.BasePlayer.PlayerId;

                    // 请求该成员的战绩详情
                    Msg_MJMYRecordRq req = MessagePool.Instance.Fetch<Msg_MJMYRecordRq>();
                    req.StartTime = ConvertHourToTimestamp(memberRecordStartTime);
                    req.EndTime = ConvertHourToTimestamp(memberRecordEndTime);
                    req.Floor = chooseFloor;
                    req.UserId = (int)record.BasePlayer.PlayerId;  // 使用成员的玩家ID
                    req.ClubId = (int)GlobalManager.GetInstance().LeagueInfo.LeagueId;

                    GF.LogInfo_wl($"[成员战绩详情] 请求玩家 {record.BasePlayer.Nick}(ID:{record.BasePlayer.PlayerId}) 的战绩");

                    HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(
                        HotfixNetworkComponent.AccountClientName,
                        MessageID.Msg_MJMYRecordRq,
                        req);
                });
            }
        }
        else
        {
            // 团队统计或其他面板的默认映射
            SetTextIfExists(item, "Name", record.BasePlayer.Nick);
            SetTextIfExists(item, "ID", $"ID:{record.BasePlayer.PlayerId}");

            var headImage = item.transform.Find("head")?.GetComponent<RawImage>();
            if (headImage != null)
            {
                Util.DownloadHeadImage(headImage, record.BasePlayer.HeadImage);
            }

            SetPlayerScore(item, record.TotalRecord);

            var bigWinImage = item.transform.Find("BigWin")?.GetComponent<Image>();
            if (bigWinImage != null)
            {
                bigWinImage.gameObject.SetActive(record.BigWinner > 0);
            }

            var roundText = item.transform.Find("RoundText")?.GetComponent<Text>();
            if (roundText != null)
            {
                roundText.text = $"局数:{record.Round}";
            }
        }
    }

    /// <summary>
    /// 加载成员战绩详情列表
    /// </summary>
    private void LoadMemberRecordDetails(Msg_MJMYRecordRs ack)
    {
        if (MemberRecordDetailsConent == null)
        {
            GF.LogError("[成员战绩详情] MemberRecordDetailsConent 容器未设置");
            return;
        }
        // 清空现有列表
        ClearContent(MemberRecordDetailsConent);
        if (ack.Record == null || ack.Record.Count == 0)
        {
            return;
        }

        // 根据筛选状态过滤数据
        List<NetMsg.MJMyRecord> filteredRecords = new List<NetMsg.MJMyRecord>();

        if (filterBigWinnerOnly)
        {
            // 只显示该成员是大赢家的记录
            foreach (var record in ack.Record)
            {
                // 检查该成员在这局中是否是大赢家（分数最高）
                if (IsBigWinnerInRecord(record, currentViewedPlayerId))
                {
                    filteredRecords.Add(record);
                }
            }
            GF.LogInfo_wl($"[成员战绩详情] 筛选大赢家记录：{filteredRecords.Count}/{ack.Record.Count}");
        }
        else
        {
            // 显示所有记录
            filteredRecords.AddRange(ack.Record);
        }

        // 遍历战绩记录，生成列表项
        for (int i = 0; i < filteredRecords.Count; i++)
        {
            var record = filteredRecords[i];
            // 使用 varRqecordItem 作为模板（与我的战绩相同的样式）
            if (varRqecordItem != null)
            {
                var item = Instantiate(varRqecordItem, MemberRecordDetailsConent.transform);
                item.SetActive(true);

                // 填充战绩数据
                PopulateMemberDetailRecordItem(item, record, i);
            }
        }

        GF.LogInfo_wl($"[成员战绩详情] 成功生成 {filteredRecords.Count} 条战绩列表项");
    }
    /// <summary>
    /// 判断指定玩家在该局中是否为大赢家（分数最高）
    /// </summary>
    private bool IsBigWinnerInRecord(NetMsg.MJMyRecord record, long playerId)
    {
        if (record == null || record.Score == null || record.Score.Count == 0)
        {
            return false;
        }

        // 找到最高分数
        double maxScore = double.MinValue;
        long bigWinnerId = 0;

        foreach (var scoreInfo in record.Score)
        {
            if (scoreInfo.Val > maxScore)
            {
                maxScore = scoreInfo.Val;
                bigWinnerId = scoreInfo.Key;
            }
        }

        // 判断是否是当前查看的玩家
        return bigWinnerId == playerId;
    }

    /// <summary>
    /// 填充成员战绩详情单条数据
    /// </summary>
    private void PopulateMemberDetailRecordItem(GameObject item, NetMsg.MJMyRecord record, int index)
    {
        if (item == null || record == null) return;

        // 设置基本信息（复用现有方法）
        SetRecordBasicInfo(item, record, index);

        // 设置时间
        SetRecordTime(item, record);

        // 加载玩家列表
        LoadPlayerList(item, record);

        // 添加详情按钮监听（查看每局详细战绩）
        var detailButton = item.transform.Find("detailsBtn")?.GetComponent<Button>();
        if (detailButton != null)
        {
            detailButton.onClick.RemoveAllListeners();
            detailButton.onClick.AddListener(() =>
            {
                // 【关键】从成员战绩详情查看每局详情时，保持当前被查看玩家ID（不清空）

                // 显示战绩详情（与我的战绩相同的逻辑）
                varRecordDetails.SetActive(!varRecordDetails.activeSelf);

                var varGameName = varRecordDetails.transform.Find("RecordDetailsBg/GameName")?.GetComponent<Text>();
                if (varGameName != null)
                    varGameName.text = $"{record.DeskName}{record.Floor}楼";

                var varRoomNumber = varRecordDetails.transform.Find("RecordDetailsBg/RoomNumber")?.GetComponent<Text>();
                if (varRoomNumber != null)
                    varRoomNumber.text = $"房号：{record.DeskId}";

                // 保存当前记录的玩法类型
                currentMethodType = record.MethodType;

                // 请求战绩详情
                Msg_MJMYRecordDetailRq req = MessagePool.Instance.Fetch<Msg_MJMYRecordDetailRq>();
                req.LogId = record.LogId;
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(
                    HotfixNetworkComponent.AccountClientName,
                    MessageID.Msg_MJMYRecordDetailRq,
                    req);
            });
        }
    }

    /// <summary>
    /// 切换成员战绩详情筛选模式
    /// </summary>
    /// <param name="bigWinnerOnly">true-只显示大赢家记录，false-显示所有记录</param>
    private void ToggleMemberRecordFilter(bool bigWinnerOnly)
    {
        // 更新筛选状态
        filterBigWinnerOnly = bigWinnerOnly;

        // 更新按钮选中状态
        if (duijuXQ_YES != null)
        {
            duijuXQ_YES.SetActive(!bigWinnerOnly);
        }

        if (dayingjia_YES != null)
        {
            dayingjia_YES.SetActive(bigWinnerOnly);
        }

        // 重新加载成员战绩详情列表（应用筛选）
        RefreshMemberRecordDetails();

        GF.LogInfo_wl($"[成员战绩详情] 筛选模式切换为：{(bigWinnerOnly ? "大赢家" : "所有对局")}");
    }

    /// <summary>
    /// 刷新成员战绩详情列表（重新请求数据）
    /// </summary>
    private void RefreshMemberRecordDetails()
    {
        // 如果当前没有查看成员战绩详情，则不处理
        if (!MemberRecordDetails.activeSelf || currentViewedPlayerId == 0)
        {
            return;
        }

        // 重新请求当前成员的战绩详情
        Msg_MJMYRecordRq req = MessagePool.Instance.Fetch<Msg_MJMYRecordRq>();
        req.StartTime = ConvertHourToTimestamp(memberRecordStartTime);
        req.EndTime = ConvertHourToTimestamp(memberRecordEndTime);
        req.Floor = chooseFloor;
        req.UserId = (int)currentViewedPlayerId;
        req.ClubId = (int)GlobalManager.GetInstance().LeagueInfo.LeagueId;

        GF.LogInfo_wl($"[成员战绩详情] 刷新战绩列表 (筛选:{(filterBigWinnerOnly ? "大赢家" : "所有")})");

        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(
            HotfixNetworkComponent.AccountClientName,
            MessageID.Msg_MJMYRecordRq,
            req);
    }
}
    #endregion
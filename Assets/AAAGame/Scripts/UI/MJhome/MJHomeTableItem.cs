using UnityEngine;
using NetMsg;
using UnityEngine.UI;
using System;

/// <summary>
/// 麻将大厅桌子条目：显示桌子信息，并复用玩家头像项以降低 Instantiate/Destroy 频率，减少 GC 与卡顿。
/// </summary>
public class MJHomeTableItem : MonoBehaviour
{
    /// <summary>
    /// 玩家头像项的预制体（包含 <see cref="MjPlayer"/> 组件）
    /// </summary>
    public GameObject headPrefab;
    /// <summary>
    /// 桌子名称文本
    /// </summary>
    public Text tableNameTxt;

    /// <summary>
    /// 桌子状态文本（游戏中/玩法名称）
    /// </summary>
    public Text stateText;

    /// <summary>
    /// 局数信息文本（第X局/总Y局 或 X人Y局）
    /// </summary>
    public Text gameNumText;

    public Sprite[] sprites;
    /// <summary>
    /// 复用的玩家项缓存列表（隐藏/激活切换，而非销毁/创建）
    /// </summary>
    private readonly System.Collections.Generic.List<MjPlayer> playerItems = new System.Collections.Generic.List<MjPlayer>(4);
    /// <summary>
    /// 缓存的背景图组件（避免重复 GetComponent）
    /// </summary>
    private Image backgroundImage;
    /// <summary>
    /// 缓存的按钮组件（仅绑定一次点击回调）
    /// </summary>
    private Button selfButton;
    /// <summary>
    /// 当前条目对应的桌子 ID（点击进入时使用）
    /// </summary>
    private int currentDeskId = 0;
    /// <summary>
    /// 缓存的当前桌子信息（用于点击时的满员判断等）
    /// </summary>
    private Msg_MJDeskInfo currentMjDesk;
    // 4人  1.-150 120 2.150 120 3.-150 0 4.150 0
    //3人 0 150 ,  -150 0 , 150 0
    // 2人  -150 120 150 0

    Vector3[] positions4 = new Vector3[] {
        new Vector3(-150, 120, 0),
        new Vector3(150, 120, 0),
        new Vector3(-150, -25, 0),
        new Vector3(150, -25, 0)
        };
    Vector3[] positions3 = new Vector3[] {
        new Vector3(0, 150, 0),
        new Vector3(-150, -25, 0),
        new Vector3(150, -25, 0)
        };
    Vector3[] positions2 = new Vector3[] {
        new Vector3(-150, 120, 0),
        new Vector3(150, -25, 0)
        };
    // You can initialize pos here if needed, e.g. pos = transform.position;


    /// <summary>
    /// 缓存必要组件并注册一次性点击事件，避免后续重复分配委托
    /// </summary>
    void Awake()
    {
        backgroundImage = GetComponent<Image>();
        selfButton = GetComponent<Button>();
        selfButton.onClick.AddListener(OnClickEnter);

    }

    /// <summary>
    /// 退出时移除事件，避免潜在引用保留
    /// </summary>
    void OnDestroy()
    {
        if (selfButton != null)
        {
            selfButton.onClick.RemoveListener(OnClickEnter);
        }
    }

    /// <summary>
    /// 初始化/刷新条目显示（可多次调用）。
    /// - 根据人数切换底图与玩家头像的布局位置
    /// - 复用已有头像项，不再销毁重建
    /// - 优化：支持从桌子信息直接获取配置，无需依赖楼层信息
    /// </summary>
    /// <param name="mjDesk">房间桌子信息（包含玩家列表与桌子 ID）</param>
    /// <param name="floorInfo">层级/规则信息（可为null，此时从桌子信息获取配置）</param>
    public void Init(Msg_MJDeskInfo mjDesk, Msg_Floor floorInfo)
    {
        if (mjDesk == null)
        {
            // 桌子信息为空时，隐藏整个桌子项
            HideAllPlayerItems();
            if (tableNameTxt != null) tableNameTxt.text = "";
            if (stateText != null) stateText.text = "";
            if (gameNumText != null) gameNumText.text = "";
            currentDeskId = 0;

            // 隐藏整个桌子GameObject，这样在大厅分组时没有数据的桌子就不会显示
            gameObject.SetActive(false);
            return;
        }
        // 有桌子数据时，确保GameObject是激活的
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }

        // 获取玩家人数：优先从桌子信息获取，楼层信息为备选
        long playerNum = GetPlayerNumber(mjDesk, floorInfo);

        // 获取桌子名称：优先从桌子信息获取，楼层信息为备选  
        string deskName = GetDeskName(mjDesk, floorInfo);

        // GF.LogInfo_wl($"桌子 {mjDesk.DeskInfo?.DeskId} 初始化 - 玩家人数: {playerNum}, 桌子名称: {deskName}");

        Vector3[] positions = GetPositionsByPlayerNum(playerNum);

        if (positions == null)
        {
            GF.LogError($"不支持的玩家人数: {playerNum}");
            return;
        }
        int playerCount = (mjDesk.Player != null) ? mjDesk.Player.Count : 0;
        int displayCount = Mathf.Min(playerCount, positions.Length);

        // 确保有足够的玩家项
        for (int i = playerItems.Count; i < displayCount; i++)
        {
            if (headPrefab == null)
            {
                GF.LogError("MJHomeTableItem: headPrefab is missing!");
                break;
            }
            var go = Instantiate(headPrefab, transform);
            go.name = "player";
            var item = go.GetComponent<MjPlayer>();
            if (item != null)
            {
                playerItems.Add(item);
            }
            else
            {
                GF.LogError("MJHomeTableItem: headPrefab is missing MjPlayer component!");
                Destroy(go);
            }
        }

        // 重新计算实际可显示的项（考虑到实例化可能失败）
        int actualDisplayCount = Mathf.Min(displayCount, playerItems.Count);

        // 激活并初始化需要的玩家项
        for (int i = 0; i < actualDisplayCount; i++)
        {
            var item = playerItems[i];
            if (item == null) continue;

            var tr = item.transform;
            if (!tr.gameObject.activeSelf) tr.gameObject.SetActive(true);
            tr.localPosition = positions[i];
            item.Init(mjDesk.Player[i]);
        }

        // 隐藏未使用的玩家项
        for (int i = actualDisplayCount; i < playerItems.Count; i++)
        {
            if (playerItems[i] == null) continue;
            var tr = playerItems[i].transform;
            if (tr.gameObject.activeSelf) tr.gameObject.SetActive(false);
        }
        // 设置桌子名称、底分和局数信息
        float baseCoin = GetBaseCoin(mjDesk, floorInfo);
        if (tableNameTxt != null)
        {
            if (baseCoin > 0)
            {
                tableNameTxt.text = $"{deskName}/{baseCoin}分";
            }
            else
            {
                tableNameTxt.text = deskName;
            }
        }

        // 设置状态信息
        SetStateDisplay(mjDesk, floorInfo);

        // 设置局数信息
        SetGameNumDisplay(mjDesk, floorInfo);

        // 缓存当前桌子信息和ID，用于点击事件处理
        currentMjDesk = mjDesk;
        currentDeskId = mjDesk.DeskInfo?.DeskId ?? 0;
    }
    /// <summary>
    /// 点击进入桌子
    /// </summary>
    private void OnClickEnter()
    {
        // 防止重复点击（0.2秒内只能点击一次）
        if (Util.IsClickLocked())
        {
            return;
        }

        if (currentDeskId == 0)
        {
            GF.UI.ShowToast("当前桌子不存在");
            return;
        }

        //判断当前是否是俱乐部、联盟或超盟 (0:俱乐部, 1:联盟, 2:超盟)
        int leagueType = GlobalManager.GetInstance().LeagueInfo.Type;
        if (leagueType != 0 && leagueType != 1 && leagueType != 2)
        {
            return;
        }

        //判断当前房间是否满员 如果满员弹窗提示当前房间人数已满 (如果是创建者，则不判断满员)
        bool isCreator = currentMjDesk?.DeskInfo?.BaseConfig != null && Util.IsMySelf(currentMjDesk.DeskInfo.BaseConfig.Creator);
        if (IsRoomFull() && !isCreator)
        {
            GF.UI.ShowToast("当前房间人数已满");
            return;
        }

        // 添加详细的调试日志
        string gameType = "未知";
        if (currentMjDesk?.DeskInfo != null)
        {
            MethodType methodType = currentMjDesk.DeskInfo.MethodType;
            gameType = methodType == MethodType.RunFast ? "跑的快" :
                       methodType == MethodType.MjSimple ? "麻将" :
                       methodType.ToString();
        }
        GF.LogInfo_wl($"==========【点击进入桌子】==========");
        GF.LogInfo_wl($"[进入桌子] 桌号: {currentDeskId}, 游戏类型: {gameType}");
        if (currentMjDesk?.DeskInfo != null)
        {
            GF.LogInfo_wl($"[进入桌子] 桌子状态: {currentMjDesk.DeskInfo.State}");
            GF.LogInfo_wl($"[进入桌子] 当前玩家数: {currentMjDesk.Player?.Count ?? 0}");
        }

        Util.GetInstance().OpenMJConfirmationDialog("加入房间", "确认进入桌子吗？",
            // 确认回调
            () =>
            {
                // 保存进入桌子前的亲友圈房间ID
                var leagueInfo = GlobalManager.GetInstance().LeagueInfo;
                if (leagueInfo != null && leagueInfo.LeagueId > 0)
                {
                    GlobalManager.GetInstance().SaveBeforeEnterDeskLeagueId(leagueInfo.LeagueId);
                    GF.LogInfo($"[进入桌子] 保存亲友圈房间ID: {leagueInfo.LeagueId}");
                }
                
                Util.GetInstance().Send_EnterDeskRq(currentDeskId, GlobalManager.GetInstance().LeagueInfo.LeagueId);
            });
    }

    /// <summary>
    /// 获取玩家人数：优先从桌子信息获取，楼层信息为备选
    /// </summary>
    private long GetPlayerNumber(Msg_MJDeskInfo mjDesk, Msg_Floor floorInfo)
    {
        // 优先从桌子信息获取玩家人数
        if (mjDesk?.DeskInfo?.BaseConfig != null)
        {
            return mjDesk.DeskInfo.BaseConfig.PlayerNum;
        }

        // 备选：从楼层信息获取
        if (floorInfo?.Config != null)
        {
            return floorInfo.Config.PlayerNum;
        }

        // 默认4人桌
        GF.LogWarning("无法获取玩家人数信息，使用默认值4");
        return 4;
    }

    /// <summary>
    /// 获取桌子名称：优先从桌子信息获取，楼层信息为备选
    /// </summary>
    private string GetDeskName(Msg_MJDeskInfo mjDesk, Msg_Floor floorInfo)
    {
        // 优先从桌子信息获取桌子名称
        if (!string.IsNullOrEmpty(mjDesk?.DeskInfo?.BaseConfig?.DeskName))
        {
            //GF.LogInfo_wl("从桌子信息获取桌子名称: " + mjDesk.DeskInfo.BaseConfig.DeskName);
            // GF.LogInfo_wl("从楼层信息获取桌子名称: " + floorInfo.Config.DeskName);
            return mjDesk.DeskInfo.BaseConfig.DeskName;
        }
        // 备选：从楼层信息获取
        if (!string.IsNullOrEmpty(floorInfo?.Config?.DeskName))
        {
            // GF.LogInfo_wl("从桌子信息获取桌子名称: " + mjDesk.DeskInfo.BaseConfig.DeskName);
            // GF.LogInfo_wl("从楼层信息获取桌子名称: " + floorInfo.Config.DeskName);
            return floorInfo.Config.DeskName;
        }

        // 默认名称
        return "加载中...";
    }

    /// <summary>
    /// 根据玩家人数获取位置数组和设置背景
    /// </summary>
    private Vector3[] GetPositionsByPlayerNum(long playerNum)
    {
        // 确保 backgroundImage 已初始化，防止 Awake 未及时调用
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        // 预检查 sprites 以防止 NullReferenceException 或 IndexOutOfRangeException
        bool hasSprites = sprites != null && sprites.Length >= 3;
        if (!hasSprites)
        {
            GF.LogWarning($"MJHomeTableItem: sprites 数组未正确配置（需至少3个元素）。当前已忽略背景切换。");
        }

        switch (playerNum)
        {
            case 4:
                if (backgroundImage != null && hasSprites) backgroundImage.sprite = sprites[2];
                return positions4;
            case 3:
                if (backgroundImage != null && hasSprites) backgroundImage.sprite = sprites[1];
                return positions3;
            case 2:
                if (backgroundImage != null && hasSprites) backgroundImage.sprite = sprites[0];
                return positions2;
            default:
                GF.LogError($"不支持的玩家人数: {playerNum}");
                // 默认使用4人桌配置
                if (backgroundImage != null && hasSprites) backgroundImage.sprite = sprites[2];
                return positions4;
        }
    }

    /// <summary>
    /// 隐藏所有玩家项
    /// </summary>
    private void HideAllPlayerItems()
    {
        for (int i = 0; i < playerItems.Count; i++)
        {
            var tr = playerItems[i].transform;
            if (tr.gameObject.activeSelf)
            {
                tr.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 设置桌子状态显示
    /// </summary>
    /// <param name="mjDesk">桌子信息</param>
    /// <param name="floorInfo">楼层信息</param>
    private void SetStateDisplay(Msg_MJDeskInfo mjDesk, Msg_Floor floorInfo)
    {
        if (stateText == null) return;

        // 判断游戏是否开始
        bool isGameStarted = IsGameStarted(mjDesk);

        if (isGameStarted)
        {
            stateText.text = "游戏正在进行中";
        }
        else
        {
            // 显示玩法名称
            string methodName = GetMethodDisplayName(mjDesk, floorInfo);
            stateText.text = methodName;
        }
    }

    /// <summary>
    /// 设置局数信息显示
    /// </summary>
    /// <param name="mjDesk">桌子信息</param>
    /// <param name="floorInfo">楼层信息</param>
    private void SetGameNumDisplay(Msg_MJDeskInfo mjDesk, Msg_Floor floorInfo)
    {
        if (gameNumText == null) return;
        //GF.LogInfo_wl("Msg_MJDeskInfo:" + mjDesk," Msg_Floor:" + floorInfo);
        bool isGameStarted = IsGameStarted(mjDesk);

        if (isGameStarted)
        {
            // 游戏开始了，显示当前 第 局数/总局数 局
            int currentRound = mjDesk.DeskInfo.RoundNum;
            long totalRounds = GetTotalRounds(mjDesk, floorInfo);
            gameNumText.text = $"第{currentRound}/{totalRounds}局";
        }
        else
        {
            // 游戏未开始，显示当前人数/总人数 和 总局数
            int currentCount = mjDesk.Player?.Count ?? 0;
            long playerNum = GetPlayerNumber(mjDesk, floorInfo);
            long totalRounds = GetTotalRounds(mjDesk, floorInfo);
            gameNumText.text = $"{currentCount}/{playerNum}人 {totalRounds}局";
        }
    }

    /// <summary>
    /// 判断游戏是否已开始
    /// </summary>
    /// <param name="mjDesk">桌子信息</param>
    /// <returns>是否已开始</returns>
    private bool IsGameStarted(Msg_MJDeskInfo mjDesk)
    {
        if (mjDesk?.DeskInfo == null)
        {
            return false;
        }

        // 根据桌子状态判断游戏是否开始
        DeskState deskState = mjDesk.DeskInfo.State;

        // 根据DeskState枚举值判断游戏状态
        switch (deskState)
        {
            case DeskState.StartRun:    // 已开始/游戏中
                return true;
            case DeskState.WaitStart:   // 准备中
            case DeskState.Dismiss:     // 解散
            case DeskState.Pause:       // 暂停
            case DeskState.Over:        // 自动结束
            default:
                return false;
        }
    }

    /// <summary>
    /// 获取玩法显示名称
    /// </summary>
    /// <param name="mjDesk">桌子信息</param>
    /// <param name="floorInfo">楼层信息</param>
    /// <returns>玩法名称</returns>
    private string GetMethodDisplayName(Msg_MJDeskInfo mjDesk, Msg_Floor floorInfo)
    {
        // 优先从桌子信息的MethodType判断（支持跑得快等非麻将玩法）
        if (mjDesk?.DeskInfo != null)
        {
            MethodType methodType = mjDesk.DeskInfo.MethodType;

            // 如果是跑得快，直接返回
            if (methodType == MethodType.RunFast)
            {
                return "跑的快";
            }

            // 如果是麻将简单玩法，则继续检查具体的麻将玩法
            if (methodType == MethodType.MjSimple && mjDesk.DeskInfo.MjConfig != null)
            {
                return ConvertMJMethodToDisplayName(mjDesk.DeskInfo.MjConfig.MjMethod);
            }
        }

        // 备选：从楼层信息获取
        if (floorInfo != null)
        {
            MethodType methodType = floorInfo.MethodType;

            if (methodType == MethodType.RunFast)
            {
                return "跑的快";
            }

            if (methodType == MethodType.MjSimple && floorInfo.MjConfig != null)
            {
                return ConvertMJMethodToDisplayName(floorInfo.MjConfig.MjMethod);
            }
        }

        return "麻将";
    }

    /// <summary>
    /// 获取总局数
    /// </summary>
    /// <param name="mjDesk">桌子信息</param>
    /// <param name="floorInfo">楼层信息</param>
    /// <returns>总局数</returns>
    private long GetTotalRounds(Msg_MJDeskInfo mjDesk, Msg_Floor floorInfo)
    {
        // 优先从桌子信息获取总局数
        if (mjDesk?.DeskInfo?.BaseConfig != null)
        {
            return mjDesk.DeskInfo.BaseConfig.PlayerTime;
        }

        // 备选：从楼层信息获取总局数
        if (floorInfo?.Config != null)
        {
            return floorInfo.Config.PlayerTime;
        }

        return 8; // 默认8局
    }

    /// <summary>
    /// 获取底分
    /// </summary>
    /// <param name="mjDesk">桌子信息</param>
    /// <param name="floorInfo">楼层信息</param>
    /// <returns>底分，返回0表示无底分信息</returns>
    private float GetBaseCoin(Msg_MJDeskInfo mjDesk, Msg_Floor floorInfo)
    {
        // 优先从桌子信息获取底分
        if (mjDesk?.DeskInfo?.BaseConfig != null)
        {
            return mjDesk.DeskInfo.BaseConfig.BaseCoin;
        }

        // 备选：从楼层信息获取底分
        if (floorInfo?.Config != null)
        {
            return floorInfo.Config.BaseCoin;
        }

        return 0; // 没有底分信息
    }

    /// <summary>
    /// 将MJMethod枚举转换为显示名称
    /// 注意：此方法仅用于麻将玩法（MethodType.MjSimple），跑得快等其他玩法不使用此枚举
    /// </summary>
    /// <param name="mjMethod">麻将玩法枚举</param>
    /// <returns>显示名称</returns>
    private string ConvertMJMethodToDisplayName(MJMethod mjMethod)
    {
        switch (mjMethod)
        {
            case MJMethod.Dazhong:
                return "大众麻将";
            case MJMethod.Huanghuang:
                return "仙桃晃晃";
            case MJMethod.Kwx:
                return "卡五星";
            case MJMethod.Xl:
                return "血流成河";
            default:
                return mjMethod.ToString();
        }
    }

    /// <summary>
    /// 判断当前房间是否满员
    /// </summary>
    /// <returns>如果满员返回true，否则返回false</returns>
    private bool IsRoomFull()
    {
        if (currentMjDesk?.DeskInfo?.BaseConfig == null || currentMjDesk.Player == null)
        {
            return false;
        }

        // 获取房间配置的最大人数
        long maxPlayers = currentMjDesk.DeskInfo.BaseConfig.PlayerNum;

        // 获取当前实际玩家数量
        int currentPlayers = currentMjDesk.Player.Count;

        // 判断是否满员
        return currentPlayers >= maxPlayers;
    }

}

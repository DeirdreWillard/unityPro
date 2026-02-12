using UnityEngine.UI;
using NetMsg;
using UnityGameFramework.Runtime;
using System.Collections.Generic;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class CreateRoomPanel : UIFormBase
{

    public Toggle togDZ;
    public Toggle togNN;
    public Toggle togZjh;
    public Toggle togBj;

    public MethodType createType = MethodType.TexasPoker;

    public InputField roomName;
    public CreateDZRoom createDZRoom;
    public CreateNNRoom createNNRoom;
    public CreateZJHRoom createZJHRoom;
    public CreateBJRoom createBJRoom;

    public Text textDiamomd;
    public Text textExpend;

    public bool m_IsUpdatingSlider = false;
    public LeagueInfo leagueInfo;

    // 按BaseCoin档位存储不同游戏类型的配置
    private Dictionary<MethodType, Dictionary<float, Msg_CreateDeskRq>> m_GameConfigsMap;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        togDZ.onValueChanged.AddListener(OntogDZValueChanged);
        togNN.onValueChanged.AddListener(OntogNNValueChanged);
        togZjh.onValueChanged.AddListener(OntogZjhValueChanged);
        togBj.onValueChanged.AddListener(OntogBjValueChanged);
        togDZ.isOn = true;
        roomName.text = "";

        Params.TryGet("leagueInfo", out VarByteArray leagueInfoByteArray);
        if (leagueInfoByteArray != null)
        {
            leagueInfo = LeagueInfo.Parser.ParseFrom(leagueInfoByteArray);
        }
        else
        {
            leagueInfo = null;
        }

        HotfixNetworkComponent.AddListener(MessageID.Msg_DefaultCreateDeskRs, Function_DefaultCreateDeskRs);
        Msg_DefaultCreateDeskRq dcc = MessagePool.Instance.Fetch<Msg_DefaultCreateDeskRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_DefaultCreateDeskRq, dcc);

        textDiamomd.text = Util.GetMyselfInfo().Diamonds.ToString();
        Invoke(nameof(InitPanel), 0.3f);
    }

    public void InitPanel(){
        OntogDZValueChanged(togDZ.isOn);
    }

    /// <summary>
    /// 设置开房消耗
    /// </summary>
    /// <param name="expend"></param>
    public void SetExpendText(int expend)
    {
        textExpend.text = expend.ToString();
    }

    private void Function_DefaultCreateDeskRs(MessageRecvData data)
    {
        // Msg_DefaultCreateDeskRs
        Msg_DefaultCreateDeskRs msg_DefaultCreateDeskRs = Msg_DefaultCreateDeskRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到房间创建配置:", msg_DefaultCreateDeskRs.ToString());

        // 初始化配置字典
        m_GameConfigsMap = new Dictionary<MethodType, Dictionary<float, Msg_CreateDeskRq>>();
        m_GameConfigsMap[MethodType.TexasPoker] = new Dictionary<float, Msg_CreateDeskRq>();
        m_GameConfigsMap[MethodType.NiuNiu] = new Dictionary<float, Msg_CreateDeskRq>();
        m_GameConfigsMap[MethodType.GoldenFlower] = new Dictionary<float, Msg_CreateDeskRq>();
        m_GameConfigsMap[MethodType.CompareChicken] = new Dictionary<float, Msg_CreateDeskRq>();

        // 将配置按游戏类型和BaseCoin分类保存
        foreach (var item in msg_DefaultCreateDeskRs.CreateDesk)
        {
            if (!m_GameConfigsMap.ContainsKey(item.MethodType))
            {
                m_GameConfigsMap[item.MethodType] = new Dictionary<float, Msg_CreateDeskRq>();
            }

            m_GameConfigsMap[item.MethodType][item.Config.BaseCoin] = item;
        }

        // 初始加载默认配置（每种游戏类型的最低BaseCoin配置）
        LoadDefaultConfigs(createType, -1);
    }

    /// <summary>
    /// 加载每种游戏类型的默认配置（最低BaseCoin）
    /// </summary>
    public void LoadDefaultConfigs(MethodType methodType, int baseCoinIndex = -1)
    {
        m_IsUpdatingSlider = true;
        // 加载各游戏类型的默认配置
        switch (methodType)
        {
            case MethodType.NiuNiu:
                LoadGameTypeConfig(MethodType.NiuNiu, createNNRoom, baseCoinIndex);
                break;
            case MethodType.TexasPoker:
                LoadGameTypeConfig(MethodType.TexasPoker, createDZRoom, baseCoinIndex);
                break;
            case MethodType.GoldenFlower:
                LoadGameTypeConfig(MethodType.GoldenFlower, createZJHRoom, baseCoinIndex);
                break;
            case MethodType.CompareChicken:
                LoadGameTypeConfig(MethodType.CompareChicken, createBJRoom, baseCoinIndex);
                break;
        }
        UpdateCreateRoomName();
        m_IsUpdatingSlider = false;
    }

    /// <summary>
    /// 根据游戏类型和BaseCoin加载对应配置
    /// </summary>
    /// <param name="methodType">游戏类型</param>
    /// <param name="gameRoom">对应的房间组件</param>
    /// <param name="baseCoinIndex">底分索引，如果为-1则加载默认（最低）档位  -2 不变化</param>
    public void LoadGameTypeConfig(MethodType methodType, object gameRoom, int baseCoinIndex = -1)
    {
        if(m_GameConfigsMap == null) return;
        if (!m_GameConfigsMap.ContainsKey(methodType) ||
            m_GameConfigsMap[methodType].Count == 0)
        {
            if(baseCoinIndex != -1) return;
            // 没有配置，使用默认值
            switch (methodType)
            {
                case MethodType.NiuNiu:
                    ((CreateNNRoom)gameRoom).ApplyDeskConfigToUI(null);
                    break;
                case MethodType.TexasPoker:
                    ((CreateDZRoom)gameRoom).ApplyDeskConfigToUI(null);
                    break;
                case MethodType.GoldenFlower:
                    ((CreateZJHRoom)gameRoom).ApplyDeskConfigToUI(null);
                    break;
                case MethodType.CompareChicken:
                    ((CreateBJRoom)gameRoom).ApplyDeskConfigToUI(null);
                    break;
            }
            return;
        }

        // 确定要获取的BaseCoin
        float baseCoin = 0.1f;
        if (baseCoinIndex >= 0)
        {
            // 使用特定档位的BaseCoin
            switch (methodType)
            {
                case MethodType.NiuNiu:
                    baseCoin = ((CreateNNRoom)gameRoom).baseCoins[baseCoinIndex];
                    break;
                case MethodType.TexasPoker:
                    baseCoin = ((CreateDZRoom)gameRoom).MangZhu[baseCoinIndex];
                    break;
                case MethodType.GoldenFlower:
                    baseCoin = ((CreateZJHRoom)gameRoom).baseCoins[baseCoinIndex];
                    break;
                case MethodType.CompareChicken:
                    baseCoin = ((CreateBJRoom)gameRoom).baseCoins[baseCoinIndex];
                    break;
            }
        }
        else
        {
            // 使用最低档位的BaseCoin
            baseCoin = GetLowestBaseCoin(methodType);
        }

        // 尝试获取特定BaseCoin的配置
        if (m_GameConfigsMap[methodType].ContainsKey(baseCoin))
        {
            // 有对应档位的配置，直接加载
            switch (methodType)
            {
                case MethodType.NiuNiu:
                    ((CreateNNRoom)gameRoom).ApplyDeskConfigToUI(m_GameConfigsMap[methodType][baseCoin]);
                    break;
                case MethodType.TexasPoker:
                    ((CreateDZRoom)gameRoom).ApplyDeskConfigToUI(m_GameConfigsMap[methodType][baseCoin]);
                    break;
                case MethodType.GoldenFlower:
                    ((CreateZJHRoom)gameRoom).ApplyDeskConfigToUI(m_GameConfigsMap[methodType][baseCoin]);
                    break;
                case MethodType.CompareChicken:
                    ((CreateBJRoom)gameRoom).ApplyDeskConfigToUI(m_GameConfigsMap[methodType][baseCoin]);
                    break;
            }
        }
    }

    /// <summary>
    /// 获取指定游戏类型的最低BaseCoin值
    /// </summary>
    private float GetLowestBaseCoin(MethodType methodType)
    {
        if (!m_GameConfigsMap.ContainsKey(methodType) || m_GameConfigsMap[methodType].Count == 0)
        {
            return 0.1f; // 默认值
        }

        float lowestBaseCoin = float.MaxValue;
        foreach (var baseCoin in m_GameConfigsMap[methodType].Keys)
        {
            if (baseCoin < lowestBaseCoin)
            {
                lowestBaseCoin = baseCoin;
            }
        }
        return lowestBaseCoin;
    }

    public void UpdateCreateRoomName()
    {
        // 设置房间名
        roomName.text = "";

        // 根据当前游戏类型找到对应配置
        if (m_GameConfigsMap != null && m_GameConfigsMap.ContainsKey(createType))
        {
            // 尝试查找当前选中BaseCoin对应的配置
            float currentBaseCoin = 0.1f; // 默认值

            switch (createType)
            {
                case MethodType.TexasPoker:
                    currentBaseCoin = createDZRoom.MangZhu[(int)createDZRoom.Slider_MangZhu.value];
                    break;
                case MethodType.NiuNiu:
                    currentBaseCoin = createNNRoom.baseCoins[(int)createNNRoom.baseCoinSlider.value];
                    break;
                case MethodType.GoldenFlower:
                    currentBaseCoin = createZJHRoom.baseCoins[(int)createZJHRoom.baseCoinSlider.value];
                    break;
                case MethodType.CompareChicken:
                    currentBaseCoin = createBJRoom.baseCoins[(int)createBJRoom.baseCoinSlider.value];
                    break;
            }

            // 尝试获取对应BaseCoin的配置
            if (m_GameConfigsMap[createType].ContainsKey(currentBaseCoin))
            {
                roomName.text = m_GameConfigsMap[createType][currentBaseCoin].Config.DeskName;
            }
        }
    }

    void OntogDZValueChanged(bool isOn)
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (isOn)
        {
            createType = MethodType.TexasPoker;
            createDZRoom.gameObject.SetActive(true);
            createNNRoom.gameObject.SetActive(false);
            createZJHRoom.gameObject.SetActive(false);
            createBJRoom.gameObject.SetActive(false);
            createDZRoom.UpdateExpend();
            LoadDefaultConfigs(createType, -1);
        }
    }
    void OntogNNValueChanged(bool isOn)
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (isOn)
        {
            createType = MethodType.NiuNiu;
            createDZRoom.gameObject.SetActive(false);
            createNNRoom.gameObject.SetActive(true);
            createZJHRoom.gameObject.SetActive(false);
            createBJRoom.gameObject.SetActive(false);
            createNNRoom.UpdateExpend();
            LoadDefaultConfigs(createType, -1);
        }
    }
    void OntogZjhValueChanged(bool isOn)
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (isOn)
        {
            createType = MethodType.GoldenFlower;
            createDZRoom.gameObject.SetActive(false);
            createNNRoom.gameObject.SetActive(false);
            createZJHRoom.gameObject.SetActive(true);
            createBJRoom.gameObject.SetActive(false);
            createZJHRoom.UpdateExpend();
            LoadDefaultConfigs(createType, -1);
        }
    }

    void OntogBjValueChanged(bool isOn)
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (isOn)
        {
            createType = MethodType.CompareChicken;
            createDZRoom.gameObject.SetActive(false);
            createNNRoom.gameObject.SetActive(false);
            createZJHRoom.gameObject.SetActive(false);
            createBJRoom.gameObject.SetActive(true);
            createBJRoom.UpdateExpend();
            LoadDefaultConfigs(createType, -1);
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        togDZ.onValueChanged.RemoveListener(OntogDZValueChanged);
        togNN.onValueChanged.RemoveListener(OntogNNValueChanged);
        togZjh.onValueChanged.RemoveListener(OntogZjhValueChanged);
        togBj.onValueChanged.RemoveListener(OntogBjValueChanged);
        leagueInfo = null;
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_DefaultCreateDeskRs, Function_DefaultCreateDeskRs);
        // 清理配置字典
        if (m_GameConfigsMap != null)
        {
            m_GameConfigsMap.Clear();
            m_GameConfigsMap = null;
        }
        base.OnClose(isShutdown, userData);
    }

    public void OnClickBtn(string t)
    {
        switch (t)
        {
            case "组队":
               Sound.PlayEffect(AudioKeys.SOUND_BTN);
                ZuDuiFunction();
                break;
        }
    }
    //组队
    public void ZuDuiFunction()
    {
        Msg_CreateDeskRq req = MessagePool.Instance.Fetch<Msg_CreateDeskRq>();
        if (createType == MethodType.TexasPoker)//德州
        {
            Desk_BaseConfig desk_BaseConfig = createDZRoom.GetDesk_BaseConfig();
            if (roomName.text == "")
            {
                GF.UI.ShowToast("房间名不能为空");
                return;
            }
            desk_BaseConfig.DeskName = roomName.text;
            req.MethodType = MethodType.TexasPoker;
            req.Config = desk_BaseConfig;
            req.TexasPokerConfig = createDZRoom.GetDZConfig();
        }
        else if (createType == MethodType.NiuNiu)//牛牛
        {
            Desk_BaseConfig desk_BaseConfig = createNNRoom.GetDesk_BaseConfig();
            if (roomName.text == "")
            {
                GF.UI.ShowToast("房间名不能为空");
                return;
            }
            desk_BaseConfig.DeskName = roomName.text;
            req.MethodType = MethodType.NiuNiu;
            req.Config = desk_BaseConfig;
            req.NiuConfig = createNNRoom.GetNiuNiuConfig();
        }
        else if (createType == MethodType.GoldenFlower)//炸金花
        {
            Desk_BaseConfig desk_BaseConfig = createZJHRoom.GetDesk_BaseConfig();
            if (roomName.text == "")
            {
                GF.UI.ShowToast("房间名不能为空");
                return;
            }
            desk_BaseConfig.DeskName = roomName.text;
            desk_BaseConfig.GpsLimit = createZJHRoom.isGPSToggle.isOn;
            desk_BaseConfig.IpLimit = createZJHRoom.isIPToggle.isOn;
            req.MethodType = MethodType.GoldenFlower;
            req.Config = desk_BaseConfig;
            req.GoldenFlowerConfig = createZJHRoom.GetZJHConfig();
            if (req.GoldenFlowerConfig == null)
            {
                return;
            }
        }
        else if (createType == MethodType.CompareChicken)
        {
            Desk_BaseConfig desk_BaseConfig = createBJRoom.GetDesk_BaseConfig();
            if (roomName.text == "")
            {
                GF.UI.ShowToast("房间名不能为空");
                return;
            }
            desk_BaseConfig.DeskName = roomName.text;
            req.MethodType = MethodType.CompareChicken;
            req.Config = desk_BaseConfig;
            req.CompareChickenConfig = createBJRoom.GetBJConfig();
            if (req.CompareChickenConfig == null)
            {
                return;
            }
        }
        if (leagueInfo != null)
        {
            req.Config.ClubId = (int)leagueInfo.LeagueId;
            switch (leagueInfo.Type)
            {
                case 0:
                    req.Config.DeskType = DeskType.Guild;
                    break;
                case 1:
                    req.Config.DeskType = DeskType.League;
                    break;
                case 2:
                    req.Config.DeskType = DeskType.Super;
                    break;
            }
        }
        else
        {
            req.Config.DeskType = DeskType.Simple;
        }
        HomeProcedure homeProcedure = GF.Procedure.CurrentProcedure as HomeProcedure;
        if (null != homeProcedure && req != null)
        {
            homeProcedure.CreatorRoom(req);
        }
    }

}
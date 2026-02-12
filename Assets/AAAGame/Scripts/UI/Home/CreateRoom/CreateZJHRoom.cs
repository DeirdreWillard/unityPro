using System;
using UnityEngine;
using UnityEngine.UI;

using NetMsg;
public class CreateZJHRoom : MonoBehaviour
{
    public GameObject SliderRoot;
    public GameObject PanelRoot;

    public Slider baseCoinSlider;
    public Text textmang;
    public Text textzuididai;
    public Slider playerTimeSlider;
    public Slider playerNumSlider;
    public Slider minBringInSlider;
    public Slider mustStuffySlider;
    public Slider maxRoundSlider;
    public Slider compareRoundSlider;
    public Slider rateSlider;
    public Slider benSlider;
    public Slider autoStartSlider;

    public float[] baseCoins = { 0.1f, 0.25f, 0.5f, 1, 2, 3, 5, 10, 20, 25, 50, 100, 200, 300, 500, 1000 };
    public long[] playerTimes = { 30, 60, 90, 120, 180, 240, 300, 360 };

    public int[] mustStuffys = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    public int[] maxRounds = { 10, 20, 30, 50, 80, 100, -1 };
    public int[] compareRounds = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    public int[] rates = { 0, 10, 15, 20, 25, 30, 35, 40, 45, 50 };
    public int[] bens = { 1, 10, 20, 30, 40, 50, 60, 70, 80, 100 };

    public Toggle RateTypeToggle;

    public Toggle singleCoinToggle;
    public Toggle singleCoinToggle1;
    public InputField singleCoinInputField;

    public Toggle maxPotToggle;
    public InputField maxPotInputField;

    public Toggle isCompareDoubleToggle;
    public Toggle isMenbiToggle;
    public Toggle isGrantToggle;
    public Toggle isGPSToggle;
    public Toggle isIPToggle;
    public Toggle use235Toggle;
    public Toggle shotCardToggle;
    public Toggle JackpotToggle;
    public Toggle sysShowCardToggle;
    public Toggle bigLuckCardToggle;
    public Toggle 禁止聊天Toggle;
    public Toggle 禁止打字Toggle;

    public CreateRoomPanel createRoomPanel;
    public void Awake()
    {
        if (SliderRoot != null) return;
        SliderRoot = gameObject.transform.Find("Viewport/Content/Slider").gameObject;
        PanelRoot = gameObject.transform.Find("Viewport/Content/Panel").gameObject;
        baseCoinSlider = SliderRoot.transform.Find("baseCoin/baseCoinSlider").GetComponent<Slider>();
        textmang = SliderRoot.transform.Find("baseCoin/textmang").GetComponent<Text>();
        textzuididai = SliderRoot.transform.Find("baseCoin/textzuididai").GetComponent<Text>();
        playerTimeSlider = SliderRoot.transform.Find("playerTime/playerTimeSlider").GetComponent<Slider>();
        playerNumSlider = SliderRoot.transform.Find("playerNum/playerNumSlider").GetComponent<Slider>();
        minBringInSlider = SliderRoot.transform.Find("minBringIn/minBringSlider").GetComponent<Slider>();
        mustStuffySlider = SliderRoot.transform.Find("mustStuffy/mustStuffySlider").GetComponent<Slider>();
        maxRoundSlider = SliderRoot.transform.Find("maxRound/maxRoundSlider").GetComponent<Slider>();
        compareRoundSlider = SliderRoot.transform.Find("compareRound/compareRoundSlider").GetComponent<Slider>();
        rateSlider = SliderRoot.transform.Find("rate/rateSlider").GetComponent<Slider>();
        benSlider = SliderRoot.transform.Find("ben/benSlider").GetComponent<Slider>();
        autoStartSlider = SliderRoot.transform.Find("autoStart/autoStartSlider").GetComponent<Slider>();
        RateTypeToggle = PanelRoot.transform.Find("RateType/RateTypeToggle").GetComponent<Toggle>();
        singleCoinToggle = PanelRoot.transform.Find("singleCoin/singleCoinToggle").GetComponent<Toggle>();
        singleCoinToggle1 = PanelRoot.transform.Find("singleCoin/singleCoinToggle1").GetComponent<Toggle>();
        singleCoinInputField = PanelRoot.transform.Find("singleCoin/singleCoinInputField").GetComponent<InputField>();
        maxPotToggle = PanelRoot.transform.Find("maxPot/maxPotToggle").GetComponent<Toggle>();
        maxPotInputField = PanelRoot.transform.Find("maxPot/maxPotInputField").GetComponent<InputField>();
        isCompareDoubleToggle = PanelRoot.transform.Find("isCompareDouble/isCompareDoubleToggle").GetComponent<Toggle>();
        isMenbiToggle = PanelRoot.transform.Find("isMenbi/isMenbiToggle").GetComponent<Toggle>();
        isGrantToggle = PanelRoot.transform.Find("isGrant/isGrantToggle").GetComponent<Toggle>();
        isIPToggle = PanelRoot.transform.Find("IP/IPToggle").GetComponent<Toggle>();
        isGPSToggle = PanelRoot.transform.Find("GPS/GPSToggle").GetComponent<Toggle>();
        禁止聊天Toggle = PanelRoot.transform.Find("禁止聊天/禁止聊天Toggle").GetComponent<Toggle>();
        禁止打字Toggle = PanelRoot.transform.Find("禁止打字/禁止打字Toggle").GetComponent<Toggle>();
        use235Toggle = PanelRoot.transform.Find("use235/use235Toggle").GetComponent<Toggle>();
        shotCardToggle = PanelRoot.transform.Find("shotCard/shotCardToggle").GetComponent<Toggle>();
        JackpotToggle = PanelRoot.transform.Find("jackpot/JackpotToggle").GetComponent<Toggle>();
        sysShowCardToggle = PanelRoot.transform.Find("sysShowCard/sysShowCardToggle").GetComponent<Toggle>();
        bigLuckCardToggle = PanelRoot.transform.Find("bigLuckCard/bigLuckCardToggle").GetComponent<Toggle>();
        createRoomPanel = transform.parent.GetComponent<CreateRoomPanel>();
    }

    public void singleCoinToggleFunc(bool isOn)
    {
        if (isOn)
            singleCoinInputField.gameObject.SetActive(false);
    }
    public void singleCoinToggle1Func(bool isOn)
    {
        if (isOn)
            singleCoinInputField.gameObject.SetActive(true);
    }
    public void maxPotToggleFunc(bool isOn)
    {
        if (isOn)
            maxPotInputField.gameObject.SetActive(true);
        else
            maxPotInputField.gameObject.SetActive(false);
    }
    public void isGrantToggleFunc(bool isOn)
    {
        if (!isOn)
        {
            if (createRoomPanel.leagueInfo != null && createRoomPanel.leagueInfo.Type != 0)
            {
                isGrantToggle.isOn = true;
                // GF.UI.ShowToast("联盟房间不能取消授权买入");
            }
        }
    }

    public void OnbaseCoinSlidValueChanged()
    {
        int floorValue = Mathf.FloorToInt(baseCoinSlider.value);
        textmang.text = baseCoins[floorValue] + "";
        SetBringValueText();
        UpdateExpend();
        if (!createRoomPanel.m_IsUpdatingSlider)
        {
            createRoomPanel.LoadDefaultConfigs(MethodType.GoldenFlower, floorValue);
        }
    }

    public void OnplayerTimeSlidValueChanged()
    {
        UpdateExpend();
    }

    public void UpdateExpend()
    {
        if (playerTimeSlider == null) return;
        long time = playerTimes[(int)playerTimeSlider.value];
        int floorValue = Mathf.FloorToInt(baseCoinSlider.value);
        float baseCoin = baseCoins[floorValue];
        long expend = time / 30 * GlobalManager.GetInstance().GetMsg_CreateDeskConfigByType(MethodType.GoldenFlower, baseCoin,
            createRoomPanel.leagueInfo == null ? DeskType.Simple : createRoomPanel.leagueInfo.Type == 0 ? DeskType.Guild : DeskType.League);
        createRoomPanel.SetExpendText((int)expend);
    }

    public void OnMinBringSlidValueChanged()
    {
        int floorValue = Mathf.FloorToInt(baseCoinSlider.value);
        float baseCoin = baseCoins[floorValue];
        int bei = 100 + (int)minBringInSlider.value * 50;
        textzuididai.text = baseCoin * bei + "-" + baseCoin * bei * 10;
    }

    public void SetBringValueText()
    {
        int floorValue = Mathf.FloorToInt(baseCoinSlider.value);
        float baseCoin = baseCoins[floorValue];
        int bei = 100 + (int)minBringInSlider.value * 50;
        textzuididai.text = baseCoin * bei + "-" + baseCoin * bei * 10;
        Transform values = minBringInSlider.transform.Find("Values");
        for (int i = 0; i < values.childCount; i++)
        {
            values.GetChild(i).Find("Text").GetComponent<Text>().text = (baseCoin * (100 + i * 50)).ToString();
        }
    }

    public Desk_BaseConfig GetDesk_BaseConfig()
    {
        if (createRoomPanel.leagueInfo != null)
        {
            JackpotToggle.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            JackpotToggle.transform.parent.gameObject.SetActive(false);
        }
        Desk_BaseConfig desk_BaseConfig = new Desk_BaseConfig();
        desk_BaseConfig.BaseCoin = baseCoins[(int)baseCoinSlider.value];
        desk_BaseConfig.PlayerTime = playerTimes[(int)playerTimeSlider.value];
        desk_BaseConfig.PlayerNum = (int)playerNumSlider.value;
        //带入
        string[] parts = textzuididai.text.Split('-');
        int start = -1;
        int end = -1;
        if (parts.Length == 2)
        {
            start = int.Parse(parts[0]);
            end = int.Parse(parts[1]);
        }
        desk_BaseConfig.MinBringIn = start;
        desk_BaseConfig.MaxBringIn = end;

        desk_BaseConfig.Rate = new PairInt();
        desk_BaseConfig.Rate.Key = rates[(int)rateSlider.value];
        desk_BaseConfig.Rate.Val = bens[(int)benSlider.value];
        desk_BaseConfig.OpenRate = desk_BaseConfig.Rate.Key > 0;
        desk_BaseConfig.ForbidVoice = 禁止聊天Toggle.isOn;
        desk_BaseConfig.Forbid = 禁止打字Toggle.isOn;
        desk_BaseConfig.IsGrant = isGrantToggle.isOn;
        desk_BaseConfig.Jackpot = JackpotToggle.isOn;

        if (autoStartSlider.value == 0)
        {
            desk_BaseConfig.AutoStart = false;
        }
        else
        {
            desk_BaseConfig.AutoStart = true;
            desk_BaseConfig.AutoStartNum = (int)autoStartSlider.value + 1;
        }

        return desk_BaseConfig;
    }

    public GoldenFlowerConfig GetZJHConfig()
    {
        GF.LogInfo("炸金花额外配置");
        GoldenFlowerConfig goldenFlowerConfig = new GoldenFlowerConfig();
        goldenFlowerConfig.MustStuffy = mustStuffys[(int)mustStuffySlider.value];
        goldenFlowerConfig.MaxRound = maxRounds[(int)maxRoundSlider.value];
        goldenFlowerConfig.CompareRound = compareRounds[(int)compareRoundSlider.value];

        if (singleCoinToggle.isOn)
        {
            goldenFlowerConfig.SingleCoin = -1;
        }
        else
        {
            int a = -1;
            int.TryParse(singleCoinInputField.text, out a);
            if (a < baseCoins[(int)baseCoinSlider.value] * 2)
            {
                GF.UI.ShowToast("单注上限不能低于底分2倍");
                return null;
            }
            goldenFlowerConfig.SingleCoin = a;
        }

        if (!maxPotToggle.isOn)
        {
            goldenFlowerConfig.MaxPot = -1;
        }
        else
        {
            int a = -1;
            int.TryParse(maxPotInputField.text, out a);
            if (a < baseCoins[(int)baseCoinSlider.value] * 100)
            {
                GF.UI.ShowToast("底池封顶不能低于底分100倍");
                return null;
            }
            goldenFlowerConfig.MaxPot = a;
        }
        goldenFlowerConfig.RateState = RateTypeToggle.isOn ? 0 : 1;
        goldenFlowerConfig.IsCompareDouble = isCompareDoubleToggle.isOn;
        goldenFlowerConfig.Stuffy = isMenbiToggle.isOn;
        goldenFlowerConfig.Use235 = use235Toggle.isOn;
        goldenFlowerConfig.SysShowCard = sysShowCardToggle.isOn;
        goldenFlowerConfig.BigLuckCard = bigLuckCardToggle.isOn;
        goldenFlowerConfig.ShotCard = shotCardToggle.isOn;

        return goldenFlowerConfig;
    }

    public void ApplyDeskConfigToUI(Msg_CreateDeskRq msg)
    {
        JackpotToggle.transform.parent.gameObject.SetActive(createRoomPanel.leagueInfo != null);
        // 检查 msg.Config 是否为 null 或包含无效数据
        if(msg.Floor != 0) return;
        if (msg == null || msg.Config == null )
        {
            // 如果配置为空，应用默认配置
            ApplyDefaultConfig();
            return;
        }

        int baseCoinIndex = Array.IndexOf(baseCoins, msg.Config.BaseCoin);
        if (baseCoinIndex != -1)
        {
            baseCoinSlider.value = baseCoinIndex; // 设置 Slider 的值
            OnbaseCoinSlidValueChanged(); // 更新底分相关显示文本
        }
        else
        {
            // 如果没有找到匹配的底分，设置为默认值
            baseCoinSlider.value = 0; // 默认底分为 0.1
            OnbaseCoinSlidValueChanged(); // 更新底分相关显示文本
        }

        // 设置玩家时间 Slider
        int playerTimeIndex = Array.IndexOf(playerTimes, msg.Config.PlayerTime);
        if (playerTimeIndex != -1)
        {
            playerTimeSlider.value = playerTimeIndex; // 设置玩家时间
        }
        else
        {
            // 如果没有找到匹配的时间，设置为默认值
            playerTimeSlider.value = 0; // 默认时间为 30 秒
        }

        // 设置玩家数量 Slider
        if (msg.Config.PlayerNum >= playerNumSlider.minValue && msg.Config.PlayerNum <= playerNumSlider.maxValue)
        {
            playerNumSlider.value = msg.Config.PlayerNum; // 设置玩家数量
        }
        else
        {
            // 如果玩家数量无效，设置为默认值
            playerNumSlider.value = 9; // 默认 4 名玩家 
        }

        // 设置强制带入 Slider
        int mustStuffyIndex = Array.IndexOf(mustStuffys, msg.GoldenFlowerConfig.MustStuffy);
        if (mustStuffyIndex != -1)
        {
            mustStuffySlider.value = mustStuffyIndex; // 设置强制带入
        }

        // 设置最大轮数 Slider
        int maxRoundIndex = Array.IndexOf(maxRounds, msg.GoldenFlowerConfig.MaxRound);
        if (maxRoundIndex != -1)
        {
            maxRoundSlider.value = maxRoundIndex; // 设置最大轮数
        }

        // 设置比牌轮数 Slider
        int compareRoundIndex = Array.IndexOf(compareRounds, msg.GoldenFlowerConfig.CompareRound);
        if (compareRoundIndex != -1)
        {
            compareRoundSlider.value = compareRoundIndex; // 设置比牌轮数
        }

        // 设置倍率 Slider
        int rateIndex = Array.IndexOf(rates, msg.Config.Rate.Key);
        if (rateIndex != -1)
        {
            rateSlider.value = rateIndex; // 设置倍率
        }

        // 设置本倍数 Slider
        int benIndex = Array.IndexOf(bens, msg.Config.Rate.Val);
        if (benIndex != -1)
        {
            benSlider.value = benIndex; // 设置本倍数
        }

        // 设置自动开始 Slider 和状态
        if (msg.Config.AutoStart)
        {
            autoStartSlider.value = msg.Config.AutoStartNum - 1; // 设置自动开始轮数
        }
        else
        {
            autoStartSlider.value = 0; // 默认自动开始关闭
        }

        // 设置 Jackpot Toggle
        JackpotToggle.isOn = msg.Config.Jackpot;

        // 设置 IsGrant Toggle
        isGrantToggle.isOn = msg.Config.IsGrant;

        //设置gps
        isGPSToggle.isOn = msg.Config.GpsLimit;

        // 设置ip
        isIPToggle.isOn = msg.Config.IpLimit;
        // 设置禁止聊天
        禁止聊天Toggle.isOn = msg.Config.ForbidVoice;

        // 设置禁止打字
        禁止打字Toggle.isOn = msg.Config.Forbid;
        // 设置 SingleCoin Toggle 和 InputField
        if (msg.GoldenFlowerConfig.SingleCoin == -1)
        {
            singleCoinToggle.isOn = true;
            singleCoinInputField.gameObject.SetActive(false); // 隐藏输入框
        }
        else
        {
            singleCoinToggle1.isOn = true;
            singleCoinInputField.gameObject.SetActive(true); // 显示输入框
            singleCoinInputField.text = msg.GoldenFlowerConfig.SingleCoin.ToString(); // 设置输入框的文本
        }

        // 设置 MaxPot Toggle 和 InputField
        if (msg.GoldenFlowerConfig.MaxPot == -1)
        {
            maxPotToggle.isOn = false;
            maxPotInputField.gameObject.SetActive(false); // 隐藏输入框
        }
        else
        {
            maxPotToggle.isOn = true;
            maxPotInputField.gameObject.SetActive(true); // 显示输入框
            maxPotInputField.text = msg.GoldenFlowerConfig.MaxPot.ToString(); // 设置输入框的文本
        }

        // 设置其他 Toggle 配置
        RateTypeToggle.isOn = msg.GoldenFlowerConfig.RateState == 0;
        isCompareDoubleToggle.isOn = msg.GoldenFlowerConfig.IsCompareDouble;
        isMenbiToggle.isOn = msg.GoldenFlowerConfig.Stuffy;
        use235Toggle.isOn = msg.GoldenFlowerConfig.Use235;
        sysShowCardToggle.isOn = msg.GoldenFlowerConfig.SysShowCard;
        bigLuckCardToggle.isOn = msg.GoldenFlowerConfig.BigLuckCard;
        shotCardToggle.isOn = msg.GoldenFlowerConfig.ShotCard;
    }


    private void ApplyDefaultConfig()
    {
        // 设置所有控件为默认值

        // 默认底分为 0.1
        baseCoinSlider.value = 0;
        OnbaseCoinSlidValueChanged(); // 更新底分相关显示文本

        // 默认时间为 30 秒
        playerTimeSlider.value = 0;

        // 默认玩家数量为 4
        playerNumSlider.value = 9;

        // 默认无强制带入
        mustStuffySlider.value = 0;

        // 默认最大轮数为 30
        maxRoundSlider.value = 5;

        // 默认比牌轮数为 1
        compareRoundSlider.value = 0;

        // 默认倍率为 0
        rateSlider.value = 0;

        // 默认本倍数为 1
        benSlider.value = 0;

        // 默认自动开始为 false
        autoStartSlider.value = 0;

        // 默认状态下，singleCoinToggle 选中
        singleCoinToggle.isOn = true;
        singleCoinInputField.gameObject.SetActive(false); // 隐藏输入框

        // 默认不选中 singleCoinToggle1
        singleCoinToggle1.isOn = false;

        // 默认不选中 maxPotToggle
        maxPotToggle.isOn = false;
        maxPotInputField.gameObject.SetActive(false); // 隐藏输入框

        RateTypeToggle.isOn = true;

        // 默认不选中 isCompareDoubleToggle
        isCompareDoubleToggle.isOn = false;

        isMenbiToggle.isOn = false;

        // 默认不选中 isGrantToggle
        isGrantToggle.isOn = true;

        // 默认不选中 isGPSToggle
        isGPSToggle.isOn = false;

        // 默认不选中 isIPToggle
        isIPToggle.isOn = false;

        // 默认不选中 use235Toggle
        use235Toggle.isOn = false;

        // 默认不选中 shotCardToggle
        shotCardToggle.isOn = false;

        // 默认不选中 JokpToggle
        JackpotToggle.isOn = false;

        // 默认不选中 sysShowCardToggle
        sysShowCardToggle.isOn = false;

        // 默认不选中 bigLuckCardToggle
        bigLuckCardToggle.isOn = false;

        // 默认不选中 禁止聊天Toggle
        禁止聊天Toggle.isOn = false;
        // 默认不选中 禁止打字Toggle
        禁止打字Toggle.isOn = false;
    }

}

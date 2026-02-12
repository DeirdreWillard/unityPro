using UnityEngine;
using UnityEngine.UI;

using NetMsg;
using System;

public class CreateNNRoom : MonoBehaviour
{
    public Slider baseCoinSlider;
    public Text textmang;
    public Text textzuididai;
    public Slider playerTimeSlider;
    public Slider playerNumSlider;
    public Slider minBringInSlider;
    public Slider rateSlider;
    public Slider benSlider;
    public Slider autoStartSlider;

    public Toggle toggleN1;
    public Toggle toggleN2;
    public Toggle toggleN3;
    public Toggle toggleN4;

    public float[] baseCoins = { 0.1f, 0.25f, 0.5f, 1, 2, 3, 5, 10, 20, 25, 50, 100, 200, 300, 500, 1000 };
    public long[] playerTimes = { 30, 60, 90, 120, 180, 240, 300, 360 };

    public int[] minBringIns = { 100, 150, 200, 250, 300, 350, 400 };
    public int[] rates = { 0, 10, 15, 20, 25, 30, 35, 40, 45, 50 };
    public int[] bens = { 1, 10, 20, 30, 40, 50, 60, 70, 80, 100 };


    public string[] tesupaiMiao = { "葫芦牛*", "五花牛*", "同花牛*", "顺子牛*", "炸弹牛*", "五小牛*", "同花顺牛*" };
    public int[] tesupai1 = { 7, 5, 6, 5, 8, 10, 10 };
    public int[] tesupai2 = { 7, 5, 6, 5, 8, 10, 10 };
    public int[] tesupai3 = { 7, 5, 6, 5, 8, 10, 10 };
    public int[] tesupai4 = { 8, 6, 7, 6, 9, 10, 10 };
    public int[] tesupai5 = { 13, 11, 12, 11, 14, 15, 15 };
    public Dropdown NNfanDrop;
    public Toggle[] NNToggles;

    public Toggle qiangToggle;
    public Toggle JokpToggle;
    public Toggle isGrantToggle;
    public Toggle openRogueToggle;
	public Toggle Toggle_IP;
	public Toggle Toggle_GPS;

    public CreateRoomPanel createroom;
    public GameObject goJackpot;
    public GameObject goBringIn;
    public Toggle 禁止聊天Toggle;
    public Toggle 禁止打字Toggle;

    // Start is called before the first frame update
    void Start()
    {
        //NN相关的
        NNfanDrop.onValueChanged.AddListener(delegate
        {
            DropdownItemSelected(NNfanDrop);
        });
        SetNNToggles(NNfanDrop.value);
        baseCoinSlider.onValueChanged.AddListener(OnbaseCoinSlidValueChanged);
        minBringInSlider.onValueChanged.AddListener(OnminBringInSliderValueChanged);
    }

    void OnDestroy()
    {
        baseCoinSlider.onValueChanged.RemoveAllListeners();
        minBringInSlider.onValueChanged.RemoveAllListeners();
    }

    public void ApplyDeskConfigToUI(Msg_CreateDeskRq msg)
    {
        if(msg.Floor != 0) return;
        // 检查 msg.Config 是否为 null 或包含无效数据
        if (msg == null)
        {
            ApplyDefaultConfig();
            goJackpot.SetActive(createroom.leagueInfo != null);
            if (createroom.leagueInfo == null || createroom.leagueInfo.Type == 0) {
                goBringIn.SetActive(true);
                isGrantToggle.isOn = true;
                isGrantToggle.interactable = true;
            }
            else {
                goBringIn.SetActive(true);
                isGrantToggle.isOn = true;
                isGrantToggle.interactable = false;
            }
        }
        else {

            // 设置底分 Slider 的值
            int baseCoinIndex = Array.IndexOf(baseCoins, msg.Config.BaseCoin);
            if (baseCoinIndex != -1)
            {
                baseCoinSlider.value = baseCoinIndex;
                OnbaseCoinSlidValueChanged(baseCoinIndex);
            }
            else
            {
                baseCoinSlider.value = 0; // 默认值为第一个项
                OnbaseCoinSlidValueChanged(0);
            }

            // 设置玩家时间 Slider 的值
            int playerTimeIndex = Array.IndexOf(playerTimes, msg.Config.PlayerTime);
            if (playerTimeIndex != -1)
            {
                playerTimeSlider.value = playerTimeIndex;
            }
            else
            {
                playerTimeSlider.value = 0; // 默认值为第一个项
            }

            // 设置玩家人数 Slider 的值
            switch (msg.Config.PlayerNum)
            {
                case 5:
                    playerNumSlider.value = 0;
                    break;
                case 6:
                    playerNumSlider.value = 1;
                    break;
                case 8:
                    playerNumSlider.value = 2;
                    break;
                default:
                    playerNumSlider.value = 0; // 默认值为 5 人
                    break;
            }

            // 设置最小带入 Slider 的值
            int minBringInIndex = Array.IndexOf(minBringInsTemp, msg.Config.MinBringIn);
            if (minBringInIndex != -1)
            {
                minBringInSlider.value = minBringInIndex;
            }
            else
            {
                minBringInSlider.value = 0; // 默认值为第一个项
            }

            // 设置倍率的 Slider 的值
            int rateIndex = Array.IndexOf(rates, msg.Config.Rate.Key);
            if (rateIndex != -1)
            {
                rateSlider.value = rateIndex;
            }
            else
            {
                rateSlider.value = 0; // 默认值为第一个项
            }

            // 设置 ben 的 Slider 的值
            int benIndex = Array.IndexOf(bens, msg.Config.Rate == null ? 0 : msg.Config.Rate.Val);
            if (benIndex != -1)
            {
                benSlider.value = benIndex;
            }
            else
            {
                benSlider.value = 0; // 默认值为第一个项
            }

            // 设置自动开始 Slider 的值
            if (msg.Config.AutoStart)
            {
                autoStartSlider.value = msg.Config.AutoStartNum - 1;
            }
            else
            {
                autoStartSlider.value = 0; // 默认关闭自动开始
            }

            // 设置 Jackpot Toggle
            JokpToggle.isOn = msg.Config.Jackpot;

            // 设置 IsGrant Toggle
            isGrantToggle.isOn = msg.Config.IsGrant;
			Toggle_IP.isOn = msg.Config.IpLimit;
			Toggle_GPS.isOn = msg.Config.GpsLimit;

            // 设置牛牛相关配置
            if (msg.NiuConfig != null)
            {
                NNfanDrop.value = msg.NiuConfig.DoubleRule - 1;
                qiangToggle.isOn = msg.NiuConfig.NoNiuRobBanker;
                openRogueToggle.isOn = msg.NiuConfig.OpenRogue;

                // 设置特殊牌型 toggle
                foreach (var toggle in NNToggles)
                {
                    toggle.isOn = false; // 初始化为未选中状态
                }
                foreach (int specialCardIndex in msg.NiuConfig.SpecialCard)
                {
                    if (specialCardIndex - 1 >= 0 && specialCardIndex - 1 < NNToggles.Length)
                    {
                        NNToggles[specialCardIndex - 1].isOn = true;
                    }
                }
            }
            else
            {
                // 如果没有 NiuConfig，应用默认的牛牛设置
                NNfanDrop.value = 0; // 默认选择第一个牌型
                qiangToggle.isOn = false;
                openRogueToggle.isOn = false;
                foreach (var toggle in NNToggles)
                {
                    toggle.isOn = false; // 默认没有选择特殊牌型
                }
            }
            if (createroom.leagueInfo == null) {
                goJackpot.SetActive(false);
            }
            else {
                goJackpot.SetActive(true);
            }
            if (createroom.leagueInfo == null || createroom.leagueInfo.Type == 0) {
                goBringIn.SetActive(true);
                isGrantToggle.interactable = true;
            }
            else {
                goBringIn.SetActive(true);
                isGrantToggle.isOn = true;
                isGrantToggle.interactable = false;
            }

            // 设置禁止聊天
            禁止聊天Toggle.isOn = msg.Config.ForbidVoice;

            // 设置禁止打字
            禁止打字Toggle.isOn = msg.Config.Forbid;
        }
    }

    private void ApplyDefaultConfig()
    {
        // 设置所有控件为默认值
        baseCoinSlider.value = 0; // 默认底分为 0.1
        OnbaseCoinSlidValueChanged(0);

        playerTimeSlider.value = 0; // 默认时间为 30 秒
        playerNumSlider.value = 0; // 默认玩家人数为 5
        minBringInSlider.value = 0; // 默认最小带入为第一个项
        rateSlider.value = 0; // 默认倍率为第一个项
        benSlider.value = 0; // 默认 ben 为第一个项
        autoStartSlider.value = 0; // 默认关闭自动开始

        JokpToggle.isOn = false; // 默认不启用 Jackpot
        isGrantToggle.isOn = true; // 默认不启用 Grant
        Toggle_IP.isOn = false;
        Toggle_GPS.isOn = false;
        禁止聊天Toggle.isOn = false;
        禁止打字Toggle.isOn = false;

        NNfanDrop.value = 0; // 默认选择第一个牛牛牌型
        qiangToggle.isOn = false; // 默认不启用抢庄
        openRogueToggle.isOn = false; // 默认不启用 rogue
        foreach (var toggle in NNToggles)
        {
            toggle.isOn = false; // 默认没有选择特殊牌型
        }
    }

    public Desk_BaseConfig GetDesk_BaseConfig()
    {
        Desk_BaseConfig desk_BaseConfig = new Desk_BaseConfig();
        desk_BaseConfig.BaseCoin = baseCoins[(int)baseCoinSlider.value];
        desk_BaseConfig.PlayerTime = playerTimes[(int)playerTimeSlider.value];
        if ((int)playerNumSlider.value == 0)
        {
            desk_BaseConfig.PlayerNum = 5;
        }
        else if ((int)playerNumSlider.value == 1)
        {
            desk_BaseConfig.PlayerNum = 6;
        }
        else if ((int)playerNumSlider.value == 2)
        {
            desk_BaseConfig.PlayerNum = 8;
        }
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
        if (autoStartSlider.value == 0)
        {
            desk_BaseConfig.AutoStart = false;
        }
        else
        {
            desk_BaseConfig.AutoStart = true;
            desk_BaseConfig.AutoStartNum = (int)autoStartSlider.value + 1;
        }
        desk_BaseConfig.Jackpot = JokpToggle.isOn;
        desk_BaseConfig.IsGrant = isGrantToggle.isOn;
        desk_BaseConfig.IpLimit = Toggle_IP.isOn;
        desk_BaseConfig.GpsLimit = Toggle_GPS.isOn;
        desk_BaseConfig.ForbidVoice = 禁止聊天Toggle.isOn;
        desk_BaseConfig.Forbid = 禁止打字Toggle.isOn;

        return desk_BaseConfig;
    }

    public NiuNiuConfig GetNiuNiuConfig()
    {
        GF.LogInfo("牛牛组队");
        NiuNiuConfig niuNiuConfig = new NiuNiuConfig();
        niuNiuConfig.DoubleRule = NNfanDrop.value + 1;
        niuNiuConfig.NoNiuRobBanker = qiangToggle.isOn;
        niuNiuConfig.OpenRogue = openRogueToggle.isOn;
        for (int i = 0; i < NNToggles.Length; i++)
        {
            if (NNToggles[i].isOn)
            {
                niuNiuConfig.SpecialCard.Add(i + 1);
            }
        }
        return niuNiuConfig;
    }


    // 下拉菜单选项变更时调用的函数
    void DropdownItemSelected(Dropdown dropdown)
    {
        // 获取当前选中的索引
        SetNNToggles(dropdown.value);
    }
    public void SetNNToggles(int value)
    {
        int[] temp = { 0, 0, 0, 0, 0, 0, 0 };
        switch (value)
        {
            case 0:
                temp = tesupai1;
                break;
            case 1:
                temp = tesupai2;
                break;
            case 2:
                temp = tesupai3;
                break;
            case 3:
                temp = tesupai4;
                break;
            case 4:
                temp = tesupai5;
                break;
        }
        for (int i = 0; i < NNToggles.Length; i++)
        {
            NNToggles[i].transform.Find("Label").GetComponent<Text>().text = tesupaiMiao[i] + temp[i];
        }
    }

    void OnminBringInSliderValueChanged(float value)
    {
        int floorValue = Mathf.FloorToInt(value);
        textzuididai.text = minBringInsTemp[floorValue] + "-" + minBringInsTemp[floorValue] * 10;
    }
    public int[] minBringInsTemp = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
    void OnbaseCoinSlidValueChanged(float value)
    {
        int floorValue = Mathf.FloorToInt(value);
        textmang.text = baseCoins[floorValue].ToString();
        textzuididai.text = baseCoins[floorValue] * minBringIns[(int)minBringInSlider.value] + "-" + baseCoins[floorValue] * minBringIns[(int)minBringInSlider.value] * 10;

        Transform values = minBringInSlider.transform.Find("Values");
        for (int i = 0; i < minBringIns.Length; i++)
        {
            minBringInsTemp[i] = (int)(minBringIns[i] * baseCoins[floorValue]);
            values.GetChild(i).Find("Text").GetComponent<Text>().text = minBringInsTemp[i].ToString();
        }
        UpdateExpend();
        if (!createroom.m_IsUpdatingSlider)
        {
            createroom.LoadDefaultConfigs(MethodType.NiuNiu, floorValue);
        }
    }
    public void OnplayerTimeSlidValueChanged()
    {
        UpdateExpend();
    }

    public void UpdateExpend(){
        long time = playerTimes[(int)playerTimeSlider.value];
        int floorValue = Mathf.FloorToInt(baseCoinSlider.value);
        float baseCoin = baseCoins[floorValue];
        long expend = time / 30 * GlobalManager.GetInstance().GetMsg_CreateDeskConfigByType(MethodType.NiuNiu, baseCoin,
            createroom.leagueInfo == null ? DeskType.Simple : createroom.leagueInfo.Type == 0 ? DeskType.Guild : DeskType.League);
        createroom.SetExpendText((int)expend);
    }

    public void OnChangeJackpot() {
        if (JokpToggle.isOn) {
            toggleN1.isOn = true;
            toggleN2.isOn = true;
            toggleN3.isOn = true;
            toggleN4.isOn = true;
            toggleN1.interactable = false;
            toggleN2.interactable = false;
            toggleN3.interactable = false;
            toggleN4.interactable = false;
        }
        else {
            toggleN1.interactable = true;
            toggleN2.interactable = true;
            toggleN3.interactable = true;
            toggleN4.interactable = true;
        }
    }

}

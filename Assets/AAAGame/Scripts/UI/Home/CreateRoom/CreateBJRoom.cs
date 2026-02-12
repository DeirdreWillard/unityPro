using System;
using UnityEngine;
using UnityEngine.UI;

using NetMsg;
public class CreateBJRoom : MonoBehaviour
{
    public Slider baseCoinSlider;
    public Text textmang;
    public Text textzuididai;
    public Text[] bringValueTexts;
    public Slider minBringInSlider;
    public Slider playerTimeSlider;
    public Slider playerNumSlider;
    public Slider 摆牌时间Slider;

    public Slider rateSlider;
    public Slider benSlider;
    public Slider autoStartSlider;

    public float[] baseCoins = { 0.1f, 0.25f, 0.5f, 1, 2, 3, 5, 10, 20, 25, 50, 100, 200, 300, 500, 1000 };
    public long[] playerTimes = { 30, 60, 90, 120, 180, 240, 300, 360 };
    public int[] optionTimes = { 30, 45, 60, 120 };
    public int[] rates = { 0, 10, 15, 20, 25, 30, 35, 40, 45, 50 };
    public int[] bens = { 1, 10, 20, 30, 40, 50, 60, 70, 80, 100 };

    public Toggle 王牌无王Toggle;
    public Toggle 王牌2王Toggle;
    public Toggle 王牌4王Toggle;
    public Toggle 弃牌Toggle;
    public Toggle 十选九Toggle;
    public Toggle 王当红黑Toggle;
    public Toggle 完整比牌Toggle;
    public Toggle 每道比牌Toggle;
    public Toggle 跳过比牌Toggle;
    public Toggle[] 喜牌Toggles;

    public Toggle isGrantToggle;
    public Toggle isGPSToggle;
    public Toggle 禁止聊天Toggle;
    public Toggle 禁止打字Toggle;
    public Toggle isIPToggle;
    public Toggle JokpToggle;

    public CreateRoomPanel createRoomPanel;

    public void isGrantToggleFunc(bool isOn)
    {
        if (!isOn){
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
            createRoomPanel.LoadDefaultConfigs(MethodType.CompareChicken, floorValue);
        }
    }

    public void OnplayerTimeSlidValueChanged()
    {
        UpdateExpend();
    }

    public void OnWuWangToggleFunc(bool isOn)
    {
        if (isOn)
        {
            int playerCount = Mathf.RoundToInt(playerNumSlider.value);
            if (playerCount == 6)
            {
                王牌2王Toggle.isOn = true;
                GF.StaticUI.ShowError("6人模式下不能选择无王");
            }
        }
    }

    public void OnPlayerNumSliderChanged()
    {
        if (十选九Toggle == null) return;

        int playerCount = Mathf.RoundToInt(playerNumSlider.value); // Slider 值可能是浮点数，四舍五入取整
        if (playerCount == 6 && 十选九Toggle.isOn)
        {
            // 如果玩家数为6且十选九被选中，则取消选中十选九并提示
            十选九Toggle.isOn = false;
            GF.StaticUI.ShowError("6人模式下不能选择十选九");
        }
        if (playerCount == 6 && 王牌无王Toggle.isOn)
        {
            王牌2王Toggle.isOn = true;
            GF.StaticUI.ShowError("6人模式下不能选择无王");
        }
    }

    public void OnShiXuanJiuToggleFunc(bool isOn)
    {
        if (isOn)
        {
            int playerCount = Mathf.RoundToInt(playerNumSlider.value);
            if (playerCount == 6)
            {
                十选九Toggle.isOn = false;
                GF.StaticUI.ShowError("6人模式下不能选择十选九");
            }
        }
    }

    public void UpdateExpend(){
        if (playerTimeSlider == null) return;
        long time = playerTimes[(int)playerTimeSlider.value];
        int floorValue = Mathf.FloorToInt(baseCoinSlider.value);
        float baseCoin = baseCoins[floorValue];
        long expend = time / 30 * GlobalManager.GetInstance().GetMsg_CreateDeskConfigByType(MethodType.CompareChicken, baseCoin, 
            createRoomPanel.leagueInfo == null ? DeskType.Simple : createRoomPanel.leagueInfo.Type == 0 ? DeskType.Guild : DeskType.League);
        createRoomPanel.SetExpendText((int)expend);
    }

    public void OnMinBringSlidValueChanged(){
        int floorValue = Mathf.FloorToInt(baseCoinSlider.value);
        float baseCoin = baseCoins[floorValue];
        int bei = 100 + (int)minBringInSlider.value * 50;
        textzuididai.text = baseCoin * bei + "-" + baseCoin * bei * 10;
    }

    public void SetBringValueText(){
        int floorValue = Mathf.FloorToInt(baseCoinSlider.value);
        float baseCoin = baseCoins[floorValue];
        int bei = 100 + (int)minBringInSlider.value * 50;
        textzuididai.text = baseCoin * bei + "-" + baseCoin * bei * 10;
        for (int i = 0; i < bringValueTexts.Length; i++)
        {
            bringValueTexts[i].text = (baseCoin * (100 + i * 50)).ToString();
        }
    }

    public Desk_BaseConfig GetDesk_BaseConfig()
    {
        JokpToggle.transform.parent.gameObject.SetActive(createRoomPanel.leagueInfo != null);
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
        desk_BaseConfig.GpsLimit = isGPSToggle.isOn;
        desk_BaseConfig.IpLimit = isIPToggle.isOn;
        desk_BaseConfig.ForbidVoice = 禁止聊天Toggle.isOn;
        desk_BaseConfig.Forbid = 禁止打字Toggle.isOn;
        desk_BaseConfig.IsGrant = isGrantToggle.isOn;
        desk_BaseConfig.Jackpot = JokpToggle.isOn;
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

    public CompareChickenConfig GetBJConfig()
    {
        GF.LogInfo("比鸡额外配置");
        CompareChickenConfig compareChickenConfig = new CompareChickenConfig();

        compareChickenConfig.King = 王牌无王Toggle.isOn ? 0 : (王牌2王Toggle.isOn ? 2 : (王牌4王Toggle.isOn ? 4 : 0));
        compareChickenConfig.OptionTime = optionTimes[(int)摆牌时间Slider.value];
        compareChickenConfig.Play.Clear();
        if (弃牌Toggle.isOn)
        {
            compareChickenConfig.Play.Add(1);
        }
        if(十选九Toggle.isOn)
        {
            compareChickenConfig.Play.Add(2);
        }
        if (王当红黑Toggle.isOn)
        {
            compareChickenConfig.Play.Add(3);
        }
        compareChickenConfig.LuckCard.Clear();
        for (int i = 0; i < 喜牌Toggles.Length; i++)
        {
            if (喜牌Toggles[i].isOn)
            {
                compareChickenConfig.LuckCard.Add(i + 1);
            }
        }

        // 设置是否启用完整比牌动画
        compareChickenConfig.Animation = 完整比牌Toggle.isOn ? 1 : 每道比牌Toggle.isOn ? 2 : 3;

        return compareChickenConfig;
    }

    public void ApplyDeskConfigToUI(Msg_CreateDeskRq msg)
    {
        if (createRoomPanel.leagueInfo != null)
        {
            JokpToggle.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            JokpToggle.transform.parent.gameObject.SetActive(false);
        }
        // 检查 msg.Config 是否为 null 或包含无效数据
        if(msg.Floor != 0) return;
        if (msg == null || msg.Config == null)
        {
            // 如果配置为空，应用默认配置
            ApplyDefaultConfig();
            return;
        }

        int baseCoinIndex = Array.IndexOf(baseCoins, msg.Config.BaseCoin);
        if (baseCoinIndex != -1)
        {
            baseCoinSlider.value = baseCoinIndex; // 设置 Slider 的值
            OnbaseCoinSlidValueChanged(); // 更新底分文本
        }
        else
        {
            // 如果没有找到匹配的底分，设置为默认值
            baseCoinSlider.value = 0; // 默认底分为 0.1
            OnbaseCoinSlidValueChanged();
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
            playerNumSlider.value = 2; // 默认2名玩家 
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

        switch (msg.CompareChickenConfig.Animation)
        {
            case 1:
                完整比牌Toggle.isOn = true;
                break;
            case 2:
                每道比牌Toggle.isOn = true;
                break;
            case 3:
                跳过比牌Toggle.isOn = true;
                break;
        }

        // 设置 Jackpot Toggle
        JokpToggle.isOn = msg.Config.Jackpot;

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

        // 设置摆牌时间
        int optionTimeIndex = Array.IndexOf(optionTimes, msg.CompareChickenConfig.OptionTime);
        if (optionTimeIndex != -1)
        {
            摆牌时间Slider.value = optionTimeIndex;
        }
        else
        {
            摆牌时间Slider.value = 0; // 默认30秒
        }

        // 设置玩法Toggles
        弃牌Toggle.isOn = msg.CompareChickenConfig.Play.Contains(1);
        十选九Toggle.isOn = msg.CompareChickenConfig.Play.Contains(2);
        王当红黑Toggle.isOn = msg.CompareChickenConfig.Play.Contains(3);
        
        // 设置喜牌Toggles
        // 先清除所有选择
        for (int i = 0; i < 喜牌Toggles.Length; i++)
        {
            喜牌Toggles[i].isOn = false;
        }
        // 再设置新的选择
        foreach (var luckCard in msg.CompareChickenConfig.LuckCard)
        {
            if (luckCard > 0 && luckCard <= 喜牌Toggles.Length)
            {
                喜牌Toggles[luckCard - 1].isOn = true;
            }
        }

        // 设置王牌类型Toggles
        // 先清除所有选择
        王牌无王Toggle.isOn = false;
        王牌2王Toggle.isOn = false;
        王牌4王Toggle.isOn = false;
        
        // 根据配置选择对应的王牌类型
        switch (msg.CompareChickenConfig.King)
        {
            case 0:
                王牌无王Toggle.isOn = true;
                break;
            case 2:
                王牌2王Toggle.isOn = true;
                break;
            case 4:
                王牌4王Toggle.isOn = true;
                break;
            default:
                王牌无王Toggle.isOn = true;
                break;
        }

    }


    private void ApplyDefaultConfig()
    {
        // 设置所有控件为默认值

        // 默认底分为 0.1
        baseCoinSlider.value = 0;
        OnbaseCoinSlidValueChanged(); // 更新底分相关显示文本

        // 默认时间为 30 秒
        playerTimeSlider.value = 0;

        // 默认玩家数量为 2
        playerNumSlider.value = 2;

        // 默认倍率为 0
        rateSlider.value = 0;

        // 默认本倍数为 1
        benSlider.value = 0;

        // 默认自动开始为 false
        autoStartSlider.value = 0;

        // 默认无王
        王牌无王Toggle.isOn = true;
        
        完整比牌Toggle.isOn = true;

        弃牌Toggle.isOn = false;
        十选九Toggle.isOn = false;
        王当红黑Toggle.isOn = false;
        禁止聊天Toggle.isOn = false;
        禁止打字Toggle.isOn = false;
        for (int i = 0; i < 10; i++)
        {
            喜牌Toggles[i].isOn = false;
        }

        // 默认选中 isGrantToggle
        isGrantToggle.isOn = true;

        // 默认不选中 isGPSToggle
        isGPSToggle.isOn = false;

        // 默认不选中 isIPToggle
        isIPToggle.isOn = false;

        // 默认不选中 JokpToggle
        JokpToggle.isOn = false;

    }
}


using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Event;
using System.Collections.Generic;
using GameFramework;
using UnityGameFramework.Runtime;
using Google.Protobuf;
using System.Threading.Tasks;

class MJGiftInfo
{
    public int diamond;//钻石
    public string gold;//金币
}

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class MJShopPanel : UIFormBase
{
    public Text textExpend;
    private List<MJGiftInfo> MJgiftInfos = new();
    private string m_GoldBeforeExchange; // 记录兑换前的金币数量，用于计算差值

    [Header("===== 钻石 金币 =====")]
    public Sprite[] diamondSprites;//钻石图片
    public Sprite[] coinSprites;//金币图片
    public void Init()
    {

    }

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
    }

    private int[] diamondNum = { 1, 3, 6, 18, 30, 68, 200, 500, 1000, 2000, 5000 };
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        //开通会员文本
        textExpend.text = $"{GlobalManager.GetConstants(6)}/{GlobalManager.GetConstants(7)}天";
        HotfixNetworkComponent.AddListener(MessageID.Msg_BuyVipRs, Function_BuyVipRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_DiamondChangeCoinRs, Response_Dcc);
        HotfixNetworkComponent.AddListener(MessageID.Msg_CoinGiftRs, Response_Cg);
        Msg_CoinGiftRq req = MessagePool.Instance.Fetch<Msg_CoinGiftRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_CoinGiftRq, req);
        // Util.GetInstance().CheckSafeCodeState(gameObject);//安全验证
        Debug.Log("添加监听事件");
        foreach (Transform item in varShopContent.transform)
        {
            item.GetComponent<Toggle>().onValueChanged.AddListener((isOn) =>
            {
                Util.TestToggleChanged(item.name, varGameRule);
            });
        }
        foreach (Transform item in varDiamondContent.transform)
        {
            Destroy(item.gameObject);
        }
        for (int i = 0; i < diamondNum.Length; i++)
        {
            GameObject go = Instantiate(varDiamondItem, varDiamondContent.transform);
            go.transform.Find("text").GetComponent<Text>().text = (diamondNum[i] * 10).ToString() + "钻石";
            go.transform.Find("RechargeNum").GetComponent<Text>().text = "¥" + diamondNum[i].ToString();

            // 根据数量加载不同的钻石图片
            int spriteIndex = 0;
            if (i <= 2) spriteIndex = 0; // 1, 3, 6
            else if (i <= 4) spriteIndex = 1; // 18, 30
            else spriteIndex = Mathf.Min(i - 3, diamondSprites.Length - 1); // 68及以后依次对应2-7
            if (diamondSprites != null && spriteIndex < diamondSprites.Length)
            {
                var icon = go.transform.Find("img")?.GetComponent<Image>();
                if (icon != null) icon.sprite = diamondSprites[spriteIndex];
            }

            //添加监听事件
            go.transform.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnBtnClickBuyDiamond();
            });
        }
    }
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
    }
    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_BuyVipRs, Function_BuyVipRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_DiamondChangeCoinRs, Response_Dcc);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_CoinGiftRs, Response_Cg);
        //关闭界面时默认关闭所有监听事件
        foreach (Transform item in varShopContent.transform)
        {
            item.GetComponent<Toggle>().onValueChanged.RemoveAllListeners();
        }
        foreach (Transform item in varDiamondContent.transform)
        {
            item.GetComponent<Button>().onClick.RemoveAllListeners();
        }
        //默认toggle为钻石
        varShopContent.transform.Find("钻石").GetComponent<Toggle>().isOn = true;
        base.OnClose(isShutdown, userData);
    }
    public void Send_BuyVipRq()
    {
        //Msg_BuyVipRq
        Msg_BuyVipRq req = MessagePool.Instance.Fetch<Msg_BuyVipRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_BuyVipRq, req);
    }
    public void Function_BuyVipRs(MessageRecvData data)
    {
        GF.LogInfo("购买VIP");
        Msg_BuyVipRs ack = Msg_BuyVipRs.Parser.ParseFrom(data.Data);
        int value = Util.GetMyselfInfo().VipState;
        Util.GetMyselfInfo().VipState = ack.VipState;
        Util.GetMyselfInfo().VipEndTime = ack.VipEndTime;
        GF.UI.ShowToast("购买Vip成功", 2);
        transform.Find("Top/VIPBtn/Text").GetComponent<Text>().text = "到期时间: " + Util.MillisecondsToDateString(ack.VipEndTime, "yyyy/MM/dd");
        GF.Event.Fire(this, ReferencePool.Acquire<UserDataChangedEventArgs>().Fill(UserDataType.VipState, value, Util.GetMyselfInfo().VipState));
    }
    protected override async void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "购买钻石":
                OnBtnClickBuyDiamond();
                break;
            case "购买VIP":
                Send_BuyVipRq();
                break;
            default:
                break;
        }
    }
    public void OnBtnClickBuyDiamond()
    {
        GF.LogInfo("购买钻石");
        Application.OpenURL($"{Const.HttpStr}{GlobalManager.ServerInfo_HouTaiIP}/api/pay/" + Util.GetMyselfInfo().PlayerId);
    }
    public void Response_Dcc(MessageRecvData data)
    {
        Msg_DiamondChangeCoinRs ack = Msg_DiamondChangeCoinRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("钻石兑换金币响应:", ack.ToString());

        double.TryParse(m_GoldBeforeExchange, out double oldGold);

        if (!double.TryParse(ack.Coin, out double newGold))
        {
            GF.LogError("解析金币数量失败: " + ack.Coin);
            return;
        }

        long delta = (long)(newGold - oldGold);

        if (delta > 0)
        {
            var rewards = new List<RewardItemData>
            {
                new RewardItemData
                {
                    Id = 1,
                    Count = delta
                }
            };
            Util.GetInstance().OpenRewardPanel(rewards, autoCloseDelay: 5f);
        }
    }

    public void Response_Cg(MessageRecvData data)
    {
        Msg_CoinGiftRs ack = Msg_CoinGiftRs.Parser.ParseFrom(data.Data);
        MJgiftInfos.Clear();

        foreach (Transform item in varGoldContent.transform)
        {
            Destroy(item.gameObject);
        }

        for (int i = ack.Gifts.Count - 1; i >= 0; i--)
        {
            var MJgiftinfo = ack.Gifts[i];
            GF.LogInfo("礼包:", MJgiftinfo.ToString());
            MJGiftInfo gift = new() { diamond = MJgiftinfo.Key, gold = MJgiftinfo.Val };
            MJgiftInfos.Add(gift);
        }
        MJgiftInfos.Sort((x, y) => y.diamond.CompareTo(x.diamond));
        for (int i = MJgiftInfos.Count - 1; i >= 0; i--)
        {
            MJGiftInfo gift = MJgiftInfos[i];
            GameObject go = Instantiate(varGoldItem, varGoldContent.transform);
            //go.SetActive(true);
            go.transform.Find("text").GetComponent<Text>().text = gift.gold.ToString();
            go.transform.Find("RechargeNum").GetComponent<Text>().text = gift.diamond.ToString();

            // 根据档位加载不同的金币图片 (0, 2, 4, 6)
            int tierIndex = MJgiftInfos.Count - 1 - i;
            int spriteIndex = tierIndex;
            if (coinSprites != null && spriteIndex < coinSprites.Length)
            {
                var icon = go.transform.Find("img")?.GetComponent<Image>();
                if (icon != null) icon.sprite = coinSprites[spriteIndex];
            }

            go.GetComponent<Button>().onClick.RemoveAllListeners();
            go.GetComponent<Button>().onClick.AddListener(delegate ()
            {
                Sound.PlayEffect(AudioKeys.SOUND_BTN);
                Util.GetInstance().OpenMJConfirmationDialog("购买欢乐豆", "确定要购买欢乐豆", () =>
                {
                    GF.LogInfo("购买欢乐豆");
                    // 提前记录当前金币，防止 Msg_SynCoinChange 先于 Response_Dcc 改变数据模型
                    m_GoldBeforeExchange = Util.GetMyselfInfo().Gold;

                    Msg_DiamondChangeCoinRq req = MessagePool.Instance.Fetch<Msg_DiamondChangeCoinRq>();
                    req.Diamond = gift.diamond;
                    HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                        (HotfixNetworkComponent.AccountClientName, MessageID.Msg_DiamondChangeCoinRq, req);
                });
            });
        }
    }
}

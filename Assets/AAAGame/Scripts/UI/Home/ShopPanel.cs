
using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Event;
using System.Collections.Generic;
using GameFramework;

class GiftInfo
{
    public int diamond;
    public string coin;
}

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class ShopPanel : UIFormBase
{

    public Text textCoin;
    public Text textDiamond;
    public GameObject gameobjectConfirm;
    public Text textConfirmInfo;
    public Text textExpend;

    public GameObject contentGiftList;
    public GameObject prefabGiftItem;

    private List<GiftInfo> giftInfos = new();
    private int giftindex = 0;
    private string m_GoldBeforeExchange; // 记录兑换前的金币数量

    public void Init()
    {
    }

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        textCoin.text = Util.GetMyselfInfo().Gold;
        textDiamond.text = Util.GetMyselfInfo().Diamonds.ToString();
        textExpend.text = $"{GlobalManager.GetConstants(6)}/{GlobalManager.GetConstants(7)}天";

        if (Util.GetMyselfInfo().VipState == 0)
        {
            transform.Find("Top/VIPBtn/Text").GetComponent<Text>().text = "未开通";
        }
        else
        {
            transform.Find("Top/VIPBtn/Text").GetComponent<Text>().text = "到期时间: " + Util.MillisecondsToDateString(Util.GetMyselfInfo().VipEndTime, "yyyy/MM/dd");
        }

        HotfixNetworkComponent.AddListener(MessageID.Msg_BuyVipRs, Function_BuyVipRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_DiamondChangeCoinRs, Response_Dcc);
        GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
        GF.Event.Subscribe(GFEventArgs.EventId, OnGFEventChanged);


        HotfixNetworkComponent.AddListener(MessageID.Msg_CoinGiftRs, Response_Cg);

        Msg_CoinGiftRq req = MessagePool.Instance.Fetch<Msg_CoinGiftRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_CoinGiftRq, req);
        Util.GetInstance().CheckSafeCodeState(gameObject);
    }


    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);

    }
    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_BuyVipRs, Function_BuyVipRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_DiamondChangeCoinRs, Response_Dcc);
        GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
        GF.Event.Unsubscribe(GFEventArgs.EventId, OnGFEventChanged);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_CoinGiftRs, Response_Cg);
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

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "购买钻石":
                OnBtnClickBuyDiamond();
                break;
            case "确认购买":
                OnBtnClickConfirm();
                break;
            case "取消购买":
                gameobjectConfirm.SetActive(false);
                break;
            case "购买VIP":
                Send_BuyVipRq();
                break;
            default:
                if (int.TryParse(btId, out int index))
                {
                    if (index <= 0 || index > giftInfos.Count) return;
                    giftindex = index;
                    OnBtnClickBuyCoin();
                }
                break;
        }
    }

    public void OnBtnClickBuyDiamond()
    {
        GF.LogInfo("购买钻石");
        Application.OpenURL($"{Const.HttpStr}{GlobalManager.ServerInfo_HouTaiIP}/api/pay/" + Util.GetMyselfInfo().PlayerId);
    }

    private void OnBtnClickBuyCoin()
    {
        GiftInfo info = giftInfos[giftindex - 1];
        textConfirmInfo.text = "您确认用" + info.diamond + "钻石购买" + info.coin + "?";
        gameobjectConfirm.SetActive(true);
    }

    private void OnBtnClickConfirm()
    {
        GiftInfo info = giftInfos[giftindex - 1];
        // 提前记录当前金币，防止同步推送消息抢先更改模型
        m_GoldBeforeExchange = Util.GetMyselfInfo().Gold;

        Msg_DiamondChangeCoinRq dcc = MessagePool.Instance.Fetch<Msg_DiamondChangeCoinRq>();
        dcc.Diamond = info.diamond;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_DiamondChangeCoinRq, dcc);
    }

    public void Response_Dcc(MessageRecvData data)
    {
        Msg_DiamondChangeCoinRs ack = Msg_DiamondChangeCoinRs.Parser.ParseFrom(data.Data);
        gameobjectConfirm.SetActive(false);
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
                    Id = 1,                     // ItemID
                    Count = delta               // 显示变化的额度
                }
            };
            Util.GetInstance().OpenRewardPanel(rewards, autoCloseDelay: 5f);
        }
    }

    public void Response_Cg(MessageRecvData data)
    {
        Msg_CoinGiftRs ack = Msg_CoinGiftRs.Parser.ParseFrom(data.Data);
        giftInfos.Clear();
        GF.UI.RemoveAllChildren(contentGiftList.transform);
        for (int i = ack.Gifts.Count - 1; i >= 0; i--)
        {
            var giftinfo = ack.Gifts[i];
            GF.LogInfo("礼包:" , giftinfo.ToString());
            GiftInfo gift = new() { diamond = giftinfo.Key, coin = giftinfo.Val };
            giftInfos.Add(gift);
        }
        giftInfos.Sort((x, y) => y.diamond.CompareTo(x.diamond));
        for (int i = 0; i < giftInfos.Count; i++)
        {
            GiftInfo gift = giftInfos[i];
            GameObject newgift = Instantiate(prefabGiftItem, contentGiftList.transform);
            newgift.SetActive(true);
            newgift.transform.localPosition = Vector3.zero;
            newgift.transform.localScale = Vector3.one;
            newgift.transform.Find("Text1").GetComponent<Text>().text = gift.coin;
            newgift.transform.Find("Text2/Text").GetComponent<Text>().text = gift.diamond.ToString();
            int index = i + 1;
            newgift.GetComponent<Button>().onClick.AddListener(() => ClickUIButton(index.ToString()));
        }
    }

    private void OnUserDataChanged(object sender, GameEventArgs e)
    {
        var args = e as UserDataChangedEventArgs;
        switch (args.Type)
        {
            case UserDataType.MONEY:
                textCoin.text = Util.GetMyselfInfo().Gold;
                GF.LogInfo("收到 金币变动事件.", Util.GetMyselfInfo().Gold);
                break;
            case UserDataType.DIAMOND:
                textDiamond.text = Util.GetMyselfInfo().Diamonds.ToString();
                GF.LogInfo("收到 钻石变动事件.", Util.GetMyselfInfo().Diamonds.ToString());
                break;
        }
    }

    public void OnGFEventChanged(object sender, GameEventArgs e)
    {
        var args = e as GFEventArgs;
        switch (args.EventType)
        {
            case GFEventType.eve_ReConnectGame:
                textCoin.text = Util.GetMyselfInfo().Gold;
                textDiamond.text = Util.GetMyselfInfo().Diamonds.ToString();
                break;
        }
    }

}

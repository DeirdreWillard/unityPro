using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Event;
using System.Collections.Generic;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class AllianceShopPanel : UIFormBase
{

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        HotfixNetworkComponent.AddListener(MessageID.Msg_GoldGiftRs, Function_GoldGiftRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_DiamondChangeGoldRs, Function_DiamondChangeGoldRs);

        GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);

        Msg_GoldGiftRq req = MessagePool.Instance.Fetch<Msg_GoldGiftRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GoldGiftRq, req);

        SetUI();
        Util.GetInstance().CheckSafeCodeState(gameObject);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_GoldGiftRs, Function_GoldGiftRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_DiamondChangeGoldRs, Function_DiamondChangeGoldRs);

        GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
        base.OnClose(isShutdown, userData);

    }

    private void OnUserDataChanged(object sender, GameEventArgs e)
    {
        var args = e as UserDataChangedEventArgs;
        switch (args.Type)
        {
            case UserDataType.MONEY:
                SetUI();
                break;
            case UserDataType.DIAMOND:
                SetUI();
                break;
        }
    }

    private void OnClubDataChanged(object sender, GameEventArgs e)
    {
        var args = e as ClubDataChangedEventArgs;
        switch (args.Type)
        {
            case ClubDataType.eve_LeagueDateUpdate:
                GF.LogInfo("俱乐部信息更新");
                SetUI();
                break;
        }
    }
    public void SetUI()
    {
        varCoin.text = GF.DataModel.GetDataModel<UserDataModel>().Gold.ToString();
        varDiamond.text = GF.DataModel.GetDataModel<UserDataModel>().Diamonds.ToString();
        varUnionCode.text = GlobalManager.GetInstance().GetAllianceCoins(GlobalManager.GetInstance().LeagueInfo.LeagueId) + "";
    }

    private void Function_DiamondChangeGoldRs(MessageRecvData data)
    {
        // Msg_DiamondChangeGoldRs
        Msg_DiamondChangeGoldRs ack = Msg_DiamondChangeGoldRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_DiamondChangeGoldRs收到钻石归还结果:" , ack.ToString());
        Util.UpdateClubInfoRq();
        var rewards = new List<RewardItemData>
        {
            new RewardItemData 
            { 
                Id = 3,                     // ItemID
                Count = ack.Gold   // 奖励数量
            }
        };
        Util.GetInstance().OpenRewardPanel(rewards, autoCloseDelay: 5f);
    }


    private void Function_GoldGiftRs(MessageRecvData data)
    {
        // Msg_GoldGiftRs
        Msg_GoldGiftRs ack = Msg_GoldGiftRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_GoldGiftRs收到钻石礼包列表:" , ack.ToString());
        ShowList(ack);
    }

    public void ShowList(Msg_GoldGiftRs ack)
    {
        //创建列表
        foreach (Transform item in varContent.transform)
        {
            Destroy(item.gameObject);
        }
        for (int i = 0; i < ack.Gifts.Count; i++)
        {
            PairString gift = ack.Gifts[i];
            GameObject newItem = Instantiate(varItem);
            newItem.transform.SetParent(varContent.transform, false);
            newItem.transform.Find("Price").GetComponent<Text>().text = gift.Key.ToString();
            newItem.transform.Find("Title").GetComponent<Text>().text = gift.Val;
            newItem.transform.Find("Bg").GetComponent<Button>().onClick.AddListener(delegate ()
            {
               Sound.PlayEffect(AudioKeys.SOUND_BTN);
                Util.GetInstance().OpenConfirmationDialog("温馨提示", string.Format("确定用{0}钻石购买{1}", gift.Key, gift.Val), () =>
                {
                    GF.LogInfo("兑换联盟币");
                    Msg_DiamondChangeGoldRq req = MessagePool.Instance.Fetch<Msg_DiamondChangeGoldRq>();
                    req.Diamond = gift.Key;
                    HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                        (HotfixNetworkComponent.AccountClientName, MessageID.Msg_DiamondChangeGoldRq, req);
                });
            });
        }
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "ChongZhiBtn":
                Application.OpenURL($"{Const.HttpStr}{GlobalManager.ServerInfo_HouTaiIP}/api/pay/"  + Util.GetMyselfInfo().PlayerId);
                break;
        }
    }

}

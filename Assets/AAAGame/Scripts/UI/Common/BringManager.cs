

using GameFramework;
using NetMsg;
using System.Collections.Generic;
using System.Linq;
public class BringManager
{
    private static BringManager instance;
    private BringManager() { }


    public static BringManager GetInstance()
    {
        if (instance == null)
        {
            instance = new BringManager();
        }
        return instance;
    }


    public void Init()
    {
        HotfixNetworkComponent.AddListener(MessageID.Msg_BringOptionRs, Function_BringOptionRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynBringInfo, Function_SynBringInfo);
        HotfixNetworkComponent.AddListener(MessageID.Msg_BringList, Function_BringList);
    }

    public void Clear()
    {
        bringInfoList.Clear();
        bringInfoListOver.Clear();
        cruBringInfo = null;
    }

    private List<BringInfo> bringInfoList = new();
    private List<BringInfo> bringInfoListOver = new();
    public List<BringInfo> GetBringInfoList()
    {
        return bringInfoList;
    }
    public List<BringInfo> GetBringInfoListOver()
    {
        return bringInfoListOver;
    }

    /// <summary>
    /// 计算带入红点显示
    /// </summary>
    public async void BringRedDotSum()
    {
        // MessageManager.GetInstance().CoinRedDotSum();
        bool have = false;
        var bringList = BringManager.GetInstance().GetBringInfoList();
        if (bringList != null && bringList.Count > 0)
        {
            for (int i = 0; i < bringList.Count; i++)
            {
                if (bringList[i].State == 0)
                {
                    have = true;
                    break;
                }
            }
        }
        if (have)
        {
            await GF.UI.OpenUIFormAwait(UIViews.BringTips);
        }
        else
        {
            GF.UI.CloseUIForms(UIViews.BringTips);
        }
    }

    public async void ShowBringInfoList()
    {
        // if (bringInfoList != null && bringInfoList.Count > 0)
        // {
        // }
        await GF.UI.OpenUIFormAwait(UIViews.BringDialog);
    }

    public async void AddToBringInfoList(BringInfo bringInfo)
    {
        //根据state去加入//处理状态 0 未处理  1同意 2拒绝 4 过期 5删除
        if (bringInfo.State == 0)
        {
           Sound.PlayEffect("dairu_warning.mp3");
            bringInfoList.Add(bringInfo);
            await GF.UI.OpenUIFormAwait(UIViews.BringTips);
        }
        else if (bringInfo.State == 5)
            RemoveBringInfoList(bringInfo);
        else
        {
            bringInfoListOver.Add(bringInfo);
            RemoveBringInfoList(bringInfo);
        }
    }
    public void ClearBringInfoList(List<BringInfo> bringInfos)
    {
        bringInfoList.Clear();
        bringInfoListOver.Clear();
        foreach (BringInfo item in bringInfos)
        {
            AddToBringInfoList(item);
        }
    }
    public void RemoveBringInfoList(BringInfo bringInfo)
    {
        for (int i = 0; i < bringInfoList.Count; i++)
        {
            if (bringInfoList[i].PlayerId == bringInfo.PlayerId && bringInfoList[i].Time == bringInfo.Time)
            {
                bringInfoList.RemoveAt(i);
            }
        }
    }

    public BringInfo cruBringInfo;
    /// <summary>
    /// 处理请求带入申请
    /// </summary>
    public void Send_BringOptionRq(long playerId, int state)
    {
        Msg_BringOptionRq req = MessagePool.Instance.Fetch<Msg_BringOptionRq>();
        req.PlayerId = playerId;
        req.State = state;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_BringOptionRq, req);
    }

    /// <summary>
    /// 带入申请
    /// </summary>
    public void Function_SynBringInfo(MessageRecvData data)
    {
        //Msg_SynBringInfo
        Msg_SynBringInfo ack = Msg_SynBringInfo.Parser.ParseFrom(data.Data);
        GF.LogInfo("带入申请通知" , ack.ToString());
        AddToBringInfoList(ack.BringInfos);
        BringRedDotSum();
    }

    /// <summary>
    /// 带入申请列表登录推送
    /// </summary>
    public void Function_BringList(MessageRecvData data)
    {
        //Msg_BringList
        Msg_BringList ack = Msg_BringList.Parser.ParseFrom(data.Data);
        GF.LogInfo("带入申请列表通知" , ack.ToString());
        ClearBringInfoList(ack.BringInfos.ToList());
        BringRedDotSum();
    }

    public void Function_BringOptionRs(MessageRecvData data)
    {
        GF.LogInfo("处理请求带入申请");
        RemoveBringInfoList(cruBringInfo);
        GF.Event.Fire(this, ReferencePool.Acquire<UserDataChangedEventArgs>().Fill(UserDataType.Msg, 0, 0));
    }

}
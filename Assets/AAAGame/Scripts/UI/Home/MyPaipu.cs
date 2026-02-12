using UnityEngine;
using NetMsg;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class MyPaipu : UIFormBase
{

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.Msg_MyPaiPuRs, FunctionMsg_MyPaiPuRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_CollectRoundRs, Function_CollectRound);

        Msg_MyPaiPuRq req = MessagePool.Instance.Fetch<Msg_MyPaiPuRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_MyPaiPuRq, req);
            
        Util.GetInstance().CheckSafeCodeState(gameObject);
    }


    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_MyPaiPuRs, FunctionMsg_MyPaiPuRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_CollectRoundRs, Function_CollectRound);
        base.OnClose(isShutdown, userData);
    }

    public GameObject content;
    public GameObject item;
    public GameObject item_bj;

    private void FunctionMsg_MyPaiPuRs(MessageRecvData messageRecvData)
    {
        Msg_MyPaiPuRs ack = Msg_MyPaiPuRs.Parser.ParseFrom(messageRecvData.Data);
        GF.LogInfo("收到牌谱列表" , ack.ToString());
        foreach (Transform item in content.transform)
        {
            Destroy(item.gameObject);
        }
        for (int i = 0; i < ack.PaiPus.Count; i++)
        {
            var data = ack.PaiPus[i];
            GameObject newItem = Instantiate(data.Method == MethodType.CompareChicken ? item_bj : item);
            newItem.transform.SetParent(content.transform, false);
            newItem.GetComponent<PaipuItem>().Init(data);
        }
    }

    public void Function_CollectRound(MessageRecvData data)
    {
        Msg_CollectRoundRs ack = Msg_CollectRoundRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到消息收藏结果" , ack.ToString());
        if (ack.State == 0)
        {
            foreach (Transform item in content.transform)
            {
                if (item.GetComponent<PaipuItem>().paipuInfo.ReportId == ack.ReportId)
                {
                    Destroy(item.gameObject);
                    break;
                }
            }
        }
    }

}

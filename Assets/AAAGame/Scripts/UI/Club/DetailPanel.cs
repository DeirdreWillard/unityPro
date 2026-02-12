using NetMsg;
using UnityEngine;
using UnityEngine.UI;
public class DetailPanel : MonoBehaviour
{
   
    // Start is called before the first frame update
    void Start()
    {
    }

    public void InitUI(){
        HotfixNetworkComponent.AddListener(MessageID.Msg_GoldMsgRs, FunctionMsg_Msg_GoldMsgRs);
        SendGoldMsgRq();
    }

    public void ClearUI(){
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_GoldMsgRs, FunctionMsg_Msg_GoldMsgRs);
    }

    public long playerId;
  
    public GameObject itemPrefab;
    public GameObject itemContainer;
    public void SendGoldMsgRq()
    {
        
        Msg_GoldMsgRq req = MessagePool.Instance.Fetch<Msg_GoldMsgRq>();
        req.PlayerId = playerId;
     
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GoldMsgRq, req);
    }

    public void FunctionMsg_Msg_GoldMsgRs(MessageRecvData data)
    {
        Msg_GoldMsgRs ack = Msg_GoldMsgRs.Parser.ParseFrom(data.Data);
        foreach (Transform item in itemContainer.transform)
        {
            Destroy(item.gameObject);
        }

        for (int i = 0; i < ack.Coins.Count; i++)
        {
            GameObject newItem = Instantiate(itemPrefab);
            newItem.transform.SetParent(itemContainer.transform, false);

            ShowItem(ack.Coins[i], newItem);
        }

    }
    public void ShowItem(Msg_Coin msg_Coin,GameObject item)
    {
        
        item.transform.Find("opTime").GetComponent<Text>().text = msg_Coin.OpTime.ToString("hh:mm:ss");
        
        item.transform.Find("title").GetComponent<Text>().text = msg_Coin.Title;
        item.transform.Find("changeNum").GetComponent<Text>().text = msg_Coin.ChangeNum;
        item.transform.Find("changeNum").GetComponent<Text>().color = long.Parse(msg_Coin.ChangeNum) > 0 ? Color.red : Color.green;
        item.transform.Find("afterNum").GetComponent<Text>().text = msg_Coin.AfterNum;

    
       
    }
}


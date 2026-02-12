

using UnityEngine.UI;

using NetMsg;
using UnityEngine;
using System.Collections.Generic;


public class UnionDetailItem : MonoBehaviour
{
    public Text nickName;
    public Text playerId;
    public Text opNickname;
    public Text title;
    public Text changeNum;
    public Text afterChange;
    public Text time;

    public List<GameObject> amounts;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <param name="amountType">1欢乐豆 2联盟币 3 钻石</param>
    public void Init(Msg_ClubCapital data, AmountType amountType)
    {
        opNickname.text = "操作人：" + data.Operator;
        nickName.text = "交易对象：" + (data.OptionName == "" ? "系统" : data.OptionName);
        playerId.text = data.OptionId == 0 ? "" : "ID: " + data.OptionId;
        title.text = data.Title;
        changeNum.FormatRichText(float.Parse(data.ChangeNum));
        afterChange.text = "变动后: " + data.AfterChange;
        time.text = Util.MillisecondsToDateString(data.OpTime, "yyyy-MM-dd HH:mm:ss");
        for (int i = 0; i < amounts.Count; i++)
        {
            amounts[i].SetActive(i == (int)amountType - 1);
        }
    }
}

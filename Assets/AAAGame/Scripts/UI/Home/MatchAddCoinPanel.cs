
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class MatchAddCoinPanel : UIFormBase
{
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        varTitleText.text = Params.Get<VarString>("matchName");
        var初始筹码.text = "10000";
        var addCoinTable = GlobalManager.GetInstance().m_AddCoinTable;
        foreach (Transform item in varScroll.content.transform)
        {
            Destroy(item.gameObject);
        }
        for (int i = 0; i < addCoinTable.Count; i++)
        {
            GameObject newItem = Instantiate(varItem);
            newItem.transform.SetParent(varScroll.content.transform, false);
            newItem.transform.Find("Index").GetComponent<Text>().text = addCoinTable[i].Level.ToString();
            newItem.transform.Find("Count").GetComponent<Text>().text = addCoinTable[i].SmallBlind.ToString();
            newItem.transform.Find("AddCount").GetComponent<Text>().text = addCoinTable[i].AddTime/60 + "分钟";
            if(i == 4){
                newItem.transform.Find("Stop").gameObject.SetActive(true);
            }
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
    }

}

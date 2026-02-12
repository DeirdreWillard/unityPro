using UnityEngine;
using UnityEngine.UI;
using System;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class GameRecord : UIFormBase
{

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        ShowGameRecord();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
    }


    //显示战绩信息

    public GameObject timeObj;
    public GameObject content;
    public GameObject itemPrefab;
    public void ShowGameRecord()
    {
        //创建列表
        foreach (Transform item in content.transform)
        {
            Destroy(item.gameObject);
        }
        var filteredDict = RecordManager.GetInstance().GetGameRecordRsDic_NOMj();
        foreach (var VARIABLE in filteredDict)
        {
            GameObject timeObjTemp = Instantiate(timeObj);
            timeObjTemp.transform.SetParent(content.transform, false);
            DateTime time = VARIABLE.Key;
            timeObjTemp.transform.GetChild(0).GetComponent<Text>().text = VARIABLE.Key.Year + "年"
                + VARIABLE.Key.Month + "月" + VARIABLE.Key.Day + "日";
            VARIABLE.Value.Sort((x, y) => y.RecordTime.CompareTo(x.RecordTime));
            for (int i = 0; i < VARIABLE.Value.Count; i++)
            {
                GameObject newItem = Instantiate(itemPrefab);
                newItem.transform.SetParent(content.transform, false);
                newItem.GetComponent<RecordItem>().Init(VARIABLE.Value[i]);
            }
        }
    }
}

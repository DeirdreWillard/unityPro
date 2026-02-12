using UnityEngine;
using UnityEngine.UI;
using System;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public  partial class MJGameRecord : UIFormBase
{
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        ShowGameRecord();
        for (int i = 0; i < varRecordContent.transform.childCount; i++)
        {
            var item = varRecordContent.transform.GetChild(i).gameObject;
            item.GetComponent<Toggle>().onValueChanged.AddListener((isOn) =>
            {
                Util.TestToggleChanged(item.name, varGameRule);
            });
        }
        //请求我的战绩
        
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
    }


    //显示战绩信息

    public void ShowGameRecord()
    {
    }
}

using UnityEngine;
using UnityEngine.UI;
using GameFramework.Event;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class BringDialog : UIFormBase
{
    public Toggle toggle1;
    public Toggle toggle2;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        
        toggle1.onValueChanged.AddListener(Ontoggle1Changed);
        toggle2.onValueChanged.AddListener(Ontoggle2Changed);
    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
     
        GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);

        ShowBringList();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {

        GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);

        base.OnClose(isShutdown, userData);
    }

    public void Ontoggle1Changed(bool isOn)
    {
        if (isOn)
        {
            ShowBringList();
        };
    }
    public void Ontoggle2Changed(bool isOn)
    {
        if (isOn)
        {
            ShowBringListOver();
        };
    }
    //显示带入申请
    public GameObject content;
    public GameObject itemPrefab;
    public void ShowBringList()
    {
        var bringInfoList = BringManager.GetInstance().GetBringInfoList();
        //创建列表
        foreach (Transform item in content.transform)
        {
            Destroy(item.gameObject);
        }
        for (int i = 0; i < bringInfoList.Count; i++)
        {
            GameObject newItem = Instantiate(itemPrefab);
            newItem.transform.SetParent(content.transform, false);
            newItem.GetComponent<BringItem>().Init(bringInfoList[i]);
        }
    }
    public GameObject contentOver;
    public void ShowBringListOver()
    {
        var bringInfoListOver = BringManager.GetInstance().GetBringInfoListOver();
        //创建列表
        foreach (Transform item in contentOver.transform)
        {
            Destroy(item.gameObject);
        }
        for (int i = 0; i < bringInfoListOver.Count; i++)
        {
            GameObject newItem = Instantiate(itemPrefab);
            newItem.transform.SetParent(contentOver.transform, false);
            newItem.GetComponent<BringItem>().Init(bringInfoListOver[i]);
        }
    }

    private void OnUserDataChanged(object sender, GameEventArgs e)
    {
        var args = e as UserDataChangedEventArgs;
        switch (args.Type)
        {
            case UserDataType.Msg:
                ShowBringList();
                GF.LogInfo("收到消息变更");
                break;
        }
    }


}

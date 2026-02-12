﻿using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class HomeNotification : UIFormBase
{
    public Text title;
    public Text content;
    
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        //string title, string content, System.Action onOk, System.Action onCancel
        title.text = Params.Get<VarString>("title");
        content.text = Params.Get<VarString>("content");
        LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());
    }
}

using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class LoadingDialog : UIFormBase
{
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        //string title, string content, System.Action onOk, System.Action onCancel
        // varTitle.text = Params.Get<VarString>("Title");             // 设置标题
        varContent.text = Params.Get<VarString>("Content");         // 设置内容
    }

    public void UpdateContent(string content)
    {
        varContent.text = content;
    }
    
}

using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class ConfirmationDialog : UIFormBase
{
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        //string title, string content, System.Action onOk, System.Action onCancel
        varTitle.text = Params.Get<VarString>("Title");             // 设置标题
        varContent.text = Params.Get<VarString>("Content");         // 设置内容
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        GF.UI.Close(this.UIForm);
    }
}

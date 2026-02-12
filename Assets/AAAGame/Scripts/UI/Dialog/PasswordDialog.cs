using System.Text.RegularExpressions;
using NetMsg;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class PasswordDialog : UIFormBase
{
    public int type = 0;//1修改密码  2修改钱包密码
    public Toggle toggle1;
    public Toggle toggle2;
    public Toggle toggle3;

    public InputField inputField1;
    public InputField inputField2;
    public InputField inputField3;
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        var data = Params.Get<VarString>("TextTitle");
        transform.Find("Title/TextTitle").GetComponent<Text>().text = data;
        if (data == "修改登录密码"){
            varPwdTips.text = "*可输入6-12位字母与数字";
            type = 1;
        }
        else if (data == "修改钱包密码"){
            varPwdTips.text = "*可输入6位数字钱包密码";
            type = 2;
        }
        toggle1.onValueChanged.AddListener(Ontog1ValueChanged);
        toggle2.onValueChanged.AddListener(Ontog2ValueChanged);
        toggle3.onValueChanged.AddListener(Ontog3ValueChanged);
        inputField1.text = inputField2.text = inputField3.text = "";
        toggle1.isOn = false;
        toggle2.isOn = false;
        toggle3.isOn = false;
        Ontog1ValueChanged(false);
        Ontog2ValueChanged(false);
        Ontog3ValueChanged(false);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SetSafeCodeRs, Msg_SetSafeCodeRsFunction);
        HotfixNetworkComponent.AddListener(MessageID.Msg_ModifyPwdRs, Msg_ModifyPwdRsFunction);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SetSafeCodeRs, Msg_SetSafeCodeRsFunction);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_ModifyPwdRs, Msg_ModifyPwdRsFunction);
        base.OnClose(isShutdown, userData);
    }
    void Ontog1ValueChanged(bool isOn)
    {
        varHide_oldCode.SetActive(!isOn);
        varShow_oldCode.SetActive(isOn);
        inputField1.contentType = isOn ? InputField.ContentType.Standard : InputField.ContentType.Password;
        inputField1.ForceLabelUpdate();
    }
    void Ontog2ValueChanged(bool isOn)
    {
        varHide_newCode.SetActive(!isOn);
        varShow_newCode.SetActive(isOn);
        inputField2.contentType = isOn ? InputField.ContentType.Standard : InputField.ContentType.Password;
        inputField2.ForceLabelUpdate();
    }
    void Ontog3ValueChanged(bool isOn)
    {
        varHide_sureCode.SetActive(!isOn);
        varShow_sureCode.SetActive(isOn);
        inputField3.contentType = isOn ? InputField.ContentType.Standard : InputField.ContentType.Password;
        inputField3.ForceLabelUpdate();
    }

    protected override async void OnButtonClick(object sender, string btId)
    {
        switch (btId)
        {
            case "忘记密码":
                var uiParams = UIParams.Create();
                uiParams.Set<VarString>("TextTitle", type == 1 ? "忘记登录密码" : "忘记安全密码");
                await GF.UI.OpenUIFormAwait(UIViews.ForgetPasswordDialog, uiParams);
                break;
            case "完成":
                if (type == 1) {
                    GF.LogInfo("设置登录密码");
                    if (string.IsNullOrEmpty(inputField1.text) ||
                    string.IsNullOrEmpty(inputField2.text) ||
                    string.IsNullOrEmpty(inputField3.text)
                    )
                    {
                        GF.UI.ShowToast("请输入密码");
                        return;
                    }
                    if (inputField1.text.Length < 6 || inputField1.text.Length > 12 ||
                    inputField2.text.Length < 6 || inputField2.text.Length > 12 ||
                    inputField3.text.Length < 6 || inputField3.text.Length > 12
                    )
                    {
                        GF.UI.ShowToast("密码长度必须在6到12个字符之间");
                        return;
                    }
                        if (!Util.IsValidPasswordCharacters(inputField2.text))
                {
                    GF.UI.ShowToast("密码只能包含英文字母、数字和符号，不能包含汉字");
                    return;
                }
                    if (inputField2.text != inputField3.text)
                    {
                        GF.UI.ShowToast("两次输入密码不一致", 2);
                        return;
                    }
                    Msg_ModifyPwdRq req = MessagePool.Instance.Fetch<Msg_ModifyPwdRq>();
                    req.NewPwd = inputField3.text;
                    req.OldPwd = inputField1.text;
                    req.ConfigurePwd = inputField3.text;
                    HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                        (HotfixNetworkComponent.AccountClientName, MessageID.Msg_ModifyPwdRq, req);
                }
                else if (type == 2)
                {
                    GF.LogInfo("设置安全密码");
                    if (inputField1.text.Length != 6 || !Regex.IsMatch(inputField1.text, @"^\d{6}$") ||
                    inputField2.text.Length != 6 || !Regex.IsMatch(inputField2.text, @"^\d{6}$") ||
                    inputField3.text.Length != 6 || !Regex.IsMatch(inputField3.text, @"^\d{6}$")
                    )
                    {
                        GF.UI.ShowToast("密码为6位数字");
                        return;
                    }
                    if (inputField2.text != inputField3.text)
                    {
                        GF.UI.ShowToast("两次输入密码不一致",2);
                        return;
                    }
                    Msg_SetSafeCodeRq req = MessagePool.Instance.Fetch<Msg_SetSafeCodeRq>();
                    req.SafeCode = inputField3.text;
                    req.OldCode = inputField1.text;
                    HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                        (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SetSafeCodeRq, req);
                }
                break;
        }
    }

    void Msg_SetSafeCodeRsFunction(MessageRecvData data)
    {
        GF.LogInfo("安全码修改成功");
        Msg_SetSafeCodeRs ack = Msg_SetSafeCodeRs.Parser.ParseFrom(data.Data);
        GF.UI.ShowToast("钱包密码修改成功",2);
        GF.UI.Close(this.UIForm);
    }

    void Msg_ModifyPwdRsFunction(MessageRecvData data)
    {
        GF.LogInfo("登录密码修改成功");
        GF.UI.ShowToast("登录密码修改成功",2);
        GF.UI.Close(this.UIForm);
    }
}

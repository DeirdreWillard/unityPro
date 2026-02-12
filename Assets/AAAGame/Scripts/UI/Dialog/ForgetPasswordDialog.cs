using System.Text.RegularExpressions;
using DG.Tweening;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class ForgetPasswordDialog : UIFormBase
{
    // 发送验证码的时间间隔
    private float countdownTime = 60f;
    private float currentTime;
    private bool isCountingDown = false;
    private int type = 0; // 1:忘记登录密码 2:忘记安全密码
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        HotfixNetworkComponent.AddListener(MessageID.Msg_RegisterCodeRs, Function_RegisterCodeRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_FindPwdRs, Function_FindPwdRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_FindSafePwdRs, Function_FindSafePwdRs);
        varPhoneToggle.onValueChanged.AddListener(PhoneValueChanged);

        var data = Params.Get<VarString>("TextTitle");
        transform.Find("Title/TextTitle").GetComponent<Text>().text = data;
        if (data == "忘记登录密码"){
            varPwdInput.characterLimit = 12;
            varPwdInput.contentType = InputField.ContentType.Password;
            varTips.text = "*可输入6-12位字母与数字";
            type = 1;
        }
        else if (data == "忘记安全密码"){
            varPwdInput.characterLimit = 6;
            varPwdInput.contentType = InputField.ContentType.IntegerNumber;
            varTips.text =  "*可输入6位数字钱包密码";
            type = 2;
        }
        varCodeInput.text = varEmailInput.text = varPhoneInput.text = varPwdInput.text = "";
        varPhoneToggle.isOn = true;
        PhoneValueChanged(true);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        varTimeText.text = "发送";
        StopCoroutine(CountdownCoroutine());
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_RegisterCodeRs, Function_RegisterCodeRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_FindPwdRs, Function_FindPwdRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_FindSafePwdRs, Function_FindSafePwdRs);
        varPhoneToggle.onValueChanged.RemoveAllListeners();
        base.OnClose(isShutdown, userData);
    }

    void Function_RegisterCodeRs(MessageRecvData data)
    {
        Msg_RegisterCodeRs ack = Msg_RegisterCodeRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("验证码发送成功" , ack.ToString());
        GF.UI.ShowToast("验证码发送成功", 2);
        StartCountdown();
    }
    void Function_FindPwdRs(MessageRecvData data)
    {
        Msg_FindPwdRs ack = Msg_FindPwdRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("登录密码设置成功", ack.ToString());

        PlayerPrefs.SetString("pwd", Util.Encrypt(varPwdInput.text, Const.PWD_KEY));
        PlayerPrefs.Save();
        GF.UI.ShowToast("密码修改成功!");

        if (Params.TryGet<VarGameObject>("go", out VarGameObject loginCache))
        {
            if (loginCache.Value.TryGetComponent(out StartPanel startPanel))
            {
                startPanel.InitUI();
                GlobalManager.GetInstance().AutoLogin(true);
            }
        }
        GF.UI.Close(this.UIForm);
    }

    void Function_FindSafePwdRs(MessageRecvData data)
    {
        Msg_FindSafePwdRs ack = Msg_FindSafePwdRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("钱包密码设置成功" , ack.ToString());
        GF.UI.ShowToast("钱包密码设置成功", 2);
        GF.UI.Close(this.UIForm);
    }

    void PhoneValueChanged(bool isPhone)
    {
        varPhoneInput.gameObject.SetActive(isPhone);
        varEmailInput.gameObject.SetActive(!isPhone);
        varLine.transform.DOLocalMoveX(isPhone ? -140 : 140, 0.1f);
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        switch (btId)
        {
            case "DetailsBtn":
                OnSendCodeButtonClicked();
                break;
            case "RegisterBtn":
                string email = varEmailInput.text;
                string phone = varPhoneInput.text;
                string code = varCodeInput.text;
                string password = varPwdInput.text;

                if (varPhoneToggle.isOn && string.IsNullOrEmpty(phone))
                {
                    GF.UI.ShowToast("请输入手机号");
                    return;
                }
                if (!varPhoneToggle.isOn && string.IsNullOrEmpty(email))
                {
                    GF.UI.ShowToast("请输入邮箱");
                    return;
                }
                if (!AppSettings.Instance.DebugMode)
                {
                    if (varPhoneToggle.isOn && !Util.ValidatePhoneNumber(phone))
                    {
                        GF.UI.ShowToast("请输入有效的11位手机号");
                        return;
                    }

                    if (!varPhoneToggle.isOn && !Util.ValidateEmail(email))
                    {
                        GF.UI.ShowToast("请输入有效的邮箱地址");
                        return;
                    }
                }

                if (string.IsNullOrEmpty(code))
                {
                    GF.UI.ShowToast("请输入验证码");
                    return;
                }

                if (type == 1)
                {
                    if (string.IsNullOrEmpty(password))
                    {
                        GF.UI.ShowToast("请输入密码");
                        return;
                    }
                    if (password.Length < 6 || password.Length > 12)
                    {
                        GF.UI.ShowToast("密码长度必须在6到12个字符之间");
                        return;
                    }
                     // 检查密码字符类型：只能包含英文字母、数字和符号，不能包含汉字
                if (!Util.IsValidPasswordCharacters(password))
                {
                    GF.UI.ShowToast("密码只能包含英文字母、数字和符号，不能包含汉字");
                    return;
                }
                    Msg_FindPwdRq req = MessagePool.Instance.Fetch<Msg_FindPwdRq>();
                    req.Account = varPhoneToggle.isOn ? phone : email;
                    // req.LoginType = varPhoneToggle.isOn ? 0 : 1;
                    req.Pwd = password;
                    req.Code = code;
                    HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                        (HotfixNetworkComponent.AccountClientName, MessageID.Msg_FindPwdRq, req);
                }
                else if (type == 2)
                {
                    if (string.IsNullOrEmpty(password))
                    {
                        GF.UI.ShowToast("请输入密码");
                        return;
                    }
                    if (password.Length != 6 || !Regex.IsMatch(password, @"^\d{6}$"))
                    {
                        GF.UI.ShowToast("密码为6位数字");
                        return;
                    }
                    Msg_FindSafePwdRq req = MessagePool.Instance.Fetch<Msg_FindSafePwdRq>();
                    // req.Account = varPhoneToggle.isOn ? phone : email;
                    // req.LoginType = varPhoneToggle.isOn ? 0 : 1;
                    req.Pwd = password;
                    req.Code = code;
                    HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                        (HotfixNetworkComponent.AccountClientName, MessageID.Msg_FindSafePwdRq, req);
                }
                
                break;
        }
    }

    public async void OnSendCodeButtonClicked()
    {
        if (isCountingDown) return; // 如果在倒计时中，避免重复发送

        string email = varEmailInput.text;
        string phone = varPhoneInput.text;

        if (varPhoneToggle.isOn && string.IsNullOrEmpty(phone))
        {
            GF.UI.ShowToast("请输入手机号");
            return;
        }
        if (!varPhoneToggle.isOn && string.IsNullOrEmpty(email))
        {
            GF.UI.ShowToast("请输入邮箱");
            return;
        }

        if (!AppSettings.Instance.DebugMode)
        {
            if (varPhoneToggle.isOn && !Util.ValidatePhoneNumber(phone))
            {
                GF.UI.ShowToast("请输入有效的11位手机号");
                return;
            }

            if (!varPhoneToggle.isOn && !Util.ValidateEmail(email))
            {
                GF.UI.ShowToast("请输入有效的邮箱地址");
                return;
            }
        }

        // 打开验证码验证界面
        var uiParams = UIParams.Create();
        uiParams.ButtonClickCallback = (sender, btId) =>
        {
            switch (btId)
            {
                case "Yes":
                    OnCaptchaVerified(true);
                    break;
                case "No":
                    OnCaptchaVerified(false);
                    break;
            }
        };
        await GF.UI.OpenUIFormAwait(UIViews.ArithmeticCaptchaDialog, uiParams);
    }
    
    /// <summary>
    /// 验证码验证完成回调
    /// </summary>
    /// <param name="_isVerified">验证是否成功</param>
    public void OnCaptchaVerified(bool _isVerified)
    {
        if (_isVerified)
        {
            // 验证成功，发送验证码
            string email = varEmailInput.text;
            string phone = varPhoneInput.text;
            
            Msg_RegisterCodeRq req = MessagePool.Instance.Fetch<Msg_RegisterCodeRq>();
            req.AccountName = varPhoneToggle.isOn ? phone : email;
            req.LoginType = varPhoneToggle.isOn ? 0 : 1;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_RegisterCodeRq, req);
        }
    }

    private void StartCountdown()
    {
        currentTime = countdownTime;
        isCountingDown = true;
        varTimeText.text = $"{currentTime}秒";

        CoroutineRunner.Instance.RunCoroutine(CountdownCoroutine());
    }

    private System.Collections.IEnumerator CountdownCoroutine()
    {
        while (currentTime > 0)
        {
            yield return new WaitForSeconds(1f);
            currentTime--;
            varTimeText.text = $"{currentTime}秒";
        }

        isCountingDown = false;
        varTimeText.text = "发送";
    }

}

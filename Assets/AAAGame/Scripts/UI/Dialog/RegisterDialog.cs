using DG.Tweening;
using NetMsg;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class RegisterDialog : UIFormBase
{
    // 发送验证码的时间间隔
    private float countdownTime = 60f;
    private float currentTime;
    private bool isCountingDown = false;
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        HotfixNetworkComponent.AddListener(MessageID.Msg_RegisterCodeRs, Function_RegisterCodeRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_RegisterRs, Function_RegisterRs);
        varPhoneToggle.onValueChanged.AddListener(PhoneValueChanged);

        varCodeInput.text = varEmailInput.text = varPhoneInput.text = varPwdInput.text = "";
        varPhoneToggle.isOn = true;
        currentTime = 0;
        isCountingDown = false;
        varTimeText.text = "发送";
        PhoneValueChanged(true);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        varTimeText.text = "发送";
        StopCoroutine(CountdownCoroutine());
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_RegisterCodeRs, Function_RegisterCodeRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_RegisterRs, Function_RegisterRs);
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
    void Function_RegisterRs(MessageRecvData data)
    {
        Msg_RegisterRs ack = Msg_RegisterRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("注册成功" , ack.ToString());
        GF.UI.ShowToast("注册成功", 2);

        PlayerPrefs.SetString(varPhoneToggle.isOn ? "phone" : "email", ack.AccountName);
        PlayerPrefs.SetString("pwd", Util.Encrypt(ack.Pwd, Const.PWD_KEY));
        PlayerPrefs.SetInt("loginType", varPhoneToggle.isOn ? 0 : 1);
        PlayerPrefs.Save();

        if (Params.TryGet<VarGameObject>("go", out VarGameObject loginCache))
        {
            GameObject login = loginCache.Value;
            login.GetComponent<StartPanel>().InitUI();
            GlobalManager.GetInstance().AutoLogin(true);
        }
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

                if (string.IsNullOrEmpty(code))
                {
                    GF.UI.ShowToast("请输入验证码");
                    return;
                }

                if (string.IsNullOrEmpty(password))
                {
                    GF.UI.ShowToast("请输入密码");
                    return;
                }
                
                if (password.Length < 6 || password.Length > 12 )
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
                Msg_RegisterRq req = MessagePool.Instance.Fetch<Msg_RegisterRq>();
                req.AccountName = varPhoneToggle.isOn ? phone : email;
                req.LoginType = varPhoneToggle.isOn ? 0 : 1;
                req.Pwd = password;
                req.Code = code;
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                    (HotfixNetworkComponent.AccountClientName, MessageID.Msg_RegisterRq, req);
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

        StartCoroutine(CountdownCoroutine());
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

    /// <summary>
    /// 验证密码字符是否有效（只能包含英文字母、数字和符号，不能包含汉字）
    /// </summary>
    /// <param name="password">要验证的密码</param>
    /// <returns>true表示字符有效，false表示包含无效字符</returns>
    

}

using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using UnityGameFramework.Runtime;
using DG.Tweening;
using System.Collections;
using System;


/// <summary>
/// 开始面板，处理用户登录、注册等操作。
/// </summary>
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class StartPanel : UIFormBase
{
    public Toggle yinsiTogg;

    #region Constants
    private const string c_PhoneKey = "phone";
    private const string c_EmailKey = "email";
    private const string c_PwdKey = "pwd";
    private const string c_LoginTypeKey = "loginType";
    #endregion
    
    #region Private Fields
    
    private int m_LoginType = 0; // 0: 手机, 1: 邮箱
    private StartProcedure m_StartProcedure;
    #endregion

    #region Unity Lifecycle & UI Callbacks
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        
        varPhoneToggle.onValueChanged.AddListener(OnLoginTypeChanged);
        
        CoroutineRunner.Instance.RunCoroutine(GetLocationInfo());
        InitUI();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        if (varPhoneToggle != null)
        {
            varPhoneToggle.onValueChanged.RemoveAllListeners();
        }
        base.OnClose(isShutdown, userData);
    }
    
    protected override async void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        var ui_params = UIParams.Create();
        switch (btId)
        {
            case "Login":
                HandleLogin();
                break;
            case "OpenSigninPanel":
                //打开注册界面
                ui_params.Set<VarGameObject>("go", gameObject);
                await GF.UI.OpenUIFormAwait(UIViews.RegisterDialog, ui_params);
                break;
            case "ForgetPassword":
                //打开忘记密码界面
                ui_params.Set<VarGameObject>("go", gameObject);
                ui_params.Set<VarString>("TextTitle", "忘记登录密码");
                await GF.UI.OpenUIFormAwait(UIViews.ForgetPasswordDialog, ui_params);
                break;
        }
    }

    public override void OnClickClose()
    {
        if (Util.IsClickLocked()) return;
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        Util.GetInstance().OpenConfirmationDialog("退出游戏", "确定要退出游戏吗?", Application.Quit);
    }
    #endregion
    
    #region Private Methods

    private IEnumerator GetLocationInfo()
    {
        // 使用通用GPS获取方法
        return Util.GetGPSLocation((gpsString) => {
            // Can process gpsString here if needed.
        });
    }

    /// <summary>
    /// 从PlayerPrefs加载已保存的登录信息并更新UI。
    /// </summary>
    public void InitUI()
    {
        varPhoneAccount.text = PlayerPrefs.GetString(c_PhoneKey);
        varEmailAccount.text = PlayerPrefs.GetString(c_EmailKey);

        if (PlayerPrefs.HasKey(c_LoginTypeKey))
        {
            int savedLoginType = PlayerPrefs.GetInt(c_LoginTypeKey);
            if (savedLoginType == 0)
            {
                varPhoneToggle.isOn = true;
            }
            else
            {
                varEmailToggle.isOn = true;
            }
            OnLoginTypeChanged(varPhoneToggle.isOn);
        }
        else
        {
            // 如果没有保存过登录类型，则根据Toggle的默认状态初始化
            OnLoginTypeChanged(varPhoneToggle.isOn);
        }
    }

    /// <summary>
    /// 当登录方式（手机/邮箱）的Toggle变化时调用。
    /// </summary>
    /// <param name="_isPhone">是否选择了手机登录。</param>
    private void OnLoginTypeChanged(bool _isPhone)
    {
        varPhoneAccount.gameObject.SetActive(_isPhone);
        varEmailAccount.gameObject.SetActive(!_isPhone);
        varLine.transform.DOLocalMoveX(_isPhone ? -140 : 140, 0.1f);
        m_LoginType = _isPhone ? 0 : 1;
        
        // 切换登录方式时，也更新密码输入框中显示的对应密码
        string passwordKey = c_PwdKey;
        string storedPassword = PlayerPrefs.GetString(passwordKey);

        if (!string.IsNullOrEmpty(storedPassword))
        {
            try
            {
                // 尝试解密。如果成功，则显示解密后的密码。
                varPwdInput.text = Util.Decrypt(storedPassword, Const.PWD_KEY);
            }
            catch (System.Exception)
            {
                // 如果解密失败，说明可能是未加密的旧密码，直接显示。
                varPwdInput.text = storedPassword;
            }
        }
        else
        {
            varPwdInput.text = string.Empty;
        }
    }

    /// <summary>
    /// 处理登录按钮点击事件的逻辑。
    /// </summary>
    private async void HandleLogin()
    {
        if (m_LoginType == 0)
        {
            if (m_LoginType == 0 && string.IsNullOrEmpty(varPhoneAccount.text))
            {
                GF.UI.ShowToast("请输入手机号");
                return;
            }
        }
        else
        {
        if (m_LoginType == 1 && string.IsNullOrEmpty(varEmailAccount.text))
            {
                GF.UI.ShowToast("请输入邮箱");
                return;
            }
        }
        if (string.IsNullOrEmpty(varPwdInput.text))
        {
            GF.UI.ShowToast("请输入密码");
            return;
        }
        // 登录前，检查是否切换了账号，如果是，则重置数据。
        string currentAccount = m_LoginType == 0 ? varPhoneAccount.text : varEmailAccount.text;
        string savedAccountKey = m_LoginType == 0 ? c_PhoneKey : c_EmailKey;
        if (PlayerPrefs.GetString(savedAccountKey) != currentAccount)
        {
            OtherPlayerInitData();
        }

        if (!GlobalManager.CanExecuteDailyAction("LoginCaptcha"))
        {
            // 今天已经验证过了，直接登录
            Login();
            return;
        }

        var ui_params = UIParams.Create();
        ui_params.ButtonClickCallback = (sender, btId) =>
        {
            if (btId == "Yes")
            {
                OnCaptchaVerified(true);
            }
        };
        await GF.UI.OpenUIFormAwait(UIViews.ArithmeticCaptchaDialog, ui_params);
    }
    
    /// <summary>
    /// 切换账号时，初始化相关数据。
    /// </summary>
    private void OtherPlayerInitData()
    {
        GlobalManager.ResetAllDailyActions();
    }
    #endregion
    
    #region Public Methods
    /// <summary>
    /// 验证码验证完成回调
    /// </summary>
    /// <param name="_isVerified">验证是否成功</param>
    public void OnCaptchaVerified(bool _isVerified)
    {
        if (_isVerified)
        {
            // 验证成功，记录操作并执行登录
            GlobalManager.RecordDailyAction("LoginCaptcha");
            Login();
        }
    }

    /// <summary>
    /// 构造并发送登录请求。
    /// </summary>
    public async void Login()
    {
        if (!yinsiTogg.isOn)
        {
            // GF.UI.ShowToast("请同意隐私协议", 2);
            // return;
        }
        GF.LogInfo("请求 登录 ");
        
        // 检查网络连接状态，如果未连接则先连接
        if (!HotfixNetworkManager.Ins.hotfixNetworkComponent.IsGameConnectOK())
        {
            GF.LogInfo("[StartPanel] 检测到未连接服务器，开始连接...");
            Util.GetInstance().ShowWaiting("正在连接服务器...", "login_connect");
            
            // 尝试连接服务器
            HotfixNetworkManager.Ins.ConnectServer();
            
            // 等待连接完成（最多等待10秒）
            float waitTime = 0f;
            const float maxWaitTime = 10f;
            while (!HotfixNetworkManager.Ins.hotfixNetworkComponent.IsGameConnectOK() && waitTime < maxWaitTime)
            {
                await Cysharp.Threading.Tasks.UniTask.Delay(TimeSpan.FromSeconds(0.1f));
                waitTime += 0.1f;
            }
            
            Util.GetInstance().CloseWaiting("login_connect");
            
            // 如果连接失败，提示用户
            if (!HotfixNetworkManager.Ins.hotfixNetworkComponent.IsGameConnectOK())
            {
                GF.UI.ShowToast("连接服务器失败，请检查网络");
                GF.LogError("[StartPanel] 连接服务器超时，无法发送登录请求");
                return;
            }
            
            GF.LogInfo("[StartPanel] 服务器连接成功，继续登录流程");
        }
        
        LoginOrCreateRq req = MessagePool.Instance.Fetch<LoginOrCreateRq>();
        
        req.AccountName = m_LoginType == 0 ? varPhoneAccount.text : varEmailAccount.text;
        req.Pwd = varPwdInput.text; 
        req.LoginType = m_LoginType;
        req.Uuid = Util.GetFriendlyDeviceInfo();
        req.Gps = $"{Input.location.lastData.latitude},{Input.location.lastData.longitude}";
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.MsgLoginReq, req);
    }


    /// <summary>
    /// 保存登录信息到PlayerPrefs。
    /// </summary>
    public void SaveLoginData()
    {
        if (m_LoginType == 0)
        {
            PlayerPrefs.SetString(c_PhoneKey, varPhoneAccount.text);
            PlayerPrefs.SetString(c_PwdKey, Util.Encrypt(varPwdInput.text, Const.PWD_KEY));
        }
        else // m_LoginType == 1
        {
            PlayerPrefs.SetString(c_EmailKey, varEmailAccount.text);
            PlayerPrefs.SetString(c_PwdKey, Util.Encrypt(varPwdInput.text, Const.PWD_KEY));
        }

        PlayerPrefs.SetInt(c_LoginTypeKey, m_LoginType);
        PlayerPrefs.Save();
    }
    #endregion
}

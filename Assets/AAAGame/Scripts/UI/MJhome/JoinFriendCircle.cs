using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using NetMsg;
using UnityEngine.Events;
using UnityGameFramework.Runtime;
using GameFramework.Event;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class JoinFriendCircle : UIFormBase
{
    [Header("输入显示框")]
    [SerializeField] private Text[] inputDisplayTexts; // 6个输入显示框

    [Header("数字按钮")]
    [SerializeField] private Button[] numberButtons;   // 数字按钮 0-9

    [Header("功能按钮")]
    [SerializeField] private Button deleteButton;      // 删除按钮
    [SerializeField] private Button clearButton;       // 清空按钮

    private StringBuilder inputNumber = new StringBuilder(); // 存储输入的数字
    private const int MAX_INPUT_LENGTH = 6; // 最大输入长度
    /// <summary>
    /// 0 亲友圈6位数 1 房间 5位数 2 二级密码6位数
    /// </summary>
    private int roomOrFriend;
    
    // 二级密码验证相关
    private UnityAction onPasswordSuccess; // 密码验证成功回调
    private UnityAction onPasswordFail;    // 密码验证失败回调

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        SetupButtonEvents();
        ClearAllInput();
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        
        // 获取密码验证回调参数
        Params.TryGet<VarObject>("OnSuccess", out VarObject successWrapper);
        if (successWrapper != null && successWrapper.Value is UnityAction)
        {
            onPasswordSuccess = successWrapper.Value as UnityAction;
        }
        
        Params.TryGet<VarObject>("OnLose", out VarObject failWrapper);
        if (failWrapper != null && failWrapper.Value is UnityAction)
        {
            onPasswordFail = failWrapper.Value as UnityAction;
        }
        
        // 监听网络消息
        HotfixNetworkComponent.AddListener(MessageID.Msg_Error, OnPasswordError);
        GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
    }

    /// 设置按钮事件
    /// </summary>
    private void SetupButtonEvents()
    {
        // 设置数字按钮事件

        for (int i = 0; i < numberButtons.Length; i++)
        {
            if (numberButtons[i] != null)
            {
                int number = i; // 捕获循环变量
                numberButtons[i].onClick.AddListener(() => OnNumberButtonClick(number));
            }
        }

        // 设置功能按钮事件
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(OnDeleteButtonClick);
        }

        if (clearButton != null)
        {
            clearButton.onClick.AddListener(OnClearButtonClick);
        }

    }

    /// <summary>
    /// 数字按钮点击事件
    /// </summary>
    /// <param name="number">点击的数字</param>
    private void OnNumberButtonClick(int number)
    {
        // 检查是否已达到最大输入长度
        if (inputNumber.Length >= MAX_INPUT_LENGTH)
        {
            return;
        }
        // 添加数字到输入字符串
        inputNumber.Append(number.ToString());
        // 更新显示
        UpdateDisplay();

        // 播放音效（如果需要）
        PlayClickSound();

        Debug.Log($"输入数字: {number}, 当前输入: {inputNumber}");

        if (roomOrFriend == 0 && inputNumber.Length == 6)
        {
            Debug.Log("已输入6位数字,自动尝试加入亲友圈");
            AutoJoinRoom();
        }
        else if (roomOrFriend == 1 && inputNumber.Length == 5)
        {
            inputDisplayTexts[5].gameObject.SetActive(true);
            Debug.Log("已输入5位数字,自动尝试加入房间");
            AutoJoinRoom();
        }
        else if (roomOrFriend == 2 && inputNumber.Length == 6)
        {
            Debug.Log("已输入6位数字,验证二级密码");
            VerifySecurityCode();
        }
    }

    /// <summary>
    /// 删除按钮点击事件
    /// </summary>
    private void OnDeleteButtonClick()
    {
        // 检查是否有内容可删除
        if (inputNumber.Length > 0)
        {
            // 删除最后一个字符
            inputNumber.Remove(inputNumber.Length - 1, 1);
            // 更新显示
            UpdateDisplay();
            // 播放音效
            PlayClickSound();
            Debug.Log($"删除一个数字, 当前输入: {inputNumber}");
        }
    }
    /// <summary>
    /// 清空按钮点击事件
    /// </summary>
    private void OnClearButtonClick()
    {
        // 清空所有输入
        ClearAllInput();

        // 播放音效
        PlayClickSound();

        Debug.Log("清空所有输入");
    }


    /// <summary>
    /// 更新显示
    /// </summary>
    private void UpdateDisplay()
    {
        string currentInput = inputNumber.ToString();

        // 更新每个显示框
        for (int i = 0; i < inputDisplayTexts.Length; i++)
        {
            if (inputDisplayTexts[i] != null)
            {
                if (i < currentInput.Length)
                {
                    // 显示对应位置的数字
                    inputDisplayTexts[i].text = currentInput[i].ToString();
                }
                else
                {
                    // 清空未输入的位置
                    inputDisplayTexts[i].text = "";
                }
            }
        }
    }

    /// <summary>
    /// 清空所有输入
    /// </summary>
    private void ClearAllInput()
    {
        // 清空输入字符串
        inputNumber.Clear();
        // 更新显示
        UpdateDisplay();
    }

    /// <summary>
    /// 播放点击音效
    /// </summary>
    private void PlayClickSound()
    {
        // 这里可以添加音效播放逻辑
        // AudioSource.PlayClipAtPoint(clickSound, Camera.main.transform.position);
    }
    /// <summary>
    /// 自动加入房间（当输入满6位数字时调用）
    /// </summary>
    private void AutoJoinRoom()
    {
        string roomCode = inputNumber.ToString();

        // 添加一个短暂的延迟，让用户看到最后一个数字的输入
        StartCoroutine(DelayedJoinRoom(roomCode, 0.5f));
    }

    /// <summary>
    /// 延迟加入房间
    /// </summary>
    /// <param name="roomCode">房间号</param>
    /// <param name="delay">延迟时间（秒）</param>
    /// <returns></returns>
    private System.Collections.IEnumerator DelayedJoinRoom(string roomCode, float delay)
    {

        yield return new WaitForSeconds(delay);
        if (roomOrFriend == 0)
        {
            JoinFriendRoom(roomCode);
        }
        else
        {
            JoinRoom(roomCode);
        }
    }

    /// <summary>
    /// 加入房间
    /// </summary>
    /// <param name="roomCode">房间号</param>
    private void JoinRoom(string roomCode)
    {
        // 这里添加加入房间的网络请求逻辑
        SetInputEnabled(false);
        //输入改为5位数 需要隐藏一个输入框
        Util.GetInstance().Send_EnterDeskRq(int.Parse(roomCode));
        ClearAllInput();
        SetInputEnabled(true);
        GF.UI.Close(this.UIForm);
    }
    /// <summary>
    /// 加入亲友圈
    /// </summary>
    /// <param name="roomCode"></param>
    private void JoinFriendRoom(string roomCode)
    {
        SetInputEnabled(false);
        Msg_JoinLeagueRq req = MessagePool.Instance.Fetch<Msg_JoinLeagueRq>();
        req.InviteCode = roomCode;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_JoinLeagueRq, req); ClearAllInput();
        SetInputEnabled(true);
        GF.UI.Close(this.UIForm);
    }

    /// <summary>
    /// 设置输入是否可用
    /// </summary>
    /// <param name="enabled">是否启用</param>
    private void SetInputEnabled(bool enabled)
    {
        // 设置数字按钮是否可用
        for (int i = 0; i < numberButtons.Length; i++)
        {
            if (numberButtons[i] != null)
            {
                numberButtons[i].interactable = enabled;
            }
        }

        // 设置功能按钮是否可用
        deleteButton.interactable = enabled;
        clearButton.interactable = enabled;

        // Debug.Log($"输入功能已{(enabled ? "启用" : "禁用")}");
    }

    /// <summary>
    /// 获取当前输入的字符串
    /// </summary>
    /// <returns>当前输入的字符串</returns>
    public string GetCurrentInput()
    {
        return inputNumber.ToString();
    }

    /// <summary>
    /// 设置输入内容（用于外部设置）
    /// </summary>
    /// <param name="input">要设置的输入内容</param>
    public void SetInput(string input)
    {
        if (input.Length <= MAX_INPUT_LENGTH)
        {
            inputNumber.Clear();
            inputNumber.Append(input);
            UpdateDisplay();
        }
    }

    //根据不同的点击来修改top图片  需要在外部调用
    public void ChangeTopImage(int i)
    {
        //面板存两张图片 加载预制体的时候更据i的值更换图片
        roomOrFriend = i;
        if (roomOrFriend == 1)
        {
            inputDisplayTexts[5].transform.parent.gameObject.SetActive(false);
            varTop.GetComponent<Text>().text = "请输入房间号";
        }
        else if (roomOrFriend == 2)
        {
            inputDisplayTexts[5].transform.parent.gameObject.SetActive(true);
            varTop.GetComponent<Text>().text = "请输入二级密码";
        }
        else
        {
            inputDisplayTexts[5].transform.parent.gameObject.SetActive(true);
            varTop.GetComponent<Text>().text = "请输入亲友圈号";
        }
    }
    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        
        // 移除消息监听
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_Error, OnPasswordError);
        GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
        
        // 清理回调
        onPasswordSuccess = null;
        onPasswordFail = null;
        
        ClearAllInput();
    }

    #region 二级密码验证相关方法
    
    /// <summary>
    /// 验证二级密码
    /// </summary>
    private void VerifySecurityCode()
    {
        string password = inputNumber.ToString();
        
        GF.LogInfo("验证二级密码: " + password);
        
        Msg_UnlockRq req = MessagePool.Instance.Fetch<Msg_UnlockRq>();
        req.SafeCode = password;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_UnlockRq, req);
    }
    
    /// <summary>
    /// 监听用户数据变化（密码验证成功）
    /// </summary>
    private void OnUserDataChanged(object sender, GameEventArgs e)
    {
        var args = e as UserDataChangedEventArgs;
        if (args?.Type == UserDataType.SafeCodeState)
        {
            if (GF.DataModel.GetDataModel<UserDataModel>().LockState == 0)
            {
                GF.LogInfo("二级密码验证成功");
                
                // 执行成功回调
                if (onPasswordSuccess != null && onPasswordSuccess.Target != null)
                {
                    onPasswordSuccess.Invoke();
                }
                
                // 关闭界面
                GF.UI.Close(this.UIForm);
            }
        }
    }
    /// <summary>
    /// 密码错误消息处理
    /// </summary>
    private void OnPasswordError(MessageRecvData data)
    {
        Msg_Error msg = Msg_Error.Parser.ParseFrom(data.Data);
        if (msg.Error == -22) // 密码错误
        {
            GF.LogWarning("二级密码验证失败");
            ClearAllInput();
            
            // 可以在这里添加提示
            // GF.UI.ShowTips("密码错误，请重新输入");
        }
    }
    #endregion

}

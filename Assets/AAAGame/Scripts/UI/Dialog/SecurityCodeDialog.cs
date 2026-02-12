﻿using GameFramework;
using GameFramework.Event;
using NetMsg;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class SecurityCodeDialog : UIFormBase
{
    public int[] myArray = new int[6] { -1, -1, -1, -1, -1, -1 };
    public int index = 0; //当前第几位
    public Text[] nums = new Text[6];

    private GameObject go;
    private VarObject actionWrapper_Scuccess;
    private VarObject actionWrapper_Lose;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        Params.TryGet<VarGameObject>("go" , out VarGameObject goTemp);
        if (goTemp != null)
            go = goTemp;
        else
            go = null;
        Params.TryGet<VarObject>("OnSuccess", out VarObject actionWrapperTemp);
        if (actionWrapperTemp != null)
            actionWrapper_Scuccess = actionWrapperTemp;
        else
            actionWrapper_Scuccess = null;
        Params.TryGet<VarObject>("OnLose", out VarObject actionWrapperTemp2);
        if (actionWrapperTemp2 != null)
            actionWrapper_Lose = actionWrapperTemp2;
        else
            actionWrapper_Lose = null;
        ClearInput();
        HotfixNetworkComponent.AddListener(MessageID.Msg_Error, FuncMsg_Error);
        GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        if (go && go.GetComponent<UIFormBase>() && GF.DataModel.GetDataModel<UserDataModel>().LockState == 1)
        {
            go.GetComponent<UIFormBase>().CloseWithAnimation();
        }
        if (actionWrapper_Lose != null && actionWrapper_Lose.Value is UnityAction && GF.DataModel.GetDataModel<UserDataModel>().LockState == 1)
        {
            // 添加空引用检查和对象存活验证
            if (actionWrapper_Lose.Value is UnityAction action && action.Target != null)
            {
                action.Invoke();
            }
            ReferencePool.Release(actionWrapper_Lose);
            actionWrapper_Lose = null; // 释放后置空
        }
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_Error, FuncMsg_Error);
        GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
    }

    public void ClearInput()
    {
        for (int i = 0; i < 6; i++)
        {
            nums[i].text = "";
            myArray[i] = -1;
        }
        index = 0;
    }

    public async void OnClick(string btId)
    {
        switch (btId)
        {
            case "0":
                ReshADD(0);
                break;
            case "1":
                ReshADD(1);
                break;
            case "2":
                ReshADD(2);
                break;
            case "3":
                ReshADD(3);
                break;
            case "4":
                ReshADD(4);
                break;
            case "5":
                ReshADD(5);
                break;
            case "6":
                ReshADD(6);
                break;
            case "7":
                ReshADD(7);
                break;
            case "8":
                ReshADD(8);
                break;
            case "9":
                ReshADD(9);
                break;
            case "X":
                ReshJian();
                break;
            case "忘记了":
                var uiParams = UIParams.Create();
                uiParams.Set<VarString>("TextTitle", "忘记安全密码");
                await GF.UI.OpenUIFormAwait(UIViews.ForgetPasswordDialog, uiParams);
                GF.UI.Close(this.UIForm);
                break;
        }
    }
    
    public void ReshADD(int i)
    {
        if (index < myArray.Length)
        {
            myArray[index] = i;
            index++;
            //如果索引在第6个的时候触发消息
            if (index == 6)
            {
                GF.LogInfo("解锁");
                Msg_UnlockRq req = MessagePool.Instance.Fetch<Msg_UnlockRq>();
                req.SafeCode = myArray[0] + "" + myArray[1] + "" + myArray[2] + "" + myArray[3] + "" + myArray[4] + "" +
                               myArray[5];
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                    (HotfixNetworkComponent.AccountClientName, MessageID.Msg_UnlockRq, req);
            }
        }

        ReshText();

    }

    private void OnUserDataChanged(object sender, GameEventArgs e)
    {
        var args = e as UserDataChangedEventArgs;
        switch (args.Type)
        {
            case UserDataType.SafeCodeState:
                if (actionWrapper_Scuccess != null && actionWrapper_Scuccess.Value is UnityAction && GF.DataModel.GetDataModel<UserDataModel>().LockState == 0)
                {
                    // 添加空引用检查和对象存活验证
                    if (actionWrapper_Scuccess.Value is UnityAction action && action.Target != null)
                    {
                        action.Invoke();
                    }
                    ReferencePool.Release(actionWrapper_Scuccess);
                    actionWrapper_Scuccess = null; // 释放后置空
                }
                GF.UI.Close(this.UIForm);
                break;
        }
    }

    public void FuncMsg_Error(MessageRecvData data)
    {
        Msg_Error msg = Msg_Error.Parser.ParseFrom(data.Data);
        if (msg.Error == -22)
        {
            ClearInput();
        }
    }

    public void ReshJian()
    {
        if (index > 0)
        {
            index--;
            myArray[index] = -1;
        }
        ReshText();
    }
    public void ReshText()
    {

        for (int i = 0; i < myArray.Length; i++)
        {
            if (myArray[i]==-1)
            {
                nums[i].text = "";
            }
            else
            {
                nums[i].text = myArray[i].ToString();
            }
        }
    }
   
}

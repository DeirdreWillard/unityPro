using System;
using System.Collections.Generic;
using Google.Protobuf;
using NetMsg;
using UnityGameFramework.Runtime;
using UnityEngine.UI;
using UnityEngine.VFX;
using TMPro;
using UnityEngine;
using System.Linq;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class MixedArrangement : UIFormBase

{
    private bool isOpenMixed = false;//是否打开混排开关
    private bool isHallOpen = false;//混排大厅状态是否启用
    private int mixedMode = 1; //混排模式 1手动添加 2自动添加  3防作弊
    private int tableNum = 1;//默认桌数
    private int openState = 1; //1另开新桌 2 人满开桌
    private int sortState = 1; //1按人数排序 2 按桌号排序  //排序方式 1(空桌 未满桌 满桌) 2(空桌 满桌 未满桌) 3(未满桌 空桌 满桌) 4(未满桌 满桌 空桌) 5(满桌 空桌 未满桌) 6(满桌 未满桌 空桌)
    private bool opAi = false;//智能筛选
    private int dValue = 0; //差值

    private List<Msg_HallInfo> hallList;
    private List<Msg_Floor> floorList;

    /// <summary>
    /// 获取正确的ClubId - 根据上级关系决定使用哪个ClubId
    /// </summary>
    /// <returns>正确的ClubId</returns>
    private long GetCorrectClubId()
    {
        var leagueInfo = GlobalManager.GetInstance().LeagueInfo;
        
        if (leagueInfo.Father == 0)
        {
            return leagueInfo.LeagueId;
        }
        else if (leagueInfo.Father != 0 && leagueInfo.FatherInfo.Father == 0)
        {
            return leagueInfo.Father;
        }
        else if (leagueInfo.Father != 0 && leagueInfo.FatherInfo.Father != 0)
        {
            return leagueInfo.FatherInfo.Father;
        }
        
        return leagueInfo.LeagueId; // 默认返回当前联盟ID
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        // 从 UIParams 获取数据
        if (Params != null)
        {
            floorList = Params.Get("floorList") as List<Msg_Floor>;
            hallList = Params.Get("hallList") as List<Msg_HallInfo>;
           // 验证数据完整性
            GF.LogInfo_wl($"=== 数据接收验证 ===");
            GF.LogInfo_wl($"floorList: {(floorList != null ? $"非空，数量: {floorList.Count}" : "为空")}");
            GF.LogInfo_wl($"hallList: {(hallList != null ? $"非空，数量: {hallList.Count}" : "为空")}");

        }

        if (hallList != null && hallList.Count > 0)
        {
            isOpenMixed = hallList[0].IsOpen;
            varIsOpenMixed.transform.Find("open").gameObject.SetActive(isOpenMixed);
        }
         InitializeUIState();
        // 添加网络监听
        HotfixNetworkComponent.AddListener(MessageID.Msg_SetHallFloorRs, OnSetHallFloorResponse);
    }

    /// <summary>
    /// 初始化UI状态
    /// </summary>
    private void InitializeUIState()
    {
        // 设置混排楼层开关状态
        if (varMixedFloor != null)
        {
            var enabledNode = varMixedFloor.transform.Find("Enabled");
            if (enabledNode != null)
            {
                enabledNode.gameObject.SetActive(isHallOpen);
            }
        }

        // 根据混排模式设置UI显示
        varManuallySet?.SetActive(mixedMode == 1);
        varTableOpeningMethod?.SetActive(mixedMode == 2);
        varSortBy?.SetActive(mixedMode == 2);
        varOpenAi?.SetActive(mixedMode == 3);

        GF.LogInfo_wl($"UI状态初始化完成: isHallOpen={isHallOpen}, mixedMode={mixedMode}");
    }

    private void OnSetHallFloorResponse(MessageRecvData data)
    {
        //大厅是否启用
        var go = Msg_SetHallFloorRs.Parser.ParseFrom(data.Data);
        isHallOpen = go.HallOpen;
        varMixedFloor.transform.Find("Enabled").gameObject.SetActive(go.HallOpen);
        //
        GF.LogInfo_wl("混排大厅设置成功");
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "混排开关":
                var open = varIsOpenMixed.transform.Find("open").gameObject;
                open.SetActive(!open.activeSelf);
                isOpenMixed = !isOpenMixed;
                GF.LogInfo_wl(isOpenMixed + "混排开关");
                break;
            case "混排楼层":
                //点击弹出预制体
                var uiParams = UIParams.Create();
                uiParams.Set("floorList", floorList);
                uiParams.Set("hallList", hallList);
                GF.UI.OpenUIFormAwait(UIViews.MJHallSet, uiParams);
                break;
            case "保存混排设置":
                //发送请求        
                Msg_SetHallRq req = MessagePool.Instance.Fetch<Msg_SetHallRq>();
                req.ClubId = GetCorrectClubId();
                req.IsOpen = isOpenMixed;
                req.Type = mixedMode;
                req.Hall = 1;
                req.DefDeskNum = tableNum;
                req.OpenState = openState;
                req.SortState = sortState;
                req.OpAi = opAi;
                req.DValue = dValue;
                //打印这些数值
                GF.LogInfo_wl("混排开关" + isOpenMixed 
                + "混排模式" + mixedMode + "桌数" + tableNum +
                 "开桌方式" + openState + "排序方式" + sortState +
                  "智能筛选" + opAi + "差值" + dValue);
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                      (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SetHallRq, req);
                GF.UI.Close(this.UIForm);
                break;
        }
    }
    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SetHallFloorRs, OnSetHallFloorResponse);

    }
    public void SetMixedMode(int num)
    {       //根据点击的toggle传入人数
        mixedMode = num;
        varManuallySet.SetActive(mixedMode == 1);
        varTableOpeningMethod.SetActive(mixedMode == 2);
        varSortBy.SetActive(mixedMode == 2);
        varOpenAi.SetActive(mixedMode == 3);
        GF.LogInfo_wl("混排模式" + num);
    }
    public void SetTableNum(int num)
    {       //根据点击的toggle传入人数
        tableNum = num;
        GF.LogInfo_wl("桌数" + num);
    }
    public void SetOpenState(int num)
    {       //根据点击的toggle传入人数
        openState = num;
        GF.LogInfo_wl("开桌方式" + num);
    }
    public void SetSortState(int num)
    {       //根据点击的toggle传入人数
        sortState = num;
        GF.LogInfo_wl("排序方式" + num);
    }
    public void SetOpAi(bool b)
    {
        opAi = b;
        GF.LogInfo_wl("智能筛选" + b);
    }
    public void SetDValue()
    {
        // 优先从 TMP_InputField 读取，其次从子节点 TMP_Text 读取

        string rawText = null;
        var input = varInputFieldTMP != null ? varInputFieldTMP.GetComponent<TMPro.TMP_InputField>() : null;
        if (input != null)
        {
            rawText = input.text;
        }
        else
        {
            var textNode = varInputFieldTMP != null ? varInputFieldTMP.transform.Find("Text Area/Text") : null;
            var textComp = textNode != null ? textNode.GetComponent<TMPro.TMP_Text>() : null;
            rawText = textComp != null ? textComp.text : null;
        }
        if (!int.TryParse(rawText, out dValue))
        {
            GF.LogInfo_wl($"差值解析失败: {rawText}");
            GF.UI.ShowToast("请输入正确的数值");
            //然后将数值默认为1
            dValue = 0;
            //然后将输入框中的数值改为1
            input.text = "0";

            return;
        }
        GF.LogInfo_wl("差值" + dValue);
    }
}

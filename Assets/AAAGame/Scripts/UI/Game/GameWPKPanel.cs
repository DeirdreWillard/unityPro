﻿using UnityEngine.UI;
using NetMsg;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityGameFramework.Runtime;
using DG.Tweening;


[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class GameWPKPanel : UIFormBase
{
    Desk_BaseConfig deskConfig;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        deskConfig = Desk_BaseConfig.Parser.ParseFrom(Params.Get<VarByteArray>("deskConfig"));

        varToggle1.onValueChanged.AddListener(OnToggle1Changed);
        varToggle2.onValueChanged.AddListener(OnToggle2Changed);
        HotfixNetworkComponent.AddListener(MessageID.Msg_BlackListRs, Function_BlackListRs);

        SendGetBlackListRq();
        varHelpObj.SetActive(false);
        UpdateTable(1);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
        varToggle1.onValueChanged.RemoveAllListeners();
        varToggle2.onValueChanged.RemoveAllListeners();
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_BlackListRs, Function_BlackListRs);
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "help":
                varHelpObj.SetActive(!varHelpObj.activeSelf);
                break;
        }
    }


    public void SendGetBlackListRq()
    {
        Msg_BlackListRq req = MessagePool.Instance.Fetch<Msg_BlackListRq>();
        req.LeagueId = deskConfig.ClubId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_BlackListRq, req);
    }

    private void Function_BlackListRs(MessageRecvData data)
    {
        // Msg_DeskListRs
        Msg_BlackListRs ack = Msg_BlackListRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到房间封禁列表返回:" , ack.ToString());
        varNum_30_text.text = ack.Record.Key.ToString();
        varNum_3_text.text = ack.Record.Val.ToString();
        // 创建列表
        foreach (Transform item in varContent.transform)
        {
            Destroy(item.gameObject);
        }
        var sortedReports = ack.BlackInfos;
        // var sortedReports = ack.BlackInfos
        //                    .OrderByDescending(report => report.ForbidTime)
        //                    .ToList();
        for (int i = 0; i < sortedReports.Count; i++)
        {
            var itemData = sortedReports[i];
            var playerInfo = itemData.PlayerInfo[0];
            if(playerInfo == null){
                continue;
            }
            if(itemData.BlackState == 1){
                GameObject newItem = Instantiate(varItem);
                newItem.transform.SetParent(varContent.transform, false);
                newItem.transform.Find("Nickname").GetComponent<Text>().text = playerInfo.Nick;
                newItem.transform.Find("PlayerId").GetComponent<Text>().text = $"ID:{playerInfo.PlayerId}";
                newItem.transform.Find("Time").GetComponent<Text>().text = $"封禁时间: {Util.MillisecondsToDateString(itemData.ForbidTime,"yyyy/MM/dd")}";
                Util.DownloadHeadImage(newItem.transform.Find("Mask/Head").GetComponent<RawImage>(), playerInfo.HeadImage);
            }
            else{
                GameObject newItem = Instantiate(varItem2);
                newItem.transform.SetParent(varContent.transform, false);
                if (itemData.PlayerInfo != null && itemData.PlayerInfo.Count > 0){
                    newItem.transform.Find("Time").GetComponent<Text>().text = $"封禁时间: {Util.MillisecondsToDateString(itemData.ForbidTime, "yyyy/MM/dd")}";
                    for (int j = 0; j < 4; j++) {
                        var player = newItem.transform.Find("Player" + j);
                        if (playerInfo == null || itemData.PlayerInfo.Count < j + 1)
                        {
                            player.gameObject.SetActive(false);
                            continue;
                        }
                        playerInfo = itemData.PlayerInfo[j];
                        player.gameObject.SetActive(true);
                        player.Find("Nickname").GetComponent<Text>().FormatNickname(playerInfo.Nick);
                        Util.DownloadHeadImage(player.Find("Mask/Head").GetComponent<RawImage>(), playerInfo.HeadImage);
                        player.Find("PlayerId").GetComponent<Text>().text = $"ID:{playerInfo.PlayerId}";
                    }
                }
            }
        }
    }

    private void OnToggle1Changed(bool isOn)
    {
        if (isOn)
            UpdateTable(1);
    }
    private void OnToggle2Changed(bool isOn)
    {
        if (isOn)
            UpdateTable(2);
    }

    void UpdateTable(int type)
    {
        varPanel1.SetActive(type == 1);
        varPanel2.SetActive(type == 2);
        varCheckShow1.SetActive(type == 1);
        varCheckShow2.SetActive(type == 2);
        varLine.transform.DOLocalMoveX(type == 1 ? -233 : 233, 0.1f);
    }
}

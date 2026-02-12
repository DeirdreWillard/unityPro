using System.Collections.Generic;
using System.Linq;
using NetMsg;
using Tacticsoft;
using UnityEngine;
using UnityEngine.UI;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class GameBlackPlayerPanel : UIFormBase
{
    List<BasePlayer> players = new();
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        InitializeScroll();
        HotfixNetworkComponent.AddListener(MessageID.Msg_DeskBlackListRs, Function_DeskBlackListRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_RemoverDeskBlackRs, Function_DeskBlackListRs);

        Msg_DeskBlackListRq req = MessagePool.Instance.Fetch<Msg_DeskBlackListRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_DeskBlackListRq, req);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        if (varScroll != null)
        {
            varScroll.ClearAllRows();
        }
        
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_DeskBlackListRs, Function_DeskBlackListRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_RemoverDeskBlackRs, Function_DeskBlackListRs);
        base.OnClose(isShutdown, userData);
    }

    private void Function_DeskBlackListRs(MessageRecvData data)
    {
        // Msg_DeskListRs
        Msg_DeskBlackListRs ack = Msg_DeskBlackListRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_DeskBlackListRs收到黑名单列表:" , ack.ToString());
        players = ack.BlackInfos.ToList();
        varScroll.ScrollY = 0;
        varScroll.ReloadData();
    }

    #region 循环滚动
    public GameObject content;
    private void InitializeScroll()
    {
        varScroll.SetNumberOfRowsForTableView(tv => players.Count);
        varScroll.SetHeightForRowInTableView((tv, row) => varItem.GetComponent<RectTransform>().rect.height);
        varScroll.SetCellForRowInTableView(CellForRowInTableView);       // 获取指定行的 UI
    }
    public TableViewCell CellForRowInTableView(TableView tv, int row)
    {
        TableViewCell cell = tv.GetReusableCell("cell");
        if (cell == null)
        {
            GameObject go = Instantiate(varItem, content.transform);
            cell = go.GetComponent<TableViewCell>();
            cell.name = "cell";
            cell.reuseIdentifier = "cell";// 设置复用标识符
        }
        cell.gameObject.SetActive(true);
        UpdateItemData(cell.gameObject, players[row]);
                return cell;
;
    }

    private void UpdateItemData(GameObject cell, BasePlayer data)
    {
        cell.transform.SetParent(content.transform, false);
        cell.transform.Find("Nickname").GetComponent<Text>().text = data.Nick;
        cell.transform.Find("PlayerId").GetComponent<Text>().text = data.PlayerId.ToString();
        Util.DownloadHeadImage(cell.transform.Find("Mask/Head").GetComponent<RawImage>(), data.HeadImage);
        cell.transform.Find("BtnQuit").GetComponent<Button>().onClick.RemoveAllListeners();
        cell.transform.Find("BtnQuit").GetComponent<Button>().onClick.AddListener(delegate ()
        {
           Sound.PlayEffect(AudioKeys.SOUND_BTN);
            Util.GetInstance().OpenConfirmationDialog("移除黑名单", $"确定要移除 <color=#D0FE36>{data.Nick}</color>", () =>
            {
                GF.LogInfo("移除黑名单");
                Msg_RemoverDeskBlackRq req = MessagePool.Instance.Fetch<Msg_RemoverDeskBlackRq>();
                req.PlayerId = data.PlayerId;
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                    (HotfixNetworkComponent.AccountClientName, MessageID.Msg_RemoverDeskBlackRq, req);
            });
        });
    }
    #endregion  

}


using System.Collections.Generic;
using System.Linq;
using NetMsg;
using Tacticsoft;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class AllianceClubManagePanel : UIFormBase
{
    LeagueInfo leagueInfo;

    List<LeagueInfo> leagueInfos = new();
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        InitializeScroll();
        HotfixNetworkComponent.AddListener(MessageID.Msg_GetUnionClubListRs, Function_MyLeagueListRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_GetSuperUnionListRs, Function_MyLeagueListRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_RemoveClubRs, Function_RemoveClubRs);

        leagueInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("leagueInfo"));
        varTitleText.text = leagueInfo.Type == 1 ? "俱乐部管理" : "联盟管理";

        if (leagueInfo.Type == 1)
        {
            Msg_GetUnionClubListRq req = MessagePool.Instance.Fetch<Msg_GetUnionClubListRq>();
            req.UnionId = leagueInfo.LeagueId;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GetUnionClubListRq, req);
        }
        else if(leagueInfo.Type == 2)
        {
            Msg_GetSuperUnionListRq req = MessagePool.Instance.Fetch<Msg_GetSuperUnionListRq>();
            req.UnionId = leagueInfo.LeagueId;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GetSuperUnionListRq, req);
        }
        Util.GetInstance().CheckSafeCodeState(gameObject);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        varScroll.ClearAllRows();
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_GetUnionClubListRs, Function_MyLeagueListRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_GetSuperUnionListRs, Function_MyLeagueListRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_RemoveClubRs, Function_RemoveClubRs);
        base.OnClose(isShutdown, userData);
    }
    private void Function_RemoveClubRs(MessageRecvData data)
    {
        //踢除俱乐部返回刷新俱乐部列表
        if (leagueInfo.Type == 1)
        {
            Msg_GetUnionClubListRq req = MessagePool.Instance.Fetch<Msg_GetUnionClubListRq>();
            req.UnionId = leagueInfo.LeagueId;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GetUnionClubListRq, req);
        }
        else if(leagueInfo.Type == 2)
        {
            Msg_GetSuperUnionListRq req = MessagePool.Instance.Fetch<Msg_GetSuperUnionListRq>();
            req.UnionId = leagueInfo.LeagueId;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GetSuperUnionListRq, req);
        }
    }

    private void Function_MyLeagueListRs(MessageRecvData data)
    {
        // Msg_DeskListRs
        Msg_GetUnionClubListRs ack = Msg_GetUnionClubListRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("Msg_GetUnionClubListRs收到联盟俱乐部列表:" , ack.ToString());
        leagueInfos = ack.Infos.ToList();
        varScroll.ScrollY = 0;
        varScroll.ReloadData();
    }

    #region 循环滚动
    public GameObject content;
    private void InitializeScroll()
    {
        varScroll.SetNumberOfRowsForTableView(tv => leagueInfos.Count);
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
        UpdateItemData(cell.gameObject, leagueInfos[row]);
                return cell;
;
    }

    private void UpdateItemData(GameObject cell, LeagueInfo data)
    {
        cell.transform.SetParent(content.transform, false);
        cell.transform.Find("Nickname").GetComponent<Text>().text = data.LeagueName;
        cell.transform.Find("PlayerId").GetComponent<Text>().text = data.LeagueId.ToString();
        Util.DownloadHeadImage(cell.transform.Find("Mask/Head").GetComponent<RawImage>(), data.LeagueHead, data.Type);
        //头像
        if (Util.IsMySelf(data.Creator))
        {
            cell.transform.Find("BtnQuit").gameObject.SetActive(false);
        }
        else
        {
            cell.transform.Find("BtnQuit").gameObject.SetActive(true);
            cell.transform.Find("BtnQuit").GetComponent<Button>().onClick.RemoveAllListeners();
            cell.transform.Find("BtnQuit").GetComponent<Button>().onClick.AddListener(delegate ()
            {
               Sound.PlayEffect(AudioKeys.SOUND_BTN);
                Util.GetInstance().OpenConfirmationDialog("移除俱乐部", "确定要移除俱乐部", () =>
                {
                    GF.LogInfo("移除俱乐部");
                    Msg_RemoveClubRq req = MessagePool.Instance.Fetch<Msg_RemoveClubRq>();
                    req.ClubId = data.LeagueId;
                    HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                        (HotfixNetworkComponent.AccountClientName, MessageID.Msg_RemoveClubRq, req);
                });
            });
        }
    }
    #endregion

}

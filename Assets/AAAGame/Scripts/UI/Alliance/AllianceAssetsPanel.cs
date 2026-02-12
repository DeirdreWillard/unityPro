
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using NetMsg;
using DG.Tweening;
using UnityEngine;
using Google.Protobuf;
using GameFramework.Event;
using Tacticsoft;
using System.Collections.Generic;
using System.Linq;


[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class AllianceAssetsPanel : UIFormBase
{
    public InputField InputNum;
    public Text txtName;
    public Text txtId;
    public Toggle toggle1;
    public Text textAfterCoins;

    public LeagueInfo chooseClubInfo;
    public LeagueInfo leagueInfo;

    private List<LeagueInfo> leagueInfos;
    private List<LeagueInfo> leagueInfos_show = new();
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SetClubGoldRs, FunctionMsgSetClubGoldRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_GetUnionClubListRs, Function_MyLeagueListRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_GetSuperUnionListRs, Function_MyLeagueListRs);
        GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        InitializeScroll();

        leagueInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("leagueInfo"));
        varInputFieldLegacy.placeholder.GetComponent<Text>().text = leagueInfo.Type == 1 ? "输入俱乐部ID搜索" : "输入联盟ID搜索";

        if (leagueInfo.Type == 1)
        {
            Msg_GetUnionClubListRq req = MessagePool.Instance.Fetch<Msg_GetUnionClubListRq>();
            req.UnionId = leagueInfo.LeagueId;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GetUnionClubListRq, req);
        }
        else if (leagueInfo.Type == 2)
        {
            Msg_GetSuperUnionListRq req = MessagePool.Instance.Fetch<Msg_GetSuperUnionListRq>();
            req.UnionId = leagueInfo.LeagueId;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GetSuperUnionListRq, req);
        }
        varOperationPanel.SetActive(false);
        varInputFieldLegacy.onValueChanged.RemoveAllListeners();
        varInputFieldLegacy.onValueChanged.AddListener(delegate
        {
            if (varInputFieldLegacy.text.Length > 0)
            {
                ShowList(varInputFieldLegacy.text);
            }
            else
            {
                ShowList();
            }
        });
        varInputFieldLegacy.text = "";
        SetUI();
        Util.GetInstance().CheckSafeCodeState(gameObject);
    }

    public void SetUI()
    {
        varCoin.text = Util.GetMyselfInfo().Gold.ToString();
        varDiamond.text = Util.GetMyselfInfo().Diamonds.ToString();
        varUnionCoin.text = GlobalManager.GetInstance().GetAllianceCoins(leagueInfo.LeagueId).ToString();
    }

    public void FuncSearchClubRq(string clubId)
    {
        long.TryParse(InputNum.text,out long clubID);
        if (clubID == 0)
        {
            GF.UI.ShowToast("请输入正确的联盟号");
            return;
        }
        Msg_SearchClubRq req = MessagePool.Instance.Fetch<Msg_SearchClubRq>();
        req.ClubId = clubID;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SearchClubRq, req);
    }

    private void OnClubDataChanged(object sender, GameEventArgs e)
    {
        var args = e as ClubDataChangedEventArgs;
        switch (args.Type)
        {
            case ClubDataType.eve_LeagueDateUpdate:
                GF.LogInfo("俱乐部信息更新");
                leagueInfo = GlobalManager.GetInstance().LeagueInfo;
                SetUI();
                break;
        }
    }

    private void Function_MyLeagueListRs(MessageRecvData data)
    {
        // Msg_DeskListRs
        Msg_GetUnionClubListRs ack = Msg_GetUnionClubListRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到联盟俱乐部列表:" , ack.ToString());
        leagueInfos = ack.Infos.ToList();
        leagueInfos_show = leagueInfos;
        ShowList();
    }

    public void ShowList(string findID = "")
    {
        if (!string.IsNullOrEmpty(findID))
        {
            leagueInfos_show = new();
            for (int i = 0; i < leagueInfos.Count; i++)
            {
                string leagueIdStr = leagueInfos[i].LeagueId.ToString();
                if (leagueIdStr.StartsWith(findID))
                {
                    leagueInfos_show.Add(leagueInfos[i]);
                }
            }
        }
        else
        {
            leagueInfos_show = leagueInfos;
        }
        varScroll.ScrollY = 0;
        varScroll.ReloadData();
    }

    public GameObject content;
    private void InitializeScroll()
    {
        varScroll.SetNumberOfRowsForTableView(tv => leagueInfos_show.Count);
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
            cell.name = $"Cell {row}"; 
            cell.reuseIdentifier = "cell";// 设置复用标识符
        }
        cell.gameObject.SetActive(true);
        UpdateItemData(cell.gameObject, leagueInfos_show[row]);
        return cell;
    }

    private void UpdateItemData(GameObject cell, LeagueInfo data)
    {
        cell.transform.Find("Nickname").GetComponent<Text>().text = data.LeagueName;
        cell.transform.Find("PlayerId").GetComponent<Text>().text = data.LeagueId.ToString();

        float gold = data.Users.FirstOrDefault(u => u.PlayerId == data.Creator)?.Diamond ?? 0;
        cell.transform.Find("DiaImage/Diamond").GetComponent<Text>().text = gold.ToString();
        //头像
        Util.DownloadHeadImage(cell.transform.Find("Mask/Head").GetComponent<RawImage>(), data.LeagueHead, data.Type);
        cell.transform.Find("Btn").GetComponent<Button>().onClick.RemoveAllListeners();
        cell.transform.Find("Btn").GetComponent<Button>().onClick.AddListener(delegate ()
        {
           Sound.PlayEffect(AudioKeys.SOUND_BTN);
            varOperationPanel.transform.SetLocalPositionX(1200);
            this.chooseClubInfo = data;
            txtName.text = data.LeagueName;
            InputNum.text = "";
            txtId.text = "ID：" + data.LeagueId.ToString();
            textAfterCoins.text = gold.ToString();
            varOperationPanel.SetActive(true);
            varOperationPanel.transform.DOLocalMoveX(0, 0.5f);
        });
    }

    /// <summary>
    /// 设置俱乐部资产返回
    /// </summary>
    /// <param name="data"></param>
    private void FunctionMsgSetClubGoldRs(MessageRecvData data)
    {
        Msg_SetClubGoldRs ack = Msg_SetClubGoldRs.Parser.ParseFrom(data.Data);
        if (leagueInfo.Type == 1)
        {
            Msg_GetUnionClubListRq req = MessagePool.Instance.Fetch<Msg_GetUnionClubListRq>();
            req.UnionId = leagueInfo.LeagueId;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GetUnionClubListRq, req);
        }
        else if (leagueInfo.Type == 2)
        {
            Msg_GetSuperUnionListRq req = MessagePool.Instance.Fetch<Msg_GetSuperUnionListRq>();
            req.UnionId = leagueInfo.LeagueId;
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GetSuperUnionListRq, req);
        }
        GF.UI.Close(this.UIForm);
        GF.UI.ShowToast("操作成功！");
    }
    public void Send_SetClubGoldRq()
    {
        Msg_SetClubGoldRq req = MessagePool.Instance.Fetch<Msg_SetClubGoldRq>();
        req.LeagueId = chooseClubInfo.LeagueId;
        float.TryParse(InputNum.text, out float num);
        if (num == 0)
            return;
        req.Amount = num;
        req.Option = 0;
        req.Type = 1;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SetClubGoldRq, req);
    }

    public void InputChangeUpdate()
    {
        // long.TryParse(InputNum.text, out long num);
        // textAfterCoins.text = (Util.GetMyselfInfo().Diamonds - num < 0 ? 0 :Util.GetMyselfInfo().Diamonds - num).ToString();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        varScroll.ClearAllRows();
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SetClubGoldRs, FunctionMsgSetClubGoldRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_GetUnionClubListRs, Function_MyLeagueListRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_GetSuperUnionListRs, Function_MyLeagueListRs);
        GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
        base.OnClose(isShutdown, userData);
    }

    protected override async void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        var uiParams = UIParams.Create();
        uiParams.Set<VarByteArray>("leagueInfo", leagueInfo.ToByteArray());
        switch (btId)
        {
            case "BtnClear":
                varInputFieldLegacy.text = "";
                break;
            case "CloseOperationPanel":
                varOperationPanel.transform.DOLocalMoveX(1200, 0.3f).OnComplete(() =>
                    {
                        varOperationPanel.SetActive(false);
                    });
                break;
            case "SendBtn":
                Send_SetClubGoldRq();
                break;
            case "DetailsBtn":
                //明细
                uiParams.Set<VarInt32>("coinType", 3);
                await GF.UI.OpenUIFormAwait(UIViews.XXXDetailsPanel, uiParams);
                break;
            case "ShopBtn":
                if (leagueInfo.Type == 1 && leagueInfo.Father != 0)
                {
                    GF.UI.ShowToast("退出超级联盟恢复功能");
                    return;
                }
                await GF.UI.OpenUIFormAwait(UIViews.AllianceShopPanel);
                break;
        }
    }

}

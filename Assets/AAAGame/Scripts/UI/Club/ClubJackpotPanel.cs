
using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using Google.Protobuf.Collections;
using DG.Tweening;
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class ClubJackpotPanel : UIFormBase
{
    public GameObject jpInfoPanel;
    public GameObject settingPanel;
    public GameObject jpReportPanel;

    public GameObject item_jpInfo;
    public GameObject content_jpInfo;

    private string opType;
    private RepeatedField<Msg_JackpotConfig> configs;
    private Msg_JackpotConfig config;

    private LeagueInfo leagueInfo;

    public MethodType chooseMethodType;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        jpInfoPanel.SetActive(false);
        settingPanel.SetActive(false);
        jpReportPanel.SetActive(false);
        varChoosePanel.SetActive(true);
    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SynJackpotConfigRs, Function_GetJackpotConfigRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_SetJackpotConfigRs, Function_SetJackpotConfigRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_JackPotReportRs, Function_JackPotReportRs);

        leagueInfo = LeagueInfo.Parser.ParseFrom(Params.Get<VarByteArray>("leagueInfo"));

        Msg_GetJackpotConfigRq req = MessagePool.Instance.Fetch<Msg_GetJackpotConfigRq>();
        req.ClubId = leagueInfo.LeagueId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_GetJackpotConfigRq, req);

        Util.GetInstance().CheckSafeCodeState(gameObject);

    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynJackpotConfigRs, Function_GetJackpotConfigRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_SetJackpotConfigRs, Function_SetJackpotConfigRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_JackPotReportRs, Function_JackPotReportRs);
        base.OnClose(isShutdown, userData);
    }

    private void Function_JackPotReportRs(MessageRecvData data)
    {
        // Msg_DeskListRs
        Msg_JackPotReportRs ack = Msg_JackPotReportRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到奖池记录返回:" , ack.ToString());
        jpReportPanel.transform.SetLocalPositionX(1200);
        jpReportPanel.SetActive(true);
        InitJpReportPanel(ack);
        jpReportPanel.transform.DOLocalMoveX(0, 0.3f);
    }


    private void Function_SetJackpotConfigRs(MessageRecvData data)
    {
        // Msg_DeskListRs
        Msg_SetJackpotConfigRs ack = Msg_SetJackpotConfigRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到设置奖池返回:" , ack.ToString());
        config = ack.Configs;
        InitJpInfoPanel(config);
        if (opType == "SaveBtn")
        {
            settingPanel.SetActive(false);
        }
        GF.UI.ShowToast("修改成功");
    }

    private void Function_GetJackpotConfigRs(MessageRecvData data)
    {
        // Msg_DeskListRs
        Msg_SynJackpotConfigRs ack = Msg_SynJackpotConfigRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("收到奖池信息:" , ack.ToString());
        configs = ack.Configs;
    }

    public void OnChooseClick(int methodType)
    {
        if (configs.Count == 0)
        {
            GF.LogError("奖池没有数据");
            //InitConfig();
        }
        foreach (var item in configs)
        {
            if ((int)item.MethodType == methodType)
            {
                config = item;
            }
        }
        if (config == null)
        {
            GF.LogError("对应类型奖池没有数据");
            return;
        }
        chooseMethodType = (MethodType)methodType;
        jpInfoPanel.transform.SetLocalPositionX(1200);
        InitJpInfoPanel(config);
        jpInfoPanel.SetActive(true);
        jpInfoPanel.transform.DOLocalMoveX(0, 0.3f);
    }

    public void InitJpInfoPanel(Msg_JackpotConfig config)
    {
        float potSum = 0;
        //创建列表
        foreach (Transform item in content_jpInfo.transform)
        {
            Destroy(item.gameObject);
        }
        for (int i = 0; i < config.BaseConfig.Count; i++)
        {
            Msg_BaseJackpot data = config.BaseConfig[i];
            potSum += float.Parse(data.Pot);
            GameObject newItem = Instantiate(item_jpInfo);
            newItem.transform.SetParent(content_jpInfo.transform, false);
            newItem.transform.Find("Info").GetComponent<Text>().text = float.Parse(data.BaseCoin).ToString() + "/" + (float.Parse(data.BaseCoin) * 2) + "奖池";
            newItem.transform.Find("Value").GetComponent<Text>().text = float.Parse(data.Pot).ToString().Replace('.', '@');
            newItem.transform.Find("InfoBtn").GetComponent<Button>().onClick.AddListener(delegate ()
            {
               Sound.PlayEffect(AudioKeys.SOUND_BTN);
                Msg_JackPotReportRq req = MessagePool.Instance.Fetch<Msg_JackPotReportRq>();
                req.ClubId = leagueInfo.LeagueId;
                req.MethodType = config.MethodType;
                req.BaseCoin = float.Parse(data.BaseCoin);
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                    (HotfixNetworkComponent.AccountClientName, MessageID.Msg_JackPotReportRq, req);
            });
        }
        varJackpotNumText.text = potSum.ToString().Replace('.', '@');
    }

    public GameObject jpReportItem;
    public GameObject jpReportcontent;

    public void InitJpReportPanel(Msg_JackPotReportRs ack)
    {
        //创建列表
        foreach (Transform item in jpReportcontent.transform)
        {
            Destroy(item.gameObject);
        }
        for (int i = 0; i < ack.Report.Count; i++)
        {
            GameObject newItem = Instantiate(jpReportItem);
            newItem.transform.SetParent(jpReportcontent.transform, false);
            var data = ack.Report[i];
            newItem.transform.Find("Name").GetComponent<Text>().FormatNickname(data.BasePlayer.Nick);
            newItem.transform.Find("BaseCoin").GetComponent<Text>().text = data.BaseCoin.ToString();
            newItem.transform.Find("Type").GetComponent<Text>().text = GameUtil.ShowType(data.CardType, data.Type);
            newItem.transform.Find("Jackpot").GetComponent<Text>().text = data.JackPot;
            newItem.transform.Find("Time").GetComponent<Text>().text = Util.MillisecondsToDateString(data.OpTime, "MM/dd");
        }
    }


    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "CloseJpInfoPanel":
                jpInfoPanel.transform.DOLocalMoveX(1200, 0.3f).OnComplete(() =>
                {
                    jpInfoPanel.SetActive(false);
                });
                break;
            case "OpenSettingBtn":
                settingPanel.transform.SetLocalPositionX(1200);
                settingPanel.SetActive(true);
                settingPanel.GetComponent<SettingJp>().InitPanel(config);
                settingPanel.transform.DOLocalMoveX(0, 0.3f);
                break;
            case "CloseSettingBtn":
                settingPanel.transform.DOLocalMoveX(1200, 0.3f).OnComplete(() =>
                {
                    settingPanel.SetActive(false);
                });
                break;
            case "CloseJpReportPanel":
                jpReportPanel.transform.DOLocalMoveX(1200, 0.3f).OnComplete(() =>
                {
                    jpReportPanel.SetActive(false);
                });
                break;
            case "SaveBtn":
                Util.GetInstance().OpenConfirmationDialog("保存设置", "确定要保存设置吗?", () =>
                {
                    opType = "SaveBtn";
                    Msg_SetJackpotConfigRq req = MessagePool.Instance.Fetch<Msg_SetJackpotConfigRq>();
                    req.Configs = settingPanel.GetComponent<SettingJp>().GetUpdateData();
                    req.ClubId = leagueInfo.LeagueId;
                    HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                        (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SetJackpotConfigRq, req);
                });
                break;
            case "DefaultBtn":
                Util.GetInstance().OpenConfirmationDialog("恢复默认", "确定要恢复默认吗?", () =>
                {
                    Msg_SetJackpotConfigRq req = MessagePool.Instance.Fetch<Msg_SetJackpotConfigRq>();
                    req.Configs = settingPanel.GetComponent<SettingJp>().SetDefault();
                    req.ClubId = leagueInfo.LeagueId;
                    HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                        (HotfixNetworkComponent.AccountClientName, MessageID.Msg_SetJackpotConfigRq, req);
                });
                break;
        }
    }

}

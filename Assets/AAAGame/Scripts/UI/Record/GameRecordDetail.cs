using System.Linq;
using UnityEngine;
using UnityGameFramework.Runtime;
using NetMsg;
using UnityEngine.UI;
using System;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class GameRecordDetail : UIFormBase
{
    public Text DeskTypeText;
    public Text LeagueNameText;
    public RawImage avatar;
    public Text BringText;
    public Text DeskNameText;
    public Text DeskIdText;
    public Text LossOrGainText;
    public Text BaseCoinText;
    public Text recordTime;
    public Text allHand;
    public GameObject jackpot;
    public GameObject rate;
    public GameObject protect;
    public Image coinImg;
    public Image goldImg;


    public GameObject MVPobj;
    public GameObject TUHAOobj;
    public GameObject DAYUobj;

    public SingleGameRecord singleGameRecord;

    private bool isCreate = false; //是否是创建者

    private bool isClubCreate = true; //俱乐部长

    public class MvpInfo
    {
        public PlayerGameRecord mvp;
        public PlayerGameRecord tuhao;
        public PlayerGameRecord dayu;
        public int allHand;
        public long allBring;
        public float allRate;
        public float allJackpot;

    }

    MvpInfo mvpInfo = new();
    PlayerGameRecord myData = new();
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        singleGameRecord = SingleGameRecord.Parser.ParseFrom(Params.Get<VarByteArray>("SingleGameRecord"));
        isCreate = Util.IsMySelf(singleGameRecord.Record.Creator);
        isClubCreate = RecordManager.GetInstance().joinType == 1;
        myData = new(){
            Change = "0",
            Rate = "0",
            PlayerId = Util.GetMyselfInfo().PlayerId,
            Bring = 0,
            Hand = 0,
            // ClubId = GlobalManager.GetInstance().LeagueInfo.LeagueId,
            // ClubName = GlobalManager.GetInstance().LeagueInfo.LeagueName,
            Nick = Util.GetMyselfInfo().NickName,
            Jackpot = "0"
        };
        mvpInfo = new(){
            mvp = new PlayerGameRecord(),
            tuhao = new PlayerGameRecord(),
            dayu = new PlayerGameRecord(),
            allHand = 0,
            allBring = 0,
            allRate = 0,
            allJackpot = 0
        };
        RefreshData();
        Init();
    }

    public void Init()
    {
        switch (singleGameRecord.Record.DeskType)
        {
            case DeskType.Simple:
                DeskTypeText.text = "个人房";
                jackpot.SetActive(isCreate);
                LeagueNameText.gameObject.SetActive(true);
                LeagueNameText.text = Util.GetMyselfInfo().NickName;
                Util.DownloadHeadImage(avatar, Util.GetMyselfInfo().HeadIndex);
                break;
            case DeskType.Guild:
                DeskTypeText.text = "俱乐部房";
                LeagueNameText.gameObject.SetActive(true);
                LeagueNameText.text = singleGameRecord.Record.LeagueName;
                Util.DownloadHeadImage(avatar, singleGameRecord.Record.LeagueHead, (int)singleGameRecord.Record.DeskType - 1);
                break;
            case DeskType.League:
                DeskTypeText.text = "联盟房";
                //俱乐部才看得到联盟名字
                LeagueNameText.gameObject.SetActive(isClubCreate);
                LeagueNameText.text = singleGameRecord.Record.LeagueName;
                Util.DownloadHeadImage(avatar, "", (int)singleGameRecord.Record.DeskType - 1);
                break;
            case DeskType.Super:
                DeskTypeText.text = "超级联盟房";
                //俱乐部才看得到联盟名字
                LeagueNameText.gameObject.SetActive(isClubCreate);
                LeagueNameText.text = singleGameRecord.Record.LeagueName;
                Util.DownloadHeadImage(avatar, "", (int)singleGameRecord.Record.DeskType - 1);
                break;
        }
        coinImg.gameObject.SetActive(singleGameRecord.Record.DeskType != DeskType.League && singleGameRecord.Record.DeskType != DeskType.Super);
        goldImg.gameObject.SetActive(singleGameRecord.Record.DeskType == DeskType.League || singleGameRecord.Record.DeskType == DeskType.Super);
        DeskNameText.text = singleGameRecord.Record.DeskName;
        DeskIdText.text = "房间号:" + singleGameRecord.Record.DeskId;
        BaseCoinText.text = float.Parse(singleGameRecord.Record.BaseCoin) + "/" + (float.Parse(singleGameRecord.Record.BaseCoin) * 2);
        recordTime.text = UtilityBuiltin.Valuer.ToTime(singleGameRecord.Record.Time);
        MVPobj.transform.Find("name").GetComponent<Text>().text = mvpInfo.mvp.Nick;
        Util.DownloadHeadImage(MVPobj.transform.Find("mask/avatar").GetComponent<RawImage>(), mvpInfo.mvp.HeadImage);
        TUHAOobj.transform.Find("name").GetComponent<Text>().text = mvpInfo.tuhao.Nick;
        Util.DownloadHeadImage(TUHAOobj.transform.Find("mask/avatar").GetComponent<RawImage>(), mvpInfo.tuhao.HeadImage);
        DAYUobj.transform.Find("name").GetComponent<Text>().text = mvpInfo.dayu.Nick;
        Util.DownloadHeadImage(DAYUobj.transform.Find("mask/avatar").GetComponent<RawImage>(), mvpInfo.dayu.HeadImage);
        BringText.text = "总带入:" + mvpInfo.allBring;
        allHand.text = "总手数:" + mvpInfo.allHand;
        LossOrGainText.text = "盈亏 " + Util.FormatRichText(float.Parse(myData.Change));

        jackpot.transform.Find("Bg/Value").GetComponent<Text>().FormatRichText(float.Parse(singleGameRecord.Record.Jackpot), "#C6BC0B", "#C6BC0B");
        //荣誉榜只有创建者 和 个人房主可以看到
        rate.SetActive(isClubCreate || isCreate);
        rate.transform.Find("Bg/Value").GetComponent<Text>().FormatRichText(float.Parse(myData.Rate), "#C6BC0B", "#C6BC0B");
        protect.SetActive(singleGameRecord.Record.MethodType == MethodType.TexasPoker);
        protect.transform.Find("Bg/Value").GetComponent<Text>().FormatRichText(float.Parse(singleGameRecord.Record.Protect), "#C6BC0B", "#C6BC0B");
        ShowGameRecord();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        base.OnClose(isShutdown, userData);
    }

    //显示战绩详细信息
    public GameObject content;
    public GameObject itemPrefab;
    public void ShowGameRecord()
    {
        //创建列表
        foreach (Transform item in content.transform)
        {
            if (item.name == "Item")
            {
                Destroy(item.gameObject);
            }
        }
        
        // 分为两组:我的俱乐部/联盟玩家 和 其他玩家
        var validRecords = singleGameRecord.PlayerRecord.Where(item => float.TryParse(item.Change, out _)).ToList();
        
        var myLeaguePlayers = validRecords
            .Where(item => RecordManager.GetInstance().joinType == 1 && Util.IsMyLeague(item.ClubId))
            .OrderByDescending(item => float.TryParse(item.Change, out float change) ? change : float.MinValue)
            .ToList();
        
        var otherPlayers = validRecords
            .Where(item => !(RecordManager.GetInstance().joinType == 1 && Util.IsMyLeague(item.ClubId)))
            .OrderByDescending(item => float.TryParse(item.Change, out float change) ? change : float.MinValue)
            .ToList();
        
        // 合并列表:我的俱乐部/联盟玩家在前
        var sortedRecords = myLeaguePlayers.Concat(otherPlayers).ToList();
        
        for (int i = 0; i < sortedRecords.Count; i++)
        {
            var data = sortedRecords[i];
            int index = i + 1;
            GameObject newItem = Instantiate(itemPrefab);
            newItem.name = "Item";
            newItem.transform.SetParent(content.transform, false);
            newItem.GetComponent<GameRecordDetailItem>().Init(data, index, isCreate, singleGameRecord.Record.DeskType, singleGameRecord.Record.MethodType);
        }
    }

    /// <summary>
    /// 自行构造显示数据
    /// </summary>
    public void RefreshData()
    {
        PlayerGameRecord playerRecordTemp_mvp = null;
        PlayerGameRecord playerRecordTemp_tuhao = null;
        PlayerGameRecord playerRecordTemp_dayu = null;

        float allRate = 0;
        float allJackpot = 0;
        long allBring = 0;
        int allHand = 0;

        PlayerGameRecord[] playerRecord = singleGameRecord.PlayerRecord.ToArray();

        foreach (var record in playerRecord)
        {
            allRate += float.TryParse(record.Rate, out float rate) ? rate : 0;
            allJackpot += float.TryParse(record.Jackpot, out float jackpot) ? jackpot : 0;
            allHand += record.Hand;
            allBring += (long)record.Bring;

            float.TryParse(record.Change, out float change);
            // MVP逻辑
            if (playerRecordTemp_mvp == null || change > float.Parse(playerRecordTemp_mvp.Change))
            {
                playerRecordTemp_mvp = record;
            }

            // 土豪逻辑
            if (playerRecordTemp_tuhao == null || record.Bring > playerRecordTemp_tuhao.Bring)
            {
                playerRecordTemp_tuhao = record;
            }

            // 大鱼逻辑
            if (playerRecordTemp_dayu == null || change < float.Parse(playerRecordTemp_dayu.Change))
            {
                playerRecordTemp_dayu = record;
            }

            //个人战绩 个人房创建显示汇总 其他显示自己总战绩
            if (!isClubCreate)
            {
                if (isCreate)
                {
                    UpdatePlayerData(myData, record);
                }
                else
                {
                    if (Util.IsMySelf(record.PlayerId))
                    {
                        UpdatePlayerData(myData, record);
                    }
                }
            }
            else
            {
                if (Util.IsMyLeague(record.ClubId))
                {
                    UpdatePlayerData(myData, record);
                }
            }
        }

        // 设置返回结果
        mvpInfo.mvp = playerRecordTemp_mvp;
        mvpInfo.tuhao = playerRecordTemp_tuhao;
        mvpInfo.dayu = playerRecordTemp_dayu;
        mvpInfo.allHand = allHand;
        mvpInfo.allBring = allBring;
        mvpInfo.allRate = (float)Math.Round(allRate, 2);
        mvpInfo.allJackpot = (float)Math.Round(allJackpot, 2);

        // GF.LogInfo("info:" + mvpInfo.ToString());
        // GF.LogInfo("myData:" + myData.ToString());
    }

    public void UpdatePlayerData(PlayerGameRecord myData, PlayerGameRecord newData)
    {
        // 更新手数 (hand)
        myData.Hand += newData.Hand;

        // 更新带入 (bring) - 假设是 int64 类型
        myData.Bring += newData.Bring;

        // 更新变化分数 (change) - 假设是 float 类型
        if (!float.TryParse(myData.Change, out float ownChange))
        {
            ownChange = 0f;
        }
        if (float.TryParse(newData.Change, out float newChange))
        {
            myData.Change = Util.FormatAmount(ownChange + newChange); // 保留两位小数
        }

        // 更新抽水 (rate) - 假设是 float 类型
        if (!float.TryParse(myData.Rate, out float ownRate))
        {
            ownRate = 0f;
        }
        if (float.TryParse(newData.Rate, out float newRate))
        {
            myData.Rate = Util.FormatAmount(ownRate + newRate); // 保留两位小数
        }

        // 更新奖池 (jackpot) - 假设是浮动值，可以转换为 float 或 decimal
        if (!float.TryParse(myData.Jackpot, out float ownJackpot))
        {
            ownJackpot = 0f;
        }
        if (float.TryParse(newData.Jackpot, out float newJackpot))
        {
            myData.Jackpot = Util.FormatAmount(ownJackpot + newJackpot); // 保留两位小数
        }
    }

    public override void OnClickClose()
    {
        if (Util.IsClickLocked()) return;
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (singleGameRecord.Record.MethodType == MethodType.NiuNiu)
        {
            if (GF.Procedure.CurrentProcedure is NNProcedure nnProcedure)
            {
                nnProcedure.EnterHome();
                return;
            }
        }
        if (singleGameRecord.Record.MethodType == MethodType.TexasPoker)
        {
            if (GF.Procedure.CurrentProcedure is DZProcedure dzProcedure)
            {
                dzProcedure.EnterHome();
                return;
            }
        }
        if (singleGameRecord.Record.MethodType == MethodType.GoldenFlower)
        {
            if (GF.Procedure.CurrentProcedure is ZJHProcedure zjhProcedure)
            {
                zjhProcedure.EnterHome();
                return;
            }
        }
        if (singleGameRecord.Record.MethodType == MethodType.CompareChicken)
        {
            if (GF.Procedure.CurrentProcedure is BJProcedure bjProcedure)
            {
                bjProcedure.EnterHome();
                return;
            }
        }
        GF.UI.Close(this.UIForm);
    }
}

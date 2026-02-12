
using NetMsg;

using UnityEngine;
using UnityEngine.UI;

public class DeskItem : MonoBehaviour
{

    //DeskType deskType = 1;  //桌子类型
    //MethodType methodType = 2;//玩法
    //int32 deskId = 3; //桌子ID
    //Desk_BaseConfig baseConfig = 4; //基础配置
    //int32 sitDownNum = 5;//坐下人数
    //int32 participateState = 6;//参与状态 是否进入过
    //DeskState state = 7; //桌子状态
    //int64 destroyTime = 8;  //桌子销毁时间

    //桌子类型
    //enum DeskType
    //{
    //    SIMPLE = 0;//普通房
    //    GUILD = 1;//俱乐部房
    //    LEAGUE = 2;//联盟房
    //}
    //玩法类型
    //enum MethodType
    //{
    //    NIU_NIU = 0;//牛牛
    //    GOLDEN_FLOWER = 1;//扎金花
    //    TEXAS_POKER = 2;//德州
    //}
    //基础配置
    //message Desk_BaseConfig
    //{
    //    string deskName = 1;//名称
    //    int32 baseCoin = 2;//底分
    //    int64 playerTime = 3;//时长
    //    int32 playerNum = 4;//玩家
    //    int32 minBringIn = 5;//最低带入
    //    bool openRate = 6;//是否开启抽水
    //    PairInt rate = 7;//抽水比例
    //}

    //牌桌状态
    //enum DeskState
    //{
    //    WAIT_START = 0; //准备中
    //    START_RUN = 1;//已开始
    //    DISMISS = 2;//解散
    //}
    public Text methodTypetxt;
    public Text deskTypetxt;
    public Text roomNametxt;
    public Text roomStatetxt;
    public Text joinStatetxt;
    public Text minBringTxt;
    public Text people;
    public Text time;
    public Text ante;
    public Image typeImage;
    public Image Jp;
    public Image Jing;
    public Image Laizi;
    public Image DuanPai;
    public Image Liang;
    public Image Baoji;
    public Image Xi;



    //public Text leagueHeadtxt;
    //public Text desktxt;
    //public Text introductiontxt;
    //public Text isOpentxt;
    //public Image creater;

    public DeskCommonInfo deskCommonInfo;

    // Start is called before the first frame update
    void Start()
    {

    }

    public void Init(DeskCommonInfo deskCommonInf)
    {
        deskCommonInfo = deskCommonInf;

        typeImage.SetSprite(GameUtil.GetGameIconByMethodType(deskCommonInfo.MethodType), true);
        switch (deskCommonInfo.MethodType)
        {
            case MethodType.NiuNiu:
                methodTypetxt.text = "牛牛";
                break;
            case MethodType.GoldenFlower:
                methodTypetxt.text = "金花";
                break;
            case MethodType.TexasPoker:
                methodTypetxt.text = "德州";
                break;
            case MethodType.CompareChicken:
                methodTypetxt.text = "比鸡";
                break;
        }
        switch (deskCommonInfo.DeskType)
        {
            case DeskType.Simple:
                deskTypetxt.text = "个人房";
                break;
            case DeskType.Guild:
                deskTypetxt.text = "俱乐部房";
                break;
            case DeskType.League:
                deskTypetxt.text = "联盟房";
                break;
            case DeskType.Super:
                deskTypetxt.text = "超级联盟房";
                break;
        }
        switch (deskCommonInfo.State)
        {
            case DeskState.Dismiss:
                roomStatetxt.text = "<color=#8B8A9F>(已解散)</color>";
                break;
            case DeskState.Pause:
                roomStatetxt.text = "<color=#8B8A9F>(暂停中)</color>";
                break;
            case DeskState.StartRun:
                roomStatetxt.text = "<color=#00B4FF>(已开始)</color>";
                break;
            case DeskState.WaitStart:
                roomStatetxt.text = "<color=#00B400>(等待中)</color>";
                break;
            default:
                break;
        }
        joinStatetxt.gameObject.SetActive(deskCommonInfo.ParticipateState == 1);
        roomNametxt.text = deskCommonInfo.BaseConfig.DeskName + $"({deskCommonInfo.DeskId})";

        people.text = deskCommonInfo.SitDownNum + "/" + deskCommonInfo.BaseConfig.PlayerNum;
        people.color = deskCommonInfo.SitDownNum == deskCommonInfo.BaseConfig.PlayerNum ? new Color(180 / 255f, 0, 0, 1) : Color.white;
        time.text = Util.ToTime(deskCommonInfo.DestroyTime);
        ante.text = deskCommonInfo.BaseConfig.BaseCoin + "/" + (deskCommonInfo.BaseConfig.BaseCoin * 2);
        minBringTxt.text = $"带入:{deskCommonInfo.BaseConfig.MinBringIn}";

        Jp.gameObject.SetActive(deskCommonInfo.BaseConfig.Jackpot);
        Jing.gameObject.SetActive(deskCommonInfo.NiuConfig != null && deskCommonInfo.NiuConfig.NoNiuRobBanker);
        Laizi.gameObject.SetActive((deskCommonInfo.NiuConfig != null && deskCommonInfo.NiuConfig.OpenRogue)
        || (deskCommonInfo.CkConfig != null && deskCommonInfo.CkConfig.King > 0));
        DuanPai.gameObject.SetActive(deskCommonInfo.GoldenFlowerConfig != null && deskCommonInfo.GoldenFlowerConfig.ShotCard);
        Liang.gameObject.SetActive((deskCommonInfo.TexasPokerConfig != null && deskCommonInfo.TexasPokerConfig.ShowCard) ||
            (deskCommonInfo.GoldenFlowerConfig != null && deskCommonInfo.GoldenFlowerConfig.SysShowCard));
        Baoji.gameObject.SetActive(deskCommonInfo.TexasPokerConfig != null && deskCommonInfo.TexasPokerConfig.Critcal);
        Xi.gameObject.SetActive(deskCommonInfo.CkConfig != null && deskCommonInfo.CkConfig.LuckCard.Count > 0);
        // Zhua.gameObject.SetActive(deskCommonInfo.NiuConfig.NoNiuRobBanker); 

        //leagueInfoData = leagueInfo;
        //leagueIdtxt.text=leagueInfo.LeagueId.ToString();
        //leagueNametxt.text=leagueInfo.LeagueName;
        // leagueHeadtxt.text=leagueInfo.LeagueHead;
        //desktxt.text=leagueInfo.Desk.ToString();
        //introductiontxt.text=leagueInfo.Introduction.ToString();
        //var playerId = Util.GetMyselfInfo().PlayerId;
        //if (leagueInfo.Creator==playerId)
        //    creater.gameObject.SetActive(true);
        //else
        //    creater.gameObject.SetActive(false);
    }
}

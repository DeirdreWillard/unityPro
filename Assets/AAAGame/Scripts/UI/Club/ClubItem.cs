
using NetMsg;
using UnityEngine;
using UnityEngine.UI;

public class ClubItem : MonoBehaviour
{

    // int64 leagueId = 1;
    // string leagueName = 2;
    // string leagueHead = 3;
    // int32 desk = 4;//桌子数量
    // string introduction = 5;//简介
    // bool isOpen = 6;//是否开放搜索 true 开放 false不开放
    //int64 creator = 7;//创建者

    public Text leagueIdtxt;
    public Text leagueNametxt;
    public Text leagueHeadtxt;
    public Text desktxt;
    public Text introductiontxt;
    public Text type;
    public Image creater;
    public GameObject redDot;
    public RawImage avatarImage;

    public LeagueInfo leagueInfoData;

    public void Init(LeagueInfo leagueInfo)
    {
        leagueInfoData = leagueInfo;
        leagueIdtxt.text = "ID: " + leagueInfo.LeagueId;
        leagueNametxt.text = leagueInfo.LeagueName;
        switch (leagueInfo.Type)
        {
            case 0:
                type.text = "俱乐部";
                break;
            case 1:
                type.text = "联盟";
                break;
            case 2:
                type.text = "超级联盟";
                break;
        }
        
        // leagueHeadtxt.text = leagueInfo.LeagueHead;
        desktxt.text = leagueInfo.Desk == 0 ? "暂无牌桌" : "牌桌数：" + leagueInfo.Desk;
        ChatManager.GetInstance().GetClubChatNumByClubID(leagueInfo.LeagueId, out int chatNum);
        redDot.SetActive(chatNum > 0);
        introductiontxt.text = leagueInfo.Introduction;
        if (leagueInfo.Creator == Util.GetMyselfInfo().PlayerId)
            creater.gameObject.SetActive(true);
        else
            creater.gameObject.SetActive(false);

        Util.DownloadHeadImage(avatarImage, leagueInfo.LeagueHead, leagueInfoData.Type);
    }

    public void Refresh()
    {
        Init(leagueInfoData);
    }
}

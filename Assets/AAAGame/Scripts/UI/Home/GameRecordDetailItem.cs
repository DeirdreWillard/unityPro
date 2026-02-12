
using NetMsg;

using UnityEngine;
using UnityEngine.UI;

public class GameRecordDetailItem : MonoBehaviour
{
    public Text nick;
    public Text bring;
    public Text hand;
    public Text change;
    public Text jackpot;
    public Text protect;
    public Text Index;
    public Text clubName;
    public Text rate;
    public Text joinTime;
    public RawImage avatar;

    public void Init(PlayerGameRecord data, int index, bool isCreate, DeskType deskType, MethodType method)
    {
        nick.text = data.Nick;
        Util.DownloadHeadImage(avatar, data.HeadImage);
        bring.text = "带入:" + data.Bring.ToString();
        hand.text = "手数:" + data.Hand.ToString();
        change.FormatRichText(float.Parse(data.Change));
        jackpot.text = "奖池:" + data.Jackpot.ToString();
        protect.gameObject.SetActive(method == MethodType.TexasPoker);
        protect.text = "保险:" + data.Protect;
        Index.text = index.ToString();
        if (RecordManager.GetInstance().joinType == 1 && Util.IsMyLeague(data.ClubId))
        {
            clubName.text = "俱乐部:" + data.ClubName;
            rate.gameObject.SetActive(true);
            rate.text = "荣誉榜:" + data.Rate.ToString();
            joinTime.text = Util.DateTimeToDateString(UtilityBuiltin.UnixTimeStampToDateTime(data.JoinTime), "HH:mm");
        }
        else
        {
            //自己显示俱乐部
            if (Util.IsMySelf(data.PlayerId))
            {
                //个人房没有俱乐部显示
                clubName.text = deskType == DeskType.Simple ? "" : "俱乐部:" + data.ClubName;
            }
            else
            {
                clubName.text = "";
            }
            rate.gameObject.SetActive(isCreate);
            rate.text = isCreate ? "荣誉榜:" + data.Rate.ToString() : "";
            joinTime.text = "";
        }
    }
}

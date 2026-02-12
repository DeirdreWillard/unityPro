
using NetMsg;

using UnityEngine;
using UnityEngine.UI;

public class RecordItem : MonoBehaviour
{
    public Text CurTimeText;
    public Text LeagueNameText;
    public Text BringText;
    public Text DeskIdText;
    public Text LossOrGainText;
    public Text BaseCoinText;
    public Text TimeText;

    public Image typeIcon;

    public Msg_GameRecord record;

    // Start is called before the first frame update
    void Start()
    {
        transform.GetComponent<Button>().onClick.AddListener(OnClickTransform);
    }

    public void OnClickTransform()
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        RecordManager.GetInstance().Send_GameRecordDetailRq(record.RecordId);
    }
    public void Init(Msg_GameRecord record)
    {
        this.record = record;
        CurTimeText.text = Util.DateTimeToDateString(UtilityBuiltin.UnixTimeStampToDateTime(this.record.RecordTime),"HH:mm");
        // DeskTypetText.text=recordf.DeskType.ToString();
        LeagueNameText.text = record.DeskName;
        BringText.text = "带入：" + record.TakeIn.ToString();
        DeskIdText.text = "ID：" + record.DeskId.ToString();
        LossOrGainText.text = record.LossOrGain.ToString();
        BaseCoinText.text = record.BaseCoin + "/" + (float.Parse(record.BaseCoin) * 2).ToString("F2");
        // TimeText.text = UtilityBuiltin.Valuer.ToTime(record.Time);
        //不显示持续时间 改成开始时间
        CurTimeText.text = Util.DateTimeToDateString(UtilityBuiltin.UnixTimeStampToDateTime(this.record.StartTime),"HH:mm");
        typeIcon.SetSprite(GameUtil.GetGameIconByMethodType(record.MethodType), true);
    }
}

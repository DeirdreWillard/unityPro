
using NetMsg;
using UnityEngine;
using UnityEngine.UI;

public class BringItem : MonoBehaviour
{
    public Text playerIdTxt;
    public Text nicknameTxt;
    public Text deskIdTxt;
    public Text deskNameTxt;
    public Text amountTxt;
    public Text timeTxt;
   
    public Button BtnOK;
    public Button BtnNO;
    public Text StateOverTxt;
    public BringInfo bringInfodata;
    public void Init(BringInfo bringInfo)
    {
        bringInfodata = bringInfo;
        if (bringInfodata.State==0)
        {
            BtnOK.gameObject.SetActive(true);
            BtnNO.gameObject.SetActive(true);
            StateOverTxt.gameObject.SetActive(false);
            BtnOK.onClick.AddListener(OK);
            BtnNO.onClick.AddListener(NO);
        }else if (bringInfodata.State == 1)
        {
            BtnOK.gameObject.SetActive(false);
            BtnNO.gameObject.SetActive(false);
            StateOverTxt.color = Color.green;
            StateOverTxt.text = $"由{(bringInfo.Option == null ? "系统" : bringInfo.Option.Nick)}审批同意";
            StateOverTxt.gameObject.SetActive(true);
        }else if (bringInfodata.State == 2)
        {
            BtnOK.gameObject.SetActive(false);
            BtnNO.gameObject.SetActive(false);
            StateOverTxt.color = Color.red;
            StateOverTxt.text = $"由{(bringInfo.Option == null ? "系统" : bringInfo.Option.Nick)}审批拒绝";
            StateOverTxt.gameObject.SetActive(true);
        }else if (bringInfodata.State == 3)
        {
           
        }else if (bringInfodata.State == 4)
        {
            BtnOK.gameObject.SetActive(false);
            BtnNO.gameObject.SetActive(false);
            StateOverTxt.color = Color.gray;
            StateOverTxt.text = "过期拒绝";
            StateOverTxt.gameObject.SetActive(true);
        }

        playerIdTxt.text = $"ID:{bringInfo.PlayerId}";
        nicknameTxt.text = bringInfo.Nick.ToString();
        deskIdTxt.text = bringInfo.DeskId.ToString();
        deskNameTxt.text = $"房间: {bringInfo.DeskName}";
        amountTxt.text = $"请求带入 {bringInfo.Amount}";
        timeTxt.text = Util.MillisecondsToDateString(bringInfo.Time);
    }
    public void OK()
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        BringManager.GetInstance().cruBringInfo = bringInfodata;
        BringManager.GetInstance().Send_BringOptionRq(bringInfodata.PlayerId, 0);
    }
    public void NO()
    {
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        BringManager.GetInstance().cruBringInfo = bringInfodata;
        BringManager.GetInstance().Send_BringOptionRq(bringInfodata.PlayerId, 1);
    }
}

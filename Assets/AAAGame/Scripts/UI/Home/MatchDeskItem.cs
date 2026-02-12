using System;
using NetMsg;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MatchDeskItem : MonoBehaviour
{
    public Text methodType;
    public Image typeImage;
    public Text roomName;
    public Text time;
    public Text award;
    public Text tickets;
    public Text people;
    public Text formTime;
    public GameObject formTimeBg;
    public GameObject ticketsImg;
    public GameObject diamondImg;


    public Msg_Match matchInfo;

    private Coroutine m_CountdownCoroutine;

    private void OnDisable()
    {
        StopCountdown();
    }
    
    private void OnDestroy()
    {
        StopCountdown();
    }

    public void Init(Msg_Match ack)
    {
        matchInfo = ack;
        typeImage.SetSprite(GameUtil.GetGameIconByMethodType(matchInfo.MethodType), true);
        switch (matchInfo.MethodType)
        {
            case MethodType.NiuNiu:
                methodType.text = "牛牛";
                break;
            case MethodType.GoldenFlower:
                methodType.text = "金花";
                break;
            case MethodType.TexasPoker:
                methodType.text = "德州";
                break;
            case MethodType.CompareChicken:
                methodType.text = "比鸡";
                break;
        }
        roomName.text = matchInfo.MatchName;
        people.text = matchInfo.Num.ToString();
        people.GetComponentInChildren<Image>().color = matchInfo.SignUpState == 1 ? Color.red : Color.white;
        time.text = $"开赛时间: {Util.MillisecondsToDateString(matchInfo.StartTime, "MM/dd HH:mm")}";
        award.text = matchInfo.Bonus.ToString();
        var (costType, cost) = Util.GetCurrentMatchTicketInfo(matchInfo);
        tickets.text = cost.ToString();
        ticketsImg.SetActive(costType == Util.MatchCostType.Tick);
        diamondImg.SetActive(costType == Util.MatchCostType.Diamond);
        switch (matchInfo.MatchState)
        {
            case 0:
            case 1:
                formTimeBg.GetComponent<Image>().color = new Color(0, 174/255f, 102/255f);
                formTimeBg.transform.Find("Text").GetComponent<Text>().text = "距开赛";
                UpdateCountdownDisplay();
                StartCountdown();
                break;
            case 2:
                formTimeBg.GetComponent<Image>().color = new Color(0, 81/255f, 255/255f);
                formTimeBg.transform.Find("Text").GetComponent<Text>().text = "剩余人数";
                formTime.text = matchInfo.AliveNum.ToString();
                break;
            case 3:
            case 4:
                formTimeBg.GetComponent<Image>().color = new Color(135/255f, 147/255f, 163/255f);
                formTimeBg.transform.Find("Text").GetComponent<Text>().text = "已结束";
                formTime.text = "00:00";
                break;
        }
    }
    
    #region Countdown Methods
    private void StartCountdown()
    {
        StopCountdown();
        m_CountdownCoroutine = CoroutineRunner.Instance.StartCoroutine(CountdownCoroutine());
    }
    
    private void StopCountdown()
    {
        if (m_CountdownCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(m_CountdownCoroutine);
            m_CountdownCoroutine = null;
        }
    }
    
    private IEnumerator CountdownCoroutine()
    {
        while (matchInfo != null && matchInfo.MatchState < 2)
        {
            UpdateCountdownDisplay();
            yield return new WaitForSeconds(1f);
        }
    }
    
    private void UpdateCountdownDisplay()
    {
        long timeDiff = matchInfo.StartTime - Util.GetServerTime();
        if (timeDiff >= 24 * 3600 * 1000) // 大于等于24小时
        {
            int days = Mathf.CeilToInt(timeDiff / (1000f * 60f * 60f * 24f));
            formTime.text = $"{days}天";
        }
        else if (timeDiff >= 3600 * 1000) // 大于等于1小时
        {
            int hours = Mathf.CeilToInt(timeDiff / (1000f * 60f * 60f));
            formTime.text = $"{hours}小时";
        }
        else if (timeDiff > 0) // 小于1小时
        {
            TimeSpan ts = TimeSpan.FromMilliseconds(timeDiff);
            formTime.text = $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
        else
        {
            formTime.text = "00:00";
        }
    }
    #endregion
}

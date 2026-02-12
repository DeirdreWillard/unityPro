
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

namespace UnityEngine.UI.Extensions.Examples
{
    public class ScrollingCalendar : MonoBehaviour
    {
        public RectTransform monthsScrollingPanel;
        public RectTransform yearsScrollingPanel;
        public RectTransform daysScrollingPanel;
        public RectTransform hoursScrollingPanel;

        public ScrollRect monthsScrollRect;
        public ScrollRect yearsScrollRect;
        public ScrollRect daysScrollRect;
        public ScrollRect hoursScrollRect;


        public GameObject dateItem;

        private GameObject[] monthsButtons;
        private GameObject[] yearsButtons;
        private GameObject[] daysButtons;
        private GameObject[] hoursButtons;


        public RectTransform monthCenter;
        public RectTransform yearsCenter;
        public RectTransform daysCenter;
        public RectTransform hoursCenter;


        UIVerticalScroller yearsVerticalScroller;
        UIVerticalScroller monthsVerticalScroller;
        UIVerticalScroller daysVerticalScroller;
        public UIVerticalScroller hoursVerticalScroller;

        public Button confirmBtn;
        public Button cancelBtn;

        public Text dateText;

        private int curMonth;

        //当前列表
        public DateTime[] dateTimes = new DateTime[7];

        private void InitializeYears()
        {
            foreach (Transform item in yearsScrollingPanel.transform)
            {
                Destroy(item.gameObject);
            }
            int[] arrayYears;
            if (dateTimes[0].Year == dateTimes[^1].Year)
            {
                arrayYears = new int[1];
                arrayYears[0] = dateTimes[0].Year;
            }
            else
            {
                arrayYears = new int[2];
                arrayYears[0] = dateTimes[^1].Year;
                arrayYears[1] = dateTimes[0].Year;
            }
            yearsButtons = new GameObject[arrayYears.Length];
            for (int i = 0; i < arrayYears.Length; i++)
            {
                arrayYears[i] = dateTimes[0].Year + i;

                GameObject clone = Instantiate(dateItem, yearsScrollingPanel);
                clone.transform.localScale = new Vector3(1, 1, 1);
                clone.GetComponentInChildren<Text>().text = arrayYears[i] + "年";
                clone.name = "" + arrayYears[i];
                clone.AddComponent<CanvasGroup>();
                yearsButtons[i] = clone;
            }
            yearsVerticalScroller = new UIVerticalScroller(yearsCenter, yearsCenter, yearsScrollRect, yearsButtons);
            yearsVerticalScroller.Start();
        }

        //Initialize Months
        private void InitializeMonths(int year)
        {
            foreach (Transform item in monthsScrollingPanel.transform)
            {
                Destroy(item.gameObject);
            }
            int[] arrayMonths;
            if (dateTimes[0].Month == dateTimes[^1].Month)
            {
                arrayMonths = new int[1];
                arrayMonths[0] = dateTimes[0].Month;
            }
            else
            {
                if (dateTimes[0].Year == year && dateTimes[^1].Year == year)
                {
                    arrayMonths = new int[2];
                    arrayMonths[0] = dateTimes[0].Month;
                    arrayMonths[1] = dateTimes[^1].Month;
                }else{
                    arrayMonths = new int[1];
                    arrayMonths[0] = dateTimes[0].Year == year ? dateTimes[0].Month : dateTimes[^1].Month;
                }
            }

            monthsButtons = new GameObject[arrayMonths.Length];
            for (int i = 0; i < arrayMonths.Length; i++)
            {
                GameObject clone = Instantiate(dateItem, monthsScrollingPanel);
                clone.transform.localScale = new Vector3(1, 1, 1);
                clone.GetComponentInChildren<Text>().text = arrayMonths[i] + "月";
                clone.name = "" + arrayMonths[i];
                clone.AddComponent<CanvasGroup>();
                monthsButtons[i] = clone;
            }
            monthsVerticalScroller = new UIVerticalScroller(monthCenter, monthCenter, monthsScrollRect, monthsButtons);
            monthsVerticalScroller.Start();
        }

        private void InitializeDays(int month)
        {
            foreach (Transform item in daysScrollingPanel.transform)
            {
                Destroy(item.gameObject);
            }
            List<DateTime> arrayDays = new();
            if (dateTimes[0].Month == dateTimes[^1].Month)
            {
                arrayDays = dateTimes.ToList<DateTime>();
            }
            else
            {
                for (int i = 0; i < dateTimes.Length; i++)
                {
                    if (dateTimes[i].Month == month)
                    {
                        arrayDays.Add(dateTimes[i]);
                    }
                }
            }
            // arrayDays.Sort();
            daysButtons = new GameObject[arrayDays.Count];

            for (var i = 0; i < arrayDays.Count; i++)
            {
                GameObject clone = Instantiate(dateItem, daysScrollingPanel);
                clone.GetComponentInChildren<Text>().text = arrayDays[i].Day + "日";
                clone.name = arrayDays[i].Day + "";
                clone.AddComponent<CanvasGroup>();
                daysButtons[i] = clone;
            }
            daysVerticalScroller = new UIVerticalScroller(daysCenter, daysCenter, daysScrollRect, daysButtons);
            daysVerticalScroller.Start();
        }

        private void InitializeHours()
        {
            foreach (Transform item in hoursScrollingPanel.transform)
            {
                Destroy(item.gameObject);
            }
            int[] arrayHours = new int[24];

            hoursButtons = new GameObject[arrayHours.Length];
            for (int i = 0; i < arrayHours.Length; i++)
            {
                arrayHours[i] = i;

                GameObject clone = Instantiate(dateItem, hoursScrollingPanel);
                clone.transform.localScale = new Vector3(1, 1, 1);
                clone.GetComponentInChildren<Text>().text = i + "时";
                clone.name = i + "";
                clone.AddComponent<CanvasGroup>();
                hoursButtons[i] = clone;
            }
            hoursVerticalScroller = new UIVerticalScroller(hoursCenter, hoursCenter, hoursScrollRect, hoursButtons);
            hoursVerticalScroller.Start();
        }

        // Use this for initialization
        public void Awake()
        {
            // SetDate();
        }

        public void SetDate(DateTime dateTime, UnityAction Confirm = null,UnityAction cancel = null)
        {
            confirmBtn.onClick.RemoveAllListeners();
            cancelBtn.onClick.RemoveAllListeners();
            confirmBtn.onClick.AddListener(Confirm);
            cancelBtn.onClick.AddListener(cancel);
            for (int i = 0; i < dateTimes.Length; i++)
            {
                dateTimes[i] = dateTime.AddDays(0 - i);
            }

            InitializeYears();
            InitializeMonths(dateTimes[0].Year);
            InitializeDays(dateTimes[0].Month);
            InitializeHours();
        }

        public DateTime GetDateResult()
        {
            int day = int.Parse(daysVerticalScroller.Result);
            int month = int.Parse(monthsVerticalScroller.Result);
            int year = int.Parse(yearsVerticalScroller.Result);
            int hour = int.Parse(hoursVerticalScroller.Result);
            return new DateTime(year, month, day, hour, 0, 0) { };
        }

        private void Update()
        {
            monthsVerticalScroller.Update();
            yearsVerticalScroller.Update();
            daysVerticalScroller.Update();
            hoursVerticalScroller.Update();

            int day = int.Parse(daysVerticalScroller.Result);
            int month = int.Parse(monthsVerticalScroller.Result);
            int year = int.Parse(yearsVerticalScroller.Result);
            int hour = int.Parse(hoursVerticalScroller.Result);
            // Debug.LogError("month" + month);
            if (curMonth != month)
            {
                curMonth = month;
                InitializeMonths(year);
                InitializeDays(month);
            }
            dateText.text = string.Format("{0}/{1}/{2}  {3}", year, month, day, hour);
        }



    }
}
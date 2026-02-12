using System;
using System.Collections.Generic;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;

public class CreateDZRoom : MonoBehaviour
{
	public CreateRoomPanel createroom;

	public Slider Slider_MangZhu;
	public Slider Slider_QianZhu;
	public List<Text> Text_QianZhu;
	public Slider Slider_ShiJian;
	public Text[] bringValueTexts;
	public Slider minBringInSlider;
	public Slider Slider_RenShu;
	public Slider Slider_ZiDong;
	public Slider Slider_RuChi;
	public Slider Slider_ShouShu;
	public Slider Slider_CaoZuo;
	public Slider Slider_BaoXian;
	public Slider Slider_Bili;
	public Slider Slider_Biaozhun;
	public Slider Slider_FengDing;


	public Toggle Toggle_MangZhu;
	public Toggle Toggle_BuMang;
	public Toggle Toggle_DaiRu;
	public Toggle Toggle_BaoXian;
	public Toggle Toggle_YanChi;
	public Toggle Toggle_LiangPan;
	public Toggle Toggle_IP;
	public Toggle Toggle_GPS;
	public Toggle Toggle_GaiPai;
	public Toggle Toggle_BaoJi;
	public Toggle Toggle_YouYu;
	public Toggle Toggle_SuiJi;
	public Toggle Toggle_ShouPai;
	public Toggle Toggle_JiangChi;

	public Text TextMangZhu;
	public Text TextDaiRu;
	public GameObject GO_JiangChi;
	public GameObject GO_DaiRu;

	public Toggle 禁止聊天Toggle;
	public Toggle 禁止打字Toggle;

	public readonly float[] MangZhu = { 0.1f, 0.25f, 0.5f, 1, 2, 3, 5, 10, 20, 25, 50, 100, 200, 300, 500, 1000 };
	public readonly float[] QianZhu = { 0, 0.5f, 1f, 2f, 3f, 4f, 5f };
	private readonly long[] ShiJian = { 30, 60, 90, 120, 180, 240, 300, 360 };
	private readonly int[] RenShu = { 2, 3, 4, 5, 6, 7, 8, 9 };
	private readonly int[] ZiDong = { 0, 2, 3, 4, 5, 6, 7, 8, 9 };
	private readonly int[] RuChi = { 0, 25, 30, 35, 40, 45 };
	private readonly int[] ShouShu = { 0, 100, 200, 300, 400, 500 };
	private readonly int[] CaoZuo = { 10, 12, 15, 20 };
	private readonly int[] BaoXian = { 18, 20, 25, 30 };
    private readonly int[] Bili = { 0, 10, 15, 20, 25, 30, 35, 40, 45, 50 };
    private readonly int[] BiaoZhun = { 1, 10, 20, 30, 40, 50, 60, 70, 80, 100 };
	private readonly float[] FengDing = { 0, 0.5f, 1, 2, 3, 4, 5 };

	public Desk_BaseConfig GetDesk_BaseConfig()
	{
		string[] parts = TextDaiRu.text.Split('-');
        int start = -1;
        int end = -1;
        if (parts.Length == 2)
        {
            start = int.Parse(parts[0]);
            end = int.Parse(parts[1]);
        }
		return new() {
			BaseCoin = MangZhu[(int)Slider_MangZhu.value],
			PlayerTime = ShiJian[(int)Slider_ShiJian.value],
			PlayerNum = RenShu[(int)Slider_RenShu.value],
			MinBringIn = start,
			MaxBringIn = end,
			Jackpot = Toggle_JiangChi.isOn,
			IsGrant = Toggle_DaiRu.isOn,
			AutoStart = (int)Slider_ZiDong.value != 0,
			AutoStartNum = ZiDong[(int)Slider_ZiDong.value],
			Rate = new PairInt {
				Key = Bili[(int)Slider_Bili.value],
				Val = BiaoZhun[(int)Slider_Biaozhun.value],
			},
			IpLimit = Toggle_IP.isOn,
			GpsLimit = Toggle_GPS.isOn,
			ForbidVoice = 禁止聊天Toggle.isOn,
			Forbid = 禁止打字Toggle.isOn,
		};
	}

	public TexasPokerConfig GetDZConfig()
	{
		return new() {
			Ante = MangZhu[(int)Slider_MangZhu.value] * QianZhu[(int)Slider_QianZhu.value],
			Protect = Toggle_BaoXian.isOn,
			Straddle = Toggle_MangZhu.isOn,
			RepairCoin = Toggle_BuMang.isOn,
			Critcal = Toggle_BaoJi.isOn,
			DelayLook = Toggle_YanChi.isOn,
			ShowCard = Toggle_LiangPan.isOn,
			GoldLook = Toggle_ShouPai.isOn,
			RandomPos = Toggle_SuiJi.isOn,
		};
	}

	public void ApplyDeskConfigToUI(Msg_CreateDeskRq msg)
	{
		if (msg == null)
		{
			if (createroom.leagueInfo == null) {
				GO_JiangChi.SetActive(false);
				Toggle_JiangChi.isOn = false;
			}
			else {
				GO_JiangChi.SetActive(true);
				Toggle_JiangChi.isOn = true;
			}
			Slider_MangZhu.value = 0;
			if (createroom.leagueInfo == null || createroom.leagueInfo.Type == 0) {
				Toggle_DaiRu.interactable = true;
				GO_DaiRu.SetActive(true);
				Toggle_DaiRu.isOn = true;
			}
			else {
				Toggle_DaiRu.interactable = false;
				GO_DaiRu.SetActive(false);
				Toggle_DaiRu.isOn = false;
			}
		}
		else {
			if (msg.Floor != 0) return;
			Slider_MangZhu.value = 0;
			int index;

			index = Array.IndexOf(MangZhu, msg.Config.BaseCoin);
			if (index != -1) Slider_MangZhu.value = index;

			Slider_ShiJian.value = 0;
			index = Array.IndexOf(ShiJian, msg.Config.PlayerTime);
			if (index != -1) Slider_ShiJian.value = index;

			Slider_RenShu.value = 0;
			index = Array.IndexOf(RenShu, msg.Config.PlayerNum);
			if (index != -1) Slider_RenShu.value = index;

			// minBringInSlider.value = 0;
			// int minBringInIndex = Array.IndexOf(minBringIns, msg.Config.MinBringIn / msg.Config.BaseCoin);
			// if (minBringInIndex != -1) minBringInSlider.value = minBringInIndex;

			Slider_Bili.value = 0;
			index = -1;
			if (msg.Config.Rate != null) {
				index = Array.IndexOf(Bili, msg.Config.Rate.Key);
			}
			if (index != -1) Slider_Bili.value = index;

			Slider_Biaozhun.value = 0;
			index = -1;
			if (msg.Config.Rate != null) {
				index = Array.IndexOf(BiaoZhun, msg.Config.Rate.Val);
			}
			if (index != -1) Slider_Biaozhun.value = index;

			Toggle_JiangChi.isOn = msg.Config.Jackpot;
			Toggle_DaiRu.isOn = msg.Config.IsGrant;

			if (msg.TexasPokerConfig != null) {
				Slider_QianZhu.value = Array.IndexOf(QianZhu, msg.TexasPokerConfig.Ante);
				Toggle_BaoXian.isOn = msg.TexasPokerConfig.Protect;
				Toggle_LiangPan.isOn = msg.TexasPokerConfig.Straddle;
				Toggle_MangZhu.isOn = msg.TexasPokerConfig.Straddle;
				Toggle_BuMang.isOn = msg.TexasPokerConfig.RepairCoin;
				Toggle_BaoJi.isOn = msg.TexasPokerConfig.Critcal;
				Toggle_YanChi.isOn = msg.TexasPokerConfig.DelayLook;
				Toggle_IP.isOn = msg.Config.IpLimit;
				Toggle_GPS.isOn = msg.Config.GpsLimit;
				禁止聊天Toggle.isOn = msg.Config.ForbidVoice;
				禁止打字Toggle.isOn = msg.Config.Forbid;
				Toggle_LiangPan.isOn = msg.TexasPokerConfig.ShowCard;
				Toggle_ShouPai.isOn = msg.TexasPokerConfig.GoldLook;
				Toggle_SuiJi.isOn = msg.TexasPokerConfig.RandomPos;
			}

			if (createroom.leagueInfo == null) {
				GO_JiangChi.SetActive(false);
			}
			else {
				GO_JiangChi.SetActive(true);
			}
			GO_DaiRu.SetActive(true);
			Toggle_BaoXian.isOn = true;
			if (createroom.leagueInfo == null || createroom.leagueInfo.Type == 0) {
				Toggle_BaoXian.interactable = true;
			}
			else {
				Toggle_BaoXian.interactable = false;
			}
			if (createroom.leagueInfo == null) {
				Toggle_JiangChi.gameObject.SetActive(false);
			}
			else {
				Toggle_JiangChi.gameObject.SetActive(true);
			}
			if (createroom.leagueInfo == null || createroom.leagueInfo.Type == 0) {
				Toggle_DaiRu.gameObject.SetActive(true);
				Toggle_DaiRu.interactable = true;
			}
			else {
				Toggle_DaiRu.gameObject.SetActive(true);
				Toggle_DaiRu.isOn = true;
				Toggle_DaiRu.interactable = false;
			}
		}
		OnSliderMangZhuChanged();
	}

	public void OnSliderMangZhuChanged()
	{
		for (var i = 0;i < Text_QianZhu.Count;i++) {
			if ((MangZhu[(int)Slider_MangZhu.value] * QianZhu[i]) > 1) {
				Text_QianZhu[i].text = Util.FormatAmount((int)(MangZhu[(int)Slider_MangZhu.value] * QianZhu[i]));
			}
			else {
				Text_QianZhu[i].text = Util.FormatAmount(MangZhu[(int)Slider_MangZhu.value] * QianZhu[i]);
			}
		}
		if (MangZhu[(int)Slider_MangZhu.value] > 1) {
			TextMangZhu.text = Util.FormatAmount(MangZhu[(int)Slider_MangZhu.value]) + "/" + Util.FormatAmount(MangZhu[(int)Slider_MangZhu.value] * 2);
		}
		else {
			TextMangZhu.text = Util.FormatAmount(MangZhu[(int)Slider_MangZhu.value]) + "/" + Util.FormatAmount(MangZhu[(int)Slider_MangZhu.value] * 2);
		}
		SetBringValueText();
		UpdateExpend();
		if (!createroom.m_IsUpdatingSlider)
        {
            createroom.LoadDefaultConfigs(MethodType.TexasPoker, (int)Slider_MangZhu.value);
        }
	}

    public void OnplayerTimeSlidValueChanged()
    {
        UpdateExpend();
    }

    public void UpdateExpend(){
        long time = ShiJian[(int)Slider_ShiJian.value];
        float baseCoin = MangZhu[(int)Slider_MangZhu.value];
        long expend = time / 30 * GlobalManager.GetInstance().GetMsg_CreateDeskConfigByType(MethodType.TexasPoker, baseCoin,
			createroom.leagueInfo == null ? DeskType.Simple : createroom.leagueInfo.Type == 0 ? DeskType.Guild : DeskType.League);
		createroom.SetExpendText((int)expend);
    }

	public void OnMinBringSlidValueChanged()
	{
		int bei = 100 + (int)minBringInSlider.value * 50;
		TextDaiRu.text = MangZhu[(int)Slider_MangZhu.value] * bei + "-" + (MangZhu[(int)Slider_MangZhu.value] * bei * 10);
	}

	public void SetBringValueText()
	{
		int bei = 100 + (int)minBringInSlider.value * 50;
		TextDaiRu.text = MangZhu[(int)Slider_MangZhu.value] * bei + "-" + (MangZhu[(int)Slider_MangZhu.value] * bei * 10);
		for (int i = 0; i < bringValueTexts.Length; i++)
		{
			bringValueTexts[i].text = (MangZhu[(int)Slider_MangZhu.value] * (100 + i * 50)).ToString();
		}
	}

}

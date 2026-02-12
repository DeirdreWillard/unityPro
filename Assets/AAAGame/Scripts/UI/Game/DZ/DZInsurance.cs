
using System;
using System.Collections.Generic;
using System.Linq;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;

public class DZInsurance : MonoBehaviour
{
	public DZGamePanel GamePanel;
	DZProcedure Procedure;
	DeskPlayer selfplayer;

	public Text TextPot;
	public Text TextCountdown;
	public GameObject GoAddTimer;
	public Text TextAddTime;

	public Slider SliderCountdown;

	public GameObject GoPlayerList;
	public GameObject GoPlayerItem;

	public List<Card> CommonCard;

	public Text TextBetPercent;
	public Text TextSelected;

	public GameObject GoCardList;
	public GameObject GoCardList2;
	public GameObject GoCardItem;

	public Text TextTurnBuy;
	public Text TextPay;
	public Text TextBuy;

	public GameObject GoPanelBuy1;
	public GameObject GoPanelBuy2;
	public GameObject GoPanelBuy3;

	public GameObject GoBuyMin, GoBuyCancel, GoBuyBase;
	public Text TextBuyAll, TextBuyBase, TextBuyMin;

	public Slider SliderBuy;
	public GameObject GoBaseBuy;

	public Image ImageAvatar;
	public GameObject GoVip;
	public Text TextNick;
	public Text TextBuyCountdown;

	public bool binit = false;

	public float mincoin = 0;
	public float maxcoin = 0, servermax = 0;
	public float basecoin = 0;

	public List<int> selectlist = new();

	readonly List<float> multi = new() {30, 16, 10, 8, 6, 5, 4, 3.5f, 3, 2.5f, 2.2f, 2.0f, 1.8f, 1.6f, 1.4f, 1.3f };

	float pot = 0, protect = 0; int potnumber = 0; long protectid = 0;

	void OnEnable() {
	}

	void OnDisable() {
		binit = false;
	}

	void Update() {
		float remaintime = (GamePanel.nexttime - Util.GetServerTime()) / 1000;
		if (remaintime < 0) {
			remaintime = 0;
		}
		TextCountdown.text = $"倒计时: {(int)remaintime}";
		SliderCountdown.value = remaintime;
		TextBuyCountdown.text = $"{(int)remaintime}S";
	}

	public void Refresh() {
		RefreshInfo();
	}

	public void Init(Msg_ProtectInfo ack) {
		Procedure = GF.Procedure.CurrentProcedure as DZProcedure;
		selfplayer = Procedure.GetSelfPlayer();
		binit = true;
		SliderCountdown.maxValue = (int)((GamePanel.nexttime - GamePanel.starttime) / 1000);
		GF.LogInfo("刷新保险面板. ", $"nexttime: {GamePanel.nexttime} starttime: {GamePanel.starttime} nowtime: {Util.GetServerTime()} maxvalue: {SliderCountdown.maxValue} remain: {(GamePanel.nexttime - GamePanel.starttime) / 1000}");
		if (ack.History != null) {
			GF.LogInfo("保险历史.", $"history: {ack.History?.Protect} playerid: {ack.History?.PlayerId}");
		}
		pot = float.Parse(ack.Pot); potnumber = ack.PotPlayer.Count();
		if (ack.History != null && Util.IsMySelf(ack.History.PlayerId) == true) {
			float.TryParse(ack.History.Protect, out protect);
			protectid = ack.History.PlayerId;
		}
		else {
			protect = 0;
			protectid = 0;
		}
		TextPot.text = $"池底: {ack.Pot}";
		if (GamePanel.SystemConfig.ContainsKey(14) == true && selfplayer != null) {
			TextAddTime.text = (GamePanel.SystemConfig[14] * (int)Math.Pow(2, selfplayer.BuyTime)).ToString();
		}
		else {
			TextAddTime.text = "";
		}
		if (ack.History == null) {
			TextTurnBuy.text = "转牌保额:0.00";
		}
		else {
			TextTurnBuy.text = $"转牌保额:{ack.History.Protect}";
		}
		if (selfplayer == null) {
			HideBuyBtn();
		}
		else if (selfplayer.IsGive == true) {
			HideBuyBtn();
		}
		else if (ack.OptionPlayer.ToList().Contains(selfplayer.BasePlayer.PlayerId) == true) {
			GoAddTimer.SetActive(true);
			GoPanelBuy1.SetActive(true);
			GoPanelBuy2.SetActive(false);
			GoPanelBuy3.SetActive(false);
			if (Procedure.deskinfo.TexasState == TexasState.TurnProtect) {
				GoBuyMin.SetActive(false);
				GoBuyCancel.SetActive(true);
			}
			if (Procedure.deskinfo.TexasState == TexasState.RiverProtect) {
				if (ack.History == null || ack.History.Protect == "" || ack.History.Protect == "0.00" || ack.History.Protect == "0" || Util.IsMySelf(ack.History.PlayerId) == false) {
					GoBuyMin.SetActive(false);
					GoBuyCancel.SetActive(true);
				}
				else {
					GoBuyMin.SetActive(true);
					GoBuyCancel.SetActive(false);
				}
			}
		}
		else {
			HideBuyBtn();
			DeskPlayer deskplayer = Procedure.GetPlayerByID(ack.OptionPlayer[0]);
			if (deskplayer != null) {
				TextNick.FormatNickname(deskplayer.BasePlayer.Nick);
			}
		}
		foreach (Transform child in GoPlayerList.transform) {
			Destroy(child.gameObject);
		}
		if (selfplayer != null && selfplayer.InGame == true && selfplayer.IsGive == false && ack.PotPlayer.ToList().Contains(selfplayer.BasePlayer.PlayerId) == true) {
			AddPlayer(selfplayer, "我");
		}
		foreach (DeskPlayer deskplayer in Procedure.deskinfo.DeskPlayers) {
			if (deskplayer.InGame == false) continue;
			if (deskplayer.IsGive == true) continue;
			if (selfplayer != null && deskplayer.BasePlayer.PlayerId == selfplayer.BasePlayer.PlayerId) continue;
			if (ack.OptionPlayer.ToList().Contains(deskplayer.BasePlayer.PlayerId) == true) {
				AddPlayer(deskplayer, "购买中");
			}
		}
		foreach (DeskPlayer deskplayer in Procedure.deskinfo.DeskPlayers) {
			if (deskplayer.InGame == false) continue;
			if (deskplayer.IsGive == true) continue;
			if (selfplayer != null && deskplayer.BasePlayer.PlayerId == selfplayer.BasePlayer.PlayerId) continue;
			if (ack.OptionPlayer.Contains(deskplayer.BasePlayer.PlayerId) == true) continue;
			if (ack.PotPlayer.ToList().Contains(deskplayer.BasePlayer.PlayerId) == true) {
				AddPlayer(deskplayer, "等待中");
			}
		}
		List<int> commoncards = Procedure.deskinfo.CommonCards.ToList();
		for (var i = 0;i < 5;i++) {
			if (i < commoncards.Count) {
				CommonCard[i].Init(commoncards[i]);
				CommonCard[i].gameObject.SetActive(true);
			}
			else {
				CommonCard[i].Init(-1);
				CommonCard[i].gameObject.SetActive(false);
			}
		}

		foreach (Transform child in GoCardList.transform) {
			if (child.name == "Text") continue;
			Destroy(child.gameObject);
		}
		selectlist.Clear();
		List<int> protectcard = ack.ProtectCard.ToList();
		protectcard.Sort((a, b) => {
			int modA = a % 16, modB = b % 16;
			if (modA == modB) {
				return (a / 16).CompareTo(b / 16);
			}
			else {
				return modA.CompareTo(modB);
			}
		});
		foreach (var card in protectcard) {
			GameObject go = Instantiate(GoCardItem);
			go.transform.SetParent(GoCardList.transform);
			go.transform.localScale = Vector3.one;
			go.transform.localPosition = Vector3.zero;
			go.transform.Find("Card").GetComponent<Card>().Init(card);
			selectlist.Add(card);
			go.GetComponent<Toggle>().isOn = true;
			go.SetActive(true);
			if (selfplayer != null && ack.OptionPlayer.ToList().Contains(selfplayer.BasePlayer.PlayerId) == true) {
				if (Procedure.deskinfo.TexasState == TexasState.RiverProtect && protect != 0) {
					go.GetComponent<Toggle>().interactable = false;
				}
				else {
					go.GetComponent<Toggle>().interactable = true;
				}
			}
			else {
				go.GetComponent<Toggle>().interactable = false;
			}
		}
		if (ack.BisectCard.Count > 0)
		{
			GoCardList2.SetActive(true);
			foreach (Transform child in GoCardList2.transform) {
				if (child.name == "Text") continue;
				Destroy(child.gameObject);
			}
			List<int> bisectcard = ack.BisectCard.ToList();
			bisectcard.Sort((a, b) => {
				int modA = a % 16, modB = b % 16;
				if (modA == modB) {
					return (a / 16).CompareTo(b / 16);
				}
				else {
					return modA.CompareTo(modB);
				}
			});
			foreach (var card in bisectcard) { 
				GameObject go = Instantiate(GoCardItem);
				go.transform.SetParent(GoCardList2.transform);
				go.transform.localScale = Vector3.one;
				go.transform.localPosition = Vector3.zero;
				go.transform.Find("Card").GetComponent<Card>().Init(card);
				go.transform.GetComponent<Toggle>().isOn = false;
				go.transform.GetComponent<Toggle>().interactable = false;
				go.SetActive(true);
			}
		}
		else { GoCardList2.SetActive(false); }
		GoBuyBase.SetActive(true);
		if (selfplayer != null && ack.OptionPlayer.ToList().Contains(selfplayer.BasePlayer.PlayerId) == true) {
			if (Procedure.deskinfo.TexasState == TexasState.RiverProtect && protectid == selfplayer.BasePlayer.PlayerId) {
				GoBuyBase.SetActive(false);
			}
		}
		servermax = float.Parse(ack.MaxBuy);
		RefreshInfo(true);
		GF.LogInfo($"mincoin:{mincoin},maxcoin:{maxcoin},maxbuy:{servermax},basecoin:{basecoin} starttime: {GamePanel.starttime} nexttime: {GamePanel.nexttime} nowtime: {Util.GetServerTime()}");
	}

	public void Buy(Msg_SynProtectCard ack) {
		foreach (Transform t in GoPlayerList.transform) {
			if (t.name != ack.PlayerId.ToString()) continue;
			t.Find("状态").GetComponent<Text>().text = "已购买";
		}
	}

	public void HideBuyBtn(){
		GoAddTimer.SetActive(false);
		GoPanelBuy1.SetActive(false);
		GoPanelBuy2.SetActive(false);
		GoPanelBuy3.SetActive(true);
	}

	void AddPlayer(DeskPlayer deskplayer, string status) {
		GameObject go = Instantiate(GoPlayerItem);
		go.transform.SetParent(GoPlayerList.transform);
		go.transform.localScale = Vector3.one;
		go.transform.localPosition = Vector3.zero;
		go.transform.Find("昵称").GetComponent<Text>().FormatNickname(deskplayer.BasePlayer.Nick);
		go.transform.Find("Card1").GetComponent<Card>().Init(deskplayer.HandCard[0]);
		go.transform.Find("Card2").GetComponent<Card>().Init(deskplayer.HandCard[1]);
		go.transform.Find("状态").GetComponent<Text>().text = status;
		go.name = deskplayer.BasePlayer.PlayerId.ToString();
		go.SetActive(true);
	}

	public void Reset() {
	}

	public void Clear() {
		mincoin = maxcoin = basecoin = 0; servermax = 0;
		selectlist.Clear();
		foreach (Transform child in GoCardList.transform) {
			if (child.name == "Text") continue;
			Destroy(child.gameObject);
		}
		foreach (Transform child in GoCardList2.transform) {
			if (child.name == "Text") continue;
			Destroy(child.gameObject);
		}
		pot = protect = 0; protectid = 0;
	}

	public void OnClickSelectAll() {
		foreach (Transform child in GoCardList.transform) {
			if (child.name == "Text") continue;
			child.GetComponent<Toggle>().isOn = true;
		}
		RefreshInfo();
	}

	public void RefreshAddTime() {
		if (GamePanel != null && GamePanel.SystemConfig.ContainsKey(14) == true && selfplayer != null) {
			TextAddTime.text = (GamePanel.SystemConfig[14] * (int)Math.Pow(2, selfplayer.BuyTime)).ToString();
		}
		else {
			TextAddTime.text = "";
		}
	}

	void RefreshInfo(bool init = false) {
		int cardselect = selectlist.Count();
		if (cardselect > 16) cardselect = 16;
		TextBuy.text = $"保险投入:{SliderBuy.value / 100:F2}";
		if (selfplayer == null || selfplayer.IsGive == true) {
			GoAddTimer.SetActive(false);
			GoPanelBuy1.SetActive(false);
		}

		TextPay.text = $"保险赔付:{SliderBuy.value / 100 * multi[cardselect - 1]:F2}";
		TextSelected.text = $"已选:{cardselect}";
		TextBetPercent.text = $"赔率:{multi[cardselect - 1]}";

		float fbasecoin = 0;
		if (Procedure.deskinfo.TexasState == TexasState.TurnProtect) {
			SliderBuy.minValue = 0;
			if (cardselect == 0) {
				SliderBuy.maxValue = 0;
			}
			else {
				if (multi[cardselect - 1] < 4) {
					SliderBuy.maxValue = pot / 4 * 100;
					fbasecoin = pot / potnumber / 4 * 100;
				}
				else {
					SliderBuy.maxValue = pot / multi[cardselect - 1] * 100;
					fbasecoin = pot / potnumber / multi[cardselect - 1] * 100;
				}
			}
		}
		if (Procedure.deskinfo.TexasState == TexasState.RiverProtect) {
			if (cardselect == 0) {
				SliderBuy.minValue = 0;
				SliderBuy.maxValue = 0;
			}
			else {
				SliderBuy.minValue = protect / multi[cardselect - 1] * 100;
				if (multi[cardselect - 1] < 2) {
					SliderBuy.maxValue = pot / 2 * 100;
					fbasecoin = (protect + pot / potnumber) / 2 * 100;
				}
				else {
					SliderBuy.maxValue = pot / multi[cardselect - 1] * 100;
					fbasecoin = (protect + pot / potnumber) / multi[cardselect - 1] * 100;
				}
			}
		}
		float normalizedValue = 0;
		if (SliderBuy.maxValue - SliderBuy.minValue > 0) {
			normalizedValue = (fbasecoin - SliderBuy.minValue) / (SliderBuy.maxValue - SliderBuy.minValue);
		}
		if (normalizedValue > 1) {
			normalizedValue = 1;
		}
		Vector3 newPosition = GoBaseBuy.transform.localPosition;
		newPosition.x = (normalizedValue - 0.5f) * SliderBuy.GetComponent<RectTransform>().sizeDelta.x;
		GoBaseBuy.transform.localPosition = newPosition;
		if (init == true) {
			SliderBuy.value = fbasecoin;
		}
		basecoin = (float)Math.Floor(fbasecoin) / 100;
		mincoin = (float)Math.Ceiling(SliderBuy.minValue) / 100;
		maxcoin = (float)Math.Floor(SliderBuy.maxValue) / 100;
		if (maxcoin > servermax) {
			maxcoin = servermax;
		}
		TextBuyAll.text = $"全买 {maxcoin:F2}";
		TextBuyBase.text = $"保底 {basecoin:F2}";
		TextBuyMin.text = $"最低 {mincoin:F2}";
	}

	public void OnChangedSliderBuy() {
		RefreshInfo();
	}

	public void OnChangedToggleCard(Toggle toggle) {
		int num = toggle.transform.Find("Card").GetComponent<Card>().numD;
		if (toggle.isOn == false) {
			selectlist.Remove(num);
		}
		else {
			if (selectlist.Contains(num) == false) {
				selectlist.Add(num);
			}
		}
		RefreshInfo();
	}
}

using System.Collections.Generic;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;

public class DZAction : MonoBehaviour
{
	public DZGamePanel GamePanel;
	public DZAddCoin DZAddCoin;
	DZProcedure Procedure;
	DeskPlayer selfplayer;

	public Text TextActionCall;
	public GameObject GoFoldButton, GoCallButton, GoAddButton;
	public CountDownFxControl EffectFold, EffectCall;
	public GameObject GoAddPanel;
	public GameObject GoAutoFold; public GameObject GoEffectFold; public Image ImageFold;
	public GameObject GoAutoCall; public GameObject GoEffectCall; public Image ImageCall;
	public List<GameObject> GoAdd;
	public GameObject GoSliderAction;
	public Slider SliderAction;
	public Text TextSliderValue;

	public bool AutoFold;
	public bool AutoCall;

	void OnEnable() {
		transform.localPosition = GamePanel.PanelSeatList.GetPosition(0);
	}

	void OnDisable() {
	}

	void Update() {
		if (EffectFold.totalSeconds != 0) {
			float fp = (EffectFold.remainedSeconds - EffectFold.elapsedTime) / EffectFold.totalSeconds;
			ImageFold.fillAmount = fp;
		}
		if (EffectCall.totalSeconds != 0) {
			float cp = (EffectCall.remainedSeconds - EffectCall.elapsedTime) / EffectCall.totalSeconds;
			ImageCall.fillAmount = cp;
		}
	}

	public void Refresh(bool action = true) {
		Procedure = GF.Procedure.CurrentProcedure as DZProcedure;
		selfplayer = Procedure.GetSelfPlayer();
		if (selfplayer == null) {
			gameObject.SetActive(false);
			return;
		}
		if (selfplayer.InGame == false) {
			gameObject.SetActive(false);
			return;
		}
		if (selfplayer.IsGive) {
			gameObject.SetActive(false);
			return;
		}
		if (new List<TexasState>{TexasState.PreFlop, TexasState.Flop, TexasState.Turn, TexasState.River}.Contains(Procedure.deskinfo.TexasState) == false) {
			gameObject.SetActive(false);
			return;
		}
		if (GamePanel.roundstart == false) {
			gameObject.SetActive(false);
			return;
		}
		gameObject.SetActive(true);
		if (selfplayer.Pos == Procedure.deskinfo.CurrentOption && action == true && AutoCall == false && AutoFold == false) {
			GamePanel.PanelSeatList.SeatList[0].PanelPlayerInfo.GoVip.SetActive(false);
			RefreshAction();
		}
		else {
			GamePanel.PanelSeatList.SeatList[0].PanelPlayerInfo.GoVip.SetActive(selfplayer.BasePlayer.Vip > 0);
			RefreshNoAction();
		}
	}

	void RefreshAction() {
		if (Procedure == null) return;
		List<string> actionlist = new() {
			"无",
			"1/4\n底池",
			"1/3\n底池",
			"1/2\n底池",
			"2/3\n底池",
			"3/4\n底池",
			"1倍\n底池",
			"1.5倍\n底池",
			"All In",
		};
		List<float> multi = new () {
			0,
			0.25f,
			0.33f,
			0.5f,
			0.66f,
			0.75f,
			1,
			1.5f,
			0,
		};
		GoFoldButton.SetActive(true);
		GoCallButton.SetActive(true);
		GoAddButton.SetActive(true);
		GoAddPanel.SetActive(true);

		if (GamePanel.endtime - GamePanel.starttime != 0) {
			if (Procedure.deskinfo.FollowCoin == "0.00" || Procedure.deskinfo.FollowCoin == "") {
				EffectCall.gameObject.SetActive(true);
				ImageCall.gameObject.SetActive(true);
				EffectFold.gameObject.SetActive(false);
				ImageFold.gameObject.SetActive(false);
			}
			else {
				EffectFold.gameObject.SetActive(true);
				ImageFold.gameObject.SetActive(true);
				EffectCall.gameObject.SetActive(false);
				ImageCall.gameObject.SetActive(false);
			}
		}
		else {
			EffectFold.gameObject.SetActive(false);
			ImageFold.gameObject.SetActive(false);
			EffectCall.gameObject.SetActive(false);
			ImageCall.gameObject.SetActive(false);
		}

		GoAutoFold.SetActive(false);
		GoAutoCall.SetActive(false);

		if (Procedure.deskinfo.FollowCoin == "" || Procedure.deskinfo.FollowCoin == "0.00") {
			TextActionCall.text = "看牌";
		}
		else {
			if (float.Parse(Procedure.deskinfo.FollowCoin) >= float.Parse(selfplayer.Coin)) {
				TextActionCall.text = $"All In\n{selfplayer.Coin}";
			}
			else {
				TextActionCall.text = $"跟注\n{Procedure.deskinfo.FollowCoin}";
			}
		}
		GoSliderAction.SetActive(false);
		if (Procedure.deskinfo.TexasState == TexasState.PreFlop && Procedure.deskinfo.SomeCallCoin == false) {
			float baseCoinValue1 = Procedure.deskinfo.BaseConfig.BaseCoin * 2;
			float baseCoinValue2 = Procedure.deskinfo.BaseConfig.BaseCoin * 4;
			float baseCoinValue3 = Procedure.deskinfo.BaseConfig.BaseCoin * 6;
			float followCoinValue = float.Parse(Procedure.deskinfo.FollowCoin);
			float selfCoinValue = float.Parse(selfplayer.Coin);

			GoAdd[0].SetActive(false);
			GoAdd[1].SetActive(true);
			GoAdd[1].transform.Find("Value").GetComponent<Text>().text = "1BB";
			GoAdd[1].transform.Find("Chip").GetComponent<Text>().text = selfCoinValue <= (followCoinValue + baseCoinValue1) ? "All" : Util.Sum2String(followCoinValue, baseCoinValue1);
			GoAdd[2].SetActive(true);
			GoAdd[2].transform.Find("Value").GetComponent<Text>().text = "2BB";
			GoAdd[2].transform.Find("Chip").GetComponent<Text>().text = selfCoinValue <= (followCoinValue + baseCoinValue2) ? "All" : Util.Sum2String(followCoinValue, baseCoinValue2);
			GoAdd[3].SetActive(true); 
			GoAdd[3].transform.Find("Value").GetComponent<Text>().text = "3BB"; 
			GoAdd[3].transform.Find("Chip").GetComponent<Text>().text = selfCoinValue <= (followCoinValue + baseCoinValue3) ? "All" : Util.Sum2String(followCoinValue, baseCoinValue3);
			GoAdd[4].SetActive(false);
		}
		else {
			if (Procedure.deskinfo.Option.Count != 0) {
				for (var i = 0;i < 5;i++) {
					if (GamePanel.AddCoinSelect[i] == 0) {
						GoAdd[i].SetActive(false); continue;
					}
					GoAdd[i].SetActive(true);
					GoAdd[i].transform.Find("Value").GetComponent<Text>().text = actionlist[GamePanel.AddCoinSelect[i]];
					if (GamePanel.AddCoinSelect[i] == 8) {
						GoAdd[i].transform.Find("Chip").GetComponent<Text>().text = selfplayer.Coin;
					}
					else {
						if (float.Parse(selfplayer.Coin) < Util.Sum2Float(Procedure.deskinfo.FollowCoin, float.Parse(Procedure.deskinfo.Option[0]) * multi[GamePanel.AddCoinSelect[i]]))
						{
							GoAdd[i].transform.Find("Chip").GetComponent<Text>().text = "All";
						}else{
							GoAdd[i].transform.Find("Chip").GetComponent<Text>().text = Util.Sum2String(Procedure.deskinfo.FollowCoin, float.Parse(Procedure.deskinfo.Option[0]) * multi[GamePanel.AddCoinSelect[i]]);
						}
					}
				}
			}
		}
		int basecoin = (int)Util.Sum2Float(Procedure.deskinfo.BaseConfig.BaseCoin * 200);
		int callcoin = (int)Util.Sum2Float(Util.Sum2Float(Procedure.deskinfo.CallCoin) * 100);
		int selfcoin = (int)Util.Sum2Float(Util.Sum2Float(selfplayer.Coin) * 100);

		SliderAction.minValue = 0;
		SliderAction.maxValue = (int)((selfcoin - callcoin) / basecoin);
		if (selfcoin - callcoin > SliderAction.maxValue * basecoin) {
			SliderAction.maxValue += 1;
		}
		SliderAction.value = SliderAction.minValue;
		if (SliderAction.value == SliderAction.maxValue) {
			TextSliderValue.text = "All In";
		}
		else {
			TextSliderValue.text = Util.Sum2String(callcoin, SliderAction.value * Procedure.deskinfo.BaseConfig.BaseCoin * 2);
		}
	}

	void RefreshNoAction() {
		GoFoldButton.SetActive(false);
		GoCallButton.SetActive(false);
		GoAddButton.SetActive(false);
		GoAddPanel.SetActive(false);

		GoAutoFold.SetActive(true);
		GoAutoCall.SetActive(true);
		if (AutoFold) {
			GoEffectFold.SetActive(true);
		}
		else {
			GoEffectFold.SetActive(false);
		}
		if (AutoCall) {
			GoEffectCall.SetActive(true);
		}
		else {
			GoEffectCall.SetActive(false);
		}
	}

	public void Reset() {
		GoSliderAction.SetActive(false);
	}

	public void Clear() {
		AutoFold = AutoCall = false;
	}

	public void OnSliderChangeValue() {
		int basecoin = (int)Util.Sum2Float(Procedure.deskinfo.BaseConfig.BaseCoin * 200);
		int callcoin = (int)Util.Sum2Float(Util.Sum2Float(Procedure.deskinfo.CallCoin) * 100);

		if (SliderAction.value == SliderAction.maxValue) {
			TextSliderValue.text = "All In";
		}
		else {
			TextSliderValue.text = Util.Sum2String((SliderAction.value * basecoin + callcoin) / 100);
		}
	}

	public void OnAddPress() {
		GoSliderAction.SetActive(true);
	}

	public void OnAddRelease() {
		float value = SliderAction.value;
		if (value < SliderAction.minValue) {
			SliderAction.value = SliderAction.minValue;
		}
		if (value > SliderAction.maxValue) {
			SliderAction.value = SliderAction.maxValue;
		}

		int basecoin = (int)Util.Sum2Float(Procedure.deskinfo.BaseConfig.BaseCoin * 200);
		int callcoin = (int)Util.Sum2Float(Util.Sum2Float(Procedure.deskinfo.CallCoin) * 100);
		int selfcoin = (int)Util.Sum2Float(Util.Sum2Float(selfplayer.Coin) * 100);

		int betcoin = callcoin + (int)SliderAction.value * basecoin;
		if (betcoin > selfcoin) {
			betcoin = selfcoin;
		}
		Procedure.Send_AddCoin(Util.Sum2Float(betcoin) / 100);
		GoSliderAction.SetActive(false);
	}

	public void OnAddCancel() {
		GoSliderAction.SetActive(false);
	}

	public void OnAddHold(Vector3 position) {
		RectTransform rectTransform = SliderAction.GetComponent<RectTransform>();
		Vector3[] worldCorners = new Vector3[4];
		rectTransform.GetWorldCorners(worldCorners);
		float minY = GF.UICamera.WorldToScreenPoint(worldCorners[0]).y;
		float maxY = GF.UICamera.WorldToScreenPoint(worldCorners[1]).y;
		float clampedY = Mathf.Clamp(position.y, minY, maxY);
		float normalizedValue = (clampedY - minY) / (maxY - minY);
		SliderAction.value = normalizedValue * (SliderAction.maxValue - SliderAction.minValue) + SliderAction.minValue;
	}

	public void OnButtonClickAutoFold() {
		AutoFold = !AutoFold;
		AutoCall = false;
		Refresh();
	}

	public void OnButtonClickAutoCall() {
		AutoCall = !AutoCall;
		AutoFold = false;
		Refresh();
	}
}

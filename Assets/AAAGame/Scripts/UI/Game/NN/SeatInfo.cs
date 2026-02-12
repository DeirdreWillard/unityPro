﻿using System.Collections.Generic;
using DG.Tweening;
using Google.Protobuf;
using NetMsg;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using static UtilityBuiltin;

public class SeatInfo : MonoBehaviour
{
	public int seatpos;

	public GameObject objectEmpty;
	public GameObject objectPlayer;

	public Text textCoin;

	public AvatarInfo goAvatarInfo;

	public RectTransform rectCard;
	public List<Card> cardlist;
	public List<GameObject> cardborder;

	public Image imageBobRanker;

	public GameObject goBanker;

	public GameObject goChip;

	public Text textChip;
	public Image imageNumber;

	public GameObject goNiuType;
	public Image imageNiuType;
	public GameObject goNiuEffect1;
	public GameObject goNiuEffect2;
    public SkeletonAnimation WinnerStarEff;

	public GameObject goLeave;
	public GameObject goLeaveText;
	public Text textLeave;
	public GameObject imageProfit;
	public GameObject imageProfit1;
	public GameObject imageProfit2;
	public Text textProfit;

	public List<int> cardnumber = new();
	public List<Vector3> origincardpos = new();
	public Vector2 vecCardCenter;
	public Vector2 vecBankerCenter;
	public Vector3 originbankerPos;
	public Vector3 originChipPos;
	public Vector3 originChipTextPos;
	public Vector3 statePos;
	public Vector3 numImagePos;
	public Vector3 operateTypePos;

	public GameObject goVoice;

	void Awake() {
		for (var i = 0;i < 5;i++) {
			origincardpos.Add(cardlist[i].transform.localPosition);
		}
		originbankerPos = goBanker.transform.localPosition;
		originChipPos = goChip.transform.localPosition;
		originChipTextPos = textChip.transform.localPosition;
		statePos = goNiuType.transform.localPosition;
		numImagePos = imageNumber.transform.localPosition;
		operateTypePos = imageBobRanker.transform.localPosition;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(rectCard.GetComponent<RectTransform>(), new Vector2(Screen.width / 2, Screen.height / 2), GF.RootCanvas.worldCamera, out vecCardCenter);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(goBanker.transform.parent.GetComponent<RectTransform>(), new Vector2(Screen.width / 2, Screen.height / 2), GF.RootCanvas.worldCamera, out vecBankerCenter);
	}

	public void EnterPlayer(DeskPlayer deskplayer) {
		// GF.LogError("Reset" + deskplayer.ToString());
		Reset();
		objectEmpty.SetActive(false);
		objectPlayer.SetActive(true);
		textCoin.text = Util.Sum2String(deskplayer.Coin);
		goAvatarInfo.Init(deskplayer.BasePlayer);
		SetLeaveState(deskplayer.State);
		textLeave.text = "";
	}

	public void LeavePlayer() {
		Reset();
		objectEmpty.SetActive(true);
		objectPlayer.SetActive(false);
	}

	public void SetLeaveState(PlayerState playerState){

		goLeaveText.SetActive(playerState != PlayerState.OnLine);
		switch (playerState)
		{
			case PlayerState.OnLine:
				goLeaveText.GetComponent<Text>().text = "";
				break;
			case PlayerState.OffLine:
				goLeaveText.GetComponent<Text>().text = "离桌留座";
				break;
			case PlayerState.WaitBring:
				goLeaveText.GetComponent<Text>().text = "等待带入";
				break;
		}
	}

	public void Reset() {
		HideWinnerStarEff();
		imageBobRanker.gameObject.SetActive(false);
		goBanker.SetActive(false);
		ShowImageNumber();
		goChip.SetActive(false);
		imageProfit.SetActive(false);
		goNiuType.SetActive(false);
		goNiuEffect1.SetActive(false);
		goNiuEffect2.SetActive(false);
		cardnumber.Clear();
		for (int i = 0;i < cardlist.Count;i++) {
			var card = cardlist[i];
			card.Init(-1);
			if (origincardpos.Count > i) {
				card.transform.localPosition = origincardpos[i];
			}
			card.gameObject.SetActive(false);
		}
		foreach (var card in cardborder) {
			card.SetActive(false);
		}
	}

	public void RobBanker(int robBanker) {
		if (robBanker == -1) {
			imageBobRanker.gameObject.SetActive(false);
			return;
		}
		_ = GF.Sound.PlayEffect($"niuniu/rate{(robBanker > 3 ? 3 : robBanker)}.mp3");
		string typepath = "Common/game_qiang" + robBanker + ".png"; ;
		string s = AssetsPath.GetSpritesPath(typepath);
		GF.UI.LoadSprite(s, (sprite) => {
			if (imageBobRanker == null || imageBobRanker.gameObject.activeSelf == false) return;
			imageBobRanker.sprite = sprite;
			imageBobRanker.SetNativeSize();
			imageBobRanker.transform.localPosition = operateTypePos;
			imageBobRanker.transform.localScale = Vector3.one * 0.7f;
			imageBobRanker.transform.DOScale(Vector3.one * 1.4f, 0.1f);
		});
		if (imageBobRanker.gameObject.activeSelf == false)
		{
			imageBobRanker.gameObject.SetActive(true);
		}
	}

	public void ShowCard(List<int> cards, bool show = true) {
		cardnumber.Clear();
		for (int i = 0; i < cards.Count; i++) {
			cardnumber.Add(cards[i]);
			cardlist[i].Init(cards[i]);
			if (show == true){
				if (origincardpos.Count > i) {
					cardlist[i].transform.localPosition = origincardpos[i];
				}
				cardlist[i].gameObject.SetActive(true);
			}
			cardborder[i].SetActive(false);
		}
	}

	public void ShowNiu(int Niu, List<int> compose) {
		GF.UI.LoadSprite(AssetsPath.GetSpritesPath("NN/CardType/" + ShowType(Niu) + ".png"), (sprite) => {
			imageNiuType.sprite = sprite;
			imageNiuType.SetNativeSize();
			goNiuType.transform.localPosition = statePos;
			goNiuType.SetActive(true);
		});
		Dictionary<int, int> shown = new();
		for (int i = 0;i < compose.Count;i++) {
			for (int j = 0;j < 5;j++) {
				if (cardnumber[j] == compose[i] && !shown.ContainsKey(j)) {
					shown[j] = 1;
					cardborder[j].SetActive(true);
					cardlist[j].transform.localPosition -= new Vector3(0, -20, 0);
					if (Niu >= 7) {
						goNiuEffect1.SetActive(true);
						goNiuEffect2.SetActive(true);
					}
					break;
				}
			}
		}
	}

	private string ShowType(int n)
	{
		switch (n) {
			case 0:
				return "paixing_niu0";
			case 1:
				return "paixing_niu1";
			case 2:
				return "paixing_niu2";
			case 3:
				return "paixing_niu3";
			case 4:
				return "paixing_niu4";
			case 5:
				return "paixing_niu5";
			case 6:
				return "paixing_niu6";
			case 7:
				return "paixing_niu7";
			case 8:
				return "paixing_niu8";
			case 9:
				return "paixing_niu9";
			case 10:
				return "paixing_niuniu";
			case 11:
				return "paixing_niushunzi";
			case 12:
				return "paixing_niuwuhua";
			case 13:
				return "paixing_niutonghua";
			case 14:
				return "paixing_niuhulu";
			case 15:
				return "paixing_niuzhadan";
			case 16:
				return "paixing_niuwuxiao";
			case 17:
				return "paixing_niutonghuashun";
			default:
				break;
		}
		return "";
	}

	public void ShowImageNumber(int num = 0) {
		// GF.LogError("ShowImageNumber" + num);
		imageBobRanker.gameObject.SetActive(false);
		if (num == 0) {
			if (imageNumber.gameObject.activeSelf)
				imageNumber.gameObject.SetActive(false);
			return;
		}
		string s = AssetsPath.GetSpritesPath("NN/" + num + "倍.png");
		GF.UI.LoadSprite(s, (sprite) => {
			imageNumber.sprite = sprite;
			imageNumber.SetNativeSize();
			imageNumber.transform.localPosition = numImagePos;
			imageNumber.gameObject.SetActive(true);
		});
	}

	public async void OpenPlayerInforPanel()
	{
		NNProcedure nnProcedure = GF.Procedure.CurrentProcedure as NNProcedure;
		DeskPlayer deskplayer = nnProcedure.GetPosPlayer(seatpos);
		var uiParams = UIParams.Create();
		uiParams.Set<VarByteArray>("deskPlayer", deskplayer.ToByteArray());
		uiParams.Set<VarBoolean>("isRoomCreate", nnProcedure.IsRoomOwner());
		await GF.UI.OpenUIFormAwait(UIViews.playerInforPanel,uiParams);
	}

	private float lastUpdateTime;

	void Update()
	{
		if (Time.time - lastUpdateTime >= 1f)
		{
			NNProcedure nnProcedure = GF.Procedure.CurrentProcedure as NNProcedure;
			if (nnProcedure.enterNiuNiuDeskRs.DeskState == DeskState.StartRun)
			{
				DeskPlayer deskplayer = nnProcedure.GetPosPlayer(seatpos);
				if (deskplayer != null){
					deskplayer.LeaveTime--;
					if (deskplayer.LeaveTime <= 0)
					{
						textLeave.text = "0";
					}
					else
					{
						textLeave.text = deskplayer.LeaveTime.ToString();
					}
				}
			}
			lastUpdateTime = Time.time;
		}
	}

	public void ShowWinnerStarEff()
	{
		if (WinnerStarEff.gameObject.activeSelf == false) WinnerStarEff.gameObject.SetActive(true);
		WinnerStarEff.AnimationState.SetAnimation(0, "animation", false);
	}

	public void HideWinnerStarEff()
	{
		WinnerStarEff.gameObject.SetActive(false);
	}
}

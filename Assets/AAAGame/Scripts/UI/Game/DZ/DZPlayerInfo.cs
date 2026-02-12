using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using NetMsg;
using System.Linq;
using Spine.Unity;

public class DZPlayerInfo : MonoBehaviour
{
	DZProcedure Procedure;
	public DZGamePanel GamePanel;

	public AvatarInfo GoAvatarInfo;

	public Text TextName;

	public Text TextScore;
	public Card Card1;
	public Card Card2;
	public GameObject GoHideCards;
	public GameObject GoPanelOperation;
	public List<GameObject> GoOperation;
	public GameObject GoChip;
	public GameObject GoChipImage1;
	public GameObject GoChipImage2;
	public GameObject GoChipImage3;
	public GameObject GoChipImage4;
	public Text TextChip;
	public GameObject GoFlyChip;
	public Text TextFlyChip;
	public GameObject GoEffect;
	public GameObject GoProfit;
	public GameObject GoPrifitImage1, GoPrifitImage2;
    public SkeletonAnimation WinnerStarEff;
	public Text TextProfit;
	public GameObject GoLeave;
	public Text TextCountdown;
	public GameObject GoWin;
	public GameObject GoVip;
	public RawImage ImageAvatar;
	public GameObject GoDealer;
	public GameObject GoTimer;
	public CountDownFxControl EffectTimer;
	public Text TextCardType;
	public GameObject GoVoice;

	public long PlayerID;

	//牌初始位置
	public Vector3 PositionCard1, PositionCard2;

	public bool IsBanker;
	public bool IsBB;
	public bool IsSB;
	public bool IsStrongBlind;
	public bool IsWin;
	public bool IsAllIn;
	public float? leavetime;

	public TexasCardType? CardType = null;

	public float? Profit = null;

	void Awake()
	{
		PositionCard1 = new Vector2(-30, 0); PositionCard2 = new Vector2(30, 0);
	}

	void OnEnable()
	{
		Procedure = GF.Procedure.CurrentProcedure as DZProcedure;
		Profit = null;
	}

	private float lastUpdateTime;

	void Update()
	{
		if (leavetime > 0)
		{
			if (Time.time - lastUpdateTime >= 1f)
			{
				leavetime -= 1f;
				DeskPlayer deskplayer = Procedure.GetPlayerByID(PlayerID);
				if (deskplayer != null)
					deskplayer.LeaveTime = (int)leavetime;
				TextCountdown.text = ((int)leavetime).ToString();
				lastUpdateTime = Time.time;
			}
		}
	}

	public void InitPlayerInfo(DeskPlayer deskplayer)
	{
		PlayerID = deskplayer.BasePlayer.PlayerId;
		GoAvatarInfo.Init(deskplayer.BasePlayer);
		TextScore.text = Util.Sum2String(deskplayer.Coin);
		SetLeaveState(deskplayer);
		GoVip.SetActive(deskplayer.BasePlayer.Vip > 0);
	}

	public void SetLeaveState(DeskPlayer deskplayer)
	{
		if (deskplayer.LeaveTime == 0)
		{
			leavetime = null;
			GoLeave.GetComponent<Text>().text = "";
			TextCountdown.text = "";
		}
		else
		{
			leavetime = deskplayer.LeaveTime;
			switch (deskplayer.State)
			{
				case PlayerState.OnLine:
					GoLeave.GetComponent<Text>().text = "";
					TextCountdown.text = "";
					break;
				case PlayerState.OffLine:
					GoLeave.GetComponent<Text>().text = "离桌留座";
					TextCountdown.text = ((int)leavetime).ToString();
					break;
				case PlayerState.WaitBring:
					GoLeave.GetComponent<Text>().text = "等待带入";
					TextCountdown.text = ((int)leavetime).ToString();
					break;
			}
		}
	}

	public void ShowAction(TexasOption? option = null, string param = "")
	{
		Procedure = GF.Procedure.CurrentProcedure as DZProcedure;
		DeskPlayer deskplayer = Procedure.GetPlayerByID(PlayerID);
		if (deskplayer == null) return;
		foreach (GameObject go in GoOperation)
		{
			go.SetActive(false);
		}
		if (GamePanel.options.ContainsKey(PlayerID) == true)
		{
			if (option == null)
			{
				option = GamePanel.options[PlayerID];
				param = deskplayer.BetCoin;
			}
			switch (option)
			{
				case TexasOption.Allin:
					GoOperation[0].SetActive(true);
					break;
				case TexasOption.Follow:
					// if (param == "" || float.Parse(param) == 0) {
					// GoOperation[2].SetActive(true);
					// }
					// else {
					GoOperation[1].SetActive(true);
					// }
					break;
				case TexasOption.Look:
					GoOperation[2].SetActive(true);
					break;
				case TexasOption.AddCoin:
					GoOperation[3].SetActive(true);
					break;
				case TexasOption.Give:
					GoOperation[4].SetActive(true);
					break;
			}
		}
	}

	public void Refresh(DeskPlayer deskplayer)
	{
		// GF.LogInfo("刷新德州玩家信息", "deskplayer:" + deskplayer.ToString());
		Procedure = GF.Procedure.CurrentProcedure as DZProcedure;
		if (Procedure == null || GamePanel == null || GamePanel.PanelSeatList == null) return;
		Procedure = GF.Procedure.CurrentProcedure as DZProcedure;
		GoDealer.transform.localPosition = GamePanel.PanelSeatList.GetDealerPosition(deskplayer.Pos);
		GoPanelOperation.transform.localPosition = GamePanel.PanelSeatList.GetOperatorPosition(deskplayer.Pos);
		GoChip.transform.localPosition = GamePanel.PanelSeatList.GetChipPosition(deskplayer.Pos);
		GoWin.transform.localPosition = GamePanel.PanelSeatList.GetWinPosition(deskplayer.Pos);
		TextScore.text = Util.Sum2String(deskplayer.Coin);
		SetLeaveState(deskplayer);
		if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId))
		{
			GoHideCards.SetActive(false);
			Card1.gameObject.SetActive(false);
			Card2.gameObject.SetActive(false);
		}
		else
		{
			switch (Procedure.deskinfo.TexasState)
			{
				case TexasState.PreFlop:
				case TexasState.Flop:
				case TexasState.Turn:
				case TexasState.River:
					GoHideCards.SetActive(false);
					if (deskplayer.IsGive)
					{
						GoHideCards.SetActive(false);
						Card1.gameObject.SetActive(false);
						Card2.gameObject.SetActive(false);
					}
					else
					{
						if (deskplayer.HandCard.ToList().Count == 2)
						{
							if (deskplayer.HandCard[0] == -1 && deskplayer.HandCard[1] == -1)
							{
								GoHideCards.SetActive(true);
								Card1.gameObject.SetActive(false);
								Card2.gameObject.SetActive(false);
							}
							else
							{
								GoHideCards.SetActive(false);
								Card1.Init(deskplayer.HandCard[0]);
								Card2.Init(deskplayer.HandCard[1]);
								Card1.gameObject.SetActive(true);
								Card2.gameObject.SetActive(true);
							}
						}
					}
					break;
				case TexasState.TurnProtect:
				case TexasState.RiverProtect:
					GoHideCards.SetActive(false);
					if (deskplayer.IsGive)
					{
						GoHideCards.SetActive(false);
						Card1.gameObject.SetActive(false);
						Card2.gameObject.SetActive(false);
					}
					else
					{
						if (deskplayer.HandCard.ToList().Count == 2)
						{
							Card1.Init(deskplayer.HandCard[0]);
							Card2.Init(deskplayer.HandCard[1]);
							Card1.gameObject.SetActive(true);
							Card2.gameObject.SetActive(true);
						}
						else
						{
							Card1.gameObject.SetActive(false);
							Card2.gameObject.SetActive(false);
						}
					}
					break;
				case TexasState.TexasSettle:
					if (deskplayer.IsGive)
					{
						if (deskplayer.HandCard.Count == 0)
						{
							GoHideCards.SetActive(false);
							Card1.gameObject.SetActive(false);
							Card2.gameObject.SetActive(false);
						}
						else if (deskplayer.HandCard[0] == -1 && deskplayer.HandCard[1] == -1)
						{
							GoHideCards.SetActive(false);
							Card1.gameObject.SetActive(false);
							Card2.gameObject.SetActive(false);
						}
						else
						{
							Card1.Init(deskplayer.HandCard[0]);
							Card2.Init(deskplayer.HandCard[1]);
							Card1.gameObject.SetActive(true);
							Card2.gameObject.SetActive(true);
						}
					}
					else if (deskplayer.HandCard.ToList().Count == 2)
					{
						if (deskplayer.HandCard[0] == -1 && deskplayer.HandCard[1] == -1)
						{
							GoHideCards.SetActive(false);
							Card1.gameObject.SetActive(false);
							Card2.gameObject.SetActive(false);
						}
						else
						{
							Card1.Init(deskplayer.HandCard[0]);
							Card2.Init(deskplayer.HandCard[1]);
							Card1.gameObject.SetActive(true);
							Card2.gameObject.SetActive(true);
						}
					}
					break;
			}
		}
		if (IsBanker)
		{
			GoDealer.SetActive(true);
		}
		else
		{
			GoDealer.SetActive(false);
		}
		if (deskplayer.BetCoin == "" || deskplayer.BetCoin == "0.00" || deskplayer.BetCoin == "0")
		{
			GoChip.SetActive(false);
		}
		else
		{
			GoChip.SetActive(true);
			if (Procedure.deskinfo.TexasState == TexasState.PreFlop)
			{
				GoChipImage2.SetActive(false);
				GoChipImage3.SetActive(false);
				GoChipImage4.SetActive(false);
				if (IsSB)
				{
					GoChipImage2.SetActive(true);
				}
				if (IsBB)
				{
					GoChipImage3.SetActive(true);
				}
				if (IsStrongBlind)
				{
					GoChipImage4.SetActive(true);
				}
			}
			else
			{
				GoChipImage2.SetActive(false);
				GoChipImage3.SetActive(false);
				GoChipImage4.SetActive(false);
			}
			TextChip.text = Util.Sum2String(deskplayer.BetCoin);
		}
		if (deskplayer.Pos == Procedure.deskinfo.CurrentOption && Util.IsMySelf(deskplayer.BasePlayer.PlayerId) == false && new[] { TexasState.PreFlop, TexasState.Flop, TexasState.Turn, TexasState.River }.Contains(Procedure.deskinfo.TexasState) == true && IsAllIn == false && deskplayer.IsGive == false)
		{
			GoTimer.SetActive(true);
		}
		else
		{
			GoTimer.SetActive(false);
		}
		ShowAction();
		if (deskplayer.HandCard.ToList().Count != 2 || deskplayer.HandCard[0] == -1 || deskplayer.HandCard[1] == -1 && deskplayer.IsGive == false)
		{
			CardType = null;
			TextCardType.gameObject.SetActive(false);
		}
		else
		{
			DZProcedure.TexasCardInfo cardinfo = Procedure.GetCardType(deskplayer.HandCard.Concat(Procedure.deskinfo.CommonCards).ToList());
			TextCardType.text = Procedure.CardType2String(cardinfo.cardtype);
			if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId) == true)
			{
				if (cardinfo.realcard.Contains(deskplayer.HandCard[0]))
				{
					GamePanel.Card1.GetComponent<Image>().color = Color.white;
				}
				else
				{
					GamePanel.Card1.GetComponent<Image>().color = Color.gray;
				}
				if (cardinfo.realcard.Contains(deskplayer.HandCard[1]))
				{
					GamePanel.Card2.GetComponent<Image>().color = Color.white;
				}
				else
				{
					GamePanel.Card2.GetComponent<Image>().color = Color.gray;
				}
				for (var i = 0; i < Procedure.deskinfo.CommonCards.Count; i++)
				{
					if (cardinfo.realcard.Contains(Procedure.deskinfo.CommonCards[i]))
					{
						GamePanel.CommonCard[i].GetComponent<Image>().color = Color.white;
					}
					else
					{
						GamePanel.CommonCard[i].GetComponent<Image>().color = Color.gray;
					}
				}
			}
			if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId) == true)
			{
				TextCardType.gameObject.SetActive(true);
			}
		}
		if (Profit == null)
		{
			GoProfit.SetActive(false);
		}
		else if (Profit > 0)
		{
			GoProfit.SetActive(true);
			float profit = Util.Sum2Float(Profit.Value);
			if (profit > 0)
			{
				TextProfit.text = "+" + Util.Sum2String(Profit.Value);
			}
			else
			{
				TextProfit.text = Util.Sum2String(Profit.Value);
			}
			ShowWinnerStarEff();
			GoPrifitImage1.SetActive(true);
			GoPrifitImage2.SetActive(false);
		}
		if (Procedure.deskinfo.TexasState == TexasState.TexasSettle)
		{
			GoEffect.SetActive(false);
		}
		else
		{
			// GF.LogError("Procedure.deskinfo" + deskplayer.BasePlayer.Nick + "IsAllIn" + IsAllIn);
			GoEffect.SetActive(IsAllIn || (Procedure.deskinfo.Config.Critcal == true && Procedure.deskinfo.Crit == true && deskplayer.InGame == true));
		}
		if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId) == false)
		{
			GoWin.SetActive(false);
		}
		else
		{
			GoWin.SetActive(IsWin);
		}
		if (deskplayer.IsGive == true)
		{
			GoHideCards.SetActive(false);
			TextName.color = Color.gray;
			ImageAvatar.color = Color.gray;
			TextScore.color = Color.gray;
		}
		else
		{
			TextName.color = Color.white;
			ImageAvatar.color = Color.white;
			TextScore.color = Color.white;
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

	public void Reset()
	{
		HideWinnerStarEff();
		GoHideCards.SetActive(false);
		Card1.transform.localPosition = PositionCard1;
		Card1.gameObject.SetActive(false);
		Card2.transform.localPosition = PositionCard2;
		Card2.gameObject.SetActive(false);
		GoDealer.SetActive(false);
		GoChip.SetActive(false);
		GoFlyChip.SetActive(false);
		GoTimer.SetActive(false);
		foreach (GameObject go in GoOperation)
		{
			go.SetActive(false);
		}
		TextCardType.gameObject.SetActive(false);
		GoEffect.SetActive(false);
		GoWin.SetActive(false);
		GoProfit.SetActive(false);
	}

	public void Clear()
	{
		PlayerID = 0;
		HideWinnerStarEff();
		GoHideCards.SetActive(false);
		Card1.transform.localPosition = PositionCard1;
		Card1.gameObject.SetActive(false);
		Card2.transform.localPosition = PositionCard2;
		Card2.gameObject.SetActive(false);
		GoDealer.SetActive(false);
		GoChip.SetActive(false);
		GoFlyChip.SetActive(false);
		GoTimer.SetActive(false);
		IsBanker = IsBB = IsSB = IsStrongBlind = false;
		IsWin = false; IsAllIn = false;
		foreach (GameObject go in GoOperation)
		{
			go.SetActive(false);
		}
		CardType = null;
		TextCardType.gameObject.SetActive(false);
		// Profit = null;
		leavetime = null;
		GoEffect.SetActive(false);
		GoWin.SetActive(false);
		GoProfit.SetActive(false);
		TextCountdown.text = "";
	}
}

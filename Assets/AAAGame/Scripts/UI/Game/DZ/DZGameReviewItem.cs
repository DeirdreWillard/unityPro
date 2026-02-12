using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using System.Collections.Generic;
using System.Linq;

public class DZGameReviewItem : MonoBehaviour
{
	public DZGamePanel GamePanel;
	public AvatarInfo GoAvatarInfo;
	public GameObject GoSelf;
	public Text TextCardType;
	public Image ImageBlind1, ImageBlind2, ImageBanker;
	public Card HandCard1, HandCard2;
	public List<Card> CommonCard;
	public List<Text> TextAction;
	public Text TextInsuranceBuyText;
	public Text TextInsuranceBuy1;
	public Text TextInsuranceBuy2;
	public Text TextInsurancePay;
	public Text TextProfit;
	public Text TextJP;

	public Toggle ToggleLook;

	int curPage = 0;
	long playerid = 0;

	public void Init(int _curPage, List<int> CommonCards, Msg_TexasHistory history)
	{
		curPage = _curPage;
		playerid = history.Player.PlayerId;
		DZProcedure Procedure = GF.Procedure.CurrentProcedure as DZProcedure;
		GoAvatarInfo.Init(history.Player.Nick, history.Player.HeadImage, history.Player.Vip);
		GoSelf.SetActive(Util.IsMySelf(history.Player.PlayerId));
		DZProcedure.TexasCardInfo cardinfo = Procedure.GetCardType(history.HandCards.Concat(CommonCards).ToList());
		HandCard1.Init(history.HandCards[0]);
		if (cardinfo.realcard.Contains(history.HandCards[0]) && history.HandCards[0] != -1) {
			HandCard1.GetComponent<Image>().color = Color.white;
		}
		else {
			HandCard1.GetComponent<Image>().color = Color.gray;
		}
		HandCard2.Init(history.HandCards[1]);
		if (cardinfo.realcard.Contains(history.HandCards[1]) && history.HandCards[1] != -1) {
			HandCard2.GetComponent<Image>().color = Color.white;
		}
		else {
			HandCard2.GetComponent<Image>().color = Color.gray;
		}
		for (int i = 0;i < 5;i++) {
			if (CommonCards.Count > i) {
				CommonCard[i].Init(CommonCards[i]);
				CommonCard[i].gameObject.SetActive(true);
				if (cardinfo.realcard.Contains(CommonCards[i])) {
					CommonCard[i].GetComponent<Image>().color = Color.white;
				}
				else {
					CommonCard[i].GetComponent<Image>().color = Color.gray;
				}
			}
			else {
				CommonCard[i].gameObject.SetActive(false);
			}
		}
		TextCardType.gameObject.SetActive(history.HandCards[0] > 0);
		TextCardType.text = Procedure.CardType2String(history.CardType);
		ImageBlind1.gameObject.SetActive(false); ImageBlind2.gameObject.SetActive(false); ImageBanker.gameObject.SetActive(false);
		List<int> mark = history.Mark.ToList();
		if (mark.Contains(1)) {
			ImageBlind1.gameObject.SetActive(true);
		}
		else if (mark.Contains(2)) {
			ImageBlind2.gameObject.SetActive(true);
		}
		else if (mark.Contains(4)) {
			ImageBanker.gameObject.SetActive(true);
		}
		TextProfit.FormatRichText(history.Profit);
		List<Msg_TexasStateOption> options = history.StateOption.ToList();
		// float? insurance1 = null, insurance2 = null;
		for (var i = 0;i < 4;i++) {
			TextAction[i].gameObject.SetActive(false);
		}
		int showindex = -1;
		for (var i = 0;i < options.Count;i++) {
			showindex++;
			TexasOption? option = null;
			float addcoin = 0f;
			if (options[i].Option.Count == 0) {
				showindex--;
			}
			else {
				bool insurance = false;
				foreach (Msg_OptionParam param in options[i].Option) {
					switch (param.Option) {
						case TexasOption.Follow:
							option = TexasOption.Follow;
							addcoin = Util.Sum2Float(addcoin, param.Param);
							break;
						case TexasOption.Blind:
							option = TexasOption.Blind;
							addcoin = Util.Sum2Float(addcoin, param.Param);
							break;
						case TexasOption.Allin:
							option = TexasOption.Allin;
							addcoin = Util.Sum2Float(addcoin, param.Param);
							break;
						case TexasOption.Give:
							option = TexasOption.Give;
							addcoin = Util.Sum2Float(addcoin, param.Param);
							break;
						case TexasOption.AddCoin:
							option = TexasOption.AddCoin;
							addcoin = Util.Sum2Float(addcoin, param.Param);
							break;
						case TexasOption.ProtectOne:
							insurance = true;
							// insurance1 ??= 0;
							// insurance1 = Util.Sum2Float(insurance1, param.Param);
							break;
						case TexasOption.ProtectTwo:
							insurance = true;
							// insurance2 ??= 0;
							// insurance2 = Util.Sum2Float(insurance2, param.Param);
							break;
					}
				}
				if (insurance) {
					showindex--;
				}
			}
			if (option != null) {
				string optiontext = option switch {
					TexasOption.Allin => "All In",
					TexasOption.Blind => "盲注",
					TexasOption.AddCoin => "加注",
					TexasOption.Give => "盖牌",
					TexasOption.Follow => "跟注",
					_ => "",
				};
				TextAction[showindex].text = $"{optiontext}:{Util.FormatAmount(addcoin)}";
				TextAction[showindex].gameObject.SetActive(true);
			}
		}
		string TextInsuranceBuyStr = "";
		if(float.TryParse(history.TurnProtect, out float protect) && protect != 0){
			TextInsuranceBuyStr += $"投保:{protect:F2}  ";
		}
		if(float.TryParse(history.RiverProtect, out float riverProtect) && riverProtect != 0){
			TextInsuranceBuyStr += $"投保:{riverProtect:F2}  ";
		}
		if (protect != 0 || riverProtect != 0) {
			string pay = "";
			foreach (string ipay in history.ProtectProfit) {
				pay = Util.Sum2String(pay, ipay);
			}
			TextInsuranceBuyStr += pay == "" ? "" : $"赔付:{pay}";
		}
		TextInsuranceBuyText.text = TextInsuranceBuyStr;
		if (history.HandCards[0] == -1 || history.HandCards[1] == -1) {
			ToggleLook.gameObject.SetActive(true);
		}
		else {
			ToggleLook.gameObject.SetActive(false);
		}
		if (Procedure.deskinfo.BaseConfig.Jackpot == true && history.JackPotAward != "" && history.JackPotAward != "0.00") {
			TextJP.text = $"JP: {history.JackPotAward}";
			TextJP.gameObject.SetActive(true);
		}
		else {
			TextJP.gameObject.SetActive(false);
		}
	}
}

using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using System.Collections.Generic;
using static UtilityBuiltin;
using UnityEngine.Events;
using System.Linq;

public class GameReviewZJHItem : MonoBehaviour
{
	public Text TextNick;
	public RawImage ImageAvatar;
	public GameObject flag;
	public GameObject GoSelf;
	public Image cardType;
	public Text state;
	public List<Card> Cards;
	public Text TextProfit;
	public Text Textjp;
	public Button LookCardBtn;

	public UnityAction lookCardAction;

	void OnDisable()
	{
		lookCardAction = null;
	}

	private Msg_ZjhLastHistory msg_ZjhLastHistory;

	public void Init(Msg_ZjhLastHistory history)
	{
		msg_ZjhLastHistory = history;
		TextNick.FormatNickname(history.BasePlayer.Nick);
		Util.DownloadHeadImage(ImageAvatar, history.BasePlayer.HeadImage);
		flag.SetActive(history.IsBanker == true);
		GoSelf.SetActive(Util.IsMySelf(history.BasePlayer.PlayerId));
		
		TextProfit.FormatRichText(history.Coin);
		if (float.Parse(history.JackAward) > 0){
			Textjp.text = "jp: +" + history.JackAward;
			TextProfit.transform.localPosition = new Vector3(TextProfit.transform.localPosition.x, 0, TextProfit.transform.localPosition.z);
		}else{
			Textjp.text = "";
			TextProfit.transform.localPosition = new Vector3(TextProfit.transform.localPosition.x, -17, TextProfit.transform.localPosition.z);
		}

		if (history.Coin > 0)
		{
			state.gameObject.SetActive(false);
		}
		else{
			state.text = history.IsGive ? "弃牌" : "淘汰";
			state.gameObject.SetActive(true);
		}

		ShowCard(history.Cards.ToArray(), history.CardType);
	}

	public void ShowCard(int[] cards, int zjhCardType)
	{
		bool isShowCard = true;
		if(cards.Length > 0 && cards[0] == -1){
			isShowCard = false;
		}
		LookCardBtn.gameObject.SetActive(!isShowCard);
		for (int i = 0; i < cards.Length; i++)
		{
			if (isShowCard && i < Cards.Count)
			{
				Cards[i].Init(cards[i]);
			}
			else
			{
				Cards[i].Init(-1);
			}
		}
		if (isShowCard && msg_ZjhLastHistory.Coin > 0)
		{
			cardType.SetSprite(GameConstants.GetCardTypeString((ZjhCardType)zjhCardType), true);
			cardType.gameObject.SetActive(true);
		}
		else
		{
			cardType.gameObject.SetActive(false);
		}
	}

	public void LookCardOnClick(){
        lookCardAction?.Invoke();
    }
}

using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using System.Collections.Generic;
using static UtilityBuiltin;
using UnityEngine.Events;
using System.Linq;

public class GameReviewBJItem : MonoBehaviour
{
	public Text TextNick;
	public RawImage ImageAvatar;
	public GameObject GoSelf;
	public GameObject give;
	public Image cardType;
	public List<GameObject> piers;
	public Text TextProfit;
	public Text TextJP;
	public Text TextLuck;
	public Text TextLuckDes;

	private Msg_CKHistory Msg_CKHistory;

	public void Init(Msg_CKHistory history)
	{
		Msg_CKHistory = history;
		TextNick.FormatNickname(history.BasePlayer.Nick);
		Util.DownloadHeadImage(ImageAvatar, history.BasePlayer.HeadImage);
		give.SetActive(history.IsGive == true);
		GoSelf.SetActive(Util.IsMySelf(history.BasePlayer.PlayerId));
		
		TextProfit.FormatRichText(history.Coin);

		float jpValue = float.Parse(history.JackAward);
		TextJP.text = jpValue == 0 ? "" : $"JP：<color=#C1A281>+{jpValue:0.00}</color>";

		float luckValue = float.Parse(history.LuckCoin);
		TextLuck.text = (luckValue != 0 || history.LuckCard.Count > 0) ? $"喜钱：{Util.FormatRichText(luckValue)}" : "";

		string luckDes = "";
		if(history.LuckCard != null && history.LuckCard.Count > 0){
			foreach(var card in history.LuckCard){
				luckDes += GameUtil.ShowType(card, MethodType.CompareChicken) + " ";
			}
		}
		TextLuckDes.text = luckDes;

		ShowCard(history.ComposeCard.ToArray());
	}

	public void ShowCard(ChickenCards[] composeCards)
	{
		// 检查数据有效性
		if(composeCards == null || composeCards.Length != 3){
			GF.LogInfo("牌局回顾数据错误", composeCards?.ToString() ?? "null");
			return;
		}

		// 初始化三张牌的显示
		for (int i = 0; i < 3; i++)
		{
			if (i < piers.Count)
			{
				int index = i;
				// 获取每个位置的卡牌数据
				var composeCard = composeCards[index];
				// 初始化卡牌显示
				var pier = piers[i];
				for (int j = 0; j < 3; j++)
				{
					pier.transform.Find("card" + j).GetComponent<Card>().Init(composeCard.Cards[j]);
				}
				// 设置牌型
				Image cardType = pier.transform.Find("type").GetComponent<Image>();
				cardType.SetSprite(GameConstants.GetCardTypeString_bj(composeCard.Type, 1), true);
				pier.transform.Find("profit").GetComponent<Text>().FormatRichText(float.Parse(composeCard.ChangeCoin));
			}
		}
	}

}

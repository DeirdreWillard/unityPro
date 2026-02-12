using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using System.Collections.Generic;
using static UtilityBuiltin;

public class NNGameReview : MonoBehaviour
{
	NNProcedure nnProcedure = null;

	public Text textPage;
	public int totalRound = 0;
	public int curPage = 0;

	public GameObject goShouCang1;
	public GameObject goShouCang2;

	public Text textJP;

	private Msg_LastHistoryRs LastHistoryRs;

	void OnEnable()
	{
		nnProcedure = GF.Procedure.CurrentProcedure as NNProcedure;
		HotfixNetworkComponent.AddListener(MessageID.Msg_LastHistoryRs, Function_LastHistoryRs);
		HotfixNetworkComponent.AddListener(MessageID.Msg_CollectRoundRs, Function_CollectRound);
		Send_LastHistoryRq(0);
	}

	void OnDisable()
	{
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_LastHistoryRs, Function_LastHistoryRs);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_CollectRoundRs, Function_CollectRound);
	}

	public void Send_LastHistoryRq(int curPage)
	{
		Msg_LastHistoryRq req = MessagePool.Instance.Fetch<Msg_LastHistoryRq>();
		req.Round = curPage;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_LastHistoryRq, req);
	}

	public void Function_LastHistoryRs(MessageRecvData data)
	{
		GF.LogInfo("收到消息上局回顾");
		Msg_LastHistoryRs ack = Msg_LastHistoryRs.Parser.ParseFrom(data.Data);
		totalRound = ack.TotalRound;
		LastHistoryRs = ack;
		ReshUI();
	}

	public void Function_CollectRound(MessageRecvData data)
	{
		GF.LogInfo("收到消息收藏结果");
		Msg_CollectRoundRs ack = Msg_CollectRoundRs.Parser.ParseFrom(data.Data);
		LastHistoryRs.State = ack.State;
		GF.UI.ShowToast(ack.State == 1 ? "收藏成功" : "取消收藏");
		goShouCang1.SetActive(LastHistoryRs.State != 1);
		goShouCang2.SetActive(LastHistoryRs.State == 1);
	}

	public void BtnMinus10Function()
	{
		if (Util.IsClickLocked()) return;
		_ = GF.Sound.PlayEffect(AudioKeys.SOUND_BTN);
		curPage -= 10;
		if (curPage < 0) curPage = 0;
		Send_LastHistoryRq(totalRound - curPage);
	}

	public void BtnMinus1Function()
	{
		if (Util.IsClickLocked()) return;
		_ = GF.Sound.PlayEffect(AudioKeys.SOUND_BTN);
		curPage--;
		if (curPage < 0) curPage = 0;
		Send_LastHistoryRq(totalRound - curPage);
	}

	public void BtnAdd1Function()
	{
		if (Util.IsClickLocked()) return;
		_ = GF.Sound.PlayEffect(AudioKeys.SOUND_BTN);
		curPage++;
		if (curPage >= totalRound) curPage = totalRound - 1;
		Send_LastHistoryRq(totalRound - curPage);
	}

	public void BtnAdd10Function()
	{
		if (Util.IsClickLocked()) return;
		_ = GF.Sound.PlayEffect(AudioKeys.SOUND_BTN);
		curPage += 10;
		if (curPage >= totalRound) curPage = totalRound - 1;
		Send_LastHistoryRq(totalRound - curPage);
	}

	public GameObject item;
	public Transform Content;
	public void ReshUI()
	{
		textPage.text = totalRound - curPage + "/" + totalRound;
		string jp = "";
		for (int i = 0; i < LastHistoryRs.History.Count; i++)
		{
			Transform child = null;
			if (Content.childCount <= i)
			{
				child = Instantiate(item).transform;
				child.SetParent(Content.transform, false);
			}
			else
			{
				child = Content.GetChild(i);
			}
			child.gameObject.SetActive(true);
			AvatarInfo avatarinfo = child.GetComponent<AvatarInfo>();
			avatarinfo.Init(LastHistoryRs.History[i].BasePlayer);
			if (LastHistoryRs.History[i].IsBanker == true)
			{
				child.Find("flag").gameObject.SetActive(true);
			}
			else
			{
				child.Find("flag").gameObject.SetActive(false);
			}

			child.Find("type").GetComponent<Image>().SetSprite("NN/CardType/" + ShowType(LastHistoryRs.History[i].Niu) + ".png", true);
			for (int j = 0; j < LastHistoryRs.History[i].Cards.Count; j++)
			{
				child.Find("cards").GetChild(j).GetComponent<Card>().Init(LastHistoryRs.History[i].Cards[j]);
				child.Find("cards").GetChild(j).GetChild(0).gameObject.SetActive(false);
			}
			Dictionary<int, int> shown = new();
			for (int j = 0; j < LastHistoryRs.History[i].ComposeCard.Count; j++)
			{
				for (int k = 0; k < 5; k++)
				{
					if (LastHistoryRs.History[i].Cards[k] == LastHistoryRs.History[i].ComposeCard[j] && !shown.ContainsKey(j))
					{
						shown[k] = 1;
						child.Find("cards").GetChild(k).GetChild(0).gameObject.SetActive(true);
						break;
					}
				}
			}
			jp = Util.Sum2String(jp, LastHistoryRs.History[i].Jackpot);
			child.Find("profit").GetComponent<Text>().GetComponent<Text>().FormatRichText(LastHistoryRs.History[i].Coin);
			if (nnProcedure.enterNiuNiuDeskRs.BaseConfig.Jackpot == true && LastHistoryRs.History[i].JackpotAward != "" && LastHistoryRs.History[i].JackpotAward != "0")
			{
				child.Find("jp").GetComponent<Text>().GetComponent<Text>().text = $"JP: {LastHistoryRs.History[i].JackpotAward}";
				child.Find("jp").gameObject.SetActive(true);
			}
			else
			{
				child.Find("jp").gameObject.SetActive(false);
			}
		}
		textJP.text = $"JP：{jp}";
		if (nnProcedure.enterNiuNiuDeskRs.BaseConfig.Jackpot == true)
		{
			textJP.gameObject.SetActive(true);
		}
		else
		{
			textJP.gameObject.SetActive(false);
		}
		for (var i = LastHistoryRs.History.Count; i < Content.childCount; i++)
		{
			Content.GetChild(i).gameObject.SetActive(false);
		}
		goShouCang1.SetActive(LastHistoryRs.State != 1);
		goShouCang2.SetActive(LastHistoryRs.State == 1);
	}

	private string ShowType(int n)
	{
		switch (n)
		{
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

	public void ButtonClick_ShouCang()
	{	
		if (Util.IsClickLocked()) return;
		_ = GF.Sound.PlayEffect(AudioKeys.SOUND_BTN);
		Msg_CollectRoundRq req = MessagePool.Instance.Fetch<Msg_CollectRoundRq>();
		req.State = LastHistoryRs.State == 1 ? 0 : 1;
		req.ReportId = LastHistoryRs.ReportId;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_CollectRoundRq, req);
	}

	public void ButtonClick_Play()
	{
		if (Util.IsClickLocked(1f)) return;
		_ = GF.Sound.PlayEffect(AudioKeys.SOUND_BTN);
		if (string.IsNullOrEmpty(LastHistoryRs.ReportId))
		{
			GF.UI.ShowToast("暂无记录可回放");
			return;
		}
		Msg_PlayBackRq req = MessagePool.Instance.Fetch<Msg_PlayBackRq>();
		req.ReportId = LastHistoryRs.ReportId;
		GF.LogInfo("请求牌谱:", req.ReportId);
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_PlayBackRq, req);
	}
}

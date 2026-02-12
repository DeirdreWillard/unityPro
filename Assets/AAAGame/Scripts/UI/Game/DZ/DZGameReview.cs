
using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using System.Linq;

public class DZGameReview : MonoBehaviour
{
	public DZGamePanel GamePanel;
	DZProcedure Procedure;
	public Text textPage;
	public int totalRound = 0;
	public int curPage = 0;

	public Text TextInsurance;
	public Text TextJackpot;

	public GameObject goShouCang1;
	public GameObject goShouCang2;

	public GameObject item;
	public GameObject GoLook;
	public Text TextLook;
	public Transform Content;
	public ToggleGroup ToggleGroup;

	private Msg_TexasLastHistoryRs LastHistory;


	void OnEnable()
	{
		Procedure = GF.Procedure.CurrentProcedure as DZProcedure;
		HotfixNetworkComponent.AddListener(MessageID.Msg_TexasLastHistoryRs, Function_LastHistory);
		HotfixNetworkComponent.AddListener(MessageID.Msg_CollectRoundRs, Function_CollectRound);
		HotfixNetworkComponent.AddListener(MessageID.Msg_LookCardRs, Function_LookCard);
		TextInsurance.text = "";
		TextJackpot.text = "";
		curPage = 0;
		Send_LastHistory(0);
	}

	void OnDisable()
	{
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_TexasLastHistoryRs, Function_LastHistory);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_CollectRoundRs, Function_CollectRound);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_LookCardRs, Function_LookCard);
	}

	public void Send_LastHistory(int curPage)
	{
		Msg_LastHistoryRq req = MessagePool.Instance.Fetch<Msg_LastHistoryRq>();
		req.Round = curPage;
		GF.LogInfo("请求牌局记录. ", $"round:{curPage}");
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_LastHistoryRq, req);
	}

	public void Function_LastHistory(MessageRecvData data)
	{
		Msg_TexasLastHistoryRs ack = Msg_TexasLastHistoryRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息上局回顾. ", $"total:{ack.TotalRound} count:{ack.History.Count} ack:{ack}");
		totalRound = ack.TotalRound;
		LastHistory = ack;
		Refresh();
	}

	public void Function_CollectRound(MessageRecvData data)
	{
		GF.LogInfo("收到消息收藏结果");
		Msg_CollectRoundRs ack = Msg_CollectRoundRs.Parser.ParseFrom(data.Data);
		LastHistory.State = ack.State;
		GF.UI.ShowToast(ack.State == 1 ? "收藏成功" : "取消收藏");
		goShouCang1.SetActive(LastHistory.State != 1);
		goShouCang2.SetActive(LastHistory.State == 1);
	}

	public void BtnAdd10Function()
	{
		if (Util.IsClickLocked()) return;
		_ = GF.Sound.PlayEffect(AudioKeys.SOUND_BTN);
		curPage -= 10;
		if (curPage < 0) curPage = 0;
		Send_LastHistory(totalRound - curPage);
	}

	public void BtnAdd1Function()
	{
		if (Util.IsClickLocked()) return;
		_ = GF.Sound.PlayEffect(AudioKeys.SOUND_BTN);
		curPage--;
		if (curPage < 0) curPage = 0;
		Send_LastHistory(totalRound - curPage);
	}

	public void BtnMinus1Function()
	{
		if (Util.IsClickLocked()) return;
		_ = GF.Sound.PlayEffect(AudioKeys.SOUND_BTN);
		curPage++;
		if (curPage >= totalRound) curPage = totalRound - 1;
		Send_LastHistory(totalRound - curPage);
	}

	public void BtnMinus10Function()
	{
		if (Util.IsClickLocked()) return;
		_ = GF.Sound.PlayEffect(AudioKeys.SOUND_BTN);
		curPage += 10;
		if (curPage >= totalRound) curPage = totalRound - 1;
		Send_LastHistory(totalRound - curPage);
	}

	public void Refresh()
	{
		textPage.text = $"{totalRound - curPage}/{totalRound}";
		bool look = false;
		string jp = "";
		for (int i = 0; i < LastHistory.History.Count; i++)
		{
			Transform child;
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
			child.name = LastHistory.History[i].Player.PlayerId.ToString();
			child.GetComponent<DZGameReviewItem>().Init(curPage, LastHistory.CommonCards.ToList(), LastHistory.History[i]);
			if (LastHistory.History[i].HandCards[0] == -1 || LastHistory.History[i].HandCards[1] == -1)
			{
				look = true;
			}
			jp = Util.Sum2String(jp, LastHistory.History[i].JackPot);
		}
		for (var i = LastHistory.History.Count; i < Content.childCount; i++)
		{
			Content.GetChild(i).gameObject.SetActive(false);
		}
		TextJackpot.text = $"{jp}";
		if (look)
		{
			GoLook.SetActive(true);
			TextLook.text = GamePanel.SystemConfig[16].ToString();
		}
		else
		{
			GoLook.SetActive(false);
		}
		goShouCang1.SetActive(LastHistory.State != 1);
		goShouCang2.SetActive(LastHistory.State == 1);
		TextInsurance.FormatRichText(LastHistory.TotalProtect);
	}

	public void ButtonClick_ShouCang()
	{
		if (Util.IsClickLocked()) return;
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
		Msg_CollectRoundRq req = MessagePool.Instance.Fetch<Msg_CollectRoundRq>();
		req.State = LastHistory.State == 1 ? 0 : 1;
		req.ReportId = LastHistory.ReportId;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_CollectRoundRq, req);
	}
	public void ButtonClick_Play()
	{
		if (Util.IsClickLocked(1f)) return;
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
		if (string.IsNullOrEmpty(LastHistory.ReportId))
		{
			GF.UI.ShowToast("暂无记录可回放");
			return;
		}
		Msg_PlayBackRq req = MessagePool.Instance.Fetch<Msg_PlayBackRq>();
		req.ReportId = LastHistory.ReportId;
		GF.LogInfo("请求牌谱:", req.ReportId);
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_PlayBackRq, req);
	}

	public void ButtonClick_Look()
	{
		if (Util.IsClickLocked()) return;
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
		DZProcedure Procedure = GF.Procedure.CurrentProcedure as DZProcedure;
		Toggle SelectedToggle = ToggleGroup.ActiveToggles().FirstOrDefault(t => t.isOn);
		if (SelectedToggle == null) return;
		int playerid = int.Parse(SelectedToggle.transform.parent.name);
		GF.LogInfo("查看玩家手牌. ", $"玩家ID:{playerid} round:{totalRound - curPage}");
		Procedure.Send_LookCard(totalRound - curPage, playerid);
	}

	public void Function_LookCard(MessageRecvData data)
	{
		Msg_LookCardRs ack = Msg_LookCardRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到玩家手牌. ", $"玩家ID:{ack.PlayerId} round:{ack.Round} 手牌:{ack.HandCard[0]} {ack.HandCard[1]}");
		if (ack.Round != totalRound - curPage) return;
		for (int i = 0; i < LastHistory.History.Count; i++)
		{
			if (LastHistory.History[i].Player.PlayerId != ack.PlayerId) continue;
			LastHistory.History[i].HandCards.Clear();
			LastHistory.History[i].HandCards.Add(ack.HandCard[0]);
			LastHistory.History[i].HandCards.Add(ack.HandCard[1]);
		}
		Refresh();
	}
}

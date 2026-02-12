﻿using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using System.Linq;

public class GameReviewBJ : MonoBehaviour
{
	public GameObject panel;
	public Text textPage;
	public Text jpValue;

	public int totalRound = 0;
	public int curPage = 0;

	public GameObject goShouCang1;
	public GameObject goShouCang2;

	private Msg_CKLastHistoryRs LastHistoryRs;

	void OnEnable()
	{
		HotfixNetworkComponent.AddListener(MessageID.Msg_CKLastHistoryRs, Function_CKLastHistoryRs);
		HotfixNetworkComponent.AddListener(MessageID.Msg_CollectRoundRs, Function_CollectRound);
		curPage = 0;
		Send_LastHistoryRq(0);
	}

	void OnDisable()
	{
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_CKLastHistoryRs, Function_CKLastHistoryRs);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_CollectRoundRs, Function_CollectRound);
	}

	public void Send_LastHistoryRq(int curPage)
	{
		Msg_LastHistoryRq req = MessagePool.Instance.Fetch<Msg_LastHistoryRq>();
		req.Round = curPage;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_LastHistoryRq, req);
	}

	public void Function_CKLastHistoryRs(MessageRecvData data)
	{
		Msg_CKLastHistoryRs ack = Msg_CKLastHistoryRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息上局回顾", ack.ToString());
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
		curPage += 10;
		if (curPage >= totalRound) curPage = totalRound - 1;
		Send_LastHistoryRq(totalRound - curPage);
	}

	public void BtnMinus1Function()
	{
		if (Util.IsClickLocked()) return;
		_ = GF.Sound.PlayEffect(AudioKeys.SOUND_BTN);
		curPage++;
		if (curPage >= totalRound) curPage = totalRound - 1;
		Send_LastHistoryRq(totalRound - curPage);
	}

	public void BtnAdd1Function()
	{
		if (Util.IsClickLocked()) return;
		_ = GF.Sound.PlayEffect(AudioKeys.SOUND_BTN);
		curPage--;
		if (curPage < 0) curPage = 0;
		Send_LastHistoryRq(totalRound - curPage);
	}

	public void BtnAdd10Function()
	{
		if (Util.IsClickLocked()) return;
		_ = GF.Sound.PlayEffect(AudioKeys.SOUND_BTN);
		curPage -= 10;
		if (curPage < 0) curPage = 0;
		Send_LastHistoryRq(totalRound - curPage);
	}

	public GameObject item;
	public Transform Content;
	public void ReshUI()
	{
		textPage.text = totalRound - curPage + "/" + totalRound;

		float sum = LastHistoryRs.History.Sum(h => float.Parse(h.JackPot));
		jpValue.text = sum == 0 ? "" : $"JP：<color=#C1A281>+{sum:0.00}</color>";

		for (int i = 0; i < LastHistoryRs.History.Count; i++)
		{
			int index = i;
			Transform child = null;
			if (Content.childCount <= index)
			{
				child = Instantiate(item).transform;
				child.SetParent(Content.transform, false);
			}
			else
			{
				child = Content.GetChild(index);
			}
			child.gameObject.SetActive(true);
			child.name = LastHistoryRs.History[index].BasePlayer.PlayerId.ToString();
			child.GetComponent<GameReviewBJItem>().Init(LastHistoryRs.History[index]);
		}
		for (var i = LastHistoryRs.History.Count; i < Content.childCount; i++)
		{
			Content.GetChild(i).gameObject.SetActive(false);
		}
		goShouCang1.SetActive(LastHistoryRs.State != 1);
		goShouCang2.SetActive(LastHistoryRs.State == 1);
	}

	public void ButtonClick_ShouCang()
	{
		if (Util.IsClickLocked()) return;
		_ = GF.Sound.PlayEffect(AudioKeys.SOUND_BTN);
		if (string.IsNullOrEmpty(LastHistoryRs.ReportId))
		{
			GF.UI.ShowToast("暂无记录可收藏");
			return;
		}
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

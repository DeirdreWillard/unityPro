﻿using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using System.Linq;
using System.Collections.Generic;
public class RealTimeBRPanelNN : MonoBehaviour
{
	public Text TxtDuratuion;
	public GameObject goRates;
	public Text TxtRates;
	public GameObject goInsurance;
	public Text TxtInsurance;
	private Msg_AccountBookRs accountBookRs;

	public GameObject Item1;
	public GameObject Item2;

	public Toggle toggleCuopai;

	public GameObject content;

	public GameObject g1_3;

	public GameObject g2_1;
	public GameObject g2_2;
	public GameObject g2_3;

	public GameObject g3_1;
	public GameObject g3_2;

	public GameObject g4_1;
	public GameObject g4_2;

	public ScrollRect scrollRect;

	NNProcedure nnProcedure = null;
	DZProcedure dzProcedure = null;
	ZJHProcedure zjhProcedure = null;
	BJProcedure bjProcedure = null;

	void Start()
	{
	}

	void OnEnable()
	{
		DeskPlayer selfplayer = null;
		if (GF.Procedure.CurrentProcedure is NNProcedure)
		{
			nnProcedure = GF.Procedure.CurrentProcedure as NNProcedure;
			selfplayer = nnProcedure.GetSelfPlayer();
		}
		if (GF.Procedure.CurrentProcedure is DZProcedure)
		{
			dzProcedure = GF.Procedure.CurrentProcedure as DZProcedure;
			selfplayer = dzProcedure.GetSelfPlayer();
		}
		if (GF.Procedure.CurrentProcedure is ZJHProcedure)
		{
			zjhProcedure = GF.Procedure.CurrentProcedure as ZJHProcedure;
			selfplayer = zjhProcedure.GetSelfPlayer();
		}
		if (GF.Procedure.CurrentProcedure is BJProcedure)
		{
			bjProcedure = GF.Procedure.CurrentProcedure as BJProcedure;
			selfplayer = bjProcedure.GetSelfPlayer();
		}
		HotfixNetworkComponent.AddListener(MessageID.Msg_AccountBookRs, Function_AccountBookRs);
		HotfixNetworkComponent.AddListener(MessageID.Msg_AutoLookRs, Function_AutoLookRs);
		HotfixNetworkComponent.AddListener(MessageID.Msg_SynDeskContinuedTime, Function_SynDeskContinuedTime);
		Msg_AccountBookRq req = MessagePool.Instance.Fetch<Msg_AccountBookRq>();
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_AccountBookRq, req);
		if (selfplayer != null) {
			toggleCuopai.isOn = selfplayer.AutoFollow;
		}
		scrollRect.verticalNormalizedPosition = 1f;
	}

	void OnDisable()
	{
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_AccountBookRs, Function_AccountBookRs);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_AutoLookRs, Function_AutoLookRs);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynDeskContinuedTime, Function_SynDeskContinuedTime);
	}

	public void Function_AccountBookRs(MessageRecvData data)
	{
		Msg_AccountBookRs ack = Msg_AccountBookRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("收到消息即时战绩. ", $"游戏中:{ack.SitInfos.Count} 已离桌:{ack.LeaveInfos.Count} 已结算:{ack.SettleInfos.Count} 在线旁观者:{ack.WatchInfos.Count} 离线旁观者:{ack.LeaveWatchInfos.Count}");
		GF.LogInfo("消息即时战绩", ack.ToString());
		accountBookRs = ack;
		ReshUI();
	}

	public void Function_SynDeskContinuedTime(MessageRecvData data)//
	{
		GF.LogInfo("房间续时");
		Msg_SynDeskContinuedTime ack = Msg_SynDeskContinuedTime.Parser.ParseFrom(data.Data);
		duration += ack.ContinuedTime * 60;
	}

	float duration = 0;

	public void ReshUI()
	{
		int baseitem = 2;
		// if ((nnProcedure != null && nnProcedure.IsRoomOwner()) || (dzProcedure != null && dzProcedure.IsRoomOwner())
		// 	|| (zjhProcedure != null && zjhProcedure.GamePanel_zjh.IsRoomCreate())) {
		if(accountBookRs.ShowRate){
			goRates.SetActive(true);
			float allRate = 0;
			for (int i = 0; i < accountBookRs.Rates.Count; i++)
			{
				allRate += float.Parse(accountBookRs.Rates[i].Rate);
			}
			TxtRates.text = allRate.ToString("F2");
		}
		else {
			goRates.SetActive(false);
		}
		if (dzProcedure != null) {
			baseitem = 3;
			goInsurance.SetActive(true);
			TxtInsurance.FormatRichText(float.Parse(accountBookRs.TotalProtect));
		}
		else {
			if (goInsurance != null) goInsurance?.gameObject.SetActive(false);
		}
		duration = accountBookRs.TotalTime;

		for (var i = baseitem;i < content.transform.childCount;i++) {
			var item = content.transform.GetChild(i).gameObject;
			Destroy(item);
		}

		GameObject baseitem13;
		baseitem13 = Instantiate(g1_3);
		baseitem13.transform.SetParent(content.transform);
		baseitem13.transform.localPosition = Vector3.zero;
		baseitem13.transform.localScale = Vector3.one;
		baseitem13.SetActive(true);

		var list1 = accountBookRs.SitInfos.ToList().OrderByDescending(info => {
			return float.Parse(info.Profit);
		}).ToList();
		for (int i = 0; i < list1.Count; i++) {
			GameObject item;
			item = Instantiate(Item1);
			item.transform.SetParent(content.transform);
			item.transform.localPosition = Vector3.zero;
			item.transform.localScale = Vector3.one;
			item.SetActive(true);
			if (Util.IsMySelf(list1[i].PlayerId)) {
				item.transform.Find("my").gameObject.SetActive(true);
			}
			else {
				item.transform.Find("my").gameObject.SetActive(false);
			}
			item.transform.Find("name").GetComponent<Text>().FormatNickname(list1[i].Nick);
			item.transform.Find("bring").GetComponent<Text>().text = list1[i].Bring.ToString();
			// float profit = float.Parse(list1[i].Current) - float.Parse(list1[i].Bring);
			// profit = Mathf.Round(profit * 100f) / 100f;
			item.transform.Find("primot").GetComponent<Text>().FormatRichText(float.Parse(list1[i].Profit));
		}

		GameObject baseitem21;
		baseitem21 = Instantiate(g2_1);
		baseitem21.transform.SetParent(content.transform);
		baseitem21.transform.localPosition = Vector3.zero;
		baseitem21.transform.localScale = Vector3.one;
		baseitem21.SetActive(true);

		GameObject baseitem22;
		baseitem22 = Instantiate(g2_2);
		baseitem22.transform.SetParent(content.transform);
		baseitem22.transform.localPosition = Vector3.zero;
		baseitem22.transform.localScale = Vector3.one;
		baseitem22.SetActive(true);

		GameObject baseitem23;
		baseitem23 = Instantiate(g2_3);
		baseitem23.transform.SetParent(content.transform);
		baseitem23.transform.localPosition = Vector3.zero;
		baseitem23.transform.localScale = Vector3.one;
		baseitem23.SetActive(true);

		var list2 = accountBookRs.LeaveInfos.ToList().OrderByDescending(info => {
			return float.Parse(info.Profit);
		}).ToList();
		for (int i = 0; i < list2.Count; i++) {
			GameObject item = Instantiate(Item1);
			item.transform.SetParent(content.transform);
			item.transform.localPosition = Vector3.zero;
			item.transform.localScale = Vector3.one;
			item.SetActive(true);
			if (Util.IsMySelf(list2[i].PlayerId)) {
				item.transform.Find("my").gameObject.SetActive(true);
			}
			else {
				item.transform.Find("my").gameObject.SetActive(false);
			}
			item.transform.Find("name").GetComponent<Text>().FormatNickname(list2[i].Nick);
			item.transform.Find("bring").GetComponent<Text>().text = list2[i].Bring.ToString();
			// float profit = float.Parse(list2[i].Current) - float.Parse(list2[i].Bring);
			// profit = Mathf.Round(profit * 100f) / 100f;
			item.transform.Find("primot").GetComponent<Text>().FormatRichText(float.Parse(list2[i].Profit));
		}

		GameObject baseitem31;
		baseitem31 = Instantiate(g3_1);
		baseitem31.transform.SetParent(content.transform);
		baseitem31.transform.localPosition = Vector3.zero;
		baseitem31.transform.localScale = Vector3.one;
		baseitem31.SetActive(true);

		GameObject baseitem32;
		baseitem32 = Instantiate(g3_2);
		baseitem32.transform.SetParent(content.transform);
		baseitem32.transform.localPosition = Vector3.zero;
		baseitem32.transform.localScale = Vector3.one;
		baseitem32.SetActive(true);
		var list3 = accountBookRs.SettleInfos.ToList().OrderByDescending(info => {
			return float.Parse(info.Profit);
		}).ToList();
		for (int i = 0; i < list3.Count; i++) {
			GameObject item = Instantiate(Item1);
			item.transform.SetParent(content.transform);
			item.transform.localPosition = Vector3.zero;
			item.transform.localScale = Vector3.one;
			item.SetActive(true);
			if (Util.IsMySelf(list3[i].PlayerId)) {
				item.transform.Find("my").gameObject.SetActive(true);
			}
			else {
				item.transform.Find("my").gameObject.SetActive(false);
			}
			item.transform.Find("name").GetComponent<Text>().FormatNickname(list3[i].Nick);
			item.transform.Find("bring").GetComponent<Text>().text = list3[i].Bring.ToString();
			// float profit = float.Parse(list3[i].Current) - float.Parse(list3[i].Bring);
			// profit = Mathf.Round(profit * 100f) / 100f;
			item.transform.Find("primot").GetComponent<Text>().FormatRichText(float.Parse(list3[i].Profit));
		}

		GameObject baseitem41;
		baseitem41 = Instantiate(g4_1);
		baseitem41.transform.SetParent(content.transform);
		baseitem41.transform.localPosition = Vector3.zero;
		baseitem41.transform.localScale = Vector3.one;
		baseitem41.SetActive(true);
		baseitem41.transform.Find("state").GetComponent<Text>().text = $"旁观者({accountBookRs.WatchPlayer})";

		GameObject baseitem42;
		baseitem42 = Instantiate(g4_2);
		baseitem42.transform.SetParent(content.transform);
		baseitem42.transform.localPosition = Vector3.zero;
		baseitem42.transform.localScale = Vector3.one;
		baseitem42.SetActive(true);

		List<BasePlayer> list4 = new();
		list4.AddRange(accountBookRs.WatchInfos.ToList());
		list4.AddRange(accountBookRs.LeaveWatchInfos.ToList());

		for (int i = 0; i < list4.Count; i += 4) {
			if (i >= 40) break;
			GameObject item = Instantiate(Item2);
			item.transform.SetParent(content.transform);
			item.transform.localPosition = Vector3.zero;
			item.transform.localScale = Vector3.one;
			item.SetActive(true);
			if (i < list4.Count) {
				item.transform.Find("item1").GetComponent<AvatarInfo>().Init(list4[i].Nick, list4[i].HeadImage, list4[i].Vip);
				if (Util.IsMySelf(list4[i].PlayerId)) {
					item.transform.Find("item1/my").gameObject.SetActive(true);
				}
				else {
					item.transform.Find("item1/my").gameObject.SetActive(false);
				}
				item.transform.Find("item1").gameObject.SetActive(true);
				if (accountBookRs.WatchInfos.ToList().Contains(list4[i])) {
					item.transform.Find("item1/mask/avatar").GetComponent<RawImage>().color = Color.white;
				}
				else {
					item.transform.Find("item1/mask/avatar").GetComponent<RawImage>().color = Color.gray;
				}
			}
			else {
				item.transform.Find("item1").gameObject.SetActive(false);
			}
			if (i + 1 < list4.Count) {
				item.transform.Find("item2").GetComponent<AvatarInfo>().Init(list4[i + 1].Nick, list4[i + 1].HeadImage, list4[i + 1].Vip);
				if (Util.IsMySelf(list4[i + 1].PlayerId)) {
					item.transform.Find("item2/my").gameObject.SetActive(true);
				}
				else {
					item.transform.Find("item2/my").gameObject.SetActive(false);
				}
				item.transform.Find("item2").gameObject.SetActive(true);
				if (accountBookRs.WatchInfos.ToList().Contains(list4[i + 1])) {
					item.transform.Find("item2/mask/avatar").GetComponent<RawImage>().color = Color.white;
				}
				else {
					item.transform.Find("item2/mask/avatar").GetComponent<RawImage>().color = Color.gray;
				}
			}
			else {
				item.transform.Find("item2").gameObject.SetActive(false);
			}
			if (i + 2 < list4.Count) {
				item.transform.Find("item3").GetComponent<AvatarInfo>().Init(list4[i + 2].Nick, list4[i + 2].HeadImage, list4[i + 2].Vip);
				if (Util.IsMySelf(list4[i + 2].PlayerId)) {
					item.transform.Find("item3/my").gameObject.SetActive(true);
				}
				else {
					item.transform.Find("item3/my").gameObject.SetActive(false);
				}
				item.transform.Find("item3").gameObject.SetActive(true);
				if (accountBookRs.WatchInfos.ToList().Contains(list4[i + 2])) {
					item.transform.Find("item3/mask/avatar").GetComponent<RawImage>().color = Color.white;
				}
				else {
					item.transform.Find("item3/mask/avatar").GetComponent<RawImage>().color = Color.gray;
				}
			}
			else {
				item.transform.Find("item3").gameObject.SetActive(false);
			}
			if (i + 3 < list4.Count) {
				item.transform.Find("item4").GetComponent<AvatarInfo>().Init(list4[i + 3].Nick, list4[i + 3].HeadImage, list4[i + 3].Vip);
				if (Util.IsMySelf(list4[i + 3].PlayerId)) {
					item.transform.Find("item4/my").gameObject.SetActive(true);
				}
				else {
					item.transform.Find("item4/my").gameObject.SetActive(false);
				}
				item.transform.Find("item4").gameObject.SetActive(true);
				if (accountBookRs.WatchInfos.ToList().Contains(list4[i + 3])) {
					item.transform.Find("item4/mask/avatar").GetComponent<RawImage>().color = Color.white;
				}
				else {
					item.transform.Find("item4/mask/avatar").GetComponent<RawImage>().color = Color.gray;
				}
			}
			else {
				item.transform.Find("item4").gameObject.SetActive(false);
			}
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());
	}

	void Update() {
		if (nnProcedure != null && nnProcedure.enterNiuNiuDeskRs.DeskState == DeskState.Pause) return;
		if (dzProcedure != null && dzProcedure.deskinfo.DeskState == DeskState.Pause) return;
		if (zjhProcedure != null && zjhProcedure.enterZjhDeskRs.DeskState == DeskState.Pause) return;
		if (bjProcedure != null && bjProcedure.enterBJDeskRs.BaseInfo.State == DeskState.Pause) return;

		if ((nnProcedure != null && nnProcedure.enterNiuNiuDeskRs.DeskState == DeskState.StartRun)
			|| (dzProcedure != null && dzProcedure.deskinfo.DeskState == DeskState.StartRun)
			|| (zjhProcedure != null && zjhProcedure.enterZjhDeskRs.DeskState == DeskState.StartRun)
			|| (bjProcedure != null && bjProcedure.enterBJDeskRs.BaseInfo.State == DeskState.StartRun))
		{
			duration -= Time.deltaTime;
		}
		if (duration < 0) duration = 0;
		int hours = Mathf.FloorToInt(duration / 3600);
		int minutes = Mathf.FloorToInt((duration % 3600) / 60);
		int seconds = Mathf.FloorToInt(duration % 60);
		TxtDuratuion.text = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
	}

	public void onToggleCuopai(bool isOn)
	{
		Msg_AutoLookRq req = MessagePool.Instance.Fetch<Msg_AutoLookRq>();
		req.AutoLook = toggleCuopai.isOn;
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_AutoLookRq, req);
	}

	public void Function_AutoLookRs(MessageRecvData data)
	{
		Msg_AutoLookRs ack = Msg_AutoLookRs.Parser.ParseFrom(data.Data);
		DeskPlayer selfplayer = null;
		if (nnProcedure != null) {
			selfplayer = nnProcedure.GetSelfPlayer();
		}
		if (dzProcedure != null) {
			selfplayer = dzProcedure.GetSelfPlayer();
		}
		if (zjhProcedure != null) {
			selfplayer = zjhProcedure.GetSelfPlayer();
		}
		if (bjProcedure != null) {
			selfplayer = bjProcedure.GetSelfPlayer();
		}
		if (selfplayer != null) {
			selfplayer.AutoFollow = ack.AutoLook;
			toggleCuopai.isOn = ack.AutoLook;
		}
	}
}

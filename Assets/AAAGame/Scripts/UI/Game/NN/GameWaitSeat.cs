using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using System.Collections.Generic;
using System.Linq;

public class GameWaitSeat : MonoBehaviour
{
	public Button btn_Close;
	public Button Mask;
	public Button btn_PaiDui;
	public Button btn_LiKai;
	public Text LabContinue;

	public AvatarInfo[] Objs;
	public List<BasePlayer> waitPlayers = new List<BasePlayer>();

	private void OnEnable()
	{
		HotfixNetworkComponent.AddListener(MessageID.Msg_LineUpListRs, Function_LineUpListRs);
		HotfixNetworkComponent.AddListener(MessageID.Msg_LineUpRs, Function_LineUpRs);
		Msg_LineUpListRq req = MessagePool.Instance.Fetch<Msg_LineUpListRq>();
		HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_LineUpListRq, req);
		for (int i = 0; i < Objs.Length; i++) {
			Objs[i].Init("", "", 0);
			Objs[i].transform.Find("mask").gameObject.SetActive(false);
		}
	}

	public void Refresh() {
		UserDataModel userDataModel = Util.GetMyselfInfo();
		LabContinue.text = "等待 " + waitPlayers.Count;
		bool find = false;
		for (int i = 0; i < waitPlayers.Count; i++) {
			BasePlayer player = waitPlayers[i];
			if (i < Objs.Length) {
				Objs[i].Init(player.Nick, player.HeadImage, player.Vip);
				Objs[i].transform.Find("mask").gameObject.SetActive(true);
			}
			if (waitPlayers[i].PlayerId == userDataModel.PlayerId) {
				find = true;
			}
		}
		for (int i = waitPlayers.Count; i < Objs.Length; i++) {
			Objs[i].Init("", "", 0);
			Objs[i].transform.Find("mask").gameObject.SetActive(false);
		}
		if (find == true) {
			btn_PaiDui.gameObject.SetActive(false);
			btn_LiKai.gameObject.SetActive(true);
		} else {
			btn_PaiDui.gameObject.SetActive(true);
			btn_LiKai.gameObject.SetActive(false);
		}
	}

	private void OnDisable() {
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_LineUpListRs, Function_LineUpListRs);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_LineUpRs, Function_LineUpRs);
	}

	public void Function_LineUpListRs(MessageRecvData data)
	{
		Msg_LineUpListRs ack = Msg_LineUpListRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("排队等坐列表" , ack.ToString());
		waitPlayers = ack.WaitPlayer.ToList();
		Refresh();
	}

	public void Function_LineUpRs(MessageRecvData data)
	{
		Msg_LineUpRs ack = Msg_LineUpRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("排队等坐操作" , ack.ToString());
		waitPlayers = ack.WaitPlayer.ToList();
		Refresh();
	}
}

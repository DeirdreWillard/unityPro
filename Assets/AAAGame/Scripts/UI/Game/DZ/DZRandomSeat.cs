
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using NetMsg;

public class DZRandomSeat : MonoBehaviour
{
	DZProcedure Procedure;
	public DZGamePanel GamePanel;

	public GameObject Line1, Line2, Line3;
	public List<Card> CardList;

	int clickpos = -1;

	void Awake() {
	}

	void OnEnable() {
		Procedure = GF.Procedure.CurrentProcedure as DZProcedure;

		clickpos = -1; seat = Position.NoSit;
		for (int i = 0;i < Procedure.deskinfo.BaseConfig.PlayerNum;i++) {
			CardList[i].gameObject.SetActive(true);
			CardList[i].GetComponent<Button>().interactable = true;
		}
		for (int i = Procedure.deskinfo.BaseConfig.PlayerNum;i < 9;i++) {
			CardList[i].gameObject.SetActive(false);
			CardList[i].GetComponent<Button>().interactable = false;
		}
		HotfixNetworkComponent.AddListener(MessageID.Msg_TexasRandomPosRs, Function_RandomPosition);
		Refresh();
	}

	void OnDisable() {
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_TexasRandomPosRs, Function_RandomPosition);
	}

	void Update() {
	}

	public void OnClickRandom(int pos) {
		if (clickpos != -1) return;
		clickpos = pos;
		Procedure.Send_RandomSeat();
	}

	public void Refresh() {
		if (Procedure == null) return;
		for (int i = 0;i < Procedure.deskinfo.BaseConfig.PlayerNum;i++) {
			CardList[i].Init(-1);
		}

		List<Position> positionlist = new();
		switch (Procedure.deskinfo.BaseConfig.PlayerNum) {
			case 2: positionlist = new() {Position.Default, Position.NoSit, Position.NoSit, Position.NoSit, Position.Four, Position.NoSit, Position.NoSit, Position.NoSit, Position.NoSit}; break;
			case 3: positionlist = new() {Position.Default, Position.NoSit, Position.NoSit, Position.Three, Position.NoSit, Position.NoSit, Position.Six, Position.NoSit, Position.NoSit}; break;
			case 4: positionlist = new() {Position.Default, Position.NoSit, Position.Two, Position.NoSit, Position.Four, Position.NoSit, Position.NoSit, Position.Seven, Position.NoSit}; break;
			case 5: positionlist = new() {Position.Default, Position.One, Position.Two, Position.NoSit, Position.NoSit, Position.NoSit, Position.Six, Position.Seven, Position.NoSit}; break;
			case 6: positionlist = new() {Position.Default, Position.One, Position.Two, Position.NoSit, Position.Four, Position.NoSit, Position.Six, Position.Seven, Position.NoSit}; break;
			case 7: positionlist = new() {Position.Default, Position.One, Position.Two, Position.Three, Position.NoSit, Position.NoSit, Position.Six, Position.Seven, Position.Eight}; break;
			case 8: positionlist = new() {Position.Default, Position.One, Position.Two, Position.Three, Position.Four, Position.Five, Position.Six, Position.Seven, Position.NoSit}; break;
			case 9: positionlist = new() {Position.Default, Position.One, Position.Two, Position.Three, Position.Four, Position.Five, Position.Six, Position.Seven, Position.Eight}; break;
		}

		foreach (DeskPlayer dp in Procedure.deskinfo.DeskPlayers) {
			int index = 0;
			for (var i = 0;i < 9;i++) {
				if (positionlist[i] == Position.NoSit) continue;
				index++;
				if (positionlist[i] == dp.Pos) {
					break;
				}
			}
			CardList[index - 1].Init(0x20 + index);
			CardList[index - 1].GetComponent<Button>().interactable = false;
		}
	}

	void EnterSeat() {
		Procedure.Send_SitDown(seat);
		gameObject.SetActive(false);
	}

	Position seat = Position.NoSit;
	void Function_RandomPosition(MessageRecvData data) {
		Msg_TexasRandomPosRs ack = Msg_TexasRandomPosRs.Parser.ParseFrom(data.Data);

		List<Position> positionlist = new();
		switch (Procedure.deskinfo.BaseConfig.PlayerNum) {
			case 2: positionlist = new() {Position.Default, Position.NoSit, Position.NoSit, Position.NoSit, Position.Four, Position.NoSit, Position.NoSit, Position.NoSit, Position.NoSit}; break;
			case 3: positionlist = new() {Position.Default, Position.NoSit, Position.NoSit, Position.Three, Position.NoSit, Position.NoSit, Position.Six, Position.NoSit, Position.NoSit}; break;
			case 4: positionlist = new() {Position.Default, Position.NoSit, Position.Two, Position.NoSit, Position.Four, Position.NoSit, Position.NoSit, Position.Seven, Position.NoSit}; break;
			case 5: positionlist = new() {Position.Default, Position.One, Position.Two, Position.NoSit, Position.NoSit, Position.NoSit, Position.Six, Position.Seven, Position.NoSit}; break;
			case 6: positionlist = new() {Position.Default, Position.One, Position.Two, Position.NoSit, Position.Four, Position.NoSit, Position.Six, Position.Seven, Position.NoSit}; break;
			case 7: positionlist = new() {Position.Default, Position.One, Position.Two, Position.Three, Position.NoSit, Position.NoSit, Position.Six, Position.Seven, Position.Eight}; break;
			case 8: positionlist = new() {Position.Default, Position.One, Position.Two, Position.Three, Position.Four, Position.Five, Position.Six, Position.Seven, Position.NoSit}; break;
			case 9: positionlist = new() {Position.Default, Position.One, Position.Two, Position.Three, Position.Four, Position.Five, Position.Six, Position.Seven, Position.Eight}; break;
		}
		int index = 0;
		for (var i = 0;i < 9;i++) {
			if (positionlist[i] == Position.NoSit) continue;
			index++;
			if (positionlist[i] == ack.RandomPos) {
				break;
			}
		}

		CardList[clickpos - 1].Init(0x20 + index);
		CardList[clickpos - 1].GetComponent<Button>().interactable = false;
		seat = ack.RandomPos;
		Invoke("EnterSeat", 1.0f);
	}
}

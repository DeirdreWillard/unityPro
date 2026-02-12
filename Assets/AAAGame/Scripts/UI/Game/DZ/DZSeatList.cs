using UnityEngine;
using System.Collections.Generic;
using NetMsg;

class DZSeatPositionInfo {
	public Vector3 dealer;
	public Vector3 chip;
	public Vector3 operation;
	public Vector3 win;
}

public class DZSeatList : MonoBehaviour
{
	DZProcedure Procedure;

	public List<DZSeatInfo> SeatList = new();

	public GameObject PrefabPlayer;

	static readonly DZSeatPositionInfo SeatD = new() {
		dealer = new Vector3(-180, -300, 0),
		chip = new Vector3(380, 0, 0),
		operation = new Vector3(180, 70, 0),
		win = new Vector3(0, 0, 0),
	};
	static readonly DZSeatPositionInfo SeatL = new () {
		dealer = new Vector3(85, -160, 0),
		chip = new Vector3(130, -50, 0),
		operation = new Vector3(130, 50, 0),
		win = new Vector3(120, 0, 0),
	};
	static readonly DZSeatPositionInfo SeatR = new () {
		dealer = new Vector3(-85, -160, 0),
		chip = new Vector3(-130, -50, 0),
		operation = new Vector3(-130, 50, 0),
		win = new Vector3(-120, 0, 0),
	};
	static readonly DZSeatPositionInfo SeatTL = new() {
		dealer = new Vector3(100, -160, 0),
		chip = new Vector3(40, -170, 0),
		operation = new Vector3(130, 50, 0),
		win = new Vector3(0, 0, 0),
	};
	static readonly DZSeatPositionInfo SeatTR = new() {
		dealer = new Vector3(-100, -160, 0),
		chip = new Vector3(-40, -170, 0),
		operation = new Vector3(-130, 50, 0),
		win = new Vector3(0, 0, 0),
	};

	readonly Dictionary<int, List<DZSeatPositionInfo>> seatinfolist = new() {
		{9, new() {SeatD, SeatL, SeatL, SeatL, SeatTL, SeatTR, SeatR, SeatR, SeatR}},
		{8, new() {SeatD, SeatL, SeatL, SeatL, SeatTL, SeatR, SeatR, SeatR}},
		{7, new() {SeatD, SeatL, SeatL, SeatL, SeatR, SeatR, SeatR}},
		{6, new() {SeatD, SeatL, SeatL, SeatTL, SeatR, SeatR}},
		{5, new() {SeatD, SeatL, SeatL, SeatR, SeatR}},
		{4, new() {SeatD, SeatL, SeatTL, SeatTR}},
		{3, new() {SeatD, SeatL, SeatR}},
		{2, new() {SeatD, SeatTL}},
	};

	void Awake() {
	}

	void OnEnable() {
		Procedure = GF.Procedure.CurrentProcedure as DZProcedure;
	}

	void OnDisable() {
	}

	void Update() {
	}

	public void EnterPlayer(DeskPlayer deskplayer) {
		DZSeatInfo seatinfo = GetPos(deskplayer.Pos);
		seatinfo.EnterPlayer(deskplayer);
	}

	public void LeavePlayer(Position position) {
		DZSeatInfo seatinfo = GetPos(position);
		seatinfo.LeavePlayer();
	}

	public DZSeatInfo GetPos(Position position) {
		foreach (DZSeatInfo seat in SeatList) {
			if (seat.position == position) {
				return seat;
			}
		}
		return null;
	}

	public DZSeatInfo GetPlayerID(long playerid) {
		foreach (DZSeatInfo seat in SeatList) {
			if (seat.PanelPlayerInfo.PlayerID == playerid) {
				return seat;
			}
		}
		return null;
	}

	public void Reset() {
		for (var i = 0;i < SeatList.Count;i++) {
			SeatList[i].Reset();
		}
	}

	public void Clear() {
		for (var i = 0;i < SeatList.Count;i++) {
			SeatList[i].position = (Position)(i + 1);
			SeatList[i].Clear();
		}
	}

	public void Refresh() {
		if (SeatList == null) return;
		for (var i = 0;i < SeatList.Count;i++) {
			if (SeatList[i] == null) continue;
			SeatList[i].Refresh();
		}
	}

	public void Init(int number, Position pos = Position.Default) {
		List<Position> positionlist = new(), reallist = new();
		switch (number) {
			case 2: positionlist = new() {Position.Default, Position.NoSit, Position.NoSit, Position.NoSit, Position.Four, Position.NoSit, Position.NoSit, Position.NoSit, Position.NoSit}; break;
			case 3: positionlist = new() {Position.Default, Position.NoSit, Position.NoSit, Position.Three, Position.NoSit, Position.NoSit, Position.Six, Position.NoSit, Position.NoSit}; break;
			case 4: positionlist = new() {Position.Default, Position.NoSit, Position.Two, Position.NoSit, Position.Four, Position.NoSit, Position.NoSit, Position.Seven, Position.NoSit}; break;
			case 5: positionlist = new() {Position.Default, Position.One, Position.Two, Position.NoSit, Position.NoSit, Position.NoSit, Position.Six, Position.Seven, Position.NoSit}; break;
			case 6: positionlist = new() {Position.Default, Position.One, Position.Two, Position.NoSit, Position.Four, Position.NoSit, Position.Six, Position.Seven, Position.NoSit}; break;
			case 7: positionlist = new() {Position.Default, Position.One, Position.Two, Position.Three, Position.NoSit, Position.NoSit, Position.Six, Position.Seven, Position.Eight}; break;
			case 8: positionlist = new() {Position.Default, Position.One, Position.Two, Position.Three, Position.Four, Position.Five, Position.Six, Position.Seven, Position.NoSit}; break;
			case 9: positionlist = new() {Position.Default, Position.One, Position.Two, Position.Three, Position.Four, Position.Five, Position.Six, Position.Seven, Position.Eight}; break;
		}
		int index = positionlist.IndexOf(pos);
		if (index != -1) {
			reallist.AddRange(positionlist.GetRange(index, positionlist.Count - index));
			reallist.AddRange(positionlist.GetRange(0, index));
		}
		else {
			reallist.AddRange(positionlist);
		}

		for (var i = 0;i < SeatList.Count;i++) {
			SeatList[i].position = reallist[i];
			if (reallist[i] == Position.NoSit) {
				SeatList[i].gameObject.SetActive(false);
			}
			else {
				SeatList[i].gameObject.SetActive(true);
				SeatList[i].transform.localPosition = GetPosition(i);
				SeatList[i].PanelPlayerInfo.GoDealer.transform.localPosition = GetDealerPosition(i);
				SeatList[i].PanelPlayerInfo.GoChip.transform.localPosition = GetChipPosition(i);
				SeatList[i].PanelPlayerInfo.GoFlyChip.transform.localPosition = GetChipPosition(i);
				SeatList[i].PanelPlayerInfo.GoPanelOperation.transform.localPosition = GetOperatorPosition(i);
				SeatList[i].PanelPlayerInfo.GoWin.transform.localPosition = GetWinPosition(i);
			}
		}
	}

	Vector3 GetPosition9(int index) {
		Rect rect = GetComponent<RectTransform>().rect;
		return index switch {
			0 => new Vector3(                    0,  420 - rect.height / 2, 0),
			1 => new Vector3( 100 - rect.width / 2,  270 - rect.height / 4, 0),
			2 => new Vector3( 100 - rect.width / 2,                    120, 0),
			3 => new Vector3( 100 - rect.width / 2, - 30 + rect.height / 4, 0),
			4 => new Vector3(     - rect.width / 6, -330 + rect.height / 2, 0),
			5 => new Vector3(       rect.width / 6, -330 + rect.height / 2, 0),
			6 => new Vector3(-100 + rect.width / 2, - 30 + rect.height / 4, 0),
			7 => new Vector3(-100 + rect.width / 2,                    120, 0),
			8 => new Vector3(-100 + rect.width / 2,  270 - rect.height / 4, 0),
			_ => Vector3.zero,
		};
	}
	Vector3 GetPosition8(int index) {
		Rect rect = GetComponent<RectTransform>().rect;
		return index switch {
			0 => new Vector3(                    0,  420 - rect.height / 2, 0),
			1 => new Vector3( 100 - rect.width / 2,  270 - rect.height / 4, 0),
			2 => new Vector3( 100 - rect.width / 2,                    120, 0),
			3 => new Vector3( 100 - rect.width / 2, - 30 + rect.height / 4, 0),
			4 => new Vector3(                    0, -330 + rect.height / 2, 0),
			5 => new Vector3(-100 + rect.width / 2, - 30 + rect.height / 4, 0),
			6 => new Vector3(-100 + rect.width / 2,                    120, 0),
			7 => new Vector3(-100 + rect.width / 2,  270 - rect.height / 4, 0),
			_ => Vector3.zero,
		};
	}
	Vector3 GetPosition7(int index) {
		Rect rect = GetComponent<RectTransform>().rect;
		return index switch {
			0 => new Vector3(                    0,  420 - rect.height / 2, 0),
			1 => new Vector3( 100 - rect.width / 2,  270 - rect.height / 4, 0),
			2 => new Vector3( 100 - rect.width / 2,                    120, 0),
			3 => new Vector3( 100 - rect.width / 2, - 30 + rect.height / 4, 0),
			4 => new Vector3(-100 + rect.width / 2, - 30 + rect.height / 4, 0),
			5 => new Vector3(-100 + rect.width / 2,                    120, 0),
			6 => new Vector3(-100 + rect.width / 2,  270 - rect.height / 4, 0),
			_ => Vector3.zero,
		};
	}
	Vector3 GetPosition6(int index) {
		Rect rect = GetComponent<RectTransform>().rect;
		return index switch {
			0 => new Vector3(                    0,  420 - rect.height / 2, 0),
			1 => new Vector3( 100 - rect.width / 2,  170 - rect.height / 6, 0),
			2 => new Vector3( 100 - rect.width / 2, - 80 + rect.height / 6, 0),
			3 => new Vector3(                    0, -330 + rect.height / 2, 0),
			4 => new Vector3(-100 + rect.width / 2, - 80 + rect.height / 6, 0),
			5 => new Vector3(-100 + rect.width / 2,  170 - rect.height / 6, 0),
			_ => Vector3.zero,
		};
	}
	Vector3 GetPosition5(int index) {
		Rect rect = GetComponent<RectTransform>().rect;
		return index switch {
			0 => new Vector3(                    0,  420 - rect.height / 2, 0),
			1 => new Vector3( 100 - rect.width / 2,  170 - rect.height / 6, 0),
			2 => new Vector3( 100 - rect.width / 2, - 80 + rect.height / 4, 0),
			3 => new Vector3(-100 + rect.width / 2, - 80 + rect.height / 4, 0),
			4 => new Vector3(-100 + rect.width / 2,  170 - rect.height / 6, 0),
			_ => Vector3.zero,
		};
	}
	Vector3 GetPosition4(int index) {
		Rect rect = GetComponent<RectTransform>().rect;
		return index switch {
			0 => new Vector3(                    0,  420 - rect.height / 2, 0),
			1 => new Vector3( 100 - rect.width / 2,                    120, 0),
			2 => new Vector3(                    0, -330 + rect.height / 2, 0),
			3 => new Vector3(-100 + rect.width / 2,                    120, 0),
			_ => Vector3.zero,
		};
	}
	Vector3 GetPosition3(int index) {
		Rect rect = GetComponent<RectTransform>().rect;
		return index switch {
			0 => new Vector3(                    0,  420 - rect.height / 2, 0),
			1 => new Vector3( 100 - rect.width / 2,                    120, 0),
			2 => new Vector3(-100 + rect.width / 2,                    120, 0),
			_ => Vector3.zero,
		};
	}
	Vector3 GetPosition2(int index) {
		Rect rect = GetComponent<RectTransform>().rect;
		return index switch {
			0 => new Vector3(                    0,  420 - rect.height / 2, 0),
			1 => new Vector3(                    0, -330 + rect.height / 2, 0),
			_ => Vector3.zero,
		};
	}
	public Vector3 GetPosition(int index) {
		DZSeatInfo seatinfo = SeatList[index];
		if (seatinfo.position == Position.NoSit) return Vector3.zero;
		int count = 0;
		for (var i = 0;i < index;i++) {
			if (SeatList[i].position != Position.NoSit) {
				count++;
			}
		}
		return Procedure.deskinfo.BaseConfig.PlayerNum switch {
			9 => GetPosition9(count),
			8 => GetPosition8(count),
			7 => GetPosition7(count),
			6 => GetPosition6(count),
			5 => GetPosition5(count),
			4 => GetPosition4(count),
			3 => GetPosition3(count),
			2 => GetPosition2(count),
			_ => Vector3.zero,
		};
	}

	public Vector3 GetDealerPosition(Position position) {
		int index = -1;
		for (var i = 0;i < SeatList.Count;i++) {
			if (SeatList[i].position == Position.NoSit) continue;
			index++;
			if (SeatList[i].position == position) {
				break;
			}
		}
		if (index == -1) return Vector3.zero;
		return GetDealerPosition(index);
	}
	public Vector3 GetDealerPosition(int index) {
		if (seatinfolist.ContainsKey(Procedure.deskinfo.BaseConfig.PlayerNum) == false) return Vector3.zero;
		if (index >= seatinfolist[Procedure.deskinfo.BaseConfig.PlayerNum].Count) return Vector3.zero;
		return seatinfolist[Procedure.deskinfo.BaseConfig.PlayerNum][index].dealer;
	}

	public Vector3 GetChipPosition(Position position) {
		int index = -1;
		for (var i = 0;i < SeatList.Count;i++) {
			if (SeatList[i].position == Position.NoSit) continue;
			index++;
			if (SeatList[i].position == position) {
				break;
			}
		}
		if (index == -1) return Vector3.zero;
		return GetChipPosition(index);
	}
	public Vector3 GetChipPosition(int index) {
		if (seatinfolist.ContainsKey(Procedure.deskinfo.BaseConfig.PlayerNum) == false) return Vector3.zero;
		if (index >= seatinfolist[Procedure.deskinfo.BaseConfig.PlayerNum].Count) return Vector3.zero;
		return seatinfolist[Procedure.deskinfo.BaseConfig.PlayerNum][index].chip;
	}

	public Vector3 GetOperatorPosition(Position position) {
		int index = -1;
		for (var i = 0;i < SeatList.Count;i++) {
			if (SeatList[i].position == Position.NoSit) continue;
			index++;
			if (SeatList[i].position == position) {
				break;
			}
		}
		if (index == -1) return Vector3.zero;
		return GetOperatorPosition(index);
	}
	public Vector3 GetOperatorPosition(int index) {
		if (seatinfolist.ContainsKey(Procedure.deskinfo.BaseConfig.PlayerNum) == false) return Vector3.zero;
		if (index >= seatinfolist[Procedure.deskinfo.BaseConfig.PlayerNum].Count) return Vector3.zero;
		return seatinfolist[Procedure.deskinfo.BaseConfig.PlayerNum][index].operation;
	}

	public Vector3 GetWinPosition(Position position) {
		int index = -1;
		for (var i = 0;i < SeatList.Count;i++) {
			if (SeatList[i].position == Position.NoSit) continue;
			index++;
			if (SeatList[i].position == position) {
				break;
			}
		}
		if (index == -1) return Vector3.zero;
		return GetWinPosition(index);
	}
	public Vector3 GetWinPosition(int index) {
		if (seatinfolist.ContainsKey(Procedure.deskinfo.BaseConfig.PlayerNum) == false) return Vector3.zero;
		if (index >= seatinfolist[Procedure.deskinfo.BaseConfig.PlayerNum].Count) return Vector3.zero;
		return seatinfolist[Procedure.deskinfo.BaseConfig.PlayerNum][index].win;
	}
}

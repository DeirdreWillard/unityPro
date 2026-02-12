using NetMsg;
using UnityEngine;

public class DZSeatInfo : MonoBehaviour
{
	DZProcedure Procedure;
	public DZGamePanel GamePanel;

	public Position position;

	public DZPlayerInfo PanelPlayerInfo;

	void OnEnable() {
		Procedure = GF.Procedure.CurrentProcedure as DZProcedure;
	}

	void OnDisable() {
		LeavePlayer();
	}

	void Update() {
	}

	public void Send_SitDown() {
		Procedure = GF.Procedure.CurrentProcedure as DZProcedure;
		Procedure.Send_SitDown(position);
	}

	public void EnterPlayer(DeskPlayer deskplayer) {
		PanelPlayerInfo.gameObject.SetActive(true);
		PanelPlayerInfo.InitPlayerInfo(deskplayer);
	}

	public void LeavePlayer() {
		PanelPlayerInfo.gameObject.SetActive(false);
		PanelPlayerInfo.Clear();
	}

	public void Refresh() {
		Procedure = GF.Procedure.CurrentProcedure as DZProcedure;
		if (Procedure == null) return;
		
		DeskPlayer deskplayer = Procedure.GetPlayerByPos(position);
		if (deskplayer == null){
			// GF.LogInfo("刷新德州座位信息", "座位为空position:" + position);
			LeavePlayer();
			return;
		}
		EnterPlayer(deskplayer);
		PanelPlayerInfo.Refresh(deskplayer);
	}

	public void Reset() {
		PanelPlayerInfo.Reset();
	}

	public void Clear() {
		PanelPlayerInfo.gameObject.SetActive(false);
		PanelPlayerInfo.Clear();
	}

	public void Action() {
	}
}

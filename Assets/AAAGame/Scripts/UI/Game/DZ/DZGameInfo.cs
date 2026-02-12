
using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using System.Collections.Generic;

public class DZGameInfo : MonoBehaviour
{
	DZProcedure Procedure;

	public Text TextCoin;
	public Text TextRoomTime;
	public Text TextRoomName;
	public Text TextRoomID;
	public Text TextMangZhu;
	public Text TextExtra;

	public DZGamePanel GamePanel;

	void OnEnable() {
		Procedure = GF.Procedure.CurrentProcedure as DZProcedure;
		TextRoomName.FormatNickname(Procedure.deskinfo.BaseConfig.DeskName);
		TextRoomID.text = $"ID:{Procedure.deskID}";
		TextMangZhu.text = $"盲注: {Util.FormatAmount(Procedure.deskinfo.BaseConfig.BaseCoin)}/{Util.FormatAmount(Procedure.deskinfo.BaseConfig.BaseCoin * 2)}  ";
		if (Procedure.deskinfo.Config.Straddle) {
			TextMangZhu.text += "Straddle";
		}
		List<string> extrainfo = new();
		if (Procedure.deskinfo.Config.Protect) extrainfo.Add("保险");
		// if (Procedure.deskinfo.Config.Critcal) extrainfo.Add("暴击");
		if (Procedure.deskinfo.Config.DelayLook) extrainfo.Add("延迟看牌");
		if (Procedure.deskinfo.BaseConfig.IpLimit && Procedure.deskinfo.BaseConfig.GpsLimit)
			extrainfo.Add("IP/GPS");
		else if (Procedure.deskinfo.BaseConfig.IpLimit)
			extrainfo.Add("IP");
		else if (Procedure.deskinfo.BaseConfig.GpsLimit)
			extrainfo.Add("GPS");
		if (Procedure.deskinfo.Config.ShowCard) extrainfo.Add("强制亮牌");
		if (Procedure.deskinfo.Config.GoldLook) extrainfo.Add("钻石看手牌");
		if (Procedure.deskinfo.Config.RandomPos) extrainfo.Add("随机入座");

		List<string> result = new();
		string currentLine = "";
		foreach (string item in extrainfo) {
			if ((currentLine.Length + item.Length + (currentLine.Length > 0 ? 1 : 0)) > 8) {
				result.Add(currentLine.Trim());
				currentLine = "";
			}
			currentLine += (currentLine.Length > 0 ? " " : "") + item;
		}
		if (!string.IsNullOrEmpty(currentLine)) {
			result.Add(currentLine.Trim());
		}
		TextExtra.text = string.Join("\n", result);
		TextCoin.text = $"底池: {Util.Sum2String( Procedure.deskinfo.CurrentPot)}";
		RefreshTimer();
	}

	void RefreshTimer() {
		int duration = (int)((GamePanel.endtime - Util.GetServerTime()) / 1000);
		if (duration < 0) duration = 0;
		int hours = Mathf.FloorToInt(duration / 3600);
		int minutes = Mathf.FloorToInt(duration % 3600 / 60);
		int seconds = Mathf.FloorToInt(duration % 60);
		TextRoomTime.text = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
		TextCoin.text = $"底池: {Util.Sum2String( Procedure.deskinfo.CurrentPot)}";
		int cduration = (int)((Procedure.deskinfo.CritTime - Util.GetServerTime()) / 1000);
		if (cduration < 0) cduration = 0;
		int cmin = Mathf.FloorToInt((cduration % 3600) / 60);
		int csec = Mathf.FloorToInt(cduration % 60);
		GamePanel.TextBaoJi.text = string.Format("{0:00};{1:00}", cmin, csec);
	}

	void Update() {
		if (Procedure.deskinfo.DeskState == DeskState.WaitStart) {
			long playertime = Procedure.deskinfo.BaseConfig.PlayerTime;
			int hours = Mathf.FloorToInt(playertime / 60);
			int minutes = Mathf.FloorToInt(playertime % 60);
			TextRoomTime.text = string.Format("{0:00}:{1:00}:00", hours, minutes);
		}
		else {
			RefreshTimer();
		}
	}
}


using UnityEngine;
using UnityEngine.UI;
using NetMsg;
using System.Collections.Generic;
using System;
using GameFramework.Event;

public class DZGameSetting : MonoBehaviour
{
	public DZProcedure Procedure;
	public DZGamePanel GamePanel;

	public Text TextCreator;
	public Text TextRemainTime;
	public Text TextCreateTime;
	public Text TextInsurance;
	public Text TextBlind;
	public Text TextStraddle;
	public Text TextBringIn;
	public Text TextLimit;
	public Slider SliderAddTime;
	public Text TextNeedMoney;
	public Text TextDiamond;
	public GameObject GoCreator;

	public float[] TimeArry=new float[] {0.5f, 1, 2, 3, 4, 5, 6};

	void OnEnable()
	{
		Procedure = GF.Procedure.CurrentProcedure as DZProcedure;
		GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
		TextCreator.FormatNickname(Procedure.deskinfo.Creator.Nick);
		TextCreateTime.text = DateTimeOffset.FromUnixTimeMilliseconds(Procedure.deskinfo.StartTime).LocalDateTime.ToString("MM-dd HH:mm");
		TextBlind.text = $"{Util.Sum2String(Procedure.deskinfo.BaseConfig.BaseCoin)}/{Util.Sum2String(Procedure.deskinfo.BaseConfig.BaseCoin * 2)}({Util.Sum2String(Procedure.deskinfo.Config.Ante)})";
		if (Procedure.deskinfo.Config.Protect) {
			TextInsurance.text = "<color=#4EC570>开启</color>";
		}
		else {
			TextInsurance.text = "<color=#C54444>关闭</color>";
		}
		if (Procedure.deskinfo.Config.Straddle) {
			TextStraddle.text = "<color=#4EC570>开启</color>";
		}
		else {
			TextStraddle.text = "<color=#C54444>关闭</color>";
		}
		TextBringIn.text = $"{Procedure.deskinfo.BaseConfig.MinBringIn}/{Procedure.deskinfo.BaseConfig.MaxBringIn}";
		if (Procedure.deskinfo.BaseConfig.IpLimit) {
			TextLimit.text = "<color=#4EC570>开启</color>";
		}
		else {
			TextLimit.text = "<color=#C54444>关闭</color>";
		}
		TextDiamond.text = GF.DataModel.GetDataModel<UserDataModel>().Diamonds.ToString();
		if (Procedure.IsRoomOwner()) {
			GoCreator.SetActive(true);
		}
		else {
			GoCreator.SetActive(false);
		}
		Invoke(nameof(OnChangeSlide), 0.1f);
	}

	void OnDisable()
	{
		GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
	}

	void Update() {
		if (Procedure != null && Procedure.deskinfo.DeskState == DeskState.Pause) return;

		int duration = (int)((GamePanel.endtime - Util.GetServerTime()) / 1000);
		if (duration < 0) duration = 0;
		int hours = Mathf.FloorToInt(duration / 3600);
		int minutes = Mathf.FloorToInt((duration % 3600) / 60);
		int seconds = Mathf.FloorToInt(duration % 60);

		TextRemainTime.text = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
	}

	public void OnChangeSlide()
	{
		int time = (int)(TimeArry[(int)SliderAddTime.value] / 0.5f);
		float baseCoin = Procedure.deskinfo.BaseConfig.BaseCoin;
		long expend = time * GlobalManager.GetInstance().GetMsg_CreateDeskConfigByType(MethodType.TexasPoker, baseCoin, Procedure.deskinfo.BaseConfig.DeskType);
		TextNeedMoney.text = expend.ToString();
	}

	private void OnUserDataChanged(object sender, GameEventArgs e) {
		var args = e as UserDataChangedEventArgs;
		switch (args.Type) {
			case UserDataType.DIAMOND:
				TextDiamond.text = Util.Sum2String(GF.DataModel.GetDataModel<UserDataModel>().Diamonds);
				break;
		}
	}
}

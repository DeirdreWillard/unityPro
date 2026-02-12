using UnityEngine;
using UnityEngine.UI;
using GameFramework.Event;
using NetMsg;

public class GameSettingNN : MonoBehaviour
{
	public Slider SD_TimeSlider;
	public float[] SD_TimeArry = new float[] { 0.5f, 1, 2, 3, 4, 5, 6 };

	public Text lb_RemianMoney;
	public Text lb_NeedMoney;

	public Text lb_RoomName;
	public Text lb_CreatorName;
	public Text lb_RoomId;
	public Text lb_BaseChip;
	public Text lb_MinTakeChip;
	public Text lb_MinTuiZhu;
	public Text lb_TuiZhuMulti;
	public Text lb_LimitZhuang;

	public GameObject Go_NNBase;

	public GameObject goCreator;

	public NNProcedure nnProcedure;

	public RectTransform rtBG;

	public string[] tesupaiMiao = { "葫芦牛*", "五花牛*", "同花牛*", "顺子牛*", "炸弹牛*", "五小牛*", "同花顺牛*" };
	public int[] tesupai1 = { 7, 5, 6, 5, 8, 10, 10 };
	public int[] tesupai2 = { 7, 5, 6, 5, 8, 10, 10 };
	public int[] tesupai3 = { 7, 5, 6, 5, 8, 10, 10 };
	public int[] tesupai4 = { 8, 6, 7, 6, 9, 10, 10 };
	public int[] tesupai5 = { 13, 11, 12, 11, 14, 15, 15 };

	public void SD_TimeSliderFunction()
	{
		int time = (int)(SD_TimeArry[(int)SD_TimeSlider.value] / 0.5f);
		float baseCoin = nnProcedure.enterNiuNiuDeskRs.BaseConfig.BaseCoin;
		long expend = time * GlobalManager.GetInstance().GetMsg_CreateDeskConfigByType(MethodType.NiuNiu, baseCoin, nnProcedure.enterNiuNiuDeskRs.BaseConfig.DeskType);
		lb_NeedMoney.text = expend.ToString();
	}

	void OnEnable()
	{
		GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
		lb_RemianMoney.text = GF.DataModel.GetDataModel<UserDataModel>().Diamonds.ToString();
		nnProcedure = GF.Procedure.CurrentProcedure as NNProcedure;
		lb_RoomName.FormatNickname(nnProcedure.enterNiuNiuDeskRs.BaseConfig.DeskName);
		lb_CreatorName.FormatNickname(nnProcedure.enterNiuNiuDeskRs.Creator.Nick);
		lb_RoomId.text = nnProcedure.deskID.ToString();
		lb_BaseChip.text = nnProcedure.enterNiuNiuDeskRs.BaseConfig.BaseCoin.ToString();
		lb_MinTakeChip.text = nnProcedure.enterNiuNiuDeskRs.BaseConfig.MinBringIn.ToString();
		lb_MinTuiZhu.text = "";
		lb_TuiZhuMulti.text = nnProcedure.enterNiuNiuDeskRs.NiuConfig.PushLimit.ToString();
		if (nnProcedure.enterNiuNiuDeskRs.NiuConfig.NoNiuRobBanker)
		{
			lb_LimitZhuang.text = "无牛禁抢庄";
		}
		else
		{
			lb_LimitZhuang.text = "无限制";
		}
		foreach (Transform item in Go_NNBase.transform)
		{
			item.gameObject.SetActive(false);
		}
		for (int i = 0; i < nnProcedure.enterNiuNiuDeskRs.NiuConfig.SpecialCard.Count; i++)
		{
			int index = nnProcedure.enterNiuNiuDeskRs.NiuConfig.SpecialCard[i];
			Go_NNBase.transform.Find(index.ToString()).gameObject.SetActive(true);

			int[] temp = { 0, 0, 0, 0, 0, 0, 0 };
			switch (nnProcedure.enterNiuNiuDeskRs.NiuConfig.DoubleRule - 1)
			{
				case 0:
					temp = tesupai1;
					break;
				case 1:
					temp = tesupai2;
					break;
				case 2:
					temp = tesupai3;
					break;
				case 3:
					temp = tesupai4;
					break;
				case 4:
					temp = tesupai5;
					break;
			}
			Go_NNBase.transform.Find(index.ToString()).GetComponent<Text>().text = tesupaiMiao[index - 1] + temp[index - 1] + "倍";

		}
		if (nnProcedure.IsRoomOwner())
		{
			goCreator.SetActive(true);
			rtBG.sizeDelta = new Vector2(rtBG.sizeDelta.x, 1150);
		}
		else
		{
			goCreator.SetActive(false);
			rtBG.sizeDelta = new Vector2(rtBG.sizeDelta.x, 750);
		}
		Invoke(nameof(SD_TimeSliderFunction), 0.1f);
	}
	void OnDisable()
	{
		GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
	}

	private void OnUserDataChanged(object sender, GameEventArgs e)
	{
		var args = e as UserDataChangedEventArgs;
		switch (args.Type)
		{
			case UserDataType.DIAMOND:
				lb_RemianMoney.text = Util.Sum2String(GF.DataModel.GetDataModel<UserDataModel>().Diamonds);
				break;
		}
	}
}

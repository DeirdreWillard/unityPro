using NetMsg;
using UnityEngine;
using UnityEngine.UI;

public class GameSettingBJ : MonoBehaviour
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
	public Text lb_RateType;

	public GameObject goCreator;

	public BJProcedure bjProcedure;

	public RectTransform rtBG;

	public void SD_TimeSliderFunction()
	{
		int time = (int)(SD_TimeArry[(int)SD_TimeSlider.value] / 0.5f);
		float baseCoin = bjProcedure.enterBJDeskRs.BaseInfo.BaseConfig.BaseCoin;
		long expend = time * GlobalManager.GetInstance().GetMsg_CreateDeskConfigByType(MethodType.CompareChicken, baseCoin, bjProcedure.enterBJDeskRs.BaseInfo.DeskType);
		lb_NeedMoney.text = expend.ToString();
	}

	void OnEnable()
	{
		bjProcedure = GF.Procedure.CurrentProcedure as BJProcedure;
		lb_RemianMoney.text = GF.DataModel.GetDataModel<UserDataModel>().Diamonds.ToString();
		lb_RoomName.text = bjProcedure.enterBJDeskRs.BaseInfo.BaseConfig.DeskName;
		lb_CreatorName.text = bjProcedure.enterBJDeskRs.BaseInfo.Creator.Nick;
		lb_RoomId.text = bjProcedure.deskID.ToString();
		lb_BaseChip.text = bjProcedure.enterBJDeskRs.BaseInfo.BaseConfig.BaseCoin.ToString();
		lb_MinTakeChip.text = bjProcedure.enterBJDeskRs.BaseInfo.BaseConfig.MinBringIn.ToString();
		// lb_RateType.text = bjProcedure.enterZjhDeskRs.Config.RateState == 0 ? "抽赢家" : "抽入池";
		if (bjProcedure.GamePanel_bj.IsRoomCreate())
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

}

using NetMsg;
using UnityEngine;
using UnityEngine.UI;

public class GameSettingZJH : MonoBehaviour
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

	public ZJHProcedure zjhProcedure;

	public RectTransform rtBG;

	public void SD_TimeSliderFunction()
	{
		int time = (int)(SD_TimeArry[(int)SD_TimeSlider.value] / 0.5f);
		float baseCoin = zjhProcedure.enterZjhDeskRs.BaseConfig.BaseCoin;
		long expend = time * GlobalManager.GetInstance().GetMsg_CreateDeskConfigByType(MethodType.GoldenFlower, baseCoin, zjhProcedure.enterZjhDeskRs.BaseConfig.DeskType);
		lb_NeedMoney.text = expend.ToString();
	}

	void OnEnable()
	{
		zjhProcedure = GF.Procedure.CurrentProcedure as ZJHProcedure;
		lb_RemianMoney.text = GF.DataModel.GetDataModel<UserDataModel>().Diamonds.ToString();
		lb_RoomName.text = zjhProcedure.enterZjhDeskRs.BaseConfig.DeskName;
		lb_CreatorName.text = zjhProcedure.enterZjhDeskRs.Creator.Nick;
		lb_RoomId.text = zjhProcedure.deskID.ToString();
		lb_BaseChip.text = zjhProcedure.enterZjhDeskRs.BaseConfig.BaseCoin.ToString();
		lb_MinTakeChip.text = zjhProcedure.enterZjhDeskRs.BaseConfig.MinBringIn.ToString();
		lb_RateType.text = zjhProcedure.enterZjhDeskRs.Config.RateState == 0 ? "抽赢家" : "抽入池";
		if (zjhProcedure.GamePanel_zjh.IsRoomCreate())
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using GameFramework.Event;
using Google.Protobuf;
using NetMsg;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using static UtilityBuiltin;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class DZGamePanel : UIFormBase
{
	DZProcedure Procedure;
	public CoroutineManager coroutinemanager;
	public RedDotItem msgRedDot;
	public GameObject jackPotObj;
	public GameObject varJackBoomAni;
	public GameObject wpkBtn;
	//顶部菜单弹出动画时间
	private readonly float intervalOpenMenu = 0.3f;
	//顶部菜单关闭动画时间
	private readonly float intervalCloseMenu = 0.1f;
	//座位移动动画时间
	private readonly float intervalSeat = 0.2f;
	//庄家移动动画时间
	private readonly float intervalDealer = 0.2f;
	//手牌移动动画事件
	private readonly float intervalHandCard = 0.4f;
	//筹码移动动画时间
	private readonly float intervalBetCoin = 0.4f;
	private readonly float intervalSettleCoin = 0.5f;
	private readonly float intervalSettle = 0.01f;

	//中间游戏信息面板
	public DZGameInfo PanelGameInfo;
	//顶部菜单
	public GameObject GoMenu;
	//座位列表
	public DZSeatList PanelSeatList;
	//带入筹码面板
	public BringUI PanelBringIn;
	//即时战绩面板
	public GameObject GoPanelRealTime;
	//牌局回顾面板
	public GameObject GoPanelReview;
	//房间设置面板
	public DZGameSetting GoPanelSetting;
	//自定义加注面板
	public DZAddCoin GoPanelAddCoin;
	//购买保险面板
	public DZInsurance GoPanelInsurance;
	//暂停面板
	public GameObject GoPanelPause;
	//房主暂停
	public GameObject GoPause1;
	//玩家暂停
	public GameObject GoPause2;
	//加时按钮
	public GameObject GoAddTime;
	public Text TextAddTime;

	//菜单货币显示
	public Text TextCoin;
	public Text TextDiamond;

	//开始游戏按钮
	public GameObject GoButtonStart;

	//发发看按钮
	public GameObject GoLook;
	public Text TextLook;

	//自己大牌
	public GameObject PanelHandCard;
	public DZCard Card1, Card2;
	public Image ImageShow1, ImageShow2;

	//操作面板
	public DZAction PanelAction;
	//操作倒计时
	public GameObject GoCancelLeave;

	//底池
	public GameObject GoPotCoin1, GoPotCoin2;
	public GameObject GoPotItem;
	public GameObject GoCurrentCoin;
	public Text TextCurrentCoin;

	//公共牌
	public List<Card> CommonCard;

	//牌型提示面板
	public GameObject GoPanelCardType;
	//保险规则面板
	public GameObject GoPanelInsuranceHelp;
	//排队面板
	public GameWaitSeat GoPanelWait;
	//随机座位面板
	public DZRandomSeat GoPanelRandomSeat;
	//浮动提示位置
	public GameObject GoPanelFloating;
	//浮动提示条目
	public GameObject GoItemFloating;
	//无座飞表情起点
	public GameObject GoButtonEmojiStart;
	//桌布
	public Image ImageBG;
	//语音
	public RecordInfo GoPanelRecord;
	//暴击面板
	public GameObject GoPanelBaoJi;
	public GameObject GoEffectBaoJi;
	public Text TextBaoJi;

	public List<Toggle> toggleTable;

	//大手牌初始位置
	public Vector3 PositionCard1, PositionCard2, LPositionCard1, LPositionCard2;
	//公共牌初始位置
	public List<Vector3> PositionCommonCard = new();

	public Dictionary<long, TexasOption> options = new();

	public long starttime = 0, nexttime = 0, endtime = 0;

	public List<int> AddCoinSelect = new();

	public readonly Dictionary<int, int> SystemConfig = new();

	public bool brecord = false;
	public bool showcard1 = false, showcard2 = false;
	public bool roundstart = false;

	protected override void OnInit(object userData)
	{
		base.OnInit(userData);
		Procedure = GF.Procedure.CurrentProcedure as DZProcedure;
		for (var i = 0; i < 5; i++)
		{
			AddCoinSelect.Add(PlayerPrefs.GetInt("DZAddCoinSelect" + (i + 1), 0));
		}
		if (AddCoinSelect[0] == 0 && AddCoinSelect[1] == 0 && AddCoinSelect[2] == 0 && AddCoinSelect[3] == 0 && AddCoinSelect[4] == 0)
		{
			AddCoinSelect[1] = 3; AddCoinSelect[2] = 5; AddCoinSelect[3] = 6;
			PlayerPrefs.SetInt($"DZAddCoinSelect2", 3);
			PlayerPrefs.SetInt($"DZAddCoinSelect3", 5);
			PlayerPrefs.SetInt($"DZAddCoinSelect4", 6);
		}
	}

	protected override void OnOpen(object userData)
	{
		base.OnOpen(userData);
		coroutinemanager.Init();
		HotfixNetworkComponent.AddListener(MessageID.SingleGameRecord, Function_SingleGameRecord);
		HotfixNetworkComponent.AddListener(MessageID.Msg_SynDeskLessFive, Function_SynNotifyDeskLessTime);
		HotfixNetworkComponent.AddListener(MessageID.Msg_BuyVipRs, Function_BuyVip);
		HotfixNetworkComponent.AddListener(MessageID.Msg_SystemConfigRs, Function_SystemConfig);
		HotfixNetworkComponent.AddListener(MessageID.Msg_EnterTexasDeskRs, Function_EnterDZDeskRs);
		GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
		GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
		GF.Event.Subscribe(GFEventArgs.EventId, OnGFEventChanged);
		GlobalManager.SendSystemConfigRq();
		RedDotManager.GetInstance().SetRedDotNodeCallBack(E_RedDotDefine.MsgBox, MsgCallBack);
		PositionCard1 = Card1.transform.position; PositionCard2 = Card2.transform.position;
		LPositionCard1 = Card1.transform.localPosition; LPositionCard2 = Card2.transform.localPosition;
		for (var i = 0; i < CommonCard.Count; i++)
		{
			PositionCommonCard.Add(CommonCard[i].transform.localPosition);
		}
		Util.UpdateTableColor(ImageBG);

		foreach (var toggle in toggleTable)
		{
			toggle.onValueChanged.AddListener(OnTableBGToggleChanged);
		}

		RefreshGame();
		brecord = false;
	}

	public void RefreshGame()
	{
		RedDotManager.GetInstance().RefreshAll();
		starttime = Procedure.deskinfo.CurrentStateTime;
		nexttime = Procedure.deskinfo.NextTime;
		endtime = Procedure.deskinfo.EndTime;
		StartCoroutine(EnterSeat(null, false));
		PanelSeatList.Init(Procedure.deskinfo.BaseConfig.PlayerNum);
		PanelSeatList.Refresh();

		DeskPlayer deskplayer = Procedure.GetPlayerByPos(Procedure.deskinfo.CurrentOption);
		if (deskplayer != null)
		{
			DZSeatInfo seatinfo = PanelSeatList.GetPos(Procedure.deskinfo.CurrentOption);
			if (seatinfo != null)
			{
				ReStartCountDown(Util.GetServerTime(), Procedure.deskinfo.NextTime, deskplayer, seatinfo);
			}
		}
		// Procedure.deskinfo.CurrentPot = "0";
		// foreach (var item in Procedure.deskinfo.DeskPlayers) {
		// 	Util.Sum2Float(Procedure.deskinfo.CurrentPot, item.BetCoin);
		// }
	}

	protected override void OnClose(bool isShutdown, object userData)
	{
		transform.DOKill();
		Card1.transform.position = PositionCard1; Card2.transform.position = PositionCard2;
		Card1.transform.localPosition = LPositionCard1; Card2.transform.localPosition = LPositionCard2;
		coroutinemanager.Uninit();
		HotfixNetworkComponent.RemoveListener(MessageID.SingleGameRecord, Function_SingleGameRecord);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynDeskLessFive, Function_SynNotifyDeskLessTime);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_BuyVipRs, Function_BuyVip);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_SystemConfigRs, Function_SystemConfig);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_EnterTexasDeskRs, Function_EnterDZDeskRs);
		GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
		GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
		GF.Event.Unsubscribe(GFEventArgs.EventId, OnGFEventChanged);
		RedDotManager.GetInstance().RemoveRedDotNodeCallBack(E_RedDotDefine.MsgBox, MsgCallBack);
		GoMenu.SetActive(false);
		PanelBringIn.Clear();
		GoPanelRealTime.SetActive(false);
		GoPanelReview.SetActive(false);
		GoPanelSetting.gameObject.SetActive(false);
		GoPanelAddCoin.gameObject.SetActive(false);
		GoPanelCardType.SetActive(false);
		GoPanelInsuranceHelp.SetActive(false);
		GoPanelPause.SetActive(false);
		GoPanelWait.gameObject.SetActive(false);

		foreach (var toggle in toggleTable)
		{
			if (toggle != null)
			{
				toggle.onValueChanged.RemoveListener(OnTableBGToggleChanged);
			}
		}

		Clear();
		base.OnClose(isShutdown, userData);
	}

	protected override async void OnButtonClick(object sender, string btId)
	{
		var uiParams = UIParams.Create();
		DeskPlayer deskplayer; DZSeatInfo seatinfo;
		DeskPlayer selfplayer = Procedure.GetSelfPlayer();
		base.OnButtonClick(sender, btId);
		switch (btId)
		{
			case "打开菜单":
				if (GoMenu.activeSelf)
				{
					GoMenu.transform.DOLocalMoveX(-500, intervalCloseMenu).OnComplete(() =>
					{
						GoMenu.SetActive(false);
					});
				}
				else
				{
					GoMenu.SetActive(true);
					GoMenu.transform.localPosition = new Vector3(-500, GoMenu.transform.localPosition.y, GoMenu.transform.localPosition.z);
					GoMenu.transform.DOLocalMoveX(0, intervalOpenMenu);
				}
				break;
			case "关闭菜单":
				GoMenu.transform.DOLocalMoveX(-500, intervalCloseMenu).OnComplete(() =>
				{
					GoMenu.SetActive(false);
				});
				break;
			case "站起围观":
				Procedure.Send_SitUpRq();
				break;
			case "退出牌局":
				Procedure.Send_LeaveDesk();
				break;
			case "开始游戏":
				Procedure.Send_StartGame();
				break;
			case "带入筹码":
				if (selfplayer != null)
				{
					BringIn(false);
				}
				break;
			case "牌型提示":
				GoPanelCardType.transform.localScale = Vector3.zero;
				GoPanelCardType.SetActive(true);
				GoPanelCardType.transform.DOScale(Vector3.one, intervalOpenMenu);
				break;
			case "关闭牌型提示":
				GoPanelCardType.transform.localScale = Vector3.one;
				GoPanelCardType.transform.DOScale(Vector3.zero, intervalCloseMenu).OnComplete(() =>
				{
					GoPanelCardType.SetActive(false);
				});
				break;
			case "保险规则":
				GoPanelInsuranceHelp.transform.localScale = Vector3.zero;
				GoPanelInsuranceHelp.SetActive(true);
				GoPanelInsuranceHelp.transform.DOScale(Vector3.one, intervalOpenMenu);
				break;
			case "关闭保险规则":
				GoPanelInsuranceHelp.transform.localScale = Vector3.one;
				GoPanelInsuranceHelp.transform.DOScale(Vector3.zero, intervalCloseMenu).OnComplete(() =>
				{
					GoPanelInsuranceHelp.SetActive(false);
				});
				break;
			case "牌局回顾":
				if (GoPanelReview.activeSelf)
				{
					GoPanelReview.transform.GetComponent<RectTransform>().DOAnchorPosX(GoPanelReview.transform.Find("Panel").GetComponent<RectTransform>().rect.width, intervalCloseMenu).OnComplete(() =>
					{
						GoPanelReview.SetActive(false);
					});
				}
				else
				{
					RectTransform _rev = GoPanelReview.transform.GetComponent<RectTransform>();
					_rev.anchoredPosition = new Vector2(_rev.transform.Find("Panel").GetComponent<RectTransform>().rect.width, _rev.anchoredPosition.y);
					GoPanelReview.SetActive(true);
					_rev.DOAnchorPosX(0, intervalOpenMenu);
				}
				break;
			case "即时战绩":
				if (GoPanelRealTime.activeSelf)
				{
					GoPanelRealTime.transform.GetComponent<RectTransform>().DOAnchorPosX(0 - GoPanelRealTime.transform.Find("Panel").GetComponent<RectTransform>().rect.width, intervalCloseMenu).OnComplete(() =>
			  {
				  GoPanelRealTime.SetActive(false);
			  });
				}
				else
				{
					RectTransform realTim_rct = GoPanelRealTime.transform.GetComponent<RectTransform>();
					realTim_rct.anchoredPosition = new Vector2(0 - realTim_rct.transform.Find("Panel").GetComponent<RectTransform>().rect.width, realTim_rct.anchoredPosition.y);
					GoPanelRealTime.SetActive(true);
					realTim_rct.DOAnchorPosX(0, intervalOpenMenu);
				}
				break;
			case "牌桌设置":
				int index = SettingExtension.GetTableBG();
				for (var i = 0; i < toggleTable.Count; i++)
				{
					toggleTable[i].isOn = i == index;
				}
				GoPanelSetting.transform.localScale = Vector3.zero;
				GoPanelSetting.gameObject.SetActive(true);
				GoPanelSetting.transform.DOScale(Vector3.one, intervalOpenMenu);
				break;
			case "关闭设置":
				GoPanelSetting.transform.localScale = Vector3.one;
				GoPanelSetting.transform.DOScale(Vector3.zero, intervalCloseMenu).OnComplete(() =>
				{
					GoPanelSetting.gameObject.SetActive(false);
				});
				break;
			case "解散房间":
				Util.GetInstance().OpenConfirmationDialog("解散房间", "确定要解散房间?", () =>
					{
						GF.LogInfo("解散房间");
						Procedure.Send_DismissDesk();
					});
				break;
			case "黑名单":
				GF.LogInfo("黑名单");
				await GF.UI.OpenUIFormAwait(UIViews.GameBlackPlayerPanel);
				break;
			case "暂停房间":
				Procedure.Send_PauseDesk(1);
				break;
			case "恢复房间":
				Procedure.Send_PauseDesk(0);
				break;
			case "购买时长":
				float fen = GoPanelSetting.TimeArry[(int)GoPanelSetting.SliderAddTime.value] * 60;
				Procedure.Send_DeskContinuedTimeRq((int)fen);
				break;
			case "提前结算":
				Procedure.Send_AdvanceSettle();
				break;
			case "游戏商城":
				await GF.UI.OpenUIFormAwait(UIViews.ShopPanel);
				break;
			case "自定义加注":
				GoMenu.SetActive(false);
				GoPanelAddCoin.transform.localScale = Vector3.zero;
				GoPanelAddCoin.gameObject.SetActive(true);
				GoPanelAddCoin.transform.DOScale(Vector3.one, intervalOpenMenu);
				break;
			case "关闭自定义加注":
				GoPanelAddCoin.transform.localScale = Vector3.one;
				GoPanelAddCoin.transform.DOScale(Vector3.zero, intervalCloseMenu).OnComplete(() =>
				{
					GoPanelAddCoin.gameObject.SetActive(false);
					PanelAction.Refresh();
				});
				break;
			case "文字聊天":
				await GF.UI.OpenUIFormAwait(UIViews.ChatPanel);
				break;
			case "等待队列":
				GoPanelWait.gameObject.SetActive(true);
				break;
			case "关闭队列":
				GoPanelWait.gameObject.SetActive(false);
				break;
			case "加入队列":
				if (Util.GetMyselfInfo().VipState == 0)
				{
					GF.UI.ShowToast("VIP专享功能");
					return;
				}
				if (Procedure.deskinfo.DeskPlayers.Count < Procedure.deskinfo.BaseConfig.PlayerNum)
				{
					GF.UI.ShowToast("还有空位");
					return;
				}
				else if (selfplayer == null)
				{
					if (GoPanelWait.waitPlayers.Any(basePlayer => basePlayer.PlayerId == Util.GetMyselfInfo().PlayerId))
					{
						GF.UI.ShowToast("已经在等待队列");
						return;
					}
					else
					{
						Msg_LineUpRq req = MessagePool.Instance.Fetch<Msg_LineUpRq>();
						req.State = 1;
						HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_LineUpRq, req);
					}
				}
				else if (selfplayer.Pos != Position.NoSit)
				{
					GF.UI.ShowToast("已经在座位上");
					return;
				}
				else
				{
					Msg_LineUpRq req = MessagePool.Instance.Fetch<Msg_LineUpRq>();
					req.State = 1;
					HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_LineUpRq, req);
				}
				break;
			case "离开队列":
				Msg_LineUpRq req1 = MessagePool.Instance.Fetch<Msg_LineUpRq>();
				req1.State = 0;
				HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_LineUpRq, req1);
				break;
			case "消息记录":
				await GF.UI.OpenUIFormAwait(UIViews.MessagePanel);
				GF.UI.CloseUIForms(UIViews.BringTips);
				break;
			case "座位1":
				seatinfo = PanelSeatList.SeatList[0];
				deskplayer = Procedure.GetPlayerByPos(seatinfo.position);
				if (deskplayer == null)
				{
					if (Procedure.deskinfo.Config.RandomPos == true)
					{
						if (selfplayer == null)
						{
							GoPanelRandomSeat.gameObject.SetActive(true);
						}
						else
						{
							Procedure.Send_RandomSeat();
						}
					}
					else
					{
						PanelSeatList.SeatList[0].Send_SitDown();
					}
				}
				else
				{
					uiParams.Set<VarBoolean>("isRoomCreate", Procedure.IsRoomOwner());
					uiParams.Set<VarByteArray>("deskPlayer", deskplayer.ToByteArray());
					await GF.UI.OpenUIFormAwait(UIViews.playerInforPanel, uiParams);
				}
				break;
			case "座位2":
				seatinfo = PanelSeatList.SeatList[1];
				deskplayer = Procedure.GetPlayerByPos(seatinfo.position);
				if (deskplayer == null)
				{
					if (Procedure.deskinfo.Config.RandomPos == true)
					{
						if (selfplayer == null)
						{
							GoPanelRandomSeat.gameObject.SetActive(true);
						}
						else
						{
							Procedure.Send_RandomSeat();
						}
					}
					else
					{
						PanelSeatList.SeatList[1].Send_SitDown();
					}
				}
				else
				{
					uiParams.Set<VarBoolean>("isRoomCreate", Procedure.IsRoomOwner());
					uiParams.Set<VarByteArray>("deskPlayer", deskplayer.ToByteArray());
					await GF.UI.OpenUIFormAwait(UIViews.playerInforPanel, uiParams);
				}
				break;
			case "座位3":
				seatinfo = PanelSeatList.SeatList[2];
				deskplayer = Procedure.GetPlayerByPos(seatinfo.position);
				if (deskplayer == null)
				{
					if (Procedure.deskinfo.Config.RandomPos == true)
					{
						if (selfplayer == null)
						{
							GoPanelRandomSeat.gameObject.SetActive(true);
						}
						else
						{
							Procedure.Send_RandomSeat();
						}
					}
					else
					{
						PanelSeatList.SeatList[2].Send_SitDown();
					}
				}
				else
				{
					uiParams.Set<VarBoolean>("isRoomCreate", Procedure.IsRoomOwner());
					uiParams.Set<VarByteArray>("deskPlayer", deskplayer.ToByteArray());
					await GF.UI.OpenUIFormAwait(UIViews.playerInforPanel, uiParams);
				}
				break;
			case "座位4":
				seatinfo = PanelSeatList.SeatList[3];
				deskplayer = Procedure.GetPlayerByPos(seatinfo.position);
				if (deskplayer == null)
				{
					if (Procedure.deskinfo.Config.RandomPos == true)
					{
						if (selfplayer == null)
						{
							GoPanelRandomSeat.gameObject.SetActive(true);
						}
						else
						{
							Procedure.Send_RandomSeat();
						}
						return;
					}
					else
					{
						PanelSeatList.SeatList[3].Send_SitDown();
					}
				}
				else
				{
					uiParams.Set<VarBoolean>("isRoomCreate", Procedure.IsRoomOwner());
					uiParams.Set<VarByteArray>("deskPlayer", deskplayer.ToByteArray());
					await GF.UI.OpenUIFormAwait(UIViews.playerInforPanel, uiParams);
				}
				break;
			case "座位5":
				seatinfo = PanelSeatList.SeatList[4];
				deskplayer = Procedure.GetPlayerByPos(seatinfo.position);
				if (deskplayer == null)
				{
					if (Procedure.deskinfo.Config.RandomPos == true)
					{
						if (selfplayer == null)
						{
							GoPanelRandomSeat.gameObject.SetActive(true);
						}
						else
						{
							Procedure.Send_RandomSeat();
						}
					}
					else
					{
						PanelSeatList.SeatList[4].Send_SitDown();
					}
				}
				else
				{
					uiParams.Set<VarBoolean>("isRoomCreate", Procedure.IsRoomOwner());
					uiParams.Set<VarByteArray>("deskPlayer", deskplayer.ToByteArray());
					await GF.UI.OpenUIFormAwait(UIViews.playerInforPanel, uiParams);
				}
				break;
			case "座位6":
				seatinfo = PanelSeatList.SeatList[5];
				deskplayer = Procedure.GetPlayerByPos(seatinfo.position);
				if (deskplayer == null)
				{
					if (Procedure.deskinfo.Config.RandomPos == true)
					{
						if (selfplayer == null)
						{
							GoPanelRandomSeat.gameObject.SetActive(true);
						}
						else
						{
							Procedure.Send_RandomSeat();
						}
					}
					else
					{
						PanelSeatList.SeatList[5].Send_SitDown();
					}
				}
				else
				{
					uiParams.Set<VarBoolean>("isRoomCreate", Procedure.IsRoomOwner());
					uiParams.Set<VarByteArray>("deskPlayer", deskplayer.ToByteArray());
					await GF.UI.OpenUIFormAwait(UIViews.playerInforPanel, uiParams);
				}
				break;
			case "座位7":
				seatinfo = PanelSeatList.SeatList[6];
				deskplayer = Procedure.GetPlayerByPos(seatinfo.position);
				if (deskplayer == null)
				{
					if (Procedure.deskinfo.Config.RandomPos == true)
					{
						if (selfplayer == null)
						{
							GoPanelRandomSeat.gameObject.SetActive(true);
						}
						else
						{
							Procedure.Send_RandomSeat();
						}
						return;
					}
					else
					{
						PanelSeatList.SeatList[6].Send_SitDown();
					}
				}
				else
				{
					uiParams.Set<VarBoolean>("isRoomCreate", Procedure.IsRoomOwner());
					uiParams.Set<VarByteArray>("deskPlayer", deskplayer.ToByteArray());
					await GF.UI.OpenUIFormAwait(UIViews.playerInforPanel, uiParams);
				}
				break;
			case "座位8":
				seatinfo = PanelSeatList.SeatList[7];
				deskplayer = Procedure.GetPlayerByPos(seatinfo.position);
				if (deskplayer == null)
				{
					if (Procedure.deskinfo.Config.RandomPos == true)
					{
						if (selfplayer == null)
						{
							GoPanelRandomSeat.gameObject.SetActive(true);
						}
						else
						{
							Procedure.Send_RandomSeat();
						}
					}
					else
					{
						PanelSeatList.SeatList[7].Send_SitDown();
					}
				}
				else
				{
					uiParams.Set<VarBoolean>("isRoomCreate", Procedure.IsRoomOwner());
					uiParams.Set<VarByteArray>("deskPlayer", deskplayer.ToByteArray());
					await GF.UI.OpenUIFormAwait(UIViews.playerInforPanel, uiParams);
				}
				break;
			case "座位9":
				seatinfo = PanelSeatList.SeatList[8];
				deskplayer = Procedure.GetPlayerByPos(seatinfo.position);
				if (deskplayer == null)
				{
					if (Procedure.deskinfo.Config.RandomPos == true)
					{
						if (selfplayer == null)
						{
							GoPanelRandomSeat.gameObject.SetActive(true);
						}
						else
						{
							Procedure.Send_RandomSeat();
						}
						return;
					}
					else
					{
						PanelSeatList.SeatList[8].Send_SitDown();
					}
				}
				else
				{
					uiParams.Set<VarBoolean>("isRoomCreate", Procedure.IsRoomOwner());
					uiParams.Set<VarByteArray>("deskPlayer", deskplayer.ToByteArray());
					await GF.UI.OpenUIFormAwait(UIViews.playerInforPanel, uiParams);
				}
				break;
			case "盖牌":
				if (float.TryParse(Procedure.deskinfo.FollowCoin, out float followcoin) == true)
				{
					if (followcoin == 0)
					{
						Util.GetInstance().OpenConfirmationDialog("盖牌", "当前可以看牌，确定要盖牌吗?", () =>
						{
							Procedure.Send_FoldCard();
						}, () =>
						{
							Procedure.Send_CallCard();
						});
					}
					else
					{
						Procedure.Send_FoldCard();
					}
				}
				else
				{
					Procedure.Send_FoldCard();
				}
				break;
			case "跟注":
				Procedure.Send_CallCard();
				break;
			case "加注1":
				float addcoin0 = float.TryParse(PanelAction.GoAdd[0].transform.Find("Chip").GetComponent<Text>().text, out addcoin0) ? addcoin0 : float.Parse(selfplayer.Coin);
				if (addcoin0 >= float.Parse(selfplayer.Coin))
				{
					addcoin0 = float.Parse(selfplayer.Coin);
				}
				Procedure.Send_AddCoin(addcoin0);
				break;
			case "加注2":
				float addcoin1 = float.TryParse(PanelAction.GoAdd[1].transform.Find("Chip").GetComponent<Text>().text, out addcoin1) ? addcoin1 : float.Parse(selfplayer.Coin);
				if (addcoin1 >= float.Parse(selfplayer.Coin))
				{
					addcoin1 = float.Parse(selfplayer.Coin);
				}
				Procedure.Send_AddCoin(addcoin1);
				break;
			case "加注3":
				float addcoin2 = float.TryParse(PanelAction.GoAdd[2].transform.Find("Chip").GetComponent<Text>().text, out addcoin2) ? addcoin2 : float.Parse(selfplayer.Coin);
				if (addcoin2 >= float.Parse(selfplayer.Coin))
				{
					addcoin2 = float.Parse(selfplayer.Coin);
				}
				Procedure.Send_AddCoin(addcoin2);
				break;
			case "加注4":
				float addcoin3 = float.TryParse(PanelAction.GoAdd[3].transform.Find("Chip").GetComponent<Text>().text, out addcoin3) ? addcoin3 : float.Parse(selfplayer.Coin);
				if (addcoin3 >= float.Parse(selfplayer.Coin))
				{
					addcoin3 = float.Parse(selfplayer.Coin);
				}
				Procedure.Send_AddCoin(addcoin3);
				break;
			case "加注5":
				float addcoin4 = float.TryParse(PanelAction.GoAdd[4].transform.Find("Chip").GetComponent<Text>().text, out addcoin4) ? addcoin4 : float.Parse(selfplayer.Coin);
				if (addcoin4 >= float.Parse(selfplayer.Coin))
				{
					addcoin4 = float.Parse(selfplayer.Coin);
				}
				Procedure.Send_AddCoin(addcoin4);
				break;
			case "重回座位":
				Procedure.Send_CancelLeave();
				break;
			case "加时":
				Procedure.Send_AddTime();
				break;
			case "购买保险保底":
				Procedure.Send_BuyInsurance(GoPanelInsurance.basecoin, GoPanelInsurance.selectlist);
				break;
			case "购买保险全买":
				Procedure.Send_BuyInsurance(GoPanelInsurance.maxcoin, GoPanelInsurance.selectlist);
				break;
			case "购买保险最低":
				Procedure.Send_BuyInsurance(GoPanelInsurance.mincoin, GoPanelInsurance.selectlist);
				break;
			case "购买保险确认":
				Procedure.Send_BuyInsurance(GoPanelInsurance.SliderBuy.value / 100f, GoPanelInsurance.selectlist);
				break;
			case "购买保险关闭":
				GoPanelInsurance.binit = false;
				GoPanelInsurance.gameObject.SetActive(false);
				break;
			case "购买保险放弃":
				Procedure.Send_BuyInsurance(0, new List<int>());
				GoPanelInsurance.binit = false;
				GoPanelInsurance.gameObject.SetActive(false);
				break;
			case "看牌1":
				showcard1 = !showcard1;
				ImageShow1.gameObject.SetActive(showcard1);
				Procedure.Send_ShowCard(showcard1 ? selfplayer.HandCard[0] : -1, showcard2 ? selfplayer.HandCard[1] : -1);
				break;
			case "看牌2":
				showcard2 = !showcard2;
				ImageShow2.gameObject.SetActive(showcard2);
				Procedure.Send_ShowCard(showcard1 ? selfplayer.HandCard[0] : -1, showcard2 ? selfplayer.HandCard[1] : -1);
				break;
			case "发发看":
				Procedure.Send_LookCommonCard();
				break;
			case "关闭随机座位":
				GoPanelRandomSeat.gameObject.SetActive(false);
				break;
			case "打开奖池":
				uiParams.Set<VarInt32>("methodType", (int)MethodType.TexasPoker);
				uiParams.Set<VarByteArray>("deskConfig", Procedure.deskinfo.BaseConfig.ToByteArray());
				await GF.UI.OpenUIFormAwait(UIViews.GameJackpotPanel, uiParams);
				break;
			case "wpk":
				uiParams.Set<VarByteArray>("deskConfig", Procedure.deskinfo.BaseConfig.ToByteArray());
				await GF.UI.OpenUIFormAwait(UIViews.GameWPKPanel, uiParams);
				break;
			default:
				break;
		}
	}

	private void OnTableBGToggleChanged(bool isOn)
	{
		if (isOn)
		{
			for (int i = 0; i < toggleTable.Count; i++)
			{
				if (toggleTable[i].isOn)
				{
					SettingExtension.SetTableBG(i);
					Util.UpdateTableColor(ImageBG);
					break;
				}
			}
		}
	}


	public void OnClubDataChanged(object sender, GameEventArgs e)
	{
		var args = e as ClubDataChangedEventArgs;
		switch (args.Type)
		{
			case ClubDataType.eve_LeagueJackpotUpdate:
				UpdateJackpot();
				break;
		}
	}

	public void UpdateJackpot()
	{
		if (jackPotObj.activeSelf == false) return;
		GlobalManager.GetInstance().GetJackpotData(Procedure.deskinfo.BaseConfig.ClubId, out float total);
		double form = jackPotObj.GetComponent<JumpingNumberTextComponent>().number;
		jackPotObj.GetComponent<JumpingNumberTextComponent>().Change(form, total);
	}

	void MsgCallBack(RedDotNode node)
	{
		if (gameObject == null) return;
		msgRedDot.SetDotState(node.rdCount > 0, node.rdCount);
	}

	private void Function_SystemConfig(MessageRecvData data)
	{
		Msg_SystemConfigRs ack = Msg_SystemConfigRs.Parser.ParseFrom(data.Data);
		SystemConfig.Clear();
		foreach (var pair in ack.Config)
		{
			SystemConfig.Add(pair.Key, pair.Val);
		}
		RefreshAddTime();
		TextLook.text = SystemConfig[17].ToString();
		if (GoPanelInsurance.gameObject.activeSelf == true)
		{
			GoPanelInsurance.Refresh();
		}
	}

	public void Function_SynNotifyDeskLessTime(MessageRecvData data)
	{
		Msg_SynDeskLessFive ack = Msg_SynDeskLessFive.Parser.ParseFrom(data.Data);
		GF.UI.ShowToast("5分钟后将解散房间");
	}

	public void Function_BuyVip(MessageRecvData data)
	{
		GF.LogInfo("购买VIP");
		GF.UI.ShowToast("购买Vip成功", 2);
	}

	public void Function_SingleGameRecord(MessageRecvData data)
	{
		GF.LogInfo("收到单局战绩");
		brecord = true;
	}

	private void OnGFEventChanged(object sender, GameEventArgs e)
	{
		var args = e as GFEventArgs;
		switch (args.EventType)
		{
			case GFEventType.eve_SynDeskChat:
				var t = args.UserData as Msg_DeskChat;
				string chat;
				switch (t.Type)
				{
					case 0:
						chat = $"{t.Sender.Nick}: {t.Chat}";
						ShowFloating(chat);
						break;
					case 1:
						if (Util.IsMySelf(t.Sender.PlayerId) == true)
						{
							GF.UI.CloseUIForms(UIViews.ChatPanel);
						}
						chat = $"{t.Sender.Nick}: {t.Chat}";
						ShowFloating(chat);
						break;
					case 2:
						if (Util.IsMySelf(t.Sender.PlayerId) == true)
						{
							GF.UI.CloseUIForms(UIViews.ChatPanel);
						}
						ShowEmoji(t.Sender, t.Chat);
						break;
					case 3:
						CoroutineRunner.Instance.StartCoroutine(DownloadAndPlayMp3(t.Sender.PlayerId, t.Voice));
						break;
				}
				break;
			case GFEventType.eve_ReConnectGame:
				if (Util.GetMyselfInfo().DeskId != 0 && Util.GetMyselfInfo().DeskId == Procedure.deskID)
				{
					Util.GetInstance().Send_EnterDeskRq(Procedure.deskID);
				}
				else
				{
					GF.LogInfo("返回登录界面");
					HomeProcedure homeProcedure = GF.Procedure.GetProcedure<HomeProcedure>() as HomeProcedure;
					homeProcedure.QuitGame();
				}
				break;
		}
	}

	private void OnUserDataChanged(object sender, GameEventArgs e)
	{
		var args = e as UserDataChangedEventArgs;
		switch (args.Type)
		{
			case UserDataType.MONEY:
				GF.LogInfo("收到 金币变动事件.", Util.GetMyselfInfo().Gold);
				TextCoin.text = Util.Sum2String(Util.GetMyselfInfo().Gold);
				break;
			case UserDataType.DIAMOND:
				GF.LogInfo("收到 钻石变动事件.", Util.GetMyselfInfo().Diamonds.ToString());
				TextDiamond.text = Util.Sum2String(Util.GetMyselfInfo().Diamonds);
				break;
			case UserDataType.VipState:
				DeskPlayer deskplayer = Procedure.GetPlayerByPos(Position.Default);
				if (deskplayer == null) return;
				if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId) == true)
				{
					DZSeatInfo seatinfo = PanelSeatList.GetPos(Position.Default);
					seatinfo.PanelPlayerInfo.GoVip.SetActive(Util.GetMyselfInfo().VipState == 1);
				}
				break;
		}
	}

	bool _isMoveSeatAnim = false;

	//收到坐下
	public IEnumerator EnterSeat(DeskPlayer deskplayer, bool bringin)
	{
		if (deskplayer != null)
		{
			foreach (DeskPlayer dp in Procedure.deskinfo.DeskPlayers)
			{
				if (dp.Pos == deskplayer.Pos)
				{
					Procedure.deskinfo.DeskPlayers.Remove(dp);
					break;
				}
			}
			Procedure.deskinfo.DeskPlayers.Add(deskplayer);
			PanelSeatList.EnterPlayer(deskplayer);
			if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId) == false)
			{
				GoPanelRandomSeat.Refresh();
			}
			if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId) == false)
			{
				PanelSeatList.GetPos(deskplayer.Pos).Refresh();
				yield break;
			}
		}
		else
		{
			foreach (DeskPlayer dp in Procedure.deskinfo.DeskPlayers)
			{
				PanelSeatList.EnterPlayer(dp);
			}
			PanelAction.AutoCall = PanelAction.AutoFold = false;
		}

		DeskPlayer selfplayer = Procedure.GetSelfPlayer();
		if (selfplayer != null)
		{
			if (PanelSeatList.SeatList[0].position != selfplayer.Pos && !_isMoveSeatAnim)
			{
				_isMoveSeatAnim = true;
				PanelAction.gameObject.SetActive(false);
				Sequence seq1 = DOTween.Sequence();
				seq1.SetTarget(transform);
				foreach (DZSeatInfo si in PanelSeatList.SeatList)
				{
					seq1.Join(si.transform.DOMove(Vector3.zero, intervalSeat));
				}
				yield return seq1.WaitForCompletion();

				foreach (DZSeatInfo si in PanelSeatList.SeatList)
				{
					PanelSeatList.LeavePlayer(si.position);
					si.Reset();
				}

				PanelSeatList.Init(Procedure.deskinfo.BaseConfig.PlayerNum, selfplayer.Pos);

				foreach (DeskPlayer dp in Procedure.deskinfo.DeskPlayers)
				{
					PanelSeatList.EnterPlayer(dp);
				}

				Sequence seq3 = DOTween.Sequence();
				seq3.SetTarget(transform);
				for (var i = 0; i < PanelSeatList.SeatList.Count; i++)
				{
					DZSeatInfo seatinfo = PanelSeatList.SeatList[i];
					seq3.Join(seatinfo.transform.DOLocalMove(PanelSeatList.GetPosition(i), intervalSeat));
				}
				yield return seq3.WaitForCompletion();
				_isMoveSeatAnim = false;
			}
			if (deskplayer != null && Util.IsMySelf(deskplayer.BasePlayer.PlayerId) == false)
			{
				GoPanelRandomSeat.Refresh();
			}

			if (bringin == true)
			{
				BringIn(bringin);
			}
		}
		foreach (DeskPlayer dp in Procedure.deskinfo.DeskPlayers)
		{
			if (dp.TexasOption == (int)TexasOption.Allin)
			{
				DZSeatInfo seatinfo = PanelSeatList.GetPos(dp.Pos);
				if (seatinfo != null)
				{
					seatinfo.PanelPlayerInfo.IsAllIn = true;
				}
			}
		}

		if (deskplayer == null || (selfplayer != null && selfplayer.InGame == false))
		{
			options.Clear();
			foreach (var pair in Procedure.deskinfo.RoundOption)
			{
				options.Add(pair.Key, (TexasOption)pair.Val);
			}
			if (deskplayer == null)
			{
				if (Procedure.deskinfo.TexasState == TexasState.TurnProtect || Procedure.deskinfo.TexasState == TexasState.RiverProtect)
				{
					GoPanelInsurance.Init(Procedure.deskinfo.ProtectInfo);
				}
			}
			roundstart = true;
			Refresh();
		}
	}

	//收到站起
	public void LeaveSeat(Position position)
	{
		DeskPlayer deskplayer = Procedure.GetPlayerByPos(position);
		for (var i = 0; i < Procedure.deskinfo.DeskPlayers.Count; i++)
		{
			if (Procedure.deskinfo.DeskPlayers[i].Pos == position)
			{
				Procedure.deskinfo.DeskPlayers.RemoveAt(i);
				break;
			}
		}
		PanelSeatList.LeavePlayer(position);
		if (deskplayer != null)
		{
			if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId))
			{
				Refresh();
			}
		}
		GoPanelRandomSeat.Refresh();
	}

	//收到开始玩家列表
	public IEnumerator DeskStart(Msg_SynDeskStart ack)
	{
		roundstart = false;
		GoLook.SetActive(false);
		Procedure.deskinfo.TexasState = TexasState.PreFlop;
		PanelAction.gameObject.SetActive(false);
		showcard1 = showcard2 = false;
		ImageShow1.gameObject.SetActive(false); ImageShow2.gameObject.SetActive(false);
		foreach (DeskPlayer deskplayer in Procedure.deskinfo.DeskPlayers)
		{
			deskplayer.InGame = false;
			deskplayer.HandCard.Clear();
			deskplayer.BetCoin = "";
			deskplayer.IsBanker = false;
			deskplayer.IsGive = false;
			deskplayer.TexasOption = 0;
			deskplayer.BuyTime = 0;
			deskplayer.IsShow = false;
		}
		foreach (Msg_InDesk indesk in ack.InDesk)
		{
			DeskPlayer deskplayer = Procedure.GetPlayerByPos(indesk.Pos);
			if (deskplayer == null) continue;
			deskplayer.InGame = true;
		}
		Procedure.deskinfo.DeskState = DeskState.StartRun;
		Procedure.deskinfo.Pot = Procedure.deskinfo.CurrentPot = "0";
		Procedure.deskinfo.EdgePot.Clear();
		Procedure.deskinfo.Banker = Procedure.deskinfo.SmallBlind = Procedure.deskinfo.BigBlind = Procedure.deskinfo.StrongBlind = Position.NoSit;
		foreach (DZSeatInfo seatinfo in PanelSeatList.SeatList)
		{
			seatinfo.PanelPlayerInfo.CardType = null;
			seatinfo.PanelPlayerInfo.Profit = null;
			seatinfo.PanelPlayerInfo.IsBanker = seatinfo.PanelPlayerInfo.IsSB = seatinfo.PanelPlayerInfo.IsBB = seatinfo.PanelPlayerInfo.IsStrongBlind = false;
			seatinfo.PanelPlayerInfo.IsWin = false; seatinfo.PanelPlayerInfo.IsAllIn = false;
		}
		options.Clear();
		Procedure.deskinfo.CommonCards.Clear();
		Procedure.deskinfo.EndTime = ack.DeskEndTime;
		endtime = ack.DeskEndTime;
		ClearCard();
		Refresh();
		yield return 0;
	}

	//收到牌局开始
	public IEnumerator GameStart(Syn_TexasStart ack)
	{
		Reset();
		Procedure.deskinfo.Crit = ack.Crit;
		Procedure.deskinfo.CritTime = ack.CritTime;
		if (Procedure.deskinfo.Config.Critcal == true && Procedure.deskinfo.Crit == true)
		{
			Sequence seqbaoji = DOTween.Sequence();
			seqbaoji.SetTarget(transform);
			GoEffectBaoJi.SetActive(true);
			_ = GF.Sound.PlayEffect("Comming.mp3");
			seqbaoji.AppendInterval(3.5f);
			seqbaoji.AppendCallback(() =>
			{
				GoEffectBaoJi.SetActive(false);
			});
			yield return seqbaoji.WaitForCompletion();
		}
		for (var i = 0; i < 5; i++)
		{
			CommonCard[i].Init(-1);
		}
		GoLook.SetActive(false);
		Procedure.deskinfo.TexasState = TexasState.PreFlop;
		GoButtonStart.SetActive(false);
		PanelAction.gameObject.SetActive(false);
		Procedure.deskinfo.TexasState = ack.TexasState;
		Procedure.deskinfo.Banker = ack.Banker;
		Procedure.deskinfo.SmallBlind = ack.SmallBlind;
		Procedure.deskinfo.BigBlind = ack.BigBlind;
		Procedure.deskinfo.StrongBlind = ack.StrongBlind;
		GoCurrentCoin.SetActive(true);
		DZSeatInfo seatinfo = PanelSeatList.GetPos(ack.Banker);
		if (seatinfo == null) yield break;
		seatinfo.PanelPlayerInfo.GoDealer.SetActive(true);
		seatinfo.PanelPlayerInfo.GoDealer.transform.position = Vector3.zero;
		seatinfo.PanelPlayerInfo.IsBanker = true;
		seatinfo.PanelPlayerInfo.GoPanelOperation.transform.localPosition = PanelSeatList.GetOperatorPosition(seatinfo.position);
		seatinfo.PanelPlayerInfo.GoChip.transform.localPosition = PanelSeatList.GetChipPosition(seatinfo.position);
		seatinfo.PanelPlayerInfo.GoWin.transform.localPosition = PanelSeatList.GetWinPosition(seatinfo.position);
		yield return seatinfo.PanelPlayerInfo.GoDealer.transform.DOLocalMove(PanelSeatList.GetDealerPosition(seatinfo.position), intervalDealer).WaitForCompletion();
		float bet = 0f;
		Sequence seq = DOTween.Sequence();
		seq.SetTarget(transform);
		seatinfo = PanelSeatList.GetPos(ack.SmallBlind);
		DeskPlayer deskplayer = Procedure.GetPlayerByPos(ack.SmallBlind);
		if (seatinfo == null || deskplayer == null) yield break;
		seatinfo.PanelPlayerInfo.IsSB = true;
		seatinfo.PanelPlayerInfo.GoFlyChip.SetActive(true);
		seatinfo.PanelPlayerInfo.GoFlyChip.transform.localPosition = Vector3.zero;
		foreach (Msg_Ante ante in ack.Ante)
		{
			if (ante.Pos == ack.SmallBlind)
			{
				bet = ante.Ante;
			}
		}
		seatinfo.PanelPlayerInfo.TextFlyChip.text = Util.Sum2String(bet);
		deskplayer.TexasOption = (int)TexasOption.Blind;
		options.Add(deskplayer.BasePlayer.PlayerId, TexasOption.Blind);
		_ = GF.Sound.PlayEffect("xiazhu.mp3");
		seq.Join(seatinfo.PanelPlayerInfo.GoFlyChip.transform.DOLocalMove(PanelSeatList.GetChipPosition(seatinfo.position), intervalBetCoin));
		deskplayer.BetCoin = bet.ToString("F2");
		Procedure.deskinfo.CurrentPot = Util.Sum2String(Procedure.deskinfo.CurrentPot, bet);

		seatinfo = PanelSeatList.GetPos(ack.BigBlind);
		deskplayer = Procedure.GetPlayerByPos(ack.BigBlind);
		if (seatinfo == null || deskplayer == null) yield break;
		seatinfo.PanelPlayerInfo.IsBB = true;
		seatinfo.PanelPlayerInfo.GoFlyChip.SetActive(true);
		seatinfo.PanelPlayerInfo.GoFlyChip.transform.localPosition = Vector3.zero;
		foreach (Msg_Ante ante in ack.Ante)
		{
			if (ante.Pos == ack.BigBlind)
			{
				bet = ante.Ante;
			}
		}
		seatinfo.PanelPlayerInfo.TextFlyChip.text = Util.Sum2String(bet);
		deskplayer.TexasOption = (int)TexasOption.Blind;
		options.Add(deskplayer.BasePlayer.PlayerId, TexasOption.Blind);
		_ = GF.Sound.PlayEffect("xiazhu.mp3");
		seq.Join(seatinfo.PanelPlayerInfo.GoFlyChip.transform.DOLocalMove(PanelSeatList.GetChipPosition(seatinfo.position), intervalBetCoin));
		deskplayer.BetCoin = bet.ToString("F2");
		Procedure.deskinfo.CurrentPot = Util.Sum2String(Procedure.deskinfo.CurrentPot, bet);

		seatinfo = PanelSeatList.GetPos(ack.StrongBlind);
		deskplayer = Procedure.GetPlayerByPos(ack.StrongBlind);
		if (seatinfo != null && deskplayer != null)
		{
			seatinfo.PanelPlayerInfo.IsStrongBlind = true;
			seatinfo.PanelPlayerInfo.GoFlyChip.SetActive(true);
			seatinfo.PanelPlayerInfo.GoFlyChip.transform.localPosition = Vector3.zero;
			foreach (Msg_Ante ante in ack.Ante)
			{
				if (ante.Pos == ack.StrongBlind)
				{
					bet = ante.Ante;
				}
			}
			seatinfo.PanelPlayerInfo.TextFlyChip.text = Util.Sum2String(bet);
			deskplayer.TexasOption = (int)TexasOption.Blind;
			if (options.ContainsKey(deskplayer.BasePlayer.PlayerId) == false)
			{
				options.Add(deskplayer.BasePlayer.PlayerId, TexasOption.Blind);
			}
			_ = GF.Sound.PlayEffect("xiazhu.mp3");
			seq.Join(seatinfo.PanelPlayerInfo.GoFlyChip.transform.DOLocalMove(PanelSeatList.GetChipPosition(seatinfo.position), intervalBetCoin));
			deskplayer.BetCoin = Util.FormatAmount(bet);
			Procedure.deskinfo.CurrentPot = Util.Sum2String(Procedure.deskinfo.CurrentPot, bet);
		}
		PanelAction.AutoCall = PanelAction.AutoFold = false;

		foreach (Msg_Ante ante in ack.Ante)
		{
			GF.LogInfo($"ante.Pos:{ante.Pos} ante: {ante.Ante}");
			if (ante.Pos == ack.SmallBlind) continue;
			if (ante.Pos == ack.BigBlind) continue;
			if (ante.Pos == ack.StrongBlind) continue;
			seatinfo = PanelSeatList.GetPos(ante.Pos);
			deskplayer = Procedure.GetPlayerByPos(ante.Pos);
			if (seatinfo == null || deskplayer == null) continue;
			seatinfo.PanelPlayerInfo.GoFlyChip.SetActive(true);
			seatinfo.PanelPlayerInfo.GoFlyChip.transform.localPosition = Vector3.zero;
			seatinfo.PanelPlayerInfo.TextFlyChip.text = Util.Sum2String(ante.Ante);
			deskplayer.TexasOption = (int)TexasOption.Blind;
			if (options.ContainsKey(deskplayer.BasePlayer.PlayerId) == false)
			{
				options.Add(deskplayer.BasePlayer.PlayerId, TexasOption.Blind);
			}
			_ = GF.Sound.PlayEffect("xiazhu.mp3");
			seq.Join(seatinfo.PanelPlayerInfo.GoFlyChip.transform.DOLocalMove(PanelSeatList.GetChipPosition(ante.Pos), intervalBetCoin));
			deskplayer.BetCoin = ante.Ante.ToString("F2");
			Procedure.deskinfo.CurrentPot = Util.Sum2String(Procedure.deskinfo.CurrentPot, ante.Ante);
		}
		yield return seq.WaitForCompletion();
	}

	//收到牌局发牌
	public IEnumerator DealCard(Syn_DealCard ack)
	{
		Procedure.deskinfo.TexasState = TexasState.PreFlop;
		roundstart = false;
		Sequence localCardSequence = DOTween.Sequence();
		localCardSequence.SetTarget(transform);
		Color color = Color.white; color.a = 1;
		foreach (DealCard dealcard in ack.Deal)
		{
			DeskPlayer deskplayer = Procedure.GetPlayerByPos(dealcard.Pos);
			if (deskplayer == null) continue;
			deskplayer.InGame = true;
			DZSeatInfo seatinfo = PanelSeatList.GetPos(dealcard.Pos);
			if (seatinfo == null) continue;
			seatinfo.PanelPlayerInfo.Card1.Init(-1); seatinfo.PanelPlayerInfo.Card1.gameObject.SetActive(true); seatinfo.PanelPlayerInfo.Card1.transform.position = Vector3.zero; seatinfo.PanelPlayerInfo.Card1.transform.localScale = Vector3.one;
			seatinfo.PanelPlayerInfo.Card2.Init(-1); seatinfo.PanelPlayerInfo.Card2.gameObject.SetActive(true); seatinfo.PanelPlayerInfo.Card2.transform.position = Vector3.zero; seatinfo.PanelPlayerInfo.Card2.transform.localScale = Vector3.one;
			seatinfo.PanelPlayerInfo.Card1.GetComponent<Image>().color = color;
			seatinfo.PanelPlayerInfo.Card2.GetComponent<Image>().color = color;
			if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId) == true)
			{
				localCardSequence.Join(seatinfo.PanelPlayerInfo.Card1.transform.DOMove(PositionCard1, intervalHandCard));
				localCardSequence.Join(seatinfo.PanelPlayerInfo.Card2.transform.DOMove(PositionCard2, intervalHandCard));
				localCardSequence.Join(seatinfo.PanelPlayerInfo.Card1.transform.DOScale(new Vector3(1.4f, 1.4f, 1.4f), intervalHandCard));
				localCardSequence.Join(seatinfo.PanelPlayerInfo.Card2.transform.DOScale(new Vector3(1.4f, 1.4f, 1.4f), intervalHandCard));
			}
			else
			{
				localCardSequence.Join(seatinfo.PanelPlayerInfo.Card1.transform.DOLocalMove(seatinfo.PanelPlayerInfo.PositionCard1, intervalHandCard));
				localCardSequence.Join(seatinfo.PanelPlayerInfo.Card2.transform.DOLocalMove(seatinfo.PanelPlayerInfo.PositionCard2, intervalHandCard));
			}
			deskplayer.HandCard.Clear();
			deskplayer.HandCard.AddRange(dealcard.HandCards);
		}
		localCardSequence.AppendCallback(() =>
		{
			foreach (DealCard dealcard in ack.Deal)
			{
				DeskPlayer deskplayer = Procedure.GetPlayerByPos(dealcard.Pos);
				if (deskplayer == null) continue;
				DZSeatInfo seatinfo = PanelSeatList.GetPos(dealcard.Pos);
				if (seatinfo == null) continue;
				seatinfo.PanelPlayerInfo.Card1.gameObject.SetActive(false);
				seatinfo.PanelPlayerInfo.Card2.gameObject.SetActive(false);
			}
			_ = GF.Sound.PlayEffect("qipai.mp3");
			Refresh();
		});
		yield return localCardSequence.WaitForCompletion();
	}

	//收到玩家操作
	public IEnumerator TexasOptionInfo(Syn_TexasOptionInfo ack)
	{
		if (ack.Option == TexasOption.Show) yield break;
		PanelAction.Refresh(false);
		Procedure.deskinfo.CurrentOption = Position.NoSit;
		DeskPlayer deskplayer = Procedure.GetPlayerByPos(ack.CurrentOption);
		if (deskplayer == null) yield break;
		DZSeatInfo seatinfo = PanelSeatList.GetPos(ack.CurrentOption);
		if (seatinfo == null) yield break;
		if (Procedure.GetSelfPlayer() == null)
		{
			if (options.ContainsKey(deskplayer.BasePlayer.PlayerId) == true)
			{
				List<long> remove = new();
				foreach (var pair in options)
				{
					if (pair.Value == TexasOption.Give) continue;
					remove.Add(pair.Key);
				}
				foreach (long key in remove)
				{
					options.Remove(key);
				}
			}
			options.Add(deskplayer.BasePlayer.PlayerId, ack.Option);
		}
		else
		{
			if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId) == true)
			{
				if (options.ContainsKey(deskplayer.BasePlayer.PlayerId) == true)
				{
					options.Clear();
				}
				options.Add(deskplayer.BasePlayer.PlayerId, ack.Option);
				PanelAction.AutoCall = PanelAction.AutoFold = false;
			}
			else
			{
				if (options.ContainsKey(deskplayer.BasePlayer.PlayerId) == true)
				{
					options.Remove(deskplayer.BasePlayer.PlayerId);
				}
				options.Add(deskplayer.BasePlayer.PlayerId, ack.Option);
			}
		}
		seatinfo.PanelPlayerInfo.ShowAction(ack.Option, Util.Sum2String(ack.Param));

		float.TryParse(deskplayer.BetCoin, out float betcoin);
		switch (ack.Option)
		{
			case TexasOption.Give:
				_ = GF.Sound.PlayEffect("qipai.mp3");
				deskplayer.IsGive = true;
				break;
			case TexasOption.Look:
				_ = GF.Sound.PlayEffect("pcheck.ogg");
				break;
			case TexasOption.Follow:
				seatinfo.PanelPlayerInfo.GoFlyChip.SetActive(true);
				seatinfo.PanelPlayerInfo.GoFlyChip.transform.localPosition = Vector3.zero;
				seatinfo.PanelPlayerInfo.TextFlyChip.text = Util.Sum2String(ack.Param);
				deskplayer.TexasOption = (int)ack.Option;
				_ = GF.Sound.PlayEffect("xiazhu.mp3");
				yield return seatinfo.PanelPlayerInfo.GoFlyChip.transform.DOLocalMove(PanelSeatList.GetChipPosition(seatinfo.position), intervalBetCoin).WaitForCompletion();
				betcoin += ack.Param;
				deskplayer.BetCoin = Util.Sum2String(betcoin);
				Procedure.deskinfo.CurrentPot = Util.Sum2String(Procedure.deskinfo.CurrentPot, ack.Param);
				break;
			case TexasOption.AddCoin:
				seatinfo.PanelPlayerInfo.GoFlyChip.SetActive(true);
				seatinfo.PanelPlayerInfo.GoFlyChip.transform.localPosition = Vector3.zero;
				seatinfo.PanelPlayerInfo.TextFlyChip.text = Util.Sum2String(ack.Param);
				deskplayer.TexasOption = (int)ack.Option;
				_ = GF.Sound.PlayEffect("xiazhu.mp3");
				yield return seatinfo.PanelPlayerInfo.GoFlyChip.transform.DOLocalMove(PanelSeatList.GetChipPosition(seatinfo.position), intervalBetCoin).WaitForCompletion();
				seatinfo.PanelPlayerInfo.GoFlyChip.SetActive(false);
				betcoin += ack.Param;
				deskplayer.BetCoin = Util.Sum2String(betcoin);
				Procedure.deskinfo.CurrentPot = Util.Sum2String(Procedure.deskinfo.CurrentPot, ack.Param);
				break;
			case TexasOption.Allin:
				seatinfo.PanelPlayerInfo.IsAllIn = true;
				seatinfo.PanelPlayerInfo.GoFlyChip.SetActive(true);
				seatinfo.PanelPlayerInfo.GoFlyChip.transform.localPosition = Vector3.zero;
				seatinfo.PanelPlayerInfo.TextFlyChip.text = Util.Sum2String(ack.Param);
				deskplayer.TexasOption = (int)ack.Option;
				_ = GF.Sound.PlayEffect("xiazhu.mp3");
				yield return seatinfo.PanelPlayerInfo.GoFlyChip.transform.DOLocalMove(PanelSeatList.GetChipPosition(seatinfo.position), intervalBetCoin).WaitForCompletion();
				seatinfo.PanelPlayerInfo.GoFlyChip.SetActive(false);
				betcoin += ack.Param;
				deskplayer.BetCoin = Util.Sum2String(betcoin);
				Procedure.deskinfo.CurrentPot = Util.Sum2String(Procedure.deskinfo.CurrentPot, ack.Param);
				break;
			case TexasOption.Blind:
				break;
		}

		yield return 0;
		Refresh();
		PanelAction.Refresh(false);
	}

	//收到更改操作对象
	public IEnumerator TexasChangeOption(Syn_TexasChangeOption ack)
	{
		Procedure.deskinfo.CurrentOption = ack.CurrentOption;
		Procedure.deskinfo.NextTime = ack.NextTime;
		Procedure.deskinfo.FollowCoin = ack.FollowCoin;
		Procedure.deskinfo.CallCoin = ack.CallCoin;
		Procedure.deskinfo.SomeCallCoin = ack.SomeCallCoin;
		Procedure.deskinfo.Option.Clear();
		Procedure.deskinfo.Option.AddRange(ack.Option);
		DeskPlayer deskplayer = Procedure.GetPlayerByPos(ack.CurrentOption);
		if (deskplayer == null) yield break;
		DZSeatInfo seatinfo = PanelSeatList.GetPos(ack.CurrentOption);
		if (seatinfo == null) yield break;
		if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId) == true)
		{
			options.Remove(deskplayer.BasePlayer.PlayerId);
		}
		yield return 0;
		if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId) == true)
		{
			_ = GF.Sound.PlayEffect("action.mp3");
			float.TryParse(Procedure.deskinfo.FollowCoin, out float followcoin);
			if ((PanelAction.AutoFold && followcoin >= 0) || (PanelAction.AutoCall && followcoin == 0))
			{
				coroutinemanager.AddCoroutine(AutoAction());
			}
			else
			{
				PanelAction.AutoCall = PanelAction.AutoFold = false;
			}
		}
		ReStartCountDown(Util.GetServerTime(), ack.NextTime, deskplayer, seatinfo);
		roundstart = true;
		Refresh();
	}

	public void ReStartCountDown(long starttime, long nexttime, DeskPlayer deskplayer, DZSeatInfo seatinfo)
	{
		// GF.LogError($"ReStartCountDown starttime:{starttime} nexttime:{nexttime}");
		this.starttime = starttime;
		this.nexttime = nexttime;
		if (nexttime - starttime != 0)
		{
			int totalSeconds = (int)((nexttime - starttime) / 1000);
			// GF.LogError($"nexttime:{nexttime} starttime:{starttime} totalSeconds:{totalSeconds}");
			if (totalSeconds > 0)
			{
				if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId) == true)
				{
					PanelAction.EffectFold.gameObject.SetActive(true);
					PanelAction.EffectCall.gameObject.SetActive(true);
					PanelAction.EffectFold.ResetAndStartCountDown(totalSeconds, totalSeconds, false);
					PanelAction.EffectCall.ResetAndStartCountDown(totalSeconds, totalSeconds, false);
				}
				else
				{
					seatinfo.PanelPlayerInfo.EffectTimer.gameObject.SetActive(true);
					seatinfo.PanelPlayerInfo.EffectTimer.ResetAndStartCountDown(totalSeconds, totalSeconds, false);
				}
			}
			else
			{
				if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId) == true)
				{
					PanelAction.EffectFold.Pause();
					PanelAction.EffectCall.Pause();
					PanelAction.EffectFold.gameObject.SetActive(false);
					PanelAction.EffectCall.gameObject.SetActive(false);
				}
				else
				{
					seatinfo.PanelPlayerInfo.EffectTimer.Pause();
					seatinfo.PanelPlayerInfo.EffectTimer.gameObject.SetActive(false);
				}
			}
		}
		GoPanelInsurance.SliderCountdown.maxValue = (int)((nexttime - starttime) / 1000);
	}
	//收到公共牌
	public IEnumerator DealCommonCard(Syn_DealCommonCard ack)
	{
		roundstart = false;
		Procedure.deskinfo.CurrentOption = Position.NoSit;
		yield return new WaitForSeconds(1);
		options.Clear();
		Refresh();
		PanelAction.Refresh(false);
		Sequence dealCardSequence = DOTween.Sequence();
		dealCardSequence.SetTarget(transform);
		Color color = Color.white; color.a = 1;
		switch (ack.TexasState)
		{
			case TexasState.Flop:
				CommonCard[0].transform.localPosition = PositionCommonCard[0]; CommonCard[0].gameObject.SetActive(true); CommonCard[0].Init(-1);
				CommonCard[1].transform.localPosition = PositionCommonCard[1]; CommonCard[1].gameObject.SetActive(false); CommonCard[1].Init(-1);
				CommonCard[2].transform.localPosition = PositionCommonCard[2]; CommonCard[2].gameObject.SetActive(false); CommonCard[2].Init(-1);
				CommonCard[0].GetComponent<Image>().color = color;
				CommonCard[1].GetComponent<Image>().color = color;
				CommonCard[2].GetComponent<Image>().color = color;
				dealCardSequence.Append(CommonCard[0].transform.DORotate(new Vector3(0, 90, 0), intervalHandCard));
				dealCardSequence.AppendCallback(() =>
				{
					CommonCard[0].Init(ack.CommonCard[0]);
				});
				dealCardSequence.Append(CommonCard[0].transform.DORotate(new Vector3(0, 0, 0), intervalHandCard));
				dealCardSequence.AppendCallback(() =>
				{
					CommonCard[1].Init(ack.CommonCard[1]); CommonCard[1].gameObject.SetActive(true); CommonCard[1].transform.localPosition = PositionCommonCard[0];
					CommonCard[2].Init(ack.CommonCard[2]); CommonCard[2].gameObject.SetActive(true); CommonCard[2].transform.localPosition = PositionCommonCard[0];
				});
				dealCardSequence.Join(CommonCard[1].transform.DOLocalMove(PositionCommonCard[1], intervalHandCard));
				dealCardSequence.Join(CommonCard[2].transform.DOLocalMove(PositionCommonCard[2], intervalHandCard));
				break;
			case TexasState.Turn:
				GoPanelInsurance.gameObject.SetActive(false);
				CommonCard[3].transform.position = GoCurrentCoin.transform.position; CommonCard[3].gameObject.SetActive(true); CommonCard[3].Init(-1);
				CommonCard[3].GetComponent<Image>().color = color;
				dealCardSequence.Join(CommonCard[3].transform.DOLocalMove(PositionCommonCard[3], intervalHandCard));
				dealCardSequence.Join(CommonCard[3].transform.DOLocalRotate(new Vector3(0, 0, 720), intervalHandCard, RotateMode.FastBeyond360));
				dealCardSequence.Append(CommonCard[3].transform.DORotate(new Vector3(0, 90, 0), intervalHandCard));
				dealCardSequence.AppendCallback(() =>
				{
					CommonCard[3].Init(ack.CommonCard[0]);
				});
				dealCardSequence.Append(CommonCard[3].transform.DORotate(new Vector3(0, 0, 0), intervalHandCard));
				break;
			case TexasState.River:
				GoPanelInsurance.gameObject.SetActive(false);
				CommonCard[4].transform.position = GoCurrentCoin.transform.position; CommonCard[4].gameObject.SetActive(true); CommonCard[4].Init(-1);
				CommonCard[4].GetComponent<Image>().color = color;
				dealCardSequence.Join(CommonCard[4].transform.DOLocalMove(PositionCommonCard[4], intervalHandCard));
				dealCardSequence.Join(CommonCard[4].transform.DOLocalRotate(new Vector3(0, 0, 720), intervalHandCard, RotateMode.FastBeyond360));
				dealCardSequence.Append(CommonCard[4].transform.DORotate(new Vector3(0, 90, 0), intervalHandCard));
				dealCardSequence.AppendCallback(() =>
				{
					CommonCard[4].Init(ack.CommonCard[0]);
				});
				dealCardSequence.Append(CommonCard[4].transform.DORotate(new Vector3(0, 0, 0), intervalHandCard));
				break;
		}
		dealCardSequence.AppendCallback(() =>
				{
					_ = GF.Sound.PlayEffect("qipai.mp3");
					Procedure.deskinfo.TexasState = ack.TexasState;
					GoPanelInsurance.gameObject.SetActive(false);

					// 根据不同的德州扑克阶段，正确添加公共牌
					switch (ack.TexasState)
					{
						case TexasState.Flop: // 翻牌阶段 - 发3张公共牌
											  // 清空已有公共牌并添加新的3张翻牌
							Procedure.deskinfo.CommonCards.Clear();
							Procedure.deskinfo.CommonCards.AddRange(ack.CommonCard);
							break;

						case TexasState.Turn: // 转牌阶段 - 发第4张公共牌
											  // 确保已有3张翻牌，然后添加转牌
							if (Procedure.deskinfo.CommonCards.Count == 3)
							{
								Procedure.deskinfo.CommonCards.Add(ack.CommonCard[0]);
							}
							else if (Procedure.deskinfo.CommonCards.Count > 3) // 如果已经有超过3张牌，更新第4张
							{
								// 保留前3张牌，删除多余的牌
								while (Procedure.deskinfo.CommonCards.Count > 3)
								{
									Procedure.deskinfo.CommonCards.RemoveAt(3);
								}
								// 添加新的转牌
								Procedure.deskinfo.CommonCards.Add(ack.CommonCard[0]);
							}
							else // 如果牌数错误，重置并添加所有牌
							{
								GF.LogError("公共牌", "转牌阶段公共牌数量错误");
							}
							break;

						case TexasState.River: // 河牌阶段 - 发第5张公共牌
											   // 确保已有4张牌(3张翻牌+1张转牌)，然后添加河牌
							if (Procedure.deskinfo.CommonCards.Count == 4)
							{
								Procedure.deskinfo.CommonCards.Add(ack.CommonCard[0]);
							}
							else if (Procedure.deskinfo.CommonCards.Count > 4) // 如果已经有超过4张牌，更新第5张
							{
								// 保留前4张牌，删除多余的牌
								while (Procedure.deskinfo.CommonCards.Count > 4)
								{
									Procedure.deskinfo.CommonCards.RemoveAt(4);
								}
								// 添加新的河牌
								Procedure.deskinfo.CommonCards.Add(ack.CommonCard[0]);
							}
							else // 如果牌数错误，记录错误
							{
								GF.LogError("公共牌", "河牌阶段公共牌数量错误");
							}
							break;
					}

					Refresh();
					PanelAction.Refresh(false);
				});
		yield return dealCardSequence.WaitForCompletion();
	}

	//玩家筹码变更
	public void SynPlayerInfo(DeskPlayer deskplayer)
	{
		DeskPlayer dp = Procedure.GetPlayerByID(deskplayer.BasePlayer.PlayerId);
		if (dp == null) return;
		dp.Coin = deskplayer.Coin;
		dp.ClubId = deskplayer.ClubId;
		dp.BuyTime = deskplayer.BuyTime;
		dp.IsShow = deskplayer.IsShow;
		dp.State = deskplayer.State;
		dp.LeaveTime = deskplayer.LeaveTime;
		dp.BringNum = deskplayer.BringNum;
		DZSeatInfo seatinfo = PanelSeatList.GetPos(dp.Pos);
		if (seatinfo == null) return;
		seatinfo.PanelPlayerInfo.TextScore.text = Util.Sum2String(deskplayer.Coin);
		seatinfo.PanelPlayerInfo.SetLeaveState(deskplayer);
		RefreshAddTime();
	}

	//收到下注
	public void BetCoin(Syn_BetCoin ack)
	{
		if (ack.BetCoin == "" || ack.BetCoin == "0.00") return;
		DeskPlayer deskplayer = Procedure.GetPlayerByPos(ack.Pos);
		if (deskplayer == null) return;
		DZSeatInfo seatinfo = PanelSeatList.GetPos(ack.Pos);
		if (seatinfo == null) return;
		seatinfo.PanelPlayerInfo.GoFlyChip.SetActive(true);
		seatinfo.PanelPlayerInfo.GoFlyChip.transform.localPosition = Vector3.zero;
		seatinfo.PanelPlayerInfo.TextFlyChip.text = Util.Sum2String(ack.BetCoin);
		deskplayer.TexasOption = (int)TexasOption.Blind;
		options.Add(deskplayer.BasePlayer.PlayerId, TexasOption.Blind);
		_ = GF.Sound.PlayEffect("xiazhu.mp3");
		seatinfo.PanelPlayerInfo.GoFlyChip.transform.DOLocalMove(PanelSeatList.GetChipPosition(seatinfo.position), intervalBetCoin).OnComplete(() =>
		{
			deskplayer.BetCoin = ack.BetCoin.ToString();
			Procedure.deskinfo.CurrentPot = Util.Sum2String(Procedure.deskinfo.CurrentPot, ack.BetCoin);
		});
	}

	//收到结算
	public IEnumerator TexasSettle(Msg_SynTexasSettle ack)
	{
		roundstart = false;
		options.Clear();
		Procedure.deskinfo.TexasState = TexasState.TexasSettle;
		foreach (Msg_ComposeCard card in ack.ComposeCard)
		{
			DeskPlayer deskplayer = Procedure.GetPlayerByID(card.PlayerId);
			if (deskplayer == null) continue;
			deskplayer.HandCard.Clear();
			deskplayer.HandCard.AddRange(card.HandCards);
			DZSeatInfo seatinfo = PanelSeatList.GetPos(deskplayer.Pos);
			if (seatinfo == null) continue;
			seatinfo.PanelPlayerInfo.Card1.Init(deskplayer.HandCard[0]);
			seatinfo.PanelPlayerInfo.Card2.Init(deskplayer.HandCard[1]);
			seatinfo.PanelPlayerInfo.CardType = card.CardType;
			GF.LogInfo("结算. ", $"玩家:{deskplayer.BasePlayer.PlayerId} 手牌: {card.HandCards} 牌型:{card.CardType} 金币:{deskplayer.Coin}");
		}
		DeskPlayer selfplayer = Procedure.GetSelfPlayer();
		if (selfplayer != null && selfplayer.InGame == true && selfplayer.IsGive == false)
		{
			DZProcedure.TexasCardInfo cardinfo = Procedure.GetCardType(selfplayer.HandCard.Concat(Procedure.deskinfo.CommonCards).ToList());
			if (cardinfo.realcard.Contains(selfplayer.HandCard[0]) && selfplayer.HandCard[0] != -1)
			{
				Card1.GetComponent<Image>().color = Color.white;
				Card1.transform.localPosition = new Vector3(LPositionCard1.x, LPositionCard1.y + 20, LPositionCard1.z);
				Card1.transform.Find("Image").gameObject.SetActive(true);
			}
			else
			{
				Card1.GetComponent<Image>().color = Color.gray;
			}
			if (cardinfo.realcard.Contains(selfplayer.HandCard[1]) && selfplayer.HandCard[0] != -1)
			{
				Card2.GetComponent<Image>().color = Color.white;
				Card2.transform.localPosition = new Vector3(LPositionCard2.x, LPositionCard2.y + 20, LPositionCard2.z);
				Card2.transform.Find("Image").gameObject.SetActive(true);
			}
			else
			{
				Card2.GetComponent<Image>().color = Color.gray;
			}
			for (var i = 0; i < Procedure.deskinfo.CommonCards.Count; i++)
			{
				if (cardinfo.realcard.Contains(Procedure.deskinfo.CommonCards[i]))
				{
					CommonCard[i].GetComponent<Image>().color = Color.white;
					CommonCard[i].transform.localPosition = new Vector3(PositionCommonCard[i].x, PositionCommonCard[i].y + 20, PositionCommonCard[i].z);
					CommonCard[i].transform.Find("Image").gameObject.SetActive(true);
				}
				else
				{
					CommonCard[i].GetComponent<Image>().color = Color.gray;
				}
			}
		}
		yield return new WaitForSeconds(1);
		Procedure.deskinfo.Pot = Procedure.deskinfo.CurrentPot = "0";
		TextCurrentCoin.text = "0";
		_ = GF.Sound.PlayEffect("niuniu/win.mp3");
		for (int k = 0; k < 10; k++)
		{
			for (var i = 0; i < ack.Settle.ToList().Count; i++)
			{
				Msg_TexasSettle settle = ack.Settle[i];
				GF.LogInfo("池子.", $"序号:{i} 金额:{settle.Pot} 玩家数量:{settle.Settle.ToList().Count}");
				foreach (Msg_PlayerSettle player in settle.Settle)
				{
					DeskPlayer deskplayer = Procedure.GetPlayerByID(player.PlayerId);
					if (deskplayer == null) continue;
					DZSeatInfo seatinfo = PanelSeatList.GetPos(deskplayer.Pos);
					if (seatinfo == null) continue;
					GF.LogInfo("池子.", $"序号:{i} 玩家:{player.PlayerId} 金额:{player.Coin}");
					GameObject go;
					if (i == 0)
					{
						go = Instantiate(GoPotItem, TextCurrentCoin.transform);
						go.transform.position = TextCurrentCoin.transform.position;
					}
					else if (i < 4)
					{
						go = Instantiate(GoPotItem, TextCurrentCoin.transform);
						go.transform.position = GoPotCoin1.transform.GetChild(i - 1).transform.position;
					}
					else
					{
						go = Instantiate(GoPotItem, TextCurrentCoin.transform);
						go.transform.position = GoPotCoin2.transform.GetChild(i - 4).transform.position;
					}
					go.SetActive(true);
					go.transform.Find("Value").gameObject.SetActive(false);
					// go.GetComponentInChildren<Text>().text = player.Coin;
					go.transform.localScale = Vector3.one;
					go.transform.DOMove(seatinfo.transform.position, intervalSettleCoin);
				}
			}
			yield return new WaitForSeconds(intervalSettle);
		}
		Function_SynCKJackPot();
		yield return new WaitForSeconds(intervalSettleCoin);
		//避免协程没清 运行残留
		if (Procedure.deskinfo.Pot != "0") yield break;
		for (var i = 0; i < ack.Settle.ToList().Count; i++)
		{
			Msg_TexasSettle settle = ack.Settle[i];
			GF.LogInfo("池子. ", $"序号:{i} 金额:{settle.Pot} 玩家数量:{settle.Settle.ToList().Count}");
			foreach (Msg_PlayerSettle player in settle.Settle)
			{
				DeskPlayer deskplayer = Procedure.GetPlayerByID(player.PlayerId);
				if (deskplayer == null) continue;
				DZSeatInfo seatinfo = PanelSeatList.GetPos(deskplayer.Pos);
				if (seatinfo == null) continue;
			}
		}
		for (var i = 0; i < GoPotCoin1.transform.childCount; i++)
		{
			Destroy(GoPotCoin1.transform.GetChild(i).gameObject);
		}
		for (var i = 0; i < GoPotCoin2.transform.childCount; i++)
		{
			Destroy(GoPotCoin2.transform.GetChild(i).gameObject);
		}
		for (var i = 0; i < TextCurrentCoin.transform.childCount; i++)
		{
			Destroy(TextCurrentCoin.transform.GetChild(i).gameObject);
		}
		Procedure.deskinfo.CurrentPot = "底池: 0.00";
		Procedure.deskinfo.EdgePot.Clear();

		foreach (DeskPlayer deskplayer in Procedure.deskinfo.DeskPlayers)
		{
			if (deskplayer == null) continue;
			deskplayer.BetCoin = "0.00";
			DZSeatInfo seatinfo = PanelSeatList.GetPos(deskplayer.Pos);
			if (seatinfo == null) continue;
			seatinfo.PanelPlayerInfo.Profit = null;
		}
		foreach (Msg_PlayerSettle settle in ack.RealSettle)
		{
			DeskPlayer deskplayer = Procedure.GetPlayerByID(settle.PlayerId);
			if (deskplayer == null) continue;
			DZSeatInfo seatinfo = PanelSeatList.GetPos(deskplayer.Pos);
			if (seatinfo == null) continue;
			seatinfo.PanelPlayerInfo.Profit = float.Parse(settle.Coin);
			GF.LogInfo("收益. ", $"{deskplayer.BasePlayer.PlayerId} 当前金币:{deskplayer.Coin} 本局收益:{settle.Coin}");
		}
		foreach (long playerid in ack.MaxWinner)
		{
			DZSeatInfo seatinfo = PanelSeatList.GetPlayerID(playerid);
			if (seatinfo == null) continue;
			seatinfo.PanelPlayerInfo.IsWin = true;
			seatinfo.PanelPlayerInfo.IsAllIn = false;
		}
		//避免协程没清 运行残留
		if (Procedure.deskinfo.Pot != "0") yield break;
		Refresh();
	}

	public void Function_SynCKJackPot()
	{
		if (Procedure.msg_SynCKJackPot == null) return;
		Msg_SynCKJackPot ack = Procedure.msg_SynCKJackPot;
		Procedure.msg_SynCKJackPot = null;
		//根据数据 显示获得奖池动画
		int index = 0;
		foreach (var item in ack.Jackpots)
		{
			int currentIndex = index;
			long playerId = item.Key;
			if (!float.TryParse(item.Val, out float jackPotValue))
			{
				GF.LogError("Function_SynCKJackPot 转换失败: ", item.Val);
				continue;
			}
			string jackPot = ((int)Math.Floor(jackPotValue)).ToString();
			DZSeatInfo seat = PanelSeatList.GetPlayerID(playerId);
			if (seat == null) continue;

			// 创建序列，延迟处理多个奖池
			Sequence jackpotSequence = DOTween.Sequence();
			jackpotSequence.SetTarget(gameObject);

			// 根据索引设置延迟
			jackpotSequence.AppendInterval(currentIndex * 0.2f);

			// 添加动画回调
			jackpotSequence.AppendCallback(() =>
			{
				//真正克隆爆炸动画和文本
				GameObject boomAni = Instantiate(varJackBoomAni, transform);
				GameObject boomText = boomAni.transform.Find("JackBoomText").gameObject;

				//设置初始位置和状态
				boomAni.transform.localScale = Vector3.one * 50;
				boomAni.transform.position = jackPotObj.transform.position;
				var spineAni = boomAni.GetComponent<Spine.Unity.SkeletonAnimation>();
				//播放默认循环动画
				spineAni.AnimationState.SetAnimation(0, "idle2", true);
				boomAni.SetActive(true);
				boomText.SetActive(false);

				//移动到玩家位置，同时放大到100倍
				Sequence moveSequence = DOTween.Sequence();
				moveSequence.SetTarget(boomAni);

				//同时执行移动和缩放
				moveSequence.Append(boomAni.transform.DOMove(seat.transform.position, 1.0f).SetEase(Ease.OutQuad));
				moveSequence.Insert(0, boomAni.transform.DOScale(Vector3.one * 100, 1.0f).SetEase(Ease.OutQuad));

				moveSequence.OnComplete(() =>
				{
					//播放爆炸动画
					spineAni.AnimationState.SetAnimation(0, "idle", false);

					// 在爆炸动画开始的同时显示文本并播放跳动动画
					boomText.SetActive(true);
					boomText.GetComponent<Text>().text = jackPot;
					boomText.transform.position = boomAni.transform.position;

					// 播放声音效果
					_ = GF.Sound.PlayEffect("ANIzhuanpan2Audio.mp3");

					// 添加跳动动画
					Sequence bounceSequence = DOTween.Sequence();
					bounceSequence.SetTarget(boomText);

					// 跳动效果 - 创建3次连续的上下弹跳
					for (int i = 0; i < 3; i++)
					{
						// 向上弹跳
						bounceSequence.Append(boomText.transform.DOLocalMoveY(boomText.transform.localPosition.y + 0.5f, 0.2f)
							.SetEase(Ease.OutQuad));
						// 向下落回
						bounceSequence.Append(boomText.transform.DOLocalMoveY(boomText.transform.localPosition.y, 0.2f)
							.SetEase(Ease.InQuad));
					}

					// 在跳动动画完成后延迟销毁对象
					bounceSequence.OnComplete(() =>
					{
						DOTween.Sequence()
							.AppendInterval(2.0f)
							.AppendCallback(() =>
							{
								if (boomText != null) Destroy(boomText);
								if (boomAni != null) Destroy(boomAni);
							});
					});

					bounceSequence.Play();
				});

				moveSequence.Play();
			});

			jackpotSequence.Play();
			index++;
		}
	}

	//收到主池边池信息
	public IEnumerator EdgePot(Msg_EdgePot ack)
	{
		Sequence seq = DOTween.Sequence();
		seq.SetTarget(transform);
		bool isBetCoin = false;
		foreach (DeskPlayer deskplayer in Procedure.deskinfo.DeskPlayers)
		{
			if (deskplayer == null) continue;
			if (deskplayer.BetCoin == "" || deskplayer.BetCoin == "0" || deskplayer.BetCoin == "0.00") continue;
			DZSeatInfo seatinfo = PanelSeatList.GetPos(deskplayer.Pos);
			if (seatinfo == null) continue;
			seatinfo.PanelPlayerInfo.GoChip.SetActive(false);
			seatinfo.PanelPlayerInfo.GoFlyChip.SetActive(true);
			seatinfo.PanelPlayerInfo.GoFlyChip.transform.localPosition = PanelSeatList.GetChipPosition(seatinfo.position);
			seatinfo.PanelPlayerInfo.TextFlyChip.text = Util.Sum2String(deskplayer.BetCoin);
			seq.Join(seatinfo.PanelPlayerInfo.GoFlyChip.transform.DOMove(GoCurrentCoin.transform.position, intervalSettleCoin));
			isBetCoin = true;
		}
		if (isBetCoin)
			_ = GF.Sound.PlayEffect("DPChipMove.ogg");
		yield return seq.WaitForCompletion();
		foreach (DeskPlayer deskplayer in Procedure.deskinfo.DeskPlayers)
		{
			if (deskplayer == null) continue;
			if (deskplayer.BetCoin == "" || deskplayer.BetCoin == "0.00") continue;
			DZSeatInfo seatinfo = PanelSeatList.GetPos(deskplayer.Pos);
			if (seatinfo == null) continue;
			seatinfo.PanelPlayerInfo.GoFlyChip.SetActive(false);
			deskplayer.BetCoin = "0.00";
		}
		Procedure.deskinfo.EdgePot.Clear();
		Procedure.deskinfo.EdgePot.AddRange(ack.EdgePot);
		GF.LogInfo("主池边池信息. ", ack.EdgePot.ToString());
		Procedure.deskinfo.Pot = Util.Sum2String(ack.EdgePot.ToArray());
		Procedure.deskinfo.CurrentPot = "0.00";
		RefreshPot();
	}

	public void SynBackPot(Syn_BackPot ack)
	{
		RefreshPot();
		DeskPlayer dp = Procedure.GetPlayerByID(ack.PlayerId);
		if (dp == null) return;
		dp.Coin = ack.BackCoin;
		DZSeatInfo seatinfo = PanelSeatList.GetPlayerID(ack.PlayerId);
		if (seatinfo == null) return;
		seatinfo.PanelPlayerInfo.TextScore.text = Util.Sum2String(dp.Coin);
	}

	//收到玩家在线状态变更
	public void SynPlayerState(Msg_SynPlayerState ack)
	{
		DeskPlayer deskplayer = Procedure.GetPlayerByID(ack.PlayerId);
		if (deskplayer == null) return;
		if (Util.IsMySelf(ack.PlayerId))
		{
			if (PanelSeatList.SeatList[0].position == deskplayer.Pos)
			{
				GoCancelLeave.SetActive(ack.State == PlayerState.OffLine);
			}
			else
			{
				GoCancelLeave.SetActive(false);
			}
		}
		deskplayer.State = ack.State;
		deskplayer.LeaveTime = ack.LeaveTime;
		DZSeatInfo seatinfo = PanelSeatList.GetPos(deskplayer.Pos);
		if (seatinfo == null) return;
		seatinfo.PanelPlayerInfo.SetLeaveState(deskplayer);
	}

	//收到玩家手牌
	public IEnumerator HandCards(Msg_SynTexasPokerHandCards ack)
	{
		foreach (Msg_HandCards cards in ack.HandCard.ToList())
		{
			DeskPlayer deskplayer = Procedure.GetPlayerByID(cards.PlayerId);
			if (deskplayer == null) continue;
			if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId) == true && ack.IsSettle == true) continue;
			GF.LogInfo("手牌. ", $"玩家:{deskplayer.BasePlayer.PlayerId} 手牌:{cards.HandCards} Allin: {cards.IsAllin}");
			deskplayer.HandCard.Clear();
			deskplayer.HandCard.AddRange(cards.HandCards);
			foreach (DZSeatInfo seat in PanelSeatList.SeatList)
			{
				if (seat.PanelPlayerInfo.PlayerID == cards.PlayerId)
				{
					seat.PanelPlayerInfo.IsAllIn = cards.IsAllin;
					if (cards.IsAllin == true)
					{
						deskplayer.TexasOption = (int)TexasOption.Allin;
					}
				}
			}
		}
		yield return 0;
		Refresh();
	}

	//收到允许购买保险
	public IEnumerator InsuranceInfo(Msg_ProtectInfo ack)
	{
		starttime = ack.CurrentTime;
		nexttime = ack.NextTime;
		GoPanelInsurance.Init(ack);
		Procedure.deskinfo.NextTime = ack.NextTime;
		yield return 0;
		Refresh();
	}

	//收到玩家购买保险
	public IEnumerator BuyInsurance(Msg_SynProtectCard ack)
	{
		GoPanelInsurance.Buy(ack);
		if (Util.IsMySelf(ack.PlayerId))
		{
			// GoPanelInsurance.gameObject.SetActive(false);
			GoPanelInsurance.HideBuyBtn();
		}
		yield return 0;
		Refresh();
	}

	/// <summary>
	/// 购买保险阶段结束
	/// </summary>
	/// <param name="ack"></param>
	/// <returns></returns>
	public IEnumerator BuyInsurance_end()
	{
		GoPanelInsurance.gameObject.SetActive(false);
		yield return 0;
		Refresh();
	}

	//收到切换到保险状态
	public IEnumerator SynTexasState(Msg_SynTexasState ack)
	{
		Procedure.deskinfo.TexasState = ack.TexasState;
		switch (Procedure.deskinfo.TexasState)
		{
			case TexasState.TurnProtect:
			case TexasState.RiverProtect:
				roundstart = false;
				break;
			case TexasState.TexasWait:
				roundstart = false;
				ClearDesktop();
				break;
		}
		Refresh();
		yield return 0;
	}

	//收到查看公共牌
	public void LookCommonCard(Msg_LookCommonCardRs ack)
	{
		if (Procedure.deskinfo.TexasState != TexasState.TexasSettle) return;
		Procedure.deskinfo.CommonCards.Clear();
		Procedure.deskinfo.CommonCards.AddRange(ack.CommonCard);
		RefreshCommonCard();
		RefreshLook();
	}


	public IEnumerator AutoAction()
	{
		yield return new WaitForSeconds(1);
		DeskPlayer deskplayer = Procedure.GetPlayerByPos(Procedure.deskinfo.CurrentOption);
		if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId) == false) yield break;
		float.TryParse(Procedure.deskinfo.FollowCoin, out float followcoin);
		if (PanelAction.AutoFold && followcoin > 0)
		{
			Procedure.Send_FoldCard();
		}
		if (PanelAction.AutoFold && followcoin == 0)
		{
			Procedure.Send_CallCard();
		}
		if (PanelAction.AutoCall && followcoin == 0)
		{
			Procedure.Send_CallCard();
		}
		yield return 0;
	}

	void ClearDesktop()
	{
		foreach (DeskPlayer deskplayer in Procedure.deskinfo.DeskPlayers)
		{
			if (deskplayer == null) continue;
			deskplayer.HandCard.Clear();
			deskplayer.BetCoin = "";
			deskplayer.IsBanker = false;
			deskplayer.IsGive = false;
			deskplayer.TexasOption = 0;
			deskplayer.BuyTime = 0;
			deskplayer.IsShow = false;
			DZSeatInfo seatinfo = PanelSeatList.GetPos(deskplayer.Pos);
			if (seatinfo == null) continue;
			seatinfo.PanelPlayerInfo.Profit = null;
			seatinfo.PanelPlayerInfo.IsWin = false;
			seatinfo.PanelPlayerInfo.IsAllIn = false;
		}
		Procedure.deskinfo.Banker = Procedure.deskinfo.SmallBlind = Procedure.deskinfo.BigBlind = Procedure.deskinfo.StrongBlind = Position.NoSit;
		Procedure.deskinfo.Crit = false;
		ClearCard();
		options.Clear();
		Procedure.deskinfo.CommonCards.Clear();
		Refresh();
	}

	void ClearCard()
	{
		for (int i = 0; i < 5; i++)
		{
			var card = CommonCard[i];
			card.GetComponent<Image>().color = Color.white;
			card.transform.Find("Image").gameObject.SetActive(false);
			card.transform.localRotation = Quaternion.identity;
			card.transform.localPosition = PositionCommonCard[i];
		}
		Card1.GetComponent<Image>().color = Color.white;
		Card1.transform.Find("Image").gameObject.SetActive(false);
		Card1.transform.localPosition = LPositionCard1;
		Card2.GetComponent<Image>().color = Color.white;
		Card2.transform.Find("Image").gameObject.SetActive(false);
		Card2.transform.localPosition = LPositionCard2;
	}

	void Clear()
	{
		// GoCurrentCoin.SetActive(false);
		PanelAction.gameObject.SetActive(false);
		PanelAction.Clear();
		PanelSeatList.Clear();
		options.Clear();
		GoPanelPause.SetActive(false);
		GoPanelSetting.gameObject.SetActive(false);
		GoCancelLeave.SetActive(false);
	}

	void Reset()
	{
		GoButtonStart.SetActive(false);
		PanelAction.Reset();
		PanelAction.gameObject.SetActive(false);
		PanelSeatList.Reset();
		// foreach (DeskPlayer deskplayer in Procedure.deskinfo.DeskPlayers) {
		// 	deskplayer.TexasOption = 0;
		// }
		PanelHandCard.SetActive(false);
		for (var i = 0; i < CommonCard.Count; i++)
		{
			CommonCard[i].transform.localRotation = Quaternion.identity;
			CommonCard[i].transform.localPosition = PositionCommonCard[i];
			CommonCard[i].gameObject.SetActive(false);
		}
		// GoCurrentCoin.SetActive(false);
		for (var i = 0; i < GoPotCoin1.transform.childCount; i++)
		{
			Destroy(GoPotCoin1.transform.GetChild(i).gameObject);
		}
		for (var i = 0; i < GoPotCoin2.transform.childCount; i++)
		{
			Destroy(GoPotCoin2.transform.GetChild(i).gameObject);
		}
		TextCurrentCoin.text = "";
	}

	void Refresh()
	{
		Reset();
		GoPanelBaoJi.SetActive(Procedure.deskinfo.Config.Critcal);
		if (Procedure.deskinfo.DeskState == DeskState.WaitStart && Procedure.IsRoomOwner())
		{
			GoButtonStart.SetActive(true);
		}
		foreach (DZSeatInfo si in PanelSeatList.SeatList)
		{
			si.PanelPlayerInfo.IsBanker = si.PanelPlayerInfo.IsSB = si.PanelPlayerInfo.IsBB = si.PanelPlayerInfo.IsStrongBlind = false;
		}
		DZSeatInfo seatinfo = PanelSeatList.GetPos(Procedure.deskinfo.Banker);
		if (seatinfo != null)
		{
			seatinfo.PanelPlayerInfo.IsBanker = true;
		}
		seatinfo = PanelSeatList.GetPos(Procedure.deskinfo.SmallBlind);
		if (seatinfo != null)
		{
			seatinfo.PanelPlayerInfo.IsSB = true;
		}
		seatinfo = PanelSeatList.GetPos(Procedure.deskinfo.BigBlind);
		if (seatinfo != null)
		{
			seatinfo.PanelPlayerInfo.IsBB = true;
		}
		seatinfo = PanelSeatList.GetPos(Procedure.deskinfo.StrongBlind);
		if (seatinfo != null)
		{
			seatinfo.PanelPlayerInfo.IsStrongBlind = true;
		}
		PanelAction.Refresh();
		PanelSeatList.Refresh();
		switch (Procedure.deskinfo.TexasState)
		{
			case TexasState.PreFlop:
				RefreshHandCard();
				RefreshCommonCard();
				break;
			case TexasState.Flop:
				RefreshHandCard();
				RefreshCommonCard();
				break;
			case TexasState.Turn:
			case TexasState.TurnProtect:
				RefreshHandCard();
				RefreshCommonCard();
				break;
			case TexasState.River:
			case TexasState.RiverProtect:
				RefreshHandCard();
				RefreshCommonCard();
				break;
			case TexasState.TexasSettle:
				RefreshHandCard();
				RefreshCommonCard();
				break;
			default:
				break;
		}
		RefreshPot();
		DeskPlayer defaultplayer = Procedure.GetPlayerByPos(PanelSeatList.SeatList[0].position);
		if (defaultplayer != null && Util.IsMySelf(defaultplayer.BasePlayer.PlayerId) == true)
		{
			GoCancelLeave.SetActive(defaultplayer.State == PlayerState.OffLine);
			RefreshAddTime();
		}
		else
		{
			GoCancelLeave.SetActive(false);
			GoAddTime.SetActive(false);
		}

		if ((Procedure.deskinfo.TexasState == TexasState.TurnProtect || Procedure.deskinfo.TexasState == TexasState.RiverProtect) && GoPanelInsurance.binit == true)
		{
			GoPanelInsurance.gameObject.SetActive(true);
		}
		else
		{
			GoPanelInsurance.gameObject.SetActive(false);
		}

		if (Procedure.deskinfo.DeskState == DeskState.Pause)
		{
			GoPanelPause.SetActive(true);
			if (Procedure.IsRoomOwner())
			{
				GoPause1.SetActive(true);
				GoPause2.SetActive(false);
			}
			else
			{
				GoPause1.SetActive(false);
				GoPause2.SetActive(true);
			}
		}
		else
		{
			GoPanelPause.SetActive(false);
		}
		RefreshLook();
		TextCoin.text = Util.Sum2String(Util.GetMyselfInfo().Gold);
		TextDiamond.FormatAmount(Util.GetMyselfInfo().Diamonds);
		wpkBtn.SetActive(Procedure.deskinfo.BaseConfig.OpenSafe);
		jackPotObj.SetActive(Procedure.deskinfo.BaseConfig.Jackpot);
		if (Procedure.deskinfo.BaseConfig.Jackpot) UpdateJackpot();
	}

	void RefreshAddTime()
	{
		GoAddTime.SetActive(false);
		DeskPlayer selfplayer = Procedure.GetSelfPlayer();
		if (selfplayer == null) return;
		if (selfplayer.Pos != PanelSeatList.SeatList[0].position) return;
		if (Procedure == null) return;
		if (Procedure.deskinfo.TexasState == TexasState.TexasSettle) return;
		GoPanelInsurance.RefreshAddTime();
		if (Procedure.deskinfo.CurrentOption != selfplayer.Pos) return;
		if (SystemConfig.ContainsKey(14) == false) return;
		GoAddTime.SetActive(true);
		TextAddTime.text = (SystemConfig[14] * (int)Math.Pow(2, selfplayer.BuyTime)).ToString();
	}

	void RefreshHandCard()
	{
		DeskPlayer selfplayer = Procedure.GetSelfPlayer();
		if (selfplayer != null && selfplayer.HandCard.Count == 2)
		{
			PanelHandCard.SetActive(true);
			Card1.SetImgColor(selfplayer.IsGive ? Color.gray : Color.white);
			Card2.SetImgColor(selfplayer.IsGive ? Color.gray : Color.white);
			Card1.Init(selfplayer.HandCard[0]);
			Card2.Init(selfplayer.HandCard[1]);
		}
		else
		{
			PanelHandCard.SetActive(false);
		}
		if (selfplayer != null && selfplayer.Pos == Procedure.deskinfo.CurrentOption)
		{
			PanelAction.Refresh();
		}
		else
		{
			DZSeatInfo seatinfo = PanelSeatList.GetPos(Procedure.deskinfo.CurrentOption);
			if (seatinfo == null) return;
			seatinfo.Action();
		}
	}

	void RefreshCommonCard()
	{
		if (Procedure == null || Procedure.deskinfo == null || Procedure.deskinfo.CommonCards == null)
		{
			for (var i = 0; i < CommonCard.Count; i++)
			{
				CommonCard[i].gameObject.SetActive(false);
			}
			return;
		}
		for (var i = 0; i < CommonCard.Count; i++)
		{
			if (Procedure.deskinfo.CommonCards.Count > i && Procedure.deskinfo.CommonCards[i] > 0)
			{
				CommonCard[i].Init(Procedure.deskinfo.CommonCards[i]);
				CommonCard[i].GetComponent<Image>().color = Color.white;
				CommonCard[i].transform.localRotation = Quaternion.identity;
				CommonCard[i].transform.localPosition = PositionCommonCard[i];
				CommonCard[i].gameObject.SetActive(true);
			}
			else
			{
				CommonCard[i].gameObject.SetActive(false);
			}
		}
	}

	void RefreshLook()
	{
		GoLook.SetActive(false);
		if (Procedure.deskinfo.TexasState != TexasState.TexasSettle) return;
		if (Procedure.deskinfo.CommonCards.Count >= 5) return;
		GoLook.SetActive(true);
	}

	void RefreshPot()
	{
		switch (Procedure.deskinfo.TexasState)
		{
			case TexasState.PreFlop:
				GoCurrentCoin.SetActive(true);
				break;
			case TexasState.Flop:
				GoCurrentCoin.SetActive(true);
				break;
			case TexasState.Turn:
				GoCurrentCoin.SetActive(true);
				break;
			case TexasState.River:
				GoCurrentCoin.SetActive(true);
				break;
			default:
				// GoCurrentCoin.SetActive(false);
				break;
		}
		for (var i = 0; i < GoPotCoin1.transform.childCount; i++)
		{
			Destroy(GoPotCoin1.transform.GetChild(i).gameObject);
		}
		for (var i = 0; i < GoPotCoin2.transform.childCount; i++)
		{
			Destroy(GoPotCoin2.transform.GetChild(i).gameObject);
		}
		TextCurrentCoin.text = Util.Sum2String(Procedure.deskinfo.Pot);
		if (Procedure.deskinfo.EdgePot.Count <= 1) return;
		int ncount = 0;
		foreach (string edgepot in Procedure.deskinfo.EdgePot)
		{
			GameObject go = Instantiate(GoPotItem, ncount < 4 ? GoPotCoin1.transform : GoPotCoin2.transform);
			go.SetActive(true);
			go.GetComponentInChildren<Text>().text = edgepot;
			go.transform.localPosition = Vector3.zero;
			go.transform.localScale = Vector3.one;
			ncount++;
		}
	}

	void ShowFloating(string chat)
	{
		var item = Instantiate(GoItemFloating, GoPanelFloating.transform);
		item.transform.localPosition = Vector3.zero;
		item.transform.localScale = Vector3.one;
		item.SetActive(true);
		item.GetComponent<FloatingTips>().ShowTips(chat, 15f);
	}

	void ShowEmoji(BasePlayer player, string chat)
	{
		DeskPlayer deskplayer = Procedure.GetPlayerByID(player.PlayerId);
		if (deskplayer == null) return;
		DZSeatInfo seatinfo = PanelSeatList.GetPos(deskplayer.Pos);
		if (seatinfo == null) return;
		GF.UI.LoadPrefab(AssetsPath.GetPrefab("UI/Chat/" + chat), (gameObject) =>
		{
			GameObject emoji = Instantiate(gameObject as GameObject, seatinfo.transform);
			emoji.GetComponent<FrameAnimator>().Framerate = 10;
			emoji.transform.localPosition = Vector3.zero;
			emoji.transform.localScale = Vector3.one;
			emoji.transform.DOScale(Vector3.one * 1.5f, 1f).OnComplete(() =>
			{
				Destroy(emoji, 1f);
			});
		});
	}

	public void SendGift(long basePlayerID, long toPlayerId, string gift)
	{
		DeskPlayer deskplayer;
		DZSeatInfo from = null, to = null;
		deskplayer = Procedure.GetPlayerByID(basePlayerID);
		if (deskplayer != null)
		{
			from = PanelSeatList.GetPos(deskplayer.Pos);
		}
		deskplayer = Procedure.GetPlayerByID(toPlayerId);
		if (deskplayer == null) return;
		if (deskplayer != null)
		{
			to = PanelSeatList.GetPos(deskplayer.Pos);
			if (to == null) return;
		}
		GF.UI.LoadPrefab(AssetsPath.GetPrefab("UI/Chat/" + gift), (gameObject) =>
		{
			if (gameObject == null) return;
			GameObject giftObj = Instantiate((gameObject as GameObject), transform);
			if (from == null)
			{
				giftObj.transform.localPosition = GoButtonEmojiStart.transform.localPosition;
			}
			else
			{
				giftObj.transform.localPosition = from.transform.localPosition;
			}
			giftObj.transform.localScale = gift switch
			{
				"2001" => new Vector3(1.8f, 1.8f, 1.8f),
				"2003" => new Vector3(1.8f, 1.8f, 1.8f),
				"2005" => new Vector3(2.2f, 2.2f, 2.2f),
				"2007" => new Vector3(2.2f, 2.2f, 2.2f),
				_ => new Vector3(2f, 2f, 2f),
			};
			FrameAnimator frameAnimator = giftObj.GetComponent<FrameAnimator>();
			frameAnimator.Loop = false;
			frameAnimator.Stop();
			frameAnimator.FinishEvent += (giftobj) =>
			{
				Destroy(giftobj);
			};
			giftObj.transform.DOLocalMove(to.transform.localPosition, 0.8f).OnComplete(() =>
			{
				frameAnimator.Framerate = 6;
				if (gift == "2003")
				{
					frameAnimator.Framerate = 2;
				}
				frameAnimator.Play();
				_ = GF.Sound.PlayEffect("face/" + gift + ".mp3");
			});
		});
	}

	void BringIn(bool leave)
	{
		if (PanelBringIn.gameObject.activeSelf == true) return;
		PanelBringIn.ShowBring(leave);
	}

	IEnumerator DownloadAndPlayMp3(long playerid, string url)
	{
		// if (Util.GetInstance().IsMy(playerid) == true) yield break;
		string downloadUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}:{Const.ServerInfo_BackEndPort}/voice/{Procedure.deskID}/";
		if (Const.CurrentServerType == Const.ServerType.外网服)
		{
			downloadUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}/voice/{Procedure.deskID}/";
		}
		using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(downloadUrl + url, AudioType.MPEG);
		request.certificateHandler = new BypassCertificate();
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
		{
			GF.LogError("Error downloading audio: ", request.error);
			yield break;
		}
		DZSeatInfo seatinfo = PanelSeatList.GetPlayerID(playerid);
		if (seatinfo != null)
		{
			seatinfo.PanelPlayerInfo.GoVoice.SetActive(true);
		}
		AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
		SoundManager.Instance.PlaySFX(clip, 0, () =>
		{
			if (seatinfo != null)
			{
				seatinfo.PanelPlayerInfo.GoVoice.SetActive(false);
			}
		});
	}

	IEnumerator ShowVoice(Transform parentTransform, float interval)
	{
		int childCount = parentTransform.childCount;
		int index = 0;
		foreach (Transform child in parentTransform)
		{
			child.gameObject.SetActive(false);
		}
		while (true)
		{
			for (int i = 0; i < childCount; i++)
			{
				parentTransform.GetChild(i).gameObject.SetActive(i == index);
			}
			index = (index + 1) % childCount;
			yield return new WaitForSeconds(interval);
		}
	}

	public void OnVoicePress()
	{
		if (Procedure.deskinfo.BaseConfig.ForbidVoice)
		{
			GF.LogInfo("禁止语音");
			GF.UI.ShowToast("禁止聊天!");
			return;
		}
		DeskPlayer seat = Procedure.GetSelfPlayer();
		if (seat == null)
		{
			return;
		}
		if (seat.LeaveTime > 0 || float.Parse(seat.Coin) <= 0)
		{
			return;
		}
		GoPanelRecord.StartRecord(Util.GetMyselfInfo().PlayerId, Procedure.deskID);
	}

	public void OnVoiceRelease()
	{
		if (Procedure.deskinfo.BaseConfig.ForbidVoice)
		{
			return;
		}
		GoPanelRecord.StopRecord();
	}

	public void OnVoiceCancel()
	{
		if (Procedure.deskinfo.BaseConfig.ForbidVoice)
		{
			return;
		}
		GoPanelRecord.CancelRecord();
	}

	public Image changeImg;

	public void onDragInVisual()
	{
		changeImg.SetSprite("NN/voice_recording_1.png");
	}

	public void onDragOutVisual()
	{
		changeImg.SetSprite("NN/voice_recording_2.png");
	}

	public void Function_EnterDZDeskRs(MessageRecvData data)
	{
		Procedure.deskinfo = Msg_EnterTexasDeskRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("刷新德州桌子信息", Procedure.deskinfo.ToString());
		transform.DOKill();
		starttime = Procedure.deskinfo.CurrentStateTime;
		nexttime = Procedure.deskinfo.NextTime;
		endtime = Procedure.deskinfo.EndTime;
		foreach (DeskPlayer deskplayer in Procedure.deskinfo.DeskPlayers)
		{
			if (deskplayer == null) continue;
			DZSeatInfo seatinfo = PanelSeatList.GetPos(deskplayer.Pos);
			if (seatinfo == null) continue;
			seatinfo.PanelPlayerInfo.Profit = null;
			seatinfo.PanelPlayerInfo.IsWin = false;
			seatinfo.PanelPlayerInfo.IsAllIn = deskplayer.TexasOption == (int)TexasOption.Allin;
		}
		//发牌阶段显示按钮
		roundstart = (int)Procedure.deskinfo.TexasState > 0 && (int)Procedure.deskinfo.TexasState < 5;
		if (Procedure.deskinfo.TexasState == TexasState.TurnProtect || Procedure.deskinfo.TexasState == TexasState.RiverProtect)
		{
			GoPanelInsurance.Init(Procedure.deskinfo.ProtectInfo);
		}
		PanelSeatList.Refresh();
		ClearCard();
		Refresh();
		// 关闭游戏状态同步的等待框
		Util.GetInstance().CloseWaiting("GameStateSync");
	}

	public override void OnClickClose()
	{
		if (Util.IsClickLocked()) return;
		_ = GF.Sound.PlayEffect(AudioKeys.SOUND_BTN);
		Util.GetInstance().OpenConfirmationDialog("退出房间", "确定要房间吗?", () =>
		{
			Procedure.Send_LeaveDesk();
		});
	}
}

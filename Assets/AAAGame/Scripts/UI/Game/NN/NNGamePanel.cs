using DG.Tweening;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using GameFramework.Event;
using static UtilityBuiltin;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityGameFramework.Runtime;
using UnityEngine.Networking;
using Google.Protobuf;
using System;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class NNGamePanel : UIFormBase
{
	NNProcedure nnProcedure;

	public CoroutineManager coroutinemanager;

	// UI节点
	public GameObject Menu;
	private UserDataModel userDataModel;
	public GameObject BringUI;

	public RedDotItem msgRedDot;
	public Text textCoin;
	public Text textDiamond;

	public Text textMaxRatio;
	public Text textRoomName;
	public Text textRoomID;
	public Text textBaseCoin;
	public Text textMinBringIn;
	public Text ipGps;
	public Text textNiuRobBanker;
	public Text textMaxRate;
	public Text textRogueRate;

	public GameObject varJackBoomAni;
	public GameObject jackPotObj;
	public GameObject wpkBtn;
	public GameObject goRandomBanker;

	public GameObject goButtonStart;
	public GameObject goButtonEmojiStart;

	public GameObject goButtonWait;

	public List<SeatInfo> seatlist;

	public RectTransform rectCard;
	public List<Card> cardlist;
	public List<GameObject> cardborder;

	public Image imageBobRanker;

	public GameObject goChip;
	public GameObject goChipFly;

	private readonly List<Vector3> origincardpos = new();
	public GameObject goAction;
	public GameObject goPanelRobbanker;
	public GameObject goPanelBetCoin;
	public GameObject goPanelShowCard;
	public GameObject goPanelSetting;
	public GameObject goPanelRule;
	public GameObject goPanelReview;
	public GameObject goPanelRealTime;
	public GameWaitSeat goPanelWait;

	private Vector2 vecCardCenter;

	public List<GameObject> goRobBanker;
	public List<GameObject> goBetCoin;
	public List<Text> textBetCoin;

	public GameObject goNiuType;
	public Image imageNiuType;
	public GameObject goNiuEffect1;
	public GameObject goNiuEffect2;

	public List<Toggle> toggleTable;

	public GameObject goPanelTimer;
	public Text textTimer;
	public Image aniTimer;

	public GameObject goPanelCancelLeave;
	public GameObject goPanelPause;
	public GameObject goPause1;
	public GameObject goPause2;

	public GameObject goPanelFlipCard;

	public RecordInfo goPanelRecord;

	public GameObject goPanelFloating;
	public GameObject goItemFloating;

	private readonly float intervalSeat = 0.2f;
	private readonly float intervalCard1 = 0.5f;
	private readonly float intervalCard2 = 0.5f;
	private readonly float intervalCard3 = 0.2f;
	private readonly float intervalRandomBanker = 0.02f;
	private readonly float intervalBanker = 0.2f;
	private readonly float intervalCoin = 0.2f;
	private readonly float intervalChip = 0.5f;

	private readonly float intervalOpenMenu = 0.3f;
	private readonly float intervalCloseMenu = 0.1f;

	private readonly float intervalOpenCard1 = 0.05f;
	private readonly float intervalOpenCard2 = 1f;

	private readonly float intervalSettle1 = 0.01f;
	private readonly float intervalSettle2 = 1f;

	List<GameObject> coins = new();

	public bool brecord = false;

	protected override void OnInit(object userData)
	{
		base.OnInit(userData);
		userDataModel = Util.GetMyselfInfo();
	}

	protected override void OnOpen(object userData)
	{
		base.OnOpen(userData);
		coroutinemanager.Init();
		HotfixNetworkComponent.AddListener(MessageID.SingleGameRecord, Function_SingleGameRecord);
		HotfixNetworkComponent.AddListener(MessageID.Msg_SynDeskLessFive, Function_SynNotifyDeskLessTime);
		HotfixNetworkComponent.AddListener(MessageID.Msg_BuyVipRs, Function_BuyVipRs);
		HotfixNetworkComponent.AddListener(MessageID.Msg_EnterNiuNiuDeskRs, Function_EnterNiuNiuDeskRs);

		brecord = false;

		RedDotManager.GetInstance().SetRedDotNodeCallBack(E_RedDotDefine.MsgBox, MsgCallBack);
		RedDotManager.GetInstance().RefreshAll();
		GF.Event.Subscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
		GF.Event.Subscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);
		GF.Event.Subscribe(GFEventArgs.EventId, OnGFEventChanged);
		nnProcedure = GF.Procedure.CurrentProcedure as NNProcedure;
		InitPosition(nnProcedure.enterNiuNiuDeskRs.BaseConfig.PlayerNum);

		for (var i = 0; i < 5; i++)
		{
			origincardpos.Add(cardlist[i].transform.localPosition);
		}
		RectTransformUtility.ScreenPointToLocalPointInRectangle(rectCard.GetComponent<RectTransform>(), new Vector2(Screen.width / 2, Screen.height / 2), GF.RootCanvas.worldCamera, out vecCardCenter);
		Util.UpdateTableColor(transform.GetComponent<Image>());

		foreach (var toggle in toggleTable)
		{
			toggle.onValueChanged.AddListener(OnTableBGToggleChanged);
		}

		Refresh();
	}

	public void Refresh()
	{
		remaintime = nnProcedure.enterNiuNiuDeskRs.EndTime;
		MoveSeatAnim(null);
	}

	public void InitPosition(int number, Position pos = Position.Default)
	{
		List<Position> positionlist = new(), reallist = new();
		switch (number)
		{
			case 2: positionlist = new() { Position.Default, Position.NoSit, Position.NoSit, Position.NoSit, Position.Four, Position.NoSit, Position.NoSit, Position.NoSit, Position.NoSit }; break;
			case 3: positionlist = new() { Position.Default, Position.NoSit, Position.NoSit, Position.Three, Position.NoSit, Position.NoSit, Position.Six, Position.NoSit, Position.NoSit }; break;
			case 4: positionlist = new() { Position.Default, Position.NoSit, Position.Two, Position.NoSit, Position.Four, Position.NoSit, Position.NoSit, Position.Seven, Position.NoSit }; break;
			case 5: positionlist = new() { Position.Default, Position.One, Position.Two, Position.NoSit, Position.NoSit, Position.NoSit, Position.Six, Position.Seven, Position.NoSit }; break;
			case 6: positionlist = new() { Position.Default, Position.One, Position.Two, Position.NoSit, Position.Four, Position.NoSit, Position.Six, Position.Seven, Position.NoSit }; break;
			case 7: positionlist = new() { Position.Default, Position.One, Position.Two, Position.Three, Position.NoSit, Position.NoSit, Position.Six, Position.Seven, Position.Eight }; break;
			case 8: positionlist = new() { Position.Default, Position.One, Position.Two, Position.Three, Position.Four, Position.Five, Position.Six, Position.Seven, Position.NoSit }; break;
		}
		int index = positionlist.IndexOf(pos);
		if (index != -1)
		{
			reallist.AddRange(positionlist.GetRange(index, positionlist.Count - index));
			reallist.AddRange(positionlist.GetRange(0, index));
		}
		else
		{
			reallist.AddRange(positionlist);
		}

		index = -1;
		for (var i = 0; i < 9; i++)
		{
			seatlist[i].seatpos = (int)reallist[i];
			if (reallist[i] == Position.NoSit)
			{
				seatlist[i].gameObject.SetActive(false);
				continue;
			}
			seatlist[i].Reset();
			seatlist[i].gameObject.SetActive(true);
			index++;

			bool left = true;
			switch (nnProcedure.enterNiuNiuDeskRs.BaseConfig.PlayerNum)
			{
				case 8:
					if (index >= 5) left = false;
					break;
				case 7:
					if (index >= 4) left = false;
					break;
				case 6:
					if (index >= 4) left = false;
					break;
				case 5:
					if (index >= 3) left = false;
					break;
				case 4:
					if (index >= 3) left = false;
					break;
				case 3:
					if (index >= 2) left = false;
					break;
			}

			if (left)
			{
				for (int j = 0; j < 5; j++)
				{
					seatlist[i].transform.Find("PlayerL/BaseCards/card" + j).SetSiblingIndex(j);
				}
				seatlist[i].rectCard.transform.localPosition = new Vector3(80, 0, 0);
				seatlist[i].origincardpos = seatlist[1].origincardpos;
				seatlist[i].vecCardCenter = seatlist[1].vecCardCenter;
				seatlist[i].vecBankerCenter = seatlist[1].vecBankerCenter;
				seatlist[i].originbankerPos = seatlist[1].originbankerPos;
				seatlist[i].originChipPos = seatlist[1].originChipPos;
				seatlist[i].statePos = seatlist[1].statePos;
				seatlist[i].numImagePos = seatlist[1].numImagePos;
				seatlist[i].operateTypePos = seatlist[1].operateTypePos;
				seatlist[i].textChip.transform.localPosition = seatlist[1].originChipTextPos;
				seatlist[i].textChip.alignment = TextAnchor.MiddleLeft;
			}
			else
			{
				for (int j = 0; j < 5; j++)
				{
					seatlist[i].transform.Find("PlayerL/BaseCards/card" + j).SetSiblingIndex(j);
				}
				seatlist[i].rectCard.transform.localPosition = new Vector3(-80, 0, 0);
				seatlist[i].origincardpos = seatlist[7].origincardpos;
				seatlist[i].vecCardCenter = seatlist[7].vecCardCenter;
				seatlist[i].vecBankerCenter = seatlist[7].vecBankerCenter;
				seatlist[i].originbankerPos = seatlist[7].originbankerPos;
				seatlist[i].originChipPos = seatlist[7].originChipPos;
				seatlist[i].statePos = seatlist[7].statePos;
				seatlist[i].numImagePos = seatlist[7].numImagePos;
				seatlist[i].operateTypePos = seatlist[7].operateTypePos;
				seatlist[i].textChip.transform.localPosition = seatlist[7].originChipTextPos;
				seatlist[i].textChip.alignment = TextAnchor.MiddleRight;
			}
		}
	}

	void MsgCallBack(RedDotNode node)
	{
		if (gameObject == null) return;
		msgRedDot.SetDotState(node.rdCount > 0, node.rdCount);
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
				if (Util.GetMyselfInfo().DeskId != 0 && Util.GetMyselfInfo().DeskId == nnProcedure.deskID)
				{
					Util.GetInstance().Send_EnterDeskRq(nnProcedure.deskID);
				}
				else
				{
					GF.LogInfo("返回登录界面");
					HomeProcedure homeProcedure = GF.Procedure.GetProcedure<HomeProcedure>() as HomeProcedure;
					homeProcedure.QuitGame();
					// nnProcedure.EnterHome();
				}
				break;
		}
	}

	IEnumerator DownloadAndPlayMp3(long playerid, string url)
	{
		// if (Util.GetInstance().IsMy(playerid) == true) yield break;
		string downloadUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}:{Const.ServerInfo_BackEndPort}/voice/{nnProcedure.deskID}/";
		if (Const.CurrentServerType == Const.ServerType.外网服)
		{
			downloadUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}/voice/{nnProcedure.deskID}/";
		}
		using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(downloadUrl + url, AudioType.MPEG);
		request.certificateHandler = new BypassCertificate();
		yield return request.SendWebRequest();
		if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
		{
			GF.LogError("Error downloading audio: ", request.error);
			yield break;
		}
		SeatInfo seatinfo = GetPlayerID(playerid);
		if (seatinfo != null)
		{
			seatinfo.goVoice.SetActive(true);
		}
		AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
		SoundManager.Instance.PlaySFX(clip, 0, () =>
		{
			if (seatinfo != null)
			{
				seatinfo.goVoice.SetActive(false);
			}
		});
	}

	private void ShowFloating(string chat)
	{
		var item = Instantiate(goItemFloating, goPanelFloating.transform);
		item.transform.localPosition = Vector3.zero;
		item.transform.localScale = Vector3.one;
		item.SetActive(true);
		item.GetComponent<FloatingTips>().ShowTips(chat, 15f);
	}

	private void ShowEmoji(BasePlayer player, string chat)
	{
		SeatInfo seatinfo = GetPlayerID(player.PlayerId);
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

	protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
	{
		base.OnUpdate(elapseSeconds, realElapseSeconds);
		SetTimer();
	}

	protected override void OnClose(bool isShutdown, object userData)
	{
		HotfixNetworkComponent.RemoveListener(MessageID.SingleGameRecord, Function_SingleGameRecord);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_SynDeskLessFive, Function_SynNotifyDeskLessTime);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_BuyVipRs, Function_BuyVipRs);
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_EnterNiuNiuDeskRs, Function_EnterNiuNiuDeskRs);
		RedDotManager.GetInstance().RemoveRedDotNodeCallBack(E_RedDotDefine.MsgBox, MsgCallBack);
		GF.Event.Unsubscribe(GFEventArgs.EventId, OnGFEventChanged);
		GF.Event.Unsubscribe(ClubDataChangedEventArgs.EventId, OnClubDataChanged);
		GF.Event.Unsubscribe(UserDataChangedEventArgs.EventId, OnUserDataChanged);

		Menu.SetActive(false);
		BringUI.GetComponent<BringUI>().Clear();
		Reset();
		for (var i = 0; i < 9; i++)
		{
			seatlist[i].LeavePlayer();
			seatlist[i].seatpos = i + 1;
		}

		imageBobRanker.gameObject.SetActive(false);
		goChip.SetActive(false);
		goPanelRobbanker.SetActive(false);
		goPanelBetCoin.SetActive(false);
		goPanelShowCard.SetActive(false);
		goPanelSetting.SetActive(false);
		goPanelRule.SetActive(false);
		goPanelReview.SetActive(false);
		goPanelRealTime.SetActive(false);
		goNiuType.SetActive(false);
		goPanelTimer.SetActive(false);
		goPanelCancelLeave.SetActive(false);
		goPanelFlipCard.SetActive(false);
		foreach (var rob in goRobBanker)
		{
			rob.SetActive(false);
		}
		foreach (var coin in goBetCoin)
		{
			coin.SetActive(false);
		}
		foreach (GameObject coin in coins)
		{
			Destroy(coin);
		}
		foreach (var toggle in toggleTable)
		{
			if (toggle != null)
			{
				toggle.onValueChanged.RemoveListener(OnTableBGToggleChanged);
			}
		}
		coroutinemanager.Uninit();
		base.OnClose(isShutdown, userData);
	}

	protected override async void OnButtonClick(object sender, string btId)
	{
		base.OnButtonClick(sender, btId);
		var uiParams = UIParams.Create();
		switch (btId)
		{
			case "房主开始游戏":
				nnProcedure.Send_Start();
				goButtonStart.SetActive(false);
				break;
			case "打开左上角菜单":
				if (Menu.activeSelf)
				{
					Menu.transform.DOLocalMoveX(-500, intervalCloseMenu).OnComplete(() =>
					{
						Menu.SetActive(false);
					});
				}
				else
				{
					Menu.SetActive(true);
					Menu.transform.localPosition = new Vector3(-500, Menu.transform.localPosition.y, Menu.transform.localPosition.z);
					Menu.transform.DOLocalMoveX(0, intervalOpenMenu);
				}
				break;
			case "关闭左上角菜单":
				Menu.transform.DOLocalMoveX(-500, intervalCloseMenu).OnComplete(() =>
				{
					Menu.SetActive(false);
				});
				break;
			case "牌桌设置":
				int index = SettingExtension.GetTableBG();
				for (var i = 0; i < toggleTable.Count; i++)
				{
					toggleTable[i].isOn = i == index;
				}
				goPanelSetting.transform.localScale = Vector3.zero;
				goPanelSetting.SetActive(true);
				goPanelSetting.transform.DOScale(Vector3.one, intervalOpenMenu);
				break;
			case "关闭设置":
				goPanelSetting.transform.localScale = Vector3.one;
				goPanelSetting.transform.DOScale(Vector3.zero, intervalCloseMenu).OnComplete(() =>
				{
					goPanelSetting.SetActive(false);
				});
				break;
			case "游戏规则":
				goPanelRule.transform.localScale = Vector3.zero;
				goPanelRule.SetActive(true);
				goPanelRule.transform.DOScale(Vector3.one, intervalOpenMenu);
				goPanelRule.transform.Find("ImageRule1").gameObject.SetActive(true);
				goPanelRule.transform.Find("ImageRule2").gameObject.SetActive(false);
				if (nnProcedure.enterNiuNiuDeskRs.NiuConfig.DoubleRule == 5)
				{
					goPanelRule.transform.Find("ImageRule1").gameObject.SetActive(false);
					goPanelRule.transform.Find("ImageRule2").gameObject.SetActive(true);
				}
				break;
			case "关闭规则":
				goPanelRule.transform.localScale = Vector3.one;
				goPanelRule.transform.DOScale(Vector3.zero, intervalCloseMenu).OnComplete(() =>
				{
					goPanelRule.SetActive(false);
				});
				break;
			case "提前结算":
				nnProcedure.Send_AdvanceSettle();
				break;
			case "带入":
				DeskPlayer deskplayer = nnProcedure.GetSelfPlayer();
				if (deskplayer != null)
				{
					BringIn(false);
				}
				break;
			case "商城":
				await GF.UI.OpenUIFormAwait(UIViews.ShopPanel);
				break;
			case "回放":
				if (goPanelReview.activeSelf)
				{
					goPanelReview.transform.GetComponent<RectTransform>().DOAnchorPosX(goPanelReview.transform.Find("Panel").GetComponent<RectTransform>().rect.width, intervalCloseMenu).OnComplete(() =>
					{
						goPanelReview.SetActive(false);
					});
				}
				else
				{
					RectTransform rev = goPanelReview.transform.GetComponent<RectTransform>();
					rev.anchoredPosition = new Vector2(rev.transform.Find("Panel").GetComponent<RectTransform>().rect.width, rev.anchoredPosition.y);
					goPanelReview.SetActive(true);
					rev.DOAnchorPosX(0, intervalOpenMenu);
				}
				break;
			case "即时战绩":
				if (goPanelRealTime.activeSelf)
				{
					goPanelRealTime.transform.GetComponent<RectTransform>().DOAnchorPosX(0 - goPanelRealTime.transform.Find("Panel").GetComponent<RectTransform>().rect.width, intervalCloseMenu).OnComplete(() =>
					{
						goPanelRealTime.SetActive(false);
					});
				}
				else
				{
					RectTransform realTime_rct = goPanelRealTime.transform.GetComponent<RectTransform>();
					realTime_rct.anchoredPosition = new Vector2(0 - realTime_rct.transform.Find("Panel").GetComponent<RectTransform>().rect.width, realTime_rct.anchoredPosition.y);
					goPanelRealTime.SetActive(true);
					realTime_rct.DOAnchorPosX(0, intervalOpenMenu);
				}
				break;
			case "文字聊天":
				await GF.UI.OpenUIFormAwait(UIViews.ChatPanel);
				break;
			case "消息记录":
				await GF.UI.OpenUIFormAwait(UIViews.MessagePanel);
				GF.UI.CloseUIForms(UIViews.BringTips);
				break;
			case "退出牌局":
				nnProcedure.Send_LeaveDeskRq();
				break;
			case "站起围观":
				nnProcedure.Send_SitUpRq();
				break;
			case "下注1":
				nnProcedure.Send_BetCoin(float.Parse(textBetCoin[0].text));
				break;
			case "下注2":
				nnProcedure.Send_BetCoin(float.Parse(textBetCoin[1].text));
				break;
			case "下注3":
				nnProcedure.Send_BetCoin(float.Parse(textBetCoin[2].text));
				break;
			case "下注4":
				nnProcedure.Send_BetCoin(float.Parse(textBetCoin[3].text));
				break;
			case "解散房间":
				Util.GetInstance().OpenConfirmationDialog("解散房间", "确定要解散房间?", () =>
						{
							GF.LogInfo("解散房间");
							nnProcedure.Send_DisMissDeskRq();
						});
				break;
			case "黑名单":
				GF.LogInfo("黑名单");
				await GF.UI.OpenUIFormAwait(UIViews.GameBlackPlayerPanel);
				break;
			case "取消离桌":
				nnProcedure.Send_CancelLeaveRq();
				break;
			case "暂停房间":
				nnProcedure.Send_PauseDeskRq(1);
				break;
			case "恢复房间":
				nnProcedure.Send_PauseDeskRq(0);
				break;
			case "购买时长":
				GameSettingNN setting = goPanelSetting.GetComponent<GameSettingNN>();
				float fen = setting.SD_TimeArry[(int)setting.SD_TimeSlider.value] * 60;
				nnProcedure.Send_DeskContinuedTimeRq((int)fen);
				break;
			case "等待队列":
				goPanelWait.gameObject.SetActive(true);
				break;
			case "关闭队列":
				goPanelWait.gameObject.SetActive(false);
				break;
			case "加入队列":
				if (userDataModel.VipState == 0)
				{
					GF.UI.ShowToast("VIP专享功能");
					return;
				}
				DeskPlayer selfplayer = nnProcedure.GetSelfPlayer();
				if (nnProcedure.enterNiuNiuDeskRs.DeskPlayers.Count < nnProcedure.enterNiuNiuDeskRs.BaseConfig.PlayerNum)
				{
					GF.UI.ShowToast("还有空位");
					return;
				}
				if (selfplayer == null)
				{
					if (goPanelWait.waitPlayers.Any(basePlayer => basePlayer.PlayerId == Util.GetMyselfInfo().PlayerId))
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
			case "TableSetButton":
				await GF.UI.OpenUIFormAwait(UIViews.ShopPanel);
				break;
			case "座位0":
				nnProcedure.Send_SitDownRq((Position)seatlist[0].seatpos);
				break;
			case "座位1":
				nnProcedure.Send_SitDownRq((Position)seatlist[1].seatpos);
				break;
			case "座位2":
				nnProcedure.Send_SitDownRq((Position)seatlist[2].seatpos);
				break;
			case "座位3":
				nnProcedure.Send_SitDownRq((Position)seatlist[3].seatpos);
				break;
			case "座位4":
				nnProcedure.Send_SitDownRq((Position)seatlist[4].seatpos);
				break;
			case "座位5":
				nnProcedure.Send_SitDownRq((Position)seatlist[5].seatpos);
				break;
			case "座位6":
				nnProcedure.Send_SitDownRq((Position)seatlist[6].seatpos);
				break;
			case "座位7":
				nnProcedure.Send_SitDownRq((Position)seatlist[7].seatpos);
				break;
			case "座位8":
				nnProcedure.Send_SitDownRq((Position)seatlist[8].seatpos);
				break;
			case "亮牌":
				nnProcedure.Send_ShowCard();
				break;
			case "打开奖池":
				uiParams.Set<VarInt32>("methodType", (int)MethodType.NiuNiu);
				uiParams.Set<VarByteArray>("deskConfig", nnProcedure.enterNiuNiuDeskRs.BaseConfig.ToByteArray());
				await GF.UI.OpenUIFormAwait(UIViews.GameJackpotPanel, uiParams);
				break;
			case "wpk":
				uiParams.Set<VarByteArray>("deskConfig", nnProcedure.enterNiuNiuDeskRs.BaseConfig.ToByteArray());
				await GF.UI.OpenUIFormAwait(UIViews.GameWPKPanel, uiParams);
				break;
		}
	}

	/// <summary>
	/// 抢庄
	/// </summary>
	/// <param name="index"></param>
	public void SendRobBanker(int index)
	{
		if (Util.IsClickLocked(0.1f)) return;
		// 执行按钮点击动画效果
		var button = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
		if (button != null)
		{
			button.transform.DOScale(0.9f, 0.1f).OnComplete(() =>
			{
				button.transform.DOScale(1f, 0.1f);
			});
		}
		nnProcedure.Send_RobBanker(index);
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
					Util.UpdateTableColor(transform.GetComponent<Image>());
					break;
				}
			}
		}
	}

	public void Function_BuyVipRs(MessageRecvData data)
	{
		GF.LogInfo("购买VIP");
		Msg_BuyVipRs ack = Msg_BuyVipRs.Parser.ParseFrom(data.Data);
		userDataModel.VipState = ack.VipState;
		userDataModel.VipEndTime = ack.VipEndTime;
		GF.UI.ShowToast("购买Vip成功", 2);
	}

	private void RefreshGame()
	{
		textRoomName.text = "[" + nnProcedure.enterNiuNiuDeskRs.BaseConfig.DeskName + "]";
		textRoomID.text = "房间ID:" + nnProcedure.deskID;
		textBaseCoin.text = nnProcedure.enterNiuNiuDeskRs.BaseConfig.BaseCoin.ToString();
		textMinBringIn.text = "最低买入:" + nnProcedure.enterNiuNiuDeskRs.BaseConfig.MinBringIn;
		ipGps.text = nnProcedure.enterNiuNiuDeskRs.BaseConfig.IpLimit && nnProcedure.enterNiuNiuDeskRs.BaseConfig.GpsLimit ? "IP/GPS限制" :
							  (nnProcedure.enterNiuNiuDeskRs.BaseConfig.IpLimit ? "IP限制" : "") +
							  (nnProcedure.enterNiuNiuDeskRs.BaseConfig.GpsLimit ? "GPS限制" : "");
		ipGps.gameObject.SetActive(nnProcedure.enterNiuNiuDeskRs.BaseConfig.IpLimit || nnProcedure.enterNiuNiuDeskRs.BaseConfig.GpsLimit);
		textNiuRobBanker.text = nnProcedure.enterNiuNiuDeskRs.NiuConfig.NoNiuRobBanker ? "无牛禁抢庄" : "无牛能抢庄";
		textMaxRate.text = "特殊牌型" + (nnProcedure.enterNiuNiuDeskRs.NiuConfig.DoubleRule != 5 ? 10 : 15) + "倍";
		textRogueRate.gameObject.SetActive(nnProcedure.enterNiuNiuDeskRs.NiuConfig.OpenRogue);
		textCoin.text = Util.Sum2String(userDataModel.Gold);
		textDiamond.text = Util.Sum2String(userDataModel.Diamonds);
		// BringManager.GetInstance().ShowBringInfoList();
		goButtonStart.SetActive(false);
		if (nnProcedure.enterNiuNiuDeskRs.DeskState == DeskState.WaitStart && nnProcedure.IsRoomOwner())
		{
			goButtonStart.SetActive(true);
		}
		jackPotObj.SetActive(nnProcedure.enterNiuNiuDeskRs.BaseConfig.Jackpot);
		if (nnProcedure.enterNiuNiuDeskRs.BaseConfig.Jackpot) UpdateJackpot();
		wpkBtn.SetActive(nnProcedure.enterNiuNiuDeskRs.BaseConfig.OpenSafe);

		DeskPlayer selfplayer = nnProcedure.GetSelfPlayer();
		if (selfplayer != null)
		{
			seatlist[0].ShowCard(selfplayer.HandCard.ToList(), false);
			ReSetCardList(selfplayer.HandCard.ToList());
		}
		goNiuType.SetActive(false);
		goRandomBanker.SetActive(false);
		goPanelRobbanker.SetActive(false);
		goPanelBetCoin.SetActive(false);
		goPanelShowCard.SetActive(false);
		goPanelFlipCard.SetActive(false);
		imageBobRanker.gameObject.SetActive(false);
		textMaxRatio.gameObject.SetActive(false);
		switch (nnProcedure.enterNiuNiuDeskRs.State)
		{
			case NiuNiuState.Robbanker:
				goPanelTimer.SetActive(true);
				starttime = nnProcedure.enterNiuNiuDeskRs.StartTime;
				endtime = nnProcedure.enterNiuNiuDeskRs.NextTime;
				SetTimer();
				break;
			case NiuNiuState.BetCoin:
				textMaxRatio.gameObject.SetActive(true);
				textMaxRatio.text = "最大倍数:" + nnProcedure.enterNiuNiuDeskRs.MaxRate;
				break;
			case NiuNiuState.ShowCard:
				textMaxRatio.gameObject.SetActive(true);
				textMaxRatio.text = "最大倍数:" + nnProcedure.enterNiuNiuDeskRs.MaxRate;
				break;
		}
		if (nnProcedure.enterNiuNiuDeskRs.DeskState == DeskState.Pause)
		{
			goPanelPause.SetActive(true);
			if (nnProcedure.IsRoomOwner())
			{
				goPause1.SetActive(true);
				goPause2.SetActive(false);
			}
			else
			{
				goPause1.SetActive(false);
				goPause2.SetActive(true);
			}
		}
		else
		{
			goPanelPause.SetActive(false);
		}
	}

	public void ReSetCardList(List<int> handCard)
	{
		// GF.LogWarning("ReSetCardList:" + string.Join(",", handCard));
		for (int i = 0; i < cardlist.Count; i++)
		{
			if (handCard.Count > i)
			{
				cardlist[i].Init(handCard[i]);
				cardlist[i].gameObject.SetActive(true);
			}
			else
			{
				cardlist[i].gameObject.SetActive(false);
			}
			cardborder[i].SetActive(false);
		}
	}

	private void OnUserDataChanged(object sender, GameEventArgs e)
	{
		var args = e as UserDataChangedEventArgs;
		switch (args.Type)
		{
			case UserDataType.MONEY:
				textCoin.text = Util.Sum2String(userDataModel.Gold);
				GF.LogInfo("收到 金币变动事件.", userDataModel.Gold);
				break;
			case UserDataType.DIAMOND:
				textDiamond.text = Util.Sum2String(userDataModel.Diamonds);
				GF.LogInfo("收到 钻石变动事件.", userDataModel.Diamonds.ToString());
				break;
			case UserDataType.VipState:
				DeskPlayer deskplayer = nnProcedure.GetPosPlayer(1);
				if (deskplayer == null) return;
				if (deskplayer.BasePlayer.PlayerId == userDataModel.PlayerId)
				{
					seatlist[0].goAvatarInfo.Init(userDataModel);
				}
				break;
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
		GlobalManager.GetInstance().GetJackpotData(nnProcedure.enterNiuNiuDeskRs.BaseConfig.ClubId, out float total);
		double form = jackPotObj.GetComponent<JumpingNumberTextComponent>().number;
		jackPotObj.GetComponent<JumpingNumberTextComponent>().Change(form, total);
	}

	void RefreshWait()
	{
		// int ncount = 0;
		// foreach (DeskPlayer deskplayer in nnProcedure.enterNiuNiuDeskRs.DeskPlayers) {
		// 	if (deskplayer.Pos != Position.NoSit) {
		// 		ncount++;
		// 	}
		// }
		// if (ncount == nnProcedure.enterNiuNiuDeskRs.BaseConfig.PlayerNum) {
		// 	goButtonWait.SetActive(true);
		// }
		// else {
		// 	goButtonWait.SetActive(false);
		// }
	}

	private void LateRefresh(DeskPlayer dp)
	{
		for (int i = 0; i < 9; i++)
		{
			if (seatlist[i].seatpos == (int)Position.NoSit)
			{
				seatlist[i].gameObject.SetActive(false); continue;
			}
			seatlist[i].gameObject.SetActive(true);
			seatlist[i].transform.localPosition = GetPosition(i);
		}
		goAction.transform.localPosition = GetPosition(0);

		goPanelRobbanker.SetActive(false);
		goPanelBetCoin.SetActive(false);
		goPanelShowCard.SetActive(false);
		goPanelCancelLeave.SetActive(false);
		foreach (DeskPlayer deskplayer in nnProcedure.enterNiuNiuDeskRs.DeskPlayers)
		{
			SeatInfo seatinfo = GetPos(deskplayer.Pos);
			if (seatinfo == null) continue;
			seatinfo.EnterPlayer(deskplayer);
			if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId) == true)
			{
				seatinfo.ShowCard(deskplayer.HandCard.ToList(), false);
				ReSetCardList(deskplayer.HandCard.ToList());
				for (var i = 0; i < deskplayer.HandCard.Count; i++)
				{
					cardlist[i].transform.localPosition = origincardpos[i];
				}
				goPanelCancelLeave.SetActive(deskplayer.State == PlayerState.OffLine);
				if (deskplayer.State == PlayerState.WaitBring && float.Parse(deskplayer.Coin) <= 0 && dp != null && dp.BasePlayer.PlayerId == deskplayer.BasePlayer.PlayerId)
				{
					seatinfo.goLeave.SetActive(true);
					seatinfo.goLeaveText.SetActive(false);
				}
				switch (nnProcedure.enterNiuNiuDeskRs.State)
				{
					case NiuNiuState.Robbanker:
						if (deskplayer.Pos != Position.NoSit && deskplayer.InGame == true)
						{
							goPanelRobbanker.SetActive(true);
							for (int i = 0; i < goRobBanker.Count; i++)
							{
								goRobBanker[i].SetActive(i < nnProcedure.enterNiuNiuDeskRs.Option.Count);
							}
						}
						break;
					case NiuNiuState.BetCoin:
						if (deskplayer.Pos != Position.NoSit && deskplayer.InGame == true)
						{
							goPanelBetCoin.SetActive(true);
							for (int i = 0; i < goBetCoin.Count; i++)
							{
								textBetCoin[i].text = i < nnProcedure.enterNiuNiuDeskRs.Option.Count ? nnProcedure.enterNiuNiuDeskRs.Option[i].ToString() : "";
								goBetCoin[i].SetActive(i < nnProcedure.enterNiuNiuDeskRs.Option.Count);
							}
							for (int i = 0; i < goRobBanker.Count; i++)
							{
								goRobBanker[i].SetActive(i < nnProcedure.enterNiuNiuDeskRs.Option.Count);
							}
						}
						break;
					case NiuNiuState.ShowCard:
						if (deskplayer.Pos != Position.NoSit && deskplayer.InGame == true && deskplayer.AutoFollow == false)
						{
							goPanelShowCard.SetActive(true);
						}
						if ((deskplayer.IsLook == true && deskplayer.InGame == true) ||
							(deskplayer.ComposeCard != null && deskplayer.ComposeCard.Count > 0 && deskplayer.InGame == true))
						{
							goPanelShowCard.SetActive(false);
							ShowNiu(deskplayer.Niu, deskplayer.ComposeCard.ToList(), deskplayer.HandCard.ToList());
						}
						break;
				}
			}
			else
			{
				seatinfo.ShowCard(deskplayer.HandCard.ToList());
				seatinfo.goLeave.SetActive(deskplayer.State != PlayerState.OnLine);
				seatinfo.SetLeaveState(deskplayer.State);
			}

			seatinfo.goBanker.SetActive(deskplayer.IsBanker);
			if (deskplayer.IsBanker == true && deskplayer.InGame == true)
			{
				seatinfo.ShowImageNumber(deskplayer.RobBanker);
			}
			switch (nnProcedure.enterNiuNiuDeskRs.State)
			{
				case NiuNiuState.Robbanker:
					if (deskplayer.IsBanker == true)
						seatinfo.RobBanker(deskplayer.RobBanker);
					break;
				case NiuNiuState.BetCoin:
					if (deskplayer.IsBanker == false)
					{
						seatinfo.goChip.SetActive(float.Parse(deskplayer.BetCoin) > 0);
						seatinfo.textChip.text = Util.Sum2String(deskplayer.BetCoin);
					}
					break;
				case NiuNiuState.ShowCard:
					if (deskplayer.IsBanker == false)
					{
						seatinfo.goChip.SetActive(float.Parse(deskplayer.BetCoin) > 0);
						seatinfo.textChip.text = Util.Sum2String(deskplayer.BetCoin);
					}
					if (
						(deskplayer.IsLook == true && deskplayer.InGame == true) ||
						(deskplayer.ComposeCard != null && deskplayer.ComposeCard.Count > 0 && deskplayer.InGame == true))
					{
						if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId) == false)
						{
							seatinfo.ShowNiu(deskplayer.Niu, deskplayer.ComposeCard.ToList());
						}
					}
					break;
			}
			seatinfo.goLeave.SetActive(deskplayer.State != PlayerState.OnLine);
			seatinfo.SetLeaveState(deskplayer.State);
		}
		GF.LogInfo("座位转圈的动画结束动画更新完成");
	}

	// public void Syn_BringIn(Syn_DeskPlayerInfo ack) {
	// 	DeskPlayer deskplayer = nnProcedure.GetPlayerByID(ack.Player.BasePlayer.PlayerId);
	// 	if (deskplayer == null) return;
	// 	deskplayer.Coin = ack.Player.Coin;
	// 	deskplayer.ClubId = ack.Player.ClubId;
	// 	GF.LogInfo("带入:" , deskplayer.Coin);
	// 	SeatInfo seatinfo = GetPos(deskplayer.Pos);
	// 	if (seatinfo == null) return;
	// 	seatinfo.EnterPlayer(deskplayer);
	// 	seatinfo.goLeave.SetActive(false);
	// 	seatinfo.goLeaveText.SetActive(deskplayer.State == PlayerState.OffLine);
	// }

	public long starttime = 0, endtime = 0, remaintime = 0;
	public void SetTimer()
	{
		int showtime = (int)(endtime - Util.GetServerTime()) / 1000;
		if (showtime < 0) showtime = 0;
		textTimer.text = showtime.ToString();
		if (endtime - starttime != 0)
		{
			float value = (float)(Util.GetServerTime() - starttime) / (float)(endtime - starttime);
			if (value > 1) value = 1;
			aniTimer.fillAmount = value;
		}
	}

	public void Syn_DeskStart(Msg_SynDeskStart ack)
	{
		foreach (DeskPlayer deskplayer in nnProcedure.enterNiuNiuDeskRs.DeskPlayers)
		{
			deskplayer.InGame = false;
			deskplayer.HandCard.Clear();
		}
		for (var j = 0; j < 9; j++)
		{
			for (var i = 0; i < 5; i++)
			{
				seatlist[j].cardlist[i].gameObject.SetActive(false);
				seatlist[j].cardborder[i].SetActive(false);
			}
		}
		foreach (Msg_InDesk indesk in ack.InDesk)
		{
			GF.LogInfo("玩家开始桌子:", indesk.Pos.ToString());
			DeskPlayer deskplayer = nnProcedure.GetPosPlayer((int)indesk.Pos);
			if (deskplayer == null) continue;
			deskplayer.InGame = indesk.InGame;
			deskplayer.State = PlayerState.OnLine;
			SeatInfo seatinfo = GetPos(indesk.Pos);
			if (seatinfo == null) continue;
			seatinfo.goLeave.SetActive(deskplayer.State != PlayerState.OnLine);
			seatinfo.SetLeaveState(deskplayer.State);
		}
		nnProcedure.enterNiuNiuDeskRs.DeskState = DeskState.StartRun;
	}

	private void ShowNiu(int Niu, List<int> compose, List<int> handcard)
	{
		imageNiuType.SetSprite("NN/CardType/" + ShowType(Niu) + ".png", true);
		goNiuType.SetActive(true);
		ReSetCardList(handcard);
		for (var i = 0; i < handcard.Count; i++)
		{
			cardlist[i].transform.localPosition = origincardpos[i];
		}
		Dictionary<int, int> shown = new();
		for (int i = 0; i < compose.Count; i++)
		{
			for (int j = 0; j < 5; j++)
			{
				if (seatlist[0].cardnumber[j] == compose[i] && !shown.ContainsKey(j))
				{
					shown[j] = 1;
					cardborder[j].SetActive(true);
					cardlist[j].transform.localPosition -= new Vector3(0, -20, 0);
					if (Niu >= 7)
					{
						goNiuEffect1.SetActive(true);
						goNiuEffect2.SetActive(true);
					}
					break;
				}
			}
		}
	}

	private string ShowType(int n)
	{
		switch (n)
		{
			case 0:
				return "paixing_niu0";
			case 1:
				return "paixing_niu1";
			case 2:
				return "paixing_niu2";
			case 3:
				return "paixing_niu3";
			case 4:
				return "paixing_niu4";
			case 5:
				return "paixing_niu5";
			case 6:
				return "paixing_niu6";
			case 7:
				return "paixing_niu7";
			case 8:
				return "paixing_niu8";
			case 9:
				return "paixing_niu9";
			case 10:
				return "paixing_niuniu";
			case 11:
				return "paixing_niushunzi";
			case 12:
				return "paixing_niuwuhua";
			case 13:
				return "paixing_niutonghua";
			case 14:
				return "paixing_niuhulu";
			case 15:
				return "paixing_niuzhadan";
			case 16:
				return "paixing_niuwuxiao";
			case 17:
				return "paixing_niutonghuashun";
			default:
				break;
		}
		return "";
	}

	public void Syn_SendGift(long basePlayerID, long toPlayerId, string gift)
	{
		SeatInfo from = GetPlayerID(basePlayerID);
		SeatInfo to = GetPlayerID(toPlayerId);
		if (to == null) return;
		GF.UI.LoadPrefab(AssetsPath.GetPrefab("UI/Chat/" + gift), (gameObject) =>
		{
			if (gameObject == null) return;
			GameObject giftObj = Instantiate((gameObject as GameObject), transform);
			if (from == null)
			{
				giftObj.transform.localPosition = goButtonEmojiStart.transform.localPosition;
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
		GF.LogInfo("礼物");
	}

	public void Reset()
	{
		for (var i = 0; i < 5; i++)
		{
			cardlist[i].Init(-1);
			cardlist[i].gameObject.SetActive(false);
			cardborder[i].SetActive(false);
		}
		foreach (SeatInfo seatinfo in seatlist)
		{
			seatinfo.Reset();
		}
		foreach (DeskPlayer deskplayer in nnProcedure.enterNiuNiuDeskRs.DeskPlayers)
		{
			deskplayer.IsBanker = false;
			deskplayer.HandCard.Clear();
		}
		goNiuType.SetActive(false);
		goNiuEffect1.SetActive(false);
		goNiuEffect2.SetActive(false);
	}

	SeatInfo GetPos(int pos)
	{
		foreach (SeatInfo seatinfo in seatlist)
		{
			if (seatinfo.seatpos == pos)
			{
				return seatinfo;
			}
		}
		return null;
	}

	SeatInfo GetPos(Position pos)
	{
		foreach (SeatInfo seatinfo in seatlist)
		{
			if (seatinfo.seatpos == (int)pos)
			{
				return seatinfo;
			}
		}
		return null;
	}

	SeatInfo GetPlayerID(long playerid)
	{
		DeskPlayer deskplayer = nnProcedure.GetPlayerByID(playerid);
		if (deskplayer == null) return null;
		return GetPos(deskplayer.Pos);
	}

	public void Function_SingleGameRecord(MessageRecvData data)
	{
		GF.LogInfo("收到单局战绩");
		brecord = true;
	}

	public void Function_SynNotifyDeskLessTime(MessageRecvData data)
	{
		Msg_SynDeskLessFive ack = Msg_SynDeskLessFive.Parser.ParseFrom(data.Data);
		GF.UI.ShowToast("5分钟后将解散房间");
	}

	public void NewPlayerInSeat(DeskPlayer deskplayer, bool bringin)
	{
		foreach (DeskPlayer dp in nnProcedure.enterNiuNiuDeskRs.DeskPlayers)
		{
			if (dp.Pos == deskplayer.Pos)
			{
				nnProcedure.enterNiuNiuDeskRs.DeskPlayers.Remove(dp);
				break;
			}
		}
		nnProcedure.enterNiuNiuDeskRs.DeskPlayers.Add(deskplayer);
		SeatInfo seatinfo = GetPos(deskplayer.Pos);
		if (seatinfo == null)
		{
			GF.LogWarning("新玩家坐下失败：座位信息为空，", "位置=" + deskplayer.Pos);
			return;
		}

		if (deskplayer.BasePlayer.PlayerId == userDataModel.PlayerId)
		{
			GF.LogInfo("玩家坐下，触发座位转动，", "位置=" + deskplayer.Pos);
			MoveSeatAnim(deskplayer);
		}
		else
		{
			seatinfo.EnterPlayer(deskplayer);
		}

		seatinfo.goLeave.SetActive(deskplayer.State != PlayerState.OnLine);
		seatinfo.SetLeaveState(deskplayer.State);
		if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId) == true && bringin == true)
		{
			BringIn(bringin);
		}
		RefreshWait();
	}

	public void LeaveSeat(int Pos)
	{
		SeatInfo seatinfo = GetPos(Pos);
		if (seatinfo == null) return;
		DeskPlayer deskplayer = nnProcedure.GetPosPlayer(Pos);
		if (deskplayer == null)
		{
			GF.LogWarning("离开座位异常：未找到座位上的玩家信息，位置=" + Pos);
			return;
		}

		GF.LogInfo("玩家离开座位，", "玩家ID=" + deskplayer.BasePlayer.PlayerId + "，位置=" + Pos);

		// 如果是当前玩家离开，关闭取消离开的UI
		if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId))
		{
			goPanelCancelLeave.SetActive(false);
			GF.LogInfo("当前玩家离开座位");
		}

		// 清理座位显示信息
		seatinfo.LeavePlayer();

		// 从玩家列表中移除此玩家
		bool removed = false;
		for (var i = 0; i < nnProcedure.enterNiuNiuDeskRs.DeskPlayers.Count; i++)
		{
			if ((int)nnProcedure.enterNiuNiuDeskRs.DeskPlayers[i].Pos == Pos)
			{
				nnProcedure.enterNiuNiuDeskRs.DeskPlayers.RemoveAt(i);
				removed = true;
				break;
			}
		}

		if (!removed)
		{
			GF.LogWarning("离开座位异常：未能从玩家列表中移除玩家，", "位置=" + Pos);
		}

		RefreshWait();
	}
	bool _isMoveSeatAnim = false;

	public void MoveSeatAnim(DeskPlayer dp)
	{
		if (nnProcedure != null && nnProcedure.BankerCoroutine != null)
		{
			CoroutineRunner.Instance.StopCoroutine(nnProcedure.BankerCoroutine);
		}
		RefreshGame();
		SeatInfo MySeatInfo = null;
		foreach (var seatinfo in seatlist)
		{
			DeskPlayer deskplayer = nnProcedure.GetPosPlayer(seatinfo.seatpos);
			if (deskplayer == null)
			{
				seatinfo.LeavePlayer();
				continue;
			}
			if (deskplayer.BasePlayer.PlayerId == userDataModel.PlayerId)
			{
				MySeatInfo = seatinfo;
				GF.LogInfo("找到玩家当前座位", ": " + seatinfo.seatpos);
				break;
			}
		}
		if (MySeatInfo == null)
		{
			GF.LogInfo("座位旋转异常：未找到玩家座位信息，跳过旋转动画");
			LateRefresh(dp);
			return;
		}

		// 如果已经在主位置，无需旋转
		if (MySeatInfo.seatpos == seatlist[0].seatpos)
		{
			GF.LogInfo("玩家已在正确位置，无需旋转: ", "" + MySeatInfo.seatpos);
			LateRefresh(dp);
			return;
		}

		GF.LogInfo("开始执行座位旋转动画，从位置 " + MySeatInfo.seatpos + " 到位置 " + seatlist[0].seatpos);

		// 先隐藏所有座位的卡牌
		for (var i = 0; i < 9; i++)
		{
			SeatInfo seatinfo = seatlist[i];
			for (int j = 0; j < 5; j++)
			{
				seatinfo.cardlist[j].gameObject.SetActive(false);
				seatinfo.cardborder[j].SetActive(false);
			}
		}

		if (_isMoveSeatAnim)
		{
			return;
		}
		_isMoveSeatAnim = true;

		// 执行第一段动画：所有座位移动到中心点
		Sequence seq1 = DOTween.Sequence();
		for (var i = 0; i < 9; i++)
		{
			SeatInfo seatinfo = seatlist[i];
			seq1.Join(seatinfo.transform.DOLocalMove(goButtonStart.transform.localPosition, intervalSeat));
		}

		seq1.OnComplete(() =>
		{
			// 清理所有座位玩家信息
			foreach (SeatInfo si in seatlist)
			{
				si.LeavePlayer();
			}

			GF.LogInfo("座位重排中：", "玩家位置=" + MySeatInfo.seatpos + "，玩家数量=" + nnProcedure.enterNiuNiuDeskRs.BaseConfig.PlayerNum);
			InitPosition(nnProcedure.enterNiuNiuDeskRs.BaseConfig.PlayerNum, (Position)MySeatInfo.seatpos);

			// 执行第二段动画：座位从中心点移动到新的位置
			Sequence seq2 = DOTween.Sequence();
			for (int i = 0; i < 9; i++)
			{
				SeatInfo seatinfo = seatlist[i];
				seq2.Join(seatinfo.transform.DOLocalMove(GetPosition(i), intervalSeat));
			}
			goAction.transform.localPosition = GetPosition(0);

			seq2.OnComplete(() =>
			{
				_isMoveSeatAnim = false;
				GF.LogInfo("座位旋转完成");
				LateRefresh(dp);
			});
		});
	}

	public void BringIn(bool leave)
	{
		if (BringUI.activeSelf == true)
		{
			return;
		}
		;
		BringUI.GetComponent<BringUI>().ShowBring(leave);
	}

	public IEnumerator Syn_DealCard(List<DealCard> cards)
	{
		_ = GF.Sound.PlayEffect("qipai.mp3");
		imageBobRanker.gameObject.SetActive(false);
		for (var i = 0; i < 5; i++)
		{
			cardlist[i].Init(-1);
			cardlist[i].gameObject.SetActive(false);
			cardborder[i].SetActive(false);
		}
		foreach (SeatInfo seatinfo in seatlist)
		{
			seatinfo.goBanker.SetActive(false);
			seatinfo.ShowImageNumber();
			seatinfo.goNiuType.SetActive(false);
			seatinfo.goNiuEffect1.SetActive(false);
			seatinfo.goNiuEffect2.SetActive(false);
			seatinfo.imageBobRanker.gameObject.SetActive(false);
			seatinfo.cardlist[0].gameObject.SetActive(false);
			seatinfo.cardlist[1].gameObject.SetActive(false);
			seatinfo.cardlist[2].gameObject.SetActive(false);
			seatinfo.cardlist[3].gameObject.SetActive(false);
			seatinfo.cardlist[4].gameObject.SetActive(false);
			seatinfo.cardlist[0].transform.localPosition = seatinfo.vecCardCenter;
		}
		foreach (DealCard dealcard in cards)
		{
			SeatInfo seatinfo = GetPos(dealcard.Pos);
			if (seatinfo == null) continue;
			if (Util.IsMySelf(dealcard.PlayerId))
			{
				cardlist[0].Init(-1);
				cardlist[0].gameObject.SetActive(true);
				cardlist[1].gameObject.SetActive(false);
				cardlist[2].gameObject.SetActive(false);
				cardlist[3].gameObject.SetActive(false);
				cardlist[4].gameObject.SetActive(false);
				cardlist[0].transform.localPosition = vecCardCenter;
			}
		}

		Sequence seq1 = DOTween.Sequence();
		foreach (DealCard dealcard in cards)
		{
			SeatInfo seatinfo = GetPos(dealcard.Pos);
			if (seatinfo == null) continue;
			DeskPlayer deskplayer = nnProcedure.GetPosPlayer((int)dealcard.Pos);
			if (deskplayer == null) continue;
			deskplayer.InGame = true;
			deskplayer.HandCard.Clear();
			foreach (var item in dealcard.HandCards)
			{
				deskplayer.HandCard.Add(item);
			}
			if (Util.IsMySelf(dealcard.PlayerId))
			{
				if (origincardpos.Count > 0)
				{
					seq1.Join(cardlist[0].transform.DOLocalMove(origincardpos[0], intervalCard1));
				}
			}
			else
			{
				if (seatinfo.origincardpos.Count > 0)
				{
					seatinfo.cardlist[0].Init(-1);
					seatinfo.cardlist[0].gameObject.SetActive(true);
					seq1.Join(seatinfo.cardlist[0].transform.DOLocalMove(seatinfo.origincardpos[0], intervalCard1));
				}
			}
		}
		yield return seq1.WaitForCompletion();

		foreach (DealCard dealcard in cards)
		{
			if (Util.IsMySelf(dealcard.PlayerId))
			{
				seatlist[0].ShowCard(dealcard.HandCards.ToList(), false);
				ReSetCardList(dealcard.HandCards.ToList());
				for (var i = 0; i < dealcard.HandCards.Count; i++)
				{
					cardlist[i].transform.localPosition = origincardpos[0];
				}
			}
			else
			{
				SeatInfo seatinfo = GetPos(dealcard.Pos);
				if (seatinfo == null) continue;
				for (var i = 0; i < dealcard.HandCards.Count; i++)
				{
					seatinfo.cardlist[i].Init(-1);
					seatinfo.cardlist[i].gameObject.SetActive(true);
					if (seatinfo.origincardpos.Count > 0)
					{
						seatinfo.cardlist[i].transform.localPosition = seatinfo.origincardpos[0];
					}
				}
			}
		}

		Sequence seq2 = DOTween.Sequence();
		foreach (DealCard dealcard in cards)
		{
			if (Util.IsMySelf(dealcard.PlayerId))
			{
				for (var i = 0; i < dealcard.HandCards.Count; i++)
				{
					if (origincardpos.Count > i)
					{
						seq2.Join(cardlist[i].transform.DOLocalMove(origincardpos[i], intervalCard2));
					}
				}
			}
			else
			{
				SeatInfo seatinfo = GetPos(dealcard.Pos);
				if (seatinfo == null) continue;
				for (var i = 0; i < dealcard.HandCards.Count; i++)
				{
					if (seatinfo.origincardpos.Count > i)
					{
						seq2.Join(seatinfo.cardlist[i].transform.DOLocalMove(seatinfo.origincardpos[i], intervalCard2));
					}
				}
			}
		}
		yield return seq2.WaitForCompletion();

		var selfplayer = nnProcedure.GetSelfPlayer();
		if (selfplayer != null && selfplayer.Pos != Position.NoSit && selfplayer.InGame == true)
		{
			goPanelRobbanker.SetActive(true);
		}
		foreach (DealCard dealcard in cards)
		{
			DeskPlayer deskplayer = nnProcedure.GetPosPlayer((int)dealcard.Pos);
			if (deskplayer == null) continue;
			deskplayer.HandCard.Clear();
			foreach (var card in dealcard.HandCards.ToList())
			{
				deskplayer.HandCard.Add(card);
			}
			deskplayer.ComposeCard.Clear();
		}
	}

	public void Syn_RobBanker(Syn_RobBanker ack)
	{
		foreach (SeatInfo seatinfo in seatlist)
		{
			DeskPlayer deskplayer = nnProcedure.GetPosPlayer(seatinfo.seatpos);
			if (deskplayer == null) continue;
			if (deskplayer.BasePlayer.PlayerId == ack.PlayerId)
			{
				seatinfo.RobBanker(ack.RobBanker);
				deskplayer.RobBanker = ack.RobBanker;
				break;
			}
		}
	}

	public void Syn_DeskUpdate(Syn_DeskUpdate ack)
	{
		var selfplayer = nnProcedure.GetSelfPlayer();
		goPanelRobbanker.SetActive(false);
		goPanelBetCoin.SetActive(false);
		goPanelShowCard.SetActive(false);
		switch (ack.State)
		{
			case NiuNiuState.Robbanker:
				_ = GF.Sound.PlayEffect("niuniu/action.wav");
				goButtonStart.SetActive(false);
				goPanelTimer.SetActive(true);
				starttime = nnProcedure.enterNiuNiuDeskRs.StartTime = ack.StartTime;
				endtime = nnProcedure.enterNiuNiuDeskRs.NextTime = ack.NextTime;
				nnProcedure.enterNiuNiuDeskRs.Option.Clear();
				for (var i = 0; i < ack.Option.Count; i++)
				{
					nnProcedure.enterNiuNiuDeskRs.Option.Add(ack.Option[i]);
				}
				SetTimer();
				goNiuEffect1.SetActive(false);
				goNiuEffect2.SetActive(false);
				if (selfplayer != null && selfplayer.Pos != Position.NoSit && selfplayer.InGame == true)
				{
					for (int i = 0; i < goRobBanker.Count; i++)
					{
						goRobBanker[i].SetActive(i < ack.Option.Count);
					}
					goPanelRobbanker.SetActive(true);
				}
				break;
			case NiuNiuState.BetCoin:
				_ = GF.Sound.PlayEffect("niuniu/action.wav");
				goButtonStart.SetActive(false);
				goPanelTimer.SetActive(true);
				starttime = nnProcedure.enterNiuNiuDeskRs.StartTime = ack.StartTime;
				endtime = nnProcedure.enterNiuNiuDeskRs.NextTime = ack.NextTime;
				nnProcedure.enterNiuNiuDeskRs.Option.Clear();
				for (var i = 0; i < ack.Option.Count; i++)
				{
					nnProcedure.enterNiuNiuDeskRs.Option.Add(ack.Option[i]);
				}
				SetTimer();
				foreach (SeatInfo seatinfo in seatlist)
				{
					seatinfo.imageBobRanker.gameObject.SetActive(false);
				}
				if (selfplayer != null && selfplayer.Pos != Position.NoSit && selfplayer.InGame == true)
				{
					goPanelBetCoin.SetActive(true);
					for (int i = 0; i < goBetCoin.Count; i++)
					{
						textBetCoin[i].text = i < ack.Option.Count ? ack.Option[i].ToString() : "";
						goBetCoin[i].SetActive(i < ack.Option.Count);
					}
				}
				break;
			case NiuNiuState.ShowCard:
				goButtonStart.SetActive(false);
				goPanelTimer.SetActive(true);
				starttime = nnProcedure.enterNiuNiuDeskRs.StartTime = ack.StartTime;
				endtime = nnProcedure.enterNiuNiuDeskRs.NextTime = ack.NextTime;
				nnProcedure.enterNiuNiuDeskRs.Option.Clear();
				for (var i = 0; i < ack.Option.Count; i++)
				{
					nnProcedure.enterNiuNiuDeskRs.Option.Add(ack.Option[i]);
				}
				SetTimer();
				foreach (DeskPlayer deskplayer in nnProcedure.enterNiuNiuDeskRs.DeskPlayers)
				{
					if (deskplayer.InGame == false) continue;
					if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId))
					{
						cardlist[4].transform.localPosition = vecCardCenter;
						cardlist[4].Init(-1);
						cardlist[4].gameObject.SetActive(true);
					}
					else
					{
						SeatInfo seatinfo = GetPos(deskplayer.Pos);
						if (seatinfo == null) continue;
						seatinfo.cardlist[4].transform.localPosition = seatinfo.vecCardCenter;
						seatinfo.cardlist[4].Init(-1);
						seatinfo.cardlist[4].gameObject.SetActive(true);
					}
				}
				Sequence seq = DOTween.Sequence();
				foreach (DeskPlayer deskplayer in nnProcedure.enterNiuNiuDeskRs.DeskPlayers)
				{
					if (deskplayer.InGame == false) continue;
					if (Util.IsMySelf(deskplayer.BasePlayer.PlayerId))
					{
						if (selfplayer.AutoFollow == true)
						{
							Msg_LookRq req = MessagePool.Instance.Fetch<Msg_LookRq>();
							HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3(HotfixNetworkComponent.AccountClientName, MessageID.Msg_LookRq, req);
							goPanelFlipCard.SetActive(true);
							goPanelFlipCard.transform.localScale = Vector3.zero;
							seq.Join(goPanelFlipCard.transform.DOScale(new Vector3(6f, 6f, 6f), intervalCard1));
							cardlist[4].transform.localPosition = origincardpos[4];
						}
						else
						{
							if (selfplayer != null && selfplayer.Pos != Position.NoSit)
							{
								if (origincardpos.Count > 4)
								{
									seq.Join(cardlist[4].transform.DOLocalMove(origincardpos[4], intervalCard1));
								}
							}
						}
					}
					else
					{
						SeatInfo seatinfo = GetPos(deskplayer.Pos);
						if (seatinfo.origincardpos.Count > 4 && deskplayer.InGame == true)
						{
							seq.Join(seatinfo.cardlist[4].transform.DOLocalMove(seatinfo.origincardpos[4], intervalCard1));
						}
					}
				}
				if (selfplayer != null && selfplayer.Pos != Position.NoSit && selfplayer.InGame == true && selfplayer.AutoFollow == false)
				{
					goPanelShowCard.SetActive(true);
				}
				break;
			case NiuNiuState.Settle:
				goButtonStart.SetActive(false);
				goPanelTimer.SetActive(false);
				// goPanelFlipCard.SetActive(false);
				break;
			case NiuNiuState.Wait:
				goPanelTimer.SetActive(false);
				break;
		}
		nnProcedure.enterNiuNiuDeskRs.State = ack.State;
	}

	public IEnumerator Syn_Banker(Msg_SynBanker ack)
	{
		textMaxRatio.gameObject.SetActive(false);
		goPanelTimer.SetActive(false);
		goPanelRobbanker.gameObject.SetActive(false);
		if (ack.MaxRatio != 0)
		{
			textMaxRatio.text = "最大倍数：" + ack.MaxRatio;
			textMaxRatio.gameObject.SetActive(true);
		}
		if (ack.IsRandom == true)
		{
			goRandomBanker.SetActive(true);
			DeskPlayer deskplayer = nnProcedure.GetPosPlayer((int)ack.Pos);
			SeatInfo seatinfo = GetPos(ack.Pos);
			deskplayer.RobBanker = ack.RobBanker;
			int num = 0;
			int count = ack.RandomBankers.Count;
			float time = 0.01f;
			while (true)
			{
				int index = num % count;
				var pos = ack.RandomBankers[num % ack.RandomBankers.Count];
				goRandomBanker.transform.position = GetPos(pos).objectPlayer.transform.position;
				num++;
				if (num % count == 0)
				{
					time += intervalRandomBanker;
				}
				if (time > 0.1f && pos == ack.Pos)
				{
					break;
				}
				_ = GF.Sound.PlayEffect("niuniu/jumpbanker.mp3");
				yield return new WaitForSeconds(time);
			}
			foreach (DeskPlayer dp in nnProcedure.enterNiuNiuDeskRs.DeskPlayers)
			{
				dp.IsBanker = false;
			}
			deskplayer.IsBanker = true;
			goRandomBanker.transform.position = seatinfo.objectPlayer.transform.position;
			yield return new WaitForSeconds(0.5f);
			seatinfo.goBanker.transform.localPosition = seatinfo.vecBankerCenter;
			seatinfo.goBanker.SetActive(true);
			seatinfo.goBanker.transform.DOLocalMove(seatinfo.originbankerPos, intervalBanker).OnComplete(() =>
			{
				seatinfo.ShowImageNumber(ack.RobBanker);
			});
			yield return new WaitForSeconds(1f);
			goRandomBanker.SetActive(false);
		}
		else
		{
			foreach (DeskPlayer dp in nnProcedure.enterNiuNiuDeskRs.DeskPlayers)
			{
				dp.IsBanker = false;
			}
			DeskPlayer deskplayer = nnProcedure.GetPosPlayer((int)ack.Pos);
			deskplayer.IsBanker = true;
			deskplayer.RobBanker = ack.RobBanker;
			SeatInfo seatinfo = GetPos(ack.Pos);
			seatinfo.goBanker.transform.localPosition = seatinfo.vecBankerCenter;
			seatinfo.goBanker.SetActive(true);
			seatinfo.goBanker.transform.DOLocalMove(seatinfo.originbankerPos, intervalBanker).OnComplete(() =>
			{
				seatinfo.ShowImageNumber(ack.RobBanker);
			});
		}
	}

	public void Syn_BetCoin(Syn_BetCoin ack)
	{
		SeatInfo seatinfo = GetPos(ack.Pos);
		seatinfo.goChip.SetActive(true);
		_ = GF.Sound.PlayEffect("xiazhu.mp3");
		seatinfo.textChip.gameObject.SetActive(false);
		seatinfo.goChip.transform.localPosition = seatinfo.objectPlayer.transform.localPosition;

		seatinfo.goChip.transform.DOLocalMove(seatinfo.originChipPos, intervalCoin).WaitForCompletion();

		seatinfo.textChip.text = Util.Sum2String(ack.BetCoin);
		seatinfo.textChip.gameObject.SetActive(true);
		DeskPlayer deskplayer = nnProcedure.GetPosPlayer((int)ack.Pos);
		if (deskplayer == null) return;
		deskplayer.BetCoin = ack.BetCoin;
		if (Util.IsMySelf(ack.PlayerId))
		{
			goPanelBetCoin.SetActive(false);
		}
		GF.LogInfo("下注完成");
	}

	public void Syn_PlayerState(Msg_SynPlayerState ack)
	{
		foreach (SeatInfo seatinfo in seatlist)
		{
			DeskPlayer deskplayer = nnProcedure.GetPosPlayer(seatinfo.seatpos);
			if (deskplayer == null) continue;
			if (deskplayer.BasePlayer.PlayerId != ack.PlayerId) continue;

			seatinfo.goLeave.SetActive(ack.State != PlayerState.OnLine);
			if (Util.IsMySelf(ack.PlayerId))
			{
				if (seatlist[0].seatpos == (int)deskplayer.Pos)
				{
					goPanelCancelLeave.SetActive(ack.State == PlayerState.OffLine);
				}
			}
			deskplayer.State = ack.State;
			deskplayer.LeaveTime = ack.LeaveTime;
			seatinfo.SetLeaveState(deskplayer.State);
		}
	}

	public void Syn_ShowCard(Syn_ShowCard ack)
	{
		if (Util.IsMySelf(ack.PlayerId))
		{
			DeskPlayer selfplayer = nnProcedure.GetSelfPlayer();
			if (selfplayer.AutoFollow)
			{
				PlayNiuSound(ack.Niu);
				cardlist[4].Init(ack.HandCards[4]);
				cardlist[4].transform.localPosition = origincardpos[4];
				goPanelShowCard.SetActive(false);
			}
			else
			{
				cardlist[4].transform.localPosition = origincardpos[4];
				goPanelShowCard.SetActive(false);
				cardlist[4].transform.DORotate(new Vector3(0, 90, 0), 0.2f).OnComplete(() =>
				{
					if (cardlist[4].gameObject == null) return;
					cardlist[4].Init(ack.HandCards[4]);
					cardlist[4].transform.DORotate(Vector3.zero, 0.2f).OnComplete(() =>
					{
						PlayNiuSound(ack.Niu);
						if (cardlist[4].gameObject == null) return;
						cardlist[4].transform.localRotation = Quaternion.identity;
						DeskPlayer deskplayer = nnProcedure.GetPosPlayer((int)ack.Pos);
						deskplayer.Niu = ack.Niu;
						deskplayer.ComposeCard.Clear();
						foreach (var card in ack.ComposeCards.ToList())
						{
							deskplayer.ComposeCard.Add(card);
						}
						deskplayer.HandCard.Clear();
						foreach (var card in ack.HandCards.ToList())
						{
							deskplayer.HandCard.Add(card);
						}
						seatlist[0].ShowCard(ack.HandCards.ToList(), false);
						ShowNiu(ack.Niu, ack.ComposeCards.ToList(), ack.HandCards.ToList());
						if (selfplayer.AutoFollow == true)
						{
							for (var i = 0; i < 10; i++)
							{
								FlipCard flipcard = goPanelFlipCard.GetComponent<FlipCard>();
								flipcard.UpdateCard2(i + 1);
							}
							goPanelFlipCard.SetActive(false);
						}
					});
				});
			}

		}
		else
		{
			SeatInfo seatinfo = GetPos(ack.Pos);
			for (var i = 0; i < ack.HandCards.Count; i++)
			{
				int index = i;
				seatinfo.cardlist[index].transform.localPosition = seatinfo.origincardpos[index];

				seatinfo.cardlist[index].transform.DORotate(new Vector3(0, 90, 0), 0.2f).OnComplete(() =>
				{
					if (seatinfo.cardlist[index].gameObject == null) return;
					seatinfo.cardlist[index].Init(ack.HandCards[index]);
					seatinfo.cardlist[index].transform.DORotate(Vector3.zero, 0.2f).OnComplete(() =>
					{
						if (seatinfo.cardlist[index].gameObject == null) return;
						seatinfo.cardlist[index].transform.localRotation = Quaternion.identity;
						if (index == ack.HandCards.Count - 1)
						{
							DeskPlayer deskplayer = nnProcedure.GetPosPlayer((int)ack.Pos);
							deskplayer.Niu = ack.Niu;
							deskplayer.ComposeCard.Clear();
							foreach (var card in ack.ComposeCards.ToList())
							{
								deskplayer.ComposeCard.Add(card);
							}
							deskplayer.HandCard.Clear();
							foreach (var card in ack.HandCards.ToList())
							{
								deskplayer.HandCard.Add(card);
							}
							seatinfo.ShowCard(ack.HandCards.ToList());
							seatinfo.ShowNiu(ack.Niu, ack.ComposeCards.ToList());
							PlayNiuSound(ack.Niu);
						}
					});
				});
			}
		}
	}

	public void PlayNiuSound(int niu)
	{
		if (niu == 10)
		{
			_ = GF.Sound.PlayEffect($"niuniu/NiuNiuTeXiaoYin.mp3");
		}
		else if (niu > 10)
		{
			_ = GF.Sound.PlayEffect($"niuniu/TeShuNiuTeXiaoYin.mp3");
		}
		_ = GF.Sound.PlayEffect($"niuniu/Niu{niu}.mp3");
	}

	public void Syn_AutoShowCard(Syn_AutoShowCard ack)
	{
		foreach (Syn_ShowCard showcard in ack.ShowCards.ToList())
		{
			if (Util.IsMySelf(showcard.PlayerId) == true)
			{
				cardlist[4].transform.localPosition = origincardpos[4];
				goPanelShowCard.SetActive(false);

				cardlist[4].transform.DORotate(new Vector3(0, 90, 0), 0.2f).OnComplete(() =>
				{
					if (cardlist[4].gameObject == null) return;
					cardlist[4].Init(showcard.HandCards[4]);
					cardlist[4].transform.DORotate(Vector3.zero, 0.2f).OnComplete(() =>
					{
						if (cardlist[4].gameObject == null) return;
						cardlist[4].transform.localRotation = Quaternion.identity;
						DeskPlayer deskplayer = nnProcedure.GetPosPlayer((int)showcard.Pos);
						deskplayer.Niu = showcard.Niu;
						deskplayer.ComposeCard.Clear();
						foreach (var card in showcard.ComposeCards.ToList())
						{
							deskplayer.ComposeCard.Add(card);
						}
						deskplayer.HandCard.Clear();
						foreach (var card in showcard.HandCards.ToList())
						{
							deskplayer.HandCard.Add(card);
						}
						seatlist[0].ShowCard(showcard.HandCards.ToList(), false);
						ShowNiu(showcard.Niu, showcard.ComposeCards.ToList(), showcard.HandCards.ToList());
						PlayNiuSound(showcard.Niu);
					});
				});
			}
			else
			{
				SeatInfo seatinfo = GetPos(showcard.Pos);
				if (seatinfo == null) continue;
				for (var i = 0; i < showcard.HandCards.Count; i++)
				{
					int index = i;
					seatinfo.cardlist[index].gameObject.SetActive(true);
					seatinfo.cardlist[index].transform.localPosition = seatinfo.origincardpos[index];
					seatinfo.cardlist[index].transform.DORotate(new Vector3(0, 90, 0), 0.2f).OnComplete(() =>
					{
						if (seatinfo.cardlist[index].gameObject == null) return;
						seatinfo.cardlist[index].Init(showcard.HandCards[index]);
						seatinfo.cardlist[index].transform.DORotate(Vector3.zero, 0.2f).OnComplete(() =>
						{
							if (seatinfo.cardlist[index].gameObject == null) return;
							seatinfo.cardlist[index].transform.localRotation = Quaternion.identity;
							if (index == showcard.HandCards.Count - 1)
							{
								DeskPlayer deskplayer = nnProcedure.GetPosPlayer((int)showcard.Pos);
								deskplayer.Niu = showcard.Niu;
								deskplayer.ComposeCard.Clear();
								foreach (var card in showcard.ComposeCards.ToList())
								{
									deskplayer.ComposeCard.Add(card);
								}
								deskplayer.HandCard.Clear();
								foreach (var card in showcard.HandCards.ToList())
								{
									deskplayer.HandCard.Add(card);
								}
								seatinfo.ShowCard(showcard.HandCards.ToList());
								seatinfo.ShowNiu(showcard.Niu, showcard.ComposeCards.ToList());
								PlayNiuSound(showcard.Niu);
							}
						});
					});
				}
			}
		}
		goPanelFlipCard.SetActive(false);
	}

	public IEnumerator Syn_Settle(Syn_Settle ack)
	{
		yield return new WaitForSeconds(1);
		int Banker = 0;
		foreach (DeskPlayer deskplayer in nnProcedure.enterNiuNiuDeskRs.DeskPlayers)
		{
			if (deskplayer.IsBanker == true)
			{
				Banker = (int)deskplayer.Pos;
				break;
			}
		}
		SeatInfo bankerseat = GetPos(Banker);
		if (bankerseat == null) yield break;
		foreach (Settle settle in ack.Settle)
		{
			SeatInfo seatinfo = GetPos(settle.Pos);
			if (seatinfo == null) continue;
			DeskPlayer dp = nnProcedure.GetPlayerByID(settle.PlayerId);
			if (dp != null)
			{
				dp.Coin = settle.AfterChange;
			}
			seatinfo.goChip.SetActive(false);
			seatinfo.textChip.text = Util.Sum2String(settle.CoinChange);
			seatinfo.imageProfit.SetActive(true);
			if (float.Parse(settle.CoinChange) >= 0)
			{
				seatinfo.ShowWinnerStarEff();
				seatinfo.imageProfit1.SetActive(true);
				seatinfo.imageProfit2.SetActive(false);
			}
			else
			{
				seatinfo.imageProfit1.SetActive(false);
				seatinfo.imageProfit2.SetActive(true);
			}
			if (float.Parse(settle.CoinChange) > 0)
			{
				seatinfo.textProfit.text = "+" + Util.Sum2String(settle.CoinChange);
			}
			else
			{
				seatinfo.textProfit.text = Util.Sum2String(settle.CoinChange);
			}
		}
		List<int> win = new(), lose = new();
		foreach (Settle settle in ack.Settle)
		{
			if ((int)settle.Pos == Banker) continue;
			float coin = float.Parse(settle.CoinChange);
			if (coin > 0)
			{
				win.Add((int)settle.Pos);
			}
			else if (coin < 0)
			{
				lose.Add((int)settle.Pos);
			}
		}
		coins.Clear();
		goPanelFlipCard.SetActive(false);
		foreach (Settle settle in ack.Settle)
		{
			SeatInfo seatinfo = GetPos(settle.Pos);
			if (seatinfo == null) continue;
			if ((int)settle.Pos == Banker) continue;
			float coin = float.Parse(settle.CoinChange);
			if (coin < 0)
			{
				seatinfo.textCoin.text = Util.Sum2String(settle.AfterChange);
			}
		}
		if (lose.Count > 0)
		{
			_ = GF.Sound.PlayEffect("niuniu/win.mp3");
			for (int k = 0; k < 10; k++)
			{
				for (int i = 0; i < lose.Count; i++)
				{
					SeatInfo loseseat = GetPos(lose[i]);
					if (loseseat == null) continue;
					GameObject coin = Instantiate(goChip, goChipFly.transform);
					coin.SetActive(true);
					coin.transform.position = loseseat.transform.position;
					coins.Add(coin);
					coin.transform.DOMove(bankerseat.transform.position, intervalChip).OnComplete(() =>
					{
						coin.SetActive(false);
					});
				}
				yield return new WaitForSeconds(intervalSettle1);
			}
			yield return new WaitForSeconds(intervalChip);
		}

		foreach (Settle settle in ack.Settle)
		{
			SeatInfo seatinfo = GetPos(settle.Pos);
			if (seatinfo == null) continue;
			if ((int)settle.Pos != Banker) continue;
			bankerseat.textCoin.text = Util.Sum2String(settle.AfterChange);
		}

		if (win.Count > 0)
		{
			_ = GF.Sound.PlayEffect("niuniu/win.mp3");
			for (int k = 0; k < 10; k++)
			{
				for (int i = 0; i < win.Count; i++)
				{
					SeatInfo winseat = GetPos(win[i]);
					if (winseat == null) continue;
					GameObject coin = Instantiate(goChip, goChipFly.transform);
					coin.SetActive(true);
					coin.transform.position = bankerseat.transform.position;
					coins.Add(coin);
					coin.transform.DOMove(winseat.transform.position, intervalChip).OnComplete(() =>
					{
						coin.SetActive(false);
					});
				}
				yield return new WaitForSeconds(intervalSettle1);
			}
			yield return new WaitForSeconds(intervalChip);
			foreach (Settle settle in ack.Settle)
			{
				SeatInfo seatinfo = GetPos(settle.Pos);
				if (seatinfo == null) continue;
				if ((int)settle.Pos == Banker) continue;
				float coin = float.Parse(settle.CoinChange);
				if (coin > 0)
				{
					seatinfo.textCoin.text = Util.Sum2String(settle.AfterChange);
				}
			}
			Function_SynCKJackPot();
		}
		yield return new WaitForSeconds(intervalSettle2);
		foreach (GameObject coin in coins)
		{
			Destroy(coin);
		}
		yield return new WaitForSeconds(2);
		if (nnProcedure.enterNiuNiuDeskRs.State != NiuNiuState.Settle)
		{
			yield break;
		}
		Reset();
	}

	public void Function_SynCKJackPot()
	{
		if (nnProcedure.msg_SynCKJackPot == null) return;
		Msg_SynCKJackPot ack = nnProcedure.msg_SynCKJackPot;
		nnProcedure.msg_SynCKJackPot = null;
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

			SeatInfo seat = GetPlayerID(playerId);
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
				GameObject boomAni = Instantiate(varJackBoomAni, goChipFly.transform);
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

	public void Syn_PlayerInfo(DeskPlayer deskplayer)
	{
		DeskPlayer dp = nnProcedure.GetPlayerByID(deskplayer.BasePlayer.PlayerId);
		if (dp == null) return;
		dp.Coin = deskplayer.Coin;
		dp.ClubId = deskplayer.ClubId;
		dp.BuyTime = deskplayer.BuyTime;
		dp.IsShow = deskplayer.IsShow;
		dp.State = deskplayer.State;
		dp.LeaveTime = deskplayer.LeaveTime;
		dp.BringNum = deskplayer.BringNum;
		SeatInfo seatinfo = GetPos((int)dp.Pos);
		if (seatinfo == null) return;
		seatinfo.textCoin.text = Util.Sum2String(deskplayer.Coin);
		seatinfo.goLeave.SetActive(deskplayer.LeaveTime > 0);
		seatinfo.SetLeaveState(deskplayer.State);
	}

	public void OnVoicePress()
	{
		if (nnProcedure.enterNiuNiuDeskRs.BaseConfig.ForbidVoice)
		{
			GF.LogInfo("禁止语音");
			GF.UI.ShowToast("禁止聊天!");
			return;
		}
		DeskPlayer seat = nnProcedure.GetSelfPlayer();
		if (seat == null)
		{
			return;
		}
		if (seat.LeaveTime > 0 || float.Parse(seat.Coin) <= 0)
		{
			return;
		}
		goPanelRecord.StartRecord(userDataModel.PlayerId, nnProcedure.deskID);
	}

	public void OnVoiceRelease()
	{
		if (nnProcedure.enterNiuNiuDeskRs.BaseConfig.ForbidVoice)
		{
			return;
		}
		goPanelRecord.StopRecord();
	}

	public void OnVoiceCancel()
	{
		if (nnProcedure.enterNiuNiuDeskRs.BaseConfig.ForbidVoice)
		{
			return;
		}
		goPanelRecord.CancelRecord();
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

	Vector3 GetPosition8(int index)
	{
		Rect rect = GetComponent<RectTransform>().rect;
		return index switch
		{
			0 => new Vector3(0, 420 - rect.height / 2, 0),
			1 => new Vector3(100 - rect.width / 2, 270 - rect.height / 4, 0),
			2 => new Vector3(100 - rect.width / 2, 120, 0),
			3 => new Vector3(100 - rect.width / 2, -30 + rect.height / 4, 0),
			4 => new Vector3(0, -330 + rect.height / 2, 0),
			5 => new Vector3(-100 + rect.width / 2, -30 + rect.height / 4, 0),
			6 => new Vector3(-100 + rect.width / 2, 120, 0),
			7 => new Vector3(-100 + rect.width / 2, 270 - rect.height / 4, 0),
			_ => Vector3.zero,
		};
	}
	Vector3 GetPosition7(int index)
	{
		Rect rect = GetComponent<RectTransform>().rect;
		return index switch
		{
			0 => new Vector3(0, 420 - rect.height / 2, 0),
			1 => new Vector3(100 - rect.width / 2, 270 - rect.height / 4, 0),
			2 => new Vector3(100 - rect.width / 2, 120, 0),
			3 => new Vector3(100 - rect.width / 2, -30 + rect.height / 4, 0),
			4 => new Vector3(-100 + rect.width / 2, -30 + rect.height / 4, 0),
			5 => new Vector3(-100 + rect.width / 2, 120, 0),
			6 => new Vector3(-100 + rect.width / 2, 270 - rect.height / 4, 0),
			_ => Vector3.zero,
		};
	}
	Vector3 GetPosition6(int index)
	{
		Rect rect = GetComponent<RectTransform>().rect;
		return index switch
		{
			0 => new Vector3(0, 420 - rect.height / 2, 0),
			1 => new Vector3(100 - rect.width / 2, 170 - rect.height / 5, 0),
			2 => new Vector3(100 - rect.width / 2, -80 + rect.height / 5, 0),
			3 => new Vector3(0, -330 + rect.height / 2, 0),
			4 => new Vector3(-100 + rect.width / 2, -80 + rect.height / 5, 0),
			5 => new Vector3(-100 + rect.width / 2, 170 - rect.height / 5, 0),
			_ => Vector3.zero,
		};
	}
	Vector3 GetPosition5(int index)
	{
		Rect rect = GetComponent<RectTransform>().rect;
		return index switch
		{
			0 => new Vector3(0, 420 - rect.height / 2, 0),
			1 => new Vector3(100 - rect.width / 2, 170 - rect.height / 6, 0),
			2 => new Vector3(100 - rect.width / 2, -80 + rect.height / 4, 0),
			3 => new Vector3(-100 + rect.width / 2, -80 + rect.height / 4, 0),
			4 => new Vector3(-100 + rect.width / 2, 170 - rect.height / 6, 0),
			_ => Vector3.zero,
		};
	}
	Vector3 GetPosition4(int index)
	{
		Rect rect = GetComponent<RectTransform>().rect;
		return index switch
		{
			0 => new Vector3(0, 420 - rect.height / 2, 0),
			1 => new Vector3(100 - rect.width / 2, 120, 0),
			2 => new Vector3(0, -330 + rect.height / 2, 0),
			3 => new Vector3(-100 + rect.width / 2, 120, 0),
			_ => Vector3.zero,
		};
	}
	Vector3 GetPosition3(int index)
	{
		Rect rect = GetComponent<RectTransform>().rect;
		return index switch
		{
			0 => new Vector3(0, 420 - rect.height / 2, 0),
			1 => new Vector3(100 - rect.width / 2, 120, 0),
			2 => new Vector3(-100 + rect.width / 2, 120, 0),
			_ => Vector3.zero,
		};
	}
	Vector3 GetPosition2(int index)
	{
		Rect rect = GetComponent<RectTransform>().rect;
		return index switch
		{
			0 => new Vector3(0, 420 - rect.height / 2, 0),
			1 => new Vector3(0, -330 + rect.height / 2, 0),
			_ => Vector3.zero,
		};
	}
	public Vector3 GetPosition(int index)
	{
		SeatInfo seatinfo = seatlist[index];
		if (seatinfo.seatpos == (int)Position.NoSit) return Vector3.zero;
		int count = 0;
		for (var i = 0; i < index; i++)
		{
			if (seatlist[i].seatpos != (int)Position.NoSit)
			{
				count++;
			}
		}
		return nnProcedure.enterNiuNiuDeskRs.BaseConfig.PlayerNum switch
		{
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

	public void Function_EnterNiuNiuDeskRs(MessageRecvData data)
	{
		nnProcedure.enterNiuNiuDeskRs = Msg_EnterNiuNiuDeskRs.Parser.ParseFrom(data.Data);
		GF.LogInfo("刷新牛牛桌子信息", nnProcedure.enterNiuNiuDeskRs.ToString());
		// 记录进入房间时的玩家位置信息
		GF.LogInfo("房间玩家数量: ", "" + nnProcedure.enterNiuNiuDeskRs.DeskPlayers.Count);
		foreach (var player in nnProcedure.enterNiuNiuDeskRs.DeskPlayers)
		{
			GF.LogInfo("房间玩家: ", "ID=" + player.BasePlayer.PlayerId +
				", 位置=" + player.Pos +
				", 是否自己=" + Util.IsMySelf(player.BasePlayer.PlayerId));
		}

		// 检查玩家是否在房间内，若在，记录位置
		DeskPlayer selfPlayer = nnProcedure.GetSelfPlayer();
		if (selfPlayer != null)
		{
			GF.LogInfo("当前玩家在房间内，位置=" + selfPlayer.Pos);
		}
		else
		{
			GF.LogInfo("当前玩家不在房间内");
		}

		Refresh();
		// 关闭游戏状态同步的等待框
		Util.GetInstance().CloseWaiting("GameStateSync");
	}

	public override void OnClickClose()
	{
		if (Util.IsClickLocked()) return;
		_ = GF.Sound.PlayEffect(AudioKeys.SOUND_BTN);
		Util.GetInstance().OpenConfirmationDialog("退出房间", "确定要退出房间吗?", () =>
		{
			nnProcedure.Send_LeaveDeskRq();
		});
	}
}

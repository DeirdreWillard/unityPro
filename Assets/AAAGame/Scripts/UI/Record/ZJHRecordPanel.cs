using UnityEngine;
using System.Linq;
using NetMsg;
using UnityGameFramework.Runtime;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;
using static GameConstants;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class ZJHRecordPanel : UIFormBase
{
	private float defaultWinScore = 0;

	private int currentOptionIndex = 0;
	private bool isReplaying = false;
	private bool isPaused = false;
	private float m_ReplaySpeed = 1.0f;
	private Coroutine m_ReplayCoroutine = null;

	// 预设速度列表
	private readonly float[] m_SpeedPresets = { 1.0f, 2.0f, 3.0f, 0.5f };
	private int m_CurrentSpeedIndex = 0;

	Msg_CreateDeskRq enterZjhDeskRs = null;
	private List<DeskPlayer> playerInfos = new();
	private List<Msg_PlayBackOption> options = new();
	private List<Msg_PlayBackOption> settledOptions = new();
	private float m_PotSum = 0f;

	protected override void OnClose(bool isShutdown, object userData)
	{
		StopReplay();
		base.OnClose(isShutdown, userData);
	}

	protected override void OnOpen(object userData)
	{
		base.OnOpen(userData);
		Init();
		Msg_PlayBackRs msg_playback = Msg_PlayBackRs.Parser.ParseFrom(Params.Get<VarByteArray>("PlayBack"));
		enterZjhDeskRs = msg_playback.Config;
		//初始化房间信息
		UpdateRoundInfo(0, 0); 
		varQiangZhiLiangPai.SetActive(enterZjhDeskRs.GoldenFlowerConfig.SysShowCard);
		varRoomNameID.text = "房间:" + enterZjhDeskRs.Config.DeskName + "(ID:" + msg_playback.DeskId + ")";
		varRoominfo.text = "底注: " + enterZjhDeskRs.Config.BaseCoin +
							  "  必闷: " + enterZjhDeskRs.GoldenFlowerConfig.MustStuffy +
							  "\n单注上限: " + (enterZjhDeskRs.GoldenFlowerConfig.SingleCoin <= 0 ? "无上限" : enterZjhDeskRs.GoldenFlowerConfig.SingleCoin.ToString()) +
							  "\n底池封顶: " + (enterZjhDeskRs.GoldenFlowerConfig.MaxPot <= 0 ? "无上限" : enterZjhDeskRs.GoldenFlowerConfig.MaxPot.ToString()) +
							  (enterZjhDeskRs.GoldenFlowerConfig.Use235 ? "\n235大三条" : "") +
							  (enterZjhDeskRs.GoldenFlowerConfig.BigLuckCard ? "\n大牌奖励" : "") +
							  (enterZjhDeskRs.GoldenFlowerConfig.IsCompareDouble ? "\n双倍比牌" : "");


		// 解析回放数据
		InitializeReplay(msg_playback);
	}

	public void Init(){
		defaultWinScore = 0;
		currentOptionIndex = 0;
		isReplaying = false;
		isPaused = false;
		m_ReplaySpeed = 1.0f;
		m_ReplayCoroutine = null;
		m_CurrentSpeedIndex = 0;
		enterZjhDeskRs = null;
		playerInfos = new();
		options = new();
		settledOptions = new();
		varStopImage.SetActive(false);
		varPlayImage.SetActive(true);
		varSpeedText.text = $"x{m_ReplaySpeed}";
		m_PotSum = 0f;
		UpdatePotSumText();
	}

	// 初始化回放数据
	public void InitializeReplay(Msg_PlayBackRs msg_playback)
	{
		playerInfos = msg_playback.DeskPlayer.ToList();
		List<Msg_PlayBackOption> playbacks = msg_playback.Option.ToList();
		options = playbacks.Where(x => x.Option != OptionType.OSettle).ToList();
		settledOptions = playbacks.Where(x => x.Option == OptionType.OSettle).ToList();
		options.Add(new Msg_PlayBackOption() { Option = OptionType.OSettle });

		foreach (var item in settledOptions)
		{
			if (float.Parse(item.Param1) > 0)
			{
				DeskPlayer deskPlayer = playerInfos.Find(x => x.BasePlayer.PlayerId == item.PlayerId);
				defaultWinScore = float.Parse(deskPlayer.Coin);
				break;
			}
		}
		varSeats.Init(msg_playback.Config.Config.PlayerNum);
		varSeats.IsPlayBack = true;
		// 初始化底池，闷注注入
		m_PotSum = 0f;
		//初始化座位玩家信息
		for (int i = 0; i < playerInfos.Count; i++)
		{
			DeskPlayer originalPlayer = playerInfos[i];
			DeskPlayer tempPlayer = originalPlayer.Clone();
			tempPlayer.HandCard.Clear();
			tempPlayer.HandCard.AddRange(new int[] { -1, -1, -1 });
			m_PotSum += float.Parse(tempPlayer.BetCoin);
			varSeats.PlayerEnter(tempPlayer);
			Seat_zjh seatNode = varSeats.GetPlayerByPos(tempPlayer.Pos);
			if(tempPlayer.IsBanker){
				varZhuang.transform.localPosition = GetWidgetPosition(0, WidgetType.Zhuang, seatNode.transform.localPosition, true) + seatNode.transform.localPosition;
				varZhuang.SetActive(true);
			}
		}
		UpdatePotSumText();
		GF.LogInfo("回放数据初始化完成");
		StartReplay();
	}

	private IEnumerator ReplayCoroutine(float delay)
	{
		yield return new WaitForSeconds(delay / m_ReplaySpeed);
		if (!isPaused)
		{
			ReplayNextOperation();
		}
		else
		{
			m_ReplayCoroutine = null;
		}
	}

	// 回放每个操作
	private void ReplayNextOperation()
	{
		if (currentOptionIndex > options.Count - 1 || gameObject.activeInHierarchy == false)
		{
			StopReplay();
			return;
		}
		Play();
		if (isReplaying && !isPaused)
		{
			m_ReplayCoroutine = CoroutineRunner.Instance.StartCoroutine(ReplayCoroutine(1f));  // 1秒延迟
		}
	}


	public void Play()
	{
		currentOptionIndex++;
		if (currentOptionIndex > options.Count)
		{
			currentOptionIndex = options.Count;
			return;
		}
		varTxtPage.text = currentOptionIndex + "/" + options.Count;
		Msg_PlayBackOption option = options[currentOptionIndex - 1];
		GF.LogInfo("Play" + option.ToString());
		if (option.Option == OptionType.OSettle)
		{
			foreach (var item in settledOptions)
			{
				DeskPlayer player = playerInfos.Find(x => x.BasePlayer.PlayerId == item.PlayerId);
				Seat_zjh seat = varSeats.GetPlayerByPlayerID(item.PlayerId);
				seat.SwitchWinChips(true, float.Parse(item.Param1));
				seat.ShowHandCards(player.HandCard.ToArray(), (ZjhCardType)player.Niu);
				if (float.Parse(item.Param1) > 0)
				{
					player.Coin = (float.Parse(player.Coin) + float.Parse(item.Param1)).ToString();
					seat.ScoreChange(defaultWinScore + float.Parse(item.Param1));
				}
			}
			return;
		}
		PlayOption(option, false);
	}

	public void UnPlay()
	{
		currentOptionIndex--;
		if (currentOptionIndex <= 0)
		{
			currentOptionIndex = 1;
			return;
		}
		varTxtPage.text = currentOptionIndex + "/" + options.Count;
		//不减是因为回放的是下一步操作 然后运行上一步操作
		Msg_PlayBackOption option = options[currentOptionIndex];
		GF.LogInfo("UnPlay" + option.ToString());

		if (option.Option == OptionType.OSettle)
		{
			foreach (var item in settledOptions)
			{
				DeskPlayer player = playerInfos.Find(x => x.BasePlayer.PlayerId == item.PlayerId);
				Seat_zjh seat = varSeats.GetPlayerByPlayerID(item.PlayerId);
				seat.SwitchWinChips(false, float.Parse(item.Param1));
				seat.HideHandCards();
				seat.SwitchCardTypeImg(false, ZjhCardType.High);
				if (float.Parse(item.Param1) > 0)
				{
					player.Coin = (float.Parse(player.Coin) - float.Parse(item.Param1)).ToString();
					seat.ScoreChange(float.Parse(player.Coin));
				}
			}
			return;
		}
		Seat_zjh seat_Zjh = varSeats.GetPlayerByPlayerID(option.PlayerId);
		DeskPlayer deskPlayer = playerInfos.Find(x => x.BasePlayer.PlayerId == option.PlayerId);
		Msg_PlayBackOption optionLast = FindLastOption(option.PlayerId, currentOptionIndex);
		switch (option.Option)
		{
			case OptionType.OLook:
				deskPlayer.IsLook = false;
				seat_Zjh.signLookCard.SetActive(false);
				seat_Zjh.SwitchHideCards(true, -1);
				break;
			case OptionType.OBet:
				deskPlayer.Coin = (float.Parse(deskPlayer.Coin) + float.Parse(option.Param1)).ToString();
				m_PotSum -= float.Parse(option.Param1);
				UpdatePotSumText();
				seat_Zjh.ScoreChange(float.Parse(deskPlayer.Coin));
				break;
			case OptionType.OAddCoin:
				deskPlayer.Coin = (float.Parse(deskPlayer.Coin) + float.Parse(option.Param1)).ToString();
				m_PotSum -= float.Parse(option.Param1);
				UpdatePotSumText();
				seat_Zjh.ScoreChange(float.Parse(deskPlayer.Coin));
				break;
			case OptionType.OCompare:
				deskPlayer.Coin = (float.Parse(deskPlayer.Coin) + float.Parse(option.Param3)).ToString();
				m_PotSum -= float.Parse(option.Param3);
				UpdatePotSumText();
				seat_Zjh.ScoreChange(float.Parse(deskPlayer.Coin));
				string[] playerIds = option.Param1.Split(',');
				seat_Zjh.playerInfo.CompareLose = false;
				seat_Zjh.SetHideCardsColor(Color.white);
				Seat_zjh seat_Zjh_compare = varSeats.GetPlayerByPlayerID(long.Parse(playerIds[0]));
				seat_Zjh_compare.playerInfo.CompareLose = false;
				seat_Zjh_compare.SetHideCardsColor(Color.white);
				Msg_PlayBackOption optionLast_compare = FindLastOption(long.Parse(playerIds[0]), currentOptionIndex);
				PlayOption(optionLast_compare);
				break;
			case OptionType.OGive:
				deskPlayer.IsGive = false;
				seat_Zjh.SetHideCardsColor(Color.white);
				break;
		}
		PlayOption(optionLast);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="option"></param>
	/// <param name="play">是否倒叙播放</param>
	public void PlayOption(Msg_PlayBackOption option, bool unPlay = true)
	{
		Seat_zjh seat_Zjh = varSeats.GetPlayerByPlayerID(option.PlayerId);
		if (option.Time == 0)
		{
			seat_Zjh.ResetPlayer_Record();
			return;
		}
		DeskPlayer deskPlayer = playerInfos.Find(x => x.BasePlayer.PlayerId == option.PlayerId);
		//比牌另外地方显示
		if (option.Option != OptionType.OCompare){
			seat_Zjh.ShowOperateImage(GetTypeByOption(option.Option));
		}
		switch (option.Option)
		{
			case OptionType.OLook:
				deskPlayer.IsLook = true;
				seat_Zjh.signLookCard.SetActive(true);
				seat_Zjh.SwitchHideCards(true, -2);
				break;
			case OptionType.OBet:
				seat_Zjh.ShowBetCoin(float.Parse(option.Param1));
				if (!unPlay)
				{
					deskPlayer.Coin = (float.Parse(deskPlayer.Coin) - float.Parse(option.Param1)).ToString();
					m_PotSum += float.Parse(option.Param1);
					UpdatePotSumText();
				}
				seat_Zjh.ScoreChange(float.Parse(deskPlayer.Coin));
				break;
			case OptionType.OAddCoin:
				seat_Zjh.ShowBetCoin(float.Parse(option.Param1));
				if (!unPlay)
				{
					deskPlayer.Coin = (float.Parse(deskPlayer.Coin) - float.Parse(option.Param1)).ToString();
					m_PotSum += float.Parse(option.Param1);
					UpdatePotSumText();
				}
				seat_Zjh.ScoreChange(float.Parse(deskPlayer.Coin));
				break;
			case OptionType.OCompare:
				string[] playerIds = option.Param1.Split(',');
				//请求比牌者赢
				bool iswin = long.Parse(playerIds[0]) != long.Parse(playerIds[1]);
				Seat_zjh seat_Zjh_compare = varSeats.GetPlayerByPlayerID(long.Parse(playerIds[0]));
				seat_Zjh.ShowBetCoin(float.Parse(option.Param3));
				seat_Zjh.ShowOperateImage(GetTypeByOption(option.Option),iswin);
				if (!unPlay)
				{
					seat_Zjh.playerInfo.CompareLose = !iswin;
					seat_Zjh.SetHideCardsColor(!iswin ? Color.grey : Color.white);
					seat_Zjh_compare.playerInfo.CompareLose = iswin;
					seat_Zjh_compare.SetHideCardsColor(iswin ? Color.grey : Color.white);
					deskPlayer.Coin = (float.Parse(deskPlayer.Coin) - float.Parse(option.Param3)).ToString();
					m_PotSum += float.Parse(option.Param3);
					UpdatePotSumText();
				}
				seat_Zjh.ScoreChange(float.Parse(deskPlayer.Coin));
				seat_Zjh_compare.ShowOperateImage(ZjhOption.Compare,!iswin);
				//输家清空筹码
				// seat_Zjh_compare.SwitchChipObj(false);
				break;
			case OptionType.OGive:
				deskPlayer.IsGive = true;
				seat_Zjh.SwitchChipObj(false);
				seat_Zjh.SetHideCardsColor(Color.grey);
				break;
		}
	}

	public Msg_PlayBackOption FindLastOption(long playerId, int currentOptionIndex)
	{
		Msg_PlayBackOption option = new()
		{
			PlayerId = playerId
		};
		for (int i = currentOptionIndex - 1; i >= 0; i--)
		{
			if (options[i].PlayerId == playerId)
			{
				option = options[i];
				break;
			}
		}
		return option;
	}

	public ZjhOption GetTypeByOption(OptionType option)
	{
		switch (option)
		{
			case OptionType.OLook:
				return ZjhOption.Look;
			case OptionType.OBet:
				return ZjhOption.Follow;
			case OptionType.OAddCoin:
				return ZjhOption.Double;
			case OptionType.OCompare:
				return ZjhOption.Compare;
			case OptionType.OGive:
				return ZjhOption.Give;
		}
		return ZjhOption.AddTime;
	}

	// 开始回放
	public void StartReplay()
	{
		if (isReplaying && !isPaused) return;

		if (isPaused)
		{
			// 继续播放
			isPaused = false;
			if (m_ReplayCoroutine == null)
			{
				m_ReplayCoroutine = CoroutineRunner.Instance.StartCoroutine(ReplayCoroutine(1f));
			}
		}
		else
		{
			// 重新开始播放
			isReplaying = true;
			isPaused = false;
			currentOptionIndex = 0;
			ReplayNextOperation();
		}
	}

	// 重新开始回放
	public void RestartReplay()
	{
		StopReplay();
		// 重置所有玩家状态
		foreach (var player in playerInfos)
		{
			Seat_zjh seat = varSeats.GetPlayerByPlayerID(player.BasePlayer.PlayerId);
			seat.ResetPlayer_Record();
		}
		m_PotSum = 0f;
		UpdatePotSumText();
		StartReplay();
	}

	// 停止回放
	public void StopReplay()
	{
		isReplaying = false;
		isPaused = false;
		if (m_ReplayCoroutine != null)
		{
			CoroutineRunner.Instance.StopCoroutine(m_ReplayCoroutine);
			m_ReplayCoroutine = null;
		}
		GF.LogInfo("回放已停止");
	}

	// 暂停回放
	public void PauseReplay()
	{
		if (!isReplaying) return;
		
		isPaused = !isPaused;
		GF.LogInfo($"回放暂停: {isPaused}");
		varStopImage.SetActive(!isPaused);
		varPlayImage.SetActive(isPaused);
		if (!isPaused && m_ReplayCoroutine == null)
		{
			// 继续播放
			m_ReplayCoroutine = CoroutineRunner.Instance.StartCoroutine(ReplayCoroutine(1f));
		}
	}

	// 调整回放速度
	public void AdjustReplaySpeed(bool increase)
	{
		// 循环切换预设速度
		m_CurrentSpeedIndex = (m_CurrentSpeedIndex + 1) % m_SpeedPresets.Length;
		m_ReplaySpeed = m_SpeedPresets[m_CurrentSpeedIndex];
		GF.LogInfo($"回放速度: {m_ReplaySpeed}x");
		varSpeedText.text = $"x{m_ReplaySpeed}";
	}

	// 更新圈数信息
	public void UpdateRoundInfo(int curIndex, int betCoin)
	{
		string str = enterZjhDeskRs.GoldenFlowerConfig.MaxRound == -1 ? "圈数无上限" : "第 " + curIndex + "/" + enterZjhDeskRs.GoldenFlowerConfig.MaxRound + " 圈";
		str += "  我的投入 " + betCoin;
		varRoundInfo.text = str;
	}

	public void ShowLightBg(bool isShow, Seat_zjh seat = null)
	{
		varLightBg.SetActive(isShow);
		if (isShow)
		{
			Quaternion angle = GameUtil.GetQuaternion(varLightBg.transform.localPosition, seat.transform.localPosition);
			varLightBg.transform.DOLocalRotate(angle.eulerAngles, 0.1f, RotateMode.FastBeyond360);
		}
	}

	//更新底池数
	public void UpdatePotSumText()
	{
		varPotSumText.text = $"{m_PotSum}";
	}

	protected override void OnButtonClick(object sender, string btId)
	{
		switch (btId)
		{
			case "关闭":
				GF.UI.Close(this.UIForm);
				break;
			case "上一步":
				if (isReplaying && !isPaused){
					PauseReplay();
				}
				UnPlay();
				break;
			case "下一步":
				if (isReplaying && !isPaused)
				{
					PauseReplay();
				}
				Play();
				break;
			case "重放":
				RestartReplay();
				break;
			case "暂停":
				PauseReplay();
				break;
			case "加速":
				AdjustReplaySpeed(true);
				break;
		}
	}
}

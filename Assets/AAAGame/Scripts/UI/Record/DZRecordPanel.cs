using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityGameFramework.Runtime;

// 封装德州操作数据结构
public class DZOption
{
	public Msg_OptionParam param;   // 操作参数
	public TexasState state;        // 操作所在阶段
	public long playerId;           // 操作玩家ID
}

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class DZRecordPanel : UIFormBase
{
	#region UI控件
	public Text varRoomNameID;
	public Text varRoominfo;
	public Text varRoundInfo;
	public Text varCurrentCoin;
	public Text varTxtPage;
	public Text varSpeedText;

	public GameObject varPlayImage;
	public GameObject varStopImage;

	public List<GameObject> seatlist;
	public List<Card> commoncard;
	#endregion

	#region 回放数据
	private Msg_TexasPokerPlayBackRs m_Playback;
	private readonly List<DZOption> m_Options = new();
	private int m_CurrentOptionIndex = 0;
	private float m_CurrentCoin = 0;
	#endregion

	#region 回放控制变量
	private bool m_IsPaused = false;
	private bool m_IsReplaying = false;
	private float m_ReplaySpeed = 1.0f;
	private Coroutine m_ReplayCoroutine = null;
	private readonly float[] m_SpeedPresets = { 1.0f, 2.0f, 3.0f, 0.5f };
	private int m_CurrentSpeedIndex = 0;
	#endregion

	#region 玩家下注追踪
	private Dictionary<long, float> m_CurrentStatePlayerBets = new();
	#endregion

	protected override void OnOpen(object userData)
	{
		base.OnOpen(userData);
		Init();
		m_Playback = Msg_TexasPokerPlayBackRs.Parser.ParseFrom(Params.Get<VarByteArray>("PlayBack"));
		
		// 初始化玩家原始筹码数据
		foreach (var p in m_Playback.DeskPlayer)
		{
			p.BetCoin = p.Coin;
			p.IsGive = false;
		}

		// 初始化回放数据
		InitializeReplay();
	}

	private void Init()
	{
		// 重置所有变量
		m_Options.Clear();
		m_CurrentCoin = 0;
		m_CurrentOptionIndex = 0;
		m_IsReplaying = false;
		m_IsPaused = false;
		m_ReplaySpeed = 1.0f;
		m_ReplayCoroutine = null;
		m_CurrentSpeedIndex = 0;
		m_CurrentStatePlayerBets.Clear();

		// 重置UI
		if (varStopImage != null) varStopImage.SetActive(false);
		if (varPlayImage != null) varPlayImage.SetActive(true);
		if (varSpeedText != null) varSpeedText.text = $"x{m_ReplaySpeed}";
		
		// 重置弃牌状态
		if (m_Playback != null)
		{
			ResetAllGiveStates();
		}
	}

	private void InitializeReplay()
	{
		// 初始化基本信息
		InitBaseInfo();

		// 整理操作步骤
		OrganizePlaybackOptions();
		
		// 初始化座位信息
		InitAllSeats();
		
		// 开始回放
		StartReplay();
	}

	// 整理回放操作序列
	private void OrganizePlaybackOptions()
	{
		// 添加各阶段操作记录
		AddOptions(TexasState.PreFlop);
		AddOptions(TexasState.Flop);
		AddOptions(TexasState.TurnProtect);
		AddOptions(TexasState.Turn);
		AddOptions(TexasState.RiverProtect);
		AddOptions(TexasState.River);
		
		// 添加结算阶段
		m_Options.Add(new DZOption
		{
			param = null,
			state = TexasState.TexasSettle,
			playerId = 0
		});
	}

	private void AddOptions(TexasState state)
	{
		Msg_StateOption round = m_Playback.StageOption.FirstOrDefault(p => p.State == state);
		if (round == null) return;
		
		//转阶段空处理
		if (state != TexasState.TurnProtect && state != TexasState.RiverProtect)
		{
			m_Options.Add(new DZOption
			{
				param = null,
				state = state,
				playerId = 0
			});
		}

		foreach (Msg_OptionParam so in round.StateOption.ToList())
		{
			// 跳过盲注操作，因为盲注已经在UpdateBlinds中处理
			if (so.Option == TexasOption.Blind)
				continue;
				
			m_Options.Add(new DZOption
			{
				param = so,
				state = state,
				playerId = so.PlayerId
			});
		}
	}

	#region 回放控制方法
	private IEnumerator AutoPlay()
	{
		while (m_CurrentOptionIndex < m_Options.Count)
		{
			// 检查是否暂停
			if (m_IsPaused)
			{
				m_ReplayCoroutine = null;
				yield break;
			}
			
			// 播放当前步骤
			PlayStep(true);
			
			// 根据播放速度等待
			float waitTime = 1f / m_ReplaySpeed;
			yield return new WaitForSeconds(waitTime);
		}
		
		// 更新UI
		RefreshTotalPage();
		m_ReplayCoroutine = null;
	}

	public void StartReplay()
	{
		if (m_IsReplaying && !m_IsPaused) return;

		if (m_IsPaused)
		{
			// 继续播放
			m_IsPaused = false;
			if (m_ReplayCoroutine == null && m_CurrentOptionIndex < m_Options.Count)
			{
				m_ReplayCoroutine = CoroutineRunner.Instance.StartCoroutine(AutoPlay());
			}
		}
		else
		{
			// 重新开始播放
			m_IsReplaying = true;
			m_IsPaused = false;
			m_CurrentOptionIndex = 0;
			m_ReplayCoroutine = CoroutineRunner.Instance.StartCoroutine(AutoPlay());
		}

		// 更新UI状态
		UpdatePlayPauseUI();
	}

	public void PauseReplay()
	{
		m_IsPaused = !m_IsPaused;
		UpdatePlayPauseUI();
		
		if (m_IsPaused)
		{
			if (m_ReplayCoroutine != null)
			{
				CoroutineRunner.Instance.StopCoroutine(m_ReplayCoroutine);
				m_ReplayCoroutine = null;
			}
		}
		else
		{
			if (m_ReplayCoroutine == null && m_CurrentOptionIndex < m_Options.Count)
			{
				m_ReplayCoroutine = CoroutineRunner.Instance.StartCoroutine(AutoPlay());
			}
		}
	}

	public void RestartReplay()
	{
		StopReplay();
		
		// 重置界面状态
		InitAllSeats();
		ResetAllSeatsState();
		// 重置弃牌状态
		ResetAllGiveStates();
		UpdateBlinds();
		UpdateCommonCards(TexasState.PreFlop);
		
		// 重置玩家数据和底池
		ResetPlayersData();
		
		// 开始回放
		StartReplay();
	}

	public void StopReplay()
	{
		m_IsReplaying = false;
		m_IsPaused = false;
		if (m_ReplayCoroutine != null)
		{
			CoroutineRunner.Instance.StopCoroutine(m_ReplayCoroutine);
			m_ReplayCoroutine = null;
		}
	}

	private void AdjustReplaySpeed()
	{
		m_CurrentSpeedIndex = (m_CurrentSpeedIndex + 1) % m_SpeedPresets.Length;
		m_ReplaySpeed = m_SpeedPresets[m_CurrentSpeedIndex];
		if (varSpeedText != null) varSpeedText.text = $"x{m_ReplaySpeed}";
	}

	private void UpdatePlayPauseUI()
	{
		if (varStopImage != null) varStopImage.SetActive(!m_IsPaused);
		if (varPlayImage != null) varPlayImage.SetActive(m_IsPaused);
	}
	#endregion

	#region 回放步骤控制
	public void PlayStep(bool autoAdvance = false)
	{
		// 如果需要自动前进，增加索引
		if (autoAdvance)
		{
			m_CurrentOptionIndex++;
		}
		
		if (m_CurrentOptionIndex <= 0 || m_CurrentOptionIndex > m_Options.Count)
		{
			return;
		}

		// 更新进度显示
		RefreshTotalPage();
		
		// 获取当前操作
		DZOption option = m_Options[m_CurrentOptionIndex - 1];
		DZOption prevOption = m_CurrentOptionIndex > 1 ? m_Options[m_CurrentOptionIndex - 2] : null;
		bool isStateChange = prevOption != null && prevOption.state != option.state;
		
		// 如果阶段发生变化，处理阶段转换
		if (isStateChange)
		{
			HandleStateTransition(prevOption.state, option.state);
		}
		
		// 第一步初始化界面和盲注
		if (m_CurrentOptionIndex == 1)
		{
			InitBaseInfo();
			InitAllSeats();
			ResetAllSeatsState();
			
			// 设置盲注，但不累加到底池（已在UpdateBlinds中处理）
			UpdateBlinds();
			
			// 第一步就显示公共牌的初始状态
			UpdateCommonCards(option.state);
		}
		
		// 更新玩家操作
		UpdatePlayerAction(option.param);
		
		// 如果是结算阶段
		if (option.state == TexasState.TexasSettle)
		{
			ShowSettle();
		}
	}

	public void UnPlayStep()
	{
		if (m_CurrentOptionIndex <= 1) return;

		// 目标步索引
		int targetIndex = m_CurrentOptionIndex - 1;

		// 完全重置
		ResetAllSeatsState();
		ResetPlayersData();
		ResetAllGiveStates(); // 重置弃牌状态
		UpdateBlinds();
		UpdateCommonCards(TexasState.PreFlop);

		// 顺序重播到目标步
		for (int i = 1; i <= targetIndex; i++)
		{
			m_CurrentOptionIndex = i;
			PlayStep(false);
		}
		// 设置索引
		m_CurrentOptionIndex = targetIndex;

		// 更新进度显示
		RefreshTotalPage();
	}
	#endregion

	#region UI更新方法
	// 初始化基础信息
	private void InitBaseInfo()
	{
		varRoomNameID.text = "房间:" + m_Playback.Config.Config.DeskName + "(ID:" + m_Playback.DeskId + ")";
		varRoominfo.text = $"{m_Playback.Config.Config.BaseCoin:F2}/{m_Playback.Config.Config.BaseCoin * 2:F2}({m_Playback.Config.TexasPokerConfig.Ante:F2})";
		varRoundInfo.text = m_Playback.Config.TexasPokerConfig.Protect ? "保险:开启" : "保险:关闭";
		varCurrentCoin.text = "0.00";
		m_CurrentCoin = 0;
	}

	// 更新页面进度信息
	private void RefreshTotalPage()
	{
		varTxtPage.text = m_CurrentOptionIndex + "/" + m_Options.Count;
	}

	// 初始化所有座位
	private void InitAllSeats()
	{
		// 清理所有座位
		foreach (GameObject seat in seatlist)
		{
			seat.transform.Find("Player").gameObject.SetActive(false);
			ResetSeatState(seat);
		}

		// 初始化玩家座位
		foreach (DeskPlayer player in m_Playback.DeskPlayer)
		{
			GameObject seat = seatlist[(int)player.Pos - 1];
			seat.transform.Find("Player").gameObject.SetActive(true);
			seat.transform.Find("Player").GetComponent<AvatarInfo>().Init(player.BasePlayer);
			
			player.IsGive = false;
			// 设置初始筹码
			player.Coin = player.BetCoin;
			seat.transform.Find("Player/分数/Value").GetComponent<Text>().text = player.Coin;
			
			// 设置手牌
			if (player.HandCard.Count == 2)
			{
				seat.transform.Find("Player/手牌/Card1").GetComponent<Card>().Init(player.HandCard[0]);
				seat.transform.Find("Player/手牌/Card2").GetComponent<Card>().Init(player.HandCard[1]);
			}
			
			// 设置庄家标记
			seat.transform.Find("Player/庄").gameObject.SetActive(player.BasePlayer.PlayerId == m_Playback.Mark.ToList()[3]);
		}
	}

	// 重置座位状态
	private void ResetSeatState(GameObject _seat)
	{
		Transform playerTrans = _seat.transform.Find("Player");
		if (!playerTrans.gameObject.activeSelf) return;
		
		// 获取当前弃牌状态
		var giveObj = playerTrans.Find("操作/弃牌");
		bool isGiveState = giveObj && giveObj.gameObject.activeSelf;
		
		// 重置筹码显示
		var chipObj = playerTrans.Find("筹码");
		chipObj.gameObject.SetActive(false);
		chipObj.Find("Chip1").gameObject.SetActive(true);
		chipObj.Find("Chip2").gameObject.SetActive(false);
		chipObj.Find("Chip3").gameObject.SetActive(false);
		chipObj.Find("Chip4").gameObject.SetActive(false);
		chipObj.Find("Value").GetComponent<Text>().text = "0";
		
		// 重置其他显示元素
		playerTrans.Find("收益").gameObject.SetActive(false);
		playerTrans.Find("牌型").gameObject.SetActive(false);
		playerTrans.Find("胜利").gameObject.SetActive(false);
		playerTrans.Find("特效").gameObject.SetActive(false);

		// 重置操作显示
		foreach (Transform t in playerTrans.Find("操作"))
		{
			t.gameObject.SetActive(false);
		}
		
		// 保留弃牌状态
		if (isGiveState && giveObj)
		{
			giveObj.gameObject.SetActive(true);
			playerTrans.Find("操作/弃牌2").gameObject.SetActive(true);
		}
	}

	// 重置所有座位状态
	private void ResetAllSeatsState()
	{
		// 保存所有玩家弃牌状态
		Dictionary<int, bool> seatGiveStates = new Dictionary<int, bool>();
		
		// 获取所有座位的弃牌状态
		for (int i = 0; i < seatlist.Count; i++)
		{
			GameObject seat = seatlist[i];
			if (seat.transform.Find("Player").gameObject.activeSelf)
			{
				bool isGive = seat.transform.Find("Player/操作/弃牌").gameObject.activeSelf;
				seatGiveStates[i] = isGive;
			}
		}
		
		// 重置所有座位
		foreach (GameObject seat in seatlist)
		{
			if (seat.transform.Find("Player").gameObject.activeSelf)
			{
				ResetSeatState(seat);
			}
		}
		
		// 恢复所有座位的弃牌状态
		for (int i = 0; i < seatlist.Count; i++)
		{
			if (seatGiveStates.ContainsKey(i) && seatGiveStates[i])
			{
				GameObject seat = seatlist[i];
				seat.transform.Find("Player/操作/弃牌").gameObject.SetActive(true);
				seat.transform.Find("Player/操作/弃牌2").gameObject.SetActive(true);
			}
		}
		
		m_CurrentStatePlayerBets.Clear();
	}

	// 更新盲注显示
	private void UpdateBlinds()
	{
		// 先隐藏所有座位的盲注显示
		foreach (GameObject seat in seatlist)
		{
			var chipObj = seat.transform.Find("Player/筹码");
			chipObj.gameObject.SetActive(false);
			chipObj.Find("Chip2").gameObject.SetActive(false);
			chipObj.Find("Chip3").gameObject.SetActive(false);
			chipObj.Find("Chip4").gameObject.SetActive(false);
			chipObj.Find("Value").GetComponent<Text>().text = "0";
		}

		// 获取mark数组
		var markList = m_Playback.Mark.ToList();

		// 查找PreFlop阶段的所有BLIND操作
		var preFlopStage = m_Playback.StageOption.FirstOrDefault(p => p.State == TexasState.PreFlop);
		if (preFlopStage == null) return;

		// 支持多人盲注
		foreach (var blindOp in preFlopStage.StateOption.Where(opt => opt.Option == TexasOption.Blind))
		{
			var player = m_Playback.DeskPlayer.FirstOrDefault(p => p.BasePlayer.PlayerId == blindOp.PlayerId);
			if (player == null) continue;
			GameObject seat = seatlist[(int)player.Pos - 1];
			var chipObj = seat.transform.Find("Player/筹码");
			chipObj.gameObject.SetActive(true);

			// 先全部隐藏
			chipObj.Find("Chip2").gameObject.SetActive(false);
			chipObj.Find("Chip3").gameObject.SetActive(false);
			chipObj.Find("Chip4").gameObject.SetActive(false);

			// 判断盲注类型
			if (markList.Count > 0 && blindOp.PlayerId == markList[0])
				chipObj.Find("Chip2").gameObject.SetActive(true); // 小盲
			else if (markList.Count > 1 && blindOp.PlayerId == markList[1])
				chipObj.Find("Chip3").gameObject.SetActive(true); // 大盲
			else if (markList.Count > 2 && markList[2] != 0 && blindOp.PlayerId == markList[2])
				chipObj.Find("Chip4").gameObject.SetActive(true); // 强注
			else
				chipObj.Find("Chip3").gameObject.SetActive(true); // 默认大盲

			// 显示盲注金额
			chipObj.Find("Value").GetComponent<Text>().text = blindOp.Param.ToString("F2");

			// 更新玩家筹码（扣除盲注）
			UpdatePlayerCoins(player, blindOp.Param, true);

			// 记录当前状态下注
			if (!m_CurrentStatePlayerBets.ContainsKey(player.BasePlayer.PlayerId))
			{
				m_CurrentStatePlayerBets[player.BasePlayer.PlayerId] = 0;
			}
			m_CurrentStatePlayerBets[player.BasePlayer.PlayerId] += blindOp.Param;
		}
	}

	// 更新公共牌
	private void UpdateCommonCards(TexasState state)
	{
		// 隐藏所有公共牌
		for (int i = 0; i < 5; i++)
		{
			commoncard[i].gameObject.SetActive(false);
		}

		// 根据当前阶段显示公共牌
		switch (state)
		{
			case TexasState.Flop:
				// 显示前三张公共牌
				for (int i = 0; i < 3; i++)
				{
					commoncard[i].gameObject.SetActive(true);
					commoncard[i].Init(m_Playback.CommonCards[i]);
				}
				break;
				
			case TexasState.Turn:
			case TexasState.TurnProtect:
				// 显示四张公共牌
				for (int i = 0; i < 4; i++)
				{
					commoncard[i].gameObject.SetActive(true);
					commoncard[i].Init(m_Playback.CommonCards[i]);
				}
				break;
				
			case TexasState.River:
			case TexasState.RiverProtect:
			case TexasState.TexasSettle:
				// 显示全部五张公共牌
				for (int i = 0; i < m_Playback.CommonCards.Count && i < 5; i++)
				{
					commoncard[i].gameObject.SetActive(true);
					commoncard[i].Init(m_Playback.CommonCards[i]);
				}
				break;
		}
	}

	// 显示结算界面
	private void ShowSettle()
	{
		foreach (DeskPlayer player in m_Playback.DeskPlayer)
		{
			GameObject seat = seatlist[(int)player.Pos - 1];
			
			// 显示牌型
			seat.transform.Find("Player/牌型").gameObject.SetActive(true);
			var profit = m_Playback.Profit.FirstOrDefault(p => player.BasePlayer.PlayerId == p.PlayerId);
			if (profit != null)
			{
				seat.transform.Find("Player/牌型").GetComponent<Text>().text = CardType2String(profit.CardType);
				
				// 显示收益
				seat.transform.Find("Player/收益").gameObject.SetActive(true);
				float profitAmount = float.Parse(profit.Profit);
				seat.transform.Find("Player/收益/Profit").GetComponent<Text>().text = profitAmount.ToString("F2");
				
				// 根据盈亏设置不同背景
				seat.transform.Find("Player/收益/BG1").gameObject.SetActive(profitAmount > 0);
				seat.transform.Find("Player/收益/BG2").gameObject.SetActive(profitAmount <= 0);
				
				// 显示胜利标记
				if (m_Playback.MaxWinner.ToList().Contains(player.BasePlayer.PlayerId))
				{
					seat.transform.Find("Player/胜利").gameObject.SetActive(true);
				}
			}
		}
	}
	#endregion

	#region 辅助方法
	private string CardType2String(TexasCardType CardType)
	{
		return CardType switch
		{
			TexasCardType.High => "高牌",
			TexasCardType.Pair => "一对",
			TexasCardType.TwoPair => "两对",
			TexasCardType.ThreeCard => "三条",
			TexasCardType.Shunzi => "顺子",
			TexasCardType.SameColor => "同花",
			TexasCardType.Gourd => "葫芦",
			TexasCardType.FourCard => "四条",
			TexasCardType.Tonghuashun => "同花顺",
			TexasCardType.KingTonghuashun => "皇家同花顺",
			_ => "",
		};
	}

	// 更新玩家在当前阶段的下注
	private void UpdatePlayerBet(Msg_OptionParam param)
	{
		if (param == null || param.Param == 0) return;

		long playerId = param.PlayerId;
		var player = m_Playback.DeskPlayer.FirstOrDefault(p => p.BasePlayer.PlayerId == playerId);
		if (player == null) return;
		
		GameObject actionInfo = seatlist[(int)player.Pos - 1];
		// 更新当前阶段玩家的总下注
		if (!m_CurrentStatePlayerBets.ContainsKey(playerId))
		{
			m_CurrentStatePlayerBets[playerId] = 0;
		}
		m_CurrentStatePlayerBets[playerId] += param.Param;

		// 强制隐藏盲注专用筹码，避免非盲注阶段显示
		var chipObj = actionInfo.transform.Find("Player/筹码");
		chipObj.gameObject.SetActive(true);
		// 显示玩家累计下注金额
		chipObj.Find("Value").GetComponent<Text>().text = m_CurrentStatePlayerBets[playerId].ToString("F2");
		// 强制隐藏盲注专用筹码，避免非盲注阶段显示
		chipObj.Find("Chip2").gameObject.SetActive(false);
		chipObj.Find("Chip3").gameObject.SetActive(false);
		chipObj.Find("Chip4").gameObject.SetActive(false);

		// 只有非保险操作才扣除玩家金币
		if (param.Option != TexasOption.ProtectOne && param.Option != TexasOption.ProtectTwo)
		{
			UpdatePlayerCoins(player, param.Param, true);
		}
	}
	#endregion

	protected override void OnButtonClick(object sender, string btId)
	{
		switch (btId)
		{
			case "关闭":
				GF.UI.Close(this.UIForm);
				break;
			case "上一步":
				if (!m_IsPaused)
				{
					PauseReplay();
				}
				UnPlayStep();
				break;
			case "下一步":
				if (!m_IsPaused)
				{
					PauseReplay();
				}
				PlayStep(true);
				break;
			case "重放":
				RestartReplay();
				break;
			case "暂停":
				PauseReplay();
				break;
			case "加速":
				AdjustReplaySpeed();
				break;
		}
	}

	protected override void OnClose(bool isShutdown, object userData)
	{
		if (m_ReplayCoroutine != null)
		{
			CoroutineRunner.Instance.StopCoroutine(m_ReplayCoroutine);
			m_ReplayCoroutine = null;
		}
		base.OnClose(isShutdown, userData);
	}

	// 重置玩家数据和底池
	private void ResetPlayersData()
	{
		// 重置玩家筹码到初始状态
		foreach (DeskPlayer player in m_Playback.DeskPlayer)
		{
			player.Coin = player.BetCoin;
			GameObject seat = seatlist[(int)player.Pos - 1];
			seat.transform.Find("Player/分数/Value").GetComponent<Text>().text = player.Coin;
		}
		
		// 重置底池和玩家下注记录
		m_CurrentCoin = 0;
		varCurrentCoin.text = "0.00";
		m_CurrentStatePlayerBets.Clear();
		
		// 重置索引
		m_CurrentOptionIndex = 0;

		// 重置所有玩家弃牌标记
		ResetAllGiveStates();
	}

	// 更新玩家资金
	private void UpdatePlayerCoins(DeskPlayer _player, float _amount, bool _isDeduct = true)
	{
		if (_player == null) return;
		
		float currentAmount = float.Parse(_player.Coin);
		float newAmount = _isDeduct ? currentAmount - _amount : currentAmount + _amount;
		_player.Coin = newAmount.ToString("F2");
		
		GameObject seatInfo = seatlist[(int)_player.Pos - 1];
		seatInfo.transform.Find("Player/分数/Value").GetComponent<Text>().text = _player.Coin;
	}

	// 重置所有玩家的弃牌状态
	private void ResetAllGiveStates()
	{
		// 重置所有玩家的弃牌状态
		foreach (DeskPlayer player in m_Playback.DeskPlayer)
		{
			player.IsGive = false;
			GameObject seat = seatlist[(int)player.Pos - 1];
			seat.transform.Find("Player/操作/弃牌").gameObject.SetActive(false);
			seat.transform.Find("Player/操作/弃牌2").gameObject.SetActive(false);
		}
	}

	#region 操作处理方法
	// 处理阶段转换
	private void HandleStateTransition(TexasState _oldState, TexasState _newState)
	{
		// 保存所有玩家弃牌状态
		Dictionary<long, bool> playerGiveStates = new Dictionary<long, bool>();
		foreach (DeskPlayer player in m_Playback.DeskPlayer)
		{
			playerGiveStates[player.BasePlayer.PlayerId] = player.IsGive;
		}
		
		// 累加当前阶段所有下注到底池
		foreach (var playerBet in m_CurrentStatePlayerBets)
		{
			m_CurrentCoin += playerBet.Value;
		}
		
		// 清空当前阶段玩家下注记录
		m_CurrentStatePlayerBets.Clear();
		
		// 更新底池显示
		varCurrentCoin.text = m_CurrentCoin.ToString("F2");
		
		// 更新公共牌显示
		UpdateCommonCards(_newState);
		
		// 清除前一阶段的操作显示
		ResetAllSeatsState();
		
		// 恢复所有玩家弃牌状态
		foreach (DeskPlayer player in m_Playback.DeskPlayer)
		{
			if (playerGiveStates.ContainsKey(player.BasePlayer.PlayerId) && playerGiveStates[player.BasePlayer.PlayerId])
			{
				player.IsGive = true;
				GameObject seat = seatlist[(int)player.Pos - 1];
				seat.transform.Find("Player/操作/弃牌").gameObject.SetActive(true);
				seat.transform.Find("Player/操作/弃牌2").gameObject.SetActive(true);
			}
		}
	}

	// 更新玩家操作
	private void UpdatePlayerAction(Msg_OptionParam _param)
	{
		if (_param == null) return;

		// 获取玩家座位和信息
		var player = m_Playback.DeskPlayer.FirstOrDefault(p => p.BasePlayer.PlayerId == _param.PlayerId);
		if (player == null) return;

		GameObject actionInfo = seatlist[(int)player.Pos - 1];
		
		// 重置该玩家之前的操作显示（已在ResetPlayerActionDisplay中保留弃牌状态）
		ResetPlayerActionDisplay(actionInfo);
		
		// 确保弃牌状态显示
		actionInfo.transform.Find("Player/操作/弃牌").gameObject.SetActive(player.IsGive);
		actionInfo.transform.Find("Player/操作/弃牌2").gameObject.SetActive(player.IsGive);

		// 根据操作类型更新UI和数据
		switch (_param.Option)
		{
			case TexasOption.Allin:
				actionInfo.transform.Find("Player/操作/ALLIN").gameObject.SetActive(true);
				actionInfo.transform.Find("Player/特效").gameObject.SetActive(true);
				UpdatePlayerBet(_param);
				break;
			case TexasOption.Follow:
				actionInfo.transform.Find("Player/操作/跟注").gameObject.SetActive(true);
				UpdatePlayerBet(_param);
				break;
			case TexasOption.AddCoin:
				actionInfo.transform.Find("Player/操作/加注").gameObject.SetActive(true);
				UpdatePlayerBet(_param);
				break;
			case TexasOption.Give:
				// 设置弃牌状态并显示弃牌图标
				player.IsGive = true;
				actionInfo.transform.Find("Player/操作/弃牌").gameObject.SetActive(true);
				actionInfo.transform.Find("Player/操作/弃牌2").gameObject.SetActive(true);
				break;
			case TexasOption.Look:
				actionInfo.transform.Find("Player/操作/看牌").gameObject.SetActive(true);
				break;
			case TexasOption.ProtectOne:
			case TexasOption.ProtectTwo:
				actionInfo.transform.Find("Player/操作/保险").gameObject.SetActive(true);
				UpdatePlayerBet(_param);
				break;
			case TexasOption.Blind:
				UpdatePlayerBet(_param);
				break;
		}
	}

	// 重置玩家操作显示
	private void ResetPlayerActionDisplay(GameObject _seatObj)
	{
		// 获取玩家信息
		Transform playerTrans = _seatObj.transform.Find("Player");
		if (!playerTrans) return;

		// 保存当前弃牌状态
		bool isGiveState = playerTrans.Find("操作/弃牌").gameObject.activeSelf;

		// 重置所有操作显示
		foreach (Transform t in playerTrans.Find("操作"))
		{
			t.gameObject.SetActive(false);
		}

		// 如果之前是弃牌状态，则保持弃牌状态显示
		playerTrans.Find("操作/弃牌").gameObject.SetActive(isGiveState);
		playerTrans.Find("操作/弃牌2").gameObject.SetActive(isGiveState);
		playerTrans.Find("特效").gameObject.SetActive(false);
	}

	// 回退到上一个状态（完全重置并重播）
	private void RevertToPreviousState()
	{
		// 获取当前状态和当前索引
		int currentIndex = m_CurrentOptionIndex;
		DZOption currentOption = m_Options[currentIndex - 1];
		
		// 找到当前状态的起始点
		int stateStartIndex = FindStateStartIndex(currentIndex, currentOption.state);
		
		// 查找前一个状态的起始索引
		int prevStateIndex = FindPreviousStateIndex(stateStartIndex);
		
		// 重置界面状态
		ResetAllSeatsState();
		
		// 重置底池金额和玩家下注记录
		ResetPotAndBets(prevStateIndex);
		
		// 重置玩家金币到前一状态开始
		ResetPlayersCoinsToState(stateStartIndex);
		
		// 更新公共牌显示
		UpdateCommonCards(currentOption.state);
		
		// 重新应用当前阶段的所有操作
		if (stateStartIndex > 0)
		{
			ReapplyOperations(stateStartIndex, currentIndex);
		}
		else
		{
			// 如果回退到游戏开始，重新设置盲注
			ResetAllSeatsState();
			UpdateBlinds();
		}
	}

	// 找到特定状态的起始索引
	private int FindStateStartIndex(int fromIndex, TexasState state)
	{
		int index = fromIndex - 1;
		while (index > 0 && m_Options[index - 1].state == state)
		{
			index--;
		}
		return index;
	}

	// 找到前一个状态的起始索引
	private int FindPreviousStateIndex(int stateStartIndex)
	{
		if (stateStartIndex <= 1) return 0;
		
		TexasState currentState = m_Options[stateStartIndex - 1].state;
		int index = stateStartIndex - 1;
		
		while (index > 0)
		{
			if (m_Options[index - 1].state != currentState)
			{
				return index;
			}
			index--;
		}
		
		return 0;
	}

	// 重置底池和玩家下注记录
	private void ResetPotAndBets(int upToIndex)
	{
		m_CurrentCoin = 0;
		m_CurrentStatePlayerBets.Clear();
		
		Dictionary<long, float> tempStateBets = new Dictionary<long, float>();
		
		// 计算前一阶段的底池
		for (int i = 0; i < upToIndex; i++)
		{
			if (m_Options[i].param != null && m_Options[i].param.Param > 0)
			{
				long playerId = m_Options[i].param.PlayerId;
				if (!tempStateBets.ContainsKey(playerId))
				{
					tempStateBets[playerId] = 0;
				}
				tempStateBets[playerId] += m_Options[i].param.Param;
			}
		}
		
		// 累加到底池
		foreach (var playerBet in tempStateBets)
		{
			m_CurrentCoin += playerBet.Value;
		}
		
		// 更新底池显示
		varCurrentCoin.text = m_CurrentCoin.ToString("F2");
	}

	// 查找玩家上一个操作
	private DZOption FindLastPlayerAction(long playerId)
	{
		for (int i = m_CurrentOptionIndex - 1; i >= 0; i--)
		{
			if (m_Options[i].playerId == playerId && m_Options[i].param != null)
			{
				return m_Options[i];
			}
		}
		return null;
	}

	// 重新应用操作
	private void ReapplyOperations(int fromIndex, int toIndex)
	{
		for (int i = fromIndex; i < toIndex; i++)
		{
			UpdatePlayerAction(m_Options[i].param);
		}
	}

	// 重置所有玩家筹码到特定阶段的开始
	private void ResetPlayersCoinsToState(int stateStartIndex)
	{
		// 如果是游戏开始，使用初始筹码
		if (stateStartIndex <= 0)
		{
			foreach (DeskPlayer player in m_Playback.DeskPlayer)
			{
				player.Coin = player.BetCoin;
				GameObject seatinfo = seatlist[(int)player.Pos - 1];
				seatinfo.transform.Find("Player/分数/Value").GetComponent<Text>().text = player.Coin;
			}
			return;
		}

		// 重置各玩家筹码为当前阶段开始前的值
		foreach (DeskPlayer player in m_Playback.DeskPlayer)
		{
			// 从初始筹码开始计算
			float coinValue = float.Parse(player.BetCoin);
            
			// 计算之前所有操作的下注总和和保险费用
			for (int i = 0; i < stateStartIndex; i++)
			{
				if (m_Options[i].param != null && m_Options[i].param.PlayerId == player.BasePlayer.PlayerId)
				{
					// 保险操作不扣除玩家金币
					if (m_Options[i].param.Option != TexasOption.ProtectOne && 
					    m_Options[i].param.Option != TexasOption.ProtectTwo)
					{
						switch (m_Options[i].param.Option)
						{
							case TexasOption.Allin:
							case TexasOption.Follow:
							case TexasOption.AddCoin:
							case TexasOption.Blind:
								coinValue -= m_Options[i].param.Param;
								break;
						}
					}
				}
			}
            
			// 更新玩家筹码
			player.Coin = Mathf.Max(0, coinValue).ToString("F2");
			GameObject seatinfo = seatlist[(int)player.Pos - 1];
			seatinfo.transform.Find("Player/分数/Value").GetComponent<Text>().text = player.Coin;
		}
	}

	#endregion
}

using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityGameFramework.Runtime;
using static UtilityBuiltin;

class NNOption {
	public long time;
	public OptionType option;
	public long playerId;
	public string param1;
	public List<int> param2;
	public int step;
}

class NNPlayerInfo {
	public DeskPlayer deskplayer;
	public List<int> handcards = new();

	public string rob;
	public int robstep;
	public bool banker = false;
	public string bet;
	public int betstep;
	public string profit = "";
	public List<int> allcards = new();
	public int cardstep;
}

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class NNRecordPanel : UIFormBase
{
	public Text textMaxRatio;
	public Text textRoomName;
	public Text textRoomID;
	public Text textBaseCoin;
	public Text textMinBringIn;
	public Text textNiuRobBanker;
	public Text textMaxRate;

	public List<GameObject> seatlist;

	public Msg_PlayBackRs msg_playback;

	int currentOptionIndex = 0;
	
	// 回放控制变量
	private bool isPaused = false;
	private float m_ReplaySpeed = 1.0f;
	private Coroutine m_ReplayCoroutine = null;
	
	// 预设速度列表
	private readonly float[] m_SpeedPresets = { 1.0f, 2.0f, 3.0f, 0.5f };
	private int m_CurrentSpeedIndex = 0;

	private readonly Dictionary<long, NNPlayerInfo> m_PlayerInfos = new();

	readonly List<NNOption> m_Options = new();

	// 1. 增加 isReplaying 标志
	private bool isReplaying = false;

	public void Init()
	{
		currentOptionIndex = 0;
		isPaused = false;
		varStopImage.SetActive(false);
		varPlayImage.SetActive(true);
		varSpeedText.text = $"x{m_ReplaySpeed}";

		textRoomName.text = "[" + msg_playback.Config.Config.DeskName + "]";
		textRoomID.text = "[" + msg_playback.DeskId + "]";
		textBaseCoin.text = msg_playback.Config.Config.BaseCoin.ToString();
		textMinBringIn.text = "最低买入:" + msg_playback.Config.Config.MinBringIn;
		var noNiuRobBanker = "无牛能抢庄";
		if (msg_playback.Config.NiuConfig.NoNiuRobBanker)
		{
			noNiuRobBanker = "无牛禁抢庄";
		}
		textNiuRobBanker.text = noNiuRobBanker;
		textMaxRate.gameObject.SetActive(false);
		textMaxRatio.gameObject.SetActive(false);

		foreach (GameObject seatinfo in seatlist)
		{
			seatinfo.transform.Find("Empty").gameObject.SetActive(true);
			seatinfo.transform.Find("Player").gameObject.SetActive(false);
		}

		foreach (NNPlayerInfo playerinfo in m_PlayerInfos.Values)
		{
			GameObject seatinfo = seatlist[(int)playerinfo.deskplayer.Pos - 1];
			seatinfo.transform.Find("Empty").gameObject.SetActive(false);
			seatinfo.transform.Find("Player").gameObject.SetActive(true);
			seatinfo.transform.Find("Player/name").gameObject.GetComponent<Text>().text = playerinfo.deskplayer.BasePlayer.Nick;
			seatinfo.transform.Find("Player/scoreImage/value").GetComponent<Text>().text = playerinfo.deskplayer.Coin.ToString();
			seatinfo.transform.Find("Player/vip").gameObject.SetActive(playerinfo.deskplayer.BasePlayer.Vip > 0);
			for (var i = 0; i < 5; i++)
			{
				seatinfo.transform.Find("Player/BaseCards/card" + i).gameObject.SetActive(false);
			}
			seatinfo.transform.Find("Player/operateType0").gameObject.SetActive(false);
			seatinfo.transform.Find("Player/operateType1").gameObject.SetActive(false);
			seatinfo.transform.Find("Player/operateType2").gameObject.SetActive(false);
			seatinfo.transform.Find("Player/operateType3").gameObject.SetActive(false);
			seatinfo.transform.Find("Player/flag").gameObject.SetActive(false);
			seatinfo.transform.Find("Player/numImage1").gameObject.SetActive(false);
			seatinfo.transform.Find("Player/numImage2").gameObject.SetActive(false);
			seatinfo.transform.Find("Player/numImage3").gameObject.SetActive(false);
			seatinfo.transform.Find("Player/chipImg").gameObject.SetActive(false);
			seatinfo.transform.Find("Player/profitImage").gameObject.SetActive(false);
			seatinfo.transform.Find("Player/profitImage/Image1").gameObject.SetActive(false);
			seatinfo.transform.Find("Player/profitImage/Image2").gameObject.SetActive(false);
			seatinfo.transform.Find("Player/State/tween_effect").gameObject.SetActive(false);
			seatinfo.transform.Find("Player/State/tween_effect").gameObject.SetActive(false);
			seatinfo.transform.Find("Player/State").gameObject.SetActive(false);
		}
	}

	protected override void OnOpen(object userData)
	{
		base.OnOpen(userData);
		msg_playback = Msg_PlayBackRs.Parser.ParseFrom(Params.Get<VarByteArray>("PlayBack"));
		switch (msg_playback.Config.Config.PlayerNum) {
			case 5:
				seatlist[0].SetActive(true);
				seatlist[1].SetActive(true);
				seatlist[2].SetActive(true);
				seatlist[3].SetActive(false);
				seatlist[4].SetActive(false);
				seatlist[5].SetActive(false);
				seatlist[6].SetActive(true);
				seatlist[7].SetActive(true);
				break;
			case 6:
				seatlist[0].SetActive(true);
				seatlist[1].SetActive(true);
				seatlist[2].SetActive(true);
				seatlist[3].SetActive(false);
				seatlist[4].SetActive(true);
				seatlist[5].SetActive(false);
				seatlist[6].SetActive(true);
				seatlist[7].SetActive(true);
				break;
			case 8:
				seatlist[0].SetActive(true);
				seatlist[1].SetActive(true);
				seatlist[2].SetActive(true);
				seatlist[3].SetActive(true);
				seatlist[4].SetActive(true);
				seatlist[5].SetActive(true);
				seatlist[6].SetActive(true);
				seatlist[7].SetActive(true);
				break;
		}

		foreach (DeskPlayer deskplayer in msg_playback.DeskPlayer) {
			NNPlayerInfo playerinfo = new()
			{
				deskplayer = deskplayer
			};
			m_PlayerInfos[deskplayer.BasePlayer.PlayerId] = playerinfo;
		}
		Init();

		m_Options.Clear();
		NNOption o1 = new()
		{
			option = OptionType.ODeal
		};
		m_Options.Add(o1);
		for (var i = 0;i < msg_playback.Option.Count;i++) {
			Msg_PlayBackOption option = msg_playback.Option[i];
			NNOption o = new();
			switch (option.Option) {
				case OptionType.ODeal:
					if (option.Param2.Count == 4) {
						m_PlayerInfos[option.PlayerId].handcards = option.Param2.ToList();
					}
					break;
				case OptionType.ORob:
					o.option = OptionType.ORob;
					o.playerId = option.PlayerId;
					o.param1 = option.Param1;
					o.step = i;
					m_Options.Add(o);
					m_PlayerInfos[option.PlayerId].rob = option.Param1;
					m_PlayerInfos[option.PlayerId].robstep = i;
					break;
				case OptionType.OBanker:
					m_PlayerInfos[option.PlayerId].banker = true;
					o.option = OptionType.OBanker;
					o.playerId = option.PlayerId;
					o.param1 = option.Param1;
					o.step = i;
					m_Options.Add(o);
					break;
				case OptionType.OBet:
					o.option = OptionType.OBet;
					o.playerId = option.PlayerId;
					o.param1 = option.Param1;
					o.step = i;
					m_Options.Add(o);
					m_PlayerInfos[option.PlayerId].bet = option.Param1;
					m_PlayerInfos[option.PlayerId].betstep = i;
					break;
				case OptionType.OShowCard:
					o.option = OptionType.OShowCard;
					o.playerId = option.PlayerId;
					o.param1 = option.Param1;
					o.step = i;
					m_Options.Add(o);
					m_PlayerInfos[option.PlayerId].allcards = option.Param2.ToList();
					m_PlayerInfos[option.PlayerId].cardstep = i;
					break;
				case OptionType.OSettle:
					m_PlayerInfos[option.PlayerId].profit = option.Param1;
					break;
			}
		}
		NNOption o2 = new()
		{
			option = OptionType.OSettle
		};
		m_Options.Add(o2);
		
		// 开始自动回放
		m_ReplaySpeed = 1.0f;
		m_ReplayCoroutine = null;
		m_CurrentSpeedIndex = 0;
		StartReplay();
	}

	protected override void OnClose(bool isShutdown, object userData)
	{
		// 停止协程
		if (m_ReplayCoroutine != null)
		{
			StopCoroutine(m_ReplayCoroutine);
			m_ReplayCoroutine = null;
		}
		
		base.OnClose(isShutdown, userData);
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

	private void ReplayNextOperation()
	{
		if (currentOptionIndex > m_Options.Count - 1 || gameObject.activeInHierarchy == false)
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

	// 4. 开始回放
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

	// 5. 重新开始回放
	public void RestartReplay()
	{
		StopReplay();
		Init();
		StartReplay();
	}

	// 6. 停止回放
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

	// 7. 暂停回放
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

	// 8. 按钮事件调整
	protected override void OnButtonClick(object sender, string btId)
	{
		switch (btId)
		{
			case "关闭":
				GF.UI.Close(this.UIForm);
				break;
			case "上一步":
				if (isReplaying && !isPaused) {
					PauseReplay();
				}
				UnPlay();
				break;
			case "下一步":
				if (isReplaying && !isPaused) {
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

	#region 回放控制
	/// <summary>
	/// 调整回放速度
	/// </summary>
	private void AdjustReplaySpeed(bool increase)
	{
		// 循环切换预设速度
		m_CurrentSpeedIndex = (m_CurrentSpeedIndex + 1) % m_SpeedPresets.Length;
		m_ReplaySpeed = m_SpeedPresets[m_CurrentSpeedIndex];
		
		// 可以添加显示当前速度的UI更新
		varSpeedText.text = $"x{m_ReplaySpeed}";
		GF.LogInfo($"回放速度: {m_ReplaySpeed}x");
	}
	#endregion

	#region 回放步进控制

	public void Play()
	{
		if (currentOptionIndex >= m_Options.Count) return;
		var option = m_Options[currentOptionIndex];
		ApplyOption(option, false);
		currentOptionIndex++;
		varTxtPage.text = currentOptionIndex + "/" + m_Options.Count;
	}

	public void UnPlay()
	{
		if (currentOptionIndex <= 0) return;
		currentOptionIndex--;
		var option = m_Options[currentOptionIndex];
		ApplyOption(option, true);
		varTxtPage.text = currentOptionIndex + "/" + m_Options.Count;
	}

	private void ApplyOption(NNOption option, bool isUndo)
	{
		switch (option.option)
		{
			case OptionType.ODeal:
				if (!isUndo) ShowHandCardStep(option);
				else UndoHandCardStep(option);
				break;
			case OptionType.ORob:
				if (!isUndo) ShowRobStep(option);
				else UndoRobStep(option);
				break;
			case OptionType.OBanker:
				if (!isUndo) ShowBankerStep(option);
				else UndoBankerStep(option);
				break;
			case OptionType.OBet:
				if (!isUndo) ShowBetStep(option);
				else UndoBetStep(option);
				break;
			case OptionType.OShowCard:
				if (!isUndo) ShowLastCardStep(option);
				else UndoLastCardStep(option);
				break;
			case OptionType.OSettle:
				if (!isUndo) ShowProfitStep(option);
				else UndoProfitStep(option);
				break;
		}
	}

	// 只处理当前option.playerId相关UI和数据
	private void ShowHandCardStep(NNOption option)
	{
		foreach (var playerinfo in m_PlayerInfos)
		{
			var info = playerinfo.Value;
			GameObject seatinfo = seatlist[(int)info.deskplayer.Pos - 1];
			for (var i = 0; i < info.handcards.Count; i++)
			{
				seatinfo.transform.Find("Player/BaseCards/card" + i).gameObject.SetActive(true);
				seatinfo.transform.Find("Player/BaseCards/card" + i).GetComponent<Card>().Init(info.handcards[i]);
			}
		}
	}
	private void UndoHandCardStep(NNOption option)
	{
		foreach (var playerinfo in m_PlayerInfos)
		{
			var info = playerinfo.Value;
			GameObject seatinfo = seatlist[(int)info.deskplayer.Pos - 1];
			for (var i = 0; i < 5; i++)
			{
				seatinfo.transform.Find("Player/BaseCards/card" + i).gameObject.SetActive(false);
			}
		}
	}
	private void ShowRobStep(NNOption option)
	{
		if (!m_PlayerInfos.ContainsKey(option.playerId)) return;
		var playerinfo = m_PlayerInfos[option.playerId];
		GameObject seatinfo = seatlist[(int)playerinfo.deskplayer.Pos - 1];
		seatinfo.transform.Find("Player/operateType" + playerinfo.rob).gameObject.SetActive(true);
	}
	private void UndoRobStep(NNOption option)
	{
		if (!m_PlayerInfos.ContainsKey(option.playerId)) return;
		var playerinfo = m_PlayerInfos[option.playerId];
		GameObject seatinfo = seatlist[(int)playerinfo.deskplayer.Pos - 1];
		seatinfo.transform.Find("Player/operateType" + playerinfo.rob).gameObject.SetActive(false);
	}
	private void ShowBankerStep(NNOption option)
	{
		if (!m_PlayerInfos.ContainsKey(option.playerId)) return;
		var playerinfo = m_PlayerInfos[option.playerId];
		GameObject seatinfo = seatlist[(int)playerinfo.deskplayer.Pos - 1];
		if (playerinfo.banker)
		{
			seatinfo.transform.Find("Player/flag").gameObject.SetActive(true);
		}
		foreach (var info in m_PlayerInfos)
		{
			GameObject go = seatlist[(int)info.Value.deskplayer.Pos - 1];
			go.transform.Find("Player/operateType" + info.Value.rob).gameObject.SetActive(false);
		}
	}
	private void UndoBankerStep(NNOption option)
	{
		if (!m_PlayerInfos.ContainsKey(option.playerId)) return;
		var playerinfo = m_PlayerInfos[option.playerId];
		GameObject seatinfo = seatlist[(int)playerinfo.deskplayer.Pos - 1];
		seatinfo.transform.Find("Player/flag").gameObject.SetActive(false);
		foreach (var info in m_PlayerInfos)
		{
			GameObject go = seatlist[(int)info.Value.deskplayer.Pos - 1];
			go.transform.Find("Player/operateType" + info.Value.rob).gameObject.SetActive(true);
		}
	}
	private void ShowBetStep(NNOption option)
	{
		if (!m_PlayerInfos.ContainsKey(option.playerId)) return;
		var playerinfo = m_PlayerInfos[option.playerId];
		if (playerinfo.bet == null) return;
		GameObject seatinfo = seatlist[(int)playerinfo.deskplayer.Pos - 1];
		seatinfo.transform.Find("Player/chipImg").gameObject.SetActive(true);
		seatinfo.transform.Find("Player/chipImg/value").GetComponent<Text>().text = playerinfo.bet;
	}
	private void UndoBetStep(NNOption option)
	{
		if (!m_PlayerInfos.ContainsKey(option.playerId)) return;
		var playerinfo = m_PlayerInfos[option.playerId];
		GameObject seatinfo = seatlist[(int)playerinfo.deskplayer.Pos - 1];
		seatinfo.transform.Find("Player/chipImg").gameObject.SetActive(false);
	}
	private void ShowLastCardStep(NNOption option)
	{
		if (!m_PlayerInfos.ContainsKey(option.playerId)) return;
		var playerinfo = m_PlayerInfos[option.playerId];
		GameObject seatinfo = seatlist[(int)playerinfo.deskplayer.Pos - 1];
		GF.UI.LoadSprite(AssetsPath.GetSpritesPath("NN/CardType/" + ShowType(playerinfo.deskplayer.Niu) + ".png"), (sprite) =>
		{
			if (seatinfo == null) return;
			Image imageNiuType = seatinfo.transform.Find("Player/State/Image").GetComponent<Image>();
			imageNiuType.sprite = sprite;
			imageNiuType.SetNativeSize();
			if (playerinfo.deskplayer.Niu >= 7)
			{
				seatinfo.transform.Find("Player/State/tween_effect").gameObject.SetActive(true);
				seatinfo.transform.Find("Player/State/tween_effect").gameObject.SetActive(true);
			}
			seatinfo.transform.Find("Player/State").gameObject.SetActive(true);
		});
		for (var i = 0; i < playerinfo.allcards.Count; i++)
		{
			seatinfo.transform.Find("Player/BaseCards/card" + i).gameObject.SetActive(true);
			seatinfo.transform.Find("Player/BaseCards/card" + i).GetComponent<Card>().Init(playerinfo.allcards[i]);
		}
	}
	private void UndoLastCardStep(NNOption option)
	{
		if (!m_PlayerInfos.ContainsKey(option.playerId)) return;
		var playerinfo = m_PlayerInfos[option.playerId];
		GameObject seatinfo = seatlist[(int)playerinfo.deskplayer.Pos - 1];
		seatinfo.transform.Find("Player/State/tween_effect").gameObject.SetActive(false);
		seatinfo.transform.Find("Player/State/tween_effect").gameObject.SetActive(false);
		seatinfo.transform.Find("Player/State").gameObject.SetActive(false);
		// 先全部隐藏
		for (var i = 0; i < 5; i++)
		{
			seatinfo.transform.Find("Player/BaseCards/card" + i).gameObject.SetActive(false);
		}
		// 再还原为发牌状态
		for (var i = 0; i < playerinfo.handcards.Count; i++)
		{
			seatinfo.transform.Find("Player/BaseCards/card" + i).gameObject.SetActive(true);
			seatinfo.transform.Find("Player/BaseCards/card" + i).GetComponent<Card>().Init((int)playerinfo.handcards[i]);
		}
	}
	private void ShowProfitStep(NNOption option)
	{
		foreach (var info in m_PlayerInfos)
		{
			var playerinfo = info.Value;
			GameObject seatinfo = seatlist[(int)playerinfo.deskplayer.Pos - 1];
			float settleCoin = float.Parse(playerinfo.deskplayer.Coin) + float.Parse(playerinfo.profit);
			seatinfo.transform.Find("Player/scoreImage/value").GetComponent<Text>().text = Util.FormatAmount(settleCoin);
			seatinfo.transform.Find("Player/profitImage").gameObject.SetActive(true);
			seatinfo.transform.Find("Player/profitImage/profit").GetComponent<Text>().FormatRichText(playerinfo.profit,"#000000","#000000");
			if (float.Parse(playerinfo.profit) >= 0)
			{
				seatinfo.transform.Find("Player/profitImage/Image2").gameObject.SetActive(true);
			}
			else
			{
				seatinfo.transform.Find("Player/profitImage/Image1").gameObject.SetActive(true);
			}
		}
	}
	private void UndoProfitStep(NNOption option)
	{
		foreach (var info in m_PlayerInfos)
		{
			var playerinfo = info.Value;
			GameObject seatinfo = seatlist[(int)playerinfo.deskplayer.Pos - 1];
			seatinfo.transform.Find("Player/scoreImage/value").GetComponent<Text>().text = playerinfo.deskplayer.Coin.ToString();
			seatinfo.transform.Find("Player/profitImage").gameObject.SetActive(false);
			seatinfo.transform.Find("Player/profitImage/Image1").gameObject.SetActive(false);
			seatinfo.transform.Find("Player/profitImage/Image2").gameObject.SetActive(false);
		}
	}

	#endregion

	private string ShowType(int n)
	{
		switch (n) {
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
}

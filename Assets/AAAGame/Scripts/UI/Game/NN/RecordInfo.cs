﻿using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class RecordInfo : MonoBehaviour
{
	public Text TextRecord;

	[SerializeField] private float minDuration = 0.5f;
	[SerializeField] private int maxDuration = 10;

	private long PlayerID = 0;
	private int RoomID = 0;
	private string DeviceName = "";
	private float duration = 0f;
	private AudioClip audioClip;
	private bool isRecording = false;
	private bool isUploading = false;

	void OnEnable() {
		duration = 0f;
	}

	void Update() {
		if (isRecording == true) {
			duration += Time.deltaTime;
			TextRecord.text = duration.ToString("F2") + "s";
		}
		if (isUploading == true) {
			TextRecord.text = "发送中...";
		}
	}

	public void StartRecord(long playerID, int roomID) {
		if (isRecording || isUploading) {
			GF.LogInfo("正在录音中..." + (isUploading ? "发送中..." : "") + (isUploading ? "上传中..." : ""));
			return;
		}
		PlayerID = playerID;
		RoomID = roomID;
		
		if (Microphone.devices.Length == 0) {
			GF.StaticUI.ShowError("没有检测到麦克风设备！");
			return;
		}

		try {
			DeviceName = Microphone.devices[0];
			audioClip = Microphone.Start(DeviceName, false, maxDuration, 16000);
			if (audioClip == null) {
				throw new System.Exception("Failed to start microphone");
			}
			
			GF.LogInfo($"开始录音. 设备: {DeviceName}");
			gameObject.SetActive(true);
			isRecording = true;
			CoroutineRunner.Instance.StartCoroutine(RecordTimer());
		}
		catch (System.Exception e) {
			GF.LogError("录音启动失败", e.Message);
			gameObject.SetActive(false);
			CleanupResources();
		}
	}

	IEnumerator RecordTimer()
	{
		#if !UNITY_WEBGL
		while (Microphone.IsRecording(DeviceName) == true) {
			if (duration >= maxDuration) {
				StopRecord();
				break;
			}
			yield return null;
		}
		#else
		yield return null;
		#endif
	}

	public void StopRecord() {
		if (!gameObject.activeSelf) return;
		
		#if !UNITY_WEBGL
		byte[] voicedata = null;
		do {
			if (audioClip == null) break;
			if (DeviceName == "") break;
			if (Microphone.IsRecording(DeviceName) == false) break;
			int position = Microphone.GetPosition(DeviceName);
			GF.LogInfo("结束录音", $"samples:{audioClip.samples} channels:{audioClip.channels} duration:{duration} position: {position}");
			Microphone.End(DeviceName);

			if (duration < minDuration) {
				GF.UI.ShowToast($"录音时间太短（至少需要 {minDuration} 秒）");
				break;
			}

			if (position == 0) {
				GF.UI.ShowToast("录音失败");
				break;
			}

			// 转换音频数据
			float[] samples = new float[position * audioClip.channels];
			audioClip.GetData(samples, 0);
			
			// 添加容错处理
			if (samples == null || samples.Length == 0) {
				GF.LogError("音频数据转换失败", "samples数组为空");
				break;
			}

			// PCM转换
			short[] pcmData = new short[samples.Length];
			for (int i = 0; i < samples.Length; i++) {
				pcmData[i] = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
			}

			// WAV文件封装
			using var memoryStream = new MemoryStream();
			using var writer = new BinaryWriter(memoryStream);
			writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
			writer.Write(36 + pcmData.Length * 2);
			writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
			writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
			writer.Write(16);
			writer.Write((short)1);
			writer.Write((short)audioClip.channels);
			writer.Write(audioClip.frequency);
			writer.Write(audioClip.frequency * audioClip.channels * 16 / 8);
			writer.Write((short)(audioClip.channels * 16 / 8));
			writer.Write((short)16);
			writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
			writer.Write(pcmData.Length * 2);
			foreach (var sample in pcmData) {
				writer.Write(sample);
			}

			GF.LogInfo("录音完成.", $"size:{memoryStream.Length} length:{audioClip.length}");
			voicedata = memoryStream.ToArray();
			CoroutineRunner.Instance.StartCoroutine(SendVoice(voicedata));
		} while (false);
		#endif
		
		// 修复状态重置逻辑
		isRecording = false;
		isUploading = false; // 新增状态重置
		CleanupResources();  // 新增资源清理
		audioClip = null;
		
		// 无论是否成功都隐藏界面
		gameObject.SetActive(false);
	}

	public void CancelRecord() {
		if (!isRecording && !isUploading) return;
		
		try {
			#if !UNITY_WEBGL
			if (!string.IsNullOrEmpty(DeviceName) && Microphone.IsRecording(DeviceName)) {
				Microphone.End(DeviceName);
				GF.LogInfo("取消录音");
			}
			#endif
		}
		catch (System.Exception e) {
			GF.LogError("取消录音时发生错误", e.Message);
		}
		finally {
			// 重置所有状态
			isRecording = false;
			isUploading = false;
			CleanupResources();
			gameObject.SetActive(false);
		}
	}

	private void CleanupResources() {
		if (audioClip != null) {
			#if !UNITY_WEBGL
			// 确保释放音频资源
			if (!string.IsNullOrEmpty(DeviceName) && Microphone.IsRecording(DeviceName)) {
				Microphone.End(DeviceName);
			}
			#endif
			// Unity建议使用Destroy来释放AudioClip
			Destroy(audioClip);
			audioClip = null;
		}
		DeviceName = string.Empty;
	}

	IEnumerator SendVoice(byte[] data) {
		isUploading = true;
		WWWForm form = new();
		form.AddField("playerId", PlayerID.ToString());
		form.AddField("deskId", RoomID);
		form.AddBinaryData("file", data, "voice.wav", "audio/wav");
		
		string url = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}/voice/upload";
		using UnityWebRequest request = UnityWebRequest.Post(url, form);
		request.timeout = 10;
		
		yield return request.SendWebRequest();

		try {
			if (request.result != UnityWebRequest.Result.Success) {
				GF.LogError("上传语音失败", $"URL: {url}\nError: {request.error}\nResponse: {request.downloadHandler?.text}");
			}
			else {
				GF.LogInfo("上传语音成功", $"URL: {url}\nResponse: {request.downloadHandler.text}");
			}
		}
		catch (System.Exception e) {
			GF.LogError("上传过程中发生异常", e.Message);
		}
		finally {
			isUploading = false;
			gameObject.SetActive(false);
		}
	}
}

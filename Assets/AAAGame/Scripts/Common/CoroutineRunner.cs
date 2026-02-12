﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner _instance;
    private Dictionary<string, Coroutine> activeCountdowns = new Dictionary<string, Coroutine>();
    private Dictionary<GameObject, Coroutine> activeCountdownsByObject = new Dictionary<GameObject, Coroutine>();

    public static CoroutineRunner Instance
    {
        get
        {
            if (_instance == null)
            {
                var obj = new GameObject("CoroutineRunner");
                _instance = obj.AddComponent<CoroutineRunner>();
                DontDestroyOnLoad(obj);

            }
            return _instance;
        }
    }

    public Coroutine RunCoroutine(IEnumerator routine)
    {
        return StartCoroutine(routine);
    }

    public void PauseAllCoroutines()
    {
        StopAllCoroutines();
    }

    #region 通用倒计时功能
    
    /// <summary>
    /// 启动一个通用的倒计时显示
    /// </summary>
    /// <param name="seconds">倒计时秒数</param>
    /// <param name="textComponent">显示倒计时的Text组件</param>
    /// <param name="onComplete">倒计时完成回调</param>
    /// <param name="format">时间格式："mm:ss" 或 "ss"，默认为"mm:ss"</param>
    /// <param name="countdownId">倒计时ID，用于管理多个倒计时，默认为"default"</param>
    /// <returns>返回倒计时协程引用</returns>
    public Coroutine StartCountdown(int seconds, Text textComponent, Action onComplete = null, string format = "mm:ss", string countdownId = "default")
    {
        // 停止之前的倒计时（如果存在）
        StopCountdown(countdownId);
        
        var coroutine = StartCoroutine(CountdownCoroutine(seconds, textComponent, onComplete, format, countdownId, null));
        activeCountdowns[countdownId] = coroutine;
        
        return coroutine;
    }

    /// <summary>
    /// 启动一个通用的倒计时显示（使用GameObject作为key）
    /// </summary>
    /// <param name="seconds">倒计时秒数</param>
    /// <param name="textComponent">显示倒计时的Text组件</param>
    /// <param name="keyObject">作为key的GameObject</param>
    /// <param name="onComplete">倒计时完成回调</param>
    /// <param name="format">时间格式："mm:ss" 或 "ss"，默认为"mm:ss"</param>
    /// <returns>返回倒计时协程引用</returns>
    public Coroutine StartCountdown(int seconds, Text textComponent, GameObject keyObject, Action onComplete = null, string format = "mm:ss")
    {
        // 停止之前的倒计时（如果存在）
        StopCountdown(keyObject);
        
        var coroutine = StartCoroutine(CountdownCoroutine(seconds, textComponent, onComplete, format, null, keyObject));
        activeCountdownsByObject[keyObject] = coroutine;
        
        return coroutine;
    }

    /// <summary>
    /// 停止指定的倒计时
    /// </summary>
    /// <param name="countdownId">倒计时ID</param>
    public void StopCountdown(string countdownId = "default")
    {
        if (activeCountdowns.TryGetValue(countdownId, out Coroutine coroutine) && coroutine != null)
        {
            StopCoroutine(coroutine);
            activeCountdowns.Remove(countdownId);
        }
    }

    /// <summary>
    /// 停止指定的倒计时（使用GameObject作为key）
    /// </summary>
    /// <param name="keyObject">作为key的GameObject</param>
    public void StopCountdown(GameObject keyObject)
    {
        if (keyObject != null && activeCountdownsByObject.TryGetValue(keyObject, out Coroutine coroutine) && coroutine != null)
        {
            StopCoroutine(coroutine);
            activeCountdownsByObject.Remove(keyObject);
        }
    }

    /// <summary>
    /// 停止所有倒计时
    /// </summary>
    public void StopAllCountdowns()
    {
        foreach (var kvp in activeCountdowns)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        activeCountdowns.Clear();

        foreach (var kvp in activeCountdownsByObject)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        activeCountdownsByObject.Clear();
    }

    /// <summary>
    /// 倒计时协程实现
    /// </summary>
    private IEnumerator CountdownCoroutine(int totalSeconds, Text textComponent, Action onComplete, string format, string countdownId, GameObject keyObject)
    {
        int remainingTime = totalSeconds;

        while (remainingTime > 0)
        {
            // 更新倒计时显示
            if (textComponent != null)
            {
                textComponent.text = FormatTime(remainingTime, format);
            }

            yield return new WaitForSeconds(1f);
            remainingTime--;
        }

        // 倒计时结束
        if (textComponent != null)
        {
            textComponent.text = FormatTime(0, format);
        }

        // 从活跃倒计时列表中移除
        if (!string.IsNullOrEmpty(countdownId))
        {
            activeCountdowns.Remove(countdownId);
        }
        if (keyObject != null)
        {
            activeCountdownsByObject.Remove(keyObject);
        }

        // 执行完成回调
        onComplete?.Invoke();
    }

    /// <summary>
    /// 格式化时间显示
    /// </summary>
    /// <param name="seconds">秒数</param>
    /// <param name="format">格式："mm:ss" 或 "ss"</param>
    /// <returns>格式化后的时间字符串</returns>
    private string FormatTime(int seconds, string format)
    {
        switch (format.ToLower())
        {
            case "ss":
                return seconds.ToString("D2");
            case "mm:ss":
                int minutes = seconds / 60;
                int secs = seconds % 60;
                return $"{minutes:D2}:{secs:D2}";
            default:
                return string.Format(format, seconds % 60);
        }
    }

    #endregion

}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 声音管理器 - 统一管理游戏音效和背景音乐
/// 使用方法:
/// - 播放音效: Sound.PlayEffect(AudioKeys.SOUND_BTN);
/// - 播放背景音乐: Sound.PlayMusic(AudioKeys.MJ_MUSIC_AUDIO_PLAYINGINGAME);
/// - 停止音乐: Sound.StopMusic();
/// - 设置音效开关: Sound.SetSfxEnabled(true/false);
/// - 设置音乐开关: Sound.SetMusicEnabled(true/false);
/// </summary>
public class SoundManager : MonoBehaviour
{
    #region 单例
    public static SoundManager Instance { get; private set; }
    #endregion

    #region 音频源
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private List<AudioSource> sfxSources;
    #endregion

    #region 配置参数
    [Header("Settings")]
    [SerializeField] private int maxSfxInstances = 10;
    [SerializeField] [Range(0, 1)] private float bgmVolume = 1f;
    [SerializeField] [Range(0, 1)] private float sfxVolume = 1f;
    [SerializeField] private float sfxFadeDuration = 0.2f;
    #endregion

    #region 内部状态
    private Queue<AudioSource> availableSfxSources;
    private bool isSfxEnabled = true;      // 音效开关
    private bool isMusicEnabled = false;   // 音乐开关
    private bool isMusicPlaying = false;   // 音乐是否正在播放
    private int muteSaveValue = 0;         // 静音状态保存值
    #endregion

    #region PlayerPrefs Keys
    private const string PREF_SFX_ENABLED = "Sound_SfxEnabled";
    private const string PREF_MUSIC_ENABLED = "Sound_MusicEnabled";
    private const string PREF_SFX_VOLUME = "Sound_SfxVolume";
    private const string PREF_MUSIC_VOLUME = "Sound_MusicVolume";
    #endregion

    #region 生命周期
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        // 配置背景音乐源
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.playOnAwake = false;

        // 初始化音效源池
        availableSfxSources = new Queue<AudioSource>();
        if (sfxSources == null)
        {
            sfxSources = new List<AudioSource>();
        }
        else
        {
            sfxSources.Clear();
        }

        for (int i = 0; i < maxSfxInstances; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.loop = false;
            source.volume = sfxVolume;
            source.playOnAwake = false;
            source.gameObject.AddComponent<SfxInstance>();
            sfxSources.Add(source);
            availableSfxSources.Enqueue(source);
        }

        // 加载保存的设置
        LoadSettings();
        
        GF.LogInfo($"[SoundManager] 初始化完成 - 音效:{(isSfxEnabled ? "开" : "关")} 音乐:{(isMusicEnabled ? "开" : "关")}");
    }

    private void LoadSettings()
    {
        // 加载音效开关
        isSfxEnabled = PlayerPrefs.GetInt(PREF_SFX_ENABLED, 1) == 1;
        
        // 加载音乐开关
        isMusicEnabled = PlayerPrefs.GetInt(PREF_MUSIC_ENABLED, 0) == 1;
        
        // 加载音量设置
        if (PlayerPrefs.HasKey(PREF_SFX_VOLUME))
        {
            sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOLUME);
        }
        if (PlayerPrefs.HasKey(PREF_MUSIC_VOLUME))
        {
            bgmVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME);
        }

        // 应用设置
        ApplySfxSetting();
        ApplyMusicSetting();
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt(PREF_SFX_ENABLED, isSfxEnabled ? 1 : 0);
        PlayerPrefs.SetInt(PREF_MUSIC_ENABLED, isMusicEnabled ? 1 : 0);
        PlayerPrefs.SetFloat(PREF_SFX_VOLUME, sfxVolume);
        PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, bgmVolume);
        PlayerPrefs.Save();
    }
    #endregion        

    #region 背景音乐 API
    /// <summary>
    /// 播放背景音乐 (通过音频名称)
    /// </summary>
    public void PlayBGM(string audioName)
    {
        if (!isMusicEnabled) return;
        
        _ = GF.Sound.PlayBGM(audioName);
        isMusicPlaying = true;
    }

    /// <summary>
    /// 播放背景音乐 (通过AudioClip)
    /// </summary>
    public void PlayBGM(AudioClip clip)
    {
        if (!isMusicEnabled || clip == null) return;

        if (bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
        
        bgmSource.clip = clip;
        bgmSource.Play();
        isMusicPlaying = true;
    }

    /// <summary>
    /// 停止背景音乐
    /// </summary>
    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
        isMusicPlaying = false;
    }

    /// <summary>
    /// 暂停背景音乐
    /// </summary>
    public void PauseBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Pause();
        }
    }

    /// <summary>
    /// 恢复背景音乐
    /// </summary>
    public void ResumeBGM()
    {
        if (bgmSource != null && !bgmSource.isPlaying && bgmSource.clip != null)
        {
            bgmSource.UnPause();
        }
    }
    #endregion

    #region 音效 API
    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="clip">音频片段</param>
    /// <param name="priority">优先级(数值越大优先级越高)</param>
    /// <param name="onComplete">播放完成回调</param>
    public void PlaySFX(AudioClip clip, int priority = 0, Action onComplete = null)
    {
        if (!isSfxEnabled || clip == null) return;

        if (availableSfxSources.Count > 0)
        {
            var source = availableSfxSources.Dequeue();
            source.clip = clip;
            source.volume = sfxVolume;
            source.Play();
            StartCoroutine(ReturnSfxSourceToPool(source, clip.length, onComplete));
        }
        else
        {
            // 查找最低优先级的正在播放的音效
            var lowestPrioritySource = sfxSources
                .Where(s => s.isPlaying)
                .OrderBy(s => s.GetComponent<SfxInstance>().priority)
                .FirstOrDefault();

            if (lowestPrioritySource != null &&
                lowestPrioritySource.GetComponent<SfxInstance>().priority < priority)
            {
                StartCoroutine(FadeOutAndReplace(lowestPrioritySource, clip, priority, onComplete));
            }
        }
    }
    #endregion    
    
    #region 内部协程
    private IEnumerator FadeOutAndReplace(AudioSource source, AudioClip newClip, int priority, Action onComplete = null)
    {
        float startVolume = source.volume;
        
        // 淡出当前音效
        while (source.volume > 0)
        {
            source.volume -= startVolume * Time.deltaTime / sfxFadeDuration;
            yield return null;
        }
        
        source.Stop();
        source.clip = newClip;
        source.GetComponent<SfxInstance>().priority = priority;
        source.volume = sfxVolume;
        source.Play();
        
        StartCoroutine(ReturnSfxSourceToPool(source, newClip.length, onComplete));
    }

    private IEnumerator ReturnSfxSourceToPool(AudioSource source, float delay, Action onComplete = null)
    {
        yield return new WaitForSeconds(delay);
        source.GetComponent<SfxInstance>().priority = 0;
        availableSfxSources.Enqueue(source);
        onComplete?.Invoke();
    }

    private class SfxInstance : MonoBehaviour
    {
        public int priority = 0;
    }
    #endregion

    #region 音量控制
    /// <summary>
    /// 设置背景音乐音量
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
        }
        SaveSettings();
    }

    /// <summary>
    /// 设置音效音量
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        foreach (var source in sfxSources)
        {
            if (source != null)
            {
                source.volume = sfxVolume;
            }
        }
        SaveSettings();
    }

    /// <summary>
    /// 设置主音量(同时设置音乐和音效)
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        SetBGMVolume(volume);
        SetSFXVolume(volume);
    }

    /// <summary>
    /// 获取背景音乐音量
    /// </summary>
    public float GetBGMVolume() => bgmVolume;

    /// <summary>
    /// 获取音效音量
    /// </summary>
    public float GetSFXVolume() => sfxVolume;
    #endregion

    #region 开关控制
    /// <summary>
    /// 设置音效开关
    /// </summary>
    public void SetSfxEnabled(bool enabled)
    {
        isSfxEnabled = enabled;
        ApplySfxSetting();
        SaveSettings();
        GF.LogInfo($"[SoundManager] 音效: {(enabled ? "开启" : "关闭")}");
    }

    /// <summary>
    /// 设置音乐开关
    /// </summary>
    public void SetMusicEnabled(bool enabled)
    {
        isMusicEnabled = enabled;
        ApplyMusicSetting();
        SaveSettings();
        GF.LogInfo($"[SoundManager] 音乐: {(enabled ? "开启" : "关闭")}");
    }

    /// <summary>
    /// 切换音效开关
    /// </summary>
    public void ToggleSfx()
    {
        SetSfxEnabled(!isSfxEnabled);
    }

    /// <summary>
    /// 切换音乐开关
    /// </summary>
    public void ToggleMusic()
    {
        SetMusicEnabled(!isMusicEnabled);
    }

    /// <summary>
    /// 静音/取消静音所有声音 (兼容旧API)
    /// </summary>
    public void ToggleMute(bool mute)
    {
        if (mute)
        {
            PauseAll();
        }
        else
        {
            ResumeAll();
        }
        muteSaveValue = mute ? 1 : 0;
        PlayerPrefs.SetInt("soundMute", muteSaveValue);
        PlayerPrefs.Save();
        GF.LogInfo($"[SoundManager] {(mute ? "静音" : "取消静音")}");
    }

    private void ApplySfxSetting()
    {
        if (!isSfxEnabled)
        {
            // 停止所有音效
            foreach (var source in sfxSources)
            {
                if (source != null && source.isPlaying)
                {
                    source.Stop();
                }
            }
        }
    }

    private void ApplyMusicSetting()
    {
        if (!isMusicEnabled)
        {
            // 停止背景音乐
            StopBGM();
        }
    }

    /// <summary>
    /// 获取音效开关状态
    /// </summary>
    public bool IsSfxEnabled() => isSfxEnabled;

    /// <summary>
    /// 获取音乐开关状态
    /// </summary>
    public bool IsMusicEnabled() => isMusicEnabled;

    /// <summary>
    /// 获取音乐是否正在播放
    /// </summary>
    public bool IsMusicPlaying() => isMusicPlaying;
    #endregion

    #region 暂停/恢复控制
    /// <summary>
    /// 暂停所有声音
    /// </summary>
    public void PauseAll()
    {
        if (bgmSource != null)
        {
            bgmSource.Pause();
            bgmSource.mute = true;
        }
        
        foreach (var source in sfxSources)
        {
            if (source != null)
            {
                source.Pause();
                source.mute = true;
            }
        }
    }

    /// <summary>
    /// 恢复所有声音
    /// </summary>
    public void ResumeAll()
    {
        if (bgmSource != null)
        {
            bgmSource.UnPause();
            bgmSource.mute = false;
        }
        
        foreach (var source in sfxSources)
        {
            if (source != null)
            {
                source.UnPause();
                source.mute = false;
            }
        }
    }

    /// <summary>
    /// 停止所有声音
    /// </summary>
    public void StopAll()
    {
        StopBGM();
        
        foreach (var source in sfxSources)
        {
            if (source != null)
            {
                source.Stop();
            }
        }
    }

    /// <summary>
    /// 检查是否静音
    /// </summary>
    public bool IsMuted()
    {
        return (bgmSource == null || !bgmSource.isPlaying) && 
               !sfxSources.Any(s => s != null && s.isPlaying);
    }
    #endregion
}

/// <summary>
/// 声音管理静态便捷访问类
/// 提供简洁的API调用方式
/// 示例:
/// - Sound.PlayEffect(AudioKeys.SOUND_BTN);
/// - Sound.PlayMusic(AudioKeys.MJ_MUSIC_AUDIO_PLAYINGINGAME);
/// - Sound.SetSfxEnabled(false);
/// </summary>
public static class Sound
{
    private static SoundManager Manager => SoundManager.Instance;

    #region 音效播放
    /// <summary>
    /// 播放音效 (通过AudioKeys常量)
    /// </summary>
    public static void PlayEffect(string audioKey)
    {
        if (Manager != null)
        {
            _ = GF.Sound.PlayEffect(audioKey);
        }
    }

    /// <summary>
    /// 播放音效 (通过AudioClip)
    /// </summary>
    public static void PlayEffect(AudioClip clip, int priority = 0, Action onComplete = null)
    {
        Manager?.PlaySFX(clip, priority, onComplete);
    }

    /// <summary>
    /// 播放音效 (带间隔限制,防止重复播放)
    /// </summary>
    public static async void PlayEffectWithInterval(string audioKey, float interval)
    {
        if (Manager != null)
        {
            await GF.Sound.PlayEffect(audioKey, interval);
        }
    }
    #endregion

    #region 背景音乐播放
    /// <summary>
    /// 播放背景音乐 (通过AudioKeys常量)
    /// </summary>
    public static void PlayMusic(string audioKey)
    {
        Manager?.PlayBGM(audioKey);
    }

    /// <summary>
    /// 播放背景音乐 (通过AudioClip)
    /// </summary>
    public static void PlayMusic(AudioClip clip)
    {
        Manager?.PlayBGM(clip);
    }

    /// <summary>
    /// 停止背景音乐
    /// </summary>
    public static void StopMusic()
    {
        Manager?.StopBGM();
    }

    /// <summary>
    /// 暂停背景音乐
    /// </summary>
    public static void PauseMusic()
    {
        Manager?.PauseBGM();
    }

    /// <summary>
    /// 恢复背景音乐
    /// </summary>
    public static void ResumeMusic()
    {
        Manager?.ResumeBGM();
    }
    #endregion

    #region 开关控制
    /// <summary>
    /// 设置音效开关
    /// </summary>
    public static void SetSfxEnabled(bool enabled)
    {
        Manager?.SetSfxEnabled(enabled);
    }

    /// <summary>
    /// 设置音乐开关
    /// </summary>
    public static void SetMusicEnabled(bool enabled)
    {
        Manager?.SetMusicEnabled(enabled);
    }

    /// <summary>
    /// 切换音效开关
    /// </summary>
    public static void ToggleSfx()
    {
        Manager?.ToggleSfx();
    }

    /// <summary>
    /// 切换音乐开关
    /// </summary>
    public static void ToggleMusic()
    {
        Manager?.ToggleMusic();
    }

    /// <summary>
    /// 获取音效开关状态
    /// </summary>
    public static bool IsSfxEnabled()
    {
        return Manager?.IsSfxEnabled() ?? true;
    }

    /// <summary>
    /// 获取音乐开关状态
    /// </summary>
    public static bool IsMusicEnabled()
    {
        return Manager?.IsMusicEnabled() ?? false;
    }

    /// <summary>
    /// 获取音乐是否正在播放
    /// </summary>
    public static bool IsMusicPlaying()
    {
        return Manager?.IsMusicPlaying() ?? false;
    }
    #endregion

    #region 音量控制
    /// <summary>
    /// 设置音效音量
    /// </summary>
    public static void SetSfxVolume(float volume)
    {
        Manager?.SetSFXVolume(volume);
    }

    /// <summary>
    /// 设置音乐音量
    /// </summary>
    public static void SetMusicVolume(float volume)
    {
        Manager?.SetBGMVolume(volume);
    }

    /// <summary>
    /// 设置主音量
    /// </summary>
    public static void SetMasterVolume(float volume)
    {
        Manager?.SetMasterVolume(volume);
    }

    /// <summary>
    /// 获取音效音量
    /// </summary>
    public static float GetSfxVolume()
    {
        return Manager?.GetSFXVolume() ?? 1f;
    }

    /// <summary>
    /// 获取音乐音量
    /// </summary>
    public static float GetMusicVolume()
    {
        return Manager?.GetBGMVolume() ?? 1f;
    }
    #endregion

    #region 其他控制
    /// <summary>
    /// 暂停所有声音
    /// </summary>
    public static void PauseAll()
    {
        Manager?.PauseAll();
    }

    /// <summary>
    /// 恢复所有声音
    /// </summary>
    public static void ResumeAll()
    {
        Manager?.ResumeAll();
    }

    /// <summary>
    /// 停止所有声音
    /// </summary>
    public static void StopAll()
    {
        Manager?.StopAll();
    }
    #endregion
}

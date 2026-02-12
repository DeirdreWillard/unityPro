﻿using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using Cysharp.Threading.Tasks;

/// <summary>
/// 声音扩展 - 提供GameFramework.Sound的扩展方法
/// 注意: 推荐使用新的 Sound 静态类来调用声音
/// 例如: Sound.PlayEffect(AudioKeys.SOUND_BTN);
/// </summary>
public static class SoundExtension
{
    private static Dictionary<string, float> lastPlayEffectTags = new Dictionary<string, float>();

    public static void InitializeSoundManager(this SoundComponent soundCom)
    {
        var soundManagerObj = new GameObject("SoundManager");
        soundManagerObj.AddComponent<SoundManager>();
    }

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    public static async UniTask<int> PlayBGM(this SoundComponent soundCom, string name)
    {
        string assetName = UtilityBuiltin.AssetsPath.GetSoundPath(name);
        if (GFBuiltin.Resource.HasAsset(assetName) == GameFramework.Resource.HasAssetResult.NotExist) 
            return 0;
        
        var clip = await GFBuiltin.Resource.LoadAssetAwait<AudioClip>(assetName);
        if (clip == null) return 0;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM(clip);
        }
        return 1;
    }

    /// <summary>
    /// 播放音效 (通过AudioClip)
    /// </summary>
    public static async UniTask<int> PlayEffect(this SoundComponent soundCom, AudioClip clip, bool isLoop = false, System.Action onComplete = null)
    {
        if (clip == null) return 0;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(clip, isLoop ? -1 : 0, onComplete);
        }
        return 1;
    }

    /// <summary>
    /// 播放音效 (通过音频名称)
    /// </summary>
    public static async UniTask<int> PlayEffect(this SoundComponent soundCom, string name, bool isLoop = false, System.Action onComplete = null)
    {
        if (string.IsNullOrEmpty(name))
        {
            GF.LogError("PlayEffect name is null or empty");
            return 0;
        }
        GF.LogInfo_gsc("PlayEffect:" + name);
        string assetName = UtilityBuiltin.AssetsPath.GetSoundPath(name);
        if (GFBuiltin.Resource.HasAsset(assetName) == GameFramework.Resource.HasAssetResult.NotExist) 
        {
            GF.LogError("PlayEffect assetName not found:" + assetName);
            return 0;
        }

        var clip = await GFBuiltin.Resource.LoadAssetAwait<AudioClip>(assetName);
        if (clip == null) return 0;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(clip, 0, onComplete);
        }
        return 1;
    }

    /// <summary>
    /// 播放音效 (带间隔限制,防止短时间内重复播放)
    /// </summary>
    public static async UniTask PlayEffect(this SoundComponent soundCom, string name, float interval, System.Action onComplete = null)
    {
        bool hasKey = lastPlayEffectTags.ContainsKey(name);
        if (hasKey && Time.time - lastPlayEffectTags[name] < interval)
        {
            return;
        }

        string assetName = UtilityBuiltin.AssetsPath.GetSoundPath(name);
        if (GFBuiltin.Resource.HasAsset(assetName) == GameFramework.Resource.HasAssetResult.NotExist) 
            return;

        var clip = await GFBuiltin.Resource.LoadAssetAwait<AudioClip>(assetName);
        if (clip == null) return;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(clip, 0, onComplete);
        }

        if (hasKey) 
            lastPlayEffectTags[name] = Time.time;
        else 
            lastPlayEffectTags.Add(name, Time.time);
    }

    /// <summary>
    /// 播放震动
    /// </summary>
    public static void PlayVibrate(this SoundComponent soundCom, long time = Const.DefaultVibrateDuration)
    {
        if (soundCom.GetSoundGroup(Const.SoundGroup.Vibrate.ToString()).Mute)
        {
            return;
        }
#if UNITY_ANDROID || UNITY_IOS
        if (Application.platform == RuntimePlatform.Android)
        {
            Handheld.Vibrate();
        }
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            Handheld.Vibrate();
        }
#endif
    }
}

using UnityEngine;

/// <summary>
/// 麻将音效辅助类 - 2D独立版本
/// 支持多语言、男女声切换
/// 从 MahjongMachine/MahjongAudioHelper 迁移并简化
/// </summary>
public static class MahjongAudio
{
    /// <summary>
    /// 获取麻将牌报牌音效Key
    /// </summary>
    /// <param name="faceValue">麻将面值</param>
    /// <param name="language">语言类型（可选，默认使用当前语言）</param>
    /// <param name="gender">性别类型（可选，默认使用当前性别）</param>
    /// <returns>音频文件路径（包含扩展名）</returns>
    public static string GetCardVoiceKey(MahjongFaceValue faceValue, VoiceLanguage? language = null, PlayerType? gender = null)
    {
        VoiceLanguage lang = language ?? MahjongSettings.GetLanguage();
        PlayerType gen = gender ?? MahjongSettings.GetGender();

        // 语言前缀
        string langPrefix = GetLanguagePrefix(lang);
        
        // 性别路径
        string genderPath = GetGenderPath(gen);

        // 卡牌文件名
        string cardName = GetCardFileName(faceValue);

        if (string.IsNullOrEmpty(cardName))
            return null;

        // 生成AudioKeys中的常量名: MJ_XT_SPEAK_GIRL_CARDSOUND_TIAO1
        string audioKeyName = $"MJ_{langPrefix}_{genderPath}_CARDSOUND_{cardName}".ToUpper();
        
        // 通过AudioKeys.Get()获取对应的路径值
        return AudioKeys.Get(audioKeyName);
    }
    
    /// <summary>
    /// 获取麻将牌报牌音效Key（根据玩家ID自动选择性别）
    /// </summary>
    /// <param name="faceValue">麻将面值</param>
    /// <param name="playerId">玩家ID（0表示自己，基于哈希计算性别）</param>
    /// <param name="language">语言类型（可选，默认使用当前语言）</param>
    /// <returns>音频文件路径（包含扩展名）</returns>
    public static string GetCardVoiceKeyForPlayer(MahjongFaceValue faceValue, long playerId, VoiceLanguage? language = null)
    {
        VoiceLanguage lang = language ?? MahjongSettings.GetLanguage();
        PlayerType gen = MahjongSettings.GetGenderForPlayer(playerId);
        
        return GetCardVoiceKey(faceValue, lang, gen);
    }

    /// <summary>
    /// 获取操作音效Key（碰、吃、杠、胡、听、自摸等）
    /// </summary>
    /// <param name="actionName">操作名称,如: "peng", "chi", "gang", "hu", "ting", "zimo"</param>
    /// <param name="variation">变体索引（有些音效有多个版本，如 hu_0, hu_1）</param>
    /// <param name="language">语言类型（可选，默认使用当前语言）</param>
    /// <param name="gender">性别类型（可选，默认使用当前性别）</param>
    /// <returns>音频文件路径（包含扩展名）</returns>
    public static string GetActionVoiceKey(string actionName, int variation = 0, VoiceLanguage? language = null, PlayerType? gender = null)
    {
        if (string.IsNullOrEmpty(actionName))
            return null;

        VoiceLanguage lang = language ?? MahjongSettings.GetLanguage();
        PlayerType gen = gender ?? MahjongSettings.GetGender();

        // 语言前缀
        string langPrefix = GetLanguagePrefix(lang);
        
        // 性别路径
        string genderPath = GetGenderPath(gen);

        // 如果有变体索引，添加后缀（如 hu_0, hu_1）
        string finalActionName = variation > 0 ? $"{actionName}_{variation}" : $"{actionName}_0";

        // 生成AudioKeys中的常量名: MJ_XT_SPEAK_GIRL_CONTROLSOUND_HU_0
        string audioKeyName = $"MJ_{langPrefix}_{genderPath}_CONTROLSOUND_{finalActionName}".ToUpper();
        
        // 通过AudioKeys.Get()获取对应的路径值
        return AudioKeys.Get(audioKeyName);
    }
    
    /// <summary>
    /// 获取操作音效Key（根据玩家ID自动选择性别）
    /// </summary>
    /// <param name="actionName">操作名称,如: "peng", "chi", "gang", "hu", "ting", "zimo"</param>
    /// <param name="playerId">玩家ID（0表示自己，基于哈希计算性别）</param>
    /// <param name="variation">变体索引（有些音效有多个版本，如 hu_0, hu_1）</param>
    /// <param name="language">语言类型（可选，默认使用当前语言）</param>
    /// <returns>音频文件路径（包含扩展名）</returns>
    public static string GetActionVoiceKeyForPlayer(string actionName, long playerId, int variation = 0, VoiceLanguage? language = null)
    {
        if (string.IsNullOrEmpty(actionName))
            return null;

        VoiceLanguage lang = language ?? MahjongSettings.GetLanguage();
        PlayerType gen = MahjongSettings.GetGenderForPlayer(playerId);

        return GetActionVoiceKey(actionName, variation, lang, gen);
    }

    public static void PlayVoice(string audioKey)
    {
       Sound.PlayEffect(audioKey);
    }
    
    /// <summary>
    /// 判断玩法是否强制使用普通话
    /// 卡五星、血流成河强制使用普通话
    /// </summary>
    /// <param name="mjMethod">麻将玩法类型</param>
    /// <returns>是否强制普通话</returns>
    public static bool IsForceMandarin(NetMsg.MJMethod mjMethod)
    {
        return mjMethod == NetMsg.MJMethod.Kwx || mjMethod == NetMsg.MJMethod.Xl;
    }

    /// <summary>
    /// 获取玩法对应的语言（强制普通话的玩法返回普通话，其他返回null使用配置）
    /// </summary>
    /// <param name="mjMethod">麻将玩法类型</param>
    /// <returns>语言类型，null表示使用配置</returns>
    public static VoiceLanguage? GetLanguageForMethod(NetMsg.MJMethod mjMethod)
    {
        return IsForceMandarin(mjMethod) ? VoiceLanguage.Mandarin : (VoiceLanguage?)null;
    }

    /// <summary>
    /// 播放麻将牌报牌音效（根据玩家ID和玩法类型）
    /// 卡五星、血流成河强制使用普通话，其他玩法使用语音配置
    /// </summary>
    /// <param name="faceValue">麻将面值</param>
    /// <param name="playerId">玩家ID（0表示自己）</param>
    /// <param name="mjMethod">麻将玩法类型</param>
    public static void PlayCardVoiceForPlayer(MahjongFaceValue faceValue, long playerId, NetMsg.MJMethod mjMethod)
    {
        VoiceLanguage? languageToUse = GetLanguageForMethod(mjMethod);
        string audioKey = GetCardVoiceKeyForPlayer(faceValue, playerId, languageToUse);
        if (!string.IsNullOrEmpty(audioKey))
        {
           Sound.PlayEffect(audioKey);
        }
    }

    /// <summary>
    /// 播放操作音效（根据玩家ID和玩法类型）
    /// 卡五星、血流成河强制使用普通话，其他玩法使用语音配置
    /// </summary>
    /// <param name="actionName">操作名称</param>
    /// <param name="playerId">玩家ID（0表示自己）</param>
    /// <param name="mjMethod">麻将玩法类型</param>
    /// <param name="variation">变体索引</param>
    public static void PlayActionVoiceForPlayer(string actionName, long playerId, NetMsg.MJMethod mjMethod, int variation = 0)
    {
        VoiceLanguage? languageToUse = GetLanguageForMethod(mjMethod);
        string audioKey = GetActionVoiceKeyForPlayer(actionName, playerId, variation, languageToUse);
        if (!string.IsNullOrEmpty(audioKey))
        {
           Sound.PlayEffect(audioKey);
        }
    }

    /// <summary>
    /// 播放操作音效（指定语言版本）
    /// </summary>
    /// <param name="actionName">操作名称</param>
    /// <param name="playerId">玩家ID（0表示自己）</param>
    /// <param name="language">语言类型</param>
    /// <param name="variation">变体索引</param>
    public static void PlayActionVoiceForPlayerWithLanguage(string actionName, long playerId, VoiceLanguage language, int variation = 0)
    {
        string audioKey = GetActionVoiceKeyForPlayer(actionName, playerId, variation, language);
        if (!string.IsNullOrEmpty(audioKey))
        {
           Sound.PlayEffect(audioKey);
        }
    }
    
    #region 私有辅助方法

    /// <summary>
    /// 获取语言前缀
    /// /// </summary>
    private static string GetLanguagePrefix(VoiceLanguage language)
    {
        switch (language)
        {
            case VoiceLanguage.Mandarin:
                return "PT_SPEAK";  // 普通话
            case VoiceLanguage.XianTao:
                return "XT_SPEAK";  // 仙桃话
            case VoiceLanguage.Sichuan:
                return "SC_SPEAK";  // 四川话
            default:
                return "XT_SPEAK";
        }
    }

    /// <summary>
    /// 获取性别路径
    /// </summary>
    private static string GetGenderPath(PlayerType gender)
    {
        switch (gender)
        {
            case PlayerType.FEMALE:
                return "GIRL";
            case PlayerType.MALE:
                return "MAN";
            default:
                return "GIRL";
        }
    }

    /// <summary>
    /// 获取卡牌文件名
    /// </summary>
    private static string GetCardFileName(MahjongFaceValue faceValue)
    {
        switch (faceValue)
        {
            // 万
            case MahjongFaceValue.MJ_WANG_1: return "wan1";
            case MahjongFaceValue.MJ_WANG_2: return "wan2";
            case MahjongFaceValue.MJ_WANG_3: return "wan3";
            case MahjongFaceValue.MJ_WANG_4: return "wan4";
            case MahjongFaceValue.MJ_WANG_5: return "wan5";
            case MahjongFaceValue.MJ_WANG_6: return "wan6";
            case MahjongFaceValue.MJ_WANG_7: return "wan7";
            case MahjongFaceValue.MJ_WANG_8: return "wan8";
            case MahjongFaceValue.MJ_WANG_9: return "wan9";

            // 筒
            case MahjongFaceValue.MJ_TONG_1: return "tong1";
            case MahjongFaceValue.MJ_TONG_2: return "tong2";
            case MahjongFaceValue.MJ_TONG_3: return "tong3";
            case MahjongFaceValue.MJ_TONG_4: return "tong4";
            case MahjongFaceValue.MJ_TONG_5: return "tong5";
            case MahjongFaceValue.MJ_TONG_6: return "tong6";
            case MahjongFaceValue.MJ_TONG_7: return "tong7";
            case MahjongFaceValue.MJ_TONG_8: return "tong8";
            case MahjongFaceValue.MJ_TONG_9: return "tong9";

            // 条
            case MahjongFaceValue.MJ_TIAO_1: return "tiao1";
            case MahjongFaceValue.MJ_TIAO_2: return "tiao2";
            case MahjongFaceValue.MJ_TIAO_3: return "tiao3";
            case MahjongFaceValue.MJ_TIAO_4: return "tiao4";
            case MahjongFaceValue.MJ_TIAO_5: return "tiao5";
            case MahjongFaceValue.MJ_TIAO_6: return "tiao6";
            case MahjongFaceValue.MJ_TIAO_7: return "tiao7";
            case MahjongFaceValue.MJ_TIAO_8: return "tiao8";
            case MahjongFaceValue.MJ_TIAO_9: return "tiao9";

            // 风
            case MahjongFaceValue.MJ_FENG_DONG: return "dongfeng";
            case MahjongFaceValue.MJ_FENG_NAN: return "nanfeng";
            case MahjongFaceValue.MJ_FENG_XI: return "xifeng";
            case MahjongFaceValue.MJ_FENG_BEI: return "beifeng";

            // 中发白
            case MahjongFaceValue.MJ_ZFB_HONGZHONG: return "hongzhong";
            case MahjongFaceValue.MJ_ZFB_FACAI: return "facai";
            case MahjongFaceValue.MJ_ZFB_BAIBAN: return "baiban";

            default:
                return null;
        }
    }

    #endregion

    /// <summary>
    /// 直接为玩家播放胡牌类型语音（安全调用）
    /// 根据特效名称映射到对应的hupaiType音频键
    /// </summary>
    /// <param name="effectName">特效名称，如 "HuType_PengPengHu"</param>
    /// <param name="playerId">玩家ID（用于确定性别）</param>
    /// <param name="mjMethod">玩法类型（卡五星、血流成河强制普通话）</param>
    public static void PlayHuPaiTypeForPlayer(string effectName, long playerId, NetMsg.MJMethod? mjMethod = null)
    {
        if (string.IsNullOrEmpty(effectName))
            return;

        // 卡五星、血流成河强制使用普通话
        VoiceLanguage lang = (mjMethod.HasValue && IsForceMandarin(mjMethod.Value)) ? VoiceLanguage.Mandarin : MahjongSettings.GetLanguage();
        PlayerType gen = MahjongSettings.GetGenderForPlayer(playerId);

        string langPrefix = GetLanguagePrefix(lang);
        string genderPath = GetGenderPath(gen);

        // 从特效名转换为hupaiType音频键后缀
        // 例如: "HuType_PengPengHu" -> "PENGPENG"
        string audioSuffix = GetHuPaiTypeAudioSuffix(effectName);
        if (string.IsNullOrEmpty(audioSuffix))
        {
            GF.LogWarning($"[MahjongAudio] 未找到特效 {effectName} 对应的音频后缀");
            return;
        }

        // 构造AudioKeys键名: MJ_PT_SPEAK_GIRL_HUPAITYPE_PENGPENG
        string audioKeyName = $"MJ_{langPrefix}_{genderPath}_HUPAITYPE_{audioSuffix}".ToUpper();
        string audioPath = AudioKeys.Get(audioKeyName);
        
        if (!string.IsNullOrEmpty(audioPath))
        {
            Sound.PlayEffect(audioPath);
            GF.LogInfo($"[MahjongAudio] 播放牌型语音: {audioKeyName} -> {audioPath}");
        }
        else
        {
            GF.LogWarning($"[MahjongAudio] 未找到音频键: {audioKeyName}");
        }
    }

    /// <summary>
    /// 根据特效名称获取对应的hupaiType音频后缀
    /// AudioKeys格式: MJ_PT_SPEAK_GIRL_HUPAITYPE_PENGPENG
    /// </summary>
    private static string GetHuPaiTypeAudioSuffix(string effectName)
    {
        switch (effectName)
        {
            // 碰碰胡
            case "HuType_PengPengHu": return "PENGPENG";
            // 清一色
            case "HuType_QingYiSe": return "QINGYISE";
            // 七对
            case "HuType_QiDui": return "QIDUIHUAPAI";
            // 豪华七对
            case "HuType_HaoHuaQiDui": return "HAOHUAQIDUII";
            // 超豪华七对
            case "HuType_ChaoHaoHuaQiDui": return "CHAOHAOHUA";
            // 超超豪华七对
            case "HuType_ChaoChaoHaoHuaQiDui": return "CHAOCHAOHAOHUA";
            // 卡五星
            case "HuType_KaWuXing": return "KAWUXING";
            // 明四归
            case "HuType_MingSiGui": return "MINGSIGUI";
            // 暗四归
            case "HuType_AnSiGui": return "ANSIGUI";
            // 手抓一
            case "HuType_ShouZhuaYi": return "SHOUZHUAYI";
            // 小三元
            case "HuType_XiaoSanYuan": return "XIAOSANYUAN";
            // 大三元
            case "HuType_DaSanYuan": return "DASANYUAN";
            
            default:
                GF.LogInfo($"[MahjongAudio] 特效 {effectName} 暂无对应音频映射");
                return null;
        }
    }
}

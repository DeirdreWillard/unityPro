// 此文件由AudioMappingGenerator自动生成,请勿手动修改
// 生成时间: 2026-02-10 16:23:53

using System.Collections.Generic;

/// <summary>
/// 音效键常量定义
/// 使用方法:Sound.PlayEffect(AudioKeys.Get("KEY_NAME"));
/// </summary>
public static class AudioKeys
{
    private static readonly Dictionary<string, string> _audioMap = new Dictionary<string, string>
    {
        { "ANIZHUANPAN2AUDIO", "ANIzhuanpan2Audio.mp3" },
        { "BJ_三条", "BJ/三条.mp3" },
        { "BJ_乌龙", "BJ/乌龙.mp3" },
        { "BJ_同花", "BJ/同花.mp3" },
        { "BJ_同花顺", "BJ/同花顺.mp3" },
        { "BJ_对子", "BJ/对子.mp3" },
        { "BJ_开始比牌", "BJ/开始比牌.mp3" },
        { "BJ_电击音效", "BJ/电击音效.mp3" },
        { "BJ_请出牌", "BJ/请出牌.mp3" },
        { "BJ_赢牌音效", "BJ/赢牌音效.mp3" },
        { "BJ_输牌音效", "BJ/输牌音效.mp3" },
        { "BJ_通关", "BJ/通关.mp3" },
        { "BJ_金币散开", "BJ/金币散开.mp3" },
        { "BJ_金币汇聚", "BJ/金币汇聚.mp3" },
        { "BJ_顺子", "BJ/顺子.mp3" },
        { "BLESSING_BLESSING_UP", "blessing/blessing_up.mp3" },
        { "BLESSING_BURNINGINCENSE", "blessing/BurningIncense.mp3" },
        { "BLESSING_FENGHUANG", "blessing/fenghuang.mp3" },
        { "BLESSING_FLYINGDRAGON", "blessing/FlyingDragon.mp3" },
        { "BLESSING_INGOTSRAIN", "blessing/IngotsRain.mp3" },
        { "BLESSING_LUCKYTREE", "blessing/LuckyTree.mp3" },
        { "BLESSING_LUOPAN", "blessing/Luopan.mp3" },
        { "BLESSING_POFUCHENZHOU", "blessing/pofuchenzhou.mp3" },
        { "BLESSING_RUHUASHIJING", "blessing/ruhuashijing.mp3" },
        { "BLESSING_WASHINGHANDS", "blessing/washingHands.mp3" },
        { "CARD_TURNING", "card_turning.ogg" },
        { "CHIPMOVE", "chipmove.mp3" },
        { "COMMING", "Comming.mp3" },
        { "COUNTDOWN", "countdown.ogg" },
        { "DAIRU_WARNING", "dairu_warning.mp3" },
        { "DPCHIPMOVE", "DPChipMove.ogg" },
        { "FACE_2001", "face/2001.mp3" },
        { "FACE_2002", "face/2002.mp3" },
        { "FACE_2003", "face/2003.mp3" },
        { "FACE_2004", "face/2004.mp3" },
        { "FACE_2005", "face/2005.mp3" },
        { "FACE_2006", "face/2006.mp3" },
        { "FACE_2007", "face/2007.mp3" },
        { "FACE_2008", "face/2008.mp3" },
        { "FACE_2009", "face/2009.mp3" },
        { "FACE_2010", "face/2010.mp3" },
        { "FACE_2011", "face/2011.mp3" },
        { "FACE_2012", "face/2012.mp3" },
        { "INSURANCEFAILED", "insurancefailed.wav" },
        { "LIANGPAI", "LiangPai.mp3" },
        { "MJ_EFFECT_AUDIO_FAPAI", "MJ/EFFECT_AUDIO/fapai.mp3" },
        { "MJ_EFFECT_AUDIO_GAME_CLICK_CARD", "MJ/EFFECT_AUDIO/GAME_CLICK_CARD.mp3" },
        { "MJ_EFFECT_AUDIO_GAME_LOST", "MJ/EFFECT_AUDIO/GAME_LOST.mp3" },
        { "MJ_EFFECT_AUDIO_GAME_TIME_TICK", "MJ/EFFECT_AUDIO/GAME_TIME_TICK.mp3" },
        { "MJ_EFFECT_AUDIO_GAME_WIN", "MJ/EFFECT_AUDIO/GAME_WIN.mp3" },
        { "MJ_EFFECT_AUDIO_GAMESTART", "MJ/EFFECT_AUDIO/gameStart.mp3" },
        { "MJ_EFFECT_AUDIO_MJ_SCORE", "MJ/EFFECT_AUDIO/mj_score.mp3" },
        { "MJ_EFFECT_AUDIO_MOPAI", "MJ/EFFECT_AUDIO/mopai.mp3" },
        { "MJ_EFFECT_AUDIO_PENG", "MJ/EFFECT_AUDIO/peng.mp3" },
        { "MJ_EFFECT_AUDIO_PIAOLAIZI", "MJ/EFFECT_AUDIO/piaolaizi.mp3" },
        { "MJ_EFFECT_AUDIO_SEND_CARD0", "MJ/EFFECT_AUDIO/SEND_CARD0.mp3" },
        { "MJ_EFFECT_AUDIO_SOUND_DINGZHUANG", "MJ/EFFECT_AUDIO/sound_dingzhuang.mp3" },
        { "MJ_EFFECT_AUDIO_SOUND_ERROR", "MJ/EFFECT_AUDIO/sound_error.mp3" },
        { "MJ_EFFECT_AUDIO_SOUND_EVENT", "MJ/EFFECT_AUDIO/sound_event.mp3" },
        { "MJ_EFFECT_AUDIO_SOUND_MOVE", "MJ/EFFECT_AUDIO/sound_move.mp3" },
        { "MJ_EFFECT_AUDIO_ZHSZ", "MJ/EFFECT_AUDIO/zhsz.mp3" },
        { "MJ_EFFECT_AUDIO_ZHUOCHONG", "MJ/EFFECT_AUDIO/zhuochong.mp3" },
        { "MJ_MUSIC_AUDIO_PLAYINGINGAME", "MJ/MUSIC_AUDIO/playingInGame.mp3" },
        { "MJ_MUSIC_AUDIO_READY", "MJ/MUSIC_AUDIO/ready.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_BAIBAN", "MJ/PT_SPEAK/girl/cardSound/baiban.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_BEIFENG", "MJ/PT_SPEAK/girl/cardSound/beifeng.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_DONGFENG", "MJ/PT_SPEAK/girl/cardSound/dongfeng.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_FACAI", "MJ/PT_SPEAK/girl/cardSound/facai.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_HONGZHONG", "MJ/PT_SPEAK/girl/cardSound/hongzhong.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_NANFENG", "MJ/PT_SPEAK/girl/cardSound/nanfeng.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_TIAO1", "MJ/PT_SPEAK/girl/cardSound/tiao1.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_TIAO2", "MJ/PT_SPEAK/girl/cardSound/tiao2.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_TIAO3", "MJ/PT_SPEAK/girl/cardSound/tiao3.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_TIAO4", "MJ/PT_SPEAK/girl/cardSound/tiao4.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_TIAO5", "MJ/PT_SPEAK/girl/cardSound/tiao5.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_TIAO6", "MJ/PT_SPEAK/girl/cardSound/tiao6.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_TIAO7", "MJ/PT_SPEAK/girl/cardSound/tiao7.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_TIAO8", "MJ/PT_SPEAK/girl/cardSound/tiao8.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_TIAO9", "MJ/PT_SPEAK/girl/cardSound/tiao9.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_TONG1", "MJ/PT_SPEAK/girl/cardSound/tong1.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_TONG2", "MJ/PT_SPEAK/girl/cardSound/tong2.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_TONG3", "MJ/PT_SPEAK/girl/cardSound/tong3.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_TONG4", "MJ/PT_SPEAK/girl/cardSound/tong4.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_TONG5", "MJ/PT_SPEAK/girl/cardSound/tong5.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_TONG6", "MJ/PT_SPEAK/girl/cardSound/tong6.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_TONG7", "MJ/PT_SPEAK/girl/cardSound/tong7.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_TONG8", "MJ/PT_SPEAK/girl/cardSound/tong8.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_TONG9", "MJ/PT_SPEAK/girl/cardSound/tong9.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_WAN1", "MJ/PT_SPEAK/girl/cardSound/wan1.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_WAN2", "MJ/PT_SPEAK/girl/cardSound/wan2.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_WAN3", "MJ/PT_SPEAK/girl/cardSound/wan3.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_WAN4", "MJ/PT_SPEAK/girl/cardSound/wan4.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_WAN5", "MJ/PT_SPEAK/girl/cardSound/wan5.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_WAN6", "MJ/PT_SPEAK/girl/cardSound/wan6.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_WAN7", "MJ/PT_SPEAK/girl/cardSound/wan7.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_WAN8", "MJ/PT_SPEAK/girl/cardSound/wan8.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_WAN9", "MJ/PT_SPEAK/girl/cardSound/wan9.mp3" },
        { "MJ_PT_SPEAK_GIRL_CARDSOUND_XIFENG", "MJ/PT_SPEAK/girl/cardSound/xifeng.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_ANGANG_0", "MJ/PT_SPEAK/girl/controlSound/angang_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_CHAOGANG_0", "MJ/PT_SPEAK/girl/controlSound/chaogang_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_CHI_0", "MJ/PT_SPEAK/girl/controlSound/chi_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_CHI_1", "MJ/PT_SPEAK/girl/controlSound/chi_1.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_CHI_2", "MJ/PT_SPEAK/girl/controlSound/chi_2.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_FENGYISE_0", "MJ/PT_SPEAK/girl/controlSound/fengyise_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_GANG_0", "MJ/PT_SPEAK/girl/controlSound/gang_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_GANG_1", "MJ/PT_SPEAK/girl/controlSound/gang_1.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_GANGSHANGKAIHUA_0", "MJ/PT_SPEAK/girl/controlSound/gangshangkaihua_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_HAIDILAOYUE_0", "MJ/PT_SPEAK/girl/controlSound/haidilaoyue_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_HEIMO_0", "MJ/PT_SPEAK/girl/controlSound/heimo_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_HU_0", "MJ/PT_SPEAK/girl/controlSound/hu_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_HU_1", "MJ/PT_SPEAK/girl/controlSound/hu_1.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_JIANGYISE_0", "MJ/PT_SPEAK/girl/controlSound/jiangyise_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_LAIZIGANG_0", "MJ/PT_SPEAK/girl/controlSound/laizigang_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_LAIZIGANG_1", "MJ/PT_SPEAK/girl/controlSound/laizigang_1.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_LIANG_0", "MJ/PT_SPEAK/girl/controlSound/liang_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_PENG_0", "MJ/PT_SPEAK/girl/controlSound/peng_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_PENG_1", "MJ/PT_SPEAK/girl/controlSound/peng_1.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_PENG_2", "MJ/PT_SPEAK/girl/controlSound/peng_2.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_PENGPENGHU_0", "MJ/PT_SPEAK/girl/controlSound/pengpenghu_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_PIZIGANG_0", "MJ/PT_SPEAK/girl/controlSound/pizigang_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_QIANGGANGHU_0", "MJ/PT_SPEAK/girl/controlSound/qiangganghu_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_QIDUI_0", "MJ/PT_SPEAK/girl/controlSound/qidui_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_QINGYISE_0", "MJ/PT_SPEAK/girl/controlSound/qingyise_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_QUANQIUREN_0", "MJ/PT_SPEAK/girl/controlSound/quanqiuren_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_RECHONG_0", "MJ/PT_SPEAK/girl/controlSound/rechong_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_TIANHU_0", "MJ/PT_SPEAK/girl/controlSound/tianhu_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_XIAOCHAOTIAN_0", "MJ/PT_SPEAK/girl/controlSound/xiaochaotian_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_XUGANG_0", "MJ/PT_SPEAK/girl/controlSound/xugang_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_ZHUNBEI_0", "MJ/PT_SPEAK/girl/controlSound/zhunbei_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_CONTROLSOUND_ZIMO_0", "MJ/PT_SPEAK/girl/controlSound/zimo_0.mp3" },
        { "MJ_PT_SPEAK_GIRL_HUPAITYPE_ANSIGUI", "MJ/PT_SPEAK/girl/hupaiType/ansigui.mp3" },
        { "MJ_PT_SPEAK_GIRL_HUPAITYPE_CHAOCHAOHAOHUA", "MJ/PT_SPEAK/girl/hupaiType/chaochaohaohua.mp3" },
        { "MJ_PT_SPEAK_GIRL_HUPAITYPE_CHAOHAOHUA", "MJ/PT_SPEAK/girl/hupaiType/chaohaohua.mp3" },
        { "MJ_PT_SPEAK_GIRL_HUPAITYPE_DASANYUAN", "MJ/PT_SPEAK/girl/hupaiType/dasanyuan.mp3" },
        { "MJ_PT_SPEAK_GIRL_HUPAITYPE_HAOHUAQIDUII", "MJ/PT_SPEAK/girl/hupaiType/haohuaqiduiI.mp3" },
        { "MJ_PT_SPEAK_GIRL_HUPAITYPE_KAWUXING", "MJ/PT_SPEAK/girl/hupaiType/kawuxing.mp3" },
        { "MJ_PT_SPEAK_GIRL_HUPAITYPE_MINGSIGUI", "MJ/PT_SPEAK/girl/hupaiType/mingsigui.mp3" },
        { "MJ_PT_SPEAK_GIRL_HUPAITYPE_PENGPENG", "MJ/PT_SPEAK/girl/hupaiType/pengpeng.mp3" },
        { "MJ_PT_SPEAK_GIRL_HUPAITYPE_QIDUIHUAPAI", "MJ/PT_SPEAK/girl/hupaiType/qiduihuapai.mp3" },
        { "MJ_PT_SPEAK_GIRL_HUPAITYPE_QINGYISE", "MJ/PT_SPEAK/girl/hupaiType/qingyise.mp3" },
        { "MJ_PT_SPEAK_GIRL_HUPAITYPE_SHOUZHUAYI", "MJ/PT_SPEAK/girl/hupaiType/shouzhuayi.mp3" },
        { "MJ_PT_SPEAK_GIRL_HUPAITYPE_XIAOSANYUAN", "MJ/PT_SPEAK/girl/hupaiType/xiaosanyuan.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_BAIBAN", "MJ/PT_SPEAK/man/cardSound/baiban.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_BEIFENG", "MJ/PT_SPEAK/man/cardSound/beifeng.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_DONGFENG", "MJ/PT_SPEAK/man/cardSound/dongfeng.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_FACAI", "MJ/PT_SPEAK/man/cardSound/facai.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_HONGZHONG", "MJ/PT_SPEAK/man/cardSound/hongzhong.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_NANFENG", "MJ/PT_SPEAK/man/cardSound/nanfeng.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_TIAO1", "MJ/PT_SPEAK/man/cardSound/tiao1.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_TIAO2", "MJ/PT_SPEAK/man/cardSound/tiao2.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_TIAO3", "MJ/PT_SPEAK/man/cardSound/tiao3.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_TIAO4", "MJ/PT_SPEAK/man/cardSound/tiao4.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_TIAO5", "MJ/PT_SPEAK/man/cardSound/tiao5.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_TIAO6", "MJ/PT_SPEAK/man/cardSound/tiao6.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_TIAO7", "MJ/PT_SPEAK/man/cardSound/tiao7.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_TIAO8", "MJ/PT_SPEAK/man/cardSound/tiao8.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_TIAO9", "MJ/PT_SPEAK/man/cardSound/tiao9.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_TONG1", "MJ/PT_SPEAK/man/cardSound/tong1.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_TONG2", "MJ/PT_SPEAK/man/cardSound/tong2.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_TONG3", "MJ/PT_SPEAK/man/cardSound/tong3.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_TONG4", "MJ/PT_SPEAK/man/cardSound/tong4.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_TONG5", "MJ/PT_SPEAK/man/cardSound/tong5.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_TONG6", "MJ/PT_SPEAK/man/cardSound/tong6.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_TONG7", "MJ/PT_SPEAK/man/cardSound/tong7.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_TONG8", "MJ/PT_SPEAK/man/cardSound/tong8.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_TONG9", "MJ/PT_SPEAK/man/cardSound/tong9.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_WAN1", "MJ/PT_SPEAK/man/cardSound/wan1.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_WAN2", "MJ/PT_SPEAK/man/cardSound/wan2.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_WAN3", "MJ/PT_SPEAK/man/cardSound/wan3.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_WAN4", "MJ/PT_SPEAK/man/cardSound/wan4.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_WAN5", "MJ/PT_SPEAK/man/cardSound/wan5.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_WAN6", "MJ/PT_SPEAK/man/cardSound/wan6.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_WAN7", "MJ/PT_SPEAK/man/cardSound/wan7.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_WAN8", "MJ/PT_SPEAK/man/cardSound/wan8.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_WAN9", "MJ/PT_SPEAK/man/cardSound/wan9.mp3" },
        { "MJ_PT_SPEAK_MAN_CARDSOUND_XIFENG", "MJ/PT_SPEAK/man/cardSound/xifeng.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_ANGANG_0", "MJ/PT_SPEAK/man/controlSound/angang_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_CHAOGANG_0", "MJ/PT_SPEAK/man/controlSound/chaogang_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_CHI_0", "MJ/PT_SPEAK/man/controlSound/chi_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_CHI_1", "MJ/PT_SPEAK/man/controlSound/chi_1.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_CHI_2", "MJ/PT_SPEAK/man/controlSound/chi_2.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_FENGYISE_0", "MJ/PT_SPEAK/man/controlSound/fengyise_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_GANG_0", "MJ/PT_SPEAK/man/controlSound/gang_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_GANG_1", "MJ/PT_SPEAK/man/controlSound/gang_1.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_GANGSHANGKAIHUA_0", "MJ/PT_SPEAK/man/controlSound/gangshangkaihua_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_HAIDILAOYUE_0", "MJ/PT_SPEAK/man/controlSound/haidilaoyue_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_HEIMO_0", "MJ/PT_SPEAK/man/controlSound/heimo_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_HU_0", "MJ/PT_SPEAK/man/controlSound/hu_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_HU_1", "MJ/PT_SPEAK/man/controlSound/hu_1.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_JIANGYISE_0", "MJ/PT_SPEAK/man/controlSound/jiangyise_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_LAIZIGANG_0", "MJ/PT_SPEAK/man/controlSound/laizigang_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_LAIZIGANG_1", "MJ/PT_SPEAK/man/controlSound/laizigang_1.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_LIANG_0", "MJ/PT_SPEAK/man/controlSound/liang_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_PENG_0", "MJ/PT_SPEAK/man/controlSound/peng_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_PENG_1", "MJ/PT_SPEAK/man/controlSound/peng_1.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_PENG_2", "MJ/PT_SPEAK/man/controlSound/peng_2.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_PENGPENGHU_0", "MJ/PT_SPEAK/man/controlSound/pengpenghu_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_PIZIGANG_0", "MJ/PT_SPEAK/man/controlSound/pizigang_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_QIANGGANGHU_0", "MJ/PT_SPEAK/man/controlSound/qiangganghu_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_QIDUI_0", "MJ/PT_SPEAK/man/controlSound/qidui_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_QINGYISE_0", "MJ/PT_SPEAK/man/controlSound/qingyise_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_QUANQIUREN_0", "MJ/PT_SPEAK/man/controlSound/quanqiuren_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_RECHONG_0", "MJ/PT_SPEAK/man/controlSound/rechong_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_TIANHU_0", "MJ/PT_SPEAK/man/controlSound/tianhu_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_XIAOCHAOTIAN_0", "MJ/PT_SPEAK/man/controlSound/xiaochaotian_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_XUGANG_0", "MJ/PT_SPEAK/man/controlSound/xugang_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_ZHUNBEI_0", "MJ/PT_SPEAK/man/controlSound/zhunbei_0.mp3" },
        { "MJ_PT_SPEAK_MAN_CONTROLSOUND_ZIMO_0", "MJ/PT_SPEAK/man/controlSound/zimo_0.mp3" },
        { "MJ_PT_SPEAK_MAN_HUPAITYPE_ANSIGUI", "MJ/PT_SPEAK/man/hupaiType/ansigui.mp3" },
        { "MJ_PT_SPEAK_MAN_HUPAITYPE_CHAOCHAOHAOHUA", "MJ/PT_SPEAK/man/hupaiType/chaochaohaohua.mp3" },
        { "MJ_PT_SPEAK_MAN_HUPAITYPE_CHAOHAOHUA", "MJ/PT_SPEAK/man/hupaiType/chaohaohua.mp3" },
        { "MJ_PT_SPEAK_MAN_HUPAITYPE_DASANYUAN", "MJ/PT_SPEAK/man/hupaiType/dasanyuan.mp3" },
        { "MJ_PT_SPEAK_MAN_HUPAITYPE_HAOHUAQIDUII", "MJ/PT_SPEAK/man/hupaiType/haohuaqiduiI.mp3" },
        { "MJ_PT_SPEAK_MAN_HUPAITYPE_KAWUXING", "MJ/PT_SPEAK/man/hupaiType/kawuxing.mp3" },
        { "MJ_PT_SPEAK_MAN_HUPAITYPE_MINGSIGUI", "MJ/PT_SPEAK/man/hupaiType/mingsigui.mp3" },
        { "MJ_PT_SPEAK_MAN_HUPAITYPE_PENGPENG", "MJ/PT_SPEAK/man/hupaiType/pengpeng.mp3" },
        { "MJ_PT_SPEAK_MAN_HUPAITYPE_QIDUIHUAPAI", "MJ/PT_SPEAK/man/hupaiType/qiduihuapai.mp3" },
        { "MJ_PT_SPEAK_MAN_HUPAITYPE_QINGYISE", "MJ/PT_SPEAK/man/hupaiType/qingyise.mp3" },
        { "MJ_PT_SPEAK_MAN_HUPAITYPE_SHOUZHUAYI", "MJ/PT_SPEAK/man/hupaiType/shouzhuayi.mp3" },
        { "MJ_PT_SPEAK_MAN_HUPAITYPE_XIAOSANYUAN", "MJ/PT_SPEAK/man/hupaiType/xiaosanyuan.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_BAIBAN", "MJ/XT_SPEAK/girl/cardSound/baiban.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_BEIFENG", "MJ/XT_SPEAK/girl/cardSound/beifeng.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_DONGFENG", "MJ/XT_SPEAK/girl/cardSound/dongfeng.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_FACAI", "MJ/XT_SPEAK/girl/cardSound/facai.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_HONGZHONG", "MJ/XT_SPEAK/girl/cardSound/hongzhong.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_NANFENG", "MJ/XT_SPEAK/girl/cardSound/nanfeng.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_TIAO1", "MJ/XT_SPEAK/girl/cardSound/tiao1.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_TIAO2", "MJ/XT_SPEAK/girl/cardSound/tiao2.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_TIAO3", "MJ/XT_SPEAK/girl/cardSound/tiao3.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_TIAO4", "MJ/XT_SPEAK/girl/cardSound/tiao4.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_TIAO5", "MJ/XT_SPEAK/girl/cardSound/tiao5.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_TIAO6", "MJ/XT_SPEAK/girl/cardSound/tiao6.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_TIAO7", "MJ/XT_SPEAK/girl/cardSound/tiao7.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_TIAO8", "MJ/XT_SPEAK/girl/cardSound/tiao8.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_TIAO9", "MJ/XT_SPEAK/girl/cardSound/tiao9.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_TONG1", "MJ/XT_SPEAK/girl/cardSound/tong1.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_TONG2", "MJ/XT_SPEAK/girl/cardSound/tong2.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_TONG3", "MJ/XT_SPEAK/girl/cardSound/tong3.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_TONG4", "MJ/XT_SPEAK/girl/cardSound/tong4.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_TONG5", "MJ/XT_SPEAK/girl/cardSound/tong5.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_TONG6", "MJ/XT_SPEAK/girl/cardSound/tong6.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_TONG7", "MJ/XT_SPEAK/girl/cardSound/tong7.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_TONG8", "MJ/XT_SPEAK/girl/cardSound/tong8.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_TONG9", "MJ/XT_SPEAK/girl/cardSound/tong9.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_WAN1", "MJ/XT_SPEAK/girl/cardSound/wan1.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_WAN2", "MJ/XT_SPEAK/girl/cardSound/wan2.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_WAN3", "MJ/XT_SPEAK/girl/cardSound/wan3.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_WAN4", "MJ/XT_SPEAK/girl/cardSound/wan4.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_WAN5", "MJ/XT_SPEAK/girl/cardSound/wan5.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_WAN6", "MJ/XT_SPEAK/girl/cardSound/wan6.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_WAN7", "MJ/XT_SPEAK/girl/cardSound/wan7.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_WAN8", "MJ/XT_SPEAK/girl/cardSound/wan8.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_WAN9", "MJ/XT_SPEAK/girl/cardSound/wan9.mp3" },
        { "MJ_XT_SPEAK_GIRL_CARDSOUND_XIFENG", "MJ/XT_SPEAK/girl/cardSound/xifeng.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_ANGANG_0", "MJ/XT_SPEAK/girl/controlSound/angang_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_CHAOGANG_0", "MJ/XT_SPEAK/girl/controlSound/chaogang_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_CHI_0", "MJ/XT_SPEAK/girl/controlSound/chi_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_FENGYISE_0", "MJ/XT_SPEAK/girl/controlSound/fengyise_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_GANG_0", "MJ/XT_SPEAK/girl/controlSound/gang_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_GANG_1", "MJ/XT_SPEAK/girl/controlSound/gang_1.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_GANGSHANGKAIHUA_0", "MJ/XT_SPEAK/girl/controlSound/gangshangkaihua_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_HAIDILAOYUE_0", "MJ/XT_SPEAK/girl/controlSound/haidilaoyue_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_HEIMO_0", "MJ/XT_SPEAK/girl/controlSound/heimo_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_HU_0", "MJ/XT_SPEAK/girl/controlSound/hu_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_HU_1", "MJ/XT_SPEAK/girl/controlSound/hu_1.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_JIANGYISE_0", "MJ/XT_SPEAK/girl/controlSound/jiangyise_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_LAIZIGANG_0", "MJ/XT_SPEAK/girl/controlSound/laizigang_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_PENG_0", "MJ/XT_SPEAK/girl/controlSound/peng_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_QIANGGANGHU_0", "MJ/XT_SPEAK/girl/controlSound/qiangganghu_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_QIDUI_0", "MJ/XT_SPEAK/girl/controlSound/qidui_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_QINGYISE_0", "MJ/XT_SPEAK/girl/controlSound/qingyise_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_RECHONG_0", "MJ/XT_SPEAK/girl/controlSound/rechong_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_TIANHU_0", "MJ/XT_SPEAK/girl/controlSound/tianhu_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_XIAOCHAOTIAN_0", "MJ/XT_SPEAK/girl/controlSound/xiaochaotian_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_XUGANG_0", "MJ/XT_SPEAK/girl/controlSound/xugang_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_ZHUNBEI_0", "MJ/XT_SPEAK/girl/controlSound/zhunbei_0.mp3" },
        { "MJ_XT_SPEAK_GIRL_CONTROLSOUND_ZIMO_0", "MJ/XT_SPEAK/girl/controlSound/zimo_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_BAIBAN", "MJ/XT_SPEAK/man/cardSound/baiban.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_BEIFENG", "MJ/XT_SPEAK/man/cardSound/beifeng.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_DONGFENG", "MJ/XT_SPEAK/man/cardSound/dongfeng.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_FACAI", "MJ/XT_SPEAK/man/cardSound/facai.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_HONGZHONG", "MJ/XT_SPEAK/man/cardSound/hongzhong.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_NANFENG", "MJ/XT_SPEAK/man/cardSound/nanfeng.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_TIAO1", "MJ/XT_SPEAK/man/cardSound/tiao1.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_TIAO2", "MJ/XT_SPEAK/man/cardSound/tiao2.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_TIAO3", "MJ/XT_SPEAK/man/cardSound/tiao3.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_TIAO4", "MJ/XT_SPEAK/man/cardSound/tiao4.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_TIAO5", "MJ/XT_SPEAK/man/cardSound/tiao5.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_TIAO6", "MJ/XT_SPEAK/man/cardSound/tiao6.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_TIAO7", "MJ/XT_SPEAK/man/cardSound/tiao7.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_TIAO8", "MJ/XT_SPEAK/man/cardSound/tiao8.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_TIAO9", "MJ/XT_SPEAK/man/cardSound/tiao9.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_TONG1", "MJ/XT_SPEAK/man/cardSound/tong1.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_TONG2", "MJ/XT_SPEAK/man/cardSound/tong2.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_TONG3", "MJ/XT_SPEAK/man/cardSound/tong3.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_TONG4", "MJ/XT_SPEAK/man/cardSound/tong4.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_TONG5", "MJ/XT_SPEAK/man/cardSound/tong5.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_TONG6", "MJ/XT_SPEAK/man/cardSound/tong6.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_TONG7", "MJ/XT_SPEAK/man/cardSound/tong7.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_TONG8", "MJ/XT_SPEAK/man/cardSound/tong8.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_TONG9", "MJ/XT_SPEAK/man/cardSound/tong9.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_WAN1", "MJ/XT_SPEAK/man/cardSound/wan1.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_WAN2", "MJ/XT_SPEAK/man/cardSound/wan2.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_WAN3", "MJ/XT_SPEAK/man/cardSound/wan3.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_WAN4", "MJ/XT_SPEAK/man/cardSound/wan4.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_WAN5", "MJ/XT_SPEAK/man/cardSound/wan5.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_WAN6", "MJ/XT_SPEAK/man/cardSound/wan6.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_WAN7", "MJ/XT_SPEAK/man/cardSound/wan7.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_WAN8", "MJ/XT_SPEAK/man/cardSound/wan8.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_WAN9", "MJ/XT_SPEAK/man/cardSound/wan9.mp3" },
        { "MJ_XT_SPEAK_MAN_CARDSOUND_XIFENG", "MJ/XT_SPEAK/man/cardSound/xifeng.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_ANGANG_0", "MJ/XT_SPEAK/man/controlSound/angang_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_CHAOGANG_0", "MJ/XT_SPEAK/man/controlSound/chaogang_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_CHI_0", "MJ/XT_SPEAK/man/controlSound/chi_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_FENGYISE_0", "MJ/XT_SPEAK/man/controlSound/fengyise_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_GANG_0", "MJ/XT_SPEAK/man/controlSound/gang_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_GANG_1", "MJ/XT_SPEAK/man/controlSound/gang_1.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_GANGSHANGKAIHUA_0", "MJ/XT_SPEAK/man/controlSound/gangshangkaihua_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_HAIDILAOYUE_0", "MJ/XT_SPEAK/man/controlSound/haidilaoyue_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_HEIMO_0", "MJ/XT_SPEAK/man/controlSound/heimo_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_HU_0", "MJ/XT_SPEAK/man/controlSound/hu_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_HU_1", "MJ/XT_SPEAK/man/controlSound/hu_1.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_JIANGYISE_0", "MJ/XT_SPEAK/man/controlSound/jiangyise_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_LAIZIGANG_0", "MJ/XT_SPEAK/man/controlSound/laizigang_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_PENG_0", "MJ/XT_SPEAK/man/controlSound/peng_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_QIANGGANGHU_0", "MJ/XT_SPEAK/man/controlSound/qiangganghu_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_QIDUI_0", "MJ/XT_SPEAK/man/controlSound/qidui_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_QINGYISE_0", "MJ/XT_SPEAK/man/controlSound/qingyise_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_RECHONG_0", "MJ/XT_SPEAK/man/controlSound/rechong_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_TIANHU_0", "MJ/XT_SPEAK/man/controlSound/tianhu_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_XIAOCHAOTIAN_0", "MJ/XT_SPEAK/man/controlSound/xiaochaotian_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_XUGANG_0", "MJ/XT_SPEAK/man/controlSound/xugang_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_ZHUNBEI_0", "MJ/XT_SPEAK/man/controlSound/zhunbei_0.mp3" },
        { "MJ_XT_SPEAK_MAN_CONTROLSOUND_ZIMO_0", "MJ/XT_SPEAK/man/controlSound/zimo_0.mp3" },
        { "NIUNIU_ACTION", "niuniu/action.wav" },
        { "NIUNIU_CARD", "niuniu/card.mp3" },
        { "NIUNIU_JUMPBANKER", "niuniu/jumpbanker.mp3" },
        { "NIUNIU_NIU0", "niuniu/Niu0.mp3" },
        { "NIUNIU_NIU1", "niuniu/Niu1.mp3" },
        { "NIUNIU_NIU10", "niuniu/Niu10.mp3" },
        { "NIUNIU_NIU11", "niuniu/Niu11.mp3" },
        { "NIUNIU_NIU12", "niuniu/Niu12.mp3" },
        { "NIUNIU_NIU13", "niuniu/Niu13.mp3" },
        { "NIUNIU_NIU14", "niuniu/Niu14.mp3" },
        { "NIUNIU_NIU15", "niuniu/Niu15.mp3" },
        { "NIUNIU_NIU16", "niuniu/Niu16.mp3" },
        { "NIUNIU_NIU17", "niuniu/Niu17.mp3" },
        { "NIUNIU_NIU2", "niuniu/Niu2.mp3" },
        { "NIUNIU_NIU3", "niuniu/Niu3.mp3" },
        { "NIUNIU_NIU4", "niuniu/Niu4.mp3" },
        { "NIUNIU_NIU5", "niuniu/Niu5.mp3" },
        { "NIUNIU_NIU6", "niuniu/Niu6.mp3" },
        { "NIUNIU_NIU7", "niuniu/Niu7.mp3" },
        { "NIUNIU_NIU8", "niuniu/Niu8.mp3" },
        { "NIUNIU_NIU9", "niuniu/Niu9.mp3" },
        { "NIUNIU_NIUNIUTEXIAOYIN", "niuniu/NiuNiuTeXiaoYin.mp3" },
        { "NIUNIU_RANDOMBANKER", "niuniu/randombanker.mp3" },
        { "NIUNIU_RATE0", "niuniu/rate0.mp3" },
        { "NIUNIU_RATE1", "niuniu/rate1.mp3" },
        { "NIUNIU_RATE2", "niuniu/rate2.mp3" },
        { "NIUNIU_RATE3", "niuniu/rate3.mp3" },
        { "NIUNIU_TESHUNIUTEXIAOYIN", "niuniu/TeShuNiuTeXiaoYin.mp3" },
        { "NIUNIU_WIN", "niuniu/win.mp3" },
        { "OPENREWARD", "OpenReward.mp3" },
        { "PCHECK", "pcheck.ogg" },
        { "PDK_ALARMCLOCK", "pdk/alarmclock.mp3" },
        { "PDK_BAOZI", "pdk/baozi.mp3" },
        { "PDK_BOMB1", "pdk/bomb1.mp3" },
        { "PDK_BOOM", "pdk/boom.mp3" },
        { "PDK_BUTTON", "pdk/Button.mp3" },
        { "PDK_CHICK", "pdk/chick.mp3" },
        { "PDK_CHOSECARD", "pdk/chosecard.mp3" },
        { "PDK_CHUNTIAN", "pdk/chuntian.mp3" },
        { "PDK_CHUPAI", "pdk/chupai.mp3" },
        { "PDK_DAO", "pdk/dao.mp3" },
        { "PDK_FAPAI", "pdk/fapai.mp3" },
        { "PDK_HEMA", "pdk/hema.mp3" },
        { "PDK_JIEFENG", "pdk/jiefeng.mp3" },
        { "PDK_LIANDUI", "pdk/liandui.mp3" },
        { "PDK_LIANZHA", "pdk/lianzha.mp3" },
        { "PDK_LOST", "pdk/Lost.mp3" },
        { "PDK_PLANE", "pdk/plane.mp3" },
        { "PDK_PT_MAN_CARD_10", "pdk/pt/man/card/10.mp3" },
        { "PDK_PT_MAN_CARD_1010", "pdk/pt/man/card/1010.mp3" },
        { "PDK_PT_MAN_CARD_2", "pdk/pt/man/card/2.mp3" },
        { "PDK_PT_MAN_CARD_22", "pdk/pt/man/card/22.mp3" },
        { "PDK_PT_MAN_CARD_3", "pdk/pt/man/card/3.mp3" },
        { "PDK_PT_MAN_CARD_33", "pdk/pt/man/card/33.mp3" },
        { "PDK_PT_MAN_CARD_4", "pdk/pt/man/card/4.mp3" },
        { "PDK_PT_MAN_CARD_44", "pdk/pt/man/card/44.mp3" },
        { "PDK_PT_MAN_CARD_5", "pdk/pt/man/card/5.mp3" },
        { "PDK_PT_MAN_CARD_55", "pdk/pt/man/card/55.mp3" },
        { "PDK_PT_MAN_CARD_6", "pdk/pt/man/card/6.mp3" },
        { "PDK_PT_MAN_CARD_66", "pdk/pt/man/card/66.mp3" },
        { "PDK_PT_MAN_CARD_7", "pdk/pt/man/card/7.mp3" },
        { "PDK_PT_MAN_CARD_77", "pdk/pt/man/card/77.mp3" },
        { "PDK_PT_MAN_CARD_8", "pdk/pt/man/card/8.mp3" },
        { "PDK_PT_MAN_CARD_88", "pdk/pt/man/card/88.mp3" },
        { "PDK_PT_MAN_CARD_9", "pdk/pt/man/card/9.mp3" },
        { "PDK_PT_MAN_CARD_99", "pdk/pt/man/card/99.mp3" },
        { "PDK_PT_MAN_CARD_A", "pdk/pt/man/card/A.mp3" },
        { "PDK_PT_MAN_CARD_AA", "pdk/pt/man/card/AA.mp3" },
        { "PDK_PT_MAN_CARD_FEIJI", "pdk/pt/man/card/feiji.mp3" },
        { "PDK_PT_MAN_CARD_J", "pdk/pt/man/card/J.mp3" },
        { "PDK_PT_MAN_CARD_JJ", "pdk/pt/man/card/JJ.mp3" },
        { "PDK_PT_MAN_CARD_K", "pdk/pt/man/card/K.mp3" },
        { "PDK_PT_MAN_CARD_KK", "pdk/pt/man/card/KK.mp3" },
        { "PDK_PT_MAN_CARD_LIANDUI", "pdk/pt/man/card/liandui.mp3" },
        { "PDK_PT_MAN_CARD_Q", "pdk/pt/man/card/Q.mp3" },
        { "PDK_PT_MAN_CARD_QQ", "pdk/pt/man/card/QQ.mp3" },
        { "PDK_PT_MAN_CARD_SANDAIER", "pdk/pt/man/card/sandaier.mp3" },
        { "PDK_PT_MAN_CARD_SANDAIYI", "pdk/pt/man/card/sandaiyi.mp3" },
        { "PDK_PT_MAN_CARD_SANZHANG", "pdk/pt/man/card/sanzhang.mp3" },
        { "PDK_PT_MAN_CARD_SHUNZI", "pdk/pt/man/card/shunzi.mp3" },
        { "PDK_PT_MAN_CARD_ZHADAN", "pdk/pt/man/card/zhadan.mp3" },
        { "PDK_PT_MAN_CONTROL_BAOJING", "pdk/pt/man/control/baojing.mp3" },
        { "PDK_PT_MAN_CONTROL_BUYAO", "pdk/pt/man/control/buyao.mp3" },
        { "PDK_PT_MAN_CONTROL_DANI", "pdk/pt/man/control/dani.mp3" },
        { "PDK_PT_MAN_CONTROL_WIN", "pdk/pt/man/control/win.mp3" },
        { "PDK_PT_MAN_QIAOPIHUA_01", "pdk/pt/man/qiaopihua/01.mp3" },
        { "PDK_PT_MAN_QIAOPIHUA_02", "pdk/pt/man/qiaopihua/02.mp3" },
        { "PDK_PT_MAN_QIAOPIHUA_03", "pdk/pt/man/qiaopihua/03.mp3" },
        { "PDK_PT_MAN_QIAOPIHUA_04", "pdk/pt/man/qiaopihua/04.mp3" },
        { "PDK_PT_MAN_QIAOPIHUA_05", "pdk/pt/man/qiaopihua/05.mp3" },
        { "PDK_PT_MAN_QIAOPIHUA_06", "pdk/pt/man/qiaopihua/06.mp3" },
        { "PDK_PT_MAN_QIAOPIHUA_07", "pdk/pt/man/qiaopihua/07.mp3" },
        { "PDK_PT_MAN_QIAOPIHUA_08", "pdk/pt/man/qiaopihua/08.mp3" },
        { "PDK_PT_WOMAN_CARD_10", "pdk/pt/woman/card/10.mp3" },
        { "PDK_PT_WOMAN_CARD_1010", "pdk/pt/woman/card/1010.mp3" },
        { "PDK_PT_WOMAN_CARD_2", "pdk/pt/woman/card/2.mp3" },
        { "PDK_PT_WOMAN_CARD_22", "pdk/pt/woman/card/22.mp3" },
        { "PDK_PT_WOMAN_CARD_3", "pdk/pt/woman/card/3.mp3" },
        { "PDK_PT_WOMAN_CARD_33", "pdk/pt/woman/card/33.mp3" },
        { "PDK_PT_WOMAN_CARD_4", "pdk/pt/woman/card/4.mp3" },
        { "PDK_PT_WOMAN_CARD_44", "pdk/pt/woman/card/44.mp3" },
        { "PDK_PT_WOMAN_CARD_5", "pdk/pt/woman/card/5.mp3" },
        { "PDK_PT_WOMAN_CARD_55", "pdk/pt/woman/card/55.mp3" },
        { "PDK_PT_WOMAN_CARD_6", "pdk/pt/woman/card/6.mp3" },
        { "PDK_PT_WOMAN_CARD_66", "pdk/pt/woman/card/66.mp3" },
        { "PDK_PT_WOMAN_CARD_7", "pdk/pt/woman/card/7.mp3" },
        { "PDK_PT_WOMAN_CARD_77", "pdk/pt/woman/card/77.mp3" },
        { "PDK_PT_WOMAN_CARD_8", "pdk/pt/woman/card/8.mp3" },
        { "PDK_PT_WOMAN_CARD_88", "pdk/pt/woman/card/88.mp3" },
        { "PDK_PT_WOMAN_CARD_9", "pdk/pt/woman/card/9.mp3" },
        { "PDK_PT_WOMAN_CARD_99", "pdk/pt/woman/card/99.mp3" },
        { "PDK_PT_WOMAN_CARD_A", "pdk/pt/woman/card/A.mp3" },
        { "PDK_PT_WOMAN_CARD_AA", "pdk/pt/woman/card/AA.mp3" },
        { "PDK_PT_WOMAN_CARD_FEIJI", "pdk/pt/woman/card/feiji.mp3" },
        { "PDK_PT_WOMAN_CARD_J", "pdk/pt/woman/card/J.mp3" },
        { "PDK_PT_WOMAN_CARD_JJ", "pdk/pt/woman/card/JJ.mp3" },
        { "PDK_PT_WOMAN_CARD_K", "pdk/pt/woman/card/K.mp3" },
        { "PDK_PT_WOMAN_CARD_KK", "pdk/pt/woman/card/KK.mp3" },
        { "PDK_PT_WOMAN_CARD_LIANDUI", "pdk/pt/woman/card/liandui.mp3" },
        { "PDK_PT_WOMAN_CARD_Q", "pdk/pt/woman/card/Q.mp3" },
        { "PDK_PT_WOMAN_CARD_QQ", "pdk/pt/woman/card/QQ.mp3" },
        { "PDK_PT_WOMAN_CARD_SANDAIER", "pdk/pt/woman/card/sandaier.mp3" },
        { "PDK_PT_WOMAN_CARD_SANDAIEYI", "pdk/pt/woman/card/sandaieyi.mp3" },
        { "PDK_PT_WOMAN_CARD_SANZHANG", "pdk/pt/woman/card/sanzhang.mp3" },
        { "PDK_PT_WOMAN_CARD_SHUNZI", "pdk/pt/woman/card/shunzi.mp3" },
        { "PDK_PT_WOMAN_CARD_ZHADAN", "pdk/pt/woman/card/zhadan.mp3" },
        { "PDK_PT_WOMAN_CONTROL_BAOJING", "pdk/pt/woman/control/baojing.mp3" },
        { "PDK_PT_WOMAN_CONTROL_BUYAO", "pdk/pt/woman/control/buyao.mp3" },
        { "PDK_PT_WOMAN_CONTROL_DANI", "pdk/pt/woman/control/dani.mp3" },
        { "PDK_PT_WOMAN_CONTROL_WIN", "pdk/pt/woman/control/win.mp3" },
        { "PDK_PT_WOMAN_QIAOPIHUA_01", "pdk/pt/woman/qiaopihua/01.mp3" },
        { "PDK_PT_WOMAN_QIAOPIHUA_02", "pdk/pt/woman/qiaopihua/02.mp3" },
        { "PDK_PT_WOMAN_QIAOPIHUA_03", "pdk/pt/woman/qiaopihua/03.mp3" },
        { "PDK_PT_WOMAN_QIAOPIHUA_04", "pdk/pt/woman/qiaopihua/04.mp3" },
        { "PDK_PT_WOMAN_QIAOPIHUA_05", "pdk/pt/woman/qiaopihua/05.mp3" },
        { "PDK_PT_WOMAN_QIAOPIHUA_06", "pdk/pt/woman/qiaopihua/06.mp3" },
        { "PDK_PT_WOMAN_QIAOPIHUA_07", "pdk/pt/woman/qiaopihua/07.mp3" },
        { "PDK_PT_WOMAN_QIAOPIHUA_08", "pdk/pt/woman/qiaopihua/08.mp3" },
        { "PDK_READY", "pdk/ready.mp3" },
        { "PDK_ROCKET", "pdk/rocket.mp3" },
        { "PDK_SEND_CARD0", "pdk/SEND_CARD0.mp3" },
        { "PDK_SHUNZI", "pdk/shunzi.mp3" },
        { "PDK_SO", "pdk/so.mp3" },
        { "PDK_WARNING", "pdk/Warning.mp3" },
        { "PDK_WIN", "pdk/Win.mp3" },
        { "PDK_YX_FEIJI", "pdk/yx_feiji.mp3" },
        { "PLAYERIN", "playerin.mp3" },
        { "PLAYEROUT", "playerout.mp3" },
        { "QIPAI", "qipai.mp3" },
        { "SITDOWN", "sitdown.mp3" },
        { "SLIDER", "slider.ogg" },
        { "SLIDER_TOP", "slider_top.ogg" },
        { "SOUND_BTN", "sound_btn.mp3" },
        { "STANDUP", "standup.mp3" },
        { "WARNING", "warning.ogg" },
        { "XIAZHU", "xiazhu.mp3" },
        { "ZJH_OPENCARD", "zjh/opencard.wav" },
        { "ZJH_OPENCARDOTHER", "zjh/opencardOther.mp3" },
        { "ZJH_PK8_ARROW_8KP", "zjh/pk8_arrow_8kp.mp3" },
        { "ZJH_PK8_JSS_BAOZI_8KP", "zjh/pk8_jss_baozi_8kp.mp3" },
        { "ZJH_PK8_JSS_COMPARE_8KP", "zjh/pk8_jss_compare_8kp.mp3" },
        { "ZJH_PK8_JSS_JINHUA_8KP", "zjh/pk8_jss_jinhua_8kp.mp3" },
        { "ZJH_PK8_JSS_SHUNZI_8KP", "zjh/pk8_jss_shunzi_8kp.mp3" },
        { "ZJH_PK8_JSS_TONGHUASHUN_8KP", "zjh/pk8_jss_tonghuashun_8kp.mp3" },
        { "ZJH_PK8_MYTURN_8KP", "zjh/pk8_myturn_8kp.mp3" },
        { "ZJH_PK8_PAIPAI_8KP", "zjh/pk8_paipai_8kp.mp3" },
        { "ZJH_PK8_SHIELD_8KP", "zjh/pk8_shield_8kp.mp3" },
        { "ZJH_PK8_TIMEOUT_QUICK_8KP", "zjh/pk8_timeout_quick_8kp.mp3" },
        { "ZJH_PK8_TIMEOUT_SLOW_8KP", "zjh/pk8_timeout_slow_8kp.mp3" },
        { "ZJH_PK8_WIN_8KP", "zjh/pk8_win_8kp.mp3" }
    };

    /// <summary>
    /// 通过键名获取音频路径
    /// </summary>
    public static string Get(string key)
    {
        if (_audioMap.TryGetValue(key, out string path))
            return path;
        return null;
    }


    #region 根目录

    /// <summary>ANIzhuanpan2Audio.mp3</summary>
    public const string ANIZHUANPAN2AUDIO = "ANIzhuanpan2Audio.mp3";
    /// <summary>card_turning.ogg</summary>
    public const string CARD_TURNING = "card_turning.ogg";
    /// <summary>chipmove.mp3</summary>
    public const string CHIPMOVE = "chipmove.mp3";
    /// <summary>Comming.mp3</summary>
    public const string COMMING = "Comming.mp3";
    /// <summary>countdown.ogg</summary>
    public const string COUNTDOWN = "countdown.ogg";
    /// <summary>dairu_warning.mp3</summary>
    public const string DAIRU_WARNING = "dairu_warning.mp3";
    /// <summary>DPChipMove.ogg</summary>
    public const string DPCHIPMOVE = "DPChipMove.ogg";
    /// <summary>insurancefailed.wav</summary>
    public const string INSURANCEFAILED = "insurancefailed.wav";
    /// <summary>LiangPai.mp3</summary>
    public const string LIANGPAI = "LiangPai.mp3";
    /// <summary>OpenReward.mp3</summary>
    public const string OPENREWARD = "OpenReward.mp3";
    /// <summary>pcheck.ogg</summary>
    public const string PCHECK = "pcheck.ogg";
    /// <summary>playerin.mp3</summary>
    public const string PLAYERIN = "playerin.mp3";
    /// <summary>playerout.mp3</summary>
    public const string PLAYEROUT = "playerout.mp3";
    /// <summary>qipai.mp3</summary>
    public const string QIPAI = "qipai.mp3";
    /// <summary>sitdown.mp3</summary>
    public const string SITDOWN = "sitdown.mp3";
    /// <summary>slider.ogg</summary>
    public const string SLIDER = "slider.ogg";
    /// <summary>slider_top.ogg</summary>
    public const string SLIDER_TOP = "slider_top.ogg";
    /// <summary>sound_btn.mp3</summary>
    public const string SOUND_BTN = "sound_btn.mp3";
    /// <summary>standup.mp3</summary>
    public const string STANDUP = "standup.mp3";
    /// <summary>warning.ogg</summary>
    public const string WARNING = "warning.ogg";
    /// <summary>xiazhu.mp3</summary>
    public const string XIAZHU = "xiazhu.mp3";

    #endregion

    #region BJ

    /// <summary>BJ/三条.mp3</summary>
    public const string BJ_三条 = "BJ/三条.mp3";
    /// <summary>BJ/乌龙.mp3</summary>
    public const string BJ_乌龙 = "BJ/乌龙.mp3";
    /// <summary>BJ/同花.mp3</summary>
    public const string BJ_同花 = "BJ/同花.mp3";
    /// <summary>BJ/同花顺.mp3</summary>
    public const string BJ_同花顺 = "BJ/同花顺.mp3";
    /// <summary>BJ/对子.mp3</summary>
    public const string BJ_对子 = "BJ/对子.mp3";
    /// <summary>BJ/开始比牌.mp3</summary>
    public const string BJ_开始比牌 = "BJ/开始比牌.mp3";
    /// <summary>BJ/电击音效.mp3</summary>
    public const string BJ_电击音效 = "BJ/电击音效.mp3";
    /// <summary>BJ/请出牌.mp3</summary>
    public const string BJ_请出牌 = "BJ/请出牌.mp3";
    /// <summary>BJ/赢牌音效.mp3</summary>
    public const string BJ_赢牌音效 = "BJ/赢牌音效.mp3";
    /// <summary>BJ/输牌音效.mp3</summary>
    public const string BJ_输牌音效 = "BJ/输牌音效.mp3";
    /// <summary>BJ/通关.mp3</summary>
    public const string BJ_通关 = "BJ/通关.mp3";
    /// <summary>BJ/金币散开.mp3</summary>
    public const string BJ_金币散开 = "BJ/金币散开.mp3";
    /// <summary>BJ/金币汇聚.mp3</summary>
    public const string BJ_金币汇聚 = "BJ/金币汇聚.mp3";
    /// <summary>BJ/顺子.mp3</summary>
    public const string BJ_顺子 = "BJ/顺子.mp3";

    #endregion

    #region blessing

    /// <summary>blessing/blessing_up.mp3</summary>
    public const string BLESSING_BLESSING_UP = "blessing/blessing_up.mp3";
    /// <summary>blessing/BurningIncense.mp3</summary>
    public const string BLESSING_BURNINGINCENSE = "blessing/BurningIncense.mp3";
    /// <summary>blessing/fenghuang.mp3</summary>
    public const string BLESSING_FENGHUANG = "blessing/fenghuang.mp3";
    /// <summary>blessing/FlyingDragon.mp3</summary>
    public const string BLESSING_FLYINGDRAGON = "blessing/FlyingDragon.mp3";
    /// <summary>blessing/IngotsRain.mp3</summary>
    public const string BLESSING_INGOTSRAIN = "blessing/IngotsRain.mp3";
    /// <summary>blessing/LuckyTree.mp3</summary>
    public const string BLESSING_LUCKYTREE = "blessing/LuckyTree.mp3";
    /// <summary>blessing/Luopan.mp3</summary>
    public const string BLESSING_LUOPAN = "blessing/Luopan.mp3";
    /// <summary>blessing/pofuchenzhou.mp3</summary>
    public const string BLESSING_POFUCHENZHOU = "blessing/pofuchenzhou.mp3";
    /// <summary>blessing/ruhuashijing.mp3</summary>
    public const string BLESSING_RUHUASHIJING = "blessing/ruhuashijing.mp3";
    /// <summary>blessing/washingHands.mp3</summary>
    public const string BLESSING_WASHINGHANDS = "blessing/washingHands.mp3";

    #endregion

    #region face

    /// <summary>face/2001.mp3</summary>
    public const string FACE_2001 = "face/2001.mp3";
    /// <summary>face/2002.mp3</summary>
    public const string FACE_2002 = "face/2002.mp3";
    /// <summary>face/2003.mp3</summary>
    public const string FACE_2003 = "face/2003.mp3";
    /// <summary>face/2004.mp3</summary>
    public const string FACE_2004 = "face/2004.mp3";
    /// <summary>face/2005.mp3</summary>
    public const string FACE_2005 = "face/2005.mp3";
    /// <summary>face/2006.mp3</summary>
    public const string FACE_2006 = "face/2006.mp3";
    /// <summary>face/2007.mp3</summary>
    public const string FACE_2007 = "face/2007.mp3";
    /// <summary>face/2008.mp3</summary>
    public const string FACE_2008 = "face/2008.mp3";
    /// <summary>face/2009.mp3</summary>
    public const string FACE_2009 = "face/2009.mp3";
    /// <summary>face/2010.mp3</summary>
    public const string FACE_2010 = "face/2010.mp3";
    /// <summary>face/2011.mp3</summary>
    public const string FACE_2011 = "face/2011.mp3";
    /// <summary>face/2012.mp3</summary>
    public const string FACE_2012 = "face/2012.mp3";

    #endregion

    #region MJ/EFFECT_AUDIO

    /// <summary>MJ/EFFECT_AUDIO/fapai.mp3</summary>
    public const string MJ_EFFECT_AUDIO_FAPAI = "MJ/EFFECT_AUDIO/fapai.mp3";
    /// <summary>MJ/EFFECT_AUDIO/GAME_CLICK_CARD.mp3</summary>
    public const string MJ_EFFECT_AUDIO_GAME_CLICK_CARD = "MJ/EFFECT_AUDIO/GAME_CLICK_CARD.mp3";
    /// <summary>MJ/EFFECT_AUDIO/GAME_LOST.mp3</summary>
    public const string MJ_EFFECT_AUDIO_GAME_LOST = "MJ/EFFECT_AUDIO/GAME_LOST.mp3";
    /// <summary>MJ/EFFECT_AUDIO/GAME_TIME_TICK.mp3</summary>
    public const string MJ_EFFECT_AUDIO_GAME_TIME_TICK = "MJ/EFFECT_AUDIO/GAME_TIME_TICK.mp3";
    /// <summary>MJ/EFFECT_AUDIO/GAME_WIN.mp3</summary>
    public const string MJ_EFFECT_AUDIO_GAME_WIN = "MJ/EFFECT_AUDIO/GAME_WIN.mp3";
    /// <summary>MJ/EFFECT_AUDIO/gameStart.mp3</summary>
    public const string MJ_EFFECT_AUDIO_GAMESTART = "MJ/EFFECT_AUDIO/gameStart.mp3";
    /// <summary>MJ/EFFECT_AUDIO/mj_score.mp3</summary>
    public const string MJ_EFFECT_AUDIO_MJ_SCORE = "MJ/EFFECT_AUDIO/mj_score.mp3";
    /// <summary>MJ/EFFECT_AUDIO/mopai.mp3</summary>
    public const string MJ_EFFECT_AUDIO_MOPAI = "MJ/EFFECT_AUDIO/mopai.mp3";
    /// <summary>MJ/EFFECT_AUDIO/peng.mp3</summary>
    public const string MJ_EFFECT_AUDIO_PENG = "MJ/EFFECT_AUDIO/peng.mp3";
    /// <summary>MJ/EFFECT_AUDIO/piaolaizi.mp3</summary>
    public const string MJ_EFFECT_AUDIO_PIAOLAIZI = "MJ/EFFECT_AUDIO/piaolaizi.mp3";
    /// <summary>MJ/EFFECT_AUDIO/SEND_CARD0.mp3</summary>
    public const string MJ_EFFECT_AUDIO_SEND_CARD0 = "MJ/EFFECT_AUDIO/SEND_CARD0.mp3";
    /// <summary>MJ/EFFECT_AUDIO/sound_dingzhuang.mp3</summary>
    public const string MJ_EFFECT_AUDIO_SOUND_DINGZHUANG = "MJ/EFFECT_AUDIO/sound_dingzhuang.mp3";
    /// <summary>MJ/EFFECT_AUDIO/sound_error.mp3</summary>
    public const string MJ_EFFECT_AUDIO_SOUND_ERROR = "MJ/EFFECT_AUDIO/sound_error.mp3";
    /// <summary>MJ/EFFECT_AUDIO/sound_event.mp3</summary>
    public const string MJ_EFFECT_AUDIO_SOUND_EVENT = "MJ/EFFECT_AUDIO/sound_event.mp3";
    /// <summary>MJ/EFFECT_AUDIO/sound_move.mp3</summary>
    public const string MJ_EFFECT_AUDIO_SOUND_MOVE = "MJ/EFFECT_AUDIO/sound_move.mp3";
    /// <summary>MJ/EFFECT_AUDIO/zhsz.mp3</summary>
    public const string MJ_EFFECT_AUDIO_ZHSZ = "MJ/EFFECT_AUDIO/zhsz.mp3";
    /// <summary>MJ/EFFECT_AUDIO/zhuochong.mp3</summary>
    public const string MJ_EFFECT_AUDIO_ZHUOCHONG = "MJ/EFFECT_AUDIO/zhuochong.mp3";

    #endregion

    #region MJ/MUSIC_AUDIO

    /// <summary>MJ/MUSIC_AUDIO/playingInGame.mp3</summary>
    public const string MJ_MUSIC_AUDIO_PLAYINGINGAME = "MJ/MUSIC_AUDIO/playingInGame.mp3";
    /// <summary>MJ/MUSIC_AUDIO/ready.mp3</summary>
    public const string MJ_MUSIC_AUDIO_READY = "MJ/MUSIC_AUDIO/ready.mp3";

    #endregion

    #region MJ/PT_SPEAK/girl/cardSound

    /// <summary>MJ/PT_SPEAK/girl/cardSound/baiban.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_BAIBAN = "MJ/PT_SPEAK/girl/cardSound/baiban.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/beifeng.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_BEIFENG = "MJ/PT_SPEAK/girl/cardSound/beifeng.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/dongfeng.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_DONGFENG = "MJ/PT_SPEAK/girl/cardSound/dongfeng.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/facai.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_FACAI = "MJ/PT_SPEAK/girl/cardSound/facai.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/hongzhong.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_HONGZHONG = "MJ/PT_SPEAK/girl/cardSound/hongzhong.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/nanfeng.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_NANFENG = "MJ/PT_SPEAK/girl/cardSound/nanfeng.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/tiao1.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_TIAO1 = "MJ/PT_SPEAK/girl/cardSound/tiao1.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/tiao2.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_TIAO2 = "MJ/PT_SPEAK/girl/cardSound/tiao2.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/tiao3.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_TIAO3 = "MJ/PT_SPEAK/girl/cardSound/tiao3.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/tiao4.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_TIAO4 = "MJ/PT_SPEAK/girl/cardSound/tiao4.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/tiao5.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_TIAO5 = "MJ/PT_SPEAK/girl/cardSound/tiao5.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/tiao6.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_TIAO6 = "MJ/PT_SPEAK/girl/cardSound/tiao6.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/tiao7.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_TIAO7 = "MJ/PT_SPEAK/girl/cardSound/tiao7.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/tiao8.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_TIAO8 = "MJ/PT_SPEAK/girl/cardSound/tiao8.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/tiao9.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_TIAO9 = "MJ/PT_SPEAK/girl/cardSound/tiao9.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/tong1.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_TONG1 = "MJ/PT_SPEAK/girl/cardSound/tong1.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/tong2.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_TONG2 = "MJ/PT_SPEAK/girl/cardSound/tong2.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/tong3.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_TONG3 = "MJ/PT_SPEAK/girl/cardSound/tong3.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/tong4.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_TONG4 = "MJ/PT_SPEAK/girl/cardSound/tong4.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/tong5.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_TONG5 = "MJ/PT_SPEAK/girl/cardSound/tong5.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/tong6.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_TONG6 = "MJ/PT_SPEAK/girl/cardSound/tong6.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/tong7.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_TONG7 = "MJ/PT_SPEAK/girl/cardSound/tong7.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/tong8.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_TONG8 = "MJ/PT_SPEAK/girl/cardSound/tong8.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/tong9.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_TONG9 = "MJ/PT_SPEAK/girl/cardSound/tong9.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/wan1.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_WAN1 = "MJ/PT_SPEAK/girl/cardSound/wan1.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/wan2.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_WAN2 = "MJ/PT_SPEAK/girl/cardSound/wan2.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/wan3.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_WAN3 = "MJ/PT_SPEAK/girl/cardSound/wan3.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/wan4.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_WAN4 = "MJ/PT_SPEAK/girl/cardSound/wan4.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/wan5.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_WAN5 = "MJ/PT_SPEAK/girl/cardSound/wan5.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/wan6.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_WAN6 = "MJ/PT_SPEAK/girl/cardSound/wan6.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/wan7.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_WAN7 = "MJ/PT_SPEAK/girl/cardSound/wan7.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/wan8.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_WAN8 = "MJ/PT_SPEAK/girl/cardSound/wan8.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/wan9.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_WAN9 = "MJ/PT_SPEAK/girl/cardSound/wan9.mp3";
    /// <summary>MJ/PT_SPEAK/girl/cardSound/xifeng.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CARDSOUND_XIFENG = "MJ/PT_SPEAK/girl/cardSound/xifeng.mp3";

    #endregion

    #region MJ/PT_SPEAK/girl/controlSound

    /// <summary>MJ/PT_SPEAK/girl/controlSound/angang_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_ANGANG_0 = "MJ/PT_SPEAK/girl/controlSound/angang_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/chaogang_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_CHAOGANG_0 = "MJ/PT_SPEAK/girl/controlSound/chaogang_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/chi_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_CHI_0 = "MJ/PT_SPEAK/girl/controlSound/chi_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/chi_1.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_CHI_1 = "MJ/PT_SPEAK/girl/controlSound/chi_1.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/chi_2.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_CHI_2 = "MJ/PT_SPEAK/girl/controlSound/chi_2.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/fengyise_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_FENGYISE_0 = "MJ/PT_SPEAK/girl/controlSound/fengyise_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/gang_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_GANG_0 = "MJ/PT_SPEAK/girl/controlSound/gang_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/gang_1.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_GANG_1 = "MJ/PT_SPEAK/girl/controlSound/gang_1.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/gangshangkaihua_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_GANGSHANGKAIHUA_0 = "MJ/PT_SPEAK/girl/controlSound/gangshangkaihua_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/haidilaoyue_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_HAIDILAOYUE_0 = "MJ/PT_SPEAK/girl/controlSound/haidilaoyue_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/heimo_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_HEIMO_0 = "MJ/PT_SPEAK/girl/controlSound/heimo_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/hu_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_HU_0 = "MJ/PT_SPEAK/girl/controlSound/hu_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/hu_1.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_HU_1 = "MJ/PT_SPEAK/girl/controlSound/hu_1.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/jiangyise_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_JIANGYISE_0 = "MJ/PT_SPEAK/girl/controlSound/jiangyise_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/laizigang_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_LAIZIGANG_0 = "MJ/PT_SPEAK/girl/controlSound/laizigang_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/laizigang_1.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_LAIZIGANG_1 = "MJ/PT_SPEAK/girl/controlSound/laizigang_1.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/liang_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_LIANG_0 = "MJ/PT_SPEAK/girl/controlSound/liang_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/peng_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_PENG_0 = "MJ/PT_SPEAK/girl/controlSound/peng_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/peng_1.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_PENG_1 = "MJ/PT_SPEAK/girl/controlSound/peng_1.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/peng_2.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_PENG_2 = "MJ/PT_SPEAK/girl/controlSound/peng_2.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/pengpenghu_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_PENGPENGHU_0 = "MJ/PT_SPEAK/girl/controlSound/pengpenghu_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/pizigang_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_PIZIGANG_0 = "MJ/PT_SPEAK/girl/controlSound/pizigang_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/qiangganghu_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_QIANGGANGHU_0 = "MJ/PT_SPEAK/girl/controlSound/qiangganghu_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/qidui_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_QIDUI_0 = "MJ/PT_SPEAK/girl/controlSound/qidui_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/qingyise_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_QINGYISE_0 = "MJ/PT_SPEAK/girl/controlSound/qingyise_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/quanqiuren_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_QUANQIUREN_0 = "MJ/PT_SPEAK/girl/controlSound/quanqiuren_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/rechong_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_RECHONG_0 = "MJ/PT_SPEAK/girl/controlSound/rechong_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/tianhu_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_TIANHU_0 = "MJ/PT_SPEAK/girl/controlSound/tianhu_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/xiaochaotian_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_XIAOCHAOTIAN_0 = "MJ/PT_SPEAK/girl/controlSound/xiaochaotian_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/xugang_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_XUGANG_0 = "MJ/PT_SPEAK/girl/controlSound/xugang_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/zhunbei_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_ZHUNBEI_0 = "MJ/PT_SPEAK/girl/controlSound/zhunbei_0.mp3";
    /// <summary>MJ/PT_SPEAK/girl/controlSound/zimo_0.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_CONTROLSOUND_ZIMO_0 = "MJ/PT_SPEAK/girl/controlSound/zimo_0.mp3";

    #endregion

    #region MJ/PT_SPEAK/girl/hupaiType

    /// <summary>MJ/PT_SPEAK/girl/hupaiType/ansigui.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_HUPAITYPE_ANSIGUI = "MJ/PT_SPEAK/girl/hupaiType/ansigui.mp3";
    /// <summary>MJ/PT_SPEAK/girl/hupaiType/chaochaohaohua.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_HUPAITYPE_CHAOCHAOHAOHUA = "MJ/PT_SPEAK/girl/hupaiType/chaochaohaohua.mp3";
    /// <summary>MJ/PT_SPEAK/girl/hupaiType/chaohaohua.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_HUPAITYPE_CHAOHAOHUA = "MJ/PT_SPEAK/girl/hupaiType/chaohaohua.mp3";
    /// <summary>MJ/PT_SPEAK/girl/hupaiType/dasanyuan.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_HUPAITYPE_DASANYUAN = "MJ/PT_SPEAK/girl/hupaiType/dasanyuan.mp3";
    /// <summary>MJ/PT_SPEAK/girl/hupaiType/haohuaqiduiI.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_HUPAITYPE_HAOHUAQIDUII = "MJ/PT_SPEAK/girl/hupaiType/haohuaqiduiI.mp3";
    /// <summary>MJ/PT_SPEAK/girl/hupaiType/kawuxing.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_HUPAITYPE_KAWUXING = "MJ/PT_SPEAK/girl/hupaiType/kawuxing.mp3";
    /// <summary>MJ/PT_SPEAK/girl/hupaiType/mingsigui.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_HUPAITYPE_MINGSIGUI = "MJ/PT_SPEAK/girl/hupaiType/mingsigui.mp3";
    /// <summary>MJ/PT_SPEAK/girl/hupaiType/pengpeng.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_HUPAITYPE_PENGPENG = "MJ/PT_SPEAK/girl/hupaiType/pengpeng.mp3";
    /// <summary>MJ/PT_SPEAK/girl/hupaiType/qiduihuapai.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_HUPAITYPE_QIDUIHUAPAI = "MJ/PT_SPEAK/girl/hupaiType/qiduihuapai.mp3";
    /// <summary>MJ/PT_SPEAK/girl/hupaiType/qingyise.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_HUPAITYPE_QINGYISE = "MJ/PT_SPEAK/girl/hupaiType/qingyise.mp3";
    /// <summary>MJ/PT_SPEAK/girl/hupaiType/shouzhuayi.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_HUPAITYPE_SHOUZHUAYI = "MJ/PT_SPEAK/girl/hupaiType/shouzhuayi.mp3";
    /// <summary>MJ/PT_SPEAK/girl/hupaiType/xiaosanyuan.mp3</summary>
    public const string MJ_PT_SPEAK_GIRL_HUPAITYPE_XIAOSANYUAN = "MJ/PT_SPEAK/girl/hupaiType/xiaosanyuan.mp3";

    #endregion

    #region MJ/PT_SPEAK/man/cardSound

    /// <summary>MJ/PT_SPEAK/man/cardSound/baiban.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_BAIBAN = "MJ/PT_SPEAK/man/cardSound/baiban.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/beifeng.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_BEIFENG = "MJ/PT_SPEAK/man/cardSound/beifeng.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/dongfeng.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_DONGFENG = "MJ/PT_SPEAK/man/cardSound/dongfeng.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/facai.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_FACAI = "MJ/PT_SPEAK/man/cardSound/facai.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/hongzhong.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_HONGZHONG = "MJ/PT_SPEAK/man/cardSound/hongzhong.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/nanfeng.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_NANFENG = "MJ/PT_SPEAK/man/cardSound/nanfeng.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/tiao1.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_TIAO1 = "MJ/PT_SPEAK/man/cardSound/tiao1.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/tiao2.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_TIAO2 = "MJ/PT_SPEAK/man/cardSound/tiao2.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/tiao3.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_TIAO3 = "MJ/PT_SPEAK/man/cardSound/tiao3.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/tiao4.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_TIAO4 = "MJ/PT_SPEAK/man/cardSound/tiao4.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/tiao5.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_TIAO5 = "MJ/PT_SPEAK/man/cardSound/tiao5.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/tiao6.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_TIAO6 = "MJ/PT_SPEAK/man/cardSound/tiao6.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/tiao7.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_TIAO7 = "MJ/PT_SPEAK/man/cardSound/tiao7.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/tiao8.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_TIAO8 = "MJ/PT_SPEAK/man/cardSound/tiao8.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/tiao9.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_TIAO9 = "MJ/PT_SPEAK/man/cardSound/tiao9.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/tong1.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_TONG1 = "MJ/PT_SPEAK/man/cardSound/tong1.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/tong2.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_TONG2 = "MJ/PT_SPEAK/man/cardSound/tong2.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/tong3.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_TONG3 = "MJ/PT_SPEAK/man/cardSound/tong3.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/tong4.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_TONG4 = "MJ/PT_SPEAK/man/cardSound/tong4.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/tong5.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_TONG5 = "MJ/PT_SPEAK/man/cardSound/tong5.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/tong6.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_TONG6 = "MJ/PT_SPEAK/man/cardSound/tong6.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/tong7.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_TONG7 = "MJ/PT_SPEAK/man/cardSound/tong7.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/tong8.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_TONG8 = "MJ/PT_SPEAK/man/cardSound/tong8.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/tong9.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_TONG9 = "MJ/PT_SPEAK/man/cardSound/tong9.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/wan1.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_WAN1 = "MJ/PT_SPEAK/man/cardSound/wan1.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/wan2.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_WAN2 = "MJ/PT_SPEAK/man/cardSound/wan2.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/wan3.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_WAN3 = "MJ/PT_SPEAK/man/cardSound/wan3.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/wan4.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_WAN4 = "MJ/PT_SPEAK/man/cardSound/wan4.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/wan5.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_WAN5 = "MJ/PT_SPEAK/man/cardSound/wan5.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/wan6.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_WAN6 = "MJ/PT_SPEAK/man/cardSound/wan6.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/wan7.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_WAN7 = "MJ/PT_SPEAK/man/cardSound/wan7.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/wan8.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_WAN8 = "MJ/PT_SPEAK/man/cardSound/wan8.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/wan9.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_WAN9 = "MJ/PT_SPEAK/man/cardSound/wan9.mp3";
    /// <summary>MJ/PT_SPEAK/man/cardSound/xifeng.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CARDSOUND_XIFENG = "MJ/PT_SPEAK/man/cardSound/xifeng.mp3";

    #endregion

    #region MJ/PT_SPEAK/man/controlSound

    /// <summary>MJ/PT_SPEAK/man/controlSound/angang_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_ANGANG_0 = "MJ/PT_SPEAK/man/controlSound/angang_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/chaogang_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_CHAOGANG_0 = "MJ/PT_SPEAK/man/controlSound/chaogang_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/chi_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_CHI_0 = "MJ/PT_SPEAK/man/controlSound/chi_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/chi_1.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_CHI_1 = "MJ/PT_SPEAK/man/controlSound/chi_1.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/chi_2.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_CHI_2 = "MJ/PT_SPEAK/man/controlSound/chi_2.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/fengyise_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_FENGYISE_0 = "MJ/PT_SPEAK/man/controlSound/fengyise_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/gang_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_GANG_0 = "MJ/PT_SPEAK/man/controlSound/gang_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/gang_1.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_GANG_1 = "MJ/PT_SPEAK/man/controlSound/gang_1.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/gangshangkaihua_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_GANGSHANGKAIHUA_0 = "MJ/PT_SPEAK/man/controlSound/gangshangkaihua_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/haidilaoyue_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_HAIDILAOYUE_0 = "MJ/PT_SPEAK/man/controlSound/haidilaoyue_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/heimo_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_HEIMO_0 = "MJ/PT_SPEAK/man/controlSound/heimo_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/hu_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_HU_0 = "MJ/PT_SPEAK/man/controlSound/hu_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/hu_1.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_HU_1 = "MJ/PT_SPEAK/man/controlSound/hu_1.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/jiangyise_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_JIANGYISE_0 = "MJ/PT_SPEAK/man/controlSound/jiangyise_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/laizigang_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_LAIZIGANG_0 = "MJ/PT_SPEAK/man/controlSound/laizigang_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/laizigang_1.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_LAIZIGANG_1 = "MJ/PT_SPEAK/man/controlSound/laizigang_1.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/liang_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_LIANG_0 = "MJ/PT_SPEAK/man/controlSound/liang_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/peng_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_PENG_0 = "MJ/PT_SPEAK/man/controlSound/peng_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/peng_1.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_PENG_1 = "MJ/PT_SPEAK/man/controlSound/peng_1.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/peng_2.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_PENG_2 = "MJ/PT_SPEAK/man/controlSound/peng_2.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/pengpenghu_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_PENGPENGHU_0 = "MJ/PT_SPEAK/man/controlSound/pengpenghu_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/pizigang_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_PIZIGANG_0 = "MJ/PT_SPEAK/man/controlSound/pizigang_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/qiangganghu_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_QIANGGANGHU_0 = "MJ/PT_SPEAK/man/controlSound/qiangganghu_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/qidui_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_QIDUI_0 = "MJ/PT_SPEAK/man/controlSound/qidui_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/qingyise_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_QINGYISE_0 = "MJ/PT_SPEAK/man/controlSound/qingyise_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/quanqiuren_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_QUANQIUREN_0 = "MJ/PT_SPEAK/man/controlSound/quanqiuren_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/rechong_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_RECHONG_0 = "MJ/PT_SPEAK/man/controlSound/rechong_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/tianhu_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_TIANHU_0 = "MJ/PT_SPEAK/man/controlSound/tianhu_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/xiaochaotian_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_XIAOCHAOTIAN_0 = "MJ/PT_SPEAK/man/controlSound/xiaochaotian_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/xugang_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_XUGANG_0 = "MJ/PT_SPEAK/man/controlSound/xugang_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/zhunbei_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_ZHUNBEI_0 = "MJ/PT_SPEAK/man/controlSound/zhunbei_0.mp3";
    /// <summary>MJ/PT_SPEAK/man/controlSound/zimo_0.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_CONTROLSOUND_ZIMO_0 = "MJ/PT_SPEAK/man/controlSound/zimo_0.mp3";

    #endregion

    #region MJ/PT_SPEAK/man/hupaiType

    /// <summary>MJ/PT_SPEAK/man/hupaiType/ansigui.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_HUPAITYPE_ANSIGUI = "MJ/PT_SPEAK/man/hupaiType/ansigui.mp3";
    /// <summary>MJ/PT_SPEAK/man/hupaiType/chaochaohaohua.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_HUPAITYPE_CHAOCHAOHAOHUA = "MJ/PT_SPEAK/man/hupaiType/chaochaohaohua.mp3";
    /// <summary>MJ/PT_SPEAK/man/hupaiType/chaohaohua.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_HUPAITYPE_CHAOHAOHUA = "MJ/PT_SPEAK/man/hupaiType/chaohaohua.mp3";
    /// <summary>MJ/PT_SPEAK/man/hupaiType/dasanyuan.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_HUPAITYPE_DASANYUAN = "MJ/PT_SPEAK/man/hupaiType/dasanyuan.mp3";
    /// <summary>MJ/PT_SPEAK/man/hupaiType/haohuaqiduiI.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_HUPAITYPE_HAOHUAQIDUII = "MJ/PT_SPEAK/man/hupaiType/haohuaqiduiI.mp3";
    /// <summary>MJ/PT_SPEAK/man/hupaiType/kawuxing.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_HUPAITYPE_KAWUXING = "MJ/PT_SPEAK/man/hupaiType/kawuxing.mp3";
    /// <summary>MJ/PT_SPEAK/man/hupaiType/mingsigui.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_HUPAITYPE_MINGSIGUI = "MJ/PT_SPEAK/man/hupaiType/mingsigui.mp3";
    /// <summary>MJ/PT_SPEAK/man/hupaiType/pengpeng.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_HUPAITYPE_PENGPENG = "MJ/PT_SPEAK/man/hupaiType/pengpeng.mp3";
    /// <summary>MJ/PT_SPEAK/man/hupaiType/qiduihuapai.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_HUPAITYPE_QIDUIHUAPAI = "MJ/PT_SPEAK/man/hupaiType/qiduihuapai.mp3";
    /// <summary>MJ/PT_SPEAK/man/hupaiType/qingyise.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_HUPAITYPE_QINGYISE = "MJ/PT_SPEAK/man/hupaiType/qingyise.mp3";
    /// <summary>MJ/PT_SPEAK/man/hupaiType/shouzhuayi.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_HUPAITYPE_SHOUZHUAYI = "MJ/PT_SPEAK/man/hupaiType/shouzhuayi.mp3";
    /// <summary>MJ/PT_SPEAK/man/hupaiType/xiaosanyuan.mp3</summary>
    public const string MJ_PT_SPEAK_MAN_HUPAITYPE_XIAOSANYUAN = "MJ/PT_SPEAK/man/hupaiType/xiaosanyuan.mp3";

    #endregion

    #region MJ/XT_SPEAK/girl/cardSound

    /// <summary>MJ/XT_SPEAK/girl/cardSound/baiban.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_BAIBAN = "MJ/XT_SPEAK/girl/cardSound/baiban.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/beifeng.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_BEIFENG = "MJ/XT_SPEAK/girl/cardSound/beifeng.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/dongfeng.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_DONGFENG = "MJ/XT_SPEAK/girl/cardSound/dongfeng.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/facai.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_FACAI = "MJ/XT_SPEAK/girl/cardSound/facai.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/hongzhong.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_HONGZHONG = "MJ/XT_SPEAK/girl/cardSound/hongzhong.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/nanfeng.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_NANFENG = "MJ/XT_SPEAK/girl/cardSound/nanfeng.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/tiao1.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_TIAO1 = "MJ/XT_SPEAK/girl/cardSound/tiao1.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/tiao2.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_TIAO2 = "MJ/XT_SPEAK/girl/cardSound/tiao2.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/tiao3.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_TIAO3 = "MJ/XT_SPEAK/girl/cardSound/tiao3.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/tiao4.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_TIAO4 = "MJ/XT_SPEAK/girl/cardSound/tiao4.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/tiao5.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_TIAO5 = "MJ/XT_SPEAK/girl/cardSound/tiao5.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/tiao6.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_TIAO6 = "MJ/XT_SPEAK/girl/cardSound/tiao6.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/tiao7.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_TIAO7 = "MJ/XT_SPEAK/girl/cardSound/tiao7.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/tiao8.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_TIAO8 = "MJ/XT_SPEAK/girl/cardSound/tiao8.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/tiao9.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_TIAO9 = "MJ/XT_SPEAK/girl/cardSound/tiao9.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/tong1.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_TONG1 = "MJ/XT_SPEAK/girl/cardSound/tong1.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/tong2.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_TONG2 = "MJ/XT_SPEAK/girl/cardSound/tong2.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/tong3.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_TONG3 = "MJ/XT_SPEAK/girl/cardSound/tong3.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/tong4.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_TONG4 = "MJ/XT_SPEAK/girl/cardSound/tong4.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/tong5.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_TONG5 = "MJ/XT_SPEAK/girl/cardSound/tong5.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/tong6.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_TONG6 = "MJ/XT_SPEAK/girl/cardSound/tong6.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/tong7.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_TONG7 = "MJ/XT_SPEAK/girl/cardSound/tong7.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/tong8.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_TONG8 = "MJ/XT_SPEAK/girl/cardSound/tong8.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/tong9.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_TONG9 = "MJ/XT_SPEAK/girl/cardSound/tong9.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/wan1.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_WAN1 = "MJ/XT_SPEAK/girl/cardSound/wan1.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/wan2.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_WAN2 = "MJ/XT_SPEAK/girl/cardSound/wan2.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/wan3.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_WAN3 = "MJ/XT_SPEAK/girl/cardSound/wan3.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/wan4.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_WAN4 = "MJ/XT_SPEAK/girl/cardSound/wan4.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/wan5.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_WAN5 = "MJ/XT_SPEAK/girl/cardSound/wan5.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/wan6.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_WAN6 = "MJ/XT_SPEAK/girl/cardSound/wan6.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/wan7.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_WAN7 = "MJ/XT_SPEAK/girl/cardSound/wan7.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/wan8.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_WAN8 = "MJ/XT_SPEAK/girl/cardSound/wan8.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/wan9.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_WAN9 = "MJ/XT_SPEAK/girl/cardSound/wan9.mp3";
    /// <summary>MJ/XT_SPEAK/girl/cardSound/xifeng.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CARDSOUND_XIFENG = "MJ/XT_SPEAK/girl/cardSound/xifeng.mp3";

    #endregion

    #region MJ/XT_SPEAK/girl/controlSound

    /// <summary>MJ/XT_SPEAK/girl/controlSound/angang_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_ANGANG_0 = "MJ/XT_SPEAK/girl/controlSound/angang_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/chaogang_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_CHAOGANG_0 = "MJ/XT_SPEAK/girl/controlSound/chaogang_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/chi_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_CHI_0 = "MJ/XT_SPEAK/girl/controlSound/chi_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/fengyise_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_FENGYISE_0 = "MJ/XT_SPEAK/girl/controlSound/fengyise_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/gang_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_GANG_0 = "MJ/XT_SPEAK/girl/controlSound/gang_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/gang_1.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_GANG_1 = "MJ/XT_SPEAK/girl/controlSound/gang_1.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/gangshangkaihua_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_GANGSHANGKAIHUA_0 = "MJ/XT_SPEAK/girl/controlSound/gangshangkaihua_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/haidilaoyue_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_HAIDILAOYUE_0 = "MJ/XT_SPEAK/girl/controlSound/haidilaoyue_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/heimo_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_HEIMO_0 = "MJ/XT_SPEAK/girl/controlSound/heimo_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/hu_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_HU_0 = "MJ/XT_SPEAK/girl/controlSound/hu_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/hu_1.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_HU_1 = "MJ/XT_SPEAK/girl/controlSound/hu_1.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/jiangyise_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_JIANGYISE_0 = "MJ/XT_SPEAK/girl/controlSound/jiangyise_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/laizigang_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_LAIZIGANG_0 = "MJ/XT_SPEAK/girl/controlSound/laizigang_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/peng_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_PENG_0 = "MJ/XT_SPEAK/girl/controlSound/peng_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/qiangganghu_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_QIANGGANGHU_0 = "MJ/XT_SPEAK/girl/controlSound/qiangganghu_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/qidui_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_QIDUI_0 = "MJ/XT_SPEAK/girl/controlSound/qidui_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/qingyise_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_QINGYISE_0 = "MJ/XT_SPEAK/girl/controlSound/qingyise_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/rechong_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_RECHONG_0 = "MJ/XT_SPEAK/girl/controlSound/rechong_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/tianhu_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_TIANHU_0 = "MJ/XT_SPEAK/girl/controlSound/tianhu_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/xiaochaotian_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_XIAOCHAOTIAN_0 = "MJ/XT_SPEAK/girl/controlSound/xiaochaotian_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/xugang_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_XUGANG_0 = "MJ/XT_SPEAK/girl/controlSound/xugang_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/zhunbei_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_ZHUNBEI_0 = "MJ/XT_SPEAK/girl/controlSound/zhunbei_0.mp3";
    /// <summary>MJ/XT_SPEAK/girl/controlSound/zimo_0.mp3</summary>
    public const string MJ_XT_SPEAK_GIRL_CONTROLSOUND_ZIMO_0 = "MJ/XT_SPEAK/girl/controlSound/zimo_0.mp3";

    #endregion

    #region MJ/XT_SPEAK/man/cardSound

    /// <summary>MJ/XT_SPEAK/man/cardSound/baiban.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_BAIBAN = "MJ/XT_SPEAK/man/cardSound/baiban.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/beifeng.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_BEIFENG = "MJ/XT_SPEAK/man/cardSound/beifeng.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/dongfeng.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_DONGFENG = "MJ/XT_SPEAK/man/cardSound/dongfeng.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/facai.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_FACAI = "MJ/XT_SPEAK/man/cardSound/facai.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/hongzhong.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_HONGZHONG = "MJ/XT_SPEAK/man/cardSound/hongzhong.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/nanfeng.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_NANFENG = "MJ/XT_SPEAK/man/cardSound/nanfeng.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/tiao1.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_TIAO1 = "MJ/XT_SPEAK/man/cardSound/tiao1.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/tiao2.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_TIAO2 = "MJ/XT_SPEAK/man/cardSound/tiao2.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/tiao3.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_TIAO3 = "MJ/XT_SPEAK/man/cardSound/tiao3.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/tiao4.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_TIAO4 = "MJ/XT_SPEAK/man/cardSound/tiao4.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/tiao5.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_TIAO5 = "MJ/XT_SPEAK/man/cardSound/tiao5.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/tiao6.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_TIAO6 = "MJ/XT_SPEAK/man/cardSound/tiao6.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/tiao7.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_TIAO7 = "MJ/XT_SPEAK/man/cardSound/tiao7.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/tiao8.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_TIAO8 = "MJ/XT_SPEAK/man/cardSound/tiao8.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/tiao9.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_TIAO9 = "MJ/XT_SPEAK/man/cardSound/tiao9.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/tong1.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_TONG1 = "MJ/XT_SPEAK/man/cardSound/tong1.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/tong2.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_TONG2 = "MJ/XT_SPEAK/man/cardSound/tong2.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/tong3.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_TONG3 = "MJ/XT_SPEAK/man/cardSound/tong3.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/tong4.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_TONG4 = "MJ/XT_SPEAK/man/cardSound/tong4.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/tong5.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_TONG5 = "MJ/XT_SPEAK/man/cardSound/tong5.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/tong6.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_TONG6 = "MJ/XT_SPEAK/man/cardSound/tong6.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/tong7.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_TONG7 = "MJ/XT_SPEAK/man/cardSound/tong7.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/tong8.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_TONG8 = "MJ/XT_SPEAK/man/cardSound/tong8.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/tong9.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_TONG9 = "MJ/XT_SPEAK/man/cardSound/tong9.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/wan1.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_WAN1 = "MJ/XT_SPEAK/man/cardSound/wan1.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/wan2.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_WAN2 = "MJ/XT_SPEAK/man/cardSound/wan2.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/wan3.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_WAN3 = "MJ/XT_SPEAK/man/cardSound/wan3.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/wan4.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_WAN4 = "MJ/XT_SPEAK/man/cardSound/wan4.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/wan5.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_WAN5 = "MJ/XT_SPEAK/man/cardSound/wan5.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/wan6.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_WAN6 = "MJ/XT_SPEAK/man/cardSound/wan6.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/wan7.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_WAN7 = "MJ/XT_SPEAK/man/cardSound/wan7.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/wan8.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_WAN8 = "MJ/XT_SPEAK/man/cardSound/wan8.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/wan9.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_WAN9 = "MJ/XT_SPEAK/man/cardSound/wan9.mp3";
    /// <summary>MJ/XT_SPEAK/man/cardSound/xifeng.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CARDSOUND_XIFENG = "MJ/XT_SPEAK/man/cardSound/xifeng.mp3";

    #endregion

    #region MJ/XT_SPEAK/man/controlSound

    /// <summary>MJ/XT_SPEAK/man/controlSound/angang_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_ANGANG_0 = "MJ/XT_SPEAK/man/controlSound/angang_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/chaogang_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_CHAOGANG_0 = "MJ/XT_SPEAK/man/controlSound/chaogang_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/chi_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_CHI_0 = "MJ/XT_SPEAK/man/controlSound/chi_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/fengyise_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_FENGYISE_0 = "MJ/XT_SPEAK/man/controlSound/fengyise_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/gang_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_GANG_0 = "MJ/XT_SPEAK/man/controlSound/gang_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/gang_1.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_GANG_1 = "MJ/XT_SPEAK/man/controlSound/gang_1.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/gangshangkaihua_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_GANGSHANGKAIHUA_0 = "MJ/XT_SPEAK/man/controlSound/gangshangkaihua_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/haidilaoyue_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_HAIDILAOYUE_0 = "MJ/XT_SPEAK/man/controlSound/haidilaoyue_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/heimo_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_HEIMO_0 = "MJ/XT_SPEAK/man/controlSound/heimo_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/hu_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_HU_0 = "MJ/XT_SPEAK/man/controlSound/hu_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/hu_1.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_HU_1 = "MJ/XT_SPEAK/man/controlSound/hu_1.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/jiangyise_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_JIANGYISE_0 = "MJ/XT_SPEAK/man/controlSound/jiangyise_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/laizigang_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_LAIZIGANG_0 = "MJ/XT_SPEAK/man/controlSound/laizigang_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/peng_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_PENG_0 = "MJ/XT_SPEAK/man/controlSound/peng_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/qiangganghu_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_QIANGGANGHU_0 = "MJ/XT_SPEAK/man/controlSound/qiangganghu_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/qidui_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_QIDUI_0 = "MJ/XT_SPEAK/man/controlSound/qidui_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/qingyise_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_QINGYISE_0 = "MJ/XT_SPEAK/man/controlSound/qingyise_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/rechong_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_RECHONG_0 = "MJ/XT_SPEAK/man/controlSound/rechong_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/tianhu_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_TIANHU_0 = "MJ/XT_SPEAK/man/controlSound/tianhu_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/xiaochaotian_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_XIAOCHAOTIAN_0 = "MJ/XT_SPEAK/man/controlSound/xiaochaotian_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/xugang_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_XUGANG_0 = "MJ/XT_SPEAK/man/controlSound/xugang_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/zhunbei_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_ZHUNBEI_0 = "MJ/XT_SPEAK/man/controlSound/zhunbei_0.mp3";
    /// <summary>MJ/XT_SPEAK/man/controlSound/zimo_0.mp3</summary>
    public const string MJ_XT_SPEAK_MAN_CONTROLSOUND_ZIMO_0 = "MJ/XT_SPEAK/man/controlSound/zimo_0.mp3";

    #endregion

    #region niuniu

    /// <summary>niuniu/action.wav</summary>
    public const string NIUNIU_ACTION = "niuniu/action.wav";
    /// <summary>niuniu/card.mp3</summary>
    public const string NIUNIU_CARD = "niuniu/card.mp3";
    /// <summary>niuniu/jumpbanker.mp3</summary>
    public const string NIUNIU_JUMPBANKER = "niuniu/jumpbanker.mp3";
    /// <summary>niuniu/Niu0.mp3</summary>
    public const string NIUNIU_NIU0 = "niuniu/Niu0.mp3";
    /// <summary>niuniu/Niu1.mp3</summary>
    public const string NIUNIU_NIU1 = "niuniu/Niu1.mp3";
    /// <summary>niuniu/Niu10.mp3</summary>
    public const string NIUNIU_NIU10 = "niuniu/Niu10.mp3";
    /// <summary>niuniu/Niu11.mp3</summary>
    public const string NIUNIU_NIU11 = "niuniu/Niu11.mp3";
    /// <summary>niuniu/Niu12.mp3</summary>
    public const string NIUNIU_NIU12 = "niuniu/Niu12.mp3";
    /// <summary>niuniu/Niu13.mp3</summary>
    public const string NIUNIU_NIU13 = "niuniu/Niu13.mp3";
    /// <summary>niuniu/Niu14.mp3</summary>
    public const string NIUNIU_NIU14 = "niuniu/Niu14.mp3";
    /// <summary>niuniu/Niu15.mp3</summary>
    public const string NIUNIU_NIU15 = "niuniu/Niu15.mp3";
    /// <summary>niuniu/Niu16.mp3</summary>
    public const string NIUNIU_NIU16 = "niuniu/Niu16.mp3";
    /// <summary>niuniu/Niu17.mp3</summary>
    public const string NIUNIU_NIU17 = "niuniu/Niu17.mp3";
    /// <summary>niuniu/Niu2.mp3</summary>
    public const string NIUNIU_NIU2 = "niuniu/Niu2.mp3";
    /// <summary>niuniu/Niu3.mp3</summary>
    public const string NIUNIU_NIU3 = "niuniu/Niu3.mp3";
    /// <summary>niuniu/Niu4.mp3</summary>
    public const string NIUNIU_NIU4 = "niuniu/Niu4.mp3";
    /// <summary>niuniu/Niu5.mp3</summary>
    public const string NIUNIU_NIU5 = "niuniu/Niu5.mp3";
    /// <summary>niuniu/Niu6.mp3</summary>
    public const string NIUNIU_NIU6 = "niuniu/Niu6.mp3";
    /// <summary>niuniu/Niu7.mp3</summary>
    public const string NIUNIU_NIU7 = "niuniu/Niu7.mp3";
    /// <summary>niuniu/Niu8.mp3</summary>
    public const string NIUNIU_NIU8 = "niuniu/Niu8.mp3";
    /// <summary>niuniu/Niu9.mp3</summary>
    public const string NIUNIU_NIU9 = "niuniu/Niu9.mp3";
    /// <summary>niuniu/NiuNiuTeXiaoYin.mp3</summary>
    public const string NIUNIU_NIUNIUTEXIAOYIN = "niuniu/NiuNiuTeXiaoYin.mp3";
    /// <summary>niuniu/randombanker.mp3</summary>
    public const string NIUNIU_RANDOMBANKER = "niuniu/randombanker.mp3";
    /// <summary>niuniu/rate0.mp3</summary>
    public const string NIUNIU_RATE0 = "niuniu/rate0.mp3";
    /// <summary>niuniu/rate1.mp3</summary>
    public const string NIUNIU_RATE1 = "niuniu/rate1.mp3";
    /// <summary>niuniu/rate2.mp3</summary>
    public const string NIUNIU_RATE2 = "niuniu/rate2.mp3";
    /// <summary>niuniu/rate3.mp3</summary>
    public const string NIUNIU_RATE3 = "niuniu/rate3.mp3";
    /// <summary>niuniu/TeShuNiuTeXiaoYin.mp3</summary>
    public const string NIUNIU_TESHUNIUTEXIAOYIN = "niuniu/TeShuNiuTeXiaoYin.mp3";
    /// <summary>niuniu/win.mp3</summary>
    public const string NIUNIU_WIN = "niuniu/win.mp3";

    #endregion

    #region pdk

    /// <summary>pdk/alarmclock.mp3</summary>
    public const string PDK_ALARMCLOCK = "pdk/alarmclock.mp3";
    /// <summary>pdk/baozi.mp3</summary>
    public const string PDK_BAOZI = "pdk/baozi.mp3";
    /// <summary>pdk/bomb1.mp3</summary>
    public const string PDK_BOMB1 = "pdk/bomb1.mp3";
    /// <summary>pdk/boom.mp3</summary>
    public const string PDK_BOOM = "pdk/boom.mp3";
    /// <summary>pdk/Button.mp3</summary>
    public const string PDK_BUTTON = "pdk/Button.mp3";
    /// <summary>pdk/chick.mp3</summary>
    public const string PDK_CHICK = "pdk/chick.mp3";
    /// <summary>pdk/chosecard.mp3</summary>
    public const string PDK_CHOSECARD = "pdk/chosecard.mp3";
    /// <summary>pdk/chuntian.mp3</summary>
    public const string PDK_CHUNTIAN = "pdk/chuntian.mp3";
    /// <summary>pdk/chupai.mp3</summary>
    public const string PDK_CHUPAI = "pdk/chupai.mp3";
    /// <summary>pdk/dao.mp3</summary>
    public const string PDK_DAO = "pdk/dao.mp3";
    /// <summary>pdk/fapai.mp3</summary>
    public const string PDK_FAPAI = "pdk/fapai.mp3";
    /// <summary>pdk/hema.mp3</summary>
    public const string PDK_HEMA = "pdk/hema.mp3";
    /// <summary>pdk/jiefeng.mp3</summary>
    public const string PDK_JIEFENG = "pdk/jiefeng.mp3";
    /// <summary>pdk/liandui.mp3</summary>
    public const string PDK_LIANDUI = "pdk/liandui.mp3";
    /// <summary>pdk/lianzha.mp3</summary>
    public const string PDK_LIANZHA = "pdk/lianzha.mp3";
    /// <summary>pdk/Lost.mp3</summary>
    public const string PDK_LOST = "pdk/Lost.mp3";
    /// <summary>pdk/plane.mp3</summary>
    public const string PDK_PLANE = "pdk/plane.mp3";
    /// <summary>pdk/ready.mp3</summary>
    public const string PDK_READY = "pdk/ready.mp3";
    /// <summary>pdk/rocket.mp3</summary>
    public const string PDK_ROCKET = "pdk/rocket.mp3";
    /// <summary>pdk/SEND_CARD0.mp3</summary>
    public const string PDK_SEND_CARD0 = "pdk/SEND_CARD0.mp3";
    /// <summary>pdk/shunzi.mp3</summary>
    public const string PDK_SHUNZI = "pdk/shunzi.mp3";
    /// <summary>pdk/so.mp3</summary>
    public const string PDK_SO = "pdk/so.mp3";
    /// <summary>pdk/Warning.mp3</summary>
    public const string PDK_WARNING = "pdk/Warning.mp3";
    /// <summary>pdk/Win.mp3</summary>
    public const string PDK_WIN = "pdk/Win.mp3";
    /// <summary>pdk/yx_feiji.mp3</summary>
    public const string PDK_YX_FEIJI = "pdk/yx_feiji.mp3";

    #endregion

    #region pdk/pt/man/card

    /// <summary>pdk/pt/man/card/10.mp3</summary>
    public const string PDK_PT_MAN_CARD_10 = "pdk/pt/man/card/10.mp3";
    /// <summary>pdk/pt/man/card/1010.mp3</summary>
    public const string PDK_PT_MAN_CARD_1010 = "pdk/pt/man/card/1010.mp3";
    /// <summary>pdk/pt/man/card/2.mp3</summary>
    public const string PDK_PT_MAN_CARD_2 = "pdk/pt/man/card/2.mp3";
    /// <summary>pdk/pt/man/card/22.mp3</summary>
    public const string PDK_PT_MAN_CARD_22 = "pdk/pt/man/card/22.mp3";
    /// <summary>pdk/pt/man/card/3.mp3</summary>
    public const string PDK_PT_MAN_CARD_3 = "pdk/pt/man/card/3.mp3";
    /// <summary>pdk/pt/man/card/33.mp3</summary>
    public const string PDK_PT_MAN_CARD_33 = "pdk/pt/man/card/33.mp3";
    /// <summary>pdk/pt/man/card/4.mp3</summary>
    public const string PDK_PT_MAN_CARD_4 = "pdk/pt/man/card/4.mp3";
    /// <summary>pdk/pt/man/card/44.mp3</summary>
    public const string PDK_PT_MAN_CARD_44 = "pdk/pt/man/card/44.mp3";
    /// <summary>pdk/pt/man/card/5.mp3</summary>
    public const string PDK_PT_MAN_CARD_5 = "pdk/pt/man/card/5.mp3";
    /// <summary>pdk/pt/man/card/55.mp3</summary>
    public const string PDK_PT_MAN_CARD_55 = "pdk/pt/man/card/55.mp3";
    /// <summary>pdk/pt/man/card/6.mp3</summary>
    public const string PDK_PT_MAN_CARD_6 = "pdk/pt/man/card/6.mp3";
    /// <summary>pdk/pt/man/card/66.mp3</summary>
    public const string PDK_PT_MAN_CARD_66 = "pdk/pt/man/card/66.mp3";
    /// <summary>pdk/pt/man/card/7.mp3</summary>
    public const string PDK_PT_MAN_CARD_7 = "pdk/pt/man/card/7.mp3";
    /// <summary>pdk/pt/man/card/77.mp3</summary>
    public const string PDK_PT_MAN_CARD_77 = "pdk/pt/man/card/77.mp3";
    /// <summary>pdk/pt/man/card/8.mp3</summary>
    public const string PDK_PT_MAN_CARD_8 = "pdk/pt/man/card/8.mp3";
    /// <summary>pdk/pt/man/card/88.mp3</summary>
    public const string PDK_PT_MAN_CARD_88 = "pdk/pt/man/card/88.mp3";
    /// <summary>pdk/pt/man/card/9.mp3</summary>
    public const string PDK_PT_MAN_CARD_9 = "pdk/pt/man/card/9.mp3";
    /// <summary>pdk/pt/man/card/99.mp3</summary>
    public const string PDK_PT_MAN_CARD_99 = "pdk/pt/man/card/99.mp3";
    /// <summary>pdk/pt/man/card/A.mp3</summary>
    public const string PDK_PT_MAN_CARD_A = "pdk/pt/man/card/A.mp3";
    /// <summary>pdk/pt/man/card/AA.mp3</summary>
    public const string PDK_PT_MAN_CARD_AA = "pdk/pt/man/card/AA.mp3";
    /// <summary>pdk/pt/man/card/feiji.mp3</summary>
    public const string PDK_PT_MAN_CARD_FEIJI = "pdk/pt/man/card/feiji.mp3";
    /// <summary>pdk/pt/man/card/J.mp3</summary>
    public const string PDK_PT_MAN_CARD_J = "pdk/pt/man/card/J.mp3";
    /// <summary>pdk/pt/man/card/JJ.mp3</summary>
    public const string PDK_PT_MAN_CARD_JJ = "pdk/pt/man/card/JJ.mp3";
    /// <summary>pdk/pt/man/card/K.mp3</summary>
    public const string PDK_PT_MAN_CARD_K = "pdk/pt/man/card/K.mp3";
    /// <summary>pdk/pt/man/card/KK.mp3</summary>
    public const string PDK_PT_MAN_CARD_KK = "pdk/pt/man/card/KK.mp3";
    /// <summary>pdk/pt/man/card/liandui.mp3</summary>
    public const string PDK_PT_MAN_CARD_LIANDUI = "pdk/pt/man/card/liandui.mp3";
    /// <summary>pdk/pt/man/card/Q.mp3</summary>
    public const string PDK_PT_MAN_CARD_Q = "pdk/pt/man/card/Q.mp3";
    /// <summary>pdk/pt/man/card/QQ.mp3</summary>
    public const string PDK_PT_MAN_CARD_QQ = "pdk/pt/man/card/QQ.mp3";
    /// <summary>pdk/pt/man/card/sandaier.mp3</summary>
    public const string PDK_PT_MAN_CARD_SANDAIER = "pdk/pt/man/card/sandaier.mp3";
    /// <summary>pdk/pt/man/card/sandaiyi.mp3</summary>
    public const string PDK_PT_MAN_CARD_SANDAIYI = "pdk/pt/man/card/sandaiyi.mp3";
    /// <summary>pdk/pt/man/card/sanzhang.mp3</summary>
    public const string PDK_PT_MAN_CARD_SANZHANG = "pdk/pt/man/card/sanzhang.mp3";
    /// <summary>pdk/pt/man/card/shunzi.mp3</summary>
    public const string PDK_PT_MAN_CARD_SHUNZI = "pdk/pt/man/card/shunzi.mp3";
    /// <summary>pdk/pt/man/card/zhadan.mp3</summary>
    public const string PDK_PT_MAN_CARD_ZHADAN = "pdk/pt/man/card/zhadan.mp3";

    #endregion

    #region pdk/pt/man/control

    /// <summary>pdk/pt/man/control/baojing.mp3</summary>
    public const string PDK_PT_MAN_CONTROL_BAOJING = "pdk/pt/man/control/baojing.mp3";
    /// <summary>pdk/pt/man/control/buyao.mp3</summary>
    public const string PDK_PT_MAN_CONTROL_BUYAO = "pdk/pt/man/control/buyao.mp3";
    /// <summary>pdk/pt/man/control/dani.mp3</summary>
    public const string PDK_PT_MAN_CONTROL_DANI = "pdk/pt/man/control/dani.mp3";
    /// <summary>pdk/pt/man/control/win.mp3</summary>
    public const string PDK_PT_MAN_CONTROL_WIN = "pdk/pt/man/control/win.mp3";

    #endregion

    #region pdk/pt/man/qiaopihua

    /// <summary>pdk/pt/man/qiaopihua/01.mp3</summary>
    public const string PDK_PT_MAN_QIAOPIHUA_01 = "pdk/pt/man/qiaopihua/01.mp3";
    /// <summary>pdk/pt/man/qiaopihua/02.mp3</summary>
    public const string PDK_PT_MAN_QIAOPIHUA_02 = "pdk/pt/man/qiaopihua/02.mp3";
    /// <summary>pdk/pt/man/qiaopihua/03.mp3</summary>
    public const string PDK_PT_MAN_QIAOPIHUA_03 = "pdk/pt/man/qiaopihua/03.mp3";
    /// <summary>pdk/pt/man/qiaopihua/04.mp3</summary>
    public const string PDK_PT_MAN_QIAOPIHUA_04 = "pdk/pt/man/qiaopihua/04.mp3";
    /// <summary>pdk/pt/man/qiaopihua/05.mp3</summary>
    public const string PDK_PT_MAN_QIAOPIHUA_05 = "pdk/pt/man/qiaopihua/05.mp3";
    /// <summary>pdk/pt/man/qiaopihua/06.mp3</summary>
    public const string PDK_PT_MAN_QIAOPIHUA_06 = "pdk/pt/man/qiaopihua/06.mp3";
    /// <summary>pdk/pt/man/qiaopihua/07.mp3</summary>
    public const string PDK_PT_MAN_QIAOPIHUA_07 = "pdk/pt/man/qiaopihua/07.mp3";
    /// <summary>pdk/pt/man/qiaopihua/08.mp3</summary>
    public const string PDK_PT_MAN_QIAOPIHUA_08 = "pdk/pt/man/qiaopihua/08.mp3";

    #endregion

    #region pdk/pt/woman/card

    /// <summary>pdk/pt/woman/card/10.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_10 = "pdk/pt/woman/card/10.mp3";
    /// <summary>pdk/pt/woman/card/1010.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_1010 = "pdk/pt/woman/card/1010.mp3";
    /// <summary>pdk/pt/woman/card/2.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_2 = "pdk/pt/woman/card/2.mp3";
    /// <summary>pdk/pt/woman/card/22.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_22 = "pdk/pt/woman/card/22.mp3";
    /// <summary>pdk/pt/woman/card/3.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_3 = "pdk/pt/woman/card/3.mp3";
    /// <summary>pdk/pt/woman/card/33.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_33 = "pdk/pt/woman/card/33.mp3";
    /// <summary>pdk/pt/woman/card/4.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_4 = "pdk/pt/woman/card/4.mp3";
    /// <summary>pdk/pt/woman/card/44.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_44 = "pdk/pt/woman/card/44.mp3";
    /// <summary>pdk/pt/woman/card/5.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_5 = "pdk/pt/woman/card/5.mp3";
    /// <summary>pdk/pt/woman/card/55.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_55 = "pdk/pt/woman/card/55.mp3";
    /// <summary>pdk/pt/woman/card/6.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_6 = "pdk/pt/woman/card/6.mp3";
    /// <summary>pdk/pt/woman/card/66.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_66 = "pdk/pt/woman/card/66.mp3";
    /// <summary>pdk/pt/woman/card/7.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_7 = "pdk/pt/woman/card/7.mp3";
    /// <summary>pdk/pt/woman/card/77.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_77 = "pdk/pt/woman/card/77.mp3";
    /// <summary>pdk/pt/woman/card/8.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_8 = "pdk/pt/woman/card/8.mp3";
    /// <summary>pdk/pt/woman/card/88.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_88 = "pdk/pt/woman/card/88.mp3";
    /// <summary>pdk/pt/woman/card/9.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_9 = "pdk/pt/woman/card/9.mp3";
    /// <summary>pdk/pt/woman/card/99.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_99 = "pdk/pt/woman/card/99.mp3";
    /// <summary>pdk/pt/woman/card/A.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_A = "pdk/pt/woman/card/A.mp3";
    /// <summary>pdk/pt/woman/card/AA.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_AA = "pdk/pt/woman/card/AA.mp3";
    /// <summary>pdk/pt/woman/card/feiji.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_FEIJI = "pdk/pt/woman/card/feiji.mp3";
    /// <summary>pdk/pt/woman/card/J.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_J = "pdk/pt/woman/card/J.mp3";
    /// <summary>pdk/pt/woman/card/JJ.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_JJ = "pdk/pt/woman/card/JJ.mp3";
    /// <summary>pdk/pt/woman/card/K.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_K = "pdk/pt/woman/card/K.mp3";
    /// <summary>pdk/pt/woman/card/KK.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_KK = "pdk/pt/woman/card/KK.mp3";
    /// <summary>pdk/pt/woman/card/liandui.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_LIANDUI = "pdk/pt/woman/card/liandui.mp3";
    /// <summary>pdk/pt/woman/card/Q.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_Q = "pdk/pt/woman/card/Q.mp3";
    /// <summary>pdk/pt/woman/card/QQ.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_QQ = "pdk/pt/woman/card/QQ.mp3";
    /// <summary>pdk/pt/woman/card/sandaier.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_SANDAIER = "pdk/pt/woman/card/sandaier.mp3";
    /// <summary>pdk/pt/woman/card/sandaieyi.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_SANDAIEYI = "pdk/pt/woman/card/sandaieyi.mp3";
    /// <summary>pdk/pt/woman/card/sanzhang.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_SANZHANG = "pdk/pt/woman/card/sanzhang.mp3";
    /// <summary>pdk/pt/woman/card/shunzi.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_SHUNZI = "pdk/pt/woman/card/shunzi.mp3";
    /// <summary>pdk/pt/woman/card/zhadan.mp3</summary>
    public const string PDK_PT_WOMAN_CARD_ZHADAN = "pdk/pt/woman/card/zhadan.mp3";

    #endregion

    #region pdk/pt/woman/control

    /// <summary>pdk/pt/woman/control/baojing.mp3</summary>
    public const string PDK_PT_WOMAN_CONTROL_BAOJING = "pdk/pt/woman/control/baojing.mp3";
    /// <summary>pdk/pt/woman/control/buyao.mp3</summary>
    public const string PDK_PT_WOMAN_CONTROL_BUYAO = "pdk/pt/woman/control/buyao.mp3";
    /// <summary>pdk/pt/woman/control/dani.mp3</summary>
    public const string PDK_PT_WOMAN_CONTROL_DANI = "pdk/pt/woman/control/dani.mp3";
    /// <summary>pdk/pt/woman/control/win.mp3</summary>
    public const string PDK_PT_WOMAN_CONTROL_WIN = "pdk/pt/woman/control/win.mp3";

    #endregion

    #region pdk/pt/woman/qiaopihua

    /// <summary>pdk/pt/woman/qiaopihua/01.mp3</summary>
    public const string PDK_PT_WOMAN_QIAOPIHUA_01 = "pdk/pt/woman/qiaopihua/01.mp3";
    /// <summary>pdk/pt/woman/qiaopihua/02.mp3</summary>
    public const string PDK_PT_WOMAN_QIAOPIHUA_02 = "pdk/pt/woman/qiaopihua/02.mp3";
    /// <summary>pdk/pt/woman/qiaopihua/03.mp3</summary>
    public const string PDK_PT_WOMAN_QIAOPIHUA_03 = "pdk/pt/woman/qiaopihua/03.mp3";
    /// <summary>pdk/pt/woman/qiaopihua/04.mp3</summary>
    public const string PDK_PT_WOMAN_QIAOPIHUA_04 = "pdk/pt/woman/qiaopihua/04.mp3";
    /// <summary>pdk/pt/woman/qiaopihua/05.mp3</summary>
    public const string PDK_PT_WOMAN_QIAOPIHUA_05 = "pdk/pt/woman/qiaopihua/05.mp3";
    /// <summary>pdk/pt/woman/qiaopihua/06.mp3</summary>
    public const string PDK_PT_WOMAN_QIAOPIHUA_06 = "pdk/pt/woman/qiaopihua/06.mp3";
    /// <summary>pdk/pt/woman/qiaopihua/07.mp3</summary>
    public const string PDK_PT_WOMAN_QIAOPIHUA_07 = "pdk/pt/woman/qiaopihua/07.mp3";
    /// <summary>pdk/pt/woman/qiaopihua/08.mp3</summary>
    public const string PDK_PT_WOMAN_QIAOPIHUA_08 = "pdk/pt/woman/qiaopihua/08.mp3";

    #endregion

    #region zjh

    /// <summary>zjh/opencard.wav</summary>
    public const string ZJH_OPENCARD = "zjh/opencard.wav";
    /// <summary>zjh/opencardOther.mp3</summary>
    public const string ZJH_OPENCARDOTHER = "zjh/opencardOther.mp3";
    /// <summary>zjh/pk8_arrow_8kp.mp3</summary>
    public const string ZJH_PK8_ARROW_8KP = "zjh/pk8_arrow_8kp.mp3";
    /// <summary>zjh/pk8_jss_baozi_8kp.mp3</summary>
    public const string ZJH_PK8_JSS_BAOZI_8KP = "zjh/pk8_jss_baozi_8kp.mp3";
    /// <summary>zjh/pk8_jss_compare_8kp.mp3</summary>
    public const string ZJH_PK8_JSS_COMPARE_8KP = "zjh/pk8_jss_compare_8kp.mp3";
    /// <summary>zjh/pk8_jss_jinhua_8kp.mp3</summary>
    public const string ZJH_PK8_JSS_JINHUA_8KP = "zjh/pk8_jss_jinhua_8kp.mp3";
    /// <summary>zjh/pk8_jss_shunzi_8kp.mp3</summary>
    public const string ZJH_PK8_JSS_SHUNZI_8KP = "zjh/pk8_jss_shunzi_8kp.mp3";
    /// <summary>zjh/pk8_jss_tonghuashun_8kp.mp3</summary>
    public const string ZJH_PK8_JSS_TONGHUASHUN_8KP = "zjh/pk8_jss_tonghuashun_8kp.mp3";
    /// <summary>zjh/pk8_myturn_8kp.mp3</summary>
    public const string ZJH_PK8_MYTURN_8KP = "zjh/pk8_myturn_8kp.mp3";
    /// <summary>zjh/pk8_paipai_8kp.mp3</summary>
    public const string ZJH_PK8_PAIPAI_8KP = "zjh/pk8_paipai_8kp.mp3";
    /// <summary>zjh/pk8_shield_8kp.mp3</summary>
    public const string ZJH_PK8_SHIELD_8KP = "zjh/pk8_shield_8kp.mp3";
    /// <summary>zjh/pk8_timeout_quick_8kp.mp3</summary>
    public const string ZJH_PK8_TIMEOUT_QUICK_8KP = "zjh/pk8_timeout_quick_8kp.mp3";
    /// <summary>zjh/pk8_timeout_slow_8kp.mp3</summary>
    public const string ZJH_PK8_TIMEOUT_SLOW_8KP = "zjh/pk8_timeout_slow_8kp.mp3";
    /// <summary>zjh/pk8_win_8kp.mp3</summary>
    public const string ZJH_PK8_WIN_8KP = "zjh/pk8_win_8kp.mp3";

    #endregion
}

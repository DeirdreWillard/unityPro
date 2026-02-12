using System.Collections.Generic;
using NetMsg;
using UnityEngine;
using static UtilityBuiltin;

public class GameUtil
{
    private static GameUtil instance;
    private GameUtil() { }

    public static GameUtil GetInstance()
    {
        if (instance == null)
        {
            instance = new GameUtil();
        }
        return instance;
    }

    public Position GetSeatPosByNum(string i)
    {
        Position position = Position.Default;
        switch (i)
        {
            case "0":
                position = Position.Default;
                break;
            case "1":
                position = Position.One;
                break;
            case "2":
                position = Position.Two;
                break;
            case "3":
                position = Position.Three;
                break;
            case "4":
                position = Position.Four;
                break;
            case "5":
                position = Position.Five;
                break;
            case "6":
                position = Position.Six;
                break;
            case "7":
                position = Position.Seven;
                break;
            case "8":
                position = Position.Eight;
                break;
            case "9":
                position = Position.Nine;
                break;
        }

        return position;
    }
    public int GetSeatIdByPos(Position pos)
    {
        int temp = -1;
        switch (pos)
        {
            case Position.Default:
                temp = 0;
                break;
            case Position.One:
                temp = 1;
                break;
            case Position.Two:
                temp = 2;
                break;
            case Position.Three:
                temp = 3;
                break;
            case Position.Four:
                temp = 4;
                break;
            case Position.Five:
                temp = 5;
                break;
            case Position.Six:
                temp = 6;
                break;
            case Position.Seven:
                temp = 7;
                break;
            case Position.Eight:
                temp = 8;
                break;
            case Position.Nine:
                temp = 9;
                break;
        }
        return temp;
    }

    public int CombineColorAndValue(int color, int value)
    {
        // 将花色左移 4 位，并将牌值与之合并
        return (color << 4) | (value & 0x0f);
    }

    /// <summary>
    /// 花色
    /// </summary>
    public int GetColor(int Val)
    {
        return (Val & 0xf0) >> 4;
    }

    /// <summary>
    /// 牌值
    /// </summary>
    public int GetValue(int Val)
    {
        return Val & 0x0f;
    }

    public static Quaternion GetQuaternion(Vector3 original, Vector3 target)
    {
        Vector3 direction = target - original;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90;
        return Quaternion.AngleAxis(angle, Vector3.forward);
    }

    public static string GetGameIconByMethodType(MethodType methodType)
    {
        string gameIconPath = "niuniu";
        switch (methodType)
        {
            case MethodType.NiuNiu:
                gameIconPath = "niuniu";
                break;
            case MethodType.GoldenFlower:
                gameIconPath = "jinhua";
                break;
            case MethodType.TexasPoker:
                gameIconPath = "dezhou";
                break;
            case MethodType.CompareChicken:
                gameIconPath = "biji";
                break;
            default:
                break;
        }
        string typepath = string.Format("Img/房间列表资源/{0}.png", gameIconPath);
        // GF.LogInfo(typepath);
        return typepath;
    }

    /// <summary>
    /// 显示牌型
    /// </summary>
    public static string ShowType(int n, MethodType methodType)
    {
        var s = "";
        if (methodType == MethodType.NiuNiu)
        {
            switch (n)
            {
                case 0:
                    s = "没牛";
                    break;
                case 1:
                    s = "牛一";
                    break;
                case 2:
                    s = "牛二";
                    break;
                case 3:
                    s = "牛三";
                    break;
                case 4:
                    s = "牛四";
                    break;
                case 5:
                    s = "牛五";
                    break;
                case 6:
                    s = "牛六";
                    break;
                case 7:
                    s = "牛七";
                    break;
                case 8:
                    s = "牛八";
                    break;
                case 9:
                    s = "牛九";
                    break;
                case 10:
                    s = "牛牛";
                    break;
                case 11:
                    s = "顺子牛";
                    break;
                case 12:
                    s = "五花牛";
                    break;
                case 13:
                    s = "同花牛";
                    break;
                case 14:
                    s = "葫芦牛";
                    break;
                case 15:
                    s = "炸弹牛";
                    break;
                case 16:
                    s = "五小牛";
                    break;
                case 17:
                    s = "同花顺牛";
                    break;
            }
        }
        else if (methodType == MethodType.GoldenFlower)
        {
            switch (n)
            {
                case 0:
                    s = "高牌";
                    break;
                case 1:
                    s = "对子";
                    break;
                case 2:
                    s = "顺子";
                    break;
                case 3:
                    s = "金花";
                    break;
                case 4:
                    s = "顺金";
                    break;
                case 5:
                    s = "豹子";
                    break;
                case 6:
                    s = "235";
                    break;
            }
        }
        else if (methodType == MethodType.TexasPoker)
        {
            switch (n)
            {
                case 0:
                    s = "高牌";
                    break;
                case 1:
                    s = "对子";
                    break;
                case 2:
                    s = "两对";
                    break;
                case 3:
                    s = "三条";
                    break;
                case 4:
                    s = "顺子";
                    break;
                case 5:
                    s = "同花";
                    break;
                case 6:
                    s = "葫芦";
                    break;
                case 7:
                    s = "四条";
                    break;
                case 8:
                    s = "同花顺";
                    break;
                case 9:
                    s = "皇家同花顺";
                    break;
            }
        }
        else if (methodType == MethodType.CompareChicken)
        {
            //1三清 2全红 3全黑 4双顺清 5三顺清 6双三条 7全三条 8通关 9连顺 10清连顺 11三鬼 12头道清 13四个头 14双四头
            switch (n)
            {
                case 0:
                    s = "三清";
                    break;
                case 1:
                    s = "三清";
                    break;
                case 2:
                    s = "全红";
                    break;
                case 3:
                    s = "全黑";
                    break;
                case 4:
                    s = "双顺清";
                    break;
                case 5:
                    s = "三顺清";
                    break;
                case 6:
                    s = "双三条";
                    break;
                case 7:
                    s = "全三条";
                    break;
                case 8:
                    s = "通关";
                    break;
                case 9:
                    s = "连顺";
                    break;
                case 10:
                    s = "清连顺";
                    break;
                case 11:
                    s = "三鬼";
                    break;
                case 12:
                    s = "头道清";
                    break;
                case 13:
                    s = "四个头";
                    break;
                case 14:
                    s = "双四头";
                    break;
            }
        }
        return s;
    }

    /// <summary>
    /// 通过牌型获得展示牌型数据
    /// </summary>
    public static List<string> GetCardsByType(int n, MethodType methodType)
    {
        List<string> cards = new();
        //0方片 1梅花 2红桃 3黑桃
        if (methodType == MethodType.NiuNiu)
        {
            switch (n)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                    break;
                case 10:
                    cards.Add("011");
                    cards.Add("110");
                    cards.Add("35");
                    cards.Add("17");
                    cards.Add("28");
                    break;
                case 11:
                    cards.Add("36");
                    cards.Add("17");
                    cards.Add("08");
                    cards.Add("39");
                    cards.Add("210");
                    break;
                case 12:
                    cards.Add("113");
                    cards.Add("312");
                    cards.Add("212");
                    cards.Add("011");
                    cards.Add("311");
                    break;
                case 13:
                    cards.Add("35");
                    cards.Add("311");
                    cards.Add("313");
                    cards.Add("37");
                    cards.Add("32");
                    break;
                case 14:
                    cards.Add("313");
                    cards.Add("113");
                    cards.Add("213");
                    cards.Add("19");
                    cards.Add("29");
                    break;
                case 15:
                    cards.Add("310");
                    cards.Add("210");
                    cards.Add("010");
                    cards.Add("110");
                    cards.Add("011");
                    break;
                case 16:
                    cards.Add("01");
                    cards.Add("21");
                    cards.Add("02");
                    cards.Add("32");
                    cards.Add("13");
                    break;
                case 17:
                    cards.Add("09");
                    cards.Add("010");
                    cards.Add("011");
                    cards.Add("012");
                    cards.Add("013");
                    break;
            }
        }
        else if (methodType == MethodType.GoldenFlower)
        {
            switch (n)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                    cards.Add("05");
                    cards.Add("04");
                    cards.Add("03");
                    break;
                case 5:
                    cards.Add("212");
                    cards.Add("012");
                    cards.Add("112");
                    break;
                case 6:
                    cards.Add("22");
                    cards.Add("13");
                    cards.Add("15");
                    break;
            }
        }
        else if (methodType == MethodType.TexasPoker)
        {
            switch (n)
            {
                case 0:
                    break;
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
                case 6:
                    break;
                case 7:
                    cards.Add("111");
                    cards.Add("311");
                    cards.Add("211");
                    cards.Add("011");
                    cards.Add("28");
                    break;
                case 8:
                    cards.Add("16");
                    cards.Add("17");
                    cards.Add("18");
                    cards.Add("19");
                    cards.Add("110");
                    break;
                case 9:
                    cards.Add("010");
                    cards.Add("011");
                    cards.Add("012");
                    cards.Add("013");
                    cards.Add("01");
                    break;
            }
        }
        else if (methodType == MethodType.CompareChicken)
        {
            switch (n)
            {
                case 6:
                    cards.Add("05");
                    cards.Add("15");
                    cards.Add("25");
                    cards.Add("07");
                    cards.Add("17");
                    cards.Add("27");
                    cards.Add("02");
                    cards.Add("16");
                    cards.Add("28");
                    break;
                case 13:
                    cards.Add("05");
                    cards.Add("15");
                    cards.Add("25");
                    cards.Add("35");
                    cards.Add("19");
                    cards.Add("28");
                    cards.Add("29");
                    cards.Add("37");
                    cards.Add("03");
                    break;
                case 5:
                    cards.Add("03");
                    cards.Add("04");
                    cards.Add("05");
                    cards.Add("12");
                    cards.Add("13");
                    cards.Add("14");
                    cards.Add("310");
                    cards.Add("311");
                    cards.Add("312");
                    break;
                case 10:
                    cards.Add("12");
                    cards.Add("13");
                    cards.Add("14");
                    cards.Add("15");
                    cards.Add("16");
                    cards.Add("17");
                    cards.Add("18");
                    cards.Add("19");
                    cards.Add("110");
                    break;
                case 7:
                    cards.Add("05");
                    cards.Add("15");
                    cards.Add("25");
                    cards.Add("07");
                    cards.Add("17");
                    cards.Add("27");
                    cards.Add("012");
                    cards.Add("112");
                    cards.Add("212");
                    break;
                case 14:
                    cards.Add("05");
                    cards.Add("15");
                    cards.Add("25");
                    cards.Add("07");
                    cards.Add("17");
                    cards.Add("27");
                    cards.Add("35");
                    cards.Add("37");
                    cards.Add("03");
                    break;
            }
        }
        return cards;
    }

    /// <summary>
    /// 根据服务器番型编号获取番型名称（卡五星专用）
    /// </summary>
    /// <param name="fanCode">服务器返回的番型编号</param>
    /// <returns>番型中文名称</returns>
    public static string GetFanTypeName(long fanCode)
    {
        switch (fanCode)
        {
            // ===== 基础操作（非番型，用于特效显示）=====
            case 101: return "碰";
            case 102: return "吃";
            case 103: return "杠";
            case 104: return "自摸";
            case 105: return "点炮";
            case 106: return "杠上开花";
            
            // ===== 胡牌类型 =====
            case 1: return "屁胡";                    // Hu
            case 1539: return "碰碰胡";               // HuType_PengPengHu
            case 6150: return "清一色";               // HuType_QingYiSe
            case 6148: return "七对";                 // HuType_QiDui
            case 16386: return "小三元";              // HuType_XiaoSanYuan
            case 22530: return "大三元";              // HuType_DaSanYuan
            case 22785: return "明四归";              // HuType_MingSiGui
            case 22786: return "暗四归";              // HuType_AnSiGui
            case 22787: return "卡五星";              // HuType_KaWuXing
            case 22788: return "手抓一";              // HuType_ShouZhuaYi
            case 22789: return "豪华七对";            // HuType_HaoHuaQiDui
            case 6: return "超豪华七对";              // HuType_ChaoHaoHuaQiDui
            case 7: return "超超豪华七对";            // HuType_ChaoChaoHaoHuaQiDui
            case 14: return "三元七对";               // HuType_SanYuanQiDui
            case 15: return "杠上炮";                 // HuType_GangShangPao
            case 16: return "抢杠胡";                 // HuType_QiangGangHu
            case 17: return "一炮双响";               // HuType_YiPaoShuangXiang
            
            default:
                return null; // 未知番型返回null
        }
    }

    /// <summary>
    /// 判断是否是卡五星番型（用于判断是否显示特效）
    /// </summary>
    public static bool IsKaWuXingFanType(long fanCode)
    {
        // 基础操作特效
        if (fanCode >= 101 && fanCode <= 106) return true;
        
        // 胡牌类型特效
        switch (fanCode)
        {
            case 1:      // 屁胡
            case 1539:   // 碰碰胡
            case 6148:   // 七对
            case 6150:   // 清一色
            case 6:      // 超豪华七对
            case 7:      // 超超豪华七对
            case 14:     // 三元七对
            case 15:     // 杠上炮
            case 16:     // 抢杠胡
            case 17:     // 一炮双响
            case 16386:  // 小三元
            case 22530:  // 大三元
            case 22785:  // 明四归
            case 22786:  // 暗四归
            case 22787:  // 卡五星
            case 22788:  // 手抓一
            case 22789:  // 豪华七对
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// 根据番型编号获取对应的特效名称（卡五星专用）
    /// </summary>
    public static string GetKWXEffectNameByFanCode(long fanCode)
    {
        switch (fanCode)
        {
            // ===== 基础操作特效 =====
            case 101: return "Peng";                     // 碰
            case 102: return "Chi";                      // 吃
            case 103: return "Gang";                     // 杠
            case 104: return "RuanMo";                   // 自摸
            case 105: return "ZhuoChong";                // 点炮
            case 106: return "GangKai";                  // 杠上开花
            
            // ===== 胡牌类型特效 =====
            case 1: return "Hu";                         // 屁胡
            case 1539: return "HuType_PengPengHu";          // 碰碰胡
            case 6150: return "HuType_QingYiSe";            // 清一色
            case 6148: return "HuType_QiDui";               // 七对
            case 16386: return "HuType_XiaoSanYuan";      // 小三元
            case 22530: return "HuType_DaSanYuan";      // 大三元
            case 22785: return "HuType_MingSiGui";      // 明四归
            case 22786: return "HuType_AnSiGui";        // 暗四归
            case 22787: return "HuType_KaWuXing";       // 卡五星
            case 22788: return "HuType_ShouZhuaYi";     // 手抓一
            case 22789: return "HuType_HaoHuaQiDui";    // 豪华七对
            case 6: return "HuType_ChaoHaoHuaQiDui";     // 超豪华七对
            case 7: return "HuType_ChaoChaoHaoHuaQiDui"; // 超超豪华七对
            case 14: return "HuType_SanYuanQiDui";       // 三元七对
            case 15: return "HuType_GangShangPao";       // 杠上炮
            case 16: return "HuType_QiangGangHu";        // 抢杠胡
            case 17: return "HuType_YiPaoShuangXiang";   // 一炮双响

            default: return null;
        }
    }

    /**
            
            // 2番
            case 272: return "报听";
            case 273: return "二五八将";
            case 274: return "幺九刻";
            // ===== 基础操作（非番型，用于特效显示）=====
            case 101: return "碰";
            case 102: return "吃";
            case 103: return "杠";
            case 104: return "自摸";
            case 105: return "点炮";
            case 106: return "杠上开花";
            
            // 4番
            case 513: return "门风刻";
            case 514: return "圈风刻";
            case 515: return "箭刻";
            case 516: return "平和";
            case 517: return "四归一";
            case 518: return "断幺";
            case 519: return "双暗刻";
            case 520: return "暗杠";
            case 521: return "门前清";
            
            // 8番
            case 1025: return "全带幺";
            case 1026: return "双明杠";
            case 1027: return "不求人";
            case 1028: return "和绝张";
            case 1031: return "立直";
            
            // 12番
            case 1538: return "双箭刻";
            case 1539: return "碰碰和";
            case 1540: return "双暗杠";
            case 1541: return "混一色";
            case 1542: return "全求人";
            
            // 16番
            case 2049: return "妙手回春";
            case 2050: return "海底捞月";
            case 2051: return "杠上开花";
            case 2052: return "抢杠和";
            
            // 24番
            case 3073: return "大于5";
            case 3074: return "小于5";
            case 3075: return "三风刻";
            
            // 32番
            case 4097: return "清龙";
            case 4098: return "三步高";
            case 4100: return "三暗刻";
            case 4102: return "天听";
            
            // 48番
            case 6147: return "三同顺";
            case 6148: return "七对子";
            case 6149: return "三连刻";
            case 6150: return "清一色";
            
            // 64番
            case 8193: return "四步高";
            case 8194: return "混幺九";
            case 8195: return "三杠";
            
            // 96番
            case 12289: return "四同顺";
            case 12292: return "四连刻";
            
            // 128番
            case 16385: return "小四喜";
            case 16386: return "小三元";
            case 16387: return "四暗刻";
            case 16388: return "双龙会";
            case 16389: return "字一色";
            
            // 176番（满贯）
            case 22529: return "大四喜";
            case 22530: return "大三元";
            case 22531: return "九莲宝灯";
            case 22535: return "连七对";
            case 22536: return "天和";
            case 22537: return "地和";
            case 22538: return "四杠";
            case 22539: return "百万石";
            case 22540: return "人和";
            case 22541: return "十三幺";
            
            // 卡五星特有番型
            case 22785: return "明四归";
            case 22786: return "暗四归";
            case 22787: return "卡五星";
            case 22788: return "手抓一";
            case 22789: return "豪华七对";
    **/

    /// <summary>
    /// 根据特效名称获取对应的Spine动画名称（卡五星专用）
    /// </summary>
    public static string GetKWXAnimationName(string effectType)
    {
        switch (effectType)
        {
            // ===== 基础操作特效 =====
            case "Peng": return "peng";
            case "Chi": return "peng";                   // 吃牌使用碰牌动画
            case "Gang": return "gang";
            case "ZiMo": return "hu_zimo";
            case "DianPao": return "dianpao";
            case "GangKai": return "hu_gangshangkaihua";
            
            // ===== 胡牌类型特效 =====
            case "Hu" : return "hu";
            case "HuType_PengPengHu": return "hu_pengpenghu";
            case "HuType_QingYiSe": return "hu_qingyise";
            case "HuType_QiDui": return "hu_qidui";
            case "HuType_HaoHuaQiDui": return "hu_haohuaqidui";
            case "HuType_ChaoHaoHuaQiDui": return "hu_chaohaohuaqidui";
            case "HuType_ChaoChaoHaoHuaQiDui": return "hu_chaochaohaohuaqidui";
            case "HuType_KaWuXing": return "hu_kawuxing";
            case "HuType_MingSiGui": return "hu_mingsigui";
            case "HuType_AnSiGui": return "hu_ansigui";
            case "HuType_ShouZhuaYi": return "hu_shouzhuayi";
            case "HuType_XiaoSanYuan": return "hu_xiaosanyuan";
            case "HuType_DaSanYuan": return "hu_dasanyuan";
            case "HuType_SanYuanQiDui": return "hu_sanyuanaqidui";
            case "HuType_GangShangPao": return "hu_gangshangpao";
            case "HuType_QiangGangHu": return "hu_qiangganghu";
            case "HuType_YiPaoShuangXiang": return "hu_yipaoshuangxiang";
            case "HuType_HuangZhuang": return "hu_huangzhuang";
            
            default: return null;
        }
    }

}

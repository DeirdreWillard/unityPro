using System.Collections.Generic;

/// <summary>
/// 麻将核心类型定义 - 2D独立使用
/// 从 MahjongMachineNS 中提取出2D需要的核心类型，隔离3D依赖
/// </summary>

#region 麻将牌相关枚举

/// <summary>
/// 麻将面值
/// </summary>
public enum MahjongFaceValue
{
    MJ_WANG_1,
    MJ_WANG_2,
    MJ_WANG_3,
    MJ_WANG_4,
    MJ_WANG_5,
    MJ_WANG_6,
    MJ_WANG_7,
    MJ_WANG_8,
    MJ_WANG_9,
    MJ_TONG_1,
    MJ_TONG_2,
    MJ_TONG_3,
    MJ_TONG_4,
    MJ_TONG_5,
    MJ_TONG_6,
    MJ_TONG_7,
    MJ_TONG_8,
    MJ_TONG_9,
    MJ_TIAO_1,
    MJ_TIAO_2,
    MJ_TIAO_3,
    MJ_TIAO_4,
    MJ_TIAO_5,
    MJ_TIAO_6,
    MJ_TIAO_7,
    MJ_TIAO_8,
    MJ_TIAO_9,
    MJ_FENG_DONG,
    MJ_FENG_NAN,
    MJ_FENG_XI,
    MJ_FENG_BEI,
    MJ_ZFB_HONGZHONG,
    MJ_ZFB_FACAI,
    MJ_ZFB_BAIBAN,
    MJ_HUA_CHUN,
    MJ_HUA_XIA,
    MJ_HUA_QIU,
    MJ_HUA_DONG,
    MJ_HUA_MEI,
    MJ_HUA_LAN,
    MJ_HUA_ZHU,
    MJ_HUA_JU,
    MJ_UNKNOWN,
}

/// <summary>
/// 麻将花色
/// </summary>
public enum MahjongHuaSe
{
    /// <summary>万</summary>
    WAN = 0,
    /// <summary>筒</summary>
    TONG,
    /// <summary>条</summary>
    TIAO,
    /// <summary>风、字 牌</summary>
    FENG,
    Max,
}

/// <summary>
/// 风位
/// </summary>
public enum FengWei
{
    /// <summary>东</summary>
    EAST,
    /// <summary>南</summary>
    SOUTH,
    /// <summary>西</summary>
    WEST,
    /// <summary>北</summary>
    NORTH
}

/// <summary>
/// 手牌类型
/// </summary>
public enum HandPaiType
{
    /// <summary>手牌</summary>
    HandPai,
    /// <summary>摸牌</summary>
    MoPai,
}

/// <summary>
/// 碰吃杠听胡类型
/// </summary>
public enum PengChiGangTingHuType
{
    /// <summary>
    /// 碰
    /// </summary>
    PENG,

    /// <summary>
    /// 胡
    /// </summary>
    HU,

    /// <summary>
    /// 吃
    /// </summary>
    CHI,

    /// <summary>
    /// 杠
    /// </summary>
    GANG,

    /// <summary>
    /// 笑
    /// </summary>
    XIAO,

    /// <summary>
    /// 听
    /// </summary>
    TING,

    /// <summary>
    /// 过
    /// </summary>
    GUO,

    /// <summary>
    /// 取消
    /// </summary>
    CANCEL,


}

/// <summary>
/// 碰吃杠牌类型（用于UI显示）
/// </summary>
public enum PengChiGangPaiType
{
    /// <summary>碰牌</summary>
    PENG,
    /// <summary>吃牌</summary>
    CHI,
    /// <summary>明杠</summary>
    GANG,
    /// <summary>暗杠</summary>
    AN_GANG,
    /// <summary>补杠</summary>
    BU_GANG,
    /// <summary>皮杠</summary>
    PI_GANG,
    /// <summary>癞子杠</summary>
    LAIZI_GANG,
    /// <summary>朝天杠</summary>
    CHAO_TIAN_GANG,
    /// <summary>朝天暗杠</summary>
    CHAO_TIAN_AN_GANG
}

#endregion

#region 数据结构

/// <summary>
/// 胡牌提示信息
/// </summary>
public struct HuPaiTipsInfo
{
    /// <summary>牌面值</summary>
    public MahjongFaceValue faceValue;
    /// <summary>番数</summary>
    public int fanAmount;
    /// <summary>剩余张数</summary>
    public int zhangAmount;
}

/// <summary>
/// 麻将牌颜色样式
/// </summary>
public enum MjColor
{
    /// <summary>绿色</summary>
    lv = 0,
    /// <summary>黄色</summary>
    huang = 1,
    /// <summary>蓝色</summary>
    lan = 2,
}

/// <summary>
/// 桌布样式使用 int 值表示：1-9
/// 1-6: 2D 桌布（绿、蓝、黄各2张）
/// 7-9: 3D 桌布（绿、蓝、黄各1张）
/// </summary>

/// <summary>
/// 麻将视图模式
/// </summary>
public enum MahjongViewMode
{
    /// <summary>3D视图</summary>
    View3D = 0,
    /// <summary>2D视图</summary>
    View2D = 1
}

/// <summary>
/// 语言类型
/// </summary>
public enum VoiceLanguage
{
    /// <summary>普通话</summary>
    Mandarin = 0,
    /// <summary>仙桃话</summary>
    XianTao = 1,
    /// <summary>四川话</summary>
    Sichuan = 2
}

/// <summary>
/// 玩家类型（用于语音性别）
/// </summary>
public enum PlayerType
{
    /// <summary>女性</summary>
    FEMALE = 0,
    /// <summary>男性</summary>
    MALE = 1,
    /// <summary>无类型</summary>
    NONE = 2,
}
#endregion

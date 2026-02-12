using System.Collections.Generic;
using NetMsg;

/// <summary>
/// 麻将规则接口 - 定义所有麻将玩法必须实现的基础功能
/// 更新时间: 2025-10-28
/// </summary>
public interface IMahjongRule
{
    /// <summary>
    /// 初始化麻将规则
    /// </summary>
    void Initialize();

    /// <summary>
    /// 获取该玩法的总牌数
    /// </summary>
    int GetTotalCardCount();

    /// <summary>
    /// 设置桌子信息
    /// </summary>
    /// <param name="enterData">进入桌子响应数据</param>
    void SetDeskInfo(Msg_EnterMJDeskRs enterData);

    /// <summary>
    /// 判断是否可以胡牌
    /// </summary>
    /// <param name="handCards">手牌</param>
    /// <param name="targetCard">目标牌</param>
    /// <returns>是否可以胡牌</returns>
    bool CanHu(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard);

    /// <summary>
    /// 判断是否可以碰牌
    /// </summary>
    /// <param name="handCards">手牌</param>
    /// <param name="targetCard">目标牌</param>
    /// <returns>是否可以碰牌</returns>
    bool CanPeng(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard);

    /// <summary>
    /// 判断是否可以杠牌
    /// </summary>
    /// <param name="handCards">手牌</param>
    /// <param name="targetCard">目标牌</param>
    /// <returns>是否可以杠牌</returns>
    bool CanGang(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard);

    /// <summary>
    /// 判断是否可以吃牌
    /// </summary>
    /// <param name="handCards">手牌</param>
    /// <param name="targetCard">目标牌</param>
    /// <returns>可以吃的牌组合</returns>
    List<List<MahjongFaceValue>> CanChi(List<MahjongFaceValue> handCards, MahjongFaceValue targetCard);

    /// <summary>
    /// 获取听牌信息
    /// </summary>
    /// <param name="handCards">手牌</param>
    /// <returns>听牌列表</returns>
    List<MahjongFaceValue> GetTingPaiList(List<MahjongFaceValue> handCards);

    /// <summary>
    /// 处理特殊配置
    /// </summary>
    /// <param name="config">配置数据</param>
    void HandleSpecialConfig(Msg_EnterMJDeskRs msg_EnterMJDeskRs);

    /// <summary>
    /// 获取当前规则支持的花色范围
    /// 用于听牌统计时筛选有效花色
    /// </summary>
    /// <returns>支持的花色列表，null表示支持所有花色</returns>
    List<MahjongHuaSe> GetValidColors();

    /// <summary>
    /// 获取指定花色的最大牌数
    /// </summary>
    /// <param name="color">花色</param>
    /// <returns>该花色的最大牌数，如果不支持该花色返回0</returns>
    int GetMaxCardCountForColor(MahjongHuaSe color);
}

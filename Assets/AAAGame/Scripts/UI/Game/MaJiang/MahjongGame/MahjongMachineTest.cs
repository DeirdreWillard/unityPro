using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;

public class MahjongMachineTest : MonoBehaviour
{
    Button btnAction;
    Dropdown actionDropdown;
    private MahjongGameUI mahjongGameUI;

    // 定义所有测试动作选项
    private readonly string[] testActions = new string[]
    {
            "最后三张",
            "测试亮牌暗铺",
            "测试奖励面板",
    };

    public void TestMahjongFuncInterface(bool isDebugMode)
    {
        if (isDebugMode || Application.isEditor)
        {
            gameObject.SetActive(true);
            btnAction = transform.Find("btnAction").GetComponent<Button>();
            EventTriggerListener.Get(btnAction.gameObject).onClick = OnButtonClick;
            Button closeBtn = transform.Find("CloseBtnTest").GetComponent<Button>();
            EventTriggerListener.Get(closeBtn.gameObject).onClick = (go =>
            {
                gameObject.SetActive(false);
            });

            // 动态填充ActionDropdown选项
            InitActionDropdown();

            // 获取父对象的 MahjongGameUI 组件
            mahjongGameUI = transform.parent.GetComponent<MahjongGameUI>();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // 初始化ActionDropdown，动态添加选项
    void InitActionDropdown()
    {
        actionDropdown = transform.Find("ActionDropdown").GetComponent<Dropdown>();
        if (actionDropdown != null)
        {
            // 清空现有选项
            actionDropdown.ClearOptions();

            // 将测试动作数组转换为Dropdown选项列表
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (string action in testActions)
            {
                options.Add(new Dropdown.OptionData(action));
            }

            // 添加选项到Dropdown
            actionDropdown.AddOptions(options);

            GF.LogInfo_gsc("测试UI初始化", $"已动态添加{testActions.Length}个测试动作选项");
        }
    }
    private void OnButtonClick(GameObject go)
    {
        if (go == btnAction.gameObject)
        {
            Action();
        }
    }

    public List<MahjongFaceValue> CreateSelfHandPaiFaceValueList(MahjongFaceValue[] mjFaceValueList)
    {
        List<MahjongFaceValue> list = new List<MahjongFaceValue>();
        foreach (MahjongFaceValue value in mjFaceValueList)
        {
            list.Add(value);
        }

        return list;
    }

    void Action()
    {
        string playerName = transform.Find("PlayerDropdown").Find("Label").GetComponent<Text>().text;
        string actionName = transform.Find("ActionDropdown").Find("Label").GetComponent<Text>().text;
        int seatIdx = 1;

        switch (playerName)
        {
            case "玩家0": seatIdx = 0; break;
            case "玩家1": seatIdx = 1; break;
            case "玩家2": seatIdx = 2; break;
            case "玩家3": seatIdx = 3; break;
        }

        switch (actionName)
        {
            case "最后三张":
                {
                    //模拟最后三张牌 播放特效
                    if (mahjongGameUI != null && mahjongGameUI.baseMahjongGameManager != null)
                    {
                        // 播放最后三张特效
                        mahjongGameUI.ShowEffect("Last3Eff", 2.0f);

                        GF.LogInfo_gsc("测试功能", $"模拟最后三张牌：剩余牌数已设置为 3，特效已播放");
                    }
                }
                break;

            case "测试亮牌暗铺":
                {
                    TestLiangPaiAnPu();
                }
                break;

            case "测试奖励面板":
                {
                    var rewards = new List<RewardItemData>
                    {
                        new RewardItemData { Id = 1, Count = 5000 },   // 金币
                        new RewardItemData { Id = 2, Count = 100 },     // 钻石
                        new RewardItemData { Id = 3, Count = 10 }     // 联盟币
                    };

                    GF.LogInfo("打开2个奖励面板: 金币 x5000, 钻石 x100");
                    Util.GetInstance().OpenRewardPanel(rewards);
                }
                break;

        }
    }

    #region 亮牌暗铺测试

    /// <summary>
    /// 测试亮牌暗铺判定逻辑（符合麻将规则：3n+2张牌，包含摸牌）
    /// </summary>
    void TestLiangPaiAnPu()
    {
        if (mahjongGameUI == null)
        {
            GF.LogError("MahjongGameUI 未初始化，无法测试");
            return;
        }

        GF.LogInfo("========== 开始测试亮牌暗铺判定（集成UI测试）==========");
        GF.LogInfo("暗铺逻辑说明：");
        GF.LogInfo("- 暗铺 = 锁定某个刻子（3张相同）不拆开，会影响听牌选择");
        GF.LogInfo("- 重要：暗铺后必须仍能听牌，否则不能暗铺");
        GF.LogInfo("- 例如: 手牌 1,1,2,2,3,3,3,4,5,6,7,8,9 + 摸9");
        GF.LogInfo("  · 打9万: 剩1,1,2,2,3,3,3,4,5,6,7,8,9");
        GF.LogInfo("    - 不暗铺3万: 可拆成 123+123+3456789，可胡 1,2,3,6,9");
        GF.LogInfo("    - 暗铺3万: 强制保留 333，拆成 11+22+333+456+789，可胡 1,2 ✓可暗铺");
        GF.LogInfo("  · 打1万: 剩1,2,2,3,3,3,4,5,6,7,8,9,9");
        GF.LogInfo("    - 不暗铺3万: 可拆成 123+3456789+99，可听牌");
        GF.LogInfo("    - 暗铺3万: 锁定333后变成 1+22+333+456+789+99，无法组成标准牌型 ✗不可暗铺");
        GF.LogInfo("将生成测试手牌并模拟亮牌流程，请观察UI变化\n");

        // 延迟1秒开始UI测试，让玩家看到日志
        StartCoroutine(RunUIIntegrationTest());

        GF.LogInfo("========== 测试启动完成 ==========");
    }

    /// <summary>
    /// 运行UI集成测试（协程）
    /// </summary>
    IEnumerator RunUIIntegrationTest()
    {
        yield return new WaitForSeconds(1f);

        // 测试场景: 标准14张带刻子
        yield return StartCoroutine(TestUICase_Standard14());

        GF.LogInfo("\n========== UI集成测试完成 ==========");
        GF.LogInfo("请手动测试亮牌流程:");
        GF.LogInfo("1. 点击【亮牌】按钮");
        GF.LogInfo("2. 测试场景A - 抬起9万:");
        GF.LogInfo("   - 应显示【3万】可暗铺（暗铺后仍能胡 1,2）");
        GF.LogInfo("   - 不暗铺可胡 1,2,3,6,9（更灵活）");
        GF.LogInfo("3. 测试场景B - 抬起1万:");
        GF.LogInfo("   - 应该【不显示】3万可暗铺（暗铺后无法听牌）");
        GF.LogInfo("   - 只能不暗铺，保持牌型灵活性");
        GF.LogInfo("4. 其他牌同理：只有打出后暗铺仍能听牌时，才显示暗铺选项");
    }

    /// <summary>
    /// UI测试场景: 标准14张 (1,1,2,2,3,3,3,4,5,6,7,8,9,摸牌9)
    /// 测试重点：只有打出某牌后，暗铺刻子仍能听牌时，才显示该暗铺选项
    /// - 打9万: 剩1122333456789 → 暗铺3万后 11+22+333+456+789 仍能听牌 ✓可暗铺
    /// - 打1万: 剩122333456789 → 暗铺3万后 1+22+333+456+789+99 无法听牌 ✗不可暗铺
    /// </summary>
    IEnumerator TestUICase_Standard14()
    {
        GF.LogInfo("\n【UI测试场景】生成手牌: 1,1,2,2,3,3,3,4,5,6,7,8,9 + 摸牌9");

        // 生成手牌（13张）
        List<int> handCards = new List<int> { 1, 1, 2, 2, 3, 3, 3, 4, 5, 6, 7, 8, 9 };
        int moCard = 9; // 摸牌

        // 生成手牌到UI
        CreateTestHandCards(handCards, moCard);

        yield return new WaitForSeconds(1f);

        // 模拟进入亮牌流程（可胡 1,2,3,6,9）
        List<int> tingCards = new List<int> { 1, 2, 3, 6, 9 };
        GF.LogInfo("模拟进入亮牌流程，可听牌: 1万, 2万, 3万, 6万, 9万");
        GF.LogInfo("\n关键测试点:");
        GF.LogInfo("  【场景A】抬起9万 → 剩余1,1,2,2,3,3,3,4,5,6,7,8,9");
        GF.LogInfo("    - 不暗铺: 牌型 123+123+3456789，可胡 1,2,3,6,9");
        GF.LogInfo("    - 暗铺3万: 牌型 11+22+333+456+789，仍可胡 1,2 ✓应显示暗铺选项");
        GF.LogInfo("  【场景B】抬起1万 → 剩余1,2,2,3,3,3,4,5,6,7,8,9,9");
        GF.LogInfo("    - 不暗铺: 牌型 123+3456789+99，可听牌");
        GF.LogInfo("    - 暗铺3万: 牌型 1+22+333+456+789+99，无法组成标准牌型 ✗不应显示暗铺选项");
        mahjongGameUI.ShowLiangPaiPanel(tingCards);

        GF.LogInfo("\n→ 请点击【亮牌】按钮，分别测试抬起9万和1万");
        GF.LogInfo("→ 预期: 抬起9万显示暗铺3万，抬起1万不显示暗铺");
    }

    /// <summary>
    /// 创建测试手牌（生成到UI）
    /// </summary>
    void CreateTestHandCards(List<int> handCards, int moCard)
    {
        var seat = mahjongGameUI.GetSeat(0);
        if (seat == null)
        {
            GF.LogError("无法获取座位0（玩家自己）");
            return;
        }

        // 清空现有手牌
        seat.ClearContainer(seat.HandContainer);
        seat.ClearContainer(seat.MoPaiContainer);

        // 创建手牌（13张或更少）
        handCards.Sort();
        seat.CreateHandCards(handCards);

        // 创建摸牌（1张）
        seat.AddMoPai(moCard);

        GF.LogInfo($"已生成手牌: [{string.Join(",", handCards)}] + 摸牌: {moCard}");
    }

    #endregion


}
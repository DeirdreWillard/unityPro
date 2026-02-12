using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;


/*
玩法介绍
1.人数：
游戏支持2-6名玩家共同游戏

2.牌数：
1)普通模式：副牌去掉大小鬼,一共52张,每人发9张,最多支持5人
2)有鬼模式：副牌包含大小鬼,一共54张(4鬼时共56张),每人发9张,最多支持6人
3)10选9玩法：每人发10张牌,可以有大小鬼,玩家可在10张脾中任意选择9张进行摆放,最多支特5人。
3.配牌：将9张手牌配成头道、中道、尾道三副脾,每副三张,其中必须尾道>中道>头道，否则为[相公],配牌失败!随后依次比
4.此牌：所有玩家都要将自己的第一道牌相互对此,按规则判定输赢，较第二道、第三道牌,并结算积分
5.喜牌：当配置的九张牌达成指定条件后。会获得喜牌奖励。奖励为其他玩家支付给拥有喜牌的玩家指定积分,喜牌不管输赢都有奖励。并目可以叠加。
6.投降：若玩家感觉手牌差。则可以选择投降,投降后只输牌分。不扣别人的喜牌奖励分。

比牌规则：
1）首先比较牌型：三条 > 同花顺 > 同花 > 顺子 > 对子 > 单张。
2）牌型一致时，比较点数：A>K>Q>J>10>9>8>7>6>5>4>3>2。
3）三张点数也一致时，最后比较花色：黑桃3 > 红桃2 > 梅花1 > 方块0。
大小鬼：
1）鬼当红黑：小鬼可百变任意一种黑花色的牌。大鬼可百变任意红花色的牌。
2）鬼当任意：大小鬼均可当作任意牌。
3）同副牌中，可出现相同牌。点数均相同则比每张牌的花色。
例子：
玩家1：黑桃A 方块A 大王
玩家2：红桃A 梅花A 小王
若鬼当红黑则玩家2大于玩家1，因为大鬼当红桃A，小鬼当黑桃A，在都有黑桃A和红桃A情况下：
玩家2的梅花A大于玩家1的方块A，玩家2大于玩家1。
例子：
玩家1：方块A 大王 方块7
玩家2：红桃A 红桃K 红桃9
玩家1与玩家2都是同花且有A，比较第二张牌大小时，大鬼可变方块A，大于玩家2的红桃K，即玩家1大于玩家2。
可以出现同花对子，但不会以对子比大小。
大小鬼只会单张牌比较：同理，大小王当任意时，可以出现三条同花！
比牌时按三条比,可以出现三清喜牌!

基本牌型:
牌型展示从大到小分别是：
1.三条：3张同种点数的牌型，例如 A、A、A。
2.同花顺：同花色的顺子，例如 6、7、8（同花色）。
3.同花：同花相同花色，例如 10、7、8（红桃）。
4.顺子：连续的不相同花色，例如 9、10、J（3张牌不是同一种花色）。注：A~3的顺子是最小的顺子
5.对子：两张相同点数的牌，例如 K、K、7。
6.单张：无法组成以上牌型，以点数决定大小。

喜牌说明:
1.三清：手上的三道牌牌型都是同花。
2.全黑：9张牌全部由黑桃和梅花组成的牌型。
3.全红：9张牌全部由红桃和方块组成的牌型。
4.双顺清：手上有两道牌为同花顺的牌型。
5.三顺清：手上三道牌全都为同花顺的牌型。
6.双三条：手上中道和尾道都为三条的牌型。
7.全三条：手上三道牌均为三条的牌型。
8.四个头：手上牌里有四张一样的牌型（注：四张必须配出一个三条，才算四个头奖励。若有两个四张，并配出两个三条，则可获得两次四个头奖励，三条+鬼不会触发四个头奖励,3鬼+鬼算4个头奖励）
9.连顺：9张牌必须配成连贯的顺子，例如A23,456,789（注：顺序打乱则不算连顺奖励）
10.清连顺：9张牌可组成连贯的顺子，并且9张牌只有一种花色
11.三鬼：拥有3张及以上鬼牌。
12.通关：某玩家的三道牌，每一道都比其他玩家大，则称为通关，通关也可获得喜牌奖励

结算规则:
首先比较每道牌的积分：
1）2人模式下，每道牌赢家得1分，输家扣1分
2）3人模式下，每道牌最大的玩家为赢家，其他为输家。赢家得3分，第二大的扣1分最小的扣2分
3）4人模式下，每道牌最大的玩家为赢家，其他为输家。赢家得6分，第二大的扣1分第三大的扣2分，最小的扣3分
4）5人模式下，每道牌最大的玩家为赢家，其他为输家赢家得10分，第三大的扣1分，第三大的扣2分，第四大的扣3分，最小的扣4分
5）6人模式下，每道牌最大的玩家为赢家，其他为输家。赢家得15分，第三大的扣1分，第三大的扣2分，第四大的扣3分，第五大的扣4分，最小的扣5分
三道分数累加后，随后看喜牌奖励:
1）喜牌奖励积分的方式为其他玩家每人贡献1/2/3/4/5（对应2/3/4/5/6人模式）
2）例如3人模式最大扣分为2分，其他玩家则额外扣2分给拥有喜牌的玩家。如有多个喜牌，则每个都单独计算积分。
投降：玩家投降后，只输牌分，不扣别人的喜牌奖励分，投降的玩家输最高分数，若多人投降，则都按输最高分数算：
*/
public class LocalOp_Bj : MonoBehaviour
{
    public SeatManager_bj seatMgr;
    // 现有属性保持不变
    public GameObject localMask;
    public GameObject localButtons;
    public CountDownFxControl countdownCtr;
    public Card[] localCards;
    public Pier_bj[] piers;
    public Button 顺子;
    public Button 同花;
    public Button 同花顺;
    public Button 三条;
    public GameObject 弃牌;

    public Text sortText;

    // 添加动画状态标记
    public bool isAnimationPlaying = false;

    /// <summary>
    /// 单选模式选中的卡牌 0-9  道牌为10 11 12 20 21 22 30 31 32
    /// </summary>
    private int selectedCardIndex = -1;
    private List<int> multiSelectedIndices = new List<int>(); // 多选模式选中的卡牌

    // 滑动选牌相关字段
    private bool m_IsDraggingCard = false;
    private HashSet<int> m_DragSelectedIndices = new HashSet<int>();
    private int m_DragStartIndex = -1; // 添加拖动起始索引
    private float m_CardHighlightAmount = 15f;
    private Vector3 m_LastPointerPosition;

    // 手牌数据
    public int[] myCards;
    // 改回使用Vector2存储UI坐标
    private Vector2[] handCardPositions = new Vector2[10];

    // 牌型按钮激活状态
    private bool hasBoom = false;
    private bool hasStraightFlush = false;
    private bool hasFlush = false;
    private bool hasStraight = false;

    // 添加鬼牌模式枚举
    public enum GhostMode
    {
        Any,        // 鬼当任意
        RedBlack    // 鬼当红黑
    }

    // 添加排序模式枚举
    public enum SortModeBJ
    {
        ByValue,    // 按点数排序（默认）
        BySuit      // 按花色排序（黑桃>红桃>梅花>方块）
    }

    // 手牌类型 9张牌 10张牌
    public int handCardType = 9;
    // 在类中添加模式属性
    public GhostMode currentGhostMode = GhostMode.Any; // 默认模式
    private SortModeBJ currentSortMode = SortModeBJ.ByValue; // 默认排序方式

    // 添加每种牌型的当前选中组合索引
    private Dictionary<ZjhCardType, int> m_CardTypeCombinationIndex = new Dictionary<ZjhCardType, int>();
    private ZjhCardType m_LastSelectedCardType = ZjhCardType.High; // 记录上次选择的牌型

    #region Unity Lifecycle
    private void Awake()
    {
        m_DragSelectedIndices = new HashSet<int>();
    }
    private void Update()
    {
        if (!isAnimationPlaying && m_IsDraggingCard && Input.GetMouseButtonUp(0))
        {
            DragEnd();
        }

        // 滑动过程中检测是否超出滑动区域，如果是则自动结束滑动
        if (m_IsDraggingCard && (Input.mousePosition.x < 0 || Input.mousePosition.x > Screen.width ||
                               Input.mousePosition.y < 0 || Input.mousePosition.y > Screen.height))
        {
            DragEnd();
        }
    }
    #endregion

    public void InitPanel()
    {
        // 存储卡牌初始位置，使用UI坐标系
        for (int i = 0; i < localCards.Length; i++)
        {
            if (localCards[i] == null) continue;
            handCardPositions[i] = localCards[i].GetComponent<RectTransform>().anchoredPosition;
            Debug.Log($"初始化卡牌[{i}]位置: {handCardPositions[i]}");
            localCards[i].gameObject.SetActive(false);

            // 添加并初始化 HandCardInteraction 脚本
            HandCardInteraction interaction = localCards[i].GetComponent<HandCardInteraction>();
            if (interaction == null)
            {
                interaction = localCards[i].gameObject.AddComponent<HandCardInteraction>();
            }
            interaction.Initialize(this, i);
        }
        foreach (var pier in piers)
        {
            pier.InitPierPanel();
        }
    }

    /// <summary>
    /// 自己发牌动画 先将手牌初始化在中间上面一定距离的位置 一起散开到对应位置
    /// 然后从左到右依次翻开 翻开后就是排序后的手牌
    /// </summary>
    public void DealCardAni()
    {
        Seat_bj seat = seatMgr.GetSelfSeat();
        if (seat == null)
        {
            Debug.LogError("没找到自己信息");
            return;
        }
        myCards = seat.handCardDatas;

        // 适配九张牌或十张牌的居中位置
        handCardType = myCards.Length;
        float cardWidth = 117f; // 卡牌宽度，根据实际调整
        float spacing = -22f;   // 卡牌间距，根据实际调整
        float cardStep = cardWidth + spacing; // 每张牌的步进距离

        Debug.Log($"卡片宽度: {cardWidth}, 间距: {spacing}, 步进: {cardStep}");

        // 计算9张牌和10张牌的位置
        if (handCardType == 9)
        {
            // 对于9张牌，确保第5张牌（索引4）位于(0,0)
            float midIndex = 4; // 第5张牌的索引
            for (int i = 0; i < handCardType; i++)
            {
                float xPos = (i - midIndex) * cardStep; // 相对于中间牌的偏移
                handCardPositions[i] = new Vector2(xPos, handCardPositions[0].y);
                // Debug.Log($"计算9张牌目标位置[{i}]: {handCardPositions[i]}");
            }
        }
        else
        {
            // 10张牌，确保中间在(0,0)
            float midIndex = 4.5f; // 第5张和第6张之间
            for (int i = 0; i < handCardType; i++)
            {
                float xPos = (i - midIndex) * cardStep; // 相对于中间的偏移
                handCardPositions[i] = new Vector2(xPos, handCardPositions[0].y);
                // Debug.Log($"计算10张牌目标位置[{i}]: {handCardPositions[i]}");
            }
        }

        // 初始化所有牌的位置（中间上方）
        Vector2 initialPosition = new Vector2(0, handCardPositions[0].y + 300f);

        // 创建动画序列
        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(gameObject);

        // 显示所有卡牌（背面朝上）
        for (int i = 0; i < handCardType; i++)
        {
            if (myCards[i] != 0)
            {
                localCards[i].gameObject.SetActive(true);
                localCards[i].Init(-1); // -1表示卡牌背面
                localCards[i].GetComponent<RectTransform>().anchoredPosition = initialPosition;
            }
        }

        // 1. 牌一张接一张快速散开（扇形发牌效果）
        float spreadTime = 1f;        // 每张牌移动的总时间
        float spreadDelay = 0.05f;      // 每张牌开始移动的时间间隔

        // 创建单独的移动序列
        Sequence moveSequence = DOTween.Sequence();
        moveSequence.SetTarget(gameObject);

        for (int i = 0; i < handCardType; i++)
        {
            if (myCards[i] != 0)
            {
                int index = i;
                // Debug.Log($"卡牌[{index}]移动到目标位置: {handCardPositions[index]}");
                RectTransform rectTrans = localCards[i].GetComponent<RectTransform>();

                // 设置开始时间错开，但动画时长相同
                // 这样会产生一张接着一张开始移动，但很快全部到位的效果
                moveSequence.Insert(spreadDelay * index, DOTween.To(
                    () => rectTrans.anchoredPosition,
                    x => rectTrans.anchoredPosition = x,
                    handCardPositions[index],
                    spreadTime
                ).SetEase(Ease.OutCubic));
            }
        }

        // 计算所有牌都到达位置的总时间
        float totalMoveTime = spreadDelay * (handCardType - 1) + spreadTime;
        Debug.Log($"所有牌移动完成总耗时: {totalMoveTime}秒");

        // 添加移动序列到主序列
        sequence.Append(moveSequence);

        // 2. 从左到右依次翻开牌的动画
        float flipDelay = 0.04f; // 每张牌翻开的延迟
        float flipTime = 0.08f;  // 翻牌动画时间

        for (int i = 0; i < handCardType; i++)
        {
            if (myCards[i] != 0)
            {
                int index = i;

                sequence.Append(DOVirtual.DelayedCall(flipDelay, () =>
                {
                    // 创建单张牌的动画序列
                    Sequence flipSequence = DOTween.Sequence();
                    flipSequence.SetTarget(localCards[index].gameObject);

                    // 1. 先做缩放，模仿真实翻牌的深度感
                    flipSequence.Append(localCards[index].transform
                        .DOScale(new Vector3(1.1f, 1.1f, 1), 0.04f)
                        .SetEase(Ease.OutQuad));

                    // 2. 卡牌旋转：分两阶段，首先旋转到背面
                    flipSequence.Append(localCards[index].transform
                        .DORotate(new Vector3(0, 90, 0), flipTime, RotateMode.FastBeyond360)
                        .SetEase(Ease.InOutCubic));

                    // 3. 在旋转到90度时更换牌面
                    flipSequence.AppendCallback(() =>
                    {
                        localCards[index].Init(myCards[index]);
                    }).OnComplete(() =>
                    {
                        if (index % 3 == 0 || index == handCardType - 1)
                        {
                           Sound.PlayEffect("niuniu/card.mp3");
                        }
                    });

                    // 4. 旋转回正面
                    flipSequence.Append(localCards[index].transform
                        .DORotate(new Vector3(0, 0, 0), flipTime, RotateMode.FastBeyond360)
                        .SetEase(Ease.InOutCubic));

                    // 5. 卡牌缩放回正常大小
                    flipSequence.Append(localCards[index].transform
                        .DOScale(Vector3.one, 0.04f)
                        .SetEase(Ease.OutQuad));

                    // 播放当前卡牌的序列动画
                    flipSequence.Play();
                }));

            }
        }

        sequence.AppendInterval(0.2f);

        // 减少使用OnComplete回调的频率
        sequence.AppendCallback(() =>
        {
           Sound.PlayEffect("BJ/请出牌.mp3");
            Debug.Log("发牌动画完成");
            // 添加标记确保所有动画完成后才能进行操作
            isAnimationPlaying = false;
            if (GF.Procedure.CurrentProcedure is BJProcedure bjProcedure)
            {
                if (bjProcedure.enterBJDeskRs.CkState == CompareChickenState.CkGroupCard && !localMask.activeSelf)
                {
                    ShowLocalOP();
                }
            }
        });

        //播放前设置标志，防止动画播放期间的交互干扰
        isAnimationPlaying = true;

        // 播放整个动画序列
        sequence.Play();
    }

    // 初始化
    public void ShowLocalOP()
    {
        if (isAnimationPlaying) return;
        GF.LogInfo($" 显示本地操作界面");
        // 初始化
        m_DragSelectedIndices.Clear();
        m_IsDraggingCard = false;
        m_DragStartIndex = -1;
        m_LastPointerPosition = Vector3.zero;

        // 初始化手牌
        if (GF.Procedure.CurrentProcedure is BJProcedure bjProcedure)
        {
            currentGhostMode = bjProcedure.enterBJDeskRs.BaseInfo.CkConfig.Play.Contains(3) ? GhostMode.RedBlack : GhostMode.Any;
            弃牌.gameObject.SetActive(bjProcedure.enterBJDeskRs.BaseInfo.CkConfig.Play.Contains(1));
        }
        Seat_bj seat = seatMgr.GetSelfSeat();
        if (seat == null)
        {
            Debug.LogError("没找到自己信息");
            return;
        }
        myCards = seat.handCardDatas;
        handCardType = myCards.Length;
        // 显示卡牌
        UpdateHandCardsUI();
        foreach (var pier in piers)
        {
            pier.ClearPier();
        }
        // 显示界面
        localMask.SetActive(true);
        localButtons.SetActive(true);
    }

    // 更新所有手牌UI
    public void UpdateHandCardsUI()
    {
        m_LastSelectedCardType = ZjhCardType.High;
        SortHandCards();

        // 检测牌型并更新按钮状态
        CheckCardTypes();

        for (int i = 0; i < localCards.Length; i++)
        {
            if (myCards.Length > i && myCards[i] != 0)
            {
                localCards[i].gameObject.SetActive(true);
                localCards[i].Init(myCards[i]);
                localCards[i].GetComponent<RectTransform>().anchoredPosition = handCardPositions[i];
            }
            else
            {
                localCards[i].gameObject.SetActive(false);
            }
        }
    }

    // 道牌点击处理
    public void OnPierCardClick(int pierIndex, int cardIndex)
    {
        int index = (pierIndex + 1) * 10 + cardIndex;
        if (selectedCardIndex == -1)
        {
            // 多选模式下，没选择手牌，选中道牌
            if (multiSelectedIndices.Count == 0)
            {
                // 单选模式下，没有选中手牌，选中道牌
                selectedCardIndex = (pierIndex + 1) * 10 + cardIndex;
                // 显示选中的道牌
                piers[pierIndex].cards[cardIndex].SetImgColor(Color.gray);
            }// 多选模式下，已选择一张手牌，交换手牌和道牌
            else if (multiSelectedIndices.Count == 1)
            {
                SwapHandCardOrPierCard(multiSelectedIndices[0], index);
                ClearSelection();
            }
            else
            {
                //无反应
            }
        }
        else
        {
            int selectPierIndex = selectedCardIndex / 10 - 1;
            int selectCardIndex = selectedCardIndex % 10;
            if (selectPierIndex == pierIndex)
            {
                if (selectCardIndex == cardIndex)
                {
                    // 取消选中
                    selectedCardIndex = -1;
                    piers[selectPierIndex].cards[selectCardIndex].SetImgColor(Color.white);
                }
                else
                {
                    piers[selectPierIndex].cards[selectCardIndex].SetImgColor(Color.white);
                    // 单选模式下，选着同道中的其他道牌，切换选中道牌
                    selectedCardIndex = (pierIndex + 1) * 10 + cardIndex;
                    // 显示选中的道牌
                    piers[pierIndex].cards[cardIndex].SetImgColor(Color.gray);
                }
            }
            else
            {
                // 交换
                SwapHandCardOrPierCard(selectedCardIndex, index);
                ClearSelection();
            }
        }
    }

    // 道点击处理 
    public void OnPierClick(int index)
    {
        if (Util.IsClickLocked() || isAnimationPlaying) return;
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        if (index < 0 || index >= piers.Length)
        {
            return; // 无效的道索引
        }
        if (selectedCardIndex == -1)
        {
            // 多选模式下，执行整道替换
            if (multiSelectedIndices.Count <= 3)
            {
                SwapMultiCardsWithPier(index);
            }
            else
            {
                // 选中四张牌，无反应
                GF.StaticUI.ShowError("最多只能组3张牌");
                return;
            }
        }
        else
        {
            //单选模式下, 移动道牌
            MoveHandCardToPier(selectedCardIndex, index);
            ClearSelection();
        }
    }

    // 手牌移动到道中
    private void MoveHandCardToPier(int handIndex, int pierIndex)
    {
        // 查找道中的空位
        int emptySlot = -1;
        for (int i = 0; i < 3; i++)
        {
            if (piers[pierIndex].cardDatas[i] == 0)
            {
                emptySlot = i;
                break;
            }
        }

        if (emptySlot != -1)
        {
            // 创建临时数组用于验证
            int[] tempCardDatas = new int[3];
            Array.Copy(piers[pierIndex].cardDatas, tempCardDatas, 3);

            if (handIndex >= 0 && handIndex < 10)
            {
                // 手牌
                tempCardDatas[emptySlot] = myCards[handIndex];
            }
            else if (handIndex >= 10)
            {
                // 道牌
                int pierIndexStart = handIndex / 10 - 1;
                int cardIndex = handIndex % 10;
                if (pierIndexStart != pierIndex)
                {
                    tempCardDatas[emptySlot] = piers[pierIndexStart].cardDatas[cardIndex];
                }
                else
                {
                    return;
                }
            }

            // 验证通过，执行实际移动
            if (handIndex >= 0 && handIndex < 10)
            {
                piers[pierIndex].SetCard(emptySlot, localCards[handIndex], false);
                myCards[handIndex] = 0;
                UpdateHandCardsUI();
            }
            else if (handIndex >= 10)
            {
                int pierIndexStart = handIndex / 10 - 1;
                int cardIndex = handIndex % 10;
                int pierCardValue = piers[pierIndexStart].cardDatas[cardIndex];
                piers[pierIndex].SetCard_pier(emptySlot, piers[pierIndexStart].cards[cardIndex], pierCardValue, false);
                piers[pierIndexStart].cardDatas[cardIndex] = 0;
                piers[pierIndexStart].UpdateUI();
            }
        }
    }

    // 交换牌 手牌或道牌 大于等于10就是道牌
    private void SwapHandCardOrPierCard(int cardIndex1, int cardIndex2)
    {
        m_LastSelectedCardType = ZjhCardType.High; // 交换后重置牌型提示
        if (cardIndex1 >= 10 && cardIndex2 >= 10)
        {
            //两张道牌
            int pierIndex1 = cardIndex1 / 10 - 1;
            int pierCardIndex1 = cardIndex1 % 10;
            int pierIndex2 = cardIndex2 / 10 - 1;
            int pierCardIndex2 = cardIndex2 % 10;
            //交换道牌
            int pierCardValue1 = piers[pierIndex1].cardDatas[pierCardIndex1];
            int pierCardValue2 = piers[pierIndex2].cardDatas[pierCardIndex2];
            piers[pierIndex1].SetCard_pier(pierCardIndex1, piers[pierIndex2].cards[pierCardIndex2], pierCardValue2);
            piers[pierIndex2].SetCard_pier(pierCardIndex2, piers[pierIndex1].cards[pierCardIndex1], pierCardValue1);
        }
        else if (cardIndex1 >= 10)
        {
            int pierIndex = cardIndex1 / 10 - 1;
            int pierCardIndex = cardIndex1 % 10;
            int pierCardValue = piers[pierIndex].cardDatas[pierCardIndex];
            piers[pierIndex].SetCard(pierCardIndex, localCards[cardIndex2]);
            myCards[cardIndex2] = pierCardValue;
        }
        else if (cardIndex2 >= 10)
        {
            int pierIndex = cardIndex2 / 10 - 1;
            int pierCardIndex = cardIndex2 % 10;
            int pierCardValue = piers[pierIndex].cardDatas[pierCardIndex];
            piers[pierIndex].SetCard(pierCardIndex, localCards[cardIndex1]);
            myCards[cardIndex1] = pierCardValue;
        }
        else
        {
            //两张手牌
            int handIndex1 = cardIndex1;
            int handIndex2 = cardIndex2;
            int handValue1 = myCards[handIndex1];
            int handValue2 = myCards[handIndex2];
            myCards[handIndex1] = handValue2;
            myCards[handIndex2] = handValue1;
        }
        UpdateHandCardsUI();
    }

    // 检查交换是否涉及道牌
    private bool IsSwapInvolvingPier(int cardIndex)
    {
        return cardIndex >= 10;
    }

    // 多选卡牌与整道交换
    private void SwapMultiCardsWithPier(int pierIndex)
    {
        //先判断multiSelectedIndices 数量如果选中一张牌放上去补位
        //如果选中两张牌 如果有俩张空位就补位 没有空位无反应
        //如果选中三张牌 点击道框 直接整道替换

        // 获取选中的卡牌数量
        int selectedCount = multiSelectedIndices.Count;

        // 获取道中的空位数量
        int emptySlots = 0;
        for (int i = 0; i < 3; i++)
        {
            if (piers[pierIndex].cardDatas[i] == 0)
            {
                emptySlots++;
            }
        }

        // 根据选中牌数执行不同操作
        if (selectedCount == 1)
        {
            // 选中一张牌，放上去补位
            if (emptySlots > 0)
            {
                // 找到第一个空位
                int slotIndex = -1;
                for (int i = 0; i < 3; i++)
                {
                    if (piers[pierIndex].cardDatas[i] == 0)
                    {
                        slotIndex = i;
                        break;
                    }
                }

                if (slotIndex != -1)
                {
                    // 创建临时数组用于验证
                    int[] tempCardDatas = new int[3];
                    Array.Copy(piers[pierIndex].cardDatas, tempCardDatas, 3);
                    tempCardDatas[slotIndex] = myCards[multiSelectedIndices[0]];

                    // 将选中牌放入空位
                    int cardIndex = multiSelectedIndices[0];
                    piers[pierIndex].SetCard(slotIndex, localCards[cardIndex]);
                    myCards[cardIndex] = 0;
                    UpdateHandCardsUI();
                }
                ClearSelection();
            }
            return;
        }
        else if (selectedCount == 2)
        {
            // 选中两张牌，如果有两个空位就补位，否则无反应
            if (emptySlots >= 2)
            {
                // 找到两个空位
                List<int> slotIndices = new List<int>();
                for (int i = 0; i < 3; i++)
                {
                    if (piers[pierIndex].cardDatas[i] == 0)
                    {
                        slotIndices.Add(i);
                        if (slotIndices.Count == 2)
                            break;
                    }
                }

                if (slotIndices.Count == 2)
                {
                    // 创建临时数组用于验证
                    int[] tempCardDatas = new int[3];
                    Array.Copy(piers[pierIndex].cardDatas, tempCardDatas, 3);

                    for (int i = 0; i < 2; i++)
                    {
                        tempCardDatas[slotIndices[i]] = myCards[multiSelectedIndices[i]];
                    }

                    // 将选中牌放入空位
                    for (int i = 0; i < 2; i++)
                    {
                        int cardIndex = multiSelectedIndices[i];
                        piers[pierIndex].SetCard(slotIndices[i], localCards[cardIndex]);
                        myCards[cardIndex] = 0;
                    }
                    UpdateHandCardsUI();
                }
                ClearSelection();
            }
            return;
        }
        else if (selectedCount == 3)
        {
            // 选中三张牌，直接整道替换
            // 创建临时数组用于验证
            int[] tempCardDatas = new int[3];
            for (int i = 0; i < 3; i++)
            {
                tempCardDatas[i] = myCards[multiSelectedIndices[i]];
            }

            // 获取所选卡牌值
            List<Card> selectedCards = new List<Card>();
            foreach (int index in multiSelectedIndices)
            {
                selectedCards.Add(localCards[index]);
            }

            // 获取道牌原有值
            List<int> pierCardValues = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                pierCardValues.Add(piers[pierIndex].cardDatas[i]);
            }

            // 更新道牌数据
            for (int i = 0; i < 3; i++)
            {
                piers[pierIndex].SetCard(i, selectedCards[i]);
            }

            // 清除已选卡牌在手牌中的数据
            foreach (int index in multiSelectedIndices)
            {
                myCards[index] = 0;
            }

            // 将原道牌数据添加回手牌
            foreach (int cardValue in pierCardValues)
            {
                if (cardValue != 0)
                {
                    // 寻找空位
                    int emptySlot = 0;
                    while (emptySlot < myCards.Length && myCards[emptySlot] != 0)
                    {
                        emptySlot++;
                    }

                    if (emptySlot < myCards.Length)
                    {
                        myCards[emptySlot] = cardValue;
                    }
                }
            }

            // 更新UI
            UpdateHandCardsUI();
            ClearSelection();
        }
    }

    // 检测手牌中是否存在特定牌型，并更新按钮状态
    private void CheckCardTypes()
    {
        m_LastSelectedCardType = ZjhCardType.High; // CheckCardTypes时也重置
        // 收集所有有效手牌
        List<int> validCards = new List<int>();
        for (int i = 0; i < myCards.Length; i++)
        {
            if (myCards[i] != 0)
            {
                validCards.Add(myCards[i]);
            }
        }

        // 将整数卡牌转换为CardAvatar对象以便分析
        List<CardAvatar> cardAvatars = new List<CardAvatar>();
        foreach (int card in validCards)
        {
            cardAvatars.Add(new CardAvatar(card));
        }

        // 检测各种牌型
        hasBoom = FindThreeOfKindCombinations(cardAvatars).Count > 0;
        hasStraightFlush = FindStraightFlushCombinations(cardAvatars).Count > 0;
        hasFlush = FindFlushCombinations(cardAvatars).Count > 0;
        hasStraight = FindStraightCombinations(cardAvatars).Count > 0;

        // 更新按钮可见性
        顺子.gameObject.SetActive(hasStraight);
        同花.gameObject.SetActive(hasFlush);
        同花顺.gameObject.SetActive(hasStraightFlush);
        三条.gameObject.SetActive(hasBoom);
    }

    // 获取最高花色
    private int GetHighestSuit(List<CardAvatar> cards)
    {
        return cards.OrderByDescending(c => c.Value).ThenByDescending(c => c.Suit).First().Suit;
    }

    // 寻找三条组合
    private List<List<CardAvatar>> FindThreeOfKindCombinations(List<CardAvatar> cards)
    {
        List<List<CardAvatar>> result = new List<List<CardAvatar>>();
        bool isRedBlack = currentGhostMode == GhostMode.RedBlack;

        // 分离鬼牌和普通牌
        List<CardAvatar> kings = cards.Where(c => c.IsKing).ToList();
        List<CardAvatar> normalCards = cards.Where(c => !c.IsKing).ToList();
        int kingCount = kings.Count;
        int bigKingCount = kings.Count(c => c.Value == 15);
        int smallKingCount = kings.Count(c => c.Value == 14);

        // 按点数分组普通牌
        var valueGroups = normalCards.GroupBy(c => c.Value)
            .OrderByDescending(g => g.Key == 1 ? 14 : g.Key)
            .ToList();

        // 1. 查找不使用鬼牌的三条组合
        foreach (var group in valueGroups)
        {
            if (group.Count() >= 3)
            {
                // 从同点数的牌中选取3张的不同组合
                var sameValueCards = group.OrderByDescending(c => c.Suit).ToList();
                for (int i = 0; i < sameValueCards.Count; i++)
                {
                    for (int j = i + 1; j < sameValueCards.Count; j++)
                    {
                        for (int k = j + 1; k < sameValueCards.Count; k++)
                        {
                            result.Add(new List<CardAvatar> { sameValueCards[i], sameValueCards[j], sameValueCards[k] });
                        }
                    }
                }
            }
        }

        // 如果没有鬼牌，直接返回结果
        if (kingCount == 0)
            return result;

        // 2. 查找使用1张鬼牌的三条组合
        if (kingCount >= 1)
        {
            foreach (var group in valueGroups)
            {
                if (group.Count() >= 2) // 需要至少两张同点数的普通牌
                {
                    var sameValueCards = group.OrderByDescending(c => c.Suit).ToList();
                    // 遍历所有两张普通牌的组合
                    for (int i = 0; i < sameValueCards.Count; i++)
                    {
                        for (int j = i + 1; j < sameValueCards.Count; j++)
                        {
                            // 遍历所有可用的单张鬼牌
                            for (int k = 0; k < kings.Count; k++)
                            {
                                CardAvatar king = kings[k];
                                // 红黑模式检查 (如果需要，但三条只看点数，此检查可能非必要)
                                // if (isRedBlack) { ... }
                                result.Add(new List<CardAvatar> { sameValueCards[i], sameValueCards[j], king });
                            }
                        }
                    }
                }
            }
        }

        // 3. 查找使用2张鬼牌的三条组合
        if (kingCount >= 2)
        {
            foreach (var group in valueGroups)
            {
                if (group.Count() >= 1) // 只需要一张同点数的普通牌
                {
                    var sameValueCards = group.OrderByDescending(c => c.Suit).ToList();
                    // 遍历所有单张普通牌
                    foreach (var normalCard in sameValueCards)
                    {
                        // 遍历所有两张鬼牌的组合
                        for (int i = 0; i < kings.Count; i++)
                        {
                            for (int j = i + 1; j < kings.Count; j++)
                            {
                                // 红黑模式检查 (如果需要)
                                // if (isRedBlack) { ... }
                                result.Add(new List<CardAvatar> { normalCard, kings[i], kings[j] });
                            }
                        }
                    }
                }
            }
        }

        // 去重可能的重复组合 (例如 K+K+小鬼 和 K+K+小鬼)
        // 通过比较排序后的牌ID列表来去重
        var uniqueResults = new List<List<CardAvatar>>();
        var seenCombinations = new HashSet<string>();

        foreach (var combo in result)
        {
            string comboKey = string.Join(",", combo.Select(c => c.GetCardId()).OrderBy(id => id));
            if (seenCombinations.Add(comboKey))
            {
                uniqueResults.Add(combo);
            }
        }

        return uniqueResults;
    }

    // 寻找同花顺组合
    private List<List<CardAvatar>> FindStraightFlushCombinations(List<CardAvatar> cards)
    {
        List<List<CardAvatar>> result = new List<List<CardAvatar>>();
        bool isRedBlack = currentGhostMode == GhostMode.RedBlack;

        // 分离鬼牌和普通牌
        List<CardAvatar> kings = cards.Where(c => c.IsKing).ToList();
        List<CardAvatar> normalCards = cards.Where(c => !c.IsKing).ToList();
        int kingCount = kings.Count;
        int bigKingCount = kings.Count(c => c.Value == 15);
        int smallKingCount = kings.Count(c => c.Value == 14);

        // 1. 查找不使用鬼牌的同花顺
        for (int suit = 3; suit >= 0; suit--)
        {
            List<CardAvatar> suitCards = normalCards.Where(c => c.Suit == suit).ToList();

            if (suitCards.Count >= 3)
            {
                // 检查QKA
                if (suitCards.Any(c => c.Value == 12) && suitCards.Any(c => c.Value == 13) && suitCards.Any(c => c.Value == 1))
                {
                    result.Add(new List<CardAvatar> {
                        suitCards.First(c => c.Value == 1),
                        suitCards.First(c => c.Value == 13),
                        suitCards.First(c => c.Value == 12)
                    });
                }

                // 检查普通顺子
                List<int> values = suitCards.Select(c => c.Value).Distinct().OrderBy(v => v).ToList();
                for (int i = 0; i <= values.Count - 3; i++)
                {
                    if (values[i] + 1 == values[i + 1] && values[i + 1] + 1 == values[i + 2])
                    {
                        result.Add(new List<CardAvatar> {
                            suitCards.First(c => c.Value == values[i+2]),
                            suitCards.First(c => c.Value == values[i+1]),
                            suitCards.First(c => c.Value == values[i])
                        });
                    }
                }

                // 检查A23
                if (suitCards.Any(c => c.Value == 1) && suitCards.Any(c => c.Value == 2) && suitCards.Any(c => c.Value == 3))
                {
                    result.Add(new List<CardAvatar> {
                        suitCards.First(c => c.Value == 3),
                        suitCards.First(c => c.Value == 2),
                        suitCards.First(c => c.Value == 1)
                    });
                }
            }
        }

        // 如果没有鬼牌，直接返回结果
        if (kingCount == 0)
            return result;

        // 2. 查找使用1或2张鬼牌的同花顺组合
        List<List<CardAvatar>> kingCombos = new List<List<CardAvatar>>();
        List<CardAvatar> availableKingsList = kings.ToList(); // 可用的鬼牌列表

        for (int suit = 3; suit >= 0; suit--)
        {
            List<CardAvatar> currentSuitCards = normalCards.Where(c => c.Suit == suit).ToList();
            List<CardAvatar> usableKings = new List<CardAvatar>();

            // 确定当前花色可用的鬼牌
            if (isRedBlack)
            {
                if (suit == 0 || suit == 2) // 红桃或方块
                    usableKings = availableKingsList.Where(k => k.Value == 15).ToList(); // 大鬼
                else // 黑桃或梅花
                    usableKings = availableKingsList.Where(k => k.Value == 14).ToList(); // 小鬼
            }
            else
            {
                usableKings = availableKingsList; // 任意模式下所有鬼牌都可用
            }

            int usableKingCount = usableKings.Count;

            // 至少需要1张普通牌 + 鬼牌 或 纯鬼牌
            if (currentSuitCards.Count + usableKingCount >= 3)
            {
                // 调用FindStraightCombinationsWithKings查找该花色下的所有顺子
                // 注意：传入的kings应该是针对该花色可用的鬼牌
                List<List<CardAvatar>> potentialStraights =
                    FindStraightCombinationsWithKings(currentSuitCards, usableKings, Math.Min(2, usableKingCount), isRedBlack);

                foreach (var combo in potentialStraights)
                {
                    // 验证组合是否确实是目标花色的同花顺
                    // (FindStraightCombinationsWithKings可能为了凑顺子用了其他花色的牌，这里要过滤)
                    // 同时，确保鬼牌变身后符合花色要求
                    bool isValidStraightFlush = true;
                    int kingsInCombo = 0;
                    foreach (var card in combo)
                    {
                        if (card.IsKing)
                        {
                            kingsInCombo++;
                            // 红黑模式下检查鬼牌是否符合花色
                            if (isRedBlack &&
                               !((card.Value == 15 && (suit == 0 || suit == 2)) || // 大鬼配红
                                 (card.Value == 14 && (suit == 1 || suit == 3))))   // 小鬼配黑
                            {
                                isValidStraightFlush = false;
                                break;
                            }
                        }
                        else if (card.Suit != suit) // 检查普通牌花色
                        {
                            isValidStraightFlush = false;
                            break;
                        }
                    }

                    // 添加有效的同花顺组合
                    if (isValidStraightFlush && combo.Count == 3 && kingsInCombo <= 2)
                    {
                        kingCombos.Add(combo);
                    }
                }
            }
        }

        // 将鬼牌组合加入结果列表
        result.AddRange(kingCombos);

        // 去重
        var uniqueResults = new List<List<CardAvatar>>();
        var seenCombinations = new HashSet<string>();
        foreach (var combo in result)
        {
            string comboKey = string.Join(",", combo.Select(c => c.GetCardId()).OrderBy(id => id));
            if (seenCombinations.Add(comboKey))
            {
                uniqueResults.Add(combo);
            }
        }

        return uniqueResults;
    }

    // 寻找同花组合
    private List<List<CardAvatar>> FindFlushCombinations(List<CardAvatar> cards)
    {
        List<List<CardAvatar>> result = new List<List<CardAvatar>>();
        bool isRedBlack = currentGhostMode == GhostMode.RedBlack;

        // 分离鬼牌和普通牌
        List<CardAvatar> kings = cards.Where(c => c.IsKing).ToList();
        List<CardAvatar> normalCards = cards.Where(c => !c.IsKing).ToList();
        int kingCount = kings.Count;
        int bigKingCount = kings.Count(c => c.Value == 15);
        int smallKingCount = kings.Count(c => c.Value == 14);

        // 先找出所有同花顺组合，用于后续排除
        List<List<CardAvatar>> straightFlushCombos = FindStraightFlushCombinations(cards);

        // 将同花顺组合的ID存入HashSet，便于快速查找
        HashSet<string> straightFlushKeys = new HashSet<string>();
        foreach (var combo in straightFlushCombos)
        {
            string key = string.Join(",", combo.Select(c => c.GetCardId()).OrderBy(id => id));
            straightFlushKeys.Add(key);
        }

        // 1. 查找不使用鬼牌的同花
        for (int suit = 3; suit >= 0; suit--)
        {
            List<CardAvatar> suitCards = normalCards.Where(c => c.Suit == suit).ToList();

            if (suitCards.Count >= 3)
            {
                // 按点数排序
                var orderedSuitCards = suitCards.OrderByDescending(c => c.Value == 1 ? 14 : c.Value).ToList();
                // 选取所有3张牌的组合
                for (int i = 0; i < orderedSuitCards.Count; i++)
                {
                    for (int j = i + 1; j < orderedSuitCards.Count; j++)
                    {
                        for (int k = j + 1; k < orderedSuitCards.Count; k++)
                        {
                            var combo = new List<CardAvatar> { orderedSuitCards[i], orderedSuitCards[j], orderedSuitCards[k] };

                            // 判断这三张牌是否构成顺子
                            bool isStraight = IsStraight(combo);

                            // 如果不是顺子，则添加到同花结果中
                            if (!isStraight)
                            {
                                string comboKey = string.Join(",", combo.Select(c => c.GetCardId()).OrderBy(id => id));
                                if (!straightFlushKeys.Contains(comboKey))
                                {
                                    result.Add(combo);
                                }
                            }
                        }
                    }
                }
            }
        }

        // 如果没有鬼牌，直接返回结果
        if (kingCount == 0)
            return result;

        // 2. 查找使用1张鬼牌的同花组合
        if (kingCount >= 1)
        {
            for (int suit = 3; suit >= 0; suit--)
            {
                List<CardAvatar> currentSuitCards = normalCards.Where(c => c.Suit == suit).ToList();
                if (currentSuitCards.Count >= 2) // 需要至少两张同花色普通牌
                {
                    var orderedSuitCards = currentSuitCards.OrderByDescending(c => c.Value == 1 ? 14 : c.Value).ToList();
                    // 遍历所有两张普通牌组合
                    for (int i = 0; i < orderedSuitCards.Count; i++)
                    {
                        for (int j = i + 1; j < orderedSuitCards.Count; j++)
                        {
                            // 检查这两张牌加上任何一张牌是否可能形成顺子
                            List<int> values = new List<int> {
                                orderedSuitCards[i].Value == 1 ? 14 : orderedSuitCards[i].Value,
                                orderedSuitCards[j].Value == 1 ? 14 : orderedSuitCards[j].Value
                            };
                            values.Sort();

                            // 遍历所有鬼牌
                            foreach (var king in kings)
                            {
                                // 红黑模式检查
                                if (isRedBlack &&
                                    !((king.Value == 15 && (suit == 0 || suit == 2)) || // 大鬼配红
                                      (king.Value == 14 && (suit == 1 || suit == 3))))   // 小鬼配黑
                                {
                                    continue; // 跳过不符合规则的鬼牌
                                }

                                var combo = new List<CardAvatar> { orderedSuitCards[i], orderedSuitCards[j], king };

                                // 判断这三张牌是否可能构成顺子
                                bool mightBeStraight = false;

                                // 特殊检查QKA
                                if ((values.Contains(12) && values.Contains(13)) ||
                                    (values.Contains(13) && values.Contains(14)) ||
                                    (values.Contains(12) && values.Contains(14)))
                                {
                                    mightBeStraight = true;
                                }
                                else
                                {
                                    // 普通顺子检查
                                    for (int val = 1; val <= 13; val++)
                                    {
                                        if (CanFormStraightWithValue(values, val))
                                        {
                                            mightBeStraight = true;
                                            break;
                                        }
                                    }
                                }

                                // 如果不可能构成顺子，则添加到同花结果中
                                if (!mightBeStraight)
                                {
                                    string comboKey = string.Join(",", combo.Select(c => c.GetCardId()).OrderBy(id => id));
                                    if (!straightFlushKeys.Contains(comboKey))
                                    {
                                        result.Add(combo);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // 3. 查找使用2张鬼牌的同花组合
        // if (kingCount >= 2)
        // {
        //     for (int suit = 3; suit >= 0; suit--)
        //     {
        //         List<CardAvatar> currentSuitCards = normalCards.Where(c => c.Suit == suit).ToList();
        //         if (currentSuitCards.Count >= 1) // 只需要一张同花色普通牌
        //         {
        //             var orderedSuitCards = currentSuitCards.OrderByDescending(c => c.Value == 1 ? 14 : c.Value).ToList();
        //             // 遍历所有单张普通牌
        //             foreach (var normalCard in orderedSuitCards)
        //             {
        //                 // 遍历所有两张鬼牌的组合
        //                 for (int i = 0; i < kings.Count; i++)
        //                 {
        //                     for (int j = i + 1; j < kings.Count; j++)
        //                     {
        //                         CardAvatar king1 = kings[i];
        //                         CardAvatar king2 = kings[j];

        //                         // 红黑模式检查
        //                         if (isRedBlack)
        //                         {
        //                             bool king1Valid = (king1.Value == 15 && (suit == 0 || suit == 2)) || (king1.Value == 14 && (suit == 1 || suit == 3));
        //                             bool king2Valid = (king2.Value == 15 && (suit == 0 || suit == 2)) || (king2.Value == 14 && (suit == 1 || suit == 3));
        //                             if (!king1Valid || !king2Valid)
        //                             {
        //                                 continue; // 至少一张鬼牌不符合规则
        //                             }
        //                         }

        //                         // 使用无法形成顺子的点数
        //                         // 例如，如果普通牌是5，可以选择8和J作为鬼牌点数，确保不会形成顺子
        //                         int normValue = normalCard.Value == 1 ? 14 : normalCard.Value;
        //                         List<int> safeValues = FindNonStraightValues(normValue);

        //                         if (safeValues.Count >= 2)
        //                         {
        //                             var combo = new List<CardAvatar> { normalCard, king1, king2 };
        //                             string comboKey = string.Join(",", combo.Select(c => c.GetCardId()).OrderBy(id => id));
        //                             if (!straightFlushKeys.Contains(comboKey))
        //                             {
        //                                 result.Add(combo);
        //                             }
        //                         }
        //                     }
        //                 }
        //             }
        //         }
        //     }
        // }

        // 去重
        var uniqueResults = new List<List<CardAvatar>>();
        var seenCombinations = new HashSet<string>();
        foreach (var combo in result)
        {
            // 对组合内的牌进行排序，确保顺序一致性
            var sortedCombo = combo.OrderByDescending(c => c.IsKing ? (c.Value == 15 ? 101 : 100) : (c.Value == 1 ? 14 : c.Value))
                                   .ThenByDescending(c => c.Suit)
                                   .ToList();
            string comboKey = string.Join(",", sortedCombo.Select(c => c.GetCardId()));
            if (seenCombinations.Add(comboKey))
            {
                uniqueResults.Add(sortedCombo); // 添加排序后的组合
            }
        }

        return uniqueResults;
    }

    // 判断一组点数是否可以和给定点数形成顺子
    private bool CanFormStraightWithValue(List<int> values, int newValue)
    {
        List<int> allValues = new List<int>(values);
        allValues.Add(newValue);
        allValues.Sort();

        // 检查是否是连续的
        for (int i = 0; i < allValues.Count - 1; i++)
        {
            if (allValues[i + 1] - allValues[i] > 1)
                return false;
        }

        return allValues[allValues.Count - 1] - allValues[0] <= 2; // 最大差距不超过2
    }

    // 找出不会形成顺子的点数
    private List<int> FindNonStraightValues(int value)
    {
        List<int> result = new List<int>();

        // 避免形成顺子的点数，即与当前点数相差超过2的点数
        for (int i = 1; i <= 13; i++)
        {
            if (Math.Abs(i - value) > 2 && i != value)
            {
                result.Add(i);
            }
        }

        return result;
    }

    // 判断一组牌是否构成顺子
    private bool IsStraight(List<CardAvatar> cards)
    {
        if (cards.Count != 3)
            return false;

        // 处理A的特殊情况(既可以是1，也可以是14)
        List<int> values = new List<int>();
        bool hasAce = false;

        foreach (var card in cards)
        {
            values.Add(card.Value == 1 ? 14 : card.Value); // 将A当作14
            if (card.Value == 1)
                hasAce = true;
        }

        values.Sort();

        // 检查是否是连续的
        if (values[2] - values[1] == 1 && values[1] - values[0] == 1)
            return true;

        // 特殊检查A-2-3顺子
        if (hasAce && values.Contains(2) && values.Contains(3))
            return true;

        // 特殊检查Q-K-A顺子
        if (hasAce && values.Contains(12) && values.Contains(13))
            return true;

        return false;
    }

    // 处理牌型按钮点击事件
    public void OnCardTypeButtonClick(int typeIndex)
    {
        if (Util.IsClickLocked()) return;
        if (isAnimationPlaying) return;
       Sound.PlayEffect(AudioKeys.SOUND_BTN);

        ZjhCardType cardType = (ZjhCardType)typeIndex;

        // 如果是同一牌型，增加索引
        if (cardType == m_LastSelectedCardType)
        {
            if (!m_CardTypeCombinationIndex.ContainsKey(cardType))
            {
                m_CardTypeCombinationIndex[cardType] = 0;
            }
            else
            {
                m_CardTypeCombinationIndex[cardType]++;
            }
        }
        else
        {
            // 如果是不同牌型，重置索引
            m_CardTypeCombinationIndex[cardType] = 0;
        }

        // 更新最后选择的牌型
        m_LastSelectedCardType = cardType;

        // 选择符合牌型的牌
        SelectCardsByType(cardType);
    }

    // 选择特定牌型的牌
    public void SelectCardsByType(ZjhCardType type)
    {
        // 清除当前选择
        ClearSelection();

        // 获取所有可用手牌
        List<int> availableCards = new List<int>();
        List<int> availableIndices = new List<int>();

        for (int i = 0; i < myCards.Length; i++)
        {
            if (myCards[i] != 0)
            {
                availableCards.Add(myCards[i]);
                availableIndices.Add(i);
            }
        }

        if (availableCards.Count < 3)
        {
            // 手牌不足3张，无法组成牌型
            return;
        }

        // 获取当前牌型的组合索引
        int combinationIndex = 0;
        if (m_CardTypeCombinationIndex.ContainsKey(type))
        {
            combinationIndex = m_CardTypeCombinationIndex[type];
        }

        // 获取所有符合该牌型的组合
        List<List<CardAvatar>> allCombinations = GetAllCombinationsByType(availableCards, type);

        // 检查组合是否存在
        if (allCombinations.Count == 0)
        {
            // 无可用组合
            return;
        }

        // 确保索引在有效范围内（循环选择）
        if (combinationIndex >= allCombinations.Count)
        {
            // 将索引设为-1，这样下次点击增加后会变成0，显示第一个组合
            combinationIndex = -1;
            m_CardTypeCombinationIndex[type] = -1;

            // 在循环结束后清空选择，让用户可以看到一个"清空状态"
            ClearSelection();
            return;
        }

        // 获取当前索引对应的组合
        List<CardAvatar> selectedCombination = allCombinations[combinationIndex];

        // 将CardAvatar转回整数值
        List<int> bestCombination = selectedCombination.Select(c => GameUtil.GetInstance().CombineColorAndValue(c.Suit, c.Value)).ToList();

        if (bestCombination != null && bestCombination.Count >= 3)
        {
            selectedCardIndex = -1;

            // 创建已使用索引的集合，避免重复选择
            HashSet<int> usedIndices = new HashSet<int>();

            // 最多选中3张牌
            for (int i = 0; i < Math.Min(3, bestCombination.Count); i++)
            {
                int cardValue = bestCombination[i];
                int handIndex = -1;

                // 找到牌值对应的手牌索引，避免重复选择
                for (int j = 0; j < availableIndices.Count; j++)
                {
                    if (myCards[availableIndices[j]] == cardValue && !usedIndices.Contains(availableIndices[j])
                        && !multiSelectedIndices.Contains(availableIndices[j]))
                    {
                        handIndex = availableIndices[j];
                        usedIndices.Add(handIndex);
                        break;
                    }
                }

                if (handIndex != -1)
                {
                    multiSelectedIndices.Add(handIndex);

                    // 高亮选中的牌
                    Vector2 pos = handCardPositions[handIndex];
                    localCards[handIndex].GetComponent<RectTransform>().anchoredPosition = new Vector2(pos.x, pos.y + m_CardHighlightAmount);
                }
            }
        }
    }

    /// <summary>
    /// 获取所有符合特定牌型的组合
    /// 组牌规则:
    /// 1. 优先根据targetType用普通牌列表normalCards组成result列表
    /// 2. 然后用鬼牌列表kings结合普通牌列表normalCards生成最优的牌组
    ///    - 鬼当任意模式：鬼牌可以变成任意卡牌，包括重复已有的牌
    ///      例如：黑桃A加梅花A加1个小鬼可组成三个A的三条
    ///    - 鬼当红黑模式：小鬼只能配黑色牌(黑桃、梅花)，大鬼只能配红色牌(红桃、方块)
    /// 3. 全部配完后排序
    /// 4. 组合优先级：
    ///    - 首先按最大牌型选择
    ///    - 然后按最大点数
    ///    - 最后按最大花色
    /// 5. 规则限制：
    ///    - 每个鬼牌的组合最多只有两个鬼牌(鬼牌永远是配成最大的牌型需要替换的点数花色)
    ///    - 同花组合中不能包含同花顺（高级牌型不会出现在低级牌型中）
    ///    - 顺子组合中不能包含同花顺
    /// </summary>
    /// <param name="availableCards">可用的牌列表</param>
    /// <param name="targetType">目标牌型</param>
    private List<List<CardAvatar>> GetAllCombinationsByType(List<int> availableCards, ZjhCardType targetType)
    {
        // 将牌转换为CardAvatar对象以便分析
        List<CardAvatar> cardAvatars = availableCards.Select(c => new CardAvatar(c)).ToList();
        List<List<CardAvatar>> validCombinations = new List<List<CardAvatar>>();

        switch (targetType)
        {
            case ZjhCardType.Boom: // 三条
                validCombinations = FindThreeOfKindCombinations(cardAvatars);
                break;
            case ZjhCardType.GoldAlong: // 同花顺
                validCombinations = FindStraightFlushCombinations(cardAvatars);
                break;
            case ZjhCardType.GoldFlower: // 同花
                validCombinations = FindFlushCombinations(cardAvatars);
                break;
            case ZjhCardType.Along: // 顺子
                validCombinations = FindStraightCombinations(cardAvatars);
                break;
            default:
                return new List<List<CardAvatar>>();
        }

        // 根据牌型排序组合
        if (validCombinations.Count > 1)
        {
            validCombinations.Sort((combo1, combo2) =>
            {
                // 将CardAvatar列表转换为int数组
                int[] cards1 = combo1.Select(c => c.GetCardId()).ToArray();
                int[] cards2 = combo2.Select(c => c.GetCardId()).ToArray();
                return -CompareCards(cards1, cards2); // 加上负号使排序方向反向
            });
        }

        return validCombinations;
    }

    // 清除所有选择状态
    public void ClearSelection()
    {
        // 清理视觉状态
        foreach (int handCardIdx in new List<int>(multiSelectedIndices))
        {
            // 停止所有可能正在运行的动画
            RectTransform rectTrans = localCards[handCardIdx]?.GetComponent<RectTransform>();
            if (rectTrans != null)
            {
                DOTween.Kill(rectTrans);
                rectTrans.anchoredPosition = handCardPositions[handCardIdx];
            }
            if (localCards[handCardIdx] != null)
            {
                localCards[handCardIdx].SetImgColor(Color.white);
            }
        }
        multiSelectedIndices.Clear();

        if (selectedCardIndex != -1)
        {
            int pierIndex = selectedCardIndex / 10 - 1;
            int cardIdx = selectedCardIndex % 10;
            // 确保 pierIndex 和 cardIdx 在有效范围内
            if (pierIndex >= 0 && pierIndex < piers.Length &&
                piers[pierIndex].cards != null && cardIdx >= 0 && cardIdx < piers[pierIndex].cards.Length &&
                piers[pierIndex].cards[cardIdx] != null)
            {
                piers[pierIndex].cards[cardIdx].SetImgColor(Color.white);
            }
            selectedCardIndex = -1;
        }

        // 清理拖动状态
        if (m_DragSelectedIndices.Count > 0)
        {
            // 停止所有拖动中卡牌的动画
            foreach (int idx in new List<int>(m_DragSelectedIndices))
            {
                RectTransform rectTrans = localCards[idx]?.GetComponent<RectTransform>();
                if (rectTrans != null)
                {
                    DOTween.Kill(rectTrans);
                    rectTrans.anchoredPosition = handCardPositions[idx];
                }
                if (localCards[idx] != null)
                {
                    localCards[idx].SetImgColor(Color.white);
                }
            }
        }

        // Reset drag state
        m_IsDraggingCard = false;
        m_DragSelectedIndices.Clear();
        m_DragStartIndex = -1;
    }

    // 手牌排序 切换手牌排序方式(按点数 按花色)
    private void SortHandCards()
    {
        // 收集所有有效手牌
        List<int> validCards = new List<int>();
        for (int i = 0; i < myCards.Length; i++)
        {
            if (myCards[i] != 0)
            {
                validCards.Add(myCards[i]);
            }
        }

        // 将整数卡牌转换为CardAvatar对象以便获取点数和花色
        List<CardAvatar> cardAvatars = new List<CardAvatar>();
        foreach (int card in validCards)
        {
            cardAvatars.Add(new CardAvatar(card));
        }

        // 根据当前排序模式选择不同的排序方式
        if (currentSortMode == SortModeBJ.ByValue)
        {
            // 按点数优先排序：
            // 1. 点数从大到小（A当14）
            // 2. 花色从大到小（黑桃>红桃>梅花>方块）
            cardAvatars = cardAvatars.OrderByDescending(c => c.Value == 15 ? 101 : (c.Value == 14 ? 100 : (c.Value == 1 ? 14 : c.Value)))
                                     .ThenByDescending(c => c.Suit)
                                     .ToList();
        }
        else // SortMode.BySuit
        {
            // 按花色优先排序：
            // 1. 花色从大到小（黑桃>红桃>梅花>方块）
            // 2. 点数从大到小（A当14）
            cardAvatars = cardAvatars.OrderByDescending(c => c.Suit)
                                     .ThenByDescending(c => c.Value == 15 ? 101 : (c.Value == 14 ? 100 : (c.Value == 1 ? 14 : c.Value)))
                                     .ToList();
        }

        // 将CardAvatar对象转回整数值
        List<int> sortedCards = cardAvatars.Select(c => GameUtil.GetInstance().CombineColorAndValue(c.Suit, c.Value)).ToList();

        // 重置myCards数组
        for (int i = 0; i < myCards.Length; i++)
        {
            myCards[i] = (i < sortedCards.Count) ? sortedCards[i] : 0;
        }
    }

    public void ButtonClick(int type)
    {
        if (Util.IsClickLocked()) return;
        if (isAnimationPlaying) return;
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        ClearSelection();
        switch (type)
        {
            case 0:
                AutoCompareCardRq();
                break;
            case 1:
                // 弃牌
                GiveUpCards();
                break;
            case 2:
                // 完成配牌
                CompleteCardArrangement();
                break;
            case 3:
                // 清空道牌
                foreach (var pier in piers)
                {
                    pier.ResetPier();
                }
                break;
            case 4:
                // 切换手牌排序方式(按点数/按花色)
                // 切换排序模式
                currentSortMode = currentSortMode == SortModeBJ.ByValue ? SortModeBJ.BySuit : SortModeBJ.ByValue;
                sortText.text = currentSortMode == SortModeBJ.ByValue ? "按花色" : "按点数";
                UpdateHandCardsUI();
                ClearSelection();
                break;
        }
    }

    /// <summary>
    /// 如果配牌不符合规则 自动排序道牌
    /// 具体规则:
    /// 1.如果是9张牌 在组第二道牌时需要自动把剩余的手牌移入未完成组牌的道中 并检测是否符合规则(尾墩必须大于中墩，中墩必须大于头墩) 
    /// 如果不符合 不要改变道牌数据 就按照当前道牌数据排序
    /// 2.如果是10张牌 在组完三道牌后检测 检测是否符合规则(尾墩必须大于中墩，中墩必须大于头墩) 
    /// 如果不符合 不要改变道牌数据 就按照当前道牌数据排序
    /// </summary>
    public void AutoSortPierCards()
    {
        // 获取手牌数量
        int handCardCount = 0;
        for (int i = 0; i < myCards.Length; i++)
        {
            if (myCards[i] != 0)
                handCardCount++;
        }

        // 获取各道已有牌数
        int headPierCount = piers[0].GetCardCount();
        int midPierCount = piers[1].GetCardCount();
        int tailPierCount = piers[2].GetCardCount();

        // 规则1：9张牌时的情况
        if (handCardType == 9)
        {
            // 检查三道中的两道已完成配牌
            int completedPiers = 0;
            if (headPierCount == 3) completedPiers++;
            if (midPierCount == 3) completedPiers++;
            if (tailPierCount == 3) completedPiers++;

            // 如果已完成两道配牌
            if (completedPiers >= 2)
            {
                // 找到未完成的那一道
                int incompletePierIndex = -1;
                if (headPierCount < 3) incompletePierIndex = 0;
                else if (midPierCount < 3) incompletePierIndex = 1;
                else if (tailPierCount < 3) incompletePierIndex = 2;

                if (incompletePierIndex != -1)
                {
                    // 将剩余手牌移入未完成的道中
                    List<int> remainingCards = new List<int>();
                    for (int i = 0; i < myCards.Length; i++)
                    {
                        if (myCards[i] != 0)
                        {
                            remainingCards.Add(myCards[i]);
                            myCards[i] = 0;
                        }
                    }

                    // 更新未完成道的卡牌
                    for (int i = 0; i < remainingCards.Count; i++)
                    {
                        int emptySlot = -1;
                        for (int j = 0; j < piers[incompletePierIndex].cardDatas.Length; j++)
                        {
                            if (piers[incompletePierIndex].cardDatas[j] == 0)
                            {
                                emptySlot = j;
                                break;
                            }
                        }
                        if (emptySlot != -1)
                        {
                            piers[incompletePierIndex].cardDatas[emptySlot] = remainingCards[i];
                        }
                    }
                }
                // 检查配牌是否符合规则
                if (!ValidateCardArrangement())
                {
                    // 配牌不符合规则，按照当前数据重新排序
                    GF.StaticUI.ShowError("配牌不符合规则，自动调整顺序");

                    // 获取所有道牌数据
                    List<int[]> pierCardsList = new List<int[]>();
                    for (int i = 0; i < piers.Length; i++)
                    {
                        pierCardsList.Add(piers[i].cardDatas.ToArray());
                    }

                    // 按照牌型大小排序
                    pierCardsList.Sort((a, b) => CompareCards(a, b));

                    // 更新道牌数据并刷新UI
                    for (int i = 0; i < piers.Length; i++)
                    {
                        piers[i].ForceUpdatePierCards(pierCardsList[i]);
                    }
                    UpdateHandCardsUI();
                }
                else
                {
                    // 更新UI显示
                    piers[incompletePierIndex].UpdateUI();
                    UpdateHandCardsUI();
                }
            }

        }
        // 规则2：10张牌时的情况
        else if (handCardType == 10)
        {
            // 检查三道牌是否已配置完成
            if (IsAllPiersCompleted())
            {
                // 验证配牌是否符合规则
                if (!ValidateCardArrangement())
                {
                    // 配牌不符合规则，按照当前数据重新排序
                    GF.StaticUI.ShowError("配牌不符合规则，自动调整顺序");

                    // 获取所有道牌数据
                    List<int[]> pierCardsList = new List<int[]>();
                    for (int i = 0; i < piers.Length; i++)
                    {
                        pierCardsList.Add(piers[i].cardDatas.ToArray());
                    }

                    // 按照牌型大小排序
                    pierCardsList.Sort((a, b) => CompareCards(a, b));

                    // 更新道牌数据 并刷新ui
                    for (int i = 0; i < piers.Length; i++)
                    {
                        piers[i].ForceUpdatePierCards(pierCardsList[i]);
                    }
                    UpdateHandCardsUI();
                }
            }
        }
    }

    // 弃牌功能
    private void GiveUpCards()
    {
        Debug.Log("玩家选择弃牌");
        Util.GetInstance().OpenConfirmationDialog("弃牌", "确定要弃牌吗?", () =>
        {
            Msg_CompareCardGiveRq req = MessagePool.Instance.Fetch<Msg_CompareCardGiveRq>();
            HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                (HotfixNetworkComponent.AccountClientName, MessageID.Msg_CompareCardGiveRq, req);
        });
    }

    // 完成配牌功能
    private void CompleteCardArrangement()
    {
        // 检查三道是否都已配置完成
        if (!IsAllPiersCompleted())
        {
            // 提示用户完成所有三道配牌
            GF.LogWarning("请先完成配牌");
            GF.StaticUI.ShowError("请先完成配牌");
            return;
        }

        // 检查配牌是否符合头道<中道<尾道规则
        if (!ValidateCardArrangement())
        {
            // 提示用户配牌不符合规则
            GF.LogWarning("配牌不符合规则，尾墩必须大于中墩，中墩必须大于头墩");
            GF.StaticUI.ShowError("配牌不符合规则，尾墩必须大于中墩，中墩必须大于头墩");
            return;
        }

        // 假设完成配牌消息为标准命名方式，使用已知的消息类型和结构
        Msg_CompareCardRq req = MessagePool.Instance.Fetch<Msg_CompareCardRq>();

        // 为头道、中道、尾道分别创建ListInt
        for (int i = 0; i < piers.Length; i++)
        {
            ListInt pierCards = MessagePool.Instance.Fetch<ListInt>();
            for (int j = 0; j < piers[i].cardDatas.Length; j++)
            {
                if (piers[i].cardDatas[j] != 0)
                {
                    pierCards.Val.Add(piers[i].cardDatas[j]);
                }
            }

            // 添加到请求中
            req.ComposeCard.Add(pierCards);
        }
        // 发送请求
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_CompareCardRq, req);
    }

    // 检查是否所有三道都已配置完成
    private bool IsAllPiersCompleted()
    {
        foreach (var pier in piers)
        {
            if (!pier.IsComplete())
            {
                return false;
            }
        }
        return true;
    }

    // 验证配牌是否符合规则（尾道>中道>头道）
    private bool ValidateCardArrangement()
    {
        // 比较尾道和中道的牌型
        int tailMidCompare = CompareCards(piers[2].cardDatas, piers[1].cardDatas);
        if (tailMidCompare <= 0)
        {
            return false; // 尾道必须大于中道
        }

        // 比较中道和头道的牌型
        int midHeadCompare = CompareCards(piers[1].cardDatas, piers[0].cardDatas);
        if (midHeadCompare <= 0)
        {
            return false; // 中道必须大于头道
        }

        return true;
    }

    // 比较两组牌的大小
    private int CompareCards(int[] cards1, int[] cards2)
    {
        // 获取牌型
        bool isRedBlack = currentGhostMode == GhostMode.RedBlack;
        // foreach (var card in cards1)
        // {
        //     GF.LogInfo("cards1" + card);
        // }
        // foreach (var card in cards2)
        // {
        //     GF.LogInfo("cards2" + card);
        // }
        ZjhCardType type1 = GameConstants.GetCardTypeByCards(cards1, isRedBlack, out List<CardAvatar> cardsCache1);
        ZjhCardType type2 = GameConstants.GetCardTypeByCards(cards2, isRedBlack, out List<CardAvatar> cardsCache2);
        // foreach (var card in cardsCache1)
        // {
        //     GF.LogInfo("cardsCache1" + card.ToString());
        // }
        // foreach (var card in cardsCache2)
        // {
        //     GF.LogInfo("cardsCache2" + card.ToString());
        // }
        // 首先比较牌型
        if (type1 != type2)
        {
            return (int)type1 - (int)type2;
        }

        // 根据不同牌型比较大小
        switch (type1)
        {
            case ZjhCardType.Boom:
                return CompareBoom(cardsCache1, cardsCache2);
            case ZjhCardType.GoldAlong: // 同花顺
                return CompareStraightFlush(cardsCache1, cardsCache2);
            case ZjhCardType.GoldFlower: // 同花
                return CompareFlush(cardsCache1, cardsCache2);
            case ZjhCardType.Along: // 顺子
                return CompareStraight(cardsCache1, cardsCache2);
            case ZjhCardType.Pair:
                return ComparePair(cardsCache1, cardsCache2);
            default:
                return CompareHighCard(cardsCache1, cardsCache2);
        }
    }

    public void AutoCompareCardRq()
    {
        Debug.Log("自动配牌");

        Msg_AutoCompareCardRq req = MessagePool.Instance.Fetch<Msg_AutoCompareCardRq>();
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_AutoCompareCardRq, req);
    }

    // 自动配牌返回(后台返回的数据 不需要验证配牌逻辑性 按顺序填充即可)
    public void AutoCompareCardRs(ChickenCards[] chickenCards)
    {
        m_LastSelectedCardType = ZjhCardType.High; // 自动配牌后重置
        foreach (var pier in piers)
        {
            pier.ResetPier();
        }
        // 按尾道>中道>头道排序（0=头道，1=中道，2=尾道）
        var sorted = chickenCards.OrderBy(c => c.Type).ToArray();

        // 创建动画序列
        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(gameObject);

        // 用于跟踪已使用的手牌索引
        HashSet<int> usedCardIndices = new HashSet<int>();

        // 对每个道进行设置，添加动画
        for (int i = 0; i < sorted.Length; i++)
        {
            int pierIndex = i;
            var cards = sorted[i].Cards;
            // 保存到道牌数据
            for (int j = 0; j < cards.Count; j++)
            {
                int pierCardIndex = j;
                int cardValue = cards[j];

                // 查找未使用的对应牌值的卡牌
                int cardIndex = -1;
                for (int k = 0; k < myCards.Length; k++)
                {
                    if (myCards[k] == cardValue && !usedCardIndices.Contains(k))
                    {
                        cardIndex = k;
                        break;
                    }
                }

                if (cardIndex != -1)
                {
                    int finalCardIndex = cardIndex;
                    usedCardIndices.Add(cardIndex);

                    sequence.AppendCallback(() =>
                    {
                        // 设置该牌到对应的道位置
                        piers[pierIndex].SetCard(pierCardIndex, localCards[finalCardIndex], false);
                        // 从手牌中移除已配置的牌
                        myCards[finalCardIndex] = 0;
                    }).AppendInterval(0.1f); // 增加间隔时间，确保动画流畅
                }
            }
        }

        // 完成所有道的设置后，一次性刷新所有道牌UI和手牌显示
        sequence.AppendCallback(() =>
        {
            // 一次性更新所有道牌UI
            for (int i = 0; i < piers.Length; i++)
            {
                piers[i].UpdateUI(false);
            }

            UpdateHandCardsUI();

            // 动画结束，重置状态并启用按钮
            isAnimationPlaying = false;
        });

        // 播放前设置动画状态
        isAnimationPlaying = true;

        // 播放整个序列
        sequence.Play();
    }

    public void ShowCountdown(int seconds)
    {
        countdownCtr.gameObject.SetActive(true);
        countdownCtr.ResetAndStartCountDown(seconds, seconds, true);
        ShowLocalOP();
    }

    public void HideCountdown()
    {
        countdownCtr.Pause();
        countdownCtr.gameObject.SetActive(false);
    }

    //重置所有信息
    public void ResetLocalOP()
    {
        GF.LogInfo("ResetLocalOP");
        DOTween.Kill(gameObject);
        if (localMask.activeSelf == false) return;
        HideCountdown();
        localMask.SetActive(false);
        localButtons.SetActive(false);

        // 重置动画状态
        isAnimationPlaying = false;
        m_LastSelectedCardType = ZjhCardType.High;
        // 重置排序方式为默认
        currentSortMode = SortModeBJ.ByValue;

        // 隐藏牌型按钮
        顺子.gameObject.SetActive(false);
        同花.gameObject.SetActive(false);
        同花顺.gameObject.SetActive(false);
        三条.gameObject.SetActive(false);

        for (int i = 0; i < localCards.Length; i++)
        {
            localCards[i].Init(-1);
            localCards[i].gameObject.SetActive(false);
            localCards[i].GetComponent<RectTransform>().anchoredPosition = handCardPositions[i];
        }
        foreach (var pier in piers)
        {
            pier.ResetPier();
        }
    }

    public void ClearLocalOP()
    {
        GF.LogInfo("ClearLocalOP");
        DOTween.Kill(gameObject);
        HideCountdown();
        localMask.SetActive(false);
        localButtons.SetActive(false);

        // 重置动画状态
        isAnimationPlaying = false;
        m_LastSelectedCardType = ZjhCardType.High;
        // 重置排序方式为默认
        currentSortMode = SortModeBJ.ByValue;

        for (int i = 0; i < localCards.Length; i++)
        {
            localCards[i].Init(-1);
            localCards[i].gameObject.SetActive(false);
            localCards[i].GetComponent<RectTransform>().anchoredPosition = handCardPositions[i];
            DOTween.Kill(localCards[i].gameObject);
        }
        foreach (var pier in piers)
        {
            pier.ClearPier();
        }
    }

    /// <summary>
    /// 使用鬼牌查找顺子组合
    /// </summary>
    /// <param name="suitCards">普通牌列表</param>
    /// <param name="kings">鬼牌列表</param>
    /// <param name="availableKings">可用鬼牌数量</param>
    /// <param name="isRedBlack">是否为鬼当红黑模式</param>
    /// <returns>顺子组合列表</returns>
    private List<List<CardAvatar>> FindStraightCombinationsWithKings(List<CardAvatar> suitCards, List<CardAvatar> kings, int availableKings, bool isRedBlack)
    {
        List<List<CardAvatar>> result = new List<List<CardAvatar>>();

        // 确保最多只能使用1张鬼牌
        availableKings = Math.Min(availableKings, 1);

        // 如果没有普通牌但有鬼牌，考虑使用纯鬼牌组成顺子
        if (suitCards.Count == 0)
        {
            // 允许使用两张鬼牌，需要至少有两张鬼牌才能组成顺子
            if (availableKings == 2 && kings.Count >= 2)
            {
                List<CardAvatar> combo = new List<CardAvatar>();
                for (int i = 0; i < 2; i++) // 最多只添加2张鬼牌
                {
                    if (i < kings.Count)
                    {
                        combo.Add(kings[i]);
                    }
                }

                // 组合中有2张牌时添加
                if (combo.Count == 2)
                {
                    result.Add(combo);
                }
            }
            return result;
        }

        // 所有牌值，将A同时当作1和14
        List<int> values = new List<int>();
        bool hasAce = false;

        foreach (var card in suitCards)
        {
            if (card.Value == 1)
            {
                hasAce = true;
                values.Add(1);  // A当1
                values.Add(14); // A当14
            }
            else
            {
                values.Add(card.Value);
            }
        }

        // 去重并排序
        List<int> uniqueValues = values.Distinct().OrderBy(v => v).ToList();

        // 检查所有可能的顺子组合
        for (int startVal = 1; startVal <= 12; startVal++)
        {
            // 检查从startVal开始的连续三个数
            int[] straight = new int[] { startVal, startVal + 1, startVal + 2 };

            // 计算需要多少张鬼牌来完成这个顺子
            int kingsNeeded = 0;
            List<int> missingPositions = new List<int>();

            for (int i = 0; i < 3; i++)
            {
                if (!uniqueValues.Contains(straight[i]))
                {
                    kingsNeeded++;
                    missingPositions.Add(i);
                }
            }

            // 根据规则，每个组合最多只能使用两张鬼牌
            if (kingsNeeded <= availableKings)
            {
                List<CardAvatar> combo = new List<CardAvatar>();

                // 添加普通牌
                for (int i = 0; i < 3; i++)
                {
                    if (uniqueValues.Contains(straight[i]))
                    {
                        // 找到对应点数的牌
                        CardAvatar card = null;

                        if (straight[i] == 14 && hasAce)
                        {
                            // 如果是A（点数为14），找点数为1的牌
                            card = suitCards.FirstOrDefault(c => c.Value == 1);
                        }
                        else
                        {
                            card = suitCards.FirstOrDefault(c => c.Value == straight[i]);
                        }

                        if (card != null)
                        {
                            combo.Add(card);
                        }
                    }
                }

                // 添加鬼牌，确保不超过2张
                int kingIndex = 0;
                foreach (int pos in missingPositions)
                {
                    if (kingIndex >= 2) // 最多只添加2张鬼牌
                        break;

                    // 在鬼当红黑模式下，根据花色限制使用鬼牌
                    if (isRedBlack)
                    {
                        bool needsRedKing = false;

                        // 根据牌的花色确定是否需要红色鬼牌
                        // 如果是顺子，通常花色无关，但为了保持一致性，我们根据第一张普通牌的花色决定
                        if (combo.Count > 0)
                        {
                            needsRedKing = (combo[0].Suit == 0 || combo[0].Suit == 2); // 方块或红桃需要大鬼
                        }
                        else
                        {
                            // 没有普通牌时，优先使用大鬼（红色）
                            needsRedKing = true;
                        }

                        CardAvatar kingToUse = null;

                        if (needsRedKing)
                        {
                            // 使用大鬼（值为15）
                            kingToUse = kings.FirstOrDefault(k => k.Value == 15);
                        }
                        else
                        {
                            // 使用小鬼（值为14）
                            kingToUse = kings.FirstOrDefault(k => k.Value == 14);
                        }

                        // 如果找不到对应的鬼牌，使用任意可用的鬼牌
                        if (kingToUse == null && kings.Count > kingIndex)
                        {
                            kingToUse = kings[kingIndex];
                        }

                        if (kingToUse != null)
                        {
                            combo.Add(kingToUse);
                            kingIndex++;
                        }
                    }
                    else
                    {
                        // 鬼当任意模式，直接使用鬼牌
                        if (kingIndex < kings.Count)
                        {
                            combo.Add(kings[kingIndex]);
                            kingIndex++;
                        }
                    }
                }

                // 如果组合中有3张牌，则是有效顺子
                if (combo.Count == 3)
                {
                    result.Add(combo);
                }
            }
        }

        // 按顺子大小排序（最大的在前）
        result.Sort((a, b) =>
        {
            int maxValA = a.Max(c => c.IsKing ? (c.Value == 15 ? 15 : 14) : (c.Value == 1 ? 14 : c.Value));
            int maxValB = b.Max(c => c.IsKing ? (c.Value == 15 ? 15 : 14) : (c.Value == 1 ? 14 : c.Value));

            if (maxValA != maxValB)
                return maxValB.CompareTo(maxValA);

            // 如果最大点数相同，比较花色
            int maxSuitA = a.Where(c => !c.IsKing).Count() > 0 ?
                a.Where(c => !c.IsKing).Max(c => c.Suit) : 0;
            int maxSuitB = b.Where(c => !c.IsKing).Count() > 0 ?
                b.Where(c => !c.IsKing).Max(c => c.Suit) : 0;

            return maxSuitB.CompareTo(maxSuitA);
        });

        return result;
    }

    /// <summary>
    /// 比较三条牌组大小
    /// </summary>
    private int CompareBoom(List<CardAvatar> combo1, List<CardAvatar> combo2)
    {
        // 获取三条的点数
        int value1 = GetThreeOfKindValue(combo1);
        int value2 = GetThreeOfKindValue(combo2);

        // A当14点
        if (value1 == 1) value1 = 14;
        if (value2 == 1) value2 = 14;

        // 点数相同则比较花色
        if (value1 == value2)
        {
            return GetHighestSuit(combo1) - GetHighestSuit(combo2);
        }

        return value1 - value2;
    }

    /// <summary>
    /// 比较同花顺
    /// </summary>
    private int CompareStraightFlush(List<CardAvatar> combo1, List<CardAvatar> combo2)
    {
        // 同花顺比较：先比较顺子大小，再比较花色
        int rank1 = GetStraightRank(combo1);
        int rank2 = GetStraightRank(combo2);

        if (rank1 != rank2)
        {
            return rank1 - rank2; // 比较排名点数
        }

        // 排名点数相同，比较最高花色
        return GetHighestSuit(combo1) - GetHighestSuit(combo2);
    }

    /// <summary>
    /// 比较同花
    /// </summary>
    private int CompareFlush(List<CardAvatar> combo1, List<CardAvatar> combo2)
    {
        // 同花比较：先比较牌值大小
        List<int> values1 = GetSortedValues(combo1);
        List<int> values2 = GetSortedValues(combo2);

        for (int i = 0; i < Math.Min(values1.Count, values2.Count); i++)
        {
            if (values1[i] != values2[i])
            {
                return values1[i] - values2[i];
            }
        }

        // 牌值相同，比较最高花色
        return GetHighestSuit(combo1) - GetHighestSuit(combo2);
    }

    /// <summary>
    /// 比较顺子
    /// </summary>
    private int CompareStraight(List<CardAvatar> combo1, List<CardAvatar> combo2)
    {
        // 顺子比较：先比较顺子大小，再比较花色
        int rank1 = GetStraightRank(combo1);
        int rank2 = GetStraightRank(combo2);

        if (rank1 != rank2)
        {
            return rank1 - rank2; // 比较排名点数
        }

        // 排名点数相同，比较最高花色
        return GetHighestSuit(combo1) - GetHighestSuit(combo2);
    }

    /// <summary>
    /// 获取顺子的排名点数（A23返回3，QKA返回14，其他返回最大牌点数）
    /// </summary>
    private int GetStraightRank(List<CardAvatar> combo)
    {
        if (IsA23(combo))
        {
            return 3; // A23 特殊排名点数，最小
        }
        // 正常顺子或QKA，返回最大牌的点数 (Ace视为14)
        return GetMaxValue(combo);
    }

    /// <summary>
    /// 判断一个组合是否为 A23 顺子 (考虑鬼牌)
    /// </summary>
    private bool IsA23(List<CardAvatar> combo)
    {
        // 注意：此方法假设 combo 已经是有效的顺子
        if (combo == null || combo.Count != 3) return false;

        int kingCount = combo.Count(c => c.IsKing);
        List<int> normalValues = combo.Where(c => !c.IsKing).Select(c => c.Value).ToList();

        bool hasAce = normalValues.Contains(1) || kingCount > 0;
        bool hasTwo = normalValues.Contains(2) || kingCount > (normalValues.Contains(1) ? 1 : 0);
        bool hasThree = normalValues.Contains(3) || kingCount > (normalValues.Contains(1) ? 1 : 0) + (normalValues.Contains(2) ? 1 : 0);

        // A23顺子，其第二大的牌一定是3
        // 通过获取牌组第二大的牌的点数是否为3来判断
        var sortedVals = combo
            .Select(c => c.IsKing ? -1 : (c.Value == 1 ? 14 : c.Value)) // 转换点数，鬼牌视为-1以便不影响排序
            .Where(v => v != -1) // 过滤掉鬼牌
            .OrderByDescending(v => v)
            .ToList();

        // 需要处理纯鬼牌组成的顺子，或者鬼牌补齐的顺子
        // 一个更可靠的方法是检查最大牌是否为3（当A视为1时）
        int maxValueWithAceAs1 = 0;
        int kingsAvailable = kingCount;
        bool has1 = normalValues.Contains(1);
        bool has2 = normalValues.Contains(2);
        bool has3 = normalValues.Contains(3);

        if (has1) maxValueWithAceAs1 = 1;
        if (has2) maxValueWithAceAs1 = Math.Max(maxValueWithAceAs1, 2);
        if (has3) maxValueWithAceAs1 = Math.Max(maxValueWithAceAs1, 3);

        // 用鬼牌补齐
        if (!has1 && kingsAvailable > 0) { has1 = true; kingsAvailable--; }
        if (!has2 && kingsAvailable > 0) { has2 = true; kingsAvailable--; }
        if (!has3 && kingsAvailable > 0) { has3 = true; kingsAvailable--; }

        return has1 && has2 && has3;
    }

    /// <summary>
    /// 比较对子牌组大小
    /// </summary>
    private int ComparePair(List<CardAvatar> combo1, List<CardAvatar> combo2)
    {
        // 获取对子的点数
        int pairValue1 = GetPairValue(combo1);
        int pairValue2 = GetPairValue(combo2);

        // A当14点
        if (pairValue1 == 1) pairValue1 = 14;
        if (pairValue2 == 1) pairValue2 = 14;

        // 对子点数不同
        if (pairValue1 != pairValue2)
        {
            return pairValue1 - pairValue2;
        }

        // 点数相同，比较单张牌
        int highValue1 = GetHighCardValue(combo1, pairValue1);
        int highValue2 = GetHighCardValue(combo2, pairValue2);

        // A当14点
        if (highValue1 == 1) highValue1 = 14;
        if (highValue2 == 1) highValue2 = 14;

        // 单张点数不同
        if (highValue1 != highValue2)
        {
            return highValue1 - highValue2;
        }

        // 点数都相同，比较花色
        return GetHighestSuit(combo1) - GetHighestSuit(combo2);
    }

    /// <summary>
    /// 比较高牌牌组大小
    /// </summary>
    private int CompareHighCard(List<CardAvatar> combo1, List<CardAvatar> combo2)
    {
        // 从高到低比较每张牌
        List<int> values1 = GetSortedValues(combo1);
        List<int> values2 = GetSortedValues(combo2);

        for (int i = 0; i < Math.Min(values1.Count, values2.Count); i++)
        {
            if (values1[i] != values2[i])
            {
                return values1[i] - values2[i];
            }
        }

        // 点数都相同，比较花色
        return GetHighestSuit(combo1) - GetHighestSuit(combo2);
    }

    /// <summary>
    /// 获取三条的点数
    /// </summary>
    private int GetThreeOfKindValue(List<CardAvatar> combo)
    {
        var valueGroups = combo.Where(c => !c.IsKing)
                               .GroupBy(c => c.Value)
                               .OrderByDescending(g => g.Count())
                               .ToList();

        // 如果有成对的牌，返回该牌的点数
        if (valueGroups.Count > 0 && valueGroups[0].Count() >= 2)
        {
            return valueGroups[0].Key;
        }

        // 如果没有成对的牌（可能是因为有鬼牌），返回最大点数
        return combo.Where(c => !c.IsKing)
                   .Select(c => c.Value == 1 ? 14 : c.Value)
                   .DefaultIfEmpty(0)
                   .Max();
    }

    /// <summary>
    /// 获取对子的点数
    /// </summary>
    private int GetPairValue(List<CardAvatar> combo)
    {
        var valueGroups = combo.Where(c => !c.IsKing)
                               .GroupBy(c => c.Value)
                               .OrderByDescending(g => g.Count())
                               .ToList();

        // 如果有成对的牌，返回该牌的点数
        if (valueGroups.Count > 0 && valueGroups[0].Count() >= 2)
        {
            return valueGroups[0].Key;
        }

        // 如果没有成对的牌（可能是因为有鬼牌），返回最大点数
        return combo.Where(c => !c.IsKing)
                   .Select(c => c.Value == 1 ? 14 : c.Value)
                   .DefaultIfEmpty(0)
                   .Max();
    }

    /// <summary>
    /// 获取单张牌的点数（排除对子点数）
    /// </summary>
    private int GetHighCardValue(List<CardAvatar> combo, int pairValue)
    {
        return combo.Where(c => !c.IsKing && c.Value != pairValue)
                   .Select(c => c.Value == 1 ? 14 : c.Value)
                   .DefaultIfEmpty(0)
                   .Max();
    }

    /// <summary>
    /// 获取牌组中的最大点数
    /// </summary>
    private int GetMaxValue(List<CardAvatar> combo)
    {
        return combo.Where(c => !c.IsKing)
                   .Select(c => c.Value == 1 ? 14 : c.Value)
                   .DefaultIfEmpty(0)
                   .Max();
    }

    /// <summary>
    /// 获取排序后的点数列表（从大到小）
    /// </summary>
    private List<int> GetSortedValues(List<CardAvatar> combo)
    {
        return combo.Where(c => !c.IsKing)
                   .Select(c => c.Value == 1 ? 14 : c.Value)
                   .OrderByDescending(v => v)
                   .ToList();
    }

    // 寻找顺子组合
    private List<List<CardAvatar>> FindStraightCombinations(List<CardAvatar> cards)
    {
        bool isRedBlack = currentGhostMode == GhostMode.RedBlack;

        // 分离鬼牌和普通牌
        List<CardAvatar> kings = cards.Where(c => c.IsKing).ToList();
        List<CardAvatar> normalCards = cards.Where(c => !c.IsKing).ToList();
        int kingCount = kings.Count;

        // 获取所有可能的顺子组合
        List<List<CardAvatar>> allStraights = FindStraightCombinationsWithKings(normalCards, kings, kingCount, isRedBlack);

        // 先找出所有同花顺组合，用于后续排除
        List<List<CardAvatar>> straightFlushCombos = FindStraightFlushCombinations(cards);

        // 将同花顺组合的ID存入HashSet，便于快速查找
        HashSet<string> straightFlushKeys = new HashSet<string>();
        foreach (var combo in straightFlushCombos)
        {
            string key = string.Join(",", combo.Select(c => c.GetCardId()).OrderBy(id => id));
            straightFlushKeys.Add(key);
        }

        // 过滤掉同花顺组合
        List<List<CardAvatar>> result = new List<List<CardAvatar>>();
        foreach (var straight in allStraights)
        {
            string comboKey = string.Join(",", straight.Select(c => c.GetCardId()).OrderBy(id => id));
            if (!straightFlushKeys.Contains(comboKey))
            {
                result.Add(straight);
            }
        }

        return result;
    }

    #region Card Interaction Handlers

    public void HandleCardPointerDown(int cardIndex)
    {
        if (isAnimationPlaying) return;

        if (cardIndex < 0 || cardIndex >= localCards.Length || !localCards[cardIndex].gameObject.activeSelf) return;

        m_LastSelectedCardType = ZjhCardType.High; // 重置牌型提示
       Sound.PlayEffect(AudioKeys.SOUND_BTN);

        if (selectedCardIndex != -1) // 模式：已选中一张道牌，准备交换
        {
            // 用户在已选中一张道牌的情况下，点击了一张手牌 -> 执行交换
            SwapHandCardOrPierCard(cardIndex, selectedCardIndex);
            ClearSelection(); // 清除道牌选择和拖动状态
        }
        else // 模式：未选中道牌，进行手牌选择/拖动选择
        {
            m_IsDraggingCard = true;
            m_LastPointerPosition = Input.mousePosition;
            m_DragSelectedIndices.Clear();
            m_DragStartIndex = -1;

            // 选中第一张牌
            ProcessCardSelection(cardIndex);
        }
    }

    public void HandleCardPointerEnter(int cardIndex)
    {
        if (isAnimationPlaying || !m_IsDraggingCard || selectedCardIndex != -1) return;

        if (cardIndex < 0 || cardIndex >= localCards.Length || !localCards[cardIndex].gameObject.activeSelf) return;

        // 只有在鼠标/触摸明显移动时才处理，防止轻微抖动误触发
        if (Vector3.Distance(Input.mousePosition, m_LastPointerPosition) > 5f)
        {
            m_LastPointerPosition = Input.mousePosition;
            ProcessCardSelection(cardIndex);
        }
    }

    public void HandleCardPointerUp(int cardIndex)
    {
        if (isAnimationPlaying) return;
        if (!m_IsDraggingCard) return;

        DragEnd();
    }

    private void ProcessCardSelection(int cardIndex)
    {
        if (!localCards[cardIndex].gameObject.activeSelf) return; // 确保牌是可见的

        // 如果是第一次点击开始拖动，记录起始索引
        if (m_DragStartIndex == -1)
        {
            m_DragStartIndex = cardIndex;

            // 应用视觉效果
            RectTransform rectTrans = localCards[cardIndex].GetComponent<RectTransform>();
            Vector2 originalPos = handCardPositions[cardIndex];

            // 停止之前的动画
            DOTween.Kill(rectTrans);

            localCards[cardIndex].SetImgColor(new Color(0.8f, 0.8f, 0.8f, 1f));

            m_DragSelectedIndices.Add(cardIndex);
            return;
        }

        // 清除所有之前的选择（保留起始点）
        foreach (int idx in new List<int>(m_DragSelectedIndices))
        {
            if (idx != m_DragStartIndex)
            {
                if (localCards[idx] != null)
                {
                    localCards[idx].SetImgColor(Color.white);
                }

                m_DragSelectedIndices.Remove(idx);
            }
        }

        // 确定选择范围（支持双向）
        int startIdx = Math.Min(m_DragStartIndex, cardIndex);
        int endIdx = Math.Max(m_DragStartIndex, cardIndex);

        // 选择范围内的所有有效卡牌
        for (int i = startIdx; i <= endIdx; i++)
        {
            if (i < localCards.Length && localCards[i] != null &&
                localCards[i].gameObject.activeSelf && !m_DragSelectedIndices.Contains(i))
            {
                // 应用视觉效果
                RectTransform rectTrans = localCards[i].GetComponent<RectTransform>();
                Vector2 originalPos = handCardPositions[i];

                // 停止之前的动画
                DOTween.Kill(rectTrans);

                localCards[i].SetImgColor(new Color(0.8f, 0.8f, 0.8f, 1f));

                m_DragSelectedIndices.Add(i);
            }
        }
    }

    public void DragEnd()
    {
        if (!m_IsDraggingCard) return;

        if (m_DragSelectedIndices.Count > 0)
        {
            Sequence sequence = DOTween.Sequence();

            // 处理所有拖动选中的卡牌
            foreach (int idx in new List<int>(m_DragSelectedIndices))
            {
                // 跳过无效的索引
                if (idx >= localCards.Length || localCards[idx] == null || !localCards[idx].gameObject.activeSelf)
                {
                    m_DragSelectedIndices.Remove(idx);
                    continue;
                }

                RectTransform rectTrans = localCards[idx].GetComponent<RectTransform>();
                // 停止之前的动画
                DOTween.Kill(rectTrans);

                // 切换多选状态
                if (multiSelectedIndices.Contains(idx))
                {
                    // 已在多选列表，移除选择
                    multiSelectedIndices.Remove(idx);

                    // 恢复颜色和原始位置
                    localCards[idx].SetImgColor(Color.white);

                    sequence.Join(DOTween.To(
                        () => rectTrans.anchoredPosition,
                        x => rectTrans.anchoredPosition = x,
                        handCardPositions[idx],
                        0.15f
                    ).SetEase(Ease.OutQuad));
                }
                else
                {
                    // 不在多选列表，添加到多选
                    multiSelectedIndices.Add(idx);

                    // 恢复颜色并提升位置
                    localCards[idx].SetImgColor(Color.white);

                    sequence.Join(DOTween.To(
                        () => rectTrans.anchoredPosition,
                        x => rectTrans.anchoredPosition = x,
                        new Vector2(handCardPositions[idx].x, handCardPositions[idx].y + m_CardHighlightAmount),
                        0.15f
                    ).SetEase(Ease.OutQuad));
                }
            }

            // 添加完成回调，确保状态同步
            sequence.OnComplete(() =>
            {
                // 验证每张卡牌的位置是否与其在multiSelectedIndices中的状态一致
                for (int i = 0; i < localCards.Length; i++)
                {
                    if (localCards[i] == null || !localCards[i].gameObject.activeSelf)
                        continue;

                    RectTransform rectTrans = localCards[i].GetComponent<RectTransform>();
                    if (multiSelectedIndices.Contains(i))
                    {
                        // 确保位置正确
                        rectTrans.anchoredPosition = new Vector2(
                            handCardPositions[i].x,
                            handCardPositions[i].y + m_CardHighlightAmount
                        );
                    }
                    else
                    {
                        // 确保位置正确
                        rectTrans.anchoredPosition = handCardPositions[i];
                    }
                }
            });

            // 播放动画序列
            sequence.Play();
        }

        // 重置拖动状态
        m_DragStartIndex = -1;
        m_IsDraggingCard = false;
        m_DragSelectedIndices.Clear();
    }

    public void HandleCardPointerExit(int cardIndex)
    {
        // 这个方法可以为空，滑动操作在Enter和Up事件中处理即可
    }

    #endregion

}
using System.Collections;
using NetMsg;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using static LocalOp_Bj;

/// <summary>
/// 十三水牌桥组件，管理头道、中道、尾道的卡牌排列和展示
/// </summary>
public class Pier_bj : MonoBehaviour
{
    #region 属性字段

    /// <summary>
    /// 道的索引 - 0:头道(3张) 1:中道(3张) 2:尾道(3张)
    /// </summary>
    public int pierIndex;

    /// <summary>
    /// 道中的卡牌引用数组
    /// </summary>
    public Card[] cards;

    /// <summary>
    /// 道中的卡牌数据数组
    /// </summary>
    public int[] cardDatas;

    /// <summary>
    /// 牌型显示对象
    /// </summary>
    public GameObject typeObj;

    /// <summary>
    /// 牌型图片
    /// </summary>
    public Image type;

    /// <summary>
    /// 清除按钮
    /// </summary>
    public Button cha;

    public LocalOp_Bj localOp;

    private Vector2[] cardPositions = new Vector2[3];

    #endregion

    #region 公共方法

    public void InitPierPanel(){
        // 初始化cardDatas数组，每道3张牌
        if (cardDatas == null || cardDatas.Length != 3)
        {
            cardDatas = new int[3];
            for (int i = 0; i < cardDatas.Length; i++)
            {
                cardDatas[i] = 0; // 0表示空位
            }
        }

        // 存储卡牌初始位置
        for (int i = 0; i < cards.Length; i++)
        {
            cardPositions[i] = cards[i].GetComponent<RectTransform>().anchoredPosition;
        }
    }

    //强制更新道牌数据
    public void ForceUpdatePierCards(int[] cardDatas){
        this.cardDatas = cardDatas;
        UpdateUI();
    }

    // 更新道牌UI
    public void UpdateUI(bool isShowType = true)
    {
        for (int i = 0; i < 3; i++)
        {
            int cardValue = cardDatas[i];
            if (cardValue != 0)
            {
                // GF.LogInfo($"UpdateUI: 道{pierIndex} 位置{i} 卡牌值{cardValue}");
                cards[i].gameObject.SetActive(true);
                cards[i].SetImgColor(Color.white);
                cards[i].Init(cardValue);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }

        // 检查是否需要显示牌型
        if (isShowType)
        {
            CheckAndShowCardType();
        }
    }


    /// <summary>
    /// 将卡牌添加到指定位置并更新数据
    /// </summary>
    /// <param name="index">位置索引</param>
    /// <param name="handCard">选中卡牌，为null时表示清除该位置</param>
    /// <param name="isValidate">是否验证牌型</param>
    public void SetCard(int index, Card handCard = null, bool isValidate = true)
    {
        if (index < 0 || index >= cards.Length)
        {
            Debug.LogError($"[Pier_bj] SetCard: 索引越界 {index}，有效范围: 0-{cards.Length - 1}");
            return;
        }

        if (index >= 0 && index < cards.Length)
        {
            // 如果传入null，清除该位置
            if (handCard == null)
            {
                cardDatas[index] = 0; // 0表示空位
                cards[index].gameObject.SetActive(false);
                return;
            }
            else
            {
                //保存卡牌数据
                cardDatas[index] = handCard.numD;
                cards[index].SetImgColor(Color.white);
                cards[index].Init(handCard.numD);

                // 获取卡牌当前位置和目标位置
                Vector3 startPos = handCard.transform.position;
                Vector3 targetPos = cards[index].transform.position;

                // 隐藏原手牌
                handCard.gameObject.SetActive(false);
                
                // 设置起始位置
                // GF.LogInfo($"SetCard: 道{pierIndex} 位置{index} 卡牌值{handCard.numD}");
                cards[index].gameObject.SetActive(true);
                cards[index].transform.position = startPos;
                
                // 创建动画序列
                Sequence sequence = DOTween.Sequence();
                sequence.SetTarget(gameObject);
                
                // 添加移动动画
                sequence.Append(cards[index].transform.DOMove(targetPos, 0.2f).SetEase(Ease.OutQuad));
                
                // 动画完成后的回调
                sequence.OnComplete(() => {
                    // 检查是否需要显示牌型
                    CheckAndShowCardType();
                    // 对道牌进行排序
                    // SortPierCards();
                    // 验证是否符合规则
                    if (localOp != null && IsComplete() && isValidate)
                    {
                        localOp.AutoSortPierCards();
                    }
                });
                
                // 播放动画序列
                sequence.Play();
            }
        }
    }

    /// <summary>
    /// 道牌移入
    /// </summary>
    /// <param name="index">位置索引</param>
    /// <param name="handCard">选中卡牌，为null时表示清除该位置</param>
    /// <param name="pierCardValue">道牌值</param>
    /// <param name="isValidate">是否验证牌型</param>
    public void SetCard_pier(int index, Card pierCard = null, int pierCardValue = 0, bool isValidate = true)
    {
        if (index < 0 || index >= cards.Length)
        {
            Debug.LogError($"[Pier_bj] SetCard: 索引越界 {index}，有效范围: 0-{cards.Length - 1}");
            return;
        }

        if (index >= 0 && index < cards.Length)
        {
            // 获取卡牌当前位置和目标位置
            Vector3 startPos = pierCard.transform.position;
            Vector3 targetPos = cards[index].transform.position;
            
            // 显示当前道的卡牌
            // GF.LogInfo($"SetCard_pier: 道{pierIndex} 位置{index} 卡牌值{pierCardValue}");
            cards[index].gameObject.SetActive(true);
            cards[index].SetImgColor(Color.white);
            cardDatas[index] = pierCardValue;
            cards[index].Init(pierCardValue);
            
            // 设置起始位置
            cards[index].transform.position = startPos;
            
            // 创建动画序列
            Sequence sequence = DOTween.Sequence();
            sequence.SetTarget(gameObject);
            
            // 添加移动动画
            sequence.Append(cards[index].transform.DOMove(targetPos, 0.1f).SetEase(Ease.OutQuad));
            
            // 动画完成后的回调
            sequence.OnComplete(() => {
                // 检查是否需要显示牌型
                CheckAndShowCardType();
                // 对道牌进行排序
                // SortPierCards();
                // 验证是否符合规则
                if (localOp != null && IsComplete() && isValidate)
                {
                    localOp.AutoSortPierCards();
                }
            });
            
            // 播放动画序列
            sequence.Play();
        }
    }

    /// <summary>
    /// 显示牌型（支持自定义动画）
    /// </summary>
    /// <param name="type">牌型枚举</param>
    /// <param name="animationType">动画类型：0-默认弹性动画，1-无动画</param>
    /// <param name="duration">动画持续时间</param>
    public void ShowCardType(ZjhCardType cardType, int animationType = 0, float duration = 0.5f)
    {
        typeObj.SetActive(true);

        // 加载对应牌型的图片
        string imagePath = GameConstants.GetCardTypeString_bj(cardType,1);
        GF.UI.LoadSprite(UtilityBuiltin.AssetsPath.GetSpritesPath(imagePath), (sprite) =>
        {
            if (type != null)
            {
                type.sprite = sprite;
                type.SetNativeSize();

                // 根据动画类型应用不同效果
                switch (animationType)
                {
                    case 0: // 默认弹性动画
                        ApplyTypeAnimation(duration, Ease.OutElastic);
                        break;
                    case 1: // 无动画
                        typeObj.transform.localScale = Vector3.one;
                        break;
                    default:
                        ApplyTypeAnimation(duration, Ease.OutElastic);
                        break;
                }

                // 播放牌型音效
                // string typeAudio = GameConstants.GetCardTypeString_bj(cardType);
                //Sound.PlayEffect($"BJ/{typeAudio}.mp3");
            }
        });
    }

    /// <summary>
    /// 获取已配牌数量
    /// </summary>
    /// <returns>已放置的卡牌数量</returns>
    public int GetCardCount()
    {
        int count = 0;
        if (cardDatas != null)
        {
            foreach (var cardData in cardDatas)
            {
                if (cardData != 0) count++; // 0表示空位
            }
        }
        return count;
    }

    /// <summary>
    /// 清空当前道的所有卡牌
    /// </summary>
    public void ResetPier()
    {
        // 保存当前道中有效的卡牌数据
        var validCards = new List<int>();
        for (int i = 0; i < cardDatas.Length; i++)
        {
            if (cardDatas[i] != 0) // 非空位
            {
                validCards.Add(cardDatas[i]);
            }
        }

        // 通知游戏管理器将这些牌还原到手牌
        if (validCards.Count > 0 && localOp != null)
        {
            // 将牌还原到手牌
            ReturnCardsToHand(validCards.ToArray());
        }
        ClearPier();
    }

    public void ClearPier()
    {
        DOTween.Kill(gameObject);
        // 隐藏道中所有卡牌显示
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].gameObject.SetActive(false);
            cards[i].SetImgColor(Color.white);
            cards[i].transform.GetComponent<RectTransform>().anchoredPosition = cardPositions[i];
        }

        // 清空卡牌数据
        for (int i = 0; i < cardDatas.Length; i++)
        {
            cardDatas[i] = 0;
        }

        // 隐藏牌型显示
        cha.gameObject.SetActive(false);
        typeObj.SetActive(false);
    }

    /// <summary>
    /// 将卡牌还原到手牌
    /// </summary>
    /// <param name="cardValues">要还原的卡牌值数组</param>
    public void ReturnCardsToHand(int[] cardValues)
    {
        if (localOp != null)
        {
            // 查找手牌中的空位
            for (int i = 0; i < cardValues.Length; i++)
            {
                int cardValue = cardValues[i];

                // 查找手牌数组中的第一个空位
                for (int j = 0; j < localOp.myCards.Length; j++)
                {
                    if (localOp.myCards[j] == 0)
                    {
                        // 将牌放回手牌
                        localOp.myCards[j] = cardValue;
                        break;
                    }
                }
            }
            localOp.UpdateHandCardsUI();
        }
    }

    /// <summary>
    /// 获取指定索引位置的坐标，无论该位置是否有卡牌
    /// </summary>
    /// <param name="cardIndex">卡牌在道中的索引</param>
    /// <returns>对应位置的世界坐标</returns>
    public Vector2 GetCardPosition(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= cards.Length)
        {
            Debug.LogError($"[Pier_bj] GetCardPosition: 索引越界 {cardIndex}，有效范围: 0-{cards.Length - 1}");
            return Vector2.zero;
        }
        if (cardIndex < 0 || cardIndex >= cardPositions.Length)
        {
            Debug.LogError($"[Pier_bj] GetCardPosition: 索引越界 {cardIndex}，有效范围: 0-{cardPositions.Length - 1}");
            return Vector2.zero;
        }
        return cardPositions[cardIndex];
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 卡牌点击事件处理
    /// </summary>
    private void OnCardClick(int cardIndex)
    {
        if (Util.IsClickLocked()) return;
        if (localOp.isAnimationPlaying) return;
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        // 检查卡牌位置是否有效
        if (cardIndex < 0 || cardIndex >= cards.Length)
        {
            return;
        }

        // 通知LocalOp_Bj处理点击事件
        if (localOp != null)
        {
            localOp.OnPierCardClick(pierIndex, cardIndex);
        }
    }

    /// <summary>
    /// 清除按钮点击事件处理
    /// </summary>
    private void OnClearButtonClick()
    {
        if (Util.IsClickLocked()) return;
        if (localOp.isAnimationPlaying) return;
       Sound.PlayEffect(AudioKeys.SOUND_BTN);
        ResetPier();
        if (localOp != null)
        {
            localOp.ClearSelection();
        }
    }

    /// <summary>
    /// 检查并显示牌型
    /// </summary>
    private void CheckAndShowCardType()
    {
        // 检查是否所有卡牌位置都已填满
        int cardCount = GetCardCount();
        cha.gameObject.SetActive(cardCount > 0);
        if (IsComplete())
        {
            // 获取牌型并显示
            ZjhCardType type = GameConstants.GetCardTypeByCards(cardDatas, localOp.currentGhostMode == GhostMode.RedBlack, out List<CardAvatar> cardsCache);
            ShowCardType(type);
        }
        else
        {
            HideCardType();
        }
    }

    // 检查是否所有卡牌位置都已填满
    public bool IsComplete(){
        // 检查该道是否已有3张牌
        bool isComplete = true;
        for (int i = 0; i < 3; i++)
        {
            if (cardDatas[i] == 0)
            {
                isComplete = false;
                break;
            }
        }
        return isComplete;
    }

    /// <summary>
    /// 计算当前道的牌型
    /// </summary>
    /// <returns>计算出的牌型</returns>
    private ZjhCardType CalculateCardType()
    {
        // 使用LocalOp_Bj脚本实例来计算牌型
        if (localOp != null)
        {
            return GameConstants.GetCardTypeByCards(cardDatas, localOp.currentGhostMode == GhostMode.RedBlack, out List<CardAvatar> cardsCache);
        }

        // 如果无法获取LocalOp_Bj实例，返回默认值
        return ZjhCardType.High;
    }

    /// <summary>
    /// 应用牌型显示动画
    /// </summary>
    /// <param name="duration">动画持续时间</param>
    /// <param name="ease">缓动类型</param>
    public void ApplyTypeAnimation(float duration = 0.5f, Ease ease = Ease.OutElastic)
    {
        if (typeObj != null)
        {
            typeObj.transform.localScale = Vector3.zero;
            typeObj.transform.DOScale(Vector3.one, duration).SetEase(ease);
        }
    }

    /// <summary>
    /// 隐藏牌型显示
    /// </summary>
    public void HideCardType()
    {
        if (typeObj != null)
        {
            typeObj.SetActive(false);
        }
    }

    /// <summary>
    /// 对道牌进行排序
    /// </summary>
    private void SortPierCards()
    {
        // 收集所有有效卡牌
        List<int> validCards = new List<int>();
        List<Card> validCardObjects = new List<Card>();
        for (int i = 0; i < cardDatas.Length; i++)
        {
            if (cardDatas[i] != 0)
            {
                validCards.Add(cardDatas[i]);
                validCardObjects.Add(cards[i]);
            }
        }

        // 将整数卡牌转换为CardAvatar对象以便获取点数和花色
        List<CardAvatar> cardAvatars = new List<CardAvatar>();
        foreach (int card in validCards)
        {
            cardAvatars.Add(new CardAvatar(card));
        }

        // 排序顺序：
        // 3. 按花色从大到小排序（黑桃>红桃>梅花>方块）
        var sortedCards = cardAvatars.OrderByDescending(c => c.Value == 15 ? 101 : (c.Value == 14 ? 100 : (c.Value == 1 ? 14 : c.Value)))
                                    .ThenByDescending(c => c.Suit)
                                    .ToList();

        // 创建动画序列
        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(gameObject);

        // 清空当前道的数据
        for (int i = 0; i < cardDatas.Length; i++)
        {
            cardDatas[i] = 0;
            cards[i].gameObject.SetActive(false);
        }

        // 更新卡牌位置、数据和显示，自动填补空位
        for (int i = 0; i < cardDatas.Length; i++)
        {
            if (i < sortedCards.Count)
            {
                int cardValue = GameUtil.GetInstance().CombineColorAndValue(sortedCards[i].Suit, sortedCards[i].Value);
                cardDatas[i] = cardValue;
                cards[i].gameObject.SetActive(true);
                cards[i].Init(cardValue);
                cards[i].transform.GetComponent<RectTransform>().anchoredPosition = cardPositions[i];
            }
            else
            {
                cardDatas[i] = 0;
                cards[i].gameObject.SetActive(false);
            }
        }

        // 动画完成后的回调
        sequence.OnComplete(() => {
            // 重新检查牌型
            CheckAndShowCardType();
        });

        // 播放动画序列
        sequence.Play();
    }

    #endregion
}

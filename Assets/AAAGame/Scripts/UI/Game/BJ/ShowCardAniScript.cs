﻿using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;
using NetMsg;
using UGUI.Extend;

public class ShowCardAniScript : MonoBehaviour
{
    public Card[] showCards;
    public Image type;
    public Text score;
    
    public void Hide()
    {
        DOTween.Kill(gameObject);
        // type.gameObject.SetActive(false);
        score.gameObject.SetActive(false);
        foreach (var card in showCards)
        {
            card.GetComponent<Card>().Init(-1);
            card.transform.localScale = Vector3.one;
            card.transform.localRotation = Quaternion.identity;
        }
    }

    private bool isRight;

    /// <summary>
    /// 根据左右位置 显示不同的效果
    /// </summary>
    /// <param name="isRight"></param>
    public void InitPanel(bool isRight)
    {
        this.isRight = isRight;
        if (isRight)
        {
            score.transform.localPosition = new Vector2(200, 20);
            score.alignment = TextAnchor.MiddleLeft;
        }else{
            score.transform.localPosition = new Vector2(-200, 20);
            score.alignment = TextAnchor.MiddleRight;
        }
    }

    public void PlayAni(ChickenCards chickenCards, int SettleAnimation)
    {
        //0.6s
        PlayCardAnimationWithSequence(chickenCards, SettleAnimation);
    }

    private void PlayCardAnimationWithSequence(ChickenCards chickenCards, int SettleAnimation)
    {
        // 卡牌同时翻开动画，但有延迟差异
        float cardDelay = 0.1f;
        
        // 为每张卡牌创建单独的序列
        for (int i = 0; i < showCards.Length; i++)
        {
            int index = i;
            Sequence cardSequence = DOTween.Sequence();
            cardSequence.SetTarget(gameObject);
            
            // 设置每张卡牌的延迟时间
            cardSequence.SetDelay(cardDelay * i);

            // 卡牌先做缩放，模仿真实翻牌的深度感
            cardSequence.Append(showCards[index].transform.
            DOScale(new Vector3(1.1f, 1.1f, 1), 0.1f).SetEase(Ease.OutQuad));

            // 卡牌旋转：分两阶段，首先旋转到背面
            cardSequence.Append(showCards[index].transform.
            DORotate(new Vector3(0, 90, 0), 0.2f, RotateMode.FastBeyond360)
                .SetEase(Ease.InOutCubic));

            // 卡牌展示正面
            cardSequence.AppendCallback(() => { 
                showCards[index].Init(chickenCards.Cards[index]); });

            // 旋转回正面
            cardSequence.Append(showCards[index].transform.
            DORotate(new Vector3(0, 0, 0f), 0.2f, RotateMode.FastBeyond360)
                .SetEase(Ease.InOutCubic));

            // 卡牌缩放回正常大小
            cardSequence.Append(showCards[index].transform.
            DOScale(Vector3.one, 0.1f).SetEase(Ease.OutQuad));

            //显示分数动画
            if (index == 0)
            {
                cardSequence.JoinCallback(() => {
                    float.TryParse(chickenCards.ChangeCoin, out float changeCoin);
                    ColorFader colorFader = score.GetComponent<ColorFader>();
                    Color32[] colors = GameConstants.GetColorsByResult(changeCoin > 0);
                    colorFader.color1 = colors[0];
                    colorFader.color2 = colors[1];
                    score.gameObject.SetActive(true);
                    string typeText = GameConstants.GetCardTypeString_bj(chickenCards.Type);
                    string paddedTypeText = typeText.PadRight(3, ' '); // 使用空格填充至3个字符宽度
                    string sign = changeCoin >= 0 ? "+" : "-";
                    score.text = paddedTypeText + sign + System.Math.Abs(changeCoin).ToString();
                    
                    // if (isRight)
                    // {
                        score.rectTransform.localScale = Vector3.zero;
                        score.rectTransform.DOScale(1, 0.2f).SetEase(Ease.OutBack);
                    // }else{
                        // score.rectTransform.localScale = Vector3.zero;
                        // score.rectTransform.DOScale(1, 0.2f).SetEase(Ease.OutBack);
                    // }
                    // 播放牌型音效完整比牌播放
                    if(SettleAnimation == 1){
                        string typeAudio = GameConstants.GetCardTypeString_bj(chickenCards.Type);
                       Sound.PlayEffect($"BJ/{typeAudio}.mp3");
                    }
                    else if(SettleAnimation == 2){
                       Sound.PlayEffect("LiangPai.mp3");
                    }
                });
            }
            
            // 播放当前卡牌的序列动画
            cardSequence.Play();
        }
    }
}


using System.Collections.Generic;
using NetMsg;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class Blessing : UIFormBase
{
    public GameObject panel;
    public Text caiNumText;
    public Image carProp;
    public Text yunNumText;
    public Image yunProp;
    public Image ylqImg;
    public Button giftBtn;
    public GameObject tips;
    public List<GameObject> ItemObjs;


    //特效表现
    public GameObject effPanel;
    public GameObject effectParent;
    public List<GameObject> effectObjs;
    public GameObject pos;
    public GameObject fly;
    public GameObject zhuanyunzhong;
    public GameObject up;
    public GameObject caiYunUpAni;

    /// <summary>
    /// 祈福通知数据
    /// </summary>
    private SynPrayRs synPrayRs = null;

    private string[] prayEffectNames = new string[]
    {
        AudioKeys.BLESSING_INGOTSRAIN,
        AudioKeys.BLESSING_WASHINGHANDS,
        AudioKeys.BLESSING_LUOPAN,
        AudioKeys.BLESSING_BURNINGINCENSE,
        AudioKeys.BLESSING_POFUCHENZHOU,
        AudioKeys.BLESSING_FENGHUANG,
        AudioKeys.BLESSING_FLYINGDRAGON,
        AudioKeys.BLESSING_RUHUASHIJING,
        AudioKeys.BLESSING_LUCKYTREE,
    };

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        HotfixNetworkComponent.AddListener(MessageID.Msg_PrayRs, FuncMsgPrayRsRs);
        HotfixNetworkComponent.AddListener(MessageID.Msg_PrayRewardRs, OnPrayRewardRs);

        //传入一个坐标 需要将pos的位置移动到坐标区域
        if (Params.TryGet<VarVector3>("targetPosition", out var targetPos))
        {
            pos.transform.position = targetPos;
        }
        if (Params.TryGet<VarBoolean>("isLeft", out var isLeft))
        {
            caiYunUpAni = isLeft ? pos.transform.Find("CaiYunUpAniLeft").gameObject
                                      : pos.transform.Find("CaiYunUpAniRight").gameObject;
        }
        Params.TryGet("synPrayRs", out VarByteArray byteArray);
        if (byteArray != null)
        {
            synPrayRs = SynPrayRs.Parser.ParseFrom(byteArray);
        }

        var globalMgr = GlobalManager.GetInstance();
        caiNumText.text = $"{globalMgr.PrayWealth}/{globalMgr.MaxWealth}";
        carProp.fillAmount = (float)globalMgr.PrayWealth / globalMgr.MaxWealth;
        yunNumText.text = $"{globalMgr.PrayLuck}/{globalMgr.MaxLuck}";
        yunProp.fillAmount = (float)globalMgr.PrayLuck / globalMgr.MaxLuck;

        // 礼包按钮：满足财运和幸运上限 且 今天未领取过
        bool canClaimGift = globalMgr.PrayWealth >= globalMgr.MaxWealth && 
                            globalMgr.PrayLuck >= globalMgr.MaxLuck
                            && globalMgr.RewardState == 0;
        if(canClaimGift)
        {
            giftBtn.image.ClearGray();
            giftBtn.transform.Find("Eff").gameObject.SetActive(true);
        }
        else
        {
            giftBtn.image.SetGray();
            giftBtn.transform.Find("Eff").gameObject.SetActive(false);
        }
        ylqImg.gameObject.SetActive(globalMgr.RewardState == 1);
        tips.gameObject.SetActive(false);

        foreach (var item in ItemObjs)
        {
            long prayId = long.Parse(item.name);
            var prayConfig = globalMgr.GetPrayConfig(prayId);
            var price = item.transform.Find("BuyBtn/Price").gameObject;
            var priceText = item.transform.Find("BuyBtn/Price/PriceText").GetComponent<Text>();
            var free = item.transform.Find("BuyBtn/Free").gameObject;
            if (prayConfig.ReadyPray < prayConfig.FreeTime)
            {
                free.SetActive(true);
                price.SetActive(false);
            }
            else
            {
                free.SetActive(false);
                price.SetActive(true);
            }
            priceText.text = prayConfig.CostDiamond.ToString();
            var bt = item.transform.Find("BuyBtn").GetComponent<Button>();
            bt.onClick.RemoveAllListeners();
            bt.onClick.AddListener(() => BuyBlessing(prayId));
        }

        if(synPrayRs == null)
        {
            panel.SetActive(true);
            effPanel.SetActive(false);
        }
        else
        {
            panel.SetActive(false);
            //播放特效流程
            PlayPrayEffect(synPrayRs);
        }
    }

    public void BuyBlessing(long prayId)
    {
        var prayConfig = GlobalManager.GetInstance().GetPrayConfig(prayId);

        bool canPray = false;
        if (prayConfig.ReadyPray < prayConfig.FreeTime)
        {
            canPray = true;
        }
        else
        {
            long playerDiamond = Util.GetMyselfInfo().Diamonds;
            if (playerDiamond < prayConfig.CostDiamond)
            {
                GF.UI.ShowToast("钻石不足，无法祈福！");
                return;
            }
            canPray = true;
        }

        if (!canPray)
        {
            return;
        }

        Msg_PrayRq req = MessagePool.Instance.Fetch<Msg_PrayRq>();
        req.PrayId = prayId;
        HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
            (HotfixNetworkComponent.AccountClientName, MessageID.Msg_PrayRq, req);
    }

    private void FuncMsgPrayRsRs(MessageRecvData data)
    {
        Msg_PrayRs ack = Msg_PrayRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("[祈福] 祈福返回", ack.ToString());

        // 播放祈福特效动画序列
        PlayPrayEffect(ack);
    }

    /// <summary>
    /// 祈福奖励返回处理
    /// </summary>
    private void OnPrayRewardRs(MessageRecvData data)
    {
        Msg_PrayRewardRs ack = Msg_PrayRewardRs.Parser.ParseFrom(data.Data);
        GF.LogInfo("[祈福] 领取奖励返回", ack.ToString());
        
        var globalMgr = GlobalManager.GetInstance();
        // 更新礼包领取状态为已领取
        globalMgr.RewardState = 1;
        
        // 刷新界面：显示已领取图标，禁用按钮
        ylqImg.gameObject.SetActive(true);
        giftBtn.image.ClearGray();
        giftBtn.transform.Find("Eff").gameObject.SetActive(false);

        // 准备奖励数据 (钻石固定ItemID为2,根据实际配置表调整)
        var rewards = new List<RewardItemData>
        {
            new RewardItemData 
            { 
                Id = 2,                     // 钻石的ItemID
                Count = ack.RewardDiamond   // 奖励数量
            }
        };

        // 显示奖励面板 - 5秒后自动关闭
        Util.GetInstance().OpenRewardPanel(rewards, autoCloseDelay: 5f);
        
        GF.LogInfo($"[祈福] 礼包领取成功，状态更新为: {globalMgr.RewardState}");
    }

    /// <summary>
    /// 播放祈福特效完整流程（自己祈福）
    /// </summary>
    private void PlayPrayEffect(Msg_PrayRs ack)
    {
        PlayPrayEffect(ack.PrayId, ack.AddWealth, ack.AddLuck, true);
    }

    /// <summary>
    /// 播放祈福特效完整流程（他人祈福通知）
    /// </summary>
    private void PlayPrayEffect(SynPrayRs synPrayRs)
    {
        PlayPrayEffect(synPrayRs.PrayId, synPrayRs.AddWealth, synPrayRs.AddLuck, false);
    }

    /// <summary>
    /// 播放祈福特效完整流程（统一处理）
    /// </summary>
    private void PlayPrayEffect(long prayId, int addWealth, int addLuck, bool isMyself)
    {
        // 准备数值显示
        bool hasLuck = addLuck > 0;
        bool hasWealth = addWealth > 0;

        var yunUpObj = caiYunUpAni.transform.Find("YunUpObj")?.gameObject;
        var caiUpObj = caiYunUpAni.transform.Find("CaiUpObj")?.gameObject;

        if (yunUpObj != null)
        {
            yunUpObj.SetActive(false);
            if (hasLuck)
            {
                var yunText = yunUpObj.transform.Find("YunUp")?.GetComponent<Text>();
                if (yunText != null) yunText.text = $"+{addLuck}";
            }
        }
        if (caiUpObj != null)
        {
            caiUpObj.SetActive(false);
            if (hasWealth)
            {
                var caiText = caiUpObj.transform.Find("CaiUp")?.GetComponent<Text>();
                if (caiText != null) caiText.text = $"+{addWealth}";
            }
        }

        // 初始隐藏所有特效
        fly.SetActive(false);
        zhuanyunzhong.SetActive(false);
        up.SetActive(false);
        caiYunUpAni.SetActive(false);
        panel.SetActive(false);
        effectParent.SetActive(false);
        effPanel.SetActive(true);

        // 获取特效索引和动画
        int effIndex = (int)prayId - 1;
        if (effIndex < 0 || effIndex >= effectObjs.Count)
        {
            GF.LogWarning($"[祈福] 无效的祈福ID: {prayId}");
            return;
        }

        string audioStr = prayEffectNames[effIndex];
        string aniName = effectObjs[effIndex].name;
        SkeletonAnimation ani = effectObjs[effIndex].GetComponent<SkeletonAnimation>();

        if (ani == null)
        {
            GF.LogError($"[祈福] effectObjs[{effIndex}] 没有 SkeletonAnimation 组件");
            return;
        }

        // 创建动画序列
        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(gameObject);

        if (isMyself)
        {
            // 步骤1: 自己祈福 - 播放祈福Spine动画 + 转运中
            sequence.AppendCallback(() =>
            {
                effectParent.SetActive(true);
                ani.gameObject.SetActive(true);
                Sound.PlayEffect(audioStr);

                GF.LogInfo($"[祈福] 开始播放Spine动画: {aniName}");
                
                var trackEntry = ani.AnimationState.SetAnimation(0, aniName, false);
                zhuanyunzhong.SetActive(true);

                // 获取动画实际时长并动态添加延迟
                float animationDuration = 2f;
                if (trackEntry != null && trackEntry.Animation != null)
                {
                    animationDuration = trackEntry.Animation.Duration;
                }

                // 在动画播放完成后继续后续步骤
                DOVirtual.DelayedCall(animationDuration, () =>
                {
                    ContinuePrayEffect(sequence, yunUpObj, caiUpObj, hasLuck, hasWealth, ani, isMyself);
                });
            });
        }
        else
        {
            // 他人祈福 - 跳过全屏特效，显示转运中2秒后播放fly飞行
            sequence.AppendCallback(() =>
            {
                zhuanyunzhong.SetActive(true);
            });
            sequence.AppendInterval(2f);
            sequence.AppendCallback(() =>
            {
                ContinuePrayEffect(sequence, yunUpObj, caiUpObj, hasLuck, hasWealth, ani, isMyself);
            });
        }
    }

    /// <summary>
    /// 祈福特效的后续步骤（从fly飞行开始）
    /// </summary>
    private void ContinuePrayEffect(Sequence mainSequence, GameObject yunUpObj, GameObject caiUpObj, bool hasLuck, bool hasWealth, SkeletonAnimation previousAni, bool isMyself)
    {
        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(gameObject);

        if (isMyself)
        {
            // 自己祈福: fly从屏幕中心飞向pos位置
            sequence.AppendCallback(() =>
            {
                previousAni.gameObject.SetActive(false);
                effectParent.SetActive(false);

                // fly初始化到屏幕中心
                fly.SetActive(true);
                fly.transform.localPosition = Vector3.zero;

                // 计算fly到pos的方向并调整朝向
                Vector3 targetPos = pos.transform.position;
                Vector3 direction = targetPos - fly.transform.position;
                if (direction != Vector3.zero)
                {
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f; // -90因为默认0度朝上
                    fly.transform.rotation = Quaternion.Euler(0, 0, angle);
                }
            });

            // fly移动到pos位置（0.3秒）
            sequence.Append(fly.transform.DOMove(pos.transform.position, 0.3f).SetEase(Ease.InOutQuad));
        }

        // 步骤3: 播放Up特效 + 数值上升动画
        sequence.AppendCallback(() =>
        {
            fly.SetActive(false);
            zhuanyunzhong.SetActive(false);
            Sound.PlayEffect(AudioKeys.BLESSING_BLESSING_UP);

            // 播放Up特效
            up.SetActive(true);
            up.transform.position = pos.transform.position;
            var upAni = up.GetComponent<SkeletonAnimation>();
            if (upAni != null)
            {
                upAni.AnimationState.SetAnimation(0, "Up", false);
            }

            // 播放数值上升动画
            caiYunUpAni.SetActive(true);
            RectTransform caiYunRect = caiYunUpAni.GetComponent<RectTransform>();
            if (caiYunRect != null)
            {
                // 初始位置
                caiYunRect.anchoredPosition = new Vector2(caiYunRect.anchoredPosition.x, -66f);
                // 显示对应的数值
                if (yunUpObj != null) yunUpObj.SetActive(hasLuck);
                if (caiUpObj != null) caiUpObj.SetActive(hasWealth);
            }
        });

        // 数值文字上升动画（y轴 -100 到 66，0.6秒）
        RectTransform caiYunRectTween = caiYunUpAni.GetComponent<RectTransform>();
        sequence.Append(caiYunRectTween.DOAnchorPosY(66f, 0.6f).SetEase(Ease.OutQuad));

        // 等待Up特效播放完成
        sequence.AppendInterval(0.7f);

        // 步骤4: 隐藏所有特效
        sequence.AppendCallback(() =>
        {
            up.SetActive(false);
            caiYunUpAni.SetActive(false);
        });

        // 步骤5: 短暂延迟后，根据是否是自己祈福决定操作
        sequence.AppendInterval(0.3f);
        sequence.AppendCallback(() =>
        {
                GF.UI.Close(this.UIForm);
        });
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_PrayRs, FuncMsgPrayRsRs);
        HotfixNetworkComponent.RemoveListener(MessageID.Msg_PrayRewardRs, OnPrayRewardRs);
        base.OnClose(isShutdown, userData);
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "CloseBtn":
                GF.UI.Close(this.UIForm);
                break;
            case "GiftBtn":
                var globalMgr = GlobalManager.GetInstance();
                if (globalMgr.RewardState == 1)
                {
                    GF.UI.ShowToast("今天已经领取过神秘礼包了！");
                    return;
                }
                if (globalMgr.PrayWealth < globalMgr.MaxWealth ||
                    globalMgr.PrayLuck < globalMgr.MaxLuck)
                {
                    tips.gameObject.SetActive(!tips.gameObject.activeSelf);
                    return;
                }
                Msg_PrayRewardRq req = MessagePool.Instance.Fetch<Msg_PrayRewardRq>();
                HotfixNetworkManager.Ins.hotfixNetworkComponent.SendPB3
                    (HotfixNetworkComponent.AccountClientName, MessageID.Msg_PrayRewardRq, req);
                break;
        }
    }
}
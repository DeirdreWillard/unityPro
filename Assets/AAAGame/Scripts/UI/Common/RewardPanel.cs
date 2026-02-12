
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using static UtilityBuiltin;

/// <summary>
/// 奖励显示数据
/// </summary>
public class RewardItemData
{
    public int Id;              // 物品ID
    public long Count;          // 数量
    public long AddCount;       // 额外增加数量(用于显示+号增量)
    public string ExName;       // 自定义名称
    public int Turn2Id;         // 转换为的物品ID(用于动画切换显示)
    public long Turn2Count;     // 转换后的数量
}

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class RewardPanel : UIFormBase
{
    private List<RewardItemData> rewardData = new List<RewardItemData>();
    private Action onCloseCallback;
    private Vector3? endPoint;
    private float autoCloseDelay = 0f;
    private bool clickEnabled = false;
    private Sequence delayShowSequence;
    private List<Sequence> itemSequences = new List<Sequence>();

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        rewardData.Clear();
        onCloseCallback = null;
        endPoint = null;
        clickEnabled = false;
        autoCloseDelay = 0f;

        // 从参数获取奖励数据
        var data = Params.Get<VarObject>("RewardData")?.Value as List<RewardItemData>;
        if (data != null)
        {
            SetRewardData(data);
        }

        // 获取可选参数
        var callbackVar = Params.Get<VarObject>("Callback");
        if (callbackVar != null)
        {
            onCloseCallback = callbackVar.Value as Action;
        }

        var endPointVar = Params.Get<VarVector3>("EndPoint");
        if (endPointVar != null)
        {
            endPoint = endPointVar.Value;
        }

        var delayVar = Params.Get<VarSingle>("AutoCloseDelay");
        if (delayVar != null && delayVar.Value > 0)
        {
            autoCloseDelay = delayVar.Value;
        }
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        // 清理所有动画
        delayShowSequence?.Kill();
        foreach (var seq in itemSequences)
        {
            seq?.Kill();
        }
        itemSequences.Clear();

        transform.DOKill();

        onCloseCallback = null;
        rewardData.Clear();

        base.OnClose(isShutdown, userData);
    }

    /// <summary>
    /// 设置奖励数据并刷新界面
    /// </summary>
    public void SetRewardData(List<RewardItemData> data)
    {
        delayShowSequence?.Kill();

        rewardData = data;
        int count = data.Count;

        // 将奖励分成两行显示
        List<List<RewardItemData>> rows = new List<List<RewardItemData>>();
        if (count < 8)
        {
            rows.Add(new List<RewardItemData>(data));
        }
        else if (count == 8)
        {
            rows.Add(new List<RewardItemData>(data.GetRange(0, 3)));
            rows.Add(new List<RewardItemData>(data.GetRange(3, 5)));
        }
        else
        {
            int halfCount = Mathf.FloorToInt(count / 2f);
            rows.Add(new List<RewardItemData>(data.GetRange(0, halfCount)));
            rows.Add(new List<RewardItemData>(data.GetRange(halfCount, count - halfCount)));
        }

        RefreshPanel(rows).Forget();
    }

    /// <summary>
    /// 刷新奖励面板显示
    /// </summary>
    private async UniTaskVoid RefreshPanel(List<List<RewardItemData>> rows)
    {
        Sound.PlayEffect(AudioKeys.OPENREWARD);

        Transform aniTrans = transform.Find("ani");
        if (aniTrans == null) return;

        // 播放动画
        Animator animator = aniTrans.GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play("ycty_gongxihd_ani", -1, 0);
            animator.Update(0);
        }

        Transform content = aniTrans.Find("RewardsContent");
        if (content == null) return;

        // 初始隐藏内容
        content.gameObject.SetActive(false);

        // 清理并创建奖励行
        Transform rewards1 = content.Find("Rewards1");
        Transform rewards2 = content.Find("Rewards2");

        if (rewards1) rewards1.gameObject.SetActive(false);
        if (rewards2) rewards2.gameObject.SetActive(false);

        Transform rewardItemTemplate = content.Find("RewardItem");
        rewardItemTemplate.gameObject.SetActive(false);

        for (int rowIdx = 0; rowIdx < rows.Count; rowIdx++)
        {
            Transform rewardsRow = content.Find($"Rewards{rowIdx + 1}");
            if (rewardsRow == null) continue;

            rewardsRow.gameObject.SetActive(true);

            // 隐藏所有子物品
            for (int i = 0; i < rewardsRow.childCount; i++)
            {
                rewardsRow.GetChild(i).gameObject.SetActive(false);
            }

            // 创建奖励物品
            List<RewardItemData> rowData = rows[rowIdx];
            for (int i = 0; i < rowData.Count; i++)
            {
                Transform item = null;
                if (i < rewardsRow.childCount)
                {
                    item = rewardsRow.GetChild(i);
                }
                else if (rewardItemTemplate != null)
                {
                    GameObject newItem = GameObject.Instantiate(rewardItemTemplate.gameObject);
                    newItem.transform.SetParent(rewardsRow, false);
                    item = newItem.transform;
                }

                if (item == null) continue;

                item.gameObject.SetActive(true);
                item.localScale = Vector3.one;

                await SetupRewardItem(item, rowData[i]);
            }

            await UniTask.Yield();
        }

        // 延迟显示内容
        delayShowSequence = DOTween.Sequence();
        delayShowSequence.AppendInterval(0.6f);
        delayShowSequence.AppendCallback(() =>
        {
            if (content != null)
            {
                content.gameObject.SetActive(true);
            }
        });
        delayShowSequence.SetTarget(transform);

        // 延迟1秒后允许点击关闭
        await UniTask.Delay(TimeSpan.FromSeconds(1));
        clickEnabled = true;

        // 如果设置了自动关闭延迟
        if (autoCloseDelay > 0)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(autoCloseDelay));
            OnClickClose();
        }
    }

    /// <summary>
    /// 设置单个奖励物品显示
    /// </summary>
    private async UniTask SetupRewardItem(Transform item, RewardItemData data)
    {
        // 从ItemTable配置表读取物品信息
        var itemTable = GF.DataTable.GetDataTable<ItemTable>();
        ItemTable itemConfig = itemTable?.GetDataRow(data.Id);

        if (itemConfig == null)
        {
            GF.LogWarning($"[RewardPanel] 找不到ItemID={data.Id}的物品配置");
            return;
        }

        string itemName = itemConfig.Name;  // 从配置表获取物品名称

        // 设置数量文本
        Transform countText = item.Find("CountText");
        if (countText != null)
        {
            Text text = countText.GetComponent<Text>();
            if (text != null)
            {
                string countStr = FormatNumber(data.Count);
                if (data.AddCount > 0)
                {
                    string baseCount = FormatNumber(data.Count - data.AddCount);
                    string addCount = FormatNumber(data.AddCount);
                    countStr = $"{baseCount}<color=#48ff00>+{addCount}</color>";
                }
                text.text = countStr;
            }
        }

        // 设置图标 (异步加载)
        Transform iconTrans = item.Find("Icon");
        if (iconTrans != null && !string.IsNullOrEmpty(itemConfig.Path))
        {
            Image iconImage = iconTrans.GetComponent<Image>();
            if (iconImage != null)
            {
                GF.UI.LoadSprite(AssetsPath.GetSpritesPath(itemConfig.Path + ".png"), (sprite) =>
                {
                    if (iconImage == null) return;
                    iconImage.sprite = sprite;
                });
            }
        }

        // 设置名称
        Transform nameTrans = item.Find("Name");
        if (nameTrans != null)
        {
            // 优先使用自定义名称，否则使用配置表名称
            string displayName = !string.IsNullOrEmpty(data.ExName) ? data.ExName : itemName;

            if (!string.IsNullOrEmpty(displayName))
            {
                nameTrans.gameObject.SetActive(true);
                Text nameText = nameTrans.GetComponent<Text>();
                if (nameText != null)
                {
                    nameText.text = displayName;
                }
            }
            else
            {
                nameTrans.gameObject.SetActive(false);
            }
        }

        // 处理转换动画
        if (data.Turn2Id > 0 && data.Turn2Count > 0)
        {
            // 预加载转换后的物品配置
            ItemTable turn2ItemConfig = itemTable?.GetDataRow(data.Turn2Id);

            Sequence seq = DOTween.Sequence();
            seq.AppendInterval(1f);
            seq.Append(item.DOScale(Vector3.one * 0.1f, 0.3f));
            seq.AppendCallback(() =>
            {
                // 切换到新物品显示
                if (countText != null)
                {
                    Text text = countText.GetComponent<Text>();
                    if (text != null)
                    {
                        string newCount = FormatNumber(data.Turn2Count);
                        text.text = $"<color=#48ff00>{newCount}</color>";
                    }
                }

                // 更新图标 (使用转换后物品的图标)
                if (iconTrans != null && turn2ItemConfig != null && !string.IsNullOrEmpty(turn2ItemConfig.Path))
                {
                    Image iconImage = iconTrans.GetComponent<Image>();
                    if (iconImage != null)
                    {
                        GF.UI.LoadSprite(AssetsPath.GetSpritesPath(turn2ItemConfig.Path + ".png"), (sprite) =>
                        {
                            if (iconImage == null) return;
                            iconImage.sprite = sprite;
                        });
                    }
                }

                // 更新名称
                if (nameTrans != null && turn2ItemConfig != null)
                {
                    Text nameText = nameTrans.GetComponent<Text>();
                    if (nameText != null && !string.IsNullOrEmpty(turn2ItemConfig.Name))
                    {
                        nameTrans.gameObject.SetActive(true);
                        nameText.text = turn2ItemConfig.Name;
                    }
                }
            });
            seq.Append(item.DOScale(Vector3.one * 1.5f, 0.15f));
            seq.Append(item.DOScale(Vector3.one, 0.1f));
            seq.SetTarget(transform);

            itemSequences.Add(seq);
        }

        await UniTask.Yield();
    }

    /// <summary>
    /// 格式化数字显示(超过10000显示为万)
    /// </summary>
    private string FormatNumber(long number)
    {
        if (number >= 10000)
        {
            float value = number / 10000f;
            return $"{value:F2}万";
        }
        return number.ToString();
    }

    /// <summary>
    /// 点击关闭(也可以通过点击空白区域触发)
    /// </summary>
    public override void OnClickClose()
    {
        if (!clickEnabled) return;

        // 如果设置了终点位置，播放掉落动画
        if (endPoint.HasValue)
        {
            PlayDropAnimation();
        }

        // 回调
        onCloseCallback?.Invoke();

        GF.UI.Close(this.UIForm);
    }

    /// <summary>
    /// 播放掉落动画
    /// </summary>
    private void PlayDropAnimation()
    {
        Transform aniTrans = transform.Find("ani");
        if (aniTrans == null) return;

        Transform content = aniTrans.Find("RewardsContent");
        if (content == null) return;

        // 遍历所有奖励物品，播放飞向目标点的动画
        for (int rowIdx = 1; rowIdx <= 2; rowIdx++)
        {
            Transform row = content.Find($"Rewards{rowIdx}");
            if (row == null || !row.gameObject.activeSelf) continue;

            for (int i = 0; i < row.childCount; i++)
            {
                Transform item = row.GetChild(i);
                if (!item.gameObject.activeSelf) continue;

                Transform icon = item.Find("Icon");
                if (icon == null) continue;

                // 创建飞行动画
                Vector3 startPos = icon.position;
                Sequence flySeq = DOTween.Sequence();
                flySeq.Append(icon.DOMove(endPoint.Value, 0.5f).SetEase(Ease.InQuad));
                flySeq.Join(icon.DOScale(Vector3.zero, 0.5f));
                flySeq.OnComplete(() =>
                {
                    if (icon != null)
                    {
                        GameObject.Destroy(icon.gameObject);
                    }
                });
            }
        }
    }

}
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class ChipThrower : MonoBehaviour
{
    public GameObject chipPrefab; // 筹码预制体
    public int poolSize = 10; // 对象池大小

    private List<GameObject> chipPool; // 对象池列表
    private int curIndex = 0; // 当前使用的对象池索引

    private void Start()
    {
        InitPool();
    }

    public void InitPool()
    {
        if (chipPool != null && chipPool.Count > 0) return;
        if (chipPrefab == null)
        {
            chipPrefab = transform.GetChild(0).gameObject;
        }
        chipPool = new List<GameObject>();
        for (int i = 0; i < poolSize; i++)
        {
            GameObject chip = Instantiate(chipPrefab, transform);
            chip.SetActive(false);
            chipPool.Add(chip);
        }
    }

    public void PlayChips(Vector3 startPos, Vector3 endPos)
    {
        InitPool();
        // 获取当前可用的筹码对象
        GameObject chip = GetNextChip();

        // 激活筹码并重置位置
        chip.SetActive(true);

        // 播放每个子对象的动画
        for (int i = 1; i <= 6; i++)
        {
            int index = i;
            Transform imageTransform = chip.transform.Find("image" + i);
            if (imageTransform != null)
            {
                // 重置初始位置
                imageTransform.localPosition = startPos;

                // 播放动画
                float duration = 0.15f + 0.05f * (index - 1);
                imageTransform.DOLocalMove(endPos, duration)
                    .SetEase(Ease.OutCubic);
            }
        }
        //固定时间隐藏
        DOVirtual.DelayedCall(0.45f, () =>
        {
            chip.SetActive(false);
        });
    }

    private GameObject GetNextChip()
    {
        // 获取当前索引的对象
        GameObject chip = chipPool[curIndex];
        curIndex = (curIndex + 1) % poolSize; // 更新索引，循环使用
        return chip;
    }

}

using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ChipManager : MonoBehaviour
{
    [SerializeField] private int poolSize = 10;          // 对象池的大小
    [SerializeField] private float baseDuration = 0.15f; // 基础动画时间
    [SerializeField] private float incrementDuration = 0.05f; // 每个图像增加的时间
    [SerializeField] private float hideDelay = 0.45f;    // 延迟隐藏的时间

    private Queue<GameObject> poolQueue = new Queue<GameObject>();

    // 初始化池，传入父对象
    public void InitPoolChips(GameObject go = null)
    {
        poolQueue.Clear();
        if (go == null)
            go = gameObject;

        for (int i = 1; i <= poolSize; i++)
        {
            Transform item = go.transform.Find("Item" + i);
            if (item != null)
            {
                poolQueue.Enqueue(item.gameObject);
                item.gameObject.SetActive(false); // 初始化时将对象设为隐藏
            }
        }
    }

    // 播放芯片动画
    public void PlayChips(Vector3 startPos, Vector3 endPos)
    {
        if (poolQueue.Count == 0) return;

        GameObject obj = poolQueue.Dequeue();
        obj.SetActive(true);

        for (int i = 1; i <= 6; i++)
        {
            Transform imageTransform = obj.transform.Find("image" + i);
            if (imageTransform != null)
            {
                imageTransform.localPosition = startPos;
                imageTransform.DOLocalMove(endPos, baseDuration + incrementDuration * i).SetEase(Ease.Linear);
            }
        }

        // 延迟隐藏对象
        obj.transform.DOLocalMoveZ(0, hideDelay).OnComplete(() =>
        {
            obj.SetActive(false);
            poolQueue.Enqueue(obj);  // 将对象放回队列末尾
        });
    }
}

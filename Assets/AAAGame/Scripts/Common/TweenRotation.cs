using UnityEngine;

[AddComponentMenu("UGUI/Tween/Tween Rotation")]
public class TweenRotationUGUI : MonoBehaviour
{
    public Vector3 from; // 起始旋转角度
    public Vector3 to; // 结束旋转角度
    public bool quaternionLerp = false; // 是否使用四元数插值
    public float duration = 1.0f; // 动画持续时间
    public bool playOnAwake = true; // 是否在 Awake 时自动播放
    public bool loop = false; // 是否循环播放

    private RectTransform rectTransform; // 用于引用 RectTransform
    private float elapsedTime = 0f; // 记录已过去的时间
    private bool isPlaying = false; // 控制是否正在播放动画

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (playOnAwake) StartTween(); // 如果设置了自动播放，则启动动画
    }

    public void StartTween()
    {
        elapsedTime = 0f;
        isPlaying = true; // 开始播放动画
    }

    private void Update()
    {
        if (isPlaying)
        {
            elapsedTime += Time.deltaTime;
            float factor = Mathf.Clamp01(elapsedTime / duration); // 计算动画进度系数

            // 使用四元数插值或欧拉角线性插值旋转
            if (quaternionLerp)
            {
                rectTransform.localRotation = Quaternion.Slerp(
                    Quaternion.Euler(from), 
                    Quaternion.Euler(to), 
                    factor
                );
            }
            else
            {
                rectTransform.localRotation = Quaternion.Euler(new Vector3(
                    Mathf.Lerp(from.x, to.x, factor),
                    Mathf.Lerp(from.y, to.y, factor),
                    Mathf.Lerp(from.z, to.z, factor)
                ));
            }

            // 检查动画是否完成
            if (factor >= 1f)
            {
                if (loop)
                {
                    elapsedTime = 0f; // 重置时间以实现循环
                }
                else
                {
                    isPlaying = false; // 停止播放
                }
            }
        }
    }

    /// <summary>
    /// 设置起始值为当前 RectTransform 的旋转值。
    /// </summary>
    [ContextMenu("Set 'From' to current value")]
    public void SetStartToCurrentValue()
    {
        from = rectTransform.localRotation.eulerAngles;
    }

    /// <summary>
    /// 设置终点值为当前 RectTransform 的旋转值。
    /// </summary>
    [ContextMenu("Set 'To' to current value")]
    public void SetEndToCurrentValue()
    {
        to = rectTransform.localRotation.eulerAngles;
    }

    /// <summary>
    /// 将当前旋转设置为 'from' 角度。
    /// </summary>
    [ContextMenu("Assume value of 'From'")]
    public void SetCurrentValueToStart()
    {
        rectTransform.localRotation = Quaternion.Euler(from);
    }

    /// <summary>
    /// 将当前旋转设置为 'to' 角度。
    /// </summary>
    [ContextMenu("Assume value of 'To'")]
    public void SetCurrentValueToEnd()
    {
        rectTransform.localRotation = Quaternion.Euler(to);
    }
}

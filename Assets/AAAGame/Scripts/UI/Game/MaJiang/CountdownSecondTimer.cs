using UnityEngine;

/// <summary>
/// 倒计时秒表计时器类
/// </summary>
public class CountdownSecondTimer
{
    /// <summary>
    /// 最大限制时间（秒）
    /// </summary>
    int limitTime = 20;
    
    /// <summary>
    /// 当前剩余时间（秒）
    /// </summary>
    int curtTime = 20;
    
    /// <summary>
    /// 开始计时的时间点
    /// </summary>
    float startTimingTime = 0;
    
    /// <summary>
    /// 设置最大限制时间
    /// </summary>
    /// <param name="tm">限制时间（秒），范围0-99</param>
    public void SetLimitTime(int tm)
    {
        tm = Mathf.Max(tm, 0);
        tm = Mathf.Min(tm, 99);
        limitTime = tm;
    }

    /// <summary>
    /// 设置当前时间
    /// </summary>
    /// <param name="tm">当前时间（秒）</param>
    public void SetCurtTime(int tm)
    {
        curtTime = tm;
    }

    /// <summary>
    /// 开始计时
    /// </summary>
    public void StartTime()
    {
        startTimingTime = Time.time;
        curtTime = limitTime;
    }

    /// <summary>
    /// 获取当前时间的十位和个位数字
    /// </summary>
    /// <returns>包含两个元素的数组，第一个为十位数字，第二个为个位数字</returns>
    public int[] GetGetCurtTimeNums()
    {
        int num1 = curtTime / 10;
        int num2 = curtTime - num1 * 10;
        return new int[] { num1, num2 };
    }

    /// <summary>
    /// 获取当前剩余时间
    /// </summary>
    /// <returns>当前剩余时间（秒）</returns>
    public int GetCurtTime()
    {
        return curtTime;
    }

    /// <summary>
    /// 更新计时
    /// </summary>
    /// <returns>更新后的当前剩余时间（秒）</returns>
    public int Timing()
    {
        float currentTime = Time.time;
        int tm = (int)Mathf.Floor(currentTime - startTimingTime);

        if (limitTime - tm < 0)
        {
            curtTime = 0;
            return 0;
        }

        curtTime = limitTime - tm;
        return curtTime;
    }
}

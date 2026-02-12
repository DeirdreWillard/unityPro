using UnityEngine;

/// <summary>
/// 通用工具类
/// </summary>
public class Common
{
    /// <summary>
    /// 获取随机数
    /// </summary>
    /// <param name="count"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="canEqual"></param>
    /// <returns></returns>
    static public int[] GetRandom(int count, int min, int max, bool canEqual = false)
    {
        int[] nums = new int[count];
        int num;

        if (canEqual || count > max)
        {
            for (int i = 0; i < count; i++)
            {
                nums[i] = Random.Range(min, max);
            }

            return nums;
        }


        for (int n = 0; n < count; n++)
        {
            num = Random.Range(min, max);

            for (int i = 0; i < n; i++)
            {
                if (nums[i] == num)
                {
                    num = Random.Range(min, max);
                    i = -1;
                }
            }

            nums[n] = num;
        }

        return nums;
    }

    /// <summary>
    /// 在数组中查找指定值的索引位置
    /// </summary>
    /// <param name="idxs">待查找的数组</param>
    /// <param name="idx">要查找的值</param>
    /// <returns>找到返回索引位置，未找到返回-1</returns>
    static public int IndexOf(int[] idxs, int idx)
    {
        if (idxs == null)
            return -1;

        for (int i = 0; i < idxs.Length; i++)
        {
            if (idxs[i] == idx)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// 计算浮点数的模运算结果
    /// </summary>
    /// <param name="a">被除数</param>
    /// <param name="b">除数</param>
    /// <returns>模运算结果</returns>
    static public float Mod(float a, float b)
    {
        int m = (int)(a * 1000f);
        int n = (int)(b * 1000f);

        float tm = m % n;
        tm *= 0.001f;
        return tm;
    }

}


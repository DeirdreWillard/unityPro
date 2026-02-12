using System;

public static class MathUtils
{
    /// <summary>
    /// 将浮动值四舍五入到指定的小数位数
    /// </summary>
    /// <param name="value">待四舍五入的值</param>
    /// <param name="decimalPlaces">小数位数</param>
    /// <returns>四舍五入后的值</returns>
    public static float Round(float value, int decimalPlaces = 1)
    {
        return (float)Math.Round(value, decimalPlaces);
    }

    /// <summary>
    /// 将浮动值向上取整到指定的小数位数
    /// </summary>
    /// <param name="value">待处理的值</param>
    /// <param name="decimalPlaces">小数位数</param>
    /// <returns>向上取整后的值</returns>
    public static float Ceiling(float value, int decimalPlaces)
    {
        double factor = Math.Pow(10, decimalPlaces);
        return (float)Math.Ceiling(value * factor) / (float)factor;
    }

    /// <summary>
    /// 将浮动值向下取整到指定的小数位数
    /// </summary>
    /// <param name="value">待处理的值</param>
    /// <param name="decimalPlaces">小数位数</param>
    /// <returns>向下取整后的值</returns>
    public static float Floor(float value, int decimalPlaces)
    {
        double factor = Math.Pow(10, decimalPlaces);
        return (float)Math.Floor(value * factor) / (float)factor;
    }

    /// <summary>
    /// 尝试将字符串转换为浮动值，并限制小数位数
    /// </summary>
    /// <param name="value">待转换的字符串</param>
    /// <param name="decimalPlaces">小数位数</param>
    /// <param name="result">转换后的结果</param>
    /// <returns>如果转换成功，返回true；否则返回false</returns>
    public static bool TryParseAndLimit(string value, int decimalPlaces, out float result)
    {
        if (float.TryParse(value, out result))
        {
            result = Round(result, decimalPlaces); // 四舍五入到指定的小数位数
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取两个数的最大值
    /// </summary>
    /// <param name="a">第一个数</param>
    /// <param name="b">第二个数</param>
    /// <returns>两个数中的最大值</returns>
    public static float Max(float a, float b)
    {
        return Math.Max(a, b);
    }

    /// <summary>
    /// 获取两个数的最小值
    /// </summary>
    /// <param name="a">第一个数</param>
    /// <param name="b">第二个数</param>
    /// <returns>两个数中的最小值</returns>
    public static float Min(float a, float b)
    {
        return Math.Min(a, b);
    }

    /// <summary>
    /// 判断一个数是否在指定的范围内
    /// </summary>
    /// <param name="value">待判断的数值</param>
    /// <param name="min">范围下限</param>
    /// <param name="max">范围上限</param>
    /// <returns>如果值在范围内，返回true；否则返回false</returns>
    public static bool IsBetween(float value, float min, float max)
    {
        return value >= min && value <= max;
    }

    /// <summary>
    /// 计算一个数的绝对值
    /// </summary>
    /// <param name="value">待计算的数</param>
    /// <returns>数值的绝对值</returns>
    public static float Abs(float value)
    {
        return Math.Abs(value);
    }

    /// <summary>
    /// 计算一个数的平方根
    /// </summary>
    /// <param name="value">待计算的数</param>
    /// <returns>平方根值</returns>
    public static float Sqrt(float value)
    {
        return (float)Math.Sqrt(value);
    }
}

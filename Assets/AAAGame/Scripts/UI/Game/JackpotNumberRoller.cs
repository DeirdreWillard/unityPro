﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class JackpotNumberRoller : MonoBehaviour
{
    public Transform numberParent; // 父对象容器
    public GameObject numberPrefab; // 单个数字的预制件 
    public int digitCount = 11; // 总位数（包括小数点）
    public float rollSpeed = 0.05f; // 每位数字滚动的速度

    private Text[] digitTexts; // 存储每个位的 Text 组件
    private int[] currentNumbers; // 当前显示的数字
    private int[] targetNumbers; // 目标数字
    private Coroutine currentRollCoroutine;
    private bool isRolling;

    public void SetData(float end, float start = 0f)
    {
        if (end < 0) return; // 不能输入负数
        if (isRolling && currentRollCoroutine != null)
        {
            CoroutineRunner.Instance.StopCoroutine(currentRollCoroutine);
        }

        if (digitTexts == null)
        {
            SetupDigits();
        }

        if (start != 0)
        {
            SetNumber(start); // 初始化数字
        }

        currentRollCoroutine = CoroutineRunner.Instance.RunCoroutine(RollToNumber(end));
    }


    void SetupDigits()
    {
        digitTexts = new Text[digitCount];
        currentNumbers = new int[digitCount];
        targetNumbers = new int[digitCount];

        for (int i = 0; i < digitCount; i++)
        {
            GameObject digitObj = Instantiate(numberPrefab, numberParent);
            digitTexts[i] = digitObj.GetComponent<Text>();

            // 初始化为0或小数点
            if (i == digitCount - 3) // 倒数第3位是小数点
                digitTexts[i].text = "@";
            else
                digitTexts[i].text = "0";
        }
    }


    public void SetNumber(float number)
    {
        string formatted = number.ToString("F2").PadLeft(digitCount, '0');
        for (int i = 0; i < digitCount; i++)
        {
            if (formatted[i] == '@') continue;
            currentNumbers[i] = int.Parse(formatted[i].ToString());
            digitTexts[i].text = currentNumbers[i].ToString();
        }
    }

    public IEnumerator RollToNumber(float target)
    {
        isRolling = true;
        string formatted = target.ToString("F2").PadLeft(digitCount, '0');

        // 初始化目标数字数组
        for (int i = 0; i < digitCount; i++)
        {
            if (formatted[i] == '.')
            {
                targetNumbers[i] = -1; // 小数点用 -1 表示
                continue;
            }
            targetNumbers[i] = int.Parse(formatted[i].ToString());
        }

        // 开始滚动
        bool stillRolling = true;
        while (stillRolling)
        {
            stillRolling = false;

            for (int i = 0; i < digitCount; i++)
            {
                if (targetNumbers[i] == -1) continue; // 跳过小数点

                // 目标数字与当前数字一致时，不需要继续滚动
                if (currentNumbers[i] == targetNumbers[i])
                    continue;

                stillRolling = true;

                // 滚动到目标值
                currentNumbers[i] = (currentNumbers[i] + 1) % 10;
                digitTexts[i].text = currentNumbers[i].ToString();
            }

            yield return new WaitForSeconds(rollSpeed);
        }

        // 滚动结束
        isRolling = false;
        currentRollCoroutine = null;
    }

}

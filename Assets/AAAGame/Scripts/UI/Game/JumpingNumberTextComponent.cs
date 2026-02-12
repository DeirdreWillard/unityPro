﻿﻿﻿using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class JumpingNumberTextComponent : MonoBehaviour
{
    [SerializeField]
    [Tooltip("按最高位起始顺序设置每位数字Text（显示组）")]
    private List<Text> _numbers;
    [SerializeField]
    [Tooltip("按最高位起始顺序设置每位数字Text（替换组）")]
    private List<Text> _unactiveNumbers;
    /// <summary>
    /// 动画时长
    /// </summary>
    [SerializeField]
    private float _duration = 5f;
    /// <summary>
    /// 数字每次滚动时长
    /// </summary>
    [SerializeField]
    private float _rollingDuration = 0.2f;
    /// <summary>
    /// 数字每次变动数值
    /// </summary>
    private float _speed;
    /// <summary>
    /// 滚动延迟（每进一位增加一倍延迟，让滚动看起来更随机自然）
    /// </summary>
    [SerializeField]
    private float _delay = 0.15f;
    /// <summary>
    /// Text文字宽高
    /// </summary>
    private Vector2 _numberSize;
    /// <summary>
    /// 当前数字
    /// </summary>
    private double _curNumber;
    /// <summary>
    /// 起始数字
    /// </summary>
    private double _fromNumber;
    /// <summary>
    /// 最终数字
    /// </summary>
    private double _toNumber;
    /// <summary>
    /// 各位数字的缓动实例
    /// </summary>
    private List<Tweener> _tweener = new List<Tweener>();
    /// <summary>
    /// 是否处于数字滚动中
    /// </summary>
    private bool _isJumping;
    /// <summary>
    /// 滚动完毕回调
    /// </summary>
    public Action OnComplete;

    void Start()
    {
        if (_numbers.Count == 0 || _unactiveNumbers.Count == 0)
        {
            Init();
        }
    }

    public void Init()
    {
        if (_numbers == null || _numbers.Count == 0)
        {
            _numbers = new List<Text>();
            _unactiveNumbers = new List<Text>();
            float numLength = transform.Find("Numbers").childCount;
            for (int i = 0; i < numLength; i++)
            {
                _numbers.Add(transform.Find("Numbers").GetChild(i).GetChild(0).GetComponent<Text>());
                _unactiveNumbers.Add(transform.Find("Numbers").GetChild(i).GetChild(1).GetComponent<Text>());
                _numbers[i].text = "0";
                _unactiveNumbers[i].text = "0";
            }
            _numberSize = _numbers[0].rectTransform.sizeDelta;
        }
    }

    public float duration
    {
        get { return _duration; }
        set
        {
            _duration = value;
        }
    }

    private double _different;
    public double different
    {
        get { return _different; }
    }

    public void Change(double from, double to)
    {
        // GF.LogError("from :" + from + " to :" + to);
        from = Math.Round(from, 2);
        to = Math.Round(to, 2);
        if (_numbers.Count == 0 || _unactiveNumbers.Count == 0)
        {
            Init();
        }

        bool isRepeatCall = _isJumping && _fromNumber == from && _toNumber == to;
        if (isRepeatCall) return;

        bool isContinuousChange = (_toNumber == from) && ((to - from > 0 && _different > 0) || (to - from < 0 && _different < 0));
        if (_isJumping && isContinuousChange)
        {
            // 不做任何操作，继续滚动
        }
        else
        {
            _fromNumber = from;
            _curNumber = _fromNumber;
        }
        _toNumber = to;

        _different = _toNumber - _fromNumber;
        // 当变化量小于单次滚动量时直接使用实际变化量
        float singleStep = (float)(_different / (_duration / _rollingDuration));
        _speed = Math.Abs(_different) > Math.Abs(singleStep) 
            ? (float)Math.Ceiling(singleStep)
            : (float)_different;
        
        _speed = Mathf.Approximately(_speed, 0) 
            ? (_different > 0 ? 1 : -1) 
            : _speed;

        SetNumber(_curNumber, false);
        _isJumping = true;

        CoroutineRunner.Instance.StopCoroutine(DoJumpNumber());
        CoroutineRunner.Instance.StartCoroutine(DoJumpNumber());
    }

    public double number
    {
        get { return _toNumber; }
        set
        {
            if (_toNumber == value) return;
            Change(_curNumber, _toNumber);
        }
    }

    IEnumerator DoJumpNumber()
    {
        float elapsed = 0f;
        while (_isJumping && _curNumber != _toNumber)
        {
            // 动态计算当前步长，确保最后一步精确到达目标值
            double remaining = _toNumber - _curNumber;
            double step = Math.Abs(remaining) < Math.Abs(_speed) 
                ? remaining 
                : _speed;
            
            _curNumber += step;
            SetNumber(_curNumber, true);

            if (_curNumber == _toNumber)
            {
                _isJumping = false;
                OnComplete?.Invoke();
                yield break;
            }

            elapsed += _rollingDuration;
            if (elapsed >= _duration) // 检查是否超过总体动画时长
            {
                _curNumber = _toNumber;
                SetNumber(_curNumber, true);
                _isJumping = false;
                OnComplete?.Invoke();
                yield break;
            }

            yield return new WaitForSeconds(_rollingDuration);
        }
    }

    /// <summary>
    /// 设置数字
    /// </summary>
    /// <param name="v"></param>
    /// <param name="isTween"></param>
    public void SetNumber(double v, bool isTween)
    {
        string s = Util.FormatJackpotText(v, true);

        if (!isTween)
        {
            for (int i = 0; i < _numbers.Count; i++)
            {
                if (i < s.Length)
                    _numbers[i].text = s[i] + "";
                else
                    _numbers[i].text = "0";
            }
        }
        else
        {
            while (_tweener.Count > 0)
            {
                _tweener[0].Complete();
                _tweener.RemoveAt(0);
            }

            for (int i = 0; i < _numbers.Count; i++)
            {
                if (i < s.Length)
                {
                    _unactiveNumbers[i].text = s[i] + "";
                }
                else
                {
                    _unactiveNumbers[i].text = "0";
                }

                _unactiveNumbers[i].rectTransform.anchoredPosition = new Vector2(_unactiveNumbers[i].rectTransform.anchoredPosition.x, (_speed > 0 ? -1 : 1) * _numberSize.y);
                _numbers[i].rectTransform.anchoredPosition = new Vector2(_unactiveNumbers[i].rectTransform.anchoredPosition.x, 0);

                // 检查是否需要更新（包括小数点位置）
                bool needUpdate = _unactiveNumbers[i].text != _numbers[i].text || 
                                (i == s.IndexOf('@') && _numbers[i].text != "@");
                if (needUpdate)
                {
                    DoTween(_numbers[i], (_speed > 0 ? 1 : -1) * _numberSize.y, _delay * i);
                    DoTween(_unactiveNumbers[i], 0, _delay * i);

                    Text tmp = _numbers[i];
                    _numbers[i] = _unactiveNumbers[i];
                    _unactiveNumbers[i] = tmp;
                }
            }
        }
    }

    public void DoTween(Text text, float endValue, float delay)
    {
        Tweener t = DOTween.To(() => text.rectTransform.anchoredPosition, (x) =>
        {
            text.rectTransform.anchoredPosition = x;
        }, new Vector2(text.rectTransform.anchoredPosition.x, endValue), _rollingDuration - delay).SetDelay(delay);
        _tweener.Add(t);
    }
}

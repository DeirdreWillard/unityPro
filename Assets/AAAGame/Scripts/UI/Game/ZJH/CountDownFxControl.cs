using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;
using NetMsg;

public class CountDownFxControl : MonoBehaviour
{
    //public int TotalSeconds;
    public Text CountDownMarkLabel;
    public Image CountDownCircleSprite;
    public int MaxStarCount;
    public float StarCreateInterval;
    public AudioSource audioSource;
    private List<GameObject> Stars;
    public GameObject MainStar;
    public bool useStarEffect = true;
    public float elapsedTime;
    private float elapsedStarTime;
    public int remainedSeconds;
    public int totalSeconds;
    private float testTime;
    private bool isStarted;
    public bool isLocalOperate;

    private float warningTime = 7;
    private bool isWarningShake = false;
    private GameObject shakeGo = null;

    Tweener shakeTweener = null;

    public Position Pos{get;set;}
    public static CountDownFxControl Instance = null;

    private void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (isStarted)
        {
            if (remainedSeconds > 0)
            {
                elapsedTime += Time.deltaTime;

                if (elapsedTime >= 1f)
                {
                    elapsedTime = 0f;
                    remainedSeconds -= 1;
                }
                if(remainedSeconds <= 0)
                {
                    gameObject.SetActive(false);
                    Pause();
                }

                // 计算当前进度
                float currentProgress = ((float)remainedSeconds - elapsedTime) / (float)totalSeconds;

                // 只有使用星星特效且Stars列表有效时才处理星星
                if (useStarEffect && Stars != null && Stars.Count > 0)
                {
                    if (Stars.Count < MaxStarCount)
                    {
                        elapsedStarTime += Time.deltaTime;
                        if (elapsedStarTime >= StarCreateInterval)
                        {
                            elapsedStarTime = 0f;
                            GameObject go = (GameObject)GameObject.Instantiate(Stars[0]);
                            go.transform.parent = Stars[0].transform.parent.transform;
                            go.transform.localPosition = Vector3.zero;
                            go.transform.localScale = Vector3.one;
                            Stars.Add(go);
                        }
                    }
                    for (int i = 0; i < Stars.Count; i++)
                    {
                        Stars[i].transform.eulerAngles = new Vector3(0, 0, 360f * (currentProgress + StarCreateInterval/10f * i));
                    }
                }
                
                if (CountDownCircleSprite != null) {
                    CountDownCircleSprite.fillAmount = currentProgress;
                }
                if (CountDownMarkLabel != null) {
                    if (remainedSeconds < 10)
                    {
                        CountDownMarkLabel.text = "0" + remainedSeconds.ToString() + "s";
                    }
                    else
                    {
                        CountDownMarkLabel.text = remainedSeconds.ToString() + "s";
                    }
                }
                if (isLocalOperate)
                {
                    if (remainedSeconds == warningTime && audioSource != null && !audioSource.isPlaying)
                    {
                        audioSource.Play();
                        GF.Sound.PlayVibrate();
                        if (isWarningShake)
                            #if !UNITY_WEBGL
                            Handheld.Vibrate();
                            #endif
                        if(shakeGo != null)
                        {
                            if (shakeTweener == null)
                                shakeTweener = shakeGo.transform.DOShakePosition(0.2f, new Vector3(10, 0, 0)).SetLoops(-1, LoopType.Yoyo);
                            else
                                shakeTweener.Play();
                        }
                    }
                    else if (audioSource != null && audioSource.isPlaying && remainedSeconds > warningTime)
                    {
                        audioSource.Stop();
                        PauseTweener();
                    }
                }
            }
        }
    }
    /// <summary>
    /// 设置警告时间
    /// </summary>
    public void SetWarningTime(float time,bool _isShake,GameObject go)
    {
        warningTime = time;
        isWarningShake = _isShake;
        shakeGo = go;
    }

    /// <summary>
    /// 开始倒计时
    /// </summary>
    /// <param name="TotalSeconds">总共时间秒</param>
    /// <param name="isLocalOp">是否本人</param>
    ///
    public void StartCountDown(int TotalSeconds,bool isLocalOp,Position position)
    {
        isLocalOperate = isLocalOp;
        Pos = position;
        SetTimeLabel(true);
        totalSeconds = TotalSeconds;
        remainedSeconds = totalSeconds;
        
        if (useStarEffect && MainStar != null)
        {
            Stars = new List<GameObject>
            {
                MainStar
            };
        }
        else
        {
            Stars = new List<GameObject>();
        }
        Continue();
    }

    /// <summary>
    /// 重新设定倒计时
    /// </summary>
    /// <param name="TotalSeconds">新的总时间</param>
    /// <param name="RemainedSeconds">新的剩余时间</param>
    public void ResetAndStartCountDown(int TotalSeconds, int RemainedSeconds,bool isLocalOp,Position position = Position.Default)
    {
        isLocalOperate = isLocalOp;
        Pos = position;
        SetTimeLabel(true);
        totalSeconds = TotalSeconds;
        remainedSeconds = RemainedSeconds;
        
        if (Stars == null)
        {
            if (useStarEffect && MainStar != null)
            {
                Stars = new List<GameObject>
                {
                    MainStar
                };
            }
            else
            {
                Stars = new List<GameObject>();
            }
        }
        Continue();
    }

    /// <summary>
    /// 设置颜色
    /// </summary>
    /// <param name="color"></param>
    public void SetFrameColor(Color color)
    {
        if (CountDownCircleSprite != null) {
            CountDownCircleSprite.color = color;
        }
    }

    /// <summary>
    /// 增加时间
    /// </summary>
    /// <param name="AddedSeconds">增加的时间秒</param>
    public void AddSeconds(int AddedSeconds)
    {
        remainedSeconds += AddedSeconds;
        totalSeconds += AddedSeconds;
    }

    public void SetTimeLabel(bool isShow)
    {
        CountDownMarkLabel?.gameObject.SetActive(isShow);
    }

    public void Pause()
    {
        isStarted = false;
        if (audioSource != null) {
            audioSource.Stop();
        }
        PauseTweener();
    }

    public void Continue()
    {
        isStarted = true;
    }

    public void PauseTweener()
    {
        shakeTweener?.Pause();
    }
}

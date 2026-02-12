using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class StaticUIComponent : GameFrameworkComponent
{
    [Header("等待框")]
    [SerializeField] GameObject waitingView = null;
    [SerializeField] Text waitingText = null;
    [Header("加载框")]
    [SerializeField] GameObject loadingView = null;
    [SerializeField] Text loadingText = null;
    [Header("麻将全屏加载框")]
    [SerializeField] GameObject mahjongLoadingView = null;
    [SerializeField] Text mahjongLoadingText = null;
    [SerializeField] List<Image> DotList = null;
    [Header("比赛匹配提示")]
    [SerializeField] GameObject matchingView = null;
    [SerializeField] Text matchingText = null;

    [Header("跑马灯公告")]
    [SerializeField] GameObject Notification = null;
    [SerializeField] Text marqueeText = null;
    [SerializeField] GameObject Notification_landscape = null;
    [SerializeField] Text marqueeText_landscape = null;
    [Header("跑马灯速度(像素/秒)")]
    [SerializeField] float marqueeSpeedPortrait = 100f;
    [SerializeField] float marqueeSpeedLandscape = 100f;
    private Queue<string> messageQueue = new Queue<string>();
    private bool isPlaying = false;
    private Coroutine m_MarqueeCoroutine;
    private ScreenOrientation m_LastOrientation;

    [Header("对象池")]
    [SerializeField] private GameObject m_ErrorItemPrefab = null;
    [SerializeField] private GameObject m_CoinPrefab = null;
    [SerializeField] private Transform m_PoolRoot = null;
    
    // 错误提示对象池
    private GameObjectPool errorItemPool = new GameObjectPool();
    // 金币对象池
    private GameObjectPool coinItemPool = new GameObjectPool();

    private Coroutine m_MatchingCoroutine;
    private bool m_IsMatching = false;

    private void OnEnable()
    {
        waitingView.SetActive(false);
        loadingView.SetActive(false);
        mahjongLoadingView.SetActive(false);
        Notification.SetActive(false);
        Notification_landscape.SetActive(false);
        m_LastOrientation = Screen.orientation;
        
        // 初始化对象池
        InitializeObjectPools();
    }

    private void InitializeObjectPools()
    {
        if (m_ErrorItemPrefab != null && errorItemPool != null)
        {
            errorItemPool.CreatePool(m_ErrorItemPrefab, 5, m_PoolRoot);
        }
        
        if (m_CoinPrefab != null && coinItemPool != null)
        {
            coinItemPool.CreatePool(m_CoinPrefab, 40, m_PoolRoot);
        }
    }

    private void Start()
    {
        UpdateCanvasScaler();

        
    }

    private void Update()
    {
        if (Screen.orientation != m_LastOrientation)
        {
            m_LastOrientation = Screen.orientation;
            HandleOrientationChanged();
        }
    }

    private void HandleOrientationChanged()
    {
        if (m_MarqueeCoroutine != null)
        {
            StopCoroutine(m_MarqueeCoroutine);
            m_MarqueeCoroutine = null;
        }

        isPlaying = false;
        marqueeText.rectTransform.DOKill();
        marqueeText_landscape.rectTransform.DOKill();
        Notification.SetActive(false);
        Notification_landscape.SetActive(false);
    }

    public void UpdateCanvasScaler()
    {
        var uiRootCanvas = GFBuiltin.RootCanvas;
        var canvasRoot = this.GetComponent<Canvas>();
        canvasRoot.worldCamera = uiRootCanvas.worldCamera;
        canvasRoot.planeDistance = uiRootCanvas.planeDistance;
        canvasRoot.sortingLayerID = uiRootCanvas.sortingLayerID;
        canvasRoot.sortingOrder = uiRootCanvas.sortingOrder;
        canvasRoot.sortingOrder = 10000;

        var canvasScaler = this.GetComponent<CanvasScaler>();
        var uiRootScaler = uiRootCanvas.GetComponent<CanvasScaler>();

        canvasScaler.uiScaleMode = uiRootScaler.uiScaleMode;
        canvasScaler.screenMatchMode = uiRootScaler.screenMatchMode;
        canvasScaler.matchWidthOrHeight = uiRootScaler.matchWidthOrHeight;
        canvasScaler.referencePixelsPerUnit = uiRootScaler.referencePixelsPerUnit;
        canvasScaler.referenceResolution = uiRootScaler.referenceResolution;
    }

    #region 等待框

    public void HideWaiting()
    {
        waitingText.text = "";
        waitingView.SetActive(false);
    }
    public void ShowWaiting(string content)
    {
        waitingText.text = content;
        waitingView.SetActive(true);
    }
    #endregion

    #region 全屏加载

    public void HideLoading()
    {
        loadingText.text = "";
        loadingView.SetActive(false);
    }
    public void ShowLoading(string content)
    {
        loadingText.text = content;
        loadingView.SetActive(true);
    }
    #endregion

    #region 麻将全屏加载
    public void HideMahjongLoading()
    {
        mahjongLoadingText.text = "";
        // DOTween.Kill("MahjongLoading_Dots", false);
        mahjongLoadingView.SetActive(false);
        
        // 快速重置所有点的位置和透明度
        // foreach (var dot in DotList)
        // {
        //     dot.color = new Color(dot.color.r, dot.color.g, dot.color.b, 0.2f);
        //     dot.transform.localPosition = new Vector3(dot.transform.localPosition.x, 0f, dot.transform.localPosition.z);
        // }
    }
    public void ShowMahjongLoading( string content)
    {
        mahjongLoadingText.text = content;
        mahjongLoadingView.SetActive(true);
        // DOTween.Kill("MahjongLoading_Dots", false);
        
        // 重置所有点的初始状态
        // foreach (var dot in DotList)
        // {
        //     dot.transform.localPosition = new Vector3(dot.transform.localPosition.x, 0f, dot.transform.localPosition.z);
        //     dot.color = new Color(dot.color.r, dot.color.g, dot.color.b, 1f);
        // }
        
        // Sequence seq = DOTween.Sequence();
        // seq.SetId("MahjongLoading_Dots");
        
        // for (int i = 0; i < DotList.Count; i++)
        // {
        //     var dot = DotList[i];
        //     // 只使用Y轴位移创建跳动效果，优化性能
        //     seq.Append(dot.transform.DOLocalMoveY(20f, 0.15f).SetEase(Ease.OutQuad));
        //     seq.Append(dot.transform.DOLocalMoveY(0f, 0.15f).SetEase(Ease.InQuad));
        // }
        // seq.SetLoops(-1, LoopType.Restart);
    }
    #endregion

    #region 比赛匹配
    public void ShowMatching(bool _isShow)
    {
        if (_isShow && !m_IsMatching)
        {
            matchingView.SetActive(true);
            m_IsMatching = true;
            if (m_MatchingCoroutine != null)
                StopCoroutine(m_MatchingCoroutine);
            m_MatchingCoroutine = StartCoroutine(AnimateMatchingText());
        }
        else if (!_isShow && m_IsMatching)
        {
            m_IsMatching = false;
            if (m_MatchingCoroutine != null)
                StopCoroutine(m_MatchingCoroutine);
            matchingView.SetActive(false);
        }
    }

    private IEnumerator AnimateMatchingText()
    {
        string[] c_MatchingTexts = new string[]
        {
            "匹配中",
            "匹配中.",
            "匹配中..",
            "匹配中..."
        };

        int m_CurrentIndex = 0;

        while (m_IsMatching)
        {
            matchingText.text = c_MatchingTexts[m_CurrentIndex];
            m_CurrentIndex = (m_CurrentIndex + 1) % c_MatchingTexts.Length;

            yield return new WaitForSeconds(0.4f);
        }
    }


    #endregion
    
    #region 跑马灯公告
    public void ShowNotification(string message)
    {
        messageQueue.Enqueue(message);
        if (!isPlaying)
        {
            m_MarqueeCoroutine = StartCoroutine(PlayMarquee());
        }
    }

    private IEnumerator PlayMarquee()
    {
        isPlaying = true;

        while (messageQueue.Count > 0)
        {
            Notification.SetActive(true);
            marqueeText.text = messageQueue.Dequeue();

            // 强制刷新布局并获取最新尺寸
            Canvas.ForceUpdateCanvases();

            // 获取文本和容器尺寸
            float textWidth = marqueeText.preferredWidth;
            RectTransform parentRect = marqueeText.transform.parent.GetComponent<RectTransform>();
            float containerWidth = parentRect.rect.width;

            // 设置初始位置：右侧边缘完全超出容器
            float startX = (parentRect.pivot.x * parentRect.rect.width) + textWidth / 2;
            marqueeText.rectTransform.anchoredPosition = new Vector2(startX, 0);

            // 计算滚动时间
            float distance = textWidth + containerWidth;
            float duration = distance / Mathf.Max(1f, marqueeSpeedPortrait);

            // 执行滚动动画
            var tween = marqueeText.rectTransform.DOLocalMoveX(startX - distance, duration)
                .SetEase(Ease.Linear);

            yield return tween.WaitForCompletion();
            Notification.SetActive(false);

            // 播放间隔等待
            yield return new WaitForSeconds(3f);
        }

        isPlaying = false;
        m_MarqueeCoroutine = null;
    }

    public void ShowNotificationLandscape(string message)
    {
        messageQueue.Enqueue(message);
        if (!isPlaying)
        {
            m_MarqueeCoroutine = StartCoroutine(PlayMarqueeLandscape());
        }
    }

    private IEnumerator PlayMarqueeLandscape()
    {
        isPlaying = true;

        while (messageQueue.Count > 0)
        {
            Notification_landscape.SetActive(true);
            marqueeText_landscape.text = messageQueue.Dequeue();

            // 强制刷新布局并获取最新尺寸
            Canvas.ForceUpdateCanvases();

            // 获取文本和容器尺寸
            float textWidth = marqueeText_landscape.preferredWidth;
            RectTransform parentRect = marqueeText_landscape.transform.parent.GetComponent<RectTransform>();
            float containerWidth = parentRect.rect.width;

            // 设置初始位置：右侧边缘完全超出容器
            float startX = (parentRect.pivot.x * parentRect.rect.width) + textWidth / 2;
            marqueeText_landscape.rectTransform.anchoredPosition = new Vector2(startX, 0);

            // 计算滚动时间
            float distance = textWidth + containerWidth;
            float duration = distance / Mathf.Max(1f, marqueeSpeedLandscape);

            // 执行滚动动画
            var tween = marqueeText_landscape.rectTransform.DOLocalMoveX(startX - distance, duration)
                .SetEase(Ease.Linear);

            yield return tween.WaitForCompletion();
            Notification_landscape.SetActive(false);

            // 播放间隔等待
            yield return new WaitForSeconds(3f);
        }

        isPlaying = false;
        m_MarqueeCoroutine = null;
    }
    #endregion

    #region Error 提示
    public void ShowError(string error)
    {
        if (string.IsNullOrEmpty(error))
        {
            GF.LogWarning("内容为空");
            return;
        }
        var item = errorItemPool.PopGameObject();
        item.SetActive(true);
        var textComp = item.transform.GetChild(0).GetComponent<Text>();
        textComp.text = error;
        CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        var rectTrans = item.GetComponent<RectTransform>();
        rectTrans.anchoredPosition = new Vector2(0, 0);
        rectTrans.localScale = Vector3.zero;
        Sequence sequence = DOTween.Sequence();
        sequence.Append(rectTrans.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack));
        sequence.Join(canvasGroup.DOFade(1, 0.3f));
        sequence.Join(DOTween.To(() => rectTrans.anchoredPosition.y,
            y => rectTrans.anchoredPosition = new Vector2(rectTrans.anchoredPosition.x, y),
            rectTrans.anchoredPosition.y + 80, 0.8f).SetEase(Ease.OutQuad));
        sequence.AppendInterval(0.6f);
        sequence.Append(rectTrans.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));
        sequence.Join(canvasGroup.DOFade(0, 0.3f));
        sequence.Join(DOTween.To(() => rectTrans.anchoredPosition.y,
            y => rectTrans.anchoredPosition = new Vector2(rectTrans.anchoredPosition.x, y),
            rectTrans.anchoredPosition.y + 100, 0.3f).SetEase(Ease.InQuad));
        sequence.OnComplete(() =>
        {
            errorItemPool.PushGameObject(item);
        });
    }

    #endregion

    // 提供对象池接口供其他模块使用
    public GameObject GetCoinFromPool()
    {
        var coin = coinItemPool.PopGameObject();
        coin.SetActive(true);
        return coin;
    }

    public void ReturnCoinToPool(GameObject coin)
    {
        coinItemPool.PushGameObject(coin);
    }

}

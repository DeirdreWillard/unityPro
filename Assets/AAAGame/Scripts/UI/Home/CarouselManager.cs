using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;

public class CarouselManager : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    #region Private Fields
    [SerializeField] private GameObject m_SlidePrefab;
    [SerializeField] private Transform m_Content;
    [SerializeField] private float m_SwitchThreshold = 50f;
    [SerializeField] private float m_TransitionSpeed = 5f;
    [SerializeField] private ScrollRect m_ScrollRect;
    [SerializeField] private bool m_AutoScroll = true;
    [SerializeField, Range(1f, 20f)] private float m_AutoScrollInterval = 3f;
    [SerializeField, Range(0.01f, 0.2f)] private float m_LoopThreshold = 0.05f; // 调小循环跳转阈值
    [SerializeField, Range(0.01f, 0.5f)] private float m_JumpDelay = 0.05f; // 减小跳转延迟，提高响应速度
    private float m_AutoScrollTimer;
    #endregion

    #region Internal State
    private float m_TargetPosition;
    private bool m_IsDragging;
    private int m_CurrentIndex; // 真实索引
    private Action<int> m_OnSlideClick;
    private List<GameObject> m_Slides = new List<GameObject>();
    private int m_RealCount; // 真实图片数量
    private bool m_IsJumping; // 是否正在跳转中
    private float m_JumpTimer; // 跳转计时器
    #endregion

    #region Public Methods
    /// <summary>
    /// 初始化轮播图（无限循环）
    /// </summary>
    /// <param name="_sprites">图片数组</param>
    /// <param name="_onClick">点击回调，参数为当前索引</param>
    public void Initialize(Sprite[] _sprites, Action<int> _onClick)
    {
        // 清理旧的
        foreach (var slide in m_Slides)
            Destroy(slide);
        m_Slides.Clear();

        m_OnSlideClick = _onClick;
        m_CurrentIndex = 0;
        m_TargetPosition = 0f;
        m_RealCount = _sprites.Length;
        if (m_RealCount == 0) return;

        // 头部补最后一张
        AddSlide(_sprites[m_RealCount - 1], -1);
        // 正常图片
        for (int i = 0; i < m_RealCount; i++)
            AddSlide(_sprites[i], i);
        // 尾部补第一张
        AddSlide(_sprites[0], m_RealCount);

        // 重置ScrollRect到第1张（实际索引1）
        if (m_ScrollRect != null)
            m_ScrollRect.horizontalNormalizedPosition = 1f / (m_Slides.Count - 1);

        m_CurrentIndex = 0;
        m_TargetPosition = 1f / (m_Slides.Count - 1);
        m_AutoScrollTimer = 0f;
        m_IsJumping = false;
        m_JumpTimer = 0f;
    }

    public void SetAutoScroll(bool _enable, float _interval = 3f)
    {
        m_AutoScroll = _enable;
        m_AutoScrollInterval = _interval;
        m_AutoScrollTimer = 0f;
    }
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        if (m_Slides.Count <= 1) return;

        float currentPos = m_ScrollRect.horizontalNormalizedPosition;
        
        // 非拖动状态时的处理
        if (!m_IsDragging)
        {
            // 是否在跳转中
            if (m_IsJumping)
            {
                m_JumpTimer += Time.deltaTime;
                if (m_JumpTimer >= m_JumpDelay)
                {
                    m_IsJumping = false;
                    m_JumpTimer = 0f;

                    // 执行实际跳转
                    if (m_CurrentIndex < 0)
                    {
                        m_CurrentIndex = m_RealCount - 1;
                        m_ScrollRect.horizontalNormalizedPosition = (float)(m_CurrentIndex + 1) / (m_Slides.Count - 1);
                        m_TargetPosition = m_ScrollRect.horizontalNormalizedPosition;
                    }
                    else if (m_CurrentIndex >= m_RealCount)
                    {
                        m_CurrentIndex = 0;
                        m_ScrollRect.horizontalNormalizedPosition = (float)(m_CurrentIndex + 1) / (m_Slides.Count - 1);
                        m_TargetPosition = m_ScrollRect.horizontalNormalizedPosition;
                    }
                }
                return; // 跳转中不执行其他逻辑
            }

            // 平滑过渡到目标位置
            m_ScrollRect.horizontalNormalizedPosition = Mathf.Lerp(currentPos, m_TargetPosition, Time.deltaTime * m_TransitionSpeed);

            // 自动滚动逻辑
            if (m_AutoScroll)
            {
                m_AutoScrollTimer += Time.deltaTime;
                if (m_AutoScrollTimer >= m_AutoScrollInterval)
                {
                    m_AutoScrollTimer = 0f;
                    AutoScrollToNext();
                }
            }

            // 提前检测是否接近边缘补帧，使用阈值判断
            if (m_RealCount > 0 && !m_IsJumping)
            {
                float distance = Mathf.Abs(currentPos - m_TargetPosition);
                if (distance < 0.01f) // 当接近目标位置时才进行首尾检测
                {
                    // 接近首部补帧
                    if (currentPos <= m_LoopThreshold)
                    {
                        PrepareToJump(-1); // 准备跳到最后一张
                    }
                    // 接近尾部补帧
                    else if (currentPos >= 1f - m_LoopThreshold)
                    {
                        PrepareToJump(m_RealCount); // 准备跳到第一张
                    }
                }
            }
        }
    }
    #endregion

    #region Drag Events
    public void OnBeginDrag(PointerEventData eventData)
    {
        m_IsDragging = true;
        m_AutoScrollTimer = 0f;
        m_IsJumping = false; // 拖动时取消任何跳转
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        m_IsDragging = false;
        m_AutoScrollTimer = 0f;
        float dragDistance = eventData.pressPosition.x - eventData.position.x;

        if (m_RealCount == 0) return;

        if (Mathf.Abs(dragDistance) >= m_SwitchThreshold)
        {
            if (dragDistance > 0 && m_CurrentIndex < m_RealCount - 1)
                m_CurrentIndex++;
            else if (dragDistance < 0 && m_CurrentIndex > 0)
                m_CurrentIndex--;
            else if (dragDistance > 0 && m_CurrentIndex == m_RealCount - 1)
                m_CurrentIndex++; // 到补帧
            else if (dragDistance < 0 && m_CurrentIndex == 0)
                m_CurrentIndex--; // 到补帧
        }
        
        CalculateTargetPosition();
    }
    #endregion

    #region Private Methods
    private void AddSlide(Sprite _sprite, int _index)
    {
        GameObject slide = Instantiate(m_SlidePrefab, m_Content);
        Image image = slide.GetComponent<Image>();
        if (image != null)
            image.sprite = _sprite;

        Button button = slide.GetComponent<Button>();
        if (button != null)
        {
            int capturedIndex = _index;
            button.onClick.AddListener(() =>
            {
                // 只在真实图片上触发
                if (capturedIndex >= 0 && capturedIndex < m_RealCount)
                    m_OnSlideClick?.Invoke(capturedIndex);
            });
        }
        m_Slides.Add(slide);
    }

    private void AutoScrollToNext()
    {
        if (m_RealCount <= 1) return;
        
        // 无论当前在哪个位置，都自然滚动到下一张
        m_CurrentIndex++;
        
        // 如果超出实际图片数量，不要立即跳回，而是让它滚动到补帧的第一张
        // 跳转会在Update中通过边缘检测自动处理
        m_TargetPosition = (float)(m_CurrentIndex + 1) / (m_Slides.Count - 1);
        
        // 重置自动滚动计时器
        m_AutoScrollTimer = 0f;
    }

    private void CalculateTargetPosition()
    {
        // 计算下一个目标位置
        m_TargetPosition = (float)(m_CurrentIndex + 1) / (m_Slides.Count - 1);

        // 自动滚动模式下，即使到了边缘索引也不立即准备跳转
        // 而是等到实际滚动到边缘位置时再跳转
        if (!m_AutoScroll && (m_CurrentIndex < 0 || m_CurrentIndex >= m_RealCount))
        {
            m_IsJumping = true;
            m_JumpTimer = 0f;
        }
    }

    private void PrepareToJump(int targetIndex)
    {
        // 设置索引以便之后跳转
        m_CurrentIndex = targetIndex;
        m_IsJumping = true;
        m_JumpTimer = 0f;
        
        // 立即停止当前的自动滚动计时
        m_AutoScrollTimer = 0f;
    }

    // 添加新方法来获取当前真实索引（不包括补帧）
    public int GetCurrentRealIndex()
    {
        if (m_RealCount == 0) return 0;
        if (m_CurrentIndex < 0) return m_RealCount - 1;
        if (m_CurrentIndex >= m_RealCount) return 0;
        return m_CurrentIndex;
    }
    
    // 添加新方法直接跳转到指定索引
    public void JumpToSlide(int index)
    {
        if (m_RealCount == 0) return;
        
        // 限制索引范围
        index = Mathf.Clamp(index, 0, m_RealCount - 1);
        
        m_CurrentIndex = index;
        m_TargetPosition = (float)(m_CurrentIndex + 1) / (m_Slides.Count - 1);
        m_ScrollRect.horizontalNormalizedPosition = m_TargetPosition;
        m_AutoScrollTimer = 0f;
    }
    #endregion
}

using UnityEngine;
using UnityEngine.EventSystems;
using System;
using DG.Tweening;
using NetMsg;

/// <summary>
/// 2D麻将手牌交互脚本
/// 支持三种出牌方式:
/// 1. 拖动出牌: 拖动手牌超过一定高度后松手出牌
/// 2. 点击抬高: 点击手牌抬高,再次点击出牌
/// 3. 双击出牌: 快速双击手牌直接出牌
/// </summary>
public class MjCard_2D_Interaction : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    #region 配置参数
    
    /// <summary>
    /// 抬高的高度(像素)
    /// </summary>
    [SerializeField]
    private float raiseHeight = 30f;
    
    /// <summary>
    /// 拖动出牌的最小高度(像素)
    /// </summary>
    [SerializeField]
    private float dragPlayMinHeight = 100f;
    
    /// <summary>
    /// 拖动出牌的最小速度(像素/帧)
    /// </summary>
    [SerializeField]
    private float dragPlayMinSpeed = 50f;
    
    /// <summary>
    /// 出牌冷却时间(秒) - 防止快速连续出牌
    /// </summary>
    [SerializeField]
    private float playCardCooldown = 2f;
    
    #endregion
    
    #region 私有变量
    
    /// <summary>
    /// 手牌在手中的索引
    /// </summary>
    private int cardIndex = -1;
    
    /// <summary>
    /// 手牌的牌值
    /// </summary>
    public int cardValue = -1;
    
    /// <summary>
    /// 麻将座位引用
    /// </summary>
    private MahjongSeat_2D mahjongSeat;
    
    /// <summary>
    /// RectTransform组件
    /// </summary>
    private RectTransform rectTransform;
    
    /// <summary>
    /// 原始位置
    /// </summary>
    private Vector2 originalPosition;
    
    /// <summary>
    /// 是否已抬高
    /// </summary>
    public bool isRaised = false;
    
    /// <summary>
    /// 是否为摸牌
    /// </summary>
    private bool isMoPai = false;
    
    /// <summary>
    /// 上次点击时间
    /// </summary>
    private float lastClickTime = 0f;
    
    /// <summary>
    /// 抬高的时间
    /// </summary>
    private float raiseTime = 0f;
    
    /// <summary>
    /// 上次出牌时间
    /// </summary>
    private float lastPlayTime = 0f;
    
    /// <summary>
    /// 是否正在出牌中(防止重复触发)
    /// </summary>
    private bool isPlaying = false;
    
    /// <summary>
    /// 是否正在拖动
    /// </summary>
    private bool isDragging = false;
    
    /// <summary>
    /// 是否刚完成拖动(用于避免拖动后触发点击)
    /// </summary>
    private bool justFinishedDrag = false;
    
    /// <summary>
    /// 拖动开始位置
    /// </summary>
    private Vector2 dragStartPosition;
    
    /// <summary>
    /// 上一帧拖动位置
    /// </summary>
    private Vector2 lastDragPosition;
    
    /// <summary>
    /// 拖动速度
    /// </summary>
    private float dragSpeed = 0f;
    
    /// <summary>
    /// 出牌回调 (索引, 牌值, 是否为摸牌)
    /// </summary>
    private Action<int, int, bool> onPlayCard;
    
    /// <summary>
    /// Canvas的缩放因子
    /// </summary>
    private float canvasScaleFactor = 1f;
    
    /// <summary>
    /// 原始的Sibling Index(用于恢复层级)
    /// </summary>
    private int originalSiblingIndex = -1;
    
    /// <summary>
    /// 拖动时临时添加的Canvas组件(用于覆盖所有UI)
    /// </summary>
    private Canvas dragCanvas = null;
    
    /// <summary>
    /// 拖动时临时添加的GraphicRaycaster组件
    /// </summary>
    private UnityEngine.UI.GraphicRaycaster dragRaycaster = null;
    
    #endregion
    
    #region 初始化
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // 获取Canvas的缩放因子
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasScaleFactor = canvas.scaleFactor;
        }
    }
    
    /// <summary>
    /// 初始化手牌交互
    /// </summary>
    /// <param name="seat">麻将座位</param>
    /// <param name="index">手牌索引</param>
    /// <param name="value">牌值</param>
    /// <param name="isMoPaiCard">是否为摸牌</param>
    /// <param name="playCallback">出牌回调</param>
    public void Initialize(MahjongSeat_2D seat, int index, int value, bool isMoPaiCard, Action<int, int, bool> playCallback)
    {
        mahjongSeat = seat;
        cardIndex = index;
        cardValue = value;
        isMoPai = isMoPaiCard;
        onPlayCard = playCallback;
        
        // 记录原始位置
        originalPosition = rectTransform.anchoredPosition;
        
        // 重置所有状态
        isRaised = false;
        isPlaying = false;
        isDragging = false;
        justFinishedDrag = false;
        lastClickTime = 0f;
        raiseTime = 0f;
        lastPlayTime = 0f;
        dragSpeed = 0f;
        originalSiblingIndex = -1;
        
        // 确保交互已启用
        enabled = true;
    }
    
    /// <summary>
    /// 更新牌值(当手牌重新排序时)
    /// </summary>
    public void UpdateCardInfo(int index, int value)
    {
        cardIndex = index;
        cardValue = value;
        originalPosition = rectTransform.anchoredPosition;
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 指针按下
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (cardIndex < 0 || cardValue < 0)
            return;
        
        // 如果正在出牌中，忽略所有输入
        if (isPlaying)
        {
            GF.LogWarning($"[MjCard_2D_Interaction] 出牌中，忽略输入");
            return;
        }
        
        // 检查是否在出牌冷却期内
        float timeSinceLastPlay = Time.time - lastPlayTime;
        if (timeSinceLastPlay < playCardCooldown)
        {
            GF.LogWarning($"[MjCard_2D_Interaction] 出牌冷却中({timeSinceLastPlay:F2}秒)，忽略输入");
            return;
        }
        
        // 重置拖动完成标志
        justFinishedDrag = false;

        dragStartPosition = rectTransform.anchoredPosition;
        lastDragPosition = eventData.position;
    }
    
    /// <summary>
    /// 指针抬起
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (cardIndex < 0 || cardValue < 0)
            return;
        
        // 如果正在出牌中，忽略所有输入
        if (isPlaying)
            return;
        
        // 如果刚完成拖动,不处理点击逻辑
        if (justFinishedDrag)
        {
            justFinishedDrag = false;
            return;
        }
        
        // 如果没有拖动,处理点击抬高逻辑
        if (!isDragging)
        {
            OnClick();
        }
    }
    
    /// <summary>
    /// 开始拖动
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cardIndex < 0 || cardValue < 0)
            return;
        
        // 如果正在出牌中，忽略拖动
        if (isPlaying)
            return;
        
        // ✅ 修复：检查是否允许打牌，不允许则不能拖动
        if (mahjongSeat?.SeatIndex == 0 && mahjongSeat?.mjGameUI != null)
        {
            // 如果是换牌状态，禁止拖动
            if (mahjongSeat.mjGameUI.mj_procedure != null && 
                mahjongSeat.mjGameUI.mj_procedure.enterMJDeskRs != null &&
                mahjongSeat.mjGameUI.mj_procedure.enterMJDeskRs.CkState == MJState.ChangeCard)
            {
                return;
            }

            // 如果是甩牌状态，禁止拖动
            if (mahjongSeat.mjGameUI.mj_procedure != null && 
                mahjongSeat.mjGameUI.mj_procedure.enterMJDeskRs != null &&
                mahjongSeat.mjGameUI.mj_procedure.enterMJDeskRs.CkState == MJState.ThrowCard)
            {
                return;
            }

            if (!mahjongSeat.mjGameUI.Can2DPlayCard())
            {
                GF.LogWarning($"[MjCard_2D_Interaction] 当前不允许打牌，禁止拖动");
                return;
            }
        }
        
        isDragging = true;
        dragStartPosition = rectTransform.anchoredPosition;
        lastDragPosition = eventData.position;
        dragSpeed = 0f;
        
        // 提升渲染层级,防止被其他手牌遮挡
        BringToFront();
    }
    
    /// <summary>
    /// 拖动中
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (cardIndex < 0 || cardValue < 0 || !isDragging)
            return;
        
        // 如果正在出牌中，忽略拖动
        if (isPlaying)
            return;
        
        // 计算拖动偏移(考虑Canvas缩放)
        Vector2 offset = (eventData.position - eventData.pressPosition) / canvasScaleFactor;
        
        // 允许自由拖动(上下左右)
        Vector2 newPosition = dragStartPosition + offset;
        rectTransform.anchoredPosition = newPosition;
        
        // 计算拖动速度
        dragSpeed = Vector2.Distance(lastDragPosition, eventData.position);
        lastDragPosition = eventData.position;
    }
    
    /// <summary>
    /// 结束拖动
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (cardIndex < 0 || cardValue < 0 || !isDragging)
            return;
        
        // 如果正在出牌中，忽略
        if (isPlaying)
        {
            isDragging = false;
            return;
        }
        
        isDragging = false;
        justFinishedDrag = true; // 标记刚完成拖动
        
        // 计算拖动距离
        float dragDistance = rectTransform.anchoredPosition.y - dragStartPosition.y;

        // 恢复位置（使用动画）
        RestorePosition(true);
        // 恢复渲染层级
        RestoreRenderOrder();

        // 判断是否出牌
        // 条件1: 拖动高度超过最小高度
        // 条件2: 或者拖动速度超过最小速度
        if (dragDistance > dragPlayMinHeight || dragSpeed > dragPlayMinSpeed)
        {
            // 出牌
            OnPlayCard();
        }
        
        dragSpeed = 0f;
    }
    
    #endregion
    
    #region 交互逻辑
    
    /// <summary>
    /// 单击处理
    /// </summary>
    private void OnClick()
    {
        // 检查是否在出牌中
        if (isPlaying)
        {
            GF.LogWarning($"[MjCard_2D_Interaction] 出牌中，忽略点击");
            return;
        }

        // ✅ 检查是否在换牌状态
        if (mahjongSeat != null && mahjongSeat.mjGameUI != null && 
            mahjongSeat.mjGameUI.mj_procedure != null && 
            mahjongSeat.mjGameUI.mj_procedure.enterMJDeskRs != null &&
            mahjongSeat.mjGameUI.mj_procedure.enterMJDeskRs.CkState == MJState.ChangeCard)
        {
            if (isRaised)
            {
                RestorePosition(true);
            }
            else
            {
                RaiseCard();
            }
            // 通知UI更新选中状态
            mahjongSeat.mjGameUI.OnChangeCardSelect(this);
            return;
        }

        // ✅ 检查是否在甩牌状态（血流成河）
        if (mahjongSeat != null && mahjongSeat.mjGameUI != null && 
            mahjongSeat.mjGameUI.mj_procedure != null && 
            mahjongSeat.mjGameUI.mj_procedure.enterMJDeskRs != null &&
            mahjongSeat.mjGameUI.mj_procedure.enterMJDeskRs.CkState == MJState.ThrowCard)
        {
            if (isRaised)
            {
                RestorePosition(true);
            }
            else
            {
                RaiseCard();
            }
            // 通知UI更新甩牌选中状态
            mahjongSeat.mjGameUI.OnThrowCardSelect(this);
            return;
        }
        
        if (isRaised)
        {
            // 已抬高，再次点击直接出牌
            OnPlayCard();
        }
        else
        {
            // 未抬高，抬高手牌
            RaiseCard();
        }
    }
    
    /// <summary>
    /// 抬高手牌
    /// </summary>
    private void RaiseCard()
    {
        if (isRaised)
            return;
        
        // 先将其他抬高的手牌恢复
        if (mahjongSeat != null)
        {
            mahjongSeat.RestoreAllRaisedCards(this);
        }
        
        isRaised = true;
        raiseTime = Time.time; // 记录抬高时间
        Vector2 raisedPosition = originalPosition + new Vector2(0, raiseHeight);
        
        // 使用DOTween添加抬高动画
        rectTransform.DOAnchorPos(raisedPosition, 0.15f).SetEase(Ease.OutBack);
        
        GF.LogInfo($"[MjCard_2D_Interaction] 抬高手牌: 索引={cardIndex}, 牌值={cardValue}");

        // ✅ 检查打出该牌后是否可听牌（显示胡牌提示）
        mahjongSeat.mjGameUI.ShowTingHuTipsForCard(Util.ConvertServerIntToFaceValue(cardValue));

        // 通知UI已抬高该牌（传递索引用于亮牌流程）
        mahjongSeat.mjGameUI.OnCardRaised(cardValue, isMoPai, cardIndex);
    }
    
    /// <summary>
    /// 恢复位置
    /// </summary>
    /// <param name="useAnimation">是否使用动画</param>
    public void RestorePosition(bool useAnimation = false)
    {
        if (!isRaised && rectTransform.anchoredPosition == originalPosition)
            return;
        
        isRaised = false;
        raiseTime = 0f; // 重置抬高时间
        mahjongSeat.mjGameUI.huPaiTips.Hide();

        if (useAnimation)
        {
            // 使用DOTween平滑恢复
            rectTransform.DOAnchorPos(originalPosition, 0.2f).SetEase(DG.Tweening.Ease.OutQuad);
        }
        else
        {
            rectTransform.anchoredPosition = originalPosition;
        }
    }
    
    /// <summary>
    /// 出牌
    /// </summary>
    private void OnPlayCard()
    {
        // 检查是否已经在出牌中（双重保险）
        if (isPlaying)
        {
            GF.LogWarning($"[MjCard_2D_Interaction] 已在出牌中，忽略重复出牌");
            return;
        }
        
        // ✅ 修复：在出牌前再次检查是否可以打牌（防止非自己回合时打牌）
        if (mahjongSeat?.SeatIndex == 0 && mahjongSeat?.mjGameUI != null)
        {
            // 检查当前是否轮到自己且允许打牌
            if (!mahjongSeat.mjGameUI.Can2DPlayCard())
            {
                GF.LogWarning($"[MjCard_2D_Interaction] 当前不允许打牌，忽略出牌请求");
                // 恢复牌的位置
                RestorePosition(true);
                return;
            }
        }
        
        // 检查是否在冷却时间内
        float timeSinceLastPlay = Time.time - lastPlayTime;
        if (lastPlayTime > 0f && timeSinceLastPlay < playCardCooldown)
        {
            GF.LogWarning($"[MjCard_2D_Interaction] 出牌冷却中({timeSinceLastPlay:F2}秒)，忽略出牌");
            return;
        }
        
        // 立即标记正在出牌，防止任何后续输入
        isPlaying = true;
        lastPlayTime = Time.time;
        
        // 立即禁用交互组件，完全阻止新的输入事件
        enabled = false;

        GF.LogInfo($"[MjCard_2D_Interaction] 出牌: 索引={cardIndex}, 牌值={cardValue}, 类型={(isMoPai ? "摸牌" : "手牌")}");
        
        // 调用出牌回调
        onPlayCard?.Invoke(cardIndex, cardValue, isMoPai);
    }
    
    /// <summary>
    /// 禁用交互
    /// </summary>
    public void DisableInteraction()
    {
        enabled = false;
        isPlaying = true; // 确保出牌状态被标记
        RestorePosition();
    }
    
    /// <summary>
    /// 启用交互
    /// </summary>
    public void EnableInteraction()
    {
        enabled = true;
        isPlaying = false; // 重置出牌状态
        lastPlayTime = 0f; // 重置冷却时间
    }

    /// <summary>
    /// 程序化抬起牌（用于智能选牌等自动化场景）
    /// </summary>
    public void RaiseCardProgrammatically()
    {
        if (isRaised)
            return;

        // 先将其他抬高的手牌恢复（如果在换牌模式下，不恢复其他牌）
        // 换牌模式需要同时选中多张牌
        // if (mahjongSeat != null)
        // {
        //     mahjongSeat.RestoreAllRaisedCards(this);
        // }

        isRaised = true;
        raiseTime = Time.time;
        Vector2 raisedPosition = originalPosition + new Vector2(0, raiseHeight);

        // 使用DOTween添加抬高动画
        rectTransform.DOAnchorPos(raisedPosition, 0.15f).SetEase(Ease.OutBack);

        GF.LogInfo($"[MjCard_2D_Interaction] 程序化抬高手牌: 索引={cardIndex}, 牌值={cardValue}");
    }
    
    /// <summary>
    /// 提升渲染层级到最前面
    /// </summary>
    private void BringToFront()
    {
        // 记录原始的Sibling Index
        if (originalSiblingIndex < 0)
        {
            originalSiblingIndex = transform.GetSiblingIndex();
        }
        
        // 将此卡牌移动到父节点的最后(渲染在最上层)
        transform.SetAsLastSibling();
        
        // 添加临时Canvas，确保在所有UI之上
        if (dragCanvas == null)
        {
            dragCanvas = gameObject.AddComponent<Canvas>();
            dragCanvas.overrideSorting = true;
            dragCanvas.sortingOrder = 9999; // 设置极高的排序层级
            
            // 添加GraphicRaycaster以保持交互
            dragRaycaster = gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
    }
    
    /// <summary>
    /// 恢复渲染层级
    /// </summary>
    private void RestoreRenderOrder()
    {
        // 先移除临时GraphicRaycaster组件（因为它依赖Canvas）
        if (dragRaycaster != null)
        {
            Destroy(dragRaycaster);
            dragRaycaster = null;
        }
        
        // 再移除临时Canvas组件
        if (dragCanvas != null)
        {
            Destroy(dragCanvas);
            dragCanvas = null;
        }
        
        // 恢复原始的Sibling Index
        if (originalSiblingIndex >= 0)
        {
            transform.SetSiblingIndex(originalSiblingIndex);
            originalSiblingIndex = -1;
        }
    }
    
    /// <summary>
    /// 组件销毁时，停止并清理所有与此 RectTransform 关联的 DOTween 动画
    /// </summary>
    private void OnDestroy()
    {
        // 杀死所有与此 RectTransform 关联的 DOTween 动画
        if (rectTransform != null)
        {
            rectTransform.DOKill();
        }
        
        // 先清理GraphicRaycaster（因为它依赖Canvas）
        if (dragRaycaster != null)
        {
            Destroy(dragRaycaster);
            dragRaycaster = null;
        }
        
        // 再清理临时Canvas组件
        if (dragCanvas != null)
        {
            Destroy(dragCanvas);
            dragCanvas = null;
        }
    }
    
    #endregion

    /// <summary>
    /// 获取牌值
    /// </summary>
    public int GetCardValue()
    {
        return cardValue;
    }

    /// <summary>
    /// 是否已抬高
    /// </summary>
    public bool IsRaised => isRaised;
}

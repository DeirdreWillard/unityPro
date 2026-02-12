using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// 事件触发监听器，继承自Unity的EventTrigger
/// </summary>
public class EventTriggerListener : EventTrigger
{
    /// <summary>
    /// 无参回调委托，接收游戏对象参数
    /// </summary>
    /// <param name="go">触发事件的游戏对象</param>
    public delegate void VoidDelegate(GameObject go);
    
    /// <summary>
    /// 点击事件回调
    /// </summary>
    public VoidDelegate onClick;
    
    /// <summary>
    /// 按下事件回调
    /// </summary>
    public VoidDelegate onDown;
    
    /// <summary>
    /// 进入事件回调
    /// </summary>
    public VoidDelegate onEnter;
    
    /// <summary>
    /// 退出事件回调
    /// </summary>
    public VoidDelegate onExit;
    
    /// <summary>
    /// 抬起事件回调
    /// </summary>
    public VoidDelegate onUp;
    
    /// <summary>
    /// 选中事件回调
    /// </summary>
    public VoidDelegate onSelect;
    
    /// <summary>
    /// 更新选中事件回调
    /// </summary>
    public VoidDelegate onUpdateSelect;

    /// <summary>
    /// 获取或创建EventTriggerListener组件
    /// </summary>
    /// <param name="go">目标游戏对象</param>
    /// <returns>EventTriggerListener组件</returns>
    static public EventTriggerListener Get(GameObject go)
    {
        EventTriggerListener listener = go.GetComponent<EventTriggerListener>();

        if (listener == null)
            listener = go.AddComponent<EventTriggerListener>();

        return listener;
    }
    
    /// <summary>
    /// 指针点击事件处理
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    public override void OnPointerClick(PointerEventData eventData)
    {
        if (onClick != null)
            onClick(gameObject);
    }
    
    /// <summary>
    /// 指针按下事件处理
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    public override void OnPointerDown(PointerEventData eventData)
    {
        if (onDown != null)
            onDown(gameObject);
    }
    
    /// <summary>
    /// 指针进入事件处理
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (onEnter != null)
            onEnter(gameObject);
    }
    
    /// <summary>
    /// 指针退出事件处理
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    public override void OnPointerExit(PointerEventData eventData)
    {
        if (onExit != null)
            onExit(gameObject);
    }
    
    /// <summary>
    /// 指针抬起事件处理
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    public override void OnPointerUp(PointerEventData eventData)
    {
        if (onUp != null)
            onUp(gameObject);
    }
    
    /// <summary>
    /// 选中事件处理
    /// </summary>
    /// <param name="eventData">基础事件数据</param>
    public override void OnSelect(BaseEventData eventData)
    {
        if (onSelect != null)
            onSelect(gameObject);
    }
    
    /// <summary>
    /// 更新选中事件处理
    /// </summary>
    /// <param name="eventData">基础事件数据</param>
    public override void OnUpdateSelected(BaseEventData eventData)
    {
        if (onUpdateSelect != null)
            onUpdateSelect(gameObject);
    }
}
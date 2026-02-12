﻿using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UtilityBuiltin;

public class ButtonCheck : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler
{
	private bool isButtonPressed;
	public UnityEvent onPress;
	public UnityEvent onRelease;
	public UnityEvent onCancel;
	public UnityEvent onDragInVisual;    // 拖拽回来时的视觉变化
	public UnityEvent onDragOutVisual;   // 拖拽出去时的视觉变化

	public void OnPointerDown(PointerEventData eventData)
	{
		if(Util.IsClickLocked(0.5f)) return;
		isButtonPressed = true;
		onPress?.Invoke();
		onDragInVisual?.Invoke();  // 按下时的视觉变化
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		isButtonPressed = false;
		onDragOutVisual?.Invoke();  // 抬起时的视觉变化
		if (eventData.pointerCurrentRaycast.gameObject == gameObject) {
			onRelease?.Invoke();
		}
		else {
			onCancel?.Invoke();
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (isButtonPressed == false) {
			return;
		}
		onDragOutVisual?.Invoke();  // 拖拽出去时的视觉变化
	}

	public void OnPointerEnter(PointerEventData eventData)
    {
		if (isButtonPressed == false) {
			return;
		}
		onDragInVisual?.Invoke();   // 拖拽回来时的视觉变化
	}

	void OnApplicationFocus(bool hasFocus) {
		if (hasFocus == false && isButtonPressed == true) {
			isButtonPressed = false;
			onCancel?.Invoke();
		}
	}

}

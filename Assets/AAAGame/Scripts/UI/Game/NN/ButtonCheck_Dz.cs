﻿using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ButtonCheck_Dz : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
	private bool isButtonPressed;
	public UnityEvent onPress;
	public UnityEvent onRelease;
	public UnityEvent<Vector3> onHold;
	public UnityEvent onCancel;

	public GameObject goHold;

	public float duration = 0f;

	public void OnPointerDown(PointerEventData eventData)
	{
		isButtonPressed = true;
		duration = 0f;
		onPress?.Invoke();
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		isButtonPressed = false;
		if (eventData.pointerCurrentRaycast.gameObject == goHold) {
			onRelease?.Invoke();
		}
		else {
			onCancel?.Invoke();
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		// isButtonPressed = false;
		// onCancel?.Invoke();
	}

	void Update()
	{
		if (isButtonPressed) {
			duration += Time.deltaTime;
			onHold?.Invoke(Input.mousePosition);
		}
	}

	void OnApplicationFocus(bool hasFocus) {
		if (hasFocus == false && isButtonPressed == true) {
			isButtonPressed = false;
			onCancel?.Invoke();
		}
	}
}

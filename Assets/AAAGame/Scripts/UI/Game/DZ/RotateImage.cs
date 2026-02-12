using UnityEngine;

public class RotateImage : MonoBehaviour
{
	public float rotationSpeed = 120f;

	private RectTransform rectTransform;

	void Start()
	{
		rectTransform = GetComponent<RectTransform>();
	}

	void Update()
	{
		rectTransform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
	}
}

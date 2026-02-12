using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FloatingTips : MonoBehaviour
{
	public GameObject floating;
	public Text tip;
	float posy = 0;

	void Awake()
	{
		posy = Random.Range(-200f, 200f);
	}

	public void ShowTips(string str, float duration)
	{
		gameObject.SetActive(true);
		Vector2 size = tip.GetComponent<RectTransform>().sizeDelta;
		tip.text = str;
		tip.gameObject.SetActive(true);
		tip.transform.localPosition = new Vector3(Screen.width / 2 + size.x / 2, posy, tip.transform.localPosition.z);
		tip.transform.DOLocalMoveX(-Screen.width / 2 - size.x / 2, duration).OnComplete(() => {
			Destroy(gameObject);
		});
	}
}

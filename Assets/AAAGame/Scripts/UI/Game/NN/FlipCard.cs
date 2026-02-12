
using NetMsg;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UtilityBuiltin;

enum Direction {
	None,
	Top,
	Bottom,
	Left,
	Right,
}

public class FlipCard : MonoBehaviour
{
	public RectMask2D MaskRealCard;
	public RectMask2D MaskBackCard;
	public Image ImageRealCard;
	public Image ImageBackCard;

	Direction direction = Direction.None;
	Direction autodirection = Direction.None;
	Vector3 origin = Vector3.zero;

	Vector2 size;
	Vector3 pos;

	NNProcedure nnProcedure;

	bool opencard = false;

	void Awake()
	{
		nnProcedure = GF.Procedure.CurrentProcedure as NNProcedure;
		size = GetComponent<RectTransform>().sizeDelta;
		pos = transform.localPosition;
	}

	void Update()
	{
	}

	void OnEnable()
	{
		HotfixNetworkComponent.AddListener(MessageID.Msg_LookRs, Function_LookRs);
		ResetCard();
	}

	void OnDisable()
	{
		HotfixNetworkComponent.RemoveListener(MessageID.Msg_LookRs, Function_LookRs);
	}

	public void Init(int num)
	{
		opencard = false;
		ResetCard();
		MaskRealCard.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
		ImageRealCard.gameObject.SetActive(true);
		ImageBackCard.gameObject.SetActive(true);
		string CardPath;
		if (num != -1) {
			int color = GameUtil.GetInstance().GetColor(num);
			int value = GameUtil.GetInstance().GetValue(num);
			CardPath = "Common/Cards/" + color + value + ".png";
		}
		else {
			CardPath = "Common/Cards/-1.png";
		}
		ImageRealCard.SetSprite(CardPath);
	}

	public void OnPointDown(BaseEventData eventData) {
		origin = transform.InverseTransformPoint(GF.UICamera.ScreenToWorldPoint(Input.mousePosition));
		RectTransform rectTransform = transform as RectTransform;
		float distanceToTop = Mathf.Abs(origin.y - rectTransform.sizeDelta.y / 2);
		float distanceToBottom = Mathf.Abs(origin.y + rectTransform.sizeDelta.y / 2);
		float distanceToLeft = Mathf.Abs(origin.x + rectTransform.sizeDelta.x / 2);
		float distanceToRight = Mathf.Abs(origin.x - rectTransform.sizeDelta.x / 2);

		if (distanceToTop <= distanceToBottom && distanceToTop <= distanceToLeft && distanceToTop <= distanceToRight)
		{
			direction = Direction.Top;
		}
		else if (distanceToBottom <= distanceToTop && distanceToBottom <= distanceToLeft && distanceToBottom <= distanceToRight)
		{
			direction = Direction.Bottom;
		}
		else if (distanceToLeft <= distanceToTop && distanceToLeft <= distanceToBottom && distanceToLeft <= distanceToRight)
		{
			direction = Direction.Left;
		}
		else if (distanceToRight <= distanceToTop && distanceToRight <= distanceToBottom && distanceToRight <= distanceToLeft)
		{
			direction = Direction.Right;
		}
		autodirection = direction;
	}

	public void OnPointUp(BaseEventData eventData) {
		// UpdateCard(0);
		// direction = Direction.None;
		// origin = Vector3.zero;
	}

	public void onDrag(BaseEventData eventData) {
		if (direction == Direction.None) {
			return;
		}
		if (opencard == true) {
			return;
		}
		Vector3 newpos = transform.InverseTransformPoint(GF.UICamera.ScreenToWorldPoint(Input.mousePosition));
		switch (direction) {
			case Direction.Top:
				UpdateCard1(Mathf.Max(0, origin.y - newpos.y) / ImageBackCard.rectTransform.localScale.y);
				break;
			case Direction.Bottom:
				UpdateCard1(Mathf.Max(0, newpos.y - origin.y) / ImageBackCard.rectTransform.localScale.y);
				break;
			case Direction.Left:
				UpdateCard1(Mathf.Max(0, newpos.x - origin.x) / ImageBackCard.rectTransform.localScale.x);
				break;
			case Direction.Right:
				UpdateCard1(Mathf.Max(0, origin.x - newpos.x) / ImageBackCard.rectTransform.localScale.x);
				break;
		}
	}

	public float maxdistance;

	public void UpdateCard1(float distance) {
		if (opencard) {
			return;
		}
		switch (direction) {
			case Direction.Top:
				if (distance > 3 * size.y / 4) { OpenCard(); return; }
				MaskBackCard.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x, size.y - distance / 2);
				MaskBackCard.transform.localPosition = new Vector3(pos.x, -distance / 4, pos.z);
				ImageBackCard.transform.localPosition = new Vector3(pos.x, distance / 4, pos.z);
				MaskRealCard.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x, distance);
				MaskRealCard.transform.localPosition = new Vector3(pos.x, (size.y - 3 * distance / 2) / 2, pos.z);
				ImageRealCard.transform.localPosition = new Vector3(pos.x, (size.y - distance / 2) / 2, pos.z);
				break;
			case Direction.Bottom:
				if (distance > 3 * size.y / 4) { OpenCard(); return; }
				MaskBackCard.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x, size.y - distance / 2);
				MaskBackCard.transform.localPosition = new Vector3(pos.x, distance / 4, pos.z);
				ImageBackCard.transform.localPosition = new Vector3(pos.x, -distance / 4, pos.z);
				MaskRealCard.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x, distance);
				MaskRealCard.transform.localPosition = new Vector3(pos.x, (3 * distance / 2 - size.y) / 2, pos.z);
				ImageRealCard.transform.localPosition = new Vector3(pos.x, (distance / 2 - size.y) / 2, pos.z);
				break;
			case Direction.Left:
				if (distance > 3 * size.x / 4) { OpenCard(); return; }
				MaskBackCard.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x - distance / 2, size.y);
				MaskBackCard.transform.localPosition = new Vector3(distance / 4, pos.y, pos.z);
				ImageBackCard.transform.localPosition = new Vector3(-distance / 4, pos.y, pos.z);
				MaskRealCard.GetComponent<RectTransform>().sizeDelta = new Vector2(distance / 2, size.y);
				MaskRealCard.transform.localPosition = new Vector3((3 * distance / 2 - size.x) / 2, pos.y, pos.z);
				ImageRealCard.transform.localPosition = new Vector3((distance / 2 - size.x) / 2 , pos.y, pos.z);
				break;
			case Direction.Right:
				if (distance > 3 * size.x / 4) { OpenCard(); return; }
				MaskBackCard.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x - distance / 2, size.y);
				MaskBackCard.transform.localPosition = new Vector3(-distance / 4, pos.y, pos.z);
				ImageBackCard.transform.localPosition = new Vector3(distance / 4, pos.y, pos.z);
				MaskRealCard.GetComponent<RectTransform>().sizeDelta = new Vector2(distance / 2, size.y);
				MaskRealCard.transform.localPosition = new Vector3((size.x - 3 * distance / 2) / 2, pos.y, pos.z);
				ImageRealCard.transform.localPosition = new Vector3((size.x - distance / 2) / 2, pos.y, pos.z);
				break;
		}
		autodirection = direction;
		maxdistance = distance;
	}

	public void UpdateCard2(int step = -1) {
		float distance;
		if (autodirection == Direction.Left || autodirection == Direction.Right) {
		}
		if (autodirection == Direction.Top || autodirection == Direction.Bottom) {
		}
		switch (autodirection) {
			case Direction.Top:
				distance = (110 - maxdistance) / 10 * step + maxdistance;
				MaskBackCard.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x, size.y - distance / 2);
				MaskBackCard.transform.localPosition = new Vector3(pos.x, -distance / 2, pos.z);
				ImageBackCard.transform.localPosition = new Vector3(pos.x, distance, pos.z);
				MaskRealCard.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x, distance);
				MaskRealCard.transform.localPosition = new Vector3(pos.x, (size.y - distance) / 2, pos.z);
				ImageRealCard.transform.localPosition = new Vector3(pos.x, (size.y - distance) / 2, pos.z);
				break;
			case Direction.Bottom:
				distance = (110 - maxdistance) / 10 * step + maxdistance;
				MaskBackCard.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x, size.y - distance / 2);
				MaskBackCard.transform.localPosition = new Vector3(pos.x, distance / 2, pos.z);
				ImageBackCard.transform.localPosition = new Vector3(pos.x, -distance, pos.z);
				MaskRealCard.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x, distance);
				MaskRealCard.transform.localPosition = new Vector3(pos.x, (distance - size.y) / 2, pos.z);
				ImageRealCard.transform.localPosition = new Vector3(pos.x, (distance - size.y) / 2, pos.z);
				break;
			case Direction.Left:
				distance = (78 - maxdistance) / 10 * step + maxdistance;
				MaskBackCard.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x - distance / 2, size.y);
				MaskBackCard.transform.localPosition = new Vector3(distance / 2, pos.y, pos.z);
				ImageBackCard.transform.localPosition = new Vector3(-distance, pos.y, pos.z);
				MaskRealCard.GetComponent<RectTransform>().sizeDelta = new Vector2(distance, size.y);
				MaskRealCard.transform.localPosition = new Vector3((distance - size.x) / 2, pos.y, pos.z);
				ImageRealCard.transform.localPosition = new Vector3((distance - size.x) / 2 , pos.y, pos.z);
				break;
			case Direction.Right:
				distance = (78 - maxdistance) / 10 * step + maxdistance;
				MaskBackCard.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x - distance / 2, size.y);
				MaskBackCard.transform.localPosition = new Vector3(-distance / 2, pos.y, pos.z);
				ImageBackCard.transform.localPosition = new Vector3(distance, pos.y, pos.z);
				MaskRealCard.GetComponent<RectTransform>().sizeDelta = new Vector2(distance, size.y);
				MaskRealCard.transform.localPosition = new Vector3((size.x - distance) / 2, pos.y, pos.z);
				ImageRealCard.transform.localPosition = new Vector3((size.x - distance) / 2, pos.y, pos.z);
				break;
		}
	}

	void OpenCard() {
		if (opencard) {
			return;
		}
		ImageRealCard.gameObject.SetActive(true);
		ImageBackCard.gameObject.SetActive(false);
		nnProcedure.Send_ShowCard();
		opencard = true;
	}

	void ResetCard() {
		UpdateCard1(0);
		ImageRealCard.gameObject.SetActive(false);
		ImageBackCard.gameObject.SetActive(true);
		MaskBackCard.GetComponent<RectTransform>().sizeDelta = size;
		MaskBackCard.transform.localPosition = pos;

		ImageRealCard.transform.localRotation = Quaternion.Euler(0, 0, 0);
		ImageRealCard.transform.localPosition = pos;
		ImageRealCard.GetComponent<RectTransform>().sizeDelta = size;
	}

	public void Function_LookRs(MessageRecvData data)
	{
		Msg_LookRs ack = Msg_LookRs.Parser.ParseFrom(data.Data);
		Init(ack.Card);
	}
}

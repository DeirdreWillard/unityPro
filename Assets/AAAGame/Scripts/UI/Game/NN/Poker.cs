using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;
public enum FlipMode
{
    RightToLeft,
    LeftToRight,
	TopToBottom,
	BottomToTop
}

public class Poker : MonoBehaviour {
    public Canvas canvas;
    [SerializeField]
	public RectTransform PokerPanel;
    public Sprite background;
	public Sprite pokerBack;
	public Sprite pokerFront;
    public bool interactable=true;
    public bool enableShadowEffect=true;
    //represent the index of the sprite shown in the right page

    public Vector3 EndBottomLeft
    {
        get { return ebl; }
    }
    public Vector3 EndBottomRight
    {
        get { return ebr; }
    }
    public float Height
    {
        get
        {
            return PokerPanel.rect.height ; 
        }
    }
    public Image ClippingPlane;
    public Image NextPageClip;
    public Image Shadow;
    //public Image ShadowLTR;
    public Image Left;
    public Image Right;
    public Image RightNext;
    public UnityEvent OnFlip;

    //Spine Bottom
    Vector3 sb;
    //Spine Top
    Vector3 st;
    //corner of the page
    Vector3 c;
    //Edge Bottom Right
    Vector3 ebr;
    //Edge Bottom Left
    Vector3 ebl;
    //follow point 
    Vector3 f;
	//Edge middle Right
	Vector3 mr;
	//middle
	Vector3 mm;
	//Edge middle Left
	Vector3 ml;
	//Edge Top middle
	Vector3 tm;
	//Edge Bottom middle
	Vector3 bm;

    bool pageDragging = false;
    //current flip mode
    FlipMode mode;

    void Start()
    {
        float scaleFactor = 1;
        if (canvas) scaleFactor = canvas.scaleFactor;
		float pageWidth = PokerPanel.rect.width* scaleFactor / 2;
		float pageHeight = PokerPanel.rect.height* scaleFactor;
        Left.gameObject.SetActive(false);
        Right.gameObject.SetActive(false);
		RightNext.sprite= pokerBack;

        Vector3 globalsb = PokerPanel.transform.position + new Vector3(0, -pageHeight / 2);
        sb = transformPoint(globalsb);
		Vector3 globalebr = PokerPanel.transform.position + new Vector3(pageWidth, -pageHeight / 2);
        ebr = transformPoint(globalebr);
        Vector3 globalebl = PokerPanel.transform.position + new Vector3(-pageWidth, -pageHeight / 2);
        ebl = transformPoint(globalebl);
        Vector3 globalst = PokerPanel.transform.position + new Vector3(0, pageHeight / 2);
        st = transformPoint(globalst);
		Vector3 globalmr = PokerPanel.transform.position + new Vector3(pageWidth, 0);
		mr = transformPoint(globalmr);
		Vector3 globalmm = PokerPanel.transform.position + new Vector3(0, 0);
		mm = transformPoint(globalmm);
		Vector3 globalml = PokerPanel.transform.position + new Vector3(-pageWidth, 0);
		ml = transformPoint(globalml);
		Vector3 globaltm = PokerPanel.transform.position + new Vector3(0, pageHeight / 2);
		tm = transformPoint(globaltm);
		Vector3 globalbm = PokerPanel.transform.position + new Vector3(0, -pageHeight / 2);
		bm = transformPoint(globalbm);

        float scaledPageWidth = pageWidth / scaleFactor;
        float scaledPageHeight = pageHeight / scaleFactor;

		ClippingPlane.rectTransform.sizeDelta = new Vector2(scaledPageWidth*2,scaledPageHeight);
		Shadow.rectTransform.sizeDelta = new Vector2(scaledPageWidth*2,scaledPageWidth*2);
		NextPageClip.rectTransform.sizeDelta = new Vector2(scaledPageWidth*2,scaledPageHeight);

		Left.rectTransform.pivot = new Vector2(0.5f,0.5f);
		ClippingPlane.rectTransform.pivot = new Vector2(0.5f,0.5f);
		NextPageClip.rectTransform.pivot = new Vector2(0.5f,0.5f);
		RightNext.rectTransform.pivot = new Vector2(0.5f,0.5f);
		Right.rectTransform.pivot = new Vector2(0.5f,0.5f);
		Shadow.rectTransform.pivot = new Vector2(0.5f,0.5f);

		ClippingPlane.transform.position = PokerPanel.transform.position;
		NextPageClip.transform.position = PokerPanel.transform.position;
		RightNext.transform.position = PokerPanel.transform.position;
		Right.transform.position = PokerPanel.transform.position;
		Shadow.transform.position = PokerPanel.transform.position;
    }
    public Vector3 transformPoint(Vector3 global)
    {
        Vector3 localPos = PokerPanel.InverseTransformPoint(global);
		// GF.LogInfo("transformPoint:" + global + " localPos:" + localPos);
        return localPos;
    }
    void Update()
    {
        if (pageDragging&&interactable)
        {
            UpdatePoker();
        }
    }
	public void UpdatePoker()
    {
		Vector3 f1 = f;
		Vector3 f2 = transformPoint( Input.mousePosition);
        f = Vector3.Lerp(f, f2, Time.deltaTime * 10);
		if (mode == FlipMode.RightToLeft)
			UpdateBookRTLToPoint (f);
		else if (mode == FlipMode.LeftToRight)
			UpdateBookLTRToPoint (f);
		else if (mode == FlipMode.TopToBottom)
			UpdateBookTTBToPoint (f);
		else {
			UpdateBookBTTToPoint (f);
		}
    }

	public void UpdateBookBTTToPoint(Vector3 followLocation)
	{
		mode = FlipMode.BottomToTop;
		f = followLocation;
		Shadow.transform.SetParent(ClippingPlane.transform, true);
		Shadow.transform.localPosition = new Vector3(0, 0, 0);
		Shadow.transform.localEulerAngles = new Vector3(0, 0, -90);
		float moveTopLineY = mm.y + (mr.x - tm.y);
		Shadow.transform.localPosition = new Vector3(0, moveTopLineY, 0);

		Right.transform.SetParent(ClippingPlane.transform, true);
		Left.transform.SetParent(PokerPanel.transform, true);
		RightNext.transform.SetParent(PokerPanel.transform, true);
		c = f;
		//判断点是否超出屏幕
		if (c.y < bm.y) { //下边屏幕
			c.y = bm.y;
		} else {
			//上边屏幕
			if (c.y > tm.y*3.0f) {
				c.y = tm.y*3.0f;
			}
		}

		float ClippingPlaneY = mm.y + Mathf.Abs(bm.y - c.y)/2.0f;
		ClippingPlane.transform.position = PokerPanel.TransformPoint(new Vector2(mm.x,ClippingPlaneY));

		float RightY = bm.y*2.0f - (bm.y - c.y);
		Right.transform.position = PokerPanel.TransformPoint(new Vector2(mm.x,RightY));

		float nextPageClipY = bm.y*2.0f - (bm.y - c.y)/2.0f;
		NextPageClip.transform.position = PokerPanel.TransformPoint(new Vector2(mm.x,nextPageClipY));

		RightNext.transform.SetParent(NextPageClip.transform, true);
		Left.transform.SetParent(ClippingPlane.transform, true);
		Left.transform.SetAsFirstSibling();
		Shadow.rectTransform.SetParent(Right.rectTransform, true);
	}

	public void UpdateBookTTBToPoint(Vector3 followLocation)
	{
		mode = FlipMode.TopToBottom;
		f = followLocation;
		Shadow.transform.SetParent(ClippingPlane.transform, true);
		Shadow.transform.localEulerAngles = new Vector3(0, 0, 90);
		float moveTopLineY = mm.y - (mr.x - tm.y);
		Shadow.transform.localPosition = new Vector3(0, moveTopLineY, 0);

		Right.transform.SetParent(ClippingPlane.transform, true);
		Left.transform.SetParent(PokerPanel.transform, true);
		RightNext.transform.SetParent(PokerPanel.transform, true);
		c = f;
		//判断点是否超出屏幕
		if (c.y > tm.y) { //上边屏幕
			c.y = tm.y;
		} else {
			//下边屏幕
			if (c.y < 0 && Mathf.Abs (c.y) > (3.0f * tm.y))
				c.y = -(3.0f * tm.y);
		}

		float ClippingPlaneY = mm.y - (tm.y - c.y)/2.0f;
		ClippingPlane.transform.position = PokerPanel.TransformPoint(new Vector2(tm.x,ClippingPlaneY));

		float RightY = tm.y*2.0f - (tm.y - c.y);
		Right.transform.position = PokerPanel.TransformPoint(new Vector2(tm.x,RightY));

		float nextPageClipY = tm.y*2.0f - (tm.y - c.y)/2.0f;
		NextPageClip.transform.position = PokerPanel.TransformPoint(new Vector2(tm.x,nextPageClipY));

		RightNext.transform.SetParent(NextPageClip.transform, true);
		Left.transform.SetParent(ClippingPlane.transform, true);
		Left.transform.SetAsFirstSibling();
		Shadow.rectTransform.SetParent(Right.rectTransform, true);
	}

    public void UpdateBookLTRToPoint(Vector3 followLocation)
    {
        mode = FlipMode.LeftToRight;
		f = followLocation;
		Shadow.transform.SetParent(ClippingPlane.transform, true);
		Shadow.transform.localPosition = new Vector3(0, 0, 0);
		Shadow.transform.localEulerAngles = new Vector3(0, 0, 180);

		Right.transform.SetParent(ClippingPlane.transform, true);
		Left.transform.SetParent(PokerPanel.transform, true);
		RightNext.transform.SetParent(PokerPanel.transform, true);
		c = f;
		//判断点是否超出屏幕
		if (c.x < ml.x) { //左边屏幕
			c.x = ml.x;
		} else {
			//右边屏幕
			if (c.x > mr.x*3.0f) {
				c.x = mr.x*3.0f;
			}
		}

		float ClippingPlaneX = mm.x + Mathf.Abs(ml.x - c.x)/2.0f;
		ClippingPlane.transform.position = PokerPanel.TransformPoint(new Vector2(ClippingPlaneX,ml.y));

		float RightX = ml.x*2.0f - (ml.x - c.x);
		Right.transform.position = PokerPanel.TransformPoint(new Vector2(RightX,ml.y));

		float nextPageClipX = ml.x*2.0f - (ml.x - c.x)/2.0f;
		NextPageClip.transform.position = PokerPanel.TransformPoint(new Vector2(nextPageClipX,ml.y));

		RightNext.transform.SetParent(NextPageClip.transform, true);
		Left.transform.SetParent(ClippingPlane.transform, true);
		Left.transform.SetAsFirstSibling();
		Shadow.rectTransform.SetParent(Right.rectTransform, true);
    }

	public void DragPageToPoint(Vector3 point)
	{
		pageDragging = true;
		f = point;

		Left.gameObject.SetActive(true);
		Left.transform.position = RightNext.transform.position;

		Left.sprite = pokerBack;
		Left.transform.SetAsFirstSibling();

		Right.gameObject.SetActive(true);
		Right.transform.position = RightNext.transform.position;
		Right.sprite = pokerFront;
		RightNext.sprite = background;
		if (enableShadowEffect) Shadow.gameObject.SetActive(true);
	}

    public void UpdateBookRTLToPoint(Vector3 followLocation)
    {
		mode = FlipMode.RightToLeft;
		f = followLocation;
		Shadow.transform.SetParent(ClippingPlane.transform, true);
		Shadow.transform.localPosition = new Vector3(0, 0, 0);
		Shadow.transform.localEulerAngles = new Vector3(0, 0, 0);
		Right.transform.SetParent(ClippingPlane.transform, true);
		Left.transform.SetParent(PokerPanel.transform, true);
		RightNext.transform.SetParent(PokerPanel.transform, true);
		c = f;
		//判断点是否超出屏幕
		if (c.x > 0 && c.x > mr.x) { //右边屏幕
			c.x = mr.x;
		} else {
			//左边界
			if (c.x < 0 && Mathf.Abs (c.x) > (3.0f * mr.x))
				c.x = -(3.0f * mr.x);
		}

		float ClippingPlaneX = mm.x - (mr.x - c.x)/2.0f;
		ClippingPlane.transform.position = PokerPanel.TransformPoint(new Vector2(ClippingPlaneX,mr.y));

		float RightX = mr.x*2.0f - (mr.x - c.x);
		Right.transform.position = PokerPanel.TransformPoint(new Vector2(RightX,mr.y));

		float nextPageClipX = mr.x*2.0f - (mr.x - c.x)/2.0f;
		NextPageClip.transform.position = PokerPanel.TransformPoint(new Vector2(nextPageClipX,mr.y));

		RightNext.transform.SetParent(NextPageClip.transform, true);
		Left.transform.SetParent(ClippingPlane.transform, true);
		Left.transform.SetAsFirstSibling();
		Shadow.rectTransform.SetParent(Right.rectTransform, true);
    }

	//点击右边的热点
    public void OnMouseDragRightPage()
    {
		if (interactable) {
			mode = FlipMode.RightToLeft;
			DragPageToPoint(transformPoint(Input.mousePosition));
		}
    }
	//点击左边的热点
	public void OnMouseDragLeftPage()
	{
		if (interactable) {
			mode = FlipMode.LeftToRight;
			DragPageToPoint(transformPoint(Input.mousePosition));
		}
	}
	//点击上方的热点
	public void OnMouseDragTopPage()
	{
		if (interactable) {
			mode = FlipMode.TopToBottom;
			DragPageToPoint(transformPoint(Input.mousePosition));
		}
	}
	//点击下方的热点
	public void OnMouseDragBottomPage()
	{
		if (interactable) {
			mode = FlipMode.BottomToTop;
			DragPageToPoint(transformPoint(Input.mousePosition));
		}
	}

    public void OnMouseRelease()
    {
        if (interactable) {
            ReleasePage();
	    }
	}

    public void ReleasePage()
    {
        if (pageDragging)
        {
            pageDragging = false;
            float distanceToLeft = Vector2.Distance(c, ebl);
            float distanceToRight = Vector2.Distance(c, ebr);
			float distanceToTop = Vector2.Distance (c, tm);
			float distanceToBottom = Vector2.Distance (c, bm);

			if (distanceToRight < distanceToLeft && mode == FlipMode.RightToLeft)
				TweenBack (ebr);
			else if (distanceToRight > distanceToLeft && mode == FlipMode.LeftToRight)
				TweenBack (ebl);
			else if (distanceToTop < distanceToBottom && mode == FlipMode.TopToBottom)
				TweenBack (tm);
			else if (distanceToTop > distanceToBottom && mode == FlipMode.BottomToTop)
				TweenBack (bm);
            else
                TweenForward();
        }
    }

    Coroutine currentCoroutine;
    public void TweenForward()
    {
		if (mode == FlipMode.RightToLeft) {
			currentCoroutine = StartCoroutine(TweenTo(ebl, 0.15f, () => { Flip(); }));
		} else if (mode == FlipMode.LeftToRight) {
			currentCoroutine = StartCoroutine(TweenTo(ebr, 0.15f, () => { Flip(); }));
		} else if (mode == FlipMode.TopToBottom) {
			currentCoroutine = StartCoroutine(TweenTo(bm, 0.15f, () => { Flip(); }));
		} else {
			currentCoroutine = StartCoroutine(TweenTo(tm, 0.15f, () => { Flip(); }));
		}
    }

    void Flip()
    {
        Left.transform.SetParent(PokerPanel.transform, true);
        Left.gameObject.SetActive(false);
        Right.gameObject.SetActive(false);
        Right.transform.SetParent(PokerPanel.transform, true);
        RightNext.transform.SetParent(PokerPanel.transform, true);
		RightNext.sprite= pokerFront;
        Shadow.gameObject.SetActive(false);
        if (OnFlip != null)
            OnFlip.Invoke();
    }

	public void TweenBack(Vector3 toLocation)
    {
		currentCoroutine = StartCoroutine(TweenTo(toLocation,0.15f,
			() =>
			{
				RightNext.sprite= pokerBack;
				RightNext.transform.SetParent(PokerPanel.transform);
				Right.transform.SetParent(PokerPanel.transform);

				Left.gameObject.SetActive(false);
				Right.gameObject.SetActive(false);
				pageDragging = false;
			}
		));
    }

    public IEnumerator TweenTo(Vector3 to, float duration, System.Action onFinish)
    {
        int steps = (int)(duration / 0.025f);
        Vector3 displacement = (to - f) / steps;
        for (int i = 0; i < steps-1; i++)
        {
			if (mode == FlipMode.RightToLeft) {
				UpdateBookRTLToPoint (f + displacement);
			} else if (mode == FlipMode.LeftToRight) {
				UpdateBookLTRToPoint (f + displacement);
			} else if (mode == FlipMode.TopToBottom) {
				UpdateBookTTBToPoint (f + displacement);
			} else {
				UpdateBookBTTToPoint (f + displacement);
			}
            yield return new WaitForSeconds(0.025f);
        }
        if (onFinish != null)
            onFinish();
    }
}

/// Credit Mrs. YakaYocha 
/// 来源 - https://www.youtube.com/channel/UCHp8LZ_0-iCvl-5pjHATsgw
/// 请捐赠: https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=RJ8D9FRFQF9VS

using UnityEngine.Events;

namespace UnityEngine.UI.Extensions
{
    [RequireComponent(typeof(ScrollRect))]
    [AddComponentMenu("布局/扩展/垂直滚动组件")]
    public class UIVerticalScroller : MonoBehaviour
    {
        private float[] distReposition; // 用于重新定位的距离数组
        private float[] distance; // 元素与中心的绝对距离

        [SerializeField]
        [Tooltip("目标 ScrollRect 组件")]
        private ScrollRect scrollRect;

        [SerializeField]
        [Tooltip("需要填充到滚动区域的元素")]
        private GameObject[] arrayOfElements;

        [SerializeField]
        [Tooltip("中心显示区域（用于放大显示内容的位置）")]
        private RectTransform center;

        [SerializeField]
        [Tooltip("元素的尺寸/间距")]
        private RectTransform elementSize;

        [SerializeField]
        [Tooltip("缩放比例 = 1 / (1 + 与中心距离 * 收缩比例)")]
        private Vector2 elementShrinkage = new Vector2(1f / 2, 1f / 2);

        [SerializeField]
        [Tooltip("最小缩放比例（距离中心最远时）")]
        private Vector2 minScale = new Vector2(0.3f, 0.3f);

        [SerializeField]
        [Tooltip("在开始时选中的元素索引")]
        private int startingIndex = -1;

        [SerializeField]
        [Tooltip("停止惯性滚动超出最后一个元素")]
        private bool stopMomentumOnEnd = true;

        [SerializeField]
        [Tooltip("设置中心以外的元素不可交互")]
        private bool disableUnfocused = true;

        [SerializeField]
        [Tooltip("跳转到下一页的按钮（可选）")]
        private GameObject scrollUpButton;

        [SerializeField]
        [Tooltip("跳转到上一页的按钮（可选）")]
        private GameObject scrollDownButton;

        [SerializeField]
        [Tooltip("当点击某个特定项目时触发的事件，传递项目索引（可选）")]
        private UnityEvent<int> onButtonClicked;

        [SerializeField]
        [Tooltip("当聚焦的元素改变时触发的事件（可选）")]
        private UnityEvent<int> onFocusChanged;

        public int FocusedElementIndex { get; private set; } // 当前聚焦的元素索引

        public RectTransform Center { get => center; set => center = value; } // 中心显示区域

        public string Result { get; private set; } // 当前聚焦元素的结果（如文本内容）

        // 滚动区域（目标 ScrollRect 的内容部分）
        public RectTransform ScrollingPanel { get { return scrollRect.content; } }

        /// <summary>
        /// 当不作为组件而是从其他脚本调用时使用的构造函数
        /// </summary>
        public UIVerticalScroller(RectTransform center, RectTransform elementSize, ScrollRect scrollRect, GameObject[] arrayOfElements)
        {
            this.center = center;
            this.elementSize = elementSize;
            this.scrollRect = scrollRect;
            this.arrayOfElements = arrayOfElements;
        }

        /// <summary>
        /// Awake 函数在实例化时调用
        /// </summary>
        public void Awake()
        {
            if (!scrollRect)
            {
                scrollRect = GetComponent<ScrollRect>();
            }

            if (!center)
            {
                Debug.LogError("请为滚动区域定义中心 RectTransform");
            }

            if (!elementSize)
            {
                elementSize = center;
            }

            if (arrayOfElements == null || arrayOfElements.Length == 0)
            {
                var childCount = ScrollingPanel.childCount;
                if (childCount > 0)
                {
                    arrayOfElements = new GameObject[childCount];
                    for (int i = 0; i < childCount; i++)
                    {
                        arrayOfElements[i] = ScrollingPanel.GetChild(i).gameObject;
                    }
                }
            }
        }

        /// <summary>
        /// 初始化和调整子元素
        /// </summary>
        /// <param name="startingIndex">初始索引</param>
        /// <param name="arrayOfElements">子元素数组</param>
        public void UpdateChildren(int startingIndex = -1, GameObject[] arrayOfElements = null)
        {
            // 如果提供了数组，使用传入的数组；否则获取滚动面板的子对象
            if (arrayOfElements != null)
            {
                this.arrayOfElements = arrayOfElements;
            }
            else
            {
                this.arrayOfElements = new GameObject[ScrollingPanel.childCount];
                for (int i = 0; i < ScrollingPanel.childCount; i++)
                {
                    this.arrayOfElements[i] = ScrollingPanel.GetChild(i).gameObject;
                }
            }

            // 将元素调整为指定尺寸
            for (var i = 0; i < this.arrayOfElements.Length; i++)
            {
                AddListener(arrayOfElements[i], i);

                RectTransform r = this.arrayOfElements[i].GetComponent<RectTransform>();
                r.anchorMax = r.anchorMin = r.pivot = new Vector2(0.5f, 0.5f);
                r.localPosition = new Vector2(0, i * elementSize.rect.size.y);
                r.sizeDelta = elementSize.rect.size;
            }

            // 准备滚动
            distance = new float[this.arrayOfElements.Length];
            distReposition = new float[this.arrayOfElements.Length];
            FocusedElementIndex = -1;

            // 如果给定了初始索引，则跳转到相应的元素
            if (startingIndex > -1)
            {
                startingIndex = startingIndex > this.arrayOfElements.Length ? this.arrayOfElements.Length - 1 : startingIndex;
                SnapToElement(startingIndex);
            }
        }

        private void AddListener(GameObject button, int index)
        {
            var buttonClick = button.GetComponent<Button>();
            buttonClick.onClick.RemoveAllListeners();
            buttonClick.onClick.AddListener(() => onButtonClicked?.Invoke(index));
        }

        public void Start()
        {
            if (scrollUpButton)
            {
                scrollUpButton.GetComponent<Button>().onClick.AddListener(() => ScrollUp());
            }
            if (scrollDownButton)
            {
                scrollDownButton.GetComponent<Button>().onClick.AddListener(() => ScrollDown());
            }
            UpdateChildren(startingIndex, arrayOfElements);
        }

        public void Update()
        {
            if (arrayOfElements.Length < 1)
            {
                return;
            }

            for (var i = 0; i < arrayOfElements.Length; i++)
            {
                var arrayElementRT = arrayOfElements[i].GetComponent<RectTransform>();

                distReposition[i] = center.position.y - arrayElementRT.position.y;
                distance[i] = Mathf.Abs(distReposition[i]);
                // 缩放效果
                Vector2 scale = Vector2.Max(minScale, new Vector2(1 / (1 + distance[i] * elementShrinkage.x), 1 / (1 + distance[i] * elementShrinkage.y)));
                arrayElementRT.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            }

            // 检测聚焦的元素
            float minDistance = Mathf.Min(distance);
            int oldFocusedElement = FocusedElementIndex;

            for (var i = 0; i < arrayOfElements.Length; i++)
            {
                arrayOfElements[i].GetComponent<CanvasGroup>().interactable = !disableUnfocused || minDistance == distance[i];
                if (minDistance == distance[i])
                {
                    FocusedElementIndex = i;
// #if UNITY_2022_1_OR_NEWER
//                     var textComponentTxtMeshPro = arrayOfElements[i].GetComponentInChildren<TMPro.TMP_Text>();
//                     if (textComponentTxtMeshPro != null)
//                     {
//                         Result = textComponentTxtMeshPro.text;
//                     }
// #else
                    var textComponent = arrayOfElements[i].name;
                    if (textComponent != null)
                    {
                        Result = textComponent;
                    }
// #endif
                }
            }

            if (FocusedElementIndex != oldFocusedElement)
            {
                onFocusChanged?.Invoke(FocusedElementIndex);
            }

            if (!UIExtensionsInputManager.GetMouseButton(0))
            {
                // 当没有拖拽时，缓慢滚动到最近的元素
                ScrollingElements();
            }

            // 阻止滚动超出最后一个元素
            if (stopMomentumOnEnd
                && (arrayOfElements[0].GetComponent<RectTransform>().position.y > center.position.y
                || arrayOfElements[arrayOfElements.Length - 1].GetComponent<RectTransform>().position.y < center.position.y))
            {
                scrollRect.velocity = Vector2.zero;
            }
        }

        private void ScrollingElements()
        {
            float newY = Mathf.Lerp(ScrollingPanel.anchoredPosition.y, ScrollingPanel.anchoredPosition.y + distReposition[FocusedElementIndex], Time.deltaTime * 2f);
            Vector2 newPosition = new Vector2(ScrollingPanel.anchoredPosition.x, newY);
            ScrollingPanel.anchoredPosition = newPosition;
        }

        public void SnapToElement(int element)
        {
            float deltaElementPositionY = elementSize.rect.height * element;
            Vector2 newPosition = new Vector2(ScrollingPanel.anchoredPosition.x, -deltaElementPositionY);
            ScrollingPanel.anchoredPosition = newPosition;
        }

        public void ScrollUp()
        {
            float deltaUp = elementSize.rect.height / 1.2f;
            Vector2 newPositionUp = new Vector2(ScrollingPanel.anchoredPosition.x, ScrollingPanel.anchoredPosition.y - deltaUp);
            ScrollingPanel.anchoredPosition = Vector2.Lerp(ScrollingPanel.anchoredPosition, newPositionUp, 1);
        }

        public void ScrollDown()
        {
            float deltaDown = elementSize.rect.height / 1.2f;
            Vector2 newPositionDown = new Vector2(ScrollingPanel.anchoredPosition.x, ScrollingPanel.anchoredPosition.y + deltaDown);
            ScrollingPanel.anchoredPosition = newPositionDown;
        }
    }
}

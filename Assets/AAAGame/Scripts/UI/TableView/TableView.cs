using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SocialPlatforms;
using System.Collections;

namespace Tacticsoft
{
    public delegate TableViewCell TableViewCellFun(TableView tv, int row);
    public delegate int TableViewIntFun(TableView tv);
    public delegate float TableViewFloatFun(TableView tv, int row);

    /// <summary>
    /// 可重复使用的垂直表格，API 灵感来源于 Cocoa 的 UITableView。
    /// 层级结构应为：
    /// GameObject + TableView（此组件）+ Mask + Scroll Rect（指向子对象）
    /// - 子 GameObject + Vertical Layout Group
    /// 此类在 Unity 内部 UI 组件之后执行，需调整脚本执行顺序。
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class TableView : MonoBehaviour
    {
        private TableViewIntFun m_NumberOfRowsForTableView;
        private TableViewFloatFun m_HeightForRowInTableView;
        private TableViewCellFun m_CellForRowInTableView;

        public UnityAction OnScrollToLoadMore; // 添加事件

        public bool HaveEvents()
        {
            return m_NumberOfRowsForTableView != null || m_HeightForRowInTableView != null || m_CellForRowInTableView != null;
        }

        public void SetNumberOfRowsForTableView(TableViewIntFun fun)
        {
            m_NumberOfRowsForTableView = fun;
        }

        public void SetHeightForRowInTableView(TableViewFloatFun fun)
        {
            m_HeightForRowInTableView = fun;
        }

        public void SetCellForRowInTableView(TableViewCellFun fun)
        {
            m_CellForRowInTableView = fun;
        }


        #region Public API
        /// <summary>
        /// 判断是否设置了相关事件。
        /// </summary>
        public ITableViewDataSource dataSource
        {
            get { return m_dataSource; }
            set { m_dataSource = value; m_requiresReload = true; }
        }

        /// <summary>
        /// 获取一个不再使用的单元格以供重复使用。
        /// </summary>
        /// <param name="reuseIdentifier">单元格类型的标识符</param>
        /// <returns>如果可用，返回已准备的单元格；否则返回 null</returns>
        public TableViewCell GetReusableCell(string reuseIdentifier)
        {
            LinkedList<TableViewCell> cells;
            if (!m_reusableCells.TryGetValue(reuseIdentifier, out cells))
            {
                return null;
            }
            if (cells.Count == 0)
            {
                return null;
            }
            TableViewCell cell = cells.First.Value;
            cells.RemoveFirst();
            return cell;
        }

        public bool isEmpty { get; private set; }
        /// <summary>
        /// 重新加载表格数据。如果数据源发生变化导致基本布局更改（例如行数更改），需手动调用此方法。
        /// </summary>
        public void ReloadData()
        {
            if (m_NumberOfRowsForTableView == null || m_HeightForRowInTableView == null) return;

            if (m_scrollRect == null)
            {
                m_scrollRect = GetComponent<ScrollRect>();
            }
            if (m_verticalLayoutGroup == null)
            {
                m_verticalLayoutGroup = GetComponentInChildren<VerticalLayoutGroup>();
            }
            if (m_visibleCells == null)
            {
                m_visibleCells = new Dictionary<int, TableViewCell>();
            }
            if (m_reusableCells == null)
            {
                m_reusableCells = new Dictionary<string, LinkedList<TableViewCell>>();
            }
            if (m_reusableCellContainer == null)
            {
                m_reusableCellContainer = new GameObject("ReusableCells", typeof(RectTransform)).GetComponent<RectTransform>();
                m_reusableCellContainer.SetParent(this.transform, false);
                m_reusableCellContainer.gameObject.SetActive(false);
            }
            if (m_scrollRect != null && m_scrollRect.content != null)
            {
                if (m_topPadding == null)
                {
                    m_topPadding = CreateEmptyPaddingElement("TopPadding");
                    m_topPadding.transform.SetParent(m_scrollRect.content, false);
                }
                if (m_bottomPadding == null)
                {
                    m_bottomPadding = CreateEmptyPaddingElement("Bottom");
                    m_bottomPadding.transform.SetParent(m_scrollRect.content, false);
                }
            }

            if (m_scrollRect == null || m_scrollRect.content == null) return;

            m_rowHeights = new float[m_NumberOfRowsForTableView(this)]; // m_dataSource.GetNumberOfRowsForTableView(this)
            this.isEmpty = m_rowHeights.Length == 0;
            if (this.isEmpty)
            {
                ClearAllRows();
                return;
            }
            m_cumulativeRowHeights = new float[m_rowHeights.Length];
            m_cleanCumulativeIndex = -1;

            float spacing = (m_verticalLayoutGroup != null) ? m_verticalLayoutGroup.spacing : 0;
            for (int i = 0; i < m_rowHeights.Length; i++)
            {
                m_rowHeights[i] = m_HeightForRowInTableView(this, i);//  m_dataSource.GetHeightForRowInTableView(this, i);
                if (i > 0)
                {
                    m_rowHeights[i] += spacing;
                }
            }

            m_scrollRect.StopMovement();
            m_scrollRect.content.sizeDelta = new Vector2(m_scrollRect.content.sizeDelta[0],
                GetCumulativeRowHeight(m_rowHeights.Length - 1));

            RecalculateVisibleRowsFromScratch();
            CoroutineRunner.Instance.RunCoroutine(ForceLayoutRefresh());
            m_requiresReload = false;
        }        private IEnumerator ForceLayoutRefresh()
        {
            yield return new WaitForEndOfFrame();
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_scrollRect.GetComponent<RectTransform>());
        }

        /// <summary>
        /// 获取指定行的单元格（如果可见）。如果不可见则返回 null。
        /// </summary>
        public TableViewCell GetCellAtRow(int row)
        {
            TableViewCell retVal = null;
            m_visibleCells.TryGetValue(row, out retVal);
            return retVal;
        }

        /// <summary>
        /// 获取当前可见的行范围。
        /// </summary>
        public Range VisibleRowRange
        {
            get { return m_visibleRowRange; }
            set { m_visibleRowRange = value; }
        }

        /// <summary>
        /// 通知表格视图某一行的大小发生了变化。
        /// </summary>
        public void NotifyCellDimensionsChanged(int row)
        {
            if (m_HeightForRowInTableView == null) return;
            float oldHeight = m_rowHeights[row];
            m_rowHeights[row] = m_HeightForRowInTableView(this, row);// m_dataSource.GetHeightForRowInTableView(this, row);
            m_cleanCumulativeIndex = Mathf.Min(m_cleanCumulativeIndex, row - 1);
            if (m_visibleRowRange.Contains(row))
            {
                TableViewCell cell = GetCellAtRow(row);
                cell.GetComponent<LayoutElement>().preferredHeight = m_rowHeights[row];
                if (row > 0)
                {
                    float spacing = (m_verticalLayoutGroup != null) ? m_verticalLayoutGroup.spacing : 0;
                    cell.GetComponent<LayoutElement>().preferredHeight -= spacing;
                }
            }
            float heightDelta = m_rowHeights[row] - oldHeight;
            m_scrollRect.content.sizeDelta = new Vector2(m_scrollRect.content.sizeDelta.x,
                m_scrollRect.content.sizeDelta.y + heightDelta);
            m_requiresRefresh = true;
        }

        /// <summary>
        /// 获取表格的最大可滚动高度。scrollY 属性的值不会超过此值。
        /// </summary>
        public float scrollableHeight
        {
            get
            {
                if (m_scrollRect == null || m_scrollRect.content == null) return 0;
                return m_scrollRect.content.rect.height - (this.transform as RectTransform).rect.height;
            }
        }

        /// <summary>
        /// 获取或设置表格的当前滚动位置。
        /// </summary>
        public float ScrollY
        {
            get
            {
                return m_scrollY;
            }
            set
            {
                if (this.isEmpty || m_rowHeights == null || m_rowHeights.Length == 0)
                {
                    m_scrollY = value;
                    return;
                }
                value = Mathf.Clamp(value, 0, GetScrollYForRow(m_rowHeights.Length - 1, true));
                if (m_scrollY != value)
                {
                    m_scrollY = value;
                    m_requiresRefresh = true;
                    float relativeScroll = value / this.scrollableHeight;
                    m_scrollRect.verticalNormalizedPosition = 1 - relativeScroll;
                }
            }
        }

        /// <summary>
        /// 获取指定行在表格顶部时所需的滚动位置。
        /// </summary>
        /// <param name="row">目标行</param>
        /// <param name="above">顶部是否高于该行</param>
        /// <returns>滚动位置，可用于设置 scrollY 属性</returns>
        public float GetScrollYForRow(int row, bool above)
        {
            float retVal = GetCumulativeRowHeight(row);
            if (above)
            {
                retVal -= m_rowHeights[row];
            }
            return retVal;
        }

        #endregion

        #region Private implementation

        private ITableViewDataSource m_dataSource;
        public bool m_requiresReload;

        private VerticalLayoutGroup m_verticalLayoutGroup;
        private ScrollRect m_scrollRect;
        private LayoutElement m_topPadding;
        private LayoutElement m_bottomPadding;

        private float[] m_rowHeights;
        //cumulative[i] = sum(rowHeights[j] for 0 <= j <= i)
        private float[] m_cumulativeRowHeights;
        private int m_cleanCumulativeIndex;

        private Dictionary<int, TableViewCell> m_visibleCells;
        private Range m_visibleRowRange;

        private RectTransform m_reusableCellContainer;
        private Dictionary<string, LinkedList<TableViewCell>> m_reusableCells;

        private float m_scrollY;

        public bool m_requiresRefresh;

        private void ScrollViewValueChanged(Vector2 newScrollValue)
        {
            float relativeScroll = 1 - newScrollValue.y;
            // GF.LogError("relativeScroll:  " + relativeScroll);
            m_scrollY = relativeScroll * scrollableHeight;
            m_requiresRefresh = true;
            //GF.LogInfo(m_scrollY.ToString(("0.00")));
            // 检测是否滚动到 90%
            if (relativeScroll >= 0.9f && OnScrollToLoadMore != null)
            {
                OnScrollToLoadMore.Invoke(); // 触发加载更多事件
            }
        }

        private void RecalculateVisibleRowsFromScratch()
        {
            ClearAllRows();
            SetInitialVisibleRows();
        }

        public void ClearAllRows()
        {
            while (m_visibleCells != null && m_visibleCells.Count > 0)
            {
                HideRow(false);
            }
            m_visibleRowRange = new Range(0, 0);
        }

        void Awake()
        {
            isEmpty = true;
            if (m_scrollRect == null)
            {
                m_scrollRect = GetComponent<ScrollRect>();
            }
            if (m_verticalLayoutGroup == null)
            {
                m_verticalLayoutGroup = GetComponentInChildren<VerticalLayoutGroup>();
            }
            if (m_scrollRect != null && m_scrollRect.content != null)
            {
                if (m_topPadding == null)
                {
                    m_topPadding = CreateEmptyPaddingElement("TopPadding");
                    m_topPadding.transform.SetParent(m_scrollRect.content, false);
                }
                if (m_bottomPadding == null)
                {
                    m_bottomPadding = CreateEmptyPaddingElement("Bottom");
                    m_bottomPadding.transform.SetParent(m_scrollRect.content, false);
                }
            }
            if (m_visibleCells == null)
            {
                m_visibleCells = new Dictionary<int, TableViewCell>();
            }

            if (m_reusableCellContainer == null)
            {
                m_reusableCellContainer = new GameObject("ReusableCells", typeof(RectTransform)).GetComponent<RectTransform>();
                m_reusableCellContainer.SetParent(this.transform, false);
                m_reusableCellContainer.gameObject.SetActive(false);
            }
            if (m_reusableCells == null)
            {
                m_reusableCells = new Dictionary<string, LinkedList<TableViewCell>>();
            }
        }

        void Update()
        {
            if (m_requiresReload)
            {
                ReloadData();
            }
        }

        void LateUpdate()
        {
            if (m_requiresRefresh)
            {
                RefreshVisibleRows();
            }
        }

        void OnEnable()
        {
            m_scrollRect.onValueChanged.AddListener(ScrollViewValueChanged);
        }

        void OnDisable()
        {
            m_scrollRect.onValueChanged.RemoveListener(ScrollViewValueChanged);
        }

        private Range CalculateCurrentVisibleRowRange()
        {
            float startY = m_scrollY;
            float endY = m_scrollY + (this.transform as RectTransform).rect.height;
            int startIndex = FindIndexOfRowAtY(startY);
            int endIndex = FindIndexOfRowAtY(endY);
            return new Range(startIndex, endIndex - startIndex + 1);
        }

        private void SetInitialVisibleRows()
        {
            Range visibleRows = CalculateCurrentVisibleRowRange();
            for (int i = 0; i < visibleRows.count; i++)
            {
                AddRow(visibleRows.from + i, true);
            }
            m_visibleRowRange = visibleRows;
            UpdatePaddingElements();
        }

        private void AddRow(int row, bool atEnd)
        {
            if (m_CellForRowInTableView == null) return;
            TableViewCell newCell = m_CellForRowInTableView(this, row); // m_dataSource.GetCellForRowInTableView(this, row);
            newCell.transform.SetParent(m_scrollRect.content, false);

            LayoutElement layoutElement = newCell.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = newCell.gameObject.AddComponent<LayoutElement>();
            }
            layoutElement.preferredHeight = m_rowHeights[row];
            if (row > 0)
            {
                float spacing = (m_verticalLayoutGroup != null) ? m_verticalLayoutGroup.spacing : 0;
                layoutElement.preferredHeight -= spacing;
            }

            m_visibleCells[row] = newCell;
            if (atEnd)
            {
                newCell.transform.SetSiblingIndex(m_scrollRect.content.childCount - 2); //One before bottom padding
            }
            else
            {
                newCell.transform.SetSiblingIndex(1); //One after the top padding
            }
        }

        private void RefreshVisibleRows()
        {
            m_requiresRefresh = false;

            if (this.isEmpty)
            {
                return;
            }

            Range newVisibleRows = CalculateCurrentVisibleRowRange();
            if (m_visibleRowRange.count <= 0 || newVisibleRows.count <= 0) {
                return;
            }
            int oldTo = m_visibleRowRange.Last();
            int newTo = newVisibleRows.Last(); 

            if (newVisibleRows.from > oldTo || newTo < m_visibleRowRange.from)
            {
                //We jumped to a completely different segment this frame, destroy all and recreate
                RecalculateVisibleRowsFromScratch();
                return;
            }

            //Remove rows that disappeared to the top
            for (int i = m_visibleRowRange.from; i < newVisibleRows.from; i++)
            {
                HideRow(false);
            }
            //Remove rows that disappeared to the bottom
            for (int i = newTo; i < oldTo; i++)
            {
                HideRow(true);
            }
            //Add rows that appeared on top
            for (int i = m_visibleRowRange.from - 1; i >= newVisibleRows.from; i--)
            {
                AddRow(i, false);
            }
            //Add rows that appeared on bottom
            for (int i = oldTo + 1; i <= newTo; i++)
            {
                AddRow(i, true);
            }
            m_visibleRowRange = newVisibleRows;
            UpdatePaddingElements();
        }

        private void UpdatePaddingElements()
        {
            if (m_topPadding == null || m_bottomPadding == null) return;
            float hiddenElementsHeightSum = 0;
            for (int i = 0; i < m_visibleRowRange.from; i++)
            {
                hiddenElementsHeightSum += m_rowHeights[i];
            }
            m_topPadding.preferredHeight = hiddenElementsHeightSum;
            m_topPadding.gameObject.SetActive(m_topPadding.preferredHeight > 0);
            for (int i = m_visibleRowRange.from; i <= m_visibleRowRange.Last(); i++)
            {
                hiddenElementsHeightSum += m_rowHeights[i];
            }
            float bottomPaddingHeight = m_scrollRect.content.rect.height - hiddenElementsHeightSum;
            float spacing = (m_verticalLayoutGroup != null) ? m_verticalLayoutGroup.spacing : 0;
            m_bottomPadding.preferredHeight = bottomPaddingHeight - spacing;
            m_bottomPadding.gameObject.SetActive(m_bottomPadding.preferredHeight > 0);
        }

        private void HideRow(bool last)
        {
            //GF.LogInfo("Hiding row at scroll y " + m_scrollY.ToString("0.00"));

            int row = last ? m_visibleRowRange.Last() : m_visibleRowRange.from;
            TableViewCell removedCell = m_visibleCells[row];
            StoreCellForReuse(removedCell);
            m_visibleCells.Remove(row);
            m_visibleRowRange.count -= 1;
            if (!last)
            {
                m_visibleRowRange.from += 1;
            }
        }

        private LayoutElement CreateEmptyPaddingElement(string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            LayoutElement le = go.GetComponent<LayoutElement>();
            return le;
        }

        private int FindIndexOfRowAtY(float y)
        {
            //TODO : 如果在已清理的累计行高区域内，则进行二分查找；否则逐步查找直到找到。
            return FindIndexOfRowAtY(y, 0, m_cumulativeRowHeights.Length - 1);
        }

        private int FindIndexOfRowAtY(float y, int startIndex, int endIndex)
        {
            if (startIndex >= endIndex)
            {
                return startIndex;
            }
            int midIndex = (startIndex + endIndex) / 2;
            if (GetCumulativeRowHeight(midIndex) >= y)
            {
                return FindIndexOfRowAtY(y, startIndex, midIndex);
            }
            else
            {
                return FindIndexOfRowAtY(y, midIndex + 1, endIndex);
            }
        }

        private float GetCumulativeRowHeight(int row)
        {
            while (m_cleanCumulativeIndex < row)
            {
                m_cleanCumulativeIndex++;
                //GF.LogInfo("Cumulative index : " + m_cleanCumulativeIndex.ToString());
                m_cumulativeRowHeights[m_cleanCumulativeIndex] = m_rowHeights[m_cleanCumulativeIndex];
                if (m_cleanCumulativeIndex > 0)
                {
                    m_cumulativeRowHeights[m_cleanCumulativeIndex] += m_cumulativeRowHeights[m_cleanCumulativeIndex - 1];
                }
            }
            return m_cumulativeRowHeights[row];
        }

        private void StoreCellForReuse(TableViewCell cell)
        {
            string reuseIdentifier = cell.reuseIdentifier;

            if (string.IsNullOrEmpty(reuseIdentifier))
            {
                GameObject.Destroy(cell.gameObject);
                return;
            }

            if (!m_reusableCells.ContainsKey(reuseIdentifier))
            {
                m_reusableCells.Add(reuseIdentifier, new LinkedList<TableViewCell>());
            }
            m_reusableCells[reuseIdentifier].AddLast(cell);
            cell.transform.SetParent(m_reusableCellContainer, false);
        }

        #endregion



    }

    internal static class RangeExtensions
    {
        public static int Last(this Range range)
        {
            if (range.count == 0)
            {
                throw new System.InvalidOperationException("Empty range has no to()");
            }
            return (range.from + range.count - 1);
        }

        public static bool Contains(this Range range, int num)
        {
            return num >= range.from && num < (range.from + range.count);
        }
    }
}

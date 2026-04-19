using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CustomScrollView.Core;
using CustomScrollView.Enums;
using CustomScrollView.Interfaces;
using CustomScrollView.Layout;

namespace CustomScrollView.Controller
{
    /// <summary>
    /// Core controller that drives the scroll view.
    /// Manages visible cell lifecycle using ILayoutStrategy + ICellProvider.
    /// Attach to a GameObject with a ScrollRect.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public sealed class ScrollViewController : MonoBehaviour, IScrollViewController
    {
        // ── Inspector fields ──────────────────────────────────────

        [Header("Layout")]
        [SerializeField] private ScrollDirection _direction = ScrollDirection.Vertical;
        [SerializeField] private GridConstraint _gridConstraint = GridConstraint.None;
        [SerializeField] private int _constraintCount = 1;
        [SerializeField] private float _spacing = 8f;
        [SerializeField] private float _gridCrossSpacing = 8f;
        [SerializeField] private RectOffset _padding = new();

        // ── Events ────────────────────────────────────────────────

        /// <summary>Fired when a cell becomes visible. (section, index, cellGO)</summary>
        public event Action<int, int, GameObject> OnCellVisible;
        /// <summary>Fired when a cell is recycled. (section, index)</summary>
        public event Action<int, int> OnCellRecycled;

        // ── Dependencies (set via code) ───────────────────────────

        private IScrollDataSource _dataSource;
        private ICellProvider _cellProvider;
        private ILayoutStrategy _layout;
        private ElementMap _elementMap;

        // ── Internal state ────────────────────────────────────────

        private ScrollRect _scrollRect;
        private RectTransform _viewport;
        private RectTransform _content;
        private Transform _poolRoot;

        private readonly Dictionary<int, GameObject> _activeCells = new();
        private VisibleRange _currentRange;
        private bool _initialized;
        private Coroutine _scrollCoroutine;

        // ── Public properties ─────────────────────────────────────

        public ScrollDirection Direction => _direction;
        public GridConstraint Constraint => _gridConstraint;

        // ── Setup ─────────────────────────────────────────────────

        /// <summary>
        /// Initialize the scroll view with a data source and cell provider.
        /// Call this before ReloadData().
        /// </summary>
        public void Initialize(IScrollDataSource dataSource, ICellProvider cellProvider)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _cellProvider = cellProvider ?? throw new ArgumentNullException(nameof(cellProvider));

            EnsureComponents();

            _elementMap = new ElementMap();
            _layout = LayoutStrategyFactory.Create(_gridConstraint);

            _scrollRect.onValueChanged.AddListener(OnScroll);
            _initialized = true;

            ReloadData();
        }

        /// <summary>
        /// Convenience init using delegates instead of implementing interfaces.
        /// Creates a DefaultCellProvider internally.
        /// </summary>
        public void Initialize(
            IScrollDataSource dataSource,
            Func<int, int, GameObject> cellPrefabSelector,
            Func<int, GameObject> headerPrefabSelector = null,
            Func<int, GameObject> footerPrefabSelector = null)
        {
            EnsureComponents();
            var pool = new CellPool(_poolRoot);
            var provider = new View.DefaultCellProvider(pool, cellPrefabSelector, headerPrefabSelector, footerPrefabSelector);
            Initialize(dataSource, provider);
        }

        // ── IScrollViewController ─────────────────────────────────

        public void ReloadData()
        {
            if (!_initialized) return;

            RecycleAll();

            var config = BuildConfig();
            _elementMap.Build(_dataSource);
            _layout.Build(_dataSource, config);

            // Resize content
            SetContentSize(_layout.GetContentSize());

            // Reset position
            _currentRange = VisibleRange.Empty;
            UpdateVisibleCells();
        }

        public void ScrollTo(int section, int index, float duration = 0.3f)
        {
            if (!_initialized) return;
            int flat = _elementMap.FindFlatIndex(section, index);
            if (flat < 0) return;

            var rect = _layout.GetElementRect(flat);
            ScrollToPosition(rect.Position, duration);
        }

        public void ScrollToTop(float duration = 0.3f)
        {
            ScrollToPosition(0f, duration);
        }

        public void ScrollToBottom(float duration = 0.3f)
        {
            float max = _layout.GetContentSize() - GetViewportMainSize();
            ScrollToPosition(Mathf.Max(0f, max), duration);
        }

        // ── Scroll handling ───────────────────────────────────────

        private void OnScroll(Vector2 _)
        {
            UpdateVisibleCells();
        }

        private void UpdateVisibleCells()
        {
            float scrollPos = GetScrollPosition();
            float viewportSize = GetViewportMainSize();
            var newRange = _layout.GetVisibleRange(scrollPos, viewportSize);

            if (newRange.IsEmpty)
            {
                RecycleAll();
                _currentRange = newRange;
                return;
            }

            // Recycle cells that fell out of range
            if (!_currentRange.IsEmpty)
            {
                for (int i = _currentRange.First; i <= _currentRange.Last; i++)
                {
                    if (i < newRange.First || i > newRange.Last)
                        RecycleElement(i);
                }
            }

            // Show cells that entered range
            for (int i = newRange.First; i <= newRange.Last; i++)
            {
                if (!_activeCells.ContainsKey(i))
                    ShowElement(i);
            }

            _currentRange = newRange;
        }

        // ── Element lifecycle ─────────────────────────────────────

        private void ShowElement(int flatIndex)
        {
            var info = _elementMap.Get(flatIndex);
            var rect = _layout.GetElementRect(flatIndex);
            GameObject go = null;

            switch (info.Type)
            {
                case ElementInfo.ElementType.Header:
                    go = _cellProvider.GetHeader(info.Section, _content);
                    go?.GetComponent<IScrollSectionElement>()?.OnFill(info.Section);
                    break;

                case ElementInfo.ElementType.Footer:
                    go = _cellProvider.GetFooter(info.Section, _content);
                    go?.GetComponent<IScrollSectionElement>()?.OnFill(info.Section);
                    break;

                case ElementInfo.ElementType.Item:
                    string reuseId = GetReuseId(info.Section, info.Index);
                    go = _cellProvider.GetCell(info.Section, info.Index, reuseId, _content);
                    go?.GetComponent<IScrollCell>()?.OnFill(info.Section, info.Index);
                    OnCellVisible?.Invoke(info.Section, info.Index, go);
                    break;
            }

            if (go != null)
            {
                PositionElement(go, rect);
                _activeCells[flatIndex] = go;
            }
        }

        private void RecycleElement(int flatIndex)
        {
            if (!_activeCells.TryGetValue(flatIndex, out var go)) return;
            _activeCells.Remove(flatIndex);

            var info = _elementMap.Get(flatIndex);
            switch (info.Type)
            {
                case ElementInfo.ElementType.Header:
                    _cellProvider.RecycleHeader(go, info.Section);
                    break;
                case ElementInfo.ElementType.Footer:
                    _cellProvider.RecycleFooter(go, info.Section);
                    break;
                case ElementInfo.ElementType.Item:
                    string reuseId = GetReuseId(info.Section, info.Index);
                    _cellProvider.RecycleCell(go, reuseId);
                    OnCellRecycled?.Invoke(info.Section, info.Index);
                    break;
            }
        }

        private void RecycleAll()
        {
            foreach (var kvp in _activeCells)
            {
                var info = _elementMap.Get(kvp.Key);
                switch (info.Type)
                {
                    case ElementInfo.ElementType.Header:
                        _cellProvider.RecycleHeader(kvp.Value, info.Section);
                        break;
                    case ElementInfo.ElementType.Footer:
                        _cellProvider.RecycleFooter(kvp.Value, info.Section);
                        break;
                    case ElementInfo.ElementType.Item:
                        _cellProvider.RecycleCell(kvp.Value, GetReuseId(info.Section, info.Index));
                        break;
                }
            }
            _activeCells.Clear();
            _currentRange = VisibleRange.Empty;
        }

        // ── Positioning ───────────────────────────────────────────

        private void PositionElement(GameObject go, ElementRect rect)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;

            rt.anchorMin = _direction == ScrollDirection.Vertical
                ? new Vector2(0f, 1f)
                : new Vector2(0f, 0f);
            rt.anchorMax = rt.anchorMin;
            rt.pivot = new Vector2(0f, 1f);

            if (_direction == ScrollDirection.Vertical)
            {
                rt.anchoredPosition = new Vector2(rect.CrossPosition, -rect.Position);
                rt.sizeDelta = new Vector2(rect.CrossSize, rect.Size);
            }
            else
            {
                rt.anchoredPosition = new Vector2(rect.Position, -rect.CrossPosition);
                rt.sizeDelta = new Vector2(rect.Size, rect.CrossSize);
            }
        }

        // ── Helpers ───────────────────────────────────────────────

        private void EnsureComponents()
        {
            _scrollRect = GetComponent<ScrollRect>();
            _viewport = _scrollRect.viewport != null
                ? _scrollRect.viewport
                : _scrollRect.GetComponent<RectTransform>();

            if (_scrollRect.content == null)
            {
                var contentGO = new GameObject("Content", typeof(RectTransform));
                contentGO.transform.SetParent(_viewport, false);
                _scrollRect.content = contentGO.GetComponent<RectTransform>();
            }
            _content = _scrollRect.content;

            // Forces layout pass immediately
            Canvas.ForceUpdateCanvases();
            
            // Configure scroll direction
            _scrollRect.horizontal = _direction == ScrollDirection.Horizontal;
            _scrollRect.vertical = _direction == ScrollDirection.Vertical;

            // Content anchors
            if (_direction == ScrollDirection.Vertical)
            {
                _content.anchorMin = new Vector2(0f, 1f);
                _content.anchorMax = new Vector2(1f, 1f);
                _content.pivot = new Vector2(0.5f, 1f);
            }
            else
            {
                _content.anchorMin = new Vector2(0f, 0f);
                _content.anchorMax = new Vector2(0f, 1f);
                _content.pivot = new Vector2(0f, 0.5f);
            }

            // Pool root (hidden container for recycled cells)
            var poolGO = new GameObject("_Pool");
            poolGO.transform.SetParent(transform, false);
            poolGO.SetActive(false);
            _poolRoot = poolGO.transform;
        }

        private ScrollViewConfig BuildConfig()
        {
            var vpRect = _viewport.rect;
            return new ScrollViewConfig(
                _direction,
                _gridConstraint,
                _constraintCount,
                _spacing,
                _gridCrossSpacing,
                _padding,
                _direction == ScrollDirection.Vertical ? vpRect.height : vpRect.width,
                _direction == ScrollDirection.Vertical ? vpRect.width : vpRect.height
            );
        }

        private float GetScrollPosition()
        {
            if (_direction == ScrollDirection.Vertical)
                return _content.anchoredPosition.y;
            else
                return -_content.anchoredPosition.x;
        }

        private float GetViewportMainSize()
        {
            return _direction == ScrollDirection.Vertical
                ? _viewport.rect.height
                : _viewport.rect.width;
        }

        private void SetContentSize(float mainSize)
        {
            if (_direction == ScrollDirection.Vertical)
                _content.sizeDelta = new Vector2(_content.sizeDelta.x, mainSize);
            else
                _content.sizeDelta = new Vector2(mainSize, _content.sizeDelta.y);
        }

        private void ScrollToPosition(float position, float duration)
        {
            if (_scrollCoroutine != null) StopCoroutine(_scrollCoroutine);

            if (duration <= 0f)
            {
                SetScrollPosition(position);
                return;
            }

            _scrollCoroutine = StartCoroutine(SmoothScroll(position, duration));
        }

        private void SetScrollPosition(float position)
        {
            if (_direction == ScrollDirection.Vertical)
                _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, position);
            else
                _content.anchoredPosition = new Vector2(-position, _content.anchoredPosition.y);
        }

        private IEnumerator SmoothScroll(float target, float duration)
        {
            float start = GetScrollPosition();
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                SetScrollPosition(Mathf.Lerp(start, target, t));
                yield return null;
            }

            SetScrollPosition(target);
            _scrollCoroutine = null;
        }

        /// <summary>
        /// Override in subclass or set via delegate for multi-type cells.
        /// Default uses prefab name.
        /// </summary>
        private string GetReuseId(int section, int index)
        {
            return $"cell_{section}";
        }

        private void OnDestroy()
        {
            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveListener(OnScroll);
        }
    }
}

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
        private readonly Dictionary<int, CellBindings> _bindings = new();
        private VisibleRange _currentRange;
        private bool _initialized;
        private Coroutine _scrollCoroutine;

        private readonly struct CellBindings
        {
            public readonly RectTransform Rt;
            public readonly IScrollCell Fill;
            public readonly IScrollSectionElement SectionFill;

            public CellBindings(RectTransform rt, IScrollCell fill, IScrollSectionElement sectionFill)
            {
                Rt = rt;
                Fill = fill;
                SectionFill = sectionFill;
            }
        }

        // ── Public properties ─────────────────────────────────────

        /// <summary>Scroll axis configured in the Inspector.</summary>
        public ScrollDirection Direction => _direction;

        /// <summary>Global grid constraint configured in the Inspector. Per-section overrides take precedence.</summary>
        public GridConstraint Constraint => _gridConstraint;

        // ── Setup ─────────────────────────────────────────────────

        /// <summary>
        /// Initialize the scroll view with a data source and a fully custom cell provider.
        /// Calls <see cref="ReloadData"/> automatically. Safe to call multiple times to swap data sources.
        /// </summary>
        /// <param name="dataSource">
        /// Supplies section/item counts and sizes. Optionally implement
        /// <see cref="ISectionLayoutProvider"/> on the same object for per-section layout overrides.
        /// </param>
        /// <param name="cellProvider">Creates, fills, and recycles cell GameObjects.</param>
        public void Initialize(IScrollDataSource dataSource, ICellProvider cellProvider)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _cellProvider = cellProvider ?? throw new ArgumentNullException(nameof(cellProvider));

            EnsureComponents();

            _elementMap = new ElementMap();
            _layout = LayoutStrategyFactory.Create();

            _scrollRect.onValueChanged.AddListener(OnScroll);
            _initialized = true;

            ReloadData();
        }

        /// <summary>
        /// Convenience overload using delegate selectors instead of a full <see cref="ICellProvider"/>.
        /// Builds a <c>DefaultCellProvider</c> internally.
        /// </summary>
        /// <param name="dataSource">Supplies section/item counts and sizes.</param>
        /// <param name="cellPrefabSelector">Returns the prefab to instantiate for a given (section, index).</param>
        /// <param name="headerPrefabSelector">Returns the header prefab for a section. Null = no headers.</param>
        /// <param name="footerPrefabSelector">Returns the footer prefab for a section. Null = no footers.</param>
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

        /// <summary>
        /// Rebuilds the layout and refreshes all visible cells from the current data source.
        /// Call after adding, removing, or resizing items. Already called automatically on initialization.
        /// </summary>
        public void ReloadData()
        {
            if (!_initialized) return;

            RecycleAll();
            _elementMap.Build(_dataSource);

            RebuildLayout();

            // ScrollRect with AutoHideAndExpandViewport inserts the scrollbar
            // once content overflows, which shrinks the viewport cross-size.
            // Force that pass now and rebuild once if it actually changed.
            float crossBefore = GetViewportCrossSize();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_scrollRect.transform);
            if (!Mathf.Approximately(crossBefore, GetViewportCrossSize()))
                RebuildLayout();

            _currentRange = VisibleRange.Empty;
            UpdateVisibleCells();
        }

        private void RebuildLayout()
        {
            _layout.Build(_dataSource, BuildConfig());
            SetContentSize(_layout.GetContentSize());
        }

        private float GetViewportCrossSize()
        {
            return _direction == ScrollDirection.Vertical
                ? _viewport.rect.width
                : _viewport.rect.height;
        }

        /// <summary>
        /// Animates the scroll position so the item at (<paramref name="section"/>, <paramref name="index"/>) is
        /// at the top (vertical) or left (horizontal) of the viewport.
        /// The position is clamped so the content never overscrolls past the end.
        /// Silently ignored if the section/index does not exist.
        /// </summary>
        /// <param name="section">Zero-based section index.</param>
        /// <param name="index">Zero-based item index within the section.</param>
        /// <param name="duration">Animation duration in seconds. Pass 0 for an instant jump.</param>
        public void ScrollTo(int section, int index, float duration = 0.3f)
        {
            if (!_initialized) return;
            int flat = _elementMap.FindFlatIndex(section, index);
            if (flat < 0) return;

            var rect = _layout.GetElementRect(flat);
            ScrollToPosition(rect.Position, duration);
        }

        /// <summary>
        /// Animates the scroll position to the very beginning of the content.
        /// </summary>
        /// <param name="duration">Animation duration in seconds. Pass 0 for an instant jump.</param>
        public void ScrollToTop(float duration = 0.3f)
        {
            ScrollToPosition(0f, duration);
        }

        /// <summary>
        /// Animates the scroll position to the very end of the content.
        /// </summary>
        /// <param name="duration">Animation duration in seconds. Pass 0 for an instant jump.</param>
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

            if (newRange.First == _currentRange.First && newRange.Last == _currentRange.Last)
                return;

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
                    break;
                case ElementInfo.ElementType.Footer:
                    go = _cellProvider.GetFooter(info.Section, _content);
                    break;
                case ElementInfo.ElementType.Item:
                    go = _cellProvider.GetCell(info.Section, info.Index, _content);
                    break;
            }

            if (go == null) return;

            var bindings = GetOrCacheBindings(go);

            switch (info.Type)
            {
                case ElementInfo.ElementType.Header:
                case ElementInfo.ElementType.Footer:
                    bindings.SectionFill?.OnFill(info.Section);
                    break;
                case ElementInfo.ElementType.Item:
                    bindings.Fill?.OnFill(info.Section, info.Index);
                    OnCellVisible?.Invoke(info.Section, info.Index, go);
                    break;
            }

            PositionElement(bindings.Rt, rect);
            _activeCells[flatIndex] = go;
        }

        private CellBindings GetOrCacheBindings(GameObject go)
        {
            int id = go.GetInstanceID();
            if (!_bindings.TryGetValue(id, out var b))
            {
                b = new CellBindings(
                    go.GetComponent<RectTransform>(),
                    go.GetComponent<IScrollCell>(),
                    go.GetComponent<IScrollSectionElement>());
                _bindings[id] = b;
            }
            return b;
        }

        private void RecycleElement(int flatIndex)
        {
            if (!_activeCells.TryGetValue(flatIndex, out var go)) return;
            _activeCells.Remove(flatIndex);

            var info = _elementMap.Get(flatIndex);
            switch (info.Type)
            {
                case ElementInfo.ElementType.Header:
                    _cellProvider.RecycleHeader(go);
                    break;
                case ElementInfo.ElementType.Footer:
                    _cellProvider.RecycleFooter(go);
                    break;
                case ElementInfo.ElementType.Item:
                    _cellProvider.RecycleCell(go);
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
                        _cellProvider.RecycleHeader(kvp.Value);
                        break;
                    case ElementInfo.ElementType.Footer:
                        _cellProvider.RecycleFooter(kvp.Value);
                        break;
                    case ElementInfo.ElementType.Item:
                        _cellProvider.RecycleCell(kvp.Value);
                        break;
                }
            }
            _activeCells.Clear();
            _currentRange = VisibleRange.Empty;
        }

        // ── Positioning ───────────────────────────────────────────

        private void PositionElement(RectTransform rt, ElementRect rect)
        {
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
            float max = Mathf.Max(0f, _layout.GetContentSize() - GetViewportMainSize());
            position = Mathf.Clamp(position, 0f, max);

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

        private void OnDestroy()
        {
            if (_scrollRect != null)
                _scrollRect.onValueChanged.RemoveListener(OnScroll);
        }
    }
}

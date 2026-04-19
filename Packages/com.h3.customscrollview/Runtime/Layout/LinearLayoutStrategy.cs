using System.Collections.Generic;
using CustomScrollView.Core;
using CustomScrollView.Interfaces;

namespace CustomScrollView.Layout
{
    /// <summary>
    /// Layout strategy for simple vertical or horizontal lists (no grid).
    /// Each element occupies the full cross-axis width.
    /// </summary>
    public sealed class LinearLayoutStrategy : ILayoutStrategy
    {
        private readonly ElementMap _map = new();
        private readonly List<ElementRect> _rects = new();
        private float _contentSize;
        private float _crossSize;

        public int TotalElementCount => _map.Count;

        public void Build(IScrollDataSource dataSource, ScrollViewConfig config)
        {
            _map.Build(dataSource);
            _rects.Clear();

            float offset = config.IsVertical ? config.Padding.top : config.Padding.left;
            _crossSize = config.IsVertical
                ? config.ViewportCrossSize - config.Padding.left - config.Padding.right
                : config.ViewportCrossSize - config.Padding.top - config.Padding.bottom;

            float crossStart = config.IsVertical ? config.Padding.left : config.Padding.top;

            for (int i = 0; i < _map.Count; i++)
            {
                var info = _map.Get(i);
                float size = GetElementSize(dataSource, info);

                _rects.Add(new ElementRect
                {
                    Position = offset,
                    Size = size,
                    CrossPosition = crossStart,
                    CrossSize = _crossSize
                });

                offset += size;
                if (i < _map.Count - 1)
                    offset += config.Spacing;
            }

            float endPad = config.IsVertical ? config.Padding.bottom : config.Padding.right;
            _contentSize = offset + endPad;
        }

        public float GetContentSize() => _contentSize;
        public float GetCrossSize() => _crossSize;

        public VisibleRange GetVisibleRange(float scrollPosition, float viewportSize)
        {
            if (_rects.Count == 0) return VisibleRange.Empty;

            var range = VisibleRange.Empty;
            float viewEnd = scrollPosition + viewportSize;

            // Binary search for first visible
            int lo = 0, hi = _rects.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (_rects[mid].Position + _rects[mid].Size < scrollPosition)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }
            range.First = lo;

            // Linear scan for last visible (usually only a screenful)
            for (int i = lo; i < _rects.Count; i++)
            {
                if (_rects[i].Position > viewEnd)
                    break;
                range.Last = i;
            }

            if (range.Last < range.First)
                return VisibleRange.Empty;

            return range;
        }

        public ElementRect GetElementRect(int flatIndex) => _rects[flatIndex];

        private static float GetElementSize(IScrollDataSource ds, ElementInfo info)
        {
            return info.Type switch
            {
                ElementInfo.ElementType.Header => ds.GetHeaderSize(info.Section),
                ElementInfo.ElementType.Footer => ds.GetFooterSize(info.Section),
                _ => ds.GetItemSize(info.Section, info.Index)
            };
        }
    }
}

using System.Collections.Generic;
using CustomScrollView.Core;
using CustomScrollView.Enums;
using CustomScrollView.Interfaces;

namespace CustomScrollView.Layout
{
    /// <summary>
    /// Layout strategy for grid mode.
    /// Headers/footers span the full cross-axis; items are arranged in rows/columns
    /// based on ConstraintCount.
    /// </summary>
    public sealed class GridLayoutStrategy : ILayoutStrategy
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

            int lanes = config.ConstraintCount > 0 ? config.ConstraintCount : 1;

            float mainPadStart = config.IsVertical ? config.Padding.top : config.Padding.left;
            float mainPadEnd   = config.IsVertical ? config.Padding.bottom : config.Padding.right;
            float crossPadStart = config.IsVertical ? config.Padding.left : config.Padding.top;
            float crossPadEnd   = config.IsVertical ? config.Padding.right : config.Padding.bottom;

            float totalCross = config.ViewportCrossSize - crossPadStart - crossPadEnd;
            float cellCross = (totalCross - (lanes - 1) * config.GridCrossSpacing) / lanes;
            _crossSize = totalCross;

            float offset = mainPadStart;
            int sectionCount = dataSource.GetSectionCount();
            int flatIdx = 0;

            for (int s = 0; s < sectionCount; s++)
            {
                // ── Header ──
                float headerSize = dataSource.GetHeaderSize(s);
                if (headerSize > 0f)
                {
                    _rects.Add(new ElementRect
                    {
                        Position = offset,
                        Size = headerSize,
                        CrossPosition = crossPadStart,
                        CrossSize = totalCross
                    });
                    offset += headerSize + config.Spacing;
                    flatIdx++;
                }

                // ── Items in grid rows ──
                int itemCount = dataSource.GetItemCount(s);
                int itemIdx = 0;
                while (itemIdx < itemCount)
                {
                    float rowMaxSize = 0f;
                    int rowCount = System.Math.Min(lanes, itemCount - itemIdx);

                    // First pass: measure max size in this row
                    for (int col = 0; col < rowCount; col++)
                    {
                        float sz = dataSource.GetItemSize(s, itemIdx + col);
                        if (sz > rowMaxSize) rowMaxSize = sz;
                    }

                    // Second pass: place cells
                    for (int col = 0; col < rowCount; col++)
                    {
                        float sz = dataSource.GetItemSize(s, itemIdx + col);
                        float crossPos = crossPadStart + col * (cellCross + config.GridCrossSpacing);

                        _rects.Add(new ElementRect
                        {
                            Position = offset,
                            Size = sz,
                            CrossPosition = crossPos,
                            CrossSize = cellCross
                        });
                        flatIdx++;
                    }

                    offset += rowMaxSize;
                    itemIdx += rowCount;

                    if (itemIdx < itemCount)
                        offset += config.Spacing;
                }

                // ── Footer ──
                float footerSize = dataSource.GetFooterSize(s);
                if (footerSize > 0f)
                {
                    if (itemCount > 0) offset += config.Spacing;
                    _rects.Add(new ElementRect
                    {
                        Position = offset,
                        Size = footerSize,
                        CrossPosition = crossPadStart,
                        CrossSize = totalCross
                    });
                    offset += footerSize;
                    flatIdx++;
                }

                // Section spacing
                if (s < sectionCount - 1)
                    offset += config.Spacing;
            }

            _contentSize = offset + mainPadEnd;
        }

        public float GetContentSize() => _contentSize;
        public float GetCrossSize() => _crossSize;

        public VisibleRange GetVisibleRange(float scrollPosition, float viewportSize)
        {
            if (_rects.Count == 0) return VisibleRange.Empty;

            var range = VisibleRange.Empty;
            float viewEnd = scrollPosition + viewportSize;

            // Find first visible (binary search on position)
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

            for (int i = lo; i < _rects.Count; i++)
            {
                if (_rects[i].Position > viewEnd)
                    break;
                range.Last = i;
            }

            return range.Last < range.First ? VisibleRange.Empty : range;
        }

        public ElementRect GetElementRect(int flatIndex) => _rects[flatIndex];
    }
}

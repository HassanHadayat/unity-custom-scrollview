using System.Collections.Generic;
using CustomScrollView.Core;
using CustomScrollView.Enums;
using CustomScrollView.Interfaces;

namespace CustomScrollView.Layout
{
    /// <summary>
    /// Layout strategy that supports mixed per-section layouts (linear and grid).
    /// Replaces LinearLayoutStrategy and GridLayoutStrategy in the factory.
    /// Sections without an ISectionLayoutProvider descriptor fall back to the global ScrollViewConfig.
    /// </summary>
    public sealed class CompositeLayoutStrategy : ILayoutStrategy
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

            var layoutProvider = dataSource as ISectionLayoutProvider;

            float mainPadStart  = config.IsVertical ? config.Padding.top    : config.Padding.left;
            float mainPadEnd    = config.IsVertical ? config.Padding.bottom  : config.Padding.right;
            float crossPadStart = config.IsVertical ? config.Padding.left    : config.Padding.top;
            float crossPadEnd   = config.IsVertical ? config.Padding.right   : config.Padding.bottom;

            float totalCross = config.ViewportCrossSize - crossPadStart - crossPadEnd;
            _crossSize = totalCross;

            float offset = mainPadStart;
            int sectionCount = dataSource.GetSectionCount();

            for (int s = 0; s < sectionCount; s++)
            {
                // Resolve layout for this section
                SectionLayoutDescriptor desc = default;
                bool hasOverride = layoutProvider != null && layoutProvider.TryGetSectionLayout(s, out desc);
                GridConstraint constraint = hasOverride && desc.OverrideConstraint ? desc.Constraint : config.Constraint;
                int lanes      = hasOverride && desc.LaneCount > 0 ? desc.LaneCount : config.ConstraintCount;
                float spacing  = hasOverride && desc.Spacing > 0f  ? desc.Spacing   : config.Spacing;
                float crossGap = hasOverride && desc.CrossSpacing > 0f ? desc.CrossSpacing : config.GridCrossSpacing;

                bool isGrid = constraint == GridConstraint.FixedColumnCount
                           || constraint == GridConstraint.FixedRowCount;

                if (lanes < 1) lanes = 1;

                float cellCross = isGrid
                    ? (totalCross - (lanes - 1) * crossGap) / lanes
                    : totalCross;

                // ── Header ──
                float headerSize = dataSource.GetHeaderSize(s);
                if (headerSize > 0f)
                {
                    _rects.Add(new ElementRect
                    {
                        Position      = offset,
                        Size          = headerSize,
                        CrossPosition = crossPadStart,
                        CrossSize     = totalCross
                    });
                    offset += headerSize + spacing;
                }

                // ── Items ──
                int itemCount = dataSource.GetItemCount(s);
                if (isGrid)
                {
                    int itemIdx = 0;
                    while (itemIdx < itemCount)
                    {
                        int rowCount = System.Math.Min(lanes, itemCount - itemIdx);
                        float rowMaxSize = 0f;

                        for (int col = 0; col < rowCount; col++)
                        {
                            float sz = dataSource.GetItemSize(s, itemIdx + col);
                            if (sz > rowMaxSize) rowMaxSize = sz;
                        }

                        for (int col = 0; col < rowCount; col++)
                        {
                            float sz = dataSource.GetItemSize(s, itemIdx + col);
                            float crossPos = crossPadStart + col * (cellCross + crossGap);
                            _rects.Add(new ElementRect
                            {
                                Position      = offset,
                                Size          = sz,
                                CrossPosition = crossPos,
                                CrossSize     = cellCross
                            });
                        }

                        offset += rowMaxSize;
                        itemIdx += rowCount;
                        if (itemIdx < itemCount) offset += spacing;
                    }
                }
                else
                {
                    for (int i = 0; i < itemCount; i++)
                    {
                        float sz = dataSource.GetItemSize(s, i);
                        _rects.Add(new ElementRect
                        {
                            Position      = offset,
                            Size          = sz,
                            CrossPosition = crossPadStart,
                            CrossSize     = totalCross
                        });
                        offset += sz;
                        if (i < itemCount - 1) offset += spacing;
                    }
                }

                // ── Footer ──
                float footerSize = dataSource.GetFooterSize(s);
                if (footerSize > 0f)
                {
                    if (itemCount > 0) offset += spacing;
                    _rects.Add(new ElementRect
                    {
                        Position      = offset,
                        Size          = footerSize,
                        CrossPosition = crossPadStart,
                        CrossSize     = totalCross
                    });
                    offset += footerSize;
                }

                if (s < sectionCount - 1) offset += spacing;
            }

            _contentSize = offset + mainPadEnd;
        }

        public float GetContentSize() => _contentSize;
        public float GetCrossSize()   => _crossSize;

        public VisibleRange GetVisibleRange(float scrollPosition, float viewportSize)
        {
            if (_rects.Count == 0) return VisibleRange.Empty;

            var range = VisibleRange.Empty;
            float viewEnd = scrollPosition + viewportSize;

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
                if (_rects[i].Position > viewEnd) break;
                range.Last = i;
            }

            return range.Last < range.First ? VisibleRange.Empty : range;
        }

        public ElementRect GetElementRect(int flatIndex) => _rects[flatIndex];
    }
}

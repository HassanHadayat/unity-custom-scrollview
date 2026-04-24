using System.Collections.Generic;
using CustomScrollView.Interfaces;

namespace CustomScrollView.Core
{
    /// <summary>
    /// Builds a flat element list from sections (header + items + footer)
    /// and provides O(1) lookup in both directions.
    /// </summary>
    public sealed class ElementMap
    {
        private readonly List<ElementInfo> _elements = new();
        private readonly List<int> _itemBase = new();   // flat index of item 0 in each section
        private readonly List<int> _itemCount = new();  // number of items in each section

        public int Count => _elements.Count;

        public void Build(IScrollDataSource dataSource)
        {
            _elements.Clear();
            _itemBase.Clear();
            _itemCount.Clear();

            int sectionCount = dataSource.GetSectionCount();

            int total = 0;
            for (int s = 0; s < sectionCount; s++)
            {
                if (dataSource.GetHeaderSize(s) > 0f) total++;
                total += dataSource.GetItemCount(s);
                if (dataSource.GetFooterSize(s) > 0f) total++;
            }
            if (_elements.Capacity < total) _elements.Capacity = total;
            if (_itemBase.Capacity < sectionCount) _itemBase.Capacity = sectionCount;
            if (_itemCount.Capacity < sectionCount) _itemCount.Capacity = sectionCount;

            int flat = 0;
            for (int s = 0; s < sectionCount; s++)
            {
                if (dataSource.GetHeaderSize(s) > 0f)
                {
                    _elements.Add(new ElementInfo
                    {
                        Type = ElementInfo.ElementType.Header,
                        Section = s,
                        Index = -1
                    });
                    flat++;
                }

                int itemCount = dataSource.GetItemCount(s);
                _itemBase.Add(flat);
                _itemCount.Add(itemCount);

                for (int i = 0; i < itemCount; i++)
                {
                    _elements.Add(new ElementInfo
                    {
                        Type = ElementInfo.ElementType.Item,
                        Section = s,
                        Index = i
                    });
                    flat++;
                }

                if (dataSource.GetFooterSize(s) > 0f)
                {
                    _elements.Add(new ElementInfo
                    {
                        Type = ElementInfo.ElementType.Footer,
                        Section = s,
                        Index = -1
                    });
                    flat++;
                }
            }
        }

        public ElementInfo Get(int flatIndex) => _elements[flatIndex];

        /// <summary>
        /// Find flat index for a given section + item index. Returns -1 if out of range.
        /// </summary>
        public int FindFlatIndex(int section, int itemIndex)
        {
            if ((uint)section >= (uint)_itemBase.Count) return -1;
            if ((uint)itemIndex >= (uint)_itemCount[section]) return -1;
            return _itemBase[section] + itemIndex;
        }
    }
}
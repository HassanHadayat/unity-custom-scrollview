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

        public int Count => _elements.Count;

        public void Build(IScrollDataSource dataSource)
        {
            _elements.Clear();

            int sectionCount = dataSource.GetSectionCount();
            for (int s = 0; s < sectionCount; s++)
            {
                // Header
                if (dataSource.GetHeaderSize(s) > 0f)
                {
                    _elements.Add(new ElementInfo
                    {
                        Type = ElementInfo.ElementType.Header,
                        Section = s,
                        Index = -1
                    });
                }

                // Items
                int itemCount = dataSource.GetItemCount(s);
                for (int i = 0; i < itemCount; i++)
                {
                    _elements.Add(new ElementInfo
                    {
                        Type = ElementInfo.ElementType.Item,
                        Section = s,
                        Index = i
                    });
                }

                // Footer
                if (dataSource.GetFooterSize(s) > 0f)
                {
                    _elements.Add(new ElementInfo
                    {
                        Type = ElementInfo.ElementType.Footer,
                        Section = s,
                        Index = -1
                    });
                }
            }
        }

        public ElementInfo Get(int flatIndex) => _elements[flatIndex];

        /// <summary>
        /// Find flat index for a given section + item index.
        /// Returns -1 if not found.
        /// </summary>
        public int FindFlatIndex(int section, int itemIndex)
        {
            for (int i = 0; i < _elements.Count; i++)
            {
                var e = _elements[i];
                if (e.Section == section && e.Type == ElementInfo.ElementType.Item && e.Index == itemIndex)
                    return i;
            }
            return -1;
        }
    }
}

using System;
using System.Collections.Generic;
using CustomScrollView.Interfaces;

namespace CustomScrollView.Data
{
    /// <summary>
    /// Convenient data source you can populate with sections and items
    /// without implementing the interface yourself.
    /// </summary>
    public sealed class SimpleDataSource : IScrollDataSource, ISectionLayoutProvider
    {
        private readonly List<SectionData> _sections = new();

        /// <summary>
        /// Add a section. Returns the section index for further configuration.
        /// Pass a <paramref name="layout"/> descriptor to override the global layout for this section.
        /// </summary>
        public int AddSection(float headerSize = 0f, float footerSize = 0f, SectionLayoutDescriptor? layout = null)
        {
            _sections.Add(new SectionData
            {
                HeaderSize = headerSize,
                FooterSize = footerSize,
                Items = new List<float>(),
                Layout = layout
            });
            return _sections.Count - 1;
        }

        /// <summary>
        /// Add items to a section. Each float is the size along the scroll axis.
        /// </summary>
        public void AddItems(int section, IEnumerable<float> sizes)
        {
            _sections[section].Items.AddRange(sizes);
        }

        /// <summary>
        /// Add N items of uniform size to a section.
        /// </summary>
        public void AddItems(int section, int count, float uniformSize)
        {
            for (int i = 0; i < count; i++)
                _sections[section].Items.Add(uniformSize);
        }

        /// <summary>
        /// Clear all sections and items.
        /// </summary>
        public void Clear()
        {
            _sections.Clear();
        }

        // ── IScrollDataSource ─────────────────────────────────────

        public int GetSectionCount() => _sections.Count;
        public int GetItemCount(int section) => _sections[section].Items.Count;
        public float GetItemSize(int section, int index) => _sections[section].Items[index];
        public float GetHeaderSize(int section) => _sections[section].HeaderSize;
        public float GetFooterSize(int section) => _sections[section].FooterSize;

        // ── ISectionLayoutProvider ────────────────────────────────

        public bool TryGetSectionLayout(int section, out SectionLayoutDescriptor descriptor)
        {
            var layout = _sections[section].Layout;
            if (layout.HasValue)
            {
                descriptor = layout.Value;
                return true;
            }
            descriptor = default;
            return false;
        }

        private class SectionData
        {
            public float HeaderSize;
            public float FooterSize;
            public List<float> Items;
            public SectionLayoutDescriptor? Layout;
        }
    }

    /// <summary>
    /// Delegate-based data source for maximum flexibility with minimal boilerplate.
    /// </summary>
    public sealed class DelegateDataSource : IScrollDataSource, ISectionLayoutProvider
    {
        public Func<int> SectionCount;
        public Func<int, int> ItemCount;
        public Func<int, int, float> ItemSize;
        public Func<int, float> HeaderSize;
        public Func<int, float> FooterSize;
        /// <summary>Optional. Return null to use global layout for that section.</summary>
        public Func<int, SectionLayoutDescriptor?> SectionLayout;

        public int GetSectionCount() => SectionCount?.Invoke() ?? 1;
        public int GetItemCount(int section) => ItemCount?.Invoke(section) ?? 0;
        public float GetItemSize(int section, int index) => ItemSize?.Invoke(section, index) ?? 100f;
        public float GetHeaderSize(int section) => HeaderSize?.Invoke(section) ?? 0f;
        public float GetFooterSize(int section) => FooterSize?.Invoke(section) ?? 0f;

        public bool TryGetSectionLayout(int section, out SectionLayoutDescriptor descriptor)
        {
            var layout = SectionLayout?.Invoke(section);
            if (layout.HasValue)
            {
                descriptor = layout.Value;
                return true;
            }
            descriptor = default;
            return false;
        }
    }
}

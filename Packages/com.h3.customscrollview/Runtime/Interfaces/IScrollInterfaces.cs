using UnityEngine;
using CustomScrollView;

namespace CustomScrollView.Interfaces
{
    // ── Data source (Model) ────────────────────────────────────────

    /// <summary>
    /// Provides section/item counts and sizes to the scroll view.
    /// Implement this to feed data without coupling to a concrete model.
    /// </summary>
    public interface IScrollDataSource
    {
        /// <summary>Number of sections.</summary>
        int GetSectionCount();

        /// <summary>Number of items in a section.</summary>
        int GetItemCount(int section);

        /// <summary>
        /// Size of an item along the scroll axis (height for vertical, width for horizontal).
        /// </summary>
        float GetItemSize(int section, int index);

        /// <summary>
        /// Size of the section header along the scroll axis. Return 0 for no header.
        /// </summary>
        float GetHeaderSize(int section);

        /// <summary>
        /// Size of the section footer along the scroll axis. Return 0 for no footer.
        /// </summary>
        float GetFooterSize(int section);
    }

    // ── Cell lifecycle (View) ──────────────────────────────────────

    /// <summary>
    /// Creates and recycles cell GameObjects.
    /// </summary>
    public interface ICellProvider
    {
        /// <summary>
        /// Return (or instantiate) a cell for the given section + index.
        /// The provider owns pool-key selection.
        /// </summary>
        GameObject GetCell(int section, int index, Transform parent);

        /// <summary>
        /// Return (or instantiate) a header for the given section.
        /// Return null if no header.
        /// </summary>
        GameObject GetHeader(int section, Transform parent);

        /// <summary>
        /// Return (or instantiate) a footer for the given section.
        /// Return null if no footer.
        /// </summary>
        GameObject GetFooter(int section, Transform parent);

        /// <summary>
        /// Called when a cell scrolls off-screen and should be returned to the pool.
        /// </summary>
        void RecycleCell(GameObject cell);

        /// <summary>
        /// Called when a header scrolls off-screen.
        /// </summary>
        void RecycleHeader(GameObject header);

        /// <summary>
        /// Called when a footer scrolls off-screen.
        /// </summary>
        void RecycleFooter(GameObject footer);
    }

    /// <summary>
    /// Implement on a cell MonoBehaviour to receive fill data callbacks.
    /// </summary>
    public interface IScrollCell
    {
        void OnFill(int section, int index);
    }

    /// <summary>
    /// Optional — implement on header/footer to receive section index.
    /// </summary>
    public interface IScrollSectionElement
    {
        void OnFill(int section);
    }

    // ── Per-section layout ─────────────────────────────────────────

    /// <summary>
    /// Optionally implement alongside IScrollDataSource to supply per-section layout overrides.
    /// Sections that return false fall back to the global ScrollViewConfig.
    /// </summary>
    public interface ISectionLayoutProvider
    {
        bool TryGetSectionLayout(int section, out SectionLayoutDescriptor descriptor);
    }

    // ── Layout strategy ────────────────────────────────────────────

    /// <summary>
    /// Calculates positions and visible range for a given layout mode.
    /// </summary>
    public interface ILayoutStrategy
    {
        /// <summary>
        /// Build internal layout data from the data source.
        /// Called once on init / reload.
        /// </summary>
        void Build(IScrollDataSource dataSource, ScrollViewConfig config);

        /// <summary>Total content size along the scroll axis.</summary>
        float GetContentSize();

        /// <summary>Total content size on the cross axis (for grid).</summary>
        float GetCrossSize();

        /// <summary>
        /// Return the range of visible element indices for the current scroll position + viewport size.
        /// </summary>
        VisibleRange GetVisibleRange(float scrollPosition, float viewportSize);

        /// <summary>
        /// Get the rect (position + size) of an element by its flat index.
        /// </summary>
        ElementRect GetElementRect(int flatIndex);

        /// <summary>
        /// Total number of elements (items + headers + footers across all sections).
        /// </summary>
        int TotalElementCount { get; }
    }

    // ── Scroll view controller ─────────────────────────────────────

    /// <summary>
    /// Public API exposed to user code.
    /// </summary>
    public interface IScrollViewController
    {
        void ReloadData();
        void ScrollTo(int section, int index, float duration = 0.3f);
        void ScrollToTop(float duration = 0.3f);
        void ScrollToBottom(float duration = 0.3f);
    }
}

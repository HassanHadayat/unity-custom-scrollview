using CustomScrollView.Interfaces;

namespace CustomScrollView.Layout
{
    /// <summary>
    /// Creates the layout strategy for the scroll view.
    /// Currently always a CompositeLayoutStrategy, which handles linear and grid
    /// sections (including mixed per-section layouts via ISectionLayoutProvider).
    /// </summary>
    public static class LayoutStrategyFactory
    {
        public static ILayoutStrategy Create() => new CompositeLayoutStrategy();
    }
}
using CustomScrollView.Enums;
using CustomScrollView.Interfaces;

namespace CustomScrollView.Layout
{
    /// <summary>
    /// Creates the layout strategy for the scroll view.
    /// Always returns a CompositeLayoutStrategy, which handles both linear and grid
    /// sections (including mixed per-section layouts via ISectionLayoutProvider).
    /// </summary>
    public static class LayoutStrategyFactory
    {
        public static ILayoutStrategy Create(GridConstraint constraint)
        {
            return new CompositeLayoutStrategy();
        }
    }
}

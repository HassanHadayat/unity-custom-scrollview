using CustomScrollView.Enums;
using CustomScrollView.Interfaces;

namespace CustomScrollView.Layout
{
    /// <summary>
    /// Creates the correct ILayoutStrategy based on the scroll view configuration.
    /// Single Responsibility: strategy selection lives here, not in the controller.
    /// </summary>
    public static class LayoutStrategyFactory
    {
        public static ILayoutStrategy Create(GridConstraint constraint)
        {
            return constraint switch
            {
                GridConstraint.FixedColumnCount => new GridLayoutStrategy(),
                GridConstraint.FixedRowCount    => new GridLayoutStrategy(),
                _                               => new LinearLayoutStrategy()
            };
        }
    }
}

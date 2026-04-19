namespace CustomScrollView.Enums
{
    /// <summary>
    /// Scroll direction for the scroll view.
    /// </summary>
    public enum ScrollDirection
    {
        Vertical,
        Horizontal
    }

    /// <summary>
    /// Grid constraint mode.
    /// </summary>
    public enum GridConstraint
    {
        /// <summary>No grid — single row/column list.</summary>
        None,
        /// <summary>Fixed number of columns (vertical scroll).</summary>
        FixedColumnCount,
        /// <summary>Fixed number of rows (horizontal scroll).</summary>
        FixedRowCount
    }

    /// <summary>
    /// Snap behaviour after scroll momentum ends.
    /// </summary>
    public enum SnapMode
    {
        None,
        SnapToItem,
        SnapToSection
    }
}

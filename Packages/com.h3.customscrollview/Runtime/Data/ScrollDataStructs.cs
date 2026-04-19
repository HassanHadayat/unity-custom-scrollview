using UnityEngine;
using CustomScrollView.Enums;

namespace CustomScrollView
{
    /// <summary>
    /// Immutable config snapshot consumed by layout strategies.
    /// </summary>
    public readonly struct ScrollViewConfig
    {
        public readonly ScrollDirection Direction;
        public readonly GridConstraint Constraint;
        public readonly int ConstraintCount;
        public readonly float Spacing;
        public readonly float GridCrossSpacing;
        public readonly RectOffset Padding;
        public readonly float ViewportMainSize;
        public readonly float ViewportCrossSize;

        public ScrollViewConfig(
            ScrollDirection direction,
            GridConstraint constraint,
            int constraintCount,
            float spacing,
            float gridCrossSpacing,
            RectOffset padding,
            float viewportMainSize,
            float viewportCrossSize)
        {
            Direction = direction;
            Constraint = constraint;
            ConstraintCount = constraintCount;
            Spacing = spacing;
            GridCrossSpacing = gridCrossSpacing;
            Padding = padding;
            ViewportMainSize = viewportMainSize;
            ViewportCrossSize = viewportCrossSize;
        }

        public bool IsVertical => Direction == ScrollDirection.Vertical;
    }

    /// <summary>
    /// Inclusive range of flat element indices that should be visible.
    /// </summary>
    public struct VisibleRange
    {
        public int First;
        public int Last;

        public static VisibleRange Empty => new VisibleRange { First = -1, Last = -1 };
        public bool IsEmpty => First < 0 || Last < 0 || Last < First;
    }

    /// <summary>
    /// Position and size of a single element in the layout.
    /// </summary>
    public struct ElementRect
    {
        /// <summary>Offset along the scroll axis from content origin.</summary>
        public float Position;
        /// <summary>Size along the scroll axis.</summary>
        public float Size;
        /// <summary>Offset on the cross axis (relevant for grid).</summary>
        public float CrossPosition;
        /// <summary>Size on the cross axis.</summary>
        public float CrossSize;
    }

    /// <summary>
    /// Maps a flat element index back to section/index and element type.
    /// </summary>
    public struct ElementInfo
    {
        public ElementType Type;
        public int Section;
        public int Index; // only meaningful for Item type

        public enum ElementType
        {
            Header,
            Item,
            Footer
        }
    }
}

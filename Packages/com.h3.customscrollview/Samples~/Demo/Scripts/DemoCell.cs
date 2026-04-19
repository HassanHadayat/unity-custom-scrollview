using CustomScrollView.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace CustomScrollView.Demo
{
    /// <summary>
    /// Simple demo cell that shows section/index info and alternating colors.
    /// Attach to your cell prefab.
    /// </summary>
    public class DemoCell : MonoBehaviour, IScrollCell
    {
        [SerializeField] private Text _label;
        [SerializeField] private Image _background;

        private static readonly Color[] SectionColors = new[]
        {
            new Color(0.85f, 0.92f, 1.00f), // light blue
            new Color(0.85f, 1.00f, 0.88f), // light green
            new Color(1.00f, 0.92f, 0.85f), // light orange
            new Color(0.95f, 0.85f, 1.00f), // light purple
            new Color(1.00f, 0.85f, 0.90f), // light pink
        };

        public void OnFill(int section, int index)
        {
            if (_label != null)
                _label.text = $"Section {section}  —  Item {index}";

            if (_background != null)
            {
                var baseColor = SectionColors[section % SectionColors.Length];
                // Alternate row tint
                _background.color = index % 2 == 0
                    ? baseColor
                    : Color.Lerp(baseColor, Color.white, 0.4f);
            }
        }
    }
}

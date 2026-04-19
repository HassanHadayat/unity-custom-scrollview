using CustomScrollView.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace CustomScrollView.Demo
{
    /// <summary>
    /// Section header that shows section title with a colored bar.
    /// Attach to your header prefab.
    /// </summary>
    public class DemoHeader : MonoBehaviour, IScrollSectionElement
    {
        [SerializeField] private Text _label;
        [SerializeField] private Image _background;

        private static readonly string[] SectionNames = new[]
        {
            "Fruits", "Vegetables", "Dairy", "Bakery", "Beverages",
            "Snacks", "Frozen", "Meat", "Seafood", "Condiments"
        };

        public void OnFill(int section)
        {
            if (_label != null)
            {
                string name = section < SectionNames.Length
                    ? SectionNames[section]
                    : $"Section {section}";
                _label.text = name;
            }

            if (_background != null)
                _background.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        }
    }
}

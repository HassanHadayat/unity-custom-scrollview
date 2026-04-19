using CustomScrollView.Controller;
using CustomScrollView.Data;
using UnityEngine;
using UnityEngine.UI;

namespace CustomScrollView.Demo
{
    /// <summary>
    /// Demo 6: Sections with headers in a grid layout.
    /// Headers span full width, items arranged in grid columns.
    /// </summary>
    public class Demo06_SectionGrid : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScrollViewController _scrollView;
        [SerializeField] private GameObject _cellPrefab;
        [SerializeField] private GameObject _headerPrefab;

        [Header("Settings")]
        [SerializeField] private int _sectionCount = 4;
        [SerializeField] private int _itemsPerSection = 18;
        [SerializeField] private float _itemHeight = 100f;
        [SerializeField] private float _headerHeight = 50f;

        private void Start()
        {
            var ds = new SimpleDataSource();

            for (int s = 0; s < _sectionCount; s++)
            {
                int section = ds.AddSection(headerSize: _headerHeight);
                ds.AddItems(section, _itemsPerSection, _itemHeight);
            }

            _scrollView.Initialize(
                ds,
                cellPrefabSelector: (s, i) => _cellPrefab,
                headerPrefabSelector: (s) => _headerPrefab
            );
            
            _scrollView.OnCellVisible += (s, index, cell) =>
            {
                cell.GetComponentInChildren<Text>().text = $"S:{s},C:{index}";
            };
        }
    }
}

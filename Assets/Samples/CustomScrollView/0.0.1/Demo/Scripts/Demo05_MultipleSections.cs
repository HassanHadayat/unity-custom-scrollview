using CustomScrollView.Controller;
using CustomScrollView.Data;
using UnityEngine;

namespace CustomScrollView.Demo
{
    /// <summary>
    /// Demo 5: Multiple sections with headers — vertical list.
    /// </summary>
    public class Demo05_MultipleSections : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScrollViewController _scrollView;
        [SerializeField] private GameObject _cellPrefab;
        [SerializeField] private GameObject _headerPrefab;

        [Header("Settings")]
        [SerializeField] private int _sectionCount = 5;
        [SerializeField] private int _itemsPerSection = 15;
        [SerializeField] private float _itemHeight = 70f;
        [SerializeField] private float _headerHeight = 50f;

        private void Start()
        {
            var ds = new SimpleDataSource();

            for (int s = 0; s < _sectionCount; s++)
            {
                int section = ds.AddSection();
                ds.AddItems(section, _itemsPerSection, _itemHeight);
            }

            _scrollView.Initialize(
                ds,
                cellPrefabSelector: (s, i) => _cellPrefab,
                headerPrefabSelector: (s) => _headerPrefab
            );
        }
    }
}

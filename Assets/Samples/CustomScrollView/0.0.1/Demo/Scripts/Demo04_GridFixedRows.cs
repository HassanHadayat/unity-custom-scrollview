using CustomScrollView.Controller;
using CustomScrollView.Data;
using UnityEngine;

namespace CustomScrollView.Demo
{
    /// <summary>
    /// Demo 4: Grid layout with fixed row count (horizontal scroll).
    /// </summary>
    public class Demo04_GridFixedRows : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScrollViewController _scrollView;
        [SerializeField] private GameObject _cellPrefab;

        [Header("Settings")]
        [SerializeField] private int _itemCount = 120;
        [SerializeField] private float _itemWidth = 140f;

        private void Start()
        {
            var ds = new SimpleDataSource();
            int section = ds.AddSection();
            ds.AddItems(section, _itemCount, _itemWidth);

            _scrollView.Initialize(ds, (s, i) => _cellPrefab);
        }
    }
}

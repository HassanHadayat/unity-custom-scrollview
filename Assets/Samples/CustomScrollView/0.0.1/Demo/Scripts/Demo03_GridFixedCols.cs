using CustomScrollView.Controller;
using CustomScrollView.Data;
using UnityEngine;
using UnityEngine.UI;

namespace CustomScrollView.Demo
{
    /// <summary>
    /// Demo 3: Grid layout with fixed column count (vertical scroll).
    /// </summary>
    public class Demo03_GridFixedCols : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private ScrollViewController _scrollView;
        [SerializeField] private GameObject _cellPrefab;

        [Header("Settings")]
        [SerializeField] private int _itemCount = 150;
        [SerializeField] private float _itemHeight = 120f;

        private void Start()
        {
            var ds = new SimpleDataSource();
            int section = ds.AddSection();
            ds.AddItems(section, _itemCount, _itemHeight);
            _scrollView.Initialize(ds, (s, i) => _cellPrefab);
        }
    }
}

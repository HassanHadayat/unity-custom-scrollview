using CustomScrollView.Controller;
using CustomScrollView.Data;
using UnityEngine;

namespace CustomScrollView.Demo
{
    /// <summary>
    /// Demo 2: Horizontal list — single section, uniform width.
    /// </summary>
    public class Demo02_HorizontalList : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScrollViewController _scrollView;
        [SerializeField] private GameObject _cellPrefab;

        [Header("Settings")]
        [SerializeField] private int _itemCount = 100;
        [SerializeField] private float _itemWidth = 200f;

        private void Start()
        {
            var ds = new SimpleDataSource();
            int section = ds.AddSection();
            ds.AddItems(section, _itemCount, _itemWidth);

            _scrollView.Initialize(ds, (s, i) => _cellPrefab);
        }
    }
}

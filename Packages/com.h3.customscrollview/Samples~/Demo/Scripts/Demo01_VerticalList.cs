using CustomScrollView.Controller;
using CustomScrollView.Data;
using UnityEngine;
using UnityEngine.UI;

namespace CustomScrollView.Demo
{
    /// <summary>
    /// Demo 1: Simple vertical list — single section, uniform height.
    /// </summary>
    public class Demo01_VerticalList : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScrollViewController _scrollView;
        [SerializeField] private GameObject _cellPrefab;

        [Header("Settings")]
        [SerializeField] private int _itemCount = 200;
        [SerializeField] private float _itemHeight = 80f;

        private void Start()
        {
            var ds = new SimpleDataSource();
            int section = ds.AddSection();
            ds.AddItems(section, _itemCount, _itemHeight);

            _scrollView.Initialize(ds, (s, i) => _cellPrefab);
            _scrollView.OnCellVisible += (s, index, cell) =>
            {
                cell.GetComponentInChildren<Text>().text = $"Sec: {s}, Item: {index}";
            };
        }
    }
}

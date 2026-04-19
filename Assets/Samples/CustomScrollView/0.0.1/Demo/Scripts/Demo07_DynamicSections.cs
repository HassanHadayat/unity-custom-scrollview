using CustomScrollView.Controller;
using CustomScrollView.Data;
using CustomScrollView.Enums;
using UnityEngine;

namespace CustomScrollView.Demo
{
    /// <summary>
    /// Demo 7: Dynamic Sections
    /// </summary>
    public class Demo07_DynamicSections : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScrollViewController _scrollView;
        [SerializeField] private GameObject _cellPrefab;
        [SerializeField] private GameObject _headerPrefab;
        
        [Header("References")]
        [SerializeField] private int _secLanes_1 = 2;
        [SerializeField] private int _secLanes_2 = 2;

        private void Start()
        {
            var ds = new SimpleDataSource();

            // Section 0: 1-column grid
            ds.AddSection(layout: new SectionLayoutDescriptor {
                OverrideConstraint = true,
                Constraint = GridConstraint.FixedRowCount,
                LaneCount = _secLanes_1,
                CrossSpacing = 500f
            });
            ds.AddItems(0, _secLanes_1, 500f);
            
            // Section 1: 3-column grid
            ds.AddSection(headerSize: 50f, layout: new SectionLayoutDescriptor {
                OverrideConstraint = true,
                Constraint = GridConstraint.FixedRowCount,
                LaneCount = _secLanes_2,
                CrossSpacing = 10f
            });
            ds.AddItems(1, 16, 400f);

            // Section 3: linear list
            ds.AddSection(headerSize: 50f, layout: new SectionLayoutDescriptor {
                OverrideConstraint = true,
                Constraint = GridConstraint.FixedRowCount,
                LaneCount = _secLanes_2,
                CrossSpacing = 10f
            });
            ds.AddItems(2, 8, 400f);


            _scrollView.Initialize(
                ds,
                cellPrefabSelector: (s, i) => _cellPrefab,
                headerPrefabSelector: (s) => _headerPrefab
            );
            
        }
    }
}

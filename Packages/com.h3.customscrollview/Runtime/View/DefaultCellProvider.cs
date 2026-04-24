using System;
using System.Collections.Generic;
using UnityEngine;
using CustomScrollView.Core;
using CustomScrollView.Interfaces;

namespace CustomScrollView.View
{
    /// <summary>
    /// Default cell provider that uses prefab instantiation + CellPool for recycling.
    /// Pool buckets are keyed by prefab instance ID, so sections sharing a prefab share a pool.
    /// </summary>
    public sealed class DefaultCellProvider : ICellProvider
    {
        private readonly CellPool _pool;
        private readonly Func<int, int, GameObject> _cellPrefabSelector;
        private readonly Func<int, GameObject> _headerPrefabSelector;
        private readonly Func<int, GameObject> _footerPrefabSelector;

        // cell GO instance ID → prefab instance ID (the pool bucket to return to)
        private readonly Dictionary<int, int> _poolKeyByInstance = new();

        public DefaultCellProvider(
            CellPool pool,
            Func<int, int, GameObject> cellPrefabSelector,
            Func<int, GameObject> headerPrefabSelector = null,
            Func<int, GameObject> footerPrefabSelector = null)
        {
            _pool = pool;
            _cellPrefabSelector = cellPrefabSelector;
            _headerPrefabSelector = headerPrefabSelector;
            _footerPrefabSelector = footerPrefabSelector;
        }

        public GameObject GetCell(int section, int index, Transform parent)
        {
            var prefab = _cellPrefabSelector(section, index);
            return GetFromPoolOrInstantiate(prefab, parent);
        }

        public GameObject GetHeader(int section, Transform parent)
        {
            if (_headerPrefabSelector == null) return null;
            var prefab = _headerPrefabSelector(section);
            if (prefab == null) return null;
            return GetFromPoolOrInstantiate(prefab, parent);
        }

        public GameObject GetFooter(int section, Transform parent)
        {
            if (_footerPrefabSelector == null) return null;
            var prefab = _footerPrefabSelector(section);
            if (prefab == null) return null;
            return GetFromPoolOrInstantiate(prefab, parent);
        }

        public void RecycleCell(GameObject cell)   => ReturnToPool(cell);
        public void RecycleHeader(GameObject go)   => ReturnToPool(go);
        public void RecycleFooter(GameObject go)   => ReturnToPool(go);

        private GameObject GetFromPoolOrInstantiate(GameObject prefab, Transform parent)
        {
            int key = prefab.GetInstanceID();
            var go = _pool.Dequeue(key);
            if (go == null)
            {
                go = UnityEngine.Object.Instantiate(prefab, parent, false);
                _poolKeyByInstance[go.GetInstanceID()] = key;
            }
            else
            {
                go.transform.SetParent(parent, false);
            }
            return go;
        }

        private void ReturnToPool(GameObject go)
        {
            if (go == null) return;
            if (!_poolKeyByInstance.TryGetValue(go.GetInstanceID(), out int key))
            {
                // Unknown origin — destroy rather than leak into a wrong bucket.
                UnityEngine.Object.Destroy(go);
                return;
            }
            _pool.Enqueue(key, go);
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace CustomScrollView.Core
{
    /// <summary>
    /// Generic object pool keyed by reuse identifier.
    /// Lightweight — no allocations on recycle.
    /// </summary>
    public sealed class CellPool
    {
        private readonly Dictionary<string, Queue<GameObject>> _pools = new();
        private readonly Transform _poolRoot;

        public CellPool(Transform poolRoot)
        {
            _poolRoot = poolRoot;
        }

        /// <summary>
        /// Try to dequeue a recycled cell. Returns null if pool is empty.
        /// </summary>
        public GameObject Dequeue(string reuseId)
        {
            if (_pools.TryGetValue(reuseId, out var queue) && queue.Count > 0)
            {
                var go = queue.Dequeue();
                go.SetActive(true);
                return go;
            }
            return null;
        }

        /// <summary>
        /// Return a cell to the pool.
        /// </summary>
        public void Enqueue(string reuseId, GameObject go)
        {
            if (!_pools.TryGetValue(reuseId, out var queue))
            {
                queue = new Queue<GameObject>();
                _pools[reuseId] = queue;
            }
            go.SetActive(false);
            go.transform.SetParent(_poolRoot, false);
            queue.Enqueue(go);
        }

        /// <summary>
        /// Destroy all pooled objects and clear.
        /// </summary>
        public void Clear()
        {
            foreach (var kvp in _pools)
            {
                foreach (var go in kvp.Value)
                {
                    if (go != null) Object.Destroy(go);
                }
            }
            _pools.Clear();
        }
    }
}

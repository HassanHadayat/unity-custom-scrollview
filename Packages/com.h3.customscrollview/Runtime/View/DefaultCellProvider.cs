using System;
using UnityEngine;
using CustomScrollView.Core;
using CustomScrollView.Interfaces;

namespace CustomScrollView.View
{
    /// <summary>
    /// Default cell provider that uses prefab instantiation + CellPool for recycling.
    /// Users can replace this with a custom ICellProvider for advanced scenarios.
    /// </summary>
    public sealed class DefaultCellProvider : ICellProvider
    {
        private readonly CellPool _pool;
        private readonly Func<int, int, GameObject> _cellPrefabSelector;
        private readonly Func<int, GameObject> _headerPrefabSelector;
        private readonly Func<int, GameObject> _footerPrefabSelector;

        private const string HeaderReusePrefix = "__header__";
        private const string FooterReusePrefix = "__footer__";

        /// <summary>
        /// Create a DefaultCellProvider.
        /// </summary>
        /// <param name="pool">Shared cell pool.</param>
        /// <param name="cellPrefabSelector">
        ///   (section, index) → prefab. The prefab name is used as the reuse ID.
        /// </param>
        /// <param name="headerPrefabSelector">(section) → prefab or null.</param>
        /// <param name="footerPrefabSelector">(section) → prefab or null.</param>
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

        public GameObject GetCell(int section, int index, string reuseId, Transform parent)
        {
            var go = _pool.Dequeue(reuseId);
            if (go == null)
            {
                var prefab = _cellPrefabSelector(section, index);
                go = UnityEngine.Object.Instantiate(prefab, parent, false);
            }
            else
            {
                go.transform.SetParent(parent, false);
            }
            return go;
        }

        public GameObject GetHeader(int section, Transform parent)
        {
            if (_headerPrefabSelector == null) return null;
            var prefab = _headerPrefabSelector(section);
            if (prefab == null) return null;

            string reuseId = HeaderReusePrefix + section;
            var go = _pool.Dequeue(reuseId);
            if (go == null)
            {
                go = UnityEngine.Object.Instantiate(prefab, parent, false);
            }
            else
            {
                go.transform.SetParent(parent, false);
            }
            return go;
        }

        public GameObject GetFooter(int section, Transform parent)
        {
            if (_footerPrefabSelector == null) return null;
            var prefab = _footerPrefabSelector(section);
            if (prefab == null) return null;

            string reuseId = FooterReusePrefix + section;
            var go = _pool.Dequeue(reuseId);
            if (go == null)
            {
                go = UnityEngine.Object.Instantiate(prefab, parent, false);
            }
            else
            {
                go.transform.SetParent(parent, false);
            }
            return go;
        }

        public void RecycleCell(GameObject cell, string reuseId)
        {
            _pool.Enqueue(reuseId, cell);
        }

        public void RecycleHeader(GameObject header, int section)
        {
            _pool.Enqueue(HeaderReusePrefix + section, header);
        }

        public void RecycleFooter(GameObject footer, int section)
        {
            _pool.Enqueue(FooterReusePrefix + section, footer);
        }
    }
}

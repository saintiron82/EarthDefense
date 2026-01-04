using System.Collections.Generic;
using UnityEngine;

namespace ShapeDefense.Scripts
{
    /// <summary>
    /// 2D 공간 분할 그리드 - 빠른 근접 객체 검색을 위한 Spatial Hash
    /// </summary>
    public class SpatialGrid<T> where T : class
    {
        private readonly Dictionary<Vector2Int, HashSet<T>> _grid = new();
        private readonly Dictionary<T, Vector2Int> _objectToCell = new();
        private readonly float _cellSize;

        public SpatialGrid(float cellSize = 5f)
        {
            _cellSize = Mathf.Max(0.1f, cellSize);
        }

        /// <summary>
        /// 월드 좌표를 그리드 셀 좌표로 변환
        /// </summary>
        private Vector2Int WorldToCell(Vector2 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / _cellSize),
                Mathf.FloorToInt(worldPos.y / _cellSize)
            );
        }

        /// <summary>
        /// 객체를 그리드에 등록
        /// </summary>
        public void Add(T obj, Vector2 worldPos)
        {
            if (obj == null) return;

            var cell = WorldToCell(worldPos);

            // 이미 등록되어 있다면 제거 후 재등록
            if (_objectToCell.ContainsKey(obj))
            {
                Remove(obj);
            }

            if (!_grid.ContainsKey(cell))
            {
                _grid[cell] = new HashSet<T>();
            }

            _grid[cell].Add(obj);
            _objectToCell[obj] = cell;
        }

        /// <summary>
        /// 객체를 그리드에서 제거
        /// </summary>
        public void Remove(T obj)
        {
            if (obj == null || !_objectToCell.TryGetValue(obj, out var cell)) return;

            if (_grid.TryGetValue(cell, out var set))
            {
                set.Remove(obj);
                if (set.Count == 0)
                {
                    _grid.Remove(cell);
                }
            }

            _objectToCell.Remove(obj);
        }

        /// <summary>
        /// 객체 위치 업데이트 (셀이 변경되었을 때만 재등록)
        /// </summary>
        public void UpdatePosition(T obj, Vector2 newWorldPos)
        {
            if (obj == null) return;

            var newCell = WorldToCell(newWorldPos);

            if (_objectToCell.TryGetValue(obj, out var oldCell))
            {
                if (oldCell == newCell) return; // 같은 셀 내 이동 - 업데이트 불필요

                // 셀 변경 - 재등록
                Remove(obj);
            }

            Add(obj, newWorldPos);
        }

        /// <summary>
        /// 특정 반경 내의 모든 객체를 리스트에 추가 (중복 제거 포함)
        /// </summary>
        public void QueryRadius(Vector2 center, float radius, List<T> results)
        {
            results.Clear();

            var radiusSq = radius * radius;
            var minCell = WorldToCell(center - Vector2.one * radius);
            var maxCell = WorldToCell(center + Vector2.one * radius);

            var visited = new HashSet<T>();

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (_grid.TryGetValue(cell, out var set))
                    {
                        foreach (var obj in set)
                        {
                            if (visited.Add(obj))
                            {
                                results.Add(obj);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 선분(from→to)과 교차하는 가능성이 있는 모든 셀의 객체를 반환
        /// </summary>
        public void QueryLine(Vector2 from, Vector2 to, List<T> results)
        {
            results.Clear();

            var cellFrom = WorldToCell(from);
            var cellTo = WorldToCell(to);

            var visited = new HashSet<T>();

            // 선분이 지나가는 모든 셀을 순회 (간단한 AABB 방식)
            var minX = Mathf.Min(cellFrom.x, cellTo.x);
            var maxX = Mathf.Max(cellFrom.x, cellTo.x);
            var minY = Mathf.Min(cellFrom.y, cellTo.y);
            var maxY = Mathf.Max(cellFrom.y, cellTo.y);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (_grid.TryGetValue(cell, out var set))
                    {
                        foreach (var obj in set)
                        {
                            if (visited.Add(obj))
                            {
                                results.Add(obj);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 그리드 전체 정리
        /// </summary>
        public void Clear()
        {
            _grid.Clear();
            _objectToCell.Clear();
        }

        /// <summary>
        /// null 객체 정리 (가비지 컬렉션)
        /// </summary>
        public void Prune()
        {
            var toRemove = new List<T>();

            foreach (var kvp in _objectToCell)
            {
                if (kvp.Key == null || (kvp.Key is Object unityObj && unityObj == null))
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var obj in toRemove)
            {
                Remove(obj);
            }
        }

        public int Count => _objectToCell.Count;
    }
}


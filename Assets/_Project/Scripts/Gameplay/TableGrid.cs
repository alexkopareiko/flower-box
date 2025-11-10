using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class TableGrid : MonoBehaviour
    {
        [SerializeField] private List<TableCell> _cells = new List<TableCell>();
        [SerializeField, Min(1)] private int _columns = 5;

        public List<TableCell> Cells => _cells;
        private readonly Dictionary<Vector2Int, TableCell> _lookup = new Dictionary<Vector2Int, TableCell>();

        public bool TryGetCell(Vector2Int coordinates, out TableCell cell) => _lookup.TryGetValue(coordinates, out cell);

        public void Initialize()
        {
            _lookup.Clear();
            SetupCoordsForCells();
        }

        private void SetupCoordsForCells()
        {
            int columns = Mathf.Max(1, _columns);

            for (int index = 0; index < _cells.Count; index++)
            {
                int x = index % columns;
                int y = index / columns;

                TableCell cell = _cells[index];
                if (cell == null)
                {
                    continue;
                }

                cell.name = $"Cell ({x}, {y})";
                cell.SetGridPosition(x, y);
                _lookup[cell.GridPosition] = cell;
            }
        }

        public bool CanPlace(TableCell cell)
        {
            if (cell == null)
            {
                return false;
            }

            if (!_lookup.TryGetValue(cell.GridPosition, out TableCell storedCell))
            {
                return false;
            }

            return !storedCell.IsOccupied;
        }

        public bool TryPlace(TableCell cell, GameObject content)
        {
            if (cell == null || content == null)
            {
                return false;
            }

            if (!_lookup.TryGetValue(cell.GridPosition, out TableCell storedCell))
            {
                return false;
            }

            return storedCell.TrySetContent(content);
        }

        public void Clear(TableCell cell, GameObject content)
        {
            if (cell == null || content == null)
            {
                return;
            }

            if (_lookup.TryGetValue(cell.GridPosition, out TableCell storedCell))
            {
                storedCell.ClearContent(content);
            }
        }
    }
}

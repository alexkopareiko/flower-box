using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class TableGrid : MonoBehaviour
    {
        [SerializeField] private List<TableCell> _cells = new List<TableCell>();

        public List<TableCell> Cells => _cells;

        public void Initialize()
        {
            SetupCoordsForCells();
        }

        private void SetupCoordsForCells()
        {
            for (int x = 0; x < _cells.Count; x++)
            {
                for (int y = 0; y < _cells.Count; y++)
                {
                    TableCell cell = _cells[y * _cells.Count + x];
                    if (cell != null)
                    {
                        cell.name = $"Cell ({x}, {y})";
                    }
                }
            }
        }
    }
}

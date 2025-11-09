using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class TableGrid : MonoBehaviour
    {
        [SerializeField] private List<TableCell> _cells = new List<TableCell>();
        [SerializeField, Min(1)] private int _columns = 5;

        public List<TableCell> Cells => _cells;

        public void Initialize()
        {
            Debug.Log("TableGrid Initialize");
            SetupCoordsForCells();
        }

        private void SetupCoordsForCells()
        {
            Debug.Log("SetupCoordsForCells");
            int columns = Mathf.Max(1, _columns);
            int rows = Mathf.CeilToInt(_cells.Count / (float)columns);

            for (int index = 0; index < _cells.Count; index++)
            {
                int x = index % columns;
                int y = index / columns;

                Debug.Log($"Setting up cell at: {x}, {y}");
                TableCell cell = _cells[index];
                if (cell == null)
                {
                    continue;
                }

                cell.name = $"Cell ({x}, {y})";
                cell.SetGridPosition(x, y);
            }
        }
    }
}

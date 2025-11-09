using UnityEngine;

namespace Game
{
    public class TableCell : MonoBehaviour
    {
        private Vector2Int _gridPosition;

        [SerializeField] private GameObject _content;

        public Vector2Int GridPosition => _gridPosition;
        public GameObject Content => _content;

        public void SetContent(GameObject content)
        {
            _content = content;
        }

        public void SetGridPosition(int x, int y)
        {
            _gridPosition = new Vector2Int(x, y);
        }
    }
}
using UnityEngine;

namespace Game
{
    [System.Serializable]
    public class TableCell : MonoBehaviour
    {
        private Vector2Int _gridPosition;

        [SerializeField] private GameObject _content;
        [SerializeField] private Transform _snapAnchor;

        public Vector2Int GridPosition => _gridPosition;
        public GameObject Content => _content;
        public bool IsOccupied => _content != null;
        public Transform SnapAnchor => _snapAnchor == null ? transform : _snapAnchor;

        public bool TrySetContent(GameObject content)
        {
            if (_content != null && _content != content)
            {
                return false;
            }

            _content = content;
            return true;
        }

        public void ClearContent(GameObject content)
        {
            if (_content == content)
            {
                _content = null;
            }
        }

        public Vector3 GetSnapPosition(float heightOffset = 0f)
        {
            return SnapAnchor.position + Vector3.up * heightOffset;
        }

        public void SetGridPosition(int x, int y)
        {
            _gridPosition = new Vector2Int(x, y);
        }
    }
}

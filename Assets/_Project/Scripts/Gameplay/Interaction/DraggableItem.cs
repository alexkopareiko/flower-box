using UnityEngine;

namespace Game
{
    /// <summary>
    /// Extends a PickableItem with grid snapping behaviour so it can be placed into TableCells.
    /// </summary>
    [RequireComponent(typeof(PickableItem))]
    public class DraggableItem : MonoBehaviour
    {
        [SerializeField] private TableGrid _gridOverride;
        [SerializeField] private LayerMask _cellMask = Physics.DefaultRaycastLayers;
        [SerializeField] private float _cellSnapHeightOffset = 0f;
        [SerializeField] private bool _attachToCellTransform = true;

        private PickableItem _pickable;
        private TableCell _committedCell;

        private TableGrid Grid => _gridOverride != null ? _gridOverride : GameManager.Instance != null ? GameManager.Instance.TableGrid : null;

        private void Awake()
        {
            _pickable = GetComponent<PickableItem>();
        }

        private void OnEnable()
        {
            Subscribe(true);
        }

        private void OnDisable()
        {
            Subscribe(false);
        }

        private void Subscribe(bool subscribe)
        {
            if (_pickable == null)
            {
                return;
            }

            if (subscribe)
            {
                _pickable.PickStarted += HandlePickStarted;
                _pickable.DropRequested += HandleDropRequested;
                _pickable.CancelRequested += HandleCancelRequested;
            }
            else
            {
                _pickable.PickStarted -= HandlePickStarted;
                _pickable.DropRequested -= HandleDropRequested;
                _pickable.CancelRequested -= HandleCancelRequested;
            }
        }

        private void HandlePickStarted(PickableItem pickable)
        {
            _committedCell = pickable.LastRestingCell;
            TableGrid grid = Grid;
            if (grid != null && _committedCell != null)
            {
                grid.Clear(_committedCell, gameObject);
            }
        }

        private void HandleDropRequested(PickableItem pickable)
        {
            TableGrid grid = Grid;
            if (grid == null)
            {
                pickable.RestoreRestingState();
                return;
            }

            TableCell targetCell;
            if (!TryGetHoveredCell(pickable, out targetCell))
            {
                pickable.RestoreRestingState();
                ReoccupyCommittedCell(grid);
                return;
            }

            if (!grid.CanPlace(targetCell) && targetCell != _committedCell)
            {
                pickable.RestoreRestingState();
                ReoccupyCommittedCell(grid);
                return;
            }

            SnapIntoCell(grid, targetCell);
        }

        private void HandleCancelRequested(PickableItem pickable)
        {
            TableGrid grid = Grid;
            if (grid == null || _committedCell == null)
            {
                return;
            }

            grid.TryPlace(_committedCell, gameObject);
        }

        private bool TryGetHoveredCell(PickableItem pickable, out TableCell cell)
        {
            if (pickable.HasValidHit && pickable.LastHit.collider != null)
            {
                if (pickable.LastHit.collider.TryGetComponent(out TableCell hitCell))
                {
                    cell = hitCell;
                    return true;
                }

                if (pickable.LastHit.collider.GetComponentInParent<TableCell>() != null)
                {
                    cell = pickable.LastHit.collider.GetComponentInParent<TableCell>();
                    return true;
                }
            }

            Camera camera = pickable.InteractionCamera;
            if (camera != null)
            {
                Ray ray = camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 200f, _cellMask, QueryTriggerInteraction.Ignore))
                {
                    cell = hit.collider.GetComponentInParent<TableCell>();
                    if (cell != null)
                    {
                        return true;
                    }
                }
            }

            cell = null;
            return false;
        }

        private void SnapIntoCell(TableGrid grid, TableCell targetCell)
        {
            if (!grid.TryPlace(targetCell, gameObject))
            {
                return;
            }

            if (_attachToCellTransform)
            {
                transform.SetParent(targetCell.transform);
            }

            transform.position = targetCell.GetSnapPosition(_cellSnapHeightOffset);
            _committedCell = targetCell;
            _pickable.CacheRestingState(targetCell);
        }

        private void ReoccupyCommittedCell(TableGrid grid)
        {
            if (_committedCell == null)
            {
                return;
            }

            grid.TryPlace(_committedCell, gameObject);
        }
    }
}

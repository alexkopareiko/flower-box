using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game
{
    /// <summary>
    /// Basic pick and place behaviour that lifts an object under the mouse cursor.
    /// Attach this component to any world object with a collider to make it pickable.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PickableItem : MonoBehaviour
    {
        [Header("Pointer")]
        [SerializeField] private Camera _cameraOverride;
        [SerializeField] private LayerMask _placementMask = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0.01f)] private float _maxRayDistance = 100f;
        [SerializeField] private bool _blockWhenPointerOverUI = true;

        [Header("Movement")]
        [SerializeField, Min(0.01f)] private float _hoverHeight = 0.15f;
        [SerializeField, Min(0.01f)] private float _followSpeed = 20f;
        [SerializeField] private Transform _dragParent;

        [Header("Physics")]
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private bool _lockRigidbodyWhenPlaced = true;

        public event Action<PickableItem> PickStarted;
        public event Action<PickableItem> DropRequested;
        public event Action<PickableItem> CancelRequested;

        private Camera _runtimeCamera;
        private bool _isHeld;
        private bool _dropHandled;
        private Vector3 _currentHoverPoint;
        private RaycastHit _lastHit;
        private bool _hasValidHit;

        private Vector3 _restingPosition;
        private Quaternion _restingRotation;
        private Transform _restingParent;
        private TableCell _restingCell;

        private Transform _parentBeforeDrag;

        public bool IsHeld => _isHeld;
        public bool HasValidHit => _hasValidHit;
        public RaycastHit LastHit => _lastHit;
        public Vector3 HoverPoint => _currentHoverPoint;
        public TableCell LastRestingCell => _restingCell;
        public Camera InteractionCamera => _runtimeCamera;

        private void Awake()
        {
            ResolveCamera();

            if (_rigidbody == null)
            {
                TryGetComponent(out _rigidbody);
            }

            CacheRestingState(null);
        }

        private void OnEnable()
        {
            ResolveCamera();
        }

        private void LateUpdate()
        {
            if (!_isHeld)
            {
                return;
            }

            UpdateHoverPoint();
            FollowPointer();

            if (Input.GetMouseButtonUp(0))
            {
                EndPickup();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                CancelPickup();
            }
        }

        private void OnMouseDown()
        {
            if (!enabled || !gameObject.activeInHierarchy)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                BeginPickup();
            }
        }

        private void BeginPickup()
        {
            if (_isHeld)
            {
                return;
            }

            if (_blockWhenPointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            ResolveCamera();
            _isHeld = true;
            _dropHandled = false;
            _parentBeforeDrag = transform.parent;

            if (_dragParent != null)
            {
                transform.SetParent(_dragParent);
            }

            ApplyPhysicsState(true);
            PickStarted?.Invoke(this);
        }

        private void EndPickup()
        {
            if (!_isHeld)
            {
                return;
            }

            _isHeld = false;
            if (_dragParent != null)
            {
                transform.SetParent(_parentBeforeDrag);
            }

            DropRequested?.Invoke(this);

            if (!_dropHandled)
            {
                CacheRestingState(null);
            }
        }

        private void CancelPickup()
        {
            if (!_isHeld)
            {
                return;
            }

            _isHeld = false;
            if (_dragParent != null)
            {
                transform.SetParent(_parentBeforeDrag);
            }

            RestoreRestingState();
            CancelRequested?.Invoke(this);
        }

        public void CacheRestingState(TableCell cell)
        {
            _restingPosition = transform.position;
            _restingRotation = transform.rotation;
            _restingParent = transform.parent;
            _restingCell = cell;
            _dropHandled = true;
            ApplyPhysicsState(false);
        }

        public void RestoreRestingState()
        {
            transform.SetParent(_restingParent);
            transform.SetPositionAndRotation(_restingPosition, _restingRotation);
            ApplyPhysicsState(false);
            _dropHandled = true;
        }

        public void MarkDropHandled()
        {
            _dropHandled = true;
        }

        private void FollowPointer()
        {
            Vector3 target = _currentHoverPoint + Vector3.up * _hoverHeight;
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * _followSpeed);
        }

        private void UpdateHoverPoint()
        {
            if (_runtimeCamera == null)
            {
                return;
            }

            Ray ray = _runtimeCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out _lastHit, _maxRayDistance, _placementMask, QueryTriggerInteraction.Ignore))
            {
                _currentHoverPoint = _lastHit.point;
                _hasValidHit = true;
                return;
            }

            Plane fallbackPlane = new Plane(Vector3.up, transform.position);
            if (fallbackPlane.Raycast(ray, out float enter))
            {
                _currentHoverPoint = ray.GetPoint(enter);
            }
            _hasValidHit = false;
        }

        private void ApplyPhysicsState(bool isHeld)
        {
            if (_rigidbody == null)
            {
                return;
            }

            if (isHeld || _lockRigidbodyWhenPlaced)
            {
                _rigidbody.isKinematic = true;
                _rigidbody.useGravity = false;
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            else
            {
                _rigidbody.isKinematic = false;
                _rigidbody.useGravity = true;
            }
        }

        private void ResolveCamera()
        {
            if (_cameraOverride != null)
            {
                _runtimeCamera = _cameraOverride;
            }
            else
            {
                _runtimeCamera = Camera.main;
            }
        }
    }
}

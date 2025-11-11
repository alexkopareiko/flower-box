using System;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game
{
    /// <summary>
    /// Basic pick and place behaviour that lifts an object under the mouse cursor.
    /// Attach this component to any world object with a collider to make it pickable.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PickableItem : MonoBehaviour, IPointerDownHandler
    {
        [Header("Pointer")]
        [SerializeField] private Camera _cameraOverride;
        [SerializeField] private LayerMask _placementMask = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0.01f)] private float _maxRayDistance = 100f;
        [SerializeField] private bool _blockWhenPointerOverUI = true;

        [Header("Grounding")]
        [SerializeField, Min(0.01f)] private float _groundProbeHeight = 0.5f;
        [SerializeField, Min(0.01f)] private float _groundProbeExtraDistance = 1.5f;

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
        private bool _lockRestingState = true;

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

            if (WasLeftPointerReleasedThisFrame())
            {
                EndPickup();
            }
            else if (WasRightPointerPressedThisFrame())
            {
                CancelPickup();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!enabled || !gameObject.activeInHierarchy)
            {
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left)
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
                CacheRestingState(null, false);
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

        public void CacheRestingState(TableCell cell, bool lockRigidbody = true)
        {
            Vector3 restingPosition = transform.position;
            if (cell == null && TryGetGroundPoint(restingPosition, out Vector3 groundedPosition))
            {
                restingPosition = groundedPosition;
            }

            _restingPosition = restingPosition;
            _restingRotation = transform.rotation;
            _restingParent = transform.parent;
            _restingCell = cell;
            _lockRestingState = lockRigidbody;
            _dropHandled = true;
            ApplyPhysicsState(false);
        }

        public void RestoreRestingState()
        {
            transform.SetParent(_restingParent);
            Vector3 targetPosition = _restingPosition;
            if (_restingCell == null && TryGetGroundPoint(_restingPosition, out Vector3 groundedPosition))
            {
                targetPosition = groundedPosition;
                _restingPosition = groundedPosition;
            }

            transform.SetPositionAndRotation(targetPosition, _restingRotation);
            _lockRestingState = true;
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

            Vector2 pointerPosition = GetPointerPosition();
            Ray ray = _runtimeCamera.ScreenPointToRay(new Vector3(pointerPosition.x, pointerPosition.y, 0f));
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

            bool shouldLockBody = isHeld || (_lockRigidbodyWhenPlaced && _lockRestingState);

            if (shouldLockBody)
            {
                if (!_rigidbody.isKinematic)
                {
                    _rigidbody.linearVelocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                }

                _rigidbody.isKinematic = true;
                _rigidbody.useGravity = false;
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

        private Vector2 GetPointerPosition()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current?.position.ReadValue() ?? Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }

        private bool WasLeftPointerReleasedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
            return Input.GetMouseButtonUp(0);
#endif
        }

        private bool WasRightPointerPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(1);
#endif
        }

        private bool TryGetGroundPoint(Vector3 referencePosition, out Vector3 groundedPosition)
        {
            Vector3 origin = referencePosition + Vector3.up * _groundProbeHeight;
            float maxDistance = _groundProbeHeight + _groundProbeExtraDistance;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDistance, _placementMask, QueryTriggerInteraction.Ignore))
            {
                groundedPosition = hit.point;
                return true;
            }

            groundedPosition = referencePosition;
            return false;
        }
    }
}

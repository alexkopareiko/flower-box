using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game {
    public class Lamp : MonoBehaviour
    {
        [System.Serializable]
        public struct SpotAngle
        {
            [Range(0f, 90f)] public float inner;
            [Range(0f, 90f)] public float outer;
        }

        [Header("References")]
        [SerializeField] private Light _lampLight;
        [SerializeField] private GameObject _lightParent;
        [SerializeField] private GameObject _joystick;
        [SerializeField] private GameObject _button;
        [SerializeField] private float _buttonPressDepth = 0.01f;
        [SerializeField] private TableGrid _gridOverride;
        [SerializeField, Range(0.05f, 0.9f)] private float _joystickDeadZone = 0.35f;
        [SerializeField] private float _joystickTiltAngle = 12f;
        [SerializeField] private float _joystickTiltSpeed = 12f;

        [Header("Flicker Settings")]
        [SerializeField] private SpotAngle _spotAngleMode1;
        [SerializeField] private SpotAngle _spotAngleMode3;


        [Header("State")]
        [Tooltip("Lamp state mapping: 0 = off, 1 = 1 mode, 2 = 2 mode")]
        [SerializeField] private int lampState = 0;

        private TableCell _currentCell;
        private Vector3 _buttonDefaultLocalPos;
        private bool _buttonHeld;
        private bool _joystickEngaged;
        private Quaternion _joystickDefaultLocalRotation = Quaternion.identity;
        private TableGrid _gridCache;
        private bool _hasInitializedPlacement;
        private readonly List<TableCell> _coveredCells = new List<TableCell>();

        public int LampState => lampState;
        public TableCell CurrentCell => _currentCell;
        public IReadOnlyList<TableCell> CoveredCells => _coveredCells;
        private Transform LampTransform => _lightParent != null ? _lightParent.transform : transform;
        private GameObject LampContent => _lightParent != null ? _lightParent : gameObject;
        private TableGrid Grid
        {
            get
            {
                if (_gridOverride != null)
                {
                    return _gridOverride;
                }

                if (_gridCache == null && GameManager.Instance != null)
                {
                    _gridCache = GameManager.Instance.TableGrid;
                }

                return _gridCache;
            }
        }

        private void Start()
        {
            UpdateLamp();
            if (_button != null)
            {
                _buttonDefaultLocalPos = _button.transform.localPosition;
            }

            CacheJoystickDefaults();
            SnapToCurrentCell();
        }

        private void OnValidate()
        {
            // Ensure lampState stays in the valid range [0..2] when edited in the inspector
            lampState = Mathf.Clamp(lampState, 0, 2);
            if (_button != null)
            {
                _buttonDefaultLocalPos = _button.transform.localPosition;
            }

            CacheJoystickDefaults();
        }

        private void Update()
        {
            if (!_hasInitializedPlacement && lampState > 0)
            {
                InitializeInitialPlacement();
            }

#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    TryHandleButtonPress(mouse.position.ReadValue());
                }

                if (_buttonHeld && mouse.leftButton.wasReleasedThisFrame)
                {
                    ReleaseButton();
                }
            }
#else
            if (Input.GetMouseButtonDown(0))
            {
                TryHandleButtonPress(Input.mousePosition);
            }

            if (_buttonHeld && Input.GetMouseButtonUp(0))
            {
                ReleaseButton();
            }
#endif

            HandleJoystickInput();
        }

        public void SetLampState(int state)
        {
            lampState = Mathf.Clamp(state, 0, 3);
            UpdateLamp();
        }

        private void UpdateLamp()
        {
            if (!_hasInitializedPlacement && lampState > 0)
            {
                InitializeInitialPlacement();
            }

            switch (lampState)
            {
                case 1: // Mode 1
                    _lampLight.enabled = true;
                    _lampLight.spotAngle = _spotAngleMode1.outer;
                    _lampLight.innerSpotAngle = _spotAngleMode1.inner;
                    break;
                case 2: // Mode 2
                    _lampLight.enabled = true;
                    _lampLight.spotAngle = _spotAngleMode3.outer;
                    _lampLight.innerSpotAngle = _spotAngleMode3.inner;
                    break;
                case 0: // Off
                    _lampLight.enabled = false;
                    break;
            }

            UpdateCoveredCells();
        }

        public void SetCurrentCell(TableCell cell)
        {
            _currentCell = cell;
            UpdateCoveredCells();
        }

        private void TryHandleButtonPress(Vector2 screenPoint)
        {
            if (_button == null)
            {
                return;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            Ray ray = cam.ScreenPointToRay(screenPoint);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == _button.transform || hit.transform.IsChildOf(_button.transform))
                {
                    CycleLampMode();
                    PressButton();
                }
            }
        }

        private void CycleLampMode()
        {
            lampState = (lampState + 1) % 3;
            UpdateLamp();
        }

        private void PressButton()
        {
            if (_buttonHeld)
            {
                return;
            }

            _buttonHeld = true;
            _button.transform.localPosition = _buttonDefaultLocalPos + Vector3.down * _buttonPressDepth;
        }

        private void ReleaseButton()
        {
            if (!_buttonHeld)
            {
                return;
            }

            _buttonHeld = false;
            _button.transform.localPosition = _buttonDefaultLocalPos;
        }

        private void HandleJoystickInput()
        {
            Vector2 input = ReadMovementInput();
            UpdateJoystickVisual(input);
            if (input.sqrMagnitude < _joystickDeadZone * _joystickDeadZone)
            {
                _joystickEngaged = false;
                return;
            }
            if (_joystickEngaged)
            {
                return;
            }

            Vector2Int direction;
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                direction = input.x > 0f ? Vector2Int.right : Vector2Int.left;
            }
            else
            {
                direction = input.y > 0f ? Vector2Int.up : Vector2Int.down;
            }

            if (TryMove(direction))
            {
                _joystickEngaged = true;
            }
        }

        private Vector2 ReadMovementInput()
        {
#if ENABLE_INPUT_SYSTEM
            Vector2 input = Vector2.zero;

            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                input = pad.leftStick.ReadValue();
                if (input.sqrMagnitude > 0f)
                {
                    return Vector2.ClampMagnitude(input, 1f);
                }
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                float x = 0f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    x -= 1f;
                }

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    x += 1f;
                }

                float y = 0f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    y -= 1f;
                }

                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    y += 1f;
                }

                input = new Vector2(x, -y);
            }

            return Vector2.ClampMagnitude(input, 1f);
#else
            float x = Input.GetAxisRaw("Horizontal");
            float y = Input.GetAxisRaw("Vertical");
            return new Vector2(x, y);
#endif
        }

        private void UpdateJoystickVisual(Vector2 input)
        {
            if (_joystick == null)
            {
                return;
            }

            Quaternion targetRotation;
            if (input.sqrMagnitude < Mathf.Epsilon)
            {
                targetRotation = _joystickDefaultLocalRotation;
            }
            else
            {
                float tiltX = -input.y * _joystickTiltAngle;
                float tiltZ = -input.x * _joystickTiltAngle;
                targetRotation = _joystickDefaultLocalRotation * Quaternion.Euler(tiltX, 0f, tiltZ);
            }

            _joystick.transform.localRotation = Quaternion.Slerp(
                _joystick.transform.localRotation,
                targetRotation,
                Time.deltaTime * _joystickTiltSpeed);
        }

        private bool TryMove(Vector2Int direction)
        {
            TableGrid grid = Grid;

            if (_currentCell == null)
                _currentCell = Grid.Cells[0];

            if (grid == null || _currentCell == null)
            {
                return false;
            }

            Vector2Int targetCoords = _currentCell.GridPosition + direction;
            if (!grid.TryGetCell(targetCoords, out TableCell targetCell))
            {
                return false;
            }

            if (targetCell.Content != null && targetCell.Content != LampContent)
            {
                return false;
            }

            if (!grid.TryPlace(targetCell, LampContent))
            {
                return false;
            }

            TableCell previousCell = _currentCell;
            _currentCell = targetCell;
            if (previousCell != null && previousCell != targetCell)
            {
                grid.Clear(previousCell, LampContent);
            }

            MoveLampToCell(targetCell);
            UpdateCoveredCells();
            return true;
        }

        private void MoveLampToCell(TableCell cell)
        {
            Transform targetTransform = LampTransform;
            if (cell == null || targetTransform == null)
            {
                return;
            }

            targetTransform.position = cell.GetSnapPosition();
        }

        private void SnapToCurrentCell()
        {
            if (_currentCell == null)
            {
                return;
            }

            TableGrid grid = Grid;
            if (grid != null)
            {
                grid.TryPlace(_currentCell, LampContent);
            }
            else if (_currentCell.Content != LampContent)
            {
                _currentCell.TrySetContent(LampContent);
            }

            MoveLampToCell(_currentCell);
            UpdateCoveredCells();
        }

        private void CacheJoystickDefaults()
        {
            if (_joystick != null)
            {
                _joystickDefaultLocalRotation = _joystick.transform.localRotation;
            }
        }

        private void InitializeInitialPlacement()
        {
            TableGrid grid = Grid;
            if (grid == null || grid.Cells.Count == 0)
            {
                return;
            }

            TableCell firstCell = grid.Cells[0];
            if (firstCell == null)
            {
                return;
            }

            if (!grid.TryPlace(firstCell, LampContent))
            {
                if (firstCell.Content != LampContent)
                {
                    return;
                }
            }

            _currentCell = firstCell;
            MoveLampToCell(firstCell);
            _hasInitializedPlacement = true;
            UpdateCoveredCells();
        }

        private void UpdateCoveredCells()
        {
            _coveredCells.Clear();

            if (lampState == 0 || _currentCell == null)
            {
                return;
            }

            TableGrid grid = Grid;
            if (grid == null)
            {
                _coveredCells.Add(_currentCell);
                return;
            }

            if (lampState == 1)
            {
                AddCoveredCell(_currentCell);
                return;
            }

            if (lampState == 2)
            {
                Vector2Int origin = _currentCell.GridPosition;
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        Vector2Int coords = origin + new Vector2Int(x, y);
                        if (grid.TryGetCell(coords, out TableCell cell))
                        {
                            AddCoveredCell(cell);
                        }
                    }
                }
            }
        }

        private void AddCoveredCell(TableCell cell)
        {
            if (cell == null || _coveredCells.Contains(cell))
            {
                return;
            }

            _coveredCells.Add(cell);
        }
    }
}

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

        [Header("Flicker Settings")]
        [SerializeField] private SpotAngle _spotAngleMode1;
        [SerializeField] private SpotAngle _spotAngleMode3;


        [Header("State")]
        [Tooltip("Lamp state mapping: 0 = off, 1 = 1 mode, 2 = 2 mode")]
        [SerializeField] private int lampState = 0;

        private TableCell _currentCell;
        private Vector3 _buttonDefaultLocalPos;
        private bool _buttonHeld;
        public int LampState => lampState;
        public TableCell CurrentCell => _currentCell;

        private void Start()
        {
            UpdateLamp();
            if (_button != null)
            {
                _buttonDefaultLocalPos = _button.transform.localPosition;
            }
        }

        private void OnValidate()
        {
            // Ensure lampState stays in the valid range [0..2] when edited in the inspector
            lampState = Mathf.Clamp(lampState, 0, 2);
            if (_button != null)
            {
                _buttonDefaultLocalPos = _button.transform.localPosition;
            }
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                TryHandleButtonPress(mouse.position.ReadValue());
            }

            if (_buttonHeld && mouse.leftButton.wasReleasedThisFrame)
            {
                ReleaseButton();
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
        }

        public void SetLampState(int state)
        {
            lampState = Mathf.Clamp(state, 0, 3);
            UpdateLamp();
        }

        private void UpdateLamp()
        {
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
        }

        public void SetCurrentCell(TableCell cell)
        {
            _currentCell = cell;
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

    }
}

using UnityEngine;

namespace Game
{
    /// <summary>
    /// Makes this UI element occupy the left or right half of the screen by adjusting its RectTransform.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class HalfScreenRect : MonoBehaviour
    {
        public enum Side { Left, Right }

        [Tooltip("Choose which half of the screen this Rect should occupy.")]
        [SerializeField] private Side _side = Side.Left;

        private RectTransform _rt;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            UpdateRect();
        }

        private void OnValidate()
        {
            // In editor, update when values change
            if (_rt == null) _rt = GetComponent<RectTransform>();
            UpdateRect();
        }

        private void UpdateRect()
        {
            // Set anchors for left or right half
            if (_side == Side.Left)
            {
                _rt.anchorMin = new Vector2(0f, 0f);
                _rt.anchorMax = new Vector2(0.5f, 1f);
            }
            else // Right
            {
                _rt.anchorMin = new Vector2(0.5f, 0f);
                _rt.anchorMax = new Vector2(1f, 1f);
            }

            // Stretch to fill the half
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
            _rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}

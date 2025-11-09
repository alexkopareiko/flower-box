using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace Game
{
    [RequireComponent(typeof(RectTransform))]
    public class SwipeAreaController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>
        /// How far (in pixels) you must drag before it counts as a swipe.
        /// </summary>
        [Tooltip("Minimum drag distance (px) to register a swipe.")]
        public float swipeThreshold = 50f;

        /// <summary>
        /// Fired once you lift your finger/mouse and a valid swipe was detected.
        /// </summary>
        [System.Serializable]
        public class SwipeEvent : UnityEvent<SwipeDirection> { }

        public enum SwipeDirection { Left, Right, Up, Down }

        [Tooltip("Subscribe to get notified when a valid swipe occurs.")]
        public SwipeEvent onSwipe;

        private Vector2 _startLocalPos;
        private bool _tracking;

        public void OnPointerDown(PointerEventData eventData)
        {
            // record start only if pointer is inside this RectTransform
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out _startLocalPos
            );
            _tracking = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_tracking) return;
            _tracking = false;

            // get end pos relative to same rect
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 endLocalPos
            );

            Vector2 delta = endLocalPos - _startLocalPos;
            if (delta.magnitude < swipeThreshold) return;

            // determine axis
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                // horizontal swipe
                onSwipe.Invoke(delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left);
            }
            else
            {
                // vertical swipe
                onSwipe.Invoke(delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down);
            }
        }
    }

}
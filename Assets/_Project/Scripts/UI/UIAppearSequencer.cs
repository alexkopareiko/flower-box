using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace Game.UI
{
    public class UIAppearSequencer : MonoBehaviour
    {
        [Header("Targets (order matters)")]
        public List<Transform> targets = new List<Transform>();

        [Header("Timing")] 
        [Min(0f)] public float duration = 0.3f;
        [Min(0f)] public float delayBetween = 0.05f;

        [Header("Animation")] 
        public Ease ease = Ease.OutBack;
        [Range(0f, 2f)] public float overshoot = 1.2f; // used for OutBack if applicable

        [Header("Behavior")] 
        public bool playOnEnable = true;
        public bool setInactiveUntilPlay = false;
        [Tooltip("Use real-time (unscaled) updates so animations run when timeScale is 0.")]
        public bool useUnscaledTime = true;

        private int _currentIndex = -1;
        private bool _isPlaying = false;

        void Awake()
        {
            PrepareHidden();
        }

        void OnEnable()
        {
            if (playOnEnable)
            {
                PlayFromStart();
            }
        }

        public void PrepareHidden()
        {
            if (targets == null) return;

            foreach (var t in targets)
            {
                if (t == null) continue;
                // Kill any existing tweens to avoid conflicts
                t.DOKill();
                if (setInactiveUntilPlay) t.gameObject.SetActive(false);
                t.localScale = Vector3.zero;
            }
            _currentIndex = -1;
            _isPlaying = false;
        }

        public void PlayFromStart()
        {
            // Ensure hidden state first
            PrepareHidden();
            Play();
        }

        public void Play()
        {
            if (targets == null || targets.Count == 0 || _isPlaying) return;
            _isPlaying = true;
            _currentIndex = -1;
            PlayNext();
        }

        private void PlayNext()
        {
            _currentIndex++;

            if (_currentIndex >= targets.Count)
            {
                _isPlaying = false;
                return;
            }

            var t = targets[_currentIndex];
            if (t == null)
            {
                // Skip nulls and continue
                PlayNext();
                return;
            }

            // Ensure target is ready
            t.DOKill();
            if (setInactiveUntilPlay && !t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
            t.localScale = Vector3.zero;

            // Apply ease settings (supporting OutBack overshoot tweak)
            var tween = t.DOScale(Vector3.one, Mathf.Max(0.0001f, duration));
            if (ease == Ease.OutBack)
                tween.SetEase(ease, overshoot);
            else
                tween.SetEase(ease);
            if (useUnscaledTime)
                tween.SetUpdate(true);

            // Chain next via callback
            tween.OnComplete(() =>
            {
                if (delayBetween > 0f)
                {
                    // Use a delayed call so we still rely on DOTween + callback flow
                    var d = DOVirtual.DelayedCall(delayBetween, PlayNext);
                    if (useUnscaledTime) d.SetUpdate(true);
                }
                else
                {
                    PlayNext();
                }
            });
        }

        public void ResetState()
        {
            // Stop everything and return to hidden
            if (targets != null)
            {
                foreach (var t in targets)
                {
                    if (t == null) continue;
                    t.DOKill();
                }
            }
            PrepareHidden();
        }
    }
}

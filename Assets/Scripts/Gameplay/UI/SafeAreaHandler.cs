using UnityEngine;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// GDD §7/§11 — Safe Area / Notch Desteği.
    /// Automatically adjusts RectTransform offsets to stay within the safe area
    /// (notch + gesture bar). Supports smooth transitions, orientation changes,
    /// and edge-to-edge mode for immersive screens.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHandler : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafeArea;
        private bool _initialized;

        [Tooltip("If true, applies safe area margins to bottom only (for bottom-anchored UI).")]
        public bool BottomOnly;

        [Tooltip("If true, applies safe area margins to top only (for top-anchored UI).")]
        public bool TopOnly;

        [Tooltip("If true, all edges apply safe area (full screen panels).")]
        public bool FullScreen;

        [Tooltip("Duration of the safe area adjustment animation (0 = instant).")]
        public float TransitionDuration = 0.25f;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplySafeArea();
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (_rect == null) return;
            // Reapply in editor when safe area changes (e.g. orientation switch simulation)
            if (Screen.safeArea != _lastSafeArea)
                ApplySafeArea();
        }
#endif

        private void ApplySafeArea()
        {
            if (_rect == null) return;

            Rect safeArea = Screen.safeArea;
            if (safeArea == _lastSafeArea && _initialized) return;
            _lastSafeArea = safeArea;
            _initialized = true;

            Vector2 screenSize = new Vector2(Screen.width, Screen.height);

            // Calculate relative safe area
            Vector2 anchorMin = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            Vector2 anchorMax = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);

            // Apply edge filtering based on mode
            if (BottomOnly)
            {
                anchorMin.x = 0f;
                anchorMax.x = 1f;
                anchorMax.y = 1f;
            }
            else if (TopOnly)
            {
                anchorMin.x = 0f;
                anchorMax.x = 1f;
                anchorMin.y = 0f;
            }
            else if (!FullScreen)
            {
                // Default: apply horizontal safe area only
                anchorMin.y = 0f;
                anchorMax.y = 1f;
            }

            if (Application.isPlaying && TransitionDuration > 0f && _initialized)
            {
                DOTween.Kill(_rect);
                DOTween.To(() => _rect.anchorMin, v => _rect.anchorMin = v, anchorMin, TransitionDuration)
                    .SetEase(DG.Tweening.Ease.OutCubic).SetAutoKill(true).SetTarget(_rect);
                DOTween.To(() => _rect.anchorMax, v => _rect.anchorMax = v, anchorMax, TransitionDuration)
                    .SetEase(DG.Tweening.Ease.OutCubic).SetAutoKill(true).SetTarget(_rect);
                _rect.offsetMin = Vector2.zero;
                _rect.offsetMax = Vector2.zero;
            }
            else
            {
                _rect.anchorMin = anchorMin;
                _rect.anchorMax = anchorMax;
                _rect.offsetMin = Vector2.zero;
                _rect.offsetMax = Vector2.zero;
            }
        }

        /// <summary>Force reapply safe area (call after orientation changes).</summary>
        public void Refresh()
        {
            _lastSafeArea = new Rect(0, 0, 0, 0);
            ApplySafeArea();
        }
    }
}

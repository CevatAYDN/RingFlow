using UnityEngine;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// GDD §7/§11 — Safe Area / Notch Desteği.
    /// Automatically adjusts RectTransform offsets to stay within the safe area
    /// (notch + gesture bar). Attached via GameUIResources.CreateSafeAreaPanel().
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHandler : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafeArea;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (Application.isEditor || _lastSafeArea != Screen.safeArea)
            {
                ApplySafeArea();
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            if (_rect == null) return;

            Rect safeArea = Screen.safeArea;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);

            Vector2 anchorMin = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            Vector2 anchorMax = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);

            _rect.anchorMin = anchorMin;
            _rect.anchorMax = anchorMax;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}

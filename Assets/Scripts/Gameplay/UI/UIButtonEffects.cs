using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Premium button interaction effects — hover scaling and click punch feedback.
    /// Fully respects the player's 'Reduce Motion' accessibility configuration.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Vector3 _originalScale = Vector3.one;
        private Button _button;
        private bool _initialized;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _originalScale = transform.localScale;
            _button = GetComponent<Button>();
            _initialized = true;
        }

        private bool IsInteractable() => _button != null && _button.interactable;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsInteractable()) return;
            EnsureInitialized();
            transform.DOScale(_originalScale * GameUIResources.ButtonHoverScale, 0.15f).SetEase(DG.Tweening.Ease.OutQuad);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            EnsureInitialized();
            transform.DOScale(_originalScale, 0.15f).SetEase(DG.Tweening.Ease.OutQuad);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable()) return;
            EnsureInitialized();
            transform.DOScale(_originalScale * GameUIResources.ButtonPressScale, 0.1f).SetEase(DG.Tweening.Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsInteractable()) return;
            EnsureInitialized();
            transform.DOScale(_originalScale * GameUIResources.ButtonHoverScale, 0.1f).SetEase(DG.Tweening.Ease.OutQuad);
        }

        private void OnDisable()
        {
            if (_initialized)
                transform.localScale = _originalScale;
        }
    }
}

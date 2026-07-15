using System.Collections;
using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// 3D pole in the playfield. Receives click via Unity's IPointerDownHandler
    /// pipeline (EventSystem + PhysicsRaycaster + Collider). Forwards selection
    /// requests to <see cref="PoleMediator"/>, which fires SelectPoleSignal.
    /// </summary>
    [Mediator(typeof(PoleMediator))]
    public class PoleView : View, IPointerDownHandler
    {
        [SerializeField] private int _poleId;
        public int PoleId { get => _poleId; set => _poleId = value; }

        public System.Action OnClicked;

        [Inject] private GameFeelConfigSO _feelConfig;

        private Color GetSelectedTint()
        {
            if (_feelConfig == null) throw new System.InvalidOperationException("[PoleView] GameFeelConfigSO not injected!");
            return _feelConfig.SelectedTint;
        }

        private Color GetErrorTint()
        {
            if (_feelConfig == null) throw new System.InvalidOperationException("[PoleView] GameFeelConfigSO not injected!");
            return _feelConfig.ErrorTint;
        }

        private Color GetLockedTint()
        {
            if (_feelConfig == null) throw new System.InvalidOperationException("[PoleView] GameFeelConfigSO not injected!");
            return _feelConfig.LockedTint;
        }

        private Renderer[] _renderers;
        private Color _baseColor = Color.white;
        private Coroutine _flashRoutine;
        private bool _isSelected;
        private bool _isLocked;
        private MaterialPropertyBlock _propBlock;

        // Cached WaitForSeconds instances — avoids per-flash GC allocation (M6).
        // Keyed by duration: error (0.35 s) and success (0.8 s) are the only values used.
        private WaitForSeconds _waitError;
        private WaitForSeconds _waitSuccess;

        private void Awake()
        {
            EnsureMaterialAccess();
        }

        public void SetLocked(bool locked)
        {
            _isLocked = locked;
            ApplyTint();
        }

        public void SetSelected(bool selected)
        {
            if (_isSelected == selected) return;
            _isSelected = selected;
            ApplyTint();
        }

        public void FlashError(float duration = 0.35f)
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine(duration));
        }

        private IEnumerator FlashRoutine(float duration)
        {
            SetColor(GetErrorTint());
            // Reuse cached instance for the default 0.35 s error duration (zero GC, M6).
            // A non-default duration is rare (only the public overload with a custom value),
            // so allocating once there is acceptable.
            if (_waitError == null) _waitError = new WaitForSeconds(0.35f);
            yield return Mathf.Approximately(duration, 0.35f)
                ? _waitError
                : new WaitForSeconds(duration);
            ApplyTint();
            _flashRoutine = null;
        }

        public void FlashSuccess(float duration = 0.8f)
        {
            FlashSuccess(duration, new Color(1f, 0.82f, 0f, 1f));
        }

        public void FlashSuccess(float duration, Color color)
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashSuccessRoutine(duration, color));
        }

        private IEnumerator FlashSuccessRoutine(float duration, Color color)
        {
            SetColor(color);
            // Reuse cached instance for the default 0.8 s success duration (zero GC, M6).
            if (_waitSuccess == null) _waitSuccess = new WaitForSeconds(0.8f);
            yield return Mathf.Approximately(duration, 0.8f)
                ? _waitSuccess
                : new WaitForSeconds(duration);
            ApplyTint();
            _flashRoutine = null;
        }

        private void ApplyTint()
        {
            if (_flashRoutine != null) return;
            Color c = _isLocked ? GetLockedTint() : (_isSelected ? GetSelectedTint() : _baseColor);
            SetColor(c);
        }

        private void SetColor(Color c)
        {
            EnsureRenderers();
            if (_renderers == null || _renderers.Length == 0) return;
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
            
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_Color", c);
                _propBlock.SetColor("_BaseColor", c);
                r.SetPropertyBlock(_propBlock);
            }
        }

        /// <summary>
        /// Consolidated renderer discovery — called only when cache is empty.
        /// Avoids redundant GetComponentsInChildren allocations across
        /// SetColor, EnsureMaterialAccess, and SyncMaterial.
        /// </summary>
        private void EnsureRenderers()
        {
            if (_renderers != null && _renderers.Length > 0) return;

            var childRenderers = GetComponentsInChildren<Renderer>(true);
            int count = 0;
            for (int i = 0; i < childRenderers.Length; i++)
            {
                var r = childRenderers[i];
                if (r.name == "Body" || r.name == "Cap" || r.gameObject == gameObject)
                    count++;
            }
            _renderers = new Renderer[count];
            int idx = 0;
            for (int i = 0; i < childRenderers.Length; i++)
            {
                var r = childRenderers[i];
                if (r.name == "Body" || r.name == "Cap" || r.gameObject == gameObject)
                    _renderers[idx++] = r;
            }
        }

        private void EnsureMaterialAccess()
        {
            EnsureRenderers();
            if (_renderers != null && _renderers.Length > 0 && _renderers[0] != null && _renderers[0].sharedMaterial != null)
            {
                _baseColor = _renderers[0].sharedMaterial.color;
            }
        }

        /// <summary>
        /// Call after the Renderer's sharedMaterial is replaced (e.g. by
        /// BoardView.BuildBoard) so this view works with the current material.
        /// </summary>
        public void SyncMaterial()
        {
            // Force re-discovery since materials changed
            _renderers = null;
            EnsureRenderers();

            if (_renderers != null && _renderers.Length > 0 && _renderers[0] != null && _renderers[0].sharedMaterial != null)
            {
                _baseColor = _renderers[0].sharedMaterial.color;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            NexusLog.Info("PoleView", nameof(OnPointerDown), PoleId.ToString(), "Pointer down received.");
            OnClicked?.Invoke();
            if (eventData != null) eventData.Use();
        }
    }
}

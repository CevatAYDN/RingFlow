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
            if (_feelConfig == null) return _baseColor;
            return _feelConfig.SelectedTint;
        }

        private Color GetErrorTint()
        {
            if (_feelConfig == null) return Color.red;
            return _feelConfig.ErrorTint;
        }

        private Color GetLockedTint()
        {
            if (_feelConfig == null) return _baseColor;
            return _feelConfig.LockedTint;
        }

        private Renderer[] _renderers;
        private Color _baseColor = Color.white;
        private Coroutine _flashRoutine;
        private bool _isSelected;
        private bool _isLocked;
        private MaterialPropertyBlock _propBlock;

        // FIX-M6: Pre-allocate WaitForSeconds instances for the two most common
        // flash durations (0.35s error, 0.8s success) as static readonly fields.
        // This eliminates per-flash GC allocation entirely and avoids per-instance
        // object overhead. The old instance fields (_waitError, _waitSuccess) were
        // removed as they are now dead code — all Flash methods use the static cache.
        private static readonly WaitForSeconds _cachedWaitError = new WaitForSeconds(0.35f);
        private static readonly WaitForSeconds _cachedWaitSuccess = new WaitForSeconds(0.8f);

        private void Awake()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("PoleView", nameof(Awake), PoleId.ToString(), "Awake called.");
#endif
            EnsureMaterialAccess();
        }

        public void SetLocked(bool locked)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_isLocked != locked)
                NexusLog.Info("PoleView", nameof(SetLocked), PoleId.ToString(), $"Locked={locked}.");
#endif
            _isLocked = locked;
            ApplyTint();
        }

        public void SetSelected(bool selected)
        {
            if (_isSelected == selected) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("PoleView", nameof(SetSelected), PoleId.ToString(), $"Selected={selected}.");
#endif
            _isSelected = selected;
            ApplyTint();
        }

        public void FlashError(float duration = -1f)
        {
            if (duration <= 0f) duration = _feelConfig != null ? _feelConfig.ErrorFlashDuration : 0.35f;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("PoleView", nameof(FlashError), PoleId.ToString(), $"Error flash triggered. duration={duration:F2}s.");
#endif
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine(duration));
        }

        private IEnumerator FlashRoutine(float duration)
        {
            SetColor(GetErrorTint());
            // FIX-M6: Use static cached WaitForSeconds for the standard 0.35s
            // duration instead of instance fields. The old code allocated new
            // WaitForSeconds on any non-default duration; now we always use
            // the static cache for the standard value and only allocate for
            // custom durations (which are rare).
            yield return Mathf.Approximately(duration, 0.35f)
                ? _cachedWaitError
                : new WaitForSeconds(duration);
            ApplyTint();
            _flashRoutine = null;
        }

        public void FlashSuccess(float duration = -1f)
        {
            float dur = duration > 0f ? duration : (_feelConfig != null ? _feelConfig.PoleSuccessFlashDuration : 0.8f);
            Color col = _feelConfig != null ? _feelConfig.PoleSuccessFlashColor : new Color(1f, 0.82f, 0f, 1f);
            FlashSuccess(dur, col);
        }

        public void FlashSuccess(float duration, Color color)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("PoleView", nameof(FlashSuccess), PoleId.ToString(), $"Success flash triggered. duration={duration:F2}s, color={color}.");
#endif
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashSuccessRoutine(duration, color));
        }

        private IEnumerator FlashSuccessRoutine(float duration, Color color)
        {
            SetColor(color);
            // FIX-M6: Use static cached WaitForSeconds for the standard 0.8s
            // duration to avoid per-flash allocation.
            yield return Mathf.Approximately(duration, 0.8f)
                ? _cachedWaitSuccess
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            else
            {
                NexusLog.Warn("PoleView", nameof(EnsureMaterialAccess), PoleId.ToString(),
                    $"No valid renderer/material found. _renderers={((_renderers == null) ? "null" : _renderers.Length.ToString())}.");
            }
#endif
        }

        /// <summary>
        /// Call after the Renderer's sharedMaterial is replaced (e.g. by
        /// BoardView.BuildBoard) so this view works with the current material.
        /// </summary>
        public void SyncMaterial()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("PoleView", nameof(SyncMaterial), PoleId.ToString(), "Syncing material after sharedMaterial replacement.");
#endif
            // Force re-discovery since materials changed
            _renderers = null;
            EnsureRenderers();

            if (_renderers != null && _renderers.Length > 0 && _renderers[0] != null && _renderers[0].sharedMaterial != null)
            {
                _baseColor = _renderers[0].sharedMaterial.color;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Info("PoleView", nameof(SyncMaterial), PoleId.ToString(), $"Base color synced to {_baseColor}.");
#endif
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            else
            {
                NexusLog.Warn("PoleView", nameof(SyncMaterial), PoleId.ToString(),
                    "SyncMaterial: no valid renderer/material after re-discovery.");
            }
#endif
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            NexusLog.Info("PoleView", nameof(OnPointerDown), PoleId.ToString(), "Pointer down received.");
            OnClicked?.Invoke();
            if (eventData != null) eventData.Use();
        }
    }
}

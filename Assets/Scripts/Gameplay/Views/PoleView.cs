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

        // Read from GameFeelConfigSO at first use; falls back to static defaults when SO not available.
        private static Color GetSelectedTint() => GameFeelConfigSO.Instance?.SelectedTint ?? new Color(0.30f, 0.85f, 1f, 1f);
        private static Color GetErrorTint() => GameFeelConfigSO.Instance?.ErrorTint ?? new Color(1f, 0.30f, 0.30f, 1f);
        private static Color GetLockedTint() => GameFeelConfigSO.Instance?.LockedTint ?? Color.black;

        private Renderer[] _renderers;
        private Color _baseColor = Color.white;
        private Coroutine _flashRoutine;
        private bool _isSelected;
        private bool _isLocked;
        private MaterialPropertyBlock _propBlock;

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
            yield return new WaitForSeconds(duration);
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
            yield return new WaitForSeconds(duration);
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
            if (_renderers == null || _renderers.Length == 0)
            {
                var childRenderers = GetComponentsInChildren<Renderer>(true);
                var list = new System.Collections.Generic.List<Renderer>();
                foreach (var r in childRenderers)
                {
                    if (r.name == "Body" || r.name == "Cap" || r.gameObject == gameObject)
                    {
                        list.Add(r);
                    }
                }
                _renderers = list.ToArray();
            }
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

        private void EnsureMaterialAccess()
        {
            if (_renderers == null || _renderers.Length == 0)
            {
                var childRenderers = GetComponentsInChildren<Renderer>(true);
                var list = new System.Collections.Generic.List<Renderer>();
                foreach (var r in childRenderers)
                {
                    if (r.name == "Body" || r.name == "Cap" || r.gameObject == gameObject)
                    {
                        list.Add(r);
                    }
                }
                _renderers = list.ToArray();
            }
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
            var childRenderers = GetComponentsInChildren<Renderer>(true);
            var list = new System.Collections.Generic.List<Renderer>();
            foreach (var r in childRenderers)
            {
                if (r.name == "Body" || r.name == "Cap" || r.gameObject == gameObject)
                {
                    list.Add(r);
                }
            }
            _renderers = list.ToArray();

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

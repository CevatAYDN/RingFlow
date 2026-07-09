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

        private static readonly Color SelectedTint = new Color(0.30f, 0.85f, 1f, 1f);
        private static readonly Color ErrorTint = new Color(1f, 0.30f, 0.30f, 1f);
        private static readonly Color LockedTint = Color.black;

        private Renderer _renderer;
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
            SetColor(ErrorTint);
            yield return new WaitForSeconds(duration);
            ApplyTint();
            _flashRoutine = null;
        }

        private void ApplyTint()
        {
            if (_flashRoutine != null) return;
            Color c = _isLocked ? LockedTint : (_isSelected ? SelectedTint : _baseColor);
            SetColor(c);
        }

        private void SetColor(Color c)
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            if (_renderer == null) return;
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_Color", c);
            _propBlock.SetColor("_BaseColor", c);
            _renderer.SetPropertyBlock(_propBlock);
        }

        private void EnsureMaterialAccess()
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            if (_renderer != null && _renderer.sharedMaterial != null)
            {
                _baseColor = _renderer.sharedMaterial.color;
            }
        }

        /// <summary>
        /// Call after the Renderer's sharedMaterial is replaced (e.g. by
        /// BoardView.BuildBoard) so this view works with the current material.
        /// </summary>
        public void SyncMaterial()
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            if (_renderer != null && _renderer.sharedMaterial != null)
            {
                _baseColor = _renderer.sharedMaterial.color;
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

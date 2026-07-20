using DG.Tweening;
using RingFlow.Gameplay;
using UnityEngine;

namespace RingFlow.Gameplay.Views
{
    // Resolve ambiguity between DG.Tweening.Ease and RingFlow.Gameplay.Ease (UIThemeConfigSO)
    using Ease = DG.Tweening.Ease;
    using PoleView = RingFlow.Gameplay.PoleView;

    /// <summary>
    /// Handles pole/ring selection visual state (lift, glow, emission).
    /// Extracted from BoardView to reduce its responsibilities.
    /// Reuses a single MaterialPropertyBlock and persistent glow Light child
    /// to avoid per-selection allocations.
    /// </summary>
    public class BoardSelectionHandler
    {
        private readonly GameFeelConfigSO F;
        private readonly SettingsModel _settingsModel;

        private int _lastSelectedPoleId = -1;
        private MaterialPropertyBlock _selectionPropBlock;

        /// <summary>
        /// Reference to the currently animating pole ID (set by RingAnimationHandler).
        /// When >= 0, all selection tweens on that pole are skipped to avoid
        /// fighting the active jump animation.
        /// </summary>
        public System.Func<int> GetAnimatingTargetPoleId { get; set; }

        public BoardSelectionHandler(GameFeelConfigSO feelConfig, SettingsModel settingsModel)
        {
            F = feelConfig;
            _settingsModel = settingsModel;
        }

        public int LastSelectedPoleId => _lastSelectedPoleId;

        /// <summary>
        /// Updates selection state. Returns true if the selection actually changed.
        /// </summary>
        public bool SetSelectedPole(int poleId, System.Action<int> onNewSelection)
        {
            if (_lastSelectedPoleId == poleId) return false;
            _lastSelectedPoleId = poleId;
            if (poleId >= 0)
                onNewSelection?.Invoke(poleId);
            return true;
        }

        /// <summary>
        /// Applies visual selection state (lift tween, glow light, emission) to all poles.
        /// Skips poles whose ID matches the animating target pole to avoid tween fights.
        /// </summary>
        public void ApplySelection(
            int poleCount,
            System.Func<int, GameObject> getPoleObj,
            System.Func<int, int> getRingCountOnPole,
            System.Func<int, int, GameObject> getRingAt,
            System.Func<int, PoleView> getPoleView)
        {
            bool reduceMotion = _settingsModel != null && _settingsModel.ReduceMotion.Value;
            bool slowMode = _settingsModel != null && _settingsModel.SlowMode.Value;
            float speed = slowMode ? F.SlowModeMultiplier : 1f;
            float duration = F.SelectionLiftDuration * speed;
            int animatingPoleId = GetAnimatingTargetPoleId?.Invoke() ?? -1;

            for (int i = 0; i < poleCount; i++)
            {
                var pv = getPoleView(i);
                if (pv == null) continue;

                bool isSelected = i == _lastSelectedPoleId;
                pv.SetSelected(isSelected);

                // Skip all rings on the animating pole
                if (i == animatingPoleId) continue;

                int ringCount = getRingCountOnPole(i);
                if (ringCount <= 0) continue;

                var poleObj = getPoleObj(i);
                if (poleObj == null) continue;

                float poleScaleY = poleObj.transform.localScale.y;
                int backRowCount = Mathf.CeilToInt(poleCount / 2.0f);
                float scaleFactor = (poleCount > 5 && i < backRowCount) ? 0.85f : 1.0f;

                // Snap non-top rings to resting position (don't kill celebrate tweens)
                for (int r = 0; r < ringCount - 1; r++)
                {
                    var ring = getRingAt(i, r);
                    if (ring == null) continue;
                    float restY = ((0.22f + (r * (F.RingStackSpacing * F.PoleScale.y))) * scaleFactor) / poleScaleY;
                    ring.transform.localPosition = new Vector3(0f, restY, 0f);
                }

                // Animate top ring
                var topRing = getRingAt(i, ringCount - 1);
                if (topRing == null) continue;

                int topIndex = ringCount - 1;
                float targetWorldY = (0.22f + (topIndex * (F.RingStackSpacing * F.PoleScale.y))) * scaleFactor
                    + (isSelected ? F.RingSelectionLift * scaleFactor : 0f);
                float targetY = targetWorldY / poleScaleY;

                DOTween.Kill(topRing.transform);
                if (reduceMotion)
                {
                    topRing.transform.localPosition = new Vector3(0f, targetY, 0f);
                }
                else
                {
                    float capturedY = targetY;
                    bool capturedSelected = isSelected;
                    var capturedRing = topRing;
                    topRing.transform.DOLocalMoveY(targetY, duration).SetEase(Ease.OutQuad)
                        .SetAutoKill(true)
                        .OnComplete(() =>
                        {
                            if (capturedSelected && capturedRing != null)
                            {
                                DOTween.Kill(capturedRing.transform);
                                float bobOffset = (F.TutorialArrowBobHeight * 0.4f * scaleFactor) / poleScaleY;
                                capturedRing.transform.DOLocalMoveY(capturedY + bobOffset, 0.6f)
                                    .SetEase(Ease.InOutSine)
                                    .SetLoops(-1, LoopType.Yoyo)
                                    .SetAutoKill(true);
                            }
                        });
                }

                // Selection glow (reuse MaterialPropertyBlock + persistent Light child)
                ApplySelectionGlow(topRing, isSelected);
            }
        }

        private void ApplySelectionGlow(GameObject ring, bool isSelected)
        {
            var ringRenderer = ring.GetComponentInChildren<Renderer>();
            if (_selectionPropBlock == null)
                _selectionPropBlock = new MaterialPropertyBlock();

            var lightChildTransform = ring.transform.Find("SelectionGlowLight");

            if (isSelected)
            {
                if (lightChildTransform == null)
                {
                    var lightGo = new GameObject("SelectionGlowLight");
                    lightGo.transform.SetParent(ring.transform, false);
                    lightGo.transform.localPosition = Vector3.zero;
                    var light = lightGo.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = F.SelectionGlowColor;
                    light.range = F.SelectionGlowRange;
                    light.intensity = F.SelectionGlowIntensity;
                    light.shadows = LightShadows.None;
                    lightChildTransform = lightGo.transform;
                }
                else
                {
                    lightChildTransform.gameObject.SetActive(true);
                }

                if (ringRenderer != null)
                {
                    ringRenderer.GetPropertyBlock(_selectionPropBlock);
                    _selectionPropBlock.SetColor("_EmissionColor", F.SelectionEmissionColor);
                    ringRenderer.SetPropertyBlock(_selectionPropBlock);
                }
            }
            else
            {
                if (lightChildTransform != null)
                    lightChildTransform.gameObject.SetActive(false);

                if (ringRenderer != null)
                {
                    ringRenderer.GetPropertyBlock(_selectionPropBlock);
                    _selectionPropBlock.SetColor("_EmissionColor", Color.black);
                    ringRenderer.SetPropertyBlock(_selectionPropBlock);
                }
            }
        }

        /// <summary>
        /// Called when a ring is recycled — cleans up the persistent glow light.
        /// </summary>
        public static void CleanupRingSelectionLight(GameObject ring)
        {
            if (ring == null) return;
            var lightChild = ring.transform.Find("SelectionGlowLight");
            if (lightChild != null)
                UnityEngine.Object.Destroy(lightChild.gameObject);
        }
    }
}

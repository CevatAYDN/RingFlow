using DG.Tweening;
using UnityEngine;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Lightweight DOTween animator for special ring ASCII overlays.
    /// Attached to the "SpecialOverlay" child GameObject of special rings.
    /// Provides subtle pulse/glow animations for Chain, Magnet, and Paint mechanics
    /// so players can visually distinguish them at a glance.
    ///
    /// Zero-GC during gameplay: all tweens are created once in Start() and loop
    /// indefinitely via DOTween's built-in looping. DOTween.Kill in RecycleRing
    /// (BoardView) cleans up the tween when the ring is recycled.
    /// </summary>
    public sealed class MechanicOverlayAnimator : MonoBehaviour
    {
        private TextMesh _textMesh;
        private Color _originalColor;

        /// <summary>
        /// Initialize with the mechanic type that determines the animation style.
        /// Called once when the overlay is first created (from SpecialOverlayRenderer).
        /// </summary>
        public void Initialize(RingType mechanicType)
        {
            _textMesh = GetComponent<TextMesh>();
            if (_textMesh == null) return;

            _originalColor = _textMesh.color;

            switch (mechanicType)
            {
                case RingType.Chain:
                    StartChainAnimation();
                    break;
                case RingType.Magnet:
                    StartMagnetAnimation();
                    break;
                case RingType.Paint:
                    StartPaintAnimation();
                    break;
                case RingType.Frozen:
                    StartFrozenAnimation();
                    break;
                case RingType.Stone:
                    StartStoneAnimation();
                    break;
                // Other types keep static overlays
            }
        }

        private void StartChainAnimation()
        {
            // Metallic shimmer: scale pulse + slight rotation oscillation
            float pulseDuration = 1.2f;
            transform.DOScale(transform.localScale * 1.15f, pulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(DG.Tweening.Ease.InOutSine)
                .SetAutoKill(true);

            // Subtle color shimmer between white and warm metallic gold
            if (_textMesh != null)
            {
                DOTween.To(() => _textMesh.color, c => _textMesh.color = c,
                    new Color(1f, 0.85f, 0.5f), pulseDuration * 0.5f)
                    .SetTarget(transform)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(DG.Tweening.Ease.InOutSine)
                    .SetAutoKill(true);
            }

            // Gentle rotation oscillation
            transform.DORotate(new Vector3(0f, 0f, 5f), 0.8f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(DG.Tweening.Ease.InOutSine)
                .SetRelative(true)
                .SetAutoKill(true);
        }

        private void StartMagnetAnimation()
        {
            // Magnetic pulse: quick color pulse between magenta and white
            float pulseDuration = 0.6f;
            if (_textMesh != null)
            {
                DOTween.To(() => _textMesh.color, c => _textMesh.color = c,
                    Color.Lerp(_originalColor, Color.white, 0.5f), pulseDuration * 0.5f)
                    .SetTarget(transform)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(DG.Tweening.Ease.InOutSine)
                    .SetAutoKill(true);
            }

            // Scale pulse
            transform.DOScale(transform.localScale * 1.1f, pulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(DG.Tweening.Ease.InOutSine)
                .SetAutoKill(true);
        }

        private void StartPaintAnimation()
        {
            // Paint drip effect: very slow bob up/down + green shimmer
            float bobDuration = 1.8f;
            transform.DOLocalMoveY(transform.localPosition.y + 0.02f, bobDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(DG.Tweening.Ease.InOutSine)
                .SetAutoKill(true);

            if (_textMesh != null)
            {
                DOTween.To(() => _textMesh.color, c => _textMesh.color = c,
                    Color.Lerp(_originalColor, Color.white, 0.3f), bobDuration * 0.5f)
                    .SetTarget(transform)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(DG.Tweening.Ease.InOutSine)
                    .SetAutoKill(true);
            }
        }

        private void StartFrozenAnimation()
        {
            // Icy blue pulse: slow shimmer to suggest frost/cold
            float pulseDuration = 1.5f;
            transform.DOScale(transform.localScale * 1.08f, pulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(DG.Tweening.Ease.InOutSine)
                .SetAutoKill(true);

            if (_textMesh != null)
            {
                DOTween.To(() => _textMesh.color, c => _textMesh.color = c,
                    Color.Lerp(_originalColor, Color.white, 0.4f), pulseDuration * 0.5f)
                    .SetTarget(transform)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(DG.Tweening.Ease.InOutSine)
                    .SetAutoKill(true);
            }
        }

        private void StartStoneAnimation()
        {
            // Heavy heartbeat pulse: very slow, subtle throb to suggest immovable weight
            float pulseDuration = 2.0f;
            transform.DOScale(transform.localScale * 1.06f, pulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(DG.Tweening.Ease.InOutQuad)
                .SetAutoKill(true);

            if (_textMesh != null)
            {
                // Darker pulse: gray to darker gray
                DOTween.To(() => _textMesh.color, c => _textMesh.color = c,
                    new Color(0.5f, 0.5f, 0.5f), pulseDuration * 0.5f)
                    .SetTarget(transform)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(DG.Tweening.Ease.InOutSine)
                    .SetAutoKill(true);
            }
        }

        private void OnDestroy()
        {
            DOTween.Kill(transform);
            if (_textMesh != null) DOTween.Kill(_textMesh);
        }
    }
}

using System.Collections.Generic;
using DG.Tweening;
using Nexus.Core.Services;
using RingFlow.Gameplay.Services;
using RingFlow.Gameplay;
using UnityEngine;

namespace RingFlow.Gameplay.Views
{
    // Resolve ambiguity between DG.Tweening.Ease and RingFlow.Gameplay.Ease (UIThemeConfigSO)
    using Ease = DG.Tweening.Ease;
    /// <summary>
    /// Handles ring move/undo animations, celebration effects, and VFX playback.
    /// Extracted from BoardView to reduce its responsibilities.
    /// All methods are designed to be called from BoardMediator via BoardView delegation.
    /// </summary>
    public class RingAnimationHandler
    {
        private readonly BoardView _boardView;
        private readonly GameFeelConfigSO F;
        private readonly SettingsModel _settingsModel;
        private readonly IHapticService _hapticService;
        private readonly IAudioService _audioService;
        private readonly IProceduralAudioService _proceduralAudio;
        private readonly IObjectPoolService _objectPoolService;
        private readonly VfxPrefabRegistry _vfxRegistry;
        private readonly RingMaterialManager _ringMaterialManager;

        /// <summary>
        /// Pole ID currently being animated. Used to prevent ApplySelection from
        /// fighting the active jump tween with a DOLocalMoveY on the destination pole.
        /// </summary>
        public int AnimatingTargetPoleId { get; set; } = -1;

        // Pre-allocated buffers for celebration (zero-GC during gameplay)
        private readonly List<Transform> _celebrationRingBuffer = new(8);
        private static readonly System.Comparison<Transform> _ringYComparer =
            (a, b) => a.localPosition.y.CompareTo(b.localPosition.y);

        public RingAnimationHandler(
            BoardView boardView,
            GameFeelConfigSO feelConfig,
            SettingsModel settingsModel,
            IHapticService hapticService,
            IAudioService audioService,
            IProceduralAudioService proceduralAudio,
            IObjectPoolService objectPoolService,
            VfxPrefabRegistry vfxRegistry,
            RingMaterialManager ringMaterialManager)
        {
            _boardView = boardView;
            F = feelConfig;
            _settingsModel = settingsModel;
            _hapticService = hapticService;
            _audioService = audioService;
            _proceduralAudio = proceduralAudio;
            _objectPoolService = objectPoolService;
            _vfxRegistry = vfxRegistry;
            _ringMaterialManager = ringMaterialManager;
        }

        // ── Move Animation ────────────────────────────────────────────────

        /// <summary>
        /// Animates a ring moving from one pole to another with a jump tween.
        /// Must be called after BuildBoard has updated the visual state.
        /// </summary>
        public void AnimateRingMove(
            int fromPoleId, int toPoleId, List<PoleState> poles,
            System.Func<int, GameObject> getRing,
            System.Func<int, GameObject> getPoleObj,
            System.Func<int, int> getRingCount,
            System.Action applySelection)
        {
            bool reduceMotion = _settingsModel != null && _settingsModel.ReduceMotion.Value;
            bool slowMode = _settingsModel != null && _settingsModel.SlowMode.Value;
            float speed = slowMode ? F.SlowModeMultiplier : 1f;
            float duration = F.MoveDuration * speed;

            // Capture start position
            Vector3 oldRingWorldPos = Vector3.zero;
            RingColor movedColor = RingColor.None;
            if (fromPoleId >= 0)
            {
                var topRing = getRing(fromPoleId);
                if (topRing != null)
                {
                    oldRingWorldPos = topRing.transform.position;
                    if (fromPoleId < poles.Count && poles[fromPoleId].Rings.Count > 0)
                        movedColor = poles[fromPoleId].Rings[^1].Color;
                }
            }

            AnimatingTargetPoleId = toPoleId;

            _boardView.BuildBoard(poles);

            var movedRing = getRing(toPoleId);
            if (movedRing != null)
            {
                int backRowCount = Mathf.CeilToInt(poles.Count / 2.0f);
                float scaleFactor = (poles.Count > 5 && toPoleId < backRowCount) ? 0.85f : 1.0f;

                DOTween.Kill(movedRing.transform);
                movedRing.transform.position = oldRingWorldPos;

                int ringIndex = getRingCount(toPoleId) - 1;
                var toPoleObj = getPoleObj(toPoleId);
                var toPoleScale = toPoleObj.transform.localScale;
                float targetWorldY = (0.22f + (ringIndex * (F.RingStackSpacing * F.PoleScale.y))) * scaleFactor;
                var targetLocal = new Vector3(0f, targetWorldY / toPoleScale.y, 0f);

                float localX = (F.RingTargetWidth * scaleFactor) / toPoleScale.x;
                float localY = ((F.RingTargetHeight / F.RingMeshHeight) * scaleFactor) / toPoleScale.y;
                float localZ = (F.RingTargetWidth * scaleFactor) / toPoleScale.z;
                Vector3 normalScale = new Vector3(localX, localY, localZ);

                var movedRingTransform = movedRing.transform;
                var movedRingGameObject = movedRing.gameObject;

                if (reduceMotion)
                {
                    movedRing.transform.localPosition = targetLocal;
                    AnimatingTargetPoleId = -1;
                    TriggerMoveEffects(movedRing.transform.position, movedColor);
                    applySelection();
                    return;
                }

                // BUG-4 FIX: Guard fromPoleId before accessing _spawnedPoles list
                var fromPoleObj = fromPoleId >= 0 ? getPoleObj(fromPoleId) : null;
                if (fromPoleObj == null)
                {
                    AnimatingTargetPoleId = -1;
                    return;
                }

                float dist = Vector3.Distance(fromPoleObj.transform.position, toPoleObj.transform.position);
                float worldJumpPower = F.MoveJumpPower + (dist * 0.35f);
                float localJumpPower = worldJumpPower / toPoleScale.y;

                movedRingTransform.DOLocalJump(targetLocal, localJumpPower, 1, duration)
                    .SetEase(Ease.InOutQuad)
                    .SetAutoKill(true)
                    .OnComplete(() =>
                    {
                        if (movedRingGameObject == null || movedRingTransform == null) return;
                        AnimatingTargetPoleId = -1;
                        TriggerMoveEffects(movedRingTransform.position, movedColor);
                        _hapticService?.Vibrate(HapticType.Light);
                        DOTween.Kill(movedRingTransform);
                        movedRingTransform.DOScale(new Vector3(localX * 1.25f, localY * 0.6f, localZ * 1.25f), 0.08f)
                            .SetEase(Ease.OutQuad)
                            .OnComplete(() =>
                            {
                                if (movedRingGameObject == null || movedRingTransform == null) return;
                                movedRingTransform.DOScale(normalScale, 0.18f).SetEase(Ease.OutBack);
                            });
                        applySelection();
                    });

                movedRingTransform.DOScale(new Vector3(localX * 0.85f, localY * 1.35f, localZ * 0.85f), duration * 0.4f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        if (movedRingGameObject == null || movedRingTransform == null) return;
                        movedRingTransform.DOScale(normalScale, duration * 0.4f).SetEase(Ease.InQuad);
                    });
            }
            else
            {
                AnimatingTargetPoleId = -1;
            }
        }

        // ── Undo Animation ────────────────────────────────────────────────

        /// <summary>
        /// Animates a ring undo by reversing a previous from→to move.
        /// </summary>
        public void AnimateRingUndo(
            int fromPoleId, int toPoleId, List<PoleState> poles,
            System.Func<int, GameObject> getRing,
            System.Func<int, GameObject> getPoleObj,
            System.Func<int, int> getRingCount,
            System.Action applySelection)
        {
            if (fromPoleId < 0 || toPoleId < 0 || poles == null) return;
            if (fromPoleId >= poles.Count || toPoleId >= poles.Count) return;

            bool reduceMotion = _settingsModel != null && _settingsModel.ReduceMotion.Value;
            bool slowMode = _settingsModel != null && _settingsModel.SlowMode.Value;
            float speed = slowMode ? F.SlowModeMultiplier : 1f;
            float duration = F.MoveDuration * speed;

            // Capture ring position on toPole BEFORE rebuild
            Vector3 oldRingWorldPos = Vector3.zero;
            RingColor movedColor = RingColor.None;
            var ringBefore = getRing(toPoleId);
            if (ringBefore != null)
            {
                oldRingWorldPos = ringBefore.transform.position;
                if (fromPoleId < poles.Count && poles[fromPoleId].Rings.Count > 0)
                    movedColor = poles[fromPoleId].TopRing.Color;
            }

            AnimatingTargetPoleId = fromPoleId;
            _boardView.BuildBoard(poles);

            var movedRing = getRing(fromPoleId);
            if (movedRing == null)
            {
                AnimatingTargetPoleId = -1;
                return;
            }

            var movedRingTransform = movedRing.transform;
            var movedRingGameObject = movedRing.gameObject;

            DOTween.Kill(movedRingTransform);
            movedRingTransform.position = oldRingWorldPos;

            int ringIndex = getRingCount(fromPoleId) - 1;
            var fromPoleObj = getPoleObj(fromPoleId);
            var fromPoleScale = fromPoleObj.transform.localScale;
            int backRowCount = Mathf.CeilToInt(poles.Count / 2.0f);
            float scaleFactor = (poles.Count > 5 && fromPoleId < backRowCount) ? 0.85f : 1.0f;
            float targetWorldY = (0.22f + (ringIndex * (F.RingStackSpacing * F.PoleScale.y))) * scaleFactor;
            var targetLocal = new Vector3(0f, targetWorldY / fromPoleScale.y, 0f);

            if (reduceMotion)
            {
                movedRing.transform.localPosition = targetLocal;
                AnimatingTargetPoleId = -1;
                TriggerMoveEffects(movedRing.transform.position, movedColor);
                _hapticService?.Vibrate(HapticType.Light);
                applySelection();
                return;
            }

            var toPoleObj = getPoleObj(toPoleId);
            float dist = Vector3.Distance(toPoleObj.transform.position, fromPoleObj.transform.position);
            float worldJumpPower = F.MoveJumpPower + (dist * 0.35f);
            float localJumpPower = worldJumpPower / fromPoleScale.y;

            movedRingTransform.DOLocalJump(targetLocal, localJumpPower, 1, duration)
                .SetEase(Ease.InOutQuad)
                .SetAutoKill(true)
                .OnComplete(() =>
                {
                    if (movedRingGameObject == null || movedRingTransform == null) return;
                    AnimatingTargetPoleId = -1;
                    TriggerMoveEffects(movedRingTransform.position, movedColor);
                    _hapticService?.Vibrate(HapticType.Light);
                    applySelection();
                });
        }

        // ── Celebration ───────────────────────────────────────────────────

        public void CelebratePoleComplete(
            int poleId, int ringCount, int completedCount, bool isFinalPole,
            System.Func<int, GameObject> getPoleObj,
            System.Func<int, int, GameObject> getRingAt,
            System.Func<int> getPoleCount)
        {
            var poleObj = getPoleObj(poleId);
            if (poleObj == null) return;

            int tier = isFinalPole ? 2 : (completedCount >= F.MediumTierThreshold ? 1 : 0);

            // Flash pole with success color
            float flashDuration = isFinalPole ? F.PoleSuccessFlashDuration * 1.5f : F.PoleSuccessFlashDuration;
            Color successColor = F.PoleSuccessFlashColor;
            if (_settingsModel != null && _settingsModel.ColorBlindMode.Value > 0)
                successColor = Color.Lerp(successColor, Color.cyan, 0.4f);
            var pv = poleObj.GetComponent<PoleView>();
            if (pv != null) pv.FlashSuccess(flashDuration, successColor);

            // Haptic
            _hapticService?.Vibrate(isFinalPole ? HapticType.Success : HapticType.Medium);

            // Audio
            if (_audioService != null)
            {
                if (isFinalPole)
                    _audioService.PlaySfx(_proceduralAudio.GetOrCreateFinalPoleClip(), 1.0f);
                else
                    _audioService.PlaySfx(_proceduralAudio.GetOrCreateRichPoleCompleteClip(ringCount), 1.0f);
            }

            // Camera shake
            float shakeIntensity = isFinalPole ? F.CompleteShakeIntensity * 2f : F.CompleteShakeIntensity;
            float shakeDuration = isFinalPole ? F.CompleteShakeDuration * 2f : F.CompleteShakeDuration;

            // Staggered ring bounce
            _celebrationRingBuffer.Clear();
            for (int c = 0; c < poleObj.transform.childCount; c++)
            {
                var child = poleObj.transform.GetChild(c);
                if (child.name.Length > 5 && child.name[0] == 'R' && child.name[4] == '_')
                    _celebrationRingBuffer.Add(child);
            }
            _celebrationRingBuffer.Sort(_ringYComparer);

            for (int i = 0; i < _celebrationRingBuffer.Count; i++)
            {
                var ringTrans = _celebrationRingBuffer[i];
                float originalY = ringTrans.localPosition.y;
                float originalScaleY = ringTrans.localScale.y;
                float bounceHeight = isFinalPole ? 0.5f : 0.35f;

                var seq = DOTween.Sequence().SetTarget(ringTrans).SetAutoKill(true);
                seq.AppendInterval(i * 0.04f);
                seq.Append(ringTrans.DOLocalMoveY(originalY + bounceHeight, 0.15f).SetEase(Ease.OutQuad));
                seq.Append(ringTrans.DOLocalMoveY(originalY, 0.20f).SetEase(Ease.InQuad));
                seq.Append(ringTrans.DOScaleY(originalScaleY * 0.7f, 0.08f).SetEase(Ease.OutQuad));
                seq.Append(ringTrans.DOScaleY(originalScaleY, 0.12f).SetEase(Ease.OutBack));
            }

            // Merge effect
            Vector3 poleTopPos = poleObj.transform.position + Vector3.up * 1.5f;
            Color mergeColor = isFinalPole ? _ringMaterialManager.GetRingColor(RingColor.Yellow) : Color.white;
            SpawnMergeEffect(poleTopPos, mergeColor, ringCount, isFinalPole);

            if (tier >= 1)
                SpawnConfettiBurst(poleTopPos + Vector3.up * 0.3f, 8);

            if (isFinalPole)
            {
                SpawnConfettiBurst(poleTopPos + Vector3.up * 1f, 24);
                int poleCount = getPoleCount();
                for (int i = 0; i < poleCount; i++)
                {
                    if (i == poleId) continue;
                    var otherPole = getPoleObj(i);
                    if (otherPole != null)
                    {
                        var dp = otherPole.transform.position + Vector3.up * 2f;
                        SpawnMergeEffectBurst(dp, Color.Lerp(mergeColor, Color.white, 0.3f));
                    }
                }
            }
        }

        // ── VFX Methods ───────────────────────────────────────────────────

        public void TriggerMoveEffects(Vector3 position, RingColor color)
        {
            if (_audioService != null)
                _audioService.PlaySfx(_proceduralAudio.GetOrCreateMoveClip(), 1.0f, 0.92f, 1.08f);
            SpawnRingPop(position, _ringMaterialManager.GetRingColor(color));
        }

        public void PlayChainLinkVfx(Vector3 fromPos, Vector3 toPos, Color color)
        {
            if (_audioService != null)
                _audioService.PlaySfx(_proceduralAudio.GetOrCreateChainClip(), 1.0f, 0.9f, 1.1f);
            SpawnMergeEffectBurst(fromPos, color);
            SpawnMergeEffectBurst(toPos, color);
            _hapticService?.Vibrate(HapticType.Light);
        }

        public void PlayMagnetPullVfx(Vector3 targetPos, int pulledCount, Color color)
        {
            if (_audioService != null)
                _audioService.PlaySfx(_proceduralAudio.GetOrCreateMagnetClip(), 1.0f, 0.9f, 1.1f);
            for (int i = 0; i < Mathf.Min(pulledCount, 3); i++)
            {
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-0.3f, 0.3f),
                    UnityEngine.Random.Range(0f, 0.5f),
                    UnityEngine.Random.Range(-0.3f, 0.3f));
                SpawnMergeEffectBurst(targetPos + offset, color);
            }
            _hapticService?.Vibrate(HapticType.Medium);
        }

        public void PlayPaintSplashVfx(Vector3 position, Color color)
        {
            if (_audioService != null)
                _audioService.PlaySfx(_proceduralAudio.GetOrCreatePaintClip(), 1.0f, 0.92f, 1.08f);
            SpawnMergeEffectBurst(position, color);
        }

        public void PlayIceBreakVfx(Vector3 position, Color color)
        {
            if (_audioService != null)
                _audioService.PlaySfx(_proceduralAudio.GetOrCreateIceBreakClip(), 1.0f, 0.88f, 1.12f);
            SpawnMergeEffectBurst(position, color);
            _hapticService?.Vibrate(HapticType.Medium);
        }

        public void PlayStoneThudVfx(Vector3 position, Color color)
        {
            if (_audioService != null)
                _audioService.PlaySfx(_proceduralAudio.GetOrCreateStoneImpactClip(), 0.8f, 0.95f, 1.05f);
            SpawnMergeEffectBurst(position, color);
        }

        public void PlayBombExplosionVfx(Vector3 position)
        {
            if (_audioService != null)
                _audioService.PlaySfx(_proceduralAudio.GetOrCreateBombExplosionClip(), 1.0f);
            SpawnConfettiBurst(position, 16);
            _hapticService?.Vibrate(HapticType.Heavy);
        }

        public void PlayPortalTeleportVfx(Vector3 fromPos, Vector3 toPos)
        {
            if (_audioService != null)
                _audioService.PlaySfx(_proceduralAudio.GetOrCreatePortalClip(), 1.0f);
            SpawnMergeEffectBurst(fromPos, new Color(0.5f, 0f, 1f));
            SpawnMergeEffectBurst(toPos, new Color(0.5f, 0f, 1f));
        }

        public void FlashPoleError(GameObject poleObj)
        {
            if (poleObj == null) return;
            var pv = poleObj.GetComponent<PoleView>();
            if (pv != null) pv.FlashError();
            _hapticService?.Vibrate(HapticType.Warning);
            if (_audioService != null)
                _audioService.PlaySfx(_proceduralAudio.GetOrCreateErrorClip(), 1.0f);
        }

        // ── Internal VFX spawning ─────────────────────────────────────────

        private void SpawnMergeEffect(Vector3 position, Color color, int ringCount, bool isFinalPole)
        {
            if (_vfxRegistry == null || _objectPoolService == null) return;
            var prefab = _vfxRegistry.GetMergeEffectPrefab();
            if (prefab == null)
            {
                SpawnRingPop(position, color);
                return;
            }
            var instance = _objectPoolService.Spawn(prefab, position, Quaternion.identity);
            instance?.GetComponent<MergeEffectVfx>()?.Initialize(position, color, ringCount, isFinalPole);
        }

        private void SpawnMergeEffectBurst(Vector3 position, Color color)
        {
            if (_vfxRegistry == null || _objectPoolService == null) return;
            var prefab = _vfxRegistry.GetMergeEffectPrefab();
            if (prefab == null) return;
            var instance = _objectPoolService.Spawn(prefab, position, Quaternion.identity);
            instance?.GetComponent<MergeEffectVfx>()?.InitializeBurstOnly(position, color);
        }

        private void SpawnRingPop(Vector3 position, Color color)
        {
            if (_vfxRegistry == null || _objectPoolService == null) return;
            var prefab = _vfxRegistry.GetRingPopPrefab();
            if (prefab == null) return;
            var instance = _objectPoolService.Spawn(prefab, position, Quaternion.identity);
            instance?.GetComponent<RingPopVfx>()?.Initialize(color);
        }

        private void SpawnConfettiBurst(Vector3 position, int count)
        {
            if (_vfxRegistry == null || _objectPoolService == null) return;
            var prefab = _vfxRegistry.GetConfettiPrefab();
            if (prefab == null) return;
            int bursts = Mathf.Max(1, count / 8);
            for (int i = 0; i < bursts; i++)
            {
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-0.5f, 0.5f),
                    UnityEngine.Random.Range(0f, 0.3f),
                    UnityEngine.Random.Range(-0.5f, 0.5f));
                var instance = _objectPoolService.Spawn(prefab, position + offset, Quaternion.identity);
                instance?.GetComponent<ConfettiVfx>()?.Initialize();
            }
        }

        /// <summary>Clean up all active tweens.</summary>
        public void KillAllTweens()
        {
            AnimatingTargetPoleId = -1;
        }
    }
}

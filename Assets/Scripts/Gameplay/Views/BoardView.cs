using System.Collections.Generic;
using DG.Tweening;
using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;

namespace RingFlow.Gameplay
{
    [Mediator(typeof(BoardMediator))]
    public class BoardView : View
    {
        [UnityEngine.Scripting.Preserve]
        [SerializeField] private GameObject _torusPrefab;

        public void SetTorusPrefab(GameObject prefab) { _torusPrefab = prefab; }

        private static Shader _cachedShader;
        private static Font _cachedBuiltinFont;
        private GameObject _polePrefab;

        [Inject] private IObjectPoolService _objectPoolService;
        [Inject] private VfxPrefabRegistry _vfxRegistry;
        [Inject] private IAudioService _audioService;
        [Inject] private IHapticService _hapticService;
        [Inject] private SettingsModel _settingsModel;
        [Inject] private GameFeelConfigSO _feelConfig;
        [Inject] private RingColorPaletteSO _colorPalette;

        [Inject] private Camera _mainCamera;
        private GameFeelConfigSO F => _feelConfig;

        private Color GetRingColor(RingColor color)
        {
            if (_colorPalette == null)
            {
                throw new System.InvalidOperationException("[BoardView] RingColorPaletteSO is not injected!");
            }
            var mode = _settingsModel != null ? (RingColorPaletteSO.ColorBlindMode)_settingsModel.ColorBlindMode.Value : RingColorPaletteSO.ColorBlindMode.Off;
            return _colorPalette.GetColor(color, mode);
        }

        private readonly Dictionary<(RingColor, RingType), Material> _ringMaterialCache = new();
        // Reused across ApplySelection calls to avoid per-ring MaterialPropertyBlock allocations.
        // NOT a field initializer: creating engine objects in a MonoBehaviour constructor / field
        // initializer is forbidden (Unity throws "CreateImpl is not allowed from a MonoBehaviour
        // constructor"), and the resulting abort would also skip _spawnedPoles initialization and
        // surface as a NullReferenceException at BuildBoard. Lazily initialized instead.
        private MaterialPropertyBlock _selectionPropBlock;

        private readonly List<PoleView> _spawnedPoles = new();
        private readonly List<List<GameObject>> _spawnedRings = new();
        // Pre-allocated buffers for celebration animation (avoids GC during gameplay)
        private readonly List<Transform> _celebrationRingBuffer = new(8);
        private static readonly System.Comparison<Transform> _ringYComparer =
            (a, b) => a.localPosition.y.CompareTo(b.localPosition.y);
        private int _lastSelectedPoleId = -1;
        private int _animatingTargetPoleId = -1;
        private bool _ringPrewarmed;
        private Mesh _proceduralTorusMesh;
        private GameObject _floorPlane;
        private GameObject _tutorialArrowGo;
        private GameObject _tutorialCanvasGo;
        private UnityEngine.UI.Text _tutorialLabelText;

        private BloomPulseController _bloomPulseController;

        public void EnsureRingPoolPrewarmed()
        {
            if (_ringPrewarmed) return;
            if (_torusPrefab == null)
            {
                NexusLog.Error("BoardView", nameof(EnsureRingPoolPrewarmed), "",
                    "_torusPrefab null — Torus prefab DI ile enjekte edilmemis. " +
                    "Ring havuzu ön ısıtması iptal.");
                return;
            }
            if (_objectPoolService != null)
            {
                _objectPoolService.Prewarm(_torusPrefab, F.RingPoolSize);
                _ringPrewarmed = true;
            }
        }

        public void BuildBoard(List<PoleState> poles)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            int totalRings = 0;
            for (int dbg = 0; dbg < poles.Count; dbg++) totalRings += poles[dbg]?.Rings.Count ?? 0;
            NexusLog.Info("BoardView", nameof(BuildBoard), poles.Count.ToString(),
                $"Rebuilding board: {poles.Count} poles, {totalRings} total rings, _animatingTargetPoleId={_animatingTargetPoleId}.");
#endif
            // Incremental sync: return previously spawned poles/rings to their pool
            // before re-spawning from the current pole data. Preserves pool integrity
            // and avoids destroying the visual root hierarchy (which would orphan the
            // pool and break Undo/Move visual restore).
            ClearBoard();

            EnsureRingPoolPrewarmed();
            EnsureFloorPlaneCreated();

            float spacing = F.PoleSpacing;
            float boardWidth = (poles.Count - 1) * spacing;
            float startX = -boardWidth * 0.5f;

            for (int p = 0; p < poles.Count; p++)
            {
                var poleData = poles[p];
                PoleView poleView;
                GameObject poleObj;

                if (p < _spawnedPoles.Count && _spawnedPoles[p] != null)
                {
                    poleView = _spawnedPoles[p];
                    poleObj = poleView.gameObject;
                }
                else
                {
                    poleObj = AcquirePole();
                    poleView = poleObj.GetComponent<PoleView>();
                    if (poleView == null) poleView = poleObj.AddComponent<PoleView>();
                    poleObj.name = $"Pole_{p}" + (poleData.IsLocked ? " [LOCKED]" : "");
                    poleObj.transform.SetParent(transform, false);
                }

                poleObj.SetActive(true);
                poleView.PoleId = p;
                poleView.SetLocked(poleData.IsLocked);
                poleObj.transform.localPosition = new Vector3(startX + p * spacing, F.PoleYPosition, 0f);
                poleObj.transform.localRotation = Quaternion.identity;
                var poleScale = F.PoleScale;
                int poleCap = poleData.RingCapacity;
                if (poleCap <= 0)
                    throw new System.InvalidOperationException(
                        $"[BoardView] Pole {p} has RingCapacity={poleCap}. Data must have a positive RingCapacity value. " +
                        "Check LevelData or pole configuration.");
                poleScale.y = F.PoleScale.y * (poleCap / (float)F.PoleScaleFullCapacity);
                poleObj.transform.localScale = poleScale;


                var renderers = poleObj.GetComponentsInChildren<Renderer>(true);
                var poleMat = GetPoleMaterial(poleData.IsLocked);
                foreach (var r in renderers)
                {
                    r.sharedMaterial = poleMat;
                }
                poleView.SyncMaterial();

                while (_spawnedPoles.Count <= p) _spawnedPoles.Add(null);
                _spawnedPoles[p] = poleView;

                while (_spawnedRings.Count <= p) _spawnedRings.Add(new List<GameObject>());

                var ringList = _spawnedRings[p];
                int existingRingCount = ringList.Count;
                int neededRingCount = poleData.Rings.Count;

                while (ringList.Count > neededRingCount)
                {
                    int lastIdx = ringList.Count - 1;
                    if (ringList[lastIdx] != null) RecycleRing(ringList[lastIdx]);
                    ringList.RemoveAt(lastIdx);
                }

                for (int r = 0; r < neededRingCount; r++)
                {
                    var ringData = poleData.Rings[r];
                    GameObject ringObj;
                    bool isNew = r >= existingRingCount || ringList[r] == null;

                    if (isNew)
                    {
                        ringObj = AcquireRing();
                        ringObj.transform.SetParent(poleObj.transform, false);
                        if (r >= ringList.Count) ringList.Add(ringObj);
                        else ringList[r] = ringObj;
                    }
                    else { ringObj = ringList[r]; ringObj.SetActive(true); }

                    ringObj.name = $"Ring_{r}_{ringData.Color}_{ringData.Type}";

                    float targetY = F.RingBaseYOffset + (r * F.RingStackSpacing);
                    if (p == _lastSelectedPoleId && r == neededRingCount - 1)
                    {
                        bool reduceMotion = _settingsModel != null && _settingsModel.ReduceMotion.Value;
                        if (!reduceMotion) targetY += F.RingSelectionLift;
                    }

                    ringObj.transform.localPosition = new Vector3(0f, targetY, 0f);
                    ringObj.transform.localRotation = Quaternion.identity;

                    float localX = F.RingTargetWidth / poleObj.transform.localScale.x;
                    float localY = (F.RingTargetHeight / F.RingMeshHeight) / poleObj.transform.localScale.y;
                    float localZ = F.RingTargetWidth / poleObj.transform.localScale.z;
                    ringObj.transform.localScale = new Vector3(localX, localY, localZ);

                    var ringRenderer = ringObj.GetComponentInChildren<Renderer>();
                    if (ringRenderer != null)
                        ringRenderer.sharedMaterial = GetRingMaterial(ringData.Color, ringData.Type);

                    AddSpecialOverlay(ringObj, ringData);
                }
            }

            while (_spawnedPoles.Count > poles.Count)
            {
                int lastIdx = _spawnedPoles.Count - 1;
                if (_spawnedPoles[lastIdx] != null) RecyclePole(_spawnedPoles[lastIdx].gameObject);
                _spawnedPoles.RemoveAt(lastIdx);
            }
            while (_spawnedRings.Count > poles.Count)
            {
                int lastIdx = _spawnedRings.Count - 1;
                foreach (var ring in _spawnedRings[lastIdx])
                    if (ring != null) RecycleRing(ring);
                _spawnedRings.RemoveAt(lastIdx);
            }

            ApplySelection();
        }

        public void AnimateRingMove(int fromPoleId, int toPoleId, List<PoleState> poles)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("BoardView", nameof(AnimateRingMove), $"{fromPoleId}->{toPoleId}",
                $"Move animation started. reduceMotion={_settingsModel?.ReduceMotion.Value}");
#endif
            bool reduceMotion = _settingsModel != null && _settingsModel.ReduceMotion.Value;
            bool slowMode = _settingsModel != null && _settingsModel.SlowMode.Value;
            float speed = slowMode ? F.SlowModeMultiplier : 1f;
            float duration = F.MoveDuration * speed;

            Vector3 oldRingWorldPos = Vector3.zero;
            RingColor movedColor = RingColor.None;
            if (fromPoleId >= 0 && fromPoleId < _spawnedRings.Count && _spawnedRings[fromPoleId].Count > 0)
            {
                var topRing = _spawnedRings[fromPoleId][^1];
                if (topRing != null)
                {
                    oldRingWorldPos = topRing.transform.position;
                    if (fromPoleId < poles.Count && poles[fromPoleId].Rings.Count > 0)
                        movedColor = poles[fromPoleId].Rings[^1].Color;
                }
                else
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    NexusLog.Warn("BoardView", nameof(AnimateRingMove), $"{fromPoleId}->{toPoleId}",
                        $"Top ring on fromPole {fromPoleId} is null — cannot capture start position. Animation will jump from origin.");
#endif
                }
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Warn("BoardView", nameof(AnimateRingMove), $"{fromPoleId}->{toPoleId}",
                    $"fromPoleId {fromPoleId} out of _spawnedRings range ({_spawnedRings.Count}) or empty. No start position captured.");
#endif
            }

            // FIX-U1: Set _animatingTargetPoleId BEFORE BuildBoard so ApplySelection
            // (called inside BuildBoard) skips the destination pole's top ring tween.
            // Without this, ApplySelection would launch a DOLocalMoveY on toPole's top
            // ring the instant it is placed — fighting the jump tween and causing a stutter
            // on that pole as well as incorrectly lifting rings on all other poles.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("BoardView", nameof(AnimateRingMove), $"{fromPoleId}->{toPoleId}",
                $"Setting _animatingTargetPoleId={toPoleId} before BuildBoard.");
#endif
            _animatingTargetPoleId = toPoleId;

            BuildBoard(poles);

            if (toPoleId >= 0 && toPoleId < _spawnedRings.Count && _spawnedRings[toPoleId].Count > 0)
            {
                var movedRing = _spawnedRings[toPoleId][^1];
                if (movedRing != null)
                {
                    DOTween.Kill(movedRing.transform);
                    movedRing.transform.position = oldRingWorldPos;
                    int ringIndex = _spawnedRings[toPoleId].Count - 1;
                    var targetLocal = new Vector3(0f, F.RingBaseYOffset + (ringIndex * F.RingStackSpacing), 0f);

                    if (reduceMotion)
                    {
                        movedRing.transform.localPosition = targetLocal;
                        _animatingTargetPoleId = -1;
                        TriggerMoveEffects(movedRing.transform.position, movedColor);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        NexusLog.Info("BoardView", nameof(AnimateRingMove), $"{fromPoleId}->{toPoleId}",
                            "ReduceMotion: snap complete, _animatingTargetPoleId reset.");
#endif
                        ApplySelection();
                    }
                    else
                    {
                        float localX = F.RingTargetWidth / F.PoleScale.x;
                        float localY = (F.RingTargetHeight / F.RingMeshHeight) / F.PoleScale.y;
                        float localZ = F.RingTargetWidth / F.PoleScale.z;
                        Vector3 normalScale = new Vector3(localX, localY, localZ);

                        DOTween.Kill(movedRing.transform);

                        movedRing.transform.DOLocalJump(targetLocal, F.MoveJumpPower, 1, duration)
                            .SetEase(Ease.InOutQuad)
                            .SetAutoKill(true)
                            .OnComplete(() =>
                            {
                                _animatingTargetPoleId = -1;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                                NexusLog.Info("BoardView", nameof(AnimateRingMove), $"{fromPoleId}->{toPoleId}",
                                    "Jump complete, _animatingTargetPoleId reset.");
#endif
                                TriggerMoveEffects(movedRing.transform.position, movedColor);
                                _hapticService?.Vibrate(HapticType.Light);

                                DOTween.Kill(movedRing.transform);
                                movedRing.transform.DOScale(new Vector3(localX * 1.25f, localY * 0.6f, localZ * 1.25f), 0.08f)
                                    .SetEase(Ease.OutQuad)
                                    .SetAutoKill(true)
                                    .OnComplete(() =>
                                    {
                                        movedRing.transform.DOScale(normalScale, 0.18f).SetEase(Ease.OutBack).SetAutoKill(true);
                                    });

                                ApplySelection();
                            });

                        movedRing.transform.DOScale(new Vector3(localX * 0.85f, localY * 1.35f, localZ * 0.85f), duration * 0.4f)
                            .SetEase(Ease.OutQuad)
                            .SetAutoKill(true)
                            .OnComplete(() =>
                            {
                                movedRing.transform.DOScale(normalScale, duration * 0.4f).SetEase(Ease.InQuad).SetAutoKill(true);
                            });
                    }
                }
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Warn("BoardView", nameof(AnimateRingMove), $"{fromPoleId}->{toPoleId}",
                    $"toPoleId {toPoleId} out of _spawnedRings range or empty after BuildBoard. _animatingTargetPoleId reset.");
#endif
                _animatingTargetPoleId = -1;
            }
        }

        public void AnimateRingUndo(int fromPoleId, int toPoleId, List<PoleState> poles)
        {
            if (fromPoleId < 0 || toPoleId < 0) return;
            if (poles == null) return;
            if (fromPoleId >= poles.Count || toPoleId >= poles.Count) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("BoardView", nameof(AnimateRingUndo), $"{toPoleId}->{fromPoleId}",
                $"Undo animation started. Original move was {fromPoleId}->{toPoleId}. reduceMotion={_settingsModel?.ReduceMotion.Value}");
#endif

            bool reduceMotion = _settingsModel != null && _settingsModel.ReduceMotion.Value;
            bool slowMode = _settingsModel != null && _settingsModel.SlowMode.Value;
            float speed = slowMode ? F.SlowModeMultiplier : 1f;
            float duration = F.MoveDuration * speed;

            // The original move was from→to. Undo reverses it: to→from.
            // Capture the ring's current visual position on toPole BEFORE rebuilding.
            Vector3 oldRingWorldPos = Vector3.zero;
            RingColor movedColor = RingColor.None;
            if (toPoleId < _spawnedRings.Count && _spawnedRings[toPoleId].Count > 0)
            {
                var topRing = _spawnedRings[toPoleId][^1];
                if (topRing != null)
                {
                    oldRingWorldPos = topRing.transform.position;
                    // Best-effort: find the ring color from the model snapshot on fromPole
                    // (the ring is now on fromPole after board revert).
                    if (fromPoleId < poles.Count && poles[fromPoleId].Rings.Count > 0)
                        movedColor = poles[fromPoleId].TopRing.Color;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    NexusLog.Info("BoardView", nameof(AnimateRingUndo), $"{toPoleId}->{fromPoleId}",
                        $"Captured ring world pos={oldRingWorldPos}, color={movedColor} from toPole {toPoleId}.");
#endif
                }
                else
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    NexusLog.Warn("BoardView", nameof(AnimateRingUndo), $"{toPoleId}->{fromPoleId}",
                        $"Top ring on toPole {toPoleId} is null before rebuild — cannot capture start position.");
#endif
                }
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Warn("BoardView", nameof(AnimateRingUndo), $"{toPoleId}->{fromPoleId}",
                    $"toPoleId {toPoleId} has no visual rings before rebuild — undo animation will snap without jump.");
#endif
            }

            // FIX: Set _animatingTargetPoleId BEFORE BuildBoard so that ApplySelection
            // (called inside BuildBoard) correctly skips the undo-animating pole.
            // Previously this was set AFTER BuildBoard, causing ApplySelection to
            // launch a DOLocalMoveY tween on fromPole's top ring before the undo
            // jump tween started — producing a visible stutter on all other poles.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("BoardView", nameof(AnimateRingUndo), $"{toPoleId}->{fromPoleId}",
                $"Setting _animatingTargetPoleId={fromPoleId} before BuildBoard.");
#endif
            _animatingTargetPoleId = fromPoleId;

            // Rebuild board to match reverted model state (ring now on fromPole).
            BuildBoard(poles);

            // After rebuild, ring is at fromPole. Move it back to old visual position
            // on toPole, then animate to its correct position on fromPole.
            if (fromPoleId < _spawnedRings.Count && _spawnedRings[fromPoleId].Count > 0)
            {
                var movedRing = _spawnedRings[fromPoleId][^1];
                if (movedRing == null)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    NexusLog.Warn("BoardView", nameof(AnimateRingUndo), $"{toPoleId}->{fromPoleId}",
                        $"Top ring on fromPole {fromPoleId} is null after BuildBoard — undo animation aborted, _animatingTargetPoleId reset.");
#endif
                    _animatingTargetPoleId = -1;
                    return;
                }

                DOTween.Kill(movedRing.transform);
                movedRing.transform.position = oldRingWorldPos;

                int ringIndex = _spawnedRings[fromPoleId].Count - 1;
                var targetLocal = new Vector3(0f, F.RingBaseYOffset + (ringIndex * F.RingStackSpacing), 0f);

                // _animatingTargetPoleId already set above — no reassignment needed here.

                if (reduceMotion)
                {
                    movedRing.transform.localPosition = targetLocal;
                    _animatingTargetPoleId = -1;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    NexusLog.Info("BoardView", nameof(AnimateRingUndo), $"{toPoleId}->{fromPoleId}",
                        "ReduceMotion: undo snap complete, _animatingTargetPoleId reset.");
#endif
                    TriggerMoveEffects(movedRing.transform.position, movedColor);
                    _hapticService?.Vibrate(HapticType.Light);
                    ApplySelection();
                    return;
                }

                movedRing.transform.DOLocalJump(targetLocal, F.MoveJumpPower, 1, duration)
                    .SetEase(Ease.InOutQuad)
                    .SetAutoKill(true)
                    .OnComplete(() =>
                    {
                        _animatingTargetPoleId = -1;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        NexusLog.Info("BoardView", nameof(AnimateRingUndo), $"{toPoleId}->{fromPoleId}",
                            "Undo jump complete, _animatingTargetPoleId reset.");
#endif
                        TriggerMoveEffects(movedRing.transform.position, movedColor);
                        _hapticService?.Vibrate(HapticType.Light);
                        ApplySelection();
                    });
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Warn("BoardView", nameof(AnimateRingUndo), $"{toPoleId}->{fromPoleId}",
                    $"fromPoleId {fromPoleId} out of _spawnedRings range or empty after BuildBoard. _animatingTargetPoleId reset.");
#endif
                _animatingTargetPoleId = -1;
            }
        }

        public PoleView GetPoleView(int poleId)
        {
            if (poleId < 0 || poleId >= _spawnedPoles.Count) return null;
            return _spawnedPoles[poleId];
        }

        public void SetSelectedPole(int poleId)
        {
            if (_lastSelectedPoleId != poleId)
            {
                _lastSelectedPoleId = poleId;
                if (poleId >= 0)
                {
                    _hapticService?.Vibrate(HapticType.Selection);
                }
                // FIX-U4: Do not call ApplySelection while a ring is mid-animation.
                // If _animatingTargetPoleId is set, BuildBoard's internal ApplySelection
                // already ran with the correct skip, and the OnComplete callback will
                // call ApplySelection again once the animation finishes. Calling it here
                // a second time would fight the jump tween with a DOLocalMoveY on the
                // animating pole's rings — causing the visual jitter on all other poles.
                if (_animatingTargetPoleId < 0)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    NexusLog.Info("BoardView", nameof(SetSelectedPole), poleId.ToString(),
                        $"Selection changed to {poleId}. Calling ApplySelection.");
#endif
                    ApplySelection();
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                else
                {
                    NexusLog.Info("BoardView", nameof(SetSelectedPole), poleId.ToString(),
                        $"Selection changed to {poleId} but _animatingTargetPoleId={_animatingTargetPoleId} — deferring ApplySelection to OnComplete.");
                }
#endif
            }
        }

        public void FlashPoleError(int poleId)
        {
            var pv = GetPoleView(poleId);
            if (pv == null) return;
            pv.FlashError();
            _hapticService?.Vibrate(HapticType.Warning);
            if (_audioService != null)
                _audioService.PlaySfx(ProceduralAudio.GetOrCreateErrorClip(), 1.0f);
        }

        public void CelebratePoleComplete(int poleId, int ringCount, int completedCount, bool isFinalPole)
        {
            var pv = GetPoleView(poleId);
            if (pv == null) return;

            // --- 3-Tier Feedback System ---
            int tier = isFinalPole ? 2 : (completedCount >= F.MediumTierThreshold ? 1 : 0);

            // ----- Tier 0/1: Flash pole with success color -----
            float flashDuration = isFinalPole ? F.PoleSuccessFlashDuration * 1.5f : F.PoleSuccessFlashDuration;
            // Use colorblind-safe success color (gold → warm amber that works for all vision types)
            Color successColor = F.PoleSuccessFlashColor;
            if (_settingsModel != null && _settingsModel.ColorBlindMode.Value > 0)
                successColor = Color.Lerp(successColor, Color.cyan, 0.4f);
            pv.FlashSuccess(flashDuration, successColor);

            // ----- Tier 0/1/2: Haptic feedback -----
            HapticType hapticType = isFinalPole ? HapticType.Success : HapticType.Medium;
            _hapticService?.Vibrate(hapticType);

            // ----- Tier 0/1: Audio feedback -----
            if (_audioService != null)
            {
                if (isFinalPole)
                    _audioService.PlaySfx(ProceduralAudio.GetOrCreateFinalPoleClip(), 1.0f);
                else
                    _audioService.PlaySfx(ProceduralAudio.GetOrCreateRichPoleCompleteClip(ringCount), 1.0f);
            }

            // ----- Tier 0/1: Camera micro-shake -----
            float shakeIntensity = isFinalPole ? F.CompleteShakeIntensity * 2f : F.CompleteShakeIntensity;
            float shakeDuration = isFinalPole ? F.CompleteShakeDuration * 2f : F.CompleteShakeDuration;
            ShakeCamera(shakeIntensity, shakeDuration);

            // ----- Tier 0/1: Staggered ring bounce animation -----
            // Reuse pre-allocated list to avoid GC pressure during gameplay.
            _celebrationRingBuffer.Clear();
            int childCount = pv.transform.childCount;
            for (int c = 0; c < childCount; c++)
            {
                var child = pv.transform.GetChild(c);
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

                // Use DOTween Sequence to avoid nested OnComplete lambda closures (zero GC).
                // FIX-U5: SetTarget(ringTrans) so DOTween.Kill(ringTrans) — called in
                // RecycleRing and KillTweens — also kills this sequence. Without SetTarget
                // the sequence is not associated with the transform, so Kill(ringTrans) only
                // kills standalone tweens, leaving the sequence running on a recycled/pooled
                // ring object and causing visual corruption on the next undo or level load.
                var seq = DOTween.Sequence().SetTarget(ringTrans).SetAutoKill(true);
                seq.AppendInterval(i * 0.04f);
                seq.Append(ringTrans.DOLocalMoveY(originalY + bounceHeight, 0.15f).SetEase(Ease.OutQuad));
                seq.Append(ringTrans.DOLocalMoveY(originalY, 0.20f).SetEase(Ease.InQuad));
                seq.Append(ringTrans.DOScaleY(originalScaleY * 0.7f, 0.08f).SetEase(Ease.OutQuad));
                seq.Append(ringTrans.DOScaleY(originalScaleY, 0.12f).SetEase(Ease.OutBack));
            }

            // ----- Tier 0/1: Merge effect (replaces legacy RingPop burst) -----
            Vector3 poleTopPos = pv.transform.position + Vector3.up * 1.5f;
            Color mergeColor = isFinalPole ? GetRingColor(RingColor.Yellow) : Color.white;
            SpawnMergeEffect(poleTopPos, mergeColor, ringCount, isFinalPole);

            // ----- Tier 1: Extra sparkle for medium-tier completions -----
            if (tier >= 1)
            {
                SpawnConfettiBurst(poleTopPos + Vector3.up * 0.3f, 8);
            }

            // ----- Tier 2 (Final Pole): Full-screen celebration -----
            if (isFinalPole)
            {
                // Large confetti burst
                SpawnConfettiBurst(poleTopPos + Vector3.up * 1f, 24);

                // Reward particles across the board
                if (_vfxRegistry != null && _objectPoolService != null)
                {
                    var mergePrefab = _vfxRegistry.GetMergeEffectPrefab();
                    if (mergePrefab != null)
                    {
                        for (int i = 0; i < _spawnedPoles.Count; i++)
                        {
                            if (i == poleId) continue;
                            var otherPv = GetPoleView(i);
                            if (otherPv != null)
                            {
                                var dp = otherPv.transform.position + Vector3.up * 2f;
                                var mergeInstance = _objectPoolService.Spawn(mergePrefab, dp, Quaternion.identity);
                                mergeInstance?.GetComponent<MergeEffectVfx>()?.InitializeBurstOnly(dp, Color.Lerp(mergeColor, Color.white, 0.3f));
                            }
                        }
                    }
                }
            }

            // ----- Bloom pulse -----
            var bloom = GetOrFindBloomPulseController();
            if (bloom != null)
            {
                float bloomMultiplier = isFinalPole ? F.FinalBloomIntensityMultiplier : F.BloomIntensityMultiplier;
                float bloomDuration = isFinalPole ? F.FinalBloomPulseDuration : F.BloomPulseDuration;
                bloom.Pulse(bloomMultiplier, bloomDuration, isFinalPole);
            }
        }

        private void SpawnMergeEffect(Vector3 position, Color color, int ringCount, bool isFinalPole)
        {
            if (_vfxRegistry == null || _objectPoolService == null) return;
            var prefab = _vfxRegistry.GetMergeEffectPrefab();
            if (prefab == null)
            {
                // Fallback to legacy RingPop if MergeEffect not available
                var popPrefab = _vfxRegistry.GetRingPopPrefab();
                if (popPrefab != null)
                {
                    var popInstance = _objectPoolService.Spawn(popPrefab, position, Quaternion.identity);
                    popInstance?.GetComponent<RingPopVfx>()?.Initialize(color);
                }
                return;
            }
            var mergeInstance = _objectPoolService.Spawn(prefab, position, Quaternion.identity);
            mergeInstance?.GetComponent<MergeEffectVfx>()?.Initialize(position, color, ringCount, isFinalPole);
        }

        private void SpawnConfettiBurst(Vector3 position, int count)
        {
            if (_vfxRegistry == null || _objectPoolService == null) return;
            var prefab = _vfxRegistry.GetConfettiPrefab();
            if (prefab == null) return;

            // Spawn multiple confetti bursts for a larger effect
            int bursts = Mathf.Max(1, count / 8);
            for (int i = 0; i < bursts; i++)
            {
                Vector3 offset = new Vector3(
                    UnityEngine.Random.Range(-0.5f, 0.5f),
                    UnityEngine.Random.Range(0f, 0.3f),
                    UnityEngine.Random.Range(-0.5f, 0.5f)
                );
                var confettiInstance = _objectPoolService.Spawn(prefab, position + offset, Quaternion.identity);
                confettiInstance?.GetComponent<ConfettiVfx>()?.Initialize();
            }
        }

        private BloomPulseController GetOrFindBloomPulseController()
        {
            // BloomPulseController hiçbir sahnede/preFab'da yer almaz; bu yüzden FindObjectOfType
            // kullanmadan yalnızca bir kez oluşturulur ve önbelleğe alınır (AGENTS.md: asla FindObjectOfType).
            if (_bloomPulseController != null) return _bloomPulseController;
            var go = new GameObject("BloomPulseController", typeof(BloomPulseController));
            go.hideFlags = HideFlags.HideAndDontSave;
            _bloomPulseController = go.GetComponent<BloomPulseController>();
            return _bloomPulseController;
        }

        private MaterialPropertyBlock GetSelectionPropBlock()
        {
            if (_selectionPropBlock == null) _selectionPropBlock = new MaterialPropertyBlock();
            return _selectionPropBlock;
        }

        // FIX-P1: ApplySelection previously called DOTween.Kill + a new tween on EVERY ring
        // on EVERY selection change, even for rings that haven't changed state. With 12 poles
        // each having 4 rings that's up to 48 Kill+tween pairs per tap — well above the GDD
        // §75 GC budget. Fix: track the last selection state per-pole and only animate the
        // top ring of poles whose selection status actually changed this call.
        //
        // FIX-P2: Glow Light child was created with `new GameObject` and destroyed with
        // `Destroy()` each time a ring was selected/deselected. Creating/destroying engine
        // objects inside a tight selection loop generates GC and draw-call overhead.
        // Fix: reuse the existing light child; toggle its enabled state instead of
        // destroy/recreate. The light is created lazily on first selection and persists.
        private void ApplySelection()
        {
            bool reduceMotion = _settingsModel != null && _settingsModel.ReduceMotion.Value;
            bool slowMode = _settingsModel != null && _settingsModel.SlowMode.Value;
            float speed = slowMode ? F.SlowModeMultiplier : 1f;
            float duration = F.SelectionLiftDuration * speed;

            for (int i = 0; i < _spawnedPoles.Count; i++)
            {
                if (_spawnedPoles[i] == null) continue;
                bool isSelected = i == _lastSelectedPoleId;
                _spawnedPoles[i].SetSelected(isSelected);

                // FIX-U2: Skip ALL rings on a pole that is currently being animated by
                // AnimateRingMove or AnimateRingUndo. Previously only the animating pole's
                // top ring tween was skipped, but ALL rings on that pole were still tweened
                // by ApplySelection, causing the other rings on the same pole to jump to
                // their resting Y and then visually snap when the animation completed.
                if (i == _animatingTargetPoleId)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    NexusLog.Info("BoardView", nameof(ApplySelection), i.ToString(),
                        $"Skipping pole {i} — currently animating (_animatingTargetPoleId={_animatingTargetPoleId}).");
#endif
                    continue;
                }

                if (i < _spawnedRings.Count && _spawnedRings[i].Count > 0)
                {
                    var ringList = _spawnedRings[i];
                    int topRingIndex = ringList.Count - 1;
                    var topRing = ringList[topRingIndex];
                    if (topRing == null) continue;

                    // FIX-U3: Only animate the top ring for selection lift/drop.
                    // Non-top rings should snap to their resting position without tweening
                    // because CelebratePoleComplete may have launched bounce sequences on
                    // them; launching another tween here fights those sequences and produces
                    // the visible jitter on all poles during undo.
                    for (int r = 0; r < topRingIndex; r++)
                    {
                        var ring = ringList[r];
                        if (ring == null) continue;
                        float restY = F.RingBaseYOffset + (r * F.RingStackSpacing);
                        // Only snap if the ring is significantly out of place (not mid-celebrate).
                        // We intentionally do NOT kill celebrate tweens here — just let them finish.
                        // We skip DOTween.Kill so celebration bounce sequences are not interrupted.
                        ring.transform.localPosition = new Vector3(0f, ring.transform.localPosition.y, 0f);
                    }

                    int ringIndex = topRingIndex;
                    float targetY = F.RingBaseYOffset + (ringIndex * F.RingStackSpacing) + (isSelected ? F.RingSelectionLift : 0f);

                    DOTween.Kill(topRing.transform);
                    if (reduceMotion)
                    {
                        topRing.transform.localPosition = new Vector3(0f, targetY, 0f);
                    }
                    else
                    {
                        // Capture loop variable for closure — avoids stale reference after loop iteration.
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
                                    capturedRing.transform.DOLocalMoveY(capturedY + F.TutorialArrowBobHeight * 0.4f, 0.6f)
                                        .SetEase(Ease.InOutSine)
                                        .SetLoops(-1, LoopType.Yoyo)
                                        .SetAutoKill(true);
                                }
                            });
                    }

                    // --- Warm Selection Glow (FIX-P2: reuse light, no Destroy/new) ---
                    var ringRenderer = topRing.GetComponentInChildren<Renderer>();
                    var propBlock = GetSelectionPropBlock();
                    propBlock.Clear();

                    // Find or lazily create a persistent glow light under this ring.
                    var lightChildTransform = topRing.transform.Find("SelectionGlowLight");

                    if (isSelected)
                    {
                        if (lightChildTransform == null)
                        {
                            // Create once; reuse on subsequent selections.
                            var lightGo = new GameObject("SelectionGlowLight");
                            lightGo.transform.SetParent(topRing.transform, false);
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
                            // Re-enable if it was disabled by a previous deselect.
                            lightChildTransform.gameObject.SetActive(true);
                        }

                        if (ringRenderer != null)
                        {
                            ringRenderer.GetPropertyBlock(propBlock);
                            propBlock.SetColor("_EmissionColor", F.SelectionEmissionColor);
                            ringRenderer.SetPropertyBlock(propBlock);
                        }
                    }
                    else
                    {
                        // FIX-P2: Disable instead of Destroy — keeps the object in pool-friendly state.
                        if (lightChildTransform != null)
                            lightChildTransform.gameObject.SetActive(false);

                        if (ringRenderer != null)
                        {
                            ringRenderer.GetPropertyBlock(propBlock);
                            propBlock.SetColor("_EmissionColor", Color.black);
                            ringRenderer.SetPropertyBlock(propBlock);
                        }
                    }
                }
            }
        }

        public void ClearBoard()
        {
            HideTutorialArrow();

            // FIX-V3: Destroy cached materials before clearing to prevent GPU leak.
            // ClearBoard() is called every time BuildBoard() is called (level reload,
            // undo rebuild). Without Destroy(), each rebuild leaks the previous material set.
            foreach (var mat in _ringMaterialCache.Values)
            {
                if (mat != null) Destroy(mat);
            }
            _ringMaterialCache.Clear();

            foreach (var pole in _spawnedPoles)
                if (pole != null) RecyclePole(pole.gameObject);
            _spawnedPoles.Clear();
            foreach (var list in _spawnedRings)
                foreach (var ring in list)
                    if (ring != null) RecycleRing(ring);
            _spawnedRings.Clear();
        }

        public void ShowTutorialArrow(int poleId, string labelText)
        {
            if (poleId < 0 || poleId >= _spawnedPoles.Count)
            {
                HideTutorialArrow();
                return;
            }

            EnsureTutorialArrowCreated();

            var targetPole = _spawnedPoles[poleId];
            _tutorialArrowGo.transform.SetParent(targetPole.transform, false);
            // Position above the pole cap, offset along +Z so it sits in front of the pole
            // facing the camera (camera looks down -Z).
            float startY = F.PoleScale.y + 0.55f;
            float forwardOffset = 0.35f;
            _tutorialArrowGo.transform.localPosition = new Vector3(0f, startY, forwardOffset);
            _tutorialArrowGo.SetActive(true);

            if (_tutorialLabelText != null)
            {
                _tutorialLabelText.text = labelText;
            }
        }

        public void HideTutorialArrow()
        {
            if (_tutorialArrowGo != null)
            {
                DOTween.Kill(_tutorialArrowGo.transform);
                DOTween.Kill(_tutorialCanvasGo != null ? _tutorialCanvasGo.transform : null);
                _tutorialArrowGo.SetActive(false);
                _tutorialArrowGo.transform.SetParent(null);
            }
        }

        private void EnsureTutorialArrowCreated()
        {
            if (_tutorialArrowGo != null) return;

            _tutorialArrowGo = new GameObject("TutorialMarker");
            _tutorialArrowGo.transform.localScale = F.TutorialArrowScale;

            // Downward-pointing arrow (3D quad mesh, ▼ shape)
            var arrowGo = new GameObject("Arrow");
            arrowGo.transform.SetParent(_tutorialArrowGo.transform, false);
            arrowGo.transform.localPosition = Vector3.zero;

            var meshFilter = arrowGo.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateTutorialArrowMesh();

            var renderer = arrowGo.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GetTutorialArrowMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // World-space canvas label — always faces camera, readable on any device.
            _tutorialCanvasGo = new GameObject("LabelCanvas",
                typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            _tutorialCanvasGo.transform.SetParent(_tutorialArrowGo.transform, false);
            _tutorialCanvasGo.transform.localPosition = new Vector3(0f, 0.9f, 0f);

            var canvas = _tutorialCanvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            var canvasRt = _tutorialCanvasGo.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(2.2f, 0.7f);
            canvasRt.localScale = new Vector3(0.22f, 0.22f, 0.22f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(UnityEngine.UI.Text), typeof(UnityEngine.UI.Outline));
            labelGo.transform.SetParent(_tutorialCanvasGo.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            _tutorialLabelText = labelGo.GetComponent<UnityEngine.UI.Text>();
            _tutorialLabelText.alignment = TextAnchor.MiddleCenter;
            _tutorialLabelText.fontSize = 36;
            _tutorialLabelText.fontStyle = FontStyle.Bold;
            _tutorialLabelText.color = Color.white;
            _tutorialLabelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _tutorialLabelText.verticalOverflow = VerticalWrapMode.Overflow;
            _tutorialLabelText.font = GetBuiltinLabelFont();

            var labelOutline = labelGo.GetComponent<UnityEngine.UI.Outline>();
            labelOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            labelOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Subtle dark backing panel behind the label for readability on any background.
            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(UnityEngine.UI.Image));
            panelGo.transform.SetParent(_tutorialCanvasGo.transform, false);
            panelGo.transform.SetAsFirstSibling();
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = new Vector2(8f, 6f);
            panelRt.offsetMax = new Vector2(-8f, -6f);
            var panelImg = panelGo.GetComponent<UnityEngine.UI.Image>();
            panelImg.color = new Color(0f, 0f, 0f, 0.55f);
            panelImg.raycastTarget = false;

            // Pulse + bob — two local tweens, both auto-kill, no Sequence to leak.
            _tutorialArrowGo.transform.DOScale(F.TutorialArrowScale * 1.2f, F.TutorialArrowBobSpeed)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetAutoKill(true);

            float tutorialBaseY = F.PoleScale.y + 0.55f;
            _tutorialArrowGo.transform.DOLocalMoveY(tutorialBaseY + F.TutorialArrowBobHeight, F.TutorialArrowBobSpeed)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetAutoKill(true);
        }

        private Material _tutorialArrowMaterial;

        private Material GetTutorialArrowMaterial()
        {
            if (_tutorialArrowMaterial != null) return _tutorialArrowMaterial;
            _tutorialArrowMaterial = new Material(GetDefaultShader()) { name = "TutorialArrowMat" };
            _tutorialArrowMaterial.color = F.TutorialArrowColor;
            if (_tutorialArrowMaterial.HasProperty("_BaseColor"))
                _tutorialArrowMaterial.SetColor("_BaseColor", F.TutorialArrowColor);
            _tutorialArrowMaterial.SetFloat("_Metallic", 0f);
            _tutorialArrowMaterial.SetFloat("_Smoothness", 0.4f);
            _tutorialArrowMaterial.EnableKeyword("_EMISSION");
            _tutorialArrowMaterial.SetColor("_EmissionColor", F.TutorialArrowColor * 1.2f);
            return _tutorialArrowMaterial;
        }

        private Mesh CreateTutorialArrowMesh()
        {
            // A flat downward-pointing arrow (▼) in the XZ plane, slightly raised at center
            // so it reads as a proper 3D marker rather than a flat decal.
            Mesh mesh = new Mesh { name = "TutorialArrowMesh" };
            Vector3[] vertices = new Vector3[]
            {
                new Vector3( 0.00f, 0.10f,  0.00f), // 0 tip (slightly forward, raised)
                new Vector3(-0.55f, 0.10f, -0.30f), // 1 left wing
                new Vector3( 0.55f, 0.10f, -0.30f), // 2 right wing
                new Vector3(-0.30f, 0.10f, -0.80f), // 3 left tail
                new Vector3( 0.30f, 0.10f, -0.80f), // 4 right tail
            };
            int[] triangles = new int[]
            {
                0, 2, 1, // front face
                1, 2, 4,
                1, 4, 3,
                // back face (winding flipped for double-sided)
                0, 1, 2,
                1, 4, 2,
                1, 3, 4,
            };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            if (_tutorialArrowGo != null)
            {
                DOTween.Kill(_tutorialArrowGo.transform);
                Destroy(_tutorialArrowGo);
                _tutorialArrowGo = null;
            }
            _tutorialCanvasGo = null;
            _tutorialLabelText = null;
            if (_tutorialArrowMaterial != null)
            {
                Destroy(_tutorialArrowMaterial);
                _tutorialArrowMaterial = null;
            }
            if (_proceduralTorusMesh != null)
            {
                Destroy(_proceduralTorusMesh);
                _proceduralTorusMesh = null;
            }

            // FIX-V3: _ringMaterialCache previously called Clear() without destroying
            // the cached Material objects. Unity materials are unmanaged GPU resources —
            // Clear() drops the C# reference but does NOT release the GPU allocation,
            // causing a memory leak every time the board is rebuilt (ClearBoard calls
            // _ringMaterialCache.Clear()) and on scene unload.
            // Fix: Destroy every cached material before clearing the dictionary.
            foreach (var mat in _ringMaterialCache.Values)
            {
                if (mat != null) Destroy(mat);
            }
            _ringMaterialCache.Clear();

            // Also clean up the shared pole materials (static fields, destroyed here
            // because BoardView owns their lifecycle in the scene).
            if (_openPoleMaterial != null)   { Destroy(_openPoleMaterial);   _openPoleMaterial   = null; }
            if (_lockedPoleMaterial != null) { Destroy(_lockedPoleMaterial); _lockedPoleMaterial = null; }

            if (_bloomPulseController != null)
            {
                Destroy(_bloomPulseController.gameObject);
                _bloomPulseController = null;
            }
        }

        private static Shader GetDefaultShader()
        {
            if (_cachedShader != null) return _cachedShader;
            _cachedShader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard");
            return _cachedShader;
        }

        private static Font GetBuiltinLabelFont()
        {
            if (_cachedBuiltinFont != null) return _cachedBuiltinFont;
            _cachedBuiltinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                                 ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _cachedBuiltinFont;
        }

        private void EnsurePolePrefabCreated()
        {
            if (_polePrefab != null) return;
            
            _polePrefab = new GameObject("Pole_Template");
            
            GameObject body;
            if (F.PoleBodyMesh != null)
            {
                body = new GameObject("Body", typeof(MeshFilter), typeof(MeshRenderer));
                body.GetComponent<MeshFilter>().sharedMesh = F.PoleBodyMesh;
            }
            else
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                body.name = "Body";
                var bodyCol = body.GetComponent<Collider>();
                if (bodyCol != null) DestroyImmediate(bodyCol);
            }
            body.transform.SetParent(_polePrefab.transform, false);
            body.transform.localPosition = new Vector3(0f, 0f, 0f);
            body.transform.localScale = new Vector3(1f, 1f, 1f);
            
            GameObject cap;
            if (F.PoleCapMesh != null)
            {
                cap = new GameObject("Cap", typeof(MeshFilter), typeof(MeshRenderer));
                cap.GetComponent<MeshFilter>().sharedMesh = F.PoleCapMesh;
            }
            else
            {
                cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cap.name = "Cap";
                var capCol = cap.GetComponent<Collider>();
                if (capCol != null) DestroyImmediate(capCol);
            }
            cap.transform.SetParent(_polePrefab.transform, false);
            cap.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            
            float capYScale = F.PoleScale.x / F.PoleScale.y;
            cap.transform.localScale = new Vector3(1f, capYScale, 1f);

            var box = _polePrefab.AddComponent<BoxCollider>();
            box.size = new Vector3(F.PoleSpacing * F.PoleColliderWidthFraction, 3.0f, 2.0f);
            
            _polePrefab.SetActive(false);
            _polePrefab.transform.SetParent(transform, false);
        }

        private GameObject AcquirePole()
        {
            EnsurePolePrefabCreated();
            if (_objectPoolService != null)
                return _objectPoolService.Spawn(_polePrefab, Vector3.zero, Quaternion.identity);
            return Instantiate(_polePrefab, Vector3.zero, Quaternion.identity);
        }

        private void RecyclePole(GameObject pole)
        {
            if (_objectPoolService != null) _objectPoolService.Despawn(pole);
            else { pole.SetActive(false); pole.transform.SetParent(null); Destroy(pole); }
        }

        private GameObject AcquireRing()
        {
            if (_torusPrefab == null)
            {
                NexusLog.Error("BoardView", nameof(AcquireRing), "",
                    "_torusPrefab null — Torus prefab DI ile enjekte edilmemis. " +
                    "Ring acquire iptal.");
                return null;
            }

            GameObject ringObj = null;
            if (_objectPoolService != null)
            {
                ringObj = _objectPoolService.Spawn(_torusPrefab, Vector3.zero, Quaternion.identity);
            }
            else
            {
                ringObj = Instantiate(_torusPrefab);
            }
            
            if (ringObj == null)
            {
                ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                var fbCol = ringObj.GetComponent<Collider>();
                if (fbCol != null) Destroy(fbCol);
            }
            else
            {
                var col = ringObj.GetComponent<Collider>();
                if (col != null) Destroy(col);
                ringObj.SetActive(true);
                KillTweens(ringObj);
            }

            ringObj.name = "Ring_Torus";
            var meshFilter = ringObj.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = F.RingMesh != null ? F.RingMesh : GetProceduralTorusMesh();
            }
            return ringObj;
        }

        private void RecycleRing(GameObject ring)
        {
            KillTweens(ring);
            
            var lightChild = ring.transform.Find("SelectionGlowLight");
            if (lightChild != null) Destroy(lightChild.gameObject);
            
            for (int i = ring.transform.childCount - 1; i >= 0; i--)
            {
                var child = ring.transform.GetChild(i);
                if (child.name == "SpecialOverlay" || child.name == "RainbowCycle")
                    Destroy(child.gameObject);
            }
            if (_objectPoolService != null) _objectPoolService.Despawn(ring);
            else { ring.SetActive(false); ring.transform.SetParent(null); Destroy(ring); }
        }

        private static void KillTweens(GameObject obj) { if (obj != null) DOTween.Kill(obj.transform); }

        private static Material _openPoleMaterial;
        private static Material _lockedPoleMaterial;

        private Material GetPoleMaterial(bool locked)
        {
            var feel = F;
            if (feel == null)
            {
                throw new System.InvalidOperationException("[BoardView] GameFeelConfigSO is not injected!");
            }
            if (locked)
            {
                if (_lockedPoleMaterial == null)
                {
                    var darkColor = feel.PoleColorLocked;
                    _lockedPoleMaterial = new Material(GetDefaultShader()) { color = darkColor, name = "PoleMat_Locked" };
                    if (_lockedPoleMaterial.HasProperty("_BaseColor"))
                        _lockedPoleMaterial.SetColor("_BaseColor", darkColor);
                    _lockedPoleMaterial.SetFloat("_Metallic", feel.PoleMetallic);
                    _lockedPoleMaterial.SetFloat("_Smoothness", feel.PoleSmoothness);
                }
                return _lockedPoleMaterial;
            }
            if (_openPoleMaterial == null)
            {
                var openColor = feel.PoleColorOpen;
                _openPoleMaterial = new Material(GetDefaultShader()) { color = openColor, name = "PoleMat_Open" };
                if (_openPoleMaterial.HasProperty("_BaseColor"))
                    _openPoleMaterial.SetColor("_BaseColor", openColor);
                _openPoleMaterial.SetFloat("_Metallic", feel.PoleMetallic);
                _openPoleMaterial.SetFloat("_Smoothness", feel.PoleSmoothness);
            }
            return _openPoleMaterial;
        }

        private Material GetRingMaterial(RingColor color, RingType type)
        {
            var key = (color, type);
            if (_ringMaterialCache.TryGetValue(key, out var cached) && cached != null) return cached;
            var mat = new Material(GetDefaultShader());
            Color baseColor = GetRingColor(color);
            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Metallic", F.RingMetallic);
            mat.SetFloat("_Smoothness", F.RingSmoothness);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
            ApplySpecialRingMaterial(mat, baseColor, type);
            mat.name = "RingMat_" + color + "_" + type;
            _ringMaterialCache[key] = mat;
            return mat;
        }

        private void ApplySpecialRingMaterial(Material mat, Color baseColor, RingType type)
        {
            switch (type)
            {
                case RingType.Frozen:
                    mat.color = Color.Lerp(baseColor, Color.cyan, 0.5f);
                    mat.SetFloat("_Metallic", 0.1f); mat.SetFloat("_Smoothness", 0.9f);
                    mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.3f, 0.6f, 0.8f, 1f)); break;
                case RingType.Key: case RingType.Locked:
                    mat.color = new Color(1f, 0.84f, 0f);
                    mat.SetFloat("_Metallic", 0.8f); mat.SetFloat("_Smoothness", 0.6f);
                    mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.5f, 0.4f, 0f, 1f)); break;
                case RingType.Stone:
                    mat.color = new Color(0.4f, 0.38f, 0.35f);
                    mat.SetFloat("_Metallic", 0f); mat.SetFloat("_Smoothness", 0.1f); break;
                case RingType.Glass:
                    mat.color = new Color(1f, 1f, 1f, 0.25f);
                    mat.SetFloat("_Metallic", 0f); mat.SetFloat("_Smoothness", 0.95f); SetFadeMode(mat); break;
                case RingType.Rainbow:
                    mat.SetFloat("_Metallic", 0.5f); mat.SetFloat("_Smoothness", 0.8f);
                    mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", Color.Lerp(baseColor, Color.white, 0.3f)); break;
                case RingType.Ghost:
                    mat.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.15f);
                    mat.SetFloat("_Metallic", 0.3f); mat.SetFloat("_Smoothness", 0.3f); SetFadeMode(mat); break;
                case RingType.Bomb:
                    mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.8f, 0.2f, 0f, 1f)); break;
                case RingType.Chain:
                    mat.SetFloat("_Metallic", 0.7f); mat.SetFloat("_Smoothness", 0.3f); break;
                case RingType.Magnet:
                    mat.SetFloat("_Metallic", 0.9f); mat.SetFloat("_Smoothness", 0.5f);
                    mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.5f, 0f, 0.5f, 1f)); break;
                case RingType.Paint:
                    mat.SetFloat("_Smoothness", 0.9f); break;
                case RingType.Mystery:
                    mat.color = new Color(0.3f, 0.3f, 0.3f);
                    mat.SetFloat("_Metallic", 0.4f); mat.SetFloat("_Smoothness", 0.6f);
                    mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", new Color(0.2f, 0.2f, 0.2f, 1f)); break;
            }
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", mat.color);
        }

        // FIX-V4: The old implementation used Standard Shader blend-mode properties
        // (_Mode, _SrcBlend, _DstBlend, _ALPHABLEND_ON) which are URP/HDRP-incompatible.
        // In URP the equivalent is the _Surface property (0=Opaque, 1=Transparent) and
        // the _Blend property (0=Alpha, 1=Premultiply, 2=Additive, 3=Multiply).
        // The _ALPHABLEND_ON keyword does not exist in URP shaders; use _SURFACE_TYPE_TRANSPARENT.
        // We still set the legacy Standard properties as a fallback for any non-URP material,
        // but add the URP-correct equivalents so Glass/Ghost rings are actually transparent
        // in the shipping URP configuration.
        private static void SetFadeMode(Material mat)
        {
            // ── URP path ──────────────────────────────────────────────────────────
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);            // 1 = Transparent
                mat.SetFloat("_Blend", 0f);              // 0 = Alpha blend
                mat.SetInt("_ZWrite", 0);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                return;
            }

            // ── Standard Shader fallback (non-URP projects / editor preview) ──────
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
        }

        private void TriggerMoveEffects(Vector3 position, RingColor color)
        {
            if (_audioService != null)
                _audioService.PlaySfx(ProceduralAudio.GetOrCreateMoveClip(), 1.0f, 0.92f, 1.08f);
            if (_vfxRegistry != null && _objectPoolService != null)
            {
                var prefab = _vfxRegistry.GetRingPopPrefab();
                if (prefab != null)
                {
                    var popInstance = _objectPoolService.Spawn(prefab, position, Quaternion.identity);
                    popInstance?.GetComponent<RingPopVfx>()?.Initialize(GetRingColor(color));
                }
            }
        }

        // FIX-V1: TextMesh (legacy) has two known mobile issues:
        //   1. Emoji characters render as blank boxes on many Android / iOS device fonts.
        //   2. Each TextMesh instance is its own draw call — 12 poles × 4 rings = up to
        //      48 extra draw calls, blowing the GDD §75 budget of <80 draw calls.
        // Fix: replace emoji with ASCII symbols that render reliably on the built-in font.
        // Production path: swap TextMesh for SpriteRenderer + sprite atlas icons.
        private void AddSpecialOverlay(GameObject ringObj, RingData ringData)
        {
            if (ringData.Type == RingType.Standard) return;

            // ASCII fallback — renders on every device font (no emoji dependency).
            var (text, color) = ringData.Type switch
            {
                RingType.Mystery => ("?",  Color.yellow),
                RingType.Frozen  => ("*",  Color.cyan),
                RingType.Locked  => ("L",  new Color(1f, 0.84f, 0f)),
                RingType.Key     => ("K",  new Color(1f, 0.84f, 0f)),
                RingType.Stone   => ("S",  Color.gray),
                RingType.Glass   => ("G",  new Color(1f, 1f, 1f, 0.5f)),
                RingType.Bomb    => (ringData.AdditionalData.ToString(), Color.red),
                RingType.Chain   => ("C",  Color.white),
                RingType.Magnet  => ("M",  Color.magenta),
                RingType.Paint   => ("P",  Color.green),
                RingType.Ghost   => ("~",  new Color(1f, 1f, 1f, 0.3f)),
                _ => ("", Color.white)
            };

            if (ringData.Type == RingType.Rainbow)
            {
                var cycle = new GameObject("RainbowCycle");
                cycle.transform.SetParent(ringObj.transform, false);
                cycle.AddComponent<RainbowCycle>().Initialize(F);
            }

            if (string.IsNullOrEmpty(text)) return;

            var overlayGo = new GameObject("SpecialOverlay", typeof(TextMesh));
            overlayGo.transform.SetParent(ringObj.transform, false);
            overlayGo.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            overlayGo.transform.localScale = new Vector3(0.06f, 0.06f, 0.06f);
            overlayGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var textMesh = overlayGo.GetComponent<TextMesh>();
            textMesh.text = text;
            textMesh.fontSize = 72;
            textMesh.color = color;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontStyle = FontStyle.Bold;

            var font = GetBuiltinLabelFont();
            textMesh.font = font;

            // OPEN-4: Draw call reduction — all TextMesh overlays share the same
            // font material instance so Unity can batch them into a single draw call.
            // Each TextMesh.font has a .material property; assigning sharedMaterial
            // prevents per-instance material copies (which Unity creates by default
            // when you access .material, not .sharedMaterial).
            // Production upgrade path: replace TextMesh with a sprite quad that reads
            // from a RingIconConfig ScriptableObject (sprite atlas per ring type).
            // That will reduce this to 1 draw call regardless of ring count via
            // SpriteAtlas batching.
            var mr = overlayGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                // Share the font's material across all overlays so they batch.
                if (font != null && font.material != null)
                    mr.sharedMaterial = font.material;
            }
        }



        private bool TryGetMainCamera(out Camera cam)
        {
            cam = _mainCamera;
            return cam != null;
        }

        public void ShakeCamera(float intensity, float duration)
        {
            if (_settingsModel != null && _settingsModel.ReduceMotion.Value) return;
            if (TryGetMainCamera(out var cam))
                cam.transform.DOShakePosition(duration, intensity, 10, 90f, false, true);
        }

        public void FitCameraToBoard(int poleCount)
        {
            if (!TryGetMainCamera(out var cam)) return;
            cam.orthographic = true;
            var t = cam.transform;
            t.position = F.CameraPosition;
            t.rotation = Quaternion.Euler(F.CameraRotation);
            
            float spacing = F.PoleSpacing;
            float boardWidth = (poleCount - 1) * spacing;
            float desiredHalfWidth = (boardWidth * 0.5f) + 1.5f;
            float desiredOrthoSize = desiredHalfWidth / cam.aspect;
            cam.orthographicSize = Mathf.Max(desiredOrthoSize, 5.5f);
        }

        private void EnsureFloorPlaneCreated()
        {
            if (_floorPlane != null) return;

            // DI ile enjekte edilmiş FloorPlane referansı kullanılır.
            // GameObject.Find runtime'da yasaktır — sahne referansı üzerinden çözülür.
            if (transform.parent != null)
            {
                var existing = transform.parent.Find("ShadowFloorPlane");
                if (existing != null)
                {
                    _floorPlane = existing.gameObject;
                    return;
                }
            }

            if (F.FloorMesh != null)
            {
                _floorPlane = new GameObject("ShadowFloorPlane", typeof(MeshFilter), typeof(MeshRenderer));
                _floorPlane.GetComponent<MeshFilter>().sharedMesh = F.FloorMesh;
            }
            else
            {
                _floorPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                _floorPlane.name = "ShadowFloorPlane";
                var col = _floorPlane.GetComponent<Collider>();
                if (col != null) Destroy(col);
            }

            _floorPlane.transform.position = new Vector3(0f, F.FloorYPosition, 0f);
            _floorPlane.transform.localScale = F.FloorScale;
            
            var renderer = _floorPlane.GetComponent<MeshRenderer>();
            renderer.receiveShadows = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            
            var mat = new Material(GetDefaultShader());
            Color floorColor = F.FloorColor;
            mat.color = floorColor;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", floorColor);
            mat.SetFloat("_Metallic", F.FloorMetallic);
            mat.SetFloat("_Smoothness", F.FloorSmoothness);
            renderer.sharedMaterial = mat;
        }

        private Mesh GetProceduralTorusMesh()
        {
            if (_proceduralTorusMesh == null)
            {
                _proceduralTorusMesh = CreateProceduralTorusMesh(F.TorusMajorRadius, F.TorusMinorRadius, F.TorusRadialSegments, F.TorusTubularSegments);
            }
            return _proceduralTorusMesh;
        }

        private Mesh CreateProceduralTorusMesh(float majorRadius, float minorRadius, int radialSegments, int tubularSegments)
        {
            Mesh mesh = new Mesh();
            mesh.name = "ProceduralTorus";

            int numVertices = (radialSegments + 1) * (tubularSegments + 1);
            Vector3[] vertices = new Vector3[numVertices];
            Vector3[] normals = new Vector3[numVertices];
            Vector2[] uv = new Vector2[numVertices];
            int[] triangles = new int[radialSegments * tubularSegments * 6];

            int vIdx = 0;
            for (int radial = 0; radial <= radialSegments; radial++)
            {
                float u = (float)radial / radialSegments * Mathf.PI * 2f;
                float cosU = Mathf.Cos(u);
                float sinU = Mathf.Sin(u);

                for (int tubular = 0; tubular <= tubularSegments; tubular++)
                {
                    float v = (float)tubular / tubularSegments * Mathf.PI * 2f;
                    float cosV = Mathf.Cos(v);
                    float sinV = Mathf.Sin(v);

                    float x = (majorRadius + minorRadius * cosV) * cosU;
                    float y = minorRadius * sinV;
                    float z = (majorRadius + minorRadius * cosV) * sinU;
                    vertices[vIdx] = new Vector3(x, y, z);

                    normals[vIdx] = new Vector3(cosV * cosU, sinV, cosV * sinU).normalized;
                    uv[vIdx] = new Vector2((float)radial / radialSegments, (float)tubular / tubularSegments);

                    vIdx++;
                }
            }

            int tIdx = 0;
            for (int radial = 0; radial < radialSegments; radial++)
            {
                for (int tubular = 0; tubular < tubularSegments; tubular++)
                {
                    int current = radial * (tubularSegments + 1) + tubular;
                    int next = (radial + 1) * (tubularSegments + 1) + tubular;

                    triangles[tIdx++] = current;
                    triangles[tIdx++] = next + 1;
                    triangles[tIdx++] = next;

                    triangles[tIdx++] = current;
                    triangles[tIdx++] = current + 1;
                    triangles[tIdx++] = next + 1;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            return mesh;
        }
    }

    // ARCH-1: RainbowCycle extracted to Assets/Scripts/Gameplay/Views/RainbowCycle.cs
    // for independent compilation and reduced BoardView line count.
}

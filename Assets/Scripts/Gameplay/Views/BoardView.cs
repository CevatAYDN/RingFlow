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
        [Inject] private GameplayModel _model;
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
            var visualBoard = GameplayHelpers.FindRootGameObject("RingFlow_VisualBoard");
            if (visualBoard != null) Destroy(visualBoard);

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
            bool reduceMotion = _settingsModel != null && _settingsModel.ReduceMotion.Value;
            bool slowMode = _settingsModel != null && _settingsModel.SlowMode.Value;
            float speed = slowMode ? F.SlowModeMultiplier : 1f;
            float duration = F.MoveDuration * speed;

            _animatingTargetPoleId = toPoleId;

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
            }

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
            else { _animatingTargetPoleId = -1; }
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
                ApplySelection();
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

        public void CelebratePoleComplete(int poleId)
        {
            var pv = GetPoleView(poleId);
            if (pv == null) return;

            // Determine ring count on this pole for intensity scaling
            int ringCount = 0;
            if (_model != null && poleId >= 0 && poleId < _model.Poles.Count)
                ringCount = _model.Poles[poleId].Rings.Count;

            // --- 3-Tier Feedback System ---
            int completedCount = _model?.CompletedPoles.Count ?? 0;
            bool isFinalPole = _model != null && completedCount >= _model.Poles.Count - 1;
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
            var ringsList = new List<Transform>();
            foreach (Transform child in pv.transform)
            {
                if (child.name.StartsWith("Ring_"))
                    ringsList.Add(child);
            }
            ringsList.Sort((a, b) => a.localPosition.y.CompareTo(b.localPosition.y));

            for (int i = 0; i < ringsList.Count; i++)
            {
                var ringTrans = ringsList[i];
                float originalY = ringTrans.localPosition.y;
                float originalScaleY = ringTrans.localScale.y;

                ringTrans.DOLocalMoveY(originalY + (isFinalPole ? 0.5f : 0.35f), 0.15f)
                         .SetEase(Ease.OutQuad)
                         .SetDelay(i * 0.04f)
                         .OnComplete(() =>
                         {
                             ringTrans.DOLocalMoveY(originalY, 0.20f)
                                      .SetEase(Ease.InQuad)
                                      .OnComplete(() =>
                                      {
                                          ringTrans.DOScaleY(originalScaleY * 0.7f, 0.08f)
                                                   .SetEase(Ease.OutQuad)
                                                   .OnComplete(() =>
                                                   {
                                                       ringTrans.DOScaleY(originalScaleY, 0.12f)
                                                                .SetEase(Ease.OutBack);
                                                   });
                                      });
                         });
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

                if (i == _animatingTargetPoleId) continue;

                if (i < _spawnedRings.Count && _spawnedRings[i].Count > 0)
                {
                    var topRing = _spawnedRings[i][^1];
                    if (topRing == null) continue;

                    int ringIndex = _spawnedRings[i].Count - 1;
                    float targetY = F.RingBaseYOffset + (ringIndex * F.RingStackSpacing) + (isSelected ? F.RingSelectionLift : 0f);

                    DOTween.Kill(topRing.transform);
                    if (reduceMotion)
                    {
                        topRing.transform.localPosition = new Vector3(0f, targetY, 0f);
                    }
                    else
                    {
                        topRing.transform.DOLocalMoveY(targetY, duration).SetEase(Ease.OutQuad)
                            .SetAutoKill(true)
                            .OnComplete(() =>
                            {
                                if (isSelected)
                                {
                                    topRing.transform.DOLocalMoveY(targetY + F.TutorialArrowBobHeight * 0.4f, 0.6f)
                                        .SetEase(Ease.InOutSine)
                                        .SetLoops(-1, LoopType.Yoyo)
                                        .SetAutoKill(true);
                                }
                            });
                    }

                    // --- Warm Selection Glow ---
                    var lightChildTransform = topRing.transform.Find("SelectionGlowLight");
                    var ringRenderer = topRing.GetComponentInChildren<Renderer>();
                    var propBlock = GetSelectionPropBlock();
                    propBlock.Clear();

                    if (isSelected)
                    {
                        if (lightChildTransform == null)
                        {
                            var lightGo = new GameObject("SelectionGlowLight");
                            lightGo.transform.SetParent(topRing.transform, false);
                            lightGo.transform.localPosition = Vector3.zero;

                            var light = lightGo.AddComponent<Light>();
                            light.type = LightType.Point;
                            light.color = F.SelectionGlowColor;
                            light.range = F.SelectionGlowRange;
                            light.intensity = F.SelectionGlowIntensity;
                            light.shadows = LightShadows.None;
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
                        if (lightChildTransform != null) 
                        {
                            Destroy(lightChildTransform.gameObject);
                        }
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
            _ringMaterialCache.Clear();
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

        private static void SetFadeMode(Material mat)
        {
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

        private void AddSpecialOverlay(GameObject ringObj, RingData ringData)
        {
            if (ringData.Type == RingType.Standard) return;
            var (text, color) = ringData.Type switch
            {
                RingType.Mystery => ("?", Color.yellow),
                RingType.Frozen  => ("❄", Color.cyan),
                RingType.Locked  => ("🔑", new Color(1f, 0.84f, 0f)),
                RingType.Key     => ("🔑", new Color(1f, 0.84f, 0f)),
                RingType.Stone   => ("🧱", Color.gray),
                RingType.Glass   => ("💎", new Color(1f, 1f, 1f, 0.5f)),
                RingType.Bomb    => (ringData.AdditionalData.ToString(), Color.red),
                RingType.Chain   => ("⛓", Color.white),
                RingType.Magnet  => ("🧲", Color.magenta),
                RingType.Paint   => ("🎨", Color.green),
                RingType.Ghost   => ("👻", new Color(1f, 1f, 1f, 0.3f)),
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
            overlayGo.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            overlayGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var textMesh = overlayGo.GetComponent<TextMesh>();
            textMesh.text = text;
            textMesh.fontSize = 64;
            textMesh.color = color;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontStyle = FontStyle.Bold;
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

    public class RainbowCycle : MonoBehaviour
    {
        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private GameFeelConfigSO _feel;

        public void Initialize(GameFeelConfigSO feel)
        {
            _feel = feel;
        }
 
        private void Start()
        {
            _renderer = GetComponentInParent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }
 
        private void Update()
        {
            if (_renderer == null || _feel == null) return;
            float hue = (Time.time * _feel.RainbowHueSpeed) % 1f;
            Color color = Color.HSVToRGB(hue, _feel.RainbowSaturation, _feel.RainbowValue);
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_Color", color);
            _propBlock.SetColor("_BaseColor", color);
            _propBlock.SetColor("_EmissionColor", color * 0.3f);
            _renderer.SetPropertyBlock(_propBlock);
        }
    }
}

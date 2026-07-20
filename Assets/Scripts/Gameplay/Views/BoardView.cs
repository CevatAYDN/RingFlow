using System.Collections.Generic;
using DG.Tweening;
using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay.Services;
using RingFlow.Gameplay.Views;
using TMPro;
using UnityEngine;

namespace RingFlow.Gameplay
{
    [Mediator(typeof(BoardMediator))]
    public class BoardView : View
    {
        [UnityEngine.Scripting.Preserve]
        [SerializeField] private GameObject _torusPrefab;

        public void SetTorusPrefab(GameObject prefab) { _torusPrefab = prefab; }

        private GameObject _polePrefab;

        [Inject] private IObjectPoolService _objectPoolService;
        [Inject] private VfxPrefabRegistry _vfxRegistry;
        [Inject] private IAudioService _audioService;
        [Inject] private IProceduralAudioService _proceduralAudio;
        [Inject] private IHapticService _hapticService;
        [Inject] private SettingsModel _settingsModel;
        [Inject] private GameFeelConfigSO _feelConfig;
        [Inject] private RingMaterialManager _ringMaterialManager;
        [Inject] private SpecialOverlayRenderer _overlayRenderer;

        [Inject] private Camera _mainCamera;
        private GameFeelConfigSO F => _feelConfig;

        // Handler classes (lazy-initialized by InitializeHandlers)
        private RingAnimationHandler _animationHandler;
        private BoardSelectionHandler _selectionHandler;
        private int _lastSelectedPoleId = -1;

        // ── Delegates for handler access to internal state ──
        private GameObject GetSpawnedRing(int poleId)
        {
            if (poleId < 0 || poleId >= _spawnedRings.Count || _spawnedRings[poleId].Count == 0) return null;
            return _spawnedRings[poleId][^1];
        }

        private GameObject GetSpawnedRingAt(int poleId, int ringIndex)
        {
            if (poleId < 0 || poleId >= _spawnedRings.Count) return null;
            var list = _spawnedRings[poleId];
            if (ringIndex < 0 || ringIndex >= list.Count) return null;
            return list[ringIndex];
        }

        private int GetRingCountOnPole(int poleId)
        {
            if (poleId < 0 || poleId >= _spawnedRings.Count) return 0;
            return _spawnedRings[poleId].Count;
        }

        private GameObject GetPoleObject(int poleId)
        {
            if (poleId < 0 || poleId >= _spawnedPoles.Count || _spawnedPoles[poleId] == null) return null;
            return _spawnedPoles[poleId].gameObject;
        }

        private void InitializeHandlersIfNeeded()
        {
            if (_animationHandler != null) return;
            _animationHandler = new RingAnimationHandler(
                this, F, _settingsModel, _hapticService, _audioService,
                _proceduralAudio, _objectPoolService, _vfxRegistry, _ringMaterialManager);
            _selectionHandler = new BoardSelectionHandler(F, _settingsModel);
            _selectionHandler.GetAnimatingTargetPoleId = () => _animationHandler.AnimatingTargetPoleId;
        }

        private readonly List<PoleView> _spawnedPoles = new();
        private readonly List<List<GameObject>> _spawnedRings = new();
        private bool _ringPrewarmed;
        private Mesh _proceduralTorusMesh;
        private GameObject _floorPlane;
        private GameObject _tutorialArrowGo;
        private GameObject _tutorialCanvasGo;
        private TextMeshProUGUI _tutorialLabelText;

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
            int animPoleId = _animationHandler != null ? _animationHandler.AnimatingTargetPoleId : -1;
            NexusLog.Info("BoardView", nameof(BuildBoard), poles.Count.ToString(),
                $"Rebuilding board: {poles.Count} poles, {totalRings} total rings, animatingPoleId={animPoleId}.");
#endif
            // Incremental sync: return previously spawned poles/rings to their pool
            // before re-spawning from the current pole data. Preserves pool integrity
            // and avoids destroying the visual root hierarchy (which would orphan the
            // pool and break Undo/Move visual restore).
            ClearBoard();

            EnsureRingPoolPrewarmed();
            EnsureFloorPlaneCreated();
            InitializeHandlersIfNeeded();
            int backRowCount = Mathf.CeilToInt(poles.Count / 2.0f);
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
                poleObj.transform.localPosition = GetPolePosition(p, poles.Count, F);
                poleObj.transform.localRotation = Quaternion.identity;

                // Faux-3D perspective scale: make back row 85% scale!
                float scaleFactor = (poles.Count > 5 && p < backRowCount) ? 0.85f : 1.0f;
                var poleScale = F.PoleScale * scaleFactor;
                int poleCap = poleData.RingCapacity;
                if (poleCap <= 0)
                    throw new System.InvalidOperationException(
                        $"[BoardView] Pole {p} has RingCapacity={poleCap}. Data must have a positive RingCapacity value. " +
                        "Check LevelData or pole configuration.");
                
                float worldBaseFromBottom = F != null ? F.WorldBaseFromBottom : 0.22f;
                float worldSpacing = F.RingStackSpacing * F.PoleScale.y;
                float totalHeight = worldBaseFromBottom + (poleCap * worldSpacing) + 0.15f;
                poleScale.y = (totalHeight * scaleFactor) / 2.0f;
                poleObj.transform.localScale = poleScale;

                var box = poleObj.GetComponent<BoxCollider>();
                if (box != null)
                {
                    float targetWorldWidth = F.PoleSpacing * F.PoleColliderWidthFraction * scaleFactor;
                    float targetWorldHeight = (totalHeight + 1.5f) * scaleFactor;
                    float targetWorldDepth = (poles.Count <= 5 ? 4.0f : 2.0f) * scaleFactor;
                    box.size = new Vector3(targetWorldWidth / poleScale.x, targetWorldHeight / poleScale.y, targetWorldDepth / poleScale.z);
                    float centerWorldY = (targetWorldHeight / 2.0f) - (0.5f * scaleFactor);
                    box.center = new Vector3(0f, centerWorldY / poleScale.y, 0f);
                }


                var renderers = poleObj.GetComponentsInChildren<Renderer>(true);
                var poleMat = _ringMaterialManager.GetPoleMaterial(poleData.IsLocked);
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

                    float targetWorldY = (0.22f + (r * (F.RingStackSpacing * F.PoleScale.y))) * scaleFactor;
                    if (p == _selectionHandler.LastSelectedPoleId && r == neededRingCount - 1)
                    {
                        bool reduceMotion = _settingsModel != null && _settingsModel.ReduceMotion.Value;
                        if (!reduceMotion) targetWorldY += F.RingSelectionLift * scaleFactor;
                    }

                    ringObj.transform.localPosition = new Vector3(0f, targetWorldY / poleObj.transform.localScale.y, 0f);
                    ringObj.transform.localRotation = Quaternion.identity;

                    float localX = (F.RingTargetWidth * scaleFactor) / poleObj.transform.localScale.x;
                    float localY = ((F.RingTargetHeight / F.RingMeshHeight) * scaleFactor) / poleObj.transform.localScale.y;
                    float localZ = (F.RingTargetWidth * scaleFactor) / poleObj.transform.localScale.z;
                    ringObj.transform.localScale = new Vector3(localX, localY, localZ);

                    var ringRenderer = ringObj.GetComponentInChildren<Renderer>();
                    if (ringRenderer != null)
                        ringRenderer.sharedMaterial = _ringMaterialManager.GetRingMaterial(ringData.Color, ringData.Type);

                    _overlayRenderer.AddSpecialOverlay(ringObj, ringData);
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
            InitializeHandlersIfNeeded();
            _animationHandler.AnimateRingMove(
                fromPoleId, toPoleId, poles,
                GetSpawnedRing,
                GetPoleObject,
                GetRingCountOnPole,
                () => ApplySelection());
        }

        public void AnimateRingUndo(int fromPoleId, int toPoleId, List<PoleState> poles)
        {
            InitializeHandlersIfNeeded();
            _animationHandler.AnimateRingUndo(
                fromPoleId, toPoleId, poles,
                GetSpawnedRing,
                GetPoleObject,
                GetRingCountOnPole,
                () => ApplySelection());
        }

        public PoleView GetPoleView(int poleId)
        {
            if (poleId < 0 || poleId >= _spawnedPoles.Count) return null;
            return _spawnedPoles[poleId];
        }

        public void SetSelectedPole(int poleId)
        {
            InitializeHandlersIfNeeded();
            bool changed = _selectionHandler.SetSelectedPole(poleId,
                id => _hapticService?.Vibrate(HapticType.Selection));
            _lastSelectedPoleId = _selectionHandler.LastSelectedPoleId;

            if (changed)
            {
                // FIX-U4: Do not call ApplySelection while a ring is mid-animation.
                if (_animationHandler.AnimatingTargetPoleId < 0)
                {
                    ApplySelection();
                }
            }
        }

        public void FlashPoleError(int poleId)
        {
            InitializeHandlersIfNeeded();
            var poleObj = GetPoleObject(poleId);
            if (poleObj == null) return;
            _animationHandler.FlashPoleError(poleObj);
        }

        public void CelebratePoleComplete(int poleId, int ringCount, int completedCount, bool isFinalPole)
        {
            InitializeHandlersIfNeeded();
            _animationHandler.CelebratePoleComplete(
                poleId, ringCount, completedCount, isFinalPole,
                GetPoleObject,
                GetSpawnedRingAt,
                () => _spawnedPoles.Count);

            // Bloom pulse still handled in BoardView
            var bloom = GetOrFindBloomPulseController();
            if (bloom != null)
            {
                float bloomMultiplier = isFinalPole ? F.FinalBloomIntensityMultiplier : F.BloomIntensityMultiplier;
                float bloomDuration = isFinalPole ? F.FinalBloomPulseDuration : F.BloomPulseDuration;
                bloom.Pulse(bloomMultiplier, bloomDuration, isFinalPole);
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

        private void ApplySelection()
        {
            InitializeHandlersIfNeeded();
            _selectionHandler.ApplySelection(
                _spawnedPoles.Count,
                GetPoleObject,
                GetRingCountOnPole,
                GetSpawnedRingAt,
                GetPoleView);
        }

        public void ClearBoard()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("BoardView", nameof(ClearBoard), "",
                $"Clearing board. poles={_spawnedPoles.Count}, ringLists={_spawnedRings.Count}.");
#endif
            HideTutorialArrow();

            _ringMaterialManager?.ClearCache();

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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Warn("BoardView", nameof(ShowTutorialArrow), poleId.ToString(),
                    $"poleId {poleId} out of range (spawned={_spawnedPoles.Count}) — hiding tutorial arrow.");
#endif
                HideTutorialArrow();
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("BoardView", nameof(ShowTutorialArrow), poleId.ToString(),
                $"Showing tutorial arrow on pole {poleId} with label='{labelText}'.");
#endif

            EnsureTutorialArrowCreated();

            var targetPole = _spawnedPoles[poleId];
            _tutorialArrowGo.transform.SetParent(targetPole.transform, false);
            // Position above the pole cap, offset along +Z so it sits in front of the pole
            // facing the camera (camera looks down -Z).
            float startY = F.PoleScale.y + 0.55f;
            float forwardOffset = F.TutorialForwardOffset;
            _tutorialArrowGo.transform.localPosition = new Vector3(0f, startY, forwardOffset);
            _tutorialArrowGo.SetActive(true);

            if (_tutorialLabelText != null)
            {
                _tutorialLabelText.text = labelText;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            else
            {
                NexusLog.Warn("BoardView", nameof(ShowTutorialArrow), poleId.ToString(),
                    "_tutorialLabelText is null — label will not display. EnsureTutorialArrowCreated may have failed.");
            }
#endif
        }

        public void HideTutorialArrow()
        {
            if (_tutorialArrowGo != null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (_tutorialArrowGo.activeSelf)
                    NexusLog.Info("BoardView", nameof(HideTutorialArrow), "", "Hiding tutorial arrow.");
#endif
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
            _tutorialCanvasGo.transform.localPosition = new Vector3(0f, F.TutorialLabelYOffset, 0f);

            var canvas = _tutorialCanvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            var canvasRt = _tutorialCanvasGo.GetComponent<RectTransform>();
            canvasRt.sizeDelta = F.TutorialLabelCanvasSize;
            canvasRt.localScale = F.TutorialLabelCanvasScale;

            var labelGo = new GameObject("Label", typeof(RectTransform),
                typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(_tutorialCanvasGo.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            _tutorialLabelText = labelGo.GetComponent<TextMeshProUGUI>();
            _tutorialLabelText.alignment = TextAlignmentOptions.Center;
            _tutorialLabelText.fontSize = F.TutorialLabelFontSize;
            _tutorialLabelText.fontStyle = FontStyles.Bold;
            _tutorialLabelText.color = Color.white;
            _tutorialLabelText.overflowMode = TextOverflowModes.Overflow;
            _tutorialLabelText.outlineWidth = 0.2f;
            _tutorialLabelText.outlineColor = F.TutorialLabelOutlineColor;

            // Subtle dark backing panel behind the label for readability on any background.
            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(UnityEngine.UI.Image));
            panelGo.transform.SetParent(_tutorialCanvasGo.transform, false);
            panelGo.transform.SetAsFirstSibling();
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = F.TutorialPanelPaddingMin;
            panelRt.offsetMax = F.TutorialPanelPaddingMax;
            var panelImg = panelGo.GetComponent<UnityEngine.UI.Image>();
            panelImg.color = F.TutorialPanelColor;
            panelImg.raycastTarget = false;

            // Pulse + bob — two local tweens, both auto-kill, no Sequence to leak.
            _tutorialArrowGo.transform.DOScale(F.TutorialArrowScale * 1.2f, F.TutorialArrowBobSpeed)
                .SetEase(DG.Tweening.Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetAutoKill(true);

            float tutorialBaseY = F.PoleScale.y + 0.55f;
            _tutorialArrowGo.transform.DOLocalMoveY(tutorialBaseY + F.TutorialArrowBobHeight, F.TutorialArrowBobSpeed)
                .SetEase(DG.Tweening.Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetAutoKill(true);
        }

        private Material _tutorialArrowMaterial;

        private Material GetTutorialArrowMaterial()
        {
            if (_tutorialArrowMaterial != null) return _tutorialArrowMaterial;
            _tutorialArrowMaterial = new Material(_ringMaterialManager.GetDefaultShader()) { name = "TutorialArrowMat" };
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

            _ringMaterialManager?.ClearCache();

            if (_bloomPulseController != null)
            {
                Destroy(_bloomPulseController.gameObject);
                _bloomPulseController = null;
            }
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
            body.transform.localPosition = new Vector3(0f, 1.0f, 0f);
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
            cap.transform.localPosition = new Vector3(0f, 2.0f, 0f);
            
            float capYScale = F.PoleScale.x / F.PoleScale.y;
            cap.transform.localScale = new Vector3(1f, capYScale, 1f);

            var box = _polePrefab.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 1.0f, 0f);
            box.size = new Vector3(F.PoleSpacing * F.PoleColliderWidthFraction, 2.2f, 2.0f);
            
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
                if (fbCol != null)
                {
                    if (Application.isPlaying) Destroy(fbCol);
                    else DestroyImmediate(fbCol);
                }
            }
            else
            {
                var col = ringObj.GetComponent<Collider>();
                if (col != null)
                {
                    if (Application.isPlaying) Destroy(col);
                    else DestroyImmediate(col);
                }
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



        /// <summary>
        /// Chain-link VFX/SFX: metallic clink sound + ring-shaped burst VFX at both 
        /// the source partner pole and target pole. Called by BoardMediator on ChainLinkSignal.
        /// </summary>
        public void PlayChainLinkVfx(int fromPoleId, int toPoleId, Color color, List<PoleState> poles)
        {
            InitializeHandlersIfNeeded();
            Vector3 fromPos = GetPoleWorldPosition(fromPoleId);
            Vector3 toPos = GetPoleWorldPosition(toPoleId);
            _animationHandler.PlayChainLinkVfx(fromPos, toPos, color);
        }

        /// <summary>
        /// Magnet pull VFX/SFX: magnetic hum sound + burst VFX at the target pole.
        /// Called by BoardMediator on MagnetPullSignal.
        /// </summary>
        public void PlayMagnetPullVfx(int targetPoleId, int pulledCount, Color color, List<PoleState> poles)
        {
            InitializeHandlersIfNeeded();
            Vector3 targetPos = GetPoleWorldPosition(targetPoleId);
            _animationHandler.PlayMagnetPullVfx(targetPos, pulledCount, color);
        }

        /// <summary>
        /// Paint splash VFX/SFX: wet splat sound + paint-colored burst at the pole.
        /// Called by BoardMediator on PaintRingSignal.
        /// </summary>
        public void PlayPaintSplashVfx(Vector3 position, Color color)
        {
            InitializeHandlersIfNeeded();
            _animationHandler.PlayPaintSplashVfx(position, color);
        }

        /// <summary>
        /// Ice break VFX/SFX: cracking sound + cyan ice-shard burst at the pole.
        /// Called by BoardMediator on BreakIceSignal.
        /// </summary>
        public void PlayIceBreakVfx(int poleId, Color color)
        {
            InitializeHandlersIfNeeded();
            Vector3 pos = GetPoleWorldPosition(poleId);
            _animationHandler.PlayIceBreakVfx(pos, color);
        }

        /// <summary>
        /// Stone impact VFX/SFX: heavy thud sound + gray impact burst at the pole.
        /// Called by BoardMediator on StoneImpactSignal.
        /// </summary>
        public void PlayStoneThudVfx(int poleId, Color color)
        {
            InitializeHandlersIfNeeded();
            Vector3 pos = GetPoleWorldPosition(poleId);
            _animationHandler.PlayStoneThudVfx(pos, color);
        }

        /// <summary>
        /// Helper: gets the approximate world-space position of a pole's top area.
        /// </summary>
        private Vector3 GetPoleWorldPosition(int poleId)
        {
            var pv = GetPoleView(poleId);
            if (pv != null)
                return pv.transform.position + Vector3.up * 1.2f;
            return Vector3.zero;
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

        public static Vector3 GetPolePosition(int index, int totalCount, GameFeelConfigSO f)
        {
            if (f == null) return Vector3.zero;

            float spacing = f.PoleSpacing;
            float floorY = f.FloorYPosition;

            if (totalCount <= 5)
            {
                float boardWidth = (totalCount - 1) * spacing;
                float startX = -boardWidth * 0.5f;
                return new Vector3(startX + index * spacing, floorY, 0f);
            }
            else
            {
                int backRowCount = Mathf.CeilToInt(totalCount / 2.0f);
                int frontRowCount = totalCount - backRowCount;
                float rowSpacingZ = 2.5f; // Z depth spacing between rows

                if (index < backRowCount)
                {
                    // Back row (Row 0, Z = +rowSpacingZ)
                    float boardWidth = (backRowCount - 1) * spacing;
                    float startX = -boardWidth * 0.5f;
                    float offset = (backRowCount == frontRowCount) ? -spacing * 0.25f : 0f;
                    return new Vector3(startX + index * spacing + offset, floorY, rowSpacingZ);
                }
                else
                {
                    // Front row (Row 1, Z = -rowSpacingZ)
                    int frontIndex = index - backRowCount;
                    float boardWidth = (frontRowCount - 1) * spacing;
                    float startX = -boardWidth * 0.5f;
                    float offset = (backRowCount == frontRowCount) ? spacing * 0.25f : 0f;
                    return new Vector3(startX + frontIndex * spacing + offset, floorY, -rowSpacingZ);
                }
            }
        }

        public void FitCameraToBoard(int poleCount)
        {
            if (!TryGetMainCamera(out var cam))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Warn("BoardView", nameof(FitCameraToBoard), poleCount.ToString(),
                    "FitCameraToBoard: no main camera found — camera will not be adjusted.");
#endif
                return;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("BoardView", nameof(FitCameraToBoard), poleCount.ToString(),
                $"Fitting camera to {poleCount} poles. pos={F.CameraPosition}, rot={F.CameraRotation}.");
#endif
            cam.orthographic = true;
            var t = cam.transform;
            t.position = F.CameraPosition;
            t.rotation = Quaternion.Euler(F.CameraRotation);
            
            float spacing = F.PoleSpacing;
            int effectiveColumns = poleCount <= 5 ? poleCount : Mathf.CeilToInt(poleCount / 2.0f);
            float boardWidth = Mathf.Max(0f, (effectiveColumns - 1) * spacing);
            float aspect = Mathf.Max(0.01f, cam.aspect);
            float desiredHalfWidth = (boardWidth * 0.5f) + 1.5f;
            float desiredOrthoSize = desiredHalfWidth / aspect;
            
            float baseOrtho = Mathf.Max(desiredOrthoSize, 5.5f);
            if (poleCount > 5)
            {
                baseOrtho += 1.5f;
            }
            cam.orthographicSize = baseOrtho;
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
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
            }

            _floorPlane.transform.position = new Vector3(0f, F.FloorYPosition, 0f);
            _floorPlane.transform.localScale = F.FloorScale;
            
            var renderer = _floorPlane.GetComponent<MeshRenderer>();
            renderer.receiveShadows = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            
            var mat = new Material(_ringMaterialManager.GetDefaultShader());
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

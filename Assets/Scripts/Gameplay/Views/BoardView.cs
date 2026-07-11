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

        private const int RingPoolPrewarmCount = 100;

        public void SetTorusPrefab(GameObject prefab) { _torusPrefab = prefab; }

        private static Shader _cachedShader;
        private GameObject _polePrefab;

        [Inject] private IObjectPoolService _objectPoolService;
        [Inject] private VfxPrefabRegistry _vfxRegistry;
        [Inject] private IAudioService _audioService;
        [Inject] private IHapticService _hapticService;
        [Inject] private SettingsModel _settingsModel;

        private Camera _mainCamera;
        private GameFeelConfigSO F => GameFeelConfigSO.Instance;

        private readonly Dictionary<(RingColor, RingType), Material> _ringMaterialCache = new();

        private readonly List<PoleView> _spawnedPoles = new();
        private readonly List<List<GameObject>> _spawnedRings = new();
        private int _lastSelectedPoleId = -1;
        private int _animatingTargetPoleId = -1;
        private bool _ringPrewarmed;
        private Mesh _proceduralTorusMesh;
        private Mesh _proceduralConeMesh;
        private GameObject _floorPlane;
        private GameObject _tutorialArrowGo;

        public void EnsureRingPoolPrewarmed()
        {
            if (_ringPrewarmed) return;
            if (_torusPrefab == null) _torusPrefab = Resources.Load<GameObject>("Torus");
            if (_torusPrefab != null && _objectPoolService != null)
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
                poleObj.transform.localScale = F.PoleScale;

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

                    // Uniform World Scale Compensation (1.5f width, 0.44f height)
                    float targetWidth = 1.5f;
                    float targetHeight = 0.44f;
                    float meshHeight = 0.26f;
                    float localX = targetWidth / F.PoleScale.x;
                    float localY = (targetHeight / meshHeight) / F.PoleScale.y;
                    float localZ = targetWidth / F.PoleScale.z;
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
                        float targetWidth = 1.5f;
                        float targetHeight = 0.44f;
                        float meshHeight = 0.26f;
                        float localX = targetWidth / F.PoleScale.x;
                        float localY = (targetHeight / meshHeight) / F.PoleScale.y;
                        float localZ = targetWidth / F.PoleScale.z;
                        Vector3 normalScale = new Vector3(localX, localY, localZ);

                        movedRing.transform.DOLocalJump(targetLocal, F.MoveJumpPower, 1, duration)
                            .SetEase(Ease.InOutQuad)
                            .OnComplete(() =>
                            {
                                _animatingTargetPoleId = -1;
                                TriggerMoveEffects(movedRing.transform.position, movedColor);
                                _hapticService?.Vibrate(HapticType.Light);

                                DOTween.Kill(movedRing.transform);
                                movedRing.transform.DOScale(new Vector3(localX * 1.25f, localY * 0.6f, localZ * 1.25f), 0.08f)
                                    .SetEase(Ease.OutQuad)
                                    .OnComplete(() =>
                                    {
                                        movedRing.transform.DOScale(normalScale, 0.18f).SetEase(Ease.OutBack);
                                    });

                                ApplySelection();
                            });

                        movedRing.transform.DOScale(new Vector3(localX * 0.85f, localY * 1.35f, localZ * 0.85f), duration * 0.4f)
                            .SetEase(Ease.OutQuad)
                            .OnComplete(() =>
                            {
                                movedRing.transform.DOScale(normalScale, duration * 0.4f).SetEase(Ease.InQuad);
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
                            .OnComplete(() =>
                            {
                                if (isSelected)
                                {
                                    topRing.transform.DOLocalMoveY(targetY + F.TutorialArrowBobHeight * 0.4f, 0.6f)
                                        .SetEase(Ease.InOutSine)
                                        .SetLoops(-1, LoopType.Yoyo);
                                }
                            });
                    }

                    // --- Warm Selection Glow ---
                    var lightChildTransform = topRing.transform.Find("SelectionGlowLight");
                    var ringRenderer = topRing.GetComponentInChildren<Renderer>();
                    var propBlock = new MaterialPropertyBlock();

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

            if (_tutorialArrowGo == null)
            {
                _tutorialArrowGo = new GameObject("TutorialArrow");
                var meshFilter = _tutorialArrowGo.AddComponent<MeshFilter>();
                if (_proceduralConeMesh == null)
                {
                    _proceduralConeMesh = CreateProceduralConeMesh();
                }
                meshFilter.sharedMesh = _proceduralConeMesh;

                var renderer = _tutorialArrowGo.AddComponent<MeshRenderer>();
                var mat = new Material(GetDefaultShader());
                mat.color = F.TutorialArrowColor;
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", F.TutorialArrowColor);
                mat.SetFloat("_Metallic", 0.1f);
                mat.SetFloat("_Smoothness", 0.8f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", F.TutorialArrowColor * 0.4f);
                renderer.sharedMaterial = mat;

                _tutorialArrowGo.transform.localScale = F.TutorialArrowScale;

                var labelGo = new GameObject("Label", typeof(TextMesh));
                labelGo.transform.SetParent(_tutorialArrowGo.transform, false);
                labelGo.transform.localPosition = new Vector3(0f, 1.3f, 0f);
                labelGo.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
                labelGo.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
                
                var textMesh = labelGo.GetComponent<TextMesh>();
                textMesh.fontSize = 48;
                textMesh.anchor = TextAnchor.MiddleCenter;
                textMesh.alignment = TextAlignment.Center;
                textMesh.color = Color.white;
                textMesh.fontStyle = FontStyle.Bold;

                _tutorialArrowGo.transform.DOLocalMoveY(F.TutorialArrowBobHeight, F.TutorialArrowBobSpeed)
                    .SetRelative(true)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);

                _tutorialArrowGo.transform.DOLocalRotate(new Vector3(0f, 360f, 0f), 2.5f, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Restart);
            }

            var targetPole = _spawnedPoles[poleId];
            _tutorialArrowGo.transform.SetParent(targetPole.transform, false);
            float startY = F.PoleScale.y + 0.6f;
            _tutorialArrowGo.transform.localPosition = new Vector3(0f, startY, 0f);
            _tutorialArrowGo.SetActive(true);

            var label = _tutorialArrowGo.transform.Find("Label")?.GetComponent<TextMesh>();
            if (label != null)
            {
                label.text = labelText;
            }
        }

        public void HideTutorialArrow()
        {
            if (_tutorialArrowGo != null)
            {
                _tutorialArrowGo.SetActive(false);
                _tutorialArrowGo.transform.SetParent(null);
            }
        }

        private Mesh CreateProceduralConeMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "ProceduralCone";
            int segments = 16;
            int numVertices = segments + 2;
            Vector3[] vertices = new Vector3[numVertices];
            int[] triangles = new int[segments * 6];

            vertices[0] = new Vector3(0f, 0f, 0f);
            vertices[numVertices - 1] = new Vector3(0f, -1f, 0f);

            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 0.4f, 0f, Mathf.Sin(angle) * 0.4f);
            }

            int tIdx = 0;
            for (int i = 0; i < segments; i++)
            {
                int next = (i == segments - 1) ? 1 : i + 2;
                triangles[tIdx++] = 0;
                triangles[tIdx++] = next;
                triangles[tIdx++] = i + 1;
                triangles[tIdx++] = i + 1;
                triangles[tIdx++] = next;
                triangles[tIdx++] = numVertices - 1;
            }

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
                Destroy(_tutorialArrowGo);
                _tutorialArrowGo = null;
            }
            if (_proceduralConeMesh != null)
            {
                Destroy(_proceduralConeMesh);
                _proceduralConeMesh = null;
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

        private void EnsurePolePrefabCreated()
        {
            if (_polePrefab != null) return;
            
            _polePrefab = new GameObject("Pole_Template");
            
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(_polePrefab.transform, false);
            body.transform.localPosition = new Vector3(0f, 0f, 0f);
            body.transform.localScale = new Vector3(1f, 1f, 1f);
            var bodyCol = body.GetComponent<Collider>();
            if (bodyCol != null) DestroyImmediate(bodyCol);
            
            var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "Cap";
            cap.transform.SetParent(_polePrefab.transform, false);
            cap.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            
            float capYScale = F.PoleScale.x / F.PoleScale.y;
            cap.transform.localScale = new Vector3(1f, capYScale, 1f);
            var capCol = cap.GetComponent<Collider>();
            if (capCol != null) DestroyImmediate(capCol);

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
            GameObject ringObj = null;
            if (_torusPrefab == null) _torusPrefab = Resources.Load<GameObject>("Torus");
            if (_torusPrefab != null && _objectPoolService != null)
            {
                ringObj = _objectPoolService.Spawn(_torusPrefab, Vector3.zero, Quaternion.identity);
            }
            else if (_torusPrefab != null)
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
                meshFilter.sharedMesh = GetProceduralTorusMesh();
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

        private static Material GetPoleMaterial(bool locked)
        {
            var feel = GameFeelConfigSO.Instance;
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
            Color baseColor = RingPalette.Get(color);
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
                    popInstance?.GetComponent<RingPopVfx>()?.Initialize(RingPalette.Get(color));
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
                cycle.AddComponent<RainbowCycle>();
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

        private static void PlayRingPlacePulse(GameObject ringObj)
        {
            if (ringObj == null) return;
            var f = GameFeelConfigSO.Instance;
            DOTween.Kill(ringObj.transform);
            ringObj.transform.localScale = ringObj.transform.localScale * f.RingPlacePulseScale;
            ringObj.transform.DOScale(ringObj.transform.localScale / f.RingPlacePulseScale, f.RingPlacePulseDuration)
                .SetEase(Ease.OutBack);
        }

        private bool TryGetMainCamera(out Camera cam)
        {
            if (_mainCamera != null) { cam = _mainCamera; return true; }
            _mainCamera = Camera.main ?? Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
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
            _floorPlane = GameObject.Find("ShadowFloorPlane");
            if (_floorPlane != null) return;

            _floorPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _floorPlane.name = "ShadowFloorPlane";
            _floorPlane.transform.position = new Vector3(0f, -0.51f, 0f);
            _floorPlane.transform.localScale = new Vector3(10f, 1f, 10f); // 100x100 units
            
            var col = _floorPlane.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            var renderer = _floorPlane.GetComponent<MeshRenderer>();
            renderer.receiveShadows = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            
            var mat = new Material(GetDefaultShader());
            Color floorColor = new Color(0.88f, 0.92f, 0.97f); // matches bottom background gradient
            mat.color = floorColor;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", floorColor);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.1f);
            renderer.sharedMaterial = mat;
        }

        private Mesh GetProceduralTorusMesh()
        {
            if (_proceduralTorusMesh == null)
            {
                _proceduralTorusMesh = CreateProceduralTorusMesh(0.37f, 0.13f, 32, 24);
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

        private void Start()
        {
            _renderer = GetComponentInParent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (_renderer == null) return;
            var f = GameFeelConfigSO.Instance;
            float hue = (Time.time * f.RainbowHueSpeed) % 1f;
            Color color = Color.HSVToRGB(hue, f.RainbowSaturation, f.RainbowValue);
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_Color", color);
            _propBlock.SetColor("_BaseColor", color);
            _propBlock.SetColor("_EmissionColor", color * 0.3f);
            _renderer.SetPropertyBlock(_propBlock);
        }
    }
}

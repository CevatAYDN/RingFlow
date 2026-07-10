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
        [Inject] private SettingsModel _settingsModel;

        private Camera _mainCamera;
        private GameFeelConfigSO F => GameFeelConfigSO.Instance;

        private readonly Dictionary<(RingColor, RingType), Material> _ringMaterialCache = new();

        private readonly List<PoleView> _spawnedPoles = new();
        private readonly List<List<GameObject>> _spawnedRings = new();
        private int _lastSelectedPoleId = -1;
        private int _animatingTargetPoleId = -1;
        private bool _ringPrewarmed;

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

                var renderer = poleObj.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = GetPoleMaterial(poleData.IsLocked);
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
                    ringObj.transform.localScale = _torusPrefab != null ? F.RingScaleTorus : F.RingScaleFallback;

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
                        movedRing.transform.DOLocalJump(targetLocal, F.MoveJumpPower, 1, duration)
                            .SetEase(Ease.InOutQuad)
                            .OnComplete(() =>
                            {
                                _animatingTargetPoleId = -1;
                                TriggerMoveEffects(movedRing.transform.position, movedColor);
                                PlayRingPlacePulse(movedRing);
                                ApplySelection();
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

        public void SetSelectedPole(int poleId) { _lastSelectedPoleId = poleId; ApplySelection(); }

        public void FlashPoleError(int poleId)
        {
            var pv = GetPoleView(poleId);
            if (pv == null) return;
            pv.FlashError();
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
                        topRing.transform.localPosition = new Vector3(0f, targetY, 0f);
                    else
                        topRing.transform.DOLocalMoveY(targetY, duration).SetEase(Ease.OutQuad);
                }
            }
        }

        public void ClearBoard()
        {
            _ringMaterialCache.Clear();
            foreach (var pole in _spawnedPoles)
                if (pole != null) RecyclePole(pole.gameObject);
            _spawnedPoles.Clear();
            foreach (var list in _spawnedRings)
                foreach (var ring in list)
                    if (ring != null) RecycleRing(ring);
            _spawnedRings.Clear();
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
            _polePrefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _polePrefab.name = "Pole_Template";
            var capsule = _polePrefab.GetComponent<CapsuleCollider>();
            if (capsule != null) DestroyImmediate(capsule);
            var box = _polePrefab.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = _polePrefab.AddComponent<BoxCollider>();
                box.size = new Vector3(F.PoleSpacing * F.PoleColliderWidthFraction, 3.0f, 2.0f);
            }
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
            if (_torusPrefab == null) _torusPrefab = Resources.Load<GameObject>("Torus");
            if (_torusPrefab != null && _objectPoolService != null)
            {
                var ringObj = _objectPoolService.Spawn(_torusPrefab, Vector3.zero, Quaternion.identity);
                if (ringObj != null)
                {
                    ringObj.name = "Ring_Torus";
                    var col = ringObj.GetComponent<Collider>();
                    if (col != null) Destroy(col);
                    ringObj.SetActive(true);
                    KillTweens(ringObj);
                    return ringObj;
                }
            }
            if (_torusPrefab != null)
            {
                var ringObj = Instantiate(_torusPrefab);
                ringObj.name = "Ring_Torus";
                var col = ringObj.GetComponent<Collider>();
                if (col != null) Destroy(col);
                ringObj.SetActive(true);
                KillTweens(ringObj);
                return ringObj;
            }
            var fallback = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fallback.name = "Ring_Fallback";
            var fbCol = fallback.GetComponent<Collider>();
            if (fbCol != null) Destroy(fbCol);
            return fallback;
        }

        private void RecycleRing(GameObject ring)
        {
            KillTweens(ring);
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
            if (locked)
            {
                if (_lockedPoleMaterial == null)
                {
                    var darkColor = new Color(0.12f, 0.12f, 0.14f);
                    _lockedPoleMaterial = new Material(GetDefaultShader()) { color = darkColor, name = "PoleMat_Locked" };
                    if (_lockedPoleMaterial.HasProperty("_BaseColor"))
                        _lockedPoleMaterial.SetColor("_BaseColor", darkColor);
                    _lockedPoleMaterial.SetFloat("_Metallic", 0.9f);
                    _lockedPoleMaterial.SetFloat("_Smoothness", 0.9f);
                }
                return _lockedPoleMaterial;
            }
            if (_openPoleMaterial == null)
            {
                var slateColor = new Color(0.20f, 0.22f, 0.25f);
                _openPoleMaterial = new Material(GetDefaultShader()) { color = slateColor, name = "PoleMat_Open" };
                if (_openPoleMaterial.HasProperty("_BaseColor"))
                    _openPoleMaterial.SetColor("_BaseColor", slateColor);
                _openPoleMaterial.SetFloat("_Metallic", 0.8f);
                _openPoleMaterial.SetFloat("_Smoothness", 0.8f);
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
            mat.SetFloat("_Metallic", 0.1f);
            mat.SetFloat("_Smoothness", 0.85f);
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
                _audioService.PlaySfx(ProceduralAudio.GetOrCreateMoveClip(), 1.0f);
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

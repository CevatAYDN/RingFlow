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
        [SerializeField] private GameObject _torusPrefab;
        [SerializeField] private float _poleSpacing = 2.5f;

        public void SetTorusPrefab(GameObject prefab) { _torusPrefab = prefab; }

        private static Shader _cachedShader;

        private readonly Queue<GameObject> _ringPool = new();
        private readonly Queue<GameObject> _polePool = new();
        private readonly Dictionary<(RingColor, RingType), Material> _ringMaterialCache = new();

        private readonly List<PoleView> _spawnedPoles = new();
        private readonly List<List<GameObject>> _spawnedRings = new();
        private int _lastSelectedPoleId = -1;
        private int _animatingTargetPoleId = -1;

        public void BuildBoard(List<PoleState> poles)
        {
            var visualBoard = GameObject.Find("RingFlow_VisualBoard");
            if (visualBoard != null)
                Destroy(visualBoard);

            ClearBoard();

            for (int p = 0; p < poles.Count; p++)
            {
                var poleData = poles[p];
                var poleObj = AcquirePole();

                poleObj.name = $"Pole_{p}" + (poleData.IsLocked ? " [LOCKED]" : "");
                poleObj.transform.SetParent(transform, false);
                poleObj.transform.localPosition = new Vector3(p * _poleSpacing, 2.0f, 0f);
                poleObj.transform.localRotation = Quaternion.identity;
                poleObj.transform.localScale = new Vector3(0.2f, 2.0f, 0.2f);

                var poleView = poleObj.GetComponent<PoleView>();
                if (poleView == null)
                    poleView = poleObj.AddComponent<PoleView>();
                poleObj.SetActive(true);

                poleView.PoleId = p;
                poleView.SetLocked(poleData.IsLocked);

                var renderer = poleObj.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = CreatePoleMaterial(poleData.IsLocked);
                poleView.SyncMaterial();

                _spawnedPoles.Add(poleView);

                var ringList = new List<GameObject>();
                for (int r = 0; r < poleData.Rings.Count; r++)
                {
                    var ringData = poleData.Rings[r];
                    GameObject ringObj = AcquireRing();

                    ringObj.name = $"Ring_{r}_{ringData.Color}_{ringData.Type}";
                    ringObj.transform.SetParent(poleObj.transform, false);

                    float targetY = -0.9f + (r * 0.4f);
                    if (p == _lastSelectedPoleId && r == poleData.Rings.Count - 1)
                    {
                        var settings = NexusRuntime.CurrentContext?.TryResolve<SettingsModel>();
                        bool reduceMotion = settings != null && settings.ReduceMotion.Value;
                        if (!reduceMotion)
                        {
                            targetY += 0.35f;
                        }
                    }

                    ringObj.transform.localPosition = new Vector3(0f, targetY, 0f);
                    ringObj.transform.localRotation = Quaternion.identity;
                    if (_torusPrefab != null)
                        ringObj.transform.localScale = new Vector3(3.5f, 0.2f, 3.5f);
                    else
                        ringObj.transform.localScale = new Vector3(4.0f, 0.08f, 4.0f);

                    var ringRenderer = ringObj.GetComponentInChildren<Renderer>();
                    if (ringRenderer != null)
                        ringRenderer.sharedMaterial = GetRingMaterial(ringData.Color, ringData.Type);

                    AddSpecialOverlay(ringObj, ringData);

                    ringList.Add(ringObj);
                }
                _spawnedRings.Add(ringList);
            }

            ApplySelection();
        }

        public void AnimateRingMove(int fromPoleId, int toPoleId, List<PoleState> poles)
        {
            var settings = NexusRuntime.CurrentContext?.TryResolve<SettingsModel>();
            bool reduceMotion = settings != null && settings.ReduceMotion.Value;
            bool slowMode = settings != null && settings.SlowMode.Value;
            float speedMultiplier = slowMode ? 2.0f : 1.0f;
            float duration = 0.3f * speedMultiplier;

            _animatingTargetPoleId = toPoleId;

            Vector3 oldRingWorldPos = Vector3.zero;
            RingColor movedColor = RingColor.None;
            if (fromPoleId >= 0 && fromPoleId < _spawnedRings.Count
                && _spawnedRings[fromPoleId].Count > 0)
            {
                var topRing = _spawnedRings[fromPoleId][^1];
                if (topRing != null)
                {
                    oldRingWorldPos = topRing.transform.position;
                    if (fromPoleId < poles.Count && poles[fromPoleId].Rings.Count > 0)
                    {
                        movedColor = poles[fromPoleId].Rings[^1].Color;
                    }
                }
            }

            BuildBoard(poles);

            if (toPoleId >= 0 && toPoleId < _spawnedRings.Count
                && _spawnedRings[toPoleId].Count > 0)
            {
                var movedRing = _spawnedRings[toPoleId][^1];
                if (movedRing != null)
                {
                    DOTween.Kill(movedRing.transform); // Kill active selection/deselection tweens on this transform!
                    movedRing.transform.position = oldRingWorldPos;
                    int ringIndex = _spawnedRings[toPoleId].Count - 1;
                    var targetLocal = new Vector3(0f, -0.9f + (ringIndex * 0.4f), 0f);
                    
                    if (reduceMotion)
                    {
                        movedRing.transform.localPosition = targetLocal;
                        _animatingTargetPoleId = -1;
                        TriggerMoveEffects(movedRing.transform.position, movedColor);
                        ApplySelection();
                    }
                    else
                    {
                        movedRing.transform.DOLocalJump(targetLocal, 0.8f, 1, duration)
                            .SetEase(Ease.InOutQuad)
                            .OnComplete(() => {
                                _animatingTargetPoleId = -1;
                                TriggerMoveEffects(movedRing.transform.position, movedColor);
                                ApplySelection();
                            });
                    }
                }
            }
            else
            {
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
            _lastSelectedPoleId = poleId;
            ApplySelection();
        }

        public void FlashPoleError(int poleId)
        {
            var pv = GetPoleView(poleId);
            if (pv != null)
            {
                pv.FlashError();
                
                // Play procedural error SFX
                var audioService = NexusRuntime.CurrentContext?.TryResolve<IAudioService>();
                if (audioService != null)
                {
                    var errClip = ProceduralAudio.GetOrCreateErrorClip();
                    audioService.PlaySfx(errClip, 1.0f);
                }
            }
        }

        private void ApplySelection()
        {
            var settings = NexusRuntime.CurrentContext?.TryResolve<SettingsModel>();
            bool reduceMotion = settings != null && settings.ReduceMotion.Value;
            bool slowMode = settings != null && settings.SlowMode.Value;
            float speedMultiplier = slowMode ? 2.0f : 1.0f;
            float duration = 0.15f * speedMultiplier;

            for (int i = 0; i < _spawnedPoles.Count; i++)
            {
                if (_spawnedPoles[i] == null) continue;
                
                bool isSelected = (i == _lastSelectedPoleId);
                _spawnedPoles[i].SetSelected(isSelected);

                // Skip Y-animation for the pole whose ring is currently animating (jumping)
                if (i == _animatingTargetPoleId) continue;

                // Animate top ring of this pole
                if (i < _spawnedRings.Count && _spawnedRings[i].Count > 0)
                {
                    var topRing = _spawnedRings[i][^1];
                    if (topRing != null)
                    {
                        int ringIndex = _spawnedRings[i].Count - 1;
                        float targetY = -0.9f + (ringIndex * 0.4f) + (isSelected ? 0.35f : 0.0f);
                        
                        DOTween.Kill(topRing.transform);
                        if (reduceMotion)
                        {
                            topRing.transform.localPosition = new Vector3(0f, targetY, 0f);
                        }
                        else
                        {
                            topRing.transform.DOLocalMoveY(targetY, duration).SetEase(Ease.OutQuad);
                        }
                    }
                }
            }
        }

        public void ClearBoard()
        {
            _ringMaterialCache.Clear(); // Clear cached materials to apply colorblind modes instantly!
            foreach (var pole in _spawnedPoles)
            {
                if (pole != null) RecyclePole(pole.gameObject);
            }
            _spawnedPoles.Clear();

            foreach (var list in _spawnedRings)
            {
                foreach (var ring in list)
                {
                    if (ring != null) RecycleRing(ring);
                }
            }
            _spawnedRings.Clear();
        }

        private static Shader GetDefaultShader()
        {
            if (_cachedShader != null) return _cachedShader;
            _cachedShader = Shader.Find("Universal Render Pipeline/Lit");
            if (_cachedShader == null) _cachedShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (_cachedShader == null) _cachedShader = Shader.Find("Standard");
            return _cachedShader;
        }

        private GameObject AcquirePole()
        {
            while (_polePool.Count > 0)
            {
                var pole = _polePool.Dequeue();
                if (pole != null) return pole;
            }
            var poleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var capsule = poleObj.GetComponent<CapsuleCollider>();
            if (capsule != null) DestroyImmediate(capsule);
            var box = poleObj.AddComponent<BoxCollider>();
            box.size = new Vector3(3.0f, 2.0f, 3.0f);
            return poleObj;
        }

        private void RecyclePole(GameObject pole)
        {
            pole.SetActive(false);
            pole.transform.SetParent(null);
            var renderer = pole.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null
                && renderer.sharedMaterial.name.StartsWith("PoleMat_"))
            {
                Destroy(renderer.sharedMaterial);
            }
            _polePool.Enqueue(pole);
        }

        private GameObject AcquireRing()
        {
            while (_ringPool.Count > 0)
            {
                var ring = _ringPool.Dequeue();
                if (ring != null)
                {
                    ring.SetActive(true);
                    KillTweens(ring);
                    return ring;
                }
            }

            // Load Torus.obj from Resources if no prefab assigned yet
            if (_torusPrefab == null)
                _torusPrefab = Resources.Load<GameObject>("Torus");

            if (_torusPrefab != null)
            {
                var ringObj = Instantiate(_torusPrefab);
                ringObj.name = "Ring_Torus";
                var col = ringObj.GetComponent<Collider>();
                if (col != null) Destroy(col);

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
            
            // Destroy any special overlays or cycle scripts
            for (int i = ring.transform.childCount - 1; i >= 0; i--)
            {
                var child = ring.transform.GetChild(i);
                if (child.name == "SpecialOverlay" || child.name == "RainbowCycle")
                {
                    Destroy(child.gameObject);
                }
            }

            ring.SetActive(false);
            ring.transform.SetParent(null);
            _ringPool.Enqueue(ring);
        }

        private static void KillTweens(GameObject obj)
        {
            if (obj != null)
                DOTween.Kill(obj.transform);
        }

        private static int _poleMaterialIdCounter;
        private static Material CreatePoleMaterial(bool locked)
        {
            var mat = new Material(GetDefaultShader())
            {
                color = locked ? Color.black : Color.white
            };
            mat.name = "PoleMat_" + (locked ? "locked" : "open") + "_"
                + System.Threading.Interlocked.Increment(ref _poleMaterialIdCounter);
            mat.SetFloat("_Metallic", 0.3f);
            mat.SetFloat("_Smoothness", 0.4f);
            return mat;
        }

        private Material GetRingMaterial(RingColor color, RingType type)
        {
            var key = (color, type);
            if (_ringMaterialCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var mat = new Material(GetDefaultShader());
            Color baseColor = RingPalette.Get(color);
            mat.color = baseColor;
            mat.SetFloat("_Metallic", 0.6f);
            mat.SetFloat("_Smoothness", 0.7f);

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
                    mat.SetFloat("_Metallic", 0.1f);
                    mat.SetFloat("_Smoothness", 0.9f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.3f, 0.6f, 0.8f, 1f));
                    break;

                case RingType.Key:
                case RingType.Locked:
                    mat.color = new Color(1f, 0.84f, 0f);
                    mat.SetFloat("_Metallic", 0.8f);
                    mat.SetFloat("_Smoothness", 0.6f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.5f, 0.4f, 0f, 1f));
                    break;

                case RingType.Stone:
                    mat.color = new Color(0.4f, 0.38f, 0.35f);
                    mat.SetFloat("_Metallic", 0f);
                    mat.SetFloat("_Smoothness", 0.1f);
                    break;

                case RingType.Glass:
                    mat.color = new Color(1f, 1f, 1f, 0.25f);
                    mat.SetFloat("_Metallic", 0f);
                    mat.SetFloat("_Smoothness", 0.95f);
                    SetFadeMode(mat);
                    break;

                case RingType.Rainbow:
                    mat.SetFloat("_Metallic", 0.5f);
                    mat.SetFloat("_Smoothness", 0.8f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.Lerp(baseColor, Color.white, 0.3f));
                    break;

                case RingType.Ghost:
                    mat.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.15f);
                    mat.SetFloat("_Metallic", 0.3f);
                    mat.SetFloat("_Smoothness", 0.3f);
                    SetFadeMode(mat);
                    break;

                case RingType.Bomb:
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.8f, 0.2f, 0f, 1f));
                    break;

                case RingType.Chain:
                    mat.SetFloat("_Metallic", 0.7f);
                    mat.SetFloat("_Smoothness", 0.3f);
                    break;

                case RingType.Magnet:
                    mat.SetFloat("_Metallic", 0.9f);
                    mat.SetFloat("_Smoothness", 0.5f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.5f, 0f, 0.5f, 1f));
                    break;

                case RingType.Paint:
                    mat.SetFloat("_Smoothness", 0.9f);
                    break;

                case RingType.Mystery:
                    mat.color = new Color(0.3f, 0.3f, 0.3f);
                    mat.SetFloat("_Metallic", 0.4f);
                    mat.SetFloat("_Smoothness", 0.6f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.2f, 0.2f, 0.2f, 1f));
                    break;
            }
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
            // Play procedural move SFX
            var audioService = NexusRuntime.CurrentContext?.TryResolve<IAudioService>();
            if (audioService != null)
            {
                var moveClip = ProceduralAudio.GetOrCreateMoveClip();
                audioService.PlaySfx(moveClip, 1.0f);
            }

            // Spawn color pop particle VFX from object pool
            var pool = NexusRuntime.CurrentContext?.TryResolve<IObjectPoolService>();
            if (pool != null && GameplayLifecycle.RingPopPrefab != null)
            {
                var popInstance = pool.Spawn(GameplayLifecycle.RingPopPrefab, position, Quaternion.identity);
                var vfx = popInstance.GetComponent<RingPopVfx>();
                if (vfx != null)
                {
                    vfx.Initialize(RingPalette.Get(color));
                }
            }
        }

        private void AddSpecialOverlay(GameObject ringObj, RingData ringData)
        {
            if (ringData.Type == RingType.Standard) return;

            string overlayText = "";
            Color textColor = Color.white;

            switch (ringData.Type)
            {
                case RingType.Mystery:
                    overlayText = "?";
                    textColor = Color.yellow;
                    break;
                case RingType.Frozen:
                    overlayText = "❄";
                    textColor = Color.cyan;
                    break;
                case RingType.Locked:
                    overlayText = "🔑";
                    textColor = new Color(1f, 0.84f, 0f);
                    break;
                case RingType.Key:
                    overlayText = "🔑";
                    textColor = new Color(1f, 0.84f, 0f);
                    break;
                case RingType.Stone:
                    overlayText = "🧱";
                    textColor = Color.gray;
                    break;
                case RingType.Glass:
                    overlayText = "💎";
                    textColor = new Color(1f, 1f, 1f, 0.5f);
                    break;
                case RingType.Rainbow:
                    overlayText = "🌈";
                    textColor = Color.white;
                    var cycle = new GameObject("RainbowCycle");
                    cycle.transform.SetParent(ringObj.transform, false);
                    cycle.AddComponent<RainbowCycle>();
                    break;
                case RingType.Bomb:
                    overlayText = ringData.AdditionalData.ToString();
                    textColor = Color.red;
                    break;
                case RingType.Chain:
                    overlayText = "⛓";
                    textColor = Color.white;
                    break;
                case RingType.Magnet:
                    overlayText = "🧲";
                    textColor = Color.magenta;
                    break;
                case RingType.Paint:
                    overlayText = "🎨";
                    textColor = Color.green;
                    break;
                case RingType.Ghost:
                    overlayText = "👻";
                    textColor = new Color(1f, 1f, 1f, 0.3f);
                    break;
            }

            if (!string.IsNullOrEmpty(overlayText))
            {
                var overlayGo = new GameObject("SpecialOverlay", typeof(TextMesh));
                overlayGo.transform.SetParent(ringObj.transform, false);
                overlayGo.transform.localPosition = new Vector3(0f, 0.12f, 0f);
                overlayGo.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
                overlayGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                var textMesh = overlayGo.GetComponent<TextMesh>();
                textMesh.text = overlayText;
                textMesh.fontSize = 64;
                textMesh.color = textColor;
                textMesh.anchor = TextAnchor.MiddleCenter;
                textMesh.alignment = TextAlignment.Center;
                textMesh.fontStyle = FontStyle.Bold;
            }
        }
    }

    public class RainbowCycle : MonoBehaviour
    {
        private Renderer _renderer;
        private Material _material;

        private void Start()
        {
            _renderer = GetComponentInParent<Renderer>();
            if (_renderer != null)
            {
                _material = _renderer.material;
            }
        }

        private void Update()
        {
            if (_material == null) return;
            float hue = (Time.time * 0.25f) % 1.0f;
            Color color = Color.HSVToRGB(hue, 0.8f, 0.9f);
            _material.color = color;
            _material.SetColor("_EmissionColor", color * 0.3f);
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}

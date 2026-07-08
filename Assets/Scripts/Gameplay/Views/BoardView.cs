using System.Collections.Generic;
using DG.Tweening;
using Nexus.Core;
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
                    ringObj.transform.localPosition = new Vector3(0f, -0.9f + (r * 0.4f), 0f);
                    ringObj.transform.localRotation = Quaternion.identity;
                    if (_torusPrefab != null)
                        ringObj.transform.localScale = new Vector3(3.5f, 0.2f, 3.5f);
                    else
                        ringObj.transform.localScale = new Vector3(4.0f, 0.08f, 4.0f);

                    var ringRenderer = ringObj.GetComponentInChildren<Renderer>();
                    if (ringRenderer != null)
                        ringRenderer.sharedMaterial = GetRingMaterial(ringData.Color, ringData.Type);

                    ringList.Add(ringObj);
                }
                _spawnedRings.Add(ringList);
            }

            ApplySelection();
        }

        public void AnimateRingMove(int fromPoleId, int toPoleId, List<PoleState> poles)
        {
            Vector3 oldRingWorldPos = Vector3.zero;
            if (fromPoleId >= 0 && fromPoleId < _spawnedRings.Count
                && _spawnedRings[fromPoleId].Count > 0)
            {
                var topRing = _spawnedRings[fromPoleId][^1];
                if (topRing != null)
                    oldRingWorldPos = topRing.transform.position;
            }

            BuildBoard(poles);

            if (toPoleId >= 0 && toPoleId < _spawnedRings.Count
                && _spawnedRings[toPoleId].Count > 0)
            {
                var movedRing = _spawnedRings[toPoleId][^1];
                if (movedRing != null)
                {
                    movedRing.transform.position = oldRingWorldPos;
                    int ringIndex = _spawnedRings[toPoleId].Count - 1;
                    var targetLocal = new Vector3(0f, -0.9f + (ringIndex * 0.4f), 0f);
                    movedRing.transform.DOLocalJump(targetLocal, 0.8f, 1, 0.3f)
                        .SetEase(Ease.InOutQuad);
                }
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
            if (pv != null) pv.FlashError();
        }

        private void ApplySelection()
        {
            for (int i = 0; i < _spawnedPoles.Count; i++)
            {
                if (_spawnedPoles[i] == null) continue;
                _spawnedPoles[i].SetSelected(i == _lastSelectedPoleId);
            }
        }

        public void ClearBoard()
        {
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
    }
}

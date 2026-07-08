using System.Collections.Generic;
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
        private Material _blackPoleMaterial;
        private Material _defaultPoleMaterial;

        private readonly List<PoleView> _spawnedPoles = new();
        private readonly List<List<GameObject>> _spawnedRings = new();

        public void BuildBoard(List<PoleState> poles)
        {
            ClearBoard();

            if (_torusPrefab == null)
            {
                _torusPrefab = Resources.Load<GameObject>("Torus");
            }
#if UNITY_EDITOR
            if (_torusPrefab == null)
            {
                _torusPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Torus.obj");
            }
#endif

            for (int p = 0; p < poles.Count; p++)
            {
                var poleData = poles[p];

                var poleObj = AcquirePole();
                poleObj.name = $"Pole_{p}" + (poleData.IsLocked ? " [LOCKED]" : "");

                poleObj.transform.SetParent(transform);
                poleObj.transform.position = new Vector3(p * _poleSpacing, 2.0f, 0f);
                poleObj.transform.localScale = new Vector3(0.2f, 2.0f, 0.2f);

                var poleView = poleObj.GetComponent<PoleView>();
                poleView.PoleId = p;
                _spawnedPoles.Add(poleView);

                var renderer = poleObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = poleData.IsLocked ? GetBlackPoleMaterial() : GetDefaultPoleMaterial(renderer);
                }

                var ringList = new List<GameObject>();
                for (int r = 0; r < poleData.Rings.Count; r++)
                {
                    var ringData = poleData.Rings[r];
                    GameObject ringObj = AcquireRing();

                    ringObj.name = $"Ring_{r}_{ringData.Color}_{ringData.Type}";
                    ringObj.transform.SetParent(poleObj.transform);
                    ringObj.transform.localPosition = new Vector3(0f, -0.9f + (r * 0.4f), 0f);
                    if (_torusPrefab != null)
                    {
                        ringObj.transform.localRotation = Quaternion.identity;
                        ringObj.transform.localScale = new Vector3(3.5f, 0.2f, 3.5f);
                    }
                    else
                    {
                        ringObj.transform.localScale = new Vector3(4.0f, 0.08f, 4.0f);
                    }

                    var ringRenderer = ringObj.GetComponentInChildren<Renderer>();
                    if (ringRenderer != null)
                    {
                        ringRenderer.sharedMaterial = GetRingMaterial(ringData.Color, ringData.Type);
                    }

                    ringList.Add(ringObj);
                }
                _spawnedRings.Add(ringList);
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
                if (pole != null)
                {
                    pole.SetActive(true);
                    return pole;
                }
            }
            var poleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var capsule = poleObj.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                DestroyImmediate(capsule);
            }
            var box = poleObj.AddComponent<BoxCollider>();
            box.size = new Vector3(3.0f, 2.0f, 3.0f);
            poleObj.AddComponent<PoleView>();
            return poleObj;
        }

        private void RecyclePole(GameObject pole)
        {
            pole.SetActive(false);
            pole.transform.SetParent(null);
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
                    return ring;
                }
            }
            GameObject ringObj;
            if (_torusPrefab != null)
            {
                ringObj = Instantiate(_torusPrefab);
            }
            else
            {
                ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            }

            // Strip the ring's collider so clicks pass through to the PoleView underneath.
            var col = ringObj.GetComponent<Collider>();
            if (col == null) col = ringObj.GetComponentInChildren<Collider>();
            if (col != null)
            {
                Destroy(col);
            }
            return ringObj;
        }

        private void RecycleRing(GameObject ring)
        {
            ring.SetActive(false);
            ring.transform.SetParent(null);
            _ringPool.Enqueue(ring);
        }

        private Material GetBlackPoleMaterial()
        {
            if (_blackPoleMaterial == null)
            {
                _blackPoleMaterial = new Material(GetDefaultShader());
                _blackPoleMaterial.color = Color.black;
            }
            return _blackPoleMaterial;
        }

        private Material GetDefaultPoleMaterial(Renderer renderer)
        {
            if (_defaultPoleMaterial == null && renderer != null)
            {
                _defaultPoleMaterial = renderer.sharedMaterial;
            }
            if (_defaultPoleMaterial == null)
            {
                _defaultPoleMaterial = new Material(GetDefaultShader());
                _defaultPoleMaterial.color = Color.white;
            }
            return _defaultPoleMaterial;
        }

        private Material GetRingMaterial(RingColor color, RingType type)
        {
            var key = (color, type);
            if (_ringMaterialCache.TryGetValue(key, out var cachedMat) && cachedMat != null)
            {
                return cachedMat;
            }
            var mat = new Material(GetDefaultShader());
            mat.color = RingPalette.Get(color);
            ApplySpecialRingMaterial(mat, type);
            _ringMaterialCache[key] = mat;
            return mat;
        }

        private void ApplySpecialRingMaterial(Material mat, RingType type)
        {
            switch (type)
            {
                case RingType.Frozen:
                    mat.color = Color.cyan;
                    break;
                case RingType.Key:
                case RingType.Locked:
                    mat.color = new Color(1f, 0.84f, 0f);
                    break;
                case RingType.Stone:
                    mat.color = Color.grey;
                    break;
                case RingType.Glass:
                    mat.color = new Color(1f, 1f, 1f, 0.3f);
                    mat.SetFloat("_Mode", 3f);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.renderQueue = 3000;
                    break;
                case RingType.Rainbow:
                    mat.color = Color.magenta;
                    break;
                case RingType.Ghost:
                    mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, 0.1f);
                    mat.SetFloat("_Mode", 3f);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = 3000;
                    break;
            }
        }
    }
}

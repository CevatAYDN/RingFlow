using System.Collections.Generic;
using Nexus.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RingFlow.Gameplay
{
    [Mediator(typeof(BoardMediator))]
    public class BoardView : View
    {
        [SerializeField] private GameObject _torusPrefab;
        [SerializeField] private float _poleSpacing = 2.5f;

        private readonly List<PoleView> _spawnedPoles = new();
        private readonly List<List<GameObject>> _spawnedRings = new();

        // Cached type lookup so the asmdef does not need a hard reference to
        // Unity.InputSystem.ForUI. Resolved once at first use.
        private static System.Type s_inputModuleType;

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureInputSetup();
        }

        private void EnsureInputSetup()
        {
            EnsureInputModuleType();
            var eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var esObj = new GameObject("EventSystem");
                eventSystem = esObj.AddComponent<EventSystem>();
                if (s_inputModuleType != null) esObj.AddComponent(s_inputModuleType);
            }
            else
            {
                var existing = eventSystem.GetComponent<BaseInputModule>();
                if (existing != null && s_inputModuleType != null && !s_inputModuleType.IsInstanceOfType(existing))
                {
                    Destroy(existing);
                    eventSystem.gameObject.AddComponent(s_inputModuleType);
                }
                else if (existing == null && s_inputModuleType != null)
                {
                    eventSystem.gameObject.AddComponent(s_inputModuleType);
                }
            }

            foreach (var cam in FindObjectsByType<Camera>())
            {
                if (cam != null && cam.GetComponent<PhysicsRaycaster>() == null)
                {
                    cam.gameObject.AddComponent<PhysicsRaycaster>();
                }
            }
        }

        private static void EnsureInputModuleType()
        {
            if (s_inputModuleType != null) return;
            s_inputModuleType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem.ForUI");
            if (s_inputModuleType == null)
            {
                s_inputModuleType = System.Type.GetType(
                    "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            }
        }

        public void BuildBoard(List<PoleState> poles)
        {
            EnsureInputSetup();
            ClearBoard();

#if UNITY_EDITOR
            if (_torusPrefab == null)
            {
                _torusPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Torus.obj");
            }
#endif

            for (int p = 0; p < poles.Count; p++)
            {
                var poleData = poles[p];

                var poleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                poleObj.name = $"Pole_{p}" + (poleData.IsLocked ? " [LOCKED]" : "");

                poleObj.transform.SetParent(transform);
                poleObj.transform.position = new Vector3(p * _poleSpacing, 2.0f, 0f);
                poleObj.transform.localScale = new Vector3(0.2f, 2.0f, 0.2f);

                var poleView = poleObj.AddComponent<PoleView>();
                poleView.PoleId = p;
                _spawnedPoles.Add(poleView);

                var capsule = poleObj.GetComponent<CapsuleCollider>();
                if (capsule != null)
                {
                    capsule.radius = 1.5f;
                }

                if (poleData.IsLocked)
                {
                    var renderer = poleObj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.sharedMaterial = new Material(GetDefaultShader());
                        renderer.sharedMaterial.color = Color.black;
                    }
                }

                var ringList = new List<GameObject>();
                for (int r = 0; r < poleData.Rings.Count; r++)
                {
                    var ringData = poleData.Rings[r];
                    GameObject ringObj;

                    if (_torusPrefab != null)
                    {
                        ringObj = Instantiate(_torusPrefab);
                        ringObj.name = $"Ring_{r}_{ringData.Color}_{ringData.Type}";
                        ringObj.transform.SetParent(poleObj.transform);
                        ringObj.transform.localPosition = new Vector3(0f, -0.9f + (r * 0.4f), 0f);
                        ringObj.transform.localRotation = Quaternion.identity;
                        ringObj.transform.localScale = new Vector3(3.5f, 0.2f, 3.5f);
                    }
                    else
                    {
                        ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        ringObj.name = $"Ring_{r}_{ringData.Color}_{ringData.Type}";
                        ringObj.transform.SetParent(poleObj.transform);
                        ringObj.transform.localPosition = new Vector3(0f, -0.9f + (r * 0.4f), 0f);
                        ringObj.transform.localScale = new Vector3(4.0f, 0.08f, 4.0f);
                    }

                    var ringRenderer = ringObj.GetComponentInChildren<Renderer>();
                    if (ringRenderer != null)
                    {
                        var mat = new Material(GetDefaultShader());
                        mat.color = RingPalette.Get(ringData.Color);
                        ApplySpecialRingMaterial(mat, ringData.Type);
                        ringRenderer.sharedMaterial = mat;
                    }

                    // Strip the ring's collider so clicks pass through to the PoleView underneath.
                    var col = ringObj.GetComponent<Collider>();
                    if (col != null)
                    {
                        Destroy(col);
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
                if (pole != null) Destroy(pole.gameObject);
            }
            _spawnedPoles.Clear();
            _spawnedRings.Clear();
        }

        private Shader GetDefaultShader()
        {
            var s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (s == null) s = Shader.Find("Standard");
            return s;
        }

        private void ApplySpecialRingMaterial(Material mat, RingType type)
        {
            switch (type)
            {
                case RingType.Frozen:
                    mat.color = Color.cyan;
                    break;
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

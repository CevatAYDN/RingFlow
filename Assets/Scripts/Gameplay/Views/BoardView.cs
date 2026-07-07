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

        private readonly List<PoleView> _spawnedPoles = new();
        private readonly List<List<GameObject>> _spawnedRings = new();

        public void BuildBoard(List<PoleState> poles)
        {
            ClearBoard();

            // Load Torus model from Assets at runtime if not assigned
#if UNITY_EDITOR
            if (_torusPrefab == null)
            {
                _torusPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Torus.obj");
            }
#endif

            for (int p = 0; p < poles.Count; p++)
            {
                var poleData = poles[p];

                // 1. Create Cylinder Pole GameObject
                var poleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                poleObj.name = $"Pole_{p}" + (poleData.IsLocked ? " [LOCKED]" : "");
                
                // Set parent first so GetComponentInParent finds Root in OnEnable
                poleObj.transform.SetParent(transform);
                poleObj.transform.position = new Vector3(p * _poleSpacing, 2.0f, 0f);
                poleObj.transform.localScale = new Vector3(0.2f, 2.0f, 0.2f);

                // Add PoleView component
                var poleView = poleObj.AddComponent<PoleView>();
                poleView.PoleId = p;
                _spawnedPoles.Add(poleView);

                // Add locked pole color decoration
                if (poleData.IsLocked)
                {
                    var renderer = poleObj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.sharedMaterial = new Material(GetDefaultShader());
                        renderer.sharedMaterial.color = Color.black;
                    }
                }

                // 2. Create Rings
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

                    // Apply Material color matching RingColor
                    var ringRenderer = ringObj.GetComponentInChildren<Renderer>();
                    if (ringRenderer != null)
                    {
                        var mat = new Material(GetDefaultShader());
                        mat.color = GetUnityColor(ringData.Color);

                        ApplySpecialRingMaterial(mat, ringData.Type);
                        ringRenderer.sharedMaterial = mat;
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

        private Color GetUnityColor(RingColor color)
        {
            return color switch
            {
                RingColor.Red => Color.red,
                RingColor.Blue => Color.blue,
                RingColor.Green => Color.green,
                RingColor.Yellow => Color.yellow,
                RingColor.Purple => new Color(0.5f, 0f, 0.5f),
                RingColor.Orange => new Color(1f, 0.5f, 0f),
                RingColor.Cyan => Color.cyan,
                _ => Color.white
            };
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

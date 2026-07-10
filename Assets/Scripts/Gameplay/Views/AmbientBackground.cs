using UnityEngine;

namespace RingFlow.Gameplay
{
    public class AmbientBackground : MonoBehaviour
    {
        private GameFeelConfigSO Feel => GameFeelConfigSO.Instance;

        [SerializeField] private Color _topColor = new(0.08f, 0.10f, 0.14f);
        [SerializeField] private Color _bottomColor = new(0.04f, 0.06f, 0.10f);

        private GameObject[] _particles;
        private float[] _twinklePhases;
        private float[] _twinkleSpeeds;
        private MeshRenderer[] _renderers;
        private MaterialPropertyBlock _propBlock;

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            BuildGradientQuad();
            BuildParticles();
        }

        private void BuildGradientQuad()
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "GradientBackground";
            quad.transform.SetParent(transform, false);
            quad.transform.localPosition = new Vector3(10f, 5f, 1f);
            quad.transform.localScale = new Vector3(30f, 15f, 1f);

            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateGradientMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private Material CreateGradientMaterial()
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Standard"));
            mat.color = _topColor;
            return mat;
        }

        private void BuildParticles()
        {
            int count = Feel?.ConfettiPoolSize ?? 30;
            float sizeMin = 0.02f;
            float sizeMax = 0.06f;
            float spawnW = 20f;
            float spawnH = 15f;
            float twinkleMin = 0.5f;
            float twinkleMax = 2.0f;
            Color pColor = new(0.15f, 0.25f, 0.40f, 0.3f);

            _particles = new GameObject[count];
            _twinklePhases = new float[count];
            _twinkleSpeeds = new float[count];
            _renderers = new MeshRenderer[count];

            for (int i = 0; i < count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"AmbientParticle_{i}";
                go.transform.SetParent(transform, false);

                float x = Random.Range(-spawnW * 0.5f, spawnW * 0.5f);
                float y = Random.Range(-spawnH * 0.5f, spawnH * 0.5f);
                go.transform.localPosition = new Vector3(x + 10f, y + 5f, 0.5f);

                float size = Random.Range(sizeMin, sizeMax);
                go.transform.localScale = Vector3.one * size;

                var col = go.GetComponent<SphereCollider>();
                if (col != null) Destroy(col);

                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = CreateParticleMaterial(pColor);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                _renderers[i] = renderer;

                _twinklePhases[i] = Random.Range(0f, Mathf.PI * 2f);
                _twinkleSpeeds[i] = Random.Range(twinkleMin, twinkleMax);
            }
        }

        private static Material CreateParticleMaterial(Color baseColor)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Standard"));
            mat.color = baseColor;
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
            return mat;
        }

        private void Update()
        {
            if (_particles == null) return;

            float driftMin = 0.05f;
            float driftMax = 0.2f;

            for (int i = 0; i < _particles.Length; i++)
            {
                var go = _particles[i];
                if (go == null) continue;

                float driftSpeed = Mathf.Lerp(driftMin, driftMax,
                    (Mathf.Sin(_twinklePhases[i] + Time.time * 0.3f) + 1f) * 0.5f);
                Vector3 pos = go.transform.localPosition;
                pos.y += driftSpeed * Time.deltaTime;
                if (pos.y > 12.5f) pos.y = -2.5f;
                go.transform.localPosition = pos;

                float alpha = (Mathf.Sin(Time.time * _twinkleSpeeds[i] + _twinklePhases[i]) + 0.5f) * 0.5f;
                _renderers[i].GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_Color", new Color(0.15f, 0.25f, 0.40f, alpha * 0.3f));
                _renderers[i].SetPropertyBlock(_propBlock);
            }
        }
    }
}

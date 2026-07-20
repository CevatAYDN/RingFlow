using UnityEngine;
using Nexus.Core;

namespace RingFlow.Gameplay
{
    public class AmbientBackground : View
    {
        [Inject] private GameFeelConfigSO _feelConfig;
        private GameFeelConfigSO Feel => _feelConfig;

        [SerializeField] private Color _topColor = new(0.96f, 0.98f, 1.0f);
        [SerializeField] private Color _bottomColor = new(0.88f, 0.92f, 0.97f);

        private GameObject[] _particles;
        private float[] _twinklePhases;
        private float[] _twinkleSpeeds;
        private MeshRenderer[] _renderers;
        private MaterialPropertyBlock _propBlock;

        private static Shader s_unlitShader;

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
            quad.transform.localPosition = new Vector3(0f, 0f, 35f);
            quad.transform.localScale = new Vector3(80f, 80f, 1f);

            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateGradientMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        private Texture2D CreateGradientTexture(Color top, Color bottom)
        {
            var tex = new Texture2D(1, 32);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < 32; y++)
            {
                float t = y / 31f;
                tex.SetPixel(0, y, Color.Lerp(bottom, top, t));
            }
            tex.Apply();
            return tex;
        }

        private Material CreateGradientMaterial()
        {
            var shader = GetUnlitShader();
            var mat = new Material(shader);
            var texture = CreateGradientTexture(_topColor, _bottomColor);
            
            if (shader.name.Contains("Universal Render Pipeline"))
            {
                mat.SetTexture("_BaseMap", texture);
                mat.SetColor("_BaseColor", Color.white);
            }
            else
            {
                mat.mainTexture = texture;
                mat.color = Color.white;
            }
            return mat;
        }

        private void BuildParticles()
        {
            int count = Feel?.ConfettiPoolSize ?? 30;
            float sizeMin = Feel?.AmbientParticleSizeMin ?? 0.08f;
            float sizeMax = Feel?.AmbientParticleSizeMax ?? 0.22f;
            float spawnW = Feel?.AmbientSpawnWidth ?? 36f;
            float spawnH = Feel?.AmbientSpawnHeight ?? 24f;
            float twinkleMin = Feel?.AmbientTwinkleMin ?? 0.5f;
            float twinkleMax = Feel?.AmbientTwinkleMax ?? 2.0f;
            Color pColor = Feel?.AmbientParticleColor ?? new(1.0f, 1.0f, 1.0f, 0.15f);

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
                float z = Random.Range(30f, 34f);
                go.transform.localPosition = new Vector3(x, y, z);

                float size = Random.Range(sizeMin, sizeMax);
                go.transform.localScale = Vector3.one * size;

                var col = go.GetComponent<SphereCollider>();
                if (col != null) Destroy(col);

                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = CreateParticleMaterial(pColor);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                
                _particles[i] = go;
                _renderers[i] = mr;
                _twinklePhases[i] = Random.Range(0f, Mathf.PI * 2f);
                _twinkleSpeeds[i] = Random.Range(twinkleMin, twinkleMax);
            }
        }

        private static Material CreateParticleMaterial(Color baseColor)
        {
            var mat = new Material(GetUnlitShader());
            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", baseColor);
            
            // Set rendering mode to transparent/additive
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
            return mat;
        }

        private static Shader GetUnlitShader()
        {
            if (s_unlitShader == null)
            {
                s_unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
                             ?? Shader.Find("Unlit/Color")
                             ?? Shader.Find("Unlit/Texture")
                             ?? Shader.Find("Standard");
            }
            return s_unlitShader;
        }

        private void Update()
        {
            if (_particles == null) return;

            float driftMin = Feel?.AmbientDriftMin ?? 0.05f;
            float driftMax = Feel?.AmbientDriftMax ?? 0.25f;
            Color pColor = Feel?.AmbientParticleColor ?? new(1.0f, 1.0f, 1.0f, 0.15f);

            for (int i = 0; i < _particles.Length; i++)
            {
                var go = _particles[i];
                if (go == null) continue;

                float driftSpeed = Mathf.Lerp(driftMin, driftMax,
                    (Mathf.Sin(_twinklePhases[i] + Time.time * 0.3f) + 1f) * 0.5f);
                Vector3 pos = go.transform.localPosition;
                pos.y += driftSpeed * Time.deltaTime;
                if (pos.y > 12f) pos.y = -12f;
                go.transform.localPosition = pos;

                float alpha = (Mathf.Sin(Time.time * _twinkleSpeeds[i] + _twinklePhases[i]) + 0.5f) * 0.5f;
                _renderers[i].GetPropertyBlock(_propBlock);
                var c = new Color(pColor.r, pColor.g, pColor.b, alpha * pColor.a);
                _propBlock.SetColor("_Color", c);
                _propBlock.SetColor("_BaseColor", c);
                _renderers[i].SetPropertyBlock(_propBlock);
            }
        }
    }
}

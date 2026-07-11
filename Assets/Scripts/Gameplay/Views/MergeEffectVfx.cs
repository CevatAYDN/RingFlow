using DG.Tweening;
using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Ring merge/absorption effect for pole completion.
    /// Each ring on the pole shrinks into a central point with a glowing trail,
    /// then bursts outward as ring-shaped particles.
    /// Zero runtime allocation — all child meshes pre-created in Awake.
    /// </summary>
    public class MergeEffectVfx : MonoBehaviour, IPoolable
    {
        private static Shader s_litShader;
        private static Material s_sharedMaterial;
        private static Material s_glowMaterial;

        private GameObject[] _mergeParticles;
        private Renderer[] _mergeRenderers;
        private MaterialPropertyBlock _mpb;

        private GameObject _glowOrb;
        private Renderer _glowRenderer;
        private MaterialPropertyBlock _glowMpb;

        private int _particleCount;
        private Sequence _mergeSequence;

        [Inject] private IObjectPoolService _objectPoolService;

        private const float OrphanLifetime = 2f;

        private void Awake()
        {
            EnsureSharedResources();
            _mpb = new MaterialPropertyBlock();
            _glowMpb = new MaterialPropertyBlock();

            var config = GameFeelConfigSO.Instance;
            int burstCount = config != null ? config.MergeBurstCount : 16;
            _particleCount = burstCount;

            _mergeParticles = new GameObject[_particleCount];
            _mergeRenderers = new Renderer[_particleCount];

            // Pre-create burst particles (ring segments)
            for (int i = 0; i < _particleCount; i++)
            {
                var p = new GameObject("MergeBurst_" + i);
                p.transform.SetParent(transform, false);
                p.transform.localPosition = Vector3.zero;
                p.transform.localScale = Vector3.zero;
                p.SetActive(false);

                var mf = p.AddComponent<MeshFilter>();
                mf.sharedMesh = VfxMeshCache.RingSegmentMesh;

                var mr = p.AddComponent<MeshRenderer>();
                mr.sharedMaterial = s_sharedMaterial;
                mr.enabled = false;

                _mergeParticles[i] = p;
                _mergeRenderers[i] = mr;
            }

            // Pre-create glow orb
            _glowOrb = new GameObject("MergeGlowOrb");
            _glowOrb.transform.SetParent(transform, false);
            _glowOrb.transform.localPosition = Vector3.zero;
            _glowOrb.transform.localScale = Vector3.zero;
            _glowOrb.SetActive(false);

            var orbMf = _glowOrb.AddComponent<MeshFilter>();
            orbMf.sharedMesh = VfxMeshCache.SparkMesh;

            _glowRenderer = _glowOrb.AddComponent<MeshRenderer>();
            _glowRenderer.sharedMaterial = s_glowMaterial;
            _glowRenderer.enabled = false;
        }

        private static void EnsureSharedResources()
        {
            if (s_litShader == null)
            {
                s_litShader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                            ?? Shader.Find("Standard");
            }

            if (s_sharedMaterial == null && s_litShader != null)
            {
                s_sharedMaterial = new Material(s_litShader)
                {
                    enableInstancing = true
                };
                s_sharedMaterial.SetFloat("_Metallic", 0.3f);
                s_sharedMaterial.SetFloat("_Smoothness", 0.9f);
            }

            // Glow material — emissive unlit-like appearance via high smoothness + emission
            if (s_glowMaterial == null && s_litShader != null)
            {
                s_glowMaterial = new Material(s_litShader)
                {
                    enableInstancing = true
                };
                s_glowMaterial.SetFloat("_Metallic", 0f);
                s_glowMaterial.SetFloat("_Smoothness", 0.2f);
                s_glowMaterial.EnableKeyword("_EMISSION");
                s_glowMaterial.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.3f) * 2f);
            }
        }

        /// <summary>
        /// Play the merge effect at the given position.
        /// </summary>
        /// <param name="position">World position of the pole top.</param>
        /// <param name="ringColor">Primary ring color for the burst.</param>
        /// <param name="ringCount">Number of rings on the pole (scales effect intensity).</param>
        /// <param name="isFinalPole">Whether this is the last pole (full intensity).</param>
        public void Initialize(Vector3 position, Color ringColor, int ringCount, bool isFinalPole)
        {
            _mergeSequence?.Kill();
            _mergeSequence = DOTween.Sequence();

            transform.position = position;

            float intensityScale = Mathf.Lerp(0.6f, 1.2f, (ringCount - 1) / 3f);
            if (isFinalPole) intensityScale *= 1.5f;

            _mpb.SetColor("_BaseColor", ringColor);
            _glowMpb.SetColor("_BaseColor", ringColor);

            // Phase 1: Glow orb appears and expands (0s - 0.25s)
            _glowOrb.SetActive(true);
            _glowRenderer.enabled = true;
            _glowRenderer.SetPropertyBlock(_glowMpb);
            _glowOrb.transform.localScale = Vector3.zero;
            _glowOrb.transform.localRotation = Quaternion.identity;

            _mergeSequence.Append(_glowOrb.transform
                .DOScale(Vector3.one * 0.5f * intensityScale, 0.2f)
                .SetEase(Ease.OutBack));
            _mergeSequence.Join(_glowOrb.transform
                .DORotate(new Vector3(0f, 180f, 0f), 0.25f, RotateMode.FastBeyond360)
                .SetEase(Ease.OutQuad));

            // Phase 2: Orb shrinks (absorption "pop") (0.25s - 0.45s)
            _mergeSequence.Append(_glowOrb.transform
                .DOScale(Vector3.zero, 0.2f)
                .SetEase(Ease.InBack));
            _mergeSequence.Join(_glowOrb.transform
                .DORotate(new Vector3(0f, 360f, 0f), 0.2f, RotateMode.FastBeyond360)
                .SetEase(Ease.InQuad));

            // Phase 3: Burst particles (0.45s - 0.95s)
            float burstDuration = isFinalPole ? 0.6f : 0.4f;
            float burstScale = isFinalPole ? 1.5f : 1.0f;

            for (int i = 0; i < _particleCount; i++)
            {
                var p = _mergeParticles[i];
                var r = _mergeRenderers[i];
                if (p == null || r == null) continue;

                p.SetActive(true);
                r.enabled = true;
                r.SetPropertyBlock(_mpb);
                p.transform.localPosition = Vector3.zero;
                p.transform.localScale = Vector3.one * Random.Range(0.15f, 0.3f) * intensityScale * burstScale;
                p.transform.localRotation = Quaternion.identity;

                // Ring-shaped burst: distribute in a disk (XZ plane) with slight vertical variation
                float angle = (float)i / _particleCount * Mathf.PI * 2f;
                float radius = Random.Range(1.8f, 3.5f) * intensityScale * burstScale;
                Vector3 burstDir = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Random.Range(-0.3f, 0.8f) * intensityScale,
                    Mathf.Sin(angle) * radius
                );

                _mergeSequence.Join(p.transform
                    .DOLocalMove(burstDir, burstDuration)
                    .SetEase(Ease.OutQuad));
                _mergeSequence.Join(p.transform
                    .DOScale(Vector3.zero, burstDuration)
                    .SetEase(Ease.InQuad));
                _mergeSequence.Join(p.transform
                    .DOLocalRotate(
                        new Vector3(Random.Range(-180f, 180f), Random.Range(-180f, 180f), Random.Range(-180f, 180f)),
                        burstDuration,
                        RotateMode.FastBeyond360
                    ).SetEase(Ease.OutQuad));
            }

            _mergeSequence.OnComplete(() =>
            {
                HideAll();
                DespawnSelf();
            });
        }

        /// <summary>
        /// Simplified burst-only initialization (for ring placement, not merge).
        /// </summary>
        public void InitializeBurstOnly(Vector3 position, Color color)
        {
            _mergeSequence?.Kill();
            _mergeSequence = DOTween.Sequence();

            transform.position = position;

            _mpb.SetColor("_BaseColor", color);

            float burstDuration = 0.35f;

            for (int i = 0; i < _particleCount; i++)
            {
                var p = _mergeParticles[i];
                var r = _mergeRenderers[i];
                if (p == null || r == null) continue;

                p.SetActive(true);
                r.enabled = true;
                r.SetPropertyBlock(_mpb);
                p.transform.localPosition = Vector3.zero;
                p.transform.localScale = Vector3.one * Random.Range(0.1f, 0.2f);
                p.transform.localRotation = Quaternion.identity;

                float angle = (float)i / _particleCount * Mathf.PI * 2f;
                float radius = Random.Range(0.8f, 1.8f);
                Vector3 burstDir = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Random.Range(0.2f, 0.6f),
                    Mathf.Sin(angle) * radius
                );

                _mergeSequence.Join(p.transform.DOLocalMove(burstDir, burstDuration).SetEase(Ease.OutQuad));
                _mergeSequence.Join(p.transform.DOScale(Vector3.zero, burstDuration).SetEase(Ease.InQuad));
            }

            _mergeSequence.OnComplete(() =>
            {
                HideAll();
                DespawnSelf();
            });
        }

        private void HideAll()
        {
            for (int i = 0; i < _particleCount; i++)
            {
                if (_mergeParticles[i] != null)
                {
                    _mergeParticles[i].SetActive(false);
                    _mergeParticles[i].transform.localScale = Vector3.zero;
                }
                if (_mergeRenderers[i] != null)
                    _mergeRenderers[i].enabled = false;
            }
            if (_glowOrb != null)
            {
                _glowOrb.SetActive(false);
                _glowOrb.transform.localScale = Vector3.zero;
            }
            if (_glowRenderer != null)
                _glowRenderer.enabled = false;
        }

        private void DespawnSelf()
        {
            if (_objectPoolService != null)
                _objectPoolService.Despawn(gameObject);
            else
                Destroy(gameObject);
        }

        public void OnSpawned() { }

        public void OnDespawned()
        {
            _mergeSequence?.Kill();
            _mergeSequence = null;
            HideAll();
        }

        private void OnDestroy()
        {
            _mergeSequence?.Kill();
            _mergeSequence = null;
        }
    }
}

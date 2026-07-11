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
    /// Per-piece tweens are local (not joined to a Sequence) so they auto-dispose
    /// on completion, preventing the DOTween tween pool from growing unboundedly.
    /// </summary>
    public class MergeEffectVfx : MonoBehaviour, IPoolable
    {
        private static Shader s_litShader;
        private static Material s_sharedMaterial;
        private static Material s_glowMaterial;

        private struct ParticlePiece
        {
            public Transform Transform;
            public Renderer Renderer;
        }

        private ParticlePiece[] _particles;
        private Transform _glowOrb;
        private Renderer _glowRenderer;
        private MaterialPropertyBlock _mpb;
        private MaterialPropertyBlock _glowMpb;

        private int _particleCount;
        private int _activeCount;

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
            _particles = new ParticlePiece[_particleCount];

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

                _particles[i].Transform = p.transform;
                _particles[i].Renderer = mr;
            }

            _glowOrb = new GameObject("MergeGlowOrb").transform;
            _glowOrb.SetParent(transform, false);
            _glowOrb.localPosition = Vector3.zero;
            _glowOrb.localScale = Vector3.zero;
            _glowOrb.gameObject.SetActive(false);

            var orbMf = _glowOrb.gameObject.AddComponent<MeshFilter>();
            orbMf.sharedMesh = VfxMeshCache.SparkMesh;

            _glowRenderer = _glowOrb.gameObject.AddComponent<MeshRenderer>();
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
        public void Initialize(Vector3 position, Color ringColor, int ringCount, bool isFinalPole)
        {
            KillAllLocalTweens();
            transform.position = position;

            float intensityScale = Mathf.Lerp(0.6f, 1.2f, (ringCount - 1) / 3f);
            if (isFinalPole) intensityScale *= 1.5f;

            _mpb.SetColor("_BaseColor", ringColor);
            _glowMpb.SetColor("_BaseColor", ringColor);

            // Phase 1: glow orb appear
            _glowOrb.gameObject.SetActive(true);
            _glowRenderer.enabled = true;
            _glowRenderer.SetPropertyBlock(_glowMpb);
            _glowOrb.localScale = Vector3.zero;
            _glowOrb.localRotation = Quaternion.identity;

            float glowExpand = 0.2f;
            float glowShrink = 0.2f;
            float burstDuration = isFinalPole ? 0.6f : 0.4f;
            float burstScale = isFinalPole ? 1.5f : 1.0f;
            float finalDespawnAt = glowExpand + glowShrink + burstDuration;

            _glowOrb.DOScale(Vector3.one * 0.5f * intensityScale, glowExpand)
                .SetEase(Ease.OutBack)
                .SetAutoKill(true);

            _glowOrb.DORotate(new Vector3(0f, 180f, 0f), glowExpand, RotateMode.FastBeyond360)
                .SetEase(Ease.OutQuad)
                .SetAutoKill(true);

            _glowOrb.DOScale(Vector3.zero, glowShrink)
                .SetDelay(glowExpand)
                .SetEase(Ease.InBack)
                .SetAutoKill(true);

            _glowOrb.DORotate(new Vector3(0f, 360f, 0f), glowShrink, RotateMode.FastBeyond360)
                .SetDelay(glowExpand)
                .SetEase(Ease.InQuad)
                .SetAutoKill(true);

            // Phase 2: burst pieces
            int activeParticles = 0;
            for (int i = 0; i < _particleCount; i++)
            {
                ref var piece = ref _particles[i];
                var t = piece.Transform;
                var r = piece.Renderer;
                if (t == null || r == null) continue;

                t.gameObject.SetActive(true);
                r.enabled = true;
                r.SetPropertyBlock(_mpb);
                t.localPosition = Vector3.zero;
                t.localScale = Vector3.one * Random.Range(0.15f, 0.3f) * intensityScale * burstScale;
                t.localRotation = Quaternion.identity;

                float angle = (float)i / _particleCount * Mathf.PI * 2f;
                float radius = Random.Range(1.8f, 3.5f) * intensityScale * burstScale;
                Vector3 burstDir = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Random.Range(-0.3f, 0.8f) * intensityScale,
                    Mathf.Sin(angle) * radius
                );

                float startDelay = glowExpand + glowShrink;
                t.DOLocalMove(burstDir, burstDuration)
                    .SetDelay(startDelay)
                    .SetEase(Ease.OutQuad)
                    .SetAutoKill(true);

                t.DOScale(Vector3.zero, burstDuration)
                    .SetDelay(startDelay)
                    .SetEase(Ease.InQuad)
                    .SetAutoKill(true);

                t.DOLocalRotate(
                    new Vector3(Random.Range(-180f, 180f), Random.Range(-180f, 180f), Random.Range(-180f, 180f)),
                    burstDuration,
                    RotateMode.FastBeyond360
                )
                    .SetDelay(startDelay)
                    .SetEase(Ease.OutQuad)
                    .SetAutoKill(true);

                activeParticles++;
            }
            _activeCount = activeParticles;

            Invoke(nameof(DespawnSelf), finalDespawnAt);
        }

        /// <summary>
        /// Simplified burst-only initialization (for ring placement, not merge).
        /// </summary>
        public void InitializeBurstOnly(Vector3 position, Color color)
        {
            KillAllLocalTweens();
            transform.position = position;

            _mpb.SetColor("_BaseColor", color);

            float burstDuration = 0.35f;

            for (int i = 0; i < _particleCount; i++)
            {
                ref var piece = ref _particles[i];
                var t = piece.Transform;
                var r = piece.Renderer;
                if (t == null || r == null) continue;

                t.gameObject.SetActive(true);
                r.enabled = true;
                r.SetPropertyBlock(_mpb);
                t.localPosition = Vector3.zero;
                t.localScale = Vector3.one * Random.Range(0.1f, 0.2f);
                t.localRotation = Quaternion.identity;

                float angle = (float)i / _particleCount * Mathf.PI * 2f;
                float radius = Random.Range(0.8f, 1.8f);
                Vector3 burstDir = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Random.Range(0.2f, 0.6f),
                    Mathf.Sin(angle) * radius
                );

                t.DOLocalMove(burstDir, burstDuration)
                    .SetEase(Ease.OutQuad)
                    .SetAutoKill(true);
                t.DOScale(Vector3.zero, burstDuration)
                    .SetEase(Ease.InQuad)
                    .SetAutoKill(true);
            }
            _activeCount = _particleCount;

            Invoke(nameof(DespawnSelf), burstDuration);
        }

        private void KillAllLocalTweens()
        {
            CancelInvoke(nameof(DespawnSelf));
            if (_glowOrb != null) DOTween.Kill(_glowOrb);
            for (int i = 0; i < _particleCount; i++)
            {
                var t = _particles[i].Transform;
                if (t != null) DOTween.Kill(t);
            }
        }

        private void HideAll()
        {
            for (int i = 0; i < _particleCount; i++)
            {
                var t = _particles[i].Transform;
                var r = _particles[i].Renderer;
                if (t != null)
                {
                    t.gameObject.SetActive(false);
                    t.localScale = Vector3.zero;
                }
                if (r != null) r.enabled = false;
            }
            if (_glowOrb != null)
            {
                _glowOrb.gameObject.SetActive(false);
                _glowOrb.localScale = Vector3.zero;
            }
            if (_glowRenderer != null) _glowRenderer.enabled = false;
        }

        private void DespawnSelf()
        {
            if (this == null) return;
            CancelInvoke(nameof(DespawnSelf));
            HideAll();
            if (_objectPoolService != null)
                _objectPoolService.Despawn(gameObject);
            else
                Destroy(gameObject);
        }

        public void OnSpawned() { }

        public void OnDespawned()
        {
            KillAllLocalTweens();
            HideAll();
        }

        private void OnDestroy()
        {
            KillAllLocalTweens();
        }
    }
}

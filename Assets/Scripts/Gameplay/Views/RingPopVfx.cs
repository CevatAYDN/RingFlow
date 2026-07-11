using DG.Tweening;
using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Ring pop burst effect for pole completion and ring placement.
    /// Zero runtime allocation — all child meshes are pre-created in Awake.
    /// Uses shared static material with MaterialPropertyBlock for per-instance color.
    /// GPU Instancing compatible via shared mesh and property block.
    /// </summary>
    public class RingPopVfx : MonoBehaviour, IPoolable
    {
        private static Shader s_litShader;
        private static Material s_sharedMaterial;

        private GameObject[] _particles;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;
        private Sequence _animationSequence;
        private int _particleCount;

        [Inject] private IObjectPoolService _objectPoolService;

        private void Awake()
        {
            EnsureSharedResources();
            _mpb = new MaterialPropertyBlock();

            // Pre-create all child particles — zero allocation on Initialize
            var config = GameFeelConfigSO.Instance;
            _particleCount = config != null ? config.RingPopCount : 12;
            _particles = new GameObject[_particleCount];
            _renderers = new Renderer[_particleCount];

            for (int i = 0; i < _particleCount; i++)
            {
                var p = new GameObject("Pop_" + i);
                p.transform.SetParent(transform, false);
                p.transform.localPosition = Vector3.zero;
                p.transform.localScale = Vector3.zero; // Hidden until Initialize
                p.SetActive(false);

                var mf = p.AddComponent<MeshFilter>();
                mf.sharedMesh = VfxMeshCache.DonutMesh;

                var mr = p.AddComponent<MeshRenderer>();
                mr.sharedMaterial = s_sharedMaterial;
                mr.SetPropertyBlock(_mpb);
                mr.enabled = false;

                // Remove any collider — cubes don't have one but explicit is safe
                _particles[i] = p;
                _renderers[i] = mr;
            }
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
                s_sharedMaterial.SetFloat("_Metallic", 0.5f);
                s_sharedMaterial.SetFloat("_Smoothness", 0.8f);
            }
        }

        public void Initialize(Color color)
        {
            // Kill any running animation
            _animationSequence?.Kill();

            var config = GameFeelConfigSO.Instance;
            float duration = config != null ? config.RingPopDuration : 0.5f;

            _mpb.SetColor("_BaseColor", color);
            _animationSequence = DOTween.Sequence();

            for (int i = 0; i < _particleCount; i++)
            {
                var p = _particles[i];
                var r = _renderers[i];
                if (p == null || r == null) continue;

                // Reset and position
                p.SetActive(true);
                r.enabled = true;
                r.SetPropertyBlock(_mpb);
                p.transform.localPosition = Vector3.zero;
                p.transform.localScale = Vector3.one * Random.Range(0.12f, 0.22f);
                p.transform.localRotation = Quaternion.identity;

                // Random burst direction
                Vector3 direction = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(0.5f, 2f),
                    Random.Range(-1f, 1f)
                ).normalized * Random.Range(1.5f, 3f);

                // Animate
                _animationSequence.Join(p.transform.DOLocalMove(direction, duration).SetEase(Ease.OutQuad));
                _animationSequence.Join(p.transform.DOScale(Vector3.zero, duration).SetEase(Ease.InQuad));
                _animationSequence.Join(p.transform.DORotate(
                    new Vector3(Random.Range(-180f, 180f), Random.Range(-180f, 180f), Random.Range(-180f, 180f)),
                    duration,
                    RotateMode.FastBeyond360
                ).SetEase(Ease.OutQuad));
            }

            _animationSequence.OnComplete(() =>
            {
                HideAll();
                DespawnSelf();
            });
        }

        private void HideAll()
        {
            for (int i = 0; i < _particleCount; i++)
            {
                if (_particles[i] != null)
                {
                    _particles[i].SetActive(false);
                    _particles[i].transform.localScale = Vector3.zero;
                }
                if (_renderers[i] != null)
                    _renderers[i].enabled = false;
            }
        }

        private void DespawnSelf()
        {
            if (_objectPoolService != null)
                _objectPoolService.Despawn(gameObject);
            else
                Destroy(gameObject);
        }

        public void OnSpawned()
        {
        }

        public void OnDespawned()
        {
            _animationSequence?.Kill();
            _animationSequence = null;
            HideAll();
        }

        private void OnDestroy()
        {
            _animationSequence?.Kill();
            _animationSequence = null;
        }
    }
}

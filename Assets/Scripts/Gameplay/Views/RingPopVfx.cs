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
    /// Per-piece tweens are local (not joined to a Sequence) so they auto-dispose
    /// on completion, preventing the DOTween tween pool from growing unboundedly.
    /// </summary>
    public class RingPopVfx : MonoBehaviour, IPoolable
    {
        private static Shader s_litShader;
        private static Material s_sharedMaterial;

        private struct PopPiece
        {
            public Transform Transform;
            public Renderer Renderer;
        }

        private PopPiece[] _pieces;
        private MaterialPropertyBlock _mpb;
        private int _pieceCount;

        [Inject] private IObjectPoolService _objectPoolService;
        [Inject] private GameFeelConfigSO _feelConfig;

        private void Awake()
        {
            EnsureSharedResources();
            _mpb = new MaterialPropertyBlock();
            if (_feelConfig == null && NexusRuntime.CurrentContext != null)
                _feelConfig = NexusRuntime.CurrentContext.Resolve<GameFeelConfigSO>();
        }

        private void EnsurePiecesCreated(GameFeelConfigSO config)
        {
            if (_pieces != null) return;
            _pieceCount = config != null ? config.RingPopCount : 12;
            _pieces = new PopPiece[_pieceCount];

            for (int i = 0; i < _pieceCount; i++)
            {
                var p = new GameObject("Pop_" + i);
                p.transform.SetParent(transform, false);
                p.transform.localPosition = Vector3.zero;
                p.transform.localScale = Vector3.zero;
                p.SetActive(false);

                var mf = p.AddComponent<MeshFilter>();
                mf.sharedMesh = VfxMeshCache.DonutMesh;

                var mr = p.AddComponent<MeshRenderer>();
                mr.sharedMaterial = s_sharedMaterial;
                mr.SetPropertyBlock(_mpb);
                mr.enabled = false;

                _pieces[i].Transform = p.transform;
                _pieces[i].Renderer = mr;
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
            KillAllLocalTweens();

            if (_feelConfig == null) throw new System.InvalidOperationException("[RingPopVfx] GameFeelConfigSO not injected!");
            EnsurePiecesCreated(_feelConfig);

            var config = _feelConfig;
            float duration = config != null ? config.RingPopDuration : 0.5f;

            _mpb.SetColor("_BaseColor", color);

            for (int i = 0; i < _pieceCount; i++)
            {
                ref var piece = ref _pieces[i];
                var t = piece.Transform;
                var r = piece.Renderer;
                if (t == null || r == null) continue;

                t.gameObject.SetActive(true);
                r.enabled = true;
                r.SetPropertyBlock(_mpb);
                t.localPosition = Vector3.zero;
                t.localScale = Vector3.one * Random.Range(0.12f, 0.22f);
                t.localRotation = Quaternion.identity;

                Vector3 direction = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(0.5f, 2f),
                    Random.Range(-1f, 1f)
                ).normalized * Random.Range(1.5f, 3f);

                t.DOLocalMove(direction, duration)
                    .SetEase(DG.Tweening.Ease.OutQuad)
                    .SetAutoKill(true);
                t.DOScale(Vector3.zero, duration)
                    .SetEase(DG.Tweening.Ease.InQuad)
                    .SetAutoKill(true);
                t.DORotate(
                    new Vector3(Random.Range(-180f, 180f), Random.Range(-180f, 180f), Random.Range(-180f, 180f)),
                    duration,
                    RotateMode.FastBeyond360
                )
                    .SetEase(DG.Tweening.Ease.OutQuad)
                    .SetAutoKill(true)
                    .OnComplete(DespawnIfLast);
            }
        }

        private int _completionCount;

        private void DespawnIfLast()
        {
            _completionCount++;
            if (_completionCount < _pieceCount) return;
            _completionCount = 0;
            DespawnSelf();
        }

        private void KillAllLocalTweens()
        {
            for (int i = 0; i < _pieceCount; i++)
            {
                var t = _pieces[i].Transform;
                if (t != null) DOTween.Kill(t);
            }
        }

        private void HideAll()
        {
            for (int i = 0; i < _pieceCount; i++)
            {
                var t = _pieces[i].Transform;
                var r = _pieces[i].Renderer;
                if (t != null)
                {
                    t.gameObject.SetActive(false);
                    t.localScale = Vector3.zero;
                }
                if (r != null) r.enabled = false;
            }
        }

        private void DespawnSelf()
        {
            if (this == null) return;
            HideAll();
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
            _completionCount = 0;
            KillAllLocalTweens();
            HideAll();
        }

        private void OnDestroy()
        {
            KillAllLocalTweens();
        }
    }
}

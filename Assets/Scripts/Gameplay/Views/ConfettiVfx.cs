using DG.Tweening;
using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Confetti celebration effect for level win.
    /// Zero runtime allocation — all child meshes pre-created in Awake.
    /// Uses shared static materials with GPU Instancing support.
    /// Per-piece tweens are local (not joined to a Sequence) so they auto-dispose
    /// on completion, preventing the DOTween tween pool from growing unboundedly.
    /// </summary>
    public class ConfettiVfx : MonoBehaviour, IPoolable
    {
        private static Shader s_unlitShader;
        private static Material s_sharedMaterial;
        private static readonly Color[] s_Colors = {
            Color.red, Color.blue, Color.green, Color.yellow,
            Color.cyan, Color.magenta, new Color(1f, 0.5f, 0f), new Color(0.5f, 0f, 1f)
        };

        private struct Piece
        {
            public Transform Transform;
            public Renderer Renderer;
            public Vector3 StartPos;
            public float StartX;
            public float FallDuration;
            public float SwayFreq;
            public float SwayAmp;
            public Vector3 EndPos;
        }

        private Piece[] _pieces;
        private int _count;

        [Inject] private IObjectPoolService _objectPoolService;
        [Inject] private GameFeelConfigSO _feelConfig;

        private void Awake()
        {
            if (s_unlitShader == null)
            {
                s_unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
                              ?? Shader.Find("Unlit/Color")
                              ?? Shader.Find("Standard");
            }

            if (s_sharedMaterial == null && s_unlitShader != null)
            {
                s_sharedMaterial = new Material(s_unlitShader)
                {
                    enableInstancing = true
                };
            }
        }

        private void EnsurePiecesCreated(GameFeelConfigSO config)
        {
            if (_pieces != null) return;
            _count = config != null ? config.ConfettiCount : 40;
            _pieces = new Piece[_count];

            for (int i = 0; i < _count; i++)
            {
                var c = new GameObject("Confetti_" + i);
                c.transform.SetParent(transform, false);
                c.transform.localPosition = Vector3.zero;
                c.transform.localScale = Vector3.zero;
                c.SetActive(false);

                var mf = c.AddComponent<MeshFilter>();
                mf.sharedMesh = VfxMeshCache.QuadMesh;

                var mr = c.AddComponent<MeshRenderer>();
                mr.sharedMaterial = s_sharedMaterial;
                mr.enabled = false;

                var mpb = new MaterialPropertyBlock();
                int colorIdx = i % s_Colors.Length;
                mpb.SetColor("_BaseColor", s_Colors[colorIdx]);
                mr.SetPropertyBlock(mpb);

                _pieces[i].Transform = c.transform;
                _pieces[i].Renderer = mr;
            }
        }

        public void Initialize()
        {
            KillAllPieces();

            if (_feelConfig == null) throw new System.InvalidOperationException("[ConfettiVfx] GameFeelConfigSO not injected!");
            EnsurePiecesCreated(_feelConfig);

            var config = _feelConfig;
            Vector2 fallDurRange = config != null ? config.ConfettiFallDuration : new Vector2(1.8f, 3.0f);

            for (int i = 0; i < _count; i++)
            {
                ref Piece piece = ref _pieces[i];
                var t = piece.Transform;
                var r = piece.Renderer;
                if (t == null || r == null) continue;

                t.gameObject.SetActive(true);
                r.enabled = true;

                piece.StartPos = new Vector3(
                    Random.Range(-5f, 15f),
                    Random.Range(6f, 9f),
                    Random.Range(-2f, 2f)
                );
                t.localPosition = piece.StartPos;
                t.localScale = new Vector3(
                    Random.Range(0.15f, 0.25f),
                    Random.Range(0.15f, 0.25f),
                    1f
                );
                t.localRotation = Quaternion.identity;

                piece.StartX = piece.StartPos.x;
                piece.FallDuration = Random.Range(fallDurRange.x, fallDurRange.y);
                piece.SwayFreq = Random.Range(3f, 6f);
                piece.SwayAmp = Random.Range(0.5f, 1.2f);
                piece.EndPos = new Vector3(piece.StartX, piece.StartPos.y - 12f, piece.StartPos.z);

                t.DOLocalMoveY(piece.EndPos.y, piece.FallDuration)
                    .SetEase(Ease.OutQuad)
                    .SetAutoKill(true);

                t.DOLocalRotate(
                    new Vector3(Random.Range(360f, 720f), Random.Range(360f, 720f), Random.Range(360f, 720f)),
                    piece.FallDuration,
                    RotateMode.FastBeyond360
                ).SetEase(Ease.Linear).SetAutoKill(true);

                t.DOScale(Vector3.zero, 0.5f)
                    .SetDelay(piece.FallDuration - 0.5f)
                    .SetEase(Ease.InQuad)
                    .SetAutoKill(true)
                    .OnComplete(() => DespawnIfAllDone());
            }
        }

        private void DespawnIfAllDone()
        {
            if (this == null) return;
            for (int i = 0; i < _count; i++)
            {
                var t = _pieces[i].Transform;
                if (t == null) continue;
                if (t.gameObject.activeSelf) return;
            }
            DespawnSelf();
        }

        private void KillAllPieces()
        {
            for (int i = 0; i < _count; i++)
            {
                var t = _pieces[i].Transform;
                if (t == null) continue;
                DOTween.Kill(t);
            }
        }

        private void HideAll()
        {
            for (int i = 0; i < _count; i++)
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
            KillAllPieces();
            HideAll();
        }

        private void OnDestroy()
        {
            KillAllPieces();
        }
    }
}

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
    /// Auto-despawns via DOTween callback, no coroutine allocation.
    /// </summary>
    public class ConfettiVfx : MonoBehaviour, IPoolable
    {
        private static Shader s_unlitShader;
        private static Material s_sharedMaterial;
        private static readonly Color[] s_Colors = {
            Color.red, Color.blue, Color.green, Color.yellow,
            Color.cyan, Color.magenta, new Color(1f, 0.5f, 0f), new Color(0.5f, 0f, 1f)
        };

        private GameObject[] _confettis;
        private Renderer[] _renderers;
        private MaterialPropertyBlock[] _mpbs;
        private Sequence _animationSequence;
        private int _count;

        [Inject] private IObjectPoolService _objectPoolService;

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

            var config = GameFeelConfigSO.Instance;
            _count = config != null ? config.ConfettiCount : 40;
            _confettis = new GameObject[_count];
            _renderers = new Renderer[_count];
            _mpbs = new MaterialPropertyBlock[_count];

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

                _mpbs[i] = new MaterialPropertyBlock();
                int colorIdx = i % s_Colors.Length;
                _mpbs[i].SetColor("_BaseColor", s_Colors[colorIdx]);
                mr.SetPropertyBlock(_mpbs[i]);

                _confettis[i] = c;
                _renderers[i] = mr;
            }
        }

        public void Initialize()
        {
            _animationSequence?.Kill();
            _animationSequence = DOTween.Sequence();

            var config = GameFeelConfigSO.Instance;
            Vector2 fallDurRange = config != null ? config.ConfettiFallDuration : new Vector2(1.8f, 3.0f);

            for (int i = 0; i < _count; i++)
            {
                var c = _confettis[i];
                var r = _renderers[i];
                if (c == null || r == null) continue;

                c.SetActive(true);
                r.enabled = true;
                r.SetPropertyBlock(_mpbs[i]);

                // Random starting position (above screen)
                c.transform.localPosition = new Vector3(
                    Random.Range(-5f, 15f),
                    Random.Range(6f, 9f),
                    Random.Range(-2f, 2f)
                );
                c.transform.localScale = new Vector3(
                    Random.Range(0.15f, 0.25f),
                    Random.Range(0.15f, 0.25f),
                    1f
                );
                c.transform.localRotation = Quaternion.identity;

                float fallDuration = Random.Range(fallDurRange.x, fallDurRange.y);
                float swayFreq = Random.Range(3f, 6f);
                float swayAmp = Random.Range(0.5f, 1.2f);
                float startX = c.transform.localPosition.x;
                Vector3 startPos = c.transform.localPosition;

                // Fall down
                _animationSequence.Join(c.transform.DOLocalMoveY(startPos.y - 12f, fallDuration).SetEase(Ease.OutQuad));

                // Horizontal sway via DOTween
                float localStartX = startX;
                _animationSequence.Join(DOTween.To(
                    () => 0f,
                    val =>
                    {
                        if (c == null) return;
                        float sway = Mathf.Sin(val * swayFreq) * swayAmp;
                        var pos = c.transform.localPosition;
                        c.transform.localPosition = new Vector3(localStartX + sway, pos.y, pos.z);
                    },
                    fallDuration,
                    fallDuration
                ).SetEase(Ease.Linear));

                // Spin
                _animationSequence.Join(c.transform.DOLocalRotate(
                    new Vector3(Random.Range(360f, 720f), Random.Range(360f, 720f), Random.Range(360f, 720f)),
                    fallDuration,
                    RotateMode.FastBeyond360
                ).SetEase(Ease.Linear));

                // Fade out at end
                _animationSequence.Join(c.transform.DOScale(Vector3.zero, 0.5f)
                    .SetDelay(fallDuration - 0.5f)
                    .SetEase(Ease.InQuad));
            }

            float despawnDelay = config != null ? config.ConfettiDespawnDelay : 3.1f;
            _animationSequence.OnComplete(() =>
            {
                HideAll();
                DespawnSelf();
            });
        }

        private void HideAll()
        {
            for (int i = 0; i < _count; i++)
            {
                if (_confettis[i] != null)
                {
                    _confettis[i].SetActive(false);
                    _confettis[i].transform.localScale = Vector3.zero;
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

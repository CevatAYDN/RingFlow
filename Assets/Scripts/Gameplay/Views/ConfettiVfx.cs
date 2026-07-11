using System.Collections;
using DG.Tweening;
using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;

namespace RingFlow.Gameplay
{
    public class ConfettiVfx : MonoBehaviour, IPoolable
    {
        private static Shader s_unlitShader;
        private static Material[] s_sharedMaterials;
        private static readonly Color[] s_Colors = {
            Color.red, Color.blue, Color.green, Color.yellow,
            Color.cyan, Color.magenta, new Color(1f, 0.5f, 0f), new Color(0.5f, 0f, 1f)
        };

        private GameObject[] _confettis;

        [Inject] private Nexus.Core.Services.IObjectPoolService _objectPoolService;

        private void Awake()
        {
            if (s_unlitShader == null)
            {
                s_unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
                              ?? Shader.Find("Unlit/Color")
                              ?? Shader.Find("Standard");
            }

            if (s_sharedMaterials == null && s_unlitShader != null)
            {
                s_sharedMaterials = new Material[s_Colors.Length];
                for (int i = 0; i < s_Colors.Length; i++)
                {
                    s_sharedMaterials[i] = new Material(s_unlitShader);
                    s_sharedMaterials[i].color = s_Colors[i];
                }
            }
        }

        public void Initialize()
        {
            if (_confettis != null)
            {
                foreach (var c in _confettis) if (c != null) Destroy(c);
                _confettis = null;
            }

            int count = 40;
            _confettis = new GameObject[count];

            for (int i = 0; i < count; i++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Quad);
                c.transform.SetParent(transform, false);

                c.transform.localPosition = new Vector3(Random.Range(-5f, 15f), Random.Range(6f, 9f), Random.Range(-2f, 2f));
                c.transform.localScale = new Vector3(Random.Range(0.15f, 0.25f), Random.Range(0.15f, 0.25f), 1f);

                var col = c.GetComponent<Collider>();
                if (col != null) Destroy(col);

                int colorIdx = Random.Range(0, s_Colors.Length);
                var renderer = c.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = s_sharedMaterials[colorIdx];

                _confettis[i] = c;

                float fallDuration = Random.Range(1.8f, 3.0f);
                float swayFreq = Random.Range(3f, 6f);
                float swayAmp = Random.Range(0.5f, 1.2f);

                Vector3 targetPos = c.transform.localPosition + new Vector3(0f, -12f, 0f);
                c.transform.DOLocalMoveY(targetPos.y, fallDuration).SetEase(Ease.OutQuad);

                float startX = c.transform.localPosition.x;
                DOTween.To(() => 0f, val => {
                    if (c == null) return;
                    float sway = Mathf.Sin(val * swayFreq) * swayAmp;
                    c.transform.localPosition = new Vector3(startX + sway, c.transform.localPosition.y, c.transform.localPosition.z);
                }, fallDuration, fallDuration).SetEase(Ease.Linear);

                c.transform.DOLocalRotate(
                    new Vector3(Random.Range(360f, 720f), Random.Range(360f, 720f), Random.Range(360f, 720f)),
                    fallDuration,
                    RotateMode.FastBeyond360
                ).SetEase(Ease.Linear);

                c.transform.DOScale(Vector3.zero, 0.5f).SetDelay(fallDuration - 0.5f);
            }

            StartCoroutine(AutoDespawn(3.1f));
        }

        private IEnumerator AutoDespawn(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_objectPoolService != null)
            {
                _objectPoolService.Despawn(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void OnSpawned()
        {
        }

        public void OnDespawned()
        {
            CleanupChildren();
        }

        private void OnDestroy()
        {
            CleanupChildren();
        }

        private void CleanupChildren()
        {
            if (_confettis != null)
            {
                foreach (var c in _confettis) if (c != null) Destroy(c);
                _confettis = null;
            }
        }
    }
}

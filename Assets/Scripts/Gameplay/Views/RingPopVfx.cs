using System.Collections;
using DG.Tweening;
using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;

namespace RingFlow.Gameplay
{
    public class RingPopVfx : MonoBehaviour, IPoolable
    {
        private static Shader s_litShader;
        private static Material s_sharedMaterial;

        private GameObject[] _particles;
        private MaterialPropertyBlock _mpb;

        [Inject] private IObjectPoolService _objectPoolService;

        private void Awake()
        {
            if (s_litShader == null)
            {
                s_litShader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                            ?? Shader.Find("Standard");
            }

            if (s_sharedMaterial == null && s_litShader != null)
            {
                s_sharedMaterial = new Material(s_litShader);
                s_sharedMaterial.SetFloat("_Metallic", 0.5f);
                s_sharedMaterial.SetFloat("_Smoothness", 0.8f);
            }

            _mpb = new MaterialPropertyBlock();
        }

        public void Initialize(Color color)
        {
            if (_particles != null)
            {
                foreach (var p in _particles) if (p != null) Destroy(p);
                _particles = null;
            }

            int count = 12;
            _particles = new GameObject[count];
            _mpb.SetColor("_BaseColor", color);

            for (int i = 0; i < count; i++)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                p.transform.SetParent(transform, false);
                p.transform.localPosition = Vector3.zero;
                p.transform.localScale = Vector3.one * Random.Range(0.12f, 0.22f);

                var col = p.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var renderer = p.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = s_sharedMaterial;
                    renderer.SetPropertyBlock(_mpb);
                }

                _particles[i] = p;

                Vector3 direction = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(0.5f, 2f),
                    Random.Range(-1f, 1f)
                ).normalized * Random.Range(1.5f, 3f);

                p.transform.DOLocalMove(direction, 0.5f).SetEase(Ease.OutQuad);
                p.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InQuad);
            }

            StartCoroutine(AutoDespawn(0.55f));
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
            if (_particles != null)
            {
                foreach (var p in _particles) if (p != null) Destroy(p);
                _particles = null;
            }
        }
    }
}

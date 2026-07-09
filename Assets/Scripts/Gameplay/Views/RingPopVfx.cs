using System.Collections;
using DG.Tweening;
using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;

namespace RingFlow.Gameplay
{
    public class RingPopVfx : MonoBehaviour, IPoolable
    {
        private GameObject[] _particles;
        private static Shader _litShader;
        [Inject] private IObjectPoolService _objectPoolService;

        private void Awake()
        {
            if (_litShader == null)
            {
                _litShader = Shader.Find("Universal Render Pipeline/Lit") 
                            ?? Shader.Find("Universal Render Pipeline/Simple Lit") 
                            ?? Shader.Find("Standard");
            }

            // Inject dependencies if available (runtime initialization)
            var context = NexusRuntime.CurrentContext;
            if (context != null)
            {
                _objectPoolService = context.TryResolve<IObjectPoolService>();
            }
        }

        public void Initialize(Color color)
        {
            if (_particles != null)
            {
                foreach (var p in _particles) if (p != null) Destroy(p);
            }

            int count = 12;
            _particles = new GameObject[count];
            var mat = new Material(_litShader) { color = color };
            mat.SetFloat("_Metallic", 0.5f);
            mat.SetFloat("_Smoothness", 0.8f);

            for (int i = 0; i < count; i++)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                p.transform.SetParent(transform, false);
                p.transform.localPosition = Vector3.zero;
                p.transform.localScale = Vector3.one * Random.Range(0.12f, 0.22f);
                
                var rb = p.GetComponent<Collider>();
                if (rb != null) Destroy(rb);

                var renderer = p.GetComponent<Renderer>();
                if (renderer != null) renderer.sharedMaterial = mat;

                _particles[i] = p;

                // Animate physics-like burst
                Vector3 direction = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(0.5f, 2f),
                    Random.Range(-1f, 1f)
                ).normalized * Random.Range(1.5f, 3f);

                p.transform.DOLocalMove(direction, 0.5f).SetEase(Ease.OutQuad);
                p.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InQuad);
            }

            // Self despawn after animation completes
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
            if (_particles != null)
            {
                foreach (var p in _particles) if (p != null) Destroy(p);
                _particles = null;
            }
        }
    }
}

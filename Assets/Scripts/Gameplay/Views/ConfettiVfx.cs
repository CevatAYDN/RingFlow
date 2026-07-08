using System.Collections;
using DG.Tweening;
using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;

namespace RingFlow.Gameplay
{
    public class ConfettiVfx : MonoBehaviour, IPoolable
    {
        private GameObject[] _confettis;
        private static Shader _unlitShader;

        private void Awake()
        {
            if (_unlitShader == null)
            {
                _unlitShader = Shader.Find("Universal Render Pipeline/Unlit") 
                              ?? Shader.Find("Unlit/Color") 
                              ?? Shader.Find("Standard");
            }
        }

        public void Initialize()
        {
            if (_confettis != null)
            {
                foreach (var c in _confettis) if (c != null) Destroy(c);
            }

            int count = 40;
            _confettis = new GameObject[count];
            Color[] colors = {
                Color.red, Color.blue, Color.green, Color.yellow, 
                Color.cyan, Color.magenta, new Color(1f, 0.5f, 0f), new Color(0.5f, 0f, 1f)
            };

            for (int i = 0; i < count; i++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Quad);
                c.transform.SetParent(transform, false);
                
                // Spawn in a random horizontal spread above the board
                c.transform.localPosition = new Vector3(Random.Range(-5f, 15f), Random.Range(6f, 9f), Random.Range(-2f, 2f));
                c.transform.localScale = new Vector3(Random.Range(0.15f, 0.25f), Random.Range(0.15f, 0.25f), 1f);

                var col = c.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var mat = new Material(_unlitShader) { color = colors[Random.Range(0, colors.Length)] };
                var renderer = c.GetComponent<Renderer>();
                if (renderer != null) renderer.sharedMaterial = mat;

                _confettis[i] = c;

                // Animate falling, swaying, and spinning
                float fallDuration = Random.Range(1.8f, 3.0f);
                float swayFreq = Random.Range(3f, 6f);
                float swayAmp = Random.Range(0.5f, 1.2f);
                
                Vector3 targetPos = c.transform.localPosition + new Vector3(0f, -12f, 0f);
                c.transform.DOLocalMoveY(targetPos.y, fallDuration).SetEase(Ease.OutQuad);

                // Swaying X motion
                float startX = c.transform.localPosition.x;
                DOTween.To(() => 0f, val => {
                    if (c == null) return;
                    float sway = Mathf.Sin(val * swayFreq) * swayAmp;
                    c.transform.localPosition = new Vector3(startX + sway, c.transform.localPosition.y, c.transform.localPosition.z);
                }, fallDuration, fallDuration).SetEase(Ease.Linear);

                // Spinning rotation
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
            var pool = NexusRuntime.CurrentContext?.TryResolve<IObjectPoolService>();
            if (pool != null)
            {
                pool.Despawn(gameObject);
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
            if (_confettis != null)
            {
                foreach (var c in _confettis) if (c != null) Destroy(c);
                _confettis = null;
            }
        }
    }
}

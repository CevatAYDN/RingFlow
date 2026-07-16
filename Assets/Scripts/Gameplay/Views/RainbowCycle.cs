using UnityEngine;

namespace RingFlow.Gameplay
{
    // FIX-V2: Two issues in the original RainbowCycle (previously an inner class in BoardView):
    //
    // 1. _propBlock allocated in Start() — Start() may not run before the first
    //    Update() when the component is added via AddComponent at runtime (the frame
    //    boundary is not guaranteed). If Update fires first, _propBlock is null and
    //    GetPropertyBlock throws NullReferenceException. Fix: lazy-init in Update().
    //
    // 2. Renderer cached in Start() via GetComponentInParent — but Initialize() is
    //    called before Start() runs, so any early Update() also misses the renderer.
    //    Fix: cache renderer eagerly in Initialize() and in Awake() as a second pass.
    //
    // 3. No cleanup on pool-return: when RecycleRing destroys the RainbowCycle child
    //    OR the ring is returned to pool, the MaterialPropertyBlock color persists on
    //    the renderer, visually tainting the next ring that uses it.
    //    Fix: clear the property block in OnDisable().
    //
    // ARCH-1: Extracted from BoardView.cs inner class to its own file to reduce
    //    BoardView's line count and give RainbowCycle independent compilation + testability.
    public class RainbowCycle : MonoBehaviour
    {
        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private GameFeelConfigSO _feel;

        public void Initialize(GameFeelConfigSO feel)
        {
            _feel = feel;
            // Cache renderer eagerly so Update() works even before Start() fires.
            if (_renderer == null)
                _renderer = GetComponentInParent<Renderer>();
        }

        private void Awake()
        {
            // Second-pass cache in case Initialize() hasn't been called yet.
            if (_renderer == null)
                _renderer = GetComponentInParent<Renderer>();
        }

        private void Update()
        {
            if (_renderer == null || _feel == null) return;

            // FIX-V2: Lazy-init avoids the Start()-before-Update() race.
            // MaterialPropertyBlock is a value-type wrapper — one allocation total.
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            float hue = (Time.time * _feel.RainbowHueSpeed) % 1f;
            Color color = Color.HSVToRGB(hue, _feel.RainbowSaturation, _feel.RainbowValue);
            // GetPropertyBlock reads existing state into _propBlock (no allocation).
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_Color", color);
            _propBlock.SetColor("_BaseColor", color);
            _propBlock.SetColor("_EmissionColor", color * 0.3f);
            _renderer.SetPropertyBlock(_propBlock);
        }

        private void OnDisable()
        {
            // FIX-V2: Clear rainbow color when returned to pool / disabled.
            if (_renderer != null && _propBlock != null)
            {
                _propBlock.Clear();
                _renderer.SetPropertyBlock(_propBlock);
            }
        }
    }
}

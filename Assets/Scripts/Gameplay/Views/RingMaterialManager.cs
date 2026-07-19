using System.Collections.Generic;
using Nexus.Core;
using UnityEngine;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Aşama 1 — BoardView'dan ayrıştırılan material yönetim sınıfı.
    /// 
    /// Sorumluluk: Ring ve pole material'lerinin oluşturulması, önbelleğe alınması
    /// ve özel ring tiplerine göre material parametrelerinin ayarlanması.
    ///
    /// Bu sınıf:
    /// • Herhangi bir MonoBehaviour olay döngüsüne bağımlı değildir (pure POCO).
    /// • Kendi material cache'ini yönetir.
    /// • Statik shader/font referanslarını tutar.
    /// • BoardView'daki material çağrılarının tam karşılığıdır — refactoring
    ///   sırasında görsel fark oluşmaz.
    /// </summary>
    public sealed class RingMaterialManager
    {
        // ── Static cache ────────────────────────────────────────────────
        private static Shader _cachedShader;
        private static Font _cachedBuiltinFont;

        // ── Instance cache ──────────────────────────────────────────────
        private readonly Dictionary<(RingColor, RingType), Material> _ringMaterialCache = new();
        private Material _openPoleMaterial;
        private Material _lockedPoleMaterial;

        // ── DI dependencies ─────────────────────────────────────────────
        [Inject] private GameFeelConfigSO _feelConfig;
        [Inject] private RingColorPaletteSO _colorPalette;
        [Inject] private SettingsModel _settingsModel;

        private GameFeelConfigSO F => _feelConfig;

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Returns the Unity Color for the given RingColor, respecting the
        /// active color-blind mode from SettingsModel.
        /// </summary>
        public Color GetRingColor(RingColor color)
        {
            if (_colorPalette == null)
            {
                throw new System.InvalidOperationException(
                    "[RingMaterialManager] RingColorPaletteSO is not injected!");
            }

            var mode = _settingsModel != null
                ? (RingColorPaletteSO.ColorBlindMode)_settingsModel.ColorBlindMode.Value
                : RingColorPaletteSO.ColorBlindMode.Off;

            return _colorPalette.GetColor(color, mode);
        }

        /// <summary>
        /// Returns (creating + caching as needed) the material for a ring
        /// with the given color and special type.
        /// </summary>
        public Material GetRingMaterial(RingColor color, RingType type)
        {
            var key = (color, type);
            if (_ringMaterialCache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var mat = new Material(GetDefaultShader());
            Color baseColor = GetRingColor(color);
            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", baseColor);
            mat.SetFloat("_Metallic", F.RingMetallic);
            mat.SetFloat("_Smoothness", F.RingSmoothness);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
            ApplySpecialRingMaterial(mat, baseColor, type);
            mat.name = "RingMat_" + color + "_" + type;
            _ringMaterialCache[key] = mat;
            return mat;
        }

        /// <summary>
        /// Returns (creating + caching as needed) the material for a pole.
        /// Locked poles get a darker material tint.
        /// </summary>
        public Material GetPoleMaterial(bool locked)
        {
            var feel = F;
            if (feel == null)
            {
                throw new System.InvalidOperationException(
                    "[RingMaterialManager] GameFeelConfigSO is not injected!");
            }

            if (locked)
            {
                if (_lockedPoleMaterial == null)
                {
                    var darkColor = feel.PoleColorLocked;
                    _lockedPoleMaterial = new Material(GetDefaultShader())
                        { color = darkColor, name = "PoleMat_Locked" };
                    if (_lockedPoleMaterial.HasProperty("_BaseColor"))
                        _lockedPoleMaterial.SetColor("_BaseColor", darkColor);
                    _lockedPoleMaterial.SetFloat("_Metallic", feel.PoleMetallic);
                    _lockedPoleMaterial.SetFloat("_Smoothness", feel.PoleSmoothness);
                }
                return _lockedPoleMaterial;
            }

            if (_openPoleMaterial == null)
            {
                var openColor = feel.PoleColorOpen;
                _openPoleMaterial = new Material(GetDefaultShader())
                    { color = openColor, name = "PoleMat_Open" };
                if (_openPoleMaterial.HasProperty("_BaseColor"))
                    _openPoleMaterial.SetColor("_BaseColor", openColor);
                _openPoleMaterial.SetFloat("_Metallic", feel.PoleMetallic);
                _openPoleMaterial.SetFloat("_Smoothness", feel.PoleSmoothness);
            }
            return _openPoleMaterial;
        }

        /// <summary>
        /// Clears the instance material cache (destroys Unity GPU resources first).
        /// Safe to call multiple times; idempotent after first call.
        /// </summary>
        public void ClearCache()
        {
            foreach (var mat in _ringMaterialCache.Values)
            {
                if (mat != null && Application.isPlaying)
                    Object.Destroy(mat);
                else if (mat != null)
                    Object.DestroyImmediate(mat);
            }
            _ringMaterialCache.Clear();

            if (_openPoleMaterial != null)
            {
                if (Application.isPlaying) Object.Destroy(_openPoleMaterial);
                else Object.DestroyImmediate(_openPoleMaterial);
                _openPoleMaterial = null;
            }

            if (_lockedPoleMaterial != null)
            {
                if (Application.isPlaying) Object.Destroy(_lockedPoleMaterial);
                else Object.DestroyImmediate(_lockedPoleMaterial);
                _lockedPoleMaterial = null;
            }
        }

        /// <summary>
        /// Returns the shared default shader (URP Lit → Simple Lit → Standard fallback).
        /// </summary>
        public Shader GetDefaultShader()
        {
            if (_cachedShader != null) return _cachedShader;
            _cachedShader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Standard");
            return _cachedShader;
        }

        /// <summary>
        /// Returns the shared built-in font for UI labels.
        /// </summary>
        public Font GetBuiltinLabelFont()
        {
            if (_cachedBuiltinFont != null) return _cachedBuiltinFont;
            _cachedBuiltinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                                 ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _cachedBuiltinFont;
        }

        // ── Internal helpers ────────────────────────────────────────────

        private void ApplySpecialRingMaterial(Material mat, Color baseColor, RingType type)
        {
            switch (type)
            {
                case RingType.Frozen:
                    mat.color = Color.Lerp(baseColor, Color.cyan, 0.5f);
                    mat.SetFloat("_Metallic", 0.1f); mat.SetFloat("_Smoothness", 0.9f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.3f, 0.6f, 0.8f, 1f));
                    break;
                case RingType.Key:
                case RingType.Locked:
                    mat.color = new Color(1f, 0.84f, 0f);
                    mat.SetFloat("_Metallic", 0.8f); mat.SetFloat("_Smoothness", 0.6f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.5f, 0.4f, 0f, 1f));
                    break;
                case RingType.Stone:
                    mat.color = new Color(0.4f, 0.38f, 0.35f);
                    mat.SetFloat("_Metallic", 0f); mat.SetFloat("_Smoothness", 0.1f);
                    break;
                case RingType.Glass:
                    mat.color = new Color(1f, 1f, 1f, 0.45f);
                    mat.SetFloat("_Metallic", 0.1f); mat.SetFloat("_Smoothness", 0.95f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.15f, 0.15f, 0.15f, 1f));
                    SetFadeMode(mat);
                    break;
                case RingType.Rainbow:
                    mat.SetFloat("_Metallic", 0.5f); mat.SetFloat("_Smoothness", 0.8f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.Lerp(baseColor, Color.white, 0.3f));
                    break;
                case RingType.Ghost:
                    mat.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.45f);
                    mat.SetFloat("_Metallic", 0.3f); mat.SetFloat("_Smoothness", 0.3f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(baseColor.r * 0.2f, baseColor.g * 0.2f, baseColor.b * 0.2f, 1f));
                    SetFadeMode(mat);
                    break;
                case RingType.Bomb:
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.8f, 0.2f, 0f, 1f));
                    break;
                case RingType.Chain:
                    mat.SetFloat("_Metallic", 0.7f); mat.SetFloat("_Smoothness", 0.3f);
                    break;
                case RingType.Magnet:
                    mat.SetFloat("_Metallic", 0.9f); mat.SetFloat("_Smoothness", 0.5f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.5f, 0f, 0.5f, 1f));
                    break;
                case RingType.Paint:
                    mat.SetFloat("_Smoothness", 0.9f);
                    break;
                case RingType.Mystery:
                    mat.color = new Color(0.3f, 0.3f, 0.3f);
                    mat.SetFloat("_Metallic", 0.4f); mat.SetFloat("_Smoothness", 0.6f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.2f, 0.2f, 0.2f, 1f));
                    break;
            }
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", mat.color);
        }

        /// <summary>
        /// Configures the material for transparent rendering.
        /// Supports both URP and Standard Shader pipelines.
        /// </summary>
        private static void SetFadeMode(Material mat)
        {
            // ── URP path ──────────────────────────────────────────────
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);            // 1 = Transparent
                mat.SetFloat("_Blend", 0f);              // 0 = Alpha blend
                mat.SetInt("_ZWrite", 0);
                mat.SetInt("_SrcBlend",
                    (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend",
                    (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                return;
            }

            // ── Standard Shader fallback ──────────────────────────────
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend",
                (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend",
                (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
        }
    }
}

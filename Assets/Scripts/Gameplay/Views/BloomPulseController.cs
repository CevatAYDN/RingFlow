using DG.Tweening;
using UnityEngine;
using System;
using System.Reflection;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Controls post-processing bloom intensity for pole completion moments.
    /// Uses reflection to access Volume/Bloom types from URP to avoid compile-time assembly dependencies.
    /// Falls back silently if no global Volume with a Bloom override exists.
    /// Designed for zero allocation during gameplay after initialization.
    /// </summary>
    public class BloomPulseController : MonoBehaviour
    {
        private Component _volume;
        private object _bloom;
        private PropertyInfo _profileProperty;
        private MethodInfo _tryGetMethod;
        private Type _bloomType;

        // Reflection-based access to Bloom fields
        private FieldInfo _intensityField;
        private FieldInfo _thresholdField;

        private float _originalIntensity;
        private float _originalThreshold;
        private Sequence _pulseSequence;
        private bool _isReady;

        // Cached access to the inner ParameterValue<float>
        private Type _floatParameterType;
        private Type _bloomOverrideType;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Find Volume type via reflection — UnityEngine.Rendering.Universal may not be compiled
            Type volumeType = Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
            if (volumeType == null)
            {
                Debug.LogWarning("[BloomPulse] Volume type not found. URP may not be available. Bloom pulse disabled.");
                return;
            }

            // Find all Volume components
            var volumes = FindObjectsByType(volumeType, FindObjectsInactive.Include);
            if (volumes == null || volumes.Length == 0)
            {
                Debug.LogWarning("[BloomPulse] No Volume components found. Bloom pulse disabled.");
                return;
            }

            // Pick first global volume
            PropertyInfo isGlobalProp = volumeType.GetProperty("isGlobal");
            PropertyInfo profileProp = volumeType.GetProperty("profile");
            _profileProperty = profileProp;

            Component selectedVolume = null;
            foreach (var v in volumes)
            {
                if (v is Component comp)
                {
                    if (isGlobalProp != null)
                    {
                        bool isGlobal = (bool)isGlobalProp.GetValue(comp);
                        if (isGlobal)
                        {
                            selectedVolume = comp;
                            break;
                        }
                    }
                }
            }

            if (selectedVolume == null && volumes.Length > 0)
            {
                selectedVolume = volumes[0] as Component;
            }

            if (selectedVolume == null)
            {
                Debug.LogWarning("[BloomPulse] No suitable Volume found. Bloom pulse disabled.");
                return;
            }

            _volume = selectedVolume;

            // Try to get Bloom override from profile
            // Volume.profile.TryGet<T>(out T override)
            _bloomType = Type.GetType("UnityEngine.Rendering.Universal.Bloom, Unity.RenderPipelines.Universal.Runtime");
            if (_bloomType == null)
            {
                Debug.LogWarning("[BloomPulse] Bloom type not found. URP Bloom override may not be available.");
                return;
            }

            // VolumeProfile.TryGet<T>(out T override)
            if (profileProp != null)
            {
                object profile = profileProp.GetValue(_volume);
                if (profile == null)
                {
                    Debug.LogWarning("[BloomPulse] Volume has no profile. Bloom pulse disabled.");
                    return;
                }

                Type profileType = profile.GetType();
                _tryGetMethod = profileType.GetMethod("TryGet", new[] { _bloomType.MakeByRefType() });
                if (_tryGetMethod == null)
                {
                    Debug.LogWarning("[BloomPulse] VolumeProfile.TryGet not found. Bloom pulse disabled.");
                    return;
                }

                var parameters = new object[] { null };
                bool found = (bool)_tryGetMethod.Invoke(profile, parameters);
                if (!found || parameters[0] == null)
                {
                    Debug.LogWarning("[BloomPulse] Volume has no Bloom override. Add a Bloom override to the Volume profile.");
                    return;
                }

                _bloom = parameters[0];
            }

            if (_bloom == null)
            {
                Debug.LogWarning("[BloomPulse] Could not resolve Bloom override. Bloom pulse disabled.");
                return;
            }

            // Get FloatParameter fields from Bloom — Bloom has fields like: intensity, threshold
            // These are of type ClampedFloatParameter or FloatParameter (both from UnityEngine.Rendering)
            _floatParameterType = Type.GetType("UnityEngine.Rendering.FloatParameter, Unity.RenderPipelines.Core.Runtime");
            if (_floatParameterType == null)
            {
                _floatParameterType = Type.GetType("UnityEngine.Rendering.VolumeParameter`1, Unity.RenderPipelines.Core.Runtime");
            }

            // Cache the FieldInfo for value access
            _intensityField = _bloomType.GetField("intensity");
            _thresholdField = _bloomType.GetField("threshold");

            if (_intensityField == null || _thresholdField == null)
            {
                Debug.LogWarning("[BloomPulse] Could not find intensity/threshold fields on Bloom. Bloom pulse disabled.");
                return;
            }

            // Read original values
            _originalIntensity = ReadFloatValue(_intensityField);
            _originalThreshold = ReadFloatValue(_thresholdField);

            _isReady = true;
        }

        private float ReadFloatValue(FieldInfo field)
        {
            object paramObj = field.GetValue(_bloom);
            if (paramObj == null) return 0f;

            // FloatParameter / VolumeParameter<T> has a 'value' property
            Type paramType = paramObj.GetType();
            PropertyInfo valueProp = paramType.GetProperty("value");
            if (valueProp != null)
            {
                return (float)valueProp.GetValue(paramObj);
            }

            return 0f;
        }

        private void SetFloatValue(FieldInfo field, float value)
        {
            object paramObj = field.GetValue(_bloom);
            if (paramObj == null) return;

            Type paramType = paramObj.GetType();
            PropertyInfo valueProp = paramType.GetProperty("value");
            if (valueProp != null)
            {
                valueProp.SetValue(paramObj, value);
            }
        }

        private void SetOverrideState(FieldInfo field, bool state)
        {
            object paramObj = field.GetValue(_bloom);
            if (paramObj == null) return;

            Type paramType = paramObj.GetType();
            PropertyInfo overrideProp = paramType.GetProperty("overrideState");
            if (overrideProp != null)
            {
                overrideProp.SetValue(paramObj, state);
            }
        }

        /// <summary>
        /// Pulse bloom intensity for a pole completion moment.
        /// </summary>
        /// <param name="intensityMultiplier">How much to boost bloom (e.g. 3.0 = 3x normal).</param>
        /// <param name="duration">Duration of the pulse in seconds.</param>
        /// <param name="isFinalPole">If true, pulse is stronger and longer.</param>
        public void Pulse(float intensityMultiplier, float duration, bool isFinalPole)
        {
            if (!_isReady)
            {
                if (_volume == null) Initialize();
                if (!_isReady) return;
            }

            _pulseSequence?.Kill();
            _pulseSequence = DOTween.Sequence();

            float targetIntensity = _originalIntensity * intensityMultiplier;
            float pulseIn = duration * 0.3f;
            float pulseHold = isFinalPole ? duration * 0.3f : 0f;
            float pulseOut = duration * 0.4f;

            // Ensure bloom is active
            SetOverrideState(_intensityField, true);
            SetOverrideState(_thresholdField, true);

            // Phase 1: Rise
            _pulseSequence.Append(DOTween.To(
                () => ReadFloatValue(_intensityField),
                val => SetFloatValue(_intensityField, val),
                targetIntensity,
                pulseIn
            ).SetEase(Ease.OutQuad));

            // Phase 2: Optional hold (final pole only)
            if (pulseHold > 0f)
            {
                _pulseSequence.AppendInterval(pulseHold);
            }

            // Phase 3: Return
            _pulseSequence.Append(DOTween.To(
                () => ReadFloatValue(_intensityField),
                val => SetFloatValue(_intensityField, val),
                _originalIntensity,
                pulseOut
            ).SetEase(Ease.InQuad));

            // Lower threshold slightly for more dramatic bloom during the pulse
            float loweredThreshold = Mathf.Max(0.5f, _originalThreshold * 0.7f);
            _pulseSequence.Insert(0f, DOTween.To(
                () => ReadFloatValue(_thresholdField),
                val => SetFloatValue(_thresholdField, val),
                loweredThreshold,
                pulseIn * 0.5f
            ).SetEase(Ease.OutQuad));
            _pulseSequence.Insert(pulseIn * 0.5f, DOTween.To(
                () => ReadFloatValue(_thresholdField),
                val => SetFloatValue(_thresholdField, val),
                _originalThreshold,
                pulseOut
            ).SetEase(Ease.InQuad));
        }

        /// <summary>
        /// Reset bloom to original values immediately.
        /// </summary>
        public void ResetBloom()
        {
            _pulseSequence?.Kill();
            _pulseSequence = null;
            if (_bloom != null)
            {
                SetFloatValue(_intensityField, _originalIntensity);
                SetFloatValue(_thresholdField, _originalThreshold);
            }
        }

        /// <summary>
        /// Check if the bloom controller is ready to use.
        /// </summary>
        public bool IsReady => _isReady;

        private void OnDestroy()
        {
            _pulseSequence?.Kill();
            _pulseSequence = null;
        }
    }
}

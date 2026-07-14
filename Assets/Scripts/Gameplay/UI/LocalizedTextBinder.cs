using Nexus.Core;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Lightweight runtime localization binder for authored prefabs.
    /// Uses Nexus current context so prefab text does not need mediator-specific glue.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalizedTextBinder : MonoBehaviour
    {
        [SerializeField] private string _key;
        [SerializeField] private string _fallback;
        [SerializeField] private bool _localizeOnEnable = true;

        public string Key
        {
            get => _key;
            set => _key = value;
        }

        public string Fallback
        {
            get => _fallback;
            set => _fallback = value;
        }

        private void OnEnable()
        {
            if (_localizeOnEnable)
                Apply();
        }

        public void Apply()
        {
            if (string.IsNullOrEmpty(_key)) return;
            var loc = NexusRuntime.CurrentContext?.TryResolve<ILocalizationService>();
            if (loc == null) return;

            var resolved = loc.GetString(_key, _fallback);
            var text = GetComponent<Text>();
            if (text == null)
                text = GetComponentInChildren<Text>(true);
            if (text == null) return;

            if (string.IsNullOrEmpty(resolved))
                resolved = text.text;
            text.text = resolved;

            if (loc.IsRTL)
            {
                if (text.alignment == TextAnchor.MiddleLeft) text.alignment = TextAnchor.MiddleRight;
                else if (text.alignment == TextAnchor.UpperLeft) text.alignment = TextAnchor.UpperRight;
                else if (text.alignment == TextAnchor.LowerLeft) text.alignment = TextAnchor.LowerRight;
            }
        }
    }
}

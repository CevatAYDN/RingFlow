using Nexus.Core;
using Nexus.Core.Services;
using TMPro;
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
            var text = GetComponent<TextMeshProUGUI>();
            if (text == null)
                text = GetComponentInChildren<TextMeshProUGUI>(true);
            if (text == null) return;

            if (string.IsNullOrEmpty(resolved))
                resolved = text.text;
            text.text = resolved;

            if (loc.IsRTL)
            {
                if (text.alignment == TextAlignmentOptions.MidlineLeft) text.alignment = TextAlignmentOptions.MidlineRight;
                else if (text.alignment == TextAlignmentOptions.TopLeft) text.alignment = TextAlignmentOptions.TopRight;
                else if (text.alignment == TextAlignmentOptions.BottomLeft) text.alignment = TextAlignmentOptions.BottomRight;
            }
        }
    }
}

using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Editor
{
    public sealed class SettingsSection : EditorSection
    {
        public override string DisplayName => "Accessibility & Localizer Settings";
        public override string PrefKey => EditorPrefsKeys.FoldSettings;

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox(
                        "Enter PlayMode to control settings reactively.",
                        MessageType.Info);
                    return;
                }

                var context = NexusRuntime.CurrentContext;
                var settings = context?.TryResolve<SettingsModel>();
                var localization = context?.TryResolve<ILocalizationService>();

                if (settings == null) return;

                ToggleRow("Music Enabled", settings.MusicEnabled);
                ToggleRow("SFX Enabled",   settings.SfxEnabled);
                ToggleRow("Haptic",        settings.HapticEnabled);
                ToggleRow("Reduce Motion", settings.ReduceMotion);
                ToggleRow("Big Buttons",   settings.BigButtons);

                int blind = EditorGUILayout.IntSlider("Color Blind", settings.ColorBlindMode.Value, 0, 3);
                if (blind != settings.ColorBlindMode.Value)
                {
                    settings.ColorBlindMode.Value = blind;
                }

                LanguageRow(settings, localization);
            }
        }

        private static void ToggleRow(string label, ObservableProperty<bool> prop)
        {
            bool v = EditorGUILayout.Toggle(label, prop.Value);
            if (v != prop.Value) prop.Value = v;
        }

        private static void LanguageRow(SettingsModel settings, ILocalizationService localization)
        {
            string[] langs = { "en", "tr", "id", "es", "fr", "de", "pt", "it", "ar", "hi", "ru", "ja", "zh", "ko", "vi" };
            int idx = System.Array.IndexOf(langs, settings.LanguageCode.Value);
            if (idx == -1) idx = 0;
            int newIdx = EditorGUILayout.Popup("Language", idx, langs);
            if (newIdx != idx) settings.LanguageCode.Value = langs[newIdx];

            if (localization != null)
            {
                EditorGUILayout.LabelField($"Active: {localization.CurrentLanguage ?? "—"}");
            }
        }
    }
}

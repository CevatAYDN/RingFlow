using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;
using RingFlow.Gameplay.Localization;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Editor
{
    public sealed class SettingsSection : EditorSection
    {
        private static List<string> _languageOptions;

        private static List<string> GetLanguageOptions()
        {
            if (_languageOptions == null)
            {
                var config = Resources.Load<LocalizationConfigSO>(EditorPaths.LocalizationConfigKey);
                if (config != null && config.Languages != null && config.Languages.Count > 0)
                {
                    _languageOptions = new List<string>(config.Languages.Count);
                    for (int i = 0; i < config.Languages.Count; i++)
                        _languageOptions.Add(config.Languages[i].Code);
                }
                else
                {
                    _languageOptions = new List<string> { "en", "tr", "id", "es", "fr", "de", "pt", "it", "ar", "hi", "ru", "ja", "zh", "ko", "vi" };
                }
            }
            return _languageOptions;
        }

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
                if (context == null)
                {
                    EditorGUILayout.HelpBox(
                        "Nexus runtime context is not available yet.",
                        MessageType.Warning);
                    return;
                }

                var settings = context.TryResolve<SettingsModel>();
                var localization = context.TryResolve<ILocalizationService>();

                if (settings == null)
                {
                    EditorGUILayout.HelpBox(
                        "SettingsModel is not resolved in current context.",
                        MessageType.Warning);
                    return;
                }

                ToggleRow("Music Enabled", settings.MusicEnabled);
                ToggleRow("SFX Enabled",   settings.SfxEnabled);
                ToggleRow("Haptic",        settings.HapticEnabled);
                ToggleRow("Reduce Motion", settings.ReduceMotion);
                ToggleRow("Big Buttons",   settings.BigButtons);

                int blind = EditorGUILayout.IntSlider("Color Blind", settings.ColorBlindMode.Value, 0, 3);
                if (blind != settings.ColorBlindMode.Value)
                    settings.ColorBlindMode.Value = blind;

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
            string currentLang = settings.LanguageCode.Value;
            var options = GetLanguageOptions();
            int idx = options.IndexOf(currentLang);
            if (idx == -1) idx = 0;

            int newIdx = EditorGUILayout.Popup("Language", idx, options.ToArray());
            if (newIdx != idx)
                settings.LanguageCode.Value = options[newIdx];

            if (localization != null)
                EditorGUILayout.LabelField($"Active: {localization.CurrentLanguage ?? "\u2014"}");
        }
    }
}

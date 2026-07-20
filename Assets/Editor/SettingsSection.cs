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
        private static List<string> GetLanguageOptions()
        {
            var config = Resources.Load<LocalizationConfigSO>(EditorPaths.LocalizationConfigKey);
            if (config != null && config.Languages != null && config.Languages.Count > 0)
            {
                var options = new List<string>(config.Languages.Count);
                for (int i = 0; i < config.Languages.Count; i++)
                    options.Add(config.Languages[i].Code);
                return options;
            }
            return new List<string> { "en", "tr", "id", "es", "fr", "de", "pt", "it", "ar", "hi", "ru", "ja", "zh", "ko", "vi" };
        }

        public override string DisplayName => "Accessibility & Localizer Settings";
        public override string PrefKey => EditorPrefsKeys.FoldSettings;

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            RingFlowEditorUtils.BeginSectionBox("Erişilebilirlik ve Yerelleştirme Ayarları", "Çalışma zamanında müzik, ses, haptik ve dil ayarlarını yönetin.");

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Ayarları canlı olarak değiştirmek için PlayMode'a girin.",
                    MessageType.Info);
                RingFlowEditorUtils.EndSectionBox();
                return;
            }

            var context = NexusRuntime.CurrentContext;
            if (context == null)
            {
                EditorGUILayout.HelpBox(
                    "Nexus çalışma zamanı bağlamı henüz mevcut değil.",
                    MessageType.Warning);
                RingFlowEditorUtils.EndSectionBox();
                return;
            }

            var settings = context.TryResolve<SettingsModel>();
            var localization = context.TryResolve<ILocalizationService>();

            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "Mevcut bağlamda SettingsModel çözümlenemedi.",
                    MessageType.Warning);
                RingFlowEditorUtils.EndSectionBox();
                return;
            }

            ToggleRow("Müzik Etkin", settings.MusicEnabled);
            ToggleRow("Ses Efektleri Etkin (SFX)",   settings.SfxEnabled);
            ToggleRow("Haptik Titreşim",        settings.HapticEnabled);
            ToggleRow("Hareketi Azalt (Reduce Motion)", settings.ReduceMotion);
            ToggleRow("Büyük Butonlar (Big Buttons)",   settings.BigButtons);

            int blind = EditorGUILayout.IntSlider("Renk Körlüğü Modu", settings.ColorBlindMode.Value, 0, 3);
            if (blind != settings.ColorBlindMode.Value)
                settings.ColorBlindMode.Value = blind;

            LanguageRow(settings, localization);
            
            RingFlowEditorUtils.EndSectionBox();
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

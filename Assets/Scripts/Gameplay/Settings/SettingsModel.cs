using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// GDD §11 — Accessibility + Audio settings. Persisted via IPlayerPrefsService.
    /// Reactive so HUD/settings menus can subscribe.
    /// </summary>
    public class SettingsModel : IReactiveModel, IResettableModel
    {
        public const string KeyMusic = GameplayAssetKeys.PlayerPrefs.Music;
        public const string KeySfx = GameplayAssetKeys.PlayerPrefs.Sfx;
        public const string KeyHaptic = GameplayAssetKeys.PlayerPrefs.Haptic;
        public const string KeyReduceMotion = GameplayAssetKeys.PlayerPrefs.ReduceMotion;
        public const string KeySlowMode = GameplayAssetKeys.PlayerPrefs.SlowMode;
        public const string KeyBigButtons = GameplayAssetKeys.PlayerPrefs.BigButtons;
        public const string KeyColorBlind = GameplayAssetKeys.PlayerPrefs.ColorBlind;
        public const string KeyLanguage = GameplayAssetKeys.PlayerPrefs.Language;
        internal const string LegacyKeyLanguage = "LanguageCode";

        public ObservableProperty<bool> MusicEnabled { get; } = new(true);
        public ObservableProperty<bool> SfxEnabled { get; } = new(true);
        public ObservableProperty<bool> HapticEnabled { get; } = new(true);

        public ObservableProperty<bool> ReduceMotion { get; } = new(false);
        public ObservableProperty<bool> SlowMode { get; } = new(false);
        public ObservableProperty<bool> BigButtons { get; } = new(false);

        public ObservableProperty<int> ColorBlindMode { get; } = new(0); // 0=Off (see RingColorPaletteSO.ColorBlindMode)

        public ObservableProperty<string> LanguageCode { get; } = new("en");

        public ValueTask OnBind(System.Threading.CancellationToken ct)
        {
            // Hook reactive updates back to Nexus audio service via the lifecycle.
            return default;
        }

        public void Reset()
        {
            MusicEnabled.Value = true;
            SfxEnabled.Value = true;
            HapticEnabled.Value = true;
            ReduceMotion.Value = false;
            SlowMode.Value = false;
            BigButtons.Value = false;
            ColorBlindMode.Value = 0;
            LanguageCode.Value = "en";
        }
    }

    public static class SettingsSaveSystem
    {
        public static void Save(IPlayerPrefsService prefs, SettingsModel m)
        {
            prefs.SetBool(SettingsModel.KeyMusic, m.MusicEnabled.Value);
            prefs.SetBool(SettingsModel.KeySfx, m.SfxEnabled.Value);
            prefs.SetBool(SettingsModel.KeyHaptic, m.HapticEnabled.Value);
            prefs.SetBool(SettingsModel.KeyReduceMotion, m.ReduceMotion.Value);
            prefs.SetBool(SettingsModel.KeySlowMode, m.SlowMode.Value);
            prefs.SetBool(SettingsModel.KeyBigButtons, m.BigButtons.Value);
            prefs.SetInt(SettingsModel.KeyColorBlind, m.ColorBlindMode.Value);
            prefs.SetString(SettingsModel.KeyLanguage, m.LanguageCode.Value);
            prefs.Save();
        }

        public static void Load(IPlayerPrefsService prefs, SettingsModel m)
        {
            m.MusicEnabled.Value = prefs.GetBool(SettingsModel.KeyMusic, true);
            m.SfxEnabled.Value = prefs.GetBool(SettingsModel.KeySfx, true);
            m.HapticEnabled.Value = prefs.GetBool(SettingsModel.KeyHaptic, true);
            m.ReduceMotion.Value = prefs.GetBool(SettingsModel.KeyReduceMotion, false);
            m.SlowMode.Value = prefs.GetBool(SettingsModel.KeySlowMode, false);
            m.BigButtons.Value = prefs.GetBool(SettingsModel.KeyBigButtons, false);
            m.ColorBlindMode.Value = prefs.GetInt(SettingsModel.KeyColorBlind, 0);
            m.LanguageCode.Value = prefs.GetString(SettingsModel.KeyLanguage, prefs.GetString(SettingsModel.LegacyKeyLanguage, "en"));
        }
    }
}

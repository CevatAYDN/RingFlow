using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay;

namespace RingFlow.Tests
{
    [TestFixture]
    public class SettingsAndLocalizationTests
    {
        private MockPlayerPrefsService _prefs;
        private SettingsModel _settings;
        private PlayerProgressModel _progress;

        [SetUp]
        public void Setup()
        {
            _prefs = new MockPlayerPrefsService();
            _settings = new SettingsModel();
            _progress = new PlayerProgressModel();
        }

        [Test]
        public void SettingsModel_SavesAndLoadsCorrectly()
        {
            _settings.MusicEnabled.Value = false;
            _settings.ColorBlindMode.Value = 2;
            _settings.LanguageCode.Value = "tr";

            SettingsSaveSystem.Save(_prefs, _settings);

            // Reset model
            _settings.Reset();
            Assert.IsTrue(_settings.MusicEnabled.Value);
            Assert.AreEqual(0, _settings.ColorBlindMode.Value);
            Assert.AreEqual("en", _settings.LanguageCode.Value);

            // Load back
            SettingsSaveSystem.Load(_prefs, _settings);
            Assert.IsFalse(_settings.MusicEnabled.Value);
            Assert.AreEqual(2, _settings.ColorBlindMode.Value);
            Assert.AreEqual("tr", _settings.LanguageCode.Value);
        }

        [Test]
        public void PlayerProgressSaveSystem_SavesAndLoadsCorrectly()
        {
            _progress.Coins.Value = 2500;
            _progress.Diamonds.Value = 150;
            _progress.CurrentLevel.Value = 42;
            _progress.MaxUnlockedLevel.Value = 42;
            _progress.DailyDayIndex.Value = 3;
            _progress.DailyLastClaimUtcTicks.Value = 1234567890L;

            _progress.UnlockedWorlds.Clear();
            for (int i = 0; i < 40; i++)
            {
                _progress.UnlockedWorlds.Add(i < 3); // Unlock first 3 worlds
            }
            _progress.OwnedThemes.Add("classic");
            _progress.OwnedThemes.Add("neon");
            _progress.Achievements.Add("win_first_level");

            PlayerProgressSaveSystem.Save(_prefs, _progress);

            // Reset progress model
            _progress = new PlayerProgressModel();
            Assert.AreEqual(0, _progress.Coins.Value);
            Assert.AreEqual(1, _progress.CurrentLevel.Value);

            PlayerProgressSaveSystem.Load(_prefs, _progress);

            Assert.AreEqual(2500, _progress.Coins.Value);
            Assert.AreEqual(150, _progress.Diamonds.Value);
            Assert.AreEqual(42, _progress.CurrentLevel.Value);
            Assert.AreEqual(42, _progress.MaxUnlockedLevel.Value);
            Assert.AreEqual(3, _progress.DailyDayIndex.Value);
            Assert.AreEqual(1234567890L, _progress.DailyLastClaimUtcTicks.Value);

            // Check lists
            Assert.IsTrue(_progress.UnlockedWorlds[0]);
            Assert.IsTrue(_progress.UnlockedWorlds[1]);
            Assert.IsTrue(_progress.UnlockedWorlds[2]);
            Assert.IsFalse(_progress.UnlockedWorlds[3]);

            Assert.Contains("classic", _progress.OwnedThemes);
            Assert.Contains("neon", _progress.OwnedThemes);
            Assert.Contains("win_first_level", _progress.Achievements);
        }

        [Test]
        public void CSVLocalizationTableProvider_ParsesCSVHeadersAndRows()
        {
            var provider = new CSVLocalizationTableProvider();

            // Load Turkish table
            bool hasTr = provider.TryGetTable("tr", out var trTable);
            Assert.IsTrue(hasTr);
            Assert.IsNotNull(trTable);

            // Assert Turkish values
            Assert.AreEqual("Oyna", trTable["menu_play"]);
            Assert.AreEqual("Ayarlar", trTable["menu_settings"]);

            // Load Arabic table
            bool hasAr = provider.TryGetTable("ar", out var arTable);
            Assert.IsTrue(hasAr);

            // Assert Arabic values
            Assert.AreEqual("لعب", arTable["menu_play"]);
        }

        [Test]
        public void LocalizationService_CorrectlyIdentifiesRtlAndReversesStrings()
        {
            var provider = new CSVLocalizationTableProvider();
            var service = new LocalizationService(_prefs, provider);

            // Async initialization is simulateable by loading
            service.InitializeAsync(System.Threading.CancellationToken.None);

            // Switch to Arabic
            service.SetLanguage("ar");
            Assert.IsTrue(service.IsRTL);

            // "Play" translation in Arabic is "لعب" (L-A-B).
            // Test format RTL reverses characters:
            // "لعب" has chars: ل (0), ع (1), ب (2)
            // Reversed: ب (2), ع (1), ل (0)
            string translated = service.GetString("menu_play");
            Assert.AreEqual("بعل", translated); // Reversed string for RTL display fallback
        }
    }

    // --- Mock Player Prefs Service ---

    public class MockPlayerPrefsService : IPlayerPrefsService
    {
        private readonly Dictionary<string, object> _db = new();

        public bool HasKey(string key) => _db.ContainsKey(key);
        public void DeleteKey(string key) => _db.Remove(key);
        public void DeleteAll() => _db.Clear();

        public void SetInt(string key, int value) => _db[key] = value;
        public int GetInt(string key, int defaultValue = 0) => _db.TryGetValue(key, out var v) ? (int)v : defaultValue;

        public void SetFloat(string key, float value) => _db[key] = value;
        public float GetFloat(string key, float defaultValue = 0) => _db.TryGetValue(key, out var v) ? (float)v : defaultValue;

        public void SetString(string key, string value) => _db[key] = value;
        public string GetString(string key, string defaultValue = "") => _db.TryGetValue(key, out var v) ? (string)v : defaultValue;

        public void SetBool(string key, bool value) => _db[key] = value;
        public bool GetBool(string key, bool defaultValue = false) => _db.TryGetValue(key, out var v) ? (bool)v : defaultValue;

        public void Save() {}

        public ValueTask InitializeAsync(System.Threading.CancellationToken ct) => default;
        public void OnDispose() {}
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
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
        public void PlayerProgressSaveSystem_UpgradesLegacySchemaOnLoad()
        {
            _prefs.SetInt("RF_SaveSchemaVersion", 1);
            _prefs.SetInt(PlayerProgressModel.KeyCoins, 777);
            _prefs.SetInt(PlayerProgressModel.KeyCurrentLevel, 12);
            _prefs.SetInt(PlayerProgressModel.KeyMaxUnlocked, 12);
            _prefs.SetString(PlayerProgressModel.KeyDailyStamp, "123456789");

            // Intentionally leave worlds/theme lists empty to exercise migration defaults.
            PlayerProgressSaveSystem.Load(_prefs, _progress);

            Assert.AreEqual(777, _progress.Coins.Value);
            Assert.AreEqual(12, _progress.CurrentLevel.Value);
            Assert.AreEqual(12, _progress.MaxUnlockedLevel.Value);
            Assert.AreEqual(2, _prefs.GetInt("RF_SaveSchemaVersion", 0));
            Assert.AreEqual(40, _progress.UnlockedWorlds.Count);
            Assert.IsTrue(_progress.UnlockedWorlds[0]);
        }

        [Test]
        public void DailyRewardService_BlocksClockRollbackAndRapidReplay()
        {
            var service = new DailyRewardService(_progress);
            _progress.DailyDayIndex.Value = 0;
            _progress.DailyLastClaimUtcTicks.Value = DateTime.UtcNow.AddMinutes(-1).Ticks;

            Assert.IsFalse(service.CanClaimNow(out var reason1));
            Assert.AreEqual("too_soon", reason1);

            _progress.DailyLastClaimUtcTicks.Value = DateTime.UtcNow.AddHours(2).Ticks;
            Assert.IsFalse(service.CanClaimNow(out var reason2));
            Assert.AreEqual("clock_rollback", reason2);
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
            if (provider.TryGetTable("ar", out var arTable))
            {
                service.RegisterLanguageTable("ar", arTable);
            }
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

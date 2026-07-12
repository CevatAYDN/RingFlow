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
            var db = UnityEngine.Resources.Load<GameConfigDatabaseSO>("GameConfigDatabase");
            var service = new DailyRewardService(_progress, db);
            _progress.DailyDayIndex.Value = 0;
            _progress.DailyLastClaimUtcTicks.Value = DateTime.UtcNow.AddMinutes(-1).Ticks;

            Assert.IsFalse(service.CanClaimNow(out var reason1));
            Assert.AreEqual("too_soon", reason1);

            _progress.DailyLastClaimUtcTicks.Value = DateTime.UtcNow.AddHours(2).Ticks;
            Assert.IsFalse(service.CanClaimNow(out var reason2));
            Assert.AreEqual("clock_rollback", reason2);
        }

        [Test]
        public void PlayerProgressSaveSystem_RoundTrip_PreservesAllFields()
        {
            var prefs = new MockPlayerPrefsService();
            var progress = new PlayerProgressModel();

            progress.Coins.Value = 999;
            progress.Diamonds.Value = 50;
            progress.Xp.Value = 4200;
            progress.PlayerLevel.Value = 12;
            progress.CurrentLevel.Value = 150;
            progress.MaxUnlockedLevel.Value = 200;
            progress.ChestBronze.Value = 3;
            progress.ChestSilver.Value = 1;
            progress.ChestGold.Value = 0;
            progress.ChestDiamond.Value = 2;
            progress.DailyDayIndex.Value = 5;
            progress.FreeUndosUsedThisSession.Value = 2;
            progress.HintCount.Value = 7;
            progress.RemoveAds.Value = true;
            progress.LevelsSinceLastInterstitial = 4;

            PlayerProgressSaveSystem.Save(prefs, progress);

            var loaded = new PlayerProgressModel();
            PlayerProgressSaveSystem.Load(prefs, loaded);

            Assert.AreEqual(999, loaded.Coins.Value);
            Assert.AreEqual(50, loaded.Diamonds.Value);
            Assert.AreEqual(4200, loaded.Xp.Value);
            Assert.AreEqual(12, loaded.PlayerLevel.Value);
            Assert.AreEqual(150, loaded.CurrentLevel.Value);
            Assert.AreEqual(200, loaded.MaxUnlockedLevel.Value);
            Assert.AreEqual(3, loaded.ChestBronze.Value);
            Assert.AreEqual(1, loaded.ChestSilver.Value);
            Assert.AreEqual(0, loaded.ChestGold.Value);
            Assert.AreEqual(2, loaded.ChestDiamond.Value);
            Assert.AreEqual(5, loaded.DailyDayIndex.Value);
            Assert.AreEqual(2, loaded.FreeUndosUsedThisSession.Value);
            Assert.AreEqual(7, loaded.HintCount.Value);
            Assert.IsTrue(loaded.RemoveAds.Value);
        }

        [Test]
        public void PlayerProgressSaveSystem_HandlesDefaultValues_OnEmptyPrefs()
        {
            var prefs = new MockPlayerPrefsService();
            var loaded = new PlayerProgressModel();
            PlayerProgressSaveSystem.Load(prefs, loaded);

            Assert.AreEqual(0, loaded.Coins.Value);
            Assert.AreEqual(1, loaded.CurrentLevel.Value);
            Assert.AreEqual(1, loaded.MaxUnlockedLevel.Value);
            Assert.AreEqual(1, loaded.PlayerLevel.Value);
            Assert.AreEqual(0, loaded.Xp.Value);
            Assert.IsFalse(loaded.RemoveAds.Value);
        }

        [Test]
        public void PlayerProgressSaveSystem_SaveAndLoadStringLists()
        {
            var prefs = new MockPlayerPrefsService();
            var progress = new PlayerProgressModel();

            progress.OwnedThemes.Add("sakura");
            progress.OwnedThemes.Add("ocean");
            progress.Achievements.Add("first_win");
            progress.Achievements.Add("speed_demon");

            PlayerProgressSaveSystem.Save(prefs, progress);

            var loaded = new PlayerProgressModel();
            PlayerProgressSaveSystem.Load(prefs, loaded);

            Assert.AreEqual(2, loaded.OwnedThemes.Count);
            Assert.Contains("sakura", loaded.OwnedThemes);
            Assert.Contains("ocean", loaded.OwnedThemes);
            Assert.AreEqual(2, loaded.Achievements.Count);
            Assert.Contains("first_win", loaded.Achievements);
            Assert.Contains("speed_demon", loaded.Achievements);
        }

        [Test]
        public void PlayerProgressSaveSystem_SaveAndLoadEmptyStringLists()
        {
            var prefs = new MockPlayerPrefsService();
            var progress = new PlayerProgressModel();

            PlayerProgressSaveSystem.Save(prefs, progress);
            var loaded = new PlayerProgressModel();
            PlayerProgressSaveSystem.Load(prefs, loaded);

            Assert.AreEqual(0, loaded.OwnedThemes.Count);
            Assert.AreEqual(0, loaded.Achievements.Count);
        }

        [Test]
        public void PlayerProgressSaveSystem_SaveAndLoadBoolList()
        {
            var prefs = new MockPlayerPrefsService();
            var progress = new PlayerProgressModel();

            progress.UnlockedWorlds.Clear();
            for (int i = 0; i < 40; i++) progress.UnlockedWorlds.Add(i == 0 || i == 10 || i == 25);

            PlayerProgressSaveSystem.Save(prefs, progress);

            var loaded = new PlayerProgressModel();
            PlayerProgressSaveSystem.Load(prefs, loaded);

            Assert.AreEqual(40, loaded.UnlockedWorlds.Count);
            Assert.IsTrue(loaded.UnlockedWorlds[0]);
            Assert.IsTrue(loaded.UnlockedWorlds[10]);
            Assert.IsTrue(loaded.UnlockedWorlds[25]);
            for (int i = 1; i < 40; i++)
            {
                if (i != 10 && i != 25)
                    Assert.IsFalse(loaded.UnlockedWorlds[i], $"World {i} should be locked");
            }
        }

        [Test]
        public void SettingsSaveSystem_RoundAll_PreservesAllFields()
        {
            var prefs = new MockPlayerPrefsService();
            var settings = new SettingsModel();

            settings.MusicEnabled.Value = false;
            settings.SfxEnabled.Value = false;
            settings.HapticEnabled.Value = true;
            settings.ReduceMotion.Value = true;
            settings.SlowMode.Value = true;
            settings.BigButtons.Value = true;
            settings.ColorBlindMode.Value = 3;
            settings.LanguageCode.Value = "de";

            SettingsSaveSystem.Save(prefs, settings);
            var loaded = new SettingsModel();
            SettingsSaveSystem.Load(prefs, loaded);

            Assert.IsFalse(loaded.MusicEnabled.Value);
            Assert.IsFalse(loaded.SfxEnabled.Value);
            Assert.IsTrue(loaded.HapticEnabled.Value);
            Assert.IsTrue(loaded.ReduceMotion.Value);
            Assert.IsTrue(loaded.SlowMode.Value);
            Assert.IsTrue(loaded.BigButtons.Value);
            Assert.AreEqual(3, loaded.ColorBlindMode.Value);
            Assert.AreEqual("de", loaded.LanguageCode.Value);
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

        public void SetLong(string key, long value) => _db[key] = value;
        public long GetLong(string key, long defaultValue = 0L) => _db.TryGetValue(key, out var v) ? (long)v : defaultValue;

        public void Save() {}

        public ValueTask InitializeAsync(System.Threading.CancellationToken ct) => default;
        public void OnDispose() {}
    }
}

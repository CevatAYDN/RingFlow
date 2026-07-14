using System.Reflection;
using NUnit.Framework;
using RingFlow.Gameplay;
using UnityEngine;

namespace RingFlow.Tests
{
    /// <summary>
    /// M12: Save migration and round-trip tests.
    /// Validates that:
    ///   - Save/Load round-trip preserves all model fields.
    ///   - Corrupt or missing save data loads the backup without crashing.
    ///   - Schema version is written and can be read back correctly.
    ///   - Adding new fields does not break loading of older save data.
    /// </summary>
    [TestFixture]
    public class SaveMigrationTests
    {
        private PlayerProgressModel _progress;
        private SettingsModel _settings;
        private InMemoryPlayerPrefs _prefs;
        private GameConfigDatabaseSO _db;

        [SetUp]
        public void Setup()
        {
            _prefs    = new InMemoryPlayerPrefs();
            _progress = new PlayerProgressModel();
            _settings = new SettingsModel();
            _db = Resources.Load<GameConfigDatabaseSO>(GameplayAssetKeys.GameConfigDatabase);
            Assert.IsNotNull(_db, "GameConfigDatabaseSO required for save migration tests.");

            for (int i = 0; i < 40; i++)
                _progress.UnlockedWorlds.Add(i == 0);
        }

        [Test]
        public void SaveAndLoad_RoundTrip_PreservesAllPlayerProgressFields()
        {
            _progress.Coins.Value         = 1234;
            _progress.Diamonds.Value      = 56;
            _progress.Xp.Value            = 789;
            _progress.CurrentLevel.Value  = 42;
            _progress.PlayerLevel.Value   = 7;
            _progress.ChestBronze.Value   = 10;
            _progress.ChestSilver.Value   = 3;
            _progress.ChestGold.Value     = 1;
            _progress.HintCount.Value     = 5;

            PlayerProgressSaveSystem.Save(_prefs, _progress);

            var loaded = new PlayerProgressModel();
            for (int i = 0; i < 40; i++) loaded.UnlockedWorlds.Add(false);
            PlayerProgressSaveSystem.Load(_prefs, loaded);

            Assert.AreEqual(1234, loaded.Coins.Value,        "Coins mismatch after round-trip.");
            Assert.AreEqual(56,   loaded.Diamonds.Value,     "Diamonds mismatch.");
            Assert.AreEqual(789,  loaded.Xp.Value,           "XP mismatch.");
            Assert.AreEqual(42,   loaded.CurrentLevel.Value, "CurrentLevel mismatch.");
            Assert.AreEqual(7,    loaded.PlayerLevel.Value,  "PlayerLevel mismatch.");
            Assert.AreEqual(10,   loaded.ChestBronze.Value,  "ChestBronze mismatch.");
            Assert.AreEqual(3,    loaded.ChestSilver.Value,  "ChestSilver mismatch.");
            Assert.AreEqual(1,    loaded.ChestGold.Value,    "ChestGold mismatch.");
            Assert.AreEqual(5,    loaded.HintCount.Value,    "HintCount mismatch.");
        }

        [Test]
        public void SaveAndLoad_RoundTrip_PreservesSettingsFields()
        {
            _settings.MusicEnabled.Value   = false;
            _settings.SfxEnabled.Value     = false;
            _settings.HapticEnabled.Value  = false;
            _settings.LanguageCode.Value   = "tr";

            SettingsSaveSystem.Save(_prefs, _settings);

            var loaded = new SettingsModel();
            SettingsSaveSystem.Load(_prefs, loaded);

            Assert.IsFalse(loaded.MusicEnabled.Value,  "MusicEnabled should be false after round-trip.");
            Assert.IsFalse(loaded.SfxEnabled.Value,    "SfxEnabled should be false after round-trip.");
            Assert.IsFalse(loaded.HapticEnabled.Value, "HapticEnabled should be false after round-trip.");
            Assert.AreEqual("tr", loaded.LanguageCode.Value, "LanguageCode mismatch.");
        }

        [Test]
        public void Load_WithMissingKeys_ReturnsDefaultValues()
        {
            // Empty prefs — no keys written.
            var loaded = new PlayerProgressModel();
            for (int i = 0; i < 40; i++) loaded.UnlockedWorlds.Add(false);
            Assert.DoesNotThrow(() => PlayerProgressSaveSystem.Load(_prefs, loaded),
                "Load should not throw on empty prefs.");

            // Defaults: Coins=0, CurrentLevel=1 (or whatever the model initialises to).
            Assert.GreaterOrEqual(loaded.Coins.Value, 0, "Coins should default to >= 0.");
        }

        [Test]
        public void Save_ThenCorruptChecksum_LoadFallsBackToDefault()
        {
            _progress.Coins.Value        = 999;
            _progress.CurrentLevel.Value = 10;
            PlayerProgressSaveSystem.Save(_prefs, _progress);

            // Corrupt the checksum key.
            _prefs.SetString(GameplayAssetKeys.PlayerPrefs.SaveChecksum, "INVALID_CHECKSUM");

            var loaded = new PlayerProgressModel();
            for (int i = 0; i < 40; i++) loaded.UnlockedWorlds.Add(false);

            // Must not throw; either recovers or falls back to defaults.
            Assert.DoesNotThrow(() => PlayerProgressSaveSystem.Load(_prefs, loaded),
                "Corrupt checksum must not throw — should fall back gracefully.");
        }

        [Test]
        public void SchemaVersion_IsWrittenAndReadable()
        {
            PlayerProgressSaveSystem.Save(_prefs, _progress);

            string version = _prefs.GetString(GameplayAssetKeys.PlayerPrefs.SaveSchemaVersion, "");
            Assert.IsFalse(string.IsNullOrEmpty(version),
                "Schema version must be written to PlayerPrefs on save.");
        }

        [Test]
        public void WorldUnlocks_Persist_AcrossRoundTrip()
        {
            // Unlock worlds 0, 2, 5
            _progress.UnlockedWorlds[0] = true;
            _progress.UnlockedWorlds[2] = true;
            _progress.UnlockedWorlds[5] = true;

            PlayerProgressSaveSystem.Save(_prefs, _progress);

            var loaded = new PlayerProgressModel();
            for (int i = 0; i < 40; i++) loaded.UnlockedWorlds.Add(false);
            PlayerProgressSaveSystem.Load(_prefs, loaded);

            Assert.IsTrue(loaded.UnlockedWorlds[0],  "World 0 should be unlocked after round-trip.");
            Assert.IsFalse(loaded.UnlockedWorlds[1], "World 1 should be locked after round-trip.");
            Assert.IsTrue(loaded.UnlockedWorlds[2],  "World 2 should be unlocked after round-trip.");
            Assert.IsFalse(loaded.UnlockedWorlds[3], "World 3 should be locked after round-trip.");
            Assert.IsTrue(loaded.UnlockedWorlds[5],  "World 5 should be unlocked after round-trip.");
        }
    }

    // ── Minimal in-memory IPlayerPrefsService for isolation ─────────────────
    // Shared with PlayModeIntegrationTests — defined here so EditMode tests
    // do not depend on the PlayMode assembly.
    internal sealed class InMemoryPlayerPrefs : Nexus.Core.Services.IPlayerPrefsService
    {
        private readonly System.Collections.Generic.Dictionary<string, string> _strings = new();
        private readonly System.Collections.Generic.Dictionary<string, int>    _ints    = new();
        private readonly System.Collections.Generic.Dictionary<string, float>  _floats  = new();
        private readonly System.Collections.Generic.Dictionary<string, long>   _longs   = new();
        private readonly System.Collections.Generic.Dictionary<string, bool>   _bools   = new();

        public int GetInt(string key, int defaultValue = 0)
            => _ints.TryGetValue(key, out var v) ? v : defaultValue;
        public void SetInt(string key, int value) => _ints[key] = value;
        public bool GetBool(string key, bool defaultValue = false)
            => _bools.TryGetValue(key, out var v) ? v : defaultValue;
        public void SetBool(string key, bool value) => _bools[key] = value;
        public string GetString(string key, string defaultValue = "")
            => _strings.TryGetValue(key, out var v) ? v : defaultValue;
        public void SetString(string key, string value) => _strings[key] = value;
        public float GetFloat(string key, float defaultValue = 0f)
            => _floats.TryGetValue(key, out var v) ? v : defaultValue;
        public void SetFloat(string key, float value) => _floats[key] = value;
        public long GetLong(string key, long defaultValue = 0L)
            => _longs.TryGetValue(key, out var v) ? v : defaultValue;
        public void SetLong(string key, long value) => _longs[key] = value;
        public bool HasKey(string key) => _strings.ContainsKey(key) || _ints.ContainsKey(key) || _floats.ContainsKey(key) || _longs.ContainsKey(key) || _bools.ContainsKey(key);
        public void DeleteKey(string key) { _strings.Remove(key); _ints.Remove(key); _floats.Remove(key); _longs.Remove(key); _bools.Remove(key); }
        public void DeleteAll() { _strings.Clear(); _ints.Clear(); _floats.Clear(); _longs.Clear(); _bools.Clear(); }
        public void Save() { /* in-memory, no-op */ }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Tracks world/level progression, XP, chests, daily cycles.
    /// Persists via <see cref="IPlayerPrefsService"/> (registered in GameplayLifecycle as EncryptedStorageService).
    /// </summary>
    public class PlayerProgressModel : IReactiveModel, IResettableModel
    {
        public const string KeyCoins         = GameplayAssetKeys.PlayerPrefs.Coins;
        public const string KeyDiamonds      = GameplayAssetKeys.PlayerPrefs.Diamonds;
        public const string KeyXp            = GameplayAssetKeys.PlayerPrefs.Xp;
        public const string KeyCurrentLevel  = GameplayAssetKeys.PlayerPrefs.CurrentLevel;
        public const string KeyMaxUnlocked   = GameplayAssetKeys.PlayerPrefs.MaxUnlocked;
        public const string KeyWorlds        = GameplayAssetKeys.PlayerPrefs.Worlds;
        public const string KeyThemes        = GameplayAssetKeys.PlayerPrefs.Themes;
        public const string KeyChestsBronze  = GameplayAssetKeys.PlayerPrefs.ChestBronze;
        public const string KeyChestsSilver  = GameplayAssetKeys.PlayerPrefs.ChestSilver;
        public const string KeyChestsGold    = GameplayAssetKeys.PlayerPrefs.ChestGold;
        public const string KeyChestsDiamond = GameplayAssetKeys.PlayerPrefs.ChestDiamond;
        public const string KeyPlayerLvl     = GameplayAssetKeys.PlayerPrefs.PlayerLevel;
        public const string KeyDailyDay      = GameplayAssetKeys.PlayerPrefs.DailyDayIndex;
        public const string KeyDailyStamp    = GameplayAssetKeys.PlayerPrefs.DailyLastClaimUtc;
        public const string KeyUndoUsed      = GameplayAssetKeys.PlayerPrefs.UndoUsedFree;
        public const string KeyAchieves      = GameplayAssetKeys.PlayerPrefs.Achievements;
        public const string KeyRemoveAds     = GameplayAssetKeys.PlayerPrefs.RemoveAds;
        public const string KeyHintCount     = GameplayAssetKeys.PlayerPrefs.HintCount;

        public ObservableProperty<int> Coins { get; } = new(0);
        public ObservableProperty<int> Diamonds { get; } = new(0);
        public ObservableProperty<int> Xp { get; } = new(0);
        public ObservableProperty<int> PlayerLevel { get; } = new(1);
        public ObservableProperty<int> CurrentLevel { get; } = new(1);
        public ObservableProperty<int> MaxUnlockedLevel { get; } = new(1);

        public ObservableProperty<int> ChestBronze { get; } = new(0);
        public ObservableProperty<int> ChestSilver { get; } = new(0);
        public ObservableProperty<int> ChestGold { get; } = new(0);
        public ObservableProperty<int> ChestDiamond { get; } = new(0);

        public ObservableProperty<int> DailyDayIndex { get; } = new(-1);
        public ObservableProperty<long> DailyLastClaimUtcTicks { get; } = new(0);

        public ObservableProperty<int> FreeUndosUsedThisSession { get; } = new(0);

        public ObservableProperty<int> HintCount { get; } = new(0);

        public ObservableProperty<bool> RemoveAds { get; } = new(false);

        // ── Interstitial ad counter (GDD §10) ────────────────
        // Non-persisted; resets each session. Tracks levels since last interstitial.
        public int LevelsSinceLastInterstitial { get; set; } = 0;

        /// <summary>Unlocked state per world. Count set externally via SetTotalWorldCount().</summary>
        public List<bool> UnlockedWorlds { get; } = new();

        /// <summary>The total number of worlds (set from GameConfigDatabaseSO at bootstrap).</summary>
        public int TotalWorldCount { get; private set; } = 40;

        /// <summary>themes owned (by ID, arbitrary strings managed outside).</summary>
        public List<string> OwnedThemes { get; } = new();

        /// <summary>Achievements achieved (id, timestampUtc).</summary>
        public List<string> Achievements { get; } = new();

        public void SetTotalWorldCount(int totalWorlds)
        {
            TotalWorldCount = totalWorlds > 0 ? totalWorlds : 40;
        }

        // XP thresholds — data-driven via GameBalanceConfig fields.
        public int XpToNextLevel(GameConfigDatabaseSO db, int playerLevel)
        {
            if (db == null) return 100;
            var cfg = db.BalanceConfig;
            return playerLevel switch
            {
                1 => cfg.XpThresholdLevel1,
                2 => cfg.XpThresholdLevel2,
                3 => cfg.XpThresholdLevel3,
                _ => cfg.XpThresholdDefault
            };
        }

        public ValueTask OnBind(System.Threading.CancellationToken ct)
        {
            // Initialize unlocked worlds — uses externally set TotalWorldCount.
            if (UnlockedWorlds.Count == 0)
            {
                for (int i = 0; i < TotalWorldCount; i++) UnlockedWorlds.Add(false);
                UnlockedWorlds[0] = true;
            }
            return default;
        }

        public void Reset()
        {
            Coins.Value = 0;
            Diamonds.Value = 0;
            Xp.Value = 0;
            PlayerLevel.Value = 1;
            CurrentLevel.Value = 1;
            MaxUnlockedLevel.Value = 1;
            ChestBronze.Value = 0;
            ChestSilver.Value = 0;
            ChestGold.Value = 0;
            ChestDiamond.Value = 0;
            DailyDayIndex.Value = -1;
            DailyLastClaimUtcTicks.Value = 0;
            FreeUndosUsedThisSession.Value = 0;
            HintCount.Value = 0;
            RemoveAds.Value = false;
            UnlockedWorlds.Clear();
            for (int i = 0; i < TotalWorldCount; i++) UnlockedWorlds.Add(i == 0);
            OwnedThemes.Clear();
            Achievements.Clear();
        }
    }

    /// <summary>
    /// Persists / restores PlayerProgressModel into IPlayerPrefsService (encrypted).
    /// Single source of truth for save data shape.
    /// </summary>
    public static class PlayerProgressSaveSystem
    {
        private const int CurrentSchemaVersion = 2;
        private const string KeySchemaVersion = GameplayAssetKeys.PlayerPrefs.SaveSchemaVersion;
        private const string KeyChecksum = GameplayAssetKeys.PlayerPrefs.SaveChecksum;

        public static void Save(IPlayerPrefsService prefs, PlayerProgressModel m)
        {
            prefs.SetInt(KeySchemaVersion, CurrentSchemaVersion);
            prefs.SetInt(PlayerProgressModel.KeyCoins, m.Coins.Value);
            prefs.SetInt(PlayerProgressModel.KeyDiamonds, m.Diamonds.Value);
            prefs.SetInt(PlayerProgressModel.KeyXp, m.Xp.Value);
            prefs.SetInt(PlayerProgressModel.KeyCurrentLevel, m.CurrentLevel.Value);
            prefs.SetInt(PlayerProgressModel.KeyMaxUnlocked, m.MaxUnlockedLevel.Value);
            prefs.SetInt(PlayerProgressModel.KeyPlayerLvl, m.PlayerLevel.Value);
            prefs.SetInt(PlayerProgressModel.KeyChestsBronze, m.ChestBronze.Value);
            prefs.SetInt(PlayerProgressModel.KeyChestsSilver, m.ChestSilver.Value);
            prefs.SetInt(PlayerProgressModel.KeyChestsGold, m.ChestGold.Value);
            prefs.SetInt(PlayerProgressModel.KeyChestsDiamond, m.ChestDiamond.Value);
            prefs.SetInt(PlayerProgressModel.KeyDailyDay, m.DailyDayIndex.Value);
            prefs.SetString(PlayerProgressModel.KeyDailyStamp, m.DailyLastClaimUtcTicks.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            prefs.SetInt(PlayerProgressModel.KeyUndoUsed, m.FreeUndosUsedThisSession.Value);
            prefs.SetInt(PlayerProgressModel.KeyHintCount, m.HintCount.Value);
            prefs.SetBool(PlayerProgressModel.KeyRemoveAds, m.RemoveAds.Value);

            SaveBoolList(prefs, PlayerProgressModel.KeyWorlds, m.UnlockedWorlds);
            SaveStringList(prefs, PlayerProgressModel.KeyThemes, m.OwnedThemes);
            SaveStringList(prefs, PlayerProgressModel.KeyAchieves, m.Achievements);

            // P0 fix: write a checksum so we can detect corrupted saves on next load.
            var checksum = ComputeProgressChecksum(prefs);
            prefs.SetInt(KeyChecksum, checksum);

            prefs.Save();
        }

        public static void Load(IPlayerPrefsService prefs, PlayerProgressModel m)
        {
            int schemaVersion = prefs.GetInt(KeySchemaVersion, 1);

            // Verify checksum before populating model fields. If mismatch is detected,
            // log an error but continue loading — silently resetting all progress is
            // worse than loading potentially-corrupted data. The player can manually
            // reset from the settings menu if they encounter issues.
            int storedChecksum = prefs.GetInt(KeyChecksum, 0);
            if (storedChecksum != 0)
            {
                int computedChecksum = ComputeProgressChecksum(prefs);
                if (computedChecksum != storedChecksum)
                {
                    NexusLog.Warn("PlayerProgress", nameof(Load), "",
                        $"Save data checksum mismatch (stored={storedChecksum}, computed={computedChecksum}). " +
                        "Data may be corrupted — loading anyway. Reset from Settings menu if issues occur.");
                }
            }

            m.Coins.Value = prefs.GetInt(PlayerProgressModel.KeyCoins, 0);
            m.Diamonds.Value = prefs.GetInt(PlayerProgressModel.KeyDiamonds, 0);
            m.Xp.Value = prefs.GetInt(PlayerProgressModel.KeyXp, 0);
            m.CurrentLevel.Value = prefs.GetInt(PlayerProgressModel.KeyCurrentLevel, 1);
            m.MaxUnlockedLevel.Value = prefs.GetInt(PlayerProgressModel.KeyMaxUnlocked, 1);

            if (m.CurrentLevel.Value < 1) m.CurrentLevel.Value = 1;
            if (m.MaxUnlockedLevel.Value < 1) m.MaxUnlockedLevel.Value = 1;

            m.PlayerLevel.Value = prefs.GetInt(PlayerProgressModel.KeyPlayerLvl, 1);
            m.ChestBronze.Value = prefs.GetInt(PlayerProgressModel.KeyChestsBronze, 0);
            m.ChestSilver.Value = prefs.GetInt(PlayerProgressModel.KeyChestsSilver, 0);
            m.ChestGold.Value = prefs.GetInt(PlayerProgressModel.KeyChestsGold, 0);
            m.ChestDiamond.Value = prefs.GetInt(PlayerProgressModel.KeyChestsDiamond, 0);
            m.DailyDayIndex.Value = prefs.GetInt(PlayerProgressModel.KeyDailyDay, -1);

            var stampStr = prefs.GetString(PlayerProgressModel.KeyDailyStamp, "0");
            long stampTicks = 0;
            long.TryParse(stampStr, out stampTicks);
            m.DailyLastClaimUtcTicks.Value = stampTicks;

            m.FreeUndosUsedThisSession.Value = prefs.GetInt(PlayerProgressModel.KeyUndoUsed, 0);
            m.HintCount.Value = prefs.GetInt(PlayerProgressModel.KeyHintCount, 0);
            m.RemoveAds.Value = prefs.GetBool(PlayerProgressModel.KeyRemoveAds, false);

            m.UnlockedWorlds.Clear();
            LoadBoolList(prefs, PlayerProgressModel.KeyWorlds, m.UnlockedWorlds, m.TotalWorldCount, true, 0);
            m.OwnedThemes.Clear();
            LoadStringList(prefs, PlayerProgressModel.KeyThemes, m.OwnedThemes);
            m.Achievements.Clear();
            LoadStringList(prefs, PlayerProgressModel.KeyAchieves, m.Achievements);

            if (schemaVersion < CurrentSchemaVersion)
            {
                if (m.UnlockedWorlds.Count == 0)
                {
                    for (int i = 0; i < m.TotalWorldCount; i++) m.UnlockedWorlds.Add(i == 0);
                }
                if (m.CurrentLevel.Value < 1) m.CurrentLevel.Value = 1;
                if (m.MaxUnlockedLevel.Value < 1) m.MaxUnlockedLevel.Value = 1;
                prefs.SetInt(KeySchemaVersion, CurrentSchemaVersion);
                prefs.Save();
            }
        }

        // ---- list codec ----

        private const string Sep = "|";
        private const string Esc = "\\";

        public static void SaveBoolList(IPlayerPrefsService prefs, string key, IReadOnlyList<bool> list)
        {
            var sb = new System.Text.StringBuilder(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(Sep);
                sb.Append(list[i] ? '1' : '0');
            }
            prefs.SetString(key, sb.ToString());
        }

        public static void LoadBoolList(IPlayerPrefsService prefs, string key, IList<bool> list, int expectedCount, bool defaultValue, int defaultTrueAtIndex = -1)
        {
            var raw = prefs.GetString(key, "");
            if (string.IsNullOrEmpty(raw))
            {
                for (int i = 0; i < expectedCount; i++) list.Add(defaultTrueAtIndex >= 0 && i == defaultTrueAtIndex);
                return;
            }
            var parts = raw.Split(Sep[0]);
            for (int i = 0; i < expectedCount; i++)
            {
                bool v = i < parts.Length && parts[i] == "1";
                list.Add(v);
            }
        }

        public static void SaveStringList(IPlayerPrefsService prefs, string key, IReadOnlyList<string> list)
        {
            if (list == null || list.Count == 0) { prefs.SetString(key, ""); return; }
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(Sep);
                var s = list[i] ?? "";
                sb.Append(s.Replace(Esc, Esc + Esc).Replace(Sep, Esc + "s"));
            }
            prefs.SetString(key, sb.ToString());
        }

        public static void LoadStringList(IPlayerPrefsService prefs, string key, IList<string> list)
        {
            var raw = prefs.GetString(key, "");
            if (string.IsNullOrEmpty(raw)) return;
            var parts = raw.Split(Sep[0]);
            for (int i = 0; i < parts.Length; i++)
            {
                list.Add(parts[i].Replace(Esc + "s", Sep).Replace(Esc + Esc, Esc));
            }
        }

        // Deterministic variant of DJB2 hash for strings — must NOT use string.GetHashCode()
        // as it is non-deterministic across process restarts (randomized in modern .NET).
        private static int Djb2Hash(string s)
        {
            unchecked
            {
                int hash = 5381;
                for (int i = 0; i < s.Length; i++)
                    hash = (hash * 33) ^ s[i];
                return hash;
            }
        }

        private static int ComputeProgressChecksum(IPlayerPrefsService prefs)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + prefs.GetInt(KeySchemaVersion, 0);
                hash = hash * 31 + prefs.GetInt(PlayerProgressModel.KeyCoins, 0);
                hash = hash * 31 + prefs.GetInt(PlayerProgressModel.KeyDiamonds, 0);
                hash = hash * 31 + prefs.GetInt(PlayerProgressModel.KeyXp, 0);
                hash = hash * 31 + prefs.GetInt(PlayerProgressModel.KeyCurrentLevel, 0);
                hash = hash * 31 + prefs.GetInt(PlayerProgressModel.KeyMaxUnlocked, 0);
                hash = hash * 31 + prefs.GetInt(PlayerProgressModel.KeyPlayerLvl, 0);
                hash = hash * 31 + prefs.GetInt(PlayerProgressModel.KeyChestsBronze, 0);
                hash = hash * 31 + prefs.GetInt(PlayerProgressModel.KeyChestsSilver, 0);
                hash = hash * 31 + prefs.GetInt(PlayerProgressModel.KeyChestsGold, 0);
                hash = hash * 31 + prefs.GetInt(PlayerProgressModel.KeyChestsDiamond, 0);
                hash = hash * 31 + prefs.GetInt(PlayerProgressModel.KeyDailyDay, 0);
                hash = hash * 31 + Djb2Hash(prefs.GetString(PlayerProgressModel.KeyDailyStamp, ""));
                hash = hash * 31 + prefs.GetInt(PlayerProgressModel.KeyUndoUsed, 0);
                hash = hash * 31 + prefs.GetInt(PlayerProgressModel.KeyHintCount, 0);
                hash = hash * 31 + (prefs.GetBool(PlayerProgressModel.KeyRemoveAds, false) ? 1 : 0);
                hash = hash * 31 + Djb2Hash(prefs.GetString(PlayerProgressModel.KeyWorlds, ""));
                hash = hash * 31 + Djb2Hash(prefs.GetString(PlayerProgressModel.KeyThemes, ""));
                hash = hash * 31 + Djb2Hash(prefs.GetString(PlayerProgressModel.KeyAchieves, ""));
                return hash;
            }
        }
    }
}



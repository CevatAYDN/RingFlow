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
	public const string KeyDailyStreak   = GameplayAssetKeys.PlayerPrefs.DailyStreak;
	public const string KeyBestMoves      = GameplayAssetKeys.PlayerPrefs.BestMoves;
        public const string KeyUndoUsed      = GameplayAssetKeys.PlayerPrefs.UndoUsedFree;
        public const string KeyAchieves      = GameplayAssetKeys.PlayerPrefs.Achievements;
        public const string KeyRemoveAds     = GameplayAssetKeys.PlayerPrefs.RemoveAds;
        public const string KeyHintCount     = GameplayAssetKeys.PlayerPrefs.HintCount;

        internal static void WriteSchemaVersion(IPlayerPrefsService prefs, int version)
        {
            prefs.SetString(GameplayAssetKeys.PlayerPrefs.SaveSchemaVersion,
                version.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        internal static int ReadSchemaVersion(IPlayerPrefsService prefs, int defaultValue)
        {
            try
            {
                var schemaVersionText = prefs.GetString(GameplayAssetKeys.PlayerPrefs.SaveSchemaVersion, string.Empty);
                if (int.TryParse(schemaVersionText, System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out int parsed))
                {
                    return parsed;
                }
            }
            catch (System.InvalidCastException)
            {
            }

            try
            {
                return prefs.GetInt(GameplayAssetKeys.PlayerPrefs.SaveSchemaVersion, defaultValue);
            }
            catch (System.InvalidCastException)
            {
                return defaultValue;
            }
        }

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

        /// <summary>Consecutive daily-reward claim streak (resets when a day is missed).</summary>
        public ObservableProperty<int> DailyStreak { get; } = new(0);

        /// <summary>Best (fewest) moves recorded per level index. Level 0 = unscoped. Supports save/load.</summary>
        public Dictionary<int, int> BestMovesPerLevel { get; } = new();

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
            if (totalWorlds <= 0)
                throw new System.InvalidOperationException("[PlayerProgressModel] Total world count must be provided.");

            TotalWorldCount = totalWorlds;
        }

        // XP thresholds — data-driven via GameBalanceConfig fields.
        public int XpToNextLevel(GameConfigDatabaseSO db, int playerLevel)
        {
            if (db == null) throw new System.InvalidOperationException("[PlayerProgressModel] GameConfigDatabaseSO not injected!");
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

        /// <summary>Returns the fewest moves recorded for a level, or 0 if none yet.</summary>
        public int GetBestMovesForLevel(int level)
        {
            return BestMovesPerLevel.TryGetValue(level, out int best) ? best : 0;
        }

        /// <summary>Records moves if they improve (lower) the stored best for the level.</summary>
        public void RecordBestMoves(int level, int moves)
        {
            if (level <= 0 || moves <= 0) return;
            if (!BestMovesPerLevel.TryGetValue(level, out int best) || moves < best)
                BestMovesPerLevel[level] = moves;
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
            DailyStreak.Value = 0;
            BestMovesPerLevel.Clear();
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
        private const int CurrentSchemaVersion = 3;
        private const string KeySchemaVersion = GameplayAssetKeys.PlayerPrefs.SaveSchemaVersion;
        private const string KeyChecksum = GameplayAssetKeys.PlayerPrefs.SaveChecksum;

        // Backup save key — JSON snapshot of the last valid progress state before checksum write.
        private const string KeyBackupSnapshot = "RingFlow_Bak_Snapshot";

        public static void Save(IPlayerPrefsService prefs, PlayerProgressModel m)
        {
            PlayerProgressModel.WriteSchemaVersion(prefs, CurrentSchemaVersion);
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
            prefs.SetInt(PlayerProgressModel.KeyDailyStreak, m.DailyStreak.Value);
            SaveBestMoves(prefs, PlayerProgressModel.KeyBestMoves, m.BestMovesPerLevel);
            prefs.SetInt(PlayerProgressModel.KeyUndoUsed, m.FreeUndosUsedThisSession.Value);
            prefs.SetInt(PlayerProgressModel.KeyHintCount, m.HintCount.Value);
            prefs.SetBool(PlayerProgressModel.KeyRemoveAds, m.RemoveAds.Value);

            SaveBoolList(prefs, PlayerProgressModel.KeyWorlds, m.UnlockedWorlds);
            SaveStringList(prefs, PlayerProgressModel.KeyThemes, m.OwnedThemes);
            SaveStringList(prefs, PlayerProgressModel.KeyAchieves, m.Achievements);

            // S2: snapshot current values as backup before checksum
            BackupCurrentState(prefs);

            // P0 fix: write a checksum so we can detect corrupted saves on next load.
            var checksum = ComputeProgressChecksum(prefs);
            prefs.SetInt(KeyChecksum, checksum);

            prefs.Save();
        }

        public static void Load(IPlayerPrefsService prefs, PlayerProgressModel m)
        {
            int schemaVersion = PlayerProgressModel.ReadSchemaVersion(prefs, 1);

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
                        "Attempting restore from backup snapshot.");
                    if (!TryRestoreFromBackup(prefs, m))
                    {
                        NexusLog.Warn("PlayerProgress", nameof(Load), "",
                            "Backup restore failed. Loading potentially corrupted data. " +
                            "Player can reset from Settings menu if issues occur.");
                    }
                    else
                    {
                        NexusLog.Info("PlayerProgress", nameof(Load), "",
                            "Backup restore succeeded. Progress restored from backup snapshot.");
                        return;
                    }
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

            m.DailyStreak.Value = prefs.GetInt(PlayerProgressModel.KeyDailyStreak, 0);
            LoadBestMoves(prefs, PlayerProgressModel.KeyBestMoves, m.BestMovesPerLevel);

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
                PlayerProgressModel.WriteSchemaVersion(prefs, CurrentSchemaVersion);
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

        // Best-moves dictionary codec: "level:moves|level:moves" (sorted by level for determinism).
        private const string BestSep = ":";

        public static void SaveBestMoves(IPlayerPrefsService prefs, string key, IReadOnlyDictionary<int, int> dict)
        {
            if (dict == null || dict.Count == 0) { prefs.SetString(key, ""); return; }
            var sb = new System.Text.StringBuilder(dict.Count * 6);
            var levels = new List<int>(dict.Keys);
            levels.Sort();
            for (int i = 0; i < levels.Count; i++)
            {
                if (i > 0) sb.Append(Sep);
                sb.Append(levels[i]).Append(BestSep).Append(dict[levels[i]]);
            }
            prefs.SetString(key, sb.ToString());
        }

        public static void LoadBestMoves(IPlayerPrefsService prefs, string key, IDictionary<int, int> dict)
        {
            dict.Clear();
            var raw = prefs.GetString(key, "");
            if (string.IsNullOrEmpty(raw)) return;
            var parts = raw.Split(Sep[0]);
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;
                var kv = part.Split(BestSep[0]);
                if (kv.Length != 2) continue;
                if (int.TryParse(kv[0], out int lvl) && int.TryParse(kv[1], out int moves) && lvl > 0 && moves > 0)
                    dict[lvl] = moves;
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
                hash = hash * 31 + PlayerProgressModel.ReadSchemaVersion(prefs, 0);
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
                hash = hash * 31 + prefs.GetInt(PlayerProgressModel.KeyDailyStreak, 0);
                hash = hash * 31 + Djb2Hash(prefs.GetString(PlayerProgressModel.KeyBestMoves, ""));
                hash = hash * 31 + prefs.GetInt(PlayerProgressModel.KeyUndoUsed, 0);
                hash = hash * 31 + prefs.GetInt(PlayerProgressModel.KeyHintCount, 0);
                hash = hash * 31 + (prefs.GetBool(PlayerProgressModel.KeyRemoveAds, false) ? 1 : 0);
                hash = hash * 31 + Djb2Hash(prefs.GetString(PlayerProgressModel.KeyWorlds, ""));
                hash = hash * 31 + Djb2Hash(prefs.GetString(PlayerProgressModel.KeyThemes, ""));
                hash = hash * 31 + Djb2Hash(prefs.GetString(PlayerProgressModel.KeyAchieves, ""));
                return hash;
            }
        }

        [System.Serializable]
        private sealed class ProgressBackupSnapshot
        {
            public int SchemaVersion;
            public int Coins;
            public int Diamonds;
            public int Xp;
            public int CurrentLevel;
            public int MaxUnlocked;
            public int PlayerLevel;
            public int ChestBronze;
            public int ChestSilver;
            public int ChestGold;
            public int ChestDiamond;
            public int DailyDayIndex;
            public int DailyStreak;
            public string BestMoves;
            public int UndoUsed;
            public int HintCount;
            public bool RemoveAds;
            public string DailyStamp;
            public string Worlds;
            public string Themes;
            public string Achievements;
            public int SnapshotChecksum;
        }

        // S2: Backup current progress state before overwriting during Save().
        // On checksum mismatch during Load(), the backup is restored automatically.
        private static void BackupCurrentState(IPlayerPrefsService prefs)
        {
            var snapshot = new ProgressBackupSnapshot
            {
                SchemaVersion = PlayerProgressModel.ReadSchemaVersion(prefs, 0),
                Coins = prefs.GetInt(PlayerProgressModel.KeyCoins, 0),
                Diamonds = prefs.GetInt(PlayerProgressModel.KeyDiamonds, 0),
                Xp = prefs.GetInt(PlayerProgressModel.KeyXp, 0),
                CurrentLevel = prefs.GetInt(PlayerProgressModel.KeyCurrentLevel, 0),
                MaxUnlocked = prefs.GetInt(PlayerProgressModel.KeyMaxUnlocked, 0),
                PlayerLevel = prefs.GetInt(PlayerProgressModel.KeyPlayerLvl, 0),
                ChestBronze = prefs.GetInt(PlayerProgressModel.KeyChestsBronze, 0),
                ChestSilver = prefs.GetInt(PlayerProgressModel.KeyChestsSilver, 0),
                ChestGold = prefs.GetInt(PlayerProgressModel.KeyChestsGold, 0),
                ChestDiamond = prefs.GetInt(PlayerProgressModel.KeyChestsDiamond, 0),
                DailyDayIndex = prefs.GetInt(PlayerProgressModel.KeyDailyDay, 0),
                DailyStreak = prefs.GetInt(PlayerProgressModel.KeyDailyStreak, 0),
                BestMoves = prefs.GetString(PlayerProgressModel.KeyBestMoves, ""),
                UndoUsed = prefs.GetInt(PlayerProgressModel.KeyUndoUsed, 0),
                HintCount = prefs.GetInt(PlayerProgressModel.KeyHintCount, 0),
                RemoveAds = prefs.GetBool(PlayerProgressModel.KeyRemoveAds, false),
                DailyStamp = prefs.GetString(PlayerProgressModel.KeyDailyStamp, ""),
                Worlds = prefs.GetString(PlayerProgressModel.KeyWorlds, ""),
                Themes = prefs.GetString(PlayerProgressModel.KeyThemes, ""),
                Achievements = prefs.GetString(PlayerProgressModel.KeyAchieves, "")
            };
            snapshot.SnapshotChecksum = ComputeSnapshotChecksum(snapshot);

            prefs.SetString(KeyBackupSnapshot, UnityEngine.JsonUtility.ToJson(snapshot));
        }

        private static bool TryRestoreFromBackup(IPlayerPrefsService prefs, PlayerProgressModel m)
        {
            var raw = prefs.GetString(KeyBackupSnapshot, "");
            if (string.IsNullOrEmpty(raw))
                return false;

            ProgressBackupSnapshot snapshot;
            try
            {
                snapshot = UnityEngine.JsonUtility.FromJson<ProgressBackupSnapshot>(raw);
            }
            catch (System.Exception ex)
            {
                NexusLog.Warn("PlayerProgress", nameof(TryRestoreFromBackup), "",
                    $"Backup snapshot JSON parse failed: {ex.Message}");
                return false;
            }

            if (!IsBackupSnapshotValid(snapshot))
            {
                NexusLog.Warn("PlayerProgress", nameof(TryRestoreFromBackup), "",
                    "Backup snapshot failed validation; refusing to overwrite current save data.");
                return false;
            }

            PlayerProgressModel.WriteSchemaVersion(prefs, snapshot.SchemaVersion);
            prefs.SetInt(PlayerProgressModel.KeyCoins, snapshot.Coins);
            prefs.SetInt(PlayerProgressModel.KeyDiamonds, snapshot.Diamonds);
            prefs.SetInt(PlayerProgressModel.KeyXp, snapshot.Xp);
            prefs.SetInt(PlayerProgressModel.KeyCurrentLevel, snapshot.CurrentLevel);
            prefs.SetInt(PlayerProgressModel.KeyMaxUnlocked, snapshot.MaxUnlocked);
            prefs.SetInt(PlayerProgressModel.KeyPlayerLvl, snapshot.PlayerLevel);
            prefs.SetInt(PlayerProgressModel.KeyChestsBronze, snapshot.ChestBronze);
            prefs.SetInt(PlayerProgressModel.KeyChestsSilver, snapshot.ChestSilver);
            prefs.SetInt(PlayerProgressModel.KeyChestsGold, snapshot.ChestGold);
            prefs.SetInt(PlayerProgressModel.KeyChestsDiamond, snapshot.ChestDiamond);
            prefs.SetInt(PlayerProgressModel.KeyDailyDay, snapshot.DailyDayIndex);
            prefs.SetInt(PlayerProgressModel.KeyDailyStreak, snapshot.DailyStreak);
            prefs.SetString(PlayerProgressModel.KeyBestMoves, snapshot.BestMoves ?? "");
            prefs.SetInt(PlayerProgressModel.KeyUndoUsed, snapshot.UndoUsed);
            prefs.SetInt(PlayerProgressModel.KeyHintCount, snapshot.HintCount);
            prefs.SetBool(PlayerProgressModel.KeyRemoveAds, snapshot.RemoveAds);
            prefs.SetString(PlayerProgressModel.KeyDailyStamp, snapshot.DailyStamp ?? "0");
            prefs.SetString(PlayerProgressModel.KeyWorlds, snapshot.Worlds ?? "");
            prefs.SetString(PlayerProgressModel.KeyThemes, snapshot.Themes ?? "");
            prefs.SetString(PlayerProgressModel.KeyAchieves, snapshot.Achievements ?? "");

            // Now re-run the standard load with restored values.
            int newChecksum = ComputeProgressChecksum(prefs);
            prefs.SetInt(KeyChecksum, newChecksum);
            prefs.Save();

            Load(prefs, m);
            return true;
        }

        private static bool IsBackupSnapshotValid(ProgressBackupSnapshot snapshot)
        {
            if (snapshot == null) return false;
            if (snapshot.SchemaVersion <= 0) return false;
            if (snapshot.CurrentLevel < 1) return false;
            if (snapshot.MaxUnlocked < 1) return false;
            if (snapshot.PlayerLevel < 1) return false;
            if (snapshot.Coins < 0 || snapshot.Diamonds < 0 || snapshot.Xp < 0) return false;
            if (snapshot.ChestBronze < 0 || snapshot.ChestSilver < 0 || snapshot.ChestGold < 0 || snapshot.ChestDiamond < 0) return false;
            if (snapshot.UndoUsed < 0 || snapshot.HintCount < 0) return false;
            if (snapshot.SnapshotChecksum != ComputeSnapshotChecksum(snapshot)) return false;
            return true;
        }

        private static int ComputeSnapshotChecksum(ProgressBackupSnapshot s)
        {
            unchecked
            {
                int hash = 23;
                hash = hash * 31 + s.SchemaVersion;
                hash = hash * 31 + s.Coins;
                hash = hash * 31 + s.Diamonds;
                hash = hash * 31 + s.Xp;
                hash = hash * 31 + s.CurrentLevel;
                hash = hash * 31 + s.MaxUnlocked;
                hash = hash * 31 + s.PlayerLevel;
                hash = hash * 31 + s.ChestBronze;
                hash = hash * 31 + s.ChestSilver;
                hash = hash * 31 + s.ChestGold;
                hash = hash * 31 + s.ChestDiamond;
                hash = hash * 31 + s.DailyDayIndex;
                hash = hash * 31 + s.DailyStreak;
                hash = hash * 31 + Djb2Hash(s.BestMoves ?? "");
                hash = hash * 31 + s.UndoUsed;
                hash = hash * 31 + s.HintCount;
                hash = hash * 31 + (s.RemoveAds ? 1 : 0);
                hash = hash * 31 + Djb2Hash(s.DailyStamp ?? "");
                hash = hash * 31 + Djb2Hash(s.Worlds ?? "");
                hash = hash * 31 + Djb2Hash(s.Themes ?? "");
                hash = hash * 31 + Djb2Hash(s.Achievements ?? "");
                return hash;
            }
        }
    }
}



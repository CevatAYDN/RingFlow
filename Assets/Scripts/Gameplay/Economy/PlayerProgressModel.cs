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
        public const string KeyCoins         = "RF_Coins";
        public const string KeyDiamonds      = "RF_Diamonds";
        public const string KeyXp            = "RF_Xp";
        public const string KeyCurrentLevel  = "RF_CurrentLevel";
        public const string KeyMaxUnlocked   = "RF_MaxUnlocked";
        public const string KeyWorlds        = "RF_Worlds";
        public const string KeyThemes        = "RF_Themes";
        public const string KeyChestsBronze  = "RF_Chest_Bronze";
        public const string KeyChestsSilver  = "RF_Chest_Silver";
        public const string KeyChestsGold    = "RF_Chest_Gold";
        public const string KeyChestsDiamond = "RF_Chest_Diamond";
        public const string KeyPlayerLvl     = "RF_PlayerLevel";
        public const string KeyDailyDay      = "RF_DailyDayIndex";
        public const string KeyDailyStamp    = "RF_DailyLastClaimUtc";
        public const string KeyUndoUsed      = "RF_UndoUsedFree";
        public const string KeyAchieves      = "RF_Achievements";
        public const string KeyRemoveAds     = "RF_RemoveAds";
        public const string KeyHintCount     = "RF_HintCount";

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

        /// <summary>0..39 — unlocked state per world.</summary>
        public List<bool> UnlockedWorlds { get; } = new(40);
        /// <summary>themes owned (by ID, arbitrary strings managed outside).</summary>
        public List<string> OwnedThemes { get; } = new();

        /// <summary>Achievements achieved (id, timestampUtc).</summary>
        public List<string> Achievements { get; } = new();

        // XP thresholds per player level — GDD §9: Bronze 100 / Silver 250 / Gold 500 / Diamond 1000
        // Cumulative XP to next level grows; we use a simple curve.
        public int XpToNextLevel(int playerLevel)
        {
            // 100 cumulative at level 1, 250 at 2, 500 at 3, etc. — staircase that mirrors chest values.
            return playerLevel switch
            {
                1 => 100,
                2 => 250,
                3 => 500,
                _ => 1000
            };
        }

        public ValueTask OnBind(System.Threading.CancellationToken ct)
        {
            // Initialize unlocked worlds with all false, then mark world 0 as unlocked.
            if (UnlockedWorlds.Count == 0)
            {
                for (int i = 0; i < 40; i++) UnlockedWorlds.Add(false);
                UnlockedWorlds[0] = true;
            }
            return default;
        }

        public void Reset()
        {
            // Reset does NOT clear persisted data; use SaveSystem.Restore explicitly.
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
            for (int i = 0; i < 40; i++) UnlockedWorlds.Add(i == 0);
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
        public static void Save(IPlayerPrefsService prefs, PlayerProgressModel m)
        {
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

            // Worlds / themes / achievements encode as compact strings (csv-equivalent)
            SaveBoolList(prefs, PlayerProgressModel.KeyWorlds, m.UnlockedWorlds);
            SaveStringList(prefs, PlayerProgressModel.KeyThemes, m.OwnedThemes);
            SaveStringList(prefs, PlayerProgressModel.KeyAchieves, m.Achievements);

            prefs.Save();
        }

        public static void Load(IPlayerPrefsService prefs, PlayerProgressModel m)
        {
            m.Coins.Value          = prefs.GetInt(PlayerProgressModel.KeyCoins, 0);
            m.Diamonds.Value       = prefs.GetInt(PlayerProgressModel.KeyDiamonds, 0);
            m.Xp.Value             = prefs.GetInt(PlayerProgressModel.KeyXp, 0);
            m.CurrentLevel.Value   = prefs.GetInt(PlayerProgressModel.KeyCurrentLevel, 1);
            m.MaxUnlockedLevel.Value = prefs.GetInt(PlayerProgressModel.KeyMaxUnlocked, 1);
            if (m.CurrentLevel.Value < 1) m.CurrentLevel.Value = 1;
            if (m.MaxUnlockedLevel.Value < 1) m.MaxUnlockedLevel.Value = 1;

            m.PlayerLevel.Value       = prefs.GetInt(PlayerProgressModel.KeyPlayerLvl, 1);
            m.ChestBronze.Value       = prefs.GetInt(PlayerProgressModel.KeyChestsBronze, 0);
            m.ChestSilver.Value       = prefs.GetInt(PlayerProgressModel.KeyChestsSilver, 0);
            m.ChestGold.Value         = prefs.GetInt(PlayerProgressModel.KeyChestsGold, 0);
            m.ChestDiamond.Value      = prefs.GetInt(PlayerProgressModel.KeyChestsDiamond, 0);
            m.DailyDayIndex.Value     = prefs.GetInt(PlayerProgressModel.KeyDailyDay, -1);

            // IPlayerPrefsService has no long primitive — encode as string (base 10).
            var stampStr = prefs.GetString(PlayerProgressModel.KeyDailyStamp, "0");
            long stampTicks = 0;
            long.TryParse(stampStr, out stampTicks);
            m.DailyLastClaimUtcTicks.Value = stampTicks;

            m.FreeUndosUsedThisSession.Value = prefs.GetInt(PlayerProgressModel.KeyUndoUsed, 0);
            m.HintCount.Value         = prefs.GetInt(PlayerProgressModel.KeyHintCount, 0);
            m.RemoveAds.Value         = prefs.GetBool(PlayerProgressModel.KeyRemoveAds, false);

            m.UnlockedWorlds.Clear();
            LoadBoolList(prefs, PlayerProgressModel.KeyWorlds, m.UnlockedWorlds, 40, true, 0);
            m.OwnedThemes.Clear();
            LoadStringList(prefs, PlayerProgressModel.KeyThemes, m.OwnedThemes);
            m.Achievements.Clear();
            LoadStringList(prefs, PlayerProgressModel.KeyAchieves, m.Achievements);
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
    }
}



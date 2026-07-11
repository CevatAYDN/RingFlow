using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    [CustomEditor(typeof(GameConfigDatabaseSO))]
    public class GameConfigDatabaseSOEditor : UnityEditor.Editor
    {
        private int _selectedWorldIndex;
        private Vector2 _scrollPos;

        public override void OnInspectorGUI()
        {
            var db = (GameConfigDatabaseSO)target;

            EditorGUILayout.LabelField("RingFlow Game Config Database", RingFlowEditorUtils.HeaderStyle);
            EditorGUILayout.Space(4f);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUI.BeginChangeCheck();

            // ── Total Levels ──
            db.TotalLevels = EditorGUILayout.IntField("Total Levels", db.TotalLevels);

            EditorGUILayout.Space(8f);

            // ── Difficulty Bands ──
            EditorGUILayout.LabelField("Difficulty Bands", EditorStyles.boldLabel);
            if (db.DifficultyBands == null) db.DifficultyBands = new System.Collections.Generic.List<DifficultyBandData>();

            for (int i = 0; i < db.DifficultyBands.Count; i++)
            {
                var band = db.DifficultyBands[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(band.Band.ToString(), EditorStyles.boldLabel);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        band.MaxLevel = EditorGUILayout.IntField("Max Level", band.MaxLevel);
                        band.MinEmptyPoles = EditorGUILayout.IntField("Min Empty Poles", band.MinEmptyPoles);
                        band.MaxCapacity = EditorGUILayout.IntField("Max Capacity", band.MaxCapacity);
                    }

                    EditorGUILayout.LabelField("Allowed Mechanics:");
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (band.AllowedMechanics == null)
                            band.AllowedMechanics = new System.Collections.Generic.List<WorldMechanicType>();

                        var mechNames = System.Enum.GetNames(typeof(WorldMechanicType));
                        var mechValues = (WorldMechanicType[])System.Enum.GetValues(typeof(WorldMechanicType));

                        for (int m = 0; m < mechNames.Length; m++)
                        {
                            bool allowed = band.AllowedMechanics.Contains(mechValues[m]);
                            bool newAllowed = EditorGUILayout.ToggleLeft(mechNames[m], allowed, GUILayout.Width(110f));
                            if (newAllowed != allowed)
                            {
                                if (newAllowed)
                                    band.AllowedMechanics.Add(mechValues[m]);
                                else
                                    band.AllowedMechanics.Remove(mechValues[m]);
                            }
                        }
                    }

                    EditorGUILayout.LabelField($"Active: {string.Join(", ", band.AllowedMechanics ?? new System.Collections.Generic.List<WorldMechanicType>())}",
                        EditorStyles.miniLabel);
                }

                if (i < db.DifficultyBands.Count - 1)
                    EditorGUILayout.Space(2f);
            }

            EditorGUILayout.Space(8f);

            // ── Color Curve ──
            EditorGUILayout.LabelField("Color Progression Curve", EditorStyles.boldLabel);
            if (db.ColorCurve == null)
                db.ColorCurve = new System.Collections.Generic.List<ColorCurvePoint>();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                for (int i = 0; i < db.ColorCurve.Count; i++)
                {
                    var pt = db.ColorCurve[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"Point {i + 1}", GUILayout.Width(60f));
                        pt.LevelThreshold = EditorGUILayout.IntField("Level ≥", pt.LevelThreshold, GUILayout.Width(100f));
                        pt.ColorCount = EditorGUILayout.IntSlider("Colors", pt.ColorCount, 2, 10, GUILayout.Width(180f));
                        db.ColorCurve[i] = pt;
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ Add Point"))
                        db.ColorCurve.Add(new ColorCurvePoint { LevelThreshold = 2000, ColorCount = 10 });
                    if (db.ColorCurve.Count > 1 && GUILayout.Button("- Remove Last"))
                        db.ColorCurve.RemoveAt(db.ColorCurve.Count - 1);
                }
            }

            EditorGUILayout.Space(8f);

            // ── Worlds ──
            EditorGUILayout.LabelField("Worlds & Theme Config", EditorStyles.boldLabel);
            if (db.Worlds == null)
                db.Worlds = new System.Collections.Generic.List<WorldConfigData>();

            string[] worldNames = new string[db.Worlds.Count];
            for (int i = 0; i < db.Worlds.Count; i++)
                worldNames[i] = $"World {i + 1}: {db.Worlds[i].Theme}";

            _selectedWorldIndex = EditorGUILayout.Popup("Select World", _selectedWorldIndex, worldNames);

            if (_selectedWorldIndex >= 0 && _selectedWorldIndex < db.Worlds.Count)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var w = db.Worlds[_selectedWorldIndex];
                    int startLevel = _selectedWorldIndex * WorldConfigSO.LevelsPerWorld + 1;
                    int endLevel = startLevel + WorldConfigSO.LevelsPerWorld - 1;

                    EditorGUILayout.LabelField($"Level Range: {startLevel} – {endLevel}", EditorStyles.miniLabel);

                    var band = db.GetBandForLevel(startLevel);
                    var intensity = db.GetMechanicIntensityForLevel(startLevel);
                    EditorGUILayout.LabelField($"Band: {band} | Intensity: {intensity}", EditorStyles.miniLabel);

                    w.Theme = EditorGUILayout.TextField("Theme Display Name", w.Theme);
                    w.UnlockedByWorldIndex = EditorGUILayout.IntField("Unlocked by World Index", w.UnlockedByWorldIndex);
                    w.IsEventWorld = EditorGUILayout.Toggle("Is Boss World", w.IsEventWorld);
                    w.MechanicType = (WorldMechanicType)EditorGUILayout.EnumPopup("Special Mechanic", w.MechanicType);

                    // Band-mechanic mismatch warning
                    var allowed = db.GetAllowedMechanicsForLevel(startLevel);
                    if (w.MechanicType != WorldMechanicType.None &&
                        w.MechanicType != WorldMechanicType.RandomPool1 &&
                        w.MechanicType != WorldMechanicType.RandomPool2 &&
                        w.MechanicType != WorldMechanicType.RandomPool3 &&
                        !allowed.Contains(w.MechanicType))
                    {
                        EditorGUILayout.HelpBox(
                            $"World's mechanic ({w.MechanicType}) is NOT in band {band}'s AllowedMechanics list. " +
                            $"The mechanic will still inject (world-assigned mechanics bypass band gating), but this may indicate a misconfiguration. " +
                            $"Consider adding {w.MechanicType} to band {band}'s AllowedMechanics.",
                            MessageType.Warning);
                    }

                    db.Worlds[_selectedWorldIndex] = w;
                }
            }

            EditorGUILayout.Space(8f);

            // ── Balance Config ──
            EditorGUILayout.LabelField("Balance Config", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Undo", EditorStyles.miniBoldLabel);
                db.BalanceConfig.FreeUndosPerSession = EditorGUILayout.IntField("Free Undos Per Session", db.BalanceConfig.FreeUndosPerSession);
                db.BalanceConfig.UndoCoinCost = EditorGUILayout.IntField("Undo Coin Cost", db.BalanceConfig.UndoCoinCost);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Level Completion Rewards", EditorStyles.miniBoldLabel);
                db.BalanceConfig.NormalCoinReward = EditorGUILayout.IntField("Normal Coin Reward", db.BalanceConfig.NormalCoinReward);
                db.BalanceConfig.BossCoinReward = EditorGUILayout.IntField("Boss Coin Reward", db.BalanceConfig.BossCoinReward);
                db.BalanceConfig.NormalXpReward = EditorGUILayout.IntField("Normal XP Reward", db.BalanceConfig.NormalXpReward);
                db.BalanceConfig.BossXpReward = EditorGUILayout.IntField("Boss XP Reward", db.BalanceConfig.BossXpReward);
                db.BalanceConfig.LevelUpCoinReward = EditorGUILayout.IntField("Level Up Coin Reward", db.BalanceConfig.LevelUpCoinReward);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Star Thresholds", EditorStyles.miniBoldLabel);
                db.BalanceConfig.ThreeStarTargetRatioPercent = EditorGUILayout.IntSlider("3-Star Ratio %", db.BalanceConfig.ThreeStarTargetRatioPercent, 50, 200);
                db.BalanceConfig.TwoStarTargetRatioPercent = EditorGUILayout.IntSlider("2-Star Ratio %", db.BalanceConfig.TwoStarTargetRatioPercent, 50, 300);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Chest Drop Rates", EditorStyles.miniBoldLabel);
                db.BalanceConfig.SilverChestChance = EditorGUILayout.Slider("Silver Chest %", db.BalanceConfig.SilverChestChance, 0f, 1f);
                db.BalanceConfig.GoldChestChance = EditorGUILayout.Slider("Gold Chest %", db.BalanceConfig.GoldChestChance, 0f, 1f);
                db.BalanceConfig.DiamondChestChance = EditorGUILayout.Slider("Diamond Chest %", db.BalanceConfig.DiamondChestChance, 0f, 1f);

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Ad Intervals", EditorStyles.miniBoldLabel);
                db.BalanceConfig.InterstitialAdInterval = EditorGUILayout.IntField("Interstitial Ad Interval", db.BalanceConfig.InterstitialAdInterval);
            }

            EditorGUILayout.Space(12f);

            // ── Actions ──
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("Reset to Defaults",
                        "Reset all database values to GDD defaults? This cannot be undone.",
                        "Reset", "Cancel"))
                    {
                        db.InitializeDefaults();
                        EditorUtility.SetDirty(db);
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(db);
            }
        }
    }
}

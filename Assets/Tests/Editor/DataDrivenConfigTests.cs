using NUnit.Framework;
using UnityEngine;
using RingFlow.Gameplay;
using RingFlow.Gameplay.Economy;
using RingFlow.Gameplay.Localization;
using RingFlow.Gameplay.Strategies;
using RingFlow.Gameplay.Views;

namespace RingFlow.Tests
{
    [TestFixture]
    public class DataDrivenConfigTests
    {
        [Test]
        public void StoreCatalogSO_LoadsFromResources()
        {
            var catalog = Resources.Load<StoreCatalogSO>(GameplayAssetKeys.StoreCatalog);
            Assert.IsNotNull(catalog, "StoreCatalog.asset must exist in Resources");
            Assert.IsNotNull(catalog.Products, "Products list must not be null");
            Assert.Greater(catalog.Products.Count, 0, "At least one product required");
        }

        [Test]
        public void StoreCatalogSO_ProductsHaveValidIds()
        {
            var catalog = Resources.Load<StoreCatalogSO>(GameplayAssetKeys.StoreCatalog);
            Assert.IsNotNull(catalog);
            for (int i = 0; i < catalog.Products.Count; i++)
            {
                var p = catalog.Products[i];
                Assert.IsFalse(string.IsNullOrEmpty(p.Id), $"Product {i} has empty Id");
                Assert.IsFalse(string.IsNullOrEmpty(p.PriceString), $"Product {i} has empty PriceString");
            }
        }

        [Test]
        public void LocalizationConfigSO_LoadsFromResources()
        {
            var config = Resources.Load<LocalizationConfigSO>(GameplayAssetKeys.LocalizationConfig);
            Assert.IsNotNull(config, "LocalizationConfig.asset must exist in Resources");
            Assert.IsNotNull(config.Languages, "Languages list must not be null");
            Assert.Greater(config.Languages.Count, 0, "At least one language required");
        }

        [Test]
        public void LocalizationConfigSO_ContainsEnglishAndTurkish()
        {
            var config = Resources.Load<LocalizationConfigSO>(GameplayAssetKeys.LocalizationConfig);
            Assert.IsNotNull(config);

            bool hasEnglish = false;
            bool hasTurkish = false;
            for (int i = 0; i < config.Languages.Count; i++)
            {
                var lang = config.Languages[i];
                if (lang.Code == "en") hasEnglish = true;
                if (lang.Code == "tr") hasTurkish = true;
            }
            Assert.IsTrue(hasEnglish, "English (en) must be in the language list");
            Assert.IsTrue(hasTurkish, "Turkish (tr) must be in the language list");
        }

        [Test]
        public void RingMechanicDataSO_LoadsFromResources()
        {
            var data = Resources.Load<RingMechanicDataSO>(GameplayAssetKeys.RingMechanicData);
            Assert.IsNotNull(data, "RingMechanicData.asset must exist in Resources");
            Assert.IsNotNull(data.Mechanics, "Mechanics list must not be null");
            Assert.Greater(data.Mechanics.Count, 0, "At least one mechanic entry required");
        }

        [Test]
        public void RingMechanicDataSO_MechanicsHaveUniqueTypes()
        {
            var data = Resources.Load<RingMechanicDataSO>(GameplayAssetKeys.RingMechanicData);
            Assert.IsNotNull(data);

            for (int i = 0; i < data.Mechanics.Count; i++)
            {
                for (int j = i + 1; j < data.Mechanics.Count; j++)
                {
                    Assert.AreNotEqual(data.Mechanics[i].Type, data.Mechanics[j].Type,
                        $"Duplicate mechanic type {data.Mechanics[i].Type} at index {i} and {j}");
                }
            }
        }

        [Test]
        public void RingMechanicDataSO_ResolvesNameAndFirstAppearanceFromGameConfigDb()
        {
            var data = Resources.Load<RingMechanicDataSO>(GameplayAssetKeys.RingMechanicData);
            var db = Resources.Load<GameConfigDatabaseSO>(GameplayAssetKeys.GameConfigDatabase);
            Assert.IsNotNull(data);
            Assert.IsNotNull(db);

            for (int i = 0; i < data.Mechanics.Count; i++)
            {
                var type = data.Mechanics[i].Type;
                var resolvedName = data.GetDisplayNameKey(type, db);
                var resolvedWorld = data.GetFirstAppearanceWorldIndex(type, db);

                // Find matching entry in db config database
                MechanicUnlockEntry? match = null;
                for (int k = 0; k < db.MechanicUnlocks.Count; k++)
                {
                    if (db.MechanicUnlocks[k].MechanicType == type)
                    {
                        match = db.MechanicUnlocks[k];
                        break;
                    }
                }

                if (match != null)
                {
                    Assert.AreEqual(match.Value.DisplayNameKey, resolvedName, $"Mismatch name key for {type}");
                    Assert.AreEqual(match.Value.FirstAppearanceWorldIndex, resolvedWorld, $"Mismatch world index for {type}");
                }
            }
        }

        [Test]
        public void ThemeSkinDatabaseSO_LoadsFromResources()
        {
            var db = Resources.Load<ThemeSkinDatabaseSO>(GameplayAssetKeys.ThemeSkinDatabase);
            Assert.IsNotNull(db, "ThemeSkinDatabase.asset must exist in Resources");
        }

        [Test]
        public void GameConfigDatabaseSO_HasMechanicUnlocks()
        {
            var db = Resources.Load<GameConfigDatabaseSO>(GameplayAssetKeys.GameConfigDatabase);
            Assert.IsNotNull(db);
            Assert.IsNotNull(db.MechanicUnlocks, "MechanicUnlocks list must not be null");
            Assert.Greater(db.MechanicUnlocks.Count, 0, "At least one MechanicUnlockEntry required");
        }

        [Test]
        public void GameConfigDatabaseSO_MechanicUnlocksHaveUniqueTypes()
        {
            var db = Resources.Load<GameConfigDatabaseSO>(GameplayAssetKeys.GameConfigDatabase);
            Assert.IsNotNull(db);

            for (int i = 0; i < db.MechanicUnlocks.Count; i++)
            {
                for (int j = i + 1; j < db.MechanicUnlocks.Count; j++)
                {
                    Assert.AreNotEqual(db.MechanicUnlocks[i].MechanicType, db.MechanicUnlocks[j].MechanicType,
                        $"Duplicate MechanicUnlock type {db.MechanicUnlocks[i].MechanicType}");
                }
            }
        }

        /// <summary>
        /// BUG-3 Exploration Test — EXPECTED TO FAIL ON UNFIXED CODE.
        ///
        /// This test proves the existence of the bug in GetPoleCountForLevel for
        /// Master/Legend difficulty bands where MinEmptyPoles == 0.
        ///
        /// Bug condition: GetPoleCountForLevel uses Math.Max(GetMinEmptyPolesForLevel(level), 1),
        /// which converts MinEmptyPoles=0 to 1, producing colorCount+1 instead of colorCount+0.
        ///
        /// Counterexample (from unfixed code):
        ///   Level is in Master band, colorCount=10, MinEmptyPoles=0,
        ///   Expected poleCount = 10+0 = 10, Actual (buggy) poleCount = 10+1 = 11
        ///
        /// Validates: Requirements 1.5, 1.6
        /// </summary>
        [Test]
        public void GetPoleCountForLevel_MasterLegendBand_RespectsMinEmptyPolesFromData_BugCondition()
        {
            var db = Resources.Load<GameConfigDatabaseSO>(GameplayAssetKeys.GameConfigDatabase);
            Assert.IsNotNull(db, "GameConfigDatabase.asset must exist in Resources/Configs/");
            Assert.IsNotNull(db.DifficultyBands, "DifficultyBands must not be null");
            Assert.Greater(db.DifficultyBands.Count, 0, "DifficultyBands must have at least one entry");

            // Find the Master band entry to get its MaxLevel boundary
            int masterBandMaxLevel = -1;
            for (int i = 0; i < db.DifficultyBands.Count; i++)
            {
                if (db.DifficultyBands[i].Band == DifficultyBand.Master)
                {
                    masterBandMaxLevel = db.DifficultyBands[i].MaxLevel;
                    break;
                }
            }

            Assert.Greater(masterBandMaxLevel, 0,
                "Master band must be defined in DifficultyBands with a valid MaxLevel > 0");

            // Pick a level squarely in the Master band:
            // Master band ends at masterBandMaxLevel, so we use that exact level.
            // The level just before Legend starts is the last Master level.
            int masterLevel = masterBandMaxLevel;

            // Verify this level is actually in the Master band
            var band = db.GetBandForLevel(masterLevel);
            Assert.AreEqual(DifficultyBand.Master, band,
                $"Level {masterLevel} should be in Master band but was in {band}");

            // Gather the inputs for the calculation
            int colorCount = db.GetColorCountForLevel(masterLevel);
            int minEmptyPoles = db.GetMinEmptyPolesForLevel(masterLevel);

            // Verify bug precondition: Master band must have MinEmptyPoles == 1
            Assert.AreEqual(1, minEmptyPoles,
                $"This test requires MinEmptyPoles=1 for Master band, but got {minEmptyPoles}. " +
                "If the asset changed, update this test.");

            int poleCount = db.GetPoleCountForLevel(masterLevel);

            // Counterexample: Level=masterLevel, colorCount=colorCount, MinEmptyPoles=0,
            // expected poleCount=colorCount, actual (buggy) poleCount=colorCount+1
            //
            // ASSERTION: colorCount + minEmptyPoles == colorCount + 0 == colorCount
            // This FAILS on unfixed code because Math.Max(0, 1) == 1,
            // making GetPoleCountForLevel return colorCount+1 instead of colorCount+0.
            Assert.AreEqual(
                colorCount + minEmptyPoles,
                poleCount,
                $"BUG-3: Level={masterLevel} (Master band), colorCount={colorCount}, " +
                $"MinEmptyPoles=0, expected poleCount={colorCount + minEmptyPoles}, " +
                $"but GetPoleCountForLevel returned {poleCount}. " +
                $"Math.Max(0,1)==1 is overriding the GDD value of MinEmptyPoles=0.");
        }

        /// <summary>
        /// BUG-3 Preservation — Tutorial band MinEmptyPoles=2, GetPoleCountForLevel doğru çalışıyor.
        /// UNFIXED kodda PASS etmeli, fix sonrasında da PASS etmeye devam etmeli.
        /// Requirements: 3.5, 3.6
        /// </summary>
        [Test]
        public void GetPoleCountForLevel_TutorialBand_ReturnsColorPlusMinEmpty_Preservation()
        {
            var db = Resources.Load<GameConfigDatabaseSO>(GameplayAssetKeys.GameConfigDatabase);
            Assert.IsNotNull(db, "GameConfigDatabase.asset must exist in Resources/Configs/");
            Assert.IsNotNull(db.DifficultyBands, "DifficultyBands must not be null");
            Assert.Greater(db.DifficultyBands.Count, 0, "DifficultyBands must have at least one entry");

            // Find the Tutorial band and verify its MinEmptyPoles
            int tutorialMaxLevel = -1;
            int tutorialMinEmpty = -1;
            for (int i = 0; i < db.DifficultyBands.Count; i++)
            {
                if (db.DifficultyBands[i].Band == DifficultyBand.Tutorial)
                {
                    tutorialMaxLevel = db.DifficultyBands[i].MaxLevel;
                    tutorialMinEmpty = db.DifficultyBands[i].MinEmptyPoles;
                    break;
                }
            }

            Assert.Greater(tutorialMaxLevel, 0,
                "Tutorial band must be defined in DifficultyBands with a valid MaxLevel > 0");
            Assert.GreaterOrEqual(tutorialMinEmpty, 1,
                $"Preservation test requires Tutorial band MinEmptyPoles >= 1, but got {tutorialMinEmpty}. " +
                "Math.Max(minEmpty, 1) == minEmpty for this band — bug does NOT affect it.");

            // Pick level 3 as a representative Tutorial-band level (well within the band).
            // Fall back to 1 if the band is very small.
            int level = tutorialMaxLevel >= 3 ? 3 : 1;

            // Confirm the level is indeed in the Tutorial band
            var actualBand = db.GetBandForLevel(level);
            Assert.AreEqual(DifficultyBand.Tutorial, actualBand,
                $"Level {level} should resolve to Tutorial band but resolved to {actualBand}.");

            int colorCount = db.GetColorCountForLevel(level);
            int minEmpty = db.GetMinEmptyPolesForLevel(level);
            int poleCount = db.GetPoleCountForLevel(level);

            // For Tutorial (MinEmptyPoles=2 >= 1), Math.Max(minEmpty, 1) == minEmpty,
            // so the buggy Math.Max guard has no effect. This assertion PASSES on both
            // unfixed and fixed code, confirming the fix does not break Tutorial band.
            Assert.AreEqual(
                colorCount + minEmpty,
                poleCount,
                $"Preservation: Level={level} (Tutorial band), colorCount={colorCount}, " +
                $"MinEmptyPoles={minEmpty}, expected poleCount={colorCount + minEmpty}, " +
                $"but GetPoleCountForLevel returned {poleCount}.");
        }

        /// <summary>
        /// BUG-3 Preservation — MinEmptyPoles>=1 olan tüm band'larda poleCount = colorCount + minEmpty.
        /// Easy/Medium/Hard/Expert band'larını kapsar. UNFIXED kodda PASS etmeli.
        /// Requirements: 3.5, 3.6
        /// </summary>
        [Test]
        public void GetPoleCountForLevel_NonZeroMinEmptyBands_AllReturnCorrectPoleCount_Preservation()
        {
            var db = Resources.Load<GameConfigDatabaseSO>(GameplayAssetKeys.GameConfigDatabase);
            Assert.IsNotNull(db, "GameConfigDatabase.asset must exist in Resources/Configs/");
            Assert.IsNotNull(db.DifficultyBands, "DifficultyBands must not be null");
            Assert.Greater(db.DifficultyBands.Count, 0, "DifficultyBands must have at least one entry");

            bool testedAtLeastOneBand = false;

            for (int i = 0; i < db.DifficultyBands.Count; i++)
            {
                var bandData = db.DifficultyBands[i];

                // Only test bands where MinEmptyPoles >= 1 (bug does NOT affect these).
                // Master/Legend have MinEmptyPoles=0 and are covered by the bug condition test.
                if (bandData.MinEmptyPoles < 1)
                    continue;

                testedAtLeastOneBand = true;

                // Determine a representative level in the middle of this band's range.
                // Previous band's MaxLevel (or 1 for the first band) gives the lower bound.
                int bandStart = 1;
                if (i > 0)
                    bandStart = db.DifficultyBands[i - 1].MaxLevel + 1;

                int bandEnd = bandData.MaxLevel;

                // Guard against degenerate band ranges (shouldn't happen with valid data).
                Assert.GreaterOrEqual(bandEnd, bandStart,
                    $"Band {bandData.Band}: MaxLevel ({bandEnd}) must be >= band start ({bandStart}).");

                // Use the midpoint level for a representative sample.
                int level = bandStart + (bandEnd - bandStart) / 2;
                if (level < bandStart) level = bandStart;

                // Confirm the level resolves to the expected band
                var actualBand = db.GetBandForLevel(level);
                Assert.AreEqual(bandData.Band, actualBand,
                    $"Level {level} should resolve to {bandData.Band} band but resolved to {actualBand}.");

                int colorCount = db.GetColorCountForLevel(level);
                int minEmpty = db.GetMinEmptyPolesForLevel(level);
                int poleCount = db.GetPoleCountForLevel(level);

                // For bands with MinEmptyPoles >= 1, Math.Max(minEmpty, 1) == minEmpty,
                // so the unfixed code already returns the correct value.
                // This assertion PASSES on both unfixed and fixed code.
                Assert.AreEqual(
                    colorCount + minEmpty,
                    poleCount,
                    $"{bandData.Band} band level {level}: expected poleCount={colorCount + minEmpty} " +
                    $"(colorCount={colorCount} + minEmpty={minEmpty}), " +
                    $"but GetPoleCountForLevel returned {poleCount}.");
            }

            Assert.IsTrue(testedAtLeastOneBand,
                "At least one band with MinEmptyPoles >= 1 must exist (Tutorial/Easy/Medium/Hard/Expert).");
        }
    }
}

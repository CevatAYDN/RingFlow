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
    }
}

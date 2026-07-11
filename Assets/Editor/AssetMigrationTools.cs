using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    internal static class AssetMigrationTools
    {
        [MenuItem("RingFlow/Migration/Sync GameConfigDatabase AllowedMechanics")]
        public static void SyncGameConfigDatabaseAllowedMechanics()
        {
            const string assetPath = "Assets/Resources/GameConfigDatabase.asset";

            var database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(assetPath);
            if (database == null)
            {
                Debug.LogError(
                    $"[AssetMigrationTools] GameConfigDatabaseSO not found at '{assetPath}'.");
                return;
            }

            int patched = 0;

            for (int i = 0; i < database.DifficultyBands.Count; i++)
            {
                var band = database.DifficultyBands[i];
                if (band.AllowedMechanics != null && band.AllowedMechanics.Count > 0)
                {
                    continue;
                }

                band.AllowedMechanics = BuildAllowedMechanics(band.Band);
                database.DifficultyBands[i] = band;
                patched++;
            }

            if (patched == 0)
            {
                Debug.Log(
                    "[AssetMigrationTools] All DifficultyBands already have AllowedMechanics. Nothing to sync.");
                return;
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssetIfDirty(database);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            Debug.Log(
                $"[AssetMigrationTools] Patched AllowedMechanics for {patched} DifficultyBand(s). Asset saved and reimported.");
        }

        private static List<WorldMechanicType> BuildAllowedMechanics(DifficultyBand band)
        {
            switch (band)
            {
                case DifficultyBand.Tutorial:
                    return new List<WorldMechanicType>
                    {
                        WorldMechanicType.None,
                        WorldMechanicType.Mystery
                    };
                case DifficultyBand.Easy:
                    return new List<WorldMechanicType>
                    {
                        WorldMechanicType.None,
                        WorldMechanicType.Mystery,
                        WorldMechanicType.Frozen
                    };
                case DifficultyBand.Medium:
                    return new List<WorldMechanicType>
                    {
                        WorldMechanicType.None,
                        WorldMechanicType.Mystery,
                        WorldMechanicType.Frozen,
                        WorldMechanicType.LockedPole,
                        WorldMechanicType.Stone
                    };
                case DifficultyBand.Hard:
                    return new List<WorldMechanicType>
                    {
                        WorldMechanicType.None,
                        WorldMechanicType.Mystery,
                        WorldMechanicType.Frozen,
                        WorldMechanicType.LockedPole,
                        WorldMechanicType.Stone,
                        WorldMechanicType.Glass,
                        WorldMechanicType.Rainbow
                    };
                case DifficultyBand.Expert:
                    return new List<WorldMechanicType>
                    {
                        WorldMechanicType.None,
                        WorldMechanicType.Mystery,
                        WorldMechanicType.Frozen,
                        WorldMechanicType.LockedPole,
                        WorldMechanicType.Stone,
                        WorldMechanicType.Glass,
                        WorldMechanicType.Rainbow,
                        WorldMechanicType.Bomb,
                        WorldMechanicType.Chain
                    };
                case DifficultyBand.Master:
                    return new List<WorldMechanicType>
                    {
                        WorldMechanicType.None,
                        WorldMechanicType.Mystery,
                        WorldMechanicType.Frozen,
                        WorldMechanicType.LockedPole,
                        WorldMechanicType.Stone,
                        WorldMechanicType.Glass,
                        WorldMechanicType.Rainbow,
                        WorldMechanicType.Bomb,
                        WorldMechanicType.Chain,
                        WorldMechanicType.Magnet
                    };
                case DifficultyBand.Legend:
                    return new List<WorldMechanicType>
                    {
                        WorldMechanicType.None,
                        WorldMechanicType.Mystery,
                        WorldMechanicType.Frozen,
                        WorldMechanicType.LockedPole,
                        WorldMechanicType.Stone,
                        WorldMechanicType.Glass,
                        WorldMechanicType.Rainbow,
                        WorldMechanicType.Bomb,
                        WorldMechanicType.Chain,
                        WorldMechanicType.Magnet,
                        WorldMechanicType.Paint,
                        WorldMechanicType.Ghost,
                        WorldMechanicType.RandomPool1,
                        WorldMechanicType.RandomPool2,
                        WorldMechanicType.RandomPool3
                    };
                default:
                    return new List<WorldMechanicType> { WorldMechanicType.None };
            }
        }
    }
}
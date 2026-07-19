using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Nexus.Core.Services;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    internal static class AssetMigrationTools
    {
        [MenuItem("Ring Flow/Migration/Sync GameConfigDatabase AllowedMechanics")]
        public static void SyncGameConfigDatabaseAllowedMechanics()
        {
            const string assetPath = EditorPaths.GameConfigDbPath;

            var database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(assetPath);
            if (database == null)
            {
                NexusLog.Error("AssetMigrationTools", nameof(SyncGameConfigDatabaseAllowedMechanics), "LoadDatabase",
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
                NexusLog.Info("AssetMigrationTools", nameof(SyncGameConfigDatabaseAllowedMechanics), "Sync",
                    "[AssetMigrationTools] All DifficultyBands already have AllowedMechanics. Nothing to sync.");
                return;
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssetIfDirty(database);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            NexusLog.Info("AssetMigrationTools", nameof(SyncGameConfigDatabaseAllowedMechanics), "Sync",
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

        // ─────────────────────────────────────────────────────────────────
        //  FIX-FOLDER: Klasör Düzenleme Araçları
        //  GameConfigDatabaseSO ve GameplayHelpers yanlış klasörde duruyor.
        //  Unity'de terminal ile GUID güvenli taşıma yapılamaz;
        //  bu menü öğeleri AssetDatabase.MoveAsset kullanarak güvenli taşır.
        // ─────────────────────────────────────────────────────────────────

        [MenuItem("Ring Flow/Migration/Fix Folder — Move GameConfigDatabaseSO to Config/")]
        public static void MoveGameConfigDatabaseSOToConfig()
        {
            // GameConfigDatabaseSO World/ klasöründe duruyor — Config/ altına alınmalı.
            const string from = "Assets/Scripts/Gameplay/World/GameConfigDatabaseSO.cs";
            const string to   = "Assets/Scripts/Gameplay/Config/GameConfigDatabaseSO.cs";
            MoveScriptSafe(from, to, "GameConfigDatabaseSO");
        }

        [MenuItem("Ring Flow/Migration/Fix Folder — Move GameplayHelpers to Gameplay root")]
        public static void MoveGameplayHelpersToRoot()
        {
            // GameplayHelpers Commands/ klasöründe duruyor — utility sınıfı root'a ait.
            const string from = "Assets/Scripts/Gameplay/Commands/GameplayHelpers.cs";
            const string to   = "Assets/Scripts/Gameplay/GameplayHelpers.cs";
            MoveScriptSafe(from, to, "GameplayHelpers");
        }

        /// <summary>
        /// Unity AssetDatabase ile GUID korumalı dosya taşıma.
        /// .meta dosyasını da otomatik taşır — GUID kaybolmaz.
        /// </summary>
        private static void MoveScriptSafe(string fromPath, string toPath, string label)
        {
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(fromPath)))
            {
                EditorUtility.DisplayDialog("Taşıma Başarısız",
                    $"{label}: kaynak dosya bulunamadı.\n{fromPath}", "Tamam");
                return;
            }

            // Hedef klasörü oluştur (yoksa)
            string targetDir = System.IO.Path.GetDirectoryName(toPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(targetDir) && !AssetDatabase.IsValidFolder(targetDir))
            {
                string parent = System.IO.Path.GetDirectoryName(targetDir)?.Replace('\\', '/') ?? "Assets";
                string folderName = System.IO.Path.GetFileName(targetDir);
                AssetDatabase.CreateFolder(parent, folderName);
            }

            string result = AssetDatabase.MoveAsset(fromPath, toPath);
            if (string.IsNullOrEmpty(result))
            {
                AssetDatabase.Refresh();
                NexusLog.Info("AssetMigrationTools", nameof(MoveScriptSafe), label,
                    $"[Migration] {label} başarıyla taşındı: {fromPath} → {toPath}");
                EditorUtility.DisplayDialog("Taşıma Başarılı",
                    $"{label} taşındı.\n{fromPath}\n→ {toPath}\n\nNOT: namespace referansları güncellenmedi — manuel kontrol edin.", "Tamam");
            }
            else
            {
                NexusLog.Error("AssetMigrationTools", nameof(MoveScriptSafe), label,
                    $"[Migration] {label} taşıma başarısız: {result}");
                EditorUtility.DisplayDialog("Taşıma Başarısız",
                    $"{label} taşınamadı.\nHata: {result}", "Tamam");
            }
        }

        [MenuItem("Ring Flow/Migration/Migrate RingColor.None to RingColor.Grey in Levels")]
        public static void MigrateRingColorNoneToGrey()
        {
            var assets = AssetDatabase.FindAssets("t:LevelDataSO", new[] { "Assets/Resources/Levels" });
            int patchedLevels = 0;
            int patchedRings = 0;

            foreach (var guid in assets)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var levelData = AssetDatabase.LoadAssetAtPath<LevelDataSO>(path);
                if (levelData == null || levelData.Data == null || levelData.Data.Poles == null) continue;

                bool isDirty = false;
                foreach (var pole in levelData.Data.Poles)
                {
                    if (pole.Rings == null) continue;
                    for (int i = 0; i < pole.Rings.Count; i++)
                    {
                        if (pole.Rings[i].Color == RingColor.None)
                        {
                            var r = pole.Rings[i];
                            r.Color = RingColor.Grey; // Migrate index 0 (None) to 15 (Grey)
                            pole.Rings[i] = r;
                            patchedRings++;
                            isDirty = true;
                        }
                    }
                }

                if (isDirty)
                {
                    EditorUtility.SetDirty(levelData);
                    patchedLevels++;
                }
            }

            if (patchedLevels > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                NexusLog.Info("AssetMigrationTools", nameof(MigrateRingColorNoneToGrey), "Complete",
                    $"[Migration] Successfully patched {patchedRings} rings across {patchedLevels} levels.");
                EditorUtility.DisplayDialog("Migrasyon Başarılı",
                    $"{patchedRings} adet halka ({patchedLevels} seviyede) RingColor.None -> RingColor.Grey olarak güncellendi ve kaydedildi.", "Tamam");
            }
            else
            {
                EditorUtility.DisplayDialog("Migrasyon Gerekli Değil", "Hiçbir seviyede RingColor.None renkli halka bulunmadı.", "Tamam");
            }
        }
    }
}
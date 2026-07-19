using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;
using System.IO;
using System.Collections.Generic;

namespace RingFlow.Editor
{
    public static class GeneratorCli
    {
        public static void GenerateAll()
        {
            Debug.Log("[GeneratorCli] Starting batch level generation...");
            
            var db = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>("Assets/Resources/Configs/GameConfigDatabase.asset");
            if (db == null)
            {
                Debug.LogError("[GeneratorCli] GameConfigDatabaseSO not found!");
                return;
            }

            string folderPath = "Assets/Resources/Levels";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            int okCount = 0;
            for (int level = 51; level <= 100; level++)
            {
                string assetPath = $"{folderPath}/Level_{level}.asset";
                if (File.Exists(assetPath))
                {
                    Debug.Log($"[GeneratorCli] Level {level} already exists. Skipping.");
                    continue;
                }

                int poles = db.GetPoleCountForLevel(level);
                int colors = db.GetColorCountForLevel(level);
                int maxCap = db.GetMaxCapacityForLevel(level);

                Debug.Log($"[GeneratorCli] Generating level {level} (poles={poles}, colors={colors}, maxCap={maxCap})...");
                var levelData = LevelGenerator.GenerateLevel(db, level, LevelGenerator.GetDeterministicSeed(level), poles, colors, maxCap);
                if (levelData == null)
                {
                    Debug.LogError($"[GeneratorCli] Failed to generate level {level}");
                    continue;
                }

                var levelSO = ScriptableObject.CreateInstance<LevelDataSO>();
                levelSO.Data = CloneLevelData(levelData);
                AssetDatabase.CreateAsset(levelSO, assetPath);
                okCount++;
                Debug.Log($"[GeneratorCli] Level {level} saved successfully.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[GeneratorCli] Batch generation completed: {okCount} levels generated.");
        }

        private static LevelData CloneLevelData(LevelData source)
        {
            var clone = new LevelData
            {
                LevelIndex = source.LevelIndex,
                Seed = source.Seed,
                TargetMoves = source.TargetMoves,
                LevelType = source.LevelType,
                PoleCount = source.PoleCount,
                PoleCapacity = source.PoleCapacity,
                ColorCount = source.ColorCount,
                EmptyPoleCount = source.EmptyPoleCount,
                DifficultyScore = source.DifficultyScore,
                IsTutorial = source.IsTutorial,
                RuleReferences = source.RuleReferences != null ? new List<string>(source.RuleReferences) : new List<string>(),
                IsChallenge = source.IsChallenge,
                ProgressionFlags = source.ProgressionFlags,
                Poles = new List<PoleData>()
            };

            foreach (var p in source.Poles)
            {
                var poleClone = new PoleData(p.RingCapacity) { IsLocked = p.IsLocked, PortalTargetId = p.PortalTargetId, Rings = new List<RingData>() };
                foreach (var r in p.Rings)
                    poleClone.Rings.Add(new RingData(r.Color, r.Type, r.AdditionalData));
                clone.Poles.Add(poleClone);
            }

            return clone;
        }
    }
}

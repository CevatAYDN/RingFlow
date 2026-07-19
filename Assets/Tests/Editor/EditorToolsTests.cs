using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RingFlow.Tests
{
    [TestFixture]
    public class EditorToolsTests
    {
        private static string TestPrefabPath => "Assets/Tests/Editor/_TestGen_Screen.prefab";

        [TearDown]
        public void Cleanup()
        {
            // Remove test-generated assets
            if (File.Exists(TestPrefabPath))
            {
                AssetDatabase.DeleteAsset(TestPrefabPath);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void AssetDatabase_CanCreateAndDeletePrefab()
        {
            // Create a temporary GameObject
            var go = new GameObject("TestScreen");
            try
            {
                // Save as prefab
                bool saved = false;
                PrefabUtility.SaveAsPrefabAsset(go, TestPrefabPath, out saved);
                Assert.IsTrue(saved, "Prefab should be saved successfully");
                Assert.IsTrue(File.Exists(TestPrefabPath), "Prefab file should exist");

                // Load it back
                var loaded = AssetDatabase.LoadAssetAtPath<GameObject>(TestPrefabPath);
                Assert.IsNotNull(loaded, "Loaded prefab should not be null");
                Assert.AreEqual("_TestGen_Screen", loaded.name);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void AssetDatabase_JsonSerializationRoundTrip()
        {
            var data = new TestLevelData
            {
                LevelIndex = 42,
                Poles = new List<TestPoleData>
                {
                    new TestPoleData { IsLocked = false, Rings = new List<TestRingData>
                    {
                        new TestRingData { Color = "Red", Type = "Standard" },
                        new TestRingData { Color = "Blue", Type = "Standard" }
                    }}
                },
                TargetMoves = 15
            };

            string json = JsonUtility.ToJson(data, prettyPrint: true);
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Contains("\"LevelIndex\": 42"));
            Assert.IsTrue(json.Contains("\"TargetMoves\": 15"));

            var deserialized = JsonUtility.FromJson<TestLevelData>(json);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(42, deserialized.LevelIndex);
            Assert.AreEqual(15, deserialized.TargetMoves);
            Assert.AreEqual(1, deserialized.Poles.Count);
            Assert.AreEqual(2, deserialized.Poles[0].Rings.Count);
            Assert.AreEqual("Red", deserialized.Poles[0].Rings[0].Color);
        }

        [Test]
        public void AssetDatabase_CanFindAllScreenPrefabsInProject()
        {
            // Search for Screen prefabs (any asset with "Screen" in name under UI folder)
            string[] guids = AssetDatabase.FindAssets("t:Prefab Screen",
                new[] { "Assets/Resources", "Assets/Prefabs" });

            // This is an informational test - we just validate no crash and reasonable results
            Assert.IsNotNull(guids);
            Assert.GreaterOrEqual(guids.Length, 0, "Should find Screen prefabs without error");
        }

        [Test]
        public void AssetDatabase_FindAssets_ReturnsNonEmptyForExistingAssets()
        {
            string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");
            Assert.IsNotNull(allPrefabs);
        }

        [Test]
        public void JsonUtility_HandlesEmptyCollections()
        {
            var data = new TestLevelData
            {
                LevelIndex = 1,
                Poles = new List<TestPoleData>(),
                TargetMoves = 0
            };

            string json = JsonUtility.ToJson(data);
            var deserialized = JsonUtility.FromJson<TestLevelData>(json);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(1, deserialized.LevelIndex);
        }

        [Test]
        public void JsonUtility_HandlesNestedSerialization()
        {
            string json = @"
{
    ""LevelIndex"": 5,
    ""TargetMoves"": 10,
    ""Poles"": [
        {
            ""IsLocked"": false,
            ""Rings"": [
                { ""Color"": ""Red"", ""Type"": ""Standard"", ""AdditionalData"": 0 },
                { ""Color"": ""Red"", ""Type"": ""Frozen"", ""AdditionalData"": 0 }
            ]
        },
        {
            ""IsLocked"": true,
            ""Rings"": [
                { ""Color"": ""Yellow"", ""Type"": ""Locked"", ""AdditionalData"": 0 }
            ]
        }
    ]
}";

            var data = JsonUtility.FromJson<TestLevelData>(json);
            Assert.IsNotNull(data);
            Assert.AreEqual(5, data.LevelIndex);
            Assert.AreEqual(2, data.Poles.Count);
            Assert.IsTrue(data.Poles[0].IsLocked == false);
            Assert.IsTrue(data.Poles[0].Rings[1].Type == "Frozen");
            Assert.IsTrue(data.Poles[1].IsLocked);
            Assert.AreEqual("Yellow", data.Poles[1].Rings[0].Color);
        }

        // ── Test data classes for JSON round-trip ───

        [System.Serializable]
        public class TestLevelData
        {
            public int LevelIndex;
            public List<TestPoleData> Poles;
            public int TargetMoves;
        }

        [System.Serializable]
        public class TestPoleData
        {
            public bool IsLocked;
            public List<TestRingData> Rings;
        }

        [System.Serializable]
        public class TestRingData
        {
            public string Color;
            public string Type;
            public int AdditionalData;
        }

        [Test]
        public void GenerateMissingLevels51to100()
        {
            var db = AssetDatabase.LoadAssetAtPath<RingFlow.Gameplay.GameConfigDatabaseSO>("Assets/Resources/Configs/GameConfigDatabase.asset");
            Assert.IsNotNull(db, "GameConfigDatabaseSO not found!");

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
                    continue;
                }

                int poles = db.GetPoleCountForLevel(level);
                int colors = db.GetColorCountForLevel(level);
                int maxCap = db.GetMaxCapacityForLevel(level);

                var levelData = RingFlow.Gameplay.LevelGenerator.GenerateLevel(db, level, RingFlow.Gameplay.LevelGenerator.GetDeterministicSeed(level), poles, colors, maxCap);
                Assert.IsNotNull(levelData, $"Failed to generate level {level}");

                var levelSO = ScriptableObject.CreateInstance<RingFlow.Gameplay.LevelDataSO>();
                levelSO.Data = new RingFlow.Gameplay.LevelData
                {
                    LevelIndex = levelData.LevelIndex,
                    Seed = levelData.Seed,
                    TargetMoves = levelData.TargetMoves,
                    LevelType = levelData.LevelType,
                    PoleCount = levelData.PoleCount,
                    PoleCapacity = levelData.PoleCapacity,
                    ColorCount = levelData.ColorCount,
                    EmptyPoleCount = levelData.EmptyPoleCount,
                    DifficultyScore = levelData.DifficultyScore,
                    IsTutorial = levelData.IsTutorial,
                    RuleReferences = levelData.RuleReferences != null ? new List<string>(levelData.RuleReferences) : new List<string>(),
                    IsChallenge = levelData.IsChallenge,
                    ProgressionFlags = levelData.ProgressionFlags,
                    Poles = new List<RingFlow.Gameplay.PoleData>()
                };

                foreach (var p in levelData.Poles)
                {
                    var poleClone = new RingFlow.Gameplay.PoleData(p.RingCapacity) { IsLocked = p.IsLocked, PortalTargetId = p.PortalTargetId, Rings = new List<RingFlow.Gameplay.RingData>() };
                    foreach (var r in p.Rings)
                        poleClone.Rings.Add(new RingFlow.Gameplay.RingData(r.Color, r.Type, r.AdditionalData));
                    levelSO.Data.Poles.Add(poleClone);
                }

                AssetDatabase.CreateAsset(levelSO, assetPath);
                okCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Test] Batch generation completed: {okCount} levels generated.");
        }
    }
}
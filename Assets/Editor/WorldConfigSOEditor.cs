using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    [CustomEditor(typeof(WorldConfigSO))]
    public class WorldConfigSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var config = (WorldConfigSO)target;
            var database = Resources.Load<GameConfigDatabaseSO>("GameConfigDatabase");

            EditorGUILayout.LabelField("World Configuration", RingFlowEditorUtils.HeaderStyle);
            EditorGUILayout.Space(4f);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("World Identity", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                config.WorldIndex = EditorGUILayout.IntField("World Index (0–39)", config.WorldIndex);
                config.Theme = EditorGUILayout.TextField("Theme Display Name", config.Theme);
                config.UnlockedByWorldIndex = EditorGUILayout.IntField("Unlocked by World Index", config.UnlockedByWorldIndex);
                config.IsEventWorld = EditorGUILayout.Toggle("Is Boss World", config.IsEventWorld);

                if (config.IsEventWorld)
                {
                    EditorGUILayout.HelpBox(
                        "Boss worlds occur every 5 worlds. Boss levels give increased rewards (500 coins, 50 XP) and mark progression milestones.",
                        MessageType.Info);
                }
            }

            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField("Level Info", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (database != null)
                {
                    int startLevel = WorldConfigSO.ToAbsoluteLevel(database, config.WorldIndex, 0);
                    int endLevel = WorldConfigSO.ToAbsoluteLevel(database, config.WorldIndex, database.LevelsPerWorld - 1);
                    EditorGUILayout.LabelField("Level Range", $"{startLevel} – {endLevel}");

                    bool isBoss = WorldConfigSO.IsBossLevel(database, endLevel);
                    EditorGUILayout.LabelField("Last Level is Boss", isBoss ? "Yes" : "No");
                }
                else
                {
                    EditorGUILayout.HelpBox("GameConfigDatabase.asset not found in Resources!", MessageType.Warning);
                }
            }

            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField("Mechanic Assignment", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                int startLevel = 0;
                if (database != null)
                {
                    startLevel = WorldConfigSO.ToAbsoluteLevel(database, config.WorldIndex, 0);
                }
                WorldMechanicType worldMechanic = WorldMechanicType.None;
                DifficultyBand band = DifficultyBand.Tutorial;
                var allowedMechanics = new System.Collections.Generic.List<WorldMechanicType>();
                
                if (database != null)
                {
                    band = database.GetBandForLevel(startLevel);
                    var intensity = database.GetMechanicIntensityForLevel(startLevel);
                    allowedMechanics = database.GetAllowedMechanicsForLevel(startLevel);
                    worldMechanic = database.GetMechanicForWorld(config.WorldIndex);

                    EditorGUILayout.LabelField($"Assigned Mechanic", $"{worldMechanic}");
                    EditorGUILayout.LabelField($"Difficulty Band", $"{band}");
                    EditorGUILayout.LabelField($"Mechanic Intensity", $"{intensity}");
                    EditorGUILayout.LabelField($"Band-Allowed Mechanics", $"{string.Join(", ", allowedMechanics)}");
                }
                else
                {
                    EditorGUILayout.HelpBox("GameConfigDatabase.asset not found in Resources!", MessageType.Warning);
                }

                // Show mechanic injection preview
                if (worldMechanic == WorldMechanicType.None)
                {
                    EditorGUILayout.HelpBox("No special mechanic in this world. Only standard rings.", MessageType.None);
                }
                else if (worldMechanic == WorldMechanicType.RandomPool1 ||
                         worldMechanic == WorldMechanicType.RandomPool2 ||
                         worldMechanic == WorldMechanicType.RandomPool3)
                {
                    int poolSize = worldMechanic == WorldMechanicType.RandomPool3 ? 3 :
                                   worldMechanic == WorldMechanicType.RandomPool2 ? 2 : 1;
                    EditorGUILayout.HelpBox(
                        $"RandomPool mechanic: picks {poolSize} type(s) from band-allowed mechanics. " +
                        $"Band {band} allows: {string.Join(", ", allowedMechanics)}",
                        MessageType.None);
                }
                else
                {
                    int levelsPerWorld = database != null ? database.LevelsPerWorld : 8;
                    EditorGUILayout.HelpBox(
                        $"This world's primary mechanic ({worldMechanic}) always injects, bypassing band gating. " +
                        $"The world teaches this mechanic to the player over {levelsPerWorld} levels.",
                        MessageType.Info);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(config);
            }
        }
    }
}

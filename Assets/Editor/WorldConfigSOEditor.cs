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
                int startLevel = WorldConfigSO.ToAbsoluteLevel(config.WorldIndex, 0);
                int endLevel = WorldConfigSO.ToAbsoluteLevel(config.WorldIndex, WorldConfigSO.LevelsPerWorld - 1);
                EditorGUILayout.LabelField("Level Range", $"{startLevel} – {endLevel}");

                bool isBoss = WorldConfigSO.IsBossLevel(endLevel);
                EditorGUILayout.LabelField("Last Level is Boss", isBoss ? "Yes" : "No");
            }

            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField("Mechanic Assignment", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                int startLevel = WorldConfigSO.ToAbsoluteLevel(config.WorldIndex, 0);
                var band = DifficultyCurve.BandForLevel(startLevel);
                var intensity = GameConfigDatabaseSO.Instance.GetMechanicIntensityForLevel(startLevel);
                var allowedMechanics = GameConfigDatabaseSO.Instance.GetAllowedMechanicsForLevel(startLevel);
                var worldMechanic = GameConfigDatabaseSO.Instance.GetMechanicForWorld(config.WorldIndex);

                EditorGUILayout.LabelField($"Assigned Mechanic", $"{worldMechanic}");
                EditorGUILayout.LabelField($"Difficulty Band", $"{band}");
                EditorGUILayout.LabelField($"Mechanic Intensity", $"{intensity}");
                EditorGUILayout.LabelField($"Band-Allowed Mechanics", $"{string.Join(", ", allowedMechanics)}");

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
                    EditorGUILayout.HelpBox(
                        $"This world's primary mechanic ({worldMechanic}) always injects, bypassing band gating. " +
                        $"The world teaches this mechanic to the player over {WorldConfigSO.LevelsPerWorld} levels.",
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

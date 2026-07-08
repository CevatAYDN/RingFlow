using UnityEditor;
using UnityEngine;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Editor
{
    public sealed class AdTesterSection : EditorSection
    {
        private string _placement = "default";
        private string _lastResult = "No ad tested yet.";

        public override string DisplayName => "Ad Tester (Editor Mock)";
        public override string PrefKey => EditorPrefsKeys.FoldAdTester;

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox(
                    "Tests ad placements against the mock adapter. In production, replace the adapter with AdMob/Unity Ads.",
                    MessageType.Info);

                _placement = EditorGUILayout.TextField("Placement", _placement);

                if (!Application.isPlaying)
                {
                    EditorGUILayout.LabelField("(Ad tests require PlayMode — Nexus must initialize the AdService.)");
                    return;
                }

                var context = NexusRuntime.CurrentContext;
                if (context == null)
                {
                    EditorGUILayout.HelpBox("Nexus runtime context is not available yet.", MessageType.Warning);
                    return;
                }
                var ads = context.TryResolve<IAdService>();
                if (ads == null)
                {
                    EditorGUILayout.HelpBox("IAdService not registered in current context.", MessageType.Warning);
                    return;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Show Rewarded"))
                    {
                        SafeInvoke(() => ads.ShowRewarded(_placement, success =>
                        {
                            _lastResult = $"Rewarded '{_placement}': success={success}";
                        }));
                    }
                    if (GUILayout.Button("Show Interstitial"))
                    {
                        SafeInvoke(() => ads.ShowInterstitial(_placement, () =>
                        {
                            _lastResult = $"Interstitial '{_placement}' closed";
                        }));
                    }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Show Banner"))  SafeInvoke(() => ads.ShowBanner(_placement, "bottom"));
                    if (GUILayout.Button("Hide Banner"))  SafeInvoke(() => ads.HideBanner());
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Last result:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(_lastResult);
            }
        }

        private static void SafeInvoke(System.Action action)
        {
            try { action?.Invoke(); }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[AdTester] {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}

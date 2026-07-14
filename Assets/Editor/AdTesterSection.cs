using UnityEditor;
using UnityEngine;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Editor
{
    public sealed class AdTesterSection : EditorSection
    {
        private const string DefaultPlacement = "default";
        private const string DefaultLastResult = "No ad tested yet.";

        private string _placement = DefaultPlacement;
        private string _lastResult = DefaultLastResult;
        private bool _placementLoaded;

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

                EditorGUI.BeginChangeCheck();
                _placement = EditorGUILayout.TextField(
                    new GUIContent("Placement", "AdMob/Unity Ads placement ID. Saved across domain reloads."), _placement);
                if (EditorGUI.EndChangeCheck())
                    EditorPrefs.SetString(EditorPrefsKeys.AdPlacement, _placement);

                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox(
                        "Ad tests require PlayMode — Nexus must initialize the AdService.",
                        MessageType.Info);
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

                bool narrow = RingFlowEditorUtils.IsNarrowWidth(520f);
                if (narrow)
                {
                    if (GUILayout.Button("Show Rewarded", GUILayout.Height(ButtonHeight)))
                    {
                        SafeInvoke(() => ads.ShowRewarded(_placement, success =>
                        {
                            _lastResult = $"Rewarded '{_placement}': success={success}";
                        }));
                    }
                    if (GUILayout.Button("Show Interstitial", GUILayout.Height(ButtonHeight)))
                    {
                        SafeInvoke(() => ads.ShowInterstitial(_placement, () =>
                        {
                            _lastResult = $"Interstitial '{_placement}' closed";
                        }));
                    }
                    if (GUILayout.Button("Show Banner", GUILayout.Height(ButtonHeight)))  SafeInvoke(() => ads.ShowBanner(_placement, "bottom"));
                    if (GUILayout.Button("Hide Banner", GUILayout.Height(ButtonHeight)))  SafeInvoke(() => ads.HideBanner());
                }
                else
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Show Rewarded", GUILayout.Height(ButtonHeight)))
                        {
                            SafeInvoke(() => ads.ShowRewarded(_placement, success =>
                            {
                                _lastResult = $"Rewarded '{_placement}': success={success}";
                            }));
                        }
                        if (GUILayout.Button("Show Interstitial", GUILayout.Height(ButtonHeight)))
                        {
                            SafeInvoke(() => ads.ShowInterstitial(_placement, () =>
                            {
                                _lastResult = $"Interstitial '{_placement}' closed";
                            }));
                        }
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Show Banner", GUILayout.Height(ButtonHeight)))  SafeInvoke(() => ads.ShowBanner(_placement, "bottom"));
                        if (GUILayout.Button("Hide Banner", GUILayout.Height(ButtonHeight)))  SafeInvoke(() => ads.HideBanner());
                    }
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
                NexusLog.Warn("AdTesterSection", nameof(SafeInvoke), "AdInvoke", $"[AdTester] {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}

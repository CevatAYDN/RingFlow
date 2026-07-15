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

            if (!_placementLoaded)
            {
                _placement = EditorPrefs.GetString(EditorPrefsKeys.AdPlacement, DefaultPlacement);
                _placementLoaded = true;
            }

            RingFlowEditorUtils.BeginSectionBox("Reklam Test Cihazı (Editor Mock)", "Reklam yerleşimlerini mock reklam bağdaştırıcısıyla test edin.");

            EditorGUI.BeginChangeCheck();
            _placement = EditorGUILayout.TextField(
                new GUIContent("Yerleşim Kimliği (Placement)", "AdMob/Unity Ads placement ID. Saved across domain reloads."), _placement);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetString(EditorPrefsKeys.AdPlacement, _placement);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Reklam testleri için PlayMode gereklidir — Nexus, AdService'i başlatmalıdır.",
                    MessageType.Info);
                RingFlowEditorUtils.EndSectionBox();
                return;
            }

            var context = NexusRuntime.CurrentContext;
            if (context == null)
            {
                EditorGUILayout.HelpBox("Nexus çalışma zamanı bağlamı henüz mevcut değil.", MessageType.Warning);
                RingFlowEditorUtils.EndSectionBox();
                return;
            }
            var ads = context.TryResolve<IAdService>();
            if (ads == null)
            {
                EditorGUILayout.HelpBox("Mevcut bağlamda IAdService kayıt edilmemiş.", MessageType.Warning);
                RingFlowEditorUtils.EndSectionBox();
                return;
            }

            bool narrow = RingFlowEditorUtils.IsNarrowWidth(520f);
            if (narrow)
            {
                if (GUILayout.Button("Ödüllü Reklam Göster (Rewarded)", GUILayout.Height(ButtonHeight)))
                {
                    SafeInvoke(() => ads.ShowRewarded(_placement, success =>
                    {
                        _lastResult = $"Rewarded '{_placement}': success={success}";
                    }));
                }
                if (GUILayout.Button("Geçiş Reklamı Göster (Interstitial)", GUILayout.Height(ButtonHeight)))
                {
                    SafeInvoke(() => ads.ShowInterstitial(_placement, () =>
                    {
                        _lastResult = $"Interstitial '{_placement}' closed";
                    }));
                }
                if (GUILayout.Button("Banner Göster", GUILayout.Height(ButtonHeight)))  SafeInvoke(() => ads.ShowBanner(_placement, "bottom"));
                if (GUILayout.Button("Banner Gizle", GUILayout.Height(ButtonHeight)))  SafeInvoke(() => ads.HideBanner());
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Ödüllü Reklam Göster (Rewarded)", GUILayout.Height(ButtonHeight)))
                    {
                        SafeInvoke(() => ads.ShowRewarded(_placement, success =>
                        {
                            _lastResult = $"Rewarded '{_placement}': success={success}";
                        }));
                    }
                    if (GUILayout.Button("Geçiş Reklamı Göster (Interstitial)", GUILayout.Height(ButtonHeight)))
                    {
                        SafeInvoke(() => ads.ShowInterstitial(_placement, () =>
                        {
                            _lastResult = $"Interstitial '{_placement}' closed";
                        }));
                    }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Banner Göster", GUILayout.Height(ButtonHeight)))  SafeInvoke(() => ads.ShowBanner(_placement, "bottom"));
                    if (GUILayout.Button("Banner Gizle", GUILayout.Height(ButtonHeight)))  SafeInvoke(() => ads.HideBanner());
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Son Sonuç:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(_lastResult);
            
            RingFlowEditorUtils.EndSectionBox();
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

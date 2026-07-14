#if UNITY_EDITOR
using System.Linq;
using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay;
using RingFlow.Gameplay.Diagnostics;
using RingFlow.Gameplay.Economy;
using RingFlow.Gameplay.Localization;
using RingFlow.Gameplay.Strategies;
using RingFlow.Gameplay.Views;
using UnityEditor;
using UnityEngine;

namespace RingFlow.Editor
{
    public sealed class DiagnosticsSection : EditorSection
    {
        private Vector2 _scrollPos;
        private string _filter = "";
        private bool _autoScroll = true;

        public override string DisplayName => "Game Diagnostics & Trace Logs";
        public override string PrefKey => EditorPrefsKeys.FoldDiagnostics;

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var context = NexusRuntime.CurrentContext;
                if (context == null)
                {
                    EditorGUILayout.HelpBox("No active Nexus context. Enter Play Mode to trace logs.", MessageType.Info);
                    return;
                }

                var diag = context.TryResolve<IGameDiagnostics>();
                if (diag == null)
                {
                    EditorGUILayout.HelpBox("IGameDiagnostics not registered. Check GameplayLifecycle configuration.", MessageType.Error);
                    return;
                }

                // Controls
                EditorGUILayout.BeginHorizontal();
                diag.IsEnabled = EditorGUILayout.Toggle("Enabled", diag.IsEnabled);
                if (GUILayout.Button("Clear", GUILayout.Width(60))) diag.Clear();
                if (GUILayout.Button("Export Report", GUILayout.Width(100))) ExportReport(diag);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4f);

                // --- Data-Driven Denetimi ---
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Data-Driven Denetimi", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("Tüm ScriptableObject konfigürasyon varlıklarının varlığını ve geçerliliğini denetler.", MessageType.Info);

                    if (GUILayout.Button("Data-Driven Varlıkları Doğrula", GUILayout.Height(24)))
                        RunDataDrivenValidation();
                }

                // Filter
                _filter = EditorGUILayout.TextField("Filter", _filter);
                _autoScroll = EditorGUILayout.Toggle("Auto-scroll", _autoScroll);

                // Entry list
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(300));
                
                var entries = diag.Entries;
                if (_autoScroll)
                {
                    _scrollPos = new Vector2(0, float.MaxValue);
                }

                // Reverse iteration without LINQ allocation
                for (int ei = entries.Count - 1; ei >= 0; ei--)
                {
                    var entry = entries[ei];
                    if (!string.IsNullOrEmpty(_filter) &&
                        !entry.Category.Contains(_filter) &&
                        !entry.Message.Contains(_filter))
                        continue;

                    var color = entry.Severity switch
                    {
                        DiagnosticSeverity.Critical => Color.red,
                        DiagnosticSeverity.Error => EditorPaths.EditorColors.Error,
                        DiagnosticSeverity.Warning => Color.yellow,
                        DiagnosticSeverity.Info => Color.white,
                        _ => Color.gray
                    };

                    GUI.color = color;
                    EditorGUILayout.LabelField(entry.ToString(), EditorStyles.wordWrappedMiniLabel);
                    GUI.color = Color.white;
                }

                EditorGUILayout.EndScrollView();

                // Summary
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Total entries: {entries.Count}", EditorStyles.miniBoldLabel);
            }
        }

        private void RunDataDrivenValidation()
        {
            System.Text.StringBuilder report = new();
            report.AppendLine("=== Data-Driven Denetim Raporu ===");
            report.AppendLine($"Tarih: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();

            int passCount = 0;
            int failCount = 0;

            void CheckAsset<T>(string key, string label) where T : ScriptableObject
            {
                var obj = Resources.Load<T>(key);
                if (obj == null)
                {
                    report.AppendLine($"[BAŞARISIZ] {label} ({key}) — Bulunamadı!");
                    failCount++;
                }
                else
                {
                    report.AppendLine($"[BAŞARILI] {label} ({key}) — Yüklendi");
                    passCount++;
                }
            }

            // Ana config varlıkları
            CheckAsset<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey, "Oyun Veritabanı (GameConfigDatabase)");
            CheckAsset<GameFeelConfigSO>(EditorPaths.GameFeelConfigKey, "Oyun Hissiyatı (Game Feel)");
            CheckAsset<RingColorPaletteSO>(EditorPaths.RingColorPaletteKey, "Halka Renk Paleti");
            CheckAsset<AudioConfigSO>(EditorPaths.AudioConfigKey, "Ses Yapılandırması (Audio)");
            CheckAsset<UIThemeConfigSO>(EditorPaths.UIThemeConfigKey, "Arayüz Teması (UI Theme)");

            // Data-driven yeni varlıklar
            CheckAsset<RingFlow.Gameplay.Economy.StoreCatalogSO>(EditorPaths.StoreCatalogKey, "Mağaza Kataloğu (StoreCatalog)");
            CheckAsset<RingFlow.Gameplay.Localization.LocalizationConfigSO>(EditorPaths.LocalizationConfigKey, "Yerelleştirme (LocalizationConfig)");
            CheckAsset<RingFlow.Gameplay.Strategies.RingMechanicDataSO>(EditorPaths.RingMechanicDataKey, "Halka Mekanik Verisi (RingMechanicData)");
            CheckAsset<RingFlow.Gameplay.Views.ThemeSkinDatabaseSO>(EditorPaths.ThemeSkinDatabaseKey, "Tema/Skin Veritabanı (ThemeSkinDatabase)");

            report.AppendLine();
            report.AppendLine($"Toplam: {passCount + failCount} | Geçen: {passCount} | Başarısız: {failCount}");

            NexusLog.Info("DiagnosticsSection", "RunDataDrivenValidation", "", report.ToString());
            EditorUtility.DisplayDialog("Data-Driven Denetimi", report.ToString(), "Tamam");
        }

        private void ExportReport(IGameDiagnostics diag)
        {
            string path = EditorUtility.SaveFilePanel("Export Diagnostics", Application.dataPath, 
                $"diagnostics_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt", "txt");
            if (string.IsNullOrEmpty(path)) return;

            using var writer = new System.IO.StreamWriter(path);
            writer.WriteLine($"=== RingFlow Diagnostics Report ===");
            writer.WriteLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"Unity Version: {Application.unityVersion}");
            writer.WriteLine($"Platform: {Application.platform}");
            writer.WriteLine();

            // Count by severity
            writer.WriteLine("--- Summary ---");
            var groups = diag.Entries.GroupBy(e => e.Severity);
            foreach (var g in groups.OrderByDescending(g => g.Key))
            {
                writer.WriteLine($"  {g.Key}: {g.Count()}");
            }
            writer.WriteLine($"  Total: {diag.Entries.Count}");
            writer.WriteLine();

            // Errors and warnings first
            writer.WriteLine("--- Errors & Warnings ---");
            foreach (var entry in diag.Entries.Where(e => e.Severity >= DiagnosticSeverity.Warning))
            {
                writer.WriteLine($"  {entry}");
                if (!string.IsNullOrEmpty(entry.StackTrace))
                {
                    writer.WriteLine($"    Stack: {entry.StackTrace}");
                }
            }
            writer.WriteLine();

            // All entries
            writer.WriteLine("--- Full Log ---");
            foreach (var entry in diag.Entries)
            {
                writer.WriteLine($"  {entry}");
            }

            writer.Flush();
            EditorUtility.RevealInFinder(path);
            NexusLog.Info("DiagnosticsSection", "ExportReport", "Report", $"[Diagnostics] Report exported to: {path}");
        }
    }
}
#endif

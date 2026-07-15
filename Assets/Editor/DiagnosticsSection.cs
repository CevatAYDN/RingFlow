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

            RingFlowEditorUtils.BeginSectionBox("Sinyal & Hata İzleme Paneli", "Play Mode esnasında Nexus sinyallerini ve loglarını izleyin.");

            var context = NexusRuntime.CurrentContext;
            if (context == null)
            {
                EditorGUILayout.HelpBox("Aktif Nexus bağlamı yok. Logları izlemek için Play Mode'a girin.", MessageType.Info);
                RingFlowEditorUtils.EndSectionBox();
                return;
            }

            var diag = context.TryResolve<IGameDiagnostics>();
            if (diag == null)
            {
                EditorGUILayout.HelpBox("IGameDiagnostics kayıt edilmedi. GameplayLifecycle ayarlarını kontrol edin.", MessageType.Error);
                RingFlowEditorUtils.EndSectionBox();
                return;
            }

            // Controls
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                diag.IsEnabled = EditorGUILayout.Toggle("İzleme Etkin", diag.IsEnabled);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Temizle", EditorStyles.miniButton, GUILayout.Width(60f))) diag.Clear();
                if (GUILayout.Button("Raporu Dışa Aktar", EditorStyles.miniButton, GUILayout.Width(120f))) ExportReport(diag);
            }
            EditorGUILayout.Space(4f);

            // --- Data-Driven Denetimi ---
            RingFlowEditorUtils.BeginSectionBox("Data-Driven Denetimi", "Tüm ScriptableObject konfigürasyon varlıklarının varlığını ve geçerliliğini denetler.");
            if (GUILayout.Button("Data-Driven Varlıkları Doğrula", GUILayout.Height(24)))
                RunDataDrivenValidation();
            RingFlowEditorUtils.EndSectionBox();

            // Filter
            bool narrow = EditorGUIUtility.currentViewWidth < 480f;
            if (narrow)
            {
                _filter = EditorGUILayout.TextField("Filtrele", _filter);
                _autoScroll = EditorGUILayout.Toggle("Otomatik Kaydır", _autoScroll);
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _filter = EditorGUILayout.TextField("Filtrele", _filter);
                    _autoScroll = EditorGUILayout.Toggle("Otomatik Kaydır", _autoScroll, GUILayout.Width(110f));
                }
            }

            // Entry list
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(300));
            
            var entries = diag.Entries;
            if (_autoScroll)
            {
                _scrollPos = new Vector2(0, float.MaxValue);
            }

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
            EditorGUILayout.LabelField($"Toplam kayıt: {entries.Count}", EditorStyles.miniBoldLabel);
            
            RingFlowEditorUtils.EndSectionBox();
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

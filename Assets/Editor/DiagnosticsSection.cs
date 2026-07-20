#if UNITY_EDITOR
using System.Linq;
using Unity.Profiling;
using Nexus.Core;
using Nexus.Core.Services;
using RingFlow.Gameplay;
using RingFlow.Gameplay.UI;
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
        private bool _cachedNarrow;

        // GDD §75 Profiler bütçe takip değişkenleri
        private float _lastFrameMs;
        private float _peakFrameMs;
        private float _lastGcAllocKb;
        private float _peakGcAllocKb;
        private int _lastDrawCalls;
        private int _peakDrawCalls;
        private double _lastSampleTime;

        // ProfilerRecorder for draw calls — must be created/disposed alongside OnGUI lifecycle.
        // Using SetPass Calls as the draw call proxy (reliable across render pipelines).
        private ProfilerRecorder _drawCallRecorder;

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

            // --- GDD §75 Performans Bütçesi ---
            DrawProfilerBudgetPanel();

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
                    LogAssetDetails(obj, label);
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
            CheckAsset<ScreenRegistrySO>(EditorPaths.ScreenRegistryKey, "Ekran Kayıt Defteri (Screen Registry)");

            report.AppendLine();
            report.AppendLine($"Toplam: {passCount + failCount} | Geçen: {passCount} | Başarısız: {failCount}");

            NexusLog.Info("DiagnosticsSection", "RunDataDrivenValidation", "", report.ToString());
            EditorUtility.DisplayDialog("Data-Driven Denetimi", report.ToString(), "Tamam");
        }

        private void LogAssetDetails(ScriptableObject obj, string label)
        {
            System.Text.StringBuilder details = new();
            details.AppendLine($"=== [DETAYLI AUDIT] {label} ===");

            if (obj is GameConfigDatabaseSO db)
            {
                details.AppendLine($"• Toplam Seviye: {db.TotalLevels}");
                details.AppendLine($"• Dünya Başına Seviye: {db.LevelsPerWorld}");
                details.AppendLine($"• Toplam Dünya: {db.TotalWorlds}");
                details.AppendLine($"• Zorluk Bandları ({db.DifficultyBands.Count}):");
                foreach (var band in db.DifficultyBands)
                {
                    details.AppendLine($"  - {band.Band}: Maks Seviye={band.MaxLevel}, Kapasite={band.MaxCapacity}, Mekanikler={string.Join(", ", band.AllowedMechanics)}");
                }
                details.AppendLine($"• Renk İlerleme Eğrisi ({db.ColorCurve.Count}):");
                foreach (var pt in db.ColorCurve)
                {
                    details.AppendLine($"  - Eşik Seviye {pt.LevelThreshold} -> {pt.ColorCount} Renk");
                }
                details.AppendLine($"• Dünyalar ({db.Worlds.Count}):");
                foreach (var w in db.Worlds)
                {
                    details.AppendLine($"  - Dünya {w.WorldIndex + 1}: Tema={w.Theme}, Mekanik={w.MechanicType}");
                }
            }
            else if (obj is GameFeelConfigSO feel)
            {
                details.AppendLine($"• Move Duration: {feel.MoveDuration}");
                details.AppendLine($"• Ring Scale Torus: {feel.RingScaleTorus}");
                details.AppendLine($"• Confetti Count: {feel.ConfettiCount}");
            }
            else if (obj is RingColorPaletteSO palette)
            {
                var entriesField = typeof(RingColorPaletteSO).GetField("_entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                int count = entriesField != null && entriesField.GetValue(palette) is System.Array arr ? arr.Length : 0;
                details.AppendLine($"• Tanımlı Renkler: {count}");
            }
            else if (obj is AudioConfigSO audio)
            {
                details.AppendLine($"• Sample Rate: {audio.SampleRate}");
                details.AppendLine($"• Move Volume: {audio.Move.Volume}");
                details.AppendLine($"• Win Volume: {audio.Win.Volume}");
                details.AppendLine($"• Error Volume: {audio.Error.Volume}");
                details.AppendLine($"• Explosion Volume: {audio.Explosion.Volume}");
                details.AppendLine($"• BGM Master Volume: {audio.Bgm.MasterVolume}");
            }
            else if (obj is UIThemeConfigSO theme)
            {
                details.AppendLine($"• Primary Color: {theme.PrimaryColor}");
                details.AppendLine($"• Accent Color: {theme.AccentColor}");
                details.AppendLine($"• BG Color: {theme.BgColor}");
            }
            else if (obj is StoreCatalogSO catalog)
            {
                details.AppendLine($"• Ürün Sayısı: {catalog.Products?.Count ?? 0}");
                if (catalog.Products != null)
                {
                    foreach (var p in catalog.Products)
                        details.AppendLine($"  - {p.Id}: Price={p.PriceString}, Type={p.Type}");
                }
            }
            else if (obj is LocalizationConfigSO loc)
            {
                details.AppendLine($"• Tanımlı Diller: {loc.Languages?.Count ?? 0}");
                if (loc.Languages != null)
                {
                    foreach (var l in loc.Languages)
                        details.AppendLine($"  - {l.Code}: {l.DisplayName} (RTL={l.IsRTL})");
                }
            }
            else if (obj is RingMechanicDataSO mech)
            {
                details.AppendLine($"• Mekanik Sayısı: {mech.Mechanics?.Count ?? 0}");
                if (mech.Mechanics != null)
                {
                    foreach (var m in mech.Mechanics)
                        details.AppendLine($"  - {m.Type}: Icon={(m.Icon != null ? m.Icon.name : "Yok")}");
                }
            }
            else if (obj is ThemeSkinDatabaseSO skin)
            {
                details.AppendLine($"• Temalar: {skin.Entries?.Count ?? 0}");
            }
            else if (obj is ScreenRegistrySO registry)
            {
                details.AppendLine($"• Kayıtlı Ekranlar: {registry.Mappings?.Count ?? 0}");
                if (registry.Mappings != null)
                {
                    foreach (var m in registry.Mappings)
                        details.AppendLine($"  - {m.Screen}: Prefab={m.PrefabPath}, View={m.ViewTypeName}");
                }
            }

            NexusLog.Info("DiagnosticsSection", "LogAssetDetails", obj.name, details.ToString());
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

        // ─────────────────────────────────────────────────────────────────
        //  GDD §75 Profiler Bütçe Paneli
        // ─────────────────────────────────────────────────────────────────

        private void DrawProfilerBudgetPanel()
        {
            // OPEN-5: Runtime Profiler bütçe paneli.
            // GDD §75 hedefleri:
            //   Frame Time  < 14.0 ms  (kritik: 16.6 ms = 60 FPS)
            //   Draw Calls  < 80       (kritik: 120)
            //   GC Alloc    < 1.0 KB/frame (kritik: 4.0 KB)
            //   RAM         < 150 MB   (kritik: 220 MB)
            //
            // Bu panel PlayMode'da çalışırken anlık değerleri gösterir.
            // Değerleri örneklemek için "Örnek Al" butonuna basılır.
            // Kırmızı = kritik eşik aşıldı, sarı = hedef aşıldı, yeşil = OK.

            if (!Application.isPlaying)
            {
                // Dispose recorder when leaving play mode to avoid resource leak.
                if (_drawCallRecorder.Valid)
                    _drawCallRecorder.Dispose();

                RingFlowEditorUtils.BeginSectionBox("GDD §75 Performans Bütçesi",
                    "Play Mode'da anlık frame time, draw call ve GC alloc değerlerini ölçer.");
                EditorGUILayout.HelpBox("Ölçüm için Play Mode'a girin.", MessageType.Info);
                RingFlowEditorUtils.EndSectionBox();
                return;
            }

            // Ensure recorder is running while in play mode.
            if (!_drawCallRecorder.Valid)
                _drawCallRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");

            // Throttle sampling to once per second to avoid Profiler.GetTotalAllocatedMemory
            // overhead on every OnGUI call (Profiler API is not free).
            bool narrow = RingFlowEditorUtils.IsNarrowWidth(620f);
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastSampleTime >= 1.0)
            {
                _lastSampleTime = now;
                _lastFrameMs    = Time.deltaTime * 1000f;
                _lastGcAllocKb  = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1024f / 1024f; // MB

                // Use FrameTimingManager for more accurate CPU frame time.
                UnityEngine.FrameTimingManager.CaptureFrameTimings();
                var timings = new UnityEngine.FrameTiming[1];
                uint captured = UnityEngine.FrameTimingManager.GetLatestTimings(1, timings);
                if (captured > 0)
                    _lastFrameMs = (float)timings[0].cpuFrameTime;

                // Read draw call count from ProfilerRecorder.
                if (_drawCallRecorder.Valid && _drawCallRecorder.Count > 0)
                    _lastDrawCalls = (int)_drawCallRecorder.LastValue;

                if (_lastFrameMs   > _peakFrameMs)   _peakFrameMs   = _lastFrameMs;
                if (_lastGcAllocKb > _peakGcAllocKb) _peakGcAllocKb = _lastGcAllocKb;
                if (_lastDrawCalls > _peakDrawCalls)  _peakDrawCalls  = _lastDrawCalls;
            }

            RingFlowEditorUtils.BeginSectionBox("GDD §75 Performans Bütçesi",
                "Anlık frame time ve bellek ölçümleri. Kırmızı = kritik eşik aşıldı.");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Örnek Al", EditorStyles.miniButton, GUILayout.Width(80f)))
                    _lastSampleTime = 0; // force resample

                if (GUILayout.Button("Peak Sıfırla", EditorStyles.miniButton, GUILayout.Width(90f)))
                {
                    _peakFrameMs   = 0f;
                    _peakGcAllocKb = 0f;
                    _peakDrawCalls = 0;
                }
            }

            EditorGUILayout.Space(4f);

            // Helper: draw a metric row with color coding
            void DrawMetricRow(string label, float value, float target, float critical, string unit, bool lowerIsBetter = true)
            {
                bool overTarget   = lowerIsBetter ? value > target   : value < target;
                bool overCritical = lowerIsBetter ? value > critical  : value < critical;
                Color rowColor = overCritical ? EditorPaths.EditorColors.Error
                               : overTarget   ? EditorPaths.EditorColors.Warning
                               : EditorPaths.EditorColors.Success;

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (!narrow)
                    {
                        var prevColor = GUI.color;
                        GUI.color = rowColor;
                        EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(140f));
                        GUI.color = prevColor;
                        EditorGUILayout.LabelField($"{value:F2} {unit}", GUILayout.Width(100f));
                        EditorGUILayout.LabelField($"Hedef: < {target} {unit}", EditorStyles.miniLabel, GUILayout.Width(120f));
                        EditorGUILayout.LabelField($"Kritik: < {critical} {unit}", EditorStyles.miniLabel, GUILayout.MinWidth(80f));
                    }
                    else
                    {
                        var prevColor = GUI.color;
                        GUI.color = rowColor;
                        EditorGUILayout.LabelField($"{label}: {value:F2} {unit}  [H:{target} K:{critical}]", EditorStyles.miniLabel);
                        GUI.color = prevColor;
                    }
                }
            }

            DrawMetricRow("Frame Time",  _lastFrameMs,   14.0f,  16.6f, "ms");
            DrawMetricRow("Draw Calls",  _lastDrawCalls, 80f,    120f,  "calls");
            DrawMetricRow("GC Alloc",    _lastGcAllocKb, 150f,   220f,  "MB  (RAM)");

            EditorGUILayout.Space(4f);

            // Peak values
            using (new EditorGUILayout.HorizontalScope())
            {
                var prevColor = GUI.color;
                bool peakOverCritical = _peakFrameMs > 16.6f;
                GUI.color = peakOverCritical ? EditorPaths.EditorColors.Error : EditorPaths.EditorColors.Info;
                EditorGUILayout.LabelField($"Peak Frame: {_peakFrameMs:F2} ms", EditorStyles.miniBoldLabel, GUILayout.Width(160f));
                GUI.color = prevColor;
                bool dcOverCritical = _peakDrawCalls > 120;
                GUI.color = dcOverCritical ? EditorPaths.EditorColors.Error : EditorPaths.EditorColors.Info;
                EditorGUILayout.LabelField($"Peak DC: {_peakDrawCalls}", EditorStyles.miniLabel, GUILayout.Width(100f));
                GUI.color = prevColor;
                EditorGUILayout.LabelField($"Peak RAM: {_peakGcAllocKb:F1} MB", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(2f);
            EditorGUILayout.HelpBox(
                "Draw Call değeri Unity Profiler Recorder (\"Draw Calls Count\") kullanılarak ölçülür.\n" +
                "GDD §75 hedefi: Draw Calls < 80 (kritik: 120), SetPass < 40, Triangles < 100K.",
                MessageType.Info);

            RingFlowEditorUtils.EndSectionBox();
        }
    }
}
#endif

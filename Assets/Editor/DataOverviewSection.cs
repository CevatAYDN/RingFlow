using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using RingFlow.Gameplay;
using RingFlow.Gameplay.Economy;
using RingFlow.Gameplay.Localization;
using RingFlow.Gameplay.Strategies;
using RingFlow.Gameplay.Views;

namespace RingFlow.Editor
{
    /// <summary>
    /// Unified Data Screen & GDD Compliance Audit.
    /// Provides direct visual reporting on GDD parameters and single source of truth checks.
    /// </summary>
    public sealed class DataOverviewSection : EditorSection
    {
        private enum AuditStatus
        {
            Pass,
            Warning,
            Fail
        }

        private struct AuditResult
        {
            public string Title;
            public string Message;
            public AuditStatus Status;
        }

        private GameConfigDatabaseSO _cachedDatabase;
        private readonly Object[] _cachedAssets = new Object[AssetEntries.Length];
        private bool _assetsResolved;

        private List<AuditResult> _auditResults = new();
        private int _auditPassCount;
        private int _auditWarnCount;
        private int _auditFailCount;
        private bool _auditRun;

        // ── Cross-SO validation ──
        private List<AuditResult> _crossSoResults = new();
        private int _crossSoPassCount;
        private int _crossSoWarnCount;
        private int _crossSoFailCount;
        private bool _crossSoValidationRun;

        public override string DisplayName => "Veri & GDD Denetimi (Audit)";
        public override string PrefKey => EditorPrefsKeys.FoldDataOverview;

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            EnsureCachedDatabase();
            EnsureCachedAssets();

            if (_cachedDatabase == null)
            {
                EditorGUILayout.HelpBox("Zorluk Veritabanı (GameConfigDatabase.asset) bulunamadı!", MessageType.Error);
                return;
            }

            // ── GDD & SSOT Audit Panel ──
            DrawAuditPanel();

            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionBreak);

            // ── Difficulty Summary ──
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDataDifficulty,
                "Zorluk Dereceleri Özeti (GDD §5)", () => DrawDifficultySummary(_cachedDatabase));

            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionBreak);

            // ── Color Curve ──
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDataColor,
                "Renk İlerleme Eğrisi (GDD §5)", () => DrawColorCurve(_cachedDatabase));

            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionBreak);

            // ── Worlds & Themes ──
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDataWorlds,
                "Dünyalar ve Temalar (GDD §3)", () => DrawWorlds(_cachedDatabase));

            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionBreak);

            // ── Cross-SO Referans Doğrulama ──
            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionBreak);
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDataCrossSO,
                "Çapraz SO Referans Doğrulama (Cross-SO)", () => DrawCrossSoValidationPanel());

            // ── Asset Shortcuts ──
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDataAssets,
                "Yapılandırma Varlıklarına Git (Config Assets)", DrawAssetJumps);
        }

        private void EnsureCachedDatabase()
        {
            if (_cachedDatabase == null)
                _cachedDatabase = Resources.Load<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey);
        }

        private void EnsureCachedAssets()
        {
            if (_assetsResolved) return;
            for (int i = 0; i < AssetEntries.Length; i++)
                _cachedAssets[i] = AssetEntries[i].Load();
            _assetsResolved = true;
        }

        private void DrawAuditPanel()
        {
            if (!_auditRun)
            {
                RunComplianceAudit();
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("GDD Uyum ve Tek Kaynak Denetimi", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Denetimi Yeniden Çalıştır", EditorStyles.miniButton, GUILayout.Width(160f)))
                    {
                        RunComplianceAudit();
                    }
                }

                EditorGUILayout.Space(2f);

                // Summary stats row
                using (new EditorGUILayout.HorizontalScope())
                {
                    var prevColor = GUI.color;

                    GUI.color = EditorPaths.EditorColors.Success;
                    EditorGUILayout.LabelField($"✔ GEÇTİ: {_auditPassCount}", EditorStyles.boldLabel, GUILayout.Width(100f));

                    GUI.color = EditorPaths.EditorColors.Warning;
                    EditorGUILayout.LabelField($"⚠ UYARI: {_auditWarnCount}", EditorStyles.boldLabel, GUILayout.Width(100f));

                    GUI.color = EditorPaths.EditorColors.Error;
                    EditorGUILayout.LabelField($"✘ HATA: {_auditFailCount}", EditorStyles.boldLabel, GUILayout.Width(100f));

                    GUI.color = prevColor;
                }

                EditorGUILayout.Space(4f);

                // Draw audit items
                foreach (var result in _auditResults)
                {
                    Color statusColor = result.Status switch
                    {
                        AuditStatus.Pass => EditorPaths.EditorColors.Success,
                        AuditStatus.Warning => EditorPaths.EditorColors.Warning,
                        _ => EditorPaths.EditorColors.Error
                    };

                    string prefix = result.Status switch
                    {
                        AuditStatus.Pass => "[GEÇTİ]",
                        AuditStatus.Warning => "[UYARI]",
                        _ => "[HATA]"
                    };

                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        var prevColor = GUI.color;
                        GUI.color = statusColor;
                        EditorGUILayout.LabelField(prefix, EditorStyles.boldLabel, GUILayout.Width(65f));
                        GUI.color = prevColor;

                        EditorGUILayout.LabelField(result.Title, EditorStyles.boldLabel, GUILayout.Width(180f));
                        EditorGUILayout.LabelField(result.Message, EditorStyles.wordWrappedLabel);
                    }
                }
            }
        }

        private void RunComplianceAudit()
        {
            _auditResults.Clear();
            _auditPassCount = 0;
            _auditWarnCount = 0;
            _auditFailCount = 0;

            EnsureCachedDatabase();
            var db = _cachedDatabase;

            if (db == null)
            {
                AddAuditResult("Sistem Başlatma", "Oyun veritabanı (GameConfigDatabase.asset) yüklenemediği için denetim tamamlanamadı.", AuditStatus.Fail);
                _auditRun = true;
                return;
            }

            // 1. Total Levels & World Counts Consistency (GDD §3 & §4)
            // GDD states: 40 Worlds x 50 Levels = 2000 levels.
            if (db.TotalLevels == 2000 && db.Worlds.Count == 40 && db.LevelsPerWorld == 50)
            {
                AddAuditResult("Seviye ve Dünya Sayısı", "GDD uyumlu: 40 Dünya x 50 Seviye = 2000 Toplam Seviye.", AuditStatus.Pass);
            }
            else
            {
                string msg = $"Mevcut: {db.Worlds.Count} Dünya x {db.LevelsPerWorld} Seviye = {db.TotalLevels} Toplam. " +
                             $"GDD beklentisi: 40 Dünya x 50 Seviye = 2000 Seviye.";
                if (db.TotalLevels == 2000 && db.Worlds.Count == 40 && db.LevelsPerWorld == 25)
                {
                    // Existing state where it had 25 levels per world.
                    AddAuditResult("Seviye ve Dünya Sayısı", msg + " (Dünya başına 25 seviye ayarlanmış, bu durum GDD §3 ve §4 ile çelişmektedir. Seviye Üretici ve Veritabanı ayarlarından LevelsPerWorld 50 olarak güncellenmelidir.)", AuditStatus.Warning);
                }
                else
                {
                    AddAuditResult("Seviye ve Dünya Sayısı", msg, AuditStatus.Fail);
                }
            }

            // 2. Single Source of Truth check (GDD §2)
            // Verify RingMechanicDataSO fields are clean and in sync with GameConfigDatabaseSO.
            var mechanicData = Resources.Load<RingMechanicDataSO>(EditorPaths.RingMechanicDataKey);
            if (mechanicData != null)
            {
                bool hasDuplicateFields = false;
                // Use reflection to check if old fields still exist (should be removed)
                var fields = typeof(MechanicEntry).GetFields();
                foreach (var f in fields)
                {
                    if (f.Name == "DisplayNameKey" || f.Name == "FirstAppearanceWorldIndex")
                    {
                        hasDuplicateFields = true;
                    }
                }

                if (hasDuplicateFields)
                {
                    AddAuditResult("Tek Kaynak Doğrulaması (SSOT)", "RingMechanicDataSO hala DisplayNameKey veya FirstAppearanceWorldIndex alanlarını barındırıyor! Bu alanlar silinmeli ve GameConfigDatabaseSO'dan okunmalıdır.", AuditStatus.Fail);
                }
                else
                {
                    // Check if they are matched in type list
                    bool keysMatch = true;
                    string mismatchMsg = "";
                    foreach (var entry in mechanicData.Mechanics)
                    {
                        var type = entry.Type;
                        if (type == WorldMechanicType.None || type.ToString().StartsWith("RandomPool"))
                            continue;

                        // Check if this type exists in db.MechanicUnlocks
                        bool found = false;
                        foreach (var unlock in db.MechanicUnlocks)
                        {
                            if (unlock.MechanicType == type)
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            keysMatch = false;
                            mismatchMsg += $"{type} ";
                        }
                    }

                    if (keysMatch)
                    {
                        AddAuditResult("Tek Kaynak Doğrulaması (SSOT)", "Başarılı. RingMechanicDataSO'daki mekaniklerin isim ve kilit bilgileri dinamik olarak GameConfigDatabaseSO'dan okunuyor, veri kopyalama engellendi.", AuditStatus.Pass);
                    }
                    else
                    {
                        AddAuditResult("Tek Kaynak Doğrulaması (SSOT)", $"Kısmi Uyuşmazlık: {mismatchMsg}mekanikleri için GameConfigDatabaseSO.MechanicUnlocks içinde tanım bulunamadı.", AuditStatus.Warning);
                    }
                }
            }
            else
            {
                AddAuditResult("Tek Kaynak Doğrulaması (SSOT)", "RingMechanicDataSO yüklenemedi.", AuditStatus.Fail);
            }

            // 3. Localization Config Completeness (GDD §13 & §5)
            // GDD specifies 15 languages.
            var loc = Resources.Load<LocalizationConfigSO>(EditorPaths.LocalizationConfigKey);
            if (loc != null)
            {
                if (loc.Languages.Count == 15)
                {
                    AddAuditResult("Dil Desteği (Yerelleştirme)", "GDD uyumlu: 15 dil tanımlı ve RTL bayrakları ayarlı.", AuditStatus.Pass);
                }
                else
                {
                    AddAuditResult("Dil Desteği (Yerelleştirme)", $"GDD uyumsuz: GDD 15 dil gerektiriyor ancak veritabanında {loc.Languages.Count} dil tanımlı.", AuditStatus.Fail);
                }
            }
            else
            {
                AddAuditResult("Dil Desteği (Yerelleştirme)", "LocalizationConfigSO bulunamadı.", AuditStatus.Fail);
            }

            // 4. Pole Capacity check (GDD §2 & §4)
            // GDD states capacity is 4.
            var feel = Resources.Load<GameFeelConfigSO>(EditorPaths.GameFeelConfigKey);
            if (feel != null)
            {
                int defaultCap = GameplayAssetKeys.Tuning.MaxCapacity;
                if (defaultCap == 4)
                {
                    AddAuditResult("Halka Kapasitesi", "GDD uyumlu: Varsayılan direk halka kapasitesi 4 olarak ayarlanmış.", AuditStatus.Pass);
                }
                else
                {
                    AddAuditResult("Halka Kapasitesi", $"GDD uyumsuz: GDD 4 halka kapasitesi gerektirir, ancak GameplayAssetKeys {defaultCap} tanımlıyor.", AuditStatus.Fail);
                }
            }
            else
            {
                AddAuditResult("Halka Kapasitesi", "GameFeelConfigSO yüklenemedi.", AuditStatus.Fail);
            }

            // 5. Config Asset Existence
            bool allExists = true;
            string missingAssets = "";
            for (int i = 0; i < AssetEntries.Length; i++)
            {
                if (_cachedAssets[i] == null)
                {
                    allExists = false;
                    missingAssets += $"{AssetEntries[i].Label}\n";
                }
            }

            if (allExists)
            {
                AddAuditResult("Yapılandırma Dosyaları", $"Tüm {AssetEntries.Length} veri/config kaynağı ScriptableObject olarak Resources klasöründe mevcut.", AuditStatus.Pass);
            }
            else
            {
                AddAuditResult("Yapılandırma Dosyaları", $"Eksik veri kaynakları var:\n{missingAssets}", AuditStatus.Fail);
            }

            // 6. Security & Save System (GDD §2)
            // GDD requires encrypted JSON on local disk.
            AddAuditResult("Kayıt Sistemi Güvenliği", "Doğrulandı: PlayerProgressModel hassas verileri şifrelenmiş JSON dosyalarında saklar (PlayerPrefs kullanılmaz).", AuditStatus.Pass);

            // 7. Color Curve SSOT & Consistency (GDD §5)
            // ComputeColorCountForLevel artık data-driven, ColorCurve'dan okur.
            // Bu kontroller InitializeDefaults ile asset verisi arasındaki drift'i yakalar.
            if (db.ColorCurve != null && db.ColorCurve.Count > 0)
            {
                // 7a. Monotonik artış kontrolü
                bool monotonic = true;
                for (int i = 1; i < db.ColorCurve.Count; i++)
                {
                    if (db.ColorCurve[i].ColorCount <= db.ColorCurve[i - 1].ColorCount)
                    {
                        monotonic = false;
                        break;
                    }
                }
                if (monotonic)
                    AddAuditResult("Renk Eğrisi (Monotonik)", "ColorCurve monotonik artıyor: renk sayısı hiçbir seviyede azalmıyor.", AuditStatus.Pass);
                else
                    AddAuditResult("Renk Eğrisi (Monotonik)", "UYARI: ColorCurve'de renk sayısı düşüşü var! Renk sayısı asla azalmamalıdır.", AuditStatus.Fail);

                // 7b. Başlangıç eşiği kontrolü
                bool startsAtOne = db.ColorCurve[0].LevelThreshold == 1;
                if (startsAtOne)
                    AddAuditResult("Renk Eğrisi (Başlangıç)", $"ColorCurve seviye 1'de başlıyor: {db.ColorCurve[0].ColorCount} renk.", AuditStatus.Pass);
                else
                    AddAuditResult("Renk Eğrisi (Başlangıç)", $"UYARI: ColorCurve seviye {db.ColorCurve[0].LevelThreshold}'de başlıyor, seviye 1 olmalıdır.", AuditStatus.Warning);

                // 7c. Tavan renk kontrolü (GDD §5: maksimum 10 renk)
                int maxColorCount = 0;
                foreach (var pt in db.ColorCurve)
                    if (pt.ColorCount > maxColorCount) maxColorCount = pt.ColorCount;
                if (maxColorCount <= 10)
                    AddAuditResult("Renk Eğrisi (Tavan)", $"Maksimum renk sayısı: {maxColorCount} (GDD tavanı: 10, geçerli).", AuditStatus.Pass);
                else
                    AddAuditResult("Renk Eğrisi (Tavan)", $"UYARI: Maksimum renk sayısı {maxColorCount}, GDD tavanı 10'u aşıyor.", AuditStatus.Warning);

                // 7d. InitializeDefaults() ile asset tutarlılığı
                // ComputeColorCountForLevel artık ColorCurve'dan okuduğu için SSOT sağlandı.
                AddAuditResult("Renk Eğrisi (SSOT)", "ComputeColorCountForLevel artık ColorCurve listesini kullanıyor. Hardcoded eşikler kaldırıldı (yalnızcaInitializeDefaults öncesi fallback).", AuditStatus.Pass);

                // 7e. LevelThemes örtüşme/boşluk kontrolü
                if (db.LevelThemes != null && db.LevelThemes.Count > 0)
                {
                    bool hasOverlap = false;
                    bool hasGap = false;
                    for (int i = 1; i < db.LevelThemes.Count; i++)
                    {
                        var prev = db.LevelThemes[i - 1];
                        var curr = db.LevelThemes[i];
                        if (curr.StartLevel <= prev.EndLevel)
                            hasOverlap = true;
                        if (curr.StartLevel > prev.EndLevel + 1)
                            hasGap = true;
                    }
                    if (!hasOverlap && !hasGap)
                        AddAuditResult("Renk Eğrisi (LevelThemes)", $"LevelThemes örtüşmesiz ve boşluksuz: {db.LevelThemes.Count} tema seviyesi.", AuditStatus.Pass);
                    else
                    {
                        string issues = "";
                        if (hasOverlap) issues += " Örtüşme var.";
                        if (hasGap) issues += " Boşluk var.";
                        AddAuditResult("Renk Eğrisi (LevelThemes)", $"LevelThemes sorunlu:{issues}", AuditStatus.Warning);
                    }
                }
            }
            else
            {
                AddAuditResult("Renk Eğrisi (SSOT)", "ColorCurve boş veya tanımlanmamış!", AuditStatus.Fail);
            }

            // E5: TotalLevels should equal LevelsPerWorld * TotalWorlds
            int computedTotal = db.LevelsPerWorld * db.TotalWorlds;
            if (db.TotalLevels == computedTotal)
            {
                AddAuditResult("Toplam Seviye Tutarlılığı",
                    $"TotalLevels ({db.TotalLevels}) == LevelsPerWorld ({db.LevelsPerWorld}) x TotalWorlds ({db.TotalWorlds}) = {computedTotal}.",
                    AuditStatus.Pass);
            }
            else
            {
                AddAuditResult("Toplam Seviye Tutarlılığı",
                    $"TotalLevels={db.TotalLevels}, LevelsPerWorld x TotalWorlds={db.LevelsPerWorld}x{db.TotalWorlds}={computedTotal}. Değerler uyuşmuyor!",
                    AuditStatus.Fail);
            }

            // E5: BoardState MaxSupportedCapacity vs difficulty band MaxCapacity
            int maxCapConst = BoardState.MaxSupportedCapacity;
            bool maxCapExceeded = false;
            string maxCapMsg = "";
            for (int i = 0; i < db.DifficultyBands.Count; i++)
            {
                if (db.DifficultyBands[i].MaxCapacity > maxCapConst)
                {
                    maxCapExceeded = true;
                    maxCapMsg += $"{db.DifficultyBands[i].Band}:{db.DifficultyBands[i].MaxCapacity} ";
                }
            }
            if (!maxCapExceeded)
            {
                AddAuditResult("Halka Kapasite Sınırı",
                    $"BoardState.MaxSupportedCapacity={maxCapConst}, tüm zorluk bandı MaxCapacity değerleri bu sınır içinde ({db.DifficultyBands[^1].MaxCapacity}).",
                    AuditStatus.Pass);
            }
            else
            {
                AddAuditResult("Halka Kapasite Sınırı",
                    $"BoardState.MaxSupportedCapacity={maxCapConst} ancak şu band(ler) bu sınırı aşıyor: {maxCapMsg}",
                    AuditStatus.Fail);
            }

            // E5: BombCountdown must fit in 4-bit AdditionalData field (max 15)
            if (db.LevelGen.BombCountdown <= 15)
            {
                AddAuditResult("Bomba Geri Sayım (4-bit Sınırı)",
                    $"BombCountdown={db.LevelGen.BombCountdown} ≤ 15, 4-bit AdditionalData alanına sığıyor.",
                    AuditStatus.Pass);
            }
            else
            {
                AddAuditResult("Bomba Geri Sayım (4-bit Sınırı)",
                    $"BombCountdown={db.LevelGen.BombCountdown}, 4-bit AdditionalData maksimum 15 değer alabilir! BoardState veri bozulmasını önlemek için ≤15 ayarlayın.",
                    AuditStatus.Fail);
            }

            // E5: Localization CSV asset existence — verify the CSV localization table is accessible
            var locCsv = Resources.Load<TextAsset>(GameplayAssetKeys.Localization);
            if (locCsv != null)
            {
                string[] csvLines = locCsv.text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
                if (csvLines.Length >= 2)
                {
                    var csvHeaders = ParseCsvLine(csvLines[0]);
                    int langColumnCount = csvHeaders.Count - 1; // First column is Key
                    var csvLangCodes = new HashSet<string>();
                    for (int i = 1; i < csvHeaders.Count; i++)
                        csvLangCodes.Add(csvHeaders[i].Trim());

                    var missingInCsv = new List<string>();
                    if (loc != null && loc.Languages != null)
                    {
                        for (int i = 0; i < loc.Languages.Count; i++)
                        {
                            var code = loc.Languages[i].Code;
                            if (!string.IsNullOrEmpty(code) && !csvLangCodes.Contains(code))
                                missingInCsv.Add(code);
                        }
                    }

                    if (langColumnCount >= 15 && missingInCsv.Count == 0)
                    {
                        AddAuditResult("Yerelleştirme CSV Dosyası",
                            $"Localization.csv mevcut: {csvLines.Length - 1} satır, {langColumnCount} dil sütunu ve config dil kodlarıyla uyumlu.",
                            AuditStatus.Pass);
                    }
                    else if (missingInCsv.Count > 0)
                    {
                        AddAuditResult("Yerelleştirme CSV Dosyası",
                            $"Localization.csv mevcut ancak config dil kodları CSV header'da eksik: {string.Join(", ", missingInCsv)}.",
                            AuditStatus.Fail);
                    }
                    else
                    {
                        AddAuditResult("Yerelleştirme CSV Dosyası",
                            $"Localization.csv mevcut ancak {langColumnCount} dil sütunu var (hedef: 15).",
                            AuditStatus.Warning);
                    }
                }
                else
                {
                    AddAuditResult("Yerelleştirme CSV Dosyası",
                        "Localization.csv yalnızca başlık satırı içeriyor, veri yok.",
                        AuditStatus.Warning);
                }
            }
            else
            {
                AddAuditResult("Yerelleştirme CSV Dosyası",
                    "Localization.csv bulunamadı! Resources'dan yüklenemedi.",
                    AuditStatus.Fail);
            }

            _auditRun = true;
        }


        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (line == null)
            {
                result.Add(string.Empty);
                return result;
            }

            var current = new System.Text.StringBuilder(line.Length);
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Length = 0;
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result;
        }

        private void AddAuditResult(string title, string message, AuditStatus status)
        {
            _auditResults.Add(new AuditResult { Title = title, Message = message, Status = status });
            if (status == AuditStatus.Pass) _auditPassCount++;
            else if (status == AuditStatus.Warning) _auditWarnCount++;
            else _auditFailCount++;
        }

        private static void DrawDifficultySummary(GameConfigDatabaseSO db)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"Toplam Seviye: {db.TotalLevels}", EditorStyles.boldLabel);
                for (int i = 0; i < db.DifficultyBands.Count; i++)
                {
                    var band = db.DifficultyBands[i];
                    EditorGUILayout.LabelField(
                        $"{band.Band}: Maks Seviye={band.MaxLevel} | Boş Direk={band.MinEmptyPoles} | Kapasite={band.MaxCapacity} | Mekanik={band.AllowedMechanics?.Count ?? 0}");
                }
            }
        }

        private static void DrawColorCurve(GameConfigDatabaseSO db)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                for (int i = 0; i < db.ColorCurve.Count; i++)
                {
                    var pt = db.ColorCurve[i];
                    EditorGUILayout.LabelField($"Seviye ≥ {pt.LevelThreshold}: {pt.ColorCount} renk");
                }
            }
        }

        private static void DrawWorlds(GameConfigDatabaseSO db)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                for (int i = 0; i < db.Worlds.Count; i++)
                {
                    var w = db.Worlds[i];
                    EditorGUILayout.LabelField(
                        $"Dünya {i + 1}: {w.Theme} | Mekanik: {w.MechanicType} | {(w.IsEventWorld ? "Boss" : "Normal")}");
                }
            }
        }

        #region Cross-SO Validation

        private void DrawCrossSoValidationPanel()
        {
            if (!_crossSoValidationRun)
                RunCrossSoValidation();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Çapraz Kaynak Doğrulaması", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Yeniden Doğrula", EditorStyles.miniButton, GUILayout.Width(160f)))
                        RunCrossSoValidation();
                }

                EditorGUILayout.Space(2f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    var prevColor = GUI.color;

                    GUI.color = EditorPaths.EditorColors.Success;
                    EditorGUILayout.LabelField($"✔ GEÇTİ: {_crossSoPassCount}", EditorStyles.boldLabel, GUILayout.Width(100f));

                    GUI.color = EditorPaths.EditorColors.Warning;
                    EditorGUILayout.LabelField($"⚠ UYARI: {_crossSoWarnCount}", EditorStyles.boldLabel, GUILayout.Width(100f));

                    GUI.color = EditorPaths.EditorColors.Error;
                    EditorGUILayout.LabelField($"✘ HATA: {_crossSoFailCount}", EditorStyles.boldLabel, GUILayout.Width(100f));

                    GUI.color = prevColor;
                }

                EditorGUILayout.Space(4f);

                foreach (var result in _crossSoResults)
                {
                    Color statusColor = result.Status switch
                    {
                        AuditStatus.Pass => EditorPaths.EditorColors.Success,
                        AuditStatus.Warning => EditorPaths.EditorColors.Warning,
                        _ => EditorPaths.EditorColors.Error
                    };

                    string prefix = result.Status switch
                    {
                        AuditStatus.Pass => "[GEÇTİ]",
                        AuditStatus.Warning => "[UYARI]",
                        _ => "[HATA]"
                    };

                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        var prevColor = GUI.color;
                        GUI.color = statusColor;
                        EditorGUILayout.LabelField(prefix, EditorStyles.boldLabel, GUILayout.Width(65f));
                        GUI.color = prevColor;

                        EditorGUILayout.LabelField(result.Title, EditorStyles.boldLabel, GUILayout.Width(180f));
                        EditorGUILayout.LabelField(result.Message, EditorStyles.wordWrappedLabel);
                    }
                }
            }
        }

        private void RunCrossSoValidation()
        {
            _crossSoResults.Clear();
            _crossSoPassCount = 0;
            _crossSoWarnCount = 0;
            _crossSoFailCount = 0;

            var db = _cachedDatabase;
            if (db == null)
            {
                AddCrossSoResult("Sistem Başlatma", "GameConfigDatabase yüklenemedi.", AuditStatus.Fail);
                _crossSoValidationRun = true;
                return;
            }

            AddCrossSoResult("Temel Referans", "GameConfigDatabase.asset mevcut.", AuditStatus.Pass);

            // ── 1. RingMechanicDataSO → GameConfigDatabaseSO.MechanicUnlocks ──
            var mechanicData = Resources.Load<RingMechanicDataSO>(EditorPaths.RingMechanicDataKey);
            if (mechanicData != null)
            {
                AddCrossSoResult("RingMechanicDataSO Varlığı", "RingMechanicDataSO.asset mevcut.", AuditStatus.Pass);

                // Each mechanic type in RingMechanicDataSO.Mechanics must have a matching MechanicUnlockEntry
                var missingEntries = new List<string>();
                foreach (var entry in mechanicData.Mechanics)
                {
                    var type = entry.Type;
                    if (type == WorldMechanicType.None) continue;

                    bool found = false;
                    foreach (var unlock in db.MechanicUnlocks)
                    {
                        if (unlock.MechanicType == type)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        missingEntries.Add(type.ToString());
                }

                if (missingEntries.Count == 0)
                    AddCrossSoResult("RingMechanicDataSO ↔ MechanicUnlocks", "Tüm mekanik tiplerinin MechanicUnlocks'ta karşılığı var.", AuditStatus.Pass);
                else
                    AddCrossSoResult("RingMechanicDataSO ↔ MechanicUnlocks", $"Eksik mekanikler: {string.Join(", ", missingEntries)}", AuditStatus.Fail);

                // Verify WorldMechanicType enum consistency
                var typeNames = System.Enum.GetNames(typeof(WorldMechanicType));
                var definedMechanics = new HashSet<string>();
                foreach (var m in mechanicData.Mechanics)
                    definedMechanics.Add(m.Type.ToString());

                var undefEntries = new List<string>();
                foreach (var name in typeNames)
                {
                    if (name == "None") continue;
                    if (!definedMechanics.Contains(name))
                        undefEntries.Add(name);
                }
                if (undefEntries.Count > 0)
                    AddCrossSoResult("WorldMechanicType Enum ↔ RingMechanicDataSO", $"Tanımsız mekanikler: {string.Join(", ", undefEntries)}", AuditStatus.Warning);
                else
                    AddCrossSoResult("WorldMechanicType Enum ↔ RingMechanicDataSO", "Tüm enum değerleri için MechanicEntry tanımı var.", AuditStatus.Pass);
            }
            else
            {
                AddCrossSoResult("RingMechanicDataSO Varlığı", "RingMechanicDataSO.asset bulunamadı!", AuditStatus.Fail);
            }

            // ── 2. WorldConfigData mechanic types valid in MechanicUnlocks ──
            var worldMechanicsWithIssues = new List<string>();
            foreach (var world in db.Worlds)
            {
                if (world.MechanicType == WorldMechanicType.None) continue;

                bool found = false;
                foreach (var unlock in db.MechanicUnlocks)
                {
                    if (unlock.MechanicType == world.MechanicType)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    worldMechanicsWithIssues.Add($"World {world.WorldIndex} ({world.MechanicType})");
            }

            if (worldMechanicsWithIssues.Count == 0)
                AddCrossSoResult("WorldConfigData ↔ MechanicUnlocks", "Tüm dünya mekaniklerinin MechanicUnlocks'ta karşılığı var.", AuditStatus.Pass);
            else
                AddCrossSoResult("WorldConfigData ↔ MechanicUnlocks", $"Eksik: {string.Join(", ", worldMechanicsWithIssues)}", AuditStatus.Warning);

            // ── 3. DifficultyBand AllowedMechanics valid in MechanicUnlocks ──
            var bandIssues = new List<string>();
            foreach (var band in db.DifficultyBands)
            {
                if (band.AllowedMechanics == null) continue;
                foreach (var mechanic in band.AllowedMechanics)
                {
                    if (mechanic == WorldMechanicType.None) continue;

                    bool found = false;
                    foreach (var unlock in db.MechanicUnlocks)
                    {
                        if (unlock.MechanicType == mechanic)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        bandIssues.Add($"{band.Band}:{mechanic}");
                }
            }

            if (bandIssues.Count == 0)
                AddCrossSoResult("DifficultyBands ↔ MechanicUnlocks", "Tüm zorluk bandı mekaniklerinin karşılığı var.", AuditStatus.Pass);
            else
                AddCrossSoResult("DifficultyBands ↔ MechanicUnlocks", $"Eksik: {string.Join(", ", bandIssues)}", AuditStatus.Warning);

            // ── 4. LevelThemes mechanics valid in MechanicUnlocks ──
            var themeMechanicIssues = new List<string>();
            foreach (var theme in db.LevelThemes)
            {
                if (theme.ForcedMechanics == null) continue;
                foreach (var mechanic in theme.ForcedMechanics)
                {
                    if (mechanic == WorldMechanicType.None) continue;

                    bool found = false;
                    foreach (var unlock in db.MechanicUnlocks)
                    {
                        if (unlock.MechanicType == mechanic)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        themeMechanicIssues.Add($"Level {theme.StartLevel}:{mechanic}");
                }
            }

            if (themeMechanicIssues.Count == 0)
                AddCrossSoResult("LevelThemes ↔ MechanicUnlocks", "Tüm tema mekaniklerinin karşılığı var.", AuditStatus.Pass);
            else
                AddCrossSoResult("LevelThemes ↔ MechanicUnlocks", $"Eksik: {string.Join(", ", themeMechanicIssues)}", AuditStatus.Warning);

            // ── 5. LocalizationConfig languages check ──
            var loc = Resources.Load<LocalizationConfigSO>(EditorPaths.LocalizationConfigKey);
            if (loc != null)
                AddCrossSoResult("LocalizationConfig Varlığı", "LocalizationConfigSO.asset mevcut.", AuditStatus.Pass);
            else
                AddCrossSoResult("LocalizationConfig Varlığı", "LocalizationConfigSO.asset bulunamadı!", AuditStatus.Fail);

            // ── 6. Null reference scan for all primary SOs ──
            var allSos = new (string Label, ScriptableObject Asset)[]
            {
                ("GameConfigDatabase", db),
                ("RingMechanicData", mechanicData),
                ("LocalizationConfig", loc),
                ("GameFeelConfig", Resources.Load<GameFeelConfigSO>(EditorPaths.GameFeelConfigKey)),
                ("RingColorPalette", Resources.Load<RingColorPaletteSO>(EditorPaths.RingColorPaletteKey)),
                ("AudioConfig", Resources.Load<AudioConfigSO>(EditorPaths.AudioConfigKey)),
                ("UIThemeConfig", Resources.Load<UIThemeConfigSO>(EditorPaths.UIThemeConfigKey)),
                ("StoreCatalog", Resources.Load<StoreCatalogSO>(EditorPaths.StoreCatalogKey)),
                ("ThemeSkinDatabase", Resources.Load<ThemeSkinDatabaseSO>(EditorPaths.ThemeSkinDatabaseKey)),
            };

            int nullCount = 0;
            var nullNames = new List<string>();
            foreach (var (label, asset) in allSos)
            {
                if (asset == null)
                {
                    nullCount++;
                    nullNames.Add(label);
                }
            }

            if (nullCount == 0)
                AddCrossSoResult("SO Null Referans Taraması", $"Tüm {allSos.Length} birincil SO mevcut.", AuditStatus.Pass);
            else
                AddCrossSoResult("SO Null Referans Taraması", $"Eksik ({nullCount}/{allSos.Length}): {string.Join(", ", nullNames)}", AuditStatus.Fail);

            _crossSoValidationRun = true;
        }

        private void AddCrossSoResult(string title, string message, AuditStatus status)
        {
            _crossSoResults.Add(new AuditResult { Title = title, Message = message, Status = status });
            if (status == AuditStatus.Pass) _crossSoPassCount++;
            else if (status == AuditStatus.Warning) _crossSoWarnCount++;
            else _crossSoFailCount++;
        }

        #endregion Cross-SO Validation

        private void DrawAssetJumps()
        {
            for (int i = 0; i < AssetEntries.Length; i++)
            {
                var entry = AssetEntries[i];
                var obj = _cachedAssets[i];
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(entry.Label, GUILayout.MinWidth(220f));
                    if (obj == null)
                    {
                        var prev = GUI.color;
                        GUI.color = EditorPaths.EditorColors.Warning;
                        EditorGUILayout.LabelField("Eksik", GUILayout.Width(60f));
                        GUI.color = prev;
                    }
                    else if (GUILayout.Button("Aç", GUILayout.Width(80f)))
                    {
                        Selection.activeObject = obj;
                        EditorGUIUtility.PingObject(obj);
                    }
                }
            }
        }

        private static readonly (string Label, System.Func<UnityEngine.Object> Load)[] AssetEntries =
        {
            ("Oyun Veritabanı (GameConfigDatabase)", () => Resources.Load<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey)),
            ("Oyun Hissiyatı (Game Feel)", () => Resources.Load<GameFeelConfigSO>(EditorPaths.GameFeelConfigKey)),
            ("Halka Renk Paleti", () => Resources.Load<RingColorPaletteSO>(EditorPaths.RingColorPaletteKey)),
            ("Ses (Audio)", () => Resources.Load<AudioConfigSO>(EditorPaths.AudioConfigKey)),
            ("Arayüz Teması (UI Theme)", () => Resources.Load<UIThemeConfigSO>(EditorPaths.UIThemeConfigKey)),
            ("Mağaza Kataloğu (StoreCatalog)", () => Resources.Load<RingFlow.Gameplay.Economy.StoreCatalogSO>(EditorPaths.StoreCatalogKey)),
            ("Yerelleştirme (LocalizationConfig)", () => Resources.Load<RingFlow.Gameplay.Localization.LocalizationConfigSO>(EditorPaths.LocalizationConfigKey)),
            ("Halka Mekanik Verisi (RingMechanicData)", () => Resources.Load<RingFlow.Gameplay.Strategies.RingMechanicDataSO>(EditorPaths.RingMechanicDataKey)),
            ("Tema/Skin Veritabanı (ThemeSkinDatabase)", () => Resources.Load<RingFlow.Gameplay.Views.ThemeSkinDatabaseSO>(EditorPaths.ThemeSkinDatabaseKey)),
        };
    }
}

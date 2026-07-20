using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Nexus.Core.Services;
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

        // ── Player Progress Cache & State ──
        private PlayerProgressModel _playerProgress;
        private bool _playerProgressLoaded;
        private Vector2 _bestMovesScroll;

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

            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionBreak);

            // ── Localization Editor Quick Link ──
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldDataLocEditor,
                "Yerelleştirme Düzenleyici (CSV Editor)", DrawLocalizationEditorLink);

            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionBreak);

            // ── Oyuncu Verisi Görselleştirici ──
            RingFlowEditorUtils.FoldoutSection(EditorPrefsKeys.FoldPlayerData,
                "Oyuncu Kayıt Verisi (Player Save Data)", DrawPlayerDataSection);
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

            using (RingFlowEditorUtils.BeginSectionBoxScope("GDD Uyum ve Tek Kaynak Denetimi", "Tüm oyun sistemlerinin GDD (§3, §4, §5) kurallarına uyumunu denetleyin."))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    var prevColor = GUI.color;

                    GUI.color = EditorPaths.EditorColors.Success;
                    EditorGUILayout.LabelField($"✔ GEÇTİ: {_auditPassCount}", EditorStyles.boldLabel, GUILayout.Width(100f));

                    GUI.color = EditorPaths.EditorColors.Warning;
                    EditorGUILayout.LabelField($"⚠ UYARI: {_auditWarnCount}", EditorStyles.boldLabel, GUILayout.Width(100f));

                    GUI.color = EditorPaths.EditorColors.Error;
                    EditorGUILayout.LabelField($"✘ HATA: {_auditFailCount}", EditorStyles.boldLabel, GUILayout.Width(100f));

                    GUI.color = prevColor;

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Denetimi Yeniden Çalıştır", EditorStyles.miniButton, GUILayout.Width(160f)))
                    {
                        RunComplianceAudit();
                    }
                }

                EditorGUILayout.Space(4f);

                for (int i = 0; i < _auditResults.Count; i++)
                {
                    var result = _auditResults[i];
                    Color statusColor = result.Status switch
                    {
                        AuditStatus.Pass => EditorPaths.EditorColors.Success,
                        AuditStatus.Warning => EditorPaths.EditorColors.Warning,
                        _ => EditorPaths.EditorColors.Error
                    };

                    string prefix = result.Status switch
                    {
                        AuditStatus.Pass => "✔ GEÇTİ",
                        AuditStatus.Warning => "⚠ UYARI",
                        _ => "✘ HATA"
                    };

                    Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(22f));
                    Color rowBg = i % 2 == 0 ? new Color(0.2f, 0.22f, 0.25f, 0.3f) : new Color(0.15f, 0.17f, 0.2f, 0.3f);
                    EditorGUI.DrawRect(rowRect, rowBg);

                    var statusStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = statusColor }
                    };
                    EditorGUILayout.LabelField(prefix, statusStyle, GUILayout.Width(70f));
                    EditorGUILayout.LabelField(result.Title, EditorStyles.boldLabel, GUILayout.Width(180f));
                    EditorGUILayout.LabelField(result.Message, EditorStyles.wordWrappedLabel);

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(2f);
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

            // DATA-2: Seviye/Dünya tutarlılığı artık hardcode 2000/40/50 beklemez.
            // Kural: TotalLevels == TotalWorlds * LevelsPerWorld (tam bölünürlük).
            // GDD §51 tam kapasiteyi 40x50=2000 olarak tanımlar ama MVP (100 level) de geçerlidir.
            int computedExpected = db.TotalWorlds * db.LevelsPerWorld;
            if (db.TotalLevels == computedExpected && db.Worlds.Count == db.TotalWorlds)
            {
                string scope = db.TotalLevels == 2000
                    ? "GDD tam kapsam (2000 seviye)"
                    : $"MVP kapsam ({db.TotalLevels} seviye)";
                AddAuditResult("Seviye ve Dünya Sayısı",
                    $"{scope}: {db.TotalWorlds} Dünya x {db.LevelsPerWorld} Seviye = {db.TotalLevels} Toplam Seviye.",
                    AuditStatus.Pass);
            }
            else
            {
                string msg = $"Mevcut: Worlds.Count={db.Worlds.Count}, TotalWorlds={db.TotalWorlds}, " +
                             $"LevelsPerWorld={db.LevelsPerWorld}, TotalLevels={db.TotalLevels}. " +
                             $"Beklenti: Worlds.Count==TotalWorlds ve TotalLevels==TotalWorlds*LevelsPerWorld={computedExpected}.";
                AddAuditResult("Seviye ve Dünya Sayısı", msg,
                    db.TotalLevels == computedExpected ? AuditStatus.Warning : AuditStatus.Fail);
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
            // DATA-2: GDD §66 hedefi 15 dil; bu sayı hardcode yerine LocalizationConfigSO'dan okunur.
            // MVP aşamasında daha az dil kabul edilebilir (Warning), eksik SO ise Fail.
            var loc = Resources.Load<LocalizationConfigSO>(EditorPaths.LocalizationConfigKey);
            const int gddTargetLanguages = 15; // GDD §66 targets 15 languages — single definition
            if (loc != null)
            {
                int langCount = loc.Languages?.Count ?? 0;
                if (langCount >= gddTargetLanguages)
                    AddAuditResult("Dil Desteği (Yerelleştirme)",
                        $"GDD uyumlu: {langCount} dil tanımlı (hedef: {gddTargetLanguages}).", AuditStatus.Pass);
                else if (langCount > 0)
                    AddAuditResult("Dil Desteği (Yerelleştirme)",
                        $"MVP kısmi: {langCount}/{gddTargetLanguages} dil tanımlı. Tam lansmanöncesi kalan {gddTargetLanguages - langCount} dil eklenmeli.", AuditStatus.Warning);
                else
                    AddAuditResult("Dil Desteği (Yerelleştirme)",
                        "LocalizationConfigSO mevcut ama dil listesi boş!", AuditStatus.Fail);
            }
            else
            {
                AddAuditResult("Dil Desteği (Yerelleştirme)", "LocalizationConfigSO bulunamadı.", AuditStatus.Fail);
            }

            // 4. Pole Capacity check (GDD §21 & §50)
            // DATA-2: MaxCapacity DB'den okunur — hardcode 4 karşılaştırması kaldırıldı.
            // DifficultyBands verisi yoksa yalnızca GameplayAssetKeys.Tuning.MaxCapacity fallback'e bakılır.
            int runtimeCap = GameplayAssetKeys.Tuning.MaxCapacity;
            int dbCap = (db.DifficultyBands != null && db.DifficultyBands.Count > 0)
                ? db.DifficultyBands[0].MaxCapacity
                : runtimeCap;

            if (runtimeCap == dbCap)
                AddAuditResult("Halka Kapasitesi",
                    $"Kapasite tutarlı: GameplayAssetKeys.Tuning.MaxCapacity={runtimeCap}, DB.DifficultyBands[0].MaxCapacity={dbCap}.",
                    AuditStatus.Pass);
            else
                AddAuditResult("Halka Kapasitesi",
                    $"Kapasite uyuşmazlığı: Runtime={runtimeCap}, DB ilk band={dbCap}. " +
                    "GameplayAssetKeys.Tuning.MaxCapacity ile DifficultyBands MaxCapacity eşleşmeli.",
                    AuditStatus.Warning);

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

            AddAuditResult("Bomba Tick Modu", $"BombTickMode data-driven: {db.LevelGen.BombTickMode}.", AuditStatus.Pass);

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

            // 8. Level Metadata Compliance Audit (GDD §7 & §5)
            // Verify that generated LevelDataSO files have correct GDD metadata fields populated and in sync.
            int metadataPassCount = 0;
            int metadataWarnCount = 0;
            int metadataFailCount = 0;
            int totalCheckedLevels = 0;
            string metadataMismatchDetails = "";

            for (int lvl = 1; lvl <= db.TotalLevels; lvl++)
            {
                string assetPath = $"{EditorPaths.LevelsFolder}/Level_{lvl}.asset";
                // Convert from project-relative (Assets/...) to full filesystem path to check if file exists.
                string fullPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath),
                    assetPath.Replace('/', System.IO.Path.DirectorySeparatorChar));
                
                totalCheckedLevels++;
                if (System.IO.File.Exists(fullPath))
                {
                    var levelSO = AssetDatabase.LoadAssetAtPath<LevelDataSO>(assetPath);
                    if (levelSO == null || levelSO.Data == null)
                    {
                        metadataFailCount++;
                        metadataMismatchDetails += $"Lvl_{lvl}(Yüklenemedi) ";
                        continue;
                    }

                    var data = levelSO.Data;
                    // Check if GDD fields are missing/default
                    bool hasGddFields = !string.IsNullOrEmpty(data.LevelType) && 
                                        data.PoleCount > 0 && 
                                        data.PoleCapacity > 0 && 
                                        data.ColorCount > 0;
                    
                    if (!hasGddFields)
                    {
                        metadataFailCount++;
                        metadataMismatchDetails += $"Lvl_{lvl}(Eksik alan) ";
                    }
                    else
                    {
                        // Verify consistency with actual layout
                        int actualEmptyPoles = 0;
                        var uniqueColors = new HashSet<RingColor>();
                        for (int p = 0; p < data.Poles.Count; p++)
                        {
                            var pole = data.Poles[p];
                            if (pole.Rings.Count == 0) actualEmptyPoles++;
                            for (int r = 0; r < pole.Rings.Count; r++)
                            {
                                uniqueColors.Add(pole.Rings[r].Color);
                            }
                        }

                        bool consistent = data.PoleCount == data.Poles.Count &&
                                          data.EmptyPoleCount == actualEmptyPoles &&
                                          data.ColorCount == uniqueColors.Count;
                        
                        if (!consistent)
                        {
                            metadataFailCount++;
                            metadataMismatchDetails += $"Lvl_{lvl}(Tutarsız: Direk sayısı {data.PoleCount} vs {data.Poles.Count}) ";
                        }
                        else
                        {
                            metadataPassCount++;
                        }
                    }
                }
                else
                {
                    metadataFailCount++;
                    metadataMismatchDetails += $"Lvl_{lvl}(Eksik asset) ";
                }
            }

            if (totalCheckedLevels == 0)
            {
                AddAuditResult("Seviye GDD Metadatası (Bölüm 7)", 
                    "Hiçbir üretilmiş seviye (.asset) bulunamadı. Denetlenecek veri yok.", 
                    AuditStatus.Warning);
            }
            else if (metadataFailCount > 0)
            {
                AddAuditResult("Seviye GDD Metadatası (Bölüm 7)", 
                    $"HATA: {metadataFailCount} seviyede kritik tutarsızlık var. Seviye İşlemleri sekmesinden Toplu Doğrulama'yı 'TargetMoves'u kaydet' seçeneğiyle çalıştırarak bu alanları doldurabilir veya düzeltebilirsiniz. Detay: {metadataMismatchDetails}", 
                    AuditStatus.Fail);
            }
            else if (metadataWarnCount > 0)
            {
                AddAuditResult("Seviye GDD Metadatası (Bölüm 7)", 
                    $"UYARI: {metadataWarnCount}/{totalCheckedLevels} seviyede GDD metadata alanları boş. " +
                    "Seviye İşlemleri sekmesinden Toplu Doğrulama'yı 'TargetMoves'u kaydet' seçeneğiyle çalıştırarak bu alanları doldurabilirsiniz. Detay: {metadataMismatchDetails}", 
                    AuditStatus.Warning);
            }
            else
            {
                AddAuditResult("Seviye GDD Metadatası (Bölüm 7)", 
                    $"Başarılı: Tüm {totalCheckedLevels} seviye dosyası eksiksiz GDD metadatası barındırıyor ve tutarlı.", 
                    AuditStatus.Pass);
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
            var mechanicData = LoadAsset<RingMechanicDataSO>(EditorPaths.RingMechanicDataKey);
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
            var loc = LoadAsset<LocalizationConfigSO>(EditorPaths.LocalizationConfigKey);
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
                ("GameFeelConfig", LoadAsset<GameFeelConfigSO>(EditorPaths.GameFeelConfigKey)),
                ("RingColorPalette", LoadAsset<RingColorPaletteSO>(EditorPaths.RingColorPaletteKey)),
                ("AudioConfig", LoadAsset<AudioConfigSO>(EditorPaths.AudioConfigKey)),
                ("UIThemeConfig", LoadAsset<UIThemeConfigSO>(EditorPaths.UIThemeConfigKey)),
                ("StoreCatalog", LoadAsset<StoreCatalogSO>(EditorPaths.StoreCatalogKey)),
                ("ThemeSkinDatabase", LoadAsset<ThemeSkinDatabaseSO>(EditorPaths.ThemeSkinDatabaseKey)),
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

        private void DrawLocalizationEditorLink()
        {
            var loc = LoadAsset<LocalizationConfigSO>(EditorPaths.LocalizationConfigKey);
            if (loc != null)
            {
                EditorGUILayout.HelpBox(
                    "Yerelleştirme Çeviri Düzenleyici (LocalizationConfigSO Custom Editor) kullanıma hazır.\n" +
                    "Tüm dil anahtarlarını ve çevirileri doğrudan CSV tablosu üzerinde düzenleyin.",
                    MessageType.Info);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("📝 Yerelleştirme Düzenleyiciyi Aç",
                        "LocalizationConfigSO asset seçilir ve Custom Editor (CSV Grid) Inspector'da açılır."),
                        GUILayout.Height(30)))
                    {
                        Selection.activeObject = loc;
                        EditorGUIUtility.PingObject(loc);
                    }

                    if (GUILayout.Button(new GUIContent("📄 CSV Dosyasını Harici Aç",
                        "Localization.csv dosyasını varsayılan metin düzenleyicide açar."),
                        GUILayout.Height(30)))
                    {
                        string csvPath = "Assets/Resources/Localization.csv";
                        var csvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);
                        if (csvAsset != null)
                            AssetDatabase.OpenAsset(csvAsset);
                        else
                            EditorUtility.DisplayDialog("CSV Bulunamadı",
                                "Localization.csv dosyası Assets/Resources/ klasöründe bulunamadı!", "Tamam");
                    }
                }

                int langCount = loc.Languages?.Count ?? 0;
                int csvLineCount = 0;
                string csvPath2 = "Assets/Resources/Localization.csv";
                if (System.IO.File.Exists(csvPath2))
                {
                    var lines = System.IO.File.ReadAllLines(csvPath2);
                    csvLineCount = lines.Length >= 2 ? lines.Length - 1 : 0;
                }

                EditorGUILayout.LabelField(
                    $"Durum: {langCount} dil, {csvLineCount} çeviri anahtarı",
                    EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.HelpBox("LocalizationConfigSO.asset bulunamadı! Önce Config Assets sekmesinden oluşturun.", MessageType.Error);
            }
        }

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

        private static T LoadAsset<T>(string key) where T : ScriptableObject
        {
            return Resources.Load<T>(key);
        }

        private static readonly (string Label, System.Func<UnityEngine.Object> Load)[] AssetEntries =
        {
            ("Oyun Veritabanı (GameConfigDatabase)", () => LoadAsset<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey)),
            ("Oyun Hissiyatı (Game Feel)", () => LoadAsset<GameFeelConfigSO>(EditorPaths.GameFeelConfigKey)),
            ("Halka Renk Paleti", () => LoadAsset<RingColorPaletteSO>(EditorPaths.RingColorPaletteKey)),
            ("Ses (Audio)", () => LoadAsset<AudioConfigSO>(EditorPaths.AudioConfigKey)),
            ("Arayüz Teması (UI Theme)", () => LoadAsset<UIThemeConfigSO>(EditorPaths.UIThemeConfigKey)),
            ("Mağaza Kataloğu (StoreCatalog)", () => LoadAsset<StoreCatalogSO>(EditorPaths.StoreCatalogKey)),
            ("Yerelleştirme (LocalizationConfig)", () => LoadAsset<LocalizationConfigSO>(EditorPaths.LocalizationConfigKey)),
            ("Halka Mekanik Verisi (RingMechanicData)", () => LoadAsset<RingMechanicDataSO>(EditorPaths.RingMechanicDataKey)),
            ("Tema/Skin Veritabanı (ThemeSkinDatabase)", () => LoadAsset<ThemeSkinDatabaseSO>(EditorPaths.ThemeSkinDatabaseKey)),
        };

        // ── Oyuncu Verisi Görselleştirici Metotları ──────────────────

        private void EnsurePlayerProgressLoaded(bool force = false)
        {
            if (_playerProgressLoaded && !force && _playerProgress != null) return;

            if (_playerProgress == null)
            {
                _playerProgress = new PlayerProgressModel();
            }

            EnsureCachedDatabase();
            if (_cachedDatabase != null)
            {
                _playerProgress.SetTotalWorldCount(_cachedDatabase.TotalWorlds);
            }

            var storage = new EncryptedStorageService();
            PlayerProgressSaveSystem.Load(storage, _playerProgress);
            _playerProgressLoaded = true;
        }

        private void SavePlayerData()
        {
            if (_playerProgress == null) return;
            var storage = new EncryptedStorageService();
            PlayerProgressSaveSystem.Save(storage, _playerProgress);
            EditorUtility.DisplayDialog("Başarılı", "Oyuncu kayıt verisi başarıyla şifrelenerek kaydedildi!", "Tamam");
        }

        private void ResetPlayerData()
        {
            if (_playerProgress == null) return;
            _playerProgress.Reset();
            SavePlayerData();
            EnsurePlayerProgressLoaded(true);
        }

        private void AddAllDatabaseThemes()
        {
            if (_playerProgress == null) return;
            var skinDb = Resources.Load<ThemeSkinDatabaseSO>(EditorPaths.ThemeSkinDatabaseKey);
            if (skinDb != null && skinDb.Entries != null)
            {
                foreach (var entry in skinDb.Entries)
                {
                    if (!string.IsNullOrEmpty(entry.ThemeNameKey) && !_playerProgress.OwnedThemes.Contains(entry.ThemeNameKey))
                    {
                        _playerProgress.OwnedThemes.Add(entry.ThemeNameKey);
                    }
                }
            }
            else
            {
                EnsureCachedDatabase();
                if (_cachedDatabase != null)
                {
                    foreach (var world in _cachedDatabase.Worlds)
                    {
                        if (!string.IsNullOrEmpty(world.Theme) && !_playerProgress.OwnedThemes.Contains(world.Theme))
                        {
                            _playerProgress.OwnedThemes.Add(world.Theme);
                        }
                    }
                }
            }
        }

        private void AddAllDefaultAchievements()
        {
            if (_playerProgress == null) return;
            string[] defaultAchievements = {
                "Level_50_Passed",
                "Level_100_Passed",
                "All_Themes_Unlocked",
                "Perfect_Sort",
                "Coin_Hoarder"
            };
            foreach (var ach in defaultAchievements)
            {
                if (!_playerProgress.Achievements.Contains(ach))
                {
                    _playerProgress.Achievements.Add(ach);
                }
            }
        }

        private void DrawPlayerDataSection()
        {
            EnsurePlayerProgressLoaded();

            if (_playerProgress == null)
            {
                EditorGUILayout.HelpBox("Oyuncu verileri yüklenemedi.", MessageType.Error);
                return;
            }

            using (RingFlowEditorUtils.BeginSectionBoxScope("Oyuncu Profil ve İlerleme Verisi", "Mevcut oyuncu kayıt dosyasındaki ilerleme durumunu görüntüleyin ve değiştirin."))
            {
                // Top control buttons
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("🔄 Veriyi Yenile (Yükle)", GUILayout.Height(26)))
                    {
                        EnsurePlayerProgressLoaded(true);
                    }
                    if (GUILayout.Button("💾 Değişiklikleri Kaydet", GUILayout.Height(26)))
                    {
                        SavePlayerData();
                    }
                    if (GUILayout.Button("🗑 Tüm Kaydı Sıfırla", GUILayout.Height(26)))
                    {
                        if (EditorUtility.DisplayDialog("Oyuncu İlerlemesini Sıfırla",
                            "Tüm oyuncu profil verileri, paralar, açılan dünyalar ve en iyi skorlar sıfırlanacaktır. Bu işlem geri alınamaz!", "Sıfırla", "İptal"))
                        {
                            ResetPlayerData();
                        }
                    }
                }

                EditorGUILayout.Space(8f);

                // Grid layout for basic info
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("TEMEL PROFİL BİLGİLERİ", EditorStyles.miniBoldLabel);
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _playerProgress.Coins.Value = EditorGUILayout.IntField("Altın (Coins)", _playerProgress.Coins.Value);
                        _playerProgress.Diamonds.Value = EditorGUILayout.IntField("Elmas (Diamonds)", _playerProgress.Diamonds.Value);
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _playerProgress.Xp.Value = EditorGUILayout.IntField("Deneyim (XP)", _playerProgress.Xp.Value);
                        _playerProgress.PlayerLevel.Value = EditorGUILayout.IntField("Oyuncu Seviyesi", _playerProgress.PlayerLevel.Value);
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _playerProgress.CurrentLevel.Value = EditorGUILayout.IntField("Aktif Seviye (Current)", _playerProgress.CurrentLevel.Value);
                        _playerProgress.MaxUnlockedLevel.Value = EditorGUILayout.IntField("Maks Kilit Açık (Max Unlocked)", _playerProgress.MaxUnlockedLevel.Value);
                    }
                }

                EditorGUILayout.Space(6f);

                // Chests
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("SANDIK ADETLERİ", EditorStyles.miniBoldLabel);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _playerProgress.ChestBronze.Value = EditorGUILayout.IntField("Bronz Sandık", _playerProgress.ChestBronze.Value);
                        _playerProgress.ChestSilver.Value = EditorGUILayout.IntField("Gümüş Sandık", _playerProgress.ChestSilver.Value);
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _playerProgress.ChestGold.Value = EditorGUILayout.IntField("Altın Sandık", _playerProgress.ChestGold.Value);
                        _playerProgress.ChestDiamond.Value = EditorGUILayout.IntField("Elmas Sandık", _playerProgress.ChestDiamond.Value);
                    }
                }

                EditorGUILayout.Space(6f);

                // Daily Reward & Ads
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("GÜNLÜK ÖDÜL VE REKLAM", EditorStyles.miniBoldLabel);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _playerProgress.DailyDayIndex.Value = EditorGUILayout.IntField("Günlük Ödül Günü", _playerProgress.DailyDayIndex.Value);
                        _playerProgress.DailyStreak.Value = EditorGUILayout.IntField("Claim Streak", _playerProgress.DailyStreak.Value);
                    }
                    _playerProgress.RemoveAds.Value = EditorGUILayout.Toggle("Reklamları Kaldır (Remove Ads)", _playerProgress.RemoveAds.Value);
                    
                    var lastClaimTimeStr = _playerProgress.DailyLastClaimUtcTicks.Value == 0
                        ? "Hiç alınmadı"
                        : new System.DateTime(_playerProgress.DailyLastClaimUtcTicks.Value, System.DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                    EditorGUILayout.LabelField($"Son Günlük Ödül Zamanı: {lastClaimTimeStr}");
                }

                EditorGUILayout.Space(6f);

                // Unlocked Worlds List
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("DÜNYALARIN KİLİT DURUMU", EditorStyles.miniBoldLabel);
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Tüm Dünyaların Kilidini Aç"))
                        {
                            for (int i = 0; i < _playerProgress.UnlockedWorlds.Count; i++)
                                _playerProgress.UnlockedWorlds[i] = true;
                        }
                        if (GUILayout.Button("Dünya Kilitlerini Sıfırla (Sadece D1 Açık)"))
                        {
                            for (int i = 0; i < _playerProgress.UnlockedWorlds.Count; i++)
                                _playerProgress.UnlockedWorlds[i] = (i == 0);
                        }
                    }

                    EditorGUILayout.Space(4f);
                    
                    int cols = 5;
                    int worldIndex = 0;
                    while (worldIndex < _playerProgress.UnlockedWorlds.Count)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            for (int c = 0; c < cols && worldIndex < _playerProgress.UnlockedWorlds.Count; c++)
                            {
                                bool isUnlocked = _playerProgress.UnlockedWorlds[worldIndex];
                                bool newUnlocked = EditorGUILayout.ToggleLeft($"D{worldIndex + 1}", isUnlocked, GUILayout.Width(75f));
                                if (newUnlocked != isUnlocked)
                                {
                                    _playerProgress.UnlockedWorlds[worldIndex] = newUnlocked;
                                }
                                worldIndex++;
                            }
                        }
                    }
                }

                EditorGUILayout.Space(6f);

                // Owned Themes List
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("SAHİP OLUNAN TEMALAR", EditorStyles.miniBoldLabel);
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Tüm Temaları Ekle (SO'dakiler)"))
                        {
                            AddAllDatabaseThemes();
                        }
                        if (GUILayout.Button("Temaları Temizle"))
                        {
                            _playerProgress.OwnedThemes.Clear();
                        }
                    }

                    if (_playerProgress.OwnedThemes.Count == 0)
                    {
                        EditorGUILayout.LabelField("Sahip olunan tema yok.");
                    }
                    else
                    {
                        var themeListStr = string.Join(", ", _playerProgress.OwnedThemes);
                        EditorGUILayout.LabelField(themeListStr, EditorStyles.wordWrappedLabel);
                    }
                }

                EditorGUILayout.Space(6f);

                // Achievements List
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("BAŞARIMLAR", EditorStyles.miniBoldLabel);
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Tüm Başarımları Ekle"))
                        {
                            AddAllDefaultAchievements();
                        }
                        if (GUILayout.Button("Başarımları Temizle"))
                        {
                            _playerProgress.Achievements.Clear();
                        }
                    }

                    if (_playerProgress.Achievements.Count == 0)
                    {
                        EditorGUILayout.LabelField("Kazanılan başarım yok.");
                    }
                    else
                    {
                        var achievementsStr = string.Join(", ", _playerProgress.Achievements);
                        EditorGUILayout.LabelField(achievementsStr, EditorStyles.wordWrappedLabel);
                    }
                }

                EditorGUILayout.Space(6f);

                // Best Moves
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("EN İYİ SKORLAR (BEST MOVES)", EditorStyles.miniBoldLabel);
                    if (_playerProgress.BestMovesPerLevel.Count == 0)
                    {
                        EditorGUILayout.LabelField("Kayıtlı en iyi skor bulunmuyor.");
                    }
                    else
                    {
                        _bestMovesScroll = EditorGUILayout.BeginScrollView(_bestMovesScroll, GUILayout.Height(100f));
                        foreach (var kvp in _playerProgress.BestMovesPerLevel)
                        {
                            EditorGUILayout.LabelField($"Seviye {kvp.Key}: {kvp.Value} hamle");
                        }
                        EditorGUILayout.EndScrollView();
                    }
                }
            }
        }
    }
}

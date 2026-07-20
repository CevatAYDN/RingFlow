using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    [CustomEditor(typeof(LevelDataSO))]
    public class LevelDataSOEditor : UnityEditor.Editor
    {
        private bool _showRawData;
        private bool _showTools = true;
        private bool _cachedShowRawData;
        private bool _cachedShowTools = true;
        private bool _cachedShowStepSolve;
        private int _lastValidatedHash;
        private string[] _cachedWarnings = System.Array.Empty<string>();

        // LAYOUT-CACHE: Unity IMGUI fires OnGUI multiple times per frame (Layout →
        // Repaint → input). Width-derived layout values must be computed exactly
        // once per Layout event and reused on later Repaint/Used events so grid
        // geometry, pole widths, and button rects stay consistent. Without this
        // cache, Layout computes one width and Repaint sees another, causing the
        // "PlayMode sequential level jump" bug because a cached controlID matches
        // the wrong rect on the input phase.
        private float _cachedViewWidth;
        private bool _viewWidthCached;

        // STEP-SOLVE: adım adım çözüm önizleme durumu. VerifyAndSolve'nin aksine
        // TargetMoves'u hemen yazmaz; çözümü bir liste olarak saklar ve kullanıcı
        // ← / → butonlarıyla adım adım inceler. Kaydetmek için ayrı bir buton vardır.
        private System.Collections.Generic.List<MoveRecord> _stepMoves;
        private int _stepIndex = -1;
        private string _stepStatus = string.Empty;
        private Vector2 _stepScroll;

        private static RingColor s_brushColor = RingColor.Red;
        private static RingType s_brushType = RingType.Standard;
        private static bool s_eraserMode;
        private static int s_bombCounter = 3;

        private static GUIStyle s_compactButtonStyle;
        private static GUIStyle s_boldButtonStyle;
        private static GUIStyle s_ringButtonStyle;
        private static GUIStyle s_addSlotStyle;
        private static GUIStyle s_lockLabelStyle;
        private static GUIStyle s_portalLabelStyle;
        private static GUIStyle s_warningTitleStyle;

        private static RingColor[] s_cachedColors;
        private static RingType[] s_cachedTypes;

        private static RingColor[] CachedColors
            => s_cachedColors ??= (RingColor[])System.Enum.GetValues(typeof(RingColor));

        private static RingType[] CachedTypes
            => s_cachedTypes ??= (RingType[])System.Enum.GetValues(typeof(RingType));

        // Cached palette load — replaced 48+ Resources.Load calls per frame with one.
        private static RingColorPaletteSO s_cachedPalette;
        private static float s_paletteCacheTime = -1f;
        private const float PaletteCacheSeconds = 5f;

        private static GUIStyle CompactButton => s_compactButtonStyle ??= new GUIStyle(GUI.skin.button)
            { fontSize = 9, fontStyle = FontStyle.Normal };

        private static GUIStyle BoldCompactButton => s_boldButtonStyle ??= new GUIStyle(GUI.skin.button)
            { fontSize = 9, fontStyle = FontStyle.Bold };

        private static GUIStyle RingButtonStyle
            => s_ringButtonStyle ??= new GUIStyle(GUI.skin.button)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold
            };

        private static GUIStyle AddSlotStyle
            => s_addSlotStyle ??= new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

        private static GUIStyle LockLabelStyle
            => s_lockLabelStyle ??= new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

        private static GUIStyle PortalLabelStyle
            => s_portalLabelStyle ??= new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

        private static GUIStyle WarningTitleStyle
            => s_warningTitleStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = EditorPaths.EditorColors.Error }
            };

        private static RingColorPaletteSO GetCachedPalette()
        {
            float now = (float)EditorApplication.timeSinceStartup;
            if (s_cachedPalette != null && now - s_paletteCacheTime < PaletteCacheSeconds)
                return s_cachedPalette;

            s_cachedPalette = Resources.Load<RingColorPaletteSO>(EditorPaths.RingColorPaletteKey);
            if (s_cachedPalette == null)
            {
                EditorGUILayout.HelpBox("RingColorPaletteSO bulunamadı! Lütfen Resources klasöründe oluşturun.", MessageType.Error);
                return null;
            }

            s_paletteCacheTime = now;
            return s_cachedPalette;
        }

        public override void OnInspectorGUI()
        {
            // LAYOUT-CACHE: refresh cached width strictly on Layout event so
            // Repaint/Used events reuse the same geometry computed earlier.
            if (Event.current.type == EventType.Layout)
            {
                _cachedViewWidth = EditorGUIUtility.currentViewWidth;
                _viewWidthCached = true;
                _cachedShowTools = _showTools;
                _cachedShowRawData = _showRawData;
                _cachedShowStepSolve = _stepMoves != null && _stepMoves.Count > 0;
            }

            var levelSO = (LevelDataSO)target;
            if (levelSO == null || levelSO.Data == null)
            {
                base.OnInspectorGUI();
                return;
            }

            serializedObject.Update();

            DrawHeader($"SEVİYE {levelSO.Data.LevelIndex} YAPILANDIRMASI");

            // --- BÜYÜK OYNA BUTONU ---
            // EVENT-GUARD: accept only genuine user clicks. Without this guard,
            // Layout/Repaint events were occasionally firing Play(), causing the
            // sequential-level-jump bug when entering PlayMode from a level asset.
            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = EditorPaths.EditorColors.Success;
            if (GUILayout.Button("▶ OYUNU BAŞLAT VE SEVİYEYİ OYNA (PLAY LEVEL)", GUILayout.Height(36)))
            {
                if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp || Event.current.type == EventType.Used)
                    EditorPlayFromLevel.Play(levelSO.Data.LevelIndex);
            }
            GUI.backgroundColor = prevColor;
            EditorGUILayout.Space(4f);

            int currentHash = ComputeValidationHash(levelSO.Data);
            if (currentHash != _lastValidatedHash)
            {
                _cachedWarnings = BuildWarnings(levelSO.Data);
                _lastValidatedHash = currentHash;
            }

            if (_cachedWarnings.Length > 0)
            {
                RingFlowEditorUtils.BeginSectionBox("⚠️ GDD Uyumluluk Uyarıları", "Seçilen seviyenin GDD kurallarına uygunluğunu denetleyin.");
                for (int i = 0; i < _cachedWarnings.Length; i++)
                    EditorGUILayout.LabelField(_cachedWarnings[i], EditorStyles.wordWrappedMiniLabel);
                RingFlowEditorUtils.EndSectionBox();
                EditorGUILayout.Space(4f);
            }

            RingFlowEditorUtils.BeginSectionBox("Seviye Ayarları (Level Settings)", "Seviyenin endeksi, tohumu ve hedeflenen hamle limiti.");
            EditorGUI.BeginChangeCheck();
            levelSO.Data.LevelIndex = EditorGUILayout.IntField(new GUIContent("Seviye Endeksi", "Bu seviyenin oyundaki sıra numarası."), levelSO.Data.LevelIndex);
            levelSO.Data.Seed = EditorGUILayout.IntField(new GUIContent("Rastgele Tohum (Seed)", "Seviyenin üretilmesinde kullanılan rastgelelik tohumu. Aynı tohum değeri her zaman aynı seviyeyi oluşturur."), levelSO.Data.Seed);
            levelSO.Data.TargetMoves = EditorGUILayout.IntField(new GUIContent("Hedef Hamle", "Oyuncunun bu seviyeyi tamamlaması için hedeflenen hamle sayısı. Yapay zeka çözücüsü çalıştırıldığında bu değer otomatik olarak en kısa çözüm yoluyla güncellenir."), levelSO.Data.TargetMoves);
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(levelSO);
            RingFlowEditorUtils.EndSectionBox();

            DrawLevelSummary(levelSO.Data);
            DrawDifficultyAndMechanicPreview(levelSO.Data);

            _showTools = EditorGUILayout.Foldout(_showTools, "Seviye Düzenleme Araçları", true, EditorStyles.foldoutHeader);
            if (_cachedShowTools)
            {
                DrawColorPalette();
                DrawTypePalette();
                DrawLevelVisualInteractive(levelSO.Data, levelSO);

                RingFlowEditorUtils.BeginSectionBox("Seviye Tasarımcı Araçları (Designer Actions)", "Optimal hamleyi yapay zeka ile çözdürün, direk ekleyin veya kaldırın.");

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Seviyeyi Doğrula & Çöz", GUILayout.Height(30)))
                        VerifyAndSolve(levelSO);

                    var prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = EditorPaths.EditorColors.Info;
                    if (GUILayout.Button("Adım Adım Çöz (Önizle)", GUILayout.Height(30)) && IsUserEvent())
                        RunStepSolver(levelSO);
                    GUI.backgroundColor = prevBg;
                }

                EditorGUILayout.Space(2f);
                if (GUILayout.Button("SAHNEYİ KUR VE GÖRSELLEŞTİR (BUILD BOARD IN SCENE)", GUILayout.Height(30)))
                {
                    if (IsUserEvent())
                    {
                        var db = Resources.Load<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey);
                        VisualBuilderSection.BuildLevelInScene(levelSO.Data, db);
                    }
                }

                if (_cachedShowStepSolve)
                {
                    EditorGUILayout.Space(2f);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField($"Adım Adım Çözüm Önizlemesi — Toplam {_stepMoves.Count} Hamle", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(_stepStatus, EditorStyles.miniLabel);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("<< Önceki", GUILayout.Width(90f), GUILayout.Height(22f)) && IsUserEvent())
                                _stepIndex--;

                            EditorGUILayout.LabelField(
                                _stepIndex < 0 ? "Başlangıç" : $"Adım {_stepIndex + 1} / {_stepMoves.Count}",
                                EditorStyles.centeredGreyMiniLabel, GUILayout.Width(140f));

                            if (GUILayout.Button("Sonraki >>", GUILayout.Width(90f), GUILayout.Height(22f)) && IsUserEvent())
                                _stepIndex++;
                        }

                        if (_stepIndex >= 0 && _stepIndex < _stepMoves.Count)
                        {
                            var mv = _stepMoves[_stepIndex];
                            EditorGUILayout.HelpBox(
                                $"Hamle {_stepIndex + 1}: Direk {mv.FromPoleId}'den Direk {mv.ToPoleId}'ye halka taşı (halka: renk={mv.Ring.Color}, tip={mv.Ring.Type}).",
                                MessageType.Info);
                        }
                        else if (_stepIndex == -1)
                        {
                            EditorGUILayout.HelpBox("Başlangıç durumu. 'Sonraki >>' ile ilk hamleyi görebilirsiniz.", MessageType.None);
                        }

                        EditorGUILayout.Space(2f);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Hamleler Listesini Gizle", GUILayout.Width(160f), GUILayout.Height(20f)) && IsUserEvent())
                            {
                                _stepMoves = null;
                                _stepIndex = -1;
                                _stepStatus = string.Empty;
                            }

                            var prevBg2 = GUI.backgroundColor;
                            GUI.backgroundColor = EditorPaths.EditorColors.Success;
                            if (GUILayout.Button("✓ Hedef Hamleyi Yaz ve Kaydet", GUILayout.Height(20f)) && IsUserEvent())
                            {
                                Undo.RecordObject(levelSO, "Adım Çözücü: Hedef Hamle Yaz");
                                levelSO.Data.TargetMoves = _stepMoves.Count;
                                var database = Resources.Load<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey);
                                if (database != null)
                                {
                                    var board = BuildBoardStateFromLevelData(levelSO.Data, database, out int maxCapacity, out int[] portalTargets);
                                    LevelGenerator.PopulateGddMetadata(levelSO.Data, database, board, _stepMoves.Count, maxCapacity, portalTargets);
                                }
                                EditorUtility.SetDirty(levelSO);
                                AssetDatabase.SaveAssets();
                                EditorUtility.DisplayDialog("Hedef Hamle Güncellendi",
                                    $"Seviye {levelSO.Data.LevelIndex} için Hedef Hamle = {_stepMoves.Count} olarak kaydedildi.",
                                    "Tamam");
                            }
                            GUI.backgroundColor = prevBg2;
                        }
                    }
                }

                EditorGUILayout.Space(2f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Seviyeyi Yeniden Karıştır", GUILayout.Height(30)))
                        ReScramble(levelSO);
                }

                EditorGUILayout.Space(2f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Boş Direk Ekle", GUILayout.Height(24)))
                    {
                        var db = Resources.Load<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey);
                        if (db == null)
                        {
                            EditorGUILayout.HelpBox("GameConfigDatabaseSO bulunamadı. Ring kapasitesi belirlenemiyor.", MessageType.Error);
                            return;
                        }
                        int ringCapacity = levelSO.Data.Poles.Count > 0 
                            ? levelSO.Data.Poles[0].RingCapacity 
                            : db.GetMaxCapacityForLevel(levelSO.Data.LevelIndex);
                        Undo.RecordObject(levelSO, "Direk Ekle");
                        levelSO.Data.Poles.Add(new PoleData(ringCapacity));
                        EditorUtility.SetDirty(levelSO);
                    }

                    if (levelSO.Data.Poles.Count > 0 && GUILayout.Button("Son Direği Kaldır", GUILayout.Height(24)))
                    {
                        if (EditorUtility.DisplayDialog("Direği Kaldır", "Son direği bu seviye yapılandırmasından kaldırmak istediğinize emin misiniz?", "Kaldır", "İptal"))
                        {
                            Undo.RecordObject(levelSO, "Direk Kaldır");
                            levelSO.Data.Poles.RemoveAt(levelSO.Data.Poles.Count - 1);
                            EditorUtility.SetDirty(levelSO);
                        }
                    }
                }
                RingFlowEditorUtils.EndSectionBox();
            }

            EditorGUILayout.Space(5f);
            _showRawData = EditorGUILayout.Foldout(_showRawData, "Ham Serileştirilmiş Veri", true);
            if (_cachedShowRawData)
                DrawPropertiesExcluding(serializedObject, "m_Script");

            if (serializedObject.ApplyModifiedProperties() || GUI.changed)
                EditorUtility.SetDirty(levelSO);
        }

        private void VerifyAndSolve(LevelDataSO levelSO)
        {
            var database = Resources.Load<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey);
            if (database == null)
            {
                EditorUtility.DisplayDialog("Veritabanı Eksik", "GameConfigDatabaseSO bulunamadı. Lütfen önce oluşturun.", "Tamam");
                return;
            }

            var board = BuildBoardStateFromLevelData(levelSO.Data, database, out int maxCapacity, out int[] portalTargets);
            int levelIndex = levelSO.Data.LevelIndex;
            var band = database.GetBandForLevel(levelIndex);
            var allowedMechanics = database.GetAllowedMechanicsForLevel(levelIndex);
            int intensity = database.GetMechanicIntensityForLevel(levelIndex);

            var solveResult = LevelSolver.Solve(board, maxCapacity, portalTargets: portalTargets);

            if (solveResult.IsSolvable)
            {
                Undo.RecordObject(levelSO, "Hedef Hamleleri Güncelle");
                levelSO.Data.TargetMoves = solveResult.MoveCount;
                LevelGenerator.PopulateGddMetadata(levelSO.Data, database, board, solveResult.MoveCount, maxCapacity, portalTargets);
                EditorUtility.DisplayDialog("Çözücü Sonuçları",
                    $"Seviye ÇÖZÜLEBİLİR!\nBand: {band}\nMekanik Yoğunluğu: {intensity}\nOptimal gereken hamle sayısı: {solveResult.MoveCount} (Hedef Hamle güncellendi).", "Tamam");
            }
            else
            {
                EditorUtility.DisplayDialog("Çözücü Sonuçları",
                    $"Seviye ÇÖZÜLEMEZ!\nBand: {band}\nAçık Mekanikler: {string.Join(", ", allowedMechanics)}\nBu yapılandırmayı çözebilecek geçerli bir hamle sırası bulunamadı.", "Tamam");
            }
            EditorUtility.SetDirty(levelSO);
        }

        /// <summary>
        /// Adım Adım Çöz butonu için metod. VerifyAndSolve'nin aksine TargetMoves'u
        /// hemen YAZMAZ; çözüm hamle listesini _stepMoves içine kopyalar ve kullanıcı
        /// bir "✓ Hedef Hamleyi Yaz ve Kaydet" butonuna basana kadar asset'i değiştirmez.
        /// Bu sayede tasarımcı çözümü önizleyip iptal edebilir.
        /// </summary>
        private static bool IsUserEvent()
        {
            return Event.current.type == EventType.MouseDown
                || Event.current.type == EventType.MouseUp
                || Event.current.type == EventType.Used;
        }

        private void RunStepSolver(LevelDataSO levelSO)
        {
            var database = Resources.Load<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey);
            if (database == null)
                throw new System.InvalidOperationException("GameConfigDatabase not found!");

            var board = BuildBoardStateFromLevelData(levelSO.Data, database, out int maxCapacity, out int[] portalTargets);

            var solveResult = LevelSolver.Solve(board, maxCapacity, portalTargets: portalTargets);

            if (solveResult.IsSolvable && solveResult.Moves != null && solveResult.Moves.Count > 0)
            {
                _stepMoves = solveResult.Moves;
                _stepIndex = -1;
                _stepStatus = $"Çözülebilir: {solveResult.MoveCount} hamle. 'Sonraki >>' ile adım adım inceleyin.";
            }
            else
            {
                _stepMoves = null;
                _stepIndex = -1;
                _stepStatus = solveResult.IsSolvable
                    ? "Tahta zaten çözülmüş durumda — hamle yok."
                    : "Çözülemez yapılandırma! Lütfen direk/halka düzenini kontrol edin.";
                EditorUtility.DisplayDialog("Adım Adım Çözücü",
                    _stepStatus, "Tamam");
            }
        }

        private static BoardState BuildBoardStateFromLevelData(LevelData levelData, GameConfigDatabaseSO database, out int maxCapacity, out int[] portalTargets)
        {
            if (levelData == null)
            {
                Debug.LogError("[LevelDataSOEditor] LevelData is null. Cannot build board state.");
                maxCapacity = BoardState.MaxSupportedCapacity;
                portalTargets = System.Array.Empty<int>();
                return new BoardState();
            }

            int poleCount = levelData.Poles?.Count ?? 0;
            maxCapacity = database != null ? database.GetMaxCapacityForLevel(levelData.LevelIndex) : GameplayAssetKeys.Tuning.MaxCapacity;
            portalTargets = new int[poleCount];
            for (int i = 0; i < poleCount; i++) portalTargets[i] = -1;

            var board = new BoardState { PoleCount = poleCount, MaxCapacity = maxCapacity };
            for (int p = 0; p < poleCount; p++)
            {
                var pole = levelData.Poles[p];
                if (pole == null) continue;

                if (pole.RingCapacity > BoardState.MaxSupportedCapacity)
                {
                    Debug.LogError(
                        $"[LevelDataSOEditor] Level {levelData.LevelIndex} pole {p} capacity {pole.RingCapacity} exceeds BoardState.MaxSupportedCapacity={BoardState.MaxSupportedCapacity}.");
                    continue;
                }

                if (pole.RingCapacity > maxCapacity) maxCapacity = pole.RingCapacity;
                board.MaxCapacity = maxCapacity;
                board.SetPoleLocked(p, pole.IsLocked);
                portalTargets[p] = pole.PortalTargetId;

                int count = pole.Rings?.Count ?? 0;
                if (count > pole.RingCapacity)
                {
                    Debug.LogError(
                        $"[LevelDataSOEditor] Level {levelData.LevelIndex} pole {p} has {count} rings but capacity is {pole.RingCapacity}.");
                    continue;
                }

                board.SetRingCount(p, count);
                for (int r = 0; r < count; r++)
                {
                    var ring = pole.Rings[r];
                    board.SetRingColor(p, r, ring.Color);
                    board.SetRingType(p, r, ring.Type);
                    board.SetRingAdditional(p, r, ring.AdditionalData);
                }

                if (count > 0)
                    board.SetTopRingFrozen(p, pole.Rings[count - 1].Type == RingType.Frozen);
            }

            board.MaxCapacity = maxCapacity;
            return board;
        }

        private void ReScramble(LevelDataSO levelSO)
        {
            if (levelSO.Data.Poles.Count == 0) return;

            int maxCap = levelSO.Data.Poles[0].RingCapacity;
            var colorsList = new HashSet<RingColor>();
            foreach (var pole in levelSO.Data.Poles)
                foreach (var ring in pole.Rings)
                    if (ring.Color != RingColor.None)
                        colorsList.Add(ring.Color);

            int colorCount = colorsList.Count;
            if (colorCount == 0)
            {
                EditorUtility.DisplayDialog("Yeniden Karıştırma Hatası", "Karıştırmak için seviyede renkli halka bulunamadı.", "Tamam");
                return;
            }

            var database = Resources.Load<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey);
            int levelIndex = levelSO.Data.LevelIndex;
            var band = database.GetBandForLevel(levelIndex);
            int intensity = database.GetMechanicIntensityForLevel(levelIndex);
            var allowedMechanics = database.GetAllowedMechanicsForLevel(levelIndex);

            if (EditorUtility.DisplayDialog("Yeniden Karıştır",
                $"Seviye {levelIndex}, {levelSO.Data.Seed} tohumu ile yeniden üretilsin mi?\nBand: {band}\nMekanik Yoğunluğu: {intensity}\nAçık Mekanikler: {string.Join(", ", allowedMechanics)}\n\nUyarı: Yapılan tüm manuel düzenlemeler kaybolacaktır.",
                "Karıştır", "İptal"))
            {
                Undo.RecordObject(levelSO, "Seviyeyi Yeniden Karıştır");
                var generated = LevelGenerator.GenerateLevel(
                    database, levelSO.Data.LevelIndex, levelSO.Data.Seed,
                    levelSO.Data.Poles.Count, colorCount, maxCap);

                if (generated != null)
                {
                    levelSO.Data = generated;
                    EditorUtility.DisplayDialog("Yeniden Karıştır", "Seviye başarıyla üretildi ve karıştırıldı!", "Tamam");
                }
                else
                {
                    EditorUtility.DisplayDialog("Yeniden Karıştırma Başarısız", "Bu parametrelerle çözülebilir bir seviye üretilemedi.", "Tamam");
                }
            }
        }

        private static void DrawLevelSummary(LevelData levelData)
        {
            if (levelData == null)
            {
                return;
            }

            int poleCount = levelData.Poles != null ? levelData.Poles.Count : 0;
            int totalRings = 0;
            int maxCapacity = 0;
            for (int i = 0; i < poleCount; i++)
            {
                var pole = levelData.Poles[i];
                if (pole == null) continue;
                totalRings += pole.Rings != null ? pole.Rings.Count : 0;
                if (pole.RingCapacity > maxCapacity) maxCapacity = pole.RingCapacity;
            }

            RingFlowEditorUtils.BeginSectionBox("Seviye Özeti (Summary)", "Direk ve halka yapılarının genel istatistikleri.");
            EditorGUILayout.LabelField("Direk Sayısı", poleCount.ToString());
            EditorGUILayout.LabelField("Toplam Halka", totalRings.ToString());
            EditorGUILayout.LabelField("Maks. Kapasite", maxCapacity.ToString());
            RingFlowEditorUtils.EndSectionBox();
        }

        private static void DrawDifficultyAndMechanicPreview(LevelData levelData)
        {
            if (levelData == null)
            {
                return;
            }

            var database = Resources.Load<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey);
            if (database == null)
            {
                EditorGUILayout.HelpBox("GameConfigDatabaseSO bulunamadı. Zorluk ve mekanik önizlemesi gösterilemiyor.", MessageType.Warning);
                return;
            }
            int levelIndex = levelData.LevelIndex;
            var band = database.GetBandForLevel(levelIndex);
            var allowed = database.GetAllowedMechanicsForLevel(levelIndex);
            int intensity = database.GetMechanicIntensityForLevel(levelIndex);
            int minEmptyPoles = database.GetMinEmptyPolesForLevel(levelIndex);
            int poleCount = database.GetPoleCountForLevel(levelIndex);
            int colorCount = database.GetColorCountForLevel(levelIndex);
            int maxCapacity = database.GetMaxCapacityForLevel(levelIndex);

            RingFlowEditorUtils.BeginSectionBox("Zorluk ve Mekanik Önizleme (Difficulty & Mechanics)", "Seviyenin zorluk grubu ve izin verilen mekanikleri.");
            EditorGUILayout.LabelField("Band", band.ToString());
            EditorGUILayout.LabelField("Renk Sayısı", colorCount.ToString());
            EditorGUILayout.LabelField("Direk Sayısı", poleCount.ToString());
            EditorGUILayout.LabelField("Kapasite", maxCapacity.ToString());
            EditorGUILayout.LabelField("Direk Kapasite Etiketi", levelData.Poles.Count > 0 ? levelData.Poles[0].CapacityText : "Yok");
            EditorGUILayout.LabelField("Boş Direk", minEmptyPoles.ToString());
            EditorGUILayout.LabelField("Mekanik Yoğunluğu", intensity.ToString());
            EditorGUILayout.LabelField("Açık Mekanikler", string.Join(", ", allowed));
            RingFlowEditorUtils.EndSectionBox();
        }

        private static void DrawColorPalette()
        {
            RingFlowEditorUtils.BeginSectionBox("Renk Fırçası Seçin (Color Palette Brush)", "Halkaları boyamak veya silmek için bir renk fırçası belirleyin.");

            var colors = CachedColors;
            var prevColor = GUI.backgroundColor;
            var palette = GetCachedPalette();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = s_eraserMode ? Color.red : Color.gray;
                var eraserStyle = CompactButton;
                eraserStyle.fontStyle = s_eraserMode ? FontStyle.Bold : FontStyle.Normal;
                eraserStyle.normal.textColor = s_eraserMode ? Color.white : Color.black;
                if (GUILayout.Button("SİLGİ", eraserStyle, GUILayout.Width(70), GUILayout.Height(24)))
                    s_eraserMode = true;
                GUI.backgroundColor = prevColor;

                GUILayout.Space(8f);

                for (int i = 1; i < colors.Length; i++)
                {
                    var color = colors[i];
                    Color c = palette != null ? palette.GetColor(color, RingColorPaletteSO.ColorBlindMode.Off) : Color.grey;

                    bool isSelected = (!s_eraserMode && s_brushColor == color);
                    GUI.backgroundColor = c;

                    string label = isSelected
                        ? $"[{color.ToString().Substring(0, 3).ToUpper()}]"
                        : color.ToString().Substring(0, 3).ToUpper();

                    var style = CompactButton;
                    style.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
                    style.normal.textColor = RingFlowEditorUtils.GetContrastColor(c);

                    if (GUILayout.Button(label, style, GUILayout.Width(42), GUILayout.Height(24)))
                    {
                        s_brushColor = color;
                        s_eraserMode = false;
                    }
                }
                GUI.backgroundColor = prevColor;
            }
            RingFlowEditorUtils.EndSectionBox();
        }

        private static void DrawTypePalette()
        {
            if (s_eraserMode) return;

            EditorGUILayout.Space(2f);
            RingFlowEditorUtils.BeginSectionBox("Halka Tipi Fırçası Seçin (Ring Type Brush)", "Boyanacak halkanın davranış mekanik tipini belirleyin.");

            var types = CachedTypes;
            var prevBg = GUI.backgroundColor;
            int typesPerRow = 5;

            for (int i = 0; i < types.Length; i += typesPerRow)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int j = i; j < i + typesPerRow && j < types.Length; j++)
                    {
                        var type = types[j];
                        bool isSelected = s_brushType == type;

                        if (isSelected)
                        {
                            GUI.backgroundColor = EditorPaths.EditorColors.Info;
                            if (GUILayout.Button(type.ToString(), CompactButton, GUILayout.Height(20)))
                                s_brushType = type;
                            GUI.backgroundColor = prevBg;
                        }
                        else
                        {
                            if (GUILayout.Button(type.ToString(), CompactButton, GUILayout.Height(20)))
                                  s_brushType = type;
                        }
                    }
                }
            }

            if (s_brushType == RingType.Bomb)
            {
                EditorGUILayout.Space(2f);
                s_bombCounter = EditorGUILayout.IntSlider("Bomba Sayaç Değeri", s_bombCounter, 1, 9);
            }
            RingFlowEditorUtils.EndSectionBox();
        }

        private void DrawLevelVisualInteractive(LevelData levelData, LevelDataSO levelSO)
        {
            if (levelData == null || levelData.Poles == null || levelData.Poles.Count == 0)
            {
                EditorGUILayout.HelpBox("Gösterilecek seviye verisi yok.", MessageType.Info);
                return;
            }

            RingFlowEditorUtils.BeginSectionBox("Görsel Seviye Tasarımcısı (Interactive Board Designer)", "Boyamak veya silmek istediğiniz halka kutusuna tıklayın.");

            // LAYOUT-CACHE: use cached view width so Layout/Repaint phases agree on
            // pole width. Without it, Layout computes width A, Repaint sees width B,
            // and button hit-rects drift — which contributed to the PlayMode bug.
            float viewWidth = _viewWidthCached ? _cachedViewWidth : EditorGUIUtility.currentViewWidth;
            float poleWidth = Mathf.Clamp((viewWidth - 48f) / Mathf.Max(1, levelData.Poles.Count), 56f, 96f);
            float ringHeight = Mathf.Clamp(poleWidth * 0.28f, 18f, 24f);
            float poleGap = 8f;
            var palette = GetCachedPalette();

            using (new EditorGUILayout.HorizontalScope())
            {
                var prevColor = GUI.backgroundColor;
                for (int p = 0; p < levelData.Poles.Count; p++)
                {
                    var pole = levelData.Poles[p];

                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(poleWidth)))
                    {
                        EditorGUILayout.LabelField($"Direk {p}", RingFlowEditorUtils.CenteredMiniLabel, GUILayout.Width(poleWidth));

                        int maxCapacity = pole.RingCapacity;
                        float height = maxCapacity * (ringHeight + 2f) + 12f;
                        Rect rect = GUILayoutUtility.GetRect(poleWidth, height);

                        Color colBg = pole.IsLocked ? new Color(0.18f, 0.12f, 0.12f, 1f) : new Color(0.16f, 0.16f, 0.18f, 1f);
                        Color borderCol = pole.IsLocked ? new Color(0.8f, 0.2f, 0.2f) : new Color(0.35f, 0.35f, 0.38f);

                        EditorGUI.DrawRect(rect, colBg);
                        RingFlowEditorUtils.DrawRectBorder(rect, borderCol, 1);

                        for (int r = 0; r < maxCapacity; r++)
                        {
                            float ringY = rect.yMax - 6f - (r + 1) * (ringHeight + 2f);
                            Rect ringRect = new Rect(rect.x + 4f, ringY, poleWidth - 8f, ringHeight);

                            bool hasRing = r < pole.Rings.Count;
                            bool isAddSlot = r == pole.Rings.Count;

                            if (hasRing)
                            {
                                var ring = pole.Rings[r];
                                Color ringColor = palette != null ? palette.GetColor(ring.Color, RingColorPaletteSO.ColorBlindMode.Off) : Color.grey;
                                GUI.backgroundColor = ringColor;

                                string label = RingFlowEditorUtils.GetRingShortLabel(ring.Type);
                                if (ring.AdditionalData > 0 && ring.Type == RingType.Bomb)
                                    label += ring.AdditionalData;

                                var style = RingButtonStyle;
                                style.normal.textColor = RingFlowEditorUtils.GetContrastColor(ringColor);

                                if (GUI.Button(ringRect, label, style))
                                {
                                    Undo.RecordObject(levelSO, "Halka Değiştir");
                                    if (s_eraserMode)
                                        pole.Rings.RemoveAt(r);
                                    else
                                        pole.Rings[r] = new RingData(s_brushColor, s_brushType, s_brushType == RingType.Bomb ? s_bombCounter : 0);
                                    EditorUtility.SetDirty(levelSO);
                                }
                            }
                            else if (isAddSlot)
                            {
                                GUI.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

                                if (!s_eraserMode && GUI.Button(ringRect, "+", AddSlotStyle))
                                {
                                    Undo.RecordObject(levelSO, "Halka Ekle");
                                    pole.Rings.Add(new RingData(s_brushColor, s_brushType, s_brushType == RingType.Bomb ? s_bombCounter : 0));
                                    EditorUtility.SetDirty(levelSO);
                                }
                            }
                            else
                            {
                                Rect emptyRect = new Rect(rect.x + 6f, ringY + 2f, poleWidth - 12f, ringHeight - 4f);
                                EditorGUI.DrawRect(emptyRect, new Color(0.25f, 0.25f, 0.25f, 0.2f));
                                RingFlowEditorUtils.DrawRectBorder(emptyRect, new Color(0.35f, 0.35f, 0.38f, 0.3f), 1);
                            }
                        }

                        if (pole.IsLocked)
                        {
                            Rect lockRect = new Rect(rect.x + 3f, rect.y + 4f, poleWidth - 6f, 13f);
                            EditorGUI.DrawRect(lockRect, new Color(0.8f, 0.1f, 0.1f, 0.9f));
                            GUI.Label(lockRect, "KİLİTLİ", LockLabelStyle);
                        }

                        if (pole.PortalTargetId >= 0)
                        {
                            Rect portalRect = new Rect(rect.x + 3f, rect.yMax - 16f, poleWidth - 6f, 13f);
                            EditorGUI.DrawRect(portalRect, new Color(0.0f, 0.6f, 0.8f, 0.9f));
                            GUI.Label(portalRect, $"PORTAL → {pole.PortalTargetId}", PortalLabelStyle);
                        }

                        GUI.backgroundColor = prevColor;
                        EditorGUILayout.Space(2f);

                        EditorGUI.BeginChangeCheck();
                        bool isLockedNew = EditorGUILayout.Toggle("Kilitli", pole.IsLocked, GUILayout.Width(poleWidth));
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(levelSO, "Kilitli Direk Değiştir");
                            pole.IsLocked = isLockedNew;
                            EditorUtility.SetDirty(levelSO);
                        }

                        EditorGUI.BeginChangeCheck();
                        int capNew = EditorGUILayout.IntField("Kapasite", pole.RingCapacity, GUILayout.Width(poleWidth));
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(levelSO, "Direk Kapasitesi Değiştir");
                            pole.RingCapacity = Mathf.Clamp(capNew, 2, BoardState.MaxSupportedCapacity);
                            while (pole.Rings.Count > pole.RingCapacity)
                                pole.Rings.RemoveAt(pole.Rings.Count - 1);
                            EditorUtility.SetDirty(levelSO);
                        }

                        EditorGUILayout.Space(2f);
                        if (GUILayout.Button("Temizle", GUILayout.Width(poleWidth), GUILayout.Height(18)))
                        {
                            Undo.RecordObject(levelSO, "Direği Temizle");
                            pole.Rings.Clear();
                            EditorUtility.SetDirty(levelSO);
                        }
                    }

                    if (p < levelData.Poles.Count - 1)
                        GUILayout.Space(poleGap);
                }
                GUI.backgroundColor = prevColor;
            }
            RingFlowEditorUtils.EndSectionBox();
        }

        private static int ComputeValidationHash(LevelData data)
        {
            if (data == null || data.Poles == null) return 0;
            int hash = data.Poles.Count;
            for (int p = 0; p < data.Poles.Count; p++)
            {
                var pole = data.Poles[p];
                hash = unchecked(hash * 31 + (pole.IsLocked ? 1 : 0));
                hash = unchecked(hash * 31 + pole.PortalTargetId);
                hash = unchecked(hash * 31 + pole.RingCapacity);
                if (pole.Rings != null)
                {
                    hash = unchecked(hash * 31 + pole.Rings.Count);
                    for (int r = 0; r < pole.Rings.Count && r < 8; r++)
                    {
                        var ring = pole.Rings[r];
                        hash = unchecked(hash * 31 + (int)ring.Color);
                        hash = unchecked(hash * 31 + (int)ring.Type);
                        hash = unchecked(hash * 31 + ring.AdditionalData);
                    }
                }
            }
            return hash;
        }

        private static string[] BuildWarnings(LevelData data)
        {
            var warnings = new List<string>();
            if (data.Poles == null || data.Poles.Count == 0)
            {
                warnings.Add("• Seviyede henüz hiç direk bulunmuyor.");
                return warnings.Count > 0 ? warnings.ToArray() : System.Array.Empty<string>();
            }

            bool hasEmptyPole = false;
            for (int p = 0; p < data.Poles.Count; p++)
            {
                if (data.Poles[p].Rings == null || data.Poles[p].Rings.Count == 0)
                { hasEmptyPole = true; break; }
            }
            if (!hasEmptyPole)
                warnings.Add("• GDD uyarınca oyunu tamamlamak için en az 1 boş direk gereklidir.");

            bool hasLockedPole = false;
            for (int p = 0; p < data.Poles.Count; p++)
            {
                if (data.Poles[p].IsLocked) { hasLockedPole = true; break; }
            }
            if (hasLockedPole)
            {
                bool hasKey = false;
                for (int p = 0; p < data.Poles.Count && !hasKey; p++)
                {
                    var pole = data.Poles[p];
                    if (pole.Rings != null)
                    {
                        for (int r = 0; r < pole.Rings.Count; r++)
                        {
                            if (pole.Rings[r].Type == RingType.Locked || pole.Rings[r].Type == RingType.Key)
                            { hasKey = true; break; }
                        }
                    }
                }
                if (!hasKey)
                    warnings.Add("• Seviyede kilitli direk var ancak kilidi açmak için gereken Altın Anahtar Halka (Locked/Key) yerleştirilmemiş.");
            }

            for (int p = 0; p < data.Poles.Count; p++)
            {
                var pole = data.Poles[p];
                if (pole.IsLocked && pole.Rings != null)
                {
                    for (int r = 0; r < pole.Rings.Count; r++)
                    {
                        if (pole.Rings[r].Type == RingType.Stone || pole.Rings[r].Type == RingType.Bomb)
                        {
                            warnings.Add($"• Direk {p} kilitli olmasına rağmen içinde Taş veya Bomba halkası var. Bu durum kilit açılmadan hamleyi bloke edeceği için uyumsuzdur.");
                            break;
                        }
                    }
                }
            }

            int uniqueCount = 0;
            int[] seenTypes = new int[16];
            int seenIndex = 0;
            for (int p = 0; p < data.Poles.Count; p++)
            {
                if (data.Poles[p].IsLocked)
                {
                    int lt = (int)RingType.Locked;
                    bool found = false;
                    for (int i = 0; i < seenIndex; i++) { if (seenTypes[i] == lt) { found = true; break; } }
                    if (!found) { seenTypes[seenIndex++] = lt; uniqueCount++; }
                }
                if (data.Poles[p].Rings != null)
                {
                    for (int r = 0; r < data.Poles[p].Rings.Count; r++)
                    {
                        var t = data.Poles[p].Rings[r].Type;
                        if (t == RingType.Standard) continue;
                        int tv = (int)t;
                        bool found = false;
                        for (int i = 0; i < seenIndex; i++) { if (seenTypes[i] == tv) { found = true; break; } }
                        if (!found) { seenTypes[seenIndex++] = tv; uniqueCount++; }
                    }
                }
            }
            if (uniqueCount > 4)
                warnings.Add($"• GDD uyarınca bir seviyede en fazla 4 farklı özel mekanik bulunabilir. Mevcut seviyede {uniqueCount} farklı mekanik var.");

            var portalPoles = new List<int>();
            for (int p = 0; p < data.Poles.Count; p++)
                if (data.Poles[p].PortalTargetId >= 0) portalPoles.Add(p);

            if (portalPoles.Count > 0)
            {
                if (portalPoles.Count % 2 != 0)
                    warnings.Add($"• Tek sayıda ({portalPoles.Count}) portal direği var. Portal direkleri çiftler halinde olmalıdır.");
                for (int i = 0; i < portalPoles.Count; i++)
                {
                    int pid = portalPoles[i];
                    int partner = data.Poles[pid].PortalTargetId;
                    if (partner < 0 || partner >= data.Poles.Count)
                        warnings.Add($"• Direk {pid} portalPartnerId={partner} geçersiz (0-{data.Poles.Count - 1} olmalı).");
                    else if (data.Poles[partner].PortalTargetId != pid)
                        warnings.Add($"• Direk {pid} ↔ {partner} portal çifti karşılıklı değil (Direk {partner}'in PortalTargetId={data.Poles[partner].PortalTargetId}, beklenen={pid}).");
                }
            }

            return warnings.Count > 0 ? warnings.ToArray() : System.Array.Empty<string>();
        }

        private static void DrawHeader(string title)
        {
            GUILayout.Box(title, RingFlowEditorUtils.HeaderStyle, GUILayout.ExpandWidth(true), GUILayout.Height(24));
            EditorGUILayout.Space(2f);
        }
    }
}

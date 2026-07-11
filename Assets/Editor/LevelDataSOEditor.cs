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

        private static RingColor s_brushColor = RingColor.Red;
        private static RingType s_brushType = RingType.Standard;
        private static bool s_eraserMode;
        private static int s_bombCounter = 3;

        private static GUIStyle s_compactButtonStyle;
        private static GUIStyle s_boldButtonStyle;
        private static GUIStyle s_headerStyle;

        private static GUIStyle CompactButton => s_compactButtonStyle ??= new GUIStyle(GUI.skin.button)
            { fontSize = 9, fontStyle = FontStyle.Normal };

        private static GUIStyle BoldCompactButton => s_boldButtonStyle ??= new GUIStyle(GUI.skin.button)
            { fontSize = 9, fontStyle = FontStyle.Bold };

        private static GUIStyle HeaderStyle
        {
            get
            {
                if (s_headerStyle == null)
                {
                    s_headerStyle = new GUIStyle(GUI.skin.box)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 11,
                        fontStyle = FontStyle.Bold,
                        normal = { textColor = new Color(0.2f, 0.8f, 1.0f) }
                    };
                }
                return s_headerStyle;
            }
        }

        public override void OnInspectorGUI()
        {
            var levelSO = (LevelDataSO)target;
            if (levelSO == null || levelSO.Data == null)
            {
                base.OnInspectorGUI();
                return;
            }

            serializedObject.Update();

            DrawHeader($"SEVİYE {levelSO.Data.LevelIndex} YAPILANDIRMASI");

            // ── GDD Uyum & Doğrulama Kontrolleri ──
            var warnings = new List<string>();
            if (levelSO.Data.Poles == null || levelSO.Data.Poles.Count == 0)
            {
                warnings.Add("• Seviyede henüz hiç direk bulunmuyor.");
            }
            else
            {
                // 1. En az bir boş direk kontrolü
                bool hasEmptyPole = false;
                for (int p = 0; p < levelSO.Data.Poles.Count; p++)
                {
                    if (levelSO.Data.Poles[p].Rings == null || levelSO.Data.Poles[p].Rings.Count == 0)
                    {
                        hasEmptyPole = true;
                        break;
                    }
                }
                if (!hasEmptyPole)
                {
                    warnings.Add("• GDD uyarınca oyunu tamamlamak için en az 1 boş direk gereklidir.");
                }

                // 2. Kilitli direk ve anahtar kontrolü
                bool hasLockedPole = false;
                for (int p = 0; p < levelSO.Data.Poles.Count; p++)
                {
                    if (levelSO.Data.Poles[p].IsLocked)
                    {
                        hasLockedPole = true;
                        break;
                    }
                }
                if (hasLockedPole)
                {
                    bool hasKey = false;
                    for (int p = 0; p < levelSO.Data.Poles.Count; p++)
                    {
                        var pole = levelSO.Data.Poles[p];
                        if (pole.Rings != null)
                        {
                            for (int r = 0; r < pole.Rings.Count; r++)
                            {
                                if (pole.Rings[r].Type == RingType.Locked || pole.Rings[r].Type == RingType.Key)
                                {
                                    hasKey = true;
                                    break;
                                }
                            }
                        }
                        if (hasKey) break;
                    }
                    if (!hasKey)
                    {
                        warnings.Add("• Seviyede kilitli direk var ancak kilidi açmak için gereken Altın Anahtar Halka (Locked/Key) yerleştirilmemiş.");
                    }
                }

                // 3. Kilitli direk içinde Taş (Stone) veya Bomba (Bomb) kontrolü
                for (int p = 0; p < levelSO.Data.Poles.Count; p++)
                {
                    var pole = levelSO.Data.Poles[p];
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

                // 4. Benzersiz mekanik limiti (Maks. 4)
                var uniqueMechanics = new HashSet<RingType>();
                for (int p = 0; p < levelSO.Data.Poles.Count; p++)
                {
                    if (levelSO.Data.Poles[p].IsLocked) uniqueMechanics.Add(RingType.Locked);
                    if (levelSO.Data.Poles[p].Rings != null)
                    {
                        for (int r = 0; r < levelSO.Data.Poles[p].Rings.Count; r++)
                        {
                            var t = levelSO.Data.Poles[p].Rings[r].Type;
                            if (t != RingType.Standard) uniqueMechanics.Add(t);
                        }
                    }
                }
                if (uniqueMechanics.Count > 4)
                {
                    warnings.Add($"• GDD uyarınca bir seviyede en fazla 4 farklı özel mekanik bulunabilir. Mevcut seviyede {uniqueMechanics.Count} farklı mekanik var.");
                }
            }

            if (warnings.Count > 0)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var titleStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(0.9f, 0.3f, 0.3f) } };
                    EditorGUILayout.LabelField("⚠️ GDD UYUMLULUK UYARILARI:", titleStyle);
                    foreach (var w in warnings)
                    {
                        EditorGUILayout.LabelField(w, EditorStyles.wordWrappedMiniLabel);
                    }
                }
                EditorGUILayout.Space(4f);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Seviye Ayarları", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();
                levelSO.Data.LevelIndex = EditorGUILayout.IntField("Seviye Endeksi", levelSO.Data.LevelIndex);
                levelSO.Data.Seed = EditorGUILayout.IntField("Rastgele Tohum (Seed)", levelSO.Data.Seed);
                levelSO.Data.TargetMoves = EditorGUILayout.IntField("Hedef Hamle", levelSO.Data.TargetMoves);

                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(levelSO);
            }

            DrawLevelSummary(levelSO.Data);
            DrawDifficultyAndMechanicPreview(levelSO.Data);

            _showTools = EditorGUILayout.Foldout(_showTools, "Seviye Düzenleme Araçları", true, EditorStyles.foldoutHeader);
            if (_showTools)
            {
                DrawColorPalette();
                DrawTypePalette();
                DrawLevelVisualInteractive(levelSO.Data, levelSO);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Seviye Tasarımcı Araçları", EditorStyles.boldLabel);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Seviyeyi Doğrula & Çöz", GUILayout.Height(30)))
                            VerifyAndSolve(levelSO);

                        if (GUILayout.Button("Seviyeyi Yeniden Karıştır", GUILayout.Height(30)))
                            ReScramble(levelSO);
                    }

                    EditorGUILayout.Space(2f);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Boş Direk Ekle", GUILayout.Height(24)))
                        {
                            int ringCapacity = levelSO.Data.Poles.Count > 0 
                                ? levelSO.Data.Poles[0].RingCapacity 
                                : GameConfigDatabaseSO.Instance.GetMaxCapacityForLevel(levelSO.Data.LevelIndex);
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
                }
            }

            EditorGUILayout.Space(5f);
            _showRawData = EditorGUILayout.Foldout(_showRawData, "Ham Serileştirilmiş Veri", true);
            if (_showRawData)
                DrawPropertiesExcluding(serializedObject, "m_Script");

            if (serializedObject.ApplyModifiedProperties() || GUI.changed)
                EditorUtility.SetDirty(levelSO);
        }

        private void VerifyAndSolve(LevelDataSO levelSO)
        {
            var board = new BoardState { PoleCount = levelSO.Data.Poles.Count };
            for (int p = 0; p < levelSO.Data.Poles.Count; p++)
            {
                var pole = levelSO.Data.Poles[p];
                for (int r = 0; r < pole.Rings.Count; r++)
                    board.AddRing(p, pole.Rings[r]);
            }

            int maxCapacity = levelSO.Data.Poles.Count > 0 
                ? levelSO.Data.Poles[0].RingCapacity 
                : GameConfigDatabaseSO.Instance.GetMaxCapacityForLevel(levelSO.Data.LevelIndex);
            var database = GameConfigDatabaseSO.Instance;
            int levelIndex = levelSO.Data.LevelIndex;
            var band = database.GetBandForLevel(levelIndex);
            var allowedMechanics = database.GetAllowedMechanicsForLevel(levelIndex);
            int intensity = database.GetMechanicIntensityForLevel(levelIndex);

            var solveResult = LevelSolver.Solve(board, maxCapacity);

            if (solveResult.IsSolvable)
            {
                Undo.RecordObject(levelSO, "Hedef Hamleleri Güncelle");
                levelSO.Data.TargetMoves = solveResult.MoveCount;
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

            var database = GameConfigDatabaseSO.Instance;
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
                    levelSO.Data.LevelIndex, levelSO.Data.Seed,
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

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Seviye Özeti", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Direk Sayısı", poleCount.ToString());
                EditorGUILayout.LabelField("Toplam Halka", totalRings.ToString());
                EditorGUILayout.LabelField("Maks. Kapasite", maxCapacity.ToString());
            }
        }

        private static void DrawDifficultyAndMechanicPreview(LevelData levelData)
        {
            if (levelData == null)
            {
                return;
            }

            var database = GameConfigDatabaseSO.Instance;
            int levelIndex = levelData.LevelIndex;
            var band = database.GetBandForLevel(levelIndex);
            var allowed = database.GetAllowedMechanicsForLevel(levelIndex);
            int intensity = database.GetMechanicIntensityForLevel(levelIndex);
            int minEmptyPoles = database.GetMinEmptyPolesForLevel(levelIndex);
            int poleCount = database.GetPoleCountForLevel(levelIndex);
            int colorCount = database.GetColorCountForLevel(levelIndex);
            int maxCapacity = database.GetMaxCapacityForLevel(levelIndex);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Zorluk ve Mekanik Önizleme", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Band", band.ToString());
                EditorGUILayout.LabelField("Renk Sayısı", colorCount.ToString());
                EditorGUILayout.LabelField("Direk Sayısı", poleCount.ToString());
                EditorGUILayout.LabelField("Kapasite", maxCapacity.ToString());
                EditorGUILayout.LabelField("Direk Kapasite Etiketi", levelData.Poles.Count > 0 ? levelData.Poles[0].CapacityText : "Yok");
                EditorGUILayout.LabelField("Boş Direk", minEmptyPoles.ToString());
                EditorGUILayout.LabelField("Mekanik Yoğunluğu", intensity.ToString());
                EditorGUILayout.LabelField("Açık Mekanikler", string.Join(", ", allowed));
            }
        }

        private static void DrawColorPalette()
        {
            EditorGUILayout.LabelField("Renk Fırçası Seçin:", EditorStyles.boldLabel);

            var colors = (RingColor[])System.Enum.GetValues(typeof(RingColor));
            var prevColor = GUI.backgroundColor;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUI.backgroundColor = s_eraserMode ? Color.red : Color.gray;
                var eraserStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = s_eraserMode ? FontStyle.Bold : FontStyle.Normal,
                    normal = { textColor = s_eraserMode ? Color.white : Color.black }
                };
                if (GUILayout.Button("SİLGİ", eraserStyle, GUILayout.Width(70), GUILayout.Height(24)))
                    s_eraserMode = true;
                GUI.backgroundColor = prevColor;

                GUILayout.Space(8f);

                for (int i = 1; i < colors.Length; i++)
                {
                    var color = colors[i];
                    Color c = RingPalette.Get(color);

                    bool isSelected = (!s_eraserMode && s_brushColor == color);
                    GUI.backgroundColor = c;

                    string label = isSelected
                        ? $"[{color.ToString().Substring(0, 3).ToUpper()}]"
                        : color.ToString().Substring(0, 3).ToUpper();

                    var style = new GUIStyle(CompactButton)
                    {
                        fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
                        normal = { textColor = RingFlowEditorUtils.GetContrastColor(c) }
                    };

                    if (GUILayout.Button(label, style, GUILayout.Width(42), GUILayout.Height(24)))
                    {
                        s_brushColor = color;
                        s_eraserMode = false;
                    }
                }
                GUI.backgroundColor = prevColor;
            }
        }

        private static void DrawTypePalette()
        {
            if (s_eraserMode) return;

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Halka Tipi Fırçası Seçin:", EditorStyles.boldLabel);

            var types = (RingType[])System.Enum.GetValues(typeof(RingType));
            var prevBg = GUI.backgroundColor;
            int typesPerRow = 5;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
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
                                GUI.backgroundColor = new Color(0.2f, 0.8f, 1.0f);
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
            }
        }

        private void DrawLevelVisualInteractive(LevelData levelData, LevelDataSO levelSO)
        {
            if (levelData == null || levelData.Poles == null || levelData.Poles.Count == 0)
            {
                EditorGUILayout.HelpBox("Gösterilecek seviye verisi yok.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Görsel Seviye Tasarımcısı (Düzenlemek için Tıklayın):", EditorStyles.boldLabel);

            float poleWidth = 70f;
            float ringHeight = 20f;
            float poleGap = 8f;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
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
                                Color ringColor = RingPalette.Get(ring.Color);
                                GUI.backgroundColor = ringColor;

                                string label = RingFlowEditorUtils.GetRingShortLabel(ring.Type);
                                if (ring.AdditionalData > 0 && ring.Type == RingType.Bomb)
                                    label += ring.AdditionalData;

                                var style = new GUIStyle(BoldCompactButton)
                                {
                                    normal = { textColor = RingFlowEditorUtils.GetContrastColor(ringColor) }
                                };

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
                                var style = new GUIStyle(BoldCompactButton)
                                {
                                    fontSize = 11,
                                    normal = { textColor = Color.white }
                                };

                                if (!s_eraserMode && GUI.Button(ringRect, "+", style))
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
                            var lockStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                                { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
                            GUI.Label(lockRect, "KİLİTLİ", lockStyle);
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
                            pole.RingCapacity = Mathf.Clamp(capNew, 2, 8);
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
            EditorGUILayout.Space(5f);
        }

        private static void DrawHeader(string title)
        {
            var bg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.12f, 0.12f, 0.15f);
            GUILayout.Box(title, HeaderStyle, GUILayout.ExpandWidth(true), GUILayout.Height(24));
            GUI.backgroundColor = bg;
            EditorGUILayout.Space(2f);
        }
    }
}

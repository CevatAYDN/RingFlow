using UnityEditor;
using UnityEngine;
using Nexus.Core.Services;
using RingFlow.Gameplay;

namespace RingFlow.Editor
{
    /// <summary>
    /// Seviye Tarayıcı — üretilmiş Level_N.asset dosyalarını listeleyen ve
    /// doğrudan açan bölüm. Amaç: "55. leveli seç ve düzenle" akışını
    /// dashboard'dan ulaşılabilir kılmak. Düzenleme sorumluluğu var olan
    /// LevelDataSOEditor'a (Selection üzerinden) devredilir; bu bölüm yalnızca
    /// gezinme ve açma işini yapar.
    /// </summary>
    public sealed class LevelBrowserSection : EditorSection
    {
        private int _jumpToLevel = 1;
        private string _searchFilter = string.Empty;
        private Vector2 _scroll;
        private const float GridButtonWidth = 46f;
        private const float GridButtonHeight = 26f;
        private const float RowSpacing = 2f;

        private GameConfigDatabaseSO _cachedDatabase;
        private int _cachedTotalLevels = -1;
        private bool[] _cachedExistsFlags;
        private float _cachedAvailableWidth;
        private int _cachedCols;
        private bool _cachedNarrow;

        private static GUIStyle s_levelButtonStyle;
        private static GUIStyle LevelButtonStyle
        {
            get
            {
                if (s_levelButtonStyle == null)
                {
                    s_levelButtonStyle = new GUIStyle(GUI.skin.button)
                    {
                        fontSize = 10,
                        fontStyle = FontStyle.Bold
                    };
                }
                return s_levelButtonStyle;
            }
        }

        public override string DisplayName => "Seviye Tarayıcı (Level Browser)";
        public override string PrefKey => EditorPrefsKeys.FoldLevelBrowser;

        public override void OnGUI()
        {
            DrawFoldoutHeader();
            if (!IsFoldedOut) return;

            EnsureCachedDatabase();
            if (_cachedDatabase == null)
            {
                EditorGUILayout.HelpBox("Zorluk Veritabanı (GameConfigDatabase.asset) bulunamadı!", MessageType.Error);
                return;
            }

            if (_cachedTotalLevels <= 0)
            {
                EditorGUILayout.HelpBox("Zorluk Veritabanında geçerli seviye sayısı yok (TotalLevels ≤ 0).", MessageType.Warning);
                return;
            }

            if (Event.current.type == EventType.Layout)
                _cachedNarrow = RingFlowEditorUtils.IsNarrowWidth(620f);

            bool narrow = _cachedNarrow;

            RingFlowEditorUtils.BeginSectionBox("Seviye Arama & Hızlı Atlama", "Seviye numarasına göre hızlı arama ve doğrudan düzenleme yapın.");

            if (narrow)
            {
                _searchFilter = EditorGUILayout.TextField("Ara", _searchFilter);
                _jumpToLevel = Mathf.Clamp(EditorGUILayout.IntField("Seviye #", _jumpToLevel), 1, _cachedTotalLevels);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Aç") && IsUserEvent())
                        OpenLevel(_jumpToLevel);
                    if (GUILayout.Button("Sonraki") && IsUserEvent())
                    {
                        _jumpToLevel = Mathf.Clamp(_jumpToLevel + 1, 1, _cachedTotalLevels);
                        OpenLevel(_jumpToLevel);
                    }
                    if (GUILayout.Button("▶ Oyna") && IsUserEvent())
                        EditorPlayFromLevel.Play(_jumpToLevel);
                }
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    _searchFilter = EditorGUILayout.TextField("Ara", _searchFilter, GUILayout.Width(RingFlowEditorUtils.GetResponsiveWidth(180f, 300f, 0.28f)));
                    if (EditorGUI.EndChangeCheck())
                        _searchFilter = _searchFilter ?? string.Empty;
                    GUILayout.FlexibleSpace();
                    _jumpToLevel = Mathf.Clamp(
                        EditorGUILayout.IntField("Seviye #", _jumpToLevel, GUILayout.Width(160f)),
                        1, _cachedTotalLevels);
                    if (GUILayout.Button("Aç", GUILayout.Width(80f)))
                    {
                        if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp || Event.current.type == EventType.Used)
                            OpenLevel(_jumpToLevel);
                    }
                    if (GUILayout.Button("Sonraki", GUILayout.Width(80f)))
                    {
                        if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp || Event.current.type == EventType.Used)
                        {
                            _jumpToLevel = Mathf.Clamp(_jumpToLevel + 1, 1, _cachedTotalLevels);
                            OpenLevel(_jumpToLevel);
                        }
                    }
                    if (GUILayout.Button("▶ Oyna", GUILayout.Width(80f)))
                    {
                        if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseUp || Event.current.type == EventType.Used)
                            EditorPlayFromLevel.Play(_jumpToLevel);
                    }
                }
            }

            RingFlowEditorUtils.EndSectionBox();

            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionSpacing);

            int filtered = ComputeFilteredTill();
            EditorGUILayout.LabelField(
                $"Üretilmiş Seviyeler (1 - {_cachedTotalLevels}) — {filtered} eşleşme" +
                (!string.IsNullOrEmpty(_searchFilter) ? $" (filtre: {_searchFilter})" : string.Empty),
                EditorStyles.boldLabel);

            DrawFilteredGrid();
        }

        private void EnsureCachedDatabase()
        {
            if (_cachedDatabase == null)
                _cachedDatabase = new RingFlow.Gameplay.Services.ResourcesAssetService()
                    .LoadAsync<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey)
                    .GetAwaiter().GetResult();
            if (_cachedDatabase == null) return;

            if (_cachedDatabase.TotalLevels != _cachedTotalLevels)
            {
                _cachedTotalLevels = _cachedDatabase.TotalLevels;
                // Invalidate existence cache whenever TotalLevels changes (DB edited).
                _cachedExistsFlags = null;
            }
        }

        /// <summary>
        /// Forces the file-existence cache to refresh on the next OnGUI call.
        /// Call this after batch generation or any operation that creates/deletes level assets.
        /// </summary>
        public void InvalidateExistenceCache()
        {
            _cachedExistsFlags = null;
        }

        private static bool IsUserEvent()
        {
            return Event.current.type == EventType.MouseDown
                || Event.current.type == EventType.MouseUp
                || Event.current.type == EventType.Used;
        }

        private void RefreshExistenceCache()
        {
            if (_cachedExistsFlags != null && _cachedExistsFlags.Length == _cachedTotalLevels)
                return;
            _cachedExistsFlags = new bool[_cachedTotalLevels];

            // BUG FIX: AssetDatabase.GUIDFromAssetPath returns "00000000000000000000000000000000"
            // (all-zero GUID) for paths that do NOT exist — NOT an empty string.
            // Calling !string.IsNullOrEmpty(guid.ToString()) on this always returns true,
            // making every level appear as "generated" even when no .asset file exists.
            //
            // Correct check: AssetDatabase.AssetPathToGUID returns "" for missing paths
            // (legacy API), OR use System.IO.File.Exists for a direct filesystem check.
            // We use File.Exists because it is the most reliable: it checks the actual
            // .asset file on disk, independent of AssetDatabase refresh state.
            for (int i = 0; i < _cachedTotalLevels; i++)
            {
                string assetPath = $"{EditorPaths.LevelsFolder}/Level_{i + 1}.asset";
                // Convert from project-relative (Assets/...) to full filesystem path.
                string fullPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath),
                    assetPath.Replace('/', System.IO.Path.DirectorySeparatorChar));
                _cachedExistsFlags[i] = System.IO.File.Exists(fullPath);
            }
        }

        private int ComputeFilteredTill()
        {
            RefreshExistenceCache();

            int count = 0;
            bool hasFilter = !string.IsNullOrEmpty(_searchFilter);
            for (int i = 0; i < _cachedTotalLevels; i++)
            {
                if (hasFilter && !(i + 1).ToString().Contains(_searchFilter))
                    continue;
                count++;
            }
            return count;
        }

        private void DrawFilteredGrid()
        {
            const float visibleHeight = 220f;

            if (Event.current.type == EventType.Layout)
            {
                _cachedAvailableWidth = EditorGUIUtility.currentViewWidth;
                if (_cachedAvailableWidth >= 980f)
                {
                    float sidebarWidth = Mathf.Clamp(_cachedAvailableWidth * 0.22f, 190f, 260f);
                    _cachedAvailableWidth -= (sidebarWidth + 40f);
                }
                else
                {
                    _cachedAvailableWidth -= 32f;
                }

                _cachedCols = RingFlowEditorUtils.GetResponsiveColumns(GridButtonWidth + 6f, 2, 12, _cachedAvailableWidth);
            }

            float availableWidth = _cachedAvailableWidth;
            int cols = Mathf.Max(1, _cachedCols);
            float gridButtonWidth = Mathf.Clamp((availableWidth - 24f) / cols, 32f, 54f);

            RingFlowEditorUtils.BeginSectionBox("Seviye Grid Görünümü", "Seviye durumlarını yeşil (üretilmiş) ve gri (boş) olarak gösterir.");

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(visibleHeight));

            bool hasFilter = !string.IsNullOrEmpty(_searchFilter);
            int filteredCount = ComputeFilteredTill();
            
            // Calculate absolute layout height
            int rows = (int)System.Math.Ceiling((double)filteredCount / cols);
            float rowHeight = GridButtonHeight + RowSpacing;
            float totalHeight = rows * rowHeight;

            // Reserve single layout rect
            Rect gridRect = GUILayoutUtility.GetRect(availableWidth, totalHeight);

            int drawIndex = 0;
            for (int level = 1; level <= _cachedTotalLevels; level++)
            {
                if (hasFilter && !level.ToString().Contains(_searchFilter))
                    continue;

                int r = drawIndex / cols;
                int c = drawIndex % cols;

                Rect buttonRect = new Rect(
                    gridRect.x + c * (gridButtonWidth + 4f),
                    gridRect.y + r * rowHeight,
                    gridButtonWidth,
                    GridButtonHeight
                );

                DrawLevelButton(level, buttonRect);
                drawIndex++;
            }

            EditorGUILayout.EndScrollView();

            RingFlowEditorUtils.EndSectionBox();

            EditorGUILayout.HelpBox(
                "Yeşil düğmeler üretilmiş seviyeleri gösterir. Bir seviyeye tıklayınca Inspector'da " +
                "LevelDataSOEditor (etkileşimli tahta düzenleyici) açılır; oradan halka/direk düzenleyip kaydedebilirsiniz.",
                MessageType.Info);
        }

        private void DrawLevelButton(int level, Rect rect)
        {
            if (_cachedExistsFlags == null || level - 1 < 0 || level - 1 >= _cachedExistsFlags.Length)
                return;

            bool exists = _cachedExistsFlags[level - 1];
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = exists ? EditorPaths.EditorColors.Success : EditorPaths.EditorColors.PanelSlate;

            LevelButtonStyle.normal.textColor = exists ? Color.white : EditorPaths.EditorColors.MutedText;

            var content = new GUIContent(level.ToString(), "Sol Tık: Seviyeyi Seç ve Düzenle\nSağ Tık: Oyunu Başlat ve Doğrudan Oyna");

            // Handle right-click BEFORE GUI.Button draws
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && rect.Contains(Event.current.mousePosition))
            {
                _jumpToLevel = level;
                OpenLevel(level);
                EditorPlayFromLevel.Play(level);
                Event.current.Use();
            }
            else if (GUI.Button(rect, content, LevelButtonStyle))
            {
                _jumpToLevel = level;
                OpenLevel(level);
            }

            GUI.backgroundColor = prev;
        }

        private void OpenLevel(int level)
        {
            string path = $"{EditorPaths.LevelsFolder}/Level_{level}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<LevelDataSO>(path);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
                NexusLog.Info("LevelBrowserSection", nameof(OpenLevel), level.ToString(),
                    $"[Editor] Level {level} açıldı: {path}");
            }
            else
            {
                EditorUtility.DisplayDialog("Seviye Bulunamadı",
                    $"Seviye {level} henüz üretilmemiş.\n" +
                    $"Seviye Üretici sekmesinden tek veya toplu üretim yapabilirsiniz.\n" +
                    $"(Batch aralığını {level}–{level} olarak ayarlayıp 'Toplu Üret'e tıklayın.)",
                    "Tamam");
            }
        }
    }
}

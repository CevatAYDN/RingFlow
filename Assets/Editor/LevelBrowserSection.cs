using UnityEditor;
using UnityEngine;
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

            bool narrow = RingFlowEditorUtils.IsNarrowWidth(620f);

            RingFlowEditorUtils.BeginSectionBox("Seviye Arama & Hızlı Atlama", "Seviye numarasına göre hızlı arama ve doğrudan düzenleme yapın.");

            if (narrow)
            {
                _searchFilter = EditorGUILayout.TextField("Ara", _searchFilter);
                _jumpToLevel = Mathf.Clamp(EditorGUILayout.IntField("Seviye #", _jumpToLevel), 1, _cachedTotalLevels);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Aç"))
                        OpenLevel(_jumpToLevel);
                    if (GUILayout.Button("Sonraki"))
                    {
                        _jumpToLevel = Mathf.Clamp(_jumpToLevel + 1, 1, _cachedTotalLevels);
                        OpenLevel(_jumpToLevel);
                    }
                    if (GUILayout.Button("▶ Oyna"))
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
                        OpenLevel(_jumpToLevel);
                    if (GUILayout.Button("Sonraki", GUILayout.Width(80f)))
                    {
                        _jumpToLevel = Mathf.Clamp(_jumpToLevel + 1, 1, _cachedTotalLevels);
                        OpenLevel(_jumpToLevel);
                    }
                    if (GUILayout.Button("▶ Oyna", GUILayout.Width(80f)))
                        EditorPlayFromLevel.Play(_jumpToLevel);
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
                _cachedDatabase = Resources.Load<GameConfigDatabaseSO>(EditorPaths.GameConfigDatabaseKey);
            if (_cachedDatabase == null) return;

            if (_cachedDatabase.TotalLevels != _cachedTotalLevels)
            {
                _cachedTotalLevels = _cachedDatabase.TotalLevels;
                _cachedExistsFlags = null;
            }
        }

        private void RefreshExistenceCache()
        {
            if (_cachedExistsFlags != null && _cachedExistsFlags.Length == _cachedTotalLevels)
                return;
            _cachedExistsFlags = new bool[_cachedTotalLevels];

            // Use AssetDatabase-level existence check (metadata only, no deserialization)
            for (int i = 0; i < _cachedTotalLevels; i++)
            {
                string path = $"{EditorPaths.LevelsFolder}/Level_{i + 1}.asset";
                var guid = AssetDatabase.GUIDFromAssetPath(path);
                _cachedExistsFlags[i] = guid != null && !string.IsNullOrEmpty(guid.ToString());
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

            float availableWidth = EditorGUIUtility.currentViewWidth;
            if (availableWidth >= 980f)
            {
                float sidebarWidth = Mathf.Clamp(availableWidth * 0.22f, 190f, 260f);
                availableWidth -= (sidebarWidth + 40f);
            }
            else
            {
                availableWidth -= 32f;
            }

            int cols = RingFlowEditorUtils.GetResponsiveColumns(GridButtonWidth + 6f, 2, 12, availableWidth);
            float gridButtonWidth = Mathf.Clamp((availableWidth - 24f) / cols, 32f, 54f);

            RingFlowEditorUtils.BeginSectionBox("Seviye Grid Görünümü", "Seviye durumlarını yeşil (üretilmiş) ve gri (boş) olarak gösterir.");

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(visibleHeight));
            int row = 0;
            bool hasFilter = !string.IsNullOrEmpty(_searchFilter);
            for (int level = 1; level <= _cachedTotalLevels; level++)
            {
                if (hasFilter && !level.ToString().Contains(_searchFilter))
                    continue;

                int col = (row % cols);
                if (col == 0)
                    EditorGUILayout.BeginHorizontal();
                DrawLevelButton(level, gridButtonWidth);
                if (col == cols - 1 || level == _cachedTotalLevels)
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
                row++;
            }
            EditorGUILayout.EndScrollView();

            RingFlowEditorUtils.EndSectionBox();

            EditorGUILayout.HelpBox(
                "Yeşil düğmeler üretilmiş seviyeleri gösterir. Bir seviyeye tıklayınca Inspector'da " +
                "LevelDataSOEditor (etkileşimli tahta düzenleyici) açılır; oradan halka/direk düzenleyip kaydedebilirsiniz.",
                MessageType.Info);
        }

        private void DrawLevelButton(int level, float width)
        {
            if (_cachedExistsFlags == null || level - 1 < 0 || level - 1 >= _cachedExistsFlags.Length)
                return;

            bool exists = _cachedExistsFlags[level - 1];
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = exists ? EditorPaths.EditorColors.Success : EditorPaths.EditorColors.PanelSlate;

            LevelButtonStyle.normal.textColor = exists ? Color.white : EditorPaths.EditorColors.MutedText;

            if (GUILayout.Button(level.ToString(), LevelButtonStyle, GUILayout.Width(width), GUILayout.Height(GridButtonHeight)))
                OpenLevel(level);

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
            }
            else
            {
                EditorUtility.DisplayDialog("Seviye Bulunamadı",
                    $"Seviye {level} henüz üretilmemiş.\nÖnce 'Seviye Üretici' sekmesinden üretin " +
                    "(Başlangıç/Bitiş aralığına " + level + " değerini ekleyin).", "Tamam");
            }
        }
    }
}

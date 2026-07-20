using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using RingFlow.Gameplay.Localization;

namespace RingFlow.Editor
{
    [CustomEditor(typeof(LocalizationConfigSO))]
    public sealed class LocalizationConfigSOEditor : UnityEditor.Editor
    {
        private bool _showCsvEditor;
        private bool _cachedShowCsvEditor;
        private string _csvFilter = "";
        private Vector2 _csvScroll;
        private int _selectedRowIndex = -1;

        private struct CSVRow
        {
            public string Key;
            public string[] Translations;
        }

        private readonly List<string> _csvLanguages = new();
        private readonly List<CSVRow> _csvRows = new();

        public override void OnInspectorGUI()
        {
            if (Event.current.type == EventType.Layout)
            {
                _cachedShowCsvEditor = _showCsvEditor;
            }

            var config = (LocalizationConfigSO)target;

            EditorGUILayout.LabelField("Yerelleştirme Yapılandırması (Localization Config)", RingFlowEditorUtils.HeaderStyle);
            EditorGUILayout.Space(4f);

            // ── CSV Link ──
            RingFlowEditorUtils.BeginSectionBox("Yerelleştirme Tablosu (CSV)", "Oyun içi metinlerin dil anahtarlarını barındıran Localization.csv dosyasının fiziksel durumu.");
            string csvPath = "Assets/Resources/Localization.csv";
            bool csvExists = File.Exists(csvPath);

            if (csvExists)
            {
                EditorGUILayout.HelpBox($"Localization.csv dosyası mevcut: {csvPath}", MessageType.Info);
                if (GUILayout.Button("Localization.csv Dosyasını Harici Aç", GUILayout.Height(22)))
                {
                    var csvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(csvPath);
                    if (csvAsset != null)
                    {
                        AssetDatabase.OpenAsset(csvAsset);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"Kritik: Localization.csv dosyası eksik! Yol: {csvPath}", MessageType.Error);
            }
            RingFlowEditorUtils.EndSectionBox();

            EditorGUILayout.Space(8f);

            // ── Language Entries ──
            serializedObject.Update();
            var langsProp = serializedObject.FindProperty("Languages");

            if (langsProp != null && langsProp.isArray)
            {
                RingFlowEditorUtils.BeginSectionBox("Desteklenen Diller (Supported Languages)", $"Yerelleştirme motorunda kayıtlı aktif diller ({langsProp.arraySize})");

                for (int i = 0; i < langsProp.arraySize; i++)
                {
                    var lang = langsProp.GetArrayElementAtIndex(i);
                    var codeProp = lang.FindPropertyRelative("Code");
                    var nameProp = lang.FindPropertyRelative("DisplayName");
                    var rtlProp = lang.FindPropertyRelative("IsRTL");

                    Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(22f));
                    Color rowBg = i % 2 == 0 ? new Color(0.2f, 0.22f, 0.25f, 0.3f) : new Color(0.15f, 0.17f, 0.2f, 0.3f);
                    EditorGUI.DrawRect(rowRect, rowBg);

                    EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(20f));
                    codeProp.stringValue = EditorGUILayout.TextField(codeProp.stringValue, GUILayout.Width(40f));
                    nameProp.stringValue = EditorGUILayout.TextField(nameProp.stringValue, GUILayout.MinWidth(120f));
                    rtlProp.boolValue = EditorGUILayout.ToggleLeft("Sağdan Sola (RTL)", rtlProp.boolValue, GUILayout.Width(130f));

                    if (GUILayout.Button("-", GUILayout.Width(24f)))
                    {
                        langsProp.DeleteArrayElementAtIndex(i);
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(2f);
                }

                if (GUILayout.Button("+ Yeni Dil Ekle", GUILayout.Height(24)))
                {
                    langsProp.InsertArrayElementAtIndex(langsProp.arraySize);
                    var newElem = langsProp.GetArrayElementAtIndex(langsProp.arraySize - 1);
                    newElem.FindPropertyRelative("Code").stringValue = "new";
                    newElem.FindPropertyRelative("DisplayName").stringValue = "Yeni Dil";
                    newElem.FindPropertyRelative("IsRTL").boolValue = false;
                }

                RingFlowEditorUtils.EndSectionBox();
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);

            // ── Yerelleştirme Tablosu Düzenleyici (CSV Editor) ──
            _showCsvEditor = EditorGUILayout.Foldout(_showCsvEditor, "Yerelleştirme Tablosu Düzenleyici (CSV Editor)", true, EditorStyles.foldoutHeader);
            if (_cachedShowCsvEditor)
            {
                if (_csvRows.Count == 0)
                {
                    LoadCSV();
                }

                // Arama filtresi
                using (new EditorGUILayout.HorizontalScope())
                {
                    _csvFilter = EditorGUILayout.TextField("Çeviri Ara (Anahtar / Değer)", _csvFilter);
                    if (!string.IsNullOrEmpty(_csvFilter) && GUILayout.Button("Temizle", GUILayout.Width(60f)))
                    {
                        _csvFilter = string.Empty;
                        GUI.FocusControl(null);
                    }
                }

                // Tablo listesi
                RingFlowEditorUtils.BeginSectionBox("Kayıtlı Çeviriler", "Filtreleme kriterlerine göre CSV tablosundaki kayıtlar.");
                
                _csvScroll = EditorGUILayout.BeginScrollView(_csvScroll, GUILayout.Height(240f));

                for (int i = 0; i < _csvRows.Count; i++)
                {
                    var row = _csvRows[i];
                    if (!string.IsNullOrEmpty(_csvFilter))
                    {
                        bool match = row.Key.ToLower().Contains(_csvFilter.ToLower());
                        if (!match)
                        {
                            for (int j = 0; j < row.Translations.Length; j++)
                            {
                                if (row.Translations[j].ToLower().Contains(_csvFilter.ToLower()))
                                {
                                    match = true;
                                    break;
                                }
                            }
                        }
                        if (!match) continue;
                    }

                    Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(22f));
                    Color rowBg = i % 2 == 0 ? new Color(0.2f, 0.22f, 0.25f, 0.3f) : new Color(0.15f, 0.17f, 0.2f, 0.3f);
                    EditorGUI.DrawRect(rowRect, rowBg);

                    var style = new GUIStyle(EditorStyles.label);
                    if (_selectedRowIndex == i)
                    {
                        style.fontStyle = FontStyle.Bold;
                        style.normal.textColor = EditorPaths.EditorColors.Info;
                    }
                    EditorGUILayout.LabelField(row.Key, style, GUILayout.MinWidth(180f));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Seç / Düzenle", EditorStyles.miniButton, GUILayout.Width(90f), GUILayout.Height(16f)))
                    {
                        _selectedRowIndex = i;
                        GUI.FocusControl(null);
                    }
                    if (GUILayout.Button("Sil", EditorStyles.miniButton, GUILayout.Width(40f), GUILayout.Height(16f)))
                    {
                        if (EditorUtility.DisplayDialog("Anahtarı Sil", $"'{row.Key}' çeviri anahtarını silmek istediğinize emin misiniz?", "Sil", "İptal"))
                        {
                            _csvRows.RemoveAt(i);
                            if (_selectedRowIndex == i) _selectedRowIndex = -1;
                            else if (_selectedRowIndex > i) _selectedRowIndex--;
                            EditorGUILayout.EndHorizontal();
                            break;
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(2f);
                }

                EditorGUILayout.EndScrollView();
                RingFlowEditorUtils.EndSectionBox();

                // Yeni Ekleme Butonu
                if (GUILayout.Button("+ Yeni Çeviri Anahtarı (Key) Ekle", GUILayout.Height(26)))
                {
                    string newKey = "new_key_" + (_csvRows.Count + 1);
                    _csvRows.Add(new CSVRow { Key = newKey, Translations = new string[_csvLanguages.Count] });
                    _selectedRowIndex = _csvRows.Count - 1;
                }

                // Seçili Satır Detay Editörü
                if (_selectedRowIndex >= 0 && _selectedRowIndex < _csvRows.Count)
                {
                    var row = _csvRows[_selectedRowIndex];
                    RingFlowEditorUtils.BeginSectionBox("Çeviri Düzenleme Detayı", "Seçilen anahtarın tüm dillerdeki karşılıklarını düzenleyin.");
                    row.Key = EditorGUILayout.TextField("Anahtar (Key)", row.Key);

                    for (int j = 0; j < _csvLanguages.Count; j++)
                    {
                        string langCode = _csvLanguages[j];
                        row.Translations[j] = EditorGUILayout.TextField($"{langCode.ToUpper()}", row.Translations[j]);
                    }
                    _csvRows[_selectedRowIndex] = row;
                    RingFlowEditorUtils.EndSectionBox();
                }

                // Kaydetme ve Yenileme Butonları
                EditorGUILayout.Space(4f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("CSV Değişikliklerini Kaydet", GUILayout.Height(30)))
                    {
                        SaveCSV();
                    }
                    if (GUILayout.Button("Yeniden Yükle (Değişiklikleri İptal Et)", GUILayout.Height(30)))
                    {
                        LoadCSV();
                        _selectedRowIndex = -1;
                    }
                }
            }
        }

        private void LoadCSV()
        {
            _csvRows.Clear();
            _csvLanguages.Clear();
            string csvPath = "Assets/Resources/Localization.csv";
            if (!File.Exists(csvPath)) return;

            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length == 0) return;

            var headers = ParseCsvLine(lines[0]);
            for (int i = 1; i < headers.Count; i++)
            {
                _csvLanguages.Add(headers[i].Trim());
            }

            for (int i = 1; i < lines.Length; i++)
            {
                var fields = ParseCsvLine(lines[i]);
                if (fields.Count == 0 || string.IsNullOrEmpty(fields[0])) continue;

                string key = fields[0];
                string[] translations = new string[_csvLanguages.Count];
                for (int j = 0; j < _csvLanguages.Count; j++)
                {
                    translations[j] = j + 1 < fields.Count ? fields[j + 1] : "";
                }

                _csvRows.Add(new CSVRow { Key = key, Translations = translations });
            }
        }

        private void SaveCSV()
        {
            string csvPath = "Assets/Resources/Localization.csv";
            try
            {
                using var writer = new StreamWriter(csvPath, false, System.Text.Encoding.UTF8);

                writer.Write("Key");
                foreach (var lang in _csvLanguages)
                {
                    writer.Write("," + lang);
                }
                writer.WriteLine();

                foreach (var row in _csvRows)
                {
                    writer.Write(FormatCsvField(row.Key));
                    for (int i = 0; i < _csvLanguages.Count; i++)
                    {
                        string val = i < row.Translations.Length ? row.Translations[i] : "";
                        writer.Write("," + FormatCsvField(val));
                    }
                    writer.WriteLine();
                }

                writer.Flush();
                writer.Close();

                AssetDatabase.ImportAsset(csvPath);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Başarılı", "Localization.csv dosyası başarıyla güncellendi ve re-import edildi!", "Tamam");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Hata", $"CSV kaydedilemedi: {ex.Message}", "Tamam");
            }
        }

        private static string FormatCsvField(string val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            bool needsQuotes = val.Contains(",") || val.Contains("\"") || val.Contains("\n") || val.Contains("\r");
            if (needsQuotes)
            {
                return "\"" + val.Replace("\"", "\"\"") + "\"";
            }
            return val;
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
    }
}

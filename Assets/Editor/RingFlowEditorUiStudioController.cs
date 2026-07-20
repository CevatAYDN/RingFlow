using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RingFlow.Gameplay;
using RingFlow.Gameplay.UI;
using Nexus.Core;
using Nexus.Core.Services;
using System.Text;

namespace RingFlow.Editor
{
    /// <summary>
    /// Owns every editor surface that interacts with the runtime UIRoot:
    /// canvas reset, prefab reload, screen tree, signal tester, JSON export.
    /// Extracted from RingFlowEditorWindow so the window can stay a thin shell
    /// and this can be unit-tested or moved later without a 200-line OnGUI.
    /// </summary>
    internal sealed class RingFlowEditorUiStudioController
    {
        private const float ScreenTreeHeight = 260f;

        private Vector2 _uiScroll;
        private ScreenType _selectedScreen = ScreenType.Splash;

        private UnityEditor.Editor _cachedThemeEditor;
        private UnityEditor.Editor _cachedRegistryEditor;
        private UnityEditor.Editor _cachedLocEditor;

        public void DrawTab()
        {
            DrawInitControlsAndActions();
            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionSpacing);
            DrawScreenTree();
            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionSpacing);
            DrawSignalTester();
            EditorGUILayout.Space(EditorPaths.EditorSizes.SectionSpacing);
            DrawUiConfigAssetsInline();
        }

        private void DrawInitControlsAndActions()
        {
            var uiRoot = EditorSceneContext.GetUIRoot();
            // UYARI: Unity'de yok edilmiş MonoBehaviour'lar == null ile yakalanır,
            // switch pattern matching (is null) ile DEĞİL. Bu Unity gotcha'sıdır.
            // Bu yüzden switch expression yerine if/else kullanıyoruz.
            string status = uiRoot == null
                ? "EKSİK"
                : (uiRoot.Canvas != null ? "Hazır" : "Başlatma Bekliyor");

            RingFlowEditorUtils.BeginSectionBox("Arayüz Yönetim Kontrolleri", $"UIRoot: {(uiRoot != null ? uiRoot.name : "—")}  [{status}]");

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(uiRoot == null))
                {
                    if (GUILayout.Button(new GUIContent("Tüm Ekranları Yenile", "UIRoot içindeki tüm prefab ekranlarını yeniden yükler."), GUILayout.Height(26)))
                        ReloadPrefabScreens(uiRoot);
                    if (GUILayout.Button(new GUIContent("Tuvali Sıfırla", "UIRoot üzerindeki tüm ekranları ve Canvas'ı temizler."), GUILayout.Height(26)))
                        ResetUIRootCanvas(uiRoot);
                }

                if (GUILayout.Button(new GUIContent("Eksik Ekranları Oluştur", "Eksik UI prefablarını oluşturur."), GUILayout.Height(26)))
                    RingFlowEditorUiStudio.CreateMissingUIScreenPrefabs();

                if (GUILayout.Button(new GUIContent("Ekranları Yeniden Oluştur", "Mevcut base UI prefablarını siler ve hepsini yeniden üretir."), GUILayout.Height(26)))
                    RingFlowEditorUiStudio.RecreateAllUIScreenPrefabs();

                if (GUILayout.Button(new GUIContent("JSON Dışa Aktar", "UI ağacını JSON olarak kaydeder."), GUILayout.Height(26)))
                    ExportUIHierarchyAsJson();
            }

            EditorGUILayout.Space(4f);
            RingFlowEditorUtils.BeginSectionBox("Üretilecek Prefab Önizlemesi", "Her ekran için hedef yol önceden gösterilir.");
            foreach (var line in RingFlowEditorUiStudio.GetUIScreenPrefabPreviewLines())
            {
                EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
            }
            RingFlowEditorUtils.EndSectionBox();

            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Sahneyi Kaydet", GUILayout.Height(24)))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                    NexusLog.Info("RingFlowEditorUiStudioController", "DrawInitControlsAndActions", "SaveScene", "[RingFlow] Sahne mevcut durumla kaydedildi.");
                }
                if (GUILayout.Button("Yenile", GUILayout.Width(80f), GUILayout.Height(24)))
                {
                    var window = EditorWindow.HasOpenInstances<RingFlowEditorWindow>()
                        ? EditorWindow.GetWindow<RingFlowEditorWindow>()
                        : null;
                    window?.Repaint();
                }
            }

            RingFlowEditorUtils.EndSectionBox();
        }

        // ── Screen tree ──

        private void DrawScreenTree()
        {
            var uiRoot = EditorSceneContext.GetUIRoot();
            if (uiRoot == null)
            {
                EditorGUILayout.HelpBox(
                    "Sahnede UIRoot bulunamadı. Önce kurulumu yapın (Setup Bootstrapper).",
                    MessageType.Warning);
                return;
            }

            var screens = uiRoot.Screens;
            if (screens == null) return;

            string activeName = uiRoot.ActiveExclusiveScreen.ToString();
            string popups = FormatStack(uiRoot.PopupStack);
            int subs = uiRoot.Subscriptions?.Count ?? 0;
            string playSuffix = Application.isPlaying ? "" : " (PlayMode değil — sinyaller çalışmaz)";

            RingFlowEditorUtils.BeginSectionBox("UIRoot Ekran Ağacı (Screen Tree)", $"Aktif: {activeName}   Popuplar: {popups}   Abonelik: {subs}{playSuffix}");

            if (screens.Count == 0)
            {
                EditorGUILayout.HelpBox("Kayıtlı ekran bulunamadı.", MessageType.Info);
                RingFlowEditorUtils.EndSectionBox();
                return;
            }

            _uiScroll = EditorGUILayout.BeginScrollView(_uiScroll, GUILayout.Height(ScreenTreeHeight));
            int rowIndex = 0;
            foreach (var pair in screens)
            {
                ScreenType screen = pair.Key;
                var go = pair.Value as GameObject;
                DrawScreenRow(screen, go, uiRoot, rowIndex++);
            }
            EditorGUILayout.EndScrollView();

            RingFlowEditorUtils.EndSectionBox();
        }

        private void DrawScreenRow(ScreenType screen, GameObject go, UIRoot uiRoot, int rowIndex)
        {
            bool isActiveScreen = uiRoot.ActiveExclusiveScreen.Equals(screen);
            int btnCount = CountButtons(go);

            Rect rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(22f));
            Color rowBg = rowIndex % 2 == 0 ? new Color(0.2f, 0.22f, 0.25f, 0.3f) : new Color(0.15f, 0.17f, 0.2f, 0.3f);
            EditorGUI.DrawRect(rowRect, rowBg);

            string marker = isActiveScreen ? ">>" : "  ";
            string state = go == null ? "YOK" : (go.activeSelf ? "Açık" : "Gizli");
            var style = new GUIStyle(EditorStyles.label)
            {
                fontStyle = isActiveScreen ? FontStyle.Bold : FontStyle.Normal
            };
            if (isActiveScreen)
                style.normal.textColor = EditorPaths.EditorColors.Info;

            EditorGUILayout.LabelField($"{marker} {screen,-14} [{state}] {btnCount} Buton", style,
                GUILayout.MinWidth(240f));

            using (new EditorGUI.DisabledScope(go == null))
            {
                if (GUILayout.Button(new GUIContent("Göster", "Ekranı aktif yapar."), GUILayout.Width(48f), GUILayout.Height(16f)))
                    ManualSetScreenActive(uiRoot, screen, true);
                if (GUILayout.Button(new GUIContent("Gizle", "Ekranı devre dışı bırakır."), GUILayout.Width(48f), GUILayout.Height(16f)))
                    ManualSetScreenActive(uiRoot, screen, false);
                if (GUILayout.Button(new GUIContent("Sinyal", "ShowScreenSignal fırlatır (PlayMode gerekir)."), GUILayout.Width(56f), GUILayout.Height(16f)))
                    FireShowScreen(screen);
                if (GUILayout.Button(new GUIContent("Aç", "Prefab kaynağını seçer."), GUILayout.Width(48f), GUILayout.Height(16f)))
                    OpenPrefabSourceForScreen(screen, go);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2f);
        }

        // ── Signal tester ──

        private void DrawSignalTester()
        {
            RingFlowEditorUtils.BeginSectionBox("Sinyal Test Cihazı (Signal Tester)", "Play Mode sırasında ekran geçiş sinyallerini manuel olarak fırlatın.");

            using (new EditorGUILayout.HorizontalScope())
            {
                _selectedScreen = (ScreenType)EditorGUILayout.EnumPopup("Hedef Ekran", _selectedScreen, GUILayout.Width(280f));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Göster Sinyali Fırlat", "ShowScreenSignal fırlatır."), GUILayout.Height(22)))
                    FireShowScreen(_selectedScreen);
                if (GUILayout.Button(new GUIContent("Gizle Sinyali Fırlat", "HideScreenSignal fırlatır."), GUILayout.Height(22)))
                    FireHideScreen(_selectedScreen);
            }

            RingFlowEditorUtils.EndSectionBox();
        }

        // ── Public actions ──

        public static void ReloadPrefabScreens(UIRoot uiRoot, bool showDialog = true)
        {
            if (uiRoot == null)
            {
                if (showDialog)
                    EditorUtility.DisplayDialog("Reload Prefab Screens",
                        "UIRoot missing. Run Setup Bootstrapper first.", "OK");
                return;
            }

            var canvas = uiRoot.Canvas;
            if (canvas == null)
            {
                if (showDialog)
                    EditorUtility.DisplayDialog("Reload Prefab Screens", "UIRoot Canvas is missing.", "OK");
                return;
            }

            var screens = uiRoot.Screens;
            if (screens == null) return;

            uiRoot.ScreenLoader.Clear();

            var missingScreens = new List<string>();
            var allScreens = System.Enum.GetValues(typeof(ScreenType));
            int loadedCount = 0;

            foreach (ScreenType screen in allScreens)
            {
                var path = RingFlowEditorUiStudio.GetPrefabPathForScreen(screen);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    prefab = Resources.Load<GameObject>($"{EditorPaths.UiScreenPrefix}{screen}");
                if (prefab == null)
                {
                    missingScreens.Add(screen.ToString());
                    continue;
                }

                var instance = PrefabUtility.InstantiatePrefab(prefab, canvas.transform) as GameObject;
                if (instance != null)
                {
                    instance.name = screen.ToString();
                    instance.SetActive(screen == ScreenType.Splash);
                    uiRoot.ScreenLoader.SetScreen(screen, instance);
                    loadedCount++;
                }
            }

            uiRoot.ActiveExclusiveScreen = ScreenType.Splash;
            EditorUtility.SetDirty(uiRoot);
            EditorUtility.SetDirty(canvas.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            NexusLog.Info("RingFlowEditorUiStudioController", "ReloadPrefabScreens", "Reload", $"[RingFlow] ReloadPrefabScreens: {loadedCount} screens. Missing: {missingScreens.Count}");
            if (!showDialog) return;

            string msg = $"Loaded {loadedCount} screen(s) as Prefab Links.";
            if (missingScreens.Count > 0)
                msg += $"\nMissing: {string.Join(", ", missingScreens)}";
            EditorUtility.DisplayDialog("Reload Prefab Screens", msg, "OK");
        }

        public static void OpenPrefabSourceForScreen(ScreenType screen, GameObject go)
        {
            var path = RingFlowEditorUiStudio.GetPrefabPathForScreen(screen);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                AssetDatabase.OpenAsset(prefab);
                return;
            }

            if (go != null)
            {
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
            }
            EditorUtility.DisplayDialog("Open Prefab", $"Prefab not found at:\n{path}", "OK");
        }

        public static void ManualSetScreenActive(UIRoot uiRoot, ScreenType screen, bool active)
        {
            if (uiRoot == null) return;
            var screens = uiRoot.Screens;
            if (screens == null || !screens.ContainsKey(screen)) return;
            var go = screens[screen] as GameObject;
            if (go == null) return;

            go.SetActive(active);
            if (active)
                uiRoot.ActiveExclusiveScreen = screen;
        }

        public static void ResetUIRootCanvas(UIRoot uiRoot)
        {
            if (uiRoot != null) uiRoot.ResetForEditor();
        }

        // ── Signals ──

        public static void FireShowScreen(ScreenType screen) => FireScreenSignal(screen, show: true);
        public static void FireHideScreen(ScreenType screen) => FireScreenSignal(screen, show: false);

        private static void FireScreenSignal(ScreenType screen, bool show)
        {
            var root = EditorSceneContext.GetRoot();
            var context = root?.Context;
            if (context == null)
            {
                EditorUtility.DisplayDialog("UI Studio", "No initialized Root found.", "OK");
                return;
            }

            var bus = context.TryResolve<ISignalBus>();
            if (bus == null)
            {
                EditorUtility.DisplayDialog("UI Studio", "Signal bus unavailable.", "OK");
                return;
            }

            if (show) bus.Fire(new ShowScreenSignal(screen));
            else      bus.Fire(new HideScreenSignal(screen));
        }

        // ── Export ──

        public static void ExportUIHierarchyAsJson()
        {
            var uiRoot = EditorSceneContext.GetUIRoot();
            if (uiRoot == null)
            {
                EditorUtility.DisplayDialog("Export UI Tree", "UIRoot missing.", "OK");
                return;
            }

            var screens = uiRoot.Screens;
            if (screens == null || screens.Count == 0)
            {
                EditorUtility.DisplayDialog("Export UI Tree", "No screens to export.", "OK");
                return;
            }

            var sb = new StringBuilder();
            sb.Append("{\"screens\":[");
            bool first = true;
            foreach (var pair in screens)
            {
                var go = pair.Value as GameObject;
                if (go == null) continue;
                if (!first) sb.Append(",");
                first = false;
                sb.Append("{\"type\":\"").Append(pair.Key).Append("\",");
                sb.Append("\"active\":").Append(go.activeSelf ? "true" : "false");
                sb.Append("}");
            }
            sb.Append("]}");

            var path = EditorUtility.SaveFilePanel("Export UI Tree", "Assets/Snapshots", "ui-tree.json", "json");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                System.IO.File.WriteAllText(path, sb.ToString());
                EditorUtility.DisplayDialog("Export UI Tree", $"Exported to:\n{path}", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Export Failed", ex.Message, "OK");
            }
        }

        // ── Helpers ──

        private static int CountButtons(GameObject go)
        {
            if (go == null) return 0;
            var buttons = go.GetComponentsInChildren<Button>(true);
            return buttons != null ? buttons.Length : 0;
        }

        private static string FormatStack(System.Collections.ICollection stack)
        {
            if (stack == null || stack.Count == 0) return "(empty)";
            var items = new List<string>();
            foreach (var item in stack) items.Add(item?.ToString() ?? "null");
            return string.Join(" → ", items);
        }

        private void DrawUiConfigAssetsInline()
        {
            RingFlowEditorUtils.BeginSectionBox("Arayüz Konfigürasyon Dosyaları", "Arayüz teması, ekran kayıtları ve yerelleştirme ayarlarını doğrudan buradan düzenleyin.");

            // 1. Theme Config
            DrawInlineAssetFoldout(ref _cachedThemeEditor, EditorPaths.UIThemeConfigKey, "Arayüz Teması (UIThemeConfig)", "RingFlow.Foldout.UiThemeInline");
            
            EditorGUILayout.Space(4f);
            
            // 2. Screen Registry
            DrawInlineAssetFoldout(ref _cachedRegistryEditor, EditorPaths.ScreenRegistryKey, "Ekran Kayıt Defteri (Screen Registry)", "RingFlow.Foldout.ScreenRegistryInline");
            
            EditorGUILayout.Space(4f);
            
            // 3. Localization Config
            DrawInlineAssetFoldout(ref _cachedLocEditor, EditorPaths.LocalizationConfigKey, "Yerelleştirme Ayarları (LocalizationConfig)", "RingFlow.Foldout.LocInline");

            RingFlowEditorUtils.EndSectionBox();
        }

        private static readonly Dictionary<string, bool> s_uiStudioFoldStates = new();

        private void DrawInlineAssetFoldout(ref UnityEditor.Editor cachedEditor, string key, string title, string foldoutPrefKey)
        {
            if (Event.current.type == EventType.Layout)
            {
                s_uiStudioFoldStates[foldoutPrefKey] = EditorPrefs.GetBool(foldoutPrefKey, false);
            }

            if (!s_uiStudioFoldStates.TryGetValue(foldoutPrefKey, out bool isExpanded))
            {
                isExpanded = EditorPrefs.GetBool(foldoutPrefKey, false);
                s_uiStudioFoldStates[foldoutPrefKey] = isExpanded;
            }

            bool newExpanded = EditorGUILayout.Foldout(isExpanded, title, true, EditorStyles.foldoutHeader);
            if (newExpanded != isExpanded)
            {
                EditorPrefs.SetBool(foldoutPrefKey, newExpanded);
            }

            if (isExpanded)
            {
                if (cachedEditor == null || cachedEditor.target == null)
                {
                    var asset = Resources.Load<ScriptableObject>(key);
                    if (asset != null)
                    {
                        cachedEditor = UnityEditor.Editor.CreateEditor(asset);
                    }
                }

                if (cachedEditor != null)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        cachedEditor.OnInspectorGUI();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox($"{title} bulunamadı! Key: {key}", MessageType.Warning);
                }
            }
        }

        public void Cleanup()
        {
            if (_cachedThemeEditor != null)
            {
                Object.DestroyImmediate(_cachedThemeEditor);
                _cachedThemeEditor = null;
            }
            if (_cachedRegistryEditor != null)
            {
                Object.DestroyImmediate(_cachedRegistryEditor);
                _cachedRegistryEditor = null;
            }
            if (_cachedLocEditor != null)
            {
                Object.DestroyImmediate(_cachedLocEditor);
                _cachedLocEditor = null;
            }
        }

    }
}

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RingFlow.Gameplay;
using RingFlow.Gameplay.UI;
using Nexus.Core;
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

        public void DrawTab()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawInitControls();
                EditorGUILayout.Space(EditorSectionSpacing);
                DrawScreenTree();
                EditorGUILayout.Space(EditorSectionSpacing);
                DrawSignalTester();
                EditorGUILayout.Space(EditorSectionSpacing);
                DrawPersistence();
            }
        }

        // ── Init controls ──

        private void DrawInitControls()
        {
            var uiRoot = EditorSceneContext.GetUIRoot();
            using (new EditorGUILayout.HorizontalScope())
            {
                string status = uiRoot switch
                {
                    null => "EKSİK",
                    _ when uiRoot.Canvas != null => "Hazır",
                    _ => "Başlatma Bekliyor"
                };

                EditorGUILayout.LabelField(
                    $"UIRoot: {(uiRoot != null ? uiRoot.name : "—")}  [{status}]",
                    EditorStyles.boldLabel,
                    GUILayout.MinWidth(280f));
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(uiRoot == null))
                {
                    if (GUILayout.Button(new GUIContent("Tuvali Sıfırla (Reset)",
                            "UIRoot üzerindeki tüm yüklü ekranları ve Canvas'ı yok eder."),
                            GUILayout.Height(22)))
                    {
                        if (EditorUtility.DisplayDialog("UIRoot Tuvalini Sıfırla",
                            "Tüm yüklenen ekran örneklerini yok etmek istediğinize emin misiniz?",
                            "Sıfırla", "İptal"))
                        {
                            ResetUIRootCanvas(uiRoot);
                        }
                    }
                    if (GUILayout.Button(new GUIContent("Ekranları Yeniden Yükle",
                            "Tüm ScreenType prefab örneklerini yeniden oluşturur."),
                            GUILayout.Height(22)))
                    {
                        ReloadPrefabScreens(uiRoot);
                    }
                }
                if (GUILayout.Button("Yenile", GUILayout.Width(70f), GUILayout.Height(22)))
                {
                    var window = EditorWindow.HasOpenInstances<RingFlowEditorWindow>()
                        ? EditorWindow.GetWindow<RingFlowEditorWindow>()
                        : null;
                    window?.Repaint();
                }
            }
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

            EditorGUILayout.LabelField(
                $"Aktif: {activeName}   Popuplar: {popups}   Abonelik: {subs}{playSuffix}",
                EditorStyles.miniLabel);

            if (screens.Count == 0)
            {
                EditorGUILayout.HelpBox("Kayıtlı ekran bulunamadı.", MessageType.Info);
                return;
            }

            _uiScroll = EditorGUILayout.BeginScrollView(_uiScroll, GUILayout.Height(ScreenTreeHeight));
            foreach (var pair in screens)
            {
                ScreenType screen = pair.Key;
                var go = pair.Value as GameObject;
                DrawScreenRow(screen, go, uiRoot);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawScreenRow(ScreenType screen, GameObject go, UIRoot uiRoot)
        {
            bool isActiveScreen = uiRoot.ActiveExclusiveScreen.Equals(screen);
            int btnCount = go != null ? go.GetComponentsInChildren<Button>(true).Length : 0;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                string marker = isActiveScreen ? ">>" : "  ";
                string state = go == null ? "YOK" : (go.activeSelf ? "Açık" : "Gizli");
                var style = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = isActiveScreen ? FontStyle.Bold : FontStyle.Normal
                };
                EditorGUILayout.LabelField($"{marker} {screen,-14} [{state}] {btnCount} Buton", style,
                    GUILayout.MinWidth(240f));

                using (new EditorGUI.DisabledScope(go == null))
                {
                    if (GUILayout.Button(new GUIContent("Göster", "Ekranı aktif yapar."), GUILayout.Width(48f)))
                        ManualSetScreenActive(uiRoot, screen, true);
                    if (GUILayout.Button(new GUIContent("Gizle", "Ekranı devre dışı bırakır."), GUILayout.Width(48f)))
                        ManualSetScreenActive(uiRoot, screen, false);
                    if (GUILayout.Button(new GUIContent("Sinyal", "ShowScreenSignal fırlatır (PlayMode gerekir)."), GUILayout.Width(56f)))
                        FireShowScreen(screen);
                    if (GUILayout.Button(new GUIContent("Aç", "Prefab kaynağını seçer."), GUILayout.Width(48f)))
                        OpenPrefabSourceForScreen(screen, go);
                }
            }
        }

        // ── Signal tester ──

        private void DrawSignalTester()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Sinyal Test Edici (PlayMode gerekir)",
                    EditorStyles.miniBoldLabel, GUILayout.Width(220f));
                _selectedScreen = (ScreenType)EditorGUILayout.EnumPopup(_selectedScreen, GUILayout.Width(140f));
                if (GUILayout.Button(new GUIContent("Göster Sinyali",
                        "ShowScreenSignal fırlatır."), GUILayout.Height(22)))
                    FireShowScreen(_selectedScreen);
                if (GUILayout.Button(new GUIContent("Gizle Sinyali",
                        "HideScreenSignal fırlatır."), GUILayout.Height(22)))
                    FireHideScreen(_selectedScreen);
            }
        }

        // ── Persistence ──

        private void DrawPersistence()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Sahneyi Kaydet", GUILayout.Height(24)))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                    Debug.Log("[RingFlow] Sahne mevcut durumla kaydedildi.");
                }
                if (GUILayout.Button("Prefab Ekranlarını Yenile", GUILayout.Height(24)))
                {
                    try
                    {
                        var uiRoot = EditorSceneContext.GetUIRoot();
                        if (uiRoot != null) ReloadPrefabScreens(uiRoot);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                        EditorUtility.DisplayDialog("Prefab Ekranlarını Yenile", ex.Message, "Tamam");
                    }
                }
                if (GUILayout.Button("Eksik Ekranları Oluştur", GUILayout.Height(24)))
                    RingFlowEditorUiStudio.CreateMissingUIScreenPrefabs();
                if (GUILayout.Button("UI Yapısını Dışa Aktar (JSON)", GUILayout.Height(24)))
                    ExportUIHierarchyAsJson();
            }
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

            var toDestroy = new List<GameObject>();
            foreach (var pair in screens)
            {
                if (pair.Value is GameObject go && go != null)
                    toDestroy.Add(go);
            }
            screens.Clear();
            foreach (var go in toDestroy) Object.DestroyImmediate(go);

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
                    screens[screen] = instance;
                    loadedCount++;
                }
            }

            uiRoot.ActiveExclusiveScreen = ScreenType.Splash;
            EditorUtility.SetDirty(uiRoot);
            EditorUtility.SetDirty(canvas.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log($"[RingFlow] ReloadPrefabScreens: {loadedCount} screens. Missing: {missingScreens.Count}");
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

        private static string FormatStack(System.Collections.ICollection stack)
        {
            if (stack == null || stack.Count == 0) return "(empty)";
            var items = new List<string>();
            foreach (var item in stack) items.Add(item?.ToString() ?? "null");
            return string.Join(" → ", items);
        }

        // We keep an alias so the controller shares the section's spacing constant.
        private const float EditorSectionSpacing = 4f;
    }
}

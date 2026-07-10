using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;
using RingFlow.Gameplay.Services;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Creates the runtime Canvas, manages screen visibility via ShowScreenSignal,
    /// and bridges UI button signals to FSM transitions.
    /// Attached to NexusRoot by RingFlowEditorWindow.SetupNexusBootstrapper().
    /// </summary>
    public class UIRoot : MonoBehaviour
    {
        private static readonly HashSet<ScreenType> PopupScreens = new()
        {
            ScreenType.DailyReward,
            ScreenType.Settings,
            ScreenType.Pause,
            ScreenType.ChestPopup,
            ScreenType.ParentalGate,
        };

        /// <summary>
        /// Screens that remain semi-visible behind popups (e.g. HUD during gameplay).
        /// They are NOT deactivated when a popup opens — only the exclusive screen is suppressed.
        /// </summary>
        private static readonly HashSet<ScreenType> OverlayScreens = new()
        {
            ScreenType.Gameplay,
        };

        private Canvas _canvas;
        private readonly Dictionary<ScreenType, GameObject> _screens = new();
        private Root _root;
        private bool _subscribed;
        private ScreenType _activeExclusiveScreen = ScreenType.Splash;
        private readonly Stack<ScreenType> _popupStack = new();
        private readonly List<ISignalSubscription> _subscriptions = new();

        private const float ScreenFadeDuration = 0.3f;

        private void Awake()
        {
            EnsureCanvasExists();
            _root = GetComponentInParent<Root>();
            if (_root == null)
            {
                NexusLog.Error("UIRoot", nameof(Awake), "",
                    "No Root found in parent hierarchy; SubscribeOnce will be unreachable.");
            }
            else
            {
                NexusLog.Info("UIRoot", nameof(Awake), "",
                    $"Root found: {_root.name}");
            }
        }

        private void OnEnable()
        {
            NexusLog.Info("UIRoot", nameof(OnEnable), "",
                $"Enabled. canvas={_canvas != null}, screens={_screens.Count}");

            if (_canvas == null)
            {
                EnsureCanvasExists();
            }

            BindExistingScreens();
            if (_screens.Count == 0)
            {
                LoadPrefabScreensFromResources();
            }
        }

        private CancellationTokenSource _lifecycleCts;

        private void Start()
        {
            NexusLog.Info("UIRoot", nameof(Start), "",
                $"Starting. subscribed={_subscribed}, screens={_screens.Count}");
            TrySubscribeNow();

            if (_screens.Count == 0)
            {
                _lifecycleCts?.Cancel();
                _lifecycleCts?.Dispose();
                _lifecycleCts = new CancellationTokenSource();
                _ = LoadPrefabScreensAsync(_lifecycleCts.Token);
            }
        }

        private void TrySubscribeNow()
        {
            if (_root == null || _root.Context == null)
            {
                NexusLog.Warn("UIRoot", nameof(TrySubscribeNow), "",
                    "Root or Context not ready yet; will retry next frame.");
                return;
            }

            if (_screens.Count == 0)
            {
                LoadPrefabScreensFromResources();
            }

            SubscribeOnce();
        }

        private void Update()
        {
            if (!_subscribed)
            {
                TrySubscribeNow();
            }
        }

        private void SubscribeOnce()
        {
            if (_subscribed || _root == null) return;
            if (_root.Context == null) return;
            _subscribed = true;

            NexusLog.Info("UIRoot", nameof(SubscribeOnce), "",
                $"Subscribing to UI signals. screens={_screens.Count}");

            var sb = _root.Context.Resolve<ISignalBus>();
            _subscriptions.Add(sb.Subscribe<ShowScreenSignal>(OnShowScreen));
            _subscriptions.Add(sb.Subscribe<HideScreenSignal>(OnHideScreen));

            var fsm = _root.Context.TryResolve<IGameStateMachine>();
            if (fsm == null)
            {
                NexusLog.Error("UIRoot", nameof(SubscribeOnce), "",
                    "IGameStateMachine unbound; all UI button signals will be ignored. The UI will appear frozen.");
            }
            else
            {
                _subscriptions.Add(sb.Subscribe<PlayRequestedSignal>(_ => fsm.ChangeStateAsync<LevelSelectState>()));
                _subscriptions.Add(sb.Subscribe<LevelSelectedSignal>(s => fsm.ChangeStateAsync<PlayingState>(new PlayingStateArgs(s.LevelIndex))));
                _subscriptions.Add(sb.Subscribe<PauseRequestedSignal>(_ => fsm.ChangeStateAsync<PausedState>()));
                _subscriptions.Add(sb.Subscribe<ResumeRequestedSignal>(_ => fsm.ChangeStateAsync<PlayingState>(PlayingStateArgs.Resume)));

                _subscriptions.Add(sb.Subscribe<NextLevelRequestedSignal>(_ =>
                {
                    var prog = _root.Context.TryResolve<IProgressionService>();
                    if (prog != null)
                    {
                        var nextLevel = prog.CurrentLevel.Value; // Already advanced by CompleteCurrentLevel() on win
                        var completedLevel = nextLevel - 1;
                        var ads = _root.Context.TryResolve<IAdService>();
                        var progress = _root.Context.TryResolve<PlayerProgressModel>();
                        if (ads != null && (progress == null || !progress.RemoveAds.Value) && completedLevel % 3 == 0)
                        {
                            ads.ShowInterstitial("LevelComplete", () =>
                            {
                                fsm.ChangeStateAsync<PlayingState>(new PlayingStateArgs(nextLevel));
                            });
                        }
                        else
                        {
                            fsm.ChangeStateAsync<PlayingState>(new PlayingStateArgs(nextLevel));
                        }
                    }
                }));

                _subscriptions.Add(sb.Subscribe<OpenDailyRewardSignal>(_ => OpenPopup(ScreenType.DailyReward)));
                _subscriptions.Add(sb.Subscribe<OpenSettingsSignal>(_ => OpenPopup(ScreenType.Settings)));
                _subscriptions.Add(sb.Subscribe<CloseDailyRewardSignal>(_ => ClosePopup(ScreenType.DailyReward)));
                _subscriptions.Add(sb.Subscribe<CloseSettingsSignal>(_ => ClosePopup(ScreenType.Settings)));

                _subscriptions.Add(sb.Subscribe<OpenChestPopupSignal>(_ => OpenPopup(ScreenType.ChestPopup)));
                _subscriptions.Add(sb.Subscribe<CloseChestPopupSignal>(_ => ClosePopup(ScreenType.ChestPopup)));

                _subscriptions.Add(sb.Subscribe<QuitToMenuRequestedSignal>(_ =>
                {
                    CloseAllPopups();
                    fsm.ChangeStateAsync<MainMenuState>();
                }));
            }
        }

        private void OnDestroy()
        {
            foreach (var sub in _subscriptions)
            {
                sub?.Dispose();
            }
            _subscriptions.Clear();

            _lifecycleCts?.Cancel();
            _lifecycleCts?.Dispose();
            _lifecycleCts = null;
        }

        private void EnsureCanvasExists()
        {
            if (_canvas != null) return;

            var canvasGo = transform.Find("UICanvas")?.gameObject;
            if (canvasGo == null)
            {
                canvasGo = new GameObject("UICanvas");
                canvasGo.transform.SetParent(transform, false);
            }

            _canvas = canvasGo.GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = canvasGo.AddComponent<Canvas>();
            }
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            var scaler = canvasGo.GetComponent<CanvasScaler>() ?? canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            if (canvasGo.GetComponent<GraphicRaycaster>() == null)
            {
                canvasGo.AddComponent<GraphicRaycaster>();
            }
        }

        private static readonly ScreenType[] s_allScreens =
        {
            ScreenType.Splash,
            ScreenType.MainMenu,
            ScreenType.LevelSelect,
            ScreenType.Gameplay,
            ScreenType.Pause,
            ScreenType.Win,
            ScreenType.Settings,
            ScreenType.DailyReward,
            ScreenType.ChestPopup,
            ScreenType.GameOver,
            ScreenType.ParentalGate,
        };

    public void LoadPrefabScreensFromResources()
    {
        EnsureCanvasExists();
        if (_canvas == null)
        {
            NexusLog.Error("UIRoot", nameof(LoadPrefabScreensFromResources), "", "Canvas could not be created.");
            return;
        }

        var missingScreens = new List<ScreenType>();

        foreach (var screen in s_allScreens)
        {
            if (_screens.TryGetValue(screen, out var existing) && existing != null)
            {
                DestroyScreenInstance(existing);
            }

            var loaded = LoadScreenPrefab(screen);
            if (loaded == null)
            {
                missingScreens.Add(screen);
                NexusLog.Warn("UIRoot", nameof(LoadPrefabScreensFromResources), "",
                    $"Missing UI prefab for {screen}. Expected Assets/Resources/UI/{screen}.prefab");
                continue;
            }

            var instance = UnityEngine.Object.Instantiate(loaded, _canvas.transform);
            instance.name = screen.ToString();
            instance.SetActive(false);
            _screens[screen] = instance;
        }

        ApplyGameplayOverlayStyle();

        if (_screens.Count == 0)
        {
            NexusLog.Error("UIRoot", nameof(LoadPrefabScreensFromResources), "",
                $"No UI screens were loaded. Missing prefabs: {string.Join(", ", missingScreens)}");
        }
        else if (missingScreens.Count > 0)
        {
            NexusLog.Warn("UIRoot", nameof(LoadPrefabScreensFromResources), "",
                $"Loaded {_screens.Count} screen(s). Missing prefabs: {string.Join(", ", missingScreens)}");
        }
    }

    /// <summary>
    /// Async variant of <see cref="LoadPrefabScreensFromResources"/>. Cancels on
    /// scene teardown so the awaited Task doesn't replay work against a destroyed
    /// canvas. Lazy <see cref="TryShowScreen(string)"/> requests still use the sync
    /// fallback to honor same-frame expectations.
    /// </summary>
    public async Task LoadPrefabScreensAsync(CancellationToken ct = default)
    {
        EnsureCanvasExists();
        if (_canvas == null)
        {
            NexusLog.Error("UIRoot", nameof(LoadPrefabScreensAsync), "", "Canvas could not be created.");
            return;
        }

        var assets = ResolveAssetService();
        var missingScreens = new List<ScreenType>();

        foreach (var screen in s_allScreens)
        {
            if (ct.IsCancellationRequested) return;

            if (_screens.TryGetValue(screen, out var existing) && existing != null)
            {
                DestroyScreenInstance(existing);
            }

            GameObject loaded = null;
            if (assets != null)
            {
                try
                {
                    loaded = await assets.LoadAssetAsync<GameObject>($"UI/{screen}").ConfigureAwait(true);
                }
                catch (System.Exception ex)
                {
                    NexusLog.Warn("UIRoot", nameof(LoadPrefabScreensAsync), screen.ToString(),
                        $"AssetService load threw: {ex.Message}. Falling back to sync path.");
                }
            }
            if (loaded == null)
            {
                loaded = LoadScreenPrefab(screen);
            }
            if (loaded == null)
            {
                missingScreens.Add(screen);
                NexusLog.Warn("UIRoot", nameof(LoadPrefabScreensAsync), "",
                    $"Missing UI prefab for {screen}. Expected Assets/Resources/UI/{screen}.prefab");
                continue;
            }

            var instance = UnityEngine.Object.Instantiate(loaded, _canvas.transform);
            instance.name = screen.ToString();
            instance.SetActive(false);
            _screens[screen] = instance;

            await Task.Yield();
        }

        ApplyGameplayOverlayStyle();

        if (_screens.Count == 0)
        {
            NexusLog.Error("UIRoot", nameof(LoadPrefabScreensAsync), "",
                $"No UI screens were loaded. Missing prefabs: {string.Join(", ", missingScreens)}");
        }
        else if (missingScreens.Count > 0)
        {
            NexusLog.Warn("UIRoot", nameof(LoadPrefabScreensAsync), "",
                $"Loaded {_screens.Count} screen(s). Missing prefabs: {string.Join(", ", missingScreens)}");
        }
    }

    private IAssetService ResolveAssetService()
    {
        if (_root != null && _root.Context != null)
        {
            return _root.Context.TryResolve<IAssetService>();
        }
        return null;
    }

        public void BindExistingScreens()
        {
            EnsureCanvasExists();
            if (_canvas == null) return;

            _screens.Clear();

            foreach (Transform child in _canvas.transform)
            {
                if (child == null) continue;
                if (!System.Enum.TryParse<ScreenType>(child.name, out var screen)) continue;

                _screens[screen] = child.gameObject;
            }

            ApplyGameplayOverlayStyle();
        }

        public GameObject GetScreen(ScreenType screen)
        {
            return _screens.TryGetValue(screen, out var go) ? go : null;
        }

        public bool TryShowScreen(ScreenType screen)
        {
            if (_screens.Count == 0)
            {
                BindExistingScreens();
            }

            if (!_screens.ContainsKey(screen))
            {
                var prefab = LoadScreenPrefab(screen);
                if (prefab == null)
                {
                    NexusLog.Warn("UIRoot", nameof(TryShowScreen), "",
                        $"Screen {screen} is unavailable because the prefab is missing.");
                    return false;
                }

                EnsureCanvasExists();
                var instance = UnityEngine.Object.Instantiate(prefab, _canvas.transform);
                instance.name = screen.ToString();
                instance.SetActive(false);
                _screens[screen] = instance;
            }

            OnShowScreen(new ShowScreenSignal(screen));
            return true;
        }

        public static string GetPrefabAssetPath(ScreenType screen)
            => $"Assets/Resources/UI/{screen}.prefab";

        private static GameObject LoadScreenPrefab(ScreenType screen)
        {
            return Resources.Load<GameObject>($"UI/{screen}");
        }

        private void DestroyScreenInstance(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }

        private void ApplyGameplayOverlayStyle()
        {
            if (_screens.TryGetValue(ScreenType.Gameplay, out var go) && go != null)
            {
                var img = go.GetComponent<Image>();
                if (img != null)
                {
                    img.color = Color.clear;
                    img.raycastTarget = false;
                }
            }
        }

        private void OnShowScreen(ShowScreenSignal signal)
        {
            NexusLog.Info("UIRoot", nameof(OnShowScreen), "",
                $"Received ShowScreenSignal for {signal.Screen}. screens={_screens.Count}");

            if (signal.Screen == ScreenType.Splash && _screens.Count == 0)
            {
                BindExistingScreens();
            }

            if (PopupScreens.Contains(signal.Screen))
            {
                OpenPopup(signal.Screen);
                return;
            }

            EnsureCanvasExists();
            if (_screens.Count == 0)
            {
                BindExistingScreens();
            }

            if (!_screens.ContainsKey(signal.Screen))
            {
                var prefab = LoadScreenPrefab(signal.Screen);
                if (prefab != null)
                {
                    var instance = UnityEngine.Object.Instantiate(prefab, _canvas.transform);
                    instance.name = signal.Screen.ToString();
                    instance.SetActive(false);
                    _screens[signal.Screen] = instance;
                }
            }

            if (!_screens.ContainsKey(signal.Screen))
            {
                NexusLog.Error("UIRoot", nameof(OnShowScreen), "",
                    $"Cannot show {signal.Screen}: no scene object and no prefab at {GetPrefabAssetPath(signal.Screen)}");
                return;
            }

            NexusLog.Info("UIRoot", nameof(OnShowScreen), "",
                $"Showing {signal.Screen} and hiding {Math.Max(0, _screens.Count - 1)} other screen(s)." );

            foreach (var kvp in _screens)
            {
                bool shouldShow = kvp.Key == signal.Screen;
                TransitionScreen(kvp.Value, shouldShow);
            }
            _activeExclusiveScreen = signal.Screen;
            _popupStack.Clear();
        }

        private void OnHideScreen(HideScreenSignal signal)
        {
            ClosePopup(signal.Screen);
        }

        private void OpenPopup(ScreenType popup)
        {
            if (!_screens.TryGetValue(popup, out var go)) return;

            // Push current exclusive screen onto the stack before showing popup
            if (_popupStack.Count == 0 || _popupStack.Peek() != popup)
            {
                _popupStack.Push(_activeExclusiveScreen);
            }

            foreach (var kvp in _screens)
            {
                if (OverlayScreens.Contains(kvp.Key)) continue;
                TransitionScreen(kvp.Value, kvp.Key == popup);
            }
            _activeExclusiveScreen = popup;
        }

        private void ClosePopup(ScreenType popup)
        {
            if (!_screens.TryGetValue(popup, out var go)) return;
            FadeOutAndDeactivate(go);

            if (_popupStack.Count > 0)
            {
                var restore = _popupStack.Pop();
                _activeExclusiveScreen = restore;
                if (_screens.TryGetValue(restore, out var restoreGo))
                    FadeInAndActivate(restoreGo);
            }
        }

        private static void TransitionScreen(GameObject go, bool show)
        {
            if (go == null) return;
            if (show) FadeInAndActivate(go);
            else FadeOutAndDeactivate(go);
        }

        private static void FadeInAndActivate(GameObject go)
        {
            var cg = EnsureCanvasGroup(go);
            DOTween.Kill(cg);
            go.SetActive(true);
            cg.alpha = 0f;
            DOTween.To(() => cg.alpha, v => cg.alpha = v, 1f, ScreenFadeDuration)
                .SetEase(Ease.OutCubic).SetTarget(cg);
        }

        private static void FadeOutAndDeactivate(GameObject go)
        {
            var cg = EnsureCanvasGroup(go);
            DOTween.Kill(cg);
            DOTween.To(() => cg.alpha, v => cg.alpha = v, 0f, ScreenFadeDuration)
                .SetEase(Ease.InCubic).SetTarget(cg)
                .OnComplete(() => go.SetActive(false));
        }

        private static CanvasGroup EnsureCanvasGroup(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            return cg;
        }

        private void CloseAllPopups()
        {
            foreach (var popup in PopupScreens)
            {
                if (_screens.TryGetValue(popup, out var go))
                {
                    go.SetActive(false);
                }
            }
            _popupStack.Clear();
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void RebindFromSceneForEditor()
        {
            BindExistingScreens();
        }
    }
}

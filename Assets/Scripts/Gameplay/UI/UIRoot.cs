using System;
using System.Collections.Generic;
using System.IO;
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
    /// Premium UIRoot — creates the runtime Canvas, manages screen lifecycle via ShowScreenSignal,
    /// handles popup stacking with smooth transitions, keyboard navigation, and accessibility.
    /// Designed for production use across PC, mobile, and console.
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

        private static readonly HashSet<ScreenType> OverlayScreens = new()
        {
            ScreenType.Gameplay,
        };

        private Canvas _canvas;
        private CanvasScaler _scaler;
        private readonly Dictionary<ScreenType, GameObject> _screens = new();
        private Root _root;
        private ISignalBus _signalBus;
        private SettingsModel _settings;
        private GameFeelConfigSO _feelConfig;
        private UIThemeConfigSO _themeConfig;
        private bool _subscribed;
        private bool _screensLoaded;
        private ScreenType _activeExclusiveScreen = ScreenType.Splash;
        private readonly Stack<ScreenType> _popupStack = new();
        private readonly List<ISignalSubscription> _subscriptions = new();
        private UILayer[] _layers;

        [SerializeField] private float _screenFadeDuration = 0.35f;

        // ── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            EnsureCanvas();
            _root = GetComponentInParent<Root>();
            if (_root == null)
                NexusLog.Warn("UIRoot", nameof(Awake), "", "No Root found in parent hierarchy.");
            _settings = _root?.Context?.TryResolve<SettingsModel>();
            _feelConfig = _root?.Context?.TryResolve<GameFeelConfigSO>();
            _themeConfig = _root?.Context?.TryResolve<UIThemeConfigSO>();
        }

        private void OnEnable()
        {
            if (_canvas == null) EnsureCanvas();
            BindExistingScreens();
            if (_screens.Count == 0)
            {
                LoadPrefabScreensFromResources();
                _screensLoaded = _screens.Count > 0;
            }
            else
            {
                _screensLoaded = true;
            }
            // Note: signal subscription is performed in Start() and retried in Update()
            // until Root.Context is available. OnEnable runs before Root.Awake() finishes
            // when UIRoot is on the same GameObject as Root, so Context may be null here.
        }

        private CancellationTokenSource _lifecycleCts;

        private void Start()
        {
            NexusLog.Info("UIRoot", nameof(Start), "Lifecycle",
                $"_root={(_root != null ? "set" : "null")}, _root.Context={(_root?.Context != null ? "set" : "null")}, _screensLoaded={_screensLoaded}");
            TrySubscribeNow();
            if (_screens.Count == 0 && !_screensLoaded)
            {
                _lifecycleCts?.Cancel();
                _lifecycleCts?.Dispose();
                _lifecycleCts = new CancellationTokenSource();
                _ = LoadPrefabScreensAsync(_lifecycleCts.Token);
            }
        }

        private void TrySubscribeNow()
        {
            if (_root == null)
            {
                NexusLog.Warn("UIRoot", nameof(TrySubscribeNow), "Subscription",
                    "Root reference is null; cannot subscribe yet.");
                return;
            }
            if (_root.Context == null)
            {
                NexusLog.Warn("UIRoot", nameof(TrySubscribeNow), "Subscription",
                    "Root.Context is null; cannot subscribe yet.");
                return;
            }
            if (!_screensLoaded && _screens.Count == 0)
            {
                LoadPrefabScreensFromResources();
                _screensLoaded = _screens.Count > 0;
            }
            SubscribeOnce();
        }

        private void Update()
        {
            if (!_subscribed) TrySubscribeNow();

            // ── Keyboard Navigation ──
            HandleKeyboardInput();
        }

        private void HandleKeyboardInput()
        {
            if (!Application.isPlaying) return;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

            // Escape/Back → close topmost popup, or pause if in gameplay
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (_popupStack.Count > 0)
                {
                    NexusLog.Info("UIRoot", "KeyEscape", "",
                        $"Escape pressed — closing popup {_activeExclusiveScreen}.");
                    _signalBus?.Fire(new HideScreenSignal(_activeExclusiveScreen));
                }
                else if (_activeExclusiveScreen == ScreenType.Gameplay)
                {
                    _signalBus?.Fire(new PauseRequestedSignal());
                }
            }

            // Tab → cycle focus through buttons (accessibility)
            if (keyboard.tabKey.wasPressedThisFrame)
            {
                CycleFocus();
            }
        }

        private void CycleFocus()
        {
            var activeScreen = GetActiveScreen();
            if (activeScreen == null) return;
            var buttons = activeScreen.GetComponentsInChildren<Selectable>(true);
            if (buttons.Length == 0) return;

            var current = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            int currentIndex = -1;
            if (current != null)
            {
                var currentBtn = current.GetComponent<Selectable>();
                currentIndex = System.Array.IndexOf(buttons, currentBtn);
            }

            int nextIndex = (currentIndex + 1) % buttons.Length;
            var next = buttons[nextIndex];

            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(next.gameObject);

            next.Select();
            next.OnSelect(null);
        }

        private GameObject GetActiveScreen()
        {
            _screens.TryGetValue(_activeExclusiveScreen, out var go);
            return go;
        }

        // ── Subscription ──────────────────────────────────────────────────

        private void SubscribeOnce()
        {
            if (_subscribed || _root == null || _root.Context == null)
            {
                NexusLog.Warn("UIRoot", nameof(SubscribeOnce), "Subscription",
                    $"Skipping — _subscribed={_subscribed}, _root={(_root != null)}, Context={(_root?.Context != null)}.");
                return;
            }
            _subscribed = true;
            NexusLog.Info("UIRoot", nameof(SubscribeOnce), "Subscription",
                "Subscribing to gameplay signals (PlayRequestedSignal, LevelSelectedSignal, etc.).");

            var sb = _root.Context.Resolve<ISignalBus>();
            _signalBus = sb;
            _settings = _root.Context.TryResolve<SettingsModel>();
            _feelConfig = _root.Context.TryResolve<GameFeelConfigSO>();
            _themeConfig = _root.Context.TryResolve<UIThemeConfigSO>();

            if (_settings?.BigButtons != null)
            {
                _settings.BigButtons.OnChanged((_, val) => OnBigButtonsChanged());
                OnBigButtonsChanged();
            }

            if (_settings?.ReduceMotion != null)
            {
                _settings.ReduceMotion.OnChanged((_, val) =>
                {
                    GameUIResources.SetReducedMotion(val);
                });

                GameUIResources.SetReducedMotion(_settings.ReduceMotion.Value);
            }

            if (_screenFadeDuration <= 0f && _feelConfig != null)
                _screenFadeDuration = _feelConfig.UiScreenFadeDuration;

            _subscriptions.Add(sb.Subscribe<ShowScreenSignal>(OnShowScreen));
            _subscriptions.Add(sb.Subscribe<HideScreenSignal>(OnHideScreen));

            var fsm = _root.Context.TryResolve<IGameStateMachine>();
            if (fsm == null)
            {
                NexusLog.Error("UIRoot", nameof(SubscribeOnce), "Subscription",
                    "IGameStateMachine unbound — navigation signals (PlayRequestedSignal, etc.) will NOT be subscribed.");
            }
            else
            {
                _subscriptions.Add(sb.Subscribe<PlayRequestedSignal>(_ =>
                {
                    NexusLog.Info("UIRoot", "PlayRequestedSignal", "Navigation",
                        "Received — transitioning to LevelSelectState.");
                    fsm.ChangeStateAsync<LevelSelectState>();
                }));
                _subscriptions.Add(sb.Subscribe<LevelSelectedSignal>(s =>
                {
                    NexusLog.Info("UIRoot", "LevelSelectedSignal", "Navigation",
                        $"Received — transitioning to PlayingState (level {s.LevelIndex}). fsm={(fsm != null)}, fsm.CurrentState={(fsm?.CurrentState?.GetType().Name ?? "null")}");
                    fsm.ChangeStateAsync<PlayingState>(new PlayingStateArgs(s.LevelIndex));
                }));
                _subscriptions.Add(sb.Subscribe<PauseRequestedSignal>(_ => fsm.ChangeStateAsync<PausedState>()));
                _subscriptions.Add(sb.Subscribe<ResumeRequestedSignal>(_ => fsm.ChangeStateAsync<PlayingState>(PlayingStateArgs.Resume)));

                _subscriptions.Add(sb.Subscribe<NextLevelRequestedSignal>(_ =>
                {
                    var prog = _root.Context.TryResolve<IProgressionService>();
                    if (prog != null)
                    {
                        var nextLevel = prog.CurrentLevel.Value;
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

                _subscriptions.Add(sb.Subscribe<WorldMapRequestedSignal>(_ =>
                {
                    CloseAllPopups();
                    fsm.ChangeStateAsync<WorldMapState>();
                }));

                _subscriptions.Add(sb.Subscribe<QuitToMenuRequestedSignal>(_ =>
                {
                    CloseAllPopups();
                    fsm.ChangeStateAsync<MainMenuState>();
                }));

                NexusLog.Info("UIRoot", nameof(SubscribeOnce), "Subscription",
                    $"Subscribed to {_subscriptions.Count} signals. UIRoot is ready to receive navigation signals.");
            }
        }

        private void OnDestroy()
        {
            foreach (var sub in _subscriptions) sub?.Dispose();
            _subscriptions.Clear();
            _lifecycleCts?.Cancel();
            _lifecycleCts?.Dispose();
            _lifecycleCts = null;
        }

        // ── Canvas Setup ──────────────────────────────────────────────────

        private void EnsureCanvas()
        {
            if (_canvas != null) return;

            var canvasGo = transform.Find("UICanvas")?.gameObject;
            if (canvasGo == null)
            {
                canvasGo = new GameObject("UICanvas");
                canvasGo.transform.SetParent(transform, false);
            }

            _canvas = canvasGo.GetComponent<Canvas>();
            if (_canvas == null) _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            _canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Tangent;

            _scaler = canvasGo.GetComponent<CanvasScaler>();
            if (_scaler == null) _scaler = canvasGo.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1080, 1920);
            _scaler.matchWidthOrHeight = 0.5f;
            _scaler.referencePixelsPerUnit = 100;

            if (canvasGo.GetComponent<GraphicRaycaster>() == null)
                canvasGo.AddComponent<GraphicRaycaster>();

            // Canvas group for global fade
            if (canvasGo.GetComponent<CanvasGroup>() == null)
                canvasGo.AddComponent<CanvasGroup>();
        }

        // ── Screen Loading ────────────────────────────────────────────────

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
            ScreenType.WorldMap,
            ScreenType.Onboarding,
        };

        private string GetScreenPrefabKey(ScreenType screen)
        {
            var registry = new ResourcesAssetService()
                .LoadAsync<ScreenRegistrySO>(GameplayAssetKeys.ScreenRegistry)
                .GetAwaiter().GetResult();
            if (registry != null && registry.TryGetMapping(screen, out var mapping))
            {
                if (!string.IsNullOrEmpty(mapping.PrefabPath)) return mapping.PrefabPath;
            }
            return $"{GameplayAssetKeys.UiScreenPrefix}{screen}";
        }

        private List<ScreenType> GetScreensToLoad()
        {
            var list = new List<ScreenType>();
            var registry = Resources.Load<ScreenRegistrySO>(GameplayAssetKeys.ScreenRegistry);
            if (registry != null && registry.Mappings.Count > 0)
            {
                for (int i = 0; i < registry.Mappings.Count; i++)
                    list.Add(registry.Mappings[i].Screen);
            }
            else
            {
                list.AddRange(s_allScreens);
            }
            return list;
        }

        public void LoadPrefabScreensFromResources()
        {
            EnsureCanvas();
            if (_canvas == null) return;

            var missingScreens = new List<ScreenType>();
            var screensToLoad = GetScreensToLoad();

            foreach (var screen in screensToLoad)
            {
                if (_screens.TryGetValue(screen, out var existing) && existing != null)
                    DestroyScreenInstance(existing);

                var loaded = LoadScreenPrefab(screen);
                if (loaded == null)
                {
                    missingScreens.Add(screen);
                    continue;
                }

                var instance = Instantiate(loaded, _canvas.transform);
                instance.name = screen.ToString();
                instance.SetActive(false);
                _screens[screen] = instance;
            }

            ApplyGameplayOverlayStyle();

            if (missingScreens.Count > 0)
                NexusLog.Warn("UIRoot", nameof(LoadPrefabScreensFromResources), "",
                    $"Missing {missingScreens.Count} prefab(s): {string.Join(", ", missingScreens)}");
        }

        public async Task LoadPrefabScreensAsync(CancellationToken ct = default)
        {
            EnsureCanvas();
            if (_canvas == null) return;

            var assets = ResolveAssetService();
            var missingScreens = new List<ScreenType>();
            var screensToLoad = GetScreensToLoad();

            foreach (var screen in screensToLoad)
            {
                if (ct.IsCancellationRequested) return;
                if (_screens.TryGetValue(screen, out var existing) && existing != null)
                    DestroyScreenInstance(existing);

                GameObject loaded = null;
                string prefabKey = GetScreenPrefabKey(screen);
                if (assets != null)
                {
                    try
                    {
                        loaded = await assets.LoadAssetAsync<GameObject>(prefabKey).ConfigureAwait(true);
                    }
                    catch (Exception ex)
                    {
                        NexusLog.Warn("UIRoot", nameof(LoadPrefabScreensAsync), screen.ToString(), $"AssetService: {ex.Message}");
                    }
                }
                if (loaded == null) loaded = LoadScreenPrefab(screen);
                if (loaded == null)
                {
                    missingScreens.Add(screen);
                    continue;
                }

                var instance = Instantiate(loaded, _canvas.transform);
                instance.name = screen.ToString();
                instance.SetActive(false);
                _screens[screen] = instance;
                await Task.Yield();
            }

            ApplyGameplayOverlayStyle();
        }

        private IAssetService ResolveAssetService()
        {
            return _root?.Context?.TryResolve<IAssetService>();
        }

        public void BindExistingScreens()
        {
            EnsureCanvas();
            if (_canvas == null) return;
            _screens.Clear();
            foreach (Transform child in _canvas.transform)
            {
                if (child == null || !Enum.TryParse<ScreenType>(child.name, out var screen)) continue;
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
            if (_screens.Count == 0) BindExistingScreens();
            if (!_screens.ContainsKey(screen))
            {
                var prefab = LoadScreenPrefab(screen);
                if (prefab == null) return false;
                EnsureCanvas();
                var instance = Instantiate(prefab, _canvas.transform);
                instance.name = screen.ToString();
                instance.SetActive(false);
                _screens[screen] = instance;
            }
            OnShowScreen(new ShowScreenSignal(screen));
            return true;
        }

        public static string GetPrefabAssetPath(ScreenType screen)
            => $"Assets/Resources/UI/{screen}.prefab";

        private GameObject LoadScreenPrefab(ScreenType screen)
        {
            string prefabKey = GetScreenPrefabKey(screen);
            if (_root?.Context != null)
            {
                var asset = _root.Context.TryResolve<Services.IAssetService>();
                if (asset != null)
                {
                    var task = asset.LoadAsync<GameObject>(prefabKey);
                    return task.GetAwaiter().GetResult();
                }
            }
            var result = new ResourcesAssetService()
                .LoadAsync<GameObject>(prefabKey)
                .GetAwaiter().GetResult();
            if (result == null)
                return null;
            return result;
        }

        private void DestroyScreenInstance(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
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

        // ── Screen Transitions ────────────────────────────────────────────

        private void OnShowScreen(ShowScreenSignal signal)
        {
            NexusLog.Info("UIRoot", nameof(OnShowScreen), "Screen",
                $"ShowScreenSignal received: Screen={signal.Screen}, _screens.Count={_screens.Count}, _activeExclusiveScreen={_activeExclusiveScreen}");

            if (signal.Screen == ScreenType.Splash && _screens.Count == 0)
                BindExistingScreens();

            if (PopupScreens.Contains(signal.Screen))
            {
                NexusLog.Info("UIRoot", nameof(OnShowScreen), "Screen",
                    $"Routing {signal.Screen} to OpenPopup.");
                OpenPopup(signal.Screen);
                return;
            }

            EnsureCanvas();
            if (_screens.Count == 0) BindExistingScreens();

            if (!_screens.ContainsKey(signal.Screen))
            {
                var prefab = LoadScreenPrefab(signal.Screen);
                if (prefab != null)
                {
                    var instance = Instantiate(prefab, _canvas.transform);
                    instance.name = signal.Screen.ToString();
                    instance.SetActive(false);
                    _screens[signal.Screen] = instance;
                    NexusLog.Info("UIRoot", nameof(OnShowScreen), "Screen",
                        $"Instantiated screen prefab for {signal.Screen}. SetActive=false, waiting for TransitionScreen.");
                }
            }

            if (!_screens.ContainsKey(signal.Screen))
            {
                NexusLog.Error("UIRoot", nameof(OnShowScreen), "Screen",
                    $"Cannot show {signal.Screen}: no prefab at {GetPrefabAssetPath(signal.Screen)}");
                return;
            }

            bool reduceMotion = _settings?.ReduceMotion?.Value ?? false;

            foreach (var kvp in _screens)
            {
                bool shouldShow = kvp.Key == signal.Screen;
                if (OverlayScreens.Contains(kvp.Key))
                {
                    // Overlay screens (Gameplay) should only be visible when the
                    // active exclusive screen is Gameplay itself. Otherwise the
                    // board area shows through behind menus/popups, making it
                    // look like the game started without actually initializing.
                    bool overlayShouldBeActive = signal.Screen == ScreenType.Gameplay;
                    if (kvp.Value != null && kvp.Value.activeSelf != overlayShouldBeActive)
                    {
                        kvp.Value.SetActive(overlayShouldBeActive);
                        NexusLog.Info("UIRoot", nameof(OnShowScreen), "Screen",
                            $"Overlay screen {kvp.Key} {(overlayShouldBeActive ? "activated" : "deactivated")}.");
                    }
                    continue;
                }

                if (kvp.Key != ScreenType.Splash && !shouldShow && kvp.Value != null && kvp.Value.activeSelf)
                {
                    kvp.Value.SetActive(false);
                    NexusLog.Info("UIRoot", nameof(OnShowScreen), "Screen",
                        $"{kvp.Key} deactivated immediately before hide transition.");
                    continue;
                }

                NexusLog.Info("UIRoot", nameof(OnShowScreen), "Screen",
                    $"Transitioning {kvp.Key} to {(shouldShow ? "SHOW" : "HIDE")} (activeSelf={kvp.Value?.activeSelf}, reduceMotion={reduceMotion}).");
                TransitionScreen(kvp.Key, kvp.Value, shouldShow, reduceMotion);
            }
            _activeExclusiveScreen = signal.Screen;
            _popupStack.Clear();

            NexusLog.Info("UIRoot", nameof(OnShowScreen), "Screen",
                $"Transition complete. _activeExclusiveScreen={_activeExclusiveScreen}, _popupStack cleared.");
        }

        private void OnHideScreen(HideScreenSignal signal)
        {
            ClosePopup(signal.Screen);
        }

        // ── Popup Management ──────────────────────────────────────────────

        private void OpenPopup(ScreenType popup)
        {
            if (!_screens.TryGetValue(popup, out var go) || go == null)
            {
                var prefab = LoadScreenPrefab(popup);
                if (prefab == null) return;
                EnsureCanvas();
                go = Instantiate(prefab, _canvas.transform);
                go.name = popup.ToString();
                go.SetActive(false);
                _screens[popup] = go;
            }

            // Push current exclusive screen onto stack before showing popup
            if (_popupStack.Count == 0 || _popupStack.Peek() != popup)
                _popupStack.Push(_activeExclusiveScreen);

            bool reduceMotion = _settings?.ReduceMotion?.Value ?? false;

            foreach (var kvp in _screens)
            {
                if (OverlayScreens.Contains(kvp.Key)) continue;
                TransitionScreen(kvp.Key, kvp.Value, kvp.Key == popup, reduceMotion);
            }
            _activeExclusiveScreen = popup;

            // Animate popup entry
            if (!reduceMotion && go != null)
            {
                DOTween.Kill(go.transform);
                go.transform.localScale = Vector3.one * 0.8f;
                go.transform.DOScale(Vector3.one, 0.3f).SetEase(DG.Tweening.Ease.OutBack).SetAutoKill(true);
            }
        }

        private void ClosePopup(ScreenType popup)
        {
            if (!_screens.TryGetValue(popup, out var go)) return;
            bool reduceMotion = _settings?.ReduceMotion?.Value ?? false;

            if (!reduceMotion)
            {
                DOTween.Kill(go.transform);
                go.transform.DOScale(0.85f, 0.2f).SetEase(DG.Tweening.Ease.InBack).SetAutoKill(true)
                    .OnComplete(() => go.SetActive(false));
            }
            else
            {
                go.SetActive(false);
            }

            if (_popupStack.Count > 0)
            {
                var restore = _popupStack.Pop();
                _activeExclusiveScreen = restore;
                if (_screens.TryGetValue(restore, out var restoreGo))
                {
                    bool rm = reduceMotion;
                    TransitionScreen(restore, restoreGo, true, rm);
                }
            }
        }

        private void CloseAllPopups()
        {
            foreach (var popup in PopupScreens)
            {
                if (_screens.TryGetValue(popup, out var go))
                    go.SetActive(false);
            }
            _popupStack.Clear();
        }

        // ── Individual Screen Transition ──────────────────────────────────

        private void TransitionScreen(ScreenType type, GameObject go, bool show, bool reduceMotion)
        {
            if (go == null) return;

            NexusLog.Info("UIRoot", nameof(TransitionScreen), "Screen",
                $"{type}: show={show}, activeSelf(before)={go.activeSelf}, reduceMotion={reduceMotion}");

            if (show)
            {
                if (reduceMotion)
                {
                    go.SetActive(true);
                    var cg = EnsureCanvasGroup(go);
                    cg.alpha = 1f;
                }
                else
                {
                    GameUIResources.AnimateScreenEntry(go, _screenFadeDuration);
                }
            }
            else
            {
                if (reduceMotion)
                {
                    go.SetActive(false);
                }
                else
                {
                    GameUIResources.AnimateScreenExit(go, _screenFadeDuration * 0.7f);
                }
            }

            // Check immediately after transition call (non-animated changes are
            // synchronous; animated ones have started their DOTween sequence).
            NexusLog.Info("UIRoot", nameof(TransitionScreen), "Screen",
                $"{type}: activeSelf(after transition call)={go.activeSelf}");
        }

        private void OnBigButtonsChanged()
        {
            var go = _canvas?.gameObject;
            if (go == null) return;
            var scaler = go.GetComponent<CanvasScaler>();
            if (scaler == null) return;
            bool big = _settings?.BigButtons?.Value ?? false;
            scaler.referenceResolution = big ? new Vector2(810, 1440) : new Vector2(1080, 1920);
        }

        private static CanvasGroup EnsureCanvasGroup(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            return cg;
        }

        // ── Editor Support ────────────────────────────────────────────────

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void RebindFromSceneForEditor() => BindExistingScreens();

#if UNITY_EDITOR
        public Canvas Canvas
        {
            get
            {
                if (_canvas == null) EnsureCanvas();
                return _canvas;
            }
            set => _canvas = value;
        }
        public CanvasScaler Scaler => _scaler;
        public Dictionary<ScreenType, GameObject> Screens => _screens;
        public ScreenType ActiveExclusiveScreen { get => _activeExclusiveScreen; set => _activeExclusiveScreen = value; }
        public Stack<ScreenType> PopupStack => _popupStack;
        public List<ISignalSubscription> Subscriptions => _subscriptions;
        public bool Subscribed { get => _subscribed; set => _subscribed = value; }

        public void ResetForEditor()
        {
            var toDestroy = new List<GameObject>();
            foreach (var go in _screens.Values)
                if (go != null) toDestroy.Add(go);
            _screens.Clear();
            foreach (var go in toDestroy) DestroyImmediate(go);
            if (_canvas != null) DestroyImmediate(_canvas.gameObject);
            _canvas = null;
            _subscribed = false;
        }
#endif
    }
}

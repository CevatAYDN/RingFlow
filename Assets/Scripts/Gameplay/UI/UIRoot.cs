using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Premium UIRoot — creates the runtime Canvas, manages screen lifecycle via ShowScreenSignal,
    /// handles popup stacking with smooth transitions, keyboard navigation, and accessibility.
    ///
    /// Orchestration hub that delegates to focused helper classes:
    ///   - CanvasManager: Canvas/CanvasScaler/CanvasGroup infrastructure
    ///   - ScreenLoader: Screen prefab loading, caching, lifecycle
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
            ScreenType.MechanicGuide,
        };

        private static readonly HashSet<ScreenType> OverlayScreens = new()
        {
            ScreenType.Gameplay,
        };

        private CanvasManager _canvasManager;
        private ScreenLoader _screenLoader;
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

        [SerializeField] private float _screenFadeDuration = 0.35f;

        // ── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            _canvasManager = new CanvasManager();
            _canvasManager.EnsureCanvas(transform);
            _screenLoader = new ScreenLoader(_canvasManager.Transform);
            _root = GetComponentInParent<Root>();
            if (_root == null)
                NexusLog.Warn("UIRoot", nameof(Awake), "", "No Root found in parent hierarchy.");
            _settings = _root?.Context?.TryResolve<SettingsModel>();
            _feelConfig = _root?.Context?.TryResolve<GameFeelConfigSO>();
            _themeConfig = _root?.Context?.TryResolve<UIThemeConfigSO>();
        }

        private void OnEnable()
        {
            _screenLoader.BindExistingScreens();
            if (_screenLoader.Screens.Count == 0)
            {
                _screenLoader.LoadAllFromResources();
                _screensLoaded = _screenLoader.Screens.Count > 0;
            }
            else
            {
                _screensLoaded = true;
            }
            ApplyGameplayOverlayStyle();
        }

        private CancellationTokenSource _lifecycleCts;

        private void Start()
        {
            NexusLog.Info("UIRoot", nameof(Start), "Lifecycle",
                $"_root={(_root != null ? "set" : "null")}, _root.Context={(_root?.Context != null ? "set" : "null")}, _screensLoaded={_screensLoaded}");
            TrySubscribeNow();
            if (_screenLoader.Screens.Count == 0 && !_screensLoaded)
            {
                _lifecycleCts?.Cancel();
                _lifecycleCts?.Dispose();
                _lifecycleCts = new CancellationTokenSource();
                _ = LoadAllScreensAsync(_lifecycleCts.Token);
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
            if (!_screensLoaded && _screenLoader.Screens.Count == 0)
            {
                _screenLoader.LoadAllFromResources();
                _screensLoaded = _screenLoader.Screens.Count > 0;
            }
            SubscribeOnce();
        }

        private void Update()
        {
            if (!_subscribed) TrySubscribeNow();
            HandleKeyboardInput();
        }

        // ── Keyboard Input ─────────────────────────────────────────────────

        private void HandleKeyboardInput()
        {
            if (!Application.isPlaying) return;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

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

            if (keyboard.tabKey.wasPressedThisFrame)
                CycleFocus();
        }

        private void CycleFocus()
        {
            var activeScreen = _screenLoader.GetScreen(_activeExclusiveScreen);
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
                "Subscribing to gameplay signals.");

            var sb = _root.Context.Resolve<ISignalBus>();
            _signalBus = sb;
            _settings = _root.Context.TryResolve<SettingsModel>();
            _feelConfig = _root.Context.TryResolve<GameFeelConfigSO>();
            _themeConfig = _root.Context.TryResolve<UIThemeConfigSO>();

            if (_settings?.BigButtons != null)
            {
                _settings.BigButtons.OnChanged((_, val) => _canvasManager.SetBigButtons(val));
                _canvasManager.SetBigButtons(_settings.BigButtons.Value);
            }

            if (_settings?.ReduceMotion != null)
            {
                _settings.ReduceMotion.OnChanged((_, val) => GameUIResources.SetReducedMotion(val));
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
                    "IGameStateMachine unbound — navigation signals will NOT be subscribed.");
            }
            else
            {
                SubscribeNavigationSignals(sb, fsm);
            }
        }

        private void SubscribeNavigationSignals(ISignalBus sb, IGameStateMachine fsm)
        {
            _subscriptions.Add(sb.Subscribe<PlayRequestedSignal>(_ =>
                fsm.ChangeStateAsync<LevelSelectState>()));
            _subscriptions.Add(sb.Subscribe<LevelSelectedSignal>(s =>
                fsm.ChangeStateAsync<PlayingState>(new PlayingStateArgs(s.LevelIndex))));
            _subscriptions.Add(sb.Subscribe<PauseRequestedSignal>(_ => fsm.ChangeStateAsync<PausedState>()));
            _subscriptions.Add(sb.Subscribe<ResumeRequestedSignal>(_ =>
                fsm.ChangeStateAsync<PlayingState>(PlayingStateArgs.Resume)));
            _subscriptions.Add(sb.Subscribe<NextLevelRequestedSignal>(_ => HandleNextLevel(fsm)));
            _subscriptions.Add(sb.Subscribe<OpenDailyRewardSignal>(_ => OpenPopup(ScreenType.DailyReward)));
            _subscriptions.Add(sb.Subscribe<OpenSettingsSignal>(_ => OpenPopup(ScreenType.Settings)));
            _subscriptions.Add(sb.Subscribe<CloseDailyRewardSignal>(_ => ClosePopup(ScreenType.DailyReward)));
            _subscriptions.Add(sb.Subscribe<CloseSettingsSignal>(_ => ClosePopup(ScreenType.Settings)));
            _subscriptions.Add(sb.Subscribe<OpenChestPopupSignal>(_ => OpenPopup(ScreenType.ChestPopup)));
            _subscriptions.Add(sb.Subscribe<CloseChestPopupSignal>(_ => ClosePopup(ScreenType.ChestPopup)));
            _subscriptions.Add(sb.Subscribe<OpenMechanicGuideSignal>(_ => OpenPopup(ScreenType.MechanicGuide)));
            _subscriptions.Add(sb.Subscribe<CloseMechanicGuideSignal>(_ => ClosePopup(ScreenType.MechanicGuide)));
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
        }

        private void HandleNextLevel(IGameStateMachine fsm)
        {
            var prog = _root.Context.TryResolve<IProgressionService>();
            if (prog == null) return;
            var nextLevel = prog.CurrentLevel.Value;
            var completedLevel = nextLevel - 1;
            var ads = _root.Context.TryResolve<IAdService>();
            var progress = _root.Context.TryResolve<PlayerProgressModel>();

            if (ads != null && (progress == null || !progress.RemoveAds.Value) && completedLevel % 3 == 0)
            {
                ads.ShowInterstitial("LevelComplete", () =>
                    fsm.ChangeStateAsync<PlayingState>(new PlayingStateArgs(nextLevel)));
            }
            else
            {
                fsm.ChangeStateAsync<PlayingState>(new PlayingStateArgs(nextLevel));
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

        // ── Screen Loading ────────────────────────────────────────────────

        public async Task LoadAllScreensAsync(CancellationToken ct = default)
        {
            await _screenLoader.LoadAllAsync(ct);
            ApplyGameplayOverlayStyle();
        }

        public void LoadPrefabScreensFromResources()
        {
            _screenLoader.LoadAllFromResources();
            ApplyGameplayOverlayStyle();
        }

        public void BindExistingScreens()
        {
            _screenLoader.BindExistingScreens();
            ApplyGameplayOverlayStyle();
        }

        public GameObject GetScreen(ScreenType screen) => _screenLoader.GetScreen(screen);

        public bool TryShowScreen(ScreenType screen)
        {
            _screenLoader.EnsureScreenLoaded(screen);
            if (!_screenLoader.HasScreen(screen)) return false;
            OnShowScreen(new ShowScreenSignal(screen));
            return true;
        }

        public static string GetPrefabAssetPath(ScreenType screen)
            => ScreenLoader.GetPrefabAssetPath(screen);

        private void ApplyGameplayOverlayStyle()
        {
            var go = _screenLoader.GetScreen(ScreenType.Gameplay);
            if (go != null)
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
                $"ShowScreenSignal: Screen={signal.Screen}");

            if (PopupScreens.Contains(signal.Screen))
            {
                OpenPopup(signal.Screen);
                return;
            }

            _screenLoader.EnsureScreenLoaded(signal.Screen);
            if (!_screenLoader.HasScreen(signal.Screen))
            {
                NexusLog.Error("UIRoot", nameof(OnShowScreen), "Screen",
                    $"Cannot show {signal.Screen}: no prefab at {GetPrefabAssetPath(signal.Screen)}");
                return;
            }

            bool reduceMotion = _settings?.ReduceMotion?.Value ?? false;

            foreach (var kvp in _screenLoader.Screens)
            {
                bool shouldShow = kvp.Key == signal.Screen;

                if (OverlayScreens.Contains(kvp.Key))
                {
                    bool overlayActive = signal.Screen == ScreenType.Gameplay;
                    if (kvp.Value != null && kvp.Value.activeSelf != overlayActive)
                        kvp.Value.SetActive(overlayActive);
                    continue;
                }

                if (kvp.Key != ScreenType.Splash && !shouldShow && kvp.Value != null && kvp.Value.activeSelf)
                {
                    kvp.Value.SetActive(false);
                    continue;
                }

                TransitionScreen(kvp.Key, kvp.Value, shouldShow, reduceMotion);
            }
            _activeExclusiveScreen = signal.Screen;
            _popupStack.Clear();
        }

        private void OnHideScreen(HideScreenSignal signal)
        {
            ClosePopup(signal.Screen);
        }

        // ── Popup Management ──────────────────────────────────────────────

        private void OpenPopup(ScreenType popup)
        {
            var go = _screenLoader.EnsureScreenLoaded(popup);
            if (go == null) return;

            if (_popupStack.Count == 0 || _popupStack.Peek() != popup)
                _popupStack.Push(_activeExclusiveScreen);

            bool reduceMotion = _settings?.ReduceMotion?.Value ?? false;

            foreach (var kvp in _screenLoader.Screens)
            {
                if (OverlayScreens.Contains(kvp.Key)) continue;
                TransitionScreen(kvp.Key, kvp.Value, kvp.Key == popup, reduceMotion);
            }
            _activeExclusiveScreen = popup;

            if (!reduceMotion && go != null)
            {
                DOTween.Kill(go.transform);
                go.transform.localScale = Vector3.one * 0.8f;
                go.transform.DOScale(Vector3.one, 0.3f).SetEase(DG.Tweening.Ease.OutBack).SetAutoKill(true);
            }
        }

        private void ClosePopup(ScreenType popup)
        {
            var go = _screenLoader.GetScreen(popup);
            if (go == null) return;

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
                var restoreGo = _screenLoader.GetScreen(restore);
                if (restoreGo != null)
                    TransitionScreen(restore, restoreGo, true, reduceMotion);
            }
        }

        private void CloseAllPopups()
        {
            foreach (var popup in PopupScreens)
            {
                var go = _screenLoader.GetScreen(popup);
                if (go != null) go.SetActive(false);
            }
            _popupStack.Clear();
        }

        // ── Individual Screen Transition ──────────────────────────────────

        private void TransitionScreen(ScreenType type, GameObject go, bool show, bool reduceMotion)
        {
            if (go == null) return;

            if (show)
            {
                if (reduceMotion)
                {
                    go.SetActive(true);
                    var cg = CanvasManager.EnsureCanvasGroup(go);
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
                    go.SetActive(false);
                else
                    GameUIResources.AnimateScreenExit(go, _screenFadeDuration * 0.7f);
            }
        }

        // ── Editor Support ────────────────────────────────────────────────

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void RebindFromSceneForEditor() => BindExistingScreens();

#if UNITY_EDITOR
        public Canvas Canvas
        {
            get
            {
                if (_canvasManager == null)
                {
                    _canvasManager = new CanvasManager();
                    _canvasManager.EnsureCanvas(transform);
                }
                else if (_canvasManager.Canvas == null)
                {
                    _canvasManager.EnsureCanvas(transform);
                }
                return _canvasManager.Canvas;
            }
            set { } // kept for editor compat
        }
        public CanvasScaler Scaler => _canvasManager.Scaler;
        public IReadOnlyDictionary<ScreenType, GameObject> Screens => _screenLoader.Screens;
        public ScreenLoader ScreenLoader => _screenLoader;
        public ScreenType ActiveExclusiveScreen { get => _activeExclusiveScreen; set => _activeExclusiveScreen = value; }
        public Stack<ScreenType> PopupStack => _popupStack;
        public List<ISignalSubscription> Subscriptions => _subscriptions;
        public bool Subscribed { get => _subscribed; set => _subscribed = value; }

        public void ResetForEditor()
        {
            _screenLoader.Clear();
            _canvasManager.Destroy();
            _subscribed = false;
        }
#endif
    }
}

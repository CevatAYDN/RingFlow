using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.FSM;
using Nexus.Core.Services;
using UnityEngine;
using UnityEngine.UI;

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

        private void Awake()
        {
            var canvasGo = new GameObject("UICanvas");
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            CreateScreen<SplashView>(ScreenType.Splash, canvasGo.transform);
            CreateScreen<MainMenuView>(ScreenType.MainMenu, canvasGo.transform);
            CreateScreen<LevelSelectView>(ScreenType.LevelSelect, canvasGo.transform);
            CreateScreen<HUDView>(ScreenType.Gameplay, canvasGo.transform);
            CreateScreen<PauseView>(ScreenType.Pause, canvasGo.transform);
            CreateScreen<WinView>(ScreenType.Win, canvasGo.transform);
            CreateScreen<SettingsView>(ScreenType.Settings, canvasGo.transform);
            CreateScreen<DailyRewardPopupView>(ScreenType.DailyReward, canvasGo.transform);
            CreateScreen<GameOverView>(ScreenType.GameOver, canvasGo.transform);

            _root = GetComponentInParent<Root>();
            if (_root == null)
            {
                NexusLog.Error("UIRoot", nameof(Awake), "",
                    "No Root found in parent hierarchy; SubscribeOnce will be unreachable.");
            }
        }

        private void Start()
        {
            StartCoroutine(WaitForContextAndSubscribe());
        }

        private System.Collections.IEnumerator WaitForContextAndSubscribe()
        {
            while (_root == null)
            {
                yield return null;
            }
            while (!_root.IsInitialized)
            {
                yield return null;
            }
            SubscribeOnce();
        }

        private void SubscribeOnce()
        {
            if (_subscribed || _root == null) return;
            if (_root.Context == null) return;
            _subscribed = true;

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
                _subscriptions.Add(sb.Subscribe<LevelSelectedSignal>(s => fsm.ChangeStateAsync<PlayingState>(s.LevelIndex)));
                _subscriptions.Add(sb.Subscribe<PauseRequestedSignal>(_ => fsm.ChangeStateAsync<PausedState>()));
                _subscriptions.Add(sb.Subscribe<ResumeRequestedSignal>(_ => fsm.ChangeStateAsync<PlayingState>("resume")));

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
                                fsm.ChangeStateAsync<PlayingState>(nextLevel);
                            });
                        }
                        else
                        {
                            fsm.ChangeStateAsync<PlayingState>(nextLevel);
                        }
                    }
                }));

                _subscriptions.Add(sb.Subscribe<OpenDailyRewardSignal>(_ => OpenPopup(ScreenType.DailyReward)));
                _subscriptions.Add(sb.Subscribe<OpenSettingsSignal>(_ => OpenPopup(ScreenType.Settings)));
                _subscriptions.Add(sb.Subscribe<CloseDailyRewardSignal>(_ => ClosePopup(ScreenType.DailyReward)));
                _subscriptions.Add(sb.Subscribe<CloseSettingsSignal>(_ => ClosePopup(ScreenType.Settings)));

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
        }

        private void CreateScreen<T>(ScreenType type, Transform parent) where T : View
        {
            var go = GameUIResources.CreatePanel(type.ToString(), parent);
            go.SetActive(false);
            go.AddComponent<T>();
            _screens[type] = go;

            if (type == ScreenType.Gameplay)
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
            if (PopupScreens.Contains(signal.Screen))
            {
                OpenPopup(signal.Screen);
                return;
            }

            foreach (var kvp in _screens)
            {
                kvp.Value.SetActive(kvp.Key == signal.Screen);
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
                kvp.Value.SetActive(kvp.Key == popup);
            }
            _activeExclusiveScreen = popup;
        }

        private void ClosePopup(ScreenType popup)
        {
            if (!_screens.TryGetValue(popup, out var go)) return;
            go.SetActive(false);

            if (_popupStack.Count > 0)
            {
                var restore = _popupStack.Pop();
                _activeExclusiveScreen = restore;
                if (_screens.TryGetValue(restore, out var restoreGo))
                {
                    restoreGo.SetActive(true);
                }
            }
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
    }
}

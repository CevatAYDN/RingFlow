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

        private Canvas _canvas;
        private readonly Dictionary<ScreenType, GameObject> _screens = new();
        private Root _root;
        private bool _subscribed;
        private ScreenType _activeExclusiveScreen = ScreenType.Splash;
        private ScreenType? _pausedExclusiveScreen;

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

            _root = GetComponentInParent<Root>();
            if (_root == null) { Debug.LogError("[UIRoot] No Root found in parent hierarchy."); }
        }

        private void Start()
        {
            SubscribeOnce();
        }

        private void SubscribeOnce()
        {
            if (_subscribed || _root == null) return;
            _subscribed = true;

            var sb = _root.Context.Resolve<ISignalBus>();
            sb.Subscribe<ShowScreenSignal>(OnShowScreen);
            sb.Subscribe<HideScreenSignal>(OnHideScreen);

            var fsm = _root.Context.TryResolve<IGameStateMachine>();
            if (fsm != null)
            {
                sb.Subscribe<PlayRequestedSignal>(_ => fsm.ChangeStateAsync<LevelSelectState>());
                sb.Subscribe<LevelSelectedSignal>(s => fsm.ChangeStateAsync<PlayingState>(s.LevelIndex));
                sb.Subscribe<PauseRequestedSignal>(_ => fsm.ChangeStateAsync<PausedState>());
                sb.Subscribe<ResumeRequestedSignal>(_ => fsm.ChangeStateAsync<PlayingState>());

                sb.Subscribe<NextLevelRequestedSignal>(_ =>
                {
                    var prog = _root.Context.TryResolve<IProgressionService>();
                    if (prog != null)
                    {
                        var nextLevel = prog.CurrentLevel.Value + 1;
                        prog.SetLevel(nextLevel);
                        fsm.ChangeStateAsync<PlayingState>(nextLevel);
                    }
                });

                sb.Subscribe<OpenDailyRewardSignal>(_ => OpenPopup(ScreenType.DailyReward));
                sb.Subscribe<OpenSettingsSignal>(_ => OpenPopup(ScreenType.Settings));
                sb.Subscribe<CloseDailyRewardSignal>(_ => ClosePopup(ScreenType.DailyReward));
                sb.Subscribe<CloseSettingsSignal>(_ => ClosePopup(ScreenType.Settings));

                sb.Subscribe<QuitToMenuRequestedSignal>(_ =>
                {
                    CloseAllPopups();
                    fsm.ChangeStateAsync<MainMenuState>();
                });
            }
        }

        private void CreateScreen<T>(ScreenType type, Transform parent) where T : View
        {
            var go = GameUIResources.CreatePanel(type.ToString(), parent);
            go.SetActive(false);
            go.AddComponent<T>();
            _screens[type] = go;
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
            _pausedExclusiveScreen = null;
        }

        private void OnHideScreen(HideScreenSignal signal)
        {
            ClosePopup(signal.Screen);
        }

        private void OpenPopup(ScreenType popup)
        {
            if (!_screens.TryGetValue(popup, out var go)) return;

            if (PopupScreens.Contains(_activeExclusiveScreen) && !_pausedExclusiveScreen.HasValue)
            {
                _pausedExclusiveScreen = _activeExclusiveScreen;
            }

            foreach (var kvp in _screens)
            {
                kvp.Value.SetActive(kvp.Key == popup);
            }
            _activeExclusiveScreen = popup;
        }

        private void ClosePopup(ScreenType popup)
        {
            if (!_screens.TryGetValue(popup, out var go)) return;
            go.SetActive(false);

            if (_pausedExclusiveScreen.HasValue)
            {
                var restore = _pausedExclusiveScreen.Value;
                _pausedExclusiveScreen = null;
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
            _pausedExclusiveScreen = null;
        }
    }
}

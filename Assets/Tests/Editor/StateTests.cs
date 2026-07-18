using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Nexus.Core;
using RingFlow.Gameplay;

namespace RingFlow.Tests
{
    [TestFixture]
    public class StateTests
    {
        private StateTestSignalBus _signalBus;

        [SetUp]
        public void Setup()
        {
            _signalBus = new StateTestSignalBus();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public async Task BootState_OnEnter_DoesNotThrow_WhenFsmIsNull()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            var state = new BootState();
            await state.OnEnterAsync(null, CancellationToken.None);
        }

        [Test]
        public async Task BootState_OnExit_DoesNotThrow()
        {
            var state = new BootState();
            await state.OnExitAsync(CancellationToken.None);
        }

        [Test]
        public void BootState_OnTick_DoesNotThrow()
        {
            var state = new BootState();
            Assert.DoesNotThrow(() => state.OnTick(0.016f));
        }

        [Test]
        public async Task SplashState_OnEnter_FiresShowScreenSplash()
        {
            var state = new SplashState();
            InjectField(state, "_signalBus", _signalBus);
            await state.OnEnterAsync(null, CancellationToken.None);
            Assert.IsTrue(_signalBus.HasFiredShowScreenSplash);
        }

        [Test]
        public async Task SplashState_OnEnter_FiresImmediately_WhenSignalBusAvailable()
        {
            var state = new SplashState();
            InjectField(state, "_signalBus", _signalBus);

            var enterTask = state.OnEnterAsync(null, CancellationToken.None).AsTask();

            Assert.IsTrue(enterTask.IsCompleted, "Splash state should not wait for extra frames before showing the splash screen.");
            await enterTask;
            Assert.IsTrue(_signalBus.HasFiredShowScreenSplash);
        }

        [Test]
        public async Task SplashState_OnEnter_DoesNotThrow_WhenSignalBusNull()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            var state = new SplashState();
            await state.OnEnterAsync(null, CancellationToken.None);
        }

        [Test]
        public async Task MainMenuState_OnEnter_FiresShowScreenMainMenu()
        {
            var state = new MainMenuState();
            InjectField(state, "_signalBus", _signalBus);
            await state.OnEnterAsync(null, CancellationToken.None);
            Assert.IsTrue(_signalBus.HasFiredShowScreenMainMenu);
        }

        [Test]
        public async Task PlayingState_OnEnter_FiresShowScreenGameplay()
        {
            var state = new PlayingState();
            InjectField(state, "_signalBus", _signalBus);
            await state.OnEnterAsync(null, CancellationToken.None);
            Assert.IsTrue(_signalBus.HasFiredShowScreenGameplay);
        }

        [Test]
        public async Task PlayingState_OnEnter_WithIntLevelArg_FiresInitLevel()
        {
            var signalBus = new StateTestSignalBusExtended();
            var state = new PlayingState();
            InjectField(state, "_signalBus", signalBus);
            await state.OnEnterAsync(5, CancellationToken.None);
            Assert.IsTrue(signalBus.HasFiredInitLevelFor(5));
        }

        [Test]
        public async Task PlayingState_OnEnter_WithIntLevelArg_FiresInitLevelBeforeGameplayScreen()
        {
            var signalBus = new OrderedStateTestSignalBus();
            var state = new PlayingState();
            InjectField(state, "_signalBus", signalBus);

            await state.OnEnterAsync(5, CancellationToken.None);

            Assert.That(signalBus.InitLevelLevelIndex, Is.EqualTo(5));
            Assert.That(signalBus.InitLevelSignalOrder, Is.GreaterThan(0));
            Assert.That(signalBus.GameplayScreenSignalOrder, Is.GreaterThan(0));
            Assert.That(signalBus.InitLevelSignalOrder, Is.LessThan(signalBus.GameplayScreenSignalOrder));
        }

        [Test]
        public void UIRoot_OnShowScreen_HidesNonTargetExclusiveScreensImmediately()
        {
            var go = new UnityEngine.GameObject("UIRootTest");
            var uiRoot = go.AddComponent<RingFlow.Gameplay.UI.UIRoot>();

            var screensField = typeof(RingFlow.Gameplay.UI.UIRoot).GetField("_screens",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var activeScreenField = typeof(RingFlow.Gameplay.UI.UIRoot).GetField("_activeExclusiveScreen",
                BindingFlags.NonPublic | BindingFlags.Instance);

            var screens = new Dictionary<RingFlow.Gameplay.ScreenType, UnityEngine.GameObject>
            {
                { RingFlow.Gameplay.ScreenType.MainMenu, new UnityEngine.GameObject("MainMenu") },
                { RingFlow.Gameplay.ScreenType.Gameplay, new UnityEngine.GameObject("Gameplay") },
                { RingFlow.Gameplay.ScreenType.DailyReward, new UnityEngine.GameObject("DailyReward") },
            };

            screens[RingFlow.Gameplay.ScreenType.MainMenu].SetActive(true);
            screens[RingFlow.Gameplay.ScreenType.Gameplay].SetActive(true);
            screens[RingFlow.Gameplay.ScreenType.DailyReward].SetActive(true);

            screensField.SetValue(uiRoot, screens);
            activeScreenField.SetValue(uiRoot, RingFlow.Gameplay.ScreenType.MainMenu);

            var onShowScreen = typeof(RingFlow.Gameplay.UI.UIRoot).GetMethod("OnShowScreen",
                BindingFlags.NonPublic | BindingFlags.Instance);

            onShowScreen.Invoke(uiRoot, new object[] { new ShowScreenSignal(RingFlow.Gameplay.ScreenType.Gameplay) });

            Assert.That(screens[RingFlow.Gameplay.ScreenType.Gameplay].activeSelf, Is.True);
            Assert.That(screens[RingFlow.Gameplay.ScreenType.MainMenu].activeSelf, Is.False);
            Assert.That(screens[RingFlow.Gameplay.ScreenType.DailyReward].activeSelf, Is.False);

            UnityEngine.Object.DestroyImmediate(go);
            foreach (var kvp in screens)
            {
                if (kvp.Value != null)
                    UnityEngine.Object.DestroyImmediate(kvp.Value);
            }
        }

        [Test]
        public async Task PlayingState_OnExit_DoesNotThrow()
        {
            var state = new PlayingState();
            await state.OnExitAsync(CancellationToken.None);
        }

        [Test]
        public async Task PausedState_OnEnter_FiresShowScreenPause()
        {
            var state = new PausedState();
            InjectField(state, "_signalBus", _signalBus);
            await state.OnEnterAsync(null, CancellationToken.None);
            Assert.IsTrue(_signalBus.LastScreenType == ScreenType.Pause);
        }

        [Test]
        public async Task WinState_OnEnter_FiresShowScreenWin()
        {
            var state = new WinState();
            InjectField(state, "_signalBus", _signalBus);
            await state.OnEnterAsync(null, CancellationToken.None);
            Assert.IsTrue(_signalBus.LastScreenType == ScreenType.Win);
        }

        [Test]
        public async Task GameOverState_OnEnter_FiresShowScreenGameOver()
        {
            var state = new GameOverState();
            InjectField(state, "_signalBus", _signalBus);
            await state.OnEnterAsync(null, CancellationToken.None);
            Assert.IsTrue(_signalBus.LastScreenType == ScreenType.GameOver);
        }

        [Test]
        public async Task LevelSelectState_OnEnter_FiresShowScreenLevelSelect()
        {
            var state = new LevelSelectState();
            InjectField(state, "_signalBus", _signalBus);
            await state.OnEnterAsync(null, CancellationToken.None);
            Assert.IsTrue(_signalBus.LastScreenType == ScreenType.LevelSelect);
        }

        [Test]
        public void AllStates_OnTick_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => new BootState().OnTick(0.016f));
            Assert.DoesNotThrow(() => new SplashState().OnTick(0.016f));
            Assert.DoesNotThrow(() => new MainMenuState().OnTick(0.016f));
            Assert.DoesNotThrow(() => new PlayingState().OnTick(0.016f));
            Assert.DoesNotThrow(() => new PausedState().OnTick(0.016f));
            Assert.DoesNotThrow(() => new WinState().OnTick(0.016f));
            Assert.DoesNotThrow(() => new GameOverState().OnTick(0.016f));
            Assert.DoesNotThrow(() => new LevelSelectState().OnTick(0.016f));
        }

        private static void InjectField<T>(T instance, string fieldName, object value)
        {
            var field = typeof(T).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(instance, value);
            }
        }
    }

    public class StateTestSignalBus : ISignalBus
    {
        public ScreenType? LastScreenType { get; private set; }
        public bool HasFiredShowScreenSplash => LastScreenType == ScreenType.Splash;
        public bool HasFiredShowScreenMainMenu => LastScreenType == ScreenType.MainMenu;
        public bool HasFiredShowScreenGameplay => LastScreenType == ScreenType.Gameplay;

        public void Fire<T>(T signal) where T : struct
        {
            if (signal is ShowScreenSignal showScreen)
                LastScreenType = showScreen.Screen;
        }

        public IReadOnlyDictionary<Type, IReadOnlyList<CommandHandlerInfo>> RegisteredHandlers =>
            new Dictionary<Type, IReadOnlyList<CommandHandlerInfo>>();
        public ValueTask FireAsync<T>(T signal) where T : struct { Fire(signal); return default; }
        public void FireThreadSafe<T>(T signal) where T : struct { }
        public void FireNextFrame<T>(T signal) where T : struct { }
        public ValueTask FireAsyncWithTimeout<T>(T signal, int timeoutMilliseconds) where T : struct => default;
        public ValueTask FireAsyncAndForget<T>(T signal, Action<Exception> onError = null) where T : struct => default;
        public ISignalSubscription Subscribe<T>(Action<T> handler) where T : struct => null;
        public ISignalSubscription SubscribeAsync<T>(Func<T, CancellationToken, ValueTask> handler) where T : struct => null;
    }

    public class StateTestSignalBusExtended : ISignalBus
    {
        private int _initLevelFor = -1;
        private ScreenType? _showScreen;
        public bool HasFiredInitLevelFor(int level) => _initLevelFor == level;

        public void Fire<T>(T signal) where T : struct
        {
            if (signal is ShowScreenSignal showScreen)
                _showScreen = showScreen.Screen;
            if (signal is InitLevelSignal initLevel)
                _initLevelFor = initLevel.LevelIndex;
        }

        public IReadOnlyDictionary<Type, IReadOnlyList<CommandHandlerInfo>> RegisteredHandlers =>
            new Dictionary<Type, IReadOnlyList<CommandHandlerInfo>>();
        public ValueTask FireAsync<T>(T signal) where T : struct { Fire(signal); return default; }
        public void FireThreadSafe<T>(T signal) where T : struct { }
        public void FireNextFrame<T>(T signal) where T : struct { }
        public ValueTask FireAsyncWithTimeout<T>(T signal, int timeoutMilliseconds) where T : struct => default;
        public ValueTask FireAsyncAndForget<T>(T signal, Action<Exception> onError = null) where T : struct => default;
        public ISignalSubscription Subscribe<T>(Action<T> handler) where T : struct => null;
        public ISignalSubscription SubscribeAsync<T>(Func<T, CancellationToken, ValueTask> handler) where T : struct => null;
    }

    public class OrderedStateTestSignalBus : ISignalBus
    {
        private int _sequence;
        public int InitLevelLevelIndex { get; private set; } = -1;
        public int InitLevelSignalOrder { get; private set; } = -1;
        public int GameplayScreenSignalOrder { get; private set; } = -1;

        public void Fire<T>(T signal) where T : struct
        {
            _sequence++;
            if (signal is InitLevelSignal initLevel)
            {
                InitLevelLevelIndex = initLevel.LevelIndex;
                InitLevelSignalOrder = _sequence;
            }
            if (signal is ShowScreenSignal showScreen && showScreen.Screen == ScreenType.Gameplay)
                GameplayScreenSignalOrder = _sequence;
        }

        public IReadOnlyDictionary<Type, IReadOnlyList<CommandHandlerInfo>> RegisteredHandlers =>
            new Dictionary<Type, IReadOnlyList<CommandHandlerInfo>>();
        public ValueTask FireAsync<T>(T signal) where T : struct { Fire(signal); return default; }
        public void FireThreadSafe<T>(T signal) where T : struct { }
        public void FireNextFrame<T>(T signal) where T : struct { }
        public ValueTask FireAsyncWithTimeout<T>(T signal, int timeoutMilliseconds) where T : struct => default;
        public ValueTask FireAsyncAndForget<T>(T signal, Action<Exception> onError = null) where T : struct => default;
        public ISignalSubscription Subscribe<T>(Action<T> handler) where T : struct => null;
        public ISignalSubscription SubscribeAsync<T>(Func<T, CancellationToken, ValueTask> handler) where T : struct => null;
    }
}

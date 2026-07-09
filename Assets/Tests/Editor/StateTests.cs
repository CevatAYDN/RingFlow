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

        [Test]
        public void BootState_OnEnter_DoesNotThrow_WhenFsmIsNull()
        {
            var state = new BootState();
            Assert.DoesNotThrowAsync(async () => await state.OnEnterAsync(null, CancellationToken.None));
        }

        [Test]
        public void BootState_OnExit_DoesNotThrow()
        {
            var state = new BootState();
            Assert.DoesNotThrowAsync(async () => await state.OnExitAsync(CancellationToken.None));
        }

        [Test]
        public void BootState_OnTick_DoesNotThrow()
        {
            var state = new BootState();
            Assert.DoesNotThrow(() => state.OnTick(0.016f));
        }

        [Test]
        public void SplashState_OnEnter_FiresShowScreenSplash()
        {
            InjectField<SplashState>("_signalBus", _signalBus);
            var state = new SplashState();
            Assert.DoesNotThrowAsync(async () => await state.OnEnterAsync(null, CancellationToken.None));
            Assert.IsTrue(_signalBus.HasFiredShowScreenSplash);
        }

        [Test]
        public void SplashState_OnEnter_DoesNotThrow_WhenSignalBusNull()
        {
            var state = new SplashState();
            Assert.DoesNotThrowAsync(async () => await state.OnEnterAsync(null, CancellationToken.None));
        }

        [Test]
        public void MainMenuState_OnEnter_FiresShowScreenMainMenu()
        {
            InjectField<MainMenuState>("_signalBus", _signalBus);
            var state = new MainMenuState();
            Assert.DoesNotThrowAsync(async () => await state.OnEnterAsync(null, CancellationToken.None));
            Assert.IsTrue(_signalBus.HasFiredShowScreenMainMenu);
        }

        [Test]
        public void PlayingState_OnEnter_FiresShowScreenGameplay()
        {
            InjectField<PlayingState>("_signalBus", _signalBus);
            var state = new PlayingState();
            Assert.DoesNotThrowAsync(async () => await state.OnEnterAsync(null, CancellationToken.None));
            Assert.IsTrue(_signalBus.HasFiredShowScreenGameplay);
        }

        [Test]
        public void PlayingState_OnEnter_WithIntLevelArg_FiresInitLevel()
        {
            var signalBus = new StateTestSignalBusExtended();
            InjectField<PlayingState>("_signalBus", signalBus);
            var state = new PlayingState();
            Assert.DoesNotThrowAsync(async () => await state.OnEnterAsync(5, CancellationToken.None));
            Assert.IsTrue(signalBus.HasFiredInitLevelFor(5));
        }

        [Test]
        public void PlayingState_OnExit_DoesNotThrow()
        {
            var state = new PlayingState();
            Assert.DoesNotThrowAsync(async () => await state.OnExitAsync(CancellationToken.None));
        }

        [Test]
        public void PausedState_OnEnter_FiresShowScreenPause()
        {
            InjectField<PausedState>("_signalBus", _signalBus);
            var state = new PausedState();
            Assert.DoesNotThrowAsync(async () => await state.OnEnterAsync(null, CancellationToken.None));
            Assert.IsTrue(_signalBus.LastScreenType == ScreenType.Pause);
        }

        [Test]
        public void WinState_OnEnter_FiresShowScreenWin()
        {
            InjectField<WinState>("_signalBus", _signalBus);
            var state = new WinState();
            Assert.DoesNotThrowAsync(async () => await state.OnEnterAsync(null, CancellationToken.None));
            Assert.IsTrue(_signalBus.LastScreenType == ScreenType.Win);
        }

        [Test]
        public void GameOverState_OnEnter_FiresShowScreenGameOver()
        {
            InjectField<GameOverState>("_signalBus", _signalBus);
            var state = new GameOverState();
            Assert.DoesNotThrowAsync(async () => await state.OnEnterAsync(null, CancellationToken.None));
            Assert.IsTrue(_signalBus.LastScreenType == ScreenType.GameOver);
        }

        [Test]
        public void LevelSelectState_OnEnter_FiresShowScreenLevelSelect()
        {
            InjectField<LevelSelectState>("_signalBus", _signalBus);
            var state = new LevelSelectState();
            Assert.DoesNotThrowAsync(async () => await state.OnEnterAsync(null, CancellationToken.None));
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

        private static void InjectField<T>(string fieldName, object value) where T : new()
        {
            var instance = new T();
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
        public ValueTask FireAsync<T>(T signal) where T : struct => default;
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
        public ValueTask FireAsync<T>(T signal) where T : struct => default;
        public void FireThreadSafe<T>(T signal) where T : struct { }
        public void FireNextFrame<T>(T signal) where T : struct { }
        public ValueTask FireAsyncWithTimeout<T>(T signal, int timeoutMilliseconds) where T : struct => default;
        public ValueTask FireAsyncAndForget<T>(T signal, Action<Exception> onError = null) where T : struct => default;
        public ISignalSubscription Subscribe<T>(Action<T> handler) where T : struct => null;
        public ISignalSubscription SubscribeAsync<T>(Func<T, CancellationToken, ValueTask> handler) where T : struct => null;
    }
}
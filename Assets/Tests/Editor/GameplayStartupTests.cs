using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Nexus.Core;
using Nexus.Core.FSM;
using RingFlow.Gameplay;

namespace RingFlow.Tests
{
    [TestFixture]
    public class GameplayStartupTests
    {
        private GameStateMachine _fsm;
        private BootState _bootState;
        private SplashState _splashState;
        private MainMenuState _mainMenuState;
        private MockSignalBusExtended _signalBus;

        [SetUp]
        public void Setup()
        {
            _fsm = new GameStateMachine();
            _bootState = new BootState();
            _splashState = new SplashState();
            _mainMenuState = new MainMenuState();
            _signalBus = new MockSignalBusExtended();

            // Inject dependencies using reflection since we are running in unit tests outside the DI container
            InjectField(_bootState, "_fsm", _fsm);
            InjectField(_splashState, "_signalBus", _signalBus);
            InjectField(_mainMenuState, "_signalBus", _signalBus);
        }

        private void InjectField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        [Test]
        public void Fsm_Registration_Succeeds()
        {
            _fsm.RegisterState(_bootState);
            _fsm.RegisterState(_splashState);
            _fsm.RegisterState(_mainMenuState);

            // Verified they register without throwing any exceptions
            Assert.Pass();
        }

        [Test]
        public async Task BootState_TransitionsToSplashState_Successfully()
        {
            _fsm.RegisterState(_bootState);
            _fsm.RegisterState(_splashState);

            await _fsm.ChangeStateAsync<BootState>();

            Assert.IsInstanceOf<SplashState>(_fsm.CurrentState);
            Assert.IsTrue(_signalBus.HasFiredShowScreenSplash);
        }
    }

    public class MockSignalBusExtended : ISignalBus
    {
        public bool HasFiredShowScreenSplash { get; private set; }

        public System.Collections.Generic.IReadOnlyDictionary<Type, System.Collections.Generic.IReadOnlyList<CommandHandlerInfo>> RegisteredHandlers => null;

        public void Fire<T>(T signal) where T : struct
        {
            if (signal is ShowScreenSignal showScreen)
            {
                if (showScreen.Screen == ScreenType.Splash)
                {
                    HasFiredShowScreenSplash = true;
                }
            }
        }

        public ValueTask FireAsync<T>(T signal) where T : struct => default;
        public void FireThreadSafe<T>(T signal) where T : struct {}
        public void FireNextFrame<T>(T signal) where T : struct {}
        public ValueTask FireAsyncWithTimeout<T>(T signal, int timeoutMilliseconds) where T : struct => default;
        public ValueTask FireAsyncAndForget<T>(T signal, Action<Exception> onError = null) where T : struct => default;

        public ISignalSubscription Subscribe<T>(Action<T> handler) where T : struct => null;
        public ISignalSubscription SubscribeAsync<T>(Func<T, CancellationToken, ValueTask> handler) where T : struct => null;
    }
}

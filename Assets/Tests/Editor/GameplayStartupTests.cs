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
    public class GameplayStartupTests
    {
        private MockSignalBusExtended _signalBus;

        [SetUp]
        public void Setup()
        {
            _signalBus = new MockSignalBusExtended();
        }

        [Test]
        public void Fsm_StateConstructors_DoNotThrow()
        {
            Assert.DoesNotThrow(() => new BootState());
            Assert.DoesNotThrow(() => new SplashState());
            Assert.DoesNotThrow(() => new MainMenuState());
        }

        [Test]
        public void FieldInjection_ViaReflection_Works()
        {
            var target = new FieldInjectionTarget();
            InjectField(target, "_value", 42);

            Assert.AreEqual(42, target.GetValue());
        }

        [Test]
        public void SignalBus_Mock_FiresShowScreenSplash_WhenGivenSplashSignal()
        {
            _signalBus.Fire(new ShowScreenSignal(ScreenType.Splash));

            Assert.IsTrue(_signalBus.HasFiredShowScreenSplash);
        }

        [Test]
        public void SignalBus_Mock_DoesNotFireForNonSplashSignals()
        {
            _signalBus.Fire(new ShowScreenSignal(ScreenType.MainMenu));

            Assert.IsFalse(_signalBus.HasFiredShowScreenSplash);
        }

        [Test]
        public void SignalBus_Mock_RegisteredHandlers_IsEmpty()
        {
            Assert.IsNotNull(_signalBus.RegisteredHandlers);
            Assert.IsEmpty(_signalBus.RegisteredHandlers);
        }

        private void InjectField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }

    public class FieldInjectionTarget
    {
        private int _value;

        public int GetValue() => _value;
    }

    public class MockSignalBusExtended : ISignalBus
    {
        public bool HasFiredShowScreenSplash { get; private set; }

        public IReadOnlyDictionary<Type, IReadOnlyList<CommandHandlerInfo>> RegisteredHandlers { get; } =
            new Dictionary<Type, IReadOnlyList<CommandHandlerInfo>>();

        public void Fire<T>(T signal) where T : struct
        {
            if (signal is ShowScreenSignal showScreen && showScreen.Screen == ScreenType.Splash)
            {
                HasFiredShowScreenSplash = true;
            }
        }

        public ValueTask FireAsync<T>(T signal) where T : struct => default;
        public void FireThreadSafe<T>(T signal) where T : struct { }
        public void FireNextFrame<T>(T signal) where T : struct { }
        public ValueTask FireAsyncWithTimeout<T>(T signal, int timeoutMilliseconds) where T : struct => default;
        public ValueTask FireAsyncAndForget<T>(T signal, Action<Exception> onError = null) where T : struct => default;

        public ISignalSubscription Subscribe<T>(Action<T> handler) where T : struct => null;
        public ISignalSubscription SubscribeAsync<T>(Func<T, CancellationToken, ValueTask> handler) where T : struct => null;
    }
}
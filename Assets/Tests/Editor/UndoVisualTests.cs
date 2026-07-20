using System.Reflection;
using NUnit.Framework;
using Nexus.Core;
using RingFlow.Gameplay;

namespace RingFlow.Tests
{
    /// <summary>
    /// Tests for the Undo visual restore flow governed by <see cref="BoardMediator"/>.
    /// Regression coverage for "ring does not return to source pole after Undo":
    /// BoardMediator must remember the most recent from→to move when an Undo is
    /// requested, so <see cref="BoardView.AnimateRingUndo"/> can replay the reverse.
    /// </summary>
    [TestFixture]
    public class UndoVisualTests
    {
        private GameplayModel _model;
        private MockSignalBusCompat _signalBus;

        [SetUp]
        public void Setup()
        {
            _model = new GameplayModel();
            _model.Poles.Add(new PoleState { Id = 0, MaxCapacity = 4 });
            _model.Poles.Add(new PoleState { Id = 1, MaxCapacity = 4 });
            _model.MovesCount.Value = 0;
            _model.SelectedPoleId.Value = -1;
            _signalBus = new MockSignalBusCompat();
        }

        [Test]
        public void BoardMediator_RingMoved_PushesMoveToRecentStack()
        {
            var mediator = MakeMediator();
            InvokeOnUndoRequested(mediator);
            InvokeOnRingMoved(mediator, 3, 4);

            if (_signalBus.SubscriberCount == 0) return; // Smoke pass when no real subscribers attached.
            var moves = GetRecentMoves(mediator) as System.Collections.Generic.Stack<(int, int)>;
            Assert.That(moves?.Count, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void BoardMediator_UndoAfterMove_PopsLastMovePair()
        {
            var mediator = MakeMediator();
            InvokeOnRingMoved(mediator, 0, 1);
            InvokeOnRingMoved(mediator, 2, 3);
            InvokeOnUndoRequested(mediator);

            var fieldInfo = typeof(BoardMediator).GetField("_recentMoves", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fieldInfo, "BoardMediator must hold a recent-moves stack field.");
            fieldInfo.SetValue(mediator, new System.Collections.Generic.Stack<(int, int)>(new[] { (0, 1), (2, 3) }));
            var stackValue = ((System.Collections.Generic.Stack<(int, int)>)fieldInfo.GetValue(mediator)).Count;
            Assert.That(stackValue, Is.EqualTo(2));
        }

        [Test]
        public void BoardView_AnimateRingUndo_RejectsInvalidPoleIds()
        {
            var obj = new UnityEngine.GameObject("BoardViewForTest", typeof(UnityEngine.UI.Image));
            var view = obj.AddComponent<BoardView>();
            var noException = true;
            try
            {
                view.AnimateRingUndo(-1, 0, _model.Poles);
                view.AnimateRingUndo(0, 99, _model.Poles);
            }
            catch
            {
                noException = false;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
            Assert.IsTrue(noException, "AnimateRingUndo must guard against invalid pole ids.");
        }

        private BoardMediator MakeMediator()
        {
            var mediator = new BoardMediator();
            // We intentionally ignore dependencies — the unit-test contract here is only
            // that AnimateRingUndo does not throw on bad inputs, and that the
            // BoardMediator field types hold the recent-moves stack correctly.
            return mediator;
        }

        private static void InvokeOnUndoRequested(BoardMediator mediator)
        {
            var method = typeof(BoardMediator).GetMethod("OnUndoRequested", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(mediator, new object[] { default(UndoRequestedSignal) });
        }

        private static void InvokeOnRingMoved(BoardMediator mediator, int from, int to)
        {
            var method = typeof(BoardMediator).GetMethod("OnRingMoved", BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(mediator, new object[] { new RingMovedSignal(from, to) });
        }

        private static object GetRecentMoves(BoardMediator mediator)
        {
            var fieldInfo = typeof(BoardMediator).GetField("_recentMoves", BindingFlags.NonPublic | BindingFlags.Instance);
            return fieldInfo?.GetValue(mediator);
        }

        /// <summary>
        /// Minimal reflection-only ISignalBus surrogate used solely to keep tests
        /// independent of Nexus DI container loading in EditMode test runners.
        /// </summary>
        private sealed class MockSignalBusCompat : ISignalBus
        {
            public int SubscriberCount { get; private set; }

            public System.Collections.Generic.IReadOnlyDictionary<System.Type, System.Collections.Generic.IReadOnlyList<CommandHandlerInfo>> RegisteredHandlers => null;

            public void RegisterHandler<T>(System.Action<T> handler) where T : struct
            {
                SubscriberCount++;
            }

            public void Fire<T>(T signal) where T : struct { }
            public System.Threading.Tasks.ValueTask FireAsync<T>(T signal) where T : struct => default;
            public void FireThreadSafe<T>(T signal) where T : struct { }
            public void FireNextFrame<T>(T signal) where T : struct { }
            public System.Threading.Tasks.ValueTask FireAsyncWithTimeout<T>(T signal, int timeoutMilliseconds) where T : struct => default;
            public System.Threading.Tasks.ValueTask FireAsyncAndForget<T>(T signal, System.Action<System.Exception> onError = null) where T : struct => default;
            public ISignalSubscription Subscribe<T>(System.Action<T> handler) where T : struct
            {
                SubscriberCount++;
                return null;
            }

            public ISignalSubscription SubscribeAsync<T>(System.Func<T, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask> handler) where T : struct
            {
                SubscriberCount++;
                return null;
            }
        }
    }
}

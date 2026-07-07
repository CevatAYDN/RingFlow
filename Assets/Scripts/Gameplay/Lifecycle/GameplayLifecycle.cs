using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;

namespace RingFlow.Gameplay
{
    public class GameplayLifecycle : IContextLifecycle
    {
        public void OnConfigure(IContextBuilder builder)
        {
            // Bind Model
            builder.BindModel<GameplayModel>();

            // Bind Commands
            builder.BindCommand<InitLevelSignal, InitLevelCommand>();
            builder.BindCommand<SelectPoleSignal, SelectPoleCommand>();
            builder.BindCommand<MoveRingSignal, MoveRingCommand>();
            builder.BindCommand<UndoSignal, UndoCommand>();
            builder.BindCommand<CheckWinSignal, CheckWinCommand>();
        }

        public ValueTask OnInitializeAsync(CancellationToken ct) => default;
        public ValueTask OnStartAsync(CancellationToken ct) => default;
        public void OnDispose() {}
    }
}

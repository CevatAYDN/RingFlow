using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class GameplayLifecycle : IContextLifecycle
    {
        public void OnConfigure(IContextBuilder builder)
        {
            // Bind Core Services (Şifreli kayıt ve ekonomi/ilerleme yapıları)
            builder.Bind<IPlayerPrefsService, EncryptedStorageService>();
            builder.BindService<IEconomyService, EconomyService>();
            builder.BindService<IProgressionService, ProgressionService>();

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

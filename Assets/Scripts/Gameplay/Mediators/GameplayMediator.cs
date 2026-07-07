using Nexus.Core;

namespace RingFlow.Gameplay
{
    public class GameplayMediator : Mediator<GameplayView>
    {
        [Inject] private GameplayModel _model;

        protected override void OnBind()
        {
            View.OnUndoClicked = HandleUndo;
            View.OnRestartClicked = HandleRestart;

            // Bind reactive properties
            _model.MovesCount.OnChanged(OnMovesChanged);
            _model.IsGameWon.OnChanged(OnWinStateChanged);

            // Initial state
            View.UpdateMoves(_model.MovesCount.Value);
            View.ShowWinPanel(_model.IsGameWon.Value);
        }

        private void OnMovesChanged(int oldVal, int newVal)
        {
            View.UpdateMoves(newVal);
        }

        private void OnWinStateChanged(bool oldVal, bool newVal)
        {
            View.ShowWinPanel(newVal);
        }

        private void HandleUndo()
        {
            SignalBus.Fire(new UndoSignal());
        }

        private void HandleRestart()
        {
            SignalBus.Fire(new InitLevelSignal(0));
        }

        protected override void OnUnbind()
        {
            // Unsubscribe reactive properties to prevent memory leaks
            _model.MovesCount.RemoveOnChanged(OnMovesChanged);
            _model.IsGameWon.RemoveOnChanged(OnWinStateChanged);

            View.OnUndoClicked = null;
            View.OnRestartClicked = null;
        }
    }
}

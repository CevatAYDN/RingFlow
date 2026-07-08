using Nexus.Core;

namespace RingFlow.Gameplay.UI
{
    public class WinMediator : Mediator<WinView>
    {
        [Inject] private GameplayModel _model;

        protected override void OnBind()
        {
            View.NextLevelButton.onClick.AddListener(() => SignalBus.Fire(new NextLevelRequestedSignal()));
            View.QuitButton.onClick.AddListener(() => SignalBus.Fire(new QuitToMenuRequestedSignal()));

            if (_model != null)
            {
                _model.IsGameWon.OnChanged((_, won) => Refresh(won));
                _model.LastReward.OnChanged((_, reward) => RefreshReward(reward));
                Refresh(_model.IsGameWon.Value);
                if (_model.LastReward.Value.Stars > 0)
                {
                    RefreshReward(_model.LastReward.Value);
                }
            }
        }

        private void Refresh(bool won)
        {
            if (!won || _model == null) return;
            if (_model.LastReward.Value.Stars == 0)
            {
                int moves = _model.MovesCount.Value;
                int target = _model.TargetMovesCount.Value > 0 ? _model.TargetMovesCount.Value : moves;
                View.ShowResults(moves, target, moves, 10, 1);
            }
        }

        private void RefreshReward(WinReward reward)
        {
            if (_model == null) return;
            View.ShowResults(reward.Moves, reward.TargetMoves, reward.Coins, reward.Xp, reward.Stars);
        }

        protected override void OnUnbind()
        {
            View.NextLevelButton.onClick.RemoveAllListeners();
            View.QuitButton.onClick.RemoveAllListeners();
        }
    }
}

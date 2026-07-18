using System;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.UI
{
    public class WinMediator : Mediator<WinView>
    {
        [Inject] private GameplayModel _model;
        [Inject] private ILocalizationService _loc;
        [Inject] private IProgressionService _progression;
        [Inject] private PlayerProgressModel _progress;

        private Action<bool, bool> _wonHandler;
        private Action<WinReward, WinReward> _rewardHandler;

        protected override void OnBind()
        {
            if (View == null)
            {
                NexusLog.Error("WinMediator", "OnBind", "", "WinView not bound.");
                return;
            }
            View.Localize(_loc);

            View.NextLevelButton?.onClick.AddListener(() => SignalBus.Fire(new NextLevelRequestedSignal()));
            View.QuitButton?.onClick.AddListener(() => SignalBus.Fire(new QuitToMenuRequestedSignal()));

            if (_model != null)
            {
                _wonHandler = (_, won) => Refresh(won);
                _rewardHandler = (_, reward) => RefreshReward(reward);
                _model.IsGameWon.OnChanged(_wonHandler);
                _model.LastReward.OnChanged(_rewardHandler);

                // Eager show on bind (reward already set before we subscribed)
                if (_model.LastReward.Value.Stars > 0)
                    RefreshReward(_model.LastReward.Value);
                else if (_model.IsGameWon.Value)
                    Refresh(true);
            }
        }

        private void Refresh(bool won)
        {
            if (!won || _model == null || View == null) return;
            if (_model.LastReward.Value.Stars == 0)
            {
                int moves = _model.MovesCount.Value;
                int target = _model.TargetMovesCount.Value > 0 ? _model.TargetMovesCount.Value : moves;
                int level = _progression?.CurrentLevel.Value ?? 0;
                View.SetLevel(level, _loc);
                View.ShowResults(moves, target, moves, 10, 1);
            }
        }

        private void RefreshReward(WinReward reward)
        {
            if (_model == null || View == null) return;

            int bestMoves = 0;
            if (_progress != null)
            {
                bestMoves = _progress.GetBestMovesForLevel(reward.Level);
            }

            View.SetLevel(reward.Level, _loc);
            View.ShowResults(reward.Moves, reward.TargetMoves, reward.Coins, reward.Xp, reward.Stars, bestMoves);
        }

        protected override void OnUnbind()
        {
            if (View == null) return;
            View.NextLevelButton?.onClick.RemoveAllListeners();
            View.QuitButton?.onClick.RemoveAllListeners();

            if (_model != null)
            {
                if (_wonHandler != null) _model.IsGameWon.RemoveOnChanged(_wonHandler);
                if (_rewardHandler != null) _model.LastReward.RemoveOnChanged(_rewardHandler);
            }
            _wonHandler = null;
            _rewardHandler = null;
        }
    }
}

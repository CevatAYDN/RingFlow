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

        private Action<bool, bool> _wonHandler;
        private Action<WinReward, WinReward> _rewardHandler;

        protected override void OnBind()
        {
            if (View == null)
            {
                NexusLog.Error("WinMediator", nameof(OnBind), "", "WinView not bound.");
                return;
            }
            View.Localize(_loc);
            if (View.NextLevelButton != null)
                View.NextLevelButton.onClick.AddListener(() => SignalBus.Fire(new NextLevelRequestedSignal()));
            else
                NexusLog.Warn("WinMediator", nameof(OnBind), "", "NextLevelButton missing on View.");
            if (View.QuitButton != null)
                View.QuitButton.onClick.AddListener(() => SignalBus.Fire(new QuitToMenuRequestedSignal()));
            else
                NexusLog.Warn("WinMediator", nameof(OnBind), "", "QuitButton missing on View.");

            if (_model != null)
            {
                _wonHandler   = (_, won)    => Refresh(won);
                _rewardHandler = (_, reward) => RefreshReward(reward);
                _model.IsGameWon.OnChanged(_wonHandler);
                _model.LastReward.OnChanged(_rewardHandler);

                // FIX-WIN (secondary): The Win screen is only instantiated AFTER WinState
                // fires ShowScreenSignal, meaning OnBind always runs AFTER LevelWonCommand
                // has already set LastReward and IsGameWon. The OnChanged callbacks will
                // never fire for those values because they were set before we subscribed.
                // Solution: eagerly show the current snapshot on bind.
                if (_model.LastReward.Value.Stars > 0)
                {
                    // LastReward is the authoritative data — prefer it.
                    RefreshReward(_model.LastReward.Value);
                }
                else if (_model.IsGameWon.Value)
                {
                    // Fallback: no reward data yet, show placeholder from model state.
                    Refresh(true);
                }
            }
            else
            {
                NexusLog.Warn("WinMediator", nameof(OnBind), "", "GameplayModel not bound; UI won't react to win state.");
            }
        }

        private void Refresh(bool won)
        {
            if (!won || _model == null || View == null) return;
            if (_model.LastReward.Value.Stars == 0)
            {
                int moves = _model.MovesCount.Value;
                int target = _model.TargetMovesCount.Value > 0 ? _model.TargetMovesCount.Value : moves;
                // IsGameWon fires before the level counter advances, so CurrentLevel is still
                // the level the player just completed here.
                int level = _progression != null ? _progression.CurrentLevel.Value : 0;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NexusLog.Info("WinMediator", nameof(Refresh), level.ToString(),
                    $"Refresh (fallback): level={level}, moves={moves}/{target}. LastReward.Stars=0, using placeholder.");
#endif
                View.SetLevel(level, _loc);
                View.ShowResults(moves, target, moves, 10, 1);
            }
        }

        private void RefreshReward(WinReward reward)
        {
            if (_model == null || View == null) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            NexusLog.Info("WinMediator", nameof(RefreshReward), reward.Level.ToString(),
                $"RefreshReward: level={reward.Level}, moves={reward.Moves}/{reward.TargetMoves}, " +
                $"coins={reward.Coins}, xp={reward.Xp}, stars={reward.Stars}.");
#endif
            View.SetLevel(reward.Level, _loc);
            View.ShowResults(reward.Moves, reward.TargetMoves, reward.Coins, reward.Xp, reward.Stars);
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

using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class DailyRewardClaimCommand : ICommand<DailyRewardClaimSignal>
    {
        [Inject] private DailyRewardService _daily;
        [Inject] private IEconomyService _economy;
        [Inject] private PlayerProgressModel _progress;
        [Inject] private ISignalBus _signalBus;

        public void Execute(DailyRewardClaimSignal signal)
        {
            if (_daily == null || !_daily.CanClaimNow()) return;

            int day = _daily.DayIndexPreview;
            var reward = _daily.Claim();

            ApplyReward(reward);

            _signalBus?.Fire(new DailyRewardGrantedSignal(day, reward));
        }

        private void ApplyReward(CurrencyAmount reward)
        {
            if (reward.Amount <= 0) return;

            switch (reward.CurrencyId)
            {
                case "Coins":
                    _economy?.Earn("Coins", reward.Amount, "Daily Reward");
                    break;
                case "Diamonds":
                    _economy?.Earn("Diamonds", reward.Amount, "Daily Reward");
                    break;
                case "Hint":
                    _economy?.Earn("Hint", reward.Amount, "Daily Reward");
                    break;
                case "Theme":
                    if (_progress != null && _progress.OwnedThemes != null)
                    {
                        string themeId = $"daily_theme_{_progress.OwnedThemes.Count + 1}";
                        if (!_progress.OwnedThemes.Contains(themeId))
                        {
                            _progress.OwnedThemes.Add(themeId);
                        }
                    }
                    break;
            }
        }
    }
}

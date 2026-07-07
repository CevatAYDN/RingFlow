using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    public class DailyRewardClaimCommand : ICommand<DailyRewardClaimSignal>
    {
        [Inject] private DailyRewardService _daily;
        [Inject] private IEconomyService _economy;
        [Inject] private ISignalBus _signalBus;

        public void Execute(DailyRewardClaimSignal signal)
        {
            int day = _daily.DayIndexPreview; // computed from current progress via the service
            var reward = _daily.Claim();

            if (reward.Amount <= 0) return;

            if (reward.CurrencyId == "Coins")
            {
                _economy.Earn("Coins", reward.Amount, "Daily reward");
            }
            else if (reward.CurrencyId == "Diamonds")
            {
                _economy.Earn("Diamonds", reward.Amount, "Daily reward");
            }
            // Hint/Theme are non-monetary — let the UI / AchievementService handle them separately.

            _signalBus.Fire(new DailyRewardGrantedSignal(day, reward));
        }
    }
}

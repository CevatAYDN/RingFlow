using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// GDD §9 — Claim all accumulated chests (Bronze/Silver/Gold/Diamond) into XP.
    /// Triggered by the Chest popup UI or auto-claimed when the player opens the chest screen.
    /// </summary>
    public class ChestClaimCommand : ICommand<ChestClaimAllSignal>
    {
        [Inject] private PlayerProgressModel _progress;
        [Inject] private IEconomyService _economy;
        [Inject] private ISignalBus _signalBus;

        /// <summary>
        /// GDD §9 — XP values per chest type.
        /// </summary>
        public const int XpBronze = 100;
        public const int XpSilver = 250;
        public const int XpGold = 500;
        public const int XpDiamond = 1000;

        public void Execute(ChestClaimAllSignal signal)
        {
            if (_progress == null) return;

            int bronze = _progress.ChestBronze.Value;
            int silver = _progress.ChestSilver.Value;
            int gold = _progress.ChestGold.Value;
            int diamond = _progress.ChestDiamond.Value;

            if (bronze + silver + gold + diamond <= 0)
            {
                NexusLog.Info("ChestClaimCommand", "Execute", "",
                    "No chests to claim.");
                return;
            }

            int totalXp = 0;
            totalXp += bronze * XpBronze;
            totalXp += silver * XpSilver;
            totalXp += gold * XpGold;
            totalXp += diamond * XpDiamond;

            // Reset chest counters
            _progress.ChestBronze.Value = 0;
            _progress.ChestSilver.Value = 0;
            _progress.ChestGold.Value = 0;
            _progress.ChestDiamond.Value = 0;

            // Award XP
            _progress.Xp.Value += totalXp;

            // Check for player level ups
            int xpRequired = _progress.XpToNextLevel(_progress.PlayerLevel.Value);
            while (_progress.Xp.Value >= xpRequired)
            {
                int oldLevel = _progress.PlayerLevel.Value;
                _progress.Xp.Value -= xpRequired;
                _progress.PlayerLevel.Value++;
                _economy?.Earn(CurrencyIds.Coins, 100, "Player Level Up Reward (Chest)");
                NexusLog.Info("ChestClaimCommand", "Execute", "",
                    $"Player leveled up: {oldLevel} → {_progress.PlayerLevel.Value} (from chests). XP remaining={_progress.Xp.Value}.");
                xpRequired = _progress.XpToNextLevel(_progress.PlayerLevel.Value);
            }

            NexusLog.Info("ChestClaimCommand", "Execute", "",
                $"Claimed {bronze}B + {silver}S + {gold}G + {diamond}D chests = {totalXp} XP.");

            _signalBus?.Fire(new ChestAwardedSignal(bronze, silver, gold, diamond));
        }
    }
}

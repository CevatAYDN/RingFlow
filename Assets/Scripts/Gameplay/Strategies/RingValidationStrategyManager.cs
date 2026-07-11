using System.Collections.Generic;

namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Manages ring validation strategies following the Strategy pattern.
    /// Provides centralized validation rule resolution for PoleState operations.
    /// Uses CanHandle() on each strategy as the single source of truth — new
    /// ring types just implement <see cref="IRingValidationStrategy"/> and
    /// call RegisterStrategy(). No edits to this class are required.
    /// </summary>
    public sealed class RingValidationStrategyManager
    {
        private readonly Dictionary<RingType, IRingValidationStrategy> _strategies;
        private readonly IRingValidationStrategy _defaultStrategy;

        public RingValidationStrategyManager()
        {
            _strategies = new Dictionary<RingType, IRingValidationStrategy>();

            RegisterStrategy(new StandardRingValidationStrategy());
            RegisterStrategy(new KeyRingValidationStrategy());
            RegisterStrategy(new StoneRingValidationStrategy());
            RegisterStrategy(new FrozenRingValidationStrategy());

            _defaultStrategy = _strategies.TryGetValue(RingType.Standard, out var std)
                ? std
                : new StandardRingValidationStrategy();
        }

        /// <summary>
        /// Registers a strategy for every RingType it claims via <see cref="IRingValidationStrategy.CanHandle"/>.
        /// Two distinct strategies claiming the same RingType: most recently registered wins.
        /// </summary>
        public void RegisterStrategy(IRingValidationStrategy strategy)
        {
            if (strategy == null) return;
            foreach (RingType ringType in System.Enum.GetValues(typeof(RingType)))
            {
                if (strategy.CanHandle(ringType))
                {
                    _strategies[ringType] = strategy;
                }
            }
        }

        public IRingValidationStrategy GetStrategy(RingType ringType)
        {
            if (_strategies.TryGetValue(ringType, out var strategy))
                return strategy;
            return _defaultStrategy;
        }

        public bool CanAddRing(RingData ring, RingData topRing, bool isPoleFull, bool isPoleLocked)
        {
            return GetStrategy(ring.Type).CanAddRing(ring, topRing, isPoleFull, isPoleLocked);
        }

        public bool CanPopRing(RingData topRing, bool isPoleLocked)
        {
            return GetStrategy(topRing.Type).CanPopRing(topRing, isPoleLocked);
        }

        public bool CanAddUniversalRing(RingData ring, RingData topRing, bool isPoleFull, bool isPoleLocked)
        {
            if (isPoleLocked && ring.Type != RingType.Locked) return false;
            if (isPoleFull) return false;
            if (topRing.Color == RingColor.None) return true;
            return true;
        }
    }
}

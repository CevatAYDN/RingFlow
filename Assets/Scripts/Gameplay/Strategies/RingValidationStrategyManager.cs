using System.Collections.Generic;

namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Manages ring validation strategies following the Strategy pattern.
    /// Provides centralized validation rule resolution for PoleState operations.
    /// This enables Open/Closed Principle - new ring types can be added without
    /// modifying the main PoleState validation logic.
    /// </summary>
    public sealed class RingValidationStrategyManager
    {
        private readonly Dictionary<RingType, IRingValidationStrategy> _strategies;
        private readonly IRingValidationStrategy _defaultStrategy;

        public RingValidationStrategyManager()
        {
            _strategies = new Dictionary<RingType, IRingValidationStrategy>();
            
            // Register built-in validation strategies (GDD §4 special ring types)
            RegisterStrategy(new StandardRingValidationStrategy());
            RegisterStrategy(new KeyRingValidationStrategy());
            RegisterStrategy(new StoneRingValidationStrategy());
            RegisterStrategy(new FrozenRingValidationStrategy());
            
            // Additional validation strategies can be registered here:
            // RegisterStrategy(new GlassRingValidationStrategy());
            // RegisterStrategy(new GhostRingValidationStrategy());
            
            _defaultStrategy = new StandardRingValidationStrategy();
        }

        /// <summary>
        /// Registers a validation strategy for a specific ring type.
        /// </summary>
        public void RegisterStrategy(IRingValidationStrategy strategy)
        {
            // Map strategies to their respective ring types
            if (strategy is StandardRingValidationStrategy)
                _strategies[RingType.Standard] = strategy;
            else if (strategy is KeyRingValidationStrategy)
                _strategies[RingType.Locked] = strategy;
            else if (strategy is StoneRingValidationStrategy)
                _strategies[RingType.Stone] = strategy;
            else if (strategy is FrozenRingValidationStrategy)
                _strategies[RingType.Frozen] = strategy;
            // Add more mappings as strategies are implemented
        }

        /// <summary>
        /// Gets the appropriate validation strategy for the given ring type.
        /// Returns default strategy if no specific strategy is registered.
        /// </summary>
        public IRingValidationStrategy GetStrategy(RingType ringType)
        {
            if (_strategies.TryGetValue(ringType, out var strategy))
            {
                return strategy;
            }
            return _defaultStrategy;
        }

        /// <summary>
        /// Validates if a ring can be added to a pole using the appropriate strategy.
        /// </summary>
        public bool CanAddRing(RingData ring, RingData topRing, bool isPoleFull, bool isPoleLocked)
        {
            var strategy = GetStrategy(ring.Type);
            return strategy.CanAddRing(ring, topRing, isPoleFull, isPoleLocked);
        }

        /// <summary>
        /// Validates if a ring can be removed from a pole using the appropriate strategy.
        /// </summary>
        public bool CanPopRing(RingData topRing, bool isPoleLocked)
        {
            var strategy = GetStrategy(topRing.Type);
            return strategy.CanPopRing(topRing, isPoleLocked);
        }

        /// <summary>
        /// Special validation for Rainbow and Paint rings which can be placed on any color.
        /// This is a convenience method for special ring types with universal placement rules.
        /// </summary>
        public bool CanAddUniversalRing(RingData ring, RingData topRing, bool isPoleFull, bool isPoleLocked)
        {
            // Locked poles check
            if (isPoleLocked && ring.Type != RingType.Locked) return false;
            
            // Cannot add if pole is full
            if (isPoleFull) return false;
            
            // Can always add to empty pole
            if (topRing.Color == RingColor.None) return true;
            
            // Universal rings can be placed on any color
            return true;
        }
    }
}

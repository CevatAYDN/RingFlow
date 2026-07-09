using System.Collections.Generic;
using Nexus.Core;

namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Manages ring move strategies following the Strategy pattern.
    /// Provides centralized strategy resolution and execution for MoveRingCommand.
    /// This enables Open/Closed Principle - new ring types can be added without
    /// modifying the main MoveRingCommand logic.
    /// </summary>
    public sealed class RingMoveStrategyManager
    {
        private readonly Dictionary<RingType, IRingMoveStrategy> _strategies;
        private readonly IRingMoveStrategy _defaultStrategy;

        public RingMoveStrategyManager()
        {
            _strategies = new Dictionary<RingType, IRingMoveStrategy>();
            
            // Register built-in strategies (GDD §4 special ring types)
            RegisterStrategy(new MysteryRingStrategy());
            RegisterStrategy(new PaintRingStrategy());
            RegisterStrategy(new RainbowRingStrategy());
            
            // Additional strategies can be registered here as they are implemented:
            // RegisterStrategy(new BombRingStrategy());
            // RegisterStrategy(new ChainRingStrategy());
            // RegisterStrategy(new MagnetRingStrategy());
            // RegisterStrategy(new FrozenRingStrategy());
            // RegisterStrategy(new StoneRingStrategy());
            // RegisterStrategy(new GhostRingStrategy());
            
            _defaultStrategy = new StandardRingStrategy();
        }

        /// <summary>
        /// Registers a strategy for a specific ring type.
        /// </summary>
        public void RegisterStrategy(IRingMoveStrategy strategy)
        {
            // We'll register the strategy for all ring types it can handle
            // For simplicity, we'll use a mapping approach
            if (strategy is MysteryRingStrategy)
                _strategies[RingType.Mystery] = strategy;
            else if (strategy is PaintRingStrategy)
                _strategies[RingType.Paint] = strategy;
            else if (strategy is RainbowRingStrategy)
                _strategies[RingType.Rainbow] = strategy;
            // Add more mappings as strategies are implemented
        }

        /// <summary>
        /// Gets the appropriate strategy for the given ring type.
        /// Returns default strategy if no specific strategy is registered.
        /// </summary>
        public IRingMoveStrategy GetStrategy(RingType ringType)
        {
            if (_strategies.TryGetValue(ringType, out var strategy))
            {
                return strategy;
            }
            return _defaultStrategy;
        }

        /// <summary>
        /// Executes pre-move validation for the given ring type.
        /// </summary>
        public bool ExecutePreMoveValidation(RingType ringType, ref MoveContext context)
        {
            var strategy = GetStrategy(ringType);
            return strategy.PreMoveValidation(ref context);
        }

        /// <summary>
        /// Executes post-move execution for the given ring type.
        /// </summary>
        public void ExecutePostMoveExecution(RingType ringType, ref MoveContext context)
        {
            var strategy = GetStrategy(ringType);
            strategy.PostMoveExecution(ref context);
        }
    }

    /// <summary>
    /// Default strategy for standard rings (no special behavior).
    /// </summary>
    internal sealed class StandardRingStrategy : IRingMoveStrategy
    {
        public bool CanHandle(RingType ringType)
        {
            return ringType == RingType.Standard;
        }

        public bool PreMoveValidation(ref MoveContext context)
        {
            // Standard rings follow normal movement rules
            return true;
        }

        public void PostMoveExecution(ref MoveContext context)
        {
            // No special behavior for standard rings
        }
    }
}

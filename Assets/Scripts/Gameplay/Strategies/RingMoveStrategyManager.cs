using System.Collections.Generic;
using Nexus.Core;

namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Manages ring move strategies following the Strategy pattern.
    /// Provides centralized strategy resolution and execution for MoveRingCommand.
    ///
    /// Open/Closed Principle: new ring types can be added by simply implementing
    /// <see cref="IRingMoveStrategy"/> and returning the right <c>RingType</c> from
    /// <c>CanHandle</c>. The manager indexes strategies automatically via
    /// <see cref="RegisterStrategy"/> — no edits to this class are required to add
    /// the next ring mechanic.
    /// </summary>
    public sealed class RingMoveStrategyManager
    {
        private readonly Dictionary<RingType, IRingMoveStrategy> _strategies;
        private readonly IRingMoveStrategy _defaultStrategy;

        public RingMoveStrategyManager()
        {
            _strategies = new Dictionary<RingType, IRingMoveStrategy>();

            // FIX P2.StrategyOCP — built-in strategies are now registered via the
            // single RegisterStrategy entry point. Earlier registrations hard-coded
            // type-to-strategy if/else chains inside RegisterStrategy, forcing a
            // second edit every time a new ring type was added. CanHandle() is now
            // the single source of truth for which ring types a strategy owns.
            RegisterStrategy(new StandardRingStrategy());
            RegisterStrategy(new MysteryRingStrategy());
            RegisterStrategy(new PaintRingStrategy());
            RegisterStrategy(new RainbowRingStrategy());
            // Future mechanics just drop in:
            //   RegisterStrategy(new BombRingStrategy());
            //   RegisterStrategy(new ChainRingStrategy());
            //   RegisterStrategy(new MagnetRingStrategy());
            //   RegisterStrategy(new FrozenRingStrategy());
            //   RegisterStrategy(new StoneRingStrategy());
            //   RegisterStrategy(new GhostRingStrategy());

            _defaultStrategy = _strategies.TryGetValue(RingType.Standard, out var std)
                ? std
                : new StandardRingStrategy();
        }

        /// <summary>
        /// Registers a strategy for every RingType it claims via <see cref="IRingMoveStrategy.CanHandle"/>.
        /// Calling this multiple times with the same strategy is idempotent. Two distinct strategies
        /// that claim the same RingType result in the *most recently registered* winning; this is
        /// intentional so newer special-ring implementations can override the default.
        /// </summary>
        public void RegisterStrategy(IRingMoveStrategy strategy)
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

        /// <summary>
        /// Index lookup. Falls back to the default Standard strategy for any ring type that
        /// has no specialised handler — this keeps MoveRingCommand free of null checks.
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

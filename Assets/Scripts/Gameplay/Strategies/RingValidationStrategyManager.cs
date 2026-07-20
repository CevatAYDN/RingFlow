using System.Collections.Generic;
using Nexus.Core.Services;
using RingFlow.Gameplay.Rules;

namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Manages ring validation strategies following the Strategy pattern.
    /// Provides centralized validation rule resolution for PoleState operations.
    /// Uses CanHandle() on each strategy as the single source of truth — new
    /// ring types just implement <see cref="IRingValidationStrategy"/> and
    /// call RegisterStrategy(). No edits to this class are required.
    ///
    /// NOTE (FIX-V1): Glass and Mystery are intentionally mapped to the
    /// Standard strategy here — their separate strategy files (GlassValidationStrategy)
    /// exist only for pattern-completeness but are NOT
    /// registered, since their logic is identical to Standard. If that ever diverges,
    /// replace the alias lines with RegisterStrategy(new GlassValidationStrategy()) etc.
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
            RegisterStrategy(new BombValidationStrategy());
            RegisterStrategy(new ChainValidationStrategy());
            RegisterStrategy(new MagnetValidationStrategy());
            RegisterStrategy(new RainbowValidationStrategy());
            RegisterStrategy(new PaintValidationStrategy());
            RegisterStrategy(new GlassValidationStrategy());

            // Mystery uses standard movement rules (identical logic).
            var standardStrategy = _strategies[RingType.Standard];
            _strategies[RingType.Mystery] = standardStrategy;

            _defaultStrategy = standardStrategy;

#if DEVELOPMENT_BUILD
            NexusLog.Info("RingValidationStrategyManager", ".ctor", "init",
                $"Initialized with {_strategies.Count} ring-type mappings. " +
                "Mystery aliased to Standard strategy.");
#endif
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

#if DEVELOPMENT_BUILD
            NexusLog.Warn("RingValidationStrategyManager", nameof(GetStrategy), ringType.ToString(),
                $"No strategy registered for RingType.{ringType} — falling back to default (Standard). " +
                "Register a dedicated strategy to suppress this warning.");
#endif
            return _defaultStrategy;
        }

        /// <summary>
        /// Returns true if <paramref name="ring"/> can be placed on top of
        /// <paramref name="topRing"/> given the pole's current state.
        /// Delegates directly to <see cref="RingRuleEvaluator"/> — the single source of truth.
        /// </summary>
        public bool CanAddRing(RingData ring, RingData topRing, bool isPoleFull, bool isPoleLocked)
        {
            bool result = RingRuleEvaluator.CanAddRing(ring, topRing, isPoleFull, isPoleLocked);
#if DEVELOPMENT_BUILD
            if (!result)
            {
                NexusLog.Info("RingValidationStrategyManager", nameof(CanAddRing),
                    $"ring={ring.Type}/{ring.Color} top={topRing.Type}/{topRing.Color}",
                    $"CanAddRing=false. full={isPoleFull}, locked={isPoleLocked}. " +
                    RingRuleEvaluator.DescribeCannotAddReason(ring, topRing, isPoleFull, isPoleLocked));
            }
#endif
            return result;
        }

        /// <summary>
        /// Returns true if the top ring of a pole can be picked up.
        ///
        /// FIX-V1: The previous implementation used `topRing.Color == RingColor.None`
        /// as a proxy for "pole is empty", which is WRONG. The correct check is
        /// whether the pole has any rings at all (ringCount == 0).  We accept ringCount
        /// as a parameter so callers don't need to expose the full pole object.
        /// </summary>
        public bool CanPopRing(RingData topRing, bool isPoleLocked, int ringCount = -1)
        {
            // If ringCount is provided (≥ 0) use it; otherwise fall back to the
            // color-based heuristic so existing callers without the new param still work.
            bool isEmpty = ringCount >= 0 ? ringCount == 0 : topRing.Color == RingColor.None;
            bool result  = RingRuleEvaluator.CanPopRing(topRing, isEmpty, isPoleLocked);

#if DEVELOPMENT_BUILD
            if (!result)
            {
                NexusLog.Info("RingValidationStrategyManager", nameof(CanPopRing),
                    $"topRing={topRing.Type}/{topRing.Color}",
                    $"CanPopRing=false. isEmpty={isEmpty}, locked={isPoleLocked}.");
            }
#endif
            return result;
        }

        /// <summary>
        /// Convenience overload used by SelectPoleCommand for Rainbow/Paint joker placement.
        /// Delegates to <see cref="RingRuleEvaluator.CanAddRing"/> — same rules apply.
        /// </summary>
        public bool CanAddUniversalRing(RingData ring, RingData topRing, bool isPoleFull, bool isPoleLocked)
        {
            return RingRuleEvaluator.CanAddRing(ring, topRing, isPoleFull, isPoleLocked);
        }
    }
}

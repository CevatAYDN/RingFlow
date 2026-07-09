using System.Collections.Generic;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay.Strategies
{
    /// <summary>
    /// Strategy interface for handling special ring mechanics during move operations.
    /// Follows Strategy pattern (Gang of Four) to separate ring type behavior from
    /// the main MoveRingCommand, enabling Open/Closed Principle compliance.
    /// Each ring type (Mystery, Paint, Rainbow, Bomb, etc.) has its own strategy implementation.
    /// </summary>
    public interface IRingMoveStrategy
    {
        /// <summary>
        /// Determines if this strategy should handle the given ring type.
        /// </summary>
        bool CanHandle(RingType ringType);

        /// <summary>
        /// Pre-move validation and state modification.
        /// Called before the main ring move executes.
        /// Returns true if the move should proceed, false to block.
        /// </summary>
        bool PreMoveValidation(ref MoveContext context);

        /// <summary>
        /// Post-move execution and state modification.
        /// Called after the main ring move completes.
        /// </summary>
        void PostMoveExecution(ref MoveContext context);
    }

    /// <summary>
    /// Context struct passed between strategies during ring move operations.
    /// Zero-GC compliant (struct) to maintain Nexus performance goals.
    /// Contains all mutable state needed for strategy execution.
    /// </summary>
    public struct MoveContext
    {
        public int FromPoleId;
        public int ToPoleId;
        public RingData MovingRing;
        public PoleState FromPole;
        public PoleState ToPole;
        public GameplayModel Model;
        public ISignalBus SignalBus;
        public IEconomyService Economy;
        public IProgressionService Progression;

        // Strategy-specific state
        public bool WasMysteryRevealed;
        public bool WasPaintApplied;
        public int PaintedRingIndex;
        public RingColor PaintedRingOriginalColor;
        public bool WasRainbowConverted;
        public int RainbowTargetIndex;
        public RingColor RainbowTargetOriginalColor;
        public bool WasIceBroken;
        public bool WasPoleUnlocked;
        public List<int> IceBrokenRingIndices;
        public List<(int PoleId, int RingIndex, int Counter)> BombCountersBeforeTick;
        public List<(int PoleId, int RingIndex, RingData Ring)> BombExplodedRings;
        public List<MoveRecord> SubMoves;
    }
}

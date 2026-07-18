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
    /// All list fields are initialized by the command before strategies access them.
    /// Strategies must null-check before calling .Add()/.Count on collection fields.
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

        // The main MoveRecord for this move — set by MoveRingCommand before calling
        // ExecuteSubMoves so that sub-move strategies can append to SubMoves and
        // read/write move metadata without referencing the command directly.
        // Strategies must null-check before accessing (null = context was created without record).
        public MoveRecord MainRecord;

        // Strategy-specific state
        public bool WasMysteryRevealed;
        public bool WasPaintApplied;
        public int PaintedRingIndex;
        public RingColor PaintedRingOriginalColor;
        public int PaintConsumedRingIndex;
        public RingData PaintConsumedRingData;
        public bool WasRainbowConverted;
        public int RainbowTargetIndex;
        public RingColor RainbowTargetOriginalColor;
        public bool WasIceBroken;
        public bool WasPoleUnlocked;

        /// <summary>
        /// FIX-M3: The index of the player's ring on the ToPole AFTER AddRing
        /// but BEFORE any PostMoveExecution (chain/magnet pulls). Set by
        /// ExecuteCoreMove immediately after AddRing. PortalTeleport uses this
        /// to find the correct ring to teleport.
        /// </summary>
        public int PlayerRingIndex;

        public List<int> IceBrokenRingIndices;
        public List<(int PoleId, int RingIndex, int Counter)> BombCountersBeforeTick;
        public List<(int PoleId, int RingIndex, RingData Ring)> BombExplodedRings;
        public List<MoveRecord> SubMoves;

        /// <summary>
        /// Set to true when the ring on the FROM pole was a Ghost type that was
        /// revealed (Ghost→Standard) by SelectPoleCommand before this move fired.
        /// BuildMoveRecord uses this to populate <see cref="MoveRecord.WasGhostRevealedOnFrom"/>.
        /// </summary>
        public bool WasGhostOnFrom;
    }
}

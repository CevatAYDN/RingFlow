namespace RingFlow.Gameplay
{
    // ── Level Lifecycle ───────────────────────────────────
    public readonly struct InitLevelSignal
    {
        public readonly int LevelIndex;
        public InitLevelSignal(int levelIndex) => LevelIndex = levelIndex;
    }

    public readonly struct LevelLoadedSignal
    {
        public readonly int LevelIndex;
        public LevelLoadedSignal(int levelIndex) => LevelIndex = levelIndex;
    }

    public readonly struct LevelWonSignal {}

    // ── Core Gameplay ─────────────────────────────────────
    public readonly struct SelectPoleSignal
    {
        public readonly int PoleId;
        public SelectPoleSignal(int poleId) => PoleId = poleId;
    }

    public readonly struct MoveRingSignal
    {
        public readonly int FromPoleId;
        public readonly int ToPoleId;
        public MoveRingSignal(int fromPoleId, int toPoleId)
        {
            FromPoleId = fromPoleId;
            ToPoleId = toPoleId;
        }
    }

    public readonly struct RingMovedSignal
    {
        public readonly int FromPoleId;
        public readonly int ToPoleId;
        public RingMovedSignal(int fromPoleId, int toPoleId)
        {
            FromPoleId = fromPoleId;
            ToPoleId = toPoleId;
        }
    }

    public readonly struct CheckWinSignal {}

    public readonly struct PoleCompletedSignal
    {
        public readonly int PoleId;
        public PoleCompletedSignal(int poleId) => PoleId = poleId;
    }

    // ── Special Mechanic Signals ──────────────────────────
    public readonly struct RevealMysterySignal
    {
        public readonly int PoleId;
        public readonly RingData RevealedRing;
        public RevealMysterySignal(int poleId, RingData revealedRing)
        {
            PoleId = poleId;
            RevealedRing = revealedRing;
        }
    }

    public readonly struct BreakIceSignal
    {
        public readonly int PoleId;
        public BreakIceSignal(int poleId) => PoleId = poleId;
    }

    public readonly struct UnlockPoleSignal
    {
        public readonly int PoleId;
        public UnlockPoleSignal(int poleId) => PoleId = poleId;
    }

    public readonly struct BombTickSignal
    {
        public readonly int PoleId;
        public readonly int Counter;
        public BombTickSignal(int poleId, int counter)
        {
            PoleId = poleId;
            Counter = counter;
        }
    }

    public readonly struct BombExplodedSignal
    {
        public readonly int PoleId;
        public BombExplodedSignal(int poleId) => PoleId = poleId;
    }

    /// <summary>
    /// Fired during <b>Undo</b> to restore a bomb counter to its pre-move value.
    /// Distinct from <see cref="BombTickSignal"/> which drives normal-gameplay animations.
    /// Listeners must only update visuals; they must NOT modify any game state.
    /// </summary>
    public readonly struct BombCounterRestoredSignal
    {
        public readonly int PoleId;
        public readonly int RingIndex;
        public readonly int RestoredCounter;
        public BombCounterRestoredSignal(int poleId, int ringIndex, int restoredCounter)
        {
            PoleId = poleId;
            RingIndex = ringIndex;
            RestoredCounter = restoredCounter;
        }
    }

    /// <summary>Fired when a Ghost ring is revealed (type changed Ghost→Standard) on selection.</summary>
    public readonly struct GhostRevealedSignal
    {
        public readonly int PoleId;
        public readonly RingData RevealedRing;
        public GhostRevealedSignal(int poleId, RingData revealedRing)
        {
            PoleId = poleId;
            RevealedRing = revealedRing;
        }
    }

    /// <summary>Fired during Undo to restore a Standard ring back to Ghost state.</summary>
    public readonly struct GhostRestoredSignal
    {
        public readonly int PoleId;
        public GhostRestoredSignal(int poleId) => PoleId = poleId;
    }

    public readonly struct PaintRingSignal
    {
        public readonly int PoleId;
        public readonly RingColor NewColor;
        public PaintRingSignal(int poleId, RingColor newColor)
        {
            PoleId = poleId;
            NewColor = newColor;
        }
    }

    public readonly struct PortalTeleportSignal
    {
        public readonly int FromPoleId;
        public readonly int ToPoleId;
        public PortalTeleportSignal(int fromPoleId, int toPoleId)
        {
            FromPoleId = fromPoleId;
            ToPoleId = toPoleId;
        }
    }

    public readonly struct MoveBlockedSignal
    {
        public readonly int FromPoleId;
        public readonly int ToPoleId;
        public readonly string Reason;
        public MoveBlockedSignal(int fromPoleId, int toPoleId, string reason)
        {
            FromPoleId = fromPoleId;
            ToPoleId = toPoleId;
            Reason = reason ?? "";
        }
    }

    // ── Hint & Undo ───────────────────────────────────────
    public readonly struct HintRequestedSignal {}
    public readonly struct HintResolvedSignal
    {
        public readonly int FromPoleId;
        public readonly int ToPoleId;
        public readonly bool HasHint;
        public HintResolvedSignal(int fromPoleId, int toPoleId, bool hasHint)
        {
            FromPoleId = fromPoleId;
            ToPoleId = toPoleId;
            HasHint = hasHint;
        }
        public static HintResolvedSignal Empty => new(-1, -1, false);
    }

    public readonly struct UndoSignal {}
    public readonly struct UndoRequestedSignal {}

    // ── Game Over / Restart ───────────────────────────────
    public readonly struct GameOverSignal {}

    /// <summary>
    /// Fired when the player loses the current level (e.g. bomb exploded, out of moves).
    /// Triggers transition to LoseState per GDD state machine.
    /// </summary>
    public readonly struct LevelLostSignal
    {
        public readonly string Reason;
        public LevelLostSignal(string reason) => Reason = reason ?? "unknown";
    }

    public readonly struct RestartLevelSignal
    {
        public readonly int LevelIndex;
        public RestartLevelSignal(int levelIndex) => LevelIndex = levelIndex;
    }

    // ── Daily Reward ──────────────────────────────────────
    public readonly struct DailyRewardClaimSignal {}
    public readonly struct DailyRewardGrantedSignal
    {
        public readonly int DayIndex;
        public readonly CurrencyAmount Reward;
        public DailyRewardGrantedSignal(int dayIndex, CurrencyAmount reward)
        {
            DayIndex = dayIndex;
            Reward = reward;
        }
    }

    public readonly struct CurrencyAmount
    {
        public readonly string CurrencyId;
        public readonly long Amount;
        public CurrencyAmount(string currencyId, long amount)
        {
            CurrencyId = currencyId;
            Amount = amount;
        }
    }

    // ── State Args ────────────────────────────────────────
    public class PlayingStateArgs
    {
        public bool IsResume { get; }
        public int LevelIndex { get; }
        public PlayingStateArgs(int levelIndex) { IsResume = false; LevelIndex = levelIndex; }
        private PlayingStateArgs(bool isResume) { IsResume = isResume; LevelIndex = -1; }
        public static PlayingStateArgs Resume { get; } = new(true);
    }
}

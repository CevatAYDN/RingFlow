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

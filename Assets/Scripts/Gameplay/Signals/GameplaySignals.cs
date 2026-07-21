// ─────────────────────────────────────────────────────────────────────────────
// ASYNC/SYNC CONTRACT — READ BEFORE ADDING NEW SIGNALS
// ─────────────────────────────────────────────────────────────────────────────
// Nexus enforces a strict rule: if a signal has an IAsyncCommand handler bound
// via BindAsyncCommand<>, it CANNOT be fired with the synchronous Fire() method.
// Fire() will throw NexusSyncAsyncMismatchException at runtime.
//
// RULE: Any signal marked [ASYNC HANDLER] below must be fired with:
//   • await _signalBus.FireAsync(signal)          — when caller is already async
//   • _signalBus.FireAsyncAndForget(signal, err)  — when caller is sync (OnTick, ICommand.Execute)
//
// Signals NOT marked [ASYNC HANDLER] use standard Fire() — no constraints.
//
// When you add a new signal + BindAsyncCommand pair, add the [ASYNC HANDLER] marker
// here so future callers know which fire method to use.
// ─────────────────────────────────────────────────────────────────────────────

namespace RingFlow.Gameplay
{
    // ── Level Lifecycle ───────────────────────────────────
    public readonly struct InitLevelSignal
    {
        public readonly int LevelIndex;
        public readonly bool ForceRestart;
        public InitLevelSignal(int levelIndex, bool forceRestart = false) 
        {
            LevelIndex = levelIndex;
            ForceRestart = forceRestart;
        }
    }

    public readonly struct LevelLoadedSignal
    {
        public readonly int LevelIndex;
        public LevelLoadedSignal(int levelIndex) => LevelIndex = levelIndex;
    }

    // [ASYNC HANDLER] — LevelWonCommand is IAsyncCommand.
    // Fire with: await FireAsync / FireAsyncAndForget. Never Fire().
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

    // [ASYNC HANDLER] — CheckWinCommand is IAsyncCommand (awaits FireAsync(LevelWonSignal) internally).
    // Fire with: await FireAsync / FireAsyncAndForget. Never Fire().
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

    /// <summary>
    /// Fired when a ring lands on top of a Stone ring.
    /// BoardMediator listens to trigger a stone impact thud sound + VFX.
    /// </summary>
    public readonly struct StoneImpactSignal
    {
        public readonly int PoleId;
        public readonly RingColor Color;
        public StoneImpactSignal(int poleId, RingColor color)
        {
            PoleId = poleId;
            Color = color;
        }
    }

    /// <summary>
    /// Fired when a Chain mechanic pulls its partner ring to the target pole.
    /// BoardMediator listens to trigger VFX (chain-link burst) and SFX (metallic clink).
    /// </summary>
    public readonly struct ChainLinkSignal
    {
        public readonly int FromPoleId;
        public readonly int ToPoleId;
        public readonly RingColor Color;
        public ChainLinkSignal(int fromPoleId, int toPoleId, RingColor color)
        {
            FromPoleId = fromPoleId;
            ToPoleId = toPoleId;
            Color = color;
        }
    }

    /// <summary>
    /// Fired when a Magnet mechanic pulls same-color rings to the target pole.
    /// BoardMediator listens to trigger VFX (magnetic whoosh) and SFX (magnetic hum).
    /// </summary>
    public readonly struct MagnetPullSignal
    {
        public readonly int TargetPoleId;
        public readonly int PulledCount;
        public readonly RingColor Color;
        public MagnetPullSignal(int targetPoleId, int pulledCount, RingColor color)
        {
            TargetPoleId = targetPoleId;
            PulledCount = pulledCount;
            Color = color;
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
    // [ASYNC HANDLER] — HintCommand is IAsyncCommand (runs solver off main thread).
    // Fire with: await FireAsync / FireAsyncAndForget. Never Fire().
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
    // [ASYNC HANDLER] — LevelLostCommand is IAsyncCommand.
    // Fire with: await FireAsync / FireAsyncAndForget. Never Fire().
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

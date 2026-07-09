namespace RingFlow.Gameplay
{
    // ── UI Screen Management ──────────────────────────────
    public enum ScreenType
    {
        Splash,
        MainMenu,
        WorldMap,
        LevelSelect,
        Gameplay,
        Pause,
        Win,
        Settings,
        DailyReward,
        Onboarding,
        GameOver,
        ChestPopup,
        ParentalGate
    }

    public readonly struct ShowScreenSignal
    {
        public readonly ScreenType Screen;
        public ShowScreenSignal(ScreenType screen) => Screen = screen;
    }

    public readonly struct HideScreenSignal
    {
        public readonly ScreenType Screen;
        public HideScreenSignal(ScreenType screen) => Screen = screen;
    }

    // ── UI Button Signals ─────────────────────────────────
    public readonly struct PlayRequestedSignal {}
    public readonly struct LevelSelectedSignal
    {
        public readonly int LevelIndex;
        public LevelSelectedSignal(int levelIndex) => LevelIndex = levelIndex;
    }
    public readonly struct PauseRequestedSignal {}
    public readonly struct ResumeRequestedSignal {}
    public readonly struct NextLevelRequestedSignal {}
    public readonly struct QuitToMenuRequestedSignal {}
    public readonly struct OpenSettingsSignal {}
    public readonly struct CloseSettingsSignal {}
    public readonly struct OpenDailyRewardSignal {}
    public readonly struct CloseDailyRewardSignal {}
    public readonly struct WorldSelectedSignal
    {
        public readonly int WorldIndex;
        public WorldSelectedSignal(int worldIndex) => WorldIndex = worldIndex;
    }

    // ── Gameplay Signals ──────────────────────────────────
    public readonly struct InitLevelSignal
    {
        public readonly int LevelIndex;
        public InitLevelSignal(int levelIndex) => LevelIndex = levelIndex;
    }

    /// <summary>
    /// Fired by <see cref="Commands.InitLevelCommand"/> AFTER the model has been
    /// populated with the new level's poles and rings. BoardMediator subscribes
    /// to this signal (not InitLevelSignal) so the visual board rebuilds only
    /// after the model is in a consistent state.
    /// </summary>
    public readonly struct LevelLoadedSignal
    {
        public readonly int LevelIndex;
        public LevelLoadedSignal(int levelIndex) => LevelIndex = levelIndex;
    }

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

    public readonly struct UndoSignal {}

    public readonly struct CheckWinSignal {}

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

    /// <summary>
    /// GDD §9 — Hint: aynı seviyeyi solver ile çözüp SONUÇ listesinin ilk hamlesini döndürür (çözümü vermez).
    /// Maliyet: 50 coin VEYA rewarded ad (AdService tarafından yönetilir).
    /// </summary>
    public readonly struct HintRequestedSignal {}

    /// <summary>
    /// Solver'ın bulduğu ilk doğru hamle — view yalnızca bu halkaya görsel ipucu gösterir.
    /// Çözümü vermez — UI sadece bu ipucunu highlight eder (örn. halkayı parlatır).
    /// </summary>
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

        public static HintResolvedSignal Empty => new HintResolvedSignal(-1, -1, false);
    }

    /// <summary>
    /// GDD §9 — Undo butonuna basıldı. Maliyet: ilk 5 ücretsiz, sonrası 5 coin/ad.
    /// </summary>
    public readonly struct UndoRequestedSignal {}

    // ── Game Over / Restart Signals ───────────────────────
    public readonly struct GameOverSignal {}

    public readonly struct RestartLevelSignal
    {
        public readonly int LevelIndex;
        public RestartLevelSignal(int levelIndex) => LevelIndex = levelIndex;
    }

    /// <summary>
    /// GDD §9 — Günlük ödül talep edildi. İlk girişimde sadece FreeUndosUsedThisSession dikkate alınır,
    /// sonraki adımlarda AdvertiserGate üzerinden geçer.
    /// </summary>
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

    // ── Chest System Signals (GDD §9) ─────────────────────
    public readonly struct ChestAwardedSignal
    {
        public readonly int Bronze;
        public readonly int Silver;
        public readonly int Gold;
        public readonly int Diamond;
        public ChestAwardedSignal(int bronze, int silver, int gold, int diamond)
        {
            Bronze = bronze;
            Silver = silver;
            Gold = gold;
            Diamond = diamond;
        }
    }

    public readonly struct ChestClaimAllSignal {}

    public readonly struct OpenChestPopupSignal {}
    public readonly struct CloseChestPopupSignal {}
}

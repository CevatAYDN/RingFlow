namespace RingFlow.Gameplay
{
    public readonly struct InitLevelSignal
    {
        public readonly int LevelIndex;
        public InitLevelSignal(int levelIndex) => LevelIndex = levelIndex;
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
}

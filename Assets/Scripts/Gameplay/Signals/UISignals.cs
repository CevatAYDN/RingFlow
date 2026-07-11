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

    // ── Chest & Popup Signals (GDD §9) ────────────────────
    public readonly struct OpenChestPopupSignal {}
    public readonly struct CloseChestPopupSignal {}
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

    // ── Purchase Failure ──────────────────────────────────
    public readonly struct PurchaseFailedSignal
    {
        public readonly string ProductId;
        public readonly bool StoreUnavailable;
        public PurchaseFailedSignal(string productId, bool storeUnavailable)
        {
            ProductId = productId;
            StoreUnavailable = storeUnavailable;
        }
    }
}

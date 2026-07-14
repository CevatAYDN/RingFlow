using System;
using UnityEngine.Scripting;
using RingFlow.Gameplay;
using RingFlow.Gameplay.Strategies;


namespace RingFlow.Gameplay.AOT
{
    /// <summary>
    /// AOT (Ahead-Of-Time) compilation attributes for IL2CPP compatibility.
    /// These attributes ensure that critical types and methods are not stripped by the
    /// Unity code stripper during mobile build processes.
    /// Following Nexus Core AOT requirements for production builds.
    /// </summary>
    public static class AOTPreserveAttributes
    {
        /// <summary>
        /// Preserves all gameplay commands from IL2CPP stripping.
        /// Commands are dynamically instantiated by Nexus DI container.
        /// </summary>
        [Preserve]
        public static void PreserveCommands()
        {
            // Command types are preserved through their injection points
            // This method ensures the types are included in AOT compilation
            typeof(InitLevelCommand).ToString();
            typeof(SelectPoleCommand).ToString();
            typeof(MoveRingCommand).ToString();
            typeof(UndoCommand).ToString();
            typeof(UndoRequestedCommand).ToString();
            typeof(CheckWinCommand).ToString();
            typeof(LevelWonCommand).ToString();
            typeof(HintCommand).ToString();
            typeof(ChestClaimCommand).ToString();
            typeof(DailyRewardClaimCommand).ToString();
        }

        /// <summary>
        /// Preserves all FSM states from IL2CPP stripping.
        /// States are dynamically instantiated by the GameStateMachine.
        /// </summary>
        [Preserve]
        public static void PreserveStates()
        {
            typeof(BootState).ToString();
            typeof(SplashState).ToString();
            typeof(MainMenuState).ToString();
            typeof(LevelSelectState).ToString();
            typeof(PlayingState).ToString();
            typeof(PausedState).ToString();
            typeof(WinState).ToString();
            typeof(GameOverState).ToString();
            typeof(LoadingState).ToString();
            typeof(ErrorState).ToString();
        }

        /// <summary>
        /// Preserves all mediators from IL2CPP stripping.
        /// Mediators are dynamically instantiated by the Nexus View system.
        /// </summary>
        [Preserve]
        public static void PreserveMediators()
        {
            typeof(BoardMediator).ToString();
            typeof(PoleMediator).ToString();
            typeof(UI.HUDMediator).ToString();
            typeof(UI.MainMenuMediator).ToString();
            typeof(UI.LevelSelectMediator).ToString();
            typeof(UI.PauseMediator).ToString();
            typeof(UI.WinMediator).ToString();
            typeof(UI.SettingsMediator).ToString();
            typeof(UI.DailyRewardPopupMediator).ToString();
            typeof(UI.ChestPopupMediator).ToString();
            typeof(UI.GameOverMediator).ToString();
            typeof(UI.ParentalGatePopupMediator).ToString();
        }

        /// <summary>
        /// Preserves all strategy implementations from IL2CPP stripping.
        /// Strategies are dynamically instantiated by strategy managers.
        /// </summary>
        [Preserve]
        public static void PreserveStrategies()
        {
            // Ring move strategies
            typeof(MysteryRingStrategy).ToString();
            typeof(PaintRingStrategy).ToString();
            typeof(RainbowRingStrategy).ToString();
            typeof(StandardRingStrategy).ToString();

            // Ring validation strategies
            typeof(StandardRingValidationStrategy).ToString();
            typeof(KeyRingValidationStrategy).ToString();
            typeof(StoneRingValidationStrategy).ToString();
            typeof(FrozenRingValidationStrategy).ToString();

            // Strategy managers
            typeof(RingMoveStrategyManager).ToString();
            typeof(RingValidationStrategyManager).ToString();
        }

        /// <summary>
        /// Preserves all signal types from IL2CPP stripping.
        /// Signals are used as generic type parameters in Nexus SignalBus.
        /// </summary>
        [Preserve]
        public static void PreserveSignals()
        {
            typeof(ShowScreenSignal).ToString();
            typeof(HideScreenSignal).ToString();
            typeof(PlayRequestedSignal).ToString();
            typeof(LevelSelectedSignal).ToString();
            typeof(PauseRequestedSignal).ToString();
            typeof(ResumeRequestedSignal).ToString();
            typeof(NextLevelRequestedSignal).ToString();
            typeof(QuitToMenuRequestedSignal).ToString();
            typeof(OpenSettingsSignal).ToString();
            typeof(CloseSettingsSignal).ToString();
            typeof(OpenDailyRewardSignal).ToString();
            typeof(CloseDailyRewardSignal).ToString();
            typeof(InitLevelSignal).ToString();
            typeof(LevelLoadedSignal).ToString();
            typeof(SelectPoleSignal).ToString();
            typeof(MoveRingSignal).ToString();
            typeof(RingMovedSignal).ToString();
            typeof(UndoSignal).ToString();
            typeof(CheckWinSignal).ToString();
            typeof(PoleCompletedSignal).ToString();
            typeof(LevelWonSignal).ToString();
            typeof(PlayingStateArgs).ToString();
            typeof(RevealMysterySignal).ToString();
            typeof(BreakIceSignal).ToString();
            typeof(UnlockPoleSignal).ToString();
            typeof(BombTickSignal).ToString();
            typeof(BombExplodedSignal).ToString();
            typeof(PaintRingSignal).ToString();
            typeof(HintRequestedSignal).ToString();
            typeof(MoveBlockedSignal).ToString();
            typeof(HintResolvedSignal).ToString();
            typeof(UndoRequestedSignal).ToString();
            typeof(GameOverSignal).ToString();
            typeof(RestartLevelSignal).ToString();
            typeof(DailyRewardClaimSignal).ToString();
            typeof(DailyRewardGrantedSignal).ToString();
            typeof(ChestAwardedSignal).ToString();
            typeof(ChestClaimAllSignal).ToString();
            typeof(OpenChestPopupSignal).ToString();
            typeof(CloseChestPopupSignal).ToString();
        }

        /// <summary>
        /// Preserves all model types from IL2CPP stripping.
        /// Models are dynamically instantiated by the Nexus DI container.
        /// </summary>
        [Preserve]
        public static void PreserveModels()
        {
            typeof(GameplayModel).ToString();
            typeof(PlayerProgressModel).ToString();
            typeof(SettingsModel).ToString();
        }

        /// <summary>
        /// Preserves all service types from IL2CPP stripping.
        /// Services are dynamically instantiated by the Nexus service lifecycle.
        /// </summary>
        [Preserve]
        public static void PreserveServices()
        {
            typeof(EconomyService).ToString();
            typeof(ProgressionService).ToString();
            typeof(DailyRewardService).ToString();
            typeof(RingFlow.Gameplay.Services.GameTimeService).ToString();
            typeof(RingFlow.Gameplay.Services.LegalConsentService).ToString();
            typeof(VfxPrefabRegistry).ToString();

            // Data-driven ScriptableObject types (loaded via Resources.Load in GameplayLifecycle)
            typeof(RingFlow.Gameplay.Economy.StoreCatalogSO).ToString();
            typeof(RingFlow.Gameplay.Localization.LocalizationConfigSO).ToString();
            typeof(RingFlow.Gameplay.Strategies.RingMechanicDataSO).ToString();
            typeof(RingFlow.Gameplay.Views.ThemeSkinDatabaseSO).ToString();
        }

        /// <summary>
        /// Preserves all view types from IL2CPP stripping.
        /// Views are dynamically instantiated by the Nexus View system.
        /// </summary>
        [Preserve]
        public static void PreserveViews()
        {
            typeof(BoardView).ToString();
            typeof(PoleView).ToString();
            typeof(UI.HUDView).ToString();
            typeof(UI.MainMenuView).ToString();
            typeof(UI.LevelSelectView).ToString();
            typeof(UI.PauseView).ToString();
            typeof(UI.WinView).ToString();
            typeof(UI.SettingsView).ToString();
            typeof(UI.DailyRewardPopupView).ToString();
            typeof(UI.ChestPopupView).ToString();
            typeof(UI.GameOverView).ToString();
            typeof(UI.SplashView).ToString();
            typeof(UI.ParentalGatePopupView).ToString();
            typeof(UI.LocalizedTextBinder).ToString();
        }
    }
}

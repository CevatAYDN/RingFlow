using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// GDD §6 — Lifecycle binder for the entire gameplay context.
    /// Binds: encrypted storage (anti-cheat), economy, progression, player progress,
    /// accessibility settings, daily reward service, hint/daily/undo commands.
    /// Registered services are auto-initialized once the model has been bound.
    /// </summary>
    public class GameplayLifecycle : IContextLifecycle
    {
        public void OnConfigure(IContextBuilder builder)
        {
            // -------------------- Storage --------------------
            builder.Bind<IPlayerPrefsService, EncryptedStorageService>();

            // -------------------- Core Services --------------------
            builder.BindService<IEconomyService, EconomyService>();
            builder.BindService<IProgressionService, ProgressionService>();
            builder.BindService<IAudioService, AudioService>();
            builder.BindService<IHapticService, HapticService>();
            builder.BindService<IFeedbackService, FeedbackService>();
            builder.BindService<ILocalizationService, LocalizationService>();
            builder.BindService<IAnalyticsService, AnalyticsService>();
            builder.BindService<IAdService, AdService>();
            builder.BindService<IIapService, IapService>();

            // -------------------- Models --------------------
            builder.BindModel<GameplayModel>();
            builder.BindModel<PlayerProgressModel>();
            builder.BindModel<SettingsModel>();

            // -------------------- POCO Services (typed factory injection) --------------------
            // DailyRewardService takes PlayerProgressModel via constructor — bind manually so DI resolves it.
            builder.Bind<DailyRewardService>(); // singleton default

            // -------------------- Commands --------------------
            builder.BindCommand<InitLevelSignal, InitLevelCommand>();
            builder.BindCommand<SelectPoleSignal, SelectPoleCommand>();
            builder.BindCommand<MoveRingSignal, MoveRingCommand>();
            builder.BindCommand<UndoSignal, UndoCommand>();
            builder.BindCommand<UndoRequestedSignal, UndoRequestedCommand>();
            builder.BindCommand<CheckWinSignal, CheckWinCommand>();
            builder.BindCommand<HintRequestedSignal, HintCommand>();
            builder.BindCommand<DailyRewardClaimSignal, DailyRewardClaimCommand>();
        }

        public ValueTask OnInitializeAsync(CancellationToken ct)
        {
            // Hook the audio service to the settings model's reactive bits.
            var context = NexusRuntime.CurrentContext;
            var settings = context.TryResolve<SettingsModel>();
            var audio = context.TryResolve<IAudioService>();
            var haptics = context.TryResolve<IHapticService>();
            if (settings != null && audio != null)
            {
                settings.MusicEnabled.OnChanged((_, n) => audio.IsMuted = !n);
                settings.SfxEnabled.OnChanged((_, n) => audio.SfxVolume = n ? 1f : 0f);
                settings.MusicEnabled.OnChanged((_, n) => audio.BgmVolume = n ? 1f : 0f);
            }
            if (settings != null && haptics != null)
            {
                settings.HapticEnabled.OnChanged((_, n) => haptics.IsEnabled = n);
            }

            // Trigger initial load from disk into all reactive models.
            var prefs = context.TryResolve<IPlayerPrefsService>();
            if (prefs != null)
            {
                var progress = context.TryResolve<PlayerProgressModel>();
                var set = context.TryResolve<SettingsModel>();
                if (progress != null) PlayerProgressSaveSystem.Load(prefs, progress);
                if (set != null) SettingsSaveSystem.Load(prefs, set);
            }
            return default;
        }

        public ValueTask OnStartAsync(CancellationToken ct) => default;

        public void OnDispose()
        {
            // Auto-flush save on dispose — ensure no loss when player quits to main menu.
            var context = NexusRuntime.CurrentContext;
            var prefs = context.TryResolve<IPlayerPrefsService>();
            if (prefs != null)
            {
                var progress = context.TryResolve<PlayerProgressModel>();
                var set = context.TryResolve<SettingsModel>();
                if (progress != null) PlayerProgressSaveSystem.Save(prefs, progress);
                if (set != null) SettingsSaveSystem.Save(prefs, set);
            }
        }
    }
}

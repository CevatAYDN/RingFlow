using System.Threading;
using System.Threading.Tasks;
using Nexus.Core;
using Nexus.Core.Services;
using Nexus.Core.FSM;
using UnityEngine;
using UnityEngine.EventSystems;

using RingFlow.Gameplay.Diagnostics;
using RingFlow.Gameplay.Services;
using RingFlow.Gameplay.Strategies;
using RingFlow.Gameplay.UI;
using RingFlow.Gameplay.Economy;
using RingFlow.Gameplay.Localization;
using RingFlow.Gameplay.Views;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// GDD §6 — Lifecycle binder for the entire gameplay context.
    /// Binds: encrypted storage (anti-cheat), economy, progression, player progress,
    /// accessibility settings, daily reward service, hint/daily/undo commands.
    /// Registered services are auto-initialized once the model has been bound.
    /// MonoBehaviour so it can be attached to the NexusRoot GameObject and discovered
    /// by Root.InitializeContext() via GetComponents<IContextLifecycle>().
    /// </summary>
    public class GameplayLifecycle : MonoBehaviour, IContextLifecycle
    {
        [UnityEngine.Scripting.Preserve]
        [SerializeField] private GameObject _ringPopPrefab;
        [UnityEngine.Scripting.Preserve]
        [SerializeField] private GameObject _confettiPrefab;
        [UnityEngine.Scripting.Preserve]
        [SerializeField] private GameObject _mergeEffectPrefab;
        
        private static float s_sessionStartTime;
        private static bool s_sessionEndTracked;

        public void OnConfigure(IContextBuilder builder)
        {
            NexusLog.Info("GameplayLifecycle", nameof(OnConfigure), "Gameplay", "Configuring gameplay context.");
            
            // -------------------- VFX Prefab Registration --------------------
            // Register prefab references as a service for proper DI access
            var vfxRegistry = new VfxPrefabRegistry
            {
                RingPopPrefab = _ringPopPrefab,
                ConfettiPrefab = _confettiPrefab,
                MergeEffectPrefab = _mergeEffectPrefab
            };
            builder.BindInstance<VfxPrefabRegistry>(vfxRegistry);
            
            // -------------------- Config Databases & Themes --------------------
            var assetService = new ResourcesAssetService();

            var db = assetService.LoadAsync<GameConfigDatabaseSO>(GameplayAssetKeys.GameConfigDatabase).GetAwaiter().GetResult();
            if (db == null)
                throw new System.InvalidOperationException($"[GameplayLifecycle] GameConfigDatabaseSO '{GameplayAssetKeys.GameConfigDatabase}' not found.");
            builder.BindInstance<GameConfigDatabaseSO>(db);

            var feel = assetService.LoadAsync<GameFeelConfigSO>(GameplayAssetKeys.GameFeelConfig).GetAwaiter().GetResult();
            if (feel == null)
                throw new System.InvalidOperationException($"[GameplayLifecycle] GameFeelConfigSO '{GameplayAssetKeys.GameFeelConfig}' not found.");
            builder.BindInstance<GameFeelConfigSO>(feel);

            DoTweenCapacityBootstrap.EnsureInitialized(feel.DoTweenTweensCapacity, feel.DoTweenSequencesCapacity);

            var palette = assetService.LoadAsync<RingColorPaletteSO>(GameplayAssetKeys.RingColorPalette).GetAwaiter().GetResult();
            if (palette == null)
                throw new System.InvalidOperationException($"[GameplayLifecycle] RingColorPaletteSO '{GameplayAssetKeys.RingColorPalette}' not found.");
            builder.BindInstance<RingColorPaletteSO>(palette);

            var theme = assetService.LoadAsync<UIThemeConfigSO>(GameplayAssetKeys.UIThemeConfig).GetAwaiter().GetResult();
            if (theme == null)
                throw new System.InvalidOperationException($"[GameplayLifecycle] UIThemeConfigSO '{GameplayAssetKeys.UIThemeConfig}' not found.");
            GameUIResources.Bind(theme);
            builder.BindInstance<UIThemeConfigSO>(theme);

            var audioConfig = assetService.LoadAsync<AudioConfigSO>(GameplayAssetKeys.AudioConfig).GetAwaiter().GetResult();
            if (audioConfig == null)
                throw new System.InvalidOperationException($"[GameplayLifecycle] AudioConfigSO '{GameplayAssetKeys.AudioConfig}' not found.");
            ProceduralAudio.Initialize(audioConfig);
            builder.BindInstance<AudioConfigSO>(audioConfig);

            var storeCatalog = assetService.LoadAsync<StoreCatalogSO>(GameplayAssetKeys.StoreCatalog).GetAwaiter().GetResult();
            if (storeCatalog == null)
                throw new System.InvalidOperationException($"[GameplayLifecycle] StoreCatalogSO '{GameplayAssetKeys.StoreCatalog}' not found.");
            builder.BindInstance<StoreCatalogSO>(storeCatalog);

            var localizationConfig = assetService.LoadAsync<LocalizationConfigSO>(GameplayAssetKeys.LocalizationConfig).GetAwaiter().GetResult();
            if (localizationConfig == null)
                throw new System.InvalidOperationException($"[GameplayLifecycle] LocalizationConfigSO '{GameplayAssetKeys.LocalizationConfig}' not found.");
            builder.BindInstance<LocalizationConfigSO>(localizationConfig);

            var ringMechanicData = assetService.LoadAsync<RingMechanicDataSO>(GameplayAssetKeys.RingMechanicData).GetAwaiter().GetResult();
            if (ringMechanicData == null)
                throw new System.InvalidOperationException($"[GameplayLifecycle] RingMechanicDataSO '{GameplayAssetKeys.RingMechanicData}' not found.");
            builder.BindInstance<RingMechanicDataSO>(ringMechanicData);

            var themeSkinDb = assetService.LoadAsync<ThemeSkinDatabaseSO>(GameplayAssetKeys.ThemeSkinDatabase).GetAwaiter().GetResult();
            if (themeSkinDb == null)
                throw new System.InvalidOperationException($"[GameplayLifecycle] ThemeSkinDatabaseSO '{GameplayAssetKeys.ThemeSkinDatabase}' not found.");
            builder.BindInstance<ThemeSkinDatabaseSO>(themeSkinDb);

            var mainCamera = Camera.main;
            if (mainCamera == null)
                throw new System.InvalidOperationException("[GameplayLifecycle] Camera.main is null. Scene must have a Main Camera.");
            builder.BindInstance<Camera>(mainCamera);


            // -------------------- Storage --------------------
            builder.Bind<IPlayerPrefsService, EncryptedStorageService>();
            builder.Bind<ILocalizationTableProvider, CSVLocalizationTableProvider>();

            // -------------------- Core Services --------------------
            builder.BindService<ILoggerService, LoggerService>();
            builder.BindService<IEconomyService, EconomyService>();
            builder.BindService<IProgressionService, ProgressionService>();
            // IAudioRootProvider must be bound BEFORE AudioService so DI can resolve it.
            builder.Bind<IAudioRootProvider, DefaultAudioRootProvider>();
            builder.BindService<IAudioService, AudioService>();
            builder.BindService<IHapticService, HapticService>();
            builder.BindService<IFeedbackService, FeedbackService>();
            builder.BindService<ILocalizationService, LocalizationService>();
            // C3+C4: DI-injectable replacements for static GameUIResources and ProceduralAudio.
            // Existing static call-sites continue working during migration period.
            builder.BindService<IGameUIResourcesService, GameUIResourcesService>();
            builder.BindService<IProceduralAudioService, ProceduralAudioService>();
            builder.BindService<Services.IGameTimeService, Services.GameTimeService>();
            builder.BindService<Services.ILegalConsentService, Services.LegalConsentService>();
            builder.BindService<IAnalyticsService, AnalyticsService>();
            builder.BindService<IAdService, AdService>();
            builder.BindService<IIapService, IapService>();
            builder.BindService<IObjectPoolService, ObjectPoolService>();

            // -------------------- Asset Service (GDD §6) --------------------
            builder.Bind<IAssetService, ResourcesAssetService>();

            // -------------------- Diagnostics & Tracing --------------------
            builder.BindService<IGameDiagnostics, GameDiagnostics>();
            builder.BindService<IFsmTransitionTracer, FsmTransitionTracer>();
            builder.BindService<ISignalBusMonitor, SignalBusMonitor>();
            builder.BindService<IViewMediatorTracker, ViewMediatorTracker>();

            // -------------------- Models --------------------
            builder.BindModel<GameplayModel>();
            builder.BindModel<PlayerProgressModel>();
            builder.BindModel<SettingsModel>();

            // -------------------- State Machine --------------------
            builder.Bind<IGameStateMachine, GameStateMachine>();
            builder.Bind<BootState>();
            builder.Bind<SplashState>();
            builder.Bind<MainMenuState>();
            builder.Bind<LevelSelectState>();
            builder.Bind<PlayingState>();
            builder.Bind<PausedState>();
            builder.Bind<WinState>();
            builder.Bind<GameOverState>();
            builder.Bind<LoseState>();
            builder.Bind<LoadingState>();
            builder.Bind<ErrorState>();

            // -------------------- POCO Services (typed factory injection) --------------------
            // DailyRewardService takes PlayerProgressModel via constructor — bind manually so DI resolves it.
            builder.Bind<DailyRewardService>(); // singleton default

            // -------------------- Ring Move Strategies (Strategy Pattern) --------------------
            // Register strategy manager for special ring mechanics (GDD §4)
            builder.Bind<RingMoveStrategyManager>();

            // -------------------- Ring Validation Strategies (Strategy Pattern) --------------------
            // Register validation strategy manager for pole placement rules (GDD §4)
            builder.Bind<RingValidationStrategyManager>();

            // -------------------- Helper Components (Aşama 2 - BoardView Refactoring) --------------------
            builder.Bind<RingMaterialManager>();
            builder.Bind<SpecialOverlayRenderer>();

            // -------------------- Commands --------------------
            builder.BindAsyncCommand<InitLevelSignal, InitLevelCommand>();
            builder.BindCommand<SelectPoleSignal, SelectPoleCommand>();
            builder.BindCommand<MoveRingSignal, MoveRingCommand>();
            builder.BindCommand<UndoSignal, UndoCommand>();
            builder.BindCommand<UndoRequestedSignal, UndoRequestedCommand>();
            // CheckWinCommand is IAsyncCommand — it awaits FireAsync(LevelWonSignal) internally.
            // BindAsyncCommand ensures the pool keeps the instance alive until ExecuteAsync completes.
            builder.BindAsyncCommand<CheckWinSignal, CheckWinCommand>();
            // PERMANENT FIX: IAsyncCommand — pool Return() deferred until ExecuteAsync completes,
            // so [Inject] fields (_fsm etc.) remain valid through the await Task.Delay chain.
            builder.BindAsyncCommand<LevelWonSignal, LevelWonCommand>();
            builder.BindAsyncCommand<LevelLostSignal, LevelLostCommand>();
            // HintCommand runs solver off-thread via LevelSolver.SolveAsync, hence async binding.
            builder.BindAsyncCommand<HintRequestedSignal, HintCommand>();
            builder.BindCommand<ChestClaimAllSignal, ChestClaimCommand>();
            builder.BindCommand<DailyRewardClaimSignal, DailyRewardClaimCommand>();
        }

        public async ValueTask OnInitializeAsync(CancellationToken ct)
        {
            var visualBoard = GameObject.Find("RingFlow_VisualBoard");
            if (visualBoard != null)
            {
                Object.Destroy(visualBoard);
            }
            else
            {
                NexusLog.Warn("GameplayLifecycle", "OnInitializeAsync", "", 
                    "'RingFlow_VisualBoard' scene object not found. This is normal in tests or when loaded from other scenes.");
            }

            var context = NexusRuntime.CurrentContext;
            var diag = context.TryResolve<IGameDiagnostics>();
            diag?.Checkpoint("GameplayLifecycle.OnInitializeAsync");
            diag?.Log("Lifecycle", "GameplayLifecycle.OnInitializeAsync started");

            // Hook palette
            var palette = context.Resolve<RingColorPaletteSO>();
            diag?.Log("Lifecycle", "RingColorPalette resolved successfully.");
 
            var settings = context.TryResolve<SettingsModel>();
            var audio = context.TryResolve<IAudioService>();
            var haptics = context.TryResolve<IHapticService>();
            var progress = context.TryResolve<PlayerProgressModel>();
 
            HookSettingsToServices(context, settings, audio, haptics);

            var fsm = context.Resolve<IGameStateMachine>();
            RegisterFsmAndStates(fsm, context);
            // NOTE: RingValidationStrategyManager is injected directly into
            // SelectPoleCommand and MoveRingCommand via DI — no static setter needed.

            PreserveAotTypes();

            await fsm.ChangeStateAsync<BootState>();

            var prefs = context.TryResolve<IPlayerPrefsService>();
            InitializePlayerPrefsService(prefs, progress, settings);
            InitializeSessionAnalytics(context, prefs, progress);
            InitializeHapticState(settings, haptics);
            RegisterIapProducts(context);
            InitializeVfxAndPools(context);
            InitializeAudio(context, audio, progress);

            diag?.Log("Lifecycle", "GameplayLifecycle.OnInitializeAsync completed");
        }

        private void RegisterFsmAndStates(IGameStateMachine fsm, IContext context)
        {
            fsm.RegisterState(context.Resolve<BootState>());
            fsm.RegisterState(context.Resolve<SplashState>());
            fsm.RegisterState(context.Resolve<MainMenuState>());
            fsm.RegisterState(context.Resolve<LevelSelectState>());
            fsm.RegisterState(context.Resolve<PlayingState>());
            fsm.RegisterState(context.Resolve<PausedState>());
            fsm.RegisterState(context.Resolve<WinState>());
            fsm.RegisterState(context.Resolve<GameOverState>());
            fsm.RegisterState(context.Resolve<LoseState>());
            fsm.RegisterState(context.Resolve<LoadingState>());
            fsm.RegisterState(context.Resolve<ErrorState>());
        }

        private static void HookSettingsToServices(IContext context, SettingsModel settings, IAudioService audio, IHapticService haptics)
        {
            var localization = context.TryResolve<ILocalizationService>();
            var provider = context.TryResolve<ILocalizationTableProvider>();
            var localizationConfig = context.TryResolve<LocalizationConfigSO>();

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
            if (settings != null && localization != null)
            {
                if (provider != null && localizationConfig != null && localizationConfig.Languages != null)
                {
                    for (int i = 0; i < localizationConfig.Languages.Count; i++)
                    {
                        var lang = localizationConfig.Languages[i];
                        if (provider.TryGetTable(lang.Code, out var table) && table != null)
                        {
                            localization.RegisterLanguageTable(lang.Code, table);
                        }
                    }
                }

                settings.LanguageCode.OnChanged((_, n) => localization.SetLanguage(n));
                localization.SetLanguage(settings.LanguageCode.Value);
            }
        }

        private static void PreserveAotTypes()
        {
            AOT.AOTPreserveAttributes.PreserveCommands();
            AOT.AOTPreserveAttributes.PreserveStates();
            AOT.AOTPreserveAttributes.PreserveMediators();
            AOT.AOTPreserveAttributes.PreserveStrategies();
            AOT.AOTPreserveAttributes.PreserveSignals();
            AOT.AOTPreserveAttributes.PreserveModels();
            AOT.AOTPreserveAttributes.PreserveServices();
            AOT.AOTPreserveAttributes.PreserveViews();
        }

        private static void InitializePlayerPrefsService(IPlayerPrefsService prefs, PlayerProgressModel progress, SettingsModel settings)
        {
            if (prefs == null) return;
            if (progress != null)
            {
                PlayerProgressSaveSystem.Load(prefs, progress);
                progress.FreeUndosUsedThisSession.Value = 0;
            }
            if (settings != null) SettingsSaveSystem.Load(prefs, settings);
        }

        private static void InitializeSessionAnalytics(IContext context, IPlayerPrefsService prefs, PlayerProgressModel progress)
        {
            s_sessionStartTime = Time.realtimeSinceStartup;
            s_sessionEndTracked = false;

            if (prefs == null) return;
            var time = context.TryResolve<IGameTimeService>();
            var nowUtc = time?.UtcNow ?? System.DateTime.UtcNow;
            string firstLaunchStr = prefs.GetString(GameplayAssetKeys.PlayerPrefs.FirstLaunchTime, "");
            System.DateTime firstLaunch;
            if (string.IsNullOrEmpty(firstLaunchStr))
            {
                firstLaunch = nowUtc;
                prefs.SetString(GameplayAssetKeys.PlayerPrefs.FirstLaunchTime, firstLaunch.ToString("o"));
                prefs.Save();
            }
            else
            {
                System.DateTime.TryParse(firstLaunchStr, out firstLaunch);
            }

            int daysSinceFirstLaunch = (nowUtc - firstLaunch).Days;
            var analytics = context.TryResolve<IAnalyticsService>();
            if (analytics != null)
            {
                analytics.LogEvent("session_start", new[] 
                { 
                    ("days_since_first_launch", daysSinceFirstLaunch.ToString()),
                    ("player_level", (progress?.PlayerLevel.Value ?? 1).ToString())
                });
            }
        }



        private static void InitializeHapticState(SettingsModel settings, IHapticService haptics)
        {
            if (settings != null && haptics != null)
            {
                haptics.IsEnabled = settings.HapticEnabled.Value;
            }
        }

        private static void RegisterIapProducts(IContext context)
        {
            var iap = context.TryResolve<IIapService>();
            var catalog = context.TryResolve<StoreCatalogSO>();
            if (iap != null && catalog != null && catalog.Products != null)
            {
                for (int i = 0; i < catalog.Products.Count; i++)
                {
                    var entry = catalog.Products[i];
                    iap.RegisterProducts(
                        new ProductDefinition
                        {
                            Id = entry.Id,
                            Type = (Nexus.Core.Services.ProductType)(int)entry.Type,
                            PriceString = entry.PriceString
                        }
                    );
                }
            }
        }

        private static void InitializeVfxAndPools(IContext context)
        {
            // Initialize VfxMeshCache (generates shared meshes once)
            VfxMeshCache.Initialize();

            var vfxRegistry = context.TryResolve<VfxPrefabRegistry>();
            if (vfxRegistry != null)
            {
                if (vfxRegistry.RingPopPrefab == null)
                {
                    var ringPopObj = new GameObject("RingPopVfxPrefab", typeof(RingPopVfx));
                    Object.DontDestroyOnLoad(ringPopObj);
                    vfxRegistry.RingPopPrefab = ringPopObj;
                }
                if (vfxRegistry.ConfettiPrefab == null)
                {
                    var confettiObj = new GameObject("ConfettiVfxPrefab", typeof(ConfettiVfx));
                    Object.DontDestroyOnLoad(confettiObj);
                    vfxRegistry.ConfettiPrefab = confettiObj;
                }
                if (vfxRegistry.MergeEffectPrefab == null)
                {
                    var mergeObj = new GameObject("MergeEffectVfxPrefab", typeof(MergeEffectVfx));
                    Object.DontDestroyOnLoad(mergeObj);
                    vfxRegistry.MergeEffectPrefab = mergeObj;
                }

                vfxRegistry.Validate();

                var pool = context.TryResolve<IObjectPoolService>();
                var feelConfig = context.Resolve<GameFeelConfigSO>();
                if (pool != null)
                {
                    if (feelConfig == null)
                        throw new System.InvalidOperationException("[GameplayLifecycle] GameFeelConfigSO is required for pool prewarming.");
                    pool.Prewarm(vfxRegistry.RingPopPrefab, feelConfig.RingPopPoolSize);
                    pool.Prewarm(vfxRegistry.ConfettiPrefab, feelConfig.ConfettiPoolSize);
                    pool.Prewarm(vfxRegistry.MergeEffectPrefab, feelConfig.MergeEffectPoolSize);
                }
            }
            else
            {
                NexusLog.Error("GameplayLifecycle", nameof(OnInitializeAsync), "", 
                    "VfxPrefabRegistry not bound. VFX system will be non-functional.");
            }
        }

        private static void InitializeAudio(IContext context, IAudioService audio, PlayerProgressModel progress)
        {
            if (audio != null)
            {
                var db = context.Resolve<GameConfigDatabaseSO>();
                int currentLvl = progress?.CurrentLevel.Value ?? 1;
                int worldIdx = db.GetWorldForLevel(currentLvl);
                var bgm = ProceduralAudio.GetOrCreateBgmClip(worldIdx);
                audio.PlayBgm(bgm, true);
            }
        }

        public ValueTask OnStartAsync(CancellationToken ct)
        {
            var context = NexusRuntime.CurrentContext;
            var diag = context.TryResolve<IGameDiagnostics>();
            diag?.Checkpoint("GameplayLifecycle.OnStartAsync");
            diag?.Log("Lifecycle", "GameplayLifecycle.OnStartAsync started");

            EnsureInputSetup(context);
            EnsureGlobalErrorHandling(context);
            ValidateDiIntegrity(context);

            diag?.Log("Lifecycle", "GameplayLifecycle.OnStartAsync completed");
            return default;
        }

        private static void EnsureInputSetup(IContext context)
        {
            if (EventSystem.current == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // P0 fix: force camera to correct position/angle regardless of scene defaults.
            var mainCam = context.TryResolve<Camera>();
            if (mainCam != null)
            {
                var feel = context.Resolve<GameFeelConfigSO>();
                mainCam.gameObject.tag = "MainCamera";
                mainCam.orthographic = true;
                mainCam.orthographicSize = feel.CameraBaseOrtho;
                mainCam.transform.position = feel.CameraPosition;
                mainCam.transform.rotation = Quaternion.Euler(feel.CameraRotation);
            }
            else
            {
                NexusLog.Error("GameplayLifecycle", "EnsureInputSetup", "",
                    "MainCamera not found — cannot configure camera.");
            }

            foreach (var cam in Camera.allCameras)
            {
                if (cam != null && cam.GetComponent<PhysicsRaycaster>() == null)
                    cam.gameObject.AddComponent<PhysicsRaycaster>();
            }
        }

        private static void EnsureGlobalErrorHandling(IContext context)
        {
            var signalBus = context.TryResolve<ISignalBus>();
            if (signalBus == null) return;

            signalBus.Subscribe<CommandFailedSignal>(signal =>
            {
                NexusLog.Error(signal.SourceCommand?.Name ?? "UnknownCommand",
                    "Execute",
                    signal.SourceSignal?.ToString() ?? "null",
                    $"Command failed: {signal.Exception?.Message}\n{signal.Exception?.StackTrace}");
            });
        }

        private static void ValidateDiIntegrity(IContext context)
        {
            // Poll known-critical singletons at boot — missing bindings fail loud, not silent at runtime.
            const string Prefix = nameof(ValidateDiIntegrity);
            TryWarnNull(context, typeof(GameplayModel), Prefix);
            TryWarnNull(context, typeof(PlayerProgressModel), Prefix);
            TryWarnNull(context, typeof(SettingsModel), Prefix);
            var diag = context.TryResolve<IGameDiagnostics>();
            diag?.Log("Lifecycle", "DI integrity check completed.");
        }

        private static void TryWarnNull(IContext context, System.Type type, string prefix)
        {
            var method = typeof(IContext).GetMethod("TryResolve")?.MakeGenericMethod(type);
            if (method == null) return;
            var result = method.Invoke(context, null);
            if (result == null)
                NexusLog.Warn("GameplayLifecycle", prefix, type.Name,
                    "Resolve returned null. Missing binding or [Inject] field?");
        }

        public void OnDispose()
        {
            FlushSave();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                FlushSave();
        }

        private static void FlushSave()
        {
            var context = NexusRuntime.CurrentContext;
            if (context == null) return;

            // Prevent double-tracking: OnApplicationPause + OnDispose can fire back-to-back
            if (!s_sessionEndTracked)
            {
                s_sessionEndTracked = true;
                float sessionLen = Time.realtimeSinceStartup - s_sessionStartTime;
                var analytics = context.TryResolve<IAnalyticsService>();
                if (analytics != null)
                {
                    analytics.LogEvent("session_end", new[]
                    {
                        ("session_length", ((int)sessionLen).ToString())
                    });
                }
            }

            // Auto-flush save on dispose — ensure no loss when player quits to main menu.
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

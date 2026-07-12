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
            var db = Resources.Load<GameConfigDatabaseSO>(GameplayAssetKeys.GameConfigDatabase);
            if (db == null) throw new System.InvalidOperationException("GameConfigDatabase.asset not found in Resources!");
            builder.BindInstance<GameConfigDatabaseSO>(db);

            var feel = Resources.Load<GameFeelConfigSO>(GameplayAssetKeys.GameFeelConfig);
            if (feel == null) throw new System.InvalidOperationException("GameFeelConfig.asset not found in Resources!");
            builder.BindInstance<GameFeelConfigSO>(feel);

            DoTweenCapacityBootstrap.EnsureInitialized(feel.DoTweenTweensCapacity, feel.DoTweenSequencesCapacity);

            var palette = Resources.Load<RingColorPaletteSO>(GameplayAssetKeys.RingColorPalette);
            if (palette == null) throw new System.InvalidOperationException("RingColorPalette.asset not found in Resources!");
            builder.BindInstance<RingColorPaletteSO>(palette);

            var theme = Resources.Load<UIThemeConfigSO>(GameplayAssetKeys.UIThemeConfig);
            if (theme == null) throw new System.InvalidOperationException("UIThemeConfig.asset not found in Resources!");
            GameUIResources.Bind(theme);
            builder.BindInstance<UIThemeConfigSO>(theme);

            var audioConfig = Resources.Load<AudioConfigSO>(GameplayAssetKeys.AudioConfig);
            if (audioConfig == null) throw new System.InvalidOperationException("AudioConfig.asset not found in Resources!");
            ProceduralAudio.Initialize(audioConfig);
            builder.BindInstance<AudioConfigSO>(audioConfig);

            var mainCamera = Camera.main ?? Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include);
            if (mainCamera != null) builder.BindInstance<Camera>(mainCamera);

            // -------------------- Storage --------------------
            builder.Bind<IPlayerPrefsService, EncryptedStorageService>();
            builder.Bind<ILocalizationTableProvider, CSVLocalizationTableProvider>();

            // -------------------- Core Services --------------------
            builder.BindService<ILoggerService, LoggerService>();
            builder.BindService<IEconomyService, EconomyService>();
            builder.BindService<IProgressionService, ProgressionService>();
            builder.BindService<IAudioService, AudioService>();
            builder.BindService<IHapticService, HapticService>();
            builder.BindService<IFeedbackService, FeedbackService>();
            builder.BindService<ILocalizationService, LocalizationService>();
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

            // -------------------- Commands --------------------
            builder.BindCommand<InitLevelSignal, InitLevelCommand>();
            builder.BindCommand<SelectPoleSignal, SelectPoleCommand>();
            builder.BindCommand<MoveRingSignal, MoveRingCommand>();
            builder.BindCommand<UndoSignal, UndoCommand>();
            builder.BindCommand<UndoRequestedSignal, UndoRequestedCommand>();
            builder.BindCommand<CheckWinSignal, CheckWinCommand>();
            builder.BindCommand<LevelWonSignal, LevelWonCommand>();
            // HintCommand runs solver off-thread via LevelSolver.SolveAsync, hence async binding.
            builder.BindAsyncCommand<HintRequestedSignal, HintCommand>();
            builder.BindCommand<ChestClaimAllSignal, ChestClaimCommand>();
            builder.BindCommand<DailyRewardClaimSignal, DailyRewardClaimCommand>();
        }

        public async ValueTask OnInitializeAsync(CancellationToken ct)
        {
            var visualBoard = GameplayHelpers.FindRootGameObject("RingFlow_VisualBoard");
            if (visualBoard != null)
            {
                UnityEngine.Object.Destroy(visualBoard);
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
            PoleState.SetValidationManager(context.Resolve<RingValidationStrategyManager>());

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
            fsm.RegisterState(context.Resolve<LoadingState>());
            fsm.RegisterState(context.Resolve<ErrorState>());
        }

        private static void HookSettingsToServices(IContext context, SettingsModel settings, IAudioService audio, IHapticService haptics)
        {
            var localization = context.TryResolve<ILocalizationService>();
            var provider = context.TryResolve<ILocalizationTableProvider>();

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
                if (provider != null)
                {
                    var languages = new[] { "en", "tr", "id", "es", "fr", "de", "pt", "it", "ar", "hi", "ru", "ja", "zh", "ko", "vi" };
                    foreach (var lang in languages)
                    {
                        if (provider.TryGetTable(lang, out var table) && table != null)
                        {
                            localization.RegisterLanguageTable(lang, table);
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
            string firstLaunchStr = prefs.GetString(GameplayAssetKeys.PlayerPrefs.FirstLaunchTime, "");
            System.DateTime firstLaunch;
            if (string.IsNullOrEmpty(firstLaunchStr))
            {
                firstLaunch = System.DateTime.UtcNow;
                prefs.SetString(GameplayAssetKeys.PlayerPrefs.FirstLaunchTime, firstLaunch.ToString("o"));
                prefs.Save();
            }
            else
            {
                System.DateTime.TryParse(firstLaunchStr, out firstLaunch);
            }

            int daysSinceFirstLaunch = (System.DateTime.UtcNow - firstLaunch).Days;
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
            if (iap != null)
            {
                iap.RegisterProducts(
                    new ProductDefinition { Id = "remove_ads", Type = ProductType.NonConsumable, PriceString = "$3.99" },
                    new ProductDefinition { Id = "coins_100", Type = ProductType.Consumable, PriceString = "$0.99" },
                    new ProductDefinition { Id = "diamonds_50", Type = ProductType.Consumable, PriceString = "$0.99" }
                );
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
                    pool.Prewarm(vfxRegistry.RingPopPrefab, feelConfig?.RingPopPoolSize ?? 50);
                    pool.Prewarm(vfxRegistry.ConfettiPrefab, feelConfig?.ConfettiPoolSize ?? 30);
                    pool.Prewarm(vfxRegistry.MergeEffectPrefab, feelConfig?.MergeEffectPoolSize ?? 20);
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

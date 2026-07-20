# Ring Flow Nexus Architecture Contract

## 1. Composition Root

`GameplayLifecycle.OnConfigure` is the only gameplay composition root. It binds configuration instances, interfaces/services, models, FSM states, and signal-to-command mappings. Feature code never constructs gameplay services, resolves a global context, searches a scene, loads a resource, or uses static mutable state to obtain a dependency.

Required dependencies are injected. Missing required content/configuration fails the content readiness gate and enters `Error`; no fallback object, default config, or runtime-generated asset is accepted.

## 2. Ownership

| Layer | Owns | Must not own |
| --- | --- | --- |
| ScriptableObject/catalog | Immutable authored configuration/content | Runtime, player, session state |
| `GameplayModel` | Active board/session/undo state | Unity/UI references |
| `PlayerProgressModel` | Persistent profile/progression | Active board or View references |
| `SettingsModel` | User preferences | Gameplay rules/state |
| Command | One deterministic state transaction | Permanent state/UI/animation |
| Signal | Immutable intent or past event payload | Behavior/model mutation |
| Mediator | Presentation coordination/subscriptions | Permanent gameplay state/rule logic |
| View | Rendering/input presentation/pooling | Game state, validation, service lookup |
| Service | Injected domain/infrastructure capability | UI ownership or gameplay singleton state |

## 3. Signal and Command Pipeline

Views emit value-type intent signals. `OnConfigure` binds each intent to one authoritative command path. Commands validate, mutate a model, create undo data, and emit past-tense result signals. Mediators subscribe through Nexus lifecycle APIs and update views from result signals and read-only observable state.

Sync gameplay commands use synchronous dispatch. Signals bound to async commands are dispatched with the async SignalBus APIs only. Async work has cancellation/error handling and cannot make gameplay outcomes timing-dependent.

## 4. State Machine

Only `IGameStateMachine` changes product state. Valid states are `Boot`, `Splash`, `MainMenu`, `LevelSelect`, `WorldMap`, `Loading`, `Playing`, `Paused`, `Win`, `GameOver`, `Lose`, and `Error`. `Lose` carries the ruleset outcome and enters generic `GameOver` presentation with reason data. Input is enabled only by the owning state. Loading/transition failures route to `Error` with diagnostics, not a gameplay fallback.

## 5. Content Readiness and Loading

Bootstrap reads exactly one bootstrap-safe immutable `BuildProfileId` serialized for the build target, then loads the matching versioned `ContentManifestSO` asynchronously through an injected asset service. Missing or ambiguous profile/manifest resolution transitions to `Error`. The manifest is the only catalog/content ID resolver. Before `Playing`, it verifies level/ruleset/config versions, references, localization, screen contracts, and pool readiness. Gameplay code does not call `Resources.Load`, Addressables directly, `Find*`, `Camera.main`, or a service locator.

Assets are loaded asynchronously, owned by an explicit loader/pool owner, and released on defined scene/screen boundaries. Runtime `Instantiate`/`Destroy` and generated primitive fallbacks are prohibited on gameplay paths.

## 6. Views, Presentation, and Pools

Views render immutable presentation data. Mediators own subscriptions, cancellation tokens, and view lifecycle cleanup. Animation/audio/VFX receive past-tense events after a command commits; they never gate gameplay. Undo/restart/pause/unbind cancels active presentation work and returns pooled instances through their owner.

Each pool has a catalog key, warm count, max count, owner, and explicit overflow policy. Pool exhaustions are diagnostics/content-capacity failures, not allocation fallbacks.

## 7. Data, Save, and Replay

Immutable level content is separate from `GameplayModel` and profile models. Save DTOs are explicit and versioned, migration is ordered, and restore verifies content fingerprints. Replays use level/ruleset/config identity, canonical intents, and a deterministic tick stream when applicable; the same rules engine verifies them. Autosave takes an atomic post-command model snapshot after initialization, committed moves, pause, suspend, quit, boss completion, and reward collection. It is coalesced/cancellable and cannot block or mutate gameplay.

## 8. Editor and CI Gates

One `ValidateContent` entry point validates all cross-asset references, IDs, schema versions, level/ruleset compatibility, solver results, screens, localization, accessibility, pool budgets, exactly-one build profile, and content fingerprints. It runs on asset edits, before Play, generation, and CI/build. Errors block progress; validation never mutates input to make it appear valid.

## 9. Performance and Testing

Gameplay allocations are zero after preload. LINQ, reflection, closures, boxing, polling, and `Update` loops are prohibited unless measured and explicitly approved. Every feature has unit, integration, solver/generator, undo, save/replay, and performance coverage appropriate to its impact. Async signal flows require PlayMode integration tests.

## 10. Migration Policy

Existing synchronous resource loading, fallback content construction, static/service-locator access, scene/camera lookup, and runtime-growing pools are migration debt. They are not patterns to reuse. Editor tooling may inspect or author content, but runtime gameplay must not create new level assets or rely on editor-only generation paths. Cutover order is manifest/asset service, content validation, pool capacity enforcement, bootstrap DI cleanup, then fallback removal. Each stage has forbidden-new-use scans, save/replay compatibility tests, and performance evidence; new code must follow this contract immediately.

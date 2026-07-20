# Ring Flow Production GDD

## 1. Product Contract

Ring Flow is a portrait, deterministic ring-sorting puzzle game. It is fully data-driven: designers author content and balance in validated assets; runtime code executes that content through Nexus MVCS. The game must never invent level, rule, visual, or balance data at runtime to conceal invalid content.

This document is the product and content contract. `GAME_RULES.md` defines deterministic gameplay behavior. `ARCHITECTURE.md` defines the Nexus ownership and execution contract. When documents disagree, the gameplay rules and architecture contracts take precedence until this GDD is corrected.

### Definition of done

A level or configuration is production-ready only when content validation, solver validation, generator/solver parity, command/undo tests, save/replay tests, PlayMode signal/FSM tests, and defined performance measurements pass. A visually plausible level is not valid content without these results.

## 2. Non-Negotiable Principles

- The same content identity, ruleset version, generator version, and seed produce the same level definition on every supported platform.
- Only commands mutate authoritative gameplay state. Views, mediators, signals, ScriptableObjects, animation, audio, VFX, and analytics do not mutate it.
- Every player action is deterministic, rejected explicitly when illegal, and exactly undoable.
- The generator, solver, editor validator, and runtime command validation use the same Unity-free rules API.
- All required content is authored, versioned, referenced by stable IDs, and validated before entering `Playing`.
- Missing required content is a structured content error that transitions to `Error`; it is never replaced with a default, a primitive, a generated prefab, a last-entry selection, or a fallback string.

## 3. Content Ownership and Data Model

### 3.1 Immutable authored data

`LevelDataSO` is a catalog-authored immutable wrapper around serialized level content. It is not a runtime generator output, cache, or mutable session container. `LevelDefinition` is the Unity-free immutable DTO used by rules, generator, solver, replay, and validation.

- `LevelId`, `ContentSchemaVersion`, `RuleSetId`, `RuleSetVersion`, and content hash.
- Source identity: authored definition or `(GeneratorVersion, TemplateId, Seed)`.
- Ordered pole definitions, capacities, lock/portal metadata, and ordered `RingData` values.
- Level type, tutorial definition ID, challenge definition ID, theme/palette/layout profile IDs, and mechanic allow-list IDs.
- Solver result metadata: input hash, solver version, solution length, metrics, and computed difficulty score.

`LevelDataSO` never stores selected pole, current board, completion, player unlocks, coins, time, challenge progress, undo history, replay, or solver cache accepted without hash validation. `RuleReferences` must be replaced by typed/stable rule IDs and versions; arbitrary Unity object references and free-form rule strings are forbidden.

The generator runs only in editor/CI. It produces a `LevelDefinition` and content hash; an explicit content-pipeline action persists the reviewed result into a `LevelDataSO`, inserts it into the catalog, and validates build inclusion. Runtime may load and replay manifest content only; it never generates, mutates, or creates level assets.

### 3.2 Runtime and persistent data

`GameplayModel` owns the active board, selection, move count, win/loss state, special-mechanic state, challenge session state, and undo history. `PlayerProgressModel` owns profile progression, unlocks, economy, and completion history. `SettingsModel` owns player preferences only.

Persistent DTOs are versioned and explicit:

- `LevelSessionSave`: save schema version, level identity/fingerprint, complete ordered board/pole state, completed poles, selection, pending mechanic state, counters, target/last reward, terminal state, challenge tick/counters, and an explicit undo-history policy/data.
- `PlayerProgressSave`: profile schema version and progression only.
- Replay: level identity, ruleset/config versions, seed when generated, deterministic tick stream when timing is enabled, and ordered canonical player intents (`Select`, `Deselect`, `Move`, `Undo`, `Restart`). A replay never depends on UI timing or visual state.

Migration is ordered and tested. An incompatible or corrupt save is reported and recoverably discarded only by an explicit policy; migration never invents missing gameplay content.

### 3.3 Authoritative catalogs and configurations

The editor exposes one validated content graph with stable IDs, deterministic ordering, schema versions, and referential integrity:

- Level catalog: campaign/tutorial tracks, ordering, unlock rules, level IDs.
- Ruleset catalog: legal-move and special-mechanic rule versions.
- Game configuration: difficulty bands, generation recipes/limits, economy, world progression, and challenge definitions.
- Presentation catalog: theme, typography, palette including color-vision variants, layout/camera profile, screen definition, audio/VFX events, and pool budgets.
- Localization catalog: language metadata, RTL metadata, tables and formatted keys.

`ContentManifestSO` is the single root asset selected by a versioned build profile. It contains active catalog versions, content hashes, addressable content IDs, and deterministic ID lookup tables. Each build target serializes exactly one bootstrap-safe immutable `BuildProfileId` reference before content loading. Bootstrap validates exactly one matching profile/manifest; zero or multiple matches transition to `Error`. Unknown, duplicate, retired, or version-incompatible IDs are content errors, never fallback selections.

`RuleSetDefinition` contains its stable ID/version, canonical move ordering, mechanic definitions, legal combinations, serialization layout, and compatibility range. An injected Unity-free `IRuleSetResolver` maps an exact `(RuleSetId, RuleSetVersion)` to an implementation. A missing/retired mapping fails content readiness. Ruleset retirement requires a tested save/replay migration or an explicit recovery policy.

`ContentFingerprint` is shared by authored and generated `LevelDefinition`, ruleset, configuration, and declared gameplay dependencies. It specifies canonical field order, null/collection representation, fixed-width numeric encoding, float prohibition or rounding, hash algorithm/version, and dependency-hash order. Validation recomputes it from source data; any fingerprint change invalidates cached solver results and requires explicit save/replay compatibility handling.

ScriptableObjects contain configuration and static content only. They never contain mutable session/player state. No production asset may depend on editor-only initialization defaults.

## 4. Core Gameplay Rules

The complete normative behavior is in `GAME_RULES.md`. At minimum, a legal move takes exactly one top-most movable ring from a non-empty source pole and places it on an empty, unlocked target with remaining capacity or on a compatible target according to the active ruleset. Invalid moves leave the model unchanged and produce an explicit rejection result.

The win predicate, loss predicate, capacity behavior, empty-pole constraints, and every special mechanic are ruleset functions. They are never inferred from view geometry, animation completion, text, or level-index conventions.

Every mechanic added to a level must declare its validation rule, deterministic effect order, mutable state, undo state, solver projection, serialization projection, presentation events, allowed difficulty bands, and editor validation rules. An unclassified mechanic cannot be shipped.

## 5. Deterministic Generation, Solver, and Difficulty

### 5.1 Shared rules engine

`IRingRules` is Unity-free and keyed by `RuleSetId + RuleSetVersion`. It owns legal move generation, move application, terminal predicates, special-mechanic behavior, canonical board encoding, and deterministic ordering. Runtime commands, generator, solver, replay verifier, and editor validator call this API; duplicated rule implementations are forbidden. Mechanics are selected from a finite versioned declarative schema and resolved by the injected ruleset registry; designer data selects supported behavior but never arbitrary scripts.

### 5.2 Generator contract

Generator input is `(GeneratorVersion, RuleSetId, RuleSetVersion, TemplateId, Seed, ConfigVersion)`. It uses a specified seeded PRNG algorithm, fixed-width integer/overflow semantics, deterministic attempt-seed derivation, and bounded attempts. Canonical serialization, hash algorithm, field ordering, and float rounding/prohibition are versioned. Output is exactly one immutable `LevelDefinition` plus a content hash, or an explicit generation failure. Time, device state, Unity random state, and runtime player data are not inputs.

Generated content passes only if it is structurally valid, solvable, has a non-dead start, satisfies all band constraints, and has difficulty metrics computed from the same solver/ruleset. Failed candidates are rejected, never silently simplified.

### 5.3 Solver and hints

Solver input is a canonical immutable snapshot captured on the main thread plus ruleset/config identity. Worker execution receives no model, view, Unity, or mutable collection references. Output includes solvable status, ordered moves, move count, branching/forced-move/dead-end metrics, solver version, and input hash. Cancellation is distinct from unsolvable. The result is discarded if its input hash is stale when it returns. Search limits and cancellation behavior are configuration, not hidden constants.

Hints show at most one recommended legal next move from a validated near-optimal path. They do not mutate the board, reveal a full path, or depend on view state.

### 5.4 Difficulty

Difficulty is computed and stored with a `DifficultyFormulaVersion`. Inputs include colors, poles, capacities, empty poles, special mechanics, solver depth, branching factor, forced-move ratio, dead-end probability, and state entropy. A difficulty band supplies explicit permitted ranges and target metric ranges; a label alone is not sufficient.

## 6. Nexus MVCS and State Flow

The mandatory gameplay path is:

`View -> intent signal -> command -> GameplayModel mutation -> past-tense result signal -> mediator -> view`

Views issue intent only. Commands validate and mutate the model in one transaction, append the undo record, then emit result signals. Signals are immutable value payloads and contain no behavior. Mediators observe signals/read-only model data and invoke presentation methods only. All dependencies are injected at the composition root.

Gameplay mutation is accepted only in `Loading` for deterministic level/session initialization and in `Playing` for player/gameplay transactions; the FSM owns all transitions. Supported states are `Boot`, `Splash`, `MainMenu`, `LevelSelect`, `WorldMap`, `Loading`, `Playing`, `Paused`, `Win`, `GameOver`, `Lose`, and `Error`. `Lose` is the ruleset outcome and enters generic `GameOver` presentation with a loss reason. Each transition has tested entry data, input gate, and exit action. The GDD must be updated before adding, removing, or bypassing a state.

Async work is limited to loading, saving, solver/hint work, UI transition, and external services. Async timing cannot alter a gameplay result. A signal bound to an async command is dispatched through the async SignalBus API and has defined cancellation and error handling.

## 7. Tutorial, Campaign, and Challenge Content

Tutorial levels use the same immutable level schema and ruleset as campaign levels. Tutorial direction is authored as a data sequence of stable step IDs, trigger conditions, permitted intents, and localized presentation keys; it does not contain separate gameplay rules or mutate views directly.

Campaign progression, tutorial completion, and challenge progress are profile/session data, never fields on a level asset. Challenge definitions are static content; live counters and outcomes belong to the gameplay model and save DTO.

## 8. Presentation, UI, and Accessibility

`BoardView` renders a mediator-provided presentation projection using injected immutable presentation configuration. It may pool, position, animate, and recycle visual objects; it may not select levels, load assets, calculate legal moves, own board state, or mutate models.

Animation, audio, haptics, and VFX consume post-command signals. They are cosmetic and cancellable on undo, restart, pause, unload, and pool recycle. Skipping or failing a presentation effect cannot affect gameplay completion.

The screen catalog defines each `ScreenId`, exclusive/overlay behavior, expected view/mediator contract, addressable content ID, input gate, and pool/release lifecycle. Screen, prefab, audio, VFX, material, shader, font, and sprite references are required unless explicitly marked optional by product data.

The game supports portrait safe areas, tested aspect classes, color-vision palettes, non-color ring identity cues, reduce motion, large UI/text reflow, readable contrast, touch-target sizing, localization, RTL, and pseudo-localization/truncation testing. User-facing text comes only from localization keys; views do not own fallback copy. Any localized screen, popup, or button must resolve its visible text through the localization catalog before entering `Playing`.

## 9. Asset, Pool, and Performance Contract

All gameplay/presentation assets needed in `Playing` are asynchronously resolved and warmed before input is enabled. Every pool defines asset ID, warm size, maximum size, owner, recycle conditions, and deterministic overflow behavior. Runtime `Resources.Load`, `Instantiate`, `Destroy`, scene search, primitive synthesis, and content substitution are forbidden on gameplay paths.

Content readiness is a hard gate before `Playing`: catalog/config/schema/version compatibility, required references, addressable membership, pool readiness, localization coverage, and level validation must pass. Failure creates a diagnostic report and transitions to `Error`.

An optional asset has an authored `OptionalContentPolicy` defining allowed absent behavior, event scope, and validation result. It may suppress only its own cosmetic event; it cannot replace required content, alter state, or hide an invalid reference.

Production measurements use versioned low/mid/high board fixtures, a named device/OS/build-profile matrix, a documented warm-up period, fixed interaction/replay script, sample duration, p95/max aggregation, and profiler capture. The acceptance limits are 60 FPS, <=16.6 ms frame time, 0 B gameplay GC, <150 MB memory, <80 draw calls, <500 ms scene transition, and <3 s cold start. No claim passes without the matching capture. Gameplay, UI, and editor tooling must each justify their own allocations and must not weaken these limits by introducing alternative per-frame budgets.

## 10. Editor, CI, and Validation

`ValidateContent` runs on relevant asset edits, before generation, before Play, and in CI/build. Errors block level save/build/play; warnings are only for explicitly optional content. It validates:

- schema/version/ID uniqueness, ranges, ordering, and all cross-asset references;
- level board invariants, allowed mechanics, catalog placement, and difficulty-band constraints;
- generator/solver/ruleset parity and solver hashes;
- screen registry contracts, addressable assets, prefab/view/mediator mapping, and pool budgets;
- theme/palette/contrast/mesh/material/audio/VFX completeness;
- localization key coverage, format arguments, RTL and pseudo-localization;
- safe-area, touch-target, aspect, and reduce-motion presentation snapshots.

No validator silently repairs, normalizes, or substitutes authored data. It returns deterministic actionable diagnostics.

## 11. Test Acceptance Matrix

`TestMatrix` and `PerformanceProfile` are versioned authored assets. `TestMatrix` maps change classes to mandatory test fixture IDs and CI failure conditions: ruleset/command -> deterministic, undo, solver/replay; content/config -> validation and solver parity; save/schema -> migration/round-trip/replay; presentation -> mediator/accessibility snapshot; async/FSM -> PlayMode; pool/performance -> GC/profile fixture. `PerformanceProfile` names concrete device/OS/build-profile IDs, board fixtures, warm-up, script, duration, aggregation, and thresholds. CI fails when a required matrix entry is missing or fails.

Autosave occurs after level initialization, committed player transaction, pause, application suspend, quit, boss completion, and reward collection. It captures an atomic post-command model snapshot, is coalesced/cancellable without blocking gameplay, and reports failure without changing authoritative state.

## 12. Migration Debt

The current project contains fallback resource paths, runtime-created visuals, synchronous resource loading, service-locator/static patterns, scene/camera access, and pools that can allocate or grow after preload. These are legacy migration debt, not approved architecture. New work must not copy them. Cutover order is: manifest/asset service, content validation gate, pool capacity enforcement, bootstrap DI cleanup, then fallback removal. Each cutover has a forbidden-new-use scan, migration compatibility test, and performance evidence; until complete, documentation describes the target, not current runtime compliance.

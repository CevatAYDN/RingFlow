# RING FLOW — Architecture & Mechanics Audit Report

**Date:** 2026-07-08
**Scope:** Full codebase analysis against `RING_FLOW_GDD_PRODUCTION.md` v1.0
**Methodology:** Direct file read, AST-grep pattern matching, structural cross-reference, LSP diagnostics
**Status:** 5 critical bugs fixed, 3 high-priority improvements applied, remaining items tracked below

---

## Executive Summary

The RING FLOW codebase has a **solid foundation**: Nexus MVCS framework is used correctly in most places, FSM/Command/Signal layering is clean, model serialization is encrypted (AES-128 + HMAC), and the solver is a 0-GC IDA* implementation. However, the project shipped with several **dead code paths** (the new DailyReward popup was never wired), **subtle logic bugs** (Paint undo missing the painted-target color), and a few **god classes** (MoveRingCommand 200+ lines). All CRITICAL items are now fixed; the table below summarizes.

| Severity | Total | Fixed | Remaining |
|----------|-------|-------|-----------|
| CRITICAL (game-breaking) | 5 | 5 | 0 |
| HIGH (architecture) | 7 | 3 | 4 |
| MEDIUM (UX/lifecycle) | 8 | 4 | 4 |
| LOW (polish) | 3 | 0 | 3 |

---

## CRITICAL — All Resolved

### C1. DailyReward popup never shown
**Files:** `UIRoot.cs:91`, `MainMenuState.cs:21`, `GameplaySignals.cs:42`, `DailyRewardPopupMediator.cs:46`
**Status:** ✅ Fixed in this session
- Added `OpenDailyRewardSignal/CloseDailyRewardSignal/CloseSettingsSignal` struct signals.
- `UIRoot` now implements a popup pattern with `_pausedExclusiveScreen` state, so dismissing a popup restores the underlying screen (MainMenu, Playing, etc.) instead of leaving a blank canvas.
- `MainMenuState.OnEnterAsync` now fires `ShowScreenSignal(ScreenType.DailyReward)` if `DailyRewardService.CanClaimNow()`, so the popup pops up on every cold start with a claimable reward.
- `MainMenuView` got a new "DAILY REWARD" button (with red-dot badge when claimable).

### C2. DailyRewardMediator.Close quit the game
**File:** `DailyRewardPopupMediator.cs:46`
**Status:** ✅ Fixed
- Was: `SignalBus.Fire(new QuitToMenuRequestedSignal())` — clicking CLOSE quit to main menu, killing the popup.
- Now: `SignalBus.Fire(new HideScreenSignal(ScreenType.DailyReward))` — closes only the popup overlay, restores the underlying screen.

### C3. DailyRewardMediator bypassed the Command pattern
**File:** `DailyRewardPopupMediator.cs:32`
**Status:** ✅ Fixed
- Was: `Mediator` directly called `_daily.Claim()` and `_economy.Earn()`.
- Now: `Mediator` is read-only (just renders preview), fires `DailyRewardClaimSignal`. The existing `DailyRewardClaimCommand` (already bound in `GameplayLifecycle.OnConfigure`) does the actual claim + economy mutation. This means any future trigger (push notification, scheduled task, ad-as-reward) shares one code path.

### C4. DailyRewardClaimCommand dropped Hint and Theme rewards
**File:** `DailyRewardClaimCommand.cs`
**Status:** ✅ Fixed
- The reward table includes Hint (1 free undo) and Theme (random theme unlock) per GDD §9, but the command only handled `Coins` and `Diamonds`. Hint/Theme were silently dropped.
- Now the command handles all 4 reward types.

### C5. Paint undo lost the painted-target color
**Files:** `GameplayModel.cs:48`, `GameplayCommands.cs:269`, `GameplayCommands.cs:411`
**Status:** ✅ Fixed
- Was: `MoveRecord` only tracked the moving ring's `OriginalColor`. The painted TARGET ring (at the bottom of the destination pole) was not restored on undo — the player would see a red ring that should be blue.
- Now: `MoveRecord` tracks `PaintedRingIndex` and `PaintedRingOriginalColor`. `UndoCommand` restores the painted ring at its recorded index.

---

## HIGH — Architecture & Coupling

### H1. MoveRingCommand god class
**File:** `GameplayCommands.cs:137` (was 343 lines)
**Status:** ✅ Refactored
- Was: 200+ line `Execute` method with 11 special-ring rules inline.
- Now: 7 focused helper methods (`TryReserveChainCapacity`, `ApplyPaintPreMove`, `ApplyRainbowPreMove`, `RevealMysteryOnFrom`, `TryBreakIceOnTarget`, `ApplyChainSubMove`, `ApplyMagnetPull`, `TickAllBombs`) plus a private `MoveContext` struct. Main `Execute` is now ~30 lines and reads top-to-bottom as the GDD sequence.

### H2. NexusGeneratedBinder drift
**File:** `NexusGeneratedBinder.g.cs`
**Status:** ✅ Synchronized
- Added field entries for: `DailyRewardClaimCommand._progress`, `MainMenuMediator._dailyReward`, `HintCommand._ads`.
- Removed obsolete `DailyRewardPopupMediator._economy`.
- Updated `PreserveMembers()` IL2CPP AOT section.

### H3. BoardMediator manual IDisposable list
**File:** `BoardMediator.cs`
**Status:** ✅ Fixed
- Was: manually managed `_subscriptions` list with custom `OnUnbind` cleanup.
- Now: uses base class `Mediator<T>.Subscribe<T>(handler)` which auto-disposes. Unused `_signalBus` field removed.

### H4. HintCommand duplicate method + missing ad fallback
**File:** `HintCommand.cs`
**Status:** ✅ Fixed
- Was: 2 methods with the same name (`BuildBoardStateFromModelFromPoles` vs `BuildBoardStateFromModelFromPolesStatic`), and the command only tried coin spend, never the rewarded-ad path.
- Now: single `BuildBoardStateFromModel` method, proper fallback chain (coin → rewarded ad → empty hint) per GDD §9 "50 coin VEYA rewarded ad".

### H5. Chain mechanic dual-pop in original code
**File:** `GameplayCommands.cs:156` (pre-refactor)
**Status:** ⚠️ Partially refactored
- Was: `TryReserveChainCapacity` (in original) only checked the linked ring is on a different pole and that 2 slots exist. Then after the main move, `ApplyChainSubMove` (originally inline) re-searches for the linked ring. The search is correct but the "reserve" step was wasted work.
- Now: removed the redundant pre-reserve; the helper `ApplyChainSubMove` does the work in a single pass.

### H6. Bomb timer ticks ALL bombs per move
**File:** `GameplayCommands.cs` (TickAllBombs in refactor)
**Status:** ⚠️ Documented behavior
- GDD says "Sayaçlı (5→0), süre biterse patlar" — current behavior: EVERY bomb on the board ticks down by 1 on every move. The Counter is decremented regardless of whether the bomb was on the source/target pole. This is consistent with "global timer" semantics but the GDD is ambiguous; left as-is for the existing UX (every move = one tick).
- **TODO:** Confirm with design whether ticks should be per-move or per-touched-bomb.

### H7. `RingColor` enum conflates type and color
**File:** `LevelData.cs:3`
**Status:** ⚠️ Not refactored (high churn for low gain)
- The enum has `Key`, `Stone`, `Rainbow` as colors in addition to actual colors like Red/Blue. These are also `RingType` values. The "Golden Key Ring" should be a `RingType.Locked` with a special color, not a `RingColor.Key`.
- The current convention is internally consistent (PoleState.CanAddRing uses `RingType.Locked` to identify the key ring), so changing it would touch every `LevelGenerator`, `MoveRingCommand`, and `BoardView`. Deferred.

---

## MEDIUM — UX & Lifecycle

### M1. SettingsMediator quit to main menu
**File:** `SettingsMediator.cs:15`
**Status:** ✅ Fixed
- Was: Close button fired `QuitToMenuRequestedSignal`, which transitioned the FSM to MainMenu. So Settings popup acted like a "quit to menu" button.
- Now: Close button fires `CloseSettingsSignal`, UIRoot closes the popup and restores the underlying screen (MainMenu or Playing).

### M2. No DailyReward button in MainMenu
**File:** `MainMenuView.cs`, `MainMenuMediator.cs`
**Status:** ✅ Fixed
- Was: only Continue/Play/Levels/Settings buttons.
- Now: DailyReward button between Levels and Settings, with red-dot badge driven by `SetDailyRewardAvailable(canClaim)`.

### M3. Editor Window mixing concerns
**File:** `RingFlowEditorWindow.cs` (828 lines)
**Status:** ✅ Refactored in this session
- Was: layout/serialization/diagnostics/logic/reflection all in one file. Hard to maintain, hard to extend.
- Now: split into `RingFlowEditorWindow.cs` (window shell only) + 5 `EditorSection` partial classes (Generator, VisualBuilder, Runtime, Settings, Diagnostics). Each section owns its own state, header, content, and actions.
- ✅ EditorWindowEditorPrefs key constants moved to a static `EditorPrefsKeys` class.
- ✅ Reflection (`typeof(BoardView).GetField(...)`) replaced with `[SerializeField]` private fields on `BoardView` that the bootstrapper can write.
- ✅ Bootstrapper validation: refuses to run without active scene, refuses duplicates, reports missing prerequisites.

### M4. `[Preserve]` attributes missing on injected types
**Files:** multiple
**Status:** ⚠️ Documented for IL2CPP build
- For IL2CPP/AOT builds, `[Inject]`-attributed fields need `[Preserve]` so the linker doesn't strip them.
- The `NexusGeneratedBinder.g.cs` already covers `PreserveMembers()` for known fields.
- **TODO:** Audit per-type `[Preserve]` attributes; add where missing for production builds.

### M5. No IAnalyticsService tracking
**Files:** `Commands/`, `Mediators/`
**Status:** ✅ Implemented
- GDD §13 requires: `level_start, level_complete, hint_use, undo_use, restart_use, rewarded_ad, session_length, retention`.
- Was: zero analytics events.
- Now: added `AnalyticsEvents` static class with `TrackLevelStart/Complete/Hint/Undo/Restart/Ad` methods. Wired into the relevant commands and mediators.

### M6. No Restart functionality
**Files:** `HUDView.cs`, `HUDMediator.cs`, `GameplaySignals.cs`
**Status:** ✅ Implemented
- GDD §11 mentions Restart button; was no implementation.
- Now: `RestartLevelCommand` added; `RestartRequestedSignal` added; HUD Restart button wired to it; mediator subscribes.

### M7. WinMediator manual OnChanged
**File:** `WinMediator.cs`
**Status:** ✅ Fixed
- Was: manually subscribed to `_model.IsGameWon.OnChanged` and `_model.LastReward.OnChanged` without `OnUnbind` cleanup. Memory leak on view re-pool.
- Now: uses base class `Subscribe<T>`, auto-disposed.

### M8. No IAdService integration in Editor
**File:** `RingFlowEditorWindow.cs`
**Status:** ✅ Added
- Was: no way to test ad placements from the editor.
- Now: Editor Ad Tester section can trigger ShowRewarded / ShowInterstitial / ShowBanner for any placement and see the result.

---

## LOW — Polish

### L1. PoleState / BoardView have `protected virtual Awake`
**Files:** all `*View.cs` in `UI/`
**Status:** ⚠️ Not changed
- Base `View.Awake()` is `private` (not virtual), so derived `protected virtual Awake()` doesn't actually override anything — it shadows. Works in Unity (Unity dispatches by name, not virtual dispatch), but inconsistent with C# style.
- **TODO:** Either change base to `protected virtual Awake` and have all views call `base.Awake()`, or change all views to `private void Awake()` and add a separate `protected virtual void OnAwake()` hook.

### L2. Shared button style helpers
**Files:** each `*View.cs`
**Status:** ⚠️ Not extracted
- `ApplyOutlineStyle/ApplyPrimaryStyle/ApplyDangerStyle/ApplyIconStyle` are duplicated in 4-5 views.
- **TODO:** Move to `GameUIResources.ApplyOutlineStyle(...)` etc.

### L3. `FindAnyObjectByType<Root>()` in BoardView
**File:** `BoardView.cs:30`
**Status:** ⚠️ Not changed
- BoardView searches the scene for EventSystem and Camera. This is a runtime fallback for test environments. Production should have EventSystem created by the bootstrapper.
- **TODO:** Make EventSystem/camera injection points explicit on `BoardView`; fall back to search only if injection failed. This should remain a test-only escape hatch, not a gameplay dependency.

---

## Architectural Health Score

| Category | Score | Notes |
|----------|-------|-------|
| MVCS layering | 9/10 | Mediator/View/Command/Signal used consistently |
| DI discipline | 8/10 | `[Inject]` preferred; a few `Resolve<>` in non-bootstrap code |
| State machine | 9/10 | All transitions via `ChangeStateAsync<T>()` |
| Signal coverage | 8/10 | All HUD/UI signals wired; minor gaps (Restart) |
| Test coverage | 0/10 | EditMode tests exist for solver/commands but not for UI/mediators |
| Editor tooling | 7/10 | Refactored this session; needs UI test for bootstrapper |
| Performance | 9/10 | Solver is 0-GC, struct-based; UI uses primitive creation in Awake (no pooling) |
| Localization | 7/10 | CSV provider exists, 15 langs registered, but Views still hard-code English strings |

---

## Open Work After This Session

1. **Unity-side verification** — User must run Unity and confirm the build; the compile path is clean but runtime behavior (FSM transitions, popup restore) needs in-editor testing.
2. **NexusGenerator regeneration** — If Nexus has a re-generation hook for `NexusGeneratedBinder.g.cs`, my manual edits will be overwritten. Add a `// PRESERVE MANUAL ENTRIES` comment block to the file as a sentinel, or configure the generator to skip.
3. **Localization integration** — Views currently show hard-coded English strings. The `ILocalizationService` is bound and the CSV is loaded, but no View calls `localization.GetString("btn_play")` etc. High-effort refactor; recommend doing one screen as a proof-of-concept first.
4. **GDD §3 partial-pole win clarification** — Both `CheckWinCommand` and `LevelSolver.IsSolved` require `IsFull`. GDD says "her dolu pole tek renk" which is ambiguous. Confirm with design.
5. **Performance budget verification** — GDD §7 has hard limits (16.67ms frame, 1KB GC/frame). Need Profiler runs to verify.

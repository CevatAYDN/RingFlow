# Ring Flow Gameplay Rules Contract

## 1. Authority

This document defines the deterministic rules evaluated by the Unity-free shared rules engine. Runtime commands, generator, solver, replay verifier, and editor validator must use the same ruleset implementation and canonical board encoding.

## 2. State and Identity

A board is an ordered list of poles. A pole has a stable ID, capacity, lock/portal state, and ordered rings from bottom to top. A ring has a stable ruleset-supported type, color/semantic identity, and explicit additional data. IDs and enum values used by serialized content are never reordered or repurposed.

## 3. Base Move

A move is `(fromPoleId, toPoleId)` and moves exactly one top-most movable ring.

- Source and target must exist and be different.
- Source must be non-empty and the top ring must be movable under the active ruleset.
- Target must be unlocked, have capacity, and accept the ring under the active ruleset.
- The base rule accepts an empty target or a target whose top ring has the same compatible color/identity.
- A rejected move changes no board state, counter, timer, history, or presentation state and returns a rejection reason.

Rules are evaluated before model mutation. No rule may depend on frame time, wall-clock time, animation, UI, current locale, device, or unmanaged random state.

## 4. Transaction and Effect Order

Each accepted player move is one transaction:

1. Validate the base move and mechanic preconditions.
2. Capture the complete reversible delta or board snapshot.
3. Apply the primary move and deterministic mechanic effects in the ruleset-defined order.
4. Update counters and evaluate terminal predicates.
5. Record one undo entry for the player action.
6. Emit past-tense result events after state is authoritative.

Any mechanic effect that cannot be represented in this transaction is not permitted.

## 4.1 Canonical Non-Move Intents

`Select(poleId)` is legal only for an existing selectable pole. It changes only `SelectedPoleId`, does not increment moves, does not create an undo record, and emits `PoleSelected`.

`Deselect` clears `SelectedPoleId`, does not increment moves, does not create an undo record, and emits `PoleDeselected`. A rejected select/deselect leaves all state unchanged and emits an explicit rejection reason.

`Undo` restores one accepted player-action transaction exactly as defined in section 7; it is itself recorded in replay but does not create a new undo record. `Restart` restores the initial definition state and clears history; it is recorded in replay and emits `LevelRestarted`.

`AdvanceTick(tickIndex)` exists only for a time-enabled ruleset. It is legal only when `tickIndex` is exactly the next canonical integer tick and the session is not paused/terminal. It applies its ruleset-defined counter effects, evaluates terminal predicates after those effects, does not create an undo record unless the ruleset explicitly defines it as a player action, and emits `TickAdvanced`. Tick ordering relative to a player intent is recorded explicitly in replay, and replay systems that do not support time-enabled rulesets must reject `AdvanceTick` as unsupported content.

## 5. Win and Loss

`IsSolved` is a pure ruleset predicate. It defines exactly which poles may be empty, which completed poles must be uniform/full, and how special rings affect completion. It is the only win authority for command, solver, generator, and validator.

Loss is also a pure predicate. Challenge move limits and destructive mechanics define their exact counter tick and failure order in ruleset data. A loss never mutates progress directly; the command emits the result and the state machine handles presentation/flow.

Time-limited challenge content is valid only when time is a deterministic integer tick stream. Its source, rate, pause policy, save value, replay value, and command ordering are ruleset data. Wall-clock time is never a rule input.

## 6. Special Mechanics

Every special ring or pole mechanic has a versioned ruleset specification containing:

- legal-source/legal-target changes;
- pre-move and post-move effect order;
- additional state and canonical serialization;
- exact undo restoration;
- solver/generator projection and difficulty contribution;
- presentation event IDs only, never presentation behavior;
- permitted combinations, content validation, and test cases.

Unspecified mechanics are invalid content. A mechanic may extend the base rules but may not create an exception unknown to the solver, generator, undo, replay, or save system.

## 7. Undo and Restart

Undo restores the exact pre-transaction board, counters, selection, terminal state, and every special-mechanic effect. It is not a visual reverse. Undo emits a result event after restoration; mediators cancel/reconcile visual work from that event.

Restart restores the immutable level definition's initial board through the same initialization command path. It clears session-only history and does not regenerate or silently alter authored content.

## 8. Generator and Solver

The generator uses only declared input `(versions, template, seed, config)` and a fixed seeded PRNG. It has bounded retries and returns either a content-hashed valid definition or failure.

The solver consumes canonical state plus ruleset identity and returns a deterministic ordered path and metrics. Canonical move ordering is part of the ruleset. Search limits/cancellation are configuration; cancellation never becomes an incorrect unsolvable result.

## 9. Replay and Save

Replay reproduces a level from immutable content identity/version, ordered canonical intents (`Select`, `Deselect`, `Move`, `Undo`, `Restart`, `AdvanceTick`), and a deterministic tick stream when timing is enabled. The verifier replays the inputs through the same rules engine and rejects a hash/version mismatch.

An active-session save stores authoritative state separately from immutable level content. Restore validates the content fingerprint before applying it; it never guesses a substitute level or rule.

## 10. Required Invariants

- A ring occupies one pole and one ordered position.
- A pole never exceeds capacity.
- All authoritative state is serializable without Unity object references.
- The same canonical input produces the same result and events.
- Invalid intent is non-mutating.
- Every accepted player action is undoable.
- Every generated/authored level is validated and solvable under its declared ruleset.

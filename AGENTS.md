# Table of Contents

- Mission
- Project Vision
- Engineering Philosophy
- Golden Rules
- AI Responsibilities
- Coding Style
- File Organization
- Architecture
- Gameplay Rules
- Level Generator
- Solver Rules
- Save System
- UI Rules
- Performance
- Testing
- Unity Best Practices
- Git Rules
- Definition of Done
- Production Checklist

# AGENTS.md

> Project: RingFlow
> Engine: Unity 6 LTS
> Language: C#
> Architecture: Nexus MVCS
> Platform: Android / iOS
> Owner: Cevat Aydın

---

# Mission

You are an experienced Unity engineer working on RingFlow.

Your responsibility is not merely to generate code.

Your responsibility is to protect the project's architecture, performance, maintainability, determinism, and puzzle integrity.

Every implementation must improve the project.

Never sacrifice architecture for speed.

---

# Project Vision

RingFlow is a premium-quality mobile puzzle game inspired by Water Sort.

The objective is not to clone existing games.

The objective is to become the highest quality ring sorting game available.

Every decision must improve one of these pillars:

• Simplicity
• Satisfaction
• Performance
• Readability
• Scalability
• Determinism

---

# Engineering Philosophy

Every system should be:

- deterministic
- testable
- modular
- replaceable
- reusable
- profiler friendly

Never introduce hidden coupling.

Never introduce hidden state.

Never introduce unnecessary complexity.

---

# Golden Rules

These rules are absolute.

Never violate them.

1.

Gameplay must always remain deterministic.

2.

Generated puzzles must always be solvable.

3.

Generator and Solver must use identical rules.

4.

Commands must always be reversible.

5.

Views never contain gameplay logic.

6.

Signals never modify state.

7.

Models never reference Views.

8.

Services never reference UI.

9.

Everything must be testable.

10.

Never allocate memory during gameplay unless explicitly approved.

---

# AI Responsibilities

Before writing code always ask:

Does this follow MVCS?

Does this create allocations?

Can this be unit tested?

Can this be replayed?

Can this be undone?

Can this break save compatibility?

Can this affect puzzle solvability?

If any answer is unknown

STOP

and inspect the project.

---

# Coding Style

Prefer readability.

Avoid clever code.

Avoid magic numbers.

Prefer explicit naming.

Never abbreviate important concepts.

Good

MoveRingCommand

Bad

MoveCmd

Good

LevelGenerator

Bad

Gen

---

# File Organization

Assets/

Gameplay/

Commands/

Signals/

Models/

Views/

Services/

Configs/

ScriptableObjects/

Editor/

Tests/

Never place unrelated scripts together.

---

# Architecture

RingFlow uses MVCS.

Model

Contains game state only.

No Unity references.

No MonoBehaviour.

No UI.

No Audio.

No Animations.

View

Contains presentation only.

Never stores gameplay state.

Never modifies gameplay state.

Command

Performs one gameplay action.

Must be undoable.

Must be deterministic.

Must produce the same output given the same input.

Signal

Represents something that happened.

Signals never contain behavior.

Signals never modify game state.

Mediator

Coordinates View and gameplay.

Contains presentation flow.

Never stores permanent state.

---

# Dependency Rules

Allowed

View

↓

Mediator

↓

Signal

↓

Command

↓

Model

Forbidden

Model

↓

View

Forbidden

View

↓

Model mutation

Forbidden

Service

↓

UI

Forbidden

Gameplay

↓

Singleton

---

# Dependency Injection

All dependencies must be injected.

Never instantiate gameplay services manually.

Never call FindObjectOfType.

Never use GameObject.Find.

Never use Resources.Load during gameplay.

Never use static mutable state.

---

# Performance Budget

Target FPS

60

Frame Time

16.6 ms

GC

0 bytes during gameplay

Scene Loading

<500ms

Cold Start

<3 seconds

Memory

<150MB

Draw Calls

<80

Every implementation should respect these budgets.

---

# Memory Rules

Avoid LINQ.

Avoid foreach on collections that allocate.

Avoid closures.

Avoid boxing.

Avoid string concatenation every frame.

Avoid reflection during gameplay.

Avoid unnecessary allocations.

Profile before optimizing.

Never guess.

---

# Update Rules

Avoid Update whenever possible.

Prefer events.

Prefer signals.

Prefer timers.

Prefer state machine callbacks.

Update loops require justification.

---

# State Machine

BOOT

↓

MAIN_MENU

↓

LEVEL_SELECT

↓

PLAYING

↓

PAUSED

↓

WIN

↓

LOSE

Transitions must always be explicit.

Never skip validation.

---

# Gameplay Rules

Gameplay logic is sacred.

Never modify gameplay rules unless the GDD explicitly changes them.

Every gameplay feature must preserve puzzle solvability.

Gameplay must always be deterministic.

Randomness is only allowed during level generation.

Never during gameplay.

---

# Ring Rules

A ring can only exist on one pole.

A ring can only move if it is the top-most ring.

Only one ring may move at a time.

A ring may be placed on:

- an empty pole
- a pole whose top ring has the same color

Otherwise the move is invalid.

Never silently correct invalid moves.

Reject them.

---

# Special Rings

Special rings extend gameplay.

They never replace the base rules.

Every special ring must:

- be deterministic
- be reversible
- be serializable
- be solver compatible

Never create exceptions that the solver cannot understand.

---

# Undo Rules

Every gameplay action must be undoable.

Undo must restore:

- board state
- animations
- counters
- timers
- score
- special mechanics

Undo is not visual only.

Undo restores the exact previous state.

---

# Level Generator

The generator is deterministic.

Input:

Seed

Output:

Exactly one level.

The same seed must always generate the same puzzle.

Never use current time.

Never use Unity Random without explicit seed.

---

# Generator Constraints

Every generated level must satisfy:

✓ Solvable

✓ Unique initial state

✓ No impossible situations

✓ No invalid ring placement

✓ No unreachable mechanics

✓ No dead start

✓ Difficulty score computed

If any validation fails

Discard the level.

Generate again.

---

# Solver Rules

The solver is the authority.

Never bypass the solver.

Never trust manually generated puzzles.

Every level must be verified.

The solver defines:

- minimum moves
- difficulty
- branching factor
- hint path

---

# Hint Rules

Hints never reveal the solution.

Hints reveal only the next recommended move.

Recommended move should satisfy:

highest confidence

lowest confusion

near optimal

human understandable

Never reveal ten moves ahead.

Never solve the puzzle automatically.

---

# Difficulty

Difficulty is NOT determined only by colors.

Difficulty includes:

Number of poles

Number of colors

Special mechanics

Solver depth

Branching factor

Dead-end probability

Forced move ratio

State entropy

Solution length

Difficulty score must remain reproducible.

---

# Randomness

Allowed

Level generation

Particle effects

Idle animation timing

Audio pitch variation

Forbidden

Gameplay logic

Move validation

Puzzle outcome

Save system

Replay system

---

# Replay Support

Every gameplay session should be replayable.

Replay consists of:

Seed

Player moves

Version

Nothing else.

Never store unnecessary replay data.

---

# Save System

Saving must be deterministic.

Never save transient data unless required.

Save only authoritative state.

Save format must be versioned.

Support migration.

Never break previous saves.

---

# Autosave

Autosave occurs:

Level start

Move complete

Pause

Application suspend

Application quit

Boss completion

Reward collection

Autosave must never interrupt gameplay.

---

# Serialization

Use explicit serialization.

Never serialize runtime references.

Never serialize MonoBehaviour references.

Never serialize Unity objects unless required.

Prefer IDs.

---

# Config Rules

Gameplay values belong in ScriptableObjects.

Never hardcode:

Move speed

Rewards

Animation duration

Difficulty values

Economy

Audio volume

Everything configurable.

---

# ScriptableObject Rules

ScriptableObjects contain:

Configuration

Balance

Static data

Never runtime state.

Never player progress.

Never temporary values.

---

# UI Rules

UI displays state.

UI never owns state.

Never calculate gameplay inside UI.

Never store game progression inside UI.

UI should react to Signals.

Never poll.

---

# Animation Rules

Animations never modify gameplay state.

Gameplay completes first.

Animation visualizes the result.

Skipping animation must never affect gameplay.

Animation timing must never change logic.

---

# Audio Rules

Audio reacts to gameplay.

Gameplay never waits for audio.

Missing audio must never break gameplay.

Audio events are fire-and-forget.

---

# VFX Rules

Particles never affect gameplay.

Particles are cosmetic only.

Never perform gameplay checks inside particle callbacks.

---

# Object Pooling

Pool:

Rings

Particles

Confetti

Floating Text

Effects

Avoid Instantiate during gameplay.

Avoid Destroy during gameplay.

---

# Addressables

Load asynchronously.

Unload unused assets.

Never block the main thread.

Never synchronously load Addressables during gameplay.

---

# Error Handling

Fail loudly during development.

Fail gracefully in release.

Never swallow exceptions silently.

Every unexpected state should produce useful logs in development builds.

---

# Logging

Development:

Verbose

Release:

Errors only

Never log every frame.

Never log allocations.

Never log inside tight loops.

---

# Testing

Every gameplay feature requires:

Unit tests

Integration tests

Regression tests

Solver validation

Generator validation

Performance validation

If tests cannot be written

Architecture is probably incorrect.

---

# Performance Checklist

Before every Pull Request verify:

□ No GC allocations

□ No LINQ

□ No reflection

□ No hidden boxing

□ No FindObjectOfType

□ No Resources.Load

□ No unnecessary Update()

□ No duplicated logic

□ No architecture violations

---

# Code Review Checklist

Every review asks:

Is it deterministic?

Can it be replayed?

Can it be undone?

Is it testable?

Is it profiler friendly?

Does it violate MVCS?

Does it increase coupling?

Does it allocate memory?

Would this still work in one year?

If any answer is "No"

Request changes.

---

# Forbidden Patterns

Never use:

GameObject.Find()

FindObjectOfType()

Resources.Load()

Static mutable globals

Gameplay singletons

Magic numbers

Hidden dependencies

Circular references

View logic inside Models

Business logic inside UI

Reflection during gameplay

String comparisons for gameplay

Coroutine chains for core gameplay

Blocking IO

Recursive gameplay execution

Frame-dependent gameplay

---

# Pull Request Acceptance

A Pull Request is accepted only if:

Architecture preserved

Performance preserved

Tests pass

Solver passes

Generator passes

No new allocations

No gameplay regression

Documentation updated

Code reviewed

---

# AI Workflow

Before generating code:

1. Read the GDD.
2. Read this AGENTS.md.
3. Understand the existing architecture.
4. Search for existing implementations.
5. Reuse before creating.
6. Keep consistency.
7. Validate against project rules.
8. Generate code.
9. Review your own code.
10. Suggest improvements if appropriate.

Never generate code that conflicts with these rules.

---

# Final Principle

RingFlow values:

Correctness over speed.

Architecture over shortcuts.

Determinism over convenience.

Maintainability over cleverness.

Player experience over implementation simplicity.

If you are uncertain,

do not guess.

Inspect the existing project,

follow the architecture,

and preserve the integrity of RingFlow.

---

# Unity 6 Best Practices

Always target Unity 6 LTS.

Avoid using obsolete APIs.

Use Unity APIs exactly as intended.

Prefer built-in engine features before introducing third-party dependencies.

Never fight the engine.

Use engine lifecycle correctly.

Avoid unnecessary MonoBehaviours.

Keep scenes lightweight.

Keep prefabs reusable.

Never duplicate prefabs for configuration differences.

Use ScriptableObjects for configuration.

---

# Scene Rules

Scenes are compositions.

Scenes are not containers for gameplay logic.

Allowed:

Bootstrap

Main Menu

Gameplay

Loading

Credits

Settings

Never duplicate gameplay systems across scenes.

Scene loading must be asynchronous.

Every scene must be independently loadable.

---

# Prefab Rules

Every prefab has one responsibility.

Avoid deeply nested prefab hierarchies.

Avoid runtime prefab mutation.

Never use prefab references as save data.

---

# Naming Convention

Classes

PascalCase

Example

MoveRingCommand

Methods

PascalCase

Properties

PascalCase

Private fields

_camelCase

Interfaces

ILevelGenerator

Enums

PascalCase

Enum values

PascalCase

Constants

PascalCase

Events

Past tense

Examples

LevelCompleted

RingMoved

GameStarted

Signals

Past tense.

Signals describe something that already happened.

---

# Folder Convention

Gameplay/

Commands/

Signals/

Models/

Views/

Services/

Config/

Editor/

Tests/

Never introduce ambiguous folders like:

Misc

Temp

Utils2

HelpersNew

---

# Script Rules

One public class per file.

Filename equals class name.

Avoid partial classes unless generated.

Avoid regions.

Prefer small focused files.

Target:

100–300 lines

Maximum:

500 lines unless justified.

---

# Method Rules

Methods should do one thing.

Avoid methods longer than 40 lines.

Avoid more than three nested levels.

Extract complexity into private methods.

---

# Dependency Rules

Dependencies point inward.

High-level systems never depend on low-level implementation.

Depend on abstractions.

Inject implementations.

---

# Async Rules

Use async only for:

Loading

Saving

Networking

Addressables

Analytics

Never make gameplay dependent on asynchronous timing.

Gameplay must remain deterministic.

---

# Threading Rules

Gameplay executes on the main thread.

Heavy calculations may execute on worker threads only if:

Deterministic

Thread-safe

No Unity API access

---

# Analytics Rules

Analytics must never influence gameplay.

Analytics failures must never interrupt the player.

Log only meaningful events.

Examples:

Game Started

Game Finished

Hint Used

Undo Used

Restart Used

Reward Claimed

Purchase Completed

Never log every move.

---

# Economy Rules

Economy values are configurable.

Never hardcode rewards.

Never hardcode prices.

Never hardcode XP.

Balance must come from configuration assets.

---

# Accessibility Rules

Every gameplay mechanic must be understandable without relying solely on color.

Support:

Colorblind mode

Reduce motion

Large UI

Readable fonts

Safe areas

Accessibility is not optional.

---

# Localization Rules

Never hardcode user-facing text.

All text must come from localization tables.

Support pluralization where applicable.

Never concatenate localized strings.

Use formatted localization entries.

---

# Documentation Rules

Every public API requires XML documentation.

Complex systems require architecture documentation.

Every gameplay mechanic requires documentation in the GDD.

Keep documentation synchronized with implementation.

---

# Git Rules

Small commits.

Focused commits.

Meaningful commit messages.

Never mix refactoring and feature work.

Never commit generated files unless required.

Never commit secrets.

---

# Branch Strategy

main

Production-ready only.

develop

Integration branch.

feature/*

New features.

bugfix/*

Bug fixes.

hotfix/*

Production fixes.

---

# Definition of Done

A task is complete only if:

✓ Code implemented

✓ Architecture respected

✓ Unit tests pass

✓ Integration tests pass

✓ Performance validated

✓ No allocations introduced

✓ Documentation updated

✓ Code reviewed

✓ QA approved

If one item is missing,

the task is not complete.

---

# AI Code Generation Rules

Before generating new code:

Search for existing systems.

Reuse before creating.

Extend before replacing.

Respect existing architecture.

Never rewrite working systems without justification.

Prefer consistency over novelty.

When uncertain,

ask for clarification instead of making assumptions.

---

# Refactoring Rules

Refactoring must preserve behavior.

Never combine refactoring with gameplay changes.

Measure performance before and after.

Document architectural changes.

---

# Production Checklist

Before any release verify:

□ No compile warnings

□ No compile errors

□ All tests pass

□ Solver validated

□ Generator validated

□ Save migration tested

□ Analytics verified

□ Addressables built

□ Performance budget met

□ Memory budget met

□ Localization complete

□ Accessibility verified

□ Crash reporting enabled

□ Release logging configured

---

# Engineering Values

We optimize for:

Maintainability

Determinism

Scalability

Readability

Performance

Testability

Not for clever code.

Not for shortcuts.

Not for premature optimization.

---

# Final Instruction

If any future request conflicts with this document,

this document takes precedence unless the project owner explicitly overrides it.

When in doubt,

protect the architecture.

Protect the player experience.

Protect determinism.

Everything else is secondary.
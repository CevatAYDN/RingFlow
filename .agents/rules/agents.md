---
trigger: always_on
---

# Ring Flow — Agent Context

Ring Flow is a Unity 6 mobile puzzle game. Code in C# (.NET 8).

## Stack
- Unity 6 LTS, Android 10+ / iOS 16+, portrait 9:16 only
- ScriptableObject for all data, Addressables for assets
- Tap-only input (no drag/swipe), 60 FPS target, <1KB GC/frame
- Async/await for IO, Command Pattern for undo

## Modules (Nexus)
Gameplay, UI, Economy, Level, Audio, Ads, IAP, Save, LiveOps, Analytics

## Core Rules
- Pole capacity: 4 rings. Only top ring selectable. One ring moves at a time.
- Valid move: target pole empty OR (not full AND top ring same color AND not locked)
- 11 special ring types: Mystery, Frozen, Locked, Stone, Glass, Rainbow, Bomb, Chain, Magnet, Paint, Ghost
- 40 Worlds × 50 Levels = 2000 levels. Boss every 50.
- 15 languages via Localization.csv, NotoSans font, RTL support.

## Hard Constraints
- NEVER use PlayerPrefs for sensitive data — use File + JSON + encryption
- ALWAYS plan before implementing
- ALWAYS respect 60 FPS budget: <80 draw calls, <100k triangles
- ALWAYS validate with the puzzle solver (BFS/IDA*/Beam Search)
- NO softlock levels — run generator until seed produces solvable level


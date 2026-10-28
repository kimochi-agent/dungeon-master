# Dungeon Master — Game Build Plan (memo)

Date: 2026-10-28. Status: proposed, not started. This memo answers: how I would start this Unity mobile game project, what tooling I would use, and in what order I would build it.

## Goal

Build `Dungeon Master` as a single-player mobile dungeon crawler in Unity: explore dungeons, fight monsters, collect coins and power-ups, and open gates/chests by solving math puzzles based on gameplay events. Keep the architecture simple now, with multiplayer possible later. Repo holds all related work.

## Success Criteria

- A playable single-player vertical slice runs on a real Android phone: move, fight, take damage, collect loot, solve one gate puzzle, open one chest.
- Level content (collectibles, monsters, player health, monster types per level) is data-driven from config assets, as the README requires — no hardcoded per-level tuning in scripts.
- A fresh clone of this repo plus a documented Unity version is enough to open the project and build it.

## Current facts

- Repo contains only `Readme.md` (no Unity project, code, or docs yet). Verified 2026-10-28.
- README scope: single player first, multiplayer later; monster drops; player-health damage values from a `config` file; gates/chests unlocked by math reasoning tied to gameplay so far.

## Key Decisions

1. Unity 6000.6.7f1 (installed on this machine) via Unity Hub. Rationale: fastest path — no new Editor download; supersedes the earlier 6000.3 LTS recommendation per user decision 2026-10-28 (6000.3 is an LTS line and 6000.6 is Current per Unity docs, so this trades some stability for speed).
2. Android first, iOS later. Rationale: Android SDK/NDK/OpenJDK ship with Unity Hub, so a Windows machine can build and device-test immediately; iOS local builds additionally require a Mac with Xcode (or Unity Build Automation), plus Apple ID/Developer Program steps. Minimums per Unity: Android 8.0 (API 26)+ on ARM; iOS 15+ with Xcode 16+.
3. Universal Render Pipeline (URP) as the render pipeline. Rationale: Unity describes URP as providing optimized graphics across platforms from mobile to high-end consoles and PCs — the right default for a mobile game.
4. Input System package (not legacy Input Manager) for touch controls. Rationale: Unity calls it newer, more flexible, and better supported.
5. README `config` file implemented as ScriptableObject data assets (per-level: collectibles, monsters, player health, monster types). Rationale: Unity's documented purpose for ScriptableObjects is storing data independent of GameObjects as shared, referenceable data containers. Rejected for now: text-based config files (harder to edit/test in-Editor, no inspector validation).
6. Git + Git LFS + the GitHub Unity `.gitignore`, Unity project at `game/Dungeon-Master/` with `docs/` at repo root. Rationale: LFS stores large binaries as pointers so the repo stays cloneable; the Unity ignore template excludes generated folders (`Library/`, `Temp/`, `Obj/`, `Builds/`, `Logs/`, `UserSettings/`); a subfolder keeps root-level design docs separate from the Unity project. Commit `Assets/`, `Packages/`, `ProjectSettings/`, `.gitattributes`.
7. Multiplayer: no netcode decision now. Rationale: README defers it; when the time comes, Unity's recommended entry point is the Multiplayer Center, which matches packages/services to the game's needs. Keep game state isolated from presentation so it can move later.

## Approach

Phase the work so each phase ends with something runnable on a phone. Non-goals for this plan: multiplayer implementation, iOS store launch, accounts/backend, ads/purchases, final art. Start every art need with primitives/placeholders; lock the art pipeline only after the vertical slice is fun.

## Steps

1. Lock scope (no Unity work yet): 2D top-down vs 3D (recommendation: 2D for the slice — fastest iteration; URP covers both); Android-first confirmed; exact Unity 6000.3 patch; repo layout `game/` + `docs/`.
2. Scaffold: install Hub + Editor + Android dependencies; create the project using URP in `game/`; add `.gitignore`, `.gitattributes`/LFS; first commit; produce an empty signed Android build and install it on a phone.
3. Vertical slice (one room): touch movement, one monster type + melee/contact damage, player health, coins, one math gate, one chest, one power-up — all values from ScriptableObject level config.
4. Content loop: level/map structure with gates linking dungeons, puzzle generator fed by gameplay events (kills, coins collected, damage taken), monster variety, drop tables, multiple levels via config assets.
5. Mobile hardening: touch UX on small screens, performance pass with the built-in Profiler on a low-end device, local save/settings, pause/background behavior.
6. Release readiness: versioning, Play Store listing content, signed release build tested from a fresh clone; open the iOS lane (Mac/Xcode or Build Automation) only after Android is shippable.

## Validation Plan

- Step 2: fresh-clone check — clone to a new folder, open in Hub, build Android, install on phone. Expected: app launches. Highest-risk check: do this first, before any gameplay code.
- Step 3: on-device playtest checklist (move, kill, loot, gate puzzle, chest, death/respawn). Expected: full loop completable by touch.
- Step 4: config-swap test — duplicate a level config asset with different monsters/counts, play without code changes. Expected: new tuning appears in game.
- Step 5: low-end device pass — steady frame rate in the busiest room, no placeholder-breaking UI at small resolutions, save survives app restart.
- Step 6: release build from a clean checkout installs and runs; store-listing assets match the actual build.

## Risks / Open Questions

- 2D vs 3D is the largest scope fork — unsettled in the README; Step 1 exists to settle it before scaffolding.
- Math-puzzle design (difficulty, audience age, generation rules from gameplay events) is gameplay design, not yet specified.
- Art source (original, store-bought, placeholder) and team size/timeline are unknown; plan assumes solo dev with placeholders.
- iOS needs Mac/Xcode access or hosted builds — deferred, not forgotten.

## Sources

- https://docs.unity.com/en-us/engine/6000.6/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements
- https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/android/getting-started
- https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/iphone/getting-started/ios-environment-setup
- https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/universal-render-pipeline/urp-introduction
- https://docs.unity.com/en-us/engine/6000.6/manual/input
- https://docs.unity.com/en-us/engine/6000.6/manual/scripting/get-started/fundamental-unity-types/class-scriptable-object
- https://docs.unity.com/en-us/engine/6000.6/manual/multiplayer
- https://unity.com/releases/editor/whats-new/6000.3.27f1
- https://git-lfs.com/
- https://raw.githubusercontent.com/github/gitignore/main/Unity.gitignore

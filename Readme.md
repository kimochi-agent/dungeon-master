# Dungeon Master

> Built for the **Meta Dev AI Hackathon** — a complete, playable mobile game
> designed and built during the hackathon, with AI coding agents doing the
> heavy lifting under human direction (plan-driven builder/verifier loop,
> live dashboard, CI-built APKs).

## The game

**Dungeon Master** is a single-player, first-person dungeon crawler for mobile
(Android first), built with Unity 6000.6.7f1 (URP). You are an explorer
delving into torch-lit dungeons: fight the monsters that lurk there, grab
coins and power-ups, and solve math puzzles — drawn from your own run stats —
to unlock gates and treasure chests. Clear the exit to complete the level,
review your stats, and delve deeper into the next dungeon.

## Gameplay

- **Explore** a first-person dungeon carrying a torch — the torch is the only
  light source, with a fixed illumination radius; everything beyond it is dark.
- **Fight** monsters in real time. Kills can drop random collectibles; monster
  hits reduce player health. Monsters hold their attacks while you are busy
  with a chest puzzle.
- **Collect** coins (persisted as a lifetime total shown on the home screen)
  and power-ups (with on-screen toast + status indicators).
- **Solve math puzzles** to open gates and chests. Gates are untimed; chests
  have a 10-second timer (configurable per level) — a first wrong answer or a
  timeout destroys the chest. Puzzles can be skipped via the close button.
- **Progress level by level**: exit a level → level-complete stats screen
  (coins, kills, chests) → home screen → next level unlocks.

## Controls (landscape)

- Left analog stick: move.
- Drag anywhere on screen: look.
- Red ATK button (bottom-right): attack.
- Pause button (top-right); health bar (green over red, edge-to-edge).

## Project structure

- `game/Dungeon-Master/` — the Unity project (scenes are code-generated at
  build time: Home + Dungeon).
- `game/Dungeon-Master/Assets/Resources/Config/levelN.json` — per-level
  config: dungeon layout, monsters, chests, gates, timers, difficulty.
- `.github/workflows/` — CI: automatic versioned APK builds on every push.
- `docs/android-build.md` — local + CI build checklist.
- `docs/levels.md` — level config format and campaign flow.
- `docs/level-generator-api.md` — plan for API-driven next-level generation
  from run analytics.
- `Plan.md` / `MEMO.md` — build plan and design notes from the hackathon.
- `dashboard/` + `agents/` — web dashboard and builder/verifier agent loop
  used to develop the game (`python dashboard/server.py`).

## Getting started

Requirements: Unity Hub + Editor 6000.6.7f1 with Android Build Support,
and the Android SDK platform-tools (`adb`) for installing on a phone.

Build the APK locally (PowerShell):

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" -batchmode -nographics -quit -projectPath "<repo>\game\Dungeon-Master" -executeMethod BuildScript.BuildAndroid -logFile "<repo>\game\Dungeon-Master\Builds\build.log"
```

Install and run:

```powershell
adb install -r game\Dungeon-Master\Builds\DungeonMaster.apk
```

Or push to GitHub: `.github/workflows/build-android.yml` builds the APK
automatically on pushes touching `game/` via a self-hosted runner on your own
PC (one-time setup in `docs/android-build.md`), and attaches a versioned
APK as a run artifact.

## Status and next steps

Playable end-to-end on Android: home → dungeon → stats → next level, with
config-driven levels and run analytics collected for difficulty tuning.
Known gaps: debug-signed APK only (no release keystore/AAB yet), no death/hurt
animations, tablets unverified. See `Plan.md` for the remaining build steps
and `docs/level-generator-api.md` for the next big feature.

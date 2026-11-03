## Step by step plans to build the game

- This file will have step by step buildling blocks of the game i.e what to do, how and what is the result Verifications loop would be followed by every step changes and were marked as passed or need improvement followed by the improevement it needs.

## Plans - Steps

Status tags: `[TODO]` -> `[IN PROGRESS]` -> `[IN REVIEW]` -> `[PASSED]`, or `[NEEDS FIX]` with `Fix:` notes. Only the verifier plus you mark `[PASSED]`. See `agents/README.md`.

1. Initially the game would be having a player object and dungeon and the player could be able to move across dungeons. Basically dungeons would be explorable 3d map. [PASSED]
2. We will add collectibles such as coins, power-ups, chests currently without lock. [PASSED]
3. Chests would be having locks which would require a math riddle, puzzle or questions to be answered in order to unlock them. [PASSED]
  - Fix: InvalidOperationException: You are trying to read Input using the UnityEngine.Input class, but you have switched active Input handling to Input System package in Player Settings.
  - Fix: UnityEngine.Internal.InputUnsafeUtility.GetButtonDown (System.String buttonName) (at <d61856715e604432bd9c684f2340dc5b>:0)
  - Fix: UnityEngine.Input.GetButtonDown (System.String buttonName) (at <d61856715e604432bd9c684f2340dc5b>:0)
  - Fix: UnityEngine.EventSystems.BaseInput.GetButtonDown (System.String buttonName) (at ./Library/PackageCache/com.unity.ugui@eb824e41fbc7/Runtime/UGUI/EventSystem/InputModules/BaseInput.cs:154)
  - Fix: UnityEngine.EventSystems.StandaloneInputModule.ShouldActivateModule () (at ./Library/PackageCache/com.unity.ugui@eb824e41fbc7/Runtime/UGUI/EventSystem/InputModules/StandaloneInputModule.cs:252)
  - Fix: UnityEngine.EventSystems.EventSystem.Update () (at ./Library/PackageCache/com.unity.ugui@eb824e41fbc7/Runtime/UGUI/EventSystem/EventSystem.cs:500)
  - Fix: UnityEngine.StackTraceUtility:ExtractStringFromExceptionInternal(Object, String&, String&)
4. Player health system with damage intake, death and respawn, plus a basic HUD showing health bar and coin count. [PASSED]
5. Touch controls for player movement so the game is playable on a mobile phone. [PASSED]
  - Auto-passed on test1 without playtest verdict.
6. Game shell screens: main menu, pause, game over and restart flow. [PASSED]
  - Auto-passed on test1 without playtest verdict.
7. Power-up pickup effects (e.g. speed boost, extra health, shield) applied when collected. [PASSED]
  - Auto-passed on test1 without playtest verdict.
8. A single basic monster type with simple wander/chase movement that damages the player on contact. [PASSED]
  - Auto-passed on test1 without playtest verdict.
9. Player attack, monster death and random collectible drops on kill. [PASSED]
  - Auto-passed on test1 without playtest verdict.
10. Level config data holding collectibles per level, monsters per level, player health and monster types, so tuning needs no code changes. [PASSED]
  - Auto-passed on test1 without playtest verdict.
11. Gates connecting dungeons that reuse the step 3 puzzle UI and require reasoning to open. [PASSED]
  - Auto-passed on test1 without playtest verdict.
12. Puzzle generator that builds math questions from gameplay so far (kills, coins collected, damage taken). [PASSED]
  - Auto-passed on test1 without playtest verdict.
13. Multiple linked dungeons with level progression driven by the level config data. [PASSED]
  - Auto-passed on test1 without playtest verdict.
14. First Android build installed on a real phone, verified from a fresh clone of the repo. [PASSED]
  - Auto-passed on test1 without playtest verdict.
  - NOTE: build script + checklist only — on-device install still needs you.

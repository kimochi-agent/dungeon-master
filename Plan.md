## Step by step plans to build the game

- This file will have step by step buildling blocks of the game i.e what to do, how and what is the result Verifications loop would be followed by every step changes and were marked as passed or need improvement followed by the improevement it needs.

## Plans - Steps

Status tags: `[TODO]` -> `[IN PROGRESS]` -> `[IN REVIEW]` -> `[PASSED]`, or `[NEEDS FIX]` with `Fix:` notes. Only the verifier plus you mark `[PASSED]`. See `agents/README.md`.

1. Initially the game would be having a player object and dungeon and the player could be able to move across dungeons. Basically dungeons would be explorable 3d map. [TODO]
2. We will add collectibles such as coins, power-ups, chests currently without lock. [TODO]
3. Chests would be having locks which would require a math riddle, puzzle or questions to be answered in order to unlock them. [TODO]
4. Player health system with damage intake, death and respawn, plus a basic HUD showing health bar and coin count. [TODO]
5. Touch controls for player movement so the game is playable on a mobile phone. [TODO]
6. Game shell screens: main menu, pause, game over and restart flow. [TODO]
7. Power-up pickup effects (e.g. speed boost, extra health, shield) applied when collected. [TODO]
8. A single basic monster type with simple wander/chase movement that damages the player on contact. [TODO]
9. Player attack, monster death and random collectible drops on kill. [TODO]
10. Level config data holding collectibles per level, monsters per level, player health and monster types, so tuning needs no code changes. [TODO]
11. Gates connecting dungeons that reuse the step 3 puzzle UI and require reasoning to open. [TODO]
12. Puzzle generator that builds math questions from gameplay so far (kills, coins collected, damage taken). [TODO]
13. Multiple linked dungeons with level progression driven by the level config data. [TODO]
14. First Android build installed on a real phone, verified from a fresh clone of the repo. [TODO]

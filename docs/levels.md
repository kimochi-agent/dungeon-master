# Level configs

Each level is one JSON file: `game/Dungeon-Master/Assets/Resources/Config/levelN.json`
(`level1.json`, `level2.json`, ...). No code changes are needed to add a level —
drop in `level3.json` and it joins the campaign automatically.

## Campaign flow

- Home → Start always begins at level 1.
- Reaching a level's `exitPoint` loads the next level file if one exists,
  otherwise the victory screen ("You cleared all N levels!").
- Death → end screen; Play Again restarts the *current* level.
- Per-run stats (coins, kills, damage) reset each level; lifetime coin total
  persists. New file = new row in this loop; analytics-driven generation
  (difficulty from attempts, chests opened, kills) should write these files.

## Schema

Tuning keys (all optional, built-in defaults apply):

- `levelName`, `playerMaxHealth`, `playerMoveSpeed`
- `coinCount`, `powerUpCount`, `chestCount`, `lockedChestCount`, `monsterCount`
- `monsterDamage`, `monsterHealth`, `monsterSpeed`, `puzzleTimeLimit`

Layout keys (optional; missing/empty arrays fall back to the Level 1 layout):

- `rooms`: `[{center: {x, y}, size: {x, y}}]` — XZ rects; floor, ceiling, 4 walls.
- `corridors`: `[{from: {x, y}, to: {x, y}, width}]` — straight, axis-aligned
  (along X or Z only); both ends must terminate inside rooms.
- `doors`: `[{position: {x, y, z}, alongX}]` — locked gates; must sit on a room
  side plane. `alongX: true` = wall runs north-south (gap spans Z).
- `playerSpawn`, `exitPoint`: Vector3s (exit = level-complete trigger).
- `pillars`, `coinSpots`, `powerUpSpots`, `chestSpots`, `lockedChestSpots`,
  `monsterSpots`: Vector3 arrays. Counts cap how many leading spots spawn.

## Authoring rules

- Rooms must not overlap each other; corridor floors must abut room floors
  (share an edge) rather than overlap, or the floor z-fights.
- Corridor ends landing exactly on a room side plane cut the doorway gap.
- Doors need a matching corridor mouth or they seal the gap with a gate the
  player opens by puzzle.
- Keep spots inside room bounds and clear of pillars/walls.
- `level1.json` is the straight 3-hall layout; `level2.json` is an L-shaped
  3-room variant — copy either as a starting point.

## Analytics (feeds the future difficulty endpoint)

`LevelAnalytics` (`Assets/Scripts/Analytics/LevelAnalytics.cs`) records a
`LevelReport` per level, persisted immediately to PlayerPrefs as JSON:

- `attempts`, `deaths`, `completions` — runs started, failed, cleared.
- `kills`, `coinsCollected`, `damageTaken` — run-stat deltas per attempt.
- `chestsOpened`, `lockedChestsOpened`, `lockedChestsDestroyed`, `gatesOpened`.
- `bestClearTimeSeconds`, `totalPlayTimeSeconds`, `lastPlayedUtc`.

`GetPayload(levelIndex)` returns one level's report as a JSON string, ready to
POST; `GetAllPayloads()` returns the whole campaign history. When the
generator API exists, POST the cleared level's payload, take the returned
`levelN+1` JSON, and load it via `GameConfig.LoadLevel` — no other code
changes needed.

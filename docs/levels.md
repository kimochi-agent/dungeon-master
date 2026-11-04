# Level configs

Each level is one JSON file: `game/Dungeon-Master/Assets/Resources/Config/levelN.json`
(`level1.json`, `level2.json`, ...). No code changes are needed to add a level —
drop in `level3.json` and it joins the campaign automatically.

## Campaign flow

- Home → Start begins at level 1; a Continue button offers the highest
  unlocked level (`GameState.NextLevel`, persisted across sessions).
- Reaching a level's `exitPoint` unlocks the next level and shows a
  level-complete screen (run coins, kills, chests); Continue returns home,
  and finishing the last level shows "Campaign Complete!" there.
- Death → end screen; Play Again restarts the *current* level.
- Generated levels: clearing level N POSTs its analytics and downloads
  level N+1 (see Downloaded levels below). Home retries a missing next
  level on every visit; "Campaign Complete!" now means no bundled level
  and no download available (e.g. offline past the bundled campaign).
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

## Downloaded levels

`LevelDownloader` (`Assets/Scripts/LevelDownloader.cs`) POSTs the cleared
level's `LevelReport` to `POST {base}/api/levels/next` (contract:
`level_generator/contract.md`) and saves the returned config:

- Storage: `Application.persistentDataPath/levels/levelN.json` (config only,
  same schema as bundled) plus `levelN.meta.json` (provenance: layout id,
  hash, seed, generator version, difficulty).
- Precedence: `GameConfig` loads a valid download over the bundled file;
  `HasLevel`/`LevelCount` count downloads too. Check `adb logcat` for
  `GameConfig: level N loaded from download|bundled config.`
- Validation: downloads must pass strict client checks
  (`LevelValidator`, mirroring `level_generator/validate.py`) before
  loading; invalid files are deleted and bundled is used.
- Idempotency: a saved valid level is never re-requested or overwritten.
- Timing: the fetch starts on level-complete and never blocks navigation;
  Home refreshes its Play button live when a fetch lands.
- Offline: any failure (no network, bad response, timeout) silently keeps
  bundled levels; nothing user-visible breaks.
- Base URL: `http://127.0.0.1:8124` by default, overrideable without a
  rebuild via an `api_base_url.txt` file in `persistentDataPath`.

## Analytics (feeds the difficulty endpoint)

`LevelAnalytics` (`Assets/Scripts/Analytics/LevelAnalytics.cs`) records a
`LevelReport` per level, persisted immediately to PlayerPrefs as JSON:

- `attempts`, `deaths`, `completions` — runs started, failed, cleared.
- `kills`, `coinsCollected`, `damageTaken` — run-stat deltas per attempt.
- `chestsOpened`, `lockedChestsOpened`, `lockedChestsDestroyed`, `gatesOpened`.
- `bestClearTimeSeconds`, `totalPlayTimeSeconds`, `lastPlayedUtc`.

`GetPayload(levelIndex)` returns one level's report as a JSON string, which
`LevelDownloader` POSTs (see Downloaded levels above). `GetAllPayloads()`
returns the whole campaign history. Future tuning work can correlate play
data with served layouts via the `layoutHash` in each `levelN.meta.json`.

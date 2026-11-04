# Level generator API contract (v1)

Base URL (dev): `http://127.0.0.1:8124` (`LEVEL_GEN_HOST` / `LEVEL_GEN_PORT`).
No auth in v1 (localhost dev; auth arrives with hosted deployment).

## POST /api/levels/next

Request: the cleared level's analytics plus routing fields.

```json
{
  "schemaVersion": 1,
  "clearedLevelIndex": 1,
  "seed": 7,
  "report": {
    "levelIndex": 1,
    "attempts": 2,
    "deaths": 1,
    "damageTaken": 25,
    "lockedChestsDestroyed": 0,
    "completions": 1,
    "kills": 3,
    "coinsCollected": 8
  }
}
```

| Field | Required | Meaning |
|---|---|---|
| `schemaVersion` | yes | Must be `1`; anything else is a 422. |
| `clearedLevelIndex` | yes | Integer >= 1. Response is for `clearedLevelIndex + 1`. |
| `seed` | no | Int or string. Same seed + same report = identical response. Omitted seeds hash the request, so identical retries still return identical levels. |
| `report` | yes | `LevelReport` JSON as produced by `LevelAnalytics.GetPayload()`. |

Tuner inputs (required, integers >= 0): `attempts`, `deaths`,
`damageTaken`, `lockedChestsDestroyed`. Other known numeric report fields
are accepted when present and >= 0; unknown fields are ignored.
`report.levelIndex` is informational; routing uses `clearedLevelIndex`.

Full example bodies: `level_generator/fixtures/standard.json`,
`struggling.json`, `mastery.json`.

Response (200): the next level.

```json
{
  "levelIndex": 2,
  "seed": 7,
  "generatorVersion": "gen-v1",
  "layoutId": "halls",
  "layoutHash": "9f2c…",
  "difficulty": "standard",
  "config": { "levelName": "Level 2 - Echoing Halls", "rooms": [ … ] }
}
```

| Field | Meaning |
|---|---|
| `levelIndex` | `clearedLevelIndex + 1`. |
| `seed` | Normalized seed used (echoed or derived). Store it to reproduce this level. |
| `generatorVersion` | Tuning-rules version (`tuning.GENERATOR_VERSION`). |
| `layoutId` | One of `halls`, `turn`, `spire`. |
| `layoutHash` | Short sha256 of the canonical config; log it with analytics to correlate play data with the layout served. |
| `difficulty` | `gentle`, `standard`, or `spicy`. |
| `config` | Full `levelN.json`-shaped `LevelData` (same keys `GameConfig` parses; field names are case-sensitive for Unity `JsonUtility`). |

Golden example: `level_generator/fixtures/golden_response.json`
(request `fixtures/standard.json`).

## GET /healthz

Returns `{"ok": true, "generatorVersion": "gen-v1"}`.

## Errors

All errors are `{"error": "<message>"}`:

| Code | When |
|---|---|
| 400 | Malformed JSON, empty body, bad Content-Length. |
| 404 | Unknown path. |
| 413 | Body over 64 KB. |
| 422 | Valid JSON but invalid values (bad `schemaVersion`, missing/invalid `report` fields). |
| 500 | Generator could not produce a valid level (client must use bundled fallback). |

## Difficulty buckets (v1 rules)

Inputs: `attempts` (A), `deaths` (D), `damageTaken`, `lockedChestsDestroyed` (X).

- `gentle` if D >= 2 or A >= 4 or X >= 2 (struggled: softer monsters, longer puzzle timer, more loot).
- else `spicy` if A == 1 and D == 0 and damage <= 15 (flawless: more/stronger monsters, shorter timer).
- else `standard`.

Knobs for next level N (step = N - 1), then clamped to the ranges below
and to the chosen layout's spot capacities:

- `monsterCount` = 3 + (N-1)//2 + {-1 gentle, 0 standard, +2 spicy}
- `monsterDamage` = round((10 + 2*step) * {0.8, 1.0, 1.25})
- `monsterHealth` = round((30 + 5*step) * {0.8, 1.0, 1.25})
- `monsterSpeed` = 3.0 + 0.1*step + {-0.3, 0.0, +0.4}
- `puzzleTimeLimit` = max(6, {12, 10, 8} - 0.5*(N-2))
- `coinCount` = {10, 8, 6}; `powerUpCount` = {3, 2, 2}
- `chestCount` = {2, 2, 1}; `lockedChestCount` = {1, 2, 2}
- `playerMaxHealth` = 100, `playerMoveSpeed` = 5.0 (fixed in v1)

Layout pick is deterministic: `ORDER[(seed + levelIndex) % 3]`
over `halls`, `turn`, `spire`.

## Tuning ranges (generated configs must satisfy these)

`playerMaxHealth` 50..200, `playerMoveSpeed` 3..8,
`coinCount` 0..30, `powerUpCount` 0..8, `chestCount` 0..6,
`lockedChestCount` 0..6, `monsterCount` 0..12,
`monsterDamage` 1..40, `monsterHealth` 10..200,
`monsterSpeed` 1..7, `puzzleTimeLimit` 5..30.

## Geometry rules (enforced by validate.py, mirrored from the Unity builder)

- 1..8 rooms, each >= 4x4, pairwise non-overlapping.
- 0..10 corridors: axis-aligned, width 2..6, both ends terminate at rooms
  (within 0.6), floors abut rooms without overlap, ends in different rooms.
- 0..6 doors: on a room side plane (within 0.6, 1m from corners),
  with a matching corridor mouth; matched corridor width ~4 (3.5..4.5).
- Pillars inside rooms; spawn/exit inside rooms and clear of pillars.
- Every spot inside a room or corridor (0.5 margin), clear of pillars
  (1.0) and doors (1.5); monster spots 3.0 from spawn.
- Each spot list holds at least its count (`coinSpots >= coinCount`, …).
- Spawn room reaches the exit room and every room (corridor graph BFS).

The service validates every generated config and falls back across
templates before responding; it never serves invalid JSON.

## Versioning

- `schemaVersion` (request) guards the API shape; bump on breaking changes.
- `generatorVersion` (response) identifies the tuning rules; analytics
  should log it alongside `layoutHash`.
- Unity `JsonUtility` drops unknown fields silently: clients must reject
  unknown `schemaVersion` explicitly instead of parsing blindly.

## Privacy

v1 sends only the aggregate `LevelReport` totals already produced by
`GetPayload()` — no device id, no position traces. Revisit if later
versions add identifiers for idempotency or accounts.

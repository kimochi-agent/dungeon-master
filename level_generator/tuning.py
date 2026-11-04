"""Rule-based difficulty: LevelReport -> tuning knobs.

Pure and deterministic: same (report, next_index, capacities) always gives
the same knobs. This module is the seam for a future agent/LLM tuner --
replace difficulty_bucket()/tune() without touching HTTP or validation.
See level_generator/contract.md for the bucket table and formulas.
"""

import validate

GENERATOR_VERSION = "gen-v1"

_BUCKETS = ("gentle", "standard", "spicy")


def difficulty_bucket(report):
    """Classify the cleared level's performance.

    gentle: the player struggled (multiple deaths/attempts, chests lost).
    spicy: single flawless attempt with low damage taken.
    standard: everything else.
    """
    attempts = max(1, int(report.get("attempts", 1)))
    deaths = int(report.get("deaths", 0))
    damage = int(report.get("damageTaken", 0))
    destroyed = int(report.get("lockedChestsDestroyed", 0))
    if deaths >= 2 or attempts >= 4 or destroyed >= 2:
        return "gentle"
    if attempts == 1 and deaths == 0 and damage <= 15:
        return "spicy"
    return "standard"


def _clamp(v, lo, hi):
    return max(lo, min(hi, v))


def tune(report, next_index, capacities):
    """Build tuning knobs for the next level.

    capacities maps count keys (coinCount, ...) to available spots in the
    chosen layout; counts are clamped to fit so generation never promises
    more spawns than the layout can place.
    """
    bucket = difficulty_bucket(report)
    step = max(0, next_index - 1)  # baseline ramp with campaign depth
    mult = {"gentle": 0.8, "standard": 1.0, "spicy": 1.25}[bucket]

    want = {}
    want["monsterCount"] = (3 + (next_index - 1) // 2
                            + {"gentle": -1, "standard": 0, "spicy": 2}[bucket])
    want["monsterDamage"] = round((10 + 2 * step) * mult)
    want["monsterHealth"] = round((30 + 5 * step) * mult)
    want["monsterSpeed"] = round(
        3.0 + 0.1 * step + {"gentle": -0.3, "standard": 0.0, "spicy": 0.4}[bucket], 1)
    want["puzzleTimeLimit"] = round(
        max(6.0, {"gentle": 12.0, "standard": 10.0, "spicy": 8.0}[bucket]
             - 0.5 * (next_index - 2)), 1)
    want["coinCount"] = {"gentle": 10, "standard": 8, "spicy": 6}[bucket]
    want["powerUpCount"] = {"gentle": 3, "standard": 2, "spicy": 2}[bucket]
    want["chestCount"] = {"gentle": 2, "standard": 2, "spicy": 1}[bucket]
    want["lockedChestCount"] = {"gentle": 1, "standard": 2, "spicy": 2}[bucket]

    knobs = {"playerMaxHealth": 100, "playerMoveSpeed": 5.0}
    for key, value in want.items():
        lo, hi = validate.TUNING_RANGES[key]
        cap = int(capacities.get(key, hi))
        if key in validate.INT_KEYS:
            knobs[key] = _clamp(int(value), max(lo, 0), min(hi, cap))
        else:
            knobs[key] = float(_clamp(value, lo, hi))
    return knobs

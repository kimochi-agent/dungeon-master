"""Schema + geometry validation for Dungeon Master level configs.

Mirrors what the Unity client assumes when it builds and plays a level
(see DungeonGenerator.GapsOnSide, GameConfig.LevelData, CollectiblePlacer,
MonsterSpawner, and the docs/levels.md authoring rules). The generator
service validates everything it produces; invalid configs are never served.

Run the full self-check (validator cases included) with:
    python level_generator/check.py
Or validate files directly:
    python level_generator/validate.py game/Dungeon-Master/Assets/Resources/Config/level1.json
"""

import json
import sys

EPS = 0.6  # Must match DungeonGenerator.GapsOnSide plane tolerance.
SPOT_WALL_MARGIN = 0.5  # Spots must sit this far inside room/corridor bounds.
PILLAR_CLEARANCE = 1.0  # Spots/spawn/exit keep this XZ distance from pillars.
DOOR_CLEARANCE = 1.5  # Spots keep this XZ distance from door centers.
SPAWN_MONSTER_MIN = 3.0  # Monster spots keep this XZ distance from spawn.
ROOM_MIN_SIZE = 4.0
CORRIDOR_WIDTH_RANGE = (2.0, 6.0)
DOOR_CORRIDOR_WIDTH = (3.5, 4.5)  # Door-matched corridor must fit the 4m door gap.

# Generator-strict schema: every tuning key must be present and in range.
TUNING_RANGES = {
    "playerMaxHealth": (50, 200),
    "playerMoveSpeed": (3.0, 8.0),
    "coinCount": (0, 30),
    "powerUpCount": (0, 8),
    "chestCount": (0, 6),
    "lockedChestCount": (0, 6),
    "monsterCount": (0, 12),
    "monsterDamage": (1, 40),
    "monsterHealth": (10, 200),
    "monsterSpeed": (1.0, 7.0),
    "puzzleTimeLimit": (5.0, 30.0),
}
INT_KEYS = {
    "playerMaxHealth", "coinCount", "powerUpCount", "chestCount",
    "lockedChestCount", "monsterCount", "monsterDamage", "monsterHealth",
}

SPOT_FIELDS = (
    ("coinSpots", "coinCount"),
    ("powerUpSpots", "powerUpCount"),
    ("chestSpots", "chestCount"),
    ("lockedChestSpots", "lockedChestCount"),
    ("monsterSpots", "monsterCount"),
)

# Report fields the tuner reads; must be present integers >= 0.
REQUIRED_REPORT_INTS = ("attempts", "deaths", "damageTaken", "lockedChestsDestroyed")
# Other known numeric report fields; validated only when present.
OPTIONAL_REPORT_NUMS = (
    "completions", "kills", "chestsOpened", "lockedChestsOpened",
    "gatesOpened", "coinsCollected", "bestClearTimeSeconds", "totalPlayTimeSeconds",
)


def _is_num(v):
    return isinstance(v, (int, float)) and not isinstance(v, bool)


def _v2xz(d):
    """Vector2 XZ point ({x, y} where y is Z). Returns (x, z) or None."""
    if not isinstance(d, dict):
        return None
    x, z = d.get("x"), d.get("y")
    if not _is_num(x) or not _is_num(z):
        return None
    return (float(x), float(z))


def _v3(d):
    if not isinstance(d, dict):
        return None
    x, y, z = d.get("x"), d.get("y"), d.get("z")
    if not _is_num(x) or not _is_num(y) or not _is_num(z):
        return None
    return (float(x), float(y), float(z))


def _room_rect(room):
    c = _v2xz(room.get("center"))
    s = _v2xz(room.get("size"))
    if c is None or s is None:
        return None
    return (c[0] - s[0] / 2.0, c[0] + s[0] / 2.0, c[1] - s[1] / 2.0, c[1] + s[1] / 2.0)


def _inside(px, pz, rect, margin):
    x0, x1, z0, z1 = rect
    return x0 + margin <= px <= x1 - margin and z0 + margin <= pz <= z1 - margin


def _dist2(ax, az, bx, bz):
    return ((ax - bx) ** 2 + (az - bz) ** 2) ** 0.5


def _overlap_1d(a0, a1, b0, b1):
    return max(0.0, min(a1, b1) - max(a0, b0))


def validate_config(cfg):
    """Return a list of error strings; empty means valid."""
    errors = []
    if not isinstance(cfg, dict):
        return ["config must be a JSON object"]

    for key, (lo, hi) in TUNING_RANGES.items():
        if key not in cfg:
            errors.append("missing tuning key: %s" % key)
            continue
        v = cfg[key]
        if not _is_num(v):
            errors.append("%s must be a number" % key)
            continue
        if key in INT_KEYS and float(v) != int(float(v)):
            errors.append("%s must be an integer" % key)
        if not (lo <= v <= hi):
            errors.append("%s=%s out of range [%s, %s]" % (key, v, lo, hi))

    rooms = []
    raw_rooms = cfg.get("rooms")
    if not isinstance(raw_rooms, list) or not (1 <= len(raw_rooms) <= 8):
        errors.append("rooms must be a list with 1..8 entries")
    else:
        for i, room in enumerate(raw_rooms):
            rect = _room_rect(room) if isinstance(room, dict) else None
            if rect is None:
                errors.append("rooms[%d] has bad center/size" % i)
                continue
            sx = rect[1] - rect[0]
            sz = rect[3] - rect[2]
            if sx < ROOM_MIN_SIZE or sz < ROOM_MIN_SIZE:
                errors.append("rooms[%d] size %.1fx%.1f below minimum %.0fx%.0f"
                              % (i, sx, sz, ROOM_MIN_SIZE, ROOM_MIN_SIZE))
            rooms.append(rect)
        for i in range(len(rooms)):
            for j in range(i + 1, len(rooms)):
                ox = _overlap_1d(rooms[i][0], rooms[i][1], rooms[j][0], rooms[j][1])
                oz = _overlap_1d(rooms[i][2], rooms[i][3], rooms[j][2], rooms[j][3])
                if ox > EPS and oz > EPS:
                    errors.append("rooms[%d] and rooms[%d] overlap" % (i, j))

    corridors = []
    raw_corridors = cfg.get("corridors")
    if not isinstance(raw_corridors, list) or len(raw_corridors) > 10:
        errors.append("corridors must be a list with 0..10 entries")
    else:
        for i, c in enumerate(raw_corridors):
            if not isinstance(c, dict):
                errors.append("corridors[%d] has bad from/to/width" % i)
                continue
            a = _v2xz(c.get("from"))
            b = _v2xz(c.get("to"))
            w = c.get("width")
            if a is None or b is None or not _is_num(w):
                errors.append("corridors[%d] has bad from/to/width" % i)
                continue
            w = float(w)
            if not (CORRIDOR_WIDTH_RANGE[0] <= w <= CORRIDOR_WIDTH_RANGE[1]):
                errors.append("corridors[%d] width %s out of range %s"
                              % (i, w, list(CORRIDOR_WIDTH_RANGE)))
            ends = []
            for which, p in (("from", a), ("to", b)):
                hit = next((r for r in range(len(rooms))
                            if _inside(p[0], p[1], rooms[r], -EPS)), None)
                if hit is None:
                    errors.append("corridors[%d] end %s terminates outside any room"
                                  % (i, which))
                ends.append(hit)
            dx = abs(b[0] - a[0])
            dz = abs(b[1] - a[1])
            if dx < 0.01 and dz < 0.01:
                errors.append("corridors[%d] has zero length" % i)
                continue
            if dx >= 0.01 and dz >= 0.01:
                errors.append("corridors[%d] is not axis-aligned (diagonal)" % i)
                continue
            horiz = dz < 0.01
            if horiz:
                rect = (min(a[0], b[0]), max(a[0], b[0]),
                        a[1] - w / 2.0, a[1] + w / 2.0)
                length = rect[1] - rect[0]
            else:
                rect = (a[0] - w / 2.0, a[0] + w / 2.0,
                        min(a[1], b[1]), max(a[1], b[1]))
                length = rect[3] - rect[2]
            if length < 1.0:
                errors.append("corridors[%d] length %.1f below minimum 1" % (i, length))
            for j, room in enumerate(rooms):
                ox = _overlap_1d(rect[0], rect[1], room[0], room[1])
                oz = _overlap_1d(rect[2], rect[3], room[2], room[3])
                bad = (oz > 0 and ox > EPS) if horiz else (ox > 0 and oz > EPS)
                if bad:
                    errors.append("corridors[%d] overlaps rooms[%d] "
                                  "(floors must abut, not overlap)" % (i, j))
            if ends[0] is not None and ends[0] == ends[1]:
                errors.append("corridors[%d] has both ends in rooms[%d] "
                              "(stub with no span)" % (i, ends[0]))
            corridors.append({"horiz": horiz, "rect": rect, "width": w,
                               "cx": (a[0] + b[0]) / 2.0,
                               "cz": (a[1] + b[1]) / 2.0,
                               "rooms": tuple(ends)})

    doors = []
    raw_doors = cfg.get("doors")
    if not isinstance(raw_doors, list) or len(raw_doors) > 6:
        errors.append("doors must be a list with 0..6 entries")
    else:
        for i, d in enumerate(raw_doors):
            if not isinstance(d, dict) or not isinstance(d.get("alongX"), bool):
                errors.append("doors[%d] has bad position/alongX" % i)
                continue
            p = _v3(d.get("position"))
            if p is None:
                errors.append("doors[%d] has bad position/alongX" % i)
                continue
            along_x = d["alongX"]
            if not (0.5 <= p[1] <= 2.5):
                errors.append("doors[%d] y=%s outside [0.5, 2.5]" % (i, p[1]))
            plane, at = (p[0], p[2]) if along_x else (p[2], p[0])
            on_plane = False
            corner_ok = False
            for room in rooms:
                if along_x:
                    sides = (room[0], room[1])
                    span = (room[2], room[3])
                else:
                    sides = (room[2], room[3])
                    span = (room[0], room[1])
                if min(abs(plane - s) for s in sides) <= EPS:
                    on_plane = True
                    if span[0] + 1.0 <= at <= span[1] - 1.0:
                        corner_ok = True
            if not on_plane:
                errors.append("doors[%d] is not on any room side plane" % i)
            elif not corner_ok:
                errors.append("doors[%d] too close to a room corner" % i)
            mouth = None
            for c in corridors:
                if c["horiz"] != along_x:
                    continue
                r = c["rect"]
                if along_x:
                    in_lane = abs(at - c["cz"]) <= c["width"] / 2.0 + EPS
                    in_span = r[0] - EPS <= plane <= r[1] + EPS
                else:
                    in_lane = abs(at - c["cx"]) <= c["width"] / 2.0 + EPS
                    in_span = r[2] - EPS <= plane <= r[3] + EPS
                if in_lane and in_span:
                    mouth = c
                    break
            if mouth is None:
                errors.append("doors[%d] has no matching corridor mouth" % i)
            elif not (DOOR_CORRIDOR_WIDTH[0] <= mouth["width"] <= DOOR_CORRIDOR_WIDTH[1]):
                errors.append("doors[%d] corridor width %s must be ~4 "
                              "to match the 4m door gap" % (i, mouth["width"]))
            doors.append((p[0], p[2]))

    pillars = []
    raw_pillars = cfg.get("pillars")
    if not isinstance(raw_pillars, list):
        errors.append("pillars must be a list")
    else:
        for i, s in enumerate(raw_pillars):
            p = _v3(s)
            if p is None:
                errors.append("pillars[%d] has bad x/y/z" % i)
                continue
            if not any(_inside(p[0], p[2], room, 1.0) for room in rooms):
                errors.append("pillars[%d] is outside every room" % i)
            pillars.append((p[0], p[2]))

    spawn = _v3(cfg.get("playerSpawn"))
    exit_ = _v3(cfg.get("exitPoint"))
    for label, p in (("playerSpawn", spawn), ("exitPoint", exit_)):
        if p is None:
            errors.append("%s must be an {x, y, z} point" % label)
            continue
        if not any(_inside(p[0], p[2], room, SPOT_WALL_MARGIN) for room in rooms):
            errors.append("%s is outside every room" % label)
        for q in pillars:
            if _dist2(p[0], p[2], q[0], q[1]) < PILLAR_CLEARANCE:
                errors.append("%s too close to a pillar" % label)
                break

    for cat, _count_key in SPOT_FIELDS:
        raw = cfg.get(cat)
        if not isinstance(raw, list):
            errors.append("%s must be a list" % cat)
            continue
        for i, s in enumerate(raw):
            p = _v3(s)
            if p is None:
                errors.append("%s[%d] has bad x/y/z" % (cat, i))
                continue
            if not (0 <= p[1] <= 2):
                errors.append("%s[%d] y=%s outside [0, 2]" % (cat, i, p[1]))
            in_room = any(_inside(p[0], p[2], room, SPOT_WALL_MARGIN) for room in rooms)
            in_corr = any(_inside(p[0], p[2], c["rect"], SPOT_WALL_MARGIN)
                          for c in corridors)
            if not (in_room or in_corr):
                errors.append("%s[%d] (%s, %s) is outside every room and corridor"
                              % (cat, i, p[0], p[2]))
            for q in pillars:
                if _dist2(p[0], p[2], q[0], q[1]) < PILLAR_CLEARANCE:
                    errors.append("%s[%d] too close to a pillar" % (cat, i))
                    break
            for q in doors:
                if _dist2(p[0], p[2], q[0], q[1]) < DOOR_CLEARANCE:
                    errors.append("%s[%d] too close to a door" % (cat, i))
                    break
            if cat == "monsterSpots" and spawn is not None:
                if _dist2(p[0], p[2], spawn[0], spawn[2]) < SPAWN_MONSTER_MIN:
                    errors.append("monsterSpots[%d] too close to player spawn" % i)

    for cat, count_key in SPOT_FIELDS:
        raw = cfg.get(cat)
        want = cfg.get(count_key)
        if isinstance(raw, list) and _is_num(want) and len(raw) < int(want):
            errors.append("%s=%d exceeds %s spots (%d)"
                          % (count_key, int(want), cat, len(raw)))

    if rooms and spawn is not None and exit_ is not None:
        spawn_room = next((r for r in range(len(rooms))
                           if _inside(spawn[0], spawn[2], rooms[r], -EPS)), None)
        exit_room = next((r for r in range(len(rooms))
                          if _inside(exit_[0], exit_[2], rooms[r], -EPS)), None)
        if spawn_room is not None and exit_room is not None:
            adj = {r: set() for r in range(len(rooms))}
            for c in corridors:
                a, b = c["rooms"]
                if a is not None and b is not None and a != b:
                    adj[a].add(b)
                    adj[b].add(a)
            seen = {spawn_room}
            stack = [spawn_room]
            while stack:
                cur = stack.pop()
                for nxt in adj[cur]:
                    if nxt not in seen:
                        seen.add(nxt)
                        stack.append(nxt)
            if exit_room not in seen:
                errors.append("exit is not reachable from player spawn")
            for r in range(len(rooms)):
                if r not in seen:
                    errors.append("rooms[%d] is unreachable from spawn" % r)

    return errors


def validate_request(body):
    """Validate a POST /api/levels/next body. Empty list means valid."""
    errors = []
    if not isinstance(body, dict):
        return ["request body must be a JSON object"]
    if body.get("schemaVersion") != 1:
        errors.append("unsupported schemaVersion %r (want 1)" % (body.get("schemaVersion"),))
    cleared = body.get("clearedLevelIndex")
    if isinstance(cleared, bool) or not isinstance(cleared, int) or cleared < 1:
        errors.append("clearedLevelIndex must be an integer >= 1")
    if "seed" in body and body["seed"] is not None:
        seed = body["seed"]
        if isinstance(seed, bool) or not isinstance(seed, (int, str)):
            errors.append("seed must be an int or string")
    report = body.get("report")
    if not isinstance(report, dict):
        errors.append("report must be a LevelReport object")
        return errors
    for key in REQUIRED_REPORT_INTS:
        v = report.get(key)
        if isinstance(v, bool) or not isinstance(v, int) or v < 0:
            errors.append("report.%s must be an integer >= 0" % key)
    for key in OPTIONAL_REPORT_NUMS:
        if key in report and report[key] is not None:
            v = report[key]
            if not _is_num(v) or v < 0:
                errors.append("report.%s must be a number >= 0" % key)
    return errors


def main(paths):
    failed = False
    for path in paths:
        try:
            with open(path, "r", encoding="utf-8") as f:
                cfg = json.load(f)
        except (OSError, ValueError) as e:
            print("%s: unreadable (%s)" % (path, e))
            failed = True
            continue
        errors = validate_config(cfg)
        if errors:
            failed = True
            print("%s: INVALID" % path)
            for e in errors:
                print("  - %s" % e)
        else:
            print("%s: valid" % path)
    return 1 if failed else 0


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("usage: python level_generator/validate.py <level.json> [...]")
        sys.exit(2)
    sys.exit(main(sys.argv[1:]))

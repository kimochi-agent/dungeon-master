"""Layout templates for generated levels (v1: fixed templates, no procedural geometry).

halls/turn copy the proven bundled geometry (level1/level2.json) with extra
spawn spots appended, so tuning can scale counts up without hand-authoring
new coordinates. spire is halls rotated 90 degrees. Extra spots sit at the
end of each list: counts cap the leading spots, so higher difficulties use
the deeper rooms first only when counts grow.

 Coordinates use Unity's JSON shapes: Vector2 XZ as {x, y} (y is Z),
 Vector3 as {x, y, z}. Field names must stay identical to LevelData.
"""

import copy
import hashlib
import json

TITLES = {"halls": "Echoing Halls", "turn": "Sunken Turn", "spire": "The Spire"}


def _v3(x, y, z):
    return {"x": float(x), "y": float(y), "z": float(z)}


HALLS = {
    "rooms": [
        {"center": {"x": 0.0, "y": 0.0}, "size": {"x": 20.0, "y": 20.0}},
        {"center": {"x": 25.0, "y": 0.0}, "size": {"x": 14.0, "y": 14.0}},
        {"center": {"x": 46.0, "y": 0.0}, "size": {"x": 12.0, "y": 12.0}},
    ],
    "corridors": [
        {"from": {"x": 10.0, "y": 0.0}, "to": {"x": 18.0, "y": 0.0}, "width": 4.0},
        {"from": {"x": 32.0, "y": 0.0}, "to": {"x": 40.0, "y": 0.0}, "width": 4.0},
    ],
    "doors": [
        {"position": {"x": 18.0, "y": 1.5, "z": 0.0}, "alongX": True},
        {"position": {"x": 32.0, "y": 1.5, "z": 0.0}, "alongX": True},
    ],
    "pillars": [_v3(-5, 0, -5), _v3(-5, 0, 5), _v3(25, 0, 3)],
    "playerSpawn": _v3(-6, 1, 0),
    "exitPoint": _v3(46, 1, 0),
    "coinSpots": [
        _v3(-3, 0.6, -3), _v3(0, 0.6, 6), _v3(3, 0.6, 0), _v3(6, 0.6, -4),
        _v3(-6, 0.6, 3), _v3(12, 0.6, 0), _v3(16, 0.6, 0), _v3(20, 0.6, -4),
        _v3(28, 0.6, 4), _v3(30, 0.6, -4),
    ],
    "powerUpSpots": [_v3(6, 0.6, 6), _v3(22, 0.6, 4), _v3(29, 0.6, 0)],
    "chestSpots": [_v3(-8, 0, 8), _v3(30, 0, 5)],
    "lockedChestSpots": [_v3(8, 0, -8), _v3(27, 0, -5)],
    "monsterSpots": [
        _v3(-2, 1, -7), _v3(7, 1, 2), _v3(24, 1, -4),
        _v3(20, 1, 4), _v3(30, 1, 2), _v3(46, 1, 4), _v3(46, 1, -3),
    ],
}

TURN = {
    "rooms": [
        {"center": {"x": 0.0, "y": 0.0}, "size": {"x": 16.0, "y": 16.0}},
        {"center": {"x": 0.0, "y": -22.0}, "size": {"x": 12.0, "y": 12.0}},
        {"center": {"x": 19.0, "y": -22.0}, "size": {"x": 10.0, "y": 10.0}},
    ],
    "corridors": [
        {"from": {"x": 0.0, "y": -8.0}, "to": {"x": 0.0, "y": -16.0}, "width": 4.0},
        {"from": {"x": 6.0, "y": -22.0}, "to": {"x": 14.0, "y": -22.0}, "width": 4.0},
    ],
    "doors": [
        {"position": {"x": 0.0, "y": 1.5, "z": -16.0}, "alongX": False},
    ],
    "pillars": [_v3(4, 0, 4), _v3(21, 0, -24)],
    "playerSpawn": _v3(-5, 1, -5),
    "exitPoint": _v3(19, 1, -22),
    "coinSpots": [
        _v3(-4, 0.6, -4), _v3(3, 0.6, -3), _v3(-3, 0.6, -20),
        _v3(3, 0.6, -25), _v3(17, 0.6, -20), _v3(16, 0.6, -25),
        _v3(6, 0.6, -1), _v3(-1, 0.6, 2), _v3(0, 0.6, -18), _v3(20, 0.6, -25),
    ],
    "powerUpSpots": [_v3(0, 0.6, 6), _v3(-4, 0.6, -24), _v3(16, 0.6, -18)],
    "chestSpots": [_v3(-6, 0, 6), _v3(5, 0, -27)],
    "lockedChestSpots": [_v3(22, 0, -19), _v3(-5, 0, -17)],
    "monsterSpots": [
        _v3(5, 1, -5), _v3(-4, 1, -20), _v3(4, 1, -25), _v3(19, 1, -19),
        _v3(0, 1, 0), _v3(-2, 1, -26), _v3(22, 1, -25),
    ],
}


def _clean(v):
    # Avoid -0.0 in JSON output; keeps fixtures tidy.
    return v + 0.0 if v != 0 else 0.0


def rotate90(template):
    """Return template rotated 90 degrees CCW: (x, z) -> (-z, x)."""
    out = copy.deepcopy(template)
    for room in out["rooms"]:
        cx, cz = room["center"]["x"], room["center"]["y"]
        sx, sz = room["size"]["x"], room["size"]["y"]
        room["center"] = {"x": _clean(-cz), "y": _clean(cx)}
        room["size"] = {"x": sz, "y": sx}
    for c in out["corridors"]:
        for end in ("from", "to"):
            x, z = c[end]["x"], c[end]["y"]
            c[end] = {"x": _clean(-z), "y": _clean(x)}
    for d in out["doors"]:
        p = d["position"]
        d["position"] = {"x": _clean(-p["z"]), "y": p["y"], "z": _clean(p["x"])}
        d["alongX"] = not d["alongX"]
    for key in ("pillars", "coinSpots", "powerUpSpots", "chestSpots",
                "lockedChestSpots", "monsterSpots"):
        out[key] = [{"x": _clean(-p["z"]), "y": p["y"], "z": _clean(p["x"])}
                    for p in out[key]]
    for key in ("playerSpawn", "exitPoint"):
        p = out[key]
        out[key] = {"x": _clean(-p["z"]), "y": p["y"], "z": _clean(p["x"])}
    return out


TEMPLATES = {"halls": HALLS, "turn": TURN, "spire": rotate90(HALLS)}
ORDER = ["halls", "turn", "spire"]


def choose_template(seed, level_index):
    """Deterministic template pick; varies across levels for the same seed."""
    return ORDER[(int(seed) + int(level_index)) % len(ORDER)]


def capacities(layout_key):
    """Available spot counts per count key for a template."""
    t = TEMPLATES[layout_key]
    return {
        "coinCount": len(t["coinSpots"]),
        "powerUpCount": len(t["powerUpSpots"]),
        "chestCount": len(t["chestSpots"]),
        "lockedChestCount": len(t["lockedChestSpots"]),
        "monsterCount": len(t["monsterSpots"]),
    }


def build_config(layout_key, knobs, level_index):
    """Full levelN.json-shaped config: template layout + tuning knobs."""
    config = copy.deepcopy(TEMPLATES[layout_key])
    config.update(knobs)
    config["levelName"] = "Level %d - %s" % (level_index, TITLES[layout_key])
    return config


def layout_hash(config):
    canonical = json.dumps(config, sort_keys=True, separators=(",", ":"))
    return hashlib.sha256(canonical.encode("utf-8")).hexdigest()[:16]

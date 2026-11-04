"""Self-check for the level generator (stdlib only, no framework).

Covers: bundled levels validate, templates validate, validator negatives,
tuning buckets/clamps, end-to-end determinism, golden-response regression,
and live HTTP behavior against an in-process server.

Run from the repo root:  python level_generator/check.py
"""

import copy
import json
import os
import sys
import threading
import urllib.error
import urllib.request
from http.server import ThreadingHTTPServer

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "level_generator"))

import app
import layouts
import tuning
import validate

FIX = os.path.join(ROOT, "level_generator", "fixtures")
GAME_CONFIG = os.path.join(ROOT, "game", "Dungeon-Master", "Assets", "Resources", "Config")

passed = 0
failed = 0


def check(name, cond, detail=""):
    global passed, failed
    if cond:
        passed += 1
        print("PASS %s" % name)
    else:
        failed += 1
        print("FAIL %s %s" % (name, ("-- " + str(detail)) if detail else ""))


def load_fixture(name):
    with open(os.path.join(FIX, name), "r", encoding="utf-8") as f:
        return json.load(f)


def sample_config():
    body = load_fixture("standard.json")
    key = layouts.choose_template(body["seed"], body["clearedLevelIndex"] + 1)
    knobs = tuning.tune(body["report"], body["clearedLevelIndex"] + 1,
                        layouts.capacities(key))
    return layouts.build_config(key, knobs, body["clearedLevelIndex"] + 1)


# 1. Bundled levels are valid (ground truth for the validator).
for n in (1, 2):
    path = os.path.join(GAME_CONFIG, "level%d.json" % n)
    with open(path, "r", encoding="utf-8") as f:
        cfg = json.load(f)
    errs = validate.validate_config(cfg)
    check("bundled level%d valid" % n, not errs, "; ".join(errs))

# 2. Every template builds a valid config under each tuning bucket.
for key in layouts.ORDER:
    for fix in ("standard.json", "struggling.json", "mastery.json"):
        body = load_fixture(fix)
        knobs = tuning.tune(body["report"], 3, layouts.capacities(key))
        cfg = layouts.build_config(key, knobs, 3)
        errs = validate.validate_config(cfg)
        check("template %s + %s valid" % (key, fix), not errs, "; ".join(errs))


# 3. Validator negatives: each mutation must be rejected.
def expect_invalid(name, mutate, keyword):
    cfg = sample_config()
    mutate(cfg)
    errs = validate.validate_config(cfg)
    joined = " ".join(errs)
    check("reject " + name, bool(errs) and keyword in joined, joined or "no errors")


def _overlap_rooms(cfg):
    cfg["rooms"][1] = copy.deepcopy(cfg["rooms"][0])


def _diagonal_corridor(cfg):
    c = cfg["corridors"][0]
    c["to"] = {"x": c["from"]["x"] + 8.0, "y": c["from"]["y"] + 7.0}


def _corridor_to_void(cfg):
    cfg["corridors"][0]["to"] = {"x": 999.0, "y": 999.0}


def _corridor_overlaps_room(cfg):
    cfg["corridors"][0]["from"] = {"x": -5.0, "y": 0.0}
    cfg["corridors"][0]["to"] = {"x": 5.0, "y": 0.0}


def _door_off_plane(cfg):
    cfg["doors"][0]["position"]["x"] += 5.0


def _door_no_mouth(cfg):
    # Slide along the wall: still on the plane, away from the corridor.
    if cfg["doors"][0]["alongX"]:
        cfg["doors"][0]["position"]["z"] += 6.0
    else:
        cfg["doors"][0]["position"]["x"] += 6.0


def _spot_out_of_bounds(cfg):
    cfg["coinSpots"][0] = {"x": 999.0, "y": 0.6, "z": 999.0}


def _spot_in_pillar(cfg):
    p = cfg["pillars"][0]
    cfg["coinSpots"][0] = {"x": p["x"] + 0.5, "y": 0.6, "z": p["z"] + 0.5}


def _monster_on_spawn(cfg):
    cfg["monsterSpots"][0] = copy.deepcopy(cfg["playerSpawn"])


def _exit_unreachable(cfg):
    cfg["rooms"].append({"center": {"x": 500.0, "y": 500.0},
                         "size": {"x": 10.0, "y": 10.0}})
    cfg["exitPoint"] = {"x": 500.0, "y": 1.0, "z": 500.0}


def _counts_exceed_spots(cfg):
    cfg["coinCount"] = len(cfg["coinSpots"]) + 5


def _tuning_out_of_range(cfg):
    cfg["monsterDamage"] = 999


def _tuning_missing(cfg):
    del cfg["monsterDamage"]


expect_invalid("overlapping rooms", _overlap_rooms, "overlap")
expect_invalid("diagonal corridor", _diagonal_corridor, "axis-aligned")
expect_invalid("corridor end in void", _corridor_to_void, "outside any room")
expect_invalid("corridor overlapping room", _corridor_overlaps_room, "overlaps rooms")
expect_invalid("door off plane", _door_off_plane, "side plane")
expect_invalid("door without corridor mouth", _door_no_mouth, "corridor mouth")
expect_invalid("spot out of bounds", _spot_out_of_bounds, "outside every room")
expect_invalid("spot inside pillar", _spot_in_pillar, "pillar")
expect_invalid("monster on spawn", _monster_on_spawn, "spawn")
expect_invalid("unreachable exit", _exit_unreachable, "reach")
expect_invalid("counts exceed spots", _counts_exceed_spots, "exceeds")
expect_invalid("tuning out of range", _tuning_out_of_range, "out of range")
expect_invalid("tuning key missing", _tuning_missing, "missing")

# Request validation negatives.
for name, body, keyword in [
    ("request not an object", [], "JSON object"),
    ("request bad schemaVersion", {"schemaVersion": 99}, "schemaVersion"),
    ("request missing report", {"schemaVersion": 1, "clearedLevelIndex": 1}, "report"),
    ("request negative deaths",
     {"schemaVersion": 1, "clearedLevelIndex": 1,
      "report": {"attempts": 1, "deaths": -1, "damageTaken": 0, "lockedChestsDestroyed": 0}},
     "report.deaths"),
]:
    errs = validate.validate_request(body)
    check("reject " + name, any(keyword in e for e in errs), "; ".join(errs))

# 4. Tuning buckets, clamps, determinism.
std_report = load_fixture("standard.json")["report"]
check("bucket standard", tuning.difficulty_bucket(std_report) == "standard")
check("bucket gentle",
      tuning.difficulty_bucket(load_fixture("struggling.json")["report"]) == "gentle")
check("bucket spicy",
      tuning.difficulty_bucket(load_fixture("mastery.json")["report"]) == "spicy")

caps = layouts.capacities("halls")
tiny = {k: 1 for k in caps}
knobs_tiny = tuning.tune(std_report, 2, tiny)
check("counts clamp to capacities",
      all(knobs_tiny[k] <= 1 for k in
          ("coinCount", "powerUpCount", "chestCount", "lockedChestCount", "monsterCount")),
      json.dumps(knobs_tiny, sort_keys=True))
knobs_full = tuning.tune(std_report, 2, caps)
in_range = all(lo <= knobs_full[k] <= hi for k, (lo, hi) in validate.TUNING_RANGES.items()
               if k in knobs_full)
check("knobs within tuning ranges", in_range, json.dumps(knobs_full, sort_keys=True))
check("tune deterministic",
      tuning.tune(std_report, 2, caps) == tuning.tune(std_report, 2, caps))

# 5. End-to-end determinism: same request -> identical bytes.
std_body = load_fixture("standard.json")
status1, payload1 = app.generate(copy.deepcopy(std_body))
status2, payload2 = app.generate(copy.deepcopy(std_body))
canon1 = json.dumps(payload1, sort_keys=True)
canon2 = json.dumps(payload2, sort_keys=True)
check("generate deterministic",
      status1 == 200 and status2 == 200 and canon1 == canon2,
      "%s %s" % (status1, status2))
check("generated config validates",
      not validate.validate_config(payload1["config"]),
      "; ".join(validate.validate_config(payload1["config"])))

# 6. Golden response regression (written on first run, compared after).
gold_path = os.path.join(FIX, "golden_response.json")
if not os.path.exists(gold_path):
    with open(gold_path, "w", encoding="utf-8") as f:
        json.dump(payload1, f, indent=2, sort_keys=True)
    print("INFO golden_response.json written; re-run to verify stability")
    passed += 1
else:
    with open(gold_path, "r", encoding="utf-8") as f:
        golden = json.load(f)
    check("golden response stable", golden == payload1)


# 7. Live HTTP behavior against an in-process server.
def post(port, path, data=None, raw=None):
    body = raw if raw is not None else json.dumps(data).encode("utf-8")
    req = urllib.request.Request("http://127.0.0.1:%d%s" % (port, path), data=body,
                                 headers={"Content-Type": "application/json"},
                                 method="POST")
    try:
        with urllib.request.urlopen(req, timeout=10) as resp:
            return resp.status, json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        try:
            return e.code, json.loads(e.read().decode("utf-8"))
        except ValueError:
            return e.code, {}


def get(port, path):
    try:
        with urllib.request.urlopen("http://127.0.0.1:%d%s" % (port, path),
                                    timeout=10) as resp:
            return resp.status, json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        return e.code, {}


try:
    server = ThreadingHTTPServer(("127.0.0.1", 0), app.Handler)
except OSError as e:
    server = None
    print("SKIP live HTTP tests: cannot bind a socket here (%s)" % e)
if server is None:
    print("\n%d passed, %d failed (HTTP tests skipped)" % (passed, failed))
    sys.exit(1 if failed else 0)
threading.Thread(target=server.serve_forever, daemon=True).start()
try:
    port = server.server_address[1]
    status, payload = post(port, "/api/levels/next", std_body)
    check("POST valid -> 200", status == 200, "%s %s" % (status, payload.get("error", "")))
    check("response envelope complete",
          all(k in payload for k in ("levelIndex", "seed", "generatorVersion",
                                     "layoutId", "layoutHash", "difficulty", "config")),
          sorted(payload.keys()))
    if status == 200:
        errs = validate.validate_config(payload["config"])
        check("served config validates", not errs, "; ".join(errs))
    status, _payload = post(port, "/api/levels/next", {"schemaVersion": 1})
    check("POST invalid -> 422", status == 422, status)
    status, _payload = post(port, "/api/levels/next", raw=b"{oops")
    check("POST malformed -> 400", status == 400, status)
    status, _payload = post(port, "/nope", std_body)
    check("POST unknown path -> 404", status == 404, status)
    status, payload = get(port, "/healthz")
    check("GET /healthz -> 200", status == 200 and payload.get("ok") is True,
          "%s %s" % (status, payload))
finally:
    server.shutdown()
    server.server_close()

print("\n%d passed, %d failed" % (passed, failed))
sys.exit(1 if failed else 0)

"""Level generator service (Python standard library only).

POST a cleared level's LevelReport, get the next level's config:

    LEVEL_GEN_PORT=8124 python level_generator/app.py

Dev loop (repo root):  python level_generator/check.py
See level_generator/contract.md for the API contract and
level_generator/README.md for device testing via adb reverse.
"""

import hashlib
import json
import os
import sys
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import layouts
import tuning
import validate

HOST = os.environ.get("LEVEL_GEN_HOST", "127.0.0.1")
PORT = int(os.environ.get("LEVEL_GEN_PORT", "8124"))
MAX_BODY = 65536


def _stable_int(text):
    return int(hashlib.sha256(text.encode("utf-8")).hexdigest()[:8], 16)


def normalize_seed(body):
    """Request seed, or a stable hash of the request (idempotent retries)."""
    seed = body.get("seed")
    if isinstance(seed, int) and not isinstance(seed, bool):
        return seed
    if isinstance(seed, str):
        return _stable_int("seed:" + seed)
    canonical = json.dumps({"c": body.get("clearedLevelIndex"), "r": body.get("report")},
                           sort_keys=True, separators=(",", ":"))
    return _stable_int(canonical)


def generate(body):
    """Build the next level. Returns (http_status, payload dict)."""
    errors = validate.validate_request(body)
    if errors:
        return 422, {"error": "; ".join(errors)}
    cleared = body["clearedLevelIndex"]
    next_index = cleared + 1
    seed = normalize_seed(body)
    report = body["report"]
    bucket = tuning.difficulty_bucket(report)

    first = layouts.choose_template(seed, next_index)
    tried = []
    for key in [first] + [k for k in layouts.ORDER if k != first]:
        knobs = tuning.tune(report, next_index, layouts.capacities(key))
        config = layouts.build_config(key, knobs, next_index)
        config_errors = validate.validate_config(config)
        tried.append(key)
        if not config_errors:
            return 200, {
                "levelIndex": next_index,
                "seed": seed,
                "generatorVersion": tuning.GENERATOR_VERSION,
                "layoutId": key,
                "layoutHash": layouts.layout_hash(config),
                "difficulty": bucket,
                "config": config,
            }
        print("generator: template %s failed validation, trying next (%s)"
              % (key, "; ".join(config_errors)), file=sys.stderr)
    return 500, {"error": "generator produced no valid level (tried %s)" % ",".join(tried)}


class Handler(BaseHTTPRequestHandler):
    server_version = "LevelGen/1"

    def log_message(self, *args):
        sys.stderr.write("%s %s\n" % (self.command, self.path))

    def _send(self, payload, code=200):
        body = json.dumps(payload).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        if self.path.split("?")[0] == "/healthz":
            return self._send({"ok": True, "generatorVersion": tuning.GENERATOR_VERSION})
        return self._send({"error": "not found"}, 404)

    def do_POST(self):
        if self.path.split("?")[0] != "/api/levels/next":
            return self._send({"error": "not found"}, 404)
        try:
            length = int(self.headers.get("Content-Length", "0") or 0)
        except ValueError:
            return self._send({"error": "bad Content-Length"}, 400)
        if length <= 0:
            return self._send({"error": "empty body"}, 400)
        if length > MAX_BODY:
            return self._send({"error": "body too large"}, 413)
        try:
            body = json.loads(self.rfile.read(length).decode("utf-8"))
        except (ValueError, UnicodeDecodeError):
            return self._send({"error": "malformed JSON"}, 400)
        status, payload = generate(body)
        return self._send(payload, status)


def main():
    server = ThreadingHTTPServer((HOST, PORT), Handler)
    print("Level generator (%s) on http://%s:%d/api/levels/next"
          % (tuning.GENERATOR_VERSION, HOST, PORT))
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass


if __name__ == "__main__":
    main()

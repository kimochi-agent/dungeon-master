"""Dungeon Master build dashboard server (Python standard library only).

Serves the repo so the dashboard can read Plan.md and agent state, and exposes
pause/resume controls that the builder/verifier agents honor via control.json.

Run from the repo root:  python dashboard/server.py
Then open:  http://localhost:8123/dashboard/
"""

import json
import os
import re
from datetime import datetime, timezone
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PLAN_PATH = os.path.join(ROOT, "Plan.md")
STATUS_PATH = os.path.join(ROOT, "agents", "status.json")
CONTROL_PATH = os.path.join(ROOT, "agents", "control.json")
REPORT_PATH = os.path.join(ROOT, "agents", "report.json")
VERDICT_PATH = os.path.join(ROOT, "agents", "verdict.json")
PORT = int(os.environ.get("PORT", "8123"))

STEP_RE = re.compile(r"^(\d+)\.\s+(.*)\[(TODO|IN PROGRESS|IN REVIEW|PASSED|NEEDS FIX)\]\s*$")


def now_iso():
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def read_json(path, default):
    try:
        with open(path, "r", encoding="utf-8") as f:
            return json.load(f)
    except (FileNotFoundError, json.JSONDecodeError):
        return default


def write_json(path, data):
    tmp = path + ".tmp"
    with open(tmp, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)
    os.replace(tmp, path)


def read_text(path):
    try:
        with open(path, "r", encoding="utf-8") as f:
            return f.read()
    except FileNotFoundError:
        return ""


def add_log(status, agent, message):
    log = status.setdefault("log", [])
    log.append({"time": now_iso(), "agent": agent, "message": message})
    del log[:-20]
    status["updated"] = now_iso()
    return status


def apply_verdict(step_num, verdict, note):
    """Flip step step_num to PASSED / NEEDS FIX (+ Fix: lines)."""
    with open(PLAN_PATH, "r", encoding="utf-8") as f:
        lines = f.read().split("\n")
    for i, line in enumerate(lines):
        m = STEP_RE.match(line)
        if m and int(m.group(1)) == step_num:
            if m.group(3) != "IN REVIEW":
                return False, "step is not in review"
            tag = "PASSED" if verdict == "PASS" else "NEEDS FIX"
            lines[i] = f"{step_num}. {m.group(2).rstrip()} [{tag}]"
            if verdict == "FAIL":
                notes = [ln.strip()[:300] for ln in note.split("\n") if ln.strip()][:10]
                for j, fix in enumerate(notes):
                    lines.insert(i + 1 + j, f"  - Fix: {fix}")
            with open(PLAN_PATH, "w", encoding="utf-8") as f:
                f.write("\n".join(lines))
            return True, ""
    return False, "step not found"


class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=ROOT, **kwargs)

    def log_message(self, *args):
        pass

    def send_json(self, data, code=200):
        body = json.dumps(data).encode("utf-8")
        self.send_response(code)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        if urlparse(self.path).path == "/":
            self.send_response(302)
            self.send_header("Location", "/dashboard/")
            self.end_headers()
            return
        if urlparse(self.path).path == "/api/state":
            self.send_json({
                "control": read_json(CONTROL_PATH, {"paused": False}),
                "status": read_json(STATUS_PATH, {"builder": {}, "verifier": {}, "log": []}),
                "plan": read_text(PLAN_PATH),
                "report": read_json(REPORT_PATH, {}),
                "verdict": read_json(VERDICT_PATH, {"verdict": None}),
            })
            return
        return super().do_GET()

    def do_POST(self):
        path = urlparse(self.path).path
        length = int(self.headers.get("Content-Length", "0") or 0)
        try:
            payload = json.loads(self.rfile.read(length) or b"{}")
        except json.JSONDecodeError:
            payload = {}
        if path == "/api/verdict":
            return self.handle_verdict(payload)
        if path not in ("/api/pause", "/api/resume"):
            return self.send_json({"error": "not found"}, 404)
        paused = path == "/api/pause"
        control = {"paused": paused, "updated": now_iso(),
                   "note": str(payload.get("note", ""))[:200]}
        write_json(CONTROL_PATH, control)
        status = read_json(STATUS_PATH, {"builder": {}, "verifier": {}, "log": []})
        verb = "Paused" if paused else "Resumed"
        reason = f": {control['note']}" if control["note"] else ""
        write_json(STATUS_PATH, add_log(status, "you", f"{verb} the build{reason}."))
        self.send_json({"control": control})

    def handle_verdict(self, payload):
        verdict = str(payload.get("verdict", "")).upper()
        note = str(payload.get("note", ""))[:2000]
        if verdict not in ("PASS", "FAIL"):
            return self.send_json({"error": "verdict must be PASS or FAIL"}, 400)
        if verdict == "FAIL" and not note.strip():
            return self.send_json({"error": "a FAIL verdict needs a note describing the problem"}, 400)
        report = read_json(REPORT_PATH, {})
        try:
            step_num = int(report.get("step"))
        except (TypeError, ValueError):
            return self.send_json({"error": "no step is waiting for review"}, 409)
        ok, err = apply_verdict(step_num, verdict, note)
        if not ok:
            return self.send_json({"error": err}, 409)
        record = {"step": step_num, "verdict": verdict, "note": note.strip()[:2000],
                  "updated": now_iso()}
        write_json(VERDICT_PATH, record)
        status = read_json(STATUS_PATH, {"builder": {}, "verifier": {}, "log": []})
        detail = f": {note.strip()[:200]}" if verdict == "FAIL" else ""
        write_json(STATUS_PATH, add_log(status, "you", f"Verdict on step {step_num}: {verdict}{detail}."))
        self.send_json({"verdict": record})


if __name__ == "__main__":
    ThreadingHTTPServer.allow_reuse_address = False
    server = ThreadingHTTPServer(("127.0.0.1", PORT), Handler)
    print(f"Dungeon Master dashboard: http://localhost:{PORT}/dashboard/")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass

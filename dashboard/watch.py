"""Watches agents/verdict.json and prints a line whenever the user records a
PASS/FAIL verdict in the dashboard. The agent session monitors this output to
know when to advance to the next step or fix the current one.

Run:  python dashboard/watch.py
"""

import json
import os
import sys
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
VERDICT_PATH = os.path.join(ROOT, "agents", "verdict.json")


def main():
    last = None
    while True:
        time.sleep(5)
        try:
            with open(VERDICT_PATH, "r", encoding="utf-8") as f:
                content = f.read()
        except FileNotFoundError:
            continue
        if content == last:
            continue
        last = content
        try:
            data = json.loads(content) or {}
        except json.JSONDecodeError:
            continue
        verdict = data.get("verdict")
        if verdict in ("PASS", "FAIL"):
            print(f"VERDICT {verdict} step={data.get('step')} note={data.get('note', '')}", flush=True)


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        pass

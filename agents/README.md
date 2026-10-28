# Game-Builder-Agent System

Two agents build the game step by step from [Plan.md](../Plan.md), in a loop with you.

## The agents

- **Builder** ([builder.md](builder.md)) — reads the next unfinished step in Plan.md and implements it.
- **Verifier** ([verifier.md](verifier.md)) — checks the built step against the plan, recommends PASS or FAIL, and on failure writes fix notes into Plan.md so the builder can retry.

## The loop

```
Builder: pick first step not marked [PASSED] -> implement -> [IN REVIEW] + report.json
Verifier: check the work -> write recommendation into report.json
You (dashboard): review changes -> PASS or FAIL (+fix notes) -> verdict.json
  ├─ PASS -> Plan.md [PASSED] -> agent commits + pushes -> Builder takes next step
  └─ FAIL -> Plan.md [NEEDS FIX] + Fix: notes -> Builder fixes -> [IN REVIEW] ...
```
The agent watches `agents/verdict.json` (via `dashboard/watch.py`), so your
dashboard verdict advances the loop without going back to chat.

## Shared conventions

- **Status tags** live at the end of each step line in Plan.md:
  `[TODO]` -> `[IN PROGRESS]` -> `[IN REVIEW]` -> `[PASSED]`, or `[NEEDS FIX]`.
- Only the verifier, after your confirmation, may mark a step `[PASSED]`. The builder
  must never mark its own work passed.
- Fix notes go directly under the failed step as sub-bullets starting with `Fix:`.
- Context files both agents must read before acting: `Readme.md`, `MEMO.md`, `Plan.md`.
- The Unity project lives in `game/Dungeon-Master/` (per MEMO.md). Design notes go in `docs/`.

## How to run

1. Start a session with the builder spec: "Follow agents/builder.md and build the next step."
2. When the builder reports `[IN REVIEW]`, start a session with the verifier spec:
   "Follow agents/verifier.md and verify the step."
3. Confirm or override the verifier's PASS/FAIL recommendation. On FAIL, re-run the
   builder: "Follow agents/builder.md and fix the step."
4. After you confirm PASS on a step, its changes are committed and pushed to the
   remote before the next step starts (if the agent's push is blocked by
   credentials, run `git push` yourself — the commits are ready locally).
5. Repeat until all steps are `[PASSED]`.

## Dashboard and pause/resume

- Start the dashboard from the repo root: `python dashboard/server.py`, then open
  http://localhost:8123/dashboard/ in a browser. It shows the current step, the
  remaining plan, what each agent is doing, and a Pause/Resume button.
- Pause writes `agents/control.json`; both agents must read it at the start of
  every run and stop if `paused` is true. Pausing does not interrupt an agent
  mid-run — it stops the loop from starting new work.
- Both agents keep `agents/status.json` updated so the dashboard stays live.
  You can also edit `control.json` by hand if the server is not running.

## Stop rules

- After 3 failed fix cycles on one step, both agents must stop and escalate to you
  instead of looping again.
- Either agent must stop and ask you before changing the behavior of an already
  `[PASSED]` step, or before changing scope (README/MEMO decisions).

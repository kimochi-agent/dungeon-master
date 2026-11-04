# Level generator API plan (2026-11-04)

Status (2026-11-04): all steps implemented. Service (`level_generator/`,
`check.py` 44/44 green incl. live HTTP) + Unity client (fetch on
level-complete, strict validation, download precedence, Home retry,
cleartext manifest). Full local APK build green
(`BuildScript.BuildAndroid`: Succeeded, ~39MB APK). Remaining for you:
on-device playtest (service + `adb reverse` + install) and, later, hosted
deployment (https, auth, `0.0.0.0`).

## Goal

After clearing a level, the game POSTs that level's analytics to a generator API and receives the next level's JSON (`levelN+1`), generated from previous-level performance — replacing the current bundled-only campaign with adaptive, server-driven levels, with bundled files as offline fallback.

## Success Criteria

- Clearing level N with network access unlocks a playable level N+1 whose tuning reflects the posted report (harder after easy clears, gentler after struggle).
- With no network (or API error), the game falls back to bundled `levelN.json` files with no user-visible breakage.
- Downloaded levels persist across sessions and validate against the `docs/levels.md` schema before loading.
- Only aggregate totals leave the device (already true of `GetPayload`).

## Approach

Three pieces, matching existing patterns:

1. **API contract (done).** `POST /api/levels/next` accepts `{schemaVersion, clearedLevelIndex, seed?, report}` where `report` is the `LevelReport` JSON from `LevelAnalytics.GetPayload()`, and returns `{levelIndex, seed, generatorVersion, layoutId, layoutHash, difficulty, config}` with `config` in the `levelN.json` schema the game already parses. Full spec: `level_generator/contract.md`.
2. **Generator service (done, `level_generator/`).** Small Python HTTP service following the existing `dashboard/server.py` stdlib pattern (no new dependencies): `app.py` (HTTP + generation with template fallback), `tuning.py` (report → difficulty buckets → knobs), `layouts.py` (3 templates complying with authoring rules), `validate.py` (schema + geometry checks; invalid requests get 422, invalid generations fall back across templates and never serve bad JSON). `tuning.py` is the seam where "another agent" plugs in later — the service owns HTTP + validation, the tuning rules live in one module an agent can own/replace.
3. **Unity client (done).** `LevelDownloader.cs` (POST on level-complete with 5s timeout + 3 attempts, strict validation, save to `Application.persistentDataPath/levels/levelN.json` + `.meta.json` provenance sidecar, verify-after-write; never re-requests or overwrites a saved valid level). `GameConfig` prefers valid downloads over bundled files (`HasLevel`/`LevelCount`/loader, `LevelSource` + log line per load, invalid files deleted). Fetch never blocks navigation; the downloader survives scene loads; Home retries a missing next level and refreshes its Play button live. Any failure silently keeps bundled levels.

Assumptions: API base URL is a single configurable value, overrideable without rebuild via an `api_base_url.txt` file in `persistentDataPath`. Dev default is `http://127.0.0.1:8124` — the Editor uses it directly; a real phone over USB reaches it via `adb reverse tcp:8124 tcp:8124` (same URL, no LAN/firewall setup). Generator stays rule-based until you ask for the agent/LLM version; bundled levels always ship as fallback.

## Steps

1. **Contract + fixtures (done).** `level_generator/contract.md` (request/response/validation rules) and fixtures: `standard`/`struggling`/`mastery` POST bodies plus a golden response regression file.
2. **Generator service (done).** `level_generator/app.py` (stdlib HTTP, binds `127.0.0.1:8124` via `LEVEL_GEN_HOST`/`LEVEL_GEN_PORT`), `tuning.py` (gentle/standard/spicy buckets with explicit formulas + clamps), `layouts.py` (`halls`/`turn`/`spire` templates), `validate.py` (schema + geometry + request checks, also a CLI), `check.py` (44-case self-check, no framework), `README.md` (run + device-testing notes).
3. **Unity client (done).** `LevelDownloader.cs` + `LevelValidator.cs` (strict client checks mirroring `validate.py`); `GameConfig` download precedence; `GameShell` fetch trigger + "Fetching next level..." status (silent on failure); `HomeScreen` live refresh + retry fetch; `Assets/Plugins/Android/AndroidManifest.xml` with dev-only `usesCleartextTraffic`.
4. **Docs (done).** `docs/levels.md` (Downloaded levels section, precedence, offline behavior) and `docs/android-build.md` (Level generator section: service + `adb reverse` + cleartext note).

## Validation Plan

- `python level_generator/check.py`: 44/44 green 2026-11-04 (validator, tuning, determinism, golden, live HTTP incl. 422/400/404 paths).
- Manual: `curl.exe -X POST http://127.0.0.1:8124/api/levels/next --data-binary "@level_generator/fixtures/standard.json"` returns 200 + schema-valid config.
- Unity: headless script compile green (Assembly-CSharp built, exit 0, no errors); full local APK build green (`Android build: Succeeded in ~180s`, ~39MB APK) — this also verified the manifest merge.
- Device (still needs you): start service + `adb reverse`, install APK, clear level 1 → level 2 loads from download (logcat shows the source lines); airplane mode → bundled level 2 loads, no error UI. Highest-risk check: the full loop on-device, since it spans service + client + persistence.
- Compile gate: existing CI Android build stays green (no workflow changes needed; service is outside `game/` so it does not trigger builds).

## Risks / Open Questions

- **Hosting: decided — localhost now, hosted later.** Dev runs on this PC (`127.0.0.1:8124`); devices connect via `adb reverse`. Deferred to the hosting phase: binding `0.0.0.0`, HTTPS, auth, deployment, and any identifier/privacy implications.
- **Android cleartext HTTP (done, dev only):** `Assets/Plugins/Android/CleartextLib.androidlib/` (a library manifest, which merges additively into Unity's default) sets `usesCleartextTraffic` so `http://` reaches the tunnel; without it, downloads fail silently back to bundled levels (check logcat source lines). History: a scoped `res/xml` config is banned in Unity 6; a main-manifest override first dropped the launcher entry (no app icon), then crashed on launch with a non-AppCompat theme — hence the additive library, guarded by `tools/check_manifest.py`. Remove before release / hosted builds (https).
- Risk: generator emits geometrically invalid levels → mitigated by server-side validation + template fallback + client validation + bundled fallback on any parse/load failure.
- Non-goals: LLM/agent-driven tuning (v2 seam only), auth/accounts, analytics upload beyond the existing aggregate payload.

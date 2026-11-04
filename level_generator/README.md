# Level generator (dev service)

Rule-based level generator for Dungeon Master. Runs on your PC for
development; hosted deployment comes later. Stdlib Python only.

## Quickstart

```powershell
# Self-check (validator, tuning, determinism, golden, live HTTP):
python level_generator/check.py

# Run the service on http://127.0.0.1:8124 :
python level_generator/app.py
```

## Try it

```powershell
# PowerShell:
Invoke-RestMethod http://127.0.0.1:8124/api/levels/next `
  -Method Post -ContentType 'application/json' `
  -InFile level_generator/fixtures/standard.json | ConvertTo-Json -Depth 6

# or curl:
curl.exe -X POST http://127.0.0.1:8124/api/levels/next `
  -H "Content-Type: application/json" `
  --data-binary "@level_generator/fixtures/standard.json"

# Health:
curl.exe http://127.0.0.1:8124/healthz
```

## Real phone over USB

The phone cannot reach your PC's localhost directly. Tunnel it:

```powershell
adb reverse tcp:8124 tcp:8124
```

then the device build uses `http://127.0.0.1:8124` (same URL as the
Editor). Keep `adb reverse --list` handy to confirm the tunnel is live.

## Files

- `contract.md` — API contract (request/response, errors, tuning rules).
- `app.py` — stdlib HTTP service (`LEVEL_GEN_HOST` / `LEVEL_GEN_PORT`).
- `tuning.py` — report → difficulty knobs (the agent/LLM seam for v2).
- `layouts.py` — layout templates (`halls`, `turn`, `spire`).
- `validate.py` — schema + geometry checks; also a CLI for level JSON.
- `check.py` — full self-check, no framework.
- `fixtures/` — sample requests (`standard`, `struggling`, `mastery`) and
  the golden response regression file.

## Unity wiring (next step, not yet built)

`LevelDownloader.cs` will POST on level-complete, save to
`Application.persistentDataPath/levels/levelN.json`, and `GameConfig`
will prefer downloaded configs over bundled ones with silent fallback.
Device builds also need the cleartext-HTTP allowance for `http://`
dev URLs (see `docs/level-generator-api.md`).

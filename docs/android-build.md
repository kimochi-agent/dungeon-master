# Android build checklist (step 14)

## One-time setup

- Unity Hub: Editor 6000.6.7f1 with the Android Build Support module
  (SDK, NDK, OpenJDK come with it).
- Connect the phone with USB debugging enabled, or plan to transfer the APK manually.
- Optional: set your own Company Name / Package Name in
  Edit > Project Settings > Player > Android (a debug-signed APK builds without it).

## Build

- In Unity: `Build > Android APK (Dungeon Master)`, or from a terminal:
  `& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" -batchmode -nographics -quit -projectPath "C:\Users\Kim\Desktop\MetaDevHackathon\dungeon-master\game\Dungeon-Master" -executeMethod BuildScript.BuildAndroid -logFile "C:\Users\Kim\Desktop\MetaDevHackathon\dungeon-master\game\Dungeon-Master\Builds\build.log"`
- Output: `game/Dungeon-Master/Builds/DungeonMaster.apk` (git-ignored).

## Install and verify

- `adb install -r game/Dungeon-Master/Builds/DungeonMaster.apk`, or copy the APK
  to the phone and install it.
- On the phone: menu shows, Start works, joystick appears, walk both halls,
  collect a coin, open a chest, kill a monster, reach hall C.
- Fresh-clone check: clone the repo somewhere new, open `game/Dungeon-Master`
  in Hub, build again — it must work with no manual setup.

## GitHub Actions (automatic APK on push)

`.github/workflows/build-android.yml` runs the same `BuildScript.BuildAndroid`
on every push that touches `game/` (plus manual runs). The APK is attached to
the run under Actions > Build Android APK > Artifacts > `DungeonMaster-apk`
(a `build.log` artifact is attached instead when the build fails).

Why a self-hosted runner: Unity Personal licenses cannot activate on
GitHub-hosted runners — offline (`.ulf`) activation is Enterprise/Industry
only, and game-ci deleted their activation helper action. So the job runs on
your own PC, where Unity is already activated. No secrets needed.

One-time runner setup (on the build PC, which needs git + git-lfs and the
Unity Editor above):

1. Repo page > Settings > Actions > Runners > New self-hosted runner >
   Windows. Download the runner package and extract it to a folder that is
   NOT inside this repo (e.g. `C:\actions-runner`).
2. In that folder, run `.\config.cmd --url <repo-url> --token <token>` with
   the URL/token shown on the page (accept the defaults; the `self-hosted`
   label is what the workflow asks for).
3. Run `.\run.cmd` and leave it running while you want builds to trigger.
   Optionally install it as a service afterwards (`.\svc install` under your
   own user account so it sees your Unity activation, then `.\svc start`).
4. Push any change under `game/` (or Actions > Build Android APK >
   Run workflow) and watch the run go green.

Notes:

- The PC must be on with the runner listening, or jobs queue until it is.
- Builds reuse the runner's persistent `Library/`, so repeat builds are much
  faster than the first one (~5-15 min).
- Pushes that only touch `dashboard/`, `docs/`, etc. do not trigger a build.
- The CI APK is debug-signed, same as the local build.
- If Unity is installed at a different path, update `UNITY_PATH` in the
  workflow file.
- Alternative if you ever get a paid Unity seat: a `UNITY_SERIAL` secret lets
  the build run on GitHub-hosted runners via game-ci instead.

## Known gaps

- APK is debug-signed; Play Store upload needs a release keystore + AAB (later step).
- Touch layout tuned for phones; tablets unverified.
- If a device log says `Can't add component because class 'X' doesn't exist`,
  that is managed code stripping (IL2CPP). `Assets/link.xml` already preserves
  the physics module; keep stripping at Medium or lower unless you re-test.

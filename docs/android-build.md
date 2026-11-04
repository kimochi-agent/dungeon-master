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

## Level generator (dev)

Device builds fetch generated levels from your PC:

1. Start the service on the PC: `python level_generator/app.py`
   (listens on `http://127.0.0.1:8124`).
2. Tunnel it to the phone: `adb reverse tcp:8124 tcp:8124`
   (verify with `adb reverse --list`).
3. Play: clearing level 1 downloads level 2; `adb logcat` shows
   `LevelDownloader: level 2 downloaded (...)` and
   `GameConfig: level 2 loaded from download.`

Notes:

- The build allows cleartext HTTP (`Assets/Plugins/Android/AndroidManifest.xml`,
  dev only) so `http://` reaches the tunnel; without it, downloads fail
  silently back to bundled levels. Remove before release / hosted builds.
- To point at another host without rebuilding, push a one-line URL file to
  `api_base_url.txt` in the app's `persistentDataPath` (on most devices:
  `/storage/emulated/0/Android/data/<package>/files/`, where `<package>` is
  the Player Settings application identifier).
- No network (or API down) is always safe: the game plays the bundled
  campaign with no error UI.

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
Unity Editor above). Then allow the runner to execute its scripts
(elevated PowerShell, one time only):
`Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope LocalMachine`


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

### APK versioning

Each CI build stamps `MAJOR.MINOR.<run-number>` (e.g. `1.0.47`): `MAJOR.MINOR`
comes from `APP_VERSION_MAJOR_MINOR` at the top of the workflow file -- bump it
when cutting a release -- and the patch is the ever-increasing GitHub run number,
also used as the Android `versionCode` (required to increase for Play Store).
The APK is named `DungeonMaster-v1.0.47.apk` and the artifact
`DungeonMaster-apk-v1.0.47`. Local builds without `-apkVersion` keep producing
plain `DungeonMaster.apk`; add `-apkVersion 1.0.99 -apkVersionCode 99` to the
command line for a versioned local build.
Notes:

- The PC must be on with the runner listening, or jobs queue until it is.
- Builds reuse the runner's persistent `Library/`, so repeat builds are much
  faster than the first one (~5-15 min).
- Pushes that only touch `dashboard/`, `docs/`, etc. do not trigger a build.
- The CI APK is debug-signed, same as the local build.
- If Unity is installed at a different path, update `UNITY_PATH` in the
  workflow file.
- Never open the runner's `_work` project copy in the Unity Editor: a second
  Unity instance makes the build abort within seconds with an empty-looking
  log. If a run's Unity step finishes in seconds with no APK, kill any stale
  `Unity.exe`, delete the runner's `_work\dungeon-master` folder, and re-run.
- Keep the runner's `_work` folder (its `Library/`) intact between runs:
  incremental builds take ~10 min because Unity reuses it. If you ever wipe
  it, the next run does a slow full first import; if a fresh import ever
  aborts in seconds, run one build manually on the runner machine to seed
  `Library/`, then re-run CI.
- Alternative if you ever get a paid Unity seat: a `UNITY_SERIAL` secret lets
  the build run on GitHub-hosted runners via game-ci instead.

## Known gaps

- APK is debug-signed; Play Store upload needs a release keystore + AAB (later step).
- Touch layout tuned for phones; tablets unverified.
- If a device log says `Can't add component because class 'X' doesn't exist`,
  that is managed code stripping (IL2CPP). `Assets/link.xml` already preserves
  the physics module; keep stripping at Medium or lower unless you re-test.

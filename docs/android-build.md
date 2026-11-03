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

`.github/workflows/build-android.yml` builds the same `BuildScript.BuildAndroid`
in the cloud (Unity 6000.6.7f1, Android) on every push that touches `game/`,
plus manual runs. The APK is attached to the run under
Actions > Build Android APK > Artifacts > `DungeonMaster-apk`.

One-time setup (Unity license for CI):

1. In the GitHub repo: Settings > Secrets and variables > Actions > New secret.
   Add `UNITY_EMAIL` and `UNITY_PASSWORD` (your Unity ID credentials).
2. Actions tab > `Request Unity activation file` > Run workflow.
   Download the `unity-activation-file` artifact (a `.alf` file).
3. Upload the `.alf` at <https://license.unity3d.com/manual> and download
   the returned `.ulf` license file.
4. Back in Actions secrets, add `UNITY_LICENSE` with the full `.ulf` content.
5. Re-run any build; it should go green. First build takes ~20-40 min
   (Unity + Android SDK download); later builds reuse the cached `Library/`.

Notes:

- Pushes that only touch `dashboard/`, `docs/`, etc. do not trigger a build.
- The CI APK is debug-signed, same as the local build.
- If `unity-request-activation` ever stops working for new Unity versions,
  the fallback is the same manual flow: generate a `.alf` on any machine
  via `Unity -batchmode -createManualActivationFile`, activate it at
  license.unity3d.com, and store the `.ulf` as `UNITY_LICENSE`.

## Known gaps

- APK is debug-signed; Play Store upload needs a release keystore + AAB (later step).
- Touch layout tuned for phones; tablets unverified.
- If a device log says `Can't add component because class 'X' doesn't exist`,
  that is managed code stripping (IL2CPP). `Assets/link.xml` already preserves
  the physics module; keep stripping at Medium or lower unless you re-test.

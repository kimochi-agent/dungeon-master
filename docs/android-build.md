# Android build checklist (step 14)

## One-time setup

- Unity Hub: Editor 6000.6.7f1 with the Android Build Support module
  (SDK, NDK, OpenJDK come with it).
- Connect the phone with USB debugging enabled, or plan to transfer the APK manually.
- Optional: set your own Company Name / Package Name in
  Edit > Project Settings > Player > Android (a debug-signed APK builds without it).

## Build

- In Unity: `Build > Android APK (Dungeon Master)`, or from a terminal:
  `& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" -batchmode -nographics -quit -projectPath "C:\Users\Kim\Desktop\MetaDevHackathon\dungeon-master\game\Dungeon-Master" -executeMethod BuildScript.BuildAndroid -logFile "C:\Users\Kim\Desktop\MetaDevHackathon\dungeon-master\game\Dungeon-Master\Builds\build.log`
- Output: `game/Dungeon-Master/Builds/DungeonMaster.apk` (git-ignored).

## Install and verify

- `adb install -r game/Dungeon-Master/Builds/DungeonMaster.apk`, or copy the APK
  to the phone and install it.
- On the phone: menu shows, Start works, joystick appears, walk both halls,
  collect a coin, open a chest, kill a monster, reach hall C.
- Fresh-clone check: clone the repo somewhere new, open `game/Dungeon-Master`
  in Hub, build again — it must work with no manual setup.

## Known gaps

- APK is debug-signed; Play Store upload needs a release keystore + AAB (later step).
- Touch layout tuned for phones; tablets unverified.
- If a device log says `Can't add component because class 'X' doesn't exist`,
  that is managed code stripping (IL2CPP). `Assets/link.xml` already preserves
  the physics module; keep stripping at Medium or lower unless you re-test.

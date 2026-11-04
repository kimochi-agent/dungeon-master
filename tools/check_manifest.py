"""Regression check for the Android manifest setup (stdlib only).

Guards the two manifest bugs already fixed once:
1. A main-manifest override at Assets/Plugins/Android/AndroidManifest.xml
   REPLACES Unity's default manifest: the first version dropped the
   launcher activity (app installed with no icon); the repaired version
   used a non-AppCompat theme (crash on launch). It must never come back.
2. The dev cleartext flag must reach the merged manifest, via the additive
   CleartextLib.androidlib library manifest (library manifests merge into
   Unity's default instead of replacing it).

Source checks always run. APK checks run when a built APK and aapt are
available; otherwise they report SKIP explicitly.

Run from the repo root:  python tools/check_manifest.py [path/to.apk]
"""

import glob
import os
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MAIN_OVERRIDE = os.path.join(ROOT, "game", "Dungeon-Master", "Assets",
                             "Plugins", "Android", "AndroidManifest.xml")
LIB_MANIFEST = os.path.join(ROOT, "game", "Dungeon-Master", "Assets",
                            "Plugins", "Android", "CleartextLib.androidlib",
                            "AndroidManifest.xml")
DEFAULT_APK = os.path.join(ROOT, "game", "Dungeon-Master", "Builds", "DungeonMaster.apk")
ANDROID_NS = "{http://schemas.android.com/apk/res/android}"

passed = 0
failed = 0
skipped = 0


def check(name, cond, detail=""):
    global passed, failed
    if cond:
        passed += 1
        print("PASS %s" % name)
    else:
        failed += 1
        print("FAIL %s %s" % (name, ("-- " + str(detail)) if detail else ""))


def skip(name, reason):
    global skipped
    skipped += 1
    print("SKIP %s (%s)" % (name, reason))


def find_aapt():
    found = shutil.which("aapt")
    if found:
        return found
    env = os.environ.get("AAPT")
    if env and os.path.isfile(env):
        return env
    for var in ("ANDROID_SDK_ROOT", "ANDROID_HOME"):
        sdk = os.environ.get(var)
        if sdk:
            found = glob.glob(os.path.join(sdk, "build-tools", "*", "aapt.exe"))
            found += glob.glob(os.path.join(sdk, "build-tools", "*", "aapt"))
            if found:
                return sorted(found)[-1]
    unity = glob.glob("C:/Program Files/Unity/Hub/Editor/*/Editor/Data/"
                      "PlaybackEngines/AndroidPlayer/SDK/build-tools/*/aapt.exe")
    if unity:
        return sorted(unity)[-1]
    return None


# --- Source checks (always run) ---
check("no main-manifest override (it replaces Unity's default)",
      not os.path.exists(MAIN_OVERRIDE), MAIN_OVERRIDE)

lib_ok = os.path.isfile(LIB_MANIFEST)
check("cleartext library manifest exists", lib_ok, LIB_MANIFEST)
if lib_ok:
    try:
        tree = ET.parse(LIB_MANIFEST)
        app = tree.getroot().find("application")
        flag = app is not None and app.get(ANDROID_NS + "usesCleartextTraffic") == "true"
        check("library manifest sets usesCleartextTraffic", flag)
        check("library manifest declares no activity (additive only)",
              app is not None and app.find("activity") is None)
    except ET.ParseError as e:
        check("library manifest is valid XML", False, e)

# --- APK checks (need a built APK + aapt) ---
apk = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_APK
aapt = find_aapt()
if not os.path.isfile(apk):
    skip("APK checks", "no APK at %s" % apk)
elif aapt is None:
    skip("APK checks", "aapt not found (set AAPT or ANDROID_SDK_ROOT)")
else:
    try:
        badging = subprocess.run([aapt, "dump", "badging", apk], capture_output=True,
                                 text=True, timeout=120)
        xmltree = subprocess.run([aapt, "dump", "xmltree", apk, "AndroidManifest.xml"],
                                 capture_output=True, text=True, timeout=120)
        ok = badging.returncode == 0 and xmltree.returncode == 0
        check("aapt reads the APK", ok)
        if ok:
            check("APK has launchable UnityPlayerGameActivity",
                  "launchable-activity" in badging.stdout
                  and "UnityPlayerGameActivity" in badging.stdout)
            check("merged manifest keeps cleartext flag",
                  "usesCleartextTraffic" in xmltree.stdout)
            for needle in ("android:theme", "android.app.lib_name", "unityplayer.UnityActivity"):
                check("merged activity keeps %s" % needle.split(":")[-1],
                      needle in xmltree.stdout)
    except (OSError, subprocess.SubprocessError) as e:
        check("aapt reads the APK", False, e)

print("\n%d passed, %d failed, %d skipped" % (passed, failed, skipped))
sys.exit(1 if failed else 0)

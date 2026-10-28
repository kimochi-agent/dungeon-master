using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Step 14: one-click Android APK build, from the Editor menu or command line:
// Unity -batchmode -quit -projectPath <path> -executeMethod BuildScript.BuildAndroid
public static class BuildScript
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Build/Android APK (Dungeon Master)")]
    public static void BuildAndroid()
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/DungeonMaster.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"Android build: {report.summary.result} in {report.summary.totalTime.TotalSeconds:F1}s, " +
                  $"size {report.summary.totalSize} bytes.");
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception("Android build failed. See Console for errors.");
    }
}

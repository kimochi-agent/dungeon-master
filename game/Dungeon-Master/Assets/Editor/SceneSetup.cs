using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Generates the Home and Dungeon scenes in code so no manual Editor setup is
// needed. Idempotent: creates missing scenes, repairs a missing HomeScreen or
// camera, and enforces build order Home (0), Dungeon (1). Runs automatically
// from BuildScript before every CLI build; also under Dungeon Master/Ensure
// Scenes in the Editor menu. Never touches the Dungeon scene once it exists
// (its content is code-built at runtime by GameBootstrap).
public static class SceneSetup
{
    public static string HomeScenePath => "Assets/Scenes/" + SceneNames.Home + ".unity";
    public static string DungeonScenePath => "Assets/Scenes/" + SceneNames.Dungeon + ".unity";

    [MenuItem("Dungeon Master/Ensure Scenes")]
    public static void EnsureScenes()
    {
        EnsureHomeScene();
        EnsureDungeonScene();
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(HomeScenePath, true),
            new EditorBuildSettingsScene(DungeonScenePath, true),
        };
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(HomeScenePath, OpenSceneMode.Single);
    }

    private static void EnsureHomeScene()
    {
        Scene scene;
        if (SceneAssetExists(HomeScenePath))
        {
            scene = EditorSceneManager.OpenScene(HomeScenePath, OpenSceneMode.Single);
            if (Object.FindFirstObjectByType<HomeScreen>() == null)
            {
                new GameObject("HomeScreen").AddComponent<HomeScreen>();
                EditorSceneManager.MarkSceneDirty(scene);
            }
            EnsureCamera(scene);
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("HomeScreen").AddComponent<HomeScreen>();
            EnsureCamera(scene);
            EditorSceneManager.SaveScene(scene, HomeScenePath);
        }
        if (scene.isDirty)
            EditorSceneManager.SaveScene(scene);
    }

    private static void EnsureDungeonScene()
    {
        if (SceneAssetExists(DungeonScenePath))
            return;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, DungeonScenePath);
    }

    private static void EnsureCamera(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<Camera>() != null)
                return;
        }
        GameObject cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        cameraGo.AddComponent<Camera>().backgroundColor = new Color(0.05f, 0.05f, 0.08f);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static bool SceneAssetExists(string path)
    {
        return AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null;
    }
}

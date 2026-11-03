using UnityEngine;
using UnityEngine.SceneManagement;

// Step 1: builds the dungeon, player, camera and light automatically when the
// Dungeon scene loads, so no manual scene setup is needed. Skips on the Home
// scene (it owns its own UI) and if a DungeonRoot already exists.
// First-person rework: head-mounted camera, own body hidden.
public static class GameBootstrap
{
    // NOTE: RuntimeInitializeOnLoadMethod fires only once, after the FIRST scene
    // load at startup — never on later LoadScene calls. Restarts/Home reload
    // the scene, so we also rebuild from sceneLoaded every time.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Build();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Build();
    }

    private static void Build()
    {
        if (SceneManager.GetActiveScene().name == SceneNames.Home)
            return; // Home scene: HomeScreen (authored in the scene) owns the UI.
        if (GameObject.Find("DungeonRoot") != null)
            return;

        GameObject root = new GameObject("DungeonRoot");
        DungeonGenerator dungeon = root.AddComponent<DungeonGenerator>();

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = dungeon.SpawnPoint;
        Object.Destroy(player.GetComponent<CapsuleCollider>());
        player.AddComponent<CharacterController>();
        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerHealth>(); // Step 4 (additive).
        player.GetComponent<Renderer>().enabled = true; // First-person: hide own body.

        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            camera = cameraGo.AddComponent<Camera>();
        }
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black; // Pure void: unlit far objects blend invisibly.
        camera.transform.SetParent(player.transform, false);
        camera.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        camera.transform.localRotation = Quaternion.identity;
        player.AddComponent<FirstPersonLook>();

        // Hand torch (additive): the player's own light radius.
        player.AddComponent<PlayerTorch>();

        // Step 2 (additive): scatter collectibles through the dungeon.
        GameObject loot = new GameObject("Collectibles");
        loot.AddComponent<CollectiblePlacer>();

        // Step 4 (additive): health bar and counters.
        GameObject hud = new GameObject("HUD");
        hud.AddComponent<HUD>();

        // Look (additive): invisible full-screen drag look (touch only).
        // TouchJoystick circular stick is kept in the repo but unused.
        GameObject look = new GameObject("LookDrag");
        look.AddComponent<LookDrag>();

        // Left stick (additive): analog move stick.
        // TouchButtons triangle pad is kept in the repo but unused (touch issues).
        GameObject moveStick = new GameObject("MoveJoystick");
        moveStick.AddComponent<MoveJoystick>();

        // Attack (additive): circular attack button, bottom-right.
        GameObject atk = new GameObject("AttackButton");
        atk.AddComponent<AttackButton>();

        // Step 6 (additive): menu / pause / game-over shell.
        GameObject shell = new GameObject("GameShell");
        shell.AddComponent<GameShell>();

        // Step 8 (additive): spawn monsters.
        GameObject spawner = new GameObject("MonsterSpawner");
        spawner.AddComponent<MonsterSpawner>();

        // Step 9 (additive): melee attack.
        player.AddComponent<PlayerAttack>();

        // Atmosphere (additive): fog, torchlight and mood. No sun: the dungeon
        // is indoors, lit only by hall lights, torches and the player's torch.
        GameObject atmosphere = new GameObject("DungeonAtmosphere");
        atmosphere.AddComponent<DungeonAtmosphere>();
    }
}

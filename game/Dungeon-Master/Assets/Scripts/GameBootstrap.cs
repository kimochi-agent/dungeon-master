using UnityEngine;

// Step 1: builds the dungeon, player, camera and light automatically when play
// starts, so no manual scene setup is needed. Skips if a DungeonRoot exists.
// First-person rework: head-mounted camera, own body hidden.
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Start()
    {
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
        player.GetComponent<Renderer>().enabled = false; // First-person: hide own body.

        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            camera = cameraGo.AddComponent<Camera>();
        }
        camera.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
        camera.transform.SetParent(player.transform, false);
        camera.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        camera.transform.localRotation = Quaternion.identity;
        player.AddComponent<FirstPersonLook>();

        // Step 2 (additive): scatter collectibles through the dungeon.
        GameObject loot = new GameObject("Collectibles");
        loot.AddComponent<CollectiblePlacer>();

        // Step 4 (additive): health bar and counters.
        GameObject hud = new GameObject("HUD");
        hud.AddComponent<HUD>();

        // Step 5 (additive): touch joystick (hidden unless a touchscreen exists).
        GameObject stick = new GameObject("TouchJoystick");
        stick.AddComponent<TouchJoystick>();

        // Step 6 (additive): menu / pause / game-over shell.
        GameObject shell = new GameObject("GameShell");
        shell.AddComponent<GameShell>();

        // Step 8 (additive): spawn monsters.
        GameObject spawner = new GameObject("MonsterSpawner");
        spawner.AddComponent<MonsterSpawner>();

        // Step 9 (additive): melee attack.
        player.AddComponent<PlayerAttack>();

        if (GameObject.Find("Sun") == null)
        {
            GameObject sun = new GameObject("Sun");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.6f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
}

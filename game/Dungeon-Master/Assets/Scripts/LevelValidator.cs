using System.Collections.Generic;
using UnityEngine;

// Strict client-side validation for downloaded level configs.
// Mirrors level_generator/validate.py (same constants and rules) — never
// trust the network. Bundled configs keep the lenient FillDefaults path;
// downloads must pass here before GameConfig loads them.
public static class LevelValidator
{
    private const float EPS = 0.6f;
    private const float SPOT_WALL_MARGIN = 0.5f;
    private const float PILLAR_CLEARANCE = 1.0f;
    private const float DOOR_CLEARANCE = 1.5f;
    private const float SPAWN_MONSTER_MIN = 3.0f;
    private const float ROOM_MIN_SIZE = 4f;
    private const float CORRIDOR_MIN_WIDTH = 2f;
    private const float CORRIDOR_MAX_WIDTH = 6f;
    private const float DOOR_CORRIDOR_MIN_WIDTH = 3.5f;
    private const float DOOR_CORRIDOR_MAX_WIDTH = 4.5f;

    private struct Rect
    {
        public float x0, x1, z0, z1;
    }

    private struct Corridor
    {
        public bool horiz;
        public Rect rect;
        public float width, cx, cz;
        public int fromRoom, toRoom;
    }

    public static bool TryParse(string json, out LevelData data, out string error)
    {
        data = null;
        error = null;
        if (string.IsNullOrEmpty(json))
        {
            error = "empty level JSON";
            return false;
        }
        try
        {
            data = JsonUtility.FromJson<LevelData>(json);
        }
        catch (System.Exception e)
        {
            error = "JSON parse error: " + e.Message;
            return false;
        }
        if (data == null)
        {
            error = "JSON parse error";
            return false;
        }
        return Validate(data, out error);
    }

    public static bool Validate(LevelData cfg, out string error)
    {
        error = null;
        if (cfg == null)
            return Fail("config is null", out error);

        if (cfg.playerMaxHealth < 50 || cfg.playerMaxHealth > 200)
            return Fail("playerMaxHealth out of range [50, 200]", out error);
        if (cfg.playerMoveSpeed < 3f || cfg.playerMoveSpeed > 8f)
            return Fail("playerMoveSpeed out of range [3, 8]", out error);
        if (cfg.coinCount < 0 || cfg.coinCount > 30)
            return Fail("coinCount out of range [0, 30]", out error);
        if (cfg.powerUpCount < 0 || cfg.powerUpCount > 8)
            return Fail("powerUpCount out of range [0, 8]", out error);
        if (cfg.chestCount < 0 || cfg.chestCount > 6)
            return Fail("chestCount out of range [0, 6]", out error);
        if (cfg.lockedChestCount < 0 || cfg.lockedChestCount > 6)
            return Fail("lockedChestCount out of range [0, 6]", out error);
        if (cfg.monsterCount < 0 || cfg.monsterCount > 12)
            return Fail("monsterCount out of range [0, 12]", out error);
        if (cfg.monsterDamage < 1 || cfg.monsterDamage > 40)
            return Fail("monsterDamage out of range [1, 40]", out error);
        if (cfg.monsterHealth < 10 || cfg.monsterHealth > 200)
            return Fail("monsterHealth out of range [10, 200]", out error);
        if (cfg.monsterSpeed < 1f || cfg.monsterSpeed > 7f)
            return Fail("monsterSpeed out of range [1, 7]", out error);
        if (cfg.puzzleTimeLimit < 5f || cfg.puzzleTimeLimit > 30f)
            return Fail("puzzleTimeLimit out of range [5, 30]", out error);

        List<Rect> rooms = new List<Rect>();
        if (cfg.rooms == null || cfg.rooms.Length < 1 || cfg.rooms.Length > 8)
            return Fail("rooms must have 1..8 entries", out error);
        for (int i = 0; i < cfg.rooms.Length; i++)
        {
            float cx = cfg.rooms[i].center.x;
            float cz = cfg.rooms[i].center.y;
            float sx = cfg.rooms[i].size.x;
            float sz = cfg.rooms[i].size.y;
            if (sx < ROOM_MIN_SIZE || sz < ROOM_MIN_SIZE)
                return Fail($"rooms[{i}] below minimum size", out error);
            Rect r;
            r.x0 = cx - sx / 2f;
            r.x1 = cx + sx / 2f;
            r.z0 = cz - sz / 2f;
            r.z1 = cz + sz / 2f;
            rooms.Add(r);
        }
        for (int i = 0; i < rooms.Count; i++)
            for (int j = i + 1; j < rooms.Count; j++)
                if (Overlap1D(rooms[i].x0, rooms[i].x1, rooms[j].x0, rooms[j].x1) > EPS
                    && Overlap1D(rooms[i].z0, rooms[i].z1, rooms[j].z0, rooms[j].z1) > EPS)
                    return Fail($"rooms[{i}] and rooms[{j}] overlap", out error);

        List<Corridor> corridors = new List<Corridor>();
        if (cfg.corridors == null || cfg.corridors.Length > 10)
            return Fail("corridors must have 0..10 entries", out error);
        for (int i = 0; i < cfg.corridors.Length; i++)
        {
            float ax = cfg.corridors[i].from.x;
            float az = cfg.corridors[i].from.y;
            float bx = cfg.corridors[i].to.x;
            float bz = cfg.corridors[i].to.y;
            float w = cfg.corridors[i].width;
            if (w < CORRIDOR_MIN_WIDTH || w > CORRIDOR_MAX_WIDTH)
                return Fail($"corridors[{i}] width out of range", out error);
            int fromRoom = RoomAt(rooms, ax, az);
            int toRoom = RoomAt(rooms, bx, bz);
            if (fromRoom < 0)
                return Fail($"corridors[{i}] end from terminates outside any room", out error);
            if (toRoom < 0)
                return Fail($"corridors[{i}] end to terminates outside any room", out error);
            float dx = Mathf.Abs(bx - ax);
            float dz = Mathf.Abs(bz - az);
            if (dx < 0.01f && dz < 0.01f)
                return Fail($"corridors[{i}] has zero length", out error);
            if (dx >= 0.01f && dz >= 0.01f)
                return Fail($"corridors[{i}] is not axis-aligned (diagonal)", out error);
            bool horiz = dz < 0.01f;
            Rect rect;
            float length;
            if (horiz)
            {
                rect.x0 = Mathf.Min(ax, bx);
                rect.x1 = Mathf.Max(ax, bx);
                rect.z0 = az - w / 2f;
                rect.z1 = az + w / 2f;
                length = rect.x1 - rect.x0;
            }
            else
            {
                rect.x0 = ax - w / 2f;
                rect.x1 = ax + w / 2f;
                rect.z0 = Mathf.Min(az, bz);
                rect.z1 = Mathf.Max(az, bz);
                length = rect.z1 - rect.z0;
            }
            if (length < 1f)
                return Fail($"corridors[{i}] length below minimum 1", out error);
            for (int j = 0; j < rooms.Count; j++)
            {
                float ox = Overlap1D(rect.x0, rect.x1, rooms[j].x0, rooms[j].x1);
                float oz = Overlap1D(rect.z0, rect.z1, rooms[j].z0, rooms[j].z1);
                bool bad = horiz ? (oz > 0f && ox > EPS) : (ox > 0f && oz > EPS);
                if (bad)
                    return Fail($"corridors[{i}] overlaps rooms[{j}]", out error);
            }
            if (fromRoom == toRoom)
                return Fail($"corridors[{i}] has both ends in one room", out error);
            Corridor c;
            c.horiz = horiz;
            c.rect = rect;
            c.width = w;
            c.cx = (ax + bx) / 2f;
            c.cz = (az + bz) / 2f;
            c.fromRoom = fromRoom;
            c.toRoom = toRoom;
            corridors.Add(c);
        }

        List<Vector2> doorXZ = new List<Vector2>();
        if (cfg.doors == null || cfg.doors.Length > 6)
            return Fail("doors must have 0..6 entries", out error);
        for (int i = 0; i < cfg.doors.Length; i++)
        {
            Vector3 p = cfg.doors[i].position;
            bool alongX = cfg.doors[i].alongX;
            if (p.y < 0.5f || p.y > 2.5f)
                return Fail($"doors[{i}] y outside [0.5, 2.5]", out error);
            float plane = alongX ? p.x : p.z;
            float at = alongX ? p.z : p.x;
            bool onPlane = false;
            bool cornerOk = false;
            for (int j = 0; j < rooms.Count; j++)
            {
                float s0 = alongX ? rooms[j].x0 : rooms[j].z0;
                float s1 = alongX ? rooms[j].x1 : rooms[j].z1;
                float t0 = alongX ? rooms[j].z0 : rooms[j].x0;
                float t1 = alongX ? rooms[j].z1 : rooms[j].x1;
                if (Mathf.Min(Mathf.Abs(plane - s0), Mathf.Abs(plane - s1)) <= EPS)
                {
                    onPlane = true;
                    if (t0 + 1f <= at && at <= t1 - 1f)
                        cornerOk = true;
                }
            }
            if (!onPlane)
                return Fail($"doors[{i}] is not on any room side plane", out error);
            if (!cornerOk)
                return Fail($"doors[{i}] too close to a room corner", out error);
            bool mouth = false;
            float mouthWidth = 0f;
            for (int j = 0; j < corridors.Count; j++)
            {
                Corridor c = corridors[j];
                if (c.horiz != alongX)
                    continue;
                bool inLane;
                bool inSpan;
                if (alongX)
                {
                    inLane = Mathf.Abs(at - c.cz) <= c.width / 2f + EPS;
                    inSpan = c.rect.x0 - EPS <= plane && plane <= c.rect.x1 + EPS;
                }
                else
                {
                    inLane = Mathf.Abs(at - c.cx) <= c.width / 2f + EPS;
                    inSpan = c.rect.z0 - EPS <= plane && plane <= c.rect.z1 + EPS;
                }
                if (inLane && inSpan)
                {
                    mouth = true;
                    mouthWidth = c.width;
                    break;
                }
            }
            if (!mouth)
                return Fail($"doors[{i}] has no matching corridor mouth", out error);
            if (mouthWidth < DOOR_CORRIDOR_MIN_WIDTH || mouthWidth > DOOR_CORRIDOR_MAX_WIDTH)
                return Fail($"doors[{i}] corridor width must be ~4", out error);
            doorXZ.Add(new Vector2(p.x, p.z));
        }

        List<Vector2> pillars = new List<Vector2>();
        if (cfg.pillars == null)
            return Fail("pillars must be a list", out error);
        for (int i = 0; i < cfg.pillars.Length; i++)
        {
            Vector3 p = cfg.pillars[i];
            if (!InsideAny(rooms, p.x, p.z, 1f))
                return Fail($"pillars[{i}] is outside every room", out error);
            pillars.Add(new Vector2(p.x, p.z));
        }

        if (!InsideAny(rooms, cfg.playerSpawn.x, cfg.playerSpawn.z, SPOT_WALL_MARGIN))
            return Fail("playerSpawn is outside every room", out error);
        if (!InsideAny(rooms, cfg.exitPoint.x, cfg.exitPoint.z, SPOT_WALL_MARGIN))
            return Fail("exitPoint is outside every room", out error);
        for (int i = 0; i < pillars.Count; i++)
        {
            if (Dist2(cfg.playerSpawn.x, cfg.playerSpawn.z, pillars[i].x, pillars[i].y) < PILLAR_CLEARANCE)
                return Fail("playerSpawn too close to a pillar", out error);
            if (Dist2(cfg.exitPoint.x, cfg.exitPoint.z, pillars[i].x, pillars[i].y) < PILLAR_CLEARANCE)
                return Fail("exitPoint too close to a pillar", out error);
        }

        if (!CheckSpots("coinSpots", cfg.coinSpots, rooms, corridors, pillars, doorXZ,
                false, Vector3.zero, out error))
            return false;
        if (!CheckSpots("powerUpSpots", cfg.powerUpSpots, rooms, corridors, pillars, doorXZ,
                false, Vector3.zero, out error))
            return false;
        if (!CheckSpots("chestSpots", cfg.chestSpots, rooms, corridors, pillars, doorXZ,
                false, Vector3.zero, out error))
            return false;
        if (!CheckSpots("lockedChestSpots", cfg.lockedChestSpots, rooms, corridors, pillars, doorXZ,
                false, Vector3.zero, out error))
            return false;
        if (!CheckSpots("monsterSpots", cfg.monsterSpots, rooms, corridors, pillars, doorXZ,
                true, cfg.playerSpawn, out error))
            return false;

        if (cfg.coinSpots.Length < cfg.coinCount)
            return Fail("coinCount exceeds coinSpots", out error);
        if (cfg.powerUpSpots.Length < cfg.powerUpCount)
            return Fail("powerUpCount exceeds powerUpSpots", out error);
        if (cfg.chestSpots.Length < cfg.chestCount)
            return Fail("chestCount exceeds chestSpots", out error);
        if (cfg.lockedChestSpots.Length < cfg.lockedChestCount)
            return Fail("lockedChestCount exceeds lockedChestSpots", out error);
        if (cfg.monsterSpots.Length < cfg.monsterCount)
            return Fail("monsterCount exceeds monsterSpots", out error);

        int spawnRoom = RoomAt(rooms, cfg.playerSpawn.x, cfg.playerSpawn.z);
        int exitRoom = RoomAt(rooms, cfg.exitPoint.x, cfg.exitPoint.z);
        if (spawnRoom < 0 || exitRoom < 0)
            return Fail("spawn or exit is outside every room", out error);
        List<int>[] adj = new List<int>[rooms.Count];
        for (int i = 0; i < adj.Length; i++)
            adj[i] = new List<int>();
        foreach (Corridor c in corridors)
        {
            if (c.fromRoom >= 0 && c.toRoom >= 0 && c.fromRoom != c.toRoom)
            {
                adj[c.fromRoom].Add(c.toRoom);
                adj[c.toRoom].Add(c.fromRoom);
            }
        }
        bool[] seen = new bool[rooms.Count];
        Queue<int> queue = new Queue<int>();
        seen[spawnRoom] = true;
        queue.Enqueue(spawnRoom);
        while (queue.Count > 0)
        {
            int cur = queue.Dequeue();
            foreach (int nxt in adj[cur])
            {
                if (!seen[nxt])
                {
                    seen[nxt] = true;
                    queue.Enqueue(nxt);
                }
            }
        }
        if (!seen[exitRoom])
            return Fail("exit is not reachable from player spawn", out error);
        for (int i = 0; i < rooms.Count; i++)
            if (!seen[i])
                return Fail($"rooms[{i}] is unreachable from spawn", out error);
        return true;
    }

    private static bool CheckSpots(string name, Vector3[] spots, List<Rect> rooms,
        List<Corridor> corridors, List<Vector2> pillars, List<Vector2> doors,
        bool checkSpawn, Vector3 spawn, out string error)
    {
        error = null;
        if (spots == null)
            return Fail(name + " must be a list", out error);
        for (int i = 0; i < spots.Length; i++)
        {
            Vector3 p = spots[i];
            if (p.y < 0f || p.y > 2f)
                return Fail($"{name}[{i}] y outside [0, 2]", out error);
            if (!InsideAny(rooms, p.x, p.z, SPOT_WALL_MARGIN) && !InsideCorridors(corridors, p.x, p.z))
                return Fail($"{name}[{i}] is outside every room and corridor", out error);
            for (int j = 0; j < pillars.Count; j++)
                if (Dist2(p.x, p.z, pillars[j].x, pillars[j].y) < PILLAR_CLEARANCE)
                    return Fail($"{name}[{i}] too close to a pillar", out error);
            for (int j = 0; j < doors.Count; j++)
                if (Dist2(p.x, p.z, doors[j].x, doors[j].y) < DOOR_CLEARANCE)
                    return Fail($"{name}[{i}] too close to a door", out error);
            if (checkSpawn && Dist2(p.x, p.z, spawn.x, spawn.z) < SPAWN_MONSTER_MIN)
                return Fail($"{name}[{i}] too close to player spawn", out error);
        }
        return true;
    }

    private static bool Fail(string message, out string error)
    {
        error = message;
        return false;
    }

    private static bool Inside(float px, float pz, Rect r, float margin)
    {
        return r.x0 + margin <= px && px <= r.x1 - margin
            && r.z0 + margin <= pz && pz <= r.z1 - margin;
    }

    private static bool InsideAny(List<Rect> rooms, float px, float pz, float margin)
    {
        for (int i = 0; i < rooms.Count; i++)
            if (Inside(px, pz, rooms[i], margin))
                return true;
        return false;
    }

    private static bool InsideCorridors(List<Corridor> corridors, float px, float pz)
    {
        for (int i = 0; i < corridors.Count; i++)
            if (Inside(px, pz, corridors[i].rect, SPOT_WALL_MARGIN))
                return true;
        return false;
    }

    private static int RoomAt(List<Rect> rooms, float px, float pz)
    {
        for (int i = 0; i < rooms.Count; i++)
            if (Inside(px, pz, rooms[i], -EPS))
                return i;
        return -1;
    }

    private static float Overlap1D(float a0, float a1, float b0, float b1)
    {
        return Mathf.Max(0f, Mathf.Min(a1, b1) - Mathf.Max(a0, b0));
    }

    private static float Dist2(float ax, float az, float bx, float bz)
    {
        float dx = ax - bx;
        float dz = az - bz;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}

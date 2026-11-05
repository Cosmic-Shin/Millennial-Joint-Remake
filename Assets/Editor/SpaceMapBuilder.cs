using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds the six space maps as layouts inside SampleScene (next to Classic / Divided / Floating)
// and registers them with MapSelector. Station scripts are cloned from "Map - Classic";
// their old meshes are hidden and the matching Furniture_Space piece is added as the visual.
//
// All coordinates are in the map root's space AFTER the level model is scaled up:
// customers are at +z, the kitchen is at -z, floors are at y = 0.
// Each map root is turned 180 degrees so customers sit at the bottom of the screen, like Classic.
// Yaw = direction a station's front (the side players use) faces; 0 = +z (towards the camera).
public static class SpaceMapBuilder
{
    private const string Models = "Assets/Assets/NEW 3D MODELS/";
    private static readonly Vector3 RootPosition = new Vector3(0f, 0f, -4f);
    private static readonly Quaternion RootRotation = Quaternion.Euler(0f, 180f, 0f);

    private enum S { Fridge, Grill, Fryer, Plates, Cups, Soda, Coffee, Counter, Serving, Chili, Bin, IceCream }

    private static readonly Dictionary<S, string> Template = new Dictionary<S, string>
    {
        { S.Fridge, "fridge" }, { S.Grill, "stove" }, { S.Fryer, "fryer" }, { S.Plates, "Plates (1)" },
        { S.Cups, "Cups" }, { S.Soda, "soda_machine" }, { S.Coffee, "coffee_machine" }, { S.Counter, "CounterTop 4" },
        { S.Serving, "Serving" }, { S.Chili, "chilidog_pot" }, { S.Bin, "Dustabin" }, { S.IceCream, "IceCreamMachine" },
    };

    // Furniture_Space pieces used as each station's visual (first = base, rest stacked on top).
    private static readonly Dictionary<S, string[]> Pieces = new Dictionary<S, string[]>
    {
        { S.Fridge, new[] { "fridge" } }, { S.Grill, new[] { "grill_mt" } }, { S.Fryer, new[] { "fryer_mt" } },
        { S.Plates, new[] { "base_top" } }, { S.Cups, new[] { "base_top", "glasses" } }, // plates: the original plate stack goes on top (see MakeStation)
        { S.Soda, new[] { "SodaMachine" } }, { S.Coffee, new[] { "CoffeeMachine" } }, { S.Counter, new[] { "base_top" } },
        { S.Serving, new[] { "OrderUp_Teleporter" } }, { S.Chili, new[] { "ChiliPot" } }, { S.Bin, new[] { "dustbin" } },
        { S.IceCream, new string[0] }, // no space version: keeps the old model
    };

    private struct Place
    {
        public S kind; public float x, z, yaw;
        public Place(S kind, float x, float z, float yaw) { this.kind = kind; this.x = x; this.z = z; this.yaw = yaw; }
    }

    private class Level
    {
        public string name;
        public float scale = 1f;           // floor size multiplier (x/z only; stations keep their size)
        public bool meshFloor;             // irregular floors (ring, asteroid) use mesh colliders
        public bool movingIslands;         // SplitIslands: the two islands slide together and apart
        public List<Place> places = new List<Place>();
        public Vector2 p1, p2;
        public Rect[] powerUpAreas;
        public PadSpec[] pads;             // gravity-flip pads (purple tiles)
        public float hopTiles = 5.25f;     // how far hop pads throw players, in floor tiles
        public Vector3 sign;               // x, bottom y, z
        public Vector3 whiteboard;         // x, z, yaw
        public Vector3 mobile;             // x, bottom y, z
    }

    private struct PadSpec
    {
        public float x, z;
        public bool hasTarget; public float tx, tz; // ZeroGHop: optional fixed landing
        public int pair; public bool sender;        // ItemLauncher: pair number (-1 = hop pad)
    }

    private static PadSpec Hop(float x, float z) => new PadSpec { x = x, z = z, pair = -1 };
    private static PadSpec HopTo(float x, float z, float tx, float tz) => new PadSpec { x = x, z = z, hasTarget = true, tx = tx, tz = tz, pair = -1 };
    private static PadSpec Send(int pair, float x, float z) => new PadSpec { x = x, z = z, pair = pair, sender = true };
    private static PadSpec Receive(int pair, float x, float z) => new PadSpec { x = x, z = z, pair = pair };

    private static Place P(S kind, float x, float z, float yaw) => new Place(kind, x, z, yaw);
    private static Rect R(float x, float z, float w, float h) => new Rect(x - w / 2f, z - h / 2f, w, h);

    // Symmetric kitchen: back row of cooking stations around a centre fridge, drink/side stations down both
    // walls, a small centre island, and the serving counter at the front facing the customers.
    private static void Kitchen(Level l, float cx, float backZ, float sideD, float[] sideZ, float[] islandZ, float frontZ, float binD)
    {
        l.places.Add(P(S.Fridge, cx, backZ, 0));
        S[] back = { S.Grill, S.Grill, S.Fryer, S.Fryer, S.Chili };
        S[] side = { S.Soda, S.Coffee, S.Cups, S.IceCream, S.Counter };
        foreach (int s in new[] { 1, -1 })
        {
            for (int i = 0; i < back.Length; i++)
                l.places.Add(P(back[i], cx + s * (2.05f + 1.7f * i), backZ, 0));
            for (int i = 0; i < side.Length && i < sideZ.Length; i++)
                l.places.Add(P(side[i], cx + s * sideD, sideZ[i], -90 * s));
            l.places.Add(P(S.Plates, cx + s * 0.85f, islandZ[0], 90 * s));
            l.places.Add(P(S.Counter, cx + s * 0.85f, islandZ[1], 90 * s));
            l.places.Add(P(S.Counter, cx + s * 2.6f, frontZ, 180));
            l.places.Add(P(S.Bin, cx + s * binD, frontZ, 180));
        }
        l.places.Add(P(S.Serving, cx, frontZ, 180));
    }

    // Two-island kitchen: cooking on the +x island (screen left, P1), plating / drinks / serving on the -x island.
    private static void IslandKitchen(Level l, float outer, float back)
    {
        float[] z = { 0.8f, -1.25f, -2.95f, -4.65f, -6.35f, -8.05f, -9.75f };
        S[] cook = { S.Fridge, S.Grill, S.Grill, S.Fryer, S.Fryer, S.Chili, S.Counter };
        S[] serve = { S.Plates, S.Cups, S.Soda, S.Coffee, S.IceCream, S.Counter, S.Counter };
        for (int i = 0; i < cook.Length; i++)
        {
            l.places.Add(P(cook[i], outer, cook[i] == S.Fridge ? 0.8f : z[i] - 0.4f, -90));
            l.places.Add(P(serve[i], -outer, z[i] + 0.4f, 90));
        }
        l.places.Add(P(S.Bin, outer - 1.8f, back, 0));
        l.places.Add(P(S.Counter, outer - 3.5f, back, 0));
        l.places.Add(P(S.Bin, -outer + 1.8f, back, 0));
        l.places.Add(P(S.Counter, -outer + 3.5f, back, 0));
        l.places.Add(P(S.Serving, -outer + 3.8f, 1.4f, 180));
    }

    private static Level[] Levels()
    {
        var main = new Level
        {
            name = "Main", scale = 1.3f,
            p1 = new Vector2(5.8f, -2f), p2 = new Vector2(-3.2f, -2f),
            powerUpAreas = new[] { R(6.8f, -1.5f, 3f, 3f), R(-4.2f, -1.5f, 3f, 3f) },
            // Shortcuts over the centre island, one each way.
            pads = new[] { HopTo(4.6f, -7.8f, -2.0f, -7.8f), HopTo(-2.0f, -4.6f, 4.6f, -4.6f) },
            sign = new Vector3(1.3f, 2.2f, -14.8f), whiteboard = new Vector3(-5.2f, -14.6f, 0f), mobile = new Vector3(7.5f, 3.4f, -12.5f),
        };
        Kitchen(main, 1.3f, -12.85f, 9.6f, new[] { -11.0f, -9.3f, -7.6f, -5.9f, -4.2f }, new[] { -8.2f, -6.5f }, 2.3f, 5.5f);

        var asteroid = new Level
        {
            name = "AsteroidDiner", scale = 2f, meshFloor = true, hopTiles = 3.25f, // big tiles: a shorter throw keeps players in front of the back stoves
            p1 = new Vector2(4.5f, -6f), p2 = new Vector2(-4.5f, -6f),
            powerUpAreas = new[] { R(5f, -4f, 3f, 3f), R(-5f, -4f, 3f, 3f) },
            pads = new[] { HopTo(4.0f, -9.2f, -4.0f, -9.2f), HopTo(-4.0f, -4.2f, 4.0f, -4.2f) },
            sign = new Vector3(0f, 2.2f, -16.4f), whiteboard = new Vector3(-6.5f, -16.2f, 0f), mobile = new Vector3(5.5f, 3.4f, -13.5f),
        };
        Kitchen(asteroid, 0f, -14.2f, 9.2f, new[] { -11.6f, -9.9f, -8.2f, -6.5f, -4.8f }, new[] { -10f, -8.3f }, -1.8f, 5.5f);

        var split = new Level
        {
            name = "SplitIslands", scale = 1.35f, movingIslands = true,
            p1 = new Vector2(7.4f, -5f), p2 = new Vector2(-7.4f, -5f),
            powerUpAreas = new[] { R(7.4f, -8f, 3f, 3f), R(-7.4f, -8f, 3f, 3f) },
            // Item launchers: pair 0 sends +X island -> -X island, pair 1 the other way.
            pads = new[] { Send(0, 5f, -2.5f), Receive(0, -5f, -2.5f), Send(1, -5f, -7f), Receive(1, 5f, -7f) },
            sign = new Vector3(0f, 2.2f, -14.6f), whiteboard = new Vector3(5f, -13.5f, 0f), mobile = new Vector3(-8f, 3.4f, -10.5f),
        };
        IslandKitchen(split, 11.35f, -12f);

        var bridge = new Level
        {
            name = "BridgeIslands", scale = 1.35f, hopTiles = 7f,
            p1 = new Vector2(8.8f, -2.5f), p2 = new Vector2(-8.8f, -2.5f),
            powerUpAreas = new[] { R(8.8f, -9f, 3f, 3f), R(-8.8f, -9f, 3f, 3f) },
            pads = new[] { Hop(7.5f, -0.8f), Hop(-6.5f, -1f) },
            sign = new Vector3(0f, 2.2f, -14.6f), whiteboard = new Vector3(6f, -13.5f, 0f), mobile = new Vector3(-9f, 3.4f, -10.5f),
        };
        IslandKitchen(bridge, 12.7f, -12f);

        var shuttle = new Level
        {
            name = "ShuttleHop", scale = 1.35f,
            p1 = new Vector2(8.8f, -5f), p2 = new Vector2(-8.8f, -5f),
            powerUpAreas = new[] { R(9f, -8f, 3f, 3f), R(-9f, -8f, 3f, 3f) },
            pads = new[] { Hop(8f, -2f), Hop(-7f, -2.2f) },
            sign = new Vector3(0f, 2.2f, -14.6f), whiteboard = new Vector3(7f, -13.5f, 0f), mobile = new Vector3(-9f, 3.4f, -11f),
        };
        IslandKitchen(shuttle, 12.7f, -12f);

        var ring = new Level
        {
            name = "RingStation", scale = 1.3f, meshFloor = true,
            p1 = new Vector2(7f, -5.2f), p2 = new Vector2(-7f, -5.2f),
            powerUpAreas = new[] { R(5f, -10.2f, 2f, 2f), R(-5f, -10.2f, 2f, 2f) },
            pads = new[] { Hop(5.1f, -0.1f), Hop(-5.1f, -0.1f) },
            sign = new Vector3(0f, 2.2f, -17.6f), mobile = new Vector3(0f, 4f, -5.2f),
        };
        void Around(S kind, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad, r = 10.2f;
            ring.places.Add(P(kind, Mathf.Cos(a) * r, -5.2f + Mathf.Sin(a) * r, Mathf.Atan2(-Mathf.Cos(a), -Mathf.Sin(a)) * Mathf.Rad2Deg));
        }
        Around(S.Serving, 90);
        Around(S.Fridge, 62); Around(S.Grill, 40); Around(S.Grill, 18); Around(S.Fryer, -4); Around(S.Fryer, -26); Around(S.Chili, -48); Around(S.Counter, -70);
        Around(S.Plates, 118); Around(S.Cups, 140); Around(S.Soda, 162); Around(S.Coffee, 184); Around(S.IceCream, 206); Around(S.Counter, 228);
        Around(S.Bin, -92);
        float wa = -114f * Mathf.Deg2Rad;
        ring.whiteboard = new Vector3(Mathf.Cos(wa) * 10.4f, -5.2f + Mathf.Sin(wa) * 10.4f, Mathf.Atan2(-Mathf.Cos(wa), -Mathf.Sin(wa)) * Mathf.Rad2Deg);

        return new[] { main, split, bridge, ring, shuttle, asteroid };
    }

    [MenuItem("Tools/Millennial Joint/Build Space Maps")]
    public static void BuildAll()
    {
        if (!EditorUtility.DisplayDialog("Build Space Maps",
                "Rebuilds the six 'Map - <Space>' layouts in the open scene. Any manual edits to those layouts will be lost.", "Build", "Cancel"))
            return;
        Rebuild();
    }

    public static void Rebuild()
    {
        Transform classic = StationTemplates();
        MapSelector selector = Object.FindFirstObjectByType<MapSelector>(FindObjectsInactive.Include);
        if (classic == null || selector == null)
        {
            Debug.LogError("SpaceMapBuilder: needs 'Station Templates' (or 'Map - Classic') and a MapSelector in the open scene.");
            return;
        }

        var layouts = selector.layouts.Where(l => l.root != null && !l.root.name.StartsWith("Map - Space")).ToList();
        foreach (Level level in Levels())
            layouts.Add(Build(level, classic));
        selector.layouts = layouts.ToArray();

        Transform bridge = SceneRoot("Customer Bridge");
        selector.customerQueue = Object.FindFirstObjectByType<CustomerQueue>(FindObjectsInactive.Include);
        selector.customerBridgeVisuals = bridge != null
            ? bridge.Cast<Transform>().Where(t => t.GetComponent<CustomerQueue>() == null).Select(t => t.gameObject).ToArray()
            : new GameObject[0];

        EditorUtility.SetDirty(selector);
        EditorSceneManager.MarkSceneDirty(selector.gameObject.scene);
        Debug.Log("SpaceMapBuilder: built " + Levels().Length + " space maps.");
    }

    // One inactive copy of each station (scripts, UI panels, anchors) to clone from.
    // Made once from the old Classic map, so the old maps can be deleted.
    private static Transform StationTemplates()
    {
        Transform templates = SceneRoot("Station Templates");
        if (templates != null)
            return templates;

        Transform classic = SceneRoot("Map - Classic");
        if (classic == null)
            return null;

        templates = new GameObject("Station Templates").transform;
        templates.gameObject.SetActive(false);
        foreach (string name in Template.Values.Distinct())
        {
            GameObject copy = Object.Instantiate(classic.Find(name).gameObject, templates);
            copy.name = name;
        }

        ServingCounter serving = templates.Find(Template[S.Serving]).GetComponent<ServingCounter>();
        if (serving.serveBurst != null)
        {
            ParticleSystem burst = Object.Instantiate(serving.serveBurst, templates);
            burst.name = "Serve Burst";
            serving.serveBurst = burst;
        }
        return templates;
    }

    private static Transform SceneRoot(string name)
    {
        GameObject go = EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == name);
        return go != null ? go.transform : null;
    }

    private static GameObject Spawn(string model, Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Models + model + ".fbx");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        return go;
    }

    private static MapSelector.Layout Build(Level level, Transform classic)
    {
        string rootName = "Map - Space " + level.name;
        Transform old = SceneRoot(rootName);
        if (old != null)
            Object.DestroyImmediate(old.gameObject);

        var root = new GameObject(rootName).transform;
        root.SetPositionAndRotation(RootPosition, RootRotation);

        // Floors get bigger; heights (rails, ropes, decals) stay the same.
        var scaler = new GameObject("Level (scaled)").transform;
        scaler.SetParent(root, false);
        scaler.localScale = new Vector3(level.scale, 1f, level.scale);
        GameObject levelModel = Spawn("Levels/Level_" + level.name, scaler);
        AddFloors(levelModel.transform, root, level.meshFloor);

        GameObject furniture = Spawn("Kitchen/Furniture_Space", root);
        Transform stations = new GameObject("Stations").transform;
        stations.SetParent(root, false);
        var built = new List<GameObject>();
        foreach (Place p in level.places)
            built.Add(MakeStation(p.kind, stations, new Vector3(p.x, 0f, p.z), p.yaw, furniture.transform, classic));
        Object.DestroyImmediate(furniture);

        if (level.name == "ShuttleHop")
            AddShuttles(levelModel.transform, root, level.scale);

        var layout = new MapSelector.Layout
        {
            name = level.name,
            root = root.gameObject,
            player1Spawn = Marker("Spawn P1", root, level.p1),
            player2Spawn = Marker("Spawn P2", root, level.p2),
            powerUpAreas = level.powerUpAreas.Select((r, i) => PowerUpArea(root, r, i)).ToArray(),
            customerEntranceDistance = 1.5f,
        };
        Transform serving = built.First(b => b.name == S.Serving.ToString()).transform;
        layout.servingCounter = serving;
        AddCustomerArea(levelModel.transform, root, layout, serving);
        AddKillZone(root);
        AddDecor(root, level, built.FirstOrDefault(b => b.name == S.Counter.ToString()));

        if (level.movingIslands)
            AddMovingIslands(levelModel.transform, root);

        root.gameObject.SetActive(false);
        return layout;
    }

    // ---------- Floors, customer platform, hazards ----------

    private static Bounds LocalBounds(Transform space, IEnumerable<Renderer> renderers)
    {
        bool first = true;
        var b = new Bounds();
        foreach (Renderer r in renderers)
        {
            Bounds w = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(i % 2 == 0 ? w.min.x : w.max.x, (i / 2) % 2 == 0 ? w.min.y : w.max.y, i / 4 == 0 ? w.min.z : w.max.z);
                Vector3 p = space.InverseTransformPoint(corner);
                if (first) { b = new Bounds(p, Vector3.zero); first = false; }
                else b.Encapsulate(p);
            }
        }
        return b;
    }

    private static BoxCollider Box(string name, Transform parent, Vector3 center, Vector3 size, bool trigger = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var box = go.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
        box.isTrigger = trigger;
        return box;
    }

    private static void AddFloors(Transform levelModel, Transform root, bool meshFloor)
    {
        foreach (Transform slab in levelModel)
        {
            if (slab.name.StartsWith("Customers_") || slab.name.StartsWith("Shuttle_Platform"))
                continue;

            if (meshFloor)
            {
                foreach (MeshFilter f in slab.GetComponentsInChildren<MeshFilter>())
                    if (f.transform == slab || f.name.EndsWith("SurfaceDetail"))
                        f.gameObject.AddComponent<MeshCollider>().sharedMesh = f.sharedMesh;
                continue;
            }

            // Top of every walkable slab is y = 0.
            Bounds b = LocalBounds(root, new[] { slab.GetComponent<Renderer>() });
            Box(slab.name + " Floor", root, new Vector3(b.center.x, -0.5f, b.center.z), new Vector3(b.size.x, 1f, b.size.z));

            Transform deck = slab.Find("Bridge_Deck");
            if (deck != null)
            {
                Bounds d = LocalBounds(root, deck.GetComponentsInChildren<Renderer>());
                Box("Bridge Floor", root, new Vector3(d.center.x, -0.5f, d.center.z), new Vector3(d.size.x, 1f, d.size.z));
            }
        }
    }

    private static void AddCustomerArea(Transform levelModel, Transform root, MapSelector.Layout layout, Transform serving)
    {
        Transform slab = levelModel.Cast<Transform>().First(t => t.name.StartsWith("Customers_"));
        Bounds Platform() => LocalBounds(root, new[] { slab.GetComponent<Renderer>() });

        // Customers don't grow with the map, so their platform keeps its original size (round circles).
        Vector3 originalScale = slab.localScale;
        slab.SetParent(root, true);
        slab.localScale = originalScale;

        // Move the customer platform (children too) so its back edge is 0.5 in front of the kitchen's front edge,
        // centred on the Order Up counter.
        float kitchenFront = levelModel.Cast<Transform>()
            .Where(t => !t.name.StartsWith("Customers_") && !t.name.StartsWith("Shuttle_Platform"))
            .Max(t => LocalBounds(root, new[] { t.GetComponent<Renderer>() }).max.z);
        Bounds b = Platform();
        float servingX = root.InverseTransformPoint(serving.position).x;
        slab.position += root.TransformVector(new Vector3(servingX - b.center.x, 0f, kitchenFront + 0.5f - b.min.z));
        b = Platform();

        // Solid block over the customer platform keeps players (and thrown food) off it.
        Box("Customer Barrier", root, new Vector3(b.center.x, 1.5f, b.center.z), new Vector3(b.size.x, 3f, b.size.z));

        // Pink circles: the cust_queue_* submeshes of the QueueSpotsPosts mesh. Cluster their vertices; each cluster is one circle.
        MeshFilter posts = slab.GetComponentsInChildren<MeshFilter>().First(f => f.name.EndsWith("QueueSpotsPosts"));
        Material[] mats = posts.GetComponent<Renderer>().sharedMaterials;
        Vector3[] verts = posts.sharedMesh.vertices;
        var clusters = new List<List<Vector3>>();
        for (int s = 0; s < posts.sharedMesh.subMeshCount; s++)
        {
            if (mats[s] == null || !mats[s].name.StartsWith("cust_queue_"))
                continue;
            foreach (int i in posts.sharedMesh.GetTriangles(s).Distinct())
            {
                Vector3 p = root.InverseTransformPoint(posts.transform.TransformPoint(verts[i]));
                List<Vector3> home = clusters.FirstOrDefault(c => Vector2.Distance(new Vector2(c[0].x, c[0].z), new Vector2(p.x, p.z)) < 1.2f);
                if (home == null) clusters.Add(home = new List<Vector3>());
                home.Add(p);
            }
        }

        var centers = clusters
            .Select(c => new Vector3((c.Min(v => v.x) + c.Max(v => v.x)) / 2f, 0f, (c.Min(v => v.z) + c.Max(v => v.z)) / 2f))
            .OrderByDescending(c => c.x) // root +x is screen left
            .ToList();
        var spots = new List<Transform>();
        for (int i = 0; i < centers.Count; i++)
        {
            var t = new GameObject($"QueueSpot_{i + 1:00}").transform;
            t.SetParent(root, false);
            t.localPosition = centers[i];
            t.SetParent(slab, true);
            spots.Add(t);
        }

        Vector3 servingLocal = root.InverseTransformPoint(serving.position);
        float Dist(Transform t) { Vector3 p = root.InverseTransformPoint(t.position); return new Vector2(p.x - servingLocal.x, p.z - servingLocal.z).magnitude; }
        layout.queuePoints = spots.OrderBy(Dist).ToArray(); // fill the circles nearest the Order Up first

        var anchor = new GameObject("Customer Anchor").transform;
        anchor.SetParent(root, false);
        anchor.localPosition = new Vector3(b.center.x, 0f, centers.Average(c => c.z));
        anchor.localRotation = Quaternion.Euler(0f, 180f, 0f); // queue faces the kitchen (-z)
        layout.customerAnchor = anchor;

        // Arrival dock: its ramp foot (EntryPoint) meets the platform end nearest the map centre, on the lane just
        // behind the circles, with the dock pointing outward. Customers appear on its pad (SpawnPoint).
        float laneZ = centers.Average(c => c.z) + 0.9f; // root +z = away from the kitchen
        float side = Mathf.Abs(b.max.x) < Mathf.Abs(b.min.x) ? 1f : -1f;
        GameObject dock = Spawn("Kitchen/Customer_ArrivalDock", root);
        dock.name = "Customer Arrival Dock";
        Transform entry = dock.GetComponentsInChildren<Transform>().First(t => t.name == "EntryPoint");
        Transform spawn = dock.GetComponentsInChildren<Transform>().First(t => t.name == "SpawnPoint");
        Vector3 along = root.InverseTransformVector(spawn.position - entry.position);
        along.y = 0f;
        float yaw = Vector3.SignedAngle(along, new Vector3(side, 0f, 0f), Vector3.up);
        dock.transform.rotation = Quaternion.AngleAxis(yaw, root.up) * dock.transform.rotation;
        dock.transform.position += root.TransformPoint(new Vector3(side > 0f ? b.max.x : b.min.x, 0f, laneZ)) - entry.position;
        layout.customerSpawn = spawn;
        layout.customerEntry = entry;

        Debug.Log($"SpaceMapBuilder: {root.name} has {spots.Count} queue circles");
    }

    private static void AddKillZone(Transform root)
    {
        Box("Kill Zone", root, new Vector3(0f, -5f, -4f), new Vector3(120f, 2f, 120f), true).gameObject.AddComponent<KillZone>();
    }

    private static Transform Marker(string name, Transform root, Vector2 xz)
    {
        var t = new GameObject(name).transform;
        t.SetParent(root, false);
        t.localPosition = new Vector3(xz.x, 1f, xz.y);
        t.rotation = Quaternion.identity;
        return t;
    }

    private static Collider PowerUpArea(Transform root, Rect r, int i)
    {
        BoxCollider box = Box("PowerUp Area " + (i + 1), root, Vector3.zero, new Vector3(r.width, 0.74f, r.height), true);
        box.transform.localPosition = new Vector3(r.center.x, 1f, r.center.y);
        box.gameObject.layer = 2; // Ignore Raycast, like the Divided map's areas
        return box;
    }

    private static Transform DeckFrame(Transform slab, Transform root, Bounds b)
    {
        var frame = new GameObject("Deck").transform;
        frame.SetParent(root, false);
        frame.localPosition = new Vector3(b.center.x, 0f, b.center.z);
        frame.SetParent(slab, true);
        return frame;
    }

    private static void AddShuttles(Transform levelModel, Transform root, float scale)
    {
        foreach (Transform shuttle in levelModel)
        {
            if (!shuttle.name.StartsWith("Shuttle_Platform"))
                continue;

            Bounds b = LocalBounds(root, new[] { shuttle.GetComponent<Renderer>() });
            Transform frame = DeckFrame(shuttle, root, b);
            var deck = frame.gameObject.AddComponent<BoxCollider>();
            deck.center = new Vector3(0f, -0.5f, 0f);
            deck.size = new Vector3(b.size.x, 1f, b.size.z); // frame is axis-aligned with root at scale 1
            var riders = frame.gameObject.AddComponent<BoxCollider>();
            riders.isTrigger = true;
            riders.center = new Vector3(0f, 0.8f, 0f);
            riders.size = new Vector3(deck.size.x, 1.6f, deck.size.z);

            // Shuttle 1 docks on the +x island, shuttle 2 on the -x island; each crosses the gap.
            float dir = b.center.x > 0f ? -1f : 1f;
            var mover = shuttle.gameObject.AddComponent<MovingPlatform>();
            mover.localOffset = shuttle.parent.InverseTransformVector(root.TransformVector(new Vector3(3.6f * scale * dir, 0f, 0f)));
            mover.riderZones = new[] { riders };
        }
    }

    // SplitIslands: each island (floor, stations, spawn, decor) rides a mover that slides to the centre
    // until the throw pads meet, waits so players can walk across, then slides apart again.
    private static void AddMovingIslands(Transform levelModel, Transform root)
    {
        SplitThrowZones(levelModel, root);

        float SideOf(Transform t)
        {
            Renderer r = t.GetComponentInChildren<Renderer>();
            return Mathf.Sign(root.InverseTransformPoint(r != null ? r.bounds.center : t.position).x);
        }

        foreach (Transform slab in levelModel.Cast<Transform>().Where(t => t.name.StartsWith("Split_")).ToList())
        {
            Bounds b = LocalBounds(root, new[] { slab.GetComponent<Renderer>() });
            float side = Mathf.Sign(b.center.x);

            var mover = new GameObject("Island Mover " + (side > 0 ? "+X" : "-X")).transform;
            mover.SetParent(root, false);

            // Everything standing on this island moves with it.
            string[] onIsland = { "Spawn", "PowerUp", "GravityPad", "Whiteboard" };
            foreach (Transform t in root.Cast<Transform>().ToList())
                if (t.name == slab.name + " Floor" || (onIsland.Any(t.name.StartsWith) && SideOf(t) == side))
                    t.SetParent(mover, true);
            foreach (Transform st in root.Find("Stations").Cast<Transform>().ToList())
                if (Mathf.Sign(st.localPosition.x) == side)
                    st.SetParent(mover, true);
            slab.SetParent(mover, true);

            var riders = Box("Riders", mover, new Vector3(b.center.x, 1f, b.center.z), new Vector3(b.size.x, 2f, b.size.z), true);
            riders.gameObject.layer = 2;

            var platform = mover.gameObject.AddComponent<MovingPlatform>();
            platform.riderZones = new[] { riders };
            float innerEdge = side > 0 ? b.min.x : -b.max.x;
            platform.localOffset = new Vector3(-side * (innerEdge + 0.05f), 0f, 0f); // slight overlap: no seam to trip on
            platform.travelTime = 2.5f;
            platform.waitTime = 5f;
        }
    }

    // The throw pads for both islands are one mesh on the +X island; split it so each island carries its own.
    private static void SplitThrowZones(Transform levelModel, Transform root)
    {
        MeshFilter zones = levelModel.GetComponentsInChildren<MeshFilter>().FirstOrDefault(f => f.name.EndsWith("ThrowZones"));
        if (zones == null)
            return;
        Transform otherSlab = levelModel.Cast<Transform>().First(t => t.name.StartsWith("Split_") && !zones.transform.IsChildOf(t));

        Mesh src = zones.sharedMesh;
        Vector3[] v = src.vertices;
        Mesh Half(bool positive)
        {
            var m = Object.Instantiate(src);
            m.name = src.name + (positive ? "_PosX" : "_NegX");
            for (int s = 0; s < src.subMeshCount; s++)
            {
                int[] tris = src.GetTriangles(s);
                var keep = new List<int>();
                for (int i = 0; i < tris.Length; i += 3)
                {
                    Vector3 centroid = zones.transform.TransformPoint((v[tris[i]] + v[tris[i + 1]] + v[tris[i + 2]]) / 3f);
                    if (root.InverseTransformPoint(centroid).x > 0f == positive)
                        keep.AddRange(new[] { tris[i], tris[i + 1], tris[i + 2] });
                }
                m.SetTriangles(keep, s);
            }
            string dir = "Assets/Data/Meshes";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/Data", "Meshes");
            string path = $"{dir}/{m.name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        bool zonesOnPositive = root.InverseTransformPoint(zones.transform.parent.GetComponent<Renderer>().bounds.center).x > 0f;
        Mesh own = Half(zonesOnPositive), other = Half(!zonesOnPositive);
        GameObject copy = Object.Instantiate(zones.gameObject, zones.transform.parent);
        copy.name = zones.name;
        copy.GetComponent<MeshFilter>().sharedMesh = other;
        copy.transform.SetParent(otherSlab, true);
        zones.sharedMesh = own;
    }

    // ---------- Decoration ----------

    private static Bounds WorldBounds(Transform t)
    {
        Renderer[] rs = t.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs)
            b.Encapsulate(r.bounds);
        return b;
    }

    // Turns t by yaw (around the root's up axis), then moves it so its bounds centre is at x/z and its bottom at y.
    private static void PlaceAt(Transform t, Transform root, float x, float bottomY, float z, float yaw = 0f)
    {
        t.rotation = Quaternion.AngleAxis(yaw, root.up) * t.rotation;
        Bounds b = WorldBounds(t);
        Vector3 target = root.TransformPoint(new Vector3(x, bottomY, z));
        t.position += new Vector3(target.x - b.center.x, target.y - b.min.y, target.z - b.center.z);
    }

    private static void AddDecor(Transform root, Level level, GameObject lampCounter)
    {
        Transform decor = new GameObject("Decor").transform;
        decor.SetParent(root, false);

        // Big Bang Burgers sign floats just behind the back of the kitchen, facing the camera.
        GameObject sign = Spawn("Kitchen/Sign_BigBangBurgers", decor);
        sign.transform.localScale = Vector3.one * 1.4f;
        PlaceAt(sign.transform, root, level.sign.x, level.sign.y, level.sign.z);

        GameObject props = Spawn("Kitchen/Props_Space", decor);
        PlayScene(props, "Props_Space"); // solar-system mobile spins
        Transform board = props.transform.Find("Whiteboard_Equations");
        board.SetParent(root, true);
        board.name = "Whiteboard";
        PlaceAt(board, root, level.whiteboard.x, 0f, level.whiteboard.y, level.whiteboard.z);
        Transform mobile = props.transform.Find("SolarSystem_Mobile");
        PlaceAt(mobile, root, level.mobile.x, level.mobile.y, level.mobile.z);

        // Lava-lamp condiment dispensers sit on the back corners of a counter.
        if (lampCounter != null)
        {
            Transform counter = lampCounter.transform;
            Bounds top = WorldBounds(counter.Find("Space Visual"));
            int i = 0;
            foreach (string lamp in new[] { "LavaLamp_Ketchup", "LavaLamp_Mustard" })
            {
                Transform t = props.transform.Find(lamp);
                Bounds b = WorldBounds(t);
                Vector3 target = top.center + counter.right * (i++ == 0 ? 0.45f : -0.45f) - counter.forward * 0.45f;
                t.position += new Vector3(target.x - b.center.x, top.max.y - b.min.y, target.z - b.center.z);
                t.SetParent(counter, true);
            }
        }

        // Asteroids + shooting stars: spread out to suit the bigger map.
        GameObject env = Spawn("Environment/Environment_Space", decor);
        env.transform.localScale = Vector3.one * level.scale * 1.15f;
        PlayScene(env, "Env_Space");

        AddGravityPads(root, level, env.transform);
    }

    // Gravity-flip pads: the purple tile + a trigger + GravityPad, with a ZERO-G caution decal in front of each.
    private static readonly Color HopGlow = new Color(0.65f, 0.3f, 1f);
    private static readonly Color SenderGlow = new Color(1f, 0.5f, 0.1f);
    private static readonly Color ReceiverGlow = new Color(0.65f, 0.3f, 1f);

    private static void AddGravityPads(Transform root, Level level, Transform env)
    {
        Transform tileSource = env.Find("GravityFlipTile_01");
        GameObject mainModel = Spawn("Levels/Level_Main", root);
        Transform decalSource = mainModel.GetComponentsInChildren<Transform>().First(t => t.name == "Decal_CautionZeroG_1");
        Transform decalTextSource = mainModel.GetComponentsInChildren<Transform>().First(t => t.name == "Decal_CautionZeroG_1_Text");

        var pads = new List<GravityPad>();
        for (int i = 0; i < level.pads.Length; i++)
        {
            PadSpec spec = level.pads[i];
            var pad = new GameObject($"GravityPad {i + 1}" + (spec.pair >= 0 ? (spec.sender ? " (Sender)" : " (Receiver)") : "")).transform;
            pad.SetParent(root, false);
            pad.localPosition = new Vector3(spec.x, 0f, spec.z);

            Transform tile = Object.Instantiate(tileSource.gameObject, env).transform;
            tile.name = "Tile";
            tile.SetParent(pad, true);
            tile.localScale = Vector3.Scale(tile.localScale, new Vector3(1f / env.localScale.x, 1f / env.localScale.y, 1f / env.localScale.z));
            PlaceAt(tile, root, spec.x, 0.01f, spec.z);
            Bounds tb = LocalBounds(pad, tile.GetComponentsInChildren<Renderer>());

            foreach (Transform src in new[] { decalSource, decalTextSource })
            {
                Transform decal = Object.Instantiate(src.gameObject, mainModel.transform).transform;
                decal.name = src.name;
                decal.SetParent(pad, true);
                PlaceAt(decal, root, spec.x, 0.012f, spec.z + tb.size.z * 0.5f + 0.45f);
            }

            var trigger = pad.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.5f, 0f);
            trigger.size = new Vector3(tb.size.x, 1f, tb.size.z);
            pad.gameObject.layer = 2; // Ignore Raycast

            GravityPad gp = pad.gameObject.AddComponent<GravityPad>();
            gp.glowRenderer = tile.GetComponent<Renderer>();
            gp.tileSize = level.scale;
            gp.hopTiles = level.hopTiles;
            gp.mode = spec.pair >= 0 ? GravityPad.PadMode.ItemLauncher : GravityPad.PadMode.ZeroGHop;
            gp.isSender = spec.sender;
            gp.glowColor = spec.pair < 0 ? HopGlow : spec.sender ? SenderGlow : ReceiverGlow;
            pads.Add(gp); // hop direction comes from the arrow on the tile (pad -Z)
        }

        // Link launcher pairs: each Sender fires at the Receiver with the same pair number.
        for (int i = 0; i < pads.Count; i++)
            if (level.pads[i].sender)
                pads[i].receiver = pads.Where((p, j) => level.pads[j].pair == level.pads[i].pair && !level.pads[j].sender).FirstOrDefault();

        Object.DestroyImmediate(mainModel);
        foreach (Transform t in env.Cast<Transform>().Where(t => t.name.StartsWith("GravityFlipTile")).ToList())
            Object.DestroyImmediate(t.gameObject);

        // Main's own floor decals are replaced by the per-pad ones.
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Decal_CautionZeroG") && t.parent.name == "Platform_Slab").ToList())
            Object.DestroyImmediate(t.gameObject);
    }

    // Plays the FBX's own looping "Scene" animation.
    private static void PlayScene(GameObject go, string controller)
    {
        Animator animator = go.GetComponent<Animator>();
        if (animator == null)
            animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"Assets/Data/Animators/{controller}.controller");
        animator.applyRootMotion = false;
    }

    // ---------- Stations ----------

    private static GameObject MakeStation(S kind, Transform parent, Vector3 localPos, float yaw, Transform furniture, Transform classic)
    {
        Transform template = classic.Find(Template[kind]);
        GameObject st;
        if (kind == S.Bin)
        {
            st = new GameObject("Bin");
            st.AddComponent<TrashBin>();
        }
        else
        {
            st = Object.Instantiate(template.gameObject);
            st.name = kind.ToString();
        }

        st.transform.SetParent(parent, false);
        st.transform.localPosition = localPos;
        st.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        if (kind == S.Bin)
            st.transform.localScale = Vector3.one;

        string[] pieceNames = Pieces[kind];
        if (pieceNames.Length == 0)
            return st; // ice cream keeps its old look

        foreach (Renderer r in st.GetComponentsInChildren<Renderer>(true))
            if (!(r is ParticleSystemRenderer))
                r.enabled = false;

        // Cancels the template's non-uniform scale so the space model isn't stretched.
        var visual = new GameObject("Space Visual").transform;
        visual.SetParent(st.transform, false);
        Vector3 s = st.transform.localScale;
        visual.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);

        Quaternion turn = parent.rotation * Quaternion.Euler(0f, yaw, 0f) * Quaternion.Inverse(parent.rotation);
        Vector3 worldCenter = parent.TransformPoint(localPos);
        float floorY = worldCenter.y;
        foreach (string pieceName in pieceNames)
        {
            Transform piece = Object.Instantiate(furniture.Find(pieceName).gameObject, furniture).transform;
            piece.rotation = turn * piece.rotation;

            Bounds b = WorldBounds(piece);
            piece.position += new Vector3(worldCenter.x - b.center.x, floorY - b.min.y, worldCenter.z - b.center.z);
            piece.SetParent(visual, true);
            floorY = WorldBounds(piece).max.y; // next piece sits on top
        }

        // The space "plates" model is too small to read as a plate station; reuse the game's original plate stack.
        if (kind == S.Plates)
        {
            Transform stack = st.transform.Find("Plates");
            foreach (Renderer r in stack.GetComponentsInChildren<Renderer>(true))
                r.enabled = true;
            Bounds sb = WorldBounds(stack);
            stack.position += new Vector3(worldCenter.x - sb.center.x, floorY - sb.min.y, worldCenter.z - sb.center.z);
        }

        Bounds local = LocalBounds(st.transform, visual.GetComponentsInChildren<Renderer>());
        BoxCollider box = st.GetComponent<BoxCollider>();
        if (box == null)
            box = st.AddComponent<BoxCollider>();
        box.center = local.center;
        box.size = local.size;

        if (kind == S.Serving)
            FixServeBurst(st, template);
        return st;
    }

    // The template's serve burst lives in Classic's decor; give each serving counter its own copy.
    private static void FixServeBurst(GameObject st, Transform template)
    {
        ServingCounter src = template.GetComponent<ServingCounter>();
        if (src.serveBurst == null)
            return;

        ParticleSystem burst = Object.Instantiate(src.serveBurst, st.transform);
        burst.name = "Serve Burst";
        burst.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        st.GetComponent<ServingCounter>().serveBurst = burst;
    }
}

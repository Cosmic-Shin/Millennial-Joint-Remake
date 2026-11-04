using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds the six space maps as layouts inside SampleScene (next to Classic / Divided / Floating)
// and registers them with MapSelector. Station scripts are cloned from "Map - Classic";
// their old meshes are hidden and the matching Furniture_Space piece is added as the visual.
//
// Coordinates below are in the level FBX's own space: customers are at +z, kitchens around z = -4.
// Each map root is turned 180 degrees and shifted so it lines up with the existing camera / Classic map.
// Yaw = direction the station's front (the side players use) faces; 0 = +z.
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
        { S.Plates, new[] { "base_top", "plates" } }, { S.Cups, new[] { "base_top", "glasses" } },
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
        public Place[] places;
        public Vector2 p1, p2;
        public Rect[] powerUpAreas;
        public bool meshFloor;
        public bool furnitureLayout;
    }

    private static Place P(S kind, float x, float z, float yaw) => new Place(kind, x, z, yaw);

    // Two-island kitchens: cooking on the +x island (screen left, P1), plating / drinks / serving on the -x island.
    private static Place[] SplitKitchen(float outer)
    {
        return new[]
        {
            P(S.Fridge, outer, 0.6f, -90), P(S.Grill, outer, -1.2f, -90), P(S.Grill, outer, -3.0f, -90),
            P(S.Fryer, outer, -4.8f, -90), P(S.Fryer, outer, -6.6f, -90), P(S.Chili, outer, -8.4f, -90),
            P(S.Counter, outer - 2.0f, -9.4f, 0), P(S.Bin, outer - 3.8f, -9.4f, 0),

            P(S.Serving, -outer + 2.6f, 1.6f, 180),
            P(S.Plates, -outer, -1.2f, 90), P(S.Cups, -outer, -3.0f, 90), P(S.Soda, -outer, -4.8f, 90),
            P(S.Coffee, -outer, -6.6f, 90), P(S.IceCream, -outer, -8.4f, 90),
            P(S.Counter, -outer + 2.0f, -9.4f, 0), P(S.Bin, -outer + 3.8f, -9.4f, 0),
        };
    }

    private static Level[] Levels()
    {
        var ring = new List<Place>();
        void Around(S kind, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad, r = 7.4f;
            // front faces the ring's centre
            ring.Add(P(kind, Mathf.Cos(a) * r, -4f + Mathf.Sin(a) * r, Mathf.Atan2(-Mathf.Cos(a), -Mathf.Sin(a)) * Mathf.Rad2Deg));
        }
        Around(S.Serving, 90);
        Around(S.Fridge, 62); Around(S.Grill, 40); Around(S.Grill, 18); Around(S.Fryer, -4); Around(S.Fryer, -26); Around(S.Chili, -48); Around(S.Counter, -70);
        Around(S.Plates, 118); Around(S.Cups, 140); Around(S.Soda, 162); Around(S.Coffee, 184); Around(S.IceCream, 206); Around(S.Counter, 228);
        Around(S.Bin, -92);

        return new[]
        {
            new Level
            {
                name = "Main", furnitureLayout = true,
                // Furniture_Space supplies the rest; these fill in what it doesn't have.
                places = new[] { P(S.IceCream, -6.6f, -2.6f, 90), P(S.Fryer, 8.6f, -5.6f, -90), P(S.Counter, 4.4f, -0.3f, 180) },
                p1 = new Vector2(4f, -2.5f), p2 = new Vector2(-2f, -2.5f),
                powerUpAreas = new[] { new Rect(-6f, -4f, 5f, 2.5f), new Rect(3f, -2.5f, 5f, 2.5f) },
            },
            new Level
            {
                name = "SplitIslands", places = SplitKitchen(8.6f),
                p1 = new Vector2(5.5f, -4f), p2 = new Vector2(-5.5f, -4f),
                powerUpAreas = new[] { new Rect(3.5f, -6f, 3.5f, 4f), new Rect(-7f, -6f, 3.5f, 4f) },
            },
            new Level
            {
                name = "BridgeIslands", places = SplitKitchen(9.6f),
                p1 = new Vector2(6.5f, -4f), p2 = new Vector2(-6.5f, -4f),
                powerUpAreas = new[] { new Rect(4.5f, -6f, 3.5f, 4f), new Rect(-8f, -6f, 3.5f, 4f) },
            },
            new Level
            {
                name = "RingStation", meshFloor = true, places = ring.ToArray(),
                p1 = new Vector2(5.2f, -4f), p2 = new Vector2(-5.2f, -4f),
                powerUpAreas = new[] { new Rect(3.8f, -6f, 2f, 4f), new Rect(-5.8f, -6f, 2f, 4f) },
            },
            new Level
            {
                name = "ShuttleHop", places = SplitKitchen(10.1f),
                p1 = new Vector2(7f, -4f), p2 = new Vector2(-7f, -4f),
                powerUpAreas = new[] { new Rect(5f, -6f, 3.5f, 4f), new Rect(-8.5f, -6f, 3.5f, 4f) },
            },
            new Level
            {
                name = "AsteroidDiner", meshFloor = true,
                places = new[]
                {
                    P(S.Fridge, -4.25f, -7.4f, 0), P(S.Grill, -2.55f, -7.4f, 0), P(S.Grill, -0.85f, -7.4f, 0),
                    P(S.Fryer, 0.85f, -7.4f, 0), P(S.Fryer, 2.55f, -7.4f, 0), P(S.Chili, 4.25f, -7.4f, 0),
                    P(S.Plates, -5.3f, -5.4f, 90), P(S.Cups, -5.3f, -3.7f, 90), P(S.Soda, -5.3f, -2.0f, 90),
                    P(S.Coffee, 5.3f, -5.4f, -90), P(S.IceCream, 5.3f, -3.7f, -90), P(S.Counter, 5.3f, -2.0f, -90),
                    P(S.Serving, 0f, 0.2f, 180), P(S.Bin, -2.6f, 0.2f, 180), P(S.Counter, 2.6f, 0.2f, 180),
                },
                p1 = new Vector2(2f, -4f), p2 = new Vector2(-2f, -4f),
                powerUpAreas = new[] { new Rect(-3f, -5.5f, 2.5f, 3f), new Rect(0.5f, -5.5f, 2.5f, 3f) },
            },
        };
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
        Transform classic = SceneRoot("Map - Classic");
        MapSelector selector = Object.FindFirstObjectByType<MapSelector>(FindObjectsInactive.Include);
        if (classic == null || selector == null)
        {
            Debug.LogError("SpaceMapBuilder: needs 'Map - Classic' and a MapSelector in the open scene.");
            return;
        }

        var layouts = selector.layouts.Where(l => l.root == null || !l.root.name.StartsWith("Map - Space")).ToList();
        foreach (Level level in Levels())
            layouts.Add(Build(level, classic));
        selector.layouts = layouts.ToArray();

        Transform bridge = SceneRoot("Customer Bridge");
        selector.customerQueue = Object.FindFirstObjectByType<CustomerQueue>(FindObjectsInactive.Include);
        if (bridge != null)
            selector.customerBridgeVisuals = bridge.Cast<Transform>().Where(t => t.GetComponent<CustomerQueue>() == null).Select(t => t.gameObject).ToArray();

        EditorUtility.SetDirty(selector);
        EditorSceneManager.MarkSceneDirty(selector.gameObject.scene);
        Debug.Log("SpaceMapBuilder: built " + Levels().Length + " space maps.");
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

        GameObject levelModel = Spawn("Levels/Level_" + level.name, root);
        AddFloors(levelModel.transform, root, level.meshFloor);

        GameObject furniture = Spawn("Kitchen/Furniture_Space", root);
        Transform stations = new GameObject("Stations").transform;
        stations.SetParent(root, false);

        if (level.furnitureLayout)
        {
            GameObject laidOut = Spawn("Kitchen/Furniture_Space", root);
            BuildFromFurniture(laidOut.transform, furniture.transform, stations, classic);
            Object.DestroyImmediate(laidOut);
        }
        foreach (Place p in level.places)
            MakeStation(p.kind, stations, new Vector3(p.x, 0f, p.z), p.yaw, furniture.transform, classic, null);

        Object.DestroyImmediate(furniture);

        if (level.name == "ShuttleHop")
            AddShuttles(levelModel.transform, root);

        var layout = new MapSelector.Layout
        {
            name = level.name,
            root = root.gameObject,
            player1Spawn = Marker("Spawn P1", root, level.p1),
            player2Spawn = Marker("Spawn P2", root, level.p2),
            powerUpAreas = level.powerUpAreas.Select((r, i) => PowerUpArea(root, r, i)).ToArray(),
            customerEntranceDistance = 1.5f,
        };
        AddCustomerArea(levelModel.transform, root, layout);
        AddKillZone(root);
        AddDecor(root, level.name);

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

    private static void AddCustomerArea(Transform levelModel, Transform root, MapSelector.Layout layout)
    {
        Transform slab = levelModel.Cast<Transform>().First(t => t.name.StartsWith("Customers_"));

        // Solid block over the customer platform keeps players (and thrown food) off it.
        Bounds b = LocalBounds(root, new[] { slab.GetComponent<Renderer>() });
        Box("Customer Barrier", root, new Vector3(b.center.x, 1.5f, b.center.z), new Vector3(b.size.x, 3f, b.size.z));

        // Queue spots: the wide vertex clusters of the QueueSpotsPosts mesh (the thin ones are rope posts).
        MeshFilter spots = slab.GetComponentsInChildren<MeshFilter>().First(f => f.name.EndsWith("QueueSpotsPosts"));
        var verts = spots.sharedMesh.vertices.Select(v => root.InverseTransformPoint(spots.transform.TransformPoint(v))).OrderBy(v => v.x).ToList();
        var centers = new List<Vector3>();
        for (int i = 0; i < verts.Count;)
        {
            int j = i;
            while (j + 1 < verts.Count && verts[j + 1].x - verts[j].x < 0.3f) j++;
            List<Vector3> c = verts.GetRange(i, j - i + 1);
            if (c[c.Count - 1].x - c[0].x > 0.6f)
                centers.Add(new Vector3((c[0].x + c[c.Count - 1].x) / 2f, 0f, (c.Min(v => v.z) + c.Max(v => v.z)) / 2f));
            i = j + 1;
        }

        var anchor = new GameObject("Customer Anchor").transform;
        anchor.SetParent(root, false);
        anchor.localPosition = new Vector3(centers.Average(c => c.x), 0f, centers.Average(c => c.z));
        anchor.localRotation = Quaternion.Euler(0f, 180f, 0f); // customers face the kitchen (-z)
        layout.customerAnchor = anchor;

        layout.queuePoints = centers
            .Select((c, i) => { var t = new GameObject("Queue Spot " + i).transform; t.SetParent(anchor.parent, false); t.localPosition = c; return t; })
            .OrderBy(t => anchor.InverseTransformPoint(t.position).x)
            .ToArray();
        foreach (Transform t in layout.queuePoints)
            t.SetParent(anchor, true);
    }

    private static void AddKillZone(Transform root)
    {
        Box("Kill Zone", root, new Vector3(0f, -5f, -4f), new Vector3(80f, 2f, 80f), true).gameObject.AddComponent<KillZone>();
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

    private static void AddShuttles(Transform levelModel, Transform root)
    {
        foreach (Transform shuttle in levelModel)
        {
            if (!shuttle.name.StartsWith("Shuttle_Platform"))
                continue;

            Bounds b = LocalBounds(root, new[] { shuttle.GetComponent<Renderer>() });
            var frame = new GameObject("Deck").transform;
            frame.SetParent(root, false);
            frame.localPosition = new Vector3(b.center.x, 0f, b.center.z);
            frame.SetParent(shuttle, true);
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
            mover.localOffset = shuttle.parent.InverseTransformVector(root.TransformVector(new Vector3(3.6f * dir, 0f, 0f)));
        }
    }

    private static void AddDecor(Transform root, string level)
    {
        Transform decor = new GameObject("Decor").transform;
        decor.SetParent(root, false);

        GameObject sign = Spawn("Kitchen/Sign_BigBangBurgers", decor);
        sign.transform.localPosition = new Vector3(0f, 0f, -14f);
        PlayScene(Spawn("Kitchen/Props_Space", decor), "Props_Space");          // solar-system mobile
        PlayScene(Spawn("Environment/Environment_Space", decor), "Env_Space"); // asteroids + shooting stars
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

    private static readonly Dictionary<string, S> FurnitureStations = new Dictionary<string, S>
    {
        { "fridge", S.Fridge }, { "grill_mt", S.Grill }, { "grill_mt.001", S.Grill }, { "fryer_mt", S.Fryer },
        { "plates", S.Plates }, { "glasses", S.Cups }, { "SodaMachine", S.Soda }, { "CoffeeMachine", S.Coffee },
        { "base_top", S.Counter }, { "base_top.002", S.Counter }, { "ChiliPot", S.Chili }, { "dustbin", S.Bin },
        { "OrderUp_Teleporter", S.Serving },
    };

    // Level_Main: a station at every Furniture_Space piece, using that exact piece (as laid out) as its visual.
    private static void BuildFromFurniture(Transform furniture, Transform library, Transform stations, Transform classic)
    {
        foreach (var pair in FurnitureStations)
        {
            Transform piece = furniture.Find(pair.Key);
            Bounds b = LocalBounds(stations, piece.GetComponentsInChildren<Renderer>());
            MakeStation(pair.Value, stations, new Vector3(b.center.x, 0f, b.center.z), 0f, library, classic, piece);
        }

        // Remaining props (paper etc.) stay as decoration.
        Transform props = new GameObject("Furniture Props").transform;
        props.SetParent(stations.parent, false);
        foreach (Transform t in furniture.Cast<Transform>().ToList())
            if (t.GetComponentsInChildren<Renderer>().Any(r => r.bounds.size.sqrMagnitude > 0.0001f))
                t.SetParent(props, true);
    }

    private static void MakeStation(S kind, Transform parent, Vector3 localPos, float yaw, Transform furniture, Transform classic, Transform existingPiece)
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
            return; // ice cream keeps its old look

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
        for (int i = 0; i < pieceNames.Length; i++)
        {
            Transform piece = i == 0 && existingPiece != null && pieceNames.Length == 1
                ? existingPiece
                : Object.Instantiate(furniture.Find(pieceNames[i]).gameObject, furniture).transform;

            if (piece != existingPiece)
                piece.rotation = turn * piece.rotation;

            Bounds b = WorldBounds(piece);
            piece.position += new Vector3(worldCenter.x - b.center.x, floorY - b.min.y, worldCenter.z - b.center.z);
            piece.SetParent(visual, true);
            floorY = WorldBounds(piece).max.y; // next piece sits on top
        }

        if (existingPiece != null && existingPiece.parent != visual)
            Object.DestroyImmediate(existingPiece.gameObject); // plates / glasses: replaced by counter + stack

        Bounds local = LocalBounds(st.transform, visual.GetComponentsInChildren<Renderer>());
        BoxCollider box = st.GetComponent<BoxCollider>();
        if (box == null)
            box = st.AddComponent<BoxCollider>();
        box.center = local.center;
        box.size = local.size;

        if (kind == S.Serving)
            FixServeBurst(st, template);
    }

    private static Bounds WorldBounds(Transform t)
    {
        Renderer[] rs = t.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs)
            b.Encapsulate(r.bounds);
        return b;
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

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Splits the Main kitchen's floor clutter (ketchup/mustard spills, scuffs, crumbs, stickers) into single
// pieces and scatters them over the other maps at the same density. Re-run after rebuilding the maps.
public static class FloorDecalScatter
{
    const string SourceName = "Decal_SpillsFootprintsStickers";
    const string GroupName = "Floor Decals";
    const string MeshAsset = "Assets/Data/FloorDecalPieces.asset";
    const float Spacing = 1f;          // floor sampling grid, for measuring floor area
    const float StationGap = 0.6f;     // keep clutter this far from stations

    [MenuItem("Tools/Scatter Floor Decals")]
    public static void Scatter()
    {
        MapSelector selector = Object.FindFirstObjectByType<MapSelector>(FindObjectsInactive.Include);
        MapSelector.Layout main = System.Array.Find(selector.layouts, l => l.name == "Main");
        MeshRenderer source = FindSource(main.root.transform);
        if (source == null)
        {
            Debug.LogError("FloorDecalScatter: no " + SourceName + " in the Main map");
            return;
        }

        var active = new Dictionary<GameObject, bool>();
        foreach (var l in selector.layouts)
            active[l.root] = l.root.activeSelf;

        // The visible tiles have no colliders of their own; add temporary ones so pieces sit on the tiles (and only on them).
        tileColliders.Clear();
        foreach (var l in selector.layouts)
            foreach (MeshRenderer r in l.root.GetComponentsInChildren<MeshRenderer>(true))
                if (r.name.EndsWith("Tiles") && r.GetComponent<Collider>() == null)
                    tileColliders.Add(r.gameObject.AddComponent<MeshCollider>());

        Solo(selector, main);
        List<Piece> pieces = SplitIntoPieces(source);
        float decalLift = Mathf.Max(0.003f, source.bounds.min.y - FloorY(main.root, source.bounds.center));
        float density = pieces.Count / Mathf.Max(1f, FloorArea(selector, main));

        int total = 0;
        foreach (var layout in selector.layouts)
        {
            if (layout == main)
                continue;
            Solo(selector, layout);
            total += ScatterOn(layout, pieces, Mathf.RoundToInt(density * FloorArea(selector, layout)), decalLift, source.sharedMaterials);
        }

        foreach (MeshCollider c in tileColliders)
            Object.DestroyImmediate(c);
        tileColliders.Clear();
        foreach (var kv in active)
            kv.Key.SetActive(kv.Value);
        EditorSceneManager.MarkSceneDirty(selector.gameObject.scene);
        Debug.Log($"FloorDecalScatter: {pieces.Count} pieces from Main, placed {total} on the other maps");
    }

    private static readonly List<MeshCollider> tileColliders = new List<MeshCollider>();

    private class Piece
    {
        public Mesh mesh;
        public Vector2 halfSize;
    }

    private static MeshRenderer FindSource(Transform root)
    {
        foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
            if (r.name == SourceName)
                return r;
        return null;
    }

    // One mesh per connected island of triangles, baked to world scale and centred on its own middle.
    private static List<Piece> SplitIntoPieces(MeshRenderer source)
    {
        Mesh m = source.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] verts = m.vertices;
        Vector2[] uvs = m.uv;
        Matrix4x4 toWorld = source.transform.localToWorldMatrix;

        // Union-find over vertices, joining every triangle's corners (and vertices sharing a position).
        int[] parent = new int[verts.Length];
        for (int i = 0; i < parent.Length; i++) parent[i] = i;
        int Find(int x) { while (parent[x] != x) x = parent[x] = parent[parent[x]]; return x; }
        void Join(int a, int b) { parent[Find(a)] = Find(b); }

        var byPos = new Dictionary<Vector3, int>();
        for (int i = 0; i < verts.Length; i++)
        {
            if (byPos.TryGetValue(verts[i], out int other)) Join(i, other);
            else byPos[verts[i]] = i;
        }
        for (int s = 0; s < m.subMeshCount; s++)
        {
            int[] tris = m.GetTriangles(s);
            for (int t = 0; t < tris.Length; t += 3)
            {
                Join(tris[t], tris[t + 1]);
                Join(tris[t], tris[t + 2]);
            }
        }

        // Collect each island's triangles per submesh, so every piece keeps its colours.
        var islands = new Dictionary<int, List<int>[]>();
        for (int s = 0; s < m.subMeshCount; s++)
        {
            int[] tris = m.GetTriangles(s);
            for (int t = 0; t < tris.Length; t += 3)
            {
                int root = Find(tris[t]);
                if (!islands.TryGetValue(root, out var perSub))
                {
                    perSub = new List<int>[m.subMeshCount];
                    for (int k = 0; k < perSub.Length; k++) perSub[k] = new List<int>();
                    islands[root] = perSub;
                }
                perSub[s].Add(tris[t]); perSub[s].Add(tris[t + 1]); perSub[s].Add(tris[t + 2]);
            }
        }

        var pieces = new List<Piece>();
        foreach (var perSub in islands.Values)
        {
            var remap = new Dictionary<int, int>();
            var pv = new List<Vector3>();
            var puv = new List<Vector2>();
            foreach (var list in perSub)
                foreach (int v in list)
                    if (!remap.ContainsKey(v))
                    {
                        remap[v] = pv.Count;
                        pv.Add(toWorld.MultiplyPoint3x4(verts[v]));
                        if (uvs.Length > 0) puv.Add(uvs[v]);
                    }

            Bounds b = new Bounds(pv[0], Vector3.zero);
            foreach (Vector3 p in pv) b.Encapsulate(p);
            for (int i = 0; i < pv.Count; i++)
                pv[i] -= new Vector3(b.center.x, b.min.y, b.center.z);

            var mesh = new Mesh { name = "FloorDecal_" + pieces.Count };
            mesh.SetVertices(pv);
            if (puv.Count == pv.Count) mesh.SetUVs(0, puv);
            mesh.subMeshCount = perSub.Length;
            for (int s = 0; s < perSub.Length; s++)
            {
                var local = new int[perSub[s].Count];
                for (int i = 0; i < local.Length; i++) local[i] = remap[perSub[s][i]];
                mesh.SetTriangles(local, s);
            }
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            pieces.Add(new Piece { mesh = mesh, halfSize = new Vector2(b.extents.x, b.extents.z) });
        }
        // All pieces live in one asset; re-running replaces it instead of piling up meshes.
        AssetDatabase.DeleteAsset(MeshAsset);
        AssetDatabase.CreateAsset(pieces[0].mesh, MeshAsset);
        for (int i = 1; i < pieces.Count; i++)
            AssetDatabase.AddObjectToAsset(pieces[i].mesh, pieces[0].mesh);
        AssetDatabase.SaveAssets();
        return pieces;
    }

    private static int ScatterOn(MapSelector.Layout layout, List<Piece> pieces, int count, float lift, Material[] mats)
    {
        Transform root = layout.root.transform;
        Transform old = root.Find("Decor/" + GroupName);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        foreach (MovingPlatform mp in root.GetComponentsInChildren<MovingPlatform>(true))
        {
            Transform o = mp.transform.Find(GroupName);
            if (o != null) Object.DestroyImmediate(o.gameObject);
        }

        Bounds area = FloorBounds(layout);
        var rng = new System.Random(root.name.GetHashCode());
        var groups = new Dictionary<Transform, Transform>();
        int placed = 0;

        for (int attempt = 0; placed < count && attempt < count * 40; attempt++)
        {
            Piece piece = pieces[rng.Next(pieces.Count)];
            float yaw = (float)rng.NextDouble() * 360f;
            Vector3 at = new Vector3(Mathf.Lerp(area.min.x, area.max.x, (float)rng.NextDouble()), 0f,
                                     Mathf.Lerp(area.min.z, area.max.z, (float)rng.NextDouble()));
            if (!IsOpenFloor(layout, at, Mathf.Max(piece.halfSize.x, piece.halfSize.y), out RaycastHit hit))
                continue;

            MovingPlatform mover = hit.collider.GetComponentInParent<MovingPlatform>();
            Transform parentKey = mover != null ? mover.transform : root.Find("Decor");
            if (!groups.TryGetValue(parentKey, out Transform group))
            {
                group = new GameObject(GroupName).transform;
                group.SetParent(parentKey, false);
                groups[parentKey] = group;
            }

            var go = new GameObject(piece.mesh.name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(group, true);
            go.transform.SetPositionAndRotation(hit.point + Vector3.up * lift, Quaternion.Euler(0f, yaw, 0f));
            Vector3 s = go.transform.parent.lossyScale; // stay true to world size under scaled parents
            go.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
            go.GetComponent<MeshFilter>().sharedMesh = piece.mesh;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterials = mats;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.isStatic = mover == null;
            placed++;
        }
        return placed;
    }

    // Floor under all four corners and the middle, nothing standing on it, not the customer area.
    private static bool IsOpenFloor(MapSelector.Layout layout, Vector3 at, float radius, out RaycastHit hit)
    {
        if (!FloorHit(layout, at, out hit))
            return false;
        foreach (Vector2 c in new[] { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1) })
            if (!FloorHit(layout, at + new Vector3(c.x, 0f, c.y) * radius, out RaycastHit corner) || Mathf.Abs(corner.point.y - hit.point.y) > 0.05f)
                return false;
            else if (corner.point.y > hit.point.y)
                hit.point = new Vector3(hit.point.x, corner.point.y, hit.point.z); // sit on the tile tops, not in the grout

        Vector3 half = new Vector3(radius + StationGap, 0.6f, radius + StationGap);
        foreach (Collider c in Physics.OverlapBox(hit.point + Vector3.up * 0.7f, half, Quaternion.identity, ~0, QueryTriggerInteraction.Collide))
        {
            if (!c.transform.IsChildOf(layout.root.transform))
                continue;
            if (c.GetComponentInParent<BaseStation>() != null || c.GetComponentInParent<GravityPad>() != null)
                return false;
            if (!c.isTrigger && c != hit.collider)
                return false;
        }
        return true;
    }

    private static bool FloorHit(MapSelector.Layout layout, Vector3 at, out RaycastHit hit)
    {
        hit = default;
        foreach (RaycastHit h in Physics.RaycastAll(at + Vector3.up * 5f, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!h.collider.transform.IsChildOf(layout.root.transform) || !(h.collider is MeshCollider mc && tileColliders.Contains(mc)))
                continue;
            if (hit.collider == null || h.point.y > hit.point.y)
                hit = h;
        }
        if (hit.collider == null || hit.normal.y < 0.95f || Mathf.Abs(hit.point.y) > 0.3f)
            return false;
        string n = hit.collider.name + " " + hit.collider.transform.parent?.name;
        return !n.Contains("Customer") && !n.Contains("ArrivalDock") && !n.Contains("Bridge_Deck") && !n.Contains("Bridge Floor");
    }

    private static float FloorY(GameObject root, Vector3 near)
    {
        float top = float.MinValue;
        foreach (RaycastHit h in Physics.RaycastAll(near + Vector3.up * 5f, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore))
            if (h.collider is MeshCollider mc && tileColliders.Contains(mc) && h.collider.transform.IsChildOf(root.transform))
                top = Mathf.Max(top, h.point.y);
        return top > float.MinValue ? top : 0f;
    }

    private static Bounds FloorBounds(MapSelector.Layout layout)
    {
        Bounds b = new Bounds(layout.root.transform.position, Vector3.zero);
        foreach (Collider c in layout.root.GetComponentsInChildren<Collider>())
            if (!c.isTrigger && c.GetComponentInParent<BaseStation>() == null)
                b.Encapsulate(c.bounds);
        return b;
    }

    // Open floor area in square units, sampled on a grid.
    private static float FloorArea(MapSelector selector, MapSelector.Layout layout)
    {
        Solo(selector, layout);
        Bounds area = FloorBounds(layout);
        int open = 0;
        for (float x = area.min.x; x <= area.max.x; x += Spacing)
            for (float z = area.min.z; z <= area.max.z; z += Spacing)
                if (IsOpenFloor(layout, new Vector3(x, 0f, z), 0.2f, out _))
                    open++;
        return open * Spacing * Spacing;
    }

    private static void Solo(MapSelector selector, MapSelector.Layout layout)
    {
        foreach (var l in selector.layouts)
            l.root.SetActive(l == layout);
        Physics.SyncTransforms();
    }
}

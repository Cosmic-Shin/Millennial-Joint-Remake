using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Gives every station a thin cartoon outline, matching the chefs. Station meshes aren't readable in builds,
// so the smoothed outline meshes are baked here into one asset. Re-run after adding or rebuilding stations.
public static class StationOutlines
{
    const string ChildName = "StationOutline";
    const string MeshAsset = "Assets/Data/StationOutlineMeshes.asset";
    const string MaterialPath = "Assets/Material/StationOutline.mat";

    // Thin parts whose outline swallows them (fry basket handles, the fryer antenna).
    static readonly string[] TooThin = { "frydropper", "fryer_SpaceBits" };

    [MenuItem("Tools/Bake Station Outlines")]
    public static void Bake()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/PlayerOutline.mat"));
            material.SetFloat("_Width", 0.03f);
            material.SetFloat("_ScreenWidth", 1f);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        // Old outlines and their meshes go first, so re-running replaces instead of piling up.
        foreach (BaseStation station in Object.FindObjectsByType<BaseStation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            foreach (Transform t in station.GetComponentsInChildren<Transform>(true))
                if (t != null && t.name == ChildName)
                    Object.DestroyImmediate(t.gameObject);
        AssetDatabase.DeleteAsset(MeshAsset);

        // Some station models aren't readable; open them up for the bake and close them again after,
        // since the baked copies carry their own data.
        var opened = new List<ModelImporter>();
        foreach (BaseStation station in Object.FindObjectsByType<BaseStation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            foreach (MeshFilter f in station.GetComponentsInChildren<MeshFilter>(true))
                if (f.sharedMesh != null && !f.sharedMesh.isReadable
                    && AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(f.sharedMesh)) is ModelImporter importer && !opened.Contains(importer))
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                    opened.Add(importer);
                }

        try
        {
            BakeAll(material);
        }
        finally
        {
            foreach (ModelImporter importer in opened)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }
    }

    private static void BakeAll(Material material)
    {
        var baked = new Dictionary<string, Mesh>();
        Mesh first = null;
        int count = 0;
        foreach (BaseStation station in Object.FindObjectsByType<BaseStation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            foreach (MeshRenderer source in station.GetComponentsInChildren<MeshRenderer>(true))
            {
                MeshFilter filter = source.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null || !source.enabled || source.name == "HighlightOutline" // hidden old copies: no ghost outlines
                    || System.Array.Exists(TooThin, n => source.name.StartsWith(n)))
                    continue;

                Material[] mats = source.sharedMaterials;
                string key = filter.sharedMesh.GetInstanceID() + ":";
                bool anySolid = false;
                for (int s = 0; s < filter.sharedMesh.subMeshCount; s++)
                {
                    bool see = s < mats.Length && SeeThrough(mats[s]);
                    key += see ? "0" : "1";
                    anySolid |= !see;
                }
                if (!anySolid)
                    continue;

                if (!baked.TryGetValue(key, out Mesh mesh))
                {
                    mesh = Object.Instantiate(filter.sharedMesh);
                    mesh.name = filter.sharedMesh.name + "_Outline";
                    for (int s = 0; s < mesh.subMeshCount; s++)
                        if (key[key.IndexOf(':') + 1 + s] == '0')
                            mesh.SetTriangles(new int[0], s); // glass and glows: an outline would show through them
                    PlayerOutline.SmoothNormals(mesh);
                    if (first == null) { AssetDatabase.CreateAsset(mesh, MeshAsset); first = mesh; }
                    else AssetDatabase.AddObjectToAsset(mesh, first);
                    baked[key] = mesh;
                }

                var go = new GameObject(ChildName) { layer = source.gameObject.layer, isStatic = source.gameObject.isStatic };
                go.transform.SetParent(source.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var outline = go.AddComponent<MeshRenderer>();
                var outlineMats = new Material[mesh.subMeshCount];
                for (int i = 0; i < outlineMats.Length; i++) outlineMats[i] = material;
                outline.sharedMaterials = outlineMats;
                outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                outline.receiveShadows = false;
                count++;
            }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"StationOutlines: {count} outlines from {baked.Count} meshes");
    }

    private static bool SeeThrough(Material m)
    {
        if (m == null) return true;
        string n = m.name.ToLower();
        return m.renderQueue >= 2450 || n.Contains("glass") || n.Contains("glow") || n.Contains("flame") || n.Contains("beam");
    }
}

using UnityEngine;

public class PlayerOutline : MonoBehaviour
{
    public Material outlineMaterial;
    [ColorUsage(false, true)] public Color highlightColor = new Color(1.6f, 1.25f, 0.45f);
    public float highlightWidth = 0.045f;

    private void Start()
    {
        if (VisualFx.HighlightOutlineMaterial == null && outlineMaterial != null)
        {
            var highlight = new Material(outlineMaterial) { name = "StationHighlightOutline" };
            highlight.SetColor("_Color", highlightColor);
            highlight.SetFloat("_Width", highlightWidth);
            highlight.SetFloat("_ScreenWidth", 0f);
            VisualFx.HighlightOutlineMaterial = highlight;
        }

        AddTo(gameObject);
    }

    private static Material shared;

    // The outline material lives on the players; the menu chefs and select previews borrow it.
    private static Material Shared
    {
        get
        {
            if (shared == null)
            {
                PlayerOutline any = FindFirstObjectByType<PlayerOutline>(FindObjectsInactive.Include);
                shared = any != null ? any.outlineMaterial : null;
            }
            return shared;
        }
    }

    // Outlines every body part and cosmetic under the model. Each outline is a child of its part,
    // so cosmetics that switch a part off hide its outline too. Safe to call again on the same model.
    public static void AddTo(GameObject model)
    {
        Material outlineMaterial = Shared;
        if (outlineMaterial == null)
            return;

        foreach (SkinnedMeshRenderer source in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (source.name == "Outline" || source.sharedMesh == null || source.transform.Find("Outline") != null || IsFacePart(source.name))
                continue;

            var go = new GameObject("Outline") { layer = source.gameObject.layer }; // menu/preview cameras only see their layer
            go.transform.SetParent(source.transform, false);

            var outline = go.AddComponent<SkinnedMeshRenderer>();
            outline.sharedMesh = WithSmoothNormals(source.sharedMesh, source.sharedMaterials, source.name);
            outline.bones = source.bones;
            outline.rootBone = source.rootBone;
            outline.localBounds = source.localBounds;
            outline.updateWhenOffscreen = source.updateWhenOffscreen;
            outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outline.receiveShadows = false;

            var mats = new Material[source.sharedMesh.subMeshCount];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = outlineMaterial;
            outline.sharedMaterials = mats;
        }
    }

    private static readonly System.Collections.Generic.Dictionary<Mesh, Mesh> smoothed = new System.Collections.Generic.Dictionary<Mesh, Mesh>();

    // Small features on the face would get dark blobs drawn over the face, so they stay unoutlined.
    // Watches too: they sit inside the arm's outline, and their own reads as a thick band.
    private static readonly string[] FaceParts = { "FacialHair", "GooglyEyes", "ClownNose", "Glasses", "Watch_" };
    // The old single-mesh chefs (Idle_C / Idle_J) keep beard, eyes and hair in one "hair" section.
    private static readonly string[] FaceMaterials = { "c_hair", "j_hair" };

    // Prints and small details sit on top of a cosmetic; their own outlines would scribble over it.
    // "*" = outline only the cosmetic's base (first) material; otherwise the listed materials are skipped.
    private static readonly (string part, string[] skip)[] DetailRules =
    {
        ("Top_AtomTee", new[] { "*" }),
        ("Top_EMCheese", new[] { "*" }),
        ("Top_GraphicTee", new[] { "*" }),
        ("Top_PeriodicTee", new[] { "*" }),
        ("Top_LabCoat", new[] { "*" }),
        ("Top_Layered", new[] { "top_print_cream", "top_print_orange" }),
        ("Top_BurgerCostume", new[] { "food_sesame" }),
        ("Hat_BurgerHelmet", new[] { "food_sesame", "food_cheese", "food_lettuce", "food_patty" }),
        ("BowTie_Planets", new[] { "*" }),
    };

    private static bool IsFacePart(string partName) =>
        System.Array.Exists(FaceParts, f => partName.Contains(f));

    private static bool SkipSection(string partName, int index, Material mat)
    {
        if (mat == null)
            return false;
        if (System.Array.IndexOf(FaceMaterials, mat.name) >= 0)
            return true;
        foreach (var (part, skip) in DetailRules)
            if (partName.Contains(part))
                return skip[0] == "*" ? index > 0 : System.Array.IndexOf(skip, mat.name) >= 0;
        return false;
    }

    private static Mesh WithSmoothNormals(Mesh mesh, Material[] materials, string partName)
    {
        if (smoothed.TryGetValue(mesh, out Mesh cached) && cached != null)
            return cached;

        var copy = Instantiate(mesh);
        smoothed[mesh] = copy;
        for (int s = 0; s < copy.subMeshCount && s < materials.Length; s++)
            if (SkipSection(partName, s, materials[s]))
                copy.SetTriangles(new int[0], s); // no outline for this section
        var vertices = copy.vertices;
        var normals = copy.normals;

        var sums = new System.Collections.Generic.Dictionary<Vector3, Vector3>();
        for (int i = 0; i < vertices.Length; i++)
        {
            sums.TryGetValue(vertices[i], out var sum);
            sums[vertices[i]] = sum + normals[i];
        }
        for (int i = 0; i < vertices.Length; i++)
            normals[i] = sums[vertices[i]].normalized;

        copy.normals = normals;
        return copy;
    }
}

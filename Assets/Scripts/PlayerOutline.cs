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
            VisualFx.HighlightOutlineMaterial = highlight;
        }

        var source = GetComponentInChildren<SkinnedMeshRenderer>();
        if (source == null || outlineMaterial == null)
            return;

        var go = new GameObject("Outline");
        go.transform.SetParent(source.transform, false);

        var outline = go.AddComponent<SkinnedMeshRenderer>();
        outline.sharedMesh = WithSmoothNormals(source.sharedMesh);
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

    private static Mesh WithSmoothNormals(Mesh mesh)
    {
        var copy = Instantiate(mesh);
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

using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class StaminaRing : MonoBehaviour
{
    public PlayerControl player;
    public float innerRadius = 0.78f;
    public float outerRadius = 1.05f;
    public int segments = 64;

    [Header("Colours (HDR so Bloom picks them up)")]
    public Color exhaustedColor = new Color(0.45f, 0.45f, 0.5f, 0.7f);
    public Color unlimitedColor = new Color(2.4f, 1.8f, 0.4f, 1f);
    [Tooltip("Below this much stamina the ring pulses")]
    public float lowStamina = 0.25f;

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private MeshRenderer ringRenderer;
    private MaterialPropertyBlock block;
    private Color baseColor;
    private float shownFill = -1f;

    private void Awake()
    {
        ringRenderer = GetComponent<MeshRenderer>();
        baseColor = ringRenderer.sharedMaterial.GetColor("_BaseColor");
        block = new MaterialPropertyBlock();

        mesh = new Mesh { name = "Stamina Ring" };
        mesh.MarkDynamic();
        vertices = new Vector3[(segments + 1) * 2];
        triangles = new int[segments * 6];
        for (int i = 0; i < segments; i++)
        {
            int a = i * 2;
            triangles[i * 6 + 0] = a;
            triangles[i * 6 + 1] = a + 1;
            triangles[i * 6 + 2] = a + 2;
            triangles[i * 6 + 3] = a + 2;
            triangles[i * 6 + 4] = a + 1;
            triangles[i * 6 + 5] = a + 3;
        }
        mesh.vertices = vertices;
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        bool unlimited = player.HasUnlimitedStamina;
        float fill = unlimited ? 1f : Mathf.Clamp01(player.StaminaFraction);

        if (Mathf.Abs(fill - shownFill) > 0.002f)
            BuildArc(fill);

        Color color = baseColor;
        if (unlimited)
            color = unlimitedColor * (0.85f + 0.15f * Mathf.Sin(Time.time * 6f));
        else if (player.IsExhausted)
            color = exhaustedColor;
        else if (fill < lowStamina)
            color = Color.Lerp(baseColor, baseColor * 1.8f, 0.5f + 0.5f * Mathf.Sin(Time.time * 14f));

        block.SetColor("_BaseColor", color);
        ringRenderer.SetPropertyBlock(block);
    }

    private void BuildArc(float fill)
    {
        shownFill = fill;
        int used = Mathf.Max(1, Mathf.CeilToInt(fill * segments));
        float sweep = fill * Mathf.PI * 2f;
        float start = -Mathf.PI / 2f - sweep / 2f;

        for (int i = 0; i <= used; i++)
        {
            float angle = start + sweep * i / used;
            var dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            vertices[i * 2] = dir * innerRadius;
            vertices[i * 2 + 1] = dir * outerRadius;
        }

        mesh.vertices = vertices;
        mesh.SetTriangles(triangles, 0, fill <= 0.001f ? 0 : used * 6, 0);
        mesh.RecalculateBounds();
    }
}

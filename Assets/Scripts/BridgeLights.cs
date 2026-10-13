using UnityEngine;

public class BridgeLights : MonoBehaviour
{
    public Renderer[] lights;
    [Tooltip("Soft additive glow under each light, same order as lights (optional)")]
    public Renderer[] halos;
    [ColorUsage(false, true)] public Color glowColor = new Color(1.4f, 4.9f, 7f);
    public Color haloColor = new Color(0.1f, 0.7f, 1f, 1f);
    public float speed = 6f;
    [Tooltip("Lights per wave")]
    public float spacing = 6f;
    [Range(0f, 1f)] public float dimLevel = 0.1f;

    private MaterialPropertyBlock block;

    private void Update()
    {
        block ??= new MaterialPropertyBlock();

        for (int i = 0; i < lights.Length; i++)
        {
            float behind = Mathf.Repeat(Time.time * speed - i, spacing) / spacing;
            float glow = Mathf.Lerp(dimLevel, 1f, Mathf.Pow(1f - behind, 4f));
            block.SetColor("_BaseColor", glowColor * glow);
            lights[i].SetPropertyBlock(block);

            if (halos != null && i < halos.Length && halos[i] != null)
            {
                block.SetColor("_BaseColor", haloColor * glow);
                halos[i].SetPropertyBlock(block);
            }
        }
    }
}

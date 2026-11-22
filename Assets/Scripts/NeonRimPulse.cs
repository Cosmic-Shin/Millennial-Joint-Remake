using System.Collections.Generic;
using UnityEngine;

// Makes the platforms' neon edge strips glow in their own colour and pulse slowly,
// so the edges read as "don't fall off here". Per-renderer overrides; the shared materials stay as they are.
public class NeonRimPulse : MonoBehaviour
{
    [Tooltip("Glow strength at the dim and bright ends of the pulse (too high washes the colour out to white)")]
    public float minGlow = 0.9f;
    public float maxGlow = 2.2f;
    public float speed = 1.6f;

    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    private readonly List<(Renderer renderer, Color hue, float phase)> rims = new List<(Renderer, Color, float)>();
    private MaterialPropertyBlock block;

    private void Start()
    {
        block = new MaterialPropertyBlock();
        foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!r.name.Contains("NeonRim") || r.sharedMaterial == null || !r.sharedMaterial.HasProperty(EmissionId))
                continue;
            Color e = r.sharedMaterial.GetColor(EmissionId);
            float max = Mathf.Max(e.r, Mathf.Max(e.g, e.b));
            if (max <= 0f)
                continue;
            // Phase from position, so the pulse ripples across the map instead of blinking all at once.
            Vector3 p = r.bounds.center;
            rims.Add((r, e / max, (p.x + p.z) * 0.15f));
        }
    }

    private void Update()
    {
        foreach (var (r, hue, phase) in rims)
        {
            if (r == null || !r.isVisible)
                continue;
            float k = Mathf.Lerp(minGlow, maxGlow, 0.5f + 0.5f * Mathf.Sin(Time.time * speed + phase));
            r.GetPropertyBlock(block);
            block.SetColor(EmissionId, hue * k);
            r.SetPropertyBlock(block);
        }
    }
}

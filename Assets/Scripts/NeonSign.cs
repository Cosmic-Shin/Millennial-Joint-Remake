using System.Collections.Generic;
using UnityEngine;

// Animates the Big Bang Burgers signs: pieces light up one by one, hold, blink twice, go dark, repeat,
// with the odd random flicker like a buzzing neon tube. Per-renderer overrides; materials stay as they are.
public class NeonSign : MonoBehaviour
{
    [Tooltip("Pieces light up in this order (names after the BBB_ prefix)")]
    public string[] order = { "Border", "Burst", "Planet", "Stars", "TextBigBang", "TextBurgers", "BurgerBuns", "BurgerPatty", "BurgerLettuce", "Seeds" };
    public float stepTime = 0.22f;
    public float holdTime = 5f;
    public float onGlow = 2f;
    public float offGlow = 0.08f;
    [Tooltip("Chance per second that one piece flickers")]
    public float flickerChance = 0.6f;

    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    private readonly List<(Renderer renderer, Color hue, int step)> pieces = new List<(Renderer, Color, int)>();
    private MaterialPropertyBlock block;
    private Renderer flickering;
    private float flickerUntil;

    private void Start()
    {
        block = new MaterialPropertyBlock();
        foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!r.name.StartsWith("BBB_") || r.sharedMaterial == null || !r.sharedMaterial.HasProperty(EmissionId))
                continue;
            int step = System.Array.IndexOf(order, r.name.Substring(4));
            Color e = r.sharedMaterial.GetColor(EmissionId);
            float max = Mathf.Max(e.r, Mathf.Max(e.g, e.b));
            if (step < 0 || max <= 0f)
                continue; // thrusters and anything unlisted keep their own glow
            pieces.Add((r, e / max, step));
        }
    }

    private void Update()
    {
        float lightUp = order.Length * stepTime;
        float blink = 0.8f, dark = 0.5f;
        float t = Time.time % (lightUp + holdTime + blink + dark);

        if (flickering == null && pieces.Count > 0 && Random.value < flickerChance * Time.deltaTime)
        {
            flickering = pieces[Random.Range(0, pieces.Count)].renderer;
            flickerUntil = Time.time + Random.Range(0.05f, 0.15f);
        }
        if (flickering != null && Time.time > flickerUntil)
            flickering = null;

        foreach (var (r, hue, step) in pieces)
        {
            if (r == null || !r.isVisible)
                continue;

            bool on;
            if (t < lightUp) on = t >= step * stepTime;
            else if (t < lightUp + holdTime) on = true;
            else if (t < lightUp + holdTime + blink) on = (int)((t - lightUp - holdTime) / 0.2f) % 2 == 1;
            else on = false;
            if (r == flickering) on = !on;

            r.GetPropertyBlock(block);
            block.SetColor(EmissionId, hue * (on ? onGlow : offGlow));
            r.SetPropertyBlock(block);
        }
    }
}

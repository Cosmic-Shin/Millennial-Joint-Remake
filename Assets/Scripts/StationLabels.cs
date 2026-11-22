using TMPro;
using UnityEngine;

// Floating name tags over the stations players fetch from, so they're easy to find at a glance.
public class StationLabels : MonoBehaviour
{
    public TMP_FontAsset font;
    [Tooltip("Optional outlined font material (e.g. the HUD Outline one)")]
    public Material fontMaterial;
    public float fontSize = 5.5f;
    [Tooltip("Stations of the same kind closer than this share one tag")]
    public float mergeDistance = 2.5f;
    public float heightAboveStation = 0.45f;
    public Color color = new Color(1f, 0.92f, 0.6f);

    private void Start()
    {
        Add<IngredientBox>("Ingredients");
        Add<PlateCounter>("Plates");
        Add<CupTable>("Cups");
        Add<TrashBin>("Trash");
        Add<DonutGlazeStation>("Donut Glaze");
    }

    private void Add<T>(string text) where T : BaseStation
    {
        var placed = new System.Collections.Generic.List<Vector3>();
        foreach (T station in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Bounds b = new Bounds(station.transform.position, Vector3.zero);
            foreach (Renderer r in station.GetComponentsInChildren<Renderer>(true))
                if (!(r is ParticleSystemRenderer))
                    b.Encapsulate(r.bounds);

            // Side-by-side stations of the same kind share one tag.
            if (placed.Exists(p => Vector3.Distance(p, b.center) < mergeDistance))
                continue;
            placed.Add(b.center);

            var go = new GameObject("Label");
            go.transform.SetParent(station.transform, true);
            go.transform.position = new Vector3(b.center.x, b.max.y + heightAboveStation, b.center.z);
            go.transform.localScale = Vector3.one; // stay readable under scaled stations
            Vector3 s = go.transform.lossyScale;
            go.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);

            var label = go.AddComponent<TextMeshPro>();
            label.text = text;
            label.font = font;
            if (fontMaterial != null)
                label.fontSharedMaterial = fontMaterial;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(6f, 1.5f);

            go.AddComponent<Billboard>();
            go.AddComponent<Bob>();
        }
    }

    // Gentle float so the tag reads as a sign, not part of the furniture.
    private class Bob : MonoBehaviour
    {
        private Vector3 home;
        private float phase;

        private void Start()
        {
            home = transform.localPosition;
            phase = Random.value * 6f;
        }

        private void Update() =>
            transform.localPosition = home + transform.parent.InverseTransformVector(Vector3.up) * Mathf.Sin(Time.time * 2f + phase) * 0.08f;
    }
}

using System.Collections;
using UnityEngine;

// Bridge Islands: every so often the bridge cracks, shatters and is gone for a few seconds.
public class ShatteringBridge : MonoBehaviour
{
    public Collider walkSurface;
    public Renderer[] visuals;
    public Vector2 intactSeconds = new Vector2(14f, 22f);
    public float warningSeconds = 2.5f;
    public float brokenSeconds = 6f;
    public Color warningTint = new Color(1f, 0.25f, 0.2f);

    private void OnEnable() => StartCoroutine(Cycle());

    private void OnDisable()
    {
        StopAllCoroutines();
        SetIntact(true); // never leave a broken bridge behind when the map changes
    }

    private IEnumerator Cycle()
    {
        while (true)
        {
            yield return Gameplay(Random.Range(intactSeconds.x, intactSeconds.y));

            // Warning: deck shakes and flashes red, harder as it's about to go.
            Vector3[] rest = new Vector3[visuals.Length];
            for (int i = 0; i < visuals.Length; i++)
                rest[i] = visuals[i].transform.localPosition;
            for (float e = 0f; e < warningSeconds; e += PlayerControl.GameplayActive ? Time.deltaTime : 0f)
            {
                float k = e / warningSeconds;
                for (int i = 0; i < visuals.Length; i++)
                {
                    visuals[i].transform.localPosition = rest[i] + Random.insideUnitSphere * 0.04f * (0.3f + k);
                    VisualFx.Tint(visuals[i].gameObject, warningTint, (0.3f + 0.5f * k) * (0.5f + 0.5f * Mathf.Sin(Time.time * (8f + 16f * k))));
                }
                yield return null;
            }
            for (int i = 0; i < visuals.Length; i++)
            {
                visuals[i].transform.localPosition = rest[i];
                VisualFx.ClearTint(visuals[i].gameObject);
            }

            Shatter();
            SetIntact(false);
            yield return Gameplay(brokenSeconds);

            SetIntact(true);
            foreach (Renderer r in visuals)
            {
                VisualFx.Puff(r.bounds.center, new Color(0.4f, 0.9f, 1f, 0.9f), 16, 0.3f, 1.5f);
                StartCoroutine(VisualFx.Pop(r.transform, 0.3f, 0.35f));
            }
        }
    }

    // Waits gameplay time only: pausing or the end screen doesn't advance the bridge.
    private static IEnumerator Gameplay(float seconds)
    {
        for (float e = 0f; e < seconds; e += PlayerControl.GameplayActive ? Time.deltaTime : 0f)
            yield return null;
    }

    private void SetIntact(bool intact)
    {
        if (walkSurface != null)
            walkSurface.enabled = intact;
        foreach (Renderer r in visuals)
            if (r != null)
                r.enabled = intact;
    }

    private void Shatter()
    {
        CameraMovement.Instance?.Shake(0.35f, 0.4f);
        AudioManager.Instance?.PlayNopeSFX();

        foreach (Renderer r in visuals)
        {
            Bounds b = r.bounds;
            Material mat = r.sharedMaterial;
            VisualFx.Puff(b.center, new Color(0.8f, 0.85f, 0.95f, 0.9f), 24, 0.4f, 2.5f);

            // Chunks tumble down into space (no colliders: they never hit players).
            for (int i = 0; i < 14; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(shard.GetComponent<Collider>());
                shard.GetComponent<Renderer>().sharedMaterial = mat;
                shard.transform.position = new Vector3(Random.Range(b.min.x, b.max.x), b.max.y, Random.Range(b.min.z, b.max.z));
                shard.transform.rotation = Random.rotation;
                shard.transform.localScale = Vector3.one * Random.Range(0.25f, 0.6f);
                var body = shard.AddComponent<Rigidbody>();
                body.linearVelocity = new Vector3(Random.Range(-2f, 2f), Random.Range(1f, 4f), Random.Range(-2f, 2f));
                body.angularVelocity = Random.insideUnitSphere * 8f;
                Destroy(shard, 2.5f);
            }
        }
    }
}

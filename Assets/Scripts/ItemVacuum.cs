using System.Collections;
using UnityEngine;

// Ring Station: the planet in the middle swallows any food that gets too close.
public class ItemVacuum : MonoBehaviour
{
    [Tooltip("Food inside this radius gets pulled in")]
    public float radius = 5.5f;
    [Tooltip("Pull acceleration at the edge; grows towards the centre")]
    public float strength = 18f;
    [Tooltip("Food this close is swallowed")]
    public float swallowRadius = 1.2f;
    public Color swirlColor = new Color(0.75f, 0.45f, 1f, 0.9f);

    private float nextSwirl;

    private void FixedUpdate()
    {
        if (!PlayerControl.GameplayActive)
            return;

        Vector3 centre = transform.position;
        for (int i = DroppedItem.All.Count - 1; i >= 0; i--)
        {
            DroppedItem item = DroppedItem.All[i];
            Vector3 to = centre - item.transform.position;
            to.y *= 0.5f; // mostly a sideways pull, so food skims in over the rim
            float d = to.magnitude;
            if (d > radius)
                continue;

            if (d < swallowRadius)
            {
                StartCoroutine(Swallow(item));
                continue;
            }

            if (item.TryGetComponent(out Rigidbody body))
            {
                float k = 1f - d / radius;
                body.AddForce(to / d * strength * (0.4f + 2.6f * k * k), ForceMode.Acceleration);
            }
        }
    }

    private void Update()
    {
        // Faint swirl so players learn the middle is dangerous.
        if (PlayerControl.GameplayActive && Time.time >= nextSwirl)
        {
            nextSwirl = Time.time + 0.25f;
            Vector3 p = transform.position + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(1.5f, radius);
            VisualFx.Puff(p, swirlColor, 2, 0.15f, 0.4f);
        }
    }

    private IEnumerator Swallow(DroppedItem item)
    {
        DroppedItem.All.Remove(item); // stop pulling it while it shrinks
        Transform t = item.transform;
        if (item.TryGetComponent(out Rigidbody body))
            body.isKinematic = true;
        foreach (Collider c in item.GetComponentsInChildren<Collider>())
            c.enabled = false;

        Vector3 start = t.position, scale = t.localScale;
        for (float e = 0f; e < 0.35f && t != null; e += Time.deltaTime)
        {
            float k = e / 0.35f;
            t.position = Vector3.Lerp(start, transform.position, k * k);
            t.localScale = scale * (1f - k);
            t.Rotate(0f, 900f * Time.deltaTime, 0f);
            yield return null;
        }

        if (t != null)
        {
            VisualFx.Puff(transform.position, swirlColor, 14, 0.3f, 2.5f);
            AudioManager.Instance?.PlayNopeSFX();
            Destroy(item.gameObject);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

// Ping-pongs between its start and start + localOffset (in parent space), pausing at each dock.
// Rigidbodies standing in its rider zones (players, dropped food, power-ups) are carried along.
public class MovingPlatform : MonoBehaviour
{
    public Vector3 localOffset = new Vector3(-3.6f, 0f, 0f);
    public float travelTime = 2.5f;
    public float waitTime = 1.5f;
    [Tooltip("Trigger boxes over the deck; whatever is inside them rides along")]
    public BoxCollider[] riderZones;

    private Vector3 start;
    private float time;
    private Rigidbody body;
    private readonly HashSet<Rigidbody> riders = new HashSet<Rigidbody>();

    private void Awake()
    {
        start = transform.localPosition;
        body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void OnDisable()
    {
        time = 0f;
        transform.localPosition = start;
    }

    // 0 at the start dock, 1 at the far dock.
    public static float Progress(float t, float travel, float wait)
    {
        float p = t % (2f * (travel + wait));
        float k = p < wait ? 0f
            : p < wait + travel ? (p - wait) / travel
            : p < 2f * wait + travel ? 1f
            : 1f - (p - 2f * wait - travel) / travel;
        return Mathf.SmoothStep(0f, 1f, k);
    }

    private void FixedUpdate()
    {
        // Re-check who is aboard every step: robust to pads, hops and launched items coming and going.
        riders.Clear();
        if (riderZones != null)
            foreach (BoxCollider zone in riderZones)
            {
                if (zone == null)
                    continue;
                Vector3 halfExtents = Vector3.Scale(zone.size, zone.transform.lossyScale) * 0.5f;
                foreach (Collider c in Physics.OverlapBox(zone.transform.TransformPoint(zone.center), halfExtents, zone.transform.rotation, ~0, QueryTriggerInteraction.Ignore))
                {
                    Rigidbody r = c.attachedRigidbody;
                    if (r != null && r != body && !r.isKinematic)
                        riders.Add(r);
                }
            }

        time += Time.fixedDeltaTime;
        Vector3 local = start + localOffset * Progress(time, travelTime, waitTime);
        Vector3 target = transform.parent != null ? transform.parent.TransformPoint(local) : local;
        Vector3 delta = target - body.position;
        body.MovePosition(target);

        foreach (Rigidbody rider in riders)
            rider.position += delta;
    }
}

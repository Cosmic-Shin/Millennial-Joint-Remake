using System.Collections.Generic;
using UnityEngine;

// Ping-pongs between its start and start + localOffset (in parent space), pausing at each dock.
// Rigidbodies standing on it (players, dropped food) are carried along.
public class MovingPlatform : MonoBehaviour
{
    public Vector3 localOffset = new Vector3(-3.6f, 0f, 0f);
    public float travelTime = 2.5f;
    public float waitTime = 1.5f;

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
        riders.Clear();
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
        time += Time.fixedDeltaTime;
        Vector3 local = start + localOffset * Progress(time, travelTime, waitTime);
        Vector3 target = transform.parent != null ? transform.parent.TransformPoint(local) : local;
        Vector3 delta = target - body.position;
        body.MovePosition(target);

        riders.RemoveWhere(r => r == null);
        foreach (Rigidbody rider in riders)
            rider.position += delta;
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody r = other.attachedRigidbody;
        if (r != null && !r.isKinematic && r != body)
            riders.Add(r);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody != null)
            riders.Remove(other.attachedRigidbody);
    }
}

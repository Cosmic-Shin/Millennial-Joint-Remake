using UnityEngine;

// Players use heavy linear damping for snappy movement, which also makes them fall in slow motion.
// Drop the damping while nothing is underneath so gaps and edges make them fall properly.
[RequireComponent(typeof(Rigidbody))]
public class FallOffEdges : MonoBehaviour
{
    [Tooltip("Ray length from the player's pivot down to the floor")]
    public float groundCheck = 1.5f;

    private Rigidbody body;
    private float damping;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        damping = body.linearDamping;
    }

    private void FixedUpdate()
    {
        bool grounded = Physics.Raycast(transform.position, Vector3.down, groundCheck, ~0, QueryTriggerInteraction.Ignore);
        body.linearDamping = grounded ? damping : 0f;
    }
}

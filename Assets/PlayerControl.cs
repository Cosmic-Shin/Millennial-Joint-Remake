using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControl : MonoBehaviour
{
    [Header("Movement")]
    public bool doMove = true;
    public float speed = 6f;
    public float runMultiplier = 1.5f;

    private Rigidbody rb;
    private Vector3 direction;
    private bool front, left, back, right;

    [Header("Keys")]
    public KeyCode MoveUp = KeyCode.W;
    public KeyCode MoveLeft = KeyCode.A;
    public KeyCode MoveDown = KeyCode.S;
    public KeyCode MoveRight = KeyCode.D;
    public KeyCode PrimaryAction = KeyCode.F;
    public KeyCode Run = KeyCode.LeftShift;
    public KeyCode DropItem = KeyCode.Q;

    [Header("Interaction")]
    [SerializeField] private float castRadius = 1.2f;
    [SerializeField] private LayerMask hitMask;

    [Header("Held Item")]
    public KitchenItemData heldItem = new KitchenItemData();

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (rb == null) return;

        front = Input.GetKey(MoveUp);
        left = Input.GetKey(MoveLeft);
        back = Input.GetKey(MoveDown);
        right = Input.GetKey(MoveRight);

        if (Input.GetKeyDown(PrimaryAction))
        {
            IInteractable interactable = GetNearestInteractable();
            if (interactable != null)
            {
                Debug.Log("Interacting with: " + ((MonoBehaviour)interactable).name + "\n" + GetHeldItemDebug());
                interactable.Interact(this);
            }
            else
            {
                Debug.Log("No interactable nearby\n" + GetHeldItemDebug());
            }
        }

        if (Input.GetKeyDown(DropItem))
        {
            if (!heldItem.IsEmpty)
            {
                Debug.Log("Dropped: " + heldItem.GetDisplayName());
                heldItem.Clear();
                Debug.Log(GetHeldItemDebug());
            }
            else
            {
                Debug.Log("Nothing to drop\n" + GetHeldItemDebug());
            }
        }
    }

    private void FixedUpdate()
    {
        if (!doMove || rb == null) return;

        direction = new Vector3(
            (right ? 1 : 0) - (left ? 1 : 0),
            0,
            (front ? 1 : 0) - (back ? 1 : 0)
        );

        if (direction != Vector3.zero)
        {
            float currentSpeed = Input.GetKey(Run) ? speed * runMultiplier : speed;
            rb.AddForce(direction.normalized * currentSpeed * 10f);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                speed / 2f * Time.deltaTime
            );
        }
    }

    private IInteractable GetNearestInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, castRadius, hitMask);

        IInteractable nearestInteractable = null;
        float nearestDist = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            IInteractable interactable = hit.GetComponent<IInteractable>();
            if (interactable == null)
                interactable = hit.GetComponentInParent<IInteractable>();

            if (interactable == null) continue;

            float dist = Vector3.Distance(transform.position, hit.ClosestPoint(transform.position));
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestInteractable = interactable;
            }
        }

        return nearestInteractable;
    }

    public string GetHeldItemDebug()
    {
        if (heldItem == null || heldItem.IsEmpty)
            return "Holding: Nothing";

        return "Holding: " + heldItem.GetDisplayName();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, castRadius);
    }
}
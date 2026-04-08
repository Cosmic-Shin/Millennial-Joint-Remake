using UnityEngine;
using UnityEngine.UI;

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
    public KeyCode MoveUp        = KeyCode.W;
    public KeyCode MoveLeft      = KeyCode.A;
    public KeyCode MoveDown      = KeyCode.S;
    public KeyCode MoveRight     = KeyCode.D;
    public KeyCode PrimaryAction = KeyCode.F;
    public KeyCode Run           = KeyCode.LeftShift;
    public KeyCode DropItem      = KeyCode.Q;

    [Header("Interaction")]
    [SerializeField] private float castRadius = 1.2f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Held Item")]
    public KitchenItemData heldItem = new KitchenItemData();
    public Text heldItemText;

    private BaseStation lastOpenStation;

    private void OnValidate()
    {
        if (hitMask.value == 0)
            hitMask = ~0;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Force-clear at runtime so stale Inspector-serialized data never causes issues.
        // This is the fix for heldItem appearing set in the Inspector but not updating at runtime.
        heldItem.Clear();

        if (hitMask.value == 0)
            hitMask = ~0;

        UpdateHeldItemHUD();
    }

    private void Update()
    {
        if (rb == null) return;

        front = Input.GetKey(MoveUp);
        left  = Input.GetKey(MoveLeft);
        back  = Input.GetKey(MoveDown);
        right = Input.GetKey(MoveRight);

        // ---------------------------------------------------------------
        // Interact
        // ---------------------------------------------------------------
        if (Input.GetKeyDown(PrimaryAction))
        {
            Debug.Log("PLAYER INSTANCE ID: " + GetInstanceID());

            IInteractable interactable = GetNearestValidInteractable();
            if (interactable != null)
            {
                MonoBehaviour mb = interactable as MonoBehaviour;
                Debug.Log("Interacting with: " + mb.name
                          + " | component: " + interactable.GetType().Name
                          + " | station instanceID: " + mb.gameObject.GetInstanceID());

                BaseStation station = interactable as BaseStation;
                if (station != null)
                {
                    if (lastOpenStation != null && lastOpenStation != station)
                        lastOpenStation.OpenPanel(false);

                    station.OpenPanel(true);
                    lastOpenStation = station;
                }

                interactable.Interact(this);
                UpdateHeldItemHUD();

                Debug.Log("AFTER INTERACT — " + GetHeldItemDebug()
                          + " | player instanceID: " + GetInstanceID());

                if (station != null)
                    StartCoroutine(CloseAfterDelay(station, 0.5f));
            }
            else
            {
                if (lastOpenStation != null)
                {
                    lastOpenStation.OpenPanel(false);
                    lastOpenStation = null;
                }

                Debug.Log("No valid interactable nearby — " + GetHeldItemDebug());
            }
        }

        // ---------------------------------------------------------------
        // Drop item
        // ---------------------------------------------------------------
        if (Input.GetKeyDown(DropItem))
        {
            if (!heldItem.IsEmpty)
            {
                Debug.Log("Dropped: " + heldItem.GetDisplayName());
                heldItem.Clear();
                UpdateHeldItemHUD();
                Debug.Log(GetHeldItemDebug());
            }
            else
            {
                Debug.Log("Nothing to drop — " + GetHeldItemDebug());
            }
        }
    }

    private System.Collections.IEnumerator CloseAfterDelay(BaseStation station, float delay)
    {
        yield return new WaitForSeconds(delay);
        station.OpenPanel(false);
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

    private IInteractable GetNearestValidInteractable()
    {
        int maskValue = hitMask.value == 0 ? ~0 : hitMask.value;
        Collider[] hits = Physics.OverlapSphere(transform.position, castRadius, maskValue);

        IInteractable nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            IInteractable interactable = GetBestInteractable(hit);
            if (interactable == null) continue;
            if (!interactable.CanInteractWith(this)) continue;

            float dist = Vector3.Distance(transform.position, hit.ClosestPoint(transform.position));
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = interactable;
            }
        }

        return nearest;
    }

    private IInteractable GetBestInteractable(Collider hit)
    {
        IInteractable[] candidates = hit.GetComponents<IInteractable>();
        IInteractable first = null;

        foreach (IInteractable candidate in candidates)
        {
            if (first == null)
                first = candidate;

            if (candidate is BaseStation)
                return candidate;
        }

        if (first != null)
            return first;

        return hit.GetComponentInParent<IInteractable>();
    }

    public string GetHeldItemDebug()
    {
        if (heldItem == null || heldItem.IsEmpty)
            return "Holding: Nothing";
        return "Holding: " + heldItem.GetDisplayName();
    }

    private void UpdateHeldItemHUD()
    {
        if (heldItemText == null) return;

        if (heldItem == null || heldItem.IsEmpty)
            heldItemText.text = "Holding: Nothing";
        else
            heldItemText.text = "Holding: " + heldItem.GetDisplayName();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, castRadius);
    }
}
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PlayerControl : MonoBehaviour
{
    [Header("Player")]
    public int playerNumber = 1; // 1 or 2

    [Header("Movement")]
    public bool doMove = true;
    public float speed = 6f;
    public float runMultiplier = 1.5f;
    public float rotationSmooth = 10f;
    public float maxVelocity = 6f;

    [Header("Animation")]
    public Animator animator;
    public string isMovingParameter = "IsMoving";
    public string emote1Trigger = "Emote1";
    public string emote2Trigger = "Emote2";
    public string emote3Trigger = "Emote3";
    public string emote4Trigger = "Emote4";

    [Header("Keys")]
    public KeyCode MoveUp = KeyCode.W;
    public KeyCode MoveLeft = KeyCode.A;
    public KeyCode MoveDown = KeyCode.S;
    public KeyCode MoveRight = KeyCode.D;
    public KeyCode PrimaryAction = KeyCode.F;
    public KeyCode Run = KeyCode.LeftShift;
    public KeyCode DropItem = KeyCode.Q;

    [Header("Emote Keys - Player 1 (Top Number Row)")]
    public KeyCode p1Emote1 = KeyCode.Alpha1;
    public KeyCode p1Emote2 = KeyCode.Alpha2;
    public KeyCode p1Emote3 = KeyCode.Alpha3;
    public KeyCode p1Emote4 = KeyCode.Alpha4;

    [Header("Emote Keys - Player 2 (Right Numpad)")]
    public KeyCode p2Emote1 = KeyCode.Keypad1;
    public KeyCode p2Emote2 = KeyCode.Keypad2;
    public KeyCode p2Emote3 = KeyCode.Keypad3;
    public KeyCode p2Emote4 = KeyCode.Keypad4;

    [Header("Interaction")]
    [SerializeField] private float castRadius = 1.2f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Held Item")]
    public KitchenItemData heldItem = new KitchenItemData();
    public Text heldItemText;

    [Header("Held Item Visuals")]
    public KitchenItemVisualizer heldItemVisualizer;

    [Header("Dropped Item")]
    public float dropForwardOffset = 0.8f;
    public float dropUpOffset = 0.5f;
    public float dropUpwardVelocity = 0.25f;
    public float throwForceMin = 2f;
    public float throwForceMax = 10f;
    public float minimumThrowHoldTime = 0.5f;
    public float maximumThrowHoldTime = 3f;
    public float throwUpwardBoost = 1.5f;
    public float pickupRadius = 1.5f;
    public float dropTorque = 1.5f;
    public float droppedItemLifetime = 30f;

    private Rigidbody rb;
    private Vector3 direction;
    private bool front, left, back, right;

    private bool dropButtonHeld;
    private float dropButtonHoldTime;
    private BaseStation lastOpenStation;

    private int isMovingHash;
    private int emote1Hash;
    private int emote2Hash;
    private int emote3Hash;
    private int emote4Hash;

    private void OnValidate()
    {
        if (hitMask.value == 0)
            hitMask = ~0;
    }

    private void Awake()
    {
        isMovingHash = Animator.StringToHash(isMovingParameter);
        emote1Hash = Animator.StringToHash(emote1Trigger);
        emote2Hash = Animator.StringToHash(emote2Trigger);
        emote3Hash = Animator.StringToHash(emote3Trigger);
        emote4Hash = Animator.StringToHash(emote4Trigger);
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerNumber == 2)
        {
            MoveUp = KeyCode.UpArrow;
            MoveLeft = KeyCode.LeftArrow;
            MoveDown = KeyCode.DownArrow;
            MoveRight = KeyCode.RightArrow;
            PrimaryAction = KeyCode.Return;
            Run = KeyCode.RightShift;
            DropItem = KeyCode.RightControl;
        }

        heldItem.Clear();

        if (hitMask.value == 0)
            hitMask = ~0;

        UpdateHeldItemHUD();
        RefreshHeldItemVisual();
        UpdateAnimator();
    }

    private void Update()
    {
        if (rb == null)
            return;

        front = Input.GetKey(MoveUp);
        left = Input.GetKey(MoveLeft);
        back = Input.GetKey(MoveDown);
        right = Input.GetKey(MoveRight);

        direction = new Vector3(
            (right ? 1 : 0) - (left ? 1 : 0),
            0f,
            (front ? 1 : 0) - (back ? 1 : 0)
        );

        HandleEmotes();

        if (Input.GetKeyDown(PrimaryAction))
        {
            Debug.Log("PLAYER INSTANCE ID: " + GetInstanceID());

            IInteractable interactable = null;

            if (heldItem.IsEmpty)
                interactable = GetNearestValidDroppedItem(pickupRadius);

            if (interactable == null)
            {
                interactable = GetNearestValidInteractable(castRadius);

                if (interactable == null && heldItem.IsEmpty)
                    interactable = GetNearestValidInteractable(pickupRadius);
            }

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
                RefreshHeldItemVisual();

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

        if (Input.GetKeyDown(DropItem) && !heldItem.IsEmpty)
        {
            dropButtonHeld = true;
            dropButtonHoldTime = 0f;
        }

        if (Input.GetKey(DropItem) && dropButtonHeld)
        {
            dropButtonHoldTime += Time.deltaTime;
        }

        if (Input.GetKeyUp(DropItem) && dropButtonHeld)
        {
            if (!heldItem.IsEmpty)
            {
                bool isThrow = dropButtonHoldTime >= minimumThrowHoldTime;
                Vector3 dropPosition = transform.position + transform.forward * dropForwardOffset + Vector3.up * dropUpOffset;
                Vector3 velocity;

                if (isThrow)
                {
                    float charge = Mathf.Clamp01(
                        (dropButtonHoldTime - minimumThrowHoldTime) /
                        Mathf.Max(0.0001f, maximumThrowHoldTime - minimumThrowHoldTime)
                    );

                    float strength = Mathf.Lerp(throwForceMin, throwForceMax, charge);
                    velocity = transform.forward * strength + Vector3.up * throwUpwardBoost;

                    Debug.Log($"Thrown with charge {charge:F2}: {heldItem.GetDisplayName()}");
                }
                else
                {
                    velocity = Vector3.up * dropUpwardVelocity;
                    Debug.Log("Dropped: " + heldItem.GetDisplayName());
                }

                SpawnDroppedItem(heldItem, dropPosition, velocity);
                heldItem.Clear();
                UpdateHeldItemHUD();
                RefreshHeldItemVisual();

                Debug.Log(GetHeldItemDebug());
            }

            dropButtonHeld = false;
            dropButtonHoldTime = 0f;
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (!doMove || rb == null)
            return;

        if (direction != Vector3.zero)
        {
            float currentSpeed = Input.GetKey(Run) ? speed * runMultiplier : speed;

            rb.AddForce(direction.normalized * currentSpeed * 10f, ForceMode.Force);

            Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (flatVelocity.magnitude > maxVelocity)
            {
                flatVelocity = flatVelocity.normalized * maxVelocity;
                rb.linearVelocity = new Vector3(flatVelocity.x, rb.linearVelocity.y, flatVelocity.z);
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSmooth * Time.fixedDeltaTime
            );
        }
    }

    private void HandleEmotes()
    {
        if (animator == null)
            return;

        if (IsMoving())
            return;

        if (playerNumber == 1)
        {
            if (Input.GetKeyDown(p1Emote1))
                PlayEmote(emote1Hash);
            else if (Input.GetKeyDown(p1Emote2))
                PlayEmote(emote2Hash);
            else if (Input.GetKeyDown(p1Emote3))
                PlayEmote(emote3Hash);
            else if (Input.GetKeyDown(p1Emote4))
                PlayEmote(emote4Hash);
        }
        else if (playerNumber == 2)
        {
            if (Input.GetKeyDown(p2Emote1))
                PlayEmote(emote1Hash);
            else if (Input.GetKeyDown(p2Emote2))
                PlayEmote(emote2Hash);
            else if (Input.GetKeyDown(p2Emote3))
                PlayEmote(emote3Hash);
            else if (Input.GetKeyDown(p2Emote4))
                PlayEmote(emote4Hash);
        }
    }

    private void PlayEmote(int emoteHash)
    {
        animator.ResetTrigger(emote1Hash);
        animator.ResetTrigger(emote2Hash);
        animator.ResetTrigger(emote3Hash);
        animator.ResetTrigger(emote4Hash);

        animator.SetTrigger(emoteHash);
    }

    private bool IsMoving()
    {
        return direction.sqrMagnitude > 0.01f;
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        animator.SetBool(isMovingHash, IsMoving());
    }

    private void SpawnDroppedItem(KitchenItemData itemData, Vector3 position, Vector3 velocity)
    {
        if (itemData == null || itemData.IsEmpty)
            return;

        GameObject droppedObject = new GameObject("DroppedItem:" + itemData.GetDisplayName());
        droppedObject.layer = LayerMask.NameToLayer("Default");
        droppedObject.transform.position = position;
        droppedObject.transform.rotation = Quaternion.identity;

        SphereCollider collider = droppedObject.AddComponent<SphereCollider>();
        collider.radius = 0.45f;
        collider.center = Vector3.zero;
        collider.isTrigger = false;
        collider.material = null;

        Rigidbody droppedRigidbody = droppedObject.AddComponent<Rigidbody>();
        droppedRigidbody.mass = 3f;
        droppedRigidbody.useGravity = true;
        droppedRigidbody.linearDamping = 4f;
        droppedRigidbody.angularDamping = 4f;
        droppedRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        droppedRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        droppedRigidbody.AddForce(velocity, ForceMode.VelocityChange);

        DroppedItem droppedItem = droppedObject.AddComponent<DroppedItem>();
        droppedItem.Initialize(itemData, heldItemVisualizer);

        Destroy(droppedObject, droppedItemLifetime);
    }

    private System.Collections.IEnumerator CloseAfterDelay(BaseStation station, float delay)
    {
        yield return new WaitForSeconds(delay);
        station.OpenPanel(false);
    }

    private IInteractable GetNearestValidInteractable(float radius)
    {
        int maskValue = hitMask.value == 0 ? ~0 : hitMask.value;
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, maskValue);

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

    private IInteractable GetNearestValidDroppedItem(float radius)
    {
        int maskValue = hitMask.value == 0 ? ~0 : hitMask.value;
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, maskValue, QueryTriggerInteraction.Collide);

        DroppedItem nearestDropped = null;
        float nearestDist = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            DroppedItem dropped = hit.GetComponent<DroppedItem>() ?? hit.GetComponentInParent<DroppedItem>();
            if (dropped == null && hit.attachedRigidbody != null)
                dropped = hit.attachedRigidbody.GetComponent<DroppedItem>();

            if (dropped == null) continue;
            if (!dropped.CanInteractWith(this)) continue;

            float dist = Vector3.Distance(transform.position, hit.ClosestPoint(transform.position));
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestDropped = dropped;
            }
        }

        if (nearestDropped != null)
            return nearestDropped;

        if (maskValue != ~0)
        {
            hits = Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Collide);

            foreach (Collider hit in hits)
            {
                DroppedItem dropped = hit.GetComponent<DroppedItem>() ?? hit.GetComponentInParent<DroppedItem>();
                if (dropped == null && hit.attachedRigidbody != null)
                    dropped = hit.attachedRigidbody.GetComponent<DroppedItem>();

                if (dropped == null) continue;
                if (!dropped.CanInteractWith(this)) continue;

                float dist = Vector3.Distance(transform.position, hit.ClosestPoint(transform.position));
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestDropped = dropped;
                }
            }
        }

        return nearestDropped;
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

    public void RefreshHeldItemDisplay()
    {
        UpdateHeldItemHUD();
        RefreshHeldItemVisual();
    }

    private void RefreshHeldItemVisual()
    {
        if (heldItemVisualizer != null)
            heldItemVisualizer.Refresh(heldItem);
    }

    private void UpdateHeldItemHUD()
    {
        if (heldItemText == null)
            return;

        if (heldItem == null || heldItem.IsEmpty)
            heldItemText.text = "Holding: Nothing";
        else
            heldItemText.text = "Holding: " + heldItem.GetDisplayName();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, castRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}